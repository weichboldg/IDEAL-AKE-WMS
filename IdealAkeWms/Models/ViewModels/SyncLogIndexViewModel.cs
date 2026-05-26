using IdealAkeWms.Models;

namespace IdealAkeWms.Models.ViewModels;

public class SyncLogIndexViewModel
{
    public List<SyncLog> Entries { get; set; } = new();
    public string? FilterService { get; set; }
    public string? FilterLevel { get; set; }
    public string? FilterReference { get; set; }
    public List<string> AvailableServices { get; set; } = new();
    public PaginationState Pagination { get; set; } = new();
}
