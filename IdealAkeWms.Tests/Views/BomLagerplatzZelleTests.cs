using System.Text.RegularExpressions;
using FluentAssertions;
using IdealAkeWms.Models.ViewModels;
using Xunit;

namespace IdealAkeWms.Tests.Views;

/// <summary>
/// QUELLTEXT-WAECHTER der Stuecklisten-Lagerplatz-Zelle (Spec 2026-10-16-stueckliste-matchcode-lagerplatz-limit, AK 10/14/15,
/// v1.62.0): Spalte filterbar, volle Liste fuer Filter (d-none) und CSV (data-csv-value), Popover-Init VOR dem readOnly-Return
/// (sonst fehlt das Popover in Abarbeitungsliste/FA-Vervollstaendigung), container body. Bedienung/Optik = Manual-UAT (TS-95).
/// </summary>
public class BomLagerplatzZelleTests
{
    private static string Html()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "IdealAkeWms", "wwwroot", "js", "table-filter.js")))
            dir = dir.Parent;
        return File.ReadAllText(Path.Combine(dir!.FullName, "IdealAkeWms", "Views", "Picking", "Bom.cshtml"));
    }

    [Fact]
    public void Zelle_FilterbarMitVolltextUndCsvWert()
    {
        var html = Html();
        html.Should().Contain("<th data-filterable data-col-key=\"storage-location\">Lagerplatz</th>");
        html.Should().Contain("data-csv-value=\"@(item.StockLocations.Any()");
        html.Should().Contain("<ul class=\"d-none bom-lagerplatz-alle\">");
        html.Should().Contain("Take(BomItemViewModel.MaxSichtbareLagerplaetze)");
        BomItemViewModel.MaxSichtbareLagerplaetze.Should().Be(3);
    }

    [Fact]
    public void PopoverInit_VorReadOnlyReturn_ContainerBody_HoverFocus()
    {
        var html = Html();
        var init = html.IndexOf("new bootstrap.Popover(badge", StringComparison.Ordinal);
        var readOnlyReturn = html.IndexOf("if (readOnly) return;", StringComparison.Ordinal);
        init.Should().BePositive();
        init.Should().BeLessThan(readOnlyReturn, "Read-only-Stuecklisten brauchen das Popover auch");
        Regex.IsMatch(html, @"trigger: 'hover focus', container: 'body'").Should().BeTrue();
        html.Should().Contain("<a tabindex=\"0\" role=\"button\" class=\"badge text-bg-secondary ms-1 bom-lagerplatz-mehr\"");
    }
}
