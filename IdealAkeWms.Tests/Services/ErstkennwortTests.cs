using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace IdealAkeWms.Tests.Services;

/// <summary>
/// Erstkennwort + Pflichtwechsel (ADR 0027, Spec 2026-11-14-sicherheit-anmeldung-und-installer AK 7, 8a, 8b, 18):
/// GenerateInitialPassword, admin-Seed (InMemory), /Users Create/Edit/Index.
/// </summary>
public class ErstkennwortTests
{
    private static readonly PasswordService Pw = new();

    [Fact]
    public void GenerateInitialPassword_12Zeichen_OhneVerwechselbare_Zufaellig()
    {
        var a = Pw.GenerateInitialPassword();
        var b = Pw.GenerateInitialPassword();
        a.Should().HaveLength(12);
        a.Should().NotContainAny("0", "O", "1", "l", "I");
        a.All(char.IsLetterOrDigit).Should().BeTrue();
        a.Should().NotBe(b);
    }

    // ----- admin-Seed (AK 8a) -----

    [Fact]
    public void AdminSeed_FehlenderAdmin_StandardErstkennwortMitHaken()
    {
        using var db = TestDbContextFactory.Create();
        var admin = AdminSeed.Ensure(db, Pw);
        admin.MussKennwortAendern.Should().BeTrue();
        Pw.VerifyPassword(admin.PasswordHash!, AdminSeed.StandardErstkennwort).Should().BeTrue();
        Pw.VerifyPassword(admin.PasswordHash!, "").Should().BeFalse();
        admin.CreatedBy.Should().Be("system");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("LEER-HASH")]
    public void AdminSeed_OhneKennwortOderLeerHash_WirdUmgestellt(string? hash)
    {
        using var db = TestDbContextFactory.Create();
        db.Users.Add(new User { Name = "admin", IsActive = true, CreatedBy = "x", CreatedByWindows = "x",
            PasswordHash = hash == "LEER-HASH" ? Pw.HashPassword("") : hash });
        db.SaveChanges();

        var admin = AdminSeed.Ensure(db, Pw);

        admin.MussKennwortAendern.Should().BeTrue();
        Pw.VerifyPassword(admin.PasswordHash!, AdminSeed.StandardErstkennwort).Should().BeTrue();
        admin.ModifiedBy.Should().Be("system");
        admin.ModifiedByWindows.Should().Be("system");
        admin.ModifiedAt.Should().NotBeNull();
    }

    [Fact]
    public void AdminSeed_EigenesKennwort_Unveraendert()
    {
        using var db = TestDbContextFactory.Create();
        var hash = Pw.HashPassword("eigenes");
        db.Users.Add(new User { Name = "admin", IsActive = true, CreatedBy = "x", CreatedByWindows = "x", PasswordHash = hash });
        db.SaveChanges();

        var admin = AdminSeed.Ensure(db, Pw);

        admin.PasswordHash.Should().Be(hash);
        admin.MussKennwortAendern.Should().BeFalse();
        admin.ModifiedAt.Should().BeNull();
    }

    // ----- /Users (AK 7, 8b) -----

    private static UsersController Users(Mock<IUserRepository> repo, Mock<IRoleRepository>? roles = null)
    {
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetDisplayName()).Returns("admin");
        currentUser.Setup(x => x.GetWindowsUserName()).Returns("win-admin");
        currentUser.Setup(x => x.GetDefaultPageSizeAsync()).ReturnsAsync((int?)null);
        var ctrl = new UsersController(repo.Object, (roles ?? new Mock<IRoleRepository>()).Object, currentUser.Object, Pw,
            new Mock<IUserViewPreferenceRepository>().Object, new Mock<IWorkStepRepository>().Object,
            new Mock<IProductionWorkplaceRepository>().Object, new Mock<IActiveDirectoryService>().Object,
            new Mock<IServiceSettingRepository>().Object);
        var http = new DefaultHttpContext();
        ctrl.ControllerContext = new ControllerContext { HttpContext = http };
        ctrl.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());
        return ctrl;
    }

    private static (UsersController Ctrl, User Existing) EditSetup(string? hash = null, bool haken = false)
    {
        var existing = new User { Id = 5, Name = "ts139-erst", IsActive = true, CreatedBy = "t", CreatedByWindows = "t",
            PasswordHash = hash, MussKennwortAendern = haken };
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(existing);
        repo.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        return (Users(repo), existing);
    }

    private static UserEditViewModel Vm(bool haken, bool erzeugen = false) =>
        new() { Id = 5, Name = "ts139-erst", IsActive = true, MussKennwortAendern = haken, ErstkennwortErzeugen = erzeugen };

    [Fact]
    public async Task Create_EingegebenesKennwort_HashUndHaken()
    {
        User? added = null;
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.AddAsync(It.IsAny<User>())).Callback<User>(u => added = u).ReturnsAsync((User u) => u);
        var ctrl = Users(repo);

        await ctrl.Create(new UserEditViewModel { Name = "ts139-erst", IsActive = true, MussKennwortAendern = true }, "Start-139a");

        Pw.VerifyPassword(added!.PasswordHash!, "Start-139a").Should().BeTrue();
        added.MussKennwortAendern.Should().BeTrue();
        ctrl.TempData["SuccessMessage"]!.ToString().Should().NotContain("Erstkennwort:");
    }

    [Fact]
    public async Task Edit_Erzeugen_HatVorrang_GenauEinmalImBanner()
    {
        var (ctrl, existing) = EditSetup();
        await ctrl.Edit(5, Vm(haken: true, erzeugen: true), "eingetippt");

        var banner = ctrl.TempData["SuccessMessage"]!.ToString()!;
        var erzeugt = System.Text.RegularExpressions.Regex.Match(banner, @"Erstkennwort: (\S{12}) ").Groups[1].Value;
        erzeugt.Should().HaveLength(12);
        Pw.VerifyPassword(existing.PasswordHash!, erzeugt).Should().BeTrue();
        Pw.VerifyPassword(existing.PasswordHash!, "eingetippt").Should().BeFalse();
        existing.PasswordHash.Should().NotContain(erzeugt);
        existing.MussKennwortAendern.Should().BeTrue();
        existing.ModifiedBy.Should().Be("admin");
    }

    [Fact]
    public async Task Edit_HakenManuellOhneNeuesKennwort_Gespeichert()
    {
        var hash = Pw.HashPassword("eigenes");
        var (ctrl, existing) = EditSetup(hash);
        await ctrl.Edit(5, Vm(haken: true), null);
        existing.MussKennwortAendern.Should().BeTrue();
        existing.PasswordHash.Should().Be(hash);
        existing.ModifiedAt.Should().NotBeNull();
        ctrl.TempData.ContainsKey("WarningMessage").Should().BeFalse();
    }

    [Fact]
    public async Task Edit_KennwortOhneHaken_Geraetekonto_Erlaubt()
    {
        var (ctrl, existing) = EditSetup(haken: true);
        await ctrl.Edit(5, Vm(haken: false), "Terminal-1");
        Pw.VerifyPassword(existing.PasswordHash!, "Terminal-1").Should().BeTrue();
        existing.MussKennwortAendern.Should().BeFalse();
    }

    // QA-Befund B-2 (Gerald 2026-11-16 19:02): ohne Kennwort wird der Haken nie gespeichert, nur ein Hinweis in der Erfolgsmeldung.
    [Fact]
    public async Task Edit_HakenOhneKennwort_NichtGespeichert_Hinweis()
    {
        var (ctrl, existing) = EditSetup(haken: true);
        await ctrl.Edit(5, Vm(haken: true), null);
        existing.MussKennwortAendern.Should().BeFalse();
        ctrl.TempData["SuccessMessage"]!.ToString().Should().Contain("nicht gespeichert").And.Contain("keine Formular-Anmeldung");
        ctrl.TempData.ContainsKey("WarningMessage").Should().BeFalse();
    }

    [Fact]
    public async Task Edit_AdBenutzerOhneKennwort_KeinHaken_WindowsHinweis()
    {
        var (ctrl, existing) = EditSetup();
        existing.WindowsUserName = "huber";
        await ctrl.Edit(5, Vm(haken: true), null);
        existing.MussKennwortAendern.Should().BeFalse();
        existing.PasswordHash.Should().BeNull();
        ctrl.TempData["SuccessMessage"]!.ToString().Should().Contain("per Windows an");
    }

    [Fact]
    public async Task Create_OhneKennwort_HakenNichtGespeichert()
    {
        User? added = null;
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.AddAsync(It.IsAny<User>())).Callback<User>(u => added = u).ReturnsAsync((User u) => u);
        var ctrl = Users(repo);
        await ctrl.Create(new UserEditViewModel { Name = "ohne", IsActive = true, MussKennwortAendern = true }, null);
        added!.PasswordHash.Should().BeNull();
        added.MussKennwortAendern.Should().BeFalse();
        ctrl.TempData["SuccessMessage"]!.ToString().Should().Contain("nicht gespeichert");
    }

    [Fact]
    public async Task Create_Get_HakenNichtVorbelegt()
    {
        var roles = new Mock<IRoleRepository>();
        roles.Setup(r => r.GetAllOrderedAsync()).ReturnsAsync(new List<Role>());
        var ctrl = Users(new Mock<IUserRepository>(), roles);
        var result = (Microsoft.AspNetCore.Mvc.ViewResult)await ctrl.Create();
        ((UserEditViewModel)result.Model!).MussKennwortAendern.Should().BeFalse();
    }

    [Fact]
    public async Task Index_HinweisOhneKennwortUndOffeneErstkennwoerter()
    {
        var users = new List<User>
        {
            new() { Id = 1, Name = "admin", IsActive = true, PasswordHash = "h", MussKennwortAendern = true, CreatedBy = "t", CreatedByWindows = "t" },
            new() { Id = 2, Name = "lager", IsActive = true, MussKennwortAendern = true, CreatedBy = "t", CreatedByWindows = "t" }, // Altbestand: Haken ohne Hash (B-3)
            new() { Id = 3, Name = "Huber", WindowsUserName = "huber", IsActive = true, CreatedBy = "t", CreatedByWindows = "t" },
            new() { Id = 4, Name = "alt", IsActive = false, CreatedBy = "t", CreatedByWindows = "t" },
            new() { Id = 5, Name = "ok", IsActive = true, PasswordHash = "h", CreatedBy = "t", CreatedByWindows = "t" },
        };
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetAllWithRolesAsync()).ReturnsAsync(users);
        var ctrl = Users(repo);

        await ctrl.Index();

        ((List<string>)ctrl.ViewBag.OhneKennwortLokal).Should().Equal("lager");
        ((List<string>)ctrl.ViewBag.OhneKennwortAd).Should().Equal("Huber");
        ((List<string>)ctrl.ViewBag.OffeneErstkennwoerter).Should().Equal("admin");
    }
}
