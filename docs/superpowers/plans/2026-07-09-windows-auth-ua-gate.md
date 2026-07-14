# Windows-Auth: User-Agent-Gate + manueller SSO-Button — Implementierungsplan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax. Jeder Task ist TDD: erst der fehlschlagende Test (RED), dann die Implementierung (GREEN), dann Commit. Kein Task gilt als fertig ohne gruenen Build + gruene Web-Tests.

**Goal:** Die `WindowsAutoLoginMiddleware` schickt die Negotiate-Challenge nur noch an **Windows-Desktop-Browser** (per User-Agent erkannt). Android/iOS/Mac/Linux/unbekannt fallen prompt-frei aufs Anmelde-Formular durch. Ein manueller Button „Mit Windows anmelden" auf dem Login-Formular erzwingt die SSO-Challenge auch fuer per UA nicht erkannte Windows-Clients (ForceSso-Cookie), sodass das UA-Gate verlustfrei bleibt. Alles hinter dem bestehenden `WindowsAuthAktiv` (Default false).

**Architecture:** Reine Web-Aenderung, kein Schema-Change. Ein neuer reiner Helfer `UserAgentHelper.IsWindowsDesktop(string?)` (unit-testbar) trifft die UA-Entscheidung. Die `WindowsAutoLoginMiddleware` gated **nur** die anonym→Challenge-Verzweigung mit `IsWindowsDesktop(UA) || ForceSso-Cookie`; der bereits-authentifiziert→SAM-Match-Zweig bleibt UA-unabhaengig. Eine neue Cookie-Konstante `ForceSsoCookie` steuert den Force-Flow; sie wird beim Verlassen jedes SSO-Pfad-Ausgangs geloescht. Der `AccountController` bekommt eine GET-Action `WindowsLogin`, die den Force-Cookie setzt (und NoAutoLogin/AutoLoginTried loescht) und auf `/` redirected — wo die Middleware greift (`/Account/*` ist von ihr ausgeschlossen). Der Login-View rendert den Button nur bei `WindowsAuthAktiv`.

**Tech Stack:** ASP.NET Core 10 MVC, IIS in-process Hosting (Windows-Auth via `IISServerDefaults.AuthenticationScheme`), xUnit + Moq + FluentAssertions + EF InMemory. Middleware-Challenge ist ueber `IChallengeIssuer` gekapselt (Fake im Test).

**Spec:** `docs/superpowers/specs/2026-07-09-windows-auth-ua-gate-design.md` (verbindlich).

**Branch/Worktree:** `feature/glas-bestellung` im Worktree `.claude/worktrees/glas-bestellung`, Basis HEAD `b0cb74c`. ALLE Befehle dort ausfuehren.

**Konventionen:**
- Code/Bezeichner Englisch, UI-Texte Deutsch.
- Build: `dotnet build IdealAkeWms.slnx` (0 Fehler). Web-Tests: `dotnet test IdealAkeWms.Tests --nologo` (kein Service-Test-Impact).
- Git im Worktree via Bash: `cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git ...`. Commit je Task, Message-Footer endet mit `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`. Commit ueber git-bash Heredoc `git commit -F` (siehe Snippet in Task 1). Bei `fatal error - add_item` den `git commit`-Aufruf 1× wiederholen.
- **KEIN AppVersion-Bump** (in v1.25.0 gefaltet, `AppVersion.cs` bleibt `1.25.0`), **KEINE Migration / kein Schema-Change**, **KEIN neues AppSetting** (nur eine neue Cookie-Konstante im Code). TempData nur Success/Warning.
- TDD-Rhythmus je Task: failing test schreiben → `dotnet test` (RED, gezielt beobachten) → implementieren → `dotnet test` (GREEN) → commit.

**Baseline (Task 0 verifiziert):** `dotnet test IdealAkeWms.Tests` = **923 erfolgreich, 1 uebersprungen, 924 gesamt, 0 Fehler**.

---

## Task 0: Pre-Flight Baseline

**Files:** keine.

- [ ] **Step 1: Branch + sauberer Stand pruefen**

Run:
```bash
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git rev-parse --abbrev-ref HEAD && git rev-parse --short HEAD && git status --short
```
Erwartet: `feature/glas-bestellung`, HEAD `b0cb74c` (oder neuer, falls Vor-Tasks committed), `git status --short` leer.

- [ ] **Step 2: Baseline-Build gruen**

Run:
```bash
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && dotnet build IdealAkeWms.slnx
```
Erwartet: `Der Buildvorgang wurde erfolgreich ausgefuehrt.` / 0 Fehler (die bestehende `CS8602`-Warning in `TrackingController.cs` ist Baseline und OK).

- [ ] **Step 3: Baseline-Tests gruen**

Run:
```bash
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && dotnet test IdealAkeWms.Tests --nologo
```
Erwartet: `Bestanden! ... Fehler: 0, erfolgreich: 923, uebersprungen: 1, gesamt: 924`.

*(Kein Commit in Task 0.)*

---

## Task 1: Reiner Helfer `UserAgentHelper.IsWindowsDesktop(string?)` (TDD)

Ablageort: `IdealAkeWms/Services/UserAgentHelper.cs` — dieselbe Stelle/Namespace wie `WindowsAccountHelper` (reiner statischer Helfer in `IdealAkeWms.Services`; die Middleware hat bereits `using IdealAkeWms.Services;`).

**Files:**
- Create: `IdealAkeWms.Tests/Services/UserAgentHelperTests.cs`
- Create: `IdealAkeWms/Services/UserAgentHelper.cs`

- [ ] **Step 1: Failing Theory-Test schreiben (RED)**

Neue Datei `IdealAkeWms.Tests/Services/UserAgentHelperTests.cs`:

```csharp
using FluentAssertions;
using IdealAkeWms.Services;
using Xunit;

namespace IdealAkeWms.Tests.Services;

public class UserAgentHelperTests
{
    // Windows-Desktops -> true
    [Theory]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36")]         // Windows Chrome
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 Edg/120.0.0.0")] // Windows Edge
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:121.0) Gecko/20100101 Firefox/121.0")]                                          // Windows Firefox
    public void WindowsDesktop_ReturnsTrue(string ua)
        => UserAgentHelper.IsWindowsDesktop(ua).Should().BeTrue();

    // Mobil / Nicht-Windows / leer -> false
    [Theory]
    [InlineData("Mozilla/5.0 (Linux; Android 13; Pixel 7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Mobile Safari/537.36")]     // Android Chrome
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1")] // iPhone Safari
    [InlineData("Mozilla/5.0 (iPad; CPU OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1")]          // iPad
    [InlineData("Mozilla/5.0 (Windows Phone 10.0; Android 6.0.1; Microsoft; Lumia 950) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/52.0.2743.116 Mobile Safari/537.36 Edge/15.15254")] // Windows Phone
    [InlineData("RandomAgent Mobile")]                                                                                                        // generic Mobile
    [InlineData("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Safari/605.1.15")]     // Mac Safari
    [InlineData("Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36")]                     // Linux
    [InlineData("")]                                                                                                                          // leer
    [InlineData("   ")]                                                                                                                       // whitespace
    [InlineData(null)]                                                                                                                        // null
    public void NonWindowsOrEmpty_ReturnsFalse(string? ua)
        => UserAgentHelper.IsWindowsDesktop(ua).Should().BeFalse();
}
```

Run (RED — muss Kompilierfehler sein, weil `UserAgentHelper` noch fehlt):
```bash
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && dotnet test IdealAkeWms.Tests --nologo
```
Erwartet: Build-/Testlauf schlaegt fehl mit `CS0103: Der Name "UserAgentHelper" ist nicht vorhanden` (o. ae.). Das ist der RED-Zustand.

- [ ] **Step 2: Helfer implementieren (GREEN)**

Neue Datei `IdealAkeWms/Services/UserAgentHelper.cs`:

```csharp
namespace IdealAkeWms.Services;

/// <summary>
/// Erkennt, ob ein User-Agent ein Windows-Desktop-Browser ist (fuer das SSO-UA-Gate).
/// Konservativ: nur eindeutige Windows-Desktops -> true; im Zweifel false (Formular + Button-Fallback).
/// </summary>
public static class UserAgentHelper
{
    // Reihenfolge wichtig: Mobile-Marker VOR dem "Windows NT"-Check pruefen,
    // damit z. B. ein "Windows Phone ... Mobile"-UA nie als Desktop gilt.
    private static readonly string[] MobileMarkers =
        { "Android", "iPhone", "iPad", "iPod", "Windows Phone", "Mobile" };

    /// <summary>null/leer/mobil/Mac/Linux/unbekannt -> false; enthaelt "Windows NT" (und keinen Mobile-Marker) -> true.</summary>
    public static bool IsWindowsDesktop(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return false;

        foreach (var marker in MobileMarkers)
            if (userAgent.Contains(marker, StringComparison.OrdinalIgnoreCase))
                return false;

        return userAgent.Contains("Windows NT", StringComparison.OrdinalIgnoreCase);
    }
}
```

Run (GREEN):
```bash
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && dotnet build IdealAkeWms.slnx && dotnet test IdealAkeWms.Tests --nologo
```
Erwartet: Build 0 Fehler; Tests `Fehler: 0`, `erfolgreich: 936` (= 923 + 13 neue Theory-Zeilen: 3 Windows + 10 Nicht-Windows/leer/null), `uebersprungen: 1`, `gesamt: 937`.

- [ ] **Step 3: Commit**

```bash
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git add -A && git commit -F- <<'EOF'
feat(auth): UserAgentHelper.IsWindowsDesktop fuer SSO-UA-Gate

Reiner, unit-getesteter Helfer: erkennt Windows-Desktop-Browser
(Windows NT ohne Mobile-Marker). Grundlage fuer das UA-Gate der
WindowsAutoLoginMiddleware. Kein Schema-Change.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
```
Erwartet: ein Commit. Bei `fatal error - add_item` den `git commit`-Aufruf 1× wiederholen (`git add` ist idempotent).

---

## Task 2: Middleware-Gate + ForceSso-Cookie (TDD)

**Files:**
- Modify: `IdealAkeWms.Tests/Middleware/WindowsAutoLoginMiddlewareTests.cs`
- Modify: `IdealAkeWms/Middleware/WindowsAutoLoginMiddleware.cs`

**Wichtiger Test-Impact (verifiziert):** Der bestehende Test `Anonymous_NoTriedCookie_Challenges_NoNext` baut den Kontext ohne User-Agent-Header. Nach dem UA-Gate wuerde `IsWindowsDesktop("")` = false → keine Challenge → der Test bricht. Deshalb bekommt `MakeContext` in Step 1 einen **UA-Parameter mit Windows-Default**, damit die bestehende anonym-Challenge-Erwartung gruen bleibt; die neuen Nicht-Windows-Faelle setzen den UA explizit.

- [ ] **Step 1: `MakeContext` um UA erweitern + neue Faelle schreiben (RED)**

In `IdealAkeWms.Tests/Middleware/WindowsAutoLoginMiddlewareTests.cs`:

(a) Zwei UA-Konstanten oben in der Klasse (direkt nach `public class WindowsAutoLoginMiddlewareTests {`) ergaenzen:

```csharp
    private const string WindowsUa =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
    private const string AndroidUa =
        "Mozilla/5.0 (Linux; Android 13; Pixel 7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Mobile Safari/537.36";
```

(b) `MakeContext`-Signatur um `string? userAgent = WindowsUa` erweitern und den Header setzen. Ersetze:

```csharp
    private static DefaultHttpContext MakeContext(bool authenticated, string? name)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = "/Home/Index";
```

durch:

```csharp
    private static DefaultHttpContext MakeContext(bool authenticated, string? name, string? userAgent = WindowsUa)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = "/Home/Index";
        if (userAgent != null)
            ctx.Request.Headers.UserAgent = userAgent;
```

(c) Am Ende der Klasse (vor der schliessenden `}`) die neuen Testfaelle (b)/(c)/(d) aus der Spec ergaenzen — Fall (a) Windows-UA-Challenge deckt der bestehende `Anonymous_NoTriedCookie_Challenges_NoNext` (jetzt mit Windows-UA-Default), Fall (e) Flag-off deckt `FlagOff_CallsNext_NoSession`:

```csharp
    [Fact]
    public async Task Anonymous_AndroidUa_NoChallenge_CallsNext_NoTriedCookie()
    {
        var challenge = new Mock<IChallengeIssuer>();
        var ctx = MakeContext(false, null, AndroidUa);
        var called = false;
        await Build(new Mock<IUserRepository>(), true, challenge)
            .InvokeAsync(ctx, _ => { called = true; return Task.CompletedTask; });
        called.Should().BeTrue();
        challenge.Verify(c => c.ChallengeAsync(It.IsAny<HttpContext>()), Times.Never);
        // Fallstrick #2: KEIN AutoLoginTried-Cookie, wenn nicht gechallenged wird
        ctx.Response.Headers["Set-Cookie"].ToString().Should().NotContain(WindowsAutoLoginMiddleware.AutoLoginTriedCookie);
    }

    [Fact]
    public async Task Anonymous_AndroidUa_ForceSsoCookie_Challenges_NoNext()
    {
        var challenge = new Mock<IChallengeIssuer>();
        var ctx = MakeContext(false, null, AndroidUa);
        ctx.Request.Headers["Cookie"] = $"{WindowsAutoLoginMiddleware.ForceSsoCookie}=1";
        var called = false;
        await Build(new Mock<IUserRepository>(), true, challenge)
            .InvokeAsync(ctx, _ => { called = true; return Task.CompletedTask; });
        called.Should().BeFalse();
        challenge.Verify(c => c.ChallengeAsync(ctx), Times.Once);
    }

    [Fact]
    public async Task IdentityMatches_WithForceSso_SetsSession_DeletesForceSso()
    {
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetActiveByWindowsUserNameAsync("jmuster"))
            .ReturnsAsync(new User { Id = 42, Name = "Max", WindowsUserName = "jmuster", IsActive = true });
        // authentifiziert -> UA egal (Gate greift nur im anonym-Zweig)
        var ctx = MakeContext(true, "AKE\\jmuster", AndroidUa);
        ctx.Request.Headers["Cookie"] = $"{WindowsAutoLoginMiddleware.ForceSsoCookie}=1";
        await Build(repo, true, new Mock<IChallengeIssuer>()).InvokeAsync(ctx, _ => Task.CompletedTask);
        ctx.Session.GetInt32(CurrentUserService.SessionKeyUserId).Should().Be(42);
        // ForceSso wird beim Erfolg geloescht (Delete emittiert Set-Cookie mit dem Namen)
        ctx.Response.Headers["Set-Cookie"].ToString().Should().Contain(WindowsAutoLoginMiddleware.ForceSsoCookie);
    }
```

Run (RED):
```bash
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && dotnet test IdealAkeWms.Tests --nologo
```
Erwartet: Kompilierfehler `CS0117: "WindowsAutoLoginMiddleware" enthaelt keine Definition fuer "ForceSsoCookie"` (die Konstante fehlt noch). Das ist RED.

- [ ] **Step 2: Middleware implementieren (GREEN)**

In `IdealAkeWms/Middleware/WindowsAutoLoginMiddleware.cs`:

(a) Cookie-Konstante ergaenzen — direkt nach `NoAutoLoginCookie`:

```csharp
    public const string AutoLoginTriedCookie = "IdealAkeWms.AutoLoginTried";
    public const string NoAutoLoginCookie = "IdealAkeWms.NoAutoLogin";
    public const string ForceSsoCookie = "IdealAkeWms.ForceSso";
```

(b) `TryAutoLoginOrChallengeAsync` komplett ersetzen (SAM-Erfolg + SAM-Fail loeschen ForceSso; anonym-Zweig gated + loescht ForceSso an jedem Ausgang; KEIN AutoLoginTried-Cookie wenn nicht gechallenged). Ersetze die komplette Methode (Zeilen ab `private async Task<bool> TryAutoLoginOrChallengeAsync` bis zur schliessenden `}` der Methode) durch:

```csharp
    /// <returns>true = Challenge ausgeloest (Response uebernommen, kein next()).</returns>
    private async Task<bool> TryAutoLoginOrChallengeAsync(HttpContext context)
    {
        var isAuthenticated = context.User?.Identity?.IsAuthenticated == true;
        var identityName = context.User?.Identity?.Name;
        // DIAGNOSE: zeigt, ob/welche Windows-Identitaet von IIS ankommt.
        _logger.LogInformation(
            "WindowsAutoLogin-Diagnose: Pfad={Path} IsAuthenticated={IsAuth} IdentityName={IdentityName} AuthType={AuthType}",
            context.Request.Path.Value, isAuthenticated, identityName ?? "(null)",
            context.User?.Identity?.AuthenticationType ?? "(null)");

        if (isAuthenticated)
        {
            var sam = WindowsAccountHelper.ExtractSam(identityName);
            var user = sam == null ? null : await _userRepository.GetActiveByWindowsUserNameAsync(sam);
            if (user != null)
            {
                context.Session.SetInt32(CurrentUserService.SessionKeyUserId, user.Id);
                context.Session.SetString(CurrentUserService.SessionKeyUserName, user.Name);
                context.Response.Cookies.Delete(AutoLoginTriedCookie);
                context.Response.Cookies.Delete(NoAutoLoginCookie);
                context.Response.Cookies.Delete(ForceSsoCookie);
                _logger.LogInformation(
                    "WindowsAutoLogin-Diagnose: TREFFER — SAM={Sam} -> UserId={UserId} ({UserName}), Auto-Login gesetzt.",
                    sam, user.Id, user.Name);
                return false;
            }
            _logger.LogInformation(
                "WindowsAutoLogin-Diagnose: KEIN aktiver Benutzer-Datensatz fuer SAM={Sam} (IdentityName={IdentityName}) -> Formular-Fallback.",
                sam ?? "(null)", identityName ?? "(null)");
            context.Response.Cookies.Delete(ForceSsoCookie);
            context.Response.Cookies.Append(AutoLoginTriedCookie, "1", new CookieOptions { HttpOnly = true, IsEssential = true });
            return false;
        }

        var forceSso = context.Request.Cookies.ContainsKey(ForceSsoCookie);

        if (!context.Request.Cookies.ContainsKey(AutoLoginTriedCookie))
        {
            var userAgent = context.Request.Headers.UserAgent.ToString();
            if (UserAgentHelper.IsWindowsDesktop(userAgent) || forceSso)
            {
                _logger.LogInformation(
                    "WindowsAutoLogin-Diagnose: anonym -> sende Negotiate-Challenge (Pfad={Path}, ForceSso={Force}).",
                    context.Request.Path.Value, forceSso);
                context.Response.Cookies.Append(AutoLoginTriedCookie, "1", new CookieOptions { HttpOnly = true, IsEssential = true });
                context.Response.Cookies.Delete(ForceSsoCookie);
                await _challenge.ChallengeAsync(context);
                return true;
            }
            _logger.LogInformation(
                "WindowsAutoLogin-Diagnose: anonym + Nicht-Windows-UA ohne ForceSso -> Formular-Fallback ohne Challenge (Pfad={Path}).",
                context.Request.Path.Value);
            return false;
        }

        if (forceSso)
            context.Response.Cookies.Delete(ForceSsoCookie);
        _logger.LogInformation(
            "WindowsAutoLogin-Diagnose: anonym + bereits gechallenged (AutoLoginTried-Cookie) -> Formular-Fallback (Pfad={Path}).",
            context.Request.Path.Value);
        return false;
    }
```

*(Kein neuer `using` noetig — `UserAgentHelper` liegt in `IdealAkeWms.Services`, das die Datei bereits mit `using IdealAkeWms.Services;` importiert. `WindowsAccountHelper` bleibt unveraendert.)*

Run (GREEN):
```bash
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && dotnet build IdealAkeWms.slnx && dotnet test IdealAkeWms.Tests --nologo
```
Erwartet: Build 0 Fehler; Tests `Fehler: 0`, `erfolgreich: 939` (= 936 + 3 neue Middleware-Facts), `uebersprungen: 1`, `gesamt: 940`. Insbesondere bleiben die bestehenden Middleware-Tests gruen (`Anonymous_NoTriedCookie_Challenges_NoNext` challenged jetzt via Windows-UA-Default).

- [ ] **Step 3: Commit**

```bash
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git add -A && git commit -F- <<'EOF'
feat(auth): UA-Gate + ForceSso-Cookie in WindowsAutoLoginMiddleware

Die anonym->Negotiate-Challenge feuert nur noch bei Windows-Desktop-UA
ODER gesetztem ForceSso-Cookie. Nicht-Windows-Clients (Android/iOS/Mac/
Linux) fallen prompt-frei aufs Formular durch und bekommen KEIN
AutoLoginTried-Cookie (spaeterer Button-Force bleibt moeglich). ForceSso
wird an jedem SSO-Pfad-Ausgang geloescht (SAM-Erfolg, SAM-Fail, Challenge,
bereits-gechallenged). SAM-Match-Zweig unveraendert UA-unabhaengig.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
```

---

## Task 3: `AccountController.WindowsLogin` + `WindowsAuthAktiv` an die Login-View (TDD)

**Files:**
- Modify: `IdealAkeWms.Tests/Controllers/AccountControllerTests.cs`
- Modify: `IdealAkeWms/Controllers/AccountController.cs`

**Test-Impact (verifiziert):** Der Konstruktor von `AccountController` bekommt eine neue Abhaengigkeit `IAppSettingRepository`. `AccountControllerTests.BuildController` baut den Controller manuell — es MUSS ein `Mock<IAppSettingRepository>` ergaenzt werden, sonst kompilieren die bestehenden Profile-Tests nicht mehr.

- [ ] **Step 1: Failing WindowsLogin-Test + BuildController anpassen (RED)**

In `IdealAkeWms.Tests/Controllers/AccountControllerTests.cs`:

(a) `using IdealAkeWms.Middleware;` zu den Usings hinzufuegen (fuer `WindowsAutoLoginMiddleware.*Cookie`).

(b) `BuildController` um den neuen Mock erweitern. Ersetze den Body-Anfang:

```csharp
        var passwordService = new Mock<IPasswordService>();
        var currentUser = new Mock<ICurrentUserService>();
```
durch:
```csharp
        var passwordService = new Mock<IPasswordService>();
        var appSettings = new Mock<IAppSettingRepository>();
        var currentUser = new Mock<ICurrentUserService>();
```

und den `new AccountController(...)`-Aufruf. Ersetze:

```csharp
        var ctrl = new AccountController(
            userRepo.Object,
            passwordService.Object,
            currentUser.Object,
            workStepRepo.Object,
            workplaceRepo.Object);
```
durch (neuer Parameter als LETZTES Argument — Reihenfolge muss zur Konstruktor-Signatur in Step 2 passen):
```csharp
        var ctrl = new AccountController(
            userRepo.Object,
            passwordService.Object,
            currentUser.Object,
            workStepRepo.Object,
            workplaceRepo.Object,
            appSettings.Object);
```

(c) Neuen Test am Ende der Klasse (vor der schliessenden `}`) ergaenzen:

```csharp
    [Fact]
    public void WindowsLogin_SetsForceSso_DeletesAutoLoginCookies_RedirectsHome()
    {
        var ctrl = BuildController(new Mock<IUserRepository>());

        var result = ctrl.WindowsLogin();

        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be("Index");
        redirect.ControllerName.Should().Be("Home");

        var setCookie = ctrl.Response.Headers["Set-Cookie"].ToString();
        setCookie.Should().Contain($"{WindowsAutoLoginMiddleware.ForceSsoCookie}=1");
        setCookie.Should().Contain(WindowsAutoLoginMiddleware.NoAutoLoginCookie);
        setCookie.Should().Contain(WindowsAutoLoginMiddleware.AutoLoginTriedCookie);
    }
```

Run (RED):
```bash
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && dotnet test IdealAkeWms.Tests --nologo
```
Erwartet: Kompilierfehler — `AccountController` hat (noch) keinen 6-Parameter-Konstruktor und keine `WindowsLogin`-Methode (`CS1729`/`CS1061`). RED.

- [ ] **Step 2: Controller implementieren (GREEN)**

In `IdealAkeWms/Controllers/AccountController.cs`:

(a) Using ergaenzen (fuer `AppSettingKeys` in `IdealAkeWms.Models`):

```csharp
using IdealAkeWms.Models;
```
(oberhalb/neben den bestehenden `using IdealAkeWms.Models.ViewModels;`).

(b) Feld + Konstruktor-Parameter ergaenzen. Ersetze den Feld-Block:

```csharp
    private readonly IProductionWorkplaceRepository _productionWorkplaceRepository;

    public AccountController(
        IUserRepository userRepository,
        IPasswordService passwordService,
        ICurrentUserService currentUserService,
        IWorkStepRepository workStepRepository,
        IProductionWorkplaceRepository productionWorkplaceRepository)
    {
        _userRepository = userRepository;
        _passwordService = passwordService;
        _currentUserService = currentUserService;
        _workStepRepository = workStepRepository;
        _productionWorkplaceRepository = productionWorkplaceRepository;
    }
```
durch:
```csharp
    private readonly IProductionWorkplaceRepository _productionWorkplaceRepository;
    private readonly IAppSettingRepository _appSettingRepository;

    public AccountController(
        IUserRepository userRepository,
        IPasswordService passwordService,
        ICurrentUserService currentUserService,
        IWorkStepRepository workStepRepository,
        IProductionWorkplaceRepository productionWorkplaceRepository,
        IAppSettingRepository appSettingRepository)
    {
        _userRepository = userRepository;
        _passwordService = passwordService;
        _currentUserService = currentUserService;
        _workStepRepository = workStepRepository;
        _productionWorkplaceRepository = productionWorkplaceRepository;
        _appSettingRepository = appSettingRepository;
    }
```

(c) `Login`-GET async machen + `WindowsAuthAktiv` an die View. Ersetze:

```csharp
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        // Wenn bereits eingeloggt, zum Dashboard
        if (HttpContext.Session.GetInt32(CurrentUserService.SessionKeyUserId).HasValue)
            return RedirectToAction("Index", "Home");

        ViewBag.ReturnUrl = returnUrl;
        return View(new LoginViewModel());
    }
```
durch:
```csharp
    [HttpGet]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        // Wenn bereits eingeloggt, zum Dashboard
        if (HttpContext.Session.GetInt32(CurrentUserService.SessionKeyUserId).HasValue)
            return RedirectToAction("Index", "Home");

        ViewBag.ReturnUrl = returnUrl;
        var flag = await _appSettingRepository.GetValueAsync(AppSettingKeys.WindowsAuthAktiv);
        ViewBag.WindowsAuthAktiv = string.Equals(flag, "true", StringComparison.OrdinalIgnoreCase);
        return View(new LoginViewModel());
    }
```

(d) Neue `WindowsLogin`-GET-Action direkt NACH der `Logout`-Action einfuegen (nutzt dieselben Cookie-Muster wie `Login`-POST / `Logout`):

```csharp
    [HttpGet]
    public IActionResult WindowsLogin()
    {
        // Force-SSO: einmalige Negotiate-Challenge auch fuer per UA nicht erkannte
        // Windows-Clients erzwingen. /Account/* ist von der Middleware ausgeschlossen,
        // deshalb auf / (Home) redirecten, wo die Middleware greift.
        Response.Cookies.Delete(Middleware.WindowsAutoLoginMiddleware.NoAutoLoginCookie);
        Response.Cookies.Delete(Middleware.WindowsAutoLoginMiddleware.AutoLoginTriedCookie);
        Response.Cookies.Append(Middleware.WindowsAutoLoginMiddleware.ForceSsoCookie, "1",
            new CookieOptions { HttpOnly = true, IsEssential = true });
        return RedirectToAction("Index", "Home");
    }
```

Run (GREEN):
```bash
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && dotnet build IdealAkeWms.slnx && dotnet test IdealAkeWms.Tests --nologo
```
Erwartet: Build 0 Fehler; Tests `Fehler: 0`, `erfolgreich: 940` (= 939 + 1), `uebersprungen: 1`, `gesamt: 941`.

- [ ] **Step 3: Commit**

```bash
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git add -A && git commit -F- <<'EOF'
feat(auth): AccountController.WindowsLogin + WindowsAuthAktiv an Login-View

WindowsLogin (GET) loescht NoAutoLogin/AutoLoginTried, setzt ForceSso und
redirected auf Home -> dort erzwingt die Middleware die SSO-Challenge auch
bei per UA nicht erkannten Windows-Clients. Login-GET reicht WindowsAuthAktiv
via ViewBag an die View (fuer den Button in Task 4).

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
```

---

## Task 4: Login-View — „Mit Windows anmelden"-Button (nur bei WindowsAuthAktiv)

**Files:**
- Modify: `IdealAkeWms/Views/Account/Login.cshtml`

*(Razor-View → verifiziert per Build, dass die View kompiliert. Keine Unit-Tests fuer reines Markup.)*

- [ ] **Step 1: Button einfuegen**

In `IdealAkeWms/Views/Account/Login.cshtml`, direkt NACH dem schliessenden `</form>` (Zeile ~83) und VOR dem `</div>` der `.login-body`, einfuegen:

```html
                @{
                    bool windowsAuthAktiv = ViewBag.WindowsAuthAktiv == true;
                }
                @if (windowsAuthAktiv)
                {
                    <div class="text-center mt-3">
                        <div class="text-muted small mb-2">oder</div>
                        <a asp-controller="Account" asp-action="WindowsLogin"
                           class="btn btn-outline-primary btn-lg w-100">Mit Windows anmelden</a>
                    </div>
                }
```

*(`ViewBag.WindowsAuthAktiv == true` ist null-sicher: bei nicht gesetztem ViewBag — z. B. POST-Re-Render mit Validierungsfehler — ist der Ausdruck `false`, der Button bleibt aus. Kein Icon, da die Login-Seite bootstrap-icons NICHT einbindet.)*

- [ ] **Step 2: Build (Razor kompiliert)**

Run:
```bash
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && dotnet build IdealAkeWms.slnx && dotnet test IdealAkeWms.Tests --nologo
```
Erwartet: Build 0 Fehler; Tests unveraendert `Fehler: 0`, `erfolgreich: 940`, `uebersprungen: 1`, `gesamt: 941`.

- [ ] **Step 3: Commit**

```bash
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git add -A && git commit -F- <<'EOF'
feat(auth): Login-Formular mit "Mit Windows anmelden"-Button (gated)

Button erscheint nur bei WindowsAuthAktiv und verlinkt auf
/Account/WindowsLogin (Force-SSO). Macht das UA-Gate verlustfrei fuer
Nicht-Windows-erkannte Domaenen-Clients.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
```

---

## Task 5: Doku + Final-Check

**Files:**
- Modify: `IdealAkeWms/Views/Help/Changelog.cshtml`
- Modify: `CLAUDE.md`
- Modify: `docs/TESTSZENARIEN.md`
- Modify: `PROJECT_STATUS.md`

- [ ] **Step 1: Changelog — Bullet an die bestehende v1.25.0-Karte**

In `IdealAkeWms/Views/Help/Changelog.cshtml`, in der **bestehenden** `v1.25.0`-Karte (KEINE neue Karte, KEIN Datum aendern), am Ende der `<ul>` (nach dem letzten `<li>`, vor `</ul>`) einfuegen:

```html
                    <li><strong>Windows-Anmeldung mobilfreundlich:</strong> Die automatische
                        Windows-Anmeldung fragt jetzt nur noch auf Windows-Desktop-Browsern nach.
                        Handys und Tablets (Android/iPhone/iPad) sowie Mac/Linux bekommen sofort das
                        normale Anmelde-Formular &ndash; ohne unerfuellbaren Windows-Anmeldedialog. Wer
                        an einem nicht erkannten Windows-Geraet doch die Windows-Anmeldung will, nutzt
                        den neuen Button <em>&bdquo;Mit Windows anmelden&ldquo;</em> auf der Anmeldeseite
                        (erscheint nur bei aktivierter Windows-Anmeldung).</li>
```

- [ ] **Step 2: CLAUDE.md — Session & Auth + neuer Fallstrick**

(a) Im Abschnitt **„## Session & Authentifizierung"**, an den `Middleware-Reihenfolge`-Bulletpoint (der `WindowsAutoLoginMiddleware` beschreibt) folgenden Satz anhaengen (im selben Bullet, nach „… erfolgreicher Login loescht ihn."):

```
Seit v1.25.0-Fold ist die anonym->Challenge UA-gegated: nur Windows-Desktop-Browser (`UserAgentHelper.IsWindowsDesktop`) ODER ein gesetzter `ForceSsoCookie` loesen die Negotiate-Challenge aus — Nicht-Windows-Clients (Android/iOS/Mac/Linux) fallen prompt-frei aufs Formular durch. Der Button „Mit Windows anmelden" auf dem Login-Formular (nur bei `WindowsAuthAktiv`) ruft `AccountController.WindowsLogin` (GET): loescht NoAutoLogin/AutoLoginTried, setzt `ForceSsoCookie` und redirected auf `/`, wo die Middleware die Challenge erzwingt.
```

(b) Im Abschnitt **„## Bekannte Fallstricke"**, am Ende der Liste einen neuen Fallstrick-Bullet ergaenzen:

```
- **Windows-Auth UA-Gate + ForceSso (v1.25.0-Fold)**: Das UA-Gate sitzt AUSSCHLIESSLICH um die anonym->Negotiate-Challenge in `WindowsAutoLoginMiddleware`, NICHT um den SAM-Match — ein bereits von IIS authentifizierter Request (auch nach Button-Force) wird IMMER gematcht (UA egal). Drei harte Regeln: (1) **Kein `AutoLoginTried`-Cookie setzen, wenn NICHT gechallenged wird** (Nicht-Windows-UA ohne ForceSso) — sonst blockiert der `if(!AutoLoginTried)`-Gate einen spaeteren Button-Force. (2) **`ForceSsoCookie` (`IdealAkeWms.ForceSso`) an JEDEM SSO-Pfad-Ausgang loeschen** (SAM-Erfolg, SAM-Fail, Challenge-ausgeloest, anonym-bereits-gechallenged) — kein verwaister Force-Cookie. (3) **Button nur bei `WindowsAuthAktiv`** (sonst toter Button auf Nicht-SSO-Systemen). UA ist spoofbar — bewusst akzeptiert: Fehldetektion fuehrt IMMER nur zum sicheren Formular (+ Button-Fallback), nie zu Zugriff ohne gueltige Windows-Credentials. `UserAgentHelper.IsWindowsDesktop` ist unit-getestet; der echte Negotiate-Handshake bleibt Manual-UAT (TESTSZENARIEN Kap. 40). **Kein neues AppSetting** — nur die Cookie-Konstante. `AccountController` injiziert seit diesem Fold `IAppSettingRepository` (Login-GET reicht `WindowsAuthAktiv` per ViewBag an die View).
```

- [ ] **Step 3: TESTSZENARIEN — Kapitel 40 erweitern**

In `docs/TESTSZENARIEN.md`, im **Kapitel 40**, nach dem bestehenden `### TS-40.6`-Block (und dessen Inhalt) drei neue Szenarien anhaengen:

```markdown
### TS-40.7 — Android/iPhone → sofort Formular, KEIN Windows-Dialog (v1.25.0-Fold)

**Vorbedingungen:** `WindowsAuthAktiv = true`. Aufruf von einem Android-Handy
oder iPhone/iPad (Mobile-Browser), frische Session (keine Cookies).

**Schritte:**
1. Die App-Startseite vom Mobilgeraet aufrufen.

**Erwartet:** Es erscheint **direkt** das Anmelde-Formular. **Kein**
Windows-Anmeldedialog, kein Passwort-Prompt-Popup. Es wird KEINE
Negotiate-Challenge gesendet (UA-Gate). Eine Formular-Anmeldung mit lokalen
Credentials funktioniert normal. (Auch nach mehrfachem Neuladen kein Dialog.)

### TS-40.8 — Windows-Desktop → Auto-Login wie bisher (v1.25.0-Fold)

**Vorbedingungen:** Wie TS-40.1 (`WindowsAuthAktiv = true`, AD-Benutzer-Datensatz
vorhanden), aber ausdruecklich aus einem **Windows-Desktop-Browser** (Chrome/Edge/
Firefox auf Windows, Intranet-Zone).

**Schritte:**
1. Frische Session, App-Startseite aufrufen.

**Erwartet:** Der Windows-Desktop-Browser erhaelt die stille Negotiate-Challenge
und der Benutzer wird **ohne Formular** angemeldet (Verhalten exakt wie vor dem
UA-Gate). Genau eine Challenge je Session (`AutoLoginTried`-Cookie).

### TS-40.9 — Button „Mit Windows anmelden" erzwingt SSO auf Nicht-Windows-Client (v1.25.0-Fold)

**Vorbedingungen:** `WindowsAuthAktiv = true`. Ein Windows-Geraet, das per UA
NICHT als Windows-Desktop erkannt wird (exotischer Browser) ODER ein Mac/Linux im
Domaenennetz, dessen Windows-Identitaet IIS liefern kann. AD-Benutzer-Datensatz
zum angemeldeten Windows-Konto vorhanden.

**Schritte:**
1. App-Startseite aufrufen → es erscheint das Formular (kein Auto-Login, da UA
   nicht als Windows-Desktop erkannt).
2. Pruefen: Der Button **„Mit Windows anmelden"** ist sichtbar.
3. Auf den Button klicken.

**Erwartet:**
- Der Button fuehrt auf `/Account/WindowsLogin` (setzt Force-Cookie) und weiter
  auf die Startseite; dort erzwingt die Middleware die Negotiate-Challenge.
- Nach erfolgreichem SAM-Match ist der Benutzer angemeldet (Dashboard); der
  Force-Cookie ist wieder geloescht (kein Challenge-Loop).
- **Negativfall:** Ist `WindowsAuthAktiv = false`, erscheint der Button **nicht**.
```

- [ ] **Step 4: PROJECT_STATUS — Bullet in der v1.25.0-Sektion**

In `PROJECT_STATUS.md`, in der Sektion **„### v1.25.0 (2026-07-03) — Glas-Bestellung als eigener Bestelltyp"**, am Ende der Bullet-Liste (nach dem `Bugfix Lagerbestand-Nullsetzen`-Bullet) ergaenzen:

```
- **Windows-Auth UA-Gate + SSO-Button (v1.25.0-Fold):** `WindowsAutoLoginMiddleware` sendet die Negotiate-Challenge nur noch bei Windows-Desktop-UA (`UserAgentHelper.IsWindowsDesktop`) ODER gesetztem `ForceSsoCookie`; Nicht-Windows-Clients (Android/iOS/Mac/Linux) fallen prompt-frei aufs Formular durch (kein `AutoLoginTried`-Cookie, wenn nicht gechallenged). Neue GET-Action `AccountController.WindowsLogin` (loescht NoAutoLogin/AutoLoginTried, setzt ForceSso, Redirect Home) + Button „Mit Windows anmelden" im Login-Formular (nur bei `WindowsAuthAktiv`). Kein Schema-Change, keine Migration, **kein AppVersion-Bump**, kein neues AppSetting (nur Cookie-Konstante). Tests: `UserAgentHelperTests` (Theory) + erweiterte `WindowsAutoLoginMiddlewareTests` (Android/ForceSso) + `AccountControllerTests.WindowsLogin`. Manual-UAT: TESTSZENARIEN Kap. 40 (TS-40.7–40.9).
```

- [ ] **Step 5: Final-Check (Build + Tests + Invarianten)**

Run:
```bash
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && dotnet build IdealAkeWms.slnx && dotnet test IdealAkeWms.Tests --nologo
```
Erwartet: Build 0 Fehler; Tests `Fehler: 0`, `erfolgreich: 940`, `uebersprungen: 1`, `gesamt: 941`.

Invarianten-Checks (duerfen KEIN Ergebnis liefern bzw. den erwarteten Wert zeigen):
```bash
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && grep -n 'Version = ' IdealAkeWms/AppVersion.cs
```
Erwartet: unveraendert `public const string Version = "1.25.0";` (KEIN Bump).

```bash
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git ls-files --others --exclude-standard IdealAkeWms/Migrations && git diff --name-only HEAD~4 -- IdealAkeWms/Migrations SQL
```
Erwartet: beide leer — KEINE neue Migration, KEIN SQL-Schema-File.

- [ ] **Step 6: Commit**

```bash
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git add -A && git commit -F- <<'EOF'
docs(auth): Windows-Auth UA-Gate + SSO-Button (Changelog/CLAUDE/TESTSZENARIEN/STATUS)

Changelog v1.25.0-Bullet, CLAUDE.md (Session&Auth + neuer Fallstrick),
TESTSZENARIEN Kap. 40 (TS-40.7 Android->Formular, TS-40.8 Windows->SSO,
TS-40.9 Button-Force), PROJECT_STATUS v1.25.0-Bullet. Kein AppVersion-Bump,
keine Migration.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
```

---

## File-Struktur-Uebersicht (Aenderungen)

**Neu:**
- `IdealAkeWms/Services/UserAgentHelper.cs` — reiner Helfer `IsWindowsDesktop(string?)`.
- `IdealAkeWms.Tests/Services/UserAgentHelperTests.cs` — Theory (14 Zeilen).

**Geaendert (Code):**
- `IdealAkeWms/Middleware/WindowsAutoLoginMiddleware.cs` — Cookie-Konstante `ForceSsoCookie`; UA-Gate + ForceSso-Loeschung in `TryAutoLoginOrChallengeAsync`.
- `IdealAkeWms/Controllers/AccountController.cs` — `IAppSettingRepository`-Injektion; `Login`-GET async + `ViewBag.WindowsAuthAktiv`; neue `WindowsLogin`-GET-Action; `using IdealAkeWms.Models;`.
- `IdealAkeWms/Views/Account/Login.cshtml` — gated Button „Mit Windows anmelden".

**Geaendert (Tests):**
- `IdealAkeWms.Tests/Middleware/WindowsAutoLoginMiddlewareTests.cs` — `MakeContext` UA-Param + 3 neue Facts.
- `IdealAkeWms.Tests/Controllers/AccountControllerTests.cs` — `BuildController` + `IAppSettingRepository`-Mock; `WindowsLogin`-Test; `using IdealAkeWms.Middleware;`.

**Geaendert (Doku):**
- `IdealAkeWms/Views/Help/Changelog.cshtml`, `CLAUDE.md`, `docs/TESTSZENARIEN.md`, `PROJECT_STATUS.md`.

**Unberuehrt (bewusst):** `IdealAkeWms/AppVersion.cs`, `IDEALAKEWMSService/*`, `IdealAkeWms/Program.cs` (DI von `IChallengeIssuer`/Middleware bleibt), `SQL/*`, `Migrations/*`, `AppSettingKeys.cs` (kein neuer Key).

---

## Fallstricke (verifiziert gegen den realen Code)

1. **Middleware-Test-Bau — `MakeContext` hat KEINEN User-Agent** (verifiziert `WindowsAutoLoginMiddlewareTests.cs:17-26`): Kontext via `new DefaultHttpContext()`, `ctx.Request.Path = "/Home/Index"`, `ctx.User` = `ClaimsPrincipal` (authentifiziert: `ClaimsIdentity` mit `ClaimTypes.Name` + AuthType `"Negotiate"`; anonym: leere `ClaimsIdentity`), Session via `ctx.Features.Set<ISessionFeature>(new SessionFeatureStub())` (Backing = `FakeSession` mit Dictionary, `IdealAkeWms.Tests/Helpers/FakeSession.cs`). **Konsequenz:** Ohne UA-Default wuerde das UA-Gate den bestehenden `Anonymous_NoTriedCookie_Challenges_NoNext` brechen → deshalb `MakeContext`-UA-Param mit `WindowsUa`-Default (Task 2, Step 1). UA im Test setzbar via `ctx.Request.Headers.UserAgent = "..."` (bestaetigt: `IHeaderDictionary.UserAgent`-Property, StringValues, implizite string-Konvertierung).
2. **`IChallengeIssuer`-Fake** (verifiziert `IChallengeIssuer.cs` liegt in **`IdealAkeWms/Middleware/`**, NICHT Services): Interface `Task ChallengeAsync(HttpContext context)`, Impl `IISChallengeIssuer` → `context.ChallengeAsync(IISServerDefaults.AuthenticationScheme)`. Test-Fake = `new Mock<IChallengeIssuer>()`, Aufruf-Zaehlung via `challenge.Verify(c => c.ChallengeAsync(ctx), Times.Once/Never)`. Kein Setup noetig (default `Task` = null → aber `await` auf `Times.Never`-Pfaden trifft ihn nicht; auf `Once`-Pfaden gibt Moq default `Task.CompletedTask` via `.Returns` NICHT — Achtung: Moq liefert fuer nicht-setup `Task`-Rueckgaben automatisch ein `completed` Task, weil `Mock` default `DefaultValue.Empty` fuer `Task` einen abgeschlossenen Task liefert). In den bestehenden Tests wird `ChallengeAsync` ohne `.Setup` gemockt und `await`et — funktioniert (Baseline gruen). Neue Tests folgen exakt diesem Muster.
3. **Cookies im Test**: Request-Cookies via `ctx.Request.Headers["Cookie"] = "name=1"` setzen (verifiziert in `Anonymous_TriedCookieSet_*` / `NoAutoLoginCookie_*`). Response-Cookies (Append/Delete) landen als `Set-Cookie`-Header → Assertion `ctx.Response.Headers["Set-Cookie"].ToString().Should().Contain(<CookieName>)` (verifiziert in `IdentityNoMatch_SetsTriedCookie_NoSession`). `Response.Cookies.Delete(name)` emittiert ebenfalls einen `Set-Cookie`-Header MIT dem Namen (abgelaufenes Datum) — deshalb pruefen die Loesch-Assertions auf `Contain(<Name>)`, die Setz-Assertion auf `Contain("<Name>=1")`.
4. **`AccountControllerTests` existiert** (`IdealAkeWms.Tests/Controllers/AccountControllerTests.cs`) — `BuildController`-Factory baut den Controller manuell mit `DefaultHttpContext` + `TempDataDictionary`. **Konstruktor-Erweiterung um `IAppSettingRepository` ist Test-breaking** → `BuildController` MUSS in Task 3 mitgezogen werden (neuer Mock als LETZTES Argument). `ctrl.Response` (= `HttpContext.Response`) ist im Test erreichbar → `WindowsLogin`-Cookie-Assertions moeglich, ohne Session.
5. **`WindowsAuthAktiv`-Read** (verifiziert Middleware `ShouldTryAsync`, `WindowsAutoLoginMiddleware.cs:55-56`): `await _appSettings.GetValueAsync(AppSettingKeys.WindowsAuthAktiv)` + `string.Equals(flag, "true", OrdinalIgnoreCase)`. Der `AccountController` nutzt dasselbe Muster (Task 3). `AppSettingKeys.WindowsAuthAktiv` existiert bereits (`Models/AppSettingKeys.cs:43`), liegt in Namespace `IdealAkeWms.Models` → `using IdealAkeWms.Models;` im Controller noetig. `IAppSettingRepository.GetValueAsync(string)` → `Task<string?>` (verifiziert).
6. **User-Agent-Zugriff in der Middleware**: `context.Request.Headers.UserAgent.ToString()` (typisierte `IHeaderDictionary`-Property, `StringValues` → `.ToString()` joint) — genau wie in der Spec §2. Kein zusaetzlicher `using` noetig (`Microsoft.AspNetCore.Http` bereits importiert).
7. **`UserAgentHelper`-Ablageort**: `IdealAkeWms/Services/` (Namespace `IdealAkeWms.Services`), analog `WindowsAccountHelper.cs`. Die Middleware importiert `IdealAkeWms.Services` bereits (`WindowsAutoLoginMiddleware.cs:3`) → direkte Nutzung ohne neuen `using`.
8. **Kein AppVersion-Bump / keine Migration** (verifiziert `AppVersion.cs` = `1.25.0`, in v1.25.0 gefaltet): Änderung ist rein Web-seitig; keine `dotnet ef migrations add`-Schritte, keine `SQL/`-Files, kein `FreshInstall`-Sync. Der Changelog erweitert die **bestehende** v1.25.0-Karte (kein neues Datum/Karte).
9. **DI unveraendert** (verifiziert `Program.cs:63-64,451`): `WindowsAutoLoginMiddleware` (scoped) + `IChallengeIssuer→IISChallengeIssuer` (scoped) sind registriert; `IAppSettingRepository` ist app-weit registriert (die Middleware nutzt es bereits) → der neue `AccountController`-Constructor-Param wird automatisch aufgeloest, keine Program.cs-Aenderung noetig.

## Offene Annahmen

- **POST-Login-Re-Render**: Bei Validierungsfehlern (`Login`-POST → `View(vm)`) wird `ViewBag.WindowsAuthAktiv` nicht gesetzt → der Button bleibt aus (der null-sichere `== true`-Check liefert `false`). Bewusst minimal gehalten (Spec-Task nennt nur Login-GET); falls der Button auch im Fehlerfall gewuenscht ist, muesste der POST-Pfad das Flag ebenfalls setzen — als Follow-up notiert, nicht Teil dieses Plans.
- **Test-Zahlen** (923 → 940 erfolgreich) gehen von genau 13 Theory-Zeilen (Task 1) + 3 Middleware-Facts (Task 2) + 1 Controller-Fact (Task 3) aus. Weichen die tatsaechlichen xUnit-Counts leicht ab (z. B. wenn eine Theory-Zeile anders zaehlt), gilt die harte Invariante: **`Fehler: 0`, `uebersprungen: 1`, gesamt = 924 + (neue Test-Faelle)** — nicht die exakte Zahl.
- **Manual-UAT unvermeidbar**: Der echte IIS-Negotiate-Handshake (401 Negotiate → still antwortender Domaenen-Browser) und das reale UA-Verhalten mobiler Browser sind nur am domaenen-gebundenen IIS-Zielsystem verifizierbar (TESTSZENARIEN Kap. 40, TS-40.7–40.9) — analog zur v1.23.0-Windows-Auth-Strecke.
