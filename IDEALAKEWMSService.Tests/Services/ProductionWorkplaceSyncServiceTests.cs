using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using IDEALAKEWMSService.Common;
using IDEALAKEWMSService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IDEALAKEWMSService.Tests.Services;

/// <summary>
/// IDEAL Baustein a (v1.44.0): Werkbank-Anlage aus Sage-Arbeitsplaetzen. Reader gemockt, Upsert gegen
/// InMemory. Mandant/Ausschlussliste (ServiceSettings, DB-getrieben) + eindeutiger Index (InMemory
/// erzwingt UNIQUE nicht) + Sammelmail = Manual-UAT (TS-77). Getestet ist die reine Upsert-Logik.
/// </summary>
public class ProductionWorkplaceSyncServiceTests
{
    private static (ProductionWorkplaceSyncService Svc, FakeSyncLogger Log) Build(
        ApplicationDbContext ctx, params SageArbeitsplatzDto[] sageRows)
        => Build(ctx, new Mock<ISageArbeitsplatzReader>(), true, sageRows);

    private static (ProductionWorkplaceSyncService Svc, FakeSyncLogger Log) Build(
        ApplicationDbContext ctx, Mock<ISageArbeitsplatzReader> reader, bool sourceExists, params SageArbeitsplatzDto[] sageRows)
    {
        reader.Setup(r => r.GetActiveAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new SageArbeitsplatzReadResult(sourceExists, sageRows.ToList()));
        var log = new FakeSyncLogger();
        var svc = new ProductionWorkplaceSyncService(
            ctx, new ConfigurationBuilder().Build(), new Mock<IMailService>().Object,
            reader.Object, new UnknownArbeitsplatzState(),
            NullLogger<ProductionWorkplaceSyncService>.Instance, log);
        return (svc, log);
    }

    private static ProductionWorkplace Existing(string nummer, string name, string? code) => new()
    {
        SageArbeitsplatznummer = nummer, Name = name, ArbeitsschrittCode = code,
        CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
    };

    // ----- Quelle (Spec 2026-10-14-ideal-sage-views-umstellung) -----

    [Fact]
    public async Task Run_OhneEinstellung_LiestDefaultTabelle()
    {
        using var ctx = TestDbContextFactory.Create();
        var reader = new Mock<ISageArbeitsplatzReader>();
        var (svc, _) = Build(ctx, reader, true);

        await svc.RunAsync(dryRun: false);

        // Keine DB-Einstellung (Test ohne Settings-DB) -> Fallback = Katalog-Default, geklammert.
        reader.Verify(r => r.GetActiveAsync("[KHKPpsArbeitsplaetze]", It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        ProductionWorkplaceSyncService.DefaultSource.Should().Be(
            IdealAkeWms.Models.ServiceSettingDefinitions.All.Single(d => d.Key == "Sync:ProductionWorkplaceSyncViewName").DefaultValue);
    }

    [Fact]
    public async Task Run_QuelleFehlt_WarnungUndSkip_OhneMutationOhneException()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.ProductionWorkplaces.Add(Existing("2300", "Kanterei W1", "KA"));
        await ctx.SaveChangesAsync();
        var (svc, log) = Build(ctx, new Mock<ISageArbeitsplatzReader>(), false, new SageArbeitsplatzDto("4711", "Neu", "NE"));

        var result = await svc.RunAsync(dryRun: false);

        result.ErrorDetails.Should().Be("Quelle nicht vorhanden.");
        (await ctx.ProductionWorkplaces.CountAsync()).Should().Be(1);
        var run = log.Runs.Single();
        run.FinishedSuccess.Should().BeTrue();
        run.FinishedFailed.Should().BeFalse();
        run.FinalMessageSuffix.Should().Be("Quelle nicht vorhanden");
        run.FinalCounts!["neu"].Should().Be(0);
        run.Events.Should().ContainSingle(e => e.Level == "Warning"
            && e.Message.Contains("[KHKPpsArbeitsplaetze] nicht vorhanden oder keine Leseberechtigung"));
    }

    [Fact]
    public async Task Run_NeuerArbeitsplatz_WirdAngelegt()
    {
        using var ctx = TestDbContextFactory.Create();
        var (svc, log) = Build(ctx, new SageArbeitsplatzDto("2300", "Kanterei W1", "KA"));

        await svc.RunAsync(dryRun: false);

        var wp = await ctx.ProductionWorkplaces.SingleAsync();
        wp.SageArbeitsplatznummer.Should().Be("2300");
        wp.Name.Should().Be("Kanterei W1");
        wp.ArbeitsschrittCode.Should().Be("KA");
        wp.BdeAktiv.Should().BeFalse();
        wp.CreatedBy.Should().Be("ProductionWorkplaceSync");
        log.Runs.Single().FinalCounts!["neu"].Should().Be(1);
    }

    [Fact]
    public async Task Run_OhneKuerzel_WirdUebersprungen()
    {
        using var ctx = TestDbContextFactory.Create();
        var (svc, log) = Build(ctx, new SageArbeitsplatzDto("9999", "Reserve", null));

        await svc.RunAsync(dryRun: false);

        (await ctx.ProductionWorkplaces.AnyAsync()).Should().BeFalse();
        log.Runs.Single().FinalCounts!["uebersprungen"].Should().Be(1);
    }

    [Fact]
    public async Task Run_NummerUndKuerzelGetrimmt_TrifftBestehendeZeile()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.ProductionWorkplaces.Add(Existing("2300", "Kanterei W1", "KA"));
        await ctx.SaveChangesAsync();
        var (svc, log) = Build(ctx, new SageArbeitsplatzDto("  2300  ", "Kanterei W1", "  KA  "));

        await svc.RunAsync(dryRun: false);

        (await ctx.ProductionWorkplaces.CountAsync()).Should().Be(1); // keine Dublette
        log.Runs.Single().FinalCounts!["neu"].Should().Be(0);
    }

    [Fact]
    public async Task Run_Abweichung_WirdUeberschriebenUndGemeldet()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.ProductionWorkplaces.Add(Existing("2300", "Alt", "XX"));
        await ctx.SaveChangesAsync();
        var (svc, log) = Build(ctx, new SageArbeitsplatzDto("2300", "Kanterei W1", "KA"));

        await svc.RunAsync(dryRun: false);

        var wp = await ctx.ProductionWorkplaces.SingleAsync();
        wp.Name.Should().Be("Kanterei W1");
        wp.ArbeitsschrittCode.Should().Be("KA");
        wp.ModifiedBy.Should().Be("ProductionWorkplaceSync");
        var counts = log.Runs.Single().FinalCounts!;
        counts["aktualisiert"].Should().Be(1);
        counts["abweichung"].Should().Be(1);
        log.Runs.Single().Events.Should().Contain(e => e.Level == "Warning" && e.Message.Contains("weicht ab"));
    }

    [Fact]
    public async Task Run_KeineAbweichung_KeinSchreiben()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.ProductionWorkplaces.Add(Existing("2300", "Kanterei W1", "KA"));
        await ctx.SaveChangesAsync();
        var (svc, log) = Build(ctx, new SageArbeitsplatzDto("2300", "Kanterei W1", "KA"));

        await svc.RunAsync(dryRun: false);

        var counts = log.Runs.Single().FinalCounts!;
        counts["neu"].Should().Be(0);
        counts["aktualisiert"].Should().Be(0);
    }

    [Fact]
    public async Task Run_DryRun_ZaehltOhneSchreiben()
    {
        using var ctx = TestDbContextFactory.Create();
        var (svc, log) = Build(ctx, new SageArbeitsplatzDto("2300", "Kanterei W1", "KA"));

        await svc.RunAsync(dryRun: true);

        (await ctx.ProductionWorkplaces.AnyAsync()).Should().BeFalse();
        log.Runs.Single().FinalCounts!["neu"].Should().Be(1);
        log.Runs.Single().FinalMessageSuffix.Should().Be("[DryRun]");
    }
}
