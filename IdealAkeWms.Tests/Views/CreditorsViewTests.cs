using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Models.ViewModels;
using Xunit;

namespace IdealAkeWms.Tests.Views;

/// <summary>
/// QUELLTEXT-WAECHTER der Kreditorenliste (E8a; Muster <see cref="OperationConfigsViewTests"/>): th-Schluessel, #column-config,
/// ColumnDefinitions und Server-Filter-Map stimmen ueberein (fallstricke §3), viewKey registriert, column-preferences.js vor table-filter.js,
/// Menuepunkt im Stammdaten-Menue. Aussehen und Speichern der Praeferenz sind Manual-UAT (TS-106.10).
/// </summary>
public class CreditorsViewTests
{
    private static string Read(params string[] parts)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "IdealAkeWms", "wwwroot", "js", "table-filter.js")))
            dir = dir.Parent;
        return File.ReadAllText(Path.Combine(new[] { dir!.FullName, "IdealAkeWms" }.Concat(parts).ToArray()));
    }

    [Fact]
    public void VierQuellen_Gleich()
    {
        var html = Read("Views", "Creditors", "Index.cshtml");
        var definition = ColumnDefinitions.Creditors.Columns.Select(c => c.Key).ToList();

        KommissionierSpaltenKonsistenzTests.ExtractThDataColKeys(html).Should().Equal(definition);
        KommissionierSpaltenKonsistenzTests.ExtractColumnConfigKeys(html).Should().Equal(definition);
        CreditorsController.ColumnMap.Keys.Should().BeEquivalentTo(definition.Where(k => k != "actions"),
            "jede Spalte ausser der Aktionsspalte ist server-filterbar (ADR 0005)");
        ColumnDefinitions.GetByViewKey("Creditors").Should().BeSameAs(ColumnDefinitions.Creditors);
    }

    [Fact]
    public void ColumnPreferences_VorTableFilter_UndMenuepunkt()
    {
        var html = Read("Views", "Creditors", "Index.cshtml");
        var prefs = html.IndexOf("column-preferences.js", StringComparison.Ordinal);
        prefs.Should().BePositive();
        prefs.Should().BeLessThan(html.IndexOf("table-filter.js", StringComparison.Ordinal));
        html.Should().Contain("\"viewKey\": \"Creditors\"").And.Contain("data-view-key=\"Creditors\"");
        html.Should().NotContain("Html.Raw", "Kreditor-Freitext nur ueber Razor-Encoding (fallstricke)");

        Read("Views", "Shared", "_Layout.cshtml").Should().Contain("asp-controller=\"Creditors\" asp-action=\"Index\">Kreditoren</a>");
    }
}
