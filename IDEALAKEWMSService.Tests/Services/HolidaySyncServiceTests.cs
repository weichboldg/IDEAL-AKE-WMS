using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using IDEALAKEWMSService.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;
using Xunit;

namespace IDEALAKEWMSService.Tests.Services;

public class HolidaySyncServiceTests
{
    private record TestSettings(bool Enabled = true, string Country = "AT", string Region = "", int Years = 1, bool DryRun = false);

    private static (ApplicationDbContext ctx, HolidaySyncService svc) Setup(
        TestSettings testSettings, IEnumerable<NagerHoliday>[] perYearResponses)
    {
        var ctx = TestDbContextFactory.Create();

        var handler = new Mock<HttpMessageHandler>();
        var responseQueue = new Queue<IEnumerable<NagerHoliday>>(perYearResponses);
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                var json = JsonSerializer.Serialize(responseQueue.Dequeue());
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                };
            });

        var http = new HttpClient(handler.Object) { BaseAddress = new Uri("https://date.nager.at/") };
        var options = Options.Create(new HolidaySyncOptions
        {
            Enabled = testSettings.Enabled,
            CountryCode = testSettings.Country,
            Region = testSettings.Region,
            JahreVoraus = testSettings.Years,
            DryRun = testSettings.DryRun
        });

        var svc = new HolidaySyncService(ctx, http, options, NullLogger<HolidaySyncService>.Instance, new FakeSyncLogger());
        return (ctx, svc);
    }

    private static NagerHoliday Holiday(string date, string name, string[]? counties = null) =>
        new(date, name, name, "AT", true, counties == null, counties, null, new[] { "Public" });

    [Fact]
    public async Task Run_SyncDisabled_NoOps()
    {
        var (ctx, svc) = Setup(new TestSettings(Enabled: false), Array.Empty<IEnumerable<NagerHoliday>>());

        var result = await svc.RunAsync(CancellationToken.None);

        result.FetchedCount.Should().Be(0);
        result.InsertedCount.Should().Be(0);
        ctx.Holidays.Count().Should().Be(0);
    }

    [Fact]
    public async Task Run_FetchesCurrentAndForwardYears()
    {
        var year = DateTime.Today.Year;
        var (ctx, svc) = Setup(new TestSettings(Years: 2), new[] {
            new[] { Holiday($"{year}-01-01", "Neujahr") }.AsEnumerable(),
            new[] { Holiday($"{year+1}-01-01", "Neujahr") }.AsEnumerable(),
            new[] { Holiday($"{year+2}-01-01", "Neujahr") }.AsEnumerable()
        });

        var result = await svc.RunAsync(CancellationToken.None);

        result.FetchedCount.Should().Be(3);
        result.InsertedCount.Should().Be(3);
    }

    [Fact]
    public async Task Run_InsertsOnlyMissingDates()
    {
        var year = DateTime.Today.Year;
        var existingDate = new DateTime(year, 1, 1);
        var (ctx, svc) = Setup(new TestSettings(Years: 0), new[] {
            new[] {
                Holiday($"{year}-01-01", "Neujahr"),
                Holiday($"{year}-01-06", "Heilige Drei Koenige")
            }.AsEnumerable()
        });
        ctx.Holidays.Add(new Holiday {
            Date = existingDate, Description = "Manual", Source = HolidaySource.Manual,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        await ctx.SaveChangesAsync();

        var result = await svc.RunAsync(CancellationToken.None);

        result.InsertedCount.Should().Be(1);
        ctx.Holidays.Count().Should().Be(2);
        ctx.Holidays.First(h => h.Date == existingDate).Source.Should().Be(HolidaySource.Manual);
    }

    [Fact]
    public async Task Run_PreservesManualEntries()
    {
        var year = DateTime.Today.Year;
        var (ctx, svc) = Setup(new TestSettings(Years: 0), new[] {
            new[] { Holiday($"{year}-05-01", "Tag der Arbeit") }.AsEnumerable()
        });
        ctx.Holidays.Add(new Holiday {
            Date = new DateTime(year, 5, 1), Description = "Manueller Eintrag", Source = HolidaySource.Manual,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        await ctx.SaveChangesAsync();

        await svc.RunAsync(CancellationToken.None);

        var entry = ctx.Holidays.First(h => h.Date == new DateTime(year, 5, 1));
        entry.Source.Should().Be(HolidaySource.Manual);
        entry.Description.Should().Be("Manueller Eintrag");
    }

    [Fact]
    public async Task Run_WithRegion_FiltersCounties()
    {
        var year = DateTime.Today.Year;
        var (ctx, svc) = Setup(new TestSettings(Region: "AT-3", Years: 0), new[] {
            new[] {
                Holiday($"{year}-01-01", "Neujahr"),                                       // global -> enthalten
                Holiday($"{year}-09-15", "NÖ Landesfeiertag", counties: new[] { "AT-3" }), // matched
                Holiday($"{year}-11-19", "Tirol Fest",        counties: new[] { "AT-7" }), // skip
            }.AsEnumerable()
        });

        var result = await svc.RunAsync(CancellationToken.None);

        result.InsertedCount.Should().Be(2);
        ctx.Holidays.Any(h => h.Description!.Contains("Neujahr")).Should().BeTrue();
        ctx.Holidays.Any(h => h.Description!.Contains("NÖ")).Should().BeTrue();
        ctx.Holidays.Any(h => h.Description!.Contains("Tirol")).Should().BeFalse();
    }

    [Fact]
    public async Task Run_DryRun_NoInserts()
    {
        var year = DateTime.Today.Year;
        var (ctx, svc) = Setup(new TestSettings(DryRun: true, Years: 0), new[] {
            new[] { Holiday($"{year}-01-01", "Neujahr") }.AsEnumerable()
        });

        var result = await svc.RunAsync(CancellationToken.None);

        result.FetchedCount.Should().Be(1);
        result.InsertedCount.Should().Be(0);
        ctx.Holidays.Count().Should().Be(0);
    }

    [Fact]
    public async Task Run_SetsSourceNagerSync()
    {
        var year = DateTime.Today.Year;
        var (ctx, svc) = Setup(new TestSettings(Years: 0), new[] {
            new[] { Holiday($"{year}-01-01", "Neujahr") }.AsEnumerable()
        });

        await svc.RunAsync(CancellationToken.None);

        ctx.Holidays.First().Source.Should().Be(HolidaySource.NagerSync);
    }

    [Fact]
    public async Task Run_ApiReturnsBadStatus_LogsError()
    {
        var ctx = TestDbContextFactory.Create();
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var http = new HttpClient(handler.Object) { BaseAddress = new Uri("https://date.nager.at/") };
        var options = Options.Create(new HolidaySyncOptions { Enabled = true, CountryCode = "AT", JahreVoraus = 0 });

        var svc = new HolidaySyncService(ctx, http, options, NullLogger<HolidaySyncService>.Instance, new FakeSyncLogger());

        var result = await svc.RunAsync(CancellationToken.None);

        result.Errors.Should().NotBeEmpty();
        result.InsertedCount.Should().Be(0);
    }

    [Fact]
    public async Task Run_FirstYearThrows_SecondYearStillProcessed_NoCrossContamination()
    {
        var ctx = TestDbContextFactory.Create();
        var year = DateTime.Today.Year;

        // First call (year N) returns 503, second call (year N+1) returns 200 with a holiday.
        var responses = new Queue<HttpResponseMessage>();
        responses.Enqueue(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        responses.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new[] { Holiday($"{year + 1}-01-01", "Neujahr") }),
                System.Text.Encoding.UTF8, "application/json")
        });

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => responses.Dequeue());

        var http = new HttpClient(handler.Object) { BaseAddress = new Uri("https://date.nager.at/") };
        var options = Options.Create(new HolidaySyncOptions { Enabled = true, CountryCode = "AT", JahreVoraus = 1 });

        var svc = new HolidaySyncService(ctx, http, options, NullLogger<HolidaySyncService>.Instance, new FakeSyncLogger());

        var result = await svc.RunAsync(CancellationToken.None);

        result.Errors.Should().HaveCount(1, "year N failed");
        result.Errors[0].Should().Contain($"Year {year}");
        result.InsertedCount.Should().Be(1, "year N+1 succeeded and inserted Neujahr");
        ctx.Holidays.Should().HaveCount(1);
        ctx.Holidays.First().Date.Should().Be(new DateTime(year + 1, 1, 1));
    }

    [Fact]
    public async Task RunAsync_writes_lifecycle_to_synclogger()
    {
        var year = DateTime.Today.Year;
        var ctx = TestDbContextFactory.Create();
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                var json = JsonSerializer.Serialize(new[] { Holiday($"{year}-01-01", "Neujahr") });
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                };
            });

        var http = new HttpClient(handler.Object) { BaseAddress = new Uri("https://date.nager.at/") };
        var options = Options.Create(new HolidaySyncOptions { Enabled = true, CountryCode = "AT", JahreVoraus = 0 });
        var fakeLogger = new FakeSyncLogger();
        var service = new HolidaySyncService(ctx, http, options, NullLogger<HolidaySyncService>.Instance, fakeLogger);

        await service.RunAsync(CancellationToken.None);

        fakeLogger.Runs.Should().HaveCount(1);
        fakeLogger.Runs[0].ServiceName.Should().Be("Holiday");
        fakeLogger.Runs[0].FinishedSuccess.Should().BeTrue();
        fakeLogger.Runs[0].FinalCounts.Should().NotBeNull();
        fakeLogger.Runs[0].FinalCounts!.Should().ContainKey("importiert");
    }
}
