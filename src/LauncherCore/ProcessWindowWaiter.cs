using System.Diagnostics;
using System.Linq;

namespace LauncherCore;

/// <summary>
/// Attend qu'un processus donné expose une fenêtre principale visible, avec timeout.
/// Factorise la boucle de polling déjà utilisée dans GenshinSteamLauncher et AumidWatcher.
/// </summary>
public static class ProcessWindowWaiter
{
    /// <param name="processName">Nom du processus sans .exe (ex: "GenshinImpact").</param>
    /// <param name="timeoutSeconds">Délai max d'attente.</param>
    /// <param name="pollIntervalMs">Intervalle entre deux vérifications.</param>
    /// <returns>Le Process trouvé (avec MainWindowHandle valide), ou null si timeout.</returns>
    public static Process? WaitForMainWindow(string processName, int timeoutSeconds = 60, int pollIntervalMs = 1000)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);

        while (DateTime.UtcNow < deadline)
        {
            var proc = Process.GetProcessesByName(processName)
                               .FirstOrDefault(p => p.MainWindowHandle != IntPtr.Zero);
            if (proc != null)
                return proc;

            Thread.Sleep(pollIntervalMs);
        }

        return null;
    }
}
