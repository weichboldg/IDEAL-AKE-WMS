using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Controllers;

public class BdeShiftCalendarControllerTests
{
    private static BdeShiftCalendarController CreateController(IdealAkeWms.Data.ApplicationDbContext ctx)
    {
        var userSvc = new Mock<ICurrentUserService>();
        userSvc.Setup(u => u.GetDisplayName()).Returns("tester");
        userSvc.Setup(u => u.GetWindowsUserName()).Returns("tester");
        var controller = new BdeShiftCalendarController(ctx, userSvc.Object);
        controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        return controller;
    }

    [Fact]
    public async Task Index_ReturnsOnlyDefaultShifts()
    {
        var ctx = TestDbContextFactory.Create();
        ctx.BdeShifts.Add(new BdeShift
        {
            DayOfWeek = DayOfWeek.Monday,
            StartTime = TimeSpan.FromHours(6),
            EndTime = TimeSpan.FromHours(14),
            Name = "Default Frueh",
            ProductionWorkplaceId = null,
            CreatedAt = DateTime.Now,
            CreatedBy = "t",
            CreatedByWindows = "t"
        });
        ctx.BdeShifts.Add(new BdeShift
        {
            DayOfWeek = DayOfWeek.Monday,
            StartTime = TimeSpan.FromHours(7),
            EndTime = TimeSpan.FromHours(15),
            Name = "Werkbank-spezifisch",
            ProductionWorkplaceId = 42,
            CreatedAt = DateTime.Now,
            CreatedBy = "t",
            CreatedByWindows = "t"
        });
        await ctx.SaveChangesAsync();

        var controller = CreateController(ctx);
        var result = await controller.Index() as ViewResult;

        var model = result!.Model as List<BdeShift>;
        model.Should().NotBeNull();
        model!.Should().HaveCount(1);
        model[0].Name.Should().Be("Default Frueh");
        model[0].ProductionWorkplaceId.Should().BeNull();
    }

    [Fact]
    public async Task Create_AddsDefaultShift()
    {
        var ctx = TestDbContextFactory.Create();
        var controller = CreateController(ctx);

        var vm = new BdeShiftEditViewModel
        {
            DayOfWeek = DayOfWeek.Tuesday,
            StartTime = TimeSpan.FromHours(6),
            EndTime = TimeSpan.FromHours(14),
            Name = "Frueh"
        };

        var result = await controller.Create(vm) as RedirectToActionResult;

        result.Should().NotBeNull();
        result!.ActionName.Should().Be(nameof(BdeShiftCalendarController.Index));
        ctx.BdeShifts.Should().HaveCount(1);
        var shift = ctx.BdeShifts.Single();
        shift.DayOfWeek.Should().Be(DayOfWeek.Tuesday);
        shift.StartTime.Should().Be(TimeSpan.FromHours(6));
        shift.EndTime.Should().Be(TimeSpan.FromHours(14));
        shift.Name.Should().Be("Frueh");
        shift.ProductionWorkplaceId.Should().BeNull();
        shift.CreatedBy.Should().Be("tester");
    }

    [Fact]
    public async Task Create_RejectsEndBeforeStart()
    {
        var ctx = TestDbContextFactory.Create();
        var controller = CreateController(ctx);

        var vm = new BdeShiftEditViewModel
        {
            DayOfWeek = DayOfWeek.Wednesday,
            StartTime = TimeSpan.FromHours(14),
            EndTime = TimeSpan.FromHours(6),
            Name = "Invalid"
        };

        var result = await controller.Create(vm) as RedirectToActionResult;

        result.Should().NotBeNull();
        result!.ActionName.Should().Be(nameof(BdeShiftCalendarController.Index));
        ctx.BdeShifts.Should().BeEmpty();
        controller.ModelState.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_RemovesShift()
    {
        var ctx = TestDbContextFactory.Create();
        var shift = new BdeShift
        {
            DayOfWeek = DayOfWeek.Friday,
            StartTime = TimeSpan.FromHours(6),
            EndTime = TimeSpan.FromHours(14),
            Name = "Frueh",
            ProductionWorkplaceId = null,
            CreatedAt = DateTime.Now,
            CreatedBy = "t",
            CreatedByWindows = "t"
        };
        ctx.BdeShifts.Add(shift);
        await ctx.SaveChangesAsync();

        var controller = CreateController(ctx);
        var result = await controller.Delete(shift.Id) as RedirectToActionResult;

        result.Should().NotBeNull();
        result!.ActionName.Should().Be(nameof(BdeShiftCalendarController.Index));
        ctx.BdeShifts.Should().BeEmpty();
    }
}
