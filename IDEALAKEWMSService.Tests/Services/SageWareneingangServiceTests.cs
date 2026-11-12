using System.Text.Json;
using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Services.SyncLogger;
using IdealAkeWms.Tests.Helpers;
using IDEALAKEWMSService.Services;
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

    private static SageWareneingangService Svc(ApplicationDbContext db, ISageWareneingangClient client, FakeSyncLogger? log = null) =>
        new(db, client, NullLogger<SageWareneingangService>.Instance, log ?? new FakeSyncLogger());

    [Fact]
    public async Task ZweiPositionenEinAbsenden_EinBeleg_Uebergeben_MitBelegnummer_AK11()
    {
        var (db, _, _) = Db();
        var v = Guid.NewGuid();
        var a = Receipt(db, 1, v, SageWareneingangStatus.Offen);
        Receipt(db, 2, v, SageWareneingangStatus.Offen);
        var client = new FakeClient();
        var log = new FakeSyncLogger();

        await Svc(db, client, log).RunOnceAsync(Endpoint, 10, 50);

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
        await Svc(db, client).RunOnceAsync(Endpoint, 10, 50);
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
    public async Task Fehler_WirdNichtAutomatischWiederholt_AK16()
    {
        var (db, _, _) = Db();
        Receipt(db, 1, Guid.NewGuid(), SageWareneingangStatus.Offen);
        var client = new FakeClient { Antwort = _ => new(false, null, "{\"ErrorText\":\"Artikel gesperrt\"}", "Artikel gesperrt") };

        await Svc(db, client).RunOnceAsync(Endpoint, 10, 50);
        await Svc(db, client).RunOnceAsync(Endpoint, 10, 50);

        client.Gesendet.Should().ContainSingle("Variante B: kein Auto-Retry");
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

        await Svc(db, client).RunOnceAsync(Endpoint, 10, 50);

        client.Gesendet.Should().BeEmpty();
        db.PurchaseOrderReceipts.Single().SageWeStatus.Should().Be(SageWareneingangStatus.Fehler);
        db.PurchaseOrderReceipts.Single().SageWeFehler.Should().StartWith("Ergebnis unklar").And.Contain($"WE#{r.Id}#");
    }

    [Fact]
    public async Task OhneArbeit_KeinProtokollLauf_NurInSageNieGesendet()
    {
        var (db, _, _) = Db();
        Receipt(db, 1, null, SageWareneingangStatus.NurInSage);
        Receipt(db, 2, null, SageWareneingangStatus.NichtNoetig);
        var client = new FakeClient();
        var log = new FakeSyncLogger();
        await Svc(db, client, log).RunOnceAsync(Endpoint, 10, 50);
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
