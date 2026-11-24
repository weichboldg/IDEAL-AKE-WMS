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
        var cfg = ColumnDefinitions.GetByViewKey(key);
        cfg.Should().NotBeNull($"{view}: viewKey „{key}“ muss in ColumnDefinitions.GetByViewKey stehen");
        // v1.99.55: wirksam ist nur #view-config (column-preferences.js) – der Katalog muss gleich sein, sonst luegt C#.
        var reorder = Regex.Match(html, @"id=""view-config"">[^<]*""supportsReorder"":\s*(true|false)");
        reorder.Success.Should().BeTrue($"{view}: supportsReorder in #view-config");
        bool.Parse(reorder.Groups[1].Value).Should().Be(cfg!.SupportsReorder, $"{view}: #view-config.supportsReorder = ColumnDefinitions.{key}.SupportsReorder");
    }

    /// <summary>v1.99.58 (TS-183): Spalte „Menge IST“ der Auftragsseite an allen drei Stellen, gleich bedingt (nur mit Auftrag),
    /// Eingabefeld .ist-menge (liest das Absenden) nur noch in dieser Spalte.</summary>
    [Fact]
    public void Stueckliste_SpalteMengeIst_DreiQuellen_NurMitAuftrag()
    {
        var html = File.ReadAllText(Path.Combine(ViewsDir().FullName, "Picking", "Bom.cshtml"));
        ColumnDefinitions.Bom.Columns.Select(c => c.Key).Should().ContainInOrder("quantity", "ist-menge", "procurement");
        Regex.IsMatch(html, @"@if \(Model\.Auftrag != null\)\s*\{\s*<th class=""text-end"" data-col-key=""ist-menge"">Menge IST</th>").Should().BeTrue();
        Regex.IsMatch(html, @"@if \(Model\.Auftrag != null\) \{ <text>\{ ""key"": ""ist-menge"", ""label"": ""Menge IST""").Should().BeTrue();
        Regex.Matches(html, @"class=""form-control form-control-sm text-end ist-menge""").Count.Should().Be(1);
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
    /// acht Pflichtspalten gesperrt; Zeilenfarben seit v1.99.62 global (TabellenZeilenfarbeTests).</summary>
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
        (cfg.SupportsReorder, cfg.SupportsSortDefault).Should().Be((true, false));
        html.Should().Contain("data-table-toolbar").And.Contain("column-preferences.js").And.NotContain("filterable-table")
            .And.Contain("class=\"table table-sm table-striped table-hover align-middle\" data-view-key=\"PurchaseOrderReceive\"");
        // v1.99.62: Streifen/Hover/Fokus-Zeile/Links kommen aus den globalen Tabellenregeln (TabellenZeilenfarbeTests); keine Ausnahme hier.
        html.Should().NotContain("table-plain");
    }
}
