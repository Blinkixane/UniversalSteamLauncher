using System.Text.Json;
using LauncherShared.Models;


namespace LauncherGenerator.Services;


public class ConfigWriter
{
    /// <param name="launchersRoot">
    /// Racine des configs, typiquement LauncherShared.AppPaths.LaunchersRoot
    /// (%LocalAppData%\UniversalSteamLauncher\Launchers) : c'est la que le
    /// launcher ira chercher sa config au demarrage. Passe en parametre plutot
    /// que lu directement ici pour rester testable independamment de AppPaths.
    /// </param>
    public void Save(LauncherConfig config, string launchersRoot)
    {
        string folder = Path.Combine(launchersRoot, config.Name);

        Directory.CreateDirectory(folder);


        string file =
            Path.Combine(
                folder,
                "launcher.json");


        string json =
            JsonSerializer.Serialize(
                config,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });


        File.WriteAllText(
            file,
            json);


        Console.WriteLine();
        Console.WriteLine("Configuration créée :");
        Console.WriteLine(file);
    }
}