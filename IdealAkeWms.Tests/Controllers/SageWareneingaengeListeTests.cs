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
using Moq;

namespace IdealAkeWms.Tests.Controllers;

/// <summary>v1.99.4 (Spec 2026-11-08-wareneingang-teil-2-sage-we-beleg-rahmenabrufe E3, AK 8/9): Liste „Wareneingänge an Sage“ und „Erneut senden“.
/// v1.99.66 (Spec 2026-11-23-sage-buchungen-gesamtsicht): die Liste ist Teil der Seite „Sage-Buchungen“ (<see cref="SageBookingQueueController"/>);
/// die Aktionen bleiben am <see cref="PurchaseOrdersController"/> und springen mit <c>returnUrl</c> zurück.</summary>
public class SageWareneingaengeListeTests
{
    private static readonly Guid VorgangA = Guid.NewGuid(), VorgangB = Guid.NewGuid(), VorgangC = Guid.NewGuid();

    private static async Task<(PurchaseOrdersController ctrl, ApplicationDbContext db)> BuildAsync(bool sageWeAn = true)
    {
        var db = TestDbContextFactory.Create();
        if (sageWeAn) db.ServiceSettings.Add(new ServiceSetting { Key = SageWareneingang.ToggleKey, Value = "true" });
        db.Articles.Add(new Article { Id = 5, ArticleNumber = "00722001", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        db.StorageLocations.Add(new StorageLocation { Id = 9, Code = "HR 0 0 0", IsActive = true, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        StockMovement M(int belPosId, SageWareneingangStatus st, Guid? vorgang) => new()
        {
            ArticleId = 5, StorageLocationId = 9, Quantity = 1m, MovementType = MovementType.Einbuchung, Timestamp = DateTime.Now, WindowsUser = "t",
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t",
            PurchaseOrderReceipt = new PurchaseOrderReceipt
            {
                BelId = 100, BelPosId = belPosId, Belegnummer = "2026-100", Quantity = 1m, Lieferart = PurchaseOrderReceiptLieferart.Teillieferung,
                SageWeStatus = st, SageWeVorgang = vorgang, SageWeVersuche = st == SageWareneingangStatus.Fehler ? 1 : 0,
                SageWeFehler = st == SageWareneingangStatus.Fehler ? "HTTP 500" : null, Lieferschein = "LS-1",
                CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t",
            },
        };
        db.StockMovements.AddRange(M(1, SageWareneingangStatus.Fehler, VorgangA), M(2, SageWareneingangStatus.Fehler, VorgangA),
            M(3, SageWareneingangStatus.Uebergeben, VorgangB), M(4, SageWareneingangStatus.NichtNoetig, null), M(5, SageWareneingangStatus.Klaeren, VorgangC));
        await db.SaveChangesAsync();

        var user = new Mock<ICurrentUserService>();
        user.Setup(u => u.GetDisplayName()).Returns("Tester");
        user.Setup(u => u.GetWindowsUserName()).Returns("AKE\\tester");
        user.Setup(u => u.CanTransferStockAsync()).ReturnsAsync(true);
        var ctrl = new PurchaseOrdersController(new SagePurchaseOrderLineRepository(db), new StorageLocationRepository(db), Mock.Of<IAppSettingRepository>(),
            user.Object, new PurchaseOrderReceiptRepository(db), new StockMovementRepository(db), new ServiceSettingRepository(db), new PurchaseProposalRepository(db), new UserViewPreferenceRepository(db),
            new ArticleRepository(db))   // v1.99.52 BEWUSST GEAENDERT: Ctor mit IArticleRepository
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        ctrl.TempData = new TempDataDictionary(ctrl.HttpContext, Mock.Of<ITempDataProvider>());
        return (ctrl, db);
    }

    /// <summary>v1.99.66: Liste der Seite „Sage-Buchungen“ über dieselbe DB. Standard = Lager mit stock_read + Keyuser, Modul an.</summary>
    internal static SageBookingQueueController Liste(ApplicationDbContext db, bool stockRead = true, bool transfer = true, bool weModul = true,
        bool einkauf = false, string? query = null)
    {
        var user = new Mock<ICurrentUserService>();
        user.Setup(u => u.CanAccessStockReadAsync()).ReturnsAsync(stockRead);
        user.Setup(u => u.CanAccessStockAsync()).ReturnsAsync(stockRead && transfer);
        user.Setup(u => u.CanTransferStockAsync()).ReturnsAsync(transfer);
        user.Setup(u => u.CanAccessEinkaufAsync()).ReturnsAsync(einkauf);
        var settings = new Mock<IAppSettingRepository>();
        settings.Setup(s => s.GetValueAsync(AppSettingKeys.WareneingangBestellungAktiv)).ReturnsAsync(weModul ? "true" : "false");
        settings.Setup(s => s.GetValueAsync(AppSettingKeys.EinkaufAktiv)).ReturnsAsync(einkauf ? "true" : null);
        var ctrl = new SageBookingQueueController(new SageBookingQueueRepository(db), user.Object, new ServiceSettingRepository(db),
            new PurchaseOrderReceiptRepository(db), settings.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        if (query != null) ctrl.HttpContext.Request.QueryString = new QueryString(query);
        return ctrl;
    }

    private static async Task<SageBookingQueueViewModel> WeAsync(SageBookingQueueController c, SageBuchungAnzeige? status = null, bool nurOffene = false)
        => (SageBookingQueueViewModel)((ViewResult)await c.Index(SageBuchungHerkunft.Wareneingang, status, nurOffene, null, null, null)).Model!;

    private static List<SageWareneingangZeile> Zeilen(SageBookingQueueViewModel vm) => vm.Zeilen.Select(z => z.We!).ToList();

    [Fact]
    public async Task Liste_Memo_GespeicherteKennung_AltbestandAltesFormat_NieGesendetMitKuerzel_v1_99_9()
    {
        var (ctrl, db) = await BuildAsync();
        db.ServiceSettings.Add(new ServiceSetting { Key = SageSystemKuerzel.Key, Value = "DEV" });
        var b = db.PurchaseOrderReceipts.Single(r => r.SageWeVorgang == VorgangB);
        b.SageWeKennung = $"WE#AT#{b.Id}#";
        await db.SaveChangesAsync();

        var vm = Zeilen(await WeAsync(Liste(db)));

        var a = vm.Where(z => z.Receipt.SageWeVorgang == VorgangA).Min(z => z.Receipt.Id);
        vm.Where(z => z.Receipt.SageWeVorgang == VorgangA).Should().OnlyContain(z => z.Memo == $"WE#{a}#", "gesendet vor v1.99.9 = altes Format");
        vm.Single(z => z.Receipt.SageWeVorgang == VorgangB).Memo.Should().Be($"WE#AT#{b.Id}#", "gespeicherte Kennung");
        var c = vm.Single(z => z.Receipt.SageWeVorgang == VorgangC);
        c.Memo.Should().Be($"WE#DEV#{c.Receipt.Id}#", "nie gesendet = aktuelles Kuerzel, wie der Dienst");
    }

    [Fact]
    public async Task Liste_OhneNichtNoetig_MitMemoDerGruppe_StatusFilter_AK8()
    {
        var (_, db) = await BuildAsync();
        var vm = await WeAsync(Liste(db));
        vm.Zeilen.Should().HaveCount(4, "NichtNoetig (Teil 1) erscheint nicht als WE-Zeile");
        var ersteId = Zeilen(vm).Where(z => z.Receipt.SageWeVorgang == VorgangA).Min(z => z.Receipt.Id);
        Zeilen(vm).Where(z => z.Receipt.SageWeVorgang == VorgangA).Should().OnlyContain(z => z.Memo == $"WE#{ersteId}#");
        vm.Pagination.TotalCount.Should().Be(4);
        vm.AusHinweis.Should().BeNull("Schalter an");

        (await WeAsync(Liste(db), SageBuchungAnzeige.Fehler)).Zeilen.Should().HaveCount(2);
    }

    [Fact]
    public async Task Einkauf_OhneLagerRecht_NurWeZeilen_KeineKnoepfe_NurOffene_v1_99_66_Rueckfrage12()
    {
        var (_, db) = await BuildAsync();
        // eine Lagerbuchung, die der Einkauf nicht sehen darf
        db.SageBookingQueueItems.Add(new SageBookingQueueItem { StockMovementId = db.StockMovements.First().Id, Status = SageBookingQueueStatus.Fehler,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        await db.SaveChangesAsync();
        var einkauf = Liste(db, stockRead: false, transfer: false, weModul: false, einkauf: true);
        var vm = (SageBookingQueueViewModel)((ViewResult)await einkauf.Index(null, null, true, null, null, null)).Model!;
        vm.Zeilen.Should().HaveCount(3).And.OnlyContain(z => z.We != null && SageBuchungenGesamtsicht.IstOffen(z.Anzeige));
        (vm.ZeigtLagerbuchungen, vm.ZeigtWareneingaenge, vm.CanRequeue, vm.CanWeAktionen, vm.CanReceive).Should().Be((false, true, false, false, false));
        (await WeAsync(Liste(db, stockRead: false, transfer: false, weModul: false, einkauf: true), SageBuchungAnzeige.Uebergeben, nurOffene: true))
            .Zeilen.Should().BeEmpty("status=Uebergeben mit nur offene zeigt nichts");
        // Lager: alle WE-Zeilen (ohne NichtNoetig) + die Lagerbuchung, Knoepfe
        var lager = (SageBookingQueueViewModel)((ViewResult)await Liste(db).Index(null, null, false, null, null, null)).Model!;
        (lager.Zeilen.Count, lager.CanReceive, lager.CanWeAktionen, lager.CanRequeue).Should().Be((5, true, true, true));
    }

    [Fact]
    public async Task ModulAus_KeineWeZeilen_AuchNichtPerHerkunft_v1_99_66_AK10()
    {
        var (_, db) = await BuildAsync();
        var vm = await WeAsync(Liste(db, weModul: false));
        (vm.Zeilen.Count, vm.ZeigtWareneingaenge, vm.CanWeAktionen).Should().Be((0, false, false));
    }

    [Fact]
    public async Task ErsteZeileJeVorgang_TraegtDieKnoepfe_AuchNachSpaltenfilter_v1_99_66_AK6()
    {
        var (_, db) = await BuildAsync();
        var vm = await WeAsync(Liste(db), SageBuchungAnzeige.Fehler);
        vm.Zeilen.Select(z => z.ZeigtWeAktionen).Should().Equal(true, false);
        var zweite = vm.Zeilen[1].We!.Receipt;
        var nurZweite = await WeAsync(Liste(db, query: $"?colf_pos={zweite.BelPosId}"), SageBuchungAnzeige.Fehler);
        nurZweite.Zeilen.Should().ContainSingle().Which.ZeigtWeAktionen.Should().BeTrue("erste Zeile des Vorgangs auf der Seite");
        nurZweite.Zeilen[0].We!.Receipt.Id.Should().Be(zweite.Id);
    }

    [Fact]
    public async Task Zeitraum_ErledigteAelterAls30TageWeg_OffeneMitHinweis_v1_99_66_AK11()
    {
        var (_, db) = await BuildAsync();
        foreach (var r in db.PurchaseOrderReceipts) r.CreatedAt = DateTime.Today.AddDays(-40);
        await db.SaveChangesAsync();
        var vm = await WeAsync(Liste(db));
        vm.Von.Should().Be(DateTime.Today.AddDays(-SageBuchungenGesamtsicht.StandardTage));
        vm.Zeilen.Should().HaveCount(3).And.OnlyContain(z => z.Anzeige != SageBuchungAnzeige.Uebergeben);
        vm.OffeneAusserhalb.Should().Be(3);
        var mitVon = (SageBookingQueueViewModel)((ViewResult)await Liste(db).Index(SageBuchungHerkunft.Wareneingang, null, false, null,
            DateTime.Today.AddDays(-50), null)).Model!;
        (mitVon.Zeilen.Count, mitVon.OffeneAusserhalb).Should().Be((4, 0));
    }

    [Fact]
    public async Task Umleitung_AlteLagerUrl_301_MitHerkunftUndUebersetztemStatus_v1_99_66_AK13()
    {
        var (ctrl, _) = await BuildAsync();
        var r = (RedirectToActionResult)ctrl.SageWareneingaenge(SageWareneingangStatus.Offen, "2026-1", new DateTime(2026, 11, 1), null);
        (r.Permanent, r.ControllerName, r.ActionName).Should().Be((true, "SageBookingQueue", "Index"));
        r.RouteValues!["herkunft"].Should().Be(SageBuchungHerkunft.Wareneingang);
        r.RouteValues["status"].Should().Be(SageBuchungAnzeige.Wartet);
        r.RouteValues["bestellung"].Should().Be("2026-1");
        r.RouteValues.ContainsKey("nurOffene").Should().BeFalse();
        var nurInSage = (RedirectToActionResult)ctrl.SageWareneingaenge(SageWareneingangStatus.NurInSage, null, null, null);
        nurInSage.RouteValues!["status"].Should().BeNull("NurInSage wird nicht angezeigt -> weggelassen");
        var einkauf = (RedirectToActionResult)new EinkaufPurchaseOrdersController(ctrl).SageWareneingaenge(SageWareneingangStatus.Fehler, null, null, null);
        (einkauf.Permanent, einkauf.RouteValues!["nurOffene"], einkauf.RouteValues["status"]).Should().Be((true, (object)true, (object)SageBuchungAnzeige.Fehler));
    }

    [Fact]
    public async Task Aktion_Ruecksprung_LokaleReturnUrl_SonstSageBuchungen_v1_99_66_AK5()
    {
        var (ctrl, _) = await BuildAsync();
        var url = new Mock<IUrlHelper>();
        url.Setup(u => u.IsLocalUrl("/SageBookingQueue?status=Fehler&page=2")).Returns(true);
        ctrl.Url = url.Object;
        var lokal = await ctrl.SageWeErneutSenden(VorgangA, "/SageBookingQueue?status=Fehler&page=2");
        lokal.Should().BeOfType<LocalRedirectResult>().Which.Url.Should().Be("/SageBookingQueue?status=Fehler&page=2");
        var fremd = (RedirectToActionResult)await ctrl.SageWeErneutSenden(VorgangA, "https://example.org/x");
        (fremd.ControllerName, fremd.ActionName, fremd.RouteValues!["herkunft"]).Should().Be(("SageBookingQueue", "Index", (object)SageBuchungHerkunft.Wareneingang));
    }

    [Fact]
    public async Task ErneutSenden_GanzeGruppeAufOffen_VersucheBleiben_ModifiedGesetzt_AK9()
    {
        var (ctrl, db) = await BuildAsync();
        await ctrl.SageWeErneutSenden(VorgangA, null);
        var a = db.PurchaseOrderReceipts.Where(r => r.SageWeVorgang == VorgangA).ToList();
        a.Should().OnlyContain(r => r.SageWeStatus == SageWareneingangStatus.Offen && r.SageWeVersuche == 1 && r.SageWeFehler == null
                                    && r.ModifiedBy == "Tester" && r.ModifiedByWindows == "AKE\\tester" && r.ModifiedAt != null);
        ctrl.TempData["SuccessMessage"]!.ToString().Should().StartWith("2 Position(en) erneut");
    }

    [Fact]
    public async Task ErneutSenden_NurBeiFehler_SonstHinweis_AK9()
    {
        var (ctrl, db) = await BuildAsync();
        await ctrl.SageWeErneutSenden(VorgangB, null);
        db.PurchaseOrderReceipts.Single(r => r.SageWeVorgang == VorgangB).SageWeStatus.Should().Be(SageWareneingangStatus.Uebergeben);
        ctrl.TempData["WarningMessage"].Should().NotBeNull();
    }

    [Fact]
    public void Rechte_ListeLesen_ErneutSendenKeyUser_AK8()
    {
        typeof(PurchaseOrdersController).GetCustomAttributes(typeof(RequireStockReadAccessAttribute), true).Should().NotBeEmpty();
        typeof(PurchaseOrdersController).GetMethod(nameof(PurchaseOrdersController.SageWeErneutSenden))!
            .GetCustomAttributes(typeof(RequireStockKeyUserAccessAttribute), false).Should().NotBeEmpty();
    }

    [Fact]
    public void Maske_StatusZeile_AnzahlJeStatus()
    {
        new ErfasstJeBelPos(3m, Array.Empty<(DateTime, decimal)>(),
                new[] { SageWareneingangStatus.Uebergeben, SageWareneingangStatus.Fehler, SageWareneingangStatus.Uebergeben })
            .SageWeText(PurchaseOrdersController.SageWeStatusText).Should().Be("Sage-Wareneingang: 2× an Sage übergeben, 1× Fehler");
        ErfasstJeBelPos.Leer.SageWeText(PurchaseOrdersController.SageWeStatusText).Should().BeNull();
    }

    // ---------------- v1.99.7 E5 / Q-2 ----------------

    [Fact]
    public async Task ErneutSenden_AuchKlaeren_AufOffen()
    {
        var (ctrl, db) = await BuildAsync();
        await ctrl.SageWeErneutSenden(VorgangC, null);
        db.PurchaseOrderReceipts.Single(r => r.SageWeVorgang == VorgangC).SageWeStatus.Should().Be(SageWareneingangStatus.Offen);
    }

    [Fact]
    public async Task Q2_SchalterAus_ErneutSendenGesperrt_HinweisMitAnzahl()
    {
        var (ctrl, db) = await BuildAsync(sageWeAn: false);
        await ctrl.SageWeErneutSenden(VorgangA, null);
        db.PurchaseOrderReceipts.Where(r => r.SageWeVorgang == VorgangA).Should().OnlyContain(r => r.SageWeStatus == SageWareneingangStatus.Fehler);
        ctrl.TempData["WarningMessage"]!.ToString().Should().Contain("ausgeschaltet");

        var vm = await WeAsync(Liste(db));
        vm.AusHinweis.Should().Be("Sage-Wareneingang ist aus – 3 Einträge offen, werden nicht gesendet. Auch nach dem Wiedereinschalten nur über „Erneut senden“.");
    }

    [Fact]
    public async Task AlsUebergeben_KlaerenUndFehler_MitBelegnummer_OhneNummerAbgelehnt()
    {
        var (ctrl, db) = await BuildAsync();
        await ctrl.SageWeAlsUebergeben(VorgangC, "  ", null);
        db.PurchaseOrderReceipts.Single(r => r.SageWeVorgang == VorgangC).SageWeStatus.Should().Be(SageWareneingangStatus.Klaeren);
        ctrl.TempData["WarningMessage"]!.ToString().Should().Contain("Belegnummer");

        await ctrl.SageWeAlsUebergeben(VorgangC, " 2027-398676 ", null);
        var c = db.PurchaseOrderReceipts.Single(r => r.SageWeVorgang == VorgangC);
        (c.SageWeStatus, c.SageWeBelegnummer, c.SageWeFehler, c.ModifiedBy).Should().Be((SageWareneingangStatus.Uebergeben, "2027-398676", (string?)null, "Tester"));
        c.SageWeAm.Should().NotBeNull();

        await ctrl.SageWeAlsUebergeben(VorgangB, "2027-1", null);
        ctrl.TempData["WarningMessage"]!.ToString().Should().Contain("nicht (mehr)", "Uebergeben bleibt unverändert");
        (await new PurchaseOrderReceiptRepository(db).SageWeAlsUebergebenAsync(VorgangA, "2027-2", "u", "w")).Should().Be(2);
    }

    [Fact]
    public async Task AlsUebergeben_Belegnummer45Zeichen_Angenommen_46Abgelehnt_v1_99_22()
    {
        var (ctrl, db) = await BuildAsync();
        await ctrl.SageWeAlsUebergeben(VorgangC, new string('9', 46), null);
        db.PurchaseOrderReceipts.Single(r => r.SageWeVorgang == VorgangC).SageWeStatus.Should().Be(SageWareneingangStatus.Klaeren);
        ctrl.TempData["WarningMessage"]!.ToString().Should().Contain("höchstens 45 Zeichen");

        var nr45 = "2027-" + new string('1', 40);
        await ctrl.SageWeAlsUebergeben(VorgangC, nr45, null);
        db.PurchaseOrderReceipts.Single(r => r.SageWeVorgang == VorgangC).SageWeBelegnummer.Should().Be(nr45);
    }

    [Fact]
    public void Belegnummer_ImModellUeberall45Zeichen_v1_99_22()
    {
        using var db = IdealAkeWms.Tests.Helpers.TestDbContextFactory.Create();
        int? Laenge<T>(string feld) => db.Model.FindEntityType(typeof(T))!.FindProperty(feld)!.GetMaxLength();
        new[] { Laenge<SagePurchaseOrderLine>("Belegnummer"), Laenge<PurchaseOrderReminder>("Belegnummer"), Laenge<PurchaseOrderReceipt>("Belegnummer"),
                Laenge<PurchaseOrderReceipt>("SageWeBelegnummer"), Laenge<ArticleSupplier>("LetzteBestellungBeleg") }
            .Should().OnlyContain(l => l == 45).And.HaveCount(5);
        SagePurchaseOrderLine.BelegnummerMaxLaenge.Should().Be(45, "Sage BelegnummerFormatiert string(45)");
    }

    [Fact]
    public void Attribute_AlsUebergeben_KeyUser_PostAntiForgery()
    {
        var m = typeof(PurchaseOrdersController).GetMethod(nameof(PurchaseOrdersController.SageWeAlsUebergeben))!;
        m.GetCustomAttributes(typeof(RequireStockKeyUserAccessAttribute), true).Should().NotBeEmpty();
        m.GetCustomAttributes(typeof(HttpPostAttribute), true).Should().NotBeEmpty();
        m.GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), true).Should().NotBeEmpty();
    }

    // ----- v1.99.11 (Spec 2026-11-12-wareneingang-barcode-lieferschein-csv-export F4, AK 9, 13, 15) -----

    private static async Task CsvSetup(ApplicationDbContext db, bool csvAn, string? pfad = @"\\srv\wms-csv")
    {
        foreach (var r in db.PurchaseOrderReceipts) r.Barcode = "BC" + r.BelPosId;
        if (csvAn) db.ServiceSettings.Add(new ServiceSetting { Key = WareneingangCsv.AktivKey, Value = "true" });
        if (pfad != null) db.ServiceSettings.Add(new ServiceSetting { Key = WareneingangCsv.PfadKey, Value = pfad });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Csv_Spalte_JeZustand_UndFilterUeberGerendertenText_AK15()
    {
        var (ctrl, db) = await BuildAsync();
        await CsvSetup(db, csvAn: true);
        db.WareneingangCsvExporte.Add(new WareneingangCsvExport { SageWeVorgang = VorgangA, Barcode = "BC1", Status = WareneingangCsvStatus.Fehler,
            Fehler = WareneingangCsv.KollisionText, Versuche = 1, CreatedAt = DateTime.Now, CreatedBy = "s", CreatedByWindows = "s" });
        await db.SaveChangesAsync();

        var vm = await WeAsync(Liste(db));
        Zeilen(vm).Where(z => z.Receipt.SageWeVorgang == VorgangA).Should().OnlyContain(z => z.Csv == "Fehler: " + WareneingangCsv.KollisionText && z.CsvFehler);
        Zeilen(vm).Single(z => z.Receipt.SageWeVorgang == VorgangB).Csv.Should().Be("wartet auf den Dienst (nächster Takt)");
        Zeilen(vm).Single(z => z.Receipt.SageWeVorgang == VorgangC).Csv.Should().Be("wartet auf Sage-Übergabe");
        vm.CsvHinweise.Should().BeEmpty();

        var gefiltert = await WeAsync(Liste(db, query: "?colf_csv=wartet"));
        Zeilen(gefiltert).Select(z => z.Receipt.SageWeVorgang).Should().BeEquivalentTo(new Guid?[] { VorgangB, VorgangC });
    }

    [Fact]
    public async Task Csv_SageWeAus_HinweisUndText_PfadLeer_Hinweis_AK9()
    {
        var (ctrl, db) = await BuildAsync(sageWeAn: false);
        await CsvSetup(db, csvAn: true, pfad: null);
        var vm = await WeAsync(Liste(db));
        Zeilen(vm).Single(z => z.Receipt.SageWeVorgang == VorgangB).Csv.Should().Be(WareneingangCsv.AusSageWeText);
        vm.CsvHinweise.Should().HaveCount(2);
        vm.CsvHinweise[0].Should().StartWith(WareneingangCsv.AusSageWeText).And.Contain("3 Wareneingänge mit Barcode");
        vm.CsvHinweise[1].Should().Be("CSV-Übergabe an, aber kein Pfad gesetzt – es wird nichts geschrieben.");
    }

    [Fact]
    public async Task Csv_SchalterAus_KeinHinweis_TextWartet_OhneBarcodeStrich()
    {
        var (ctrl, db) = await BuildAsync();
        await CsvSetup(db, csvAn: false);
        db.PurchaseOrderReceipts.Single(r => r.SageWeVorgang == VorgangC).Barcode = null;
        await db.SaveChangesAsync();
        var vm = await WeAsync(Liste(db));
        vm.CsvHinweise.Should().BeEmpty();
        Zeilen(vm).Single(z => z.Receipt.SageWeVorgang == VorgangB).Csv.Should().Be("CSV-Übergabe aus – wartet");
        Zeilen(vm).Single(z => z.Receipt.SageWeVorgang == VorgangC).Csv.Should().Be("–");
    }

    [Fact]
    public async Task CsvErneutSchreiben_NurBeiFehler_AufOffen_AuditGesetzt_AK13()
    {
        var (ctrl, db) = await BuildAsync();
        db.WareneingangCsvExporte.Add(new WareneingangCsvExport { SageWeVorgang = VorgangB, Barcode = "BC3", Status = WareneingangCsvStatus.Fehler,
            Fehler = "Zugriff verweigert", Versuche = 4, CreatedAt = DateTime.Now, CreatedBy = "s", CreatedByWindows = "s" });
        await db.SaveChangesAsync();

        await ctrl.CsvErneutSchreiben(VorgangB, null);
        var e = db.WareneingangCsvExporte.Single();
        (e.Status, e.Fehler, e.ModifiedBy, e.ModifiedByWindows).Should().Be((WareneingangCsvStatus.Offen, (string?)null, "Tester", "AKE\\tester"));

        await ctrl.CsvErneutSchreiben(VorgangB, null);
        ctrl.TempData["WarningMessage"].Should().Be("Nichts vorgemerkt: Die CSV-Übergabe steht nicht (mehr) auf „Fehler“.");
        typeof(PurchaseOrdersController).GetMethod(nameof(PurchaseOrdersController.CsvErneutSchreiben))!
            .GetCustomAttributes(typeof(RequireStockKeyUserAccessAttribute), false).Should().NotBeEmpty();
    }
}
