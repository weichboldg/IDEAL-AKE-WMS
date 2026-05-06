using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Controllers;

public class StorageLocationsControllerTests
{
    [Fact]
    public async Task Edit_Post_SourceSage_IgnoresCodeZoneDescriptionAndIsActive()
    {
        using var ctx = TestDbContextFactory.Create();
        var existing = new StorageLocation
        {
            Code = "S-01",
            Zone = "HALLE-1",
            Description = "Sage-Beschreibung",
            BarcodeValue = "S-01",
            Source = StorageLocationSource.Sage,
            IsActive = true,
            Capacity = null,
            IsPickingTransport = false,
            CreatedBy = "x",
            CreatedByWindows = "x"
        };
        ctx.StorageLocations.Add(existing);
        await ctx.SaveChangesAsync();

        var repo = new StorageLocationRepository(ctx);
        var userSvc = new Mock<ICurrentUserService>();
        userSvc.Setup(x => x.GetDisplayName()).Returns("admin");
        userSvc.Setup(x => x.GetWindowsUserName()).Returns("admin");
        var ctrl = new StorageLocationsController(repo, userSvc.Object);

        var posted = new StorageLocation
        {
            Id = existing.Id,
            Code = "HACKED",
            Zone = "BAD",
            Description = "BAD",
            IsActive = false,                  // Versuch zu deaktivieren
            Capacity = 99,                     // erlaubt
            IsPickingTransport = true          // erlaubt
        };

        var result = await ctrl.Edit(existing.Id, posted);
        result.Should().BeOfType<RedirectToActionResult>();

        var saved = ctx.StorageLocations.Single();
        saved.Code.Should().Be("S-01");
        saved.Zone.Should().Be("HALLE-1");
        saved.Description.Should().Be("Sage-Beschreibung");
        saved.IsActive.Should().BeTrue();
        saved.Capacity.Should().Be(99);
        saved.IsPickingTransport.Should().BeTrue();
    }

    [Fact]
    public async Task Edit_Post_SourceManual_AcceptsAllFields_IncludingIsActive()
    {
        using var ctx = TestDbContextFactory.Create();
        var existing = new StorageLocation
        {
            Code = "M-01",
            Zone = "Z1",
            Description = "Manuell",
            BarcodeValue = "M-01",
            Source = StorageLocationSource.Manual,
            IsActive = true,
            CreatedBy = "x",
            CreatedByWindows = "x"
        };
        ctx.StorageLocations.Add(existing);
        await ctx.SaveChangesAsync();

        var repo = new StorageLocationRepository(ctx);
        var userSvc = new Mock<ICurrentUserService>();
        userSvc.Setup(x => x.GetDisplayName()).Returns("admin");
        userSvc.Setup(x => x.GetWindowsUserName()).Returns("admin");
        var ctrl = new StorageLocationsController(repo, userSvc.Object);

        var posted = new StorageLocation
        {
            Id = existing.Id,
            Code = "M-01-NEU",
            Zone = "Z2",
            Description = "Geaendert",
            IsActive = false,
            Capacity = 5,
            IsPickingTransport = false
        };

        await ctrl.Edit(existing.Id, posted);

        var saved = ctx.StorageLocations.Single();
        saved.Code.Should().Be("M-01-NEU");
        saved.Zone.Should().Be("Z2");
        saved.Description.Should().Be("Geaendert");
        saved.IsActive.Should().BeFalse();
        saved.BarcodeValue.Should().Be("M-01-NEU");
    }
}
