using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;

namespace IdealAkeWms.Tests.Repositories;

public class PickerAssignmentRepositoryTests
{
    // --- GetReleasedForPickingByPickerAsync Tests ---

    [Fact]
    public async Task GetReleasedForPickingByPickerAsync_ReturnsOnlyOrdersForSpecificPicker()
    {
        using var context = TestDbContextFactory.Create();
        context.ProductionOrders.AddRange(
            new ProductionOrder { OrderNumber = "WA-1", IsReleasedForPicking = true, IsDone = false, AssignedPickerId = 5, PickingPriority = 1, CreatedAt = DateTime.UtcNow, CreatedBy = "test", CreatedByWindows = "test" },
            new ProductionOrder { OrderNumber = "WA-2", IsReleasedForPicking = true, IsDone = false, AssignedPickerId = 10, PickingPriority = 2, CreatedAt = DateTime.UtcNow, CreatedBy = "test", CreatedByWindows = "test" },
            new ProductionOrder { OrderNumber = "WA-3", IsReleasedForPicking = true, IsDone = false, AssignedPickerId = 5, PickingPriority = 3, CreatedAt = DateTime.UtcNow, CreatedBy = "test", CreatedByWindows = "test" }
        );
        await context.SaveChangesAsync();

        var repo = new ProductionOrderRepository(context);
        var result = await repo.GetReleasedForPickingByPickerAsync(5);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(o => o.AssignedPickerId == 5);
        result.Select(o => o.OrderNumber).Should().ContainInOrder("WA-1", "WA-3");
    }

    [Fact]
    public async Task GetReleasedForPickingByPickerAsync_DoesNotReturnDoneOrders()
    {
        using var context = TestDbContextFactory.Create();
        context.ProductionOrders.AddRange(
            new ProductionOrder { OrderNumber = "WA-1", IsReleasedForPicking = true, IsDone = false, AssignedPickerId = 5, PickingPriority = 1, CreatedAt = DateTime.UtcNow, CreatedBy = "test", CreatedByWindows = "test" },
            new ProductionOrder { OrderNumber = "WA-2", IsReleasedForPicking = true, IsDone = true, AssignedPickerId = 5, PickingPriority = 2, CreatedAt = DateTime.UtcNow, CreatedBy = "test", CreatedByWindows = "test" }
        );
        await context.SaveChangesAsync();

        var repo = new ProductionOrderRepository(context);
        var result = await repo.GetReleasedForPickingByPickerAsync(5);

        result.Should().ContainSingle()
            .Which.OrderNumber.Should().Be("WA-1");
    }

    [Fact]
    public async Task GetReleasedForPickingByPickerAsync_DoesNotReturnNonReleasedOrders()
    {
        using var context = TestDbContextFactory.Create();
        context.ProductionOrders.AddRange(
            new ProductionOrder { OrderNumber = "WA-1", IsReleasedForPicking = true, IsDone = false, AssignedPickerId = 5, PickingPriority = 1, CreatedAt = DateTime.UtcNow, CreatedBy = "test", CreatedByWindows = "test" },
            new ProductionOrder { OrderNumber = "WA-2", IsReleasedForPicking = false, IsDone = false, AssignedPickerId = 5, CreatedAt = DateTime.UtcNow, CreatedBy = "test", CreatedByWindows = "test" }
        );
        await context.SaveChangesAsync();

        var repo = new ProductionOrderRepository(context);
        var result = await repo.GetReleasedForPickingByPickerAsync(5);

        result.Should().ContainSingle()
            .Which.OrderNumber.Should().Be("WA-1");
    }

    [Fact]
    public async Task GetReleasedForPickingByPickerAsync_ReturnsEmptyForUnknownPicker()
    {
        using var context = TestDbContextFactory.Create();
        context.ProductionOrders.Add(
            new ProductionOrder { OrderNumber = "WA-1", IsReleasedForPicking = true, IsDone = false, AssignedPickerId = 5, CreatedAt = DateTime.UtcNow, CreatedBy = "test", CreatedByWindows = "test" }
        );
        await context.SaveChangesAsync();

        var repo = new ProductionOrderRepository(context);
        var result = await repo.GetReleasedForPickingByPickerAsync(999);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetReleasedForPickingByPickerAsync_OrdersByPriorityThenDate()
    {
        using var context = TestDbContextFactory.Create();
        context.ProductionOrders.AddRange(
            new ProductionOrder { OrderNumber = "WA-NoPrio", IsReleasedForPicking = true, IsDone = false, AssignedPickerId = 5, PickingPriority = null, ProductionDate = new DateTime(2026, 1, 1), CreatedAt = DateTime.UtcNow, CreatedBy = "test", CreatedByWindows = "test" },
            new ProductionOrder { OrderNumber = "WA-Prio3", IsReleasedForPicking = true, IsDone = false, AssignedPickerId = 5, PickingPriority = 3, CreatedAt = DateTime.UtcNow, CreatedBy = "test", CreatedByWindows = "test" },
            new ProductionOrder { OrderNumber = "WA-Prio1", IsReleasedForPicking = true, IsDone = false, AssignedPickerId = 5, PickingPriority = 1, CreatedAt = DateTime.UtcNow, CreatedBy = "test", CreatedByWindows = "test" }
        );
        await context.SaveChangesAsync();

        var repo = new ProductionOrderRepository(context);
        var result = await repo.GetReleasedForPickingByPickerAsync(5);

        result.Should().HaveCount(3);
        result[0].OrderNumber.Should().Be("WA-Prio1");
        result[1].OrderNumber.Should().Be("WA-Prio3");
        result[2].OrderNumber.Should().Be("WA-NoPrio"); // null priority last
    }

    // --- GetActivePickersAsync Tests ---

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
