using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Controllers;

/// <summary>v1.99.71 Spec 2026-11-24-lagerplatz-anlage-wms-nach-sage (F1/F1a/F2/F6/F7) – Web-Seite der Lagerplatz-Anlage in Sage.</summary>
public class SageLagerplatzAnlageControllerTests
{
    private static Mock<ICurrentUserService> User()
    {
        var user = new Mock<ICurrentUserService>();
        user.Setup(u => u.GetDisplayName()).Returns("Anwender Test");
        user.Setup(u => u.GetWindowsUserName()).Returns("AKE\\test");
        user.Setup(u => u.GetDefaultPageSizeAsync()).ReturnsAsync((int?)null);
        return user;
    }

    private static StorageLocationsController Ctrl(IStorageLocationRepository repo, bool an = true, string? stufe = null)
    {
        var settings = new Mock<IServiceSettingRepository>();
        settings.Setup(s => s.GetValueAsync(SageLagerplatzAnlage.ToggleKey)).ReturnsAsync(an ? "true" : "false");
        settings.Setup(s => s.GetValueAsync(SageLagerplatzAnlage.LagertechnikenKey)).ReturnsAsync(stufe);
        var ctrl = new StorageLocationsController(repo, User().Object, settings.Object);
        var http = new DefaultHttpContext();
        ctrl.ControllerContext = new ControllerContext { HttpContext = http };
        ctrl.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());
        return ctrl;
    }

    private static StorageArea Lager(string kennung, bool aktiv = true) => new()
    {
        Lagerkennung = kennung, Bezeichnung = "Lager " + kennung, Lagertechnik = "LTD3HR", IsActive = aktiv,
        CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t",
    };

    private static StorageLocation Platz(string code, int? areaId = null, string source = StorageLocationSource.Manual) => new()
    {
        Code = code, BarcodeValue = code, Zone = code.Split(';')[0], StorageAreaId = areaId, Source = source,
        CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t",
    };

    [Fact]
    public async Task CreateInSage_SchalterAus_GetUndPost_Redirect_MitWarnung_NichtsAngelegt()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.StorageAreas.Add(Lager("TB"));
        await ctx.SaveChangesAsync();
        var ctrl = Ctrl(new StorageLocationRepository(ctx), an: false);

        (await ctrl.CreateInSage()).Should().BeOfType<RedirectToActionResult>();
        ctrl.TempData["WarningMessage"].Should().Be(StorageLocationsController.AusgeschaltetText);
        (await ctrl.CreateInSage(new StorageLocationSageAnlageViewModel { Lagerort = ctx.StorageAreas.Single().Id.ToString() }))
            .Should().BeOfType<RedirectToActionResult>();
        ctx.StorageLocations.Should().BeEmpty();
        ctx.SageStammdatenOutbox.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateInSage_Get_Auswahl_NurAktiveNichtFehlende_MitWartendUndFehlerZusatz()
    {
        using var ctx = TestDbContextFactory.Create();
        var fehlt = Lager("FEHLT");
        fehlt.SageMissingSince = DateTime.Now;
        ctx.StorageAreas.AddRange(Lager("TB"), Lager("INAKTIV", aktiv: false), fehlt, Lager("WARTET"), Lager("KAPUTT"));
        await ctx.SaveChangesAsync();
        var wartet = ctx.StorageAreas.Single(a => a.Lagerkennung == "WARTET");
        var kaputt = ctx.StorageAreas.Single(a => a.Lagerkennung == "KAPUTT");
        SageStammdatenOutbox.Vormerken(ctx, SageStammdatenOutbox.TypLagerort, wartet, new { wartet.Id });
        SageStammdatenOutbox.Vormerken(ctx, SageStammdatenOutbox.TypLagerort, kaputt, new { kaputt.Id });
        await ctx.SaveChangesAsync();
        ctx.SageStammdatenOutbox.Single(o => o.EntityKey == kaputt.Id.ToString()).Status = SageBookingQueueStatus.Fehler;
        await ctx.SaveChangesAsync();

        var vm = (StorageLocationSageAnlageViewModel)((ViewResult)await Ctrl(new StorageLocationRepository(ctx)).CreateInSage()).Model!;

        vm.Lagerorte.Select(o => o.Text.Split(' ')[0]).Should().Equal("KAPUTT", "TB", "WARTET");
        vm.Lagerorte.Single(o => o.Id == wartet.Id).Should().Match<StorageLocationSageAnlageViewModel.LagerortOption>(o => o.Waehlbar && o.Text.EndsWith("(neu, wartet auf Sage)"));
        vm.Lagerorte.Single(o => o.Id == kaputt.Id).Waehlbar.Should().BeFalse();
        vm.LagerAnlageFreigegeben.Should().BeFalse("Stufe leer = Aus");
    }

    [Fact]
    public async Task CreateInSage_KeineLagerorte_Post_Fehler_NichtsAngelegt()
    {
        using var ctx = TestDbContextFactory.Create();
        var ctrl = Ctrl(new StorageLocationRepository(ctx), stufe: SageLagerplatzAnlage.StufeAlle);

        var r = await ctrl.CreateInSage(new StorageLocationSageAnlageViewModel
            { Lagerort = StorageLocationSageAnlageViewModel.NeuesLagerWert, NeueKennung = "NEU", NeueBezeichnung = "x", NeueTechnik = "LTD1BL" });

        r.Should().BeOfType<ViewResult>();
        ctrl.ModelState[string.Empty]!.Errors.Single().ErrorMessage.Should().Be(StorageLocationsController.KeineLagerorteText);
        ctx.StorageAreas.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateInSage_VorhandenesLager_LegtBereichAn_UeberspringtCodeOhneGrossKlein_OutboxJePlatz()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.StorageAreas.Add(Lager("TB"));
        await ctx.SaveChangesAsync();
        var area = ctx.StorageAreas.Single();
        ctx.StorageLocations.Add(Platz("tb;1;2;0"));
        await ctx.SaveChangesAsync();
        var ctrl = Ctrl(new StorageLocationRepository(ctx));

        var r = await ctrl.CreateInSage(new StorageLocationSageAnlageViewModel
        {
            Lagerort = area.Id.ToString(), D1Von = 1, D1Bis = 2, D2Von = 1, D2Bis = 2, D3Von = 0, D3Bis = 0, IstBuchbar = false, SageBuchungErlaubt = true,
        });

        r.Should().BeOfType<RedirectToActionResult>();
        var neu = ctx.StorageLocations.Where(l => l.StorageAreaId == area.Id).OrderBy(l => l.Id).ToList();
        neu.Select(l => l.Code).Should().Equal("TB;1;1;0", "TB;2;1;0", "TB;2;2;0");
        neu.Should().OnlyContain(l => l.Source == StorageLocationSource.Manual && l.Zone == "TB" && l.BarcodeValue == l.Code
            && l.Description == null && l.SageUebergabeStatus == SageUebergabeStatus.Offen && !l.IstBuchbar && l.SageBuchungErlaubt
            && l.CreatedBy == "Anwender Test" && l.CreatedByWindows == "AKE\\test");
        var outbox = ctx.SageStammdatenOutbox.OrderBy(o => o.Id).ToList();
        outbox.Should().HaveCount(3).And.OnlyContain(o => o.EntityType == SageStammdatenOutbox.TypLagerplatz && o.Status == SageBookingQueueStatus.Offen);
        outbox[0].PayloadJson.Should().Contain("\"Lagerkennung\":\"TB\"").And.Contain("\"D1\":1").And.Contain("\"D2\":1").And.Contain("\"D3\":0");
        ctrl.TempData["WarningMessage"].Should().Be("3 Lagerplätze angelegt, der Dienst legt sie in Sage an, 1 übersprungen (Code im WMS schon vorhanden: TB;1;2;0).");
    }

    [Fact]
    public async Task CreateInSage_AlleCodesVorhanden_NichtsAngelegt_Warnung()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.StorageAreas.Add(Lager("TB"));
        ctx.StorageLocations.Add(Platz("TB;1;0;0"));
        await ctx.SaveChangesAsync();
        var ctrl = Ctrl(new StorageLocationRepository(ctx));

        await ctrl.CreateInSage(new StorageLocationSageAnlageViewModel { Lagerort = ctx.StorageAreas.Single().Id.ToString() });

        ctx.StorageLocations.Should().HaveCount(1);
        ctx.SageStammdatenOutbox.Should().BeEmpty();
        ctrl.TempData["WarningMessage"].Should().Be("0 Lagerplätze angelegt, 1 übersprungen (Code im WMS schon vorhanden: TB;1;0;0).");
    }

    [Fact]
    public async Task CreateInSage_LagerMitOutboxFehler_Abgelehnt()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.StorageAreas.Add(Lager("KAPUTT"));
        await ctx.SaveChangesAsync();
        var area = ctx.StorageAreas.Single();
        SageStammdatenOutbox.Vormerken(ctx, SageStammdatenOutbox.TypLagerort, area, new { area.Id });
        await ctx.SaveChangesAsync();
        ctx.SageStammdatenOutbox.Single().Status = SageBookingQueueStatus.Fehler;
        await ctx.SaveChangesAsync();
        var ctrl = Ctrl(new StorageLocationRepository(ctx));

        var r = await ctrl.CreateInSage(new StorageLocationSageAnlageViewModel { Lagerort = area.Id.ToString() });

        r.Should().BeOfType<ViewResult>();
        ctrl.ModelState.IsValid.Should().BeFalse();
        ctx.StorageLocations.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0, 1, 0, 0, 0, 0)]        // D1 ab 1
    [InlineData(1, 10000, 0, 0, 0, 0)]    // > 9999
    [InlineData(2, 1, 0, 0, 0, 0)]        // von > bis
    [InlineData(1, 1, 0, 0, 5, 4)]        // von > bis D3
    [InlineData(1, 3, 1, 10, 1, 7)]       // 3*10*7 = 210 > 200
    public async Task CreateInSage_UngueltigerBereich_Abgelehnt(int d1v, int d1b, int d2v, int d2b, int d3v, int d3b)
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.StorageAreas.Add(Lager("TB"));
        await ctx.SaveChangesAsync();
        var ctrl = Ctrl(new StorageLocationRepository(ctx));

        var r = await ctrl.CreateInSage(new StorageLocationSageAnlageViewModel
            { Lagerort = ctx.StorageAreas.Single().Id.ToString(), D1Von = d1v, D1Bis = d1b, D2Von = d2v, D2Bis = d2b, D3Von = d3v, D3Bis = d3b });

        r.Should().BeOfType<ViewResult>();
        ctrl.ModelState.IsValid.Should().BeFalse();
        ctx.StorageLocations.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateInSage_Genau200Plaetze_Erlaubt()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.StorageAreas.Add(Lager("TB"));
        await ctx.SaveChangesAsync();
        var ctrl = Ctrl(new StorageLocationRepository(ctx));

        await ctrl.CreateInSage(new StorageLocationSageAnlageViewModel
            { Lagerort = ctx.StorageAreas.Single().Id.ToString(), D1Von = 1, D1Bis = 2, D2Von = 1, D2Bis = 10, D3Von = 1, D3Bis = 10 });

        ctx.StorageLocations.Should().HaveCount(200);
        ctx.SageStammdatenOutbox.Should().HaveCount(200);
    }

    [Fact]
    public async Task CreateInSage_NeuesLager_Mitschnitt_Platzverwaltet_LagerUndPlaetze_JeOutbox()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.StorageAreas.Add(Lager("TB"));
        await ctx.SaveChangesAsync();
        var ctrl = Ctrl(new StorageLocationRepository(ctx), stufe: SageLagerplatzAnlage.StufeMitschnitt);

        await ctrl.CreateInSage(new StorageLocationSageAnlageViewModel
        {
            Lagerort = StorageLocationSageAnlageViewModel.NeuesLagerWert, NeueKennung = " BL1 ", NeueBezeichnung = " Blocklager 1 ",
            NeueTechnik = "LTD1BL", Platzverwaltet = true, D1Von = 1, D1Bis = 2,
        });

        var lager = ctx.StorageAreas.Single(a => a.Lagerkennung == "BL1");
        (lager.Bezeichnung, lager.Lagertechnik, lager.IstDispoLager, lager.IsActive, lager.SageMissingSince)
            .Should().Be(("Blocklager 1", "LTD1BL", true, true, (DateTime?)null));
        ctx.StorageLocations.Where(l => l.StorageAreaId == lager.Id).Select(l => l.Code).Should().BeEquivalentTo("BL1;1;0;0", "BL1;2;0;0");
        var outbox = ctx.SageStammdatenOutbox.OrderBy(o => o.Id).ToList();
        outbox.Select(o => o.EntityType).Should().Equal(SageStammdatenOutbox.TypLagerort, SageStammdatenOutbox.TypLagerplatz, SageStammdatenOutbox.TypLagerplatz);
        outbox[0].PayloadJson.Should().Contain("\"Lagertechnik\":\"LTD1BL\"");
        ctrl.TempData["SuccessMessage"].Should().Be("Lager BL1 erfasst, wird vom Dienst in Sage angelegt; 2 Lagerplätze angelegt, der Dienst legt sie in Sage an.");
    }

    [Fact]
    public async Task CreateInSage_NeuesLager_NichtPlatzverwaltet_NurLager()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.StorageAreas.Add(Lager("TB"));
        await ctx.SaveChangesAsync();
        var ctrl = Ctrl(new StorageLocationRepository(ctx), stufe: SageLagerplatzAnlage.StufeMitschnitt);

        await ctrl.CreateInSage(new StorageLocationSageAnlageViewModel
            { Lagerort = "neu", NeueKennung = "ST1", NeueBezeichnung = "Standard", NeueTechnik = "LTD0ST", D1Von = 0, D1Bis = 0 });

        ctx.StorageAreas.Should().Contain(a => a.Lagerkennung == "ST1");
        ctx.StorageLocations.Should().BeEmpty();
        ctx.SageStammdatenOutbox.Single().EntityType.Should().Be(SageStammdatenOutbox.TypLagerort);
        ctrl.TempData["SuccessMessage"].Should().Be("Lager ST1 erfasst, wird vom Dienst in Sage angelegt; 0 Lagerplätze angelegt.");
    }

    [Theory]
    [InlineData(null, "BL1", "LTD1BL", false, "Lagerort")]               // Stufe Aus
    [InlineData("Mitschnitt", "tb", "LTD1BL", false, "NeueKennung")]     // Kennung schon da (ohne Gross/Klein)
    [InlineData("Mitschnitt", "A B", "LTD1BL", false, "NeueKennung")]    // Leerzeichen
    [InlineData("Mitschnitt", "A;B", "LTD1BL", false, "NeueKennung")]    // Semikolon
    [InlineData("Mitschnitt", "ELFZEICHEN1", "LTD1BL", false, "NeueKennung")]
    [InlineData("Mitschnitt", "HR1", "LTD3HR", false, "NeueTechnik")]    // Technik nicht in Stufe
    [InlineData("Mitschnitt", "ST1", "LTD0ST", true, "Platzverwaltet")]  // Standardtechnik ohne Plaetze
    [InlineData("Mitschnitt", "X1", "LTD1BL", false, "NeueKennung")]     // Basis-Code X1;0;0;0 schon vorhanden
    public async Task CreateInSage_NeuesLager_Ungueltig_Abgelehnt(string? stufe, string kennung, string technik, bool platzverwaltet, string feld)
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.StorageAreas.Add(Lager("TB"));
        ctx.StorageLocations.Add(Platz("X1;0;0;0"));
        await ctx.SaveChangesAsync();
        var ctrl = Ctrl(new StorageLocationRepository(ctx), stufe: stufe);

        var r = await ctrl.CreateInSage(new StorageLocationSageAnlageViewModel
            { Lagerort = "neu", NeueKennung = kennung, NeueBezeichnung = "Bez", NeueTechnik = technik, Platzverwaltet = platzverwaltet });

        r.Should().BeOfType<ViewResult>();
        ctrl.ModelState[feld]!.Errors.Should().NotBeEmpty();
        ctx.StorageAreas.Should().HaveCount(1);
        ctx.SageStammdatenOutbox.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateInSage_UniqueVerletzung_ModelStateFehler_StattAbsturz()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.StorageAreas.Add(Lager("TB"));
        await ctx.SaveChangesAsync();
        var echt = new StorageLocationRepository(ctx);
        var repo = new Mock<IStorageLocationRepository>();
        repo.Setup(r => r.GetStorageAreasAsync()).Returns(echt.GetStorageAreasAsync);
        repo.Setup(r => r.GetOffeneLagerUebergabenAsync()).Returns(echt.GetOffeneLagerUebergabenAsync);
        repo.Setup(r => r.GetAllCodesAsync()).ReturnsAsync(new List<string>());
        repo.Setup(r => r.AddSageAnlageAsync(It.IsAny<StorageArea?>(), It.IsAny<IReadOnlyList<StorageLocation>>()))
            .ThrowsAsync(new DbUpdateException("unique"));
        var ctrl = Ctrl(repo.Object);

        var r = await ctrl.CreateInSage(new StorageLocationSageAnlageViewModel { Lagerort = ctx.StorageAreas.Single().Id.ToString() });

        r.Should().BeOfType<ViewResult>();
        ctrl.ModelState[string.Empty]!.Errors.Single().ErrorMessage.Should().Be(StorageLocationsController.InzwischenAngelegtText);
    }

    [Fact]
    public async Task Create_IgnoriertStorageAreaId_KeinSageAnlagePlatzPerUeberposting()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.StorageAreas.Add(Lager("TB"));
        await ctx.SaveChangesAsync();
        var ctrl = Ctrl(new StorageLocationRepository(ctx));

        await ctrl.Create(new StorageLocation { Code = "W-01", StorageAreaId = ctx.StorageAreas.Single().Id });

        ctx.StorageLocations.Single().StorageAreaId.Should().BeNull();
    }

    [Fact]
    public async Task Edit_SageAnlagePlatz_CodeZoneBezeichnungGesperrt_AktivUndBuchbarAenderbar()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.StorageAreas.Add(Lager("TB"));
        await ctx.SaveChangesAsync();
        var platz = Platz("TB;1;0;0", ctx.StorageAreas.Single().Id);
        ctx.StorageLocations.Add(platz);
        await ctx.SaveChangesAsync();
        var ctrl = Ctrl(new StorageLocationRepository(ctx));

        var r = await ctrl.Edit(platz.Id, new StorageLocation
            { Id = platz.Id, Code = "HACK", Zone = "Z", Description = "D", IsActive = false, IstBuchbar = false, Capacity = 3 });

        r.Should().BeOfType<RedirectToActionResult>();
        var s = ctx.StorageLocations.Single();
        (s.Code, s.Zone, s.Description, s.IsActive, s.IstBuchbar, s.Capacity, s.StorageAreaId)
            .Should().Be(("TB;1;0;0", "TB", (string?)null, false, false, (decimal?)3, platz.StorageAreaId));
        s.ModifiedBy.Should().Be("Anwender Test");
    }

    [Fact]
    public async Task ErneutSenden_Platz_FehlerWirdOffen_OutboxZurueckgesetzt_MitAudit()
    {
        using var ctx = TestDbContextFactory.Create();
        var platz = Platz("TB;1;0;0");
        platz.SageUebergabeStatus = SageUebergabeStatus.Fehler;
        platz.SageUebergabeFehler = "Sage-Fehler 50012";
        ctx.StorageLocations.Add(platz);
        await ctx.SaveChangesAsync();
        SageStammdatenOutbox.Vormerken(ctx, SageStammdatenOutbox.TypLagerplatz, platz, new { platz.Id });
        await ctx.SaveChangesAsync();
        var o = ctx.SageStammdatenOutbox.Single();
        (o.Status, o.AttemptCount, o.LastError) = (SageBookingQueueStatus.Fehler, 2, "Sage-Fehler 50012");
        await ctx.SaveChangesAsync();
        var ctrl = Ctrl(new StorageLocationRepository(ctx));

        await ctrl.ErneutSenden(platz.Id);

        (o.Status, o.AttemptCount, o.LastError, o.ModifiedBy).Should().Be((SageBookingQueueStatus.Offen, 0, (string?)null, (string?)"Anwender Test"));
        (platz.SageUebergabeStatus, platz.SageUebergabeFehler, platz.ModifiedByWindows).Should().Be((SageUebergabeStatus.Offen, (string?)null, (string?)"AKE\\test"));
        ctrl.TempData["SuccessMessage"].Should().NotBeNull();

        await ctrl.ErneutSenden(platz.Id); // zweites Mal: nichts mehr im Status Fehler
        ctrl.TempData["WarningMessage"].Should().NotBeNull();
    }

    [Fact]
    public async Task LagerErneutSenden_SchalterAus_NichtsGeaendert()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.StorageAreas.Add(Lager("KAPUTT"));
        await ctx.SaveChangesAsync();
        var area = ctx.StorageAreas.Single();
        SageStammdatenOutbox.Vormerken(ctx, SageStammdatenOutbox.TypLagerort, area, new { area.Id });
        await ctx.SaveChangesAsync();
        ctx.SageStammdatenOutbox.Single().Status = SageBookingQueueStatus.Fehler;
        await ctx.SaveChangesAsync();

        await Ctrl(new StorageLocationRepository(ctx), an: false).LagerErneutSenden(area.Id);
        ctx.SageStammdatenOutbox.Single().Status.Should().Be(SageBookingQueueStatus.Fehler);

        await Ctrl(new StorageLocationRepository(ctx)).LagerErneutSenden(area.Id);
        ctx.SageStammdatenOutbox.Single().Status.Should().Be(SageBookingQueueStatus.Offen);
    }

    [Fact]
    public async Task Index_ZeigtWartendeLager_UndSchalter()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.StorageAreas.Add(Lager("WARTET"));
        await ctx.SaveChangesAsync();
        var area = ctx.StorageAreas.Single();
        SageStammdatenOutbox.Vormerken(ctx, SageStammdatenOutbox.TypLagerort, area, new { area.Id });
        await ctx.SaveChangesAsync();
        var ctrl = Ctrl(new StorageLocationRepository(ctx));

        await ctrl.Index();

        ((bool)ctrl.ViewBag.SageAnlageAktiv).Should().BeTrue();
        var lager = (List<(StorageArea Area, SageStammdatenOutboxItem Eintrag)>)ctrl.ViewBag.LagerUebergaben;
        lager.Single().Area.Lagerkennung.Should().Be("WARTET");
    }

    [Theory]
    [InlineData("TB;1;2;3", "TB", true, 1, 2, 3)]
    [InlineData("tb;0;0;0", "TB", true, 0, 0, 0)]
    [InlineData("TB;1;2", "TB", false, 0, 0, 0)]
    [InlineData("XX;1;2;3", "TB", false, 0, 0, 0)]
    [InlineData("TB;-1;2;3", "TB", false, 0, 0, 0)]
    [InlineData("TB; 1;2;3", "TB", false, 0, 0, 0)]
    public void TryParseCode_NurExaktesMuster(string code, string lager, bool ok, int d1, int d2, int d3)
    {
        SageLagerplatzAnlage.TryParseCode(code, lager, out var a, out var b, out var c).Should().Be(ok);
        if (ok) (a, b, c).Should().Be((d1, d2, d3));
    }

    [Fact]
    public void Koordinaten_D1Aussen_D3Innen_UndStufen()
    {
        SageLagerplatzAnlage.Koordinaten(1, 2, 0, 0, 1, 2).Should().Equal((1, 0, 1), (1, 0, 2), (2, 0, 1), (2, 0, 2));
        SageLagerplatzAnlage.Anzahl(1, 9999, 0, 9999, 0, 9999).Should().Be(9999L * 10000 * 10000);
        SageLagerplatzAnlage.TechnikenDerStufe("Aus").Should().BeEmpty();
        SageLagerplatzAnlage.TechnikenDerStufe("unbekannt").Should().BeEmpty();
        SageLagerplatzAnlage.TechnikenDerStufe(" Mitschnitt ").Should().Equal("LTD0ST", "LTD1BL");
        SageLagerplatzAnlage.TechnikenDerStufe("Alle").Should().HaveCount(5);
    }
}
