; UniversalSteamLauncher.iss
; ------------------------------------------------------------------
; Compile with Inno Setup (freeware) : https://jrsoftware.org/isinfo.php
; 1. Please execute the build-release.ps1  ( dist\) first.
; ------------------------------------------------------------------

#define MyAppName "Universal Steam Launcher"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Blinkixane"
#define MyAppExeName "LauncherGenerator.exe"

[Setup]
AppId={{B7B1B6B1-2B6E-4B7E-9C1D-8F1A6B0B6E10}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\UniversalSteamLauncher
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputBaseFilename=UniversalSteamLauncherSetup
Compression=lzma2
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64
SetupIconFile=..\Assets\AppIcon.ico
PrivilegesRequired=admin
UninstallDisplayIcon={app}\{#MyAppExeName}

VersionInfoVersion={#MyAppVersion}
VersionInfoProductVersion={#MyAppVersion}
VersionInfoCompany=Blinkixane
VersionInfoDescription=Universal Steam Launcher Setup
VersionInfoProductName=Universal Steam Launcher
VersionInfoCopyright=Copyright (c) 2026 Blinkixane

LicenseFile=..\LICENSE

[Languages]
Name: "french"; MessagesFile: "compiler:Languages\French.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "..\dist\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:Uninstall}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
[CustomMessages]

english.DesktopIcon=Create a desktop shortcut
french.DesktopIcon=Créer une icône sur le Bureau

english.ExtraIcons=Additional icons:
french.ExtraIcons=Icônes supplémentaires:

english.Uninstall=Uninstall {#MyAppName}
french.Uninstall=Désinstaller {#MyAppName}

english.RunNow=Launch {#MyAppName} now
french.RunNow=Lancer {#MyAppName} maintenant

[Tasks]
Name: "desktopicon"; Description: "{cm:DesktopIcon}"; GroupDescription: "{cm:ExtraIcons}"; Flags: unchecked

[Run]
Filename: "{app}\{#MyAppExeName}";Description: "{cm:RunNow}"; Flags: nowait postinstall skipifsilent

[Code]
procedure CurStepChanged(CurStep: TSetupStep);
var
  LangCode: String;
begin
  if CurStep = ssPostInstall then
  begin
    if ActiveLanguage = 'english' then
      LangCode := 'en'
    else
      LangCode := 'fr';

    SaveStringToFile(ExpandConstant('{app}\lang.txt'), LangCode, False);
  end;
end;
