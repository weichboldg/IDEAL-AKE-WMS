using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Services.SyncLogger;
using IdealAkeWms.Tests.Helpers;
using IDEALAKEWMSService.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace IDEALAKEWMSService.Tests.Services;

/// <summary>v1.99.70 (Spec 2026-11-25-kommissionierauftraege-nachtrag-nachts F1–F5, TS-191): Lauf „Kommissionier-Nachtrag“ – Termin ueber
/// den Startmerker im Aktivitaetsprotokoll, DryRun = kein Lauf, je FA eigener Scope, Fehler eines FA bricht den Lauf nicht ab.</summary>
public class KommissionierNachtragServiceTests
{
    private readonly ApplicationDbContext _db = TestDbContextFactory.Create();
    private readonly FakeSyncLogger _log = new();
    private readonly Mock<IBomRepository> _bom = new();

    private ServiceProvider Provider(bool mitBomMock = true)
    {
        var s = new ServiceCollection();
        s.AddLogging();
        s.AddMemoryCache();
        s.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:OseonConnection"] = "Server=x;" }).Build());
        s.AddSingleton(_db);
        s.AddSingleton<ISyncLogger>(_log);
        s.AddScoped<IProductionOrderPickingStatusRepository, ProductionOrderPickingStatusRepository>();
        KommissionierNachtragService.Registrieren(s);
        if (mitBomMock) s.AddScoped(_ => _bom.Object);
        return s.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }

    private async Task SeedAsync()
    {
        _db.PickingAreas.Add(new PickingArea { Id = 1, Name = "Lager", Kuerzel = "L", IsActive = true, IsDefault = true, CreatedBy = "t", CreatedByWindows = "t" });
        _db.ProductionOrders.AddRange(
            new ProductionOrder { Id = 1, OrderNumber = "FA1", SubOrderNumber = "FA1", ArticleNumber = "BAD", Quantity = 1, CreatedBy = "t", CreatedByWindows = "t" },
            new ProductionOrder { Id = 2, OrderNumber = "FA2", SubOrderNumber = "FA2", ArticleNumber = "X", Quantity = 1, CreatedBy = "t", CreatedByWindows = "t" },
            new ProductionOrder { Id = 3, OrderNumber = "FA3", SubOrderNumber = "FA3", ArticleNumber = "X", Quantity = 1, CreatedBy = "t", CreatedByWindows = "t" });
        _db.ProductionOrderPickingStatuses.AddRange(
            new ProductionOrderPickingStatus { ProductionOrderId = 1, IsReleasedForPicking = true, PickingPriority = 1, CreatedBy = "t", CreatedByWindows = "t" },
            new ProductionOrderPickingStatus { ProductionOrderId = 2, IsReleasedForPicking = true, PickingPriority = 2, CreatedBy = "t", CreatedByWindows = "t" },
            new ProductionOrderPickingStatus { ProductionOrderId = 3, IsReleasedForPicking = false, CreatedBy = "t", CreatedByWindows = "t" });
        await _db.SaveChangesAsync();
        _bom.Setup(b => b.GetBomItemsAsync(It.Is<BomKey>(k => k.ArticleNumber == "BAD"), It.IsAny<BomScope>()))
            .ThrowsAsync(new InvalidOperationException("Sage-View nicht lesbar"));
        _bom.Setup(b => b.GetBomItemsAsync(It.Is<BomKey>(k => k.ArticleNumber == "X"), It.IsAny<BomScope>()))
            .ReturnsAsync(new BomQueryResult(new List<BomItem>
            {
                new() { Artikelnummer = "X", Ressourcenummer = "A1", Position = "10", Menge = 2m },
                new() { Artikelnummer = "X", Ressourcenummer = "A2", Position = "20", Menge = 1m },
            }, BomDataSources.Sage));
    }

    [Fact]
    public async Task Lauf_FehlerEinesFa_NaechsterLaeuft_ZaehlerUndDienstName()
    {
        await SeedAsync();
        using var sp = Provider();

        await sp.CreateScope().ServiceProvider.GetRequiredService<KommissionierNachtragService>().RunAsync();

        var run = _log.Runs.Single();
        run.ServiceName.Should().Be(SyncLogServices.KommissionierNachtrag);
        run.FinishedSuccess.Should().BeTrue();
        run.FinalCounts!["fa_geprueft"].Should().Be(2); // FA3 nicht freigegeben
        (run.FinalCounts["fehler"], run.FinalCounts["fa_mit_nachtrag"], run.FinalCounts["auftraege_angelegt"], run.FinalCounts["zeilen_angelegt"])
            .Should().Be((1, 1, 1, 2));
        run.Events.Should().Contain(e => e.Level == SyncLogLevel.Warning && e.Message.StartsWith("FA FA1: Fehler – Sage-View nicht lesbar"));
        run.Events.Should().Contain(e => e.Level == SyncLogLevel.Info && e.Message == $"FA FA2: KO-{DateTime.Now.Year}-00001 (2 Zeilen)");
        _db.PickingOrders.Single().CreatedBy.Should().Be(KommissionierNachtragService.ServiceUser);

        // idempotent: zweiter Lauf legt nichts an
        await sp.CreateScope().ServiceProvider.GetRequiredService<KommissionierNachtragService>().RunAsync();
        (_log.Runs[1].FinalCounts!["auftraege_angelegt"], _log.Runs[1].FinalCounts!["ohne_rest"]).Should().Be((0, 1));
    }

    [Fact]
    public async Task Faellig_UeberStartmerker_DryRunLaeuftNicht()
    {
        await SeedAsync();
        using var sp = Provider();
        var svc = sp.CreateScope().ServiceProvider.GetRequiredService<KommissionierNachtragService>();
        var jetzt = new DateTime(2026, 11, 25, 3, 0, 0);

        (await svc.RunWennFaelligAsync(dryRun: true, jetzt)).Should().BeFalse();
        _log.Runs.Should().BeEmpty();

        // Startmerker nach dem heutigen 2-Uhr-Termin → nicht faellig
        _db.SyncLogs.Add(new SyncLog { Service = SyncLogServices.KommissionierNachtrag, Message = "Run gestartet", Timestamp = jetzt.AddMinutes(-30) });
        await _db.SaveChangesAsync();
        (await svc.RunWennFaelligAsync(false, jetzt)).Should().BeFalse();
        _log.Runs.Should().BeEmpty();

        // Merker von gestern → verpasster Termin wird nachgeholt (Default-Uhrzeit 2, Default an)
        _db.SyncLogs.RemoveRange(_db.SyncLogs);
        _db.SyncLogs.Add(new SyncLog { Service = SyncLogServices.KommissionierNachtrag, Message = "Run gestartet", Timestamp = jetzt.AddDays(-1) });
        await _db.SaveChangesAsync();
        (await svc.RunWennFaelligAsync(false, jetzt)).Should().BeTrue();
        _log.Runs.Should().ContainSingle();
    }

    [Fact]
    public async Task DI_LoestAuf_LogNameRegistriert_SyncLoggerLetzterParameter_DienstBenutzer()
    {
        using var sp = Provider(mitBomMock: false);
        using var scope = sp.CreateScope();
        scope.ServiceProvider.GetRequiredService<KommissionierNachtragService>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<PickingOrderWorkflow>().Should().NotBeNull();
        var user = scope.ServiceProvider.GetRequiredService<ICurrentUserService>();
        (user.GetDisplayName(), user.GetWindowsUserName()).Should().Be(("KommissionierNachtrag", "KommissionierNachtrag"));
        await user.Invoking(u => u.IsAdminAsync()).Should().ThrowAsync<NotSupportedException>();
        SyncLogServices.All.Should().Contain(SyncLogServices.KommissionierNachtrag);
        SyncLogServices.DisplayNames[SyncLogServices.KommissionierNachtrag].Should().Be("Kommissionier-Nachtrag");
        typeof(KommissionierNachtragService).GetConstructors().Single().GetParameters().Last().ParameterType.Should().Be(typeof(ISyncLogger));
        ServiceSettingDefinitions.All.Select(d => d.Key).Should().Contain(new[] { KommissionierNachtragService.KeyEnabled, KommissionierNachtragService.KeyUhrzeit });
    }
}
