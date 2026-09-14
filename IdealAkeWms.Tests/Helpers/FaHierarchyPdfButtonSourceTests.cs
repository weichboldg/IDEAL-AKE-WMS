using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace IdealAkeWms.Tests.Helpers;

/// <summary>Kein JS/View-Testharness im Projekt — Quelltext-Waechter fuer die zwei Dinge, die beim
/// Kopieren des Drucken-Links still schiefgehen: <c>target="_blank"</c> am PDF-Knopf (AK 16) und ein
/// PDF-Knopf ohne Sichtbarkeits-Bedingung (AK 13).</summary>
public class FaHierarchyPdfButtonSourceTests
{
    [Theory]
    [InlineData("Views/FaHierarchyKommissionierListen/Index.cshtml")]
    [InlineData("Views/FaHierarchyBeschichtung/Index.cshtml")]
    public void PdfButton_IsGatedAndDownloadsDirectly(string relativePath)
    {
        var src = File.ReadAllText(Path.Combine(FindRepoRoot(), "IdealAkeWms", relativePath));

        var anchors = Regex.Matches(src, "<a [^>]*Url\\.Action\\(\"Pdf\"[^>]*>", RegexOptions.Singleline).Select(m => m.Value).ToList();
        anchors.Should().HaveCount(1, "genau ein PDF-Knopf je Gruppen-Kopfzeile");
        anchors[0].Should().NotContain("target=", "direkter Download, kein neuer Tab — nicht vom Drucken-Link erben (AK 16)");
        src.Should().Contain("pdfQuery = Context.Request.QueryString.ToString()", "target/colf_* muessen mit ins PDF (Klarstellung zu Antwort 7)");
        anchors[0].Should().Contain("+ pdfQuery", "der Query-String haengt am PDF-Link");
        src.Should().Contain("@if (Model.PdfVerfuegbar)", "ohne Edge erscheint der Knopf gar nicht (AK 13)");
        src.Should().MatchRegex(">\\s*PDF\\s*</a>", "Icon UND Text — der Text ist Pflicht");
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "IdealAkeWms.slnx"))) dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("IdealAkeWms.slnx nicht gefunden");
    }
}
