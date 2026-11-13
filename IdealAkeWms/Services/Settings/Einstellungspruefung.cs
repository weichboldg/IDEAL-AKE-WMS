using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services.SyncLogger;

namespace IdealAkeWms.Services.Settings;

/// <summary>
/// Spec 2026-11-07-einstellungen-auswahlfelder-und-beschreibung F7/E4b (AK 18, Rueckfrage 1 = ja): prueft beim Web-Start einmal alle
/// vorhandenen AppSettings- und ServiceSettings-Zeilen gegen die Kataloge. Lesestellen bleiben unveraendert — ungueltige Werte werden nur
/// gemeldet (je Wert eine Warnzeile im Lauf „Einstellungspruefung“), ohne ungueltige Werte entsteht kein Lauf.
/// </summary>
public static class Einstellungspruefung
{
    /// <summary>Befunde (Key, Rohwert, erlaubte Werte, wirksamer Wert) — rein, testbar.</summary>
    /// <param name="stammdaten">Schluessel je Stammdaten-Quelle (Existenzpruefung MasterData); fehlt eine Quelle, wird sie nicht geprueft.</param>
    public static List<string> Befunde(
        IEnumerable<(string Key, string? Value)> appSettings,
        IEnumerable<(string Key, string? Value)> serviceSettings,
        IReadOnlyDictionary<SettingStammdaten, IReadOnlyList<string>> stammdaten)
    {
        var befunde = new List<string>();
        void Pruefe(IReadOnlyList<SettingDefinition> katalog, IEnumerable<(string Key, string? Value)> zeilen, bool exakt, string bereich)
        {
            foreach (var (key, value) in zeilen)
            {
                var d = katalog.FirstOrDefault(x => x.Key == key);
                if (d == null || value == null || d.Type == SettingType.Geheim) continue; // v1.99.14: Geheimnis nie protokollieren
                var optionen = d.Type == SettingType.MasterData && stammdaten.TryGetValue(d.Stammdaten, out var s)
                    ? s.Select(x => (x, x)).ToList() : null;
                var item = SettingItem.Aus(d, value, optionen, exakt);
                if (item.Ungueltig)
                    befunde.Add($"{bereich} {key}: {item.Fehler} Wirksam: „{item.Wirksam}“.");
            }
        }
        Pruefe(AppSettingDefinitions.All, appSettings, exakt: false, "Einstellungen (/Settings)");
        Pruefe(ServiceSettingDefinitions.All, serviceSettings, exakt: true, "Service-Einstellungen (/ServiceSettings)");
        return befunde;
    }

    /// <summary>Schreibt die Befunde in einen Lauf <see cref="SyncLogServices.Einstellungspruefung"/>; ohne Befund kein Lauf.</summary>
    public static async Task ProtokollierenAsync(ISyncLogger logger, IReadOnlyList<string> befunde, CancellationToken ct = default)
    {
        if (befunde.Count == 0) return;
        await using var run = await logger.BeginRunAsync(SyncLogServices.Einstellungspruefung, ct);
        foreach (var b in befunde)
            await run.LogWarningAsync(b.Length <= 1000 ? b : b[..1000], ct: ct); // SyncLogs.Message = 1000
        await run.FinishSuccessAsync(new Dictionary<string, int> { ["ungueltig"] = befunde.Count }, ct: ct);
    }
}
