using IdealAkeWms.Data.Repositories;

namespace IdealAkeWms.Models.ViewModels;

/// <summary>
/// FA-Abarbeitungsliste je Werkbank (v1.22.0): pro offenem FA eine Zeile mit
/// Erledigt-Checkboxen je gemapptem Arbeitsgang der Werkbank plus Merkmal-Spalten.
/// </summary>
public class FaWorklistViewModel
{
    public int? SelectedWorkplaceId { get; set; }
    public List<ProductionWorkplace> AvailableWorkplaces { get; set; } = new();
    public List<WorkStep> MappedWorkSteps { get; set; } = new();          // Spalten dieser Werkbank
    public List<FaAttributeDefinition> AttributeColumns { get; set; } = new(); // aktive Defs der gemappten AGs
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
    public DateTime? VorkommissionierTermin { get; set; }  // BG-Termin
    public DateTime? KommissionierTermin { get; set; }
    public DateTime? ProductionDate { get; set; }
    public Dictionary<int, string?> AttributeValues { get; set; } = new();        // DefinitionId -> Anzeigetext
    public Dictionary<int, FaWorklistCell> WorkStepCells { get; set; } = new();   // WorkStepId -> Zelle
    public int OrphanWorkStepCount { get; set; }           // offene AGs ausserhalb des Mappings
}

public class FaWorklistCell
{
    public int FaWorkStepId { get; set; }
    public bool IsCompleted { get; set; }
}
