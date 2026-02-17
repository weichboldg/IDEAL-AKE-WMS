using AKEBDELight.Data;
using AKEBDELight.Models;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;

namespace AKEBDELight.Tests.Helpers;

public static class TestDbContextFactory
{
    public static ApplicationDbContext Create([CallerMemberName] string dbName = "")
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName + Guid.NewGuid())
            .Options;
        return new TestApplicationDbContext(options);
    }
}

/// <summary>
/// InMemory-DB unterstützt kein rowversion - RowVersion wird automatisch gesetzt.
/// </summary>
public class TestApplicationDbContext : ApplicationDbContext
{
    public TestApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // RowVersion für InMemory: als normale byte[]-Property ohne rowversion-Semantik
        modelBuilder.Entity<PickingItem>(entity =>
        {
            entity.Property(e => e.RowVersion)
                .IsConcurrencyToken(false)
                .ValueGeneratedNever()
                .IsRequired(true);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // RowVersion automatisch setzen für neue/geänderte PickingItems
        foreach (var entry in ChangeTracker.Entries<PickingItem>())
        {
            if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
            {
                if (entry.Entity.RowVersion == null || entry.Entity.RowVersion.Length == 0)
                    entry.Entity.RowVersion = BitConverter.GetBytes(DateTime.UtcNow.Ticks);
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        foreach (var entry in ChangeTracker.Entries<PickingItem>())
        {
            if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
            {
                if (entry.Entity.RowVersion == null || entry.Entity.RowVersion.Length == 0)
                    entry.Entity.RowVersion = BitConverter.GetBytes(DateTime.UtcNow.Ticks);
            }
        }
        return base.SaveChanges();
    }
}
