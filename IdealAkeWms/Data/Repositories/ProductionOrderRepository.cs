using System.Linq.Expressions;
using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class ProductionOrderRepository : Repository<ProductionOrder>, IProductionOrderRepository
{
    public ProductionOrderRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<bool> HierarchicalDataExistsAsync()
        => await _dbSet
            .AsNoTracking()
            .AnyAsync(p => p.OrderNumber != p.SubOrderNumber);

    public async Task<List<ProductionOrder>> GetAllOrderedAsync()
    {
        return await _dbSet
            .Include(o => o.ProductionWorkplace)
            .Include(o => o.PickingStatus)
            .Include(o => o.ExtraInfo)
            .OrderBy(o => o.OrderNumber)
            .ToListAsync();
    }

    public async Task<LeitstandOrderPage> GetForLeitstandAsync(
        string? filterOrderNumber,
        string? filterArticleNumber,
        string? filterCustomer,
        bool showDone,
        int page,
        int pageSize,
        IReadOnlyDictionary<string, string>? columnFilters = null)
    {
        var q = BuildLeitstandQuery(filterOrderNumber, filterArticleNumber, filterCustomer,
            showDone, columnFilters, hierarchical: false);

        var totalCount = await q.CountAsync();

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = int.MaxValue;
        var skip = (page - 1) * pageSize;

        var rows = await ProjectLeitstandRows(
            q.OrderBy(o => o.OrderNumber).Skip(skip).Take(pageSize)).ToListAsync();

        return new LeitstandOrderPage(rows, totalCount);
    }

    public async Task<LeitstandOrderGroupedPage> GetForLeitstandGroupedAsync(
        string? filterOrderNumber,
        string? filterArticleNumber,
        string? filterCustomer,
        bool showDone,
        int page,
        int pageSize,
        IReadOnlyDictionary<string, string>? columnFilters = null)
    {
        // Dieselben Filter (Zeilen-Ebene, Z3b), aber order-number matcht SubOrderNumber ODER OrderNumber.
        var q = BuildLeitstandQuery(filterOrderNumber, filterArticleNumber, filterCustomer,
            showDone, columnFilters, hierarchical: true);

        var totalRowCount = await q.CountAsync();

        // Paginierung ueber GRUPPEN: distinkte OrderNumber, geordnet. Eine Gruppe wird nie geteilt.
        var groupKeysQuery = q.Select(o => o.OrderNumber).Distinct().OrderBy(k => k);
        var totalGroupCount = await groupKeysQuery.CountAsync();

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = int.MaxValue;
        var pageKeys = await groupKeysQuery.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        if (pageKeys.Count == 0)
            return new LeitstandOrderGroupedPage(new List<LeitstandOrderGroup>(), totalGroupCount, totalRowCount);

        // Alle passenden Zeilen der Seiten-Gruppen, je Gruppe SubOrderNumber aufsteigend (Z2).
        var rows = await ProjectLeitstandRows(
            q.Where(o => pageKeys.Contains(o.OrderNumber))
             .OrderBy(o => o.OrderNumber).ThenBy(o => o.SubOrderNumber)).ToListAsync();

        // GroupBy erhaelt die Quell-Reihenfolge (nach OrderNumber sortiert) -> Gruppen in Schluessel-Ordnung.
        var groups = rows
            .GroupBy(r => r.OrderNumber)
            .Select(g => new LeitstandOrderGroup(g.Key, g.ToList()))
            .ToList();

        return new LeitstandOrderGroupedPage(groups, totalGroupCount, totalRowCount);
    }

    // Gemeinsamer Filter-Aufbau fuer flache und gruppierte Leitstand-/FA-Liste. hierarchical=true laesst
    // die order-number-Suche zusaetzlich SubOrderNumber treffen (Z3b/§4: SubOrderNumber ODER OrderNumber).
    private IQueryable<ProductionOrder> BuildLeitstandQuery(
        string? filterOrderNumber, string? filterArticleNumber, string? filterCustomer,
        bool showDone, IReadOnlyDictionary<string, string>? columnFilters, bool hierarchical)
    {
        IQueryable<ProductionOrder> q = _dbSet.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filterOrderNumber))
            q = hierarchical
                ? q.Where(o => EF.Functions.Like(o.OrderNumber, $"%{filterOrderNumber}%")
                            || EF.Functions.Like(o.SubOrderNumber, $"%{filterOrderNumber}%"))
                : q.Where(o => EF.Functions.Like(o.OrderNumber, $"%{filterOrderNumber}%"));

        if (!string.IsNullOrWhiteSpace(filterArticleNumber))
            q = q.Where(o => o.ArticleNumber != null && EF.Functions.Like(o.ArticleNumber, $"%{filterArticleNumber}%"));

        if (!string.IsNullOrWhiteSpace(filterCustomer))
            q = q.Where(o => o.Customer != null && EF.Functions.Like(o.Customer, $"%{filterCustomer}%"));

        if (!showDone)
            q = q.Where(o => !o.IsDone && !o.IsCancelled && (o.PickingStatus == null || !o.PickingStatus.IsDonePicking));

        if (columnFilters != null)
        {
            foreach (var (key, raw) in columnFilters)
            {
                var (tokens, negate) = Services.ColumnFilterHelper.Parse(raw);
                if (tokens.Count == 0) continue;
                q = ApplyLeitstandColumnFilter(q, key, tokens, negate, hierarchical);
            }
        }

        return q;
    }

    private static IQueryable<LeitstandOrderRow> ProjectLeitstandRows(IQueryable<ProductionOrder> q) =>
        q.Select(o => new LeitstandOrderRow(
            o.Id,
            o.OrderNumber,
            o.Quantity,
            o.Customer,
            o.ArticleNumber,
            o.Description1,
            o.Description2,
            o.ProductionDate,
            o.DeliveryDate,
            o.IsDone,
            o.PickingStatus != null && o.PickingStatus.IsDonePicking,
            o.IsCancelled,
            o.ProductionWorkplace != null ? o.ProductionWorkplace.Name : null,
            o.ExtraInfo != null ? o.ExtraInfo.Kaeltemittel : null,
            o.ExtraInfo != null ? o.ExtraInfo.Ventil : null,
            o.ExtraInfo != null ? o.ExtraInfo.AusfuehrungEZ : null,
            o.ExtraInfo != null ? o.ExtraInfo.Maschine : null,
            o.ExtraInfo != null ? o.ExtraInfo.SageStatus : null,
            o.ProductionWorkplace != null ? o.ProductionWorkplace.OverridePrePickingDays : null,
            o.SubOrderNumber,
            o.ParentSubOrderNumber,
            o.SageMissingSince));

    public async Task<List<ProductionOrder>> GetOpenOrdersAsync()
    {
        return await _dbSet.Where(o => !o.IsDone && !o.IsCancelled).OrderBy(o => o.OrderNumber).ToListAsync();
    }

    public async Task<ProductionOrder?> GetByOrderNumberAsync(string orderNumber)
    {
        return await _dbSet.FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
    }

    public async Task<int> CountByOrderNumberAsync(string orderNumber)
    {
        return await _dbSet.CountAsync(o => o.OrderNumber == orderNumber);
    }

    public async Task<List<ProductionOrder>> SearchAsync(string? query, int limit = 20)
    {
        var q = _dbSet.Where(o => !o.IsDone && !o.IsCancelled);

        if (!string.IsNullOrWhiteSpace(query))
        {
            q = q.Where(o =>
                o.OrderNumber.Contains(query) ||
                (o.ArticleNumber != null && o.ArticleNumber.Contains(query)) ||
                (o.Customer != null && o.Customer.Contains(query)));
        }

        return await q
            .OrderBy(o => o.ProductionDate.HasValue ? 0 : 1)
            .ThenBy(o => o.ProductionDate)
            .Take(limit).ToListAsync();
    }

    public async Task<List<ProductionOrder>> GetOpenOrdersInWindowAsync(int weeksAhead, int maxCount)
    {
        if (weeksAhead <= 0) weeksAhead = 8;
        if (maxCount <= 0) maxCount = 200;

        var cutoff = DateTime.Now.AddDays(weeksAhead * 7);

        return await _dbSet
            .Where(po => !po.IsDone
                         && !po.IsCancelled
                         && !(po.PickingStatus != null && po.PickingStatus.IsDonePicking)
                         && po.ProductionDate != null
                         && po.ProductionDate <= cutoff)
            .OrderBy(po => po.ProductionDate)
            .Take(maxCount)
            .ToListAsync();
    }

    public async Task<List<ProductionOrder>> GetByArticleNumbersAsync(List<string> articleNumbers)
    {
        if (articleNumbers == null || articleNumbers.Count == 0)
            return new List<ProductionOrder>();

        return await _dbSet
            .AsNoTracking()
            .Include(o => o.PickingStatus)
            .Where(o => o.ArticleNumber != null && articleNumbers.Contains(o.ArticleNumber))
            .OrderBy(o => o.ProductionDate)
            .ToListAsync();
    }

    public async Task<List<ProductionOrder>> GetBySubOrderNumbersAsync(IReadOnlyCollection<string> subOrderNumbers)
    {
        if (subOrderNumbers == null || subOrderNumbers.Count == 0)
            return new List<ProductionOrder>();

        var set = subOrderNumbers.ToList();
        return await _dbSet
            .AsNoTracking()
            .Include(o => o.PickingStatus)
            .Where(o => o.SubOrderNumber != null && set.Contains(o.SubOrderNumber))
            .ToListAsync();
    }

    /// <summary>
    /// Maps a <c>data-col-key</c> der FA-/Leitstand-Liste auf die entsprechende
    /// Property von <see cref="ProductionOrder"/> und appliziert OR-/NOT-Tokens.
    /// Date-Spalten werden bewusst NICHT serverseitig gefiltert
    /// (clientseitiger Kalender/KW-Picker uebernimmt das auf der aktuellen Seite).
    /// </summary>
    private static IQueryable<ProductionOrder> ApplyLeitstandColumnFilter(
        IQueryable<ProductionOrder> q, string key, List<string> tokens, bool negate, bool hierarchical = false)
    {
        // Pattern: jeder Token wird zu "%token%" — EF Core kann lokale Listen in
        // .Any(...) zu OR-LIKE-Chains uebersetzen.
        var patterns = tokens.Select(t => $"%{t}%").ToList();

        return key switch
        {
            // Im hierarchischen Modus zeigt die order-number-Zelle SubOrderNumber -> der Filter matcht
            // SubOrderNumber ODER OrderNumber (HauptFA bringt die ganze Gruppe, Sub-FA die Zeile).
            "order-number" => hierarchical
                ? (negate
                    ? q.Where(o => !patterns.Any(p => EF.Functions.Like(o.OrderNumber, p) || EF.Functions.Like(o.SubOrderNumber, p)))
                    : q.Where(o => patterns.Any(p => EF.Functions.Like(o.OrderNumber, p) || EF.Functions.Like(o.SubOrderNumber, p))))
                : (negate
                    ? q.Where(o => !patterns.Any(p => EF.Functions.Like(o.OrderNumber, p)))
                    : q.Where(o => patterns.Any(p => EF.Functions.Like(o.OrderNumber, p)))),

            "customer" => negate
                ? q.Where(o => o.Customer == null || !patterns.Any(p => EF.Functions.Like(o.Customer, p)))
                : q.Where(o => o.Customer != null && patterns.Any(p => EF.Functions.Like(o.Customer, p))),

            "article-number" => negate
                ? q.Where(o => o.ArticleNumber == null || !patterns.Any(p => EF.Functions.Like(o.ArticleNumber, p)))
                : q.Where(o => o.ArticleNumber != null && patterns.Any(p => EF.Functions.Like(o.ArticleNumber, p))),

            "description1" => negate
                ? q.Where(o => o.Description1 == null || !patterns.Any(p => EF.Functions.Like(o.Description1, p)))
                : q.Where(o => o.Description1 != null && patterns.Any(p => EF.Functions.Like(o.Description1, p))),

            "description2" => negate
                ? q.Where(o => o.Description2 == null || !patterns.Any(p => EF.Functions.Like(o.Description2, p)))
                : q.Where(o => o.Description2 != null && patterns.Any(p => EF.Functions.Like(o.Description2, p))),

            "workbench" => negate
                ? q.Where(o => o.ProductionWorkplace == null || !patterns.Any(p => EF.Functions.Like(o.ProductionWorkplace.Name, p)))
                : q.Where(o => o.ProductionWorkplace != null && patterns.Any(p => EF.Functions.Like(o.ProductionWorkplace.Name, p))),

            // FA-Zusatzinfos (Sage, v1.26.0): Contains-basiert mit Null-Guards —
            // KEIN EF.Functions.Like (InMemory-Testbarkeit, Spec §5.3). Tokens sind
            // lowercase (ColumnFilterHelper.Parse), daher ToLower() auf dem Wert.
            // OR-Kette als Expression-Tree (BdeBookings-Pattern): ein nested
            // tokens.Any(...)-Lambda auf der nullable Navigation uebersetzt der
            // InMemory-Provider nicht.
            "kaeltemittel" => q.Where(BuildExtraInfoOrContains(o => o.ExtraInfo!.Kaeltemittel, tokens, negate)),
            "ventil" => q.Where(BuildExtraInfoOrContains(o => o.ExtraInfo!.Ventil, tokens, negate)),
            "ausfuehrung" => q.Where(BuildExtraInfoOrContains(o => o.ExtraInfo!.AusfuehrungEZ, tokens, negate)),
            "maschine" => q.Where(BuildExtraInfoOrContains(o => o.ExtraInfo!.Maschine, tokens, negate)),
            "sage-status" => q.Where(BuildExtraInfoOrContains(o => o.ExtraInfo!.SageStatus, tokens, negate)),

            _ => q
        };
    }

    /// <summary>
    /// FA-Zusatzinfos (Sage, v1.26.0): baut fuer ein ExtraInfo-Feld eine OR-Kette
    /// von <c>value.ToLower().Contains(token)</c>-Calls als Expression-Tree mit
    /// Null-Guards auf <c>ExtraInfo</c> UND dem Feld selbst.
    /// Positiv: <c>ExtraInfo != null &amp;&amp; value != null &amp;&amp; (contains t1 || ...)</c>;
    /// Negation: <c>ExtraInfo == null || value == null || !(contains t1 || ...)</c>
    /// (leere Zelle matcht NOT — identisch zum Client-Filter).
    /// KEIN EF.Functions.Like und KEIN nested tokens.Any(...)-Lambda — beides
    /// scheitert am InMemory-Provider (Pattern aus BdeBookingRepository).
    /// </summary>
    private static Expression<Func<ProductionOrder, bool>> BuildExtraInfoOrContains(
        Expression<Func<ProductionOrder, string?>> selector,
        IReadOnlyList<string> tokens,
        bool negate)
    {
        var param = selector.Parameters[0];
        var valueExpr = (MemberExpression)selector.Body;          // o.ExtraInfo.X
        var extraInfoExpr = valueExpr.Expression!;                // o.ExtraInfo

        var toLowerMethod = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!;
        var containsMethod = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })!;
        var lowered = Expression.Call(valueExpr, toLowerMethod);

        Expression? orChain = null;
        foreach (var t in tokens)
        {
            var call = Expression.Call(lowered, containsMethod, Expression.Constant(t));
            orChain = orChain == null ? (Expression)call : Expression.OrElse(orChain, call);
        }

        var nullExtraInfo = Expression.Constant(null, extraInfoExpr.Type);
        var nullString = Expression.Constant(null, typeof(string));

        Expression body = negate
            ? Expression.OrElse(
                Expression.Equal(extraInfoExpr, nullExtraInfo),
                Expression.OrElse(
                    Expression.Equal(valueExpr, nullString),
                    Expression.Not(orChain!)))
            : Expression.AndAlso(
                Expression.NotEqual(extraInfoExpr, nullExtraInfo),
                Expression.AndAlso(
                    Expression.NotEqual(valueExpr, nullString),
                    orChain!));

        return Expression.Lambda<Func<ProductionOrder, bool>>(body, param);
    }

    public async Task<ProductionOrderExtraInfo?> GetExtraInfoAsync(int productionOrderId)
    {
        return await _context.ProductionOrderExtraInfos
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.ProductionOrderId == productionOrderId);
    }
}
