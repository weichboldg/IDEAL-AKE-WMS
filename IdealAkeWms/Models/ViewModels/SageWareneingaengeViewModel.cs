namespace IdealAkeWms.Models.ViewModels;

/// <summary>v1.99.4 (Wareneingang Teil 2, E3/F4): Liste „Wareneingänge an Sage“ (Listen-View-Pattern, ADR 0005).</summary>
public class SageWareneingaengeViewModel
{
    public List<SageWareneingangZeile> Zeilen { get; set; } = new();
    public SageWareneingangStatus? Status { get; set; }
    public string? Bestellung { get; set; }
    public DateTime? Von { get; set; }
    public DateTime? Bis { get; set; }
    public bool CanErneutSenden { get; set; }
    public PaginationState Pagination { get; set; } = new();
}

public class SageWareneingangZeile
{
    public required PurchaseOrderReceipt Receipt { get; init; }
    /// <summary>Memo am Sage-Beleg der Gruppe (WE#&lt;erste Receipt-Id&gt;#), fuer die Pruefung in Sage vor „Erneut senden“.</summary>
    public string? Memo { get; init; }
}
