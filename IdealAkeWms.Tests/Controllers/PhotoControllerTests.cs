using System.Reflection;
using FluentAssertions;
using IdealAkeWms.Controllers.Api;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Controllers;

/// <summary>v1.30.4 (Hotfix Kommissionierfotos, AK 4, 5, 9, 10, 12): Loeschen nur eigener FA, Auslieferung aus App_Data, Pfadpruefung.</summary>
public class PhotoControllerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "wms-fotos-" + Guid.NewGuid().ToString("N"));
    private readonly string _dir;

    public PhotoControllerTests()
    {
        _dir = Path.Combine(_root, "App_Data", "Fotos", "Kommissionierung");
        Directory.CreateDirectory(_dir);
        File.WriteAllText(Path.Combine(_dir, "FA-A_20261108000000_1.jpg"), "a");
        File.WriteAllText(Path.Combine(_dir, "FA-B_20261108000000_1.jpg"), "b");
        File.WriteAllText(Path.Combine(_root, "appsettings.json"), "{}");
    }

    public void Dispose() => Directory.Delete(_root, true);

    private PhotoController Ctrl()
    {
        var repo = new Mock<IProductionOrderRepository>();
        repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new ProductionOrder { Id = 1, OrderNumber = "FA-A" });
        repo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new ProductionOrder { Id = 2, OrderNumber = "FA-B" });
        var env = new Mock<IWebHostEnvironment>();
        env.Setup(e => e.ContentRootPath).Returns(_root);
        env.Setup(e => e.WebRootPath).Returns(Path.Combine(_root, "wwwroot"));
        return new PhotoController(repo.Object, env.Object);
    }

    [Fact]
    public async Task Delete_FremderFa_400_DateiBleibt_AK9()
    {
        (await Ctrl().Delete(1, "FA-B_20261108000000_1.jpg")).Should().BeOfType<BadRequestResult>();
        File.Exists(Path.Combine(_dir, "FA-B_20261108000000_1.jpg")).Should().BeTrue();
    }

    [Fact]
    public async Task Delete_EigenerFa_Geloescht_AK11()
    {
        (await Ctrl().Delete(1, "FA-A_20261108000000_1.jpg")).Should().BeOfType<OkResult>();
        File.Exists(Path.Combine(_dir, "FA-A_20261108000000_1.jpg")).Should().BeFalse();
    }

    [Fact]
    public async Task Delete_UnbekannterFa_404() =>
        (await Ctrl().Delete(99, "FA-A_20261108000000_1.jpg")).Should().BeOfType<NotFoundResult>();

    [Theory]
    [InlineData("..\\..\\appsettings.json")]
    [InlineData("../x.jpg")]
    [InlineData("a/b.jpg")]
    [InlineData("FA-A_x.png")]
    [InlineData("")]
    public async Task Delete_Und_GetFile_Pfadpruefung_400_AK10(string name)
    {
        (await Ctrl().Delete(1, name)).Should().BeOfType<BadRequestResult>();
        Ctrl().GetFile(name).Should().BeOfType<BadRequestResult>();
        File.Exists(Path.Combine(_root, "appsettings.json")).Should().BeTrue();
    }

    [Fact]
    public void GetFile_AusAppData_Jpeg_Oder404_AK5()
    {
        var r = Ctrl().GetFile("FA-A_20261108000000_1.jpg").Should().BeOfType<PhysicalFileResult>().Subject;
        r.ContentType.Should().Be("image/jpeg");
        r.FileName.Should().Be(Path.Combine(_dir, "FA-A_20261108000000_1.jpg"));
        Ctrl().GetFile("FA-A_fehlt.jpg").Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public void GetFile_LiegtUnterClassLevelKommissionierRecht_AK4()
    {
        typeof(PhotoController).GetCustomAttribute<RequirePickingAccessAttribute>().Should().NotBeNull();
        var get = typeof(PhotoController).GetMethod(nameof(PhotoController.GetFile))!;
        get.GetCustomAttribute<HttpGetAttribute>()!.Template.Should().Be("/Fotos/Kommissionierung/{fileName}");
        get.GetCustomAttributes().OfType<Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute>().Should().BeEmpty();
    }

    // ---- PhotoStorage.MigrateLegacy (AK 6) ----

    [Fact]
    public void Migration_Verschiebt_ZweiterLaufNichts_KollisionBleibtLiegen()
    {
        var alt = Path.Combine(_root, "wwwroot", "Fotos", "Kommissionierung");
        Directory.CreateDirectory(alt);
        File.WriteAllText(Path.Combine(alt, "FA-C_1.jpg"), "c");
        File.WriteAllText(Path.Combine(alt, "FA-A_20261108000000_1.jpg"), "kollision");

        PhotoStorage.MigrateLegacy(alt, _dir, NullLogger.Instance).Should().Be((1, 1));
        File.Exists(Path.Combine(_dir, "FA-C_1.jpg")).Should().BeTrue();
        File.Exists(Path.Combine(alt, "FA-C_1.jpg")).Should().BeFalse();
        File.ReadAllText(Path.Combine(_dir, "FA-A_20261108000000_1.jpg")).Should().Be("a", "Ziel wird nie ueberschrieben");
        File.Exists(Path.Combine(alt, "FA-A_20261108000000_1.jpg")).Should().BeTrue("Kollision bleibt liegen");

        File.Delete(Path.Combine(alt, "FA-A_20261108000000_1.jpg"));
        PhotoStorage.MigrateLegacy(alt, _dir, NullLogger.Instance).Should().Be((0, 0));
        PhotoStorage.MigrateLegacy(Path.Combine(_root, "gibtsnicht"), _dir, NullLogger.Instance).Should().Be((0, 0));
    }
}
