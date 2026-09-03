; UniversalSteamLauncher.iss
; ------------------------------------------------------------------
; Compile with Inno Setup (freeware) : https://jrsoftware.org/isinfo.php
; 1. Please execute the build-release.ps1  ( dist\) first.
; ------------------------------------------------------------------

#define MyAppName "Universal Steam Launcher"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Blinkixane"
#define MyAppExeName "LauncherGenerator.exe"
#define MyAgentTaskName "Universal Steam Launcher AUMID Agent"

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
ArchitecturesInstallIn64BitMode=x64compatible
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

[Registry]
; Supprime l'ancien demarrage HKLM Run utilise par les versions precedentes.
; L'agent est maintenant lance par une tache planifiee avec elevation.
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: none; ValueName: "UniversalSteamLauncherAgent"; Flags: deletevalue

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

english.StartAgent=Start taskbar integration in the background
french.StartAgent=Lancer l'integration de la barre des taches en arriere-plan

[Tasks]
Name: "desktopicon"; Description: "{cm:DesktopIcon}"; GroupDescription: "{cm:ExtraIcons}"; Flags: unchecked

[Run]
Filename: "{app}\UniversalSteamLauncherAgent.exe"; Parameters: "--stop"; Flags: runhidden
Filename: "{app}\UniversalSteamLauncherAgent.exe"; Parameters: "--install-task"; Flags: runhidden
Filename: "{app}\{#MyAppExeName}";Description: "{cm:RunNow}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{sys}\schtasks.exe"; Parameters: "/End /TN ""{#MyAgentTaskName}"""; Flags: runhidden; RunOnceId: StopUniversalSteamLauncherAgent
Filename: "{sys}\schtasks.exe"; Parameters: "/Delete /TN ""{#MyAgentTaskName}"" /F"; Flags: runhidden; RunOnceId: DeleteUniversalSteamLauncherAgent

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
