namespace IdealAkeWms.Models.ViewModels;

public record OseonReportingDayGroup(DateTime Date, int Count, List<OseonReportingRowViewModel> Rows);
