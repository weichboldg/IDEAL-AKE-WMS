using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Services;

public class BdeDefaultWorkOperationServiceTests
{
    private static (ApplicationDbContext ctx, BdeDefaultWorkOperationService svc) Setup(string defaultAgName = "PRODUKTION")
    {
        var ctx = TestDbContextFactory.Create();
        var settingsMock = new Mock<IAppSettingRepository>();
        settingsMock.Setup(s => s.GetValueAsync("BdeDefaultArbeitsgang")).ReturnsAsync(defaultAgName);
        var svc = new BdeDefaultWorkOperationService(ctx, settingsMock.Object);
        return (ctx, svc);
    }

    private static int SeedFaAndWorkplace(ApplicationDbContext ctx)
    {
        var wp = new ProductionWorkplace { Name = "WB1", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        var po = new ProductionOrder { OrderNumber = "FA-100", Quantity = 10, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t", ProductionWorkplaceId = wp.Id };
        ctx.ProductionWorkplaces.Add(wp);
        ctx.ProductionOrders.Add(po);
        ctx.SaveChanges();
        po.ProductionWorkplaceId = wp.Id;
        ctx.SaveChanges();
        return po.Id;
    }

    [Fact]
    public async Task FindOrCreate_CreatesNewWO_WhenNoneExists()
    {
        var (ctx, svc) = Setup();
        var poId = SeedFaAndWorkplace(ctx);
        var wpId = ctx.ProductionWorkplaces.First().Id;

        var woId = await svc.FindOrCreateDefaultAsync(poId, wpId);

        woId.Should().BeGreaterThan(0);
        var wo = ctx.WorkOperations.First(w => w.Id == woId);
        wo.Name.Should().Be("PRODUKTION");
        wo.OperationNumber.Should().Be("01");
        wo.ProductionOrderId.Should().Be(poId);
        wo.IsReportable.Should().BeTrue();
    }

    [Fact]
    public async Task FindOrCreate_ReturnsExisting_WhenAlreadyExists()
    {
        var (ctx, svc) = Setup();
        var poId = SeedFaAndWorkplace(ctx);
        var wpId = ctx.ProductionWorkplaces.First().Id;

        var woId1 = await svc.FindOrCreateDefaultAsync(poId, wpId);
        var woId2 = await svc.FindOrCreateDefaultAsync(poId, wpId);

        woId1.Should().Be(woId2);
        ctx.WorkOperations.Count(w => w.ProductionOrderId == poId && w.Name == "PRODUKTION").Should().Be(1);
    }

    [Fact]
    public async Task FindOrCreate_DoesNotConfuseWithOtherAGs()
    {
        var (ctx, svc) = Setup();
        var poId = SeedFaAndWorkplace(ctx);
        var wpId = ctx.ProductionWorkplaces.First().Id;
        // Existierender AG "Fräsen" soll nicht gefunden werden
        ctx.WorkOperations.Add(new WorkOperation {
            ProductionOrderId = poId, OperationNumber = "10", Name = "Fräsen",
            Sequence = 1, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        ctx.SaveChanges();

        var woId = await svc.FindOrCreateDefaultAsync(poId, wpId);

        var wo = ctx.WorkOperations.First(w => w.Id == woId);
        wo.Name.Should().Be("PRODUKTION");
        ctx.WorkOperations.Count(w => w.ProductionOrderId == poId).Should().Be(2); // Fräsen + PRODUKTION
    }
}
