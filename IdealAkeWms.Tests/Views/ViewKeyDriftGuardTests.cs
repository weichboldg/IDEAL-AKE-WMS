using System.Text.RegularExpressions;
using FluentAssertions;
using IdealAkeWms.Models.ViewModels;
using Xunit;

namespace IdealAkeWms.Tests.Views;

/// <summary>
/// v1.95.0 (Abnahme 2026-11-08, Befund B5.1): DRIFT-WAECHTER fuer die Spaltenauswahl. Jede View mit <c>#view-config</c> muss ihren viewKey in
/// <see cref="ColumnDefinitions.GetByViewKey"/> registriert haben (sonst antwortet /api/user-view-preferences mit 400 und das Zahnrad fehlt),
/// Offene Bestellungen hatte den Key ohne Katalogeintrag und ohne die beiden Bloecke.
/// </summary>
public class ViewKeyDriftGuardTests
{
    private static DirectoryInfo ViewsDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "IdealAkeWms", "Views"))) dir = dir.Parent;
        return new DirectoryInfo(Path.Combine(dir!.FullName, "IdealAkeWms", "Views"));
    }

    public static IEnumerable<object[]> ViewsMitViewConfig() =>
        ViewsDir().GetFiles("*.cshtml", SearchOption.AllDirectories)
            .Where(f => File.ReadAllText(f.FullName).Contains("id=\"view-config\""))
            .Select(f => new object[] { Path.GetRelativePath(ViewsDir().FullName, f.FullName) });

    [Theory]
    [MemberData(nameof(ViewsMitViewConfig))]
    public void ViewKey_Registriert(string view)
    {
        var html = File.ReadAllText(Path.Combine(ViewsDir().FullName, view));
        var key = Regex.Match(html, "id=\"view-config\">\\s*\\{\\s*\"viewKey\":\\s*\"([A-Za-z]+)\"").Groups[1].Value;
        key.Should().NotBeEmpty($"{view}: viewKey in #view-config");
        // Nur die Registrierung allgemein: einige Listen haben bedingte (Razor) bzw. zusaetzliche Spalten im #column-config – deren Abgleich
        // pruefen die view-eigenen Waechter (z. B. CreditorsViewTests, KommissionierSpaltenKonsistenzTests).
        ColumnDefinitions.GetByViewKey(key).Should().NotBeNull($"{view}: viewKey „{key}“ muss in ColumnDefinitions.GetByViewKey stehen");
    }

    [Fact]
    public void OffeneBestellungen_DreiQuellenGleich_UndToolbarHost()
    {
        var html = File.ReadAllText(Path.Combine(ViewsDir().FullName, "PurchaseOrders", "Index.cshtml"));
        var katalog = ColumnDefinitions.PurchaseOrders.Columns.Select(c => c.Key).ToList();
        KommissionierSpaltenKonsistenzTests.ExtractThDataColKeys(html).Should().Equal(katalog);
        KommissionierSpaltenKonsistenzTests.ExtractColumnConfigKeys(html).Should().Equal(katalog);
        html.Should().Contain("data-table-toolbar");
        ViewsMitViewConfig().Select(o => (string)o[0]).Should().Contain(Path.Combine("PurchaseOrders", "Index.cshtml"));
    }

    /// <summary>v1.99.52 (Spec 2026-11-23 F2/F2b): Positionstabelle Wareneingang – drei Quellen gleich, Toolbar-Host, kein Filter/CSV, genau die
    /// acht Pflichtspalten gesperrt; Streifen/Hover/Fokus-Klasse; die Fokus-Regeln setzen keine eigene Hintergrundfarbe (table-danger bleibt).</summary>
    [Fact]
    public void WareneingangPositionen_DreiQuellenGleich_Gesperrt_FokusZeile()
    {
        var html = File.ReadAllText(Path.Combine(ViewsDir().FullName, "PurchaseOrders", "Receive.cshtml"));
        var cfg = ColumnDefinitions.PurchaseOrderReceive;
        var katalog = cfg.Columns.Select(c => c.Key).ToList();
        KommissionierSpaltenKonsistenzTests.ExtractThDataColKeys(html).Should().Equal(katalog);
        KommissionierSpaltenKonsistenzTests.ExtractColumnConfigKeys(html).Should().Equal(katalog);
        ColumnDefinitions.GetByViewKey("PurchaseOrderReceive").Should().BeSameAs(cfg);
        cfg.Columns.Where(c => c.Locked).Select(c => c.Key).Should()
            .Equal("pos", "artikel", "rest", "einheit", "lagerplatz", "menge", "ueberlieferung", "status");
        (cfg.SupportsReorder, cfg.SupportsSortDefault).Should().Be((false, false));
        html.Should().Contain("data-table-toolbar").And.Contain("column-preferences.js").And.NotContain("filterable-table")
            .And.Contain("class=\"table table-sm table-striped table-hover align-middle we-positionen\" data-view-key=\"PurchaseOrderReceive\"");

        var css = File.ReadAllText(Path.Combine(ViewsDir().Parent!.FullName, "wwwroot", "css", "site.css"));
        var regeln = Regex.Matches(css, @"\.we-positionen[^{]*\{([^}]*)\}").Select(m => m.Groups[1].Value).ToList();
        regeln.Should().HaveCount(2);
        regeln.Should().OnlyContain(r => !r.Contains("background"));
        regeln[0].Should().Contain("--bs-table-bg-state: var(--bs-table-active-bg)");
        regeln[1].Should().Contain("inset 4px 0 0").And.Contain("var(--bs-table-bg-state, var(--bs-table-bg-type");
    }
}
