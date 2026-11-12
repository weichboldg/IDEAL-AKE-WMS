using System.Text.Json;
using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Services.SyncLogger;
using IdealAkeWms.Tests.Helpers;
using IDEALAKEWMSService.Services;
using IDEALAKEWMSService.Services.Sdata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IDEALAKEWMSService.Tests.Services;

/// <summary>v1.99.4 (Spec 2026-11-08-wareneingang-teil-2-sage-we-beleg-rahmenabrufe E4, AK 11-13, 16, 17): Payload, Sende-Lauf Variante B, DI.</summary>
public class SageWareneingangServiceTests
{
    private static readonly SageBookingEndpoint Endpoint = new("https://sagetest01.ake.at:5493", "ol", "CommonWawiServices", "ake_TEST2026;1", "u", "p");

    private sealed class FakeClient : ISageWareneingangClient
    {
        public List<SageWareneingangRequest> Gesendet { get; } = new();
        public Func<SageWareneingangRequest, SageWareneingangSendResult> Antwort { get; set; } = _ => new(true, "WE-2026-1", "{}", null);
        public Task<SageWareneingangSendResult> SendAsync(SageWareneingangRequest request, SageBookingEndpoint endpoint, CancellationToken ct = default)
        {
            Gesendet.Add(request);
            return Task.FromResult(Antwort(request));
        }
    }

    /// <summary>v1.99.7 E5: Sage-Suche ohne SData. Wirft, wenn <see cref="Wirft"/> gesetzt (Sage nicht erreichbar).</summary>
    private sealed class FakeLookup : ISageWareneingangLookup
    {
        public List<SageWeBelegTreffer> Memo { get; } = new();
        public List<SageWeBelegTreffer> Kandidaten { get; } = new();
        public bool Wirft { get; set; }
        public int MemoAufrufe { get; private set; }
        public int KandidatenAufrufe { get; private set; }
        public (int BelId, DateTime Ab, string? Ls)? LetzteKandidatenSuche { get; private set; }
        public Task<IReadOnlyList<SageWeBelegTreffer>> NachMemoAsync(int ersteReceiptId, CancellationToken ct)
        {
            MemoAufrufe++;
            if (Wirft) throw new IdealAkeWms.Services.SageSdata.SdataLeseException("EinkaufBelegeInfo", 503, "Sage nicht erreichbar");
            return Task.FromResult<IReadOnlyList<SageWeBelegTreffer>>(Memo);
        }
        public Task<IReadOnlyList<SageWeBelegTreffer>> KandidatenAsync(int bestellBelId, DateTime abDatum, string? lieferschein, CancellationToken ct)
        {
            KandidatenAufrufe++;
            LetzteKandidatenSuche = (bestellBelId, abDatum, lieferschein);
            return Task.FromResult<IReadOnlyList<SageWeBelegTreffer>>(Kandidaten);
        }
    }

    private static readonly SageWeWiederholung Wdh = new(3, 15);

    private static (ApplicationDbContext db, int sageLoc, int wmsLoc) Db()
    {
        var db = TestDbContextFactory.Create();
        db.Articles.Add(new Article { Id = 5, ArticleNumber = "00722001", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        db.StorageLocations.AddRange(
            new StorageLocation { Id = 9, Code = "HR 0 0 0", SageBuchungErlaubt = true, SageLagerkennung = "HR;0;0;0", SageLagerplatzId = 77, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" },
            new StorageLocation { Id = 10, Code = "WMS-1", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        db.SaveChanges();
        return (db, 9, 10);
    }

    private static PurchaseOrderReceipt Receipt(ApplicationDbContext db, int belPosId, Guid? vorgang, SageWareneingangStatus status, int loc = 9,
        string? lieferschein = "LS-4711", decimal menge = 2m)
    {
        var m = new StockMovement
        {
            ArticleId = 5, StorageLocationId = loc, Quantity = menge, MovementType = MovementType.Einbuchung, Timestamp = DateTime.Now, WindowsUser = "t",
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t",
            PurchaseOrderReceipt = new PurchaseOrderReceipt
            {
                BelId = 500, BelPosId = belPosId, Belegnummer = "2026-500", LieferantKto = "70001", Quantity = menge,
                Lieferart = PurchaseOrderReceiptLieferart.Teillieferung, SageWeStatus = status, SageWeVorgang = vorgang, Lieferschein = lieferschein,
                CreatedAt = new DateTime(2026, 11, 11, 9, 0, 0), CreatedBy = "t", CreatedByWindows = "t",
            },
        };
        db.StockMovements.Add(m);
        db.SaveChanges();
        return m.PurchaseOrderReceipt!;
    }

    private static SageWareneingangService Svc(ApplicationDbContext db, ISageWareneingangClient client, FakeSyncLogger? log = null, FakeLookup? lookup = null) =>
        new(db, client, lookup ?? new FakeLookup(), NullLogger<SageWareneingangService>.Instance, log ?? new FakeSyncLogger());

    /// <summary>Fehler nach einem echten Versuch, aelter als der Abstand.</summary>
    private static PurchaseOrderReceipt FehlerNachVersuch(ApplicationDbContext db, int versuche = 1, string? lieferschein = "LS-4711")
    {
        var r = Receipt(db, 1, Guid.NewGuid(), SageWareneingangStatus.Fehler, lieferschein: lieferschein);
        r.SageWeVersuche = versuche; r.SageWeFehler = "Ergebnis unklar"; r.ModifiedAt = DateTime.Now.AddMinutes(-30);
        db.SaveChanges();
        return r;
    }

    [Fact]
    public async Task ZweiPositionenEinAbsenden_EinBeleg_Uebergeben_MitBelegnummer_AK11()
    {
        var (db, _, _) = Db();
        var v = Guid.NewGuid();
        var a = Receipt(db, 1, v, SageWareneingangStatus.Offen);
        Receipt(db, 2, v, SageWareneingangStatus.Offen);
        var client = new FakeClient();
        var log = new FakeSyncLogger();

        await Svc(db, client, log).RunOnceAsync(Endpoint, 10, 50, Wdh);

        client.Gesendet.Should().ContainSingle();
        client.Gesendet[0].StandardBelegPositionen.Select(p => p.PositionHandle).Should().Equal(1, 2);
        db.PurchaseOrderReceipts.Should().OnlyContain(r => r.SageWeStatus == SageWareneingangStatus.Uebergeben && r.SageWeBelegnummer == "WE-2026-1"
            && r.SageWeAm != null && r.SageWeVersuche == 1 && r.ModifiedBy == SageWareneingang.ServiceActor);
        log.Runs.Single().ServiceName.Should().Be(SyncLogServices.SageWareneingang);
        log.Runs.Single().FinalCounts!["uebergeben"].Should().Be(1);
        client.Gesendet[0].Memo.Should().Be($"WE#{a.Id}#");
    }

    [Fact]
    public async Task ZweiAbsenden_ZweiBelege_AK12()
    {
        var (db, _, _) = Db();
        Receipt(db, 1, Guid.NewGuid(), SageWareneingangStatus.Offen);
        Receipt(db, 1, Guid.NewGuid(), SageWareneingangStatus.Offen);
        var client = new FakeClient();
        await Svc(db, client).RunOnceAsync(Endpoint, 10, 50, Wdh);
        client.Gesendet.Should().HaveCount(2);
    }

    [Fact]
    public void Payload_Kopf_Lieferschein_Memo_Lagermenge_AK13()
    {
        var (db, _, _) = Db();
        var v = Guid.NewGuid();
        var a = Receipt(db, 1, v, SageWareneingangStatus.Offen, lieferschein: " LS-4711 ");
        var gruppe = db.PurchaseOrderReceipts.Where(r => r.SageWeVorgang == v).ToList();
        foreach (var r in gruppe) { db.Entry(r).Reference(x => x.StockMovement).Load(); db.Entry(r.StockMovement).Reference(m => m.Article).Load(); db.Entry(r.StockMovement).Reference(m => m.StorageLocation).Load(); }

        var p = SageWareneingangPayloadBuilder.Build(gruppe);

        (p.Erfassungsart, p.BelegTransformArt, p.IsBelegTransform, p.BelegKennzeichen).Should().Be((4100, 1, true, "ELL"));
        p.TransformSettings.Single().SourceBelegHandle.Should().Be(500);
        (p.A0Konto, p.EKLieferschein, p.Memo, p.Belegdatum).Should().Be(("70001", "LS-4711", $"WE#{a.Id}#", "2026-11-11"));
        var pos = p.StandardBelegPositionen.Single();
        (pos.Artikelnummer, pos.MengeBasis, pos.Positionstyp, pos.Mengenart).Should().Be(("00722001", 2m, 1, 1));
        pos.BelegPositionLagermengen.Single().Should().BeEquivalentTo(new SageWeLagermenge { PlatzId = 77, Kurzbezeichnung = "HR;0;0;0", MengeBasis = 2m });
        pos.Artikelmemo.Should().Be($"WE#{a.Id}# LS LS-4711 Platz HR;0;0;0", "v1.99.5: Memo je Position");
        SageWareneingangPayloadBuilder.PositionsMemo(7, null, "GL;0;0;0").Should().Be("WE#7# Platz GL;0;0;0");
        // v1.99.6: Eingabe des Lagers vorne, Kennung dahinter; leer = nur Kennung
        SageWareneingangPayloadBuilder.PositionsMemo(7, "WMS-TEST-2", "GL;0;0;0", " Kante beschädigt ")
            .Should().Be("Kante beschädigt | WE#7# LS WMS-TEST-2 Platz GL;0;0;0");
        SageWareneingangPayloadBuilder.PositionsMemo(7, "WMS-TEST-2", "GL;0;0;0", "  ").Should().Be("WE#7# LS WMS-TEST-2 Platz GL;0;0;0");
        gruppe[0].StockMovement.Note = "Kante beschädigt";
        SageWareneingangPayloadBuilder.Build(gruppe).StandardBelegPositionen.Single().Artikelmemo.Should().StartWith("Kante beschädigt | WE#");
        SageWareneingangClient.Serialize(p).Should().Contain("\"EKLieferschein\":\"LS-4711\"");
    }

    [Fact]
    public void Payload_OhneLieferschein_FeldNichtGesendet_NichtSagePlatz_Fehler_AK13()
    {
        var (db, _, wms) = Db();
        var v = Guid.NewGuid();
        Receipt(db, 1, v, SageWareneingangStatus.Offen, lieferschein: null);
        var gruppe = db.PurchaseOrderReceipts.ToList();
        foreach (var r in gruppe) { db.Entry(r).Reference(x => x.StockMovement).Load(); db.Entry(r.StockMovement).Reference(m => m.Article).Load(); db.Entry(r.StockMovement).Reference(m => m.StorageLocation).Load(); }
        JsonDocument.Parse(SageWareneingangClient.Serialize(SageWareneingangPayloadBuilder.Build(gruppe))).RootElement
            .TryGetProperty("EKLieferschein", out _).Should().BeFalse("leer = Feld nicht senden");

        gruppe[0].StockMovement.StorageLocation = db.StorageLocations.Find(wms)!;
        var act = () => SageWareneingangPayloadBuilder.Build(gruppe);
        act.Should().Throw<SageWareneingangPayloadException>().WithMessage("*WMS-1*kein Sage-Platz*");
    }

    [Fact]
    public async Task Fehler_WirdNichtVorAblaufDesAbstandsWiederholt_AK16()
    {
        var (db, _, _) = Db();
        Receipt(db, 1, Guid.NewGuid(), SageWareneingangStatus.Offen);
        var client = new FakeClient { Antwort = _ => new(false, null, "{\"ErrorText\":\"Artikel gesperrt\"}", "Artikel gesperrt") };

        await Svc(db, client).RunOnceAsync(Endpoint, 10, 50, Wdh);
        await Svc(db, client).RunOnceAsync(Endpoint, 10, 50, Wdh);

        client.Gesendet.Should().ContainSingle("v1.99.7: erst nach dem Abstand (15 min) wird wiederholt");
        var r = db.PurchaseOrderReceipts.Single();
        (r.SageWeStatus, r.SageWeVersuche).Should().Be((SageWareneingangStatus.Fehler, 1));
        r.SageWeFehler.Should().Contain("Artikel gesperrt");
    }

    [Fact]
    public async Task HaengendGesendet_WirdFehlerErgebnisUnklar_NichtNeuGesendet_AK16()
    {
        var (db, _, _) = Db();
        var r = Receipt(db, 1, Guid.NewGuid(), SageWareneingangStatus.Gesendet);
        r.ModifiedAt = DateTime.Now.AddMinutes(-30);
        db.SaveChanges();
        var client = new FakeClient();

        await Svc(db, client).RunOnceAsync(Endpoint, 10, 50, Wdh);

        client.Gesendet.Should().BeEmpty();
        db.PurchaseOrderReceipts.Single().SageWeStatus.Should().Be(SageWareneingangStatus.Fehler);
        db.PurchaseOrderReceipts.Single().SageWeFehler.Should().StartWith("Ergebnis unklar").And.Contain($"WE#{r.Id}#");
    }

    // ---------------- v1.99.7 E5 (Variante A), Q-2 ----------------

    [Fact]
    public async Task E5_MemoGefunden_Uebergeben_OhneSenden()
    {
        var (db, _, _) = Db();
        var r = FehlerNachVersuch(db);
        var client = new FakeClient();
        var lookup = new FakeLookup();
        lookup.Memo.Add(new SageWeBelegTreffer(363226, 2027, 398676, $"WE#{r.Id}#"));
        var log = new FakeSyncLogger();

        await Svc(db, client, log, lookup).RunOnceAsync(Endpoint, 10, 50, Wdh);

        client.Gesendet.Should().BeEmpty("Beleg in Sage gefunden - kein POST");
        var x = db.PurchaseOrderReceipts.Single();
        (x.SageWeStatus, x.SageWeBelegnummer, x.SageWeFehler, x.SageWeVersuche).Should().Be((SageWareneingangStatus.Uebergeben, "2027-398676", (string?)null, 1));
        x.SageWeAm.Should().NotBeNull();
        lookup.KandidatenAufrufe.Should().Be(0);
        log.Runs.Single().FinalCounts!["in Sage gefunden"].Should().Be(1);
    }

    [Fact]
    public async Task E5_KeinTreffer_KeinKandidat_WirdErneutGesendet()
    {
        var (db, _, _) = Db();
        var r = FehlerNachVersuch(db);
        var client = new FakeClient();
        var lookup = new FakeLookup();

        await Svc(db, client, null, lookup).RunOnceAsync(Endpoint, 10, 50, Wdh);

        client.Gesendet.Should().ContainSingle();
        (lookup.MemoAufrufe, lookup.KandidatenAufrufe).Should().Be((1, 1));
        lookup.LetzteKandidatenSuche.Should().Be((500, new DateTime(2026, 11, 11), "LS-4711"));
        var x = db.PurchaseOrderReceipts.Single();
        (x.SageWeStatus, x.SageWeVersuche).Should().Be((SageWareneingangStatus.Uebergeben, 2));
    }

    [Fact]
    public async Task E5_Sicherungsnetz_Treffer_Klaeren_OhneSenden_FremdeKennungZaehltNicht()
    {
        var (db, _, _) = Db();
        var r = FehlerNachVersuch(db);
        var client = new FakeClient();
        var lookup = new FakeLookup();
        lookup.Kandidaten.Add(new SageWeBelegTreffer(1, 2027, 1000, "WE#99999#"));   // anderer WMS-Wareneingang
        lookup.Kandidaten.Add(new SageWeBelegTreffer(2, 2027, 1001, "von Hand geaendert"));

        await Svc(db, client, null, lookup).RunOnceAsync(Endpoint, 10, 50, Wdh);

        client.Gesendet.Should().BeEmpty();
        var x = db.PurchaseOrderReceipts.Single();
        x.SageWeStatus.Should().Be(SageWareneingangStatus.Klaeren);
        x.SageWeFehler.Should().Contain("2027-1001").And.NotContain("2027-1000").And.Contain("Memo fehlt/geändert");

        await Svc(db, client, null, lookup).RunOnceAsync(Endpoint, 10, 50, Wdh);
        client.Gesendet.Should().BeEmpty("Klären wird nie automatisch gesendet");
    }

    [Fact]
    public async Task E5_MehrereTreffer_Fehler_ZaehltVersuch_OhneSenden()
    {
        var (db, _, _) = Db();
        FehlerNachVersuch(db);
        var client = new FakeClient();
        var lookup = new FakeLookup();
        lookup.Memo.AddRange(new[] { new SageWeBelegTreffer(1, 2027, 1, "x"), new SageWeBelegTreffer(2, 2027, 2, "x") });

        await Svc(db, client, null, lookup).RunOnceAsync(Endpoint, 10, 50, Wdh);

        client.Gesendet.Should().BeEmpty();
        var x = db.PurchaseOrderReceipts.Single();
        (x.SageWeStatus, x.SageWeVersuche).Should().Be((SageWareneingangStatus.Fehler, 2));
        x.SageWeFehler.Should().StartWith("Mehrere Sage-Belege").And.Contain("2027-1, 2027-2");
    }

    [Fact]
    public async Task E5_SucheScheitert_NichtGesendet_VersucheUnveraendert()
    {
        var (db, _, _) = Db();
        FehlerNachVersuch(db);
        var client = new FakeClient();

        await Svc(db, client, null, new FakeLookup { Wirft = true }).RunOnceAsync(Endpoint, 10, 50, Wdh);

        client.Gesendet.Should().BeEmpty("ohne Sage-Prüfung wird nie gesendet");
        var x = db.PurchaseOrderReceipts.Single();
        (x.SageWeStatus, x.SageWeVersuche).Should().Be((SageWareneingangStatus.Fehler, 1));
        x.SageWeFehler.Should().Contain("Sage-Prüfung").And.Contain("nicht gesendet");
    }

    [Fact]
    public async Task E5_Grenze_ErreichtOderNull_KeineWiederholung_PayloadFehlerOhneVersuch_Nie()
    {
        var (db, _, _) = Db();
        FehlerNachVersuch(db, versuche: 4);                         // 3 Wiederholungen schon gemacht
        var ohneVersuch = Receipt(db, 2, Guid.NewGuid(), SageWareneingangStatus.Fehler);
        ohneVersuch.ModifiedAt = DateTime.Now.AddHours(-1); db.SaveChanges();   // Payload-Fehler, nie gesendet
        var client = new FakeClient();
        var lookup = new FakeLookup();

        await Svc(db, client, null, lookup).RunOnceAsync(Endpoint, 10, 50, Wdh);
        await Svc(db, client, null, lookup).RunOnceAsync(Endpoint, 10, 50, new SageWeWiederholung(0, 15));

        client.Gesendet.Should().BeEmpty();
        lookup.MemoAufrufe.Should().Be(0);
    }

    [Fact]
    public async Task E5_ErneutSendenVonHand_SuchtMemo_OhneSicherungsnetz()
    {
        var (db, _, _) = Db();
        var r = Receipt(db, 1, Guid.NewGuid(), SageWareneingangStatus.Offen);
        r.SageWeVersuche = 1; db.SaveChanges();                     // „Erneut senden“ setzt Offen, Versuche bleiben
        var client = new FakeClient();
        var lookup = new FakeLookup();
        lookup.Kandidaten.Add(new SageWeBelegTreffer(2, 2027, 1001, "x"));

        await Svc(db, client, null, lookup).RunOnceAsync(Endpoint, 10, 50, Wdh);

        (lookup.MemoAufrufe, lookup.KandidatenAufrufe).Should().Be((1, 0));
        client.Gesendet.Should().ContainSingle("der Mensch hat geprüft");
    }

    [Fact]
    public async Task E5_Erstversand_OhneSuche()
    {
        var (db, _, _) = Db();
        Receipt(db, 1, Guid.NewGuid(), SageWareneingangStatus.Offen);
        var lookup = new FakeLookup();
        await Svc(db, new FakeClient(), null, lookup).RunOnceAsync(Endpoint, 10, 50, Wdh);
        lookup.MemoAufrufe.Should().Be(0);
    }

    [Fact]
    public async Task Q2_Ausgeschaltet_OffenUndFehler_WerdenKlaeren_NichtsGesendet()
    {
        var (db, _, _) = Db();
        Receipt(db, 1, Guid.NewGuid(), SageWareneingangStatus.Offen);
        FehlerNachVersuch(db);
        Receipt(db, 3, Guid.NewGuid(), SageWareneingangStatus.Uebergeben);
        var log = new FakeSyncLogger();

        (await Svc(db, new FakeClient(), log).AusgeschaltetAsync()).Should().Be(2);
        (await Svc(db, new FakeClient(), log).AusgeschaltetAsync()).Should().Be(0);

        db.PurchaseOrderReceipts.Count(r => r.SageWeStatus == SageWareneingangStatus.Klaeren).Should().Be(2);
        db.PurchaseOrderReceipts.Where(r => r.SageWeStatus == SageWareneingangStatus.Klaeren).Should()
            .OnlyContain(r => r.SageWeFehler!.StartsWith("Sage-Wareneingang war ausgeschaltet") && r.ModifiedBy == SageWareneingang.ServiceActor);
        db.PurchaseOrderReceipts.Single(r => r.SageWeFehler != null && r.SageWeFehler.Contains("Vorher: Ergebnis unklar")).Should().NotBeNull();
        log.Runs.Should().ContainSingle("ohne Arbeit kein Protokoll-Eintrag");

        var client = new FakeClient();
        await Svc(db, client).RunOnceAsync(Endpoint, 10, 50, Wdh);
        client.Gesendet.Should().BeEmpty("nach dem Wiedereinschalten nichts automatisch nachschicken");
    }

    [Fact]
    public void Lookup_Filter_EngUndKodierbar()
    {
        SdataWareneingangLookupReader.WhereMemo(7).Should().Be("Belegkennzeichen eq 'ELL' and Memo like '%WE#7#%'");
        SdataWareneingangLookupReader.WhereKandidaten(362313, new DateTime(2026, 11, 11, 14, 5, 0), " O'Neil ")
            .Should().Be("Belegkennzeichen eq 'ELL' and ReferenzBelID eq 362313 and Belegdatum ge @2026-11-11@ and Lieferschein eq 'O''Neil'");
        SdataWareneingangLookupReader.WhereKandidaten(1, new DateTime(2026, 11, 11), null).Should().NotContain("Lieferschein");
        SdataWareneingangLookupReader.FremdeKennung("WE#7#", 7).Should().BeFalse();
        SdataWareneingangLookupReader.FremdeKennung("x WE#70# y", 7).Should().BeTrue();
        SdataWareneingangLookupReader.FremdeKennung(null, 7).Should().BeFalse();
        new SageWeBelegTreffer(1, 2027, 398675, null).Anzeige.Should().Be("2027-398675");
    }

    [Fact]
    public async Task OhneArbeit_KeinProtokollLauf_NurInSageNieGesendet()
    {
        var (db, _, _) = Db();
        Receipt(db, 1, null, SageWareneingangStatus.NurInSage);
        Receipt(db, 2, null, SageWareneingangStatus.NichtNoetig);
        var client = new FakeClient();
        var log = new FakeSyncLogger();
        await Svc(db, client, log).RunOnceAsync(Endpoint, 10, 50, Wdh);
        client.Gesendet.Should().BeEmpty();
        log.Runs.Should().BeEmpty();
    }

    [Fact]
    public void Antwort_Auswerten_ErrorTextIstFehler_BelegnummerUebernommen()
    {
        SageWareneingangClient.Auswerten("{\"BelegnummerFormatiert\":\"2026-WE-17\",\"ErrorText\":\"\",\"ResultNumber\":0}")
            .Should().BeEquivalentTo(new { Success = true, Belegnummer = "2026-WE-17" });
        SageWareneingangClient.Auswerten("{\"ErrorText\":\"Platz unbekannt\"}").Success.Should().BeFalse();
        SageWareneingangClient.Auswerten("<html>").Success.Should().BeFalse();
        // QA 2026-11-12: 2xx ohne Belegnummer = Ergebnis unklar (Fehler), in beiden Formaten gleich
        SageWareneingangClient.Auswerten("").Success.Should().BeFalse();
        SageWareneingangClient.Auswerten("{\"ErrorText\":\"\"}").Success.Should().BeFalse();
        SageWareneingangClient.Auswerten("<response><ErrorText></ErrorText></response>").Success.Should().BeFalse();
        SageWareneingangClient.BuildServiceUrl(Endpoint)
            .Should().Be("https://sagetest01.ake.at:5493/sdata/ol/CommonWawiServices/ake_TEST2026;1/$service/StandardBelegService");
    }

    [Fact]
    public void DI_LoestAuf_LogNameRegistriert_AK17()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddSingleton(new Mock<ISyncLogger>().Object);
        services.AddScoped(_ => TestDbContextFactory.Create());
        services.AddHttpClient<ISageWareneingangClient, SageWareneingangClient>();
        services.AddScoped<SageWareneingangService>();
        services.AddScoped<IdealAkeWms.Services.SageSdata.ISageSdataReadClient>(_ => new Mock<IdealAkeWms.Services.SageSdata.ISageSdataReadClient>().Object);
        services.AddScoped<ISageWareneingangLookup, SdataWareneingangLookupReader>();
        using var sp = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using var s = sp.CreateScope();
        s.ServiceProvider.GetRequiredService<SageWareneingangService>().Should().NotBeNull();
        SyncLogServices.All.Should().Contain(SyncLogServices.SageWareneingang);
        SyncLogServices.DisplayNames[SyncLogServices.SageWareneingang].Should().Be("Sage-Wareneingang");
        typeof(SageWareneingangService).GetConstructors().Single().GetParameters().Last().ParameterType.Should().Be(typeof(ISyncLogger));
    }

    /// <summary>Echte Antwort des Schreibtests 2026-11-12 (sagetest01/ake_TEST2026, Beleg 2027-398675): Sage liefert ohne format=json Atom-XML.
    /// Vor dem Fix wertete der Client sie als „nicht lesbar“ -> Fehler, obwohl der Beleg angelegt war.</summary>
    private const string SchreibtestAntwort = """
<entry xmlns:sdata="http://schemas.sage.com/sdata/2008/1" xmlns:http="http://schemas.sage.com/sdata/http/2008/1" xmlns:xs="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns="http://www.w3.org/2005/Atom"><id>https://sagetest01.ake.at:5493/sdata/ol/CommonWawiServices/ake_TEST2026;1/$service/StandardBelegService</id><title>StandardBelegService</title><updated>2026-11-12T08:28:51+01:00</updated><link rel="http://schemas.sage.com/sdata/link-relations/schema" type="application/xml" title="Schema" href="https://sagetest01.ake.at:5493/sdata/ol/CommonWawiServices/ake_TEST2026;1/$service/StandardBelegService/$schema" /><link rel="http://schemas.sage.com/sdata/link-relations/service" type="application/atom+xml;type=feed" title="Service" href="https://sagetest01.ake.at:5493/sdata/ol/CommonWawiServices/ake_TEST2026;1///$service" /><category term="response" label="Response from a service operation" scheme="http://schemas.sage.com/sdata/categories" /><sdata:payload><StandardBelegService xmlns="http://schemas.dpsbs.de/CommonWawiServices/2019"><response><Handle>363225</Handle><VorgangHandle>66927</VorgangHandle><BelegnummerFormatiert>2027-398675</BelegnummerFormatiert><Projektnummer></Projektnummer><ResultNumber>200</ResultNumber><ResultText>Ok</ResultText><ErrorText></ErrorText><ResponseBelegPositionen sdata:url="https://sagetest01.ake.at:5493/sdata/ol/CommonWawiServices/ake_TEST2026;1/$service/StandardBelegService('Handle:=0;BelegnummerFormatiert:=;')/ResponseBelegPositionen"><ResponseBelegPosition sdata:lookup="https://sagetest01.ake.at:5493/sdata/ol/CommonWawiServices/ake_TEST2026;1/ResponseBelegPositionen"><Handle>406344</Handle><VorgangPositionHandle>246324</VorgangPositionHandle><Artikelnummer>EK001640</Artikelnummer><AuspraegungsHandle>0</AuspraegungsHandle><ResultText></ResultText><BacklogPrimaryKeys /></ResponseBelegPosition></ResponseBelegPositionen><ChildResponseWithHandleResults sdata:url="https://sagetest01.ake.at:5493/sdata/ol/CommonWawiServices/ake_TEST2026;1/$service/StandardBelegService('Handle:=0;BelegnummerFormatiert:=;')/ChildResponseWithHandleResults"><ChildResponseWithHandleResult sdata:lookup="https://sagetest01.ake.at:5493/sdata/ol/CommonWawiServices/ake_TEST2026;1/ChildResponseWithHandleResults"><ChildPrimaryKeys><ChildPrimaryKey sdata:lookup="https://sagetest01.ake.at:5493/sdata/ol/CommonWawiServices/ake_TEST2026;1/ChildPrimaryKeys"><PrimaryKeyName>Handle</PrimaryKeyName><PrimaryKeyValue>406344</PrimaryKeyValue><PrimaryKeyType>System.Int32</PrimaryKeyType></ChildPrimaryKey><ChildPrimaryKey sdata:lookup="https://sagetest01.ake.at:5493/sdata/ol/CommonWawiServices/ake_TEST2026;1/ChildPrimaryKeys"><PrimaryKeyName>Artikelnummer</PrimaryKeyName><PrimaryKeyValue>EK001640</PrimaryKeyValue><PrimaryKeyType>System.String</PrimaryKeyType></ChildPrimaryKey><ChildPrimaryKey sdata:lookup="https://sagetest01.ake.at:5493/sdata/ol/CommonWawiServices/ake_TEST2026;1/ChildPrimaryKeys"><PrimaryKeyName>AuspraegungsHandle</PrimaryKeyName><PrimaryKeyValue>0</PrimaryKeyValue><PrimaryKeyType>System.Int32</PrimaryKeyType></ChildPrimaryKey><ChildPrimaryKey sdata:lookup="https://sagetest01.ake.at:5493/sdata/ol/CommonWawiServices/ake_TEST2026;1/ChildPrimaryKeys"><PrimaryKeyName>VorgangPositionHandle</PrimaryKeyName><PrimaryKeyValue>246324</PrimaryKeyValue><PrimaryKeyType>System.Int32</PrimaryKeyType></ChildPrimaryKey></ChildPrimaryKeys><ResultNumber>200</ResultNumber><ResultText></ResultText><ErrorText></ErrorText><BacklogPrimaryKeys /></ChildResponseWithHandleResult></ChildResponseWithHandleResults></response></StandardBelegService></sdata:payload></entry>
""";

    [Fact]
    public void Antwort_SdataAtomXml_Schreibtest_IstErfolg_MitBelegnummer()
    {
        var r = SageWareneingangClient.Auswerten(SchreibtestAntwort);
        (r.Success, r.Belegnummer, r.Error).Should().Be((true, "2027-398675", (string?)null));
        SageWareneingangClient.Auswerten(SchreibtestAntwort.Replace("<ErrorText></ErrorText><ResponseBelegPositionen", "<ErrorText>Position gesperrt</ErrorText><ResponseBelegPositionen"))
            .Should().BeEquivalentTo(new { Success = false, Error = "Position gesperrt" });
    }
}
