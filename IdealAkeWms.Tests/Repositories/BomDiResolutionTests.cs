using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Repositories;

/// <summary>
/// BOM-Bridge (Spec 2026-09-08): Aufloesungstest fuer den DI-Graph aus <c>Program.cs</c> (Zeilen 91-104).
/// <para>
/// Der BOM-Graph ist zyklisch angelegt: <see cref="BomRepository"/> injiziert
/// <see cref="IBomCacheRepository"/> — und diese Registrierung ist seit der Bridge die
/// <see cref="BomRepositoryMasterSwitch"/> selbst. Der Zyklus ist nur deshalb aufloesbar, weil die Weiche
/// ihre vier Ziele als <c>Func&lt;&gt;</c>-Delegates LAZY zieht und <see cref="CachedBomRepository"/> die
/// KONKRETE Klasse <see cref="BomRepository"/> injiziert (nicht <see cref="IBomRepository"/>).
/// </para>
/// <para>
/// Dieser Test ist der Waechter dagegen, dass jemand den Ctor von <see cref="CachedBomRepository"/> von
/// <see cref="BomRepository"/> auf <see cref="IBomRepository"/> umstellt (oder die Delegates eager macht):
/// dann liefe die Kette Weiche → CachedBomRepository → Weiche → … in eine Endlosrekursion bzw. in einen
/// von der DI erkannten Zyklus. Beides wuerde erst zur Laufzeit auf der Stuecklisten-Seite knallen und ist
/// in keinem der Repository-Unit-Tests sichtbar, weil die dort alle direkt konstruiert werden.
/// </para>
/// </summary>
public class BomDiResolutionTests
{
    private const string ArticleNumber = "X";

    /// <summary>Baut den BOM-Teilgraph exakt wie <c>IdealAkeWms/Program.cs:91-104</c>.</summary>
    private static ServiceProvider BuildProvider(ApplicationDbContext db)
    {
        var services = new ServiceCollection();

        // --- Minimale Abhaengigkeiten der beteiligten Ctors ---
        services.AddSingleton(db);
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // BomRepository wirft ohne diesen Key bereits im Ctor.
                ["ConnectionStrings:OseonConnection"] = "Server=x;"
            })
            .Build());
        services.AddMemoryCache();
        services.AddLogging();
        services.AddScoped(_ => new Mock<IFaHierarchyNodeRepository>().Object);
        services.AddScoped<FaHierarchyTreeBuilder>();

        // Master nicht gesetzt => Flachmodus (AKE-Pfad).
        var settings = new Mock<IServiceSettingRepository>();
        settings.Setup(s => s.GetValueAsync(It.IsAny<string>())).ReturnsAsync((string?)null);
        services.AddScoped(_ => settings.Object);

        // --- Spiegel von Program.cs:91-104 ---
        services.AddScoped<BomCacheRepository>();
        services.AddScoped<BomRepository>();
        services.AddScoped<CachedBomRepository>();
        services.AddScoped<FaHierarchyBomRepository>();
        services.AddScoped<BomRepositoryMasterSwitch>(sp => new BomRepositoryMasterSwitch(
            sp.GetRequiredService<IServiceSettingRepository>(),
            hierarchicalBom: () => sp.GetRequiredService<FaHierarchyBomRepository>(),
            hierarchicalCache: () => sp.GetRequiredService<FaHierarchyBomRepository>(),
            flatBom: () => sp.GetRequiredService<CachedBomRepository>(),
            flatCache: () => sp.GetRequiredService<BomCacheRepository>()));
        services.AddScoped<IBomRepository>(sp => sp.GetRequiredService<BomRepositoryMasterSwitch>());
        services.AddScoped<IBomCacheRepository>(sp => sp.GetRequiredService<BomRepositoryMasterSwitch>());

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true
        });
    }

    [Fact]
    public void Di_BothInterfaces_ResolveToSameSwitchInstance()
    {
        using var db = TestDbContextFactory.Create();
        using var provider = BuildProvider(db);
        using var scope = provider.CreateScope();

        var bom = scope.ServiceProvider.GetRequiredService<IBomRepository>();
        var cache = scope.ServiceProvider.GetRequiredService<IBomCacheRepository>();

        bom.Should().BeOfType<BomRepositoryMasterSwitch>();
        cache.Should().BeOfType<BomRepositoryMasterSwitch>();
        cache.Should().BeSameAs(bom, "beide Interfaces muessen in EINER Weiche zusammenlaufen — sonst " +
                                     "entscheidet der Cache-Pfad anders als der Lese-Pfad");
    }

    [Fact]
    public async Task Di_FlatChain_ResolvesWithoutCycle()
    {
        using var db = TestDbContextFactory.Create();
        // Persistenter BOM-Cache-Treffer: laesst BomRepository VOR dem (unter InMemory nicht
        // ausfuehrbaren) SqlQueryRaw zurueckkehren. Damit laeuft die komplette Lazy-Kette
        // Weiche -> CachedBomRepository -> BomRepository -> IBomCacheRepository (= Weiche) ->
        // BomCacheRepository echt durch, ohne dass ein Provider-Detail den Test traegt.
        db.CachedBomHeaders.Add(new CachedBomHeader
        {
            Artikelnummer = ArticleNumber,
            Source = BomDataSources.Sage,
            ContentHash = "hash",
            ItemCount = 1,
            CachedAt = DateTime.UtcNow,
            Items = new List<CachedBomItem>
            {
                new() { Position = "10", Ressourcenummer = "TEIL-1", Menge = 2m, SortOrder = 1 }
            }
        });
        await db.SaveChangesAsync();

        using var provider = BuildProvider(db);
        using var scope = provider.CreateScope();
        var bomRepo = scope.ServiceProvider.GetRequiredService<IBomRepository>();

        // Kein StackOverflow, kein "circular dependency" aus der DI:
        var result = await bomRepo.GetBomItemsAsync(new BomKey(ArticleNumber));

        result.DataSource.Should().Be(BomDataSources.Sage);
        result.MengeIstAuftragsmenge.Should().BeFalse("der Flachmodus rechnet weiterhin mit der Auftragsmenge hoch");
        result.Items.Should().ContainSingle().Which.Ressourcenummer.Should().Be("TEIL-1");
    }
}
