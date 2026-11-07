using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace IdealAkeWms.Tests.Controllers;

/// <summary>Spec 2026-11-08-artikeldetails-lieferanten-anzeigen (v1.80.0): AK 1-3, 8-9.</summary>
public class ArtikelinfoLieferantenTests
{
    private static (ArticlesController ctrl, Mock<IArticleRepository> article, Mock<ICreditorRepository> creditors) Build(ArticleSageDetail? sd)
    {
        var article = new Mock<IArticleRepository>();
        article.Setup(a => a.GetByArticleNumberAsync("00722001")).ReturnsAsync(new Article { Id = 5, ArticleNumber = "00722001" });
        article.Setup(a => a.GetSageDetailAsync(5)).ReturnsAsync(sd);
        article.Setup(a => a.GetSuppliersAsync(5)).ReturnsAsync(new List<ArticleSupplierRow>());
        var stock = new Mock<IStockMovementRepository>();
        stock.Setup(s => s.GetCurrentStockAsync(It.IsAny<string>(), null, null, null)).ReturnsAsync(new List<StockOverviewItem>());
        var attr = new Mock<IArticleAttributeRepository>();
        attr.Setup(a => a.GetActiveDefinitionsOrderedAsync()).ReturnsAsync(new List<ArticleAttributeDefinition>());
        attr.Setup(a => a.GetValuesByArticleIdAsync(It.IsAny<int>())).ReturnsAsync(new List<ArticleAttributeValue>());
        var bom = new Mock<IBomCacheRepository>();
        bom.Setup(b => b.GetDeviceArticleNumbersByComponentAsync(It.IsAny<string>())).ReturnsAsync(new List<string>());
        bom.Setup(b => b.GetComponentMengePerDeviceAsync(It.IsAny<string>())).ReturnsAsync(new Dictionary<string, decimal>());
        var orders = new Mock<IProductionOrderRepository>();
        orders.Setup(o => o.GetByArticleNumbersAsync(It.IsAny<List<string>>())).ReturnsAsync(new List<ProductionOrder>());
        var creditors = new Mock<ICreditorRepository>();
        var ctrl = new ArticlesController(article.Object, stock.Object, Mock.Of<ICurrentUserService>(), attr.Object,
            Mock.Of<IArticleCategoryRepository>(), bom.Object, orders.Object, Mock.Of<IStorageLocationRepository>(),
            Mock.Of<IServiceSettingRepository>(), creditors.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        return (ctrl, article, creditors);
    }

    private static ArticleInfoViewModel Vm(IActionResult r) => (ArticleInfoViewModel)((ViewResult)r).Model!;

    [Fact]
    public async Task Info_LieferantenGetrimmtNachgeschlagen_EinAufruf_AK1_AK2()
    {
        var (ctrl, article, creditors) = Build(new ArticleSageDetail { ArticleId = 5, Hauptlieferant = " K31013 ", LetzterLieferant = "K99999" });
        IReadOnlyCollection<string>? angefragt = null;
        creditors.Setup(c => c.GetBySageKtosAsync(It.IsAny<IReadOnlyCollection<string>>()))
            .Callback<IReadOnlyCollection<string>>(k => angefragt = k)
            .ReturnsAsync(new List<Creditor> { new() { Id = 3, Name = "Lieferant A", SageKto = "k31013" } });

        var vm = Vm(await ctrl.Info("00722001"));

        angefragt.Should().BeEquivalentTo(new[] { "K31013", "K99999" });
        creditors.Verify(c => c.GetBySageKtosAsync(It.IsAny<IReadOnlyCollection<string>>()), Times.Once);
        article.Verify(a => a.GetSuppliersAsync(5), Times.Once);
        vm.HauptlieferantKreditor!.Id.Should().Be(3, "Gross/Klein egal, Satellit-Wert getrimmt");
        vm.LetzterLieferantKreditor.Should().BeNull("kein Kreditor zu K99999");
    }

    [Fact]
    public async Task Info_OhneSatellit_KeinKreditorAufruf_LieferantenTrotzdem_AK1()
    {
        var (ctrl, article, creditors) = Build(null);

        var vm = Vm(await ctrl.Info("00722001"));

        vm.SageDetail.Should().BeNull();
        creditors.Verify(c => c.GetBySageKtosAsync(It.IsAny<IReadOnlyCollection<string>>()), Times.Never);
        article.Verify(a => a.GetSuppliersAsync(5), Times.Once, "Karte „Lieferanten“ steht unabhaengig vom Satelliten");
    }

    [Fact]
    public async Task Repository_GetSuppliers_NurArtikel_HauptZuerstDannName_GetBySageKtos_NurAngefragte_AK3()
    {
        using var db = TestDbContextFactory.Create();
        Creditor K(int id, string name, string kto) => new() { Id = id, Name = name, SageKto = kto, CreatedBy = "t", CreatedByWindows = "t" };
        db.Creditors.AddRange(K(1, "Zeta", "K1"), K(2, "Alpha", "K2"), K(3, "Mitte", "K3"));
        ArticleSupplier S(int art, int kred, bool haupt) => new()
        {
            ArticleId = art, CreditorId = kred, Artikelnummer = "A" + art, IstHauptlieferant = haupt, CreatedBy = "t", CreatedByWindows = "t"
        };
        db.ArticleSuppliers.AddRange(S(5, 1, true), S(5, 2, false), S(5, 3, false), S(6, 2, true));
        await db.SaveChangesAsync();

        var rows = await new ArticleRepository(db).GetSuppliersAsync(5);
        rows.Select(r => r.Name).Should().Equal("Zeta", "Alpha", "Mitte");
        rows[0].IstHauptlieferant.Should().BeTrue();

        var kred = await new CreditorRepository(db).GetBySageKtosAsync(new[] { "K2", "K3" });
        kred.Select(c => c.Id).Should().BeEquivalentTo(new[] { 2, 3 });
    }

    // ---- AK 8/9: Verlinkung (View-Text) ----
    private static string ViewText(params string[] teile)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "IdealAkeWms", "Views", "Articles"))) dir = dir.Parent;
        return File.ReadAllText(Path.Combine(new[] { dir!.FullName, "IdealAkeWms", "Views", "Articles" }.Concat(teile).ToArray()));
    }

    [Fact]
    public void Verlinkung_ListeUndBearbeiten_AK8_AK9()
    {
        ViewText("Index.cshtml").Should().Contain("<a asp-action=\"Info\" asp-route-articleNumber=\"@article.ArticleNumber\">@article.ArticleNumber</a>");
        ViewText("Edit.cshtml").Should().Contain("asp-route-articleNumber=\"@Model.Article.ArticleNumber\"").And.Contain(">Artikelinfo</a>");
        var info = ViewText("Info.cshtml");
        info.Should().NotContain("(\"Hauptlieferant\", T(sd.Hauptlieferant))", "AK 7: nicht doppelt in „Bestand und Dispo“");
        info.Should().Contain("Sync:Quelle:Artikel").And.Contain("Sync:ArtikelLieferantEnabled").And.NotContain("Html.Raw");
    }
}
