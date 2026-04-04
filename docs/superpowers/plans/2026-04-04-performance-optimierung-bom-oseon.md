# Performance-Optimierung: BOM & OSEON Teileverfolgung

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Eliminate N+1 queries in PickingTransferService, add missing DB indexes for StockMovement queries, introduce application-level caching for frequently-read reference data, and add a command timeout to prevent long-running queries from hanging.

**Architecture:** The app uses ASP.NET Core 10 MVC + EF Core 10 + SQL Server with Repository pattern. Performance bottlenecks are: (1) N+1 queries in `DoTransferAsync` — 4-5 DB calls per picked item inside a loop, (2) missing indexes on `StockMovements.ProductionOrder` and composite indexes for stock calculations, (3) no caching for reference data that rarely changes (holidays, operation configs, settings), (4) duplicate data loading in `ApplyPickingSelectionsAsync`. The fix strategy is: batch-load data before loops with running stock tracking, add targeted indexes, wrap reference repos in MemoryCache decorators, and configure EF command timeout.

**Tech Stack:** ASP.NET Core 10, EF Core 10, SQL Server, IMemoryCache, xUnit + FluentAssertions

---

## Critical Design Notes

### Stale-Stock Problem in Batch Transfer
When two PickingItems reference the **same article at the same source location**, a naive batch pre-calculation would show sufficient stock for both — even if the combined quantity exceeds available stock. The solution: maintain a **running stock counter** in the loop that decrements after each item. This preserves the batch-load efficiency while ensuring correctness.

### AsNoTracking Scope
EF Core automatically skips change tracking for `.Select()` projections (DTOs/anonymous types). Adding `.AsNoTracking()` is only beneficial for queries that return **entity objects** (e.g., `GetPagedAsync`, `GetAllWithOperationsAsync`). For projection-based queries it has no effect and should not be added to avoid misleading code.

### EF Migrations
This project uses **SQL-script-based migrations** with `OBJECT_ID` guards (not `dotnet ef migrations add`). New indexes are added via SQL scripts + manual `__EFMigrationsHistory` entries.

---

## File Structure

| Action | File | Responsibility |
|--------|------|----------------|
| Modify | `IdealAkeWms/Services/PickingTransferService.cs` | Fix N+1 in DoTransferAsync + duplicate load in ApplyPickingSelectionsAsync |
| Modify | `IdealAkeWms/Data/ApplicationDbContext.cs` | Add missing indexes on StockMovements |
| Create | `IdealAkeWms/Data/Repositories/CachedSettingRepository.cs` | MemoryCache decorator for AppSettings (4 interface methods) |
| Create | `IdealAkeWms/Data/Repositories/CachedHolidayRepository.cs` | MemoryCache decorator for Holidays (IRepository + 2 custom methods) |
| Create | `IdealAkeWms/Data/Repositories/CachedOseonOperationConfigRepository.cs` | MemoryCache decorator for AG-Configs (9 interface methods) |
| Modify | `IdealAkeWms/Program.cs` | Register cached repos + EF command timeout |
| Create | `SQL/38_StockMovementPerformanceIndexes.sql` | SQL migration for new indexes |
| Modify | `SQL/00_FreshInstall.sql` | Add new indexes to fresh install script |
| Modify | `IdealAkeWms/Data/Repositories/OseonProductionOrderRepository.cs` | AsNoTracking for entity-returning read queries |
| Create | `IdealAkeWms.Tests/Services/PickingTransferServiceTests.cs` | Tests for batched transfer logic |
| Create | `IdealAkeWms.Tests/Repositories/CachedSettingRepositoryTests.cs` | Tests for cache decorator |

---

### Task 1: Fix N+1 in DoTransferAsync (PickingTransferService)

**Files:**
- Modify: `IdealAkeWms/Services/PickingTransferService.cs:148-253`
- Test: `IdealAkeWms.Tests/Services/PickingTransferServiceTests.cs`

This is the **highest-impact fix**. Currently, for N picked items, DoTransferAsync executes:
- N × `FirstOrDefaultAsync` for Article (line 174)
- N × `SumAsync` for destination stock (line 179)
- N × `SumAsync` for source stock (line 185)
- N × `GetValueAsync` for NegativeBuchungErlaubt setting (line 196) — each item!
- N × `FirstOrDefaultAsync` for NAN storage location on negative stock (line 205)

That's up to **5N + 1** DB calls in a single transaction. With 50 BOM items = **251 DB calls**.

The fix: batch-load all articles and pre-calculate all stock sums **before** the loop, then maintain a **running stock counter** to handle duplicate article+location pairs.

- [ ] **Step 1: Create test file with comprehensive scenarios**

Create file `IdealAkeWms.Tests/Services/PickingTransferServiceTests.cs`:

```csharp
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace IdealAkeWms.Tests.Services;

public class PickingTransferServiceTests
{
    private static (ApplicationDbContext ctx, PickingTransferService svc, StorageLocation source, StorageLocation target, ProductionOrder order) SetupTransferScenario(int itemCount, decimal stockPerArticle = 10m, decimal quantityPerItem = 2m)
    {
        var ctx = TestDbContextFactory.Create();

        var source = new StorageLocation { Code = "SRC", CreatedAt = DateTime.Now, CreatedBy = "Test", CreatedByWindows = "T\\u" };
        var target = new StorageLocation { Code = "TGT", CreatedAt = DateTime.Now, CreatedBy = "Test", CreatedByWindows = "T\\u" };
        var nan = new StorageLocation { Code = "NAN", CreatedAt = DateTime.Now, CreatedBy = "Test", CreatedByWindows = "T\\u" };
        ctx.StorageLocations.AddRange(source, target, nan);

        var order = new ProductionOrder
        {
            OrderNumber = "FA-001",
            ArticleNumber = "MAIN-001",
            CreatedAt = DateTime.Now,
            CreatedBy = "Test",
            CreatedByWindows = "T\\u"
        };
        ctx.ProductionOrders.Add(order);
        ctx.SaveChanges();

        for (int i = 1; i <= itemCount; i++)
        {
            var article = new Article
            {
                ArticleNumber = $"PART-{i:D3}",
                Description = $"Part {i}",
                CreatedAt = DateTime.Now,
                CreatedBy = "Test",
                CreatedByWindows = "T\\u"
            };
            ctx.Articles.Add(article);
            ctx.SaveChanges();

            ctx.StockMovements.Add(new StockMovement
            {
                ArticleId = article.Id,
                StorageLocationId = source.Id,
                Quantity = stockPerArticle,
                MovementType = MovementType.Einbuchung,
                Timestamp = DateTime.Now,
                WindowsUser = "T\\u",
                CreatedAt = DateTime.Now,
                CreatedBy = "Test",
                CreatedByWindows = "T\\u"
            });

            ctx.PickingItems.Add(new PickingItem
            {
                ProductionOrderId = order.Id,
                BomArticleNumber = $"PART-{i:D3}",
                BomPosition = $"{i}",
                Quantity = quantityPerItem,
                IsPicked = true,
                SourceStorageLocationId = source.Id,
                PickedAt = DateTime.Now,
                PickedBy = "Test",
                PickedByWindows = "T\\u",
                CreatedAt = DateTime.Now,
                CreatedBy = "Test",
                CreatedByWindows = "T\\u"
            });
        }
        ctx.SaveChanges();

        var settingRepo = new AppSettingRepository(ctx);
        var stockRepo = new StockMovementRepository(ctx);
        var logger = Mock.Of<ILogger<PickingTransferService>>();
        var svc = new PickingTransferService(ctx, stockRepo, settingRepo, logger);

        return (ctx, svc, source, target, order);
    }

    [Fact]
    public async Task TransferPickedItems_MultipleItems_AllTransferred()
    {
        var (ctx, svc, source, target, order) = SetupTransferScenario(10);

        var result = await svc.CheckAndTransferPickedItemsAsync(
            order.Id, target.Id, forceTransfer: false, selectedItems: null,
            appUserId: 1, displayName: "Test", windowsUser: "T\\u");

        result.Success.Should().BeTrue();
        result.TransferredCount.Should().Be(10);

        var items = ctx.PickingItems.Where(p => p.ProductionOrderId == order.Id).ToList();
        items.Should().AllSatisfy(p => p.IsTransferred.Should().BeTrue());
    }

    [Fact]
    public async Task TransferPickedItems_ArticleNotFound_SkipsItem()
    {
        var (ctx, svc, source, target, order) = SetupTransferScenario(1);

        var item = ctx.PickingItems.First();
        item.BomArticleNumber = "NONEXISTENT";
        ctx.SaveChanges();

        var result = await svc.CheckAndTransferPickedItemsAsync(
            order.Id, target.Id, forceTransfer: false, selectedItems: null,
            appUserId: 1, displayName: "Test", windowsUser: "T\\u");

        result.Success.Should().BeTrue();
        result.TransferredCount.Should().Be(0);
    }

    [Fact]
    public async Task TransferPickedItems_InsufficientStock_ThrowsWhenNegativeNotAllowed()
    {
        var (ctx, svc, source, target, order) = SetupTransferScenario(1, stockPerArticle: 5m, quantityPerItem: 10m);

        var act = () => svc.CheckAndTransferPickedItemsAsync(
            order.Id, target.Id, forceTransfer: false, selectedItems: null,
            appUserId: 1, displayName: "Test", windowsUser: "T\\u");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Nicht genügend Bestand*");
    }

    [Fact]
    public async Task TransferPickedItems_DuplicateArticleSameSource_DecrementsStockCorrectly()
    {
        // CRITICAL TEST: Same article at two BOM positions, same source location.
        // Stock = 15, Item1 needs 8, Item2 needs 8. Total = 16 > 15 → must throw!
        var ctx = TestDbContextFactory.Create();

        var source = new StorageLocation { Code = "SRC", CreatedAt = DateTime.Now, CreatedBy = "Test", CreatedByWindows = "T\\u" };
        var target = new StorageLocation { Code = "TGT", CreatedAt = DateTime.Now, CreatedBy = "Test", CreatedByWindows = "T\\u" };
        ctx.StorageLocations.AddRange(source, target);

        var order = new ProductionOrder
        {
            OrderNumber = "FA-002",
            ArticleNumber = "MAIN-002",
            CreatedAt = DateTime.Now,
            CreatedBy = "Test",
            CreatedByWindows = "T\\u"
        };
        ctx.ProductionOrders.Add(order);

        var article = new Article
        {
            ArticleNumber = "SHARED-001",
            Description = "Shared Part",
            CreatedAt = DateTime.Now,
            CreatedBy = "Test",
            CreatedByWindows = "T\\u"
        };
        ctx.Articles.Add(article);
        ctx.SaveChanges();

        // Stock = 15 at source
        ctx.StockMovements.Add(new StockMovement
        {
            ArticleId = article.Id,
            StorageLocationId = source.Id,
            Quantity = 15,
            MovementType = MovementType.Einbuchung,
            Timestamp = DateTime.Now,
            WindowsUser = "T\\u",
            CreatedAt = DateTime.Now,
            CreatedBy = "Test",
            CreatedByWindows = "T\\u"
        });

        // Two picking items for same article, same source, total > stock
        ctx.PickingItems.Add(new PickingItem
        {
            ProductionOrderId = order.Id,
            BomArticleNumber = "SHARED-001",
            BomPosition = "10",
            Quantity = 8,
            IsPicked = true,
            SourceStorageLocationId = source.Id,
            PickedAt = DateTime.Now,
            PickedBy = "Test",
            PickedByWindows = "T\\u",
            CreatedAt = DateTime.Now,
            CreatedBy = "Test",
            CreatedByWindows = "T\\u"
        });
        ctx.PickingItems.Add(new PickingItem
        {
            ProductionOrderId = order.Id,
            BomArticleNumber = "SHARED-001",
            BomPosition = "20",
            Quantity = 8,
            IsPicked = true,
            SourceStorageLocationId = source.Id,
            PickedAt = DateTime.Now,
            PickedBy = "Test",
            PickedByWindows = "T\\u",
            CreatedAt = DateTime.Now,
            CreatedBy = "Test",
            CreatedByWindows = "T\\u"
        });
        ctx.SaveChanges();

        var settingRepo = new AppSettingRepository(ctx);
        var stockRepo = new StockMovementRepository(ctx);
        var logger = Mock.Of<ILogger<PickingTransferService>>();
        var svc = new PickingTransferService(ctx, stockRepo, settingRepo, logger);

        var act = () => svc.CheckAndTransferPickedItemsAsync(
            order.Id, target.Id, forceTransfer: false, selectedItems: null,
            appUserId: 1, displayName: "Test", windowsUser: "T\\u");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Nicht genügend Bestand*SHARED-001*");
    }

    [Fact]
    public async Task TransferPickedItems_DuplicateArticleSameSource_BothFitInStock()
    {
        // Same article at two positions, same source, but total fits: Stock=20, 8+8=16 < 20
        var ctx = TestDbContextFactory.Create();

        var source = new StorageLocation { Code = "SRC", CreatedAt = DateTime.Now, CreatedBy = "Test", CreatedByWindows = "T\\u" };
        var target = new StorageLocation { Code = "TGT", CreatedAt = DateTime.Now, CreatedBy = "Test", CreatedByWindows = "T\\u" };
        ctx.StorageLocations.AddRange(source, target);

        var order = new ProductionOrder
        {
            OrderNumber = "FA-003",
            ArticleNumber = "MAIN-003",
            CreatedAt = DateTime.Now,
            CreatedBy = "Test",
            CreatedByWindows = "T\\u"
        };
        ctx.ProductionOrders.Add(order);

        var article = new Article
        {
            ArticleNumber = "SHARED-002",
            Description = "Shared Part 2",
            CreatedAt = DateTime.Now,
            CreatedBy = "Test",
            CreatedByWindows = "T\\u"
        };
        ctx.Articles.Add(article);
        ctx.SaveChanges();

        ctx.StockMovements.Add(new StockMovement
        {
            ArticleId = article.Id,
            StorageLocationId = source.Id,
            Quantity = 20,
            MovementType = MovementType.Einbuchung,
            Timestamp = DateTime.Now,
            WindowsUser = "T\\u",
            CreatedAt = DateTime.Now,
            CreatedBy = "Test",
            CreatedByWindows = "T\\u"
        });

        ctx.PickingItems.Add(new PickingItem
        {
            ProductionOrderId = order.Id,
            BomArticleNumber = "SHARED-002",
            BomPosition = "10",
            Quantity = 8,
            IsPicked = true,
            SourceStorageLocationId = source.Id,
            PickedAt = DateTime.Now,
            PickedBy = "Test",
            PickedByWindows = "T\\u",
            CreatedAt = DateTime.Now,
            CreatedBy = "Test",
            CreatedByWindows = "T\\u"
        });
        ctx.PickingItems.Add(new PickingItem
        {
            ProductionOrderId = order.Id,
            BomArticleNumber = "SHARED-002",
            BomPosition = "20",
            Quantity = 8,
            IsPicked = true,
            SourceStorageLocationId = source.Id,
            PickedAt = DateTime.Now,
            PickedBy = "Test",
            PickedByWindows = "T\\u",
            CreatedAt = DateTime.Now,
            CreatedBy = "Test",
            CreatedByWindows = "T\\u"
        });
        ctx.SaveChanges();

        var settingRepo = new AppSettingRepository(ctx);
        var stockRepo = new StockMovementRepository(ctx);
        var logger = Mock.Of<ILogger<PickingTransferService>>();
        var svc = new PickingTransferService(ctx, stockRepo, settingRepo, logger);

        var result = await svc.CheckAndTransferPickedItemsAsync(
            order.Id, target.Id, forceTransfer: false, selectedItems: null,
            appUserId: 1, displayName: "Test", windowsUser: "T\\u");

        result.Success.Should().BeTrue();
        result.TransferredCount.Should().Be(2);
    }

    [Fact]
    public async Task TransferPickedItems_SameArticleDifferentSources_CalculatesStockSeparately()
    {
        // Same article picked from two different source locations
        var ctx = TestDbContextFactory.Create();

        var srcA = new StorageLocation { Code = "SRC-A", CreatedAt = DateTime.Now, CreatedBy = "Test", CreatedByWindows = "T\\u" };
        var srcB = new StorageLocation { Code = "SRC-B", CreatedAt = DateTime.Now, CreatedBy = "Test", CreatedByWindows = "T\\u" };
        var target = new StorageLocation { Code = "TGT", CreatedAt = DateTime.Now, CreatedBy = "Test", CreatedByWindows = "T\\u" };
        ctx.StorageLocations.AddRange(srcA, srcB, target);

        var order = new ProductionOrder
        {
            OrderNumber = "FA-004",
            ArticleNumber = "MAIN-004",
            CreatedAt = DateTime.Now,
            CreatedBy = "Test",
            CreatedByWindows = "T\\u"
        };
        ctx.ProductionOrders.Add(order);

        var article = new Article
        {
            ArticleNumber = "MULTI-SRC",
            Description = "Multi-source Part",
            CreatedAt = DateTime.Now,
            CreatedBy = "Test",
            CreatedByWindows = "T\\u"
        };
        ctx.Articles.Add(article);
        ctx.SaveChanges();

        // 5 at SRC-A, 5 at SRC-B
        ctx.StockMovements.Add(new StockMovement
        {
            ArticleId = article.Id, StorageLocationId = srcA.Id, Quantity = 5,
            MovementType = MovementType.Einbuchung, Timestamp = DateTime.Now,
            WindowsUser = "T\\u", CreatedAt = DateTime.Now, CreatedBy = "Test", CreatedByWindows = "T\\u"
        });
        ctx.StockMovements.Add(new StockMovement
        {
            ArticleId = article.Id, StorageLocationId = srcB.Id, Quantity = 5,
            MovementType = MovementType.Einbuchung, Timestamp = DateTime.Now,
            WindowsUser = "T\\u", CreatedAt = DateTime.Now, CreatedBy = "Test", CreatedByWindows = "T\\u"
        });

        // Pick 3 from A, 3 from B — both fit
        ctx.PickingItems.Add(new PickingItem
        {
            ProductionOrderId = order.Id, BomArticleNumber = "MULTI-SRC", BomPosition = "10",
            Quantity = 3, IsPicked = true, SourceStorageLocationId = srcA.Id,
            PickedAt = DateTime.Now, PickedBy = "Test", PickedByWindows = "T\\u",
            CreatedAt = DateTime.Now, CreatedBy = "Test", CreatedByWindows = "T\\u"
        });
        ctx.PickingItems.Add(new PickingItem
        {
            ProductionOrderId = order.Id, BomArticleNumber = "MULTI-SRC", BomPosition = "20",
            Quantity = 3, IsPicked = true, SourceStorageLocationId = srcB.Id,
            PickedAt = DateTime.Now, PickedBy = "Test", PickedByWindows = "T\\u",
            CreatedAt = DateTime.Now, CreatedBy = "Test", CreatedByWindows = "T\\u"
        });
        ctx.SaveChanges();

        var settingRepo = new AppSettingRepository(ctx);
        var stockRepo = new StockMovementRepository(ctx);
        var logger = Mock.Of<ILogger<PickingTransferService>>();
        var svc = new PickingTransferService(ctx, stockRepo, settingRepo, logger);

        var result = await svc.CheckAndTransferPickedItemsAsync(
            order.Id, target.Id, forceTransfer: false, selectedItems: null,
            appUserId: 1, displayName: "Test", windowsUser: "T\\u");

        result.Success.Should().BeTrue();
        result.TransferredCount.Should().Be(2);
    }

    [Fact]
    public async Task TransferPickedItems_NegativeAllowed_UsesNanLocation()
    {
        var (ctx, svc, source, target, order) = SetupTransferScenario(1, stockPerArticle: 1m, quantityPerItem: 5m);

        // Enable negative booking + ensure NAN location exists
        ctx.AppSettings.Add(new AppSetting { Key = "NegativeBuchungErlaubt", Value = "true" });
        ctx.AppSettings.Add(new AppSetting { Key = "NegativeBuchungLagerplatz", Value = "NAN" });
        ctx.SaveChanges();

        var result = await svc.CheckAndTransferPickedItemsAsync(
            order.Id, target.Id, forceTransfer: false, selectedItems: null,
            appUserId: 1, displayName: "Test", windowsUser: "T\\u");

        result.Success.Should().BeTrue();
        result.TransferredCount.Should().Be(1);

        // Verify the movement uses NAN as source, not the original source
        var nanLoc = ctx.StorageLocations.First(sl => sl.Code == "NAN");
        var movement = ctx.StockMovements.First(sm => sm.MovementType == MovementType.Umbuchung);
        movement.SourceStorageLocationId.Should().Be(nanLoc.Id);
    }

    [Fact]
    public async Task TransferPickedItems_NoPickedItems_Throws()
    {
        var (ctx, svc, source, target, order) = SetupTransferScenario(0);

        // Add one unpicked item
        ctx.PickingItems.Add(new PickingItem
        {
            ProductionOrderId = order.Id,
            BomArticleNumber = "X",
            BomPosition = "1",
            Quantity = 1,
            IsPicked = false,
            CreatedAt = DateTime.Now,
            CreatedBy = "Test",
            CreatedByWindows = "T\\u"
        });
        ctx.SaveChanges();

        var act = () => svc.CheckAndTransferPickedItemsAsync(
            order.Id, target.Id, forceTransfer: false, selectedItems: null,
            appUserId: 1, displayName: "Test", windowsUser: "T\\u");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Keine gepickten Artikel*");
    }
}
```

- [ ] **Step 2: Run tests to establish baseline**

Run: `dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "FullyQualifiedName~PickingTransferServiceTests" -v n`

Expected: Most tests PASS with current code, BUT `DuplicateArticleSameSource_DecrementsStockCorrectly` may PASS incorrectly (the current code also doesn't decrement per-iteration — it reads stale DB too, but the DB doesn't change within the transaction so the bug exists in the current code too). This test documents the **desired behavior** of the new code.

- [ ] **Step 3: Refactor DoTransferAsync with batch-load + running stock counter**

Replace the entire `DoTransferAsync` method in `IdealAkeWms/Services/PickingTransferService.cs:148-253`:

```csharp
    private async Task<int> DoTransferAsync(
        int productionOrderId,
        int targetStorageLocationId,
        string? productionOrder,
        int? appUserId,
        string displayName,
        string windowsUser)
    {
        var pickedItems = await _context.PickingItems
            .Where(p => p.ProductionOrderId == productionOrderId
                     && p.IsPicked && !p.IsTransferred)
            .ToListAsync();

        if (!pickedItems.Any())
            throw new InvalidOperationException("Keine gepickten Artikel zum Umbuchen vorhanden.");

        // --- Batch-load all needed data BEFORE the loop ---

        // 1. Batch-load articles as dictionary (1 query instead of N)
        var articleNumbers = pickedItems
            .Select(p => p.BomArticleNumber)
            .Where(a => !string.IsNullOrEmpty(a))
            .Distinct()
            .ToList();

        var articlesByNumber = await _context.Articles
            .Where(a => articleNumbers.Contains(a.ArticleNumber))
            .ToDictionaryAsync(a => a.ArticleNumber);

        // 2. Collect all unique source location IDs
        var sourceLocationIds = pickedItems
            .Where(p => p.SourceStorageLocationId.HasValue)
            .Select(p => p.SourceStorageLocationId!.Value)
            .Distinct()
            .ToList();

        // 3. Batch-load article IDs for stock calculation
        var articleIds = articlesByNumber.Values.Select(a => a.Id).ToList();

        // 4. Pre-calculate ALL stock sums in 2 queries (instead of 2N)
        var destStockRaw = await _context.StockMovements
            .Where(sm => articleIds.Contains(sm.ArticleId)
                      && sourceLocationIds.Contains(sm.StorageLocationId))
            .GroupBy(sm => new { sm.ArticleId, sm.StorageLocationId })
            .Select(g => new
            {
                g.Key.ArticleId,
                g.Key.StorageLocationId,
                Sum = g.Sum(sm =>
                    sm.MovementType == MovementType.Einbuchung ? sm.Quantity :
                    sm.MovementType == MovementType.Umbuchung ? sm.Quantity :
                    -sm.Quantity)
            })
            .ToListAsync();

        var srcStockRaw = await _context.StockMovements
            .Where(sm => sm.MovementType == MovementType.Umbuchung
                      && sm.SourceStorageLocationId != null
                      && articleIds.Contains(sm.ArticleId)
                      && sourceLocationIds.Contains(sm.SourceStorageLocationId!.Value))
            .GroupBy(sm => new { sm.ArticleId, LocId = sm.SourceStorageLocationId!.Value })
            .Select(g => new
            {
                g.Key.ArticleId,
                g.Key.LocId,
                Sum = g.Sum(sm => sm.Quantity)
            })
            .ToListAsync();

        // Build running stock lookup: (articleId, locationId) -> currentStock
        var stockLookup = new Dictionary<(int articleId, int locationId), decimal>();
        foreach (var d in destStockRaw)
            stockLookup[(d.ArticleId, d.StorageLocationId)] = d.Sum;
        foreach (var s in srcStockRaw)
        {
            var key = (s.ArticleId, s.LocId);
            if (stockLookup.ContainsKey(key))
                stockLookup[key] -= s.Sum;
            else
                stockLookup[key] = -s.Sum;
        }

        // 5. Pre-load negative booking settings (1-3 queries instead of up to 2N)
        var negativErlaubt = (await _settingRepository.GetValueAsync("NegativeBuchungErlaubt"))
            ?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
        StorageLocation? negativLagerplatz = null;
        if (negativErlaubt)
        {
            var negativLagerplatzCode = await _settingRepository.GetValueAsync("NegativeBuchungLagerplatz") ?? "NAN";
            negativLagerplatz = await _context.StorageLocations
                .FirstOrDefaultAsync(sl => sl.Code == negativLagerplatzCode);
        }

        // --- Now loop WITHOUT any DB calls, using running stock counter ---

        var now = DateTime.Now;
        var transferredCount = 0;

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            foreach (var item in pickedItems)
            {
                if (!item.SourceStorageLocationId.HasValue) continue;

                if (!articlesByNumber.TryGetValue(item.BomArticleNumber, out var article))
                    continue;

                var sourceLocationId = item.SourceStorageLocationId.Value;
                var stockKey = (article.Id, sourceLocationId);
                stockLookup.TryGetValue(stockKey, out var currentStock);

                if (currentStock < item.Quantity)
                {
                    if (!negativErlaubt)
                    {
                        throw new InvalidOperationException(
                            $"Nicht genügend Bestand für {item.BomArticleNumber} am Quell-Lagerplatz. " +
                            $"Verfügbar: {currentStock:N3}, Benötigt: {item.Quantity:N3}");
                    }

                    if (negativLagerplatz != null)
                        sourceLocationId = negativLagerplatz.Id;

                    _logger.LogWarning(
                        "Bestand nicht verfügbar für {Article} (Verfügbar: {Stock}, Benötigt: {Needed}). Buche vom Lagerplatz {Location}.",
                        item.BomArticleNumber, currentStock, item.Quantity, negativLagerplatz?.Code ?? "NAN");
                }

                // CRITICAL: Decrement running stock counter to prevent stale-stock bug
                // when multiple items reference the same article + source location
                if (stockLookup.ContainsKey(stockKey))
                    stockLookup[stockKey] -= item.Quantity;

                _context.StockMovements.Add(new StockMovement
                {
                    ArticleId = article.Id,
                    Quantity = item.Quantity,
                    StorageLocationId = targetStorageLocationId,
                    SourceStorageLocationId = sourceLocationId,
                    ProductionOrder = productionOrder,
                    MovementType = MovementType.Umbuchung,
                    Timestamp = now,
                    UserId = appUserId,
                    WindowsUser = windowsUser,
                    CreatedAt = now,
                    CreatedBy = displayName,
                    CreatedByWindows = windowsUser
                });

                item.IsTransferred = true;
                item.TransferredAt = now;
                transferredCount++;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation(
                "TransferPicked: {Count} Artikel umgebucht. ProductionOrder: {ProductionOrder}",
                transferredCount, productionOrder);

            return transferredCount;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex,
                "TransferPicked fehlgeschlagen für FA-Id {OrderId}. Transaktion zurückgerollt.",
                productionOrderId);
            throw;
        }
    }
```

**Query reduction:** From `5N + 1` to **constant ~7 queries** regardless of item count.
**Correctness:** Running stock counter ensures duplicate article+location pairs are handled correctly.

- [ ] **Step 4: Run tests to verify refactor is correct**

Run: `dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "FullyQualifiedName~PickingTransferServiceTests" -v n`

Expected: All 8 tests PASS

- [ ] **Step 5: Fix duplicate loading in ApplyPickingSelectionsAsync**

In `IdealAkeWms/Services/PickingTransferService.cs`, replace the `ApplyPickingSelectionsAsync` method (lines 90-135):

```csharp
    private async Task ApplyPickingSelectionsAsync(List<PickingSelectionItem> items, string userName, string windowsUser)
    {
        var now = DateTime.Now;

        if (items.Count == 0) return;

        var itemIds = items.Select(i => i.PickingItemId).ToHashSet();

        // Load ALL non-transferred items for this order in ONE query (covers both reset + selection)
        var firstItemId = items.First().PickingItemId;
        var productionOrderId = await _context.PickingItems
            .Where(p => p.Id == firstItemId)
            .Select(p => p.ProductionOrderId)
            .FirstOrDefaultAsync();

        var allOrderItems = await _context.PickingItems
            .Where(p => p.ProductionOrderId == productionOrderId && !p.IsTransferred)
            .ToListAsync();

        // Reset all, then mark selected
        var selectionMap = items.ToDictionary(i => i.PickingItemId);
        foreach (var item in allOrderItems)
        {
            if (itemIds.Contains(item.Id) && selectionMap.TryGetValue(item.Id, out var selection))
            {
                item.IsPicked = true;
                item.IsBaugruppe = selection.IsBaugruppe;
                item.PickedAt = now;
                item.PickedBy = userName;
                item.PickedByWindows = windowsUser;
                item.SourceStorageLocationId = selection.SourceStorageLocationId;
                item.ModifiedAt = now;
                item.ModifiedBy = userName;
                item.ModifiedByWindows = windowsUser;
            }
            else
            {
                item.IsPicked = false;
                item.PickedAt = null;
                item.PickedBy = null;
                item.PickedByWindows = null;
                item.SourceStorageLocationId = null;
            }
        }

        await _context.SaveChangesAsync();
    }
```

- [ ] **Step 6: Run all tests**

Run: `dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj -v n`

Expected: All tests PASS

- [ ] **Step 7: Commit**

```bash
git add IdealAkeWms/Services/PickingTransferService.cs IdealAkeWms.Tests/Services/PickingTransferServiceTests.cs
git commit -m "perf: fix N+1 queries in PickingTransferService — batch-load + running stock counter

DoTransferAsync reduced from 5N+1 to ~7 constant DB queries.
Running stock counter prevents stale-stock bug with duplicate article+location pairs.
ApplyPickingSelectionsAsync reduced from 3 to 2 queries.
8 test scenarios including duplicate articles, negative booking, multi-source."
```

---

### Task 2: Add Missing Database Indexes for StockMovement Queries

**Files:**
- Modify: `IdealAkeWms/Data/ApplicationDbContext.cs:175-212`
- Create: `SQL/38_StockMovementPerformanceIndexes.sql`
- Modify: `SQL/00_FreshInstall.sql`

The `StockMovements` table is the most-queried table. Current indexes cover individual FK columns but miss:
- Composite `(ArticleId, StorageLocationId)` for stock calculations — used in every BOM view and stock overview
- Composite `(ArticleId, SourceStorageLocationId, MovementType)` for source deduction queries
- `ProductionOrder` column — used in FA-filter, movement history, transfer

- [ ] **Step 1: Add indexes to ApplicationDbContext.OnModelCreating**

In `IdealAkeWms/Data/ApplicationDbContext.cs`, after line 211 (`entity.HasIndex(e => e.SourceStorageLocationId);`), add:

```csharp
            // Performance: Stock calculations always group by (ArticleId, StorageLocationId)
            entity.HasIndex(e => new { e.ArticleId, e.StorageLocationId })
                .HasDatabaseName("IX_StockMovements_ArticleId_StorageLocationId");

            // Performance: Source deduction queries filter on all three columns
            entity.HasIndex(e => new { e.ArticleId, e.SourceStorageLocationId, e.MovementType })
                .HasDatabaseName("IX_StockMovements_ArticleId_SourceStorageLocationId_MovementType");

            // Performance: FA-filter in Bestandsuebersicht and Bewegungshistorie
            entity.HasIndex(e => e.ProductionOrder)
                .HasDatabaseName("IX_StockMovements_ProductionOrder");
```

- [ ] **Step 2: Create SQL migration script**

Create file `SQL/38_StockMovementPerformanceIndexes.sql`:

```sql
-- Performance-Indexes fuer StockMovements
-- Optimiert die haeufigsten Abfrage-Patterns: Bestandsberechnung, FA-Filter, Umbuchungs-Quell-Abfragen

-- Composite Index: Bestandsberechnung (GetCurrentStockAsync, GetStockByArticleNumbersAsync)
-- Queries gruppieren immer nach (ArticleId, StorageLocationId)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StockMovements_ArticleId_StorageLocationId' AND object_id = OBJECT_ID('StockMovements'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_StockMovements_ArticleId_StorageLocationId]
    ON [StockMovements] ([ArticleId], [StorageLocationId])
    INCLUDE ([Quantity], [MovementType])
END
GO

-- Composite Index: Umbuchungs-Quell-Abfragen (source deduction in stock calculation)
-- Queries filtern auf (ArticleId, SourceStorageLocationId, MovementType=Umbuchung)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StockMovements_ArticleId_SourceStorageLocationId_MovementType' AND object_id = OBJECT_ID('StockMovements'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_StockMovements_ArticleId_SourceStorageLocationId_MovementType]
    ON [StockMovements] ([ArticleId], [SourceStorageLocationId], [MovementType])
    INCLUDE ([Quantity])
END
GO

-- Index: FA-Nummer fuer Bestandsuebersicht FA-Filter und Bewegungshistorie
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StockMovements_ProductionOrder' AND object_id = OBJECT_ID('StockMovements'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_StockMovements_ProductionOrder]
    ON [StockMovements] ([ProductionOrder])
END
GO

-- EF Migrations History
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260404000001_StockMovementPerformanceIndexes')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260404000001_StockMovementPerformanceIndexes', '10.0.0')
END
GO
```

- [ ] **Step 3: Add indexes to FreshInstall script**

In `SQL/00_FreshInstall.sql`, add before the `-- Standard-Daten` section:

```sql
-- Performance-Indexes StockMovements (38)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StockMovements_ArticleId_StorageLocationId' AND object_id = OBJECT_ID('StockMovements'))
    CREATE NONCLUSTERED INDEX [IX_StockMovements_ArticleId_StorageLocationId]
    ON [StockMovements] ([ArticleId], [StorageLocationId]) INCLUDE ([Quantity], [MovementType]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StockMovements_ArticleId_SourceStorageLocationId_MovementType' AND object_id = OBJECT_ID('StockMovements'))
    CREATE NONCLUSTERED INDEX [IX_StockMovements_ArticleId_SourceStorageLocationId_MovementType]
    ON [StockMovements] ([ArticleId], [SourceStorageLocationId], [MovementType]) INCLUDE ([Quantity]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StockMovements_ProductionOrder' AND object_id = OBJECT_ID('StockMovements'))
    CREATE NONCLUSTERED INDEX [IX_StockMovements_ProductionOrder]
    ON [StockMovements] ([ProductionOrder]);
```

- [ ] **Step 4: Build to verify**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj --no-restore -v q`

Expected: Build succeeded, 0 errors

- [ ] **Step 5: Commit**

```bash
git add IdealAkeWms/Data/ApplicationDbContext.cs SQL/38_StockMovementPerformanceIndexes.sql SQL/00_FreshInstall.sql
git commit -m "perf: add composite indexes on StockMovements for stock calculations and FA-filter

- IX_StockMovements_ArticleId_StorageLocationId INCLUDE (Quantity, MovementType)
- IX_StockMovements_ArticleId_SourceStorageLocationId_MovementType INCLUDE (Quantity)
- IX_StockMovements_ProductionOrder"
```

---

### Task 3: Cache AppSettings with MemoryCache Decorator

**Files:**
- Create: `IdealAkeWms/Data/Repositories/CachedSettingRepository.cs`
- Modify: `IdealAkeWms/Program.cs`
- Test: `IdealAkeWms.Tests/Repositories/CachedSettingRepositoryTests.cs`

IAppSettingRepository has exactly 4 methods: `GetAllAsync`, `GetValueAsync`, `GetIntValueAsync`, `SetValueAsync`.

- [ ] **Step 1: Create CachedSettingRepository**

Create file `IdealAkeWms/Data/Repositories/CachedSettingRepository.cs`:

```csharp
using IdealAkeWms.Models;
using Microsoft.Extensions.Caching.Memory;

namespace IdealAkeWms.Data.Repositories;

public class CachedSettingRepository : IAppSettingRepository
{
    private readonly AppSettingRepository _inner;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(2);
    private const string CachePrefix = "setting:";

    public CachedSettingRepository(AppSettingRepository inner, IMemoryCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public async Task<string?> GetValueAsync(string key)
    {
        var cacheKey = CachePrefix + key;
        if (_cache.TryGetValue(cacheKey, out string? cached))
            return cached;

        var value = await _inner.GetValueAsync(key);
        _cache.Set(cacheKey, value, CacheDuration);
        return value;
    }

    public async Task<int> GetIntValueAsync(string key, int defaultValue)
    {
        var value = await GetValueAsync(key);
        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    public async Task SetValueAsync(string key, string value)
    {
        await _inner.SetValueAsync(key, value);
        _cache.Remove(CachePrefix + key);
    }

    public Task<List<AppSetting>> GetAllAsync() => _inner.GetAllAsync();
}
```

- [ ] **Step 2: Write tests**

Create file `IdealAkeWms.Tests/Repositories/CachedSettingRepositoryTests.cs`:

```csharp
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;

namespace IdealAkeWms.Tests.Repositories;

public class CachedSettingRepositoryTests
{
    [Fact]
    public async Task GetValueAsync_ReturnsCachedValue_OnSecondCall()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.AppSettings.Add(new AppSetting { Key = "TestKey", Value = "TestValue" });
        ctx.SaveChanges();

        var inner = new AppSettingRepository(ctx);
        var cache = new MemoryCache(new MemoryCacheOptions());
        var cached = new CachedSettingRepository(inner, cache);

        var value1 = await cached.GetValueAsync("TestKey");
        value1.Should().Be("TestValue");

        // Change value directly in DB (bypassing cache)
        var setting = ctx.AppSettings.First(s => s.Key == "TestKey");
        setting.Value = "Changed";
        ctx.SaveChanges();

        // Should still return cached value
        var value2 = await cached.GetValueAsync("TestKey");
        value2.Should().Be("TestValue");
    }

    [Fact]
    public async Task SetValueAsync_InvalidatesCache()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.AppSettings.Add(new AppSetting { Key = "TestKey", Value = "OldValue" });
        ctx.SaveChanges();

        var inner = new AppSettingRepository(ctx);
        var cache = new MemoryCache(new MemoryCacheOptions());
        var cached = new CachedSettingRepository(inner, cache);

        await cached.GetValueAsync("TestKey"); // populate cache
        await cached.SetValueAsync("TestKey", "NewValue");

        var value = await cached.GetValueAsync("TestKey");
        value.Should().Be("NewValue");
    }

    [Fact]
    public async Task GetIntValueAsync_ReturnsDefault_WhenKeyMissing()
    {
        using var ctx = TestDbContextFactory.Create();
        var inner = new AppSettingRepository(ctx);
        var cache = new MemoryCache(new MemoryCacheOptions());
        var cached = new CachedSettingRepository(inner, cache);

        var value = await cached.GetIntValueAsync("NonExistent", 42);
        value.Should().Be(42);
    }

    [Fact]
    public async Task GetValueAsync_CachesNullValues()
    {
        // Important: null must be cached too, otherwise missing keys hit DB every time
        using var ctx = TestDbContextFactory.Create();
        var inner = new AppSettingRepository(ctx);
        var cache = new MemoryCache(new MemoryCacheOptions());
        var cached = new CachedSettingRepository(inner, cache);

        var value1 = await cached.GetValueAsync("Missing");
        value1.Should().BeNull();

        // Add value directly to DB
        ctx.AppSettings.Add(new AppSetting { Key = "Missing", Value = "NowExists" });
        ctx.SaveChanges();

        // Should still return null (cached)
        var value2 = await cached.GetValueAsync("Missing");
        value2.Should().BeNull();
    }
}
```

- [ ] **Step 3: Run tests**

Run: `dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "FullyQualifiedName~CachedSettingRepositoryTests" -v n`

Expected: All 4 tests PASS

- [ ] **Step 4: Register in Program.cs**

In `IdealAkeWms/Program.cs`, replace line 44:
```csharp
builder.Services.AddScoped<IAppSettingRepository, AppSettingRepository>();
```
With:
```csharp
builder.Services.AddScoped<AppSettingRepository>();
builder.Services.AddScoped<IAppSettingRepository, CachedSettingRepository>();
```

- [ ] **Step 5: Run all tests, commit**

Run: `dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj -v n`

```bash
git add IdealAkeWms/Data/Repositories/CachedSettingRepository.cs IdealAkeWms.Tests/Repositories/CachedSettingRepositoryTests.cs IdealAkeWms/Program.cs
git commit -m "perf: add MemoryCache decorator for AppSettings (2-min TTL, write-through)

Eliminates 5-10 DB calls per page load for frequently-read settings.
Correctly caches null values to prevent repeated misses."
```

---

### Task 4: Cache Holidays and OseonOperationConfig

**Files:**
- Create: `IdealAkeWms/Data/Repositories/CachedHolidayRepository.cs`
- Create: `IdealAkeWms/Data/Repositories/CachedOseonOperationConfigRepository.cs`
- Modify: `IdealAkeWms/Program.cs`

IHolidayRepository extends `IRepository<Holiday>` (6 base methods) + 2 custom = 8 methods.
IOseonOperationConfigRepository has 9 methods.

- [ ] **Step 1: Create CachedHolidayRepository**

Create file `IdealAkeWms/Data/Repositories/CachedHolidayRepository.cs`:

```csharp
using System.Linq.Expressions;
using IdealAkeWms.Models;
using Microsoft.Extensions.Caching.Memory;

namespace IdealAkeWms.Data.Repositories;

public class CachedHolidayRepository : IHolidayRepository
{
    private readonly HolidayRepository _inner;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);
    private const string DatesKey = "holidays:dates";
    private const string AllKey = "holidays:all";
    private const string AllOrderedKey = "holidays:allOrdered";

    public CachedHolidayRepository(HolidayRepository inner, IMemoryCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public async Task<HashSet<DateTime>> GetHolidayDatesAsync()
    {
        if (_cache.TryGetValue(DatesKey, out HashSet<DateTime>? cached))
            return cached!;

        var dates = await _inner.GetHolidayDatesAsync();
        _cache.Set(DatesKey, dates, CacheDuration);
        return dates;
    }

    public async Task<List<Holiday>> GetAllOrderedAsync()
    {
        if (_cache.TryGetValue(AllOrderedKey, out List<Holiday>? cached))
            return cached!;

        var all = await _inner.GetAllOrderedAsync();
        _cache.Set(AllOrderedKey, all, CacheDuration);
        return all;
    }

    public async Task<List<Holiday>> GetAllAsync()
    {
        if (_cache.TryGetValue(AllKey, out List<Holiday>? cached))
            return cached!;

        var all = await _inner.GetAllAsync();
        _cache.Set(AllKey, all, CacheDuration);
        return all;
    }

    private void InvalidateCache()
    {
        _cache.Remove(DatesKey);
        _cache.Remove(AllKey);
        _cache.Remove(AllOrderedKey);
    }

    public async Task<Holiday> AddAsync(Holiday entity) { InvalidateCache(); return await _inner.AddAsync(entity); }
    public Task UpdateAsync(Holiday entity) { InvalidateCache(); return _inner.UpdateAsync(entity); }
    public Task DeleteAsync(int id) { InvalidateCache(); return _inner.DeleteAsync(id); }
    public Task<Holiday?> GetByIdAsync(int id) => _inner.GetByIdAsync(id);
    public Task<List<Holiday>> FindAsync(Expression<Func<Holiday, bool>> predicate) => _inner.FindAsync(predicate);
}
```

- [ ] **Step 2: Create CachedOseonOperationConfigRepository**

Create file `IdealAkeWms/Data/Repositories/CachedOseonOperationConfigRepository.cs`:

```csharp
using IdealAkeWms.Models;
using Microsoft.Extensions.Caching.Memory;

namespace IdealAkeWms.Data.Repositories;

public class CachedOseonOperationConfigRepository : IOseonOperationConfigRepository
{
    private readonly OseonOperationConfigRepository _inner;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);
    private const string DictKey = "oseonOpConfig:dict";
    private const string AllKey = "oseonOpConfig:all";

    public CachedOseonOperationConfigRepository(OseonOperationConfigRepository inner, IMemoryCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public async Task<Dictionary<string, OseonOperationConfig>> GetAllAsDictionaryAsync()
    {
        if (_cache.TryGetValue(DictKey, out Dictionary<string, OseonOperationConfig>? cached))
            return cached!;

        var dict = await _inner.GetAllAsDictionaryAsync();
        _cache.Set(DictKey, dict, CacheDuration);
        return dict;
    }

    public async Task<List<OseonOperationConfig>> GetAllAsync()
    {
        if (_cache.TryGetValue(AllKey, out List<OseonOperationConfig>? cached))
            return cached!;

        var all = await _inner.GetAllAsync();
        _cache.Set(AllKey, all, CacheDuration);
        return all;
    }

    private void InvalidateCache()
    {
        _cache.Remove(DictKey);
        _cache.Remove(AllKey);
    }

    public Task AddAsync(OseonOperationConfig config) { InvalidateCache(); return _inner.AddAsync(config); }
    public Task UpdateAsync(OseonOperationConfig config) { InvalidateCache(); return _inner.UpdateAsync(config); }
    public Task DeleteAsync(int id) { InvalidateCache(); return _inner.DeleteAsync(id); }
    public Task<OseonOperationConfig?> GetByIdAsync(int id) => _inner.GetByIdAsync(id);
    public Task<OseonOperationConfig?> GetByNameAsync(string operationName) => _inner.GetByNameAsync(operationName);
    public Task<List<string>> GetUnconfiguredOperationNamesAsync() => _inner.GetUnconfiguredOperationNamesAsync();
    public Task<bool> ExistsAsync(string operationName) => _inner.ExistsAsync(operationName);
}
```

- [ ] **Step 3: Register in Program.cs**

In `IdealAkeWms/Program.cs`, replace line 46:
```csharp
builder.Services.AddScoped<IHolidayRepository, HolidayRepository>();
```
With:
```csharp
builder.Services.AddScoped<HolidayRepository>();
builder.Services.AddScoped<IHolidayRepository, CachedHolidayRepository>();
```

Replace line 52:
```csharp
builder.Services.AddScoped<IOseonOperationConfigRepository, OseonOperationConfigRepository>();
```
With:
```csharp
builder.Services.AddScoped<OseonOperationConfigRepository>();
builder.Services.AddScoped<IOseonOperationConfigRepository, CachedOseonOperationConfigRepository>();
```

- [ ] **Step 4: Run all tests, commit**

Run: `dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj -v n`

Expected: All tests PASS (existing OseonOperationConfigRepository tests instantiate the concrete class directly, so DI changes don't affect them)

```bash
git add IdealAkeWms/Data/Repositories/CachedHolidayRepository.cs IdealAkeWms/Data/Repositories/CachedOseonOperationConfigRepository.cs IdealAkeWms/Program.cs
git commit -m "perf: add MemoryCache decorators for Holidays (10-min) and OseonOperationConfig (10-min)

Eliminates 2-3 DB calls per OSEON page load and Kommissionierliste request.
Write-through invalidation on add/update/delete.
IHolidayRepository: all 8 methods (IRepository<Holiday> + custom) implemented.
IOseonOperationConfigRepository: all 9 methods implemented."
```

---

### Task 5: Add EF Command Timeout + AsNoTracking for Entity Queries

**Files:**
- Modify: `IdealAkeWms/Program.cs:17-18`
- Modify: `IdealAkeWms/Data/Repositories/OseonProductionOrderRepository.cs`

Two changes bundled: (1) command timeout, (2) AsNoTracking on entity-returning read-only queries.

Note: AsNoTracking is only added to queries returning **entity objects** (not projections). `GetCurrentStockAsync`, `GetStockByArticleNumbersAsync`, `GetMovementHistoryAsync` already use `.Select()` projections where EF skips tracking automatically.

- [ ] **Step 1: Add command timeout to EF configuration**

In `IdealAkeWms/Program.cs`, replace lines 17-18:
```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
```
With:
```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.CommandTimeout(120)));
```

- [ ] **Step 2: Add AsNoTracking to OseonProductionOrderRepository entity queries**

In `IdealAkeWms/Data/Repositories/OseonProductionOrderRepository.cs`, in the `GetPagedAsync` method, change the items query from:
```csharp
            var items = await _dbSet
                .Include(o => o.WorkOperations)
```
To:
```csharp
            var items = await _dbSet
                .AsNoTracking()
                .Include(o => o.WorkOperations)
```

In `GetAllWithOperationsAsync`, change from:
```csharp
        return await _dbSet
            .Include(o => o.WorkOperations)
```
To:
```csharp
        return await _dbSet
            .AsNoTracking()
            .Include(o => o.WorkOperations)
```

- [ ] **Step 3: Run all tests**

Run: `dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj -v n`

Expected: All tests PASS

- [ ] **Step 4: Commit**

```bash
git add IdealAkeWms/Program.cs IdealAkeWms/Data/Repositories/OseonProductionOrderRepository.cs
git commit -m "perf: set EF command timeout to 120s + AsNoTracking for OSEON read queries

Command timeout prevents errors on complex stock calculations (was 30s default).
AsNoTracking on GetPagedAsync/GetAllWithOperationsAsync reduces memory for OSEON pages."
```

---

## Summary of Impact

| Optimization | Before | After | Impact |
|-------------|--------|-------|--------|
| **DoTransferAsync (50 items)** | 251 DB calls | ~7 DB calls | **97% reduction** |
| **Stale-stock bug** | Silent over-transfer possible | Running counter prevents it | **Correctness fix** |
| **ApplyPickingSelectionsAsync** | 3 DB calls | 2 DB calls | 33% reduction |
| **OSEON page load** | 8 DB calls | ~3 DB calls (+cached) | **63% reduction** |
| **BOM page load (settings)** | 5-10 setting reads | 0-2 (cached) | **80% reduction** |
| **Stock calculation queries** | Individual FK indexes | Composite covering indexes | **Index seeks** |
| **FA-filter in Bestandsuebersicht** | No index on ProductionOrder | Index seek | **Significant** |
| **OSEON entity queries** | Full change tracking | AsNoTracking | **~15-30% faster** |
| **Long query timeout** | 30s default | 120s configured | **Prevents timeout errors** |

## Test Coverage

| Test | Scenario | Purpose |
|------|----------|---------|
| `TransferPickedItems_MultipleItems_AllTransferred` | 10 unique items | Happy path, batch load works |
| `TransferPickedItems_ArticleNotFound_SkipsItem` | Missing article | Graceful skip |
| `TransferPickedItems_InsufficientStock_Throws` | Qty > stock | Error handling |
| `TransferPickedItems_DuplicateArticleSameSource_DecrementsStockCorrectly` | Same art+loc, total > stock | **Running counter** |
| `TransferPickedItems_DuplicateArticleSameSource_BothFitInStock` | Same art+loc, total < stock | Happy path for dupes |
| `TransferPickedItems_SameArticleDifferentSources_CalculatesStockSeparately` | Same art, diff locations | Location isolation |
| `TransferPickedItems_NegativeAllowed_UsesNanLocation` | NegativeBuchungErlaubt=true | NAN fallback |
| `TransferPickedItems_NoPickedItems_Throws` | No picked items | Edge case |
| `CachedSetting_ReturnsCachedValue` | Read-read | Cache hit |
| `CachedSetting_SetValueInvalidatesCache` | Write-through | Invalidation |
| `CachedSetting_ReturnsDefault_WhenMissing` | Missing key | Default value |
| `CachedSetting_CachesNullValues` | null caching | Prevents repeated DB misses |
