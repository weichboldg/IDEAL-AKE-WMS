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
    /// <summary>v1.99.4 (F6): Lieferschein-Nr. des Lieferanten (Kopf, einmal je Absenden); bleibt bei Fehlern stehen.</summary>
    public string? Lieferschein { get; set; }
    /// <summary>v1.99.4 (F6): AppSetting WareneingangLieferscheinPflicht.</summary>
    public bool LieferscheinPflicht { get; set; }
    /// <summary>v1.99.4 (F2): Sage-WE fuer diese Maske - Status beim Buchen und Platzregel. Seit v1.99.15
    /// <c>SageWareneingang.GiltFuer</c> (Schalter SageWareneingangBelegAktiv und Belegart).</summary>
    public bool SageWeAktiv { get; set; }
    /// <summary>v1.99.15: ServiceSetting SageWareneingangBelegAktiv selbst (unterscheidet „Schalter aus“ von „Abruf ohne Sage-WE“).</summary>
    public bool SageWeSchalterAn { get; set; }
    /// <summary>v1.99.15 (Teil 3, F3): Rahmenabruf (EBZ) - Kopf-Badge und Hinweise.</summary>
    public bool Rahmenabruf { get; set; }
    /// <summary>v1.99.15 (F4): Schalter an, aber Rahmenabrufe laufen ohne Sage-WE (<c>SageWareneingang.RahmenabrufMitSageWe</c> = false).</summary>
    public bool AbrufOhneSageWe => Rahmenabruf && SageWeSchalterAn && !SageWeAktiv;
    /// <summary>v1.99.11 (Wareneingang-CSV F1): Barcode des Lieferschein-Aufklebers (Kopf, einmal je Absenden); bleibt bei Fehlern stehen.</summary>
    public string? Barcode { get; set; }
    /// <summary>v1.99.11: AppSetting WareneingangBarcodeAktiv - Feld sichtbar.</summary>
    public bool BarcodeAktiv { get; set; }

    /// <summary>Sage-Feld EKLieferschein(20) (sage-sdata-masterdata).</summary>
    public const int LieferscheinMaxLaenge = 20;
    /// <summary>v1.99.6: Positionsmemo (Notiz der Bewegung hat 500; 200 laesst Platz fuer die Kennung im Sage-Artikelmemo).</summary>
    public const int MemoMaxLaenge = 200;
}

public class PurchaseOrderReceiveRow
{
    public required PurchaseOrderLineRow Row { get; init; }
    /// <summary>Rest = offen laut Sage − im WMS erfasst (Freigabe-Antwort 2), nie negativ.</summary>
    public decimal Rest => IdealAkeWms.Data.Repositories.ErfasstJeBelPos.RestAus(Row.Line.MengeOffen, Row.ImWmsAbzuziehen);   // v1.99.4 F5; v1.99.10 eine Regel mit der Lieferverzug-Mail
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
    /// <summary>v1.99.6 (Freigabe-Nachtrag 2026-11-12): Positionsmemo, gespeichert als Notiz der Bewegung; im Sage-Wareneingang vor der Kennung.</summary>
    public string? Memo { get; set; }
}
