using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Tests.Controllers;

public class SyncLogControllerTests
{
    [Fact]
    public async Task Index_FiltersByServiceAndLevel()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.SyncLogs.AddRange(
            new SyncLog { Service = "Lagerplatz", Level = SyncLogLevel.Info,    Message = "A" },
            new SyncLog { Service = "Lagerplatz", Level = SyncLogLevel.Warning, Message = "B" },
            new SyncLog { Service = "OseonTracking", Level = SyncLogLevel.Warning, Message = "C" }
        );
        await ctx.SaveChangesAsync();
        var repo = new SyncLogRepository(ctx);
        var ctrl = new SyncLogController(repo, new FakeCurrentUserService());

        var result = await ctrl.Index(service: "Lagerplatz", level: SyncLogLevel.Warning, reference: null);

        var view = result.Should().BeOfType<ViewResult>().Subject;
        var vm = view.Model.Should().BeOfType<SyncLogIndexViewModel>().Subject;
        vm.Entries.Should().ContainSingle();
        vm.Entries[0].Message.Should().Be("B");
    }
}
