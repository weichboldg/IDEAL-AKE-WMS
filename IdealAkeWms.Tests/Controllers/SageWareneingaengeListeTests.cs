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
    private static readonly Guid VorgangA = Guid.NewGuid(), VorgangB = Guid.NewGuid();

    private static async Task<(PurchaseOrdersController ctrl, ApplicationDbContext db)> BuildAsync()
    {
        var db = TestDbContextFactory.Create();
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
            M(3, SageWareneingangStatus.Uebergeben, VorgangB), M(4, SageWareneingangStatus.NichtNoetig, null));
        await db.SaveChangesAsync();

        var user = new Mock<ICurrentUserService>();
        user.Setup(u => u.GetDisplayName()).Returns("Tester");
        user.Setup(u => u.GetWindowsUserName()).Returns("AKE\\tester");
        user.Setup(u => u.CanTransferStockAsync()).ReturnsAsync(true);
        var ctrl = new PurchaseOrdersController(new SagePurchaseOrderLineRepository(db), new StorageLocationRepository(db), Mock.Of<IAppSettingRepository>(),
            user.Object, new PurchaseOrderReceiptRepository(db), new StockMovementRepository(db))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        ctrl.TempData = new TempDataDictionary(ctrl.HttpContext, Mock.Of<ITempDataProvider>());
        return (ctrl, db);
    }

    [Fact]
    public async Task Liste_OhneNichtNoetig_MitMemoDerGruppe_StatusFilter_AK8()
    {
        var (ctrl, _) = await BuildAsync();
        var vm = (SageWareneingaengeViewModel)((ViewResult)await ctrl.SageWareneingaenge(null, null, null, null)).Model!;
        vm.Zeilen.Should().HaveCount(3, "NichtNoetig (Teil 1) erscheint nicht");
        var ersteId = vm.Zeilen.Where(z => z.Receipt.SageWeVorgang == VorgangA).Min(z => z.Receipt.Id);
        vm.Zeilen.Where(z => z.Receipt.SageWeVorgang == VorgangA).Should().OnlyContain(z => z.Memo == $"WE#{ersteId}#");
        vm.Pagination.TotalCount.Should().Be(3);

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
}
