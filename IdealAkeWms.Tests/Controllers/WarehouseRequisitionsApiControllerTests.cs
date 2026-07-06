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

    private static int SeedDraft(ApplicationDbContext ctx, WarehouseRequisitionType type)
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
            CreatedAt = DateTime.Now, CreatedBy = "tester", CreatedByWindows = "DOMAIN\\tester"
        };
        ctx.WarehouseRequisitions.Add(r);
        ctx.SaveChanges();
        return r.Id;
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
}
