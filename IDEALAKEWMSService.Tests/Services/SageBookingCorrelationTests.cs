using FluentAssertions;
using IDEALAKEWMSService.Services;

namespace IDEALAKEWMSService.Tests.Services;

public class SageBookingCorrelationTests
{
    [Fact]
    public void Memo_ContainsOwnMarker()
    {
        SageBookingCorrelation.Memo(42).Should().Contain(SageBookingCorrelation.Marker(42));
    }

    [Fact]
    public void Marker_HasNoPrefixCollision()
    {
        // Der abschliessende '#' verhindert, dass der Marker einer Id Praefix des Markers einer
        // laengeren Id ist — sonst wuerde der Sage-Lookup fuer #12 faelschlich #123 treffen.
        SageBookingCorrelation.Memo(123).Should().NotContain(SageBookingCorrelation.Marker(12));
        SageBookingCorrelation.Marker(123).Should().NotStartWith(SageBookingCorrelation.Marker(12));
    }

    [Fact]
    public void LikePattern_WrapsMarkerWithWildcards_NoLikeMetacharacters()
    {
        var pattern = SageBookingCorrelation.LikePattern(7);
        pattern.Should().Be("%SM#7#%");
        pattern.Should().NotContainAny("[", "]", "_");
    }
}
