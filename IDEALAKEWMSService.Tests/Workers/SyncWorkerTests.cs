using FluentAssertions;
using IDEALAKEWMSService.Services;
using IDEALAKEWMSService.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace IDEALAKEWMSService.Tests.Workers;

public class SyncWorkerTests
{
    // Hilfsmethode: Mock-Infrastruktur für IServiceScopeFactory aufbauen
    private static (Mock<ISageImportService> sageImport, Mock<IServiceScopeFactory> scopeFactory)
        CreateScopeFactoryMock()
    {
        var mockSageImport = new Mock<ISageImportService>();
        mockSageImport.Setup(x => x.SyncProductionOrdersAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncResult(1, 2, 0));
        mockSageImport.Setup(x => x.SyncArticlesAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncResult(3, 0, 0));

        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider
            .Setup(x => x.GetService(typeof(ISageImportService)))
            .Returns(mockSageImport.Object);

        var mockScope = new Mock<IServiceScope>();
        mockScope.Setup(x => x.ServiceProvider).Returns(mockServiceProvider.Object);

        var mockScopeFactory = new Mock<IServiceScopeFactory>();
        mockScopeFactory.Setup(x => x.CreateScope()).Returns(mockScope.Object);

        return (mockSageImport, mockScopeFactory);
    }

    private static IConfiguration BuildConfig(Dictionary<string, string?> values)
        => new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    [Fact]
    public async Task SyncWorker_CallsProductionOrdersSync_WhenEnabled()
    {
        var (sageImport, scopeFactory) = CreateScopeFactoryMock();
        var config = BuildConfig(new()
        {
            ["WorkerSettings:SyncIntervalMinutes"] = "0",
            ["WorkerSettings:SyncDryRun"] = "false",
            ["Sync:ProductionOrdersEnabled"] = "true",
            ["Sync:ArticlesEnabled"] = "false",
        });

        using var worker = new SyncWorker(Mock.Of<ILogger<SyncWorker>>(), config, scopeFactory.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        await worker.StartAsync(cts.Token);
        await Task.Delay(150);
        await worker.StopAsync(CancellationToken.None);

        sageImport.Verify(x =>
            x.SyncProductionOrdersAsync(false, It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task SyncWorker_SkipsProductionOrdersSync_WhenDisabled()
    {
        var (sageImport, scopeFactory) = CreateScopeFactoryMock();
        var config = BuildConfig(new()
        {
            ["WorkerSettings:SyncIntervalMinutes"] = "0",
            ["WorkerSettings:SyncDryRun"] = "false",
            ["Sync:ProductionOrdersEnabled"] = "false",
            ["Sync:ArticlesEnabled"] = "false",
        });

        using var worker = new SyncWorker(Mock.Of<ILogger<SyncWorker>>(), config, scopeFactory.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        await worker.StartAsync(cts.Token);
        await Task.Delay(150);
        await worker.StopAsync(CancellationToken.None);

        sageImport.Verify(x =>
            x.SyncProductionOrdersAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SyncWorker_CallsArticlesSync_WhenEnabled()
    {
        var (sageImport, scopeFactory) = CreateScopeFactoryMock();
        var config = BuildConfig(new()
        {
            ["WorkerSettings:SyncIntervalMinutes"] = "0",
            ["WorkerSettings:SyncDryRun"] = "false",
            ["Sync:ProductionOrdersEnabled"] = "false",
            ["Sync:ArticlesEnabled"] = "true",
        });

        using var worker = new SyncWorker(Mock.Of<ILogger<SyncWorker>>(), config, scopeFactory.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        await worker.StartAsync(cts.Token);
        await Task.Delay(150);
        await worker.StopAsync(CancellationToken.None);

        sageImport.Verify(x =>
            x.SyncArticlesAsync(false, It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task SyncWorker_SkipsArticlesSync_WhenDisabled()
    {
        var (sageImport, scopeFactory) = CreateScopeFactoryMock();
        var config = BuildConfig(new()
        {
            ["WorkerSettings:SyncIntervalMinutes"] = "0",
            ["WorkerSettings:SyncDryRun"] = "false",
            ["Sync:ProductionOrdersEnabled"] = "false",
            ["Sync:ArticlesEnabled"] = "false",
        });

        using var worker = new SyncWorker(Mock.Of<ILogger<SyncWorker>>(), config, scopeFactory.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        await worker.StartAsync(cts.Token);
        await Task.Delay(150);
        await worker.StopAsync(CancellationToken.None);

        sageImport.Verify(x =>
            x.SyncArticlesAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SyncWorker_PassesDryRunTrue_WhenConfigured()
    {
        var (sageImport, scopeFactory) = CreateScopeFactoryMock();
        var config = BuildConfig(new()
        {
            ["WorkerSettings:SyncIntervalMinutes"] = "0",
            ["WorkerSettings:SyncDryRun"] = "true",
            ["Sync:ProductionOrdersEnabled"] = "true",
            ["Sync:ArticlesEnabled"] = "true",
        });

        using var worker = new SyncWorker(Mock.Of<ILogger<SyncWorker>>(), config, scopeFactory.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        await worker.StartAsync(cts.Token);
        await Task.Delay(150);
        await worker.StopAsync(CancellationToken.None);

        sageImport.Verify(x =>
            x.SyncProductionOrdersAsync(true, It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
        sageImport.Verify(x =>
            x.SyncArticlesAsync(true, It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task SyncWorker_ContinuesAfterServiceException()
    {
        var (sageImport, scopeFactory) = CreateScopeFactoryMock();

        // Erste Calls werfen Exception, danach normaler Rückgabewert
        var callCount = 0;
        sageImport.Setup(x => x.SyncProductionOrdersAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount == 1)
                    throw new InvalidOperationException("Simulierter DB-Fehler");
                return new SyncResult(0, 0, 0);
            });

        var config = BuildConfig(new()
        {
            ["WorkerSettings:SyncIntervalMinutes"] = "0",
            ["WorkerSettings:SyncDryRun"] = "false",
            ["Sync:ProductionOrdersEnabled"] = "true",
            ["Sync:ArticlesEnabled"] = "false",
        });

        using var worker = new SyncWorker(Mock.Of<ILogger<SyncWorker>>(), config, scopeFactory.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
        await worker.StartAsync(cts.Token);
        await Task.Delay(250);
        await worker.StopAsync(CancellationToken.None);

        // Muss nach der Exception weiterlaufen — mindestens 2 Aufrufe
        callCount.Should().BeGreaterThan(1);
    }

    [Fact]
    public async Task SyncWorker_BothSyncsEnabled_CallsBothServices()
    {
        var (sageImport, scopeFactory) = CreateScopeFactoryMock();
        var config = BuildConfig(new()
        {
            ["WorkerSettings:SyncIntervalMinutes"] = "0",
            ["WorkerSettings:SyncDryRun"] = "false",
            ["Sync:ProductionOrdersEnabled"] = "true",
            ["Sync:ArticlesEnabled"] = "true",
        });

        using var worker = new SyncWorker(Mock.Of<ILogger<SyncWorker>>(), config, scopeFactory.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        await worker.StartAsync(cts.Token);
        await Task.Delay(150);
        await worker.StopAsync(CancellationToken.None);

        sageImport.Verify(x =>
            x.SyncProductionOrdersAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
        sageImport.Verify(x =>
            x.SyncArticlesAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }
}
