using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using Xunit;

namespace IdealAkeWms.Tests.Repositories;

/// <summary>
/// Tests fuer die gebuendelte Mengen-Methode <see cref="FaHierarchyOrderInfoRepository.GetByHauptFaKeysAsync"/>
/// (Plan 2026-09-09, Task 6) — EINE Abfrage je Seite statt Fan-out je HauptFA-Gruppe.
/// </summary>
public class FaHierarchyOrderInfoRepositoryTests
{
    private static FaHierarchyOrderInfo Info(int hauptFa, string montageAbteilung, string? kunde = null)
        => new() { HauptFA = hauptFa, MontageAbteilung = montageAbteilung, Kunde = kunde, SyncedAt = DateTime.Now };

    [Fact]
    public async Task GetByHauptFaKeysAsync_ReturnsAllRowsForGivenKeys_IncludingDuplicatesPerHauptFa()
    {
        using var context = TestDbContextFactory.Create();
        context.FaHierarchyOrderInfos.AddRange(
            Info(1000, "Montage A", "Kunde X"),
            Info(1000, "Montage B", "Kunde Y"),   // Kombigeraet: zweite Zeile fuer denselben HauptFA
            Info(2000, "Montage A", "Kunde Z"),
            Info(3000, "Montage A", "Kunde ausserhalb der Anfrage"));
        await context.SaveChangesAsync();

        var repo = new FaHierarchyOrderInfoRepository(context);
        var result = await repo.GetByHauptFaKeysAsync(new[] { 1000, 2000 });

        result.Should().HaveCount(3);
        result.Where(r => r.HauptFA == 1000).Should().HaveCount(2); // kein Dedup bei Kombigeraeten
        result.Should().OnlyContain(r => r.HauptFA == 1000 || r.HauptFA == 2000);
    }

    [Fact]
    public async Task GetByHauptFaKeysAsync_EmptyInput_ReturnsEmptyWithoutQuery()
    {
        using var context = TestDbContextFactory.Create();
        context.FaHierarchyOrderInfos.Add(Info(1000, "Montage A"));
        await context.SaveChangesAsync();

        var repo = new FaHierarchyOrderInfoRepository(context);
        var result = await repo.GetByHauptFaKeysAsync(Array.Empty<int>());

        result.Should().BeEmpty();
    }
}
