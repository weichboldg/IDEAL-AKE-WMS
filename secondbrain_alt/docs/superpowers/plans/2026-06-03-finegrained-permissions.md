# Feingranulare Berechtigungen v1.20.0 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Read/Edit-Trennung in allen Stammdaten-Sichten via neuer Rolle `masterdata_read`, picker raus aus Lager-Worklist via neuer Filter `RequireLagerProcessingAccess`, neue hand-gepflegte Rollen-Uebersicht im Users-Bereich.

**Architecture:** Zwei neue Filter-Attribute (Class-Level Read + Action-Level Edit-Overrides als kanonisches Pattern), zwei neue View-Helper-Methoden in `CurrentUserService`, eine neue EF-Migration mit Role-Seed, hand-gepflegte Razor-Tabelle fuer Rollen-Uebersicht. Bestehende `[RequireMasterDataAccess]`-Attribute werden auf 10 Stammdaten-Controllern systematisch umgehaengt; `[RequireStockAccess]` an WarehousePicking + MissingPartsLager wird auf den neuen Lager-Filter ersetzt.

**Tech Stack:** ASP.NET Core 10.0 MVC, EF Core 10.0 (SQL Server), xUnit + FluentAssertions + Moq + EF InMemory, Bootstrap 5 / Razor.

**Spec:** [`secondbrain/docs/superpowers/specs/2026-06-03-finegrained-permissions-design.md`](../specs/2026-06-03-finegrained-permissions-design.md) (Commit `2899fea`)

**Worktree:** `.claude/worktrees/missingparts-include-pd`, Branch `bugfix/missingparts-include-pd` (piggyback auf v1.19.0-Arbeit). Alle Bash-Kommandos laufen mit `cwd` = Worktree-Root.

---

## Datei-Struktur (Uebersicht)

**Neu:**
- `IdealAkeWms/Filters/RequireMasterDataReadAccessAttribute.cs`
- `IdealAkeWms/Filters/RequireLagerProcessingAccessAttribute.cs`
- `IdealAkeWms/Migrations/<timestamp>_AddMasterDataReadRole.cs` (von `dotnet ef` generiert)
- `IdealAkeWms/Migrations/ApplicationDbContextModelSnapshot.cs` (von `dotnet ef` aktualisiert)
- `SQL/67_AddMasterDataReadRole.sql`
- `IdealAkeWms/Views/Users/RoleOverview.cshtml`
- `IdealAkeWms.Tests/Filters/RequireMasterDataReadAccessAttributeTests.cs`
- `IdealAkeWms.Tests/Filters/RequireLagerProcessingAccessAttributeTests.cs`

**Modifiziert:**
- `IdealAkeWms/Models/RoleKeys.cs` (+1 Konstante)
- `IdealAkeWms/Services/ICurrentUserService.cs` (+2 Methoden-Signaturen)
- `IdealAkeWms/Services/CurrentUserService.cs` (+2 Implementierungen)
- 10 Stammdaten-Controller: `Users`, `Roles`, `Workstations`, `Settings`, `ProductionWorkplaces`, `OrderRecipients`, `ArticleCategories`, `ArticleAttributes`, `BdeShiftCalendar`, `SyncLog`
- `IdealAkeWms/Controllers/UsersController.cs` zusaetzlich +1 Action (`RoleOverview`)
- `IdealAkeWms/Controllers/WarehousePickingController.cs` (Filter-Swap)
- `IdealAkeWms/Controllers/MissingPartsLagerController.cs` (Filter-Swap)
- 10 Index-Views (Edit-Buttons hide) + Settings-Form (disabled+Banner)
- `IdealAkeWms/Views/Shared/_Layout.cshtml` (Stammdaten- und Lager-Block)
- `IdealAkeWms.Tests/Services/CurrentUserServiceTests.cs` (+2 Test-Gruppen)
- `IdealAkeWms.Tests/Controllers/UsersControllerTests.cs` (+1 RoleOverview-Test)
- `SQL/00_FreshInstall.sql` (INSERT + MigrationsHistory)
- `IdealAkeWms/AppVersion.cs` und `IDEALAKEWMSService/AppVersion.cs`
- `IdealAkeWms/Views/Help/Changelog.cshtml`
- `PROJECT_STATUS.md`, `CLAUDE.md`, `docs/TESTSZENARIEN.md`

---

## Task 0: Pre-Flight Baseline

**Files:** keine — nur Verifikation.

- [ ] **Step 1: Verifiziere dass Worktree sauber ist**

```bash
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd status
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd log --oneline -5
```

Expected: Working tree clean, HEAD ist `2899fea docs(spec): v1.20.0 Berechtigungen Spec — Self-Review-Fixes`

- [ ] **Step 2: Build muss gruen sein**

```bash
cd C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd
dotnet build IdealAkeWms.slnx 2>&1 | tail -3
```

Expected: `0 Fehler`

- [ ] **Step 3: Tests muessen gruen sein**

```bash
cd C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd
dotnet test IdealAkeWms.slnx --no-build 2>&1 | tail -4
```

Expected: `Bestanden!` mit `643` (Web) + `99` (Service) Tests gruen.

- [ ] **Step 4: SQL-Migration-Nummer pruefen**

```bash
ls C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd/SQL/ | grep -E "^[0-9]" | sort -n
```

Expected: SQL/65 + SQL/66 sind die letzten. SQL/67 ist frei.

---

## Task 1: RoleKeys + Filter-Attribute (TDD)

**Files:**
- Modify: `IdealAkeWms/Models/RoleKeys.cs`
- Create: `IdealAkeWms/Filters/RequireMasterDataReadAccessAttribute.cs`
- Create: `IdealAkeWms/Filters/RequireLagerProcessingAccessAttribute.cs`
- Create: `IdealAkeWms.Tests/Filters/RequireMasterDataReadAccessAttributeTests.cs`
- Create: `IdealAkeWms.Tests/Filters/RequireLagerProcessingAccessAttributeTests.cs`

- [ ] **Step 1: RoleKeys-Konstante hinzufuegen**

Modify `IdealAkeWms/Models/RoleKeys.cs` — fuege nach `MasterData` die neue Konstante hinzu:

```csharp
public const string MasterData = "masterdata";
public const string MasterDataRead = "masterdata_read";
public const string Picking = "picking";
```

- [ ] **Step 2: Reference-Filter lesen**

```bash
cat C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd/IdealAkeWms/Filters/RequireMasterDataAccessAttribute.cs
```

Vorlage fuer beide neuen Filter. Notiere das Pattern: erbt von `RequireRoleAccessAttribute`, gibt allowed Roles als String-Array zurueck.

- [ ] **Step 3: Test fuer RequireMasterDataReadAccess schreiben (failing)**

Create `IdealAkeWms.Tests/Filters/RequireMasterDataReadAccessAttributeTests.cs`:

```csharp
using FluentAssertions;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using Xunit;

namespace IdealAkeWms.Tests.Filters;

public class RequireMasterDataReadAccessAttributeTests
{
    [Fact]
    public void AllowedRoles_ContainsAdmin()
    {
        var attr = new RequireMasterDataReadAccessAttribute();
        attr.AllowedRoles.Should().Contain(RoleKeys.Admin);
    }

    [Fact]
    public void AllowedRoles_ContainsMasterDataRead()
    {
        var attr = new RequireMasterDataReadAccessAttribute();
        attr.AllowedRoles.Should().Contain(RoleKeys.MasterDataRead);
    }

    [Fact]
    public void AllowedRoles_ContainsMasterData_EditImpliesRead()
    {
        var attr = new RequireMasterDataReadAccessAttribute();
        attr.AllowedRoles.Should().Contain(RoleKeys.MasterData);
    }

    [Fact]
    public void AllowedRoles_DoesNotContainPicking()
    {
        var attr = new RequireMasterDataReadAccessAttribute();
        attr.AllowedRoles.Should().NotContain(RoleKeys.Picking);
    }

    [Fact]
    public void AllowedRoles_ExactlyThreeEntries()
    {
        var attr = new RequireMasterDataReadAccessAttribute();
        attr.AllowedRoles.Should().HaveCount(3);
    }
}
```

- [ ] **Step 4: Test laufen lassen (rot, weil Attribut fehlt)**

```bash
cd C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd
dotnet build IdealAkeWms.slnx 2>&1 | grep -E "Fehler|error"
```

Expected: Build-Fehler `RequireMasterDataReadAccessAttribute` not found.

- [ ] **Step 5: RequireMasterDataReadAccessAttribute implementieren**

Create `IdealAkeWms/Filters/RequireMasterDataReadAccessAttribute.cs`:

```csharp
using IdealAkeWms.Models;

namespace IdealAkeWms.Filters;

/// <summary>
/// Erfordert Read-Zugriff auf Stammdaten (Listing-Sichten). Edit-Rolle
/// `masterdata` impliziert Read. Wird typisch class-level appliziert,
/// Action-Level [RequireMasterDataAccess] verschaerft fuer Edits.
/// </summary>
public class RequireMasterDataReadAccessAttribute : RequireRoleAccessAttribute
{
    public RequireMasterDataReadAccessAttribute()
        : base(RoleKeys.Admin, RoleKeys.MasterDataRead, RoleKeys.MasterData)
    {
    }
}
```

- [ ] **Step 6: Tests gruen**

```bash
cd C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd
dotnet test IdealAkeWms.slnx --filter "FullyQualifiedName~RequireMasterDataReadAccess" 2>&1 | tail -3
```

Expected: `Bestanden! 5/5`

- [ ] **Step 7: Test fuer RequireLagerProcessingAccess schreiben (failing)**

Create `IdealAkeWms.Tests/Filters/RequireLagerProcessingAccessAttributeTests.cs`:

```csharp
using FluentAssertions;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using Xunit;

namespace IdealAkeWms.Tests.Filters;

public class RequireLagerProcessingAccessAttributeTests
{
    [Fact]
    public void AllowedRoles_ContainsAdmin()
    {
        var attr = new RequireLagerProcessingAccessAttribute();
        attr.AllowedRoles.Should().Contain(RoleKeys.Admin);
    }

    [Fact]
    public void AllowedRoles_ContainsStock()
    {
        var attr = new RequireLagerProcessingAccessAttribute();
        attr.AllowedRoles.Should().Contain(RoleKeys.Stock);
    }

    [Fact]
    public void AllowedRoles_ContainsStockKeyUser()
    {
        var attr = new RequireLagerProcessingAccessAttribute();
        attr.AllowedRoles.Should().Contain(RoleKeys.StockKeyUser);
    }

    [Fact]
    public void AllowedRoles_DoesNotContainPicking_KeyAssertion()
    {
        var attr = new RequireLagerProcessingAccessAttribute();
        attr.AllowedRoles.Should().NotContain(RoleKeys.Picking,
            "picker arbeiten an der Werkbank, nicht im Lager — sie duerfen Lager-Worklists NICHT sehen");
    }

    [Fact]
    public void AllowedRoles_ExactlyThreeEntries()
    {
        var attr = new RequireLagerProcessingAccessAttribute();
        attr.AllowedRoles.Should().HaveCount(3);
    }
}
```

- [ ] **Step 8: RequireLagerProcessingAccessAttribute implementieren**

Create `IdealAkeWms/Filters/RequireLagerProcessingAccessAttribute.cs`:

```csharp
using IdealAkeWms.Models;

namespace IdealAkeWms.Filters;

/// <summary>
/// Erfordert Lager-Mitarbeiter-Zugriff fuer Worklist-Sichten (eingehende
/// Lagerbestellungen, Lager-Fehlteile). Bewusst OHNE `picking` — picker
/// arbeiten an der Werkbank, nicht im Lager.
/// </summary>
public class RequireLagerProcessingAccessAttribute : RequireRoleAccessAttribute
{
    public RequireLagerProcessingAccessAttribute()
        : base(RoleKeys.Admin, RoleKeys.Stock, RoleKeys.StockKeyUser)
    {
    }
}
```

- [ ] **Step 9: Tests gruen**

```bash
cd C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd
dotnet test IdealAkeWms.slnx --filter "FullyQualifiedName~RequireLagerProcessingAccess|FullyQualifiedName~RequireMasterDataReadAccess" 2>&1 | tail -3
```

Expected: `Bestanden! 10/10`

- [ ] **Step 10: Commit**

```bash
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd add IdealAkeWms/Models/RoleKeys.cs IdealAkeWms/Filters/RequireMasterDataReadAccessAttribute.cs IdealAkeWms/Filters/RequireLagerProcessingAccessAttribute.cs IdealAkeWms.Tests/Filters/RequireMasterDataReadAccessAttributeTests.cs IdealAkeWms.Tests/Filters/RequireLagerProcessingAccessAttributeTests.cs
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd commit -m "feat(auth): MasterDataRead+LagerProcessing Filter + RoleKeys

Zwei neue Filter-Attribute mit TDD:
- RequireMasterDataReadAccess: admin/masterdata_read/masterdata
- RequireLagerProcessingAccess: admin/stock/stock_keyuser (KEIN picker)
RoleKeys.MasterDataRead-Konstante."
```

---

## Task 2: CurrentUserService-Helper-Methoden (TDD)

**Files:**
- Modify: `IdealAkeWms/Services/ICurrentUserService.cs`
- Modify: `IdealAkeWms/Services/CurrentUserService.cs`
- Modify: `IdealAkeWms.Tests/Services/CurrentUserServiceTests.cs`

- [ ] **Step 1: ICurrentUserService um 2 Signaturen erweitern**

Modify `IdealAkeWms/Services/ICurrentUserService.cs`. Suche die Zeile mit `Task<bool> HasMasterDataAccessAsync();` und ergaenze direkt darunter:

```csharp
Task<bool> HasMasterDataAccessAsync();
Task<bool> HasMasterDataReadAccessAsync();
```

Suche `Task<bool> CanAccessStockAsync();` und ergaenze direkt darunter:

```csharp
Task<bool> CanAccessStockAsync();
Task<bool> CanProcessLagerAsync();
```

- [ ] **Step 2: Tests fuer HasMasterDataReadAccessAsync schreiben (failing)**

In `IdealAkeWms.Tests/Services/CurrentUserServiceTests.cs` ergaenze am Ende der Klasse:

```csharp
[Theory]
[InlineData(new[] { "admin" }, true)]
[InlineData(new[] { "masterdata" }, true)]
[InlineData(new[] { "masterdata_read" }, true)]
[InlineData(new[] { "picking" }, false)]
[InlineData(new[] { "stock" }, false)]
[InlineData(new string[] { }, false)]
public async Task HasMasterDataReadAccessAsync_ReturnsExpected(string[] roles, bool expected)
{
    var svc = BuildSvcWithUserRoles(roles);
    (await svc.HasMasterDataReadAccessAsync()).Should().Be(expected);
}

[Theory]
[InlineData(new[] { "admin" }, true)]
[InlineData(new[] { "stock" }, true)]
[InlineData(new[] { "stock_keyuser" }, true)]
[InlineData(new[] { "picking" }, false)]
[InlineData(new[] { "masterdata" }, false)]
[InlineData(new string[] { }, false)]
public async Task CanProcessLagerAsync_ReturnsExpected(string[] roles, bool expected)
{
    var svc = BuildSvcWithUserRoles(roles);
    (await svc.CanProcessLagerAsync()).Should().Be(expected);
}
```

**Hinweis**: `BuildSvcWithUserRoles` ist ein Helper der bereits in `CurrentUserServiceTests.cs` existiert — siehe Verwendung an bestehenden `[Theory]`-Tests fuer `CanAccessStockAsync` etc. (Falls die Methode anders heisst, das bestehende Pattern uebernehmen.)

- [ ] **Step 3: Tests laufen lassen (Build-Fehler — Methoden fehlen)**

```bash
cd C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd
dotnet build IdealAkeWms.slnx 2>&1 | grep -E "Fehler|error"
```

Expected: `HasMasterDataReadAccessAsync` und `CanProcessLagerAsync` not found.

- [ ] **Step 4: CurrentUserService um 2 Methoden erweitern**

In `IdealAkeWms/Services/CurrentUserService.cs` ergaenze nach `HasMasterDataAccessAsync()`:

```csharp
public async Task<bool> HasMasterDataAccessAsync()
    => await HasAnyRoleAsync(RoleKeys.Admin, RoleKeys.MasterData);

public async Task<bool> HasMasterDataReadAccessAsync()
    => await HasAnyRoleAsync(RoleKeys.Admin, RoleKeys.MasterDataRead, RoleKeys.MasterData);
```

Und nach `CanAccessStockAsync()`:

```csharp
public async Task<bool> CanAccessStockAsync()
    => await HasAnyRoleAsync(RoleKeys.Admin, RoleKeys.Stock, RoleKeys.StockKeyUser, RoleKeys.Picking);

public async Task<bool> CanProcessLagerAsync()
    => await HasAnyRoleAsync(RoleKeys.Admin, RoleKeys.Stock, RoleKeys.StockKeyUser);
```

- [ ] **Step 5: Tests gruen**

```bash
cd C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd
dotnet test IdealAkeWms.slnx --filter "FullyQualifiedName~CurrentUserService" 2>&1 | tail -3
```

Expected: alle (mind. 12 neue + bestehende) gruen.

- [ ] **Step 6: Commit**

```bash
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd add IdealAkeWms/Services/ICurrentUserService.cs IdealAkeWms/Services/CurrentUserService.cs IdealAkeWms.Tests/Services/CurrentUserServiceTests.cs
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd commit -m "feat(auth): CurrentUserService HasMasterDataReadAccessAsync + CanProcessLagerAsync

Zwei neue View-Helper analog zu HasMasterDataAccessAsync/CanAccessStockAsync.
HasMasterDataReadAccessAsync inkludiert die masterdata-Edit-Rolle implizit.
CanProcessLagerAsync schliesst picking explizit aus."
```

---

## Task 3: EF Migration + SQL/67 + FreshInstall

**Files:**
- Create: `IdealAkeWms/Migrations/<timestamp>_AddMasterDataReadRole.cs` (von EF generiert)
- Modify: `IdealAkeWms/Migrations/ApplicationDbContextModelSnapshot.cs` (von EF aktualisiert)
- Create: `SQL/67_AddMasterDataReadRole.sql`
- Modify: `SQL/00_FreshInstall.sql`

- [ ] **Step 1: EF Migration generieren**

```bash
cd C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd
dotnet ef migrations add AddMasterDataReadRole --project IdealAkeWms --startup-project IdealAkeWms 2>&1 | tail -5
```

Expected: `Done.` oder vergleichbare Erfolg-Meldung. Eine neue Datei in `IdealAkeWms/Migrations/` mit Namen `<timestamp>_AddMasterDataReadRole.cs`.

**Wenn EF-Tool fehlschlaegt** (siehe DesignTimeDbContextFactory aus v1.19.0-Task-1) — Migration manuell erstellen analog zur letzten Migration `20260602133102_AddNoteEinkaufToWarehouseRequisitionItems.cs`. Filename-Format: `YYYYMMDDHHMMSS_AddMasterDataReadRole.cs`.

- [ ] **Step 2: Migration-Up-Methode fuellen**

Die EF-Migration startet leer (kein Model-Schema-Change). Fuelle `Up()` mit dem Role-Seed-INSERT:

```csharp
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddMasterDataReadRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM Roles WHERE [Key] = 'masterdata_read')
BEGIN
    INSERT INTO Roles ([Key], [Name], [Description], [AdGroup], [IsSystem], [SortOrder],
                       [CreatedAt], [CreatedBy], [CreatedByWindows])
    VALUES ('masterdata_read', 'Stammdaten ansehen',
            'Nur-Lesen-Zugriff auf alle Stammdaten-Sichten (Benutzer, Rollen, Arbeitsplaetze, Einstellungen, Werkbaenke, Empfaenger, Artikelkategorien/-attribute, Schichtkalender, Aktivitaets-Protokoll).',
            NULL, 1, 5,
            SYSDATETIME(), 'system', 'system')
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM Roles WHERE [Key] = 'masterdata_read'");
        }
    }
}
```

- [ ] **Step 3: SQL/67_AddMasterDataReadRole.sql erstellen (idempotent)**

Create `SQL/67_AddMasterDataReadRole.sql`:

```sql
-- ============================================================================
-- Migration 67: AddMasterDataReadRole
-- ============================================================================
-- Fuegt eine neue Rolle 'masterdata_read' hinzu (Nur-Lesen-Zugriff auf
-- Stammdaten-Sichten). Idempotent via IF NOT EXISTS.
-- ============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM Roles WHERE [Key] = 'masterdata_read')
    BEGIN
        INSERT INTO Roles ([Key], [Name], [Description], [AdGroup], [IsSystem], [SortOrder],
                           [CreatedAt], [CreatedBy], [CreatedByWindows])
        VALUES ('masterdata_read', 'Stammdaten ansehen',
                'Nur-Lesen-Zugriff auf alle Stammdaten-Sichten (Benutzer, Rollen, Arbeitsplaetze, Einstellungen, Werkbaenke, Empfaenger, Artikelkategorien/-attribute, Schichtkalender, Aktivitaets-Protokoll).',
                NULL, 1, 5,
                SYSDATETIME(), 'system', 'system');
        PRINT 'Rolle masterdata_read angelegt.';
    END
    ELSE
    BEGIN
        PRINT 'Rolle masterdata_read existiert bereits — uebersprungen.';
    END

    -- __EFMigrationsHistory eintragen (separater Batch nicht noetig, weil kein DDL)
    IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId LIKE '%AddMasterDataReadRole')
    BEGIN
        -- Den exakten Timestamp aus der EF-generierten Datei in IdealAkeWms/Migrations/ lesen und hier eintragen
        DECLARE @migrationId NVARCHAR(150) = '<timestamp>_AddMasterDataReadRole';
        INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
        VALUES (@migrationId, '10.0.0');
    END

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH
```

**Vor Commit**: `<timestamp>` durch den exakten Timestamp aus der EF-Migration-Datei ersetzen (z.B. `20260603140532`).

- [ ] **Step 4: SQL/00_FreshInstall.sql synchronisieren**

In `SQL/00_FreshInstall.sql`:

1. Im Roles-Seeding-Block den neuen INSERT ergaenzen (nach `masterdata` einfuegen):

```sql
IF NOT EXISTS (SELECT 1 FROM Roles WHERE [Key] = 'masterdata_read')
BEGIN
    INSERT INTO Roles ([Key], [Name], [Description], [AdGroup], [IsSystem], [SortOrder],
                       [CreatedAt], [CreatedBy], [CreatedByWindows])
    VALUES ('masterdata_read', 'Stammdaten ansehen',
            'Nur-Lesen-Zugriff auf alle Stammdaten-Sichten (Benutzer, Rollen, Arbeitsplaetze, Einstellungen, Werkbaenke, Empfaenger, Artikelkategorien/-attribute, Schichtkalender, Aktivitaets-Protokoll).',
            NULL, 1, 5,
            SYSDATETIME(), 'system', 'system');
END
```

2. Am Ende im `__EFMigrationsHistory`-Block einen neuen INSERT ergaenzen (nach dem letzten bestehenden Eintrag):

```sql
-- AddMasterDataReadRole (Migration 67, v1.20.0)
IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '<timestamp>_AddMasterDataReadRole')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('<timestamp>_AddMasterDataReadRole', '10.0.0');
```

`<timestamp>` 1:1 aus dem EF-Migration-Dateinamen kopieren.

- [ ] **Step 5: Build muss gruen sein**

```bash
cd C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd
dotnet build IdealAkeWms.slnx 2>&1 | tail -3
```

Expected: `0 Fehler`

- [ ] **Step 6: Tests muessen weiterhin gruen sein**

```bash
cd C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd
dotnet test IdealAkeWms.slnx --no-build 2>&1 | tail -4
```

Expected: alle Tests gruen.

- [ ] **Step 7: Commit**

```bash
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd add IdealAkeWms/Migrations/*AddMasterDataReadRole* IdealAkeWms/Migrations/ApplicationDbContextModelSnapshot.cs SQL/67_AddMasterDataReadRole.sql SQL/00_FreshInstall.sql
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd commit -m "feat(db): Migration 67 AddMasterDataReadRole

Seed-Migration fuer Rolle masterdata_read (IsSystem=1, SortOrder=5).
FreshInstall + SQL/67 synchron. Idempotent.
Up() seedet Rolle, Down() loescht sie."
```

---

## Task 4: UsersController-Refactor + RoleOverview-Action

**Files:**
- Modify: `IdealAkeWms/Controllers/UsersController.cs`

- [ ] **Step 1: Aktuellen Stand UsersController lesen**

```bash
cat C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd/IdealAkeWms/Controllers/UsersController.cs | head -30
```

Erwarte: Class-Level `[RequireMasterDataAccess]` an Zeile 10.

- [ ] **Step 2: Class-Level Filter umstellen + Action-Level Edits hinzufuegen**

Modify `IdealAkeWms/Controllers/UsersController.cs`:

1. Class-Level: `[RequireMasterDataAccess]` → `[RequireMasterDataReadAccess]`

2. Pro Edit-Action ein zusaetzliches `[RequireMasterDataAccess]` direkt ueber dem `[HttpGet]`/`[HttpPost]`-Attribut:
   - GET `Create()` (Zeile ~53): `[RequireMasterDataAccess]` darueber
   - POST `Create(UserEditViewModel, string?)` (Zeile ~62): `[RequireMasterDataAccess]`
   - GET `Edit(int)` (Zeile ~102): `[RequireMasterDataAccess]`
   - POST `Edit(int, UserEditViewModel, string?)` (Zeile ~141): `[RequireMasterDataAccess]`
   - POST `ResetViewPreferences(int, string?)` (Zeile ~188): `[RequireMasterDataAccess]`

Beispiel-Diff:

```csharp
// VORHER:
[RequireMasterDataAccess]
public class UsersController : Controller
{
    // ...
    public async Task<IActionResult> Create()

// NACHHER:
[RequireMasterDataReadAccess]
public class UsersController : Controller
{
    // ...
    [RequireMasterDataAccess]
    public async Task<IActionResult> Create()
```

- [ ] **Step 3: Neue RoleOverview-Action am Ende der Klasse hinzufuegen**

Vor der Klassen-schliessenden Klammer:

```csharp
    /// <summary>
    /// Hand-gepflegte Uebersicht: welche Rolle hat Zugriff auf welche Seiten.
    /// Wird von Users/Index|Create|Edit verlinkt.
    /// </summary>
    [HttpGet]
    public IActionResult RoleOverview()
    {
        return View();
    }
```

Class-Level-Filter `[RequireMasterDataReadAccess]` gilt automatisch.

- [ ] **Step 4: Build + Tests**

```bash
cd C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd
dotnet build IdealAkeWms.slnx 2>&1 | tail -3
dotnet test IdealAkeWms.slnx --no-build --filter "FullyQualifiedName~UsersController" 2>&1 | tail -3
```

Expected: Build `0 Fehler`. Bestehende UsersControllerTests bleiben gruen (sie testen Actions direkt, nicht Filter — masterdata-Rolle erfuellt beide Filter).

- [ ] **Step 5: Commit**

```bash
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd add IdealAkeWms/Controllers/UsersController.cs
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd commit -m "refactor(auth): UsersController Read/Edit-Split + RoleOverview-Action

Class-Level: RequireMasterDataReadAccess (war RequireMasterDataAccess).
Action-Level Edits (Create/Edit/ResetViewPreferences): RequireMasterDataAccess.
Neue Action RoleOverview als Stub (View kommt in Task 9)."
```

---

## Task 5: RolesController, WorkstationsController, SettingsController

**Files:**
- Modify: `IdealAkeWms/Controllers/RolesController.cs`
- Modify: `IdealAkeWms/Controllers/WorkstationsController.cs`
- Modify: `IdealAkeWms/Controllers/SettingsController.cs`

Pattern: Class-Level `[RequireMasterDataAccess]` → `[RequireMasterDataReadAccess]`, Edit-Actions zusaetzlich `[RequireMasterDataAccess]`.

- [ ] **Step 1: RolesController umstellen**

In `IdealAkeWms/Controllers/RolesController.cs`:
- Class: `[RequireMasterDataAccess]` → `[RequireMasterDataReadAccess]`
- Action-Level `[RequireMasterDataAccess]` an: GET Create (~Z.54), POST Create (~Z.62), GET Edit (~Z.93), POST Edit (~Z.116), POST Delete (~Z.169)

- [ ] **Step 2: WorkstationsController umstellen**

In `IdealAkeWms/Controllers/WorkstationsController.cs`:
- Class: `[RequireMasterDataAccess]` → `[RequireMasterDataReadAccess]`
- Action-Level `[RequireMasterDataAccess]` an: GET Create (~Z.46), POST Create (~Z.57), GET Edit (~Z.90), POST Edit (~Z.112)

- [ ] **Step 3: SettingsController umstellen**

In `IdealAkeWms/Controllers/SettingsController.cs`:
- Class: `[RequireMasterDataAccess]` → `[RequireMasterDataReadAccess]`
- GET Index (~Z.33) + GET OperationConfig (~Z.110): NICHT touchen (read-only mit Class-Filter)
- Action-Level `[RequireMasterDataAccess]` an: POST SaveSettings (~Z.45), POST AddHoliday (~Z.58), POST DeleteHoliday (~Z.76), POST SyncHolidays (~Z.85), POST AddOperationConfig (~Z.122), POST UpdateOperationConfig (~Z.151), POST DeleteOperationConfig (~Z.168)

- [ ] **Step 4: Build + Tests**

```bash
cd C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd
dotnet build IdealAkeWms.slnx 2>&1 | tail -3
dotnet test IdealAkeWms.slnx --no-build --filter "FullyQualifiedName~RolesController|FullyQualifiedName~WorkstationsController|FullyQualifiedName~SettingsController" 2>&1 | tail -3
```

Expected: Build clean, bestehende Tests gruen.

- [ ] **Step 5: Commit**

```bash
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd add IdealAkeWms/Controllers/RolesController.cs IdealAkeWms/Controllers/WorkstationsController.cs IdealAkeWms/Controllers/SettingsController.cs
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd commit -m "refactor(auth): Roles+Workstations+Settings Read/Edit-Split

Class-Level RequireMasterDataReadAccess + Action-Level RequireMasterDataAccess
fuer alle schreibenden Actions (POST/Edit-Forms)."
```

---

## Task 6: ProductionWorkplaces, OrderRecipients, ArticleCategories, ArticleAttributes

**Files:**
- Modify: `IdealAkeWms/Controllers/ProductionWorkplacesController.cs`
- Modify: `IdealAkeWms/Controllers/OrderRecipientsController.cs`
- Modify: `IdealAkeWms/Controllers/ArticleCategoriesController.cs`
- Modify: `IdealAkeWms/Controllers/ArticleAttributesController.cs`

- [ ] **Step 1: ProductionWorkplacesController umstellen**

- Class: `[RequireMasterDataAccess]` → `[RequireMasterDataReadAccess]`
- Action-Level `[RequireMasterDataAccess]` an: GET Create (~Z.54), POST Create (~Z.66), GET Edit (~Z.104), POST Edit (~Z.133)

- [ ] **Step 2: OrderRecipientsController umstellen**

- Class: `[RequireMasterDataAccess]` → `[RequireMasterDataReadAccess]`
- GET Index (~Z.24) + GET ArticleGroupMappings (~Z.234): NICHT touchen (Class reicht)
- Action-Level `[RequireMasterDataAccess]` an: GET Create (~Z.43), POST Create (~Z.51), GET Edit (~Z.71), POST Edit (~Z.96), POST Delete (~Z.138), POST AddRecipient (~Z.163), POST UpdateRecipient (~Z.194), POST DeleteRecipient (~Z.221), POST SaveArticleGroupMappings (~Z.260)

- [ ] **Step 3: ArticleCategoriesController umstellen**

- Class: `[RequireMasterDataAccess]` → `[RequireMasterDataReadAccess]`
- Action-Level `[RequireMasterDataAccess]` an: POST Create (~Z.45), POST Update (~Z.75), POST Delete (~Z.108)

- [ ] **Step 4: ArticleAttributesController umstellen**

- Class: `[RequireMasterDataAccess]` → `[RequireMasterDataReadAccess]`
- Action-Level `[RequireMasterDataAccess]` an: POST CreateDefinition (~Z.51), POST UpdateDefinition (~Z.83), POST DeleteDefinition (~Z.109), POST AddOption (~Z.129), POST DeleteOption (~Z.155)

- [ ] **Step 5: Build + Tests**

```bash
cd C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd
dotnet build IdealAkeWms.slnx 2>&1 | tail -3
dotnet test IdealAkeWms.slnx --no-build 2>&1 | tail -4
```

Expected: alle Tests gruen.

- [ ] **Step 6: Commit**

```bash
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd add IdealAkeWms/Controllers/ProductionWorkplacesController.cs IdealAkeWms/Controllers/OrderRecipientsController.cs IdealAkeWms/Controllers/ArticleCategoriesController.cs IdealAkeWms/Controllers/ArticleAttributesController.cs
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd commit -m "refactor(auth): Workplaces+Recipients+ArticleCats+Attributes Read/Edit-Split

Pattern: Class RequireMasterDataReadAccess + Action-Level
RequireMasterDataAccess auf Edit-Actions."
```

---

## Task 7: BdeShiftCalendarController + SyncLogController

**Files:**
- Modify: `IdealAkeWms/Controllers/BdeShiftCalendarController.cs`
- Modify: `IdealAkeWms/Controllers/SyncLogController.cs`

- [ ] **Step 1: BdeShiftCalendarController umstellen**

- Class: `[RequireMasterDataAccess]` → `[RequireMasterDataReadAccess]`
- GET Index (~Z.23): NICHT touchen
- Action-Level `[RequireMasterDataAccess]` an: POST Create (~Z.33), POST Delete (~Z.66)

- [ ] **Step 2: SyncLogController umstellen**

- Class: `[RequireMasterDataAccess]` → `[RequireMasterDataReadAccess]`
- GET Index (~Z.25): NICHT touchen (Sonderfall, nur Read-Action)
- Keine Action-Level-Filter noetig — SyncLog hat keine Edit-Actions.

- [ ] **Step 3: Build + Tests**

```bash
cd C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd
dotnet build IdealAkeWms.slnx 2>&1 | tail -3
dotnet test IdealAkeWms.slnx --no-build --filter "FullyQualifiedName~BdeShiftCalendar|FullyQualifiedName~SyncLog" 2>&1 | tail -3
```

Expected: alle gruen.

- [ ] **Step 4: Commit**

```bash
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd add IdealAkeWms/Controllers/BdeShiftCalendarController.cs IdealAkeWms/Controllers/SyncLogController.cs
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd commit -m "refactor(auth): BdeShiftCalendar+SyncLog Read/Edit-Split

BdeShiftCalendar: Class Read + Action-Level Edit auf Create/Delete.
SyncLog: nur Class Read (keine Edit-Actions)."
```

---

## Task 8: Lager-Filter — WarehousePicking + MissingPartsLager

**Files:**
- Modify: `IdealAkeWms/Controllers/WarehousePickingController.cs`
- Modify: `IdealAkeWms/Controllers/MissingPartsLagerController.cs`
- Modify: `IdealAkeWms.Tests/Controllers/WarehousePickingControllerTests.cs` (Sanity)
- Modify: `IdealAkeWms.Tests/Controllers/MissingPartsLagerControllerTests.cs` (Sanity)

- [ ] **Step 1: WarehousePickingController Filter umstellen**

In `IdealAkeWms/Controllers/WarehousePickingController.cs` (Zeile 10):

```csharp
// VORHER:
[RequireStockAccess]
public class WarehousePickingController : Controller

// NACHHER:
[RequireLagerProcessingAccess]
public class WarehousePickingController : Controller
```

- [ ] **Step 2: MissingPartsLagerController Filter umstellen**

In `IdealAkeWms/Controllers/MissingPartsLagerController.cs`:

```csharp
// VORHER:
[RequireStockAccess]
public class MissingPartsLagerController : Controller

// NACHHER:
[RequireLagerProcessingAccess]
public class MissingPartsLagerController : Controller
```

- [ ] **Step 3: Build + Tests**

```bash
cd C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd
dotnet build IdealAkeWms.slnx 2>&1 | tail -3
dotnet test IdealAkeWms.slnx --no-build --filter "FullyQualifiedName~WarehousePicking|FullyQualifiedName~MissingPartsLager" 2>&1 | tail -3
```

Expected: alle gruen (Tests stellen Mock-Setups, keine echte Filter-Pruefung).

- [ ] **Step 4: Commit**

```bash
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd add IdealAkeWms/Controllers/WarehousePickingController.cs IdealAkeWms/Controllers/MissingPartsLagerController.cs
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd commit -m "refactor(auth): Lager-Filter WarehousePicking+MissingPartsLager

RequireStockAccess → RequireLagerProcessingAccess.
Effekt: picker verlieren Zugriff auf Lager-Eingehende-Listen +
Lager-Fehlteile. Bestand + Bewegungshistorie bleiben fuer picker
zugaenglich (StockOverview + StockMovements unveraendert)."
```

---

## Task 9: Views/Users/RoleOverview.cshtml — hand-gepflegte Tabelle

**Files:**
- Create: `IdealAkeWms/Views/Users/RoleOverview.cshtml`
- Modify: `IdealAkeWms.Tests/Controllers/UsersControllerTests.cs` (+1 Test)

- [ ] **Step 1: Test fuer RoleOverview-Action schreiben**

In `IdealAkeWms.Tests/Controllers/UsersControllerTests.cs` einen neuen Test ergaenzen:

```csharp
[Fact]
public void RoleOverview_ReturnsViewResult()
{
    var (ctrl, _) = BuildController();  // bestehendes Build-Pattern verwenden
    var result = ctrl.RoleOverview();
    result.Should().BeOfType<ViewResult>();
}
```

Falls `BuildController` anders heisst, bestehendes Test-Pattern aus der Datei kopieren.

- [ ] **Step 2: Test laufen lassen (gruen, weil Action existiert)**

```bash
cd C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd
dotnet test IdealAkeWms.slnx --no-build --filter "FullyQualifiedName~UsersControllerTests.RoleOverview" 2>&1 | tail -3
```

Expected: 1/1 gruen.

- [ ] **Step 3: RoleOverview.cshtml erstellen**

Create `IdealAkeWms/Views/Users/RoleOverview.cshtml`:

```razor
@{
    ViewData["Title"] = "Rollen-Uebersicht";
}

<div class="d-flex justify-content-between flex-wrap gap-2 mb-3">
    <h2 class="page-header mb-0">@ViewData["Title"]</h2>
    <a asp-controller="Users" asp-action="Index" class="btn btn-secondary">Zurueck zur Benutzerliste</a>
</div>

<div class="alert alert-info">
    <strong>Wie funktionieren Berechtigungen?</strong>
    Ein Benutzer kann mehrere Rollen haben (Many-to-Many). Wer mehrere Rollen hat, hat die
    <em>Vereinigung</em> der Rechte — mehr Rollen = mehr Zugriff. Die Rolle <code>admin</code> ist ein
    Wildcard und ueberspringt alle Pruefungen. Rollen koennen optional mit AD-Gruppen verknuepft werden;
    Login uebernimmt dann die Rollen automatisch.
</div>

<div class="alert alert-warning small">
    <strong>Hinweis fuer Entwickler:</strong> Diese Uebersicht ist hand-gepflegt. Bei Aenderungen an
    Controller-Filtern bitte <code>Views/Users/RoleOverview.cshtml</code> mit-aktualisieren. Die Quelle
    fuer Filter-Mappings ist <code>CLAUDE.md</code> + die <code>Filters/Require*Attribute.cs</code>-Klassen.
</div>

<div class="table-responsive">
    <table class="table table-striped">
        <thead>
            <tr>
                <th>Rolle-Key</th>
                <th>Klartext-Name</th>
                <th>Beschreibung</th>
                <th>Zugang zu (Auswahl)</th>
            </tr>
        </thead>
        <tbody>
            <tr class="table-warning">
                <td><code>admin</code></td>
                <td>Administrator</td>
                <td>Wildcard — Vollzugriff auf alles.</td>
                <td>Alle Seiten und Aktionen.</td>
            </tr>
            <tr>
                <td><code>masterdata</code></td>
                <td>Stammdaten verwalten</td>
                <td>Lesen UND aendern aller Stammdatensichten.</td>
                <td>
                    <ul class="mb-0 small">
                        <li>Benutzer, Rollen, Arbeitsplaetze, Einstellungen (lesen + aendern)</li>
                        <li>Werkbaenke, Empfaengergruppen, Artikelkategorien/-attribute (lesen + aendern)</li>
                        <li>BDE-Schichtkalender, Aktivitaets-Protokoll (lesen + aendern)</li>
                    </ul>
                </td>
            </tr>
            <tr>
                <td><code>masterdata_read</code></td>
                <td>Stammdaten ansehen</td>
                <td>Nur-Lesen-Zugriff auf alle Stammdatensichten.</td>
                <td>
                    <ul class="mb-0 small">
                        <li>Alle Stammdatensichten lesbar, KEINE Edit-Buttons</li>
                        <li>Settings-Form wird Read-Only angezeigt</li>
                        <li>Rollen-Uebersicht (diese Seite) sichtbar</li>
                    </ul>
                </td>
            </tr>
            <tr>
                <td><code>picking</code></td>
                <td>Kommissionierung</td>
                <td>Komm.-Picker an der Werkbank.</td>
                <td>
                    <ul class="mb-0 small">
                        <li>Meine Bestellungen / Meine Fehlteile (eigene Werkbaenke)</li>
                        <li>Picking (FA + Stueckliste)</li>
                        <li>Bestandsuebersicht + Bewegungshistorie (read-only)</li>
                        <li>KEIN Zugriff auf Lager-Eingehende-Listen + Lager-Fehlteile</li>
                    </ul>
                </td>
            </tr>
            <tr>
                <td><code>stock</code></td>
                <td>Lager-Mitarbeiter</td>
                <td>Lager-Worklist + Bestaende.</td>
                <td>
                    <ul class="mb-0 small">
                        <li>Lager: Eingehende Listen (Lagerbestellungen kommissionieren)</li>
                        <li>Lager: Fehlteile (globale Sicht)</li>
                        <li>Einbuchung / Ausbuchung / Bestandsuebersicht</li>
                        <li>Bewegungshistorie</li>
                    </ul>
                </td>
            </tr>
            <tr>
                <td><code>stock_keyuser</code></td>
                <td>Lager-Keyuser</td>
                <td>Lager + Lagerplatz-Sonderaktionen.</td>
                <td>
                    <ul class="mb-0 small">
                        <li>Alles von <code>stock</code></li>
                        <li>Lagerplatz ausbuchen / umbuchen</li>
                    </ul>
                </td>
            </tr>
            <tr>
                <td><code>tracking</code></td>
                <td>OSEON-Tracking</td>
                <td>OSEON-Auftraege + Rueckmeldungen.</td>
                <td>
                    <ul class="mb-0 small">
                        <li>OSEON-Auftragsbaum + Ampelsystem</li>
                        <li>Rueckmeldungen scannen</li>
                    </ul>
                </td>
            </tr>
            <tr>
                <td><code>reporting</code></td>
                <td>OSEON-Reporting</td>
                <td>Reporting-Sichten.</td>
                <td>
                    <ul class="mb-0 small">
                        <li>OSEON-Reporting (Horizont, Ueberfaellig-Slice)</li>
                    </ul>
                </td>
            </tr>
            <tr>
                <td><code>leitstand</code></td>
                <td>Leitstand</td>
                <td>FA freigeben + priorisieren.</td>
                <td>
                    <ul class="mb-0 small">
                        <li>Picking-Leitstand (Freigabe, Priorisierung, Picker-Zuweisung)</li>
                    </ul>
                </td>
            </tr>
            <tr>
                <td><code>fa_completion</code></td>
                <td>FA-Vervollstaendigung</td>
                <td>Merkmalsauspraegungen pro Vormontageplatz.</td>
                <td>
                    <ul class="mb-0 small">
                        <li>FA-Vervollstaendigung Edit + Specs-CRUD</li>
                    </ul>
                </td>
            </tr>
            <tr>
                <td><code>bde_user</code></td>
                <td>BDE-Benutzer</td>
                <td>Terminal-Buchung.</td>
                <td>
                    <ul class="mb-0 small">
                        <li>BDE-Terminal (scannen, Status wechseln)</li>
                    </ul>
                </td>
            </tr>
            <tr>
                <td><code>bde_shiftlead</code></td>
                <td>BDE-Schichtleiter</td>
                <td>+ Stammdaten, Buchungsliste, Cockpit.</td>
                <td>
                    <ul class="mb-0 small">
                        <li>Alles von <code>bde_user</code></li>
                        <li>BDE-Cockpit, Buchungsliste, BDE-Stammdaten</li>
                    </ul>
                </td>
            </tr>
            <tr>
                <td><code>bde_admin</code></td>
                <td>BDE-Admin</td>
                <td>+ Buchungen korrigieren/stornieren + Terminals.</td>
                <td>
                    <ul class="mb-0 small">
                        <li>Alles von <code>bde_shiftlead</code></li>
                        <li>Buchungen Edit/Cancel</li>
                        <li>Terminals konfigurieren</li>
                    </ul>
                </td>
            </tr>
        </tbody>
    </table>
</div>
```

- [ ] **Step 4: Build + Tests**

```bash
cd C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd
dotnet build IdealAkeWms.slnx 2>&1 | tail -3
dotnet test IdealAkeWms.slnx --no-build 2>&1 | tail -4
```

Expected: alle gruen.

- [ ] **Step 5: Commit**

```bash
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd add IdealAkeWms/Views/Users/RoleOverview.cshtml IdealAkeWms.Tests/Controllers/UsersControllerTests.cs
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd commit -m "feat(view): Users/RoleOverview hand-gepflegte Rollen-Uebersicht

Tabelle mit 13 Rollen (incl. masterdata_read), Klartext-Beschreibung +
Liste sichtbarer Seiten/Aktionen. Erklaer-Header zu Many-to-Many +
Union-Semantik. Pflege-Hinweis fuer Entwickler in der View selbst.
Verlinkt: 'Zurueck zur Benutzerliste'. Test: Action liefert ViewResult."
```

---

## Task 10: Hide Edit-Buttons in 10 Index-Views + RoleOverview-Links in Users-Views

**Files:**
- Modify: `IdealAkeWms/Views/Users/Index.cshtml`
- Modify: `IdealAkeWms/Views/Users/Create.cshtml` (RoleOverview-Link)
- Modify: `IdealAkeWms/Views/Users/Edit.cshtml` (RoleOverview-Link)
- Modify: `IdealAkeWms/Views/Roles/Index.cshtml`
- Modify: `IdealAkeWms/Views/Workstations/Index.cshtml`
- Modify: `IdealAkeWms/Views/ProductionWorkplaces/Index.cshtml`
- Modify: `IdealAkeWms/Views/OrderRecipients/Index.cshtml`
- Modify: `IdealAkeWms/Views/ArticleCategories/Index.cshtml`
- Modify: `IdealAkeWms/Views/ArticleAttributes/Index.cshtml`
- Modify: `IdealAkeWms/Views/BdeShiftCalendar/Index.cshtml`

**Pattern fuer jede Index-View:**

1. Oben in der View (nach `@model`-Direktive) ergaenzen:

```razor
@inject IdealAkeWms.Services.ICurrentUserService _user
@{
    var canEdit = await _user.HasMasterDataAccessAsync();
}
```

2. Jeden `<a asp-action="Create" ...>`-, `<a asp-action="Edit" ...>`-, `<a asp-action="Delete" ...>`- oder `<form ... asp-action="Delete">`-Block in `@if (canEdit) { ... }` einwickeln.

3. Optional bei Read-Only: Info-Banner ueber der Tabelle wenn nicht canEdit:

```razor
@if (!canEdit)
{
    <div class="alert alert-info small">Sie haben Nur-Lesen-Zugriff. Aenderungen sind nicht moeglich.</div>
}
```

- [ ] **Step 1: Users/Index.cshtml — Buttons-Hide + RoleOverview-Link**

In `IdealAkeWms/Views/Users/Index.cshtml`:
- Inject + canEdit-Check oben
- Im Page-Header ueber der Tabelle: Button "Rollen-Uebersicht" hinzufuegen (immer sichtbar):

```razor
<a asp-action="RoleOverview" class="btn btn-outline-secondary btn-sm">Rollen-Uebersicht</a>
```

- "Neu"-Button + Edit/Delete-Links in `@if (canEdit) { ... }` einwickeln

- [ ] **Step 2: Users/Create.cshtml + Users/Edit.cshtml — RoleOverview-Link**

In beiden Files oberhalb der Rollen-Checkboxen einen Link einfuegen:

```razor
<div class="mb-2">
    <a asp-action="RoleOverview" class="text-decoration-none small">
        <i class="bi bi-info-circle"></i> Was darf welche Rolle?
    </a>
</div>
```

- [ ] **Step 3: Restliche 8 Index-Views umstellen**

Fuer jede der folgenden Index-Views das gleiche Inject + canEdit-Pattern anwenden (siehe Pattern oben), Buttons in `@if (canEdit)` einwickeln:
- `Views/Roles/Index.cshtml`
- `Views/Workstations/Index.cshtml`
- `Views/ProductionWorkplaces/Index.cshtml`
- `Views/OrderRecipients/Index.cshtml`
- `Views/ArticleCategories/Index.cshtml`
- `Views/ArticleAttributes/Index.cshtml`
- `Views/BdeShiftCalendar/Index.cshtml`

Falls eine View bereits einen `_user`-Inject hat: bestehendes Pattern erweitern, nicht doppelt.

`SyncLog/Index.cshtml` braucht KEINEN Button-Hide (hat keine Edit-Buttons).

- [ ] **Step 4: Build**

```bash
cd C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd
dotnet build IdealAkeWms.slnx 2>&1 | tail -3
```

Expected: `0 Fehler`. Razor-Compile-Fehler sind moeglich → Inject-Statement-Syntax pruefen.

- [ ] **Step 5: Commit**

```bash
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd add IdealAkeWms/Views/Users/Index.cshtml IdealAkeWms/Views/Users/Create.cshtml IdealAkeWms/Views/Users/Edit.cshtml IdealAkeWms/Views/Roles/Index.cshtml IdealAkeWms/Views/Workstations/Index.cshtml IdealAkeWms/Views/ProductionWorkplaces/Index.cshtml IdealAkeWms/Views/OrderRecipients/Index.cshtml IdealAkeWms/Views/ArticleCategories/Index.cshtml IdealAkeWms/Views/ArticleAttributes/Index.cshtml IdealAkeWms/Views/BdeShiftCalendar/Index.cshtml
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd commit -m "feat(view): Edit-Buttons in 9 Stammdaten-Index-Views ausblenden + RoleOverview-Link

Read-Only-User (masterdata_read ohne masterdata): Neu/Bearbeiten/Loeschen-
Buttons verschwinden, Info-Banner zeigt Nur-Lesen-Modus.
Users-Bereich: Button 'Rollen-Uebersicht' (Page-Header) + Link
'Was darf welche Rolle?' in Create/Edit-Forms."
```

---

## Task 11: Settings-Form disabled + Banner (Read-Only-UX)

**Files:**
- Modify: `IdealAkeWms/Views/Settings/Index.cshtml`
- Modify: `IdealAkeWms/Views/Settings/OperationConfig.cshtml`

- [ ] **Step 1: Settings/Index.cshtml — Banner + disabled-Inputs**

In `IdealAkeWms/Views/Settings/Index.cshtml`:

1. Oben (nach `@model`-Direktive):

```razor
@inject IdealAkeWms.Services.ICurrentUserService _user
@{
    var canEdit = await _user.HasMasterDataAccessAsync();
}
```

2. Direkt unter dem Page-Header:

```razor
@if (!canEdit)
{
    <div class="alert alert-warning">
        <strong>Nur-Lesen-Modus:</strong> Sie haben keine Berechtigung, Einstellungen zu aendern.
        Eingaben sind deaktiviert.
    </div>
}
```

3. Save-Button (typisch `<button type="submit">Speichern</button>`) in `@if (canEdit) { ... }` einwickeln.

4. Bei allen `<input ...>`-/`<select ...>`-/`<textarea ...>`-Elementen das Attribut `disabled="@(!canEdit)"` ergaenzen. Beispiel:

```razor
<input type="text" name="settings[KommissionierTage]" value="..." class="form-control" disabled="@(!canEdit)" />
```

Falls Razor das Attribut bei `false` nicht renderfrei stellt, alternative Form: `@(canEdit ? "" : "disabled")` als ganzes Attribut.

- [ ] **Step 2: Settings/OperationConfig.cshtml — gleiches Pattern**

Identische Anwendung in `IdealAkeWms/Views/Settings/OperationConfig.cshtml`: Inject + canEdit + Banner + disabled-Inputs + Save-Button in `@if (canEdit)`.

- [ ] **Step 3: Build**

```bash
cd C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd
dotnet build IdealAkeWms.slnx 2>&1 | tail -3
```

Expected: `0 Fehler`.

- [ ] **Step 4: Commit**

```bash
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd add IdealAkeWms/Views/Settings/Index.cshtml IdealAkeWms/Views/Settings/OperationConfig.cshtml
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd commit -m "feat(view): Settings-Forms Read-Only-Banner + disabled-Inputs

User mit masterdata_read sehen Settings-Index + OperationConfig
mit Banner 'Nur-Lesen-Modus' und allen Inputs disabled. Save-Buttons
versteckt. POST-Endpoints sind ohnehin durch RequireMasterDataAccess
geschuetzt."
```

---

## Task 12: _Layout.cshtml — Stammdaten-Block + Lager-Block

**Files:**
- Modify: `IdealAkeWms/Views/Shared/_Layout.cshtml`

- [ ] **Step 1: Stammdaten-Block auf Read-Helper umstellen**

In `IdealAkeWms/Views/Shared/_Layout.cshtml` an Zeilen ~154 und ~180:

```razor
// VORHER:
@if (await CurrentUserService.HasMasterDataAccessAsync())
{
    <li><hr class="dropdown-divider" style="border-color: rgba(255,255,255,0.2);" /></li>
    // ... Eintraege: Benutzer, Rollen, ...
}

// NACHHER:
@if (await CurrentUserService.HasMasterDataReadAccessAsync())
{
    <li><hr class="dropdown-divider" style="border-color: rgba(255,255,255,0.2);" /></li>
    // ... Eintraege: Benutzer, Rollen, ... (unveraendert)
}
```

Beide Stellen (Zeile ~154 und ~180) auf `HasMasterDataReadAccessAsync()` umstellen.

- [ ] **Step 2: Lager-Block auf neuen Helper umstellen**

An der Stelle, wo "Lager: Eingehende Listen" + "Lager: Fehlteile" gerendert werden (siehe Zeile ~118 mit `@if (canAccessStock)`):

```razor
// VORHER:
@if (canAccessStock)
{
    <li><a class="dropdown-item" asp-controller="WarehousePicking" asp-action="Index">Lager: Eingehende Listen</a></li>
    <li><a class="dropdown-item" asp-controller="MissingPartsLager" asp-action="Index">Lager: Fehlteile</a></li>
}
```

In zwei Bloecke aufteilen — der bisherige `canAccessStock`-Block schliesst Bestand+Bewegungen ein (picker darf das sehen), nur die Lager-Worklist-Eintraege brauchen den engeren Helper:

```razor
// NACHHER:
@{
    var canProcessLager = await CurrentUserService.CanProcessLagerAsync();
}
@if (canProcessLager)
{
    <li><a class="dropdown-item" asp-controller="WarehousePicking" asp-action="Index">Lager: Eingehende Listen</a></li>
    <li><a class="dropdown-item" asp-controller="MissingPartsLager" asp-action="Index">Lager: Fehlteile</a></li>
}
```

`canAccessStock` bleibt fuer andere Lager-Eintraege (Bestand, Bewegungshistorie) erhalten.

- [ ] **Step 3: Build**

```bash
cd C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd
dotnet build IdealAkeWms.slnx 2>&1 | tail -3
```

Expected: `0 Fehler`.

- [ ] **Step 4: Commit**

```bash
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd add IdealAkeWms/Views/Shared/_Layout.cshtml
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd commit -m "feat(layout): Stammdaten + Lager-Block auf neue Helpers

Stammdaten-Block: HasMasterDataAccessAsync → HasMasterDataReadAccessAsync.
Read-Only-User sehen die Menue-Eintraege (Index-Sichten sind ja lesbar).

Lager-Block: 'Lager: Eingehende Listen' + 'Lager: Fehlteile' hinter
CanProcessLagerAsync (ohne picker) statt canAccessStock. Bestand +
Bewegungen bleiben unter canAccessStock (picker behaelt Zugriff)."
```

---

## Task 13: Version-Bump v1.20.0 + Changelog

**Files:**
- Modify: `IdealAkeWms/AppVersion.cs`
- Modify: `IDEALAKEWMSService/AppVersion.cs`
- Modify: `IdealAkeWms/Views/Help/Changelog.cshtml`

- [ ] **Step 1: AppVersion.cs (Web) hochzaehlen**

In `IdealAkeWms/AppVersion.cs` die Version von `"1.19.0"` auf `"1.20.0"` setzen. Build-Datum auf `2026-06-03` aktualisieren.

- [ ] **Step 2: AppVersion.cs (Service) hochzaehlen**

In `IDEALAKEWMSService/AppVersion.cs` analog `"1.19.0"` → `"1.20.0"`, Build-Datum `2026-06-03`.

- [ ] **Step 3: Changelog v1.20.0-Karte einfuegen**

In `IdealAkeWms/Views/Help/Changelog.cshtml` direkt ueber dem `v1.19.0`-Block einfuegen:

```razor
<div class="card mb-3">
    <div class="card-header text-white" style="background-color: var(--ake-primary);">
        <strong>v1.20.0</strong> <span class="text-white-50 ms-2">03.06.2026</span>
    </div>
    <div class="card-body">
        <h6>Feingranulare Berechtigungen — Read/Edit-Split in Stammdaten, Lager-Worklist nur fuer Lager-Mitarbeiter</h6>
        <ul>
            <li><strong>Neue Rolle "Stammdaten ansehen"</strong> (<code>masterdata_read</code>): erlaubt
                Nur-Lesen-Zugriff auf alle 10 Stammdaten-Sichten (Benutzer, Rollen, Arbeitsplaetze,
                Einstellungen, Werkbaenke, Empfaenger, Artikelkategorien/-attribute,
                Schichtkalender, Aktivitaets-Protokoll). Edit-Buttons + Save-Buttons werden
                ausgeblendet, Settings-Form zeigt einen Banner "Nur-Lesen-Modus" + disabled
                Inputs.</li>
            <li><strong>Lager-Worklist nur fuer Lager-Mitarbeiter:</strong> "Lager: Eingehende
                Listen" + "Lager: Fehlteile" sind ab v1.20.0 nicht mehr fuer Komm.-Picker (Rolle
                <code>picking</code>) sichtbar. Lager-Personal (Rollen <code>stock</code> /
                <code>stock_keyuser</code>) und Admins behalten Zugriff. Bestandsuebersicht +
                Bewegungshistorie bleiben fuer picker weiter zugaenglich.</li>
            <li><strong>Rollen-Uebersicht im Benutzerstamm:</strong> Neue Seite
                <code>/Users/RoleOverview</code> mit Klartext-Tabelle aller 13 Rollen — welche
                Rolle hat Zugriff auf welche Seiten. Verlinkt aus Users/Index (Page-Header) und
                aus Users/Create + Users/Edit ("Was darf welche Rolle?"). Hand-gepflegt.</li>
            <li><strong>Migration:</strong> Bestehende User mit <code>masterdata</code> behalten
                vollen Zugriff (Filter inkludiert Edit-Rolle implizit). Kein User-Backfill noetig.
                Die neue Rolle <code>masterdata_read</code> wird per Seed-Migration angelegt
                (<code>IsSystem=1</code>, nicht loeschbar).</li>
        </ul>
    </div>
</div>
```

- [ ] **Step 4: Build**

```bash
cd C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd
dotnet build IdealAkeWms.slnx 2>&1 | tail -3
```

Expected: `0 Fehler`.

- [ ] **Step 5: Commit**

```bash
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd add IdealAkeWms/AppVersion.cs IDEALAKEWMSService/AppVersion.cs IdealAkeWms/Views/Help/Changelog.cshtml
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd commit -m "chore: Version v1.20.0 + Changelog

Read/Edit-Split Stammdaten + Lager-Filter + RoleOverview."
```

---

## Task 14: CLAUDE.md-Korrektur (Filter-Tabelle veraltet)

**Files:**
- Modify: `CLAUDE.md`

- [ ] **Step 1: Filter-Tabelle ergaenzen**

In `CLAUDE.md` im Abschnitt `## Zugriffsschutz`:

1. Zeile fuer `RequireMasterDataAccess` Controller-Liste **korrigieren** (alle 10):

```markdown
| `[RequireMasterDataAccess]` | admin, masterdata | UsersController, RolesController, WorkstationsController, SettingsController, ProductionWorkplacesController, OrderRecipientsController, ArticleCategoriesController, ArticleAttributesController, BdeShiftCalendarController, SyncLogController (Action-Level fuer Edits; Class ist seit v1.20.0 `[RequireMasterDataReadAccess]`) |
```

2. Neue Zeile direkt darunter:

```markdown
| `[RequireMasterDataReadAccess]` | admin, masterdata_read, masterdata | Class-Level der 10 Stammdaten-Controller (Read-Filter, Edit-Actions verschaerfen mit `[RequireMasterDataAccess]`) |
```

3. Zeile fuer `RequireStockAccess` Controller-Liste **korrigieren** (WarehousePicking + MissingPartsLager raus):

```markdown
| `[RequireStockAccess]` | admin, stock, stock_keyuser, picking | StockMovementsController, StockOverviewController |
```

4. Neue Zeile direkt darunter:

```markdown
| `[RequireLagerProcessingAccess]` | admin, stock, stock_keyuser | WarehousePickingController, MissingPartsLagerController (Lager-Worklist — picker explizit ausgeschlossen) |
```

- [ ] **Step 2: Rollen-Tabelle ergaenzen**

Im Abschnitt `## Rollenkonzept` die Rollen-Tabelle erweitern (nach `masterdata`):

```markdown
| `masterdata` | Benutzer, Arbeitsplaetze, Einstellungen + 6 weitere Stammdaten-Sichten (lesen + aendern) |
| `masterdata_read` | Nur-Lesen-Zugriff auf alle Stammdatensichten (seit v1.20.0) |
```

- [ ] **Step 3: Bekannte Fallstricke ergaenzen**

Im Abschnitt `## Bekannte Fallstricke` zwei neue Bullets ergaenzen:

```markdown
- **Stammdaten Read/Edit-Pattern (seit v1.20.0)**: Class-Level der 10 Stammdaten-Controller traegt `[RequireMasterDataReadAccess]`, schreibende Actions verschaerfen mit `[RequireMasterDataAccess]`. ASP.NET kumuliert beide Filter — Edit-User passieren beide. Read-User (`masterdata_read`) sehen Index/Listing-Views, bekommen Buttons via Razor-Check `await _user.HasMasterDataAccessAsync()` ausgeblendet. Bei Erweiterung weiterer Module nach demselben Pattern: zusaetzlich `xxx_read`-Rolle + `RequireXxxReadAccess`-Filter + Class-Level-Umhaengung.
- **Rollen-Uebersicht (`/Users/RoleOverview`) ist hand-gepflegt (seit v1.20.0)**: Bei Aenderungen an Controller-Filtern (neuer Controller, Filter-Swap, neue Rolle) bitte `Views/Users/RoleOverview.cshtml` mit-updaten. Pflege-Hinweis steht in der View selbst. Verlinkt aus Users/Index (Page-Header) und Users/Create + Users/Edit ("Was darf welche Rolle?").
- **Lager-Worklist ist NICHT picker-zugaenglich (seit v1.20.0)**: WarehousePicking + MissingPartsLager nutzen `[RequireLagerProcessingAccess]` (admin/stock/stock_keyuser). Picker behalten Bestand + Bewegungshistorie (`[RequireStockAccess]` inkludiert picking weiter). Layout-Menue zeigt "Lager: ..."-Eintraege hinter `await CurrentUserService.CanProcessLagerAsync()`.
```

- [ ] **Step 4: Commit**

```bash
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd add CLAUDE.md
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd commit -m "docs(claude): Filter-Tabelle korrigiert + Rollen + Fallstricke

Filter-Tabelle: RequireMasterDataAccess listet jetzt alle 10
Controller (vorher nur 4), RequireMasterDataReadAccess +
RequireLagerProcessingAccess neu. RequireStockAccess Liste
verkleinert (WarehousePicking + MissingPartsLager raus).

Rollen: masterdata_read als Read-Bundle.

Fallstricke: Read/Edit-Pattern, RoleOverview-Pflege, Lager-Worklist."
```

---

## Task 15: PROJECT_STATUS + TESTSZENARIEN

**Files:**
- Modify: `PROJECT_STATUS.md`
- Modify: `docs/TESTSZENARIEN.md`

- [ ] **Step 1: PROJECT_STATUS.md ergaenzen**

In `PROJECT_STATUS.md` einen v1.20.0-Eintrag im oeben "Aktueller Stand"-Abschnitt einfuegen:

```markdown
### v1.20.0 (2026-06-03) — Feingranulare Berechtigungen

- Read/Edit-Split in allen 10 Stammdaten-Sichten via neuer Rolle `masterdata_read`
- Lager-Worklist (`WarehousePicking`, `MissingPartsLager`) jetzt nur fuer
  Lager-Mitarbeiter (`stock`, `stock_keyuser`) + Admin — picker explizit raus
- Neue Rollen-Uebersichts-Seite `/Users/RoleOverview` (hand-gepflegt)
- Migration `AddMasterDataReadRole` legt die neue Rolle als Systemrolle an
- 2 neue View-Helper in `CurrentUserService`: `HasMasterDataReadAccessAsync`,
  `CanProcessLagerAsync`
```

- [ ] **Step 2: TESTSZENARIEN.md — neues Kapitel anhaengen**

In `docs/TESTSZENARIEN.md` am Ende einen neuen Kapitel-Block hinzufuegen:

```markdown
## Kapitel 32: Feingranulare Berechtigungen (v1.20.0)

### 32.1 Read-Only-User in Stammdaten

**Vorbedingungen:**
- Admin angelegt einen neuen User "test_read" mit nur der Rolle `masterdata_read`
- "test_read" ist eingeloggt

**Schritte:**
1. Im Hauptmenue auf "Stammdaten" klicken → Eintraege sichtbar (Benutzer, Rollen, Arbeitsplaetze, Einstellungen, ...)
2. Auf "Benutzer" klicken → Index-View laedt
3. Pruefen: "Neu"-Button ist NICHT vorhanden. "Bearbeiten"-Link pro Zeile ist NICHT vorhanden. "Rollen-Uebersicht"-Button ist sichtbar
4. URL direkt `/Users/Create` aufrufen → 403 Forbidden
5. Auf "Einstellungen" klicken → Index-View mit Banner "Nur-Lesen-Modus" oben
6. Alle Input-Felder sind disabled (grau, nicht editierbar). Save-Button ist NICHT vorhanden

**Erwartet:** Alle Read-Aktionen funktionieren, alle Schreib-Aktionen sind blockiert (UI + Server).

### 32.2 Picker verliert Lager-Worklist-Zugriff

**Vorbedingungen:**
- User "test_picker" hat nur die Rolle `picking`
- Es existieren Lagerbestellungen im Status `Submitted`

**Schritte:**
1. "test_picker" loggt sich ein
2. Im Hauptmenue auf "Bestellungen" klicken
3. Pruefen: "Meine Bestellungen" + "Meine Fehlteile" sind sichtbar. "Lager: Eingehende Listen" + "Lager: Fehlteile" sind NICHT sichtbar
4. URL direkt `/WarehousePicking/Index` aufrufen → 403 Forbidden
5. URL direkt `/MissingPartsLager/Index` aufrufen → 403 Forbidden
6. "Bestandsuebersicht" im Lager-Menue ist weiterhin sichtbar + funktioniert
7. "Bewegungshistorie" im Lager-Menue ist weiterhin sichtbar + funktioniert

**Erwartet:** Picker behaelt Bestand + Bewegungen, verliert Worklist-Sichten.

### 32.3 Multi-Rollen-User

**Vorbedingungen:**
- User "test_multi" hat Rollen `picking` + `masterdata_read`

**Schritte:**
1. Login als "test_multi"
2. Stammdaten-Menue ist sichtbar (Read), Edit-Buttons sind ausgeblendet
3. Komm.-Sichten (Meine Bestellungen, Picking) sind sichtbar
4. Lager-Worklist ist NICHT sichtbar (kein stock/stock_keyuser)

**Erwartet:** Union der Rechte — alles was eine der Rollen erlaubt.

### 32.4 Rollen-Uebersicht-Seite

**Vorbedingungen:**
- Login als beliebiger User mit `masterdata_read` oder `masterdata` oder `admin`

**Schritte:**
1. Auf Users/Index navigieren
2. Button "Rollen-Uebersicht" oben klicken
3. Seite `/Users/RoleOverview` laedt
4. Tabelle mit 13 Rollen sichtbar (admin, masterdata, masterdata_read, picking, stock, stock_keyuser, tracking, reporting, leitstand, fa_completion, bde_user, bde_shiftlead, bde_admin)
5. Pro Rolle: Beschreibung + Liste sichtbarer Seiten/Aktionen
6. Erklaer-Header oben erklaert Many-to-Many + Union-Semantik
7. Button "Zurueck zur Benutzerliste" funktioniert

**Erwartet:** Alle 13 Rollen-Zeilen mit korrekten Beschreibungen.

### 32.5 Migration

**Vorbedingungen:**
- Update auf v1.20.0 mit Backup-DB

**Schritte:**
1. App startet → Migration `AddMasterDataReadRole` laeuft
2. In SQL pruefen: `SELECT * FROM Roles WHERE [Key]='masterdata_read'` liefert 1 Zeile
3. Felder: `Name='Stammdaten ansehen'`, `IsSystem=1`, `SortOrder=5`, `AdGroup=NULL`
4. Bestehende User mit `masterdata` haben weiterhin Vollzugriff
5. Im Roles/Index ist die neue Rolle sichtbar (zwischen masterdata und picking sortiert)
6. Versuch die Rolle zu loeschen → blockt wegen `IsSystem=1`

**Erwartet:** Migration laeuft idempotent. Rolle ist als Systemrolle markiert.
```

- [ ] **Step 3: Commit**

```bash
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd add PROJECT_STATUS.md docs/TESTSZENARIEN.md
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd commit -m "docs: PROJECT_STATUS + TESTSZENARIEN Kapitel 32

Manuelle Test-Szenarien: Read-Only-User, Picker-Sperre, Multi-Rolle,
RoleOverview-Klick, Migration-Sanity."
```

---

## Task 16: Final-Check Build + Tests + Sanity-Run

**Files:** keine — nur Verifikation.

- [ ] **Step 1: Vollstaendiger Build**

```bash
cd C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd
dotnet build IdealAkeWms.slnx 2>&1 | tail -3
```

Expected: `0 Fehler`. Warnungen aus dem v1.19.0-Baseline (NU1902 fuer MimeKit/MailKit, CS8602 TrackingController) sind erlaubt.

- [ ] **Step 2: Vollstaendige Test-Suite**

```bash
cd C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd
dotnet test IdealAkeWms.slnx --no-build 2>&1 | tail -4
```

Expected: Web `> 643` (neue Tests) + Service `99` alle gruen. Genaue Zahlen:
- +5 RequireMasterDataReadAccess-Filter-Tests
- +5 RequireLagerProcessingAccess-Filter-Tests
- +12 CurrentUserService-Theory-Cases (2 Methoden, je 6 InlineData)
- +1 UsersController.RoleOverview-Test
- = **mind. 666 Web-Tests** gruen.

- [ ] **Step 3: Sanity-Scan auf zurueckgebliebene `[RequireMasterDataAccess]` Class-Level**

```bash
cd C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd
grep -lE "^\[RequireMasterDataAccess\]" IdealAkeWms/Controllers/*.cs
```

Expected: Keine Treffer (alle 10 Controller haben jetzt Class-Level `[RequireMasterDataReadAccess]`).

- [ ] **Step 4: Sanity-Scan auf zurueckgebliebene `[RequireStockAccess]` an WarehousePicking + MissingPartsLager**

```bash
cd C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd
grep -E "RequireStockAccess" IdealAkeWms/Controllers/WarehousePickingController.cs IdealAkeWms/Controllers/MissingPartsLagerController.cs
```

Expected: Keine Treffer. (StockMovementsController + StockOverviewController behalten `[RequireStockAccess]` — picker dort weiter erlaubt.)

- [ ] **Step 5: Sanity-Scan auf neue Filter-Anwendung**

```bash
cd C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd
grep -lE "RequireMasterDataReadAccess|RequireLagerProcessingAccess" IdealAkeWms/Controllers/*.cs
```

Expected: 12 Files (10 Stammdaten + WarehousePicking + MissingPartsLager).

- [ ] **Step 6: git log Zusammenfassung**

```bash
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd log --oneline 2899fea..HEAD
```

Expected: ~14 neue Commits (Task 1 bis Task 15).

- [ ] **Step 7: Status sauber**

```bash
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd status
```

Expected: `nothing to commit, working tree clean`.

---

## Task 17: PAUSE — User-Bestaetigung + Merge in main

**WICHTIG:** Diese Task NICHT autonom ausfuehren. Auf User-Bestaetigung warten (manueller Test der UI-Aenderungen mit den 3 Personas).

- [ ] **Step 1: Status-Report an User**

Praesentiere dem User:
- Anzahl neuer Commits (`git log 2899fea..HEAD --oneline`)
- Test-Zaehlung (Web/Service)
- Manuelle Test-Szenarien aus TESTSZENARIEN.md Kapitel 32
- Erinnere an Memory-Regel: Worktree wird NICHT automatisch nach Merge geloescht — User muss explizit "Worktree raeumen" sagen

- [ ] **Step 2: Auf User-Bestaetigung warten**

User entweder:
- "Mergen": → weiter zu Step 3
- "Erst manuell testen": → Pause bis Feedback
- "Aenderung X": → spezifische Fix-Iteration, dann zurueck zu Task 16 Step 2

- [ ] **Step 3: Merge in main (nur nach Bestaetigung)**

```bash
cd C:/Git/IDEAL-AKE-WMS
git checkout main
git pull
git merge --no-ff bugfix/missingparts-include-pd -m "Merge branch 'bugfix/missingparts-include-pd' — v1.19.0 + v1.20.0"
git push
```

- [ ] **Step 4: Worktree-Cleanup (NUR nach expliziter User-Nachfrage)**

Per Memory-Feedback `feedback_worktree_cleanup_ask_first.md`: NICHT automatisch loeschen. Erst nach expliziter "Worktree raeumen"-Bestaetigung:

```bash
git worktree remove C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd
git branch -d bugfix/missingparts-include-pd
```

---

## Self-Review (vom Plan-Autor durchlaufen)

**Spec coverage:**
- Sektion 1 Goal #1 Read/Edit-Split → Tasks 1, 4, 5, 6, 7
- Sektion 1 Goal #2 Lager-Filter → Tasks 1, 8, 12
- Sektion 1 Goal #3 Rollen-Uebersicht → Tasks 4, 9, 10
- Sektion 4.1 neue Rolle → Task 3
- Sektion 4.2 Filter-Attribute → Task 1
- Sektion 4.3 Filter-Anwendung pro Controller → Tasks 4-8
- Sektion 4.4 Rollen-Uebersicht-Seite → Task 9
- Sektion 4.5 Button-Verstecken → Tasks 10, 11
- Sektion 4.6 Layout-Menue → Task 12
- Sektion 4.7 CurrentUserService-Helper → Task 2
- Sektion 4.8 API Out-of-Scope → explizit in Plan-Header (keine Tasks)
- Sektion 5 Migration → Task 3
- Sektion 6 Tests → Tasks 1, 2, 9 (TDD), Task 16 (Aggregat)
- Sektion 7 Doku → Tasks 13, 14, 15

**Placeholder scan:** 1 Stelle mit `<timestamp>` (Task 3 Step 3/4) — bewusst, weil der EF-Tool den Timestamp generiert. Subagent muss ihn 1:1 aus der EF-Datei kopieren.

**Type consistency:**
- `HasMasterDataReadAccessAsync` Name konsistent in Tasks 2, 10, 11, 12, 14
- `CanProcessLagerAsync` Name konsistent in Tasks 2, 12, 14
- `RequireMasterDataReadAccessAttribute` Klassenname konsistent
- `RequireLagerProcessingAccessAttribute` Klassenname konsistent
- `RoleKeys.MasterDataRead = "masterdata_read"` konsistent (Konstante vs DB-Key)

Keine offenen Punkte.
