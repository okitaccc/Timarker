#ifndef AppVersion
  #define AppVersion "0.1.0"
#endif

[Setup]
AppId={{A8CB01C9-B48A-48FB-A7B0-20A728809FB3}
AppName=事刻 Timarker
AppVersion={#AppVersion}
AppPublisher=Timarker
DefaultDirName={localappdata}\Programs\Timarker
DefaultGroupName=事刻 Timarker
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\artifacts\installer
OutputBaseFilename=Timarker-Setup-{#AppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
Uninstallable=yes
CloseApplications=yes
RestartApplications=no
SetupLogging=yes
VersionInfoVersion={#AppVersion}
VersionInfoProductName=事刻 Timarker

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "chinesesimplified"; MessagesFile: "{#SourcePath}\ChineseSimplified.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "..\artifacts\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\事刻 Timarker"; Filename: "{app}\Timarker.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\事刻 Timarker"; Filename: "{app}\Timarker.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[InstallDelete]
Type: files; Name: "{app}\Timeline.exe"

[Run]
Filename: "{app}\Timarker.exe"; Description: "{cm:LaunchProgram,Timarker}"; Flags: nowait postinstall skipifsilent
