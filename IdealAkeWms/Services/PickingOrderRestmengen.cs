using IdealAkeWms.Models;

namespace IdealAkeWms.Services;

/// <summary>
/// v1.88.0 (Spec 2026-11-08-kommissionierung-auftraege-kopf-zeilen, E2): offene Restmengen je Stuecklistenschluessel. Abgedeckt = Summe ueber
/// die Zeilen aller NICHT stornierten Auftraege des FA: Umgebucht → IstMenge; Offen/Gepickt/Entfallen → SollMenge; Uebertragen → 0 (steht im
/// Folgeauftrag). Rest = Soll − Abgedeckt, nur &gt; 0. DB-frei, unit-testbar; Grundlage fuer idempotentes Erzeugen/Nachziehen.
/// </summary>
public static class PickingOrderRestmengen
{
    public sealed record Soll(string ArticleNumber, string? BomPosition, decimal Menge, bool IsBaugruppe, string? Kommissionierziel);

    public sealed record Rest(Soll Position, decimal Menge);

    public static (string, string) Schluessel(string articleNumber, string? bomPosition) =>
        (articleNumber.Trim().ToUpperInvariant(), (bomPosition ?? "").Trim());

    public static decimal Abgedeckt(PickingOrderLine z) => z.Status switch
    {
        // „Vollstaendig“ abgeschlossen: der Kommissionierer hat entschieden, dass der Rest nicht gebraucht wird (wie „Entfallen“) –
        // sonst legte das Nachziehen eine bewusst kleinere Ist-Menge als neuen Rest an.
        PickingOrderLineStatus.Umgebucht when z.PickingOrder?.Status == PickingOrderStatus.Abgeschlossen => z.SollMenge,
        PickingOrderLineStatus.Umgebucht => z.IstMenge ?? 0m,
        PickingOrderLineStatus.Uebertragen => 0m,
        _ => z.SollMenge,
    };

    /// <param name="vorhandeneZeilen">Zeilen aller nicht stornierten Auftraege des FA.</param>
    public static List<Rest> Berechne(IEnumerable<Soll> soll, IEnumerable<PickingOrderLine> vorhandeneZeilen)
    {
        var abgedeckt = vorhandeneZeilen
            .GroupBy(z => Schluessel(z.ArticleNumber, z.BomPosition))
            .ToDictionary(g => g.Key, g => g.Sum(Abgedeckt));
        // Doppelte Schluessel in der Stueckliste (gleiche Ressource + Position) zusammenfassen, sonst zaehlte die Abdeckung doppelt.
        return soll
            .GroupBy(s => Schluessel(s.ArticleNumber, s.BomPosition))
            .Select(g => g.First() with { Menge = g.Sum(s => s.Menge) })
            .Select(s => new Rest(s, s.Menge - abgedeckt.GetValueOrDefault(Schluessel(s.ArticleNumber, s.BomPosition))))
            .Where(r => r.Menge > 0m)
            .ToList();
    }

    /// <summary>v1.99.70 (Spec 2026-11-25-kommissionierauftraege-nachtrag-nachts F8): Stueckliste kleiner als beauftragt.</summary>
    public sealed record Ueberdeckung(string ArticleNumber, string? BomPosition, decimal Soll, decimal Beauftragt);

    public const int TextMaxLaenge = 1000;

    /// <summary>
    /// v1.99.70 (F8, eine Regel): Schluessel mit mindestens einer offenen Zeile (Offen/Gepickt) und beauftragter Menge
    /// (<see cref="Abgedeckt"/>) ueber dem aktuellen Soll (entfallene Position = Soll 0). Ohne offene Zeile ist nichts mehr zu tun – keine
    /// Warnung. Nichts wird gekuerzt (Antwort 2).
    /// </summary>
    /// <param name="vorhandeneZeilen">Zeilen aller nicht stornierten Auftraege des FA.</param>
    public static List<Ueberdeckung> Ueberdeckt(IEnumerable<Soll> soll, IEnumerable<PickingOrderLine> vorhandeneZeilen)
    {
        var sollJe = soll
            .GroupBy(s => Schluessel(s.ArticleNumber, s.BomPosition))
            .ToDictionary(g => g.Key, g => g.Sum(s => s.Menge));
        return vorhandeneZeilen
            .GroupBy(z => Schluessel(z.ArticleNumber, z.BomPosition))
            .Where(g => g.Any(z => z.Status is PickingOrderLineStatus.Offen or PickingOrderLineStatus.Gepickt))
            .Select(g => new Ueberdeckung(g.First().ArticleNumber.Trim(), g.First().BomPosition, sollJe.GetValueOrDefault(g.Key), g.Sum(Abgedeckt)))
            .Where(u => u.Beauftragt > u.Soll)
            .OrderBy(u => u.ArticleNumber, StringComparer.OrdinalIgnoreCase).ThenBy(u => u.BomPosition, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>v1.99.70 (F8): ein Text fuer Leitstand-Tooltip, Marker und Nachtlauf-Warnung; null = keine Ueberdeckung; hoechstens
    /// <see cref="TextMaxLaenge"/> Zeichen (Spalte NVARCHAR(1000)).</summary>
    public static string? Text(IReadOnlyList<Ueberdeckung> ueberdeckt)
    {
        if (ueberdeckt.Count == 0) return null;
        var text = string.Join("; ", ueberdeckt.Select(u => $"{u.ArticleNumber} Pos {u.BomPosition}: Stückliste {u.Soll:0.###}, beauftragt {u.Beauftragt:0.###}"));
        return text.Length <= TextMaxLaenge ? text : text[..(TextMaxLaenge - 1)] + "…";
    }
}
