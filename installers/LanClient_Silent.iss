; LanC Client — Silent Auto Installer
; Double-click or download and run → installs with ZERO prompts, ZERO clicks, NO admin rights.
; Installs to %LOCALAPPDATA%\LanC Client — no UAC prompt, no elevation needed.

#define AppName "LanC Client"
#define AppVersion "1.0.0"
#define AppPublisher "LanC"
#define AppExe "LanClient.exe"
#define OutputName "LanClient_AutoInstall"

[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
; Install to user's local app data — no admin rights needed
DefaultDirName={localappdata}\LanC Client
OutputDir=..\dist
OutputBaseFilename={#OutputName}
Compression=lzma2/ultra64
SolidCompression=yes
; No elevation — runs as current user, no UAC prompt
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=
CreateUninstallRegKey=yes
UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\{#AppExe}
SetupIconFile=client.ico
AllowNoIcons=yes
; Force silent — no wizard UI shown under any circumstance
DisableWelcomePage=yes
DisableDirPage=yes
DisableProgramGroupPage=yes
DisableReadyPage=yes
DisableFinishedPage=yes
DisableReadyMemo=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "..\LanClientExe\LanClient.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autodesktop}\LanC Client"; Filename: "{app}\{#AppExe}"; Comment: "LanC Client Dashboard"

[Registry]
; Auto-start on Windows login — HKCU, no admin needed
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; \
  ValueType: string; ValueName: "LanClient"; \
  ValueData: """{app}\{#AppExe}"""; Flags: uninsdeletevalue

[Run]
; Launch silently into tray immediately after install
Filename: "{app}\{#AppExe}"; Flags: nowait runhidden

[UninstallRun]
Filename: "taskkill"; Parameters: "/f /im LanClient.exe"; Flags: runhidden; RunOnceId: "KillLanClient"

[Code]
// Always force silent — hide wizard the moment it appears regardless of how launched
function InitializeSetup(): Boolean;
begin
  Result := True;
end;

procedure InitializeWizard();
begin
  WizardForm.Hide;
end;
