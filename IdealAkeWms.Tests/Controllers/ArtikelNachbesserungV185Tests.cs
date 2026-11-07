using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace IdealAkeWms.Tests.Controllers;

/// <summary>Spec 2026-11-08-artikeldetails-lieferanten-anzeigen, Nachbesserung 19:17 (v1.85.0): Punkte 2, 3, 5.</summary>
public class ArtikelNachbesserungV185Tests
{
    private static async Task<IdealAkeWms.Data.ApplicationDbContext> SeedAsync()
    {
        var db = TestDbContextFactory.Create();
        Article A(int id, string nr) => new() { Id = id, ArticleNumber = nr, Description = "Bez " + nr, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        db.Articles.AddRange(A(5, "00722001"), A(6, "00722002"), A(7, "00722003"));
        db.Creditors.AddRange(
            new Creditor { Id = 1, Name = "Huber", SageKto = "K31013", CreatedBy = "t", CreatedByWindows = "t" },
            new Creditor { Id = 2, Name = "Maier", SageKto = "K30021", CreatedBy = "t", CreatedByWindows = "t" });
        db.ArticleSageDetails.AddRange(
            new ArticleSageDetail { ArticleId = 5, Hauptlieferant = " K31013 ", LetzterLieferant = "K30021", Basismengeneinheit = "RLL",
                Lagermengeneinheit = "Stk", UmrechnungsFaktorLME = 1m, CreatedBy = "t", CreatedByWindows = "t" },
            new ArticleSageDetail { ArticleId = 6, Hauptlieferant = "K99999", LetzterLieferant = "K31013", CreatedBy = "t", CreatedByWindows = "t" });
        ArticleSupplier S(int art, int kred, bool haupt) => new()
        {
            ArticleId = art, CreditorId = kred, Artikelnummer = "x", IstHauptlieferant = haupt, SageBestellnummer = "B" + art,
            Einkaufsmengeneinheit = "PKG", UmrechnungsfaktorEK = 10m, CreatedBy = "t", CreatedByWindows = "t"
        };
        db.ArticleSuppliers.AddRange(S(5, 1, true), S(6, 1, false), S(7, 2, true));
        await db.SaveChangesAsync();
        return db;
    }

    [Fact]
    public async Task Punkt2_KreditorKarte_ServerFilterVorPaging()
    {
        // QA Q-1/Q-2: Karte ist Server-Mode (Filter aus der URL, Pagination), nicht alle Zeilen im Browser.
        using var db = await SeedAsync();
        var user = new Mock<ICurrentUserService>();
        user.Setup(x => x.GetDefaultPageSizeAsync()).ReturnsAsync((int?)null);
        var ctrl = new IdealAkeWms.Controllers.CreditorsController(new CreditorRepository(db), new FaHierarchyOrderInfoRepository(db), user.Object,
            new ArticleRepository(db));
        var http = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        http.Request.QueryString = new Microsoft.AspNetCore.Http.QueryString("?colf_artikel=00722002");
        ctrl.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext { HttpContext = http };

        await ctrl.Details(1, page: 1, pageSize: 25);

        ((List<LieferantArtikelRow>)ctrl.ViewBag.LieferantArtikel).Select(r => r.ArticleNumber).Should().Equal("00722002");
        ((int)ctrl.ViewBag.LieferantArtikelGesamt).Should().Be(2);
        ((PaginationState)ctrl.ViewBag.LieferantArtikelPagination).TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Punkt2_ArtikelJeLieferant_HauptUndLetzterKennzeichen()
    {
        using var db = await SeedAsync();
        var rows = await new ArticleRepository(db).GetArtikelJeLieferantAsync(1, "K31013");
        rows.Select(r => (r.ArticleNumber, r.IstHauptlieferant, r.IstLetzterLieferant))
            .Should().Equal(("00722001", true, false), ("00722002", false, true));
        rows[0].Einkaufsmengeneinheit.Should().Be("PKG");
    }

    [Fact]
    public async Task Punkt3_LieferantTexte_UndSpaltenfilter_GleicherText()
    {
        using var db = await SeedAsync();
        var repo = new ArticleRepository(db);
        var texte = await repo.GetLieferantTexteAsync(new[] { 5, 6, 7 });
        texte[5].Should().Be(("Huber (K31013)", "Maier (K30021)"));
        texte[6].Should().Be(("K99999", "Huber (K31013)"), "ohne Kreditor nur das Konto");
        texte.ContainsKey(7).Should().BeFalse("ohne Satellit keine Werte");

        // Spaltenfilter laeuft ueber EF.Functions.Like (unter InMemory nicht ausfuehrbar) -> SQL-Uebersetzung pruefen (Muster ArticleRepositoryTests).
        var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<IdealAkeWms.Data.ApplicationDbContext>()
            .UseSqlServer("Server=nonexistent-host-for-tests;Database=x;TrustServerCertificate=True;Connect Timeout=1").Options;
        using var sqlCtx = new IdealAkeWms.Data.ApplicationDbContext(options);
        foreach (var key in new[] { "hauptlieferant", "letzter-lieferant" })
        {
            var sql = new ArticleRepository(sqlCtx).BuildArticleQuery(null, new Dictionary<string, string> { [key] = "huber" }).ToQueryString();
            sql.Should().Contain("Creditors").And.Contain("ArticleSageDetails").And.Contain("LIKE", key);
        }

        var cfg = ColumnDefinitions.GetByViewKey("Articles");
        cfg!.Columns.Where(c => c.DefaultHidden).Select(c => c.Key).Should().Equal("hauptlieferant", "letzter-lieferant");
    }

    [Fact]
    public async Task Punkt5_Einheiten_SupplierRowMitUmrechnung()
    {
        using var db = await SeedAsync();
        var row = (await new ArticleRepository(db).GetSuppliersAsync(5)).Single();
        (row.Einkaufsmengeneinheit, row.UmrechnungsfaktorEK).Should().Be(("PKG", (decimal?)10m));
        var view = File.ReadAllText(Pfad("Views", "Shared", "_ArtikelSageKarten.cshtml"));
        view.Should().Contain("\"Mengeneinheiten\"").And.Contain("Lagereinheit").And.Contain("letzte Änderung durch den Artikel-Abgleich")
            .And.NotContain("aus Sage gelesen am");
    }

    private static string Pfad(params string[] teile)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "IdealAkeWms", "Views"))) dir = dir.Parent;
        return Path.Combine(new[] { dir!.FullName, "IdealAkeWms" }.Concat(teile).ToArray());
    }
}
