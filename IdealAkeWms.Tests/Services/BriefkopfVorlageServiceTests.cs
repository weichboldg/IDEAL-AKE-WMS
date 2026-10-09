using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Services;

/// <summary>
/// Tests fuer den Briefkopf aus der IT-Vorlage (Spec 2026-10-06-beschichtungsauftrag-ausbau-teil-2, v1.49.0):
/// Laden, Bereinigen (schlanke Regex-Bereinigung), Einbetten als Data-URI, Grenzen, Ersatz-Kopf.
/// Jeder Test arbeitet in einem eigenen Temp-Ordner (= Branding-Ordner).
/// </summary>
public sealed class BriefkopfVorlageServiceTests : IDisposable
{
    private readonly string _root;
    private readonly string _branding;

    public BriefkopfVorlageServiceTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "briefkopf-tests-" + Guid.NewGuid().ToString("N"));
        _branding = Path.Combine(_root, "Branding");
        Directory.CreateDirectory(_branding);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { /* Temp-Ordner, egal */ }
    }

    private string Vorlage(string html, string name = "briefkopf.html")
    {
        var p = Path.Combine(_branding, name);
        File.WriteAllText(p, html, Encoding.UTF8);
        return p;
    }

    private void Datei(string name, int bytes, string? dir = null)
        => File.WriteAllBytes(Path.Combine(dir ?? _branding, name), Enumerable.Repeat((byte)7, bytes).ToArray());

    private const string Html = """
        <!DOCTYPE html><html><head><style>
        @font-face { font-family: 'Nimbus'; src: url('nimbus.woff2') format('woff2'); }
        .briefkopf .logo { height: 17.5mm; }
        </style></head>
        <body><div class="kopf"><img class="logo" src="logo.jpg" alt="Logo"><span>IDEAL Kältetechnik GmbH</span></div></body></html>
        """;

    // ----- Happy Path -----

    [Fact]
    public void Load_ValidTemplate_EmbedsImageAndFont_AsDataUri()
    {
        Datei("logo.jpg", 100);
        Datei("nimbus.woff2", 50);

        var v = BriefkopfVorlageService.Load(Vorlage(Html));

        v.IstErsatz.Should().BeFalse();
        v.Hinweise.Should().BeEmpty();
        v.Markup.Should().Contain("src=\"data:image/jpeg;base64,").And.Contain("IDEAL Kältetechnik GmbH");
        v.Markup.Should().NotContain("<style").And.NotContain("<body").And.NotContain("<head");
        v.Styles.Should().Contain("url(\"data:font/woff2;base64,").And.Contain(".briefkopf .logo");
    }

    [Fact]
    public void Load_FragmentWithoutBody_UsesWholeFile()
    {
        Datei("logo.jpg", 10);
        var v = BriefkopfVorlageService.Load(Vorlage("<style>.briefkopf p{color:#4a494a}</style><p>Hallo</p><img src=\"logo.jpg\">"));

        v.IstErsatz.Should().BeFalse();
        v.Markup.Should().Contain("<p>Hallo</p>").And.NotContain("<style");
        v.Styles.Should().Contain("color:#4a494a");
    }

    [Fact]
    public void Load_UpperCaseExtension_Embedded()
    {
        Datei("LOGO.JPG", 10);
        var v = BriefkopfVorlageService.Load(Vorlage("<img src=\"LOGO.JPG\">"));

        v.Markup.Should().Contain("data:image/jpeg;base64,");
        v.Hinweise.Should().BeEmpty();
    }

    // ----- Ersatz-Kopf -----

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Load_NoPath_IsErsatz_NotConfigured(string? pfad)
    {
        var v = BriefkopfVorlageService.Load(pfad);

        v.IstErsatz.Should().BeTrue();
        v.Hinweise.Should().ContainSingle().Which.Should().Contain("nicht konfiguriert");
    }

    [Fact]
    public void Load_FileMissing_IsErsatz_WithPath()
    {
        var p = Path.Combine(_branding, "fehlt.html");
        var v = BriefkopfVorlageService.Load(p);

        v.IstErsatz.Should().BeTrue();
        v.Hinweise.Should().ContainSingle().Which.Should().Contain("nicht gefunden").And.Contain(p);
    }

    [Fact]
    public void Load_WrongExtension_IsErsatz_AndContentNeverAppears()
    {
        var p = Vorlage("""{ "ConnectionStrings": { "Default": "Server=geheim" } }""", "appsettings.json");

        var v = BriefkopfVorlageService.Load(p);

        v.IstErsatz.Should().BeTrue();
        v.Markup.Should().BeEmpty();
        v.Styles.Should().BeEmpty();
        v.Hinweise.Single().Should().Contain(".html");
    }

    [Fact]
    public void Load_TemplateTooLarge_IsErsatz()
    {
        var p = Vorlage("<p>x</p><!--" + new string('a', BriefkopfVorlageService.MaxVorlageBytes) + "-->");

        var v = BriefkopfVorlageService.Load(p);

        v.IstErsatz.Should().BeTrue();
        v.Hinweise.Single().Should().Contain("zu groß");
    }

    [Fact]
    public void Load_PathIsDirectory_FallsBackToErsatz()
    {
        var dirNamedHtml = Path.Combine(_branding, "ordner.html");
        Directory.CreateDirectory(dirNamedHtml);

        var v = BriefkopfVorlageService.Load(dirNamedHtml);

        v.IstErsatz.Should().BeTrue();
        v.Hinweise.Should().ContainSingle();
    }

    // ----- fehlende Ressourcen -----

    [Fact]
    public void Load_ImageMissing_ImgRemoved_HintNamesFile()
    {
        var v = BriefkopfVorlageService.Load(Vorlage("<div><img class=\"logo\" src=\"logo.jpg\"><span>Text</span></div>"));

        v.IstErsatz.Should().BeFalse();
        v.Markup.Should().NotContain("<img").And.Contain("<span>Text</span>");
        v.Hinweise.Should().ContainSingle().Which.Should().Contain("logo.jpg");
    }

    [Fact]
    public void Load_FontMissing_UrlBecomesNone_HintNamesFile()
    {
        Datei("logo.jpg", 10);
        var v = BriefkopfVorlageService.Load(Vorlage(Html));

        // url(...) wird zu none -> der src-Deskriptor ist ungueltig, die Regel laedt nichts, Systemschrift greift.
        v.Styles.Should().Contain("src: none").And.NotContain("nimbus.woff2");
        v.Hinweise.Should().ContainSingle().Which.Should().Contain("nimbus.woff2");
    }

    // ----- Bereinigung aktiver Inhalte -----

    [Fact]
    public void Load_RemovesActiveContent()
    {
        Datei("logo.jpg", 10);
        var v = BriefkopfVorlageService.Load(Vorlage("""
            <html><head><meta http-equiv="refresh" content="0;url=https://x"><link rel="stylesheet" href="https://x/a.css"><base href="https://x/">
            <style>@import url('https://x/b.css'); .briefkopf{color:#053153}</style></head><body>
            <script>alert(1)</script><SCRIPT type="text/javascript">alert(9)</SCRIPT>
            <img src="logo.jpg" onerror="alert(2)" srcset="https://x/big.jpg 2x">
            <a href="javascript:alert(3)">x</a><a href=" JaVaScRiPt:alert(4)">y</a><a href="vbscript:msgbox">z</a>
            <iframe src="https://x"></iframe><object data="x.swf"></object><embed src="x.swf">
            <svg><script>alert(5)</script><circle r="1"/></svg>
            <div onclick='alert(6)' style="color:#4a494a">ok</div><!-- Kommentar <script>alert(7)</script> -->
            </body></html>
            """));

        var all = v.Styles + v.Markup;
        foreach (var bad in new[] { "<script", "alert(", "onerror", "onclick", "javascript:", "vbscript:", "srcset",
                                    "<iframe", "<object", "<embed", "<svg", "<link", "<base", "<meta", "@import", "https://x" })
            all.Should().NotContainEquivalentOf(bad);
        v.Markup.Should().Contain(">ok</div>").And.Contain("data:image/jpeg;base64,");
        v.Styles.Should().Contain(".briefkopf{color:#053153}");
    }

    // ----- Unausgeglichenes Markup (QA I-1): lieber Ersatz-Kopf + Hinweis als eine zerstoerte Druckseite -----

    [Theory]
    [InlineData("<p>Kopf</p><!-- vergessen zu schliessen <p>Rest</p>", "Kommentar")]
    [InlineData("<p>Kopf</p><style>.briefkopf p{color:red}", "Element")]
    [InlineData("<p>Kopf</p><textarea>offen", "Element")]
    [InlineData("<div><p>Kopf</p></div></div>", "div")]
    [InlineData("<div><div><p>Kopf</p></div>", "div")]
    [InlineData("<style>.briefkopf{color:red}</style x><p>raus aus dem Style</p></style><p>Kopf</p>", "Style")]
    public void Load_UnbalancedMarkup_IsErsatz_WithHint(string html, string grund)
    {
        var v = BriefkopfVorlageService.Load(Vorlage(html));

        v.IstErsatz.Should().BeTrue();
        v.Markup.Should().BeEmpty();
        v.Styles.Should().BeEmpty();
        v.Hinweise.Should().ContainSingle().Which.Should().Contain(grund);
    }

    [Fact]
    public void Load_TextThatLooksLikeAnAttribute_StaysUntouched()
    {
        var v = BriefkopfVorlageService.Load(Vorlage("<p>Kontakt online = 24/7 · onsite=ja</p>"));

        v.Markup.Should().Be("<p>Kontakt online = 24/7 · onsite=ja</p>");
    }

    [Fact]
    public void Load_QuotedPath_IsAccepted()
    {
        var p = Vorlage("<p>Hallo</p>");

        var v = BriefkopfVorlageService.Load("\"" + p + "\"");

        v.IstErsatz.Should().BeFalse();
        v.Markup.Should().Be("<p>Hallo</p>");
    }

    [Fact]
    public void Load_ManyReferencesToSameFile_StopAtOutputBudget()
    {
        Datei("a.jpg", BriefkopfVorlageService.MaxDateiBytes);
        var html = string.Concat(Enumerable.Repeat("<img src=\"a.jpg\">", 30));

        var v = BriefkopfVorlageService.Load(Vorlage(html));

        System.Text.RegularExpressions.Regex.Matches(v.Markup, "<img").Count.Should().BeLessThan(30);
        v.Hinweise.Should().NotBeEmpty().And.OnlyContain(h => h.Contains("a.jpg"));
    }

    // ----- Pfad-Ausbruch und fremde Quellen -----

    [Theory]
    [InlineData("../geheim.png")]
    [InlineData("..\\geheim.png")]
    [InlineData("C:\\Windows\\logo.png")]
    [InlineData("\\\\server\\share\\logo.png")]
    [InlineData("https://www.ideal-ake.at/logo.png")]
    [InlineData("http://www.ideal-ake.at/logo.png")]
    [InlineData("//cdn.example/logo.png")]
    [InlineData("file:///C:/logo.png")]
    [InlineData("/logo.png")]
    public void Load_ForeignOrEscapingReference_NotEmbedded(string src)
    {
        Datei("geheim.png", 10, _root);   // existiert ausserhalb des Branding-Ordners
        Datei("logo.png", 10);

        var v = BriefkopfVorlageService.Load(Vorlage($"<img src=\"{src}\"><style>.briefkopf{{background:url('{src}')}}</style>"));

        v.Markup.Should().NotContain("<img").And.NotContain("data:");
        v.Styles.Should().NotContain("data:").And.NotContain(src);
        v.Hinweise.Should().HaveCount(2).And.OnlyContain(h => h.Contains("abgelehnt"));
    }

    [Fact]
    public void Load_SvgFile_Rejected()
    {
        File.WriteAllText(Path.Combine(_branding, "logo.svg"), "<svg/>");
        var v = BriefkopfVorlageService.Load(Vorlage("<img src=\"logo.svg\">"));

        v.Markup.Should().NotContain("<img");
        v.Hinweise.Single().Should().Contain("logo.svg");
    }

    [Fact]
    public void Load_DataUri_AllowedKept_SvgRemoved()
    {
        var v = BriefkopfVorlageService.Load(Vorlage(
            "<img id=\"a\" src=\"data:image/png;base64,AAAA\"><img id=\"b\" src=\"data:image/svg+xml;base64,PHN2Zz4=\">"));

        v.Markup.Should().Contain("id=\"a\"").And.NotContain("id=\"b\"").And.NotContain("svg");
    }

    // ----- Groessengrenzen -----

    [Fact]
    public void Load_ResourceOverFileLimit_NotEmbedded()
    {
        Datei("gross.jpg", BriefkopfVorlageService.MaxDateiBytes + 1);
        var v = BriefkopfVorlageService.Load(Vorlage("<img src=\"gross.jpg\">"));

        v.IstErsatz.Should().BeFalse();
        v.Markup.Should().NotContain("<img");
        v.Hinweise.Single().Should().Contain("gross.jpg").And.Contain("zu groß");
    }

    [Fact]
    public void Load_ResourcesOverSumLimit_LaterOnesNotEmbedded()
    {
        var mb = BriefkopfVorlageService.MaxDateiBytes;
        Datei("a.jpg", mb); Datei("b.jpg", mb); Datei("c.jpg", mb); Datei("d.jpg", 10);

        var v = BriefkopfVorlageService.Load(Vorlage("<img src=\"a.jpg\"><img src=\"b.jpg\"><img src=\"c.jpg\"><img src=\"d.jpg\">"));

        System.Text.RegularExpressions.Regex.Matches(v.Markup, "<img").Count.Should().Be(3);
        v.Hinweise.Single().Should().Contain("d.jpg").And.Contain("Summe");
    }

    [Fact]
    public void Load_SameFileTwice_CountsOnceAgainstSum()
    {
        var mb = BriefkopfVorlageService.MaxDateiBytes;
        Datei("a.jpg", mb); Datei("b.jpg", mb); Datei("c.jpg", mb);

        var v = BriefkopfVorlageService.Load(Vorlage("<img src=\"a.jpg\"><img src=\"a.jpg\"><img src=\"b.jpg\"><img src=\"c.jpg\">"));

        System.Text.RegularExpressions.Regex.Matches(v.Markup, "<img").Count.Should().Be(4);
        v.Hinweise.Should().BeEmpty();
    }

    // ----- Muster-Vorlage im Repo (Branding/briefkopf.html, Lieferumfang fuer die IT) -----

    [Fact]
    public void MusterVorlage_Laedt_LogoEingebettet_NurSchriftdateienFehlen_AlleRegelnUnterBriefkopf()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Branding", "briefkopf.html")))
            dir = dir.Parent;
        dir.Should().NotBeNull("Branding/briefkopf.html liegt in der Repo-Wurzel");

        var v = BriefkopfVorlageService.Load(Path.Combine(dir!.FullName, "Branding", "briefkopf.html"));

        v.IstErsatz.Should().BeFalse();
        v.Markup.Should().Contain("data:image/jpeg;base64,").And.Contain("In der Schörihub 28, 4810 Gmunden");
        // Schriftdateien sind lizenziert und bewusst NICHT im Repo -> genau diese zwei Hinweise.
        v.Hinweise.Should().HaveCount(2).And.OnlyContain(h => h.Contains("NimbusSansNovus") && h.Contains("nicht gefunden"));

        // AK 11: jede Regel ausser @font-face unter .briefkopf; AKE Blau nicht als Textfarbe.
        var selectors = System.Text.RegularExpressions.Regex.Matches(v.Styles, @"(^|\})\s*([^{}@]+)\{")
            .Select(m => m.Groups[2].Value.Trim()).Where(s => s.Length > 0).ToList();
        selectors.Should().NotBeEmpty().And.OnlyContain(s => s.StartsWith(".briefkopf"));
        v.Styles.Should().NotMatchRegex(@"(?<![-\w])color:\s*#43a6e2");
        v.Styles.Should().Contain("color: #053153").And.Contain("color: #4a494a").And.Contain("height: 17.5mm");
    }

    // ----- GetAsync: Einstellung, Firmendaten, Log, einmal je Anfrage -----

    [Fact]
    public async Task GetAsync_ReadsSettings_LogsHints_AndLoadsOncePerScope()
    {
        var settings = new Mock<IAppSettingRepository>();
        settings.Setup(s => s.GetValueAsync(AppSettingKeys.BriefkopfVorlagePfad)).ReturnsAsync((string?)null);
        settings.Setup(s => s.GetValueAsync(AppSettingKeys.Firmenname)).ReturnsAsync("IDEAL Kältetechnik GmbH");
        settings.Setup(s => s.GetValueAsync(AppSettingKeys.Firmenanschrift)).ReturnsAsync("In der Schörihub 28, 4810 Gmunden");
        var logger = new Mock<ILogger<BriefkopfVorlageService>>();
        var svc = new BriefkopfVorlageService(settings.Object, logger.Object);

        var v1 = await svc.GetAsync();
        var v2 = await svc.GetAsync();

        v1.Should().BeSameAs(v2);
        v1.IstErsatz.Should().BeTrue();
        v1.Firmenname.Should().Be("IDEAL Kältetechnik GmbH");
        v1.Firmenanschrift.Should().Be("In der Schörihub 28, 4810 Gmunden");
        settings.Verify(s => s.GetValueAsync(AppSettingKeys.BriefkopfVorlagePfad), Times.Once);
        logger.Verify(l => l.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
}
