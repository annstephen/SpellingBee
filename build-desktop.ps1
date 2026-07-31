<#
Builds the Angular frontend and publishes SpellingBee.Desktop as a self-contained,
single-file win-x64 executable that serves it. Output lands in .\publish\.
#>
$ErrorActionPreference = "Stop"

$repoRoot = $PSScriptRoot
$wwwroot = Join-Path $repoRoot "src\SpellingBee.Desktop\wwwroot"
$publishDir = Join-Path $repoRoot "publish"

Write-Host "Building Angular frontend..."
npm --prefix (Join-Path $repoRoot "frontend") run build
if ($LASTEXITCODE -ne 0) { throw "npm run build failed" }

Write-Host "Copying frontend build into $wwwroot ..."
if (Test-Path $wwwroot) { Remove-Item $wwwroot -Recurse -Force }
New-Item -ItemType Directory -Path $wwwroot | Out-Null
Copy-Item (Join-Path $repoRoot "frontend\dist\spelling-bee\browser\*") $wwwroot -Recurse

Write-Host "Publishing SpellingBee.Desktop (win-x64, self-contained, single file)..."
dotnet publish (Join-Path $repoRoot "src\SpellingBee.Desktop\SpellingBee.Desktop.csproj") `
    -c Release `
    -r win-x64 `
    --self-contained `
    -p:PublishSingleFile=true `
    -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

Write-Host "Done. Launch $publishDir\SpellingBee.Desktop.exe"
