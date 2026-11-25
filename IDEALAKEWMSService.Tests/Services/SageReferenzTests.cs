using FluentAssertions;
using IdealAkeWms.Models;
using IDEALAKEWMSService.Services;
using IDEALAKEWMSService.Services.Sdata;

namespace IDEALAKEWMSService.Tests.Services;

/// <summary>v1.99.68 (Spec 2026-11-24-sage-buchung-referenz-benutzer-abteilung): Referenz/Memo-Text mit Kennung, Benutzer, Abteilung.</summary>
public class SageReferenzTests
{
    [Fact]
    public void Kurz_KennungAbteilungBenutzer_Passt()
    {
        SageReferenz.Kurz("SM#AT#1234#", "C1", "Gerald Weichbold").Should().Be("SM#AT#1234# C1 Gerald Weichbold", "genau 31 Zeichen");
    }

    [Fact]
    public void Kurz_KuerztZuerstBenutzerAmEnde_DannAbteilung_KennungNie()
    {
        // Beispiel aus der Freigabe: 33 Zeichen -> Benutzer am Ende abgeschnitten
        var r = SageReferenz.Kurz("SM#ATI#12345#", "C1", "Gerald Weichbold");
        r.Should().Be("SM#ATI#12345# C1 Gerald Weichbo").And.HaveLength(SageReferenz.ReferenzMaxLaenge);

        var lang = new string('A', 200);
        SageReferenz.Kurz("SM#ABCDE#2147483647#", lang, new string('B', 200))
            .Should().Be("SM#ABCDE#2147483647# " + new string('A', 10), "Benutzer ganz weg, dann Abteilung gekuerzt");
        SageReferenz.Kurz("SM#ABCDE#2147483647#", null, new string('B', 200))
            .Should().Be("SM#ABCDE#2147483647# " + new string('B', 10));
        SageReferenz.Kurz(new string('K', 40), "C1", "x").Should().Be(new string('K', 40), "Kennung wird nie gekuerzt");
    }

    [Fact]
    public void Kurz_LeereTeileEntfallen_OhneDoppeltesLeerzeichen_KeinLeerzeichenAmEnde()
    {
        SageReferenz.Kurz("SM#7#", null, "  ").Should().Be("SM#7#");
        SageReferenz.Kurz("SM#7#", " ", "Max").Should().Be("SM#7# Max");
        // Schnitt genau hinter der Abteilung -> kein Leerzeichen am Ende
        SageReferenz.Kurz("SM#ABCDE#2147483647#", "Abteilung1", "Max").Should().Be("SM#ABCDE#2147483647# Abteilung1");
    }

    [Fact]
    public void Voll_FormatMitUndOhneAbteilung_BenutzerUnveraendert()
    {
        SageReferenz.Voll("SM#AT#5#", "Gerald Weichbold", "C1").Should().Be("WMS: SM#AT#5# Gerald Weichbold Abteilung:C1");
        SageReferenz.Voll("WE#7#", "Gerald Weichbold", null).Should().Be("WMS: WE#7# Gerald Weichbold");
        SageReferenz.Voll("WE#7#", null, "  ").Should().Be("WMS: WE#7#");
        var lang = new string('A', 200);
        SageReferenz.Voll("SM#1#", lang, lang).Should().Be($"WMS: SM#1# {lang} Abteilung:{lang}", "Memo ist varchar(max), keine Kuerzung");
    }

    [Fact]
    public void Lagerbuchung_ReferenzUndZeilenMemo_KopfMemoUnveraendert_AK1_AK2()
    {
        var m = new StockMovement
        {
            Id = 12345, MovementType = MovementType.Ausbuchung, Quantity = 1m, CreatedBy = "Gerald Weichbold", Abteilung = "C1",
            Article = new Article { ArticleNumber = "A" },
            StorageLocation = new StorageLocation { Code = "L", SageLagerkennung = "LL;1;4;0", SageLagerplatzId = 1 },
        };
        var p = SageLagerbuchungPayloadBuilder.Build(m, "SM#ATI#12345#");

        p.Memo.Should().Be("IdealAkeWms Lagerbuchung SM#ATI#12345#", "Kopf-Memo byte-gleich (Korrelation)");
        var z = p.Lagerbuchungen.Single();
        z.Referenz.Should().Be("SM#ATI#12345# C1 Gerald Weichbo");
        z.Memo.Should().Be("WMS: SM#ATI#12345# Gerald Weichbold Abteilung:C1");

        // AK5: erneuter Versand mit gespeicherter Kennung -> derselbe Text
        var p2 = SageLagerbuchungPayloadBuilder.Build(m, "SM#ATI#12345#");
        (p2.Lagerbuchungen.Single().Referenz, p2.Lagerbuchungen.Single().Memo).Should().Be((z.Referenz, z.Memo));
    }

    [Fact]
    public void Lagerbuchung_Einbuchung_OhneAbteilung_OhneKennungsparameter_AK3()
    {
        var m = new StockMovement
        {
            Id = 7, MovementType = MovementType.Einbuchung, Quantity = 1m, CreatedBy = "max",
            Article = new Article { ArticleNumber = "A" },
            StorageLocation = new StorageLocation { Code = "L", SageLagerkennung = "LL;1;4;0", SageLagerplatzId = 1 },
        };
        var z = SageLagerbuchungPayloadBuilder.Build(m).Lagerbuchungen.Single();
        (z.Referenz, z.Memo).Should().Be(("SM#7# max", "WMS: SM#7# max"));
    }

    [Fact]
    public void Lagerbuchung_Serialisiert_ReferenzUndMemoInDerZeile()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(new SageLagerbuchungZeile { Referenz = "R", Memo = "M" });
        json.Should().Contain("\"Referenz\":\"R\"").And.Contain("\"Memo\":\"M\"");
    }

    [Fact]
    public void WeKopfMemo_WiedererkennungUnveraendert_AK7()
    {
        var memo = SageReferenz.Voll("WE#DEV#7#", "Gerald Weichbold", null);
        SdataWareneingangLookupReader.FremdeKennung(memo, "WE#DEV#7#").Should().BeFalse();
        memo.Should().Contain("WE#DEV#7#", "Memo like '%WE#DEV#7#%' trifft weiter");
    }
}
