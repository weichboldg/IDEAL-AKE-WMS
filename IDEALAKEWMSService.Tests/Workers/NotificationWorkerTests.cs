using FluentAssertions;
using IDEALAKEWMSService.Services;
using IDEALAKEWMSService.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace IDEALAKEWMSService.Tests.Workers;

public class NotificationWorkerTests
{
    // Hilfsmethode: Mock-Infrastruktur für Scope + Services aufbauen
    private static (Mock<IStockCheckService> stockCheck, Mock<IMailService> mailService, Mock<IServiceScopeFactory> scopeFactory)
        CreateScopeFactoryMock()
    {
        var mockStockCheck = new Mock<IStockCheckService>();
        var mockMailService = new Mock<IMailService>();

        mockMailService
            .Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider.Setup(x => x.GetService(typeof(IStockCheckService))).Returns(mockStockCheck.Object);
        mockServiceProvider.Setup(x => x.GetService(typeof(IMailService))).Returns(mockMailService.Object);

        var mockScope = new Mock<IServiceScope>();
        mockScope.Setup(x => x.ServiceProvider).Returns(mockServiceProvider.Object);

        var mockScopeFactory = new Mock<IServiceScopeFactory>();
        mockScopeFactory.Setup(x => x.CreateScope()).Returns(mockScope.Object);

        return (mockStockCheck, mockMailService, mockScopeFactory);
    }

    // Minimale Konfig ohne DefaultConnection → GetServiceSettingAsync gibt null zurück → enabled = true (Fallback)
    private static IConfiguration BuildConfig(int intervalMs = 0)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["WorkerSettings:NotificationCheckIntervalMinutes"] = "0",
            })
            .Build();

    [Fact]
    public async Task NotificationWorker_SendsMail_WhenArticlesBelowReorderLevel()
    {
        var (stockCheck, mailService, scopeFactory) = CreateScopeFactoryMock();

        var items = new List<StockBelowReorderItem>
        {
            new("ART001", "Testartikel 1", "Stk", 10m, 3m, ["Regal-A1", "Regal-A2"]),
        };
        stockCheck.Setup(x => x.GetArticlesBelowReorderLevelAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);
        stockCheck.Setup(x => x.GetNotificationRecipientsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(["test@example.com"]);

        using var worker = new NotificationWorker(Mock.Of<ILogger<NotificationWorker>>(), BuildConfig(), scopeFactory.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        await worker.StartAsync(cts.Token);
        await Task.Delay(150);
        await worker.StopAsync(CancellationToken.None);

        mailService.Verify(x =>
            x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task NotificationWorker_DoesNotSendMail_WhenNoArticlesBelowReorderLevel()
    {
        var (stockCheck, mailService, scopeFactory) = CreateScopeFactoryMock();

        stockCheck.Setup(x => x.GetArticlesBelowReorderLevelAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        stockCheck.Setup(x => x.GetNotificationRecipientsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(["test@example.com"]);

        using var worker = new NotificationWorker(Mock.Of<ILogger<NotificationWorker>>(), BuildConfig(), scopeFactory.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        await worker.StartAsync(cts.Token);
        await Task.Delay(150);
        await worker.StopAsync(CancellationToken.None);

        mailService.Verify(x =>
            x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task NotificationWorker_HtmlContainsArticleNumber()
    {
        var (stockCheck, mailService, scopeFactory) = CreateScopeFactoryMock();

        var items = new List<StockBelowReorderItem>
        {
            new("ART-99999", "Spezial-Artikel", "m²", 5m, 1m, ["Lager-B"]),
        };
        stockCheck.Setup(x => x.GetArticlesBelowReorderLevelAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);
        stockCheck.Setup(x => x.GetNotificationRecipientsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(["empfaenger@example.com"]);

        string? capturedHtml = null;
        mailService.Setup(x =>
            x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, IEnumerable<string>, string?, CancellationToken>((_, html, _, _, _) => capturedHtml = html)
            .Returns(Task.CompletedTask);

        using var worker = new NotificationWorker(Mock.Of<ILogger<NotificationWorker>>(), BuildConfig(), scopeFactory.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        await worker.StartAsync(cts.Token);
        await Task.Delay(150);
        await worker.StopAsync(CancellationToken.None);

        capturedHtml.Should().NotBeNull();
        capturedHtml.Should().Contain("ART-99999");
        capturedHtml.Should().Contain("Spezial-Artikel");
        capturedHtml.Should().Contain("Lager-B");
    }

    [Fact]
    public async Task NotificationWorker_HtmlContainsAkeCiColors()
    {
        var (stockCheck, mailService, scopeFactory) = CreateScopeFactoryMock();

        stockCheck.Setup(x => x.GetArticlesBelowReorderLevelAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new("ART001", null, null, 1m, 0m, [])]);
        stockCheck.Setup(x => x.GetNotificationRecipientsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(["x@y.com"]);

        string? capturedHtml = null;
        mailService.Setup(x =>
            x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, IEnumerable<string>, string?, CancellationToken>((_, html, _, _, _) => capturedHtml = html)
            .Returns(Task.CompletedTask);

        using var worker = new NotificationWorker(Mock.Of<ILogger<NotificationWorker>>(), BuildConfig(), scopeFactory.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        await worker.StartAsync(cts.Token);
        await Task.Delay(150);
        await worker.StopAsync(CancellationToken.None);

        capturedHtml.Should().Contain("#053153");  // AKE Primary (Header)
        capturedHtml.Should().Contain("#43A6E2");  // AKE Secondary
        capturedHtml.Should().Contain("IDEAL AKE WMS");
    }

    [Fact]
    public async Task NotificationWorker_ZeroStock_UsesRedColor()
    {
        var (stockCheck, mailService, scopeFactory) = CreateScopeFactoryMock();

        // Artikel mit Bestand 0 → rot (#dc3545)
        stockCheck.Setup(x => x.GetArticlesBelowReorderLevelAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new("ART-ZERO", "Nullbestand", "Stk", 5m, 0m, [])]);
        stockCheck.Setup(x => x.GetNotificationRecipientsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(["x@y.com"]);

        string? capturedHtml = null;
        mailService.Setup(x =>
            x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, IEnumerable<string>, string?, CancellationToken>((_, html, _, _, _) => capturedHtml = html)
            .Returns(Task.CompletedTask);

        using var worker = new NotificationWorker(Mock.Of<ILogger<NotificationWorker>>(), BuildConfig(), scopeFactory.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        await worker.StartAsync(cts.Token);
        await Task.Delay(150);
        await worker.StopAsync(CancellationToken.None);

        capturedHtml.Should().Contain("#dc3545");  // Rot = Nullbestand
    }

    [Fact]
    public async Task NotificationWorker_PositiveLowStock_UsesOrangeColor()
    {
        var (stockCheck, mailService, scopeFactory) = CreateScopeFactoryMock();

        // Artikel mit Bestand > 0 aber unter Meldebestand → orange (#E87A1E)
        stockCheck.Setup(x => x.GetArticlesBelowReorderLevelAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new("ART-LOW", "Niedrigbestand", "Stk", 10m, 3m, [])]);
        stockCheck.Setup(x => x.GetNotificationRecipientsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(["x@y.com"]);

        string? capturedHtml = null;
        mailService.Setup(x =>
            x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, IEnumerable<string>, string?, CancellationToken>((_, html, _, _, _) => capturedHtml = html)
            .Returns(Task.CompletedTask);

        using var worker = new NotificationWorker(Mock.Of<ILogger<NotificationWorker>>(), BuildConfig(), scopeFactory.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        await worker.StartAsync(cts.Token);
        await Task.Delay(150);
        await worker.StopAsync(CancellationToken.None);

        capturedHtml.Should().Contain("#E87A1E");  // Orange = Bestand > 0 aber unter Meldebestand
    }

    [Fact]
    public async Task NotificationWorker_MultipleItems_AllAppearsInHtml()
    {
        var (stockCheck, mailService, scopeFactory) = CreateScopeFactoryMock();

        var items = new List<StockBelowReorderItem>
        {
            new("ART-001", "Artikel Eins", "Stk", 10m, 2m, ["A1"]),
            new("ART-002", "Artikel Zwei", "m",   20m, 5m, ["B1"]),
            new("ART-003", "Artikel Drei", "kg",   5m, 0m, []),
        };
        stockCheck.Setup(x => x.GetArticlesBelowReorderLevelAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);
        stockCheck.Setup(x => x.GetNotificationRecipientsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(["a@b.com"]);

        string? capturedHtml = null;
        mailService.Setup(x =>
            x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, IEnumerable<string>, string?, CancellationToken>((_, html, _, _, _) => capturedHtml = html)
            .Returns(Task.CompletedTask);

        using var worker = new NotificationWorker(Mock.Of<ILogger<NotificationWorker>>(), BuildConfig(), scopeFactory.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        await worker.StartAsync(cts.Token);
        await Task.Delay(150);
        await worker.StopAsync(CancellationToken.None);

        capturedHtml.Should().Contain("ART-001").And.Contain("ART-002").And.Contain("ART-003");
        capturedHtml.Should().Contain("Artikel Eins").And.Contain("Artikel Zwei").And.Contain("Artikel Drei");
    }

    [Fact]
    public async Task NotificationWorker_SendsToCorrectRecipients()
    {
        var (stockCheck, mailService, scopeFactory) = CreateScopeFactoryMock();

        stockCheck.Setup(x => x.GetArticlesBelowReorderLevelAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new("ART001", null, null, 1m, 0m, [])]);

        var expectedRecipients = new List<string> { "user1@test.com", "user2@test.com" };
        stockCheck.Setup(x => x.GetNotificationRecipientsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedRecipients);

        IEnumerable<string>? capturedRecipients = null;
        mailService.Setup(x =>
            x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, IEnumerable<string>, string?, CancellationToken>((_, _, recipients, _, _) => capturedRecipients = recipients)
            .Returns(Task.CompletedTask);

        using var worker = new NotificationWorker(Mock.Of<ILogger<NotificationWorker>>(), BuildConfig(), scopeFactory.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        await worker.StartAsync(cts.Token);
        await Task.Delay(150);
        await worker.StopAsync(CancellationToken.None);

        capturedRecipients.Should().BeEquivalentTo(expectedRecipients);
    }

    [Fact]
    public async Task NotificationWorker_ContinuesAfterException()
    {
        var (stockCheck, mailService, scopeFactory) = CreateScopeFactoryMock();

        var callCount = 0;
        stockCheck.Setup(x => x.GetArticlesBelowReorderLevelAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount == 1)
                    throw new InvalidOperationException("Simulierter Fehler");
                return [];
            });

        using var worker = new NotificationWorker(Mock.Of<ILogger<NotificationWorker>>(), BuildConfig(), scopeFactory.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
        await worker.StartAsync(cts.Token);
        await Task.Delay(250);
        await worker.StopAsync(CancellationToken.None);

        // Nach der Exception muss der Worker weiterlaufen
        callCount.Should().BeGreaterThan(1);
    }

    [Fact]
    public async Task NotificationWorker_DefaultSubjectUsed_WhenNoDatabaseConnection()
    {
        var (stockCheck, mailService, scopeFactory) = CreateScopeFactoryMock();

        stockCheck.Setup(x => x.GetArticlesBelowReorderLevelAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new("ART001", null, null, 1m, 0m, [])]);
        stockCheck.Setup(x => x.GetNotificationRecipientsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(["x@y.com"]);

        string? capturedSubject = null;
        mailService.Setup(x =>
            x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, IEnumerable<string>, string?, CancellationToken>((subject, _, _, _, _) => capturedSubject = subject)
            .Returns(Task.CompletedTask);

        using var worker = new NotificationWorker(Mock.Of<ILogger<NotificationWorker>>(), BuildConfig(), scopeFactory.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        await worker.StartAsync(cts.Token);
        await Task.Delay(150);
        await worker.StopAsync(CancellationToken.None);

        // Kein DefaultConnection → Fallback-Betreff
        capturedSubject.Should().Be("Meldebestand unterschritten — IDEAL AKE WMS");
    }
}
