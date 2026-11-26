using System.Globalization;
using IdealAkeWms.Models;

namespace IdealAkeWms.Services;

/// <summary>
/// v1.99.89 (Spec 2026-11-26 Teil 3, K8): Verteil-Buchung einer Summenzeile der Maske „Liste“. Die Ist-Menge geht in Positionsreihenfolge
/// (natuerlich, Zeilen ohne Position zuletzt – H-1) auf die uebergebenen offenen Zeilen, jede bis zu ihrem Soll. Unterdeckung: die zuletzt
/// erreichte Zeile bekommt den Rest, nicht erreichte fehlen im Ergebnis (bleiben Offen). Ueberdeckung oder Ist ≤ 0: abgelehnt. DB-frei.
/// </summary>
public static class PickingOrderVerteilung
{
    private static readonly NaturalPositionComparer Natuerlich = new();

    public static List<(PickingOrderLine Zeile, decimal Menge)> Verteile(IEnumerable<PickingOrderLine> zeilen, decimal ist)
    {
        var liste = zeilen.OrderBy(z => string.IsNullOrEmpty(z.BomPosition)).ThenBy(z => z.BomPosition, Natuerlich).ToList();
        var offenSoll = liste.Sum(z => z.SollMenge);
        var de = CultureInfo.GetCultureInfo("de-DE");
        if (ist <= 0m)
            throw new InvalidOperationException($"Ist-Menge {ist.ToString("0.###", de)} muss größer als 0 sein.");
        if (ist > offenSoll)
            throw new InvalidOperationException($"Ist-Menge {ist.ToString("0.###", de)} größer als offenes Soll {offenSoll.ToString("0.###", de)}.");

        var ergebnis = new List<(PickingOrderLine, decimal)>();
        var rest = ist;
        foreach (var z in liste)
        {
            if (rest <= 0m) break;
            var menge = Math.Min(rest, z.SollMenge);
            if (menge > 0m) ergebnis.Add((z, menge));
            rest -= menge;
        }
        return ergebnis;
    }
}
