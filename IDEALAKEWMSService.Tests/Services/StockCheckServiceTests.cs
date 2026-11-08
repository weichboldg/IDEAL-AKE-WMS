using FluentAssertions;
using IdealAkeWms.Models.ViewModels;
using IDEALAKEWMSService.Services;
using Xunit;

namespace IDEALAKEWMSService.Tests.Services;

/// <summary>v1.93.0 (Bug 2026-11-08-meldebestand-mail-bestand-falsch-berechnet): Zusammenfuehrung Artikelstamm + Bestand (AK 3, 4, 5).</summary>
public class StockCheckServiceTests
{
    private static StockLocationInfo P(string code, decimal menge) => new() { Code = code, Quantity = menge };

    [Fact]
    public void BuildBelowReorder_OhneBewegung_Grenze_OhneMeldebestand_Sortierung()
    {
        var articles = new (string, string?, string?, decimal)[]
        {
            ("B-OHNE", "ohne Bewegung", "Stk", 5m),   // Bestand 0 ≤ 5 → gemeldet, keine Plaetze
            ("A-GLEICH", null, null, 10m),           // 6 + 4 = 10 → gemeldet (Grenze inklusive)
            ("C-DRUEBER", null, null, 10m),          // 10,001 → nicht
            ("D-NULL", null, null, 0m),              // Meldebestand 0 → nie
        };
        var stock = new Dictionary<string, List<StockLocationInfo>>
        {
            ["A-GLEICH"] = new() { P("L02", 6m), P("L01", 4m) },
            ["C-DRUEBER"] = new() { P("L01", 10.001m) },
            ["D-NULL"] = new() { P("L01", -3m) },
        };

        var r = StockCheckService.BuildBelowReorder(articles, stock);

        r.Select(i => i.ArticleNumber).Should().Equal("A-GLEICH", "B-OHNE");
        r[0].CurrentStock.Should().Be(10m);
        r[0].StorageLocations.Should().Equal("L02", "L01");
        r[1].CurrentStock.Should().Be(0m);
        r[1].StorageLocations.Should().BeEmpty();
        r[1].Description.Should().Be("ohne Bewegung");
    }
}
