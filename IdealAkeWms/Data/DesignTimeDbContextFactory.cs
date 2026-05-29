using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace IdealAkeWms.Data;

/// <summary>
/// Design-time-only factory fuer EF Core Tools (dotnet ef ...).
/// Wird zur Laufzeit NICHT verwendet — dort registriert Program.cs den DbContext
/// via AddDbContext + AddDbContextFactory.
/// Hintergrund: Die parallele Registrierung von Scoped-DbContext und
/// Singleton-DbContextFactory schlaegt die DI-Validierung der EF-Tools fehl
/// ("Cannot consume scoped service from singleton"). Diese Factory umgeht das,
/// indem sie die Options direkt aus appsettings.json baut.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlServer(
            configuration.GetConnectionString("DefaultConnection"),
            sqlOptions => sqlOptions.CommandTimeout(120));

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
