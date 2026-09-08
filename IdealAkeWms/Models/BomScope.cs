namespace IdealAkeWms.Models;

/// <summary>Umfang einer hierarchischen Stuecklisten-Abfrage (BOM-Bridge, Spec Design D).</summary>
public enum BomScope
{
    /// <summary>Direkte Kinder des Sub-FA (Standard, AKE-Aequivalent; Partition fuer Aggregate).</summary>
    DirectChildren,
    /// <summary>Alle Nachfahren ueber alle Ebenen, jeder Knoten einmal, flach mit rekursivem
    /// Positions-Pfad. NUR Anzeige-Scope des Picking-Workflows, nie fuer Aggregate.</summary>
    FullStructure
}

public static class BomScopes
{
    /// <summary>
    /// Fachliche Anforderung 6: Auftrag ist HauptFA (<c>OrderNumber == SubOrderNumber</c>) → komplette
    /// Struktur; Sub-FA → nur eigene Stueckliste. Im Flachmodus gilt die Backfill-Invariante
    /// <c>SubOrderNumber == OrderNumber</c>; die flache Implementierung ignoriert den Scope ohnehin.
    /// </summary>
    public static BomScope ForOrder(ProductionOrder order) =>
        string.Equals(order.SubOrderNumber, order.OrderNumber, StringComparison.Ordinal)
            ? BomScope.FullStructure
            : BomScope.DirectChildren;
}
