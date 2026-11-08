using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Services;

/// <summary>Spec 2026-11-08-kommissionierung-auftraege-kopf-zeilen, E1 (v1.88.0): Bereichsregel (AK 4), Restmengen (E2-Grundlage),
/// Nummernformat (AK 5), Bereichs- und Regelpruefung (AK 2, 3). SQL-UNIQUE-Indizes erzwingt InMemory nicht → App-Guards getestet.</summary>
public class KommissionierauftraegeE1Tests
{
    private static PickingArea A(int id, string name, bool aktiv = true, bool standard = false) =>
        new() { Id = id, Name = name, Kuerzel = name[..1], IsActive = aktiv, IsDefault = standard, CreatedBy = "t", CreatedByWindows = "t" };

    private static PickingAreaRule R(PickingArea a, PickingAreaRuleType t, string v) =>
        new() { PickingArea = a, PickingAreaId = a.Id, RuleType = t, Value = v, CreatedBy = "t", CreatedByWindows = "t" };

    private static readonly PickingArea Lager = A(1, "Lager", standard: true), Glas = A(2, "Glas"), Iso = A(3, "Iso"), Alt = A(4, "Alt", aktiv: false);

    private static readonly List<PickingAreaRule> Regeln = new()
    {
        R(Glas, PickingAreaRuleType.Artikel, "EK000001"),
        R(Iso, PickingAreaRuleType.Kommissionierziel, "ISO"),
        R(Glas, PickingAreaRuleType.Lagerort, "GL"),
        R(Iso, PickingAreaRuleType.Artikelgruppe, "960"),
        R(Alt, PickingAreaRuleType.Artikelgruppe, "030102"),
    };

    [Theory]
    [InlineData("EK000001", "ISO", "GL", "960", "Glas")]   // Artikel schlaegt alles
    [InlineData("X", "iso ", "GL", "960", "Iso")]           // Kommissionierziel (getrimmt, Gross/Klein egal) schlaegt Lagerort
    [InlineData("X", "KOM", "GL", "960", "Glas")]           // Lagerort schlaegt Artikelgruppe
    [InlineData("X", null, null, "960", "Iso")]             // Artikelgruppe
    [InlineData("X", null, null, "999", "Lager")]           // ohne Treffer Standard
    public void Bereichsregel_Rangfolge_AK4(string art, string? ziel, string? ort, string? gruppe, string erwartet)
    {
        var r = KommissionierbereichRegel.Resolve(new(art, ziel, ort, gruppe), Regeln, Lager);
        (r.Bereich.Name, r.Meldung).Should().Be((erwartet, (string?)null));
    }

    [Fact]
    public void Bereichsregel_InaktiverBereich_StandardMitMeldung_AK4()
    {
        var r = KommissionierbereichRegel.Resolve(new("X", null, null, "030102"), Regeln, Lager);
        r.Bereich.Should().BeSameAs(Lager);
        r.Meldung.Should().Contain("inaktiven Bereich Alt").And.Contain("Standardbereich Lager");
    }

    [Fact]
    public void Restmengen_AbgedecktJeStatus_DoppelteSchluesselSummiert()
    {
        var soll = new[]
        {
            new PickingOrderRestmengen.Soll("A", "10", 10m, false, null),
            new PickingOrderRestmengen.Soll("B", "20", 5m, false, null),
            new PickingOrderRestmengen.Soll("C", "30", 3m, false, null),
            new PickingOrderRestmengen.Soll("D", "40", 2m, false, null), new PickingOrderRestmengen.Soll("d", "40", 2m, false, null),
        };
        PickingOrderLine Z(string art, string pos, PickingOrderLineStatus st, decimal s, decimal? i = null) =>
            new() { ArticleNumber = art, BomPosition = pos, Status = st, SollMenge = s, IstMenge = i, CreatedBy = "t", CreatedByWindows = "t" };
        var zeilen = new[]
        {
            Z("A", "10", PickingOrderLineStatus.Umgebucht, 10m, 4m),      // nur 4 abgedeckt
            Z("A", "10", PickingOrderLineStatus.Uebertragen, 6m),        // 0 (steht im Folgeauftrag)
            Z("B", "20", PickingOrderLineStatus.Offen, 5m),              // voll abgedeckt
            Z("C", "30", PickingOrderLineStatus.Entfallen, 3m),          // abgedeckt (bewusst nicht gebraucht)
            Z("D", "40", PickingOrderLineStatus.Gepickt, 1m),
        };
        PickingOrderRestmengen.Berechne(soll, zeilen).Select(r => (r.Position.ArticleNumber, r.Menge))
            .Should().Equal(("A", 6m), ("D", 3m));
        PickingOrderRestmengen.Berechne(soll, Array.Empty<PickingOrderLine>()).Should().HaveCount(4);
    }

    [Fact]
    public void Nummernformat_KO_BA_AK5() =>
        (Nummernkreis.Format("KO", 2026, 1), Nummernkreis.Format("KO", 2026, 2), Nummernkreis.Format("BA", 2027, 12345))
            .Should().Be(("KO-2026-00001", "KO-2026-00002", "BA-2027-12345"));

    // ---------- Stammdaten (AK 2, 3) ----------

    private static List<PickingArea> Alle() => new() { A(1, "Lager", standard: true), A(2, "Glas") };

    [Fact]
    public void Bereich_PflichtEindeutigStandardAktiv_AK2()
    {
        var alle = Alle();
        PickingAreasController.Uebernehmen(new PickingArea(), new PickingAreaEingabe("glas", "X", 0, true, false, false), alle)
            .Should().ContainSingle(f => f.Contains("schon vergeben"));
        PickingAreasController.Uebernehmen(new PickingArea(), new PickingAreaEingabe("", "", -1, true, false, false), alle).Should().HaveCount(3);
        PickingAreasController.Uebernehmen(alle[1], new PickingAreaEingabe("Glas", "G", 0, false, true, false), alle)
            .Should().ContainSingle(f => f.Contains("Standardbereich muss aktiv"));
        // v1.89.0 (Q10): zweiter Standard wird abgelehnt
        PickingAreasController.Uebernehmen(alle[1], new PickingAreaEingabe("Glas ", "G", 5, true, true, true), alle)
            .Should().ContainSingle(f => f == "Es gibt bereits einen Standardbereich („Lager“). Zuerst dort abwählen.");
        alle[1].IsDefault.Should().BeFalse();
        // Abwaehlen ist erlaubt (dann gibt es voruebergehend keinen Standard)
        PickingAreasController.Uebernehmen(alle[0], new PickingAreaEingabe("Lager", "L", 0, true, false, false), alle).Should().BeEmpty();
        PickingAreasController.Uebernehmen(alle[1], new PickingAreaEingabe("Glas ", "G", 5, true, true, true), alle).Should().BeEmpty();
        (alle[1].Name, alle[1].IsDefault, alle[1].IstVorkommissionierung).Should().Be(("Glas", true, true));
    }

    [Fact]
    public async Task Bereich_StandardWechsel_AbwaehlenDannSetzen_Q10()
    {
        using var db = TestDbContextFactory.Create();
        db.PickingAreas.AddRange(Alle());
        await db.SaveChangesAsync();
        var ctrl = Ctrl(db);
        // zweiter Standard: abgelehnt, nichts gespeichert
        (await ctrl.Edit(2, new PickingAreaEingabe("Glas", "G", 0, true, true, false))).Should().BeOfType<Microsoft.AspNetCore.Mvc.ViewResult>();
        ctrl.ModelState.IsValid.Should().BeFalse();
        db.PickingAreas.Where(a => a.IsDefault).Select(a => a.Name).Should().Equal("Lager");
        // abwaehlen: Warnung „kein aktiver Standardbereich“
        ctrl = Ctrl(db);
        await ctrl.Edit(1, new PickingAreaEingabe("Lager", "L", 0, true, false, false));
        ctrl.TempData["WarningMessage"].Should().NotBeNull();
        db.PickingAreas.Any(a => a.IsDefault).Should().BeFalse();
        // neu setzen
        ctrl = Ctrl(db);
        await ctrl.Edit(2, new PickingAreaEingabe("Glas", "G", 0, true, true, false));
        ctrl.TempData.ContainsKey("WarningMessage").Should().BeFalse();
        db.PickingAreas.Where(a => a.IsDefault).Select(a => a.Name).Should().Equal("Glas");
        db.PickingAreas.Single(a => a.Id == 1).ModifiedBy.Should().Be("Pfleger");
    }

    private static PickingAreasController Ctrl(IdealAkeWms.Data.ApplicationDbContext db)
    {
        var user = new Mock<ICurrentUserService>();
        user.Setup(u => u.GetDisplayName()).Returns("Pfleger");
        user.Setup(u => u.GetWindowsUserName()).Returns("AKE\\p");
        var c = new PickingAreasController(new PickingAreaRepository(db), user.Object);
        var http = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        c.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext { HttpContext = http };
        c.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(http, Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());
        return c;
    }

    [Fact]
    public async Task Regel_WertImStamm_Eindeutig_AK3()
    {
        using var db = TestDbContextFactory.Create();
        db.PickingAreas.AddRange(Alle());
        db.Articles.Add(new Article { Id = 7, ArticleNumber = "EK1", ArticleGroup = "960", CreatedBy = "t", CreatedByWindows = "t" });
        await db.SaveChangesAsync();
        var c = Ctrl(db);

        (await c.RegelPruefenAsync(2, PickingAreaRuleType.Artikelgruppe, null, "777")).Fehler.Should().Contain("im Stamm nicht vor");
        (await c.RegelPruefenAsync(99, PickingAreaRuleType.Artikelgruppe, null, "960")).Fehler.Should().Be("Bereich unbekannt.");
        (await c.RegelPruefenAsync(2, (PickingAreaRuleType)9, null, "960")).Fehler.Should().Be("Regeltyp unbekannt.");
        (await c.RegelPruefenAsync(2, PickingAreaRuleType.Artikel, 7, null)).Wert.Should().Be("EK1");

        await c.RegelAnlegen(2, PickingAreaRuleType.Artikelgruppe, null, " 960 ", null);
        db.PickingAreaRules.Single().Value.Should().Be("960");
        (await c.RegelPruefenAsync(1, PickingAreaRuleType.Artikelgruppe, null, "960")).Fehler.Should().Contain("schon dem Bereich Glas");
    }
}
