using IdealAkeWms.Data;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;

namespace IdealAkeWms.Tests.Helpers;

/// <summary>
/// In-Memory <see cref="IDbContextFactory{ApplicationDbContext}"/>-Implementierung fuer Tests.
/// Alle Contexts teilen dieselbe InMemory-DB, sodass Inserts aus dem SUT in einem
/// separaten Verifier-Context wieder gelesen werden koennen.
/// </summary>
public sealed class TestApplicationDbContextFactory : IDbContextFactory<ApplicationDbContext>, IDisposable
{
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public TestApplicationDbContextFactory([CallerMemberName] string dbName = "")
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName + Guid.NewGuid())
            .Options;
    }

    public ApplicationDbContext CreateDbContext() => new TestApplicationDbContext(_options);

    public Task<ApplicationDbContext> CreateDbContextAsync(CancellationToken ct = default)
        => Task.FromResult<ApplicationDbContext>(new TestApplicationDbContext(_options));

    public void Dispose() { /* InMemory hat kein File-Handle */ }
}
