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

/// <summary>v1.99.4 (Spec 2026-11-08-wareneingang-teil-2-sage-we-beleg-rahmenabrufe E3, AK 8/9): Liste „Wareneingänge an Sage“ und „Erneut senden“.</summary>
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
            user.Object, new PurchaseOrderReceiptRepository(db), new StockMovementRepository(db), new ServiceSettingRepository(db))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        ctrl.TempData = new TempDataDictionary(ctrl.HttpContext, Mock.Of<ITempDataProvider>());
        return (ctrl, db);
    }

    [Fact]
    public async Task Liste_Memo_GespeicherteKennung_AltbestandAltesFormat_NieGesendetMitKuerzel_v1_99_9()
    {
        var (ctrl, db) = await BuildAsync();
        db.ServiceSettings.Add(new ServiceSetting { Key = SageSystemKuerzel.Key, Value = "DEV" });
        var b = db.PurchaseOrderReceipts.Single(r => r.SageWeVorgang == VorgangB);
        b.SageWeKennung = $"WE#AT#{b.Id}#";
        await db.SaveChangesAsync();

        var vm = (SageWareneingaengeViewModel)((ViewResult)await ctrl.SageWareneingaenge(null, null, null, null)).Model!;

        var a = vm.Zeilen.Where(z => z.Receipt.SageWeVorgang == VorgangA).Min(z => z.Receipt.Id);
        vm.Zeilen.Where(z => z.Receipt.SageWeVorgang == VorgangA).Should().OnlyContain(z => z.Memo == $"WE#{a}#", "gesendet vor v1.99.9 = altes Format");
        vm.Zeilen.Single(z => z.Receipt.SageWeVorgang == VorgangB).Memo.Should().Be($"WE#AT#{b.Id}#", "gespeicherte Kennung");
        var c = vm.Zeilen.Single(z => z.Receipt.SageWeVorgang == VorgangC);
        c.Memo.Should().Be($"WE#DEV#{c.Receipt.Id}#", "nie gesendet = aktuelles Kuerzel, wie der Dienst");
    }

    [Fact]
    public async Task Liste_OhneNichtNoetig_MitMemoDerGruppe_StatusFilter_AK8()
    {
        var (ctrl, _) = await BuildAsync();
        var vm = (SageWareneingaengeViewModel)((ViewResult)await ctrl.SageWareneingaenge(null, null, null, null)).Model!;
        vm.Zeilen.Should().HaveCount(4, "NichtNoetig (Teil 1) erscheint nicht");
        var ersteId = vm.Zeilen.Where(z => z.Receipt.SageWeVorgang == VorgangA).Min(z => z.Receipt.Id);
        vm.Zeilen.Where(z => z.Receipt.SageWeVorgang == VorgangA).Should().OnlyContain(z => z.Memo == $"WE#{ersteId}#");
        vm.Pagination.TotalCount.Should().Be(4);
        vm.AusHinweis.Should().BeNull("Schalter an");

        var fehler = (SageWareneingaengeViewModel)((ViewResult)await ctrl.SageWareneingaenge(SageWareneingangStatus.Fehler, null, null, null)).Model!;
        fehler.Zeilen.Should().HaveCount(2);
    }

    [Fact]
    public async Task ErneutSenden_GanzeGruppeAufOffen_VersucheBleiben_ModifiedGesetzt_AK9()
    {
        var (ctrl, db) = await BuildAsync();
        await ctrl.SageWeErneutSenden(VorgangA);
        var a = db.PurchaseOrderReceipts.Where(r => r.SageWeVorgang == VorgangA).ToList();
        a.Should().OnlyContain(r => r.SageWeStatus == SageWareneingangStatus.Offen && r.SageWeVersuche == 1 && r.SageWeFehler == null
                                    && r.ModifiedBy == "Tester" && r.ModifiedByWindows == "AKE\\tester" && r.ModifiedAt != null);
        ctrl.TempData["SuccessMessage"]!.ToString().Should().StartWith("2 Position(en) erneut");
    }

    [Fact]
    public async Task ErneutSenden_NurBeiFehler_SonstHinweis_AK9()
    {
        var (ctrl, db) = await BuildAsync();
        await ctrl.SageWeErneutSenden(VorgangB);
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
        await ctrl.SageWeErneutSenden(VorgangC);
        db.PurchaseOrderReceipts.Single(r => r.SageWeVorgang == VorgangC).SageWeStatus.Should().Be(SageWareneingangStatus.Offen);
    }

    [Fact]
    public async Task Q2_SchalterAus_ErneutSendenGesperrt_HinweisMitAnzahl()
    {
        var (ctrl, db) = await BuildAsync(sageWeAn: false);
        await ctrl.SageWeErneutSenden(VorgangA);
        db.PurchaseOrderReceipts.Where(r => r.SageWeVorgang == VorgangA).Should().OnlyContain(r => r.SageWeStatus == SageWareneingangStatus.Fehler);
        ctrl.TempData["WarningMessage"]!.ToString().Should().Contain("ausgeschaltet");

        var vm = (SageWareneingaengeViewModel)((ViewResult)await ctrl.SageWareneingaenge(null, null, null, null)).Model!;
        vm.AusHinweis.Should().Be("Sage-Wareneingang ist aus – 3 Einträge offen, werden nicht gesendet. Auch nach dem Wiedereinschalten nur über „Erneut senden“.");
    }

    [Fact]
    public async Task AlsUebergeben_KlaerenUndFehler_MitBelegnummer_OhneNummerAbgelehnt()
    {
        var (ctrl, db) = await BuildAsync();
        await ctrl.SageWeAlsUebergeben(VorgangC, "  ");
        db.PurchaseOrderReceipts.Single(r => r.SageWeVorgang == VorgangC).SageWeStatus.Should().Be(SageWareneingangStatus.Klaeren);
        ctrl.TempData["WarningMessage"]!.ToString().Should().Contain("Belegnummer");

        await ctrl.SageWeAlsUebergeben(VorgangC, " 2027-398676 ");
        var c = db.PurchaseOrderReceipts.Single(r => r.SageWeVorgang == VorgangC);
        (c.SageWeStatus, c.SageWeBelegnummer, c.SageWeFehler, c.ModifiedBy).Should().Be((SageWareneingangStatus.Uebergeben, "2027-398676", (string?)null, "Tester"));
        c.SageWeAm.Should().NotBeNull();

        await ctrl.SageWeAlsUebergeben(VorgangB, "2027-1");
        ctrl.TempData["WarningMessage"]!.ToString().Should().Contain("nicht (mehr)", "Uebergeben bleibt unverändert");
        (await new PurchaseOrderReceiptRepository(db).SageWeAlsUebergebenAsync(VorgangA, "2027-2", "u", "w")).Should().Be(2);
    }

    [Fact]
    public void Attribute_AlsUebergeben_KeyUser_PostAntiForgery()
    {
        var m = typeof(PurchaseOrdersController).GetMethod(nameof(PurchaseOrdersController.SageWeAlsUebergeben))!;
        m.GetCustomAttributes(typeof(RequireStockKeyUserAccessAttribute), true).Should().NotBeEmpty();
        m.GetCustomAttributes(typeof(HttpPostAttribute), true).Should().NotBeEmpty();
        m.GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), true).Should().NotBeEmpty();
    }
}
