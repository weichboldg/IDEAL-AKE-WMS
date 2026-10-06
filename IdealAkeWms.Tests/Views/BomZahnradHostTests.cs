using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace IdealAkeWms.Tests.Views;

/// <summary>
/// QUELLTEXT-WAECHTER (kein Verhaltenstest): column-preferences.js insertGearButton haengt das Zahnrad an
/// das Element DIREKT vor .table-responsive (previousElementSibling). In Bom.cshtml muss das der leere
/// Toolbar-Host sein — seit v1.42 standen dort #bomDefaultFilterBadges/#bomKzEmptyState (hidden) dazwischen,
/// das Zahnrad landete im versteckten Leerzustand (v1.47.3). Sichtbarkeit selbst ist Manual-UAT (TS-79.20).
/// </summary>
public class BomZahnradHostTests
{
    private static readonly string BomCshtml;

    static BomZahnradHostTests()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "IdealAkeWms", "wwwroot", "js", "table-filter.js")))
            dir = dir.Parent;
        var root = dir?.FullName ?? throw new FileNotFoundException("Repository-Root nicht gefunden");
        BomCshtml = File.ReadAllText(Path.Combine(root, "IdealAkeWms", "Views", "Picking", "Bom.cshtml"));
    }

    [Fact]
    public void ElementDirektVorTableResponsive_IstDerZahnradToolbarHost()
    {
        var idx = BomCshtml.IndexOf("<div class=\"table-responsive\">", StringComparison.Ordinal);
        idx.Should().BeGreaterThanOrEqualTo(0, "Bom.cshtml muss den Haupt-Tabellen-Wrapper enthalten");

        // Razor-Kommentare erzeugen kein DOM — vor dem Vergleich entfernen.
        var davor = Regex.Replace(BomCshtml[..idx], @"@\*.*?\*@", "", RegexOptions.Singleline).TrimEnd();

        davor.Should().EndWith("<div class=\"d-flex justify-content-end mb-1\"></div>",
            "das Zahnrad (column-preferences.js insertGearButton) haengt am direkten Vorgaenger von .table-responsive");
    }
}
