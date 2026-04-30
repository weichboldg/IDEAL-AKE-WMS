namespace IdealAkeWms.Models.ViewModels;

public record OseonReportingKpiViewModel(
    int OverdueCount,
    int TodayPlannedCount,
    int TodayDoneCount,
    int FutureCount);
