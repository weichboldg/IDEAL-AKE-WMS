using FluentAssertions;
using Xunit;

namespace IdealAkeWms.Tests.Helpers;

/// <summary>
/// QUELLTEXT-WAECHTER (kein Verhaltenstest) fuer die Tabellen-Skripte.
///
/// Was dieser Test beweist: dass die DOM-Iterationen in table-filter.js, column-preferences.js und
/// fa-liste-gruppierung.js auf direkte Kinder skopiert sind (':scope >').
/// Was er NICHT beweist: dass Sortierung, Filter oder Spaltenkonfiguration fachlich richtig
/// arbeiten. Er liest Quelltext, er fuehrt kein JavaScript aus — es gibt in diesem Projekt keine
/// JS-Testinfrastruktur. Die Wirkung wird per Manual-UAT nachgewiesen
/// (docs/TESTSZENARIEN.md, Szenario "Kombigeraete-Gruppe sortieren").
/// Der Nutzen liegt allein im Rueckfallschutz: Wer einen der Selektoren wieder auf eine
/// Descendant-Abfrage zurueckdreht, macht diesen Test rot.
///
/// Hintergrund (Befund 2026-09-10, Schwere hoch): sortTable iterierte
/// _table.querySelectorAll('tbody') und sammelte Zeilen per tbody.querySelectorAll('tr') —
/// beides Descendant-Abfragen. In der FA-Liste steckt in einer Kombigeraete-Gruppe eine
/// VERSCHACHTELTE Varianten-Tabelle; ihre Zeilen tragen sieben blanke &lt;td&gt; ohne colspan,
/// passieren also den td[colspan]-Filter. tbody.appendChild(row) verschob sie anschliessend in
/// das AEUSSERE Gruppen-&lt;tbody&gt;: Varianten-Tabelle leer, Sieben-Zellen-Zeilen in einer
/// 25-spaltigen Tabelle.
///
/// Der Denkfehler, der das lange verdeckt hat — und der Grund fuer diesen Waechter:
/// "appendChild haelt jede Zeile in ihrem eigenen tbody." Falsch. appendChild haengt an DAS
/// Element, auf dem es gerufen wird.
/// </summary>
public class TableScriptScopedSelectorTests
{
    private static readonly string TableFilterJs;
    private static readonly string ColumnPreferencesJs;
    private static readonly string FaListeGruppierungJs;
    private static readonly string FaListeWiederholungJs;

    static TableScriptScopedSelectorTests()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "IdealAkeWms", "wwwroot", "js", "table-filter.js")))
            dir = dir.Parent;

        var root = dir?.FullName ?? throw new FileNotFoundException("Repository-Root nicht gefunden");
        var jsDir = Path.Combine(root, "IdealAkeWms", "wwwroot", "js");
        TableFilterJs = File.ReadAllText(Path.Combine(jsDir, "table-filter.js"));
        ColumnPreferencesJs = File.ReadAllText(Path.Combine(jsDir, "column-preferences.js"));
        FaListeGruppierungJs = File.ReadAllText(Path.Combine(jsDir, "fa-liste-gruppierung.js"));
        FaListeWiederholungJs = File.ReadAllText(Path.Combine(jsDir, "fa-liste-wiederholung.js"));
    }

    // ========================================================================
    // table-filter.js — der schaedliche Fall (sortTable verschiebt Zeilen)
    // ========================================================================

    [Fact]
    public void TableFilter_SortTable_IteratesOnlyDirectTbodyChildren()
    {
        TableFilterJs.Should().Contain("querySelectorAll(':scope > tbody')",
            "sortTable darf nur die eigenen <tbody> der Tabelle sortieren — ein Descendant-'tbody' " +
            "erfasst auch das <tbody> einer verschachtelten Tabelle");
        TableFilterJs.Should().NotContain("querySelectorAll('tbody')",
            "ein unskopiertes querySelectorAll('tbody') ist genau der Rueckfall, der die " +
            "Kombigeraete-Varianten-Tabelle geleert hat (Befund 2026-09-10)");
    }

    [Fact]
    public void TableFilter_RowIteration_IsScopedToDirectChildren()
    {
        TableFilterJs.Should().Contain("querySelectorAll(':scope > tr')",
            "die Zeilen eines <tbody> muessen als direkte Kinder gelesen werden; sonst landen die " +
            "Zeilen einer Kindtabelle in dataRows und werden per appendChild nach aussen verschoben");
        TableFilterJs.Should().NotContain("querySelectorAll('tr')",
            "unskopiertes querySelectorAll('tr') greift in verschachtelte Tabellen hinein");
    }

    [Fact]
    public void TableFilter_HeaderIndexLookup_IsScopedToOuterThead()
    {
        TableFilterJs.Should().Contain("querySelectorAll(':scope > thead > tr:first-child > th')",
            "getPhysicalIndex muss die Spaltenkoepfe der AEUSSEREN Tabelle zaehlen — zieht der " +
            "Selektor die <th> einer Kindtabelle mit, verschiebt sich jeder Spaltenindex, " +
            "sobald dort ein data-col-key auftaucht");
        TableFilterJs.Should().NotContain("querySelectorAll('thead tr:first-child th')",
            "der Descendant-Selektor auf thead-th ist die latente Variante desselben Fehlers");
    }

    [Fact]
    public void TableFilter_CellLookup_IsScopedToDirectChildren()
    {
        TableFilterJs.Should().Contain("querySelectorAll(':scope > td')",
            "Zellzugriffe ueber den physischen Spaltenindex duerfen nur die eigenen <td> einer " +
            "Zeile zaehlen, nie die <td> einer darin verschachtelten Tabelle");
        TableFilterJs.Should().NotContain("querySelectorAll('td')",
            "unskopiertes querySelectorAll('td') verschiebt den Spaltenindex in Zeilen mit Kindtabelle");
    }

    // ========================================================================
    // column-preferences.js — gleiche Ursache, Wirkung: Zelle unter falscher Ueberschrift
    // ========================================================================

    [Fact]
    public void ColumnPreferences_UsesScopedSelectorConstants()
    {
        ColumnPreferencesJs.Should().Contain("var THS_SELECTOR = ':scope > thead > tr:first-child > th'",
            "Spaltenindex und Spaltenbreite beziehen sich auf die Koepfe der AEUSSEREN Tabelle");
        ColumnPreferencesJs.Should().Contain("var ROWS_SELECTOR = ':scope > tbody > tr'",
            "Spalte ausblenden / Spalten umordnen darf nur die eigenen Zeilen anfassen — sonst wird " +
            "eine Zelle der verschachtelten Tabelle geleert bzw. deren Zellen vertauscht");
    }

    [Fact]
    public void ColumnPreferences_HasNoUnscopedTableIteration()
    {
        ColumnPreferencesJs.Should().NotContain("querySelectorAll('tbody tr')",
            "Descendant-'tbody tr' erfasst die Zeilen verschachtelter Tabellen " +
            "(ProductionOrders-Kombigeraet, FaHierarchy-Kopfdaten)");
        ColumnPreferencesJs.Should().NotContain("querySelectorAll('thead tr:first-child th')",
            "Descendant-'thead ... th' zaehlt die <th> einer Kindtabelle mit");
        ColumnPreferencesJs.Should().NotContain("querySelectorAll('td, th')",
            "die Zellen einer Zeile muessen als direkte Kinder gelesen werden");
        ColumnPreferencesJs.Should().NotContain("querySelector('thead tr.filter-row')",
            "die Filterzeile gehoert zur aeusseren Tabelle");
    }

    // ========================================================================
    // FA-Liste-Bausteine — heute latent, aber dieselbe Klasse
    // ========================================================================

    [Fact]
    public void FaListeGruppierung_RowToggle_IsScopedToDirectChildren()
    {
        FaListeGruppierungJs.Should().Contain(":scope > tr.fa-liste-node-row",
            "Auf-/Zuklappen und Auto-Expand duerfen nur die eigenen Zeilen der Gruppe anfassen — " +
            "die Varianten-Zeile traegt selbst fa-liste-node-row und enthaelt eine Kindtabelle");
        FaListeGruppierungJs.Should().NotContain("querySelectorAll('.fa-liste-node-row')",
            "unskopierte Klassensuche greift in die verschachtelte Varianten-Tabelle hinein, " +
            "sobald dort Zeilen mit dieser Klasse auftauchen");
    }

    [Fact]
    public void FaListeWiederholung_StaysScoped()
    {
        // Diese Datei war von Anfang an skopiert — der Waechter haelt den Zustand fest.
        FaListeWiederholungJs.Should().Contain(":scope > tr.fa-liste-node-row",
            "die Wiederholungswert-Unterdrueckung darf nicht in die Varianten-Tabelle greifen");
        FaListeWiederholungJs.Should().Contain(":scope > [data-repeat-key]",
            "Zellen werden als direkte Kinder der Zeile gelesen");
    }
}
