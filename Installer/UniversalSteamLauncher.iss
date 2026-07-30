; UniversalSteamLauncher.iss
; ------------------------------------------------------------------
; Compile avec Inno Setup (gratuit) : https://jrsoftware.org/isinfo.php
; 1. Lance d'abord build-release.ps1 (remplit dist\).
; 2. Ouvre ce fichier dans Inno Setup Compiler, ou en ligne de commande :
;      iscc UniversalSteamLauncher.iss
; Produit : Output\UniversalSteamLauncherSetup.exe
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
; L'installation dans Program Files necessite l'admin (une seule fois, a
; l'installation) - a ne pas confondre avec l'UAC demande a CHAQUE lancement
; de UniversalSteamLauncher.exe (necessaire pour le tag AUMID runtime, voir
; app.manifest du projet).
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
; Copie tout le dossier dist\ depuis la racine du projet.
; Contient les deux exécutables autonomes.
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
// Ecrit la langue choisie dans l'assistant d'installation (french/english,
// voir [Languages] ci-dessus) dans un petit fichier "lang.txt" a cote des
// exe installes. LauncherShared.AppLanguageProvider le lit au demarrage
// pour savoir dans quelle langue afficher les textes du generateur.
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
