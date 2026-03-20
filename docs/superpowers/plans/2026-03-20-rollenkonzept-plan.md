# Rollenkonzept Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Convert boolean-based permission system to role-based access control with Role/UserRole tables, AD group mapping, and two-phase migration.

**Architecture:** New `Role` + `UserRole` entities with `RoleKeys` constants. `CurrentUserService` loads all roles (direct + AD) once per request into a `HashSet<string>`. Existing filters delegate to `HasRoleAsync()`. Two-phase migration: Phase 1 adds tables + migrates data, Phase 2 removes old boolean columns after verification.

**Tech Stack:** ASP.NET Core 10.0, EF Core 10.0, SQL Server, xUnit + FluentAssertions + Moq

**Spec:** `docs/superpowers/specs/2026-03-20-rollenkonzept-design.md`

---

## File Structure

### New Files
| File | Responsibility |
|------|---------------|
| `IdealAkeWms/Models/Role.cs` | Role entity (Key, Name, Description, AdGroup, IsSystem, SortOrder) |
| `IdealAkeWms/Models/UserRole.cs` | Junction entity (UserId, RoleId) |
| `IdealAkeWms/Models/RoleKeys.cs` | Static constants for role key strings |
| `IdealAkeWms/Data/Repositories/IRoleRepository.cs` | Role repository interface |
| `IdealAkeWms/Data/Repositories/RoleRepository.cs` | Role repository implementation |
| `IdealAkeWms/Models/ViewModels/RoleEditViewModel.cs` | ViewModel for role CRUD |
| `IdealAkeWms/Models/ViewModels/UserEditViewModel.cs` | ViewModel for user edit with role checkboxes |
| `IdealAkeWms/Controllers/RolesController.cs` | CRUD controller for roles |
| `IdealAkeWms/Views/Roles/Index.cshtml` | Roles list view |
| `IdealAkeWms/Views/Roles/Edit.cshtml` | Role edit view |
| `IdealAkeWms/Views/Roles/Create.cshtml` | Role create view |
| `IdealAkeWms/Filters/RequireStockAccessAttribute.cs` | Filter: stock, stock_keyuser, picking, admin |
| `IdealAkeWms/Filters/RequireStockKeyUserAccessAttribute.cs` | Filter: stock_keyuser, picking, admin |
| `IdealAkeWms/Filters/RequireReportingAccessAttribute.cs` | Filter: admin, reporting |
| `IdealAkeWms.Tests/Services/CurrentUserServiceRoleTests.cs` | Unit tests for role-based CurrentUserService |
| `IdealAkeWms.Tests/Filters/RoleFilterTests.cs` | Unit tests for filters |

### Modified Files
| File | Changes |
|------|---------|
| `IdealAkeWms/Models/User.cs` | Phase 1: Add `UserRoles` navigation (keep booleans). Phase 2: Remove 5 boolean fields |
| `IdealAkeWms/Data/ApplicationDbContext.cs` | Add DbSets for Role/UserRole, fluent config |
| `IdealAkeWms/Services/ICurrentUserService.cs` | Add `HasRoleAsync`, `HasAnyRoleAsync`, `CanAccessStockAsync`, `CanTransferStockAsync` |
| `IdealAkeWms/Services/CurrentUserService.cs` | Rewrite to load roles from DB + AD, cache per request |
| `IdealAkeWms/Controllers/UsersController.cs` | Use UserEditViewModel, manage role assignments |
| `IdealAkeWms/Controllers/StockMovementsController.cs` | Remove class-level filter, add per-action filters |
| `IdealAkeWms/Controllers/StockOverviewController.cs` | Change `[RequirePickingAccess]` → `[RequireStockAccess]` |
| `IdealAkeWms/Views/Users/Edit.cshtml` | Replace boolean checkboxes with role checkbox group |
| `IdealAkeWms/Views/Users/Create.cshtml` | Replace boolean checkboxes with role checkbox group |
| `IdealAkeWms/Views/Users/Index.cshtml` | Replace `IsAdmin` badge with role badges |
| `IdealAkeWms/Views/Shared/_Layout.cshtml` | Navbar visibility based on roles |
| `IdealAkeWms/Filters/RequireMasterDataAccessAttribute.cs` | Delegate to `HasMasterDataAccessAsync()` (unchanged call, new implementation) |
| `IdealAkeWms/Filters/RequirePickingAccessAttribute.cs` | Delegate to `CanPickAsync()` (unchanged) |
| `IdealAkeWms/Filters/RequireTrackingAccessAttribute.cs` | Delegate to `CanViewTrackingAsync()` (unchanged) |
| `IdealAkeWms/Filters/RequirePickingOrTrackingAccessAttribute.cs` | Delegate (unchanged) |
| `IdealAkeWms/Filters/RequireAdminAccessAttribute.cs` | Delegate to `IsAdminAsync()` (unchanged) |
| `IdealAkeWms/Program.cs` | Update admin seeding, register IRoleRepository, seed roles |
| `IdealAkeWms/appsettings.json` | Add `Security:AdGroupCacheMinutes` |
| `SQL/00_FreshInstall.sql` | Add Role/UserRole tables, seed data |

---

## Task 1: Role + UserRole Entities & RoleKeys

**Files:**
- Create: `IdealAkeWms/Models/Role.cs`
- Create: `IdealAkeWms/Models/UserRole.cs`
- Create: `IdealAkeWms/Models/RoleKeys.cs`

- [ ] **Step 1: Create `Role.cs`**

```csharp
using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

public class Role : AuditableEntity
{
    [Required(ErrorMessage = "Schlüssel ist erforderlich")]
    [StringLength(50)]
    [Display(Name = "Schlüssel")]
    public string Key { get; set; } = string.Empty;

    [Required(ErrorMessage = "Name ist erforderlich")]
    [StringLength(100)]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Beschreibung")]
    public string? Description { get; set; }

    [StringLength(200)]
    [Display(Name = "AD-Gruppe")]
    public string? AdGroup { get; set; }

    [Display(Name = "Systemrolle")]
    public bool IsSystem { get; set; }

    [Display(Name = "Sortierung")]
    public int SortOrder { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
```

- [ ] **Step 2: Create `UserRole.cs`**

```csharp
namespace IdealAkeWms.Models;

public class UserRole : AuditableEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
}
```

- [ ] **Step 3: Create `RoleKeys.cs`**

```csharp
namespace IdealAkeWms.Models;

public static class RoleKeys
{
    public const string Admin = "admin";
    public const string MasterData = "masterdata";
    public const string Picking = "picking";
    public const string Stock = "stock";
    public const string StockKeyUser = "stock_keyuser";
    public const string Tracking = "tracking";
    public const string Reporting = "reporting";
}
```

- [ ] **Step 4: Commit**

```bash
git add IdealAkeWms/Models/Role.cs IdealAkeWms/Models/UserRole.cs IdealAkeWms/Models/RoleKeys.cs
git commit -m "feat: add Role, UserRole entities and RoleKeys constants"
```

---

## Task 2: Update User Model — Phase 1 (Add Navigation Only)

**IMPORTANT:** Do NOT remove boolean fields yet! They stay until Phase 2 (Task 17) after verification.

**Files:**
- Modify: `IdealAkeWms/Models/User.cs`

- [ ] **Step 1: Add UserRoles navigation property**

Add after line 54 (`CanReportOperations`), before the existing collection properties:
```csharp
public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
```

All existing boolean fields (`HasMasterDataAccess`, `IsAdmin`, `CanPick`, `CanViewTracking`, `CanReportOperations`) remain unchanged for now.

- [ ] **Step 2: Commit**

```bash
git add IdealAkeWms/Models/User.cs
git commit -m "feat: add UserRoles navigation property to User (keep boolean fields for Phase 1)"
```

---

## Task 3: ApplicationDbContext — Role & UserRole Configuration

**Files:**
- Modify: `IdealAkeWms/Data/ApplicationDbContext.cs`

- [ ] **Step 1: Add DbSets**

After line 28 (`DbSet<ServiceSetting>`), add:
```csharp
public DbSet<Role> Roles => Set<Role>();
public DbSet<UserRole> UserRoles => Set<UserRole>();
```

- [ ] **Step 2: Add fluent config in OnModelCreating**

After the User entity configuration block (after line 46), add:

```csharp
// Role
modelBuilder.Entity<Role>(entity =>
{
    entity.ToTable("Roles");
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Key).HasMaxLength(50).IsRequired();
    entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
    entity.Property(e => e.Description).HasMaxLength(500);
    entity.Property(e => e.AdGroup).HasMaxLength(200);
    entity.Property(e => e.CreatedBy).HasMaxLength(200).IsRequired();
    entity.Property(e => e.CreatedByWindows).HasMaxLength(200).IsRequired();
    entity.Property(e => e.ModifiedBy).HasMaxLength(200);
    entity.Property(e => e.ModifiedByWindows).HasMaxLength(200);

    entity.HasIndex(e => e.Key).IsUnique();
});

// UserRole
modelBuilder.Entity<UserRole>(entity =>
{
    entity.ToTable("UserRoles");
    entity.HasKey(e => e.Id);
    entity.Property(e => e.CreatedBy).HasMaxLength(200).IsRequired();
    entity.Property(e => e.CreatedByWindows).HasMaxLength(200).IsRequired();
    entity.Property(e => e.ModifiedBy).HasMaxLength(200);
    entity.Property(e => e.ModifiedByWindows).HasMaxLength(200);

    entity.HasIndex(e => new { e.UserId, e.RoleId }).IsUnique();

    entity.HasOne(e => e.User)
        .WithMany(u => u.UserRoles)
        .HasForeignKey(e => e.UserId)
        .OnDelete(DeleteBehavior.Cascade);

    entity.HasOne(e => e.Role)
        .WithMany(r => r.UserRoles)
        .HasForeignKey(e => e.RoleId)
        .OnDelete(DeleteBehavior.Cascade);
});
```

- [ ] **Step 3: Remove old User boolean columns from User fluent config if referenced**

The User entity config (lines 35-46) does not explicitly configure the boolean fields — they're convention-based. No changes needed here. EF will detect the model change (removed properties) in the migration.

- [ ] **Step 4: Commit**

```bash
git add IdealAkeWms/Data/ApplicationDbContext.cs
git commit -m "feat: add Role and UserRole DbSets and fluent configuration"
```

---

## Task 4: Role Repository

**Files:**
- Create: `IdealAkeWms/Data/Repositories/IRoleRepository.cs`
- Create: `IdealAkeWms/Data/Repositories/RoleRepository.cs`

- [ ] **Step 1: Create `IRoleRepository.cs`**

```csharp
using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public interface IRoleRepository
{
    Task<List<Role>> GetAllOrderedAsync();
    Task<Role?> GetByIdAsync(int id);
    Task<Role?> GetByKeyAsync(string key);
    Task AddAsync(Role role);
    Task UpdateAsync(Role role);
    Task DeleteAsync(Role role);
    Task<List<Role>> GetRolesWithAdGroupAsync();
    Task<List<string>> GetRoleKeysByUserIdAsync(int userId);
    Task SetUserRolesAsync(int userId, List<int> roleIds, string createdBy, string createdByWindows);
}
```

- [ ] **Step 2: Create `RoleRepository.cs`**

```csharp
using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly ApplicationDbContext _context;

    public RoleRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Role>> GetAllOrderedAsync()
    {
        return await _context.Roles
            .Include(r => r.UserRoles)
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.Name)
            .ToListAsync();
    }

    public async Task<Role?> GetByIdAsync(int id)
    {
        return await _context.Roles
            .Include(r => r.UserRoles)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<Role?> GetByKeyAsync(string key)
    {
        return await _context.Roles.FirstOrDefaultAsync(r => r.Key == key);
    }

    public async Task AddAsync(Role role)
    {
        _context.Roles.Add(role);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Role role)
    {
        _context.Roles.Update(role);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Role role)
    {
        _context.Roles.Remove(role);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Role>> GetRolesWithAdGroupAsync()
    {
        return await _context.Roles
            .Where(r => r.AdGroup != null && r.AdGroup != "")
            .ToListAsync();
    }

    public async Task<List<string>> GetRoleKeysByUserIdAsync(int userId)
    {
        return await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.Role.Key)
            .ToListAsync();
    }

    public async Task SetUserRolesAsync(int userId, List<int> roleIds, string createdBy, string createdByWindows)
    {
        var existing = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .ToListAsync();

        // Remove roles not in the new list
        var toRemove = existing.Where(ur => !roleIds.Contains(ur.RoleId)).ToList();
        _context.UserRoles.RemoveRange(toRemove);

        // Add new roles
        var existingRoleIds = existing.Select(ur => ur.RoleId).ToHashSet();
        var toAdd = roleIds
            .Where(rid => !existingRoleIds.Contains(rid))
            .Select(rid => new UserRole
            {
                UserId = userId,
                RoleId = rid,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                CreatedByWindows = createdByWindows
            });

        _context.UserRoles.AddRange(toAdd);
        await _context.SaveChangesAsync();
    }
}
```

- [ ] **Step 3: Register in `Program.cs`**

After line 50 (`IOseonProductionOrderRepository`), add:
```csharp
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
```

- [ ] **Step 4: Commit**

```bash
git add IdealAkeWms/Data/Repositories/IRoleRepository.cs IdealAkeWms/Data/Repositories/RoleRepository.cs IdealAkeWms/Program.cs
git commit -m "feat: add IRoleRepository and RoleRepository with user-role management"
```

---

## Task 5: ICurrentUserService + CurrentUserService — Rollenbasiert

**Files:**
- Modify: `IdealAkeWms/Services/ICurrentUserService.cs`
- Modify: `IdealAkeWms/Services/CurrentUserService.cs`
- Modify: `IdealAkeWms/appsettings.json`

- [ ] **Step 1: Update `ICurrentUserService.cs`**

Replace entire file:
```csharp
namespace IdealAkeWms.Services;

public interface ICurrentUserService
{
    string GetWindowsUserName();
    string GetDisplayName();
    int? GetCurrentAppUserId();
    string? GetCurrentAppUserName();
    bool IsLoggedIn();

    Task<bool> HasRoleAsync(string roleKey);
    Task<bool> HasAnyRoleAsync(params string[] roleKeys);

    Task<bool> IsAdminAsync();
    Task<bool> HasMasterDataAccessAsync();
    Task<bool> CanPickAsync();
    Task<bool> CanViewTrackingAsync();
    Task<bool> CanReportOperationsAsync();
    Task<bool> CanAccessStockAsync();
    Task<bool> CanTransferStockAsync();
}
```

- [ ] **Step 2: Rewrite `CurrentUserService.cs`**

Replace entire file:
```csharp
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using Microsoft.Extensions.Caching.Memory;

namespace IdealAkeWms.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IRoleRepository _roleRepository;
    private readonly IMemoryCache _memoryCache;
    private readonly IConfiguration _configuration;

    private HashSet<string>? _cachedRoleKeys;

    public const string SessionKeyUserId = "AppUserId";
    public const string SessionKeyUserName = "AppUserName";

    public CurrentUserService(
        IHttpContextAccessor httpContextAccessor,
        IRoleRepository roleRepository,
        IMemoryCache memoryCache,
        IConfiguration configuration)
    {
        _httpContextAccessor = httpContextAccessor;
        _roleRepository = roleRepository;
        _memoryCache = memoryCache;
        _configuration = configuration;
    }

    public string GetWindowsUserName()
    {
        return _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "SYSTEM";
    }

    public string GetDisplayName()
    {
        var appUserName = GetCurrentAppUserName();
        if (!string.IsNullOrEmpty(appUserName))
            return appUserName;

        var identity = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "SYSTEM";
        if (identity.Contains('\\'))
            return identity.Split('\\').Last();
        return identity;
    }

    public int? GetCurrentAppUserId()
    {
        return _httpContextAccessor.HttpContext?.Session.GetInt32(SessionKeyUserId);
    }

    public string? GetCurrentAppUserName()
    {
        return _httpContextAccessor.HttpContext?.Session.GetString(SessionKeyUserName);
    }

    public bool IsLoggedIn()
    {
        return GetCurrentAppUserId().HasValue;
    }

    public async Task<bool> HasRoleAsync(string roleKey)
    {
        var roles = await LoadRoleKeysAsync();
        return roles.Contains(roleKey);
    }

    public async Task<bool> HasAnyRoleAsync(params string[] roleKeys)
    {
        var roles = await LoadRoleKeysAsync();
        return roleKeys.Any(roles.Contains);
    }

    public async Task<bool> IsAdminAsync()
        => await HasRoleAsync(RoleKeys.Admin);

    public async Task<bool> HasMasterDataAccessAsync()
        => await HasAnyRoleAsync(RoleKeys.Admin, RoleKeys.MasterData);

    public async Task<bool> CanPickAsync()
        => await HasAnyRoleAsync(RoleKeys.Admin, RoleKeys.Picking);

    public async Task<bool> CanViewTrackingAsync()
        => await HasAnyRoleAsync(RoleKeys.Admin, RoleKeys.Tracking);

    public async Task<bool> CanReportOperationsAsync()
        => await HasAnyRoleAsync(RoleKeys.Admin, RoleKeys.Reporting);

    public async Task<bool> CanAccessStockAsync()
        => await HasAnyRoleAsync(RoleKeys.Admin, RoleKeys.Stock, RoleKeys.StockKeyUser, RoleKeys.Picking);

    public async Task<bool> CanTransferStockAsync()
        => await HasAnyRoleAsync(RoleKeys.Admin, RoleKeys.StockKeyUser, RoleKeys.Picking);

    private async Task<HashSet<string>> LoadRoleKeysAsync()
    {
        if (_cachedRoleKeys != null)
            return _cachedRoleKeys;

        var roleKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 1. Direct roles from DB
        var userId = GetCurrentAppUserId();
        if (userId.HasValue)
        {
            var directRoles = await _roleRepository.GetRoleKeysByUserIdAsync(userId.Value);
            foreach (var key in directRoles)
                roleKeys.Add(key);
        }

        // 2. AD-group based roles
        var adRoles = await GetAdGroupRolesAsync();
        foreach (var key in adRoles)
            roleKeys.Add(key);

        _cachedRoleKeys = roleKeys;
        return roleKeys;
    }

    private async Task<List<string>> GetAdGroupRolesAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var windowsUser = httpContext?.User;
        if (windowsUser?.Identity?.IsAuthenticated != true)
            return new List<string>();

        var cacheMinutes = _configuration.GetValue("Security:AdGroupCacheMinutes", 5);
        var windowsName = windowsUser.Identity.Name ?? "UNKNOWN";
        var cacheKey = $"AdGroupRoles:{windowsName}";

        if (_memoryCache.TryGetValue(cacheKey, out List<string>? cached) && cached != null)
            return cached;

        var rolesWithAdGroup = await _roleRepository.GetRolesWithAdGroupAsync();
        var matchedKeys = new List<string>();

        foreach (var role in rolesWithAdGroup)
        {
            if (!string.IsNullOrEmpty(role.AdGroup) && windowsUser.IsInRole(role.AdGroup))
                matchedKeys.Add(role.Key);
        }

        _memoryCache.Set(cacheKey, matchedKeys, TimeSpan.FromMinutes(cacheMinutes));
        return matchedKeys;
    }
}
```

- [ ] **Step 3: Add config to `appsettings.json`**

Add to the root JSON object:
```json
"Security": {
    "AdGroupCacheMinutes": 5
}
```

- [ ] **Step 4: Commit**

```bash
git add IdealAkeWms/Services/ICurrentUserService.cs IdealAkeWms/Services/CurrentUserService.cs IdealAkeWms/appsettings.json
git commit -m "feat: rewrite CurrentUserService to role-based with AD group caching"
```

---

## Task 6: New Filters (Stock, StockKeyUser, Reporting)

**Files:**
- Create: `IdealAkeWms/Filters/RequireStockAccessAttribute.cs`
- Create: `IdealAkeWms/Filters/RequireStockKeyUserAccessAttribute.cs`
- Create: `IdealAkeWms/Filters/RequireReportingAccessAttribute.cs`

- [ ] **Step 1: Create `RequireStockAccessAttribute.cs`**

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using IdealAkeWms.Services;

namespace IdealAkeWms.Filters;

public class RequireStockAccessAttribute : TypeFilterAttribute
{
    public RequireStockAccessAttribute() : base(typeof(RequireStockAccessFilter)) { }
}

public class RequireStockAccessFilter : IAsyncActionFilter
{
    private readonly ICurrentUserService _currentUserService;

    public RequireStockAccessFilter(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!await _currentUserService.CanAccessStockAsync())
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
            return;
        }

        await next();
    }
}
```

- [ ] **Step 2: Create `RequireStockKeyUserAccessAttribute.cs`**

Same pattern, but calls `CanTransferStockAsync()`.

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using IdealAkeWms.Services;

namespace IdealAkeWms.Filters;

public class RequireStockKeyUserAccessAttribute : TypeFilterAttribute
{
    public RequireStockKeyUserAccessAttribute() : base(typeof(RequireStockKeyUserAccessFilter)) { }
}

public class RequireStockKeyUserAccessFilter : IAsyncActionFilter
{
    private readonly ICurrentUserService _currentUserService;

    public RequireStockKeyUserAccessFilter(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!await _currentUserService.CanTransferStockAsync())
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
            return;
        }

        await next();
    }
}
```

- [ ] **Step 3: Create `RequireReportingAccessAttribute.cs`**

Same pattern, calls `CanReportOperationsAsync()`.

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using IdealAkeWms.Services;

namespace IdealAkeWms.Filters;

public class RequireReportingAccessAttribute : TypeFilterAttribute
{
    public RequireReportingAccessAttribute() : base(typeof(RequireReportingAccessFilter)) { }
}

public class RequireReportingAccessFilter : IAsyncActionFilter
{
    private readonly ICurrentUserService _currentUserService;

    public RequireReportingAccessFilter(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!await _currentUserService.CanReportOperationsAsync())
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
            return;
        }

        await next();
    }
}
```

- [ ] **Step 4: Commit**

```bash
git add IdealAkeWms/Filters/RequireStockAccessAttribute.cs IdealAkeWms/Filters/RequireStockKeyUserAccessAttribute.cs IdealAkeWms/Filters/RequireReportingAccessAttribute.cs
git commit -m "feat: add RequireStockAccess, RequireStockKeyUserAccess, RequireReportingAccess filters"
```

---

## Task 7: Update Controllers (StockMovements, StockOverview)

**Files:**
- Modify: `IdealAkeWms/Controllers/StockMovementsController.cs`
- Modify: `IdealAkeWms/Controllers/StockOverviewController.cs`

- [ ] **Step 1: Update `StockMovementsController.cs`**

Remove class-level `[RequirePickingAccess]` from line 11. Add per-action filters:

- `Index` → `[RequireStockAccess]`
- `Inbound` (GET + POST) → `[RequireStockAccess]`
- `Outbound` (GET + POST) → `[RequireStockAccess]`
- `OutboundAll` (GET + POST) → `[RequireStockKeyUserAccess]`
- `Transfer` (GET + POST) → `[RequireStockAccess]`
- `LocationTransfer` (GET + POST) → `[RequireStockKeyUserAccess]`

Replace line 11:
```csharp
// Old: [RequirePickingAccess]
// New: Per-action filters (see individual actions)
```

Add `[RequireStockAccess]` before `Index()`, `Inbound()`, `Outbound()`, `Transfer()` methods.
Add `[RequireStockKeyUserAccess]` before `OutboundAll()`, `LocationTransfer()` methods.

- [ ] **Step 2: Update `StockOverviewController.cs`**

Change line 9 from `[RequirePickingAccess]` to `[RequireStockAccess]`.

- [ ] **Step 3: Commit**

```bash
git add IdealAkeWms/Controllers/StockMovementsController.cs IdealAkeWms/Controllers/StockOverviewController.cs
git commit -m "refactor: replace class-level picking filter with per-action stock filters"
```

---

## Task 8: RolesController + Views

**Files:**
- Create: `IdealAkeWms/Models/ViewModels/RoleEditViewModel.cs`
- Create: `IdealAkeWms/Controllers/RolesController.cs`
- Create: `IdealAkeWms/Views/Roles/Index.cshtml`
- Create: `IdealAkeWms/Views/Roles/Edit.cshtml`
- Create: `IdealAkeWms/Views/Roles/Create.cshtml`

- [ ] **Step 1: Create `RoleEditViewModel.cs`**

```csharp
using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models.ViewModels;

public class RoleEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Schlüssel ist erforderlich")]
    [StringLength(50)]
    [Display(Name = "Schlüssel")]
    public string Key { get; set; } = string.Empty;

    [Required(ErrorMessage = "Name ist erforderlich")]
    [StringLength(100)]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Beschreibung")]
    public string? Description { get; set; }

    [StringLength(200)]
    [Display(Name = "AD-Gruppe")]
    public string? AdGroup { get; set; }

    [Display(Name = "Sortierung")]
    public int SortOrder { get; set; }

    public bool IsSystem { get; set; }
    public int UserCount { get; set; }
}
```

- [ ] **Step 2: Create `RolesController.cs`**

```csharp
using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;

namespace IdealAkeWms.Controllers;

[RequireMasterDataAccess]
public class RolesController : Controller
{
    private readonly IRoleRepository _roleRepository;
    private readonly ICurrentUserService _currentUserService;

    public RolesController(IRoleRepository roleRepository, ICurrentUserService currentUserService)
    {
        _roleRepository = roleRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IActionResult> Index()
    {
        var roles = await _roleRepository.GetAllOrderedAsync();
        return View(roles);
    }

    public IActionResult Create()
    {
        return View(new RoleEditViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RoleEditViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var existing = await _roleRepository.GetByKeyAsync(vm.Key);
        if (existing != null)
        {
            ModelState.AddModelError("Key", "Dieser Schlüssel ist bereits vergeben.");
            return View(vm);
        }

        var role = new Role
        {
            Key = vm.Key,
            Name = vm.Name,
            Description = vm.Description,
            AdGroup = vm.AdGroup,
            SortOrder = vm.SortOrder,
            IsSystem = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentUserService.GetDisplayName(),
            CreatedByWindows = _currentUserService.GetWindowsUserName()
        };

        await _roleRepository.AddAsync(role);
        TempData["SuccessMessage"] = $"Rolle \"{role.Name}\" wurde erstellt.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var role = await _roleRepository.GetByIdAsync(id);
        if (role == null)
            return NotFound();

        var vm = new RoleEditViewModel
        {
            Id = role.Id,
            Key = role.Key,
            Name = role.Name,
            Description = role.Description,
            AdGroup = role.AdGroup,
            SortOrder = role.SortOrder,
            IsSystem = role.IsSystem,
            UserCount = role.UserRoles.Count
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, RoleEditViewModel vm)
    {
        if (id != vm.Id)
            return NotFound();

        if (!ModelState.IsValid)
            return View(vm);

        var role = await _roleRepository.GetByIdAsync(id);
        if (role == null)
            return NotFound();

        role.Name = vm.Name;
        role.Description = vm.Description;
        role.AdGroup = vm.AdGroup;

        if (!role.IsSystem)
        {
            role.Key = vm.Key;
            role.SortOrder = vm.SortOrder;
        }

        role.ModifiedAt = DateTime.UtcNow;
        role.ModifiedBy = _currentUserService.GetDisplayName();
        role.ModifiedByWindows = _currentUserService.GetWindowsUserName();

        await _roleRepository.UpdateAsync(role);
        TempData["SuccessMessage"] = $"Rolle \"{role.Name}\" wurde gespeichert.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var role = await _roleRepository.GetByIdAsync(id);
        if (role == null)
            return NotFound();

        if (role.IsSystem)
        {
            TempData["WarningMessage"] = "Systemrollen können nicht gelöscht werden.";
            return RedirectToAction(nameof(Index));
        }

        await _roleRepository.DeleteAsync(role);
        TempData["SuccessMessage"] = $"Rolle \"{role.Name}\" wurde gelöscht.";
        return RedirectToAction(nameof(Index));
    }
}
```

- [ ] **Step 3: Create `Views/Roles/Index.cshtml`**

```html
@model List<Role>
@{
    ViewData["Title"] = "Rollen";
}

<div class="d-flex justify-content-between align-items-center mb-3">
    <h2 class="page-header mb-0">Rollen</h2>
    <a asp-action="Create" class="btn btn-primary">Neue Rolle anlegen</a>
</div>

<div class="table-responsive">
    <table class="table table-striped mb-0">
        <thead>
            <tr>
                <th>Name</th>
                <th>Schlüssel</th>
                <th>AD-Gruppe</th>
                <th>Benutzer</th>
                <th style="width: 100px;"></th>
            </tr>
        </thead>
        <tbody>
            @foreach (var role in Model)
            {
                <tr>
                    <td>
                        @role.Name
                        @if (role.IsSystem)
                        {
                            <span class="badge bg-secondary ms-1" title="Systemrolle">System</span>
                        }
                    </td>
                    <td><code>@role.Key</code></td>
                    <td>@role.AdGroup</td>
                    <td>@role.UserRoles.Count</td>
                    <td>
                        <a asp-action="Edit" asp-route-id="@role.Id" class="btn btn-sm btn-secondary">Bearbeiten</a>
                    </td>
                </tr>
            }
        </tbody>
    </table>
</div>
```

- [ ] **Step 4: Create `Views/Roles/Edit.cshtml`**

```html
@model RoleEditViewModel
@{
    ViewData["Title"] = "Rolle bearbeiten";
}

<h2 class="page-header">Rolle bearbeiten</h2>

<div class="row">
    <div class="col-md-6">
        <div class="card">
            <div class="card-body">
                <form asp-action="Edit" method="post">
                    <input type="hidden" asp-for="Id" />
                    <input type="hidden" asp-for="IsSystem" />
                    <div asp-validation-summary="ModelOnly" class="text-danger mb-3"></div>

                    <div class="mb-3">
                        <label asp-for="Key" class="form-label"></label>
                        @if (Model.IsSystem)
                        {
                            <input asp-for="Key" class="form-control" readonly />
                            <div class="form-text">Schlüssel von Systemrollen kann nicht geändert werden.</div>
                        }
                        else
                        {
                            <input asp-for="Key" class="form-control" />
                        }
                        <span asp-validation-for="Key" class="text-danger"></span>
                    </div>

                    <div class="mb-3">
                        <label asp-for="Name" class="form-label"></label>
                        <input asp-for="Name" class="form-control" autofocus />
                        <span asp-validation-for="Name" class="text-danger"></span>
                    </div>

                    <div class="mb-3">
                        <label asp-for="Description" class="form-label"></label>
                        <textarea asp-for="Description" class="form-control" rows="2"></textarea>
                    </div>

                    <div class="mb-3">
                        <label asp-for="AdGroup" class="form-label"></label>
                        <input asp-for="AdGroup" class="form-control" placeholder="z.B. WMS_Lager" />
                        <div class="form-text">SAMAccountName der AD-Gruppe (nur Gruppenname, kein DN). Mitglieder dieser Gruppe erhalten die Rolle automatisch.</div>
                    </div>

                    <div class="mb-3">
                        <label asp-for="SortOrder" class="form-label"></label>
                        @if (Model.IsSystem)
                        {
                            <input asp-for="SortOrder" class="form-control" readonly />
                        }
                        else
                        {
                            <input asp-for="SortOrder" class="form-control" type="number" />
                        }
                    </div>

                    @if (Model.UserCount > 0)
                    {
                        <div class="alert alert-info">
                            Diese Rolle ist @Model.UserCount Benutzer(n) zugewiesen.
                        </div>
                    }

                    <div class="d-flex gap-2">
                        <button type="submit" class="btn btn-primary">Speichern</button>
                        <a asp-action="Index" class="btn btn-outline-secondary">Abbrechen</a>
                    </div>
                </form>

                @if (!Model.IsSystem)
                {
                    <form asp-action="Delete" asp-route-id="@Model.Id" method="post" class="mt-3"
                          onsubmit="return confirm('Rolle wirklich löschen?');">
                        @Html.AntiForgeryToken()
                        <button type="submit" class="btn btn-outline-danger">Löschen</button>
                    </form>
                }
            </div>
        </div>
    </div>
</div>

@section Scripts {
    <partial name="_ValidationScriptsPartial" />
}
```

- [ ] **Step 5: Create `Views/Roles/Create.cshtml`**

```html
@model RoleEditViewModel
@{
    ViewData["Title"] = "Rolle anlegen";
}

<h2 class="page-header">Neue Rolle anlegen</h2>

<div class="row">
    <div class="col-md-6">
        <div class="card">
            <div class="card-body">
                <form asp-action="Create" method="post">
                    <div asp-validation-summary="ModelOnly" class="text-danger mb-3"></div>

                    <div class="mb-3">
                        <label asp-for="Key" class="form-label"></label>
                        <input asp-for="Key" class="form-control" placeholder="z.B. warehouse_supervisor" />
                        <div class="form-text">Technischer Schlüssel (eindeutig, keine Leerzeichen).</div>
                        <span asp-validation-for="Key" class="text-danger"></span>
                    </div>

                    <div class="mb-3">
                        <label asp-for="Name" class="form-label"></label>
                        <input asp-for="Name" class="form-control" autofocus />
                        <span asp-validation-for="Name" class="text-danger"></span>
                    </div>

                    <div class="mb-3">
                        <label asp-for="Description" class="form-label"></label>
                        <textarea asp-for="Description" class="form-control" rows="2"></textarea>
                    </div>

                    <div class="mb-3">
                        <label asp-for="AdGroup" class="form-label"></label>
                        <input asp-for="AdGroup" class="form-control" placeholder="z.B. WMS_Lager" />
                        <div class="form-text">SAMAccountName der AD-Gruppe (nur Gruppenname, kein DN). Mitglieder dieser Gruppe erhalten die Rolle automatisch.</div>
                    </div>

                    <div class="mb-3">
                        <label asp-for="SortOrder" class="form-label"></label>
                        <input asp-for="SortOrder" class="form-control" type="number" value="100" />
                    </div>

                    <div class="d-flex gap-2">
                        <button type="submit" class="btn btn-primary">Speichern</button>
                        <a asp-action="Index" class="btn btn-outline-secondary">Abbrechen</a>
                    </div>
                </form>
            </div>
        </div>
    </div>
</div>

@section Scripts {
    <partial name="_ValidationScriptsPartial" />
}
```

- [ ] **Step 6: Commit**

```bash
git add IdealAkeWms/Models/ViewModels/RoleEditViewModel.cs IdealAkeWms/Controllers/RolesController.cs IdealAkeWms/Views/Roles/ IdealAkeWms/Data/Repositories/RoleRepository.cs
git commit -m "feat: add RolesController with CRUD views for role management"
```

---

## Task 9: UserEditViewModel + UsersController Umbau

**Files:**
- Create: `IdealAkeWms/Models/ViewModels/UserEditViewModel.cs`
- Modify: `IdealAkeWms/Controllers/UsersController.cs`
- Modify: `IdealAkeWms/Views/Users/Edit.cshtml`
- Modify: `IdealAkeWms/Views/Users/Create.cshtml`
- Modify: `IdealAkeWms/Views/Users/Index.cshtml`

- [ ] **Step 1: Create `UserEditViewModel.cs`**

```csharp
using System.ComponentModel.DataAnnotations;
using IdealAkeWms.Models;

namespace IdealAkeWms.Models.ViewModels;

public class UserEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Name ist erforderlich")]
    [StringLength(200)]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(50)]
    [Display(Name = "Personalnummer")]
    public string? PersonalNumber { get; set; }

    [Display(Name = "Aktiv")]
    public bool IsActive { get; set; } = true;

    [StringLength(200)]
    [EmailAddress]
    [Display(Name = "E-Mail")]
    public string? Email { get; set; }

    [Display(Name = "Meldebestand-Benachrichtigung")]
    public bool NotifyOnReorderLevel { get; set; }

    [StringLength(100)]
    [Display(Name = "Standard-Filter Beschaffung")]
    public string? DefaultFilterBeschaffung { get; set; }

    [StringLength(100)]
    [Display(Name = "Standard-Filter Artikelgruppe")]
    public string? DefaultFilterArtikelgruppe { get; set; }

    [Display(Name = "Rekursive Suche bei aktiver Filterung")]
    public bool RecursiveFilterSearch { get; set; }

    // Role assignment
    public List<RoleCheckboxItem> AvailableRoles { get; set; } = new();
    public List<int> SelectedRoleIds { get; set; } = new();
    public List<string> AdGroupRoleNames { get; set; } = new();

    // For hidden fields on Edit
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string CreatedByWindows { get; set; } = string.Empty;
}

public class RoleCheckboxItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
}
```

- [ ] **Step 2: Rewrite `UsersController.cs`**

```csharp
using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;

namespace IdealAkeWms.Controllers;

[RequireMasterDataAccess]
public class UsersController : Controller
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPasswordService _passwordService;

    public UsersController(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        ICurrentUserService currentUserService,
        IPasswordService passwordService)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _currentUserService = currentUserService;
        _passwordService = passwordService;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _userRepository.GetAllWithRolesAsync();
        return View(users.OrderBy(u => u.Name).ToList());
    }

    public async Task<IActionResult> Create()
    {
        var vm = new UserEditViewModel { IsActive = true };
        await PopulateRolesAsync(vm, new List<int>());
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserEditViewModel vm, string? newPassword)
    {
        if (!ModelState.IsValid)
        {
            await PopulateRolesAsync(vm, vm.SelectedRoleIds);
            return View(vm);
        }

        var user = new User
        {
            Name = vm.Name,
            PersonalNumber = vm.PersonalNumber,
            IsActive = vm.IsActive,
            Email = vm.Email,
            NotifyOnReorderLevel = vm.NotifyOnReorderLevel,
            DefaultFilterBeschaffung = vm.DefaultFilterBeschaffung,
            DefaultFilterArtikelgruppe = vm.DefaultFilterArtikelgruppe,
            RecursiveFilterSearch = vm.RecursiveFilterSearch,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentUserService.GetDisplayName(),
            CreatedByWindows = _currentUserService.GetWindowsUserName()
        };

        if (!string.IsNullOrEmpty(newPassword))
            user.PasswordHash = _passwordService.HashPassword(newPassword);

        await _userRepository.AddAsync(user);

        if (vm.SelectedRoleIds.Any())
        {
            await _roleRepository.SetUserRolesAsync(
                user.Id,
                vm.SelectedRoleIds,
                _currentUserService.GetDisplayName(),
                _currentUserService.GetWindowsUserName());
        }

        TempData["SuccessMessage"] = $"Benutzer \"{user.Name}\" wurde erstellt.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
            return NotFound();

        var userRoleIds = await _roleRepository.GetRoleKeysByUserIdAsync(id);
        var allRoles = await _roleRepository.GetAllOrderedAsync();
        var selectedIds = allRoles
            .Where(r => userRoleIds.Contains(r.Key))
            .Select(r => r.Id)
            .ToList();

        var vm = new UserEditViewModel
        {
            Id = user.Id,
            Name = user.Name,
            PersonalNumber = user.PersonalNumber,
            IsActive = user.IsActive,
            Email = user.Email,
            NotifyOnReorderLevel = user.NotifyOnReorderLevel,
            DefaultFilterBeschaffung = user.DefaultFilterBeschaffung,
            DefaultFilterArtikelgruppe = user.DefaultFilterArtikelgruppe,
            RecursiveFilterSearch = user.RecursiveFilterSearch,
            CreatedAt = user.CreatedAt,
            CreatedBy = user.CreatedBy,
            CreatedByWindows = user.CreatedByWindows
        };

        await PopulateRolesAsync(vm, selectedIds);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UserEditViewModel vm, string? newPassword)
    {
        if (id != vm.Id)
            return NotFound();

        if (!ModelState.IsValid)
        {
            await PopulateRolesAsync(vm, vm.SelectedRoleIds);
            return View(vm);
        }

        var existing = await _userRepository.GetByIdAsync(id);
        if (existing == null)
            return NotFound();

        existing.Name = vm.Name;
        existing.PersonalNumber = vm.PersonalNumber;
        existing.IsActive = vm.IsActive;
        existing.Email = vm.Email;
        existing.NotifyOnReorderLevel = vm.NotifyOnReorderLevel;
        existing.DefaultFilterBeschaffung = vm.DefaultFilterBeschaffung;
        existing.DefaultFilterArtikelgruppe = vm.DefaultFilterArtikelgruppe;
        existing.RecursiveFilterSearch = vm.RecursiveFilterSearch;

        if (!string.IsNullOrEmpty(newPassword))
            existing.PasswordHash = _passwordService.HashPassword(newPassword);

        existing.ModifiedAt = DateTime.UtcNow;
        existing.ModifiedBy = _currentUserService.GetDisplayName();
        existing.ModifiedByWindows = _currentUserService.GetWindowsUserName();

        await _userRepository.UpdateAsync(existing);

        await _roleRepository.SetUserRolesAsync(
            id,
            vm.SelectedRoleIds,
            _currentUserService.GetDisplayName(),
            _currentUserService.GetWindowsUserName());

        TempData["SuccessMessage"] = $"Benutzer \"{existing.Name}\" wurde gespeichert.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateRolesAsync(UserEditViewModel vm, List<int> selectedIds)
    {
        var roles = await _roleRepository.GetAllOrderedAsync();
        vm.AvailableRoles = roles.Select(r => new RoleCheckboxItem
        {
            Id = r.Id,
            Name = r.Name,
            Key = r.Key,
            IsSelected = selectedIds.Contains(r.Id)
        }).ToList();
    }
}
```

**Note:** The controller uses `_userRepository.GetAllWithRolesAsync()` — this method needs to be added to `IUserRepository` / `UserRepository`. It should load Users with their UserRoles→Role included.

- [ ] **Step 3: Add `GetAllWithRolesAsync` to IUserRepository / UserRepository**

Add to `IUserRepository`:
```csharp
Task<List<User>> GetAllWithRolesAsync();
```

Add to `UserRepository`:
```csharp
public async Task<List<User>> GetAllWithRolesAsync()
{
    return await _context.Users
        .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
        .ToListAsync();
}
```

- [ ] **Step 4: Rewrite `Views/Users/Edit.cshtml`**

```html
@model UserEditViewModel
@{
    ViewData["Title"] = "Benutzer bearbeiten";
}

<h2 class="page-header">Benutzer bearbeiten</h2>

<div class="row">
    <div class="col-md-6">
        <div class="card">
            <div class="card-body">
                <form asp-action="Edit" method="post">
                    <input type="hidden" asp-for="Id" />
                    <input type="hidden" asp-for="CreatedAt" />
                    <input type="hidden" asp-for="CreatedBy" />
                    <input type="hidden" asp-for="CreatedByWindows" />
                    <div asp-validation-summary="ModelOnly" class="text-danger mb-3"></div>

                    <div class="mb-3">
                        <label asp-for="Name" class="form-label"></label>
                        <input asp-for="Name" class="form-control" autofocus />
                        <span asp-validation-for="Name" class="text-danger"></span>
                    </div>

                    <div class="mb-3">
                        <label asp-for="PersonalNumber" class="form-label"></label>
                        <input asp-for="PersonalNumber" class="form-control" />
                        <span asp-validation-for="PersonalNumber" class="text-danger"></span>
                    </div>

                    <div class="mb-3 form-check">
                        <input asp-for="IsActive" class="form-check-input" />
                        <label asp-for="IsActive" class="form-check-label"></label>
                    </div>

                    <hr />
                    <h6 class="text-muted">Rollen</h6>

                    @for (int i = 0; i < Model.AvailableRoles.Count; i++)
                    {
                        <div class="mb-2 form-check">
                            <input type="checkbox"
                                   name="SelectedRoleIds"
                                   value="@Model.AvailableRoles[i].Id"
                                   id="role_@Model.AvailableRoles[i].Id"
                                   class="form-check-input"
                                   @(Model.AvailableRoles[i].IsSelected ? "checked" : "") />
                            <label for="role_@Model.AvailableRoles[i].Id" class="form-check-label">
                                @Model.AvailableRoles[i].Name
                            </label>
                        </div>
                    }
                    @if (Model.AdGroupRoleNames.Any())
                    {
                        <div class="form-text mt-2">
                            Via AD-Gruppe zugewiesen:
                            @foreach (var name in Model.AdGroupRoleNames)
                            {
                                <span class="badge bg-info ms-1">@name</span>
                            }
                        </div>
                    }

                    <hr />
                    <h6 class="text-muted">Kontakt</h6>

                    <div class="mb-3">
                        <label asp-for="Email" class="form-label"></label>
                        <input asp-for="Email" class="form-control" type="email" placeholder="vorname.nachname@ake.at" />
                        <div class="form-text">Wird für automatische Benachrichtigungen (z.B. Meldebestand) verwendet.</div>
                        <span asp-validation-for="Email" class="text-danger"></span>
                    </div>

                    <div class="mb-3 form-check">
                        <input asp-for="NotifyOnReorderLevel" class="form-check-input" />
                        <label asp-for="NotifyOnReorderLevel" class="form-check-label"></label>
                        <div class="form-text">Benutzer erhält automatisch Meldebestand-Benachrichtigungen per E-Mail.</div>
                    </div>

                    <hr />
                    <h6 class="text-muted">Stücklisten-Filter Standardwerte</h6>

                    <div class="mb-3">
                        <label asp-for="DefaultFilterBeschaffung" class="form-label"></label>
                        <input asp-for="DefaultFilterBeschaffung" class="form-control" placeholder="z.B. Ja oder Nein" />
                        <div class="form-text">Standard-Filter in der Stücklisten-Ansicht.</div>
                    </div>

                    <div class="mb-3">
                        <label asp-for="DefaultFilterArtikelgruppe" class="form-label"></label>
                        <input asp-for="DefaultFilterArtikelgruppe" class="form-control" placeholder="z.B. 886 - Isoliergläser" />
                        <div class="form-text">Standard-Filter in der Stücklisten-Ansicht.</div>
                    </div>

                    <div class="mb-3 form-check">
                        <input asp-for="RecursiveFilterSearch" class="form-check-input" />
                        <label asp-for="RecursiveFilterSearch" class="form-check-label"></label>
                    </div>

                    <hr />

                    <div class="mb-3">
                        <label class="form-label">Passwort ändern</label>
                        <input type="password" name="newPassword" class="form-control" placeholder="Neues Passwort (leer = unverändert)" />
                        <div class="form-text">Nur ausfüllen wenn das Passwort geändert werden soll.</div>
                    </div>

                    <div class="d-flex gap-2">
                        <button type="submit" class="btn btn-primary">Speichern</button>
                        <a asp-action="Index" class="btn btn-outline-secondary">Abbrechen</a>
                    </div>
                </form>
            </div>
        </div>
    </div>
</div>

@section Scripts {
    <partial name="_ValidationScriptsPartial" />
}
```

- [ ] **Step 5: Rewrite `Views/Users/Create.cshtml`**

Same structure as Edit but without hidden fields for CreatedAt/CreatedBy/CreatedByWindows and without NotifyOnReorderLevel/RecursiveFilterSearch sections. Include the same role checkbox block.

- [ ] **Step 6: Rewrite `Views/Users/Index.cshtml`**

Replace the `IsAdmin` badge with role badges from `user.UserRoles`:

```html
@model List<User>
@{
    ViewData["Title"] = "Benutzer";
}

<div class="d-flex justify-content-between align-items-center mb-3">
    <h2 class="page-header mb-0">Benutzer</h2>
    <a asp-action="Create" class="btn btn-primary">Neuen Benutzer anlegen</a>
</div>

<div class="table-responsive">
    <table class="table table-striped mb-0">
        <thead>
            <tr>
                <th>Name</th>
                <th>Personalnummer</th>
                <th>Rollen</th>
                <th>E-Mail</th>
                <th>Status</th>
                <th>Erstellt am</th>
                <th style="width: 100px;"></th>
            </tr>
        </thead>
        <tbody>
            @foreach (var user in Model)
            {
                <tr>
                    <td>@user.Name</td>
                    <td>@user.PersonalNumber</td>
                    <td>
                        @foreach (var ur in user.UserRoles.OrderBy(ur => ur.Role.SortOrder))
                        {
                            var badgeClass = ur.Role.Key == "admin" ? "bg-danger" : "bg-primary";
                            <span class="badge @badgeClass me-1">@ur.Role.Name</span>
                        }
                    </td>
                    <td>@user.Email</td>
                    <td>
                        @if (user.IsActive)
                        {
                            <span class="badge badge-active">Aktiv</span>
                        }
                        else
                        {
                            <span class="badge badge-inactive">Inaktiv</span>
                        }
                    </td>
                    <td>@user.CreatedAt.ToString("dd.MM.yyyy HH:mm")</td>
                    <td>
                        <a asp-action="Edit" asp-route-id="@user.Id" class="btn btn-sm btn-secondary">Bearbeiten</a>
                    </td>
                </tr>
            }
            @if (!Model.Any())
            {
                <tr>
                    <td colspan="7" class="text-center text-muted py-4">Keine Benutzer vorhanden.</td>
                </tr>
            }
        </tbody>
    </table>
</div>
```

- [ ] **Step 7: Commit**

```bash
git add IdealAkeWms/Models/ViewModels/UserEditViewModel.cs IdealAkeWms/Controllers/UsersController.cs IdealAkeWms/Views/Users/ IdealAkeWms/Data/Repositories/IUserRepository.cs IdealAkeWms/Data/Repositories/UserRepository.cs
git commit -m "feat: rewrite UsersController with role checkbox assignment"
```

---

## Task 10: Navbar Update

**Files:**
- Modify: `IdealAkeWms/Views/Shared/_Layout.cshtml`

- [ ] **Step 1: Rewrite navbar permission checks**

Replace lines 29-78 (the `canPickAsync`/`canViewTracking` block) with role-based checks:

```html
@{
    var canAccessStock = await CurrentUserService.CanAccessStockAsync();
    var canPick = await CurrentUserService.CanPickAsync();
    var canViewTracking = await CurrentUserService.CanViewTrackingAsync();
    var teileverfolgungAktiv = (await AppSettings.GetValueAsync("TeileverfolgungAktiv"))?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
}
@if (canAccessStock || canPick)
{
    <li class="nav-item dropdown">
        <a class="nav-link dropdown-toggle" href="#" role="button" data-bs-toggle="dropdown" aria-expanded="false">
            Lagerbewegungen
        </a>
        <ul class="dropdown-menu">
            <li><a class="dropdown-item" asp-controller="StockMovements" asp-action="Inbound">Einbuchung</a></li>
            <li><a class="dropdown-item" asp-controller="StockMovements" asp-action="Outbound">Ausbuchung</a></li>
            @if (await CurrentUserService.CanTransferStockAsync())
            {
                <li><a class="dropdown-item" asp-controller="StockMovements" asp-action="OutboundAll">Lagerplatz ausbuchen</a></li>
                <li><a class="dropdown-item" asp-controller="StockMovements" asp-action="LocationTransfer">Lagerplatz umbuchen</a></li>
            }
            <li><a class="dropdown-item" asp-controller="StockMovements" asp-action="Transfer">Umbuchung</a></li>
            <li><hr class="dropdown-divider" style="border-color: rgba(255,255,255,0.2);" /></li>
            <li><a class="dropdown-item" asp-controller="StockMovements" asp-action="Index">Bewegungshistorie</a></li>
        </ul>
    </li>
    <li class="nav-item">
        <a class="nav-link" asp-controller="StockOverview" asp-action="Index">Bestände</a>
    </li>
}
@if (canPick || canViewTracking)
{
    <li class="nav-item">
        <a class="nav-link" asp-controller="ProductionOrders" asp-action="Index">Werkstattaufträge</a>
    </li>
}
@if (canPick)
{
    <li class="nav-item">
        <a class="nav-link" asp-controller="ProductionOrders" asp-action="Picking">Kommissionierung</a>
    </li>
}
@if (teileverfolgungAktiv && canViewTracking)
{
    <li class="nav-item dropdown">
        <a class="nav-link dropdown-toggle" href="#" role="button" data-bs-toggle="dropdown" aria-expanded="false">
            Teileverfolgung
        </a>
        <ul class="dropdown-menu">
            <li><a class="dropdown-item" asp-controller="Tracking" asp-action="Index">Rückmeldungen</a></li>
            <li><a class="dropdown-item" asp-controller="Tracking" asp-action="OseonIndex">OSEON Aufträge</a></li>
        </ul>
    </li>
}
```

Add "Rollen" link in the Stammdaten dropdown (after "Werkbänke", before the divider), inside the `HasMasterDataAccessAsync` block:
```html
<li><a class="dropdown-item" asp-controller="Roles" asp-action="Index">Rollen</a></li>
```

- [ ] **Step 2: Commit**

```bash
git add IdealAkeWms/Views/Shared/_Layout.cshtml
git commit -m "refactor: update navbar to use role-based permission checks"
```

---

## Task 11: Program.cs — Seed Roles + Update Admin Seeding

**Files:**
- Modify: `IdealAkeWms/Program.cs`

- [ ] **Step 1: Add role seeding after `db.Database.Migrate()`**

After the NAN storage location seeding (line 91), add role seeding:

```csharp
// Standard-Rollen
var defaultRoles = new (string Key, string Name, string? Description, int SortOrder)[]
{
    (RoleKeys.Admin, "Administrator", "Vollzugriff auf alle Funktionen", 0),
    (RoleKeys.MasterData, "Stammdaten", "Verwaltung von Benutzern, Arbeitsplätzen und Einstellungen", 10),
    (RoleKeys.Picking, "Kommissionierer", "Kommissionierung und vollständiger Lagerzugriff", 20),
    (RoleKeys.Stock, "Lager", "Einbuchung, Ausbuchung und Bestandsübersicht", 30),
    (RoleKeys.StockKeyUser, "Lager Keyuser", "Lager + Lagerplatz ausbuchen/umbuchen", 40),
    (RoleKeys.Tracking, "Teileverfolgung", "OSEON Teileverfolgung und Rückmeldungen", 50),
    (RoleKeys.Reporting, "Betriebsdaten (BDE)", "Arbeitsgänge stempeln und rückmelden", 60),
};
foreach (var (key, name, description, sortOrder) in defaultRoles)
{
    if (!db.Roles.Any(r => r.Key == key))
    {
        db.Roles.Add(new IdealAkeWms.Models.Role
        {
            Key = key,
            Name = name,
            Description = description,
            SortOrder = sortOrder,
            IsSystem = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "system",
            CreatedByWindows = "system"
        });
    }
}
db.SaveChanges();
```

- [ ] **Step 2: Update admin user seeding**

Replace the existing admin seeding block (lines 93-113) with:

```csharp
// Standard-Admin-Benutzer
var adminUser = db.Users.FirstOrDefault(u => u.Name == "admin");
if (adminUser == null)
{
    adminUser = new IdealAkeWms.Models.User
    {
        Name = "admin",
        IsActive = true,
        PasswordHash = passwordService.HashPassword(""),
        CreatedAt = DateTime.UtcNow,
        CreatedBy = "system",
        CreatedByWindows = "system"
    };
    db.Users.Add(adminUser);
    db.SaveChanges();
}
else if (adminUser.PasswordHash == null)
{
    adminUser.PasswordHash = passwordService.HashPassword("");
    db.SaveChanges();
}

// Admin-Rolle zuweisen
var adminRole = db.Roles.FirstOrDefault(r => r.Key == RoleKeys.Admin);
if (adminRole != null && !db.UserRoles.Any(ur => ur.UserId == adminUser.Id && ur.RoleId == adminRole.Id))
{
    db.UserRoles.Add(new IdealAkeWms.Models.UserRole
    {
        UserId = adminUser.Id,
        RoleId = adminRole.Id,
        CreatedAt = DateTime.UtcNow,
        CreatedBy = "system",
        CreatedByWindows = "system"
    });
    db.SaveChanges();
}
```

Add `using IdealAkeWms.Models;` at the top if not already present.

- [ ] **Step 3: Commit**

```bash
git add IdealAkeWms/Program.cs
git commit -m "feat: seed default roles and assign admin role on startup"
```

---

## Task 12: EF Migration (Phase 1 Only)

**Files:**
- New EF migration files (auto-generated)

- [ ] **Step 1: Create EF Migration**

Run from `IdealAkeWms/` directory:
```bash
dotnet ef migrations add AddRolesAndUserRoles
```

- [ ] **Step 2: Verify migration**

Review the generated migration file. It should:
- Create `Roles` table with all columns
- Create `UserRoles` table with FKs
- Add unique index on `Role.Key`
- Add unique index on `(UserRole.UserId, UserRole.RoleId)`
- **NOT** remove any columns from Users (old booleans stay — Phase 2 is Task 17)

If the migration includes column drops, the User.cs still has old booleans from Task 2 and EF should not generate drops. Verify this.

- [ ] **Step 3: Commit**

```bash
git add IdealAkeWms/Migrations/
git commit -m "db: add Phase 1 migration for Role/UserRole tables"
```

---

## Task 13: SQL Migration Script

**Files:**
- Create: `SQL/32_AddRoles.sql`
- Create: `SQL/33_RemoveOldPermissionColumns.sql`
- Modify: `SQL/00_FreshInstall.sql`

- [ ] **Step 1: Create `SQL/32_AddRoles.sql`**

```sql
-- Migration: Rollenkonzept — Phase 1: Tabellen + Datenmigration
-- Idempotent dank OBJECT_ID-Guards

BEGIN TRANSACTION;

-- 1. Roles-Tabelle
IF OBJECT_ID('dbo.Roles', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Roles] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Key] NVARCHAR(50) NOT NULL,
        [Name] NVARCHAR(100) NOT NULL,
        [Description] NVARCHAR(500) NULL,
        [AdGroup] NVARCHAR(200) NULL,
        [IsSystem] BIT NOT NULL DEFAULT 0,
        [SortOrder] INT NOT NULL DEFAULT 0,
        [CreatedAt] DATETIME2 NOT NULL,
        [CreatedBy] NVARCHAR(200) NOT NULL,
        [CreatedByWindows] NVARCHAR(200) NOT NULL,
        [ModifiedAt] DATETIME2 NULL,
        [ModifiedBy] NVARCHAR(200) NULL,
        [ModifiedByWindows] NVARCHAR(200) NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
    );
    CREATE UNIQUE INDEX [IX_Roles_Key] ON [dbo].[Roles] ([Key]);
END
GO

-- 2. Standardrollen seeden
IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Key] = 'admin')
BEGIN
    INSERT INTO [dbo].[Roles] ([Key], [Name], [Description], [IsSystem], [SortOrder], [CreatedAt], [CreatedBy], [CreatedByWindows])
    VALUES
        ('admin', 'Administrator', 'Vollzugriff auf alle Funktionen', 1, 0, GETUTCDATE(), 'system', 'system'),
        ('masterdata', 'Stammdaten', 'Verwaltung von Benutzern, Arbeitsplätzen und Einstellungen', 1, 10, GETUTCDATE(), 'system', 'system'),
        ('picking', 'Kommissionierer', 'Kommissionierung und vollständiger Lagerzugriff', 1, 20, GETUTCDATE(), 'system', 'system'),
        ('stock', 'Lager', 'Einbuchung, Ausbuchung und Bestandsübersicht', 1, 30, GETUTCDATE(), 'system', 'system'),
        ('stock_keyuser', 'Lager Keyuser', 'Lager + Lagerplatz ausbuchen/umbuchen', 1, 40, GETUTCDATE(), 'system', 'system'),
        ('tracking', 'Teileverfolgung', 'OSEON Teileverfolgung und Rückmeldungen', 1, 50, GETUTCDATE(), 'system', 'system'),
        ('reporting', 'Betriebsdaten (BDE)', 'Arbeitsgänge stempeln und rückmelden', 1, 60, GETUTCDATE(), 'system', 'system');
END
GO

-- 3. UserRoles-Tabelle
IF OBJECT_ID('dbo.UserRoles', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[UserRoles] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL,
        [RoleId] INT NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL,
        [CreatedBy] NVARCHAR(200) NOT NULL,
        [CreatedByWindows] NVARCHAR(200) NOT NULL,
        [ModifiedAt] DATETIME2 NULL,
        [ModifiedBy] NVARCHAR(200) NULL,
        [ModifiedByWindows] NVARCHAR(200) NULL,
        CONSTRAINT [PK_UserRoles] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserRoles_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserRoles_Roles] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles]([Id]) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX [IX_UserRoles_UserId_RoleId] ON [dbo].[UserRoles] ([UserId], [RoleId]);
END
GO

-- 4. Bestehende Boolean-Flags → Rollen migrieren
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'IsAdmin')
BEGIN
    -- IsAdmin → admin
    INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId], [CreatedAt], [CreatedBy], [CreatedByWindows])
    SELECT u.Id, r.Id, GETUTCDATE(), 'migration', 'migration'
    FROM [dbo].[Users] u
    CROSS JOIN [dbo].[Roles] r
    WHERE u.IsAdmin = 1 AND r.[Key] = 'admin'
    AND NOT EXISTS (SELECT 1 FROM [dbo].[UserRoles] ur WHERE ur.UserId = u.Id AND ur.RoleId = r.Id);

    -- HasMasterDataAccess → masterdata
    INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId], [CreatedAt], [CreatedBy], [CreatedByWindows])
    SELECT u.Id, r.Id, GETUTCDATE(), 'migration', 'migration'
    FROM [dbo].[Users] u
    CROSS JOIN [dbo].[Roles] r
    WHERE u.HasMasterDataAccess = 1 AND r.[Key] = 'masterdata'
    AND NOT EXISTS (SELECT 1 FROM [dbo].[UserRoles] ur WHERE ur.UserId = u.Id AND ur.RoleId = r.Id);

    -- CanPick → picking
    INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId], [CreatedAt], [CreatedBy], [CreatedByWindows])
    SELECT u.Id, r.Id, GETUTCDATE(), 'migration', 'migration'
    FROM [dbo].[Users] u
    CROSS JOIN [dbo].[Roles] r
    WHERE u.CanPick = 1 AND r.[Key] = 'picking'
    AND NOT EXISTS (SELECT 1 FROM [dbo].[UserRoles] ur WHERE ur.UserId = u.Id AND ur.RoleId = r.Id);

    -- CanViewTracking → tracking
    INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId], [CreatedAt], [CreatedBy], [CreatedByWindows])
    SELECT u.Id, r.Id, GETUTCDATE(), 'migration', 'migration'
    FROM [dbo].[Users] u
    CROSS JOIN [dbo].[Roles] r
    WHERE u.CanViewTracking = 1 AND r.[Key] = 'tracking'
    AND NOT EXISTS (SELECT 1 FROM [dbo].[UserRoles] ur WHERE ur.UserId = u.Id AND ur.RoleId = r.Id);

    -- CanReportOperations → reporting
    INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId], [CreatedAt], [CreatedBy], [CreatedByWindows])
    SELECT u.Id, r.Id, GETUTCDATE(), 'migration', 'migration'
    FROM [dbo].[Users] u
    CROSS JOIN [dbo].[Roles] r
    WHERE u.CanReportOperations = 1 AND r.[Key] = 'reporting'
    AND NOT EXISTS (SELECT 1 FROM [dbo].[UserRoles] ur WHERE ur.UserId = u.Id AND ur.RoleId = r.Id);
END
GO

-- 5. StammdatenADGruppe → Role.AdGroup migrieren
IF EXISTS (SELECT 1 FROM [dbo].[AppSettings] WHERE [Key] = 'StammdatenADGruppe')
BEGIN
    DECLARE @adGroup NVARCHAR(200);
    SELECT @adGroup = [Value] FROM [dbo].[AppSettings] WHERE [Key] = 'StammdatenADGruppe';
    IF @adGroup IS NOT NULL AND @adGroup <> ''
    BEGIN
        UPDATE [dbo].[Roles] SET [AdGroup] = @adGroup WHERE [Key] = 'masterdata';
    END
    DELETE FROM [dbo].[AppSettings] WHERE [Key] = 'StammdatenADGruppe';
END
GO

-- EF Migrations History
IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] LIKE '%_AddRolesAndUserRoles')
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (CONCAT(FORMAT(GETDATE(), 'yyyyMMddHHmmss'), '_AddRolesAndUserRoles'), '10.0.0');
END

COMMIT;
```

- [ ] **Step 2: Create `SQL/33_RemoveOldPermissionColumns.sql`**

```sql
-- Migration: Rollenkonzept — Phase 2: Alte Boolean-Spalten entfernen
-- NUR ausführen NACHDEM Phase 1 verifiziert wurde!

BEGIN TRANSACTION;

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'IsAdmin')
BEGIN
    ALTER TABLE [dbo].[Users] DROP COLUMN [IsAdmin];
    ALTER TABLE [dbo].[Users] DROP COLUMN [HasMasterDataAccess];
    ALTER TABLE [dbo].[Users] DROP COLUMN [CanPick];
    ALTER TABLE [dbo].[Users] DROP COLUMN [CanViewTracking];
    ALTER TABLE [dbo].[Users] DROP COLUMN [CanReportOperations];
END

-- EF Migrations History
IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] LIKE '%_RemoveOldPermissionColumns')
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (CONCAT(FORMAT(GETDATE(), 'yyyyMMddHHmmss'), '_RemoveOldPermissionColumns'), '10.0.0');
END

COMMIT;
```

- [ ] **Step 3: Update `SQL/00_FreshInstall.sql`**

Add Roles and UserRoles table creation (same DDL as above). Remove old boolean columns from Users CREATE TABLE. Add role seed data. Add admin UserRole assignment in seed data section.

- [ ] **Step 4: Commit**

```bash
git add SQL/32_AddRoles.sql SQL/33_RemoveOldPermissionColumns.sql SQL/00_FreshInstall.sql
git commit -m "db: add SQL migration scripts for role system (Phase 1 + Phase 2)"
```

---

## Task 14: Unit Tests

**Files:**
- Create: `IdealAkeWms.Tests/Services/CurrentUserServiceRoleTests.cs`
- Create: `IdealAkeWms.Tests/Filters/RoleFilterTests.cs`
- Modify/Remove: `IdealAkeWms.Tests/Services/CurrentUserServiceIsAdminTests.cs` (if exists — old boolean-based tests)

- [ ] **Step 1: Check for existing CurrentUserService tests and update/remove**

```bash
grep -r "IsAdmin\|CanPick\|HasMasterDataAccess\|CanViewTracking\|CanReportOperations" IdealAkeWms.Tests/ --include="*.cs" -l
```

Update or remove any tests that reference old boolean-based User properties. Replace with role-based test setup.

- [ ] **Step 2: Write CurrentUserService role tests**

Key test cases:
- User with `admin` role → `IsAdminAsync()` returns true
- User with `admin` role → `CanPickAsync()` returns true (wildcard)
- User with `picking` role → `CanPickAsync()` true, `IsAdminAsync()` false
- User with `stock` role → `CanAccessStockAsync()` true, `CanTransferStockAsync()` false
- User with `stock_keyuser` role → both stock methods true
- User with no roles → everything false
- `HasAnyRoleAsync("picking", "stock")` with user having "stock" → true
- Roles are cached per request (second call doesn't hit repository)

- [ ] **Step 3: Write filter tests**

Key test cases per filter:
- Filter grants access when user has matching role
- Filter redirects to AccessDenied when user lacks role
- Admin wildcard: every filter grants access for admin role

- [ ] **Step 4: Run all tests**

```bash
cd IdealAkeWms.Tests && dotnet test
```

- [ ] **Step 5: Fix any failures**

- [ ] **Step 6: Commit**

```bash
git add IdealAkeWms.Tests/
git commit -m "test: add role-based unit tests for CurrentUserService and filters"
```

---

## Task 15: Documentation Update

**Files:**
- Modify: `CLAUDE.md`
- Modify: `PROJECT_STATUS.md`
- Modify: `README.md`

- [ ] **Step 1: Update CLAUDE.md**

Update the following sections:
- **Zugriffsschutz**: Replace boolean-based descriptions with role-based
- **ICurrentUserService**: Add new methods (`HasRoleAsync`, `HasAnyRoleAsync`, `CanAccessStockAsync`, `CanTransferStockAsync`), update existing
- **AppSettings**: Remove `StammdatenADGruppe`, note it's now on `Role.AdGroup`
- **Bekannte Fallstricke**: Add "Rollen-Migration: Phase 2 erst nach Verifikation ausführen"
- **Wichtige Dateien**: Add `Models/Role.cs`, `Models/UserRole.cs`, `Models/RoleKeys.cs`, `Controllers/RolesController.cs`, `Data/Repositories/RoleRepository.cs`
- Add new section **Rollenkonzept** documenting the 7 roles, their keys, and AD-group mapping

- [ ] **Step 2: Update PROJECT_STATUS.md**

Add entry for the role-based access control implementation.

- [ ] **Step 3: Update README.md**

Document the new role-based access control feature.

- [ ] **Step 4: Commit**

```bash
git add CLAUDE.md PROJECT_STATUS.md README.md
git commit -m "docs: update documentation for role-based access control"
```

---

## Task 16: Build Verification & Remaining References

- [ ] **Step 1: Grep for all remaining references to old boolean properties**

```bash
grep -rn "\.IsAdmin\|\.CanPick\|\.HasMasterDataAccess\|\.CanViewTracking\|\.CanReportOperations" IdealAkeWms/ --include="*.cs" --include="*.cshtml"
```

Files that may still reference old booleans (not yet covered):
- `Controllers/HomeController.cs` — may use `ViewBag.CanPick` etc. → update to role-based checks
- `Views/Home/Index.cshtml` — may read ViewBag values → update accordingly
- `Controllers/AccountController.cs` — verify no direct flag reads (should use `ICurrentUserService`)
- `Controllers/ProductionOrdersController.cs` — verify delegates to `ICurrentUserService`

**NOTE:** Since we kept the boolean fields in Phase 1, these references will still compile. But they should be updated to use `ICurrentUserService` role methods for consistency. The old booleans are still populated but no longer authoritative.

- [ ] **Step 2: Verify _ViewImports.cshtml**

Check that `@using IdealAkeWms.Models` and `@using IdealAkeWms.Models.ViewModels` are registered. If not, add them.

- [ ] **Step 3: Build the solution**

```bash
cd IdealAkeWms && dotnet build
```

- [ ] **Step 4: Fix any build errors**

- [ ] **Step 5: Run all tests**

```bash
cd IdealAkeWms.Tests && dotnet test
```

- [ ] **Step 6: Fix any test failures**

- [ ] **Step 7: Final commit**

```bash
git add -A && git commit -m "fix: resolve remaining references and build issues for role system"
```

---

## Task 17: Phase 2 — Remove Old Boolean Columns (AFTER VERIFICATION)

**IMPORTANT:** Only execute this task after the role system has been verified in a running environment. This is the point of no return — after this, the old permission fields are gone.

**Files:**
- Modify: `IdealAkeWms/Models/User.cs`
- New EF migration

- [ ] **Step 1: Remove boolean fields from User.cs**

Remove these properties from `User.cs`:
- `HasMasterDataAccess`
- `IsAdmin`
- `CanPick`
- `CanViewTracking`
- `CanReportOperations`

- [ ] **Step 2: Remove ALL remaining code references to old booleans**

```bash
grep -rn "\.IsAdmin\|\.CanPick\|\.HasMasterDataAccess\|\.CanViewTracking\|\.CanReportOperations" IdealAkeWms/ --include="*.cs" --include="*.cshtml"
```

Fix every remaining reference. Common locations:
- `UsersController.cs` Edit POST — remove lines like `existing.IsAdmin = user.IsAdmin`
- `HomeController.cs` — replace ViewBag boolean checks with role checks
- `Program.cs` admin seeding — remove `HasMasterDataAccess = true`

- [ ] **Step 3: Create EF Migration**

```bash
cd IdealAkeWms && dotnet ef migrations add RemoveOldPermissionColumns
```

- [ ] **Step 4: Run SQL/33_RemoveOldPermissionColumns.sql on target database**

- [ ] **Step 5: Build + test**

```bash
cd IdealAkeWms && dotnet build && cd ../IdealAkeWms.Tests && dotnet test
```

- [ ] **Step 6: Commit**

```bash
git add -A && git commit -m "refactor: Phase 2 — remove old boolean permission columns from User"
```
