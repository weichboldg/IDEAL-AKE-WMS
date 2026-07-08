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
    public async Task SyncWorker_DryRunDefaultsFalse_WhenDbUnreachable()
    {
        // Seit v1.25.0 (Task 5) liest der Worker SyncDryRun DB-first (ServiceSettings).
        // Der "DryRun=true"-Pfad ist damit GUI-/DB-gesteuert und nur Manual-UAT.
        // Unit-testbar bleibt der resiliente Default: ohne erreichbare DB faellt DryRun
        // auf false (der dokumentierte Default) → die Syncs laufen mit dryRun=false.
        var (sageImport, scopeFactory) = CreateScopeFactoryMock();
        var config = BuildConfig(new()
        {
            ["Sync:ProductionOrdersEnabled"] = "true",
            ["Sync:ArticlesEnabled"] = "true",
        });

        using var worker = new SyncWorker(Mock.Of<ILogger<SyncWorker>>(), config, scopeFactory.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        await worker.StartAsync(cts.Token);
        await Task.Delay(150);
        await worker.StopAsync(CancellationToken.None);

        sageImport.Verify(x =>
            x.SyncProductionOrdersAsync(false, It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
        sageImport.Verify(x =>
            x.SyncArticlesAsync(false, It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task SyncWorker_ContinuesAfterServiceException()
    {
        // Seit v1.25.0 (Task 5) liest der Worker SyncIntervalMinutes DB-first
        // (ServiceSettings). Ohne DefaultConnection faellt das Intervall auf 15 Min
        // zurueck → der Loop iteriert im Testfenster nur einmal, ein call-count>1 ueber
        // mehrere Iterationen ist nicht mehr beobachtbar (Manual-UAT).
        // Statt der Loop-Wiederholung sichern wir hier den eigentlichen Vertrag ab:
        // Eine Exception aus einem Einzel-Sync wird von RunResilientAsync geschluckt
        // und darf den Worker NICHT abstuerzen lassen (kein Propagieren, sauberer Stop).
        var (sageImport, scopeFactory) = CreateScopeFactoryMock();

        sageImport.Setup(x => x.SyncProductionOrdersAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Simulierter DB-Fehler"));

        var config = BuildConfig(new()
        {
            ["Sync:ProductionOrdersEnabled"] = "true",
            ["Sync:ArticlesEnabled"] = "false",
        });

        using var worker = new SyncWorker(Mock.Of<ILogger<SyncWorker>>(), config, scopeFactory.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        var run = async () =>
        {
            await worker.StartAsync(cts.Token);
            await Task.Delay(150);
            await worker.StopAsync(CancellationToken.None);
        };

        // RunResilientAsync fängt die Exception ab → der Worker läuft weiter / stoppt sauber.
        await run.Should().NotThrowAsync();
        sageImport.Verify(x =>
            x.SyncProductionOrdersAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
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
