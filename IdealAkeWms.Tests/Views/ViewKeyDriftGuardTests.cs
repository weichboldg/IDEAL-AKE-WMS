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
}
