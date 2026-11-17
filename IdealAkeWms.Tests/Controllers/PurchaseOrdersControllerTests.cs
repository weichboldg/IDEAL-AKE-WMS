using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Services.Settings;
using IdealAkeWms.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Moq;

namespace IdealAkeWms.Tests.Controllers;

/// <summary>Spec 2026-11-08-wareneingang-aus-sage-bestellungen E2 (AK 6-11).</summary>
public partial class PurchaseOrdersControllerTests
{
    private static SagePurchaseOrderLine L(int belPosId, DateTime? termin, string kto = "K1", string art = "A1", decimal offen = 5m) => new()
    {
        BelPosId = belPosId, BelId = belPosId / 10, Belegkennzeichen = "EBB", Belegnummer = $"2026-{belPosId / 10}", LieferantKto = kto,
        LieferantName = "Lieferant " + kto, Artikelnummer = art, MengeBestellt = offen, MengeOffen = offen, Liefertermin = termin,
        MengeBasiseinheit = "Stk", SyncedAt = new DateTime(2026, 11, 8, 6, 0, 0),
    };

    /// <summary>v1.99.31: Standard-Query <c>?kw=1</c> = schon vorbelegt (leerer Liefertermin-Filter, kein Redirect), sonst Erstaufruf.</summary>
    internal static (PurchaseOrdersController ctrl, Mock<ISagePurchaseOrderLineRepository> lines, Mock<ICurrentUserService> user) Build(
        List<SagePurchaseOrderLine> zeilen, int wochen = 2, bool canStock = true, Dictionary<int, ErfasstJeBelPos>? erfasst = null,
        string query = "?kw=1", IdealAkeWms.Data.ApplicationDbContext? db = null, string? wareneingangAktiv = "true")
    {
        var lines = new Mock<ISagePurchaseOrderLineRepository>();
        lines.Setup(l => l.GetAllAsync()).ReturnsAsync(zeilen);
        lines.Setup(l => l.GetStandAsync()).ReturnsAsync(zeilen.Count == 0 ? null : zeilen.Max(z => z.SyncedAt));
        var locations = new Mock<IStorageLocationRepository>();
        locations.Setup(l => l.GetAllOrderedAsync()).ReturnsAsync(new List<StorageLocation>());
        var settings = new Mock<IAppSettingRepository>();
        settings.Setup(s => s.GetIntValueAsync(AppSettingKeys.OffeneBestellungenVorschauWochen, 2)).ReturnsAsync(wochen);
        settings.Setup(s => s.GetValueAsync(AppSettingKeys.WareneingangBestellungAktiv)).ReturnsAsync(wareneingangAktiv);
        var user = new Mock<ICurrentUserService>();
        user.Setup(u => u.CanAccessStockAsync()).ReturnsAsync(canStock);
        user.Setup(u => u.GetCurrentAppUserId()).Returns(7);
        user.Setup(u => u.GetDisplayName()).Returns("Tester");
        user.Setup(u => u.GetWindowsUserName()).Returns("AKE\\tester");
        var receipts = new Mock<IPurchaseOrderReceiptRepository>();
        receipts.Setup(r => r.GetErfasstJeBelPosAsync(It.IsAny<IReadOnlyCollection<int>>())).ReturnsAsync(erfasst ?? new Dictionary<int, ErfasstJeBelPos>());
        var ctrl = new PurchaseOrdersController(lines.Object, locations.Object, settings.Object, user.Object, receipts.Object,
            Mock.Of<IStockMovementRepository>(), Mock.Of<IServiceSettingRepository>(), Mock.Of<IPurchaseProposalRepository>(),
            new UserViewPreferenceRepository(db ?? TestDbContextFactory.Create()))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        ctrl.ControllerContext.HttpContext.Request.QueryString = new QueryString(query);
        return (ctrl, lines, user);
    }

    private static PurchaseOrderListViewModel Vm(IActionResult r) => (PurchaseOrderListViewModel)((ViewResult)r).Model!;

    // ----- v1.99.15 (Spec 2026-11-11-wareneingang-teil-3-rahmenabrufe F3, AK 6-7) -----

    private static SagePurchaseOrderLine Abruf(int belPosId, decimal menge)
    {
        var l = L(belPosId, DateTime.Today.AddDays(1), offen: menge);
        l.Belegkennzeichen = "EBZ";
        return l;
    }

    [Fact]
    public async Task Abruf_HinweisRahmenabruf_GeliefertStrich_Spaltenfilter_AK6()
    {
        var (ctrl, _, _) = Build(new List<SagePurchaseOrderLine> { L(11, DateTime.Today.AddDays(1)), Abruf(21, 10m) });
        var vm = Vm(await ctrl.Index());
        var abruf = vm.Items.Single(i => i.Line.BelPosId == 21);
        PurchaseOrdersController.Hinweis(abruf).Should().StartWith("Rahmenabruf");
        PurchaseOrdersController.Hinweis(vm.Items.Single(i => i.Line.BelPosId == 11)).Should().NotContain("Rahmenabruf");
        PurchaseOrdersController.GeliefertText(abruf.Line).Should().Be("–");
        PurchaseOrdersController.GeliefertText(vm.Items.Single(i => i.Line.BelPosId == 11).Line).Should().Be("0");

        ctrl.ControllerContext.HttpContext.Request.QueryString = new QueryString("?kw=1&colf_hinweis=Rahmenabruf");
        Vm(await ctrl.Index()).Items.Select(i => i.Line.BelPosId).Should().Equal(21);
    }

    [Fact]
    public async Task Abruf_Rest0_Ausgeblendet_Gezaehlt_MitFilterSichtbar_EbbRest0Bleibt_AK7()
    {
        var sync = new DateTime(2026, 11, 8, 6, 0, 0);
        var uebergeben = new List<(DateTime, decimal)> { (sync.AddHours(-1), 4m) };
        var erfasst = new Dictionary<int, ErfasstJeBelPos>
        {
            [21] = new(10m, uebergeben),   // Abruf 10, alles erfasst (4 davon an Sage uebergeben) -> Rest 0
            [31] = new(3m, uebergeben),    // Abruf 10, 3 erfasst -> Rest 7
            [11] = new(5m, Array.Empty<(DateTime, decimal)>()),   // EBB offen 5, 5 erfasst -> Rest 0, bleibt sichtbar
        };
        var (ctrl, _, _) = Build(new List<SagePurchaseOrderLine> { L(11, DateTime.Today.AddDays(1)), Abruf(21, 10m), Abruf(31, 10m) }, erfasst: erfasst);

        var vm = Vm(await ctrl.Index());
        vm.Items.Select(i => i.Line.BelPosId).Should().BeEquivalentTo(new[] { 11, 31 });
        (vm.AusgeblendeteAbrufe, vm.Pagination.TotalCount, vm.AuchErfassteAbrufe).Should().Be((1, 2, false));

        vm = Vm(await ctrl.Index(auchErfassteAbrufe: true));
        vm.Items.Select(i => i.Line.BelPosId).Should().BeEquivalentTo(new[] { 11, 21, 31 });
        (vm.AusgeblendeteAbrufe, vm.AuchErfassteAbrufe).Should().Be((0, true));
    }

    // ----- v1.99.31 (Spec 2026-11-16-offene-bestellungen-sofort-neu-lesen-einkauf-kw-filter F3/F4) -----
    // BEWUSST GEAENDERT: bis v1.99.30 stand hier „Vorschau_HorizontXWochen_UeberfaelligUndOhneTerminImmer_AK7“ (Horizont heute + X Wochen,
    // Parameter wochen). Feld „Wochen“ und Horizont entfallen; Zeitraum = Spaltenfilter Liefertermin (vorbelegt), Ueberfaellige = Checkbox.

    [Theory]
    [InlineData("2026-11-17", 2, "16.11.2026...29.11.2026")]
    [InlineData("2026-11-17", 1, "16.11.2026...22.11.2026")]
    [InlineData("2026-11-22", 1, "16.11.2026...22.11.2026")]   // Sonntag gehoert zur laufenden ISO-KW
    [InlineData("2026-12-30", 2, "28.12.2026...10.01.2027")]   // KW 53 -> Folgejahr (AK 15a)
    [InlineData("2027-01-01", 1, "28.12.2026...03.01.2027")]   // 1.1.2027 liegt in KW 53/2026
    [InlineData("2026-11-17", 99, "16.11.2026...16.05.2027")]  // auf 26 Wochen begrenzt
    public void Vorbelegung_MontagBisSonntag_Jahresgenau_F3_3(string heute, int wochen, string erwartet)
        => PurchaseOrdersController.LieferterminVorbelegung(DateTime.Parse(heute, System.Globalization.CultureInfo.InvariantCulture), wochen)
            .Should().Be(erwartet);

    [Fact]
    public async Task Erstaufruf_RedirectMitVorbelegungUndMerker_MitMerkerKeinRedirect_F3_3()
    {
        var (ctrl, _, _) = Build(new List<SagePurchaseOrderLine> { L(11, DateTime.Today) }, wochen: 1, query: "?lieferant=K1");
        var r = (RedirectToActionResult)await ctrl.Index(lieferant: "K1");
        (r.ActionName, r.ControllerName).Should().Be(("Index", "PurchaseOrders"));
        r.RouteValues!["colf_liefertermin"].Should().Be(PurchaseOrdersController.LieferterminVorbelegung(DateTime.Today, 1));
        (r.RouteValues["kw"], r.RouteValues["lieferant"]).Should().Be(("1", "K1"));

        ctrl.ControllerContext.HttpContext.Request.QueryString = new QueryString("?kw=1");
        (await ctrl.Index()).Should().BeOfType<ViewResult>("geleerter Filter + Merker = alle Termine, keine erneute Vorbelegung (AK 17)");
        ctrl.ControllerContext.HttpContext.Request.QueryString = new QueryString("?colf_liefertermin=KW47");
        (await ctrl.Index()).Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task Sichtbarkeit_OhneTerminImmer_UeberfaelligNurMitCheckbox_DatumsfilterNurAufNichtUeberfaellige_F4_2()
    {
        var heute = DateTime.Today;
        var zeilen = new List<SagePurchaseOrderLine>
        {
            L(11, heute.AddDays(1)), L(21, heute.AddDays(60)), L(31, heute.AddDays(-30)), L(41, null), L(51, heute.AddDays(-1)),
        };
        var bereich = PurchaseOrdersController.LieferterminVorbelegung(heute, 2);
        var (ctrl, _, _) = Build(zeilen, query: "?kw=1&colf_liefertermin=" + bereich);

        // Lager, Default aus: 21 ausserhalb, Ueberfaellige (31, 51) ausgeblendet und gezaehlt, ohne Termin (41) da
        var vm = Vm(await ctrl.Index());
        vm.Items.Select(i => i.Line.BelPosId).Should().Equal(11, 41);
        (vm.Ueberfaellige, vm.UeberfaelligAusgeblendet).Should().Be((false, 2));

        // Checkbox an: Ueberfaellige aller Wochen trotz Datumsfilter (ab Montag der KW) sichtbar, Hinweis weg
        vm = Vm(await ctrl.Index(ueberfaellige: true));
        vm.Items.Select(i => i.Line.BelPosId).Should().Equal(31, 51, 11, 41);
        vm.UeberfaelligAusgeblendet.Should().Be(0);
        PurchaseOrdersController.Hinweis(vm.Items.Single(i => i.Line.BelPosId == 31)).Should().Contain("überfällig");

        // „nur überfällig“: nur Ueberfaellige, unabhaengig von Checkbox und Datumsfilter (AK 22)
        Vm(await ctrl.Index(nurUeberfaellig: true, ueberfaellige: false)).Items.Select(i => i.Line.BelPosId).Should().Equal(31, 51);
        // „Überfällige anzeigen“ unter den anderen Filtern: Lieferant-Filter zaehlt mit
        Vm(await ctrl.Index(lieferant: "K9", ueberfaellige: false)).UeberfaelligAusgeblendet.Should().Be(0);
    }

    [Fact]
    public async Task Zelle_UndSpaltenfilter_MitKw_F3_1()
    {
        var t = new DateTime(2026, 11, 18);
        PurchaseOrdersController.LieferterminText(t).Should().Be("18.11.2026 KW47");
        PurchaseOrdersController.LieferterminText(null).Should().Be("ohne Termin");
        // feste Zukunftsdaten (KW 50 vs. KW 51, keine Teilstring-Kollision wie KW4/KW47 - Grenze F3.5); Ueberfaellige spielen hier keine Rolle
        var (ctrl, _, _) = Build(new List<SagePurchaseOrderLine> { L(11, new DateTime(2099, 12, 16)), L(21, new DateTime(2099, 12, 9)) },
            query: "?kw=1&colf_liefertermin=KW" + System.Globalization.ISOWeek.GetWeekOfYear(new DateTime(2099, 12, 9)));
        Vm(await ctrl.Index()).Items.Select(i => i.Line.BelPosId).Should().Contain(21).And.NotContain(11);
        File.ReadAllText(ViewPfad("Index.cshtml")).Should().Contain("LieferterminText(l.Liefertermin)").And.NotContain("name=\"wochen\"");
    }

    [Fact]
    public async Task Checkbox_JeBenutzerUndAnsicht_DefaultLagerAusEinkaufEin_Gespeichert_F4_5()
    {
        using var db = TestDbContextFactory.Create();
        var zeilen = new List<SagePurchaseOrderLine> { L(31, DateTime.Today.AddDays(-3)) };
        var (lager, _, _) = Build(zeilen, db: db);
        Vm(await lager.Index()).Ueberfaellige.Should().BeFalse("Lager Default aus");
        db.UserViewPreferences.Should().BeEmpty("Default wird nicht gespeichert");

        Vm(await lager.Index(ueberfaellige: true)).Ueberfaellige.Should().BeTrue();
        var pref = db.UserViewPreferences.Single();
        (pref.UserId, pref.ViewKey, pref.SettingsJson, pref.CreatedBy).Should().Be((7, "PurchaseOrders.Ueberfaellige", "{\"an\":true}", "Tester"));
        Vm(await lager.Index()).Ueberfaellige.Should().BeTrue("ohne Parameter (Pagination, neuer Aufruf) gilt die Praeferenz");

        var (einkauf, _, _) = Build(zeilen, db: db);
        var vm = Vm(await einkauf.ListeAsync(true, null, null, null, null, false, null, false, 1, null));
        (vm.Einkauf, vm.Ueberfaellige).Should().Be((true, true), "Einkauf Default ein, eigener ViewKey");
        await einkauf.ListeAsync(true, null, null, null, null, false, false, false, 1, null);
        db.UserViewPreferences.Single(p => p.ViewKey == "EinkaufPurchaseOrders.Ueberfaellige").SettingsJson.Should().Be("{\"an\":false}");
        Vm(await lager.Index()).Ueberfaellige.Should().BeTrue("die Lager-Ansicht behaelt ihren Zustand");
        PurchaseOrdersController.UeberfaelligeLesen("kaputt").Should().BeNull();
    }

    [Theory]
    [InlineData(true, "true", true)]
    [InlineData(true, null, false)]    // WareneingangBestellungAktiv aus -> Receive waere 404
    [InlineData(false, "true", false)] // ohne Lager-Schreibrecht
    public async Task Einkauf_KnopfWareneingang_NurMitLagerRechtUndGate_F2_3(bool canStock, string? gate, bool erwartet)
    {
        var (ctrl, _, _) = Build(new List<SagePurchaseOrderLine> { L(11, DateTime.Today) }, canStock: canStock, wareneingangAktiv: gate);
        Vm(await ctrl.ListeAsync(true, null, null, null, null, false, null, false, 1, null)).CanReceive.Should().Be(erwartet);
        var r = (RedirectToActionResult)await Build(new List<SagePurchaseOrderLine>(), query: "").ctrl.ListeAsync(true, null, null, null, null, false, null, false, 1, null);
        r.ControllerName.Should().Be("EinkaufPurchaseOrders");
    }

    [Fact]
    public void Einkauf_Attribute_UndReceiveBleibtGeschuetzt_F2_2_F2_4()
    {
        typeof(EinkaufPurchaseOrdersController).GetCustomAttributes(false).Select(a => a.GetType())
            .Should().Contain(new[] { typeof(RequireEinkaufAccessAttribute), typeof(RequireEinkaufAktivAttribute) })
            .And.NotContain(typeof(RequireStockReadAccessAttribute)).And.NotContain(typeof(RequireWareneingangBestellungAktivAttribute));
        // QA v1.99.31: Einkauf-Ansichten nur lesend - keine schreibende Action (Waechter; Aktionen bleiben im PurchaseOrdersController)
        typeof(EinkaufPurchaseOrdersController).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttributes(true).Any(a => a is Microsoft.AspNetCore.Mvc.Routing.IActionHttpMethodProvider p && p.HttpMethods.Any(h => h != "GET")))
            .Should().BeEmpty();
        foreach (var m in typeof(PurchaseOrdersController).GetMethods().Where(m => m.Name == "Receive"))
            m.GetCustomAttributes(typeof(RequireStockAccessAttribute), false).Should().NotBeEmpty($"{m} bucht nur mit Lager-Recht");
        // Formulare/Links der gemeinsamen Views zielen ausdruecklich auf PurchaseOrders (sonst /EinkaufPurchaseOrders/Receive)
        File.ReadAllText(ViewPfad("Index.cshtml")).Should().NotContain("<a asp-action=\"Receive\"");
        var sw = File.ReadAllText(ViewPfad("SageWareneingaenge.cshtml"));
        foreach (var a in new[] { "SageWeErneutSenden", "SageWeAlsUebergeben", "CsvErneutSchreiben" })
            sw.Should().Contain($"asp-controller=\"PurchaseOrders\" asp-action=\"{a}\"");
    }

    [Fact]
    public async Task EinkaufAktiv_Aus_UmleitungstextAllgemein_F2_2()
    {
        var settings = new Mock<IAppSettingRepository>();
        var filter = new RequireEinkaufAktivFilter(settings.Object);
        var ctrl = new EinkaufPurchaseOrdersController(Build(new List<SagePurchaseOrderLine>()).ctrl);
        ctrl.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(new DefaultHttpContext(),
            Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());
        var ctx = new ActionExecutingContext(new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
            new List<IFilterMetadata>(), new Dictionary<string, object?>(), ctrl);
        await filter.OnActionExecutionAsync(ctx, () => Task.FromResult<ActionExecutedContext>(null!));
        ctx.Result.Should().BeOfType<RedirectToActionResult>();
        ctrl.TempData["WarningMessage"].Should().Be("Einkauf ist deaktiviert (Einstellung „Einkauf aktiv“).");
    }

    [Fact]
    public async Task Filter_Lieferant_UndPagination_AK9()
    {
        var t = DateTime.Today.AddDays(1);
        var zeilen = Enumerable.Range(1, 30).Select(i => L(i * 10 + 1, t, kto: i % 2 == 0 ? "K2" : "K1")).ToList();
        var (ctrl, _, _) = Build(zeilen);

        var vm = Vm(await ctrl.Index(lieferant: "k2", pageSize: 25));
        vm.Pagination.TotalCount.Should().Be(15);
        vm.Items.Should().OnlyContain(i => i.Line.LieferantKto == "K2");
        vm.Lieferanten.Select(l => l.Kto).Should().BeEquivalentTo(new[] { "K1", "K2" });
    }

    [Fact]
    public async Task StockRead_OhneKnopf_AK10_LeererCache_Hinweis_AK11()
    {
        var (ctrl, _, _) = Build(new List<SagePurchaseOrderLine>(), canStock: false);
        var vm = Vm(await ctrl.Index());
        (vm.CanReceive, vm.Stand).Should().Be((false, (DateTime?)null));
        File.ReadAllText(ViewPfad("Index.cshtml")).Should().Contain("Sync:EinkaufBestellungenEnabled").And.Contain("@if (Model.CanReceive)");
    }

    [Fact]
    public void Nachbesserung_v1_86_Farbkonzept_UeberfaelligRotMitLegende_ZielHinweis()
    {
        var index = File.ReadAllText(ViewPfad("Index.cshtml"));
        index.Should().Contain("r.Ueberfaellig ? \"table-danger\"").And.Contain("rot hinterlegt").And.NotContain("table-warning");
        // v1.91.0 (N-1): Legende im Zeilenton (Hellrot, dunkler Text), nicht kraeftig rot
        index.Should().Contain("badge bg-danger-subtle text-danger-emphasis").And.NotContain("text-bg-danger");
        File.ReadAllText(ViewPfad("Receive.cshtml")).Should().Contain("ist im WMS nicht buchbar – bitte Ziel wählen");
    }

    internal static string ViewPfad(string datei)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "IdealAkeWms", "Views"))) dir = dir.Parent;
        return Path.Combine(dir!.FullName, "IdealAkeWms", "Views", "PurchaseOrders", datei);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("false", false)]
    [InlineData(" true", false)]
    [InlineData("TRUE", true)]
    public async Task FeatureGate_NurTrue_Sonst404_AK6(string? wert, bool durch)
    {
        var settings = new Mock<IAppSettingRepository>();
        settings.Setup(s => s.GetValueAsync(AppSettingKeys.WareneingangBestellungAktiv)).ReturnsAsync(wert);
        var filter = new RequireWareneingangBestellungAktivFilter(settings.Object);
        var ctx = new ActionExecutingContext(new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
            new List<IFilterMetadata>(), new Dictionary<string, object?>(), new object());
        var weiter = false;

        await filter.OnActionExecutionAsync(ctx, () => { weiter = true; return Task.FromResult<ActionExecutedContext>(null!); });

        weiter.Should().Be(durch);
        if (!durch) ctx.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public void Einstellungen_Katalog_Wochen1bis26_AK8()
    {
        var d = AppSettingDefinitions.All.Single(x => x.Key == AppSettingKeys.OffeneBestellungenVorschauWochen);
        SettingValidator.Validate(d, "0").Gueltig.Should().BeFalse();
        SettingValidator.Validate(d, "27").Gueltig.Should().BeFalse();
        SettingValidator.Validate(d, "abc").Gueltig.Should().BeFalse();
        SettingValidator.Validate(d, "4").Kanonisch.Should().Be("4");
        AppSettingDefinitions.All.Single(x => x.Key == AppSettingKeys.WareneingangBestellungAktiv).DefaultValue.Should().Be("false");
        typeof(PurchaseOrdersController).GetCustomAttributes(false).Select(a => a.GetType())
            .Should().Contain(new[] { typeof(RequireStockReadAccessAttribute), typeof(RequireWareneingangBestellungAktivAttribute) });
    }
}
