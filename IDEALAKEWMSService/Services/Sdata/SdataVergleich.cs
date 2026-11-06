using System.Globalization;

namespace IDEALAKEWMSService.Services.Sdata;

/// <summary>Eine Abweichung: Feld <c>(fehlt in SData)</c>/<c>(fehlt in View)</c>/<c>(Anzahl Zeilen)</c> oder ein verglichenes Feld.</summary>
public sealed record SdataAbweichung(string Schluessel, string Feld, string? ViewWert, string? SDataWert);

public sealed record SdataVergleichErgebnis(int SchluesselView, int SchluesselSData, int AbweichendeSchluessel, IReadOnlyList<SdataAbweichung> Beispiele);

/// <summary>
/// Spec 2026-11-06-sage-sdata-stammdaten-sync (E0, F1): reine Diff-Funktion fuer den Vergleichsmodus — je Schluessel
/// (case-insensitiv wie SQL Server) die Felder beider Quellen vergleichen; Anzahl abweichender Schluessel + die ersten
/// <see cref="MaxBeispiele"/> Abweichungen (nach Schluessel sortiert, deterministisch).
/// </summary>
public static class SdataVergleich
{
    public const int MaxBeispiele = 20;

    public sealed record Feld<T>(string Name, Func<T, object?> Wert);

    public static SdataVergleichErgebnis Vergleiche<T>(IEnumerable<T> view, IEnumerable<T> sdata, Func<T, string?> schluessel,
        IReadOnlyList<Feld<T>> felder, int maxBeispiele = MaxBeispiele)
    {
        var v = view.ToLookup(z => schluessel(z) ?? "", StringComparer.OrdinalIgnoreCase);
        var s = sdata.ToLookup(z => schluessel(z) ?? "", StringComparer.OrdinalIgnoreCase);
        var beispiele = new List<SdataAbweichung>();
        var abweichend = 0;

        foreach (var key in v.Select(g => g.Key).Union(s.Select(g => g.Key), StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase))
        {
            var vz = v[key].ToList();
            var sz = s[key].ToList();
            var diffs = new List<SdataAbweichung>();
            if (vz.Count == 0) diffs.Add(new(key, "(fehlt in View)", null, sz.Count.ToString(CultureInfo.InvariantCulture)));
            else if (sz.Count == 0) diffs.Add(new(key, "(fehlt in SData)", vz.Count.ToString(CultureInfo.InvariantCulture), null));
            else if (vz.Count != sz.Count) diffs.Add(new(key, "(Anzahl Zeilen)", vz.Count.ToString(CultureInfo.InvariantCulture), sz.Count.ToString(CultureInfo.InvariantCulture)));
            else
                foreach (var f in felder)
                {
                    var a = Text(f.Wert(vz[0]));
                    var b = Text(f.Wert(sz[0]));
                    if (!string.Equals(a, b, StringComparison.Ordinal)) diffs.Add(new(key, f.Name, a, b));
                }

            if (diffs.Count == 0) continue;
            abweichend++;
            beispiele.AddRange(diffs.Take(maxBeispiele - beispiele.Count));
        }

        return new SdataVergleichErgebnis(v.Count, s.Count, abweichend, beispiele);
    }

    /// <summary>Kulturunabhaengige Textform; Dezimalwerte ohne Nachkomma-Nullen (1,50 aus der View = 1,5 aus SData).</summary>
    internal static string? Text(object? wert) => wert switch
    {
        null => null,
        decimal d => d.ToString("G29", CultureInfo.InvariantCulture),
        DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
        bool b => b ? "true" : "false",
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => wert.ToString(),
    };
}
