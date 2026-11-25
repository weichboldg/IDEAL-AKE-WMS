using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Xunit;

namespace IdealAkeWms.Tests.Services;

/// <summary>v1.99.77 (Spec 2026-11-25-bde-masken-vereinheitlichung E4-4, AK 25): Kopf-Buchungen = dieselben Abfragen wie
/// BdeTerminal ActiveBookings/PausedBookings (eine Quelle fuer Terminal und FA-Sicht).</summary>
public class BdeKopfBuchungenTests
{
    [Fact]
    public async Task Laufend_NurEigeneLaufende_Pausiert_OhneFortgesetzte()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var anderer = await BdeBookingTestSeed.AddSecondOperatorAsync(ctx);
        var laufend = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running, DateTime.Now.AddMinutes(-20));
        var taetigkeit = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Activity, BdeBookingStatus.Running, DateTime.Now.AddMinutes(-5));
        var pausiert = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Paused, DateTime.Now.AddHours(-3), DateTime.Now.AddHours(-2));
        var fortgesetzt = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Paused, DateTime.Now.AddHours(-5), DateTime.Now.AddHours(-4));
        var fremd = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running, DateTime.Now.AddMinutes(-1));
        fremd.BdeOperatorId = anderer;
        ctx.BdeBookings.AddRange(laufend, taetigkeit, pausiert, fortgesetzt, fremd);
        await ctx.SaveChangesAsync();
        var kind = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Finished, DateTime.Now.AddHours(-4), DateTime.Now.AddHours(-3));
        kind.ParentBookingId = fortgesetzt.Id;
        ctx.BdeBookings.Add(kind);
        await ctx.SaveChangesAsync();

        ctx.ProductionOrders.Single(po => po.Id == ids.ProductionOrderId).SubOrderNumber = "SUB-7";
        await ctx.SaveChangesAsync();
        var l = await BdeKopfBuchungen.LaufendeAsync(ctx, ids.OperatorId);
        l.Select(b => b.BookingId).Should().Equal(laufend.Id, taetigkeit.Id);
        l[0].TargetQuantity.Should().Be(10);
        l[0].OrderNumber.Should().NotBeEmpty();
        l[0].SubOrderNumber.Should().Be("SUB-7"); // v1.99.79 F-35
        l[1].ActivityName.Should().Be("Test Activity");

        var p = await BdeKopfBuchungen.PausierteAsync(ctx, ids.OperatorId);
        p.Select(b => b.BookingId).Should().Equal(pausiert.Id);
        p[0].PausedAt.Should().Be(pausiert.EndedAt);
    }

    [Fact]
    public void Laufzeit_StundenMinuten()
    {
        BdeKopfBuchungen.Laufzeit(new DateTime(2026, 11, 25, 8, 0, 0), new DateTime(2026, 11, 25, 9, 5, 0)).Should().Be("1:05 h");
        BdeKopfBuchungen.Laufzeit(new DateTime(2026, 11, 25, 8, 0, 0), new DateTime(2026, 11, 25, 7, 0, 0)).Should().Be("0:00 h");
    }

    [Fact]
    public void SeiteMitZeile_FindetSeite_SonstNull()
    {
        var ids = new[] { 5, 6, 7, 8, 9 };
        BdeTeilelisteController.SeiteMitZeile(ids, 5, 2).Should().Be(1);
        BdeTeilelisteController.SeiteMitZeile(ids, 8, 2).Should().Be(2);
        BdeTeilelisteController.SeiteMitZeile(ids, 9, 2).Should().Be(3);
        BdeTeilelisteController.SeiteMitZeile(ids, 42, 2).Should().BeNull("ausgefiltert oder nicht offen");
    }
}
