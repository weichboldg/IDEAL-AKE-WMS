using FluentAssertions;
using IdealAkeWms.Models;
using IdealAkeWms.Services.SageSdata;
using IdealAkeWms.Services.SyncLogger;
using IdealAkeWms.Tests.Helpers;
using IDEALAKEWMSService.Services.Sdata;
using IDEALAKEWMSService.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace IDEALAKEWMSService.Tests.Services.Sdata;

/// <summary>Spec 2026-11-06-sage-sdata-stammdaten-sync, E8c (A13, P-K1): Kontokorrent → Kreditoren/Debitoren (Mapping, Sage- vs.
/// WMS-Felder, Verknuepfung, Dublette, Missing, Empty-Guard), ArtikelLieferantInfo → ArticleSuppliers, Gates aus = kein Lauf, DI.</summary>
public class KontokorrentArtikelLieferantTests
{
    private static readonly SdataLeseZiel Ziel = new("https://sagetest01:5493", "ol", "ake_TEST2026;1", "u", "p", true);

    private static Mock<ISageSdataReadClient> Client<T>(string ressource, params T[] zeilen)
    {
        var client = new Mock<ISageSdataReadClient>();
        Setup(client, ressource, zeilen);
        return client;
    }

    private static void Setup<T>(Mock<ISageSdataReadClient> client, string ressource, params T[] zeilen) =>
        client.Setup(c => c.QueryAllAsync<T>(It.IsAny<SdataLeseZiel>(), It.IsAny<string>(), ressource,
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ReturnsAsync(new SdataSeiten<T>(zeilen, ressource, 1, zeilen.Length, 1));

    private static KontokorrentSyncService Konto(IdealAkeWms.Data.ApplicationDbContext ctx, FakeSyncLogger log, Mock<ISageSdataReadClient> client) =>
        new(ctx, client.Object, new ConfigurationBuilder().Build(), NullLogger<KontokorrentSyncService>.Instance, log);

    private static Creditor Kred(string name, string? kto = null, string source = StorageLocationSource.Manual) => new()
    {
        Name = name, SageKto = kto, Source = source, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t",
    };

    // ----- Kreditoren -----

    [Fact]
    public async Task Kreditoren_Abfrage_Anlage_SageFelderUeberschreiben_WmsFelderNie()
    {
        using var ctx = TestDbContextFactory.Create();
        var sage = Kred("Alt-Matchcode", "K31013", StorageLocationSource.Sage);
        sage.ShortName = "PULVER"; sage.Remark = "WMS-Notiz"; sage.IsCoater = true; sage.IsActive = false; sage.Matchcode = "WMS-MC";
        sage.SageGruppe = "G1"; sage.SageEUUStID = "ATU1";
        ctx.Creditors.Add(sage);
        await ctx.SaveChangesAsync();
        var client = Client(KontokorrentSyncService.Ressource,
            new SdataKontoZeile(" K31013 ", "Pulverbeschichtung Huber", null, "30T", "G2", false, USER_EKMindestbestellwert: 150.555m),
            new SdataKontoZeile("K40000", " Neu GmbH ", "ATU9", "14T", "G1", false, USER_EKMindestbestellwert: null));
        var log = new FakeSyncLogger();

        await Konto(ctx, log, client).RunKreditorenAsync(dryRun: false, ziel: Ziel);

        client.Verify(c => c.QueryAllAsync<SdataKontoZeile>(Ziel, SageSdataReadClient.VertragResources, "Kontokorrent", "KtoArt eq 'K'",
            "Kto,Matchcode,EUUStID,Zahlungskond,Gruppe,Aktiv,Adresse,USER_EKMindestbestellwert", "Kto", It.IsAny<CancellationToken>(), It.IsAny<int>()), Times.Once);
        var k = ctx.Creditors.Single(c => c.SageKto == "K31013");
        (k.Name, k.SageMatchcode, k.SageGruppe, k.SageZahlungskondition, k.SageEKMindestbestellwert)
            .Should().Be(("Alt-Matchcode", "Pulverbeschichtung Huber", "G2", "30T", (decimal?)150.56m), "Adressen nicht lesbar: Name bleibt");
        k.SageEUUStID.Should().Be("ATU1", "null = Sage liefert nichts = nicht schreiben (F1)");
        (k.ShortName, k.Remark, k.IsCoater).Should().Be(("PULVER", "WMS-Notiz", true), "WMS-Felder nie");
        (k.IsActive, k.Matchcode).Should().Be((false, "Pulverbeschichtung Huber"), "G6: Sage gewinnt (Aktiv aus Sage, Matchcode = Sage-Matchcode)");
        log.Runs.Single().Events.Should().Contain(e => e.Level == "Warning" && e.Message.Contains("Adressen") && e.Message.Contains("nicht lesbar"),
            "Adressen im Mock nicht eingerichtet = nicht lesbar: melden, Konto-Abgleich laeuft weiter");
        (k.ModifiedBy, k.ModifiedByWindows).Should().Be(("KontokorrentSync", "KontokorrentSync"));

        var n = ctx.Creditors.Single(c => c.SageKto == "K40000");
        (n.Name, n.Source, n.ShortName, n.IsActive, n.SageEUUStID, n.CreatedBy).Should()
            .Be(("Neu GmbH", StorageLocationSource.Sage, (string?)null, false, "ATU9", "KontokorrentSync"), "IsActive nur bei der Anlage aus Sage Aktiv");
        log.Runs.Single(r => r.ServiceName == SyncLogServices.Kreditoren).FinalCounts.Should().BeEquivalentTo(new Dictionary<string, int>
        {
            ["gelesen"] = 2, ["neu"] = 1, ["aktualisiert"] = 1, ["verknuepft"] = 0, ["unveraendert"] = 0, ["uebersprungen"] = 0,
            ["nicht_in_sage"] = 0, ["nicht_in_sage_neu"] = 0, ["wieder_in_sage"] = 0, ["moegliche_dubletten"] = 0, ["wms_eigen"] = 0,
        });
    }

    [Fact]
    public async Task Kreditoren_Verknuepfung_Dublette_Missing_Wiederfinden()
    {
        using var ctx = TestDbContextFactory.Create();
        var manuellMitKto = Kred("Lackiererei Maier", "K50000");
        manuellMitKto.SageUebergabeStatus = SageUebergabeStatus.Offen;
        var manuellOhneKto = Kred("Muster GmbH");
        var weg = Kred("Weg AG", "K60000", StorageLocationSource.Sage);
        var wiederDa = Kred("Zurueck AG", "K70000", StorageLocationSource.Sage);
        wiederDa.SageMissingSince = DateTime.Now.AddDays(-2);
        ctx.Creditors.AddRange(manuellMitKto, manuellOhneKto, weg, wiederDa);
        await ctx.SaveChangesAsync();
        var log = new FakeSyncLogger();

        await Konto(ctx, log, Client(KontokorrentSyncService.Ressource,
            new SdataKontoZeile("K50000", "MAIER LACK", null, null, null, true),
            new SdataKontoZeile("K80000", "muster gmbh", null, null, null, true),
            new SdataKontoZeile("K70000", "Zurueck AG", null, null, null, true))).RunKreditorenAsync(false, ziel: Ziel);

        var m = ctx.Creditors.Single(c => c.SageKto == "K50000");
        (m.Source, m.Name, m.SageMatchcode, m.SageUebergabeStatus, m.SageUebergabeAm != null)
            .Should().Be((StorageLocationSource.Sage, "Lackiererei Maier", "MAIER LACK", SageUebergabeStatus.Uebergeben, true),
                "verknuepft ueber SageKto; der WMS-Name bleibt");
        ctx.Creditors.Single(c => c.Name == "Muster GmbH").SageKto.Should().BeNull("gleichnamig wird nie automatisch verknuepft");
        ctx.Creditors.Should().HaveCount(5, "das Sage-Konto muster gmbh wird zusaetzlich angelegt");
        ctx.Creditors.Single(c => c.SageKto == "K60000").SageMissingSince.Should().NotBeNull("fehlt in Sage, nie loeschen");
        ctx.Creditors.Single(c => c.SageKto == "K70000").SageMissingSince.Should().BeNull("wieder da");

        var lauf = log.Runs.Single();
        lauf.FinalCounts!["verknuepft"].Should().Be(1);
        lauf.FinalCounts["moegliche_dubletten"].Should().Be(1);
        lauf.FinalCounts["nicht_in_sage_neu"].Should().Be(1);
        lauf.FinalCounts["wieder_in_sage"].Should().Be(1);
        lauf.FinalCounts["wms_eigen"].Should().Be(1, "G6: Muster GmbH ohne Sage-Konto wird gemeldet, nicht geloescht");
        lauf.Events.Should().Contain(e => e.Level == "Warning" && e.Message.Contains("nur im WMS") && e.Message.Contains("Muster GmbH"));
        lauf.Events.Should().Contain(e => e.Level == "Warning" && e.Message.Contains("moegliche Dublette") && e.Message.Contains("Muster GmbH")
            && e.Message.Contains("K80000"));
        lauf.Events.Should().Contain(e => e.Message.Contains("verknuepft") && e.Message.Contains("K50000"));
    }

    [Fact]
    public async Task Kreditoren_EmptyGuard_KennzeichnetNichts_DryRunSchreibtNicht()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.Creditors.Add(Kred("A", "K1", StorageLocationSource.Sage));
        await ctx.SaveChangesAsync();
        var log = new FakeSyncLogger();

        await Konto(ctx, log, Client<SdataKontoZeile>(KontokorrentSyncService.Ressource)).RunKreditorenAsync(false, ziel: Ziel);
        ctx.Creditors.Single().SageMissingSince.Should().BeNull();
        log.Runs.Single().Events.Should().Contain(e => e.Level == "Warning" && e.Message.Contains("Empty-Guard"));

        using var ctx2 = TestDbContextFactory.Create();
        await Konto(ctx2, new FakeSyncLogger(), Client(KontokorrentSyncService.Ressource, new SdataKontoZeile("K2", "B", null, null, null, true)))
            .RunKreditorenAsync(dryRun: true, ziel: Ziel);
        ctx2.Creditors.Should().BeEmpty("DryRun schreibt nichts");
    }

    [Fact]
    public async Task Kreditoren_SDataFehler_LaufFehler_Wirft()
    {
        using var ctx = TestDbContextFactory.Create();
        var client = new Mock<ISageSdataReadClient>();
        client.Setup(c => c.QueryAllAsync<SdataKontoZeile>(It.IsAny<SdataLeseZiel>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ThrowsAsync(new SdataLeseException("Kontokorrent", 500, "kaputt"));
        var log = new FakeSyncLogger();

        await ((Func<Task>)(() => Konto(ctx, log, client).RunKreditorenAsync(false, ziel: Ziel))).Should().ThrowAsync<SdataLeseException>();
        log.Runs.Single().FinishedFailed.Should().BeTrue();
    }

    [Fact]
    public async Task Kreditoren_AdresseUndAnsprechpartner_AusSage_SageGewinnt()
    {
        using var ctx = TestDbContextFactory.Create();
        var verknuepft = Kred("Lackiererei Maier", "K50000");
        verknuepft.Matchcode = "WMS-MC";
        ctx.Creditors.Add(verknuepft);
        await ctx.SaveChangesAsync();
        var client = Client(KontokorrentSyncService.Ressource,
            new SdataKontoZeile("K31013", "LOYSCH MELK", "ATU1", "30T", "G2", true, USER_EKMindestbestellwert: 100m, Adresse: 7),
            new SdataKontoZeile("K50000", "MAIER LACK", null, null, null, true, Adresse: 8),
            new SdataKontoZeile("K60000", "OHNE ADRESSE", null, null, null, true, Adresse: 99));
        Setup(client, KontokorrentSyncService.RessourceAdressen,
            new SdataAdresseZeile(7, "\"carlo\" Loysch GmbH", " ", "Pielacher Straße 50", "3390", "Melk", "AT", "+43-2752-52911", "info@loysch.at"),
            new SdataAdresseZeile(8, "Maier", "Lackiererei GmbH", "Gasse 1", "4020", "Linz", "AT", null, null));
        Setup(client, KontokorrentSyncService.RessourceAnsprechpartner,
            new SdataAnsprechpartnerZeile(12, 7, "Eva", "Zweit", null),
            new SdataAnsprechpartnerZeile(11, 7, " Anna ", "Huber", null),
            new SdataAnsprechpartnerZeile(20, 8, null, null, "Einkauf"));
        var log = new FakeSyncLogger();

        await Konto(ctx, log, client).RunKreditorenAsync(false, ziel: Ziel);

        client.Verify(c => c.QueryAllAsync<SdataAdresseZeile>(Ziel, SageSdataReadClient.VertragResources, "Adressen", null,
            "Adresse,Name1,Name2,LieferStrasse,LieferPLZ,LieferOrt,LieferLand,Telefon,EMail", "Adresse", It.IsAny<CancellationToken>(), It.IsAny<int>()), Times.Once);
        var n = ctx.Creditors.Single(c => c.SageKto == "K31013");
        (n.Name, n.Street, n.PostalCode, n.City, n.Country, n.Phone, n.Email, n.ContactPerson, n.Matchcode, n.ShortName).Should().Be((
            "\"carlo\" Loysch GmbH", "Pielacher Straße 50", "3390", "Melk", "AT", "+43-2752-52911", "info@loysch.at", "Anna Huber (+1 weitere)",
            "LOYSCH MELK", (string?)null), "Name = Name1 (+Name2), erster Ansprechpartner nach Nummer, Matchcode = Sage-Matchcode (G6), Kurzname bleibt leer");
        var m = ctx.Creditors.Single(c => c.SageKto == "K50000");
        (m.Name, m.ContactPerson, m.Matchcode, m.Source).Should().Be(("Maier Lackiererei GmbH", "Einkauf", "MAIER LACK", StorageLocationSource.Sage),
            "verknuepft: G6 – Name, Kontakt und Matchcode aus Sage");
        var o = ctx.Creditors.Single(c => c.SageKto == "K60000");
        (o.Name, o.Street).Should().Be(("OHNE ADRESSE", (string?)null), "keine Adresszeile: Matchcode als Name bei der Anlage");
        log.Runs.Single().Events.Should().NotContain(e => e.Level == "Warning" && e.Message.Contains("nicht lesbar"));
    }

    [Fact]
    public async Task Kreditoren_Dublette_GegenSageName1Name2_RueckfallMatchcode_Q5()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.Creditors.AddRange(Kred("Loysch GmbH Melk"), Kred("OHNE ADR"), Kred("LOYSCH MC"));
        await ctx.SaveChangesAsync();
        var client = Client(KontokorrentSyncService.Ressource,
            new SdataKontoZeile("K31013", "LOYSCH MC", null, null, null, true, Adresse: 7),
            new SdataKontoZeile("K60000", "ohne adr", null, null, null, true, Adresse: 99));
        Setup(client, KontokorrentSyncService.RessourceAdressen, new SdataAdresseZeile(7, "Loysch GmbH", "Melk", null, null, null, null, null, null));
        Setup<SdataAnsprechpartnerZeile>(client, KontokorrentSyncService.RessourceAnsprechpartner);
        var log = new FakeSyncLogger();

        await Konto(ctx, log, client).RunKreditorenAsync(false, ziel: Ziel);

        var lauf = log.Runs.Single();
        lauf.FinalCounts!["moegliche_dubletten"].Should().Be(2, "Name1+Name2 trifft „Loysch GmbH Melk“, ohne Adresse greift der Matchcode");
        var w = lauf.Events.Single(e => e.Message.Contains("moegliche Dublette")).Message;
        w.Should().Contain("Loysch GmbH Melk").And.Contain("K31013").And.Contain("OHNE ADR").And.Contain("K60000");
        w.Should().NotContain("LOYSCH MC", "der Konto-Matchcode zaehlt nicht mehr, wenn eine Adresse da ist");
    }

    [Fact]
    public async Task Kreditoren_SageWertZuLang_NichtUebernommen_LaufLaeuft()
    {
        using var ctx = TestDbContextFactory.Create();
        var k = Kred("Alt", "K1", StorageLocationSource.Sage);
        k.PostalCode = "4020";
        ctx.Creditors.Add(k);
        await ctx.SaveChangesAsync();
        var client = Client(KontokorrentSyncService.Ressource, new SdataKontoZeile("K1", "MC", null, null, null, true, Adresse: 1));
        Setup(client, KontokorrentSyncService.RessourceAdressen,
            new SdataAdresseZeile(1, "Firma", null, "Weg 1", new string('9', 21), "Linz", "AT", null, new string('a', 196) + "@x.at"));
        Setup<SdataAnsprechpartnerZeile>(client, KontokorrentSyncService.RessourceAnsprechpartner);
        var log = new FakeSyncLogger();

        await Konto(ctx, log, client).RunKreditorenAsync(false, ziel: Ziel);

        var r = ctx.Creditors.Single();
        (r.Name, r.Street, r.PostalCode, r.Email).Should().Be(("Firma", "Weg 1", "4020", (string?)null),
            "zu lange Werte (PLZ 21 > 20, E-Mail 200+ > 200) nicht schreiben, alter Wert bleibt; Rest wird uebernommen");
        var lauf = log.Runs.Single();
        lauf.FinishedFailed.Should().BeFalse();
        lauf.Events.Should().Contain(e => e.Level == "Warning" && e.Message.Contains("2 Sage-Wert(e) laenger") && e.Message.Contains("K1 PLZ (21 > 20)")
            && e.Message.Contains("K1 E-Mail"));
    }

    [Fact]
    public async Task Kreditoren_AdressenNichtLesbar_NameBleibt()
    {
        using var ctx = TestDbContextFactory.Create();
        var k = Kred("Huber Pulver GmbH", "K1", StorageLocationSource.Sage);
        k.Street = "Alt 1";
        ctx.Creditors.Add(k);
        await ctx.SaveChangesAsync();
        var client = Client(KontokorrentSyncService.Ressource, new SdataKontoZeile("K1", "HUBER", null, null, "G9", true, Adresse: 1));
        client.Setup(c => c.QueryAllAsync<SdataAdresseZeile>(It.IsAny<SdataLeseZiel>(), It.IsAny<string>(), KontokorrentSyncService.RessourceAdressen,
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ThrowsAsync(new SdataLeseException("Adressen", 500, "Ungueltiger Spaltenname"));
        var log = new FakeSyncLogger();

        await Konto(ctx, log, client).RunKreditorenAsync(false, ziel: Ziel);

        var r = ctx.Creditors.Single();
        (r.Name, r.Street, r.SageGruppe).Should().Be(("Huber Pulver GmbH", "Alt 1", "G9"), "ohne Adressen kein Namenswechsel auf den Matchcode");
        var lauf = log.Runs.Single();
        lauf.FinishedFailed.Should().BeFalse();
        lauf.Events.Should().Contain(e => e.Level == "Warning" && e.Message.Contains("Adressen") && e.Message.Contains("Ungueltiger Spaltenname"));
    }

    // ----- Debitoren -----

    [Fact]
    public async Task Debitoren_Abfrage_Vertreter_EigeneTabelle()
    {
        using var ctx = TestDbContextFactory.Create();
        var client = Client(KontokorrentSyncService.Ressource, new SdataKontoZeile("D10000", "Kunde AG", "ATU5", "30T", "KG", true, Vertreter: " V1 ", Adresse: 3));
        Setup(client, KontokorrentSyncService.RessourceAdressen,
            new SdataAdresseZeile(3, "Kunde AG", null, "Weg 2", "1010", "Wien", "AT", null, "k@kunde.at", USER_KundenNameFA: " KUNDE "));
        var log = new FakeSyncLogger();

        await Konto(ctx, log, client).RunDebitorenAsync(false, ziel: Ziel);

        client.Verify(c => c.QueryAllAsync<SdataKontoZeile>(Ziel, SageSdataReadClient.VertragResources, "Kontokorrent", "KtoArt eq 'D'",
            "Kto,Matchcode,EUUStID,Zahlungskond,Gruppe,Aktiv,Adresse,Vertreter", "Kto", It.IsAny<CancellationToken>(), It.IsAny<int>()), Times.Once);
        var d = ctx.Debitors.Single();
        (d.SageKto, d.Name, d.Source, d.SageVertreter, d.SageGruppe, d.IsActive)
            .Should().Be(("D10000", "Kunde AG", StorageLocationSource.Sage, "V1", "KG", true));
        (d.Street, d.City, d.Email, d.SageKundenNameFA, d.Matchcode).Should().Be(("Weg 2", "Wien", "k@kunde.at", "KUNDE", "Kunde AG"));
        client.Verify(c => c.QueryAllAsync<SdataAdresseZeile>(Ziel, SageSdataReadClient.VertragResources, "Adressen", null,
            KontokorrentSyncService.SelectAdressenDebitoren, "Adresse", It.IsAny<CancellationToken>(), It.IsAny<int>()), Times.Once);
        ctx.Creditors.Should().BeEmpty();
        log.Runs.Single().ServiceName.Should().Be(SyncLogServices.Debitoren);
    }

    // ----- Artikel-Lieferanten -----

    private static Article Art(string nr) => new() { ArticleNumber = nr, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };

    private static ArtikelLieferantSyncService Al(IdealAkeWms.Data.ApplicationDbContext ctx, FakeSyncLogger log,
        SdataArtikelLieferantZeile[] zeilen, params SdataHauptlieferantZeile[] haupt)
    {
        var client = new Mock<ISageSdataReadClient>();
        Setup(client, ArtikelLieferantSyncService.Ressource, zeilen);
        Setup(client, ArtikelLieferantSyncService.RessourceVarianten, haupt);
        return new(ctx, client.Object, new ConfigurationBuilder().Build(), NullLogger<ArtikelLieferantSyncService>.Instance, log);
    }

    private static SdataArtikelLieferantZeile Zeile(string? artikel, string? lieferant, decimal preis = 1.23456m) =>
        new(lieferant, artikel, "B-" + artikel, "Bez1", null, 10m, "Stk", 1m, 14, preis);

    [Fact]
    public async Task ArtikelLieferant_VariantenLeer_Warnung_HauptlieferantBleibt()
    {
        using var ctx = TestDbContextFactory.Create();
        var a1 = Art("00722001"); var k1 = Kred("Huber", "K31013", StorageLocationSource.Sage);
        ctx.AddRange(a1, k1);
        await ctx.SaveChangesAsync();
        ctx.ArticleSuppliers.Add(new ArticleSupplier { ArticleId = a1.Id, CreditorId = k1.Id, Artikelnummer = "00722001", IstHauptlieferant = true,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        await ctx.SaveChangesAsync();
        var log = new FakeSyncLogger();

        await Al(ctx, log, new[] { Zeile("00722001", "K31013") }).RunAsync(dryRun: false, ziel: Ziel);

        ctx.ArticleSuppliers.Single().IstHauptlieferant.Should().BeTrue("0 Varianten = Filter kaputt: Kennzeichen nicht auf false setzen");
        log.Runs.Single().Events.Should().Contain(e => e.Level == "Warning" && e.Message.Contains("0 Zeilen mit Hauptlieferant")
            && e.Message.Contains("like '_%'"));
    }

    [Fact]
    public async Task ArtikelLieferant_Upsert_Hauptlieferant_Ueberspringen_Entfernen()
    {
        using var ctx = TestDbContextFactory.Create();
        var a1 = Art("00722001"); var a2 = Art("00722002"); var a3 = Art("00722003");
        var k1 = Kred("Huber", "K31013", StorageLocationSource.Sage); var k2 = Kred("Maier", "K31014", StorageLocationSource.Sage);
        ctx.AddRange(a1, a2, a3, k1, k2);
        await ctx.SaveChangesAsync();
        ctx.ArticleSuppliers.AddRange(
            new ArticleSupplier { ArticleId = a2.Id, CreditorId = k2.Id, Artikelnummer = "00722002", Einzelpreis = 9m, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" },
            new ArticleSupplier { ArticleId = a3.Id, CreditorId = k1.Id, Artikelnummer = "00722003", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        await ctx.SaveChangesAsync();
        var log = new FakeSyncLogger();

        await Al(ctx, log, new[]
            {
                Zeile("00722001 ", "K31013"),
                Zeile("00722001", "K31013", 7m),   // gleiches Paar (z. B. zweite Auspraegung) -> uebersprungen
                Zeile("00722002", "K31014", 5m),   // vorhanden -> aktualisiert
                Zeile("99999999", "K31013"),       // Artikel nicht im WMS
                Zeile("00722001", "K99999"),       // Kreditor fehlt
                Zeile(null, "K31013"),
            },
            new SdataHauptlieferantZeile("00722001", "k31013 ")).RunAsync(dryRun: false, ziel: Ziel);

        var s1 = ctx.ArticleSuppliers.Single(s => s.ArticleId == a1.Id);
        (s1.CreditorId, s1.IstHauptlieferant, s1.SageBestellnummer, s1.Einzelpreis, s1.Wiederbeschaffungszeit, s1.CreatedBy)
            .Should().Be((k1.Id, true, "B-00722001", (decimal?)1.2346m, (int?)14, "ArtikelLieferantSync"));
        var s2 = ctx.ArticleSuppliers.Single(s => s.ArticleId == a2.Id);
        (s2.Einzelpreis, s2.IstHauptlieferant, s2.ModifiedBy).Should().Be(((decimal?)5m, false, "ArtikelLieferantSync"));
        ctx.ArticleSuppliers.Any(s => s.ArticleId == a3.Id).Should().BeFalse("liefert Sage nicht mehr -> entfernt");

        var lauf = log.Runs.Single(r => r.ServiceName == SyncLogServices.ArtikelLieferant);
        lauf.FinalCounts.Should().BeEquivalentTo(new Dictionary<string, int>
        {
            ["gelesen"] = 6, ["neu"] = 1, ["aktualisiert"] = 1, ["unveraendert"] = 0, ["entfernt"] = 1, ["hauptlieferant"] = 1,
            ["artikel_fehlt"] = 1, ["kreditor_fehlt"] = 1, ["uebersprungen"] = 3,
        });
        lauf.Events.Should().Contain(e => e.Level == "Warning" && e.Message.Contains("K99999") && e.Message.Contains("Sync:KreditorenEnabled"));
    }

    [Fact]
    public async Task ArtikelLieferant_EmptyGuard_OhneZuordenbareZeile_EntferntNichts()
    {
        using var ctx = TestDbContextFactory.Create();
        var a = Art("1"); var k = Kred("K", "K1", StorageLocationSource.Sage);
        ctx.AddRange(a, k);
        await ctx.SaveChangesAsync();
        ctx.ArticleSuppliers.Add(new ArticleSupplier { ArticleId = a.Id, CreditorId = k.Id, Artikelnummer = "1", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        await ctx.SaveChangesAsync();
        var log = new FakeSyncLogger();

        await Al(ctx, log, new[] { Zeile("1", "K-UNBEKANNT") }).RunAsync(false, ziel: Ziel);

        ctx.ArticleSuppliers.Should().ContainSingle();
        log.Runs.Single().Events.Should().Contain(e => e.Level == "Warning" && e.Message.Contains("Empty-Guard"));
        ArtikelLieferantSyncService.Select.Should().NotContain("USER_");
    }

    // ----- Gates + DI -----

    [Fact]
    public async Task SyncWorker_GatesAus_OhneDb_KeinLauf()
    {
        var provider = new Mock<IServiceProvider>();
        var scope = new Mock<IServiceScope>();
        scope.Setup(s => s.ServiceProvider).Returns(provider.Object);
        var factory = new Mock<IServiceScopeFactory>();
        factory.Setup(f => f.CreateScope()).Returns(scope.Object);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["WorkerSettings:SyncIntervalMinutes"] = "0",
            ["Sync:KreditorenEnabled"] = "true", // IConfiguration-Seed wirkt nicht (DB-first)
            ["Sync:DebitorenEnabled"] = "true",
            ["Sync:ArtikelLieferantEnabled"] = "true",
        }).Build();

        using var worker = new SyncWorker(Mock.Of<ILogger<SyncWorker>>(), config, factory.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
        await worker.StartAsync(cts.Token);
        await Task.Delay(250);
        await worker.StopAsync(CancellationToken.None);

        provider.Verify(p => p.GetService(typeof(KontokorrentSyncService)), Times.Never);
        provider.Verify(p => p.GetService(typeof(ArtikelLieferantSyncService)), Times.Never);
    }

    [Fact]
    public void DI_BeideAbgleicheLoesenAuf()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddSingleton(new Mock<ISyncLogger>().Object);
        services.AddScoped(_ => TestDbContextFactory.Create());
        services.AddSageSdataReadClient();
        services.AddScoped<KontokorrentSyncService>();
        services.AddScoped<ArtikelLieferantSyncService>();
        using var sp = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using var scope = sp.CreateScope();

        scope.ServiceProvider.GetRequiredService<KontokorrentSyncService>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<ArtikelLieferantSyncService>().Should().NotBeNull();
        SyncLogServices.All.Should().Contain(new[] { SyncLogServices.Kreditoren, SyncLogServices.Debitoren, SyncLogServices.ArtikelLieferant });
        SyncLogServices.DisplayNames.Should().ContainKeys(SyncLogServices.Kreditoren, SyncLogServices.Debitoren, SyncLogServices.ArtikelLieferant);
    }
}
