using System.Diagnostics;
using System.Text.Json;
using LauncherCore;
using LauncherShared;
using LauncherShared.Models;

const string MutexName = "Local\\UniversalSteamLauncher.AumidAgent";
const string StopEventName = "Local\\UniversalSteamLauncher.AumidAgent.Stop";

string logPath = Path.Combine(Path.GetTempPath(), "UniversalSteamLauncherAgent.log");

if (args.Any(arg => string.Equals(arg, "--stop", StringComparison.OrdinalIgnoreCase)))
{
    try
    {
        using var stopEvent = EventWaitHandle.OpenExisting(StopEventName);
        stopEvent.Set();
    }
    catch (WaitHandleCannotBeOpenedException)
    {
        // L'agent n'est pas en cours d'execution.
    }

    return;
}

using var mutex = new Mutex(true, MutexName, out bool createdNew);
if (!createdNew)
    return;

using var stopSignal = new EventWaitHandle(
    false,
    EventResetMode.ManualReset,
    StopEventName);

Log("Agent AUMID demarre.");

try
{
    Run(stopSignal);
}
catch (Exception ex)
{
    Log($"Erreur fatale: {ex}");
}

void Run(EventWaitHandle stopEvent)
{
    var states = new Dictionary<string, WatchState>(StringComparer.OrdinalIgnoreCase);

    while (!stopEvent.WaitOne(0))
    {
        HashSet<string> activeConfigPaths = new(StringComparer.OrdinalIgnoreCase);

        foreach (string configPath in FindConfigPaths())
        {
            activeConfigPaths.Add(configPath);

            LauncherConfig? config = ReadConfig(configPath);
            if (config == null
                || string.IsNullOrWhiteSpace(config.Executable)
                || string.IsNullOrWhiteSpace(config.Aumid))
            {
                continue;
            }

            if (!states.TryGetValue(configPath, out WatchState? state)
                || !state.Matches(config))
            {
                state = new WatchState(config.Executable, config.Aumid);
                states[configPath] = state;
                Log($"Configuration surveillee: {config.Name} ({config.Executable}).");
            }

            TagProcessTree(state);
        }

        foreach (string stalePath in states.Keys.Except(activeConfigPaths).ToArray())
            states.Remove(stalePath);

        stopEvent.WaitOne(1000);
    }

    Log("Agent AUMID arrete.");
}

void TagProcessTree(WatchState state)
{
    IReadOnlyList<Process> processes;
    try
    {
        processes = ProcessWindowWaiter.FindProcessTree(
            state.Executable,
            DateTime.MinValue,
            state.TrackedProcessIds);
    }
    catch (Exception ex)
    {
        Log($"Impossible d'inspecter '{state.Executable}': {ex.Message}");
        return;
    }

    foreach (Process process in processes)
    {
        try
        {
            IntPtr hwnd = process.MainWindowHandle;
            if (hwnd == IntPtr.Zero || !state.TaggedWindowHandles.Add((process.Id, hwnd)))
                continue;

            AumidWindowTagger.TagResult result = AumidWindowTagger.Apply(
                hwnd,
                state.Aumid,
                out string? error);

            Log($"Fenetre taggee: processus={process.ProcessName} PID={process.Id} hwnd={hwnd}, result={result}, err={error}");
        }
        catch (Exception ex)
        {
            Log($"Impossible de tagger le PID {process.Id}: {ex.Message}");
        }
    }
}

IEnumerable<string> FindConfigPaths()
{
    if (!Directory.Exists(AppPaths.LaunchersRoot))
        yield break;

    IEnumerable<string> launcherDirectories;
    try
    {
        launcherDirectories = Directory.EnumerateDirectories(AppPaths.LaunchersRoot);
    }
    catch (Exception ex)
    {
        Log($"Impossible de lire le dossier des configurations: {ex.Message}");
        yield break;
    }

    foreach (string directory in launcherDirectories)
        yield return Path.Combine(directory, "launcher.json");
}

LauncherConfig? ReadConfig(string configPath)
{
    try
    {
        if (!File.Exists(configPath))
            return null;

        return JsonSerializer.Deserialize<LauncherConfig>(File.ReadAllText(configPath));
    }
    catch (Exception ex)
    {
        Log($"Configuration ignoree ({configPath}): {ex.Message}");
        return null;
    }
}

void Log(string message)
{
    try
    {
        File.AppendAllText(
            logPath,
            $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - {message}{Environment.NewLine}");
    }
    catch
    {
        // Un agent d'arriere-plan ne doit pas s'arreter a cause du fichier log.
    }
}

sealed class WatchState
{
    public WatchState(string executable, string aumid)
    {
        Executable = executable;
        Aumid = aumid;
    }

    public string Executable { get; }
    public string Aumid { get; }
    public HashSet<int> TrackedProcessIds { get; } = new();
    public HashSet<(int processId, IntPtr hwnd)> TaggedWindowHandles { get; } = new();

    public bool Matches(LauncherConfig config) =>
        string.Equals(Executable, config.Executable, StringComparison.OrdinalIgnoreCase)
        && string.Equals(Aumid, config.Aumid, StringComparison.Ordinal);
}
