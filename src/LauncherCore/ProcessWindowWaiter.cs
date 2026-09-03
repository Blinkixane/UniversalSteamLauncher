using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace LauncherCore;

public static class ProcessWindowWaiter
{
    public static IReadOnlyList<Process> FindProcessTree(
        string executablePath,
        DateTime startedAfterUtc,
        ISet<int> trackedProcessIds)
    {
        string expectedName = Path.GetFileNameWithoutExtension(executablePath);
        string? expectedPath = TryGetFullPath(executablePath);

        Process[] processes = Process.GetProcesses();
        var processesById = processes.ToDictionary(process => process.Id);
        var parentById = ReadParentProcessIds();
        var treeIds = new HashSet<int>(trackedProcessIds);

        foreach (Process process in processes)
        {
            if (IsTargetProcess(process, expectedName, expectedPath)
                && WasStartedAfter(process, startedAfterUtc))
            {
                treeIds.Add(process.Id);
            }
        }

        bool added;
        do
        {
            added = false;
            foreach (Process process in processes)
            {
                if (treeIds.Contains(process.Id))
                    continue;

                if (parentById.TryGetValue(process.Id, out int parentId)
                    && treeIds.Contains(parentId))
                {
                    treeIds.Add(process.Id);
                    added = true;
                }
            }
        }
        while (added);

        trackedProcessIds.UnionWith(treeIds);

        return treeIds
            .Where(processesById.ContainsKey)
            .Select(id => processesById[id])
            .ToArray();
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
        if (!string.Equals(process.ProcessName, expectedName, StringComparison.OrdinalIgnoreCase))
            return false;

        if (expectedPath == null)
            return true;

        try
        {
            string? actualPath = process.MainModule?.FileName;
            if (actualPath == null)
                return true;

            return string.Equals(
                TryGetFullPath(actualPath),
                expectedPath,
                StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return true;
        }
    }

    private static bool WasStartedAfter(Process process, DateTime startedAfterUtc)
    {
        try
        {
            return process.StartTime.ToUniversalTime() >= startedAfterUtc;
        }
        catch
        {
            return true;
        }
    }

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
