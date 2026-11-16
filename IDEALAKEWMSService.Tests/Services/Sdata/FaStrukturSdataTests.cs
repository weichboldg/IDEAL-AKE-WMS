using System.Text.Json;
using FluentAssertions;
using IdealAkeWms.Models;
using IdealAkeWms.Services.SageSdata;
using IdealAkeWms.Services.SyncLogger;
using IdealAkeWms.Tests.Helpers;
using IDEALAKEWMSService.Services;
using IDEALAKEWMSService.Services.Sdata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace IDEALAKEWMSService.Tests.Services.Sdata;

/// <summary>Spec 2026-11-06-sage-sdata-stammdaten-sync, E4a: FA-Struktur-Kanten aus SData (Regel F6), Weiche inkl. Sperre P-F2,
/// Vergleich der Kanten, Abgleich ueber das Reader-Interface (Empty-Guard, ungueltiger View-Name), DI.
/// E4b: Lesefilter eingelastet/aktiv, Positionen-Projektion (ProductionOrderPositions), Schreibregel (nur wenn SData gelesen wurde).</summary>
public class FaStrukturSdataTests
{
    private static readonly SdataLeseZiel Ziel = new("https://sagetest01:5493", "ol", "ake_TEST2026;1", "u", "p", true);
    private static readonly DateTime Jetzt = new(2026, 11, 6, 12, 0, 0);

    /// <summary>Kopfzeile; Default eingelastet + aktiv (E4b-Filter laesst sie durch).</summary>
    private static SdataFaKopfZeile K(int? belId, int? struktur, string? art, bool? eingelastet = true, bool? aktiv = true) =>
        new(belId, struktur, art, eingelastet, aktiv);

    private static SdataFaPositionZeile Pos(int belPosId, int belId, int position, int? nachfolger, string typ, string nr, decimal menge = 1m) =>
        new(belPosId, belId, position, nachfolger, typ, nr, menge);

    /// <summary>Struktur 835 am AKE-Test (sage-sdata-commonwawiservicesresources): 7 FAs, 17 Positionen.</summary>
    private static (SdataFaKopfZeile[] Koepfe, SdataFaPositionZeile[] Positionen) Struktur835()
    {
        var koepfe = new[] { K(835, 835, "86003871") }
            .Concat(Enumerable.Range(836, 6).Select(id => K(id, 835, "BG-" + id))).ToArray();
        var pos = new List<SdataFaPositionZeile>();
        for (var i = 1; i <= 6; i++) pos.Add(Pos(10600 + i, 835, i, 835 + i, "MA", "BG-" + (835 + i)));
        pos.Add(Pos(10607, 835, 7, 0, "MA", "KAUF-1"));
        pos.Add(Pos(10608, 835, 8, null, "MA", "KAUF-2"));
        pos.Add(Pos(10656, 835, 9, 0, "AG", "999"));
        pos.Add(Pos(10641, 836, 1, 0, "MA", "00722001", 0.6m));
        pos.Add(Pos(10642, 836, 2, 0, "AG", "10"));
        for (var id = 837; id <= 841; id++) pos.Add(Pos(10600 + id, id, 1, 0, "MA ", "T-" + id));
        pos.Add(Pos(10700, 837, 2, 0, "AG", "20"));
        pos.Should().HaveCount(17);
        return (koepfe, pos.ToArray());
    }

    // ----- Projektion / Hierarchie-Regel F6 -----

    [Fact]
    public void Projizieren_Struktur835_WurzelKantenBlaetter_OhneAg()
    {
        var (koepfe, pos) = Struktur835();

        var k = SdataFaStrukturReader.Projizieren(koepfe, pos, Jetzt);

        k.Should().HaveCount(1 + 6 + 2 + 1 + 5, "Wurzel + 6 Kanten + 2 Blaetter an 835 + 836er Blatt + je ein Blatt 837–841; AG faellt weg");
        k.Should().OnlyContain(n => n.HauptFA == 835 && n.HauptArtnr == "86003871" && n.SyncedAt == Jetzt);

        var wurzel = k.Single(n => n.VaterFA == null);
        (wurzel.SubFA, wurzel.Position).Should().Be((835, (int?)null), "Wurzel: SubFA = HauptFA, Position NULL");

        k.Where(n => n.VaterFA == 835 && n.SubFA != 0).Select(n => n.SubFA).Should().Equal(836, 837, 838, 839, 840, 841);
        k.Where(n => n.VaterFA == 835 && n.SubFA == 0).Select(n => n.Artnr).Should().Equal("KAUF-1", "KAUF-2");

        var blatt = k.Single(n => n.VaterFA == 836);
        (blatt.SubFA, blatt.Position, blatt.Artnr, blatt.Sollmenge, blatt.Fertigungmenge).Should().Be((0, (int?)1, "00722001", 0.6m, 0.6m));
        k.Should().NotContain(n => n.Artnr == "999" || n.Artnr == "10" || n.Artnr == "20");
    }

    [Fact]
    public void Projizieren_Zwischenebene_VaterIstSubFaDerEbeneDarueber_HauptFaBleibtStruktur()
    {
        var koepfe = new[] { K(900, 900, "GERAET"), K(901, 900, "BG1"), K(902, 900, "BG2") };
        var pos = new[]
        {
            Pos(1, 900, 1, 901, "MA", "BG1"),
            Pos(2, 901, 1, 902, "MA", "BG2"),
            Pos(3, 902, 1, 0, "MA", "BLECH"),
        };

        var k = SdataFaStrukturReader.Projizieren(koepfe, pos, Jetzt);

        k.Select(n => (n.HauptFA, n.VaterFA, n.Position, n.SubFA)).Should().Equal(
            (900, (int?)null, (int?)null, 900),
            (900, 900, 1, 901),
            (900, 901, 1, 902),
            (900, 902, 1, 0));
    }

    [Fact]
    public void Projizieren_HauptFaNichtInMenge_KeineWurzel_Hinweis_FremdePositionenFallenWeg()
    {
        var hinweise = new List<SdataHinweis>();
        var k = SdataFaStrukturReader.Projizieren(
            new[] { K(951, 950, "BG") },
            new[] { Pos(1, 951, 1, 0, "MA", "T"), Pos(2, 777, 1, 0, "MA", "FREMD") }, Jetzt, hinweise);

        k.Should().ContainSingle().Which.Should().Match<FaHierarchyNode>(n => n.HauptFA == 950 && n.VaterFA == 951 && n.HauptArtnr == null);
        hinweise.Should().ContainSingle(h => h.Warnung && h.Text.Contains("950"));
    }

    [Fact]
    public void Projizieren_ZeileOhneBelId_Wirft()
    {
        ((Action)(() => SdataFaStrukturReader.Projizieren(new[] { K(null, 1, "A") },
            Array.Empty<SdataFaPositionZeile>(), Jetzt))).Should().Throw<SdataLeseException>();
        ((Action)(() => SdataFaStrukturReader.Projizieren(new[] { K(1, 1, "A") },
            new[] { new SdataFaPositionZeile(1, null, 1, 0, "MA", "X", 1m) }, Jetzt))).Should().Throw<SdataLeseException>();
    }

    [Fact]
    public void WhereKoepfe_OffeneUndBeendeteDerLetzten30Tage() =>
        SdataFaStrukturReader.WhereKoepfe(new DateTime(2026, 11, 6, 15, 30, 0))
            .Should().Be("IstBeendet eq 0 or Istendtermin ge @2026-10-07@");

    [Fact]
    public async Task LesenAsync_KoepfeMitFenster_PositionenInOrBloecken_OhneFaInfos()
    {
        var (koepfe, pos) = Struktur835();
        var client = new Mock<ISageSdataReadClient>();
        client.Setup(c => c.QueryAllAsync<SdataFaKopfZeile>(Ziel, SageSdataReadClient.VertragResources, "PpsFaBelegeInfo",
                SdataFaStrukturReader.WhereKoepfe(Jetzt), SdataFaStrukturReader.SelectKoepfe, "BelID", It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ReturnsAsync(new SdataSeiten<SdataFaKopfZeile>(koepfe, "PpsFaBelegeInfo", 1, koepfe.Length, 1));
        client.Setup(c => c.QueryAllAsync<SdataFaPositionZeile>(Ziel, SageSdataReadClient.VertragResources, "PpsFaBelegePositionenInfo",
                "BelID eq 835 or BelID eq 836 or BelID eq 837 or BelID eq 838 or BelID eq 839 or BelID eq 840 or BelID eq 841",
                SdataFaStrukturReader.SelectPositionen, "BelPosID", It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ReturnsAsync(new SdataSeiten<SdataFaPositionZeile>(pos, "PpsFaBelegePositionenInfo", 1, pos.Length, 1));
        Artikel(client);
        var reader = new SdataFaStrukturReader(client.Object, new ConfigurationBuilder().Build(), NullLogger<SdataFaStrukturReader>.Instance);

        var l = await reader.LesenAsync(Ziel, Jetzt, CancellationToken.None);

        l.Knoten.Should().HaveCount(15);
        (l.Infos.Count, l.InfosAnzahl).Should().Be((0, 0L), "G11: FAInfos bleiben SQL – im Modus SData wuerde der Empty-Guard greifen");
        reader.Hinweise.Should().Contain(h => h.Text.Contains("PpsFaBelegePositionenInfo") && h.Text.Contains("1 Block"));
    }

    // ----- Spec 2026-11-16-fa-struktur-ideal-ueber-sdata E3: beendete Haupt-FAs ueber dtsFaBelegsuche (nur Vergleich) -----

    internal static Dictionary<string, JsonElement> Zeile(string json) =>
        JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;

    /// <summary>Struktur 1059463 (IDEAL, L11): Haupt-FA fehlt in PpsFaBelegeInfo, Unter-FA 1065143 ist da.</summary>
    private static Mock<ISageSdataReadClient> Client1059463(params Dictionary<string, JsonElement>[] belegsuche)
    {
        var client = new Mock<ISageSdataReadClient>(MockBehavior.Strict);
        var koepfe = new[] { K(1065143, 1059463, "50002143") };
        var selectBelegsuche = SdataFaStrukturReader.SelectBelegsucheKopf + "," + string.Join(",", SdataFaStrukturReader.FelderFa.Select(f => SdataFaStrukturReader.PraefixBelegsuche + f));
        client.Setup(c => c.QueryAllAsync<SdataFaKopfZeile>(Ziel, SageSdataReadClient.VertragResources, "PpsFaBelegeInfo",
                SdataFaStrukturReader.WhereKoepfe(Jetzt), SdataFaStrukturReader.SelectKoepfe, "BelID", It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ReturnsAsync(new SdataSeiten<SdataFaKopfZeile>(koepfe, "PpsFaBelegeInfo", 1, 1, 1));
        client.Setup(c => c.GetAsync<Dictionary<string, JsonElement>>(Ziel, SageSdataQuellen.FaBelegsuche, "BelID eq 1059463 or BelID eq 1065143",
                selectBelegsuche, "BelID", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SdataErgebnis<Dictionary<string, JsonElement>>(belegsuche,
                new SdataCall(SageSdataQuellen.FaBelegsuche, "url", 200, 1, belegsuche.Length, null, null)));
        var pos = new[]
        {
            Pos(1, 1059463, 1, 1065143, "MA", "50002143"),
            Pos(2, 1059463, 2, 0, "MA", "73002467"),
            Pos(3, 1065143, 1, 0, "MA", "50002143-B"),
        };
        client.Setup(c => c.QueryAllAsync<SdataFaPositionZeile>(Ziel, SageSdataReadClient.VertragResources, "PpsFaBelegePositionenInfo",
                It.IsAny<string>(), SdataFaStrukturReader.SelectPositionen, "BelPosID", It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ReturnsAsync((SdataLeseZiel _, string _, string _, string? where, string? _, string _, CancellationToken _, int _) =>
            {
                var ids = where!.Split(" or ").Select(t => int.Parse(t["BelID eq ".Length..])).ToHashSet();
                var treffer = pos.Where(p => ids.Contains(p.BelID!.Value)).ToList();
                return new SdataSeiten<SdataFaPositionZeile>(treffer, "PpsFaBelegePositionenInfo", 1, treffer.Count, 1);
            });
        Artikel(client);
        return client;
    }

    /// <summary>E4: Artikel-Abfrage (Stamm- und Blattfelder); ohne Zeilen = keine Felder.</summary>
    private static void Artikel(Mock<ISageSdataReadClient> client, params Dictionary<string, JsonElement>[] zeilen) =>
        client.Setup(c => c.QueryAllAsync<Dictionary<string, JsonElement>>(Ziel, SageSdataReadClient.VertragResources, "Artikel",
                It.IsAny<string?>(), It.IsAny<string?>(), "Artikelnummer", It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ReturnsAsync(new SdataSeiten<Dictionary<string, JsonElement>>(zeilen, "Artikel", 1, zeilen.Length, 1));

    private static SdataFaStrukturReader Reader(Mock<ISageSdataReadClient> client, bool realTimeData) =>
        new(client.Object, new ConfigurationBuilder().Build(), NullLogger<SdataFaStrukturReader>.Instance) { RealTimeDataErlaubt = realTimeData };

    [Fact]
    public async Task LesenAsync_BeendeterHauptFa_KopfUeberBelegsuche_WurzelUndWurzelPositionen_MitHauptArtnr()
    {
        var client = Client1059463(Zeile("""{"BelID":1059463,"StrukturID":1059463,"Artikelnummer":"80141025"}"""));
        var reader = Reader(client, realTimeData: true);

        var l = await reader.LesenAsync(Ziel, Jetzt, CancellationToken.None);

        l.Knoten.Select(n => (n.VaterFA, n.Position, n.SubFA, n.Artnr)).Should().Equal(
            ((int?)null, (int?)null, 1059463, "80141025"), (1059463, 1, 1065143, "50002143"), (1059463, 2, 0, "73002467"),
            (1065143, 1, 0, "50002143-B"));
        l.Knoten.Should().OnlyContain(n => n.HauptFA == 1059463 && n.HauptArtnr == "80141025");
        reader.Hinweise.Should().NotContain(h => h.Text.Contains("ohne Haupt-FA in der SData-Menge"));
        reader.Hinweise.Should().Contain(h => h.Text.Contains("1 von 1 Haupt-FA(s)") && h.Text.Contains("1059463"));
        l.Positionen!.Select(p => p.BelID).Should().OnlyContain(id => id == 1065143, "ProductionOrderPositions: Schreibverhalten unveraendert (nur die Menge)");
    }

    [Fact]
    public async Task LesenAsync_HauptFaAuchInBelegsucheNicht_WarnungOhneWurzel()
    {
        var reader = Reader(Client1059463(), realTimeData: true);

        var l = await reader.LesenAsync(Ziel, Jetzt, CancellationToken.None);

        l.Knoten.Should().ContainSingle().Which.VaterFA.Should().Be(1065143);
        reader.Hinweise.Should().Contain(h => h.Warnung && h.Text.Contains("ohne Haupt-FA in der SData-Menge") && h.Text.Contains("1059463"));
    }

    [Fact]
    public async Task LesenAsync_OhneRealTimeDataFreigabe_KeinAufrufBelegsuche_Hinweis()
    {
        var client = Client1059463();
        var reader = Reader(client, realTimeData: false);

        await reader.LesenAsync(Ziel, Jetzt, CancellationToken.None);

        client.Verify(c => c.GetAsync<Dictionary<string, JsonElement>>(It.IsAny<SdataLeseZiel>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        reader.Hinweise.Should().Contain(h => h.Text.Contains("nur im Modus Vergleich"));
    }

    [Fact]
    public async Task LesenAsync_BelegsucheHttpFehler_Wirft()
    {
        var client = Client1059463();
        client.Setup(c => c.GetAsync<Dictionary<string, JsonElement>>(Ziel, SageSdataQuellen.FaBelegsuche, It.IsAny<string>(),
                It.IsAny<string>(), "BelID", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SdataErgebnis<Dictionary<string, JsonElement>>(Array.Empty<Dictionary<string, JsonElement>>(),
                new SdataCall(SageSdataQuellen.FaBelegsuche, "url", 500, 1, 0, "HTTP 500", null)));

        await Reader(client, realTimeData: true).Invoking(r => r.LesenAsync(Ziel, Jetzt, CancellationToken.None))
            .Should().ThrowAsync<SdataLeseException>().Where(e => e.Status == 500);
    }

    [Theory]
    [InlineData(SageQuelle.Vergleich, true)]
    [InlineData(SageQuelle.SData, false)]
    [InlineData(SageQuelle.View, false)]
    [InlineData(null, false)]
    public void RealTimeData_NurImModusVergleich(SageQuelle? quelle, bool erlaubt) =>
        FaStrukturQuelleSwitch.RealTimeDataErlaubt(quelle).Should().Be(erlaubt, "Freigabe-Antwort 1: bis DPS-Antwort/P-FS3");

    // ----- Spec 2026-11-16-fa-struktur-ideal-ueber-sdata E4: Knotenfelder + erweiterter Vergleich -----

    private static Dictionary<string, string?> D(params (string K, string? V)[] w) =>
        w.ToDictionary(x => x.K, x => x.V, StringComparer.OrdinalIgnoreCase);

    [Fact]
    public void Projizieren_UnterFaBekommtFaFelder_BlattArtikelfelder_AlleGefuellt_BoolWieView()
    {
        var koepfe = new[] { K(900, 900, "G"), K(901, 900, "BG") };
        var pos = new[] { Pos(1, 900, 1, 901, "MA", "BG"), Pos(2, 901, 1, 0, "MA", "BLATT") };
        var fa = new Dictionary<int, Dictionary<string, string?>>
        {
            [901] = D(("USER_OSBemerkungPN", "Bem"), ("USER_OSFertigungsInfoPN", "FInfo"), ("USER_OSKommissionieren", "Ja"),
                ("USER_OSAbteilung", "Montage"), ("USER_OSArbeitsschritte", "SG"), ("USER_OSArtikeltyp", "BG"), ("USER_OSMaterial", "1.4301"),
                ("USER_OSVMBedarf", "VM"), ("USER_OSBeschichtet", "-1"), ("USER_OSBreite", "100.5"), ("USER_OSHoehe", "200"),
                ("USER_OSTiefe", "300"), ("USER_OSEKBedarf", "Ja")),
        };
        var artikel = new Dictionary<string, Dictionary<string, string?>>(StringComparer.OrdinalIgnoreCase)
        {
            ["BG"] = D(("Matchcode", "MC-BG"), ("Bezeichnung1", "Baugruppe"), ("Bezeichnung2", "Z2"), ("Artikelgruppe", "100"),
                ("IstBestellartikel", "0"), ("PlatzID", "11")),
            ["BLATT"] = D(("Matchcode", "MC-B"), ("Bezeichnung1", "Blech"), ("Bezeichnung2", "Z2b"), ("Artikelgruppe", "200"),
                ("IstBestellartikel", "-1"), ("PlatzID", "11"), ("USER_OSBemerkungPN", "BemA"), ("USER_OSFertigungsInfoPN", "FA"),
                ("USER_OSKommissionieren", "Nein"), ("USER_OSAbteilung", "Laser"), ("USER_OSArbeitsschritte", "LA"),
                ("USER_OSArtikeltyp", "Blech"), ("USER_OSMaterial", "DC01"), ("USER_OSVMBedarf", "x")),
        };

        var k = SdataFaStrukturReader.Projizieren(koepfe, pos, Jetzt, null, fa, artikel, Plaetze);

        var bg = k.Single(n => n.SubFA == 901);
        (bg.Matchcode, bg.Bezeichnung1, bg.Bezeichnung2, bg.Artikelgruppe, bg.Hauptlagerplatz).Should().Be(("MC-BG", "Baugruppe", "Z2", "100", "HR 0 0 0 "), "Kurzbezeichnung roh wie die View");
        (bg.BemerkungPN, bg.FertigungsInfoPN, bg.Kommissionieren, bg.Arbeitsbereich, bg.Arbeitsschritte, bg.Artikeltyp, bg.Material, bg.VMBedarf)
            .Should().Be(("Bem", "FInfo", "Ja", "Montage", "SG", "BG", "1.4301", "VM"));
        (bg.Beschichtet, bg.EKBedarf, bg.Beschaffungsartikel, bg.Breite, bg.Hoehe, bg.Tiefe).Should().Be((true, true, false, 100.5m, 200m, 300m),
            "-1 und Ja → true, 0 → false (Regel des View-Wegs)");

        var blatt = k.Single(n => n.SubFA == 0);
        (blatt.Matchcode, blatt.Bezeichnung1, blatt.Arbeitsschritte, blatt.Arbeitsbereich, blatt.Material, blatt.Hauptlagerplatz)
            .Should().Be(("MC-B", "Blech", "LA", "Laser", "DC01", "HR 0 0 0 "));
        blatt.Beschaffungsartikel.Should().BeTrue("IstBestellartikel -1");
        (blatt.Beschichtet, blatt.EKBedarf, blatt.Breite).Should().Be((false, false, 0m), "Blatt: keine Quelle (Rueckfrage 4), NULL → false/0");

        k.Single(n => n.VaterFA is null).Matchcode.Should().BeNull("Wurzel-Artikel G ohne Artikelzeile → leer");
    }

    [Theory]
    [InlineData(-1, true)]
    [InlineData(1, true)]
    [InlineData(0, false)]
    [InlineData("Ja", true)]
    [InlineData("Nein", false)]
    [InlineData(null, false)]
    public void SageBool_EineRegel_ViewUndSdata(object? wert, bool erwartet) =>
        SageFaStrukturReader.SageBool(wert is int i ? (decimal)i : wert).Should().Be(erwartet);

    [Fact]
    public void Vergleich_NurArbeitsschritteAnEinemBlatt_GenauEineAbweichendeZeileMitFeldname()
    {
        FaHierarchyNode Blatt(string? schritte) => new()
        {
            HauptFA = 900, VaterFA = 900, Position = 1, SubFA = 0, HauptArtnr = "G", Artnr = "B", Sollmenge = 1, Fertigungmenge = 1,
            Matchcode = "MC", Arbeitsschritte = schritte, Beschichtet = true,
        };
        var wurzel = new FaHierarchyNode { HauptFA = 900, SubFA = 900, HauptArtnr = "G", Artnr = "G", Matchcode = "W" };

        var e = SdataVergleich.Vergleiche(new[] { wurzel, Blatt("LA") }, new[] { wurzel, Blatt("LA,SG") },
            FaStrukturQuelleSwitch.Schluessel, FaStrukturQuelleSwitch.VergleichsFelder);

        e.AbweichendeSchluessel.Should().Be(1);
        e.Beispiele.Should().ContainSingle().Which.Should().Match<SdataAbweichung>(a => a.Feld == "Arbeitsschritte" && a.Schluessel == "Blatt 900/1");
    }

    [Fact]
    public void Vergleich_LeererTextGleichNull_KeineAbweichung()
    {
        var v = new FaHierarchyNode { HauptFA = 1, SubFA = 1, Material = "", BemerkungPN = " x " };
        var s = new FaHierarchyNode { HauptFA = 1, SubFA = 1, Material = null, BemerkungPN = "x" };
        SdataVergleich.Vergleiche(new[] { v }, new[] { s }, FaStrukturQuelleSwitch.Schluessel, FaStrukturQuelleSwitch.VergleichsFelder)
            .AbweichendeSchluessel.Should().Be(0);
    }

    [Fact]
    public async Task LesenAsync_FaFelderAusBelegsuche_ArtikelfelderAmBlatt()
    {
        var client = Client1059463(
            Zeile("""{"BelID":1059463,"StrukturID":1059463,"Artikelnummer":"80141025"}"""),
            Zeile("""{"BelID":1065143,"StrukturID":1059463,"Artikelnummer":"50002143","KHKPpsFaBelege_USER_OSArbeitsschritte":"SG","KHKPpsFaBelege_USER_OSBeschichtet":-1,"USER_OSMaterial":"ohnePraefix"}"""));
        Artikel(client, Zeile("""{"Artikelnummer":"73002467","Matchcode":"MC-73","USER_OSArbeitsschritte":"LA","IstBestellartikel":-1}"""));

        var l = await Reader(client, realTimeData: true).LesenAsync(Ziel, Jetzt, CancellationToken.None);

        var bg = l.Knoten.Single(n => n.SubFA == 1065143);
        (bg.Arbeitsschritte, bg.Beschichtet, bg.Material).Should().Be(("SG", true, null)); // R1: nur Praefix-Namen zaehlen
        var blatt = l.Knoten.Single(n => n.Artnr == "73002467");
        (blatt.Matchcode, blatt.Arbeitsschritte, blatt.Beschaffungsartikel).Should().Be(("MC-73", "LA", true));
    }

    [Fact]
    public async Task LesenAsync_UnbekanntesArtikelfeld_Warnung_LaufLaeuftWeiter()
    {
        var client = Client1059463(Zeile("""{"BelID":1059463,"StrukturID":1059463,"Artikelnummer":"80141025"}"""));
        var alle = "Artikelnummer," + string.Join(",", SdataFaStrukturReader.FelderArtikel);
        var ohne = "Artikelnummer," + string.Join(",", SdataFaStrukturReader.FelderArtikel.Where(f => f != "USER_OSMaterial"));
        client.Setup(c => c.QueryAllAsync<Dictionary<string, JsonElement>>(Ziel, SageSdataReadClient.VertragResources, "Artikel",
                It.IsAny<string?>(), It.IsAny<string?>(), "Artikelnummer", It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ReturnsAsync((SdataLeseZiel _, string _, string _, string? _, string? select, string _, CancellationToken _, int _) =>
                select == alle || select == "Artikelnummer,USER_OSMaterial"
                    ? throw new SdataLeseException("Artikel", 500, "Invalid column name 'USER_OSMaterial'")
                    : new SdataSeiten<Dictionary<string, JsonElement>>(
                        select == ohne ? new[] { Zeile("""{"Artikelnummer":"73002467","Matchcode":"MC-73"}""") } : Array.Empty<Dictionary<string, JsonElement>>(),
                        "Artikel", 1, 1, 1));
        var reader = Reader(client, realTimeData: true);

        var l = await reader.LesenAsync(Ziel, Jetzt, CancellationToken.None);

        reader.Hinweise.Should().Contain(h => h.Warnung && h.Text.Contains("USER_OSMaterial") && h.Text.Contains("unbekannt"));
        l.Knoten.Single(n => n.Artnr == "73002467").Matchcode.Should().Be("MC-73");
    }

    // ----- E4b: Lesefilter eingelastet/aktiv -----

    [Fact]
    public void KoepfeFiltern_NurEingelastetUndAktiv_AusgefilterteWerdenGemeldet()
    {
        var hinweise = new List<SdataHinweis>();
        var menge = SdataFaStrukturReader.KoepfeFiltern(new[]
        {
            K(835, 835, "G"), K(836, 835, "BG", eingelastet: false), K(837, 835, "EK", aktiv: false), K(838, 835, "BG"),
        }, hinweise);

        menge.Select(k => k.BelID).Should().Equal(835, 838);
        hinweise.Should().ContainSingle().Which.Text.Should().Contain("2 FA(s)").And.Contain("836, 837");
    }

    [Fact]
    public void KoepfeFiltern_FeldFehltInAntwort_Wirft_StattAllesAuszufiltern() =>
        ((Action)(() => SdataFaStrukturReader.KoepfeFiltern(new[] { K(835, 835, "G"), K(836, 835, "BG", eingelastet: null) })))
            .Should().Throw<SdataLeseException>().WithMessage("*IstEingelastet/Aktiv*");

    [Fact]
    public void Projizieren_NachFilter_InaktiverUnterFaFehlt_SeineKanteBleibtAlsVerweis()
    {
        // Unter-FA 836 inaktiv (z. B. Fertigungsart Einkauf): er fehlt in der Menge -> keine Knoten unter 836; die MA-Position von 835
        // zeigt weiter auf 836 (wie in Sage). Die Materialisierung behandelt 836 dann wie verschwunden (SageMissingSince, AK 14).
        var menge = SdataFaStrukturReader.KoepfeFiltern(new[] { K(835, 835, "G"), K(836, 835, "BG", aktiv: false) });
        var k = SdataFaStrukturReader.Projizieren(menge, new[] { Pos(1, 835, 1, 836, "MA", "BG"), Pos(2, 836, 1, 0, "MA", "BLECH") }, Jetzt);

        k.Select(n => (n.VaterFA, n.SubFA)).Should().Equal(((int?)null, 835), (835, 836));
    }

    // ----- E4b: Positionen-Projektion -----

    private static readonly SdataLagerplatzZeile[] Plaetze =
    {
        new(11, null, "HR 0 0 0 ", null, null, null, null, null, null, null, null),
        new(2864, null, "WE", null, null, null, null, null, null, null, null),
    };

    [Fact]
    public void PositionenProjizieren_Struktur835_AllePositionenJederArt_MitStrukturUndLagerplatz()
    {
        var (koepfe, pos) = Struktur835();
        pos = pos.Select(p => p.BelPosID == 10641 ? p with { PlatzID = 11, Status = 0, Schwund = 0.5m, Bedarfstermin = "2026-02-03T00:00:00" } : p).ToArray();

        var r = SdataFaStrukturReader.PositionenProjizieren(koepfe, pos, Plaetze, Jetzt);

        r.Should().HaveCount(17, "alle Positionen der Menge, auch AG und Blaetter");
        r.Should().OnlyContain(x => x.StrukturID == 835 && x.SyncedAt == Jetzt);
        r.Select(x => x.BelPosID).Should().OnlyHaveUniqueItems();
        var p836 = r.Single(x => x.BelPosID == 10641);
        (p836.BelID, p836.Position, p836.RessourceTyp, p836.RessourceNummer, p836.Menge, p836.PlatzID, p836.LagerplatzKurzbezeichnung)
            .Should().Be((836, (int?)1, "MA", "00722001", (decimal?)0.6m, (int?)11, "HR 0 0 0"));
        (p836.Status, p836.Schwund, p836.Bedarfstermin).Should().Be(((int?)0, (decimal?)0.5m, (DateTime?)new DateTime(2026, 2, 3)));
        r.Single(x => x.BelPosID == 10656).RessourceTyp.Should().Be("AG");
        r.Single(x => x.BelPosID == 11437).RessourceTyp.Should().Be("MA", "getrimmt (Sage liefert \"MA \")");
    }

    [Fact]
    public void PositionenProjizieren_FremdeFaWeg_DoppelteBelPosIdEinmal_UnbekannterPlatzUndTerminGemeldet()
    {
        var hinweise = new List<SdataHinweis>();
        var r = SdataFaStrukturReader.PositionenProjizieren(
            new[] { K(900, 900, "G") },
            new[]
            {
                Pos(1, 900, 1, 0, "MA", "A") with { PlatzID = 4711 },
                Pos(1, 900, 1, 0, "MA", "A"),
                Pos(2, 900, 2, 0, "MA", "B") with { PlatzID = 0, Bedarfstermin = "kein Datum" },
                Pos(3, 777, 1, 0, "MA", "FREMD"),
            }, Plaetze, Jetzt, hinweise);

        r.Select(x => x.BelPosID).Should().Equal(1, 2);
        r[0].LagerplatzKurzbezeichnung.Should().BeNull();
        r[1].Bedarfstermin.Should().BeNull();
        hinweise.Should().HaveCount(3).And.OnlyContain(h => h.Warnung);
        hinweise.Should().Contain(h => h.Text.Contains("doppelte BelPosID")).And.Contain(h => h.Text.Contains("4711"))
            .And.Contain(h => h.Text.Contains("Bedarfstermin"));
    }

    [Fact]
    public void PositionenProjizieren_OhneBelPosId_Wirft() =>
        ((Action)(() => SdataFaStrukturReader.PositionenProjizieren(new[] { K(900, 900, "G") },
            new[] { new SdataFaPositionZeile(null, 900, 1, 0, "MA", "A", 1m) }, Plaetze, Jetzt)))
            .Should().Throw<SdataLeseException>().WithMessage("*BelPosID*");

    [Fact]
    public async Task LesenAsync_FiltertKoepfe_PositionenNurDerMenge_LagerplatzUeberLagerplaetzeInfo()
    {
        var (koepfe, pos) = Struktur835();
        koepfe = koepfe.Append(K(842, 835, "NICHT-EINGELASTET", eingelastet: false)).ToArray();
        pos = pos.Select(p => p.BelPosID == 10641 ? p with { PlatzID = 11 } : p).ToArray();
        var client = new Mock<ISageSdataReadClient>();
        client.Setup(c => c.QueryAllAsync<SdataFaKopfZeile>(Ziel, SageSdataReadClient.VertragResources, "PpsFaBelegeInfo",
                SdataFaStrukturReader.WhereKoepfe(Jetzt), SdataFaStrukturReader.SelectKoepfe, "BelID", It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ReturnsAsync(new SdataSeiten<SdataFaKopfZeile>(koepfe, "PpsFaBelegeInfo", 1, koepfe.Length, 1));
        client.Setup(c => c.QueryAllAsync<SdataFaPositionZeile>(Ziel, SageSdataReadClient.VertragResources, "PpsFaBelegePositionenInfo",
                "BelID eq 835 or BelID eq 836 or BelID eq 837 or BelID eq 838 or BelID eq 839 or BelID eq 840 or BelID eq 841",
                SdataFaStrukturReader.SelectPositionen, "BelPosID", It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ReturnsAsync(new SdataSeiten<SdataFaPositionZeile>(pos, "PpsFaBelegePositionenInfo", 1, pos.Length, 1));
        client.Setup(c => c.QueryAllAsync<SdataLagerplatzZeile>(Ziel, SageSdataReadClient.VertragResources, "LagerplaetzeInfo",
                null, "PlatzID,Kurzbezeichnung", "PlatzID", It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ReturnsAsync(new SdataSeiten<SdataLagerplatzZeile>(Plaetze, "LagerplaetzeInfo", 1, Plaetze.Length, 1));
        Artikel(client);
        var reader = new SdataFaStrukturReader(client.Object, new ConfigurationBuilder().Build(), NullLogger<SdataFaStrukturReader>.Instance);

        var l = await reader.LesenAsync(Ziel, Jetzt, CancellationToken.None);

        l.Knoten.Should().HaveCount(15, "842 ist nicht eingelastet und fehlt in der Menge");
        l.Positionen.Should().HaveCount(17).And.Contain(p => p.BelPosID == 10641 && p.LagerplatzKurzbezeichnung == "HR 0 0 0");
        reader.Hinweise.Should().Contain(h => h.Text.Contains("nicht eingelastet") && h.Text.Contains("842"));
        client.VerifyAll();
    }

    // ----- Weiche -----

    private sealed class FakeReader : ISageFaStrukturReader
    {
        public FaStrukturLesung Lesung { get; set; } = new(new(), new(), 0, 0);
        public Exception? ThrowOnRead { get; set; }
        public int Aufrufe { get; private set; }
        public Task<FaStrukturLesung> LesenAsync(CancellationToken ct = default)
        {
            Aufrufe++;
            if (ThrowOnRead is not null) throw ThrowOnRead;
            return Task.FromResult(Lesung);
        }
    }

    private sealed class TestSwitch : FaStrukturQuelleSwitch
    {
        public string? Schalter { get; set; }
        /// <summary>Nur fuer den Hybrid-Test: simuliert die spaetere Freigabe. Default = echte Sperre (SageQuelleSperre).</summary>
        public bool SperreSimuliertAufgehoben { get; set; }
        public TestSwitch(ISageFaStrukturReader view, ISageFaStrukturReader sdata, ISyncErrorNotifier notifier, ISyncLogger logger)
            : base(view, sdata, new ConfigurationBuilder().Build(), notifier, logger) { }
        protected override Task<string?> LeseSchalterAsync(CancellationToken ct) => Task.FromResult(Schalter);
        protected override Task<string?> SdataGesperrtBisAsync(CancellationToken ct) =>
            SperreSimuliertAufgehoben ? Task.FromResult<string?>(null) : base.SdataGesperrtBisAsync(ct);
    }

    private static FaHierarchyNode N(int haupt, int? vater, int? pos, int sub, string? artnr = null, decimal menge = 1m) =>
        new() { HauptFA = haupt, VaterFA = vater, Position = pos, SubFA = sub, HauptArtnr = "G", Artnr = artnr, Sollmenge = menge, Fertigungmenge = menge };

    private static (TestSwitch W, FakeReader View, FakeReader Sdata, FakeSyncLogger Logger, Mock<ISyncErrorNotifier> Notifier) Weiche(string? schalter)
    {
        var view = new FakeReader
        {
            Lesung = new(new() { N(835, null, null, 835), N(835, 835, 1, 836, "BG"), N(835, 836, 1, 0, "00722001", 0.6m) },
                new() { new FaHierarchyOrderInfo { HauptFA = 835 } }, 3, 1),
        };
        var sdata = new FakeReader
        {
            Lesung = new(new()
            {
                N(835, null, null, 835, "WURZEL-ARTNR", 99m), // Wurzel-Artnr/Menge werden nicht verglichen
                N(835, 835, 2, 836, "BG"),                     // Eltern-Kante weicht ab (Position)
                N(835, 836, 1, 0, "00722001", 0.60m),          // 0,60 = 0,6
                N(999, null, null, 999),                       // Struktur ohne FAInfos -> nicht im Vergleich
            }, new(), 4, 0),
        };
        var logger = new FakeSyncLogger();
        var notifier = new Mock<ISyncErrorNotifier>();
        return (new TestSwitch(view, sdata, notifier.Object, logger) { Schalter = schalter }, view, sdata, logger, notifier);
    }

    [Fact]
    public async Task Weiche_View_LiestNurView()
    {
        var (w, view, sdata, logger, _) = Weiche("View");
        (await w.LesenAsync()).Should().BeSameAs(view.Lesung);
        sdata.Aufrufe.Should().Be(0);
        logger.Runs.Should().BeEmpty();
        w.GenutzteQuelle.Should().Be(SageQuelle.View);
    }

    [Fact]
    public async Task Weiche_SData_BleibtGesperrt_WirktWieView_MitWarnungUndGrund()
    {
        // Auflage Gerald 2026-11-16: Umschalten auf SData bleibt gesperrt bis DPS-Antwort/P-FS3 und Rueckfragen 4/5 - Hybrid ist gebaut,
        // aber weder waehlbar noch wirksam.
        var (w, view, sdata, logger, _) = Weiche("SData");

        (await w.LesenAsync()).Should().BeSameAs(view.Lesung);

        (view.Aufrufe, sdata.Aufrufe).Should().Be((1, 0));
        w.GenutzteQuelle.Should().Be(SageQuelle.View);
        w.QuellenWarnung.Should().Contain("gesperrt bis P-FS3").And.Contain("Rückfragen 4/5").And.Contain("Quelle View");
        logger.Runs.Single(r => r.ServiceName == SyncLogServices.SageQuellenvergleich).Events
            .Should().ContainSingle(e => e.Level == "Warning" && e.Message.Contains("gesperrt bis P-FS3"));
    }

    [Fact]
    public void Sperre_FaStruktur_BleibtGesetzt_BeginntMitPruefabfrage() =>
        SageQuelleSperre.Grund(SageQuelleSchalter.FaStruktur, null).Should().StartWith("P-FS3").And.Contain("RealTimeData");

    [Fact]
    public async Task Weiche_SData_Hybrid_BeiSimulierterFreigabe_KnotenAusSData_InfosAusView_NurStrukturenMitInfos()
    {
        var (w, view, sdata, logger, _) = Weiche("SData");
        w.SperreSimuliertAufgehoben = true;

        var l = await w.LesenAsync();

        w.GenutzteQuelle.Should().Be(SageQuelle.SData);
        l.Knoten.Should().HaveCount(3).And.OnlyContain(k => k.HauptFA == 835, "Struktur 999 hat keine FAInfos-Zeile");
        l.Knoten.Should().Contain(k => k.Artnr == "WURZEL-ARTNR", "Knoten aus SData");
        l.Infos.Should().BeSameAs(view.Lesung.Infos);
        l.InfosAnzahl.Should().Be(1, "sonst ueberspraenge der Empty-Guard den Lauf");
        (view.Aufrufe, sdata.Aufrufe).Should().Be((1, 1));
        logger.Runs.Should().BeEmpty("SData-Modus ohne Warnung und ohne Vergleichslauf");
    }

    [Fact]
    public async Task Weiche_Vergleich_GibtViewZurueck_MeldetElternKante_OhneStrukturenOhneFaInfos()
    {
        var (w, view, sdata, logger, _) = Weiche("Vergleich");

        (await w.LesenAsync()).Should().BeSameAs(view.Lesung, "geschrieben wird nur aus der View");

        sdata.Aufrufe.Should().Be(1);
        var lauf = logger.Runs.Single(r => r.ServiceName == SyncLogServices.SageQuellenvergleich);
        lauf.FinalCounts!["abweichend"].Should().Be(1);
        lauf.FinalCounts["Schlüssel SData"].Should().Be(3, "Struktur 999 hat keine FAInfos (EXISTS wie im View-Select)");
        lauf.Events.Should().ContainSingle(e => e.Level == "Warning").Which.Message.Should().Contain("[FA 836] Position").And.Contain("'1'").And.Contain("'2'");
    }

    [Fact]
    public async Task Weiche_Vergleich_SdataFehler_ViewDatenWeiter_Mail()
    {
        var (w, view, sdata, logger, notifier) = Weiche("Vergleich");
        sdata.ThrowOnRead = new SdataLeseException("PpsFaBelegeInfo", 500, "Ungültiger Spaltenname");

        (await w.LesenAsync()).Should().BeSameAs(view.Lesung);

        logger.Runs.Single(r => r.ServiceName == SyncLogServices.SageQuellenvergleich).FinishedFailed.Should().BeTrue();
        notifier.Verify(n => n.NotifyAsync(It.IsAny<string>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static readonly List<ProductionOrderPosition> SdataPositionen = new() { new() { BelPosID = 10641, BelID = 836, StrukturID = 835 } };

    [Fact]
    public async Task Weiche_Positionen_NurWennSdataGelesen()
    {
        foreach (var (schalter, erwartet) in new[] { ("View", false), ("SData", false), ("Neu", false), ("Vergleich", true) })
        {
            var (w, view, sdata, _, _) = Weiche(schalter);
            sdata.Lesung = sdata.Lesung with { Positionen = SdataPositionen };

            var l = await w.LesenAsync();

            l.Knoten.Should().BeSameAs(view.Lesung.Knoten, $"{schalter}: geschrieben wird die Struktur nur aus der View");
            if (erwartet) l.Positionen.Should().BeSameAs(SdataPositionen, "Vergleich: Positionen aus SData (keine View-Quelle, F6)");
            else l.Positionen.Should().BeNull($"{schalter}: SData nicht gelesen -> Tabelle bleibt unberuehrt");
        }
    }

    [Fact]
    public async Task Weiche_Vergleich_SdataFehler_KeinePositionen()
    {
        var (w, _, sdata, _, _) = Weiche("Vergleich");
        sdata.Lesung = sdata.Lesung with { Positionen = SdataPositionen };
        sdata.ThrowOnRead = new SdataLeseException("PpsFaBelegePositionenInfo", 500, "Ungültiger Spaltenname 'Status'");

        (await w.LesenAsync()).Positionen.Should().BeNull("ein gescheiterter SData-Read darf die Positionen nie leeren");
    }

    [Fact]
    public void Schluessel_FaUeberBelId_BlattUeberPlatzImVater()
    {
        FaStrukturQuelleSwitch.Schluessel(N(835, null, null, 835)).Should().Be("FA 835");
        FaStrukturQuelleSwitch.Schluessel(N(835, 835, 1, 836)).Should().Be("FA 836");
        FaStrukturQuelleSwitch.Schluessel(N(835, 836, 3, 0)).Should().Be("Blatt 836/3");
    }

    // ----- Abgleich ueber das Reader-Interface (Extraktion E4a; Pfade ohne Schreib-SQL) -----

    private static (FaHierarchySyncService S, FakeSyncLogger Logger, Mock<ISyncErrorNotifier> Notifier) Abgleich(ISageFaStrukturReader reader)
    {
        var logger = new FakeSyncLogger();
        var notifier = new Mock<ISyncErrorNotifier>();
        return (new FaHierarchySyncService(TestDbContextFactory.Create(), reader, notifier.Object,
            NullLogger<FaHierarchySyncService>.Instance, logger), logger, notifier);
    }

    [Fact]
    public async Task Abgleich_LeererViewRead_UeberspringtOhneMutation_MitMail()
    {
        var (s, logger, notifier) = Abgleich(new FakeReader { Lesung = new(new(), new(), 5, 0) });

        var r = await s.RunAsync(dryRun: false);

        r.Skipped.Should().BeTrue();
        var lauf = logger.Runs.Single();
        lauf.FinalMessageSuffix.Should().Be("[uebersprungen: leerer View-Read]");
        lauf.Events.Should().Contain(e => e.Level == "Warning" && e.Message.Contains("FAListe=5, FAInfos=0"));
        notifier.Verify(n => n.NotifyAsync("FaHierarchy-Sync: leerer View-Read", It.IsAny<Exception>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Abgleich_UngueltigerViewName_Uebersprungen_Fehler_Mail_KeinThrow()
    {
        var (s, logger, notifier) = Abgleich(new FakeReader { ThrowOnRead = new FaHierarchyViewNameException("[x];DROP") });

        var r = await s.RunAsync(dryRun: false);

        r.Skipped.Should().BeTrue();
        logger.Runs.Single().FinishedFailed.Should().BeTrue();
        notifier.Verify(n => n.NotifyAsync("FaHierarchy-Sync: ungueltiger View-Name", It.IsAny<Exception>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Abgleich_DryRun_ZaehltKnotenUndInfos_SchreibtNichts()
    {
        var reader = new FakeReader { Lesung = new(new() { N(1, null, null, 1) }, new() { new FaHierarchyOrderInfo { HauptFA = 1 } }, 1, 1) };
        var (s, logger, _) = Abgleich(reader);

        var r = await s.RunAsync(dryRun: true);

        (r.Nodes, r.OrderInfos, r.DryRun).Should().Be((1, 1, true));
        logger.Runs.Single().FinalCounts!["neu"].Should().Be(2);
    }

    [Fact]
    public async Task Abgleich_DryRun_MitPositionen_CountKeysUnveraendert_InfoZeile()
    {
        var reader = new FakeReader
        {
            Lesung = new(new() { N(1, null, null, 1) }, new() { new FaHierarchyOrderInfo { HauptFA = 1 } }, 1, 1, SdataPositionen),
        };
        var (s, logger, _) = Abgleich(reader);

        await s.RunAsync(dryRun: true);

        var lauf = logger.Runs.Single();
        lauf.FinalCounts!.Keys.Should().BeEquivalentTo("neu", "geloescht");
        lauf.Events.Should().Contain(e => e.Level == "Info" && e.Message.Contains("würde 1 Positionen"));
    }

    [Fact]
    public async Task PositionenErsetzen_LeereListe_ErsetztNicht_Warnung_KeineMail()
    {
        var (s, logger, notifier) = Abgleich(new FakeReader());
        var run = (FakeSyncRun)await logger.BeginRunAsync(SyncLogServices.FaHierarchy);

        (await s.PositionenErsetzenAsync(run, new(), CancellationToken.None)).Should().Be(0);

        run.Events.Should().ContainSingle(e => e.Level == "Warning" && e.Message.Contains("keine Positionen"));
        notifier.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PositionenErsetzen_SchreibFehler_FehlerImProtokoll_Mail_KeinThrow()
    {
        // InMemory kennt keine Transaktion/kein Raw-SQL -> derselbe Pfad wie ein Fehler am SQL Server (z. B. UNIQUE BelPosID).
        var (s, logger, notifier) = Abgleich(new FakeReader());
        var run = (FakeSyncRun)await logger.BeginRunAsync(SyncLogServices.FaHierarchy);

        (await s.PositionenErsetzenAsync(run, SdataPositionen, CancellationToken.None)).Should().Be(0);

        run.Events.Should().ContainSingle(e => e.Level == "Error" && e.Message.StartsWith("ProductionOrderPositions nicht ersetzt"));
        notifier.Verify(n => n.NotifyAsync("FaHierarchy-Sync: FA-Positionen nicht ersetzt", It.IsAny<Exception>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Abgleich_LeseFehler_FinishFailed_Rethrow()
    {
        var (s, logger, _) = Abgleich(new FakeReader { ThrowOnRead = new InvalidOperationException("View fehlt") });

        await s.Invoking(x => x.RunAsync(dryRun: false)).Should().ThrowAsync<InvalidOperationException>();
        logger.Runs.Single().FinishedFailed.Should().BeTrue();
    }

    // ----- DI (fallstricke §10, wie ADR 0013) -----

    [Fact]
    public void DI_FaStrukturReader_IstWeiche_MitBeidenReadern_AbgleichAufloesbar()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddSingleton(new Mock<ISyncLogger>().Object);
        services.AddScoped(_ => new Mock<ISyncErrorNotifier>().Object);
        services.AddScoped(_ => TestDbContextFactory.Create());
        services.AddSageSdataReadClient();
        services.AddFaStrukturQuelle();
        services.AddScoped<IFaHierarchySyncService, FaHierarchySyncService>();
        using var sp = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using var scope = sp.CreateScope();

        scope.ServiceProvider.GetRequiredService<ISageFaStrukturReader>().Should().BeOfType<FaStrukturQuelleSwitch>();
        scope.ServiceProvider.GetRequiredService<SageFaStrukturReader>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<SdataFaStrukturReader>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IFaHierarchySyncService>().Should().NotBeNull();
    }
}
