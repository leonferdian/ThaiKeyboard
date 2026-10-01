[Setup]
AppName=Thai Virtual Keyboard
AppVersion=1.0
DefaultDirName={autopf}\Thai Virtual Keyboard
DefaultGroupName=Thai Virtual Keyboard
OutputDir=C:\project\ThaiKeyboard\Installer
OutputBaseFilename=ThaiKeyboardSetup
SetupIconFile=C:\project\ThaiKeyboard\icon.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\ThaiKeyboard.exe
PrivilegesRequired=lowest

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "C:\project\ThaiKeyboard\bin\Release\net10.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Thai Virtual Keyboard"; Filename: "{app}\ThaiKeyboard.exe"
Name: "{autodesktop}\Thai Virtual Keyboard"; Filename: "{app}\ThaiKeyboard.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\ThaiKeyboard.exe"; Description: "{cm:LaunchProgram,Thai Virtual Keyboard}"; Flags: nowait postinstall skipifsilent
