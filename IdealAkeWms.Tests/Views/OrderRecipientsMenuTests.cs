using FluentAssertions;
using Xunit;

namespace IdealAkeWms.Tests.Views;

/// <summary>
/// QUELLTEXT-WAECHTER (v1.99.44): „Empfänger“ haengt an allen drei Nutzern der Empfaengergruppen (Bedarfsmeldungen,
/// Lager-/Glasbestellung, BF-Auftraege), die „Artikelgruppen-Zuordnung“ nur an den Bedarfsmeldungen.
/// Sichtbarkeit zur Laufzeit ist Manual-UAT (docs/TESTSZENARIEN.md).
/// </summary>
public class OrderRecipientsMenuTests
{
    private static string Layout()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "IdealAkeWms", "wwwroot", "js", "table-filter.js")))
            dir = dir.Parent;
        return File.ReadAllText(Path.Combine(dir!.FullName, "IdealAkeWms", "Views", "Shared", "_Layout.cshtml"));
    }

    private static string BedingungVor(string layout, string link)
    {
        var pos = layout.IndexOf(link, StringComparison.Ordinal);
        pos.Should().BePositive();
        var ifPos = layout.LastIndexOf("@if (", pos, StringComparison.Ordinal);
        return layout.Substring(ifPos, layout.IndexOf('{', ifPos) - ifPos);
    }

    [Fact]
    public void Empfaenger_BeiBestellungenLagerbestellungOderBlechfertigung()
    {
        var b = BedingungVor(Layout(), "asp-action=\"Index\">Empfänger</a>");
        b.Should().Contain("bestellungenAktiv").And.Contain("lagerbestellungAktiv").And.Contain("AppSettingKeys.BlechfertigungAktiv");
    }

    [Fact]
    public void ArtikelgruppenZuordnung_NurBeiBestellungen()
    {
        BedingungVor(Layout(), "asp-action=\"ArticleGroupMappings\">Artikelgruppen-Zuordnung</a>").Trim()
            .Should().Be("@if (bestellungenAktiv)");
    }
}
