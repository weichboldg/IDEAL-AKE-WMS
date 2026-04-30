using IdealAkeWms.Models;

namespace IdealAkeWms.Models.ViewModels;

public class OseonReportingViewModel
{
    public OseonReportingKpiViewModel Kpis { get; set; } = new(0, 0, 0, 0);
    public List<OseonReportingRowViewModel> Rows { get; set; } = new();
    public List<OseonReportingDayGroup> FutureDayGroups { get; set; } = new();
    public int OperationsWithoutConfigCount { get; set; }
    public DateTime? DataAsOf { get; set; }
    public OseonReportingFilter Filter { get; set; } = new();
    public List<string> AvailableOperationNames { get; set; } = new();
    public List<ProductionWorkplace> AvailableWorkplaces { get; set; } = new();
    public int HorizonDaysEffective { get; set; } = 10;
}
