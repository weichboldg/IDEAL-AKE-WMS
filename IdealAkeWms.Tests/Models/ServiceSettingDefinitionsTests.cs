using System.Linq;
using FluentAssertions;
using IdealAkeWms.Models;

namespace IdealAkeWms.Tests.Models;

public class ServiceSettingDefinitionsTests
{
    [Fact]
    public void All_HasNoDuplicateKeys()
    {
        var keys = ServiceSettingDefinitions.All.Select(d => d.Key).ToList();
        keys.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void All_BoolDefaults_AreTrueOrFalse()
    {
        foreach (var def in ServiceSettingDefinitions.All.Where(d => d.Type == ServiceSettingType.Bool))
        {
            def.DefaultValue.Should().BeOneOf(new[] { "true", "false" },
                $"{def.Key} ist Bool und muss 'true'/'false' als Default haben");
        }
    }

    [Fact]
    public void All_IntDefaults_ParseAsInt()
    {
        foreach (var def in ServiceSettingDefinitions.All.Where(d => d.Type == ServiceSettingType.Int))
        {
            int.TryParse(def.DefaultValue, out _).Should().BeTrue(
                because: $"{def.Key} ist Int und muss einen int-parsebaren Default haben (war '{def.DefaultValue}')");
        }
    }

    [Fact]
    public void All_CategoryAndDescription_AreNotEmpty()
    {
        foreach (var def in ServiceSettingDefinitions.All)
        {
            def.Category.Should().NotBeNullOrWhiteSpace(because: $"{def.Key} braucht eine Kategorie");
            def.Description.Should().NotBeNullOrWhiteSpace(because: $"{def.Key} braucht eine Beschreibung");
        }
    }

    [Fact]
    public void TryGet_ExistingKey_ReturnsDefinition()
    {
        ServiceSettingDefinitions.TryGet("Sync:BomCacheEnabled", out var def).Should().BeTrue();
        def!.Type.Should().Be(ServiceSettingType.Bool);
        def.DefaultValue.Should().Be("false");
    }

    [Fact]
    public void TryGet_UnknownKey_ReturnsFalse()
    {
        ServiceSettingDefinitions.TryGet("Does:NotExist", out var def).Should().BeFalse();
        def.Should().BeNull();
    }

    // Drift-Guard: JEDER dokumentierte, service-gelesene Key MUSS im Katalog sein.
    // Diese feste Liste stammt aus dem Grep aller ServiceSettings-Reads im Service
    // (Common/ServiceSettings.Get*Async + direkte [ServiceSettings]-Tabellen-Reads)
    // sowie den IConfiguration-Reads, die dieser Umbau DB-first stellt.
    [Theory]
    [InlineData("Sync:ProductionOrdersEnabled")]
    [InlineData("Sync:ArticlesEnabled")]
    [InlineData("Sync:OseonArticleCategoryEnabled")]
    [InlineData("Sync:OseonTrackingEnabled")]
    [InlineData("Sync:EnaioDmsEnabled")]
    [InlineData("Sync:PartRequisitionEmailEnabled")]
    [InlineData("Sync:WarehouseRequisitionEmailEnabled")]
    [InlineData("Sync:LagerplaetzeEnabled")]
    [InlineData("Sync:LagerbestandEnabled")]
    [InlineData("Sync:LagerbestandIntervalMinutes")]
    [InlineData("Sync:LagerbestandNullsetzenMaxPerRun")]
    [InlineData("Sync:ProductionOrderReconcileEnabled")]
    [InlineData("Sync:ReconcileMaxCancelPerRun")]
    [InlineData("Sync:FaZusatzinfoEnabled")]
    [InlineData("Sync:FaZusatzinfoAutoDoneMaxPerRun")]
    [InlineData("Sync:BomCacheEnabled")]
    [InlineData("Sync:BomCacheWeeks")]
    [InlineData("Sync:BomCacheMaxOrders")]
    [InlineData("Sync:BomCacheMaxAgeHours")]
    [InlineData("Sync:FaWorkStepDetectionEnabled")]
    [InlineData("Sync:CoatingDetectionEnabled")]
    [InlineData("Sync:BdeAutoPauseIntervalMinutes")]
    [InlineData("Sync:FeiertagSyncEnabled")]
    [InlineData("Sync:FeiertagCountryCode")]
    [InlineData("Sync:FeiertagRegion")]
    [InlineData("Sync:FeiertagJahreVoraus")]
    [InlineData("WorkerSettings:SyncIntervalMinutes")]
    [InlineData("WorkerSettings:NotificationCheckIntervalMinutes")]
    [InlineData("WorkerSettings:SyncDryRun")]
    [InlineData("ErrorNotification:Enabled")]
    [InlineData("ErrorNotification:Recipients")]
    [InlineData("Notifications:MeldebestandEnabled")]
    [InlineData("Notifications:MeldebestandSubject")]
    [InlineData("Notifications:Recipients")]
    [InlineData("Notifications:AppBaseUrl")]
    [InlineData("Cleanup:AktivitaetsprotokollAufbewahrungTage")]
    public void All_ContainsDocumentedServiceReadKey(string key)
    {
        ServiceSettingDefinitions.All.Select(d => d.Key).Should().Contain(key);
    }
}
