using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace IdealAkeWms.Tests.Repositories;

/// <summary>
/// Integrationstests des Enqueue-Decorators auf der InMemory-DB: prueft das Gating
/// (globaler Toggle UND Lagerplatz-Flag), den Bewegungsart-Filter (Feedback-Loop-Schutz)
/// und die Pass-through-Zusicherung (S7: bei Toggle aus entsteht kein Queue-Eintrag).
/// </summary>
public class SageBookingEnqueueingStockMovementRepositoryTests
{
    private static (SageBookingEnqueueingStockMovementRepository repo, IdealAkeWms.Data.ApplicationDbContext ctx, int articleId, int locationId)
        Build(bool toggleOn, bool locationAllows)
    {
        var ctx = TestDbContextFactory.Create();

        var loc = new StorageLocation
        {
            Code = "LL;1;4;0",
            Source = StorageLocationSource.Sage,
            SageBuchungErlaubt = locationAllows,
            SageLagerkennung = "LL;1;4;0",
            SageLagerplatzId = 42,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        };
        var art = new Article { ArticleNumber = "ART-1", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        ctx.StorageLocations.Add(loc);
        ctx.Articles.Add(art);
        ctx.ServiceSettings.Add(new ServiceSetting { Key = "SageLagerbuchungAktiv", Value = toggleOn ? "true" : "false" });
        ctx.SaveChanges();

        var repo = new SageBookingEnqueueingStockMovementRepository(
            ctx,
            new ServiceSettingRepository(ctx),
            new SageBookingQueueRepository(ctx),
            NullLogger<SageBookingEnqueueingStockMovementRepository>.Instance);

        return (repo, ctx, art.Id, loc.Id);
    }

    private static StockMovement Movement(int articleId, int locationId, MovementType type) => new()
    {
        ArticleId = articleId,
        StorageLocationId = locationId,
        Quantity = 3m,
        MovementType = type,
        Timestamp = DateTime.Now,
        WindowsUser = "t",
        CreatedAt = DateTime.Now, CreatedBy = "tester", CreatedByWindows = "t"
    };

    [Fact]
    public async Task AddAsync_BothOn_Einbuchung_CreatesOpenQueueItem()
    {
        var (repo, ctx, artId, locId) = Build(toggleOn: true, locationAllows: true);

        var saved = await repo.AddAsync(Movement(artId, locId, MovementType.Einbuchung));

        var items = await ctx.SageBookingQueueItems.ToListAsync();
        items.Should().ContainSingle();
        items[0].StockMovementId.Should().Be(saved.Id);
        items[0].Status.Should().Be(SageBookingQueueStatus.Offen);
    }

    [Fact]
    public async Task AddAsync_ToggleOff_CreatesNoQueueItem_ButStillSavesMovement()
    {
        var (repo, ctx, artId, locId) = Build(toggleOn: false, locationAllows: true);

        var saved = await repo.AddAsync(Movement(artId, locId, MovementType.Ausbuchung));

        (await ctx.StockMovements.CountAsync()).Should().Be(1);   // Buchung bleibt (Pass-through)
        saved.Id.Should().BeGreaterThan(0);
        (await ctx.SageBookingQueueItems.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task AddAsync_LocationFlagOff_CreatesNoQueueItem()
    {
        var (repo, ctx, artId, locId) = Build(toggleOn: true, locationAllows: false);

        await repo.AddAsync(Movement(artId, locId, MovementType.Einbuchung));

        (await ctx.SageBookingQueueItems.CountAsync()).Should().Be(0);
    }

    [Theory]
    [InlineData(MovementType.SageEinbuchung)]
    [InlineData(MovementType.SageAusbuchung)]
    [InlineData(MovementType.Umbuchung)]
    public async Task AddAsync_NonBookableType_CreatesNoQueueItem_EvenWithBothOn(MovementType type)
    {
        var (repo, ctx, artId, locId) = Build(toggleOn: true, locationAllows: true);

        await repo.AddAsync(Movement(artId, locId, type));

        (await ctx.SageBookingQueueItems.CountAsync()).Should().Be(0);
    }
}
