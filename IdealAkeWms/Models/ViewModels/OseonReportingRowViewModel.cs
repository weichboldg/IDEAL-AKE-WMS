namespace IdealAkeWms.Models.ViewModels;

public record OseonReportingRowViewModel(
    string CustomerOrderNumber,
    string FaNumber,
    string PositionNumber,
    string OperationName,
    string? WorkplaceName,
    DateTime CalculatedDueDate,
    int OseonStatus,
    string StatusText,
    string StatusBadgeClass,
    OseonReportingSlice Slice,
    bool IsDoneToday);
