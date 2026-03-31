using FluentAssertions;
using Xunit;

namespace IdealAkeWms.Tests.Helpers;

/// <summary>
/// Verifiziert dass die CSS-Regeln fuer responsive Tabellen und Mobile-Layouts korrekt sind.
///
/// Damit horizontale Scrollbar funktioniert muessen 3 Bedingungen erfuellt sein:
///   1) Wrapper (.table-responsive) hat overflow-x: auto
///   2) Tabelle hat NICHT width: 100% (Bootstrap-Default) → muss ueberschrieben werden
///   3) Zellen (th/td) haben white-space: nowrap → Text bricht nicht um, Tabelle wird breiter als Wrapper
///
/// Wenn eine dieser Bedingungen fehlt, quetscht die Tabelle ihren Inhalt zusammen
/// statt den Wrapper zu ueberragen → keine Scrollbar.
/// </summary>
public class ResponsiveCssTests
{
    private static readonly string CssContent;
    private static readonly string ViewsDir;
    private static readonly string LayoutContent;
    private static readonly string JsContent;

    static ResponsiveCssTests()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "IdealAkeWms", "wwwroot", "css", "site.css")))
            dir = dir.Parent;

        var root = dir?.FullName ?? throw new FileNotFoundException("Repository-Root nicht gefunden");
        CssContent = File.ReadAllText(Path.Combine(root, "IdealAkeWms", "wwwroot", "css", "site.css"));
        ViewsDir = Path.Combine(root, "IdealAkeWms", "Views");
        LayoutContent = File.ReadAllText(Path.Combine(ViewsDir, "Shared", "_Layout.cshtml"));
        JsContent = File.ReadAllText(Path.Combine(root, "IdealAkeWms", "wwwroot", "js", "site.js"));
    }

    // ========================================================================
    // Scrollable Tables (3 Bedingungen)
    // ========================================================================

    [Fact]
    public void Wrapper_HasOverflowXAuto()
    {
        var block = ExtractCssBlock(".table-responsive {");
        block.Should().NotBeNull();
        block.Should().Contain("overflow-x: auto",
            ".table-responsive braucht overflow-x: auto fuer die Scrollbar");
    }

    [Fact]
    public void TableInWrapper_OverridesBootstrapWidth()
    {
        var hasWidthOverride =
            CssContent.Contains("width: auto !important") ||
            CssContent.Contains("width: max-content !important");

        hasWidthOverride.Should().BeTrue(
            "Bootstrap setzt .table {{ width: 100% }}. Ohne Override mit !important " +
            "wird die Tabelle nie breiter als der Container → keine Scrollbar.");
    }

    [Fact]
    public void TableInWrapper_HasMinWidth100Percent()
    {
        CssContent.Should().Contain("min-width: 100%",
            "min-width: 100% sorgt dafuer, dass die Tabelle auf breiten Bildschirmen " +
            "die volle Containerbreite nutzt (kein schmaler Inhalt links)");
    }

    [Fact]
    public void CellsInWrapper_HaveWhiteSpaceNowrap()
    {
        var hasThNowrap = CssContent.Contains(".table-responsive th") &&
                          CssContent.Contains("white-space: nowrap");
        var hasTdNowrap = CssContent.Contains(".table-responsive td") &&
                          CssContent.Contains("white-space: nowrap");

        hasThNowrap.Should().BeTrue(
            "th-Zellen in .table-responsive brauchen white-space: nowrap " +
            "damit Header nicht umbrechen");
        hasTdNowrap.Should().BeTrue(
            "td-Zellen in .table-responsive brauchen white-space: nowrap " +
            "damit Zellentext nicht umbricht und die Tabelle breiter als der Container wird");
    }

    // ========================================================================
    // Schutz vor bekannten Fehlern
    // ========================================================================

    [Fact]
    public void Table_DoesNotHaveOverflowHidden()
    {
        var tableBlock = ExtractCssBlock(".table {");
        tableBlock.Should().NotBeNull("es muss eine .table CSS-Regel geben");
        tableBlock.Should().NotContain("overflow: hidden",
            ".table mit overflow: hidden schneidet Inhalt ab und verhindert Scrollbar");
        tableBlock.Should().NotContain("overflow:hidden");
    }

    [Fact]
    public void Html_DoesNotHaveOverflowXHidden()
    {
        var htmlBlock = ExtractCssBlock("html {");
        htmlBlock.Should().NotBeNull();
        htmlBlock.Should().NotContain("overflow-x: hidden",
            "overflow-x: hidden auf html blockiert Scrollbar von Kindelementen in manchen Browsern");
        htmlBlock.Should().NotContain("overflow-x:hidden");
    }

    [Fact]
    public void Table_DoesNotHaveBorderRadiusWithOverflow()
    {
        var tableBlock = ExtractCssBlock(".table {");
        if (tableBlock != null && tableBlock.Contains("border-radius"))
        {
            tableBlock.Should().NotContain("overflow",
                "border-radius mit overflow auf .table schneidet Inhalt ab — " +
                "border-radius gehoert auf den .table-responsive Wrapper");
        }
    }

    // ========================================================================
    // Struktureller Test: Views haben Wrapper
    // ========================================================================

    [Fact]
    public void AllViews_HaveTableResponsiveWrapper()
    {
        var cshtmlFiles = Directory.GetFiles(ViewsDir, "*.cshtml", SearchOption.AllDirectories);
        var viewsMissingWrapper = new List<string>();

        foreach (var file in cshtmlFiles)
        {
            var fileName = Path.GetFileName(file);
            if (fileName.StartsWith("Print", StringComparison.OrdinalIgnoreCase)) continue;
            if (fileName.StartsWith("_", StringComparison.OrdinalIgnoreCase)) continue;

            var content = File.ReadAllText(file);
            if (content.Contains("<table") && !content.Contains("table-responsive"))
                viewsMissingWrapper.Add(Path.GetRelativePath(ViewsDir, file));
        }

        viewsMissingWrapper.Should().BeEmpty(
            $"Views mit <table> ohne .table-responsive Wrapper: {string.Join(", ", viewsMissingWrapper)}");
    }

    // ========================================================================
    // Viewport und Mobile-Grundlagen
    // ========================================================================

    [Fact]
    public void Layout_HasViewportMetaTag()
    {
        LayoutContent.Should().Contain("width=device-width",
            "viewport meta tag muss width=device-width enthalten fuer responsive Rendering");
        LayoutContent.Should().Contain("initial-scale=1",
            "viewport meta tag muss initial-scale=1 enthalten");
    }

    [Fact]
    public void Layout_DoesNotDisableZoom()
    {
        // user-scalable=no oder maximum-scale=1 verhindert Zoom — schlecht fuer Barrierefreiheit
        LayoutContent.Should().NotContain("user-scalable=no",
            "user-scalable=no verhindert Zoom und ist schlecht fuer Barrierefreiheit (WCAG)");
        LayoutContent.Should().NotContain("maximum-scale=1",
            "maximum-scale=1 verhindert Zoom auf Mobile-Geraeten");
    }

    [Fact]
    public void Css_HasMobileFirstMediaQuery()
    {
        // Es muss mindestens eine max-width Media-Query fuer Mobile geben
        CssContent.Should().Contain("@media (max-width:",
            "CSS muss Mobile-spezifische @media-Queries enthalten");
    }

    // ========================================================================
    // Touch Targets (min 44px auf Mobile)
    // ========================================================================

    [Fact]
    public void Css_HasMinimumTouchTargetForButtons()
    {
        // Innerhalb einer max-width Media-Query muss min-height auf Buttons gesetzt sein
        CssContent.Should().Contain("min-height: 44px",
            "Buttons muessen auf Mobile min-height: 44px haben (WCAG Touch Target Size)");
    }

    [Fact]
    public void Css_HasMinimumTouchTargetForFormControls()
    {
        // form-control und form-select sollten auf Mobile 44px hoch sein
        var hasMobileFormHeight =
            CssContent.Contains("min-height: 44px") &&
            (CssContent.Contains(".form-control") || CssContent.Contains(".form-select"));

        hasMobileFormHeight.Should().BeTrue(
            "Form Controls muessen auf Mobile min-height: 44px haben fuer Touch-Bedienung");
    }

    // ========================================================================
    // iOS Auto-Zoom Schutz
    // ========================================================================

    [Fact]
    public void Css_PreventsIosAutoZoomOnInputFocus()
    {
        // font-size: 16px auf Inputs verhindert den iOS Safari Auto-Zoom bei Focus
        CssContent.Should().Contain("font-size: 16px",
            "Form Inputs brauchen font-size: 16px auf Mobile um iOS Safari Auto-Zoom zu verhindern");
    }

    // ========================================================================
    // Navbar Mobile User Info
    // ========================================================================

    [Fact]
    public void Layout_HasMobileUserInfo()
    {
        LayoutContent.Should().Contain("navbar-user-mobile",
            "Layout muss ein navbar-user-mobile Element enthalten, " +
            "damit Benutzer sich auf Mobile abmelden koennen");
    }

    [Fact]
    public void Css_HasMobileUserInfoStyles()
    {
        CssContent.Should().Contain(".navbar-user-mobile",
            "CSS muss Styles fuer .navbar-user-mobile enthalten");
    }

    // ========================================================================
    // Page Header Flex-Wrap
    // ========================================================================

    [Fact]
    public void AllViews_PageHeadersHaveFlexWrap()
    {
        var cshtmlFiles = Directory.GetFiles(ViewsDir, "*.cshtml", SearchOption.AllDirectories);
        var viewsMissingFlexWrap = new List<string>();

        foreach (var file in cshtmlFiles)
        {
            var fileName = Path.GetFileName(file);
            if (fileName.StartsWith("_", StringComparison.OrdinalIgnoreCase)) continue;

            var content = File.ReadAllText(file);

            // Suche nach d-flex justify-content-between OHNE flex-wrap
            if (content.Contains("d-flex justify-content-between") &&
                !content.Contains("flex-wrap"))
            {
                viewsMissingFlexWrap.Add(Path.GetRelativePath(ViewsDir, file));
            }
        }

        viewsMissingFlexWrap.Should().BeEmpty(
            $"Views mit d-flex justify-content-between ohne flex-wrap (bricht auf Mobile): " +
            $"{string.Join(", ", viewsMissingFlexWrap)}");
    }

    // ========================================================================
    // Scroll Indicator
    // ========================================================================

    [Fact]
    public void Css_HasStickyScrollbar()
    {
        CssContent.Should().Contain(".table-sticky-scrollbar",
            "CSS muss Styles fuer die sticky Scrollbar enthalten (.table-sticky-scrollbar)");
        CssContent.Should().Contain("position: sticky",
            "Die sticky Scrollbar muss position: sticky verwenden um am Viewport-Rand zu kleben");
    }

    [Fact]
    public void Js_HasStickyScrollbarLogic()
    {
        JsContent.Should().Contain("table-sticky-scrollbar",
            "site.js muss die sticky Scrollbar dynamisch erstellen");
        JsContent.Should().Contain("scrollLeft",
            "site.js muss die Scroll-Position zwischen Tabelle und Sticky-Bar synchronisieren");
        JsContent.Should().Contain("IntersectionObserver",
            "site.js muss IntersectionObserver verwenden um die Sticky-Bar auszublenden " +
            "wenn die echte Scrollbar sichtbar ist");
    }

    // ========================================================================
    // Scrollbar Styling (immer sichtbar)
    // ========================================================================

    [Fact]
    public void Css_HasWebkitScrollbarStyling()
    {
        CssContent.Should().Contain("::-webkit-scrollbar",
            "CSS muss Webkit-Scrollbar-Styling haben damit die Scrollbar immer sichtbar ist");
    }

    [Fact]
    public void Css_HasFirefoxScrollbarStyling()
    {
        CssContent.Should().Contain("scrollbar-color",
            "CSS muss Firefox scrollbar-color haben fuer sichtbare Scrollbar");
    }

    // ========================================================================
    // Responsive Pagination
    // ========================================================================

    [Fact]
    public void Css_HasResponsivePagination()
    {
        // Pagination sollte flex-wrap haben fuer Mobile
        var hasPaginationWrap =
            CssContent.Contains(".pagination") &&
            CssContent.Contains("flex-wrap");

        hasPaginationWrap.Should().BeTrue(
            "Pagination muss flex-wrap haben damit Seitenzahlen auf Mobile umbrechen");
    }

    // ========================================================================
    // Nested Table Responsive
    // ========================================================================

    [Fact]
    public void Css_HasNestedTableResponsive()
    {
        CssContent.Should().Contain(".nested-table-responsive",
            "CSS muss .nested-table-responsive fuer verschachtelte Tabellen enthalten");
    }

    [Fact]
    public void TrackingIndex_HasNestedTableWrapper()
    {
        var trackingIndexPath = Path.Combine(ViewsDir, "Tracking", "Index.cshtml");
        if (File.Exists(trackingIndexPath))
        {
            var content = File.ReadAllText(trackingIndexPath);
            content.Should().Contain("nested-table-responsive",
                "Tracking/Index.cshtml muss nested-table-responsive fuer verschachtelte AG-Tabelle verwenden");
        }
    }

    // ========================================================================
    // Filter Form Mobile Stacking
    // ========================================================================

    [Fact]
    public void Css_HasFilterCardMobileStacking()
    {
        // Filter cards sollten col-md Spalten auf Mobile zu 100% Breite aendern
        var hasFilterStacking =
            CssContent.Contains(".filter-card") &&
            CssContent.Contains("col-md");

        hasFilterStacking.Should().BeTrue(
            "Filter-Karten-Spalten muessen auf Mobile auf volle Breite stacken");
    }

    // ========================================================================
    // Hilfsmethoden
    // ========================================================================

    private static string? ExtractCssBlock(string selector)
    {
        var idx = CssContent.IndexOf(selector, StringComparison.Ordinal);
        if (idx < 0) return null;

        var braceStart = CssContent.IndexOf('{', idx);
        if (braceStart < 0) return null;

        var depth = 1;
        var pos = braceStart + 1;
        while (pos < CssContent.Length && depth > 0)
        {
            if (CssContent[pos] == '{') depth++;
            else if (CssContent[pos] == '}') depth--;
            pos++;
        }
        return CssContent[braceStart..pos];
    }
}
