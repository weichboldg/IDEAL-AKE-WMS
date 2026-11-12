namespace IdealAkeWms.Models.ViewModels;

/// <summary>v1.99.4 (Wareneingang Teil 2, E3/F4): Liste „Sage-Wareneingänge“ (bis v1.99.6 „Wareneingänge an Sage“; Listen-View-Pattern, ADR 0005).</summary>
public class SageWareneingaengeViewModel
{
    public List<SageWareneingangZeile> Zeilen { get; set; } = new();
    public SageWareneingangStatus? Status { get; set; }
    public string? Bestellung { get; set; }
    public DateTime? Von { get; set; }
    public DateTime? Bis { get; set; }
    public bool CanErneutSenden { get; set; }
    /// <summary>v1.99.7 Q-2: Hinweis bei ausgeschaltetem Sage-Wareneingang mit liegengebliebenen Einträgen; null = keiner.</summary>
    public string? AusHinweis { get; set; }
    public PaginationState Pagination { get; set; } = new();
}

public class SageWareneingangZeile
{
    public required PurchaseOrderReceipt Receipt { get; init; }
    /// <summary>Memo am Sage-Beleg der Gruppe (WE#&lt;erste Receipt-Id&gt;#), fuer die Pruefung in Sage vor „Erneut senden“.</summary>
    public string? Memo { get; init; }
}
