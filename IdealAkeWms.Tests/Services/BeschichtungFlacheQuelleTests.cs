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
        flach.Setup(q => q.LadenAsync(It.IsAny<IReadOnlyCollection<int>?>())).Returns<IReadOnlyCollection<int>?>(h => echt.LadenAsync(h));
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
        flach.Setup(q => q.LadenAsync(It.IsAny<IReadOnlyCollection<int>?>()))
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
