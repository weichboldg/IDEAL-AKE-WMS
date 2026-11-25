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

    private async Task<(PickingOrderWorkflow W, ApplicationDbContext Db)> SetupAsync(decimal faMenge = 2m,
        Func<IPickingOrderRepository, IPickingOrderRepository>? repoHuelle = null)
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
        IPickingOrderRepository repo = new PickingOrderRepository(db);
        if (repoHuelle != null) repo = repoHuelle(repo);
        var w = new PickingOrderWorkflow(repo, new PickingAreaRepository(db), new ProductionOrderRepository(db),
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

    [Theory]
    [InlineData(PickingOrderLineStatus.Offen, "offen")]
    [InlineData(PickingOrderLineStatus.Gepickt, "gepickt")]
    [InlineData(PickingOrderLineStatus.Umgebucht, "gepickt")]
    [InlineData(PickingOrderLineStatus.Entfallen, "entfallen")]
    [InlineData(PickingOrderLineStatus.Uebertragen, "in Folgeauftrag")]
    public void ZeilenStatusText_NachDemKommissionierenEinheitlichGepickt_Q9(PickingOrderLineStatus s, string text) =>
        PickingOrderListItem.ZeilenStatusText(s).Should().Be(text);

    // ---------- v1.89.0 Entscheidung Q11: Altbestand pruefen ----------

    [Fact]
    public async Task AltbestandPruefen_KeinAutomatischerRest_ErstNachBestaetigung_Q11()
    {
        Pos("A1", "10", 3m);
        var (w, db) = await SetupAsync(faMenge: 2m);
        // Altbestand-Auftrag wie vom Einmal-Skript: Soll = Stueckmenge 3, umgebucht, Kennzeichen gesetzt
        db.PickingOrders.Add(new PickingOrder
        {
            Nummer = "KO-2026-00001", Jahr = 2026, LaufendeNummer = 1, ProductionOrderId = 10, PickingAreaId = 1,
            Status = PickingOrderStatus.Abgeschlossen, Bemerkung = "Altbestand vor v1.88.0", AltbestandPruefen = true,
            CreatedBy = "Migration-KO-Altbestand", CreatedByWindows = "t",
            Zeilen = { new PickingOrderLine { ProductionOrderId = 10, ArticleNumber = "A1", BomPosition = "10", SollMenge = 3m, IstMenge = 3m,
                Status = PickingOrderLineStatus.Umgebucht, CreatedBy = "t", CreatedByWindows = "t" } },
        });
        await db.SaveChangesAsync();

        var e = await w.ErzeugeOffeneAsync(10);
        e.AltbestandPruefen.Should().BeTrue();
        e.Nummern.Should().BeEmpty();
        PickingOrderWorkflow.Sammelmeldung(new[] { ("FA10", e) }).Warnung.Should().Contain("Altbestand prüfen");
        db.PickingOrders.Should().ContainSingle();

        (await w.AltbestandGeprueftAsync(10)).Should().Equal("KO-2026-00001");
        db.PickingOrders.Single().ModifiedBy.Should().Be("Leitstand");
        (await w.AltbestandGeprueftAsync(10)).Should().BeEmpty(); // zweites Mal: nichts zu tun

        // jetzt der Rest (2 × 3 − 3 = 3) wie gewohnt
        var danach = await w.ErzeugeOffeneAsync(10);
        danach.Nummern.Should().Equal("KO-2026-00002");
        db.PickingOrderLines.Single(z => z.PickingOrder.Nummer == "KO-2026-00002").SollMenge.Should().Be(3m);
    }

    // ---------- v1.99.70 F8: Marker „Stückliste kleiner“ ----------

    [Fact]
    public async Task StuecklisteKleiner_MarkerGesetzt_UnveraendertKeinUpdate_DanachGeloescht()
    {
        Pos("A1", "10", 3m);
        var (w, db) = await SetupAsync(faMenge: 1m);
        (await w.ErzeugeOffeneAsync(10)).Ueberdeckt.Should().BeEmpty();
        db.ProductionOrderPickingStatuses.Single().StuecklisteKleiner.Should().BeNull();

        _stueckliste.Clear();
        Pos("A1", "10", 1m); // Stueckliste nach der Freigabe gesenkt
        var r = await w.ErzeugeOffeneAsync(10);
        r.Nummern.Should().BeEmpty();
        r.Meldungen.Should().ContainSingle(m => m == "FA FA10: Stückliste kleiner als offene Kommissionierzeilen – A1 Pos 10: Stückliste 1, beauftragt 3");
        var ps = db.ProductionOrderPickingStatuses.Single();
        (ps.StuecklisteKleiner, ps.ModifiedBy).Should().Be(("A1 Pos 10: Stückliste 1, beauftragt 3", "Leitstand"));
        db.PickingOrderLines.Single().SollMenge.Should().Be(3m); // nie gekuerzt (Antwort 2)

        ps.ModifiedAt = new DateTime(2000, 1, 1);
        await db.SaveChangesAsync();
        await w.ErzeugeOffeneAsync(10); // gleicher Text → kein Update
        db.ProductionOrderPickingStatuses.Single().ModifiedAt.Should().Be(new DateTime(2000, 1, 1));

        Pos("A1", "10", 2m); // Soll wieder 3
        (await w.ErzeugeOffeneAsync(10)).Ueberdeckt.Should().BeEmpty();
        db.ProductionOrderPickingStatuses.Single().StuecklisteKleiner.Should().BeNull();
    }

    [Fact]
    public async Task StuecklisteKleiner_AltbestandOderOhneStueckliste_MarkerBleibtStehen()
    {
        Pos("A1", "10", 1m);
        var (w, db) = await SetupAsync(faMenge: 1m);
        db.ProductionOrderPickingStatuses.Single().StuecklisteKleiner = "alt";
        await db.SaveChangesAsync();
        _stueckliste.Clear();
        (await w.ErzeugeOffeneAsync(10)).OhneStueckliste.Should().BeTrue();
        db.ProductionOrderPickingStatuses.Single().StuecklisteKleiner.Should().Be("alt");
    }

    [Fact]
    public async Task ZeilenAngelegt_Gezaehlt()
    {
        Pos("A1", "10", 1m);
        Pos("A1", "20", 1m);
        Pos("G1", "30", 1m);
        var (w, _) = await SetupAsync(faMenge: 1m);
        (await w.ErzeugeOffeneAsync(10)).ZeilenAngelegt.Should().Be(3);
    }

    // ---------- E3: Abschluss (AK 13, 14) ----------

    private static void Umgebucht(PickingOrderLine z, decimal ist)
    {
        z.Status = PickingOrderLineStatus.Umgebucht;
        z.IstMenge = ist;
    }

    [Fact]
    public async Task Abschluss_Teilweise_Folgeauftrag_MitRest_FaNichtErledigt_AK14()
    {
        Pos("A1", "10", 10m);
        Pos("A2", "20", 2m);
        var (w, db) = await SetupAsync(faMenge: 1m);
        await w.ErzeugeOffeneAsync(10);
        var auftrag = db.PickingOrders.Single();
        Umgebucht(db.PickingOrderLines.Single(z => z.ArticleNumber == "A1"), 4m);
        await db.SaveChangesAsync();

        var r = await w.AbschliessenAsync(auftrag.Id, vollstaendig: false);

        r.Nummern.Should().Equal("KO-2026-00002");
        r.FaAbgeschlossen.Should().BeFalse();
        var folge = db.PickingOrders.Single(o => o.Nummer == "KO-2026-00002");
        (folge.UrsprungsauftragId, folge.PickingAreaId, folge.PickingPriority, folge.Status)
            .Should().Be(((int?)auftrag.Id, auftrag.PickingAreaId, (int?)3, PickingOrderStatus.Offen));
        db.PickingOrderLines.Where(z => z.PickingOrderId == folge.Id).OrderBy(z => z.ArticleNumber).AsEnumerable()
            .Select(z => (z.ArticleNumber, z.SollMenge)).Should().Equal(("A1", 6m), ("A2", 2m));
        db.PickingOrders.Single(o => o.Id == auftrag.Id).Status.Should().Be(PickingOrderStatus.TeilweiseAbgeschlossen);
        db.PickingOrderLines.Single(z => z.PickingOrderId == auftrag.Id && z.ArticleNumber == "A2").Status.Should().Be(PickingOrderLineStatus.Uebertragen);
        db.ProductionOrderPickingStatuses.Single().PickingStatus.Should().Be("teilkommissioniert");
        db.ProductionOrderPickingStatuses.Single().IsDonePicking.Should().BeFalse();

        // Nachziehen erzeugt nichts doppelt (Rest steht im Folgeauftrag)
        (await w.ErzeugeOffeneAsync(10)).Nummern.Should().BeEmpty();

        // Folgeauftrag vollstaendig → FA erledigt
        var f = await w.AbschliessenAsync(folge.Id, vollstaendig: true);
        f.FaAbgeschlossen.Should().BeTrue();
        db.ProductionOrderPickingStatuses.Single().IsDonePicking.Should().BeTrue();
        db.ProductionOrderPickingStatuses.Single().PickingStatus.Should().Be("abgeschlossen");
    }

    [Fact]
    public async Task Abschluss_Vollstaendig_OffeneZeilenEntfallen_NachziehenLegtNichtsAn_AK13()
    {
        Pos("A1", "10", 10m);
        Pos("A2", "20", 2m);
        var (w, db) = await SetupAsync(faMenge: 1m);
        await w.ErzeugeOffeneAsync(10);
        var auftrag = db.PickingOrders.Single();
        Umgebucht(db.PickingOrderLines.Single(z => z.ArticleNumber == "A1"), 4m); // bewusst weniger, vollstaendig
        await db.SaveChangesAsync();

        var r = await w.AbschliessenAsync(auftrag.Id, vollstaendig: true);

        (r.Nummern.Count, r.FaAbgeschlossen).Should().Be((0, true));
        db.PickingOrderLines.Single(z => z.ArticleNumber == "A2").Status.Should().Be(PickingOrderLineStatus.Entfallen);
        var abg = db.PickingOrders.Single();
        (abg.Status, abg.AbgeschlossenVon, abg.ModifiedBy).Should().Be((PickingOrderStatus.Abgeschlossen, "Leitstand", "Leitstand"));
        (await w.ErzeugeOffeneAsync(10)).Nummern.Should().BeEmpty("„vollständig“ heißt: der Rest wird nicht gebraucht");
        (await w.AbschliessenAsync(auftrag.Id, true)).Fehler.Should().Contain("bereits abgeschlossen");
    }

    /// <summary>v1.99.23 (K-1): Repository-Huelle – vor dem ersten Abschluss-Versuch schliesst „eine zweite Anfrage“ den Auftrag ab,
    /// der Versuch verliert den Gleichstand (Concurrency-Fehler), der Wiederholversuch muss den neuen Status sehen.</summary>
    private sealed class GleichstandRepo(IPickingOrderRepository inner) : IPickingOrderRepository
    {
        public ApplicationDbContext? Db { get; set; }
        public int? AuftragId { get; set; } // gesetzt = beim naechsten Transaktionsaufruf einmal den Gleichstand verlieren
        public Task<int> GetMaxLaufendeNummerAsync(int jahr) => inner.GetMaxLaufendeNummerAsync(jahr);
        public Task<List<PickingOrderLine>> GetAktiveZeilenAsync(int id) => inner.GetAktiveZeilenAsync(id);
        public Task<Dictionary<string, (string? Lagerort, string? Artikelgruppe)>> GetArtikelBereichsdatenAsync(IReadOnlyCollection<string> a) => inner.GetArtikelBereichsdatenAsync(a);
        public Task<List<PickingOrder>> GetByProductionOrderAsync(int id) => inner.GetByProductionOrderAsync(id);
        public Task<Dictionary<int, List<PickingOrder>>> GetByProductionOrdersAsync(IReadOnlyCollection<int> ids) => inner.GetByProductionOrdersAsync(ids);
        public Task<List<PickingOrder>> GetListeAsync(bool nurOffene) => inner.GetListeAsync(nurOffene);
        public Task<PickingOrder?> GetMitZeilenAsync(int id) => inner.GetMitZeilenAsync(id);
        public void Add(PickingOrder order) => inner.Add(order);
        public Task SaveChangesAsync() => inner.SaveChangesAsync();
        public void VerwerfeAenderungen() => inner.VerwerfeAenderungen();
        public async Task<T> InSerializableTransactionAsync<T>(Func<Task<T>> action)
        {
            if (AuftragId is not { } id) return await inner.InSerializableTransactionAsync(action);
            AuftragId = null;
            Db!.PickingOrders.Single(o => o.Id == id).Status = PickingOrderStatus.TeilweiseAbgeschlossen;
            await Db.SaveChangesAsync();
            throw new Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException("Gleichstand verloren");
        }
    }

    [Fact]
    public async Task Abschluss_GleichstandVerloren_WiederholversuchPrueftStatus_KeinFolgeauftrag_AK2()
    {
        Pos("A1", "10", 10m);
        GleichstandRepo? huelle = null;
        var (w, db) = await SetupAsync(faMenge: 1m, repoHuelle: inner => huelle = new GleichstandRepo(inner));
        await w.ErzeugeOffeneAsync(10);
        var auftrag = db.PickingOrders.Single();
        Umgebucht(db.PickingOrderLines.Single(), 4m);
        await db.SaveChangesAsync();
        var statusVorher = db.ProductionOrderPickingStatuses.Single().PickingStatus;
        huelle!.Db = db;
        huelle.AuftragId = auftrag.Id;

        var r = await w.AbschliessenAsync(auftrag.Id, vollstaendig: false);

        r.Fehler.Should().Be($"Auftrag {auftrag.Nummer} ist bereits teilweise abgeschlossen – nichts geändert.");
        r.Nummern.Should().BeEmpty();
        db.PickingOrders.Should().ContainSingle("kein Folgeauftrag");
        db.ProductionOrderPickingStatuses.Single().PickingStatus.Should().Be(statusVorher, "FA-Status unveraendert");
        db.PickingOrderLines.Single().Status.Should().Be(PickingOrderLineStatus.Umgebucht);
    }

    [Fact]
    public async Task Abschluss_Teilweise_Zweimal_GenauEinFolgeauftrag_AK3()
    {
        Pos("A1", "10", 10m);
        var (w, db) = await SetupAsync(faMenge: 1m);
        await w.ErzeugeOffeneAsync(10);
        var auftrag = db.PickingOrders.Single();
        Umgebucht(db.PickingOrderLines.Single(), 4m);
        await db.SaveChangesAsync();

        (await w.AbschliessenAsync(auftrag.Id, false)).Fehler.Should().BeNull();
        var zweiter = await w.AbschliessenAsync(auftrag.Id, false);

        zweiter.Fehler.Should().EndWith("– nichts geändert.");
        db.PickingOrders.Count(o => o.UrsprungsauftragId == auftrag.Id).Should().Be(1);
    }

    [Fact]
    public async Task Abschluss_ZweiBereiche_FaErstNachBeidenErledigt()
    {
        Pos("A1", "10", 1m);
        Pos("G1", "20", 1m);
        var (w, db) = await SetupAsync(faMenge: 1m);
        await w.ErzeugeOffeneAsync(10);
        var ids = db.PickingOrders.OrderBy(o => o.Id).Select(o => o.Id).ToList();
        (await w.AbschliessenAsync(ids[0], true)).FaAbgeschlossen.Should().BeFalse();
        (await w.AbschliessenAsync(ids[1], true)).FaAbgeschlossen.Should().BeTrue();
    }

    // ---------- E4 (AK 16, 17) ----------

    [Fact]
    public async Task FreigabeZuruecknehmen_StorniertOhneFortschritt_BehaeltMitFortschritt_AK16()
    {
        Pos("A1", "10", 1m);
        Pos("G1", "20", 1m);
        var (w, db) = await SetupAsync(faMenge: 1m);
        await w.ErzeugeOffeneAsync(10);
        db.PickingOrderLines.Single(z => z.ArticleNumber == "G1").Status = PickingOrderLineStatus.Gepickt;
        await db.SaveChangesAsync();

        var (storniert, behalten) = await w.FreigabeZuruecknehmenAsync(10);

        (storniert.Single(), behalten.Single()).Should().Be(("KO-2026-00001", "KO-2026-00002"));
        db.PickingOrders.Single(o => o.Nummer == "KO-2026-00001").Should().Match<PickingOrder>(o => o.Status == PickingOrderStatus.Storniert && o.ModifiedBy == "Leitstand");
        db.PickingOrders.Single(o => o.Nummer == "KO-2026-00002").Status.Should().Be(PickingOrderStatus.Offen);
    }

    [Fact]
    public async Task PrioUndKommissionierer_NurOffeneAuftraege_AK17()
    {
        Pos("A1", "10", 1m);
        Pos("G1", "20", 1m);
        var (w, db) = await SetupAsync(faMenge: 1m);
        await w.ErzeugeOffeneAsync(10);
        var erster = db.PickingOrders.OrderBy(o => o.Id).First();
        erster.Status = PickingOrderStatus.Abgeschlossen;
        await db.SaveChangesAsync();

        (await w.PrioritaetDurchschreibenAsync(10, 9)).Should().Be(1);
        (await w.KommissioniererDurchschreibenAsync(10, 5, "Neu")).Should().Be(1);

        db.PickingOrders.OrderBy(o => o.Id).AsEnumerable().Select(o => (o.PickingPriority, o.AssignedPickerName))
            .Should().Equal(((int?)3, "Kommi"), ((int?)9, "Neu"));
    }
}
