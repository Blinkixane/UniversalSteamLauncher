using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Text.Json;
using LauncherCore;
using LauncherShared;
using LauncherShared.Models;

class Program
{
    static string logPath = Path.Combine(Path.GetTempPath(), "UniversalSteamLauncher.log");

    static void Log(string msg) =>
        File.AppendAllText(logPath, $"{DateTime.Now:HH:mm:ss.fff} - {msg}{Environment.NewLine}");

    static int Main(string[] args)
    {
        string? game = ParseGameArg(args);
        if (string.IsNullOrWhiteSpace(game))
        {
            Console.WriteLine("Usage : UniversalSteamLauncher.exe --game \"NomDuJeu\"");
            return 1;
        }

        // Un log par jeu, pour ne pas melanger les diagnostics si plusieurs
        // launchers generes sont utilises sur la meme machine.
        logPath = Path.Combine(Path.GetTempPath(), $"{game}LauncherAumid.log");
        try { File.Delete(logPath); } catch { /* pas grave si absent */ }

        bool isAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent())
            .IsInRole(WindowsBuiltInRole.Administrator);
        Log($"Demarrage pour '{game}'. Elevated = {isAdmin}");

        // IMPORTANT : on resout le chemin de la config via AppPaths.LaunchersRoot
        // (%LocalAppData%\UniversalSteamLauncher\Launchers), PAS par rapport au
        // dossier de l'exe ni au repertoire courant. Une fois installe dans
        // Program Files, le dossier de l'exe n'est plus inscriptible sans
        // elevation, et depuis un raccourci epingle le CWD n'est de toute facon
        // pas garanti (souvent System32).
        string configPath = Path.Combine(AppPaths.LaunchersRoot, game, "launcher.json");

        if (!File.Exists(configPath))
        {
            Log($"Config introuvable : {configPath}");
            Console.WriteLine($"Config introuvable : {configPath}");
            return 1;
        }

        LauncherConfig config;
        try
        {
            string json = File.ReadAllText(configPath);
            config = JsonSerializer.Deserialize<LauncherConfig>(json)
                      ?? throw new InvalidDataException("Fichier JSON vide ou invalide.");
        }
        catch (Exception ex)
        {
            Log($"Erreur de lecture/parsing de la config : {ex}");
            Console.WriteLine($"Config invalide : {ex.Message}");
            return 1;
        }

        if (string.IsNullOrWhiteSpace(config.SteamUri) || string.IsNullOrWhiteSpace(config.Executable))
        {
            Log("Config incomplete (SteamUri ou Executable manquant).");
            Console.WriteLine("Config incomplete : SteamUri et Executable sont obligatoires.");
            return 1;
        }

        Log($"Config chargee : SteamUri={config.SteamUri}, Executable={config.Executable}, Aumid={config.Aumid}");

        Process.Start(new ProcessStartInfo(config.SteamUri) { UseShellExecute = true });
        Log("steam:// lance");

        // Le nom de processus a surveiller est derive du champ Executable de
        // la config, pas du nom affiche du jeu (les deux peuvent differer,
        // ex: "Genshin" vs process reel "GenshinImpact").
        string processName = Path.GetFileNameWithoutExtension(config.Executable);
        var proc = ProcessWindowWaiter.WaitForMainWindow(processName);

        if (proc == null)
        {
            Log($"Fenetre de '{processName}' jamais trouvee apres 60s, abandon.");
            Console.WriteLine($"Le jeu n'a pas ete detecte apres 60s (process attendu : {processName}).");
            return 1;
        }

        Log($"Fenetre trouvee : PID {proc.Id}, hwnd {proc.MainWindowHandle}");

        if (string.IsNullOrWhiteSpace(config.Aumid))
        {
            Log("Aucun AUMID configure pour ce jeu, regroupement ignore.");
            return 0;
        }

        var result = AumidWindowTagger.Apply(proc.MainWindowHandle, config.Aumid, out var err);
        Log($"Tag result: {result}, err: {err}");

        return result == AumidWindowTagger.TagResult.Success ? 0 : 1;
    }

    static string? ParseGameArg(string[] args)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], "--game", StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        }
        return null;
    }
}
