using System.Globalization;
using System.Linq.Expressions;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class StockMovementRepository : Repository<StockMovement>, IStockMovementRepository
{
    public StockMovementRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<StockOverviewItem>> GetCurrentStockAsync(
        string? filterArticle = null,
        int? filterStorageLocationId = null,
        decimal? filterMinQuantity = null,
        decimal? filterMaxQuantity = null)
    {
        var query = _dbSet
            .Include(sm => sm.Article)
            .Include(sm => sm.StorageLocation)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filterArticle))
        {
            query = query.Where(sm =>
                sm.Article.ArticleNumber.Contains(filterArticle) ||
                (sm.Article.Description != null && sm.Article.Description.Contains(filterArticle)) ||
                (sm.Article.Matchcode != null && sm.Article.Matchcode.Contains(filterArticle)));
        }

        if (filterStorageLocationId.HasValue)
        {
            query = query.Where(sm => sm.StorageLocationId == filterStorageLocationId.Value);
        }

        // Query 1: Reguläre Bewegungen + Umbuchung-Ziel (gruppiert nach StorageLocationId)
        var destinationQuery = query
            .GroupBy(sm => new
            {
                sm.ArticleId,
                sm.Article.ArticleNumber,
                ArticleDescription = sm.Article.Description,
                sm.Article.Unit,
                sm.Article.ReorderLevel,
                sm.StorageLocationId,
                StorageLocationCode = sm.StorageLocation.Code,
                StorageLocationDescription = sm.StorageLocation.Description,
                sm.StorageLocation.IsPickingTransport,
                StorageLocationIsActive = sm.StorageLocation.IsActive,
                StorageLocationIstBuchbar = sm.StorageLocation.IstBuchbar
            })
            .Select(g => new StockOverviewItem
            {
                ArticleId = g.Key.ArticleId,
                ArticleNumber = g.Key.ArticleNumber,
                ArticleDescription = g.Key.ArticleDescription,
                Unit = g.Key.Unit,
                StorageLocationId = g.Key.StorageLocationId,
                StorageLocationCode = g.Key.StorageLocationCode,
                StorageLocationDescription = g.Key.StorageLocationDescription,
                CurrentQuantity = g.Sum(sm =>
                    sm.MovementType == MovementType.Einbuchung || sm.MovementType == MovementType.SageEinbuchung
                        ? sm.Quantity :
                    sm.MovementType == MovementType.Umbuchung
                        ? sm.Quantity :
                    sm.MovementType == MovementType.Ausbuchung || sm.MovementType == MovementType.SageAusbuchung
                        ? -sm.Quantity :
                    0m),
                ReorderLevel = g.Key.ReorderLevel,
                IsPickingTransport = g.Key.IsPickingTransport,
                StorageLocationIsActive = g.Key.StorageLocationIsActive,
                StorageLocationIstBuchbar = g.Key.StorageLocationIstBuchbar
            });

        var destinationItems = await destinationQuery.ToListAsync();

        // Query 2: Umbuchung-Quell-Subtraktionen (gruppiert nach SourceStorageLocationId)
        var sourceQuery = _dbSet
            .Include(sm => sm.Article)
            .Include(sm => sm.SourceStorageLocation)
            .Where(sm => sm.MovementType == MovementType.Umbuchung && sm.SourceStorageLocationId != null)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filterArticle))
        {
            sourceQuery = sourceQuery.Where(sm =>
                sm.Article.ArticleNumber.Contains(filterArticle) ||
                (sm.Article.Description != null && sm.Article.Description.Contains(filterArticle)) ||
                (sm.Article.Matchcode != null && sm.Article.Matchcode.Contains(filterArticle)));
        }

        if (filterStorageLocationId.HasValue)
        {
            sourceQuery = sourceQuery.Where(sm => sm.SourceStorageLocationId == filterStorageLocationId.Value);
        }

        var sourceItems = await sourceQuery
            .GroupBy(sm => new
            {
                sm.ArticleId,
                sm.Article.ArticleNumber,
                ArticleDescription = sm.Article.Description,
                sm.Article.Unit,
                sm.Article.ReorderLevel,
                StorageLocationId = sm.SourceStorageLocationId!.Value,
                StorageLocationCode = sm.SourceStorageLocation!.Code,
                StorageLocationDescription = sm.SourceStorageLocation!.Description,
                sm.SourceStorageLocation!.IsPickingTransport,
                StorageLocationIsActive = sm.SourceStorageLocation!.IsActive,
                StorageLocationIstBuchbar = sm.SourceStorageLocation!.IstBuchbar
            })
            .Select(g => new StockOverviewItem
            {
                ArticleId = g.Key.ArticleId,
                ArticleNumber = g.Key.ArticleNumber,
                ArticleDescription = g.Key.ArticleDescription,
                Unit = g.Key.Unit,
                StorageLocationId = g.Key.StorageLocationId,
                StorageLocationCode = g.Key.StorageLocationCode,
                StorageLocationDescription = g.Key.StorageLocationDescription,
                CurrentQuantity = -g.Sum(sm => sm.Quantity),
                ReorderLevel = g.Key.ReorderLevel,
                IsPickingTransport = g.Key.IsPickingTransport,
                StorageLocationIsActive = g.Key.StorageLocationIsActive,
                StorageLocationIstBuchbar = g.Key.StorageLocationIstBuchbar
            })
            .ToListAsync();

        // Merge: Nach (ArticleId, StorageLocationId) zusammenführen
        var merged = destinationItems
            .Concat(sourceItems)
            .GroupBy(x => new { x.ArticleId, x.StorageLocationId })
            .Select(g =>
            {
                var first = g.First();
                return new StockOverviewItem
                {
                    ArticleId = first.ArticleId,
                    ArticleNumber = first.ArticleNumber,
                    ArticleDescription = first.ArticleDescription,
                    Unit = first.Unit,
                    StorageLocationId = first.StorageLocationId,
                    StorageLocationCode = first.StorageLocationCode,
                    StorageLocationDescription = first.StorageLocationDescription,
                    CurrentQuantity = g.Sum(x => x.CurrentQuantity),
                    ReorderLevel = first.ReorderLevel,
                    IsPickingTransport = first.IsPickingTransport,
                    StorageLocationIsActive = first.StorageLocationIsActive,
                    StorageLocationIstBuchbar = first.StorageLocationIstBuchbar
                };
            })
            .ToList();

        // Standardmäßig 0-Bestände ausblenden (nur wenn kein expliziter Mengenfilter)
        if (!filterMinQuantity.HasValue && !filterMaxQuantity.HasValue)
        {
            merged = merged.Where(r => r.CurrentQuantity != 0).ToList();
        }

        if (filterMinQuantity.HasValue)
        {
            merged = merged.Where(g => g.CurrentQuantity >= filterMinQuantity.Value).ToList();
        }

        if (filterMaxQuantity.HasValue)
        {
            merged = merged.Where(g => g.CurrentQuantity <= filterMaxQuantity.Value).ToList();
        }

        // Hauptlagerplatz je Artikel laden (nur die abgefragten Artikel).
        var articleIds = merged.Select(m => m.ArticleId).Distinct().ToList();
        var primaryByArticleId = await _context.Set<Article>()
            .Where(a => articleIds.Contains(a.Id) && a.PrimaryStorageLocationId != null)
            .Select(a => new { a.Id, a.PrimaryStorageLocationId })
            .ToDictionaryAsync(a => a.Id, a => a.PrimaryStorageLocationId!.Value);

        foreach (var item in merged)
        {
            item.IsPrimaryStorageLocation =
                primaryByArticleId.TryGetValue(item.ArticleId, out var primaryLocId)
                && item.StorageLocationId == primaryLocId;
        }

        return merged
            .OrderBy(g => g.ArticleNumber)
            .ThenBy(g => g.IsPrimaryStorageLocation ? 0 : 1)
            .ThenBy(g => g.StorageLocationCode)
            .ToList();
    }

    public async Task<List<StockOverviewItem>> GetStockByProductionOrderAsync(string productionOrder, bool onlyActualStock = true)
    {
        // Kandidaten-Ermittlung (in BEIDEN Pfaden identisch): alle Bewegungen mit dieser FA-Nummer.
        var movements = await _dbSet
            .Include(sm => sm.Article)
            .Include(sm => sm.StorageLocation)
            .Where(sm => sm.ProductionOrder != null && sm.ProductionOrder.Contains(productionOrder))
            .ToListAsync();

        if (movements.Count == 0)
            return new List<StockOverviewItem>();

        if (!onlyActualStock)
        {
            // Historischer Pfad (StockOverview-FA-Filter): Netto-Summe NUR der FA-getaggten
            // Bewegungen je (ArticleId, StorageLocationId). Bewusst bit-identisch zum bisherigen
            // Verhalten (inkl. Phantom-Menge bei komplett ungetaggt ausgebuchten FAs).
            var entries = new List<(int ArticleId, string ArticleNumber, string? ArticleDescription,
                string? Unit, int StorageLocationId, string StorageLocationCode,
                string? StorageLocationDescription, bool IsPickingTransport, bool IsActive, bool IstBuchbar, decimal Qty)>();

            foreach (var sm in movements)
            {
                var qty = sm.MovementType switch
                {
                    MovementType.Einbuchung => sm.Quantity,
                    MovementType.SageEinbuchung => sm.Quantity,
                    MovementType.Umbuchung => sm.Quantity,
                    MovementType.Ausbuchung => -sm.Quantity,
                    MovementType.SageAusbuchung => -sm.Quantity,
                    _ => 0m
                };
                entries.Add((sm.ArticleId, sm.Article.ArticleNumber, sm.Article.Description,
                    sm.Article.Unit, sm.StorageLocationId, sm.StorageLocation.Code,
                    sm.StorageLocation.Description, sm.StorageLocation.IsPickingTransport,
                    sm.StorageLocation.IsActive, sm.StorageLocation.IstBuchbar, qty));
            }

            return entries
                .GroupBy(e => new { e.ArticleId, e.StorageLocationId })
                .Select(g =>
                {
                    var first = g.First();
                    return new StockOverviewItem
                    {
                        ArticleId = first.ArticleId,
                        ArticleNumber = first.ArticleNumber,
                        ArticleDescription = first.ArticleDescription,
                        Unit = first.Unit,
                        StorageLocationId = first.StorageLocationId,
                        StorageLocationCode = first.StorageLocationCode,
                        StorageLocationDescription = first.StorageLocationDescription,
                        IsPickingTransport = first.IsPickingTransport,
                        StorageLocationIsActive = first.IsActive,
                        StorageLocationIstBuchbar = first.IstBuchbar,
                        CurrentQuantity = g.Sum(e => e.Qty)
                    };
                })
                .Where(x => x.CurrentQuantity != 0)
                .OrderBy(x => x.ArticleNumber)
                .ThenBy(x => x.StorageLocationCode)
                .ToList();
        }

        // Ist-Bestand-Pfad (Einbuchungs-Hinweis + Tracking-Modal): Kandidaten-Paare über das
        // FA-Tag ermitteln, Menge aber = tatsächlicher Bestand über ALLE Bewegungen an diesem
        // Artikel/Lagerplatz-Paar (zentrale Aggregationsregel via GetCurrentStockAtLocationAsync,
        // keine weitere Kopie der MovementType-Switch-Logik). Nur Ist-Bestand > 0 zurückgeben.
        var candidates = movements
            .GroupBy(sm => new { sm.ArticleId, sm.StorageLocationId })
            .Select(g => g.First())
            .ToList();

        var result = new List<StockOverviewItem>();
        foreach (var c in candidates)
        {
            var actualStock = await GetCurrentStockAtLocationAsync(c.ArticleId, c.StorageLocationId);
            if (actualStock > 0)
            {
                result.Add(new StockOverviewItem
                {
                    ArticleId = c.ArticleId,
                    ArticleNumber = c.Article.ArticleNumber,
                    ArticleDescription = c.Article.Description,
                    Unit = c.Article.Unit,
                    StorageLocationId = c.StorageLocationId,
                    StorageLocationCode = c.StorageLocation.Code,
                    StorageLocationDescription = c.StorageLocation.Description,
                    IsPickingTransport = c.StorageLocation.IsPickingTransport,
                    StorageLocationIsActive = c.StorageLocation.IsActive,
                    StorageLocationIstBuchbar = c.StorageLocation.IstBuchbar,
                    CurrentQuantity = actualStock
                });
            }
        }

        return result
            .OrderBy(x => x.ArticleNumber)
            .ThenBy(x => x.StorageLocationCode)
            .ToList();
    }

    public async Task<(List<MovementHistoryItem> Items, int TotalCount)> GetMovementHistoryAsync(
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        string? filterArticle = null,
        int? filterStorageLocationId = null,
        MovementType? filterMovementType = null,
        int? filterUserId = null,
        string? filterProductionOrder = null,
        int page = 1,
        int pageSize = 50,
        IReadOnlyDictionary<string, string>? columnFilters = null)
    {
        var query = _dbSet
            .Include(sm => sm.Article)
            .Include(sm => sm.StorageLocation)
            .Include(sm => sm.User)
            .Include(sm => sm.SourceStorageLocation)
            .AsQueryable();

        if (dateFrom.HasValue)
            query = query.Where(sm => sm.Timestamp >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(sm => sm.Timestamp <= dateTo.Value.Date.AddDays(1));

        if (!string.IsNullOrWhiteSpace(filterArticle))
        {
            query = query.Where(sm =>
                sm.Article.ArticleNumber.Contains(filterArticle) ||
                (sm.Article.Description != null && sm.Article.Description.Contains(filterArticle)) ||
                (sm.Article.Matchcode != null && sm.Article.Matchcode.Contains(filterArticle)));
        }

        if (filterStorageLocationId.HasValue)
            query = query.Where(sm => sm.StorageLocationId == filterStorageLocationId.Value
                                   || sm.SourceStorageLocationId == filterStorageLocationId.Value);

        if (filterMovementType.HasValue)
            query = query.Where(sm => sm.MovementType == filterMovementType.Value);

        if (filterUserId.HasValue)
            query = query.Where(sm => sm.UserId == filterUserId.Value);

        if (!string.IsNullOrWhiteSpace(filterProductionOrder))
            query = query.Where(sm => sm.ProductionOrder != null && sm.ProductionOrder.Contains(filterProductionOrder));

        if (columnFilters != null)
        {
            foreach (var (key, raw) in columnFilters)
            {
                var type = ColumnFilterTypes.Of("StockMovements/Index", key);
                var parsed = ColumnFilterSyntax.Parse(raw, type);
                if (parsed.IsEmpty) continue;
                query = ApplyMovementColumnFilter(query, key, parsed);
            }
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(sm => sm.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(sm => new MovementHistoryItem
            {
                Id = sm.Id,
                Timestamp = sm.Timestamp,
                ArticleNumber = sm.Article.ArticleNumber,
                ArticleDescription = sm.Article.Description,
                Quantity = sm.Quantity,
                StorageLocationCode = sm.StorageLocation.Code,
                SourceStorageLocationCode = sm.SourceStorageLocation != null ? sm.SourceStorageLocation.Code : null,
                MovementType = sm.MovementType,
                MovementTypeName = sm.MovementType == MovementType.Einbuchung ? "Einbuchung" :
                                   sm.MovementType == MovementType.Ausbuchung ? "Ausbuchung" :
                                   sm.MovementType == MovementType.Umbuchung ? "Umbuchung" :
                                   sm.MovementType == MovementType.SageEinbuchung ? "Sage-Einbuchung" :
                                   sm.MovementType == MovementType.SageAusbuchung ? "Sage-Ausbuchung" :
                                   "Unbekannt",
                UserName = sm.User != null ? sm.User.Name : sm.WindowsUser,
                ProductionOrder = sm.ProductionOrder,
                // v1.87.0: verknuepfter FA (nur die Felder fuer Link + Status-Text)
                LinkedFa = sm.LinkedProductionOrder == null ? null : new ProductionOrder
                {
                    Id = sm.LinkedProductionOrder.Id,
                    OrderNumber = sm.LinkedProductionOrder.OrderNumber,
                    SubOrderNumber = sm.LinkedProductionOrder.SubOrderNumber,
                    IsDone = sm.LinkedProductionOrder.IsDone,
                    IsCancelled = sm.LinkedProductionOrder.IsCancelled,
                    SageMissingSince = sm.LinkedProductionOrder.SageMissingSince,
                },
                Note = sm.Note
            })
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<Dictionary<string, List<StockLocationInfo>>> GetStockByArticleNumbersAsync(List<string> articleNumbers)
    {
        if (!articleNumbers.Any())
            return new Dictionary<string, List<StockLocationInfo>>();

        // Query 1: Destination stock (Einbuchung + Umbuchung-Ziel) — ohne Kommissionierwagen
        var destItems = await _dbSet
            .Include(sm => sm.Article)
            .Include(sm => sm.StorageLocation)
            .Where(sm => articleNumbers.Contains(sm.Article.ArticleNumber)
                      && !sm.StorageLocation.IsPickingTransport)
            .GroupBy(sm => new
            {
                sm.Article.ArticleNumber,
                sm.StorageLocationId,
                StorageLocationCode = sm.StorageLocation.Code
            })
            .Select(g => new
            {
                g.Key.ArticleNumber,
                g.Key.StorageLocationId,
                g.Key.StorageLocationCode,
                Quantity = g.Sum(sm =>
                    sm.MovementType == MovementType.Einbuchung || sm.MovementType == MovementType.SageEinbuchung
                        ? sm.Quantity :
                    sm.MovementType == MovementType.Umbuchung
                        ? sm.Quantity :
                    sm.MovementType == MovementType.Ausbuchung || sm.MovementType == MovementType.SageAusbuchung
                        ? -sm.Quantity :
                    0m)
            })
            .ToListAsync();

        // Query 2: Source subtraction (Umbuchung-Quelle) — ohne Kommissionierwagen
        var srcItems = await _dbSet
            .Include(sm => sm.Article)
            .Include(sm => sm.SourceStorageLocation)
            .Where(sm => sm.MovementType == MovementType.Umbuchung
                      && sm.SourceStorageLocationId != null
                      && articleNumbers.Contains(sm.Article.ArticleNumber)
                      && !sm.SourceStorageLocation!.IsPickingTransport)
            .GroupBy(sm => new
            {
                sm.Article.ArticleNumber,
                StorageLocationId = sm.SourceStorageLocationId!.Value,
                StorageLocationCode = sm.SourceStorageLocation!.Code
            })
            .Select(g => new
            {
                g.Key.ArticleNumber,
                g.Key.StorageLocationId,
                g.Key.StorageLocationCode,
                Quantity = -g.Sum(sm => sm.Quantity)
            })
            .ToListAsync();

        // Hauptlagerplatz je Artikelnummer laden (nur abgefragte Artikel).
        var primaryByArticleNumber = await _dbSet
            .Where(sm => articleNumbers.Contains(sm.Article.ArticleNumber)
                      && sm.Article.PrimaryStorageLocationId != null)
            .Select(sm => new { sm.Article.ArticleNumber, PrimaryId = sm.Article.PrimaryStorageLocationId!.Value })
            .Distinct()
            .ToDictionaryAsync(x => x.ArticleNumber, x => x.PrimaryId);

        return destItems.Concat(srcItems)
            .GroupBy(x => new { x.ArticleNumber, x.StorageLocationId, x.StorageLocationCode })
            .Select(g => new
            {
                g.Key.ArticleNumber,
                Info = new StockLocationInfo
                {
                    StorageLocationId = g.Key.StorageLocationId,
                    Code = g.Key.StorageLocationCode,
                    Quantity = g.Sum(x => x.Quantity),
                    IsPrimaryStorageLocation =
                        primaryByArticleNumber.TryGetValue(g.Key.ArticleNumber, out var primaryId)
                        && g.Key.StorageLocationId == primaryId
                }
            })
            .Where(x => x.Info.Quantity != 0)
            .GroupBy(x => x.ArticleNumber)
            .ToDictionary(
                g => g.Key,
                // Stuecklisten-Reihenfolge (v1.62.0, Spec 2026-10-16-stueckliste-matchcode-lagerplatz-limit):
                // Hauptlagerplatz, dann Menge absteigend, bei Gleichstand Code. Wirkt auf Zelle, Popover, CSV und Druck.
                g => g.Select(x => x.Info)
                      .OrderBy(i => i.IsPrimaryStorageLocation ? 0 : 1)
                      .ThenByDescending(i => i.Quantity)
                      .ThenBy(i => i.Code)
                      .ToList()
            );
    }

    public async Task<decimal> GetCurrentStockAtLocationAsync(int articleId, int storageLocationId)
    {
        var destSum = await _dbSet
            .Where(sm => sm.ArticleId == articleId && sm.StorageLocationId == storageLocationId)
            .SumAsync(sm =>
                sm.MovementType == MovementType.Einbuchung || sm.MovementType == MovementType.SageEinbuchung
                    ? sm.Quantity :
                sm.MovementType == MovementType.Umbuchung
                    ? sm.Quantity :
                sm.MovementType == MovementType.Ausbuchung || sm.MovementType == MovementType.SageAusbuchung
                    ? -sm.Quantity :
                0m);

        var srcSum = await _dbSet
            .Where(sm => sm.ArticleId == articleId
                      && sm.SourceStorageLocationId == storageLocationId
                      && sm.MovementType == MovementType.Umbuchung)
            .SumAsync(sm => sm.Quantity);

        return destSum - srcSum;
    }

    public async Task<List<string>> GetProductionOrdersAtLocationAsync(int storageLocationId)
    {
        // Distinct ProductionOrder-Werte von Umbuchungen auf diesen Lagerplatz
        // (nur für Artikel die dort aktuell positiven Bestand haben)
        var currentStock = await GetCurrentStockAsync(filterStorageLocationId: storageLocationId);
        var articleIdsWithStock = currentStock
            .Where(s => s.CurrentQuantity > 0)
            .Select(s => s.ArticleId)
            .ToHashSet();

        if (articleIdsWithStock.Count == 0)
            return new List<string>();

        var orders = await _dbSet
            .Where(sm => sm.StorageLocationId == storageLocationId
                      && sm.ProductionOrder != null
                      && articleIdsWithStock.Contains(sm.ArticleId))
            .Select(sm => sm.ProductionOrder!)
            .Distinct()
            .ToListAsync();

        // v1.99.13: Texte aus „Trotzdem buchen“ („A;B“) in Einzelnummern zerlegen, jede einmal — wirkt fuer Konfliktpruefung, Modal,
        // Zusammenfuehren und „Lagerplatz ausbuchen“; Altbestand mit Wiederholungen wird beim Lesen normalisiert.
        return orders
            .SelectMany(o => o.Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct()
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    public async Task<Dictionary<(int ArticleId, int StorageLocationId), decimal>> GetCurrentStockByArticleAndLocationAsync()
    {
        var movements = await _dbSet
            .AsNoTracking()
            .ToListAsync();

        var dict = new Dictionary<(int, int), decimal>();

        foreach (var sm in movements)
        {
            decimal effect = sm.MovementType switch
            {
                MovementType.Einbuchung => sm.Quantity,
                MovementType.SageEinbuchung => sm.Quantity,
                MovementType.Umbuchung => sm.Quantity,   // Ziel-Lagerplatz
                MovementType.Ausbuchung => -sm.Quantity,
                MovementType.SageAusbuchung => -sm.Quantity,
                _ => 0m
            };

            var key = (sm.ArticleId, sm.StorageLocationId);
            dict[key] = dict.GetValueOrDefault(key, 0m) + effect;

            // Umbuchung-Quell-Seite: -Quantity am SourceStorageLocationId
            if (sm.MovementType == MovementType.Umbuchung && sm.SourceStorageLocationId.HasValue)
            {
                var srcKey = (sm.ArticleId, sm.SourceStorageLocationId.Value);
                dict[srcKey] = dict.GetValueOrDefault(srcKey, 0m) - sm.Quantity;
            }
        }

        return dict;
    }

    /// <summary>Maps Bewegungshistorie Column-Keys auf StockMovement-Properties.</summary>
    /// <remarks>
    /// internal statt private (Task 6, Matchcode): direkt gegen einen nie verbundenen SqlServer-
    /// Context via ToQueryString() testbar, siehe ProductionOrderRepository.BuildLeitstandQuery
    /// (gleiches Muster). InternalsVisibleTo fuer IdealAkeWms.Tests siehe csproj.
    /// </remarks>
    internal static IQueryable<StockMovement> ApplyMovementColumnFilter(IQueryable<StockMovement> q, string key, ParsedColumnFilter f)
    {
        switch (key)
        {
            case "movement-type":
            {
                // Bewegungsart: Anzeigename gegen den Filter (Kern), in SQL ein IN ueber die passenden Enum-Werte.
                var matching = ColumnFilterSyntax.MatchingValues(_movementTypeNames, kv => kv.Value, f).Select(kv => kv.Key).ToList();
                if (f.IsEmpty) return q;
                return f.Negate ? q.Where(sm => !matching.Contains(sm.MovementType)) : q.Where(sm => matching.Contains(sm.MovementType));
            }
            case "datetime":
            {
                // Datum/Zeit: Vergleich/Bereich tageweise (Antwort 3), Tag/Monat/Jahr ohne Operator wie bisher als Zeitraum.
                var pred = ColumnFilterSql.DatePredicate<StockMovement>(f, sm => sm.Timestamp);
                if (pred != null) return q.Where(pred);
                return f.Negate || f.IsEmpty ? q : q.Where(sm => false); // kein lesbares Datum: positiv leer, negiert alle (wie bisher)
            }
        }
        var text = key switch
        {
            "article" => ColumnFilterSql.Predicate<StockMovement>(f, sm => sm.Article.ArticleNumber, sm => sm.Article.Description, sm => sm.Article.Matchcode),
            "storage-location" => ColumnFilterSql.Predicate<StockMovement>(f, sm => sm.StorageLocation.Code, sm => sm.SourceStorageLocation!.Code),
            "user" => ColumnFilterSql.Predicate<StockMovement>(f, sm => sm.User!.Name, sm => sm.WindowsUser),
            "production-order" => ColumnFilterSql.Predicate<StockMovement>(f, sm => sm.ProductionOrder),
            _ => null
        };
        return text == null ? q : q.Where(text);
    }

    private static readonly Dictionary<MovementType, string> _movementTypeNames = new()
    {
        [MovementType.Einbuchung] = "einbuchung",
        [MovementType.Ausbuchung] = "ausbuchung",
        [MovementType.Umbuchung] = "umbuchung",
        [MovementType.SageEinbuchung] = "sage-einbuchung",
        [MovementType.SageAusbuchung] = "sage-ausbuchung",
    };

    private static Expression<Func<T, bool>> OrElse<T>(
        Expression<Func<T, bool>> a, Expression<Func<T, bool>> b)
    {
        var param = Expression.Parameter(typeof(T), "sm");
        var left = new ReplaceParameterVisitor(a.Parameters[0], param).Visit(a.Body)!;
        var right = new ReplaceParameterVisitor(b.Parameters[0], param).Visit(b.Body)!;
        return Expression.Lambda<Func<T, bool>>(Expression.OrElse(left, right), param);
    }

    private sealed class ReplaceParameterVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _from;
        private readonly ParameterExpression _to;
        public ReplaceParameterVisitor(ParameterExpression from, ParameterExpression to)
        {
            _from = from;
            _to = to;
        }
        protected override Expression VisitParameter(ParameterExpression node)
            => node == _from ? _to : base.VisitParameter(node);
    }
}
