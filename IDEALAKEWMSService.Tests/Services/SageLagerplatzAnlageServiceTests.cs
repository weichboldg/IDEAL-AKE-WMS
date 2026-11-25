using System.Data;
using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Models;
using IdealAkeWms.Services.SyncLogger;
using IdealAkeWms.Tests.Helpers;
using IDEALAKEWMSService.Services;
using IDEALAKEWMSService.Workers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Anlage = IdealAkeWms.Services.SageLagerplatzAnlage;

namespace IDEALAKEWMSService.Tests.Services;

/// <summary>v1.99.71 Spec 2026-11-24-lagerplatz-anlage-wms-nach-sage F3/F3a/F5/S3/S7 – Sende-Lauf mit Fake-Sage (kein SQL gegen Sage).</summary>
public class SageLagerplatzAnlageServiceTests
{
    private sealed class FakeSage : ISageLagerplatzAnlage
    {
        public List<string> Aufrufe { get; } = new();
        public Func<string, int, int, int, SageLagerplatzAnlageErgebnis> Platz { get; set; } =
            (_, _, _, _) => new(SageLagerplatzAnlageArt.Angelegt, 4711, " Lager TB;Regal 1 ", null, false, true, 1.23456m, 2m, 3m, 4m, 5m, "AKESQL20/ake_TEST2026");
        public Func<string, bool, SageLagerplatzAnlageErgebnis> Lager { get; set; } =
            (_, _) => new(SageLagerplatzAnlageArt.Angelegt, 900, "Neu", null, SageDb: "AKESQL20/ake_TEST2026");
        public Exception? Wirft { get; set; }

        public Task<SageLagerplatzAnlageErgebnis> AnlegenAsync(string lagerkennung, int d1, int d2, int d3, CancellationToken ct)
        {
            Aufrufe.Add($"{lagerkennung};{d1};{d2};{d3}");
            if (Wirft is not null) throw Wirft;
            return Task.FromResult(Platz(lagerkennung, d1, d2, d3));
        }

        public Task<SageLagerplatzAnlageErgebnis> LagerAnlegenAsync(string lagerkennung, string bezeichnung, string lagertechnik, bool neuanlageErlaubt,
            CancellationToken ct)
        {
            Aufrufe.Add($"Lager {lagerkennung} {lagertechnik} {neuanlageErlaubt}");
            if (Wirft is not null) throw Wirft;
            return Task.FromResult(Lager(lagerkennung, neuanlageErlaubt));
        }
    }

    private readonly ApplicationDbContext _db = TestDbContextFactory.Create(Guid.NewGuid().ToString());
    private readonly FakeSage _sage = new();
    private readonly FakeSyncLogger _log = new();

    private SageLagerplatzAnlageService Svc() => new(_db, _sage, NullLogger<SageLagerplatzAnlageService>.Instance, _log);

    private StorageArea Lager(string kennung, string technik = "LTD3HR", bool mitOutbox = false)
    {
        var a = new StorageArea { Lagerkennung = kennung, Bezeichnung = "Lager " + kennung, Lagertechnik = technik, IstDispoLager = true, CreatedBy = "t", CreatedByWindows = "t" };
        _db.StorageAreas.Add(a);
        _db.SaveChanges();
        if (mitOutbox)
        {
            SageStammdatenOutbox.Vormerken(_db, SageStammdatenOutbox.TypLagerort, a, new { a.Id });
            _db.SaveChanges();
        }
        return a;
    }

    private StorageLocation Platz(StorageArea area, string code, bool aktiv = true, string source = StorageLocationSource.Manual)
    {
        var p = new StorageLocation
        {
            Code = code, BarcodeValue = code, Zone = area.Lagerkennung, StorageAreaId = area.Id, Source = source, IsActive = aktiv,
            SageUebergabeStatus = SageUebergabeStatus.Offen, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t",
        };
        _db.StorageLocations.Add(p);
        _db.SaveChanges();
        SageStammdatenOutbox.Vormerken(_db, SageStammdatenOutbox.TypLagerplatz, p, new { p.Id });
        _db.SaveChanges();
        return p;
    }

    private SageStammdatenOutboxItem Outbox(string typ, int id) =>
        _db.SageStammdatenOutbox.Single(o => o.EntityType == typ && o.EntityKey == id.ToString());

    [Fact]
    public async Task OhneArbeit_KeinLauf_KeinAufruf()
    {
        (await Svc().RunOnceAsync(10, Anlage.StufeAlle, null)).Should().BeNull();
        _log.Runs.Should().BeEmpty();
        _sage.Aufrufe.Should().BeEmpty();
    }

    [Fact]
    public async Task Platz_Angelegt_Uebernommen_SageWerte_OutboxBestaetigt_EinmalSageDb()
    {
        var tb = Lager("TB");
        var p1 = Platz(tb, "TB;1;1;1");
        var p2 = Platz(tb, "TB;1;1;2");
        _sage.Platz = (l, a, b, c) => c == 2
            ? new(SageLagerplatzAnlageArt.Vorhanden, 12, "x", null, SageDb: "AKESQL20/ake_TEST2026")
            : new(SageLagerplatzAnlageArt.Angelegt, 4711, " Lager TB;Regal 1 ", null, false, true, 1.235m, 2m, 3m, 4m, 5m, "AKESQL20/ake_TEST2026");

        (await Svc().RunOnceAsync(10, null, null)).Should().BeNull();

        _sage.Aufrufe.Should().Equal("TB;1;1;1", "TB;1;1;2");
        (p1.Source, p1.SageLagerplatzId, p1.SageLagerkennung, p1.Description, p1.SageUebergabeStatus, p1.SageUebergabeFehler)
            .Should().Be((StorageLocationSource.Sage, (int?)4711, "TB;1;1;1", "Lager TB;Regal 1", SageUebergabeStatus.Uebergeben, (string?)null));
        (p1.SageIstGesperrt, p1.SageIstGesperrtInventur, p1.SageLaenge, p1.SageTragkraft).Should().Be(((bool?)false, (bool?)true, (decimal?)1.235m, (decimal?)5m));
        p1.SageUebergabeAm.Should().NotBeNull();
        p1.ModifiedBy.Should().Be(Anlage.ServiceActor);
        p2.SageLagerplatzId.Should().Be(12);
        var o = Outbox(SageStammdatenOutbox.TypLagerplatz, p1.Id);
        (o.Status, o.AttemptCount, o.ModifiedBy).Should().Be((SageBookingQueueStatus.Bestaetigt, 1, Anlage.ServiceActor));

        var run = _log.Runs.Single();
        run.ServiceName.Should().Be(SyncLogServices.SageLagerplatzAnlage);
        run.Events.Count(e => e.Message.StartsWith("Sage-Datenbank:")).Should().Be(1);
        run.Events.Should().Contain(e => e.Message == "Sage-Datenbank: AKESQL20/ake_TEST2026");
        run.Events.Should().Contain(e => e.Message == "Lagerplatz TB;1;1;1 in Sage angelegt (PlatzID 4711)");
        run.Events.Should().Contain(e => e.Message == "Lagerplatz TB;1;1;2 war in Sage schon vorhanden (PlatzID 12) – übernommen");
        run.FinishedSuccess.Should().BeTrue();
        (run.FinalCounts!["angelegt"], run.FinalCounts["vorhanden"], run.FinalCounts["fehler"]).Should().Be((1, 1, 0));
    }

    [Fact]
    public async Task Platz_FachlicherFehler_FehlerAmEintrag_WeiterMitNaechstem_KeinTechnischerText()
    {
        var tb = Lager("TB");
        var p1 = Platz(tb, "TB;1;0;0");
        var p2 = Platz(tb, "TB;2;0;0");
        _sage.Platz = (_, a, _, _) => a == 1
            ? SageLagerplatzAnlageErgebnis.Abgelehnt("Sage-Fehler 50014: Konflikt in Sage")
            : new(SageLagerplatzAnlageArt.Angelegt, 5, "b", null);

        (await Svc().RunOnceAsync(10, null, null)).Should().BeNull("fachlich = keine Mail");

        (p1.SageUebergabeStatus, p1.SageUebergabeFehler).Should().Be((SageUebergabeStatus.Fehler, "Sage-Fehler 50014: Konflikt in Sage"));
        var o1 = Outbox(SageStammdatenOutbox.TypLagerplatz, p1.Id);
        (o1.Status, o1.LastError).Should().Be((SageBookingQueueStatus.Fehler, "Sage-Fehler 50014: Konflikt in Sage"));
        p2.SageUebergabeStatus.Should().Be(SageUebergabeStatus.Uebergeben);
        _log.Runs.Single().Events.Should().Contain(e => e.Level == "Warning" && e.Message.Contains("TB;1;0;0"));
        _log.Runs.Single().FinalCounts!["fehler"].Should().Be(1);

        _sage.Aufrufe.Clear();
        await Svc().RunOnceAsync(10, null, null);
        _sage.Aufrufe.Should().BeEmpty("kein automatisches Wiederholen eines fachlichen Fehlers");
    }

    [Fact]
    public async Task Platz_CodeOhneMuster_FachlicherFehler_NichtsGesendet()
    {
        var tb = Lager("TB");
        var p = Platz(tb, "TB-01");

        await Svc().RunOnceAsync(10, null, null);

        _sage.Aufrufe.Should().BeEmpty();
        p.SageUebergabeStatus.Should().Be(SageUebergabeStatus.Fehler);
        p.SageUebergabeFehler.Should().Contain("Code passt nicht zum Muster TB;D1;D2;D3");
    }

    [Fact]
    public async Task Technisch_EintragZurueckAufOffen_RestUebersprungen_MeldungNurBeimWechsel()
    {
        var tb = Lager("TB");
        var p1 = Platz(tb, "TB;1;0;0");
        Platz(tb, "TB;2;0;0");
        _sage.Wirft = new TimeoutException("Zeitüberschreitung");

        var t1 = await Svc().RunOnceAsync(10, null, null);

        t1.Should().Be("Letzter Versuch fehlgeschlagen: Lagerplatz TB;1;0;0: Zeitüberschreitung");
        _sage.Aufrufe.Should().HaveCount(1, "Rest des Takts übersprungen");
        (p1.SageUebergabeStatus, p1.SageUebergabeFehler).Should().Be((SageUebergabeStatus.Offen, t1));
        var o = Outbox(SageStammdatenOutbox.TypLagerplatz, p1.Id);
        (o.Status, o.AttemptCount, o.LastError).Should().Be((SageBookingQueueStatus.Offen, 1, t1));
        _log.Runs.Single().FinishedFailed.Should().BeTrue();

        // 10x gleicher Text -> kein weiterer Lauf
        for (var i = 0; i < 10; i++)
            (await Svc().RunOnceAsync(10, null, t1)).Should().Be(t1);
        _log.Runs.Should().HaveCount(1);

        // anderer Text -> neuer Lauf
        _sage.Wirft = new IOException("Netz weg");
        (await Svc().RunOnceAsync(10, null, t1)).Should().Contain("Netz weg");
        _log.Runs.Should().HaveCount(2);

        // Erholung: kein technischer Fehler -> null (Worker setzt Merker zurueck)
        _sage.Wirft = null;
        (await Svc().RunOnceAsync(10, null, t1)).Should().BeNull();
        p1.SageUebergabeStatus.Should().Be(SageUebergabeStatus.Uebergeben);
    }

    [Theory]
    [InlineData("Neu", null, true)]
    [InlineData("Alt", "Alt", false)]
    [InlineData(null, null, false)]
    [InlineData("Neu", "Alt", true)]
    public void Worker_MeldetNurBeimWechsel(string? technisch, string? zuletzt, bool erwartet) =>
        SageBookingWorker.MeldenBeimWechsel(technisch, zuletzt).Should().Be(erwartet);

    [Fact]
    public async Task InaktiverPlatz_UndSagePlatz_NichtGesendet_KeinLauf()
    {
        var tb = Lager("TB");
        Platz(tb, "TB;1;0;0", aktiv: false);
        Platz(tb, "TB;2;0;0", source: StorageLocationSource.Sage);

        (await Svc().RunOnceAsync(10, null, null)).Should().BeNull();
        _sage.Aufrufe.Should().BeEmpty();
        _log.Runs.Should().BeEmpty();
    }

    [Fact]
    public async Task HaengendGesendet_NachStuckMinuten_ErneutVerarbeitet_FrischNicht()
    {
        var tb = Lager("TB");
        var p = Platz(tb, "TB;1;0;0");
        var o = Outbox(SageStammdatenOutbox.TypLagerplatz, p.Id);
        o.Status = SageBookingQueueStatus.Gesendet;
        o.ModifiedAt = DateTime.Now.AddMinutes(-2);
        _db.SaveChanges();

        await Svc().RunOnceAsync(10, null, null);
        _sage.Aufrufe.Should().BeEmpty("noch nicht hängend");

        o.ModifiedAt = DateTime.Now.AddMinutes(-11);
        _db.SaveChanges();
        await Svc().RunOnceAsync(10, null, null);
        _sage.Aufrufe.Should().Equal("TB;1;0;0");
        o.Status.Should().Be(SageBookingQueueStatus.Bestaetigt);
    }

    [Fact]
    public async Task Lager_ZuerstAngelegt_DannSeinePlaetze_ImSelbenTakt()
    {
        var neu = Lager("BL1", "LTD1BL", mitOutbox: true);
        Platz(neu, "BL1;1;0;0");

        await Svc().RunOnceAsync(10, Anlage.StufeMitschnitt, null);

        _sage.Aufrufe.Should().Equal("Lager BL1 LTD1BL True", "BL1;1;0;0");
        Outbox(SageStammdatenOutbox.TypLagerort, neu.Id).Status.Should().Be(SageBookingQueueStatus.Bestaetigt);
        var run = _log.Runs.Single();
        run.Events.Should().Contain(e => e.Message == "Lager BL1 in Sage angelegt (Basisplatz PlatzID 900)");
        (run.FinalCounts!["lager_angelegt"], run.FinalCounts["angelegt"]).Should().Be((1, 1));
        neu.Lagerkennung.Should().Be("BL1");
    }

    [Fact]
    public async Task Lager_TechnikNichtInStufe_NeuanlageNichtErlaubt_Uebergeben()
    {
        var neu = Lager("HR1", "LTD3HR", mitOutbox: true);
        _sage.Lager = (_, erlaubt) => erlaubt
            ? new(SageLagerplatzAnlageArt.Angelegt, 1, "x", null)
            : SageLagerplatzAnlageErgebnis.Abgelehnt("Sage-Fehler 50006: Lagertechnik fuer die Lager-Anlage an diesem Standort nicht freigegeben");

        await Svc().RunOnceAsync(10, Anlage.StufeMitschnitt, null);

        _sage.Aufrufe.Should().Equal("Lager HR1 LTD3HR False");
        var o = Outbox(SageStammdatenOutbox.TypLagerort, neu.Id);
        (o.Status, o.LastError).Should().Be((SageBookingQueueStatus.Fehler, "Sage-Fehler 50006: Lagertechnik fuer die Lager-Anlage an diesem Standort nicht freigegeben"));
        _log.Runs.Single().FinalCounts!["lager_fehler"].Should().Be(1);
    }

    [Fact]
    public async Task Lager_Fehler_PlaetzeWarten_ZaehlenNichtAlsArbeit()
    {
        var neu = Lager("BL1", "LTD1BL", mitOutbox: true);
        var p = Platz(neu, "BL1;1;0;0");
        _sage.Lager = (_, _) => SageLagerplatzAnlageErgebnis.Abgelehnt("Sage-Fehler 50003: anders belegt");

        await Svc().RunOnceAsync(10, Anlage.StufeAlle, null);

        _sage.Aufrufe.Should().Equal("Lager BL1 LTD1BL True");
        p.SageUebergabeStatus.Should().Be(SageUebergabeStatus.Offen, "wartet auf das Lager");
        Outbox(SageStammdatenOutbox.TypLagerplatz, p.Id).AttemptCount.Should().Be(0);
        _log.Runs.Single().FinalCounts!["uebersprungen"].Should().Be(1);

        _sage.Aufrufe.Clear();
        (await Svc().RunOnceAsync(10, Anlage.StufeAlle, null)).Should().BeNull();
        _sage.Aufrufe.Should().BeEmpty();
        _log.Runs.Should().HaveCount(1, "Lager im Fehler + wartende Plätze = keine Arbeit, kein Lauf");
    }

    [Fact]
    public async Task Lager_TechnischerFehler_RestDesTaktsUebersprungen()
    {
        var neu = Lager("BL1", "LTD1BL", mitOutbox: true);
        Platz(neu, "BL1;1;0;0");
        var tb = Lager("TB");
        Platz(tb, "TB;1;0;0");
        _sage.Wirft = new InvalidOperationException("Verbindung nicht möglich");

        var t = await Svc().RunOnceAsync(10, Anlage.StufeAlle, null);

        t.Should().Be("Letzter Versuch fehlgeschlagen: Lager BL1: Verbindung nicht möglich");
        _sage.Aufrufe.Should().HaveCount(1);
        Outbox(SageStammdatenOutbox.TypLagerort, neu.Id).Status.Should().Be(SageBookingQueueStatus.Offen);
    }

    [Fact]
    public async Task InaktivesLager_NichtGesendet()
    {
        var neu = Lager("BL1", "LTD1BL", mitOutbox: true);
        neu.IsActive = false;
        _db.SaveChanges();

        await Svc().RunOnceAsync(10, Anlage.StufeAlle, null);
        _sage.Aufrufe.Should().BeEmpty();
    }

    // ----- SqlSageLagerplatzAnlage: reine Funktionen und SQL-Text (kein SQL gegen Sage) -----

    [Theory]
    [InlineData(-2, 11, true)]     // Timeout
    [InlineData(1205, 13, true)]   // Deadlock-Opfer (DEADLOCK_PRIORITY LOW)
    [InlineData(1222, 16, true)]
    [InlineData(229, 14, true)]    // Recht fehlt
    [InlineData(230, 14, true)]
    [InlineData(262, 14, true)]
    [InlineData(4060, 11, true)]
    [InlineData(18456, 14, true)]
    [InlineData(233, 20, true)]
    [InlineData(10053, 20, true)]
    [InlineData(10054, 20, true)]
    [InlineData(10060, 20, true)]
    [InlineData(53, 20, true)]
    [InlineData(-1, 20, true)]
    [InlineData(4000, 20, true)]   // Class >= 20
    [InlineData(50014, 16, false)] // THROW fachlich
    [InlineData(50001, 16, false)]
    [InlineData(515, 16, false)]
    [InlineData(207, 16, false)]
    [InlineData(208, 16, false)]
    [InlineData(2812, 16, false)]
    [InlineData(8152, 16, false)]
    [InlineData(2627, 14, false)]
    [InlineData(547, 16, false)]
    [InlineData(245, 16, false)]
    public void IstTechnisch_Fehlerklassen(int number, byte klasse, bool technisch) =>
        SqlSageLagerplatzAnlage.IstTechnisch(number, klasse).Should().Be(technisch);

    [Fact]
    public void IstTechnisch_NichtSqlException()
    {
        SqlSageLagerplatzAnlage.IstTechnisch(new InvalidOperationException()).Should().BeTrue();
        SqlSageLagerplatzAnlage.IstTechnisch(new IOException()).Should().BeTrue();
        SqlSageLagerplatzAnlage.IstTechnisch(new TimeoutException()).Should().BeTrue();
        SqlSageLagerplatzAnlage.IstTechnisch(new ArgumentException()).Should().BeFalse();
    }

    [Fact]
    public void Klartext_Zusaetze()
    {
        SqlSageLagerplatzAnlage.Klartext(515, "NULL nicht zulässig", null).Should().EndWith(SqlSageLagerplatzAnlage.AbweichungZusatz);
        SqlSageLagerplatzAnlage.Klartext(2812, "spSysTan fehlt", null).Should().Contain("P-1 bis P-3");
        SqlSageLagerplatzAnlage.Klartext(50012, "Koordinaten passen nicht zur Lagertechnik", "Regal, Ebene, Platz")
            .Should().Be("Sage-Fehler 50012: Koordinaten passen nicht zur Lagertechnik (Dimensionen der Lagertechnik: Regal, Ebene, Platz; genutzte ab 1, nicht genutzte 0)");
        SqlSageLagerplatzAnlage.Klartext(50014, "Konflikt", null).Should().Be("Sage-Fehler 50014: Konflikt");
    }

    [Fact]
    public void SqlText_PlatzBefehl_Pflichtteile_UndTypisierteParameter()
    {
        var sql = SqlSageLagerplatzAnlage.PlatzSql;
        sql.Should().StartWith("SET XACT_ABORT ON;").And.Contain("SET DEADLOCK_PRIORITY LOW;")
            .And.Contain("WITH (UPDLOCK, HOLDLOCK)").And.Contain("EXEC spSysTan @Table = 'KHKLagerplaetze'")
            .And.Contain("THROW 50014, @Msg, 1;").And.Contain("STRING_AGG(CONCAT('PlatzID '").And.Contain("IstPlatzverwaltet = -1");
        sql.IndexOf("COMMIT;", StringComparison.Ordinal).Should().BeLessThan(sql.LastIndexOf("SELECT p.PlatzID", StringComparison.Ordinal), "Ergebnis erst nach COMMIT (S5)");
        sql.Should().NotContain("nvarchar").And.NotContain("MAX(PlatzID)");

        using var cmd = SqlSageLagerplatzAnlage.PlatzBefehl(null, "TB", 1, 2, 3);
        cmd.Parameters["@Lager"].SqlDbType.Should().Be(SqlDbType.VarChar);
        cmd.Parameters["@Lager"].Size.Should().Be(10);
        cmd.Parameters["@D3"].Value.Should().Be(3);
        cmd.Parameters["@Mandant"].SqlDbType.Should().Be(SqlDbType.SmallInt);
        cmd.Parameters["@Mandant"].Value.Should().Be((short)1);
    }

    [Fact]
    public void SqlText_LagerBefehl_OhneUserSpalten_SwfUndNeuanlageSchalter()
    {
        var sql = SqlSageLagerplatzAnlage.LagerSql;
        sql.Should().StartWith("SET XACT_ABORT ON;").And.Contain("SET DEADLOCK_PRIORITY LOW;").And.Contain("'SWF'")
            .And.Contain("IF @NeuanlageErlaubt = 0").And.Contain("THROW 50006").And.Contain("THROW 50004").And.Contain("THROW 50007")
            .And.NotContain("USER_");
        sql.IndexOf("COMMIT;", StringComparison.Ordinal).Should().BeLessThan(sql.LastIndexOf("SELECT @PlatzID AS PlatzID", StringComparison.Ordinal));

        using var cmd = SqlSageLagerplatzAnlage.LagerBefehl(null, "BL1", "Blocklager", "LTD1BL", false);
        (cmd.Parameters["@Bez"].SqlDbType, cmd.Parameters["@Bez"].Size).Should().Be((SqlDbType.VarChar, 40));
        cmd.Parameters["@NeuanlageErlaubt"].SqlDbType.Should().Be(SqlDbType.Bit);
        cmd.Parameters["@NeuanlageErlaubt"].Value.Should().Be(false);
    }
}
