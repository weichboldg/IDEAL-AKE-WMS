namespace IdealAkeWms.Models.ViewModels;

/// <summary>
/// v1.88.0 (Spec 2026-11-08-kommissionierung-auftraege-kopf-zeilen, E2): <c>/Picking</c> listet Kommissionierauftraege statt FAs. Die
/// fruehere FA-Liste (inkl. IDEAL-Gruppierung nach HauptFA) entfaellt – Auftraege gibt es nur am HauptFA.
/// </summary>
public class PickingListViewModel
{
    public List<PickingOrderListItem> Items { get; set; } = new();
    public bool PickerAssignmentEnabled { get; set; }
    /// <summary>„Nur meine“ aus (nur bei Zuweisung relevant).</summary>
    public bool ShowAllOrders { get; set; }
    /// <summary>Statusfilter: „offen“ (Offen + In Arbeit, Standard) oder „alle“.</summary>
    public string StatusFilter { get; set; } = "offen";
    public int? BereichId { get; set; }
    public List<PickingArea> Bereiche { get; set; } = new();
    public PaginationState Pagination { get; set; } = new();
}

/// <summary>v1.88.0: eine Zeile der Auftragsliste.</summary>
public class PickingOrderListItem
{
    public int Id { get; set; }
    public string Nummer { get; set; } = string.Empty;
    public int? ProductionOrderId { get; set; }
    public string? OrderNumber { get; set; }
    public string? ArticleNumber { get; set; }
    public string? Description1 { get; set; }
    public string? WorkplaceName { get; set; }
    public string Bereich { get; set; } = string.Empty;
    public int? PickingPriority { get; set; }
    public DateTime? Faelligkeit { get; set; }
    public PickingOrderStatus Status { get; set; }
    public string? AssignedPickerName { get; set; }

    /// <summary>QA Q9: Zeilenstatus als deutscher Text (Druck, Anzeige).</summary>
    public static string ZeilenStatusText(PickingOrderLineStatus s) => s switch
    {
        PickingOrderLineStatus.Offen => "offen",
        PickingOrderLineStatus.Gepickt => "gepickt",
        PickingOrderLineStatus.Umgebucht => "umgebucht",
        PickingOrderLineStatus.Entfallen => "entfallen",
        PickingOrderLineStatus.Uebertragen => "in Folgeauftrag",
        _ => s.ToString(),
    };

    public static string StatusText(PickingOrderStatus s) => s switch
    {
        PickingOrderStatus.Offen => "Offen",
        PickingOrderStatus.InArbeit => "In Arbeit",
        PickingOrderStatus.TeilweiseAbgeschlossen => "Teilweise abgeschlossen",
        PickingOrderStatus.Abgeschlossen => "Abgeschlossen",
        PickingOrderStatus.Storniert => "Storniert",
        _ => s.ToString(),
    };
}
