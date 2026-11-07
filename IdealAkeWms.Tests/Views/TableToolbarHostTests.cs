using System.Text.RegularExpressions;
using FluentAssertions;
using IdealAkeWms.Models.ViewModels;
using Xunit;

namespace IdealAkeWms.Tests.Views;

/// <summary>
/// QUELLTEXT-WAECHTER (kein Verhaltenstest) fuer den zentralen Toolbar-Host (v1.65.0, Spec
/// 2026-10-14-spaltenauswahl-zahnrad-sammel). Ersetzt die *ZahnradHostTests: Zahnrad und CSV-Knopf haengen nicht mehr am
/// „DIV direkt vor .table-responsive“ (dreimal still falsch: Stueckliste v1.42, Lagerbestellung v1.30.1, Leitstand
/// hierarchisch), sondern an site.js tableToolbarHost — Marker data-table-toolbar oder Fallback-Host vor dem Wrapper.
/// Wo die Knoepfe sichtbar stehen, ist Manual-UAT (TS-98.1).
/// </summary>
public class TableToolbarHostTests
{
    private static readonly string Root = FindRoot();

    private static string FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "IdealAkeWms", "wwwroot", "js", "table-filter.js")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new FileNotFoundException("Repository-Root nicht gefunden");
    }

    private static string Js(string file) => File.ReadAllText(Path.Combine(Root, "IdealAkeWms", "wwwroot", "js", file));

    /// <summary>View-Quelltext ohne Razor-Kommentare (die erzeugen kein DOM).</summary>
    private static string View(string relPath) =>
        Regex.Replace(File.ReadAllText(Path.Combine(Root, "IdealAkeWms", "Views", relPath)), @"@\*.*?\*@", "", RegexOptions.Singleline);

    private static IEnumerable<string> AllViews() =>
        Directory.EnumerateFiles(Path.Combine(Root, "IdealAkeWms", "Views"), "*.cshtml", SearchOption.AllDirectories)
            .Select(p => Path.GetRelativePath(Path.Combine(Root, "IdealAkeWms", "Views"), p));

    [Theory]
    [InlineData("column-preferences.js")]
    [InlineData("csv-export.js")]
    public void Skripte_PlatzierenNurUeberTableToolbarHost(string file)
    {
        var js = Js(file);
        js.Should().NotContain("previousElementSibling", "die Nachbar-Heuristik ist abgeloest (fallstricke §19)");
        js.Should().Contain("tableToolbarHost(");
    }

    [Fact]
    public void SiteJs_StelltHostBereit_FallbackNurUeberEigenenWrapper()
    {
        var js = Js("site.js");
        js.Should().Contain("window.tableToolbarHost = function (table)");
        js.Should().Contain("prev.hasAttribute('data-table-toolbar')", "Stufe 1: Host direkt vor dem eigenen Wrapper");
        js.Should().Contain("[data-table-toolbar]:not([data-table-toolbar=\"auto\"])",
            "Stufe 2 nur fuer explizite Marker — ein Fallback-Host einer anderen Tabelle (BdeMasterData: drei Listen) darf nicht gefunden werden");
        js.Should().Contain("host.setAttribute('data-table-toolbar', 'auto')");
    }

    [Fact]
    public void JedeView_HoechstensEinMarker_NieVersteckt()
    {
        foreach (var rel in AllViews())
        {
            var tags = Regex.Matches(View(rel), @"<[a-zA-Z][^>]*\bdata-table-toolbar\b[^>]*>").Select(m => m.Value).ToList();
            tags.Count.Should().BeLessThanOrEqualTo(1, $"{rel}: ein Toolbar-Host je Seite (Stufe 2 sucht dokumentweit)");
            foreach (var tag in tags)
                tag.Should().NotMatchRegex(@"\bhidden\b|\bd-none\b|display:\s*none", $"{rel}: der Host muss immer sichtbar sein");
        }
    }

    [Theory]
    [InlineData("Picking/Bom.cshtml")]
    [InlineData("WarehousePicking/Details.cshtml")]
    [InlineData("Beschichtungsauftraege/Index.cshtml")]
    [InlineData("Beschichtungsauftraege/Details.cshtml")]
    [InlineData("FaHierarchyBeschichtung/Summiert.cshtml")]
    [InlineData("PickingLeitstand/Index.cshtml")]
    [InlineData("OperationConfigs/Index.cshtml")]
    [InlineData("FaHierarchy/Index.cshtml")]
    [InlineData("Tracking/OseonIndex.cshtml")]
    [InlineData("ProductionOrders/Index.cshtml")]
    [InlineData("FaCompletion/Index.cshtml")]
    [InlineData("FaWorklist/Index.cshtml")]
    [InlineData("Picking/Index.cshtml")]
    public void MarkerGesetzt(string rel)
        => View(rel).Should().Contain("data-table-toolbar");

    [Theory]
    [InlineData("PickingLeitstand/Index.cshtml")]
    [InlineData("ProductionOrders/Index.cshtml")]
    [InlineData("FaCompletion/Index.cshtml")]
    [InlineData("FaWorklist/Index.cshtml")]
    public void Marker_StehtUeberDerZaehlzeile(string rel)
    {
        var html = View(rel);
        var marker = html.IndexOf("data-table-toolbar", StringComparison.Ordinal);
        var zaehlzeile = html.IndexOf("@Model.HierarchicalRowCount", StringComparison.Ordinal);
        zaehlzeile.Should().BePositive();
        marker.Should().BePositive().And.BeLessThan(zaehlzeile, "das Zahnrad gehoert nicht in die Zaehlzeile „… Aufträge · … Sub-FAs“");
    }

    [Fact]
    public void JederViewKeyAusViewConfig_IstRegistriert()
    {
        // Haeufigste 400-Ursache beim Speichern (fallstricke §3): viewKey im #view-config, aber nicht in GetByViewKey.
        var keys = AllViews()
            .Select(View)
            .Where(html => html.Contains("id=\"view-config\""))
            .SelectMany(html => Regex.Matches(html, "\"viewKey\":\\s*\"([^\"]+)\"").Select(m => m.Groups[1].Value))
            .Distinct()
            .ToList();
        keys.Should().HaveCountGreaterThanOrEqualTo(18);
        keys.Where(k => ColumnDefinitions.GetByViewKey(k) == null).Should().BeEmpty();
    }

    [Fact]
    public void Vorschlagsliste_AnzeigenameWieSeitentitel()
    {
        // D: DisplayName erscheint in Benutzer → Ansichts-Einstellungen; ViewKey bleibt (gespeicherte Einstellungen).
        ColumnDefinitions.GetByViewKey("FaHierarchyBeschichtung")!.DisplayName.Should().Be("Vorschlagsliste Beschichtung (IDEAL)");
        ColumnDefinitions.GetByViewKey("FaHierarchyBeschichtungSummiert")!.DisplayName.Should().Be("Vorschlagsliste Beschichtung — Summiert (IDEAL)");
    }
}

/// <summary>
/// QUELLTEXT-WAECHTER Paket C + Z1/Z2 (v1.65.0): Laden, Speichern und Zuruecksetzen der Spaltenauswahl scheitern nicht mehr
/// still. Sichtbarkeit und Wortlaut im Browser sind Manual-UAT (TS-98.8–98.13).
/// </summary>
public class ColumnPreferencesFehlerhinweisTests
{
    private static readonly string Js = File.ReadAllText(Path.Combine(FindRoot(), "IdealAkeWms", "wwwroot", "js", "column-preferences.js"));

    private static string FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "IdealAkeWms", "wwwroot", "js", "table-filter.js")))
            dir = dir.Parent;
        return dir!.FullName;
    }

    private static string Function(string name)
    {
        var start = Js.IndexOf("function " + name + "(", StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0, $"Funktion {name} fehlt");
        var next = Js.IndexOf("\n    function ", start + 10, StringComparison.Ordinal);
        return Js[start..(next < 0 ? Js.Length : next)];
    }

    [Fact]
    public void Hinweis_IstAlertImToolbarHost()
    {
        var f = Function("showPrefsMessage");
        f.Should().Contain("tableToolbarHost(_table)").And.Contain("role', 'alert'").And.Contain("textContent")
            .And.Contain(":scope > .column-prefs-msg", "hoechstens ein Hinweis zugleich");
    }

    [Fact]
    public void Speichern_PrueftResponseOk()
        => Function("saveSettings").Should().Contain("resp.ok").And.Contain("showPrefsMessage(failureText(SAVE_FAILED[0], resp")
            .And.Contain("showPrefsMessage(failureText(SAVE_FAILED[0], null");

    [Fact]
    public void Zuruecksetzen_LaedtNurBeiErfolgNeu()
    {
        var f = Function("resetToDefault");
        Regex.Matches(f, @"location\.reload\(\)").Count.Should().Be(1, "der Fehlerweg darf nicht neu laden");
        f.Should().Contain("if (resp.ok) { window.location.reload(); return; }");
    }

    [Fact]
    public void Laden_MeldetFehler_Z1()
        => Function("loadSettings").Should().Contain("_loadFailedResp = resp").And.Contain("LOAD_FAILED[0]");

    [Fact]
    public void AusstehendesSpeichern_BeiPagehideMitKeepalive_Z2()
    {
        Js.Should().Contain("window.addEventListener('pagehide'");
        Js.Should().Contain("saveSettings(true)");
        Js.Should().Contain("keepalive: !!keepalive");
    }

    [Fact]
    public void Panel_ZeigtDenselbenHinweis()
        => Function("renderOffcanvasContent").Should().Contain("if (_prefsMsg)");
}
