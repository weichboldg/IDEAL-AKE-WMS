using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Helpers;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Controllers;

/// <summary>v1.99.72 (Spec 2026-11-20-bde-liste-isolierungsfraese-pfad-material F3/F4/F14, AK 5/8/21): Pruefregel, file-Link und der
/// einzige Schreibweg Articles/FraesenPfad.</summary>
public class FraesenPfadTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("   ", null)]
    [InlineData(@"  \\idealfs1\Daten$\Produktion\CAM\Gerhard  ", @"\\idealfs1\Daten$\Produktion\CAM\Gerhard")]
    public void Pruefen_gueltig(string? eingabe, string? erwartet)
    {
        var (wert, fehler) = FraesenPfad.Pruefen(eingabe);
        fehler.Should().BeNull();
        wert.Should().Be(erwartet);
    }

    [Theory]
    [InlineData(@"Y:\Gerhard\Listen")]
    [InlineData(@"Gerhard\Listen")]
    [InlineData(@"\\")]
    [InlineData(@"\\\server")]
    [InlineData("\\\\srv\\a\tb")]
    public void Pruefen_ungueltig(string eingabe)
    {
        var (wert, fehler) = FraesenPfad.Pruefen(eingabe);
        wert.Should().BeNull();
        fehler.Should().Be(FraesenPfad.Fehlertext);
    }

    // v1.99.81 (7B): Laufwerk Y: an der Fraese = ...\CAM\Format4 (Gerald 2026-11-26), gleiche Angabe wie das Einmal-Skript
    [Fact]
    public void Fehlertext_NenntFormat4()
    {
        FraesenPfad.Fehlertext.Should().EndWith(@"Y: an der Fräse = \\idealfs1\Daten$\Produktion\CAM\Format4");
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "SQL", "Einmalig"))) dir = dir.Parent;
        var skript = File.ReadAllText(Path.Combine(dir!.FullName, "SQL", "Einmalig", "2026-11-25_v1.99.72_FraesenPfad_Erstimport.sql"));
        skript.Should().Contain(@"THEN N'\\idealfs1\Daten$\Produktion\CAM\Format4\' + SUBSTRING");
    }

    [Fact]
    public void Pruefen_501_Zeichen_abgewiesen_500_gueltig()
    {
        var p500 = @"\\s\" + new string('x', 496);
        p500.Length.Should().Be(500);
        FraesenPfad.Pruefen(p500).Fehler.Should().BeNull();
        FraesenPfad.Pruefen(p500 + "x").Fehler.Should().Be(FraesenPfad.Fehlertext);
    }

    [Theory]
    [InlineData(@"\\idealfs1\Daten$\Produktion\CAM\Gerhard\Listen\EKP\EKP_System_20mm_03_2022_NEU",
        "file://idealfs1/Daten$/Produktion/CAM/Gerhard/Listen/EKP/EKP_System_20mm_03_2022_NEU")]
    [InlineData(@"\\srv\Fräse Teil#1\50%", "file://srv/Fr%C3%A4se%20Teil%231/50%25")]
    [InlineData(@"\\srv\Ordner\", "file://srv/Ordner")]
    public void FileUri_kodiert_und_laesst_Dollar(string unc, string erwartet)
        => FraesenPfad.FileUri(unc).Should().Be(erwartet);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(@"Y:\Gerhard")]
    public void FileUri_ohne_Unc_null(string? wert) => FraesenPfad.FileUri(wert).Should().BeNull();

    private static (ArticlesController ctrl, IdealAkeWms.Data.ApplicationDbContext db) Controller(bool fraesenAktiv = false)
    {
        var db = TestDbContextFactory.Create();
        db.Articles.Add(new Article
        {
            ArticleNumber = "ISO-1", Description = "Isolierung", Matchcode = "MC", Unit = "Stk", ArticleGroup = "G",
            CreatedAt = new DateTime(2026, 1, 1), CreatedBy = "anlage", CreatedByWindows = "anlage",
            ModifiedBy = "alt", ModifiedByWindows = "alt", CamPath = @"\\alt\pfad",
        });
        db.SaveChanges();
        var user = new Mock<ICurrentUserService>();
        user.Setup(u => u.GetDisplayName()).Returns("Pfleger A");
        user.Setup(u => u.GetWindowsUserName()).Returns(@"IDEAL\pflegera");
        var ctrl = new ArticlesController(new ArticleRepository(db), Mock.Of<IStockMovementRepository>(), user.Object,
            Mock.Of<IArticleAttributeRepository>(), Mock.Of<IArticleCategoryRepository>(), Mock.Of<IPlannedConsumptionService>(),
            Mock.Of<IStorageLocationRepository>(), Mock.Of<ICreditorRepository>(),
            Mock.Of<IAppSettingRepository>(s => s.GetValueAsync(AppSettingKeys.FraesenPfadAktiv) == Task.FromResult<string?>(fraesenAktiv ? "true" : null)));
        var http = new DefaultHttpContext();
        ctrl.ControllerContext = new ControllerContext { HttpContext = http };
        ctrl.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());
        var url = new Mock<IUrlHelper>();
        url.Setup(u => u.IsLocalUrl(It.IsAny<string?>())).Returns<string?>(s => s != null && s.StartsWith('/') && !s.StartsWith("//"));
        ctrl.Url = url.Object;
        return (ctrl, db);
    }

    [Fact]
    public async Task Speichern_gueltig_schreibt_nur_CamPath_und_Modified()
    {
        var (ctrl, db) = Controller();
        var r = await ctrl.FraesenPfad(" ISO-1 ", @"  \\idealfs1\Daten$\Produktion\CAM\X  ", "/Articles/Info?articleNumber=ISO-1");

        r.Should().BeOfType<RedirectResult>().Which.Url.Should().Be("/Articles/Info?articleNumber=ISO-1");
        ctrl.TempData["SuccessMessage"].Should().Be("Fräsen-Pfad gespeichert.");
        var a = db.Articles.Single();
        a.CamPath.Should().Be(@"\\idealfs1\Daten$\Produktion\CAM\X");
        a.ModifiedBy.Should().Be("Pfleger A");
        a.ModifiedByWindows.Should().Be(@"IDEAL\pflegera");
        a.ModifiedAt.Should().NotBeNull();
        (a.Description, a.Matchcode, a.Unit, a.ArticleGroup, a.CreatedBy).Should().Be(("Isolierung", "MC", "Stk", "G", "anlage"));
    }

    [Fact]
    public async Task Speichern_leer_setzt_null_und_fremde_ReturnUrl_geht_auf_Info()
    {
        var (ctrl, db) = Controller();
        var r = await ctrl.FraesenPfad("ISO-1", "   ", "https://boese.example/");

        r.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("Info");
        db.Articles.Single().CamPath.Should().BeNull();
    }

    [Fact]
    public async Task Edit_Validierungsfehler_FraesenPfadKarte_bleibt()
    {
        // v1.99.79 F-17: im Fehlerfall Schalter + gespeicherter Pfad wieder setzen (CamPath kommt nie aus dem Edit-POST)
        var (ctrl, db) = Controller(fraesenAktiv: true);
        var id = db.Articles.Single().Id;
        ctrl.ModelState.AddModelError("Article.Description", "Pflicht");

        var r = await ctrl.Edit(id, new ArticleEditViewModel { Article = new Article { Id = id, ArticleNumber = "ISO-1" } });

        var vm = r.Should().BeOfType<ViewResult>().Which.Model.Should().BeOfType<ArticleEditViewModel>().Which;
        vm.Article.CamPath.Should().Be(@"\\alt\pfad");
        ((bool?)ctrl.ViewData["FraesenPfadAktiv"]).Should().BeTrue();
    }

    [Fact]
    public async Task Speichern_ungueltig_aendert_nichts()
    {
        var (ctrl, db) = Controller();
        await ctrl.FraesenPfad("ISO-1", @"Y:\Gerhard", null);

        ctrl.TempData["WarningMessage"].Should().Be("Fräsen-Pfad nicht gespeichert: " + FraesenPfad.Fehlertext);
        var a = db.Articles.Single();
        a.CamPath.Should().Be(@"\\alt\pfad");
        a.ModifiedBy.Should().Be("alt");
    }

    [Fact]
    public async Task Unbekannter_Artikel_Warnung()
    {
        var (ctrl, db) = Controller();
        await ctrl.FraesenPfad("GIBTSNICHT", @"\\s\p", null);

        ctrl.TempData["WarningMessage"].Should().Be("Artikel GIBTSNICHT nicht gefunden – Pfad nicht gespeichert.");
        db.Articles.Single().CamPath.Should().Be(@"\\alt\pfad");
    }

    [Fact]
    public void Aktion_traegt_Schalter_und_Rollenfilter()
    {
        var m = typeof(ArticlesController).GetMethod(nameof(ArticlesController.FraesenPfad))!;
        m.GetCustomAttributes(typeof(IdealAkeWms.Filters.RequireFraesenPfadAktivAttribute), false).Should().HaveCount(1);
        m.GetCustomAttributes(typeof(IdealAkeWms.Filters.RequireFraesenPfadPflegenAccessAttribute), false).Should().HaveCount(1);
        m.GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), false).Should().HaveCount(1);
    }
}
