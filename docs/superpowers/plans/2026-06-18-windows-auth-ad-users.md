# Windows-Auth + AD-User-Rollen — Implementierungsplan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax.

**Goal:** Domänen-Benutzer melden sich per Windows-SSO automatisch an (mit Formular-Fallback), und AD-Benutzer einer Berechtigungsgruppe lassen sich wie lokale Benutzer mit Rollen anlegen.

**Architecture:** `WindowsAutoLoginMiddleware` (IMiddleware, vor LoginRedirect, hinter Flag `WindowsAuthAktiv`) überführt die von IIS gelieferte Windows-Identität in die App-Session (mit aktiver Negotiate-Challenge, da IIS-Anonymous aktiv bleibt). `IActiveDirectoryService` listet per LDAP die Mitglieder einer Berechtigungsgruppe für den „AD-User anlegen"-Picker. `User.WindowsUserName` (SAM) ist der Login-Schlüssel; die alte `Role.AdGroup`-Automatik entfällt (Rollen nur noch per `UserRole`).

**Tech Stack:** ASP.NET Core 10 MVC, EF Core 10 (SQL Server), IIS in-process Hosting, `System.DirectoryServices.AccountManagement`, xUnit + Moq + EF InMemory.

**Spec:** `docs/superpowers/specs/2026-06-18-windows-auth-ad-users-design.md`

**Branch/Worktree:** `feature/windows-auth-ad-users` im Worktree `.claude/worktrees/missingparts-include-pd`. Alle Befehle dort ausführen.

**Konventionen:** Code/Bezeichner Englisch, UI Deutsch. Build: `dotnet build IdealAkeWms/IdealAkeWms.csproj -c Debug`. Tests Web: `dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj -c Debug`. Nach jeder Migration `dotnet ef migrations has-pending-model-changes` = leer.

---

## Task 0: Pre-flight Baseline

**Files:** keine.

- [ ] **Step 1: Branch + sauberer Stand prüfen**

Run: `git -C . rev-parse --abbrev-ref HEAD` → erwartet `feature/windows-auth-ad-users`. `git status --short` → leer.

- [ ] **Step 2: Baseline-Build + Tests grün**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj -c Debug --nologo -v q` → 0 Fehler.
Run: `dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj -c Debug --nologo -v q` → alle grün (Baseline 719/1 skip).

---

## Task 1: AppSetting-Keys + appsettings.json

**Files:**
- Modify: `IdealAkeWms/Services/AppSettingKeys.cs`
- Modify: `IdealAkeWms/appsettings.json`

- [ ] **Step 1: Konstanten ergänzen**

In `AppSettingKeys.cs` zwei Konstanten hinzufügen (Muster der bestehenden Keys, z. B. `FaCompletionAktiv`):

```csharp
public const string WindowsAuthAktiv = "WindowsAuthAktiv";
public const string WindowsAuthBerechtigungsgruppe = "WindowsAuthBerechtigungsgruppe";
```

- [ ] **Step 2: appsettings.json — Security:AdDomain ergänzen, AdGroupCacheMinutes entfernen**

`Security`-Block ersetzen:

```json
  "Security": {
    "AdDomain": ""
  }
```

(`AdGroupCacheMinutes` entfällt — wird in Task 5 auch im Code entfernt.)

- [ ] **Step 3: Build + Commit**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj -c Debug --nologo -v q` → 0 Fehler.

```bash
git add IdealAkeWms/Services/AppSettingKeys.cs IdealAkeWms/appsettings.json
git commit -m "feat(auth): AppSetting-Keys WindowsAuthAktiv/Berechtigungsgruppe + Security:AdDomain"
```

---

## Task 2: User.WindowsUserName + Migration 73 (Spalte + AdGroup-Drop)

**Files:**
- Modify: `IdealAkeWms/Models/User.cs`
- Modify: `IdealAkeWms/Data/ApplicationDbContext.cs` (User-Config + Role-Config)
- Create: `IdealAkeWms/Migrations/<timestamp>_AddWindowsUserNameDropAdGroup.cs` (via `dotnet ef`)
- Create: `SQL/73_AddWindowsUserNameDropAdGroup.sql`
- Modify: `SQL/00_FreshInstall.sql`

- [ ] **Step 1: User-Feld ergänzen**

In `Models/User.cs` nach `PasswordHash` einfügen:

```csharp
    [StringLength(200)]
    [Display(Name = "Windows-Benutzer")]
    public string? WindowsUserName { get; set; }
```

- [ ] **Step 2: DbContext — User-Index + Role-AdGroup-Property entfernen**

In `ApplicationDbContext.cs` beim `User`-Entity-Konfigurationsblock ergänzen:

```csharp
            entity.Property(e => e.WindowsUserName).HasMaxLength(200);
            entity.HasIndex(e => e.WindowsUserName)
                .IsUnique()
                .HasFilter("[WindowsUserName] IS NOT NULL")
                .HasDatabaseName("UQ_Users_WindowsUserName");
```

Die Zeile `entity.Property(e => e.AdGroup).HasMaxLength(200);` (Role-Block, ~Zeile 101) **entfernen**.

- [ ] **Step 3: Role-Model — AdGroup entfernen**

In `Models/Role.cs` die Property `public string? AdGroup { get; set; }` (samt `[StringLength(200)]`/Display-Attribut) **entfernen**. (Folge-Compile-Fehler in RolesController/RoleEditViewModel/RoleRepository werden in Task 5 behandelt — diese Task lässt den Build evtl. rot; daher Task 5 unmittelbar danach. Falls der Migrations-Befehl einen grünen Build braucht: Role.AdGroup-Entfernung kann mit Task 5 zusammengezogen werden. **Empfehlung:** Step 3 hier NICHT machen, sondern komplett in Task 5 — diese Task entfernt AdGroup nur aus dem DbContext-Mapping-Snapshot via Migration. Siehe Step-Anpassung unten.)

> **Reihenfolge-Hinweis (wichtig):** Damit `dotnet ef migrations add` einen kompilierenden Stand braucht, wird das **Property-Entfernen aus `Role.cs` + alle Folge-Stellen in Task 5 erledigt, BEVOR** die Migration generiert wird. Praktisch: Task 5 VOR der Migration-Generierung ausführen, oder beide Tasks in einem Rutsch. Der Plan ordnet Task 5 als „Task 2b" direkt mit ein — siehe Task 5. Die Migration (dieser Task, Steps 4-7) wird **nach** Task 5 generiert.

- [ ] **Step 4: Migration generieren** (NACH Task 5)

Run: `dotnet ef migrations add AddWindowsUserNameDropAdGroup --project IdealAkeWms/IdealAkeWms.csproj`
Erwartet: Up() enthält `AddColumn<string>("WindowsUserName", "Users", maxLength:200, nullable:true)`, `CreateIndex(... unique, filter)`, `DropColumn("AdGroup", "Roles")`.

- [ ] **Step 5: Pending-Check**

Run: `dotnet ef migrations has-pending-model-changes --project IdealAkeWms/IdealAkeWms.csproj` → „No changes".

- [ ] **Step 6: SQL/73 idempotent schreiben**

`SQL/73_AddWindowsUserNameDropAdGroup.sql`:

```sql
-- 73: WindowsUserName auf Users + Drop Roles.AdGroup
IF COL_LENGTH('dbo.Users', 'WindowsUserName') IS NULL
    ALTER TABLE dbo.Users ADD WindowsUserName NVARCHAR(200) NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Users_WindowsUserName' AND object_id = OBJECT_ID('dbo.Users'))
    CREATE UNIQUE INDEX UQ_Users_WindowsUserName ON dbo.Users(WindowsUserName) WHERE WindowsUserName IS NOT NULL;
GO
IF COL_LENGTH('dbo.Roles', 'AdGroup') IS NOT NULL
    ALTER TABLE dbo.Roles DROP COLUMN AdGroup;
GO
IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '<timestamp>_AddWindowsUserNameDropAdGroup')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('<timestamp>_AddWindowsUserNameDropAdGroup', '10.0.0');
GO
```

`<timestamp>` durch den real generierten Migrations-Präfix ersetzen.

- [ ] **Step 7: FreshInstall aktualisieren**

In `SQL/00_FreshInstall.sql`: (a) im `Users`-CREATE TABLE die Spalte `[WindowsUserName] NVARCHAR(200) NULL` + den gefilterten Unique-Index ergänzen; (b) im `Roles`-CREATE TABLE die Spalte `[AdGroup]` **entfernen**; (c) in allen Roles-Seed-`INSERT`-Statements die `[AdGroup]`-Spalte + deren Wert **entfernen**; (d) History-Insert `<timestamp>_AddWindowsUserNameDropAdGroup` am Ende ergänzen.

- [ ] **Step 8: Build + Commit**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj -c Debug --nologo -v q` → 0 Fehler.

```bash
git add IdealAkeWms/Models/User.cs IdealAkeWms/Data/ApplicationDbContext.cs IdealAkeWms/Migrations/ SQL/73_AddWindowsUserNameDropAdGroup.sql SQL/00_FreshInstall.sql
git commit -m "feat(auth): User.WindowsUserName + Migration 73 (Spalte + Drop Roles.AdGroup)"
```

---

## Task 5 (= 2b): Role.AdGroup-Automatik vollständig entfernen

> Wird VOR der Migration-Generierung (Task 2 Step 4) ausgeführt, damit der Build kompiliert.

**Files:**
- Modify: `IdealAkeWms/Models/Role.cs` (Property entfernen)
- Modify: `IdealAkeWms/Models/ViewModels/RoleEditViewModel.cs` (AdGroup entfernen)
- Modify: `IdealAkeWms/Controllers/RolesController.cs` (ColumnMap `ad-group`, 4 AdGroup-Zuweisungen entfernen)
- Modify: `IdealAkeWms/Data/Repositories/IRoleRepository.cs` (GetRolesWithAdGroupAsync entfernen)
- Modify: `IdealAkeWms/Data/Repositories/RoleRepository.cs` (GetRolesWithAdGroupAsync entfernen)
- Modify: `IdealAkeWms/Services/CurrentUserService.cs` (GetAdGroupRolesAsync + Union + unbenutzte Deps entfernen)
- Modify: `IdealAkeWms/Views/Roles/Edit.cshtml` (AdGroup-Feld entfernen)
- Modify: `IdealAkeWms/Views/Roles/Create.cshtml` (AdGroup-Feld entfernen, falls vorhanden)
- Modify: `IdealAkeWms/Views/Roles/Index.cshtml` (AdGroup-Spalte `ad-group` entfernen)
- Modify: `IdealAkeWms.Tests/Services/CurrentUserServiceRoleTests.cs`
- Modify: `IdealAkeWms.Tests/Services/CurrentUserServiceIsAdminTests.cs`

- [ ] **Step 1: CurrentUserService.LoadRoleKeysAsync — Union + Methode entfernen**

`LoadRoleKeysAsync` so kürzen (AD-Block raus):

```csharp
private async Task<HashSet<string>> LoadRoleKeysAsync()
{
    if (_cachedRoleKeys != null)
        return _cachedRoleKeys;

    var roleKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var userId = GetCurrentAppUserId();
    if (userId.HasValue)
    {
        var directRoles = await _roleRepository.GetRoleKeysByUserIdAsync(userId.Value);
        foreach (var key in directRoles)
            roleKeys.Add(key);
    }

    _cachedRoleKeys = roleKeys;
    return roleKeys;
}
```

Die komplette Methode `GetAdGroupRolesAsync()` **entfernen**. Den `using Microsoft.Extensions.Caching.Memory;` belassen oder entfernen je nach weiterer Nutzung.

- [ ] **Step 2: CurrentUserService — unbenutzte Deps `_memoryCache`/`_configuration` entfernen**

Felder + Konstruktor-Parameter `IMemoryCache memoryCache` und `IConfiguration configuration` entfernen (werden nur von der gelöschten Methode genutzt). Falls `_configuration`/`_memoryCache` anderswo in der Datei verwendet werden: belassen. (Per `grep` prüfen.)

- [ ] **Step 3: RoleRepository + Interface — GetRolesWithAdGroupAsync entfernen**

Aus `IRoleRepository.cs` die Zeile `Task<List<Role>> GetRolesWithAdGroupAsync();` und aus `RoleRepository.cs` die Implementierung (Zeilen ~54-59) entfernen.

- [ ] **Step 4: RolesController — AdGroup raus**

- ColumnMap-Eintrag `["ad-group"] = r => r.AdGroup,` entfernen.
- Alle `AdGroup = …`-Zuweisungen entfernen (Index-Mapping, Create, Edit-System, Edit-Custom — 4 Stellen).

- [ ] **Step 5: RoleEditViewModel — AdGroup entfernen**

`public string? AdGroup { get; set; }` (+ Attribute) entfernen.

- [ ] **Step 6: Views — AdGroup-Markup entfernen**

- `Roles/Edit.cshtml`: den `<div class="mb-3">…asp-for="AdGroup"…</div>`-Block entfernen.
- `Roles/Create.cshtml`: analoges AdGroup-Feld entfernen (falls vorhanden).
- `Roles/Index.cshtml`: `<th data-filterable data-col-key="ad-group">AD-Gruppe</th>` + die zugehörige `<td>`-Zelle in der Datenzeile entfernen.

- [ ] **Step 7: Tests anpassen**

In `CurrentUserServiceRoleTests.cs` + `CurrentUserServiceIsAdminTests.cs`:
- Das `roleRepoMock.Setup(r => r.GetRolesWithAdGroupAsync())…`-Setup entfernen.
- Den Config-Eintrag `{ "Security:AdGroupCacheMinutes", "5" }` entfernen.
- Die `CurrentUserService`-Konstruktion an die neue Signatur (ohne IMemoryCache/IConfiguration) anpassen.

- [ ] **Step 8: Build + Tests + Commit**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj -c Debug --nologo -v q` → 0 Fehler.
Run: `dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj -c Debug --nologo -v q` → grün.

```bash
git add -A
git commit -m "refactor(auth): Role.AdGroup-Automatik entfernt — Rollen nur noch per UserRole"
```

> Danach Task 2 Steps 4-8 (Migration generieren + SQL/73 + FreshInstall) ausführen.

---

## Task 3: WindowsAccountHelper.ExtractSam (TDD)

**Files:**
- Create: `IdealAkeWms/Services/WindowsAccountHelper.cs`
- Create: `IdealAkeWms.Tests/Services/WindowsAccountHelperTests.cs`

- [ ] **Step 1: Failing Test**

```csharp
using IdealAkeWms.Services;
using Xunit;
using FluentAssertions;

namespace IdealAkeWms.Tests.Services;

public class WindowsAccountHelperTests
{
    [Theory]
    [InlineData("AKE\\jmuster", "jmuster")]
    [InlineData("ake\\JMuster", "JMuster")]
    [InlineData("jmuster", "jmuster")]
    [InlineData("jmuster@ake.at", "jmuster")]
    [InlineData("DOMAIN\\sam@upn", "sam@upn")]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    public void ExtractSam_ParsesIdentityName(string? input, string? expected)
    {
        WindowsAccountHelper.ExtractSam(input).Should().Be(expected);
    }
}
```

- [ ] **Step 2: Test rot**

Run: `dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter WindowsAccountHelperTests -v q` → FAIL (Typ fehlt).

- [ ] **Step 3: Implementierung**

```csharp
namespace IdealAkeWms.Services;

/// <summary>Extrahiert den SAM-Account aus einem Windows-Identity-Namen.</summary>
public static class WindowsAccountHelper
{
    /// <summary>"DOMAIN\\sam" -> "sam"; "sam@domain" -> "sam"; sonst Eingabe getrimmt; leer -> null.</summary>
    public static string? ExtractSam(string? identityName)
    {
        if (string.IsNullOrWhiteSpace(identityName))
            return null;

        var value = identityName.Trim();
        var backslash = value.LastIndexOf('\\');
        if (backslash >= 0)
            return value[(backslash + 1)..];

        var at = value.IndexOf('@');
        if (at > 0)
            return value[..at];

        return value;
    }
}
```

- [ ] **Step 4: Test grün**

Run: `dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter WindowsAccountHelperTests -v q` → PASS (8).

- [ ] **Step 5: Commit**

```bash
git add IdealAkeWms/Services/WindowsAccountHelper.cs IdealAkeWms.Tests/Services/WindowsAccountHelperTests.cs
git commit -m "feat(auth): WindowsAccountHelper.ExtractSam + Tests"
```

---

## Task 4: UserRepository.GetActiveByWindowsUserNameAsync (TDD)

**Files:**
- Modify: `IdealAkeWms/Data/Repositories/IUserRepository.cs`
- Modify: `IdealAkeWms/Data/Repositories/UserRepository.cs`
- Create: `IdealAkeWms.Tests/Repositories/UserRepositoryWindowsAuthTests.cs`

- [ ] **Step 1: Failing Test** (Muster bestehender Repo-Tests; `TestDbContextFactory.Create()` nutzen)

```csharp
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using Xunit;
using FluentAssertions;

namespace IdealAkeWms.Tests.Repositories;

public class UserRepositoryWindowsAuthTests
{
    [Fact]
    public async Task GetActiveByWindowsUserNameAsync_MatchesCaseInsensitive_AndOnlyActive()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.Users.Add(new User { Name = "A", WindowsUserName = "jmuster", IsActive = true, CreatedBy="t", CreatedByWindows="t" });
        ctx.Users.Add(new User { Name = "B", WindowsUserName = "inaktiv", IsActive = false, CreatedBy="t", CreatedByWindows="t" });
        ctx.Users.Add(new User { Name = "C", WindowsUserName = null, IsActive = true, CreatedBy="t", CreatedByWindows="t" });
        await ctx.SaveChangesAsync();
        var repo = new UserRepository(ctx);

        (await repo.GetActiveByWindowsUserNameAsync("JMUSTER"))!.Name.Should().Be("A");
        (await repo.GetActiveByWindowsUserNameAsync("inaktiv")).Should().BeNull();   // inaktiv
        (await repo.GetActiveByWindowsUserNameAsync("unbekannt")).Should().BeNull();
        (await repo.GetActiveByWindowsUserNameAsync("")).Should().BeNull();
    }
}
```

(Falls `TestDbContextFactory.Create()` / `UserRepository`-Ctor abweichen: an bestehende Repo-Tests anpassen.)

- [ ] **Step 2: Test rot**

Run: `dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter UserRepositoryWindowsAuthTests -v q` → FAIL (Methode fehlt).

- [ ] **Step 3: Interface + Implementierung**

In `IUserRepository.cs` ergänzen:

```csharp
    Task<User?> GetActiveByWindowsUserNameAsync(string samAccountName);
```

In `UserRepository.cs` ergänzen (case-insensitiv in-memory-sicher via ToLower):

```csharp
public async Task<User?> GetActiveByWindowsUserNameAsync(string samAccountName)
{
    if (string.IsNullOrWhiteSpace(samAccountName))
        return null;

    var sam = samAccountName.Trim().ToLowerInvariant();
    return await _dbSet
        .FirstOrDefaultAsync(u => u.IsActive
            && u.WindowsUserName != null
            && u.WindowsUserName.ToLower() == sam);
}
```

- [ ] **Step 4: Test grün**

Run: `dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter UserRepositoryWindowsAuthTests -v q` → PASS.

- [ ] **Step 5: Commit**

```bash
git add IdealAkeWms/Data/Repositories/IUserRepository.cs IdealAkeWms/Data/Repositories/UserRepository.cs IdealAkeWms.Tests/Repositories/UserRepositoryWindowsAuthTests.cs
git commit -m "feat(auth): UserRepository.GetActiveByWindowsUserNameAsync + Tests"
```

---

## Task 6: IActiveDirectoryService (LDAP-Gruppenmitglieder)

**Files:**
- Modify: `IdealAkeWms/IdealAkeWms.csproj` (NuGet)
- Create: `IdealAkeWms/Services/AdUserCandidate.cs`
- Create: `IdealAkeWms/Services/IActiveDirectoryService.cs`
- Create: `IdealAkeWms/Services/ActiveDirectoryService.cs`
- Modify: `IdealAkeWms/Program.cs` (DI)

- [ ] **Step 1: NuGet hinzufügen**

Run: `dotnet add IdealAkeWms/IdealAkeWms.csproj package System.DirectoryServices.AccountManagement`

- [ ] **Step 2: DTO + Interface**

`AdUserCandidate.cs`:

```csharp
namespace IdealAkeWms.Services;

public record AdUserCandidate(string SamAccountName, string? DisplayName, string? Email, bool Enabled);
```

`IActiveDirectoryService.cs`:

```csharp
namespace IdealAkeWms.Services;

public interface IActiveDirectoryService
{
    /// <summary>Liest die Mitglieder der konfigurierten Berechtigungsgruppe. Bei jedem Fehler: leere Liste.</summary>
    Task<IReadOnlyList<AdUserCandidate>> GetAuthorizationGroupMembersAsync(CancellationToken ct = default);
}
```

- [ ] **Step 3: Implementierung (Windows-only, fehlertolerant)**

`ActiveDirectoryService.cs`:

```csharp
using System.DirectoryServices.AccountManagement;
using System.Runtime.Versioning;
using Microsoft.Extensions.Configuration;

namespace IdealAkeWms.Services;

public class ActiveDirectoryService : IActiveDirectoryService
{
    private readonly IAppSettingRepository _appSettings;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ActiveDirectoryService> _logger;

    public ActiveDirectoryService(IAppSettingRepository appSettings, IConfiguration configuration,
        ILogger<ActiveDirectoryService> logger)
    {
        _appSettings = appSettings;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AdUserCandidate>> GetAuthorizationGroupMembersAsync(CancellationToken ct = default)
    {
        var group = await _appSettings.GetValueAsync(AppSettingKeys.WindowsAuthBerechtigungsgruppe);
        if (string.IsNullOrWhiteSpace(group) || !OperatingSystem.IsWindows())
            return Array.Empty<AdUserCandidate>();

        var domain = _configuration["Security:AdDomain"];
        try
        {
            return QueryMembers(group.Trim(), string.IsNullOrWhiteSpace(domain) ? null : domain);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AD-Abfrage der Berechtigungsgruppe '{Group}' fehlgeschlagen", group);
            return Array.Empty<AdUserCandidate>();
        }
    }

    [SupportedOSPlatform("windows")]
    private static List<AdUserCandidate> QueryMembers(string group, string? domain)
    {
        using var ctx = domain == null
            ? new PrincipalContext(ContextType.Domain)
            : new PrincipalContext(ContextType.Domain, domain);
        using var grp = GroupPrincipal.FindByIdentity(ctx, IdentityType.SamAccountName, group);

        var result = new List<AdUserCandidate>();
        if (grp == null)
            return result;

        foreach (var member in grp.GetMembers())
        {
            using (member)
            {
                if (member is UserPrincipal up && !string.IsNullOrEmpty(up.SamAccountName))
                    result.Add(new AdUserCandidate(up.SamAccountName, up.DisplayName, up.EmailAddress, up.Enabled ?? true));
            }
        }
        return result.OrderBy(c => c.DisplayName ?? c.SamAccountName).ToList();
    }
}
```

- [ ] **Step 4: DI registrieren**

In `Program.cs` bei den Service-Registrierungen ergänzen:

```csharp
builder.Services.AddScoped<IActiveDirectoryService, ActiveDirectoryService>();
```

- [ ] **Step 5: Build + Commit**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj -c Debug --nologo -v q` → 0 Fehler (CA1416-Warnungen sind durch `[SupportedOSPlatform]`/`OperatingSystem.IsWindows()` abgedeckt; falls dennoch CA1416 als Fehler: `<NoWarn>CA1416</NoWarn>` für die Datei prüfen).

```bash
git add IdealAkeWms/IdealAkeWms.csproj IdealAkeWms/Services/AdUserCandidate.cs IdealAkeWms/Services/IActiveDirectoryService.cs IdealAkeWms/Services/ActiveDirectoryService.cs IdealAkeWms/Program.cs
git commit -m "feat(auth): IActiveDirectoryService (LDAP-Mitglieder der Berechtigungsgruppe)"
```

---

## Task 7: WindowsAutoLoginMiddleware + Auth-Registrierung (TDD)

**Files:**
- Create: `IdealAkeWms/Middleware/WindowsAutoLoginMiddleware.cs`
- Create: `IdealAkeWms.Tests/Middleware/WindowsAutoLoginMiddlewareTests.cs`
- Modify: `IdealAkeWms/Program.cs` (Auth-Registrierung + UseMiddleware + DI)

- [ ] **Step 1: Failing Tests**

```csharp
using System.Security.Claims;
using System.Security.Principal;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Middleware;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;
using FluentAssertions;

namespace IdealAkeWms.Tests.Middleware;

public class WindowsAutoLoginMiddlewareTests
{
    private static DefaultHttpContext MakeContext(bool authenticated, string? name)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = "/Home/Index";
        if (authenticated && name != null)
            ctx.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, name) }, "Negotiate", ClaimTypes.Name, ClaimTypes.Role));
        else
            ctx.User = new ClaimsPrincipal(new ClaimsIdentity());
        ctx.Session = new FakeSession();
        return ctx;
    }

    private static WindowsAutoLoginMiddleware Build(Mock<IUserRepository> repo, bool flag, Mock<IChallengeIssuer> challenge)
    {
        var settings = new Mock<IAppSettingRepository>();
        settings.Setup(s => s.GetValueAsync(AppSettingKeys.WindowsAuthAktiv)).ReturnsAsync(flag ? "true" : "false");
        return new WindowsAutoLoginMiddleware(settings.Object, repo.Object, challenge.Object,
            Mock.Of<ILogger<WindowsAutoLoginMiddleware>>());
    }

    [Fact]
    public async Task FlagOff_CallsNext_NoSession()
    {
        var repo = new Mock<IUserRepository>();
        var challenge = new Mock<IChallengeIssuer>();
        var ctx = MakeContext(true, "AKE\\jmuster");
        var called = false;
        await Build(repo, flag: false, challenge).InvokeAsync(ctx, _ => { called = true; return Task.CompletedTask; });
        called.Should().BeTrue();
        ctx.Session.GetInt32(CurrentUserService.SessionKeyUserId).Should().BeNull();
        challenge.Verify(c => c.ChallengeAsync(It.IsAny<HttpContext>()), Times.Never);
    }

    [Fact]
    public async Task IdentityMatches_SetsSession()
    {
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetActiveByWindowsUserNameAsync("jmuster"))
            .ReturnsAsync(new User { Id = 42, Name = "Max", WindowsUserName = "jmuster", IsActive = true });
        var ctx = MakeContext(true, "AKE\\jmuster");
        var called = false;
        await Build(repo, flag: true, new Mock<IChallengeIssuer>()).InvokeAsync(ctx, _ => { called = true; return Task.CompletedTask; });
        called.Should().BeTrue();
        ctx.Session.GetInt32(CurrentUserService.SessionKeyUserId).Should().Be(42);
    }

    [Fact]
    public async Task IdentityNoMatch_SetsTriedCookie_NoSession()
    {
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetActiveByWindowsUserNameAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        var ctx = MakeContext(true, "AKE\\unknown");
        await Build(repo, flag: true, new Mock<IChallengeIssuer>()).InvokeAsync(ctx, _ => Task.CompletedTask);
        ctx.Session.GetInt32(CurrentUserService.SessionKeyUserId).Should().BeNull();
        ctx.Response.Headers.SetCookie.ToString().Should().Contain(WindowsAutoLoginMiddleware.AutoLoginTriedCookie);
    }

    [Fact]
    public async Task Anonymous_NoTriedCookie_Challenges_NoNext()
    {
        var repo = new Mock<IUserRepository>();
        var challenge = new Mock<IChallengeIssuer>();
        var ctx = MakeContext(false, null);
        var called = false;
        await Build(repo, flag: true, challenge).InvokeAsync(ctx, _ => { called = true; return Task.CompletedTask; });
        called.Should().BeFalse();
        challenge.Verify(c => c.ChallengeAsync(ctx), Times.Once);
    }

    [Fact]
    public async Task Anonymous_TriedCookieSet_CallsNext_NoChallenge()
    {
        var repo = new Mock<IUserRepository>();
        var challenge = new Mock<IChallengeIssuer>();
        var ctx = MakeContext(false, null);
        ctx.Request.Headers.Cookie = $"{WindowsAutoLoginMiddleware.AutoLoginTriedCookie}=1";
        var called = false;
        await Build(repo, flag: true, challenge).InvokeAsync(ctx, _ => { called = true; return Task.CompletedTask; });
        called.Should().BeTrue();
        challenge.Verify(c => c.ChallengeAsync(It.IsAny<HttpContext>()), Times.Never);
    }

    [Fact]
    public async Task NoAutoLoginCookie_CallsNext_NoChallenge()
    {
        var repo = new Mock<IUserRepository>();
        var challenge = new Mock<IChallengeIssuer>();
        var ctx = MakeContext(false, null);
        ctx.Request.Headers.Cookie = $"{WindowsAutoLoginMiddleware.NoAutoLoginCookie}=1";
        var called = false;
        await Build(repo, flag: true, challenge).InvokeAsync(ctx, _ => { called = true; return Task.CompletedTask; });
        called.Should().BeTrue();
        challenge.Verify(c => c.ChallengeAsync(It.IsAny<HttpContext>()), Times.Never);
    }
}
```

Plus ein kleiner `FakeSession`-Helper (falls nicht vorhanden) in `IdealAkeWms.Tests/Helpers/FakeSession.cs` — minimaler `ISession` mit Dictionary-Backing. (Suche zuerst, ob ein Session-Fake existiert; sonst anlegen.)

- [ ] **Step 2: Tests rot**

Run: `dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter WindowsAutoLoginMiddlewareTests -v q` → FAIL (Typen fehlen).

- [ ] **Step 3: Challenge-Abstraktion (testbar)**

`IdealAkeWms/Middleware/IChallengeIssuer.cs`:

```csharp
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;

namespace IdealAkeWms.Middleware;

/// <summary>Kapselt die IIS-Negotiate-Challenge (für Testbarkeit der Middleware).</summary>
public interface IChallengeIssuer
{
    Task ChallengeAsync(HttpContext context);
}

public class IISChallengeIssuer : IChallengeIssuer
{
    public Task ChallengeAsync(HttpContext context)
        => context.ChallengeAsync(IISServerDefaults.AuthenticationScheme);
}
```

- [ ] **Step 4: Middleware implementieren**

`IdealAkeWms/Middleware/WindowsAutoLoginMiddleware.cs`:

```csharp
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Services;

namespace IdealAkeWms.Middleware;

public class WindowsAutoLoginMiddleware : IMiddleware
{
    public const string AutoLoginTriedCookie = "IdealAkeWms.AutoLoginTried";
    public const string NoAutoLoginCookie = "IdealAkeWms.NoAutoLogin";

    private readonly IAppSettingRepository _appSettings;
    private readonly IUserRepository _userRepository;
    private readonly IChallengeIssuer _challenge;
    private readonly ILogger<WindowsAutoLoginMiddleware> _logger;

    public WindowsAutoLoginMiddleware(IAppSettingRepository appSettings, IUserRepository userRepository,
        IChallengeIssuer challenge, ILogger<WindowsAutoLoginMiddleware> logger)
    {
        _appSettings = appSettings;
        _userRepository = userRepository;
        _challenge = challenge;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            if (await ShouldTryAsync(context))
            {
                var handled = await TryAutoLoginOrChallengeAsync(context);
                if (handled) return; // Challenge ausgelöst → kein next()
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "WindowsAutoLogin fehlgeschlagen — Fallback Formular");
        }
        await next(context);
    }

    private async Task<bool> ShouldTryAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        if (path.StartsWith("/account/") || path.StartsWith("/api/") || path.StartsWith("/lib/")
            || path.StartsWith("/css/") || path.StartsWith("/js/") || path.StartsWith("/_framework/")
            || path.Contains('.'))
            return false;
        if (context.Session.GetInt32(CurrentUserService.SessionKeyUserId).HasValue)
            return false;
        if (context.Request.Cookies.ContainsKey(NoAutoLoginCookie))
            return false;
        var flag = await _appSettings.GetValueAsync(AppSettingKeys.WindowsAuthAktiv);
        return string.Equals(flag, "true", StringComparison.OrdinalIgnoreCase);
    }

    /// <returns>true = Challenge ausgelöst (Response übernommen).</returns>
    private async Task<bool> TryAutoLoginOrChallengeAsync(HttpContext context)
    {
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var sam = WindowsAccountHelper.ExtractSam(context.User.Identity.Name);
            var user = sam == null ? null : await _userRepository.GetActiveByWindowsUserNameAsync(sam);
            if (user != null)
            {
                context.Session.SetInt32(CurrentUserService.SessionKeyUserId, user.Id);
                context.Session.SetString(CurrentUserService.SessionKeyUserName, user.Name);
                context.Response.Cookies.Delete(AutoLoginTriedCookie);
                context.Response.Cookies.Delete(NoAutoLoginCookie);
                return false;
            }
            // kein Treffer → einmal markieren, Formular
            context.Response.Cookies.Append(AutoLoginTriedCookie, "1", new CookieOptions { HttpOnly = true, IsEssential = true });
            return false;
        }

        // anonym → einmalig challengen
        if (!context.Request.Cookies.ContainsKey(AutoLoginTriedCookie))
        {
            context.Response.Cookies.Append(AutoLoginTriedCookie, "1", new CookieOptions { HttpOnly = true, IsEssential = true });
            await _challenge.ChallengeAsync(context);
            return true;
        }
        return false; // bereits versucht → Formular
    }
}
```

- [ ] **Step 5: Tests grün**

Run: `dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter WindowsAutoLoginMiddlewareTests -v q` → PASS (6).

- [ ] **Step 6: Program.cs verdrahten**

(a) Auth-Registrierung ersetzen:

```csharp
// vorher:
// builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme).AddNegotiate();
// nachher:
builder.Services.AddAuthentication(IISServerDefaults.AuthenticationScheme);
```

`using Microsoft.AspNetCore.Authentication.Negotiate;` entfernen. (`IISServerDefaults` ist über die impliziten Web-Usings/`Microsoft.AspNetCore.Builder` verfügbar; falls nicht: `using Microsoft.AspNetCore.Builder;`.)

(b) DI für Middleware + Challenge:

```csharp
builder.Services.AddScoped<WindowsAutoLoginMiddleware>();
builder.Services.AddScoped<IChallengeIssuer, IISChallengeIssuer>();
```

(c) Middleware einhängen — **nach** `app.UseSession();` und **vor** der inline LoginRedirect-Middleware:

```csharp
app.UseMiddleware<WindowsAutoLoginMiddleware>();
```

(d) PackageReference `Microsoft.AspNetCore.Authentication.Negotiate` aus `IdealAkeWms.csproj` entfernen.

- [ ] **Step 7: Build + Tests + Commit**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj -c Debug --nologo -v q` → 0 Fehler.
Run: `dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj -c Debug --nologo -v q` → grün.

```bash
git add -A
git commit -m "feat(auth): WindowsAutoLoginMiddleware + IIS-Auth-Registrierung (Challenge + Fallback)"
```

---

## Task 8: AccountController — NoAutoLogin-Cookie

**Files:**
- Modify: `IdealAkeWms/Controllers/AccountController.cs`

- [ ] **Step 1: Logout setzt NoAutoLogin-Cookie**

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public IActionResult Logout()
{
    HttpContext.Session.Clear();
    Response.Cookies.Append(Middleware.WindowsAutoLoginMiddleware.NoAutoLoginCookie, "1",
        new CookieOptions { HttpOnly = true, IsEssential = true });
    return RedirectToAction(nameof(Login));
}
```

- [ ] **Step 2: Erfolgreicher Login löscht beide Cookies**

In `Login` POST direkt nach dem Setzen der Session (`SetInt32`/`SetString`) ergänzen:

```csharp
    Response.Cookies.Delete(Middleware.WindowsAutoLoginMiddleware.NoAutoLoginCookie);
    Response.Cookies.Delete(Middleware.WindowsAutoLoginMiddleware.AutoLoginTriedCookie);
```

- [ ] **Step 3: Build + Commit**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj -c Debug --nologo -v q` → 0 Fehler.

```bash
git add IdealAkeWms/Controllers/AccountController.cs
git commit -m "feat(auth): Logout setzt NoAutoLogin-Cookie, Login loescht Auto-Login-Cookies"
```

---

## Task 9: UsersController.CreateAdUser + Views + Users/Index/Edit

**Files:**
- Create: `IdealAkeWms/Models/ViewModels/AdUserCreateViewModel.cs`
- Modify: `IdealAkeWms/Controllers/UsersController.cs`
- Create: `IdealAkeWms/Views/Users/CreateAdUser.cshtml`
- Modify: `IdealAkeWms/Views/Users/Index.cshtml` (Button + AD/Lokal-Spalte)
- Modify: `IdealAkeWms/Views/Users/Edit.cshtml` (WindowsUserName anzeigen)
- Modify: `IdealAkeWms/Views/Users/Create.cshtml` (Hinweis optional — keine Pflicht)
- Create: `IdealAkeWms.Tests/Controllers/UsersControllerAdUserTests.cs`

- [ ] **Step 1: ViewModel**

```csharp
using IdealAkeWms.Services;

namespace IdealAkeWms.Models.ViewModels;

public class AdUserCreateViewModel
{
    public List<AdUserCandidate> Candidates { get; set; } = new();
    public bool AdQueryFailed { get; set; }       // Gruppe leer/AD nicht erreichbar
    public List<RoleCheckboxItem> AvailableRoles { get; set; } = new();

    // POST-Felder
    public string SamAccountName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public List<int> SelectedRoleIds { get; set; } = new();
}
```

- [ ] **Step 2: Controller-Actions** (Konstruktor um `IActiveDirectoryService _activeDirectory` erweitern)

```csharp
public async Task<IActionResult> CreateAdUser()
{
    var members = await _activeDirectory.GetAuthorizationGroupMembersAsync();
    var existing = (await _userRepository.GetAllAsync())
        .Where(u => u.WindowsUserName != null)
        .Select(u => u.WindowsUserName!.ToLowerInvariant())
        .ToHashSet();

    var vm = new AdUserCreateViewModel
    {
        Candidates = members.Where(m => !existing.Contains(m.SamAccountName.ToLowerInvariant())).ToList(),
        AdQueryFailed = members.Count == 0
    };
    await PopulateAdRolesAsync(vm, new List<int>());
    return View(vm);
}

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> CreateAdUser(AdUserCreateViewModel vm)
{
    if (string.IsNullOrWhiteSpace(vm.SamAccountName))
        ModelState.AddModelError(nameof(vm.SamAccountName), "Windows-Benutzer ist erforderlich.");

    var sam = vm.SamAccountName?.Trim() ?? "";
    if (!string.IsNullOrEmpty(sam) && await _userRepository.GetActiveByWindowsUserNameAsync(sam) != null)
        ModelState.AddModelError(nameof(vm.SamAccountName), "Für diesen Windows-Benutzer existiert bereits ein Datensatz.");

    if (!ModelState.IsValid)
    {
        var members = await _activeDirectory.GetAuthorizationGroupMembersAsync();
        vm.Candidates = members.ToList();
        await PopulateAdRolesAsync(vm, vm.SelectedRoleIds);
        return View(vm);
    }

    var user = new User
    {
        Name = string.IsNullOrWhiteSpace(vm.DisplayName) ? sam : vm.DisplayName!.Trim(),
        WindowsUserName = sam,
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        CreatedBy = _currentUserService.GetDisplayName(),
        CreatedByWindows = _currentUserService.GetWindowsUserName()
    };
    await _userRepository.AddAsync(user);
    await _roleRepository.SetUserRolesAsync(user.Id, vm.SelectedRoleIds,
        _currentUserService.GetDisplayName(), _currentUserService.GetWindowsUserName());

    TempData["SuccessMessage"] = $"AD-Benutzer '{user.Name}' wurde angelegt.";
    return RedirectToAction(nameof(Index));
}

private async Task PopulateAdRolesAsync(AdUserCreateViewModel vm, List<int> selectedIds)
{
    var roles = await _roleRepository.GetAllOrderedAsync();
    vm.AvailableRoles = roles.Select(r => new RoleCheckboxItem
    {
        Id = r.Id, Name = r.Name, Key = r.Key, IsSelected = selectedIds.Contains(r.Id)
    }).ToList();
}
```

(Falls `IUserRepository.GetAllAsync()` nicht existiert: `GetAllWithRolesAsync()` oder vorhandene „alle"-Methode nutzen.)

- [ ] **Step 3: View `CreateAdUser.cshtml`** (Muster aus `Create.cshtml`)

```html
@model IdealAkeWms.Models.ViewModels.AdUserCreateViewModel
@{ ViewData["Title"] = "AD-Benutzer anlegen"; }

<h2 class="page-header">AD-Benutzer anlegen</h2>

@if (Model.AdQueryFailed)
{
    <div class="alert alert-warning">Keine Mitglieder gefunden oder AD nicht erreichbar.
        Berechtigungsgruppe in den Einstellungen prüfen, oder Benutzer
        <a asp-action="Create">manuell anlegen</a>.</div>
}

<div class="row"><div class="col-md-8"><div class="card"><div class="card-body">
<form asp-action="CreateAdUser" method="post">
    <div asp-validation-summary="ModelOnly" class="text-danger mb-3"></div>
    <div class="mb-3">
        <label class="form-label">AD-Benutzer (Berechtigungsgruppe)</label>
        <select name="SamAccountName" id="adSelect" class="form-select" asp-items="@(new SelectList(Model.Candidates, nameof(IdealAkeWms.Services.AdUserCandidate.SamAccountName), nameof(IdealAkeWms.Services.AdUserCandidate.DisplayName)))">
            <option value="">— bitte wählen —</option>
        </select>
        <input type="hidden" name="DisplayName" id="adDisplay" />
        <span asp-validation-for="SamAccountName" class="text-danger"></span>
    </div>
    <hr /><h6 class="text-muted">Rollen</h6>
    @for (int i = 0; i < Model.AvailableRoles.Count; i++)
    {
        <div class="mb-2 form-check">
            <input type="checkbox" name="SelectedRoleIds" value="@Model.AvailableRoles[i].Id"
                   id="role_@Model.AvailableRoles[i].Id" class="form-check-input"
                   @(Model.AvailableRoles[i].IsSelected ? "checked" : "") />
            <label for="role_@Model.AvailableRoles[i].Id" class="form-check-label">@Model.AvailableRoles[i].Name</label>
        </div>
    }
    <div class="mt-3">
        <button type="submit" class="btn btn-primary">Anlegen</button>
        <a asp-action="Index" class="btn btn-outline-secondary">Abbrechen</a>
    </div>
</form>
</div></div></div></div>

@section Scripts {
<script>
  // DisplayName synchron zum gewählten Kandidaten setzen (Anzeigename = Option-Text)
  const sel = document.getElementById('adSelect');
  sel?.addEventListener('change', () => {
    document.getElementById('adDisplay').value = sel.options[sel.selectedIndex]?.text || '';
  });
</script>
}
```

- [ ] **Step 4: Users/Index — Button + AD/Lokal-Spalte**

- Im Kopf-`<div class="d-flex gap-2">` nach dem „Neuen Benutzer anlegen"-Link ergänzen (innerhalb `@if (canEdit)`):

```html
            <a asp-action="CreateAdUser" class="btn btn-outline-primary">AD-Benutzer anlegen</a>
```

- Tabellenkopf: nach `<th data-filterable data-col-key="name">Name</th>` neue Spalte:

```html
                <th data-filterable data-col-key="auth-type">Typ</th>
```

- In der Datenzeile die entsprechende Zelle (Badge je nach `WindowsUserName`):

```html
                <td>@(user.WindowsUserName != null ? "AD" : "Lokal")</td>
```

(Server-Column-Filter-Mapping `auth-type` im `UsersController.Index`-ColumnMap ergänzen: Getter `u => u.WindowsUserName != null ? "AD" : "Lokal"`. Falls die Users-Liste server-seitig gemappt ist — die ColumnMap-Stelle im Controller suchen und Eintrag ergänzen.)

- [ ] **Step 5: Users/Edit — WindowsUserName anzeigen**

Im Edit-View ein read-only Feld ergänzen (nur wenn gesetzt), z. B. nach Name:

```html
@if (!string.IsNullOrEmpty(Model.WindowsUserName))
{
    <div class="mb-3">
        <label class="form-label">Windows-Benutzer</label>
        <input class="form-control" value="@Model.WindowsUserName" readonly />
    </div>
}
```

(`UserEditViewModel.WindowsUserName` ergänzen + im Edit-GET-Mapping setzen.)

- [ ] **Step 6: Controller-Test**

```csharp
public class UsersControllerAdUserTests
{
    [Fact]
    public async Task CreateAdUser_Get_FiltersAlreadyImported()
    {
        // Fake-AD liefert 2 Kandidaten; einer ist bereits als User mit WindowsUserName vorhanden.
        // Erwartet: vm.Candidates enthält nur den nicht-importierten.
        // (Controller mit InMemory-Context + Mock<IActiveDirectoryService> aufbauen.)
    }

    [Fact]
    public async Task CreateAdUser_Post_CreatesUserWithRoles()
    {
        // POST mit SamAccountName + 1 Rolle → User mit WindowsUserName + UserRole angelegt.
    }
}
```

Die beiden Tests konkret nach Muster bestehender `UsersController`-Tests ausschreiben (Mock<IActiveDirectoryService>, InMemory-Context, echte Repos). Mindestens: GET filtert importierte raus; POST legt User mit `WindowsUserName` + Rolle an; POST mit Duplikat → ModelState-Fehler, kein zweiter User.

- [ ] **Step 7: Build + Tests + Commit**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj -c Debug --nologo -v q` → 0 Fehler.
Run: `dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj -c Debug --nologo -v q` → grün.

```bash
git add -A
git commit -m "feat(auth): AD-User anlegen (Picker via Berechtigungsgruppe) + Users-Index AD/Lokal"
```

---

## Task 10: Version + Doku

**Files:**
- Modify: `IdealAkeWms/AppVersion.cs`, `IDEALAKEWMSService/AppVersion.cs` (1.22.0 → 1.23.0, Date 2026-06-18)
- Modify: `IdealAkeWms/Views/Help/Changelog.cshtml` (neue v1.23.0-Karte)
- Modify: `IdealAkeWms/Views/Help/*` Hilfeseite (Windows-Auth + AD-User erklären)
- Modify: `CLAUDE.md` (Auth-Abschnitt: Negotiate→IIS, WindowsUserName, AD-User-Anlage, AdGroup entfernt; Rollenkonzept-Tabelle AdGroup-Erwähnung anpassen; AppSettings-Tabelle + ServiceSettings ergänzen)
- Modify: `PROJECT_STATUS.md`
- Modify: `docs/TESTSZENARIEN.md` (neues Kapitel 40)

- [ ] **Step 1: Version-Bump** beide `AppVersion.cs` auf `1.23.0` / `2026-06-18`.

- [ ] **Step 2: Changelog-Karte v1.23.0** mit: Windows-SSO Auto-Login (hinter `WindowsAuthAktiv`, Formular-Fallback), AD-User anlegen via Berechtigungsgruppe, `Role.AdGroup`-Automatik entfernt (Rollen nur noch per Benutzer), Migration 73, Deploy-Hinweis (IIS beide Auth-Modi, App+DB zusammen).

- [ ] **Step 3: CLAUDE.md** Auth-Abschnitt aktualisieren (siehe Spec §1.1/§5/§6/§7): Hosting = IIS in-process + `IISServerDefaults`; `User.WindowsUserName`; AD-User-Anlage; `Role.AdGroup` entfernt; AppSettings `WindowsAuthAktiv`/`WindowsAuthBerechtigungsgruppe`; `Security:AdDomain`. Fallstrick dokumentieren: echte AD-Impl/Negotiate-Challenge nur im IIS-Zielsystem testbar.

- [ ] **Step 4: TESTSZENARIEN Kapitel 40** — Szenarien: (40.1) Domänen-User mit Datensatz → Auto-Login ohne Formular; (40.2) Domänen-User ohne Datensatz → Formular-Fallback; (40.3) Logout → Formular (kein sofortiges Re-Login); (40.4) AD-User anlegen über Picker + Rollen; (40.5) `WindowsAuthAktiv=false` → Verhalten wie bisher; (40.6) Rollen kommen nur noch aus Benutzer-Zuordnung (AdGroup entfernt). Jeweils Vorbedingungen/Schritte/Erwartet.

- [ ] **Step 5: PROJECT_STATUS** kurz fortschreiben.

- [ ] **Step 6: Build + Commit**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj -c Debug --nologo -v q` → 0 Fehler.

```bash
git add -A
git commit -m "docs(auth): v1.23.0 Changelog/Hilfe/CLAUDE.md/TESTSZENARIEN + Version-Bump"
```

---

## Task 11: Final-Check + Review

**Files:** keine.

- [ ] **Step 1: Pending-Migrations-Check**

Run: `dotnet ef migrations has-pending-model-changes --project IdealAkeWms/IdealAkeWms.csproj` → „No changes".

- [ ] **Step 2: Voller Build + alle Tests**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj -c Debug --nologo -v q` → 0 Fehler.
Run: `dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj -c Debug --nologo -v q` → grün (Baseline + neue Tests).
Run: `dotnet build IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Debug --nologo -v q` → 0 Fehler (Version-Bump).

- [ ] **Step 3: Grep-Sanity**

Run: `grep -rni "AdGroup\|AddNegotiate\|GetAdGroupRolesAsync\|GetRolesWithAdGroupAsync" IdealAkeWms/ --include=*.cs --include=*.cshtml` → nur noch in `Migrations/` (historisch) erlaubt, sonst leer.

- [ ] **Step 4: Code-Review** (Skill `superpowers:requesting-code-review` bzw. `code-review`) über den gesamten Branch-Diff. Findings beheben.

- [ ] **Step 5: PAUSE** — User-Test + Merge-Entscheidung. **NICHT autonom mergen.** Deploy ist konfig-kritisch (IIS-Auth-Modi, `WindowsAuthAktiv` erst nach AD-User-Anlage einschalten, App-Pool darf AD lesen).

---

## Self-Review-Notiz (Plan-Autor)

- Spec-Abdeckung: Auto-Login (T7), Fallback+Challenge (T7), AD-Picker (T6+T9), WindowsUserName (T2+T4), AdGroup-Entfernung (T5), Logout-Marker (T8), Config (T1), IIS-Auth-Korrektur (T7 Step 6), Migration/FreshInstall (T2), Doku/Version (T10). ✔
- Reihenfolge-Falle: `Role.AdGroup` muss aus dem Modell raus, bevor die Migration generiert wird → **Task 5 läuft vor Task 2 Step 4**. Im Subagent-Ablauf: Task 1 → Task 5 → Task 2 → Task 3 → Task 4 → Task 6 → Task 7 → Task 8 → Task 9 → Task 10 → Task 11.
- Nicht InMemory-testbar (im Zielsystem verifizieren): echte `ActiveDirectoryService`-LDAP-Abfrage + IIS-Negotiate-Challenge-Handshake. Middleware-Entscheidungslogik ist über `IChallengeIssuer` voll unit-getestet.
