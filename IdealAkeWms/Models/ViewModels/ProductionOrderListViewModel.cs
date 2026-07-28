namespace IdealAkeWms.Models.ViewModels;

public class ProductionOrderListViewModel
{
    public List<ProductionOrderListItem> Items { get; set; } = new();
    public string? FilterOrderNumber { get; set; }
    public string? FilterArticleNumber { get; set; }
    public string? FilterCustomer { get; set; }
    public bool ShowDone { get; set; }
    public int KommissionierTage { get; set; }
    public int VorkommissionierTage { get; set; }
    public int BeschichtungTage { get; set; }
    public bool CanPick { get; set; }

    /// <summary>vorbau-Zugriff (read-only Stueckliste-Button in der FA-Liste, v1.25.0).</summary>
    public bool HasVorbauAccess { get; set; }

    /// <summary>enaio DMS-Links pro FA-Nummer (Key=OrderNumber, Value=Liste von DMS-Dokumenten)</summary>
    public Dictionary<string, List<Data.Repositories.EnaioDmsDocumentLink>> EnaioDmsLinks { get; set; } = new();

    public PaginationState Pagination { get; set; } = new();
}

public class ProductionOrderListItem
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? Customer { get; set; }
    public string? ArticleNumber { get; set; }
    public string? Description1 { get; set; }
    public string? Description2 { get; set; }
    public DateTime? ProductionDate { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public bool IsDone { get; set; }
    public bool IsCancelled { get; set; }
    public string? WorkplaceName { get; set; }

    /// <summary>
    /// Werkbank-Override „Abweichende Vorkommissioniertage" (v1.27.0) — nur gesetzt, wenn fuer
    /// diese Zeile tatsaechlich ein Override statt des globalen Werts griff (UI-Rueckmeldung).
    /// </summary>
    public int? PrePickingDaysOverride { get; set; }

    // FA-Zusatzinfos aus Sage (v1.26.0) — read-only, Default-ausgeblendete Spalten.
    public string? Kaeltemittel { get; set; }
    public string? Ventil { get; set; }
    public string? AusfuehrungEZ { get; set; }
    public string? Maschine { get; set; }
    public string? SageStatus { get; set; }

    // Calculated dates
    public DateTime? KommissionierTermin { get; set; }
    public DateTime? VorkommissionierTermin { get; set; }
    public DateTime? BeschichtungTermin { get; set; }

    // Cross-cutting from PickingStatus (siehe Spec 6.1) — fuer Beschichtungstermin-Logik
    public bool HasCoatingParts { get; set; }
    public bool IsCoatingDone { get; set; }
}
