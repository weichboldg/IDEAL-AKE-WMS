using FluentAssertions;
using IDEALAKEWMSService.Services;
using Xunit;

namespace IDEALAKEWMSService.Tests.Services;

/// <summary>
/// Arbeitsplatz-Quelle (Spec 2026-10-14-ideal-sage-views-umstellung, v1.60.2): Tabelle = bisheriges SQL zeichengleich,
/// View = deren Spalten (Arbeitsgangnummer, Bezeichnung, AG) ohne Mandant/Aktiv. Der echte Read ist Manual-UAT (TS-93.3b).
/// </summary>
public class SageArbeitsplatzSqlTests
{
    [Theory]
    [InlineData("[KHKPpsArbeitsplaetze]")]
    [InlineData("[dbo].[KHKPpsArbeitsplaetze]")]
    [InlineData("[khkppsarbeitsplaetze]")]
    public void Tabelle_LiestDasBisherigeSqlZeichengleich(string quoted)
    {
        SageArbeitsplatzSql.IsTable(quoted).Should().BeTrue();
        // Dauerhafter Waechter: exakt der Select vor v1.60.0 (SageArbeitsplatzReader, Stand d4108f1a), LF-normalisiert.
        SageArbeitsplatzSql.Build(quoted).Replace("\r\n", "\n").Should().Be(
            "SELECT Arbeitsplatznummer, Bezeichnung1, USER_ArbeitsSchritt\n" +
            "FROM KHKPpsArbeitsplaetze\n" +
            "WHERE Mandant = @mandant\n" +
            "  AND Aktiv = -1");
    }

    [Theory]
    [InlineData("[vw_IDEAL_AKE_WMS_Arbeitsgaenge]")]
    [InlineData("[dbo].[vw_IDEAL_AKE_WMS_Arbeitsgaenge]")]
    [InlineData("[KHKPpsArbeitsplaetze_Kopie]")]
    public void View_LiestArbeitsgangnummerBezeichnungAg_OhneMandantAktiv(string quoted)
    {
        SageArbeitsplatzSql.IsTable(quoted).Should().BeFalse();
        var sql = SageArbeitsplatzSql.Build(quoted);

        sql.Should().Contain($"FROM {quoted}")
            .And.Contain("SELECT Arbeitsgangnummer, Bezeichnung, AG")
            .And.NotContain("Mandant").And.NotContain("Aktiv").And.NotContain("@mandant")
            .And.NotContain("Arbeitsplatznummer").And.NotContain("Bezeichnung1").And.NotContain("USER_ArbeitsSchritt");
    }
}
