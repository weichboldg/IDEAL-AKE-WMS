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
    private static async Task<(PurchaseOrdersController ctrl, ApplicationDbContext db)> BuildAsync(bool lieferscheinPflicht = false)
    {
        var db = TestDbContextFactory.Create();
        db.Articles.AddRange(
            new Article { Id = 5, ArticleNumber = "00722001", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" },
            new Article { Id = 6, ArticleNumber = "00722002", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        db.StorageLocations.Add(new StorageLocation { Id = 9, Code = "HR 0 0 0", IsActive = true, IstBuchbar = true, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        SagePurchaseOrderLine L(int belPosId, int? articleId, string art, decimal offen, bool einheit = false, int belId = 100) => new()
        {
            BelPosId = belPosId, BelId = belId, Belegkennzeichen = "EBB", Belegnummer = $"2026-{belId}", LieferantKto = "K1", LieferantName = "L",
            Artikelnummer = art, ArticleId = articleId, MengeBestellt = offen, MengeOffen = offen, Mengeneinheit = einheit ? "RLL" : "Stk",
            Lagermengeneinheit = "Stk", MengeBasiseinheit = "Stk", EinheitAbweichend = einheit, StorageLocationId = 9, SyncedAt = DateTime.Now,
        };
        db.SagePurchaseOrderLines.AddRange(L(1001, 5, "00722001", 10m), L(1002, 6, "00722002", 4m), L(1003, null, "FEHLT", 3m),
            L(1004, 5, "00722001", 2m, einheit: true), L(2001, 5, "00722001", 1m, belId: 200), L(3001, 5, "00722001", 1m, belId: 300));
        db.SagePurchaseOrderLines.Local.Single(l => l.BelPosId == 3001).Belegnummer = "2026-200"; // zwei Belege mit gleicher Nummer
        await db.SaveChangesAsync();

        var user = new Mock<ICurrentUserService>();
        user.Setup(u => u.GetDisplayName()).Returns("Tester");
        user.Setup(u => u.GetWindowsUserName()).Returns("AKE\\tester");
        user.Setup(u => u.CanAccessStockAsync()).ReturnsAsync(true);
        var settings = new Mock<IAppSettingRepository>();
        settings.Setup(x => x.GetValueAsync(AppSettingKeys.WareneingangLieferscheinPflicht)).ReturnsAsync(lieferscheinPflicht ? "true" : null);
        var ctrl = new PurchaseOrdersController(new SagePurchaseOrderLineRepository(db), new StorageLocationRepository(db),
            settings.Object, user.Object, new PurchaseOrderReceiptRepository(db), new StockMovementRepository(db))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        ctrl.TempData = new TempDataDictionary(ctrl.HttpContext, Mock.Of<ITempDataProvider>());
        return (ctrl, db);
    }

    private static PurchaseOrderReceiveInput E(int belPosId, string? menge, bool ueber = false, int? platz = 9) =>
        new() { BelPosId = belPosId, Menge = menge, StorageLocationId = platz, UeberlieferungBestaetigt = ueber };

    [Fact]
    public async Task Einstieg_Belegnummer_Eindeutig_Unbekannt_Mehrfach_AK12()
    {
        var (ctrl, _) = await BuildAsync();
        ((RedirectToActionResult)await ctrl.Receive(null, " 2026-100 ")).RouteValues!["belId"].Should().Be(100);

        await ctrl.Receive(null, "2026-999");
        ctrl.TempData["WarningMessage"].Should().Be("Bestellung „2026-999“ nicht gefunden (nur offene normale Bestellungen aus Sage, Stand des letzten Abgleichs).");

        await ctrl.Receive(null, "2026-200");
        ctrl.TempData["WarningMessage"]!.ToString().Should().Contain("gehört zu 2 Bestellungen");
    }

    [Fact]
    public async Task Buchen_ZweiPositionen_BewegungenMitWareneingang_Lieferart_Komma_AK13_AK15_AK17()
    {
        var (ctrl, db) = await BuildAsync();

        var r = await ctrl.Receive(100, new List<PurchaseOrderReceiveInput> { E(1001, "2,5"), E(1002, "4"), E(1003, null) });

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

        var r = await ctrl.Receive(100, new List<PurchaseOrderReceiveInput> { E(1001, "3"), E(1002, "0") });

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
        var vm = (PurchaseOrderReceiveViewModel)((ViewResult)await ctrl.Receive(100, new List<PurchaseOrderReceiveInput> { E(belPosId, menge) })).Model!;
        vm.Zeilen.Single(z => z.Row.Line.BelPosId == belPosId).Fehler.Should().Contain(fehler);
        db.StockMovements.Should().BeEmpty();
    }

    [Fact]
    public async Task Ueberlieferung_MitBestaetigung_Gebucht_AK15()
    {
        var (ctrl, db) = await BuildAsync();
        await ctrl.Receive(100, new List<PurchaseOrderReceiveInput> { E(1001, "11", ueber: true) });
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
        await ctrl.Receive(100, new List<PurchaseOrderReceiveInput> { E(1001, "1"), E(1002, "1") }, " LS-4711 ");
        db.PurchaseOrderReceipts.Select(r => r.Lieferschein).ToList().Should().Equal("LS-4711", "LS-4711");
    }

    [Fact]
    public async Task Lieferschein_LaengerAls20_Fehler_NichtsGebucht_EingabeBleibt_AK5()
    {
        var (ctrl, db) = await BuildAsync();
        var r = await ctrl.Receive(100, new List<PurchaseOrderReceiveInput> { E(1001, "1") }, new string('X', 21));
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
        await ctrl.Receive(100, new List<PurchaseOrderReceiveInput> { E(1001, "1") }, lieferschein);
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
}
