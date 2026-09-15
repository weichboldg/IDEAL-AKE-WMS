using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace IdealAkeWms.Tests.Helpers;

/// <summary>
/// QUELLTEXT-WAECHTER (kein Verhaltenstest) fuer die Artikel-Typeahead-Suche.
///
/// Was dieser Test beweist: dass die drei Widgets, die gegen /api/articles/search suchen
/// (Select2-Partial, InboundBulk-Zeilen, WarehouseRequisitions-Fetch), erst ab drei Zeichen
/// feuern. Was er NICHT beweist: dass die Entprellung im Browser tatsaechlich wirkt — es gibt in
/// diesem Projekt keine JS-Testinfrastruktur, der Nachweis dafuer ist Manual-UAT.
///
/// Hintergrund (AK 15, 2026-09-15): /api/articles/search scannt ohne Mindestlaenge ~108.818
/// Zeilen pro Tastendruck. Der Guard ist die einzige Bremse dagegen.
///
/// Bewusst NICHT geprueft: _Select2ProductionOrderPartial.cshtml (Auftragssuche, nicht
/// Artikelsuche) und Views/Picking/Bom.cshtml (BOM-Komponentensuche) — beide ausserhalb des
/// Matchcode-Scopes.
/// </summary>
public class ArticleSearchMinLengthSourceTests
{
    private static readonly string Select2ArticlePartial;
    private static readonly string InboundBulk;
    private static readonly string WarehouseRequisitionsEdit;

    static ArticleSearchMinLengthSourceTests()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "IdealAkeWms", "Views", "Shared", "_Select2ArticlePartial.cshtml")))
            dir = dir.Parent;

        var root = dir?.FullName ?? throw new FileNotFoundException("Repository-Root nicht gefunden");
        var viewsDir = Path.Combine(root, "IdealAkeWms", "Views");
        Select2ArticlePartial = File.ReadAllText(Path.Combine(viewsDir, "Shared", "_Select2ArticlePartial.cshtml"));
        InboundBulk = File.ReadAllText(Path.Combine(viewsDir, "StockMovements", "InboundBulk.cshtml"));
        WarehouseRequisitionsEdit = File.ReadAllText(Path.Combine(viewsDir, "WarehouseRequisitions", "Edit.cshtml"));
    }

    // Erlaubt Whitespace-Varianten um den Doppelpunkt (z.B. "minimumInputLength:3").
    private static readonly Regex MinInputLengthZero = new(@"minimumInputLength\s*:\s*0\b", RegexOptions.Compiled);
    private static readonly Regex MinInputLengthThree = new(@"minimumInputLength\s*:\s*3\b", RegexOptions.Compiled);
    private static readonly Regex QLengthLessThanThree = new(@"q\.length\s*<\s*3\b", RegexOptions.Compiled);

    [Fact]
    public void Select2ArticlePartial_ReferencesArticleSearchApi()
    {
        // Sanity-Check: verhindert, dass der Waechter durch eine URL-Aenderung wirkungslos wird.
        Select2ArticlePartial.Should().Contain("/api/articles/search");
    }

    [Fact]
    public void Select2ArticlePartial_RequiresThreeCharsMinimum()
    {
        MinInputLengthThree.IsMatch(Select2ArticlePartial).Should().BeTrue(
            "das Select2-Artikel-Widget darf erst ab drei Zeichen gegen /api/articles/search suchen " +
            "(AK 15, ~108.818 Zeilen pro Tastendruck ohne Mindestlaenge)");
        MinInputLengthZero.IsMatch(Select2ArticlePartial).Should().BeFalse(
            "minimumInputLength: 0 waere der Rueckfall auf einen Scan pro Tastendruck");
    }

    [Fact]
    public void InboundBulk_ReferencesArticleSearchApi()
    {
        InboundBulk.Should().Contain("/api/articles/search");
    }

    [Fact]
    public void InboundBulk_ArticleSelect2_RequiresThreeCharsMinimum()
    {
        MinInputLengthThree.IsMatch(InboundBulk).Should().BeTrue(
            "das Zeilen-Select2 in der Mehrfach-Einbuchung darf erst ab drei Zeichen suchen");
        MinInputLengthZero.IsMatch(InboundBulk).Should().BeFalse(
            "minimumInputLength: 0 an diesem Widget scannt wieder pro Tastendruck");
    }

    [Fact]
    public void WarehouseRequisitionsEdit_ReferencesArticleSearchApi()
    {
        WarehouseRequisitionsEdit.Should().Contain("/api/articles/search");
    }

    [Fact]
    public void WarehouseRequisitionsEdit_ArticleFetch_RequiresThreeCharsMinimum()
    {
        QLengthLessThanThree.IsMatch(WarehouseRequisitionsEdit).Should().BeTrue(
            "der custom fetch bei #article-search muss den Laengen-Guard auf drei Zeichen anheben");
        WarehouseRequisitionsEdit.Should().NotContain("q.length < 2",
            "der alte Zwei-Zeichen-Guard ist genau der Rueckfall, den AK 15 vermeiden soll");
    }

    [Fact]
    public void WarehouseRequisitionsEdit_ArticleFetch_KeepsExistingDebounce()
    {
        // Die Entprellung existierte schon vor AK 15 (setTimeout, ~250ms) — dieser Test haelt nur
        // fest, dass sie nicht versehentlich beim Anheben des Guards entfernt wurde.
        WarehouseRequisitionsEdit.Should().Contain("setTimeout",
            "die Entprellung des #article-search-Fetches darf beim Anheben des Laengen-Guards nicht wegfallen");
    }
}
