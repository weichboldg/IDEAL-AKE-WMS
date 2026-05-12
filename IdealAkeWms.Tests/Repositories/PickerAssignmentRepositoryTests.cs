using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;

namespace IdealAkeWms.Tests.Repositories;

/// <summary>
/// Tests fuer Picker-Assignment-Queries.
/// Seit Phase 1 (v1.11.0) wandert die Methode <c>GetReleasedForPickingByPickerAsync</c>
/// vom <see cref="ProductionOrderRepository"/> in den <see cref="ProductionOrderPickingStatusRepository"/>.
/// Die <c>GetActivePickersAsync</c>-Tests bleiben unveraendert auf dem <see cref="UserRepository"/>.
/// </summary>
public class PickerAssignmentRepositoryTests
{
    // --- GetReleasedForPickingByPickerAsync Tests (jetzt auf PickingStatusRepository) ---

    [Fact]
    public async Task GetReleasedForPickingByPickerAsync_ReturnsOnlyOrdersForSpecificPicker()
    {
        using var context = TestDbContextFactory.Create();
        TestDataHelper.CreateOrderWithStatuses(context, "WA-1", releaseForPicking: true, assignedPickerId: 5, pickingPriority: 1);
        TestDataHelper.CreateOrderWithStatuses(context, "WA-2", releaseForPicking: true, assignedPickerId: 10, pickingPriority: 2);
        TestDataHelper.CreateOrderWithStatuses(context, "WA-3", releaseForPicking: true, assignedPickerId: 5, pickingPriority: 3);

        var repo = new ProductionOrderPickingStatusRepository(context);
        var result = await repo.GetReleasedForPickingByPickerAsync(5);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(o => o.PickingStatus!.AssignedPickerId == 5);
        result.Select(o => o.OrderNumber).Should().ContainInOrder("WA-1", "WA-3");
    }

    [Fact]
    public async Task GetReleasedForPickingByPickerAsync_DoesNotReturnDoneOrders()
    {
        using var context = TestDbContextFactory.Create();
        TestDataHelper.CreateOrderWithStatuses(context, "WA-1", releaseForPicking: true, assignedPickerId: 5, pickingPriority: 1, isDone: false);
        TestDataHelper.CreateOrderWithStatuses(context, "WA-2", releaseForPicking: true, assignedPickerId: 5, pickingPriority: 2, isDone: true);

        var repo = new ProductionOrderPickingStatusRepository(context);
        var result = await repo.GetReleasedForPickingByPickerAsync(5);

        result.Should().ContainSingle()
            .Which.OrderNumber.Should().Be("WA-1");
    }

    [Fact]
    public async Task GetReleasedForPickingByPickerAsync_DoesNotReturnNonReleasedOrders()
    {
        using var context = TestDbContextFactory.Create();
        TestDataHelper.CreateOrderWithStatuses(context, "WA-1", releaseForPicking: true, assignedPickerId: 5, pickingPriority: 1);
        TestDataHelper.CreateOrderWithStatuses(context, "WA-2", releaseForPicking: false, assignedPickerId: 5);

        var repo = new ProductionOrderPickingStatusRepository(context);
        var result = await repo.GetReleasedForPickingByPickerAsync(5);

        result.Should().ContainSingle()
            .Which.OrderNumber.Should().Be("WA-1");
    }

    [Fact]
    public async Task GetReleasedForPickingByPickerAsync_ReturnsEmptyForUnknownPicker()
    {
        using var context = TestDbContextFactory.Create();
        TestDataHelper.CreateOrderWithStatuses(context, "WA-1", releaseForPicking: true, assignedPickerId: 5);

        var repo = new ProductionOrderPickingStatusRepository(context);
        var result = await repo.GetReleasedForPickingByPickerAsync(999);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetReleasedForPickingByPickerAsync_OrdersByPriorityThenDate()
    {
        using var context = TestDbContextFactory.Create();
        TestDataHelper.CreateOrderWithStatuses(context, "WA-NoPrio",
            releaseForPicking: true, assignedPickerId: 5, pickingPriority: null,
            productionDate: new DateTime(2026, 1, 1));
        TestDataHelper.CreateOrderWithStatuses(context, "WA-Prio3",
            releaseForPicking: true, assignedPickerId: 5, pickingPriority: 3);
        TestDataHelper.CreateOrderWithStatuses(context, "WA-Prio1",
            releaseForPicking: true, assignedPickerId: 5, pickingPriority: 1);

        var repo = new ProductionOrderPickingStatusRepository(context);
        var result = await repo.GetReleasedForPickingByPickerAsync(5);

        result.Should().HaveCount(3);
        result[0].OrderNumber.Should().Be("WA-Prio1");
        result[1].OrderNumber.Should().Be("WA-Prio3");
        result[2].OrderNumber.Should().Be("WA-NoPrio"); // null priority last
    }

    // --- GetActivePickersAsync Tests (unveraendert auf UserRepository) ---

    [Fact]
    public async Task GetActivePickersAsync_ReturnsOnlyActivePickerUsers()
    {
        using var context = TestDbContextFactory.Create();
        context.Users.AddRange(
            new User { Name = "Picker Active", IsActive = true, IsPicker = true, CreatedAt = DateTime.UtcNow, CreatedBy = "test", CreatedByWindows = "test" },
            new User { Name = "Non-Picker Active", IsActive = true, IsPicker = false, CreatedAt = DateTime.UtcNow, CreatedBy = "test", CreatedByWindows = "test" },
            new User { Name = "Picker Inactive", IsActive = false, IsPicker = true, CreatedAt = DateTime.UtcNow, CreatedBy = "test", CreatedByWindows = "test" },
            new User { Name = "Both Inactive", IsActive = false, IsPicker = false, CreatedAt = DateTime.UtcNow, CreatedBy = "test", CreatedByWindows = "test" }
        );
        await context.SaveChangesAsync();

        var repo = new UserRepository(context);
        var result = await repo.GetActivePickersAsync();

        result.Should().ContainSingle()
            .Which.Name.Should().Be("Picker Active");
    }

    [Fact]
    public async Task GetActivePickersAsync_ReturnsEmptyWhenNoPickers()
    {
        using var context = TestDbContextFactory.Create();
        context.Users.AddRange(
            new User { Name = "Regular User", IsActive = true, IsPicker = false, CreatedAt = DateTime.UtcNow, CreatedBy = "test", CreatedByWindows = "test" }
        );
        await context.SaveChangesAsync();

        var repo = new UserRepository(context);
        var result = await repo.GetActivePickersAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetActivePickersAsync_OrdersByName()
    {
        using var context = TestDbContextFactory.Create();
        context.Users.AddRange(
            new User { Name = "Zoe", IsActive = true, IsPicker = true, CreatedAt = DateTime.UtcNow, CreatedBy = "test", CreatedByWindows = "test" },
            new User { Name = "Anna", IsActive = true, IsPicker = true, CreatedAt = DateTime.UtcNow, CreatedBy = "test", CreatedByWindows = "test" },
            new User { Name = "Max", IsActive = true, IsPicker = true, CreatedAt = DateTime.UtcNow, CreatedBy = "test", CreatedByWindows = "test" }
        );
        await context.SaveChangesAsync();

        var repo = new UserRepository(context);
        var result = await repo.GetActivePickersAsync();

        result.Should().HaveCount(3);
        result[0].Name.Should().Be("Anna");
        result[1].Name.Should().Be("Max");
        result[2].Name.Should().Be("Zoe");
    }
}
