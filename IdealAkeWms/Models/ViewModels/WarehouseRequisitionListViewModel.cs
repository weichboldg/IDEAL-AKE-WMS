using IdealAkeWms.Models;

namespace IdealAkeWms.Models.ViewModels;

public class WarehouseRequisitionListViewModel
{
    public List<WarehouseRequisitionListItemViewModel> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public WarehouseRequisitionStatus? StatusFilter { get; set; }
    public int? WorkplaceFilter { get; set; }
    public List<ProductionWorkplace> AvailableWorkplaces { get; set; } = new();
    public int OpenCount { get; set; }       // KPI fuer Lager-Sicht
    public PaginationState Pagination { get; set; } = new();
    public int MissingPartsWaitingItemCount { get; set; }
    public int MissingPartsWaitingRequisitionCount { get; set; }
    public int MissingPartsNoRestockItemCount { get; set; }
    public int MissingPartsNoRestockRequisitionCount { get; set; }
}
