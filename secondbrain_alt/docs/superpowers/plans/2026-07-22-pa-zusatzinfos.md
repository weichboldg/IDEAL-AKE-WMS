# FA-Zusatzinfos (Sage) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Zusaetzliche FA-Informationen (Kaeltemittel, Ventil, Ausfuehrung E/Z, Maschine, Sage-Status) werden per Service-Sync aus der Sage-View `dbo.vw_IDEAL_AKE_WMS_FAZusatzinformationen` in den neuen 1:1-Satelliten `ProductionOrderExtraInfo` uebernommen und read-only in vier Views angezeigt (FA-Vervollstaendigung-Reiter ALLGEMEIN, FA-Abarbeitungsliste, FA-Liste, Leitstand).

**Architecture:** Neuer 1:1-Satellit `ProductionOrderExtraInfo` (Muster `ProductionOrderPickingStatus`: UNIQUE-FK + Cascade, Migration 81, rein additiv, KEIN AgentJob-Eager-Create). Der Sync folgt dem Reader-Seam-Muster von `LagerbestandSyncService`: `ISageZusatzinfoReader` (raw SQL inkl. View-Existenz-Guard, Manual-UAT) + `FaZusatzinfoSyncService` (EF-Write ueber shared `ApplicationDbContext`, komplett InMemory-testbar), eingebunden als eigener `RunResilientAsync`-Block im `SyncWorker` hinter dem DB-first-Gate `Sync:FaZusatzinfoEnabled` (Default false). Anzeige: FaCompletion-Pseudo-Tab „ALLGEMEIN", FaWorklist-Spalten (default sichtbar, inkl. Prefs-Bug-Fix „FaWorklist"-ViewKey), FA-Liste/Leitstand-Spalten ueber die `LeitstandOrderRow`-Projektion + SQL-Filter-Switch (default versteckt via neuem `defaultHidden`-Mechanismus in `column-preferences.js`).

**Tech Stack:** ASP.NET Core 10.0 MVC, EF Core 10.0 (SQL Server + InMemory fuer Tests), xUnit + FluentAssertions + Moq, Vanilla-JS (`column-preferences.js`/`table-filter.js`), Bootstrap 5.

**Arbeitsverzeichnis (alle Kommandos):** `C:\Git\IDEAL-AKE-WMS\.claude\worktrees\pa-zusatzinfos` (Branch `feature/pa-zusatzinfos`).

**Spec (Quelle der Wahrheit):** [docs/superpowers/specs/2026-07-22-pa-zusatzinfos-design.md](../specs/2026-07-22-pa-zusatzinfos-design.md) (Rev. 2).

**Junction-Fallstrick:** Im Worktree sind `secondbrain/docs` + `secondbrain/sql` ECHTE Kopien (keine Junctions). Jede Aenderung unter `docs/**` bzw. `SQL/**` MUSS identisch nach `secondbrain/docs/**` bzw. `secondbrain/sql/**` kopiert und mit-committet werden (Hash-Check `git ls-files -s <A> <B>` → gleiche Blob-Hashes).

---

## File Structure

**Create:**

| Datei | Verantwortung |
|-------|---------------|
| `IdealAkeWms/Models/ProductionOrderExtraInfo.cs` | Entity: 1:1-Satellit mit 5 nvarchar(200)-Feldern, erbt AuditableEntity |
| `IdealAkeWms/Migrations/<TS>_AddProductionOrderExtraInfo.cs` (+Designer) | EF-Migration 81 (via `dotnet ef migrations add` generiert) |
| `SQL/81_AddProductionOrderExtraInfo.sql` (+ Kopie `secondbrain/sql/`) | Idempotentes SQL-Skript (OBJECT_ID-Guard + History-Insert) |
| `IDEALAKEWMSService/Services/ISageZusatzinfoReader.cs` | Reader-Interface + Records `SageZusatzinfoRow`/`SageZusatzinfoReadResult` |
| `IDEALAKEWMSService/Services/SageZusatzinfoReader.cs` | Raw-SQL-Read der Sage-View inkl. View-Existenz-Guard (Manual-UAT) |
| `IDEALAKEWMSService/Services/IFaZusatzinfoSyncService.cs` | Sync-Interface (`Task<SyncResult> SyncAsync(bool dryRun, CancellationToken)`) |
| `IDEALAKEWMSService/Services/FaZusatzinfoSyncService.cs` | EF-Write-Sync: Set-Read, neu/aktualisiert/uebersprungen, DryRun, Protokoll |
| `IDEALAKEWMSService.Tests/Helpers/FakeSageZusatzinfoReader.cs` | Test-Fake fuer den Reader-Seam |
| `IDEALAKEWMSService.Tests/Services/FaZusatzinfoSyncServiceTests.cs` | 8 InMemory-End-to-End-Tests des Syncs |

**Modify:**

| Datei | Verantwortung |
|-------|---------------|
| `IdealAkeWms/Models/ProductionOrder.cs` (~Z. 50) | Navigation `ExtraInfo` |
| `IdealAkeWms/Data/ApplicationDbContext.cs` (Z. 21 + nach Z. 445) | DbSet + Entity-Konfiguration (UNIQUE-FK, Cascade) |
| `SQL/00_FreshInstall.sql` (~Z. 319 + Ende) (+ Kopie `secondbrain/sql/`) | Schema-Block 8b2 + History-Insert (BEIDE Stellen) |
| `IdealAkeWms/Services/SyncLogger/SyncLogServices.cs` | Konstante `FaZusatzinfo` + `All` |
| `IdealAkeWms/Models/ServiceSettingDefinitions.cs` (~Z. 31) | Katalog-Key `Sync:FaZusatzinfoEnabled` |
| `IdealAkeWms.Tests/Models/ServiceSettingDefinitionsTests.cs` (~Z. 99) | Drift-Guard-InlineData |
| `IDEALAKEWMSService/Program.cs` (~Z. 53/60) | DI: Reader + SyncService |
| `IDEALAKEWMSService/Workers/SyncWorker.cs` (nach Z. 54) | Gate-Block direkt NACH dem FA-Import-Block |
| `IDEALAKEWMSService.Tests/Workers/SyncWorkerTests.cs` | Fail-Safe-Invarianten-Test (Gate default false, kein Resolve) |
| `IdealAkeWms/Data/Repositories/IProductionOrderRepository.cs` | `LeitstandOrderRow` +5 nullable Strings, `GetExtraInfoAsync`, Doku-Kommentar |
| `IdealAkeWms/Data/Repositories/ProductionOrderRepository.cs` (Z. 12-19, 60-78, ~181) | Include, Projektion, 5 Filter-Keys (Contains+ToLower), GetExtraInfoAsync |
| `IdealAkeWms.Tests/Repositories/ProductionOrderRepositoryTests.cs` | Repo-Tests (Projektion, Filter, GetExtraInfoAsync, Include) |
| `IdealAkeWms/Controllers/FaCompletionController.cs` (Z. 173-275, 304-326, 364-381) | Tab ALLGEMEIN (OrdinalIgnoreCase), ExtraInfo-Load, Tab-Erhalt |
| `IdealAkeWms/Models/ViewModels/FaCompletionEditViewModel.cs` | +6 Properties (5 Werte + HasExtraInfo) |
| `IdealAkeWms/Views/FaCompletion/Edit.cshtml` (Z. 53-70, 82-125, 153-159) | Pseudo-Tab-li, ALLGEMEIN-Pane, hidden tab-Inputs |
| `IdealAkeWms/Controllers/WorkStepsController.cs` (Z. 70-98, 110-145) | Reserved-Code-Check „ALLGEMEIN" (Create + Edit) |
| `IdealAkeWms.Tests/Controllers/FaCompletionControllerTests.cs` | Tests Tab ALLGEMEIN / Default / ohne AGs / Tab-Erhalt |
| `IdealAkeWms.Tests/Controllers/WorkStepsControllerTests.cs` | Tests Reserved-Code |
| `IdealAkeWms/Models/ViewModels/FaWorklistViewModel.cs` (FaWorklistRow) | +5 Properties |
| `IdealAkeWms/Controllers/FaWorklistController.cs` (Z. 187-202, 311-335) | Row-Mapping + BuildColumnMap +5 Getter |
| `IdealAkeWms/Views/FaWorklist/Index.cshtml` (Z. 16-17, 82-98, 111-151, 162-180) | 5 th/td, columnCount 15, column-config |
| `IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs` (Z. 3, 19-84, 233-244) | `DefaultHidden`-Flag, +5 Spalten in 2 Configs, NEUE ViewConfig `FaWorklist` |
| `IdealAkeWms.Tests/Controllers/UserViewPreferencesApiControllerTests.cs` | Test: ViewKey „FaWorklist" kein 400 mehr |
| `IdealAkeWms/wwwroot/js/column-preferences.js` (Z. 55-63, 102-110) | `visible: !c.defaultHidden` in buildDefaultSettings + mergeWithDefaults |
| `IdealAkeWms/Models/ViewModels/ProductionOrderListViewModel.cs` | +5 Properties auf `ProductionOrderListItem` |
| `IdealAkeWms/Models/ViewModels/PickingLeitstandViewModel.cs` | +5 Properties auf `PickingLeitstandItem` |
| `IdealAkeWms/Controllers/ProductionOrdersController.cs` (Z. 101-121) | Mapping der 5 Felder |
| `IdealAkeWms/Controllers/PickingLeitstandController.cs` (Z. 96-126) | Mapping der 5 Felder |
| `IdealAkeWms/Views/ProductionOrders/Index.cshtml` (Z. 80, 146, 219, 243) | 5 th/td, colCount 19, column-config +defaultHidden |
| `IdealAkeWms/Views/PickingLeitstand/Index.cshtml` (Z. 112, 190, 385, 520) | 5 th/td, colCount 27, column-config +defaultHidden |
| `IdealAkeWms.Tests/Controllers/ProductionOrdersControllerSlimTests.cs` | Mapping- + Filter-Pass-Through-Test |
| `IdealAkeWms.Tests/Controllers/PickingLeitstandControllerTests.cs` | Mapping- + Filter-Pass-Through-Test |
| `IdealAkeWms/AppVersion.cs` + `IDEALAKEWMSService/AppVersion.cs` | v1.26.0 / 2026-07-22 |
| `IdealAkeWms/Views/Help/Changelog.cshtml` (vor Z. 10) | v1.26.0-Card |
| `IdealAkeWms/Views/Help/Index.cshtml` (Z. 313-351, 387-392, 1154-1158) | 4 Hilfe-Stellen |
| `CLAUDE.md` | Service-Konfig-Tabelle + Fallstrick-Eintrag |
| `README.md` (~Z. 622-670) | SyncWorker-Bullet + Sync-Gates-Beispiel |
| `PROJECT_STATUS.md` | Eintrag v1.26.0 |
| `docs/TESTSZENARIEN.md` (+ Kopie `secondbrain/docs/`) | Kapitel 55, Index-Zeile, Stand-Zeilen |

---

### Task 0: Pre-Flight — Baseline gruen + Plan committen

**Files:**
- Create: `secondbrain/docs/superpowers/plans/2026-07-22-pa-zusatzinfos.md` (Kopie dieses Plans)

- [ ] **Step 1: Worktree pruefen**

Run: `cd C:\Git\IDEAL-AKE-WMS\.claude\worktrees\pa-zusatzinfos; git branch --show-current; git status --short`
Expected: `feature/pa-zusatzinfos`, tree sauber (ausser dem neuen Plan-File).

- [ ] **Step 2: Baseline-Build**

Run: `dotnet build IdealAkeWms.slnx --nologo -clp:ErrorsOnly`
Expected: Build succeeded, 0 Errors.

- [ ] **Step 3: Baseline Web-Tests**

Run: `dotnet test IdealAkeWms.Tests --nologo`
Expected: alle Tests gruen (Stand v1.25.0: ~955). Anzahl notieren.

- [ ] **Step 4: Baseline Service-Tests**

Run: `dotnet test IDEALAKEWMSService.Tests --nologo`
Expected: alle Tests gruen (Stand v1.25.0: ~153). Anzahl notieren.

- [ ] **Step 5: Plan in secondbrain spiegeln + committen**

```powershell
Copy-Item docs/superpowers/plans/2026-07-22-pa-zusatzinfos.md secondbrain/docs/superpowers/plans/2026-07-22-pa-zusatzinfos.md
git add docs/superpowers/plans/2026-07-22-pa-zusatzinfos.md secondbrain/docs/superpowers/plans/2026-07-22-pa-zusatzinfos.md
git ls-files -s docs/superpowers/plans/2026-07-22-pa-zusatzinfos.md secondbrain/docs/superpowers/plans/2026-07-22-pa-zusatzinfos.md
```

Expected: beide Pfade mit IDENTISCHEM Blob-Hash. Dann:

```powershell
git commit -m @'
docs(plan): Implementierungsplan FA-Zusatzinfos (Sage) v1.26.0

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
'@
```

---

### Task 1: Entity `ProductionOrderExtraInfo` + Migration 81 + SQL-Skripte

**Files:**
- Create: `IdealAkeWms/Models/ProductionOrderExtraInfo.cs`
- Modify: `IdealAkeWms/Models/ProductionOrder.cs:50-52` (Navigation)
- Modify: `IdealAkeWms/Data/ApplicationDbContext.cs:21` (DbSet) + nach `:445` (Entity-Config)
- Create: `IdealAkeWms/Migrations/<TS>_AddProductionOrderExtraInfo.cs` (generiert)
- Create: `SQL/81_AddProductionOrderExtraInfo.sql` + `secondbrain/sql/81_AddProductionOrderExtraInfo.sql`
- Modify: `SQL/00_FreshInstall.sql:319` (neuer Block 8b2) + `:2115` (History-Insert) + `secondbrain/sql/00_FreshInstall.sql` (identische Kopie)

- [ ] **Step 1: Entity anlegen**

`IdealAkeWms/Models/ProductionOrderExtraInfo.cs` (neu):

```csharp
using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

/// <summary>
/// FA-Zusatzinfos aus Sage (v1.26.0) — 1:1-Satellit zu <see cref="ProductionOrder"/>
/// (Muster ProductionOrderPickingStatus: UNIQUE-FK + Cascade). Sage ist Master:
/// Zeilen entstehen/aendern sich AUSSCHLIESSLICH im FaZusatzinfoSyncService,
/// in der App read-only. Kein Loeschen — verschwindet ein WA aus der Sage-View,
/// bleibt der letzte bekannte Stand stehen. Kein AgentJob-Eager-Create.
/// Deutsche Property-Namen = Sage-Domaenenvokabular (1:1 zur View nachvollziehbar).
/// </summary>
public class ProductionOrderExtraInfo : AuditableEntity
{
    public int ProductionOrderId { get; set; }
    public ProductionOrder ProductionOrder { get; set; } = null!;

    [StringLength(200)]
    [Display(Name = "Kältemittel")]
    public string? Kaeltemittel { get; set; }

    [StringLength(200)]
    [Display(Name = "Ventil")]
    public string? Ventil { get; set; }

    [StringLength(200)]
    [Display(Name = "Ausführung E/Z")]
    public string? AusfuehrungEZ { get; set; }

    [StringLength(200)]
    [Display(Name = "Maschine")]
    public string? Maschine { get; set; }

    [StringLength(200)]
    [Display(Name = "Sage-Status")]
    public string? SageStatus { get; set; }
}
```

- [ ] **Step 2: Navigation auf ProductionOrder**

In `IdealAkeWms/Models/ProductionOrder.cs` nach den bestehenden Navigationen (Z. 50-51) ergaenzen:

```csharp
    // Phase 1 — neue Nav-Properties (siehe Spec 5.1)
    // AssemblyGroups-Collection entfernt in v1.22.0 (ersetzt durch FaWorkSteps)
    public ProductionOrderPickingStatus? PickingStatus { get; set; }
    public ProductionOrderBdeStatus? BdeStatus { get; set; }

    /// <summary>FA-Zusatzinfos aus Sage (v1.26.0, read-only Satellit).</summary>
    public ProductionOrderExtraInfo? ExtraInfo { get; set; }
```

(Die ersten 4 Zeilen existieren bereits — nur die 2 neuen Zeilen ergaenzen.)

- [ ] **Step 3: DbSet + Entity-Konfiguration im ApplicationDbContext**

In `IdealAkeWms/Data/ApplicationDbContext.cs` nach Z. 21 (`ProductionOrderBdeStatuses`) ergaenzen:

```csharp
    public DbSet<ProductionOrderExtraInfo> ProductionOrderExtraInfos => Set<ProductionOrderExtraInfo>();
```

Und in `OnModelCreating` DIREKT NACH dem `ProductionOrderBdeStatus`-Block (endet Z. 445 mit `});`) einfuegen:

```csharp
        // ProductionOrderExtraInfo (FA-Zusatzinfos aus Sage, v1.26.0)
        modelBuilder.Entity<ProductionOrderExtraInfo>(entity =>
        {
            entity.ToTable("ProductionOrderExtraInfo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Kaeltemittel).HasMaxLength(200);
            entity.Property(e => e.Ventil).HasMaxLength(200);
            entity.Property(e => e.AusfuehrungEZ).HasMaxLength(200);
            entity.Property(e => e.Maschine).HasMaxLength(200);
            entity.Property(e => e.SageStatus).HasMaxLength(200);
            entity.Property(e => e.CreatedBy).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CreatedByWindows).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ModifiedBy).HasMaxLength(200);
            entity.Property(e => e.ModifiedByWindows).HasMaxLength(200);

            entity.HasIndex(e => e.ProductionOrderId).IsUnique()
                .HasDatabaseName("UQ_ProductionOrderExtraInfo_ProductionOrderId");

            entity.HasOne(e => e.ProductionOrder)
                .WithOne(p => p.ExtraInfo)
                .HasForeignKey<ProductionOrderExtraInfo>(e => e.ProductionOrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });
```

- [ ] **Step 4: Build**

Run: `dotnet build IdealAkeWms.slnx --nologo -clp:ErrorsOnly`
Expected: 0 Errors.

- [ ] **Step 5: EF-Migration generieren**

Run: `dotnet ef migrations add AddProductionOrderExtraInfo --project IdealAkeWms`
Expected: `Migrations/<TS>_AddProductionOrderExtraInfo.cs` erzeugt (TS = generierter Timestamp, z. B. `20260722xxxxxx`). Inhalt pruefen: `CreateTable ProductionOrderExtraInfo` (Id, ProductionOrderId, 5x nvarchar(200) NULL, Audit-Felder) + UNIQUE-Index `UQ_ProductionOrderExtraInfo_ProductionOrderId` + FK Cascade. KEINE anderen Schema-Aenderungen (sonst PendingModelChanges-Problem → abbrechen und Model pruefen).

- [ ] **Step 6: SQL/81 schreiben**

`SQL/81_AddProductionOrderExtraInfo.sql` (neu). **WICHTIG:** `<TS>` durch den in Step 5 generierten Timestamp der MigrationId ersetzen (Dateiname in `IdealAkeWms/Migrations/` nachsehen):

```sql
-- SQL/81_AddProductionOrderExtraInfo.sql
-- Migration 81: AddProductionOrderExtraInfo (v1.26.0)
-- FA-Zusatzinfos aus Sage: 1:1-Satellit ProductionOrderExtraInfo (UNIQUE-FK, Cascade).
-- Rein additiv, idempotent. Befuellt AUSSCHLIESSLICH vom FaZusatzinfoSyncService
-- (kein AgentJob-Eager-Create).
SET NOCOUNT ON;
GO

IF OBJECT_ID('dbo.ProductionOrderExtraInfo', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ProductionOrderExtraInfo] (
        [Id]                INT IDENTITY(1,1) NOT NULL,
        [ProductionOrderId] INT               NOT NULL,
        [Kaeltemittel]      NVARCHAR(200)     NULL,
        [Ventil]            NVARCHAR(200)     NULL,
        [AusfuehrungEZ]     NVARCHAR(200)     NULL,
        [Maschine]          NVARCHAR(200)     NULL,
        [SageStatus]        NVARCHAR(200)     NULL,
        [CreatedAt]         DATETIME2         NOT NULL DEFAULT GETDATE(),
        [CreatedBy]         NVARCHAR(200)     NOT NULL,
        [CreatedByWindows]  NVARCHAR(200)     NOT NULL,
        [ModifiedAt]        DATETIME2         NULL,
        [ModifiedBy]        NVARCHAR(200)     NULL,
        [ModifiedByWindows] NVARCHAR(200)     NULL,
        CONSTRAINT [PK_ProductionOrderExtraInfo] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_ProductionOrderExtraInfo_ProductionOrderId] UNIQUE ([ProductionOrderId]),
        CONSTRAINT [FK_ProductionOrderExtraInfo_ProductionOrder]
            FOREIGN KEY ([ProductionOrderId]) REFERENCES [dbo].[ProductionOrders]([Id]) ON DELETE CASCADE
    );
    PRINT 'Tabelle ProductionOrderExtraInfo erstellt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory
    WHERE MigrationId = '<TS>_AddProductionOrderExtraInfo')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('<TS>_AddProductionOrderExtraInfo', '10.0.2');
END
GO
```

- [ ] **Step 7: FreshInstall — BEIDE Stellen**

(a) In `SQL/00_FreshInstall.sql` DIREKT NACH dem `ProductionOrderBdeStatus`-Block (endet ~Z. 319 mit `GO`) neuen Block einfuegen:

```sql
-- =============================================
-- 8b2. ProductionOrderExtraInfo (FA-Zusatzinfos aus Sage, 1 Zeile/FA, v1.26.0)
--      Befuellt nur vom FaZusatzinfoSyncService — KEIN AgentJob-Eager-Create.
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProductionOrderExtraInfo')
BEGIN
    CREATE TABLE [dbo].[ProductionOrderExtraInfo] (
        [Id]                INT IDENTITY(1,1) NOT NULL,
        [ProductionOrderId] INT               NOT NULL,
        [Kaeltemittel]      NVARCHAR(200)     NULL,
        [Ventil]            NVARCHAR(200)     NULL,
        [AusfuehrungEZ]     NVARCHAR(200)     NULL,
        [Maschine]          NVARCHAR(200)     NULL,
        [SageStatus]        NVARCHAR(200)     NULL,
        [CreatedAt]         DATETIME2         NOT NULL DEFAULT GETDATE(),
        [CreatedBy]         NVARCHAR(200)     NOT NULL,
        [CreatedByWindows]  NVARCHAR(200)     NOT NULL,
        [ModifiedAt]        DATETIME2         NULL,
        [ModifiedBy]        NVARCHAR(200)     NULL,
        [ModifiedByWindows] NVARCHAR(200)     NULL,
        CONSTRAINT [PK_ProductionOrderExtraInfo] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_ProductionOrderExtraInfo_ProductionOrderId] UNIQUE ([ProductionOrderId]),
        CONSTRAINT [FK_ProductionOrderExtraInfo_ProductionOrder]
            FOREIGN KEY ([ProductionOrderId]) REFERENCES [dbo].[ProductionOrders]([Id]) ON DELETE CASCADE
    );
    PRINT 'Tabelle ProductionOrderExtraInfo erstellt.';
END
GO
```

(b) Am Ende von `SQL/00_FreshInstall.sql` im `__EFMigrationsHistory`-Block NACH dem `20260707140249_AddProductionOrderCancellation`-Insert (VOR dem abschliessenden `GO`, ~Z. 2115) ergaenzen (`<TS>` wie in Step 6):

```sql
IF NOT EXISTS (SELECT * FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '<TS>_AddProductionOrderExtraInfo')
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('<TS>_AddProductionOrderExtraInfo', '10.0.2');
```

- [ ] **Step 8: SQL-Zwillinge spiegeln + Build + Regressionslauf**

```powershell
Copy-Item SQL/81_AddProductionOrderExtraInfo.sql secondbrain/sql/81_AddProductionOrderExtraInfo.sql
Copy-Item SQL/00_FreshInstall.sql secondbrain/sql/00_FreshInstall.sql
dotnet build IdealAkeWms.slnx --nologo -clp:ErrorsOnly
dotnet test IdealAkeWms.Tests --nologo
```

Expected: Build 0 Errors, alle Web-Tests weiter gruen (Model-Change ist additiv).

- [ ] **Step 9: Commit**

```powershell
git add IdealAkeWms/Models/ProductionOrderExtraInfo.cs IdealAkeWms/Models/ProductionOrder.cs IdealAkeWms/Data/ApplicationDbContext.cs IdealAkeWms/Migrations/ SQL/81_AddProductionOrderExtraInfo.sql SQL/00_FreshInstall.sql secondbrain/sql/81_AddProductionOrderExtraInfo.sql secondbrain/sql/00_FreshInstall.sql
git ls-files -s SQL/81_AddProductionOrderExtraInfo.sql secondbrain/sql/81_AddProductionOrderExtraInfo.sql SQL/00_FreshInstall.sql secondbrain/sql/00_FreshInstall.sql
git commit -m @'
feat(fa-zusatzinfo): Entity ProductionOrderExtraInfo + Migration 81 (Satellit, UNIQUE-FK)

1:1-Satellit fuer FA-Zusatzinfos aus Sage (Kaeltemittel/Ventil/AusfuehrungEZ/
Maschine/SageStatus, je nvarchar(200) NULL). Muster ProductionOrderPickingStatus:
UNIQUE-FK UQ_ProductionOrderExtraInfo_ProductionOrderId, OnDelete Cascade.
SQL/81 idempotent + FreshInstall (Schema-Block 8b2 + History-Insert).
Kein AgentJob-Eager-Create — Zeilen entstehen nur im FaZusatzinfoSyncService.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
'@
```

Expected: Hash-Paare identisch, Commit ok.

---

### Task 2: SyncLogServices.FaZusatzinfo + Katalog-Key `Sync:FaZusatzinfoEnabled` (TDD)

**Files:**
- Modify: `IdealAkeWms.Tests/Models/ServiceSettingDefinitionsTests.cs:99` (InlineData)
- Modify: `IdealAkeWms/Models/ServiceSettingDefinitions.cs:31` (Katalog-Eintrag)
- Modify: `IdealAkeWms/Services/SyncLogger/SyncLogServices.cs` (Konstante + All)

- [ ] **Step 1: Drift-Guard-Test ergaenzen (RED)**

In `IdealAkeWms.Tests/Models/ServiceSettingDefinitionsTests.cs` in der `[Theory]`-Liste von `All_ContainsDocumentedServiceReadKey` nach `[InlineData("Sync:ReconcileMaxCancelPerRun")]` (Z. 78) einfuegen:

```csharp
    [InlineData("Sync:FaZusatzinfoEnabled")]
```

- [ ] **Step 2: Test laufen lassen (RED)**

Run: `dotnet test IdealAkeWms.Tests --nologo --filter "FullyQualifiedName~ServiceSettingDefinitionsTests"`
Expected: FAIL — `All_ContainsDocumentedServiceReadKey(key: "Sync:FaZusatzinfoEnabled")` (Key nicht im Katalog).

- [ ] **Step 3: Katalog-Eintrag + SyncLogServices-Konstante**

(a) In `IdealAkeWms/Models/ServiceSettingDefinitions.cs` nach der Zeile `new("Sync:ReconcileMaxCancelPerRun", ...)` (Z. 31) einfuegen:

```csharp
        new("Sync:FaZusatzinfoEnabled",              ServiceSettingType.Bool, "false", "Sync", "FA-Zusatzinfos (Sage): Kaeltemittel/Ventil/Ausfuehrung/Maschine/Status je FA synchronisieren"),
```

(b) In `IdealAkeWms/Services/SyncLogger/SyncLogServices.cs` nach `public const string Article = "Article";` (Z. 22) einfuegen:

```csharp
    public const string FaZusatzinfo = "FaZusatzinfo";        // FA-Zusatzinfos aus Sage (v1.26.0)
```

und in der `All`-Liste die Zeile `ProductionOrder, ProductionOrderReconciliation, Article,` ersetzen durch:

```csharp
        ProductionOrder, ProductionOrderReconciliation, Article, FaZusatzinfo,
```

- [ ] **Step 4: Tests laufen lassen (GREEN)**

Run: `dotnet test IdealAkeWms.Tests --nologo --filter "FullyQualifiedName~ServiceSettingDefinitionsTests"`
Expected: PASS (alle, inkl. neuem InlineData).

- [ ] **Step 5: Commit**

```powershell
git add IdealAkeWms/Models/ServiceSettingDefinitions.cs IdealAkeWms/Services/SyncLogger/SyncLogServices.cs IdealAkeWms.Tests/Models/ServiceSettingDefinitionsTests.cs
git commit -m @'
feat(fa-zusatzinfo): Katalog-Key Sync:FaZusatzinfoEnabled + Protokoll-Name FaZusatzinfo

Drift-Guard-InlineData zuerst (RED), dann Katalog-Eintrag (Bool, Default false,
Kategorie Sync) + SyncLogServices.FaZusatzinfo inkl. All-Liste (sonst kennt der
Aktivitaets-Protokoll-Filter den Namen nicht).

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
'@
```

---

### Task 3: `ISageZusatzinfoReader` + `SageZusatzinfoReader` (raw SQL, View-Guard)

**Files:**
- Create: `IDEALAKEWMSService/Services/ISageZusatzinfoReader.cs`
- Create: `IDEALAKEWMSService/Services/SageZusatzinfoReader.cs`

Kein Unit-Test — die raw-SQL-Strecke gegen Sage ist (wie `SageBestandReader`) Manual-UAT; die Entscheidungslogik dahinter wird in Task 4 ueber den Fake voll getestet.

- [ ] **Step 1: Interface + Records**

`IDEALAKEWMSService/Services/ISageZusatzinfoReader.cs` (neu):

```csharp
namespace IDEALAKEWMSService.Services;

/// <summary>
/// Eine Zeile der Sage-View <c>dbo.vw_IDEAL_AKE_WMS_FAZusatzinformationen</c>.
/// Die View liefert fertige Texte (Status-CASE-Logik lebt in der View, nicht bei uns).
/// </summary>
public record SageZusatzinfoRow(
    string? WaNummer,
    string? Kaeltemittel,
    string? Ventil,
    string? AusfuehrungEZ,
    string? Maschine,
    string? Status);

/// <summary>
/// <c>ViewExists=false</c>: die View fehlt am Zielsystem — der Sync ueberspringt dann
/// regulaer mit Warn-Zeile (kein throw, keine Fehlermail; Spec §2 View-Guard).
/// </summary>
public record SageZusatzinfoReadResult(bool ViewExists, IReadOnlyList<SageZusatzinfoRow> Rows);

public interface ISageZusatzinfoReader
{
    Task<SageZusatzinfoReadResult> ReadAsync(CancellationToken ct = default);
}
```

- [ ] **Step 2: Reader-Implementierung**

`IDEALAKEWMSService/Services/SageZusatzinfoReader.cs` (neu):

```csharp
using Microsoft.Data.SqlClient;

namespace IDEALAKEWMSService.Services;

/// <summary>
/// Raw-SQL-Read der FA-Zusatzinfos aus Sage (SageConnection, immer dbo.-Praefix).
/// Defensive CASTs = hartes Abschneiden an der Quelle statt Truncation-Fehler beim
/// Write (Muster der bestehenden Sage-Reads). View-Existenz-Guard VOR dem Read:
/// fehlt die View, liefert der Reader ViewExists=false — der Sync endet dann
/// regulaer (kein Mail-Spam alle 15 min auf Systemen ohne View).
/// </summary>
public class SageZusatzinfoReader : ISageZusatzinfoReader
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SageZusatzinfoReader> _logger;

    public SageZusatzinfoReader(IConfiguration configuration, ILogger<SageZusatzinfoReader> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<SageZusatzinfoReadResult> ReadAsync(CancellationToken ct = default)
    {
        var sageConnection = _configuration.GetConnectionString("SageConnection")
            ?? throw new InvalidOperationException("SageConnection nicht konfiguriert.");

        await using var conn = new SqlConnection(sageConnection);
        await conn.OpenAsync(ct);

        // View-Existenz-Guard (Spec §2, Pflicht).
        await using (var checkCmd = new SqlCommand(
            "SELECT OBJECT_ID('dbo.vw_IDEAL_AKE_WMS_FAZusatzinformationen', 'V')", conn))
        {
            var objId = await checkCmd.ExecuteScalarAsync(ct);
            if (objId == null || objId == DBNull.Value)
            {
                _logger.LogWarning("Sage-View vw_IDEAL_AKE_WMS_FAZusatzinformationen nicht vorhanden — Sync wird uebersprungen.");
                return new SageZusatzinfoReadResult(false, new List<SageZusatzinfoRow>());
            }
        }

        const string sql = """
            SELECT CAST([WA Nummer] AS nvarchar(100))       AS WaNummer,
                   CAST(Kaeltemittel AS nvarchar(200))      AS Kaeltemittel,
                   CAST(Ventil AS nvarchar(200))            AS Ventil,
                   CAST([Ausfuehrung E/Z] AS nvarchar(200)) AS AusfuehrungEZ,
                   CAST(Maschine AS nvarchar(200))          AS Maschine,
                   CAST(Status AS nvarchar(200))            AS Status
            FROM dbo.vw_IDEAL_AKE_WMS_FAZusatzinformationen
            """;

        var rows = new List<SageZusatzinfoRow>();
        await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 120 };
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            rows.Add(new SageZusatzinfoRow(
                WaNummer:      reader.IsDBNull(0) ? null : reader.GetString(0),
                Kaeltemittel:  reader.IsDBNull(1) ? null : reader.GetString(1),
                Ventil:        reader.IsDBNull(2) ? null : reader.GetString(2),
                AusfuehrungEZ: reader.IsDBNull(3) ? null : reader.GetString(3),
                Maschine:      reader.IsDBNull(4) ? null : reader.GetString(4),
                Status:        reader.IsDBNull(5) ? null : reader.GetString(5)));
        }

        _logger.LogInformation("Sage liefert {Count} FA-Zusatzinfo-Zeilen.", rows.Count);
        return new SageZusatzinfoReadResult(true, rows);
    }
}
```

- [ ] **Step 3: Build**

Run: `dotnet build IdealAkeWms.slnx --nologo -clp:ErrorsOnly`
Expected: 0 Errors.

- [ ] **Step 4: Commit**

```powershell
git add IDEALAKEWMSService/Services/ISageZusatzinfoReader.cs IDEALAKEWMSService/Services/SageZusatzinfoReader.cs
git commit -m @'
feat(fa-zusatzinfo): ISageZusatzinfoReader + raw-SQL-Reader mit View-Existenz-Guard

Reader-Seam (Muster SageBestandReader): SageConnection, dbo.-Praefix, defensive
CASTs (nvarchar 100/200). OBJECT_ID-Guard VOR dem Read — fehlende View liefert
ViewExists=false statt Exception (kein Fehlermail-Spam). Raw-SQL = Manual-UAT.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
'@
```

---

### Task 4: `FaZusatzinfoSyncService` (EF-Write, TDD mit 8 Testfaellen)

**Files:**
- Create: `IDEALAKEWMSService.Tests/Helpers/FakeSageZusatzinfoReader.cs`
- Create: `IDEALAKEWMSService.Tests/Services/FaZusatzinfoSyncServiceTests.cs`
- Create: `IDEALAKEWMSService/Services/IFaZusatzinfoSyncService.cs`
- Create: `IDEALAKEWMSService/Services/FaZusatzinfoSyncService.cs`
- Test: `dotnet test IDEALAKEWMSService.Tests --nologo --filter "FullyQualifiedName~FaZusatzinfoSyncServiceTests"`

- [ ] **Step 1: Test-Fake anlegen**

`IDEALAKEWMSService.Tests/Helpers/FakeSageZusatzinfoReader.cs` (neu, Muster `FakeSageBestandReader`):

```csharp
using IDEALAKEWMSService.Services;

namespace IDEALAKEWMSService.Tests.Helpers;

public class FakeSageZusatzinfoReader : ISageZusatzinfoReader
{
    public bool ViewExists { get; set; } = true;
    public List<SageZusatzinfoRow> Rows { get; set; } = new();
    public Exception? ThrowOnRead { get; set; }

    public Task<SageZusatzinfoReadResult> ReadAsync(CancellationToken ct = default)
    {
        if (ThrowOnRead != null)
            throw ThrowOnRead;
        return Task.FromResult(new SageZusatzinfoReadResult(ViewExists, Rows));
    }
}
```

- [ ] **Step 2: Testklasse schreiben (RED — kompiliert noch nicht)**

`IDEALAKEWMSService.Tests/Services/FaZusatzinfoSyncServiceTests.cs` (neu):

```csharp
using FluentAssertions;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using IDEALAKEWMSService.Services;
using IDEALAKEWMSService.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace IDEALAKEWMSService.Tests.Services;

/// <summary>
/// End-to-End-InMemory-Tests des FA-Zusatzinfo-Syncs (v1.26.0, Spec §4/§6).
/// Reader ist gefaked (raw SQL = Manual-UAT), Entscheidungs- UND Schreibpfad
/// laufen ueber den echten EF-Service (kein Deko-Helper).
/// </summary>
public class FaZusatzinfoSyncServiceTests
{
    private const string SyncUser = "FaZusatzinfoSync";

    private static (FaZusatzinfoSyncService svc, FakeSageZusatzinfoReader reader,
                    IdealAkeWms.Data.ApplicationDbContext ctx, FakeSyncLogger fakeLogger)
        Build()
    {
        var ctx = TestDbContextFactory.Create();
        var reader = new FakeSageZusatzinfoReader();
        var fakeLogger = new FakeSyncLogger();
        var svc = new FaZusatzinfoSyncService(
            ctx, reader, NullLogger<FaZusatzinfoSyncService>.Instance, fakeLogger);
        return (svc, reader, ctx, fakeLogger);
    }

    private static ProductionOrder SeedOrder(IdealAkeWms.Data.ApplicationDbContext ctx, string orderNumber)
    {
        var order = new ProductionOrder
        {
            OrderNumber = orderNumber,
            CreatedAt = DateTime.Now,
            CreatedBy = "t",
            CreatedByWindows = "t"
        };
        ctx.ProductionOrders.Add(order);
        ctx.SaveChanges();
        return order;
    }

    private static ProductionOrderExtraInfo SeedExtraInfo(
        IdealAkeWms.Data.ApplicationDbContext ctx, int productionOrderId,
        string? kaelte = "R290", string? ventil = "Danfoss", string? ausfuehrung = "E",
        string? maschine = "M1", string? status = "in Produktion")
    {
        var info = new ProductionOrderExtraInfo
        {
            ProductionOrderId = productionOrderId,
            Kaeltemittel = kaelte,
            Ventil = ventil,
            AusfuehrungEZ = ausfuehrung,
            Maschine = maschine,
            SageStatus = status,
            CreatedAt = DateTime.Now,
            CreatedBy = SyncUser,
            CreatedByWindows = SyncUser
        };
        ctx.ProductionOrderExtraInfos.Add(info);
        ctx.SaveChanges();
        return info;
    }

    private static SageZusatzinfoRow Row(string wa, string? kaelte = "R290", string? ventil = "Danfoss",
        string? ausfuehrung = "E", string? maschine = "M1", string? status = "in Produktion")
        => new(wa, kaelte, ventil, ausfuehrung, maschine, status);

    // 1) Neu: WA mit FA-Treffer, kein Satellit -> Insert
    [Fact]
    public async Task Sync_NewWa_InsertsSatellite()
    {
        var (svc, reader, ctx, fakeLogger) = Build();
        var order = SeedOrder(ctx, "WA-1");
        reader.Rows = new() { Row("WA-1") };

        var result = await svc.SyncAsync(dryRun: false);

        result.Inserted.Should().Be(1);
        result.Updated.Should().Be(0);
        var info = ctx.ProductionOrderExtraInfos.Single();
        info.ProductionOrderId.Should().Be(order.Id);
        info.Kaeltemittel.Should().Be("R290");
        info.Ventil.Should().Be("Danfoss");
        info.AusfuehrungEZ.Should().Be("E");
        info.Maschine.Should().Be("M1");
        info.SageStatus.Should().Be("in Produktion");
        info.CreatedBy.Should().Be(SyncUser);
        info.CreatedByWindows.Should().Be(SyncUser);
        info.ModifiedAt.Should().BeNull();

        fakeLogger.Runs[0].ServiceName.Should().Be("FaZusatzinfo");
        fakeLogger.Runs[0].FinishedSuccess.Should().BeTrue();
        fakeLogger.Runs[0].FinalCounts!["gelesen"].Should().Be(1);
        fakeLogger.Runs[0].FinalCounts!["neu"].Should().Be(1);
        fakeLogger.Runs[0].FinalCounts!["aktualisiert"].Should().Be(0);
        fakeLogger.Runs[0].FinalCounts!["uebersprungen"].Should().Be(0);
    }

    // 2) Aktualisiert: Satellit vorhanden, mindestens 1 Feld geaendert -> Update + Modified*
    [Fact]
    public async Task Sync_ChangedField_UpdatesSatellite_SetsModified()
    {
        var (svc, reader, ctx, fakeLogger) = Build();
        var order = SeedOrder(ctx, "WA-1");
        SeedExtraInfo(ctx, order.Id, kaelte: "R134a");
        reader.Rows = new() { Row("WA-1", kaelte: "R290") };

        var result = await svc.SyncAsync(dryRun: false);

        result.Updated.Should().Be(1);
        result.Inserted.Should().Be(0);
        var info = ctx.ProductionOrderExtraInfos.Single();
        info.Kaeltemittel.Should().Be("R290");
        info.ModifiedAt.Should().NotBeNull();
        info.ModifiedBy.Should().Be(SyncUser);
        info.ModifiedByWindows.Should().Be(SyncUser);
        fakeLogger.Runs[0].FinalCounts!["aktualisiert"].Should().Be(1);
        fakeLogger.Runs[0].FinalCounts!["neu"].Should().Be(0);
    }

    // 3) Unveraendert: kein Write, kein Count
    [Fact]
    public async Task Sync_Unchanged_NoWrite_NoCount()
    {
        var (svc, reader, ctx, fakeLogger) = Build();
        var order = SeedOrder(ctx, "WA-1");
        SeedExtraInfo(ctx, order.Id);
        reader.Rows = new() { Row("WA-1") };

        var result = await svc.SyncAsync(dryRun: false);

        result.Inserted.Should().Be(0);
        result.Updated.Should().Be(0);
        ctx.ProductionOrderExtraInfos.Single().ModifiedAt.Should().BeNull();
        fakeLogger.Runs[0].FinalCounts!["gelesen"].Should().Be(1);
        fakeLogger.Runs[0].FinalCounts!["neu"].Should().Be(0);
        fakeLogger.Runs[0].FinalCounts!["aktualisiert"].Should().Be(0);
    }

    // 4) Kein FA-Treffer: uebersprungen-Count, KEINE Warn-Zeile je WA (alte WAs sind normal)
    [Fact]
    public async Task Sync_WaWithoutFa_CountsSkipped_NoWarnPerWa()
    {
        var (svc, reader, ctx, fakeLogger) = Build();
        reader.Rows = new() { Row("WA-UNBEKANNT") };

        var result = await svc.SyncAsync(dryRun: false);

        result.Inserted.Should().Be(0);
        ctx.ProductionOrderExtraInfos.Should().BeEmpty();
        fakeLogger.Runs[0].FinishedSuccess.Should().BeTrue();
        fakeLogger.Runs[0].FinalCounts!["uebersprungen"].Should().Be(1);
        fakeLogger.Runs[0].Events.Should().NotContain(e => e.Level == "Warning");
    }

    // 5) IDEAL-Fall: mehrere ProductionOrders je WA-Nummer -> Upsert je gematchter Id
    //    (InMemory enforced den UNIQUE-Index auf OrderNumber nicht — genau richtig,
    //     um die IDEAL-Linie mit SubOrder-Zeilen zu simulieren.)
    [Fact]
    public async Task Sync_MultipleFasPerWa_UpsertsEachMatch()
    {
        var (svc, reader, ctx, fakeLogger) = Build();
        var o1 = SeedOrder(ctx, "WA-1");
        var o2 = SeedOrder(ctx, "WA-1");
        reader.Rows = new() { Row("WA-1") };

        var result = await svc.SyncAsync(dryRun: false);

        result.Inserted.Should().Be(2);
        ctx.ProductionOrderExtraInfos.Should().HaveCount(2);
        ctx.ProductionOrderExtraInfos.Select(i => i.ProductionOrderId)
            .Should().BeEquivalentTo(new[] { o1.Id, o2.Id });
        fakeLogger.Runs[0].FinalCounts!["neu"].Should().Be(2);
    }

    // 6) DryRun: voller Plan mit echten Would-be-Counts, aber KEIN Write (Reconciler-Muster)
    [Fact]
    public async Task Sync_DryRun_RealCounts_NoWrites()
    {
        var (svc, reader, ctx, fakeLogger) = Build();
        var oNew = SeedOrder(ctx, "WA-NEU");
        var oChanged = SeedOrder(ctx, "WA-GEAENDERT");
        SeedExtraInfo(ctx, oChanged.Id, kaelte: "R134a");
        reader.Rows = new() { Row("WA-NEU"), Row("WA-GEAENDERT", kaelte: "R290") };

        var result = await svc.SyncAsync(dryRun: true);

        result.Inserted.Should().Be(1);
        result.Updated.Should().Be(1);
        ctx.ProductionOrderExtraInfos.Should().HaveCount(1);            // kein Insert
        ctx.ProductionOrderExtraInfos.Single().Kaeltemittel.Should().Be("R134a"); // kein Update
        fakeLogger.Runs[0].FinalCounts!["neu"].Should().Be(1);
        fakeLogger.Runs[0].FinalCounts!["aktualisiert"].Should().Be(1);
        fakeLogger.Runs[0].FinalMessageSuffix.Should().Be("[DryRun]");
    }

    // 7) View fehlt: Warn-Zeile + regulaeres Lauf-Ende (FinishSuccess), kein Throw
    [Fact]
    public async Task Sync_ViewMissing_WarnsAndFinishesSuccess()
    {
        var (svc, reader, ctx, fakeLogger) = Build();
        reader.ViewExists = false;

        var act = async () => await svc.SyncAsync(dryRun: false);

        await act.Should().NotThrowAsync();
        fakeLogger.Runs[0].FinishedSuccess.Should().BeTrue();
        fakeLogger.Runs[0].FinishedFailed.Should().BeFalse();
        fakeLogger.Runs[0].Events.Should().Contain(e =>
            e.Level == "Warning" && e.Message.Contains("nicht vorhanden"));
        fakeLogger.Runs[0].FinalCounts!["gelesen"].Should().Be(0);
        fakeLogger.Runs[0].FinalCounts!["uebersprungen"].Should().Be(0);
    }

    // 8) Fehlerpfad: Reader wirft -> LogError + FinishFailed + rethrow
    [Fact]
    public async Task Sync_ReaderThrows_FinishesFailed_AndRethrows()
    {
        var (svc, reader, ctx, fakeLogger) = Build();
        reader.ThrowOnRead = new InvalidOperationException("Sage down");

        var act = async () => await svc.SyncAsync(dryRun: false);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Sage down");
        fakeLogger.Runs[0].FinishedFailed.Should().BeTrue();
        fakeLogger.Runs[0].FinalErrorMessage.Should().Be("Sage down");
        fakeLogger.Runs[0].Events.Should().Contain(e => e.Level == "Error");
    }
}
```

- [ ] **Step 3: Kompilieren → RED bestaetigen**

Run: `dotnet build IdealAkeWms.slnx --nologo -clp:ErrorsOnly`
Expected: FAIL — `FaZusatzinfoSyncService`/`IFaZusatzinfoSyncService` existieren noch nicht (Compile-Error = RED).

- [ ] **Step 4: Interface implementieren**

`IDEALAKEWMSService/Services/IFaZusatzinfoSyncService.cs` (neu):

```csharp
namespace IDEALAKEWMSService.Services;

public interface IFaZusatzinfoSyncService
{
    Task<SyncResult> SyncAsync(bool dryRun, CancellationToken ct = default);
}
```

- [ ] **Step 5: Service implementieren**

`IDEALAKEWMSService/Services/FaZusatzinfoSyncService.cs` (neu):

```csharp
using IdealAkeWms.Data;
using IdealAkeWms.Models;
using IdealAkeWms.Services.SyncLogger;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IDEALAKEWMSService.Services;

/// <summary>
/// FA-Zusatzinfos aus Sage (v1.26.0, Spec §4): liest die View ueber den Reader-Seam
/// und upsertet den 1:1-Satelliten <see cref="ProductionOrderExtraInfo"/> per EF
/// (shared ApplicationDbContext — kompletter Entscheidungs- UND Schreibpfad InMemory-
/// testbar). Mehrfach-treffer-faehig (IDEAL-Linie: mehrere ProductionOrders je
/// WA-Nummer → Upsert je Id). Kein Loeschen: verschwindet ein WA aus der View,
/// bleibt der letzte Stand stehen. DryRun = voller Plan inkl. echter Would-be-Counts,
/// nur SaveChanges wird uebersprungen (Reconciler-Muster).
/// </summary>
public class FaZusatzinfoSyncService : IFaZusatzinfoSyncService
{
    private const string SyncUser = "FaZusatzinfoSync";

    private readonly ApplicationDbContext _ctx;
    private readonly ISageZusatzinfoReader _reader;
    private readonly ILogger<FaZusatzinfoSyncService> _logger;
    private readonly ISyncLogger _syncLogger;

    public FaZusatzinfoSyncService(
        ApplicationDbContext ctx,
        ISageZusatzinfoReader reader,
        ILogger<FaZusatzinfoSyncService> logger,
        ISyncLogger syncLogger)
    {
        _ctx = ctx;
        _reader = reader;
        _logger = logger;
        _syncLogger = syncLogger;
    }

    public async Task<SyncResult> SyncAsync(bool dryRun, CancellationToken ct = default)
    {
        await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.FaZusatzinfo, ct);
        int read = 0, inserted = 0, updated = 0, skipped = 0;

        try
        {
            var readResult = await _reader.ReadAsync(ct);

            // View-fehlt-Guard (Spec §2): Warn-Zeile + regulaeres Lauf-Ende — bewusst
            // KEIN Fehlerpfad (kein throw, keine Fehlermail alle 15 min).
            if (!readResult.ViewExists)
            {
                await run.LogWarningAsync(
                    "View dbo.vw_IDEAL_AKE_WMS_FAZusatzinformationen nicht vorhanden — Sync uebersprungen.", ct: ct);
                await run.FinishSuccessAsync(new Dictionary<string, int>
                {
                    ["gelesen"] = 0,
                    ["neu"] = 0,
                    ["aktualisiert"] = 0,
                    ["uebersprungen"] = 0,
                }, messageSuffix: dryRun ? "View nicht vorhanden [DryRun]" : "View nicht vorhanden", ct: ct);
                return new SyncResult(0, 0, 0, "View nicht vorhanden.");
            }

            var rows = readResult.Rows;
            read = rows.Count;

            // Einmaliger Set-Read der FA-Zuordnung (kein Zeile-fuer-Zeile-Roundtrip):
            // OrderNumber -> List<ProductionOrder> inkl. ExtraInfo-Satellit.
            var waNumbers = rows
                .Where(r => !string.IsNullOrWhiteSpace(r.WaNummer))
                .Select(r => r.WaNummer!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var orders = await _ctx.ProductionOrders
                .Include(o => o.ExtraInfo)
                .Where(o => waNumbers.Contains(o.OrderNumber))
                .ToListAsync(ct);

            var ordersByNumber = orders
                .GroupBy(o => o.OrderNumber, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                ct.ThrowIfCancellationRequested();

                var wa = row.WaNummer?.Trim();
                if (string.IsNullOrWhiteSpace(wa) || !ordersByNumber.TryGetValue(wa, out var matches))
                {
                    // Kein FA-Treffer: alte/erledigte WAs sind normal — Count, keine Warn-Zeile je WA.
                    skipped++;
                    continue;
                }

                foreach (var order in matches)
                {
                    var info = order.ExtraInfo;
                    if (info == null)
                    {
                        if (!dryRun)
                        {
                            _ctx.ProductionOrderExtraInfos.Add(new ProductionOrderExtraInfo
                            {
                                ProductionOrderId = order.Id,
                                Kaeltemittel = row.Kaeltemittel,
                                Ventil = row.Ventil,
                                AusfuehrungEZ = row.AusfuehrungEZ,
                                Maschine = row.Maschine,
                                SageStatus = row.Status,
                                CreatedAt = DateTime.Now,
                                CreatedBy = SyncUser,
                                CreatedByWindows = SyncUser,
                            });
                        }
                        inserted++;
                    }
                    else if (info.Kaeltemittel != row.Kaeltemittel
                          || info.Ventil != row.Ventil
                          || info.AusfuehrungEZ != row.AusfuehrungEZ
                          || info.Maschine != row.Maschine
                          || info.SageStatus != row.Status)
                    {
                        if (!dryRun)
                        {
                            info.Kaeltemittel = row.Kaeltemittel;
                            info.Ventil = row.Ventil;
                            info.AusfuehrungEZ = row.AusfuehrungEZ;
                            info.Maschine = row.Maschine;
                            info.SageStatus = row.Status;
                            info.ModifiedAt = DateTime.Now;
                            info.ModifiedBy = SyncUser;
                            info.ModifiedByWindows = SyncUser;
                        }
                        updated++;
                    }
                    // unveraendert -> kein Write, kein Count (haelt ModifiedAt aussagekraeftig)
                }
            }

            if (!dryRun) await _ctx.SaveChangesAsync(ct);

            _logger.LogInformation(
                "FA-Zusatzinfo-Sync abgeschlossen: {Read} gelesen, {Inserted} neu, {Updated} aktualisiert, {Skipped} uebersprungen{DryRun}",
                read, inserted, updated, skipped, dryRun ? " [DryRun]" : "");

            await run.FinishSuccessAsync(new Dictionary<string, int>
            {
                ["gelesen"] = read,
                ["neu"] = inserted,
                ["aktualisiert"] = updated,
                ["uebersprungen"] = skipped,
            }, messageSuffix: dryRun ? "[DryRun]" : null, ct: ct);

            return new SyncResult(inserted, updated, 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim FA-Zusatzinfo-Sync.");
            await run.LogErrorAsync(ex.Message, ct: ct);
            await run.FinishFailedAsync(ex.Message, counts: new Dictionary<string, int>
            {
                ["gelesen"] = read,
                ["neu"] = inserted,
                ["aktualisiert"] = updated,
                ["uebersprungen"] = skipped,
            }, ct: ct);
            throw;
        }
    }
}
```

- [ ] **Step 6: Tests laufen lassen (GREEN)**

Run: `dotnet test IDEALAKEWMSService.Tests --nologo --filter "FullyQualifiedName~FaZusatzinfoSyncServiceTests"`
Expected: PASS — 8/8.

- [ ] **Step 7: Commit**

```powershell
git add IDEALAKEWMSService/Services/IFaZusatzinfoSyncService.cs IDEALAKEWMSService/Services/FaZusatzinfoSyncService.cs IDEALAKEWMSService.Tests/Helpers/FakeSageZusatzinfoReader.cs IDEALAKEWMSService.Tests/Services/FaZusatzinfoSyncServiceTests.cs
git commit -m @'
feat(fa-zusatzinfo): FaZusatzinfoSyncService (EF-Write, Set-Read, DryRun, Protokoll)

TDD: 8 InMemory-Tests (neu/aktualisiert/unveraendert/kein-Treffer/Mehrfach-FA/
DryRun/View-fehlt/Fehlerpfad) mit FakeSageZusatzinfoReader + FakeSyncLogger.
Counts gelesen/neu/aktualisiert/uebersprungen; Audit "FaZusatzinfoSync"/DateTime.Now;
DryRun = voller Plan ohne SaveChanges; View-fehlt = Warn + FinishSuccess;
Exception = LogError + FinishFailed + rethrow.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
'@
```

---

### Task 5: DI-Registrierung + SyncWorker-Block + Fail-Safe-Test

**Files:**
- Modify: `IDEALAKEWMSService/Program.cs:53-60` (2 Registrierungen)
- Modify: `IDEALAKEWMSService/Workers/SyncWorker.cs:54` (Block NACH dem FA-Import-Block)
- Modify: `IDEALAKEWMSService.Tests/Workers/SyncWorkerTests.cs` (neuer Test)
- Test: `dotnet test IDEALAKEWMSService.Tests --nologo --filter "FullyQualifiedName~SyncWorkerTests"`

- [ ] **Step 1: DI in Program.cs**

In `IDEALAKEWMSService/Program.cs` nach der Zeile `builder.Services.AddScoped<ISageBestandReader, SageBestandReader>();` (Z. 53) einfuegen:

```csharp
    builder.Services.AddScoped<ISageZusatzinfoReader, SageZusatzinfoReader>();
    builder.Services.AddScoped<IFaZusatzinfoSyncService, FaZusatzinfoSyncService>();
```

- [ ] **Step 2: SyncWorker-Block**

In `IDEALAKEWMSService/Workers/SyncWorker.cs` DIREKT NACH dem Produktionsauftraege-Block (endet Z. 54 mit `}`) und VOR dem `// Artikel sync`-Kommentar einfuegen:

```csharp
                // FA-Zusatzinfos aus Sage (v1.26.0) — direkt NACH dem FA-Import.
                // Gate DB-first (Default false); Resolve INNERHALB des gegateten Blocks,
                // sonst werfen die SyncWorkerTests (Mock-Provider kennt nur ISageImportService).
                // Akzeptiert: schlaegt der FA-Import fehl/ist er aus, laeuft dieser Sync gegen
                // den alten FA-Stand — neue WAs zaehlen als uebersprungen und heilen sich im
                // Folgezyklus (erhoehte Skip-Counts nach FA-Import-Fehlern sind KEIN Bug).
                if (await ServiceSettings.GetBoolSafeAsync(_configuration, "Sync:FaZusatzinfoEnabled", false, stoppingToken))
                {
                    await RunResilientAsync("FA-Zusatzinfo-Sync", async () =>
                    {
                        var zusatzinfoSync = scope.ServiceProvider.GetRequiredService<IFaZusatzinfoSyncService>();

                        _logger.LogInformation("FA-Zusatzinfo-Sync startet...");
                        var ziResult = await zusatzinfoSync.SyncAsync(dryRun, stoppingToken);
                        _logger.LogInformation(
                            "FA-Zusatzinfo-Sync: {Inserted} neu, {Updated} aktualisiert, {Errors} Fehler.{Details}",
                            ziResult.Inserted, ziResult.Updated, ziResult.Errors,
                            ziResult.ErrorDetails != null ? $" Details: {ziResult.ErrorDetails}" : "");
                    }, stoppingToken);
                }
```

- [ ] **Step 3: Fail-Safe-Invarianten-Test**

Ans Ende von `IDEALAKEWMSService.Tests/Workers/SyncWorkerTests.cs` (vor der schliessenden Klassen-Klammer) einfuegen. Hinweis: das ist ein Invarianten-Test (kein klassisches RED — die Absicherung ist `Times.Never` gegen den frisch verdrahteten Block):

```csharp
    [Fact]
    public async Task SyncWorker_SkipsFaZusatzinfoSync_WhenDbUnreachable()
    {
        // Fail-Safe-Invariante (v1.26.0): Sync:FaZusatzinfoEnabled hat Default false →
        // ohne erreichbare DB laeuft der Block NICHT und IFaZusatzinfoSyncService wird
        // NIE resolved (der Mock-Provider kennt nur ISageImportService — ein Resolve
        // wuerde InvalidOperationException werfen). Der IConfiguration-Seed "true"
        // wirkt NICHT (Gate liest DB-first via ServiceSettings.GetBoolSafeAsync).
        var mockSageImport = new Mock<ISageImportService>();
        mockSageImport.Setup(x => x.SyncProductionOrdersAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncResult(1, 2, 0));
        mockSageImport.Setup(x => x.SyncArticlesAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncResult(3, 0, 0));

        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider
            .Setup(x => x.GetService(typeof(ISageImportService)))
            .Returns(mockSageImport.Object);

        var mockScope = new Mock<IServiceScope>();
        mockScope.Setup(x => x.ServiceProvider).Returns(mockServiceProvider.Object);
        var mockScopeFactory = new Mock<IServiceScopeFactory>();
        mockScopeFactory.Setup(x => x.CreateScope()).Returns(mockScope.Object);

        var config = BuildConfig(new()
        {
            ["WorkerSettings:SyncIntervalMinutes"] = "0",
            ["WorkerSettings:SyncDryRun"] = "false",
            ["Sync:FaZusatzinfoEnabled"] = "true",
        });

        using var worker = new SyncWorker(Mock.Of<ILogger<SyncWorker>>(), config, mockScopeFactory.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        var run = async () =>
        {
            await worker.StartAsync(cts.Token);
            await Task.Delay(150);
            await worker.StopAsync(CancellationToken.None);
        };

        await run.Should().NotThrowAsync();
        mockServiceProvider.Verify(x => x.GetService(typeof(IFaZusatzinfoSyncService)), Times.Never());
    }
```

- [ ] **Step 4: Alle Service-Tests laufen lassen**

Run: `dotnet test IDEALAKEWMSService.Tests --nologo`
Expected: PASS — alle bestehenden SyncWorkerTests + neuer Test gruen (Baseline-Anzahl + 9 seit Task 4/5).

- [ ] **Step 5: Commit**

```powershell
git add IDEALAKEWMSService/Program.cs IDEALAKEWMSService/Workers/SyncWorker.cs IDEALAKEWMSService.Tests/Workers/SyncWorkerTests.cs
git commit -m @'
feat(fa-zusatzinfo): SyncWorker-Block hinter Sync:FaZusatzinfoEnabled + DI

Eigener RunResilientAsync-Block direkt NACH dem FA-Import (Gate DB-first,
Default false, Resolve INNERHALB des Blocks). Fail-Safe-Invarianten-Test:
ohne DB kein Lauf, kein Resolve, kein Crash.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
'@
```

---

### Task 6: Repository — `GetExtraInfoAsync`, Include, `LeitstandOrderRow`-Projektion, 5 Filter-Keys (TDD)

**Files:**
- Modify: `IdealAkeWms/Data/Repositories/IProductionOrderRepository.cs:5-18` (Record) + `:22-58` (Interface + Doku)
- Modify: `IdealAkeWms/Data/Repositories/ProductionOrderRepository.cs:12-19` (Include), `:60-78` (Projektion), `:148-183` (Filter-Switch), Ende (GetExtraInfoAsync)
- Test: `IdealAkeWms.Tests/Repositories/ProductionOrderRepositoryTests.cs`

- [ ] **Step 1: Repo-Tests schreiben (RED — kompiliert noch nicht)**

Ans Ende von `IdealAkeWms.Tests/Repositories/ProductionOrderRepositoryTests.cs` (vor der schliessenden Klassen-Klammer) einfuegen:

```csharp
    // ------------------------------------------------------------------
    // FA-Zusatzinfos (Sage, v1.26.0)
    // ------------------------------------------------------------------

    private static ProductionOrderExtraInfo MakeExtraInfo(int productionOrderId,
        string? kaelte = "R290", string? ventil = "Danfoss", string? ausfuehrung = "E",
        string? maschine = "M1", string? status = "in Produktion") =>
        new()
        {
            ProductionOrderId = productionOrderId,
            Kaeltemittel = kaelte,
            Ventil = ventil,
            AusfuehrungEZ = ausfuehrung,
            Maschine = maschine,
            SageStatus = status,
            CreatedAt = DateTime.Now,
            CreatedBy = "t",
            CreatedByWindows = "t"
        };

    [Fact]
    public async Task GetExtraInfoAsync_ReturnsSatellite_OrNull()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new ProductionOrderRepository(ctx);
        ctx.ProductionOrders.Add(new ProductionOrder { Id = 1, OrderNumber = "FA-1" });
        ctx.ProductionOrders.Add(new ProductionOrder { Id = 2, OrderNumber = "FA-2" });
        ctx.ProductionOrderExtraInfos.Add(MakeExtraInfo(1));
        await ctx.SaveChangesAsync();

        var info = await repo.GetExtraInfoAsync(1);
        info.Should().NotBeNull();
        info!.Kaeltemittel.Should().Be("R290");

        (await repo.GetExtraInfoAsync(2)).Should().BeNull();
    }

    [Fact]
    public async Task GetAllOrderedAsync_IncludesExtraInfo()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new ProductionOrderRepository(ctx);
        ctx.ProductionOrders.Add(new ProductionOrder { Id = 1, OrderNumber = "FA-1" });
        ctx.ProductionOrderExtraInfos.Add(MakeExtraInfo(1));
        await ctx.SaveChangesAsync();

        var result = await repo.GetAllOrderedAsync();

        result.Should().ContainSingle().Which.ExtraInfo.Should().NotBeNull();
        result[0].ExtraInfo!.Kaeltemittel.Should().Be("R290");
    }

    [Fact]
    public async Task GetForLeitstand_ProjectsExtraInfoFields()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new ProductionOrderRepository(ctx);
        ctx.ProductionOrders.Add(new ProductionOrder { Id = 1, OrderNumber = "FA-1" });
        ctx.ProductionOrders.Add(new ProductionOrder { Id = 2, OrderNumber = "FA-OHNE" });
        ctx.ProductionOrderExtraInfos.Add(MakeExtraInfo(1));
        await ctx.SaveChangesAsync();

        var page = await repo.GetForLeitstandAsync(null, null, null, showDone: false, page: 1, pageSize: 100);

        var row = page.Rows.Single(r => r.OrderNumber == "FA-1");
        row.Kaeltemittel.Should().Be("R290");
        row.Ventil.Should().Be("Danfoss");
        row.AusfuehrungEZ.Should().Be("E");
        row.Maschine.Should().Be("M1");
        row.SageStatus.Should().Be("in Produktion");

        var empty = page.Rows.Single(r => r.OrderNumber == "FA-OHNE");
        empty.Kaeltemittel.Should().BeNull();
        empty.SageStatus.Should().BeNull();
    }

    [Fact]
    public async Task GetForLeitstand_FiltersOnKaeltemittel_CaseInsensitive_WithNullGuard()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new ProductionOrderRepository(ctx);
        ctx.ProductionOrders.Add(new ProductionOrder { Id = 1, OrderNumber = "FA-R290" });
        ctx.ProductionOrders.Add(new ProductionOrder { Id = 2, OrderNumber = "FA-R134" });
        ctx.ProductionOrders.Add(new ProductionOrder { Id = 3, OrderNumber = "FA-NULL" });
        ctx.ProductionOrderExtraInfos.Add(MakeExtraInfo(1, kaelte: "R290"));
        ctx.ProductionOrderExtraInfos.Add(MakeExtraInfo(2, kaelte: "R134a"));
        await ctx.SaveChangesAsync();

        // Positiv: Token ist lowercase (ColumnFilterHelper.Parse lowercased) — Wert "R290"
        // matcht via ToLower().Contains. FA ohne ExtraInfo faellt beim Positiv-Filter raus.
        var page = await repo.GetForLeitstandAsync(null, null, null, showDone: false, page: 1, pageSize: 100,
            columnFilters: new Dictionary<string, string> { ["kaeltemittel"] = "r290" });
        page.Rows.Should().ContainSingle(r => r.OrderNumber == "FA-R290");
        page.TotalCount.Should().Be(1);

        // Negation: !r290 zeigt R134a UND die Null-Zeile (leere Zelle matcht NOT).
        var negPage = await repo.GetForLeitstandAsync(null, null, null, showDone: false, page: 1, pageSize: 100,
            columnFilters: new Dictionary<string, string> { ["kaeltemittel"] = "!r290" });
        negPage.Rows.Select(r => r.OrderNumber).Should().BeEquivalentTo(new[] { "FA-R134", "FA-NULL" });
    }

    [Fact]
    public async Task GetForLeitstand_FiltersOnSageStatus_OrTokens()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new ProductionOrderRepository(ctx);
        ctx.ProductionOrders.Add(new ProductionOrder { Id = 1, OrderNumber = "FA-1" });
        ctx.ProductionOrders.Add(new ProductionOrder { Id = 2, OrderNumber = "FA-2" });
        ctx.ProductionOrders.Add(new ProductionOrder { Id = 3, OrderNumber = "FA-3" });
        ctx.ProductionOrderExtraInfos.Add(MakeExtraInfo(1, status: "verpackt"));
        ctx.ProductionOrderExtraInfos.Add(MakeExtraInfo(2, status: "abgeholt"));
        ctx.ProductionOrderExtraInfos.Add(MakeExtraInfo(3, status: "begonnen"));
        await ctx.SaveChangesAsync();

        // Komma-OR: verpackt ODER abgeholt
        var page = await repo.GetForLeitstandAsync(null, null, null, showDone: false, page: 1, pageSize: 100,
            columnFilters: new Dictionary<string, string> { ["sage-status"] = "verpackt,abgeholt" });

        page.Rows.Select(r => r.OrderNumber).Should().BeEquivalentTo(new[] { "FA-1", "FA-2" });
    }
```

- [ ] **Step 2: Kompilieren → RED bestaetigen**

Run: `dotnet build IdealAkeWms.slnx --nologo -clp:ErrorsOnly`
Expected: FAIL — `GetExtraInfoAsync` fehlt am Interface, `LeitstandOrderRow.Kaeltemittel` existiert nicht.

- [ ] **Step 3: Record + Interface erweitern**

In `IdealAkeWms/Data/Repositories/IProductionOrderRepository.cs` das Record ersetzen (5 neue optionale Parameter am ENDE — Default `null`, damit die bestehenden target-typed `new(...)`-Aufrufe in `ProductionOrdersControllerSlimTests`/`PickingLeitstandControllerTests` weiter kompilieren):

```csharp
public record LeitstandOrderRow(
    int Id,
    string OrderNumber,
    decimal Quantity,
    string? Customer,
    string? ArticleNumber,
    string? Description1,
    string? Description2,
    DateTime? ProductionDate,
    DateTime? DeliveryDate,
    bool IsDone,
    bool IsDonePicking,
    bool IsCancelled,
    string? WorkplaceName,
    // FA-Zusatzinfos aus Sage (v1.26.0) — aus der ExtraInfo-Projektion,
    // Defaults halten bestehende Konstruktor-Aufrufe kompatibel.
    string? Kaeltemittel = null,
    string? Ventil = null,
    string? AusfuehrungEZ = null,
    string? Maschine = null,
    string? SageStatus = null);
```

Im Interface `IProductionOrderRepository` nach `GetByArticleNumbersAsync` ergaenzen und den `columnFilters`-Doku-Kommentar von `GetForLeitstandAsync` aktualisieren:

```csharp
    /// <summary>
    /// FA-Zusatzinfos (Sage, v1.26.0): liest den 1:1-Satelliten zu einem FA
    /// (ein gezielter Read, AsNoTracking). Null wenn (noch) keine Sage-Daten da sind.
    /// </summary>
    Task<ProductionOrderExtraInfo?> GetExtraInfoAsync(int productionOrderId);
```

```csharp
    /// <param name="columnFilters">
    /// Optionale Spalten-Filter aus der URL (<c>colf_&lt;col-key&gt;=value</c>).
    /// Bekannte Keys: order-number, customer, article-number, description1,
    /// description2, workbench, kaeltemittel, ventil, ausfuehrung, maschine,
    /// sage-status. OR-/NOT-Syntax wie clientseitig.
    /// </param>
```

- [ ] **Step 4: Repository implementieren**

In `IdealAkeWms/Data/Repositories/ProductionOrderRepository.cs`:

(a) `GetAllOrderedAsync` (Z. 12-19) ersetzen:

```csharp
    public async Task<List<ProductionOrder>> GetAllOrderedAsync()
    {
        return await _dbSet
            .Include(o => o.ProductionWorkplace)
            .Include(o => o.PickingStatus)
            .Include(o => o.ExtraInfo)
            .OrderBy(o => o.OrderNumber)
            .ToListAsync();
    }
```

(b) Projektion in `GetForLeitstandAsync` (Z. 60-78) ersetzen:

```csharp
        var rows = await q
            .OrderBy(o => o.OrderNumber)
            .Skip(skip)
            .Take(pageSize)
            .Select(o => new LeitstandOrderRow(
                o.Id,
                o.OrderNumber,
                o.Quantity,
                o.Customer,
                o.ArticleNumber,
                o.Description1,
                o.Description2,
                o.ProductionDate,
                o.DeliveryDate,
                o.IsDone,
                o.PickingStatus != null && o.PickingStatus.IsDonePicking,
                o.IsCancelled,
                o.ProductionWorkplace != null ? o.ProductionWorkplace.Name : null,
                o.ExtraInfo != null ? o.ExtraInfo.Kaeltemittel : null,
                o.ExtraInfo != null ? o.ExtraInfo.Ventil : null,
                o.ExtraInfo != null ? o.ExtraInfo.AusfuehrungEZ : null,
                o.ExtraInfo != null ? o.ExtraInfo.Maschine : null,
                o.ExtraInfo != null ? o.ExtraInfo.SageStatus : null))
            .ToListAsync();
```

(c) In `ApplyLeitstandColumnFilter` (~Z. 181) VOR dem `_ => q`-Default die 5 neuen Cases einfuegen. WICHTIG: bewusst `Contains` + `ToLower()` statt `EF.Functions.Like` (Spec §5.3 — InMemory-testbar; Tokens sind bereits lowercase aus `ColumnFilterHelper.Parse`, SQL Server uebersetzt `ToLower().Contains` zu `LOWER(...) LIKE`; identische Semantik zum Client-Filter):

```csharp
            // FA-Zusatzinfos (Sage, v1.26.0): Contains-basiert mit Null-Guards —
            // KEIN EF.Functions.Like (InMemory-Testbarkeit, Spec §5.3). Tokens sind
            // lowercase (ColumnFilterHelper.Parse), daher ToLower() auf dem Wert.
            "kaeltemittel" => negate
                ? q.Where(o => o.ExtraInfo == null || o.ExtraInfo.Kaeltemittel == null
                            || !tokens.Any(t => o.ExtraInfo.Kaeltemittel!.ToLower().Contains(t)))
                : q.Where(o => o.ExtraInfo != null && o.ExtraInfo.Kaeltemittel != null
                            && tokens.Any(t => o.ExtraInfo.Kaeltemittel!.ToLower().Contains(t))),

            "ventil" => negate
                ? q.Where(o => o.ExtraInfo == null || o.ExtraInfo.Ventil == null
                            || !tokens.Any(t => o.ExtraInfo.Ventil!.ToLower().Contains(t)))
                : q.Where(o => o.ExtraInfo != null && o.ExtraInfo.Ventil != null
                            && tokens.Any(t => o.ExtraInfo.Ventil!.ToLower().Contains(t))),

            "ausfuehrung" => negate
                ? q.Where(o => o.ExtraInfo == null || o.ExtraInfo.AusfuehrungEZ == null
                            || !tokens.Any(t => o.ExtraInfo.AusfuehrungEZ!.ToLower().Contains(t)))
                : q.Where(o => o.ExtraInfo != null && o.ExtraInfo.AusfuehrungEZ != null
                            && tokens.Any(t => o.ExtraInfo.AusfuehrungEZ!.ToLower().Contains(t))),

            "maschine" => negate
                ? q.Where(o => o.ExtraInfo == null || o.ExtraInfo.Maschine == null
                            || !tokens.Any(t => o.ExtraInfo.Maschine!.ToLower().Contains(t)))
                : q.Where(o => o.ExtraInfo != null && o.ExtraInfo.Maschine != null
                            && tokens.Any(t => o.ExtraInfo.Maschine!.ToLower().Contains(t))),

            "sage-status" => negate
                ? q.Where(o => o.ExtraInfo == null || o.ExtraInfo.SageStatus == null
                            || !tokens.Any(t => o.ExtraInfo.SageStatus!.ToLower().Contains(t)))
                : q.Where(o => o.ExtraInfo != null && o.ExtraInfo.SageStatus != null
                            && tokens.Any(t => o.ExtraInfo.SageStatus!.ToLower().Contains(t))),
```

(d) Ans Ende der Klasse (nach `ApplyLeitstandColumnFilter`) die neue Methode:

```csharp
    public async Task<ProductionOrderExtraInfo?> GetExtraInfoAsync(int productionOrderId)
    {
        return await _context.ProductionOrderExtraInfos
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.ProductionOrderId == productionOrderId);
    }
```

- [ ] **Step 5: Tests laufen lassen (GREEN)**

Run: `dotnet test IdealAkeWms.Tests --nologo --filter "FullyQualifiedName~ProductionOrderRepositoryTests"`
Expected: PASS — alle, inkl. der 5 neuen Tests.

- [ ] **Step 6: Voll-Build + Web-Regressionslauf**

Run: `dotnet build IdealAkeWms.slnx --nologo -clp:ErrorsOnly; dotnet test IdealAkeWms.Tests --nologo`
Expected: 0 Errors, alle Tests gruen (Record-Erweiterung mit Defaults bricht keine Aufrufer).

- [ ] **Step 7: Commit**

```powershell
git add IdealAkeWms/Data/Repositories/IProductionOrderRepository.cs IdealAkeWms/Data/Repositories/ProductionOrderRepository.cs IdealAkeWms.Tests/Repositories/ProductionOrderRepositoryTests.cs
git commit -m @'
feat(fa-zusatzinfo): Repository-Datenpfad — Projektion, Include, Filter-Keys, GetExtraInfoAsync

LeitstandOrderRow +5 nullable Strings (Defaults halten Aufrufer kompatibel),
GetForLeitstandAsync projiziert ExtraInfo (Include waere wirkungslos),
ApplyLeitstandColumnFilter +5 Keys (Contains+ToLower, Null-Guards, InMemory-testbar,
KEIN EF.Functions.Like), GetAllOrderedAsync inkludiert ExtraInfo,
neue gezielte Methode GetExtraInfoAsync. TDD: 5 neue Repo-Tests.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
'@
```

---

### Task 7: FA-Vervollstaendigung — Reiter „ALLGEMEIN" + Reserved-Code im WorkSteps-CRUD (TDD)

**Files:**
- Modify: `IdealAkeWms/Models/ViewModels/FaCompletionEditViewModel.cs` (+6 Properties)
- Modify: `IdealAkeWms/Controllers/FaCompletionController.cs:173-275` (Edit), `:304-326` (SetWorkplace), `:364-381` (RemoveWorkStep)
- Modify: `IdealAkeWms/Views/FaCompletion/Edit.cshtml:53-70` (SetWorkplace-Form), `:82-96` (Tab-Leiste), `:114-125` (Pane-Verzweigung), `:153-159` (RemoveWorkStep-Form)
- Modify: `IdealAkeWms/Controllers/WorkStepsController.cs:70-98` (Create-POST), `:110-145` (Edit-POST)
- Test: `IdealAkeWms.Tests/Controllers/FaCompletionControllerTests.cs`, `IdealAkeWms.Tests/Controllers/WorkStepsControllerTests.cs`

- [ ] **Step 1: Controller-Tests schreiben (RED)**

Ans Ende von `IdealAkeWms.Tests/Controllers/FaCompletionControllerTests.cs` (vor der schliessenden Klassen-Klammer) einfuegen:

```csharp
    // ---------------------------------------------------------- Reiter ALLGEMEIN (v1.26.0)

    private static void SeedExtraInfo(ApplicationDbContext ctx, int productionOrderId,
        string? kaelte = "R290", string? ventil = "Danfoss", string? ausfuehrung = "E",
        string? maschine = "M1", string? sageStatus = "in Produktion")
    {
        ctx.ProductionOrderExtraInfos.Add(new ProductionOrderExtraInfo
        {
            ProductionOrderId = productionOrderId,
            Kaeltemittel = kaelte,
            Ventil = ventil,
            AusfuehrungEZ = ausfuehrung,
            Maschine = maschine,
            SageStatus = sageStatus,
            CreatedAt = DateTime.Now,
            CreatedBy = "t",
            CreatedByWindows = "t"
        });
        ctx.SaveChanges();
    }

    [Fact]
    public async Task Edit_TabAllgemein_SetsActiveTabAndExtraFields()
    {
        var (ctx, ctrl, _) = Build();
        var o = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-001");
        var vk = SeedWorkStep(ctx, "VK", "Kuehlung");
        SeedFaWorkStep(ctx, o.Order.Id, vk.Id);
        SeedExtraInfo(ctx, o.Order.Id);

        var result = await ctrl.Edit(o.Order.Id, tab: "ALLGEMEIN");

        var vm = result.Should().BeOfType<ViewResult>().Subject
            .Model.Should().BeOfType<FaCompletionEditViewModel>().Subject;
        vm.ActiveTab.Should().Be("ALLGEMEIN");
        vm.HasExtraInfo.Should().BeTrue();
        vm.ExtraKaeltemittel.Should().Be("R290");
        vm.ExtraVentil.Should().Be("Danfoss");
        vm.ExtraAusfuehrungEZ.Should().Be("E");
        vm.ExtraMaschine.Should().Be("M1");
        vm.ExtraSageStatus.Should().Be("in Produktion");
    }

    [Fact]
    public async Task Edit_TabAllgemein_IsCaseInsensitive()
    {
        var (ctx, ctrl, _) = Build();
        var o = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-001");

        var result = await ctrl.Edit(o.Order.Id, tab: "allgemein");

        var vm = ((ViewResult)result).Model.Should().BeOfType<FaCompletionEditViewModel>().Subject;
        vm.ActiveTab.Should().Be("ALLGEMEIN");
    }

    [Fact]
    public async Task Edit_DefaultTab_StaysFirstWorkStep_WhenAgsExist()
    {
        // User-Entscheid Spec §5.1: Default-aktiv bleibt der erste FA-Vorbau-AG-Reiter.
        var (ctx, ctrl, _) = Build();
        var o = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-001");
        var vk = SeedWorkStep(ctx, "VK", "Kuehlung");
        SeedFaWorkStep(ctx, o.Order.Id, vk.Id);
        SeedExtraInfo(ctx, o.Order.Id);

        var result = await ctrl.Edit(o.Order.Id);

        var vm = ((ViewResult)result).Model.Should().BeOfType<FaCompletionEditViewModel>().Subject;
        vm.ActiveTab.Should().Be("VK");
    }

    [Fact]
    public async Task Edit_NoWorkSteps_DefaultsToAllgemein()
    {
        var (ctx, ctrl, _) = Build();
        var o = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-001");

        var result = await ctrl.Edit(o.Order.Id);

        var vm = ((ViewResult)result).Model.Should().BeOfType<FaCompletionEditViewModel>().Subject;
        vm.ActiveTab.Should().Be("ALLGEMEIN");
        vm.Tabs.Should().BeEmpty();
    }

    [Fact]
    public async Task Edit_NoExtraInfo_HasExtraInfoFalse()
    {
        var (ctx, ctrl, _) = Build();
        var o = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-001");

        var result = await ctrl.Edit(o.Order.Id, tab: "ALLGEMEIN");

        var vm = ((ViewResult)result).Model.Should().BeOfType<FaCompletionEditViewModel>().Subject;
        vm.HasExtraInfo.Should().BeFalse();
        vm.ExtraKaeltemittel.Should().BeNull();
    }

    [Fact]
    public async Task SetWorkplace_PreservesTab()
    {
        var (ctx, ctrl, _) = Build();
        var o = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-001");
        var wp = new ProductionWorkplace { Name = "WB-1", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        ctx.ProductionWorkplaces.Add(wp);
        ctx.SaveChanges();

        var result = await ctrl.SetWorkplace(o.Order.Id, wp.Id, tab: "ALLGEMEIN");

        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be(nameof(FaCompletionController.Edit));
        redirect.RouteValues!["tab"].Should().Be("ALLGEMEIN");
    }
```

Ans Ende von `IdealAkeWms.Tests/Controllers/WorkStepsControllerTests.cs` (vor der schliessenden Klassen-Klammer) einfuegen:

```csharp
    // ------------------------------------------------ Reservierter Code ALLGEMEIN (v1.26.0)

    [Theory]
    [InlineData("ALLGEMEIN")]
    [InlineData("allgemein")]
    [InlineData(" Allgemein ")]
    public async Task Create_RejectsReservedCodeAllgemein(string code)
    {
        var (ctrl, repo) = CreateController();
        repo.Setup(r => r.GetByCodeAsync(It.IsAny<string>())).ReturnsAsync((WorkStep?)null);

        var result = await ctrl.Create(new WorkStep { Code = code, Name = "Pseudo" });

        result.Should().BeOfType<ViewResult>();
        ctrl.ModelState.IsValid.Should().BeFalse();
        ctrl.ModelState[nameof(WorkStep.Code)]!.Errors.Should().NotBeEmpty();
        repo.Verify(r => r.AddAsync(It.IsAny<WorkStep>()), Times.Never);
    }

    [Fact]
    public async Task Edit_RejectsReservedCodeAllgemein()
    {
        var (ctrl, repo) = CreateController();
        repo.Setup(r => r.GetByCodeAsync(It.IsAny<string>())).ReturnsAsync((WorkStep?)null);

        var result = await ctrl.Edit(7, new WorkStep { Id = 7, Code = "ALLGEMEIN", Name = "Pseudo" });

        result.Should().BeOfType<ViewResult>();
        ctrl.ModelState.IsValid.Should().BeFalse();
        repo.Verify(r => r.UpdateAsync(It.IsAny<WorkStep>()), Times.Never);
    }
```

- [ ] **Step 2: Kompilieren/Tests → RED bestaetigen**

Run: `dotnet test IdealAkeWms.Tests --nologo --filter "FullyQualifiedName~FaCompletionControllerTests|FullyQualifiedName~WorkStepsControllerTests"`
Expected: FAIL — Compile-Errors (ViewModel-Properties, `SetWorkplace`-Signatur) bzw. rote Reserved-Code-Tests.

- [ ] **Step 3: ViewModel erweitern**

In `IdealAkeWms/Models/ViewModels/FaCompletionEditViewModel.cs` in der Klasse `FaCompletionEditViewModel` nach `EnaioDmsLinks` ergaenzen:

```csharp
    // ------------------------------------------------------------------
    // FA-Zusatzinfos aus Sage (v1.26.0) — read-only Reiter „ALLGEMEIN".
    // ------------------------------------------------------------------
    public bool HasExtraInfo { get; set; }
    public string? ExtraKaeltemittel { get; set; }
    public string? ExtraVentil { get; set; }
    public string? ExtraAusfuehrungEZ { get; set; }
    public string? ExtraMaschine { get; set; }
    public string? ExtraSageStatus { get; set; }
```

- [ ] **Step 4: FaCompletionController anpassen**

(a) In `Edit` (Z. 249-251) den ActiveTab-Block ersetzen:

```csharp
        // Reiter ALLGEMEIN (v1.26.0): Pseudo-Tab fuer die read-only FA-Zusatzinfos aus
        // Sage — NICHT in tabs. Vergleich OrdinalIgnoreCase. Default-aktiv bleibt der
        // erste FA-Vorbau-AG-Reiter (User-Entscheid); FA ganz ohne AG-Reiter -> ALLGEMEIN.
        const string allgemeinTab = "ALLGEMEIN";
        string activeTab;
        if (!string.IsNullOrWhiteSpace(tab)
            && string.Equals(tab, allgemeinTab, StringComparison.OrdinalIgnoreCase))
        {
            activeTab = allgemeinTab;
        }
        else if (!string.IsNullOrWhiteSpace(tab) && tabs.Any(t => t.Code == tab))
        {
            activeTab = tab!;
        }
        else
        {
            activeTab = tabs.FirstOrDefault()?.Code ?? allgemeinTab;
        }

        var extraInfo = await _productionOrderRepository.GetExtraInfoAsync(id);
```

(b) Im `vm`-Initializer (Z. 253-272) die Zeile `ActiveTab = activeTab,` beibehalten und NACH `Tabs = tabs,` ergaenzen:

```csharp
            HasExtraInfo = extraInfo != null,
            ExtraKaeltemittel = extraInfo?.Kaeltemittel,
            ExtraVentil = extraInfo?.Ventil,
            ExtraAusfuehrungEZ = extraInfo?.AusfuehrungEZ,
            ExtraMaschine = extraInfo?.Maschine,
            ExtraSageStatus = extraInfo?.SageStatus,
```

(c) `SetWorkplace` (Z. 307-326): Signatur + Redirect um `tab` erweitern (Tab-Erhalt-Nit aus Spec §5.1):

```csharp
    // POST /FaCompletion/SetWorkplace
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetWorkplace(int id, int? workplaceId, string? tab = null)
    {
        var order = await _productionOrderRepository.GetByIdAsync(id);
        if (order == null)
        {
            return NotFound();
        }

        order.ProductionWorkplaceId = workplaceId;
        order.ModifiedAt = DateTime.Now;
        order.ModifiedBy = _currentUser.GetDisplayName();
        order.ModifiedByWindows = _currentUser.GetWindowsUserName();

        await _productionOrderRepository.UpdateAsync(order);

        TempData["SuccessMessage"] = workplaceId.HasValue
            ? "Werkbank zugewiesen."
            : "Werkbank-Zuweisung entfernt.";
        return RedirectToAction(nameof(Edit), new { id, tab });
    }
```

(d) `RemoveWorkStep` (Z. 364-381): Signatur + Redirect um `tab` erweitern:

```csharp
    // POST /FaCompletion/RemoveWorkStep
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveWorkStep(int id, int workStepId, string? tab = null)
    {
        var step = await _workStepRepository.GetByIdAsync(workStepId);
        if (step == null)
        {
            return NotFound();
        }

        await _faWorkStepRepository.SetActiveAsync(
            id, workStepId, active: false,
            _currentUser.GetDisplayName(), _currentUser.GetWindowsUserName());

        TempData["SuccessMessage"] = $"Arbeitsgang {step.Code} entfernt.";
        return RedirectToAction(nameof(Edit), new { id, tab });
    }
```

(Wird der gerade aktive AG entfernt, faellt der Edit-GET ueber die ActiveTab-Logik sauber auf den ersten verbleibenden Tab bzw. ALLGEMEIN zurueck.)

- [ ] **Step 5: WorkStepsController — Reserved-Code-Check**

(a) In `Create` (POST, Z. 70-98) VOR dem bestehenden Duplicate-Check einfuegen:

```csharp
        // Reserviert (v1.26.0): "ALLGEMEIN" ist der Pseudo-Tab der FA-Vervollstaendigung
        // (FA-Zusatzinfos aus Sage) — als WorkStep-Code verboten, case-insensitiv.
        if (!string.IsNullOrWhiteSpace(model.Code)
            && string.Equals(model.Code.Trim(), "ALLGEMEIN", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(WorkStep.Code),
                "Code 'ALLGEMEIN' ist reserviert (Reiter der FA-Vervollstaendigung).");
        }
```

(b) In `Edit` (POST, Z. 110-145) denselben Block VOR dem bestehenden Duplicate-Check einfuegen (identischer Code wie (a)).

- [ ] **Step 6: View Edit.cshtml anpassen**

(a) SetWorkplace-Form (Z. 54-70): nach `<input type="hidden" name="id" ... />` ergaenzen:

```html
                    <input type="hidden" name="tab" value="@Model.ActiveTab" />
```

(b) Tab-Leiste (Z. 84): in `<ul class="nav nav-tabs mb-3">` VOR der `@foreach (var t in Model.Tabs)`-Schleife den Pseudo-Tab einfuegen (bewusst OHNE ✓/●-Indikator — der basiert auf IsSpecComplete, das es hier nicht gibt):

```html
        @* Pseudo-Tab ALLGEMEIN (v1.26.0): FA-Zusatzinfos aus Sage, read-only — NICHT in Model.Tabs *@
        <li class="nav-item">
            <a class="nav-link @(Model.ActiveTab == "ALLGEMEIN" ? "active" : "")"
               asp-action="Edit" asp-route-id="@Model.ProductionOrderId" asp-route-tab="ALLGEMEIN">
                <strong>ALLGEMEIN</strong>
            </a>
        </li>
```

(c) Pane-Verzweigung (Z. 114-125): den Block

```html
@{
    var active = Model.Tabs.FirstOrDefault(t => t.Code == Model.ActiveTab);
}

@if (active == null)
{
    <div class="alert alert-info">
        Für diesen FA sind noch keine FA-Vorbau-AG aktiv. Über „+ FA-Vorbau-AG" einen FA-Vorbau-Arbeitsgang hinzufügen.
    </div>
}
else
```

ersetzen durch (der ALLGEMEIN-Zweig MUSS VOR dem `active == null`-Alert laufen, sonst frisst der Alert den Inhalt bei FAs mit AG-Reitern; im Leer-AG-Fall erscheint der bisherige Hinweis ZUSAETZLICH unter dem Pane):

```html
@{
    var active = Model.Tabs.FirstOrDefault(t => t.Code == Model.ActiveTab);
}

@if (Model.ActiveTab == "ALLGEMEIN")
{
    @* FA-Zusatzinfos aus Sage (v1.26.0) — read-only, Sage ist Master, keine Eingabefelder/POSTs *@
    <div class="tab-content">
        <div class="tab-pane fade show active">
            <div class="card mb-3">
                <div class="card-header">FA-Zusatzinfos aus Sage</div>
                <div class="card-body">
                    @if (!Model.HasExtraInfo)
                    {
                        <p class="text-muted mb-0">Noch keine Zusatzinformationen aus Sage vorhanden.</p>
                    }
                    else
                    {
                        <dl class="row mb-0">
                            <dt class="col-sm-3">Kältemittel</dt>
                            <dd class="col-sm-9">@(string.IsNullOrWhiteSpace(Model.ExtraKaeltemittel) ? "–" : Model.ExtraKaeltemittel)</dd>
                            <dt class="col-sm-3">Ventil</dt>
                            <dd class="col-sm-9">@(string.IsNullOrWhiteSpace(Model.ExtraVentil) ? "–" : Model.ExtraVentil)</dd>
                            <dt class="col-sm-3">Ausführung E/Z</dt>
                            <dd class="col-sm-9">@(string.IsNullOrWhiteSpace(Model.ExtraAusfuehrungEZ) ? "–" : Model.ExtraAusfuehrungEZ)</dd>
                            <dt class="col-sm-3">Maschine</dt>
                            <dd class="col-sm-9">@(string.IsNullOrWhiteSpace(Model.ExtraMaschine) ? "–" : Model.ExtraMaschine)</dd>
                            <dt class="col-sm-3">Sage-Status</dt>
                            <dd class="col-sm-9">@(string.IsNullOrWhiteSpace(Model.ExtraSageStatus) ? "–" : Model.ExtraSageStatus)</dd>
                        </dl>
                    }
                </div>
            </div>
        </div>
    </div>

    @if (Model.Tabs.Count == 0)
    {
        <div class="alert alert-info">
            Für diesen FA sind noch keine FA-Vorbau-AG aktiv. Über „+ FA-Vorbau-AG" einen FA-Vorbau-Arbeitsgang hinzufügen.
        </div>
    }
}
else if (active == null)
{
    <div class="alert alert-info">
        Für diesen FA sind noch keine FA-Vorbau-AG aktiv. Über „+ FA-Vorbau-AG" einen FA-Vorbau-Arbeitsgang hinzufügen.
    </div>
}
else
```

(Der bestehende `else { <div class="tab-content"> ... }`-Block bleibt unveraendert.)

(d) RemoveWorkStep-Form (Z. 153-159): nach `<input type="hidden" name="workStepId" ... />` ergaenzen:

```html
                    <input type="hidden" name="tab" value="@Model.ActiveTab" />
```

- [ ] **Step 7: Tests laufen lassen (GREEN)**

Run: `dotnet test IdealAkeWms.Tests --nologo --filter "FullyQualifiedName~FaCompletionControllerTests|FullyQualifiedName~WorkStepsControllerTests"`
Expected: PASS — alle bestehenden + 8 neue Tests. (Bestehende Tests asserten ActiveTab "VK"/"VL" — bleiben durch den unveraenderten Default gruen.)

- [ ] **Step 8: Commit**

```powershell
git add IdealAkeWms/Models/ViewModels/FaCompletionEditViewModel.cs IdealAkeWms/Controllers/FaCompletionController.cs IdealAkeWms/Views/FaCompletion/Edit.cshtml IdealAkeWms/Controllers/WorkStepsController.cs IdealAkeWms.Tests/Controllers/FaCompletionControllerTests.cs IdealAkeWms.Tests/Controllers/WorkStepsControllerTests.cs
git commit -m @'
feat(fa-zusatzinfo): Reiter ALLGEMEIN in der FA-Vervollstaendigung (read-only)

Pseudo-Tab VOR den AG-Reitern (ohne Indikator), Pane-Zweig VOR dem
keine-AGs-Alert, OrdinalIgnoreCase-Tab-Wert, Default bleibt erster AG
(FA ohne AGs -> ALLGEMEIN). Tab-Erhalt fuer SetWorkplace/RemoveWorkStep
(hidden tab-Input). WorkStep-Code ALLGEMEIN im CRUD reserviert
(Create+Edit, case-insensitiv). TDD: 8 neue Controller-Tests.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
'@
```

---

### Task 8: FA-Abarbeitungsliste — 5 Spalten (Default sichtbar) + Prefs-Bug-Fix „FaWorklist" (TDD)

**Files:**
- Modify: `IdealAkeWms/Models/ViewModels/FaWorklistViewModel.cs` (FaWorklistRow +5)
- Modify: `IdealAkeWms/Controllers/FaWorklistController.cs:187-202` (Mapping) + `:311-335` (BuildColumnMap)
- Modify: `IdealAkeWms/Views/FaWorklist/Index.cshtml:16-17` (columnCount), `:83-97` (th), `:113-151` (td), `:162-180` (column-config)
- Modify: `IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs` (NEUE ViewConfig `FaWorklist` + GetByViewKey)
- Test: `IdealAkeWms.Tests/Controllers/FaWorklistControllerTests.cs`, `IdealAkeWms.Tests/Controllers/UserViewPreferencesApiControllerTests.cs`

- [ ] **Step 1: Tests schreiben (RED)**

(a) Ans Ende von `IdealAkeWms.Tests/Controllers/FaWorklistControllerTests.cs` (vor der schliessenden Klassen-Klammer) einfuegen. Die Helper `SeedWorkplace`/`SeedWorkStep` existieren in der Datei; FaWorkStep-Zeilen werden inline geseedet:

```csharp
    // ---------------------------------------------------- FA-Zusatzinfos (Sage, v1.26.0)

    private static void SeedFaWorkStepRow(ApplicationDbContext ctx, int productionOrderId, int workStepId)
    {
        ctx.FaWorkSteps.Add(new FaWorkStep
        {
            ProductionOrderId = productionOrderId,
            WorkStepId = workStepId,
            Source = FaWorkStepSources.Manual,
            CreatedAt = DateTime.Now,
            CreatedBy = "t",
            CreatedByWindows = "t"
        });
        ctx.SaveChanges();
    }

    private static void SeedExtraInfoRow(ApplicationDbContext ctx, int productionOrderId, string kaelte)
    {
        ctx.ProductionOrderExtraInfos.Add(new ProductionOrderExtraInfo
        {
            ProductionOrderId = productionOrderId,
            Kaeltemittel = kaelte,
            Ventil = "Danfoss",
            AusfuehrungEZ = "E",
            Maschine = "M1",
            SageStatus = "in Produktion",
            CreatedAt = DateTime.Now,
            CreatedBy = "t",
            CreatedByWindows = "t"
        });
        ctx.SaveChanges();
    }

    [Fact]
    public async Task Index_MapsExtraInfoColumns()
    {
        var (ctx, ctrl, _) = Build();
        var vk = SeedWorkStep(ctx, "VK", "Kuehlung");
        var o = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-100");
        SeedFaWorkStepRow(ctx, o.Order.Id, vk.Id);
        SeedExtraInfoRow(ctx, o.Order.Id, "R290");

        var result = await ctrl.Index(vk.Id) as ViewResult;

        var vm = result!.Model.Should().BeOfType<FaWorklistViewModel>().Subject;
        var row = vm.Items.Should().ContainSingle().Subject;
        row.Kaeltemittel.Should().Be("R290");
        row.Ventil.Should().Be("Danfoss");
        row.AusfuehrungEZ.Should().Be("E");
        row.Maschine.Should().Be("M1");
        row.SageStatus.Should().Be("in Produktion");
    }

    [Fact]
    public async Task Index_FiltersOnKaeltemittelColumn()
    {
        var (ctx, ctrl, _) = Build();
        var vk = SeedWorkStep(ctx, "VK", "Kuehlung");
        var o1 = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-100");
        var o2 = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-200");
        SeedFaWorkStepRow(ctx, o1.Order.Id, vk.Id);
        SeedFaWorkStepRow(ctx, o2.Order.Id, vk.Id);
        SeedExtraInfoRow(ctx, o1.Order.Id, "R290");
        SeedExtraInfoRow(ctx, o2.Order.Id, "R134a");

        var httpCtx = new DefaultHttpContext();
        httpCtx.Request.QueryString = new QueryString($"?workStepId={vk.Id}&colf_kaeltemittel=r290");
        ctrl.ControllerContext = new ControllerContext { HttpContext = httpCtx };

        var result = await ctrl.Index(vk.Id) as ViewResult;

        var vm = result!.Model.Should().BeOfType<FaWorklistViewModel>().Subject;
        vm.Items.Should().ContainSingle().Which.OrderNumber.Should().Be("FA-100");
        vm.Pagination.TotalCount.Should().Be(1);
    }
```

(b) Ans Ende von `IdealAkeWms.Tests/Controllers/UserViewPreferencesApiControllerTests.cs` (vor der schliessenden Klassen-Klammer) einfuegen:

```csharp
    [Fact]
    public async Task Get_FaWorklistViewKey_IsAccepted()
    {
        // Prefs-Bug-Fix (v1.26.0): "FaWorklist" war nicht in ColumnDefinitions.GetByViewKey
        // registriert -> die API antwortete 400 und Zahnrad-Einstellungen gingen bei jedem
        // Reload verloren. Jetzt: gueltiger ViewKey -> 204 (keine Prefs gespeichert).
        var controller = CreateController();
        _repoMock.Setup(r => r.GetByUserAndViewAsync(42, "FaWorklist"))
            .ReturnsAsync((UserViewPreference?)null);

        var result = await controller.Get("FaWorklist");

        result.Should().BeOfType<NoContentResult>();
    }
```

- [ ] **Step 2: Tests laufen lassen (RED)**

Run: `dotnet test IdealAkeWms.Tests --nologo --filter "FullyQualifiedName~FaWorklistControllerTests|FullyQualifiedName~UserViewPreferencesApiControllerTests"`
Expected: FAIL — Compile-Error (`FaWorklistRow.Kaeltemittel` fehlt); nach Behebung waere `Get_FaWorklistViewKey_IsAccepted` rot (BadRequestObjectResult).

- [ ] **Step 3: FaWorklistRow erweitern**

In `IdealAkeWms/Models/ViewModels/FaWorklistViewModel.cs` in der Klasse `FaWorklistRow` nach `WorkplaceName` ergaenzen:

```csharp
    // FA-Zusatzinfos aus Sage (v1.26.0) — read-only Anzeige-Spalten.
    public string? Kaeltemittel { get; set; }
    public string? Ventil { get; set; }
    public string? AusfuehrungEZ { get; set; }
    public string? Maschine { get; set; }
    public string? SageStatus { get; set; }
```

- [ ] **Step 4: FaWorklistController — Mapping + ColumnMap**

(a) Im Row-Initializer (Z. 187-202) nach `WorkplaceName = order.ProductionWorkplace?.Name,` ergaenzen:

```csharp
                Kaeltemittel = order.ExtraInfo?.Kaeltemittel,
                Ventil = order.ExtraInfo?.Ventil,
                AusfuehrungEZ = order.ExtraInfo?.AusfuehrungEZ,
                Maschine = order.ExtraInfo?.Maschine,
                SageStatus = order.ExtraInfo?.SageStatus,
```

(`GetAllOrderedAsync` laedt `ExtraInfo` seit Task 6 per `.Include` — Nebenwirkung auf `FaCompletion/Index` ist ein unkritischer 1:1-LEFT-JOIN, Spec §5.2.)

(b) In `BuildColumnMap` (Z. 311-335) nach `["production-date"] = ...` die 5 Getter ergaenzen und den XML-Doku-Kommentar der Methode erweitern:

```csharp
    /// <summary>
    /// ColumnMap-Keys: order-number, workbench (= WorkplaceName), article-number, quantity,
    /// bg-date, picking-date, production-date, coating-date, description1, description2,
    /// kaeltemittel, ventil, ausfuehrung, maschine, sage-status (FA-Zusatzinfos, v1.26.0)
    /// + je Merkmal-Spalte dynamisch "attr-{DefinitionId}".
    /// </summary>
```

```csharp
            ["kaeltemittel"] = r => r.Kaeltemittel,
            ["ventil"] = r => r.Ventil,
            ["ausfuehrung"] = r => r.AusfuehrungEZ,
            ["maschine"] = r => r.Maschine,
            ["sage-status"] = r => r.SageStatus,
```

- [ ] **Step 5: View FaWorklist/Index.cshtml**

(a) columnCount (Z. 16-17) ersetzen:

```csharp
    // FA-Nr, Werkbank, Artikel, Bez1, Bez2, Stk, Beschicht., BG, Komm, Fert,
    // Kaeltemittel, Ventil, Ausfuehrung, Maschine, Sage-Status + Merkmale + 1 Erledigt-Spalte
    var columnCount = 15 + Model.AttributeColumns.Count + 1;
```

(b) Im `<thead>` nach `<th class="text-nowrap" data-filterable data-col-key="production-date" data-date-filter>Fert.-Termin</th>` (Z. 92) einfuegen:

```html
                    <th data-filterable data-col-key="kaeltemittel">Kältemittel</th>
                    <th data-filterable data-col-key="ventil">Ventil</th>
                    <th data-filterable data-col-key="ausfuehrung">Ausführung E/Z</th>
                    <th data-filterable data-col-key="maschine">Maschine</th>
                    <th data-filterable data-col-key="sage-status">Sage-Status</th>
```

(c) Im `<tbody>` nach `<td class="text-nowrap">@FormatDateWithKw(item.ProductionDate)</td>` (Z. 135) einfuegen:

```html
                            <td>@item.Kaeltemittel</td>
                            <td>@item.Ventil</td>
                            <td>@item.AusfuehrungEZ</td>
                            <td>@item.Maschine</td>
                            <td>@item.SageStatus</td>
```

(d) Im `#column-config`-JSON (Z. 162-180) nach dem `production-date`-Eintrag einfuegen (Default SICHTBAR — daher KEIN defaultHidden):

```json
        { "key": "kaeltemittel", "label": "Kältemittel", "locked": false, "defaultWidth": null },
        { "key": "ventil", "label": "Ventil", "locked": false, "defaultWidth": null },
        { "key": "ausfuehrung", "label": "Ausführung E/Z", "locked": false, "defaultWidth": null },
        { "key": "maschine", "label": "Maschine", "locked": false, "defaultWidth": null },
        { "key": "sage-status", "label": "Sage-Status", "locked": false, "defaultWidth": null },
```

- [ ] **Step 6: ViewConfig „FaWorklist" registrieren (Prefs-Bug-Fix)**

In `IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs` nach der `FaCompletion`-ViewConfig (Z. 106) einfuegen:

```csharp
    /// <summary>
    /// FaWorklist/Index.cshtml columns — FA-Abarbeitungsliste (v1.22.0).
    /// Registrierung seit v1.26.0: vorher kannte GetByViewKey den Key nicht,
    /// die Prefs-API antwortete 400 und Zahnrad-Einstellungen gingen bei jedem
    /// Reload verloren. Dynamische Merkmal-Spalten ("attr-{id}") sind bewusst
    /// NICHT enthalten (die API-Validierung prueft nur den ViewKey).
    /// </summary>
    public static readonly ViewConfig FaWorklist = new(
        "FaWorklist", "FA-Abarbeitungsliste",
        SupportsReorder: true, SupportsSortDefault: true)
    {
        Columns =
        [
            new ColumnDef("order-number",   "FA Nr.",           Locked: true),
            new ColumnDef("workbench",      "Werkbank",         Locked: false),
            new ColumnDef("article-number", "Artikelnummer",    Locked: false),
            new ColumnDef("description1",   "Bezeichnung 1",    Locked: false),
            new ColumnDef("description2",   "Bezeichnung 2",    Locked: false),
            new ColumnDef("quantity",       "Stk.",             Locked: false),
            new ColumnDef("coating-date",   "Beschicht.",       Locked: false),
            new ColumnDef("bg-date",        "BG-Termin",        Locked: false),
            new ColumnDef("picking-date",   "Komm.",            Locked: false),
            new ColumnDef("production-date","Fert.-Termin",     Locked: false),
            new ColumnDef("kaeltemittel",   "Kaeltemittel",     Locked: false),
            new ColumnDef("ventil",         "Ventil",           Locked: false),
            new ColumnDef("ausfuehrung",    "Ausfuehrung E/Z",  Locked: false),
            new ColumnDef("maschine",       "Maschine",         Locked: false),
            new ColumnDef("sage-status",    "Sage-Status",      Locked: false),
            new ColumnDef("done",           "Erledigt",         Locked: true),
        ]
    };
```

Und in `GetByViewKey` (Z. 233-244) nach `"FaCompletion"     => FaCompletion,` ergaenzen:

```csharp
        "FaWorklist"       => FaWorklist,
```

- [ ] **Step 7: Tests laufen lassen (GREEN)**

Run: `dotnet test IdealAkeWms.Tests --nologo --filter "FullyQualifiedName~FaWorklistControllerTests|FullyQualifiedName~UserViewPreferencesApiControllerTests"`
Expected: PASS — alle, inkl. 3 neuer Tests.

- [ ] **Step 8: Commit**

```powershell
git add IdealAkeWms/Models/ViewModels/FaWorklistViewModel.cs IdealAkeWms/Controllers/FaWorklistController.cs IdealAkeWms/Views/FaWorklist/Index.cshtml IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs IdealAkeWms.Tests/Controllers/FaWorklistControllerTests.cs IdealAkeWms.Tests/Controllers/UserViewPreferencesApiControllerTests.cs
git commit -m @'
feat(fa-zusatzinfo): FA-Abarbeitungsliste — 5 Zusatzinfo-Spalten (default sichtbar)

FaWorklistRow +5 Felder, Mapping aus ExtraInfo-Include, BuildColumnMap +5
server-seitige Filter-Getter, View th/td/columnCount(15)/column-config.
Prefs-Bug mitgefixt: ViewConfig "FaWorklist" in GetByViewKey registriert —
vorher 400 der Prefs-API, Zahnrad-Einstellungen gingen bei Reload verloren.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
'@
```

---

### Task 9: FA-Liste + Leitstand — 5 Spalten Default AUSGEBLENDET (`DefaultHidden`-Mechanik)

**Files:**
- Modify: `IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs:3` (Record) + ProductionOrders-/PickingLeitstand-Configs
- Modify: `IdealAkeWms/wwwroot/js/column-preferences.js:55-63` (buildDefaultSettings) + `:102-110` (mergeWithDefaults)
- Modify: `IdealAkeWms/Models/ViewModels/ProductionOrderListViewModel.cs` (+5) und `PickingLeitstandViewModel.cs` (+5)
- Modify: `IdealAkeWms/Controllers/ProductionOrdersController.cs:101-121` + `PickingLeitstandController.cs:96-126` (Mapping)
- Modify: `IdealAkeWms/Views/ProductionOrders/Index.cshtml:80,146,219,243` + `IdealAkeWms/Views/PickingLeitstand/Index.cshtml:112,190,385,520`
- Test: `IdealAkeWms.Tests/Controllers/ProductionOrdersControllerSlimTests.cs`, `IdealAkeWms.Tests/Controllers/PickingLeitstandControllerTests.cs`

- [ ] **Step 1: Controller-Tests schreiben (RED)**

(a) Ans Ende von `IdealAkeWms.Tests/Controllers/ProductionOrdersControllerSlimTests.cs` (vor der schliessenden Klassen-Klammer) einfuegen:

```csharp
    // ---------------------------------------------------- FA-Zusatzinfos (Sage, v1.26.0)

    [Fact]
    public async Task Index_MapsExtraInfoFields_FromLeitstandRow()
    {
        var row = new LeitstandOrderRow(
            1, "FA-100", 1m, null, "ART-001", null, null, null, null,
            false, false, false, null,
            Kaeltemittel: "R290", Ventil: "Danfoss", AusfuehrungEZ: "E",
            Maschine: "M1", SageStatus: "verpackt");
        _orderRepo.Setup(r => r.GetForLeitstandAsync(
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IReadOnlyDictionary<string, string>?>()))
            .ReturnsAsync(MakePage(row));
        _pickingStatusRepo.Setup(r => r.GetByProductionOrderIdsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new Dictionary<int, ProductionOrderPickingStatus>());

        var result = await _controller.Index(null, null, null) as ViewResult;

        var vm = result!.Model.Should().BeOfType<ProductionOrderListViewModel>().Subject;
        var item = vm.Items.Should().ContainSingle().Subject;
        item.Kaeltemittel.Should().Be("R290");
        item.Ventil.Should().Be("Danfoss");
        item.AusfuehrungEZ.Should().Be("E");
        item.Maschine.Should().Be("M1");
        item.SageStatus.Should().Be("verpackt");
    }

    [Fact]
    public async Task Index_ZusatzinfoColumnFilter_IsPassedToSqlFilters()
    {
        // Invariante: die 5 neuen Keys sind KEINE Datums-Keys -> sie MUESSEN in den
        // SQL-Filter-Pfad (GetForLeitstandAsync columnFilters) laufen, nicht in den
        // C#-Memory-Filter (kein Force-Full-Load, Spec §5.3).
        IReadOnlyDictionary<string, string>? captured = null;
        _orderRepo.Setup(r => r.GetForLeitstandAsync(
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IReadOnlyDictionary<string, string>?>()))
            .Callback<string?, string?, string?, bool, int, int, IReadOnlyDictionary<string, string>?>(
                (_, _, _, _, _, _, colf) => captured = colf)
            .ReturnsAsync(MakePage(MakeRow(1, "FA-100")));
        _pickingStatusRepo.Setup(r => r.GetByProductionOrderIdsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new Dictionary<int, ProductionOrderPickingStatus>());

        var httpCtx = new DefaultHttpContext();
        httpCtx.Request.QueryString = new QueryString("?colf_kaeltemittel=r290&colf_sage-status=verpackt");
        _controller.ControllerContext = new ControllerContext { HttpContext = httpCtx };

        await _controller.Index(null, null, null);

        captured.Should().NotBeNull();
        captured!.Should().ContainKey("kaeltemittel").WhoseValue.Should().Be("r290");
        captured!.Should().ContainKey("sage-status").WhoseValue.Should().Be("verpackt");
    }
```

(b) Ans Ende von `IdealAkeWms.Tests/Controllers/PickingLeitstandControllerTests.cs` (vor der schliessenden Klassen-Klammer) einfuegen:

```csharp
    // ---------------------------------------------------- FA-Zusatzinfos (Sage, v1.26.0)

    [Fact]
    public async Task Index_MapsExtraInfoFields_FromLeitstandRow()
    {
        var row = new LeitstandOrderRow(
            1, "FA-100", 1m, null, "ART-001", null, null, null, null,
            false, false, false, null,
            Kaeltemittel: "R290", Ventil: "Danfoss", AusfuehrungEZ: "Z",
            Maschine: "M2", SageStatus: "abgeholt");
        _orderRepo.Setup(r => r.GetForLeitstandAsync(
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IReadOnlyDictionary<string, string>?>()))
            .ReturnsAsync(MakePage(row));
        _pickingStatusRepo.Setup(r => r.GetByProductionOrderIdsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new Dictionary<int, ProductionOrderPickingStatus>());
        _faWorkStepRepo.Setup(r => r.GetWorkStepDetailPivotAsync(It.IsAny<List<int>>()))
            .ReturnsAsync(new Dictionary<int, Dictionary<string, FaWorkStepPivotCell>>());

        var result = await _controller.Index(null, null, null) as ViewResult;

        var vm = result!.Model.Should().BeOfType<PickingLeitstandViewModel>().Subject;
        var item = vm.Items.Should().ContainSingle().Subject;
        item.Kaeltemittel.Should().Be("R290");
        item.Ventil.Should().Be("Danfoss");
        item.AusfuehrungEZ.Should().Be("Z");
        item.Maschine.Should().Be("M2");
        item.SageStatus.Should().Be("abgeholt");
    }

    [Fact]
    public async Task Index_ZusatzinfoColumnFilter_IsPassedToSqlFilters_NotMemoryFiltered()
    {
        // Invariante: die 5 neuen Keys duerfen NICHT in LeitstandDateColumnKeys /
        // LeitstandWorkStepColumnKeys landen — sie laufen als SQL-Filter durch
        // (SQL-Pagination bleibt erhalten, Spec §5.3).
        IReadOnlyDictionary<string, string>? captured = null;
        _orderRepo.Setup(r => r.GetForLeitstandAsync(
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IReadOnlyDictionary<string, string>?>()))
            .Callback<string?, string?, string?, bool, int, int, IReadOnlyDictionary<string, string>?>(
                (_, _, _, _, _, _, colf) => captured = colf)
            .ReturnsAsync(MakePage(MakeRow(1, "FA-100")));
        _pickingStatusRepo.Setup(r => r.GetByProductionOrderIdsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new Dictionary<int, ProductionOrderPickingStatus>());
        _faWorkStepRepo.Setup(r => r.GetWorkStepDetailPivotAsync(It.IsAny<List<int>>()))
            .ReturnsAsync(new Dictionary<int, Dictionary<string, FaWorkStepPivotCell>>());

        var httpCtx = new DefaultHttpContext();
        httpCtx.Request.QueryString = new QueryString("?colf_maschine=m1");
        _controller.ControllerContext = new ControllerContext { HttpContext = httpCtx };

        await _controller.Index(null, null, null);

        captured.Should().NotBeNull();
        captured!.Should().ContainKey("maschine").WhoseValue.Should().Be("m1");
    }
```

- [ ] **Step 2: Tests laufen lassen (RED)**

Run: `dotnet test IdealAkeWms.Tests --nologo --filter "FullyQualifiedName~ProductionOrdersControllerSlimTests|FullyQualifiedName~PickingLeitstandControllerTests"`
Expected: FAIL — Compile-Errors (`item.Kaeltemittel` existiert auf beiden ListItems nicht). Hinweis: die Pass-Through-Tests sind Invarianten-Tests (waeren allein auch vorher gruen) — ihr Wert ist der Regressionsschutz gegen versehentliches Memory-Filtern.

- [ ] **Step 3: ViewModels erweitern**

(a) In `IdealAkeWms/Models/ViewModels/ProductionOrderListViewModel.cs` in der Klasse `ProductionOrderListItem` nach `WorkplaceName` (Z. 37) ergaenzen:

```csharp
    // FA-Zusatzinfos aus Sage (v1.26.0) — read-only, Default-ausgeblendete Spalten.
    public string? Kaeltemittel { get; set; }
    public string? Ventil { get; set; }
    public string? AusfuehrungEZ { get; set; }
    public string? Maschine { get; set; }
    public string? SageStatus { get; set; }
```

(b) In `IdealAkeWms/Models/ViewModels/PickingLeitstandViewModel.cs` in der Klasse `PickingLeitstandItem` nach `WorkplaceName` (Z. 36) denselben Block ergaenzen:

```csharp
    // FA-Zusatzinfos aus Sage (v1.26.0) — read-only, Default-ausgeblendete Spalten.
    public string? Kaeltemittel { get; set; }
    public string? Ventil { get; set; }
    public string? AusfuehrungEZ { get; set; }
    public string? Maschine { get; set; }
    public string? SageStatus { get; set; }
```

- [ ] **Step 4: Controller-Mappings**

(a) In `IdealAkeWms/Controllers/ProductionOrdersController.cs` im Item-Initializer (Z. 105-121) nach `WorkplaceName = o.WorkplaceName,` ergaenzen:

```csharp
                Kaeltemittel = o.Kaeltemittel,
                Ventil = o.Ventil,
                AusfuehrungEZ = o.AusfuehrungEZ,
                Maschine = o.Maschine,
                SageStatus = o.SageStatus,
```

(b) In `IdealAkeWms/Controllers/PickingLeitstandController.cs` im Item-Initializer (Z. 101-126) nach `WorkplaceName = o.WorkplaceName,` denselben Block ergaenzen:

```csharp
                Kaeltemittel = o.Kaeltemittel,
                Ventil = o.Ventil,
                AusfuehrungEZ = o.AusfuehrungEZ,
                Maschine = o.Maschine,
                SageStatus = o.SageStatus,
```

WICHTIG: `FaListDateColumnKeys` (ProductionOrdersController) und `LeitstandDateColumnKeys`/`LeitstandWorkStepColumnKeys` (PickingLeitstandController) NICHT anfassen — die 5 Keys muessen als SQL-Filter durchlaufen.

- [ ] **Step 5: ColumnDef um `DefaultHidden` erweitern + Configs**

(a) In `IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs` Z. 3 ersetzen (optionaler Parameter am Ende — ALLE bestehenden Aufrufer nutzen positional/named Args bis `DefaultWidth` und kompilieren unveraendert; einziger Konstruktions-Ort ist diese Datei, per Grep verifiziert):

```csharp
public record ColumnDef(string Key, string Label, bool Locked = false, int? DefaultWidth = null, bool DefaultHidden = false);
```

(b) In der `ProductionOrders`-ViewConfig nach `new ColumnDef("workbench", ...)` einfuegen:

```csharp
            // FA-Zusatzinfos aus Sage (v1.26.0) — Default AUSGEBLENDET (Zahnrad blendet ein)
            new ColumnDef("kaeltemittel","Kaeltemittel",    Locked: false, DefaultHidden: true),
            new ColumnDef("ventil",      "Ventil",          Locked: false, DefaultHidden: true),
            new ColumnDef("ausfuehrung", "Ausfuehrung E/Z", Locked: false, DefaultHidden: true),
            new ColumnDef("maschine",    "Maschine",        Locked: false, DefaultHidden: true),
            new ColumnDef("sage-status", "Sage-Status",     Locked: false, DefaultHidden: true),
```

(c) In der `PickingLeitstand`-ViewConfig nach `new ColumnDef("workbench", ...)` denselben 5-Zeilen-Block einfuegen:

```csharp
            // FA-Zusatzinfos aus Sage (v1.26.0) — Default AUSGEBLENDET (Zahnrad blendet ein)
            new ColumnDef("kaeltemittel","Kaeltemittel",    Locked: false, DefaultHidden: true),
            new ColumnDef("ventil",      "Ventil",          Locked: false, DefaultHidden: true),
            new ColumnDef("ausfuehrung", "Ausfuehrung E/Z", Locked: false, DefaultHidden: true),
            new ColumnDef("maschine",    "Maschine",        Locked: false, DefaultHidden: true),
            new ColumnDef("sage-status", "Sage-Status",     Locked: false, DefaultHidden: true),
```

- [ ] **Step 6: column-preferences.js — defaultHidden-Fallback (BEIDE Stellen)**

In `IdealAkeWms/wwwroot/js/column-preferences.js`:

(a) `buildDefaultSettings()` (Z. 55-63) ersetzen:

```javascript
    function buildDefaultSettings() {
        return {
            columns: _columnConfig.map(function (c, i) {
                // defaultHidden (v1.26.0): neue Spalten koennen per Config default-unsichtbar sein
                return { key: c.key, visible: !c.defaultHidden, width: c.defaultWidth || null, order: i };
            }),
            defaultSortColumn: null,
            defaultSortDirection: 'asc'
        };
    }
```

(b) In `mergeWithDefaults()` (Z. 102-110) den `defaults.columns`-Mapper ersetzen — gespeicherte Einstellungen gewinnen weiterhin JE SPALTE (wer je explizit ein-/ausgeblendet hat, behaelt das); nur der Fallback fuer neue/unbekannte Spalten nutzt `!c.defaultHidden` statt hart `true` (sonst wuerden neue Spalten bei JEDEM User mit gespeicherten Prefs sichtbar — Gegenteil des Ziels):

```javascript
        defaults.columns = _columnConfig.map(function (c) {
            var s = savedMap[c.key];
            return {
                key: c.key,
                visible: s && s.visible !== undefined ? s.visible : !c.defaultHidden,
                width: s && s.width !== undefined ? s.width : (c.defaultWidth || null),
                order: resultKeys.indexOf(c.key)
            };
        });
```

- [ ] **Step 7: View ProductionOrders/Index.cshtml**

(a) Im `<thead>` nach `<th data-filterable data-col-key="workbench">Werkbank</th>` (Z. 80) einfuegen:

```html
                <th data-filterable data-col-key="kaeltemittel">Kältemittel</th>
                <th data-filterable data-col-key="ventil">Ventil</th>
                <th data-filterable data-col-key="ausfuehrung">Ausführung E/Z</th>
                <th data-filterable data-col-key="maschine">Maschine</th>
                <th data-filterable data-col-key="sage-status">Sage-Status</th>
```

(b) Im `<tbody>` nach `<td>@item.WorkplaceName</td>` (Z. 146, VOR dem `@{ var lackName = ... }`-Block) einfuegen:

```html
                    <td>@item.Kaeltemittel</td>
                    <td>@item.Ventil</td>
                    <td>@item.AusfuehrungEZ</td>
                    <td>@item.Maschine</td>
                    <td>@item.SageStatus</td>
```

(c) Empty-Row-colspan (Z. 219): `var colCount = 14;` ersetzen durch:

```csharp
                        var colCount = 19;
```

(d) Im `#column-config`-JSON nach dem `workbench`-Eintrag (Z. 243) einfuegen:

```json
    { "key": "kaeltemittel", "label": "Kältemittel", "locked": false, "defaultWidth": null, "defaultHidden": true },
    { "key": "ventil", "label": "Ventil", "locked": false, "defaultWidth": null, "defaultHidden": true },
    { "key": "ausfuehrung", "label": "Ausführung E/Z", "locked": false, "defaultWidth": null, "defaultHidden": true },
    { "key": "maschine", "label": "Maschine", "locked": false, "defaultWidth": null, "defaultHidden": true },
    { "key": "sage-status", "label": "Sage-Status", "locked": false, "defaultWidth": null, "defaultHidden": true },
```

- [ ] **Step 8: View PickingLeitstand/Index.cshtml**

(a) Im `<thead>` nach `<th data-filterable data-col-key="workbench">Werkbank</th>` (Z. 112) denselben 5-th-Block wie in Step 7(a) einfuegen:

```html
                <th data-filterable data-col-key="kaeltemittel">Kältemittel</th>
                <th data-filterable data-col-key="ventil">Ventil</th>
                <th data-filterable data-col-key="ausfuehrung">Ausführung E/Z</th>
                <th data-filterable data-col-key="maschine">Maschine</th>
                <th data-filterable data-col-key="sage-status">Sage-Status</th>
```

(b) Im `<tbody>` nach `<td>@item.WorkplaceName</td>` (Z. 190) einfuegen:

```html
                    <td>@item.Kaeltemittel</td>
                    <td>@item.Ventil</td>
                    <td>@item.AusfuehrungEZ</td>
                    <td>@item.Maschine</td>
                    <td>@item.SageStatus</td>
```

(c) Empty-Row-colspan (Z. 385): `var colCount = 22;` ersetzen durch:

```csharp
                        var colCount = 27;
```

(d) Im `#column-config`-JSON nach dem `workbench`-Eintrag (Z. 520) einfuegen:

```json
    { "key": "kaeltemittel", "label": "Kältemittel", "locked": false, "defaultWidth": null, "defaultHidden": true },
    { "key": "ventil", "label": "Ventil", "locked": false, "defaultWidth": null, "defaultHidden": true },
    { "key": "ausfuehrung", "label": "Ausführung E/Z", "locked": false, "defaultWidth": null, "defaultHidden": true },
    { "key": "maschine", "label": "Maschine", "locked": false, "defaultWidth": null, "defaultHidden": true },
    { "key": "sage-status", "label": "Sage-Status", "locked": false, "defaultWidth": null, "defaultHidden": true },
```

- [ ] **Step 9: Tests laufen lassen (GREEN) + Voll-Build**

Run: `dotnet test IdealAkeWms.Tests --nologo --filter "FullyQualifiedName~ProductionOrdersControllerSlimTests|FullyQualifiedName~PickingLeitstandControllerTests"`
Expected: PASS — alle, inkl. 4 neuer Tests.

Run: `dotnet build IdealAkeWms.slnx --nologo -clp:ErrorsOnly; dotnet test IdealAkeWms.Tests --nologo`
Expected: 0 Errors, alle Web-Tests gruen.

- [ ] **Step 10: Commit**

```powershell
git add IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs IdealAkeWms/wwwroot/js/column-preferences.js IdealAkeWms/Models/ViewModels/ProductionOrderListViewModel.cs IdealAkeWms/Models/ViewModels/PickingLeitstandViewModel.cs IdealAkeWms/Controllers/ProductionOrdersController.cs IdealAkeWms/Controllers/PickingLeitstandController.cs IdealAkeWms/Views/ProductionOrders/Index.cshtml IdealAkeWms/Views/PickingLeitstand/Index.cshtml IdealAkeWms.Tests/Controllers/ProductionOrdersControllerSlimTests.cs IdealAkeWms.Tests/Controllers/PickingLeitstandControllerTests.cs
git commit -m @'
feat(fa-zusatzinfo): FA-Liste + Leitstand — 5 Zusatzinfo-Spalten default ausgeblendet

Neue defaultHidden-Mechanik: ColumnDef.DefaultHidden + defaultHidden im inline
JSON; column-preferences.js faellt in buildDefaultSettings UND mergeWithDefaults
auf visible: !c.defaultHidden zurueck (gespeicherte Prefs gewinnen je Spalte).
Mapping aus der LeitstandOrderRow-Projektion; Filter-Keys laufen als SQL-Filter
(kein Force-Full-Load). colspan 14->19 bzw. 22->27.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
'@
```

---

### Task 10: Version v1.26.0 + Changelog + Hilfe + Doku (inkl. secondbrain-Spiegelung)

**Files:**
- Modify: `IdealAkeWms/AppVersion.cs` + `IDEALAKEWMSService/AppVersion.cs`
- Modify: `IdealAkeWms/Views/Help/Changelog.cshtml` (neue Card VOR der v1.25.0-Card, Z. 10)
- Modify: `IdealAkeWms/Views/Help/Index.cshtml` (4 Stellen: Z. 313-351, 355-408, ~vor 313, 1154-1158)
- Modify: `CLAUDE.md` (Service-Konfig-Tabelle + Fallstricke)
- Modify: `README.md` (~Z. 622-628 SyncWorker-Bullets, ~Z. 669 Sync-Gates-Beispiele)
- Modify: `PROJECT_STATUS.md` (Kopf)
- Modify: `docs/TESTSZENARIEN.md` (Kopf-Stand, Index-Tabelle, neues Kapitel 55, Ende-Zeile) + Kopie `secondbrain/docs/TESTSZENARIEN.md`

- [ ] **Step 1: Version hochziehen**

`IdealAkeWms/AppVersion.cs` UND `IDEALAKEWMSService/AppVersion.cs` — in BEIDEN Dateien:

```csharp
    public const string Version = "1.26.0";
    public const string Date = "2026-07-22";
```

- [ ] **Step 2: Changelog-Card**

In `IdealAkeWms/Views/Help/Changelog.cshtml` DIREKT NACH `<div class="col-lg-8">` (Z. 8) und VOR der v1.25.0-Card einfuegen:

```html
        <div class="card mb-3">
            <div class="card-header text-white" style="background-color: var(--ake-primary);">
                <strong>v1.26.0</strong> <span class="text-white-50 ms-2">22.07.2026</span>
            </div>
            <div class="card-body">
                <h6>FA-Zusatzinfos (Sage)</h6>
                <ul>
                    <li><strong>Neue FA-Zusatzinfos aus Sage:</strong> Kaeltemittel, Ventil,
                        Ausfuehrung E/Z, Maschine und Sage-Status werden je Fertigungsauftrag
                        per Service-Sync aus Sage uebernommen. Die Daten sind in der App
                        <strong>read-only</strong> &mdash; Sage ist Master; verschwindet ein
                        Auftrag aus der Sage-Sicht, bleibt der letzte Stand sichtbar.</li>
                    <li><strong>FA-Vervollstaendigung:</strong> neuer erster Reiter
                        <strong>ALLGEMEIN</strong> zeigt die Zusatzinfos. Standardmaessig
                        oeffnet weiterhin der erste FA-Vorbau-AG-Reiter; ein FA ohne
                        FA-Vorbau-AG oeffnet auf ALLGEMEIN. Der Code
                        <code>ALLGEMEIN</code> ist fuer FA-Vorbau-AG jetzt reserviert.</li>
                    <li><strong>FA-Abarbeitungsliste:</strong> fuenf neue filterbare Spalten
                        (standardmaessig eingeblendet, per Zahnrad ausblendbar). Zusaetzlich
                        merkt sich die Liste die Zahnrad-Einstellungen jetzt dauerhaft
                        (vorher gingen sie beim Neuladen verloren).</li>
                    <li><strong>FA-Liste + Leitstand:</strong> dieselben fuenf Spalten,
                        standardmaessig <strong>ausgeblendet</strong> &mdash; bei Bedarf ueber
                        das Zahnrad (Tabellenansicht anpassen) einblenden.</li>
                    <li><strong>Hinweis fuer Admins:</strong> Der Sync ist Opt-in &mdash; in den
                        Service-Einstellungen <code>Sync:FaZusatzinfoEnabled</code> aktivieren
                        (Default aus). Fehlt die Sage-View am System, ueberspringt der Sync mit
                        Warnhinweis (keine Fehlermail). Neuer Aktivitaets-Protokoll-Filter
                        <code>FaZusatzinfo</code>.</li>
                </ul>
            </div>
        </div>
```

- [ ] **Step 3: Hilfeseite — 4 Stellen**

In `IdealAkeWms/Views/Help/Index.cshtml`:

(a) **FA-Liste + Leitstand** (Fertigungsauftraege-Card): VOR `<dt>FA-Vervollstaendigung (seit v1.13.0, umgebaut in v1.22.0)</dt>` (Z. 313) einfuegen:

```html
                    <dt>FA-Zusatzinfos aus Sage in FA-Liste + Leitstand (seit v1.26.0)</dt>
                    <dd>Die FA-Liste und der Kommissionier-Leitstand kennen fuenf zusaetzliche
                        Spalten <strong>Kaeltemittel, Ventil, Ausfuehrung E/Z, Maschine,
                        Sage-Status</strong> (read-only aus Sage). Sie sind dort standardmaessig
                        <strong>ausgeblendet</strong> und werden bei Bedarf ueber das Zahnrad
                        (Tabellenansicht anpassen) eingeblendet. Befuellt werden sie vom
                        Windows-Dienst &mdash; Service-Einstellung
                        <code>Sync:FaZusatzinfoEnabled</code> (Default aus) aktivieren.
                        Hinweis: bei ausgeblendeter Spalte ist auch deren Filterzelle versteckt
                        &mdash; ein per URL geteilter Spaltenfilter filtert dann unsichtbar.</dd>
```

(b) **FA-Vervollstaendigung** (dieselbe Card): in der `<ul class="mt-1 mb-1">` (Z. 318) als ERSTES `<li>` einfuegen:

```html
                            <li><strong>Reiter ALLGEMEIN (seit v1.26.0):</strong> Erster Reiter
                                mit den FA-Zusatzinfos aus Sage (Kaeltemittel, Ventil,
                                Ausfuehrung E/Z, Maschine, Sage-Status) &mdash; read-only,
                                Sage ist Master. Ohne Sage-Daten erscheint der Hinweis
                                &bdquo;Noch keine Zusatzinformationen aus Sage vorhanden.&ldquo;
                                Standardmaessig oeffnet weiterhin der erste FA-Vorbau-AG-Reiter;
                                ein FA ganz ohne FA-Vorbau-AG oeffnet auf ALLGEMEIN. Der Code
                                <code>ALLGEMEIN</code> ist fuer FA-Vorbau-AG reserviert.</li>
```

(c) **FA-Abarbeitungsliste-Card**: NACH dem `<dd>`-Ende von „Werkbank-Spalte + Merkmal-Spalten + Termine" (Z. 392) einfuegen:

```html
                    <dt>FA-Zusatzinfos aus Sage (seit v1.26.0)</dt>
                    <dd>Fuenf zusaetzliche filterbare Spalten <strong>Kaeltemittel, Ventil,
                        Ausfuehrung E/Z, Maschine, Sage-Status</strong> &mdash; standardmaessig
                        eingeblendet, ueber das Zahnrad ausblendbar (die Zahnrad-Einstellungen
                        der Liste werden seit v1.26.0 dauerhaft gespeichert). Die Werte kommen
                        per Service-Sync aus Sage (read-only; leer, solange Sage nichts
                        liefert).</dd>
```

(d) **Aktivitaets-Protokoll-Filterliste** (Z. 1154-1158): die Aufzaehlung ersetzen durch:

```html
                <p>
                    Aktivitaet-Filter im Dropdown: Lagerplatz, Lagerbestand, BomCache, OseonTracking,
                    OseonWorkplaces, OseonArticleCategories, EnaioDms, Holiday, CoatingDetection,
                    FaWorkStepDetection, ProductionOrder, ProductionOrderReconciliation, Article,
                    FaZusatzinfo, PartRequisitionEmail, WarehouseRequisitionEmail, BdeAutoPause,
                    CleanupAktivitaetsprotokoll.
                </p>
```

- [ ] **Step 4: CLAUDE.md**

(a) In der Tabelle „Service-Konfiguration" NACH der Zeile `| Sync:ReconcileMaxCancelPerRun | ... |` einfuegen:

```markdown
| `Sync:FaZusatzinfoEnabled` | `false` | FA-Zusatzinfos (Sage): Kaeltemittel/Ventil/Ausfuehrung/Maschine/Status je FA synchronisieren (v1.26.0) |
```

(b) Am Ende des Abschnitts „Bekannte Fallstricke" (nach dem Cleanup-Jobs-Eintrag) einfuegen:

```markdown
- **FA-Zusatzinfos (Sage) — read-only Satellit `ProductionOrderExtraInfo` (v1.26.0)**: 1:1-Satellit (UNIQUE-FK `UQ_ProductionOrderExtraInfo_ProductionOrderId`, Cascade, Migration 81, rein additiv) mit 5 nvarchar(200)-Feldern (`Kaeltemittel`/`Ventil`/`AusfuehrungEZ`/`Maschine`/`SageStatus`), befuellt AUSSCHLIESSLICH vom `FaZusatzinfoSyncService` (SyncWorker-Block direkt NACH dem FA-Import, Gate `Sync:FaZusatzinfoEnabled` Default false, Protokoll-Name `FaZusatzinfo`, Counts `gelesen/neu/aktualisiert/uebersprungen`). **Kein Loeschen/Reconcile:** verschwindet ein WA aus der View, bleibt der letzte Stand stehen. **View-Guard:** fehlt `dbo.vw_IDEAL_AKE_WMS_FAZusatzinformationen`, endet der Lauf regulaer mit Warn-Zeile (kein throw, keine Fehlermail). **Kein AgentJob-Eager-Create** (anders als PickingStatus/BdeStatus). **Mehrfach-treffer-faehig** (IDEAL-Linie: mehrere ProductionOrders je WA-Nummer → Upsert je Id; auf der AKE-Linie via UNIQUE-Index immer genau einer). **Anzeige:** FaCompletion-Pseudo-Tab „ALLGEMEIN" (NICHT in Model.Tabs, OrdinalIgnoreCase, Default bleibt erster AG; Code `ALLGEMEIN` im WorkSteps-CRUD reserviert), FaWorklist (5 Spalten default sichtbar; ViewKey „FaWorklist" ist seit v1.26.0 in `ColumnDefinitions.GetByViewKey` registriert — vorher 400 der Prefs-API), FA-Liste + Leitstand (default versteckt). **FA-Liste/Leitstand-Datenpfad:** die Felder laufen ueber die `LeitstandOrderRow`-Projektion (`.Include` waere dort wirkungslos) und die 5 Filter-Keys (`kaeltemittel`/`ventil`/`ausfuehrung`/`maschine`/`sage-status`) ueber den SQL-Switch `ApplyLeitstandColumnFilter` (Contains + `ToLower()`, Null-Guards, KEIN `EF.Functions.Like` — InMemory-testbar; SQL-Pagination bleibt erhalten). **defaultHidden-Mechanik:** `ColumnDef.DefaultHidden` + `defaultHidden` im inline `#column-config`-JSON; `column-preferences.js` faellt in `buildDefaultSettings()` UND `mergeWithDefaults()` auf `visible: !c.defaultHidden` zurueck — gespeicherte Prefs gewinnen weiterhin je Spalte (haert hart `true` waere der Bug: neue Spalten wuerden bei jedem User mit gespeicherten Prefs sichtbar).
```

- [ ] **Step 5: README.md**

(a) In der SyncWorker-Bullet-Liste (~Z. 624) NACH dem Produktionsauftraege-Bullet einfuegen:

```markdown
- **FA-Zusatzinfos (Sage)**: Kältemittel/Ventil/Ausführung E-Z/Maschine/Status je FA aus `vw_IDEAL_AKE_WMS_FAZusatzinformationen` → `ProductionOrderExtraInfo` (read-only 1:1-Satellit, kein Löschen; `Sync:FaZusatzinfoEnabled`, v1.26.0)
```

(b) In der Tabelle „Wichtige Gruppen" (~Z. 669) die Zeile `| Sync-Gates | ... |` ersetzen durch:

```markdown
| Sync-Gates | `Sync:ProductionOrdersEnabled`, `Sync:ArticlesEnabled`, `Sync:FaZusatzinfoEnabled`, `Sync:OseonTrackingEnabled`, `Sync:EnaioDmsEnabled`, `Sync:BomCacheEnabled`, `Sync:LagerplaetzeEnabled`, `Sync:LagerbestandEnabled`, `Sync:FeiertagSyncEnabled` |
```

- [ ] **Step 6: PROJECT_STATUS.md**

Im Abschnitt „Aktueller Fortschritt (laufend)" NACH der Stand-Beschreibung (vor „### Wo wir aufgehoert haben") einfuegen:

```markdown
**In Arbeit (Worktree `.claude/worktrees/pa-zusatzinfos`, Branch `feature/pa-zusatzinfos`): v1.26.0
„FA-Zusatzinfos (Sage)"** — neuer read-only Satellit `ProductionOrderExtraInfo` (Migration 81,
additiv) + `FaZusatzinfoSyncService` (Gate `Sync:FaZusatzinfoEnabled`, Default aus, View-Guard)
+ Anzeige in FA-Vervollstaendigung (Reiter ALLGEMEIN), FA-Abarbeitungsliste (5 Spalten default
sichtbar, inkl. Prefs-Bug-Fix ViewKey „FaWorklist"), FA-Liste + Leitstand (5 Spalten default
versteckt via neuer `defaultHidden`-Mechanik). Spec:
`docs/superpowers/specs/2026-07-22-pa-zusatzinfos-design.md`, Plan:
`docs/superpowers/plans/2026-07-22-pa-zusatzinfos.md`. Deploy-Reihenfolge zwingend:
Sage-View verifizieren → Web (Migration+Katalog-Seed) → Service publishen → DryRun → scharf.
```

- [ ] **Step 7: TESTSZENARIEN — Kapitel 55 + Index + Stand**

In `docs/TESTSZENARIEN.md`:

(a) Kopf (Z. 3): `**Stand:** 2026-06-18 (v1.23.0)` ersetzen durch:

```markdown
**Stand:** 2026-07-22 (v1.26.0)
```

(b) Index-Tabelle: NACH der Kapitel-54-Zeile einfuegen:

```markdown
| Kapitel 55: FA-Zusatzinfos (Sage) (v1.26.0) | [→](#kapitel-55-fa-zusatzinfos-sage-v1260) | TS-55.1 – TS-55.9 |
```

(c) VOR der Ende-Zeile (`*Ende des Dokuments. ...*`) das neue Kapitel einfuegen:

```markdown
## Kapitel 55: FA-Zusatzinfos (Sage) (v1.26.0)

**Vorbedingungen:**
- Sage-View `dbo.vw_IDEAL_AKE_WMS_FAZusatzinformationen` existiert am Zielsystem und liefert
  Zeilen (`SELECT TOP 1 ...` — Spaltennamen `[WA Nummer]`, `Kaeltemittel`, `Ventil`,
  `[Ausfuehrung E/Z]`, `Maschine`, `Status`).
- Web v1.26.0 deployed (Migration 81 gelaufen, `Sync:FaZusatzinfoEnabled` in `/ServiceSettings`
  sichtbar, Default AUS), Windows-Service v1.26.0 published.
- Rollen: `fa_completion` (Reiter ALLGEMEIN), `vorbau` (Abarbeitungsliste), `picking`/`leitstand`
  (FA-Liste/Leitstand), `admin` (ServiceSettings + Aktivitaets-Protokoll).
- `FaCompletionAktiv = true` (fuer FA-Vervollstaendigung + Abarbeitungsliste).

> **Hinweis Aktivitaets-Protokoll:** Der Sync erscheint als Eintrag `FaZusatzinfo` mit Counts
> `gelesen/neu/aktualisiert/uebersprungen`. „uebersprungen" = WA-Zeilen ohne FA im WMS
> (alte/erledigte WAs — normal, KEINE Warn-Zeile je WA). Erhoehte Skip-Counts nach einem
> FA-Import-Fehler sind KEIN Bug (heilt sich im Folgezyklus).

### TS-55.1 Gate aus → an (Erstlauf)
1. `/ServiceSettings`: `Sync:FaZusatzinfoEnabled` ist AUS. Einen Sync-Zyklus abwarten.
2. **Erwartet:** KEIN `FaZusatzinfo`-Eintrag im Aktivitaets-Protokoll.
3. Gate auf AN stellen, Zyklus abwarten.
4. **Erwartet:** `FaZusatzinfo`-Eintrag „Run erfolgreich beendet — gelesen=..., neu=...,
   aktualisiert=0, uebersprungen=...". In der DB existieren `ProductionOrderExtraInfo`-Zeilen
   fuer FAs, deren WA-Nummer die View liefert (CreatedBy `FaZusatzinfoSync`).

### TS-55.2 DryRun
1. `WorkerSettings:SyncDryRun = true`, Gate AN. Zyklus abwarten.
2. **Erwartet:** `FaZusatzinfo`-Eintrag mit Suffix `[DryRun]` und ECHTEN Would-be-Counts
   (`neu`/`aktualisiert` wie ein Echtlauf sie schreiben wuerde) — aber KEINE neuen/geaenderten
   Zeilen in `ProductionOrderExtraInfo`.
3. DryRun aus → naechster Zyklus schreibt die Zeilen tatsaechlich.

### TS-55.3 View fehlt (Warn-Skip, keine Mail)
1. Auf einem System OHNE die View (bzw. View temporaer umbenennen): Gate AN, Zyklus abwarten.
2. **Erwartet:** `FaZusatzinfo`-Lauf endet REGULAER (kein Fehler-Status) mit Warn-Zeile
   „View ... nicht vorhanden — Sync uebersprungen" und Message-Suffix „View nicht vorhanden".
   KEINE Fehlermail (auch bei aktivierter `ErrorNotification`). Kein Mail-Spam alle 15 min.

### TS-55.4 WA ohne FA
1. Die View liefert eine WA-Nummer, die es im WMS nicht (mehr) gibt.
2. **Erwartet:** Zeile zaehlt als `uebersprungen`; KEINE Warn-Detailzeile je WA.

### TS-55.5 FA ohne Zusatzinfo
1. Einen FA oeffnen, dessen WA-Nummer die View NICHT liefert: FA-Vervollstaendigung →
   Reiter ALLGEMEIN.
2. **Erwartet:** Hinweis „Noch keine Zusatzinformationen aus Sage vorhanden." In
   Abarbeitungsliste/FA-Liste/Leitstand sind die 5 Zellen dieses FA leer.

### TS-55.6 Update-Fall
1. In Sage einen Wert aendern (z. B. Kaeltemittel), Zyklus abwarten.
2. **Erwartet:** Count `aktualisiert=1`; die Zeile traegt `ModifiedAt`/`ModifiedBy =
   FaZusatzinfoSync`; die Views zeigen den neuen Wert. Unveraenderte Zeilen behalten
   `ModifiedAt = NULL` (kein Blind-Update).

### TS-55.7 Gate an → aus (letzter Stand bleibt)
1. Gate AUS stellen, mehrere Zyklen abwarten.
2. **Erwartet:** Kein neuer `FaZusatzinfo`-Eintrag; die vorhandenen Zusatzinfos bleiben in
   allen Views sichtbar (kein Loeschen). Gleiches gilt fuer WAs, die aus der View
   verschwinden: letzter bekannter Stand bleibt stehen.

### TS-55.8 Spalten-Defaults + Prefs
1. Als BESTANDS-User mit bereits GESPEICHERTEN Spalten-Einstellungen (Zahnrad je View
   mindestens einmal benutzt) FA-Liste und Leitstand oeffnen.
2. **Erwartet:** Die 5 neuen Spalten sind dort NICHT sichtbar (defaultHidden greift auch im
   Merge mit gespeicherten Prefs). Ueber das Zahnrad lassen sie sich einblenden; die Wahl
   ueberlebt einen Reload.
3. Die FA-Abarbeitungsliste oeffnen.
4. **Erwartet:** Dort sind die 5 Spalten SICHTBAR (Default eingeblendet). Spalten per Zahnrad
   aus-/einblenden → Einstellungen ueberleben den Reload (Prefs-Bug-Fix: vorher 400 der
   Prefs-API fuer ViewKey „FaWorklist"). Spaltenfilter (ENTER) auf `Kaeltemittel` etc.
   filtern serverseitig; in FA-Liste/Leitstand filtert ein per URL geteilter
   `?colf_kaeltemittel=...` auch bei ausgeblendeter Spalte (unsichtbar — dokumentiertes
   Verhalten).

### TS-55.9 Reiter ALLGEMEIN + reservierter Code
1. FA mit FA-Vorbau-AGs oeffnen (FA-Vervollstaendigung → Bearbeiten).
2. **Erwartet:** Reiter ALLGEMEIN steht VOR den AG-Reitern (ohne ✓/●-Indikator); aktiv ist
   weiterhin der ERSTE AG-Reiter. Klick auf ALLGEMEIN zeigt die read-only `dl` (Kaeltemittel,
   Ventil, Ausfuehrung E/Z, Maschine, Sage-Status), keine Eingabefelder. `?tab=allgemein`
   (klein) funktioniert ebenfalls.
3. Werkbank im Kopf aendern, waehrend ALLGEMEIN aktiv ist.
4. **Erwartet:** Nach dem Speichern bleibt ALLGEMEIN der aktive Reiter (Tab-Erhalt).
5. FA OHNE FA-Vorbau-AGs oeffnen.
6. **Erwartet:** ALLGEMEIN ist der aktive (einzige) Reiter; darunter erscheint zusaetzlich der
   Hinweis „+ FA-Vorbau-AG hinzufuegen".
7. Stammdaten → FA-Vorbau-AG → Neu: Code `ALLGEMEIN` (auch `allgemein`) anlegen.
8. **Erwartet:** Validierungsfehler „Code 'ALLGEMEIN' ist reserviert ..." — kein Anlegen;
   dasselbe beim Umbenennen eines bestehenden AGs auf `ALLGEMEIN`.

**Negativ/Regression:**
- Ein Fehler im FA-Zusatzinfo-Sync (z. B. Sage nicht erreichbar) stoppt die uebrigen Syncs des
  Zyklus NICHT (`RunResilientAsync`) und loest bei aktivierter `ErrorNotification` eine
  Fehlermail aus; der `FaZusatzinfo`-Lauf steht auf „fehlgeschlagen".
- Leitstand/FA-Liste: SQL-Pagination bleibt bei aktivem Zusatzinfo-Spaltenfilter erhalten
  (kein Force-Full-Load — im Gegensatz zu Datums-/VK-VA-Filtern).
```

(d) Ende-Zeile ersetzen durch:

```markdown
*Ende des Dokuments. Stand: v1.26.0 (2026-07-22)*
```

- [ ] **Step 8: docs-Zwilling spiegeln + Build**

```powershell
Copy-Item docs/TESTSZENARIEN.md secondbrain/docs/TESTSZENARIEN.md
dotnet build IdealAkeWms.slnx --nologo -clp:ErrorsOnly
```

Expected: 0 Errors (Razor-Aenderungen kompilieren).

- [ ] **Step 9: Commit**

```powershell
git add IdealAkeWms/AppVersion.cs IDEALAKEWMSService/AppVersion.cs IdealAkeWms/Views/Help/Changelog.cshtml IdealAkeWms/Views/Help/Index.cshtml CLAUDE.md README.md PROJECT_STATUS.md docs/TESTSZENARIEN.md secondbrain/docs/TESTSZENARIEN.md
git ls-files -s docs/TESTSZENARIEN.md secondbrain/docs/TESTSZENARIEN.md
git commit -m @'
docs(fa-zusatzinfo): v1.26.0 — Changelog, Hilfe (4 Stellen), CLAUDE/README/STATUS, TESTSZENARIEN Kap. 55

AppVersion Web+Service 1.26.0 (2026-07-22). Hilfe: FA-Liste/Leitstand-Hinweis
(default versteckt + Zahnrad), Reiter ALLGEMEIN, Abarbeitungslisten-Spalten,
Aktivitaets-Protokoll-Filter FaZusatzinfo. CLAUDE.md Service-Key + Fallstrick,
README SyncWorker-Bullet + Sync-Gates, PROJECT_STATUS, TESTSZENARIEN Kapitel 55
(TS-55.1-55.9) + Index + Stand — docs-Zwilling gespiegelt.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
'@
```

Expected: TESTSZENARIEN-Hash-Paar identisch, Commit ok.

---

### Task 11: Final-Check — Voll-Build, beide Testsuiten, Zwillings-Hashes, PAUSE

**Files:** keine neuen Aenderungen (reine Verifikation).

- [ ] **Step 1: Voll-Build**

Run: `dotnet build IdealAkeWms.slnx --nologo -clp:ErrorsOnly`
Expected: Build succeeded, 0 Errors, 0 (neue) Warnings.

- [ ] **Step 2: Komplette Web-Testsuite**

Run: `dotnet test IdealAkeWms.Tests --nologo`
Expected: alle Tests gruen — Baseline (Task 0) + 20 neue Web-Tests (1 Drift-Guard, 5 Repo, 6 FaCompletion, 3 WorkSteps [Theory=3 Faelle+1 Fact], 2 FaWorklist, 1 Prefs-API, 2 ProductionOrders-Slim, 2 PickingLeitstand — Zaehlung je nach Theory-Expansion).

- [ ] **Step 3: Komplette Service-Testsuite**

Run: `dotnet test IDEALAKEWMSService.Tests --nologo`
Expected: alle Tests gruen — Baseline + 9 neue (8 Sync + 1 SyncWorker).

- [ ] **Step 4: Zwillings-Hash-Check (alle Paare)**

```powershell
git ls-files -s SQL/81_AddProductionOrderExtraInfo.sql secondbrain/sql/81_AddProductionOrderExtraInfo.sql
git ls-files -s SQL/00_FreshInstall.sql secondbrain/sql/00_FreshInstall.sql
git ls-files -s docs/TESTSZENARIEN.md secondbrain/docs/TESTSZENARIEN.md
git ls-files -s docs/superpowers/plans/2026-07-22-pa-zusatzinfos.md secondbrain/docs/superpowers/plans/2026-07-22-pa-zusatzinfos.md
git ls-files -s docs/superpowers/specs/2026-07-22-pa-zusatzinfos-design.md secondbrain/docs/superpowers/specs/2026-07-22-pa-zusatzinfos-design.md
```

Expected: JEDES Paar zeigt identische Blob-Hashes. (Wurde der Plan waehrend der Ausfuehrung veraendert — Checkboxen! —, VOR dem Check erneut nach secondbrain kopieren und committen.)

- [ ] **Step 5: Arbeitsstand pruefen**

Run: `git status --short; git log --oneline main..HEAD`
Expected: tree sauber; ~9 Commits auf `feature/pa-zusatzinfos` (Plan, Migration, Katalog, Reader, Sync, Worker, Repo, FaCompletion, FaWorklist, Listen, Doku).

- [ ] **Step 6: PAUSE — KEIN Merge**

**STOPP.** NICHT nach `main` mergen, Worktree/Branch NICHT aufraeumen. Der Rollout wartet auf
den User-Test (Manual-UAT TESTSZENARIEN Kapitel 55, insbesondere: echte Sage-View am
Zielsystem verifizieren — Spaltennamen/Laengen gegen Spec §2 —, Deploy-Reihenfolge
Web → Service → DryRun → scharf). Merge nur auf explizite Freigabe des Users.

---

## Abweichungen von der Spec

1. **`ApplyLeitstandColumnFilter`: Contains statt Like — bewusster Stilbruch zu den Bestands-Keys.** Die Spec (§5.3) verlangt fuer die 5 neuen Keys „Contains-basiert und InMemory-testbar, KEIN `EF.Functions.Like`". Die BESTEHENDEN Text-Keys (order-number, customer, ...) im selben Switch nutzen aber `EF.Functions.Like` mit `%token%`-Patterns. Der Plan folgt der Spec (Contains + `ToLower()` auf dem Wert, weil `ColumnFilterHelper.Parse` die Tokens lowercased) und repliziert nur die Token-Semantik (OR ueber Tokens, `!`-Negation, Null-Guards) exakt. Auf SQL Server (CI-Collation) sind beide Varianten gleichwertig; InMemory ist nur die Contains+ToLower-Variante case-insensitiv testbar. Die Alt-Keys wurden bewusst NICHT umgestellt (kein Scope-Creep).
2. **Labels in `ColumnDefinitions.cs` ASCII, in Views/JSON echte Umlaute.** Die Datei `ColumnDefinitions.cs` ist durchgaengig ASCII-transliteriert („Fertigungsauftraege", „Auspraegungen") — die neuen C#-Labels folgen dem („Kaeltemittel", „Ausfuehrung E/Z"). Die sichtbaren Labels (View-`<th>` + inline `#column-config`-JSON, aus dem das Zahnrad liest) verwenden echte Umlaute („Kältemittel", „Ausführung E/Z") wie der Bestand der Views. Fuer diese drei Views wird das C#-Label nirgends gerendert (nur der ViewKey wird validiert) — keine sichtbare Inkonsistenz.
3. **Spec §6 nennt fuer die FaCompletion-Tests „Felder im ViewModel"** — der Plan benennt die ViewModel-Properties `ExtraKaeltemittel`/... (Praefix `Extra`), um Kollisionen mit moeglichen kuenftigen FA-Master-Feldern zu vermeiden; die Spec schreibt keine Property-Namen vor.
4. **SyncWorker-Fail-Safe-Test und Filter-Pass-Through-Tests sind Invarianten-Tests (kein echtes RED).** „Gate default false → Block laeuft nicht" und „neuer Key laeuft in den SQL-Filter-Pfad" sind vor der Implementierung trivially gruen bzw. nicht kompilierbar; ihr Wert ist der Regressionsschutz (Times.Never-Verify bzw. Schutz gegen versehentliche Aufnahme in die Memory-Filter-Sets). Alle uebrigen Tests folgen strikt RED→GREEN.
5. **Migration-Timestamp `<TS>` ist ein bewusster Ausfuehrungs-Platzhalter.** Der EF-Timestamp entsteht erst bei `dotnet ef migrations add` (Task 1 Step 5); SQL/81 + FreshInstall-History-Insert uebernehmen den generierten Wert (explizite Ersetzungs-Anweisung im Step — gleiches Vorgehen wie Plan `2026-07-07-fa-reconciliation.md`).
6. **FreshInstall nutzt `UNIQUE`-CONSTRAINT, EF-Migration einen UNIQUE-INDEX** — bewusst identisch zum bestehenden Praezedenzfall `ProductionOrderPickingStatus`/`ProductionOrderBdeStatus` (FreshInstall: `CONSTRAINT UQ_... UNIQUE`, EF: `HasIndex().IsUnique()` mit demselben Namen). Kein funktionaler Unterschied fuer die App.
7. **`GetAllOrderedAsync` laedt `ExtraInfo` jetzt fuer ALLE Aufrufer** (auch `FaCompletion/Index`, `Picking` etc.) — von der Spec (§5.2) explizit als unkritischer 1:1-LEFT-JOIN akzeptiert.




