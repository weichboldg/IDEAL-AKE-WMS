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
using Moq;

namespace IdealAkeWms.Tests.Controllers;

/// <summary>Spec 2026-11-08-wareneingang-aus-sage-bestellungen E3 (AK 12, 13, 15-17) mit echten Repositories (InMemory).</summary>
public class PurchaseOrdersReceiveTests
{
    private static async Task<(PurchaseOrdersController ctrl, ApplicationDbContext db)> BuildAsync(bool lieferscheinPflicht = false, bool sageWe = false, bool barcode = false,
        string? lagerbestellungAktiv = null, string? bestellungenAktiv = null)
    {
        var db = TestDbContextFactory.Create();
        db.Articles.AddRange(
            new Article { Id = 5, ArticleNumber = "00722001", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" },
            new Article { Id = 6, ArticleNumber = "00722002", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        db.StorageLocations.Add(new StorageLocation { Id = 9, Code = "HR 0 0 0", IsActive = true, IstBuchbar = true, SageBuchungErlaubt = true,
            SageLagerkennung = "HR;0;0;0", SageLagerplatzId = 77, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        db.StorageLocations.Add(new StorageLocation { Id = 10, Code = "OHNE-SAGE", IsActive = true, IstBuchbar = true, SageBuchungErlaubt = true,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        db.StorageLocations.Add(new StorageLocation { Id = 11, Code = "SAGE-AUS", IsActive = true, IstBuchbar = true, SageBuchungErlaubt = false,
            SageLagerkennung = "HR;1;0;0", SageLagerplatzId = 78, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        if (sageWe) db.ServiceSettings.Add(new ServiceSetting { Key = SageWareneingang.ToggleKey, Value = "true" });
        SagePurchaseOrderLine L(int belPosId, int? articleId, string art, decimal offen, bool einheit = false, int belId = 100, string kz = "EBB") => new()
        {
            BelPosId = belPosId, BelId = belId, Belegkennzeichen = kz, Belegnummer = $"2026-{belId}", LieferantKto = "K1", LieferantName = "L",
            Artikelnummer = art, ArticleId = articleId, MengeBestellt = offen, MengeOffen = offen, Mengeneinheit = einheit ? "RLL" : "Stk",
            Lagermengeneinheit = "Stk", MengeBasiseinheit = "Stk", EinheitAbweichend = einheit, StorageLocationId = 9, SyncedAt = DateTime.Now,
        };
        db.SagePurchaseOrderLines.AddRange(L(1001, 5, "00722001", 10m), L(1002, 6, "00722002", 4m), L(1003, null, "FEHLT", 3m),
            L(1004, 5, "00722001", 2m, einheit: true), L(2001, 5, "00722001", 1m, belId: 200), L(3001, 5, "00722001", 1m, belId: 300),
            L(4001, 5, "00722001", 10m, belId: 400, kz: "EBZ"));   // v1.99.15: Rahmenabruf
        db.SagePurchaseOrderLines.Local.Single(l => l.BelPosId == 3001).Belegnummer = "2026-200"; // zwei Belege mit gleicher Nummer
        await db.SaveChangesAsync();

        var user = new Mock<ICurrentUserService>();
        user.Setup(u => u.GetDisplayName()).Returns("Tester");
        user.Setup(u => u.GetWindowsUserName()).Returns("AKE\\tester");
        user.Setup(u => u.CanAccessStockAsync()).ReturnsAsync(true);
        var settings = new Mock<IAppSettingRepository>();
        settings.Setup(x => x.GetValueAsync(AppSettingKeys.WareneingangLieferscheinPflicht)).ReturnsAsync(lieferscheinPflicht ? "true" : null);
        settings.Setup(x => x.GetValueAsync(AppSettingKeys.WareneingangBarcodeAktiv)).ReturnsAsync(barcode ? "true" : null);
        settings.Setup(x => x.GetValueAsync(AppSettingKeys.LagerbestellungAktiv)).ReturnsAsync(lagerbestellungAktiv);
        settings.Setup(x => x.GetValueAsync(AppSettingKeys.BestellungenAktiv)).ReturnsAsync(bestellungenAktiv);
        var ctrl = new PurchaseOrdersController(new SagePurchaseOrderLineRepository(db), new StorageLocationRepository(db),
            settings.Object, user.Object, new PurchaseOrderReceiptRepository(db), new StockMovementRepository(db), new ServiceSettingRepository(db), new PurchaseProposalRepository(db), new UserViewPreferenceRepository(db))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        ctrl.TempData = new TempDataDictionary(ctrl.HttpContext, Mock.Of<ITempDataProvider>());
        return (ctrl, db);
    }

    private static PurchaseOrderReceiveInput E(int belPosId, string? menge, bool ueber = false, int? platz = 9) =>
        new() { BelPosId = belPosId, Menge = menge, StorageLocationId = platz, UeberlieferungBestaetigt = ueber };

    /// <summary>v1.99.23 (WE-1): POST wie die Maske – jedes Absenden mit eigener Kennung (sonst „Seite veraltet“).</summary>
    private static Task<IActionResult> Post(PurchaseOrdersController ctrl, int belId, List<PurchaseOrderReceiveInput> zeilen,
        string? lieferschein = null, string? barcode = null, Guid? kennung = null) =>
        ctrl.Receive(belId, zeilen, lieferschein, barcode, kennung ?? Guid.NewGuid());

    [Fact]
    public async Task Einstieg_Belegnummer_Eindeutig_Unbekannt_Mehrfach_AK12()
    {
        var (ctrl, _) = await BuildAsync();
        ((RedirectToActionResult)await ctrl.Receive(null, " 2026-100 ")).RouteValues!["belId"].Should().Be(100);

        await ctrl.Receive(null, "2026-999");
        ctrl.TempData["WarningMessage"].Should().Be("Bestellung „2026-999“ nicht gefunden (nur offene Bestellungen und Rahmenabrufe aus Sage, Stand des letzten Abgleichs).");
        ((RedirectToActionResult)await ctrl.Receive(null, "2026-400")).RouteValues!["belId"].Should().Be(400, "Abruf per Belegnummer (AK 8)");

        await ctrl.Receive(null, "2026-200");
        ctrl.TempData["WarningMessage"]!.ToString().Should().Contain("gehört zu 2 Bestellungen");
    }

    [Fact]
    public async Task Buchen_ZweiPositionen_BewegungenMitWareneingang_Lieferart_Komma_AK13_AK15_AK17()
    {
        var (ctrl, db) = await BuildAsync();

        var r = await Post(ctrl, 100, new List<PurchaseOrderReceiveInput> { E(1001, "2,5"), E(1002, "4"), E(1003, null) });

        r.Should().BeOfType<RedirectToActionResult>();
        var bewegungen = await db.StockMovements.Include(m => m.PurchaseOrderReceipt).OrderBy(m => m.Id).ToListAsync();
        bewegungen.Should().HaveCount(2);
        bewegungen.Should().OnlyContain(m => m.MovementType == MovementType.Einbuchung && m.StorageLocationId == 9 && m.CreatedBy == "Tester"
            && m.PurchaseOrderReceipt != null && m.PurchaseOrderReceipt.StockMovementId == m.Id && m.PurchaseOrderReceipt.CreatedByWindows == "AKE\\tester");
        (bewegungen[0].Quantity, bewegungen[0].PurchaseOrderReceipt!.Lieferart).Should().Be((2.5m, PurchaseOrderReceiptLieferart.Teillieferung));
        (bewegungen[1].Quantity, bewegungen[1].PurchaseOrderReceipt!.Lieferart).Should().Be((4m, PurchaseOrderReceiptLieferart.Vollstaendig));
        ctrl.TempData["SuccessMessage"].Should().Be("2 Position(en) eingebucht (davon 1 Teil-, 0 Überlieferung).");

        var erfasst = await new PurchaseOrderReceiptRepository(db).GetErfasstJeBelPosAsync(new[] { 1001, 1002 });
        erfasst.ToDictionary(e => e.Key, e => e.Value.Gesamt).Should().BeEquivalentTo(new Dictionary<int, decimal> { [1001] = 2.5m, [1002] = 4m });
        var vm = (PurchaseOrderReceiveViewModel)((ViewResult)await ctrl.Receive(100, (string?)null)).Model!;
        vm.Zeilen.Single(z => z.Row.Line.BelPosId == 1001).Rest.Should().Be(7.5m, "Rest = offen laut Sage − im WMS erfasst");
    }

    [Fact]
    public async Task EineUngueltigeZeile_NichtsGebucht_AK16()
    {
        var (ctrl, db) = await BuildAsync();

        var r = await Post(ctrl, 100, new List<PurchaseOrderReceiveInput> { E(1001, "3"), E(1002, "0") });

        r.Should().BeOfType<ViewResult>();
        db.StockMovements.Should().BeEmpty();
        var vm = (PurchaseOrderReceiveViewModel)((ViewResult)r).Model!;
        vm.Zeilen.Single(z => z.Row.Line.BelPosId == 1002).Fehler.Should().Contain("größer als 0");
        vm.Zeilen.Single(z => z.Row.Line.BelPosId == 1001).Eingabe.Menge.Should().Be("3", "Eingaben bleiben stehen");
    }

    [Theory]
    [InlineData(1003, "1", "Artikel nicht im WMS")]
    [InlineData(1004, "1", "Einheit abweichend")]
    [InlineData(1001, "abc", "keine Zahl")]
    [InlineData(1001, "11", "bitte bestätigen")]
    [InlineData(1001, "1,2345", "Höchstens 3 Nachkommastellen")]  // QA Q-5
    public async Task NichtBuchbar_Gruende_AK15_AK16(int belPosId, string menge, string fehler)
    {
        var (ctrl, db) = await BuildAsync();
        var vm = (PurchaseOrderReceiveViewModel)((ViewResult)await Post(ctrl, 100, new List<PurchaseOrderReceiveInput> { E(belPosId, menge) })).Model!;
        vm.Zeilen.Single(z => z.Row.Line.BelPosId == belPosId).Fehler.Should().Contain(fehler);
        db.StockMovements.Should().BeEmpty();
    }

    [Fact]
    public async Task Ueberlieferung_MitBestaetigung_Gebucht_AK15()
    {
        var (ctrl, db) = await BuildAsync();
        await Post(ctrl, 100, new List<PurchaseOrderReceiveInput> { E(1001, "11", ueber: true) });
        db.PurchaseOrderReceipts.Single().Lieferart.Should().Be(PurchaseOrderReceiptLieferart.Ueberlieferung);
    }

    [Fact]
    public void Receive_NurMitSchreibrecht_AK10()
    {
        typeof(PurchaseOrdersController).GetMethods().Where(m => m.Name == nameof(PurchaseOrdersController.Receive))
            .Should().HaveCount(2).And.OnlyContain(m => m.GetCustomAttributes(typeof(RequireStockAccessAttribute), false).Any());
    }

    // ----- v1.99.4 (Wareneingang Teil 2, E2/F6, AK 5-7): Lieferschein-Nr. des Lieferanten -----

    [Fact]
    public async Task Lieferschein_Getrimmt_AnJedemReceiptDesAbsendens_AK5()
    {
        var (ctrl, db) = await BuildAsync();
        await Post(ctrl, 100, new List<PurchaseOrderReceiveInput> { E(1001, "1"), E(1002, "1") }, " LS-4711 ");
        db.PurchaseOrderReceipts.Select(r => r.Lieferschein).ToList().Should().Equal("LS-4711", "LS-4711");
    }

    [Fact]
    public async Task Lieferschein_LaengerAls20_Fehler_NichtsGebucht_EingabeBleibt_AK5()
    {
        var (ctrl, db) = await BuildAsync();
        var r = await Post(ctrl, 100, new List<PurchaseOrderReceiveInput> { E(1001, "1") }, new string('X', 21));
        var vm = (PurchaseOrderReceiveViewModel)((ViewResult)r).Model!;
        ctrl.ModelState[string.Empty]!.Errors.Select(e => e.ErrorMessage).Should().Contain("Lieferschein-Nr. höchstens 20 Zeichen (Sage-Feld EKLieferschein).");
        vm.Lieferschein.Should().Be(new string('X', 21));
        vm.Zeilen.Single(z => z.Row.Line.BelPosId == 1001).Eingabe.Menge.Should().Be("1");
        db.StockMovements.Should().BeEmpty();
    }

    [Theory]
    [InlineData(false, null, 1)]
    [InlineData(true, "   ", 0)]
    [InlineData(true, "LS-1", 1)]
    public async Task Lieferschein_Pflicht_NurPerEinstellung_AK6(bool pflicht, string? lieferschein, int gebucht)
    {
        var (ctrl, db) = await BuildAsync(lieferscheinPflicht: pflicht);
        await Post(ctrl, 100, new List<PurchaseOrderReceiveInput> { E(1001, "1") }, lieferschein);
        db.StockMovements.Count().Should().Be(gebucht);
        if (gebucht == 0)
            ctrl.ModelState[string.Empty]!.Errors.Select(e => e.ErrorMessage).Should().Contain("Bitte die Lieferschein-Nummer des Lieferanten eingeben.");
    }

    [Fact]
    public void Einstellung_LieferscheinPflicht_ImKatalog_SchalterStandardAus_AK7()
    {
        var d = AppSettingDefinitions.All.Single(x => x.Key == AppSettingKeys.WareneingangLieferscheinPflicht);
        (d.Type, d.DefaultValue, d.Seed, d.AbhaengigVon).Should().Be((SettingType.Bool, "false", false, AppSettingKeys.WareneingangBestellungAktiv));
        d.Anzeigename.Should().Be("Wareneingang: Lieferschein-Nr. Pflicht");
    }

    // ----- v1.99.4 (Wareneingang Teil 2, E4/F2, AK 10, 11, 14, 15): Status beim Buchen und Platzregel -----

    [Fact]
    public async Task SageWeAus_NichtNoetig_JederPlatzWieTeil1_AK10()
    {
        var (ctrl, db) = await BuildAsync();
        await Post(ctrl, 100, new List<PurchaseOrderReceiveInput> { E(1001, "1", platz: 10), E(1002, "1", platz: 11) });
        db.PurchaseOrderReceipts.Should().HaveCount(2).And.OnlyContain(r => r.SageWeStatus == SageWareneingangStatus.NichtNoetig && r.SageWeVorgang == null);
    }

    [Fact]
    public async Task SageWeAn_ZweiPositionen_Offen_GleicherVorgang_ZweitesAbsendenNeuerVorgang_AK11_AK12()
    {
        var (ctrl, db) = await BuildAsync(sageWe: true);
        await Post(ctrl, 100, new List<PurchaseOrderReceiveInput> { E(1001, "1"), E(1002, "1") });
        await Post(ctrl, 100, new List<PurchaseOrderReceiveInput> { E(1001, "1") });
        var r = db.PurchaseOrderReceipts.OrderBy(x => x.Id).ToList();
        r.Should().OnlyContain(x => x.SageWeStatus == SageWareneingangStatus.Offen && x.SageWeVorgang != null);
        r[0].SageWeVorgang.Should().Be(r[1].SageWeVorgang!.Value);
        r[2].SageWeVorgang.Should().NotBe(r[0].SageWeVorgang!.Value);
        db.SageBookingQueueItems.Should().BeEmpty();
    }

    [Theory]
    [InlineData(10, "OHNE-SAGE")]
    [InlineData(11, "SAGE-AUS")]
    public async Task SageWeAn_PlatzOhneSageOderSageBuchungAus_Zeilenfehler_NichtsGebucht_AK15(int platz, string code)
    {
        var (ctrl, db) = await BuildAsync(sageWe: true);
        var vm = (PurchaseOrderReceiveViewModel)((ViewResult)await Post(ctrl, 100,
            new List<PurchaseOrderReceiveInput> { E(1001, "1"), E(1002, "1", platz: platz) })).Model!;
        vm.Zeilen.Single(z => z.Row.Line.BelPosId == 1002).Fehler.Should().Be(SageWareneingang.PlatzFehler(code)).And.Contain(code);
        db.StockMovements.Should().BeEmpty("nichts aus diesem Absenden wird gebucht");
    }

    [Fact]
    public async Task SageWeAn_Chargenpflicht_NurInSage_OhneVorgang_KeinQueueEintrag_AK14()
    {
        var (ctrl, db) = await BuildAsync(sageWe: true);
        db.SagePurchaseOrderLines.Single(l => l.BelPosId == 1001).Chargenpflicht = true;
        await db.SaveChangesAsync();
        await Post(ctrl, 100, new List<PurchaseOrderReceiveInput> { E(1001, "1"), E(1002, "1") });
        var nurSage = db.PurchaseOrderReceipts.Single(r => r.BelPosId == 1001);
        (nurSage.SageWeStatus, nurSage.SageWeVorgang).Should().Be((SageWareneingangStatus.NurInSage, (Guid?)null));
        db.PurchaseOrderReceipts.Single(r => r.BelPosId == 1002).SageWeStatus.Should().Be(SageWareneingangStatus.Offen);
        db.SageBookingQueueItems.Should().BeEmpty();
    }

    // ----- v1.99.6 (Freigabe-Nachtrag 2026-11-12): Positionsmemo -----

    [Fact]
    public async Task Positionsmemo_AlsNotizDerBewegung_Getrimmt_LeerBleibtNull_AuchOhneSageWe()
    {
        var (ctrl, db) = await BuildAsync();
        var a = E(1001, "1"); a.Memo = "  Kante beschädigt ";
        await Post(ctrl, 100, new List<PurchaseOrderReceiveInput> { a, E(1002, "1") });
        db.StockMovements.OrderBy(m => m.Id).Select(m => m.Note).ToList().Should().Equal("Kante beschädigt", null);
    }

    [Fact]
    public async Task Positionsmemo_LaengerAls200_Zeilenfehler_NichtsGebucht()
    {
        var (ctrl, db) = await BuildAsync();
        var a = E(1001, "1"); a.Memo = new string('M', 201);
        var vm = (PurchaseOrderReceiveViewModel)((ViewResult)await Post(ctrl, 100, new List<PurchaseOrderReceiveInput> { a })).Model!;
        vm.Zeilen.Single(z => z.Row.Line.BelPosId == 1001).Fehler.Should().Be("Positionsmemo höchstens 200 Zeichen.");
        db.StockMovements.Should().BeEmpty();
    }

    // ----- v1.99.11 (Spec 2026-11-12-wareneingang-barcode-lieferschein-csv-export F1, AK 1-4) -----

    private static List<string> Fehler(PurchaseOrdersController c) => c.ModelState[string.Empty]?.Errors.Select(e => e.ErrorMessage).ToList() ?? new();

    [Fact]
    public async Task Barcode_SchalterAus_Ignoriert_AK1()
    {
        var (ctrl, db) = await BuildAsync(sageWe: true);
        await Post(ctrl, 100, new List<PurchaseOrderReceiveInput> { E(1001, "1") }, null, "AKE1235564654");
        db.PurchaseOrderReceipts.Single().Barcode.Should().BeNull();
        ((PurchaseOrderReceiveViewModel)((ViewResult)await ctrl.Receive(100, (string?)null)).Model!).BarcodeAktiv.Should().BeFalse();
    }

    [Fact]
    public async Task Barcode_Gueltig_AnAllenReceipts_MitKreditorname_AK2()
    {
        var (ctrl, db) = await BuildAsync(sageWe: true, barcode: true);
        await Post(ctrl, 100, new List<PurchaseOrderReceiveInput> { E(1001, "1"), E(1002, "1") }, "LS-1", " AKE1235564654 ");
        db.PurchaseOrderReceipts.Select(r => new { r.Barcode, r.LieferantName }).ToList()
            .Should().HaveCount(2).And.OnlyContain(r => r.Barcode == "AKE1235564654" && r.LieferantName == "L");
    }

    [Theory]
    [InlineData("AB;12")]
    [InlineData("AB 12")]
    [InlineData("Ä12")]
    [InlineData("CON")]
    public async Task Barcode_Ungueltig_Fehler_NichtsGebucht_EingabeBleibt_AK3(string barcode)
    {
        var (ctrl, db) = await BuildAsync(sageWe: true, barcode: true);
        var vm = (PurchaseOrderReceiveViewModel)((ViewResult)await Post(ctrl, 100, new List<PurchaseOrderReceiveInput> { E(1001, "1") }, null, barcode)).Model!;
        Fehler(ctrl).Should().Contain(WareneingangCsv.BarcodeFehlerText);
        vm.Barcode.Should().Be(barcode);
        db.StockMovements.Should().BeEmpty();
    }

    [Fact]
    public async Task Barcode_51Zeichen_Fehler_50Zeichen_Ok_AK3()
    {
        var (ctrl, db) = await BuildAsync(sageWe: true, barcode: true);
        await Post(ctrl, 100, new List<PurchaseOrderReceiveInput> { E(1001, "1") }, null, new string('A', 51));
        db.StockMovements.Should().BeEmpty();
        ctrl.ModelState.Clear();   // neuer Request
        await Post(ctrl, 100, new List<PurchaseOrderReceiveInput> { E(1001, "1") }, null, new string('A', 50));
        db.StockMovements.Should().HaveCount(1);
    }

    [Fact]
    public async Task Barcode_Duplikat_OhneGrossKlein_Fehler_MitBestellungUndDatum_AK4()
    {
        var (ctrl, db) = await BuildAsync(sageWe: true, barcode: true);
        await Post(ctrl, 100, new List<PurchaseOrderReceiveInput> { E(1001, "1") }, null, "AKE123");
        await Post(ctrl, 100, new List<PurchaseOrderReceiveInput> { E(1002, "1") }, null, "ake123");
        db.StockMovements.Should().HaveCount(1);
        Fehler(ctrl).Should().ContainSingle(e => e.StartsWith("Barcode ake123 ist bereits dem Wareneingang zu Bestellung 2026-100 vom "));
    }

    // ----- v1.99.33 (Spec 2026-11-17-wareneingang-sammellieferschein-und-bestellkopf A1, AK 1-4; B5 AK 13) -----

    [Fact]
    public async Task Sammellieferschein_GleicherLieferant_GleicheLsOhneGrossKlein_Gebucht_HinweisInErfolgsmeldung_AK1()
    {
        var (ctrl, db) = await BuildAsync(sageWe: true, barcode: true);
        await Post(ctrl, 100, new List<PurchaseOrderReceiveInput> { E(1001, "1") }, "L1", "B");
        await Post(ctrl, 200, new List<PurchaseOrderReceiveInput> { E(2001, "1") }, "l1", "b");
        Fehler(ctrl).Should().BeEmpty();
        db.PurchaseOrderReceipts.Select(r => r.Barcode).ToList().Should().Equal("B", "b");
        ctrl.TempData["SuccessMessage"].Should().Be("1 Position(en) eingebucht (davon 0 Teil-, 0 Überlieferung)."
            + " Sammellieferschein – Barcode b bereits bei Bestellung 2026-100 verwendet; die CSV-Datei erhält eine laufende Nummer (_1, _2 …).");
    }

    [Fact]
    public async Task Sammellieferschein_AndererLieferant_Fehler_NichtsGebucht_AK2()
    {
        var (ctrl, db) = await BuildAsync(sageWe: true, barcode: true);
        db.SagePurchaseOrderLines.Single(l => l.BelPosId == 3001).LieferantKto = "K2";
        await db.SaveChangesAsync();
        await Post(ctrl, 100, new List<PurchaseOrderReceiveInput> { E(1001, "1") }, "L1", "B");
        await Post(ctrl, 300, new List<PurchaseOrderReceiveInput> { E(3001, "1") }, "L1", "B");
        db.StockMovements.Should().HaveCount(1);
        Fehler(ctrl).Should().ContainSingle(e => e.StartsWith("Barcode B ist bereits dem Wareneingang zu Bestellung 2026-100 vom ")
            && e.EndsWith("zugeordnet – anderer Lieferant."));
    }

    [Theory]
    [InlineData("L1", "L2", "für einen Sammellieferschein bitte dieselbe Lieferschein-Nr. eingeben (dort: L1).")]   // AK 3
    [InlineData("L1", null, "für einen Sammellieferschein bitte dieselbe Lieferschein-Nr. eingeben (dort: L1).")]   // AK 4 (hier leer)
    [InlineData(null, "L1", "dort ohne Lieferschein-Nr., kein Sammellieferschein möglich.")]                       // AK 4 (dort leer)
    public async Task Sammellieferschein_LieferscheinUngleichOderLeer_Fehler_NichtsGebucht_AK3_AK4(string? ersterLs, string? zweiterLs, string ende)
    {
        var (ctrl, db) = await BuildAsync(sageWe: true, barcode: true);
        await Post(ctrl, 100, new List<PurchaseOrderReceiveInput> { E(1001, "1") }, ersterLs, "B");
        await Post(ctrl, 200, new List<PurchaseOrderReceiveInput> { E(2001, "1") }, zweiterLs, "B");
        db.StockMovements.Should().HaveCount(1);
        Fehler(ctrl).Should().ContainSingle(e => e.StartsWith("Barcode B ist bereits dem Wareneingang zu Bestellung 2026-100 vom ") && e.EndsWith(ende));
    }

    [Fact]
    public async Task Kopf_AbNummerUndKommission_AusDemCache_LeerBleibtNull_AK13()
    {
        var (ctrl, db) = await BuildAsync();
        foreach (var l in db.SagePurchaseOrderLines.Where(l => l.BelId == 100)) { l.AbNummer = "AB-4711"; l.Kommission = "K-12"; }
        await db.SaveChangesAsync();
        var vm = (PurchaseOrderReceiveViewModel)((ViewResult)await ctrl.Receive(100)).Model!;
        (vm.AbNummer, vm.Kommission).Should().Be(("AB-4711", "K-12"));
        var leer = (PurchaseOrderReceiveViewModel)((ViewResult)await ctrl.Receive(200)).Model!;
        (leer.AbNummer, leer.Kommission).Should().Be(((string?)null, (string?)null));
    }

    [Fact]
    public async Task Barcode_SageWeAus_Fehler_FeldGesperrt_Rueckfrage7()
    {
        var (ctrl, db) = await BuildAsync(barcode: true);
        await Post(ctrl, 100, new List<PurchaseOrderReceiveInput> { E(1001, "1") }, null, "AKE123");
        db.StockMovements.Should().BeEmpty();
        Fehler(ctrl).Should().ContainSingle(e => e.Contains("Sage-Wareneingang ist aus"));
        // ohne Barcode (gesperrtes Feld postet nichts) wird gebucht
        ctrl.ModelState.Clear();   // neuer Request
        await Post(ctrl, 100, new List<PurchaseOrderReceiveInput> { E(1001, "1") });
        db.PurchaseOrderReceipts.Single().Barcode.Should().BeNull();
    }

    [Fact]
    public void Einstellung_BarcodeAktiv_ImKatalog_SchalterStandardAus()
    {
        var d = AppSettingDefinitions.All.Single(x => x.Key == AppSettingKeys.WareneingangBarcodeAktiv);
        (d.Type, d.DefaultValue, d.Seed, d.AbhaengigVon).Should().Be((SettingType.Bool, "false", false, AppSettingKeys.WareneingangBestellungAktiv));
        d.Anzeigename.Should().Be("Wareneingang: Barcode-Feld (Lieferschein-Aufkleber)");
    }

    // ----- v1.99.15 (Spec 2026-11-11-wareneingang-teil-3-rahmenabrufe F4, AK 8-9) -----

    [Theory]
    [InlineData(true, "EBB", false, true)]
    [InlineData(true, "EBZ", false, false)]
    [InlineData(true, "EBZ", true, true)]
    [InlineData(false, "EBB", true, false)]
    [InlineData(false, "EBZ", true, false)]
    public void GiltFuer_SchalterUndKonstante(bool schalter, string kz, bool abrufMitSageWe, bool erwartet) =>
        SageWareneingang.GiltFuer(schalter, new SagePurchaseOrderLine { Belegkennzeichen = kz }, abrufMitSageWe).Should().Be(erwartet);

    [Fact]
    public void Konstante_RahmenabrufMitSageWe_BisZumSchreibtestAus() =>
        SageWareneingang.RahmenabrufMitSageWe.Should().BeFalse("E2 (Schreibtest) ist noch nicht erfolgt");

    [Fact]
    public async Task Abruf_SageWeAn_KonstanteAus_NichtNoetig_OhnePlatzregel_EbbUnveraendert_AK9()
    {
        var (ctrl, db) = await BuildAsync(sageWe: true);
        var vm = (PurchaseOrderReceiveViewModel)((ViewResult)await ctrl.Receive(400, (string?)null)).Model!;
        (vm.Rahmenabruf, vm.SageWeAktiv, vm.AbrufOhneSageWe).Should().Be((true, false, true));

        await Post(ctrl, 400, new List<PurchaseOrderReceiveInput> { E(4001, "3", platz: 10) });   // OHNE-SAGE: keine Platzregel
        var abruf = db.PurchaseOrderReceipts.Single();
        (abruf.SageWeStatus, abruf.SageWeVorgang).Should().Be((SageWareneingangStatus.NichtNoetig, (Guid?)null));

        await Post(ctrl, 100, new List<PurchaseOrderReceiveInput> { E(1001, "1") });
        db.PurchaseOrderReceipts.Single(r => r.BelPosId == 1001).SageWeStatus.Should().Be(SageWareneingangStatus.Offen, "EBB im selben Zustand unveraendert");
        var vmEbb = (PurchaseOrderReceiveViewModel)((ViewResult)await Post(ctrl, 100, new List<PurchaseOrderReceiveInput> { E(1001, "1", platz: 10) })).Model!;
        vmEbb.Zeilen.Single(z => z.Row.Line.BelPosId == 1001).Fehler.Should().Be(SageWareneingang.PlatzFehler("OHNE-SAGE"));
    }

    [Fact]
    public async Task Abruf_SageWeAn_KonstanteAus_Barcode_AbgewiesenMitAbrufText_AK9()
    {
        var (ctrl, db) = await BuildAsync(sageWe: true, barcode: true);
        await Post(ctrl, 400, new List<PurchaseOrderReceiveInput> { E(4001, "1") }, null, "AKE123");
        db.StockMovements.Should().BeEmpty();
        Fehler(ctrl).Should().ContainSingle(e => e == "Barcode nicht möglich: " + SageWareneingang.AbrufOhneSageWeText);
    }

    [Fact]
    public async Task Abruf_Rest_ZiehtAuchUebergebenesAb_Rest0_OeffnetTrotzdem_AK5()
    {
        var (ctrl, db) = await BuildAsync();
        await Post(ctrl, 400, new List<PurchaseOrderReceiveInput> { E(4001, "3") });
        var r = db.PurchaseOrderReceipts.Single();
        r.SageWeStatus = SageWareneingangStatus.Uebergeben; r.SageWeAm = DateTime.Now.AddDays(-1);   // vor SyncedAt: bei EBB nicht abgezogen
        await db.SaveChangesAsync();
        var vm = (PurchaseOrderReceiveViewModel)((ViewResult)await ctrl.Receive(400, (string?)null)).Model!;
        vm.Zeilen.Single().Rest.Should().Be(7m);

        await Post(ctrl, 400, new List<PurchaseOrderReceiveInput> { E(4001, "7") });
        vm = (PurchaseOrderReceiveViewModel)((ViewResult)await ctrl.Receive(400, (string?)null)).Model!;
        vm.Zeilen.Single().Rest.Should().Be(0m, "Abruf mit Rest 0 oeffnet sich trotzdem (Ueberlieferung mit Bestaetigung moeglich)");
    }

    // ----- v1.99.22 (Freigabe-Nachtrag 3): Hinweis offene Lagerbestellungen / Bedarfsmeldungen je Position -----

    private static async Task BedarfeSeedAsync(ApplicationDbContext db)
    {
        db.ProductionWorkplaces.Add(new ProductionWorkplace { Id = 7, Name = "Werkbank A1", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        db.ProductionOrders.Add(new ProductionOrder { Id = 8, OrderNumber = "2614167", SubOrderNumber = "2614167", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        db.WarehouseRequisitions.Add(new WarehouseRequisition { Id = 31, ProductionWorkplaceId = 7, Status = WarehouseRequisitionStatus.Submitted, CreatedBy = "Müller", CreatedByWindows = "t" });
        db.WarehouseRequisitions.Add(new WarehouseRequisition { Id = 32, ProductionWorkplaceId = 7, Status = WarehouseRequisitionStatus.Draft, CreatedBy = "Müller", CreatedByWindows = "t" });
        db.WarehouseRequisitionItems.AddRange(
            new WarehouseRequisitionItem { WarehouseRequisitionId = 31, Position = 1, ArticleNumber = " 00722001", ArticleDescription = "x", Unit = "Stk", QuantityRequested = 5m, CreatedBy = "t", CreatedByWindows = "t" },
            new WarehouseRequisitionItem { WarehouseRequisitionId = 32, Position = 1, ArticleNumber = "00722001", ArticleDescription = "x", Unit = "Stk", QuantityRequested = 99m, CreatedBy = "t", CreatedByWindows = "t" });
        db.PartRequisitions.AddRange(
            new PartRequisition { Id = 41, ProductionOrderId = 8, ArticleNumber = "00722002", Quantity = 3m, Unit = "Stk", Status = PartRequisitionStatus.Offen, CreatedBy = "Huber", CreatedByWindows = "t" },
            new PartRequisition { Id = 42, ProductionOrderId = 8, ArticleNumber = "00722002", Quantity = 7m, Status = PartRequisitionStatus.Erfuellt, CreatedBy = "Huber", CreatedByWindows = "t" });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Hinweis_OffeneLagerbestellungUndBedarfsmeldung_JeArtikel_NurOffene()
    {
        var (ctrl, db) = await BuildAsync(bestellungenAktiv: "true");
        await BedarfeSeedAsync(db);

        var vm = (PurchaseOrderReceiveViewModel)((ViewResult)await ctrl.Receive(belId: 100)).Model!;

        var z1 = vm.Zeilen.First(z => z.Row.Line.Artikelnummer == "00722001");
        z1.OffeneLagerbestellungen.Should().ContainSingle().Which.Should().Be(new OffenerBedarf(" 00722001", "Stk", 5m, 31, "Werkbank A1", "Müller"));
        z1.OffeneBedarfsmeldungen.Should().BeEmpty();
        var z2 = vm.Zeilen.Single(z => z.Row.Line.Artikelnummer == "00722002");
        z2.OffeneLagerbestellungen.Should().BeEmpty();
        z2.OffeneBedarfsmeldungen.Should().ContainSingle().Which.Should().Be(new OffenerBedarf("00722002", "Stk", 3m, 41, "2614167", "Huber", 8));
    }

    [Fact]
    public async Task Hinweis_SchalterAus_KeineHinweise_BuchenUnveraendert()
    {
        var (ctrl, db) = await BuildAsync(lagerbestellungAktiv: "false", bestellungenAktiv: null);
        await BedarfeSeedAsync(db);

        var vm = (PurchaseOrderReceiveViewModel)((ViewResult)await ctrl.Receive(belId: 100)).Model!;

        vm.Zeilen.Should().OnlyContain(z => z.OffeneLagerbestellungen.Count == 0 && z.OffeneBedarfsmeldungen.Count == 0);
        db.PartRequisitions.Single(p => p.Id == 41).Status.Should().Be(PartRequisitionStatus.Offen, "nur Anzeige, nichts erfuellt");
    }

    // --- v1.99.23 (WE-1/WE-4): Absendekennung ---

    [Fact]
    public async Task ZweiterPost_GleicheKennung_NichtsGebucht_Warnung_AK6()
    {
        var (ctrl, db) = await BuildAsync(barcode: true, sageWe: true);
        var k = Guid.NewGuid();
        await Post(ctrl, 100, new List<PurchaseOrderReceiveInput> { E(1001, "3"), E(1002, "1") }, null, "AKE777", k);
        db.PurchaseOrderReceipts.Should().HaveCount(2);

        var r = await Post(ctrl, 100, new List<PurchaseOrderReceiveInput> { E(1001, "3"), E(1002, "1") }, null, "AKE777", k);

        ((RedirectToActionResult)r).RouteValues!["belId"].Should().Be(100);
        ctrl.TempData["WarningMessage"].Should().Be(PurchaseOrdersController.BereitsGebuchtText);
        db.PurchaseOrderReceipts.Should().HaveCount(2, "nichts doppelt gebucht");
        db.StockMovements.Should().HaveCount(2);
    }

    [Fact]
    public async Task JederReceipt_TraegtKennung_AuchOhneSageWe_AK7()
    {
        var (ctrl, db) = await BuildAsync();
        var k = Guid.NewGuid();
        await Post(ctrl, 100, new List<PurchaseOrderReceiveInput> { E(1001, "1"), E(1002, "1") }, kennung: k);
        db.PurchaseOrderReceipts.AsEnumerable().Select(x => (x.SageWeStatus, x.Absendekennung)).ToList()
            .Should().OnlyContain(x => x.SageWeStatus == SageWareneingangStatus.NichtNoetig && x.Absendekennung == k).And.HaveCount(2);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Post_OhneKennung_Abgelehnt_NichtsGebucht_AK8(bool leer)
    {
        var (ctrl, db) = await BuildAsync();
        var r = await ctrl.Receive(100, new List<PurchaseOrderReceiveInput> { E(1001, "1") }, null, null, leer ? Guid.Empty : null);
        ((RedirectToActionResult)r).RouteValues!["belId"].Should().Be(100);
        ctrl.TempData["WarningMessage"].Should().Be(PurchaseOrdersController.SeiteVeraltetText);
        db.StockMovements.Should().BeEmpty();
    }

    [Fact]
    public async Task Pruefehler_DannKorrektur_GleicheKennung_BuchtEinmal()
    {
        var (ctrl, db) = await BuildAsync();
        var k = Guid.NewGuid();
        var vm = (PurchaseOrderReceiveViewModel)((ViewResult)await Post(ctrl, 100, new List<PurchaseOrderReceiveInput> { E(1001, "abc") }, kennung: k)).Model!;
        vm.Absendekennung.Should().Be(k, "die Korrektur sendet dieselbe Kennung");
        db.StockMovements.Should().BeEmpty();
        ctrl.ModelState.Clear();   // neue Anfrage = neuer ModelState
        await Post(ctrl, 100, new List<PurchaseOrderReceiveInput> { E(1001, "2") }, kennung: k);
        db.PurchaseOrderReceipts.Should().ContainSingle().Which.Absendekennung.Should().Be(k);
    }

    [Fact]
    public async Task Get_SetztNeueKennung()
    {
        var (ctrl, _) = await BuildAsync();
        var a = ((PurchaseOrderReceiveViewModel)((ViewResult)await ctrl.Receive(100, (string?)null)).Model!).Absendekennung;
        var b = ((PurchaseOrderReceiveViewModel)((ViewResult)await ctrl.Receive(100, (string?)null)).Model!).Absendekennung;
        a.Should().NotBe(Guid.Empty);
        b.Should().NotBe(a);
    }
}
