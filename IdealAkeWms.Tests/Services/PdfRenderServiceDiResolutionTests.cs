using FluentAssertions;
using IdealAkeWms.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IdealAkeWms.Tests.Services;

/// <summary>Waechter (Vorbild <c>BomDiResolutionTests</c>): <see cref="IPdfRenderService"/> MUSS Singleton
/// sein — bei Scoped/Transient haette jede Anfrage ihr eigenes Semaphor, die Prozess-Begrenzung waere
/// wirkungslos und saehe im Code richtig aus (AK 14).</summary>
public class PdfRenderServiceDiResolutionTests
{
    /// <summary>Spiegel der PdfRender-Zeilen in <c>IdealAkeWms/Program.cs</c>.</summary>
    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddLogging();

        services.AddSingleton<PdfRenderStatus>();
        services.AddSingleton<IEdgeProcessRunner, EdgeProcessRunner>();
        services.AddSingleton<IPdfRenderService, PdfRenderService>();

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    [Fact]
    public void Di_PdfRenderService_IsOneInstanceAcrossScopes()
    {
        using var provider = BuildProvider();
        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var a = scope1.ServiceProvider.GetRequiredService<IPdfRenderService>();
        var b = scope2.ServiceProvider.GetRequiredService<IPdfRenderService>();

        a.Should().BeOfType<PdfRenderService>();
        b.Should().BeSameAs(a, "das SemaphoreSlim muss anfrageuebergreifend wirken");
        scope2.ServiceProvider.GetRequiredService<PdfRenderStatus>()
            .Should().BeSameAs(scope1.ServiceProvider.GetRequiredService<PdfRenderStatus>());
    }

    [Fact]
    public void ProgramCs_RegistersPdfRenderServiceAsSingleton()
    {
        var programCs = File.ReadAllText(Path.Combine(FindRepoRoot(), "IdealAkeWms", "Program.cs"));
        programCs.Should().Contain("AddSingleton<IPdfRenderService, PdfRenderService>()");
        programCs.Should().Contain("AddSingleton<PdfRenderStatus>()");
        programCs.Should().Contain("AddSingleton<IEdgeProcessRunner, EdgeProcessRunner>()");
        programCs.Should().Contain("GetRequiredService<PdfRenderStatus>().Refresh()", "Start-Probe (Freigabe-Antwort 2)");
        programCs.Should().NotContain("AddScoped<IPdfRenderService");
        programCs.Should().NotContain("AddTransient<IPdfRenderService");
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "IdealAkeWms.slnx"))) dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("IdealAkeWms.slnx nicht gefunden");
    }
}
