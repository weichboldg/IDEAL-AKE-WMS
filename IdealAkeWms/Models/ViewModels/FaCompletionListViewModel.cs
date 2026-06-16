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

    /// <summary>Anzahl der aktiven FaWorkSteps mit IsCompleted = true.</summary>
    public int CompletedCount { get; set; }

    /// <summary>Summe aller Spec-Eintraege ueber alle aktiven FaWorkSteps dieses FAs.</summary>
    public int SpecCount { get; set; }

    /// <summary>True wenn dem FA keine Werkbank zugewiesen ist (ProductionWorkplaceId == null).</summary>
    public bool HasNoWorkplace { get; set; }

    /// <summary>Name der zugewiesenen Werkbank (null wenn keine zugewiesen ist).</summary>
    public string? WorkplaceName { get; set; }
}
