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
}
