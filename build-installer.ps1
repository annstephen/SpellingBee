<#
Builds SpellingBee.Desktop (via build-desktop.ps1) and wraps the published output in an
Inno Setup installer. Output lands in .\installer\output\SpellingBeeSetup-<version>.exe.

Requires Inno Setup 6 (ISCC.exe) on PATH or in its default install location:
    winget install JRSoftware.InnoSetup
#>
$ErrorActionPreference = "Stop"

$repoRoot = $PSScriptRoot
$csprojPath = Join-Path $repoRoot "src\SpellingBee.Desktop\SpellingBee.Desktop.csproj"
$issPath = Join-Path $repoRoot "installer\SpellingBee.iss"

Write-Host "Publishing SpellingBee.Desktop..."
& (Join-Path $repoRoot "build-desktop.ps1")
if ($LASTEXITCODE -ne 0) { throw "build-desktop.ps1 failed" }

Write-Host "Reading app version from $csprojPath ..."
[xml]$csproj = Get-Content $csprojPath
$version = $csproj.Project.PropertyGroup.Version | Select-Object -First 1
if ([string]::IsNullOrWhiteSpace($version)) { throw "Could not read <Version> from $csprojPath" }
Write-Host "App version: $version"

Write-Host "Locating ISCC.exe (Inno Setup compiler)..."
$iscc = Get-Command iscc.exe -ErrorAction SilentlyContinue
if (-not $iscc) {
    $candidates = @(
        "$env:ProgramFiles(x86)\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
    )
    $found = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
    if ($found) { $iscc = @{ Source = $found } }
}
if (-not $iscc) {
    throw "ISCC.exe (Inno Setup 6) not found. Install it with:`n`n    winget install JRSoftware.InnoSetup`n`nthen re-run this script."
}

Write-Host "Compiling installer with $($iscc.Source) ..."
& $iscc.Source "/DMyAppVersion=$version" $issPath
if ($LASTEXITCODE -ne 0) { throw "ISCC.exe failed" }

$outputExe = Join-Path $repoRoot "installer\output\SpellingBeeSetup-$version.exe"
Write-Host "Done. Installer: $outputExe"
