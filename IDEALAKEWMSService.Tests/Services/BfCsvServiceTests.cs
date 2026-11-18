using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using IDEALAKEWMSService.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IDEALAKEWMSService.Tests.Services;

/// <summary>v1.99.35 (Spec 2026-11-13-blechfertigung-bf-auftraege-wicam-csv F4): Bytes gegen die Vorlage (Windows-1252, CRLF, nur uebergebene
/// Positionen), Kollision ohne Wiederholung, Wiederholung nach Abstand, nur Freigegeben. Echte Dateien in einem lokalen Temp-Ordner (kein Pool).
/// v1.99.42: Vorlage ist die echte WiCAM-Datei <c>TestData/BF00000002.csv</c> (Befund P-1 (2), „Zielform“). Sie stammt aus zwei Bestellern:
/// Zeilen 1-15 = <see cref="VorlageAuftrag"/> (WAK), Zeile 16 = <see cref="ZweiterAuftrag"/> (MAW).</summary>
public sealed class BfCsvServiceTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "wms-bfcsv-test-" + Guid.NewGuid().ToString("N"));
    private static readonly BfCsvWiederholung Wdh = new(3, 15);

    public BfCsvServiceTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
    }

    private static BfAuftragPosition P(int pos, string? matchcode, string? matnr, decimal menge, string? bemerkung, string? lp, string teileart = "FT",
        bool gestrichen = false, string? arbeitsschritte = null) => new()
    {
        Position = pos, ArticleNumber = "A" + pos, Matchcode = matchcode, Materialnummer = matnr, Menge = menge, Bemerkung = bemerkung,
        Hauptlagerplatz = lp, Teileart = teileart, Gestrichen = gestrichen, Arbeitsschritte = arbeitsschritte,
        CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t",
    };

    /// <summary>Genau der Auftrag hinter der Vorlage-Datei.</summary>
    public static BfAuftrag VorlageAuftrag() => new()
    {
        Nummer = "BF00000002", Status = BfAuftragStatus.Freigegeben, CsvStatus = BfCsvStatus.Offen, BestellerName = "Walter Kronberger",
        BestellerKuerzel = "WAK", Schachteltermin = new DateTime(2026, 11, 19), FeTermin = new DateTime(2026, 12, 2),
        CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t",
        Positionen =
        {
            P(2, "g-kt-e-lasto-h-bh-760-v02", "3.000", 232m, "EILT - 18.11", "K-02;0;0;0", arbeitsschritte: " ST EG KA "),
            P(1, "  a-z-filter-210-230-v01\t ", "1", 10m, "18.11 - 1", "K-02;0;0;0", "BG", arbeitsschritte: "S-01"),
            P(3, "OHNE NUMMER", null, 1m, "18.11", "L-3"),
            P(4, "GESTRICHEN", "9", 1m, "18.11", "L-4", gestrichen: true),
        },
    };

    /// <summary>Der Auftrag hinter Zeile 16 der Vorlage (anderer Besteller, Freigabe einen Tag frueher).</summary>
    public static BfAuftrag ZweiterAuftrag() => new()
    {
        Nummer = "BF00000003", Status = BfAuftragStatus.Freigegeben, BestellerName = "M. W.", BestellerKuerzel = "MAW",
        Schachteltermin = new DateTime(2026, 11, 18), FeTermin = new DateTime(2026, 12, 1),
        Positionen = { P(1, "S-F01-E-LOCHLEISTE-A-1300-L-V01", "1", 24m, "18.11", "H4-02;0;0;0", arbeitsschritte: "ST EG KA") },
    };

    private static byte[] VorlageDatei() => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "TestData", "BF00000002.csv"));

    /// <summary>Zeilen 1-15 der Vorlage (bis einschliesslich des 15. CRLF).</summary>
    private static byte[] Vorlage()
    {
        var b = VorlageDatei();
        int n = 0, i = 0;
        for (; i < b.Length && n < 15; i++) if (b[i] == 0x0A) n++;
        return b[..i];
    }

    private (ApplicationDbContext Db, BfAuftrag A, FakeSyncLogger Log, BfCsvService Svc) Fx(Action<BfAuftrag>? anpassen = null)
    {
        var db = TestDbContextFactory.Create();
        var a = VorlageAuftrag();
        anpassen?.Invoke(a);
        db.BfAuftraege.Add(a);
        db.SaveChanges();
        var log = new FakeSyncLogger();
        return (db, a, log, new BfCsvService(db, NullLogger<BfCsvService>.Instance, log));
    }

    [Fact]
    public void Inhalt_EntsprichtVorlageByteGenau_Cp1252_Crlf()
    {
        var bytes = BfCsv.Kodierung().GetBytes(BfCsv.Inhalt(VorlageAuftrag(), new BfCsvKopf()));
        bytes.Should().Equal(Vorlage());

        // Zeile 16 der Vorlage = einzige Positionszeile des zweiten Auftrags
        var zweiter = BfCsv.Kodierung().GetBytes(BfCsv.Inhalt(ZweiterAuftrag(), new BfCsvKopf()));
        var rest = VorlageDatei()[Vorlage().Length..];
        zweiter[^rest.Length..].Should().Equal(rest);
        BfCsv.Inhalt(ZweiterAuftrag(), new BfCsvKopf()).Split("\r\n").Should().HaveCount(15, "14 Zeilen + leeres Ende, keine Auffuellzeilen")
            .And.OnlyContain(z => z.Length == 0 || z.Count(c => c == ';') == BfCsv.Spalten - 1);
    }

    [Fact]
    public void Inhalt_Umlaut_EinByteCp1252()
    {
        var a = VorlageAuftrag();
        a.Positionen.Single(p => p.Position == 1).Matchcode = "Abdeckung ä";
        BfCsv.Kodierung().GetBytes(BfCsv.Inhalt(a, new BfCsvKopf())).Should().Contain(0xC4, "Ä als ein Byte in Windows-1252, kein UTF-8");
    }

    [Fact]
    public async Task Freigegeben_Offen_SchreibtDatei_KeineTmp_Geschrieben()
    {
        var (db, a, log, svc) = Fx();

        await svc.RunOnceAsync(_dir, new BfCsvKopf(), Wdh);

        Directory.GetFiles(_dir).Select(Path.GetFileName).Should().Equal("BF00000002.csv");
        File.ReadAllBytes(Path.Combine(_dir, "BF00000002.csv")).Should().Equal(Vorlage());
        (a.CsvStatus, a.CsvDatei, a.ModifiedBy).Should().Be((BfCsvStatus.Geschrieben, Path.Combine(_dir, "BF00000002.csv"), BfCsv.ServiceActor));
        a.CsvGeschriebenAm.Should().NotBeNull();
        log.Runs.Should().ContainSingle(r => r.ServiceName == "BfAuftragCsv");
    }

    [Fact]
    public async Task Kollision_Fehler_OhneAutomatischeWiederholung()
    {
        var (db, a, log, svc) = Fx();
        File.WriteAllText(Path.Combine(_dir, "BF00000002.csv"), "alt");

        await svc.RunOnceAsync(_dir, new BfCsvKopf(), Wdh);
        (a.CsvStatus, a.CsvFehler, a.CsvVersuche).Should().Be((BfCsvStatus.Fehler, BfCsv.KollisionText, 1));
        File.ReadAllText(Path.Combine(_dir, "BF00000002.csv")).Should().Be("alt", "nie ueberschreiben");

        a.ModifiedAt = DateTime.Now.AddHours(-1);
        db.SaveChanges();
        await svc.RunOnceAsync(_dir, new BfCsvKopf(), Wdh);
        a.CsvVersuche.Should().Be(1, "Kollision wird nicht automatisch wiederholt");
    }

    [Fact]
    public async Task Schreibfehler_WiederholungErstNachAbstand()
    {
        var (db, a, log, svc) = Fx();
        var fehlt = Path.Combine(_dir, "gibt-es-nicht");

        await svc.RunOnceAsync(fehlt, new BfCsvKopf(), Wdh);
        (a.CsvStatus, a.CsvVersuche).Should().Be((BfCsvStatus.Fehler, 1));

        await svc.RunOnceAsync(_dir, new BfCsvKopf(), Wdh);
        a.CsvStatus.Should().Be(BfCsvStatus.Fehler, "Abstand 15 min noch nicht um");

        a.ModifiedAt = DateTime.Now.AddMinutes(-16);
        db.SaveChanges();
        await svc.RunOnceAsync(_dir, new BfCsvKopf(), Wdh);
        a.CsvStatus.Should().Be(BfCsvStatus.Geschrieben);
    }

    [Theory]
    [InlineData(BfAuftragStatus.Bestellt)]
    [InlineData(BfAuftragStatus.Storniert)]
    [InlineData(BfAuftragStatus.Fertig)]
    public async Task NurFreigegeben_SonstNichts_UndKeinProtokoll(BfAuftragStatus status)
    {
        var (_, _, log, svc) = Fx(a => a.Status = status);

        await svc.RunOnceAsync(_dir, new BfCsvKopf(), Wdh);

        Directory.GetFiles(_dir).Should().BeEmpty();
        log.Runs.Should().BeEmpty();
    }

    [Fact]
    public async Task NichtKodierbaresZeichen_Fehler_KeineDatei()
    {
        var (_, a, _, svc) = Fx(a => a.Positionen[0].Arbeitsschritte = "Łaser");

        await svc.RunOnceAsync(_dir, new BfCsvKopf(), Wdh);

        a.CsvStatus.Should().Be(BfCsvStatus.Fehler);
        a.CsvFehler.Should().Contain("Windows-1252");
        Directory.GetFiles(_dir).Should().BeEmpty();
    }

    /// <summary>v1.99.42 (Walter 2026-11-18, ersetzt B-1 Variante c): Lagerplatz <c>;</c> → <c>,</c>, Hauptlagerplatz = Teil davor, keine Anfuehrungszeichen.</summary>
    [Theory]
    [InlineData("S-03;0;0;0", "S-03,0,0,0", "S-03")]
    [InlineData("K-02", "K-02", "K-02")]
    [InlineData(" H4-02,1,2,3 ", "H4-02,1,2,3", "H4-02")]
    [InlineData(null, "", "")]
    public void Lagerplatz_KommaStattSemikolon(string? lp, string position, string haupt) =>
        (BfCsv.Lagerplatz(lp), BfCsv.Hauptlagerplatz(lp)).Should().Be((position, haupt));

    [Theory]
    [InlineData("1", "1")]
    [InlineData("3.000", "3")]
    [InlineData("12,0", "12")]
    [InlineData("1.5", null)]
    [InlineData("M-1", null)]
    [InlineData(null, null)]
    public void MaterialNr_Ganzzahlig(string? wert, string? erwartet) => BfCsv.MaterialNr(wert).Should().Be(erwartet);
}
