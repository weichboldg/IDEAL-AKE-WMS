using System.Reflection;
using FluentAssertions;
using IdealAkeWms.Models;

namespace IdealAkeWms.Tests.Models;

/// <summary>Spec 2026-11-07-einstellungen-auswahlfelder-und-beschreibung (E1, ADR 0020): Katalog der AppSettings.</summary>
public class AppSettingDefinitionsTests
{
    /// <summary>Die bis v1.76.0 in Program.cs fest codierten Seed-Eintraege (acht Bloecke, 38 Keys) — AK 2: Menge und Werte bleiben gleich.</summary>
    private static readonly (string Key, string Value)[] FruehererSeed =
    {
        ("TeileverfolgungAktiv", "false"), ("OseonRueckmeldungAktiv", "false"), ("SageRueckmeldungAktiv", "false"),
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
    };

    [Fact]
    public void Seed_GleichDerFrueherenListe_AK2()
    {
        FruehererSeed.Should().HaveCount(38);
        AppSettingDefinitions.SeedEintraege.Select(e => (e.Key, e.Value)).Should().BeEquivalentTo(FruehererSeed);
    }

    [Fact]
    public void Katalog_EnthaeltJedenKeyGenauEinmal_MitPflichtangaben_AK1()
    {
        var all = AppSettingDefinitions.All;
        all.Should().HaveCount(54);
        all.Select(d => d.Key).Should().OnlyHaveUniqueItems();
        all.Should().OnlyContain(d => !string.IsNullOrWhiteSpace(d.Anzeigename) && !string.IsNullOrWhiteSpace(d.Description)
            && !string.IsNullOrWhiteSpace(d.Verwendung) && !string.IsNullOrWhiteSpace(d.Standort) && !string.IsNullOrWhiteSpace(d.Category));

        var konstanten = typeof(AppSettingKeys).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string)).Select(f => (string)f.GetRawConstantValue()!).ToList();
        konstanten.Should().NotBeEmpty().And.OnlyContain(k => all.Any(d => d.Key == k), "jede AppSettingKeys-Konstante steht im Katalog");
        new[] { "KommissionierTage", "VorkommissionierTage", "BeschichtungTage", "WarningThresholdPercent", "CriticalThresholdPercent",
                "OseonAmpelGelbTage", "OseonAmpelBlauTage", "TeileverfolgungAktiv", "LeitstandAktiv", "BdeAktiv", "BestellungenAktiv",
                "OseonRueckmeldungAktiv", "SageRueckmeldungAktiv" }
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
}
