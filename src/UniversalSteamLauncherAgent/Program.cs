using System.Diagnostics;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using LauncherCore;
using LauncherShared;
using LauncherShared.Models;

const string MutexName = "Local\\UniversalSteamLauncher.AumidAgent";
const string StopEventName = "Local\\UniversalSteamLauncher.AumidAgent.Stop";
const string TaskName = "Universal Steam Launcher AUMID Agent";

string logPath = Path.Combine(Path.GetTempPath(), "UniversalSteamLauncherAgent.log");

if (args.Any(arg => string.Equals(arg, "--stop", StringComparison.OrdinalIgnoreCase)))
{
    try
    {
        using var stopEvent = EventWaitHandle.OpenExisting(StopEventName);
        stopEvent.Set();
        WaitForAgentToStop();
    }
    catch (WaitHandleCannotBeOpenedException)
    {
        // L'agent n'est pas en cours d'execution.
    }

    return;
}

if (args.Any(arg => string.Equals(arg, "--install-task", StringComparison.OrdinalIgnoreCase)))
{
    Environment.ExitCode = InstallScheduledTask();
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
            state.TrackedProcesses);
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
            if (hwnd == IntPtr.Zero
                || !ProcessWindowWaiter.TryGetProcessIdentity(process, out ProcessIdentity identity)
                || !state.TaggedWindowHandles.Add((identity, hwnd)))
                continue;

            AumidWindowTagger.TagResult result = AumidWindowTagger.Apply(
                hwnd,
                state.Aumid,
                out string? error);

            Log($"Fenetre taggee: processus={process.ProcessName} PID={process.Id} demarre={identity.StartedAtUtc:O} hwnd={hwnd}, result={result}, err={error}");
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

int InstallScheduledTask()
{
    try
    {
        string agentPath = Environment.ProcessPath
            ?? throw new InvalidOperationException("Chemin de l'agent introuvable.");
        string userName = WindowsIdentity.GetCurrent().Name;

        string script = $@"
$ErrorActionPreference = 'Stop'
$action = New-ScheduledTaskAction -Execute '{EscapePowerShell(agentPath)}'
$trigger = New-ScheduledTaskTrigger -AtLogOn
$principal = New-ScheduledTaskPrincipal -UserId '{EscapePowerShell(userName)}' -LogonType Interactive -RunLevel Highest
Register-ScheduledTask -TaskName '{EscapePowerShell(TaskName)}' -Action $action -Trigger $trigger -Principal $principal -Force -ErrorAction Stop | Out-Null
Start-ScheduledTask -TaskName '{EscapePowerShell(TaskName)}' -ErrorAction Stop
";

        string encodedScript = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-NonInteractive");
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");
        startInfo.ArgumentList.Add("-EncodedCommand");
        startInfo.ArgumentList.Add(encodedScript);

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Impossible de lancer PowerShell.");
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        Log($"Installation de la tache planifiee: code={process.ExitCode}, sortie={output}, erreur={error}");
        return process.ExitCode;
    }
    catch (Exception ex)
    {
        Log($"Impossible d'installer la tache planifiee: {ex}");
        return 1;
    }
}

string EscapePowerShell(string value) => value.Replace("'", "''", StringComparison.Ordinal);

void WaitForAgentToStop()
{
    DateTime deadline = DateTime.UtcNow.AddSeconds(10);

    while (DateTime.UtcNow < deadline)
    {
        try
        {
            using var mutex = new Mutex(true, MutexName, out bool createdNew);
            if (createdNew)
                return;
        }
        catch
        {
            return;
        }

        Thread.Sleep(100);
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
    public HashSet<ProcessIdentity> TrackedProcesses { get; } = new();
    public HashSet<(ProcessIdentity process, IntPtr hwnd)> TaggedWindowHandles { get; } = new();

    public bool Matches(LauncherConfig config) =>
        string.Equals(Executable, config.Executable, StringComparison.OrdinalIgnoreCase)
        && string.Equals(Aumid, config.Aumid, StringComparison.Ordinal);
}
