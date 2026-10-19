using IdealAkeWms.Models;

namespace IdealAkeWms.Services;

/// <summary>
/// BDE Stufe 1 (v1.63.0, Variante B): EINE Stelle fuer die Kuerzel-Familie einer Werkbank. Ein Arbeitsgang behaelt seine
/// bevorzugte Werkbank (<c>WorkOperation.ProductionWorkplaceId</c>); stempelbar ist er an jeder Werkbank mit demselben
/// <c>ArbeitsschrittCode</c>. Vergleich getrimmt + OrdinalIgnoreCase wie die Struktur-Erkennung, in C# ueber die geladenen
/// Werkbaenke (rund 90 Zeilen) statt in SQL (Fallstrick Kollation). Werkbank ohne Kuerzel oder nicht mehr in Sage
/// (<c>SageMissingSince</c>) = nur sie selbst — AKE-Werkbaenke tragen kein Kuerzel, dort aendert sich nichts.
/// </summary>
public static class WerkbankKuerzelFamilie
{
    private static string? Code(ProductionWorkplace w) =>
        string.IsNullOrWhiteSpace(w.ArbeitsschrittCode) ? null : w.ArbeitsschrittCode.Trim();

    private static bool Gleich(ProductionWorkplace w, string code) =>
        string.Equals(Code(w), code, StringComparison.OrdinalIgnoreCase);

    /// <summary>Ids der Werkbaenke, deren offene Arbeitsgaenge an <paramref name="w"/> angeboten und gebucht werden (FA-a1).
    /// Familienmitglieder duerfen gekennzeichnet oder BDE-inaktiv sein — so wird Altbestand an W1-Werkbaenken sichtbar.</summary>
    public static HashSet<int> FamilieIds(ProductionWorkplace w, IEnumerable<ProductionWorkplace> alle)
    {
        var ids = new HashSet<int> { w.Id };
        var code = Code(w);
        if (code == null || w.SageMissingSince != null) return ids;
        ids.UnionWith(alle.Where(x => Gleich(x, code)).Select(x => x.Id));
        return ids;
    }

    /// <summary>FA-Baum-Gate (FA-a7): BDE-aktive Werkbaenke plus jede, deren Kuerzel an einer BDE-aktiven, nicht
    /// gekennzeichneten Werkbank steht.</summary>
    public static HashSet<int> BdeRelevanteIds(IEnumerable<ProductionWorkplace> alle)
    {
        var liste = alle.ToList();
        var aktiveCodes = liste.Where(w => w.BdeAktiv && w.SageMissingSince == null)
            .Select(Code).OfType<string>().ToHashSet(StringComparer.OrdinalIgnoreCase);
        return liste.Where(w => w.BdeAktiv || (Code(w) is string c && aktiveCodes.Contains(c))).Select(w => w.Id).ToHashSet();
    }

    /// <summary>Bevorzugte Werkbank bei mehreren je Kuerzel (Antwort 4): BDE-aktive zuerst, dann kleinste Id.</summary>
    public static ProductionWorkplace Bevorzugt(IEnumerable<ProductionWorkplace> kandidaten) =>
        kandidaten.OrderByDescending(w => w.BdeAktiv).ThenBy(w => w.Id).First();

    /// <summary>Werkbank-Option „nur AG-Fertigmeldung" wirksam (FA-c2): globales <c>BdeNurFaMeldung</c> gewinnt.</summary>
    public static bool NurAgFertigmeldungWirksam(ProductionWorkplace w, bool nurFaGlobal) =>
        w.BdeAktiv && w.BdeNurAgFertigmeldung && !nurFaGlobal;
}
