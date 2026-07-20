using System.Collections.Generic;
using System.Linq;

namespace IDEALAKEWMSService.Services;

/// <summary>
/// Reine Auswertung der BOM-Cache-Abdeckung fuers Aktivitaets-Protokoll: Zusatz-Zaehler +
/// optionale Warn-Texte. Keine DB/IO -> unit-testbar (der raw-SQL-Teil des
/// BomCacheSyncService bleibt Manual-UAT).
/// </summary>
public static class BomCacheCoverage
{
    /// <summary>Max. Anzahl Artikelnummern in der "ohne BOM-Daten"-Liste.</summary>
    public const int NoBomListCap = 30;

    public static BomCacheCoverageResult Build(
        int totalEligibleFas, int cachedFas, int cap,
        IReadOnlyList<string> articlesWithoutBom)
    {
        var counts = new Dictionary<string, int>
        {
            ["fa im fenster"] = totalEligibleFas,
            ["fa gecacht"] = cachedFas,
            ["artikel ohne bom"] = articlesWithoutBom.Count,
        };

        string? capWarning = null;
        if (totalEligibleFas > cap)
        {
            var diff = totalEligibleFas - cachedFas;
            capWarning =
                $"Cap erreicht: {cachedFas} von {totalEligibleFas} offenen FAs gecacht (Cap {cap}) — " +
                $"{diff} FAs ohne Cache-Eintrag, werden NICHT automatisch erkannt.";
        }

        string? noBomWarning = null;
        if (articlesWithoutBom.Count > 0)
        {
            var shown = articlesWithoutBom.Take(NoBomListCap).ToList();
            var extra = articlesWithoutBom.Count - shown.Count;
            noBomWarning = $"Artikel ohne BOM-Daten (SAGE+OSEON leer): {string.Join(", ", shown)}"
                + (extra > 0 ? $" … (+{extra} weitere)" : "");
        }

        return new BomCacheCoverageResult(counts, capWarning, noBomWarning);
    }
}

public sealed record BomCacheCoverageResult(
    IReadOnlyDictionary<string, int> Counts,
    string? CapWarning,
    string? NoBomWarning);
