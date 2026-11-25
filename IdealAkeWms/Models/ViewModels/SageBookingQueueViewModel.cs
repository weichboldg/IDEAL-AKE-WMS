using IdealAkeWms.Services;

namespace IdealAkeWms.Models.ViewModels;

/// <summary>v1.99.66 (Spec 2026-11-23-sage-buchungen-gesamtsicht): Seite „Sage-Buchungen“ - Lagerbuchungs-Queue und Sage-Wareneingänge in einer
/// Liste (bis v1.99.65 „Sage-Lagerbuchungen“ und die eigene Liste „Sage-Wareneingänge“). Listen-View-Pattern, ADR 0005.</summary>
public class SageBookingQueueViewModel
{
    public List<SageBuchungZeile> Zeilen { get; set; } = new();
    public PaginationState Pagination { get; set; } = new();

    // Filterkarte (F8)
    public SageBuchungHerkunft? Herkunft { get; set; }
    public SageBuchungAnzeige? Status { get; set; }
    public bool NurOffene { get; set; }
    public string? Bestellung { get; set; }
    /// <summary>Wirksamer Wert (ohne Eingabe heute − 30 Tage).</summary>
    public DateTime Von { get; set; }
    public DateTime? Bis { get; set; }
    /// <summary>F8: offene Zeilen außerhalb des Zeitraums, die trotzdem angezeigt werden (Hinweis „melden statt still“).</summary>
    public int OffeneAusserhalb { get; set; }

    // Rechte (F7)
    /// <summary>Lagerbuchungs-Zeilen (stock_read-Kreis).</summary>
    public bool ZeigtLagerbuchungen { get; set; }
    /// <summary>WE-Zeilen, -Spalten, -Filter und -Hinweise (stock_read + WareneingangBestellungAktiv, oder Einkauf).</summary>
    public bool ZeigtWareneingaenge { get; set; }
    /// <summary>„Erneut senden“ an Lagerbuchungen (CanTransferStock).</summary>
    public bool CanRequeue { get; set; }
    /// <summary>WE-Aktionen (CanTransferStock + WareneingangBestellungAktiv, sonst 404 am PurchaseOrdersController).</summary>
    public bool CanWeAktionen { get; set; }
    /// <summary>Knopf „Wareneingang zu Bestellung“.</summary>
    public bool CanReceive { get; set; }

    // Hinweise (F9)
    /// <summary>TLS-Zertifikatspruefung des Sage-Clients ist deaktiviert (Warnhinweis anzeigen).</summary>
    public bool SslCheckDisabled { get; set; }
    /// <summary>v1.99.7 Q-2: Hinweis bei ausgeschaltetem Sage-Wareneingang mit liegengebliebenen Einträgen; null = keiner.</summary>
    public string? AusHinweis { get; set; }
    /// <summary>v1.99.11 (Wareneingang-CSV F4): Hinweise „Sage-Wareneingang deaktiviert – CSV-Übergabe wartet“ bzw. „kein Pfad“; leer = keine.</summary>
    public List<string> CsvHinweise { get; set; } = new();
}

/// <summary>v1.99.4/v1.99.11: eine Position eines Sage-Wareneingangs mit Memo-Kennung des Vorgangs und CSV-Stand (seit v1.99.66 Teil der
/// Seite „Sage-Buchungen“, vorher <c>SageWareneingaengeViewModel</c>).</summary>
public class SageWareneingangZeile
{
    public required PurchaseOrderReceipt Receipt { get; init; }
    /// <summary>Memo am Sage-Beleg der Gruppe (WE#&lt;erste Receipt-Id&gt;#), fuer die Pruefung in Sage vor „Erneut senden“.</summary>
    public string? Memo { get; init; }
    /// <summary>v1.99.11 (F4): Text der Spalte „CSV“ (<c>WareneingangCsv.StatusText</c>).</summary>
    public string Csv { get; init; } = "–";
    /// <summary>v1.99.11: Ausgangszeile steht auf Fehler - Knopf „CSV erneut schreiben“.</summary>
    public bool CsvFehler { get; init; }
}
