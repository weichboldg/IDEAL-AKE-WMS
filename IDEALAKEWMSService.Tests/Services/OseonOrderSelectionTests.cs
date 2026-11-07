using FluentAssertions;
using IDEALAKEWMSService.Services;
using Xunit;

namespace IDEALAKEWMSService.Tests.Services;

public class OseonOrderSelectionTests
{
    private static OseonRawRow Row(string number, long id, string? pos = null,
        DateTime? lastReport = null, int agaStatus = 0) => new()
    {
        OseonOrderNumber = number,
        OseonId = id,
        PositionNumber = pos,
        LastStatusReport = lastReport,
        AgaStatus = agaStatus,
    };

    // AK 3: Mehrfachlieferung -> hoechste ID, unabhaengig von der Lesereihenfolge
    [Fact]
    public void Select_MultipleIds_PicksHighest_InBothOrders()
    {
        foreach (var rows in new[]
                 {
                     new[] { Row("X", 100), Row("X", 200) },
                     new[] { Row("X", 200), Row("X", 100) },
                 })
        {
            var result = OseonOrderSelection.Select(rows);

            result.Orders.Should().ContainSingle().Which.OseonId.Should().Be(200);
            var m = result.Multiples.Should().ContainSingle().Subject;
            m.OrderNumber.Should().Be("X");
            m.OseonIds.Should().Equal(100, 200);
            m.SelectedOseonId.Should().Be(200);
        }
    }

    // AK 4: AGs nur aus der gewaehlten Zeile, je Position einmal
    [Fact]
    public void Select_Operations_OnlyFromSelectedRow_OncePerPosition()
    {
        var rows = new[]
        {
            Row("X", 100, "10"), Row("X", 100, "20"),
            Row("X", 200, "10"), Row("X", 200, "20"),
        };

        var result = OseonOrderSelection.Select(rows);

        result.Operations.Should().HaveCount(2);
        result.Operations.Select(o => o.PositionNumber).Should().BeEquivalentTo("10", "20");
        result.Operations.Should().OnlyContain(o => o.OseonId == 200);
    }

    // AK 5: Gross/Klein ergibt einen Auftrag (DB-Kollation CI), hoechste ID gewinnt
    [Fact]
    public void Select_CaseInsensitiveNumber_IsOneOrder()
    {
        var result = OseonOrderSelection.Select(new[] { Row("ab-01", 5), Row("AB-01", 7) });

        result.Orders.Should().ContainSingle().Which.OseonId.Should().Be(7);
        result.Multiples.Should().ContainSingle();
    }

    [Fact]
    public void Select_DuplicatePosition_LatestStatusReportWins_ThenHighestStatus()
    {
        var older = new DateTime(2026, 11, 1);
        var newer = new DateTime(2026, 11, 2);
        var result = OseonOrderSelection.Select(new[]
        {
            Row("X", 1, "10", older, agaStatus: 90),
            Row("X", 1, "10", newer, agaStatus: 10),
            Row("X", 1, "20", newer, agaStatus: 10),
            Row("X", 1, "20", newer, agaStatus: 50),
        });

        result.Operations.Single(o => o.PositionNumber == "10").LastStatusReport.Should().Be(newer);
        result.Operations.Single(o => o.PositionNumber == "20").AgaStatus.Should().Be(50);
    }

    // AK 6 (Normalfall): eine Zeile je Auftrag, keine Mehrfachmeldung; leere Nummern/Positionen fallen raus
    [Fact]
    public void Select_NormalCase_NoMultiples()
    {
        var result = OseonOrderSelection.Select(new[]
        {
            Row("A", 1, "10"), Row("A", 1, "20"), Row("B", 2, null), Row("", 3, "10"),
        });

        result.Orders.Select(o => o.OseonOrderNumber).Should().BeEquivalentTo("A", "B");
        result.Operations.Should().HaveCount(2);
        result.Multiples.Should().BeEmpty();
    }
}
