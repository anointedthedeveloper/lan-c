; LanC Server — Inno Setup Script
; Standard installer, requires admin, installs to Program Files.

#define AppName "LanC Server"
#define AppVersion "1.0.0"
#define AppPublisher "LanC"
#define AppExe "LanServer.exe"
#define OutputName "LanServer_Setup"

[Setup]
AppId={{B2C3D4E5-F6A7-8901-BCDE-F12345678901}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\LanC Server
DefaultGroupName=LanC Server
OutputDir=..\dist
OutputBaseFilename={#OutputName}
Compression=lzma2/ultra64
SolidCompression=yes
PrivilegesRequired=admin
DisableProgramGroupPage=yes
; Use the app's own icon for the installer EXE
SetupIconFile=server.ico

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "..\LanServerExe\LanServer.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\LanServerExe\Assets\*"; DestDir: "{app}\Assets"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autodesktop}\LanC Server"; Filename: "{app}\{#AppExe}"
Name: "{group}\LanC Server"; Filename: "{app}\{#AppExe}"
Name: "{group}\Uninstall LanC Server"; Filename: "{uninstallexe}"

[Run]
Filename: "{app}\{#AppExe}"; Description: "Launch LanC Server"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "taskkill"; Parameters: "/f /im LanServer.exe"; Flags: runhidden; RunOnceId: "KillLanServer"
