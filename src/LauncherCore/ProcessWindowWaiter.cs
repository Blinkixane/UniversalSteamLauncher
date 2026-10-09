using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace LauncherCore;

public readonly record struct ProcessIdentity(int ProcessId, DateTime StartedAtUtc);

public static class ProcessWindowWaiter
{
    public static IReadOnlyList<Process> FindProcessTree(
        string executablePath,
        DateTime startedAfterUtc,
        ISet<ProcessIdentity> trackedProcesses)
    {
        string expectedName = Path.GetFileNameWithoutExtension(executablePath);
        string? expectedPath = TryGetFullPath(executablePath);

        ProcessSnapshot[] snapshots = Process.GetProcesses()
            .Select(TryCreateSnapshot)
            .Where(snapshot => snapshot.HasValue)
            .Select(snapshot => snapshot!.Value)
            .ToArray();

        var snapshotsByIdentity = snapshots.ToDictionary(snapshot => snapshot.Identity);
        var snapshotIdentityById = snapshots.ToDictionary(
            snapshot => snapshot.Identity.ProcessId,
            snapshot => snapshot.Identity);
        var parentById = ReadParentProcessIds();
        var treeIdentities = trackedProcesses
            .Where(snapshotsByIdentity.ContainsKey)
            .ToHashSet();

        foreach (ProcessSnapshot snapshot in snapshots)
        {
            if (IsTargetProcess(snapshot.Process, expectedName, expectedPath)
                && snapshot.Identity.StartedAtUtc >= startedAfterUtc)
            {
                treeIdentities.Add(snapshot.Identity);
            }
        }

        bool added;
        do
        {
            added = false;
            foreach (ProcessSnapshot snapshot in snapshots)
            {
                if (treeIdentities.Contains(snapshot.Identity))
                    continue;

                if (parentById.TryGetValue(snapshot.Identity.ProcessId, out int parentId)
                    && snapshotIdentityById.TryGetValue(parentId, out ProcessIdentity parentIdentity)
                    && treeIdentities.Contains(parentIdentity))
                {
                    treeIdentities.Add(snapshot.Identity);
                    added = true;
                }
            }
        }
        while (added);

        trackedProcesses.Clear();
        trackedProcesses.UnionWith(treeIdentities);

        return treeIdentities
            .Where(snapshotsByIdentity.ContainsKey)
            .Select(identity => snapshotsByIdentity[identity].Process)
            .ToArray();
    }

    public static bool TryGetProcessIdentity(Process process, out ProcessIdentity identity)
    {
        try
        {
            identity = new ProcessIdentity(
                process.Id,
                process.StartTime.ToUniversalTime());
            return true;
        }
        catch
        {
            identity = default;
            return false;
        }
    }

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

    private static bool IsTargetProcess(Process process, string expectedName, string? expectedPath)
    {
        try
        {
            if (!string.Equals(process.ProcessName, expectedName, StringComparison.OrdinalIgnoreCase))
                return false;
        }
        catch
        {
            return false;
        }

        if (expectedPath == null)
            return true;

        try
        {
            string? actualPath = process.MainModule?.FileName;
            if (actualPath == null)
                return false;

            return string.Equals(
                TryGetFullPath(actualPath),
                expectedPath,
                StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            // Un chemin non verifiable ne doit jamais etre associe a une
            // configuration : cela pourrait appliquer le mauvais AUMID.
            return false;
        }
    }

    private static ProcessSnapshot? TryCreateSnapshot(Process process)
    {
        if (!TryGetProcessIdentity(process, out ProcessIdentity identity))
            return null;

        return new ProcessSnapshot(process, identity);
    }

    private readonly record struct ProcessSnapshot(Process Process, ProcessIdentity Identity);

    private static string? TryGetFullPath(string path)
    {
        try
        {
            return Path.GetFullPath(path);
        }
        catch
        {
            return null;
        }
    }

    private static Dictionary<int, int> ReadParentProcessIds()
    {
        var result = new Dictionary<int, int>();

        if (!OperatingSystem.IsWindows())
            return result;

        IntPtr snapshot = CreateToolhelp32Snapshot(Th32csSnapProcess, 0);
        if (snapshot == InvalidHandleValue)
            return result;

        try
        {
            var entry = new ProcessEntry32
            {
                Size = (uint)Marshal.SizeOf<ProcessEntry32>()
            };

            if (!Process32First(snapshot, ref entry))
                return result;

            do
            {
                result[(int)entry.ProcessId] = (int)entry.ParentProcessId;
                entry.Size = (uint)Marshal.SizeOf<ProcessEntry32>();
            }
            while (Process32Next(snapshot, ref entry));
        }
        finally
        {
            CloseHandle(snapshot);
        }

        return result;
    }

    private const uint Th32csSnapProcess = 0x00000002;
    private static readonly IntPtr InvalidHandleValue = new(-1);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr CreateToolhelp32Snapshot(uint flags, uint processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Process32First(IntPtr snapshot, ref ProcessEntry32 entry);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Process32Next(IntPtr snapshot, ref ProcessEntry32 entry);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr handle);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ProcessEntry32
    {
        public uint Size;
        public uint Usage;
        public uint ProcessId;
        public IntPtr DefaultHeapId;
        public uint ModuleId;
        public uint Threads;
        public uint ParentProcessId;
        public int BasePriority;
        public uint Flags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string ExecutableFile;
    }
}
