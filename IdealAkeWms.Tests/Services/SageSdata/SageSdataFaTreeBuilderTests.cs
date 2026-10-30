using System.Text.Json;
using FluentAssertions;
using IdealAkeWms.Services.SageSdata;

namespace IdealAkeWms.Tests.Services.SageSdata;

/// <summary>Echte Sage-Antworten vom AKE-Test 2026-10-30 (Kopie der Backlog-Anhaenge, Huelle _quelle/belid_…).</summary>
internal static class SageSdataFixtures
{
    private static string Datei(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "SageSdata", name));

    /// <summary>Der <c>$resources</c>-Feed als JSON-Text, wie Sage ihn liefert (optional unter einem Huellen-Schluessel).</summary>
    public static string Feed(string datei, string? huelle = null)
    {
        using var doc = JsonDocument.Parse(Datei(datei));
        var wurzel = huelle is null ? doc.RootElement : doc.RootElement.GetProperty(huelle);
        return $"{{\"$resources\":{wurzel.GetProperty("$resources").GetRawText()}}}";
    }

    public const string Struktur835 = "dtsFaBelegsuche-struktur-835.json";
    public const string Positionen = "dtsTeileverwendungsnachweis-belid-835-836.json";

    public static List<T> Lies<T>(string feed) =>
        JsonSerializer.Deserialize<SdataFeed<T>>(feed, SageSdataReadClient.JsonOptions)!.Resources!;

    public static List<SageFaKopf> Fas835() => Lies<SageFaKopf>(Feed(Struktur835));

    public static List<SageFaPosition> Positionen835Und836() =>
        Lies<SageFaPosition>(Feed(Positionen, "belid_835")).Concat(Lies<SageFaPosition>(Feed(Positionen, "belid_836"))).ToList();
}

public class SageSdataFaTreeBuilderTests
{
    private static SageFaKopf Fa(int belId, int strukturId, string artikel, string verursacher = "FA") =>
        new(belId, strukturId, artikel, null, 1, 1, 0, -1, 0, 0, 0, verursacher, null, null, null);

    private static SageFaPosition Ma(int belId, string ressource) =>
        new("MA", ressource, null, 1, null, null, null, 2, belId, 0);

    [Fact]
    public void Fixture835_HauptFaMitSechsSubFas_KeineMeldung()
    {
        var s = SageSdataFaTreeBuilder.Build(SageSdataFixtures.Fas835(), SageSdataFixtures.Positionen835Und836());

        s.HauptFa!.BelID.Should().Be(835);
        s.HauptFa.VerursacherTyp.Should().Be("MAN");
        s.Meldungen.Should().BeEmpty();
        s.NichtZugeordnet.Should().BeEmpty();
        s.Knoten.Should().HaveCount(7);
        s.Knoten[0].Ebene.Should().Be(0);
        s.Knoten.Skip(1).Should().OnlyContain(k => k.Ebene == 1);

        var haupt = s.Knoten[0].Positionen;
        haupt.Should().HaveCount(11);
        haupt.Count(z => z.SubFaBelId is not null).Should().Be(6);
        haupt.Where(z => z.SubFaBelId is not null).Select(z => z.SubFaBelId!.Value).Should().BeEquivalentTo(new[] { 836, 837, 838, 839, 840, 841 });
        haupt.Single(z => z.Position.Ressourcetyp == "AG").Position.Arbeitsplatznummer.Should().Be("9999");

        var k836 = s.Knoten.Single(k => k.Fa.BelID == 836);
        k836.Positionen.Should().ContainSingle().Which.Position.Menge.Should().Be(0.6m);
    }

    [Fact]
    public void Fixture_SageBooleanUndTermine()
    {
        var haupt = SageSdataFixtures.Fas835().Single(f => f.BelID == 835);
        SageBool.IsTrue(haupt.IstEingelastet).Should().BeTrue();   // -1
        SageBool.IsTrue(haupt.IstBegonnen).Should().BeFalse();     // 0
        SageBool.IsTrue(null).Should().BeFalse();
        haupt.Istmenge.Should().Be(0);
        haupt.Sollstarttermin.Should().Be(new DateTimeOffset(2026, 2, 3, 8, 45, 0, TimeSpan.FromHours(1)));
    }

    [Fact]
    public void TerminOhneZeitzone_WirdGelesen()
    {
        var zeilen = SageSdataFixtures.Lies<SageFaKopf>(
            "{\"$resources\":[{\"BelID\":1,\"StrukturID\":1,\"Sollstarttermin\":\"2026-02-03T00:00:00\",\"$url\":\"x\"}]}");
        zeilen.Single().Sollstarttermin!.Value.DateTime.Should().Be(new DateTime(2026, 2, 3));
    }

    [Fact]
    public void VkWurzel_IstHauptFa_KeineMeldung()
    {
        var s = SageSdataFaTreeBuilder.Build(
            new[] { Fa(846, 846, "A", "VK"), Fa(847, 846, "B") },
            new[] { Ma(846, "B") });

        s.HauptFa!.BelID.Should().Be(846);
        s.Meldungen.Should().BeEmpty();
        s.Knoten.Select(k => (k.Fa.BelID, k.Ebene)).Should().Equal((846, 0), (847, 1));
    }

    [Fact]
    public void ZweistufigeStruktur_SubFaEbeneZwei_TiefensortiertUnterIhremVater()
    {
        var s = SageSdataFaTreeBuilder.Build(
            new[] { Fa(1, 1, "R", "MAN"), Fa(2, 1, "B"), Fa(3, 1, "C"), Fa(4, 1, "D") },
            new[] { Ma(1, "B"), Ma(1, "D"), Ma(2, "C") });

        s.Meldungen.Should().BeEmpty();
        s.Knoten.Select(k => (k.Fa.BelID, k.Ebene)).Should().Equal((1, 0), (2, 1), (3, 2), (4, 1));
    }

    [Fact]
    public void ZweiSubFasGleicherArtikel_Meldung_BeideNichtZugeordnet()
    {
        var s = SageSdataFaTreeBuilder.Build(
            new[] { Fa(835, 835, "R", "MAN"), Fa(837, 835, "86003866"), Fa(842, 835, "86003866") },
            new[] { Ma(835, "86003866") });

        s.Meldungen.Should().ContainSingle().Which.Should().Contain("837, 842").And.Contain("86003866").And.Contain("nicht eindeutig");
        s.NichtZugeordnet.Select(k => k.Fa.BelID).Should().BeEquivalentTo(new[] { 837, 842 });
        s.Knoten.Should().ContainSingle();
        s.Knoten[0].Positionen.Single().SubFaBelId.Should().BeNull();
    }

    [Fact]
    public void GleicherArtikelAnZweiPositionen_Meldung_KeineZuordnung()
    {
        var s = SageSdataFaTreeBuilder.Build(
            new[] { Fa(1, 1, "R", "MAN"), Fa(2, 1, "B") },
            new[] { Ma(1, "B"), Ma(1, "B") });

        s.Meldungen.Should().ContainSingle().Which.Should().Contain("an 2 Positionen");
        s.NichtZugeordnet.Should().ContainSingle().Which.Fa.BelID.Should().Be(2);
        s.Knoten[0].Positionen.Should().OnlyContain(z => z.SubFaBelId == null);
    }

    [Fact]
    public void SubFaOhnePosition_Meldung_UnterNichtZugeordnetMitIhrenPositionen()
    {
        var s = SageSdataFaTreeBuilder.Build(
            new[] { Fa(1, 1, "R", "MAN"), Fa(2, 1, "B") },
            new[] { Ma(2, "X") });

        s.Meldungen.Should().ContainSingle().Which.Should().Contain("Sub-FA 2").And.Contain("keiner Position");
        s.NichtZugeordnet.Single().Positionen.Should().ContainSingle();
    }

    [Fact]
    public void OhneHauptFa_Meldung_AllesNichtZugeordnet()
    {
        var s = SageSdataFaTreeBuilder.Build(new[] { Fa(2, 1, "B"), Fa(3, 1, "C") }, new[] { Ma(2, "C") });

        s.HauptFa.Should().BeNull();
        s.Meldungen.Should().Contain(m => m.Contains("keinen Haupt-FA"));
        s.Knoten.Should().BeEmpty();
        s.NichtZugeordnet.Should().HaveCount(2);
    }

    [Fact]
    public void Zyklus_KeinEndloslauf_Meldung()
    {
        // 2 verweist auf 3, 3 auf 2; der Haupt-FA auf keinen.
        var s = SageSdataFaTreeBuilder.Build(
            new[] { Fa(1, 1, "R", "MAN"), Fa(2, 1, "B"), Fa(3, 1, "C") },
            new[] { Ma(2, "C"), Ma(3, "B") });

        s.Knoten.Should().ContainSingle();
        s.NichtZugeordnet.Select(k => k.Fa.BelID).Should().BeEquivalentTo(new[] { 2, 3 });
        s.Meldungen.Should().Contain(m => m.Contains("nicht erreichbar"));
    }

    [Fact]
    public void VsUndAgPositionen_WerdenAngezeigt_NieZugeordnet()
    {
        var s = SageSdataFaTreeBuilder.Build(
            new[] { Fa(1, 1, "R", "MAN"), Fa(2, 1, "B") },
            new[] { new SageFaPosition("VS", "B", null, 1, null, null, null, 2, 1, 0), new SageFaPosition("AG", "B", null, 0, "B", "9", null, 2, 1, 0) });

        s.Knoten[0].Positionen.Should().HaveCount(2).And.OnlyContain(z => z.SubFaBelId == null);
        s.Meldungen.Should().ContainSingle().Which.Should().Contain("keiner Position");
    }

    [Fact]
    public void PositionenFremderBelIds_WerdenGemeldet()
    {
        var s = SageSdataFaTreeBuilder.Build(new[] { Fa(1, 1, "R", "MAN") }, new[] { Ma(1, "X"), Ma(99, "Y") });

        s.Knoten[0].Positionen.Should().ContainSingle();
        s.Meldungen.Should().ContainSingle().Which.Should().Contain("1 Position(en)");
    }

    [Fact]
    public void Summiere_JeBelId_AnzahlUndMengeGut()
    {
        var r = SageSdataFaTreeBuilder.Summiere(new[] { new SageRueckmeldung(1, 2m), new SageRueckmeldung(1, 3.5m), new SageRueckmeldung(2, null) });

        r[1].Should().Be((2, 5.5m));
        r[2].Should().Be((1, 0m));
    }

    [Fact]
    public void OrBloecke_HoechstensFuenfzigJeBlock_OhneDoppelte()
    {
        var bloecke = SageSdataQuellen.OrBloecke("BelID", Enumerable.Range(1, 120).Append(5));

        bloecke.Should().HaveCount(3);
        bloecke[0].Should().StartWith("BelID eq 1 or BelID eq 2").And.EndWith("BelID eq 50");
        bloecke[2].Split(" or ").Should().HaveCount(20);
        SageSdataQuellen.OrBloecke("BelID", new[] { 835, 836 }).Should().Equal("BelID eq 835 or BelID eq 836");
    }
}
