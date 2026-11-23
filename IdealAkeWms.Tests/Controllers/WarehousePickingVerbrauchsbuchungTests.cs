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
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Controllers;

/// <summary>v1.99.53 (TS-178): Lieferung einer Lager-/Glasbestellung als WMS-Ausbuchung. Echte Repositories + InMemory.
/// Nicht InMemory-pruefbar (Manual-UAT TS-178.8): gemeinsame Transaktion Abschluss + Ausbuchungen und RowVersion-Konflikt.</summary>
public class WarehousePickingVerbrauchsbuchungTests
{
    private const int WmsPlatz = 1, ZweitPlatz = 2, Nan = 3, SagePlatzOhne = 4, SagePlatzMit = 5, NichtBuchbar = 6;

    private static (WarehousePickingController ctrl, ApplicationDbContext db) Build(bool aktiv = true, bool negativ = false, bool sageAn = false)
    {
        var db = TestDbContextFactory.Create();
        StorageLocation L(int id, string code, bool buchbar = true, string source = StorageLocationSource.Manual,
            bool sageErlaubt = false, string? kennung = null, int? sageId = null) => new()
        {
            Id = id, Code = code, IsActive = true, IstBuchbar = buchbar, Source = source, SageBuchungErlaubt = sageErlaubt,
            SageLagerkennung = kennung, SageLagerplatzId = sageId, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        };
        db.StorageLocations.AddRange(L(WmsPlatz, "A-01"), L(ZweitPlatz, "B-02"), L(Nan, "NAN"),
            L(SagePlatzOhne, "HR 1", source: StorageLocationSource.Sage),
            L(SagePlatzMit, "HR 2", source: StorageLocationSource.Sage, sageErlaubt: true, kennung: "HR;2", sageId: 2),
            L(NichtBuchbar, "GESPERRT", buchbar: false));
        db.Articles.AddRange(
            new Article { Id = 1, ArticleNumber = "ART-1", Unit = "Stk", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" },
            new Article { Id = 2, ArticleNumber = "ART-2", Unit = "Stk", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        if (aktiv) db.AppSettings.Add(new AppSetting { Key = AppSettingKeys.LagerbestellungVerbrauchsbuchungAktiv, Value = "true" });
        if (negativ) db.AppSettings.Add(new AppSetting { Key = AppSettingKeys.NegativeBuchungErlaubt, Value = "true" });
        if (sageAn) db.ServiceSettings.Add(new ServiceSetting { Key = "SageLagerbuchungAktiv", Value = "true" });
        db.SaveChanges();

        var user = new Mock<ICurrentUserService>();
        user.Setup(u => u.GetCurrentAppUserId()).Returns((int?)null);
        user.Setup(u => u.GetDisplayName()).Returns("stocker");
        user.Setup(u => u.GetWindowsUserName()).Returns("DOMAIN\\stocker");
        var ctrl = new WarehousePickingController(new WarehouseRequisitionRepository(db), new ProductionWorkplaceRepository(db),
            new StockMovementRepository(db), user.Object, Mock.Of<IUserViewPreferenceRepository>(), new ArticleRepository(db),
            new StorageLocationRepository(db), new AppSettingRepository(db), new ServiceSettingRepository(db), db);
        var http = new DefaultHttpContext();
        ctrl.ControllerContext = new ControllerContext { HttpContext = http };
        var url = new Mock<IUrlHelper>();
        url.Setup(u => u.Action(It.IsAny<UrlActionContext>())).Returns("/WarehousePicking/Print/1");
        ctrl.Url = url.Object;
        ctrl.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());
        return (ctrl, db);
    }

    private static void Bestand(ApplicationDbContext db, int articleId, int platz, decimal menge)
    {
        db.StockMovements.Add(new StockMovement
        {
            ArticleId = articleId, StorageLocationId = platz, Quantity = menge, MovementType = MovementType.Einbuchung,
            Timestamp = DateTime.Now, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        db.SaveChanges();
    }

    private static (WarehouseRequisition r, int[] ids) Liste(ApplicationDbContext db, params string[] artikel)
    {
        var wp = new ProductionWorkplace { Name = "WB-1", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        db.ProductionWorkplaces.Add(wp); db.SaveChanges();
        var r = new WarehouseRequisition
        {
            ProductionWorkplaceId = wp.Id, Status = WarehouseRequisitionStatus.Submitted, SubmittedAt = DateTime.Now,
            CreatedAt = DateTime.Now, CreatedBy = "x", CreatedByWindows = "x"
        };
        for (var p = 0; p < artikel.Length; p++)
            r.Items.Add(new WarehouseRequisitionItem
            {
                ArticleNumber = artikel[p], ArticleDescription = "x", Unit = "Stk", QuantityRequested = 5, Position = p + 1,
                CreatedAt = DateTime.Now, CreatedBy = "x", CreatedByWindows = "x"
            });
        db.WarehouseRequisitions.Add(r); db.SaveChanges();
        return (r, r.Items.OrderBy(i => i.Position).Select(i => i.Id).ToArray());
    }

    private static Task<IActionResult> Close(WarehousePickingController c, WarehouseRequisition r, int[] ids, int?[] ist, int?[]? plaetze, int[]? fehlteil = null) =>
        c.Close(r.Id, ids, ist, null, null, fehlteil ?? ids.Select(_ => 1).ToArray(), r.RowVersion, plaetze);

    private static List<StockMovement> Ausbuchungen(ApplicationDbContext db) =>
        db.StockMovements.AsNoTracking().Where(m => m.MovementType == MovementType.Ausbuchung).ToList();

    private static void ZurueckAufTeilgeliefert(ApplicationDbContext db, int id)
    {
        var r = db.WarehouseRequisitions.First(x => x.Id == id);
        r.Status = WarehouseRequisitionStatus.PartiallyDelivered;
        db.SaveChanges();
    }

    [Fact]
    public async Task SchalterAus_BuchtNichts_AK1()
    {
        var (c, db) = Build(aktiv: false);
        Bestand(db, 1, WmsPlatz, 10);
        var (r, ids) = Liste(db, "ART-1");

        var res = await Close(c, r, ids, new int?[] { 5 }, null) as RedirectToActionResult;

        res!.ActionName.Should().Be("Index");
        Ausbuchungen(db).Should().BeEmpty();
        db.WarehouseRequisitions.AsNoTracking().First(x => x.Id == r.Id).Status.Should().Be(WarehouseRequisitionStatus.Closed);
    }

    [Fact]
    public async Task Abschluss_BuchtIst_DannNurDifferenz_UnveraendertNichts_AK2()
    {
        var (c, db) = Build();
        Bestand(db, 1, WmsPlatz, 10);
        Bestand(db, 1, ZweitPlatz, 10);
        var (r, ids) = Liste(db, "ART-1");

        await Close(c, r, ids, new int?[] { 3 }, new int?[] { WmsPlatz });   // Teil-Lieferung mit Fehlteil
        var m = Ausbuchungen(db).Should().ContainSingle().Subject;
        m.Quantity.Should().Be(3); m.StorageLocationId.Should().Be(WmsPlatz); m.WarehouseRequisitionItemId.Should().Be(ids[0]);
        m.ArticleId.Should().Be(1); m.Note.Should().Be($"Lagerbestellung #{r.Id} Pos 1 – WB-1");
        m.CreatedBy.Should().Be("stocker"); m.CreatedByWindows.Should().Be("DOMAIN\\stocker");
        c.TempData["SuccessMessage"]!.ToString().Should().Contain("Pos 1 3 Stk von A-01");

        ZurueckAufTeilgeliefert(db, r.Id);   // CloseAsync setzt bei Fehlteil PartiallyDelivered; sicherheitshalber explizit
        await Close(c, r, ids, new int?[] { 3 }, new int?[] { WmsPlatz });   // unveraendert → keine Buchung
        Ausbuchungen(db).Should().HaveCount(1);

        ZurueckAufTeilgeliefert(db, r.Id);
        await Close(c, r, ids, new int?[] { 7 }, new int?[] { ZweitPlatz }, new[] { 0 });   // Rest + Ueberlieferung, anderer Platz
        Ausbuchungen(db).Should().ContainSingle(x => x.Quantity == 4 && x.StorageLocationId == ZweitPlatz);
        Ausbuchungen(db).Sum(x => x.Quantity).Should().Be(7);
    }

    [Fact]
    public async Task IstUnterBereitsAusgebucht_WirdAbgewiesen_AK3()
    {
        var (c, db) = Build();
        Bestand(db, 1, WmsPlatz, 10);
        var (r, ids) = Liste(db, "ART-1");
        await Close(c, r, ids, new int?[] { 3 }, new int?[] { WmsPlatz });
        ZurueckAufTeilgeliefert(db, r.Id);

        var res = await Close(c, r, ids, new int?[] { 2 }, new int?[] { WmsPlatz }) as RedirectToActionResult;

        res!.ActionName.Should().Be("Details");
        c.TempData["WarningMessage"]!.ToString().Should().Contain("Pos 1: Ist 2 kleiner als bereits ausgebucht 3");
        Ausbuchungen(db).Should().HaveCount(1);
        db.WarehouseRequisitions.AsNoTracking().First(x => x.Id == r.Id).Status.Should().Be(WarehouseRequisitionStatus.PartiallyDelivered);
    }

    [Theory]
    [InlineData(WarehouseRequisitionStatus.Closed, "erledigt")]
    [InlineData(WarehouseRequisitionStatus.Cancelled, "storniert")]
    public async Task StatusGuard_NurOffeneListen_AK9(WarehouseRequisitionStatus status, string text)
    {
        var (c, db) = Build();
        Bestand(db, 1, WmsPlatz, 10);
        var (r, ids) = Liste(db, "ART-1");
        db.WarehouseRequisitions.First(x => x.Id == r.Id).Status = status; db.SaveChanges();

        var res = await Close(c, r, ids, new int?[] { 5 }, new int?[] { WmsPlatz }) as RedirectToActionResult;
        res!.ActionName.Should().Be("Details");
        c.TempData["WarningMessage"].Should().Be($"Liste #{r.Id} ist {text} – nichts gebucht.");

        var json = await c.PrintAndClose(r.Id, ids, new int?[] { 5 }, null, null, null, r.RowVersion, new int?[] { WmsPlatz });
        json.Should().BeOfType<BadRequestObjectResult>();
        Ausbuchungen(db).Should().BeEmpty();
    }

    [Fact]
    public async Task AllesOderNichts_SammelmeldungUndEingabenGesichert_AK5()
    {
        var (c, db) = Build();
        Bestand(db, 1, WmsPlatz, 10);
        Bestand(db, 2, WmsPlatz, 1);
        var (r, ids) = Liste(db, "ART-1", "ART-2", "ART-1X");
        db.Articles.Add(new Article { Id = 3, ArticleNumber = "ART-1X", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        db.SaveChanges();

        // Pos 1 ok, Pos 2 zu wenig Bestand, Pos 3 ohne Platz
        var res = await Close(c, r, ids, new int?[] { 5, 5, 2 }, new int?[] { WmsPlatz, WmsPlatz, null }, new[] { 0, 0, 1 }) as RedirectToActionResult;

        res!.ActionName.Should().Be("Details");
        var msg = c.TempData["WarningMessage"]!.ToString();
        msg.Should().StartWith("Nicht abgeschlossen – nichts gebucht:")
            .And.Contain("Pos 2: nicht genug Bestand auf A-01, verfügbar")
            .And.Contain("Pos 3: kein Entnahme-Platz gewählt").And.NotContain("Pos 1");
        Ausbuchungen(db).Should().BeEmpty();
        var liste = db.WarehouseRequisitions.AsNoTracking().Include(x => x.Items).First(x => x.Id == r.Id);
        liste.Status.Should().Be(WarehouseRequisitionStatus.Submitted);
        liste.Items.Single(i => i.Position == 1).QuantityPicked.Should().Be(5);   // Zwischenstand gesichert
    }

    [Fact]
    public async Task PlatzAusserhalbDerListe_UndSagePlatzOhneSageBuchung_WerdenAbgewiesen_AK4()
    {
        var (c, db) = Build();
        Bestand(db, 1, NichtBuchbar, 10);
        Bestand(db, 2, SagePlatzOhne, 10);
        var (r, ids) = Liste(db, "ART-1", "ART-2");

        await Close(c, r, ids, new int?[] { 5, 5 }, new int?[] { NichtBuchbar, SagePlatzOhne }, new[] { 0, 0 });

        var msg = c.TempData["WarningMessage"]!.ToString();
        msg.Should().Contain("Pos 1: Platz GESPERRT ist als Entnahme-Platz nicht wählbar")
            .And.Contain("Pos 2: HR 1 ist ein Sage-Platz ohne Sage-Buchung – der Lagerbestand-Abgleich würde die Entnahme zurückbuchen.");
        Ausbuchungen(db).Should().BeEmpty();
    }

    [Fact]
    public async Task SagePlatzMitSageBuchung_IstWaehlbar_NurWennServiceSchalterAn_AK4()
    {
        var (c, db) = Build(sageAn: true);
        Bestand(db, 1, SagePlatzMit, 10);
        var (r, ids) = Liste(db, "ART-1");

        var vm = ((await c.Details(r.Id) as ViewResult)!.Model as WarehouseRequisitionDetailViewModel)!;
        vm.Items[0].EntnahmeOptionen!.Select(p => p.Code).Should().Contain("HR 2").And.NotContain("HR 1").And.NotContain("GESPERRT");
        vm.Items[0].EntnahmeVorschlagId.Should().Be(SagePlatzMit);

        await Close(c, r, ids, new int?[] { 5 }, new int?[] { SagePlatzMit }, new[] { 0 });
        Ausbuchungen(db).Should().ContainSingle(m => m.StorageLocationId == SagePlatzMit && m.Quantity == 5);
    }

    [Theory]
    [InlineData(false, false, false)]   // Service-Schalter aus: Sage-Platz nie
    [InlineData(true, false, false)]    // Platz ohne Sage-Buchung
    [InlineData(true, true, true)]
    public void EntnahmeErlaubt_SagePlatzNurMitSageBuchung(bool serviceAn, bool platzFlag, bool erwartet)
    {
        var sage = new StorageLocation { Code = "HR", Source = StorageLocationSource.Sage, SageBuchungErlaubt = platzFlag, SageLagerkennung = "HR;1", SageLagerplatzId = 1 };
        WarehousePickingController.EntnahmeErlaubt(sage, serviceAn).Should().Be(erwartet);
        WarehousePickingController.EntnahmeErlaubt(new StorageLocation { Code = "W", Source = StorageLocationSource.Manual }, false).Should().BeTrue();
    }

    [Fact]
    public void EntnahmeOptionen_NurPlaetzeMitBestandPlusNan_SonstVolleListe()
    {
        var plaetze = new List<StorageLocation> { new() { Id = 1, Code = "A" }, new() { Id = 2, Code = "B" }, new() { Id = 3, Code = "C" }, new() { Id = 9, Code = "NAN" } };
        var bestand = new[] { new StockLocationInfo { StorageLocationId = 2, Quantity = 5 }, new StockLocationInfo { StorageLocationId = 3, Quantity = 0 },
                              new StockLocationInfo { StorageLocationId = 77, Quantity = 4 } };   // 77 = nicht waehlbar

        WarehousePickingController.EntnahmeOptionen(plaetze, bestand, nanId: 9).Select(p => p.Code).Should().Equal("B", "NAN");
        WarehousePickingController.EntnahmeOptionen(plaetze, Array.Empty<StockLocationInfo>(), nanId: 9).Select(p => p.Code).Should().Equal("NAN");
        WarehousePickingController.EntnahmeOptionen(plaetze, Array.Empty<StockLocationInfo>(), nanId: null).Should().HaveCount(4);   // Rueckfall
    }

    [Fact]
    public async Task DummyUndUnbekannterArtikel_OhneAuswahl_WerdenNichtGebucht_AK7()
    {
        var (c, db) = Build();
        Bestand(db, 1, WmsPlatz, 10);
        var (r, ids) = Liste(db, "ART-1", Article.DummyArticleNumber, "GIBTSNICHT");

        var vm = ((await c.Details(r.Id) as ViewResult)!.Model as WarehouseRequisitionDetailViewModel)!;
        vm.Items.Select(i => i.IstLagerartikel).Should().Equal(true, false, false);
        vm.Items[0].EntnahmeVorschlagId.Should().Be(WmsPlatz);
        vm.Items[0].EntnahmeHinweis.Should().Be("Vorschlag: Platz mit dem meisten Bestand");

        var res = await Close(c, r, ids, new int?[] { 5, 2, 1 }, new int?[] { WmsPlatz, null, null }, new[] { 0, 0, 0 }) as RedirectToActionResult;

        res!.ActionName.Should().Be("Index");
        Ausbuchungen(db).Should().ContainSingle(m => m.ArticleId == 1 && m.Quantity == 5);
        c.TempData["SuccessMessage"]!.ToString().Should().Contain("2 Position(en) nicht gebucht (kein Lagerartikel/DUMMY): Pos 2 (DUMMY), Pos 3 (GIBTSNICHT)");
    }

    [Fact]
    public async Task ZuWenigBestand_MitMinusBuchung_BuchtVomAusweichplatzMitWarnung_AK6()
    {
        var (c, db) = Build(negativ: true);
        Bestand(db, 1, WmsPlatz, 1);
        var (r, ids) = Liste(db, "ART-1");

        await Close(c, r, ids, new int?[] { 5 }, new int?[] { WmsPlatz }, new[] { 0 });

        Ausbuchungen(db).Should().ContainSingle(m => m.StorageLocationId == Nan && m.Quantity == 5);
        c.TempData["WarningMessage"]!.ToString().Should().Contain("buche vom Lagerplatz NAN ab");
        c.TempData["SuccessMessage"]!.ToString().Should().Contain("von NAN");
    }

    // QA v1.99.53: zwei Positionen desselben Artikels vom selben Platz - Bestand zaehlt gemeinsam (einzeln je 4 <= 6, zusammen 8 > 6).
    [Fact]
    public async Task GleicherArtikelInZweiPositionen_BestandZaehltGemeinsam()
    {
        var (c, db) = Build();
        Bestand(db, 1, WmsPlatz, 6);
        var (r, ids) = Liste(db, "ART-1", "ART-1");

        await Close(c, r, ids, new int?[] { 4, 4 }, new int?[] { WmsPlatz, WmsPlatz }, new[] { 0, 0 });

        Ausbuchungen(db).Should().BeEmpty("alles oder nichts");
        c.TempData["WarningMessage"]!.ToString().Should().Contain("Pos 2: nicht genug Bestand auf A-01, verfügbar 2", "geplante Menge der Pos 1 zaehlt ab");
    }

    [Fact]
    public async Task PlatzArrayFehltOderFalscheLaenge_NichtsGebucht()
    {
        var (c, db) = Build();
        Bestand(db, 1, WmsPlatz, 10);
        var (r, ids) = Liste(db, "ART-1");

        await Close(c, r, ids, new int?[] { 5 }, null, new[] { 0 });
        c.TempData["WarningMessage"]!.ToString().Should().Contain("Entnahme-Plätze fehlen oder passen nicht");

        var json = await c.PrintAndClose(r.Id, ids, new int?[] { 5 }, null, null, new[] { 0 }, r.RowVersion, new int?[] { WmsPlatz, WmsPlatz });
        json.Should().BeOfType<BadRequestObjectResult>();
        Ausbuchungen(db).Should().BeEmpty();
    }

    [Fact]
    public async Task PrintAndClose_BuchtUndLiefertDruckUrl()
    {
        var (c, db) = Build();
        Bestand(db, 1, WmsPlatz, 10);
        var (r, ids) = Liste(db, "ART-1");

        var res = await c.PrintAndClose(r.Id, ids, new int?[] { 5 }, null, null, new[] { 0 }, r.RowVersion, new int?[] { WmsPlatz });

        res.Should().BeOfType<OkObjectResult>();
        Ausbuchungen(db).Should().ContainSingle(m => m.Quantity == 5);
    }

    [Fact]
    public async Task SaveProgress_BuchtNie_AK18()
    {
        var (c, db) = Build();
        Bestand(db, 1, WmsPlatz, 10);
        var (r, ids) = Liste(db, "ART-1");

        await c.SaveProgress(r.Id, ids, new int?[] { 5 }, null, null, null);

        Ausbuchungen(db).Should().BeEmpty();
    }

    [Fact]
    public async Task Repository_SummiertAusbuchungenJePosition()
    {
        var db = TestDbContextFactory.Create();
        StockMovement M(MovementType t, decimal q, int? item) => new()
        {
            ArticleId = 1, StorageLocationId = 1, Quantity = q, MovementType = t, WarehouseRequisitionItemId = item,
            Timestamp = DateTime.Now, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        };
        db.StockMovements.AddRange(M(MovementType.Ausbuchung, 3, 10), M(MovementType.Ausbuchung, 2, 10), M(MovementType.Einbuchung, 9, 10),
            M(MovementType.Ausbuchung, 4, 11), M(MovementType.Ausbuchung, 7, null));
        db.SaveChanges();

        var sum = await new StockMovementRepository(db).GetAusgebuchtJeLagerbestellPositionAsync(new[] { 10, 11, 12 });

        sum.Should().BeEquivalentTo(new Dictionary<int, decimal> { [10] = 5, [11] = 4 });
    }

    /// <summary>Die gemeinsame Bestandsregel (auch Warenausgang/Outbound, Regression): abweisen ohne Minus-Schalter, sonst Ausweichplatz,
    /// fehlender Ausweichplatz = gewaehlter Platz (Altverhalten).</summary>
    [Theory]
    [InlineData(false, "NAN", true, WmsPlatz, null)]
    [InlineData(true, "NAN", false, Nan, "NAN")]
    [InlineData(true, "FEHLT", false, WmsPlatz, null)]
    public async Task AusbuchungBestandsregel_WieWarenausgang(bool negativ, string ausweich, bool abgewiesen, int platz, string? code)
    {
        var (_, db) = Build(negativ: negativ);
        db.AppSettings.Add(new AppSetting { Key = AppSettingKeys.NegativeBuchungLagerplatz, Value = ausweich });
        db.SaveChanges();
        Bestand(db, 1, WmsPlatz, 2);

        var e = await AusbuchungBestandsregel.PruefeAsync(new StockMovementRepository(db), new AppSettingRepository(db),
            new StorageLocationRepository(db), 1, WmsPlatz, 5);

        e.Abgewiesen.Should().Be(abgewiesen);
        e.StorageLocationId.Should().Be(platz);
        e.AusweichCode.Should().Be(code);
        e.Verfuegbar.Should().Be(2);
        (await AusbuchungBestandsregel.PruefeAsync(new StockMovementRepository(db), new AppSettingRepository(db),
            new StorageLocationRepository(db), 1, WmsPlatz, 2)).Should().Be(new AusbuchungBestandsregel.Ergebnis(false, WmsPlatz, 2, null));
    }
}
