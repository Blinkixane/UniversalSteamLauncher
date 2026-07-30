param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

# Compile les deux exe en autonome / fichier unique (aucune dependance .NET
# requise sur la machine de l'utilisateur final) et les depose cote a cote
# dans dist\ - exactement la disposition attendue par UniversalSteamLauncher.iss
# et par la recherche "meme dossier" de LauncherGenerator (voir FindUniversalLauncherExe).

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$dist = Join-Path $root "dist"

if (Test-Path $dist) { Remove-Item $dist -Recurse -Force }
New-Item -ItemType Directory -Path $dist | Out-Null

Write-Host "Publication de UniversalSteamLauncher (lanceur runtime)..." -ForegroundColor Cyan
dotnet publish "$root\src\UniversalSteamLauncher\UniversalSteamLauncher.csproj" `
    -c $Configuration -r $Runtime --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=none `
    -o "$dist"
if ($LASTEXITCODE -ne 0) { throw "Echec de publication de UniversalSteamLauncher." }

Write-Host "Publication de LauncherGenerator (outil d'ajout de jeu)..." -ForegroundColor Cyan
dotnet publish "$root\src\LauncherGenerator\LauncherGenerator.csproj" `
    -c $Configuration -r $Runtime --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=none `
    -o "$dist"
if ($LASTEXITCODE -ne 0) { throw "Echec de publication de LauncherGenerator." }

Write-Host ""
Write-Host "Fichiers prets dans $dist :" -ForegroundColor Green
Get-ChildItem $dist | Select-Object Name, Length | Format-Table -AutoSize

Write-Host ""
Write-Host "Etape suivante : ouvre UniversalSteamLauncher.iss dans Inno Setup et compile"
Write-Host "(ou 'iscc UniversalSteamLauncher.iss' si ISCC.exe est dans le PATH)."
