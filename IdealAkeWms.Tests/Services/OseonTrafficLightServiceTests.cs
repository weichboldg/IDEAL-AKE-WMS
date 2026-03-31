using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Services;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Services;

public class OseonTrafficLightServiceTests
{
    private readonly Mock<IAppSettingRepository> _settingsMock;
    private readonly OseonTrafficLightService _service;

    public OseonTrafficLightServiceTests()
    {
        _settingsMock = new Mock<IAppSettingRepository>();
        _settingsMock.Setup(s => s.GetIntValueAsync("OseonAmpelGelbTage", 1)).ReturnsAsync(1);
        _settingsMock.Setup(s => s.GetIntValueAsync("OseonAmpelBlauTage", 2)).ReturnsAsync(2);
        _service = new OseonTrafficLightService(_settingsMock.Object);
    }

    [Theory]
    [InlineData(90)]
    [InlineData(95)]
    public async Task GetColorAsync_FertigOrStorniert_ReturnsGreen(int status)
    {
        var result = await _service.GetColorAsync(status, DateTime.Today.AddDays(-10));
        result.Should().Be(TrafficLightColor.Green);
    }

    [Fact]
    public async Task GetColorAsync_NoDueDate_ReturnsGray()
    {
        var result = await _service.GetColorAsync(60, null);
        result.Should().Be(TrafficLightColor.Gray);
    }

    [Fact]
    public async Task GetColorAsync_Overdue_ReturnsRed()
    {
        var result = await _service.GetColorAsync(60, DateTime.Today.AddDays(-1));
        result.Should().Be(TrafficLightColor.Red);
    }

    [Fact]
    public async Task GetColorAsync_DueToday_ReturnsYellow()
    {
        var result = await _service.GetColorAsync(30, DateTime.Today);
        result.Should().Be(TrafficLightColor.Yellow);
    }

    [Fact]
    public async Task GetColorAsync_DueTomorrow_ReturnsYellow()
    {
        var result = await _service.GetColorAsync(30, DateTime.Today.AddDays(1));
        result.Should().Be(TrafficLightColor.Yellow);
    }

    [Fact]
    public async Task GetColorAsync_DueInTwoDays_ReturnsBlue()
    {
        var result = await _service.GetColorAsync(30, DateTime.Today.AddDays(2));
        result.Should().Be(TrafficLightColor.Blue);
    }

    [Fact]
    public async Task GetColorAsync_DueFarFuture_ReturnsGray()
    {
        var result = await _service.GetColorAsync(30, DateTime.Today.AddDays(30));
        result.Should().Be(TrafficLightColor.Gray);
    }

    [Fact]
    public async Task GetColorAsync_Status10_NoDueDate_ReturnsGray()
    {
        var result = await _service.GetColorAsync(10, null);
        result.Should().Be(TrafficLightColor.Gray);
    }

    [Fact]
    public async Task GetColorAsync_FertigOverrideDueDate_ReturnsGreen()
    {
        // Even if overdue, status 90 means Green
        var result = await _service.GetColorAsync(90, DateTime.Today.AddDays(-30));
        result.Should().Be(TrafficLightColor.Green);
    }

    // === GetColorForOperationAsync Tests ===

    [Fact]
    public async Task GetColorForOperationAsync_UsesCalculatedDueDate()
    {
        // Operation overdue by calculated date → Red
        var result = await _service.GetColorForOperationAsync(60, DateTime.Today.AddDays(-1));
        result.Should().Be(TrafficLightColor.Red);
    }

    [Fact]
    public async Task GetColorForOperationAsync_FertigStatus_AlwaysGreen()
    {
        var result = await _service.GetColorForOperationAsync(90, DateTime.Today.AddDays(-10));
        result.Should().Be(TrafficLightColor.Green);
    }

    [Fact]
    public async Task GetColorForOperationAsync_NoDueDate_Gray()
    {
        var result = await _service.GetColorForOperationAsync(60, null);
        result.Should().Be(TrafficLightColor.Gray);
    }
}
