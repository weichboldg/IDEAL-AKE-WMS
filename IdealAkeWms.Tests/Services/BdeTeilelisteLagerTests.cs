using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace IdealAkeWms.Tests.Services;

/// <summary>v1.99.72 (Spec 2026-11-20-bde-liste-isolierungsfraese-pfad-material F6–F11, ADR 0038): Lagerbestell-Zeilen in der BDE-Teileliste –
/// Auswahl nach Merkmal Arbeitsschritte, Termin, KW, Summiert, Melden mit Negativfaellen, Formular-Schluessel, CSV-Fix (F12).</summary>
public class BdeTeilelisteLagerTests
{
    private static readonly DateTime Freitag = new(2026, 11, 20, 9, 30, 0);

    private sealed record Lager(BdeTeilelisteMeldenTests.Umgebung U, int ItemKa, int ItemFremd, int ItemEntwurf);

    private static async Task<Lager> AufbauAsync(bool option = true)
    {
        var u = await BdeTeilelisteMeldenTests.AufbauAsync();
        var ctx = u.Ctx;
        var wp = (await ctx.ProductionWorkplaces.FindAsync(u.Ids.WorkplaceId))!;
        wp.BdeTeilelisteLagerbestellungen = option;
        ctx.Articles.AddRange(
            new Article { Id = 501, ArticleNumber = "L1", Matchcode = "Iso 1", CreatedBy = "t", CreatedByWindows = "t" },
            new Article { Id = 502, ArticleNumber = "L2", Matchcode = "Iso 2", CreatedBy = "t", CreatedByWindows = "t" });
        ctx.ArticleAttributeDefinitions.AddRange(
            new ArticleAttributeDefinition { Id = 601, Name = "Arbeitsschritte", AttributeType = AttributeType.Text, SyncSource = "Sage", SyncFieldName = BfArtikelFelder.Arbeitsschritte, CreatedBy = "t", CreatedByWindows = "t" },
            new ArticleAttributeDefinition { Id = 602, Name = "Blechdicke", AttributeType = AttributeType.Text, SyncSource = "Sage", SyncFieldName = BfArtikelFelder.Blechdicke, CreatedBy = "t", CreatedByWindows = "t" });
        ctx.ArticleAttributeValues.AddRange(
            new ArticleAttributeValue { ArticleId = 501, ArticleAttributeDefinitionId = 601, TextValue = "IF KA", CreatedBy = "t", CreatedByWindows = "t" },
            new ArticleAttributeValue { ArticleId = 501, ArticleAttributeDefinitionId = 602, TextValue = "2", CreatedBy = "t", CreatedByWindows = "t" },
            new ArticleAttributeValue { ArticleId = 502, ArticleAttributeDefinitionId = 601, TextValue = "LA", CreatedBy = "t", CreatedByWindows = "t" });
        ctx.WarehouseRequisitions.AddRange(
            new WarehouseRequisition { Id = 31, ProductionWorkplaceId = u.Ids.WorkplaceId, Status = WarehouseRequisitionStatus.Submitted, SubmittedAt = Freitag, Comment = "dringend", CreatedBy = "t", CreatedByWindows = "t" },
            new WarehouseRequisition { Id = 32, ProductionWorkplaceId = u.Ids.WorkplaceId, Status = WarehouseRequisitionStatus.Draft, CreatedBy = "t", CreatedByWindows = "t" });
        var ka = new WarehouseRequisitionItem { WarehouseRequisitionId = 31, Position = 1, ArticleNumber = "L1", ArticleDescription = "Isolierung", Unit = "Stk", QuantityRequested = 5m, CreatedBy = "t", CreatedByWindows = "t" };
        var fremd = new WarehouseRequisitionItem { WarehouseRequisitionId = 31, Position = 2, ArticleNumber = "L2", ArticleDescription = "x", Unit = "Stk", QuantityRequested = 1m, CreatedBy = "t", CreatedByWindows = "t" };
        var entwurf = new WarehouseRequisitionItem { WarehouseRequisitionId = 32, Position = 1, ArticleNumber = "L1", ArticleDescription = "x", Unit = "Stk", QuantityRequested = 1m, CreatedBy = "t", CreatedByWindows = "t" };
        ctx.WarehouseRequisitionItems.AddRange(ka, fremd, entwurf);
        await ctx.SaveChangesAsync();
        return new Lager(u, ka.Id, fremd.Id, entwurf.Id);
    }

    private static Task<BdeTeilelisteService.MeldeErgebnis> MeldeLager(Lager l, int itemId, decimal? gut = null) =>
        l.U.Svc.MeldenAsync(l.U.Ids.OperatorId, l.U.Ids.WorkplaceId, l.U.Ids.TerminalId,
            new[] { new BdeTeilelisteService.MeldeAuftrag(null, gut, null, itemId) });

    [Fact]
    public async Task Laden_NurAbgeschickteMitKuerzel_TerminUndSpalten()
    {
        var l = await AufbauAsync();

        var d = await l.U.Svc.LadenAsync("KA", null, false, null, mitLagerbestellungen: true);

        var z = d.Zeilen.Where(x => x.Lager != null).Should().ContainSingle().Subject;
        (z.Lager!.ItemId, z.Lager.RequisitionId, z.Lager.Kommentar).Should().Be((l.ItemKa, 31, "dringend"));
        z.Lager.Termin.Should().Be(new DateTime(2026, 12, 4), "Freitag + 10 Arbeitstage");
        (z.Node.HauptFA, z.Node.Matchcode, z.Node.Sollmenge).Should().Be((0, "Iso 1", 5m));
        z.FormSchluessel.Should().Be("LB|" + l.ItemKa);
        z.Meldbar.Should().BeTrue();
        var sp = BdeTeilelisteService.SpaltenZeile(new Dictionary<int, VormontageService.KopfWerte>(), null, d.Zusatz);
        sp["hauptfa"](z).Should().BeNull();
        sp["blechdicke"](z).Should().Be("2");
        sp["montage-abteilung"](z).Should().StartWith("Werkbank");
        sp["fe-termin"](z).Should().Be("04.12.2026");
        d.Zeilen.Should().Contain(x => x.Lager == null && x.Node.Artnr == "B1", "FA-Zeilen bleiben");
    }

    // v1.99.81 (F-11): Glasbestellungen (dieselbe Tabelle, Type Glas) erscheinen nicht und sind nicht meldbar
    [Fact]
    public async Task Glasbestellung_NichtInListe_NichtMeldbar()
    {
        var l = await AufbauAsync();
        l.U.Ctx.WarehouseRequisitions.Single(x => x.Id == 31).Type = WarehouseRequisitionType.Glas;
        await l.U.Ctx.SaveChangesAsync();

        (await l.U.Svc.LadenAsync("KA", null, false, null, mitLagerbestellungen: true)).Zeilen.Should().OnlyContain(x => x.Lager == null);
        var r = await MeldeLager(l, l.ItemKa);
        r.Gemeldet.Should().Be(0);
        r.Abgewiesen.Should().ContainSingle().Which.Should().Contain("keine Lagerbestellung");
    }

    [Fact]
    public async Task Laden_OhneOption_KeineLagerZeilen()
    {
        var l = await AufbauAsync();
        (await l.U.Svc.LadenAsync("KA", null, false, null)).Zeilen.Should().OnlyContain(x => x.Lager == null);
    }

    [Fact]
    public async Task Kw_FiltertUeberTermin()
    {
        var l = await AufbauAsync();
        var drin = await l.U.Svc.LadenAsync("KA", (new DateTime(2026, 11, 30), new DateTime(2026, 12, 7)), false, null, mitLagerbestellungen: true);
        var draussen = await l.U.Svc.LadenAsync("KA", (new DateTime(2026, 11, 23), new DateTime(2026, 11, 30)), false, null, mitLagerbestellungen: true);
        drin.Zeilen.Should().ContainSingle(x => x.Lager != null);
        draussen.Zeilen.Should().NotContain(x => x.Lager != null);
    }

    [Fact]
    public async Task Summiert_LagerZeileMitLbNummerUndFormSchluessel()
    {
        var l = await AufbauAsync();
        var d = await l.U.Svc.LadenAsync("KA", null, false, null, mitLagerbestellungen: true);

        var a = BdeTeilelisteService.Summieren(d.Zeilen, Array.Empty<FaHierarchyOrderInfo>(), null, d.Zusatz).Single(x => x.Matchcode == "Iso 1");

        a.HauptFas.Should().Be("LB 31");
        a.OffeneSchluessel.Should().Equal("LB|" + l.ItemKa);
        a.Blechdicken.Should().Be("2");
    }

    [Fact]
    public async Task Melden_Lager_SpeichertOhneFaUndOhneLagerbuchung_ZweitesMalBereitsGemeldet()
    {
        var l = await AufbauAsync();

        var r = await MeldeLager(l, l.ItemKa);

        r.Gemeldet.Should().Be(1);
        var m = l.U.Ctx.BdeTeilMeldungen.Single();
        (m.WarehouseRequisitionItemId, m.HauptFA, m.SubFA, m.VaterFA, m.Position).Should().Be((l.ItemKa, 0, 0, (int?)null, (int?)null));
        (m.Artnr, m.Matchcode, m.GoodQuantity, m.SollmengeBeiMeldung, m.Kuerzel).Should().Be(("L1", "Iso 1", 5m, 5m, "KA"));
        l.U.Ctx.StockMovements.Should().BeEmpty();
        l.U.Ctx.WarehouseRequisitions.Single(x => x.Id == 31).Status.Should().Be(WarehouseRequisitionStatus.Submitted);

        var zweit = await MeldeLager(l, l.ItemKa);
        zweit.Gemeldet.Should().Be(0);
        zweit.BereitsGemeldet.Should().ContainSingle();

        // FA-Zeilen sehen die Lagerbestell-Meldung nicht (ADR 0038)
        var d = await l.U.Svc.LadenAsync("KA", null, true, null, mitLagerbestellungen: true);
        d.Zeilen.Single(x => x.Lager == null && x.Node.Artnr == "B1").Meldungen.Should().BeEmpty();
        d.Zeilen.Single(x => x.Lager != null).Abschluss.Should().NotBeNull();
        d.Waisen.Should().Be(0);
    }

    [Fact]
    public async Task Melden_Lager_Teilfertig_DannFertigMitRest()
    {
        var l = await AufbauAsync();
        var teil = await l.U.Svc.MeldenAsync(l.U.Ids.OperatorId, l.U.Ids.WorkplaceId, l.U.Ids.TerminalId,
            new[] { new BdeTeilelisteService.MeldeAuftrag(null, 2, null, l.ItemKa, Abschliessend: false) });
        teil.AllesGut.Should().BeTrue(teil.Text());
        var z = (await l.U.Svc.LadenAsync("KA", null, true, null, mitLagerbestellungen: true)).Zeilen.Single(x => x.Lager != null);
        (z.Meldbar, z.Rest).Should().Be((true, 3m));

        (await MeldeLager(l, l.ItemKa)).AllesGut.Should().BeTrue();

        l.U.Ctx.BdeTeilMeldungen.Single(m => m.IstAbschliessend).GoodQuantity.Should().Be(3, "Vorgabe = Rest (5 - 2)");
        (await MeldeLager(l, l.ItemKa)).BereitsGemeldet.Should().ContainSingle();
    }

    [Fact]
    public async Task Melden_Lager_Negativfaelle_Abgewiesen()
    {
        var l = await AufbauAsync();

        (await MeldeLager(l, l.ItemFremd)).Abgewiesen.Should().ContainSingle(t => t.Contains("Kürzel KA nicht"));
        (await MeldeLager(l, l.ItemEntwurf)).Abgewiesen.Should().ContainSingle(t => t.Contains("nicht mehr abgeschickt"));
        (await MeldeLager(l, 99999)).Abgewiesen.Should().ContainSingle(t => t.Contains("nicht mehr vorhanden"));
        (await MeldeLager(l, l.ItemKa, gut: -1)).Abgewiesen.Should().ContainSingle(t => t.Contains("negativ"));
        l.U.Ctx.BdeTeilMeldungen.Should().BeEmpty();
    }

    [Fact]
    public async Task Melden_Lager_OhneWerkbankOption_Abgewiesen()
    {
        var l = await AufbauAsync(option: false);
        (await MeldeLager(l, l.ItemKa)).Abgewiesen.Should().ContainSingle(t => t.Contains("nicht aktiv"));
        l.U.Ctx.BdeTeilMeldungen.Should().BeEmpty();
    }

    [Theory]
    [InlineData("LB|5", 5)]
    [InlineData("LB|0", null)]
    [InlineData("LB|x", null)]
    [InlineData("LB|", null)]
    [InlineData("100|200|0|1|B1", null)]
    [InlineData(null, null)]
    public void ParseLagerSchluessel(string? text, int? erwartet) =>
        BdeTeilelisteService.ParseLagerSchluessel(text).Should().Be(erwartet);

    [Theory]
    [InlineData(null, true)]
    [InlineData("navigate", true)]
    [InlineData("cors", false)]
    [InlineData("same-origin", false)]
    public void CsvFetch_IstKeineNavigation(string? secFetchMode, bool erwartet)
    {
        var ctx = new DefaultHttpContext();
        if (secFetchMode != null) ctx.Request.Headers["Sec-Fetch-Mode"] = secFetchMode;
        BdeTeilelisteController.IstNavigation(ctx.Request).Should().Be(erwartet);
    }

    [Fact]
    public void Teilmeldungen_LagerZeile_HauptFaStrichUndBestellnummer()
    {
        var m = new BdeTeilMeldung
        {
            HauptFA = 0, WarehouseRequisitionItemId = 7,
            WarehouseRequisitionItem = new WarehouseRequisitionItem { Id = 7, WarehouseRequisitionId = 31, Position = 2 },
        };
        BdeBookingsController.TeilmeldungSpalten["hauptfa"](m).Should().Be("–");
        BdeBookingsController.TeilmeldungSpalten["lagerbestellung"](m).Should().Be("31 / Pos. 2");
        BdeBookingsController.TeilmeldungSpalten["lagerbestellung"](new BdeTeilMeldung { HauptFA = 100 }).Should().BeNull();
    }
}
