using LauncherCore;
using LauncherGenerator.Services;
using LauncherShared;
using LauncherShared.Models;

Console.WriteLine("================================");
Console.WriteLine(Strings.Get("banner.title"));
Console.WriteLine(Strings.Get("banner.subtitle"));
Console.WriteLine(Strings.Get("banner.author"));
Console.WriteLine("================================");
Console.WriteLine();

string launcherExePath;
try
{
    launcherExePath = FindUniversalLauncherExe();
}
catch (FileNotFoundException ex)
{
    Console.WriteLine(ex.Message);
    Console.WriteLine(Strings.Get("error.launcherNotFound.hint"));
    return;
}

Console.Write(Strings.Get("prompt.name"));
string name = (Console.ReadLine() ?? "").Trim();

Console.Write(Strings.Get("prompt.steamUri"));
string steamInput = (Console.ReadLine() ?? "").Trim();
string steamUri = steamInput.StartsWith("steam://", StringComparison.OrdinalIgnoreCase)
    ? steamInput
    : $"steam://rungameid/{steamInput}";

Console.Write(Strings.Get("prompt.exePath"));
string exePath = (Console.ReadLine() ?? "").Trim('"');

Console.Write(Strings.Get("prompt.iconPath"));
string iconInput = (Console.ReadLine() ?? "").Trim('"');
string icon = string.IsNullOrWhiteSpace(iconInput) ? exePath : iconInput;

Console.WriteLine();
Console.WriteLine(Strings.Get("aumid.searching"));

var resolver = new AumidResolver();
string? aumid = resolver.Find(name);

if (aumid == null)
{
    Console.WriteLine(Strings.Get("aumid.notFound.line1"));
    Console.WriteLine(Strings.Get("aumid.notFound.line2"));
    Console.WriteLine(Strings.Get("aumid.notFound.line3"));
    Console.WriteLine(Strings.Get("aumid.notFound.line4"));
    Console.Write(Strings.Get("aumid.prompt"));
    aumid = Console.ReadLine();
}

Console.WriteLine();
Console.WriteLine(Strings.Get("summary.title"));
Console.WriteLine(Strings.Get("summary.separator"));
Console.WriteLine(Strings.Get("summary.name", name));
Console.WriteLine(Strings.Get("summary.steam", steamUri));
Console.WriteLine(Strings.Get("summary.exe", exePath));
Console.WriteLine(Strings.Get("summary.icon", icon));
Console.WriteLine(Strings.Get("summary.aumid", aumid));

var config = new LauncherConfig
{
    Name = name,
    SteamUri = steamUri,
    Executable = exePath,
    Icon = icon,
    Aumid = aumid ?? ""
};

// La config est ecrite dans %LocalAppData%\UniversalSteamLauncher\Launchers
// (AppPaths.LaunchersRoot) - pas a cote de UniversalSteamLauncher.exe, qui
// n'est plus inscriptible sans elevation une fois installe dans Program Files.
var writer = new ConfigWriter();
writer.Save(config, AppPaths.LaunchersRoot);

string shortcutPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
    $"{name}.lnk");

Console.WriteLine();
Console.WriteLine(Strings.Get("shortcut.creating", shortcutPath));
ShortcutAumidStore.CreateGameShortcut(
    shortcutPath,
    launcherExePath,
    $"--game \"{name}\"",
    icon,
    config.Aumid);

Console.WriteLine(!string.IsNullOrWhiteSpace(config.Aumid)
    ? Strings.Get("shortcut.doneWithAumid")
    : Strings.Get("shortcut.doneWithoutAumid"));

Console.WriteLine();
Console.WriteLine(Strings.Get("done.line1"));
Console.WriteLine(Strings.Get("done.line2"));

static string FindUniversalLauncherExe()
{
    // Cas "produit installe" : les deux exe sont livres cote a cote dans le
    // meme dossier (ex: Program Files\UniversalSteamLauncher\). C'est le chemin
    // rapide et c'est ce que produit build-release.ps1 + l'installeur Inno Setup.
    string sameFolder = Path.Combine(AppContext.BaseDirectory, "UniversalSteamLauncher.exe");
    if (File.Exists(sameFolder))
        return sameFolder;

    // Cas "arbre de dev" (dotnet run depuis le depot source, sans avoir publie) :
    // on remonte depuis le dossier de LauncherGenerator.exe (bin/Debug|Release/net8.0-windows/...)
    // jusqu'a trouver le dossier solution contenant UniversalSteamLauncher/,
    // puis on cherche l'exe compile (Release en priorite, sinon Debug).
    var dir = new DirectoryInfo(AppContext.BaseDirectory);

    while (dir != null)
    {
        string projFile = Path.Combine(dir.FullName, "UniversalSteamLauncher", "UniversalSteamLauncher.csproj");
        if (File.Exists(projFile))
        {
            string binRoot = Path.Combine(dir.FullName, "UniversalSteamLauncher", "bin");
            foreach (string configName in new[] { "Release", "Debug" })
            {
                string candidate = Path.Combine(binRoot, configName, "net8.0-windows", "UniversalSteamLauncher.exe");
                if (File.Exists(candidate))
                    return candidate;
            }
        }
        dir = dir.Parent;
    }

    throw new FileNotFoundException(Strings.Get("error.launcherNotFound"));
}
