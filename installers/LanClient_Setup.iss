; LanC Client — Inno Setup Script
; Self-contained silent installer
; Installs to Program Files, creates Desktop + Start Menu shortcuts,
; launches the app in background (tray) after install.

#define AppName "LanC Client"
#define AppVersion "1.0.0"
#define AppPublisher "LanC"
#define AppExe "LanClient.exe"
#define OutputName "LanClient_Setup"

[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\LanC Client
DefaultGroupName=LanC Client
OutputDir=..\dist
OutputBaseFilename={#OutputName}
Compression=lzma2/ultra64
SolidCompression=yes
PrivilegesRequired=admin
DisableProgramGroupPage=yes
DisableWelcomePage=no
DisableReadyPage=no
CreateUninstallRegKey=yes
UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\{#AppExe}
; Use the app's own icon for the installer EXE
SetupIconFile=client.ico
AllowNoIcons=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
; Main executable — built from LanClientExe\LanClient.exe
Source: "..\LanClientExe\LanClient.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
; Desktop shortcut
Name: "{autodesktop}\LanC Client"; Filename: "{app}\{#AppExe}"; Comment: "LanC Client Dashboard"
; Start Menu shortcut
Name: "{group}\LanC Client"; Filename: "{app}\{#AppExe}"
Name: "{group}\Uninstall LanC Client"; Filename: "{uninstallexe}"

[Run]
; Launch the app after install (tray, no window shown)
Filename: "{app}\{#AppExe}"; Description: "Start LanC Client"; Flags: nowait postinstall skipifsilent
; Also launch silently when doing /VERYSILENT
Filename: "{app}\{#AppExe}"; Flags: nowait runhidden; Check: IsVerySilent

[Registry]
; Auto-start on Windows login (belt + suspenders alongside the app's own registry write)
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; \
  ValueType: string; ValueName: "LanClient"; \
  ValueData: """{app}\{#AppExe}"""; Flags: uninsdeletevalue

[UninstallRun]
; Kill the running process before uninstall
Filename: "taskkill"; Parameters: "/f /im LanClient.exe"; Flags: runhidden; RunOnceId: "KillLanClient"

[Code]
function IsVerySilent: Boolean;
var
  i: Integer;
begin
  Result := False;
  for i := 1 to ParamCount do
    if CompareText(ParamStr(i), '/VERYSILENT') = 0 then
    begin
      Result := True;
      Exit;
    end;
end;
