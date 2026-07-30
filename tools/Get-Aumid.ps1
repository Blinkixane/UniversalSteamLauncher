# Get-Aumid.ps1
#
# Aide a retrouver l'AUMID (AppUserModelID) d'un jeu deja lance au moins une
# fois, a coller dans LauncherGenerator si la detection automatique echoue.
# Get-Aumid.ps1
#
# Helps find the AUMID (AppUserModelID) of a game that has been launched at
# least once, to paste into LauncherGenerator if automatic detection fails.
#
# Usage :
#   1. Lance le jeu une fois / Launch the game once.
#   2. powershell -ExecutionPolicy Bypass -File Get-Aumid.ps1
#   3. Tape (une partie de) son nom / Type (part of) its name.
#   4. Copie la valeur AppID affichee / Copy the displayed AppID value.

$name = Read-Host "Nom du jeu / Game name"

$matches = Get-StartApps | Where-Object { $_.Name -like "*$name*" }

if (-not $matches) {
    Write-Host ""
    Write-Host "Aucune correspondance / No match for '$name'." -ForegroundColor Yellow
    Write-Host "Verifie que le jeu a ete lance au moins une fois." -ForegroundColor Yellow
    Write-Host "Make sure the game has been launched at least once." -ForegroundColor Yellow
    exit
}

Write-Host ""
$matches | Format-Table Name, AppID -AutoSize

Write-Host ""
Write-Host "Copie la valeur AppID ci-dessus dans LauncherGenerator." -ForegroundColor Green
Write-Host "Copy the AppID value above into LauncherGenerator." -ForegroundColor Green
