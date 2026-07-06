# Glas-Bestellung (v1.25.0) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Zweiter Bestelltyp „Glas" im Lagerbestellungs-Modul — Typ pro Bestellung (Enum), Rolle `glasbestellung`, getrennte Empfänger-Mail je Typ, erzwungene Artikelgruppen-Trennung (Glas-Gruppen + gemeinsame Gruppen/EUZ), Lager/Glas-Reiter in WarehouseRequisitions, WarehousePicking, MissingParts, MissingPartsLager.

**Architektur:** Additive Spalte `WarehouseRequisitions.Type` (Enum `WarehouseRequisitionType`, Default Lager) + neue Rolle in EINER Migration 77. Artikelgruppen-Regel als pure-static Helper `GlasArticleGroupFilter` (TDD), durchgesetzt in Artikel-Suche UND AddItem-API. Empfänger-Wahl beim Submit nach Typ (2. AppSetting). Reiter = `?type=`-Query-Param, server-seitig gefiltert.

**Tech Stack:** ASP.NET Core 10 MVC, EF Core 10 (SQL Server + InMemory-Tests), xUnit + FluentAssertions + Moq.

**Spec:** `docs/superpowers/specs/2026-07-03-glas-bestellung-design.md`
**Worktree:** `C:\Git\IDEAL-AKE-WMS\.claude\worktrees\glas-bestellung` (Branch `feature/glas-bestellung`)

**Wichtige Projektregeln:**
- Alle Pfade relativ zum Worktree-Root. IMMER im Worktree arbeiten (`Set-Location` vor git).
- Commits: PowerShell here-string `@'…'@` (schliessendes `'@` auf Spalte 0), Trailer `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`.
- Build: `dotnet build IdealAkeWms.sln` · Web-Tests: `dotnet test IdealAkeWms.Tests` · Service-Tests: `dotnet test IDEALAKEWMSService.Tests`.
- InMemory: kein rowversion → Tests nutzen `TestDbContextFactory.Create()` (liefert `TestApplicationDbContext`).
- UI-Texte Deutsch, Code Englisch. TempData nur `SuccessMessage`/`WarningMessage`.

---

## Datei-Landkarte (was wird angefasst)

| Datei | Aktion |
|---|---|
| `IdealAkeWms/Models/WarehouseRequisitionType.cs` | NEU (Enum) |
| `IdealAkeWms/Models/WarehouseRequisition.cs` | +`Type`-Property |
| `IdealAkeWms/Migrations/*_AddWarehouseRequisitionTypeAndGlasRole.cs` | NEU (Migration 77: Spalte + Rolle) |
| `SQL/77_AddWarehouseRequisitionTypeAndGlasRole.sql` | NEU (idempotent) |
| `SQL/00_FreshInstall.sql` | Spalte + Rolle + History-Insert |
| `IdealAkeWms/Models/RoleKeys.cs` | +`Glasbestellung` |
| `IdealAkeWms/Services/ICurrentUserService.cs` + `CurrentUserService.cs` | +3 Helper |
| `IdealAkeWms/Filters/RequirePickingOrStockOrLagerbestellungAccessAttribute.cs` | +Glas-Bedingung |
| `IdealAkeWms/Filters/RequireStockOrLagerbestellungAccessAttribute.cs` | +Glas-Bedingung |
| `IdealAkeWms/Services/GlasArticleGroupFilter.cs` | NEU (pure Helper) |
| `IdealAkeWms.Tests/Services/GlasArticleGroupFilterTests.cs` | NEU |
| `IdealAkeWms/Models/AppSettingKeys.cs` | +3 Keys |
| `IdealAkeWms/Program.cs` | Seed 3 AppSettings |
| `IdealAkeWms/Views/Settings/Index.cshtml` | 3 Keys in Gruppe „Bestellungen" |
| `IdealAkeWms/Data/Repositories/IWarehouseRequisitionRepository.cs` + `WarehouseRequisitionRepository.cs` | Signaturen: CreateDraft/GetForWarehouse/GetMissingParts + Typ |
| `IdealAkeWms/Controllers/ArticlesApiController.cs` | Search `type`-Param |
| `IdealAkeWms/Controllers/Api/WarehouseRequisitionsApiController.cs` | AddItem-Enforcement |
| `IdealAkeWms/Controllers/WarehouseRequisitionsController.cs` | Index/CreateDraft/Edit/Submit typ-fähig |
| `IdealAkeWms/Models/ViewModels/WarehouseRequisitionListViewModel.cs` | +ActiveType/CanOrder* |
| `IdealAkeWms/Models/ViewModels/WarehouseRequisitionEditViewModel.cs` | +Type |
| `IdealAkeWms/Models/ViewModels/MissingPartsListViewModel.cs` | +ActiveType/CanOrder* |
| `IdealAkeWms/Controllers/WarehousePickingController.cs` | Typ-Param + Counts |
| `IdealAkeWms/Controllers/MissingPartsController.cs` + `MissingPartsLagerController.cs` | Typ-Param |
| `IdealAkeWms/Views/WarehouseRequisitions/Index.cshtml` + `Edit.cshtml` | Tabs / Badge / Such-Param |
| `IdealAkeWms/Views/WarehousePicking/Index.cshtml` | Tabs |
| `IdealAkeWms/Views/MissingParts/Index.cshtml` + `Views/MissingPartsLager/Index.cshtml` | äußere Typ-Tabs |
| `IDEALAKEWMSService/Services/WarehouseRequisitionEmailService.cs` | Typ-Label |
| `IdealAkeWms/Views/Shared/_Layout.cshtml` | Menü-Gating |
| `IdealAkeWms/Views/Users/RoleOverview.cshtml` | Rolle + Filter-Zeilen |
| `IdealAkeWms/AppVersion.cs` + `IDEALAKEWMSService/AppVersion.cs` | v1.25.0 |
| Doku: `Views/Help/Changelog.cshtml`, Hilfeseite, `docs/TESTSZENARIEN.md`, `CLAUDE.md`, `PROJECT_STATUS.md` | v1.25.0 |

Tests: bestehende Dateien `IdealAkeWms.Tests/Controllers/WarehouseRequisitionsControllerTests.cs`, `IdealAkeWms.Tests/Repositories/WarehouseRequisitionRepositoryTests.cs`, `IDEALAKEWMSService.Tests/Services/WarehouseRequisitionEmailServiceTests.cs` erweitern; neue Datei für GlasArticleGroupFilter.

---

### Task 0: Pre-Flight Baseline

- [ ] **Step 0.1:** `Set-Location C:\Git\IDEAL-AKE-WMS\.claude\worktrees\glas-bestellung`; `git status --short` (muss leer sein), `git log --oneline -1` (erwartet `16ea5bc`).
- [ ] **Step 0.2:** `dotnet build IdealAkeWms.sln` → 0 Errors.
- [ ] **Step 0.3:** `dotnet test IdealAkeWms.Tests --nologo` + `dotnet test IDEALAKEWMSService.Tests --nologo` → alle grün. Anzahl notieren (Baseline).

---

### Task 1: Enum + Model + Migration 77 + SQL/77 + FreshInstall

**Files:**
- Create: `IdealAkeWms/Models/WarehouseRequisitionType.cs`
- Modify: `IdealAkeWms/Models/WarehouseRequisition.cs`
- Create (generiert): `IdealAkeWms/Migrations/<ts>_AddWarehouseRequisitionTypeAndGlasRole.cs`
- Create: `SQL/77_AddWarehouseRequisitionTypeAndGlasRole.sql`
- Modify: `SQL/00_FreshInstall.sql`

- [ ] **Step 1.1: Enum anlegen** — `IdealAkeWms/Models/WarehouseRequisitionType.cs`:

```csharp
namespace IdealAkeWms.Models;

/// <summary>
/// Bestelltyp einer Lagerbestellung: normale Lager-Bestellung oder Glas-Bestellung.
/// Steuert Empfaenger-Gruppe (Mail) und erlaubte Artikelgruppen (v1.25.0).
/// </summary>
public enum WarehouseRequisitionType
{
    Lager = 1,
    Glas = 2
}
```

- [ ] **Step 1.2: Property ergänzen** — in `IdealAkeWms/Models/WarehouseRequisition.cs` direkt NACH der Zeile `public WarehouseRequisitionStatus Status { get; set; } = WarehouseRequisitionStatus.Draft;` einfügen:

```csharp
    /// <summary>Bestelltyp (Lager/Glas, v1.25.0). Wird bei Anlage gesetzt, kein Wechsel.</summary>
    public WarehouseRequisitionType Type { get; set; } = WarehouseRequisitionType.Lager;
```

- [ ] **Step 1.3: EF-Migration generieren** — im Worktree:
`dotnet ef migrations add AddWarehouseRequisitionTypeAndGlasRole --project IdealAkeWms`
Erwartet: `Up()` enthält `migrationBuilder.AddColumn<int>(name: "Type", table: "WarehouseRequisitions", type: "int", nullable: false, defaultValue: 1);` (Enum→int Default-Konvention wie `Status`). Generierten Typ/Default INSPIZIEREN und notieren — SQL/77 + FreshInstall müssen exakt matchen.

- [ ] **Step 1.4: Rolle in dieselbe Migration** — in der generierten Migration am ENDE von `Up()` ergänzen (Muster = `20260619063919_AddLagerbestellungRole.cs`):

```csharp
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM Roles WHERE [Key] = 'glasbestellung')
BEGIN
    INSERT INTO Roles ([Key], [Name], [Description], [IsSystem], [SortOrder],
                       [CreatedAt], [CreatedBy], [CreatedByWindows])
    VALUES ('glasbestellung', 'Glasbestellungen',
            'Glas-Bestellungen erfassen und eigene Fehlteile verfolgen (Zugriff nur auf Lagerbestellungen (Reiter Glas) + Meine Fehlteile).',
            1, 9,
            SYSDATETIME(), 'system', 'system')
END
");
```

und in `Down()` VOR dem DropColumn: `migrationBuilder.Sql(@"DELETE FROM Roles WHERE [Key] = 'glasbestellung'");`

- [ ] **Step 1.5: SQL/77 schreiben** — `SQL/77_AddWarehouseRequisitionTypeAndGlasRole.sql` (Muster = `SQL/74_AddLagerbestellungRole.sql`; MigrationId = exakter Dateiname-Timestamp aus Step 1.3):

```sql
-- ============================================================================
-- Migration 77: AddWarehouseRequisitionTypeAndGlasRole (v1.25.0)
-- ============================================================================
-- 1) Spalte WarehouseRequisitions.Type (1=Lager, 2=Glas), Default 1.
-- 2) Rolle 'glasbestellung'. Idempotent.
-- ============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF COL_LENGTH('dbo.WarehouseRequisitions', 'Type') IS NULL
    BEGIN
        ALTER TABLE [dbo].[WarehouseRequisitions]
            ADD [Type] INT NOT NULL CONSTRAINT [DF_WarehouseRequisitions_Type] DEFAULT 1;
        PRINT 'Spalte WarehouseRequisitions.Type angelegt.';
    END

    IF NOT EXISTS (SELECT 1 FROM Roles WHERE [Key] = 'glasbestellung')
    BEGIN
        INSERT INTO Roles ([Key], [Name], [Description], [IsSystem], [SortOrder],
                           [CreatedAt], [CreatedBy], [CreatedByWindows])
        VALUES ('glasbestellung', 'Glasbestellungen',
                'Glas-Bestellungen erfassen und eigene Fehlteile verfolgen (Zugriff nur auf Lagerbestellungen (Reiter Glas) + Meine Fehlteile).',
                1, 9,
                SYSDATETIME(), 'system', 'system');
        PRINT 'Rolle glasbestellung angelegt.';
    END

    IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '<TIMESTAMP>_AddWarehouseRequisitionTypeAndGlasRole')
    BEGIN
        INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
        VALUES ('<TIMESTAMP>_AddWarehouseRequisitionTypeAndGlasRole', '10.0.2');
    END

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH
```

`<TIMESTAMP>` = echter Wert aus Step 1.3 (z. B. `20260703xxxxxx`). Der EF-Default-Constraint-Name kann abweichen (EF generiert eigenen Namen) — der Guard ist `COL_LENGTH`, daher unkritisch; Default-WERT (1) und Typ (INT NOT NULL) müssen matchen.

- [ ] **Step 1.6: FreshInstall aktualisieren** — `SQL/00_FreshInstall.sql`, DREI Stellen:
  1. Im `CREATE TABLE`-Block für `WarehouseRequisitions` (Suchen: `CREATE TABLE [dbo].[WarehouseRequisitions]`): nach der `[Status]`-Spalte einfügen: `    [Type] INT NOT NULL CONSTRAINT [DF_WarehouseRequisitions_Type] DEFAULT 1,` (Stil an Nachbarspalten anpassen).
  2. Nach dem `lagerbestellung`-Rollen-Block (Zeile ~1333–1344) analogen Block für `glasbestellung` (Text wie SQL/77, `GETDATE()` statt `SYSDATETIME()` — Stil des FreshInstall-Blocks spiegeln, SortOrder 9).
  3. Im `__EFMigrationsHistory`-INSERT-Block am Ende: neue Zeile `('<TIMESTAMP>_AddWarehouseRequisitionTypeAndGlasRole', N'10.0.2'),` in der bestehenden Werteliste (Formatierung der Nachbarzeilen exakt spiegeln).

- [ ] **Step 1.7: Verifizieren** — `dotnet build IdealAkeWms.sln` (0 Errors) und `dotnet ef migrations has-pending-model-changes --project IdealAkeWms` → „No changes have been made to the model...". `dotnet test IdealAkeWms.Tests --nologo` → grün (Type-Default 1 bricht nichts).

- [ ] **Step 1.8: Commit** — `feat(model): WarehouseRequisitionType (Lager/Glas) + Rolle glasbestellung (Migration 77)`

---

### Task 2: RoleKeys + CurrentUserService-Helper + Zugriffsfilter (TDD)

**Files:**
- Modify: `IdealAkeWms/Models/RoleKeys.cs`, `IdealAkeWms/Services/ICurrentUserService.cs`, `IdealAkeWms/Services/CurrentUserService.cs`
- Modify: `IdealAkeWms/Filters/RequirePickingOrStockOrLagerbestellungAccessAttribute.cs`, `IdealAkeWms/Filters/RequireStockOrLagerbestellungAccessAttribute.cs`
- Test: bestehende CurrentUserService-Tests erweitern (Datei via `grep -r "CanAccessLagerbestellungAsync" IdealAkeWms.Tests` finden — Tests aus v1.23.0 Task „RoleKeys + CanAccessLagerbestellungAsync (TDD)" existieren dort)

- [ ] **Step 2.1: Failing Tests schreiben** — in der gefundenen Test-Datei die bestehenden `CanAccessLagerbestellungAsync`-Tests EXAKT duplizieren (gleiche Konstruktion/Fakes) für:
  - `CanAccessGlasbestellungAsync`: true für `admin`, true für `glasbestellung`, false für `lagerbestellung`/`stock`/`picking`/keine Rolle.
  - `CanOrderLagerAsync`: true für `admin`/`picking`/`stock`/`stock_keyuser`/`lagerbestellung`; false für `glasbestellung` allein und keine Rolle.
  - `CanOrderGlasAsync`: true für `admin`/`picking`/`stock`/`stock_keyuser`/`glasbestellung`; false für `lagerbestellung` allein und keine Rolle.
- [ ] **Step 2.2:** `dotnet test IdealAkeWms.Tests --filter "CanOrder|CanAccessGlas" --nologo` → FAIL (Methoden existieren nicht → Compile-Error zählt als fail; danach Stubs).
- [ ] **Step 2.3: Implementieren** —

`RoleKeys.cs` (nach `Lagerbestellung`):
```csharp
    public const string Glasbestellung = "glasbestellung";
```

`ICurrentUserService.cs` (nach `CanAccessLagerbestellungAsync();`):
```csharp
    Task<bool> CanAccessGlasbestellungAsync();
    /// <summary>Darf der User Lager-Bestellungen sehen/anlegen (Reiter Lager)?</summary>
    Task<bool> CanOrderLagerAsync();
    /// <summary>Darf der User Glas-Bestellungen sehen/anlegen (Reiter Glas)?</summary>
    Task<bool> CanOrderGlasAsync();
```

`CurrentUserService.cs` (nach `CanAccessLagerbestellungAsync`-Impl):
```csharp
    public async Task<bool> CanAccessGlasbestellungAsync()
        => await HasAnyRoleAsync(RoleKeys.Admin, RoleKeys.Glasbestellung);

    public async Task<bool> CanOrderLagerAsync()
        => await HasAnyRoleAsync(RoleKeys.Admin, RoleKeys.Picking, RoleKeys.Stock,
            RoleKeys.StockKeyUser, RoleKeys.Lagerbestellung);

    public async Task<bool> CanOrderGlasAsync()
        => await HasAnyRoleAsync(RoleKeys.Admin, RoleKeys.Picking, RoleKeys.Stock,
            RoleKeys.StockKeyUser, RoleKeys.Glasbestellung);
```

- [ ] **Step 2.4: Beide Filter erweitern** — in `RequirePickingOrStockOrLagerbestellungAccessFilter.OnActionExecutionAsync` die Bedingung ersetzen:

```csharp
        if (!await _currentUserService.CanPickAsync()
            && !await _currentUserService.CanAccessStockAsync()
            && !await _currentUserService.CanAccessLagerbestellungAsync()
            && !await _currentUserService.CanAccessGlasbestellungAsync())
```

Analog in `RequireStockOrLagerbestellungAccessFilter`:
```csharp
        if (!await _currentUserService.CanAccessStockAsync()
            && !await _currentUserService.CanAccessLagerbestellungAsync()
            && !await _currentUserService.CanAccessGlasbestellungAsync())
```
Jeweils XML-/Klassen-Kommentar ergänzen: „seit v1.25.0 auch glasbestellung". Filter-NAMEN bleiben unverändert (bewusst, kleinster Diff — Doku in Task 13).

- [ ] **Step 2.5:** Tests laufen lassen → PASS. Kompletter Testlauf `dotnet test IdealAkeWms.Tests --nologo` grün.
- [ ] **Step 2.6: Commit** — `feat(auth): Rolle glasbestellung — Helper CanOrderLager/CanOrderGlas + Filter-Erweiterung`

---

### Task 3: GlasArticleGroupFilter (TDD, pure)

**Files:**
- Create: `IdealAkeWms/Services/GlasArticleGroupFilter.cs`
- Create: `IdealAkeWms.Tests/Services/GlasArticleGroupFilterTests.cs`

- [ ] **Step 3.1: Failing Tests** — `IdealAkeWms.Tests/Services/GlasArticleGroupFilterTests.cs`:

```csharp
using FluentAssertions;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using Xunit;

namespace IdealAkeWms.Tests.Services;

public class GlasArticleGroupFilterTests
{
    private static readonly IReadOnlySet<string> Glas = GlasArticleGroupFilter.ParseGroups("GLAS,SPIEGEL");
    private static readonly IReadOnlySet<string> Shared = GlasArticleGroupFilter.ParseGroups("EUZ");

    [Theory]
    [InlineData("940", WarehouseRequisitionType.Lager, true)]   // normale Gruppe -> Lager ok
    [InlineData("940", WarehouseRequisitionType.Glas, false)]   // normale Gruppe -> kein Glas
    [InlineData("GLAS", WarehouseRequisitionType.Glas, true)]   // Glas-Gruppe -> Glas ok
    [InlineData("GLAS", WarehouseRequisitionType.Lager, false)] // Glas-Gruppe -> im Lager ausgenommen
    [InlineData("SPIEGEL", WarehouseRequisitionType.Glas, true)]
    [InlineData("EUZ", WarehouseRequisitionType.Lager, true)]   // gemeinsame Gruppe -> beide
    [InlineData("EUZ", WarehouseRequisitionType.Glas, true)]
    public void IsAllowedForType_Basisfaelle(string group, WarehouseRequisitionType type, bool expected)
        => GlasArticleGroupFilter.IsAllowedForType(group, type, Glas, Shared).Should().Be(expected);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsAllowedForType_LeereGruppe_NurLager(string? group)
    {
        GlasArticleGroupFilter.IsAllowedForType(group, WarehouseRequisitionType.Lager, Glas, Shared).Should().BeTrue();
        GlasArticleGroupFilter.IsAllowedForType(group, WarehouseRequisitionType.Glas, Glas, Shared).Should().BeFalse();
    }

    [Theory]
    [InlineData(" glas ")]                 // Trim + Case
    [InlineData("GLAS - Glasteile")]       // Sage-Format "Code - Name"
    [InlineData("glas - glasteile")]
    public void IsAllowedForType_Normalisierung(string group)
        => GlasArticleGroupFilter.IsAllowedForType(group, WarehouseRequisitionType.Glas, Glas, Shared).Should().BeTrue();

    [Fact]
    public void LeereGlasKonfig_GlasZeigtNurGemeinsame_LagerAlles()
    {
        var empty = GlasArticleGroupFilter.ParseGroups("");
        GlasArticleGroupFilter.IsAllowedForType("940", WarehouseRequisitionType.Glas, empty, Shared).Should().BeFalse();
        GlasArticleGroupFilter.IsAllowedForType("EUZ", WarehouseRequisitionType.Glas, empty, Shared).Should().BeTrue();
        GlasArticleGroupFilter.IsAllowedForType("940", WarehouseRequisitionType.Lager, empty, Shared).Should().BeTrue();
        GlasArticleGroupFilter.IsAllowedForType("GLAS", WarehouseRequisitionType.Lager, empty, Shared).Should().BeTrue();
    }

    [Fact]
    public void ParseGroups_TrimtNormalisiertUndIgnoriertLeereTokens()
    {
        var set = GlasArticleGroupFilter.ParseGroups(" glas , ,SPIEGEL - Spiegelteile,euz ");
        set.Should().BeEquivalentTo(new[] { "GLAS", "SPIEGEL", "EUZ" });
    }
}
```

- [ ] **Step 3.2:** `dotnet test IdealAkeWms.Tests --filter GlasArticleGroupFilter --nologo` → FAIL (Klasse fehlt).
- [ ] **Step 3.3: Implementieren** — `IdealAkeWms/Services/GlasArticleGroupFilter.cs`:

```csharp
using IdealAkeWms.Models;

namespace IdealAkeWms.Services;

/// <summary>
/// Regel fuer die Artikelgruppen-Trennung Lager vs. Glas (v1.25.0).
/// Glas-Bestellung: nur Gruppen aus GlasArtikelgruppen + GemeinsameArtikelgruppen.
/// Lager-Bestellung: alles AUSSER GlasArtikelgruppen; GemeinsameArtikelgruppen (z.B. EUZ)
/// sind IMMER in beiden erlaubt. Normalisierung: Trim, Uppercase, Sage-Suffix " - Name" ab.
/// </summary>
public static class GlasArticleGroupFilter
{
    public static string NormalizeGroup(string? group)
    {
        if (string.IsNullOrWhiteSpace(group)) return string.Empty;
        var g = group.Trim();
        // Sage liefert teils "940 - Kleinmaterial" — Articles speichert nur den Code.
        var dashIdx = g.IndexOf(" - ", StringComparison.Ordinal);
        if (dashIdx > 0) g = g[..dashIdx].Trim();
        return g.ToUpperInvariant();
    }

    public static HashSet<string> ParseGroups(string? csv)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(csv)) return set;
        foreach (var token in csv.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var n = NormalizeGroup(token);
            if (n.Length > 0) set.Add(n);
        }
        return set;
    }

    public static bool IsAllowedForType(string? articleGroup, WarehouseRequisitionType type,
        IReadOnlySet<string> glasGroups, IReadOnlySet<string> sharedGroups)
    {
        var g = NormalizeGroup(articleGroup);
        if (sharedGroups.Contains(g)) return true;
        return type == WarehouseRequisitionType.Glas
            ? glasGroups.Contains(g)
            : !glasGroups.Contains(g);
    }
}
```

- [ ] **Step 3.4:** Tests → PASS.
- [ ] **Step 3.5: Commit** — `feat(service): GlasArticleGroupFilter — Artikelgruppen-Regel Lager/Glas/gemeinsam (TDD)`

---

### Task 4: 3 neue AppSettings + Settings-Seite

**Files:**
- Modify: `IdealAkeWms/Models/AppSettingKeys.cs`, `IdealAkeWms/Program.cs`, `IdealAkeWms/Views/Settings/Index.cshtml`, ggf. `SQL/00_FreshInstall.sql`

- [ ] **Step 4.1: Keys** — in `AppSettingKeys.cs`, Block „Picking / Leitstand / Warehouse Requisitions", nach `DefaultLagerbestellempfaengerId`:

```csharp
    public const string DefaultGlasbestellempfaengerId = "DefaultGlasbestellempfaengerId";
    public const string GlasArtikelgruppen = "GlasArtikelgruppen";
    public const string GemeinsameArtikelgruppen = "GemeinsameArtikelgruppen";
```

- [ ] **Step 4.2: Seed** — in `IdealAkeWms/Program.cs` im Array `requisitionSettings` (Zeile ~290) nach dem `DefaultLagerbestellempfaengerId`-Tupel:

```csharp
    ("DefaultGlasbestellempfaengerId", "", "Default-OrderRecipientGroup-ID fuer Glas-Bestellungen (leer = Submit blockt)"),
    ("GlasArtikelgruppen", "", "Kommaseparierte Artikelgruppen fuer Glas-Bestellungen (in der Lager-Bestellung ausgenommen)"),
    ("GemeinsameArtikelgruppen", "EUZ", "Kommaseparierte Artikelgruppen, die in Lager- UND Glas-Bestellungen verfuegbar sind"),
```

- [ ] **Step 4.3: Settings-View** — in `Views/Settings/Index.cshtml` das Tupel `("Bestellungen", new[] { "BestellungenAktiv" })` ersetzen durch:

```csharp
    ("Bestellungen", new[] { "BestellungenAktiv", "DefaultLagerbestellempfaengerId", "DefaultGlasbestellempfaengerId", "GlasArtikelgruppen", "GemeinsameArtikelgruppen" }),
```
(Prüfen, ob `DefaultLagerbestellempfaengerId` bereits in einer anderen Gruppe gelistet ist — `grep DefaultLagerbestellempfaengerId Views/Settings/Index.cshtml`. Wenn ja, dort belassen und nur die 3 neuen Keys ergänzen; KEINE Doppel-Listung.)

- [ ] **Step 4.4: FreshInstall prüfen** — `grep -n "DefaultLagerbestellempfaengerId" SQL/00_FreshInstall.sql`. Wenn dort AppSettings-Inserts existieren: die 3 neuen Keys im selben Stil ergänzen. Wenn nicht: nichts tun (Program.cs-Seed reicht).
- [ ] **Step 4.5:** Build → 0 Errors. **Commit** — `feat(settings): AppSettings DefaultGlasbestellempfaengerId + GlasArtikelgruppen + GemeinsameArtikelgruppen`

---

### Task 5: Repository — Typ in CreateDraft/GetForWarehouse/GetMissingParts (TDD)

**Files:**
- Modify: `IdealAkeWms/Data/Repositories/IWarehouseRequisitionRepository.cs`, `WarehouseRequisitionRepository.cs`
- Test: `IdealAkeWms.Tests/Repositories/WarehouseRequisitionRepositoryTests.cs` (erweitern)
- Modify (Compile-Folge): alle Aufrufer der 3 Methoden (Controller — werden in Task 7/9/10 fachlich umgebaut; hier nur minimal kompilierfähig halten, siehe Step 5.4)

- [ ] **Step 5.1: Failing Tests** — in `WarehouseRequisitionRepositoryTests.cs` (bestehende Test-Konstruktion mit `TestDbContextFactory.Create()` spiegeln) drei Tests:

```csharp
    [Fact]
    public async Task CreateDraftAsync_SetztTyp()
    {
        using var ctx = TestDbContextFactory.Create();
        var wp = new ProductionWorkplace { Name = "WB1", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        ctx.ProductionWorkplaces.Add(wp); ctx.SaveChanges();
        var repo = new WarehouseRequisitionRepository(ctx);

        var id = await repo.CreateDraftAsync(wp.Id, WarehouseRequisitionType.Glas, 1, "t", "win");

        (await ctx.WarehouseRequisitions.FindAsync(id))!.Type.Should().Be(WarehouseRequisitionType.Glas);
    }

    [Fact]
    public async Task GetForWarehouseAsync_FiltertNachTyp()
    {
        using var ctx = TestDbContextFactory.Create();
        var wp = new ProductionWorkplace { Name = "WB1", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        ctx.ProductionWorkplaces.Add(wp); ctx.SaveChanges();
        ctx.WarehouseRequisitions.AddRange(
            new WarehouseRequisition { ProductionWorkplaceId = wp.Id, Status = WarehouseRequisitionStatus.Submitted, Type = WarehouseRequisitionType.Lager, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" },
            new WarehouseRequisition { ProductionWorkplaceId = wp.Id, Status = WarehouseRequisitionStatus.Submitted, Type = WarehouseRequisitionType.Glas, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        ctx.SaveChanges();
        var repo = new WarehouseRequisitionRepository(ctx);

        var (glasOnly, total) = await repo.GetForWarehouseAsync(
            new[] { WarehouseRequisitionStatus.Submitted }, null, WarehouseRequisitionType.Glas, 1, 50);

        total.Should().Be(1);
        glasOnly.Should().OnlyContain(r => r.Type == WarehouseRequisitionType.Glas);

        var (both, totalBoth) = await repo.GetForWarehouseAsync(
            new[] { WarehouseRequisitionStatus.Submitted }, null, null, 1, 50);
        totalBoth.Should().Be(2);
    }

    [Fact]
    public async Task GetMissingPartsAsync_FiltertNachTyp()
    {
        using var ctx = TestDbContextFactory.Create();
        var wp = new ProductionWorkplace { Name = "WB1", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        ctx.ProductionWorkplaces.Add(wp); ctx.SaveChanges();
        var lager = new WarehouseRequisition { ProductionWorkplaceId = wp.Id, Status = WarehouseRequisitionStatus.PartiallyDelivered, Type = WarehouseRequisitionType.Lager, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        var glas  = new WarehouseRequisition { ProductionWorkplaceId = wp.Id, Status = WarehouseRequisitionStatus.PartiallyDelivered, Type = WarehouseRequisitionType.Glas,  CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        ctx.WarehouseRequisitions.AddRange(lager, glas); ctx.SaveChanges();
        ctx.WarehouseRequisitionItems.AddRange(
            new WarehouseRequisitionItem { WarehouseRequisitionId = lager.Id, ArticleNumber = "A1", ArticleDescription = "x", QuantityRequested = 1, Position = 1, ShortageStatus = ShortageStatus.WillBeRestocked, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" },
            new WarehouseRequisitionItem { WarehouseRequisitionId = glas.Id, ArticleNumber = "G1", ArticleDescription = "x", QuantityRequested = 1, Position = 1, ShortageStatus = ShortageStatus.WillBeRestocked, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        ctx.SaveChanges();
        var repo = new WarehouseRequisitionRepository(ctx);

        var (items, totalCount) = await repo.GetMissingPartsAsync(
            ShortageStatus.WillBeRestocked, WarehouseRequisitionType.Glas, null, null, null, null, 1, 50);

        totalCount.Should().Be(1);
        items.Should().OnlyContain(i => i.ArticleNumber == "G1");
    }
```
(Property-Namen der Testdaten an bestehende Tests in der Datei angleichen — bestehende Helper/Builder wiederverwenden, falls vorhanden.)

- [ ] **Step 5.2:** Testlauf → FAIL (Signaturen existieren nicht).
- [ ] **Step 5.3: Implementieren** — Interface + Impl:
  - `CreateDraftAsync(int productionWorkplaceId, WarehouseRequisitionType type, int currentUserId, string currentUserName, string windowsUserName)` — im Objekt-Initializer `Type = type,` nach `Status = ...` ergänzen.
  - `GetForWarehouseAsync(WarehouseRequisitionStatus[] statuses, int? workplaceId, WarehouseRequisitionType? type, int page, int pageSize)` — nach dem workplaceId-Filter: `if (type.HasValue) q = q.Where(r => r.Type == type.Value);`
  - `GetMissingPartsAsync(ShortageStatus filterStatus, WarehouseRequisitionType? type, int? workplaceFilter, ...)` — in die Basis-Query: `if (type.HasValue) q = q.Where(i => i.WarehouseRequisition.Type == type.Value);` (direkt nach dem Status/Closed-Where).
  - `GetForUserAsync` + `GetShortageCountsForUserAsync` bleiben UNVERÄNDERT (Index filtert in-memory; Zähler bewusst typ-übergreifend — Spec §out-of-scope).
- [ ] **Step 5.4: Aufrufer kompilierfähig halten** — Compile-Errors zeigen alle Stellen:
  - `WarehouseRequisitionsController.CreateDraft`: vorerst `WarehouseRequisitionType.Lager` durchreichen (fachlich in Task 7).
  - `WarehousePickingController.Index`: beide `GetForWarehouseAsync`-Aufrufe vorerst `null` als type (fachlich in Task 9).
  - `MissingPartsController` + `MissingPartsLagerController`: `GetMissingPartsAsync`-Aufrufe vorerst `null` (fachlich Task 10).
- [ ] **Step 5.5:** Alle Tests grün (Baseline + 3 neue). **Commit** — `feat(repo): WarehouseRequisitionType-Filter in CreateDraft/GetForWarehouse/GetMissingParts (TDD)`

---

### Task 6: Enforcement — Artikel-Suche `type`-Param + AddItem-Prüfung (TDD)

**Files:**
- Modify: `IdealAkeWms/Controllers/ArticlesApiController.cs`, `IdealAkeWms/Controllers/Api/WarehouseRequisitionsApiController.cs`
- Test: `IdealAkeWms.Tests` — bestehende Api-Controller-Tests erweitern bzw. neue Datei `Controllers/WarehouseRequisitionsApiControllerTests.cs` (falls nicht vorhanden) nach dem Setup-Muster aus `WarehouseRequisitionsControllerTests.cs`

- [ ] **Step 6.1: Failing Tests (AddItem)** — Setup: InMemory-Ctx + echte Repos + `Mock<IAppSettingRepository>` mit
`settings.Setup(s => s.GetValueAsync(AppSettingKeys.GlasArtikelgruppen)).ReturnsAsync("GLAS");`
`settings.Setup(s => s.GetValueAsync(AppSettingKeys.GemeinsameArtikelgruppen)).ReturnsAsync("EUZ");`
Artikel seeden (`Articles.Add(new Article { ArticleNumber = "A-940", ArticleGroup = "940", ... })` etc.). Tests:
  - `AddItem_LagerBestellung_GlasArtikel_BadRequest` — Draft mit `Type=Lager`, Artikel mit `ArticleGroup="GLAS"` → `BadRequestObjectResult`.
  - `AddItem_GlasBestellung_NormalerArtikel_BadRequest` — Draft `Type=Glas`, Gruppe `"940"` → BadRequest.
  - `AddItem_GlasBestellung_GlasArtikel_Ok` — Draft `Type=Glas`, Gruppe `"GLAS"` → `OkResult` + Item existiert.
  - `AddItem_EuzArtikel_InBeidenErlaubt` — je ein Draft Lager+Glas, Gruppe `"EUZ"` → beide Ok.
- [ ] **Step 6.2:** Testlauf → FAIL.
- [ ] **Step 6.3: AddItem implementieren** — `WarehouseRequisitionsApiController`: Konstruktor um `IAppSettingRepository settings` erweitern (Feld `_settings`). In `AddItem` nach dem Artikel-Lookup einfügen:

```csharp
        var requisition = await _repo.GetByIdAsync(id, includeItems: false);
        if (requisition == null)
            return NotFound();

        var glasGroups = GlasArticleGroupFilter.ParseGroups(
            await _settings.GetValueAsync(AppSettingKeys.GlasArtikelgruppen));
        var sharedGroups = GlasArticleGroupFilter.ParseGroups(
            await _settings.GetValueAsync(AppSettingKeys.GemeinsameArtikelgruppen));
        if (!GlasArticleGroupFilter.IsAllowedForType(article.ArticleGroup, requisition.Type, glasGroups, sharedGroups))
        {
            var msg = requisition.Type == WarehouseRequisitionType.Glas
                ? $"Artikelgruppe '{article.ArticleGroup}' ist keine Glas-Artikelgruppe — Artikel gehoert in die Lager-Bestellung."
                : $"Artikelgruppe '{article.ArticleGroup}' gehoert zur Glas-Bestellung.";
            return BadRequest(new { error = msg });
        }
```
(`using IdealAkeWms.Services;` + `using IdealAkeWms.Models;` sicherstellen. Falls `IAppSettingRepository.GetValueAsync` anders heißt — Signatur in `IAppSettingRepository` prüfen und exakt verwenden.)
- [ ] **Step 6.4: Artikel-Suche** — `ArticlesApiController`: Konstruktor um `IAppSettingRepository settings` erweitern. `Search` ersetzen:

```csharp
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string? q, [FromQuery] int limit = 50,
        [FromQuery] string? type = null)
    {
        IEnumerable<Article> results;
        if (Enum.TryParse<WarehouseRequisitionType>(type, ignoreCase: true, out var reqType))
        {
            // Typ-gescopte Suche (Lagerbestellung/Glasbestellung): erst breiter suchen,
            // dann nach erlaubten Artikelgruppen filtern, dann auf limit kappen.
            var glasGroups = GlasArticleGroupFilter.ParseGroups(
                await _settings.GetValueAsync(AppSettingKeys.GlasArtikelgruppen));
            var sharedGroups = GlasArticleGroupFilter.ParseGroups(
                await _settings.GetValueAsync(AppSettingKeys.GemeinsameArtikelgruppen));
            var raw = await _articleRepository.SearchAsync(q, Math.Max(limit * 5, 100));
            results = raw
                .Where(a => GlasArticleGroupFilter.IsAllowedForType(a.ArticleGroup, reqType, glasGroups, sharedGroups))
                .Take(limit);
        }
        else
        {
            results = await _articleRepository.SearchAsync(q, limit);
        }
        return Ok(results.Select(a => new
        {
            id = a.Id,
            text = a.ArticleNumber + (a.Description != null ? " - " + a.Description : "")
        }));
    }
```
Ohne `type`-Param bleibt das Verhalten byte-identisch (alle anderen Aufrufer der Suche unverändert).
- [ ] **Step 6.5: Such-Tests** — 2 Tests auf `ArticlesApiController.Search`: mit `type="glas"` nur GLAS+EUZ-Artikel; mit `type=null` alle (Setup analog Step 6.1).
- [ ] **Step 6.6:** Alle Tests grün. **Commit** — `feat(api): Artikelgruppen-Enforcement — Suche mit type-Param + AddItem-Validierung (TDD)`

---

### Task 7: WarehouseRequisitionsController typ-fähig (TDD)

**Files:**
- Modify: `IdealAkeWms/Controllers/WarehouseRequisitionsController.cs`
- Modify: `IdealAkeWms/Models/ViewModels/WarehouseRequisitionListViewModel.cs`, `WarehouseRequisitionEditViewModel.cs`
- Test: `IdealAkeWms.Tests/Controllers/WarehouseRequisitionsControllerTests.cs`

- [ ] **Step 7.1: ViewModels erweitern** —

`WarehouseRequisitionListViewModel` (Zusatz-Properties):
```csharp
    /// <summary>Aktiver Bestelltyp-Reiter (Lager/Glas, v1.25.0).</summary>
    public WarehouseRequisitionType ActiveType { get; set; } = WarehouseRequisitionType.Lager;
    public bool CanOrderLager { get; set; }
    public bool CanOrderGlas { get; set; }
```

`WarehouseRequisitionEditViewModel`:
```csharp
    public WarehouseRequisitionType Type { get; set; } = WarehouseRequisitionType.Lager;
```

- [ ] **Step 7.2: Failing Tests** — im bestehenden Setup (`WarehouseRequisitionsControllerTests.Setup`) das `Mock<ICurrentUserService>` um Defaults erweitern:
`currentUser.Setup(s => s.CanOrderLagerAsync()).ReturnsAsync(true);`
`currentUser.Setup(s => s.CanOrderGlasAsync()).ReturnsAsync(true);`
und das Settings-Mock um `settings.Setup(s => s.GetIntValueAsync("DefaultGlasbestellempfaengerId", 0)).ReturnsAsync(glasRecipientGroupId ?? 0);` (Setup-Parameter `int? glasRecipientGroupId = null` ergänzen). Tests:
  - `CreateDraft_Glas_SetztTypGlas` — POST `CreateDraft(wpId, WarehouseRequisitionType.Glas)` → Requisition in DB hat `Type == Glas`.
  - `CreateDraft_Glas_OhneGlasRecht_Warnung` — `CanOrderGlasAsync=false` → Redirect Index + `TempData["WarningMessage"]` gesetzt, keine Requisition angelegt.
  - `Submit_GlasBestellung_NutztGlasEmpfaengerSetting` — Draft Type=Glas mit Item, `glasRecipientGroupId` = gültige Gruppe → Status Submitted, `OrderRecipientGroupId` == Glas-Gruppe; das Lager-Setting darf 0 sein (beweist Key-Auswahl).
  - `Submit_GlasBestellung_OhneGlasSetting_Blockt` — Glas-Draft, Glas-Setting 0, Lager-Setting gesetzt → Warning „Default-Glasbestellempfaenger nicht konfiguriert", Status bleibt Draft.
  - `Index_FiltertNachAktivemTyp` — je 1 Lager- + 1 Glas-Requisition des Users → `Index(type: Glas)` liefert VM mit nur der Glas-Zeile, `ActiveType == Glas`.
  - `Index_GlasOnlyUser_FaelltAufGlasZurueck` — `CanOrderLagerAsync=false`, `CanOrderGlasAsync=true`, `Index(null)` → `ActiveType == Glas`.
- [ ] **Step 7.3:** Testlauf → FAIL.
- [ ] **Step 7.4: Controller implementieren** —

**Index** — Signatur: `public async Task<IActionResult> Index(WarehouseRequisitionType? type = null, int page = 1, int? pageSize = null)`. Nach den PageSize-Zeilen:

```csharp
        var canOrderLager = await _user.CanOrderLagerAsync();
        var canOrderGlas = await _user.CanOrderGlasAsync();
        var activeType = type ?? (canOrderLager ? WarehouseRequisitionType.Lager : WarehouseRequisitionType.Glas);
        if (activeType == WarehouseRequisitionType.Glas && !canOrderGlas) activeType = WarehouseRequisitionType.Lager;
        if (activeType == WarehouseRequisitionType.Lager && !canOrderLager && canOrderGlas) activeType = WarehouseRequisitionType.Glas;
```

Den ownOnly-Filter ergänzen: `.Where(r => r.Type == activeType)` (direkt an die bestehende `ownOnly`-Where-Kette). Im VM-Initializer ergänzen: `ActiveType = activeType, CanOrderLager = canOrderLager, CanOrderGlas = canOrderGlas,`.

**CreateDraft** — Signatur: `public async Task<IActionResult> CreateDraft(int? workplaceId, WarehouseRequisitionType type = WarehouseRequisitionType.Lager)`. Als ERSTE Prüfung:

```csharp
        var allowed = type == WarehouseRequisitionType.Glas
            ? await _user.CanOrderGlasAsync()
            : await _user.CanOrderLagerAsync();
        if (!allowed)
        {
            TempData["WarningMessage"] = "Keine Berechtigung fuer diesen Bestelltyp.";
            return RedirectToAction(nameof(Index));
        }
```
Aufruf ändern: `await _repo.CreateDraftAsync(chosenWp, type, userId, _user.GetDisplayName(), _user.GetWindowsUserName());`
Die beiden Warning-Redirects im CreateDraft (`Werkbank-Zuordnung`, `Werkbank waehlen`) auf `RedirectToAction(nameof(Index), new { type })` ändern (Reiter bleibt erhalten).

**Edit** — im VM-Initializer: `Type = r.Type,`.

**Submit** — den Block `var groupId = await _settings.GetIntValueAsync("DefaultLagerbestellempfaengerId", 0);` ... ersetzen durch:

```csharp
        var settingKey = r.Type == WarehouseRequisitionType.Glas
            ? AppSettingKeys.DefaultGlasbestellempfaengerId
            : AppSettingKeys.DefaultLagerbestellempfaengerId;
        var groupId = await _settings.GetIntValueAsync(settingKey, 0);
        if (groupId <= 0)
        {
            TempData["WarningMessage"] = r.Type == WarehouseRequisitionType.Glas
                ? "Default-Glasbestellempfaenger nicht konfiguriert (Einstellungen)."
                : "Default-Lagerbestellempfaenger nicht konfiguriert (Einstellungen).";
            return RedirectToAction(nameof(Edit), new { id });
        }
```
(Rest des Submit unverändert.)
- [ ] **Step 7.5:** Alle Tests grün (bestehende Submit-Tests laufen weiter, da Lager-Pfad denselben Key nutzt — jetzt via `AppSettingKeys`-Konstante; Test-Mocks matchen den String weiterhin). **Commit** — `feat(web): Lagerbestellung typ-faehig — Index-Tab, CreateDraft(type), Submit-Empfaenger je Typ (TDD)`

---

### Task 8: Views WarehouseRequisitions (Tabs, Badge, Such-Scope)

**Files:**
- Modify: `IdealAkeWms/Views/WarehouseRequisitions/Index.cshtml`, `Edit.cshtml`

- [ ] **Step 8.1: Index — Tabs + Titel + CreateDraft-Hidden** — direkt VOR dem `page-header`-Div einfügen (Muster = MissingParts-Tabs, Zeilen 27–44 dort):

```razor
@if (Model.CanOrderLager && Model.CanOrderGlas)
{
    <ul class="nav nav-tabs mb-3">
        <li class="nav-item">
            <a class="nav-link @(Model.ActiveType == WarehouseRequisitionType.Lager ? "active" : "")"
               asp-action="Index" asp-route-type="Lager">Lager</a>
        </li>
        <li class="nav-item">
            <a class="nav-link @(Model.ActiveType == WarehouseRequisitionType.Glas ? "active" : "")"
               asp-action="Index" asp-route-type="Glas">Glas</a>
        </li>
    </ul>
}
```
`@using IdealAkeWms.Models` sicherstellen (falls nicht via _ViewImports). Titel-H2 ersetzen:
`<h2 class="mb-0">@(Model.ActiveType == WarehouseRequisitionType.Glas ? "Glasbestellungen" : "Lagerbestellungen") — meine Listen</h2>`
Im CreateDraft-`<form>` nach `@Html.AntiForgeryToken()`: `<input type="hidden" name="type" value="@Model.ActiveType" />`.
Hinweis: Spaltenfilter-Navigation + Pagination erhalten bestehende Query-Params (`?type=` bleibt beim Filtern/Blättern erhalten — `applyServerFilters` löscht nur `colf_*` + `page`).

- [ ] **Step 8.2: Edit — Badge + Such-Scope** — im Status-Card (bei WorkplaceName/Status) ergänzen:
```razor
<span class="badge @(Model.Type == WarehouseRequisitionType.Glas ? "bg-info text-dark" : "bg-secondary")">
    @(Model.Type == WarehouseRequisitionType.Glas ? "Glas" : "Lager")
</span>
```
Im Script-Block die Such-URL ändern:
`fetch(\`/api/articles/search?q=${encodeURIComponent(q)}&limit=20\`)` → `fetch(\`/api/articles/search?q=${encodeURIComponent(q)}&limit=20&type=@Model.Type\`)`.
H2/Seitentitel: „Lagerbestellung #…" → dynamisch `@(Model.Type == WarehouseRequisitionType.Glas ? "Glasbestellung" : "Lagerbestellung") #@Model.Id`.

- [ ] **Step 8.3:** Build + kurzer Smoke (`dotnet build`). **Commit** — `feat(ui): WarehouseRequisitions — Lager/Glas-Reiter, Typ-Badge, typ-gescopte Artikelsuche`

---

### Task 9: WarehousePicking (Lager: Eingehende Listen) — Typ-Reiter

**Files:**
- Modify: `IdealAkeWms/Controllers/WarehousePickingController.cs` (nur Index), `IdealAkeWms/Views/WarehousePicking/Index.cshtml`
- Test: bestehende WarehousePickingController-Tests erweitern (1 Test)

- [ ] **Step 9.1: Failing Test** — `Index_FiltertNachTyp`: je 1 Submitted-Lager + 1 Submitted-Glas → `Index(type: Glas, ...)` liefert nur die Glas-Zeile, `ActiveType == Glas`.
- [ ] **Step 9.2: Controller** — Signatur: `public async Task<IActionResult> Index(WarehouseRequisitionStatus? statusFilter, int? workplaceId, WarehouseRequisitionType type = WarehouseRequisitionType.Lager, int page = 1, int? pageSize = null)`.
  - Daten-Aufruf: `await _repo.GetForWarehouseAsync(statusList, workplaceId, type, 1, int.MaxValue);`
  - OpenCount je Typ (Badge des AKTIVEN Reiters) + Gegen-Count für den anderen Reiter:
```csharp
        var openCount = (await _repo.GetForWarehouseAsync(
            new[] { WarehouseRequisitionStatus.Submitted, WarehouseRequisitionStatus.PartiallyDelivered },
            null, type, 1, 1)).TotalCount;
```
  - VM: `ActiveType = type, CanOrderLager = true, CanOrderGlas = true,` ergänzen.
- [ ] **Step 9.3: View** — Tabs vor dem page-header (beide immer sichtbar — Lager-Rollen sehen beide Typen):
```razor
<ul class="nav nav-tabs mb-3">
    <li class="nav-item">
        <a class="nav-link @(Model.ActiveType == WarehouseRequisitionType.Lager ? "active" : "")"
           asp-action="Index" asp-route-type="Lager"
           asp-route-statusFilter="@Model.StatusFilter" asp-route-workplaceId="@Model.WorkplaceFilter">Lager</a>
    </li>
    <li class="nav-item">
        <a class="nav-link @(Model.ActiveType == WarehouseRequisitionType.Glas ? "active" : "")"
           asp-action="Index" asp-route-type="Glas"
           asp-route-statusFilter="@Model.StatusFilter" asp-route-workplaceId="@Model.WorkplaceFilter">Glas</a>
    </li>
</ul>
```
Bestehende Status-Filter-Links/Forms in der View um `type`-Roundtrip ergänzen (jede asp-route/hidden-input-Stelle prüfen: `grep -n "asp-route\|asp-action=\"Index\"\|name=\"statusFilter\"" Views/WarehousePicking/Index.cshtml`).
- [ ] **Step 9.4:** Tests grün + Build. **Commit** — `feat(lager): Eingehende Listen — Lager/Glas-Reiter (Typ-Filter server-seitig)`

---

### Task 10: MissingParts + MissingPartsLager — äußere Typ-Reiter (TDD)

**Files:**
- Modify: `IdealAkeWms/Models/ViewModels/MissingPartsListViewModel.cs`
- Modify: `IdealAkeWms/Controllers/MissingPartsController.cs`, `MissingPartsLagerController.cs`
- Modify: `IdealAkeWms/Views/MissingParts/Index.cshtml`, `IdealAkeWms/Views/MissingPartsLager/Index.cshtml`
- Test: bestehende MissingParts(-Lager)-Controller-Tests erweitern (je 1 Typ-Filter-Test)

- [ ] **Step 10.1: VM erweitern** — `MissingPartsListViewModel`:
```csharp
    /// <summary>Aktiver Bestelltyp-Reiter (aeussere Ebene, v1.25.0).</summary>
    public WarehouseRequisitionType ActiveType { get; set; } = WarehouseRequisitionType.Lager;
    public bool CanOrderLager { get; set; } = true;
    public bool CanOrderGlas { get; set; } = true;
```
- [ ] **Step 10.2: Failing Tests** — `MissingParts_Index_FiltertNachTyp` (Werker) + `MissingPartsLager_Index_FiltertNachTyp`: je 1 Fehlteil-Item in Lager- und Glas-Requisition → `Index(type: Glas, ...)` zeigt nur Glas-Item, `ActiveTab`-Counts beziehen sich auf Glas.
- [ ] **Step 10.3: MissingPartsController** — Signatur: `Index(ShortageStatus tab = ShortageStatus.WillBeRestocked, WarehouseRequisitionType? type = null, int? workplaceId = null, bool mineOnly = true, int page = 1, int? pageSize = null)`.
  - Typ-Auflösung wie Task 7 (canOrderLager/canOrderGlas + Fallback).
  - ALLE `GetMissingPartsAsync`-Aufrufe (Haupt-Query + die beiden Tab-Count-Queries `WaitingTotalCount`/`NoRestockTotalCount`) bekommen `activeType` als neuen 2. Parameter — Counts gelten je aktivem Typ.
  - VM: `ActiveType = activeType, CanOrderLager = canOrderLager, CanOrderGlas = canOrderGlas,`.
- [ ] **Step 10.4: MissingPartsLagerController** — Signatur: `Index(ShortageStatus tab = ..., WarehouseRequisitionType type = WarehouseRequisitionType.Lager, int? workplaceId = null, ...)`. Alle `GetMissingPartsAsync`-Aufrufe mit `type`; VM `ActiveType = type` (CanOrder*-Defaults true reichen — Lager sieht beide).
- [ ] **Step 10.5: Views** — in BEIDEN Index.cshtml VOR dem bestehenden `nav nav-tabs`-Block (Fehlteil-Status) die äußere Typ-Ebene als `nav nav-pills` einfügen:
```razor
@if (Model.CanOrderLager && Model.CanOrderGlas)
{
    <ul class="nav nav-pills mb-2">
        <li class="nav-item">
            <a class="nav-link @(Model.ActiveType == WarehouseRequisitionType.Lager ? "active" : "")"
               asp-action="Index" asp-route-type="Lager" asp-route-tab="@Model.ActiveTab"
               asp-route-workplaceId="@Model.WorkplaceFilter">Lager</a>
        </li>
        <li class="nav-item">
            <a class="nav-link @(Model.ActiveType == WarehouseRequisitionType.Glas ? "active" : "")"
               asp-action="Index" asp-route-type="Glas" asp-route-tab="@Model.ActiveTab"
               asp-route-workplaceId="@Model.WorkplaceFilter">Glas</a>
        </li>
    </ul>
}
```
(In MissingParts/Index.cshtml zusätzlich `asp-route-mineOnly="@Model.MineOnly"` an beide Links.) Die INNEREN Status-Tab-Links (bestehend) um `asp-route-type="@Model.ActiveType"` ergänzen, damit der Typ beim Tab-Wechsel erhalten bleibt. Ebenso alle weiteren Index-Links/Forms der Views (Werkbank-Filter etc. — `grep -n "asp-action=\"Index\"" <view>`).
- [ ] **Step 10.6:** Tests grün + Build. **Commit** — `feat(fehlteile): MissingParts + MissingPartsLager — aeussere Lager/Glas-Reiter, Counts je Typ (TDD)`

---

### Task 11: Mail — Typ-Label in Betreff + Body (TDD)

**Files:**
- Modify: `IDEALAKEWMSService/Services/WarehouseRequisitionEmailService.cs`
- Test: `IDEALAKEWMSService.Tests/Services/WarehouseRequisitionEmailServiceTests.cs`

- [ ] **Step 11.1: Failing Tests** — 
  - `BuildSubmitText_GlasBestellung_LabelGlasbestellung`: Requisition mit `Type = Glas` → Text beginnt mit `"Glasbestellung #"`.
  - `BuildCancellationText_GlasBestellung_LabelGlasbestellung`: → beginnt mit `"[STORNO] Glasbestellung #"`.
  - Bestehende Tests, die `"Lagerbestellung #"` asserten, bleiben unverändert gültig (Default Type=Lager).
- [ ] **Step 11.2:** Testlauf → FAIL.
- [ ] **Step 11.3: Implementieren** — im Service:
```csharp
    internal static string TypeLabel(WarehouseRequisition r)
        => r.Type == WarehouseRequisitionType.Glas ? "Glasbestellung" : "Lagerbestellung";
```
Alle 4 Stellen umstellen:
  - Submit-Subject: `var subject = $"{TypeLabel(r)} #{r.Id} — Werkbank {r.ProductionWorkplace.Name}";`
  - Storno-Subject: `var subject = $"[STORNO] {TypeLabel(r)} #{r.Id} — Werkbank {r.ProductionWorkplace.Name}";`
  - `BuildSubmitText`: erste Zeile `sb.AppendLine($"{TypeLabel(r)} #{r.Id}");`; die Link-Zeile `"Lagerbestellung oeffnen:"` → `$"{TypeLabel(r)} oeffnen:"`.
  - `BuildCancellationText`: erste Zeile `sb.AppendLine($"[STORNO] {TypeLabel(r)} #{r.Id}");`
  - `BuildSubmitBody` (HTML): analoge Label-Stellen (Überschrift/Titel) via `TypeLabel(r)`.
- [ ] **Step 11.4:** `dotnet test IDEALAKEWMSService.Tests --nologo` grün. **Commit** — `feat(mail): Betreff/Body-Label je Bestelltyp (Lagerbestellung/Glasbestellung) (TDD)`

---

### Task 12: Layout-Menü + RoleOverview

**Files:**
- Modify: `IdealAkeWms/Views/Shared/_Layout.cshtml`, `IdealAkeWms/Views/Users/RoleOverview.cshtml`

- [ ] **Step 12.1: Layout** — im Variablen-Block (bei `canAccessLagerbestellung`):
`var canAccessGlasbestellung = await CurrentUserService.CanAccessGlasbestellungAsync();`
Dropdown-Gate ändern:
`@if (bestellungenAktiv && (canPick || canAccessStock || canAccessLagerbestellung || canAccessGlasbestellung))`
Menü-Einträge bleiben unverändert („Lagerbestellungen" + „Meine Fehlteile" decken beide Typen via Reiter ab).
- [ ] **Step 12.2: RoleOverview** — `Views/Users/RoleOverview.cshtml`: Zeile für `glasbestellung` ergänzen (Beschreibung analog `lagerbestellung`, mit „Reiter Glas"); die Filter-Beschreibungen der beiden erweiterten Composite-Filter aktualisieren („… ODER glasbestellung, seit v1.25.0").
- [ ] **Step 12.3:** Build. **Commit** — `feat(ui): Menue-Gating + RoleOverview fuer glasbestellung`

---

### Task 13: Doku v1.25.0

**Files:**
- Modify: `IdealAkeWms/AppVersion.cs` + `IDEALAKEWMSService/AppVersion.cs` (Version `1.25.0`, Date = Implementierungsdatum)
- Modify: `IdealAkeWms/Views/Help/Changelog.cshtml` (neue v1.25.0-Card VOR der v1.24.0-Card)
- Modify: Hilfeseite (`Views/Help/Index.cshtml` bzw. bestehender Bestellungen-Abschnitt — `grep -rn "Lagerbestellung" IdealAkeWms/Views/Help/`): Abschnitt Glas-Bestellung mit KONKRETEN Details (Reiter, Rollen, 3 AppSettings inkl. Konfigurations-Anleitung: Empfänger-Gruppe anlegen → ID in Setting; Artikelgruppen-Codes wie in Artikeln gespeichert, kommasepariert; EUZ-Default)
- Modify: `docs/TESTSZENARIEN.md` (neues Kapitel 46) + `CLAUDE.md` + `PROJECT_STATUS.md`

- [ ] **Step 13.1: Changelog-Card** (Inhalt): 3-Wert-Aufzählung — (1) Glas-Bestellung als eigener Reiter (Erfassen, Lager-Worklist, Fehlteile beidseitig), (2) getrennte Empfänger je Typ + neue Rolle `glasbestellung`, (3) Artikelgruppen-Trennung mit gemeinsamen Gruppen (EUZ) + Hinweis „Rolle wird bei Update automatisch angelegt; 2 neue Einstellungen konfigurieren".
- [ ] **Step 13.2: TESTSZENARIEN Kapitel 46** — Szenarien (je Vorbedingung/Schritte/Erwartet):
  - TS-46.1 Glas-Draft anlegen (Reiter Glas → + Neue Liste → Typ-Badge Glas).
  - TS-46.2 Artikelsuche im Glas-Draft zeigt NUR Glas- + EUZ-Gruppen; im Lager-Draft KEINE Glas-Gruppen, EUZ aber schon.
  - TS-46.3 AddItem-Schutz: Artikel per API/2. Tab in falschen Typ → Fehlermeldung.
  - TS-46.4 Submit Glas ohne `DefaultGlasbestellempfaengerId` → Warnung; mit Setting → Mail an Glas-Gruppe, Betreff „Glasbestellung #…".
  - TS-46.5 Lager: Eingehende Listen — Reiter trennen Lager/Glas, Badge-Count je Typ.
  - TS-46.6 Fehlteile (Werker + Lager): äußere Typ-Reiter × innere Status-Reiter, Counts je Kombination.
  - TS-46.7 Rollen-Matrix: nur `lagerbestellung` → kein Glas-Reiter; nur `glasbestellung` → nur Glas; picking/stock → beide.
  - TS-46.8 Bestehende Bestellungen nach Update = Typ Lager (Migration-Default).
- [ ] **Step 13.3: CLAUDE.md** — (a) Rollen-Tabelle +`glasbestellung`; (b) Zugriffsschutz-Tabelle: beide Composite-Filter-Zeilen um glasbestellung ergänzen (Hinweis: Filter-NAME unverändert, seit v1.25.0 breiter); (c) AppSettings-Tabelle +3 Keys; (d) Fallstricke-Eintrag: „Glas-Bestellung Typ-Trennung (v1.25.0)" — Typ nur bei Anlage, Enforcement zweifach (Suche+AddItem), `GlasArticleGroupFilter.NormalizeGroup` (Trim/Upper/`" - "`-Suffix), `GemeinsameArtikelgruppen` Default EUZ immer beidseitig, Empfänger-Key je Typ beim Submit, Reiter = `?type=` (Spaltenfilter/Pagination erhalten den Param).
- [ ] **Step 13.4: PROJECT_STATUS.md** — v1.25.0-Absatz.
- [ ] **Step 13.5:** Build + **Commit** — `docs(v1.25.0): Changelog/Help/TESTSZENARIEN/CLAUDE/PROJECT_STATUS + Version-Bump`

---

### Task 14: Final-Check + Review

- [ ] **Step 14.1:** `dotnet build IdealAkeWms.sln` → 0 Errors/0 Warnings-Regression.
- [ ] **Step 14.2:** `dotnet test IdealAkeWms.Tests --nologo` + `dotnet test IDEALAKEWMSService.Tests --nologo` → ALLE grün (Baseline + neue).
- [ ] **Step 14.3:** `dotnet ef migrations has-pending-model-changes --project IdealAkeWms` → keine Änderungen.
- [ ] **Step 14.4:** Konsistenz-Greps:
  - `grep -rn "DefaultLagerbestellempfaengerId" IdealAkeWms/Controllers` → nur noch via `AppSettingKeys`-Konstante.
  - `grep -n "AddWarehouseRequisitionTypeAndGlasRole" SQL/00_FreshInstall.sql SQL/77_*.sql` → History-Einträge vorhanden + Timestamp identisch mit Migrations-Dateinamen.
  - `grep -rn "glasbestellung" SQL/00_FreshInstall.sql` → Rollen-Insert vorhanden.
- [ ] **Step 14.5:** Code-Review via Skill `code-review` (bzw. superpowers:requesting-code-review) über den gesamten Branch-Diff; Findings fixen.
- [ ] **Step 14.6:** Abschluss-Commit falls Review-Fixes.

---

### Task 15: PAUSE — User-Test + Merge (NICHT autonom)

- [ ] **Step 15.1:** Zusammenfassung + Testszenarien-Verweis an den User; auf manuelle Abnahme warten (inkl. echter Glas-Artikelgruppen-Codes in `GlasArtikelgruppen` eintragen!).
- [ ] **Step 15.2:** NICHT mergen, NICHT Worktree aufräumen ohne explizite Freigabe.

**Deploy-Hinweise (für die Abnahme-Nachricht):** Migration 77 ist additiv (kein Datenverlust); Rolle wird automatisch angelegt; danach in den Einstellungen `DefaultGlasbestellempfaengerId` + `GlasArtikelgruppen` konfigurieren (`GemeinsameArtikelgruppen` Default `EUZ` prüfen).

---

## Self-Review-Notizen (beim Planen geprüft)

- **Spec-Abdeckung:** Enum+Spalte (T1), Rolle+Helper+Filter (T2), EUZ-Regel (T3), AppSettings (T4), Repo (T5), Enforcement Suche+AddItem (T6), Order-Entry-UI (T7/8), Lager-Worklist (T9), Fehlteile beidseitig zweistufig (T10), Mail je Typ (T11), Menü/RoleOverview (T12), Doku/Version (T13). GetShortageCountsForUserAsync bleibt bewusst typ-übergreifend (Alert-Kacheln, Spec out-of-scope).
- **Typ-Konsistenz:** `WarehouseRequisitionType?` überall als optionaler Filter; `CreateDraftAsync(wp, type, userId, name, win)`; `GetForWarehouseAsync(statuses, workplaceId, type, page, pageSize)`; `GetMissingPartsAsync(filterStatus, type, workplaceFilter, columnFilters, closedFrom, closedUntil, page, pageSize)`.
- **Bekannte Unschärfen (Implementer verifiziert vor Ort):** exakter Name von `IAppSettingRepository.GetValueAsync`; Rückgabetyp `IArticleRepository.SearchAsync`; vorhandene Test-Dateinamen für Api/WarehousePicking/MissingParts; Stil der FreshInstall-Spaltendeklaration. Alle vier sind reine Muster-Spiegelungen bestehenden Codes.
