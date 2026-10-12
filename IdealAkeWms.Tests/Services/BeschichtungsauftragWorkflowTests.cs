using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Services;

/// <summary>
/// Gespeicherter Beschichtungsauftrag (Spec 2026-10-06-beschichtungsauftrag-ausbau-teil-3, v1.50.0): Nummernkreis,
/// Status-Automat, Dienstleister-Pflicht, Doppelvergabe (Status-Tabelle), Anlegen nur freie Positionen, Bearbeiten,
/// Hinzufuegen (Erst- und Reklamations-Entwurf), Reklamation inkl. Ketten, Audit, Snapshot nach Sync.
/// EF InMemory: die Vergabe-Pruefung ist App-Layer (der Positionsindex ist ohnehin nicht UNIQUE); RowVersion,
/// Serializable-Transaktion und echter Parallelfall sind Manual-UAT/SQL (TS-83).
/// </summary>
public class BeschichtungsauftragWorkflowTests
{
    private const int HauptFa = 4711;

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;
        public override DateTimeOffset GetUtcNow() => Now;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    /// <summary>Simuliert parallele Anlage: der erste <paramref name="konflikte"/> SaveChanges-Aufrufe scheitern am
    /// UNIQUE-Index der Nummer, und danach ist die Nummer vergeben (Max liefert +1).</summary>
    private sealed class KonfliktRepository(ApplicationDbContext db, int konflikte) : BeschichtungsauftragRepository(db)
    {
        public int Versuche { get; private set; }
        private int _vergeben;

        public override async Task<int> GetMaxLaufendeNummerAsync(int jahr) => await base.GetMaxLaufendeNummerAsync(jahr) + _vergeben;

        public override Task SaveChangesAsync()
        {
            Versuche++;
            if (Versuche <= konflikte)
            {
                _vergeben++;
                throw new DbUpdateException("Cannot insert duplicate key row ... IX_Beschichtungsauftraege_Nummer");
            }
            return base.SaveChangesAsync();
        }
    }

    private sealed class Fx
    {
        public ApplicationDbContext Db { get; }
        public FixedTime Time { get; } = new(new DateTimeOffset(2026, 10, 12, 9, 0, 0, TimeSpan.Zero));
        public BeschichtungsauftragRepository Repo { get; }
        public BeschichtungsauftragWorkflow Wf { get; }

        public Fx(ApplicationDbContext? db = null, Func<ApplicationDbContext, BeschichtungsauftragRepository>? repo = null)
        {
            Db = db ?? TestDbContextFactory.Create(Guid.NewGuid().ToString());
            Repo = repo?.Invoke(Db) ?? new BeschichtungsauftragRepository(Db);
            var user = new Mock<ICurrentUserService>();
            user.Setup(u => u.GetDisplayName()).Returns("Tester");
            user.Setup(u => u.GetWindowsUserName()).Returns("DOM\\tester");
            user.Setup(u => u.GetCurrentAppUserId()).Returns(7);
            Wf = new BeschichtungsauftragWorkflow(Repo, new FaHierarchyNodeRepository(Db),
                new FaHierarchyOrderInfoRepository(Db), user.Object, Time);
        }

        public FaHierarchyNode Node(int vater, int subFa, int pos, bool beschichtet = true, decimal soll = 2m,
            string artnr = "A", int hauptFa = HauptFa)
        {
            var n = new FaHierarchyNode
            {
                HauptFA = hauptFa, VaterFA = vater, SubFA = subFa, Position = pos, Beschichtet = beschichtet,
                Sollmenge = soll, Artnr = artnr, HauptArtnr = "H-" + artnr, Matchcode = "MC-" + artnr,
                Breite = 500m, Hoehe = 20m, Tiefe = 3m,
            };
            Db.FaHierarchyNodes.Add(n);
            Db.SaveChanges();
            return n;
        }

        public FaHierarchyOrderInfo Info(string? dienstleister = "Huber GmbH", string? ral = "RAL 9016", int hauptFa = HauptFa)
        {
            var i = new FaHierarchyOrderInfo
            {
                HauptFA = hauptFa, Dienstleister = dienstleister, RAL = ral,
                Start_Beschichtung = new DateTime(2026, 11, 2), Beschichten_Retour = new DateTime(2026, 11, 9),
            };
            Db.FaHierarchyOrderInfos.Add(i);
            Db.SaveChanges();
            return i;
        }

        /// <summary>Standard-HauptFA mit drei beschichteten und einer unbeschichteten Position + eindeutigem Kopf.</summary>
        public void Standard(int hauptFa = HauptFa)
        {
            Node(100, 0, 1, artnr: "A1", hauptFa: hauptFa);
            Node(100, 0, 2, artnr: "A2", soll: 3m, hauptFa: hauptFa);
            Node(100, 200, 3, artnr: "A3", hauptFa: hauptFa);
            Node(100, 0, 4, beschichtet: false, artnr: "X", hauptFa: hauptFa);
            Info(hauptFa: hauptFa);
        }

        public async Task<Beschichtungsauftrag> Anlegen(int hauptFa = HauptFa)
        {
            var r = await Wf.AnlegenAsync(hauptFa);
            r.Fehler.Should().BeEmpty();
            return r.Auftrag!;
        }

        public async Task Status(Beschichtungsauftrag a, BeschichtungsauftragStatus ziel)
            => (await Wf.StatusWechselnAsync(a.Id, ziel, a.RowVersion)).Fehler.Should().BeEmpty();

        public async Task<Beschichtungsauftrag> Erledigt(int hauptFa = HauptFa)
        {
            var a = await Anlegen(hauptFa);
            await Status(a, BeschichtungsauftragStatus.Uebergeben);
            await Status(a, BeschichtungsauftragStatus.Erledigt);
            return a;
        }
    }

    // ===== Anlegen (AK 2, 2a, 3, 4, 7, 8) =====

    [Fact]
    public async Task Anlegen_ErzeugtEntwurf_MitNummer_Snapshot_Kopf_Audit()
    {
        var fx = new Fx();
        fx.Standard();

        var r = await fx.Wf.AnlegenAsync(HauptFa);

        r.Erfolg.Should().BeTrue();
        var a = r.Auftrag!;
        a.Nummer.Should().Be("BA-2026-00001");
        a.Status.Should().Be(BeschichtungsauftragStatus.Entwurf);
        a.HauptFA.Should().Be(HauptFa);
        a.Dienstleister.Should().Be("Huber GmbH");
        a.Ral.Should().Be("RAL 9016");
        a.StartBeschichtung.Should().Be(new DateTime(2026, 11, 2));
        a.BeschichtenRetour.Should().Be(new DateTime(2026, 11, 9));
        a.CreatedBy.Should().Be("Tester");
        a.CreatedByWindows.Should().Be("DOM\\tester");
        a.Positionen.Should().HaveCount(3, "nur beschichtete Positionen");
        var p2 = a.Positionen.Single(p => p.Position == 2);
        (p2.HauptFA, p2.VaterFA, p2.SubFA, p2.Artnr, p2.HauptArtnr, p2.Matchcode, p2.Menge, p2.Breite, p2.Hoehe, p2.Tiefe)
            .Should().Be((HauptFa, (int?)100, 0, "A2", "H-A2", "MC-A2", 3m, 500m, 20m, 3m));
        p2.CreatedBy.Should().Be("Tester");
        (await fx.Db.Beschichtungsauftraege.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Anlegen_Nummern_Fortlaufend_UndNeuesJahrBeginntBei1_OhneStandortkuerzel()
    {
        var fx = new Fx();
        fx.Standard(1);
        fx.Standard(2);
        fx.Standard(3);

        (await fx.Anlegen(1)).Nummer.Should().Be("BA-2026-00001");
        (await fx.Anlegen(2)).Nummer.Should().Be("BA-2026-00002");
        fx.Time.Now = new DateTimeOffset(2027, 1, 2, 8, 0, 0, TimeSpan.Zero);
        var a3 = await fx.Anlegen(3);
        a3.Nummer.Should().Be("BA-2027-00001");
        (a3.Jahr, a3.LaufendeNummer).Should().Be((2027, 1));
    }

    [Fact]
    public async Task Anlegen_NummernKonflikt_WirdMitNeuerNummerWiederholt()
    {
        KonfliktRepository? repo = null;
        var fx = new Fx(repo: db => repo = new KonfliktRepository(db, konflikte: 1));
        fx.Standard();

        var r = await fx.Wf.AnlegenAsync(HauptFa);

        r.Fehler.Should().BeEmpty();
        r.Auftrag!.Nummer.Should().Be("BA-2026-00002");
        repo!.Versuche.Should().Be(2);
        (await fx.Db.Beschichtungsauftraege.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Anlegen_NummernKonflikt_Erschoepft_Klartext_NichtsGespeichert()
    {
        var fx = new Fx(repo: db => new KonfliktRepository(db, konflikte: 99));
        fx.Standard();

        var r = await fx.Wf.AnlegenAsync(HauptFa);

        r.Erfolg.Should().BeFalse();
        r.Fehler.Single().Should().Contain("Nummer").And.Contain("erneut versuchen");
        fx.Db.ChangeTracker.Entries<Beschichtungsauftrag>().Should().BeEmpty();
    }

    [Fact]
    public async Task Anlegen_TeilweiseVergeben_NurFreie_UndHinweisMitNummer()
    {
        var fx = new Fx();
        fx.Standard();
        var a = await fx.Anlegen();
        var entfernt = a.Positionen.Single(p => p.Position == 2);
        (await fx.Wf.EntwurfBearbeitenAsync(a.Id, Kopf(a), new Dictionary<int, decimal>(), new[] { entfernt.Id }, a.RowVersion))
            .Fehler.Should().BeEmpty();

        var r = await fx.Wf.AnlegenAsync(HauptFa);

        r.Fehler.Should().BeEmpty();
        r.Auftrag!.Positionen.Select(p => p.Position).Should().Equal(2);
        r.Hinweise.Should().ContainSingle(h => h.Contains("2 Positionen") && h.Contains("BA-2026-00001") && h.Contains("nicht übernommen"));
    }

    [Fact]
    public async Task Anlegen_AlleVergeben_Abgelehnt_OhneNummernverbrauch()
    {
        var fx = new Fx();
        fx.Standard();
        fx.Standard(99);
        await fx.Anlegen();

        var r = await fx.Wf.AnlegenAsync(HauptFa);

        r.Erfolg.Should().BeFalse();
        r.Fehler.Single().Should().Contain("bereits vergeben").And.Contain("BA-2026-00001");
        (await fx.Anlegen(99)).Nummer.Should().Be("BA-2026-00002", "die abgelehnte Anlage verbraucht keine Nummer");
    }

    [Fact]
    public async Task Anlegen_OhneBeschichtetePositionen_Abgelehnt()
    {
        var fx = new Fx();
        fx.Node(100, 0, 1, beschichtet: false);

        var r = await fx.Wf.AnlegenAsync(HauptFa);

        r.Erfolg.Should().BeFalse();
        r.Fehler.Single().Should().Contain("keine beschichteten Positionen");
    }

    [Fact]
    public async Task Anlegen_Kombigeraet_AbweichendeKopffelderLeer_HinweisMitVarianten()
    {
        var fx = new Fx();
        fx.Node(100, 0, 1);
        fx.Info("Huber GmbH", "RAL 9016");
        fx.Info("Maier KG", "RAL 9016");

        var r = await fx.Wf.AnlegenAsync(HauptFa);

        r.Fehler.Should().BeEmpty();
        r.Auftrag!.Dienstleister.Should().BeNull("nie still eine Variante waehlen");
        r.Auftrag.Ral.Should().Be("RAL 9016", "gleicher Wert in allen Kopfzeilen");
        r.Hinweise.Should().ContainSingle(h => h.Contains("mehrdeutig") && h.Contains("2 Kopfzeilen")
            && h.Contains("Huber GmbH") && h.Contains("Maier KG"));
    }

    [Fact]
    public async Task Anlegen_DoppelterSchluesselImCache_Hinweis()
    {
        var fx = new Fx();
        fx.Node(100, 0, 1, artnr: "A1");
        fx.Node(100, 0, 1, artnr: "A1b");

        var r = await fx.Wf.AnlegenAsync(HauptFa);

        r.Hinweise.Should().Contain(h => h.Contains("nicht eindeutig"));
    }

    // ===== Statusautomat (AK 5, 5a) =====

    [Fact]
    public async Task Status_ErlaubteUebergaenge_SetzenZeitpunktBenutzerUndModified()
    {
        var fx = new Fx();
        fx.Standard();
        fx.Standard(2);
        var a = await fx.Anlegen();

        await fx.Status(a, BeschichtungsauftragStatus.Uebergeben);
        a.UebergebenAt.Should().Be(new DateTime(2026, 10, 12, 9, 0, 0));
        a.UebergebenByUserId.Should().Be(7);
        a.ModifiedBy.Should().Be("Tester");
        a.ModifiedByWindows.Should().Be("DOM\\tester");

        await fx.Status(a, BeschichtungsauftragStatus.Erledigt);
        (a.Status, a.ErledigtByUserId).Should().Be((BeschichtungsauftragStatus.Erledigt, (int?)7));

        var b = await fx.Anlegen(2);
        await fx.Status(b, BeschichtungsauftragStatus.Storniert);
        (b.Status, b.StorniertByUserId).Should().Be((BeschichtungsauftragStatus.Storniert, (int?)7));
    }

    [Theory]
    [InlineData(BeschichtungsauftragStatus.Entwurf, BeschichtungsauftragStatus.Erledigt)]
    [InlineData(BeschichtungsauftragStatus.Uebergeben, BeschichtungsauftragStatus.Storniert)]
    [InlineData(BeschichtungsauftragStatus.Uebergeben, BeschichtungsauftragStatus.Entwurf)]
    [InlineData(BeschichtungsauftragStatus.Erledigt, BeschichtungsauftragStatus.Entwurf)]
    [InlineData(BeschichtungsauftragStatus.Erledigt, BeschichtungsauftragStatus.Uebergeben)]
    [InlineData(BeschichtungsauftragStatus.Erledigt, BeschichtungsauftragStatus.Storniert)]
    [InlineData(BeschichtungsauftragStatus.Storniert, BeschichtungsauftragStatus.Entwurf)]
    [InlineData(BeschichtungsauftragStatus.Entwurf, BeschichtungsauftragStatus.Entwurf)]
    public async Task Status_VerboteneUebergaenge_Abgelehnt_NichtsGeaendert(BeschichtungsauftragStatus von, BeschichtungsauftragStatus nach)
    {
        var fx = new Fx();
        fx.Standard();
        var a = await fx.Anlegen();
        if (von == BeschichtungsauftragStatus.Storniert) await fx.Status(a, von);
        if (von is BeschichtungsauftragStatus.Uebergeben or BeschichtungsauftragStatus.Erledigt) await fx.Status(a, BeschichtungsauftragStatus.Uebergeben);
        if (von == BeschichtungsauftragStatus.Erledigt) await fx.Status(a, BeschichtungsauftragStatus.Erledigt);
        var modifiedVorher = a.ModifiedAt;
        fx.Time.Now = fx.Time.Now.AddHours(1);

        var r = await fx.Wf.StatusWechselnAsync(a.Id, nach, a.RowVersion);

        r.Erfolg.Should().BeFalse();
        r.Fehler.Single().Should().Contain("nicht erlaubt");
        a.Status.Should().Be(von);
        a.ModifiedAt.Should().Be(modifiedVorher);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Uebergeben_OhneDienstleister_Abgelehnt_NichtsGeaendert(string? dienstleister)
    {
        var fx = new Fx();
        fx.Node(100, 0, 1);
        fx.Info(dienstleister);
        var a = await fx.Anlegen();
        a.ModifiedAt.Should().BeNull();

        var r = await fx.Wf.StatusWechselnAsync(a.Id, BeschichtungsauftragStatus.Uebergeben, a.RowVersion);

        r.Fehler.Single().Should().StartWith("Dienstleister fehlt");
        (a.Status, a.UebergebenAt, a.ModifiedAt).Should().Be((BeschichtungsauftragStatus.Entwurf, (DateTime?)null, (DateTime?)null));

        (await fx.Wf.EntwurfBearbeitenAsync(a.Id, Kopf(a) with { Dienstleister = "Huber" }, new Dictionary<int, decimal>(), Array.Empty<int>(), a.RowVersion))
            .Fehler.Should().BeEmpty();
        (await fx.Wf.StatusWechselnAsync(a.Id, BeschichtungsauftragStatus.Uebergeben, a.RowVersion)).Fehler.Should().BeEmpty();
    }

    // ===== Doppelvergabe (AK 6, 6a, 6b) =====

    [Theory]
    [InlineData(BeschichtungsauftragStatus.Entwurf)]
    [InlineData(BeschichtungsauftragStatus.Uebergeben)]
    [InlineData(BeschichtungsauftragStatus.Erledigt)]
    public async Task Doppelvergabe_SperrendeStatus_ZweiterErstauftragAbgelehnt(BeschichtungsauftragStatus status)
    {
        var fx = new Fx();
        fx.Standard();
        var a = await fx.Anlegen();
        if (status != BeschichtungsauftragStatus.Entwurf) await fx.Status(a, BeschichtungsauftragStatus.Uebergeben);
        if (status == BeschichtungsauftragStatus.Erledigt) await fx.Status(a, BeschichtungsauftragStatus.Erledigt);

        var r = await fx.Wf.AnlegenAsync(HauptFa);

        r.Erfolg.Should().BeFalse();
        r.Fehler.Single().Should().Contain(a.Nummer);
    }

    [Fact]
    public async Task Storno_GibtFrei_EntfernteSindFrei_HinzufuegenInAnderenEntwurf()
    {
        var fx = new Fx();
        fx.Standard();
        var a = await fx.Anlegen();                                   // Pos 1,2,3
        var pos2 = a.Positionen.Single(p => p.Position == 2);
        (await fx.Wf.EntwurfBearbeitenAsync(a.Id, Kopf(a), new Dictionary<int, decimal>(), new[] { pos2.Id }, a.RowVersion))
            .Fehler.Should().BeEmpty();

        var b = await fx.Anlegen();                                   // entfernte Pos 2 ist frei (AK 6b)
        b.Positionen.Select(p => p.Position).Should().Equal(2);

        await fx.Status(a, BeschichtungsauftragStatus.Storniert);     // Pos 1, 3 wieder frei (AK 6a)
        var kandidaten = await fx.Wf.GetHinzufuegbarePositionenAsync(b.Id);
        kandidaten.Select(k => k.Schluessel.Position).Should().BeEquivalentTo(new int?[] { 1, 3 });

        var r = await fx.Wf.PositionenHinzufuegenAsync(b.Id, kandidaten.Select(k => k.Schluessel).ToList(), b.RowVersion);
        r.Fehler.Should().BeEmpty();
        b.Positionen.Select(p => p.Position).Should().BeEquivalentTo(new int?[] { 1, 2, 3 });

        var c = await fx.Wf.AnlegenAsync(HauptFa);                  // jetzt alles wieder in b vergeben
        c.Fehler.Single().Should().Contain(b.Nummer);
    }

    // ===== Bearbeiten, Bemerkung (AK 8b + Review Focus) =====

    [Fact]
    public async Task Bearbeiten_NurImEntwurf_SetztModified_DanachNurBemerkung()
    {
        var fx = new Fx();
        fx.Standard();
        var a = await fx.Anlegen();
        var p1 = a.Positionen.Single(p => p.Position == 1);

        var r = await fx.Wf.EntwurfBearbeitenAsync(a.Id, new KopfDaten("Maier KG", "RAL 7016", null, new DateTime(2026, 12, 1)),
            new Dictionary<int, decimal> { [p1.Id] = 5m }, Array.Empty<int>(), a.RowVersion);

        r.Fehler.Should().BeEmpty();
        (a.Dienstleister, a.Ral, a.StartBeschichtung, a.BeschichtenRetour).Should().Be(("Maier KG", "RAL 7016", (DateTime?)null, (DateTime?)new DateTime(2026, 12, 1)));
        p1.Menge.Should().Be(5m);
        p1.ModifiedBy.Should().Be("Tester");
        a.ModifiedBy.Should().Be("Tester");

        await fx.Status(a, BeschichtungsauftragStatus.Uebergeben);
        (await fx.Wf.EntwurfBearbeitenAsync(a.Id, Kopf(a) with { Ral = "anders" }, new Dictionary<int, decimal>(), Array.Empty<int>(), a.RowVersion))
            .Fehler.Single().Should().Contain("nur im Entwurf");
        a.Ral.Should().Be("RAL 7016");
        (await fx.Wf.BemerkungAendernAsync(a.Id, "Lieferschein liegt bei", a.RowVersion)).Fehler.Should().BeEmpty();
        a.Bemerkung.Should().Be("Lieferschein liegt bei");
    }

    [Fact]
    public async Task Bemerkung_ImStorniert_Abgelehnt()
    {
        var fx = new Fx();
        fx.Standard();
        var a = await fx.Anlegen();
        await fx.Status(a, BeschichtungsauftragStatus.Storniert);

        (await fx.Wf.BemerkungAendernAsync(a.Id, "x", a.RowVersion)).Fehler.Single().Should().Contain("Storniert");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Bearbeiten_UngueltigeMenge_Abgelehnt(int menge)
    {
        var fx = new Fx();
        fx.Standard();
        var a = await fx.Anlegen();
        var p1 = a.Positionen.Single(p => p.Position == 1);

        var r = await fx.Wf.EntwurfBearbeitenAsync(a.Id, Kopf(a), new Dictionary<int, decimal> { [p1.Id] = menge }, Array.Empty<int>(), a.RowVersion);

        r.Fehler.Single().Should().Contain("größer 0");
        p1.Menge.Should().Be(2m);
    }

    [Fact]
    public async Task Bearbeiten_FremdePositionsId_Abgelehnt()
    {
        var fx = new Fx();
        fx.Standard();
        var a = await fx.Anlegen();

        var r = await fx.Wf.EntwurfBearbeitenAsync(a.Id, Kopf(a), new Dictionary<int, decimal> { [999] = 1m }, new[] { 998 }, a.RowVersion);

        r.Fehler.Should().HaveCount(2).And.OnlyContain(f => f.Contains("gehört nicht zu"));
        a.Positionen.Should().HaveCount(3);
    }

    [Fact]
    public async Task Bemerkung_ZuLang_Abgelehnt()
    {
        var fx = new Fx();
        fx.Standard();
        var a = await fx.Anlegen();

        (await fx.Wf.BemerkungAendernAsync(a.Id, new string('x', 1001), a.RowVersion)).Fehler.Single().Should().Contain("1000");
        a.Bemerkung.Should().BeNull();
    }

    [Fact]
    public async Task UnbekannteId_Klartext()
    {
        var fx = new Fx();
        var rv = Array.Empty<byte>();

        foreach (var r in new[]
        {
            await fx.Wf.StatusWechselnAsync(123, BeschichtungsauftragStatus.Uebergeben, rv),
            await fx.Wf.BemerkungAendernAsync(123, "x", rv),
            await fx.Wf.EntwurfBearbeitenAsync(123, new KopfDaten(null, null, null, null), new Dictionary<int, decimal>(), Array.Empty<int>(), rv),
            await fx.Wf.PositionenHinzufuegenAsync(123, new[] { new PositionsSchluessel(1, 0, 1) }, rv),
            await fx.Wf.ReklamationAnlegenAsync(123, new[] { 1 }),
        })
            r.Fehler.Single().Should().Contain("nicht gefunden");
        (await fx.Wf.GetHinzufuegbarePositionenAsync(123)).Should().BeEmpty();
    }

    // ===== Hinzufuegen im Erst-Entwurf (AK 8f) =====

    [Fact]
    public async Task Hinzufuegen_ErstEntwurf_NeuBeschichtetePosition_MitSnapshotUndAudit()
    {
        var fx = new Fx();
        fx.Standard();
        var a = await fx.Anlegen();
        var neu = fx.Node(100, 0, 5, soll: 7m, artnr: "A5");          // „nach der Anlage in Sage neu beschichtet"
        fx.Time.Now = fx.Time.Now.AddMinutes(5);

        var r = await fx.Wf.PositionenHinzufuegenAsync(a.Id, new[] { PositionsSchluessel.Von(neu) }, a.RowVersion);

        r.Fehler.Should().BeEmpty();
        var p5 = a.Positionen.Single(p => p.Position == 5);
        (p5.Menge, p5.Artnr, p5.Matchcode, p5.Breite, p5.CreatedBy).Should().Be((7m, "A5", "MC-A5", 500m, "Tester"));
        a.ModifiedAt.Should().Be(fx.Time.Now.DateTime);
    }

    [Fact]
    public async Task Hinzufuegen_ErstEntwurf_Ablehnungen_NichtsGeaendert()
    {
        var fx = new Fx();
        fx.Standard();
        fx.Node(300, 0, 9, artnr: "Z", hauptFa: 999);                 // gehoert zu anderem HauptFA
        var a = await fx.Anlegen();
        var unbeschichtet = new PositionsSchluessel(100, 0, 4);
        var drin = new PositionsSchluessel(100, 0, 1);
        var neu = PositionsSchluessel.Von(fx.Node(100, 0, 5));
        var fremd = new PositionsSchluessel(300, 0, 9);
        var weg = new PositionsSchluessel(100, 0, 77);

        async Task Abgelehnt(IReadOnlyCollection<PositionsSchluessel> keys, string text)
        {
            var r = await fx.Wf.PositionenHinzufuegenAsync(a.Id, keys, a.RowVersion);
            r.Erfolg.Should().BeFalse();
            r.Fehler.Should().Contain(f => f.Contains(text));
            a.Positionen.Should().HaveCount(3);
        }

        await Abgelehnt(Array.Empty<PositionsSchluessel>(), "keine Positionen ausgewählt");
        await Abgelehnt(new[] { unbeschichtet }, "nicht beschichtet");
        await Abgelehnt(new[] { drin }, "bereits in diesem Auftrag");
        await Abgelehnt(new[] { fremd }, "nicht im Cache");
        await Abgelehnt(new[] { weg }, "nicht im Cache");
        await Abgelehnt(new[] { neu, unbeschichtet }, "nicht beschichtet");   // teilweise gueltig -> ganz abgelehnt

        var b = await fx.Anlegen();                                           // nimmt die freie Pos 5
        await Abgelehnt(new[] { neu }, b.Nummer);                             // vergeben, mit Auftragsnummer

        await fx.Status(a, BeschichtungsauftragStatus.Uebergeben);
        await Abgelehnt(new[] { neu }, "nur im Entwurf");
    }

    // ===== Reklamation (AK 8c, 8d, 8e, 8g, 8h) =====

    [Fact]
    public async Task Reklamation_AusErledigt_KopfVomUrsprung_NurAuswahl_UrsprungBleibtErledigt()
    {
        var fx = new Fx();
        fx.Standard();
        var u = await fx.Erledigt();
        (await fx.Wf.BemerkungAendernAsync(u.Id, "Ursprungsbemerkung", u.RowVersion)).Fehler.Should().BeEmpty();
        fx.Db.FaHierarchyOrderInfos.RemoveRange(fx.Db.FaHierarchyOrderInfos);  // PPS-View hat jetzt nichts / anderes
        fx.Info("Ganz Anders AG", "RAL 1000");
        var p1 = u.Positionen.Single(p => p.Position == 1);
        p1.Menge = 9m; await fx.Db.SaveChangesAsync();                        // Snapshot kommt aus dem Ursprung

        var r = await fx.Wf.ReklamationAnlegenAsync(u.Id, new[] { p1.Id });

        r.Fehler.Should().BeEmpty();
        var rek = r.Auftrag!;
        (rek.UrsprungsauftragId, rek.HauptFA, rek.Status).Should().Be((u.Id, HauptFa, BeschichtungsauftragStatus.Entwurf));
        (rek.Dienstleister, rek.Ral, rek.StartBeschichtung, rek.BeschichtenRetour)
            .Should().Be((u.Dienstleister, u.Ral, u.StartBeschichtung, u.BeschichtenRetour));
        rek.Bemerkung.Should().BeNull();
        rek.Positionen.Should().ContainSingle().Which.Menge.Should().Be(9m);
        rek.Nummer.Should().Be("BA-2026-00002");
        u.Status.Should().Be(BeschichtungsauftragStatus.Erledigt);
        (await fx.Db.Beschichtungsauftraege.Include(a => a.Folgeauftraege).SingleAsync(a => a.Id == u.Id))
            .Folgeauftraege.Select(f => f.Id).Should().Equal(rek.Id);

        (await fx.Wf.EntwurfBearbeitenAsync(rek.Id, Kopf(rek) with { Dienstleister = "Neu" }, new Dictionary<int, decimal>(), Array.Empty<int>(), rek.RowVersion))
            .Fehler.Should().BeEmpty();
    }

    [Theory]
    [InlineData(BeschichtungsauftragStatus.Entwurf)]
    [InlineData(BeschichtungsauftragStatus.Uebergeben)]
    [InlineData(BeschichtungsauftragStatus.Storniert)]
    public async Task Reklamation_NurAusErledigt(BeschichtungsauftragStatus status)
    {
        var fx = new Fx();
        fx.Standard();
        var u = await fx.Anlegen();
        if (status != BeschichtungsauftragStatus.Entwurf) await fx.Status(u, status);

        var r = await fx.Wf.ReklamationAnlegenAsync(u.Id, new[] { u.Positionen.First().Id });

        r.Fehler.Single().Should().Contain("nur aus „Erledigt\"");
        (await fx.Db.Beschichtungsauftraege.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Reklamation_LeereAuswahl_UndFremdePositionsId_Abgelehnt()
    {
        var fx = new Fx();
        fx.Standard();
        fx.Standard(2);
        var u = await fx.Erledigt();
        var anderer = await fx.Anlegen(2);

        (await fx.Wf.ReklamationAnlegenAsync(u.Id, Array.Empty<int>())).Fehler.Single().Should().Contain("keine Positionen ausgewählt");
        (await fx.Wf.ReklamationAnlegenAsync(u.Id, new[] { u.Positionen.First().Id, anderer.Positionen.First().Id }))
            .Fehler.Single().Should().Contain("gehört nicht zu");
        (await fx.Db.Beschichtungsauftraege.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task Hinzufuegen_ReklamationsEntwurf_NurUrsprungspositionen_SnapshotAusUrsprung_OhneCache()
    {
        var fx = new Fx();
        fx.Standard();
        var u = await fx.Erledigt();
        var ids = u.Positionen.OrderBy(p => p.Position).Select(p => p.Id).ToList();
        var rek = (await fx.Wf.ReklamationAnlegenAsync(u.Id, new[] { ids[0] })).Auftrag!;
        var zweite = (await fx.Wf.ReklamationAnlegenAsync(u.Id, new[] { ids[1] })).Auftrag!;  // offene zweite Reklamation mit Pos 2
        fx.Db.FaHierarchyNodes.RemoveRange(fx.Db.FaHierarchyNodes);                         // HauptFA aus dem Cache verschwunden
        await fx.Db.SaveChangesAsync();
        var freieNeue = new PositionsSchluessel(100, 0, 8);

        (await fx.Wf.GetHinzufuegbarePositionenAsync(rek.Id)).Select(k => k.Schluessel.Position).Should().BeEquivalentTo(new int?[] { 2, 3 });

        var r = await fx.Wf.PositionenHinzufuegenAsync(rek.Id, new[] { new PositionsSchluessel(100, 0, 2) }, rek.RowVersion);
        r.Fehler.Should().BeEmpty("Pos 2 steckt zwar in einer anderen offenen Reklamation — Reklamations-Pfade pruefen keine Vergabe");
        rek.Positionen.Single(p => p.Position == 2).Menge.Should().Be(3m);
        zweite.Positionen.Should().ContainSingle();

        foreach (var (keys, text) in new (PositionsSchluessel[], string)[]
        {
            (new[] { freieNeue }, "nur Positionen des Ursprungs"),
            (new[] { new PositionsSchluessel(100, 0, 1) }, "bereits in diesem Auftrag"),
            (Array.Empty<PositionsSchluessel>(), "keine Positionen ausgewählt"),
            (new[] { new PositionsSchluessel(100, 200, 3), freieNeue }, "nur Positionen des Ursprungs"),
        })
        {
            var abgelehnt = await fx.Wf.PositionenHinzufuegenAsync(rek.Id, keys, rek.RowVersion);
            abgelehnt.Fehler.Should().Contain(f => f.Contains(text));
            rek.Positionen.Should().HaveCount(2);
        }
    }

    [Fact]
    public async Task Reklamation_EinerReklamation_UndZweiteReklamation_ErstauftragNimmtPositionNicht()
    {
        var fx = new Fx();
        fx.Standard();
        var u = await fx.Erledigt();
        var p1 = u.Positionen.Single(p => p.Position == 1).Id;

        var rek1 = (await fx.Wf.ReklamationAnlegenAsync(u.Id, new[] { p1 })).Auftrag!;
        var rek2 = await fx.Wf.ReklamationAnlegenAsync(u.Id, new[] { p1 });               // zweite, erste noch Entwurf
        rek2.Fehler.Should().BeEmpty();
        await fx.Status(rek1, BeschichtungsauftragStatus.Uebergeben);
        await fx.Status(rek1, BeschichtungsauftragStatus.Erledigt);
        var rekVonRek = await fx.Wf.ReklamationAnlegenAsync(rek1.Id, new[] { rek1.Positionen.Single().Id });
        rekVonRek.Fehler.Should().BeEmpty();
        rekVonRek.Auftrag!.UrsprungsauftragId.Should().Be(rek1.Id);

        var erst = await fx.Wf.AnlegenAsync(HauptFa);
        erst.Erfolg.Should().BeFalse("alle Positionen stecken im Erledigt-Erstauftrag");
        foreach (var nr in new[] { u.Nummer, rek1.Nummer, rek2.Auftrag!.Nummer, rekVonRek.Auftrag.Nummer })
            erst.Fehler.Single().Should().Contain(nr);
    }

    // ===== Sync, Enum (AK 9, 11) =====

    [Fact]
    public async Task NachSimuliertemSync_AuftragUnveraendert()
    {
        var fx = new Fx();
        fx.Standard();
        var a = await fx.Anlegen();
        var vorher = a.Positionen.Select(p => (p.VaterFA, p.SubFA, p.Position, p.Artnr, p.Menge)).OrderBy(x => x.Position).ToList();

        fx.Db.FaHierarchyNodes.RemoveRange(fx.Db.FaHierarchyNodes);
        fx.Db.FaHierarchyOrderInfos.RemoveRange(fx.Db.FaHierarchyOrderInfos);
        await fx.Db.SaveChangesAsync();
        fx.Node(100, 0, 1, soll: 99m, artnr: "NEU");
        fx.Db.ChangeTracker.Clear();

        var geladen = await new BeschichtungsauftragRepository(fx.Db).GetMitPositionenAsync(a.Id);
        geladen!.Positionen.Select(p => (p.VaterFA, p.SubFA, p.Position, p.Artnr, p.Menge)).OrderBy(x => x.Position)
            .Should().Equal(vorher);
    }

    [Fact]
    public void Enum_KeinFreigegeben_Wert20Frei()
    {
        Enum.GetValues<BeschichtungsauftragStatus>().Select(v => (byte)v).Should().Equal(10, 30, 40, 90);
        Enum.GetNames<BeschichtungsauftragStatus>().Should().NotContain(n => n.StartsWith("Freigegeben"));
    }

    private static KopfDaten Kopf(Beschichtungsauftrag a) => new(a.Dienstleister, a.Ral, a.StartBeschichtung, a.BeschichtenRetour);
}
