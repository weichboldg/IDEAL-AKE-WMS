using IdealAkeWms.Models;

namespace IdealAkeWms.Models.ViewModels;

public class MissingPartsListViewModel
{
    public List<MissingPartRow> Items { get; set; } = new();
    public List<ProductionWorkplace> AvailableWorkplaces { get; set; } = new();
    public int? WorkplaceFilter { get; set; }
    public bool MineOnly { get; set; }
    public PaginationState Pagination { get; set; } = new();
}
