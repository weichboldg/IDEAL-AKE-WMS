using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class ProductionOrderRepository : Repository<ProductionOrder>, IProductionOrderRepository
{
    public ProductionOrderRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<ProductionOrder>> GetAllOrderedAsync()
    {
        return await _dbSet
            .Include(o => o.ProductionWorkplace)
            .Include(o => o.PickingStatus)
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
        IQueryable<ProductionOrder> q = _dbSet.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filterOrderNumber))
            q = q.Where(o => EF.Functions.Like(o.OrderNumber, $"%{filterOrderNumber}%"));

        if (!string.IsNullOrWhiteSpace(filterArticleNumber))
            q = q.Where(o => o.ArticleNumber != null && EF.Functions.Like(o.ArticleNumber, $"%{filterArticleNumber}%"));

        if (!string.IsNullOrWhiteSpace(filterCustomer))
            q = q.Where(o => o.Customer != null && EF.Functions.Like(o.Customer, $"%{filterCustomer}%"));

        if (!showDone)
            q = q.Where(o => !o.IsDone && (o.PickingStatus == null || !o.PickingStatus.IsDonePicking));

        if (columnFilters != null)
        {
            foreach (var (key, raw) in columnFilters)
            {
                var (tokens, negate) = Services.ColumnFilterHelper.Parse(raw);
                if (tokens.Count == 0) continue;
                q = ApplyLeitstandColumnFilter(q, key, tokens, negate);
            }
        }

        var totalCount = await q.CountAsync();

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = int.MaxValue;
        var skip = (page - 1) * pageSize;

        var rows = await q
            .OrderBy(o => o.OrderNumber)
            .Skip(skip)
            .Take(pageSize)
            .Select(o => new LeitstandOrderRow(
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
                o.ProductionWorkplace != null ? o.ProductionWorkplace.Name : null))
            .ToListAsync();

        return new LeitstandOrderPage(rows, totalCount);
    }

    public async Task<List<ProductionOrder>> GetOpenOrdersAsync()
    {
        return await _dbSet.Where(o => !o.IsDone).OrderBy(o => o.OrderNumber).ToListAsync();
    }

    public async Task<ProductionOrder?> GetByOrderNumberAsync(string orderNumber)
    {
        return await _dbSet.FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
    }

    public async Task<List<ProductionOrder>> SearchAsync(string? query, int limit = 20)
    {
        var q = _dbSet.Where(o => !o.IsDone);

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

    /// <summary>
    /// Maps a <c>data-col-key</c> der FA-/Leitstand-Liste auf die entsprechende
    /// Property von <see cref="ProductionOrder"/> und appliziert OR-/NOT-Tokens.
    /// Date-Spalten werden bewusst NICHT serverseitig gefiltert
    /// (clientseitiger Kalender/KW-Picker uebernimmt das auf der aktuellen Seite).
    /// </summary>
    private static IQueryable<ProductionOrder> ApplyLeitstandColumnFilter(
        IQueryable<ProductionOrder> q, string key, List<string> tokens, bool negate)
    {
        // Pattern: jeder Token wird zu "%token%" — EF Core kann lokale Listen in
        // .Any(...) zu OR-LIKE-Chains uebersetzen.
        var patterns = tokens.Select(t => $"%{t}%").ToList();

        return key switch
        {
            "order-number" => negate
                ? q.Where(o => !patterns.Any(p => EF.Functions.Like(o.OrderNumber, p)))
                : q.Where(o => patterns.Any(p => EF.Functions.Like(o.OrderNumber, p))),

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

            _ => q
        };
    }
}
