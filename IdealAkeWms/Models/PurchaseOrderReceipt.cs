namespace IdealAkeWms.Models;

/// <summary>Art eines Wareneingangs zu einer Bestellposition (Rest = offen laut Sage − im WMS erfasst, Freigabe-Antwort 2).</summary>
public enum PurchaseOrderReceiptLieferart : byte
{
    Vollstaendig = 1,
    Teillieferung = 2,
    Ueberlieferung = 3,
}

/// <summary>
/// v1.99.4 (Spec 2026-11-08-wareneingang-teil-2-sage-we-beleg-rahmenabrufe F4): Sage-Wareneingangsbeleg (ELL) je Wareneingang. Als int gespeichert.
/// <see cref="NichtNoetig"/> = Default/Altbestand = Verhalten Teil 1 (Lagerbuchungs-Queue). Jeder andere Wert schliesst die Bewegung aus der Queue aus (F3).
/// </summary>
public enum SageWareneingangStatus
{
    NichtNoetig = 0,
    Offen = 1,
    Gesendet = 2,
    Uebergeben = 3,
    Fehler = 4,
    NurInSage = 5,
    /// <summary>v1.99.7 E5: kein automatisches Senden - möglicher Beleg in Sage ohne passende Memo-Kennung (Sicherungsnetz) oder beim
    /// Ausschalten liegengeblieben (Q-2). Weiter nur „Erneut senden“ oder „als übergeben markieren“ von Hand.</summary>
    Klaeren = 6,
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

    // ---- v1.99.4 (Wareneingang Teil 2, E1/E2): Sage-Wareneingangsbeleg und Lieferschein ----
    /// <summary>Beim Buchen festgelegt; ein spaeteres Umschalten von SageWareneingangBelegAktiv aendert gebuchte Wareneingaenge nicht.</summary>
    public SageWareneingangStatus SageWeStatus { get; set; }
    /// <summary>Gruppenschluessel: alle Offen-Receipts EINES Absendens der Maske bilden EINEN ELL-Beleg (Freigabe-Antwort 2).</summary>
    public Guid? SageWeVorgang { get; set; }
    /// <summary>Zeitpunkt der erfolgreichen Uebergabe (Rest-Regel F5: Uebergeben mit SageWeAm &lt;= SyncedAt ist im Sage-Stand enthalten).</summary>
    public DateTime? SageWeAm { get; set; }
    public string? SageWeBelegnummer { get; set; }
    public string? SageWeFehler { get; set; }
    /// <summary>Anzahl Sende-Versuche (Status vor dem POST auf Gesendet + 1).</summary>
    public int SageWeVersuche { get; set; }
    /// <summary>v1.99.9: beim Senden verwendete Memo-Kennung dieses Receipts (<c>WE#[K#]&lt;Id&gt;#</c>, System-Kürzel); die Suche vor dem
    /// erneuten Senden nutzt sie, auch wenn das Kürzel inzwischen geändert wurde. NULL = nie gesendet bzw. vor v1.99.9 (altes Format).</summary>
    public string? SageWeKennung { get; set; }
    /// <summary>Lieferschein-Nr. des Lieferanten (F6), an jedem Receipt eines Absendens; an Sage nur im Sage-WE als EKLieferschein.</summary>
    public string? Lieferschein { get; set; }
}
