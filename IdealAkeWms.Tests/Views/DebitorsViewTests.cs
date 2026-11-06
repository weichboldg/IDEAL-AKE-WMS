using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Models.ViewModels;
using Xunit;

namespace IdealAkeWms.Tests.Views;

/// <summary>QUELLTEXT-WAECHTER der Debitorenliste (E8b; Muster <see cref="CreditorsViewTests"/>): th-Schluessel, #column-config,
/// ColumnDefinitions und Server-Filter-Map stimmen ueberein (fallstricke §3), viewKey registriert, Menuepunkt. Aussehen ist Manual-UAT (TS-106.11).</summary>
public class DebitorsViewTests
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
        var html = Read("Views", "Debitors", "Index.cshtml");
        var definition = ColumnDefinitions.Debitors.Columns.Select(c => c.Key).ToList();

        KommissionierSpaltenKonsistenzTests.ExtractThDataColKeys(html).Should().Equal(definition);
        KommissionierSpaltenKonsistenzTests.ExtractColumnConfigKeys(html).Should().Equal(definition);
        DebitorsController.ColumnMap.Keys.Should().BeEquivalentTo(definition.Where(k => k != "actions"));
        ColumnDefinitions.GetByViewKey("Debitors").Should().BeSameAs(ColumnDefinitions.Debitors);
    }

    [Fact]
    public void ColumnPreferences_VorTableFilter_UndMenuepunkt()
    {
        var html = Read("Views", "Debitors", "Index.cshtml");
        var prefs = html.IndexOf("column-preferences.js", StringComparison.Ordinal);
        prefs.Should().BePositive();
        prefs.Should().BeLessThan(html.IndexOf("table-filter.js", StringComparison.Ordinal));
        html.Should().Contain("\"viewKey\": \"Debitors\"").And.Contain("data-view-key=\"Debitors\"");
        html.Should().NotContain("Html.Raw");

        Read("Views", "Shared", "_Layout.cshtml").Should().Contain("asp-controller=\"Debitors\" asp-action=\"Index\">Debitoren</a>");
    }
}
