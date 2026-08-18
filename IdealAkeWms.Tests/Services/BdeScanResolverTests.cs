using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;

namespace IdealAkeWms.Tests.Services;

/// <summary>
/// IDEAL-Standort Teil 8, Etappe 2: serverseitige Aufloesungslogik (BdeScanResolver).
/// Deckt beide Modi (Normal/NurFA), alle vier Ergebnisarten, den SubOrderNumber-Fallback und die
/// beiden KP2-6-Randfaelle (0 im Scope trotz HauptFA; genau 1 im Scope trotz global mehrerer) ab.
/// </summary>
public class BdeScanResolverTests
{
    private const int Wp = 5;        // Werkbank des Werkers
    private const int OtherWp = 6;   // fremde Werkbank

    private static void SeedPo(ApplicationDbContext ctx, int poId, string order, string subOrder,
        int? workplaceId, bool done = false, bool cancelled = false, string? sageStatus = null)
    {
        var po = new ProductionOrder
        {
            Id = poId,
            OrderNumber = order,
            SubOrderNumber = subOrder,
            Description1 = $"Desc {subOrder}",
            ArticleNumber = $"ART-{subOrder}",
            ProductionWorkplaceId = workplaceId,
            IsDone = done,
            IsCancelled = cancelled,
            CreatedAt = DateTime.Now,
            CreatedBy = "Test",
            CreatedByWindows = "TEST\\user"
        };
        ctx.ProductionOrders.Add(po);
        if (sageStatus != null)
        {
            ctx.Set<ProductionOrderExtraInfo>().Add(new ProductionOrderExtraInfo
            {
                ProductionOrderId = poId,
                SageStatus = sageStatus,
                CreatedAt = DateTime.Now,
                CreatedBy = "Test",
                CreatedByWindows = "TEST\\user"
            });
        }
    }

    private static WorkOperation SeedWo(ApplicationDbContext ctx, int woId, int poId, int? workplaceId,
        string opNumber = "01", bool reported = false, int sequence = 1)
    {
        var wo = new WorkOperation
        {
            Id = woId,
            ProductionOrderId = poId,
            OperationNumber = opNumber,
            Name = $"AG {opNumber}",
            Sequence = sequence,
            ProductionWorkplaceId = workplaceId,
            IsReported = reported,
            CreatedAt = DateTime.Now,
            CreatedBy = "Test",
            CreatedByWindows = "TEST\\user"
        };
        ctx.WorkOperations.Add(wo);
        return wo;
    }

    private static BdeScanResolver Resolver(ApplicationDbContext ctx) => new(ctx);

    // ---------------------------------------------------------------- Normal-Modus

    [Fact]
    public async Task Normal_Exact_WhenSingleWorkOperationAtWorkplace()
    {
        var ctx = TestDbContextFactory.Create();
        SeedPo(ctx, 1, "GRP-1", "SUB-1", Wp);
        SeedWo(ctx, 10, 1, Wp, "01");
        await ctx.SaveChangesAsync();

        var res = await Resolver(ctx).ResolveAsync(Wp, "GRP-1", "01", nurFaMode: false, operatorId: null);

        res.Kind.Should().Be(BdeScanResolutionKind.Exact);
        res.Single.Should().NotBeNull();
        res.Single!.Target.Should().Be(BdeBookTarget.WorkOperation);
        res.Single.Id.Should().Be(10);
        res.Single.SubOrderNumber.Should().Be("SUB-1");
        res.Single.OperationNumber.Should().Be("01");
    }

    [Fact]
    public async Task Normal_Ambiguous_WhenMultipleSubFasShareOrderAndOperation()
    {
        var ctx = TestDbContextFactory.Create();
        SeedPo(ctx, 1, "GRP-1", "SUB-1", Wp);
        SeedPo(ctx, 2, "GRP-1", "SUB-2", Wp);
        SeedWo(ctx, 10, 1, Wp, "01");
        SeedWo(ctx, 20, 2, Wp, "01");
        await ctx.SaveChangesAsync();

        var res = await Resolver(ctx).ResolveAsync(Wp, "GRP-1", "01", nurFaMode: false, operatorId: null);

        res.Kind.Should().Be(BdeScanResolutionKind.Ambiguous);
        res.Candidates.Should().HaveCount(2);
        res.Candidates.Select(c => c.SubOrderNumber).Should().BeEquivalentTo(new[] { "SUB-1", "SUB-2" });
        res.Candidates.Should().OnlyContain(c => c.Target == BdeBookTarget.WorkOperation);
    }

    [Fact]
    public async Task Normal_ExactInScope_EvenWhenGloballyMultiple()
    {
        // KP2-6 Randfall b: zweite passende Sub-FA liegt an fremder Werkbank → Direktbuchung.
        var ctx = TestDbContextFactory.Create();
        SeedPo(ctx, 1, "GRP-1", "SUB-1", Wp);
        SeedPo(ctx, 2, "GRP-1", "SUB-2", OtherWp);
        SeedWo(ctx, 10, 1, Wp, "01");
        SeedWo(ctx, 20, 2, OtherWp, "01");
        await ctx.SaveChangesAsync();

        var res = await Resolver(ctx).ResolveAsync(Wp, "GRP-1", "01", nurFaMode: false, operatorId: null);

        res.Kind.Should().Be(BdeScanResolutionKind.Exact);
        res.Single!.Id.Should().Be(10);
    }

    [Fact]
    public async Task Normal_NotInScope_WhenHauptFaExistsButNoOpenOperationAtWorkplace()
    {
        // KP2-6 Randfall a: HauptFA laeuft, aber nicht an dieser Werkbank.
        var ctx = TestDbContextFactory.Create();
        SeedPo(ctx, 1, "GRP-1", "SUB-1", OtherWp);
        SeedWo(ctx, 10, 1, OtherWp, "01");
        await ctx.SaveChangesAsync();

        var res = await Resolver(ctx).ResolveAsync(Wp, "GRP-1", "01", nurFaMode: false, operatorId: null);

        res.Kind.Should().Be(BdeScanResolutionKind.NotInScope);
        res.Candidates.Should().BeEmpty();
    }

    [Fact]
    public async Task Normal_NotInScope_WhenOnlyMatchIsAlreadyActivelyBooked()
    {
        // Deckungsgleich mit GetAvailableOperations: ein bereits aktiv gebuchter AG faellt aus dem
        // Scope → 0 Treffer, HauptFA existiert → NotInScope.
        var ctx = TestDbContextFactory.Create();
        SeedPo(ctx, 1, "GRP-1", "SUB-1", Wp);
        var wo = SeedWo(ctx, 10, 1, Wp, "01");
        ctx.BdeBookings.Add(new BdeBooking
        {
            Id = 100,
            BdeOperatorId = 1,
            ProductionWorkplaceId = Wp,
            BdeTerminalId = 1,
            WorkOperationId = wo.Id,
            StartedAt = DateTime.Now,
            EndedAt = null,
            IsCancelled = false,
            CreatedAt = DateTime.Now,
            CreatedBy = "Test",
            CreatedByWindows = "TEST\\user"
        });
        await ctx.SaveChangesAsync();

        var res = await Resolver(ctx).ResolveAsync(Wp, "GRP-1", "01", nurFaMode: false, operatorId: null);

        res.Kind.Should().Be(BdeScanResolutionKind.NotInScope);
    }

    [Fact]
    public async Task Normal_ExcludesPackedOrders_FromScope()
    {
        var ctx = TestDbContextFactory.Create();
        SeedPo(ctx, 1, "GRP-1", "SUB-1", Wp, sageStatus: FaZusatzinfoStatus.Verpackt);
        SeedWo(ctx, 10, 1, Wp, "01");
        await ctx.SaveChangesAsync();

        var res = await Resolver(ctx).ResolveAsync(Wp, "GRP-1", "01", nurFaMode: false, operatorId: null);

        res.Kind.Should().Be(BdeScanResolutionKind.NotInScope); // HauptFA existiert, aber verpackt → nicht im Scope
    }

    [Fact]
    public async Task Normal_StripsCommaFromScannedFa()
    {
        var ctx = TestDbContextFactory.Create();
        SeedPo(ctx, 1, "GRP-1", "SUB-1", Wp);
        SeedWo(ctx, 10, 1, Wp, "01");
        await ctx.SaveChangesAsync();

        var res = await Resolver(ctx).ResolveAsync(Wp, "GRP-1,01", "01", nurFaMode: false, operatorId: null);

        res.Kind.Should().Be(BdeScanResolutionKind.Exact);
        res.Single!.Id.Should().Be(10);
    }

    [Fact]
    public async Task Normal_AkeFlatCase_Exact_WhenOrderEqualsSubOrder()
    {
        var ctx = TestDbContextFactory.Create();
        SeedPo(ctx, 1, "FA-42", "FA-42", Wp); // flacher AKE-Fall: OrderNumber == SubOrderNumber
        SeedWo(ctx, 10, 1, Wp, "01");
        await ctx.SaveChangesAsync();

        var res = await Resolver(ctx).ResolveAsync(Wp, "FA-42", "01", nurFaMode: false, operatorId: null);

        res.Kind.Should().Be(BdeScanResolutionKind.Exact);
        res.ViaSubOrderNumberFallback.Should().BeFalse();
    }

    // ---------------------------------------------------------------- NurFA-Modus

    [Fact]
    public async Task NurFa_Exact_WhenSingleOrderAtWorkplace()
    {
        var ctx = TestDbContextFactory.Create();
        SeedPo(ctx, 1, "GRP-1", "SUB-1", Wp);
        await ctx.SaveChangesAsync();

        var res = await Resolver(ctx).ResolveAsync(Wp, "GRP-1", null, nurFaMode: true, operatorId: null);

        res.Kind.Should().Be(BdeScanResolutionKind.Exact);
        res.Single!.Target.Should().Be(BdeBookTarget.ProductionOrder);
        res.Single.Id.Should().Be(1);
        res.Single.OperationNumber.Should().BeNull();
    }

    [Fact]
    public async Task NurFa_Ambiguous_WhenMultipleSubFasSameOrder()
    {
        var ctx = TestDbContextFactory.Create();
        SeedPo(ctx, 1, "GRP-1", "SUB-1", Wp);
        SeedPo(ctx, 2, "GRP-1", "SUB-2", Wp);
        await ctx.SaveChangesAsync();

        var res = await Resolver(ctx).ResolveAsync(Wp, "GRP-1", null, nurFaMode: true, operatorId: null);

        res.Kind.Should().Be(BdeScanResolutionKind.Ambiguous);
        res.Candidates.Should().HaveCount(2);
        res.Candidates.Should().OnlyContain(c => c.Target == BdeBookTarget.ProductionOrder);
    }

    [Fact]
    public async Task NurFa_ExcludesDoneAndCancelledOrders()
    {
        var ctx = TestDbContextFactory.Create();
        SeedPo(ctx, 1, "GRP-1", "SUB-1", Wp, done: true);
        SeedPo(ctx, 2, "GRP-1", "SUB-2", Wp, cancelled: true);
        await ctx.SaveChangesAsync();

        var res = await Resolver(ctx).ResolveAsync(Wp, "GRP-1", null, nurFaMode: true, operatorId: null);

        res.Kind.Should().Be(BdeScanResolutionKind.NotInScope); // HauptFA existiert, aber alle geschlossen
    }

    // ---------------------------------------------------------------- Fallback / NotFound

    [Fact]
    public async Task Fallback_SubOrderNumber_WhenNoOrderMatch()
    {
        // Heute unerreichbare Vorruestung (AK 3): kein OrderNumber-Treffer, aber SubOrderNumber trifft.
        var ctx = TestDbContextFactory.Create();
        SeedPo(ctx, 1, "GRP-9", "SUB-7", Wp); // gescannt wird "SUB-7" == SubOrderNumber, nie OrderNumber
        await ctx.SaveChangesAsync();

        var res = await Resolver(ctx).ResolveAsync(Wp, "SUB-7", null, nurFaMode: true, operatorId: null);

        res.Kind.Should().Be(BdeScanResolutionKind.Exact);
        res.ViaSubOrderNumberFallback.Should().BeTrue();
        res.Single!.SubOrderNumber.Should().Be("SUB-7");
    }

    [Fact]
    public async Task NotFound_WhenNoMatchAnywhere()
    {
        var ctx = TestDbContextFactory.Create();
        SeedPo(ctx, 1, "GRP-1", "SUB-1", Wp);
        await ctx.SaveChangesAsync();

        var res = await Resolver(ctx).ResolveAsync(Wp, "DOES-NOT-EXIST", "01", nurFaMode: false, operatorId: null);

        res.Kind.Should().Be(BdeScanResolutionKind.NotFound);
        res.Candidates.Should().BeEmpty();
    }

    [Fact]
    public async Task NotFound_WhenScannedFaIsEmpty()
    {
        var ctx = TestDbContextFactory.Create();
        var res = await Resolver(ctx).ResolveAsync(Wp, "   ", "01", nurFaMode: false, operatorId: null);
        res.Kind.Should().Be(BdeScanResolutionKind.NotFound);
    }
}
