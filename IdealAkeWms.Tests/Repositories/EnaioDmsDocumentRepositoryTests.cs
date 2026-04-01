using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using Xunit;

namespace IdealAkeWms.Tests.Repositories;

public class EnaioDmsDocumentRepositoryTests
{
    private static EnaioDmsDocument CreateDoc(long objectId, string type, string? orderNumber, DateTime? createdInEnaio = null)
        => new()
        {
            EnaioDmsObjectId = objectId,
            DocumentType = type,
            OrderNumber = orderNumber,
            CreatedInEnaio = createdInEnaio ?? DateTime.UtcNow,
            LastSyncedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "Test"
        };

    [Fact]
    public async Task GetByOrderNumberAsync_ReturnsMatchingDocuments()
    {
        using var context = TestDbContextFactory.Create();
        context.EnaioDmsDocuments.AddRange(
            CreateDoc(1001, "Werkstattauftrag", "2614027"),
            CreateDoc(1002, "Zeichnung", "2614027"),
            CreateDoc(1003, "Werkstattauftrag", "2614999"));
        await context.SaveChangesAsync();

        var repo = new EnaioDmsDocumentRepository(context);
        var result = await repo.GetByOrderNumberAsync("2614027");

        result.Should().HaveCount(2);
        result.Should().OnlyContain(d => d.OrderNumber == "2614027");
    }

    [Fact]
    public async Task GetByOrderNumberAsync_NoMatch_ReturnsEmpty()
    {
        using var context = TestDbContextFactory.Create();
        context.EnaioDmsDocuments.Add(CreateDoc(2001, "Werkstattauftrag", "2614027"));
        await context.SaveChangesAsync();

        var repo = new EnaioDmsDocumentRepository(context);
        var result = await repo.GetByOrderNumberAsync("9999999");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByOrderNumbersAsync_BulkLookup_ReturnsGroupedByOrderNumber()
    {
        using var context = TestDbContextFactory.Create();
        context.EnaioDmsDocuments.AddRange(
            CreateDoc(3001, "Werkstattauftrag", "WA001"),
            CreateDoc(3002, "Zeichnung", "WA001"),
            CreateDoc(3003, "Werkstattauftrag", "WA002"),
            CreateDoc(3004, "Werkstattauftrag", "WA003"));
        await context.SaveChangesAsync();

        var repo = new EnaioDmsDocumentRepository(context);
        var result = await repo.GetByOrderNumbersAsync(new[] { "WA001", "WA002", "WA999" });

        result.Should().ContainKey("WA001");
        result["WA001"].Should().HaveCount(2);
        result.Should().ContainKey("WA002");
        result["WA002"].Should().HaveCount(1);
        result.Should().NotContainKey("WA999");
    }

    [Fact]
    public async Task GetByOrderNumbersAsync_EmptyInput_ReturnsEmptyDictionary()
    {
        using var context = TestDbContextFactory.Create();
        var repo = new EnaioDmsDocumentRepository(context);
        var result = await repo.GetByOrderNumbersAsync(Array.Empty<string>());
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByOrderNumbersAsync_ReturnsCorrectLinkData()
    {
        using var context = TestDbContextFactory.Create();
        context.EnaioDmsDocuments.Add(CreateDoc(5001, "Zeichnung", "WA100"));
        await context.SaveChangesAsync();

        var repo = new EnaioDmsDocumentRepository(context);
        var result = await repo.GetByOrderNumbersAsync(new[] { "WA100" });

        var link = result["WA100"].Single();
        link.EnaioDmsObjectId.Should().Be(5001);
        link.DocumentType.Should().Be("Zeichnung");
    }
}
