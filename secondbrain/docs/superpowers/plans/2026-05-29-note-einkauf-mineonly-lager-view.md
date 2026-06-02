# NoteEinkauf + MissingParts mineOnly-Default + MissingPartsLager-View — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** v1.19.0-Branch um drei Aspekte erweitern: zweites Notiz-Feld `NoteEinkauf` auf `WarehouseRequisitionItem`, default `mineOnly=true` in `MissingPartsController` mit No-Workplace-Banner, neuer `MissingPartsLagerController` + View ohne `mineOnly`.

**Architecture:** Additive Migration (NVARCHAR 500 Spalte), Repository-Sig-Erweiterung um `itemNotesEinkauf`-Dict in `CloseAsync`/`SaveProgressAsync`, `MissingPartRow` um `NoteEinkauf` erweitert, Controller-Default-Aenderung + neuer Klon-Controller, UI-Label-Aenderungen in 4 Views + Layout-Menue.

**Tech Stack:** ASP.NET Core 10, EF Core 10, SQL Server, Razor, Bootstrap 5, xUnit + FluentAssertions

**Worktree:** `.claude/worktrees/missingparts-include-pd`, Branch `bugfix/missingparts-include-pd`

**Spec:** [secondbrain/docs/superpowers/specs/2026-05-29-note-einkauf-mineonly-lager-view-design.md](../specs/2026-05-29-note-einkauf-mineonly-lager-view-design.md) (Commit 9cdc4cc)

**Vorgaenger-Branch-Stand:** 631 Web (+1 skip) + 99 Service Tests alle gruen. Letzter Commit: `5232b44 fix(view): Radios mutually exclusive + Soll=Ist-Modal respektiert ShortageStatus`. Bestehende v1.19.0-Doku (Changelog-Card, PROJECT_STATUS-Block, CLAUDE.md-Fallstricke) ist bereits drin und wird inkrementell erweitert.

---

## File Structure

**Create:**
- `IdealAkeWms/Migrations/<TIMESTAMP>_AddNoteEinkaufToWarehouseRequisitionItems.cs` (manuell, EF-Tool laeuft nicht auf broken-Build-Pfaden)
- `SQL/66_AddNoteEinkaufToWarehouseRequisitionItems.sql`
- `IdealAkeWms/Controllers/MissingPartsLagerController.cs`
- `IdealAkeWms/Views/MissingPartsLager/Index.cshtml`
- `IdealAkeWms.Tests/Controllers/MissingPartsLagerControllerTests.cs`

**Modify:**
- `IdealAkeWms/Models/WarehouseRequisitionItem.cs` — neue `NoteEinkauf`-Property + XML-Doc-Update fuer `Note`
- `IdealAkeWms/Data/Repositories/IWarehouseRequisitionRepository.cs` — `CloseAsync` + `SaveProgressAsync` Sig
- `IdealAkeWms/Data/Repositories/WarehouseRequisitionRepository.cs` — Impl + `GetMissingPartsAsync` + Column-Filter
- `IdealAkeWms/Models/ViewModels/WarehouseRequisitionDetailViewModel.cs` — `NoteEinkauf`-Param
- `IdealAkeWms/Models/ViewModels/MissingPartRow.cs` — `NoteEinkauf`-Param
- `IdealAkeWms/Models/ViewModels/MissingPartsListViewModel.cs` — `HasNoWorkplaceMapping`-Flag
- `IdealAkeWms/Controllers/WarehousePickingController.cs` — Close/SaveProgress/PrintAndClose + Details/Print-Mapping
- `IdealAkeWms/Controllers/MissingPartsController.cs` — Default `mineOnly=true` + `HasNoWorkplaceMapping`
- `IdealAkeWms/Views/WarehousePicking/Details.cshtml` — Spalte umbenennen + Notiz-EK-Input + JS
- `IdealAkeWms/Views/WarehousePicking/Print.cshtml` — Header + neue Spalte
- `IdealAkeWms/Views/MissingParts/Index.cshtml` — 2 Notiz-Spalten + Banner
- `IdealAkeWms/Views/Shared/_Layout.cshtml` — Menue-Eintrag-Aenderung + neuer Eintrag
- `SQL/00_FreshInstall.sql` — Schema + MigrationsHistory
- `IdealAkeWms.Tests/Repositories/WarehouseRequisitionRepositoryTests.cs` — bestehende Tests + 4 neue
- `IdealAkeWms.Tests/Controllers/WarehousePickingControllerTests.cs` — bestehende Tests + 1 neuer
- `IdealAkeWms.Tests/Controllers/MissingPartsControllerTests.cs` — bestehende Tests + 2 neue
- `IdealAkeWms/Views/Help/Changelog.cshtml` — bestehende v1.19.0-Card um 2 Bullets erweitern
- `PROJECT_STATUS.md` — bestehende Sub-Task-Tabelle erweitern
- `CLAUDE.md` — 2 neue Fallstricke
- `docs/TESTSZENARIEN.md` — Kapitel 33 um 4 Szenarien (33.11–33.14)

---

## Task 0: Pre-Flight Baseline

**Files:** keine Aenderungen

- [ ] **Step 1: Branch + Commits verifizieren**

```
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd rev-parse --abbrev-ref HEAD
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd log --oneline -3
```

Expected: Branch `bugfix/missingparts-include-pd`. Letzte 3 Commits: `9cdc4cc docs(spec): v1.19.0 NoteEinkauf ...`, `5232b44 fix(view): Radios ...`, `de536c8 docs+version: v1.19.0 ...`.

- [ ] **Step 2: Baseline-Build**

```
dotnet build C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd/IdealAkeWms.slnx
```

Expected: `Build succeeded. 0 Error(s)`. NU1902/CS8602 ok.

- [ ] **Step 3: Baseline-Tests**

```
dotnet test C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd/IdealAkeWms.slnx --no-build
```

Expected: Web 631 passed + 1 skipped, Service 99 passed.

- [ ] **Step 4: Migration-Counter pruefen**

```
ls C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd/SQL/*.sql | sort | tail -3
```

Expected: `65_ReplaceIsFinalShortageWithShortageStatus.sql` ist letzte. Naechste = **66**.

---

## Task 1: NoteEinkauf Property + Migration + FreshInstall

**Files:**
- Modify: `IdealAkeWms/Models/WarehouseRequisitionItem.cs`
- Create: `IdealAkeWms/Migrations/<TIMESTAMP>_AddNoteEinkaufToWarehouseRequisitionItems.cs`
- Create: `SQL/66_AddNoteEinkaufToWarehouseRequisitionItems.sql`
- Modify: `SQL/00_FreshInstall.sql`

- [ ] **Step 1: WarehouseRequisitionItem.cs — XML-Doc + neue Property**

In `IdealAkeWms/Models/WarehouseRequisitionItem.cs` such die bestehende `Note`-Property mit XML-Doc und ersetze das gesamte Property-Block (Doc + Property) durch:

```csharp
    /// <summary>
    /// Notiz vom Lagermitarbeiter zur Position (UI-Label "Notiz Lager" seit v1.19.0).
    /// Wird auf dem Druck angezeigt.
    /// </summary>
    [StringLength(500)]
    public string? Note { get; set; }

    /// <summary>
    /// Notiz fuer den Einkaeufer (z.B. Lieferanten-Hinweis bei endgueltigem Fehlteil).
    /// Wird im Picking/Details vom Lagermitarbeiter gefuellt. Werkbank-Edit nicht beeinflusst.
    /// </summary>
    [StringLength(500)]
    public string? NoteEinkauf { get; set; }
```

- [ ] **Step 2: EF Migration manuell anlegen**

Naechste verfuegbare Timestamp ermitteln (UTC-jetzt im Format `yyyyMMddHHmmss`). Im weiteren Plan-Text als `<TS>` referenziert.

Neue Datei `IdealAkeWms/Migrations/<TS>_AddNoteEinkaufToWarehouseRequisitionItems.cs`:

```csharp
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    public partial class AddNoteEinkaufToWarehouseRequisitionItems : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NoteEinkauf",
                table: "WarehouseRequisitionItems",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NoteEinkauf",
                table: "WarehouseRequisitionItems");
        }
    }
}
```

Plus Designer.cs analog zur Vorgaenger-Migration `20260529101707_ReplaceIsFinalShortageWithShortageStatus.Designer.cs` — kopieren, anpassen:
- Klassen-Name: `AddNoteEinkaufToWarehouseRequisitionItemsDesigner` → eigentlich `AddNoteEinkaufToWarehouseRequisitionItems` (Designer ist part of partial)
- MigrationId-Attribut: `[Migration("<TS>_AddNoteEinkaufToWarehouseRequisitionItems")]`
- Snapshot-Body: identisch zur Vorgaenger-Designer, aber in der Property-Liste von `WarehouseRequisitionItem` ein `NoteEinkauf`-Eintrag ergaenzen:
  ```csharp
  b.Property<string>("NoteEinkauf")
      .HasMaxLength(500)
      .HasColumnType("nvarchar(500)");
  ```

Auch `ApplicationDbContextModelSnapshot.cs` aktualisieren — gleiche `NoteEinkauf`-Property-Eintragung in der WarehouseRequisitionItem-Section.

**Praktisch:** statt manuelles Designer-Kopieren — falls EF-Tool unter dem broken-Build doch laeuft, einfach:
```
dotnet ef migrations add AddNoteEinkaufToWarehouseRequisitionItems --project IdealAkeWms --startup-project IdealAkeWms
```
und nur Up/Down checken. Falls nicht: manueller Pfad oben.

- [ ] **Step 3: SQL/66 idempotent**

Neue Datei `SQL/66_AddNoteEinkaufToWarehouseRequisitionItems.sql`:

```sql
-- =============================================
-- 66_AddNoteEinkaufToWarehouseRequisitionItems
-- Adds NoteEinkauf NVARCHAR(500) NULL — separate Notiz fuer Einkauf
-- (semantische Trennung zu Note = "Notiz Lager").
-- Used by v1.19.0.
-- =============================================

IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE Name = N'NoteEinkauf'
               AND Object_ID = Object_ID(N'[dbo].[WarehouseRequisitionItems]'))
BEGIN
    ALTER TABLE [dbo].[WarehouseRequisitionItems]
        ADD [NoteEinkauf] NVARCHAR(500) NULL;
END
GO
```

- [ ] **Step 4: 00_FreshInstall.sql sync**

In `SQL/00_FreshInstall.sql`:

a) Tabellen-Definition `CREATE TABLE [dbo].[WarehouseRequisitionItems]` finden. Such die `[Note] NVARCHAR(500) NULL,`-Zeile. Direkt darunter (oder direkt vor `[ShortageStatus] TINYINT NOT NULL ...`) einfuegen:

```sql
    [NoteEinkauf] NVARCHAR(500) NULL,
```

b) `__EFMigrationsHistory`-Block ganz am Ende. Nach dem Eintrag fuer `20260529101707_ReplaceIsFinalShortageWithShortageStatus` neuen Eintrag mit dem exakten `<TS>` aus Step 2 ergaenzen:

```sql
    (N'<TS>_AddNoteEinkaufToWarehouseRequisitionItems', N'10.0.2'),
```

Achte auf Komma/Semikolon je nachdem ob es der letzte Eintrag ist.

- [ ] **Step 5: Build verifizieren**

```
dotnet build C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd/IdealAkeWms.slnx
```

Expected: build success. Falls `PendingModelChangesWarning`: Snapshot-Update unvollstaendig — Step 2 c) nachziehen.

- [ ] **Step 6: Commit**

```
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd add IdealAkeWms/Models/WarehouseRequisitionItem.cs IdealAkeWms/Migrations/ SQL/66_AddNoteEinkaufToWarehouseRequisitionItems.sql SQL/00_FreshInstall.sql
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd commit -m "feat(db): add NoteEinkauf column to WarehouseRequisitionItems

EF Migration + idempotent SQL/66. Note bleibt im Code als 'Notiz Lager'.
NoteEinkauf NVARCHAR(500) NULL fuer separate Einkaufs-Notiz.
FreshInstall konsolidiert (Schema + neuer MigrationsHistory-Eintrag).

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 2: ViewModels erweitern

**Files:**
- Modify: `IdealAkeWms/Models/ViewModels/WarehouseRequisitionDetailViewModel.cs`
- Modify: `IdealAkeWms/Models/ViewModels/MissingPartRow.cs`
- Modify: `IdealAkeWms/Models/ViewModels/MissingPartsListViewModel.cs`

- [ ] **Step 1: WarehouseRequisitionDetailItemViewModel**

In `IdealAkeWms/Models/ViewModels/WarehouseRequisitionDetailViewModel.cs` den `record` aktualisieren — neuen Param `NoteEinkauf` am Ende anhaengen:

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
    ShortageStatus ShortageStatus = ShortageStatus.None,
    string? NoteEinkauf = null);
```

- [ ] **Step 2: MissingPartRow**

In `IdealAkeWms/Models/ViewModels/MissingPartRow.cs`:

```csharp
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
    ShortageStatus Status,
    string? NoteEinkauf);
```

- [ ] **Step 3: MissingPartsListViewModel**

In `IdealAkeWms/Models/ViewModels/MissingPartsListViewModel.cs` am Ende der Klasse (vor schliessender `}`):

```csharp
    /// <summary>
    /// True wenn mineOnly aktiv UND User hat keine Workplace-Zuordnung.
    /// Triggert Banner-Hinweis in der View.
    /// </summary>
    public bool HasNoWorkplaceMapping { get; set; }
```

- [ ] **Step 4: Build + Commit**

```
dotnet build C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd/IdealAkeWms.slnx
```

Expected: weiterhin Build-Fehler im Repository/Controller (alte Sigs), aber ViewModels selbst kompilieren.

```
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd add IdealAkeWms/Models/ViewModels/
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd commit -m "feat(viewmodel): NoteEinkauf in DetailItem + MissingPartRow, HasNoWorkplaceMapping flag

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 3: Repository CloseAsync + SaveProgressAsync + Tests (TDD)

**Files:**
- Modify: `IdealAkeWms/Data/Repositories/IWarehouseRequisitionRepository.cs`
- Modify: `IdealAkeWms/Data/Repositories/WarehouseRequisitionRepository.cs`
- Modify: `IdealAkeWms.Tests/Repositories/WarehouseRequisitionRepositoryTests.cs`

- [ ] **Step 1: Tests fuer NoteEinkauf-Persistierung schreiben**

In `IdealAkeWms.Tests/Repositories/WarehouseRequisitionRepositoryTests.cs` am Ende der Klasse anfuegen:

```csharp
    [Fact]
    public async Task CloseAsync_PersistsNoteEinkauf()
    {
        using var db = TestDbContextFactory.Create();
        var repo = new WarehouseRequisitionRepository(db);
        var id = await SeedRequisitionAsync(db, (10, null, ShortageStatus.None));
        var items = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).ToList();

        await repo.CloseAsync(id,
            new Dictionary<int, decimal> { [items[0].Id] = 10m },
            new Dictionary<int, string?> { [items[0].Id] = "lager note" },
            new Dictionary<int, string?> { [items[0].Id] = "ek note" },
            new Dictionary<int, ShortageStatus> { [items[0].Id] = ShortageStatus.None },
            1, "u", "w", new byte[0]);

        var item = await db.WarehouseRequisitionItems.FindAsync(items[0].Id);
        item!.Note.Should().Be("lager note");
        item.NoteEinkauf.Should().Be("ek note");
    }

    [Fact]
    public async Task SaveProgressAsync_PersistsNoteEinkauf_WithoutStatusChange()
    {
        using var db = TestDbContextFactory.Create();
        var repo = new WarehouseRequisitionRepository(db);
        var id = await SeedRequisitionAsync(db, (10, null, ShortageStatus.None));
        var items = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).ToList();

        await repo.SaveProgressAsync(id,
            new Dictionary<int, decimal?>(),
            new Dictionary<int, string?>(),
            new Dictionary<int, string?> { [items[0].Id] = "  ek wert  " },
            new Dictionary<int, ShortageStatus>(),
            "u", "w");

        var item = await db.WarehouseRequisitionItems.FindAsync(items[0].Id);
        item!.NoteEinkauf.Should().Be("ek wert");
        var r = await db.WarehouseRequisitions.FindAsync(id);
        r!.Status.Should().Be(WarehouseRequisitionStatus.Submitted);
    }
```

- [ ] **Step 2: Tests laufen — FAIL erwarten**

```
dotnet test C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd/IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "FullyQualifiedName~CloseAsync_PersistsNoteEinkauf|FullyQualifiedName~SaveProgressAsync_PersistsNoteEinkauf"
```

Expected: Build-Fail mit "No overload for method ... takes 9 arguments" bzw. "8 arguments" — die neuen Aufrufe nutzen 1 Extra-Dict-Param.

- [ ] **Step 3: Interface anpassen**

In `IdealAkeWms/Data/Repositories/IWarehouseRequisitionRepository.cs` such `Task CloseAsync(`. Ersetz die Sig durch:

```csharp
    Task CloseAsync(int id,
        IReadOnlyDictionary<int, decimal> itemQuantitiesPicked,
        IReadOnlyDictionary<int, string?> itemNotes,
        IReadOnlyDictionary<int, string?> itemNotesEinkauf,
        IReadOnlyDictionary<int, ShortageStatus> itemShortageStatuses,
        int closedByUserId, string user, string winUser, byte[] rowVersion);
```

Und `Task SaveProgressAsync(`:

```csharp
    Task SaveProgressAsync(int id,
        IReadOnlyDictionary<int, decimal?> itemQuantitiesPicked,
        IReadOnlyDictionary<int, string?> itemNotes,
        IReadOnlyDictionary<int, string?> itemNotesEinkauf,
        IReadOnlyDictionary<int, ShortageStatus> itemShortageStatuses,
        string user, string winUser);
```

- [ ] **Step 4: Repository-Impl — CloseAsync**

In `IdealAkeWms/Data/Repositories/WarehouseRequisitionRepository.cs` such die `CloseAsync`-Methode. Ersetz komplett durch:

```csharp
    public async Task CloseAsync(int id, IReadOnlyDictionary<int, decimal> itemQuantitiesPicked,
        IReadOnlyDictionary<int, string?> itemNotes,
        IReadOnlyDictionary<int, string?> itemNotesEinkauf,
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
            if (itemNotesEinkauf.TryGetValue(item.Id, out var noteEk))
                item.NoteEinkauf = string.IsNullOrWhiteSpace(noteEk) ? null : noteEk.Trim();
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

- [ ] **Step 5: Repository-Impl — SaveProgressAsync**

Such die `SaveProgressAsync`-Methode. Ersetz komplett durch:

```csharp
    public async Task SaveProgressAsync(int id,
        IReadOnlyDictionary<int, decimal?> itemQuantitiesPicked,
        IReadOnlyDictionary<int, string?> itemNotes,
        IReadOnlyDictionary<int, string?> itemNotesEinkauf,
        IReadOnlyDictionary<int, ShortageStatus> itemShortageStatuses,
        string user, string winUser)
    {
        var allKeys = itemQuantitiesPicked.Keys
            .Concat(itemNotes.Keys)
            .Concat(itemNotesEinkauf.Keys)
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
            if (itemNotesEinkauf.TryGetValue(row.Id, out var noteEk))
            {
                var normalized = string.IsNullOrWhiteSpace(noteEk) ? null : noteEk.Trim();
                if (row.NoteEinkauf != normalized)
                {
                    row.NoteEinkauf = normalized;
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

- [ ] **Step 6: Bestehende Repository-Tests migrieren**

Pro Aufruf von `repo.CloseAsync(...)` oder `repo.SaveProgressAsync(...)` in den bestehenden Tests ein neues Dict `new Dictionary<int, string?>()` als `itemNotesEinkauf`-Param zwischen `itemNotes` (3. Param) und `itemShortageStatuses` (jetzt 5. Param) einfuegen.

Mechanisch fuer jeden Test:
```csharp
// Alt:
await repo.CloseAsync(id, qty, notes, statuses, 1, "u", "w", new byte[0]);
// Neu:
await repo.CloseAsync(id, qty, notes, new Dictionary<int, string?>(), statuses, 1, "u", "w", new byte[0]);

// Alt:
await repo.SaveProgressAsync(id, qtyDict, noteDict, statusDict, "u", "w");
// Neu:
await repo.SaveProgressAsync(id, qtyDict, noteDict, new Dictionary<int, string?>(), statusDict, "u", "w");
```

Etwa 20-30 Aufrufe in der Datei — alle durchgehen.

- [ ] **Step 7: WarehousePickingController temporaer fixen**

In `IdealAkeWms/Controllers/WarehousePickingController.cs` such `_repo.CloseAsync(` und `_repo.SaveProgressAsync(`. Bei beiden Aufrufen (in Close, SaveProgress, PrintAndClose-Actions) zwischen `noteDict` (3. Param) und `statusDict` (4. Param) ein leeres `new Dictionary<int, string?>()` einfuegen. Beispiel:

```csharp
await _repo.CloseAsync(id, qtyDict, noteDict,
    new Dictionary<int, string?>(),   // temporaer leer, Task 5 fixt das
    statusDict,
    _user.GetCurrentAppUserId() ?? 0,
    _user.GetDisplayName(), _user.GetWindowsUserName(), rowVersion);
```

In SaveProgress + PrintAndClose analog.

- [ ] **Step 8: Build + Tests**

```
dotnet build C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd/IdealAkeWms.slnx
dotnet test C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd/IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "FullyQualifiedName~WarehouseRequisitionRepositoryTests" --no-build
```

Expected: alle Repo-Tests passing inkl. die 2 neuen. Controller-Tests koennten noch fehlschlagen — Task 5 fixt.

- [ ] **Step 9: Commit**

```
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd add IdealAkeWms/Data/Repositories/IWarehouseRequisitionRepository.cs IdealAkeWms/Data/Repositories/WarehouseRequisitionRepository.cs IdealAkeWms/Controllers/WarehousePickingController.cs IdealAkeWms.Tests/Repositories/WarehouseRequisitionRepositoryTests.cs
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd commit -m "feat(repo): CloseAsync + SaveProgressAsync persist NoteEinkauf

Neuer Param itemNotesEinkauf zwischen itemNotes und itemShortageStatuses.
Reihenfolge analog zu itemNotes. 2 neue Tests. Bestehende Tests mit leerem
Dict erweitert. Controller-Aufrufe vorerst mit leerem Dict (Task 5 fixt).

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 4: Repository GetMissingPartsAsync + Column-Filter (TDD)

**Files:**
- Modify: `IdealAkeWms/Data/Repositories/WarehouseRequisitionRepository.cs`
- Modify: `IdealAkeWms.Tests/Repositories/WarehouseRequisitionRepositoryTests.cs`

- [ ] **Step 1: 2 neue Tests schreiben**

In `IdealAkeWms.Tests/Repositories/WarehouseRequisitionRepositoryTests.cs` am Ende:

```csharp
    [Fact]
    public async Task GetMissingPartsAsync_FiltersByNoteLager_Column()
    {
        using var db = TestDbContextFactory.Create();
        db.ProductionWorkplaces.Add(new ProductionWorkplace { Id = 1, Name = "WB1" });
        await db.SaveChangesAsync();
        var repo = new WarehouseRequisitionRepository(db);
        var id = await SeedRequisitionAsync(db, (10, 0m, ShortageStatus.NoRestock), (5, 0m, ShortageStatus.NoRestock));
        var items = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).OrderBy(i => i.Position).ToList();
        items[0].Note = "lager-hinweis";
        items[1].Note = "andere notiz";
        await db.SaveChangesAsync();
        await repo.CloseAsync(id,
            new Dictionary<int, decimal> { [items[0].Id] = 0m, [items[1].Id] = 0m },
            new Dictionary<int, string?>(),
            new Dictionary<int, string?>(),
            new Dictionary<int, ShortageStatus> { [items[0].Id] = ShortageStatus.NoRestock, [items[1].Id] = ShortageStatus.NoRestock },
            1, "u", "w", new byte[0]);

        var filters = new Dictionary<string, string> { ["NoteLager"] = "lager" };
        var (result, _) = await repo.GetMissingPartsAsync(ShortageStatus.NoRestock, null, filters, null, null, 1, 100);
        result.Should().HaveCount(1);
        result[0].ItemId.Should().Be(items[0].Id);
    }

    [Fact]
    public async Task GetMissingPartsAsync_FiltersByNoteEinkauf_Column()
    {
        using var db = TestDbContextFactory.Create();
        db.ProductionWorkplaces.Add(new ProductionWorkplace { Id = 1, Name = "WB1" });
        await db.SaveChangesAsync();
        var repo = new WarehouseRequisitionRepository(db);
        var id = await SeedRequisitionAsync(db, (10, 0m, ShortageStatus.NoRestock), (5, 0m, ShortageStatus.NoRestock));
        var items = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).OrderBy(i => i.Position).ToList();
        await repo.CloseAsync(id,
            new Dictionary<int, decimal> { [items[0].Id] = 0m, [items[1].Id] = 0m },
            new Dictionary<int, string?>(),
            new Dictionary<int, string?> { [items[0].Id] = "ek-hinweis", [items[1].Id] = "egal" },
            new Dictionary<int, ShortageStatus> { [items[0].Id] = ShortageStatus.NoRestock, [items[1].Id] = ShortageStatus.NoRestock },
            1, "u", "w", new byte[0]);

        var filters = new Dictionary<string, string> { ["NoteEinkauf"] = "ek-hinweis" };
        var (result, _) = await repo.GetMissingPartsAsync(ShortageStatus.NoRestock, null, filters, null, null, 1, 100);
        result.Should().HaveCount(1);
        result[0].ItemId.Should().Be(items[0].Id);
        result[0].NoteEinkauf.Should().Be("ek-hinweis");
    }
```

- [ ] **Step 2: FAIL erwarten**

```
dotnet test C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd/IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "FullyQualifiedName~GetMissingPartsAsync_FiltersByNote"
```

Expected: Build-Fail (`MissingPartRow` has no `NoteEinkauf`-Param-Setter — wait, das ist in Task 2 schon gefixt → eigentlich passing nach Mapping-Update; aber Filter NoteEinkauf existiert noch nicht → 0 Items returned wo 1 erwartet).

- [ ] **Step 3: GetMissingPartsAsync — MissingPartRow-Mapping erweitern**

In `WarehouseRequisitionRepository.cs` such die `Select(i => new MissingPartRow(...))`-Stelle in `GetMissingPartsAsync`. Am Ende der Parameter-Liste `i.NoteEinkauf` ergaenzen:

```csharp
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
                i.ShortageStatus,
                i.NoteEinkauf))
```

- [ ] **Step 4: Column-Filter erweitern**

Such den `columnFilters`-Block in `GetMissingPartsAsync`. Ersetz alle `if (columnFilters.TryGetValue(...))`-Stellen durch:

```csharp
        if (columnFilters != null)
        {
            if (columnFilters.TryGetValue("ArticleNumber", out var an) && !string.IsNullOrWhiteSpace(an))
                q = ApplyMissingPartsTextFilter(q, an, isArticleNumber: true);
            if (columnFilters.TryGetValue("ArticleDescription", out var ad) && !string.IsNullOrWhiteSpace(ad))
                q = ApplyMissingPartsTextFilter(q, ad, isArticleNumber: false, isDescription: true);
            if (columnFilters.TryGetValue("WorkplaceName", out var wn) && !string.IsNullOrWhiteSpace(wn))
                q = ApplyMissingPartsTextFilter(q, wn, isArticleNumber: false, isDescription: false, isWorkplace: true);
            if (columnFilters.TryGetValue("NoteLager", out var nl) && !string.IsNullOrWhiteSpace(nl))
                q = ApplyMissingPartsTextFilter(q, nl, isArticleNumber: false, isDescription: false, isWorkplace: false, isNoteLager: true);
            if (columnFilters.TryGetValue("NoteEinkauf", out var ne) && !string.IsNullOrWhiteSpace(ne))
                q = ApplyMissingPartsTextFilter(q, ne, isArticleNumber: false, isDescription: false, isWorkplace: false, isNoteLager: false, isNoteEinkauf: true);
        }
```

- [ ] **Step 5: ApplyMissingPartsTextFilter erweitern**

Such die `ApplyMissingPartsTextFilter`-Methode (private static, am Ende der Klasse). Ersetz Signatur + Body durch:

```csharp
    private static IQueryable<WarehouseRequisitionItem> ApplyMissingPartsTextFilter(
        IQueryable<WarehouseRequisitionItem> q, string filterValue,
        bool isArticleNumber = false, bool isDescription = false,
        bool isWorkplace = false, bool isNoteLager = false, bool isNoteEinkauf = false)
    {
        var tokens = filterValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var positives = tokens.Where(t => !t.StartsWith("!")).ToList();
        var negatives = tokens.Where(t => t.StartsWith("!")).Select(t => t.Substring(1)).ToList();

        if (positives.Count > 0)
        {
            if (isArticleNumber)
                q = q.Where(i => positives.Any(p => i.ArticleNumber.Contains(p)));
            else if (isDescription)
                q = q.Where(i => positives.Any(p => i.ArticleDescription.Contains(p)));
            else if (isWorkplace)
                q = q.Where(i => positives.Any(p => i.WarehouseRequisition.ProductionWorkplace.Name.Contains(p)));
            else if (isNoteLager)
                q = q.Where(i => i.Note != null && positives.Any(p => i.Note.Contains(p)));
            else if (isNoteEinkauf)
                q = q.Where(i => i.NoteEinkauf != null && positives.Any(p => i.NoteEinkauf.Contains(p)));
        }
        foreach (var n in negatives)
        {
            if (isArticleNumber)
                q = q.Where(i => !i.ArticleNumber.Contains(n));
            else if (isDescription)
                q = q.Where(i => !i.ArticleDescription.Contains(n));
            else if (isWorkplace)
                q = q.Where(i => !i.WarehouseRequisition.ProductionWorkplace.Name.Contains(n));
            else if (isNoteLager)
                q = q.Where(i => i.Note == null || !i.Note.Contains(n));
            else if (isNoteEinkauf)
                q = q.Where(i => i.NoteEinkauf == null || !i.NoteEinkauf.Contains(n));
        }
        return q;
    }
```

- [ ] **Step 6: Tests**

```
dotnet build C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd/IdealAkeWms.slnx
dotnet test C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd/IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "FullyQualifiedName~WarehouseRequisitionRepositoryTests" --no-build
```

Expected: alle Repo-Tests passing.

- [ ] **Step 7: Commit**

```
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd add IdealAkeWms/Data/Repositories/WarehouseRequisitionRepository.cs IdealAkeWms.Tests/Repositories/WarehouseRequisitionRepositoryTests.cs
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd commit -m "feat(repo): GetMissingPartsAsync NoteEinkauf-Mapping + Column-Filter

MissingPartRow bekommt NoteEinkauf. ApplyMissingPartsTextFilter erweitert
um isNoteLager und isNoteEinkauf Branches. 2 neue Tests fuer Column-Filter.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 5: WarehousePickingController notesEinkauf Form-Binding + Tests

**Files:**
- Modify: `IdealAkeWms/Controllers/WarehousePickingController.cs`
- Modify: `IdealAkeWms.Tests/Controllers/WarehousePickingControllerTests.cs`

- [ ] **Step 1: Close-Action — notesEinkauf-Param**

In `IdealAkeWms/Controllers/WarehousePickingController.cs` die `[HttpPost] Close`-Action komplett ersetzen durch:

```csharp
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(int id, int[] itemIds, int[] quantitiesPicked,
        string?[]? notes, string?[]? notesEinkauf, int[]? shortageStatuses, byte[] rowVersion)
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

        var noteEkDict = new Dictionary<int, string?>();
        if (notesEinkauf != null)
            for (int idx = 0; idx < itemIds.Length; idx++)
                noteEkDict[itemIds[idx]] = idx < notesEinkauf.Length ? notesEinkauf[idx] : null;

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
            await _repo.CloseAsync(id, qtyDict, noteDict, noteEkDict, statusDict,
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

- [ ] **Step 2: SaveProgress-Action analog**

Such `[HttpPost] SaveProgress`. Ersetz durch:

```csharp
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveProgress(int id,
        [FromForm] int[] itemIds,
        [FromForm] int?[]? quantitiesPicked,
        [FromForm] string?[]? notes,
        [FromForm] string?[]? notesEinkauf,
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
        var noteEkDict = new Dictionary<int, string?>();
        if (notesEinkauf != null)
            for (int idx = 0; idx < itemIds.Length; idx++)
                noteEkDict[itemIds[idx]] = idx < notesEinkauf.Length ? notesEinkauf[idx] : null;
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

        await _repo.SaveProgressAsync(id, qtyDict, noteDict, noteEkDict, statusDict,
            _user.GetDisplayName(), _user.GetWindowsUserName());
        return Ok();
    }
```

- [ ] **Step 3: PrintAndClose-Action analog**

Such `[HttpPost] PrintAndClose`. Ersetz durch:

```csharp
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PrintAndClose(int id, int[] itemIds, int[] quantitiesPicked,
        string?[]? notes, string?[]? notesEinkauf, int[]? shortageStatuses, byte[] rowVersion)
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
        var noteEkDict = new Dictionary<int, string?>();
        if (notesEinkauf != null)
            for (int idx = 0; idx < itemIds.Length; idx++)
                noteEkDict[itemIds[idx]] = idx < notesEinkauf.Length ? notesEinkauf[idx] : null;
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
            await _repo.CloseAsync(id, qtyDict, noteDict, noteEkDict, statusDict,
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

- [ ] **Step 4: Details + Print Mapping**

In `Details`- und `Print`-Action: such die `detailItems.Add(new WarehouseRequisitionDetailItemViewModel(...))`-Aufrufe. Am Ende der Parameter-Liste `i.NoteEinkauf` ergaenzen:

```csharp
detailItems.Add(new WarehouseRequisitionDetailItemViewModel(
    i.Id, i.Position, i.ArticleNumber, i.ArticleDescription, i.Unit,
    i.QuantityRequested, i.QuantityPicked, locationStr, i.Note, i.ShortageStatus, i.NoteEinkauf));
```

- [ ] **Step 5: Bestehende Controller-Tests migrieren + 1 neuer Test**

In `IdealAkeWms.Tests/Controllers/WarehousePickingControllerTests.cs`:

a) Bestehende Tests die `notes:` und `shortageStatuses:` als named Args verwenden — neuen Arg `notesEinkauf: null` zwischen den beiden einfuegen.

b) `Mock.Verify` und `Mock.Setup`-Aufrufe fuer `CloseAsync` und `SaveProgressAsync`: zusaetzliches `It.IsAny<IReadOnlyDictionary<int, string?>>()` als 4. Param einfuegen (zwischen itemNotes und itemShortageStatuses).

c) NEUER Test am Ende der Klasse:

```csharp
    [Fact]
    public async Task Close_BindsNotesEinkaufArray()
    {
        var (ctrl, repo, _, _) = SetupWithMockRepo();
        await ctrl.Close(id: 1, itemIds: new[] { 10, 20 },
            quantitiesPicked: new[] { 5, 0 },
            notes: new string?[] { "lager", "lager2" },
            notesEinkauf: new string?[] { "ek1", "ek2" },
            shortageStatuses: new[] { 0, 2 },
            rowVersion: new byte[0]);
        repo.Verify(r => r.CloseAsync(1,
            It.IsAny<IReadOnlyDictionary<int, decimal>>(),
            It.IsAny<IReadOnlyDictionary<int, string?>>(),
            It.Is<IReadOnlyDictionary<int, string?>>(d => d[10] == "ek1" && d[20] == "ek2"),
            It.IsAny<IReadOnlyDictionary<int, ShortageStatus>>(),
            It.IsAny<int>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>()),
            Times.Once);
    }
```

- [ ] **Step 6: Build + Tests**

```
dotnet build C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd/IdealAkeWms.slnx
dotnet test C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd/IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "FullyQualifiedName~WarehousePickingControllerTests" --no-build
```

Expected: alle Picking-Controller-Tests passing.

- [ ] **Step 7: Commit**

```
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd add IdealAkeWms/Controllers/WarehousePickingController.cs IdealAkeWms.Tests/Controllers/WarehousePickingControllerTests.cs
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd commit -m "feat(controller): WarehousePickingController notesEinkauf Form-Binding

Close/SaveProgress/PrintAndClose binden zusaetzlich notesEinkauf[] ein.
Details/Print-Mapping reicht i.NoteEinkauf durch. 1 neuer Test fuer
Form-Binding. Bestehende Tests migriert.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 6: MissingPartsController Default mineOnly=true + HasNoWorkplaceMapping (TDD)

**Files:**
- Modify: `IdealAkeWms/Controllers/MissingPartsController.cs`
- Modify: `IdealAkeWms.Tests/Controllers/MissingPartsControllerTests.cs`

- [ ] **Step 1: 2 neue Tests**

In `IdealAkeWms.Tests/Controllers/MissingPartsControllerTests.cs`:

a) Bestehender Test `Index_NoMineOnly_PassesWorkplaceIdToRepoUnchanged` UMBENENNEN zu `Index_MineOnlyFalse_PassesWorkplaceIdToRepoUnchanged` (Body unveraendert — er ruft `mineOnly: false` explizit auf).

b) Am Ende neu:

```csharp
    [Fact]
    public async Task Index_DefaultIsMineOnlyTrue()
    {
        var (ctrl, _, wp, user) = Build();
        user.Setup(u => u.GetCurrentAppUserId()).Returns(42);
        wp.Setup(w => w.GetByUserIdAsync(42)).ReturnsAsync(new List<ProductionWorkplace>
            { new ProductionWorkplace { Id = 7, Name = "WB7" } });
        await ctrl.Index();
        wp.Verify(w => w.GetByUserIdAsync(42), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Index_MineOnly_NoWorkplaceMapping_SetsHasNoWorkplaceMappingTrue()
    {
        var (ctrl, _, wp, user) = Build();
        user.Setup(u => u.GetCurrentAppUserId()).Returns(99);
        wp.Setup(w => w.GetByUserIdAsync(99)).ReturnsAsync(new List<ProductionWorkplace>());
        var result = await ctrl.Index(mineOnly: true);
        var vm = (result as ViewResult)?.Model as MissingPartsListViewModel;
        vm!.HasNoWorkplaceMapping.Should().BeTrue();
        vm.Items.Should().BeEmpty();
    }
```

- [ ] **Step 2: FAIL erwarten**

```
dotnet test C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd/IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "FullyQualifiedName~Index_DefaultIsMineOnlyTrue|FullyQualifiedName~Index_MineOnly_NoWorkplaceMapping_SetsHasNoWorkplaceMappingTrue"
```

Expected: beide Tests scheitern (Default ist bisher `mineOnly=false`, ViewModel hat noch keine `HasNoWorkplaceMapping`).

- [ ] **Step 3: MissingPartsController.Index — Default + Banner-Flag**

In `IdealAkeWms/Controllers/MissingPartsController.cs` die `Index`-Action — Signatur und ViewModel-Befuellung anpassen.

Signatur:
```csharp
    public async Task<IActionResult> Index(
        ShortageStatus tab = ShortageStatus.WillBeRestocked,
        int? workplaceId = null,
        bool mineOnly = true,    // GEAENDERT
        int page = 1, int? pageSize = null)
```

Im Body nach dem `if (mineOnly) { ... }`-Block (wo `userWorkplaceIds` befuellt wird) und vor dem `GetMissingPartsAsync`-Call:

```csharp
        bool hasNoWorkplaceMapping = false;
        if (mineOnly && userWorkplaceIds != null && userWorkplaceIds.Count == 0)
            hasNoWorkplaceMapping = true;
```

Im ViewModel-Init:
```csharp
            HasNoWorkplaceMapping = hasNoWorkplaceMapping,
```

direkt nach `MineOnly = mineOnly,` ergaenzen.

- [ ] **Step 4: Bei HasNoWorkplaceMapping leere Liste erzwingen**

Im Body — wenn `hasNoWorkplaceMapping == true` soll das Resultat leer sein UND die Repo-Aufrufe sollten gar nicht laufen (Performance):

Direkt nach dem `hasNoWorkplaceMapping`-Block einfuegen:

```csharp
        if (hasNoWorkplaceMapping)
        {
            return View(new MissingPartsListViewModel
            {
                Items = new List<MissingPartRow>(),
                AvailableWorkplaces = (await _workplaces.GetAllAsync()).OrderBy(w => w.Name).ToList(),
                WorkplaceFilter = workplaceId,
                MineOnly = mineOnly,
                ActiveTab = tab,
                WaitingTotalCount = 0,
                NoRestockTotalCount = 0,
                HasNoWorkplaceMapping = true,
                Pagination = new PaginationState
                {
                    CurrentPage = page,
                    PageSize = effectivePageSize,
                    PageSizeRaw = rawPageSize,
                    TotalCount = 0
                }
            });
        }
```

- [ ] **Step 5: Build + Tests**

```
dotnet build C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd/IdealAkeWms.slnx
dotnet test C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd/IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "FullyQualifiedName~MissingPartsControllerTests" --no-build
```

Expected: alle Tests passing inkl. die 2 neuen.

- [ ] **Step 6: Commit**

```
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd add IdealAkeWms/Controllers/MissingPartsController.cs IdealAkeWms.Tests/Controllers/MissingPartsControllerTests.cs
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd commit -m "feat(controller): MissingParts default mineOnly=true + HasNoWorkplaceMapping flag

Werkbank-Sicht default mineOnly=true. User ohne Workplace-Zuordnung
bekommt leere Liste + Banner-Flag. 2 neue Tests, 1 bestehender umbenannt.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 7: MissingPartsLagerController + View + Tests (NEU)

**Files:**
- Create: `IdealAkeWms/Controllers/MissingPartsLagerController.cs`
- Create: `IdealAkeWms/Views/MissingPartsLager/Index.cshtml`
- Create: `IdealAkeWms.Tests/Controllers/MissingPartsLagerControllerTests.cs`

- [ ] **Step 1: Controller anlegen**

Neue Datei `IdealAkeWms/Controllers/MissingPartsLagerController.cs`:

```csharp
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers;

[RequireStockAccess]
public class MissingPartsLagerController : Controller
{
    private readonly IWarehouseRequisitionRepository _repo;
    private readonly IProductionWorkplaceRepository _workplaces;
    private readonly ICurrentUserService _user;

    public MissingPartsLagerController(
        IWarehouseRequisitionRepository repo,
        IProductionWorkplaceRepository workplaces,
        ICurrentUserService user)
    {
        _repo = repo; _workplaces = workplaces; _user = user;
    }

    public async Task<IActionResult> Index(
        ShortageStatus tab = ShortageStatus.WillBeRestocked,
        int? workplaceId = null,
        int page = 1, int? pageSize = null)
    {
        if (page < 1) page = 1;
        if (tab == ShortageStatus.None) tab = ShortageStatus.WillBeRestocked;

        var userDefaultPageSize = await _user.GetDefaultPageSizeAsync();
        var effectivePageSize = IdealAkeWms.Services.PageSize.Resolve(pageSize, userDefaultPageSize);
        var rawPageSize = IdealAkeWms.Services.PageSize.ResolveRaw(pageSize, userDefaultPageSize);

        var columnFilters = IdealAkeWms.Services.ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);

        var (rows, total) = await _repo.GetMissingPartsAsync(
            tab, workplaceId, columnFilters, null, null, page, effectivePageSize);

        var waitingResult = await _repo.GetMissingPartsAsync(
            ShortageStatus.WillBeRestocked, workplaceId, null, null, null, 1, 1);
        var noRestockResult = await _repo.GetMissingPartsAsync(
            ShortageStatus.NoRestock, workplaceId, null, null, null, 1, 1);

        var vm = new MissingPartsListViewModel
        {
            Items = rows.ToList(),
            AvailableWorkplaces = (await _workplaces.GetAllAsync()).OrderBy(w => w.Name).ToList(),
            WorkplaceFilter = workplaceId,
            MineOnly = false,
            ActiveTab = tab,
            WaitingTotalCount = waitingResult.TotalCount,
            NoRestockTotalCount = noRestockResult.TotalCount,
            HasNoWorkplaceMapping = false,
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
}
```

- [ ] **Step 2: View anlegen**

Neue Datei `IdealAkeWms/Views/MissingPartsLager/Index.cshtml`:

```html
@model IdealAkeWms.Models.ViewModels.MissingPartsListViewModel
@using IdealAkeWms.Models
@{
    ViewData["Title"] = "Fehlteile (Lager)";
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
           asp-controller="MissingPartsLager" asp-action="Index" asp-route-tab="WillBeRestocked"
           asp-route-workplaceId="@Model.WorkplaceFilter">
            Offene Fehlteile
            <span class="badge bg-warning text-dark ms-1">@Model.WaitingTotalCount</span>
        </a>
    </li>
    <li class="nav-item">
        <a class="nav-link @(Model.ActiveTab == ShortageStatus.NoRestock ? "active" : "")"
           asp-controller="MissingPartsLager" asp-action="Index" asp-route-tab="NoRestock"
           asp-route-workplaceId="@Model.WorkplaceFilter">
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
        </form>
    </div>
</div>

<div class="table-responsive">
    <table class="table table-striped filterable-table"
           data-server-column-filter="true"
           data-view-key="MissingPartsLager">
        <thead>
            <tr>
                <th data-col-key="RequisitionId">Bestell-ID</th>
                <th data-col-key="WorkplaceName">Werkbank</th>
                <th data-col-key="ArticleNumber">Artikel-Nr</th>
                <th data-col-key="ArticleDescription">Bezeichnung</th>
                <th data-col-key="QuantityRequested">Soll</th>
                <th data-col-key="QuantityPicked">Geliefert</th>
                <th data-col-key="QuantityMissing">Fehlt</th>
                <th data-col-key="NoteLager">Notiz Lager</th>
                <th data-col-key="NoteEinkauf">Notiz EK</th>
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
                    <td>@row.NoteEinkauf</td>
                    <td>@row.CreatedBy</td>
                    <td>@(row.ClosedAt?.ToString("dd.MM.yyyy HH:mm") ?? "—")</td>
                </tr>
            }
        </tbody>
    </table>
</div>

<partial name="_Pagination" model="Model.Pagination" />
```

- [ ] **Step 3: Controller-Tests anlegen**

Neue Datei `IdealAkeWms.Tests/Controllers/MissingPartsLagerControllerTests.cs`:

```csharp
using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace IdealAkeWms.Tests.Controllers;

public class MissingPartsLagerControllerTests
{
    private static (MissingPartsLagerController ctrl, Mock<IWarehouseRequisitionRepository> repo,
                    Mock<IProductionWorkplaceRepository> wp, Mock<ICurrentUserService> user) Build()
    {
        var repo = new Mock<IWarehouseRequisitionRepository>();
        var wp = new Mock<IProductionWorkplaceRepository>();
        var user = new Mock<ICurrentUserService>();
        user.Setup(u => u.GetDefaultPageSizeAsync()).ReturnsAsync((int?)null);
        user.Setup(u => u.GetCurrentAppUserId()).Returns(1);
        wp.Setup(w => w.GetAllAsync()).ReturnsAsync(new List<ProductionWorkplace>());
        repo.Setup(r => r.GetMissingPartsAsync(It.IsAny<ShortageStatus>(),
                It.IsAny<int?>(),
                It.IsAny<IReadOnlyDictionary<string, string>?>(),
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(((IReadOnlyList<MissingPartRow>)new List<MissingPartRow>(), 0));
        var ctrl = new MissingPartsLagerController(repo.Object, wp.Object, user.Object);
        ctrl.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return (ctrl, repo, wp, user);
    }

    [Fact]
    public async Task Index_DefaultReturnsViewModel()
    {
        var (ctrl, _, _, _) = Build();
        var result = await ctrl.Index();
        result.Should().BeOfType<ViewResult>();
        var vm = (result as ViewResult)!.Model as MissingPartsListViewModel;
        vm!.MineOnly.Should().BeFalse();
        vm.ActiveTab.Should().Be(ShortageStatus.WillBeRestocked);
    }

    [Fact]
    public async Task Index_TabParam_PassedToRepo()
    {
        var (ctrl, repo, _, _) = Build();
        await ctrl.Index(tab: ShortageStatus.NoRestock);
        repo.Verify(r => r.GetMissingPartsAsync(ShortageStatus.NoRestock,
            It.IsAny<int?>(), It.IsAny<IReadOnlyDictionary<string, string>?>(),
            It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
            It.IsAny<int>(), It.IsAny<int>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Index_DoesNotApplyMineOnly()
    {
        var (ctrl, _, wp, _) = Build();
        await ctrl.Index();
        wp.Verify(w => w.GetByUserIdAsync(It.IsAny<int>()), Times.Never);
    }
}
```

- [ ] **Step 4: Build + Tests**

```
dotnet build C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd/IdealAkeWms.slnx
dotnet test C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd/IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "FullyQualifiedName~MissingPartsLagerControllerTests" --no-build
```

Expected: 3 neue Tests passing.

- [ ] **Step 5: Commit**

```
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd add IdealAkeWms/Controllers/MissingPartsLagerController.cs IdealAkeWms/Views/MissingPartsLager/Index.cshtml IdealAkeWms.Tests/Controllers/MissingPartsLagerControllerTests.cs
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd commit -m "feat(view): MissingPartsLagerController + View (Lager-Sicht ohne mineOnly)

Klon-Controller mit eigenem View 'Fehlteile (Lager)'. Kein mineOnly-Param,
sieht alle Fehlteile global. 2 Notiz-Spalten (Lager + EK) wie im
Werkbank-View. 3 neue Tests.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 8: Details.cshtml UI — Spalte umbenennen + Notiz-EK-Input + JS

**Files:**
- Modify: `IdealAkeWms/Views/WarehousePicking/Details.cshtml`

- [ ] **Step 1: Header umbenennen + neue Spalte**

In `IdealAkeWms/Views/WarehousePicking/Details.cshtml` such die `<thead>`-Zeile. Aktuelle Form:
```html
<th>Pos</th><th>Artikel-Nr</th><th>Bezeichnung</th><th>Bestellt</th>
<th>Ist</th><th>ME</th><th>Lagerplatz</th><th>Notiz</th>
<th title="Fehlteil-Status">Fehlteil-Status</th>
```

Ersetz durch:
```html
<th>Pos</th><th>Artikel-Nr</th><th>Bezeichnung</th><th>Bestellt</th>
<th>Ist</th><th>ME</th><th>Lagerplatz</th><th>Notiz Lager</th><th>Notiz EK</th>
<th title="Fehlteil-Status">Fehlteil-Status</th>
```

- [ ] **Step 2: Body — neue Notiz-EK-Spalte**

Such die Stelle in der `tbody`-Schleife wo der bestehende Notiz-Input ist. Nach dem schliessenden `</td>` der Notiz-Spalte und VOR der `<td class="text-nowrap">`-Spalte fuer Fehlteil-Status eine neue Spalte einfuegen:

```html
                    <td>
                        @if (isEditable)
                        {
                            <input type="text" name="notesEinkauf" value="@i.NoteEinkauf" maxlength="500"
                                   class="form-control form-control-sm note-einkauf-input" placeholder="Notiz EK (optional)" />
                        }
                        else
                        {
                            <small>@i.NoteEinkauf</small>
                        }
                    </td>
```

Die bestehende Notiz-Spalte (mit `name="notes"` und `class="note-input"`) bleibt unveraendert. UI-Label-Aenderung (Header) reicht.

Aktualisier auch `placeholder` der bestehenden Notiz-Spalte: aus "Notiz (optional)" wird "Notiz Lager (optional)":

Such `placeholder="Notiz (optional)"` und ersetz durch `placeholder="Notiz Lager (optional)"`.

- [ ] **Step 3: JS — collectProgress + saveProgress + Note-EK-Input-Listener**

Such die `collectProgress`-Funktion in `<script>`:

```javascript
    function collectProgress() {
        const itemIds = Array.from(form.querySelectorAll('input[name="itemIds"]')).map(i => i.value);
        const quantitiesPicked = Array.from(form.querySelectorAll('input[name="quantitiesPicked"]'))
            .map(i => i.value || '');
        const notes = Array.from(form.querySelectorAll('input[name="notes"]')).map(i => i.value || '');
        const shortageStatuses = Array.from(form.querySelectorAll('input.shortage-hidden')).map(i => i.value);
        return { itemIds, quantitiesPicked, notes, shortageStatuses };
    }
```

Ersetz durch:

```javascript
    function collectProgress() {
        const itemIds = Array.from(form.querySelectorAll('input[name="itemIds"]')).map(i => i.value);
        const quantitiesPicked = Array.from(form.querySelectorAll('input[name="quantitiesPicked"]'))
            .map(i => i.value || '');
        const notes = Array.from(form.querySelectorAll('input[name="notes"]')).map(i => i.value || '');
        const notesEinkauf = Array.from(form.querySelectorAll('input[name="notesEinkauf"]')).map(i => i.value || '');
        const shortageStatuses = Array.from(form.querySelectorAll('input.shortage-hidden')).map(i => i.value);
        return { itemIds, quantitiesPicked, notes, notesEinkauf, shortageStatuses };
    }
```

Such die `saveProgress`-Funktion. In `body.append`-Block nach `notes.forEach(v => body.append('notes', v));` eine Zeile einfuegen:

```javascript
        notesEinkauf.forEach(v => body.append('notesEinkauf', v));
```

Stelle sicher dass die Destructuring-Zeile `const { itemIds, quantitiesPicked, notes, shortageStatuses } = collectProgress();` zu `const { itemIds, quantitiesPicked, notes, notesEinkauf, shortageStatuses } = collectProgress();` erweitert wird.

Such den Listener-Block fuer `.note-input` und ergaenze danach eine analoge Schleife fuer `.note-einkauf-input`:

```javascript
        document.querySelectorAll('.note-einkauf-input').forEach(inp => {
            inp.addEventListener('input', () => { dirty = true; });
            inp.addEventListener('blur', saveProgress);
        });
```

- [ ] **Step 4: Build + Tests**

```
dotnet build C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd/IdealAkeWms.slnx
dotnet test C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd/IdealAkeWms.slnx --no-build
```

Expected: success, Tests unveraendert.

- [ ] **Step 5: Commit**

```
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd add IdealAkeWms/Views/WarehousePicking/Details.cshtml
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd commit -m "feat(view): Details mit Notiz Lager + Notiz EK Spalten

Header 'Notiz' wird 'Notiz Lager', neue Spalte 'Notiz EK' rechts daneben.
JS collectProgress + saveProgress + Note-EK-Input-Listener fuer Autosave.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 9: Print.cshtml — Header + neue Spalte

**Files:**
- Modify: `IdealAkeWms/Views/WarehousePicking/Print.cshtml`

- [ ] **Step 1: Header umbenennen + neue Spalte**

In `IdealAkeWms/Views/WarehousePicking/Print.cshtml` such die `<thead>`-Zeile. Such `<th>Notiz</th>` und ersetz durch `<th>Notiz Lager</th><th>Notiz EK</th>`.

- [ ] **Step 2: Body — neue Spalte**

Such die `<td>@i.Note</td>`-Stelle im `tbody`-Loop. Direkt danach einfuegen:

```html
                <td>@i.NoteEinkauf</td>
```

- [ ] **Step 3: Build + Commit**

```
dotnet build C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd/IdealAkeWms.slnx
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd add IdealAkeWms/Views/WarehousePicking/Print.cshtml
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd commit -m "feat(view): Print zeigt Notiz Lager + Notiz EK Spalten

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 10: MissingParts/Index.cshtml — 2 Notiz-Spalten + Banner

**Files:**
- Modify: `IdealAkeWms/Views/MissingParts/Index.cshtml`

- [ ] **Step 1: Banner nach Page-Header**

In `IdealAkeWms/Views/MissingParts/Index.cshtml` such die `<h2 class="page-header">`-Zeile. Direkt nach dem `</h2>` und VOR den TempData-Alerts einfuegen:

```html
@if (Model.HasNoWorkplaceMapping)
{
    <div class="alert alert-info">
        Du hast keine Werkbank-Zuordnung. Diese Liste ist deshalb leer.
        Du kannst entweder den Toggle <em>"Nur meine Werkbaenke"</em> deaktivieren
        oder die Zuordnung in den Stammdaten pflegen.
    </div>
}
```

- [ ] **Step 2: Notiz-Spalten-Header**

Such die Tabellen-Header-Zeile mit `<th data-col-key="Note">Notiz</th>`. Ersetz durch:

```html
                <th data-col-key="NoteLager">Notiz Lager</th>
                <th data-col-key="NoteEinkauf">Notiz EK</th>
```

- [ ] **Step 3: Notiz-Spalten-Body**

Such die Body-Zeile mit `<td>@row.Note</td>`. Ersetz durch:

```html
                    <td>@row.Note</td>
                    <td>@row.NoteEinkauf</td>
```

- [ ] **Step 4: Build + Commit**

```
dotnet build C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd/IdealAkeWms.slnx
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd add IdealAkeWms/Views/MissingParts/Index.cshtml
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd commit -m "feat(view): MissingParts mit 2 Notiz-Spalten + No-Workplace-Banner

Notiz-Spalte aufgesplittet in 'Notiz Lager' (col-key NoteLager) + neue
'Notiz EK' (col-key NoteEinkauf), beide einzeln filterbar. Info-Banner
zeigt sich wenn User keine Werkbank-Zuordnung hat.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 11: _Layout.cshtml — Menue-Eintraege

**Files:**
- Modify: `IdealAkeWms/Views/Shared/_Layout.cshtml`

- [ ] **Step 1: Menue-Eintrag suchen + umbenennen**

In `IdealAkeWms/Views/Shared/_Layout.cshtml` such die Zeile mit `asp-controller="MissingParts" asp-action="Index"`. Aktueller Text vermutlich "Fehlteile". Ersetz Link-Text durch "Meine Fehlteile":

```html
<a class="dropdown-item" asp-controller="MissingParts" asp-action="Index">Meine Fehlteile</a>
```

- [ ] **Step 2: Neuer Eintrag direkt darunter**

Direkt nach dem oben gefundenen `<a>`-Tag einfuegen:

```html
<a class="dropdown-item" asp-controller="MissingPartsLager" asp-action="Index">Fehlteile (Lager)</a>
```

Beide Eintraege haben implizit die gleichen Rollen-Bedingungen (sie liegen im selben `@if`-Block).

- [ ] **Step 3: Build + Commit**

```
dotnet build C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd/IdealAkeWms.slnx
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd add IdealAkeWms/Views/Shared/_Layout.cshtml
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd commit -m "feat(layout): 'Fehlteile' wird 'Meine Fehlteile' + neuer Eintrag 'Fehlteile (Lager)'

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 12: Doku — Changelog + PROJECT_STATUS + CLAUDE.md + TESTSZENARIEN

**Files:**
- Modify: `IdealAkeWms/Views/Help/Changelog.cshtml`
- Modify: `PROJECT_STATUS.md`
- Modify: `CLAUDE.md`
- Modify: `docs/TESTSZENARIEN.md`

- [ ] **Step 1: Changelog v1.19.0-Card erweitern**

In `IdealAkeWms/Views/Help/Changelog.cshtml` such die bestehende v1.19.0-Card. In der `<ul>`-Liste vor `</ul>` zwei neue `<li>`-Bullets ergaenzen:

```html
                    <li><strong>Notiz EK:</strong> Pro Position gibt es jetzt zwei Notiz-Felder.
                        Die bestehende Notiz wurde zu "Notiz Lager" umbenannt, daneben neu
                        "Notiz EK" fuer Hinweise an den Einkauf (z.B. Lieferanten-Info bei
                        endgueltigem Fehlteil).</li>
                    <li><strong>Fehlteile getrennt nach Sicht:</strong> Das Lager-Menue zeigt
                        jetzt zwei Eintraege — "Meine Fehlteile" (Werkbank-Sicht, default nur
                        eigene Werkbaenke) und "Fehlteile (Lager)" (globale Sicht ohne Filter).
                        Werkbank-User ohne Workplace-Zuordnung sehen einen Hinweis-Banner.</li>
```

- [ ] **Step 2: PROJECT_STATUS Sub-Task-Tabelle erweitern**

In `PROJECT_STATUS.md` such die bestehende v1.19.0-Sub-Task-Tabelle. Vor dem schliessenden `---` (Trennlinie nach Sub-Task 12) ergaenzen:

```markdown
| 13 | NoteEinkauf Column + Migration + FreshInstall | ✅ erledigt |
| 14 | ViewModels NoteEinkauf + HasNoWorkplaceMapping | ✅ erledigt |
| 15 | Repo CloseAsync/SaveProgressAsync + Tests | ✅ erledigt |
| 16 | Repo GetMissingPartsAsync NoteEinkauf-Mapping + Column-Filter | ✅ erledigt |
| 17 | WarehousePickingController notesEinkauf Form-Binding | ✅ erledigt |
| 18 | MissingPartsController default mineOnly=true + HasNoWorkplaceMapping | ✅ erledigt |
| 19 | MissingPartsLagerController + View (NEU) | ✅ erledigt |
| 20 | Details/Print/MissingParts/Layout UI-Updates | ✅ erledigt |
| 21 | Doku (Changelog/CLAUDE.md/TESTSZENARIEN) erweitert | ✅ erledigt |
```

(Die existierenden Sub-Tasks 11+12 fuer Final-Check + Merge bleiben — werden weiter unten in der Tabelle ausgefuehrt.)

- [ ] **Step 3: CLAUDE.md 2 neue Fallstricke**

In `CLAUDE.md` im Abschnitt `## Bekannte Fallstricke` am Ende (vor `## Standard-Daten (Neuinstallation)`) zwei neue Eintraege:

```markdown
- **MissingPartsController default mineOnly=true (seit v1.19.0)**: Werkbank-Sicht (`/MissingParts`) filtert per Default auf eigene Werkbaenke. Lager-Sicht laeuft ueber separaten `MissingPartsLagerController` (`/MissingPartsLager`) ohne mineOnly-Param. Layout-Menue zeigt beide als "Meine Fehlteile" + "Fehlteile (Lager)". User ohne Workplace-Zuordnung sehen leere Liste mit Info-Banner statt automatischem Fallback auf alle Fehlteile.
- **Note vs NoteEinkauf (seit v1.19.0)**: Property im Code heisst `WarehouseRequisitionItem.Note`, UI-Label aber "Notiz Lager". Zweite Notiz fuer den Einkauf heisst sowohl im Code als auch im UI `NoteEinkauf` / "Notiz EK". Property-Rename Note→NoteLager bewusst NICHT durchgefuehrt (groesserer Diff, keine semantische Notwendigkeit fuer DB-Layer). Form-Param-Reihenfolge in Picking-Controller-Actions: `notes` vor `notesEinkauf` vor `shortageStatuses`.
```

- [ ] **Step 4: TESTSZENARIEN Kapitel 33 erweitern**

In `docs/TESTSZENARIEN.md` am Ende von Kapitel 33 (nach 33.10) anhaengen:

```markdown

### 33.11 Notiz EK persistiert

**Vorbedingung:** Submitted-Bestellung mit mindestens einem Item.
**Schritte:**
1. Picking/Details oeffnen.
2. Im Notiz-EK-Feld "Test-EK-Notiz" eintragen.
3. "Speichern + Abschliessen" klicken.
4. Bestellung wieder oeffnen (falls Status PartiallyDelivered) ODER Print aufrufen.
**Erwartet:** "Test-EK-Notiz" ist nach dem Abschliessen weiterhin sichtbar. Print zeigt die Notiz in der "Notiz EK"-Spalte.

### 33.12 MissingParts default mineOnly

**Vorbedingung:** Werkbank-User mit Workplace-Zuordnung; mindestens 1 IsFinalShortage-Item in eigener Bestellung und mindestens 1 in einer fremden Bestellung (Status Closed).
**Schritte:**
1. `/MissingParts` direkt im Browser aufrufen (ohne URL-Parameter).
**Erwartet:** Tab "Wird nicht nachgeliefert" zeigt nur das eigene Item. Toggle "Nur meine Werkbaenke" ist aktiv. Deaktivieren zeigt auch das fremde Item.

### 33.13 No-Workplace-Banner

**Vorbedingung:** Test-User ohne Workplace-Zuordnung.
**Schritte:**
1. Login als Test-User.
2. Menue "Lager" → "Meine Fehlteile" aufrufen.
**Erwartet:** Banner "Du hast keine Werkbank-Zuordnung. Diese Liste ist deshalb leer. Du kannst entweder den Toggle 'Nur meine Werkbaenke' deaktivieren oder die Zuordnung in den Stammdaten pflegen." erscheint. Items-Tabelle ist leer.

### 33.14 Fehlteile (Lager)-View

**Vorbedingung:** Lager-User (admin/stock/stock_keyuser/picking).
**Schritte:**
1. Menue "Lager" → "Fehlteile (Lager)" aufrufen.
**Erwartet:** Globale Sicht. Kein mineOnly-Toggle in der Filter-Card. Tabs zeigen jeweils Counts aller Werkbaenke. Notiz-Spalten zeigen sowohl "Notiz Lager" als auch "Notiz EK".
```

- [ ] **Step 5: Build + Commit**

```
dotnet build C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd/IdealAkeWms.slnx
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd add IdealAkeWms/Views/Help/Changelog.cshtml PROJECT_STATUS.md CLAUDE.md docs/TESTSZENARIEN.md
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd commit -m "docs: erweitere v1.19.0 Doku um NoteEinkauf + mineOnly + Lager-View

Changelog v1.19.0-Card um 2 Bullets, PROJECT_STATUS Sub-Tasks 13-21,
CLAUDE.md 2 neue Fallstricke, TESTSZENARIEN Kap 33.11-33.14.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 13: Final-Check Build + Tests + Sanity

**Files:** keine Aenderungen

- [ ] **Step 1: Vollstaendiger Build**

```
dotnet build C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd/IdealAkeWms.slnx
```

Expected: `Build succeeded. 0 Error(s)`. Warnungen pre-existing ok.

- [ ] **Step 2: Volle Test-Suite**

```
dotnet test C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd/IdealAkeWms.slnx --no-build
```

Expected: Web ~640 passed + 1 skipped (Baseline 631 + ~9 neue), Service 99 passed.

- [ ] **Step 3: NoteEinkauf-Sanity**

```
grep -c "NoteEinkauf" IdealAkeWms/Data/Repositories/WarehouseRequisitionRepository.cs
grep -c "NoteEinkauf" IdealAkeWms/Models/WarehouseRequisitionItem.cs
grep -c "notesEinkauf" IdealAkeWms/Controllers/WarehousePickingController.cs
grep -c "notesEinkauf" IdealAkeWms/Views/WarehousePicking/Details.cshtml
```

Expected: alle > 0.

- [ ] **Step 4: MissingPartsLager-Sanity**

```
ls IdealAkeWms/Controllers/MissingPartsLagerController.cs
ls IdealAkeWms/Views/MissingPartsLager/Index.cshtml
ls IdealAkeWms.Tests/Controllers/MissingPartsLagerControllerTests.cs
```

Expected: alle 3 Files existieren.

- [ ] **Step 5: Working-Tree clean**

```
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd status
git -C C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd log --oneline 9cdc4cc..HEAD
```

Expected: clean. ~12 neue Commits seit dem Spec-Commit.

- [ ] **Step 6: Layout-Menue-Sanity**

```
grep "MissingPartsLager" IdealAkeWms/Views/Shared/_Layout.cshtml
grep "Meine Fehlteile" IdealAkeWms/Views/Shared/_Layout.cshtml
```

Expected: jeweils mindestens 1 Treffer.

---

## Task 14: Merge in main (NACH User-Bestaetigung)

**WICHTIG:** Diese Task NICHT automatisch ausfuehren. Per Memory-Feedback (`feedback_worktree_cleanup_ask_first`): Vor Merge + Cleanup explizit User-Bestaetigung einholen.

Nach Task 13 stoppen und melden:
> "v1.19.0 ist fertig auf Branch `bugfix/missingparts-include-pd` mit allen Erweiterungen (3-State + Hotfix + Bugfixes + NoteEinkauf + mineOnly + Lager-View). Build + Tests gruen. Merge in main + Push? Worktree danach loeschen, ja/nein?"

Erst nach explizitem Go die `git checkout main / pull / merge --no-ff / build / test / push / worktree remove / branch delete`-Sequenz analog zu vorherigen Rollouts ausfuehren.

---

## Final-Review-Subagent (nach Task 14)

Code-Reviewer-Subagent mit Diff-Range `5cc204a..HEAD` (alle v1.19.0-Commits inkl. Bugfixes + Merge). Pruefkriterien:

1. Migration: NoteEinkauf NVARCHAR(500) NULL korrekt im Schema + MigrationsHistory?
2. CloseAsync + SaveProgressAsync: Param-Reihenfolge `notes` vor `notesEinkauf` vor `statuses`?
3. GetMissingPartsAsync: MissingPartRow.NoteEinkauf befuellt? Column-Filter funktioniert?
4. WarehousePickingController: alle 3 Actions (Close, SaveProgress, PrintAndClose) binden notesEinkauf?
5. MissingPartsController: Default mineOnly=true, HasNoWorkplaceMapping bei empty Workplace-Liste?
6. MissingPartsLagerController: existiert, kein mineOnly-Param, kein GetByUserIdAsync-Call?
7. Details.cshtml: 2 Notiz-Inputs, JS sammelt notesEinkauf?
8. Print.cshtml: 2 Notiz-Spalten?
9. MissingParts/Index.cshtml: 2 Notiz-Spalten + Banner?
10. MissingPartsLager/Index.cshtml: Klon mit data-view-key="MissingPartsLager", kein mineOnly-Toggle?
11. Layout-Menue: "Meine Fehlteile" + "Fehlteile (Lager)"?
12. Doku: Changelog erweitert, PROJECT_STATUS Sub-Tasks 13-21, CLAUDE.md 2 neue Fallstricke, TESTSZENARIEN 33.11-33.14?
13. Test-Counts: ~640 Web + 1 skip, 99 Service?
14. Out-of-Scope: keine kollateralen Aenderungen ausserhalb der erwarteten Files?
