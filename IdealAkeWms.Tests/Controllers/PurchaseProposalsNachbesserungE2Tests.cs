using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Tests.Controllers;

/// <summary>v1.99.30 Spec 2026-11-16-bestellvorschlaege-nachbesserung E2: letzte Bestellung ohne Artikel-Lieferant (F3, Antwort 4, AK 6),
/// Hinweise nach Ursache, Lieferant-Kaskade (F8, AK 18).</summary>
public class PurchaseProposalsNachbesserungE2Tests
{
    private static Article A(int id, string nr, ArticleSageDetail? sage = null) => new()
    {
        Id = id, ArticleNumber = nr, Description = nr, ReorderLevel = 85m, Unit = "Stk",
        SageDetail = sage ?? new ArticleSageDetail { Basismengeneinheit = "Stk", Lagermengeneinheit = "Stk" },
    };

    private static ArticleSupplierRow Lf(int id, string name, bool haupt = false, decimal? letzte = null, int tag = 1, bool ohne = false) =>
        new(id, name, "K" + id, true, null, null, null, 10 + id, haupt, 1m, letzte, letzte == null ? null : new DateTime(2026, 4, tag),
            letzte == null ? null : "B-" + tag, OhneArtikelLieferant: ohne);

    private static Dictionary<string, List<StockLocationInfo>> Bestand(params string[] nr) =>
        nr.ToDictionary(n => n, _ => new List<StockLocationInfo> { new() { Code = "L1", Quantity = 60m } });

    private static PurchaseProposalListViewModel Vm(IActionResult r) => (PurchaseProposalListViewModel)((ViewResult)r).Model!;

    [Fact]
    public async Task OhneZuordnungJuenger_GiltAlsL_HinweisNenntKtoUndName_AK6()
    {
        var (ctrl, _) = PurchaseProposalsControllerTests.Build(new() { A(1, "A1") }, Bestand("A1"),
            lieferanten: new() { [1] = Lf(1, "Huber", haupt: true, letzte: 400m, tag: 9) },
            letzte: new() { [1] = Lf(77, "Kreditor77", letzte: 30m, tag: 20, ohne: true) }, letzteGelesen: true);
        var r = Vm(await ctrl.Index()).Items.Single();

        r.Ergebnis.Vorschlag.Should().Be(30m);
        r.Hinweis.Should().Contain("Letzte Bestellung bei K77 Kreditor77 – kein Artikel-Lieferant in Sage");
        r.LieferantText.Should().Be("Huber (K1)", "Lieferant bleibt der Hauptlieferant");
    }

    [Fact]
    public async Task OhneZuordnung_OhneKreditor_NurKto_UndAlsLieferant_F8()
    {
        var ohneKreditor = new ArticleSupplierRow(0, "K55", "K55", false, null, null, null, null, false, null, 12m, new DateTime(2026, 3, 1), "B", true);
        var (ctrl, _) = PurchaseProposalsControllerTests.Build(new() { A(1, "A1") }, Bestand("A1"),
            letzte: new() { [1] = ohneKreditor }, letzteGelesen: true);
        var r = Vm(await ctrl.Index()).Items.Single();

        r.Hinweis.Should().Contain("Letzte Bestellung bei K55 – kein Artikel-Lieferant in Sage").And.Contain("kein Hauptlieferant – Lieferant der letzten Bestellung");
        (r.LieferantKto, r.LieferantText, r.Lieferant).Should().Be(("K55", "K55", (ArticleSupplierRow?)null));
    }

    [Fact]
    public async Task Kaskade_EinzigerArtikelLieferant_MehrereMitJuengster_MehrereOhne_Keiner_AK18()
    {
        var artikel = new List<Article> { A(1, "A1"), A(2, "A2"), A(3, "A3"), A(4, "A4") };
        var (ctrl, _) = PurchaseProposalsControllerTests.Build(artikel, Bestand("A1", "A2", "A3", "A4"),
            weitere: new()
            {
                [1] = new() { Lf(1, "Huber") },
                [2] = new() { Lf(1, "Huber"), Lf(2, "Maier", letzte: 20m, tag: 5) },
                [3] = new() { Lf(1, "Huber"), Lf(2, "Maier") },
            },
            letzte: new() { [2] = Lf(2, "Maier", letzte: 20m, tag: 5) }, letzteGelesen: true);
        var vm = Vm(await ctrl.Index());
        PurchaseProposalRow Z(string nr) => vm.Items.Single(i => i.Article.ArticleNumber == nr);

        (Z("A1").LieferantText, Z("A1").Wbz).Should().Be(("Huber (K1)", (int?)11));
        Z("A1").Hinweis.Should().Contain("kein Hauptlieferant – einziger Artikel-Lieferant");
        Z("A2").LieferantText.Should().Be("Maier (K2)");
        Z("A2").Hinweis.Should().Contain("kein Hauptlieferant – Lieferant der letzten Bestellung");
        Z("A2").Ergebnis.Vorschlag.Should().Be(26m, "v 60 + L 20 ≤ M 85: Mengenregel A (E6) M − v + 1, kein Vielfaches");
        Z("A3").LieferantText.Should().BeNull();
        Z("A3").Hinweis.Should().Contain(PurchaseProposalsController.HinweisMehrereLieferanten);
        Z("A4").Hinweis.Should().Contain(PurchaseProposalsController.HinweisKeinLieferant);
        vm.OhneArtikelLieferant.Should().Be(4, "Banner zaehlt Artikel ohne Hauptlieferant");
    }

    [Fact]
    public async Task Repository_ZweiteQuelle_JuengsteGewinnt_GleichstandZugeordnet_NameAusKreditor()
    {
        using var db = TestDbContextFactory.Create();
        Article Art(int id, string nr) => new() { Id = id, ArticleNumber = nr, ReorderLevel = 5m, CreatedBy = "t", CreatedByWindows = "t" };
        Creditor Kred(int id, string name, string kto) => new() { Id = id, Name = name, SageKto = kto, CreatedBy = "t", CreatedByWindows = "t" };
        ArticleSupplier Sup(int art, int kred, int tag) => new()
        {
            ArticleId = art, CreditorId = kred, Artikelnummer = "x", LetzteBestellmenge = 10m, LetzteBestellungAm = new DateTime(2026, 4, tag),
            CreatedBy = "t", CreatedByWindows = "t",
        };
        ArticleLastOrderUnassigned Ohne(int art, string kto, int tag) => new()
        {
            ArticleId = art, Kto = kto, Menge = 30m, Am = new DateTime(2026, 4, tag), CreatedBy = "t", CreatedByWindows = "t",
        };
        db.Articles.AddRange(Art(1, "A1"), Art(2, "A2"), Art(3, "A3"));
        db.Creditors.AddRange(Kred(1, "Huber", "K1"), Kred(2, "Maier", "K2"));
        db.ArticleSuppliers.AddRange(Sup(1, 1, 5), Sup(2, 1, 5));
        db.ArticleLastOrdersUnassigned.AddRange(Ohne(1, "K2", 9), Ohne(2, "K2", 5), Ohne(3, "K99", 1));
        await db.SaveChangesAsync();
        var repo = new PurchaseProposalRepository(db);

        var letzte = await repo.GetLetzteBestellungenAsync();
        (letzte[1].OhneArtikelLieferant, letzte[1].Name, letzte[1].SageKto, letzte[1].LetzteBestellmenge).Should().Be((true, "Maier", "K2", (decimal?)30m), "juenger");
        letzte[2].OhneArtikelLieferant.Should().BeFalse("gleiches Datum: zugeordnete gewinnt (Antwort 4)");
        (letzte[3].Name, letzte[3].CreditorId).Should().Be(("K99", 0), "ohne Kreditor die Kto");
        (await repo.HatLetzteBestellungenAsync()).Should().BeTrue();
        (await repo.GetLieferantenAsync())[1].Should().ContainSingle().Which.Name.Should().Be("Huber");
    }

    [Fact]
    public async Task Repository_NurOhneZuordnung_ZaehltAlsGelesen()
    {
        using var db = TestDbContextFactory.Create();
        db.Articles.Add(new Article { Id = 1, ArticleNumber = "A1", ReorderLevel = 5m, CreatedBy = "t", CreatedByWindows = "t" });
        db.ArticleLastOrdersUnassigned.Add(new ArticleLastOrderUnassigned { ArticleId = 1, Kto = "K1", Menge = 1m, Am = DateTime.Today, CreatedBy = "t", CreatedByWindows = "t" });
        await db.SaveChangesAsync();
        (await new PurchaseProposalRepository(db).HatLetzteBestellungenAsync()).Should().BeTrue();
    }
}
