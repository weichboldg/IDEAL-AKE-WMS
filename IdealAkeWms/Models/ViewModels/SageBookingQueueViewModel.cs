namespace IdealAkeWms.Models.ViewModels;

public class SageBookingQueueViewModel
{
    public List<SageBookingQueueItem> Items { get; set; } = new();
    public SageBookingQueueStatus? FilterStatus { get; set; }
    public PaginationState Pagination { get; set; } = new();
}
