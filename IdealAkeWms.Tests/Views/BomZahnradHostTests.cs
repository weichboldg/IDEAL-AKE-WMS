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
    internal const string GearHost = "<div class=\"d-flex justify-content-end mb-1\"></div>";

    /// <summary>Quelltext vor dem ersten <c>&lt;div class="table-responsive"&gt;</c>, ohne Razor-Kommentare (erzeugen kein DOM).</summary>
    internal static string TextBeforeTableWrapper(params string[] viewPath)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "IdealAkeWms", "wwwroot", "js", "table-filter.js")))
            dir = dir.Parent;
        var root = dir?.FullName ?? throw new FileNotFoundException("Repository-Root nicht gefunden");
        var cshtml = File.ReadAllText(Path.Combine(new[] { root, "IdealAkeWms", "Views" }.Concat(viewPath).ToArray()));

        var idx = cshtml.IndexOf("<div class=\"table-responsive\">", StringComparison.Ordinal);
        idx.Should().BeGreaterThanOrEqualTo(0, "die View muss den Haupt-Tabellen-Wrapper enthalten");
        return Regex.Replace(cshtml[..idx], @"@\*.*?\*@", "", RegexOptions.Singleline).TrimEnd();
    }

    [Fact]
    public void ElementDirektVorTableResponsive_IstDerZahnradToolbarHost()
    {
        TextBeforeTableWrapper("Picking", "Bom.cshtml").Should().EndWith(GearHost,
            "das Zahnrad (column-preferences.js insertGearButton) haengt am direkten Vorgaenger von .table-responsive");
    }
}

/// <summary>
/// Gleiche Regel fuer die Lagerbestellung-Detailseite (v1.47.5): vorher stand #incomplete-alert (d-none) direkt
/// vor der Tabelle — Zahnrad unsichtbar und beim Einblenden des Pflicht-Hinweises per textContent geloescht.
/// Sichtbarkeit ist Manual-UAT (TS-18.10).
/// </summary>
public class WarehousePickingDetailsZahnradHostTests
{
    [Fact]
    public void ElementDirektVorTableResponsive_IstDerZahnradToolbarHost()
    {
        BomZahnradHostTests.TextBeforeTableWrapper("WarehousePicking", "Details.cshtml").Should().EndWith(BomZahnradHostTests.GearHost,
            "das Zahnrad haengt am direkten Vorgaenger von .table-responsive; #incomplete-alert gehoert darueber");
    }
}
