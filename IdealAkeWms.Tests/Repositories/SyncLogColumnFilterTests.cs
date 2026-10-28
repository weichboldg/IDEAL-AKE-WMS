using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Services.SyncLogger;
using IdealAkeWms.Tests.Helpers;
using IdealAkeWms.Tests.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Tests.Repositories;

/// <summary>v1.70.0 (TS-102): Spaltenfilter des Aktivitaets-Protokolls in SQL vor dem Paging, deutsche Anzeigenamen.
/// Testdaten klein geschrieben (InMemory vergleicht ordinal, SQL nach Kollation – P-2).</summary>
public class SyncLogColumnFilterTests
{
    private static async Task<SyncLogRepository> Seed(ApplicationDbContext ctx)
    {
        // 30 Zeilen, Zeile i ist i Stunden alt (i = 1 neueste); nur Zeile 28 enthaelt "nadel".
        var basis = new DateTime(2026, 10, 3, 23, 0, 0);
        for (var i = 1; i <= 30; i++)
            ctx.SyncLogs.Add(new SyncLog
            {
                Timestamp = basis.AddHours(-i),
                Service = i % 3 == 0 ? SyncLogServices.OperationConfigSync : i % 3 == 1 ? SyncLogServices.ProductionWorkplaceSync : SyncLogServices.Lagerplatz,
                Level = i % 2 == 0 ? SyncLogLevel.Warning : SyncLogLevel.Info,
                Message = i == 28 ? "heuhaufen nadel" : $"zeile {i} konflikt",
                Reference = $"ref{i}",
            });
        await ctx.SaveChangesAsync();
        return new SyncLogRepository(ctx);
    }

    private static Dictionary<string, string> F(string key, string value) => new() { [key] = value };

    [Fact]
    public async Task Meldung_AufSeite2_ErscheintAufSeite1_TrefferzahlGefiltert()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = await Seed(ctx);

        var (rows, total) = await repo.GetPagedAsync(null, null, null, 1, 25, F("Message", "nadel"));

        rows.Should().ContainSingle().Which.Message.Should().Be("heuhaufen nadel");
        total.Should().Be(1);
        (await repo.GetPagedAsync(null, null, null, 1, 25)).TotalCount.Should().Be(30, "ohne Spaltenfilter wie bisher");
    }

    [Fact]
    public async Task Zeitpunkt_TagMonatJahrVergleichBereich_Tageweise_UnlesbarPositivLeerNegiertAlle()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = await Seed(ctx); // 03.10. 22:00 bis 02.10. 17:00

        async Task<int> N(string f) => (await repo.GetPagedAsync(null, null, null, 1, 100, F("Timestamp", f))).TotalCount;

        (await N("03.10.2026")).Should().Be(23);
        (await N("02.10.2026")).Should().Be(7);
        (await N("10.2026")).Should().Be(30);
        (await N("2026")).Should().Be(30);
        (await N(">=03.10.2026")).Should().Be(23);
        (await N("01.10.2026...02.10.2026")).Should().Be(7);
        (await N("32.13.2026")).Should().Be(0);
        (await N("!32.13.2026")).Should().Be(30);
    }

    [Fact]
    public async Task Aktivitaet_AnzeigenameOderTechnischerName_UndNegation()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = await Seed(ctx);

        async Task<List<SyncLog>> R(string f) => (await repo.GetPagedAsync(null, null, null, 1, 100, F("Service", f))).Rows;

        (await R("abgleich")).Should().HaveCount(10).And.OnlyContain(x => x.Service == SyncLogServices.OperationConfigSync);
        (await R("OperationConfig%")).Should().HaveCount(10).And.OnlyContain(x => x.Service == SyncLogServices.OperationConfigSync);
        (await R("!abgleich")).Should().HaveCount(20).And.NotContain(x => x.Service == SyncLogServices.OperationConfigSync);
        (await R("werkbank")).Should().HaveCount(10).And.OnlyContain(x => x.Service == SyncLogServices.ProductionWorkplaceSync);
        (await R("gibtsnicht")).Should().BeEmpty();
    }

    [Fact]
    public async Task Altname_ErscheintTechnisch_PositivNichtGetroffen_NegiertSchon()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.SyncLogs.Add(new SyncLog { Service = "FaWorkStepStructureDetection", Message = "alt" });
        await ctx.SaveChangesAsync();
        var repo = new SyncLogRepository(ctx);

        (await repo.GetPagedAsync(null, null, null, 1, 25, F("Service", "struktur"))).Rows.Should().BeEmpty();
        (await repo.GetPagedAsync(null, null, null, 1, 25, F("Service", "!abgleich"))).Rows.Should().ContainSingle();
        SyncLogServices.DisplayName("FaWorkStepStructureDetection").Should().Be("FaWorkStepStructureDetection");
    }

    [Fact]
    public async Task FilterkarteUndSpaltenfilter_WirkenZusammen()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = await Seed(ctx);

        var (rows, total) = await repo.GetPagedAsync(SyncLogServices.Lagerplatz, SyncLogLevel.Warning, null, 1, 100, F("Message", "konflikt"));

        total.Should().Be(rows.Count);
        rows.Should().NotBeEmpty().And.OnlyContain(x => x.Service == SyncLogServices.Lagerplatz && x.Level == SyncLogLevel.Warning
            && x.Message.Contains("konflikt"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task SeiteKleiner1_LiefertSeite1(int page)
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = await Seed(ctx);

        var (rows, _) = await repo.GetPagedAsync(null, null, null, page, 25);

        rows.Should().HaveCount(25);
        rows[0].Reference.Should().Be("ref1");
    }

    [Fact]
    public async Task UnbekannterSchluessel_ListeUnveraendert()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = await Seed(ctx);

        (await repo.GetPagedAsync(null, null, null, 1, 25, F("Unbekannt", "x"))).TotalCount.Should().Be(30);
    }

    private static ApplicationDbContext SqlCtx() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseSqlServer("Server=nonexistent-host-for-tests;Database=x;TrustServerCertificate=True;Connect Timeout=1").Options);

    [Fact]
    public void SqlUebersetzung_LikeOhneLower_EscapeUndMengeAufService()
    {
        using var ctx = SqlCtx();
        string Sql(string key, string f) => SyncLogRepository.ApplyColumnFilter(ctx.SyncLogs, key,
            ColumnFilterSyntax.Parse(f, ColumnFilterTypes.Of("SyncLog/Index", key))).ToQueryString();

        Sql("Message", "a_1").Should().Contain("LIKE").And.Contain("ESCAPE").And.NotContain("LOWER(");
        Sql("Reference", "ref%").Should().Contain("[Reference]").And.NotContain("LOWER(");
        Sql("Level", "=warning").Should().Contain("[Level]");
        Sql("Service", "abgleich").Should().Contain("[Service]").And.Contain("OperationConfigSync");
        Sql("Timestamp", "03.10.2026").Should().Contain("[Timestamp]");
    }

    [Fact]
    public void Anzeigenamen_VollstaendigNichtLeerEindeutig_Rueckfall()
    {
        SyncLogServices.All.Should().OnlyContain(s => !string.IsNullOrWhiteSpace(SyncLogServices.DisplayNames.GetValueOrDefault(s)));
        SyncLogServices.DisplayNames.Keys.Should().BeEquivalentTo(SyncLogServices.All);
        SyncLogServices.All.Select(SyncLogServices.DisplayName).Should().OnlyHaveUniqueItems();
        SyncLogServices.DisplayName("Unbekannt").Should().Be("Unbekannt");
    }

    [Fact]
    public async Task Controller_SpaltenfilterAusQuery_TrefferzahlGefiltert_AuswahlNachAnzeigename()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = await Seed(ctx);
        var ctrl = new SyncLogController(repo, new FakeCurrentUserService());
        var http = new DefaultHttpContext();
        http.Request.QueryString = new QueryString("?colf_Message=nadel");
        ctrl.ControllerContext = new ControllerContext { HttpContext = http };

        var result = await ctrl.Index(service: null, level: null, reference: null);

        var vm = result.Should().BeOfType<ViewResult>().Subject.Model.Should().BeOfType<SyncLogIndexViewModel>().Subject;
        vm.Entries.Should().ContainSingle();
        vm.Pagination.TotalCount.Should().Be(1);
        vm.AvailableServices.Select(SyncLogServices.DisplayName).Should().BeInAscendingOrder(StringComparer.OrdinalIgnoreCase);
        vm.AvailableServices.Should().BeEquivalentTo(SyncLogServices.All);
    }

    [Fact]
    public void Controller_OhneInMemorySpaltenfilter()
    {
        var src = File.ReadAllText(Path.Combine(ColumnFilterSyntaxQuelltextTests.Root(), "IdealAkeWms", "Controllers", "SyncLogController.cs"));
        src.Should().NotContain("ColumnFilterHelper.Apply").And.NotContain("SyncLogColumnMap");
    }
}
