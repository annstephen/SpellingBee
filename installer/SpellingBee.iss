; Inno Setup script for SpellingBee.
;
; Packages the output of `..\publish\` (produced by build-desktop.ps1) into a single
; SpellingBeeSetup-<version>.exe installer. Not meant to be compiled by hand — run
; `..\build-installer.ps1` from the repo root, which refreshes publish\, resolves the
; app version from the csproj, and invokes ISCC with /DMyAppVersion set.
;
; Requires Inno Setup 6 (ISCC.exe): winget install JRSoftware.InnoSetup

#ifndef MyAppVersion
  #define MyAppVersion "0.0.0"
#endif

#define MyAppName "SpellingBee"
#define MyAppPublisher "SpellingBee"
#define MyAppExeName "SpellingBee.Desktop.exe"
#define PublishDir "..\publish"

[Setup]
; This GUID identifies the app across versions. Do not change it — Inno Setup uses it
; to detect an existing install and upgrade in place instead of side-installing.
AppId={{0CA434E1-ABD0-4946-8101-E6E4A87AE406}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=output
OutputBaseFilename=SpellingBeeSetup-{#MyAppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent

; Deliberately no [UninstallDelete] entries: everything mutable (SQLite DB, audio
; cache, WebView2 profile, config overlay) lives under %LOCALAPPDATA%\SpellingBee\,
; outside {app}, and is left in place on uninstall so a user's practice history and
; API-key overlay survive reinstalls/upgrades.

[Code]
const
  WebView2ClientId = '{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}';
  WebView2BootstrapperUrl = 'https://go.microsoft.com/fwlink/p/?LinkId=2124703';

function IsWebView2RuntimeInstalled: Boolean;
var
  Version: String;
begin
  Result :=
    RegQueryStringValue(HKLM, 'SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\' + WebView2ClientId, 'pv', Version) or
    RegQueryStringValue(HKLM, 'SOFTWARE\Microsoft\EdgeUpdate\Clients\' + WebView2ClientId, 'pv', Version) or
    RegQueryStringValue(HKCU, 'SOFTWARE\Microsoft\EdgeUpdate\Clients\' + WebView2ClientId, 'pv', Version);
end;

function InitializeSetup(): Boolean;
var
  BootstrapperPath: String;
  DownloadCmd: String;
  ResultCode: Integer;
begin
  Result := True;

  if IsWebView2RuntimeInstalled then
    Exit;

  if MsgBox(
    'SpellingBee requires the Microsoft Edge WebView2 Runtime, which was not detected on this machine.' + #13#10 + #13#10 +
    'Setup can download and install it now (requires an internet connection). Continue?',
    mbConfirmation, MB_YESNO) <> IDYES then
    Exit;

  BootstrapperPath := ExpandConstant('{tmp}\MicrosoftEdgeWebview2Setup.exe');
  DownloadCmd := Format(
    '-NoProfile -ExecutionPolicy Bypass -Command "Invoke-WebRequest -Uri ''%s'' -OutFile ''%s''"', [
    WebView2BootstrapperUrl, BootstrapperPath]);

  if not Exec('powershell.exe', DownloadCmd, '', SW_HIDE, ewWaitUntilTerminated, ResultCode) or (ResultCode <> 0) then
  begin
    MsgBox(
      'Could not download the WebView2 Runtime automatically. Please install it manually from ' +
      'https://developer.microsoft.com/microsoft-edge/webview2/ and re-run this installer.',
      mbError, MB_OK);
    Exit;
  end;

  if not Exec(BootstrapperPath, '/silent /install', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) or (ResultCode <> 0) then
  begin
    MsgBox(
      'The WebView2 Runtime installer did not complete successfully. SpellingBee may not run ' +
      'until it is installed manually from https://developer.microsoft.com/microsoft-edge/webview2/.',
      mbError, MB_OK);
  end;
end;
