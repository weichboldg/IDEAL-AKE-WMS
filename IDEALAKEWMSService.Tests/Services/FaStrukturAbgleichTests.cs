using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using IDEALAKEWMSService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace IDEALAKEWMSService.Tests.Services;

/// <summary>v1.99.37 (Spec 2026-11-17-fa-struktur-abgleich-statt-full-replace, ADR 0032): reiner Planer + Abgleich im Sync (InMemory).
/// UNIQUE-Index und Spalten-Kollation am SQL Server: nur Manual-UAT (TS-158).</summary>
public class FaStrukturAbgleichTests
{
    // ----- Planer -----

    private static FaHierarchyNode N(int haupt, int? vater, int? pos, int sub, string? artnr = null, string? bez = "X") =>
        new() { HauptFA = haupt, VaterFA = vater, Position = pos, SubFA = sub, HauptArtnr = "G", Artnr = artnr, Bezeichnung1 = bez, Sollmenge = 1m };

    private static FaHierarchyOrderInfo I(int haupt, string? ab = "AB1", string? pos = "1", string? kunde = "K") =>
        new() { HauptFA = haupt, ABNr = ab, Pos = pos, Kunde = kunde };

    private static readonly IReadOnlySet<int> Keine = new HashSet<int>();

    [Fact]
    public void Planen_NeuPaareFehlend_ArtnrNachKollation()
    {
        var bestand = new List<FaHierarchyNode> { N(1, 1, 1, 0, "abc "), N(1, 1, 2, 0, "ALT") };
        var read = new List<FaHierarchyNode> { N(1, 1, 1, 0, "ABC"), N(1, 1, 3, 0, "NEU") };

        var p = FaStrukturAbgleich.Planen(bestand, read, FaStrukturAbgleich.Schluessel, n => n.HauptFA, Keine);

        p.Paare.Should().ContainSingle().Which.Alt.Artnr.Should().Be("abc ");
        p.Fehlend.Should().ContainSingle().Which.Artnr.Should().Be("ALT");
        p.Neu.Should().ContainSingle().Which.Artnr.Should().Be("NEU");
    }

    [Fact]
    public void Planen_LeerUndNullSindGleich()
    {
        var p = FaStrukturAbgleich.Planen(new List<FaHierarchyNode> { N(1, null, null, 1, null) }, new List<FaHierarchyNode> { N(1, null, null, 1, "  ") },
            FaStrukturAbgleich.Schluessel, n => n.HauptFA, Keine);
        p.Paare.Should().ContainSingle();
    }

    [Fact]
    public void Doppelt_SperrtDieHauptFa_PlanenLaesstSieUnberuehrt()
    {
        var read = new List<FaHierarchyNode> { N(1, 1, 1, 0, "A"), N(1, 1, 1, 0, "a"), N(2, 2, 1, 0, "B") };
        var (gesperrt, schluessel) = FaStrukturAbgleich.Doppelt(read, FaStrukturAbgleich.Schluessel, n => n.HauptFA);
        gesperrt.Should().BeEquivalentTo(new[] { 1 });
        schluessel.Should().ContainSingle();

        var p = FaStrukturAbgleich.Planen(new List<FaHierarchyNode> { N(1, 1, 9, 0, "Z") }, read, FaStrukturAbgleich.Schluessel, n => n.HauptFA, gesperrt);
        p.Fehlend.Should().BeEmpty("die gesperrte HauptFA wird nicht vermisst");
        p.Neu.Should().ContainSingle().Which.HauptFA.Should().Be(2);
    }

    [Theory]
    [InlineData(0, 5, true, null, null, null)]                       // Owner gelesen → Position in Sage entfernt
    [InlineData(0, 5, false, null, null, null)]                      // Owner in Sage nicht gefunden → entfernen
    [InlineData(0, 5, false, FaSageStatus.Beendet, null, "beendet")]
    [InlineData(0, 5, false, FaSageStatus.Inaktiv, null, "inaktiv")]
    [InlineData(0, 5, false, FaSageStatus.Offen, null, "unklar")]
    [InlineData(7, 5, true, null, FaSageStatus.Beendet, "beendet")]   // FA-Knoten: Sub-FA beendet geht vor „Owner gelesen“
    [InlineData(7, 5, true, null, FaSageStatus.Offen, null)]
    public void Entscheiden_Tabelle(int sub, int owner, bool ownerGelesen, FaSageStatus? ownerStatus, FaSageStatus? subStatus, string? erwartet)
    {
        var gelesen = ownerGelesen ? new HashSet<int> { owner } : new HashSet<int>();
        var status = new Dictionary<int, FaSageStatus>();
        if (ownerStatus is { } o) status[owner] = o;
        if (subStatus is { } s) status[sub] = s;
        FaStrukturAbgleich.Entscheiden(sub, owner, gelesen, status).Should().Be(erwartet);
    }

    [Fact]
    public void GrundText_NieNurFarbe()
    {
        FaStrukturGrund.Text(null).Should().BeNull();
        FaStrukturGrund.Text(FaStrukturGrund.Beendet).Should().Be("in Sage beendet");
        FaStrukturGrund.Text(FaStrukturGrund.Unklar).Should().Be("in Sage ungeklärt");
    }

    // ----- Abgleich im Sync (InMemory, eine Datenbank ueber mehrere Laeufe) -----

    private sealed class Reader : ISageFaStrukturReader
    {
        public Func<FaStrukturLesung> Lesung { get; set; } = () => new(new(), new(), 0, 0);
        public Dictionary<int, FaSageStatus>? Status { get; set; }
        public Exception? StatusFehler { get; set; }
        public List<int> Gefragt { get; } = new();
        public Task<FaStrukturLesung> LesenAsync(CancellationToken ct = default) => Task.FromResult(Lesung());

        public Task<IReadOnlyDictionary<int, FaSageStatus>> FaStatusLesenAsync(IReadOnlyCollection<int> belIds, CancellationToken ct = default)
        {
            Gefragt.AddRange(belIds);
            if (StatusFehler is not null) throw StatusFehler;
            return Task.FromResult<IReadOnlyDictionary<int, FaSageStatus>>(Status is null
                ? belIds.ToDictionary(i => i, _ => FaSageStatus.Offen)
                : belIds.Where(Status.ContainsKey).ToDictionary(i => i, i => Status[i]));
        }
    }

    private sealed class Umgebung
    {
        private readonly DbContextOptions<ApplicationDbContext> _opt =
            new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase("abgleich" + Guid.NewGuid()).Options;
        public Reader Reader { get; } = new();
        public Mock<ISyncErrorNotifier> Notifier { get; } = new();
        public FakeSyncLogger Logger { get; } = new();
        public ApplicationDbContext Db() => new TestApplicationDbContext(_opt);

        public async Task<FakeSyncRun> LaufAsync(bool dryRun = false)
        {
            await using var db = Db();
            await new FaHierarchySyncService(db, Reader, Notifier.Object, NullLogger<FaHierarchySyncService>.Instance, Logger).RunAsync(dryRun);
            return Logger.Runs.Last();
        }
    }

    /// <summary>HauptFA 100 (Wurzel, Unter-FA 101, Blatt), HauptFA 200 (Wurzel + Blatt), je ein Auftragskopf.</summary>
    private static FaStrukturLesung Zwei(Func<List<FaHierarchyNode>, List<FaHierarchyNode>>? knoten = null,
        Func<List<FaHierarchyOrderInfo>, List<FaHierarchyOrderInfo>>? infos = null)
    {
        var k = new List<FaHierarchyNode>
        {
            N(100, null, null, 100), N(100, 100, 1, 101, "BG"), N(100, 101, 1, 0, "BLATT"),
            N(200, null, null, 200), N(200, 200, 1, 0, "B2"),
        };
        var i = new List<FaHierarchyOrderInfo> { I(100), I(200, "AB2") };
        k = knoten?.Invoke(k) ?? k;
        i = infos?.Invoke(i) ?? i;
        return new(k, i, k.Count, i.Count);
    }

    [Fact]
    public async Task ErsterLauf_LegtAn_MitLebenslauf_ZweiterLauf_GleicheIds_NichtsGeaendert()
    {
        var u = new Umgebung { Reader = { Lesung = () => Zwei() } };

        (await u.LaufAsync()).FinalCounts!["neu"].Should().Be(7);
        int[] ids;
        await using (var db = u.Db())
        {
            var n = await db.FaHierarchyNodes.OrderBy(x => x.Id).ToListAsync();
            n.Should().OnlyContain(x => x.ErstGesehen != default && x.ZuletztGesehen == x.ErstGesehen && x.VerschwundenSeit == null);
            ids = n.Select(x => x.Id).ToArray();
        }

        var zweiter = await u.LaufAsync();

        zweiter.FinalCounts!["neu"].Should().Be(0);
        zweiter.FinalCounts["geaendert"].Should().Be(0);
        await using (var db = u.Db())
            (await db.FaHierarchyNodes.OrderBy(x => x.Id).Select(x => x.Id).ToArrayAsync()).Should().Equal(ids, "Id bleibt stabil");
    }

    [Fact]
    public async Task GeaenderterWert_ZaehltGeaendert_SetztSyncedAt()
    {
        var u = new Umgebung { Reader = { Lesung = () => Zwei() } };
        await u.LaufAsync();
        DateTime vorher;
        await using (var db = u.Db()) vorher = (await db.FaHierarchyNodes.SingleAsync(x => x.Artnr == "B2")).SyncedAt;

        u.Reader.Lesung = () => Zwei(k => { k.Single(x => x.Artnr == "B2").Bezeichnung1 = "neu"; return k; });
        await Task.Delay(5);
        (await u.LaufAsync()).FinalCounts!["geaendert"].Should().Be(1);

        await using (var db = u.Db())
        {
            var b2 = await db.FaHierarchyNodes.SingleAsync(x => x.Artnr == "B2");
            b2.Bezeichnung1.Should().Be("neu");
            b2.SyncedAt.Should().BeAfter(vorher);
        }
    }

    [Fact]
    public async Task PositionInSageEntfernt_OwnerGelesen_WirdEntfernt_OhneSageAbfrage()
    {
        var u = new Umgebung { Reader = { Lesung = () => Zwei() } };
        await u.LaufAsync();

        u.Reader.Lesung = () => Zwei(k => k.Where(x => x.Artnr != "B2").ToList());
        var lauf = await u.LaufAsync();

        lauf.FinalCounts!["entfernt_position"].Should().Be(1);
        u.Reader.Gefragt.Should().BeEmpty();
        await using var db = u.Db();
        (await db.FaHierarchyNodes.AnyAsync(x => x.Artnr == "B2")).Should().BeFalse();
    }

    [Fact]
    public async Task HauptFaVerschwunden_SageOffen_Unklar_MitWarnungUndMail_DannWiederDa()
    {
        var u = new Umgebung { Reader = { Lesung = () => Zwei() } };
        await u.LaufAsync();

        u.Reader.Lesung = () => Zwei(k => k.Where(x => x.HauptFA != 200).ToList(), i => i.Where(x => x.HauptFA != 200).ToList());
        var lauf = await u.LaufAsync();

        lauf.FinalCounts!["verschwunden_unklar"].Should().Be(3); // Wurzel + Blatt + Kopf
        lauf.Events.Should().Contain(e => e.Level == "Warning" && e.Message.Contains("ungeklärt"));
        u.Notifier.Verify(n => n.NotifyAsync("FaHierarchy-Sync: ungeklärt verschwundene Zeilen", It.IsAny<Exception>(), It.IsAny<CancellationToken>()), Times.Once);
        DateTime seit;
        await using (var db = u.Db())
        {
            var weg = await db.FaHierarchyNodes.Where(x => x.HauptFA == 200).ToListAsync();
            weg.Should().HaveCount(2).And.OnlyContain(x => x.VerschwundenGrund == FaStrukturGrund.Unklar && x.VerschwundenSeit != null);
            seit = weg[0].VerschwundenSeit!.Value;
        }

        (await u.LaufAsync()).FinalCounts!["verschwunden_unklar"].Should().Be(0, "erster Wert bleibt, keine neue Meldung");
        await using (var db = u.Db())
            (await db.FaHierarchyNodes.FirstAsync(x => x.HauptFA == 200)).VerschwundenSeit.Should().Be(seit);

        u.Reader.Lesung = () => Zwei();
        (await u.LaufAsync()).FinalCounts!["wieder_da"].Should().Be(3);
        await using (var db = u.Db())
            (await db.FaHierarchyNodes.AllAsync(x => x.VerschwundenSeit == null && x.VerschwundenGrund == null)).Should().BeTrue();
    }

    [Fact]
    public async Task SubFaBeendet_FaKnotenBleibtBeendet_ObwohlVaterGelesen()
    {
        var u = new Umgebung { Reader = { Lesung = () => Zwei() } };
        await u.LaufAsync();
        u.Reader.Status = new() { [101] = FaSageStatus.Beendet };

        u.Reader.Lesung = () => Zwei(k => k.Where(x => x.SubFA != 101 && x.VaterFA != 101).ToList());
        var lauf = await u.LaufAsync();

        // Knoten 100/1 → 101: Sub-FA beendet geht vor „Vater 100 gelesen“; Blatt unter 101: Owner 101 nicht gelesen, in Sage beendet.
        lauf.FinalCounts!["verschwunden_beendet"].Should().Be(2);
        lauf.FinalCounts["entfernt_position"].Should().Be(0);
        u.Reader.Gefragt.Should().BeEquivalentTo(new[] { 101 });
    }

    [Fact]
    public async Task OwnerInSageNichtGefunden_WirdEntfernt()
    {
        var u = new Umgebung { Reader = { Lesung = () => Zwei() } };
        await u.LaufAsync();
        u.Reader.Status = new();

        u.Reader.Lesung = () => Zwei(k => k.Where(x => x.HauptFA != 200).ToList(), i => i.Where(x => x.HauptFA != 200).ToList());
        var lauf = await u.LaufAsync();

        lauf.FinalCounts!["entfernt_in_sage"].Should().Be(3);
        await using var db = u.Db();
        (await db.FaHierarchyNodes.AnyAsync(x => x.HauptFA == 200)).Should().BeFalse();
        (await db.FaHierarchyOrderInfos.AnyAsync(x => x.HauptFA == 200)).Should().BeFalse();
    }

    [Fact]
    public async Task AllesVerschwunden_Gesperrt_NichtsMarkiert_NeueTrotzdemAngelegt_Mail()
    {
        var u = new Umgebung { Reader = { Lesung = () => Zwei() } };
        await u.LaufAsync();

        u.Reader.Lesung = () => new(new() { N(300, null, null, 300) }, new() { I(300, "AB3") }, 1, 1);
        var lauf = await u.LaufAsync();

        lauf.FinalCounts!["verschwunden_gesperrt"].Should().Be(1);
        lauf.FinalCounts["neu"].Should().Be(2);
        u.Reader.Gefragt.Should().BeEmpty();
        u.Notifier.Verify(n => n.NotifyAsync("FaHierarchy-Sync: Read ohne bisherige HauptFA", It.IsAny<Exception>(), It.IsAny<CancellationToken>()), Times.Once);
        await using var db = u.Db();
        (await db.FaHierarchyNodes.AllAsync(x => x.VerschwundenSeit == null)).Should().BeTrue();
        (await db.FaHierarchyNodes.CountAsync()).Should().Be(6);
    }

    [Fact]
    public async Task DoppelterSchluessel_HauptFaUnberuehrt_RestAbgeglichen_Mail()
    {
        var u = new Umgebung { Reader = { Lesung = () => Zwei() } };
        await u.LaufAsync();

        u.Reader.Lesung = () => Zwei(k =>
        {
            k.Add(N(100, 101, 1, 0, "blatt")); // gleicher Schluessel wie BLATT (Kollation)
            k.RemoveAll(x => x.Artnr == "BG");  // waere sonst verschwunden
            k.Single(x => x.Artnr == "B2").Bezeichnung1 = "neu";
            return k;
        });
        var lauf = await u.LaufAsync();

        lauf.FinalCounts!["schluessel_doppelt"].Should().Be(1);
        lauf.FinalCounts["geaendert"].Should().Be(1, "HauptFA 200 wird abgeglichen");
        u.Notifier.Verify(n => n.NotifyAsync("FaHierarchy-Sync: doppelte Schlüssel", It.IsAny<Exception>(), It.IsAny<CancellationToken>()), Times.Once);
        await using var db = u.Db();
        (await db.FaHierarchyNodes.CountAsync(x => x.HauptFA == 100 && x.VerschwundenSeit == null)).Should().Be(3, "HauptFA 100 unberuehrt");
    }

    [Fact]
    public async Task SageAbfrageFehler_NichtsMarkiertOderEntfernt_LaufErfolgreich_Mail()
    {
        var u = new Umgebung { Reader = { Lesung = () => Zwei() } };
        await u.LaufAsync();
        u.Reader.StatusFehler = new InvalidOperationException("Sage weg");

        u.Reader.Lesung = () => Zwei(k => k.Where(x => x.HauptFA != 200).ToList(), i => i.Where(x => x.HauptFA != 200).ToList());
        var lauf = await u.LaufAsync();

        lauf.FinishedSuccess.Should().BeTrue();
        lauf.FinalCounts!["sage_abfrage_fehler"].Should().Be(1);
        lauf.FinalCounts["verschwunden_unklar"].Should().Be(0);
        u.Notifier.Verify(n => n.NotifyAsync("FaHierarchy-Sync: Sage-Abfrage fehlgeschlagen", It.IsAny<Exception>(), It.IsAny<CancellationToken>()), Times.Once);
        await using var db = u.Db();
        (await db.FaHierarchyNodes.AllAsync(x => x.VerschwundenSeit == null)).Should().BeTrue();
    }

    // ----- E4: ProductionOrderPositions (nur wenn SData gelesen wurde) -----

    private static ProductionOrderPosition P(int belPosId, int belId, int struktur = 100, decimal menge = 1m) =>
        new() { BelPosID = belPosId, BelID = belId, StrukturID = struktur, Menge = menge };

    private static FaStrukturLesung MitPositionen(params ProductionOrderPosition[] p) => Zwei() with { Positionen = p.ToList() };

    [Fact]
    public async Task Positionen_Abgleich_NeuGeaendertEntfernt_IdStabil()
    {
        var u = new Umgebung { Reader = { Lesung = () => MitPositionen(P(1, 101), P(2, 101), P(3, 100)) } };
        (await u.LaufAsync()).FinalCounts!["positionen_neu"].Should().Be(3);
        int id1;
        await using (var db = u.Db()) id1 = (await db.ProductionOrderPositions.SingleAsync(x => x.BelPosID == 1)).Id;

        u.Reader.Lesung = () => MitPositionen(P(1, 101, menge: 5m), P(3, 100), P(4, 100));
        var lauf = await u.LaufAsync();

        lauf.FinalCounts!["positionen_neu"].Should().Be(1);
        lauf.FinalCounts["positionen_entfernt"].Should().Be(1, "BelPosID 2: Beleg 101 gelesen → in Sage entfernt");
        lauf.FinalCounts["geaendert"].Should().Be(1);
        await using var db2 = u.Db();
        var p1 = await db2.ProductionOrderPositions.SingleAsync(x => x.BelPosID == 1);
        (p1.Id, p1.Menge).Should().Be((id1, 5m));
        (await db2.ProductionOrderPositions.AnyAsync(x => x.BelPosID == 2)).Should().BeFalse();
    }

    [Fact]
    public async Task Positionen_LeereListe_TabelleUnveraendert_Warnung_KeineMail()
    {
        var u = new Umgebung { Reader = { Lesung = () => MitPositionen(P(1, 101)) } };
        await u.LaufAsync();

        u.Reader.Lesung = () => MitPositionen();
        var lauf = await u.LaufAsync();

        lauf.Events.Should().ContainSingle(e => e.Level == "Warning" && e.Message.Contains("keine Positionen"));
        u.Notifier.VerifyNoOtherCalls();
        await using var db = u.Db();
        (await db.ProductionOrderPositions.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Positionen_ViewBetrieb_Null_Unberuehrt_OhneCountKeys()
    {
        var u = new Umgebung { Reader = { Lesung = () => MitPositionen(P(1, 101)) } };
        await u.LaufAsync();

        u.Reader.Lesung = () => Zwei();
        var lauf = await u.LaufAsync();

        lauf.FinalCounts!.Keys.Should().NotContain("positionen_neu");
        await using var db = u.Db();
        (await db.ProductionOrderPositions.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Positionen_SchreibFehler_FehlerImProtokoll_Mail_StrukturGeschrieben_LaufErfolgreich()
    {
        var u = new Umgebung { Reader = { Lesung = () => MitPositionen(P(1, 101)) } };
        await u.LaufAsync();
        int id;
        await using (var db = u.Db()) id = (await db.ProductionOrderPositions.SingleAsync()).Id;

        // Neue Position mit belegter Id: der Tracker wirft beim Anlegen (am SQL Server waere es z. B. ein UNIQUE-Verstoss).
        u.Reader.Lesung = () => Zwei(k => { k.Single(x => x.Artnr == "B2").Bezeichnung1 = "neu"; return k; }) with
        {
            Positionen = new() { P(1, 101), new ProductionOrderPosition { Id = id, BelPosID = 9, BelID = 101, StrukturID = 100 } },
        };
        var lauf = await u.LaufAsync();

        lauf.FinishedSuccess.Should().BeTrue();
        lauf.Events.Should().ContainSingle(e => e.Level == "Error" && e.Message.StartsWith("ProductionOrderPositions nicht abgeglichen"));
        u.Notifier.Verify(n => n.NotifyAsync("FaHierarchy-Sync: FA-Positionen nicht abgeglichen", It.IsAny<Exception>(), It.IsAny<CancellationToken>()), Times.Once);
        await using var db2 = u.Db();
        (await db2.FaHierarchyNodes.SingleAsync(x => x.Artnr == "B2")).Bezeichnung1.Should().Be("neu");
    }

    [Fact]
    public async Task DryRun_RechnetWieEchterLauf_SchreibtNichts()
    {
        var u = new Umgebung { Reader = { Lesung = () => Zwei() } };
        await u.LaufAsync();
        u.Reader.Lesung = () => Zwei(k => k.Where(x => x.HauptFA != 200).ToList(), i => i.Where(x => x.HauptFA != 200).ToList());

        var lauf = await u.LaufAsync(dryRun: true);

        lauf.FinalMessageSuffix.Should().Be("[DryRun]");
        lauf.FinalCounts!["verschwunden_unklar"].Should().Be(3);
        await using var db = u.Db();
        (await db.FaHierarchyNodes.AllAsync(x => x.VerschwundenSeit == null)).Should().BeTrue();
    }
}
