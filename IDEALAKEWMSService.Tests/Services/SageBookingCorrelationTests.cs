using FluentAssertions;
using IDEALAKEWMSService.Services;

namespace IDEALAKEWMSService.Tests.Services;

public class SageBookingCorrelationTests
{
    [Fact]
    public void Memo_ContainsOwnMarker()
    {
        SageBookingCorrelation.Memo(SageBookingCorrelation.Marker(42)).Should().Be("IdealAkeWms Lagerbuchung SM#42#");
    }

    [Fact]
    public void Marker_HasNoPrefixCollision()
    {
        // Der abschliessende '#' verhindert, dass der Marker einer Id Praefix des Markers einer
        // laengeren Id ist — sonst wuerde der Sage-Lookup fuer #12 faelschlich #123 treffen.
        SageBookingCorrelation.Memo(SageBookingCorrelation.Marker(123)).Should().NotContain(SageBookingCorrelation.Marker(12));
        SageBookingCorrelation.Marker(123).Should().NotStartWith(SageBookingCorrelation.Marker(12));
    }

    [Fact]
    public void LikePattern_WrapsMarkerWithWildcards_NoLikeMetacharacters()
    {
        var pattern = SageBookingCorrelation.LikePattern(SageBookingCorrelation.Marker(7));
        pattern.Should().Be("%SM#7#%");
        pattern.Should().NotContainAny("[", "]", "_");
    }

    // v1.99.9 System-Kuerzel (Freigabe-Nachtrag 14:20, Punkt 4)
    [Fact]
    public void Marker_MitKuerzel_UndKennungDesQueueEintrags()
    {
        SageBookingCorrelation.Marker(7, "DEV").Should().Be("SM#DEV#7#");
        SageBookingCorrelation.Marker(7, "  ").Should().Be("SM#7#");
        var nieGesendet = new IdealAkeWms.Models.SageBookingQueueItem { StockMovementId = 7 };
        SageBookingCorrelation.Kennung(nieGesendet, "DEV").Should().Be("SM#DEV#7#");
        var altGesendet = new IdealAkeWms.Models.SageBookingQueueItem { StockMovementId = 7, SentAt = DateTime.Now };
        SageBookingCorrelation.Kennung(altGesendet, "DEV").Should().Be("SM#7#", "vor v1.99.9 gesendet = altes Format");
        altGesendet.SageKennung = "SM#AT#7#";
        SageBookingCorrelation.Kennung(altGesendet, "DEV").Should().Be("SM#AT#7#", "gespeicherte Kennung geht vor");
    }

    [Theory]
    [InlineData("SM#7#", "IdealAkeWms Lagerbuchung SM#DEV#7#", false)]
    [InlineData("SM#DEV#7#", "IdealAkeWms Lagerbuchung SM#7#", false)]
    [InlineData("SM#AT#7#", "IdealAkeWms Lagerbuchung SM#AT#17#", false)]
    [InlineData("SM#AT#7#", "IdealAkeWms Lagerbuchung SM#ATI#7#", false)]
    [InlineData("SM#DEV#7#", "IdealAkeWms Lagerbuchung SM#DEV#7#", true)]
    public void LikePattern_MitKuerzel_TrifftNurEigeneKennung(string marker, string memoInSage, bool trifft)
    {
        SageBookingCorrelation.LikePattern(marker).Should().Be($"%{marker}%");
        memoInSage.Contains(marker).Should().Be(trifft);
    }
}
