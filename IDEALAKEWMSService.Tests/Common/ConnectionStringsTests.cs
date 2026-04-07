using FluentAssertions;
using IDEALAKEWMSService.Common;
using Microsoft.Extensions.Configuration;

namespace IDEALAKEWMSService.Tests.Common;

public class ConnectionStringsTests
{
    private static IConfiguration Build(params (string Key, string Value)[] entries)
    {
        var dict = new Dictionary<string, string?>();
        foreach (var (k, v) in entries) dict[k] = v;
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    [Fact]
    public void GetRequired_ReturnsValue_WhenPresent()
    {
        var cfg = Build(("ConnectionStrings:DefaultConnection", "Server=A;Database=B"));
        ConnectionStrings.GetRequired(cfg, "DefaultConnection").Should().Be("Server=A;Database=B");
    }

    [Fact]
    public void GetRequired_Throws_WhenMissing()
    {
        var cfg = Build();
        var act = () => ConnectionStrings.GetRequired(cfg, "DefaultConnection");
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*DefaultConnection nicht konfiguriert*");
    }

    [Fact]
    public void NamedHelpers_ReturnRespectiveStrings()
    {
        var cfg = Build(
            ("ConnectionStrings:DefaultConnection", "wms"),
            ("ConnectionStrings:SageConnection", "sage"),
            ("ConnectionStrings:OseonConnection", "oseon"),
            ("ConnectionStrings:EnaioDmsConnection", "enaio"));

        ConnectionStrings.Wms(cfg).Should().Be("wms");
        ConnectionStrings.Sage(cfg).Should().Be("sage");
        ConnectionStrings.Oseon(cfg).Should().Be("oseon");
        ConnectionStrings.EnaioDms(cfg).Should().Be("enaio");
    }
}
