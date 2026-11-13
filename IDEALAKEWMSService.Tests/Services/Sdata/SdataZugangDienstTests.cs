using FluentAssertions;
using IdealAkeWms.Services.SageSdata;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IDEALAKEWMSService.Tests.Services.Sdata;

/// <summary>
/// Spec 2026-11-13-sdata-teil-2-userfelder-stueckliste F6: Der Dienst entschluesselt mit dem Schluesselring der Web-App (gleicher Ordner,
/// gleicher ApplicationName), nur lesend. Registrierung wie IdealAkeWms/Program.cs.
/// </summary>
public class SdataZugangDienstTests
{
    [Fact]
    public void Dienst_EntschluesseltWasDieWebAppVerschluesselt()
    {
        var ordner = Path.Combine(Path.GetTempPath(), "wms-dp-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(ordner);
        try
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["DataProtection:KeysPath"] = ordner }).Build();
            var web = new ServiceCollection();
            web.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(SdataZugangsdaten.SchluesselPfad(config)))
                .SetApplicationName(SdataZugangsdaten.ApplicationName);
            using var sp = web.BuildServiceProvider();
            var gespeichert = SdataZugangsdaten.Schuetzen("geheim", sp.GetRequiredService<IDataProtectionProvider>());

            var zugang = SdataZugangsdaten.Aufloesen("svc", gespeichert, config, SdataZugangsdaten.DienstProvider(config));

            zugang.Fehler.Should().BeNull();
            zugang.Kennwort.Should().Be("geheim");
        }
        finally
        {
            try { Directory.Delete(ordner, true); } catch (IOException) { }
        }
    }
}
