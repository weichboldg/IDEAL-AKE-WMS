using AKEBDELight.Services;
using FluentAssertions;

namespace AKEBDELight.Tests.Services;

public class NaturalPositionComparerTests
{
    private readonly NaturalPositionComparer _sut = new();

    [Fact]
    public void Sort_NaturalNumericOrder()
    {
        var input = new List<string?> { "10", "2", "1", "10.1", "11", "15", "15.1" };
        input.Sort(_sut);
        input.Should().ContainInOrder("1", "2", "10", "10.1", "11", "15", "15.1");
    }

    [Fact]
    public void Sort_MultiLevel_Hierarchy()
    {
        var input = new List<string?> { "15.1.1", "15.1", "15", "2", "1", "10.2", "10.1", "10" };
        input.Sort(_sut);
        input.Should().ContainInOrder("1", "2", "10", "10.1", "10.2", "15", "15.1", "15.1.1");
    }

    [Fact]
    public void Compare_NullHandling()
    {
        _sut.Compare(null, "1").Should().Be(-1);
        _sut.Compare("1", null).Should().Be(1);
        _sut.Compare(null, null).Should().Be(0);
    }

    [Fact]
    public void Compare_EqualValues_ReturnsZero()
    {
        _sut.Compare("5", "5").Should().Be(0);
        _sut.Compare("5.1", "5.1").Should().Be(0);
    }

    [Fact]
    public void Compare_ParentBeforeChild()
    {
        _sut.Compare("15", "15.1").Should().BeLessThan(0);
        _sut.Compare("15.1", "15.1.1").Should().BeLessThan(0);
    }

    [Fact]
    public void Compare_ChildAfterParent()
    {
        _sut.Compare("15.1", "15").Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_NonNumericSegments_FallbackToStringCompare()
    {
        _sut.Compare("A", "B").Should().BeLessThan(0);
        _sut.Compare("1.A", "1.B").Should().BeLessThan(0);
    }
}
