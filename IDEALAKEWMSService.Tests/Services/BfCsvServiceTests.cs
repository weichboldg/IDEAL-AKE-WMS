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
/// Die Vorlage <c>TestData/BfCsv_PlatzhalterBisP1.csv</c> ist ein PLATZHALTER BIS P-1 (Musterdatei aus dem WiCAM-Pool).</summary>
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
        bool gestrichen = false) => new()
    {
        Position = pos, ArticleNumber = "A" + pos, Matchcode = matchcode, Materialnummer = matnr, Menge = menge, Bemerkung = bemerkung,
        Hauptlagerplatz = lp, Teileart = teileart, Gestrichen = gestrichen, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t",
    };

    /// <summary>Genau der Auftrag hinter der Vorlage-Datei.</summary>
    public static BfAuftrag VorlageAuftrag() => new()
    {
        Nummer = "BF00000042", Status = BfAuftragStatus.Freigegeben, CsvStatus = BfCsvStatus.Offen, BestellerName = "Jörg Müller",
        Schachteltermin = new DateTime(2026, 11, 18), FeTermin = new DateTime(2026, 12, 1),
        CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t",
        Positionen =
        {
            P(2, "Abdeckung Ä", "1.4301", 2.5m, "17.11", "L-2"),
            P(1, "  blech  winkel\t 4301 ", "4301", 10m, "EILT - Laser - 17.11 - 1", "HR 1 2 3", "BG"),
            P(3, "OHNE NUMMER", null, 1m, "17.11", "L-3"),
            P(4, "GESTRICHEN", "9", 1m, "17.11", "L-4", gestrichen: true),
        },
    };

    private static byte[] Vorlage() => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "TestData", "BfCsv_PlatzhalterBisP1.csv"));

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
        bytes.Should().Contain(0xC4, "Ä als ein Byte in Windows-1252, kein UTF-8");
    }

    [Fact]
    public async Task Freigegeben_Offen_SchreibtDatei_KeineTmp_Geschrieben()
    {
        var (db, a, log, svc) = Fx();

        await svc.RunOnceAsync(_dir, new BfCsvKopf(), Wdh);

        Directory.GetFiles(_dir).Select(Path.GetFileName).Should().Equal("BF00000042.csv");
        File.ReadAllBytes(Path.Combine(_dir, "BF00000042.csv")).Should().Equal(Vorlage());
        (a.CsvStatus, a.CsvDatei, a.ModifiedBy).Should().Be((BfCsvStatus.Geschrieben, Path.Combine(_dir, "BF00000042.csv"), BfCsv.ServiceActor));
        a.CsvGeschriebenAm.Should().NotBeNull();
        log.Runs.Should().ContainSingle(r => r.ServiceName == "BfAuftragCsv");
    }

    [Fact]
    public async Task Kollision_Fehler_OhneAutomatischeWiederholung()
    {
        var (db, a, log, svc) = Fx();
        File.WriteAllText(Path.Combine(_dir, "BF00000042.csv"), "alt");

        await svc.RunOnceAsync(_dir, new BfCsvKopf(), Wdh);
        (a.CsvStatus, a.CsvFehler, a.CsvVersuche).Should().Be((BfCsvStatus.Fehler, BfCsv.KollisionText, 1));
        File.ReadAllText(Path.Combine(_dir, "BF00000042.csv")).Should().Be("alt", "nie ueberschreiben");

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
        var (_, a, _, svc) = Fx(a => a.BestellerName = "Łukasz");

        await svc.RunOnceAsync(_dir, new BfCsvKopf(), Wdh);

        a.CsvStatus.Should().Be(BfCsvStatus.Fehler);
        a.CsvFehler.Should().Contain("Windows-1252");
        Directory.GetFiles(_dir).Should().BeEmpty();
    }

    /// <summary>Entscheidung B-1 Variante c (vorlaeufig, wie Excel, durch P-1 zu bestaetigen): Lagerplatz mit <c>;</c> eingefasst, die Zeile hat
    /// weiter genau 8 Spalten (Semikolon-Zaehlung ausserhalb der Anfuehrungszeichen).</summary>
    [Fact]
    public void Inhalt_LagerplatzMitSemikolon_InAnfuehrungszeichen()
    {
        var a = VorlageAuftrag();
        a.Positionen.Single(p => p.Position == 2).Hauptlagerplatz = "S-03;0;0;0";
        var zeile = BfCsv.Inhalt(a, new BfCsvKopf()).Split("\r\n").Single(z => z.StartsWith("ABDECKUNG"));
        zeile.Should().Be("ABDECKUNG Ä;1.4301;2,5;17.11;\"S-03;0;0;0\";Jörg Müller;18.11.2026;2026-12-01");
        System.Text.RegularExpressions.Regex.Replace(zeile, "\"[^\"]*\"", "x").Count(c => c == ';').Should().Be(7);
    }
}
