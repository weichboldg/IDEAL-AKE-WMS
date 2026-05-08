using FluentAssertions;
using IdealAkeWms.Controllers.Api;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Tests.Controllers;

public class PickingApiControllerTests
{
    [Fact]
    public async Task SearchSourceLocations_ReturnsActiveBookableLocations_WithStockForArticle()
    {
        using var ctx = TestDbContextFactory.Create();
        // Article
        ctx.Articles.Add(new Article
        {
            Id = 1, ArticleNumber = "A-1", Description = "Test", Unit = "Stk",
            CreatedBy = "x", CreatedByWindows = "x"
        });
        // 4 Locations: aktiv+buchbar mit Bestand, aktiv+buchbar ohne Bestand, inaktiv, Wagen
        ctx.StorageLocations.AddRange(
            new StorageLocation { Id = 1, Code = "L-1", BarcodeValue = "L-1", IsActive = true,  IstBuchbar = true,  IsPickingTransport = false, Source = StorageLocationSource.Manual, CreatedBy = "x", CreatedByWindows = "x" },
            new StorageLocation { Id = 2, Code = "L-2", BarcodeValue = "L-2", IsActive = true,  IstBuchbar = true,  IsPickingTransport = false, Source = StorageLocationSource.Manual, CreatedBy = "x", CreatedByWindows = "x" },
            new StorageLocation { Id = 3, Code = "L-3", BarcodeValue = "L-3", IsActive = false, IstBuchbar = true,  IsPickingTransport = false, Source = StorageLocationSource.Manual, CreatedBy = "x", CreatedByWindows = "x" },
            new StorageLocation { Id = 4, Code = "WAGEN-1", BarcodeValue = "WAGEN-1", IsActive = true, IstBuchbar = true, IsPickingTransport = true, Source = StorageLocationSource.Manual, CreatedBy = "x", CreatedByWindows = "x" }
        );
        // Stock auf L-1: 12.5; L-2: kein Bestand
        ctx.StockMovements.Add(new StockMovement
        {
            ArticleId = 1, StorageLocationId = 1, Quantity = 12.5m, MovementType = MovementType.Einbuchung,
            Timestamp = DateTime.Now, WindowsUser = "x", CreatedAt = DateTime.Now,
            CreatedBy = "x", CreatedByWindows = "x"
        });
        await ctx.SaveChangesAsync();

        var locRepo = new StorageLocationRepository(ctx);
        var stockRepo = new StockMovementRepository(ctx);
        var ctrl = new PickingApiController(locRepo, stockRepo);

        var result = await ctrl.SearchSourceLocations(articleNumber: "A-1", q: null, limit: 50);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var items = ok.Value.Should().BeAssignableTo<IEnumerable<object>>().Subject.ToList();
        // Erwartet: L-1 (Bestand 12.5) zuerst, dann L-2 (kein Bestand). Inaktive (L-3) und Wagen (WAGEN-1) raus.
        items.Should().HaveCount(2);
    }

    [Fact]
    public async Task SearchSourceLocations_FiltersByQuery_OnCode()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.Articles.Add(new Article { Id = 1, ArticleNumber = "A-1", Description = "x", Unit = "Stk", CreatedBy = "x", CreatedByWindows = "x" });
        ctx.StorageLocations.AddRange(
            new StorageLocation { Id = 1, Code = "HALLE-A1", BarcodeValue = "HALLE-A1", IsActive = true, IstBuchbar = true, IsPickingTransport = false, Source = StorageLocationSource.Manual, CreatedBy = "x", CreatedByWindows = "x" },
            new StorageLocation { Id = 2, Code = "HALLE-B1", BarcodeValue = "HALLE-B1", IsActive = true, IstBuchbar = true, IsPickingTransport = false, Source = StorageLocationSource.Manual, CreatedBy = "x", CreatedByWindows = "x" }
        );
        await ctx.SaveChangesAsync();

        var locRepo = new StorageLocationRepository(ctx);
        var stockRepo = new StockMovementRepository(ctx);
        var ctrl = new PickingApiController(locRepo, stockRepo);

        var result = await ctrl.SearchSourceLocations(articleNumber: "A-1", q: "A1", limit: 50);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var items = ok.Value.Should().BeAssignableTo<IEnumerable<object>>().Subject.ToList();
        items.Should().ContainSingle();
    }
}
