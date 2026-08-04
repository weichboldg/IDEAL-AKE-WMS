using FluentAssertions;
using IdealAkeWms.Services;

namespace IdealAkeWms.Tests.Services;

public class SdataUrlBuilderTests
{
    private const string Base = "https://sagetest01.ake.at:5493";
    private const string App = "ol";
    private const string Contract = "CommonWawiServices";
    private const string Dataset = "ake_TEST2026;1";

    [Fact]
    public void BuildLagerbuchungUrl_MatchesDiscoveredTarget()
    {
        SdataUrlBuilder.BuildLagerbuchungUrl(Base, App, Contract, Dataset)
            .Should().Be("https://sagetest01.ake.at:5493/sdata/ol/CommonWawiServices/ake_TEST2026;1/$service/LagerbuchungService");
    }

    [Fact]
    public void BuildResourceUrl_Schema_KeepsDollarAndSemicolonLiteral()
    {
        var url = SdataUrlBuilder.BuildResourceUrl(Base, App, Contract, Dataset, "$schema");
        url.Should().Be("https://sagetest01.ake.at:5493/sdata/ol/CommonWawiServices/ake_TEST2026;1/$schema");
        url.Should().NotContain("%24").And.NotContain("%3B");
    }

    [Fact]
    public void BuildResourceUrl_QueryResource_AppendedLiteral()
    {
        SdataUrlBuilder.BuildResourceUrl(Base, App, Contract, Dataset, "Adressen?count=1&format=application/json")
            .Should().Be("https://sagetest01.ake.at:5493/sdata/ol/CommonWawiServices/ake_TEST2026;1/Adressen?count=1&format=application/json");
    }

    [Theory]
    [InlineData("https://sagetest01.ake.at:5493")]
    [InlineData("https://sagetest01.ake.at:5493/")]
    [InlineData("https://sagetest01.ake.at:5493/sdata")]
    [InlineData("https://sagetest01.ake.at:5493/sdata/")]
    public void BuildResourceUrl_DoesNotDoubleSdataRoot(string baseUrl)
    {
        SdataUrlBuilder.BuildResourceUrl(baseUrl, App, Contract, Dataset, "$schema")
            .Should().Be("https://sagetest01.ake.at:5493/sdata/ol/CommonWawiServices/ake_TEST2026;1/$schema");
    }

    [Fact]
    public void BuildResourceUrl_TrimsLeadingSlashOnResource()
    {
        SdataUrlBuilder.BuildResourceUrl(Base, App, Contract, Dataset, "/$schema")
            .Should().EndWith("/ake_TEST2026;1/$schema");
    }
}
