using System.Runtime.InteropServices;

namespace LauncherCore;

/// <summary>
/// Applique (ou relit) un AppUserModelID directement sur une fenêtre au runtime,
/// via SHGetPropertyStoreForWindow + PKEY_AppUserModel_ID.
/// Extrait et nettoyé depuis GenshinSteamLauncher.Program et AumidWatcher.Program
/// (code identique dans les deux, désormais centralisé ici).
/// </summary>
public static class AumidWindowTagger
{
    public static readonly Guid PKEY_AppUserModel_ID_FormatId = new("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3");
    public const int PKEY_AppUserModel_ID_Pid = 5;

    public enum TagResult
    {
        Success,
        WindowHandleInvalid,
        PropertyStoreUnavailable,
        Exception
    }

    /// <summary>
    /// Tente d'appliquer l'AUMID sur la fenêtre donnée. Ne lève jamais : retourne un TagResult
    /// et remplit errorDetail pour le logging appelant (voir LogPath dans GenshinSteamLauncher.Program).
    /// </summary>
    public static TagResult Apply(IntPtr hwnd, string aumid, out string? errorDetail)
    {
        errorDetail = null;

        if (hwnd == IntPtr.Zero)
        {
            errorDetail = "hwnd est IntPtr.Zero";
            return TagResult.WindowHandleInvalid;
        }

        Guid iid = typeof(IPropertyStore).GUID;
        int hr = SHGetPropertyStoreForWindow(hwnd, ref iid, out IPropertyStore store);
        if (hr != 0 || store == null)
        {
            errorDetail = $"SHGetPropertyStoreForWindow hr=0x{hr:X8}";
            return TagResult.PropertyStoreUnavailable;
        }

        var key = new PropertyKey(PKEY_AppUserModel_ID_FormatId, PKEY_AppUserModel_ID_Pid);
        var pv = new PropVariant { vt = 31, pointerValue = Marshal.StringToCoTaskMemUni(aumid) };
        try
        {
            store.SetValue(ref key, ref pv);
            store.Commit();
            return TagResult.Success;
        }
        catch (Exception ex)
        {
            errorDetail = $"{ex.GetType().Name} - {ex.Message}";
            return TagResult.Exception;
        }
        finally
        {
            PropVariantClear(ref pv);
        }
    }

    /// <summary>
    /// Relit l'AUMID actuellement appliqué sur la fenêtre. Utile pour diagnostic/tests,
    /// pas nécessaire dans le chemin critique du lancement.
    /// </summary>
    public static string? Read(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return null;

        Guid iid = typeof(IPropertyStore).GUID;
        int hr = SHGetPropertyStoreForWindow(hwnd, ref iid, out IPropertyStore store);
        if (hr != 0 || store == null) return null;

        var key = new PropertyKey(PKEY_AppUserModel_ID_FormatId, PKEY_AppUserModel_ID_Pid);
        store.GetValue(ref key, out PropVariant pv);
        return pv.GetString();
    }

    // ---- P/Invoke & COM plumbing ----

    [DllImport("shell32.dll")]
    private static extern int SHGetPropertyStoreForWindow(IntPtr hwnd, ref Guid riid, out IPropertyStore propertyStore);

    [DllImport("ole32.dll")]
    private static extern int PropVariantClear(ref PropVariant pvar);

    [ComImport]
    [Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPropertyStore
    {
        void GetCount(out uint cProps);
        void GetAt(uint iProp, out PropertyKey pkey);
        void GetValue(ref PropertyKey key, out PropVariant pv);
        void SetValue(ref PropertyKey key, ref PropVariant pv);
        void Commit();
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PropertyKey
    {
        public Guid fmtid;
        public int pid;
        public PropertyKey(Guid fmtid, int pid) { this.fmtid = fmtid; this.pid = pid; }
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct PropVariant
    {
        [FieldOffset(0)] public ushort vt;
        [FieldOffset(8)] public IntPtr pointerValue;
        public readonly string? GetString() => vt == 31 ? Marshal.PtrToStringUni(pointerValue) : null;
    }
}
