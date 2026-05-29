using IdealAkeWms.Models;

namespace IdealAkeWms.Models.ViewModels;

public class MissingPartsListViewModel
{
    public List<MissingPartRow> Items { get; set; } = new();
    public List<ProductionWorkplace> AvailableWorkplaces { get; set; } = new();
    public int? WorkplaceFilter { get; set; }
    public bool MineOnly { get; set; }
    public PaginationState Pagination { get; set; } = new();

    /// <summary>Welcher Tab ist gerade aktiv.</summary>
    public ShortageStatus ActiveTab { get; set; } = ShortageStatus.WillBeRestocked;

    /// <summary>Total-Count des WillBeRestocked-Tab (fuer Tab-Header-Badge).</summary>
    public int WaitingTotalCount { get; set; }

    /// <summary>Total-Count des NoRestock-Tab (fuer Tab-Header-Badge).</summary>
    public int NoRestockTotalCount { get; set; }
}
