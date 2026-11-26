using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Controllers;

/// <summary>
/// Oberflaeche des gespeicherten Beschichtungsauftrags (Spec 2026-10-06-beschichtungsauftrag-ausbau-teil-4, v1.51.0).
/// Der Controller laeuft gegen den ECHTEN Teil-3-Workflow auf EF InMemory — nur Rendering, PDF, Barcode und Benutzer sind
/// gemockt. Jeder Schreib-Test nutzt einen frischen Controller (ein Request = ein Workflow-Aufruf, wie im Betrieb).
/// TS-83.6 (Parallelitaet) ist hier mit veralteter RowVersion und doppelter Positionsvergabe abgedeckt; die echte
/// Serializable-Transaktion bleibt Manual-UAT.
/// </summary>
public class BeschichtungsauftraegeControllerTests
{
    private const int HauptFa = 4711;

    private readonly ApplicationDbContext _db = TestDbContextFactory.Create(Guid.NewGuid().ToString());
    private readonly Mock<ICurrentUserService> _user = new();
    private readonly Mock<IRazorViewRenderer> _renderer = new();
    private readonly Mock<IPdfRenderService> _pdf = new();
    private readonly Mock<IUserViewPreferenceRepository> _viewPrefs = new();

    public BeschichtungsauftraegeControllerTests()
    {
        _user.Setup(u => u.GetDisplayName()).Returns("Tester");
        _user.Setup(u => u.GetWindowsUserName()).Returns(@"DOM\tester");
        _user.Setup(u => u.GetCurrentAppUserId()).Returns(7);
        _user.Setup(u => u.GetDefaultPageSizeAsync()).ReturnsAsync((int?)null);
        _user.Setup(u => u.HasBeschichtungsauftragWriteAccessAsync()).ReturnsAsync(true);

        for (var p = 1; p <= 3; p++)
            _db.FaHierarchyNodes.Add(new FaHierarchyNode
            {
                HauptFA = HauptFa, VaterFA = 100, SubFA = 0, Position = p, Beschichtet = true, Sollmenge = p,
                Artnr = p == 3 ? "A1" : "A" + p, HauptArtnr = "H", Matchcode = "MC" + p, Breite = 500m, Hoehe = 20m, Tiefe = p == 3 ? 4m : 3m,
            });
        _db.FaHierarchyOrderInfos.Add(new FaHierarchyOrderInfo { HauptFA = HauptFa, Dienstleister = "Huber GmbH", RAL = "9016" });
        _db.SaveChanges();
    }

    /// <summary>Frischer Controller je Request (eigene Workflow-Instanz auf demselben Context).</summary>
    private BeschichtungsauftraegeController Ctrl(string? query = null)
    {
        var repo = new BeschichtungsauftragRepository(_db);
        var wf = new BeschichtungsauftragWorkflow(repo, new FaHierarchyNodeRepository(_db), new FaHierarchyOrderInfoRepository(_db),
            IdealAkeWms.Tests.Helpers.FlacheQuelleTestHelper.Hierarchisch(), _user.Object, TimeProvider.System, NullLogger<BeschichtungsauftragWorkflow>.Instance);
        var users = new Mock<IUserRepository>();
        users.Setup(u => u.GetAllWithRolesAsync()).ReturnsAsync(new List<User> { new() { Id = 7, Name = "Tester" } });
        var barcode = new Mock<IBarcodeService>();
        barcode.Setup(b => b.EncodeCode128Base64(It.IsAny<string>())).Returns<string>(s => "bc:" + s);
        var pdfStatus = new PdfRenderStatus(new ConfigurationBuilder().Build(), NullLogger<PdfRenderStatus>.Instance);

        var c = new BeschichtungsauftraegeController(repo, wf, new FaHierarchyOrderInfoRepository(_db), users.Object, _user.Object,
            barcode.Object, _renderer.Object, _pdf.Object, pdfStatus, _viewPrefs.Object);
        var http = new DefaultHttpContext();
        if (query != null) http.Request.QueryString = new QueryString(query);
        c.ControllerContext = new ControllerContext { HttpContext = http };
        c.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());
        return c;
    }

    private static string Rv(Beschichtungsauftrag a) => Convert.ToBase64String(a.RowVersion);

    private Beschichtungsauftrag Laden(int id)
    {
        _db.ChangeTracker.Clear();
        return _db.Beschichtungsauftraege.AsNoTracking().Include(a => a.Positionen).Single(a => a.Id == id);
    }

    /// <summary>Alle drei Positionen angehakt (v1.99.41 A3: Anlage nur mit Auswahl).</summary>
    private static readonly string[] Alle = { "100|0|1", "100|0|2", "100|0|3" };

    private async Task<Beschichtungsauftrag> Anlegen()
    {
        var c = Ctrl();
        var r = (RedirectToActionResult)await c.Create(HauptFa, null, Alle);
        r.ActionName.Should().Be("Details", c.TempData["WarningMessage"]?.ToString());
        return Laden((int)r.RouteValues!["id"]!);
    }

    private async Task<Beschichtungsauftrag> Status(Beschichtungsauftrag a, BeschichtungsauftragStatus ziel)
    {
        var c = Ctrl();
        await c.Status(a.Id, ziel, Rv(a));
        c.TempData.ContainsKey("WarningMessage").Should().BeFalse(c.TempData["WarningMessage"]?.ToString());
        return Laden(a.Id);
    }

    private static string? Warnung(Controller c) => c.TempData["WarningMessage"] as string;

    // ===================== Anlegen (Anforderung 2, AK 1/2) =====================

    [Fact]
    public async Task Create_Erfolg_RedirectDetail_MitNummer()
    {
        var c = Ctrl();
        var r = (RedirectToActionResult)await c.Create(HauptFa, null, Alle);

        r.ActionName.Should().Be("Details");
        var a = Laden((int)r.RouteValues!["id"]!);
        c.TempData["SuccessMessage"].Should().BeOfType<string>().Which.Should().Contain(a.Nummer).And.Contain("3 Positionen");
        a.Dienstleister.Should().Be("Huber GmbH", "Erstauftrag vorbelegt aus der PPS-View");
    }

    [Theory]
    [InlineData(null, "Summiert")]
    [InlineData("Index", "Index")]
    public async Task Create_AllesVergeben_WarnungZurueckZurVorschlagsliste(string? zurueck, string ziel)
    {
        await Anlegen();
        var c = Ctrl();

        var r = (RedirectToActionResult)await c.Create(HauptFa, zurueck, Alle);

        (r.ControllerName, r.ActionName).Should().Be(("FaHierarchyBeschichtung", ziel));
        Warnung(c).Should().Contain("bereits vergeben");
        _db.Beschichtungsauftraege.Count().Should().Be(1);
    }

    // ===================== Anlage nur mit Auswahl (v1.99.41 A1-A4, AK 1-6) =====================

    [Fact]
    public async Task Create_NichtsAngehakt_AlleFreienOffen()
    {
        // v1.99.65 B1: ohne Haken alle freien beschichteten Positionen, offen (Verpackt false)
        var c = Ctrl();

        var r = (RedirectToActionResult)await c.Create(HauptFa, "Index", null, new[] { " " }, new[] { "" }, new[] { "" });

        r.ActionName.Should().Be("Details", Warnung(c));
        var a = Laden((int)r.RouteValues!["id"]!);
        a.Positionen.Should().HaveCount(3).And.OnlyContain(p => !p.Verpackt);
        (Warnung(c) ?? "").Should().NotContain("Keine Position angehakt");
    }

    [Fact]
    public async Task Create_ZweiVonDrei_GenauDieseVerpackt_RestBleibtFrei()
    {
        var r = (RedirectToActionResult)await Ctrl().Create(HauptFa, null, new[] { "100|0|1", "100|0|3" });

        var a = Laden((int)r.RouteValues!["id"]!);
        a.Positionen.Select(p => (p.Position, p.Verpackt)).Should().BeEquivalentTo(new[] { ((int?)1, true), ((int?)3, true) });
        var b = Laden((int)((RedirectToActionResult)await Ctrl().Create(HauptFa, null, new[] { "100|0|2" })).RouteValues!["id"]!);
        b.Positionen.Single().Position.Should().Be(2);
    }

    [Fact]
    public async Task Create_SummiertArtnr_NimmtAlleFreienDieserArtnr_TeilweiseVergeben()
    {
        await Ctrl().Create(HauptFa, null, new[] { "100|0|1" });            // A1 Pos 1 vergeben, Pos 3 (auch A1) frei

        var r = (RedirectToActionResult)await Ctrl().Create(HauptFa, null, null, new[] { "A1" });

        Laden((int)r.RouteValues!["id"]!).Positionen.Select(p => p.Position).Should().Equal(3);
        var c = Ctrl();
        await c.Create(HauptFa, null, null, new[] { "A1" });
        Warnung(c).Should().Contain("Artnr A1: keine freie Position mehr");
    }

    [Fact]
    public async Task Create_TeilweiseUngueltig_GanzAbgelehnt_NenntVergabe()
    {
        var erster = await Anlegen();
        var c = Ctrl();

        await c.Create(HauptFa, null, new[] { "100|0|2", "100|0|9" });

        Warnung(c).Should().Contain(erster.Nummer).And.Contain("nicht im Cache");
        _db.Beschichtungsauftraege.Count().Should().Be(1);
    }

    [Fact]
    public async Task Create_NurFreiePosition_OhneFa_Menge3()
    {
        var art = new Article { ArticleNumber = "F-1", Matchcode = "Muster" };
        _db.Articles.Add(art);
        _db.SaveChanges();

        var r = (RedirectToActionResult)await Ctrl().Create(HauptFa, null, null, null, new[] { art.Id.ToString() }, new[] { "3" });

        var p = Laden((int)r.RouteValues!["id"]!).Positionen.Single();
        (p.OhneFa, p.Artnr, p.Matchcode, p.Menge, p.Verpackt).Should().Be((true, "F-1", "Muster", 3m, true));
        var c = Ctrl();
        await c.Create(HauptFa, null, null, null, new[] { art.Id.ToString() }, new[] { "0" });
        Warnung(c).Should().Contain("Menge muss größer 0");
    }

    // ===================== Bearbeiten (Antwort 7, Review-Fokus 1) =====================

    [Fact]
    public async Task Bearbeiten_LeereDaten_Null_MengeMitKommaOderPunkt()
    {
        var a = await Anlegen();
        var p = a.Positionen.OrderBy(x => x.Position).ToList();
        var c = Ctrl();

        await c.Bearbeiten(a.Id, "  Neu AG ", "", null, null,
            new Dictionary<int, string> { [p[0].Id] = "2,5", [p[1].Id] = "7.25" }, new[] { p[2].Id }, Rv(a));

        Warnung(c).Should().BeNull();
        var b = Laden(a.Id);
        (b.Dienstleister, b.Ral, b.StartBeschichtung, b.BeschichtenRetour).Should().Be(("Neu AG", (string?)null, (DateTime?)null, (DateTime?)null));
        b.Positionen.Select(x => x.Menge).Should().BeEquivalentTo(new[] { 2.5m, 7.25m });
    }

    [Fact]
    public async Task Bearbeiten_UngueltigesDatum_KeineAenderung()
    {
        var a = await Anlegen();
        var c = Ctrl();
        c.ModelState.AddModelError("startBeschichtung", "ungueltig");

        await c.Bearbeiten(a.Id, "Neu AG", null, null, null, null, null, Rv(a));

        Warnung(c).Should().Contain("startBeschichtung");
        Laden(a.Id).Dienstleister.Should().Be("Huber GmbH");
    }

    [Fact]
    public async Task Bearbeiten_MengeKeineZahl_KeineAenderung()
    {
        var a = await Anlegen();
        var c = Ctrl();

        await c.Bearbeiten(a.Id, "Neu AG", null, null, null, new Dictionary<int, string> { [a.Positionen.First().Id] = "abc" }, null, Rv(a));

        Warnung(c).Should().Contain("abc");
        Laden(a.Id).Dienstleister.Should().Be("Huber GmbH");
    }

    [Fact]
    public async Task Bearbeiten_NachUebergeben_Abgelehnt()
    {
        var a = await Status(await Anlegen(), BeschichtungsauftragStatus.Uebergeben);
        var c = Ctrl();

        await c.Bearbeiten(a.Id, "Neu AG", null, null, null, null, null, Rv(a));

        Warnung(c).Should().NotBeNullOrEmpty();
        Laden(a.Id).Dienstleister.Should().Be("Huber GmbH");
    }

    // ===================== TS-83.6 / AK 10: Parallelitaet =====================

    [Fact]
    public async Task Status_VeralteteRowVersion_Konfliktmeldung_StatusBleibt()
    {
        var a = await Anlegen();
        var c = Ctrl();

        await c.Status(a.Id, BeschichtungsauftragStatus.Uebergeben, Convert.ToBase64String(new byte[] { 9, 9, 9 }));

        Warnung(c).Should().Contain("inzwischen von jemand anderem geändert");
        Laden(a.Id).Status.Should().Be(BeschichtungsauftragStatus.Entwurf);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("kein base64!")]
    public async Task Status_FehlendeOderKaputteRowVersion_VeralteterStand(string? rowVersion)
    {
        var a = await Anlegen();
        var c = Ctrl();

        await c.Status(a.Id, BeschichtungsauftragStatus.Uebergeben, rowVersion);

        Warnung(c).Should().Contain("neu laden");
        Laden(a.Id).Status.Should().Be(BeschichtungsauftragStatus.Entwurf);
    }

    [Fact]
    public async Task DieselbeFreiePosition_InZweiEntwuerfe_ZweiterErhaeltWarnung()
    {
        // Entwurf A mit allen, dann Pos 2+3 entfernt → Entwurf B nimmt 2+3, entfernt 3 → Pos 3 frei, A und B offen.
        var a = await Anlegen();
        var c = Ctrl();
        await c.Bearbeiten(a.Id, "Huber GmbH", null, null, null, null,
            a.Positionen.Where(p => p.Position != 1).Select(p => p.Id).ToArray(), Rv(a));
        var b = Laden((int)((RedirectToActionResult)await Ctrl().Create(HauptFa, null, new[] { "100|0|2", "100|0|3" })).RouteValues!["id"]!);
        await Ctrl().Bearbeiten(b.Id, "Huber GmbH", null, null, null, null, b.Positionen.Where(p => p.Position == 3).Select(p => p.Id).ToArray(), Rv(b));
        a = Laden(a.Id);
        b = Laden(b.Id);
        const string pos3 = "100|0|3";

        var c1 = Ctrl();
        await c1.Bearbeiten(a.Id, null, null, null, null, null, null, Rv(a), nurPositionen: true, schluessel: new[] { pos3 });
        var c2 = Ctrl();
        await c2.Bearbeiten(b.Id, null, null, null, null, null, null, Rv(b), nurPositionen: true, schluessel: new[] { pos3 });

        c1.TempData["SuccessMessage"].Should().NotBeNull(Warnung(c1));
        Warnung(c2).Should().Contain("vergeben");
        Laden(b.Id).Positionen.Should().NotContain(p => p.Position == 3);
    }

    // ===================== Positionen hinzufuegen (Anforderung 4a, Review-Fokus 2) =====================

    [Theory]
    [InlineData("abc")]
    [InlineData("1|x|2")]
    [InlineData("1|2")]
    public async Task PositionenHinzufuegen_UngueltigerSchluessel_Warnung(string schluessel)
    {
        var a = await Anlegen();
        var c = Ctrl();

        await c.Bearbeiten(a.Id, null, null, null, null, null, null, Rv(a), nurPositionen: true, schluessel: new[] { schluessel });

        Warnung(c).Should().Contain("ungültiger Positionsschlüssel");
    }

    /// <summary>v1.99.41 A4/A7: freie Zeilen paarweise; leere Zeile uebersprungen, halbe Zeile = Fehler (nie still verworfen).</summary>
    [Theory]
    [InlineData(new[] { "5", "" }, new[] { "3", "" }, null)]
    [InlineData(new[] { "" }, new[] { "3" }, "kein Artikel")]
    [InlineData(new[] { "5" }, new[] { "abc" }, "keine Zahl")]
    [InlineData(new[] { "5" }, new[] { "" }, "keine Zahl")]
    public void FreiePositionen_Formularzeilen(string[] artikel, string[] mengen, string? fehlerText)
    {
        var ok = BeschichtungsauftraegeController.TryFreiePositionen(artikel, mengen, out var frei, out var fehler);

        ok.Should().Be(fehlerText is null);
        if (fehlerText is null) frei.Should().Equal(new FreiePosition(5, 3m));
        else fehler.Should().Contain(fehlerText);
    }

    [Fact]
    public async Task Bearbeiten_Uebergeben_VerpackteEntfernen_Warnung_NichtsGeaendert()
    {
        // v1.99.65 (Antwort R-2): in Uebergeben nur nicht verpackte Positionen entfernbar
        var a = await Status(await Anlegen(), BeschichtungsauftragStatus.Uebergeben);
        var p1 = a.Positionen.First();
        var c = Ctrl();

        await c.Bearbeiten(a.Id, null, null, null, null, null, new[] { p1.Id }, Rv(a), nurPositionen: true, hakenGesendet: true,
            verpackt: a.Positionen.Where(p => p.Id != p1.Id).Select(p => p.Id).ToArray());

        Warnung(c).Should().Contain("nur nicht verpackte");
        Laden(a.Id).Positionen.Should().HaveCount(3).And.OnlyContain(p => p.Verpackt);
    }

    [Fact]
    public async Task Bearbeiten_ZielUebergeben_AlleEntfernt_Warnung_NichtsGespeichert()
    {
        // v1.99.67 F1: auch ohne JS bzw. von Hand — kein Uebergeben mit 0 Positionen, nichts gespeichert
        var a = await Anlegen();
        var c = Ctrl();

        await c.Bearbeiten(a.Id, null, null, null, null, null, a.Positionen.Select(p => p.Id).ToArray(), Rv(a), nurPositionen: true,
            hakenGesendet: true, ziel: BeschichtungsauftragStatus.Uebergeben);

        Warnung(c).Should().Contain("0 Positionen");
        var geladen = Laden(a.Id);
        (geladen.Status, geladen.Positionen.Count).Should().Be((BeschichtungsauftragStatus.Entwurf, 3));
    }

    [Fact]
    public async Task Bearbeiten_Uebergeben_HakenUndFreiePosition_KopfUnveraendert()
    {
        var a = await Anlegen();
        await Ctrl().Status(a.Id, BeschichtungsauftragStatus.Uebergeben, Rv(a));
        a = Laden(a.Id);
        var art = new Article { ArticleNumber = "F-9" };
        _db.Articles.Add(art);
        _db.SaveChanges();
        var p1 = a.Positionen.First();
        var c = Ctrl();

        await c.Bearbeiten(a.Id, "ignoriert", null, null, null, null, null, Rv(a), nurPositionen: true, hakenGesendet: true,
            verpackt: new[] { p1.Id }, freiArtikelId: new[] { art.Id.ToString() }, freiMenge: new[] { "2,5" });

        c.TempData["SuccessMessage"].Should().NotBeNull(Warnung(c));
        var nach = Laden(a.Id);
        nach.Dienstleister.Should().Be(a.Dienstleister);
        nach.Positionen.Single(p => p.Id == p1.Id).Verpackt.Should().BeTrue();
        nach.Positionen.Single(p => p.OhneFa).Menge.Should().Be(2.5m);
    }

    [Fact]
    public void Schluessel_RoundTrip_MitLeerenTeilen()
    {
        var k = new PositionsSchluessel(null, 5, null);
        BeschichtungsauftraegeController.TrySchluessel(BeschichtungsauftraegeController.SchluesselText(k), out var back).Should().BeTrue();
        back.Should().Be(k);
    }

    // ===================== Status, Dienstleister-Pflicht (AK 6b) =====================

    [Fact]
    public async Task Uebergeben_OhneDienstleister_Warnung_StatusBleibtEntwurf()
    {
        var a = await Anlegen();
        await Ctrl().Bearbeiten(a.Id, " ", null, null, null, null, null, Rv(a));
        a = Laden(a.Id);
        var c = Ctrl();

        await c.Status(a.Id, BeschichtungsauftragStatus.Uebergeben, Rv(a));

        Warnung(c).Should().Contain("Dienstleister fehlt");
        Laden(a.Id).Status.Should().Be(BeschichtungsauftragStatus.Entwurf);
    }

    // ===================== Detail (Anforderungen 4, 6) =====================

    [Fact]
    public async Task Details_Unbekannt_NotFound()
        => (await Ctrl().Details(999)).Should().BeOfType<NotFoundResult>();

    [Fact]
    public async Task Details_Entwurf_ZeigtKandidatenNurMitSchreibrecht_UndSageAbweichung()
    {
        var a = await Anlegen();
        await Ctrl().Bearbeiten(a.Id, "Anderer Beschichter", null, null, null, null,
            a.Positionen.Where(p => p.Position == 2).Select(p => p.Id).ToArray(), Rv(a));

        var vm = (BeschichtungsauftragDetailViewModel)((ViewResult)await Ctrl().Details(a.Id)).Model!;
        vm.Kandidaten.Select(k => k.Schluessel.Position).Should().Equal(2);
        vm.SageDienstleisterAbweichend.Should().Be("Huber GmbH");
        vm.ErlaubteZiele.Should().BeEquivalentTo(new[] { BeschichtungsauftragStatus.Uebergeben, BeschichtungsauftragStatus.Storniert });

        _user.Setup(u => u.HasBeschichtungsauftragWriteAccessAsync()).ReturnsAsync(false);
        var lesend = (BeschichtungsauftragDetailViewModel)((ViewResult)await Ctrl().Details(a.Id)).Model!;
        lesend.DarfSchreiben.Should().BeFalse();
        lesend.Kandidaten.Should().BeEmpty();
    }

    // ===================== Reklamation (Anforderung 5, AK 5/5a) =====================

    [Fact]
    public async Task Reklamation_NichtErledigt_GetUndPost_Warnung_KeinAuftrag()
    {
        var a = await Status(await Anlegen(), BeschichtungsauftragStatus.Uebergeben);

        var get = Ctrl();
        (await get.Reklamation(a.Id)).Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("Details");
        Warnung(get).Should().Contain("Erledigt");

        var post = Ctrl();
        await post.Reklamation(a.Id, a.Positionen.Select(p => p.Id).Take(1).ToArray());
        Warnung(post).Should().NotBeNullOrEmpty();
        _db.Beschichtungsauftraege.Count().Should().Be(1);
    }

    [Fact]
    public async Task Reklamation_Erledigt_LegtFolgeauftragAn_UndLeereAuswahlAbgelehnt()
    {
        var a = await Status(await Status(await Status(await Anlegen(), BeschichtungsauftragStatus.Uebergeben), BeschichtungsauftragStatus.Versendet), BeschichtungsauftragStatus.Erledigt);

        var vm = (BeschichtungsauftragReklamationViewModel)((ViewResult)await Ctrl().Reklamation(a.Id)).Model!;
        vm.Ursprung.Positionen.Should().HaveCount(3);

        var leer = Ctrl();
        await leer.Reklamation(a.Id, null);
        Warnung(leer).Should().NotBeNullOrEmpty();

        var c = Ctrl();
        var r = (RedirectToActionResult)await c.Reklamation(a.Id, a.Positionen.Where(p => p.Position != 2).Select(p => p.Id).ToArray());
        var rek = Laden((int)r.RouteValues!["id"]!);
        rek.UrsprungsauftragId.Should().Be(a.Id);
        rek.Positionen.Should().HaveCount(2);
        c.TempData["SuccessMessage"].Should().BeOfType<string>().Which.Should().Contain(rek.Nummer);

        var ursprung = (BeschichtungsauftragDetailViewModel)((ViewResult)await Ctrl().Details(a.Id)).Model!;
        ursprung.Auftrag.Folgeauftraege.Select(f => f.Nummer).Should().Equal(rek.Nummer);
    }

    // ===================== Druck / PDF (Anforderung 7, AK 7/7a, Review-Fokus 5) =====================

    [Fact]
    public async Task Print_Entwurf_Warnung_RedirectDetail()
    {
        var a = await Anlegen();
        var c = Ctrl();

        var r = (RedirectToActionResult)await c.Print(a.Id, null);

        r.ActionName.Should().Be("Details");
        Warnung(c).Should().Be("Im Entwurf nicht druckbar — erst übergeben.");
        (await Ctrl().Pdf(a.Id, null)).Should().BeOfType<RedirectToActionResult>();
    }

    [Fact]
    public async Task Print_Uebergeben_Summiert_Reihenfolge_BarcodeNummer()
    {
        var a = await Status(await Anlegen(), BeschichtungsauftragStatus.Uebergeben);

        var vm = (BeschichtungsauftragPrintViewModel)((ViewResult)await Ctrl().Print(a.Id, null)).Model!;

        vm.Barcode.Should().Be("bc:" + a.Nummer);
        // v1.99.87 S1-S3: Pos. 1 (A1) + Pos. 3 (A1) summiert, Reihenfolge der ersten Position; Tiefe 3 vs 4 -> uneinheitlich
        vm.Zeilen.Select(z => (z.Artnr, z.Menge, z.Positionen, z.MasseUneinheitlich))
            .Should().Equal(("A1", 4m, 2, true), ("A2", 2m, 1, false));
        vm.Summe.Should().Be(6m);   // v1.99.88 N-1: nur Teilesumme
    }

    private static BeschichtungsauftragPosition Pos(int id, string? artnr, decimal menge, bool verpackt = false,
        bool ohneFa = false, decimal tiefe = 3m, string? matchcode = null) =>
        new() { Id = id, Position = ohneFa ? null : id, Artnr = artnr, Menge = menge, Verpackt = verpackt, OhneFa = ohneFa,
            Breite = 500m, Hoehe = 20m, Tiefe = tiefe, Matchcode = matchcode };

    [Fact]
    public void Druck_Summiert_GruppiertJeArtnr_ReihenfolgeErstePosition()
    {
        // AK 1: A x3 (2, 3, 5) + B (1) -> zwei Zeilen; " A " getrimmt gleich "A"
        var z = BeschichtungsauftraegeDruck.Summiert(new[]
            { Pos(4, "A", 5), Pos(2, "B", 1), Pos(1, "A", 2), Pos(3, " A ", 3) });

        z.Select(r => (r.Artnr, r.Menge, r.Positionen)).Should().Equal(("A", 10m, 3), ("B", 1m, 1));
    }

    [Fact]
    public void Druck_Summiert_VerpacktZaehlung()
    {
        var z = BeschichtungsauftraegeDruck.Summiert(new[]
            { Pos(1, "A", 1, verpackt: true), Pos(2, "A", 1, verpackt: true), Pos(3, "A", 1), Pos(4, "B", 1, verpackt: true), Pos(5, "C", 1) });

        z.Select(r => (r.Artnr, r.Verpackt, r.Positionen)).Should().Equal(("A", 2, 3), ("B", 1, 1), ("C", 0, 1));
    }

    [Fact]
    public void Druck_Summiert_MasseUneinheitlich_NurBeiAbweichung()
    {
        var z = BeschichtungsauftraegeDruck.Summiert(new[] { Pos(1, "A", 1), Pos(2, "A", 1, tiefe: 4m), Pos(3, "B", 1), Pos(4, "B", 1) });

        z.Select(r => (r.Artnr, r.Tiefe, r.MasseUneinheitlich)).Should().Equal(("A", 3m, true), ("B", 3m, false));
    }

    [Fact]
    public void Druck_Summiert_OhneArtnr_JePositionEigeneZeile()
    {
        // AK 10/15: null, "" und Leerzeichen nie zusammenfassen
        var z = BeschichtungsauftraegeDruck.Summiert(new[]
            { Pos(1, null, 2, matchcode: "X"), Pos(2, "", 3, matchcode: "Y"), Pos(3, "  ", 4, matchcode: "Z"), Pos(4, null, 1, matchcode: "W") });

        z.Select(r => (r.Matchcode, r.Menge, r.Positionen)).Should().Equal(("X", 2m, 1), ("Y", 3m, 1), ("Z", 4m, 1), ("W", 1m, 1));
    }

    [Fact]
    public void Druck_Summiert_FreiePositionGetrenntUndZuletzt()
    {
        // AK 4
        var z = BeschichtungsauftraegeDruck.Summiert(new[] { Pos(9, "A", 1, ohneFa: true), Pos(1, "A", 2), Pos(2, "B", 1) });

        z.Select(r => (r.Artnr, r.OhneFa, r.Menge)).Should().Equal(("A", false, 2m), ("B", false, 1m), ("A", true, 1m));
    }

    [Fact]
    public void Druck_Sortiert_FreiePositionenZuletzt()
    {
        var sortiert = BeschichtungsauftraegeDruck.Sortiert(new[]
        {
            new BeschichtungsauftragPosition { OhneFa = true, Artnr = "F" },
            new BeschichtungsauftragPosition { Position = 2, Artnr = "B" },
            new BeschichtungsauftragPosition { Position = null, PositionText = "51.10", Artnr = "D" },
            new BeschichtungsauftragPosition { Position = null, PositionText = "51.9", Artnr = "C" },
            new BeschichtungsauftragPosition { Position = 1, Artnr = "A" },
        });

        sortiert.Select(p => p.Artnr).Should().Equal("A", "B", "C", "D", "F");
    }

    [Fact]
    public async Task Print_Unbekannt_NotFound()
        => (await Ctrl().Print(999, null)).Should().BeOfType<NotFoundResult>();

    [Fact]
    public async Task Pdf_Erfolg_DateinameMitNummer()
    {
        var a = await Status(await Anlegen(), BeschichtungsauftragStatus.Uebergeben);
        _renderer.Setup(r => r.RenderToStringAsync(It.IsAny<ControllerContext>(), "Print", It.IsAny<object>())).ReturnsAsync("<html/>");
        _pdf.Setup(p => p.RenderHtmlToPdfAsync("<html/>", It.IsAny<CancellationToken>())).ReturnsAsync(new byte[] { 1 });

        var f = (FileContentResult)await Ctrl().Pdf(a.Id, null);

        f.FileDownloadName.Should().StartWith($"Beschichtungsauftrag_{a.Nummer}_").And.MatchRegex("^[A-Za-z0-9_.-]+$");
    }

    [Fact]
    public async Task Pdf_RenderFehler_RedirectDetail()
    {
        var a = await Status(await Anlegen(), BeschichtungsauftragStatus.Uebergeben);
        _renderer.Setup(r => r.RenderToStringAsync(It.IsAny<ControllerContext>(), "Print", It.IsAny<object>())).ReturnsAsync("<html/>");
        _pdf.Setup(p => p.RenderHtmlToPdfAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(PdfRenderException.Failed("boom"));
        var c = Ctrl();

        var r = (RedirectToActionResult)await c.Pdf(a.Id, null);

        (r.ActionName, r.RouteValues!["id"]).Should().Be(("Details", (object)a.Id));
        Warnung(c).Should().Be(PdfRenderException.FailedMessage);
    }

    // ===================== Liste (Anforderung 3, AK 3) =====================

    [Fact]
    public async Task Index_StatusFilter_Spaltenfilter_Pagination()
    {
        var a = await Anlegen();
        await Status(a, BeschichtungsauftragStatus.Storniert);
        var b = await Anlegen();

        var alle = (BeschichtungsauftragListViewModel)((ViewResult)await Ctrl().Index(null)).Model!;
        alle.Pagination.TotalCount.Should().Be(2);
        alle.Rows.First().Nummer.Should().Be(b.Nummer, "neueste zuerst");

        var entwurf = (BeschichtungsauftragListViewModel)((ViewResult)await Ctrl().Index(BeschichtungsauftragStatus.Entwurf)).Model!;
        entwurf.Rows.Select(r => r.Nummer).Should().Equal(b.Nummer);

        var gefiltert = (BeschichtungsauftragListViewModel)((ViewResult)await Ctrl("?colf_status=storniert").Index(null)).Model!;
        gefiltert.Rows.Select(r => r.Nummer).Should().Equal(a.Nummer);

        // PageSize.Resolve kennt nur 25/50/100/Alle — Seite 2 bei 25 ist leer, die Gesamtzahl bleibt.
        var seite2 = (BeschichtungsauftragListViewModel)((ViewResult)await Ctrl().Index(null, page: 2, pageSize: 25)).Model!;
        (seite2.Rows.Count, seite2.Pagination.TotalCount, seite2.Pagination.PageSize).Should().Be((0, 2, 25));
    }

    // ===================== Zugriff (Anforderung 8, AK 8) =====================

    [Fact]
    public void JedeSchreibendeAction_HatSchreibfilter_UndPostsAntiforgery()
    {
        var t = typeof(BeschichtungsauftraegeController);
        t.GetCustomAttribute<RequireBeschichtungsauftragAccessAttribute>().Should().NotBeNull();
        t.GetCustomAttribute<RequireFaHierarchyBeschichtungAktivAttribute>().Should().NotBeNull();

        var actions = t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).ToList();
        var posts = actions.Where(m => m.GetCustomAttribute<HttpPostAttribute>() != null).ToList();
        posts.Select(m => m.Name).Should().BeEquivalentTo(new[]
            { "Create", "Bearbeiten", "Bemerkung", "Status", "Reklamation" });
        foreach (var m in posts)
        {
            m.GetCustomAttribute<RequireBeschichtungsauftragWriteAccessAttribute>().Should().NotBeNull(m.Name);
            m.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>().Should().NotBeNull(m.Name);
        }
        actions.Where(m => m.Name == "Reklamation").Should().OnlyContain(m => m.GetCustomAttribute<RequireBeschichtungsauftragWriteAccessAttribute>() != null);
        actions.Where(m => m.Name is "Index" or "Details" or "Print" or "Pdf")
            .Should().OnlyContain(m => m.GetCustomAttribute<RequireBeschichtungsauftragWriteAccessAttribute>() == null);
    }

    // ===================== v1.99.56 (TS-181): Status-Knopf speichert die Eingaben mit =====================

    [Fact]
    public async Task Bearbeiten_MitZiel_SpeichertUndWechseltStatus()
    {
        var a = await Anlegen();
        var c = Ctrl();

        await c.Bearbeiten(a.Id, "Neu AG", "RAL 9016", null, null, null, null, Rv(a), ziel: BeschichtungsauftragStatus.Uebergeben);

        Warnung(c).Should().BeNull();
        c.TempData["SuccessMessage"].Should().Be("Gespeichert, Status auf „Übergeben“ gesetzt.");
        var nach = Laden(a.Id);
        (nach.Dienstleister, nach.Ral, nach.Status).Should().Be(("Neu AG", "RAL 9016", BeschichtungsauftragStatus.Uebergeben));
    }

    [Fact]
    public async Task Bearbeiten_MitZiel_StatusAbgelehnt_GespeichertUndWarnung()
    {
        var a = await Anlegen();
        var c = Ctrl();

        await c.Bearbeiten(a.Id, "", "RAL 7016", null, null, null, null, Rv(a), ziel: BeschichtungsauftragStatus.Uebergeben);

        c.TempData["SuccessMessage"].Should().Be("Gespeichert.");
        Warnung(c).Should().Contain("Dienstleister fehlt");
        var nach = Laden(a.Id);
        (nach.Dienstleister, nach.Ral, nach.Status).Should().Be(((string?)null, "RAL 7016", BeschichtungsauftragStatus.Entwurf));
    }

    [Fact]
    public async Task Bearbeiten_MitZiel_SpeichernAbgelehnt_KeinStatuswechsel()
    {
        var a = await Anlegen();
        var c = Ctrl();

        await c.Bearbeiten(a.Id, "Neu AG", null, null, null, new Dictionary<int, string> { [a.Positionen.First().Id] = "abc" }, null, Rv(a),
            ziel: BeschichtungsauftragStatus.Uebergeben);

        Warnung(c).Should().Contain("abc");
        c.TempData.ContainsKey("SuccessMessage").Should().BeFalse();
        var nach = Laden(a.Id);
        (nach.Dienstleister, nach.Status).Should().Be(("Huber GmbH", BeschichtungsauftragStatus.Entwurf));
    }

    [Fact]
    public async Task Bearbeiten_Uebergeben_HakenMitZielVersendet()
    {
        var a = await Status(await Anlegen(), BeschichtungsauftragStatus.Uebergeben);
        var ids = a.Positionen.Select(p => p.Id).ToArray();

        var c1 = Ctrl();
        await c1.Bearbeiten(a.Id, null, null, null, null, null, null, Rv(a), nurPositionen: true, hakenGesendet: true,
            verpackt: ids.Skip(1).ToArray(), ziel: BeschichtungsauftragStatus.Versendet);
        c1.TempData["SuccessMessage"].Should().Be("Gespeichert.");
        Warnung(c1).Should().Contain("nicht verpackt (1)");
        a = Laden(a.Id);
        (a.Status, a.Positionen.Count(p => p.Verpackt)).Should().Be((BeschichtungsauftragStatus.Uebergeben, ids.Length - 1));

        var c2 = Ctrl();
        await c2.Bearbeiten(a.Id, null, null, null, null, null, null, Rv(a), nurPositionen: true, hakenGesendet: true,
            verpackt: ids, ziel: BeschichtungsauftragStatus.Versendet);
        Warnung(c2).Should().BeNull();
        Laden(a.Id).Status.Should().Be(BeschichtungsauftragStatus.Versendet);
    }
}
