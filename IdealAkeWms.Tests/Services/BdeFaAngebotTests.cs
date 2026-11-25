using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Services;

/// <summary>v1.99.76 (Spec 2026-11-25-bde-masken-vereinheitlichung E4-1/E4-2, AK 23): gemeinsame Zeilenquelle Terminal-Liste + FA-Sicht.</summary>
public class BdeFaAngebotTests
{
    [Fact]
    public async Task EigeneLaufende_TerminalOhne_FaSichtMitLaeuftBeiMir_AgStatusAllerAgMitMarkierung()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var wo2Id = await BdeBookingTestSeed.AddSecondWorkOperationAsync(ctx, ids);
        var laufend = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running, DateTime.Now.AddMinutes(-5));
        ctx.BdeBookings.Add(laufend);
        await ctx.SaveChangesAsync();
        var wo1 = (await ctx.WorkOperations.FindAsync(ids.WorkOperationId))!;
        var wo2 = (await ctx.WorkOperations.FindAsync(wo2Id))!;

        var workOps = new Mock<IWorkOperationRepository>();
        workOps.Setup(r => r.GetOpenByWorkplaceIdsAsync(It.IsAny<IReadOnlyCollection<int>>(), true)).ReturnsAsync(new List<WorkOperation> { wo1, wo2 });
        workOps.Setup(r => r.GetBdeStatusAsync(It.IsAny<IReadOnlyCollection<int>>()))
            .ReturnsAsync(new FaBdeStatus(new HashSet<int>(), new HashSet<int> { wo1.Id }));
        var bookings = new Mock<IBdeBookingRepository>();
        bookings.Setup(r => r.GetActiveCockpitAsync()).ReturnsAsync(new List<BdeBooking> { laufend });
        var svc = new BdeFaAngebot(ctx, workOps.Object, bookings.Object, new FaHierarchyOrderInfoRepository(ctx), new HolidayRepository(ctx),
            new BusinessDayService(), new OperationConfigRepository(ctx));
        var wp = (await ctx.ProductionWorkplaces.FindAsync(ids.WorkplaceId))!;

        (await svc.LadenAsync(wp, ids.OperatorId, eigeneLaufendeAusblenden: true)).Select(z => z.Ag.Op.Id).Should().Equal(wo2.Id);

        var fa = await svc.FaSichtAsync(wp, ids.OperatorId);
        fa.Select(z => z.Op.Id).Should().BeEquivalentTo(new[] { wo1.Id, wo2.Id });
        var eigen = fa.Single(z => z.Op.Id == wo1.Id);
        eigen.Zeile.LaeuftBeiMir.Should().BeTrue();
        BdeFaAngebot.Spalten["status"](eigen).Should().Be("läuft bei Ihnen");
        eigen.AgSymbole.Should().HaveCount(2, "AG-Status zeigt alle AG des Auftrags (Antwort 4)");
        eigen.AgSymbole.Single(a => a.Markiert).Kuerzel.Should().Be(wo1.OperationNumber.Trim());
        eigen.AgSymbole.Single(a => a.Markiert).Status.Should().Be(FaAgStatus.Laeuft);
        eigen.Knoten.Should().BeNull("AKE: keine FA-Struktur");
        BdeFaAngebot.Spalten["status"](fa.Single(z => z.Op.Id == wo2.Id)).Should().Be("offen");

        // fremder Werker: dieselbe Zeile ohne „bei Ihnen“, Status läuft (Antwort 6 = A)
        var fremd = (await svc.FaSichtAsync(wp, ids.OperatorId + 999)).Single(z => z.Op.Id == wo1.Id);
        BdeFaAngebot.Spalten["status"](fremd).Should().Be("läuft");
    }
}
