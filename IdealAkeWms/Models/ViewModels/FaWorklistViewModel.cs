using IdealAkeWms.Data.Repositories;

namespace IdealAkeWms.Models.ViewModels;

/// <summary>
/// FA-Abarbeitungsliste je Arbeitsgang (v1.22.0): pro offenem FA mit aktivem
/// FaWorkStep des gewaehlten Arbeitsgangs eine Zeile — ueber ALLE Werkbaenke.
/// Spalten: Werkbank (Info), Merkmal-Spalten des AGs, EINE Erledigt-Checkbox
/// (der gewaehlte AG, AJAX-Toggle <c>/api/fa-work-steps/toggle-completed</c>).
/// </summary>
public class FaWorklistViewModel
{
    public int? SelectedWorkStepId { get; set; }
    public List<WorkStep> AvailableWorkSteps { get; set; } = new();
    public WorkStep? SelectedWorkStep { get; set; }                            // Header der Erledigt-Spalte
    public int? SelectedWorkplaceId { get; set; }                              // Zusatzfilter Werkbank (NULL = alle)
    public List<ProductionWorkplace> AvailableWorkplaces { get; set; } = new();
    public List<FaAttributeDefinition> AttributeColumns { get; set; } = new(); // Merkmale des gewaehlten AG
    public bool ShowDone { get; set; }
    public List<FaWorklistRow> Items { get; set; } = new();
    public Dictionary<string, List<EnaioDmsDocumentLink>> EnaioDmsLinks { get; set; } = new();
    public PaginationState Pagination { get; set; } = new();
}

public class FaWorklistRow
{
    public int ProductionOrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string? ArticleNumber { get; set; }
    public decimal Quantity { get; set; }
    public string? WorkplaceName { get; set; }                  // Werkbank als Info-Spalte
    public DateTime? VorkommissionierTermin { get; set; }  // BG-Termin
    public DateTime? KommissionierTermin { get; set; }
    public DateTime? ProductionDate { get; set; }
    public Dictionary<int, string?> AttributeValues { get; set; } = new();   // DefinitionId -> Anzeigetext
    public FaWorklistCell? WorkStepCell { get; set; }                        // genau EIN AG (der gewaehlte)
}

public class FaWorklistCell
{
    public int FaWorkStepId { get; set; }
    public bool IsCompleted { get; set; }
}
