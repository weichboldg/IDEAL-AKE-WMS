using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Tests.Controllers;

/// <summary>v1.99.52 Spec 2026-11-23-wareneingang-zu-bestellung-bedienung: F1 Bestellnummer ohne Jahr, F3 Hauptlagerplatz als Ziel-Vorschlag.</summary>
public partial class PurchaseOrdersReceiveTests
{
    private static SagePurchaseOrderLine Pos(int belPosId, int belId, string belegnummer, int? articleId, int? zielId = null, int? sagePlatzId = null) => new()
    {
        BelPosId = belPosId, BelId = belId, Belegkennzeichen = "EBB", Belegnummer = belegnummer, LieferantKto = "K1", LieferantName = "L",
        Artikelnummer = "X" + belPosId, ArticleId = articleId, MengeBestellt = 5m, MengeOffen = 5m, Mengeneinheit = "Stk", Lagermengeneinheit = "Stk",
        MengeBasiseinheit = "Stk", StorageLocationId = zielId, SagePlatzId = sagePlatzId, SyncedAt = DateTime.Now,
    };

    // ----- F1 -----

    [Fact]
    public async Task Repository_NurZiffern_Endung_SonstExakt_F1_AK1_AK3_AK5_AK6()
    {
        var db = TestDbContextFactory.Create();
        db.SagePurchaseOrderLines.AddRange(Pos(1, 500, "2027-167578", null), Pos(2, 500, "2027-167578", null), Pos(3, 501, "2026-167578", null),
            Pos(4, 502, "2027-168715", null), Pos(5, 503, "2027-1675", null));
        await db.SaveChangesAsync();
        var repo = new SagePurchaseOrderLineRepository(db);

        (await repo.GetBelIdsByBelegnummerAsync(" 168715 ")).Should().Equal((502, "2027-168715"));
        (await repo.GetBelIdsByBelegnummerAsync("167578")).Should().Equal((501, "2026-167578"), (500, "2027-167578"));
        (await repo.GetBelIdsByBelegnummerAsync("2027-167578")).Should().Equal((500, "2027-167578"));
        (await repo.GetBelIdsByBelegnummerAsync("1675")).Should().Equal(new[] { (503, "2027-1675") }, "nur die volle Endung hinter dem Bindestrich (AK 6)");
        (await repo.GetBelIdsByBelegnummerAsync("67578")).Should().BeEmpty();
        (await repo.GetBelIdsByBelegnummerAsync("27-167578")).Should().BeEmpty("mit Bindestrich exakt (AK 3)");
        (await repo.GetBelIdsByBelegnummerAsync("x167578")).Should().BeEmpty();
    }

    [Fact]
    public async Task Einstieg_OhneJahr_EinTreffer_Oeffnet_MehrereTreffer_MeldungMitListe_F1()
    {
        var (ctrl, db) = await BuildAsync();
        db.SagePurchaseOrderLines.AddRange(Pos(7001, 700, "2027-168715", 5), Pos(7101, 701, "2027-167578", 5), Pos(7201, 702, "2026-167578", 5));
        await db.SaveChangesAsync();

        ((RedirectToActionResult)await ctrl.Receive(null, "168715")).RouteValues!["belId"].Should().Be(700);

        var r = (RedirectToActionResult)await ctrl.Receive(null, "167578");
        r.RouteValues.Should().BeNull("keine Bestellung geoeffnet");
        ctrl.TempData["WarningMessage"].Should().Be("„167578“ passt zu 2 Bestellungen: 2026-167578, 2027-167578 – bitte mit Jahr eingeben.");

        await ctrl.Receive(null, "999999");
        ctrl.TempData["WarningMessage"]!.ToString().Should().StartWith("Bestellung „999999“ nicht gefunden");
    }

    // ----- F3 -----

    /// <summary>Plaetze aus BuildAsync: 9 Sage-Platz (buchbar, Sage-Buchung), 10 OHNE-SAGE (buchbar, kein Sage-Platz), 11 SAGE-AUS;
    /// dazu 12 INAKTIV. Artikel 5 → Hauptlagerplatz 10, Artikel 6 → 12 (nicht waehlbar), Artikel 7 ohne Hauptlagerplatz, Artikel 8 → 9.</summary>
    private static async Task F3DatenAsync(ApplicationDbContext db)
    {
        db.StorageLocations.Add(new StorageLocation { Id = 12, Code = "INAKTIV", IsActive = false, IstBuchbar = true, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        db.Articles.AddRange(
            new Article { Id = 7, ArticleNumber = "OHNE-HAUPT", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" },
            new Article { Id = 8, ArticleNumber = "HAUPT-SAGE", PrimaryStorageLocationId = 9, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        db.Articles.Find(5)!.PrimaryStorageLocationId = 10;
        db.Articles.Find(6)!.PrimaryStorageLocationId = 12;
        db.SagePurchaseOrderLines.AddRange(
            Pos(6001, 600, "2026-600", 5, zielId: 9, sagePlatzId: 77),   // AK 12: Sage-Platz waehlbar
            Pos(6002, 600, "2026-600", 5),                               // AK 13: ohne Sage-Platz
            Pos(6003, 600, "2026-600", 5, sagePlatzId: 55),              // AK 14: Sage-Platz nicht im WMS
            Pos(6004, 600, "2026-600", 5, zielId: 12, sagePlatzId: 56),  // AK 15: Sage-Platz zugeordnet, nicht waehlbar
            Pos(6005, 600, "2026-600", 6),                               // AK 16: Hauptlagerplatz nicht waehlbar
            Pos(6006, 600, "2026-600", 7),                               // AK 18: weder Sage- noch Hauptlagerplatz
            Pos(6007, 600, "2026-600", 8));                              // Hauptlagerplatz ist Sage-Platz
        await db.SaveChangesAsync();
    }

    private static PurchaseOrderReceiveRow Zeile(PurchaseOrderReceiveViewModel vm, int belPosId) => vm.Zeilen.Single(z => z.Row.Line.BelPosId == belPosId);

    [Fact]
    public async Task Vorschlag_SagePlatzVorrang_SonstHauptlagerplatz_Hinweise_F3_AK12_bis_AK18()
    {
        var (ctrl, db) = await BuildAsync();
        await F3DatenAsync(db);
        var vm = (PurchaseOrderReceiveViewModel)((ViewResult)await ctrl.Receive(600, (string?)null)).Model!;

        (Zeile(vm, 6001).Eingabe.StorageLocationId, Zeile(vm, 6001).ZielVorschlagHauptlagerplatz, Zeile(vm, 6001).ZielHinweis).Should().Be(((int?)9, false, (string?)null));
        (Zeile(vm, 6002).Eingabe.StorageLocationId, Zeile(vm, 6002).ZielVorschlagHauptlagerplatz, Zeile(vm, 6002).ZielHinweis).Should().Be(((int?)10, true, (string?)null));
        (Zeile(vm, 6003).Eingabe.StorageLocationId, Zeile(vm, 6003).ZielVorschlagHauptlagerplatz).Should().Be(((int?)10, true));
        Zeile(vm, 6003).Row.ZielNichtImWms.Should().BeTrue("Hinweis „Sage-Platz … nicht im WMS“ bleibt (AK 14)");
        (Zeile(vm, 6004).Eingabe.StorageLocationId, Zeile(vm, 6004).ZielVorschlagHauptlagerplatz).Should().Be(((int?)10, true));
        Zeile(vm, 6004).Row.Line.StorageLocationId.Should().Be(12, "Hinweis „Sage-Platz … nicht buchbar“ bleibt (AK 15)");
        (Zeile(vm, 6005).Eingabe.StorageLocationId, Zeile(vm, 6005).ZielHinweis).Should().Be(((int?)null, "Hauptlagerplatz INAKTIV ist im WMS nicht buchbar – bitte Ziel wählen"));
        (Zeile(vm, 6006).Eingabe.StorageLocationId, Zeile(vm, 6006).ZielHinweis).Should().Be(((int?)null, "Kein Sage-Platz an der Position – bitte Ziel wählen"));
        (Zeile(vm, 6007).Eingabe.StorageLocationId, Zeile(vm, 6007).ZielVorschlagHauptlagerplatz).Should().Be(((int?)9, true));
    }

    [Fact]
    public async Task Vorschlag_SageWe_NurSagePlatzMitSageBuchung_F3_AK17()
    {
        var (ctrl, db) = await BuildAsync(sageWe: true);
        await F3DatenAsync(db);
        var vm = (PurchaseOrderReceiveViewModel)((ViewResult)await ctrl.Receive(600, (string?)null)).Model!;

        vm.SageWeAktiv.Should().BeTrue();
        (Zeile(vm, 6002).Eingabe.StorageLocationId, Zeile(vm, 6002).ZielVorschlagHauptlagerplatz, Zeile(vm, 6002).ZielHinweis)
            .Should().Be(((int?)null, false, "Hauptlagerplatz OHNE-SAGE hat keine Sage-Buchung – bitte Sage-Platz wählen"));
        (Zeile(vm, 6007).Eingabe.StorageLocationId, Zeile(vm, 6007).ZielVorschlagHauptlagerplatz).Should().Be(((int?)9, true));
    }

    [Fact]
    public async Task Post_EingabeBleibt_KeinVorschlag_Buchen_AufVorgeschlagenenPlatz_F3_AK19_AK20()
    {
        var (ctrl, db) = await BuildAsync();
        await F3DatenAsync(db);

        var vm = (PurchaseOrderReceiveViewModel)((ViewResult)await Post(ctrl, 600, new List<PurchaseOrderReceiveInput>
            { E(6002, "abc", platz: 11), E(6003, null, platz: null) })).Model!;
        (Zeile(vm, 6002).Eingabe.StorageLocationId, Zeile(vm, 6002).ZielVorschlagHauptlagerplatz).Should().Be(((int?)11, false));

        ctrl.ModelState.Clear();   // gleiche Controller-Instanz wie der erste POST
        await Post(ctrl, 600, new List<PurchaseOrderReceiveInput> { E(6002, "1", platz: 10) });
        var m = db.StockMovements.Single();
        m.StorageLocationId.Should().Be(10);
        db.PurchaseOrderReceipts.Single().SagePlatzId.Should().BeNull();
    }
}
