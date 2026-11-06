; Inno Setup script. Build with: iscc /DAppVersion=1.0.0 /DSourceDir=<publish folder> /DOutputDir=<dir> Transcriber.iss

#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif
#ifndef SourceDir
  #define SourceDir "..\..\publish\win-x64"
#endif
#ifndef OutputDir
  #define OutputDir "."
#endif

[Setup]
AppId={{6E1B7C0A-3D4F-4B1A-9C55-7A2F1E8D4B10}
AppName=Transcriber
AppVersion={#AppVersion}
AppPublisher=Transcriber
DefaultDirName={autopf}\Transcriber
DefaultGroupName=Transcriber
DisableProgramGroupPage=yes
OutputDir={#OutputDir}
OutputBaseFilename=Transcriber-{#AppVersion}-win-x64-setup
Compression=lzma2
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
UninstallDisplayIcon={app}\Transcriber.Maui.exe
WizardStyle=modern

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Transcriber"; Filename: "{app}\Transcriber.Maui.exe"
Name: "{autodesktop}\Transcriber"; Filename: "{app}\Transcriber.Maui.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; Flags: unchecked

[Run]
Filename: "{app}\Transcriber.Maui.exe"; Description: "Launch Transcriber"; Flags: nowait postinstall skipifsilent
