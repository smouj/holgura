#ifndef AppVersion
#define AppVersion "1.1.0"
#endif

[Setup]
AppId={{E1B48E32-3D68-4D85-9B75-7E95BDA6064E}
AppName=Holgura
AppVersion={#AppVersion}
AppPublisher=smouj
AppPublisherURL=https://github.com/smouj/holgura
AppSupportURL=https://github.com/smouj/holgura/issues
AppUpdatesURL=https://github.com/smouj/holgura/releases
DefaultDirName={localappdata}\Programs\Holgura
DefaultGroupName=Holgura
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
LicenseFile=..\LICENSE
SetupIconFile=..\assets\holgura.ico
UninstallDisplayIcon={app}\Holgura.App.exe
OutputDir=..\artifacts
OutputBaseFilename=Holgura-Setup-win-x64
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no

[Tasks]
Name: "desktopicon"; Description: "Crear un acceso directo en el escritorio"; GroupDescription: "Accesos directos:"

[Files]
Source: "..\artifacts\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\Holgura"; Filename: "{app}\Holgura.App.exe"; IconFilename: "{app}\Holgura.App.exe"; IconIndex: 0
Name: "{autodesktop}\Holgura"; Filename: "{app}\Holgura.App.exe"; IconFilename: "{app}\Holgura.App.exe"; IconIndex: 0; Tasks: desktopicon

[Run]
Filename: "{app}\Holgura.App.exe"; Description: "Iniciar Holgura"; Flags: postinstall nowait skipifsilent
