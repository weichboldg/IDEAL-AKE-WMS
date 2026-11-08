using FluentAssertions;
using Xunit;

namespace IdealAkeWms.Tests.Views;

/// <summary>v1.94.0 (Spec 2026-11-08-seiten-querscrollen-navigationsleiste): Navbar bricht um, Benutzername nie abgeschnitten,
/// Kartenlink „wird nachgeliefert“ mit ausreichendem Kontrast. Sichtpruefung bleibt Pflicht (TS-123).</summary>
public class LayoutQuerscrollenV194Tests
{
    private static string ViewText(params string[] teile)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "IdealAkeWms", "Views"))) dir = dir.Parent;
        return File.ReadAllText(Path.Combine(new[] { dir!.FullName, "IdealAkeWms", "Views" }.Concat(teile).ToArray()));
    }

    [Fact]
    public void Navbar_UmbruchStattUeberlauf_BenutzernameNichtAbgeschnitten_KartenlinkKontrast()
    {
        var layout = ViewText("Shared", "_Layout.cshtml");
        layout.Should().Contain("<ul class=\"navbar-nav me-auto flex-wrap\">");
        layout.Should().Contain("<span class=\"user-info d-none d-lg-inline flex-shrink-0 text-nowrap\">");
        var karte = ViewText("WarehouseRequisitions", "Index.cshtml");
        karte.Should().Contain("class=\"text-warning-emphasis fw-semibold text-decoration-none\"").And.NotContain("class=\"text-warning fw-semibold");
    }
}
