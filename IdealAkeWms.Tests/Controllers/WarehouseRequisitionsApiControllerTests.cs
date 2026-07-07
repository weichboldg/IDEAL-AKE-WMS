using FluentAssertions;
using IdealAkeWms.Controllers.Api;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Controllers;

public class WarehouseRequisitionsApiControllerTests
{
    private static (WarehouseRequisitionsApiController ctrl, ApplicationDbContext ctx) Setup()
    {
        var ctx = TestDbContextFactory.Create();

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(s => s.GetCurrentAppUserId()).Returns(1);
        currentUser.Setup(s => s.GetDisplayName()).Returns("tester");
        currentUser.Setup(s => s.GetWindowsUserName()).Returns("DOMAIN\\tester");

        var settings = new Mock<IAppSettingRepository>();
        settings.Setup(s => s.GetValueAsync(AppSettingKeys.GlasArtikelgruppen)).ReturnsAsync("GLAS");
        settings.Setup(s => s.GetValueAsync(AppSettingKeys.GemeinsameArtikelgruppen)).ReturnsAsync("EUZ");

        var repo = new WarehouseRequisitionRepository(ctx);
        var articles = new ArticleRepository(ctx);
        var stock = new Mock<IStockMovementRepository>();

        var ctrl = new WarehouseRequisitionsApiController(
            repo, articles, stock.Object, currentUser.Object, settings.Object);
        return (ctrl, ctx);
    }

    private static void SeedArticles(ApplicationDbContext ctx)
    {
        ctx.Articles.AddRange(
            new Article { ArticleNumber = "ART-940", Description = "Kleinmaterial", Unit = "Stk", ArticleGroup = "940", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" },
            new Article { ArticleNumber = "ART-GLAS", Description = "Glasscheibe", Unit = "Stk", ArticleGroup = "GLAS", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" },
            new Article { ArticleNumber = "ART-EUZ", Description = "Einbauzubehoer", Unit = "Stk", ArticleGroup = "EUZ", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        ctx.SaveChanges();
    }

    // Setup-User: GetCurrentAppUserId() == 1, GetDisplayName() == "tester".
    private const int SetupUserId = 1;

    private static int SeedDraft(ApplicationDbContext ctx, WarehouseRequisitionType type)
        => SeedRequisition(ctx, type, WarehouseRequisitionStatus.Draft, SetupUserId);

    private static int SeedRequisition(ApplicationDbContext ctx, WarehouseRequisitionType type,
        WarehouseRequisitionStatus status, int createdByUserId)
    {
        var wp = new ProductionWorkplace
        {
            Name = "WB-" + Guid.NewGuid().ToString("N")[..8],
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        };
        ctx.ProductionWorkplaces.Add(wp);
        ctx.SaveChanges();

        var r = new WarehouseRequisition
        {
            ProductionWorkplaceId = wp.Id,
            Type = type,
            Status = status,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.Now, CreatedBy = "tester", CreatedByWindows = "DOMAIN\\tester"
        };
        ctx.WarehouseRequisitions.Add(r);
        ctx.SaveChanges();
        return r.Id;
    }

    private static int SeedItem(ApplicationDbContext ctx, int requisitionId, string articleNumber, decimal qty)
    {
        var item = new WarehouseRequisitionItem
        {
            WarehouseRequisitionId = requisitionId,
            ArticleNumber = articleNumber,
            ArticleDescription = "desc",
            Unit = "Stk",
            QuantityRequested = qty,
            Position = 1,
            CreatedAt = DateTime.Now, CreatedBy = "tester", CreatedByWindows = "DOMAIN\\tester"
        };
        ctx.WarehouseRequisitionItems.Add(item);
        ctx.SaveChanges();
        return item.Id;
    }

    [Fact]
    public async Task AddItem_LagerBestellung_GlasArtikel_BadRequest()
    {
        var (ctrl, ctx) = Setup();
        SeedArticles(ctx);
        var reqId = SeedDraft(ctx, WarehouseRequisitionType.Lager);

        var result = await ctrl.AddItem(reqId,
            new WarehouseRequisitionsApiController.AddItemRequest("ART-GLAS", 1));

        result.Should().BeOfType<BadRequestObjectResult>();
        ctx.WarehouseRequisitionItems.Should().BeEmpty("Glas-Artikel darf nicht in Lager-Bestellung");
    }

    [Fact]
    public async Task AddItem_GlasBestellung_NormalerArtikel_BadRequest()
    {
        var (ctrl, ctx) = Setup();
        SeedArticles(ctx);
        var reqId = SeedDraft(ctx, WarehouseRequisitionType.Glas);

        var result = await ctrl.AddItem(reqId,
            new WarehouseRequisitionsApiController.AddItemRequest("ART-940", 1));

        result.Should().BeOfType<BadRequestObjectResult>();
        ctx.WarehouseRequisitionItems.Should().BeEmpty("Normaler Artikel darf nicht in Glas-Bestellung");
    }

    [Fact]
    public async Task AddItem_GlasBestellung_GlasArtikel_Ok()
    {
        var (ctrl, ctx) = Setup();
        SeedArticles(ctx);
        var reqId = SeedDraft(ctx, WarehouseRequisitionType.Glas);

        var result = await ctrl.AddItem(reqId,
            new WarehouseRequisitionsApiController.AddItemRequest("ART-GLAS", 2));

        result.Should().BeOfType<OkResult>();
        ctx.WarehouseRequisitionItems.Should().ContainSingle(i =>
            i.WarehouseRequisitionId == reqId && i.ArticleNumber == "ART-GLAS");
    }

    [Fact]
    public async Task AddItem_EuzArtikel_InBeidenErlaubt()
    {
        var (ctrl, ctx) = Setup();
        SeedArticles(ctx);
        var lagerId = SeedDraft(ctx, WarehouseRequisitionType.Lager);
        var glasId = SeedDraft(ctx, WarehouseRequisitionType.Glas);

        var lagerResult = await ctrl.AddItem(lagerId,
            new WarehouseRequisitionsApiController.AddItemRequest("ART-EUZ", 1));
        var glasResult = await ctrl.AddItem(glasId,
            new WarehouseRequisitionsApiController.AddItemRequest("ART-EUZ", 1));

        lagerResult.Should().BeOfType<OkResult>("gemeinsame Gruppe EUZ ist in Lager-Bestellung erlaubt");
        glasResult.Should().BeOfType<OkResult>("gemeinsame Gruppe EUZ ist in Glas-Bestellung erlaubt");
        ctx.WarehouseRequisitionItems.Should().HaveCount(2);
    }

    [Fact]
    public async Task AddItem_FremdeBestellung_Forbid()
    {
        var (ctrl, ctx) = Setup();
        SeedArticles(ctx);
        // Fremde Bestellung (anderer Owner), aber Draft.
        var reqId = SeedRequisition(ctx, WarehouseRequisitionType.Lager,
            WarehouseRequisitionStatus.Draft, SetupUserId + 1000);

        var result = await ctrl.AddItem(reqId,
            new WarehouseRequisitionsApiController.AddItemRequest("ART-EUZ", 1));

        result.Should().BeOfType<ForbidResult>();
        ctx.WarehouseRequisitionItems.Should().BeEmpty("fremde Bestellung darf nicht bearbeitet werden");
    }

    [Fact]
    public async Task AddItem_NichtDraft_BadRequest()
    {
        var (ctrl, ctx) = Setup();
        SeedArticles(ctx);
        // Eigene Bestellung, aber bereits abgeschickt.
        var reqId = SeedRequisition(ctx, WarehouseRequisitionType.Lager,
            WarehouseRequisitionStatus.Submitted, SetupUserId);

        var result = await ctrl.AddItem(reqId,
            new WarehouseRequisitionsApiController.AddItemRequest("ART-EUZ", 1));

        result.Should().BeOfType<BadRequestObjectResult>();
        ctx.WarehouseRequisitionItems.Should().BeEmpty("nicht-Entwurf darf nicht mehr bearbeitet werden");
    }

    [Fact]
    public async Task UpdateItem_FremdeBestellung_Forbid()
    {
        var (ctrl, ctx) = Setup();
        var reqId = SeedRequisition(ctx, WarehouseRequisitionType.Lager,
            WarehouseRequisitionStatus.Draft, SetupUserId + 1000);
        var itemId = SeedItem(ctx, reqId, "ART-EUZ", 5);

        var result = await ctrl.UpdateItem(itemId,
            new WarehouseRequisitionsApiController.UpdateItemRequest(99));

        result.Should().BeOfType<ForbidResult>();
        ctx.WarehouseRequisitionItems.Single(i => i.Id == itemId)
            .QuantityRequested.Should().Be(5, "fremde Position darf nicht geaendert werden");
    }

    [Fact]
    public async Task RemoveItem_NichtDraft_BadRequest()
    {
        var (ctrl, ctx) = Setup();
        var reqId = SeedRequisition(ctx, WarehouseRequisitionType.Lager,
            WarehouseRequisitionStatus.Submitted, SetupUserId);
        var itemId = SeedItem(ctx, reqId, "ART-EUZ", 5);

        var result = await ctrl.RemoveItem(itemId);

        result.Should().BeOfType<BadRequestObjectResult>();
        ctx.WarehouseRequisitionItems.Should().ContainSingle(i => i.Id == itemId,
            "Position einer abgeschickten Bestellung darf nicht geloescht werden");
    }

    [Fact]
    public async Task UpdateItem_EigenerDraft_Ok()
    {
        var (ctrl, ctx) = Setup();
        var reqId = SeedRequisition(ctx, WarehouseRequisitionType.Lager,
            WarehouseRequisitionStatus.Draft, SetupUserId);
        var itemId = SeedItem(ctx, reqId, "ART-EUZ", 5);

        var result = await ctrl.UpdateItem(itemId,
            new WarehouseRequisitionsApiController.UpdateItemRequest(42));

        result.Should().BeOfType<OkResult>();
        ctx.WarehouseRequisitionItems.Single(i => i.Id == itemId)
            .QuantityRequested.Should().Be(42, "eigener Entwurf ist bearbeitbar");
    }
}
