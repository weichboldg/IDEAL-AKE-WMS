using System.Text.Json;
using FluentAssertions;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Services.SageSdata;
using Xunit;

namespace IdealAkeWms.Tests.Services;

/// <summary>v1.99.38 (Spec 2026-11-17-arbeitsgaenge-aus-wms-in-sage-fa-anlegen E0): Vorschau als reine Funktion, Sage-Stand nur GET.</summary>
public class SageFaArbeitsgaengeTests
{
    private static readonly List<OperationConfig> Configs = new()
    {
        new() { Id = 1, OperationName = "PU", SageArbeitsgangnummer = "180" },
        new() { Id = 2, OperationName = "VL", SageArbeitsgangnummer = "999", SageArbeitsgangnummerManuell = "260" }, // Hand vor Sage
        new() { Id = 3, OperationName = "MK" },                                                                      // ohne Sage-AG
        new() { Id = 4, OperationName = "PR", SageArbeitsgangnummer = "310" },
        new() { Id = 5, OperationName = "PL", SageArbeitsgangnummer = "320" },
        new() { Id = 6, OperationName = "VP", SageArbeitsgangnummer = "330" },
        new() { Id = 7, OperationName = " pu ", SageArbeitsgangnummer = "111" },                                     // Dublette: kleinste Id gewinnt
    };

    private static FaHierarchyNode Knoten(int haupt, int? vater, int sub, string? schritte, DateTime? weg = null) => new()
    {
        HauptFA = haupt, VaterFA = vater, SubFA = sub, Arbeitsschritte = schritte, Artnr = "A" + sub, Matchcode = "M" + sub, VerschwundenSeit = weg,
    };

    [Fact]
    public void Ak1_SiebenZeilen_OhneSageAg_UndNummern_NichtGeprueft()
    {
        var e = SageFaArbeitsgaenge.BuildVorschau(new[] { Knoten(1059463, null, 1059463, "PU ISO VL MK PR PL VP") }, Configs, null);

        e.Zeilen.Select(z => (z.Kuerzel, z.SageAg, z.Reihenfolge)).Should().Equal(
            ("PU", "180", 1), ("ISO", null, 2), ("VL", "260", 3), ("MK", null, 4), ("PR", "310", 5), ("PL", "320", 6), ("VP", "330", 7));
        e.Zeilen.Where(z => z.SageAg == null).Should().OnlyContain(z => z.Status == SageFaArbeitsgaenge.OhneSageAg);
        e.Zeilen.Where(z => z.SageAg != null).Should().OnlyContain(z => z.Status == SageFaArbeitsgaenge.NichtGeprueft);
        e.Zeilen.Should().OnlyContain(z => z.IstHauptFa && z.BelId == 1059463);
    }

    [Fact]
    public void Blaetter_Gezaehlt_Verschwundene_Weg_Dubletten_Einmal_MitHinweis()
    {
        var e = SageFaArbeitsgaenge.BuildVorschau(new[]
        {
            Knoten(1, null, 1, "PU"),
            Knoten(1, 1, 2, "PR pr PL"),
            Knoten(1, 2, 0, "VP"),                                // Blatt
            Knoten(1, 1, 3, "PU", weg: DateTime.Now),             // verschwunden
        }, Configs, null);

        e.Blaetter.Should().Be(1);
        e.Zeilen.Select(z => (z.BelId, z.Kuerzel)).Should().Equal((1, "PU"), (2, "PR"), (2, "PL"));
        e.Zeilen.Single(z => z.Kuerzel == "PR").Hinweis.Should().Contain("2× im Feld");
        e.Zeilen.Single(z => z.BelId == 2 && z.Kuerzel == "PR").IstHauptFa.Should().BeFalse();
    }

    [Fact]
    public void MitSageStand_Status_JeRegel_UndNurInSage()
    {
        var stand = new Dictionary<int, SageFaArbeitsgaenge.FaStand>
        {
            [1] = new(true, false, false, 2, new HashSet<string> { "180", "500" }),  // 180 vorhanden, 500 nur in Sage
            [2] = new(false, true, false, null, new HashSet<string>()),             // nicht in PpsFaBelegeInfo
            [3] = new(true, false, true, 1, new HashSet<string>()),                 // begonnen
        };
        var e = SageFaArbeitsgaenge.BuildVorschau(new[]
        {
            Knoten(1, null, 1, "PU PR MK"), Knoten(1, 1, 2, "PL"), Knoten(1, 1, 3, "VP"), Knoten(1, 1, 4, "VL"),
        }, Configs, stand);

        e.Zeilen.Select(z => (z.Kuerzel, z.Status)).Should().Equal(
            ("PU", SageFaArbeitsgaenge.InSageVorhanden), ("PR", SageFaArbeitsgaenge.Anlegbar), ("MK", SageFaArbeitsgaenge.OhneSageAg),
            ("PL", SageFaArbeitsgaenge.FaBeendet), ("VP", SageFaArbeitsgaenge.FaBegonnen),
            ("VL", SageFaArbeitsgaenge.SageNichtLesbar)); // FA 4 nicht gelesen: nie anlegbar
        e.NurInSage.Should().Be(1);
        e.Zeilen.First().AgInSage.Should().Be(2);
    }

    [Fact]
    public void SageFehler_KeineZeileAnlegbar()
    {
        var e = SageFaArbeitsgaenge.BuildVorschau(new[] { Knoten(1, null, 1, "PU MK") }, Configs, null, sageFehler: true);
        e.Zeilen.Select(z => z.Status).Should().Equal(SageFaArbeitsgaenge.SageNichtLesbar, SageFaArbeitsgaenge.OhneSageAg);
    }

    [Fact]
    public void QueueStatus_GewinntVorLiveStand()
    {
        var q = new Dictionary<(int, string), (string, string?)> { [(1, "180")] = ("eingereiht", null) };
        var e = SageFaArbeitsgaenge.BuildVorschau(new[] { Knoten(1, null, 1, "PU") }, Configs,
            new Dictionary<int, SageFaArbeitsgaenge.FaStand> { [1] = new(true, false, false, 1, new HashSet<string>()) }, queueStatus: q);
        e.Zeilen.Single().Status.Should().Be("eingereiht");
    }

    [Fact]
    public void Auswerten_JsonWieSage_ZahlenUndBooleans_NurAgPositionen()
    {
        const string koepfe = """
            [{"BelID":"10","Sollmenge":5,"Fertigungmenge":0,"Istmenge":0,"IstBegonnen":0,"IstBeendet":0,"Aktiv":-1},
             {"BelID":11,"Fertigungmenge":3,"Istmenge":1,"IstBeendet":0,"Aktiv":-1},
             {"BelID":12,"Fertigungmenge":3,"IstBeendet":0,"Aktiv":0}]
            """;
        const string positionen = """
            [{"BelID":10,"RessourceTyp":"AG","RessourceNummer":"3001","ArbeitsgangNummer":180},
             {"BelID":10,"RessourceTyp":"MA","RessourceNummer":"320","ArbeitsgangNummer":null}]
            """;
        var k = JsonSerializer.Deserialize<List<SageFaArbeitsgaenge.KopfZeile>>(koepfe, SageSdataReadClient.PagingJsonOptions)!;
        var p = JsonSerializer.Deserialize<List<SageFaArbeitsgaenge.PositionZeile>>(positionen, SageSdataReadClient.PagingJsonOptions)!;

        var s = SageFaArbeitsgaenge.Auswerten(new[] { 10, 11, 12, 13 }, k, p);

        s[10].Should().Match<SageFaArbeitsgaenge.FaStand>(x => x.Gefunden && !x.Beendet && !x.Begonnen && x.Fertigungsmenge == 5);
        s[10].AgNummern.Should().BeEquivalentTo("180", "3001");           // MA-Position zaehlt nicht
        s[11].Begonnen.Should().BeTrue();
        s[12].Beendet.Should().BeTrue();                                  // Aktiv = 0
        s[13].Should().Match<SageFaArbeitsgaenge.FaStand>(x => !x.Gefunden && x.Beendet);
    }

    [Fact]
    public async Task LesenAsync_NurGet_MitOrderBy_InBloecken()
    {
        var client = new FakeClient();
        var ziel = new SdataLeseZiel("https://x", "ol", "IDEAL;1", "u", "p", true);
        await SageFaArbeitsgaenge.LesenAsync(client, ziel, Enumerable.Range(1, 60), CancellationToken.None);

        client.Aufrufe.Should().HaveCount(4);
        client.Aufrufe.Should().OnlyContain(a => a.OrderBy == "BelID" || a.OrderBy == "BelPosID");
        client.Aufrufe.Select(a => a.Where).Distinct().Should().HaveCount(2);
    }

    private sealed class FakeClient : ISageSdataReadClient
    {
        public List<(string Ressource, string? Where, string OrderBy)> Aufrufe { get; } = new();
        public Task<SdataErgebnis<T>> GetAsync<T>(SdataLeseZiel ziel, string quelle, string where, string select, string? orderBy, CancellationToken ct)
            => throw new NotSupportedException();
        public Task<SdataSeiten<T>> QueryAllAsync<T>(SdataLeseZiel ziel, string vertrag, string ressource, string? where, string? select,
            string orderBy, CancellationToken ct, int count = SageSdataReadClient.Seitengroesse)
        {
            Aufrufe.Add((ressource, where, orderBy));
            return Task.FromResult(new SdataSeiten<T>(Array.Empty<T>(), ressource, 1, 0, 0));
        }
    }
}
