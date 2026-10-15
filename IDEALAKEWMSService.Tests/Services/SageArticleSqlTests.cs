using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using IDEALAKEWMSService.Services;
using Xunit;

namespace IDEALAKEWMSService.Tests.Services;

/// <summary>
/// Artikel-Sync-Quelle (Spec 2026-10-14-ideal-sage-views-umstellung, AK 10/11): leer = bisheriger Select
/// zeichengleich, gesetzt = View-Select mit denselben Aliasen (Reader liest per Ordinal). Der echte Sage-Read ist
/// Manual-UAT (TS-93).
/// </summary>
public class SageArticleSqlTests
{
    [Fact]
    public void Select_OhneView_IstDerBisherigeSelect()
    {
        SageArticleSql.Select(null).Should().BeSameAs(SageArticleSql.LegacySql);
        SageArticleSql.LegacySql.Should().StartWith("WITH RawArticles AS (")
            .And.Contain("FROM [dbo].[KHKPpsRessourcenPositionen] r")
            .And.Contain("WHERE a.IstBestellartikel = -1 AND a.Aktiv = -1")
            .And.EndWith("GROUP BY ArticleNumber");
    }

    /// <summary>Dauerhafter Waechter (QA-Minor M-7 v1.60.0): LegacySql ist zeichengleich mit dem Literal, das bis
    /// v1.59.0 in SageImportService.SyncArticlesAsync stand (Stand d4108f1a). Hash ueber den Text mit LF-Zeilenenden
    /// (Checkout-unabhaengig). Wer den alten Select aendert, aendert AKE — dann bewusst hier nachziehen.</summary>
    [Fact]
    public void LegacySql_IstZeichengleichMitDemBisherigenSelect()
    {
        var text = SageArticleSql.LegacySql.Replace("\r\n", "\n");

        text.Length.Should().Be(1940);
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant()
            .Should().Be("8b97486eaa71ab616624be873bdedd9cf2aacb10716f09439071349ac30c5058");
    }

    [Fact]
    public void Select_MitView_LiestDieViewMitGleichenAliasenInGleicherReihenfolge()
    {
        var sql = SageArticleSql.Select("[vw_IDEAL_AKE_WMS_Artikel]");

        sql.Should().Contain("FROM [vw_IDEAL_AKE_WMS_Artikel]")
            .And.Contain("GROUP BY CAST(ArticleNumber AS nvarchar(100))")
            .And.NotContain("KHK");
        // P4 (Gerald 2026-10-15): die View liefert die WMS-Namen — keine Sage-Spaltennamen im View-Select.
        foreach (var col in new[] { "ArticleNumber", "Description", "Unit", "ArticleGroup", "ReorderLevel", "PrimaryStorageLocation", "Matchcode" })
            sql.Should().Contain($"CAST({col} AS nvarchar(");
        foreach (var sage in new[] { "Artikelnummer", "Bezeichnung1", "Lagermengeneinheit", "Artikelgruppe", "Meldebestand", "Kurzbezeichnung" })
            sql.Should().NotContain(sage);
        var aliases = new[] { "ArticleNumber", "Description", "Unit", "ArticleGroup", "ReorderLevel", "PrimaryStorageLocation", "Matchcode" };
        var positions = aliases.Select(a => sql.IndexOf("AS " + a, StringComparison.Ordinal)).ToList();
        positions.Should().OnlyContain(p => p > 0).And.BeInAscendingOrder();
    }
}
