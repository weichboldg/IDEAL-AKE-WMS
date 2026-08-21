namespace IdealAkeWms.Models.ViewModels;

public class PickingListViewModel
{
    public List<PickingListItem> Items { get; set; } = new();
    public bool ShowAllOrders { get; set; }
    public bool PickerAssignmentEnabled { get; set; }
    public PaginationState Pagination { get; set; } = new();

    // IDEAL FA-Liste-Hierarchie (Teil 7/8): nur bei Master ProduktionsauftragHierarchisch = true.
    /// <summary>Hierarchischer Modus aktiv → Zwei-Ebenen-Gruppierung (Groups) statt Items.</summary>
    public bool Hierarchical { get; set; }
    /// <summary>
    /// HauptFA-Gruppen. Gruppen-Reihenfolge = dringlichste (kleinste Prio) zuerst, innerhalb der Gruppe
    /// die Prio-Reihenfolge der Kommissionier-Warteschlange (bewusst NICHT SubOrderNumber wie sonst Z2 —
    /// die Kommissionier-Liste ist eine Prioritaets-Warteschlange).
    /// </summary>
    public List<PickingListGroup> Groups { get; set; } = new();
    /// <summary>Anzahl aller Sub-FA-Zeilen auf der Filterauswahl (Z4: „N Aufträge · M Sub-FAs").</summary>
    public int HierarchicalRowCount { get; set; }
}

/// <summary>Eine HauptFA-Gruppe der Kommissionier-Liste im hierarchischen Modus.</summary>
public class PickingListGroup
{
    public string OrderNumber { get; set; } = string.Empty;
    public List<PickingListItem> Items { get; set; } = new();
}

/// <summary>
/// Kontext fuer das Zeilen-Partial <c>_PickingRow.cshtml</c> — traegt die Zeile plus den gemeinsamen
/// View-Kontext, damit flache und gruppierte Darstellung dasselbe Zeilen-Markup teilen.
/// </summary>
public class PickingRowContext
{
    public PickingListItem Item { get; set; } = new();
    public bool PickerAssignmentEnabled { get; set; }
    /// <summary>true → order-number-Zelle zeigt SubOrderNumber, Elternspalte + SageMissingSince-Badge aktiv.</summary>
    public bool Hierarchical { get; set; }
}

public class PickingListItem
{
    public int Id { get; set; }
    public int? PickingPriority { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string? ArticleNumber { get; set; }
    public string? Description1 { get; set; }
    public string? Customer { get; set; }
    public decimal Quantity { get; set; }
    public DateTime? ProductionDate { get; set; }
    public DateTime? KommissionierTermin { get; set; }
    public string? PickingStatus { get; set; }
    public int? AssignedPickerId { get; set; }
    public string? AssignedPickerName { get; set; }

    // IDEAL Hierarchie (Teil 7) — nur im hierarchischen Modus befuellt.
    /// <summary>Sub-FA-Nummer (= Sage BelID). Im Flachmodus == OrderNumber (Backfill).</summary>
    public string? SubOrderNumber { get; set; }
    /// <summary>Elternzeiger (Sub-FA des Vaters); Spalte parent-sub-order-number, defaultHidden.</summary>
    public string? ParentSubOrderNumber { get; set; }
    /// <summary>Zeitstempel „aus der Struktur vermisst" (Sync-Regel 2); eigener Badge, kein IsCancelled.</summary>
    public DateTime? SageMissingSince { get; set; }
}
