namespace IdealAkeWms.Models.ViewModels;

public class OseonReportingFilter
{
    public int? WorkplaceId { get; set; }
    public List<string> OperationNames { get; set; } = new();
    public string? CustomerOrderNumber { get; set; }
    public string? FaNumber { get; set; }
    public int? HorizonDaysOverride { get; set; }
    public OseonReportingSlice Slice { get; set; } = OseonReportingSlice.Today;
}
