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
