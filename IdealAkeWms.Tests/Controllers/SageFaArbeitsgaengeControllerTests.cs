using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Services.HierarchischeStruktur;
using IdealAkeWms.Services.SageSdata;
using IdealAkeWms.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Controllers;

/// <summary>v1.99.38 E1a: Einreihen „In Sage anlegen“ — Schalter und Ziel-Stand am Schreibort (ADR 0026), nie ein Sage-Aufruf im Web.</summary>
public class SageFaArbeitsgaengeControllerTests
{
    private readonly ApplicationDbContext _db = TestDbContextFactory.Create(Guid.NewGuid().ToString());
    private readonly Mock<IServiceSettingRepository> _svc = new();
    private readonly Mock<ISageSdataReadClient> _sage = new(MockBehavior.Strict);   // jeder Sage-Aufruf waere ein Fehler

    public SageFaArbeitsgaengeControllerTests()
    {
        _svc.Setup(s => s.GetValueAsync(HierarchischeStrukturKeys.Master)).ReturnsAsync("true");
        _db.OperationConfigs.AddRange(
            new OperationConfig { OperationName = "PU", SageArbeitsgangnummer = "180", CreatedBy = "t", CreatedByWindows = "t" },
            new OperationConfig { OperationName = "PR", SageArbeitsgangnummer = "310", CreatedBy = "t", CreatedByWindows = "t" },
            new OperationConfig { OperationName = "MK", CreatedBy = "t", CreatedByWindows = "t" });
        _db.FaHierarchyNodes.Add(new FaHierarchyNode { HauptFA = 100, SubFA = 100, Arbeitsschritte = "PU MK PR", Artnr = "A1" });
        _db.SaveChanges();
    }

    private void Schalter(bool an) => _svc.Setup(s => s.GetValueAsync(SageFaArbeitsgaenge.ToggleKey)).ReturnsAsync(an ? "true" : "false");

    private SageFaArbeitsgaengeController Controller()
    {
        var c = new SageFaArbeitsgaengeController(new FaHierarchyNodeRepository(_db), new OperationConfigRepository(_db), _svc.Object, _sage.Object,
            new ConfigurationBuilder().Build(), new Microsoft.AspNetCore.DataProtection.EphemeralDataProtectionProvider(), new FakeCurrentUserService(),
            new SageFaArbeitsgangAnlageRepository(_db), NullLogger<SageFaArbeitsgaengeController>.Instance);
        var http = new DefaultHttpContext();
        c.ControllerContext = new ControllerContext { HttpContext = http };
        c.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());
        c.Url = Mock.Of<IUrlHelper>();
        return c;
    }

    [Fact]
    public void NurAdmin() =>
        typeof(SageFaArbeitsgaengeController).GetCustomAttributes(typeof(RequireAdminAccessAttribute), true).Should().NotBeEmpty();

    [Fact]
    public async Task SchalterAus_NichtsEingereiht_Warnung()
    {
        Schalter(false);
        var c = Controller();
        await c.Anlegen(100, new[] { "180" }, null);
        _db.SageFaArbeitsgangAnlagen.Should().BeEmpty();
        c.TempData["WarningMessage"].Should().Be(SageFaArbeitsgaenge.AusText);
    }

    [Fact]
    public async Task ZielStandAbweichend_NichtsEingereiht()
    {
        Schalter(true);
        var c = Controller();
        await c.Anlegen(100, new[] { "180", "999" }, null);   // 999 gehoert nicht (mehr) zum FA
        _db.SageFaArbeitsgangAnlagen.Should().BeEmpty();
        c.TempData["WarningMessage"].Should().Be(SageFaArbeitsgaengeController.StandGeaendert);
    }

    [Fact]
    public async Task Einreihen_JeAgEineZeile_MitReihenfolge_UndAudit_ZweitesMalNichts()
    {
        Schalter(true);
        var c = Controller();
        await c.Anlegen(100, new[] { "180", "310", "180" }, null);

        var rows = _db.SageFaArbeitsgangAnlagen.OrderBy(a => a.Reihenfolge).ToList();
        rows.Select(a => (a.SageArbeitsgangnummer, a.Kuerzel, a.Reihenfolge, a.Status)).Should().Equal(
            ("180", "PU", 1, SageFaArbeitsgangStatus.Offen), ("310", "PR", 3, SageFaArbeitsgangStatus.Offen));
        rows.Should().OnlyContain(a => a.HauptFa == 100 && a.CreatedBy == "Test User" && a.CreatedByWindows == "TEST\\user");
        c.TempData["SuccessMessage"].Should().NotBeNull();

        var c2 = Controller();
        await c2.Anlegen(100, new[] { "180" }, null);
        _db.SageFaArbeitsgangAnlagen.Count().Should().Be(2);
        c2.TempData["WarningMessage"].As<string>().Should().Contain("nichts eingereiht");
    }

    [Fact]
    public async Task FehlerZeile_WirdWiederOffen_MitModifiedAudit()
    {
        Schalter(true);
        _db.SageFaArbeitsgangAnlagen.Add(new SageFaArbeitsgangAnlage
        {
            BelId = 100, HauptFa = 100, Kuerzel = "PU", SageArbeitsgangnummer = "180", Reihenfolge = 1, Status = SageFaArbeitsgangStatus.Fehler,
            Versuche = 4, Fehler = "x", CreatedBy = "t", CreatedByWindows = "t",
        });
        _db.SaveChanges();

        await Controller().Anlegen(100, new[] { "180" }, null);

        var a = _db.SageFaArbeitsgangAnlagen.Single();
        a.Status.Should().Be(SageFaArbeitsgangStatus.Offen);
        a.ModifiedBy.Should().Be("Test User");
        a.ModifiedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Index_FaFilter_Pruefen_LiestAuchDieUnterFas()   // QA F1: Unter-FA stand ohne Lesefehler auf „Sage nicht lesbar“
    {
        Schalter(false);
        _db.FaHierarchyNodes.Add(new FaHierarchyNode { HauptFA = 100, VaterFA = 100, SubFA = 101, Arbeitsschritte = "PU", Artnr = "A2" });
        _db.SaveChanges();
        _svc.Setup(s => s.GetValueAsync("SData:BaseUrl")).ReturnsAsync("https://x");
        _svc.Setup(s => s.GetValueAsync("SData:Dataset")).ReturnsAsync("IDEAL;1");
        _svc.Setup(s => s.GetValueAsync("SData:Username")).ReturnsAsync("u");
        var wheres = new List<string?>();
        _sage.Setup(s => s.QueryAllAsync<SageFaArbeitsgaenge.KopfZeile>(It.IsAny<SdataLeseZiel>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .Callback((SdataLeseZiel _, string _, string _, string? w, string? _, string _, CancellationToken _, int _) => wheres.Add(w))
            .ReturnsAsync(new SdataSeiten<SageFaArbeitsgaenge.KopfZeile>(Array.Empty<SageFaArbeitsgaenge.KopfZeile>(), "k", 1, 0, 0));
        _sage.Setup(s => s.QueryAllAsync<SageFaArbeitsgaenge.PositionZeile>(It.IsAny<SdataLeseZiel>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ReturnsAsync(new SdataSeiten<SageFaArbeitsgaenge.PositionZeile>(Array.Empty<SageFaArbeitsgaenge.PositionZeile>(), "p", 1, 0, 0));

        var vm = (IdealAkeWms.Models.ViewModels.SageFaArbeitsgaengeViewModel)((ViewResult)await Controller().Index(null, 100, pruefen: true)).Model!;

        wheres.Should().ContainSingle().Which.Should().Contain("101");
        vm.SageBanner.Should().BeNull();
        vm.Zeilen.Where(z => z.BelId == 101).Should().NotBeEmpty().And.OnlyContain(z => z.Status == SageFaArbeitsgaenge.FaBeendet);
    }

    [Fact]
    public async Task Index_QueueStatus_UndKnopfNurBeiSchalterAn()
    {
        Schalter(true);
        _db.SageFaArbeitsgangAnlagen.Add(new SageFaArbeitsgangAnlage
        {
            BelId = 100, HauptFa = 100, Kuerzel = "PU", SageArbeitsgangnummer = "180", Reihenfolge = 1, Status = SageFaArbeitsgangStatus.Angelegt,
            CreatedBy = "t", CreatedByWindows = "t",
        });
        _db.SaveChanges();

        var vm = (IdealAkeWms.Models.ViewModels.SageFaArbeitsgaengeViewModel)((ViewResult)await Controller().Index(null, null)).Model!;

        vm.Zeilen.Single(z => z.SageAg == "180").Status.Should().Be(SageFaArbeitsgaenge.Angelegt);
        vm.SchalterAn.Should().BeTrue();
        vm.BearbeiterFehlt.Should().BeTrue();
        vm.Einreihbar.Should().BeEmpty();   // ohne Pruefung „nicht geprueft“ = nicht anlegbar; 180 schon angelegt

        Schalter(false);
        var aus = (IdealAkeWms.Models.ViewModels.SageFaArbeitsgaengeViewModel)((ViewResult)await Controller().Index(null, null)).Model!;
        aus.SchalterAn.Should().BeFalse();
    }
}
