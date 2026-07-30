namespace LauncherShared;

/// <summary>
/// Chemins fixes partagés par LauncherGenerator ET UniversalSteamLauncher.
/// Centralisé ici pour que les deux exécutables s'accordent toujours,
/// qu'ils tournent depuis le dépôt en dev (dotnet run) ou une fois
/// installés côte à côte dans Program Files.
/// </summary>
public static class AppPaths
{
    /// <summary>
    /// Dossier racine des configs par jeu (un sous-dossier "&lt;Nom&gt;\launcher.json"
    /// par jeu généré).
    ///
    /// Volontairement PAS à côté de UniversalSteamLauncher.exe : une fois l'appli
    /// installée dans Program Files, ce dossier n'est plus inscriptible par un
    /// utilisateur standard (il faudrait élever LauncherGenerator, ce qu'on veut
    /// éviter - lui n'a besoin d'aucun privilège particulier).
    /// %LocalAppData% est l'emplacement Windows normal pour les données propres
    /// à un utilisateur et à une appli, sans élévation.
    /// </summary>
    public static string LaunchersRoot =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "UniversalSteamLauncher", "Launchers");
}
