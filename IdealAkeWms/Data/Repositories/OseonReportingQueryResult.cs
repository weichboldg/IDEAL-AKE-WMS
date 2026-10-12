using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public record OseonReportingQueryRow(
    OseonWorkOperation WorkOperation,
    OseonProductionOrder Order,
    OperationConfig? Config);

public record OseonReportingQueryResult(
    List<OseonReportingQueryRow> Rows,
    int OperationsWithoutConfigCount,
    DateTime? DataAsOf);
