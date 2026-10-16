using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Models.ViewModels;
using Xunit;

namespace IdealAkeWms.Tests.Views;

/// <summary>
/// QUELLTEXT-WAECHTER des Reiters „Arbeitsgänge" (Spec 2026-10-16-arbeitsgang-stammdaten-abgleich-aus-sage-view, AK 26; Muster
/// <see cref="BeschichtungsauftraegeViewTests"/>): Spaltenschluessel aus th, #column-config, ColumnDefinitions und Server-Filter-Map
/// stimmen ueberein (fallstricke §3), Zahnrad-Host direkt vor .table-responsive (§19), column-preferences.js vor table-filter.js.
/// Speichern der Praeferenz und Aussehen sind Manual-UAT (TS-94.16).
/// </summary>
public class OperationConfigsViewTests
{
    private static string Html()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "IdealAkeWms", "wwwroot", "js", "table-filter.js")))
            dir = dir.Parent;
        return File.ReadAllText(Path.Combine(dir!.FullName, "IdealAkeWms", "Views", "OperationConfigs", "Index.cshtml"));
    }

    [Fact]
    public void VierQuellen_Gleich()
    {
        var html = Html();
        var definition = ColumnDefinitions.OperationConfigs.Columns.Select(c => c.Key).ToList();

        KommissionierSpaltenKonsistenzTests.ExtractThDataColKeys(html).Should().Equal(definition);
        KommissionierSpaltenKonsistenzTests.ExtractColumnConfigKeys(html).Should().Equal(definition);
        OperationConfigsController.ColumnMap.Keys.Should().BeEquivalentTo(definition.Where(k => k != "actions"),
            "jede Spalte ausser der Aktionsspalte ist server-filterbar (ADR 0005)");
        ColumnDefinitions.GetByViewKey("OperationConfigs").Should().BeSameAs(ColumnDefinitions.OperationConfigs);
        ColumnDefinitions.OperationConfigs.Columns.Where(c => c.Locked).Select(c => c.Key).Should().Equal("name", "actions");
    }

    [Fact]
    public void ZahnradHost_DirektVorTableResponsive()
        => BomZahnradHostTests.TextBeforeTableWrapper("OperationConfigs", "Index.cshtml").Should().EndWith(BomZahnradHostTests.GearHost);

    [Fact]
    public void ColumnPreferences_VorTableFilter()
    {
        var html = Html();
        var prefs = html.IndexOf("column-preferences.js", StringComparison.Ordinal);
        prefs.Should().BePositive();
        prefs.Should().BeLessThan(html.IndexOf("table-filter.js", StringComparison.Ordinal));
        html.Should().Contain("\"viewKey\": \"OperationConfigs\"").And.Contain("data-view-key=\"OperationConfigs\"");
    }
}
