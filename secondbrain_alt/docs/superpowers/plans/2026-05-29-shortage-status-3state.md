# ShortageStatus 3-State + 2-Tab MissingParts — Implementation Plan v1.19.0

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `IsFinalShortage` (bool) wird durch `ShortageStatus` (Enum None/WillBeRestocked/NoRestock) ersetzt. UI: 2 Radios je Item mit 3-State-Verhalten. MissingParts: 2 Tabs. Werkbank-Karte: 2 Counts. Migration konvertiert bestehende Daten.

**Architecture:** Datenmodell-Bruch + Migration mit Daten-Konvertierung. Repository + Controller + ViewModels + Views auf Enum umgestellt. Bestehende v1.18.1-Commits (Filter-Erweiterung) bleiben in der Branch-Historie, die v1.18.1-Doku wird im Rahmen von v1.19.0 ueberarbeitet.

**Tech Stack:** ASP.NET Core 10, EF Core 10, SQL Server, Razor, Bootstrap 5, xUnit + FluentAssertions

**Worktree:** `.claude/worktrees/missingparts-include-pd`, Branch `bugfix/missingparts-include-pd`

**Spec:** [secondbrain/docs/superpowers/specs/2026-05-29-shortage-status-3state-design.md](../specs/2026-05-29-shortage-status-3state-design.md) (Commit 36c2c99)

**Branch-Historie vor v1.19.0:**
- `36c2c99` docs(spec): v1.19.0 spec
- `fb4d21e` docs+version v1.18.1
- `f3deba6` fix(repo): v1.18.1 Filter-Fix
- `75ca15c` docs(plan): v1.18.1 plan
- `92d3e9a` docs(spec): v1.18.1 spec

Daraufhin baut v1.19.0 auf. Test-Baseline am Branch-Start (nach Task 0): Web 619 + 1 skip = 620, Service 99.

---

## File Structure

**Create:**
- `IdealAkeWms/Models/ShortageStatus.cs` — neues Enum
- `IdealAkeWms/Migrations/<TIMESTAMP>_ReplaceIsFinalShortageWithShortageStatus.cs` (via `dotnet ef`)
- `SQL/65_ReplaceIsFinalShortageWithShortageStatus.sql`

**Modify:**
- `IdealAkeWms/Models/WarehouseRequisitionItem.cs` — `IsFinalShortage` → `ShortageStatus`
- `IdealAkeWms/Data/Repositories/IWarehouseRequisitionRepository.cs` — Interface-Aenderungen
- `IdealAkeWms/Data/Repositories/WarehouseRequisitionRepository.cs` — DeriveStatus, CloseAsync, SaveProgressAsync, GetMissingPartsAsync, GetShortageCountsForUserAsync
- `IdealAkeWms/Controllers/WarehousePickingController.cs` — Close/SaveProgress/PrintAndClose: bool[] → int[] (ShortageStatus byte values)
- `IdealAkeWms/Controllers/MissingPartsController.cs` — neuer `tab`-Param, Counts laden
- `IdealAkeWms/Controllers/WarehouseRequisitionsController.cs` — 4 Counts statt 2
- `IdealAkeWms/Models/ViewModels/WarehouseRequisitionDetailViewModel.cs` — `IsFinalShortage` → `ShortageStatus`
- `IdealAkeWms/Models/ViewModels/MissingPartRow.cs` — neue Property `Status`
- `IdealAkeWms/Models/ViewModels/MissingPartsListViewModel.cs` — `ActiveTab`, `WaitingTotalCount`, `NoRestockTotalCount`
- `IdealAkeWms/Models/ViewModels/WarehouseRequisitionListViewModel.cs` — 4 Counts statt 2
- `IdealAkeWms/Views/WarehousePicking/Details.cshtml` — 2 Radios + 3-State-JS
- `IdealAkeWms/Views/MissingParts/Index.cshtml` — nav-tabs
- `IdealAkeWms/Views/WarehousePicking/Print.cshtml` — ShortageStatus-Text statt ✓
- `IdealAkeWms/Views/WarehouseRequisitions/Index.cshtml` — Karte mit 2 Zeilen
- `SQL/00_FreshInstall.sql` — Schema-Konsolidierung (IsFinalShortage raus, ShortageStatus rein, 2 Indizes, MigrationsHistory)
- `IdealAkeWms.Tests/Repositories/WarehouseRequisitionRepositoryTests.cs` — bestehende Tests migrieren (bool → enum) + 8 neue
- `IdealAkeWms.Tests/Controllers/WarehousePickingControllerTests.cs` — bestehende migrieren + 2 neue
- `IdealAkeWms.Tests/Controllers/MissingPartsControllerTests.cs` — 3 neue Tests
- `IdealAkeWms.Tests/Controllers/WarehouseRequisitionsControllerTests.cs` — bestehende migrieren + 2 neue
- `IdealAkeWms/AppVersion.cs` + `IDEALAKEWMSService/AppVersion.cs` — v1.19.0
- `IdealAkeWms/Views/Help/Changelog.cshtml` — v1.18.1-Card entfernen, v1.19.0-Card prependen
- `PROJECT_STATUS.md` — v1.18.1-Block entfernen, v1.19.0-Block einfuegen
- `CLAUDE.md` — 1 Fallstrick raus, 3 neue rein
- `docs/TESTSZENARIEN.md` — Kapitel 33 (10 Szenarien)

---

## Task 0: Pre-Flight Baseline

**Files:** keine Aenderungen

- [ ] **Step 1: Branch verifizieren**

```
git rev-parse --abbrev-ref HEAD
git log --oneline -6
```

Expected: `bugfix/missingparts-include-pd`. Letzte 6 Commits enthalten `36c2c99 docs(spec): v1.19.0`, `fb4d21e docs+version: v1.18.1`, `f3deba6 fix(repo): v1.18.1`, `75ca15c docs(plan): v1.18.1`, `92d3e9a docs(spec): v1.18.1`, `5cc204a merge ...v1.18.0`.

- [ ] **Step 2: Baseline-Build**

```
dotnet build IdealAkeWms.slnx
```

Expected: `Build succeeded. 0 Error(s)`. NU1902/CS8602 ok.

- [ ] **Step 3: Baseline-Tests**

```
dotnet test IdealAkeWms.slnx --no-build
```

Expected: Web 619 passed + 1 skipped, Service 99 passed.

- [ ] **Step 4: Migration-Counter pruefen**

```
ls SQL/*.sql | sort | tail -3
```

Expected: letzte SQL-Migration `64_AddIsFinalShortageToWarehouseRequisitionItems.sql` (aus v1.18.0). Naechste = **65**.

- [ ] **Step 5: Bestehende IsFinalShortage-Stellen identifizieren**

```
grep -rn "IsFinalShortage" --include="*.cs" --include="*.cshtml" IdealAkeWms IdealAkeWms.Tests | wc -l
grep -rn "isFinalShortage" --include="*.cs" --include="*.cshtml" IdealAkeWms IdealAkeWms.Tests | wc -l
grep -rn "shortage" --include="*.cs" --include="*.cshtml" IdealAkeWms IdealAkeWms.Tests | head -20
```

Expected: zaehlt alle Stellen (Model + Repo + Controller + Views + Tests). Notiere die Gesamtzahl als Sanity-Check.

---

## Task 1: ShortageStatus Enum + WarehouseRequisitionItem-Property

**Files:**
- Create: `IdealAkeWms/Models/ShortageStatus.cs`
- Modify: `IdealAkeWms/Models/WarehouseRequisitionItem.cs`

- [ ] **Step 1: Enum-Datei anlegen**

`IdealAkeWms/Models/ShortageStatus.cs`:

```csharp
namespace IdealAkeWms.Models;

/// <summary>
/// Klassifizierung des Fehlteil-Status eines Items in einer Lagerbestellung.
/// Seit v1.19.0 ersetzt das bisherige IsFinalShortage-Bool.
/// </summary>
public enum ShortageStatus : byte
{
    /// <summary>
    /// Kein Fehlteil. Default fuer Items mit Ist>=Soll oder ungeklaerten Differenzen.
    /// </summary>
    None = 0,

    /// <summary>
    /// "Fehlteil" — Lagermitarbeiter bestaetigt: Position fehlt, Restlieferung
    /// wird erwartet. Treibt Bestell-Status auf PartiallyDelivered.
    /// </summary>
    WillBeRestocked = 1,

    /// <summary>
    /// "Wird nicht nachgeliefert" — Eskalation. Position fehlt endgueltig.
    /// Action durch Werkbank/Disposition noetig.
    /// </summary>
    NoRestock = 2
}
```

- [ ] **Step 2: WarehouseRequisitionItem.cs — Property tauschen**

In `IdealAkeWms/Models/WarehouseRequisitionItem.cs`:

Suche die Zeile mit `public bool IsFinalShortage { get; set; }` (mit ihrem XML-Doc-Kommentar daruber).

Ersetze die KOMPLETTE Property + Doc durch:

```csharp
    /// <summary>
    /// Fehlteil-Klassifizierung durch den Lagermitarbeiter.
    /// Seit v1.19.0 ersetzt das bisherige IsFinalShortage-Bool durch eine
    /// 3-State-Enum (None / WillBeRestocked / NoRestock).
    /// </summary>
    public ShortageStatus ShortageStatus { get; set; } = ShortageStatus.None;
```

- [ ] **Step 3: Build verifizieren (mit erwarteten Errors)**

```
dotnet build IdealAkeWms.slnx
```

Expected: Build FAIL — Compile-Errors in Repository, Controller, ViewModels, Views, Tests (alle Stellen die `IsFinalShortage` verwenden). Das ist gewollt — Tasks 2-13 fixen sie.

- [ ] **Step 4: Commit (broken build, gewollt)**

```
git add IdealAkeWms/Models/ShortageStatus.cs IdealAkeWms/Models/WarehouseRequisitionItem.cs
git commit -m "feat(model): add ShortageStatus enum, replace IsFinalShortage property

Datenmodell-Bruch. Folge-Tasks fixen Repository/Controller/Views/Tests.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 2: EF-Migration + SQL/65 + FreshInstall

**Files:**
- Create: `IdealAkeWms/Migrations/<TIMESTAMP>_ReplaceIsFinalShortageWithShortageStatus.cs` (via `dotnet ef`)
- Create: `SQL/65_ReplaceIsFinalShortageWithShortageStatus.sql`
- Modify: `SQL/00_FreshInstall.sql`

- [ ] **Step 1: Migration generieren**

```
dotnet ef migrations add ReplaceIsFinalShortageWithShortageStatus --project IdealAkeWms --startup-project IdealAkeWms
```

Notiere den Migration-Dateinamen mit exaktem Timestamp (z.B. `20260529XXXXXX_ReplaceIsFinalShortageWithShortageStatus.cs`).

- [ ] **Step 2: Migration-Up() vervollstaendigen**

Die generierte Migration `Up()`-Methode komplett ersetzen durch:

```csharp
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Neue Spalte (default 0 = None)
            migrationBuilder.AddColumn<byte>(
                name: "ShortageStatus",
                table: "WarehouseRequisitionItems",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            // 2. Daten-Konvertierung: alten Bool-Status auf neuen Enum mappen
            migrationBuilder.Sql(@"
                UPDATE [dbo].[WarehouseRequisitionItems]
                SET [ShortageStatus] = CASE
                    WHEN [IsFinalShortage] = 1 THEN 2
                    WHEN ([QuantityPicked] IS NULL OR [QuantityPicked] < [QuantityRequested]) THEN 1
                    ELSE 0
                END;
            ");

            // 3. Alten Filtered Index droppen
            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS IX_WarehouseRequisitionItems_IsFinalShortage
                    ON [dbo].[WarehouseRequisitionItems];
            ");

            // 4. Default-Constraint dynamisch finden + droppen, dann Spalte droppen
            migrationBuilder.Sql(@"
                DECLARE @c NVARCHAR(200) = (
                    SELECT dc.name FROM sys.default_constraints dc
                    INNER JOIN sys.columns c ON dc.parent_object_id = c.object_id
                        AND dc.parent_column_id = c.column_id
                    WHERE c.object_id = OBJECT_ID('[dbo].[WarehouseRequisitionItems]')
                      AND c.name = 'IsFinalShortage');
                IF @c IS NOT NULL EXEC('ALTER TABLE [dbo].[WarehouseRequisitionItems] DROP CONSTRAINT [' + @c + ']');
            ");
            migrationBuilder.DropColumn(
                name: "IsFinalShortage",
                table: "WarehouseRequisitionItems");

            // 5. Zwei neue Filtered Indizes
            migrationBuilder.Sql(@"
                CREATE INDEX IX_WarehouseRequisitionItems_ShortageStatus_WillBeRestocked
                    ON [dbo].[WarehouseRequisitionItems]([ShortageStatus])
                    WHERE [ShortageStatus] = 1;
                CREATE INDEX IX_WarehouseRequisitionItems_ShortageStatus_NoRestock
                    ON [dbo].[WarehouseRequisitionItems]([ShortageStatus])
                    WHERE [ShortageStatus] = 2;
            ");
        }
```

- [ ] **Step 3: Migration-Down() vervollstaendigen**

`Down()`-Methode komplett ersetzen durch:

```csharp
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS IX_WarehouseRequisitionItems_ShortageStatus_WillBeRestocked
                    ON [dbo].[WarehouseRequisitionItems];
                DROP INDEX IF EXISTS IX_WarehouseRequisitionItems_ShortageStatus_NoRestock
                    ON [dbo].[WarehouseRequisitionItems];
            ");

            migrationBuilder.AddColumn<bool>(
                name: "IsFinalShortage",
                table: "WarehouseRequisitionItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(@"
                UPDATE [dbo].[WarehouseRequisitionItems]
                SET [IsFinalShortage] = CASE WHEN [ShortageStatus] = 2 THEN 1 ELSE 0 END;
            ");

            migrationBuilder.Sql(@"
                CREATE INDEX IX_WarehouseRequisitionItems_IsFinalShortage
                    ON [dbo].[WarehouseRequisitionItems]([IsFinalShortage])
                    WHERE [IsFinalShortage] = 1;
            ");

            migrationBuilder.DropColumn(
                name: "ShortageStatus",
                table: "WarehouseRequisitionItems");
        }
```

- [ ] **Step 4: SQL/65 idempotent erstellen**

Neue Datei `SQL/65_ReplaceIsFinalShortageWithShortageStatus.sql`:

```sql
-- =============================================
-- 65_ReplaceIsFinalShortageWithShortageStatus
-- Replace IsFinalShortage BIT with ShortageStatus TINYINT enum.
-- Data conversion preserves status semantics of existing orders.
-- Used by v1.19.0.
-- =============================================

-- 1. Neue Spalte (idempotent)
IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE Name = N'ShortageStatus'
               AND Object_ID = Object_ID(N'[dbo].[WarehouseRequisitionItems]'))
BEGIN
    ALTER TABLE [dbo].[WarehouseRequisitionItems]
        ADD [ShortageStatus] TINYINT NOT NULL
            CONSTRAINT DF_WarehouseRequisitionItems_ShortageStatus DEFAULT 0;
END
GO

-- 2. Daten-Konvertierung (nur wenn IsFinalShortage noch existiert)
IF EXISTS (SELECT 1 FROM sys.columns
           WHERE Name = N'IsFinalShortage'
           AND Object_ID = Object_ID(N'[dbo].[WarehouseRequisitionItems]'))
BEGIN
    UPDATE [dbo].[WarehouseRequisitionItems]
    SET [ShortageStatus] = CASE
        WHEN [IsFinalShortage] = 1 THEN 2
        WHEN ([QuantityPicked] IS NULL OR [QuantityPicked] < [QuantityRequested]) THEN 1
        ELSE 0
    END
    WHERE [ShortageStatus] = 0;
END
GO

-- 3. Alten Index droppen
IF EXISTS (SELECT 1 FROM sys.indexes
           WHERE name = N'IX_WarehouseRequisitionItems_IsFinalShortage'
           AND object_id = OBJECT_ID(N'[dbo].[WarehouseRequisitionItems]'))
BEGIN
    DROP INDEX IX_WarehouseRequisitionItems_IsFinalShortage
        ON [dbo].[WarehouseRequisitionItems];
END
GO

-- 4. Default-Constraint + alte Spalte droppen
IF EXISTS (SELECT 1 FROM sys.columns
           WHERE Name = N'IsFinalShortage'
           AND Object_ID = Object_ID(N'[dbo].[WarehouseRequisitionItems]'))
BEGIN
    DECLARE @c NVARCHAR(200) = (
        SELECT dc.name FROM sys.default_constraints dc
        INNER JOIN sys.columns c ON dc.parent_object_id = c.object_id
            AND dc.parent_column_id = c.column_id
        WHERE c.object_id = OBJECT_ID('[dbo].[WarehouseRequisitionItems]')
          AND c.name = 'IsFinalShortage');
    IF @c IS NOT NULL EXEC('ALTER TABLE [dbo].[WarehouseRequisitionItems] DROP CONSTRAINT [' + @c + ']');

    ALTER TABLE [dbo].[WarehouseRequisitionItems] DROP COLUMN [IsFinalShortage];
END
GO

-- 5. Neue Filtered Indizes (idempotent)
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = N'IX_WarehouseRequisitionItems_ShortageStatus_WillBeRestocked'
               AND object_id = OBJECT_ID(N'[dbo].[WarehouseRequisitionItems]'))
BEGIN
    CREATE INDEX IX_WarehouseRequisitionItems_ShortageStatus_WillBeRestocked
        ON [dbo].[WarehouseRequisitionItems]([ShortageStatus])
        WHERE [ShortageStatus] = 1;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = N'IX_WarehouseRequisitionItems_ShortageStatus_NoRestock'
               AND object_id = OBJECT_ID(N'[dbo].[WarehouseRequisitionItems]'))
BEGIN
    CREATE INDEX IX_WarehouseRequisitionItems_ShortageStatus_NoRestock
        ON [dbo].[WarehouseRequisitionItems]([ShortageStatus])
        WHERE [ShortageStatus] = 2;
END
GO
```

- [ ] **Step 5: SQL/00_FreshInstall.sql konsolidieren**

In `SQL/00_FreshInstall.sql`:

a) **WarehouseRequisitionItems-Tabellendefinition** finden. Suche nach `IsFinalShortage BIT NOT NULL CONSTRAINT DF_WarehouseRequisitionItems_IsFinalShortage DEFAULT 0,`. Diese Zeile ersetzen durch:
```sql
    [ShortageStatus] TINYINT NOT NULL CONSTRAINT DF_WarehouseRequisitionItems_ShortageStatus DEFAULT 0,
```

b) **Index-Definitionen** fuer WarehouseRequisitionItems finden. Such die Zeile:
```sql
CREATE INDEX IX_WarehouseRequisitionItems_IsFinalShortage
    ON [dbo].[WarehouseRequisitionItems]([IsFinalShortage])
    WHERE [IsFinalShortage] = 1;
GO
```

Ersetze durch:
```sql
CREATE INDEX IX_WarehouseRequisitionItems_ShortageStatus_WillBeRestocked
    ON [dbo].[WarehouseRequisitionItems]([ShortageStatus])
    WHERE [ShortageStatus] = 1;
GO

CREATE INDEX IX_WarehouseRequisitionItems_ShortageStatus_NoRestock
    ON [dbo].[WarehouseRequisitionItems]([ShortageStatus])
    WHERE [ShortageStatus] = 2;
GO
```

c) **`__EFMigrationsHistory`-Block** am Ende. Such den bestehenden Eintrag `(N'<TIMESTAMP>_AddIsFinalShortageToWarehouseRequisitionItems', N'10.0.2')` (aus v1.18.0). Ersetze ihn durch zwei Eintraege:
```sql
    (N'20260529074719_AddIsFinalShortageToWarehouseRequisitionItems', N'10.0.2'),
    (N'<NEUE_TIMESTAMP>_ReplaceIsFinalShortageWithShortageStatus', N'10.0.2'),
```

`<NEUE_TIMESTAMP>` durch den exakten Timestamp aus Step 1 ersetzen. Beachte das Komma-Syntax — wenn der Eintrag nicht der letzte ist, Komma am Ende; wenn doch, Semikolon.

- [ ] **Step 6: Build verifizieren**

```
dotnet build IdealAkeWms.slnx
```

Expected: weiterhin Build FAIL — andere Stellen (Repo, Controller, etc.) noch nicht migriert. Aber: keine NEUEN Errors aus der Migration-Datei.

Pruefen mit:
```
dotnet build IdealAkeWms.slnx 2>&1 | grep -c "ShortageStatus\|IsFinalShortage"
```

Expected: Anzahl bleibt aehnlich wie nach Task 1 (Migration-Datei fuegt keine neuen Compile-Errors hinzu).

- [ ] **Step 7: Commit**

```
git add IdealAkeWms/Migrations/ SQL/65_ReplaceIsFinalShortageWithShortageStatus.sql SQL/00_FreshInstall.sql
git commit -m "feat(db): Migration ReplaceIsFinalShortageWithShortageStatus

EF + idempotent SQL/65 mit Daten-Konvertierung. FreshInstall konsolidiert
(IsFinalShortage durch ShortageStatus ersetzt, 2 Filtered Indizes, beide
MigrationIds in History).

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 3: ViewModels migrieren

**Files:**
- Modify: `IdealAkeWms/Models/ViewModels/WarehouseRequisitionDetailViewModel.cs`
- Modify: `IdealAkeWms/Models/ViewModels/MissingPartRow.cs`
- Modify: `IdealAkeWms/Models/ViewModels/MissingPartsListViewModel.cs`
- Modify: `IdealAkeWms/Models/ViewModels/WarehouseRequisitionListViewModel.cs`

- [ ] **Step 1: WarehouseRequisitionDetailItemViewModel**

In `IdealAkeWms/Models/ViewModels/WarehouseRequisitionDetailViewModel.cs` den `record` aktualisieren — `bool IsFinalShortage = false` durch `ShortageStatus ShortageStatus = ShortageStatus.None`:

```csharp
public record WarehouseRequisitionDetailItemViewModel(
    int Id,
    int Position,
    string ArticleNumber,
    string ArticleDescription,
    string? Unit,
    decimal QuantityRequested,
    decimal? QuantityPicked,
    string StorageLocations,
    string? Note = null,
    ShortageStatus ShortageStatus = ShortageStatus.None);
```

Oben in der Datei sicherstellen dass `using IdealAkeWms.Models;` vorhanden ist.

- [ ] **Step 2: MissingPartRow**

In `IdealAkeWms/Models/ViewModels/MissingPartRow.cs` den `record` um `ShortageStatus Status` erweitern (am Ende der Parameter-Liste):

```csharp
namespace IdealAkeWms.Models.ViewModels;

public record MissingPartRow(
    int RequisitionId,
    int ItemId,
    int Position,
    string WorkplaceName,
    string ArticleNumber,
    string ArticleDescription,
    decimal QuantityRequested,
    decimal QuantityPicked,
    decimal QuantityMissing,
    string? Unit,
    string? Note,
    string CreatedBy,
    DateTime? ClosedAt,
    ShortageStatus Status);
```

Falls `using IdealAkeWms.Models;` fehlt — ergaenzen.

- [ ] **Step 3: MissingPartsListViewModel**

In `IdealAkeWms/Models/ViewModels/MissingPartsListViewModel.cs` 3 neue Properties hinzufuegen:

```csharp
public class MissingPartsListViewModel
{
    public List<MissingPartRow> Items { get; set; } = new();
    public List<ProductionWorkplace> AvailableWorkplaces { get; set; } = new();
    public int? WorkplaceFilter { get; set; }
    public bool MineOnly { get; set; }
    public PaginationState Pagination { get; set; } = new();

    /// <summary>Welcher Tab ist gerade aktiv.</summary>
    public ShortageStatus ActiveTab { get; set; } = ShortageStatus.WillBeRestocked;

    /// <summary>Total-Count des WillBeRestocked-Tab (fuer Tab-Header-Badge).</summary>
    public int WaitingTotalCount { get; set; }

    /// <summary>Total-Count des NoRestock-Tab (fuer Tab-Header-Badge).</summary>
    public int NoRestockTotalCount { get; set; }
}
```

- [ ] **Step 4: WarehouseRequisitionListViewModel**

In `IdealAkeWms/Models/ViewModels/WarehouseRequisitionListViewModel.cs` die zwei bestehenden Counts ersetzen durch 4:

Such die Zeilen:
```csharp
    public int MissingPartsItemCount { get; set; }
    public int MissingPartsRequisitionCount { get; set; }
```

Ersetz durch:
```csharp
    public int MissingPartsWaitingItemCount { get; set; }
    public int MissingPartsWaitingRequisitionCount { get; set; }
    public int MissingPartsNoRestockItemCount { get; set; }
    public int MissingPartsNoRestockRequisitionCount { get; set; }
```

- [ ] **Step 5: Build pruefen**

```
dotnet build IdealAkeWms.slnx
```

Expected: weiterhin Errors (Repo+Controller+Views noch nicht migriert), aber die ViewModel-Dateien selbst kompilieren.

- [ ] **Step 6: Commit**

```
git add IdealAkeWms/Models/ViewModels/WarehouseRequisitionDetailViewModel.cs IdealAkeWms/Models/ViewModels/MissingPartRow.cs IdealAkeWms/Models/ViewModels/MissingPartsListViewModel.cs IdealAkeWms/Models/ViewModels/WarehouseRequisitionListViewModel.cs
git commit -m "feat(viewmodel): migrate to ShortageStatus enum + 2-tab counts

DetailItem.IsFinalShortage -> ShortageStatus. MissingPartRow bekommt Status.
MissingPartsListVM bekommt ActiveTab + 2 TabCounts. ListVM 2 Counts -> 4.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 4: Repository — Interface + DeriveStatus + CloseAsync + SaveProgressAsync (TDD)

**Files:**
- Modify: `IdealAkeWms/Data/Repositories/IWarehouseRequisitionRepository.cs`
- Modify: `IdealAkeWms/Data/Repositories/WarehouseRequisitionRepository.cs`
- Modify: `IdealAkeWms.Tests/Repositories/WarehouseRequisitionRepositoryTests.cs`

- [ ] **Step 1: Tests-Helper migrieren (`SeedRequisitionAsync`)**

In der Test-Datei den `SeedRequisitionAsync`-Helper anpassen. Suche:

```csharp
private async Task<int> SeedRequisitionAsync(ApplicationDbContext db, params (int requested, decimal? picked, bool finalShortage)[] items)
```

Ersetze KOMPLETT durch:

```csharp
    private async Task<int> SeedRequisitionAsync(ApplicationDbContext db, params (int requested, decimal? picked, ShortageStatus status)[] items)
    {
        var r = new WarehouseRequisition
        {
            ProductionWorkplaceId = 1,
            Status = WarehouseRequisitionStatus.Submitted,
            CreatedAt = DateTime.Now,
            CreatedBy = "test",
            CreatedByWindows = "test\\test",
            SubmittedAt = DateTime.Now,
        };
        db.WarehouseRequisitions.Add(r);
        await db.SaveChangesAsync();
        int pos = 1;
        foreach (var (req, picked, status) in items)
        {
            db.WarehouseRequisitionItems.Add(new WarehouseRequisitionItem
            {
                WarehouseRequisitionId = r.Id,
                Position = pos++,
                ArticleNumber = $"ART-{pos}",
                ArticleDescription = $"Article {pos}",
                QuantityRequested = req,
                QuantityPicked = picked,
                ShortageStatus = status,
                CreatedAt = DateTime.Now,
                CreatedBy = "test",
                CreatedByWindows = "test\\test",
            });
        }
        await db.SaveChangesAsync();
        return r.Id;
    }
```

- [ ] **Step 2: Bestehende Tests umstellen (mechanisch)**

In `WarehouseRequisitionRepositoryTests.cs` global ersetzen:

Pattern 1 — `Dictionary<int, bool>` → `Dictionary<int, ShortageStatus>`:
```csharp
// Alt:
new Dictionary<int, bool> { [items[0].Id] = true }
// Neu:
new Dictionary<int, ShortageStatus> { [items[0].Id] = ShortageStatus.NoRestock }

// Alt:
new Dictionary<int, bool> { [items[0].Id] = false }
// Neu (kontextabhaengig):
new Dictionary<int, ShortageStatus> { [items[0].Id] = ShortageStatus.WillBeRestocked }
```

Pattern 2 — `SeedRequisitionAsync(db, (qty, picked, true/false))` → `(qty, picked, ShortageStatus.X)`:
- `true` → `ShortageStatus.NoRestock`
- `false` mit Picked<Requested gemeint → `ShortageStatus.WillBeRestocked`
- `false` mit Picked>=Requested → `ShortageStatus.None`

Pattern 3 — `IsFinalShortage`-Assertions:
```csharp
// Alt:
item.IsFinalShortage.Should().BeTrue();
// Neu:
item.ShortageStatus.Should().Be(ShortageStatus.NoRestock);
```

Empfohlenes Vorgehen: pro Test einzeln durchgehen, Semantik beibehalten. Nicht global Search/Replace — der semantische Kontext (`true` kann sowohl NoRestock als auch... naja, eigentlich `true` ist immer NoRestock).

**Bestehende Test-Methoden umbenennen** wenn Name irrefuehrend wird:
- `GetMissingPartsAsync_IncludesClosedAndPartiallyDelivered_WithFinalShortages` — Body anpassen, Name kann bleiben (semantisch noch korrekt)
- `GetMissingPartsAsync_OnlyIncludesItemsWithIsFinalShortageTrue_InPartiallyDelivered` → umbenennen zu `GetMissingPartsAsync_OnlyIncludesItemsWithNoRestock_InPartiallyDelivered` (semantisch korrekter)
- `CloseAsync_IsFinalShortageTrueButFullyDelivered_FlagIgnoredStatusClosed` → umbenennen zu `CloseAsync_NoRestockButFullyDelivered_StatusIgnoredClosed`
- weitere `IsFinalShortage` in Test-Namen sinngemaess uebersetzen

- [ ] **Step 3: 4 neue Repository-Tests anfuegen**

Am Ende der Test-Klasse:

```csharp
    [Fact]
    public async Task CloseAsync_AllItemsWillBeRestocked_SetsStatusPartiallyDelivered()
    {
        using var db = TestDbContextFactory.Create();
        var repo = new WarehouseRequisitionRepository(db);
        var id = await SeedRequisitionAsync(db, (10, null, ShortageStatus.None), (5, null, ShortageStatus.None));
        var items = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).OrderBy(i => i.Position).ToList();
        var qty = new Dictionary<int, decimal> { [items[0].Id] = 3m, [items[1].Id] = 2m };
        var notes = new Dictionary<int, string?>();
        var statuses = new Dictionary<int, ShortageStatus>
        {
            [items[0].Id] = ShortageStatus.WillBeRestocked,
            [items[1].Id] = ShortageStatus.WillBeRestocked
        };
        await repo.CloseAsync(id, qty, notes, statuses, 1, "u", "w", new byte[0]);
        var r = await db.WarehouseRequisitions.FindAsync(id);
        r!.Status.Should().Be(WarehouseRequisitionStatus.PartiallyDelivered);
    }

    [Fact]
    public async Task CloseAsync_AllShortagesNoRestock_SetsStatusClosed()
    {
        using var db = TestDbContextFactory.Create();
        var repo = new WarehouseRequisitionRepository(db);
        var id = await SeedRequisitionAsync(db, (10, null, ShortageStatus.None), (5, null, ShortageStatus.None));
        var items = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).OrderBy(i => i.Position).ToList();
        var qty = new Dictionary<int, decimal> { [items[0].Id] = 0m, [items[1].Id] = 0m };
        var notes = new Dictionary<int, string?>();
        var statuses = new Dictionary<int, ShortageStatus>
        {
            [items[0].Id] = ShortageStatus.NoRestock,
            [items[1].Id] = ShortageStatus.NoRestock
        };
        await repo.CloseAsync(id, qty, notes, statuses, 1, "u", "w", new byte[0]);
        var r = await db.WarehouseRequisitions.FindAsync(id);
        r!.Status.Should().Be(WarehouseRequisitionStatus.Closed);
    }

    [Fact]
    public async Task CloseAsync_MixedShortageStatuses_SetsStatusPartiallyDelivered()
    {
        using var db = TestDbContextFactory.Create();
        var repo = new WarehouseRequisitionRepository(db);
        var id = await SeedRequisitionAsync(db, (10, null, ShortageStatus.None), (5, null, ShortageStatus.None));
        var items = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).OrderBy(i => i.Position).ToList();
        var qty = new Dictionary<int, decimal> { [items[0].Id] = 5m, [items[1].Id] = 0m };
        var notes = new Dictionary<int, string?>();
        var statuses = new Dictionary<int, ShortageStatus>
        {
            [items[0].Id] = ShortageStatus.WillBeRestocked,
            [items[1].Id] = ShortageStatus.NoRestock
        };
        await repo.CloseAsync(id, qty, notes, statuses, 1, "u", "w", new byte[0]);
        var r = await db.WarehouseRequisitions.FindAsync(id);
        r!.Status.Should().Be(WarehouseRequisitionStatus.PartiallyDelivered);
    }

    [Fact]
    public async Task CloseAsync_ShortageStatusNoneWithShortage_SetsStatusClosed()
    {
        using var db = TestDbContextFactory.Create();
        var repo = new WarehouseRequisitionRepository(db);
        var id = await SeedRequisitionAsync(db, (10, null, ShortageStatus.None));
        var items = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).ToList();
        var qty = new Dictionary<int, decimal> { [items[0].Id] = 3m };
        var notes = new Dictionary<int, string?>();
        var statuses = new Dictionary<int, ShortageStatus> { [items[0].Id] = ShortageStatus.None };
        await repo.CloseAsync(id, qty, notes, statuses, 1, "u", "w", new byte[0]);
        var r = await db.WarehouseRequisitions.FindAsync(id);
        r!.Status.Should().Be(WarehouseRequisitionStatus.Closed);
    }
```

- [ ] **Step 4: Tests laufen — FAIL erwarten**

```
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "FullyQualifiedName~CloseAsync_AllItemsWillBeRestocked|FullyQualifiedName~CloseAsync_AllShortagesNoRestock|FullyQualifiedName~CloseAsync_MixedShortageStatuses|FullyQualifiedName~CloseAsync_ShortageStatusNoneWithShortage"
```

Expected: Build-Fail (CloseAsync nimmt noch bool-Dict — Tests verwenden ShortageStatus-Dict).

- [ ] **Step 5: Interface anpassen**

In `IdealAkeWms/Data/Repositories/IWarehouseRequisitionRepository.cs`:

Such die `CloseAsync`-Signatur. Ersetze `IReadOnlyDictionary<int, bool> itemIsFinalShortages` durch `IReadOnlyDictionary<int, ShortageStatus> itemShortageStatuses`. Komplette neue Signatur:

```csharp
    Task CloseAsync(int id,
        IReadOnlyDictionary<int, decimal> itemQuantitiesPicked,
        IReadOnlyDictionary<int, string?> itemNotes,
        IReadOnlyDictionary<int, ShortageStatus> itemShortageStatuses,
        int closedByUserId, string user, string winUser, byte[] rowVersion);
```

Analog `SaveProgressAsync`:

```csharp
    Task SaveProgressAsync(int id,
        IReadOnlyDictionary<int, decimal?> itemQuantitiesPicked,
        IReadOnlyDictionary<int, string?> itemNotes,
        IReadOnlyDictionary<int, ShortageStatus> itemShortageStatuses,
        string user, string winUser);
```

Stelle sicher dass `using IdealAkeWms.Models;` oben vorhanden ist.

- [ ] **Step 6: Repository — CloseAsync + DeriveStatus + SaveProgressAsync**

In `IdealAkeWms/Data/Repositories/WarehouseRequisitionRepository.cs`:

a) `CloseAsync`-Methode komplett ersetzen durch:

```csharp
    public async Task CloseAsync(int id, IReadOnlyDictionary<int, decimal> itemQuantitiesPicked,
        IReadOnlyDictionary<int, string?> itemNotes,
        IReadOnlyDictionary<int, ShortageStatus> itemShortageStatuses,
        int closedByUserId, string user, string winUser, byte[] rowVersion)
    {
        var r = await _context.WarehouseRequisitions
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new InvalidOperationException($"Requisition {id} not found");
        _context.Entry(r).Property(x => x.RowVersion).OriginalValue = rowVersion;
        foreach (var item in r.Items)
        {
            item.QuantityPicked = itemQuantitiesPicked.TryGetValue(item.Id, out var q) ? q : 0m;
            if (itemNotes.TryGetValue(item.Id, out var note))
                item.Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
            if (itemShortageStatuses.TryGetValue(item.Id, out var status))
                item.ShortageStatus = status;
            item.ModifiedAt = DateTime.Now;
            item.ModifiedBy = user;
            item.ModifiedByWindows = winUser;
        }
        r.Status = DeriveStatus(r);
        r.ClosedAt = DateTime.Now;
        r.ClosedByUserId = closedByUserId;
        r.ModifiedAt = DateTime.Now;
        r.ModifiedBy = user;
        r.ModifiedByWindows = winUser;
        await _context.SaveChangesAsync();
    }
```

b) `DeriveStatus`-Methode komplett ersetzen durch:

```csharp
    private static WarehouseRequisitionStatus DeriveStatus(WarehouseRequisition req)
    {
        bool isFullyDelivered = req.Items.All(i =>
            (i.QuantityPicked ?? 0) >= i.QuantityRequested);
        bool hasWaitingRestock = req.Items.Any(i =>
            i.ShortageStatus == ShortageStatus.WillBeRestocked);

        return (isFullyDelivered || !hasWaitingRestock)
            ? WarehouseRequisitionStatus.Closed
            : WarehouseRequisitionStatus.PartiallyDelivered;
    }
```

c) `SaveProgressAsync`-Methode komplett ersetzen durch:

```csharp
    public async Task SaveProgressAsync(int id,
        IReadOnlyDictionary<int, decimal?> itemQuantitiesPicked,
        IReadOnlyDictionary<int, string?> itemNotes,
        IReadOnlyDictionary<int, ShortageStatus> itemShortageStatuses,
        string user, string winUser)
    {
        var allKeys = itemQuantitiesPicked.Keys
            .Concat(itemNotes.Keys)
            .Concat(itemShortageStatuses.Keys)
            .Distinct()
            .ToList();
        if (allKeys.Count == 0) return;

        var rows = await _context.WarehouseRequisitionItems
            .Where(i => i.WarehouseRequisitionId == id && allKeys.Contains(i.Id))
            .ToListAsync();

        var now = DateTime.Now;
        bool anyChanged = false;
        foreach (var row in rows)
        {
            bool rowChanged = false;
            if (itemQuantitiesPicked.TryGetValue(row.Id, out var qty))
            {
                if (row.QuantityPicked != qty)
                {
                    row.QuantityPicked = qty;
                    rowChanged = true;
                }
            }
            if (itemNotes.TryGetValue(row.Id, out var note))
            {
                var normalized = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
                if (row.Note != normalized)
                {
                    row.Note = normalized;
                    rowChanged = true;
                }
            }
            if (itemShortageStatuses.TryGetValue(row.Id, out var status))
            {
                if (row.ShortageStatus != status)
                {
                    row.ShortageStatus = status;
                    rowChanged = true;
                }
            }
            if (rowChanged)
            {
                row.ModifiedAt = now;
                row.ModifiedBy = user;
                row.ModifiedByWindows = winUser;
                anyChanged = true;
            }
        }
        if (anyChanged) await _context.SaveChangesAsync();
    }
```

- [ ] **Step 7: WarehousePickingController.cs temporaer kompilierbar machen**

Da Controller noch ein `bool[] isFinalShortages`-Array uebergibt, muss er angepasst werden um v1.19.0-Build-Stabilitaet zu erreichen. Aber das ist Task 6.

Workaround fuer JETZT: in den `_repo.CloseAsync(...)` und `_repo.SaveProgressAsync(...)`-Aufrufen im Controller statt `flagDict` einen leeren `new Dictionary<int, ShortageStatus>()` uebergeben.

In `IdealAkeWms/Controllers/WarehousePickingController.cs` finde alle Stellen die `_repo.CloseAsync` oder `_repo.SaveProgressAsync` oder `flagDict` verwenden. Ersetze die Aufrufe mit Dummy-Dict:

```csharp
// Beispiel in Close-Action:
await _repo.CloseAsync(id, qtyDict, noteDict,
    new Dictionary<int, ShortageStatus>(),   // temporaer leer, Task 6 fixt das
    _user.GetCurrentAppUserId() ?? 0,
    _user.GetDisplayName(), _user.GetWindowsUserName(), rowVersion);
```

Auch sicherstellen dass `using IdealAkeWms.Models;` oben im Controller vorhanden ist.

In `SaveProgress`-Action analog. In `PrintAndClose`-Action analog.

- [ ] **Step 8: Build verifizieren**

```
dotnet build IdealAkeWms.slnx
```

Expected: Web-Projekt baut moeglicherweise — Views (Details.cshtml, MissingParts/Index.cshtml, etc.) haben aber noch IsFinalShortage-Referenzen. Test-Projekt wird wegen Test-Migrationen evtl. noch nicht bauen.

Wenn Views noch Compile-Errors haben: temporaer das `i.IsFinalShortage` in den Views durch `false` ersetzen — Tasks 9-11 stellen die Views richtig um. Pragmatisch: Razor-Compile passiert erst zur Laufzeit, daher passieren Razor-Errors NICHT beim Solution-Build. Daher sollte `dotnet build` durchlaufen.

- [ ] **Step 9: Tests laufen**

```
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --no-build
```

Expected: alle Repository-Tests gruen (Helper migriert + 4 neue). Controller-Tests evtl. noch broken — werden in Task 6 fixiert.

Falls Controller-Tests Compile-Fehler werfen: WarehousePickingControllerTests + WarehouseRequisitionsControllerTests temporaer mit `[Fact(Skip = "Task 6 fixes")]` markieren. Pragmatischer Workaround damit Repo-Tests pruefbar sind.

Idealerweise nur Repository-Tests filtern:
```
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "FullyQualifiedName~WarehouseRequisitionRepositoryTests" --no-build
```

Expected: alle Repo-Tests gruen.

- [ ] **Step 10: Commit**

```
git add IdealAkeWms/Data/Repositories/IWarehouseRequisitionRepository.cs IdealAkeWms/Data/Repositories/WarehouseRequisitionRepository.cs IdealAkeWms/Controllers/WarehousePickingController.cs IdealAkeWms.Tests/Repositories/WarehouseRequisitionRepositoryTests.cs
git commit -m "feat(repo): migrate CloseAsync + SaveProgressAsync + DeriveStatus to ShortageStatus

CloseAsync/SaveProgressAsync nehmen jetzt Dictionary<int,ShortageStatus>
statt Dictionary<int,bool>. DeriveStatus prueft jetzt WillBeRestocked
statt !IsFinalShortage. Repository-Tests migriert + 4 neue Tests fuer
3-State-Logik. Controller-Aufrufe vorerst mit leerem Dict (Task 6 fixt).

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 5: Repository — GetMissingPartsAsync + GetShortageCountsForUserAsync (TDD)

**Files:**
- Modify: `IdealAkeWms/Data/Repositories/IWarehouseRequisitionRepository.cs`
- Modify: `IdealAkeWms/Data/Repositories/WarehouseRequisitionRepository.cs`
- Modify: `IdealAkeWms.Tests/Repositories/WarehouseRequisitionRepositoryTests.cs`

- [ ] **Step 1: Bestehende GetMissingPartsAsync-Tests anpassen**

Bestehende Tests (`GetMissingPartsAsync_IncludesClosedAndPartiallyDelivered_WithFinalShortages`, `_AppliesWorkplaceFilter`, `_AppliesColumnFilter_OnArticleNumber_WithOrSyntax`, `_PaginationLimitsResults`, `_OnlyIncludesItemsWithNoRestock_InPartiallyDelivered`, `_ExcludesCancelledRequisitions`) brauchen einen zusaetzlichen Param `ShortageStatus filterStatus`.

Anpassung: Im Aufruf `repo.GetMissingPartsAsync(...)` als ersten Param `ShortageStatus.NoRestock` einsetzen (das war die Default-Bedeutung bisher).

Beispiel:
```csharp
// Alt:
var (items, total) = await repo.GetMissingPartsAsync(null, null, null, null, 1, 100);
// Neu:
var (items, total) = await repo.GetMissingPartsAsync(ShortageStatus.NoRestock, null, null, null, null, 1, 100);
```

Pro Test in der Datei diese Mechanik anwenden (5-7 Aufrufe).

- [ ] **Step 2: 2 neue Tests fuer Tab-Filter**

Am Ende der Test-Klasse:

```csharp
    [Fact]
    public async Task GetMissingPartsAsync_TabWillBeRestocked_ReturnsOnlyMatchingItems()
    {
        using var db = TestDbContextFactory.Create();
        db.ProductionWorkplaces.Add(new ProductionWorkplace { Id = 1, Name = "WB1" });
        await db.SaveChangesAsync();
        var repo = new WarehouseRequisitionRepository(db);
        var id = await SeedRequisitionAsync(db, (10, 0m, ShortageStatus.WillBeRestocked), (5, 0m, ShortageStatus.NoRestock));
        var items = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).OrderBy(i => i.Position).ToList();
        await repo.CloseAsync(id,
            new Dictionary<int, decimal> { [items[0].Id] = 0m, [items[1].Id] = 0m },
            new Dictionary<int, string?>(),
            new Dictionary<int, ShortageStatus> { [items[0].Id] = ShortageStatus.WillBeRestocked, [items[1].Id] = ShortageStatus.NoRestock },
            1, "u", "w", new byte[0]);
        // Bestellung wird PartiallyDelivered (Item 1 ist WillBeRestocked)

        var (result, total) = await repo.GetMissingPartsAsync(ShortageStatus.WillBeRestocked, null, null, null, null, 1, 100);
        result.Should().HaveCount(1);
        result[0].ItemId.Should().Be(items[0].Id);
        result[0].Status.Should().Be(ShortageStatus.WillBeRestocked);
    }

    [Fact]
    public async Task GetMissingPartsAsync_TabNoRestock_ReturnsOnlyMatchingItems()
    {
        using var db = TestDbContextFactory.Create();
        db.ProductionWorkplaces.Add(new ProductionWorkplace { Id = 1, Name = "WB1" });
        await db.SaveChangesAsync();
        var repo = new WarehouseRequisitionRepository(db);
        var id = await SeedRequisitionAsync(db, (10, 0m, ShortageStatus.WillBeRestocked), (5, 0m, ShortageStatus.NoRestock));
        var items = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).OrderBy(i => i.Position).ToList();
        await repo.CloseAsync(id,
            new Dictionary<int, decimal> { [items[0].Id] = 0m, [items[1].Id] = 0m },
            new Dictionary<int, string?>(),
            new Dictionary<int, ShortageStatus> { [items[0].Id] = ShortageStatus.WillBeRestocked, [items[1].Id] = ShortageStatus.NoRestock },
            1, "u", "w", new byte[0]);

        var (result, total) = await repo.GetMissingPartsAsync(ShortageStatus.NoRestock, null, null, null, null, 1, 100);
        result.Should().HaveCount(1);
        result[0].ItemId.Should().Be(items[1].Id);
        result[0].Status.Should().Be(ShortageStatus.NoRestock);
    }
```

- [ ] **Step 3: GetShortageCountsForUserAsync-Tests neu**

Bestehende `GetFinalShortagesCountForUserAsync_*` Tests UMBENENNEN und ergaenzen:

```csharp
    [Fact]
    public async Task GetShortageCountsForUserAsync_ReturnsBothCounts()
    {
        using var db = TestDbContextFactory.Create();
        db.ProductionWorkplaces.Add(new ProductionWorkplace { Id = 1, Name = "WB1" });
        db.ProductionWorkplaceUsers.Add(new ProductionWorkplaceUser
        {
            UserId = 42, ProductionWorkplaceId = 1,
            CreatedAt = DateTime.Now, CreatedBy = "test", CreatedByWindows = "test\\test"
        });
        await db.SaveChangesAsync();
        var repo = new WarehouseRequisitionRepository(db);

        // Bestellung 1: 2 Items, beide WillBeRestocked
        var r1 = await SeedRequisitionAsync(db, (10, 0m, ShortageStatus.WillBeRestocked), (5, 0m, ShortageStatus.WillBeRestocked));
        var i1 = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == r1).OrderBy(i => i.Position).ToList();
        await repo.CloseAsync(r1,
            new Dictionary<int, decimal> { [i1[0].Id] = 0m, [i1[1].Id] = 0m },
            new Dictionary<int, string?>(),
            new Dictionary<int, ShortageStatus> { [i1[0].Id] = ShortageStatus.WillBeRestocked, [i1[1].Id] = ShortageStatus.WillBeRestocked },
            1, "u", "w", new byte[0]);

        // Bestellung 2: 1 Item NoRestock
        var r2 = await SeedRequisitionAsync(db, (10, 0m, ShortageStatus.NoRestock));
        var i2 = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == r2).Single();
        await repo.CloseAsync(r2,
            new Dictionary<int, decimal> { [i2.Id] = 0m },
            new Dictionary<int, string?>(),
            new Dictionary<int, ShortageStatus> { [i2.Id] = ShortageStatus.NoRestock },
            1, "u", "w", new byte[0]);

        var (waitingItems, waitingReqs, noRestockItems, noRestockReqs) = await repo.GetShortageCountsForUserAsync(42);
        waitingItems.Should().Be(2);
        waitingReqs.Should().Be(1);
        noRestockItems.Should().Be(1);
        noRestockReqs.Should().Be(1);
    }

    [Fact]
    public async Task GetShortageCountsForUserAsync_OnlyForUserWorkplaces()
    {
        using var db = TestDbContextFactory.Create();
        db.ProductionWorkplaces.AddRange(
            new ProductionWorkplace { Id = 1, Name = "WB1" },
            new ProductionWorkplace { Id = 2, Name = "WB2" });
        db.ProductionWorkplaceUsers.Add(new ProductionWorkplaceUser
        {
            UserId = 42, ProductionWorkplaceId = 1,
            CreatedAt = DateTime.Now, CreatedBy = "test", CreatedByWindows = "test\\test"
        });
        await db.SaveChangesAsync();
        var repo = new WarehouseRequisitionRepository(db);

        // WB2 (User 42 NICHT zugeordnet)
        var r2req = new WarehouseRequisition
        {
            ProductionWorkplaceId = 2, Status = WarehouseRequisitionStatus.Submitted,
            CreatedAt = DateTime.Now, CreatedBy = "u", CreatedByWindows = "w", SubmittedAt = DateTime.Now
        };
        db.WarehouseRequisitions.Add(r2req); await db.SaveChangesAsync();
        var r2item = new WarehouseRequisitionItem
        {
            WarehouseRequisitionId = r2req.Id, Position = 1, ArticleNumber = "X",
            ArticleDescription = "Y", QuantityRequested = 5m,
            CreatedAt = DateTime.Now, CreatedBy = "u", CreatedByWindows = "w"
        };
        db.WarehouseRequisitionItems.Add(r2item); await db.SaveChangesAsync();
        await repo.CloseAsync(r2req.Id,
            new Dictionary<int, decimal> { [r2item.Id] = 0m },
            new Dictionary<int, string?>(),
            new Dictionary<int, ShortageStatus> { [r2item.Id] = ShortageStatus.NoRestock },
            1, "u", "w", new byte[0]);

        var (waitingItems, _, noRestockItems, _) = await repo.GetShortageCountsForUserAsync(42);
        waitingItems.Should().Be(0);
        noRestockItems.Should().Be(0);
    }
```

- [ ] **Step 4: Tests laufen — FAIL erwarten**

```
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "FullyQualifiedName~GetMissingPartsAsync_TabWillBeRestocked|FullyQualifiedName~GetMissingPartsAsync_TabNoRestock|FullyQualifiedName~GetShortageCountsForUserAsync"
```

Expected: Compile-Fail oder Test-Fail — Methoden noch nicht angepasst.

- [ ] **Step 5: Interface erweitern**

In `IdealAkeWms/Data/Repositories/IWarehouseRequisitionRepository.cs`:

a) `GetMissingPartsAsync`-Signatur erweitern um `ShortageStatus filterStatus` als ERSTER Param:

```csharp
    Task<(IReadOnlyList<MissingPartRow> Items, int TotalCount)>
        GetMissingPartsAsync(ShortageStatus filterStatus,
                             int? workplaceFilter,
                             IReadOnlyDictionary<string, string>? columnFilters,
                             DateTime? closedFrom, DateTime? closedUntil,
                             int page, int pageSize);
```

b) `GetFinalShortagesCountForUserAsync` ersetzen durch `GetShortageCountsForUserAsync`:

```csharp
    Task<(int WaitingItemCount, int WaitingRequisitionCount,
          int NoRestockItemCount, int NoRestockRequisitionCount)>
        GetShortageCountsForUserAsync(int userId);
```

- [ ] **Step 6: Implementierung — GetMissingPartsAsync**

In `WarehouseRequisitionRepository.cs` die bestehende `GetMissingPartsAsync`-Methode komplett ersetzen durch:

```csharp
    public async Task<(IReadOnlyList<MissingPartRow> Items, int TotalCount)>
        GetMissingPartsAsync(ShortageStatus filterStatus,
                             int? workplaceFilter,
                             IReadOnlyDictionary<string, string>? columnFilters,
                             DateTime? closedFrom, DateTime? closedUntil,
                             int page, int pageSize)
    {
        var q = _context.WarehouseRequisitionItems
            .Include(i => i.WarehouseRequisition)
                .ThenInclude(r => r.ProductionWorkplace)
            .Where(i => i.ShortageStatus == filterStatus
                && (i.WarehouseRequisition.Status == WarehouseRequisitionStatus.Closed
                    || i.WarehouseRequisition.Status == WarehouseRequisitionStatus.PartiallyDelivered));

        if (workplaceFilter.HasValue)
            q = q.Where(i => i.WarehouseRequisition.ProductionWorkplaceId == workplaceFilter.Value);
        if (closedFrom.HasValue)
            q = q.Where(i => i.WarehouseRequisition.ClosedAt >= closedFrom.Value);
        if (closedUntil.HasValue)
            q = q.Where(i => i.WarehouseRequisition.ClosedAt < closedUntil.Value);

        if (columnFilters != null)
        {
            if (columnFilters.TryGetValue("ArticleNumber", out var an) && !string.IsNullOrWhiteSpace(an))
                q = ApplyMissingPartsTextFilter(q, an, isArticleNumber: true);
            if (columnFilters.TryGetValue("ArticleDescription", out var ad) && !string.IsNullOrWhiteSpace(ad))
                q = ApplyMissingPartsTextFilter(q, ad, isArticleNumber: false, isDescription: true);
            if (columnFilters.TryGetValue("WorkplaceName", out var wn) && !string.IsNullOrWhiteSpace(wn))
                q = ApplyMissingPartsTextFilter(q, wn, isArticleNumber: false, isDescription: false, isWorkplace: true);
        }

        var total = await q.CountAsync();
        var rows = await q.OrderByDescending(i => i.WarehouseRequisition.ClosedAt ?? DateTime.MinValue)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new MissingPartRow(
                i.WarehouseRequisitionId,
                i.Id,
                i.Position,
                i.WarehouseRequisition.ProductionWorkplace.Name,
                i.ArticleNumber,
                i.ArticleDescription,
                i.QuantityRequested,
                i.QuantityPicked ?? 0m,
                i.QuantityRequested - (i.QuantityPicked ?? 0m),
                i.Unit,
                i.Note,
                i.WarehouseRequisition.CreatedBy,
                i.WarehouseRequisition.ClosedAt,
                i.ShortageStatus))
            .ToListAsync();

        return (rows, total);
    }
```

Die `ApplyMissingPartsTextFilter`-Helper-Methode bleibt unveraendert.

- [ ] **Step 7: Implementierung — GetShortageCountsForUserAsync**

Die bestehende `GetFinalShortagesCountForUserAsync`-Methode komplett ersetzen durch:

```csharp
    public async Task<(int WaitingItemCount, int WaitingRequisitionCount,
                       int NoRestockItemCount, int NoRestockRequisitionCount)>
        GetShortageCountsForUserAsync(int userId)
    {
        var userWorkplaceIds = await _context.ProductionWorkplaceUsers
            .Where(u => u.UserId == userId)
            .Select(u => u.ProductionWorkplaceId)
            .ToListAsync();
        if (userWorkplaceIds.Count == 0) return (0, 0, 0, 0);

        var q = _context.WarehouseRequisitionItems
            .Where(i => (i.WarehouseRequisition.Status == WarehouseRequisitionStatus.Closed
                         || i.WarehouseRequisition.Status == WarehouseRequisitionStatus.PartiallyDelivered)
                && userWorkplaceIds.Contains(i.WarehouseRequisition.ProductionWorkplaceId));

        var waiting = q.Where(i => i.ShortageStatus == ShortageStatus.WillBeRestocked);
        var noRestock = q.Where(i => i.ShortageStatus == ShortageStatus.NoRestock);

        int waitingItems = await waiting.CountAsync();
        int waitingReqs = await waiting.Select(i => i.WarehouseRequisitionId).Distinct().CountAsync();
        int noRestockItems = await noRestock.CountAsync();
        int noRestockReqs = await noRestock.Select(i => i.WarehouseRequisitionId).Distinct().CountAsync();

        return (waitingItems, waitingReqs, noRestockItems, noRestockReqs);
    }
```

- [ ] **Step 8: Aufrufer in Controllers temporaer fixen**

In `IdealAkeWms/Controllers/WarehouseRequisitionsController.cs` such den Aufruf:

```csharp
var (missingItemCount, missingReqCount) = await _repo.GetFinalShortagesCountForUserAsync(userId);
```

Ersetz durch:

```csharp
var (missingItemCount, missingReqCount, missingNoRestockItemCount, missingNoRestockReqCount) =
    await _repo.GetShortageCountsForUserAsync(userId);
```

Und im ViewModel-Init die alte Zuweisung:
```csharp
MissingPartsItemCount = missingItemCount,
MissingPartsRequisitionCount = missingReqCount,
```
Ersetz durch:
```csharp
MissingPartsWaitingItemCount = missingItemCount,
MissingPartsWaitingRequisitionCount = missingReqCount,
MissingPartsNoRestockItemCount = missingNoRestockItemCount,
MissingPartsNoRestockRequisitionCount = missingNoRestockReqCount,
```

In `IdealAkeWms/Controllers/MissingPartsController.cs` such alle `_repo.GetMissingPartsAsync(...)`-Aufrufe. Sie nehmen jetzt einen Erst-Param `ShortageStatus filterStatus`. Provisorisch alle Aufrufe mit `ShortageStatus.NoRestock` als Erst-Param ergaenzen (Task 7 macht das richtig).

- [ ] **Step 9: Build + Tests**

```
dotnet build IdealAkeWms.slnx
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "FullyQualifiedName~WarehouseRequisitionRepositoryTests" --no-build
```

Expected: alle Repository-Tests passing (inkl. die neuen 4).

- [ ] **Step 10: Commit**

```
git add IdealAkeWms/Data/Repositories/IWarehouseRequisitionRepository.cs IdealAkeWms/Data/Repositories/WarehouseRequisitionRepository.cs IdealAkeWms/Controllers/WarehouseRequisitionsController.cs IdealAkeWms/Controllers/MissingPartsController.cs IdealAkeWms.Tests/Repositories/WarehouseRequisitionRepositoryTests.cs
git commit -m "feat(repo): GetMissingPartsAsync tab param + GetShortageCountsForUserAsync 4-tuple

GetMissingPartsAsync bekommt ShortageStatus filterStatus als erster Param.
GetFinalShortagesCountForUserAsync ersetzt durch GetShortageCountsForUserAsync
mit 4-Tuple (Waiting + NoRestock je Items+Reqs). 4 neue Tests.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 6: WarehousePickingController vollstaendig migrieren

**Files:**
- Modify: `IdealAkeWms/Controllers/WarehousePickingController.cs`
- Modify: `IdealAkeWms.Tests/Controllers/WarehousePickingControllerTests.cs`

- [ ] **Step 1: Controller — Close/SaveProgress/PrintAndClose**

In `IdealAkeWms/Controllers/WarehousePickingController.cs`:

a) **`Close`-Action**: `bool[]? isFinalShortages` durch `int[]? shortageStatuses` ersetzen + Dict-Build entsprechend anpassen:

```csharp
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(int id, int[] itemIds, int[] quantitiesPicked,
        string?[]? notes, int[]? shortageStatuses, byte[] rowVersion)
    {
        if (quantitiesPicked.Any(q => q < 0))
        {
            TempData["WarningMessage"] = "Ist-Mengen duerfen nicht negativ sein.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var qtyDict = new Dictionary<int, decimal>();
        for (int idx = 0; idx < itemIds.Length; idx++)
            qtyDict[itemIds[idx]] = idx < quantitiesPicked.Length ? quantitiesPicked[idx] : 0m;

        var noteDict = new Dictionary<int, string?>();
        if (notes != null)
            for (int idx = 0; idx < itemIds.Length; idx++)
                noteDict[itemIds[idx]] = idx < notes.Length ? notes[idx] : null;

        var statusDict = new Dictionary<int, ShortageStatus>();
        if (shortageStatuses != null)
        {
            for (int idx = 0; idx < itemIds.Length; idx++)
            {
                var raw = idx < shortageStatuses.Length ? shortageStatuses[idx] : 0;
                statusDict[itemIds[idx]] = raw switch
                {
                    1 => ShortageStatus.WillBeRestocked,
                    2 => ShortageStatus.NoRestock,
                    _ => ShortageStatus.None
                };
            }
        }

        try
        {
            await _repo.CloseAsync(id, qtyDict, noteDict, statusDict,
                _user.GetCurrentAppUserId() ?? 0,
                _user.GetDisplayName(), _user.GetWindowsUserName(), rowVersion);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            TempData["WarningMessage"] = "Bestellung wurde inzwischen geaendert — bitte Liste neu laden.";
            return RedirectToAction(nameof(Details), new { id });
        }
        TempData["SuccessMessage"] = $"Liste #{id} abgeschlossen.";
        return RedirectToAction(nameof(Index));
    }
```

b) **`SaveProgress`-Action** analog mit `int[]? shortageStatuses`:

```csharp
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveProgress(int id,
        [FromForm] int[] itemIds,
        [FromForm] int?[]? quantitiesPicked,
        [FromForm] string?[]? notes,
        [FromForm] int[]? shortageStatuses)
    {
        if (itemIds == null || itemIds.Length == 0) return BadRequest("itemIds required");

        var qtyDict = new Dictionary<int, decimal?>();
        if (quantitiesPicked != null)
            for (int idx = 0; idx < itemIds.Length; idx++)
                qtyDict[itemIds[idx]] = idx < quantitiesPicked.Length ? (decimal?)quantitiesPicked[idx] : null;
        var noteDict = new Dictionary<int, string?>();
        if (notes != null)
            for (int idx = 0; idx < itemIds.Length; idx++)
                noteDict[itemIds[idx]] = idx < notes.Length ? notes[idx] : null;
        var statusDict = new Dictionary<int, ShortageStatus>();
        if (shortageStatuses != null)
        {
            for (int idx = 0; idx < itemIds.Length; idx++)
            {
                var raw = idx < shortageStatuses.Length ? shortageStatuses[idx] : 0;
                statusDict[itemIds[idx]] = raw switch
                {
                    1 => ShortageStatus.WillBeRestocked,
                    2 => ShortageStatus.NoRestock,
                    _ => ShortageStatus.None
                };
            }
        }

        await _repo.SaveProgressAsync(id, qtyDict, noteDict, statusDict,
            _user.GetDisplayName(), _user.GetWindowsUserName());
        return Ok();
    }
```

c) **`PrintAndClose`-Action** analog (gleiches Pattern wie Close, aber returnt JSON):

```csharp
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PrintAndClose(int id, int[] itemIds, int[] quantitiesPicked,
        string?[]? notes, int[]? shortageStatuses, byte[] rowVersion)
    {
        if (quantitiesPicked.Any(q => q < 0))
            return BadRequest(new { error = "Ist-Mengen duerfen nicht negativ sein." });

        var qtyDict = new Dictionary<int, decimal>();
        for (int idx = 0; idx < itemIds.Length; idx++)
            qtyDict[itemIds[idx]] = idx < quantitiesPicked.Length ? quantitiesPicked[idx] : 0m;
        var noteDict = new Dictionary<int, string?>();
        if (notes != null)
            for (int idx = 0; idx < itemIds.Length; idx++)
                noteDict[itemIds[idx]] = idx < notes.Length ? notes[idx] : null;
        var statusDict = new Dictionary<int, ShortageStatus>();
        if (shortageStatuses != null)
        {
            for (int idx = 0; idx < itemIds.Length; idx++)
            {
                var raw = idx < shortageStatuses.Length ? shortageStatuses[idx] : 0;
                statusDict[itemIds[idx]] = raw switch
                {
                    1 => ShortageStatus.WillBeRestocked,
                    2 => ShortageStatus.NoRestock,
                    _ => ShortageStatus.None
                };
            }
        }

        try
        {
            await _repo.CloseAsync(id, qtyDict, noteDict, statusDict,
                _user.GetCurrentAppUserId() ?? 0,
                _user.GetDisplayName(), _user.GetWindowsUserName(), rowVersion);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            return Conflict(new { error = "Bestellung wurde inzwischen geaendert." });
        }
        return Ok(new { redirectUrl = Url.Action(nameof(Print), new { id }) });
    }
```

d) **`Details`-Action** und **`Print`-Action**: DetailItem-Mapping anpassen — `i.IsFinalShortage` → `i.ShortageStatus`:

```csharp
detailItems.Add(new WarehouseRequisitionDetailItemViewModel(
    i.Id, i.Position, i.ArticleNumber, i.ArticleDescription, i.Unit,
    i.QuantityRequested, i.QuantityPicked, locationStr, i.Note, i.ShortageStatus));
```

Beide Actions anpassen.

- [ ] **Step 2: Bestehende Controller-Tests migrieren**

In `IdealAkeWms.Tests/Controllers/WarehousePickingControllerTests.cs` alle Stellen die `bool[] isFinalShortages` verwenden auf `int[] shortageStatuses` umstellen. Wert-Konvertierung:
- `new[] { true, false }` → `new[] { 2, 0 }` (NoRestock, None)
- `new[] { true, true }` → `new[] { 2, 2 }`
- `null` bleibt `null`

Pro Test einzeln durchgehen.

Mock-Verifications:
```csharp
// Alt:
It.Is<IReadOnlyDictionary<int, bool>>(d => d[1] == true && d[2] == false)
// Neu:
It.Is<IReadOnlyDictionary<int, ShortageStatus>>(d => d[1] == ShortageStatus.NoRestock && d[2] == ShortageStatus.None)
```

- [ ] **Step 3: 2 neue Controller-Tests**

Am Ende der Test-Klasse:

```csharp
    [Fact]
    public async Task Close_BindsShortageStatusesIntArray()
    {
        var (ctrl, repo, _, _) = SetupWithMockRepo();
        await ctrl.Close(id: 1, itemIds: new[] { 10, 20 },
            quantitiesPicked: new[] { 5, 0 },
            notes: null,
            shortageStatuses: new[] { 1, 2 },  // WillBeRestocked, NoRestock
            rowVersion: new byte[0]);
        repo.Verify(r => r.CloseAsync(1,
            It.IsAny<IReadOnlyDictionary<int, decimal>>(),
            It.IsAny<IReadOnlyDictionary<int, string?>>(),
            It.Is<IReadOnlyDictionary<int, ShortageStatus>>(d =>
                d[10] == ShortageStatus.WillBeRestocked && d[20] == ShortageStatus.NoRestock),
            It.IsAny<int>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>()),
            Times.Once);
    }

    [Fact]
    public async Task SaveProgress_PersistsShortageStatuses()
    {
        var (ctrl, repo, _, _) = SetupWithMockRepo();
        await ctrl.SaveProgress(id: 1,
            itemIds: new[] { 10 },
            quantitiesPicked: new int?[] { 5 },
            notes: new string?[] { "n" },
            shortageStatuses: new[] { 1 });
        repo.Verify(r => r.SaveProgressAsync(1,
            It.IsAny<IReadOnlyDictionary<int, decimal?>>(),
            It.IsAny<IReadOnlyDictionary<int, string?>>(),
            It.Is<IReadOnlyDictionary<int, ShortageStatus>>(d => d[10] == ShortageStatus.WillBeRestocked),
            It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);
    }
```

Falls `SetupWithMockRepo()` und ein Setup-Helper schon existiert (aus v1.18.x) — verwenden. Sonst Pattern aus bestehenden Tests in der Datei uebernehmen.

- [ ] **Step 4: Build + Tests**

```
dotnet build IdealAkeWms.slnx
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "FullyQualifiedName~WarehousePickingControllerTests" --no-build
```

Expected: alle Picking-Controller-Tests passing.

- [ ] **Step 5: Commit**

```
git add IdealAkeWms/Controllers/WarehousePickingController.cs IdealAkeWms.Tests/Controllers/WarehousePickingControllerTests.cs
git commit -m "feat(controller): WarehousePickingController auf ShortageStatus int[] umgestellt

Close/SaveProgress/PrintAndClose binden jetzt int[] shortageStatuses (Werte
0/1/2) statt bool[] isFinalShortages. Mapped intern auf ShortageStatus-Enum.
Bestehende Tests migriert, 2 neue Tests fuer Form-Binding.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 7: MissingPartsController mit Tabs + WarehouseRequisitionsController mit 4 Counts

**Files:**
- Modify: `IdealAkeWms/Controllers/MissingPartsController.cs`
- Modify: `IdealAkeWms/Controllers/WarehouseRequisitionsController.cs`
- Modify: `IdealAkeWms.Tests/Controllers/MissingPartsControllerTests.cs`
- Modify: `IdealAkeWms.Tests/Controllers/WarehouseRequisitionsControllerTests.cs`

- [ ] **Step 1: MissingPartsController.Index — Tab-Param + 2-Count-Load**

In `IdealAkeWms/Controllers/MissingPartsController.cs` die `Index`-Action komplett ersetzen durch:

```csharp
    public async Task<IActionResult> Index(
        ShortageStatus tab = ShortageStatus.WillBeRestocked,
        int? workplaceId = null,
        bool mineOnly = false,
        int page = 1, int? pageSize = null)
    {
        if (page < 1) page = 1;

        // Tab-Normalisierung: None ist ungueltig fuer die Liste -> Default-Tab
        if (tab == ShortageStatus.None) tab = ShortageStatus.WillBeRestocked;

        var userDefaultPageSize = await _user.GetDefaultPageSizeAsync();
        var effectivePageSize = IdealAkeWms.Services.PageSize.Resolve(pageSize, userDefaultPageSize);
        var rawPageSize = IdealAkeWms.Services.PageSize.ResolveRaw(pageSize, userDefaultPageSize);

        int? effectiveWorkplaceId = workplaceId;
        List<int>? userWorkplaceIds = null;
        if (mineOnly)
        {
            var userId = _user.GetCurrentAppUserId() ?? 0;
            var userWorkplaces = await _workplaces.GetByUserIdAsync(userId);
            userWorkplaceIds = userWorkplaces.Select(w => w.Id).ToList();
            if (workplaceId.HasValue && !userWorkplaceIds.Contains(workplaceId.Value))
                effectiveWorkplaceId = -1;
            else if (!workplaceId.HasValue && userWorkplaceIds.Count == 1)
                effectiveWorkplaceId = userWorkplaceIds[0];
        }

        var columnFilters = IdealAkeWms.Services.ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);

        // Items des aktiven Tabs laden
        var (rawRows, total) = await _repo.GetMissingPartsAsync(
            tab,
            effectiveWorkplaceId == -1 ? null : effectiveWorkplaceId,
            columnFilters,
            null, null, page, effectivePageSize);

        IReadOnlyList<MissingPartRow> rows = rawRows;
        if (mineOnly && effectiveWorkplaceId == null && userWorkplaceIds != null)
        {
            var allWp = await _workplaces.GetAllAsync();
            var allowedNames = allWp.Where(w => userWorkplaceIds.Contains(w.Id))
                                    .Select(w => w.Name).ToHashSet();
            var filtered = rawRows.Where(r => allowedNames.Contains(r.WorkplaceName)).ToList();
            rows = filtered;
            total = filtered.Count;
        }
        else if (mineOnly && effectiveWorkplaceId == -1)
        {
            rows = new List<MissingPartRow>();
            total = 0;
        }

        // Counts fuer beide Tabs (fuer Tab-Header-Badges)
        var (waitingTotal, _) = await _repo.GetMissingPartsAsync(
            ShortageStatus.WillBeRestocked,
            effectiveWorkplaceId == -1 ? null : effectiveWorkplaceId,
            null, null, null, 1, 1);
        var (noRestockTotal, _) = await _repo.GetMissingPartsAsync(
            ShortageStatus.NoRestock,
            effectiveWorkplaceId == -1 ? null : effectiveWorkplaceId,
            null, null, null, 1, 1);

        var vm = new MissingPartsListViewModel
        {
            Items = rows.ToList(),
            AvailableWorkplaces = (await _workplaces.GetAllAsync()).OrderBy(w => w.Name).ToList(),
            WorkplaceFilter = workplaceId,
            MineOnly = mineOnly,
            ActiveTab = tab,
            WaitingTotalCount = waitingTotal.Count,    // count via Tuple-Item — actually fix:
            NoRestockTotalCount = noRestockTotal.Count,
            Pagination = new PaginationState
            {
                CurrentPage = page,
                PageSize = effectivePageSize,
                PageSizeRaw = rawPageSize,
                TotalCount = total
            }
        };
        return View(vm);
    }
```

**Wichtig**: `(waitingTotal, _)` destructures into `(IReadOnlyList<MissingPartRow>, int)`. Daher `waitingTotal.Count` (Liste-Count statt Tuple-int). Oder lieber nach Wert-Indizes destructuren:

```csharp
var waitingResult = await _repo.GetMissingPartsAsync(ShortageStatus.WillBeRestocked,
    effectiveWorkplaceId == -1 ? null : effectiveWorkplaceId, null, null, null, 1, 1);
var waitingTotalCount = waitingResult.TotalCount;

var noRestockResult = await _repo.GetMissingPartsAsync(ShortageStatus.NoRestock,
    effectiveWorkplaceId == -1 ? null : effectiveWorkplaceId, null, null, null, 1, 1);
var noRestockTotalCount = noRestockResult.TotalCount;
```

Und dann im ViewModel:
```csharp
WaitingTotalCount = waitingTotalCount,
NoRestockTotalCount = noRestockTotalCount,
```

**Performance-Hinweis**: 3 DB-Roundtrips fuer eine Page (Items + 2 Counts). Optional koennte das Repository eine combined-Methode bieten, aber YAGNI fuer jetzt.

- [ ] **Step 2: MissingPartsControllerTests migrieren + 3 neue**

In `IdealAkeWms.Tests/Controllers/MissingPartsControllerTests.cs`:

a) Mock-Setup anpassen — `GetMissingPartsAsync` nimmt jetzt `ShortageStatus`-Param:

```csharp
repo.Setup(r => r.GetMissingPartsAsync(
        It.IsAny<ShortageStatus>(),
        It.IsAny<int?>(),
        It.IsAny<IReadOnlyDictionary<string, string>?>(),
        It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
        It.IsAny<int>(), It.IsAny<int>()))
    .ReturnsAsync(((IReadOnlyList<MissingPartRow>)new List<MissingPartRow>(), 0));
```

b) Bestehende Tests anpassen — `Index(workplaceId: 5, mineOnly: false)` → `Index(tab: ShortageStatus.WillBeRestocked, workplaceId: 5, mineOnly: false)` (oder die named-arg-Variante).

c) 3 neue Tests:

```csharp
    [Fact]
    public async Task Index_DefaultTab_WillBeRestocked()
    {
        var (ctrl, repo, _, _) = Build();
        await ctrl.Index();
        repo.Verify(r => r.GetMissingPartsAsync(ShortageStatus.WillBeRestocked,
            It.IsAny<int?>(), It.IsAny<IReadOnlyDictionary<string,string>?>(),
            It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
            It.IsAny<int>(), It.IsAny<int>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Index_TabNone_NormalizedToWillBeRestocked()
    {
        var (ctrl, repo, _, _) = Build();
        await ctrl.Index(tab: ShortageStatus.None);
        repo.Verify(r => r.GetMissingPartsAsync(ShortageStatus.WillBeRestocked,
            It.IsAny<int?>(), It.IsAny<IReadOnlyDictionary<string,string>?>(),
            It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
            It.IsAny<int>(), It.IsAny<int>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Index_ViewModelHasBothCounts()
    {
        var (ctrl, repo, _, _) = Build();
        repo.Setup(r => r.GetMissingPartsAsync(ShortageStatus.WillBeRestocked,
                It.IsAny<int?>(), It.IsAny<IReadOnlyDictionary<string,string>?>(),
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(((IReadOnlyList<MissingPartRow>)new List<MissingPartRow>(), 3));
        repo.Setup(r => r.GetMissingPartsAsync(ShortageStatus.NoRestock,
                It.IsAny<int?>(), It.IsAny<IReadOnlyDictionary<string,string>?>(),
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(((IReadOnlyList<MissingPartRow>)new List<MissingPartRow>(), 5));
        var result = await ctrl.Index();
        var vm = (result as ViewResult)?.Model as MissingPartsListViewModel;
        vm!.WaitingTotalCount.Should().Be(3);
        vm.NoRestockTotalCount.Should().Be(5);
    }
```

- [ ] **Step 3: WarehouseRequisitionsController.Index — 4 Counts**

In `IdealAkeWms/Controllers/WarehouseRequisitionsController.cs` (sollte schon in Task 5 Step 8 angepasst sein — verifizieren). Falls noch nicht, jetzt nachziehen.

- [ ] **Step 4: WarehouseRequisitionsControllerTests migrieren + 2 neue**

Bestehende Tests `Index_ShowsMissingPartsCard_WhenUserHasFinalShortages` und `Index_HidesMissingPartsCard_WhenNoShortages` anpassen:
- Mock `GetFinalShortagesCountForUserAsync` → `GetShortageCountsForUserAsync` mit 4-Tuple
- Assertions auf `MissingPartsWaitingItemCount` etc.

Pattern:
```csharp
// Setup
repo.Setup(r => r.GetShortageCountsForUserAsync(It.IsAny<int>()))
    .ReturnsAsync((3, 2, 1, 1));  // waiting items 3, waiting reqs 2, noRestock items 1, noRestock reqs 1

// Assert (Index_ShowsMissingPartsCard_WhenUserHasFinalShortages umbenennen):
vm.MissingPartsWaitingItemCount.Should().Be(3);
vm.MissingPartsWaitingRequisitionCount.Should().Be(2);
vm.MissingPartsNoRestockItemCount.Should().Be(1);
vm.MissingPartsNoRestockRequisitionCount.Should().Be(1);
```

- [ ] **Step 5: Build + Tests**

```
dotnet build IdealAkeWms.slnx
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --no-build
```

Expected: alle Controller-Tests passing.

- [ ] **Step 6: Commit**

```
git add IdealAkeWms/Controllers/MissingPartsController.cs IdealAkeWms/Controllers/WarehouseRequisitionsController.cs IdealAkeWms.Tests/Controllers/MissingPartsControllerTests.cs IdealAkeWms.Tests/Controllers/WarehouseRequisitionsControllerTests.cs
git commit -m "feat(controller): MissingParts mit Tab-Param, Werkbank mit 4 Counts

MissingPartsController.Index nimmt tab-Param, laedt beide Counts fuer
Tab-Header. None wird auf WillBeRestocked normalisiert. WarehouseRequisitions
laedt 4 Counts via GetShortageCountsForUserAsync. Tests migriert + 5 neue.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 8: Details.cshtml — 2 Radios + 3-State-JS

**Files:**
- Modify: `IdealAkeWms/Views/WarehousePicking/Details.cshtml`

- [ ] **Step 1: View komplett ueberschreiben**

`IdealAkeWms/Views/WarehousePicking/Details.cshtml` komplett ersetzen durch:

```html
@model IdealAkeWms.Models.ViewModels.WarehouseRequisitionDetailViewModel
@using IdealAkeWms.Models
@{
    ViewData["Title"] = $"Lagerbestellung #{Model.Id}";
    bool isEditable = Model.Status == WarehouseRequisitionStatus.Submitted
                   || Model.Status == WarehouseRequisitionStatus.PartiallyDelivered;
}

<div class="d-flex justify-content-between align-items-center flex-wrap gap-2 page-header">
    <h2 class="mb-0">Lagerbestellung #@Model.Id</h2>
    <a asp-action="Index" class="btn btn-sm btn-outline-secondary">Zurueck</a>
</div>

<div class="card mb-3">
    <div class="card-body p-2">
        <div><strong>Werkbank:</strong> @Model.WorkplaceName</div>
        <div><strong>Erfasser:</strong> @Model.CreatedBy</div>
        <div><strong>Submit:</strong> @(Model.SubmittedAt?.ToString("dd.MM.yyyy HH:mm") ?? "—")</div>
        <div><strong>Status:</strong>
            @switch (Model.Status)
            {
                case WarehouseRequisitionStatus.Submitted: <span class="badge bg-primary">Abgeschickt</span> break;
                case WarehouseRequisitionStatus.PartiallyDelivered: <span class="badge bg-warning text-dark">Teilgeliefert</span> break;
                case WarehouseRequisitionStatus.Closed: <span class="badge bg-success">Erledigt am @Model.ClosedAt?.ToString("dd.MM.yyyy HH:mm")</span> break;
                case WarehouseRequisitionStatus.Cancelled: <span class="badge bg-dark">Storniert am @Model.CancelledAt?.ToString("dd.MM.yyyy HH:mm")</span> break;
            }
        </div>
        @if (!string.IsNullOrEmpty(Model.CancellationReason))
        {
            <div><strong>Storno-Grund:</strong> @Model.CancellationReason</div>
        }
    </div>
</div>

@if (Model.Status == WarehouseRequisitionStatus.PartiallyDelivered)
{
    <div class="alert alert-warning">
        <strong>Teilgeliefert.</strong> Items als <em>Fehlteil</em> markiert bleiben offen (Restlieferung erwartet).
        Eskalation per <em>Wird nicht nachgeliefert</em>.
    </div>
}

<form method="post" asp-action="Close" asp-route-id="@Model.Id" id="detailsForm">
    @Html.AntiForgeryToken()
    <input type="hidden" name="rowVersion" value="@Convert.ToBase64String(Model.RowVersion)" />
    <div class="table-responsive">
    <table class="table table-sm">
        <thead>
            <tr>
                <th>Pos</th><th>Artikel-Nr</th><th>Bezeichnung</th><th>Bestellt</th>
                <th>Ist</th><th>ME</th><th>Lagerplatz</th><th>Notiz</th>
                <th title="Fehlteil-Status">Fehlteil-Status</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var i in Model.Items)
            {
                var requestedInt = (int)Math.Round(i.QuantityRequested, MidpointRounding.AwayFromZero);
                var pickedInt = i.QuantityPicked.HasValue
                    ? (int?)Math.Round(i.QuantityPicked.Value, MidpointRounding.AwayFromZero)
                    : null;
                <tr data-requested="@requestedInt">
                    <td>@i.Position</td>
                    <td>@i.ArticleNumber</td>
                    <td>@i.ArticleDescription</td>
                    <td>@requestedInt</td>
                    <td>
                        <input type="hidden" name="itemIds" value="@i.Id" />
                        @if (isEditable)
                        {
                            <input type="number" name="quantitiesPicked" step="1" min="0"
                                   value="@(pickedInt?.ToString() ?? string.Empty)"
                                   placeholder="@requestedInt"
                                   class="form-control form-control-sm qty-input" style="width:90px;" />
                        }
                        else
                        {
                            @(pickedInt?.ToString() ?? "—")
                        }
                    </td>
                    <td>@i.Unit</td>
                    <td><small>@i.StorageLocations</small></td>
                    <td>
                        @if (isEditable)
                        {
                            <input type="text" name="notes" value="@i.Note" maxlength="500"
                                   class="form-control form-control-sm note-input" placeholder="Notiz (optional)" />
                        }
                        else
                        {
                            <small>@i.Note</small>
                        }
                    </td>
                    <td class="text-nowrap">
                        @if (isEditable)
                        {
                            <input type="hidden" name="shortageStatuses" value="@((byte)i.ShortageStatus)" class="shortage-hidden" />
                            <div class="form-check form-check-inline shortage-radio-group">
                                <input type="radio" class="form-check-input shortage-radio shortage-radio-restock"
                                       id="sr_@(i.Id)_1" value="1"
                                       @(i.ShortageStatus == ShortageStatus.WillBeRestocked ? "checked" : "") />
                                <label class="form-check-label text-warning" for="sr_@(i.Id)_1"
                                       title="Restlieferung wird erwartet">Fehlteil</label>
                            </div>
                            <div class="form-check form-check-inline">
                                <input type="radio" class="form-check-input shortage-radio shortage-radio-norestock"
                                       id="sr_@(i.Id)_2" value="2"
                                       @(i.ShortageStatus == ShortageStatus.NoRestock ? "checked" : "") />
                                <label class="form-check-label text-danger" for="sr_@(i.Id)_2"
                                       title="Endgueltig nicht lieferbar">Wird nicht nachgeliefert</label>
                            </div>
                        }
                        else
                        {
                            @switch (i.ShortageStatus)
                            {
                                case ShortageStatus.WillBeRestocked: <span class="badge bg-warning text-dark">Fehlteil</span> break;
                                case ShortageStatus.NoRestock: <span class="badge bg-danger">Wird nicht nachgeliefert</span> break;
                            }
                        }
                    </td>
                </tr>
            }
        </tbody>
    </table>
    </div>

    <a asp-action="Print" asp-route-id="@Model.Id" target="_blank" class="btn btn-outline-secondary" id="btn-print">Drucken</a>

    @if (isEditable)
    {
        <button type="button" id="btn-print-and-close" class="btn btn-primary">Drucken und Abschliessen</button>
        <button type="button" id="btn-close-finish" class="btn btn-success">Speichern + Abschliessen</button>
        <button type="button" class="btn btn-outline-danger" data-bs-toggle="modal" data-bs-target="#cancel-modal">Stornieren</button>
    }
</form>

<div class="modal fade" id="cancel-modal" tabindex="-1">
    <div class="modal-dialog">
        <form method="post" asp-action="Cancel" asp-route-id="@Model.Id">
            @Html.AntiForgeryToken()
            <input type="hidden" name="rowVersion" value="@Convert.ToBase64String(Model.RowVersion)" />
            <div class="modal-content">
                <div class="modal-header"><h5 class="modal-title">Liste stornieren</h5></div>
                <div class="modal-body">
                    <label class="form-label">Grund (optional)</label>
                    <textarea name="reason" class="form-control" rows="3"></textarea>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Abbrechen</button>
                    <button type="submit" class="btn btn-danger">Stornieren</button>
                </div>
            </div>
        </form>
    </div>
</div>

<div class="modal fade" id="close-confirm-modal" tabindex="-1">
    <div class="modal-dialog">
        <div class="modal-content">
            <div class="modal-header"><h5 class="modal-title">Ist-Menge fehlt</h5></div>
            <div class="modal-body">
                Bei einigen Positionen wurde keine Ist-Menge eingegeben.<br />
                <strong>Soll = Ist-Menge buchen?</strong>
            </div>
            <div class="modal-footer">
                <button type="button" id="close-yes" class="btn btn-success">Ja</button>
                <button type="button" id="close-no" class="btn btn-warning">Nein</button>
                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Abbrechen</button>
            </div>
        </div>
    </div>
</div>

@section Scripts {
<script>
(function () {
    const form = document.getElementById('detailsForm');
    if (!form) return;
    const isEditable = @Json.Serialize(isEditable);
    const closeBtn = document.getElementById('btn-close-finish');
    const printAndCloseBtn = document.getElementById('btn-print-and-close');
    const printBtn = document.getElementById('btn-print');
    const saveProgressUrl = '@Url.Action("SaveProgress", "WarehousePicking", new { id = Model.Id })';
    const printAndCloseUrl = '@Url.Action("PrintAndClose", "WarehousePicking", new { id = Model.Id })';
    const token = form.querySelector('input[name="__RequestVerificationToken"]')?.value;
    let dirty = false;

    function syncShortageRadios(row) {
        const hidden = row.querySelector('.shortage-hidden');
        const restock = row.querySelector('.shortage-radio-restock');
        const norestock = row.querySelector('.shortage-radio-norestock');
        const qtyInput = row.querySelector('.qty-input');
        if (!hidden || !restock || !norestock) return;

        const requested = parseInt(row.dataset.requested, 10) || 0;
        const picked = qtyInput && qtyInput.value ? parseInt(qtyInput.value, 10) : 0;
        const shortage = picked < requested;

        restock.disabled = !shortage;
        norestock.disabled = !shortage;
        if (!shortage) {
            restock.checked = false;
            norestock.checked = false;
            hidden.value = '0';
            return;
        }

        // Default bei Ist=0 und nichts gewaehlt: Fehlteil (WillBeRestocked)
        if (picked === 0 && !restock.checked && !norestock.checked) {
            restock.checked = true;
            hidden.value = '1';
            return;
        }

        hidden.value = norestock.checked ? '2' : (restock.checked ? '1' : '0');
    }

    if (isEditable) {
        // Initiale Synchronisation
        document.querySelectorAll('#detailsForm tbody tr').forEach(syncShortageRadios);

        // Mengen-Input triggert Sync + dirty
        document.querySelectorAll('.qty-input').forEach(inp => {
            inp.addEventListener('input', () => {
                syncShortageRadios(inp.closest('tr'));
                dirty = true;
            });
        });

        // Radio-Click: Hidden-Wert spiegeln + 3-State (Doppelklick = None)
        document.querySelectorAll('.shortage-radio').forEach(r => {
            let wasChecked = r.checked;
            r.addEventListener('mousedown', () => { wasChecked = r.checked; });
            r.addEventListener('click', () => {
                const tr = r.closest('tr');
                const restock = tr.querySelector('.shortage-radio-restock');
                const norestock = tr.querySelector('.shortage-radio-norestock');
                const hidden = tr.querySelector('.shortage-hidden');
                if (wasChecked) {
                    r.checked = false;
                    hidden.value = '0';
                } else {
                    hidden.value = norestock.checked ? '2' : (restock.checked ? '1' : '0');
                }
                dirty = true;
            });
        });

        // Notiz-Input: dirty + save on blur
        document.querySelectorAll('.note-input').forEach(inp => {
            inp.addEventListener('input', () => { dirty = true; });
            inp.addEventListener('blur', saveProgress);
        });
    }

    function collectProgress() {
        const itemIds = Array.from(form.querySelectorAll('input[name="itemIds"]')).map(i => i.value);
        const quantitiesPicked = Array.from(form.querySelectorAll('input[name="quantitiesPicked"]'))
            .map(i => i.value || '');
        const notes = Array.from(form.querySelectorAll('input[name="notes"]')).map(i => i.value || '');
        const shortageStatuses = Array.from(form.querySelectorAll('input.shortage-hidden')).map(i => i.value);
        return { itemIds, quantitiesPicked, notes, shortageStatuses };
    }

    async function saveProgress() {
        if (!dirty || !token) return;
        const { itemIds, quantitiesPicked, notes, shortageStatuses } = collectProgress();
        const body = new FormData();
        body.append('__RequestVerificationToken', token);
        itemIds.forEach(v => body.append('itemIds', v));
        quantitiesPicked.forEach(v => body.append('quantitiesPicked', v));
        notes.forEach(v => body.append('notes', v));
        shortageStatuses.forEach(v => body.append('shortageStatuses', v));
        try {
            await fetch(saveProgressUrl, { method: 'POST', body });
            dirty = false;
        } catch { /* bleibt dirty */ }
    }

    if (printBtn) {
        printBtn.addEventListener('click', async (e) => {
            if (!dirty) return;
            e.preventDefault();
            const href = printBtn.getAttribute('href');
            const win = window.open('about:blank', '_blank');
            await saveProgress();
            if (win) win.location = href; else window.location = href;
        });
    }

    if (!closeBtn) return;

    const modalEl = document.getElementById('close-confirm-modal');
    const modal = new bootstrap.Modal(modalEl);
    function emptyRows() {
        return Array.from(form.querySelectorAll('input[name="quantitiesPicked"]'))
            .filter(i => !i.value || parseInt(i.value, 10) === 0);
    }
    function fillSollAsIst() {
        emptyRows().forEach(inp => {
            const tr = inp.closest('tr');
            inp.value = tr.dataset.requested;
            syncShortageRadios(tr);
        });
    }

    closeBtn.addEventListener('click', () => {
        const empties = emptyRows();
        if (empties.length === 0) { form.submit(); return; }
        modal.show();
        document.getElementById('close-yes').onclick = () => { fillSollAsIst(); modal.hide(); form.submit(); };
        document.getElementById('close-no').onclick = () => { modal.hide(); form.submit(); };
    });

    if (printAndCloseBtn) {
        printAndCloseBtn.addEventListener('click', async () => {
            const empties = emptyRows();
            if (empties.length > 0) {
                modal.show();
                document.getElementById('close-yes').onclick = () => { fillSollAsIst(); modal.hide(); performPrintAndClose(); };
                document.getElementById('close-no').onclick = () => { modal.hide(); performPrintAndClose(); };
                return;
            }
            performPrintAndClose();
        });
    }

    async function performPrintAndClose() {
        const printTab = window.open('about:blank', '_blank');
        const body = new FormData(form);
        try {
            const resp = await fetch(printAndCloseUrl, { method: 'POST', body });
            if (!resp.ok) {
                if (printTab) printTab.close();
                alert('Fehler beim Abschliessen — bitte Liste neu laden.');
                window.location.reload();
                return;
            }
            const data = await resp.json();
            if (printTab && data.redirectUrl) printTab.location.href = data.redirectUrl;
            window.location.href = '@Url.Action("Index", "WarehousePicking")';
        } catch (ex) {
            if (printTab) printTab.close();
            alert('Netzwerkfehler — bitte erneut versuchen.');
        }
    }
})();
</script>
}
```

- [ ] **Step 2: Build verifizieren**

```
dotnet build IdealAkeWms.slnx
```

Expected: success (Razor compile sollte sauber sein).

- [ ] **Step 3: Commit**

```
git add IdealAkeWms/Views/WarehousePicking/Details.cshtml
git commit -m "feat(view): Details mit 2 Radios + 3-State-JS

Spalte 'Fehlteil-Status' bekommt 2 Radio-Buttons (Fehlteil / Wird nicht
nachgeliefert) mit Doppelklick-zu-None-Verhalten. Hidden-Input sammelt
ShortageStatus-Byte-Werte (0/1/2). Default bei Ist=0 ist WillBeRestocked.
SaveProgress + PrintAndClose senden jetzt shortageStatuses statt
isFinalShortages.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 9: MissingParts/Index.cshtml + Print.cshtml + Werkbank-Karte

**Files:**
- Modify: `IdealAkeWms/Views/MissingParts/Index.cshtml`
- Modify: `IdealAkeWms/Views/WarehousePicking/Print.cshtml`
- Modify: `IdealAkeWms/Views/WarehouseRequisitions/Index.cshtml`

- [ ] **Step 1: MissingParts/Index.cshtml — nav-tabs**

`IdealAkeWms/Views/MissingParts/Index.cshtml` komplett ersetzen durch:

```html
@model IdealAkeWms.Models.ViewModels.MissingPartsListViewModel
@using IdealAkeWms.Models
@{
    ViewData["Title"] = Model.MineOnly ? "Meine Fehlteile" : "Fehlteile";
}

<h2 class="page-header">@ViewData["Title"]</h2>

@if (TempData["SuccessMessage"] != null)
{
    <div class="alert alert-success">@TempData["SuccessMessage"]</div>
}
@if (TempData["WarningMessage"] != null)
{
    <div class="alert alert-warning">@TempData["WarningMessage"]</div>
}

<ul class="nav nav-tabs mb-3">
    <li class="nav-item">
        <a class="nav-link @(Model.ActiveTab == ShortageStatus.WillBeRestocked ? "active" : "")"
           asp-action="Index" asp-route-tab="WillBeRestocked"
           asp-route-workplaceId="@Model.WorkplaceFilter" asp-route-mineOnly="@Model.MineOnly">
            Offene Fehlteile
            <span class="badge bg-warning text-dark ms-1">@Model.WaitingTotalCount</span>
        </a>
    </li>
    <li class="nav-item">
        <a class="nav-link @(Model.ActiveTab == ShortageStatus.NoRestock ? "active" : "")"
           asp-action="Index" asp-route-tab="NoRestock"
           asp-route-workplaceId="@Model.WorkplaceFilter" asp-route-mineOnly="@Model.MineOnly">
            Wird nicht nachgeliefert
            <span class="badge bg-danger ms-1">@Model.NoRestockTotalCount</span>
        </a>
    </li>
</ul>

<div class="card filter-card mb-3">
    <div class="card-body p-2">
        <form method="get" class="d-flex gap-2 align-items-center flex-wrap">
            <input type="hidden" name="tab" value="@Model.ActiveTab" />
            <label class="form-label mb-0">Werkbank:</label>
            <select name="workplaceId" class="form-select form-select-sm" style="width:auto;" onchange="this.form.submit()">
                <option value="">— Alle —</option>
                @foreach (var w in Model.AvailableWorkplaces)
                {
                    <option value="@w.Id" selected="@(Model.WorkplaceFilter == w.Id)">@w.Name</option>
                }
            </select>
            <div class="form-check ms-3">
                <input type="checkbox" name="mineOnly" value="true" id="mineOnly" class="form-check-input"
                       @(Model.MineOnly ? "checked" : "") onchange="this.form.submit()" />
                <label for="mineOnly" class="form-check-label">Nur meine Werkbaenke</label>
            </div>
        </form>
    </div>
</div>

<div class="table-responsive">
    <table class="table table-striped filterable-table"
           data-server-column-filter="true"
           data-view-key="MissingParts">
        <thead>
            <tr>
                <th data-col-key="RequisitionId">Bestell-ID</th>
                <th data-col-key="WorkplaceName">Werkbank</th>
                <th data-col-key="ArticleNumber">Artikel-Nr</th>
                <th data-col-key="ArticleDescription">Bezeichnung</th>
                <th data-col-key="QuantityRequested">Soll</th>
                <th data-col-key="QuantityPicked">Geliefert</th>
                <th data-col-key="QuantityMissing">Fehlt</th>
                <th data-col-key="Note">Notiz</th>
                <th data-col-key="CreatedBy">Erfasst von</th>
                <th data-col-key="ClosedAt">Abgeschlossen am</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var row in Model.Items)
            {
                <tr>
                    <td><a asp-controller="WarehousePicking" asp-action="Details" asp-route-id="@row.RequisitionId">#@row.RequisitionId</a></td>
                    <td>@row.WorkplaceName</td>
                    <td>@row.ArticleNumber</td>
                    <td>@row.ArticleDescription</td>
                    <td>@((int)Math.Round(row.QuantityRequested, MidpointRounding.AwayFromZero))</td>
                    <td>@((int)Math.Round(row.QuantityPicked, MidpointRounding.AwayFromZero))</td>
                    <td><strong>@((int)Math.Round(row.QuantityMissing, MidpointRounding.AwayFromZero))</strong></td>
                    <td>@row.Note</td>
                    <td>@row.CreatedBy</td>
                    <td>@(row.ClosedAt?.ToString("dd.MM.yyyy HH:mm") ?? "—")</td>
                </tr>
            }
        </tbody>
    </table>
</div>

<partial name="_Pagination" model="Model.Pagination" />
```

- [ ] **Step 2: Print.cshtml — ShortageStatus-Text**

In `IdealAkeWms/Views/WarehousePicking/Print.cshtml`:

a) Oben sicherstellen `@using IdealAkeWms.Models` vorhanden ist (sonst ergaenzen).

b) Such die Zeile mit der Fehlteil-Spalte im `<tbody>`:
```html
<td>@(i.IsFinalShortage ? "✓" : "")</td>
```

Ersetz durch:
```html
<td>
    @switch (i.ShortageStatus)
    {
        case ShortageStatus.WillBeRestocked: <text>Fehlteil</text> break;
        case ShortageStatus.NoRestock: <text>Wird nicht nachgeliefert</text> break;
        default: <text></text> break;
    }
</td>
```

- [ ] **Step 3: WarehouseRequisitions/Index.cshtml — Karte mit 2 Zeilen**

In `IdealAkeWms/Views/WarehouseRequisitions/Index.cshtml`:

Such den bestehenden Card-Block `@if (Model.MissingPartsItemCount > 0)`. Komplett ersetzen durch:

```html
@if (Model.MissingPartsWaitingItemCount > 0 || Model.MissingPartsNoRestockItemCount > 0)
{
    <div class="card border-warning mb-3">
        <div class="card-body">
            <h6 class="card-title">⚠ Meine Fehlteile</h6>
            @if (Model.MissingPartsWaitingItemCount > 0)
            {
                <div class="mb-1">
                    <a asp-controller="MissingParts" asp-action="Index"
                       asp-route-tab="WillBeRestocked" asp-route-mineOnly="true"
                       class="text-warning fw-semibold text-decoration-none">
                        @Model.MissingPartsWaitingItemCount Fehlteile (wird nachgeliefert)
                        <small class="text-muted">aus @Model.MissingPartsWaitingRequisitionCount Bestellungen →</small>
                    </a>
                </div>
            }
            @if (Model.MissingPartsNoRestockItemCount > 0)
            {
                <div>
                    <a asp-controller="MissingParts" asp-action="Index"
                       asp-route-tab="NoRestock" asp-route-mineOnly="true"
                       class="text-danger fw-semibold text-decoration-none">
                        @Model.MissingPartsNoRestockItemCount Wird nicht nachgeliefert
                        <small class="text-muted">aus @Model.MissingPartsNoRestockRequisitionCount Bestellungen →</small>
                    </a>
                </div>
            }
        </div>
    </div>
}
```

- [ ] **Step 4: Build + Tests**

```
dotnet build IdealAkeWms.slnx
dotnet test IdealAkeWms.slnx --no-build
```

Expected: build success, alle Tests gruen.

- [ ] **Step 5: Commit**

```
git add IdealAkeWms/Views/MissingParts/Index.cshtml IdealAkeWms/Views/WarehousePicking/Print.cshtml IdealAkeWms/Views/WarehouseRequisitions/Index.cshtml
git commit -m "feat(view): MissingParts mit 2 Tabs + Print + Werkbank-Karte

MissingParts/Index: nav-tabs (Offene Fehlteile / Wird nicht nachgeliefert)
mit Tab-Counts. Print: Status-Text statt ✓. Werkbank-Karte: 1 Karte, 2
Zeilen mit eigenen Tab-Links.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 10: Version + Changelog + PROJECT_STATUS + CLAUDE.md + TESTSZENARIEN

**Files:**
- Modify: `IdealAkeWms/AppVersion.cs`
- Modify: `IDEALAKEWMSService/AppVersion.cs`
- Modify: `IdealAkeWms/Views/Help/Changelog.cshtml`
- Modify: `PROJECT_STATUS.md`
- Modify: `CLAUDE.md`
- Modify: `docs/TESTSZENARIEN.md`

- [ ] **Step 1: AppVersion Web + Service**

Beide auf `1.19.0` / `2026-05-29`.

`IdealAkeWms/AppVersion.cs`:
```csharp
namespace IdealAkeWms;

public static class AppVersion
{
    public const string Version = "1.19.0";
    public const string Date = "2026-05-29";
}
```

`IDEALAKEWMSService/AppVersion.cs`:
```csharp
namespace IDEALAKEWMSService;

public static class AppVersion
{
    public const string Version = "1.19.0";
    public const string Date = "2026-05-29";
}
```

- [ ] **Step 2: Changelog — v1.18.1-Card entfernen, v1.19.0-Card prependen**

In `IdealAkeWms/Views/Help/Changelog.cshtml`:

a) Such die in v1.18.1 angelegte `<div class="card mb-3">`-Karte mit `<strong>v1.18.1</strong>`. ENTFERNE diese komplette Card (alle Zeilen vom oeffnenden `<div class="card mb-3">` bis zum schliessenden `</div>` der Card).

b) Direkt nach `<div class="col-lg-8">` und VOR der v1.18.0-Card NEUE Card einfuegen:

```html
        <div class="card mb-3">
            <div class="card-header text-white" style="background-color: var(--ake-primary);">
                <strong>v1.19.0</strong> <span class="text-white-50 ms-2">29.05.2026</span>
            </div>
            <div class="card-body">
                <h6>Lagerbestellungen: 3-State-Klassifizierung + 2-Tab Fehlteile-Liste</h6>
                <ul>
                    <li><strong>Klare Fehlteil-Klassifizierung:</strong> Pro Item kann der
                        Lagermitarbeiter jetzt zwischen drei Zustaenden waehlen:
                        <em>kein Fehlteil</em>, <em>Fehlteil</em> (Restlieferung erwartet)
                        oder <em>Wird nicht nachgeliefert</em> (endgueltige Eskalation).
                        Die bisherige einzelne <code>IsFinalShortage</code>-Checkbox wird durch
                        zwei Radio-Buttons je Item ersetzt. Default bei Ist-Menge=0 ist
                        <em>Fehlteil</em>.</li>
                    <li><strong>Zwei Tabs in der Fehlteile-Liste:</strong> Lager- und Werkbank-View
                        zeigen jetzt einen <em>Offene Fehlteile</em>-Tab (Restlieferungs-Pipeline)
                        und einen <em>Wird nicht nachgeliefert</em>-Tab (Eskalationen). Beide
                        Tabs sind unabhaengig filter- und sortierbar.</li>
                    <li><strong>Werkbank-Karte zeigt beide Counts:</strong> Die "Meine Fehlteile"-Karte
                        in der Werkbank-Sicht zeigt beide Zahlen mit eigenen Tab-Links (z.B.
                        "3 Fehlteile (wird nachgeliefert)" + "1 Wird nicht nachgeliefert").</li>
                    <li><strong>Datenbank-Migration:</strong> Bestehende Bestellungen werden beim
                        Update automatisch konvertiert — alle PartiallyDelivered-Bestellungen
                        bleiben PartiallyDelivered, alle bisherigen endgueltigen Fehlteile bleiben
                        in der Eskalations-Liste sichtbar.</li>
                    <li><em>Hinweis fuer Admins:</em> Backup der DB vor Deploy empfohlen — die
                        Migration droppt die alte <code>IsFinalShortage</code>-Spalte nach
                        Daten-Konvertierung.</li>
                </ul>
            </div>
        </div>
```

- [ ] **Step 3: PROJECT_STATUS — v1.18.1-Block entfernen, v1.19.0-Block einfuegen**

In `PROJECT_STATUS.md`:

a) Such den bestehenden `### v1.18.1 — Hotfix MissingParts ...`-Block. Komplett entfernen (inkl. der Trennlinie `---` darunter falls vorhanden).

b) Direkt nach der `## Aktueller Fortschritt`-Zeile und VOR dem bestehenden `### v1.18.0`-Block einfuegen:

```markdown
### v1.19.0 — Lagerbestellungen: 3-State + 2-Tab Fehlteile

Hintergrund: v1.18.0 fuehrte einen einzelnen `IsFinalShortage`-Bool-Flag ein.
Beim Testen erkannte der User dass die Label-Semantik unklar war (Lagermitarbeiter
moechte explizit zwischen "Fehlteil wird nachgeliefert" und "wird nicht nachgeliefert"
unterscheiden koennen). v1.19.0 ersetzt das Bool durch ein `ShortageStatus`-Enum
(None / WillBeRestocked / NoRestock) mit 2 Radio-Buttons je Item und 2 Tabs in
der Fehlteile-Liste. Die im selben Branch begonnene v1.18.1-Filter-Erweiterung
(MissingParts inkludiert auch PartiallyDelivered) ist Teil von v1.19.0.

| # | Sub-Task | Status |
|---|---------|--------|
| 0 | Pre-Flight Baseline | ✅ erledigt |
| 1 | ShortageStatus-Enum + Property | ✅ erledigt |
| 2 | EF Migration + SQL/65 + FreshInstall | ✅ erledigt |
| 3 | ViewModels migrieren | ✅ erledigt |
| 4 | Repo CloseAsync + DeriveStatus + SaveProgressAsync (TDD) | ✅ erledigt |
| 5 | Repo GetMissingPartsAsync Tab + GetShortageCountsForUserAsync (TDD) | ✅ erledigt |
| 6 | WarehousePickingController migriert (int[] shortageStatuses) | ✅ erledigt |
| 7 | MissingPartsController Tab-Param + Werkbank 4 Counts | ✅ erledigt |
| 8 | Details.cshtml mit 2 Radios + 3-State-JS | ✅ erledigt |
| 9 | MissingParts/Index nav-tabs + Print + Werkbank-Karte | ✅ erledigt |
| 10 | Version + Changelog + PROJECT_STATUS + CLAUDE.md + TESTSZENARIEN | ✅ erledigt |
| 11 | Final-Check Build + Tests | ⏳ offen |
| 12 | Merge in main (NACH User-Bestaetigung) | ⏳ offen |

---
```

- [ ] **Step 4: CLAUDE.md — 1 raus, 3 rein**

In `CLAUDE.md` im Abschnitt `## Bekannte Fallstricke`:

a) Such den in v1.18.1 angelegten Fallstrick beginnend mit `- **MissingParts zeigt \`IsFinalShortage=true\` aus Closed UND PartiallyDelivered (seit v1.18.1)**:`. ENTFERNE die KOMPLETTE Zeile.

b) Such den in v1.18.0 angelegten Fallstrick beginnend mit `- **MissingParts zeigt nur \`IsFinalShortage=true\` UND Status=Closed (seit v1.18.0)**:`. Falls noch vorhanden, ebenfalls ENTFERNEN (er ist semantisch ueberholt).

c) Direkt vor `## Standard-Daten (Neuinstallation)` die drei neuen Fallstricke einfuegen:

```markdown
- **ShortageStatus-Enum statt IsFinalShortage-Bool (seit v1.19.0)**: `WarehouseRequisitionItem.IsFinalShortage` (bool) wurde durch `ShortageStatus` (Enum None=0/WillBeRestocked=1/NoRestock=2) ersetzt. Status-Ableitung: Order wird `PartiallyDelivered` wenn ein Item `WillBeRestocked` ist, sonst `Closed`. MissingParts-Liste filtert per Tab — `WillBeRestocked` = "Offene Fehlteile", `NoRestock` = "Wird nicht nachgeliefert". Werkbank-Karte zeigt beide Counts.
- **Migration v1.19.0 ist daten-destruktiv**: Die EF-Migration `ReplaceIsFinalShortageWithShortageStatus` (sowie das idempotente `SQL/65`) droppen die `IsFinalShortage`-Spalte NACH einer Daten-Konvertierung (`IsFinalShortage=true` → `NoRestock`; `false + Picked<Requested` → `WillBeRestocked`; sonst → `None`). Up() ist reversibel via Down(), aber Down() verliert die Unterscheidung zwischen None und WillBeRestocked (beide werden zu `IsFinalShortage=false`). **Backup der DB vor Produktions-Deploy** empfohlen.
- **Radio-3-State Pattern (Doppelklick → None)**: In `Details.cshtml` benutzen die ShortageStatus-Radios ein selbstgebautes 3-State-Verhalten — Doppelklick auf den aktiven Radio setzt ihn zurueck zu None. Implementiert via `mousedown`-Snapshot des `checked`-Status und `click`-Handler, der den Radio wieder unchecked setzt wenn er bereits aktiv war. Bootstrap-Radios unterstuetzen das per Default nicht.
```

- [ ] **Step 5: TESTSZENARIEN Kapitel 33**

Am Ende von `docs/TESTSZENARIEN.md` ergaenzen:

```markdown

---

## 33. ShortageStatus 3-State + 2-Tab Fehlteile (v1.19.0)

### 33.1 Default-Fehlteil bei Ist=0
**Vorbedingung:** Bestellung in Submitted mit 1 Item, Soll=5.
**Schritte:** Picking/Details oeffnen. Ist=0 eintragen.
**Erwartet:** Radio "Fehlteil" wird automatisch aktiv. Hidden-Input shortageStatuses hat Wert "1".

### 33.2 Manueller Wechsel auf "Wird nicht nachgeliefert"
**Vorbedingung:** Item hat "Fehlteil" aktiv.
**Schritte:** Klick auf "Wird nicht nachgeliefert"-Radio.
**Erwartet:** "Fehlteil" deaktiviert, "Wird nicht nachgeliefert" aktiv. Hidden hat Wert "2".

### 33.3 Doppelklick auf aktiven Radio → zurueck zu None
**Vorbedingung:** Item hat "Fehlteil" aktiv.
**Schritte:** Klick erneut auf "Fehlteil".
**Erwartet:** Beide Radios deaktiviert. Hidden hat Wert "0".

### 33.4 Ist=Soll → beide Radios disabled
**Vorbedingung:** Item mit Soll=5.
**Schritte:** Ist=5 eintragen.
**Erwartet:** Beide Radios disabled (grayed out), kein Klick moeglich. Hidden hat Wert "0".

### 33.5 Bestellung mit allen "Fehlteil" → PartiallyDelivered
**Vorbedingung:** 2 Items.
**Schritte:** Beide Ist<Soll, beide Radios auf "Fehlteil". "Speichern + Abschliessen".
**Erwartet:** Status PartiallyDelivered. Bestellung bleibt im Picking/Index.

### 33.6 Bestellung mit allen "Wird nicht nachgeliefert" → Closed
**Schritte:** Beide Ist<Soll, beide Radios auf "Wird nicht nachgeliefert". Abschliessen.
**Erwartet:** Status Closed. Beide Items in /MissingParts Tab "Wird nicht nachgeliefert".

### 33.7 Tab "Offene Fehlteile" zeigt nur WillBeRestocked
**Vorbedingung:** Mischung aus WillBeRestocked und NoRestock Items in der DB.
**Schritte:** /MissingParts oeffnen (Default-Tab = Offene Fehlteile).
**Erwartet:** Nur Items mit ShortageStatus=WillBeRestocked sichtbar. Tab-Badge zeigt korrekte Count.

### 33.8 Tab "Wird nicht nachgeliefert" zeigt nur NoRestock
**Schritte:** /MissingParts?tab=NoRestock oeffnen.
**Erwartet:** Nur Items mit ShortageStatus=NoRestock sichtbar. Tab-Badge zeigt Count.

### 33.9 Werkbank-Karte zeigt beide Counts mit Tab-Links
**Vorbedingung:** User hat eigene Items: 2 WillBeRestocked aus 1 Bestellung, 1 NoRestock aus 1 Bestellung.
**Schritte:** Werkbank-Index (WarehouseRequisitions/Index) oeffnen.
**Erwartet:** Karte "Meine Fehlteile" zeigt 2 Zeilen:
  - "2 Fehlteile (wird nachgeliefert) aus 1 Bestellungen" (orange Link auf /MissingParts?tab=WillBeRestocked&mineOnly=true)
  - "1 Wird nicht nachgeliefert aus 1 Bestellungen" (rot Link auf /MissingParts?tab=NoRestock&mineOnly=true)

### 33.10 Migration: vorhandene v1.18.x PartiallyDelivered-Bestellung bleibt PD nach v1.19.0-Migration
**Vorbedingung:** DB-Snapshot von vor dem Deploy mit mindestens einer PartiallyDelivered-Bestellung.
**Schritte:**
1. v1.19.0 deployen (DB-Backup vorher).
2. Nach Migration-Run die Bestellung in der DB pruefen: Status weiterhin PartiallyDelivered?
3. Items pruefen: alle vorher IsFinalShortage=false mit Ist<Soll haben jetzt ShortageStatus=1 (WillBeRestocked)? Alle IsFinalShortage=true haben jetzt ShortageStatus=2 (NoRestock)?
**Erwartet:** Status bleibt PartiallyDelivered. Items haben korrekte ShortageStatus-Werte. Lager kann die Bestellung erneut oeffnen und editieren wie gewohnt.
```

- [ ] **Step 6: Build + Tests**

```
dotnet build IdealAkeWms.slnx
dotnet test IdealAkeWms.slnx --no-build
```

Expected: build success, alle Tests gruen.

- [ ] **Step 7: Commit**

```
git add IdealAkeWms/AppVersion.cs IDEALAKEWMSService/AppVersion.cs IdealAkeWms/Views/Help/Changelog.cshtml PROJECT_STATUS.md CLAUDE.md docs/TESTSZENARIEN.md
git commit -m "docs+version: v1.19.0 changelog, project status, claude, testszenarien

Web + Service AppVersion 1.19.0. v1.18.1-Card im Changelog entfernt
(war nie produktiv), v1.19.0-Card prependet. PROJECT_STATUS v1.18.1-Block
ersetzt durch v1.19.0-Block. CLAUDE.md: 2 v1.18.x-Fallstricke entfernt,
3 v1.19.0-Fallstricke ergaenzt. TESTSZENARIEN Kap 33 mit 10 Szenarien.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 11: Final-Check Build + Tests

**Files:** keine Aenderungen

- [ ] **Step 1: Vollstaendiger Build**

```
dotnet build IdealAkeWms.slnx
```

Expected: `Build succeeded. 0 Error(s)`. Warnungen wie Baseline ok.

- [ ] **Step 2: Volle Test-Suite**

```
dotnet test IdealAkeWms.slnx --no-build
```

Expected: Web ~627-628 passed + 1 skipped (Baseline 619 + ~9 neue netto), Service 99 passed.

- [ ] **Step 3: Versions-Sanity**

```
grep "1.19.0" IdealAkeWms/AppVersion.cs IDEALAKEWMSService/AppVersion.cs
grep "v1.19.0" IdealAkeWms/Views/Help/Changelog.cshtml PROJECT_STATUS.md
```

Expected: alle 4 enthalten `1.19.0`.

- [ ] **Step 4: IsFinalShortage-Restspuren**

```
grep -rn "IsFinalShortage\|isFinalShortage" --include="*.cs" --include="*.cshtml" IdealAkeWms IdealAkeWms.Tests IDEALAKEWMSService | grep -v "/Migrations/" | grep -v "DEPRECATED"
```

Expected: 0 Treffer (nur Migration-Datei darf noch Referenzen haben). Falls Treffer: gezielt fixen.

- [ ] **Step 5: ShortageStatus-Sanity**

```
grep -c "ShortageStatus" IdealAkeWms/Data/Repositories/WarehouseRequisitionRepository.cs
```

Expected: mind. 10 (Methoden-Signaturen + DeriveStatus + Filter + Mapping).

- [ ] **Step 6: v1.18.1-Spuren entfernt**

```
grep "v1.18.1" IdealAkeWms/Views/Help/Changelog.cshtml PROJECT_STATUS.md CLAUDE.md
```

Expected: 0 Treffer (alle v1.18.1-Eintraege wurden durch v1.19.0 ersetzt).

- [ ] **Step 7: Working-Tree clean**

```
git status
git log --oneline 5cc204a..HEAD
```

Expected: clean. ~14-16 neue Commits seit `5cc204a` (vor v1.18.1-Arbeiten).

---

## Task 12: Merge in main (NACH User-Bestaetigung)

**WICHTIG:** Diese Task NICHT automatisch ausfuehren. Per Memory-Feedback (`feedback_worktree_cleanup_ask_first`): Vor Merge + Cleanup explizit User-Bestaetigung einholen.

Nach Task 11 stoppen und melden:
> "v1.19.0 ist fertig auf Branch `bugfix/missingparts-include-pd`. Build + Tests gruen. Merge in main + Push? Worktree danach loeschen, ja/nein?"

Erst nach explizitem Go:

- [ ] **Step 1: Auf main wechseln**

```
git -C C:/Git/IDEAL-AKE-WMS checkout main
git -C C:/Git/IDEAL-AKE-WMS pull origin main
```

- [ ] **Step 2: Merge --no-ff**

```
git -C C:/Git/IDEAL-AKE-WMS merge --no-ff bugfix/missingparts-include-pd -m "merge bugfix/missingparts-include-pd into main (v1.19.0)"
```

- [ ] **Step 3: Build + Tests auf main**

```
dotnet build C:/Git/IDEAL-AKE-WMS/IdealAkeWms.slnx
dotnet test C:/Git/IDEAL-AKE-WMS/IdealAkeWms.slnx --no-build
```

Expected: gruen.

- [ ] **Step 4: Push**

```
git -C C:/Git/IDEAL-AKE-WMS push origin main
```

- [ ] **Step 5: Worktree + Branch entfernen (NUR wenn User in vorheriger Bestaetigung 'ja' zu Cleanup sagte)**

```
git -C C:/Git/IDEAL-AKE-WMS worktree remove .claude/worktrees/missingparts-include-pd
git -C C:/Git/IDEAL-AKE-WMS branch -d bugfix/missingparts-include-pd
```

Falls Windows-File-Lock: nach `dotnet build-server shutdown` retry. Falls weiterhin gelockt: melden, nicht erzwingen — User kann manuell entfernen.

---

## Final-Review-Subagent (nach Task 12)

Code-Reviewer-Subagent mit Diff-Range `5cc204a..HEAD` (alle v1.19.0-Commits inkl. v1.18.1-Vorarbeit + Merge). Pruefkriterien:

1. **Migration**: ShortageStatus TINYINT mit Daten-Konvertierungs-SQL? IsFinalShortage komplett gedroppt (Up + SQL/65 + FreshInstall)? Down() rekonstruiert?
2. **00_FreshInstall.sql**: Schema enthaelt nur ShortageStatus, beide Filtered Indizes, MigrationsHistory hat beide IDs (64 + 65)?
3. **DeriveStatus-Logik**: `(isFullyDelivered || !hasWaitingRestock)` korrekt? Pruefen mit Status-Matrix aus Spec §5.
4. **GetMissingPartsAsync**: filter `ShortageStatus == filterStatus && (Closed || PartiallyDelivered)`?
5. **GetShortageCountsForUserAsync**: 4-Tuple, beide Status, nur User-Workplaces?
6. **WarehousePickingController**: Form-Binding `int[] shortageStatuses` korrekt auf Dict<int, ShortageStatus> gemapped (switch-Expression)?
7. **MissingPartsController**: Default-Tab WillBeRestocked, None-Normalisierung, beide Counts geladen?
8. **WarehouseRequisitionsController.Index**: 4 Counts statt 2, ViewModel-Properties richtig benannt?
9. **Details.cshtml**: 2 Radios je Item, Hidden-Sync, 3-State-JS (Doppelklick → None), Default bei Ist=0 = WillBeRestocked?
10. **MissingParts/Index**: nav-tabs mit beiden Tabs, Tab-Counts, hidden tab-Param im Filter-Form (preserves Tab bei Workplace-Wechsel)?
11. **Print.cshtml**: ShortageStatus-Text statt ✓?
12. **WarehouseRequisitions/Index**: Karte mit 2 Zeilen, beide Tab-Links?
13. **AppVersion**: Web + Service beide auf 1.19.0?
14. **Changelog**: v1.18.1-Card entfernt, v1.19.0-Card prependet, v1.18.0-Card unveraendert?
15. **CLAUDE.md**: alte v1.18.x-Fallstricke entfernt, 3 neue v1.19.0-Fallstricke ergaenzt?
16. **TESTSZENARIEN**: Kapitel 33 mit 10 Szenarien?
17. **Test-Counts**: Web ~627-628 + 1 skip, Service 99?
18. **Out-of-Scope**: nur erwartete Files veraendert?
