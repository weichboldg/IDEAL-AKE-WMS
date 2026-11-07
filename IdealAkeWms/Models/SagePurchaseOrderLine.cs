namespace IdealAkeWms.Models;

/// <summary>
/// Offene Sage-Bestellposition (Cache), v1.84.0 Spec 2026-11-08-wareneingang-aus-sage-bestellungen E1. Quelle SData
/// <c>EinkaufPositionenInfo</c> (liefert nur offene Positionen), nur Belegkennzeichen <c>EBB</c> (Entscheidung E0-Blocker B-1).
/// Kein <see cref="AuditableEntity"/>: der Abgleich ersetzt den Cache je Lauf vollstaendig; Nachvollziehbarkeit ueber <see cref="SyncedAt"/>
/// und das Aktivitaets-Protokoll. Verweise (<see cref="ArticleId"/>, <see cref="CreditorId"/>, <see cref="StorageLocationId"/>) sind beim Lauf
/// aufgeloeste Werte OHNE FK. Mengen in der Basiseinheit (<c>PositionGGBestellt</c>/<c>PositionGGGeliefert</c> = Vorgangsposition, bei EBB
/// 1:1 zur Belegposition; E0 B-2).
/// </summary>
public class SagePurchaseOrderLine
{
    public int Id { get; set; }
    public int BelPosId { get; set; }
    public int BelId { get; set; }
    public string Belegkennzeichen { get; set; } = string.Empty;
    /// <summary><c>BelegnummerFormatiert</c>, z. B. <c>2027-168975</c> (Belegjahr-Belegnummer).</summary>
    public string Belegnummer { get; set; } = string.Empty;
    public string? LieferantKto { get; set; }
    public string? LieferantName { get; set; }
    /// <summary>Sage <c>Liefersperre</c> = <c>KHKKontokorrent.IstGesperrt</c> des Lieferanten (E0 B-3), keine Sperre der Bestellung.</summary>
    public bool LieferantGesperrt { get; set; }
    public int? CreditorId { get; set; }
    public string Artikelnummer { get; set; } = string.Empty;
    public int? ArticleId { get; set; }
    public string? Bezeichnung1 { get; set; }
    public string? Bezeichnung2 { get; set; }
    /// <summary>Bestellt laut Sage (<c>PositionGGBestellt</c>), Basiseinheit.</summary>
    public decimal MengeBestellt { get; set; }
    /// <summary>Geliefert laut Sage (<c>PositionGGGeliefert</c>), Basiseinheit.</summary>
    public decimal MengeGeliefert { get; set; }
    /// <summary>Offen laut Sage = bestellt − geliefert (Basiseinheit), beim Lauf berechnet und gespeichert (Spaltenfilter).</summary>
    public decimal MengeOffen { get; set; }
    public string? Mengeneinheit { get; set; }
    public string? MengeBasiseinheit { get; set; }
    public string? Lagermengeneinheit { get; set; }
    /// <summary>E0 B-2 (+ v1.86.0): Bestell- oder Basiseinheit ≠ Lagermengeneinheit → Position sichtbar markiert und nicht buchbar, bis die
    /// Einheitenfrage geklaert ist (gebucht wird in der Basiseinheit).</summary>
    public bool EinheitAbweichend { get; set; }
    public int? SagePlatzId { get; set; }
    public string? SagePlatzKurzbezeichnung { get; set; }
    public int? StorageLocationId { get; set; }
    /// <summary>Liefertermin der Position, ersatzweise des Belegs (Datum).</summary>
    public DateTime? Liefertermin { get; set; }
    public bool Chargenpflicht { get; set; }
    public bool Nachweispflicht { get; set; }
    public DateTime SyncedAt { get; set; }
}
