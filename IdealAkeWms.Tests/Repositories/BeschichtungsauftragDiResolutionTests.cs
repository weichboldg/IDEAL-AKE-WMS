using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Repositories;

/// <summary>
/// DI-Aufloesung des Beschichtungsauftrag-Workflows (Spec 2026-10-06-beschichtungsauftrag-ausbau-teil-3), Muster
/// <see cref="BomDiResolutionTests"/>: spiegelt die Registrierungen aus <c>Program.cs</c>. Waechter dagegen, dass der
/// Workflow den gecachten <see cref="IFaHierarchyNodeRepository"/>-Decorator statt der konkreten, ungecachten
/// <see cref="FaHierarchyNodeRepository"/> bekommt oder eine Registrierung (TimeProvider) fehlt.
/// </summary>
public class BeschichtungsauftragDiResolutionTests
{
    [Fact]
    public void Workflow_LaesstSichAusDemProgramGraphAufloesen()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ApplicationDbContext>(TestDbContextFactory.Create());
        services.AddScoped(_ => new Mock<ICurrentUserService>().Object);

        // --- Spiegel von Program.cs (FA-Hierarchie-Repositories + Teil 3) ---
        services.AddScoped<FaHierarchyNodeRepository>();
        services.AddScoped<IFaHierarchyOrderInfoRepository, FaHierarchyOrderInfoRepository>();
        services.AddScoped<IBeschichtungsauftragRepository, BeschichtungsauftragRepository>();
        services.AddScoped<BeschichtungsauftragWorkflow>();
        services.AddSingleton(TimeProvider.System);

        using var sp = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using var scope = sp.CreateScope();

        scope.ServiceProvider.GetRequiredService<BeschichtungsauftragWorkflow>().Should().NotBeNull();
    }
}
