using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using IDEALAKEWMSService.Common;
using IDEALAKEWMSService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IDEALAKEWMSService.Tests.Services;

/// <summary>
/// Werkbank-Zuweisung (Variante B, Spec K2): Sage liefert je Struktur-Knoten einen
/// <c>Arbeitsbereich</c>, daraus wird <see cref="ProductionOrder.ProductionWorkplaceId"/>.
/// Sage ist fuehrend (ADR "Werkbank-Datenhoheit") — ein abweichender Bestandswert wird
/// ueberschrieben. Unbekannter oder mehrdeutiger Name: melden, NICHT zuweisen/anlegen.
/// </summary>
public class FaMaterializationWorkplaceTests
{
    private static FaMaterializationSyncService Build(
        ApplicationDbContext ctx, IUnknownWorkplaceState? unknownState = null)
    {
        var config = new ConfigurationBuilder().Build(); // leer; SendMissingDigest/Digest no-op
        var mail = new Mock<IMailService>();
        return new FaMaterializationSyncService(
            ctx, config, mail.Object, unknownState ?? new UnknownWorkplaceState(),
            new ProductionOrderPickingStatusRepository(ctx),
            NullLogger<FaMaterializationSyncService>.Instance, new FakeSyncLogger());
    }

    private static ProductionWorkplace Workplace(string name) => new()
    {
        Name = name,
        CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
    };

    [Fact]
    public async Task RunAsync_KnownArbeitsbereich_AssignsWorkplace()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.FaHierarchyNodes.Add(new FaHierarchyNode
        {
            HauptFA = 1000, SubFA = 1001, VaterFA = null, Sollmenge = 1, Arbeitsbereich = "S-01"
        });
        var wp = Workplace("S-01");
        ctx.ProductionWorkplaces.Add(wp);
        await ctx.SaveChangesAsync();

        await Build(ctx).RunAsync(dryRun: false);

        var order = await ctx.ProductionOrders.SingleAsync(o => o.SubOrderNumber == "1001");
        order.ProductionWorkplaceId.Should().Be(wp.Id);
    }

    [Fact]
    public async Task RunAsync_ArbeitsbereichMatchesCaseInsensitiveAndTrimmed_AssignsWorkplace()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.FaHierarchyNodes.Add(new FaHierarchyNode
        {
            HauptFA = 1000, SubFA = 1001, VaterFA = null, Sollmenge = 1, Arbeitsbereich = " s-01 "
        });
        var wp = Workplace("S-01");
        ctx.ProductionWorkplaces.Add(wp);
        await ctx.SaveChangesAsync();

        await Build(ctx).RunAsync(dryRun: false);

        var order = await ctx.ProductionOrders.SingleAsync(o => o.SubOrderNumber == "1001");
        order.ProductionWorkplaceId.Should().Be(wp.Id);
    }

    [Fact]
    public async Task RunAsync_UnknownArbeitsbereich_LeavesWorkplaceNullAndReports()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.FaHierarchyNodes.Add(new FaHierarchyNode
        {
            HauptFA = 1000, SubFA = 1001, VaterFA = null, Sollmenge = 1, Arbeitsbereich = "K-99"
        });
        await ctx.SaveChangesAsync();

        // HasChanged=false stubben: kein Digest-Versandversuch -> kein DB-Zugriff im Test noetig
        // (ServiceSettings liest die Mail-Konfiguration live aus der SQL-Server-DB, S1-Feature-Gate).
        var unknownState = new Mock<IUnknownWorkplaceState>();
        unknownState.Setup(s => s.HasChanged(It.IsAny<IReadOnlyCollection<string>>())).Returns(false);

        var fakeLogger = new FakeSyncLogger();
        var svc = new FaMaterializationSyncService(
            ctx, new ConfigurationBuilder().Build(), new Mock<IMailService>().Object,
            unknownState.Object, new ProductionOrderPickingStatusRepository(ctx),
            NullLogger<FaMaterializationSyncService>.Instance, fakeLogger);

        await svc.RunAsync(dryRun: false);

        var order = await ctx.ProductionOrders.SingleAsync(o => o.SubOrderNumber == "1001");
        order.ProductionWorkplaceId.Should().BeNull();
        fakeLogger.Runs.Single().FinalCounts!["arbeitsbereich_unbekannt"].Should().Be(1);
    }

    [Fact]
    public async Task RunAsync_AmbiguousWorkplaceName_DoesNotAssign()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.FaHierarchyNodes.Add(new FaHierarchyNode
        {
            HauptFA = 1000, SubFA = 1001, VaterFA = null, Sollmenge = 1, Arbeitsbereich = "S-01"
        });
        ctx.ProductionWorkplaces.AddRange(Workplace("S-01"), Workplace("S-01"));
        await ctx.SaveChangesAsync();

        var fakeLogger = new FakeSyncLogger();
        var svc = new FaMaterializationSyncService(
            ctx, new ConfigurationBuilder().Build(), new Mock<IMailService>().Object,
            new UnknownWorkplaceState(), new ProductionOrderPickingStatusRepository(ctx),
            NullLogger<FaMaterializationSyncService>.Instance, fakeLogger);

        await svc.RunAsync(dryRun: false);

        var order = await ctx.ProductionOrders.SingleAsync(o => o.SubOrderNumber == "1001");
        order.ProductionWorkplaceId.Should().BeNull();
        fakeLogger.Runs.Single().FinalCounts!["arbeitsbereich_mehrdeutig"].Should().Be(1);
    }

    [Fact]
    public async Task RunAsync_ExistingDifferentWorkplace_IsOverwrittenAndCounted()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.FaHierarchyNodes.Add(new FaHierarchyNode
        {
            HauptFA = 1000, SubFA = 1001, VaterFA = null, Sollmenge = 1, Arbeitsbereich = "S-01"
        });
        var wpOld = Workplace("K-02");
        var wpNew = Workplace("S-01");
        ctx.ProductionWorkplaces.AddRange(wpOld, wpNew);
        await ctx.SaveChangesAsync();

        ctx.ProductionOrders.Add(new ProductionOrder
        {
            OrderNumber = "1000", SubOrderNumber = "1001", Quantity = 1,
            ProductionWorkplaceId = wpOld.Id,
            CreatedAt = DateTime.Now, CreatedBy = "test", CreatedByWindows = "test"
        });
        await ctx.SaveChangesAsync();

        var fakeLogger = new FakeSyncLogger();
        var svc = new FaMaterializationSyncService(
            ctx, new ConfigurationBuilder().Build(), new Mock<IMailService>().Object,
            new UnknownWorkplaceState(), new ProductionOrderPickingStatusRepository(ctx),
            NullLogger<FaMaterializationSyncService>.Instance, fakeLogger);

        await svc.RunAsync(dryRun: false);

        var order = await ctx.ProductionOrders.SingleAsync(o => o.SubOrderNumber == "1001");
        order.ProductionWorkplaceId.Should().Be(wpNew.Id);
        fakeLogger.Runs.Single().FinalCounts!["werkbank_abweichend"].Should().Be(1);
    }

    [Fact]
    public async Task RunAsync_DryRun_DoesNotChangeWorkplace()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.FaHierarchyNodes.Add(new FaHierarchyNode
        {
            HauptFA = 1000, SubFA = 1001, VaterFA = null, Sollmenge = 1, Arbeitsbereich = "S-01"
        });
        var wp = Workplace("S-01");
        ctx.ProductionWorkplaces.Add(wp);
        await ctx.SaveChangesAsync();

        await Build(ctx).RunAsync(dryRun: true);

        (await ctx.ProductionOrders.CountAsync()).Should().Be(0);
    }
}
