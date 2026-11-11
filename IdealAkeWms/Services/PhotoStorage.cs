namespace IdealAkeWms.Services;

/// <summary>
/// v1.30.4 (Hotfix 2026-11-08-kommissionierfotos-ohne-anmeldung-abrufbar): Kommissionierfotos liegen ausserhalb von <c>wwwroot</c> unter
/// <c>ContentRoot/App_Data/Fotos/Kommissionierung</c> (nie statisch ausgeliefert; IIS sperrt <c>App_Data</c> zusaetzlich per hiddenSegments).
/// Ausgeliefert werden sie nur ueber <c>PhotoController.GetFile</c> mit Kommissionier-Recht – unter der bisherigen URL.
/// </summary>
public static class PhotoStorage
{
    public static string GetDirectory(IWebHostEnvironment env) =>
        Path.Combine(env.ContentRootPath, "App_Data", "Fotos", "Kommissionierung");

    /// <summary>Alter Ablageort bis v1.30.3 (frei abrufbar).</summary>
    public static string GetLegacyDirectory(IWebHostEnvironment env) =>
        Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "Fotos", "Kommissionierung");

    /// <summary>Gemeinsame Pfadpruefung fuer Loeschen und Ausliefern: nur ein reiner Dateiname mit Endung .jpg, der im Foto-Ordner liegt.</summary>
    public static bool TryResolve(string dir, string? fileName, out string fullPath)
    {
        fullPath = string.Empty;
        if (string.IsNullOrWhiteSpace(fileName) || fileName.Contains("..") || fileName.Contains('/') || fileName.Contains('\\')
            || Path.GetFileName(fileName) != fileName
            || !string.Equals(Path.GetExtension(fileName), ".jpg", StringComparison.OrdinalIgnoreCase))
            return false;
        var root = Path.GetFullPath(dir).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var full = Path.GetFullPath(Path.Combine(root, fileName));
        if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase)) return false;
        fullPath = full;
        return true;
    }

    /// <summary>Verschiebt Fotos vom alten Ort (wwwroot) in den geschuetzten Ordner; idempotent. Kollision: Quelle bleibt, Warnung. Ein Fehler je
    /// Datei wird geloggt und bricht den Start nicht ab. Rueckgabe (verschoben, uebersprungen).</summary>
    public static (int Verschoben, int Uebersprungen) MigrateLegacy(string legacyDir, string targetDir, ILogger logger)
    {
        if (!Directory.Exists(legacyDir)) return (0, 0);
        int verschoben = 0, uebersprungen = 0;
        foreach (var src in Directory.GetFiles(legacyDir))
        {
            var dst = Path.Combine(targetDir, Path.GetFileName(src));
            try
            {
                if (File.Exists(dst))
                {
                    uebersprungen++;
                    logger.LogWarning("Kommissionierfoto {Datei} nicht verschoben: im geschuetzten Ordner existiert bereits eine Datei gleichen Namens.",
                        Path.GetFileName(src));
                    continue;
                }
                File.Move(src, dst, overwrite: false);
                verschoben++;
            }
            catch (Exception ex)
            {
                uebersprungen++;
                logger.LogWarning(ex, "Kommissionierfoto {Datei} konnte nicht verschoben werden.", Path.GetFileName(src));
            }
        }
        if (verschoben + uebersprungen > 0)
            logger.LogInformation("Kommissionierfotos verschoben: {Verschoben}, uebersprungen: {Uebersprungen}.", verschoben, uebersprungen);
        return (verschoben, uebersprungen);
    }
}
