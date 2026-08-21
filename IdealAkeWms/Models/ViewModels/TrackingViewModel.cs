namespace IdealAkeWms.Models.ViewModels;

public class TrackingViewModel
{
    public List<TrackingOrderGroup> OrderGroups { get; set; } = new();
    public string? FilterOrderNumber { get; set; }
    public int? FilterWorkplaceId { get; set; }
    public bool ShowReported { get; set; }
    public List<ProductionWorkplace> AvailableWorkplaces { get; set; } = new();
    public bool CanReport { get; set; }
    public PaginationState Pagination { get; set; } = new();

    // IDEAL FA-Liste-Hierarchie (Teil 7/8): nur bei Master ProduktionsauftragHierarchisch = true.
    /// <summary>
    /// Hierarchischer Modus aktiv → 3-Ebenen-Baum (HauptFA → Sub-FA → Arbeitsgang) statt der flachen
    /// 2-Ebenen-Darstellung. Muster uebernommen von Tracking/OseonIndex (Chevron-Baum, zweistufiger
    /// Fortschritt). Der Flachmodus (AKE) bleibt bit-identisch (Bootstrap-collapse, OrderGroups).
    /// </summary>
    public bool Hierarchical { get; set; }
    /// <summary>HauptFA-Uebergruppen (je HauptFA-OrderNumber die Sub-FA-Gruppen mit ihren Arbeitsgaengen).</summary>
    public List<TrackingHauptFaGroup> HauptGroups { get; set; } = new();
    /// <summary>Anzahl aller Sub-FAs auf der Filterauswahl (Z4: „N Aufträge · M Sub-FAs").</summary>
    public int HierarchicalRowCount { get; set; }
}

/// <summary>
/// HauptFA-Uebergruppe im hierarchischen Modus. Fortschritt zweistufig: hier „fertige Sub-FAs / alle",
/// je Sub-FA (<see cref="TrackingOrderGroup"/>) „rueckgemeldete AGs / alle".
/// </summary>
public class TrackingHauptFaGroup
{
    public string OrderNumber { get; set; } = string.Empty;
    public List<TrackingOrderGroup> SubGroups { get; set; } = new();

    /// <summary>Gesamtzahl der Sub-FAs dieser HauptFA (Nenner des HauptFA-Fortschritts).</summary>
    public int TotalSubOrders => SubGroups.Count;
    /// <summary>Sub-FAs, deren Arbeitsgaenge alle rueckgemeldet sind (Zaehler des HauptFA-Fortschritts).</summary>
    public int FinishedSubOrders =>
        SubGroups.Count(s => s.TotalOperations > 0 && s.ReportedOperations == s.TotalOperations);
}

public class TrackingOrderGroup
{
    public int ProductionOrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string? ArticleNumber { get; set; }
    public string? Description1 { get; set; }
    public string? Customer { get; set; }
    public DateTime? ProductionDate { get; set; }
    public string? WorkplaceName { get; set; }
    public int TotalOperations { get; set; }
    public int ReportedOperations { get; set; }
    public List<TrackingOperationItem> Operations { get; set; } = new();

    // IDEAL Hierarchie (Teil 7) — nur im hierarchischen Modus befuellt.
    /// <summary>Sub-FA-Nummer (= Sage BelID). Im Flachmodus == OrderNumber (Backfill).</summary>
    public string? SubOrderNumber { get; set; }
    /// <summary>Elternzeiger (Sub-FA des Vaters).</summary>
    public string? ParentSubOrderNumber { get; set; }
    /// <summary>Zeitstempel „aus der Struktur vermisst" (Sync-Regel 2); eigener Badge, kein IsCancelled.</summary>
    public DateTime? SageMissingSince { get; set; }
}

public class TrackingOperationItem
{
    public int Id { get; set; }
    public string OperationNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Sequence { get; set; }
    public string? WorkplaceName { get; set; }
    public string? OrderNumber { get; set; }
    public bool IsReportable { get; set; }
    public bool IsExternalSystem { get; set; }
    public bool IsReported { get; set; }
    public DateTime? ReportedAt { get; set; }
    public string? ReportedBy { get; set; }
    public string? ExternalSource { get; set; }
}

public class TrackingByWorkplaceViewModel
{
    public ProductionWorkplace Workplace { get; set; } = null!;
    public List<TrackingOperationItem> Operations { get; set; } = new();
    public bool CanReport { get; set; }
    public string? FilterOrderNumber { get; set; }
    public bool ShowReported { get; set; }
}
