# Customizable View Preferences — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Allow users to customize table views (hide/show columns, resize, reorder, default sort) with per-user persistence in the database.

**Architecture:** New `UserViewPreference` entity stores JSON settings per user per view. A new generic JS module (`column-preferences.js`) reads settings via API and applies them to any table with `data-view-key`. Prerequisite: refactor `table-filter.js` from numeric `data-col` indices to string-based `data-col-key` identifiers.

**Tech Stack:** ASP.NET Core 10, EF Core 10, SQL Server, Vanilla JS, Bootstrap 5 Offcanvas

**Spec:** `docs/superpowers/specs/2026-04-10-customizable-view-preferences-design.md`

---

## File Structure

### New Files
- `IdealAkeWms/Models/UserViewPreference.cs` — Entity
- `IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs` — Static column configs per view
- `IdealAkeWms/Data/Repositories/IUserViewPreferenceRepository.cs` — Interface
- `IdealAkeWms/Data/Repositories/UserViewPreferenceRepository.cs` — Implementation
- `IdealAkeWms/Controllers/Api/UserViewPreferencesApiController.cs` — REST API
- `IdealAkeWms/wwwroot/js/column-preferences.js` — Client-side module
- `SQL/41_AddUserViewPreferences.sql` — Migration script
- `IdealAkeWms.Tests/UserViewPreferenceRepositoryTests.cs` — Repository tests
- `IdealAkeWms.Tests/UserViewPreferencesApiControllerTests.cs` — API tests

### Modified Files
- `IdealAkeWms/Data/ApplicationDbContext.cs` — Add DbSet + OnModelCreating
- `IdealAkeWms/Program.cs` — DI registration
- `IdealAkeWms/wwwroot/js/table-filter.js` — Refactor to string-based keys
- `IdealAkeWms/wwwroot/css/site.css` — Resize handles, context menu, offcanvas styles
- `IdealAkeWms/Views/ProductionOrders/Index.cshtml` — Add data-col-key, column-config, gear icon
- `IdealAkeWms/Views/Picking/Index.cshtml` — Same
- `IdealAkeWms/Views/Tracking/OseonIndex.cshtml` — Same (limited: no reorder)
- `IdealAkeWms/Views/Picking/Bom.cshtml` — Same (limited: no reorder)
- `IdealAkeWms/Controllers/UsersController.cs` — Admin reset actions
- `IdealAkeWms/Views/Users/Edit.cshtml` — Reset buttons UI
- `SQL/00_FreshInstall.sql` — Add UserViewPreferences table

---

## Task 1: Entity + DbContext + Migration

**Files:**
- Create: `IdealAkeWms/Models/UserViewPreference.cs`
- Modify: `IdealAkeWms/Data/ApplicationDbContext.cs:41-43` (add DbSet)
- Modify: `IdealAkeWms/Data/ApplicationDbContext.cs:632+` (add OnModelCreating)
- Create: `SQL/41_AddUserViewPreferences.sql`

- [ ] **Step 1: Create the UserViewPreference entity**

```csharp
// IdealAkeWms/Models/UserViewPreference.cs
namespace IdealAkeWms.Models;

public class UserViewPreference : AuditableEntity
{
    public int UserId { get; set; }
    public string ViewKey { get; set; } = string.Empty;
    public string SettingsJson { get; set; } = "{}";

    public User User { get; set; } = null!;
}
```

- [ ] **Step 2: Add DbSet to ApplicationDbContext**

In `ApplicationDbContext.cs`, after line 42 (`public DbSet<CachedBomItem> CachedBomItems => Set<CachedBomItem>();`), add:

```csharp
public DbSet<UserViewPreference> UserViewPreferences => Set<UserViewPreference>();
```

- [ ] **Step 3: Add OnModelCreating configuration**

In `ApplicationDbContext.cs`, after the `CachedBomItem` configuration block (after line 662, before the closing `}`), add:

```csharp
        // UserViewPreference
        modelBuilder.Entity<UserViewPreference>(entity =>
        {
            entity.ToTable("UserViewPreferences");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ViewKey).HasMaxLength(50).IsRequired();
            entity.Property(e => e.SettingsJson).IsRequired();
            entity.Property(e => e.CreatedBy).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CreatedByWindows).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ModifiedBy).HasMaxLength(200);
            entity.Property(e => e.ModifiedByWindows).HasMaxLength(200);

            entity.HasIndex(e => new { e.UserId, e.ViewKey }).IsUnique()
                .HasDatabaseName("UQ_UserViewPreferences_User_View");

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
```

- [ ] **Step 4: Create SQL migration script**

```sql
-- SQL/41_AddUserViewPreferences.sql
IF OBJECT_ID(N'dbo.UserViewPreferences', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[UserViewPreferences] (
        [Id]                INT IDENTITY(1,1) NOT NULL,
        [UserId]            INT NOT NULL,
        [ViewKey]           NVARCHAR(50) NOT NULL,
        [SettingsJson]      NVARCHAR(MAX) NOT NULL,
        [CreatedAt]         DATETIME2 NOT NULL,
        [CreatedBy]         NVARCHAR(200) NOT NULL,
        [CreatedByWindows]  NVARCHAR(200) NOT NULL,
        [ModifiedAt]        DATETIME2 NULL,
        [ModifiedBy]        NVARCHAR(200) NULL,
        [ModifiedByWindows] NVARCHAR(200) NULL,
        CONSTRAINT [PK_UserViewPreferences] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserViewPreferences_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE,
        CONSTRAINT [UQ_UserViewPreferences_User_View] UNIQUE ([UserId], [ViewKey])
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260410000000_AddUserViewPreferences')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260410000000_AddUserViewPreferences', N'10.0.0');
END
GO
```

- [ ] **Step 5: Create EF migration**

Run: `cd IdealAkeWms && dotnet ef migrations add AddUserViewPreferences`
Expected: Migration files created in `Migrations/`

- [ ] **Step 6: Update 00_FreshInstall.sql**

Add the `UserViewPreferences` CREATE TABLE block to `SQL/00_FreshInstall.sql` in the appropriate section (after CachedBomItems).

- [ ] **Step 7: Verify build**

Run: `cd IdealAkeWms && dotnet build`
Expected: Build succeeded

- [ ] **Step 8: Commit**

```bash
git add IdealAkeWms/Models/UserViewPreference.cs IdealAkeWms/Data/ApplicationDbContext.cs SQL/41_AddUserViewPreferences.sql SQL/00_FreshInstall.sql IdealAkeWms/Migrations/
git commit -m "feat: add UserViewPreference entity + migration (view preferences)"
```

---

## Task 2: Repository

**Files:**
- Create: `IdealAkeWms/Data/Repositories/IUserViewPreferenceRepository.cs`
- Create: `IdealAkeWms/Data/Repositories/UserViewPreferenceRepository.cs`
- Modify: `IdealAkeWms/Program.cs:64` (add DI registration)
- Create: `IdealAkeWms.Tests/UserViewPreferenceRepositoryTests.cs`

- [ ] **Step 1: Write the repository tests**

```csharp
// IdealAkeWms.Tests/UserViewPreferenceRepositoryTests.cs
using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;

namespace IdealAkeWms.Tests;

public class UserViewPreferenceRepositoryTests
{
    private ApplicationDbContext CreateContext()
    {
        return TestDbContextFactory.CreateContext();
    }

    private User CreateTestUser(ApplicationDbContext ctx)
    {
        var user = new User
        {
            Name = "TestUser",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
            CreatedByWindows = "test"
        };
        ctx.Users.Add(user);
        ctx.SaveChanges();
        return user;
    }

    [Fact]
    public async Task GetByUserAndViewAsync_ReturnsNull_WhenNoPreference()
    {
        using var ctx = CreateContext();
        var user = CreateTestUser(ctx);
        var repo = new UserViewPreferenceRepository(ctx);

        var result = await repo.GetByUserAndViewAsync(user.Id, "ProductionOrders");

        result.Should().BeNull();
    }

    [Fact]
    public async Task SaveAsync_CreatesNew_WhenNoExistingPreference()
    {
        using var ctx = CreateContext();
        var user = CreateTestUser(ctx);
        var repo = new UserViewPreferenceRepository(ctx);
        var json = """{"columns":[{"key":"OrderNumber","visible":true}]}""";

        await repo.SaveAsync(user.Id, "ProductionOrders", json, "test", "test\\user");

        var result = await repo.GetByUserAndViewAsync(user.Id, "ProductionOrders");
        result.Should().NotBeNull();
        result!.SettingsJson.Should().Be(json);
    }

    [Fact]
    public async Task SaveAsync_UpdatesExisting_WhenPreferenceExists()
    {
        using var ctx = CreateContext();
        var user = CreateTestUser(ctx);
        var repo = new UserViewPreferenceRepository(ctx);
        await repo.SaveAsync(user.Id, "ProductionOrders", "{}", "test", "test\\user");

        var newJson = """{"columns":[{"key":"OrderNumber","visible":false}]}""";
        await repo.SaveAsync(user.Id, "ProductionOrders", newJson, "test2", "test2\\user");

        var result = await repo.GetByUserAndViewAsync(user.Id, "ProductionOrders");
        result!.SettingsJson.Should().Be(newJson);
        result.ModifiedBy.Should().Be("test2");
    }

    [Fact]
    public async Task DeleteByUserAndViewAsync_RemovesPreference()
    {
        using var ctx = CreateContext();
        var user = CreateTestUser(ctx);
        var repo = new UserViewPreferenceRepository(ctx);
        await repo.SaveAsync(user.Id, "ProductionOrders", "{}", "test", "test\\user");

        await repo.DeleteByUserAndViewAsync(user.Id, "ProductionOrders");

        var result = await repo.GetByUserAndViewAsync(user.Id, "ProductionOrders");
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAllByUserAsync_RemovesAllPreferences()
    {
        using var ctx = CreateContext();
        var user = CreateTestUser(ctx);
        var repo = new UserViewPreferenceRepository(ctx);
        await repo.SaveAsync(user.Id, "ProductionOrders", "{}", "test", "test\\user");
        await repo.SaveAsync(user.Id, "Picking", "{}", "test", "test\\user");

        await repo.DeleteAllByUserAsync(user.Id);

        var all = await repo.GetAllByUserAsync(user.Id);
        all.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllByUserAsync_ReturnsAllPreferencesForUser()
    {
        using var ctx = CreateContext();
        var user = CreateTestUser(ctx);
        var repo = new UserViewPreferenceRepository(ctx);
        await repo.SaveAsync(user.Id, "ProductionOrders", "{}", "test", "test\\user");
        await repo.SaveAsync(user.Id, "Picking", "{}", "test", "test\\user");

        var all = await repo.GetAllByUserAsync(user.Id);

        all.Should().HaveCount(2);
        all.Select(p => p.ViewKey).Should().Contain("ProductionOrders");
        all.Select(p => p.ViewKey).Should().Contain("Picking");
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd IdealAkeWms.Tests && dotnet test --filter "FullyQualifiedName~UserViewPreferenceRepositoryTests" -v n`
Expected: FAIL — `IUserViewPreferenceRepository` and `UserViewPreferenceRepository` do not exist yet

- [ ] **Step 3: Create the interface**

```csharp
// IdealAkeWms/Data/Repositories/IUserViewPreferenceRepository.cs
using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public interface IUserViewPreferenceRepository
{
    Task<UserViewPreference?> GetByUserAndViewAsync(int userId, string viewKey);
    Task SaveAsync(int userId, string viewKey, string settingsJson, string modifiedBy, string modifiedByWindows);
    Task DeleteByUserAndViewAsync(int userId, string viewKey);
    Task DeleteAllByUserAsync(int userId);
    Task<List<UserViewPreference>> GetAllByUserAsync(int userId);
}
```

- [ ] **Step 4: Create the repository implementation**

```csharp
// IdealAkeWms/Data/Repositories/UserViewPreferenceRepository.cs
using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class UserViewPreferenceRepository : IUserViewPreferenceRepository
{
    private readonly ApplicationDbContext _context;

    public UserViewPreferenceRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserViewPreference?> GetByUserAndViewAsync(int userId, string viewKey)
    {
        return await _context.UserViewPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId && p.ViewKey == viewKey);
    }

    public async Task SaveAsync(int userId, string viewKey, string settingsJson, string modifiedBy, string modifiedByWindows)
    {
        var existing = await GetByUserAndViewAsync(userId, viewKey);
        if (existing != null)
        {
            existing.SettingsJson = settingsJson;
            existing.ModifiedAt = DateTime.UtcNow;
            existing.ModifiedBy = modifiedBy;
            existing.ModifiedByWindows = modifiedByWindows;
        }
        else
        {
            _context.UserViewPreferences.Add(new UserViewPreference
            {
                UserId = userId,
                ViewKey = viewKey,
                SettingsJson = settingsJson,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = modifiedBy,
                CreatedByWindows = modifiedByWindows
            });
        }
        await _context.SaveChangesAsync();
    }

    public async Task DeleteByUserAndViewAsync(int userId, string viewKey)
    {
        var existing = await GetByUserAndViewAsync(userId, viewKey);
        if (existing != null)
        {
            _context.UserViewPreferences.Remove(existing);
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteAllByUserAsync(int userId)
    {
        var prefs = await _context.UserViewPreferences
            .Where(p => p.UserId == userId)
            .ToListAsync();
        _context.UserViewPreferences.RemoveRange(prefs);
        await _context.SaveChangesAsync();
    }

    public async Task<List<UserViewPreference>> GetAllByUserAsync(int userId)
    {
        return await _context.UserViewPreferences
            .Where(p => p.UserId == userId)
            .OrderBy(p => p.ViewKey)
            .ToListAsync();
    }
}
```

- [ ] **Step 5: Register in DI**

In `Program.cs`, after line 64 (`builder.Services.AddScoped<IArticleAttributeRepository, ArticleAttributeRepository>();`), add:

```csharp
builder.Services.AddScoped<IUserViewPreferenceRepository, UserViewPreferenceRepository>();
```

- [ ] **Step 6: Run tests to verify they pass**

Run: `cd IdealAkeWms.Tests && dotnet test --filter "FullyQualifiedName~UserViewPreferenceRepositoryTests" -v n`
Expected: All 6 tests PASS

- [ ] **Step 7: Commit**

```bash
git add IdealAkeWms/Data/Repositories/IUserViewPreferenceRepository.cs IdealAkeWms/Data/Repositories/UserViewPreferenceRepository.cs IdealAkeWms/Program.cs IdealAkeWms.Tests/UserViewPreferenceRepositoryTests.cs
git commit -m "feat: add UserViewPreference repository with tests"
```

---

## Task 3: API Controller

**Files:**
- Create: `IdealAkeWms/Controllers/Api/UserViewPreferencesApiController.cs`
- Create: `IdealAkeWms.Tests/UserViewPreferencesApiControllerTests.cs`

- [ ] **Step 1: Write the API controller tests**

```csharp
// IdealAkeWms.Tests/UserViewPreferencesApiControllerTests.cs
using FluentAssertions;
using IdealAkeWms.Controllers.Api;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace IdealAkeWms.Tests;

public class UserViewPreferencesApiControllerTests
{
    private readonly Mock<IUserViewPreferenceRepository> _repoMock = new();
    private readonly Mock<ICurrentUserService> _userServiceMock = new();

    private UserViewPreferencesApiController CreateController()
    {
        _userServiceMock.Setup(s => s.GetCurrentAppUserId()).Returns(42);
        _userServiceMock.Setup(s => s.IsLoggedIn()).Returns(true);
        _userServiceMock.Setup(s => s.GetDisplayName()).Returns("TestUser");
        _userServiceMock.Setup(s => s.GetWindowsUserName()).Returns("DOMAIN\\testuser");
        return new UserViewPreferencesApiController(_repoMock.Object, _userServiceMock.Object);
    }

    [Fact]
    public async Task Get_Returns204_WhenNoPreference()
    {
        var controller = CreateController();
        _repoMock.Setup(r => r.GetByUserAndViewAsync(42, "ProductionOrders"))
            .ReturnsAsync((UserViewPreference?)null);

        var result = await controller.Get("ProductionOrders");

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Get_Returns200WithJson_WhenPreferenceExists()
    {
        var controller = CreateController();
        var pref = new UserViewPreference { SettingsJson = """{"columns":[]}""" };
        _repoMock.Setup(r => r.GetByUserAndViewAsync(42, "ProductionOrders"))
            .ReturnsAsync(pref);

        var result = await controller.Get("ProductionOrders");

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be("""{"columns":[]}""");
    }

    [Fact]
    public async Task Get_ReturnsBadRequest_ForInvalidViewKey()
    {
        var controller = CreateController();

        var result = await controller.Get("InvalidView");

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Put_CallsSave_WithCorrectParams()
    {
        var controller = CreateController();
        var json = """{"columns":[{"key":"OrderNumber","visible":true}]}""";

        var result = await controller.Put("ProductionOrders", json);

        result.Should().BeOfType<OkResult>();
        _repoMock.Verify(r => r.SaveAsync(42, "ProductionOrders", json, "TestUser", "DOMAIN\\testuser"), Times.Once);
    }

    [Fact]
    public async Task Delete_CallsDeleteByUserAndView()
    {
        var controller = CreateController();

        var result = await controller.Delete("ProductionOrders");

        result.Should().BeOfType<OkResult>();
        _repoMock.Verify(r => r.DeleteByUserAndViewAsync(42, "ProductionOrders"), Times.Once);
    }

    [Fact]
    public async Task Get_ReturnsUnauthorized_WhenNotLoggedIn()
    {
        _userServiceMock.Setup(s => s.IsLoggedIn()).Returns(false);
        _userServiceMock.Setup(s => s.GetCurrentAppUserId()).Returns((int?)null);
        var controller = new UserViewPreferencesApiController(_repoMock.Object, _userServiceMock.Object);

        var result = await controller.Get("ProductionOrders");

        result.Should().BeOfType<UnauthorizedResult>();
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd IdealAkeWms.Tests && dotnet test --filter "FullyQualifiedName~UserViewPreferencesApiControllerTests" -v n`
Expected: FAIL — controller does not exist

- [ ] **Step 3: Create the API controller**

```csharp
// IdealAkeWms/Controllers/Api/UserViewPreferencesApiController.cs
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers.Api;

[ApiController]
[Route("api/user-view-preferences")]
public class UserViewPreferencesApiController : ControllerBase
{
    private static readonly HashSet<string> AllowedViewKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "ProductionOrders", "Picking", "OseonTracking", "Bom"
    };

    private readonly IUserViewPreferenceRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public UserViewPreferencesApiController(
        IUserViewPreferenceRepository repository,
        ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    [HttpGet("{viewKey}")]
    public async Task<IActionResult> Get(string viewKey)
    {
        var userId = _currentUserService.GetCurrentAppUserId();
        if (!_currentUserService.IsLoggedIn() || userId == null)
            return Unauthorized();

        if (!AllowedViewKeys.Contains(viewKey))
            return BadRequest($"Invalid view key: {viewKey}");

        var pref = await _repository.GetByUserAndViewAsync(userId.Value, viewKey);
        if (pref == null)
            return NoContent();

        return Ok(pref.SettingsJson);
    }

    [HttpPut("{viewKey}")]
    public async Task<IActionResult> Put(string viewKey, [FromBody] string settingsJson)
    {
        var userId = _currentUserService.GetCurrentAppUserId();
        if (!_currentUserService.IsLoggedIn() || userId == null)
            return Unauthorized();

        if (!AllowedViewKeys.Contains(viewKey))
            return BadRequest($"Invalid view key: {viewKey}");

        if (settingsJson != null && settingsJson.Length > 65536)
            return BadRequest("Settings too large (max 64KB)");

        await _repository.SaveAsync(
            userId.Value,
            viewKey,
            settingsJson ?? "{}",
            _currentUserService.GetDisplayName(),
            _currentUserService.GetWindowsUserName());

        return Ok();
    }

    [HttpDelete("{viewKey}")]
    public async Task<IActionResult> Delete(string viewKey)
    {
        var userId = _currentUserService.GetCurrentAppUserId();
        if (!_currentUserService.IsLoggedIn() || userId == null)
            return Unauthorized();

        if (!AllowedViewKeys.Contains(viewKey))
            return BadRequest($"Invalid view key: {viewKey}");

        await _repository.DeleteByUserAndViewAsync(userId.Value, viewKey);
        return Ok();
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd IdealAkeWms.Tests && dotnet test --filter "FullyQualifiedName~UserViewPreferencesApiControllerTests" -v n`
Expected: All 6 tests PASS

- [ ] **Step 5: Verify full build**

Run: `cd IdealAkeWms && dotnet build`
Expected: Build succeeded

- [ ] **Step 6: Commit**

```bash
git add IdealAkeWms/Controllers/Api/UserViewPreferencesApiController.cs IdealAkeWms.Tests/UserViewPreferencesApiControllerTests.cs
git commit -m "feat: add UserViewPreferences API controller with tests"
```

---

## Task 4: ColumnDefinitions (Static Config)

**Files:**
- Create: `IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs`

- [ ] **Step 1: Create the ColumnDefinitions class**

This class defines column metadata for each view. The column keys match what will be used as `data-col-key` in the HTML. Conditional columns (rendered only with certain permissions) are included here but marked — the Razor view will only emit them to the column-config JSON when they are actually rendered.

```csharp
// IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs
namespace IdealAkeWms.Models.ViewModels;

public record ColumnDef(string Key, string Label, bool Locked = false, int? DefaultWidth = null);

public record ViewConfig(string ViewKey, bool SupportsReorder, bool SupportsSortDefault)
{
    public List<ColumnDef> Columns { get; init; } = new();
}

public static class ColumnDefinitions
{
    public static readonly ViewConfig ProductionOrders = new("ProductionOrders", supportsReorder: true, supportsSortDefault: true)
    {
        Columns = new()
        {
            // Conditional: BomAction (only when CanPick) — handled in Razor
            new("OrderNumber", "FA Nr.", locked: true, defaultWidth: 90),
            new("Quantity", "Stk.", defaultWidth: 55),
            new("Customer", "Kunde"),
            new("ArticleNumber", "Artikelnummer"),
            new("Description1", "Bezeichnung 1"),
            new("Description2", "Bezeichnung 2"),
            new("Workplace", "Werkbank"),
            new("CoatingDate", "Beschicht.", defaultWidth: 110),
            new("PreCommissionDate", "BG-Termin", defaultWidth: 110),
            new("CommissioningDate", "Komm.", defaultWidth: 110),
            new("ProductionDate", "Fert.-Termin", defaultWidth: 110),
            new("DeliveryDate", "Liefertermin", defaultWidth: 110),
            new("CoatingDone", "Lack-T", defaultWidth: 55),
            new("HasGlass", "Glas", defaultWidth: 45),
            new("HasExternalPurchase", "Zukauf", defaultWidth: 55),
            new("Status", "Status"),
            // Actions column (OSEON link + Done toggle) — not hideable, not in config
            // Conditional: Release (LeitstandAktiv && CanManagePickingRelease) — handled in Razor
            // Conditional: Picker (PickerAssignmentEnabled) — handled in Razor
        }
    };

    public static readonly ViewConfig Picking = new("Picking", supportsReorder: true, supportsSortDefault: true)
    {
        Columns = new()
        {
            new("Priority", "Prio", defaultWidth: 60),
            new("OrderNumber", "FA Nr.", locked: true),
            new("ArticleNumber", "Artikelnummer"),
            new("Description", "Bezeichnung"),
            new("Customer", "Kunde"),
            new("Quantity", "Stk.", defaultWidth: 55),
            new("CommissioningDate", "Komm.-Termin"),
            new("Status", "Status"),
            // Conditional: Picker (PickerAssignmentEnabled) — handled in Razor
        }
    };

    public static readonly ViewConfig OseonTracking = new("OseonTracking", supportsReorder: false, supportsSortDefault: false)
    {
        Columns = new()
        {
            new("TrafficLight", "", locked: true, defaultWidth: 30),
            new("OrderNumber", "Auftrag", locked: true),
            new("ArticleNumber", "Artikelnr."),
            new("Description", "Bezeichnung"),
            new("Workplace", "Werkbank"),
            new("Status", "Status"),
            new("QuantityTargetActual", "Soll / Ist"),
            new("DueDate", "Endtermin"),
        }
    };

    public static readonly ViewConfig Bom = new("Bom", supportsReorder: false, supportsSortDefault: false)
    {
        Columns = new()
        {
            new("Position", "Pos.", locked: true, defaultWidth: 60),
            new("ArticleNumber", "Artikelnummer"),
            new("Description", "Bezeichnung"),
            new("Quantity", "Menge", defaultWidth: 80),
            new("Unit", "Einheit", defaultWidth: 60),
            new("Procurement", "Beschaffung"),
            new("ArticleGroup", "Artikelgruppe"),
            new("Category", "Kategorie"),
            new("Stock", "Bestand", defaultWidth: 80),
            new("StorageLocation", "Lagerplatz"),
        }
    };

    public static ViewConfig? GetByViewKey(string viewKey)
    {
        return viewKey switch
        {
            "ProductionOrders" => ProductionOrders,
            "Picking" => Picking,
            "OseonTracking" => OseonTracking,
            "Bom" => Bom,
            _ => null
        };
    }
}
```

- [ ] **Step 2: Verify build**

Run: `cd IdealAkeWms && dotnet build`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs
git commit -m "feat: add ColumnDefinitions static config for view preferences"
```

---

## Task 5: Refactor table-filter.js to String-Based Keys

**Files:**
- Modify: `IdealAkeWms/wwwroot/js/table-filter.js`

This is the critical prerequisite. All `data-col` numeric references must become `data-col-key` string references. The physical column index is resolved at runtime.

- [ ] **Step 1: Refactor table-filter.js**

Replace the entire content of `wwwroot/js/table-filter.js` with the refactored version. Key changes:
- `data-col` → `data-col-key` everywhere
- `parseInt(th.getAttribute('data-col'))` → `th.getAttribute('data-col-key')`
- Filter inputs get `data-col-key` attribute instead of `data-col`
- `sortTable(col, dir)` resolves column key to physical index at runtime
- `applyFilters()` resolves column keys to physical indices at runtime
- Global functions `getActiveFilters()`, `setColumnFilter()`, `triggerSort()` use string keys

```javascript
// wwwroot/js/table-filter.js
// Client-side table filtering and sorting for tables with .filterable-table class
(function () {
    'use strict';

    var _filterRow = null;
    var _headers = null;
    var _tbody = null;
    var _table = null;

    function getPhysicalIndex(colKey) {
        if (!_table) return -1;
        var allThs = _table.querySelectorAll('thead tr:first-child th');
        for (var i = 0; i < allThs.length; i++) {
            if (allThs[i].getAttribute('data-col-key') === colKey) return i;
        }
        return -1;
    }

    function init() {
        _table = document.querySelector('.filterable-table');
        if (!_table) return;

        var thead = _table.querySelector('thead');
        _tbody = _table.querySelector('tbody');
        _headers = thead.querySelectorAll('th[data-filterable]');

        if (_headers.length === 0) return;

        // Create filter row
        _filterRow = document.createElement('tr');
        _filterRow.className = 'filter-row';
        var allThs = thead.querySelectorAll('tr:first-child th');
        allThs.forEach(function (th) {
            var filterTd = document.createElement('th');
            filterTd.style.padding = '4px';
            filterTd.style.backgroundColor = '#f8f9fa';

            if (th.hasAttribute('data-filterable')) {
                var colKey = th.getAttribute('data-col-key');
                var isDateCol = th.hasAttribute('data-date-filter');

                if (isDateCol) {
                    var wrapper = document.createElement('div');
                    wrapper.style.display = 'flex';
                    wrapper.style.gap = '2px';

                    var input = document.createElement('input');
                    input.type = 'text';
                    input.className = 'form-control form-control-sm';
                    input.style.fontSize = '0.75rem';
                    input.style.flex = '1';
                    input.style.minWidth = '0';
                    input.placeholder = 'Filter...';
                    input.setAttribute('data-col-key', colKey);
                    input.addEventListener('input', applyFilters);

                    var calBtn = document.createElement('button');
                    calBtn.type = 'button';
                    calBtn.className = 'btn btn-sm btn-outline-secondary date-filter-btn';
                    calBtn.title = 'Kalender / KW-Filter';
                    calBtn.innerHTML = '<svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" fill="currentColor" viewBox="0 0 16 16"><path d="M3.5 0a.5.5 0 0 1 .5.5V1h8V.5a.5.5 0 0 1 1 0V1h1a2 2 0 0 1 2 2v11a2 2 0 0 1-2 2H2a2 2 0 0 1-2-2V3a2 2 0 0 1 2-2h1V.5a.5.5 0 0 1 .5-.5M1 4v10a1 1 0 0 0 1 1h12a1 1 0 0 0 1-1V4z"/></svg>';
                    calBtn.addEventListener('click', function (e) {
                        e.stopPropagation();
                        openDatePicker(input, calBtn);
                    });

                    wrapper.appendChild(input);
                    wrapper.appendChild(calBtn);
                    filterTd.appendChild(wrapper);
                } else {
                    var input = document.createElement('input');
                    input.type = 'text';
                    input.className = 'form-control form-control-sm';
                    input.style.fontSize = '0.75rem';
                    input.placeholder = 'Filter...';
                    input.setAttribute('data-col-key', colKey);
                    input.addEventListener('input', applyFilters);
                    filterTd.appendChild(input);
                }
            }

            _filterRow.appendChild(filterTd);
        });
        thead.appendChild(_filterRow);

        // Sorting
        _headers.forEach(function (th) {
            th.style.cursor = 'pointer';
            th.style.userSelect = 'none';
            var span = document.createElement('span');
            span.className = 'sort-indicator ms-1';
            span.style.fontSize = '0.7rem';
            th.appendChild(span);

            th.addEventListener('click', function () {
                var colKey = th.getAttribute('data-col-key');
                var currentDir = th.getAttribute('data-sort-dir');
                var newDir = currentDir === 'asc' ? 'desc' : 'asc';

                _headers.forEach(function (h) {
                    h.removeAttribute('data-sort-dir');
                    h.querySelector('.sort-indicator').textContent = '';
                });

                th.setAttribute('data-sort-dir', newDir);
                th.querySelector('.sort-indicator').textContent = newDir === 'asc' ? '\u25B2' : '\u25BC';

                sortTable(colKey, newDir);
            });
        });
    }

    function matchesFilter(text, val) {
        if (val.startsWith('!')) {
            var excludes = val.substring(1).split(',').map(function (s) { return s.trim(); }).filter(Boolean);
            return excludes.every(function (ex) { return text.indexOf(ex) === -1; });
        }
        var parts = val.split(',').map(function (s) { return s.trim(); }).filter(Boolean);
        return parts.some(function (p) { return text.indexOf(p) !== -1; });
    }

    window.getActiveFilters = function () {
        if (!_filterRow) return {};
        var filters = {};
        _filterRow.querySelectorAll('input').forEach(function (input) {
            var colKey = input.getAttribute('data-col-key');
            var val = input.value.toLowerCase().trim();
            if (val && colKey) filters[colKey] = val;
        });
        return filters;
    };

    function applyFilters() {
        if (!_filterRow || !_tbody) return;
        var filters = window.getActiveFilters();

        var rows = _tbody.querySelectorAll('tr');
        rows.forEach(function (row) {
            if (row.querySelector('td[colspan]')) return;
            var visible = true;

            for (var colKey in filters) {
                var colIndex = getPhysicalIndex(colKey);
                if (colIndex < 0) continue;
                var cell = row.querySelectorAll('td')[colIndex];
                if (cell) {
                    var text = cell.textContent.toLowerCase();
                    if (!matchesFilter(text, filters[colKey])) {
                        visible = false;
                        break;
                    }
                }
            }

            row.style.display = visible ? '' : 'none';
        });
    }

    function sortTable(colKey, dir) {
        if (!_tbody) return;
        var colIndex = getPhysicalIndex(colKey);
        if (colIndex < 0) return;

        var rows = Array.from(_tbody.querySelectorAll('tr'));
        var dataRows = rows.filter(function (r) { return !r.querySelector('td[colspan]'); });

        dataRows.sort(function (a, b) {
            var cellA = a.querySelectorAll('td')[colIndex];
            var cellB = b.querySelectorAll('td')[colIndex];
            if (!cellA || !cellB) return 0;

            var valA = cellA.textContent.trim();
            var valB = cellB.textContent.trim();

            var dateRegex = /^(\d{2})\.(\d{2})\.(\d{4})/;
            var matchA = valA.match(dateRegex);
            var matchB = valB.match(dateRegex);
            if (matchA && matchB) {
                var dateA = new Date(matchA[3], matchA[2] - 1, matchA[1]);
                var dateB = new Date(matchB[3], matchB[2] - 1, matchB[1]);
                return dir === 'asc' ? dateA - dateB : dateB - dateA;
            }
            if (matchA && !matchB) return dir === 'asc' ? -1 : 1;
            if (!matchA && matchB) return dir === 'asc' ? 1 : -1;

            var numA = parseFloat(valA.replace(/\./g, '').replace(',', '.'));
            var numB = parseFloat(valB.replace(/\./g, '').replace(',', '.'));
            if (!isNaN(numA) && !isNaN(numB)) {
                return dir === 'asc' ? numA - numB : numB - numA;
            }

            var cmp = valA.localeCompare(valB, 'de');
            return dir === 'asc' ? cmp : -cmp;
        });

        dataRows.forEach(function (row) {
            _tbody.appendChild(row);
        });
    }

    // ========================================================================
    // Date Picker / KW-Filter Popup
    // ========================================================================

    var _activePopup = null;

    function openDatePicker(input, anchorBtn) {
        closeDatePicker();

        var now = new Date();
        var displayMonth = now.getMonth();
        var displayYear = now.getFullYear();

        var existingMatch = input.value.match(/(\d{2})\.(\d{2})\.(\d{4})/);
        if (existingMatch) {
            displayMonth = parseInt(existingMatch[2]) - 1;
            displayYear = parseInt(existingMatch[3]);
        }

        var popup = document.createElement('div');
        popup.className = 'date-filter-popup';
        document.body.appendChild(popup);
        _activePopup = popup;

        var rect = anchorBtn.getBoundingClientRect();
        popup.style.top = (rect.bottom + window.scrollY + 4) + 'px';
        popup.style.left = Math.max(4, rect.left + window.scrollX - 200) + 'px';

        function render() {
            popup.innerHTML = '';

            var header = document.createElement('div');
            header.className = 'date-filter-header';

            var prevBtn = document.createElement('button');
            prevBtn.type = 'button';
            prevBtn.textContent = '\u25C0';
            prevBtn.addEventListener('click', function (e) {
                e.stopPropagation();
                displayMonth--;
                if (displayMonth < 0) { displayMonth = 11; displayYear--; }
                render();
            });

            var title = document.createElement('span');
            var monthNames = ['Januar', 'Februar', 'M\u00e4rz', 'April', 'Mai', 'Juni',
                'Juli', 'August', 'September', 'Oktober', 'November', 'Dezember'];
            title.textContent = monthNames[displayMonth] + ' ' + displayYear;

            var nextBtn = document.createElement('button');
            nextBtn.type = 'button';
            nextBtn.textContent = '\u25B6';
            nextBtn.addEventListener('click', function (e) {
                e.stopPropagation();
                displayMonth++;
                if (displayMonth > 11) { displayMonth = 0; displayYear++; }
                render();
            });

            header.appendChild(prevBtn);
            header.appendChild(title);
            header.appendChild(nextBtn);
            popup.appendChild(header);

            var grid = document.createElement('table');
            grid.className = 'date-filter-grid';

            var headRow = document.createElement('tr');
            ['KW', 'Mo', 'Di', 'Mi', 'Do', 'Fr', 'Sa', 'So'].forEach(function (label) {
                var th = document.createElement('th');
                th.textContent = label;
                headRow.appendChild(th);
            });
            grid.appendChild(headRow);

            var firstDay = new Date(displayYear, displayMonth, 1);
            var lastDay = new Date(displayYear, displayMonth + 1, 0);
            var startDow = (firstDay.getDay() + 6) % 7;

            var day = 1 - startDow;
            while (day <= lastDay.getDate()) {
                var row = document.createElement('tr');

                var thursdayOfWeek = new Date(displayYear, displayMonth, day + 3);
                if (day + 3 < 1) thursdayOfWeek = new Date(displayYear, displayMonth, 1);
                var kw = getIsoWeek(thursdayOfWeek);
                var kwCell = document.createElement('td');
                kwCell.className = 'date-filter-kw';
                kwCell.textContent = 'KW' + kw;
                kwCell.title = 'Nach KW' + kw + ' filtern';
                kwCell.addEventListener('click', (function (kwVal) {
                    return function (e) {
                        e.stopPropagation();
                        input.value = 'KW' + kwVal;
                        applyFilters();
                        closeDatePicker();
                    };
                })(kw));
                row.appendChild(kwCell);

                for (var d = 0; d < 7; d++) {
                    var cell = document.createElement('td');
                    if (day >= 1 && day <= lastDay.getDate()) {
                        cell.textContent = day;
                        var cellDate = new Date(displayYear, displayMonth, day);
                        var isToday = cellDate.toDateString() === now.toDateString();
                        if (isToday) cell.className = 'date-filter-today';

                        cell.addEventListener('click', (function (dd) {
                            return function (e) {
                                e.stopPropagation();
                                var formatted = pad2(dd.getDate()) + '.' + pad2(dd.getMonth() + 1) + '.' + dd.getFullYear();
                                input.value = formatted;
                                applyFilters();
                                closeDatePicker();
                            };
                        })(new Date(cellDate)));
                        cell.title = pad2(cellDate.getDate()) + '.' + pad2(cellDate.getMonth() + 1) + '.' + cellDate.getFullYear();
                    }
                    row.appendChild(cell);
                    day++;
                }

                grid.appendChild(row);
            }

            popup.appendChild(grid);

            var clearBtn = document.createElement('button');
            clearBtn.type = 'button';
            clearBtn.className = 'date-filter-clear';
            clearBtn.textContent = 'Filter entfernen';
            clearBtn.addEventListener('click', function (e) {
                e.stopPropagation();
                input.value = '';
                applyFilters();
                closeDatePicker();
            });
            popup.appendChild(clearBtn);
        }

        render();

        setTimeout(function () {
            document.addEventListener('click', onOutsideClick);
        }, 10);

        function onOutsideClick(e) {
            if (popup && !popup.contains(e.target) && e.target !== anchorBtn) {
                closeDatePicker();
                document.removeEventListener('click', onOutsideClick);
            }
        }
    }

    function closeDatePicker() {
        if (_activePopup && _activePopup.parentNode) {
            _activePopup.parentNode.removeChild(_activePopup);
        }
        _activePopup = null;
    }

    function getIsoWeek(date) {
        var d = new Date(date.getTime());
        d.setHours(0, 0, 0, 0);
        d.setDate(d.getDate() + 3 - (d.getDay() + 6) % 7);
        var jan4 = new Date(d.getFullYear(), 0, 4);
        return 1 + Math.round(((d.getTime() - jan4.getTime()) / 86400000 - 3 + (jan4.getDay() + 6) % 7) / 7);
    }

    function pad2(n) {
        return n < 10 ? '0' + n : '' + n;
    }

    // Global function: Set a column filter value programmatically
    window.setColumnFilter = function (colKey, value) {
        if (!_filterRow) return;
        var input = _filterRow.querySelector('input[data-col-key="' + colKey + '"]');
        if (input) {
            input.value = value;
            applyFilters();
        }
    };

    // Global function: Trigger sorting on a column programmatically
    window.triggerSort = function (colKey, direction) {
        if (!_headers) return;
        var th = null;
        _headers.forEach(function (h) {
            if (h.getAttribute('data-col-key') === colKey) th = h;
        });
        if (!th) return;

        _headers.forEach(function (h) {
            h.removeAttribute('data-sort-dir');
            h.querySelector('.sort-indicator').textContent = '';
        });

        th.setAttribute('data-sort-dir', direction);
        th.querySelector('.sort-indicator').textContent = direction === 'asc' ? '\u25B2' : '\u25BC';
        sortTable(colKey, direction);
    };

    // Support deferred init: column-preferences.js dispatches 'column-preferences-ready'
    // If no column-preferences module, init immediately on DOMContentLoaded
    var _initialized = false;
    document.addEventListener('column-preferences-ready', function () {
        if (!_initialized) {
            _initialized = true;
            init();
        }
    });
    document.addEventListener('DOMContentLoaded', function () {
        // If no data-view-key table, init immediately (no column-preferences involved)
        var table = document.querySelector('.filterable-table');
        if (table && !table.hasAttribute('data-view-key')) {
            if (!_initialized) {
                _initialized = true;
                init();
            }
        }
        // If data-view-key exists, wait for column-preferences-ready event
        // But if after 2s no event, init anyway (fallback for pages without column-preferences.js)
        if (table && table.hasAttribute('data-view-key')) {
            setTimeout(function () {
                if (!_initialized) {
                    _initialized = true;
                    init();
                }
            }, 2000);
        }
    });
})();
```

- [ ] **Step 2: Verify build (JS is static, just check no syntax errors)**

Run: `cd IdealAkeWms && dotnet build`
Expected: Build succeeded (JS files are just served as static content)

- [ ] **Step 3: Commit**

```bash
git add IdealAkeWms/wwwroot/js/table-filter.js
git commit -m "refactor: table-filter.js from numeric data-col to string data-col-key"
```

---

## Task 6: Update Views — data-col to data-col-key

**Files:**
- Modify: `IdealAkeWms/Views/ProductionOrders/Index.cshtml`
- Modify: `IdealAkeWms/Views/Picking/Index.cshtml`

All `data-col="N"` attributes become `data-col-key="keyName"`. Also update any inline `triggerSort()` calls.

- [ ] **Step 1: Update ProductionOrders/Index.cshtml — thead**

Replace all `data-col` in the `<thead>` section. The key names match `ColumnDefinitions.ProductionOrders.Columns`:

| Old | New |
|-----|-----|
| `data-col="1"` on FA Nr. | `data-col-key="OrderNumber"` |
| `data-col="3"` on Kunde | `data-col-key="Customer"` |
| `data-col="4"` on Artikelnummer | `data-col-key="ArticleNumber"` |
| `data-col="5"` on Bezeichnung 1 | `data-col-key="Description1"` |
| `data-col="6"` on Bezeichnung 2 | `data-col-key="Description2"` |
| `data-col="7"` on Werkbank | `data-col-key="Workplace"` |
| `data-col="8"` on Beschicht. | `data-col-key="CoatingDate"` |
| `data-col="9"` on BG-Termin | `data-col-key="PreCommissionDate"` |
| `data-col="10"` on Komm. | `data-col-key="CommissioningDate"` |
| `data-col="11"` on Fert.-Termin | `data-col-key="ProductionDate"` |
| `data-col="12"` on Liefertermin | `data-col-key="DeliveryDate"` |
| `data-col="13"` on Lack-T | `data-col-key="CoatingDone"` |
| `data-col="14"` on Glas | `data-col-key="HasGlass"` |
| `data-col="15"` on Zukauf | `data-col-key="HasExternalPurchase"` |
| `data-col="16"` on Status | `data-col-key="Status"` |
| `data-col="picker"` on Kommissionierer | `data-col-key="Picker"` |

Also update the inline script's `triggerSort(10, 'asc')` call (if present) to `triggerSort('CommissioningDate', 'asc')`.

- [ ] **Step 2: Update Picking/Index.cshtml — thead**

| Old | New |
|-----|-----|
| `data-col="0"` on Prio | `data-col-key="Priority"` |
| `data-col="1"` on FA Nr. | `data-col-key="OrderNumber"` |
| `data-col="2"` on Artikelnummer | `data-col-key="ArticleNumber"` |
| `data-col="3"` on Bezeichnung | `data-col-key="Description"` |
| `data-col="4"` on Kunde | `data-col-key="Customer"` |
| `data-col="6"` on Komm.-Termin | `data-col-key="CommissioningDate"` |
| `data-col="7"` on Status | `data-col-key="Status"` |
| `data-col="8"` on Kommissionierer | `data-col-key="Picker"` |

- [ ] **Step 3: Check and update ALL other views using data-col**

Run: `grep -r 'data-col="' IdealAkeWms/Views/ --include="*.cshtml"` to find any remaining views that use old-style `data-col`. ALL views with `filterable-table` class must be updated — otherwise `table-filter.js` breaks on those pages. Known candidates beyond ProductionOrders and Picking:
- `Views/StockOverview/Index.cshtml`
- `Views/PartRequisitions/Index.cshtml`
- `Views/Tracking/OseonIndex.cshtml`
- Any other views with `data-col` attributes

Each needs `data-col="N"` → `data-col-key="keyName"` on all `<th>` elements. Choose descriptive key names (e.g., `ArticleNumber`, `StorageLocation`, `Status`).

- [ ] **Step 4: Check for inline triggerSort or setColumnFilter calls**

Run: `grep -rn 'triggerSort\|setColumnFilter' IdealAkeWms/Views/ --include="*.cshtml"` — update any hard-coded numeric references to string keys.

- [ ] **Step 5: Verify build**

Run: `cd IdealAkeWms && dotnet build`
Expected: Build succeeded

- [ ] **Step 6: Commit**

```bash
git add IdealAkeWms/Views/ProductionOrders/Index.cshtml IdealAkeWms/Views/Picking/Index.cshtml
git commit -m "refactor: update views from data-col to data-col-key string identifiers"
```

---

## Task 7: column-preferences.js — Core Module

**Files:**
- Create: `IdealAkeWms/wwwroot/js/column-preferences.js`

This is the main client-side module. It handles: loading preferences via API, applying column visibility/width/order, auto-saving changes with debounce, and dispatching the `column-preferences-ready` event.

- [ ] **Step 1: Create column-preferences.js**

```javascript
// wwwroot/js/column-preferences.js
// Customizable column preferences: hide/show, resize, reorder, default sort
(function () {
    'use strict';

    var _table = null;
    var _viewConfig = null;
    var _columnConfig = null;
    var _currentSettings = null;
    var _saveTimeout = null;
    var API_BASE = '/api/user-view-preferences/';

    document.addEventListener('DOMContentLoaded', function () {
        _table = document.querySelector('.filterable-table[data-view-key]');
        if (!_table) {
            document.dispatchEvent(new Event('column-preferences-ready'));
            return;
        }

        var viewConfigEl = document.getElementById('view-config');
        var columnConfigEl = document.getElementById('column-config');
        if (!viewConfigEl || !columnConfigEl) {
            document.dispatchEvent(new Event('column-preferences-ready'));
            return;
        }

        _viewConfig = JSON.parse(viewConfigEl.textContent);
        _columnConfig = JSON.parse(columnConfigEl.textContent);

        loadAndApply();
    });

    function loadAndApply() {
        fetch(API_BASE + _viewConfig.viewKey)
            .then(function (resp) {
                if (resp.status === 204) return null;
                if (!resp.ok) return null;
                return resp.json();
            })
            .then(function (settingsJson) {
                if (settingsJson && typeof settingsJson === 'string') {
                    _currentSettings = JSON.parse(settingsJson);
                } else if (settingsJson && typeof settingsJson === 'object') {
                    _currentSettings = settingsJson;
                } else {
                    _currentSettings = null;
                }
                applySettings();
                document.dispatchEvent(new Event('column-preferences-ready'));
            })
            .catch(function () {
                _currentSettings = null;
                document.dispatchEvent(new Event('column-preferences-ready'));
            });
    }

    function applySettings() {
        if (!_currentSettings || !_currentSettings.columns) return;
        var thead = _table.querySelector('thead');
        var headerRow = thead.querySelector('tr:first-child');
        var allThs = headerRow.querySelectorAll('th');

        // Apply visibility and width
        _currentSettings.columns.forEach(function (col) {
            var th = headerRow.querySelector('th[data-col-key="' + col.key + '"]');
            if (!th) return; // unknown key, skip

            // Check if this is a locked column — cannot be hidden
            var configEntry = _columnConfig.find(function (c) { return c.key === col.key; });
            if (configEntry && configEntry.locked && col.visible === false) return;

            if (col.visible === false) {
                hideColumn(col.key);
            }
            if (col.width) {
                setColumnWidth(col.key, col.width);
            }
        });

        // Apply column order (only if view supports reorder)
        if (_viewConfig.supportsReorder && _currentSettings.columns.length > 0) {
            applyColumnOrder(_currentSettings.columns);
        }

        // Apply default sort (only if view supports it)
        if (_viewConfig.supportsSortDefault && _currentSettings.defaultSortColumn) {
            // triggerSort is defined in table-filter.js, called after column-preferences-ready
            setTimeout(function () {
                if (window.triggerSort) {
                    window.triggerSort(
                        _currentSettings.defaultSortColumn,
                        _currentSettings.defaultSortDirection || 'asc'
                    );
                }
            }, 50);
        }
    }

    function hideColumn(colKey) {
        _table.querySelectorAll('th[data-col-key="' + colKey + '"]').forEach(function (el) {
            el.style.display = 'none';
        });
        // Hide corresponding td cells
        var headerRow = _table.querySelector('thead tr:first-child');
        var allThs = Array.from(headerRow.querySelectorAll('th'));
        var colIndex = -1;
        for (var i = 0; i < allThs.length; i++) {
            if (allThs[i].getAttribute('data-col-key') === colKey) { colIndex = i; break; }
        }
        if (colIndex >= 0) {
            _table.querySelectorAll('tbody tr').forEach(function (row) {
                var cells = row.querySelectorAll('td');
                if (cells[colIndex]) cells[colIndex].style.display = 'none';
            });
            // Also hide filter row cell
            var filterRow = _table.querySelector('.filter-row');
            if (filterRow) {
                var filterCells = filterRow.querySelectorAll('th');
                if (filterCells[colIndex]) filterCells[colIndex].style.display = 'none';
            }
        }
    }

    function showColumn(colKey) {
        _table.querySelectorAll('th[data-col-key="' + colKey + '"]').forEach(function (el) {
            el.style.display = '';
        });
        var headerRow = _table.querySelector('thead tr:first-child');
        var allThs = Array.from(headerRow.querySelectorAll('th'));
        var colIndex = -1;
        for (var i = 0; i < allThs.length; i++) {
            if (allThs[i].getAttribute('data-col-key') === colKey) { colIndex = i; break; }
        }
        if (colIndex >= 0) {
            _table.querySelectorAll('tbody tr').forEach(function (row) {
                var cells = row.querySelectorAll('td');
                if (cells[colIndex]) cells[colIndex].style.display = '';
            });
            var filterRow = _table.querySelector('.filter-row');
            if (filterRow) {
                var filterCells = filterRow.querySelectorAll('th');
                if (filterCells[colIndex]) filterCells[colIndex].style.display = '';
            }
        }
    }

    function setColumnWidth(colKey, width) {
        var th = _table.querySelector('thead tr:first-child th[data-col-key="' + colKey + '"]');
        if (th) th.style.width = width + 'px';
    }

    function applyColumnOrder(columns) {
        var headerRow = _table.querySelector('thead tr:first-child');
        var allThs = Array.from(headerRow.querySelectorAll('th'));

        // Build order map from settings: key -> desired order
        var orderMap = {};
        columns.forEach(function (col) {
            if (col.order !== undefined && col.order !== null) {
                orderMap[col.key] = col.order;
            }
        });

        // Sort th elements by their order value
        var sortedThs = allThs.slice().sort(function (a, b) {
            var keyA = a.getAttribute('data-col-key') || '';
            var keyB = b.getAttribute('data-col-key') || '';
            var orderA = orderMap[keyA] !== undefined ? orderMap[keyA] : 9999;
            var orderB = orderMap[keyB] !== undefined ? orderMap[keyB] : 9999;
            return orderA - orderB;
        });

        // Build index mapping: oldIndex -> newIndex
        var oldIndices = sortedThs.map(function (th) { return allThs.indexOf(th); });

        // Reorder header
        sortedThs.forEach(function (th) { headerRow.appendChild(th); });

        // Reorder body rows
        _table.querySelectorAll('tbody tr').forEach(function (row) {
            if (row.querySelector('td[colspan]')) return; // skip "no data" row
            var cells = Array.from(row.querySelectorAll('td'));
            var sorted = oldIndices.map(function (i) { return cells[i]; }).filter(Boolean);
            sorted.forEach(function (cell) { row.appendChild(cell); });
        });

        // Reorder filter row
        var filterRow = _table.querySelector('.filter-row');
        if (filterRow) {
            var filterCells = Array.from(filterRow.querySelectorAll('th'));
            var sortedFilter = oldIndices.map(function (i) { return filterCells[i]; }).filter(Boolean);
            sortedFilter.forEach(function (cell) { filterRow.appendChild(cell); });
        }
    }

    // ========================================================================
    // Auto-Save with Debounce
    // ========================================================================

    function scheduleSave() {
        if (_saveTimeout) clearTimeout(_saveTimeout);
        _saveTimeout = setTimeout(function () {
            saveCurrentState();
        }, 1500);
    }

    function saveCurrentState() {
        if (!_viewConfig) return;
        var settings = buildCurrentState();
        fetch(API_BASE + _viewConfig.viewKey, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(JSON.stringify(settings))
        });
    }

    function buildCurrentState() {
        var headerRow = _table.querySelector('thead tr:first-child');
        var allThs = Array.from(headerRow.querySelectorAll('th'));
        var columns = [];
        allThs.forEach(function (th, index) {
            var key = th.getAttribute('data-col-key');
            if (!key) return;
            columns.push({
                key: key,
                visible: th.style.display !== 'none',
                width: th.style.width ? parseInt(th.style.width) : null,
                order: index
            });
        });

        var state = { columns: columns };

        // Include default sort if supported
        if (_viewConfig.supportsSortDefault) {
            var sortedTh = _table.querySelector('th[data-sort-dir]');
            if (sortedTh) {
                state.defaultSortColumn = sortedTh.getAttribute('data-col-key');
                state.defaultSortDirection = sortedTh.getAttribute('data-sort-dir');
            }
        }

        return state;
    }

    // ========================================================================
    // Expose for other modules (gear dialog, context menu, resize)
    // ========================================================================

    window.columnPreferences = {
        hideColumn: function (key) { hideColumn(key); scheduleSave(); },
        showColumn: function (key) { showColumn(key); scheduleSave(); },
        showAllColumns: function () {
            _columnConfig.forEach(function (col) { showColumn(col.key); });
            scheduleSave();
        },
        setColumnWidth: function (key, width) { setColumnWidth(key, width); scheduleSave(); },
        getViewConfig: function () { return _viewConfig; },
        getColumnConfig: function () { return _columnConfig; },
        getCurrentSettings: function () { return buildCurrentState(); },
        scheduleSave: scheduleSave,
        resetToDefault: function () {
            fetch(API_BASE + _viewConfig.viewKey, { method: 'DELETE' })
                .then(function () { window.location.reload(); });
        }
    };
})();
```

- [ ] **Step 2: Verify build**

Run: `cd IdealAkeWms && dotnet build`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add IdealAkeWms/wwwroot/js/column-preferences.js
git commit -m "feat: add column-preferences.js core module (load, apply, auto-save)"
```

---

## Task 8: column-preferences.js — Gear Dialog (Offcanvas)

**Files:**
- Modify: `IdealAkeWms/wwwroot/js/column-preferences.js` (append gear dialog code)
- Modify: `IdealAkeWms/wwwroot/css/site.css` (add offcanvas styles)

- [ ] **Step 1: Add gear dialog functionality to column-preferences.js**

Append the following code inside the IIFE, before the closing `})();`, in `column-preferences.js`. This creates the gear icon button and the Offcanvas settings panel:

```javascript
    // ========================================================================
    // Gear Icon + Offcanvas Settings Panel
    // ========================================================================

    function createGearButton() {
        if (!_table || !_viewConfig) return;

        var tableContainer = _table.closest('.table-responsive');
        var insertTarget = tableContainer ? tableContainer.parentElement : _table.parentElement;

        var btn = document.createElement('button');
        btn.type = 'button';
        btn.className = 'btn btn-sm btn-outline-secondary column-prefs-gear';
        btn.title = 'Spalten-Einstellungen';
        btn.innerHTML = '<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" class="bi bi-gear" viewBox="0 0 16 16"><path d="M8 4.754a3.246 3.246 0 1 0 0 6.492 3.246 3.246 0 0 0 0-6.492M5.754 8a2.246 2.246 0 1 1 4.492 0 2.246 2.246 0 0 1-4.492 0"/><path d="M9.796 1.343c-.527-1.79-3.065-1.79-3.592 0l-.094.319a.873.873 0 0 1-1.255.52l-.292-.16c-1.64-.892-3.433.902-2.54 2.541l.159.292a.873.873 0 0 1-.52 1.255l-.319.094c-1.79.527-1.79 3.065 0 3.592l.319.094a.873.873 0 0 1 .52 1.255l-.16.292c-.892 1.64.901 3.434 2.541 2.54l.292-.159a.873.873 0 0 1 1.255.52l.094.319c.527 1.79 3.065 1.79 3.592 0l.094-.319a.873.873 0 0 1 1.255-.52l.292.16c1.64.893 3.434-.902 2.54-2.541l-.159-.292a.873.873 0 0 1 .52-1.255l.319-.094c1.79-.527 1.79-3.065 0-3.592l-.319-.094a.873.873 0 0 1-.52-1.255l.16-.292c.893-1.64-.902-3.433-2.541-2.54l-.292.159a.873.873 0 0 1-1.255-.52zm-2.633.283c.246-.835 1.428-.835 1.674 0l.094.319a1.873 1.873 0 0 0 2.693 1.115l.291-.16c.764-.415 1.6.42 1.184 1.185l-.159.292a1.873 1.873 0 0 0 1.116 2.692l.318.094c.835.246.835 1.428 0 1.674l-.319.094a1.873 1.873 0 0 0-1.115 2.693l.16.291c.415.764-.42 1.6-1.185 1.184l-.291-.159a1.873 1.873 0 0 0-2.693 1.116l-.094.318c-.246.835-1.428.835-1.674 0l-.094-.319a1.873 1.873 0 0 0-2.692-1.115l-.292.16c-.764.415-1.6-.42-1.184-1.185l.159-.291A1.873 1.873 0 0 0 1.945 8.93l-.319-.094c-.835-.246-.835-1.428 0-1.674l.319-.094A1.873 1.873 0 0 0 3.06 4.377l-.16-.292c-.415-.764.42-1.6 1.185-1.184l.292.159a1.873 1.873 0 0 0 2.692-1.115z"/></svg>';
        btn.addEventListener('click', openOffcanvas);

        // Insert before the table container
        var wrapper = document.createElement('div');
        wrapper.className = 'column-prefs-toolbar d-flex justify-content-end mb-1';
        wrapper.appendChild(btn);
        insertTarget.insertBefore(wrapper, tableContainer || _table);
    }

    function openOffcanvas() {
        // Remove existing
        var existing = document.getElementById('columnPrefsOffcanvas');
        if (existing) existing.remove();

        var offcanvas = document.createElement('div');
        offcanvas.className = 'offcanvas offcanvas-end';
        offcanvas.id = 'columnPrefsOffcanvas';
        offcanvas.setAttribute('tabindex', '-1');

        var header = document.createElement('div');
        header.className = 'offcanvas-header';
        header.innerHTML = '<h5 class="offcanvas-title">Spalten-Einstellungen</h5>' +
            '<button type="button" class="btn-close" data-bs-dismiss="offcanvas" aria-label="Schliessen"></button>';

        var body = document.createElement('div');
        body.className = 'offcanvas-body';

        // Column list
        var list = document.createElement('div');
        list.className = 'column-prefs-list';

        var headerRow = _table.querySelector('thead tr:first-child');
        var allThs = Array.from(headerRow.querySelectorAll('th'));

        _columnConfig.forEach(function (colDef) {
            var th = headerRow.querySelector('th[data-col-key="' + colDef.key + '"]');
            if (!th) return; // column not rendered (conditional)

            var item = document.createElement('div');
            item.className = 'column-prefs-item d-flex align-items-center gap-2 py-1 px-2';
            item.setAttribute('data-col-key', colDef.key);

            // Drag handle (only if reorder supported and not locked)
            if (_viewConfig.supportsReorder && !colDef.locked) {
                var handle = document.createElement('span');
                handle.className = 'column-prefs-drag-handle';
                handle.innerHTML = '<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" viewBox="0 0 16 16"><path d="M7 2a1 1 0 1 1-2 0 1 1 0 0 1 2 0m3 0a1 1 0 1 1-2 0 1 1 0 0 1 2 0M7 5a1 1 0 1 1-2 0 1 1 0 0 1 2 0m3 0a1 1 0 1 1-2 0 1 1 0 0 1 2 0M7 8a1 1 0 1 1-2 0 1 1 0 0 1 2 0m3 0a1 1 0 1 1-2 0 1 1 0 0 1 2 0m-3 3a1 1 0 1 1-2 0 1 1 0 0 1 2 0m3 0a1 1 0 1 1-2 0 1 1 0 0 1 2 0m-3 3a1 1 0 1 1-2 0 1 1 0 0 1 2 0m3 0a1 1 0 1 1-2 0 1 1 0 0 1 2 0"/></svg>';
                handle.style.cursor = 'grab';
                item.setAttribute('draggable', 'true');
                item.appendChild(handle);
            } else {
                var spacer = document.createElement('span');
                spacer.style.width = '16px';
                spacer.style.display = 'inline-block';
                item.appendChild(spacer);
            }

            // Checkbox
            var cb = document.createElement('input');
            cb.type = 'checkbox';
            cb.className = 'form-check-input';
            cb.checked = th.style.display !== 'none';
            cb.disabled = colDef.locked;
            cb.addEventListener('change', function () {
                if (cb.checked) {
                    window.columnPreferences.showColumn(colDef.key);
                } else {
                    window.columnPreferences.hideColumn(colDef.key);
                }
            });
            item.appendChild(cb);

            // Label
            var label = document.createElement('span');
            label.className = colDef.locked ? 'text-muted' : '';
            label.textContent = colDef.label + (colDef.locked ? ' (Pflichtspalte)' : '');
            item.appendChild(label);

            list.appendChild(item);
        });

        body.appendChild(list);

        // Default Sort section (only for supported views)
        if (_viewConfig.supportsSortDefault) {
            var sortSection = document.createElement('div');
            sortSection.className = 'mt-3 pt-3 border-top';
            sortSection.innerHTML = '<h6 class="text-muted">Standard-Sortierung</h6>';

            var sortSelect = document.createElement('select');
            sortSelect.className = 'form-select form-select-sm mb-2';
            var emptyOpt = document.createElement('option');
            emptyOpt.value = '';
            emptyOpt.textContent = '(keine)';
            sortSelect.appendChild(emptyOpt);
            _columnConfig.forEach(function (col) {
                var opt = document.createElement('option');
                opt.value = col.key;
                opt.textContent = col.label;
                if (_currentSettings && _currentSettings.defaultSortColumn === col.key) opt.selected = true;
                sortSelect.appendChild(opt);
            });

            var dirSelect = document.createElement('select');
            dirSelect.className = 'form-select form-select-sm';
            var ascOpt = document.createElement('option');
            ascOpt.value = 'asc';
            ascOpt.textContent = 'Aufsteigend';
            var descOpt = document.createElement('option');
            descOpt.value = 'desc';
            descOpt.textContent = 'Absteigend';
            if (_currentSettings && _currentSettings.defaultSortDirection === 'desc') descOpt.selected = true;
            dirSelect.appendChild(ascOpt);
            dirSelect.appendChild(descOpt);

            sortSelect.addEventListener('change', function () { scheduleSave(); });
            dirSelect.addEventListener('change', function () { scheduleSave(); });

            sortSection.appendChild(sortSelect);
            sortSection.appendChild(dirSelect);
            body.appendChild(sortSection);

            // Override buildCurrentState to include sort selects
            var origBuild = buildCurrentState;
            buildCurrentState = function () {
                var state = origBuild();
                if (sortSelect.value) {
                    state.defaultSortColumn = sortSelect.value;
                    state.defaultSortDirection = dirSelect.value;
                } else {
                    delete state.defaultSortColumn;
                    delete state.defaultSortDirection;
                }
                return state;
            };
        }

        // Reset button
        var resetBtn = document.createElement('button');
        resetBtn.type = 'button';
        resetBtn.className = 'btn btn-outline-danger btn-sm mt-3 w-100';
        resetBtn.textContent = 'Auf Standard zurücksetzen';
        resetBtn.addEventListener('click', function () {
            if (confirm('Alle Spalten-Einstellungen für diese Ansicht zurücksetzen?')) {
                window.columnPreferences.resetToDefault();
            }
        });
        body.appendChild(resetBtn);

        offcanvas.appendChild(header);
        offcanvas.appendChild(body);
        document.body.appendChild(offcanvas);

        // Init drag & drop on list items (if reorder supported)
        if (_viewConfig.supportsReorder) {
            initListDragDrop(list);
        }

        var bsOffcanvas = new bootstrap.Offcanvas(offcanvas);
        bsOffcanvas.show();

        offcanvas.addEventListener('hidden.bs.offcanvas', function () {
            offcanvas.remove();
        });
    }

    function initListDragDrop(list) {
        var dragItem = null;
        list.querySelectorAll('.column-prefs-item[draggable]').forEach(function (item) {
            item.addEventListener('dragstart', function (e) {
                dragItem = item;
                item.classList.add('dragging');
                e.dataTransfer.effectAllowed = 'move';
            });
            item.addEventListener('dragend', function () {
                item.classList.remove('dragging');
                dragItem = null;
                // Apply new order to table
                applyListOrderToTable(list);
                scheduleSave();
            });
            item.addEventListener('dragover', function (e) {
                e.preventDefault();
                if (!dragItem || dragItem === item) return;
                var rect = item.getBoundingClientRect();
                var midY = rect.top + rect.height / 2;
                if (e.clientY < midY) {
                    list.insertBefore(dragItem, item);
                } else {
                    list.insertBefore(dragItem, item.nextSibling);
                }
            });
        });
    }

    function applyListOrderToTable(list) {
        var items = list.querySelectorAll('.column-prefs-item');
        var keyOrder = [];
        items.forEach(function (item) {
            keyOrder.push(item.getAttribute('data-col-key'));
        });

        // Build columns array with order for applyColumnOrder
        var columns = keyOrder.map(function (key, index) {
            return { key: key, order: index };
        });
        applyColumnOrder(columns);
    }

    // Call createGearButton after settings are applied
    var origApplySettings = applySettings;
    applySettings = function () {
        origApplySettings();
        createGearButton();
    };
    // Also create gear button if no settings loaded
    var origLoadAndApply = loadAndApply;
    loadAndApply = function () {
        fetch(API_BASE + _viewConfig.viewKey)
            .then(function (resp) {
                if (resp.status === 204) return null;
                if (!resp.ok) return null;
                return resp.json();
            })
            .then(function (settingsJson) {
                if (settingsJson && typeof settingsJson === 'string') {
                    _currentSettings = JSON.parse(settingsJson);
                } else if (settingsJson && typeof settingsJson === 'object') {
                    _currentSettings = settingsJson;
                } else {
                    _currentSettings = null;
                }
                applySettings();
                createGearButton();
                document.dispatchEvent(new Event('column-preferences-ready'));
            })
            .catch(function () {
                _currentSettings = null;
                createGearButton();
                document.dispatchEvent(new Event('column-preferences-ready'));
            });
    };
```

Note: The gear button creation and offcanvas code should be integrated into the IIFE. Since the `loadAndApply` and `applySettings` functions are being extended, the actual implementation should merge this cleanly into the existing module. The key pattern is: `createGearButton()` is called after settings are applied (whether settings exist or not).

- [ ] **Step 2: Add CSS for offcanvas and gear button**

Append to `wwwroot/css/site.css`:

```css
/* ========================================================================
   Column Preferences
   ======================================================================== */

.column-prefs-gear {
    border-color: var(--ake-secondary);
    color: var(--ake-secondary);
}
.column-prefs-gear:hover {
    background-color: var(--ake-secondary);
    color: white;
}

.column-prefs-list .column-prefs-item {
    border-bottom: 1px solid #eee;
}
.column-prefs-list .column-prefs-item:last-child {
    border-bottom: none;
}
.column-prefs-list .column-prefs-item.dragging {
    opacity: 0.4;
    background: #e3f2fd;
}
.column-prefs-drag-handle {
    color: var(--ake-secondary);
}
```

- [ ] **Step 3: Commit**

```bash
git add IdealAkeWms/wwwroot/js/column-preferences.js IdealAkeWms/wwwroot/css/site.css
git commit -m "feat: add gear dialog (offcanvas) for column preferences"
```

---

## Task 9: Context Menu + Column Resize

**Files:**
- Modify: `IdealAkeWms/wwwroot/js/column-preferences.js` (append context menu + resize code)
- Modify: `IdealAkeWms/wwwroot/css/site.css` (add context menu + resize styles)

- [ ] **Step 1: Add context menu code to column-preferences.js**

Append inside the IIFE:

```javascript
    // ========================================================================
    // Right-Click Context Menu
    // ========================================================================

    var _contextMenu = null;

    function initContextMenu() {
        if (!_table) return;
        // Disable on touch devices
        if ('ontouchstart' in window) return;

        _table.querySelector('thead').addEventListener('contextmenu', function (e) {
            var th = e.target.closest('th[data-col-key]');
            if (!th) return;
            e.preventDefault();
            showContextMenu(e.pageX, e.pageY, th);
        });

        document.addEventListener('click', function () {
            closeContextMenu();
        });
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') closeContextMenu();
        });
    }

    function showContextMenu(x, y, th) {
        closeContextMenu();
        var colKey = th.getAttribute('data-col-key');
        var colDef = _columnConfig.find(function (c) { return c.key === colKey; });

        var menu = document.createElement('div');
        menu.className = 'column-context-menu';
        menu.style.left = x + 'px';
        menu.style.top = y + 'px';

        // "Hide column" (only if not locked)
        if (!colDef || !colDef.locked) {
            var hideItem = document.createElement('div');
            hideItem.className = 'column-context-item';
            hideItem.textContent = 'Spalte ausblenden';
            hideItem.addEventListener('click', function () {
                window.columnPreferences.hideColumn(colKey);
                closeContextMenu();
            });
            menu.appendChild(hideItem);
        }

        // "Show all columns"
        var showAllItem = document.createElement('div');
        showAllItem.className = 'column-context-item';
        showAllItem.textContent = 'Alle Spalten anzeigen';
        showAllItem.addEventListener('click', function () {
            window.columnPreferences.showAllColumns();
            closeContextMenu();
        });
        menu.appendChild(showAllItem);

        // Separator
        var sep = document.createElement('div');
        sep.className = 'column-context-separator';
        menu.appendChild(sep);

        // "Column settings..."
        var settingsItem = document.createElement('div');
        settingsItem.className = 'column-context-item';
        settingsItem.textContent = 'Spalten-Einstellungen...';
        settingsItem.addEventListener('click', function () {
            closeContextMenu();
            openOffcanvas();
        });
        menu.appendChild(settingsItem);

        document.body.appendChild(menu);
        _contextMenu = menu;

        // Adjust if off-screen
        var rect = menu.getBoundingClientRect();
        if (rect.right > window.innerWidth) menu.style.left = (x - rect.width) + 'px';
        if (rect.bottom > window.innerHeight) menu.style.top = (y - rect.height) + 'px';
    }

    function closeContextMenu() {
        if (_contextMenu && _contextMenu.parentNode) {
            _contextMenu.parentNode.removeChild(_contextMenu);
        }
        _contextMenu = null;
    }

    // ========================================================================
    // Column Resize
    // ========================================================================

    function initColumnResize() {
        if (!_table) return;
        // Disable on touch devices
        if (window.matchMedia && window.matchMedia('(hover: none)').matches) return;

        var headerRow = _table.querySelector('thead tr:first-child');
        headerRow.querySelectorAll('th[data-col-key]').forEach(function (th) {
            th.style.position = 'relative';

            var handle = document.createElement('div');
            handle.className = 'col-resize-handle';
            handle.addEventListener('mousedown', function (e) {
                e.preventDefault();
                e.stopPropagation();
                startResize(th, handle, e);
            });
            handle.addEventListener('dblclick', function (e) {
                e.preventDefault();
                e.stopPropagation();
                // Reset to default width
                var colKey = th.getAttribute('data-col-key');
                var colDef = _columnConfig.find(function (c) { return c.key === colKey; });
                if (colDef && colDef.defaultWidth) {
                    th.style.width = colDef.defaultWidth + 'px';
                } else {
                    th.style.width = '';
                }
                scheduleSave();
            });
            th.appendChild(handle);
        });
    }

    function startResize(th, handle, startEvent) {
        var startX = startEvent.pageX;
        var startWidth = th.offsetWidth;
        handle.classList.add('active');

        function onMove(e) {
            var newWidth = Math.max(40, startWidth + (e.pageX - startX));
            th.style.width = newWidth + 'px';
        }

        function onUp() {
            handle.classList.remove('active');
            document.removeEventListener('mousemove', onMove);
            document.removeEventListener('mouseup', onUp);
            scheduleSave();
        }

        document.addEventListener('mousemove', onMove);
        document.addEventListener('mouseup', onUp);
    }

    // Init context menu and resize after gear button
    var origCreateGearButton = createGearButton;
    createGearButton = function () {
        origCreateGearButton();
        initContextMenu();
        initColumnResize();
    };
```

- [ ] **Step 2: Add CSS for context menu and resize handles**

Append to `wwwroot/css/site.css`:

```css
/* Column Context Menu */
.column-context-menu {
    position: absolute;
    z-index: 1060;
    background: white;
    border: 1px solid rgba(0,0,0,.15);
    border-radius: .375rem;
    box-shadow: 0 .5rem 1rem rgba(0,0,0,.15);
    min-width: 200px;
    padding: 4px 0;
}
.column-context-item {
    padding: 6px 16px;
    cursor: pointer;
    font-size: 0.875rem;
}
.column-context-item:hover {
    background-color: #f8f9fa;
}
.column-context-separator {
    border-top: 1px solid #eee;
    margin: 4px 0;
}

/* Column Resize Handles */
.col-resize-handle {
    position: absolute;
    right: 0;
    top: 0;
    bottom: 0;
    width: 4px;
    cursor: col-resize;
    opacity: 0;
    transition: opacity 0.15s;
    z-index: 1;
}
th:hover .col-resize-handle {
    opacity: 0.5;
    background: var(--ake-secondary);
}
.col-resize-handle.active {
    opacity: 1 !important;
    background: var(--ake-secondary) !important;
}

/* Disable resize on touch devices */
@media (hover: none) {
    .col-resize-handle {
        display: none !important;
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add IdealAkeWms/wwwroot/js/column-preferences.js IdealAkeWms/wwwroot/css/site.css
git commit -m "feat: add context menu and column resize to column preferences"
```

---

## Task 10: Wire Up Views with column-config + gear icon

**Files:**
- Modify: `IdealAkeWms/Views/ProductionOrders/Index.cshtml`
- Modify: `IdealAkeWms/Views/Picking/Index.cshtml`
- Modify: `IdealAkeWms/Views/Tracking/OseonIndex.cshtml`
- Modify: `IdealAkeWms/Views/Picking/Bom.cshtml`

Each view needs: `data-view-key` on `<table>`, `<script type="application/json">` blocks for column-config and view-config, and script includes.

- [ ] **Step 1: Update ProductionOrders/Index.cshtml**

Add `data-view-key="ProductionOrders"` to the `<table>` element:

```html
<table class="table table-striped mb-0 filterable-table" data-view-key="ProductionOrders">
```

Add the JSON config blocks before the `@section Scripts` block. Use Razor to dynamically build the column config based on which conditional columns are rendered:

```html
<script type="application/json" id="view-config">
{ "viewKey": "ProductionOrders", "supportsReorder": true, "supportsSortDefault": true }
</script>
<script type="application/json" id="column-config">
[
    @if (Model.CanPick) { <text>{ "key": "BomAction", "label": "", "locked": true, "defaultWidth": 40 },</text> }
    { "key": "OrderNumber", "label": "FA Nr.", "locked": true, "defaultWidth": 90 },
    { "key": "Quantity", "label": "Stk.", "locked": false, "defaultWidth": 55 },
    { "key": "Customer", "label": "Kunde", "locked": false, "defaultWidth": null },
    { "key": "ArticleNumber", "label": "Artikelnummer", "locked": false, "defaultWidth": null },
    { "key": "Description1", "label": "Bezeichnung 1", "locked": false, "defaultWidth": null },
    { "key": "Description2", "label": "Bezeichnung 2", "locked": false, "defaultWidth": null },
    { "key": "Workplace", "label": "Werkbank", "locked": false, "defaultWidth": null },
    { "key": "CoatingDate", "label": "Beschicht.", "locked": false, "defaultWidth": 110 },
    { "key": "PreCommissionDate", "label": "BG-Termin", "locked": false, "defaultWidth": 110 },
    { "key": "CommissioningDate", "label": "Komm.", "locked": false, "defaultWidth": 110 },
    { "key": "ProductionDate", "label": "Fert.-Termin", "locked": false, "defaultWidth": 110 },
    { "key": "DeliveryDate", "label": "Liefertermin", "locked": false, "defaultWidth": 110 },
    { "key": "CoatingDone", "label": "Lack-T", "locked": false, "defaultWidth": 55 },
    { "key": "HasGlass", "label": "Glas", "locked": false, "defaultWidth": 45 },
    { "key": "HasExternalPurchase", "label": "Zukauf", "locked": false, "defaultWidth": 55 },
    { "key": "Status", "label": "Status", "locked": false, "defaultWidth": null },
    { "key": "Actions", "label": "", "locked": true, "defaultWidth": 80 }
    @if (Model.LeitstandAktiv && Model.CanManagePickingRelease) { <text>,{ "key": "Release", "label": "Freigabe", "locked": false, "defaultWidth": 160 }</text> }
    @if (Model.PickerAssignmentEnabled) { <text>,{ "key": "Picker", "label": "Kommissionierer", "locked": false, "defaultWidth": null }</text> }
]
</script>
```

Also add `data-col-key` to all `<th>` that don't have it yet (BomAction, Quantity, Actions, Release columns), and to the corresponding `<td>` elements in the `<tbody>`.

Update the `@section Scripts` to include `column-preferences.js` before `table-filter.js`:

```html
@section Scripts {
    <script src="~/js/column-preferences.js" asp-append-version="true"></script>
    <script src="~/js/table-filter.js" asp-append-version="true"></script>
    @* ... existing inline scripts ... *@
}
```

- [ ] **Step 2: Update Picking/Index.cshtml**

Similar pattern. Add `data-view-key="Picking"` to `<table>`:

```html
<table class="table table-striped table-hover mb-0 filterable-table" data-view-key="Picking">
```

Add JSON config blocks and update `@section Scripts` to include `column-preferences.js`.

- [ ] **Step 3: Update Tracking/OseonIndex.cshtml**

Add `data-view-key="OseonTracking"` to the table. Add column-config and view-config with `supportsReorder: false, supportsSortDefault: false`. Add `data-col-key` to `<th>` elements. Include `column-preferences.js` in scripts.

Note: OseonIndex doesn't use `filterable-table` class currently — it uses `id="oseonTree"`. Add the `filterable-table` class OR adjust `column-preferences.js` to also match `[data-view-key]` without requiring `filterable-table`. The simplest approach: add `data-view-key` and the column-preferences module already looks for `.filterable-table[data-view-key]` — so either add `filterable-table` class to the OSEON table, or update the selector in column-preferences.js to also match `table[data-view-key]`.

- [ ] **Step 4: Update Picking/Bom.cshtml**

Add `data-view-key="Bom"` to the BOM table, add column-config/view-config blocks, add `data-col-key` to BOM columns, include `column-preferences.js`. Since BOM is a tree structure, set `supportsReorder: false, supportsSortDefault: false`.

- [ ] **Step 5: Verify build and manual smoke test**

Run: `cd IdealAkeWms && dotnet build`
Expected: Build succeeded

Manual test checklist:
- Load ProductionOrders/Index — gear icon visible, clicking opens offcanvas
- Load Picking/Index — same
- Load Tracking/OseonIndex — gear icon visible, no reorder handles
- Filters still work on all views
- Sorting still works

- [ ] **Step 6: Commit**

```bash
git add IdealAkeWms/Views/ProductionOrders/Index.cshtml IdealAkeWms/Views/Picking/Index.cshtml IdealAkeWms/Views/Tracking/OseonIndex.cshtml IdealAkeWms/Views/Picking/Bom.cshtml IdealAkeWms/wwwroot/js/column-preferences.js
git commit -m "feat: wire up 4 views with column-preferences (gear icon, column-config)"
```

---

## Task 11: Admin Reset in UsersController + View

**Files:**
- Modify: `IdealAkeWms/Controllers/UsersController.cs`
- Modify: `IdealAkeWms/Views/Users/Edit.cshtml`

- [ ] **Step 1: Add ResetViewPreferences action to UsersController**

In `UsersController.cs`, add the new action after the `Edit` POST action (after line 163), and inject `IUserViewPreferenceRepository`:

Add to constructor dependencies:

```csharp
private readonly IUserViewPreferenceRepository _viewPreferenceRepository;
```

Add to constructor parameters and assignment.

Add the action:

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ResetViewPreferences(int id, string? viewKey)
{
    var user = await _userRepository.GetByIdAsync(id);
    if (user == null)
        return NotFound();

    if (!string.IsNullOrEmpty(viewKey))
    {
        await _viewPreferenceRepository.DeleteByUserAndViewAsync(id, viewKey);
        TempData["SuccessMessage"] = $"Ansichts-Einstellungen '{viewKey}' fuer '{user.Name}' wurden zurueckgesetzt.";
    }
    else
    {
        await _viewPreferenceRepository.DeleteAllByUserAsync(id);
        TempData["SuccessMessage"] = $"Alle Ansichts-Einstellungen fuer '{user.Name}' wurden zurueckgesetzt.";
    }

    return RedirectToAction(nameof(Edit), new { id });
}
```

Also pass saved view preferences to the Edit GET action, so the view knows which views have settings:

In the `Edit` GET action, before `return View(vm);`:

```csharp
ViewBag.SavedViewPreferences = await _viewPreferenceRepository.GetAllByUserAsync(id);
```

- [ ] **Step 2: Add reset buttons to Users/Edit.cshtml**

After the password section (after line 104, before the submit buttons), add:

```html
@{
    var savedPrefs = ViewBag.SavedViewPreferences as List<IdealAkeWms.Models.UserViewPreference>
                     ?? new List<IdealAkeWms.Models.UserViewPreference>();
}
@if (savedPrefs.Any())
{
    <hr />
    <h6 class="text-muted">Ansichts-Einstellungen</h6>

    @foreach (var pref in savedPrefs)
    {
        var displayName = pref.ViewKey switch
        {
            "ProductionOrders" => "Fertigungsauftraege",
            "Picking" => "Kommissionierliste",
            "OseonTracking" => "OSEON Teileverfolgung",
            "Bom" => "Stueckliste (BOM)",
            _ => pref.ViewKey
        };
        <div class="d-flex justify-content-between align-items-center mb-2">
            <span>@displayName</span>
            <form asp-action="ResetViewPreferences" asp-route-id="@Model.Id" method="post" class="d-inline">
                @Html.AntiForgeryToken()
                <input type="hidden" name="viewKey" value="@pref.ViewKey" />
                <button type="submit" class="btn btn-sm btn-outline-warning"
                        onclick="return confirm('Ansichts-Einstellungen fuer @displayName zuruecksetzen?')">
                    Zuruecksetzen
                </button>
            </form>
        </div>
    }

    <form asp-action="ResetViewPreferences" asp-route-id="@Model.Id" method="post" class="mt-2">
        @Html.AntiForgeryToken()
        <button type="submit" class="btn btn-sm btn-outline-danger"
                onclick="return confirm('Alle Ansichts-Einstellungen fuer diesen Benutzer zuruecksetzen?')">
            Alle Ansichten zuruecksetzen
        </button>
    </form>
}
```

- [ ] **Step 3: Verify build**

Run: `cd IdealAkeWms && dotnet build`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add IdealAkeWms/Controllers/UsersController.cs IdealAkeWms/Views/Users/Edit.cshtml
git commit -m "feat: add admin reset for view preferences in user edit"
```

---

## Task 12: Documentation + Version Update

**Files:**
- Modify: `IdealAkeWms/AppVersion.cs` — Bump version
- Modify: `IDEALAKEWMSService/AppVersion.cs` — Bump version
- Modify: `Views/Help/Index.cshtml` — Document feature
- Modify: `Views/Help/Changelog.cshtml` — Add changelog entry
- Modify: `CLAUDE.md` — Add relevant notes
- Modify: `PROJECT_STATUS.md` — Update status

- [ ] **Step 1: Bump version in both AppVersion.cs files**

Update version to next minor (e.g., `1.7.0`) and date to `2026-04-10`.

- [ ] **Step 2: Add help documentation**

Add a section about customizable views to `Views/Help/Index.cshtml`:
- Explain gear icon for column settings
- Explain right-click to hide columns
- Explain drag to resize
- Explain drag & drop to reorder (flat tables only)
- Explain default sort setting
- Explain reset via gear panel

- [ ] **Step 3: Add changelog entry**

Add entry to `Views/Help/Changelog.cshtml` for the new version.

- [ ] **Step 4: Update CLAUDE.md**

Add notes about:
- `UserViewPreference` entity and `UserViewPreferencesApiController`
- `ColumnDefinitions.cs` as source of truth for column metadata
- `column-preferences.js` + `table-filter.js` interaction (custom event `column-preferences-ready`)
- `data-col-key` convention (replaces old numeric `data-col`)

- [ ] **Step 5: Update PROJECT_STATUS.md**

- [ ] **Step 6: Commit**

```bash
git add IdealAkeWms/AppVersion.cs IDEALAKEWMSService/AppVersion.cs IdealAkeWms/Views/Help/ CLAUDE.md PROJECT_STATUS.md
git commit -m "docs: document customizable view preferences, bump version to v1.7.0"
```

---

## Important Notes for Implementation

1. **table-filter.js backward compatibility**: After the refactoring (Task 5+6), ALL views that use `filterable-table` must have `data-col-key` instead of `data-col`. Check for any views not covered in Task 6 (e.g., PartRequisitions/Index, StockOverview/Index if they use filterable-table).

2. **Column-preferences.js is one large file**: The plan shows additions in stages (Tasks 7, 8, 9), but the final file should be one cohesive IIFE module. When implementing Task 8 and 9, merge the code into the existing module rather than creating separate files.

3. **OSEON tree view selector**: `column-preferences.js` looks for `.filterable-table[data-view-key]`. OseonIndex doesn't use `filterable-table`. Either add the class or update the selector to `table[data-view-key]`.

4. **BOM view complexity**: Bom.cshtml is a complex view with nested structures. The column-preferences integration should only affect the main `<thead>` columns, not the nested content. Test thoroughly.

5. **The `triggerSort` call timing**: `column-preferences.js` dispatches `column-preferences-ready`, then `table-filter.js` inits, then column-preferences applies the default sort via `setTimeout(50ms)`. This sequence must be preserved.
