using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models.Standort;
using IdealAkeWms.Services.HierarchischeStruktur;
using IdealAkeWms.Services.Standort;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace IdealAkeWms.Tests.Controllers;

public class StandortEinstellungenControllerTests
{
    private readonly Mock<IAppSettingRepository> _app = new();
    private readonly Mock<IServiceSettingRepository> _svc = new();
    private readonly Mock<IStandortSettingsWriter> _writer = new();
    private readonly HierarchischeStrukturStatus _status = new(Mock.Of<IServiceScopeFactory>());

    private StandortEinstellungenController Create()
    {
        _app.Setup(r => r.GetValueAsync(It.IsAny<string>())).ReturnsAsync((string?)null);
        _svc.Setup(r => r.GetValueAsync(It.IsAny<string>())).ReturnsAsync((string?)null);
        return new StandortEinstellungenController(_app.Object, _svc.Object, _writer.Object, _status)
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        };
    }

    [Fact]
    public async Task Index_RendersAllGroups_WithoutMasterAsEditableField()
    {
        var result = await Create().Index();

        var vm = result.Should().BeOfType<ViewResult>().Subject.Model
            .Should().BeOfType<StandortEinstellungenViewModel>().Subject;
        vm.Groups.Should().HaveCount(StandortSettingsCatalog.GroupOrder.Count);
        var keys = vm.Groups.SelectMany(g => g.Fields).Select(f => f.Key).ToList();
        keys.Should().Contain("Firmenname");
        keys.Should().Contain("Sync:HierarchicalFaEnabled");
        keys.Should().NotContain(StandortSettingsCatalog.MasterKey); // Master ist read-only, kein Feld
    }

    [Fact]
    public async Task Index_ShowsMasterReadOnlyStatus()
    {
        var ctrl = Create();
        // spezifisches Setup NACH Create() -> gewinnt gegen die It.IsAny-Vorgabe (Moq: last wins)
        _svc.Setup(r => r.GetValueAsync(StandortSettingsCatalog.MasterKey)).ReturnsAsync("true");
        var result = await ctrl.Index();
        var vm = (StandortEinstellungenViewModel)((ViewResult)result).Model!;
        vm.Master.Enabled.Should().BeTrue();
        vm.Master.Locked.Should().BeFalse(); // frischer Status ohne Refresh
    }

    [Fact]
    public async Task Save_ValidInput_SplitsBackends_AndWritesAtomically()
    {
        IReadOnlyDictionary<string, string?>? app = null;
        IReadOnlyList<(string Key, string Value, string? Category, string? Description)>? svc = null;
        _writer.Setup(w => w.SaveAtomicAsync(
                It.IsAny<IReadOnlyDictionary<string, string?>>(),
                It.IsAny<IReadOnlyList<(string, string, string?, string?)>>(),
                It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyDictionary<string, string?>, IReadOnlyList<(string, string, string?, string?)>, CancellationToken>(
                (a, s, _) => { app = a; svc = s; })
            .Returns(Task.CompletedTask);

        var settings = new Dictionary<string, string>
        {
            ["Firmenname"] = "Neu AG",
            ["FaHierarchyMaxTiefe"] = "5",
            ["FaHierarchyKommissionierlistenAktiv"] = "true",
            ["Sync:HierarchicalFaEnabled"] = "false",
        };

        var result = await Create().Save(settings);

        result.Should().BeOfType<RedirectToActionResult>();
        app!["Firmenname"].Should().Be("Neu AG");
        app["FaHierarchyMaxTiefe"].Should().Be("5");
        app["FaHierarchyKommissionierlistenAktiv"].Should().Be("true");
        svc!.Should().Contain(x => x.Key == "Sync:HierarchicalFaEnabled" && x.Value == "false");
    }

    [Fact]
    public async Task Save_InvalidInt_WritesNothing_AndKeepsEnteredValues()
    {
        var settings = new Dictionary<string, string> { ["FaHierarchyMaxTiefe"] = "abc", ["Firmenname"] = "X" };

        var ctrl = Create();
        var result = await ctrl.Save(settings);

        ctrl.ModelState.IsValid.Should().BeFalse();
        _writer.Verify(w => w.SaveAtomicAsync(It.IsAny<IReadOnlyDictionary<string, string?>>(),
            It.IsAny<IReadOnlyList<(string, string, string?, string?)>>(), It.IsAny<CancellationToken>()), Times.Never);

        var vm = result.Should().BeOfType<ViewResult>().Subject.Model
            .Should().BeOfType<StandortEinstellungenViewModel>().Subject;
        var maxTiefe = vm.Groups.SelectMany(g => g.Fields).First(f => f.Key == "FaHierarchyMaxTiefe");
        maxTiefe.Value.Should().Be("abc"); // eingegebener Wert bleibt sichtbar
    }

    [Fact]
    public async Task Save_MasterKeyInRequest_IsIgnored_NeverWritten()
    {
        IReadOnlyDictionary<string, string?>? app = null;
        IReadOnlyList<(string Key, string Value, string? Category, string? Description)>? svc = null;
        _writer.Setup(w => w.SaveAtomicAsync(
                It.IsAny<IReadOnlyDictionary<string, string?>>(),
                It.IsAny<IReadOnlyList<(string, string, string?, string?)>>(),
                It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyDictionary<string, string?>, IReadOnlyList<(string, string, string?, string?)>, CancellationToken>(
                (a, s, _) => { app = a; svc = s; })
            .Returns(Task.CompletedTask);

        // manipulierter Request: versucht, den Master mitzuschreiben
        var settings = new Dictionary<string, string>
        {
            [StandortSettingsCatalog.MasterKey] = "true",
            ["Firmenname"] = "X",
        };

        await Create().Save(settings);

        app!.Should().NotContainKey(StandortSettingsCatalog.MasterKey);
        svc!.Should().NotContain(x => x.Key == StandortSettingsCatalog.MasterKey);
        app.Should().ContainKey("Firmenname");
    }

    [Fact]
    public void Controller_IsAdminOnly()
    {
        typeof(StandortEinstellungenController)
            .GetCustomAttributes(typeof(RequireAdminAccessAttribute), inherit: true)
            .Should().NotBeEmpty();
    }
}
