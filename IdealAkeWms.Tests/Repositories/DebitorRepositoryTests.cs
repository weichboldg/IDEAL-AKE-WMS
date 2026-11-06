using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using Xunit;

namespace IdealAkeWms.Tests.Repositories;

/// <summary>Debitoren-Repository (E8b): App-Layer-Duplikat-Guards (InMemory erzwingt die gefilterten UNIQUE-Indexe nicht) und Sortierung.</summary>
public class DebitorRepositoryTests
{
    [Fact]
    public async Task Guards_KurznameGrossKleinEgal_KontoExakt_EigeneIdAusgenommen()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new DebitorRepository(ctx);
        var a = new Debitor { ShortName = "Hofer", Name = "A", SageKto = "0000", CreatedBy = "x", CreatedByWindows = "x" };
        await repo.AddAsync(a);

        (await repo.ExistsShortNameAsync(" HOFER ", null)).Should().BeTrue();
        (await repo.ExistsShortNameAsync("hofer", a.Id)).Should().BeFalse();
        (await repo.ExistsSageKtoAsync(" 0000 ", null)).Should().BeTrue();
        (await repo.ExistsSageKtoAsync("0", null)).Should().BeFalse("Kontonummer ist Text, 0000 ist nicht 0");
        (await repo.ExistsSageKtoAsync("0000", a.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task GetAll_NachKurzname_OhneKurznameZuletzt()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new DebitorRepository(ctx);
        foreach (var (s, n) in new[] { ((string?)null, "Sage AG"), ("Zett", "Z"), ("Alpha", "A") })
            await repo.AddAsync(new Debitor { ShortName = s, Name = n, CreatedBy = "x", CreatedByWindows = "x" });

        (await repo.GetAllAsync()).Select(d => d.Name).Should().Equal("A", "Z", "Sage AG");
    }
}
