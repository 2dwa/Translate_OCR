#define MyAppName "Translate"
#define MyAppVersion "0.4.1"
#define MyAppExeName "Translate.exe"
#ifndef BundleDir
  #define BundleDir "..\dist\Translate-0.4.1"
#endif

[Setup]
AppId={{0D8E655D-5C5E-44ED-9DB2-894DAEF14BC5}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher=GPT-6 ASTRA
AppPublisherURL=https://github.com/2dwa/Translate_OCR
LicenseFile=..\LICENSE
DefaultDirName={localappdata}\Programs\Translate
DefaultGroupName={#MyAppName}
OutputDir=..\dist\installer
OutputBaseFilename=Translate-Setup
Compression=lzma2/fast
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64compatible
ArchitecturesAllowed=x64compatible
PrivilegesRequired=lowest
WizardStyle=modern
MinVersion=10.0.19041
DiskSpanning=yes
DiskSliceSize=2000000000

[Files]
Source: "{#BundleDir}\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "其他选项："; Flags: unchecked

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "启动 {#MyAppName}"; Flags: nowait postinstall skipifsilent
