namespace IdealAkeWms.Models.ViewModels;

/// <summary>v1.84.0 (Spec 2026-11-08-wareneingang-aus-sage-bestellungen F3): Wareneingang zu einer Sage-Bestellung (eine BelId).</summary>
public class PurchaseOrderReceiveViewModel
{
    /// <summary>Eingabe/Scan der Belegnummer (Einstieg).</summary>
    public string? Belegnummer { get; set; }
    public int? BelId { get; set; }
    public string? LieferantText { get; set; }
    public bool LieferantGesperrt { get; set; }
    public DateTime? Liefertermin { get; set; }
    public List<PurchaseOrderReceiveRow> Zeilen { get; set; } = new();
    public List<StorageLocation> Lagerplaetze { get; set; } = new();
}

public class PurchaseOrderReceiveRow
{
    public required PurchaseOrderLineRow Row { get; init; }
    /// <summary>Rest = offen laut Sage − im WMS erfasst (Freigabe-Antwort 2), nie negativ.</summary>
    public decimal Rest => Math.Max(0m, Row.Line.MengeOffen - Row.ImWmsErfasst);
    /// <summary>Grund, warum die Zeile nicht buchbar ist (Text in der Zeile); null = buchbar.</summary>
    public string? NichtBuchbar => Row.Line.ArticleId == null ? "Artikel nicht im WMS"
        : Row.Line.EinheitAbweichend ? $"Einheit abweichend (Bestellung {Row.Line.Mengeneinheit}, Basis {Row.Line.MengeBasiseinheit}, Lager {Row.Line.Lagermengeneinheit}) – nicht buchbar, bis geklärt"
        : null;
    public PurchaseOrderReceiveInput Eingabe { get; set; } = new();
    public string? Fehler { get; set; }
}

/// <summary>Eingabe je Position (Post). Menge leer = Position nicht buchen. Menge als Text, kulturunabhaengig gelesen (Komma oder Punkt,
/// fallstricke „Die Anwendung hat keine feste Request-Kultur“ – sonst wird „2.5“ unter de-AT zu 25).</summary>
public class PurchaseOrderReceiveInput
{
    public int BelPosId { get; set; }
    public string? Menge { get; set; }
    public int? StorageLocationId { get; set; }
    public bool UeberlieferungBestaetigt { get; set; }
}
