using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using Xunit;

namespace IdealAkeWms.Tests.Repositories;

/// <summary>Kreditoren-Repository (E8a): App-Layer-Duplikat-Guards (InMemory erzwingt die gefilterten UNIQUE-Indexe nicht) und Sortierung.</summary>
public class CreditorRepositoryTests
{
    [Fact]
    public async Task Guards_KurznameGrossKleinEgal_KontoExakt_EigeneIdAusgenommen()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new CreditorRepository(ctx);
        var a = new Creditor { ShortName = "Pulver", Name = "A", SageKto = "0000", CreatedBy = "x", CreatedByWindows = "x" };
        await repo.AddAsync(a);

        (await repo.ExistsShortNameAsync(" PULVER ", null)).Should().BeTrue();
        (await repo.ExistsShortNameAsync("pulver", a.Id)).Should().BeFalse();
        (await repo.ExistsShortNameAsync("Glas", null)).Should().BeFalse();
        (await repo.ExistsSageKtoAsync(" 0000 ", null)).Should().BeTrue();
        (await repo.ExistsSageKtoAsync("0", null)).Should().BeFalse("Kontonummer ist Text, 0000 ist nicht 0");
        (await repo.ExistsSageKtoAsync("0000", a.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task GetAll_NachKurzname_OhneKurznameZuletzt()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new CreditorRepository(ctx);
        foreach (var (s, n) in new[] { ((string?)null, "Sage AG"), ("Zink", "Z"), ("Alu", "A") })
            await repo.AddAsync(new Creditor { ShortName = s, Name = n, CreatedBy = "x", CreatedByWindows = "x" });

        (await repo.GetAllAsync()).Select(c => c.Name).Should().Equal("A", "Z", "Sage AG");
    }
}
