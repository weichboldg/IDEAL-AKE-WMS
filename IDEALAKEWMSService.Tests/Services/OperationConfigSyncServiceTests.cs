using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using IDEALAKEWMSService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IDEALAKEWMSService.Tests.Services;

/// <summary>
/// Arbeitsgang-Abgleich (v1.61.0, Spec 2026-10-16-arbeitsgang-stammdaten-abgleich-aus-sage-view). Reader gemockt, Anlage und
/// Kennzeichnung gegen InMemory. Sage-Read, Settings-DB, Unique-Index und Kollation = Manual-UAT (TS-94).
/// </summary>
public class OperationConfigSyncServiceTests
{
    private static readonly OperationConfigSyncSettings Defaults = new(null, null, false, 1);

    private static (OperationConfigSyncService Svc, FakeSyncLogger Log, Mock<ISageArbeitsplatzReader> Reader) Build(
        ApplicationDbContext ctx, bool sourceExists, params SageArbeitsplatzDto[] rows)
    {
        var reader = new Mock<ISageArbeitsplatzReader>();
        reader.Setup(r => r.GetActiveAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new SageArbeitsplatzReadResult(sourceExists, rows.ToList()));
        var log = new FakeSyncLogger();
        var svc = new OperationConfigSyncService(ctx, new ConfigurationBuilder().Build(), reader.Object,
            NullLogger<OperationConfigSyncService>.Instance, log);
        return (svc, log, reader);
    }

    private static SageArbeitsplatzDto Row(string? ag, string? bezeichnung = null, string nummer = "1") => new(nummer, bezeichnung, ag);

    private static OperationConfig Existing(string name, string source, DateTime? missing = null, int offset = 0, string? display = null) => new()
    {
        OperationName = name, DisplayName = display, DueDateOffsetDays = offset, Source = source, SageMissingSince = missing,
        IsExternalSystemRelevant = true, CreatedAt = new DateTime(2026, 1, 1), CreatedBy = "x", CreatedByWindows = "x",
    };

    private static async Task<ApplicationDbContext> Seed(params OperationConfig[] configs)
    {
        var ctx = TestDbContextFactory.Create();
        ctx.OperationConfigs.AddRange(configs);
        await ctx.SaveChangesAsync();
        return ctx;
    }

    // ----- Katalog / Quelle -----

    [Fact]
    public async Task Run_OhneEinstellung_LiestDefaultView_UndDefaultsStimmenMitKatalog()
    {
        using var ctx = TestDbContextFactory.Create();
        var (svc, _, reader) = Build(ctx, true, Row("KA", "Kanten"));

        await svc.RunAsync(dryRun: false); // ohne Settings-DB: Safe-Reads liefern null/false

        reader.Verify(r => r.GetActiveAsync("[vw_IDEAL_AKE_WMS_Arbeitsgaenge]", 1, It.IsAny<CancellationToken>()), Times.Once);
        var catalog = ServiceSettingDefinitions.All.ToDictionary(d => d.Key);
        OperationConfigSyncService.DefaultViewName.Should().Be(catalog["Sync:OperationConfigSyncViewName"].DefaultValue);
        OperationConfigSyncService.DefaultAusschlussliste.Should().Be(catalog["Sync:ProductionWorkplaceSyncAusschlussliste"].DefaultValue);
        catalog["Sync:OperationConfigSyncEnabled"].DefaultValue.Should().Be("false");
        catalog["Sync:OperationConfigSyncFremdsystemRelevant"].DefaultValue.Should().Be("false");
        foreach (var key in new[] { "Sync:OperationConfigSyncEnabled", "Sync:OperationConfigSyncViewName", "Sync:OperationConfigSyncFremdsystemRelevant" })
            catalog[key].Description.Should().NotStartWith("IDEAL:", "standortneutral (Antwort 4)");
    }

    // ----- Anlage (FA 2) -----

    [Fact]
    public async Task Run_NeuesKuerzel_WirdMitDefaultsAngelegt()
    {
        using var ctx = TestDbContextFactory.Create();
        var (svc, log, _) = Build(ctx, true, Row(" KA ", " Kanten "));

        await svc.RunAsync(false, Defaults, default);

        var c = await ctx.OperationConfigs.SingleAsync();
        c.OperationName.Should().Be("KA");
        c.DisplayName.Should().Be("Kanten");
        c.DueDateOffsetDays.Should().Be(0);
        c.IsExternalSystemRelevant.Should().BeFalse("Key fehlt -> false, obwohl der Model-Default true ist");
        c.Source.Should().Be(OperationConfigSource.Sage);
        c.SageMissingSince.Should().BeNull();
        c.CreatedBy.Should().Be("OperationConfigSync");
        c.CreatedByWindows.Should().Be("OperationConfigSync");
        var run = log.Runs.Single();
        run.ServiceName.Should().Be("OperationConfigSync");
        run.FinishedSuccess.Should().BeTrue();
        run.FinalCounts!["neu"].Should().Be(1);
        run.Events.Should().Contain(e => e.Level == "Info" && e.Message == "angelegt: KA (Kanten)");
    }

    [Fact]
    public async Task Run_FremdsystemRelevantTrue_SetztFlagNurBeiNeuanlage()
    {
        using var ctx = await Seed(Existing("ST", OperationConfigSource.ExternalSystem));
        ctx.OperationConfigs.Single().IsExternalSystemRelevant = false;
        await ctx.SaveChangesAsync();
        var (svc, _, _) = Build(ctx, true, Row("KA"), Row("ST"));

        await svc.RunAsync(false, Defaults with { FremdsystemRelevant = true }, default);

        (await ctx.OperationConfigs.SingleAsync(c => c.OperationName == "KA")).IsExternalSystemRelevant.Should().BeTrue();
        (await ctx.OperationConfigs.SingleAsync(c => c.OperationName == "ST")).IsExternalSystemRelevant.Should().BeFalse();
    }

    [Fact]
    public async Task Run_VorhandenMitLeerzeichenUndKleinschreibung_KeinInsert_KeineAenderung()
    {
        using var ctx = await Seed(Existing(" ka ", OperationConfigSource.Sage, offset: 3, display: "Hand"));
        var (svc, log, _) = Build(ctx, true, Row("KA", "Kanten"));

        await svc.RunAsync(false, Defaults, default);

        var c = await ctx.OperationConfigs.SingleAsync();
        c.DisplayName.Should().Be("Hand");
        c.DueDateOffsetDays.Should().Be(3);
        c.IsExternalSystemRelevant.Should().BeTrue();
        c.ModifiedAt.Should().BeNull();
        c.ModifiedBy.Should().BeNull();
        log.Runs.Single().FinalCounts!["vorhanden"].Should().Be(1);
    }

    [Fact]
    public async Task Run_UmlautKuerzel_VorhandenKleingeschrieben_KeinInsert()
    {
        using var ctx = await Seed(Existing("sä", OperationConfigSource.Sage));
        var (svc, _, _) = Build(ctx, true, Row("SÄ", "Sägen"));

        await svc.RunAsync(false, Defaults, default);

        (await ctx.OperationConfigs.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Run_Ausschlussliste_KatalogDefaultGreift_WederAngelegtNochGekennzeichnet()
    {
        using var ctx = await Seed(Existing("BS", OperationConfigSource.Manual));
        var (svc, log, _) = Build(ctx, true, Row("STO"), Row("x01"), Row("KA"));

        await svc.RunAsync(false, Defaults, default); // Ausschlussliste null -> Katalog-Default

        (await ctx.OperationConfigs.Select(c => c.OperationName).ToListAsync()).Should().BeEquivalentTo("BS", "KA");
        (await ctx.OperationConfigs.SingleAsync(c => c.OperationName == "BS")).SageMissingSince.Should().BeNull();
        var counts = log.Runs.Single().FinalCounts!;
        counts["uebersprungen"].Should().Be(2);
        counts["nicht_in_sage"].Should().Be(0);
    }

    [Fact]
    public async Task Run_AusschlusslisteBewusstLeer_GiltLeer()
    {
        using var ctx = TestDbContextFactory.Create();
        var (svc, _, _) = Build(ctx, true, Row("STO"));

        await svc.RunAsync(false, Defaults with { Ausschlussliste = "" }, default);

        (await ctx.OperationConfigs.SingleAsync()).OperationName.Should().Be("STO");
    }

    [Fact]
    public void Plan_Bezeichnung_EindeutigGesetzt_MehrdeutigLeerMitWarnung()
    {
        var plan = OperationConfigSyncService.Plan(
            new[] { Row("KA", "Kanten", "1"), Row("KA", "Kanten", "2"), Row("PL", "Plasma", "3"), Row("PL", "Plasma 2", "4") },
            Array.Empty<ExistingOperationConfig>(), new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        plan.New.Should().BeEquivalentTo(new[] { ("KA", (string?)"Kanten"), ("PL", (string?)null) });
        plan.NameMehrdeutig.Should().Be(1);
        plan.Warnings.Should().ContainSingle(w => w == "Kürzel PL: mehrere Bezeichnungen (Plasma / Plasma 2) — Anzeigename leer gelassen.");
    }

    [Fact]
    public void Plan_Schreibvarianten_EinEintrag_OrdinalKleinsteSchreibweise_MitWarnung()
    {
        var plan = OperationConfigSyncService.Plan(new[] { Row("ka", "Kanten"), Row("KA", "Kanten") },
            Array.Empty<ExistingOperationConfig>(), new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        plan.New.Should().ContainSingle().Which.OperationName.Should().Be("KA");
        plan.Warnings.Should().ContainSingle(w => w.StartsWith("Kürzel in Sage in mehreren Schreibweisen: KA / ka"));
    }

    [Fact]
    public void Plan_LeeresUndUeberlangesKuerzel_Uebersprungen()
    {
        var plan = OperationConfigSyncService.Plan(new[] { Row(null), Row("  "), Row(new string('X', 101)) },
            Array.Empty<ExistingOperationConfig>(), new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        plan.New.Should().BeEmpty();
        plan.Uebersprungen.Should().Be(3);
        plan.Warnings.Should().ContainSingle(w => w.Contains("länger als 100 Zeichen"));
    }

    // ----- Kennzeichnung (FA 4) -----

    [Fact]
    public async Task Run_KennzeichnenNurEinmalMelden_DannZuruecksetzen()
    {
        using var ctx = await Seed(Existing("ZZTEST", OperationConfigSource.Manual), Existing("SL", OperationConfigSource.Sage));
        var (svc, log, reader) = Build(ctx, true, Row("KA"));

        await svc.RunAsync(false, Defaults, default);
        var first = (await ctx.OperationConfigs.SingleAsync(c => c.OperationName == "ZZTEST")).SageMissingSince;
        first.Should().NotBeNull();
        (await ctx.OperationConfigs.SingleAsync(c => c.OperationName == "SL")).SageMissingSince.Should().NotBeNull("Sage-Kuerzel verschwunden");
        log.Runs[0].Events.Should().ContainSingle(e => e.Level == "Warning" && e.Message == "neu nicht in Sage: ZZTEST, SL");
        log.Runs[0].FinalCounts!["nicht_in_sage"].Should().Be(2);

        await svc.RunAsync(false, Defaults, default);
        (await ctx.OperationConfigs.SingleAsync(c => c.OperationName == "ZZTEST")).SageMissingSince.Should().Be(first);
        log.Runs[1].Events.Should().NotContain(e => e.Message.StartsWith("neu nicht in Sage"));
        log.Runs[1].FinalCounts!["nicht_in_sage"].Should().Be(2);

        reader.Setup(r => r.GetActiveAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new SageArbeitsplatzReadResult(true, new List<SageArbeitsplatzDto> { Row("KA"), Row("SL") }));
        await svc.RunAsync(false, Defaults, default);
        (await ctx.OperationConfigs.SingleAsync(c => c.OperationName == "SL")).SageMissingSince.Should().BeNull();
        log.Runs[2].Events.Should().Contain(e => e.Message == "wieder in Sage: SL");
        log.Runs[2].FinalCounts!["wieder_in_sage"].Should().Be(1);
        log.Runs[2].FinalCounts!["nicht_in_sage"].Should().Be(1);
    }

    [Fact]
    public async Task Run_Fremdsystem_NieGekennzeichnet_VorbelegterZeitstempelStillZurueckgesetzt()
    {
        using var ctx = await Seed(Existing("BR", OperationConfigSource.ExternalSystem),
                                   Existing("RE", OperationConfigSource.ExternalSystem, new DateTime(2026, 1, 1)));
        var (svc, log, _) = Build(ctx, true, Row("KA"));

        await svc.RunAsync(false, Defaults, default);

        (await ctx.OperationConfigs.SingleAsync(c => c.OperationName == "BR")).SageMissingSince.Should().BeNull();
        var re = await ctx.OperationConfigs.SingleAsync(c => c.OperationName == "RE");
        re.SageMissingSince.Should().BeNull();
        re.ModifiedBy.Should().Be("OperationConfigSync");
        var run = log.Runs.Single();
        run.FinalCounts!["nicht_in_sage"].Should().Be(0);
        run.FinalCounts!["wieder_in_sage"].Should().Be(0);
        run.Events.Should().NotContain(e => e.Message.Contains("nicht in Sage") || e.Message.Contains("wieder in Sage"));
    }

    // ----- Quelle-Wechsel (FA 6, Antwort 5) -----

    [Fact]
    public async Task Run_ManuellInSage_WechseltAufSage_FremdsystemNie()
    {
        using var ctx = await Seed(Existing("KA", OperationConfigSource.Manual, offset: 2, display: "Hand"),
                                   Existing("ST", OperationConfigSource.ExternalSystem));
        var (svc, log, _) = Build(ctx, true, Row("KA", "Kanten"), Row("ST", "Stanzen"));

        await svc.RunAsync(false, Defaults, default);

        var ka = await ctx.OperationConfigs.SingleAsync(c => c.OperationName == "KA");
        ka.Source.Should().Be(OperationConfigSource.Sage);
        ka.DisplayName.Should().Be("Hand");
        ka.DueDateOffsetDays.Should().Be(2);
        ka.ModifiedBy.Should().Be("OperationConfigSync");
        (await ctx.OperationConfigs.SingleAsync(c => c.OperationName == "ST")).Source.Should().Be(OperationConfigSource.ExternalSystem);
        log.Runs[0].FinalCounts!["quelle_sage"].Should().Be(1);
        log.Runs[0].Events.Should().Contain(e => e.Message == "Quelle Manuell → Sage: KA");

        await svc.RunAsync(false, Defaults, default);
        log.Runs[1].FinalCounts!["quelle_sage"].Should().Be(0);
        log.Runs[1].Events.Should().NotContain(e => e.Message.StartsWith("Quelle Manuell"));
    }

    // ----- Fehler / Randfaelle (FA 2.7, FA 5) -----

    [Fact]
    public async Task Run_EintragWaehrendDesLaufsGeloescht_WarnungFehlerKeinAbbruch()
    {
        using var factory = new TestApplicationDbContextFactory();
        await using (var seed = factory.CreateDbContext())
        {
            seed.OperationConfigs.AddRange(Existing("ZZA", OperationConfigSource.Manual), Existing("ZZB", OperationConfigSource.Manual));
            await seed.SaveChangesAsync();
        }
        await using var ctx = factory.CreateDbContext();
        var reader = new Mock<ISageArbeitsplatzReader>();
        reader.Setup(r => r.GetActiveAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
              .Returns(async () =>
              {
                  await using var other = factory.CreateDbContext(); // Loeschen im Reiter waehrend des Laufs
                  other.OperationConfigs.Remove(other.OperationConfigs.Single(c => c.OperationName == "ZZA"));
                  await other.SaveChangesAsync();
                  return new SageArbeitsplatzReadResult(true, new List<SageArbeitsplatzDto> { Row("KA") });
              });
        var log = new FakeSyncLogger();
        var svc = new OperationConfigSyncService(ctx, new ConfigurationBuilder().Build(), reader.Object,
            NullLogger<OperationConfigSyncService>.Instance, log);

        var act = () => svc.RunAsync(false, Defaults, default);

        await act.Should().NotThrowAsync();
        var run = log.Runs.Single();
        run.FinishedSuccess.Should().BeTrue();
        run.FinishedFailed.Should().BeFalse();
        run.FinalCounts!["fehler"].Should().Be(1);
        run.FinalCounts!["nicht_in_sage"].Should().Be(1);
        run.Events.Should().Contain(e => e.Level == "Warning" && e.Message == "Eintrag ZZA inzwischen gelöscht — übersprungen.");
        await using var check = factory.CreateDbContext();
        (await check.OperationConfigs.SingleAsync(c => c.OperationName == "ZZB")).SageMissingSince.Should().NotBeNull();
        (await check.OperationConfigs.SingleAsync(c => c.OperationName == "KA")).Source.Should().Be(OperationConfigSource.Sage);
    }

    [Fact]
    public async Task Run_ManuellGekennzeichnetInSage_WaehrendDesLaufsGeloescht_KeinFalschesWiederInSage()
    {
        // QA 2026-10-16: Quelle-Wechsel UND Zuruecksetzen fuer denselben Eintrag; scheitert der Wechsel (geloescht), darf das
        // Zuruecksetzen nicht auf dem abgehaengten Objekt "erfolgreich" zaehlen.
        using var factory = new TestApplicationDbContextFactory();
        await using (var seed = factory.CreateDbContext())
        {
            seed.OperationConfigs.Add(Existing("ZZA", OperationConfigSource.Manual, new DateTime(2026, 10, 1)));
            await seed.SaveChangesAsync();
        }
        await using var ctx = factory.CreateDbContext();
        var reader = new Mock<ISageArbeitsplatzReader>();
        reader.Setup(r => r.GetActiveAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
              .Returns(async () =>
              {
                  await using var other = factory.CreateDbContext();
                  other.OperationConfigs.Remove(other.OperationConfigs.Single(c => c.OperationName == "ZZA"));
                  await other.SaveChangesAsync();
                  return new SageArbeitsplatzReadResult(true, new List<SageArbeitsplatzDto> { Row("ZZA") });
              });
        var log = new FakeSyncLogger();
        var svc = new OperationConfigSyncService(ctx, new ConfigurationBuilder().Build(), reader.Object,
            NullLogger<OperationConfigSyncService>.Instance, log);

        await svc.RunAsync(false, Defaults, default);

        var run = log.Runs.Single();
        run.FinalCounts!["fehler"].Should().Be(1);
        run.FinalCounts!["quelle_sage"].Should().Be(0);
        run.FinalCounts!["wieder_in_sage"].Should().Be(0);
        run.Events.Should().NotContain(e => e.Message.StartsWith("wieder in Sage"));
    }

    [Fact]
    public async Task Run_QuelleFehlt_WarnungSkip_OhneMutation()
    {
        using var ctx = await Seed(Existing("ZZTEST", OperationConfigSource.Manual));
        var (svc, log, _) = Build(ctx, false, Row("KA"));

        var result = await svc.RunAsync(false, Defaults, default);

        result.ErrorDetails.Should().Be("Quelle nicht vorhanden.");
        (await ctx.OperationConfigs.CountAsync()).Should().Be(1);
        (await ctx.OperationConfigs.SingleAsync()).SageMissingSince.Should().BeNull();
        var run = log.Runs.Single();
        run.FinishedSuccess.Should().BeTrue();
        run.FinalMessageSuffix.Should().Be("Quelle nicht vorhanden");
        run.Events.Should().ContainSingle(e => e.Level == "Warning" && e.Message.Contains("Lauf übersprungen"));
    }

    [Fact]
    public async Task Run_QuelleLeer_KeineAnlageKeineKennzeichnung()
    {
        using var ctx = await Seed(Existing("ZZTEST", OperationConfigSource.Manual));
        var (svc, log, _) = Build(ctx, true);

        await svc.RunAsync(false, Defaults, default);

        (await ctx.OperationConfigs.SingleAsync()).SageMissingSince.Should().BeNull();
        log.Runs.Single().FinalMessageSuffix.Should().Be("Quelle leer");
    }

    [Fact]
    public async Task Run_UngueltigerViewName_FehlerOhneSql()
    {
        using var ctx = TestDbContextFactory.Create();
        var (svc, log, reader) = Build(ctx, true, Row("KA"));

        var act = () => svc.RunAsync(false, Defaults with { ViewName = "vw;DROP" }, default);

        await act.Should().ThrowAsync<InvalidOperationException>();
        reader.Verify(r => r.GetActiveAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        log.Runs.Single().FinishedFailed.Should().BeTrue();
    }

    [Fact]
    public async Task Run_DryRun_ZaehltOhneMutation()
    {
        using var ctx = await Seed(Existing("ZZTEST", OperationConfigSource.Manual), Existing("PL", OperationConfigSource.Manual));
        var (svc, log, _) = Build(ctx, true, Row("KA"), Row("PL"));

        await svc.RunAsync(true, Defaults, default);

        (await ctx.OperationConfigs.CountAsync()).Should().Be(2);
        (await ctx.OperationConfigs.SingleAsync(c => c.OperationName == "ZZTEST")).SageMissingSince.Should().BeNull();
        (await ctx.OperationConfigs.SingleAsync(c => c.OperationName == "PL")).Source.Should().Be(OperationConfigSource.Manual);
        var counts = log.Runs.Single().FinalCounts!;
        counts["neu"].Should().Be(1);
        counts["nicht_in_sage"].Should().Be(1);
        counts["quelle_sage"].Should().Be(1);
        log.Runs.Single().FinalMessageSuffix.Should().Be("[DryRun]");
    }
}
