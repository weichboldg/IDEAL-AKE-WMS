using System.Text;
using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Services.SyncLogger;
using IdealAkeWms.Tests.Helpers;
using IDEALAKEWMSService.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IDEALAKEWMSService.Tests.Services;

/// <summary>v1.99.11 (Spec 2026-11-12-wareneingang-barcode-lieferschein-csv-export F2/F3, AK 7, 8, 11, 12): Ausgangstabelle und Schreiben.
/// Echte Dateien in einem lokalen Temp-Ordner je Test (kein UNC).</summary>
public sealed class WareneingangCsvServiceTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "wms-csv-test-" + Guid.NewGuid().ToString("N"));
    private static readonly WareneingangCsvWiederholung Wdh = new(3, 15);

    public WareneingangCsvServiceTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
    }

    private static ApplicationDbContext Db()
    {
        var db = TestDbContextFactory.Create();
        db.Articles.Add(new Article { Id = 5, ArticleNumber = "00722001", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        db.StorageLocations.Add(new StorageLocation { Id = 9, Code = "HR 0 0 0", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        db.SaveChanges();
        return db;
    }

    private static PurchaseOrderReceipt Receipt(ApplicationDbContext db, Guid? vorgang, string? barcode, SageWareneingangStatus status = SageWareneingangStatus.Uebergeben)
    {
        var m = new StockMovement
        {
            ArticleId = 5, StorageLocationId = 9, Quantity = 1m, MovementType = MovementType.Einbuchung, Timestamp = DateTime.Now, WindowsUser = "t",
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t",
            PurchaseOrderReceipt = new PurchaseOrderReceipt
            {
                BelId = 500, BelPosId = 1, Belegnummer = "2026-500", LieferantKto = "70001", LieferantName = "Muster; \"GmbH\"", Quantity = 1m,
                Lieferart = PurchaseOrderReceiptLieferart.Vollstaendig, SageWeStatus = status, SageWeVorgang = vorgang, Lieferschein = "LS-4711",
                Barcode = barcode, SageWeBelegnummer = status == SageWareneingangStatus.Uebergeben ? "2027-398675" : null,
                SageWeAm = status == SageWareneingangStatus.Uebergeben ? new DateTime(2026, 11, 12, 10, 5, 0) : null,
                CreatedAt = new DateTime(2026, 11, 12, 9, 30, 0), CreatedBy = "Max Lager", CreatedByWindows = "AKE\\max",
            },
        };
        db.StockMovements.Add(m);
        db.SaveChanges();
        return m.PurchaseOrderReceipt!;
    }

    private static WareneingangCsvService Svc(ApplicationDbContext db, FakeSyncLogger log) =>
        new(db, NullLogger<WareneingangCsvService>.Instance, log);

    [Fact]
    public async Task Uebergeben_MitBarcode_EineZeile_EineDatei_KeineTmp_Geschrieben_AK7()
    {
        var db = Db();
        var v = Guid.NewGuid();
        Receipt(db, v, "AKE1235564654");
        Receipt(db, v, "AKE1235564654");   // zweite Position desselben Absendens
        var log = new FakeSyncLogger();

        await Svc(db, log).RunOnceAsync(_dir, Wdh);

        var e = db.WareneingangCsvExporte.Single();
        (e.SageWeVorgang, e.Status, e.Versuche, e.CreatedBy, e.ModifiedBy).Should()
            .Be((v, WareneingangCsvStatus.Geschrieben, 0, WareneingangCsv.ServiceActor, WareneingangCsv.ServiceActor));
        e.Datei.Should().Be(Path.Combine(_dir, "AKE1235564654.csv"));
        e.GeschriebenAm.Should().NotBeNull();
        Directory.GetFiles(_dir).Select(f => Path.GetFileName(f)).Should().Equal("AKE1235564654.csv");
        var bytes = File.ReadAllBytes(e.Datei!);
        bytes.Take(3).Should().Equal(new byte[] { 0xEF, 0xBB, 0xBF }, "UTF-8 mit BOM");
        var text = Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        // v1.99.29: ohne Kopfzeile, genau eine Zeile; ohne UserId an der Bewegung -> Erfasser = CreatedBy
        text.Should().Be("AKE1235564654;70001;\"Muster; \"\"GmbH\"\"\";2026-500;LS-4711;2027-398675;Max Lager;2026-11-12 09:30;2026-11-12 10:05\r\n");
        log.Runs.Single().ServiceName.Should().Be(SyncLogServices.WareneingangCsv);
        log.Runs.Single().FinalCounts.Should().BeEquivalentTo(new Dictionary<string, int> { ["geschrieben"] = 1, ["fehler"] = 0 });

        // naechster Takt: nichts zu tun, kein Protokoll-Eintrag
        await Svc(db, log).RunOnceAsync(_dir, Wdh);
        log.Runs.Should().HaveCount(1);
    }

    [Fact]
    public async Task OhneArbeit_KeinLauf_NichtUebergeben_NurInSage_OhneBarcode_PfadLeer()
    {
        var db = Db();
        var v = Guid.NewGuid();
        Receipt(db, v, "BC1");
        Receipt(db, v, "BC1", SageWareneingangStatus.Fehler);       // ein Receipt des Vorgangs noch nicht uebergeben
        Receipt(db, null, "BC2", SageWareneingangStatus.NurInSage);  // ohne Vorgang
        Receipt(db, Guid.NewGuid(), null);                            // ohne Barcode
        var log = new FakeSyncLogger();

        await Svc(db, log).RunOnceAsync(_dir, Wdh);
        await Svc(db, log).RunOnceAsync("  ", Wdh);

        db.WareneingangCsvExporte.Should().BeEmpty();
        log.Runs.Should().BeEmpty();
        Directory.GetFiles(_dir).Should().BeEmpty();
    }

    [Fact]
    public async Task AlsUebergebenMarkiert_BekommtCsvMitHandBelegnummer_AK8()
    {
        var db = Db();
        var r = Receipt(db, Guid.NewGuid(), "BC9", SageWareneingangStatus.Klaeren);
        // Weg „Als übergeben markieren“ (Repository der Web-App) - ohne Aenderung abgedeckt
        await new IdealAkeWms.Data.Repositories.PurchaseOrderReceiptRepository(db).SageWeAlsUebergebenAsync(r.SageWeVorgang!.Value, "QA-1", "u", "w");

        await Svc(db, new FakeSyncLogger()).RunOnceAsync(_dir, Wdh);

        File.ReadAllText(Path.Combine(_dir, "BC9.csv")).Should().Contain(";QA-1;");
    }

    /// <summary>v1.99.33 (Spec 2026-11-17-wareneingang-sammellieferschein-und-bestellkopf A2, AK 8) BEWUSST GEAENDERT: bis v1.99.32 erwartete
    /// dieser Test bei vorhandener <c>BC1.csv</c> den Fehler <see cref="WareneingangCsv.KollisionText"/>. Jetzt nimmt der Dienst die naechste
    /// freie Nummer; die fremde Datei bleibt unveraendert, kein Fehler.</summary>
    [Fact]
    public async Task DateiExistiert_NieUeberschrieben_NaechsteNummer_KeinFehler_AK8()
    {
        var db = Db();
        Receipt(db, Guid.NewGuid(), "BC1");
        var ziel = Path.Combine(_dir, "BC1.csv");
        File.WriteAllText(ziel, "fremd");
        var log = new FakeSyncLogger();

        await Svc(db, log).RunOnceAsync(_dir, Wdh);

        File.ReadAllText(ziel).Should().Be("fremd");
        var e = db.WareneingangCsvExporte.Single();
        (e.Status, e.Fehler, e.Versuche, e.Datei).Should().Be((WareneingangCsvStatus.Geschrieben, (string?)null, 0, Path.Combine(_dir, "BC1_1.csv")));
        File.ReadAllText(e.Datei!).Should().StartWith("BC1;", "Feld 1 = Barcode ohne Suffix");
        log.Runs.Single().FinalCounts.Should().BeEquivalentTo(new Dictionary<string, int> { ["geschrieben"] = 1, ["fehler"] = 0 });
        Directory.GetFiles(_dir, "*.tmp").Should().BeEmpty();
    }

    [Fact]
    public async Task Sammellieferschein_ZweiVorgaengeImTakt_GrossKlein_LaufendeNummer_InhaltBarcodeOhneSuffix_AK5_AK7()
    {
        var db = Db();
        Receipt(db, Guid.NewGuid(), "B");
        Receipt(db, Guid.NewGuid(), "b");

        await Svc(db, new FakeSyncLogger()).RunOnceAsync(_dir, Wdh);

        var e = db.WareneingangCsvExporte.OrderBy(x => x.Id).ToList();
        e.Select(x => x.Datei).Should().Equal(Path.Combine(_dir, "B.csv"), Path.Combine(_dir, "b_1.csv"));
        e.Should().OnlyContain(x => x.Status == WareneingangCsvStatus.Geschrieben);
        // Inhalt unveraendert (TS-153-Format, 9 Felder): Feld 1 = Barcode der Zeile ohne Suffix
        File.ReadAllText(e[0].Datei!).Should().StartWith("B;70001;").And.EndWith(";2026-11-12 10:05\r\n");
        File.ReadAllText(e[1].Datei!).Should().StartWith("b;70001;").And.EndWith(";2026-11-12 10:05\r\n");
    }

    [Fact]
    public async Task Sammellieferschein_NachAbholung_NummerAusDerDatenbank_NichtAusDemOrdner_AK6()
    {
        var db = Db();
        Receipt(db, Guid.NewGuid(), "B");
        await Svc(db, new FakeSyncLogger()).RunOnceAsync(_dir, Wdh);
        File.Delete(Path.Combine(_dir, "B.csv"));   // enaio hat abgeholt

        Receipt(db, Guid.NewGuid(), "B");
        await Svc(db, new FakeSyncLogger()).RunOnceAsync(_dir, Wdh);

        Directory.GetFiles(_dir).Select(f => Path.GetFileName(f)).Should().Equal("B_1.csv");
    }

    [Fact]
    public async Task Sammellieferschein_FremderBarcodeMitSuffixNamen_Uebersprungen_AK9()
    {
        var db = Db();
        Receipt(db, Guid.NewGuid(), "B_1");
        Receipt(db, Guid.NewGuid(), "B");
        await Svc(db, new FakeSyncLogger()).RunOnceAsync(_dir, Wdh);
        File.Delete(Path.Combine(_dir, "B_1.csv"));   // auch nach Abholung belegt (DB)

        Receipt(db, Guid.NewGuid(), "B");
        await Svc(db, new FakeSyncLogger()).RunOnceAsync(_dir, Wdh);

        db.WareneingangCsvExporte.OrderBy(x => x.Id).Select(x => x.Datei).ToList().Select(d => Path.GetFileName(d))
            .Should().Equal("B_1.csv", "B.csv", "B_2.csv");
    }

    [Fact]
    public async Task Altzeile_Kollision_NieAutomatisch_NachErneutSchreiben_NaechsteFreieNummer_AK10()
    {
        var db = Db();
        Receipt(db, Guid.NewGuid(), "B");
        await Svc(db, new FakeSyncLogger()).RunOnceAsync(_dir, Wdh);
        var alt = Receipt(db, Guid.NewGuid(), "B");
        db.WareneingangCsvExporte.Add(new WareneingangCsvExport
        {
            SageWeVorgang = alt.SageWeVorgang!.Value, Barcode = "B", Status = WareneingangCsvStatus.Fehler, Fehler = WareneingangCsv.KollisionText,
            Versuche = 1, CreatedAt = DateTime.Now.AddHours(-2), CreatedBy = "x", CreatedByWindows = "x", ModifiedAt = DateTime.Now.AddHours(-1),
        });
        db.SaveChanges();
        var log = new FakeSyncLogger();

        await Svc(db, log).RunOnceAsync(_dir, Wdh);
        log.Runs.Should().BeEmpty("Altzeile mit KollisionText wird nie automatisch wiederholt");

        (await new IdealAkeWms.Data.Repositories.PurchaseOrderReceiptRepository(db).CsvErneutSchreibenAsync(alt.SageWeVorgang!.Value, "u", "w")).Should().Be(1);
        await Svc(db, log).RunOnceAsync(_dir, Wdh);

        db.WareneingangCsvExporte.Single(x => x.SageWeVorgang == alt.SageWeVorgang).Datei.Should().Be(Path.Combine(_dir, "B_1.csv"));
    }

    [Fact]
    public void Dateiname_ErsteFreieNummerAbStart()
    {
        WareneingangCsvService.Dateiname("B", 0, _ => false).Should().Be("B.csv");
        WareneingangCsvService.Dateiname("B", 0, n => n is "B.csv" or "B_1.csv").Should().Be("B_2.csv");
        WareneingangCsvService.Dateiname("B", 2, _ => false).Should().Be("B_2.csv");
    }

    [Fact]
    public async Task PfadNichtErreichbar_Fehler_VersuchePlus1_WiederholungNachAbstand_HoechstensMax_AK12()
    {
        var db = Db();
        Receipt(db, Guid.NewGuid(), "BC1");
        var fehlt = Path.Combine(_dir, "gibt-es-nicht");
        var log = new FakeSyncLogger();

        await Svc(db, log).RunOnceAsync(fehlt, Wdh);   // wirft nicht

        var e = db.WareneingangCsvExporte.Single();
        (e.Status, e.Versuche).Should().Be((WareneingangCsvStatus.Fehler, 1));
        e.Fehler.Should().NotBeNullOrEmpty();
        log.Runs.Single().FinishedSuccess.Should().BeTrue("ein Dateifehler bricht den Lauf nicht ab");

        await Svc(db, log).RunOnceAsync(fehlt, Wdh);   // Abstand noch nicht vorbei
        log.Runs.Should().HaveCount(1);

        for (var i = 0; i < 5; i++)
        {
            e.ModifiedAt = DateTime.Now.AddMinutes(-20);
            db.SaveChanges();
            await Svc(db, log).RunOnceAsync(fehlt, Wdh);
        }
        e.Versuche.Should().Be(4, "erster Versuch + 3 automatische Wiederholungen");

        // Netz wieder da + „CSV erneut schreiben“ (Offen) -> geschrieben, ohne Rest-.tmp
        e.Status = WareneingangCsvStatus.Offen;
        db.SaveChanges();
        await Svc(db, log).RunOnceAsync(_dir, Wdh);
        e.Status.Should().Be(WareneingangCsvStatus.Geschrieben);
        Directory.GetFiles(_dir).Select(f => Path.GetFileName(f)).Should().Equal("BC1.csv");
    }

    [Fact]
    public async Task EigeneTmpLeiche_WirdUeberschrieben_UndUmbenannt()
    {
        var db = Db();
        Receipt(db, Guid.NewGuid(), "BC1");
        File.WriteAllText(Path.Combine(_dir, "BC1.tmp"), "halb");
        await Svc(db, new FakeSyncLogger()).RunOnceAsync(_dir, Wdh);
        Directory.GetFiles(_dir).Select(f => Path.GetFileName(f)).Should().Equal("BC1.csv");
        File.ReadAllText(Path.Combine(_dir, "BC1.csv")).Should().StartWith("BC1;");
    }

    [Fact]
    public async Task UngueltigerAltwert_Fehler_KeineDatei()
    {
        var db = Db();
        Receipt(db, Guid.NewGuid(), "..\\boese");
        await Svc(db, new FakeSyncLogger()).RunOnceAsync(_dir, Wdh);
        db.WareneingangCsvExporte.Single().Fehler.Should().Be(WareneingangCsv.UngueltigText);
        Directory.GetFiles(_dir).Should().BeEmpty();
        Directory.GetFiles(Path.GetDirectoryName(_dir)!, "boese*").Should().BeEmpty();
    }

    [Fact]
    public async Task HoechstensZehnDateienJeTakt()
    {
        var db = Db();
        for (var i = 0; i < 12; i++) Receipt(db, Guid.NewGuid(), "BC" + i);
        await Svc(db, new FakeSyncLogger()).RunOnceAsync(_dir, Wdh);
        db.WareneingangCsvExporte.Count().Should().Be(12, "Zeilen entstehen alle");
        db.WareneingangCsvExporte.Count(e => e.Status == WareneingangCsvStatus.Geschrieben).Should().Be(WareneingangCsv.MaxDateienJeTakt);
        Directory.GetFiles(_dir).Should().HaveCount(10);
    }

    [Fact]
    public void CsvInhalt_LeereWerteLeer_ZeilenumbruchGequotet()
    {
        var r = new PurchaseOrderReceipt { Belegnummer = "2026-1", Lieferschein = "A\nB", CreatedBy = "x", CreatedAt = new DateTime(2026, 1, 2, 3, 4, 0) };
        WareneingangCsvService.CsvInhalt(r, "BC").Should().Be("BC;;;2026-1;\"A\nB\";;x;2026-01-02 03:04;\r\n", "genau eine Zeile, keine Kopfzeile");
    }

    /// <summary>v1.99.29 (Spec 2026-11-14-wareneingang-csv-erfasser-samaccountname F2, AK 3-7): Erfasser = WindowsUserName ohne Domaene,
    /// sonst User.Name, ohne Benutzer CreatedBy - nie CreatedByWindows/WindowsUser (Sitzung).</summary>
    [Theory]
    [InlineData("jmuster", "jmuster")]
    [InlineData("AKE\\jmuster", "jmuster")]
    [InlineData("jmuster@ake.at", "jmuster")]
    [InlineData(" jmuster ", "jmuster")]
    [InlineData("JMuster", "JMuster")]   // Schreibweise wie gespeichert (Freigabe-Antwort 3)
    [InlineData(null, "Max Anzeige")]
    [InlineData("", "Max Anzeige")]
    [InlineData("  ", "Max Anzeige")]
    public void CsvInhalt_Erfasser_AusBenutzerDerBewegung(string? windowsUserName, string erwartet)
    {
        var r = new PurchaseOrderReceipt
        {
            CreatedBy = "Max Lager", CreatedByWindows = "AKE\\terminal01",
            StockMovement = new StockMovement { WindowsUser = "AKE\\terminal01", User = new User { Name = "Max Anzeige", WindowsUserName = windowsUserName } },
        };
        WareneingangCsvService.CsvInhalt(r, "BC").Split(';')[6].Should().Be(erwartet);
    }

    [Fact]
    public void CsvInhalt_Erfasser_OhneBewegungOderBenutzer_CreatedBy()
    {
        var ohneBewegung = new PurchaseOrderReceipt { CreatedBy = "Max Lager", CreatedByWindows = "AKE\\terminal01" };
        var ohneUser = new PurchaseOrderReceipt { CreatedBy = "Max Lager", CreatedByWindows = "AKE\\terminal01", StockMovement = new StockMovement { WindowsUser = "AKE\\terminal01" } };
        WareneingangCsvService.CsvInhalt(ohneBewegung, "BC").Split(';')[6].Should().Be("Max Lager");
        WareneingangCsvService.CsvInhalt(ohneUser, "BC").Split(';')[6].Should().Be("Max Lager");
    }

    [Fact]
    public void CsvInhalt_Erfasser_Sonderzeichen_Escaping_AK9()
    {
        var r = new PurchaseOrderReceipt { CreatedBy = "x", StockMovement = new StockMovement { User = new User { Name = "Muster; \"Max\"" } } };
        WareneingangCsvService.CsvInhalt(r, "BC").Should().Be("BC;;;;;;\"Muster; \"\"Max\"\"\";0001-01-01 00:00;\r\n");
    }

    [Fact]
    public async Task RunOnce_IncludeWirkt_DateiMitKontoname_AK8()
    {
        var db = Db();
        db.Users.Add(new User { Id = 77, Name = "Max Anzeige", WindowsUserName = "AKE\\jmuster", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        db.SaveChanges();
        var r = Receipt(db, Guid.NewGuid(), "BC7");
        db.StockMovements.Single(m => m.Id == r.StockMovementId).UserId = 77;
        db.SaveChanges();
        db.ChangeTracker.Clear();   // nichts aus dem Tracker - nur das Include liefert den Benutzer

        await Svc(db, new FakeSyncLogger()).RunOnceAsync(_dir, Wdh);

        File.ReadAllText(Path.Combine(_dir, "BC7.csv")).Should().Contain(";2027-398675;jmuster;2026-11-12 09:30;").And.NotContain("Max Lager");
    }

    [Fact]
    public void DI_LoestAuf_LogNameRegistriert_SyncLoggerLetzter()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(new Mock<ISyncLogger>().Object);
        services.AddScoped(_ => TestDbContextFactory.Create());
        services.AddScoped<WareneingangCsvService>();
        using var sp = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using var s = sp.CreateScope();
        s.ServiceProvider.GetRequiredService<WareneingangCsvService>().Should().NotBeNull();
        SyncLogServices.All.Should().Contain(SyncLogServices.WareneingangCsv);
        SyncLogServices.DisplayNames[SyncLogServices.WareneingangCsv].Should().Be("Wareneingang-CSV");
        typeof(WareneingangCsvService).GetConstructors().Single().GetParameters().Last().ParameterType.Should().Be(typeof(ISyncLogger));
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "IDEALAKEWMSService", "Program.cs"))
            .Should().Contain("AddScoped<WareneingangCsvService>()");
    }
}
