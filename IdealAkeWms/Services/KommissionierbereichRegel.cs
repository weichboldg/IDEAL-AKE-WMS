using IdealAkeWms.Models;

namespace IdealAkeWms.Services;

/// <summary>
/// v1.88.0 (Spec 2026-11-08-kommissionierung-auftraege-kopf-zeilen, E1): Bereich einer Stuecklistenzeile. Rangfolge (Antwort 1):
/// Artikel → Kommissionierziel → Lagerort → Artikelgruppe → Standardbereich. Regel auf inaktivem Bereich → Standardbereich + Meldung
/// (melden statt still behandeln). DB-frei, unit-testbar.
/// </summary>
public static class KommissionierbereichRegel
{
    public sealed record Zeile(string ArticleNumber, string? Kommissionierziel, string? Lagerort, string? Artikelgruppe);

    public sealed record Ergebnis(PickingArea Bereich, string? Meldung);

    private static readonly PickingAreaRuleType[] Rangfolge =
    {
        PickingAreaRuleType.Artikel, PickingAreaRuleType.Kommissionierziel, PickingAreaRuleType.Lagerort, PickingAreaRuleType.Artikelgruppe,
    };

    public static string Anzeige(PickingAreaRuleType t) => t switch
    {
        PickingAreaRuleType.Artikel => "Artikel",
        PickingAreaRuleType.Kommissionierziel => "Kommissionierziel",
        PickingAreaRuleType.Lagerort => "Lagerort",
        PickingAreaRuleType.Artikelgruppe => "Artikelgruppe",
        _ => t.ToString(),
    };

    /// <summary>Normalform eines Regelwerts (getrimmt, Vergleich ohne Gross/Klein).</summary>
    public static string Norm(string? v) => (v ?? "").Trim().ToUpperInvariant();

    /// <param name="regeln">Alle Regeln mit geladenem <see cref="PickingAreaRule.PickingArea"/>.</param>
    public static Ergebnis Resolve(Zeile z, IReadOnlyCollection<PickingAreaRule> regeln, PickingArea standard)
    {
        foreach (var typ in Rangfolge)
        {
            var wert = typ switch
            {
                PickingAreaRuleType.Artikel => z.ArticleNumber,
                PickingAreaRuleType.Kommissionierziel => z.Kommissionierziel,
                PickingAreaRuleType.Lagerort => z.Lagerort,
                _ => z.Artikelgruppe,
            };
            if (string.IsNullOrWhiteSpace(wert)) continue;
            var n = Norm(wert);
            var regel = regeln.FirstOrDefault(r => r.RuleType == typ && Norm(r.Value) == n);
            if (regel == null) continue;
            return regel.PickingArea.IsActive
                ? new Ergebnis(regel.PickingArea, null)
                : new Ergebnis(standard, $"Regel {Anzeige(typ)} „{wert.Trim()}“ zeigt auf den inaktiven Bereich {regel.PickingArea.Name} – " +
                                         $"Standardbereich {standard.Name} verwendet.");
        }
        return new Ergebnis(standard, null);
    }
}
