using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class OseonProductionOrderRepository : Repository<OseonProductionOrder>, IOseonProductionOrderRepository
{
    public OseonProductionOrderRepository(ApplicationDbContext context) : base(context) { }

    public async Task<List<OseonProductionOrder>> GetAllWithOperationsAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .Include(o => o.WorkOperations)
            .Include(o => o.ProductionWorkplace)
            .OrderBy(o => o.CustomerOrderNumber)
            .ThenBy(o => o.OseonOrderNumber)
            .ToListAsync();
    }

    public async Task<List<OseonProductionOrder>> GetByCustomerOrderNumberAsync(string customerOrderNumber)
    {
        return await _dbSet
            .Include(o => o.WorkOperations)
            .Include(o => o.ProductionWorkplace)
            .Where(o => o.CustomerOrderNumber == customerOrderNumber)
            .OrderBy(o => o.OseonOrderNumber)
            .ToListAsync();
    }

    public async Task<OseonProductionOrder?> GetByOseonIdAsync(long oseonId)
    {
        return await _dbSet.FirstOrDefaultAsync(o => o.OseonId == oseonId);
    }

    public async Task<OseonPagedResult> GetPagedAsync(string? searchTerm, string? workplaceName, bool showFinished, int page, int pageSize, HashSet<string>? relevantOperationNames = null, string? articleNumber = null)
    {
        // Basis-Query mit Such- und Werkbank-Filter (OHNE Status-Filter)
        var baseQuery = _dbSet.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            baseQuery = baseQuery.Where(o =>
                (o.CustomerOrderNumber != null && o.CustomerOrderNumber.Contains(term)) ||
                o.OseonOrderNumber.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(articleNumber))
        {
            var artTerm = articleNumber.Trim();
            baseQuery = baseQuery.Where(o => o.ArticleNumber != null
                && o.ArticleNumber.Contains(artTerm));
        }

        if (!string.IsNullOrWhiteSpace(workplaceName))
            baseQuery = baseQuery.Where(o => o.WorkplaceName != null && o.WorkplaceName == workplaceName);

        // Gruppen-Keys ermitteln:
        // showFinished=false → nur Gruppen die mindestens einen OFFENEN Auftrag haben
        // showFinished=true  → alle Gruppen
        IQueryable<OseonProductionOrder> groupKeyQuery;
        if (showFinished)
        {
            groupKeyQuery = baseQuery;
        }
        else if (relevantOperationNames != null)
        {
            // Mit Relevanz-Filter: Ein Auftrag gilt als "offen" wenn er mindestens einen
            // relevanten AG hat der noch nicht fertig ist (Status != 90 und != 95).
            // Auftraege OHNE relevante AGs gelten als "fertig".
            groupKeyQuery = baseQuery.Where(o =>
                o.WorkOperations.Any(op =>
                    relevantOperationNames.Contains(op.Name) &&
                    op.OseonStatus != 90 && op.OseonStatus != 95));
        }
        else
        {
            groupKeyQuery = baseQuery.Where(o => o.OseonStatus != 90 && o.OseonStatus != 95);
        }

        // CustomerOrderNumber als Gruppierungsschlüssel (Fallback OseonOrderNumber)
        var distinctKeysQuery = groupKeyQuery
            .Select(o => o.CustomerOrderNumber ?? o.OseonOrderNumber)
            .Distinct();

        var totalGroupCount = await distinctKeysQuery.CountAsync();

        var pagedGroupKeys = await distinctKeysQuery
            .OrderBy(k => k)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        if (pagedGroupKeys.Count == 0)
        {
            return new OseonPagedResult
            {
                Items = new List<OseonProductionOrder>(),
                TotalGroupCount = totalGroupCount,
                Page = page,
                PageSize = pageSize
            };
        }

        // ALLE Orders dieser Gruppen laden (inkl. fertige Sub-Aufträge!)
        // So sehen wir z.B. "1/14 fertig" korrekt und fertige Sub-Orders werden angezeigt
        var items = await _dbSet
            .AsNoTracking()
            .Include(o => o.WorkOperations)
            .Include(o => o.ProductionWorkplace)
            .Where(o => pagedGroupKeys.Contains(o.CustomerOrderNumber ?? o.OseonOrderNumber))
            .OrderBy(o => o.CustomerOrderNumber)
            .ThenBy(o => o.OseonOrderNumber)
            .ToListAsync();

        return new OseonPagedResult
        {
            Items = items,
            TotalGroupCount = totalGroupCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<OseonReportingQueryResult> GetRelevantOperationsForReportingAsync(
        int? workplaceId,
        IReadOnlyCollection<string>? operationNames,
        string? customerOrderNumber,
        string? faNumberPrefix,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken ct = default)
    {
        // Aktive Auftragsstatus: 20=Gueltig, 30=Freigegeben, 60=In Arbeit, 90=Fertig
        var activeOrderStatuses = new[] { 20, 30, 60, 90 };

        // Configs zuerst laden, um den maximal moeglichen Offset-Drift zu kennen.
        // calculatedDueDate = DueDate + offsetDays (Werktage). Der aufrufende Code filtert spaeter auf
        // calculatedDueDate IN [fromDate, toDate]. Damit kein Auftrag durchs Raster faellt, dessen DueDate
        // ausserhalb [fromDate, toDate] liegt aber dessen calc'd Date innerhalb, erweitern wir den DB-Filter
        // um den groessten absoluten Offset (max ueber alle Configs).
        var configs = await _context.OseonOperationConfigs.AsNoTracking().ToListAsync(ct);
        var configByName = configs.ToDictionary(c => c.OperationName, StringComparer.OrdinalIgnoreCase);
        var maxOffsetMagnitude = configs.Count == 0 ? 0 : configs.Max(c => Math.Abs(c.DueDateOffsetDays));
        var dbFromDate = fromDate.AddDays(-maxOffsetMagnitude);
        var dbToDate = toDate.AddDays(maxOffsetMagnitude);

        var ordersQuery = _context.OseonProductionOrders
            .AsNoTracking()
            .Include(o => o.WorkOperations)
            .Where(o => activeOrderStatuses.Contains(o.OseonStatus))
            .Where(o => o.DueDate != null)
            .Where(o => o.DueDate >= dbFromDate && o.DueDate <= dbToDate);

        if (workplaceId.HasValue)
            ordersQuery = ordersQuery.Where(o => o.ProductionWorkplaceId == workplaceId.Value);

        if (!string.IsNullOrWhiteSpace(customerOrderNumber))
            ordersQuery = ordersQuery.Where(o => o.CustomerOrderNumber != null
                && o.CustomerOrderNumber.StartsWith(customerOrderNumber));

        if (!string.IsNullOrWhiteSpace(faNumberPrefix))
            ordersQuery = ordersQuery.Where(o => o.OseonOrderNumber.StartsWith(faNumberPrefix));

        var orders = await ordersQuery.ToListAsync(ct);

        var rows = new List<OseonReportingQueryRow>();
        var noConfigCount = 0;
        var opNameSet = operationNames is { Count: > 0 }
            ? new HashSet<string>(operationNames, StringComparer.OrdinalIgnoreCase)
            : null;

        foreach (var order in orders)
        {
            foreach (var wo in order.WorkOperations.Where(w => w.OseonStatus != 95))
            {
                if (opNameSet != null && !opNameSet.Contains(wo.Name)) continue;

                if (!configByName.TryGetValue(wo.Name, out var cfg))
                {
                    noConfigCount++;
                    continue;
                }

                if (!cfg.IsOseonRelevant) continue;

                rows.Add(new OseonReportingQueryRow(wo, order, cfg));
            }
        }

        var dataAsOf = orders.Count == 0
            ? (DateTime?)null
            : orders.Max(o => o.LastChangedInOseon);

        return new OseonReportingQueryResult(rows, noConfigCount, dataAsOf);
    }
}
