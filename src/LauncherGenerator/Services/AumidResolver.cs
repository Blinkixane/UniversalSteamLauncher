using System.Diagnostics;

namespace LauncherGenerator.Services;

public class AumidResolver
{
    public string? Find(string appName)
    {
        ProcessStartInfo psi = new()
        {
            FileName = "powershell",
            Arguments = "-Command \"Get-StartApps | ConvertTo-Csv -NoTypeInformation\"",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using Process process = Process.Start(psi)!;
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        string[] lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (string line in lines.Skip(1))
        {
            string[] values = ParseCsvLine(line);
            if (values.Length < 2)
                continue;

            string name = values[0];
            string appId = values[1];

            if (name.Contains(appName, StringComparison.OrdinalIgnoreCase))
                return appId;
        }

        return null;
    }

    /// <summary>
    /// Parseur CSV minimal mais correct : respecte les champs entre guillemets
    /// (avec "" comme guillemet echappe) et les virgules a l'interieur.
    /// Le Split(',') naif utilise precedemment cassait des que le champ AppID
    /// contenait des caracteres qui forcaient PowerShell a le quoter, d'ou les
    /// guillemets residuels observes dans les launcher.json generes.
    /// </summary>
    private static string[] ParseCsvLine(string line)
    {
        line = line.TrimEnd('\r', '\n');
        var fields = new List<string>();
        var current = new System.Text.StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++; // guillemet echappe, on saute le second
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    fields.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
        }

        fields.Add(current.ToString());
        return fields.ToArray();
    }
}