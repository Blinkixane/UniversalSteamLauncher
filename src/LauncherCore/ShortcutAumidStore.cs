using ShellLink;
using ShellLink.Structures;
using PropertyStore.Structures;
using PropertyStore.Flags;

namespace LauncherCore;

/// <summary>
/// Lit et écrit l'AUMID stocké dans le PropertyStoreDataBlock d'un raccourci .lnk épinglé,
/// via securifybv.ShellLink. Extrait et nettoyé depuis AumidDiag.Program (ProgramFinal).
/// </summary>
public static class ShortcutAumidStore
{
    public static readonly Guid PKEY_AppUserModel_ID_FormatId = new("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3");
    public const uint PKEY_AppUserModel_ID_Pid = 5;

    /// <summary>
    /// Lecture seule, pensée pour être appelée au lancement (check ultra-léger) : renvoie
    /// l'AUMID actuellement stocké sur le .lnk, ou null si absent / fichier introuvable /
    /// illisible. Ne modifie jamais le fichier.
    /// </summary>
    public static string? ReadAumid(string lnkPath)
    {
        if (!File.Exists(lnkPath)) return null;

        Shortcut lnk;
        try { lnk = Shortcut.ReadFromFile(lnkPath); }
        catch { return null; }

        var storage = lnk.ExtraData?.PropertyStoreDataBlock?.PropertyStore
            .FirstOrDefault(s => s.FormatID == PKEY_AppUserModel_ID_FormatId);
        var entry = storage?.PropertyStorage.OfType<IntegerName>()
            .FirstOrDefault(e => e.ID == PKEY_AppUserModel_ID_Pid);
        return entry?.TypedPropertyValue.Value as string;
    }

    /// <summary>
    /// Crée un nouveau raccourci .lnk de A à Z (cible, arguments, icône, ET AUMID)
    /// en UNE SEULE écriture avec securifybv.ShellLink.
    ///
    /// À préférer systématiquement à "créer avec un autre outil COM puis réécrire
    /// l'AUMID à part" : deux bibliothèques différentes qui resérialisent le même
    /// .lnk l'une après l'autre peuvent perdre des données entre les deux passes
    /// (LinkTargetIDList, IconLocation, Arguments...). C'est ce qui causait le
    /// raccourci cassé (icône absente, non épinglable, clic sans effet) généré par
    /// l'ancien ShortcutCreator (COM brut) suivi d'un WriteAumid séparé.
    /// </summary>
    /// <param name="lnkPath">Chemin du .lnk à créer (écrasé s'il existe déjà).</param>
    /// <param name="targetPath">Exécutable cible.</param>
    /// <param name="arguments">Arguments de ligne de commande (ex: --game "Genshin").</param>
    /// <param name="iconPath">Fichier source de l'icône (.ico ou .exe). Null = utilise targetPath.</param>
    /// <param name="aumid">AUMID à appliquer. Null/vide = raccourci créé sans AUMID.</param>
    public static void CreateGameShortcut(string lnkPath, string targetPath, string arguments, string? iconPath, string? aumid)
    {
        string workingDirectory = Path.GetDirectoryName(targetPath) ?? string.Empty;
        string icon = string.IsNullOrWhiteSpace(iconPath) ? targetPath : iconPath;

        var lnk = Shortcut.CreateShortcut(targetPath, arguments, workingDirectory, icon, 0);

        if (!string.IsNullOrWhiteSpace(aumid))
        {
            lnk.ExtraData.PropertyStoreDataBlock = new PropertyStoreDataBlock
            {
                PropertyStore = new List<SerializedPropertyStorage> { BuildAumidStorage(aumid) }
            };
        }

        lnk.WriteToFile(lnkPath);
    }

    /// <summary>
    /// Écrit (ou remplace) l'AUMID sur un .lnk EXISTANT. Réservé aux cas où le
    /// raccourci existe déjà et a été créé ailleurs (ex: raccourci d'un jeu tiers
    /// à retagger). Pour un raccourci qu'on génère nous-mêmes, préférer
    /// CreateGameShortcut qui évite le risque de double-réécriture ci-dessus.
    /// Idempotent par défaut : si l'AUMID stocké est déjà correct, ne touche pas
    /// au fichier et renvoie false. Sauvegarde .bak avant toute écriture réelle,
    /// sauf si createBackup=false.
    /// </summary>
    /// <returns>true si le fichier a été réécrit, false si l'AUMID était déjà correct.</returns>
    public static bool WriteAumid(string lnkPath, string aumid, bool createBackup = true, bool forceRewrite = false)
    {
        if (!File.Exists(lnkPath))
            throw new FileNotFoundException("Raccourci introuvable.", lnkPath);

        if (!forceRewrite && ReadAumid(lnkPath) == aumid)
            return false;

        if (createBackup)
            File.Copy(lnkPath, lnkPath + ".bak", overwrite: true);

        var lnk = Shortcut.ReadFromFile(lnkPath);

        lnk.ExtraData.PropertyStoreDataBlock ??= new PropertyStoreDataBlock
        {
            PropertyStore = new List<SerializedPropertyStorage>()
        };

        var existing = lnk.ExtraData.PropertyStoreDataBlock.PropertyStore
            .FirstOrDefault(s => s.FormatID == PKEY_AppUserModel_ID_FormatId);
        if (existing != null)
            lnk.ExtraData.PropertyStoreDataBlock.PropertyStore.Remove(existing);

        lnk.ExtraData.PropertyStoreDataBlock.PropertyStore.Add(BuildAumidStorage(aumid));

        lnk.WriteToFile(lnkPath);
        return true;
    }

    private static SerializedPropertyStorage BuildAumidStorage(string aumid)
    {
        var tpv = new TypedPropertyValue(PropertyType.VT_LPWSTR, EncodeLpwstr(aumid));
        var integerName = new IntegerName(PKEY_AppUserModel_ID_Pid, tpv);
        return new SerializedPropertyStorage
        {
            FormatID = PKEY_AppUserModel_ID_FormatId,
            PropertyStorage = new List<SerializedPropertyValue> { integerName }
        };
    }

    /// <summary>
    /// Encodage VT_LPWSTR selon [MS-PROPSTORE] : compteur de caractères sur 4 octets
    /// (\0 final inclus), puis chaîne UTF-16LE avec \0 final, paddée à 4 octets.
    /// Validé par round-trip (écriture -> relecture -> comparaison) sur ce projet.
    /// </summary>
    private static byte[] EncodeLpwstr(string s)
    {
        var strBytes = System.Text.Encoding.Unicode.GetBytes(s + "\0");
        int cch = s.Length + 1;
        var buffer = new byte[4 + strBytes.Length];
        BitConverter.GetBytes((uint)cch).CopyTo(buffer, 0);
        strBytes.CopyTo(buffer, 4);

        int padding = (4 - (buffer.Length % 4)) % 4;
        if (padding == 0) return buffer;

        var padded = new byte[buffer.Length + padding];
        buffer.CopyTo(padded, 0);
        return padded;
    }
}
