using FluentAssertions;
using IdealAkeWms.Models;
using IdealAkeWms.Models.Standort;
using IdealAkeWms.Services.Settings;

namespace IdealAkeWms.Tests.Services;

/// <summary>Spec 2026-11-07-einstellungen-auswahlfelder-und-beschreibung, E4a (AK 12, 15-17): Serverpruefung und Drift-Guards beider Kataloge.</summary>
public class SettingValidatorTests
{
    private static SettingDefinition App(string key) => AppSettingDefinitions.All.Single(d => d.Key == key);
    private static SettingDefinition Svc(string key) => ServiceSettingDefinitions.All.Single(d => d.Key == key);

    [Theory]
    [InlineData("SData", true)]
    [InlineData("sdata", false)]
    [InlineData("SData ", false)]
    [InlineData("Neu", false)]
    public void Quellenschalter_NurExakt_AK12(string wert, bool gueltig)
    {
        var p = SettingValidator.Validate(Svc("Sync:Quelle:Artikel"), wert, exakt: true);
        p.Gueltig.Should().Be(gueltig);
        if (!gueltig) p.Fehler.Should().Contain("Quelle Artikel-Abgleich").And.Contain("„View“, „SData“, „Vergleich“");
    }

    // v1.99.9 (Spec 2026-11-08-wareneingang-teil-2 Freigabe-Nachtrag 14:20): System-Kuerzel, serverseitig gepruefte Form
    [Theory]
    [InlineData("", true, "")]
    [InlineData("DEV", true, "DEV")]
    [InlineData(" AT ", true, "AT")]
    [InlineData("ATI", true, "ATI")]
    [InlineData("A1B2C", true, "A1B2C")]
    [InlineData("ABCDEF", false, null)]
    [InlineData("dev", false, null)]
    [InlineData("12", false, null)]
    [InlineData("1AB", false, null)]
    [InlineData("A-1", false, null)]
    [InlineData("D%", false, null)]
    [InlineData("A B", false, null)]
    public void SystemKuerzel_NurGrossbuchstabenUndZiffern_Max5_BuchstabeVorne(string wert, bool gueltig, string? kanonisch)
    {
        var p = SettingValidator.Validate(Svc(IdealAkeWms.Services.SageSystemKuerzel.Key), wert, exakt: true);
        p.Gueltig.Should().Be(gueltig);
        p.Kanonisch.Should().Be(kanonisch);
        if (!gueltig) p.Fehler.Should().Contain("System-Kürzel für Sage-Kennungen").And.Contain("Großbuchstaben");
        IdealAkeWms.Services.SageSystemKuerzel.Gueltig(wert).Should().Be(gueltig);
    }

    [Fact]
    public void SystemKuerzel_Katalog_DefaultLeer_BeideStandorte_Warnhinweis()
    {
        var d = Svc(IdealAkeWms.Services.SageSystemKuerzel.Key);
        (d.Type, d.DefaultValue, d.Standort).Should().Be((SettingType.String, "", "beide"));
        d.Warnhinweis.Should().Contain("Produktion LEER").And.Contain("nur auf neue Sendungen");
        IdealAkeWms.Services.SageSystemKuerzel.Pruefen("  ").Should().BeNull();
        IdealAkeWms.Services.SageSystemKuerzel.Pruefen(" DEV ").Should().Be("DEV");
        ((Action)(() => IdealAkeWms.Services.SageSystemKuerzel.Pruefen("dev"))).Should().Throw<InvalidOperationException>().WithMessage("*Nichts gesendet*");
    }

    [Fact]
    public void Int_Grenzen_MultiChoice_MasterData_AK12()
    {
        SettingValidator.Validate(App(AppSettingKeys.OseonReportingHorizonDays), "61").Gueltig.Should().BeFalse("Max 60 laut Leser");
        SettingValidator.Validate(App(AppSettingKeys.OseonReportingHorizonDays), " 60 ").Kanonisch.Should().Be("60");
        SettingValidator.Validate(App(AppSettingKeys.HardwareScannerMaxZeichenabstandMs), "").Gueltig.Should().BeTrue("leer erlaubt = 50 ms");
        SettingValidator.Validate(App("KommissionierTage"), "x").Gueltig.Should().BeFalse();

        var abhol = App(AppSettingKeys.BeschichtungAbholtage);
        SettingValidator.Validate(abhol, "donnerstag, Montag").Kanonisch.Should().Be("Montag,Donnerstag", "Katalog-Reihenfolge, Schreibweise");
        SettingValidator.Validate(abhol, "Montag,Feiertag").Gueltig.Should().BeFalse();
        SettingValidator.Validate(abhol, "").Kanonisch.Should().Be("", "leer = keine Abholtage, erlaubt");

        var empf = App(AppSettingKeys.DefaultLagerbestellempfaengerId);
        SettingValidator.Validate(empf, "7", new[] { "7" }).Gueltig.Should().BeTrue();
        SettingValidator.Validate(empf, "99", new[] { "7" }).Gueltig.Should().BeFalse();
        SettingValidator.Validate(empf, "", new[] { "7" }).Gueltig.Should().BeTrue("(leer = aus)");
        SettingValidator.Validate(App(AppSettingKeys.NegativeBuchungLagerplatz), "", new[] { "NAN" }).Gueltig.Should().BeFalse("Lagerplatz ist Pflicht");

        SettingValidator.Validate(App(AppSettingKeys.BdeAktiv), "1").Gueltig.Should().BeFalse();
        SettingValidator.Validate(App(AppSettingKeys.BdeAktiv), "TRUE").Kanonisch.Should().Be("true");
    }

    public static IEnumerable<object[]> Kataloge() =>
        new[] { new object[] { "App" }, new object[] { "Service" } };

    [Theory]
    [MemberData(nameof(Kataloge))]
    public void DriftGuard_Pflichtangaben_Defaults_Werte_Laengen_AK15_AK16(string katalog)
    {
        var all = katalog == "App" ? AppSettingDefinitions.All : ServiceSettingDefinitions.All;
        all.Select(d => d.Key).Should().OnlyHaveUniqueItems();
        foreach (var d in all)
        {
            d.Key.Length.Should().BeLessThanOrEqualTo(100, d.Key);
            d.DefaultValue.Length.Should().BeLessThanOrEqualTo(500, d.Key);
            new[] { d.Anzeigename, d.Description, d.Verwendung, d.Standort, d.Category }.Should().OnlyContain(x => !string.IsNullOrWhiteSpace(x), d.Key);
            switch (d.Type)
            {
                case SettingType.Bool:
                    d.DefaultValue.Should().BeOneOf(new[] { "true", "false" }, d.Key);
                    break;
                case SettingType.Int:
                    if (d.DefaultValue.Length > 0 || !d.LeerErlaubt)
                        SettingValidator.Validate(d, d.DefaultValue).Gueltig.Should().BeTrue($"{d.Key}: Default parsebar und in Min/Max");
                    break;
                case SettingType.Choice:
                case SettingType.MultiChoice:
                    d.Werte.Should().NotBeNullOrEmpty(d.Key);
                    SettingValidator.Validate(d, d.DefaultValue, exakt: true).Gueltig.Should().BeTrue($"{d.Key}: Default liegt in den Werten");
                    break;
                case SettingType.MasterData:
                    d.Stammdaten.Should().NotBe(SettingStammdaten.Keine, d.Key);
                    break;
            }
        }
    }

    [Fact]
    public void ServiceKatalog_JederKeyHatEigeneMetadaten_QA_B5()
    {
        // Ergaenzen() faellt ohne Eintrag auf den Key als Anzeigename und "Dienst" als Verwendung zurueck — das soll nie vorkommen.
        ServiceSettingDefinitions.All.Should().OnlyContain(d => d.Anzeigename != d.Key && d.Verwendung != "Dienst",
            "jeder Service-Key braucht einen Eintrag in ServiceSettingDefinitions.Meta");
    }

    [Fact]
    public void Quellenschalter_SindChoice_GleichSageQuelle_AK16()
    {
        var quellen = ServiceSettingDefinitions.All.Where(d => d.Key.StartsWith("Sync:Quelle:", StringComparison.Ordinal)).ToList();
        quellen.Should().HaveCount(6).And.OnlyContain(d => d.Type == SettingType.Choice);
        // gleich SageQuelle im Dienst (IDEALAKEWMSService/Services/Sdata/SageQuelleSchalter.cs: View, SData, Vergleich)
        quellen.Should().OnlyContain(d => d.Werte!.Select(w => w.Wert).SequenceEqual(new[] { "View", "SData", "Vergleich" }));
    }

    [Fact]
    public void Standorteinstellungen_StehenImPassendenKatalog_TypUndGrenzenPassen_AK17()
    {
        foreach (var f in StandortSettingsCatalog.Fields)
        {
            var katalog = f.Backend == StandortBackend.App ? AppSettingDefinitions.All : ServiceSettingDefinitions.All;
            var d = katalog.SingleOrDefault(x => x.Key == f.Key);
            d.Should().NotBeNull($"{f.Key} ({f.Backend}) fehlt im Katalog");
            var passt = f.Type switch
            {
                StandortFieldType.Bool => d!.Type == SettingType.Bool,
                StandortFieldType.Int => d!.Type == SettingType.Int,
                _ => d!.Type is SettingType.String or SettingType.MasterData,
            };
            passt.Should().BeTrue($"{f.Key}: Typ {f.Type} vs. {d!.Type}");
            if (f.Min != null && d!.Min != null) d.Min.Should().Be(f.Min, f.Key);
            if (f.Max != null && d!.Max != null) d.Max.Should().Be(f.Max, f.Key);
        }
    }
}
