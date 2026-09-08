using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;

namespace IdealAkeWms.Services;

/// <summary>
/// Einzige Stelle, die die BOM-Menge auf die Auftragsmenge hochrechnet (Spec Design C).
/// AKE-Cache/-View fuehren <c>Menge</c> je Stueck → × <c>order.Quantity</c>;
/// die FA-Hierarchie liefert <c>Sollmenge</c> bereits als Auftragsgesamtmenge → unveraendert.
/// Das Ergebnis beschreibt sich selbst (<see cref="BomQueryResult.MengeIstAuftragsmenge"/>),
/// keine Master-Abfrage in Verbrauchern.
/// </summary>
public static class BomQuantityResolver
{
    public static decimal Resolve(BomQueryResult result, BomItem item, ProductionOrder order) =>
        result.MengeIstAuftragsmenge ? item.Menge : item.Menge * order.Quantity;
}
