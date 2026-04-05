; AlarmTool Installer Script
; Compatible with Inno Setup 6.x

#define MyAppName "AlarmTool"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Your Name"
#define MyAppURL "https://github.com/jemaineosia/CaptchaAlarm"
#define MyAppExeName "AlarmTool.exe"
#define MyAppDescription "Aion Daeva Verification Monitor"

[Setup]
; Basic app info
AppId={{YOUR-GUID-HERE}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
LicenseFile=LICENSE.txt
; Output configuration
OutputDir=Installer
OutputBaseFilename=AlarmTool-Setup-v{#MyAppVersion}
SetupIconFile=CaptchaAlarm\Resources\icon.ico
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
; Privileges
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
; Target platform
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
; Uninstaller
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1; Check: not IsAdminInstallMode

[Files]
; Main executable and DLLs
Source: "CaptchaAlarm\bin\Release\net8.0-windows10.0.17763\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "CaptchaAlarm\bin\Release\net8.0-windows10.0.17763\*.dll"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs
Source: "CaptchaAlarm\bin\Release\net8.0-windows10.0.17763\*.json"; DestDir: "{app}"; Flags: ignoreversion

; Native x64 libraries (Tesseract OCR)
Source: "CaptchaAlarm\bin\Release\net8.0-windows10.0.17763\x64\*"; DestDir: "{app}\x64"; Flags: ignoreversion recursesubdirs

; Tesseract language data
Source: "CaptchaAlarm\bin\Release\net8.0-windows10.0.17763\tessdata\*"; DestDir: "{app}\tessdata"; Flags: ignoreversion recursesubdirs

; Resources (icon, alarm sound)
Source: "CaptchaAlarm\bin\Release\net8.0-windows10.0.17763\Resources\*"; DestDir: "{app}\Resources"; Flags: ignoreversion recursesubdirs

; Runtime files if doing framework-dependent deployment
; If you use self-contained, these are already included

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: quicklaunchicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
procedure InitializeWizard();
begin
  WizardForm.LicenseAcceptedRadio.Checked := True;
end;
