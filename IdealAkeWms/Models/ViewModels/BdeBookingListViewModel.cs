namespace IdealAkeWms.Models.ViewModels;

public class BdeBookingListViewModel
{
    public int Id { get; set; }
    public string OperatorName { get; set; } = "";
    public string WorkplaceName { get; set; } = "";
    public string BookingType { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string? Target { get; set; }
    public decimal TotalGood { get; set; }
    public decimal TotalScrap { get; set; }
    public bool IsCancelled { get; set; }
}
