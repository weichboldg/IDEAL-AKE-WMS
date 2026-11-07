using System.Text.Json;
using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Controllers;

public class ArticlesApiControllerTests
{
    private static (ArticlesApiController ctrl, ApplicationDbContext ctx) Setup()
    {
        var ctx = TestDbContextFactory.Create();

        var settings = new Mock<IAppSettingRepository>();
        settings.Setup(s => s.GetValueAsync(AppSettingKeys.GlasArtikelgruppen)).ReturnsAsync("GLAS");
        settings.Setup(s => s.GetValueAsync(AppSettingKeys.GemeinsameArtikelgruppen)).ReturnsAsync("EUZ");

        var ctrl = new ArticlesApiController(new ArticleRepository(ctx), settings.Object);
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

    // v1.68.0: GET /api/articles/by-matchcode
    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task GetByMatchcode_Leer_BadRequest(string? matchcode)
    {
        var (ctrl, _) = Setup();
        (await ctrl.GetByMatchcode(matchcode)).Should().BeOfType<BadRequestResult>();
    }

    [Theory]
    [InlineData(0, 0, false)]
    [InlineData(1, 1, false)]
    [InlineData(6, 5, true)]
    public async Task GetByMatchcode_KeinsEinsMehrdeutig(int anzahl, int erwartet, bool more)
    {
        var (ctrl, ctx) = Setup();
        for (var i = 0; i < anzahl; i++)
            ctx.Articles.Add(new Article { ArticleNumber = "M" + i, Description = "Teil", Matchcode = "G-KT-FRR-510-2010--1760B-250-V01", Unit = "Stk", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        ctx.SaveChanges();

        var ok = (await ctrl.GetByMatchcode("g-kt-frr-510-2010--1760b-250-v01")).Should().BeOfType<OkObjectResult>().Subject;
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        doc.RootElement.GetProperty("articles").GetArrayLength().Should().Be(erwartet);
        doc.RootElement.GetProperty("more").GetBoolean().Should().Be(more);
        if (anzahl == 1)
        {
            var a = doc.RootElement.GetProperty("articles")[0];
            a.GetProperty("articleNumber").GetString().Should().Be("M0");
            a.GetProperty("text").GetString().Should().Be("M0 - Teil", "Select2-Textformat wie by-number");
        }
    }

    // v1.78.0: GET /api/articles/by-ean (Spec 2026-11-07-ean-einkaufsartikel-scan F2/F3/F5)
    private static Article MitEan(string nummer, string? ean) => new()
    {
        ArticleNumber = nummer, Description = "Teil", Unit = "Stk", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t",
        SageDetail = ean == null ? null : new ArticleSageDetail { EANNummer = ean, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" },
    };

    private static async Task<(string[] Nummern, bool More)> Ean(ArticlesApiController ctrl, string ean)
    {
        var ok = (await ctrl.GetByEan(ean)).Should().BeOfType<OkObjectResult>().Subject;
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        return (doc.RootElement.GetProperty("articles").EnumerateArray().Select(a => a.GetProperty("articleNumber").GetString()!).ToArray(),
            doc.RootElement.GetProperty("more").GetBoolean());
    }

    [Fact]
    public async Task GetByEan_ArtikelnummerZuerst_DannEan_Mehrtreffer_KeinTreffer()
    {
        var (ctrl, ctx) = Setup();
        ctx.Articles.AddRange(MitEan("00726502", "4002872120264 "), MitEan("12345678", null), MitEan("X1", "87654321"),
            MitEan("Y1", "9007646008637"), MitEan("Y2", "9007646008637"));
        ctx.SaveChanges();

        (await Ean(ctrl, " 4002872120264 ")).Nummern.Should().Equal("00726502");     // EAN, im Satelliten ungetrimmt gespeichert
        (await Ean(ctrl, "12345678")).Nummern.Should().Equal("12345678");            // Artikelnummer in GTIN-Form zuerst (F2)
        (await Ean(ctrl, "9007646008637")).Nummern.Should().Equal("Y1", "Y2");       // Mehrtreffer (F5)
        (await Ean(ctrl, "1111111111111")).Nummern.Should().BeEmpty();               // kein Treffer -> Client nimmt den bisherigen Weg (F4)
        (await ctrl.GetByEan("  ")).Should().BeOfType<BadRequestResult>();
    }

    [Fact]
    public async Task GetByEan_MehrAls5_More()
    {
        var (ctrl, ctx) = Setup();
        for (var i = 0; i < 6; i++) ctx.Articles.Add(MitEan("E" + i, "40028721"));
        ctx.SaveChanges();

        var (nummern, more) = await Ean(ctrl, "40028721");
        (nummern.Length, more).Should().Be((5, true));
    }

    [Fact]
    public async Task Search_TypGlas_NurGlasUndEuzGruppen()
    {
        var (ctrl, ctx) = Setup();
        SeedArticles(ctx);

        var result = await ctrl.Search(q: "ART", limit: 50, type: "Glas");

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var json = JsonSerializer.Serialize(ok.Value);
        json.Should().Contain("ART-GLAS");
        json.Should().Contain("ART-EUZ");
        json.Should().NotContain("ART-940", "Nicht-Glas-Gruppe darf bei type=Glas nicht vorgeschlagen werden");
    }

    [Fact]
    public async Task Search_OhneTyp_AlleArtikel()
    {
        var (ctrl, ctx) = Setup();
        SeedArticles(ctx);

        var result = await ctrl.Search(q: "ART", limit: 50, type: null);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var json = JsonSerializer.Serialize(ok.Value);
        json.Should().Contain("ART-940");
        json.Should().Contain("ART-GLAS");
        json.Should().Contain("ART-EUZ");
    }
}
