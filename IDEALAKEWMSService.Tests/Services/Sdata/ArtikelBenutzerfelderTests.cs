using System.Text.Json;
using FluentAssertions;
using IdealAkeWms.Models;
using IdealAkeWms.Services.SageSdata;
using IdealAkeWms.Tests.Helpers;
using IDEALAKEWMSService.Services;
using IDEALAKEWMSService.Services.Sdata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace IDEALAKEWMSService.Tests.Services.Sdata;

/// <summary>Spec 2026-11-06-sage-sdata-stammdaten-sync, E3b: Benutzerfelder lesen (SData) und als Text-Merkmal schreiben.</summary>
public class ArtikelBenutzerfelderTests
{
    private static readonly SdataLeseZiel Ziel = new("https://sagetest01:5493", "ol", "ake_TEST2026;1", "u", "p", true);

    // ----- Liste -----

    [Fact]
    public void Parsen_GueltigUngueltigDoppelt()
    {
        var (g, u) = SdataArtikelReader.BenutzerfelderParsen(" USER_Maschine, USER_MASCHINE ,USER_Ventil,Bezeichnung1,USER_X;DROP,, ");
        g.Should().Equal("USER_Maschine", "USER_Ventil");
        u.Should().Equal("Bezeichnung1", "USER_X;DROP");
        SdataArtikelReader.BenutzerfelderParsen(null).Gueltig.Should().BeEmpty();
    }

    [Fact]
    public void DefaultListe_15Felder_AlleGueltig()
    {
        var roh = ServiceSettingDefinitions.All.Single(d => d.Key == SdataArtikelReader.BenutzerfelderKey).DefaultValue;
        var (g, u) = SdataArtikelReader.BenutzerfelderParsen(roh);
        (g.Count, u.Count).Should().Be((15, 0));
    }

    [Fact]
    public void AlsText_StringZahlBoolNull()
    {
        static JsonElement J(string json) => JsonDocument.Parse(json).RootElement;
        SdataArtikelReader.AlsText(J("\" M-200 \"")).Should().Be("M-200");
        SdataArtikelReader.AlsText(J("\"  \"")).Should().BeNull();
        SdataArtikelReader.AlsText(J("-1")).Should().Be("-1");
        SdataArtikelReader.AlsText(J("null")).Should().BeNull();
        SdataArtikelReader.AlsText(default).Should().BeNull("Feld fehlt in der Antwort");
    }

    // ----- Reader -----

    private static SdataArtikelZeile Artikel(string nr) =>
        new(nr, "Teil", null, null, "940", null, null, null, null, null, "Stk", null, null, null, null, null, null, null,
            true, null, null, null, null, true, null, null, null, null, null, null, null, null, null, null, null);

    private static Dictionary<string, JsonElement> U(string json) =>
        JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;

    private static Mock<ISageSdataReadClient> Client()
    {
        var client = new Mock<ISageSdataReadClient>();
        static SdataSeiten<T> S<T>(string r, params T[] z) => new(z, r, 1, z.Length, 1);
        client.Setup(c => c.QueryAllAsync<SdataArtikelZeile>(Ziel, It.IsAny<string>(), "Artikel", SdataArtikelReader.WhereBestellartikel,
                It.IsAny<string?>(), "Artikelnummer", It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ReturnsAsync(S("Artikel", Artikel("A-1"), Artikel("B-2")));
        client.Setup(c => c.QueryAllAsync<SdataRessourceZeile>(Ziel, It.IsAny<string>(), "PpsRessourcenPositionenInfo", It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ReturnsAsync(S("PpsRessourcenPositionenInfo", new SdataRessourceZeile("R-OHNE-STAMM")));
        client.Setup(c => c.QueryAllAsync<SdataArtikelZeile>(Ziel, It.IsAny<string>(), "Artikel", "Artikelnummer in ('R-OHNE-STAMM')",
                It.IsAny<string?>(), "Artikelnummer", It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ReturnsAsync(S<SdataArtikelZeile>("Artikel"));
        client.Setup(c => c.QueryAllAsync<SdataArtikelVarianteZeile>(Ziel, It.IsAny<string>(), "ArtikelVariantenExtended", It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ReturnsAsync(S<SdataArtikelVarianteZeile>("ArtikelVariantenExtended"));
        client.Setup(c => c.QueryAllAsync<SdataLagerplatzZeile>(Ziel, It.IsAny<string>(), "LagerplaetzeInfo", null,
                It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ReturnsAsync(S<SdataLagerplatzZeile>("LagerplaetzeInfo"));
        return client;
    }

    private static SdataArtikelReader Reader(Mock<ISageSdataReadClient> client) =>
        new(client.Object, new ConfigurationBuilder().Build(), NullLogger<SdataArtikelReader>.Instance);

    [Fact]
    public async Task Lesen_BenutzerfelderJeStammartikel_UngueltigeAlsWarnung()
    {
        var client = Client();
        client.Setup(c => c.QueryAllAsync<Dictionary<string, JsonElement>>(Ziel, It.IsAny<string>(), "Artikel", "Artikelnummer in ('A-1','B-2')",
                "Artikelnummer,USER_Maschine,USER_Ventil", "Artikelnummer", It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ReturnsAsync(new SdataSeiten<Dictionary<string, JsonElement>>(new[]
            {
                U("""{"Artikelnummer":"A-1","USER_Maschine":"M-200","USER_Ventil":null}"""),
                U("""{"Artikelnummer":"B-2 ","user_maschine":"","USER_Ventil":-1}"""),
            }, "Artikel", 1, 2, 1));
        var reader = Reader(client);

        var dtos = await reader.LesenAsync(Ziel, CancellationToken.None, SdataArtikelReader.BenutzerfelderParsen("USER_Maschine,USER_Ventil,Kaputt"));

        dtos.Single(d => d.ArticleNumber == "A-1").Benutzerfelder.Should().BeEquivalentTo(new Dictionary<string, string?> { ["USER_Maschine"] = "M-200", ["USER_Ventil"] = null });
        dtos.Single(d => d.ArticleNumber == "B-2").Benutzerfelder!["USER_Ventil"].Should().Be("-1");
        dtos.Single(d => d.ArticleNumber == "R-OHNE-STAMM").Benutzerfelder.Should().BeNull("ohne Artikelstamm keine Benutzerfelder = unberuehrt");
        reader.Hinweise.Should().Contain(h => h.Warnung && h.Text.Contains("„Kaputt“"));
    }

    [Fact]
    public async Task Lesen_UnbekanntesFeld_Http500_WirdEinzelnErmittelt_Warnung_RestGelesen()
    {
        var client = Client();
        client.Setup(c => c.QueryAllAsync<Dictionary<string, JsonElement>>(Ziel, It.IsAny<string>(), "Artikel", It.IsAny<string?>(),
                "Artikelnummer,USER_Maschine,USER_GibtEsNicht", "Artikelnummer", It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ThrowsAsync(new SdataLeseException("Artikel", 500, "Invalid column name 'USER_GibtEsNicht'"));
        client.Setup(c => c.QueryAllAsync<Dictionary<string, JsonElement>>(Ziel, It.IsAny<string>(), "Artikel", "Artikelnummer in ('A-1')",
                "Artikelnummer,USER_GibtEsNicht", "Artikelnummer", It.IsAny<CancellationToken>(), 1))
            .ThrowsAsync(new SdataLeseException("Artikel", 500, "Invalid column name"));
        client.Setup(c => c.QueryAllAsync<Dictionary<string, JsonElement>>(Ziel, It.IsAny<string>(), "Artikel", It.IsAny<string?>(),
                "Artikelnummer,USER_Maschine", "Artikelnummer", It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ReturnsAsync(new SdataSeiten<Dictionary<string, JsonElement>>(new[] { U("""{"Artikelnummer":"A-1","USER_Maschine":"M"}""") }, "Artikel", 1, 1, 1));
        var reader = Reader(client);

        var dtos = await reader.LesenAsync(Ziel, CancellationToken.None, SdataArtikelReader.BenutzerfelderParsen("USER_Maschine,USER_GibtEsNicht"));

        dtos.Single(d => d.ArticleNumber == "A-1").Benutzerfelder!.Keys.Should().Equal("USER_Maschine");
        reader.Hinweise.Should().Contain(h => h.Warnung && h.Text.Contains("USER_GibtEsNicht") && h.Text.Contains("unbekannt"));
    }

    [Fact]
    public async Task Lesen_Http500OhneSchuldigesFeld_Wirft()
    {
        var client = Client();
        client.Setup(c => c.QueryAllAsync<Dictionary<string, JsonElement>>(Ziel, It.IsAny<string>(), "Artikel", It.IsAny<string?>(),
                It.IsAny<string?>(), "Artikelnummer", It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ThrowsAsync(new SdataLeseException("Artikel", 500, "Timeout im Server"));
        client.Setup(c => c.QueryAllAsync<Dictionary<string, JsonElement>>(Ziel, It.IsAny<string>(), "Artikel", It.IsAny<string?>(),
                It.IsAny<string?>(), "Artikelnummer", It.IsAny<CancellationToken>(), 1))
            .ReturnsAsync(new SdataSeiten<Dictionary<string, JsonElement>>(Array.Empty<Dictionary<string, JsonElement>>(), "Artikel", 1, 0, 1));

        await Reader(client).Invoking(r => r.LesenAsync(Ziel, CancellationToken.None, SdataArtikelReader.BenutzerfelderParsen("USER_Maschine")))
            .Should().ThrowAsync<SdataLeseException>();
    }

    [Fact]
    public async Task Lesen_OhneFelder_KeineAbfrage()
    {
        var client = Client();
        var dtos = await Reader(client).LesenAsync(Ziel, CancellationToken.None);
        dtos.Should().OnlyContain(d => d.Benutzerfelder == null);
        client.Verify(c => c.QueryAllAsync<Dictionary<string, JsonElement>>(It.IsAny<SdataLeseZiel>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<int>()), Times.Never);
    }

    // ----- Writer -----

    private static SageArtikelDto Dto(string nr, Dictionary<string, string?>? felder) =>
        new(nr, null, null, null, null, null, null, null, felder);

    [Fact]
    public async Task Writer_LegtDefinitionenIdempotentAn_SchreibtNurSDataZeilen_MitAudit()
    {
        var ctx = TestDbContextFactory.Create();
        ctx.Articles.AddRange(
            new Article { Id = 1, ArticleNumber = "A-1", CreatedBy = "x", CreatedByWindows = "x" },
            new Article { Id = 2, ArticleNumber = "B-2", CreatedBy = "x", CreatedByWindows = "x" });
        ctx.ArticleAttributeDefinitions.Add(new ArticleAttributeDefinition { Id = 50, Name = "Laser", SortOrder = 3, CreatedBy = "x", CreatedByWindows = "x" });
        ctx.SaveChanges();
        var felder = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase) { ["USER_Maschine"] = "M-200", ["USER_Ventil"] = null };

        var r1 = await ArtikelBenutzerfelderWriter.SchreibenAsync(ctx, new[]
        {
            Dto("a-1 ", felder),
            Dto("B-2", null), // View-Zeile: nie anfassen
            Dto("FEHLT", felder),
        }, CancellationToken.None);

        (r1.DefinitionenNeu, r1.Neu, r1.OhneArtikel).Should().Be((2, 1, 1));
        var maschine = ctx.ArticleAttributeDefinitions.Single(d => d.SyncFieldName == "USER_Maschine");
        (maschine.Name, maschine.AttributeType, maschine.SyncSource, maschine.SortOrder, maschine.IsActive, maschine.CreatedBy)
            .Should().Be(("Maschine", AttributeType.Text, "Sage", 4, true, "IDEALAKEWMSService"));
        var wert = ctx.ArticleAttributeValues.Single();
        (wert.ArticleId, wert.TextValue, wert.ModifiedAt).Should().Be((1, "M-200", (DateTime?)null));

        // zweiter Lauf, gleiche Daten: keine neue Definition, kein Audit-Update
        var r2 = await ArtikelBenutzerfelderWriter.SchreibenAsync(ctx, new[] { Dto("A-1", felder) }, CancellationToken.None);
        (r2.DefinitionenNeu, r2.Neu, r2.Aktualisiert).Should().Be((0, 0, 0));
        ctx.ArticleAttributeDefinitions.Count().Should().Be(3);
        wert.ModifiedAt.Should().BeNull();

        // Aenderung -> Audit; leer -> Wert entfernt
        felder["USER_Maschine"] = "M-300";
        (await ArtikelBenutzerfelderWriter.SchreibenAsync(ctx, new[] { Dto("A-1", felder) }, CancellationToken.None)).Aktualisiert.Should().Be(1);
        (wert.TextValue, wert.ModifiedBy).Should().Be(("M-300", "IDEALAKEWMSService"));
        wert.ModifiedAt.Should().NotBeNull();
        wert.ModifiedByWindows.Should().NotBeNullOrEmpty();
        felder["USER_Maschine"] = " ";
        (await ArtikelBenutzerfelderWriter.SchreibenAsync(ctx, new[] { Dto("A-1", felder) }, CancellationToken.None)).Entfernt.Should().Be(1);
        ctx.ArticleAttributeValues.Should().BeEmpty();
    }

    [Fact]
    public async Task Writer_ManuellesMerkmalGleichenNamens_Konflikt_Unveraendert()
    {
        var ctx = TestDbContextFactory.Create();
        ctx.Articles.Add(new Article { Id = 1, ArticleNumber = "A-1", CreatedBy = "x", CreatedByWindows = "x" });
        ctx.ArticleAttributeDefinitions.Add(new ArticleAttributeDefinition { Id = 9, Name = "maschine", AttributeType = AttributeType.Dropdown, CreatedBy = "x", CreatedByWindows = "x" });
        ctx.SaveChanges();

        var r = await ArtikelBenutzerfelderWriter.SchreibenAsync(ctx,
            new[] { Dto("A-1", new() { ["USER_Maschine"] = "M-200" }) }, CancellationToken.None);

        r.Warnungen.Should().ContainSingle(w => w.Contains("Konflikt") && w.Contains("USER_Maschine"));
        ctx.ArticleAttributeDefinitions.Single().Should().Match<ArticleAttributeDefinition>(d => d.SyncSource == null && d.AttributeType == AttributeType.Dropdown);
        ctx.ArticleAttributeValues.Should().BeEmpty();
    }

    [Fact]
    public async Task Writer_UeberLaenge_GekuerztMitWarnung()
    {
        var ctx = TestDbContextFactory.Create();
        ctx.Articles.Add(new Article { Id = 1, ArticleNumber = "A-1", CreatedBy = "x", CreatedByWindows = "x" });
        ctx.SaveChanges();

        var r = await ArtikelBenutzerfelderWriter.SchreibenAsync(ctx,
            new[] { Dto("A-1", new() { ["USER_PfadSchaltplan"] = new string('x', 1200) }) }, CancellationToken.None);

        ctx.ArticleAttributeValues.Single().TextValue!.Length.Should().Be(1000);
        r.Warnungen.Should().ContainSingle(w => w.Contains("1200 Zeichen") && w.Contains("gekürzt"));
    }
}
