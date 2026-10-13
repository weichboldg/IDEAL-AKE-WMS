using IdealAkeWms.Models;
using IdealAkeWms.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IdealAkeWms.Data.Repositories;

public class WorkOperationRepository : Repository<WorkOperation>, IWorkOperationRepository
{
    private readonly ILogger<WorkOperationRepository>? _logger;

    // Logger optional (Default null): DI injiziert ihn produktiv; Unit-Tests konstruieren ohne.
    public WorkOperationRepository(ApplicationDbContext context, ILogger<WorkOperationRepository>? logger = null)
        : base(context)
    {
        _logger = logger;
    }

    public async Task<List<WorkOperation>> GetByProductionOrderIdAsync(int productionOrderId)
    {
        return await _dbSet
            .Where(wo => wo.ProductionOrderId == productionOrderId)
            .OrderBy(wo => wo.Sequence)
            .ToListAsync();
    }

    public async Task<List<WorkOperation>> GetByProductionOrderIdWithWorkplaceAsync(int productionOrderId)
    {
        return await _dbSet
            .Include(wo => wo.ProductionWorkplace)
            .Where(wo => wo.ProductionOrderId == productionOrderId)
            .OrderBy(wo => wo.Sequence)
            .ToListAsync();
    }

    public async Task<List<WorkOperation>> GetAllWithOrderAndWorkplaceAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .Include(wo => wo.ProductionOrder)
            .Include(wo => wo.ProductionWorkplace)
            .OrderBy(wo => wo.ProductionOrder.OrderNumber)
            .ThenBy(wo => wo.Sequence)
            .ToListAsync();
    }

    public async Task<List<WorkOperation>> GetByWorkplaceIdAsync(int workplaceId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(wo => wo.ProductionOrder)
            .Include(wo => wo.ProductionWorkplace)
            .Where(wo => wo.ProductionWorkplaceId == workplaceId)
            .OrderBy(wo => wo.ProductionOrder.OrderNumber)
            .ThenBy(wo => wo.Sequence)
            .ToListAsync();
    }

    public async Task<List<WorkOperation>> GetOpenByWorkplaceIdAsync(int workplaceId, bool excludePackedOrders = false)
    {
        var query = _dbSet
            .AsNoTracking()
            .Include(wo => wo.ProductionOrder)
            .Include(wo => wo.ProductionWorkplace)
            .Where(wo => wo.ProductionWorkplaceId == workplaceId && !wo.IsReported);

        if (excludePackedOrders)
        {
            // Fold 2 (Spec §10.6): Filter IN der EF-Query (die Methode laedt ExtraInfo
            // nicht — kein nachgelagerter In-Memory-Filter moeglich). Ausgeschriebenes
            // Null-Guard-Praedikat, InMemory-kompatibel (kein statischer Helper in EF).
            query = query.Where(wo => wo.ProductionOrder.ExtraInfo == null
                || wo.ProductionOrder.ExtraInfo.SageStatus == null
                || (wo.ProductionOrder.ExtraInfo.SageStatus.Trim().ToLower() != FaZusatzinfoStatus.Verpackt
                    && wo.ProductionOrder.ExtraInfo.SageStatus.Trim().ToLower() != FaZusatzinfoStatus.Abgeholt));
        }

        return await query
            .OrderBy(wo => wo.ProductionOrder.OrderNumber)
            .ThenBy(wo => wo.Sequence)
            .ToListAsync();
    }

    public async Task<WorkOperation?> GetByFaAndOperationAsync(string faNumber, string operationNumber)
    {
        // Verhalten unveraendert (erste Zeile), aber Mehrfachtreffer protokollieren (Teil 7 AK 10):
        // im hierarchischen Modus ist OrderNumber nicht mehr eindeutig. Aufrufer-Umstellung = Teil 8.
        var hits = await _dbSet
            .Include(w => w.ProductionOrder)
            .Include(w => w.ProductionWorkplace)
            .Where(w => w.ProductionOrder.OrderNumber == faNumber && w.OperationNumber == operationNumber)
            .Take(2)
            .ToListAsync();
        if (hits.Count > 1)
            _logger?.LogWarning(
                "GetByFaAndOperationAsync: Mehrfachtreffer fuer FA {Fa} / Operation {Op} (hierarchischer Modus) — Einzel-Lookup liefert die erste Zeile; Aufrufer sollte GetAllByFaAndOperationAsync nutzen (Teil 8).",
                faNumber, operationNumber);
        return hits.FirstOrDefault();
    }

    public Task<List<WorkOperation>> GetAllByFaAndOperationAsync(string faNumber, string operationNumber) =>
        _dbSet
            .Include(w => w.ProductionOrder)
            .Include(w => w.ProductionWorkplace)
            .Where(w =>
                w.ProductionOrder.OrderNumber == faNumber &&
                w.OperationNumber == operationNumber)
            .ToListAsync();

    public async Task<HashSet<string>> GetOrderNumbersWithWorkOperationsAsync(IReadOnlyCollection<string> orderNumbers)
    {
        if (orderNumbers == null || orderNumbers.Count == 0)
            return new HashSet<string>();

        return (await OrderNumbersWithWorkOperationsQuery(orderNumbers).ToListAsync()).ToHashSet();
    }

    // internal fuer den ToQueryString-Test (SqlServer-Provider, nie verbunden): belegt, wie EF das
    // Contains ueber die HauptFA-Liste uebersetzt (Parameter-Grenze 2100 bei Seitengroesse "Alle").
    internal IQueryable<string> OrderNumbersWithWorkOperationsQuery(IReadOnlyCollection<string> orderNumbers)
    {
        var set = orderNumbers.ToList();
        return _dbSet
            .AsNoTracking()
            .Where(wo => set.Contains(wo.ProductionOrder.OrderNumber))
            .Select(wo => wo.ProductionOrder.OrderNumber)
            .Distinct();
    }

    public async Task<List<FaStatusWorkOperation>> GetForFaStatusAsync(IReadOnlyCollection<string> orderNumbers)
    {
        if (orderNumbers == null || orderNumbers.Count == 0) return new List<FaStatusWorkOperation>();
        return await FaStatusQuery(orderNumbers).ToListAsync();
    }

    // internal fuer den ToQueryString-Test (OPENJSON statt Parameterliste bei Seitengroesse "Alle").
    internal IQueryable<FaStatusWorkOperation> FaStatusQuery(IReadOnlyCollection<string> orderNumbers)
    {
        var set = orderNumbers.ToList();
        return _dbSet
            .AsNoTracking()
            .Where(wo => set.Contains(wo.ProductionOrder.OrderNumber))
            .Select(wo => new FaStatusWorkOperation(wo.Id, wo.ProductionOrder.OrderNumber, wo.ProductionOrder.SubOrderNumber!,
                wo.OperationNumber, wo.ProductionWorkplaceId));
    }

    public async Task<FaBdeStatus> GetBdeStatusAsync(IReadOnlyCollection<int> workOperationIds)
    {
        if (workOperationIds == null || workOperationIds.Count == 0)
            return new FaBdeStatus(new HashSet<int>(), new HashSet<int>());
        var (erledigt, laeuft) = BdeStatusQueries(workOperationIds);
        return new FaBdeStatus((await erledigt.ToListAsync()).ToHashSet(), (await laeuft.ToListAsync()).ToHashSet());
    }

    // Praedikate identisch zum Terminal (BdeApiController: IsFinal && !IsCancelled) bzw. BdeBookingRepository (EndedAt == null).
    internal (IQueryable<int> Erledigt, IQueryable<int> Laeuft) BdeStatusQueries(IReadOnlyCollection<int> workOperationIds)
    {
        var ids = workOperationIds.ToList();
        var erledigt = _context.BdeBookingQuantities.AsNoTracking()
            .Where(q => q.IsFinal && !q.BdeBooking.IsCancelled && q.BdeBooking.WorkOperationId != null
                        && ids.Contains(q.BdeBooking.WorkOperationId.Value))
            .Select(q => q.BdeBooking.WorkOperationId!.Value)
            .Distinct();
        var laeuft = _context.BdeBookings.AsNoTracking()
            .Where(b => b.EndedAt == null && !b.IsCancelled && b.WorkOperationId != null && ids.Contains(b.WorkOperationId.Value))
            .Select(b => b.WorkOperationId!.Value)
            .Distinct();
        return (erledigt, laeuft);
    }

    public async Task<List<WorkOperation>> GetByOrderNumberWithWorkplaceAsync(string orderNumber)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(wo => wo.ProductionOrder)
            .Include(wo => wo.ProductionWorkplace)
            .Where(wo => wo.ProductionOrder.OrderNumber == orderNumber)
            .OrderBy(wo => wo.ProductionOrder.SubOrderNumber)
            .ThenBy(wo => wo.Sequence)
            .ToListAsync();
    }
}
