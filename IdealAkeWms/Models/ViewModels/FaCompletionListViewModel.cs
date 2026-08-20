using IdealAkeWms.Data.Repositories;

namespace IdealAkeWms.Models.ViewModels;

/// <summary>
/// FaCompletion/Index.cshtml — Phase 4 FA-Vervollstaendigung-Liste.
/// Slim FA-overview with completion-summary (z.B. "3/5 Gruppen vervollstaendigt").
/// </summary>
public class FaCompletionListViewModel
{
    public List<FaCompletionListItem> Items { get; set; } = new();
    public string? FilterOrderNumber { get; set; }
    public string? FilterArticleNumber { get; set; }
    public string? FilterCustomer { get; set; }
    public bool ShowDone { get; set; }

    /// <summary>enaio-DMS-Links je FA-Nummer (Bulk-Lookup, fuer die einheitlichen FA-Vorbau-Buttons).</summary>
    public Dictionary<string, List<EnaioDmsDocumentLink>> EnaioDmsLinks { get; set; } = new();

    public PaginationState Pagination { get; set; } = new();

    // IDEAL FA-Liste-Hierarchie (Teil 7/8): nur bei Master ProduktionsauftragHierarchisch = true.
    /// <summary>Hierarchischer Modus aktiv → Zwei-Ebenen-Gruppierung (Groups) statt Items.</summary>
    public bool Hierarchical { get; set; }
    /// <summary>HauptFA-Gruppen (Gruppen-Kopfzeile = OrderNumber, darunter alle Nachfahren-Sub-FAs).</summary>
    public List<FaCompletionListGroup> Groups { get; set; } = new();
    /// <summary>Anzahl aller Sub-FA-Zeilen auf der Filterauswahl (Z4: „N Aufträge · M Sub-FAs").</summary>
    public int HierarchicalRowCount { get; set; }
}

/// <summary>Eine HauptFA-Gruppe der FA-Vervollstaendigung-Liste im hierarchischen Modus.</summary>
public class FaCompletionListGroup
{
    public string OrderNumber { get; set; } = string.Empty;
    public List<FaCompletionListItem> Items { get; set; } = new();
}

/// <summary>
/// Kontext fuer das Zeilen-Partial <c>_FaCompletionRow.cshtml</c> — traegt die Zeile plus den
/// gemeinsamen View-Kontext, damit flache und gruppierte Darstellung dasselbe Zeilen-Markup teilen.
/// </summary>
public class FaCompletionRowContext
{
    public FaCompletionListItem Item { get; set; } = new();
    public Dictionary<string, List<EnaioDmsDocumentLink>> EnaioDmsLinks { get; set; } = new();
    /// <summary>true → order-number-Zelle zeigt SubOrderNumber, Elternspalte + SageMissingSince-Badge aktiv.</summary>
    public bool Hierarchical { get; set; }
}

public class FaCompletionListItem
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? Customer { get; set; }
    public string? ArticleNumber { get; set; }
    public string? Description1 { get; set; }
    public DateTime? ProductionDate { get; set; }
    public bool IsDone { get; set; }

    /// <summary>Anzahl der aktiven FaWorkSteps (IsRemoved = false) dieses FAs.</summary>
    public int ApplicableCount { get; set; }

    /// <summary>Anzahl der aktiven FaWorkSteps mit IsSpecComplete = true ("vollstaendig definiert").</summary>
    public int CompletedCount { get; set; }

    /// <summary>Summe aller Spec-Eintraege ueber alle aktiven FaWorkSteps dieses FAs.</summary>
    public int SpecCount { get; set; }

    /// <summary>True wenn dem FA keine Werkbank zugewiesen ist (ProductionWorkplaceId == null).</summary>
    public bool HasNoWorkplace { get; set; }

    /// <summary>Name der zugewiesenen Werkbank (null wenn keine zugewiesen ist).</summary>
    public string? WorkplaceName { get; set; }

    // IDEAL Hierarchie (Teil 7) — nur im hierarchischen Modus befuellt.
    /// <summary>Sub-FA-Nummer (= Sage BelID). Im Flachmodus == OrderNumber (Backfill).</summary>
    public string? SubOrderNumber { get; set; }
    /// <summary>Elternzeiger (Sub-FA des Vaters); Spalte parent-sub-order-number, defaultHidden.</summary>
    public string? ParentSubOrderNumber { get; set; }
    /// <summary>Zeitstempel „aus der Struktur vermisst" (Sync-Regel 2); eigener Badge, kein IsCancelled.</summary>
    public DateTime? SageMissingSince { get; set; }
}
