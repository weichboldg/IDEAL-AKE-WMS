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

    /// <summary>Anzahl der AssemblyGroups mit IsApplicable = true (0..5).</summary>
    public int ApplicableCount { get; set; }

    /// <summary>Anzahl der AssemblyGroups mit IsCompleted = true (0..5).</summary>
    public int CompletedCount { get; set; }

    /// <summary>Summe aller Spec-Eintraege ueber alle 5 Gruppen dieses FAs.</summary>
    public int SpecCount { get; set; }
}
