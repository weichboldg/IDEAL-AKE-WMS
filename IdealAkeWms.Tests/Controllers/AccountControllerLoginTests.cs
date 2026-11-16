using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace IdealAkeWms.Tests.Controllers;

/// <summary>
/// Login nach Regel R (nur mit gespeichertem Kennwort) und Regel P (Erstkennwort + Pflichtwechsel), ADR 0027,
/// Spec 2026-11-14-sicherheit-anmeldung-und-installer AK 1-6, 8c. Echter <see cref="PasswordService"/> (PBKDF2).
/// </summary>
public class AccountControllerLoginTests
{
    private static readonly PasswordService Pw = new();

    private static (AccountController Ctrl, Mock<IUserRepository> Repo) Setup(User user, string environment = "Production",
        bool windowsAuthAktiv = false)
    {
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetByNameAsync(user.Name)).ReturnsAsync(user);
        repo.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        repo.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        var appSettings = new Mock<IAppSettingRepository>();
        appSettings.Setup(a => a.GetValueAsync(AppSettingKeys.WindowsAuthAktiv)).ReturnsAsync(windowsAuthAktiv ? "true" : "false");
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetCurrentAppUserId()).Returns(user.Id);
        currentUser.Setup(x => x.GetCurrentAppUserName()).Returns(user.Name);
        currentUser.Setup(x => x.GetWindowsUserName()).Returns("SYSTEM");
        var workSteps = new Mock<IWorkStepRepository>();
        workSteps.Setup(x => x.GetActiveAsync()).ReturnsAsync(new List<WorkStep>());
        var workplaces = new Mock<IProductionWorkplaceRepository>();
        workplaces.Setup(x => x.GetAllOrderedAsync()).ReturnsAsync(new List<ProductionWorkplace>());

        var ctrl = new AccountController(repo.Object, Pw, currentUser.Object, workSteps.Object, workplaces.Object,
            appSettings.Object, new Mock<IServiceSettingRepository>().Object,
            Mock.Of<IWebHostEnvironment>(e => e.EnvironmentName == environment));
        var http = new DefaultHttpContext();
        http.Features.Set<ISessionFeature>(new SessionFeatureStub());
        ctrl.ControllerContext = new ControllerContext { HttpContext = http };
        ctrl.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());
        return (ctrl, repo);
    }

    private static User Benutzer(string? kennwort, bool haken = false, string? windows = null) => new()
    {
        Id = 7, Name = "ts139", IsActive = true, WindowsUserName = windows, CreatedBy = "t", CreatedByWindows = "t",
        PasswordHash = kennwort == null ? null : Pw.HashPassword(kennwort), MussKennwortAendern = haken
    };

    private static bool HatSitzung(AccountController c) => c.HttpContext.Session.GetInt32(CurrentUserService.SessionKeyUserId).HasValue;

    private static LoginViewModel Vm(IActionResult r) => (LoginViewModel)((ViewResult)r).Model!;

    [Theory]
    [InlineData(null)]
    [InlineData("x")]
    public async Task OhneKennwort_MeldungR_KeineSitzung(string? eingabe)
    {
        var (ctrl, _) = Setup(Benutzer(null));
        var result = await ctrl.Login(new LoginViewModel { UserName = "ts139", Password = eingabe });
        Vm(result).ErrorMessage.Should().Be(AccountController.MeldungOhneKennwort(false)).And.NotContain("Windows");
        HatSitzung(ctrl).Should().BeFalse();
    }

    [Fact]
    public async Task AdBenutzerOhneKennwort_MeldungMitWindowsHinweis()
    {
        var (ctrl, _) = Setup(Benutzer(null, windows: "ts139win"), windowsAuthAktiv: true);
        var result = await ctrl.Login(new LoginViewModel { UserName = "ts139" });
        Vm(result).ErrorMessage.Should().Contain("Mit Windows anmelden");
        HatSitzung(ctrl).Should().BeFalse();
    }

    [Fact]
    public async Task OhneKennwortMitHaken_MeldungR()
    {
        var (ctrl, repo) = Setup(Benutzer(null, haken: true));
        var result = await ctrl.Login(new LoginViewModel
            { UserName = "ts139", NewPassword = "a", NewPasswordRepeat = "a", KennwortFestlegen = true });
        Vm(result).ErrorMessage.Should().Be(AccountController.MeldungOhneKennwort(false));
        HatSitzung(ctrl).Should().BeFalse();
        repo.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Entwicklungsmodus_OhneKennwort_LeeresFeld_Angemeldet()
    {
        var (ctrl, _) = Setup(Benutzer(null), environment: "Development");
        var result = await ctrl.Login(new LoginViewModel { UserName = "ts139" });
        result.Should().BeOfType<RedirectToActionResult>();
        HatSitzung(ctrl).Should().BeTrue();
    }

    [Theory]
    [InlineData("geheim", true)]
    [InlineData("falsch", false)]
    [InlineData(null, false)]
    [InlineData("", false)]
    public async Task MitKennwortOhneHaken(string? eingabe, bool angemeldet)
    {
        var (ctrl, _) = Setup(Benutzer("geheim"));
        var result = await ctrl.Login(new LoginViewModel { UserName = "ts139", Password = eingabe });
        HatSitzung(ctrl).Should().Be(angemeldet);
        if (!angemeldet)
            Vm(result).ErrorMessage.Should().Be("Falsches Passwort.");
    }

    [Fact]
    public async Task ManipulierterPostOhneHaken_AendertNichts()
    {
        var user = Benutzer("geheim");
        var hash = user.PasswordHash;
        var (ctrl, repo) = Setup(user);
        await ctrl.Login(new LoginViewModel
            { UserName = "ts139", Password = "geheim", NewPassword = "neu", NewPasswordRepeat = "neu", KennwortFestlegen = true });
        user.PasswordHash.Should().Be(hash);
        repo.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task MitHaken_ErsterPost_ZeigtSchritt_KeineSitzung()
    {
        var (ctrl, repo) = Setup(Benutzer("Start-139a", haken: true));
        var vm = Vm(await ctrl.Login(new LoginViewModel { UserName = "ts139", Password = "Start-139a" }));
        vm.KennwortFestlegen.Should().BeTrue();
        vm.ErrorMessage.Should().BeNull();
        HatSitzung(ctrl).Should().BeFalse();
        repo.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Theory]
    [InlineData("Start-139a", "", "", "Bitte ein neues Kennwort eingeben.")]
    [InlineData("Start-139a", "   ", "   ", "Bitte ein neues Kennwort eingeben.")]
    [InlineData("Start-139a", "Start-139a", "Start-139a", "Das neue Kennwort muss sich vom bisherigen unterscheiden.")]
    [InlineData("Start-139a", "Neu-139a", "Neu-139x", "Die Wiederholung stimmt nicht mit dem neuen Kennwort überein.")]
    [InlineData("falsch", "Neu-139a", "Neu-139a", "Falsches Passwort.")]
    public async Task Pflichtwechsel_Negativ_NichtsGeaendert(string bisher, string neu, string wdh, string meldung)
    {
        var user = Benutzer("Start-139a", haken: true);
        var hash = user.PasswordHash;
        var (ctrl, repo) = Setup(user);
        var result = await ctrl.Login(new LoginViewModel
            { UserName = "ts139", Password = bisher, NewPassword = neu, NewPasswordRepeat = wdh, KennwortFestlegen = true });
        Vm(result).ErrorMessage.Should().Be(meldung);
        HatSitzung(ctrl).Should().BeFalse();
        user.PasswordHash.Should().Be(hash);
        user.MussKennwortAendern.Should().BeTrue();
        repo.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Pflichtwechsel_Gueltig_SpeichertUndMeldetAn()
    {
        var user = Benutzer("Start-139a", haken: true);
        var (ctrl, repo) = Setup(user);
        var result = await ctrl.Login(new LoginViewModel
            { UserName = "ts139", Password = "Start-139a", NewPassword = "Neu-139a", NewPasswordRepeat = "Neu-139a", KennwortFestlegen = true });
        result.Should().BeOfType<RedirectToActionResult>();
        HatSitzung(ctrl).Should().BeTrue();
        user.MussKennwortAendern.Should().BeFalse();
        Pw.VerifyPassword(user.PasswordHash!, "Neu-139a").Should().BeTrue();
        Pw.VerifyPassword(user.PasswordHash!, "Start-139a").Should().BeFalse();
        user.ModifiedBy.Should().Be("ts139");
        user.ModifiedByWindows.Should().Be("SYSTEM");
        user.ModifiedAt.Should().NotBeNull();
        repo.Verify(r => r.UpdateAsync(user), Times.Once);
        ctrl.TempData["SuccessMessage"].Should().Be("Kennwort geändert.");
    }

    [Fact]
    public async Task Profile_NeuesKennwort_LoeschtHaken()
    {
        var user = Benutzer("Start-139a", haken: true);
        var (ctrl, _) = Setup(user);
        await ctrl.Profile(new ProfileViewModel { Name = user.Name }, "eigenes");
        user.MussKennwortAendern.Should().BeFalse();
        Pw.VerifyPassword(user.PasswordHash!, "eigenes").Should().BeTrue();
    }
}
