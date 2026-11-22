using FluentAssertions;
using Xunit;

namespace IdealAkeWms.Tests.Helpers;

/// <summary>v1.99.47 (QA Teil 6): <c>data-x="@Json.Serialize(obj)"</c> schreibt IHtmlContent ROH ins Attribut – die JSON-Anfuehrungszeichen
/// beenden das doppelt gequotete Attribut, <c>JSON.parse</c> im Browser wirft. Erlaubt nur mit <c>.ToString()</c> (Razor kodiert dann).</summary>
public class JsonImAttributWaechterTests
{
    [Fact]
    public void KeinRohesJsonSerializeInDoppeltGequotetemAttribut()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "IdealAkeWms", "Views"))) dir = dir.Parent;
        dir.Should().NotBeNull("Repository-Root nicht gefunden");

        var funde = Directory.EnumerateFiles(Path.Combine(dir!.FullName, "IdealAkeWms", "Views"), "*.cshtml", SearchOption.AllDirectories)
            .SelectMany(f => File.ReadLines(f).Select((z, i) => (f, i, z)))
            .Where(x => x.z.Contains("=\"@Json.Serialize(") && !x.z.Contains(").ToString()\""))
            .Select(x => $"{Path.GetFileName(x.f)}:{x.i + 1}")
            .ToList();

        funde.Should().BeEmpty();
    }
}
