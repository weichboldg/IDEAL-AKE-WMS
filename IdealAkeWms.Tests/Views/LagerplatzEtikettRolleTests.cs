using FluentAssertions;
using IdealAkeWms.Models;
using IdealAkeWms.Models.Standort;
using Xunit;

namespace IdealAkeWms.Tests.Views;

/// <summary>
/// Lagerplatz-Etikett 93 x 55 mm (v1.72.0, Spec 2026-11-06-lagerplatz-etikett-rolle-und-vormontage-felder): QUELLTEXT-WAECHTER fuer
/// Katalog und Druckansicht. Das Druckbild selbst (Seitenzahl, Barcode-Lesbarkeit) ist Manual-UAT.
/// </summary>
public class LagerplatzEtikettRolleTests
{
    private static string View()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "IdealAkeWms", "Program.cs")))
            dir = dir.Parent;
        return File.ReadAllText(Path.Combine(dir!.FullName, "IdealAkeWms", "Views", "StorageLocations", "PrintLabels.cshtml"));
    }

    [Fact]
    public void Katalog_BoolImAppBackend_InGruppeDruck_MitHinweis()
    {
        var f = StandortSettingsCatalog.Fields.Single(x => x.Key == AppSettingKeys.LagerplatzEtikettRolle93x55);
        (f.Type, f.Backend, f.Group).Should().Be((StandortFieldType.Bool, StandortBackend.App, StandortSettingsCatalog.GroupDruck));
        f.Hint.Should().Contain("93 × 55").And.Contain("Keine");
        StandortSettingsCatalog.GroupOrder.Should().Contain(StandortSettingsCatalog.GroupDruck);
    }

    [Fact]
    public void Druckansicht_A4Unveraendert_RolleNurMitSchalter_EinEtikettJeSeite()
    {
        var v = View();
        // Default aus: nur exakt "true" aktiviert, A4-Block bleibt wie bisher.
        v.Should().Contain("AppSettingKeys.LagerplatzEtikettRolle93x55), \"true\", StringComparison.OrdinalIgnoreCase)");
        v.Should().Contain("size: A4 portrait;").And.Contain("height: 85mm;").And.Contain("border: 2px solid #000;");
        v.Should().Contain("!rolle && labelIndex % 3 == 0", "Modulo-3-Umbruch nur im A4-Format");
        // Rolle: eigenes Seitenformat, Umbruch vor jedem weiteren Etikett, ohne Rahmen, kein :last-child-Trick.
        v.Should().Contain("@if (rolle)").And.Contain("size: 93mm 55mm;").And.Contain("body.labels-rolle .label + .label")
            .And.Contain("break-before: page;").And.Contain("border: none;");
        v.Should().NotContain(":last-child");
        v.Should().Contain("Papierformat 93 × 55 mm, Ränder „Keine“", "Treiber-Hinweis in der Bildschirmleiste");
    }
}
