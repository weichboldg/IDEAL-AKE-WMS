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
    /// Fachliche Anforderung 6: Auftrag ist HauptFA → komplette Struktur; Sub-FA → nur eigene
    /// Stueckliste. EINE Sub-FA-Definition (Spec 2026-09-25-kommissionierung-nur-hauptfa): die Regel
    /// lebt in <see cref="ProductionOrder.IsHauptFa"/>, keine eigene Formel hier. Im Flachmodus gilt
    /// die Backfill-Invariante <c>SubOrderNumber == OrderNumber</c>; die flache Implementierung
    /// ignoriert den Scope ohnehin.
    /// </summary>
    public static BomScope ForOrder(ProductionOrder order) =>
        order.IsSubFa ? BomScope.DirectChildren : BomScope.FullStructure;
}
