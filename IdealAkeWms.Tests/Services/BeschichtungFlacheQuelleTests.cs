using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Services.Beschichtung;
using IdealAkeWms.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Services;

/// <summary>
/// v1.99.34 (Spec 2026-11-16-beschichtungslogik-je-standort E5 + Nachtrag 3): flache Beschichtungsquelle der AKE —
/// offene WA mit Kennzeichen, Lackierteile aus dem Stuecklisten-Cache, Positionsschluessel Artnr + PositionText,
/// natuerliche Sortierung, Menge ueber BomQuantityResolver, Sammelmeldung, Anlage aus der flachen Quelle.
/// Was InMemory nicht beweist: SQL-Kollation der Kategorie/Artikel-Joins und den echten Cache-Inhalt (Manual-UAT TS-145).
/// </summary>
public class BeschichtungFlacheQuelleTests
{
    private const string Kategorie = "Lackierteil";
    private readonly ApplicationDbContext _db = TestDbContextFactory.Create(Guid.NewGuid().ToString());

    private ProductionOrder Wa(string nr, string artikel, decimal menge = 4m, bool kennzeichen = true, bool done = false,
        bool cancelled = false, string? sub = null)
    {
        var o = new ProductionOrder
        {
            OrderNumber = nr, SubOrderNumber = sub ?? nr, ArticleNumber = artikel, Quantity = menge, IsDone = done, IsCancelled = cancelled,
            ProductionDate = new DateTime(2026, 12, 10),
        };
        o.PickingStatus = new ProductionOrderPickingStatus { ProductionOrder = o, HasCoatingParts = kennzeichen };
        _db.ProductionOrders.Add(o);
        return o;
    }

    private void Stueckliste(string artikel, params (string? Pos, string Ressource, decimal Menge, string? Baugruppe, string Kat)[] zeilen)
    {
        var h = new CachedBomHeader { Artikelnummer = artikel, Source = "Cache", ContentHash = "x", CachedAt = DateTime.Now };
        var sort = 0;
        foreach (var z in zeilen)
        {
            h.Items.Add(new CachedBomItem { Position = z.Pos, Ressourcenummer = z.Ressource, Menge = z.Menge, Baugruppe = z.Baugruppe, SortOrder = sort++ });
            if (!_db.Articles.Any(a => a.ArticleNumber == z.Ressource) && !_db.Articles.Local.Any(a => a.ArticleNumber == z.Ressource))
            {
                var cat = _db.ArticleCategories.Local.FirstOrDefault(c => c.Name == z.Kat) ?? _db.ArticleCategories.Add(new ArticleCategory { Name = z.Kat }).Entity;
                _db.Articles.Add(new Article { ArticleNumber = z.Ressource, ArticleCategory = cat });
            }
        }
        _db.CachedBomHeaders.Add(h);
    }

    private void Kommissioniert(ProductionOrder o) => o.PickingStatus!.IsDonePicking = true;

    private void SageMasse(string artikel, decimal? breite, decimal? hoehe, decimal? laenge)
    {
        var a = _db.Articles.Local.Single(x => x.ArticleNumber == artikel);
        _db.ArticleSageDetails.Add(new ArticleSageDetail { Article = a, Breite = breite, Hoehe = hoehe, Laenge = laenge });
    }

    private BeschichtungsauftragWorkflow Workflow(BeschichtungFlacheQuelle echt)
    {
        var flach = new Mock<BeschichtungFlacheQuelle>(null!, null!, null!, null!, null!, null!, null!) { CallBase = false };
        flach.Setup(q => q.IstFlachAsync()).ReturnsAsync(true);
        flach.Setup(q => q.LadenAsync(It.IsAny<IReadOnlyCollection<int>?>(), It.IsAny<bool>()))
            .Returns<IReadOnlyCollection<int>?, bool>((h, m) => echt.LadenAsync(h, m));
        flach.Setup(q => q.StartBeschichtungAsync(It.IsAny<ProductionOrder>())).ReturnsAsync((DateTime?)null);
        var user = new Mock<ICurrentUserService>();
        user.Setup(u => u.GetDisplayName()).Returns("Tester");
        user.Setup(u => u.GetWindowsUserName()).Returns(@"DOM	ester");
        return new BeschichtungsauftragWorkflow(new BeschichtungsauftragRepository(_db), new FaHierarchyNodeRepository(_db),
            new FaHierarchyOrderInfoRepository(_db), flach.Object, user.Object, TimeProvider.System,
            NullLogger<BeschichtungsauftragWorkflow>.Instance);
    }

    private BeschichtungFlacheQuelle Quelle(string? master = "false")
    {
        var settings = new Mock<IAppSettingRepository>();
        settings.Setup(s => s.GetValueAsync(AppSettingKeys.LackierteilKategorieName)).ReturnsAsync(Kategorie);
        var service = new Mock<IServiceSettingRepository>();
        service.Setup(s => s.GetValueAsync(IdealAkeWms.Services.HierarchischeStruktur.HierarchischeStrukturKeys.Master)).ReturnsAsync(master);
        var articles = new Mock<IArticleRepository>();
        articles.Setup(a => a.GetMatchcodesByArticleNumbersAsync(It.IsAny<IReadOnlyCollection<string>>()))
            .ReturnsAsync(new Dictionary<string, string?> { ["L-1"] = "MC-L1" });
        return new BeschichtungFlacheQuelle(new BeschichtungsauftragRepository(_db), articles.Object, settings.Object, service.Object,
            new Mock<IHolidayRepository>().Object, new Mock<IBusinessDayService>().Object, NullLogger<BeschichtungFlacheQuelle>.Instance);
    }

    [Fact]
    public async Task IstFlach_NurOhneHierarchischesAuftragsmodell()
    {
        (await Quelle("false").IstFlachAsync()).Should().BeTrue();
        (await Quelle(null).IstFlachAsync()).Should().BeTrue();
        (await Quelle("true").IstFlachAsync()).Should().BeFalse("IDEAL: FA-Struktur bleibt Quelle");
    }

    [Fact]
    public async Task Laden_NurOffeneWaMitKennzeichen_NurLackierteile_MengeMalWaMenge_PositionGetrimmt()
    {
        Wa("1001", "S1402262", menge: 4m);
        Wa("1002", "S1402262", done: true);
        Wa("1003", "S1402262", cancelled: true);
        Wa("1004", "S1402262", kennzeichen: false);
        Wa("1001", "S1402262", sub: "1001-2"); // Sub-FA: nie eine eigene Gruppe
        Stueckliste("S1402262",
            (" 10.140.40.10.10 ", "L-1", 2.5m, "BG-7", Kategorie),
            (null, "L-2", 1m, null, Kategorie),
            ("20", "K-1", 3m, null, "Kaufteil"));
        await _db.SaveChangesAsync();

        var f = await Quelle().LadenAsync();

        f.Knoten.Should().HaveCount(2).And.OnlyContain(n => n.HauptFA == 1001 && n.Beschichtet && n.SubFA == 0 && n.VaterFA == null && n.Position == null);
        var l1 = f.Knoten.Single(n => n.Artnr == "L-1");
        l1.PositionText.Should().Be("10.140.40.10.10");
        l1.Sollmenge.Should().Be(10m, "2,5 je Stueck × WA-Menge 4 — auch an einer Baugruppen-Zeile");
        l1.Matchcode.Should().Be("MC-L1");
        f.Knoten.Single(n => n.Artnr == "L-2").PositionText.Should().Be("", "fehlende Position = leerer Text, nie null (flacher Schluessel)");
        f.OhneLackierteile.Should().BeEmpty();
        f.Auftraege.Should().ContainSingle().Which.OrderNumber.Should().Be("1001");
    }

    [Fact]
    public async Task Laden_WaOhneLackierteil_KommtInDieSammelmeldung_NichtInDieListe()
    {
        Wa("2001", "A-OHNE");
        Wa("2002", "A-MIT");
        Stueckliste("A-OHNE", ("10", "K-9", 1m, null, "Kaufteil"));
        Stueckliste("A-MIT", ("10", "L-1", 1m, null, Kategorie));
        await _db.SaveChangesAsync();

        var f = await Quelle().LadenAsync();

        f.OhneLackierteile.Should().Equal("2001");
        f.Knoten.Should().OnlyContain(n => n.HauptFA == 2002);
        BeschichtungFlacheQuelle.Sammelmeldung(f.OhneLackierteile).Should()
            .Be("1 WA mit Beschichtungskennzeichen ohne Lackierteile im Stücklisten-Cache – nicht in der Vorschlagsliste: 2001");
    }

    /// <summary>Nachtrag 4 Punkt 2: Masse aus dem Sage-Artikel in cm × 10 = mm (Laenge = Tiefe); ohne Sage-Zeile 0.</summary>
    [Fact]
    public async Task Laden_MasseAusSageArtikel_CmMalZehn()
    {
        Wa("5001", "W-1");
        Stueckliste("W-1", ("10", "L-1", 1m, null, Kategorie), ("20", "L-2", 1m, null, Kategorie));
        SageMasse("L-1", breite: 69.32m, hoehe: 83.82m, laenge: 2.41m); // Stichprobe 87070769
        await _db.SaveChangesAsync();

        var f = await Quelle().LadenAsync();

        var l1 = f.Knoten.Single(n => n.Artnr == "L-1");
        (l1.Breite, l1.Hoehe, l1.Tiefe).Should().Be((693.2m, 838.2m, 24.1m));
        var l2 = f.Knoten.Single(n => n.Artnr == "L-2");
        (l2.Breite, l2.Hoehe, l2.Tiefe).Should().Be((0m, 0m, 0m), "keine ArticleSageDetails-Zeile");
    }

    /// <summary>Nachtrag 4 Punkte 4 und 5: kommissionierte WA fehlen in der Gesamtliste (Haekchen holt sie), in der Anlage
    /// (hauptFas) zaehlen sie; die Sammelmeldung nennt nur nicht kommissionierte WA mit Nummer.</summary>
    [Fact]
    public async Task Laden_KommissionierteAusgeblendet_HaekchenUndAnlageZeigen_SammelmeldungNurAnzahl()
    {
        Wa("6001", "W-1");
        Kommissioniert(Wa("6002", "W-1"));
        Wa("6003", "A-OHNE");
        Kommissioniert(Wa("6004", "A-OHNE"));
        Kommissioniert(Wa("6005", "A-OHNE"));
        Stueckliste("W-1", ("10", "L-1", 1m, null, Kategorie));
        Stueckliste("A-OHNE", ("10", "K-9", 1m, null, "Kaufteil"));
        await _db.SaveChangesAsync();

        var standard = await Quelle().LadenAsync();
        standard.Knoten.Select(n => n.HauptFA).Should().Equal(6001);
        standard.Auftraege.Select(o => o.OrderNumber).Should().Equal("6001");
        standard.OhneLackierteile.Should().Equal("6003");
        standard.OhneLackierteileKommissioniert.Should().Be(2);
        BeschichtungFlacheQuelle.Sammelmeldung(standard.OhneLackierteile, standard.OhneLackierteileKommissioniert).Should().Be(
            "1 WA mit Beschichtungskennzeichen ohne Lackierteile im Stücklisten-Cache – nicht in der Vorschlagsliste: 6003. " +
            "Dazu 2 bereits kommissionierte WA ohne Lackierteile (nicht einzeln genannt).");

        (await Quelle().LadenAsync(null, mitKommissionierten: true)).Knoten.Select(n => n.HauptFA).Should().BeEquivalentTo(new[] { 6001, 6002 });
        (await Quelle().LadenAsync(new[] { 6002 })).Knoten.Should().ContainSingle("Anlage/Hinzufuegen: der WA zaehlt auch kommissioniert");
        BeschichtungFlacheQuelle.Sammelmeldung(Array.Empty<string>(), 3).Should()
            .Be("3 bereits kommissionierte WA ohne Lackierteile (nicht einzeln genannt).");
    }

    /// <summary>Nachtrag 4 Punkt 1: Kopfzeile aus dem WA (Id 0, Kunde, Fertigungstermin, berechneter Beschichtungstermin).</summary>
    [Fact]
    public async Task KopfInfos_AusDemWa()
    {
        var o = Wa("7001", "W-1");
        o.Customer = "Kunde A";
        var settings = new Mock<IAppSettingRepository>();
        settings.Setup(s => s.GetIntValueAsync(It.IsAny<string>(), It.IsAny<int>())).Returns<string, int>((_, d) => Task.FromResult(d));
        settings.Setup(s => s.GetValueAsync(AppSettingKeys.BeschichtungAbholtage)).ReturnsAsync("Dienstag,Donnerstag");
        settings.Setup(s => s.GetValueAsync(AppSettingKeys.LackierteilKategorieName)).ReturnsAsync(Kategorie);
        var holidays = new Mock<IHolidayRepository>();
        holidays.Setup(h => h.GetHolidayDatesAsync()).ReturnsAsync(new HashSet<DateTime>());
        var q = new BeschichtungFlacheQuelle(new BeschichtungsauftragRepository(_db), new Mock<IArticleRepository>().Object, settings.Object,
            new Mock<IServiceSettingRepository>().Object, holidays.Object, new BusinessDayService(), NullLogger<BeschichtungFlacheQuelle>.Instance);

        var info = (await q.KopfInfosAsync(new[] { o })).Should().ContainSingle().Subject;

        info.Id.Should().Be(0);
        info.HauptFA.Should().Be(7001);
        info.Kunde.Should().Be("Kunde A");
        info.FE_Termin.Should().Be(new DateTime(2026, 12, 10));
        info.Start_Beschichtung.Should().Be(await q.StartBeschichtungAsync(o)).And.NotBeNull().And.BeBefore(new DateTime(2026, 12, 10));
    }

    /// <summary>QA-F2 AK 20: nur <see cref="BomQuantityResolver"/> multipliziert Stuecklistenmenge × WA-Menge (Quelltext-Waechter).</summary>
    [Fact]
    public void Waechter_MengeNurUeberBomQuantityResolver()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "SQL", "00_FreshInstall.sql"))) dir = dir.Parent;
        var quelle = File.ReadAllText(Path.Combine(dir!.FullName, "IdealAkeWms", "Services", "Beschichtung", "BeschichtungFlacheQuelle.cs"));
        Regex.IsMatch(quelle, @"\.Quantity\b").Should().BeFalse("die WA-Menge darf nur BomQuantityResolver.Resolve verrechnen");
        quelle.Should().Contain("BomQuantityResolver.Resolve(");
    }

    /// <summary>QA-F2 AK 22: zwei Cache-Staende, nur SortOrder verschoben — die Positionsschluessel bleiben dieselben.</summary>
    [Fact]
    public async Task ZweiCacheStaende_NurSortOrderVerschoben_SchluesselGleich()
    {
        Wa("8001", "W-1");
        Stueckliste("W-1", ("5.2", "L-1", 1m, null, Kategorie), ("5.10", "L-1", 1m, null, Kategorie), ("6", "L-2", 1m, null, Kategorie));
        await _db.SaveChangesAsync();
        var vorher = (await Quelle().LadenAsync()).Knoten.Select(PositionsSchluessel.Von).ToHashSet();

        var items = _db.CachedBomItems.ToList();
        foreach (var i in items) i.SortOrder = 100 - i.SortOrder;
        await _db.SaveChangesAsync();
        var nachher = (await Quelle().LadenAsync()).Knoten.Select(PositionsSchluessel.Von).ToHashSet();

        nachher.Should().BeEquivalentTo(vorher).And.HaveCount(3);
    }

    /// <summary>QA-F1 + F2 AK 25: „Positionen hinzufuegen“ natuerlich sortiert; Snapshot (Hinzufuegen) und Kopie (Reklamation)
    /// uebernehmen PositionText.</summary>
    [Fact]
    public async Task Hinzufuegen_NatuerlichSortiert_SnapshotUndKopieBehaltenPositionText()
    {
        Wa("9001", "W-1");
        Stueckliste("W-1", ("1", "L-1", 1m, null, Kategorie));
        await _db.SaveChangesAsync();
        var wf = Workflow(Quelle());
        var angelegt = await wf.AnlegenAsync(9001);
        angelegt.Fehler.Should().BeEmpty();
        var a = angelegt.Auftrag!;

        // neue Stuecklistenzeilen nach der Anlage: sind frei und kommen in die Auswahl
        var h = _db.CachedBomHeaders.Single();
        h.Items.Add(new CachedBomItem { Position = "51.10", Ressourcenummer = "L-1", Menge = 1m, SortOrder = 1 });
        h.Items.Add(new CachedBomItem { Position = "51.9", Ressourcenummer = "L-1", Menge = 1m, SortOrder = 2 });
        h.Items.Add(new CachedBomItem { Position = "5", Ressourcenummer = "L-1", Menge = 1m, SortOrder = 3 });
        await _db.SaveChangesAsync();

        var kandidaten = await wf.GetHinzufuegbarePositionenAsync(a.Id);
        kandidaten.Select(k => k.Schluessel.PositionText).Should().Equal("5", "51.9", "51.10");

        var r = await wf.PositionenHinzufuegenAsync(a.Id, kandidaten.Select(k => k.Schluessel).ToList(), a.RowVersion);
        r.Fehler.Should().BeEmpty();
        r.Auftrag!.Positionen.Select(p => p.PositionText).Should().BeEquivalentTo("1", "5", "51.9", "51.10");

        var gespeichert = await _db.Beschichtungsauftraege.Include(x => x.Positionen).SingleAsync(x => x.Id == a.Id);
        gespeichert.Status = BeschichtungsauftragStatus.Erledigt;
        await _db.SaveChangesAsync();
        var pos = gespeichert.Positionen.Where(p => p.PositionText is "51.9" or "51.10").Select(p => p.Id).ToList();
        var rekl = await wf.ReklamationAnlegenAsync(a.Id, pos);
        rekl.Fehler.Should().BeEmpty();
        rekl.Auftrag!.Positionen.Select(p => p.PositionText).Should().BeEquivalentTo("51.9", "51.10");
    }

    /// <summary>QA-F2 AK 26: Summiert nimmt Masse und Eigenschaften der natuerlich ersten Position (5.2 vor 5.10).</summary>
    [Fact]
    public void Summiert_NimmtNatuerlichErstePosition()
    {
        var knoten = new List<FaHierarchyNode>
        {
            new() { HauptFA = 1, Artnr = "L-1", PositionText = "5.10", Beschichtet = true, Sollmenge = 1m, Breite = 10m },
            new() { HauptFA = 1, Artnr = "L-1", PositionText = "5.2", Beschichtet = true, Sollmenge = 2m, Breite = 20m },
        };

        var row = BeschichtungsauftragService.AggregateSummiert(knoten, null, null, 1, 10, paginate: false).Rows.Single();

        row.Breite.Should().Be(20m);
        row.SummeSollmenge.Should().Be(3m);
        row.MasseUneinheitlich.Should().BeTrue();
    }

    [Fact]
    public void Sammelmeldung_LeerIstNull_UndHoechstens20Nummern()
    {
        BeschichtungFlacheQuelle.Sammelmeldung(Array.Empty<string>()).Should().BeNull("IDEAL bzw. alles gefunden: kein Hinweis");
        var text = BeschichtungFlacheQuelle.Sammelmeldung(Enumerable.Range(1, 21).Select(i => i.ToString()).ToList())!;
        text.Should().StartWith("21 WA").And.EndWith("20, …").And.NotContain(" 21,");
    }

    [Fact]
    public void Schluessel_ArtnrUndPositionText_Normalisiert_520IstNicht52()
    {
        var a = PositionsSchluessel.Bilden(null, 0, null, " l-1 ", " 5.2 ");
        a.Should().Be(PositionsSchluessel.Bilden(null, 0, null, "L-1", "5.2"));
        a.Should().NotBe(PositionsSchluessel.Bilden(null, 0, null, "L-1", "5.20"));
        a.Should().NotBe(PositionsSchluessel.Bilden(null, 0, null, "L-2", "5.2"));
        PositionsSchluessel.Von(new BeschichtungsauftragPosition { SubFA = 0, Artnr = "L-1", PositionText = "5.2" })
            .Should().Be(PositionsSchluessel.Von(new FaHierarchyNode { SubFA = 0, Artnr = "l-1", PositionText = "5.2 " }));
        PositionsSchluessel.Von(new BeschichtungsauftragPosition { SubFA = 0, Artnr = "L-1", PositionText = null })
            .Should().NotBe(a, "Altbestand ohne Text gilt an der AKE nicht als vergeben (Pruefung/BeschichtungPositionOhneTextFlach)");
        PositionsSchluessel.Bilden(10, 0, 3, "X", null).Should().Be(new PositionsSchluessel(10, 0, 3), "IDEAL-Schluessel unveraendert dreiteilig");
    }

    [Fact]
    public void Sortierung_Natuerlich_BisFuenfEbenen()
    {
        var liste = new[] { "51.5.10", "5", "51", "5.A", "51.5.2", "5.10", "5.2", "51.5", "", "10.140.40.10.10", "10.140.40.10.9" };
        liste.OrderBy(x => x, new NaturalPositionComparer()).Should()
            .Equal("", "5", "5.2", "5.10", "5.A", "10.140.40.10.9", "10.140.40.10.10", "51", "51.5", "51.5.2", "51.5.10");
    }

    [Theory]
    [InlineData("10.140.40.10.10")]
    [InlineData("5|2 x.y")]
    [InlineData("")]
    public void SchluesselText_FuenfteiligerRundlauf(string positionText)
    {
        var k = PositionsSchluessel.Bilden(null, 0, null, "L|1", positionText);
        var text = BeschichtungsauftraegeController.SchluesselText(k);
        text.Split('|').Should().HaveCount(5);
        BeschichtungsauftraegeController.TrySchluessel(text, out var back).Should().BeTrue();
        back.Should().Be(k);
    }

    [Theory]
    [InlineData("|0||A")]
    [InlineData("|0||A|B|C")]
    public void SchluesselText_VierOderSechsTeile_Ungueltig(string text) =>
        BeschichtungsauftraegeController.TrySchluessel(text, out _).Should().BeFalse();

    [Fact]
    public async Task Anlegen_Flach_PositionenMitText_StartBerechnet_KopfSonstLeer_ZweiteAnlageOhneFreiePositionen()
    {
        var wa = Wa("3001", "S1402262", menge: 2m);
        Stueckliste("S1402262", ("10.140.40.10.10", "L-1", 1m, null, Kategorie), ("5.2", "L-1", 1m, null, Kategorie), ("5.20", "L-1", 1m, null, Kategorie));
        await _db.SaveChangesAsync();
        var echt = Quelle();
        var flach = new Mock<BeschichtungFlacheQuelle>(null!, null!, null!, null!, null!, null!, null!) { CallBase = false };
        flach.Setup(q => q.IstFlachAsync()).ReturnsAsync(true);
        flach.Setup(q => q.LadenAsync(It.IsAny<IReadOnlyCollection<int>?>(), It.IsAny<bool>()))
            .Returns<IReadOnlyCollection<int>?, bool>((h, m) => echt.LadenAsync(h, m));
        flach.Setup(q => q.StartBeschichtungAsync(It.IsAny<ProductionOrder>())).ReturnsAsync(new DateTime(2026, 12, 1));
        var user = new Mock<ICurrentUserService>();
        user.Setup(u => u.GetDisplayName()).Returns("Tester");
        user.Setup(u => u.GetWindowsUserName()).Returns(@"DOM	ester");
        // FA-Struktur ist leer: findet die Anlage Positionen, kommen sie aus der flachen Quelle.
        var wf = new BeschichtungsauftragWorkflow(new BeschichtungsauftragRepository(_db), new FaHierarchyNodeRepository(_db),
            new FaHierarchyOrderInfoRepository(_db), flach.Object, user.Object, TimeProvider.System,
            NullLogger<BeschichtungsauftragWorkflow>.Instance);

        var r = await wf.AnlegenAsync(3001);

        r.Fehler.Should().BeEmpty();
        r.Auftrag!.Positionen.Select(p => p.PositionText).Should().BeEquivalentTo("10.140.40.10.10", "5.2", "5.20");
        r.Auftrag.Positionen.Should().OnlyContain(p => p.Menge == 2m && p.Position == null && p.VaterFA == null);
        r.Auftrag.StartBeschichtung.Should().Be(new DateTime(2026, 12, 1));
        r.Auftrag.Dienstleister.Should().BeNull();
        r.Auftrag.Ral.Should().BeNull();
        r.Auftrag.BeschichtenRetour.Should().BeNull();

        var zweite = await wf.AnlegenAsync(3001);
        zweite.Erfolg.Should().BeFalse("alle Positionen sind schon vergeben");
        _ = wa;
    }

    [Fact]
    public async Task Anlegen_Flach_WaOhneLackierteile_EigenerFehlertext()
    {
        var flach = new Mock<BeschichtungFlacheQuelle>(null!, null!, null!, null!, null!, null!, null!);
        flach.Setup(q => q.IstFlachAsync()).ReturnsAsync(true);
        flach.Setup(q => q.LadenAsync(It.IsAny<IReadOnlyCollection<int>?>(), It.IsAny<bool>()))
            .ReturnsAsync(new FlachePositionen(new List<FaHierarchyNode>(), new List<string>(), new List<ProductionOrder>()));
        var wf = new BeschichtungsauftragWorkflow(new BeschichtungsauftragRepository(_db), new FaHierarchyNodeRepository(_db),
            new FaHierarchyOrderInfoRepository(_db), flach.Object, new Mock<ICurrentUserService>().Object,
            TimeProvider.System, NullLogger<BeschichtungsauftragWorkflow>.Instance);

        var r = await wf.AnlegenAsync(4711);

        r.Fehler.Should().ContainSingle().Which.Should().StartWith("Für WA 4711 gibt es keine offenen Lackierteil-Positionen");
    }

    [Fact]
    public void Sql157_Migration_FreshInstall_Manifest_Konsistent()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "SQL", "00_FreshInstall.sql"))) dir = dir.Parent;
        var root = dir!.FullName;
        const string id = "20261117141308_AddBeschichtungsauftragPositionText";
        File.Exists(Path.Combine(root, "IdealAkeWms", "Migrations", id + ".cs")).Should().BeTrue();
        var sql = File.ReadAllText(Path.Combine(root, "SQL", "157_AddBeschichtungsauftragPositionText.sql"));
        sql.Should().Contain("COL_LENGTH(N'dbo.BeschichtungsauftragPositionen', N'PositionText') IS NULL").And.Contain($"'{id}'");
        var fresh = File.ReadAllText(Path.Combine(root, "SQL", "00_FreshInstall.sql"));
        Regex.IsMatch(fresh, @"\[PositionText\]\s+NVARCHAR\(50\)\s+NULL").Should().BeTrue();
        Regex.Matches(fresh, Regex.Escape($"'{id}'")).Count.Should().Be(2, "Existenzpruefung + Insert in der History");
    }
}
