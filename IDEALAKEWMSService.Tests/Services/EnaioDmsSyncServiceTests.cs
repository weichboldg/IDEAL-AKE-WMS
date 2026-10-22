using FluentAssertions;
using IdealAkeWms.Tests.Helpers;
using IDEALAKEWMSService.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace IDEALAKEWMSService.Tests.Services;

public class EnaioDmsSyncServiceTests
{
    [Fact]
    public async Task SyncDocumentsAsync_writes_lifecycle_via_failure_path()
    {
        // EnaioDmsSyncService uses raw ADO.NET with EnaioDmsConnection.
        // Missing connection string causes InvalidOperationException inside the try block
        // (connection string guard is inside try) — catch triggers FinishFailedAsync.
        var config = new ConfigurationBuilder().Build(); // no connection strings
        var fakeLogger = new FakeSyncLogger();
        var service = new EnaioDmsSyncService(config, NullLogger<EnaioDmsSyncService>.Instance, fakeLogger);

        var act = async () => await service.SyncDocumentsAsync(dryRun: false, ct: CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
        fakeLogger.Runs.Should().ContainSingle();
        fakeLogger.Runs[0].ServiceName.Should().Be("EnaioDms");
        fakeLogger.Runs[0].FinishedFailed.Should().BeTrue();
    }

    // ----- v1.67.0: Quelle aus Sync:EnaioDmsViewName -----

    [Fact]
    public void DefaultViewName_gleich_Katalog_Default()
    {
        IdealAkeWms.Models.ServiceSettingDefinitions.All.Single(d => d.Key == "Sync:EnaioDmsViewName").DefaultValue
            .Should().Be(EnaioDmsSyncService.DefaultViewName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("[enaio].[dbo].[vw_IDEAL-AKE_Fertigungsauftraege]")]
    [InlineData("enaio.dbo.vw_IDEAL-AKE_Fertigungsauftraege")]
    public void ResolveSource_Default_und_dreiteiliger_Name_ergeben_das_bisherige_SQL(string? configured)
    {
        // Zeichengleich mit dem bis v1.66 fest codierten FROM-Bezeichner (Bindestrich + drei Teile).
        EnaioDmsSyncService.ResolveSource(configured).Should().Be("[enaio].[dbo].[vw_IDEAL-AKE_Fertigungsauftraege]");
    }

    [Theory]
    [InlineData("vw;DROP")]
    [InlineData("a.b.c.d")]          // mehr als drei Teile
    [InlineData("[vw--x]")]          // SQL-Kommentar-Einleiter
    [InlineData("vw_IDEАL")]         // kyrillisches 'А' (Homoglyph)
    [InlineData("vw Foo")]
    public void ResolveSource_ungueltiger_Name_wirft(string configured)
    {
        var act = () => EnaioDmsSyncService.ResolveSource(configured);
        act.Should().Throw<FaHierarchyViewNameException>();
    }

    [Fact]
    public void ValidateAndQuote_ohne_maxSegments_bleibt_bei_zwei_Teilen()
    {
        // Die Sage-Aufrufer uebergeben kein maxSegments - fuer sie bleibt ein dreiteiliger Name verboten.
        var act = () => FaHierarchySql.ValidateAndQuote("[enaio].[dbo].[vw_IDEAL-AKE_Fertigungsauftraege]");
        act.Should().Throw<FaHierarchyViewNameException>();
        FaHierarchySql.ValidateAndQuote("[enaio].[dbo].[vw_IDEAL-AKE_Fertigungsauftraege]", maxSegments: 3)
            .Should().Be("[enaio].[dbo].[vw_IDEAL-AKE_Fertigungsauftraege]");
    }
}
