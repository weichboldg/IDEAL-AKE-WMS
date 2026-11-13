namespace IdealAkeWms.Models.ViewModels;

/// <summary>v1.84.0 (Spec 2026-11-08-wareneingang-aus-sage-bestellungen F2): Vorschau „Offene Bestellungen“.</summary>
public class PurchaseOrderListViewModel
{
    public List<PurchaseOrderLineRow> Items { get; set; } = new();
    public PaginationState Pagination { get; set; } = new();
    public int Wochen { get; set; }
    public string? Lieferant { get; set; }
    public string? Bestellung { get; set; }
    public string? Artikel { get; set; }
    public string? Lagerplatz { get; set; }
    public bool NurUeberfaellig { get; set; }
    /// <summary>v1.99.15 (Teil 3, F3): Filter „auch im WMS vollständig erfasste Abrufe“ (Standard aus).</summary>
    public bool AuchErfassteAbrufe { get; set; }
    /// <summary>v1.99.15: Anzahl ausgeblendeter Rahmenabrufe mit Rest 0 (Meldung über der Tabelle).</summary>
    public int AusgeblendeteAbrufe { get; set; }
    public DateTime HorizontBis { get; set; }
    /// <summary>Letzter Abgleich (max SyncedAt); null = Cache leer (Hinweis mit Dienst-Schalter).</summary>
    public DateTime? Stand { get; set; }
    public bool CanReceive { get; set; }
    /// <summary>Lieferanten-Auswahl (Kto, Anzeigetext) aus dem Cache.</summary>
    public List<(string Kto, string Text)> Lieferanten { get; set; } = new();
}

/// <summary>Eine Position der Vorschau bzw. der Wareneingangs-Maske (Cache-Zeile + aufgeloeste WMS-Anzeige).</summary>
public class PurchaseOrderLineRow
{
    public required SagePurchaseOrderLine Line { get; init; }
    public string? LieferantText { get; init; }
    public string? ZielLagerplatz { get; init; }
    /// <summary>Sage-Platz ohne WMS-Lagerplatz → Anzeige Sage-Kurzbezeichnung + „nicht im WMS“.</summary>
    public bool ZielNichtImWms { get; init; }
    /// <summary>Im WMS zu dieser Position bereits erfasst (Summe Wareneingaenge, ab E3).</summary>
    public decimal ImWmsErfasst { get; init; }
    /// <summary>v1.99.4 (F5): davon noch NICHT im Sage-Stand des Abgleichs enthalten (Rest-Regel, ErfasstJeBelPos.Abzuziehen).</summary>
    public decimal ImWmsAbzuziehen { get; init; }
    /// <summary>v1.99.4 (E3): „Sage-Wareneingang: n× Status“ der Receipts dieser Position; null = keiner (Maske Wareneingang).</summary>
    public string? SageWeText { get; init; }
    public bool Ueberfaellig { get; init; }
    public bool OhneTermin => Line.Liefertermin == null;
    /// <summary>v1.99.15: Rahmenabruf mit Rest 0 (Rest-Regel <c>ErfasstJeBelPos</c>, dieselbe wie in der Maske).</summary>
    public bool AbrufVollstaendigErfasst => Line.IstRahmenabruf
        && IdealAkeWms.Data.Repositories.ErfasstJeBelPos.RestAus(Line.MengeOffen, ImWmsAbzuziehen) == 0m;
}
