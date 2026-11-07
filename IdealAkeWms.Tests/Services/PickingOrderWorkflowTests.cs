using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Services;

/// <summary>Spec 2026-11-08-kommissionierung-auftraege-kopf-zeilen, E2 (v1.88.0): Erzeugen je FA und Bereich, idempotent, Nachziehen,
/// FA ohne Stueckliste, Sub-FA gesperrt, Soll hochgerechnet (AK 5, 6, 7, 8, 9).</summary>
public class PickingOrderWorkflowTests
{
    private readonly List<BomItem> _stueckliste = new();

    private async Task<(PickingOrderWorkflow W, ApplicationDbContext Db)> SetupAsync(decimal faMenge = 2m)
    {
        var db = TestDbContextFactory.Create();
        var lager = new PickingArea { Id = 1, Name = "Lager", Kuerzel = "L", IsActive = true, IsDefault = true, CreatedBy = "t", CreatedByWindows = "t" };
        var glas = new PickingArea { Id = 2, Name = "Glas", Kuerzel = "G", IsActive = true, Reihenfolge = 20, CreatedBy = "t", CreatedByWindows = "t" };
        db.PickingAreas.AddRange(lager, glas);
        db.PickingAreaRules.Add(new PickingAreaRule { PickingAreaId = 2, RuleType = PickingAreaRuleType.Artikelgruppe, Value = "GLAS", CreatedBy = "t", CreatedByWindows = "t" });
        db.Articles.AddRange(
            new Article { Id = 1, ArticleNumber = "A1", ArticleGroup = "960", CreatedBy = "t", CreatedByWindows = "t" },
            new Article { Id = 2, ArticleNumber = "G1", ArticleGroup = "GLAS", CreatedBy = "t", CreatedByWindows = "t" });
        db.ProductionOrders.AddRange(
            new ProductionOrder { Id = 10, OrderNumber = "FA10", SubOrderNumber = "FA10", ArticleNumber = "X", Quantity = faMenge, CreatedBy = "t", CreatedByWindows = "t" },
            new ProductionOrder { Id = 11, OrderNumber = "FA10", SubOrderNumber = "77", ArticleNumber = "X", Quantity = 1, CreatedBy = "t", CreatedByWindows = "t" });
        db.ProductionOrderPickingStatuses.Add(new ProductionOrderPickingStatus
        {
            ProductionOrderId = 10, IsReleasedForPicking = true, PickingPriority = 3, AssignedPickerId = null, AssignedPickerName = "Kommi",
            CreatedBy = "t", CreatedByWindows = "t",
        });
        await db.SaveChangesAsync();

        var bom = new Mock<IBomRepository>();
        bom.Setup(b => b.GetBomItemsAsync(It.IsAny<BomKey>(), It.IsAny<BomScope>()))
            .ReturnsAsync(() => new BomQueryResult(_stueckliste.ToList(), BomDataSources.Sage));
        var user = new Mock<ICurrentUserService>();
        user.Setup(u => u.GetDisplayName()).Returns("Leitstand");
        user.Setup(u => u.GetWindowsUserName()).Returns("AKE\\l");
        var w = new PickingOrderWorkflow(new PickingOrderRepository(db), new PickingAreaRepository(db), new ProductionOrderRepository(db),
            new ProductionOrderPickingStatusRepository(db), bom.Object, user.Object, NullLogger<PickingOrderWorkflow>.Instance)
        { Jetzt = () => new DateTime(2026, 11, 8) };
        return (w, db);
    }

    private void Pos(string art, string pos, decimal menge) =>
        _stueckliste.Add(new BomItem { Artikelnummer = "X", Ressourcenummer = art, Position = pos, Menge = menge });

    [Fact]
    public async Task Freigabe_ZweiBereiche_ZweiAuftraege_Idempotent_AK5_AK6()
    {
        Pos("A1", "10", 3m);
        Pos("G1", "20", 1m);
        var (w, db) = await SetupAsync(faMenge: 2m);

        var r = await w.ErzeugeOffeneAsync(10);

        r.Nummern.Should().Equal("KO-2026-00001", "KO-2026-00002");
        var auftraege = db.PickingOrders.OrderBy(o => o.LaufendeNummer).ToList();
        auftraege.Select(o => (o.PickingAreaId, o.Status, o.Typ, o.PickingPriority, o.AssignedPickerName))
            .Should().Equal((1, PickingOrderStatus.Offen, PickingOrderType.FA, 3, "Kommi"), (2, PickingOrderStatus.Offen, PickingOrderType.FA, 3, "Kommi"));
        // Soll hochgerechnet (AKE: Menge x Auftragsmenge, Antwort 3)
        db.PickingOrderLines.OrderBy(z => z.ArticleNumber).AsEnumerable().Select(z => (z.ArticleNumber, z.SollMenge)).Should().Equal(("A1", 6m), ("G1", 2m));

        var zweimal = await w.ErzeugeOffeneAsync(10);
        zweimal.Nummern.Should().BeEmpty();
        zweimal.OhneRest.Should().BeTrue();
        db.PickingOrders.Should().HaveCount(2);
    }

    [Fact]
    public async Task SubFa_Gesperrt_KeinAuftrag_AK7()
    {
        Pos("A1", "10", 1m);
        var (w, db) = await SetupAsync();
        (await w.ErzeugeOffeneAsync(11)).Fehler.Should().Contain("nur am HauptFA FA10");
        db.PickingOrders.Should().BeEmpty();
    }

    [Fact]
    public async Task OhneStueckliste_Meldung_KeinAuftrag_AK8()
    {
        var (w, db) = await SetupAsync();
        var r = await w.ErzeugeOffeneAsync(10);
        (r.OhneStueckliste, r.Meldungen.Single()).Should().Be((true, "FA FA10 ohne Stückliste – kein Kommissionierauftrag."));
        db.PickingOrders.Should().BeEmpty();
        PickingOrderWorkflow.Sammelmeldung(new[] { ("FA10", r) }).Warnung.Should().Contain("ohne Stückliste");
    }

    [Fact]
    public async Task Nachziehen_NeuePosition_NeuerAuftragNurMitDieser_AK9()
    {
        Pos("A1", "10", 1m);
        var (w, db) = await SetupAsync(faMenge: 1m);
        await w.ErzeugeOffeneAsync(10);
        Pos("A1", "30", 4m); // nach der Freigabe in der Stueckliste ergaenzt

        var r = await w.ErzeugeOffeneAsync(10);

        r.Nummern.Should().Equal("KO-2026-00002");
        db.PickingOrderLines.Where(z => z.PickingOrder.Nummer == "KO-2026-00002").AsEnumerable().Select(z => (z.BomPosition, z.SollMenge))
            .Should().Equal(("30", 4m));
    }

    [Fact]
    public async Task StornierterAuftrag_ZaehltNicht_RestWirdNeuAngelegt()
    {
        Pos("A1", "10", 1m);
        var (w, db) = await SetupAsync(faMenge: 1m);
        await w.ErzeugeOffeneAsync(10);
        db.PickingOrders.Single().Status = PickingOrderStatus.Storniert;
        await db.SaveChangesAsync();
        (await w.ErzeugeOffeneAsync(10)).Nummern.Should().Equal("KO-2026-00002");
    }

    [Fact]
    public async Task OhneStandardbereich_Fehler()
    {
        Pos("A1", "10", 1m);
        var (w, db) = await SetupAsync();
        db.PickingAreas.Single(a => a.IsDefault).IsDefault = false;
        await db.SaveChangesAsync();
        (await w.ErzeugeOffeneAsync(10)).Fehler.Should().Contain("Kein aktiver Standardbereich");
    }
}
