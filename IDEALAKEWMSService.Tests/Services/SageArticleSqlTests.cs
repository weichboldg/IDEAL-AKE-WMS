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

    [Fact]
    public void Select_MitView_LiestDieViewMitGleichenAliasenInGleicherReihenfolge()
    {
        var sql = SageArticleSql.Select("[vw_IDEAL_AKE_WMS_Artikel]");

        sql.Should().Contain("FROM [vw_IDEAL_AKE_WMS_Artikel]")
            .And.Contain("GROUP BY CAST(Artikelnummer AS nvarchar(100))")
            .And.NotContain("KHK");
        var aliases = new[] { "ArticleNumber", "Description", "Unit", "ArticleGroup", "ReorderLevel", "PrimaryStorageLocation", "Matchcode" };
        var positions = aliases.Select(a => sql.IndexOf("AS " + a, StringComparison.Ordinal)).ToList();
        positions.Should().OnlyContain(p => p > 0).And.BeInAscendingOrder();
    }
}
