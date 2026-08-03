using FluentAssertions;
using IDEALAKEWMSService.Services;

namespace IDEALAKEWMSService.Tests.Services;

public class SageLagerbuchungClientUrlTests
{
    private static SageBookingEndpoint DiscoveredEndpoint() => new(
        BaseUrl: "https://sagetest01.ake.at:5493",
        Application: "ol",
        ServiceContract: "CommonWawiServices",
        Dataset: "ake_TEST2026;1",
        Username: "u",
        Password: "p");

    // Die per SData-Discovery ermittelte Ziel-URL — literales Semikolon, literales '$'.
    private const string ExpectedUrl =
        "https://sagetest01.ake.at:5493/sdata/ol/CommonWawiServices/ake_TEST2026;1/$service/LagerbuchungService";

    [Fact]
    public void BuildServiceUrl_ProducesDiscoveredTargetUrl_Exactly()
    {
        var url = SageLagerbuchungClient.BuildServiceUrl(DiscoveredEndpoint());
        url.Should().Be(ExpectedUrl);
    }

    [Fact]
    public void BuildServiceUrl_KeepsSemicolonAndDollar_Literal_NotPercentEncoded()
    {
        var url = SageLagerbuchungClient.BuildServiceUrl(DiscoveredEndpoint());
        url.Should().Contain("ake_TEST2026;1");   // ';' literal
        url.Should().Contain("/$service/");        // '$' literal
        url.Should().NotContain("%3B");            // ';' NICHT kodiert
        url.Should().NotContain("%24");            // '$' NICHT kodiert
    }

    [Fact]
    public void BuiltUrl_SurvivesUriPipeline_WithoutEscaping()
    {
        // Beweis, dass die .NET-Uri-Pipeline (die HttpRequestMessage intern nutzt) ';' und '$'
        // im Pfad literal laesst — sonst waere die Buchung gegen den falschen Endpunkt gegangen.
        var url = SageLagerbuchungClient.BuildServiceUrl(DiscoveredEndpoint());
        var uri = new Uri(url, UriKind.Absolute);

        uri.AbsoluteUri.Should().Be(ExpectedUrl);
        uri.AbsoluteUri.Should().NotContain("%3B").And.NotContain("%24");
        uri.PathAndQuery.Should().Contain("ake_TEST2026;1").And.Contain("/$service/");
    }

    [Fact]
    public void BuildServiceUrl_TrimsStraySlashes_BetweenSegments()
    {
        var endpoint = DiscoveredEndpoint() with { BaseUrl = "https://sagetest01.ake.at:5493/", Dataset = "/ake_TEST2026;1/" };
        SageLagerbuchungClient.BuildServiceUrl(endpoint).Should().Be(ExpectedUrl);
    }

    [Theory]
    [InlineData("https://sagetest01.ake.at:5493")]        // ohne /sdata (Soll)
    [InlineData("https://sagetest01.ake.at:5493/")]       // Trailing-Slash
    [InlineData("https://sagetest01.ake.at:5493/sdata")]  // Admin hat /sdata mit reingeschrieben
    [InlineData("https://sagetest01.ake.at:5493/sdata/")] // dito + Trailing-Slash
    [InlineData("https://sagetest01.ake.at:5493/SData")]  // Case-insensitiv
    public void BuildServiceUrl_DoesNotDoubleSdataRoot(string baseUrl)
    {
        var endpoint = DiscoveredEndpoint() with { BaseUrl = baseUrl };
        SageLagerbuchungClient.BuildServiceUrl(endpoint).Should().Be(ExpectedUrl);
    }
}
