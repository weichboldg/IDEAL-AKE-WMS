using System.Reflection;
using FluentAssertions;
using IdealAkeWms.Models;

namespace IdealAkeWms.Tests.Models;

/// <summary>Spec 2026-11-07-einstellungen-auswahlfelder-und-beschreibung (E1, ADR 0020): Katalog der AppSettings.</summary>
public class AppSettingDefinitionsTests
{
    /// <summary>Die bis v1.76.0 in Program.cs fest codierten Seed-Eintraege (acht Bloecke, 38 Keys; seit v1.79.0 ohne OseonRueckmeldungAktiv/SageRueckmeldungAktiv = 36, Spec 2026-11-08-bereinigungen F5) — AK 2.</summary>
    private static readonly (string Key, string Value)[] FruehererSeed =
    {
        ("TeileverfolgungAktiv", "false"),
        ("OseonReportingHorizonDays", "10"), ("OseonReportingOverdueLookbackDays", "90"),
        ("LeitstandAktiv", "false"), ("KommissionierungMitZuweisung", "false"), ("FaCompletionAktiv", "false"),
        ("LackierteilKategorieName", ""),
        ("BestellungenAktiv", "false"), ("DefaultLagerbestellempfaengerId", ""), ("DefaultGlasbestellempfaengerId", ""),
        ("GlasArtikelgruppen", ""), ("GemeinsameArtikelgruppen", "EUZ"), ("LagerbestellungAktiv", "true"),
        ("VaultLinksAktiv", "true"),
        ("VaultLinkVorlage", "http://akevault24.ake.at/AutodeskTC/AKE-VAULT01/explore?search={Artikelnummer}&searchContext=0"),
        ("EnaioLinksAktiv", "true"), ("EnaioLinkVorlage", "http://akeosapp.ake.at/oscontentviewer/viewer/{ObjektId}/?pagecount=1"),
        ("WindowsAuthAktiv", "false"), ("WindowsAuthBerechtigungsgruppe", ""),
        ("BdeAktiv", "false"), ("BdeNurFaMeldung", "false"), ("BdeDefaultArbeitsgang", ""), ("BdeMehrfachBuchungProOperator", "false"),
        ("BdeMehrfachBuchungProArbeitsgang", "false"), ("BdeGleichzeitigerAbschlussBeiMehrfachStart", "false"), ("BdeSchichtkalenderAktiv", "false"),
        ("BdeTerminalMaxHauptFa", "5"),
        ("FaHierarchyKommissionierlistenAktiv", "false"), ("FaHierarchyBeschichtungAktiv", "false"), ("FaHierarchyVormontageAktiv", "false"),
        ("FaHierarchyMaxTiefe", "500"),
        ("OseonAmpelGelbTage", "1"), ("OseonAmpelBlauTage", "2"), ("QrMitFaNummer", "false"), ("QrErstesFeld", "Artikelnummer"), ("QrTrennzeichen", ";"),
        // v1.99.25 (Spec 2026-11-16-beschichtungslogik-je-standort): neue Keys mit Seed
        ("BeschichtungKennzeichenQuelle", "Automatisch"), ("BeschichtungTerminQuelle", "Automatisch"),
        // v1.99.43 (Spec 2026-11-18-ideal-kommissionierliste-filter-spalten-leitstand)
        ("FaHierarchyKommissionierlisteVorfilterZiele", "K-01,K-02"), ("FaHierarchyKommissionierlisteNurFreigegebene", "true"),
    };

    [Fact]
    public void Seed_GleichDerFrueherenListe_AK2()
    {
        FruehererSeed.Should().HaveCount(40);
        AppSettingDefinitions.SeedEintraege.Select(e => (e.Key, e.Value)).Should().BeEquivalentTo(FruehererSeed);
    }

    [Fact]
    public void Katalog_EnthaeltJedenKeyGenauEinmal_MitPflichtangaben_AK1()
    {
        var all = AppSettingDefinitions.All;
        all.Should().HaveCount(70);  // v1.99.72: +2 FraesenPfadAktiv, BdeTeilelisteLagerbestellungArbeitstage; v1.99.53: +1 LagerbestellungVerbrauchsbuchungAktiv; v1.99.43: +2 FaHierarchyKommissionierlisteVorfilterZiele, FaHierarchyKommissionierlisteNurFreigegebene; v1.99.35: +5 Blechfertigung (BlechfertigungAktiv, BfEmpfaengergruppeId, BfStatuswerte, BfSchachtelterminArbeitstage, BfFeTerminArbeitstage); v1.99.25: +2 BeschichtungKennzeichenQuelle, BeschichtungTerminQuelle; v1.99.20: +2 EinkaufAktiv, EinkaufOffeneBedarfe; v1.99.14: -1 SageSdataFaAnsichtAktiv (Gate entfaellt); v1.99.11: +1 WareneingangBarcodeAktiv; v1.79.0 F5: zwei Rueckmelde-Schalter entfernt; v1.84.0: +2 Wareneingang aus Sage-Bestellungen; v1.87.0: +1 FaPflichtBuchungsarten; v1.99.4: +1 WareneingangLieferscheinPflicht
        all.Select(d => d.Key).Should().OnlyHaveUniqueItems();
        all.Should().OnlyContain(d => !string.IsNullOrWhiteSpace(d.Anzeigename) && !string.IsNullOrWhiteSpace(d.Description)
            && !string.IsNullOrWhiteSpace(d.Verwendung) && !string.IsNullOrWhiteSpace(d.Standort) && !string.IsNullOrWhiteSpace(d.Category));

        var konstanten = typeof(AppSettingKeys).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string)).Select(f => (string)f.GetRawConstantValue()!).ToList();
        konstanten.Should().NotBeEmpty().And.OnlyContain(k => all.Any(d => d.Key == k), "jede AppSettingKeys-Konstante steht im Katalog");
        new[] { "KommissionierTage", "VorkommissionierTage", "BeschichtungTage", "WarningThresholdPercent", "CriticalThresholdPercent",
                "OseonAmpelGelbTage", "OseonAmpelBlauTage", "TeileverfolgungAktiv", "LeitstandAktiv", "BdeAktiv", "BestellungenAktiv" }
            .Should().OnlyContain(k => all.Any(d => d.Key == k), "Literal-Keys ohne Konstante");
    }

    [Fact]
    public void QrFesteWerte_AlsChoice_AK1()
    {
        AppSettingDefinitions.TryGetWerte(AppSettingKeys.QrErstesFeld, out var erst).Should().BeTrue();
        erst!.Select(w => w.Wert).Should().Equal("Artikelnummer", "Matchcode");
        AppSettingDefinitions.TryGetWerte(AppSettingKeys.QrTrennzeichen, out var trenn).Should().BeTrue();
        trenn!.Select(w => w.Wert).Should().Equal(";", ",");
        AppSettingDefinitions.TryGetWerte(AppSettingKeys.BeschichtungAbholtage, out _).Should().BeFalse("MultiChoice ist kein Dropdown");
    }

    [Fact]
    public void BeschichtungQuellen_AlsChoice_OhneSData_AK1_AK19()
    {
        AppSettingDefinitions.TryGetWerte(AppSettingKeys.BeschichtungKennzeichenQuelle, out var k).Should().BeTrue();
        k!.Select(w => w.Wert).Should().Equal("Automatisch", "Artikelkategorie", "FaStruktur");
        AppSettingDefinitions.TryGetWerte(AppSettingKeys.BeschichtungTerminQuelle, out var t).Should().BeTrue();
        t!.Select(w => w.Wert).Should().Equal("Automatisch", "Berechnet", "View");
        foreach (var key in new[] { AppSettingKeys.BeschichtungKennzeichenQuelle, AppSettingKeys.BeschichtungTerminQuelle })
        {
            AppSettingDefinitions.TryGet(key, out var d).Should().BeTrue();
            (d!.DefaultValue, d.Seed, d.Standort, d.Category).Should().Be(("Automatisch", true, "beide", "Termine und Beschichtung"));
        }
        AppSettingDefinitions.TryGet(AppSettingKeys.BeschichtungKennzeichenQuelle, out var kd);
        kd!.Warnhinweis.Should().NotBeNullOrWhiteSpace("Umschalten setzt „Beschichtung erledigt“ zurueck (ADR 0020)");
        AppSettingDefinitions.TryGet(AppSettingKeys.LackierteilKategorieName, out var lack);
        lack!.AbhaengigVon.Should().Be(AppSettingKeys.BeschichtungKennzeichenQuelle);
    }
}
