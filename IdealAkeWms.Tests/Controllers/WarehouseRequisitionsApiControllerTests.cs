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
    private static (WarehouseRequisitionsApiController ctrl, ApplicationDbContext ctx, Mock<ICurrentUserService> user) Setup(
        bool canOrderLager = true, bool canOrderGlas = true)
    {
        var ctx = TestDbContextFactory.Create();

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(s => s.GetCurrentAppUserId()).Returns(1);
        currentUser.Setup(s => s.GetDisplayName()).Returns("tester");
        currentUser.Setup(s => s.GetWindowsUserName()).Returns("DOMAIN\\tester");
        currentUser.Setup(s => s.CanOrderLagerAsync()).ReturnsAsync(canOrderLager);
        currentUser.Setup(s => s.CanOrderGlasAsync()).ReturnsAsync(canOrderGlas);

        var settings = new Mock<IAppSettingRepository>();
        settings.Setup(s => s.GetValueAsync(AppSettingKeys.GlasArtikelgruppen)).ReturnsAsync("GLAS");
        settings.Setup(s => s.GetValueAsync(AppSettingKeys.GemeinsameArtikelgruppen)).ReturnsAsync("EUZ");

        var repo = new WarehouseRequisitionRepository(ctx);
        var articles = new ArticleRepository(ctx);
        var stock = new Mock<IStockMovementRepository>();
        var workplaces = new ProductionWorkplaceRepository(ctx);

        var ctrl = new WarehouseRequisitionsApiController(
            repo, articles, stock.Object, currentUser.Object, settings.Object, workplaces);
        return (ctrl, ctx, currentUser);
    }

    private static void SeedUserWorkplace(ApplicationDbContext ctx, int userId = SetupUserId)
    {
        var wp = new ProductionWorkplace
        {
            Name = "WB-Default", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        };
        ctx.ProductionWorkplaces.Add(wp);
        ctx.SaveChanges();
        ctx.ProductionWorkplaceUsers.Add(new ProductionWorkplaceUser
        {
            ProductionWorkplaceId = wp.Id, UserId = userId,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        ctx.SaveChanges();
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
        var (ctrl, ctx, _) = Setup();
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
        var (ctrl, ctx, _) = Setup();
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
        var (ctrl, ctx, _) = Setup();
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
        var (ctrl, ctx, _) = Setup();
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
        var (ctrl, ctx, _) = Setup();
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
        var (ctrl, ctx, _) = Setup();
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
    public async Task AddItem_DummyArtikel_WirdAbgelehnt()
    {
        var (ctrl, ctx, _) = Setup();
        SeedArticles(ctx);
        // Der DUMMY existiert (geseedet), darf aber NICHT ueber den normalen Add-Pfad rein —
        // sonst uebernaehme er die geteilte Default-Bezeichnung ohne Pflicht-Ueberschreibung.
        ctx.Articles.Add(new Article
        {
            ArticleNumber = Article.DummyArticleNumber,
            Description = Article.DummyDefaultDescription,
            CreatedAt = DateTime.Now, CreatedBy = "seed", CreatedByWindows = "seed"
        });
        ctx.SaveChanges();
        var reqId = SeedDraft(ctx, WarehouseRequisitionType.Lager);

        var result = await ctrl.AddItem(reqId,
            new WarehouseRequisitionsApiController.AddItemRequest(Article.DummyArticleNumber, 1));

        result.Should().BeOfType<BadRequestObjectResult>();
        ctx.WarehouseRequisitionItems.Should().BeEmpty("DUMMY nur ueber den DUMMY-Endpunkt mit Pflicht-Bezeichnung");
    }

    [Fact]
    public async Task UpdateItem_FremdeBestellung_Forbid()
    {
        var (ctrl, ctx, _) = Setup();
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
        var (ctrl, ctx, _) = Setup();
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
        var (ctrl, ctx, _) = Setup();
        var reqId = SeedRequisition(ctx, WarehouseRequisitionType.Lager,
            WarehouseRequisitionStatus.Draft, SetupUserId);
        var itemId = SeedItem(ctx, reqId, "ART-EUZ", 5);

        var result = await ctrl.UpdateItem(itemId,
            new WarehouseRequisitionsApiController.UpdateItemRequest(42));

        result.Should().BeOfType<OkResult>();
        ctx.WarehouseRequisitionItems.Single(i => i.Id == itemId)
            .QuantityRequested.Should().Be(42, "eigener Entwurf ist bearbeitbar");
    }

    [Fact]
    public async Task QuickAdd_EinLagerArtikel_NeuerLagerDraftPlusItem()
    {
        var (ctrl, ctx, _) = Setup();
        SeedArticles(ctx);
        SeedUserWorkplace(ctx);

        var result = await ctrl.QuickAdd(new WarehouseRequisitionsApiController.QuickAddRequest(
            new List<WarehouseRequisitionsApiController.QuickAddItem>
            {
                new("ART-940", 3)
            }));

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var resp = (WarehouseRequisitionsApiController.QuickAddResponse)ok.Value!;
        resp.AddedLager.Should().Be(1);
        resp.AddedGlas.Should().Be(0);
        resp.LagerRequisitionId.Should().NotBeNull();
        resp.GlasRequisitionId.Should().BeNull();
        ctx.WarehouseRequisitions.Should().ContainSingle(r => r.Type == WarehouseRequisitionType.Lager && r.Status == WarehouseRequisitionStatus.Draft);
        ctx.WarehouseRequisitionItems.Should().ContainSingle(i => i.ArticleNumber == "ART-940" && i.QuantityRequested == 3);
    }

    [Fact]
    public async Task QuickAdd_EinGlasArtikel_NeuerGlasDraft()
    {
        var (ctrl, ctx, _) = Setup();
        SeedArticles(ctx);
        SeedUserWorkplace(ctx);

        var result = await ctrl.QuickAdd(new WarehouseRequisitionsApiController.QuickAddRequest(
            new List<WarehouseRequisitionsApiController.QuickAddItem> { new("ART-GLAS", 2) }));

        var resp = (WarehouseRequisitionsApiController.QuickAddResponse)result.Should().BeOfType<OkObjectResult>().Subject.Value!;
        resp.AddedGlas.Should().Be(1);
        resp.GlasRequisitionId.Should().NotBeNull();
        resp.LagerRequisitionId.Should().BeNull();
        ctx.WarehouseRequisitions.Should().ContainSingle(r => r.Type == WarehouseRequisitionType.Glas);
    }

    [Fact]
    public async Task QuickAdd_ZweiLagerArtikel_SelberDraft()
    {
        var (ctrl, ctx, _) = Setup();
        SeedArticles(ctx);
        SeedUserWorkplace(ctx);

        var result = await ctrl.QuickAdd(new WarehouseRequisitionsApiController.QuickAddRequest(
            new List<WarehouseRequisitionsApiController.QuickAddItem> { new("ART-940", 1), new("ART-EUZ", 2) }));

        var resp = (WarehouseRequisitionsApiController.QuickAddResponse)result.Should().BeOfType<OkObjectResult>().Subject.Value!;
        resp.AddedLager.Should().Be(2);
        ctx.WarehouseRequisitions.Count(r => r.Type == WarehouseRequisitionType.Lager).Should().Be(1, "beide Lager-Items in EINEN Draft");
        ctx.WarehouseRequisitionItems.Count().Should().Be(2);
    }

    [Fact]
    public async Task QuickAdd_VorhandenerOffenerDraft_Wiederverwendet()
    {
        var (ctrl, ctx, _) = Setup();
        SeedArticles(ctx);
        SeedUserWorkplace(ctx);
        var existing = SeedDraft(ctx, WarehouseRequisitionType.Lager);

        var result = await ctrl.QuickAdd(new WarehouseRequisitionsApiController.QuickAddRequest(
            new List<WarehouseRequisitionsApiController.QuickAddItem> { new("ART-940", 1) }));

        var resp = (WarehouseRequisitionsApiController.QuickAddResponse)result.Should().BeOfType<OkObjectResult>().Subject.Value!;
        resp.LagerRequisitionId.Should().Be(existing, "offener Draft wird wiederverwendet, kein neuer");
        ctx.WarehouseRequisitions.Count(r => r.Type == WarehouseRequisitionType.Lager).Should().Be(1);
    }

    [Fact]
    public async Task QuickAdd_GemischteListe_ZweiDrafts()
    {
        var (ctrl, ctx, _) = Setup();
        SeedArticles(ctx);
        SeedUserWorkplace(ctx);

        var result = await ctrl.QuickAdd(new WarehouseRequisitionsApiController.QuickAddRequest(
            new List<WarehouseRequisitionsApiController.QuickAddItem> { new("ART-940", 1), new("ART-GLAS", 1) }));

        var resp = (WarehouseRequisitionsApiController.QuickAddResponse)result.Should().BeOfType<OkObjectResult>().Subject.Value!;
        resp.AddedLager.Should().Be(1);
        resp.AddedGlas.Should().Be(1);
        resp.LagerRequisitionId.Should().NotBeNull();
        resp.GlasRequisitionId.Should().NotBeNull();
        ctx.WarehouseRequisitions.Count(r => r.Type == WarehouseRequisitionType.Lager).Should().Be(1);
        ctx.WarehouseRequisitions.Count(r => r.Type == WarehouseRequisitionType.Glas).Should().Be(1);
    }

    [Fact]
    public async Task QuickAdd_FehlendesGlasRecht_Skipped()
    {
        var (ctrl, ctx, _) = Setup(canOrderLager: true, canOrderGlas: false);
        SeedArticles(ctx);
        SeedUserWorkplace(ctx);

        var result = await ctrl.QuickAdd(new WarehouseRequisitionsApiController.QuickAddRequest(
            new List<WarehouseRequisitionsApiController.QuickAddItem> { new("ART-GLAS", 1) }));

        // alles skipped -> BadRequest
        var bad = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var resp = (WarehouseRequisitionsApiController.QuickAddResponse)bad.Value!;
        resp.Skipped.Should().ContainSingle(s => s.ArticleNumber == "ART-GLAS");
        ctx.WarehouseRequisitions.Should().BeEmpty();
    }

    [Fact]
    public async Task QuickAdd_ArtikelNichtGefunden_Skipped()
    {
        var (ctrl, ctx, _) = Setup();
        SeedArticles(ctx);
        SeedUserWorkplace(ctx);

        var result = await ctrl.QuickAdd(new WarehouseRequisitionsApiController.QuickAddRequest(
            new List<WarehouseRequisitionsApiController.QuickAddItem> { new("ART-940", 1), new("UNBEKANNT", 1) }));

        var resp = (WarehouseRequisitionsApiController.QuickAddResponse)result.Should().BeOfType<OkObjectResult>().Subject.Value!;
        resp.AddedLager.Should().Be(1);
        resp.Skipped.Should().ContainSingle(s => s.ArticleNumber == "UNBEKANNT");
    }

    [Fact]
    public async Task QuickAdd_MengeNullOderNegativ_Skipped()
    {
        var (ctrl, ctx, _) = Setup();
        SeedArticles(ctx);
        SeedUserWorkplace(ctx);

        var result = await ctrl.QuickAdd(new WarehouseRequisitionsApiController.QuickAddRequest(
            new List<WarehouseRequisitionsApiController.QuickAddItem> { new("ART-940", 0) }));

        var bad = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var resp = (WarehouseRequisitionsApiController.QuickAddResponse)bad.Value!;
        resp.Skipped.Should().ContainSingle(s => s.ArticleNumber == "ART-940");
        ctx.WarehouseRequisitionItems.Should().BeEmpty();
    }

    [Fact]
    public async Task QuickAdd_KeineWerkbank_BadRequest()
    {
        var (ctrl, ctx, _) = Setup();
        SeedArticles(ctx);
        // KEIN SeedUserWorkplace -> Werkbank nicht aufloesbar

        var result = await ctrl.QuickAdd(new WarehouseRequisitionsApiController.QuickAddRequest(
            new List<WarehouseRequisitionsApiController.QuickAddItem> { new("ART-940", 1) }));

        result.Should().BeOfType<BadRequestObjectResult>();
        ctx.WarehouseRequisitions.Should().BeEmpty();
    }

    // ---- DUMMY-Position (Teil-7) ----

    private static void SeedDummyArticle(ApplicationDbContext ctx)
    {
        ctx.Articles.Add(new Article
        {
            ArticleNumber = Article.DummyArticleNumber,
            Description = Article.DummyDefaultDescription,
            CreatedAt = DateTime.Now, CreatedBy = "System-Seed", CreatedByWindows = "System-Seed"
        });
        ctx.SaveChanges();
    }

    [Fact]
    public async Task AddDummyItem_ValidDescription_Ok_PositionHasDescription_ArticleUnchanged()
    {
        var (ctrl, ctx, _) = Setup();
        SeedDummyArticle(ctx);
        var reqId = SeedDraft(ctx, WarehouseRequisitionType.Lager);

        var result = await ctrl.AddDummyItem(reqId,
            new WarehouseRequisitionsApiController.AddDummyItemRequest("Spezialdichtung 12mm", 4));

        result.Should().BeOfType<OkResult>();
        var item = ctx.WarehouseRequisitionItems.Single(i => i.WarehouseRequisitionId == reqId);
        item.ArticleNumber.Should().Be(Article.DummyArticleNumber);
        item.ArticleDescription.Should().Be("Spezialdichtung 12mm");
        item.QuantityRequested.Should().Be(4);
        ctx.Articles.Single(a => a.ArticleNumber == Article.DummyArticleNumber)
            .Description.Should().Be(Article.DummyDefaultDescription, "geteilte Article.Description bleibt unveraendert");
    }

    [Fact]
    public async Task AddDummyItem_EmptyDescription_BadRequest()
    {
        var (ctrl, ctx, _) = Setup();
        SeedDummyArticle(ctx);
        var reqId = SeedDraft(ctx, WarehouseRequisitionType.Lager);

        var result = await ctrl.AddDummyItem(reqId,
            new WarehouseRequisitionsApiController.AddDummyItemRequest("   ", 1));

        result.Should().BeOfType<BadRequestObjectResult>();
        ctx.WarehouseRequisitionItems.Should().BeEmpty();
    }

    [Fact]
    public async Task AddDummyItem_UnchangedDefaultDescription_BadRequest()
    {
        var (ctrl, ctx, _) = Setup();
        SeedDummyArticle(ctx);
        var reqId = SeedDraft(ctx, WarehouseRequisitionType.Lager);

        // Case-insensitiv + Trim: die Default-Seed-Bezeichnung wird abgelehnt.
        var result = await ctrl.AddDummyItem(reqId,
            new WarehouseRequisitionsApiController.AddDummyItemRequest(
                "  " + Article.DummyDefaultDescription.ToUpperInvariant() + "  ", 1));

        result.Should().BeOfType<BadRequestObjectResult>();
        ctx.WarehouseRequisitionItems.Should().BeEmpty("unveraenderte Bezeichnung ist Pflicht-Verstoss");
    }

    [Fact]
    public async Task AddDummyItem_SeedMissing_BadRequest()
    {
        var (ctrl, ctx, _) = Setup();
        // KEIN SeedDummyArticle -> DUMMY-Artikel fehlt.
        var reqId = SeedDraft(ctx, WarehouseRequisitionType.Lager);

        var result = await ctrl.AddDummyItem(reqId,
            new WarehouseRequisitionsApiController.AddDummyItemRequest("Irgendeine Bezeichnung", 1));

        var bad = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        bad.Value!.ToString().Should().Contain("Seed");
        ctx.WarehouseRequisitionItems.Should().BeEmpty();
    }

    [Fact]
    public async Task AddDummyItem_ZweiVerschiedeneBezeichnungen_BeideErlaubt()
    {
        var (ctrl, ctx, _) = Setup();
        SeedDummyArticle(ctx);
        var reqId = SeedDraft(ctx, WarehouseRequisitionType.Lager);

        var r1 = await ctrl.AddDummyItem(reqId,
            new WarehouseRequisitionsApiController.AddDummyItemRequest("Teil A", 1));
        var r2 = await ctrl.AddDummyItem(reqId,
            new WarehouseRequisitionsApiController.AddDummyItemRequest("Teil B", 2));

        r1.Should().BeOfType<OkResult>();
        r2.Should().BeOfType<OkResult>("Duplikat-Guard wird fuer DUMMY uebersprungen");
        ctx.WarehouseRequisitionItems.Count(i => i.WarehouseRequisitionId == reqId).Should().Be(2);
    }

    [Fact]
    public async Task AddDummyItem_InGlasBestellung_Ok()
    {
        var (ctrl, ctx, _) = Setup();
        SeedDummyArticle(ctx);
        var reqId = SeedDraft(ctx, WarehouseRequisitionType.Glas);

        var result = await ctrl.AddDummyItem(reqId,
            new WarehouseRequisitionsApiController.AddDummyItemRequest("Glas-Sonderteil", 1));

        result.Should().BeOfType<OkResult>("DUMMY umgeht den Glas-Artikelgruppen-Guard");
        ctx.WarehouseRequisitionItems.Should().ContainSingle(i => i.WarehouseRequisitionId == reqId);
    }

    [Fact]
    public async Task AddDummyItem_FremdeBestellung_Forbid()
    {
        var (ctrl, ctx, _) = Setup();
        SeedDummyArticle(ctx);
        var reqId = SeedRequisition(ctx, WarehouseRequisitionType.Lager,
            WarehouseRequisitionStatus.Draft, SetupUserId + 1000);

        var result = await ctrl.AddDummyItem(reqId,
            new WarehouseRequisitionsApiController.AddDummyItemRequest("Teil A", 1));

        result.Should().BeOfType<ForbidResult>();
        ctx.WarehouseRequisitionItems.Should().BeEmpty();
    }

    [Fact]
    public async Task AddDummyItem_NichtDraft_BadRequest()
    {
        var (ctrl, ctx, _) = Setup();
        SeedDummyArticle(ctx);
        var reqId = SeedRequisition(ctx, WarehouseRequisitionType.Lager,
            WarehouseRequisitionStatus.Submitted, SetupUserId);

        var result = await ctrl.AddDummyItem(reqId,
            new WarehouseRequisitionsApiController.AddDummyItemRequest("Teil A", 1));

        result.Should().BeOfType<BadRequestObjectResult>();
        ctx.WarehouseRequisitionItems.Should().BeEmpty();
    }

    // ---- Kommentar (Teil-7) ----

    [Fact]
    public async Task UpdateComment_EigenerDraft_Ok_Persisted()
    {
        var (ctrl, ctx, _) = Setup();
        var reqId = SeedDraft(ctx, WarehouseRequisitionType.Lager);

        var result = await ctrl.UpdateComment(reqId,
            new WarehouseRequisitionsApiController.UpdateCommentRequest("  Bitte dringend  "));

        result.Should().BeOfType<OkResult>();
        ctx.WarehouseRequisitions.Single(r => r.Id == reqId).Comment
            .Should().Be("Bitte dringend", "Kommentar wird getrimmt gespeichert");
    }

    [Fact]
    public async Task UpdateComment_FremdeBestellung_Forbid()
    {
        var (ctrl, ctx, _) = Setup();
        var reqId = SeedRequisition(ctx, WarehouseRequisitionType.Lager,
            WarehouseRequisitionStatus.Draft, SetupUserId + 1000);

        var result = await ctrl.UpdateComment(reqId,
            new WarehouseRequisitionsApiController.UpdateCommentRequest("Fremd"));

        result.Should().BeOfType<ForbidResult>();
        ctx.WarehouseRequisitions.Single(r => r.Id == reqId).Comment.Should().BeNull();
    }

    [Fact]
    public async Task UpdateComment_NichtDraft_BadRequest()
    {
        var (ctrl, ctx, _) = Setup();
        var reqId = SeedRequisition(ctx, WarehouseRequisitionType.Lager,
            WarehouseRequisitionStatus.Submitted, SetupUserId);

        var result = await ctrl.UpdateComment(reqId,
            new WarehouseRequisitionsApiController.UpdateCommentRequest("Zu spaet"));

        result.Should().BeOfType<BadRequestObjectResult>();
        ctx.WarehouseRequisitions.Single(r => r.Id == reqId).Comment.Should().BeNull();
    }
}
