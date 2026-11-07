namespace IdealAkeWms.Models;

/// <summary>Art eines Wareneingangs zu einer Bestellposition (Rest = offen laut Sage − im WMS erfasst, Freigabe-Antwort 2).</summary>
public enum PurchaseOrderReceiptLieferart : byte
{
    Vollstaendig = 1,
    Teillieferung = 2,
    Ueberlieferung = 3,
}

/// <summary>
/// v1.84.0 (Spec 2026-11-08-wareneingang-aus-sage-bestellungen E3): ein Wareneingang zu einer Sage-Bestellposition – genau eine
/// <see cref="StockMovement"/> (Einbuchung, FK Restrict, UNIQUE), gespeichert im selben SaveChanges (Navigation an der Bewegung).
/// <b>Snapshot</b> statt FK auf den Cache: die Cache-Zeile verschwindet, sobald Sage die Position schliesst. Andockpunkt der Qualitaetspruefung
/// (F4) ist <see cref="Id"/>. Mengen in der Basiseinheit.
/// </summary>
public class PurchaseOrderReceipt : AuditableEntity
{
    public int StockMovementId { get; set; }
    public StockMovement StockMovement { get; set; } = null!;
    public int BelId { get; set; }
    public int BelPosId { get; set; }
    public string Belegnummer { get; set; } = string.Empty;
    public string? LieferantKto { get; set; }
    public decimal Quantity { get; set; }
    /// <summary>Offen laut Sage beim Buchen (Basiseinheit), Nachvollziehbarkeit der Teil-/Ueberlieferung.</summary>
    public decimal? OffenLautSageBeimBuchen { get; set; }
    /// <summary>Sage-Platz der Bestellposition (Vorschlag), unabhaengig vom tatsaechlich gewaehlten WMS-Lagerplatz.</summary>
    public int? SagePlatzId { get; set; }
    public PurchaseOrderReceiptLieferart Lieferart { get; set; }
}
