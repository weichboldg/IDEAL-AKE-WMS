using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace IdealAkeWms.Tests.Views;

/// <summary>
/// QUELLTEXT-WAECHTER (kein Verhaltenstest) fuer Befund B-ABN-1 (Spec 2026-09-25-kommissionierung-nur-hauptfa):
/// Die Komm.-Ziel-Auswahl der Stueckliste muss F5 ueberstehen. Gespeichert wird der sessionStorage
/// ('tableFilters:Bom') ausschliesslich ueber table-filter.js applyFilters() -> saveFiltersToStorage(); der
/// einzige oeffentliche Einstieg dorthin ist window.setColumnFilter (in Bom.cshtml zusaetzlich um
/// updateBomVisibility() gewickelt). Wer input.value direkt setzt, filtert zwar, speichert aber nicht.
///
/// Was der Test NICHT beweist: dass die Wiederherstellung im Browser tatsaechlich greift — das ist
/// Manual-UAT (TS-79.6 in docs/TESTSZENARIEN.md).
/// </summary>
public class BomKommissionierzielPersistenzTests
{
    private static readonly string BomCshtml;

    static BomKommissionierzielPersistenzTests()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "IdealAkeWms", "wwwroot", "js", "table-filter.js")))
            dir = dir.Parent;
        var root = dir?.FullName ?? throw new FileNotFoundException("Repository-Root nicht gefunden");
        BomCshtml = File.ReadAllText(Path.Combine(root, "IdealAkeWms", "Views", "Picking", "Bom.cshtml"));
    }

    private static string FunctionBody(string name)
    {
        var start = BomCshtml.IndexOf("function " + name + "(", StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0, $"Bom.cshtml muss die Funktion {name} enthalten");
        var next = BomCshtml.IndexOf("\n            function ", start + 10, StringComparison.Ordinal);
        return next < 0 ? BomCshtml[start..] : BomCshtml[start..next];
    }

    [Fact]
    public void DropdownChange_SpeichertUeberSetColumnFilter_NichtDirektInsInput()
    {
        var body = FunctionBody("setupKommissionierzielDropdown");

        body.Should().NotContain("input.value = vals.join(",
            "eine direkte Zuweisung filtert, schreibt aber nichts in den sessionStorage (B-ABN-1)");
        body.Should().Contain("window.setColumnFilter('kommissionieren', vals.join(','))",
            "nur window.setColumnFilter fuehrt ueber applyFilters() zu saveFiltersToStorage()");
    }

    [Fact]
    public void AlleFilterZuruecksetzen_LeertUeberSetColumnFilter()
    {
        var body = FunctionBody("resetAllBomFilters");

        Regex.IsMatch(body, @"input\.value\s*=\s*''").Should().BeFalse(
            "direktes Leeren laesst den alten Eintrag im sessionStorage stehen — nach F5 waeren die Filter wieder da");
        body.Should().Contain("window.setColumnFilter(");
    }

    [Fact]
    public void ProfilStandard_UeberschreibtKeineWiederhergestellteSessionAuswahl()
    {
        BomCshtml.Should().Contain(
            "if (defaultBomKommissionierziel && !bomFilterInputValue('kommissionieren')) window.setColumnFilter('kommissionieren', defaultBomKommissionierziel);",
            "die Auswahl dieser Sitzung (aus dem sessionStorage wiederhergestellt) gewinnt vor dem Profil-Standard");
    }
}
