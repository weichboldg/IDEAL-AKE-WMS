namespace IdealAkeWms.Models;

/// <summary>
/// Schluessel einer Stuecklisten-Abfrage (BOM-Bridge, Spec 2026-09-08).
/// AKE (flach) liest ausschliesslich <see cref="ArticleNumber"/>; die FA-Hierarchie (IDEAL) braucht
/// <see cref="SubOrderNumber"/> (WELCHER Fertigungsauftrag) und <see cref="OrderNumber"/> (HauptFA =
/// Struktur-Gruppe fuer den gecachten Lookup <c>GetByHauptFaAsync</c>). Die flache Implementierung
/// ignoriert die beiden Zusatzfelder vollstaendig — bit-identisches AKE-Verhalten.
/// </summary>
public sealed record BomKey(string ArticleNumber, string? SubOrderNumber = null, string? OrderNumber = null)
{
    public static BomKey ForOrder(ProductionOrder order) =>
        new(order.ArticleNumber ?? string.Empty, order.SubOrderNumber, order.OrderNumber);
}
