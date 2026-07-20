# Rolle „Lagerbestellung" + Artikelinfo-Kachel für `masterdata_read` — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Eine neue, eng abgegrenzte Rolle `lagerbestellung` (Zugriff nur auf `/WarehouseRequisitions` + `/MissingParts`) hinzufügen und die Artikelinfo-Dashboard-Kachel zusätzlich für `masterdata_read`-User sichtbar machen.

**Architecture:** Additive Rolle nach dem etablierten Muster (RoleKeys-Konstante → `ICurrentUserService.CanAccessLagerbestellungAsync()` → zwei neue Composite-Filter-Attribute → Seed-Migration 74 + SQL/74 + FreshInstall). Die bestehenden geteilten Filter (`RequirePickingOrStockAccess`, `RequireStockAccess`) bleiben unverändert; die zwei betroffenen Controller-Gruppen bekommen je einen neuen, um `lagerbestellung` erweiterten Filter. Teil B setzt im HomeController ein zusätzliches ViewBag-Flag und lagert die Artikelinfo-Kachel in ein Partial aus, das in zwei Sichtbarkeits-Blöcken verwendet wird.

**Tech Stack:** ASP.NET Core 10 MVC, EF Core 10 (SQL Server), xUnit + FluentAssertions + Moq + EF InMemory.

**Branch / Worktree:** `feature/windows-auth-ad-users` im Worktree `.claude/worktrees/missingparts-include-pd`. Spec: [docs/superpowers/specs/2026-06-19-lagerbestellung-rolle-artikelinfo-design.md](../specs/2026-06-19-lagerbestellung-rolle-artikelinfo-design.md).

**WICHTIG (Standing Constraints):** KEIN Merge nach `main` und KEIN Worktree-/Branch-Cleanup ohne ausdrückliche User-Freigabe. Dieser Plan endet mit grünem Build + grünen Tests auf dem Branch.

---

## File Structure

**Neu:**
- `IdealAkeWms/Filters/RequirePickingOrStockOrLagerbestellungAccessAttribute.cs` — Composite-Filter (admin/picking/stock/stock_keyuser/lagerbestellung) für WarehouseRequisitions.
- `IdealAkeWms/Filters/RequireStockOrLagerbestellungAccessAttribute.cs` — Composite-Filter (admin/stock/stock_keyuser/picking/lagerbestellung) für MissingParts.
- `IdealAkeWms/Migrations/<timestamp>_AddLagerbestellungRole.cs` — Seed-Migration (von `dotnet ef migrations add` generiert, Body manuell befüllt).
- `SQL/74_AddLagerbestellungRole.sql` — idempotenter Role-Seed + History-Insert.
- `IdealAkeWms/Views/Home/_ArtikelinfoTile.cshtml` — extrahiertes Kachel-Markup (kein State).

**Geändert:**
- `IdealAkeWms/Models/RoleKeys.cs` — `Lagerbestellung`-Konstante.
- `IdealAkeWms/Services/ICurrentUserService.cs` — `CanAccessLagerbestellungAsync()`.
- `IdealAkeWms/Services/CurrentUserService.cs` — Implementierung.
- `IdealAkeWms/Controllers/WarehouseRequisitionsController.cs` — Filter-Swap.
- `IdealAkeWms/Controllers/Api/WarehouseRequisitionsApiController.cs` — Filter-Swap.
- `IdealAkeWms/Controllers/MissingPartsController.cs` — Filter-Swap.
- `IdealAkeWms/Controllers/HomeController.cs` — ViewBag-Flag.
- `IdealAkeWms/Views/Home/Index.cshtml` — Partial-Verwendung + masterdata_read-Block.
- `IdealAkeWms/Views/Shared/_Layout.cshtml` — Menü-Sichtbarkeit.
- `SQL/00_FreshInstall.sql` — Role-Seed + History-Insert.
- `IdealAkeWms/Views/Users/RoleOverview.cshtml` — Doku-Zeile.
- `IdealAkeWms/Views/Help/Changelog.cshtml` — v1.23.0-Bullets.
- `CLAUDE.md`, `docs/TESTSZENARIEN.md`, `PROJECT_STATUS.md` — Doku.
- `IdealAkeWms.Tests/Services/CurrentUserServiceRoleTests.cs` — neue Can-Method-Tests.

---

## Task 1: Rolle-Konstante + `CanAccessLagerbestellungAsync` (TDD)

**Files:**
- Modify: `IdealAkeWms/Models/RoleKeys.cs`
- Modify: `IdealAkeWms/Services/ICurrentUserService.cs`
- Modify: `IdealAkeWms/Services/CurrentUserService.cs:90` (nach `CanAccessStockAsync`)
- Test: `IdealAkeWms.Tests/Services/CurrentUserServiceRoleTests.cs`

- [ ] **Step 1: Write the failing tests**

In `IdealAkeWms.Tests/Services/CurrentUserServiceRoleTests.cs` ans Ende der Klasse (vor der schließenden `}`) einfügen. Die Test-Helper `CreateService(sessionUserId, roleKeys)` existiert bereits in dieser Datei:

```csharp
    [Fact]
    public async Task CanAccessLagerbestellungAsync_WithLagerbestellungRole_ReturnsTrue()
    {
        var (service, _) = CreateService(sessionUserId: 1,
            roleKeys: new List<string> { RoleKeys.Lagerbestellung });

        (await service.CanAccessLagerbestellungAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task CanAccessLagerbestellungAsync_WithAdminRole_ReturnsTrue()
    {
        var (service, _) = CreateService(sessionUserId: 1,
            roleKeys: new List<string> { RoleKeys.Admin });

        (await service.CanAccessLagerbestellungAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task CanAccessLagerbestellungAsync_WithUnrelatedRole_ReturnsFalse()
    {
        var (service, _) = CreateService(sessionUserId: 1,
            roleKeys: new List<string> { RoleKeys.Stock });

        (await service.CanAccessLagerbestellungAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task LagerbestellungRole_DoesNotGrantPickingOrStock()
    {
        var (service, _) = CreateService(sessionUserId: 1,
            roleKeys: new List<string> { RoleKeys.Lagerbestellung });

        (await service.CanPickAsync()).Should().BeFalse();
        (await service.CanAccessStockAsync()).Should().BeFalse();
    }
```

- [ ] **Step 2: Run tests to verify they fail to compile**

Run: `dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "FullyQualifiedName~CanAccessLagerbestellungAsync"`
Expected: FAIL — Compile-Fehler `RoleKeys.Lagerbestellung` und `CanAccessLagerbestellungAsync` existieren nicht.

- [ ] **Step 3: Add the RoleKeys constant**

In `IdealAkeWms/Models/RoleKeys.cs` nach der Zeile `public const string Vorbau = "vorbau";` einfügen:

```csharp
    public const string Lagerbestellung = "lagerbestellung";
```

- [ ] **Step 4: Add the interface method**

In `IdealAkeWms/Services/ICurrentUserService.cs` nach `Task<bool> CanAccessStockAsync();` einfügen:

```csharp
    Task<bool> CanAccessLagerbestellungAsync();
```

- [ ] **Step 5: Add the implementation**

In `IdealAkeWms/Services/CurrentUserService.cs` direkt nach der `CanAccessStockAsync`-Methode (Zeile 90-91) einfügen:

```csharp
    public async Task<bool> CanAccessLagerbestellungAsync()
        => await HasAnyRoleAsync(RoleKeys.Admin, RoleKeys.Lagerbestellung);
```

- [ ] **Step 6: Run tests to verify they pass**

Run: `dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "FullyQualifiedName~CanAccessLagerbestellungAsync|FullyQualifiedName~LagerbestellungRole"`
Expected: PASS (4 Tests grün).

- [ ] **Step 7: Commit**

```bash
git add IdealAkeWms/Models/RoleKeys.cs IdealAkeWms/Services/ICurrentUserService.cs IdealAkeWms/Services/CurrentUserService.cs IdealAkeWms.Tests/Services/CurrentUserServiceRoleTests.cs
git commit -m "feat(roles): RoleKeys.Lagerbestellung + CanAccessLagerbestellungAsync"
```

---

## Task 2: Zwei Composite-Filter-Attribute

**Files:**
- Create: `IdealAkeWms/Filters/RequirePickingOrStockOrLagerbestellungAccessAttribute.cs`
- Create: `IdealAkeWms/Filters/RequireStockOrLagerbestellungAccessAttribute.cs`

Muster: bestehende `RequirePickingOrStockAccessAttribute` / `RequireStockAccessAttribute` (TypeFilterAttribute → `IAsyncActionFilter`, Deny = `RedirectToActionResult("AccessDenied", "Account", null)`).

- [ ] **Step 1: Create RequirePickingOrStockOrLagerbestellungAccessAttribute.cs**

```csharp
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace IdealAkeWms.Filters;

public class RequirePickingOrStockOrLagerbestellungAccessAttribute : TypeFilterAttribute
{
    public RequirePickingOrStockOrLagerbestellungAccessAttribute()
        : base(typeof(RequirePickingOrStockOrLagerbestellungAccessFilter)) { }
}

public class RequirePickingOrStockOrLagerbestellungAccessFilter : IAsyncActionFilter
{
    private readonly ICurrentUserService _currentUserService;

    public RequirePickingOrStockOrLagerbestellungAccessFilter(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!await _currentUserService.CanPickAsync()
            && !await _currentUserService.CanAccessStockAsync()
            && !await _currentUserService.CanAccessLagerbestellungAsync())
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
            return;
        }
        await next();
    }
}
```

- [ ] **Step 2: Create RequireStockOrLagerbestellungAccessAttribute.cs**

```csharp
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace IdealAkeWms.Filters;

public class RequireStockOrLagerbestellungAccessAttribute : TypeFilterAttribute
{
    public RequireStockOrLagerbestellungAccessAttribute()
        : base(typeof(RequireStockOrLagerbestellungAccessFilter)) { }
}

public class RequireStockOrLagerbestellungAccessFilter : IAsyncActionFilter
{
    private readonly ICurrentUserService _currentUserService;

    public RequireStockOrLagerbestellungAccessFilter(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!await _currentUserService.CanAccessStockAsync()
            && !await _currentUserService.CanAccessLagerbestellungAsync())
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
            return;
        }
        await next();
    }
}
```

- [ ] **Step 3: Build to verify compile**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj`
Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add IdealAkeWms/Filters/RequirePickingOrStockOrLagerbestellungAccessAttribute.cs IdealAkeWms/Filters/RequireStockOrLagerbestellungAccessAttribute.cs
git commit -m "feat(filters): zwei additive Lagerbestellung-Composite-Filter"
```

---

## Task 3: Filter an die drei Controller hängen

**Files:**
- Modify: `IdealAkeWms/Controllers/WarehouseRequisitionsController.cs:10`
- Modify: `IdealAkeWms/Controllers/Api/WarehouseRequisitionsApiController.cs` (Zeile mit `[RequirePickingOrStockAccess]`)
- Modify: `IdealAkeWms/Controllers/MissingPartsController.cs:10`

- [ ] **Step 1: WarehouseRequisitionsController**

Ersetze `[RequirePickingOrStockAccess]` über `public class WarehouseRequisitionsController` durch:

```csharp
[RequirePickingOrStockOrLagerbestellungAccess]
```

- [ ] **Step 2: WarehouseRequisitionsApiController**

Ersetze `[RequirePickingOrStockAccess]` über `public class WarehouseRequisitionsApiController` durch:

```csharp
[RequirePickingOrStockOrLagerbestellungAccess]
```

- [ ] **Step 3: MissingPartsController**

Ersetze `[RequireStockAccess]` über `public class MissingPartsController` durch:

```csharp
[RequireStockOrLagerbestellungAccess]
```

> **NICHT ändern:** `PartRequisitionsController`, `OrderRecipientGroupsController` (behalten `[RequirePickingOrStockAccess]`), `StockOverviewController`, `StockMovementsController`, `MissingPartsLagerController` (behalten ihre Filter). Sonst Scope-Creep.

- [ ] **Step 4: Build to verify compile**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj`
Expected: Build succeeded.

- [ ] **Step 5: Commit**

```bash
git add IdealAkeWms/Controllers/WarehouseRequisitionsController.cs "IdealAkeWms/Controllers/Api/WarehouseRequisitionsApiController.cs" IdealAkeWms/Controllers/MissingPartsController.cs
git commit -m "feat(access): Lagerbestellung-Rolle an WarehouseRequisitions + MissingParts"
```

---

## Task 4: Menü-Sichtbarkeit im Layout

**Files:**
- Modify: `IdealAkeWms/Views/Shared/_Layout.cshtml:48` (Var-Block) + `:123-141` (Bestellungen-Dropdown)

Ziel: `lagerbestellung`-only-User sehen den „Bestellungen"-Dropdown mit **nur** „Lagerbestellungen" + „Meine Fehlteile" — NICHT „Bedarfsmeldungen" (das bleibt picking/stock) und NICHT die „Lager: …"-Einträge (bleiben `canProcessLager`).

- [ ] **Step 1: Variable hinzufügen**

In `IdealAkeWms/Views/Shared/_Layout.cshtml` im `@{ … }`-Block (nach Zeile 33 `var canPick = await CurrentUserService.CanPickAsync();`) einfügen:

```csharp
                            var canAccessLagerbestellung = await CurrentUserService.CanAccessLagerbestellungAsync();
```

- [ ] **Step 2: Outer-Gate erweitern + Bedarfsmeldungen gaten**

Ersetze den Block Zeile 123-141:

```razor
                        @if (bestellungenAktiv && (canPick || canAccessStock))
                        {
                            <li class="nav-item dropdown">
                                <a class="nav-link dropdown-toggle @((ViewContext.RouteData.Values["controller"]?.ToString() is "PartRequisitions" or "WarehouseRequisitions" or "WarehousePicking") ? "active" : "")" href="#" role="button" data-bs-toggle="dropdown" aria-expanded="false">
                                    Bestellungen
                                </a>
                                <ul class="dropdown-menu">
                                    <li><a class="dropdown-item" asp-controller="PartRequisitions" asp-action="Index">Bedarfsmeldungen</a></li>
                                    <li><a class="dropdown-item" asp-controller="WarehouseRequisitions" asp-action="Index">Lagerbestellungen</a></li>
                                    <li><a class="dropdown-item" asp-controller="MissingParts" asp-action="Index">Meine Fehlteile</a></li>
                                    @if (canProcessLager)
                                    {
                                        <li><hr class="dropdown-divider" style="border-color: rgba(255,255,255,0.2);" /></li>
                                        <li><a class="dropdown-item" asp-controller="WarehousePicking" asp-action="Index">Lager: Eingehende Listen</a></li>
                                        <li><a class="dropdown-item" asp-controller="MissingPartsLager" asp-action="Index">Lager: Fehlteile</a></li>
                                    }
                                </ul>
                            </li>
                        }
```

durch:

```razor
                        @if (bestellungenAktiv && (canPick || canAccessStock || canAccessLagerbestellung))
                        {
                            <li class="nav-item dropdown">
                                <a class="nav-link dropdown-toggle @((ViewContext.RouteData.Values["controller"]?.ToString() is "PartRequisitions" or "WarehouseRequisitions" or "WarehousePicking") ? "active" : "")" href="#" role="button" data-bs-toggle="dropdown" aria-expanded="false">
                                    Bestellungen
                                </a>
                                <ul class="dropdown-menu">
                                    @if (canPick || canAccessStock)
                                    {
                                        <li><a class="dropdown-item" asp-controller="PartRequisitions" asp-action="Index">Bedarfsmeldungen</a></li>
                                    }
                                    <li><a class="dropdown-item" asp-controller="WarehouseRequisitions" asp-action="Index">Lagerbestellungen</a></li>
                                    <li><a class="dropdown-item" asp-controller="MissingParts" asp-action="Index">Meine Fehlteile</a></li>
                                    @if (canProcessLager)
                                    {
                                        <li><hr class="dropdown-divider" style="border-color: rgba(255,255,255,0.2);" /></li>
                                        <li><a class="dropdown-item" asp-controller="WarehousePicking" asp-action="Index">Lager: Eingehende Listen</a></li>
                                        <li><a class="dropdown-item" asp-controller="MissingPartsLager" asp-action="Index">Lager: Fehlteile</a></li>
                                    }
                                </ul>
                            </li>
                        }
```

- [ ] **Step 3: Build to verify Razor compiles**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj`
Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add IdealAkeWms/Views/Shared/_Layout.cshtml
git commit -m "feat(menu): Bestellungen-Dropdown fuer Lagerbestellung-Rolle (ohne Bedarfsmeldungen)"
```

---

## Task 5: Seed-Migration 74 + SQL/74 + FreshInstall

**Files:**
- Create: `IdealAkeWms/Migrations/<timestamp>_AddLagerbestellungRole.cs` (generiert)
- Create: `SQL/74_AddLagerbestellungRole.sql`
- Modify: `SQL/00_FreshInstall.sql` (Role-Seed-Block ~Zeile 1352 + History-Insert ~Zeile 2044)

Rolle: `Key='lagerbestellung'`, `Name='Lagerbestellungen'`, Description=„Lagerbestellungen erfassen + eigene Fehlteile verfolgen", `IsSystem=1`, `SortOrder=8`.

- [ ] **Step 1: Migration generieren**

Run: `dotnet ef migrations add AddLagerbestellungRole --project IdealAkeWms/IdealAkeWms.csproj`
Expected: Erzeugt `IdealAkeWms/Migrations/20260619HHMMSS_AddLagerbestellungRole.cs` (leeres Up/Down, da kein Model-Change). **Notiere die exakte MigrationId (Dateiname ohne `.cs`)** — sie wird in Step 3 + 4 gebraucht.

> Falls EF wegen ausstehender Model-Changes meckert (`PendingModelChangesWarning`): Es gibt KEINEN Model-Change in diesem Feature — die Warnung darf nicht auftreten. Tritt sie auf, vorher klären (nicht mit `--force` übergehen).

- [ ] **Step 2: Migration-Body befüllen**

Ersetze den generierten `Up`/`Down`-Body durch (idempotenter Seed):

```csharp
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM Roles WHERE [Key] = 'lagerbestellung')
BEGIN
    INSERT INTO Roles ([Key], [Name], [Description], [IsSystem], [SortOrder],
                       [CreatedAt], [CreatedBy], [CreatedByWindows])
    VALUES ('lagerbestellung', 'Lagerbestellungen',
            'Lagerbestellungen erfassen und eigene Fehlteile verfolgen (Zugriff nur auf Meine Lagerbestellungen + Meine Fehlteile).',
            1, 8,
            SYSDATETIME(), 'system', 'system')
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM Roles WHERE [Key] = 'lagerbestellung'");
        }
```

> Hinweis: `Role` ist `AuditableEntity` ohne `AdGroup`-Spalte mehr (in v1.23.0 gedroppt) — die `INSERT`-Spaltenliste oben ist korrekt und enthält KEIN `[AdGroup]`.

- [ ] **Step 3: SQL/74_AddLagerbestellungRole.sql erstellen**

`<MigrationId>` = exakte ID aus Step 1 (z. B. `20260619HHMMSS_AddLagerbestellungRole`):

```sql
-- ============================================================================
-- Migration 74: AddLagerbestellungRole
-- ============================================================================
-- Fuegt die Rolle 'lagerbestellung' hinzu (Zugriff nur auf Meine
-- Lagerbestellungen + Meine Fehlteile). Idempotent via IF NOT EXISTS.
-- ============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM Roles WHERE [Key] = 'lagerbestellung')
    BEGIN
        INSERT INTO Roles ([Key], [Name], [Description], [IsSystem], [SortOrder],
                           [CreatedAt], [CreatedBy], [CreatedByWindows])
        VALUES ('lagerbestellung', 'Lagerbestellungen',
                'Lagerbestellungen erfassen und eigene Fehlteile verfolgen (Zugriff nur auf Meine Lagerbestellungen + Meine Fehlteile).',
                1, 8,
                SYSDATETIME(), 'system', 'system');
        PRINT 'Rolle lagerbestellung angelegt.';
    END
    ELSE
    BEGIN
        PRINT 'Rolle lagerbestellung existiert bereits — uebersprungen.';
    END

    IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '<MigrationId>')
    BEGIN
        INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
        VALUES ('<MigrationId>', '10.0.2');
    END

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH
```

- [ ] **Step 4: FreshInstall — Role-Seed + History-Insert**

(a) Role-Seed: In `SQL/00_FreshInstall.sql` nach dem `vorbau`-Seed-Block (der mit `GO` endet, ~Zeile 1352) anfügen:

```sql
-- Rolle 'lagerbestellung' (nur Lagerbestellungen + eigene Fehlteile, v1.23.0)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Key] = 'lagerbestellung')
BEGIN
    INSERT INTO [dbo].[Roles] ([Key], [Name], [Description], [IsSystem], [SortOrder],
                               [CreatedAt], [CreatedBy], [CreatedByWindows])
    VALUES ('lagerbestellung', 'Lagerbestellungen',
            'Lagerbestellungen erfassen und eigene Fehlteile verfolgen (Zugriff nur auf Meine Lagerbestellungen + Meine Fehlteile).',
            1, 8,
            GETDATE(), 'system', 'system');
    PRINT 'Rolle lagerbestellung eingefuegt.';
END
GO
```

(b) History-Insert: In `SQL/00_FreshInstall.sql` nach der `20260618070606_AddWindowsUserNameDropAdGroup`-Zeile (~Zeile 2044) anfügen (`<MigrationId>` = ID aus Step 1):

```sql
IF NOT EXISTS (SELECT * FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '<MigrationId>')
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('<MigrationId>', '10.0.2');
```

- [ ] **Step 5: Build + Tests (Migration darf nichts brechen)**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj`
Expected: Build succeeded.

- [ ] **Step 6: Commit**

```bash
git add IdealAkeWms/Migrations/ SQL/74_AddLagerbestellungRole.sql SQL/00_FreshInstall.sql
git commit -m "feat(db): Migration 74 + SQL/74 + FreshInstall — Rolle lagerbestellung"
```

---

## Task 6: Artikelinfo-Kachel für `masterdata_read`

**Files:**
- Modify: `IdealAkeWms/Controllers/HomeController.cs:Index` (nach `ViewBag.HasMasterDataAccess = …`)
- Create: `IdealAkeWms/Views/Home/_ArtikelinfoTile.cshtml`
- Modify: `IdealAkeWms/Views/Home/Index.cshtml:8` (Header-Flag), `:102-117` (Kachel → Partial), `:119` (neuer Block)

- [ ] **Step 1: HomeController — ViewBag-Flag setzen**

In `IdealAkeWms/Controllers/HomeController.cs` in `Index()` direkt nach `ViewBag.HasMasterDataAccess = await _currentUserService.HasMasterDataAccessAsync();` einfügen:

```csharp
        ViewBag.HasMasterDataReadAccess = await _currentUserService.HasMasterDataReadAccessAsync();
```

- [ ] **Step 2: Partial erstellen**

`IdealAkeWms/Views/Home/_ArtikelinfoTile.cshtml` (exaktes Kachel-Markup aus Index.cshtml 102-117):

```razor
<div class="col-md-6 col-lg-3">
    <a asp-controller="Articles" asp-action="Info" class="text-decoration-none">
        <div class="card dashboard-card h-100" style="border-left-color: var(--ake-secondary);">
            <div class="card-body text-center">
                <h5 class="card-title">Artikelinfo</h5>
                <p class="card-text mb-0" style="color: var(--ake-secondary); font-size: 2rem;">
                    <svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" fill="currentColor" viewBox="0 0 16 16">
                        <path d="M1.5 1a.5.5 0 0 0-.5.5v3a.5.5 0 0 1-1 0v-3A1.5 1.5 0 0 1 1.5 0h3a.5.5 0 0 1 0 1zM11 .5a.5.5 0 0 1 .5-.5h3A1.5 1.5 0 0 1 16 1.5v3a.5.5 0 0 1-1 0v-3a.5.5 0 0 0-.5-.5h-3a.5.5 0 0 1-.5-.5M.5 11a.5.5 0 0 1 .5.5v3a.5.5 0 0 0 .5.5h3a.5.5 0 0 1 0 1h-3A1.5 1.5 0 0 1 0 14.5v-3a.5.5 0 0 1 .5-.5m15 0a.5.5 0 0 1 .5.5v3a1.5 1.5 0 0 1-1.5 1.5h-3a.5.5 0 0 1 0-1h3a.5.5 0 0 0 .5-.5v-3a.5.5 0 0 1 .5-.5"/>
                        <path d="M3 4.5a.5.5 0 0 1 1 0v7a.5.5 0 0 1-1 0zm2 0a.5.5 0 0 1 1 0v7a.5.5 0 0 1-1 0zm2 0a.5.5 0 0 1 1 0v7a.5.5 0 0 1-1 0zm2 0a.5.5 0 0 1 .5-.5h1a.5.5 0 0 1 .5.5v7a.5.5 0 0 1-.5.5h-1a.5.5 0 0 1-.5-.5zm3 0a.5.5 0 0 1 1 0v7a.5.5 0 0 1-1 0z"/>
                    </svg>
                </p>
                <small class="text-muted">QR-Code scannen → Details &amp; Bestand</small>
            </div>
        </div>
    </a>
</div>
```

- [ ] **Step 3: Header-Flag in Index.cshtml**

In `IdealAkeWms/Views/Home/Index.cshtml` im `@{ … }`-Block nach Zeile 8 (`bool hasMasterDataAccess = ViewBag.HasMasterDataAccess ?? false;`) einfügen:

```csharp
    bool hasMasterDataReadAccess = ViewBag.HasMasterDataReadAccess ?? false;
```

- [ ] **Step 4: Inline-Kachel im canPick-Block durch Partial ersetzen**

In `IdealAkeWms/Views/Home/Index.cshtml` den gesamten Kachel-Block Zeile 102-117 (das `<div class="col-md-6 col-lg-3"> … </div>` mit `asp-action="Info"`) ersetzen durch:

```razor
        @await Html.PartialAsync("_ArtikelinfoTile")
```

- [ ] **Step 5: masterdata_read-only Block einfügen**

In `IdealAkeWms/Views/Home/Index.cshtml` direkt **nach** dem schließenden `}` des `@if (canPick)`-Blocks (war Zeile 119, jetzt nach dem ersetzten Partial) einfügen:

```razor

@if (!canPick && hasMasterDataReadAccess)
{
    <h6 class="text-muted mb-2 mt-2">Artikel</h6>
    <div class="row g-4 mb-4">
        @await Html.PartialAsync("_ArtikelinfoTile")
    </div>
}
```

- [ ] **Step 6: Build to verify Razor compiles**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj`
Expected: Build succeeded.

- [ ] **Step 7: Commit**

```bash
git add IdealAkeWms/Controllers/HomeController.cs IdealAkeWms/Views/Home/_ArtikelinfoTile.cshtml IdealAkeWms/Views/Home/Index.cshtml
git commit -m "feat(dashboard): Artikelinfo-Kachel auch fuer masterdata_read"
```

---

## Task 7: Doku (RoleOverview, CLAUDE.md, Changelog, TESTSZENARIEN, PROJECT_STATUS)

**Files:**
- Modify: `IdealAkeWms/Views/Users/RoleOverview.cshtml`
- Modify: `CLAUDE.md`
- Modify: `IdealAkeWms/Views/Help/Changelog.cshtml`
- Modify: `docs/TESTSZENARIEN.md`
- Modify: `PROJECT_STATUS.md`

- [ ] **Step 1: RoleOverview.cshtml — neue Zeile**

In `IdealAkeWms/Views/Users/RoleOverview.cshtml` nach dem `<tr>`-Block der Rolle `stock_keyuser` (endet ~Zeile 101) eine neue `<tr>` einfügen (Struktur 1:1 wie die Nachbarn — vier `<td>`: `<code>key</code>`, Name, Beschreibung, Zugriff):

```razor
            <tr>
                <td><code>lagerbestellung</code></td>
                <td>Lagerbestellungen</td>
                <td>Lagerbestellungen erfassen + eigene Fehlteile verfolgen — eng abgegrenzt, ohne picking/stock.</td>
                <td>
                    Meine Lagerbestellungen (<code>/WarehouseRequisitions</code>) und Meine Fehlteile (<code>/MissingParts</code>).
                    Kein Zugriff auf Bedarfsmeldungen, Bestand, Bewegungen, Lager-Worklist.
                </td>
            </tr>
```

- [ ] **Step 2: CLAUDE.md — Zugriffsschutz + Rollenkonzept**

(a) In der **Zugriffsschutz**-Tabelle zwei neue Zeilen ergänzen (vor oder nach `[RequirePickingOrStockAccess]`):

```
| `[RequirePickingOrStockOrLagerbestellungAccess]` | admin, picking, stock, stock_keyuser ODER lagerbestellung | WarehouseRequisitionsController, WarehouseRequisitionsApiController (seit v1.23.0; ersetzt dort [RequirePickingOrStockAccess]) |
| `[RequireStockOrLagerbestellungAccess]` | admin, stock, stock_keyuser, picking ODER lagerbestellung | MissingPartsController (seit v1.23.0; ersetzt [RequireStockAccess]) |
```

Außerdem in der `[RequirePickingOrStockAccess]`-Zeile `WarehouseRequisitionsController, WarehouseRequisitionsApiController` entfernen (bleiben nur `PartRequisitionsController, OrderRecipientGroupsController`); in der `[RequireStockAccess]`-Zeile bleibt `StockMovementsController, StockOverviewController` (MissingParts war dort nicht gelistet — prüfen, nichts entfernen was nicht da ist).

(b) In der **Rollenkonzept**-Tabelle nach `vorbau` eine Zeile:

```
| `lagerbestellung` | Lagerbestellungen erfassen + eigene Fehlteile verfolgen (nur /WarehouseRequisitions + /MissingParts). Eng abgegrenzt, ohne picking/stock (seit v1.23.0) |
```

(c) Neuer Fallstrick-Eintrag (Abschnitt „Bekannte Fallstricke") — Artikelinfo-Kachel-Sichtbarkeit:

```
- **Artikelinfo-Dashboard-Kachel: canPick ODER masterdata_read (seit v1.23.0)**: Die Artikelinfo-Kachel (`Articles/Info`) liegt als Partial `Views/Home/_ArtikelinfoTile.cshtml` vor und wird an ZWEI Stellen gerendert: im `@if (canPick)`-Block (Kommissionier-Sektion, unverändert) UND in einem eigenen Mini-Block `@if (!canPick && hasMasterDataReadAccess)`. `masterdata_read`-User sehen NUR die Artikelinfo-Kachel (keine Lager-/Komm-Kacheln). HomeController setzt dafür `ViewBag.HasMasterDataReadAccess`. Der Zugriff auf `Articles/Info` selbst besteht für masterdata_read bereits über das Class-Level-`[RequireMasterDataReadAccess]`.
```

- [ ] **Step 3: Changelog.cshtml — v1.23.0-Bullets**

In `IdealAkeWms/Views/Help/Changelog.cshtml` in der bestehenden **v1.23.0**-Karte (`<ul>` der Windows-Anmeldung) am Ende der Liste vor `</ul>` ergänzen:

```razor
                    <li><strong>Neue Rolle „Lagerbestellungen":</strong> Eng abgegrenzte Rolle für
                        Benutzer, die nur Lagerbestellungen erfassen und ihre eigenen Fehlteile
                        verfolgen sollen — Zugriff auf „Meine Lagerbestellungen" und „Meine Fehlteile",
                        ohne volle Kommissionier-/Lager-Rechte.</li>
                    <li><strong>Artikelinfo am Dashboard für „Stammdaten ansehen":</strong> Benutzer mit
                        der Rolle <code>masterdata_read</code> sehen jetzt die Artikelinfo-Kachel am
                        Dashboard und können die Artikelinfo (QR-Scan → Details &amp; Bestand) öffnen.</li>
```

- [ ] **Step 4: TESTSZENARIEN.md — neues Kapitel**

In `docs/TESTSZENARIEN.md` ein neues Kapitel am Ende anfügen (höchstes vorhandenes Kapitel ist 40 → neues Kapitel **41**):

```markdown
## Kapitel 41: Rolle „Lagerbestellung" + Artikelinfo für Stammdaten-ansehen (v1.23.0)

**Vorbedingung:** `BestellungenAktiv=true`. Ein Benutzer `lb-test` mit NUR der Rolle
`lagerbestellung`. Ein Benutzer `md-test` mit NUR der Rolle `masterdata_read`.

### 41.1 Lagerbestellung-User: erlaubte Sichten
1. Als `lb-test` einloggen.
2. Menü „Bestellungen" öffnen.
   - **Erwartet:** Einträge „Lagerbestellungen" + „Meine Fehlteile" sichtbar.
   - **Erwartet:** „Bedarfsmeldungen" NICHT sichtbar; „Lager: …" NICHT sichtbar.
3. „Lagerbestellungen" öffnen → Liste lädt (kein AccessDenied).
4. „Meine Fehlteile" öffnen → Liste lädt (kein AccessDenied).

### 41.2 Lagerbestellung-User: verweigerte Sichten (Negativ)
1. Als `lb-test` direkt `/PartRequisitions`, `/StockOverview`, `/StockMovements`,
   `/MissingPartsLager` aufrufen.
   - **Erwartet:** jeweils Redirect auf `/Account/AccessDenied`.

### 41.3 Regression picking/stock
1. Als picking-User und als stock-User je „Lagerbestellungen" + „Meine Fehlteile" öffnen.
   - **Erwartet:** unverändert erreichbar; „Bedarfsmeldungen" weiterhin sichtbar.

### 41.4 Artikelinfo-Kachel für masterdata_read
1. Als `md-test` das Dashboard öffnen.
   - **Erwartet:** Sektion „Artikel" mit der Artikelinfo-Kachel sichtbar.
   - **Erwartet:** KEINE Lager-/Kommissionier-Kacheln.
2. Artikelinfo-Kachel klicken → `Articles/Info` öffnet (kein AccessDenied).

### 41.5 Regression picking-Dashboard
1. Als picking-User das Dashboard öffnen.
   - **Erwartet:** Artikelinfo-Kachel weiterhin in der Kommissionier-Sektion (kein Doppelt, keine fehlende Kachel).
```

- [ ] **Step 5: PROJECT_STATUS.md — Eintrag**

In `PROJECT_STATUS.md` unter dem aktuellen v1.23.0-Abschnitt einen Bullet ergänzen:

```markdown
- Rolle `lagerbestellung` (nur /WarehouseRequisitions + /MissingParts) + Artikelinfo-Dashboard-Kachel für `masterdata_read` (zwei additive Composite-Filter, Migration 74).
```

- [ ] **Step 6: Build + Commit**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj`
Expected: Build succeeded.

```bash
git add IdealAkeWms/Views/Users/RoleOverview.cshtml CLAUDE.md IdealAkeWms/Views/Help/Changelog.cshtml docs/TESTSZENARIEN.md PROJECT_STATUS.md
git commit -m "docs(v1.23.0): Rolle lagerbestellung + Artikelinfo-Kachel — RoleOverview/CLAUDE/Changelog/TESTSZENARIEN/PROJECT_STATUS"
```

---

## Task 8: Final-Check

**Files:** keine.

- [ ] **Step 1: Vollständiger Build**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj IdealAkeWms.Tests/IdealAkeWms.Tests.csproj`
Expected: Build succeeded, 0 Errors.

- [ ] **Step 2: Vollständige Web-Tests**

Run: `dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj`
Expected: Alle Tests grün (Baseline 738 + 4 neue = 742; tatsächliche Zahl prüfen, keine Regression).

- [ ] **Step 3: Migrations-Konsistenz prüfen**

Run: `dotnet ef migrations list --project IdealAkeWms/IdealAkeWms.csproj`
Expected: `AddLagerbestellungRole` als letzte (Pending) Migration sichtbar; `dotnet ef migrations has-pending-model-changes` meldet KEINE Model-Changes (reiner Seed).

- [ ] **Step 4: Manueller Sichtcheck (optional, falls lokale Instanz)**

Dashboard als admin → Artikelinfo-Kachel in Kommissionier-Sektion; Menü „Bestellungen" vollständig. (Voll abgedeckt durch TESTSZENARIEN Kapitel 41.)

> **STOP — keine Merge-/Cleanup-Aktion.** Der Plan endet hier. Merge nach `main` und Worktree-/Branch-Cleanup nur auf ausdrückliche User-Freigabe.

---

## Self-Review (vom Plan-Autor durchgeführt)

**Spec-Coverage:**
- Spec §3.1 Rolle → Task 1 (Konstante) + Task 5 (Seed). ✅
- Spec §3.2 zwei additive Filter → Task 2 + Task 3. ✅
- Spec §3.3 Menü → Task 4. ✅
- Spec §3.4 Seed/Migration/FreshInstall → Task 5. ✅
- Spec §4.1 HomeController ViewBag → Task 6 Step 1. ✅
- Spec §4.2 Partial + zwei Blöcke → Task 6 Steps 2-5. ✅
- Spec §5 Doku (RoleOverview/CLAUDE/Changelog/TESTSZENARIEN/PROJECT_STATUS) → Task 7. ✅
- Spec §6 Tests (Can-Method-Tests) → Task 1. ✅
- Spec §7 TESTSZENARIEN → Task 7 Step 4. ✅
- Spec §9 offener Punkt „dedizierte Can-Methode" → entschieden: ja, `CanAccessLagerbestellungAsync` (DRY für Layout + Filter), Task 1. ✅
- Spec §9 „Filter brauchen keine Program.cs-Registrierung" → bestätigt: TypeFilterAttribute wird per Reflection gefunden, keine DI-Registrierung nötig (Muster der bestehenden Filter). ✅

**Type-Konsistenz:** `CanAccessLagerbestellungAsync` identisch in Interface/Impl/Tests/Filtern. `RoleKeys.Lagerbestellung` = `"lagerbestellung"` konsistent in Konstante + SQL-Seeds. Filter-Klassennamen konsistent zwischen Datei (Task 2) und Controller-Attribut (Task 3).

**Placeholder-Scan:** Einziger bewusster Platzhalter `<MigrationId>` / `<N>` in Task 5/7 — kein Vagheits-Platzhalter, sondern ein zur Laufzeit von `dotnet ef migrations add` generierter Wert bzw. die nächste TESTSZENARIEN-Kapitelnummer; Beschaffung ist in den Steps explizit beschrieben (Projekt-Standard für jede Migration).
