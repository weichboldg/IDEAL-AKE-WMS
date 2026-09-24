using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace IdealAkeWms.Tests.Repositories;

/// <summary>FA-Struktur-Arbeitsgaenge (Spec 2026-09-23): Existenz-Check (Stufe 1) + Modal-Details (Stufe 2).</summary>
public class WorkOperationRepositoryFaStructureTests
{
    private readonly ITestOutputHelper _output;
    public WorkOperationRepositoryFaStructureTests(ITestOutputHelper output) => _output = output;

    private static ProductionOrder Order(string hauptFa, string subFa) => new()
    {
        OrderNumber = hauptFa, SubOrderNumber = subFa, ArticleNumber = "ART", Description1 = "x",
        CreatedAt = DateTime.Now, CreatedBy = "Test", CreatedByWindows = @"TEST\user"
    };

    private static WorkOperation Op(ProductionOrder po, string code, int seq, ProductionWorkplace? wp = null) => new()
    {
        ProductionOrder = po, OperationNumber = code, Name = wp?.Name ?? code, Sequence = seq, ProductionWorkplace = wp,
        CreatedAt = DateTime.Now, CreatedBy = "Test", CreatedByWindows = @"TEST\user"
    };

    [Fact]
    public async Task GetOrderNumbersWithWorkOperationsAsync_ReturnsOnlyRequestedOrdersWithOperations()
    {
        using var ctx = TestDbContextFactory.Create();
        var a1 = Order("100", "101"); var a2 = Order("100", "102");
        var b = Order("200", "201");  // ohne Arbeitsgang
        var c = Order("300", "301");  // mit Arbeitsgang, aber nicht angefragt
        ctx.ProductionOrders.AddRange(a1, a2, b, c);
        ctx.WorkOperations.AddRange(Op(a1, "KA", 1), Op(a2, "SW", 1), Op(c, "KA", 1));
        await ctx.SaveChangesAsync();

        var result = await new WorkOperationRepository(ctx).GetOrderNumbersWithWorkOperationsAsync(new[] { "100", "200" });

        result.Should().BeEquivalentTo(new[] { "100" });
    }

    [Fact]
    public async Task GetOrderNumbersWithWorkOperationsAsync_EmptyInput_ReturnsEmpty()
    {
        using var ctx = TestDbContextFactory.Create();
        (await new WorkOperationRepository(ctx).GetOrderNumbersWithWorkOperationsAsync(Array.Empty<string>()))
            .Should().BeEmpty();
    }

    [Fact]
    public async Task GetByOrderNumberWithWorkplaceAsync_LoadsWorkplace_SortedBySubOrderThenSequence()
    {
        using var ctx = TestDbContextFactory.Create();
        var wp = new ProductionWorkplace { Name = "Kante", SageArbeitsplatznummer = "4711",
            CreatedAt = DateTime.Now, CreatedBy = "Test", CreatedByWindows = @"TEST\user" };
        var s1 = Order("100", "102"); var s2 = Order("100", "101"); var other = Order("900", "901");
        ctx.ProductionOrders.AddRange(s1, s2, other);
        ctx.WorkOperations.AddRange(Op(s1, "SW", 2), Op(s1, "KA", 1, wp), Op(s2, "LA", 1), Op(other, "KA", 1));
        await ctx.SaveChangesAsync();

        var result = await new WorkOperationRepository(ctx).GetByOrderNumberWithWorkplaceAsync("100");

        result.Select(w => $"{w.ProductionOrder.SubOrderNumber}:{w.OperationNumber}")
            .Should().Equal("101:LA", "102:KA", "102:SW");
        result.Single(w => w.OperationNumber == "KA").ProductionWorkplace!.SageArbeitsplatznummer.Should().Be("4711");
    }

    /// <summary>
    /// AK 10: Die Existenz-Abfrage darf bei Seitengroesse "Alle" (bis 5000 Strukturen) nicht an der
    /// SQL-Server-Grenze von 2100 Parametern scheitern. Belegt an der echten SqlServer-Uebersetzung
    /// (Provider-Konfiguration wie Program.cs, nie verbunden).
    /// </summary>
    [Fact]
    public void OrderNumbersWithWorkOperationsQuery_TranslatesContainsAsSingleParameter()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=nonexistent-host-for-tests;Database=x;TrustServerCertificate=True;Connect Timeout=1")
            .Options;
        using var ctx = new ApplicationDbContext(options);
        var keys = Enumerable.Range(1, 5000).Select(i => i.ToString()).ToList();

        var sql = new WorkOperationRepository(ctx).OrderNumbersWithWorkOperationsQuery(keys).ToQueryString();
        _output.WriteLine(sql);

        sql.Should().Contain("OPENJSON");
        System.Text.RegularExpressions.Regex.Matches(sql, @"DECLARE @").Count.Should().Be(1);
    }
}
