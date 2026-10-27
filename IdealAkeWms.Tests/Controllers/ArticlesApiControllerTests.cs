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
