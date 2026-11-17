using System.Text.Json;
using FluentAssertions;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Services.SageSdata;
using IdealAkeWms.Services.SyncLogger;
using IdealAkeWms.Tests.Helpers;
using IDEALAKEWMSService.Services.Sdata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace IDEALAKEWMSService.Tests.Services.Sdata;

/// <summary>Spec 2026-11-13-blechfertigung-bf-auftraege-wicam-csv, V-B: Blechteile-Felder per SData nachlesen (nur GET) und als Merkmale schreiben.</summary>
public class BlechteileFelderNachleseTests
{
    private static readonly SdataLeseZiel Ziel = new("https://sagetest01:5493", "ol", "ideal_TEST;1", "u", "p", true);

    private static Dictionary<string, JsonElement> U(string json) => JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;

    private static Mock<ISageSdataReadClient> Client(Dictionary<string, JsonElement>[] artikel, params SdataArtikelVarianteZeile[] varianten)
    {
        var client = new Mock<ISageSdataReadClient>(MockBehavior.Strict);
        client.Setup(c => c.QueryAllAsync<Dictionary<string, JsonElement>>(Ziel, It.IsAny<string>(), "Artikel", SdataArtikelReader.WhereLagerartikel,
                It.Is<string?>(s => s!.Contains(BfArtikelFelder.Materialnummer)), "Artikelnummer", It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ReturnsAsync(new SdataSeiten<Dictionary<string, JsonElement>>(artikel, "Artikel", 1, artikel.Length, 1));
        client.Setup(c => c.QueryAllAsync<SdataArtikelVarianteZeile>(Ziel, It.IsAny<string>(), "ArtikelVariantenExtended", It.IsAny<string?>(),
                "Artikelnummer,AuspraegungID,Bestellmenge", "Artikelnummer,AuspraegungID", It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ReturnsAsync(new SdataSeiten<SdataArtikelVarianteZeile>(varianten, "ArtikelVariantenExtended", 1, varianten.Length, 1));
        return client;
    }

    private static SdataArtikelVarianteZeile V(string nr, int auspraegung, decimal? menge) =>
        new(nr, auspraegung, null, null, null, null, null, null, null, menge, null, null, null, null, null, null, null);

    private static SdataArtikelReader Reader(Mock<ISageSdataReadClient> c) =>
        new(c.Object, new ConfigurationBuilder().Build(), NullLogger<SdataArtikelReader>.Instance);

    [Fact]
    public async Task Lesen_FelderUndBestellmenge_GrundvarianteEinheitenregel()
    {
        var client = Client(new[]
        {
            U("""{"Artikelnummer":"BG-1 ","Basismengeneinheit":"Stk","Lagermengeneinheit":"Stk","USER_OSIstLagerartikel":-1,"USER_OSArtikeltyp":"BG","USER_OSMaterial":"4301 BLANK","USER_OSMaterialnummerPN":"3","USER_OSFertigungsInfoPN":"Laser"}"""),
            U("""{"Artikelnummer":"FT-2","Basismengeneinheit":"Stk","Lagermengeneinheit":"Stk","USER_OSIstLagerartikel":-1,"USER_OSArtikeltyp":"FT"}"""),
            U("""{"Artikelnummer":"FT-3","Basismengeneinheit":"m","Lagermengeneinheit":"Stk","USER_OSIstLagerartikel":-1,"USER_OSArtikeltyp":"FT"}"""),
        }, V("BG-1", 2, 99m), V("BG-1", 1, 20m), V("FT-2", 1, 0m), V("FT-3", 1, 5m));
        var hinweise = new List<SdataHinweis>();

        var dtos = await Reader(client).BlechteileFelderLesenAsync(Ziel, hinweise, CancellationToken.None);

        var bg = dtos!.Single(d => d.ArticleNumber == "BG-1").Benutzerfelder!;
        (bg[BfArtikelFelder.Artikeltyp], bg[BfArtikelFelder.Materialnummer], bg[BfArtikelFelder.IstLagerartikel], bg[BfArtikelFelder.Bestellmenge])
            .Should().Be(("BG", "3", "-1", "20"), "kleinste AuspraegungID = Grundvariante");
        bg[BfArtikelFelder.Abteilung].Should().BeNull();
        dtos!.Single(d => d.ArticleNumber == "FT-2").Benutzerfelder![BfArtikelFelder.Bestellmenge].Should().BeNull("0 = frei");
        dtos!.Single(d => d.ArticleNumber == "FT-3").Benutzerfelder![BfArtikelFelder.Bestellmenge].Should().BeNull("Basis ≠ Lager = frei");
        hinweise.Should().Contain(h => h.Text.Contains("1 Artikel mit Basis- ≠ Lagermengeneinheit"));
    }

    [Fact]
    public async Task Lesen_NullZeilen_Warnung_NichtsSchreiben()
    {
        var hinweise = new List<SdataHinweis>();
        (await Reader(Client(Array.Empty<Dictionary<string, JsonElement>>())).BlechteileFelderLesenAsync(Ziel, hinweise, CancellationToken.None))
            .Should().BeNull();
        hinweise.Should().Contain(h => h.Warnung && h.Text.Contains("0 Zeilen"));
    }

    [Fact]
    public async Task Schritt_SchreibtMerkmale_EntferntNichtMehrGelieferte()
    {
        var ctx = TestDbContextFactory.Create();
        ctx.Articles.AddRange(
            new Article { Id = 1, ArticleNumber = "BG-1", CreatedBy = "x", CreatedByWindows = "x" },
            new Article { Id = 2, ArticleNumber = "ALT-9", CreatedBy = "x", CreatedByWindows = "x" });
        ctx.SaveChanges();
        var run = new Mock<ISyncRun>();

        // Lauf 1: beide Lagerartikel
        await BlechteileFelderNachlese.SchrittAsync(Reader(Client(new[]
        {
            U("""{"Artikelnummer":"BG-1","USER_OSIstLagerartikel":-1,"USER_OSArtikeltyp":"BG"}"""),
            U("""{"Artikelnummer":"ALT-9","USER_OSIstLagerartikel":-1,"USER_OSArtikeltyp":"FT"}"""),
        }, V("BG-1", 1, 20m))), ctx, run.Object, () => Task.FromResult(Ziel), CancellationToken.None);
        ctx.ArticleAttributeValues.Count(v => v.ArticleId == 2).Should().Be(2);
        ctx.ArticleAttributeValues.Single(v => v.ArticleId == 1 && v.ArticleAttributeDefinition.SyncFieldName == BfArtikelFelder.Bestellmenge)
            .TextValue.Should().Be("20");

        // Lauf 2: ALT-9 ist in Sage kein Lagerartikel mehr -> Merkmale weg, BG-1 bleibt
        await BlechteileFelderNachlese.SchrittAsync(Reader(Client(new[]
        {
            U("""{"Artikelnummer":"BG-1","USER_OSIstLagerartikel":-1,"USER_OSArtikeltyp":"BG"}"""),
        }, V("BG-1", 1, 20m))), ctx, run.Object, () => Task.FromResult(Ziel), CancellationToken.None);
        ctx.ArticleAttributeValues.Count(v => v.ArticleId == 2).Should().Be(0);
        ctx.ArticleAttributeValues.Count(v => v.ArticleId == 1).Should().Be(3);
        run.Verify(r => r.LogWarningAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Schritt_SDataFehler_NurWarnung_MerkmaleUnveraendert()
    {
        var ctx = TestDbContextFactory.Create();
        var client = new Mock<ISageSdataReadClient>();
        client.Setup(c => c.QueryAllAsync<Dictionary<string, JsonElement>>(It.IsAny<SdataLeseZiel>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ThrowsAsync(new SdataLeseException("Artikel", 500, "Feld unbekannt"));
        var run = new Mock<ISyncRun>();

        await BlechteileFelderNachlese.SchrittAsync(Reader(client), ctx, run.Object, () => Task.FromResult(Ziel), CancellationToken.None);

        run.Verify(r => r.LogWarningAsync(It.Is<string>(s => s.Contains("bleibt gültig")), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        ctx.ArticleAttributeDefinitions.Should().BeEmpty();
    }
}
