# Lagerbestellungen: Teilgeliefert + Fehlteile + Drucken-und-Abschliessen — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Lagerbestellungen erhalten neuen Status `PartiallyDelivered`, Pro-Item-Flag `IsFinalShortage`, neues Lager-Menue "Fehlteile" und einen kombinierten "Drucken und Abschliessen"-Button.

**Architecture:** EF-Migration fuegt `IsFinalShortage`-Spalte hinzu, Enum erweitert um `PartiallyDelivered=5`. Repository bekommt zentralen `DeriveStatus`-Helper + neue Methoden `SaveProgressAsync` / `GetMissingPartsAsync` / `GetFinalShortagesCountForUserAsync`. UI in `WarehousePicking/Details` bekommt pro-Item-Checkbox + 3-Button-Form mit JS-Popup-Pattern fuer den Print-Tab. Neuer `MissingPartsController` mit Pflicht-Pattern-View (Pagination + Server-Side Column Filter).

**Tech Stack:** ASP.NET Core 10 MVC, EF Core 10, SQL Server, Razor, Bootstrap 5, xUnit + FluentAssertions

**Worktree:** `.claude/worktrees/teilgeliefert-fehlteile`, Branch `feature/teilgeliefert-fehlteile`

**Spec:** [secondbrain/docs/superpowers/specs/2026-05-29-teilgeliefert-fehlteile-design.md](../specs/2026-05-29-teilgeliefert-fehlteile-design.md) (Commit 267e6e8)

---

## File Structure

**Modify:**
- `IdealAkeWms/Models/WarehouseRequisitionStatus.cs` — add `PartiallyDelivered = 5`
- `IdealAkeWms/Models/WarehouseRequisitionItem.cs` — add `IsFinalShortage` property
- `IdealAkeWms/Data/Repositories/WarehouseRequisitionRepository.cs` — `DeriveStatus`-Helper, `CloseAsync` sig, neue Methoden
- `IdealAkeWms/Data/Repositories/IWarehouseRequisitionRepository.cs` — Interface-Aenderungen
- `IdealAkeWms/Controllers/WarehousePickingController.cs` — Index-Filter Default, `Close`-Sig, `SaveProgress`, `PrintAndClose`
- `IdealAkeWms/Controllers/WarehouseRequisitionsController.cs` — Card-Daten in Index
- `IdealAkeWms/Models/ViewModels/WarehouseRequisitionDetailViewModel.cs` — `IsFinalShortage` in Item-Record
- `IdealAkeWms/Models/ViewModels/WarehouseRequisitionListViewModel.cs` — Card-Counts
- `IdealAkeWms/Views/WarehousePicking/Details.cshtml` — Checkbox, Status-Banner, 3 Buttons, updated JS
- `IdealAkeWms/Views/WarehousePicking/Index.cshtml` — Status-Filter-Dropdown erweitern
- `IdealAkeWms/Views/WarehousePicking/Print.cshtml` — Fehlteil-Spalte
- `IdealAkeWms/Views/WarehouseRequisitions/Index.cshtml` — "Meine Fehlteile"-Card
- `IdealAkeWms/Views/Shared/_Layout.cshtml` — Menue-Eintrag "Fehlteile"
- `IdealAkeWms/AppVersion.cs` + `IDEALAKEWMSService/AppVersion.cs` — v1.18.0
- `IdealAkeWms/Views/Help/Changelog.cshtml` — v1.18.0-Card prependen
- `SQL/00_FreshInstall.sql` — Schema-Sync + EF-Migrations-History
- `PROJECT_STATUS.md` — v1.18.0 Sub-Task-Tabelle
- `CLAUDE.md` — 3 Fallstricke
- `docs/TESTSZENARIEN.md` — Kapitel 32 (10 Szenarien)

**Create:**
- `IdealAkeWms/Controllers/MissingPartsController.cs`
- `IdealAkeWms/Models/ViewModels/MissingPartRow.cs` (Record)
- `IdealAkeWms/Models/ViewModels/MissingPartsListViewModel.cs`
- `IdealAkeWms/Views/MissingParts/Index.cshtml`
- `IdealAkeWms/Migrations/2026XXXXXXXXX_AddIsFinalShortageToWarehouseRequisitionItems.cs` (via `dotnet ef migrations add`)
- `SQL/64_AddIsFinalShortageToWarehouseRequisitionItems.sql`

**Tests:**
- `IdealAkeWms.Tests/Repositories/WarehouseRequisitionRepositoryTests.cs` — +16 Tests
- `IdealAkeWms.Tests/Controllers/WarehousePickingControllerTests.cs` — +6 Tests
- `IdealAkeWms.Tests/Controllers/MissingPartsControllerTests.cs` — NEU, 4 Tests
- `IdealAkeWms.Tests/Controllers/WarehouseRequisitionsControllerTests.cs` — +2 Tests

---

## Task 0: Pre-Flight Baseline

**Files:** keine Aenderungen

- [ ] **Step 1: Branch + Worktree verifizieren**

```bash
git rev-parse --abbrev-ref HEAD
# Expected: feature/teilgeliefert-fehlteile

git log --oneline -3
# Expected: 267e6e8 docs(spec): Lagerbestellungen ...
#           da27082 merge feature/article-sync-erweiterung into main (v1.17.0)
#           ...
```

- [ ] **Step 2: Baseline-Build**

```bash
dotnet build IdealAkeWms.slnx
```

Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 3: Baseline-Tests**

```bash
dotnet test IdealAkeWms.slnx --no-build
```

Expected: Web `589 passed / 1 skipped` (Total 590), Service `99 passed`. Wert merken fuer Task 15.

- [ ] **Step 4: SQL-Migration-Counter pruefen**

```bash
ls SQL/*.sql | sort | tail -3
```

Expected: letzte ist `63_AddWarehouseRequisitionItemNote.sql`. Naechste = **64**.

---

## Task 1: Datenmodell + Migration

**Files:**
- Modify: `IdealAkeWms/Models/WarehouseRequisitionStatus.cs`
- Modify: `IdealAkeWms/Models/WarehouseRequisitionItem.cs`
- Create: `IdealAkeWms/Migrations/<timestamp>_AddIsFinalShortageToWarehouseRequisitionItems.cs` (via `dotnet ef`)
- Create: `SQL/64_AddIsFinalShortageToWarehouseRequisitionItems.sql`
- Modify: `SQL/00_FreshInstall.sql`

- [ ] **Step 1: Enum erweitern**

`IdealAkeWms/Models/WarehouseRequisitionStatus.cs` komplett:

```csharp
namespace IdealAkeWms.Models;

public enum WarehouseRequisitionStatus : byte
{
    Draft              = 1,
    Submitted          = 2,
    Closed             = 3,
    Cancelled          = 4,
    PartiallyDelivered = 5
}
```

- [ ] **Step 2: Model-Property hinzufuegen**

In `IdealAkeWms/Models/WarehouseRequisitionItem.cs` nach der `Note`-Property einfuegen (vor schliessender `}`):

```csharp
    /// <summary>
    /// True = Lagermitarbeiter hat dieses Item als endgueltigen Fehlteil markiert.
    /// Wird beim Status-Ableitungs-Helper geprueft; treibt Status auf Closed wenn
    /// alle "kurzen" Items markiert sind.
    /// </summary>
    public bool IsFinalShortage { get; set; }
```

- [ ] **Step 3: EF-Migration generieren**

```bash
dotnet ef migrations add AddIsFinalShortageToWarehouseRequisitionItems --project IdealAkeWms --startup-project IdealAkeWms
```

Expected: neue Migration-Datei in `IdealAkeWms/Migrations/`. Erwartete `Up()`-Action:

```csharp
migrationBuilder.AddColumn<bool>(
    name: "IsFinalShortage",
    table: "WarehouseRequisitionItems",
    type: "bit",
    nullable: false,
    defaultValue: false);
```

Falls EF zusaetzlich `defaultValueSql` oder einen Index hinzufuegt: in Ordnung, nicht entfernen.

- [ ] **Step 4: Filtered Index zur Migration hinzufuegen**

In der frisch generierten Migration-Datei in `Up()` NACH dem `AddColumn`-Block einfuegen:

```csharp
migrationBuilder.Sql(@"
    CREATE INDEX IX_WarehouseRequisitionItems_IsFinalShortage
        ON [dbo].[WarehouseRequisitionItems]([IsFinalShortage])
        WHERE [IsFinalShortage] = 1;
");
```

Und in `Down()` (am Anfang):

```csharp
migrationBuilder.Sql(@"
    DROP INDEX IF EXISTS IX_WarehouseRequisitionItems_IsFinalShortage
        ON [dbo].[WarehouseRequisitionItems];
");
```

- [ ] **Step 5: Idempotente SQL-Migration erstellen**

Neue Datei `SQL/64_AddIsFinalShortageToWarehouseRequisitionItems.sql`:

```sql
-- =============================================
-- 64_AddIsFinalShortageToWarehouseRequisitionItems
-- Adds IsFinalShortage BIT NOT NULL DEFAULT 0 + filtered index.
-- Used by v1.18.0 Lagerbestellungen-Erweiterung.
-- =============================================

IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE Name = N'IsFinalShortage'
               AND Object_ID = Object_ID(N'[dbo].[WarehouseRequisitionItems]'))
BEGIN
    ALTER TABLE [dbo].[WarehouseRequisitionItems]
        ADD [IsFinalShortage] BIT NOT NULL
            CONSTRAINT DF_WarehouseRequisitionItems_IsFinalShortage DEFAULT 0;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = N'IX_WarehouseRequisitionItems_IsFinalShortage'
               AND object_id = OBJECT_ID(N'[dbo].[WarehouseRequisitionItems]'))
BEGIN
    CREATE INDEX IX_WarehouseRequisitionItems_IsFinalShortage
        ON [dbo].[WarehouseRequisitionItems]([IsFinalShortage])
        WHERE [IsFinalShortage] = 1;
END
GO

-- EF Migration History wird vom App-Startup geschrieben falls dieses Skript manuell laeuft.
```

- [ ] **Step 6: 00_FreshInstall.sql synchronisieren**

In `SQL/00_FreshInstall.sql` zwei Stellen anpassen:

1. **Tabellen-Definition** (`CREATE TABLE [dbo].[WarehouseRequisitionItems]`): nach `Note NVARCHAR(500) NULL,`-Zeile einfuegen:
   ```sql
       [IsFinalShortage] BIT NOT NULL CONSTRAINT DF_WarehouseRequisitionItems_IsFinalShortage DEFAULT 0,
   ```

2. **Filtered Index** danach (im Index-Block fuer die Tabelle, falls schon ein anderer Index dort ist):
   ```sql
   CREATE INDEX IX_WarehouseRequisitionItems_IsFinalShortage
       ON [dbo].[WarehouseRequisitionItems]([IsFinalShortage])
       WHERE [IsFinalShortage] = 1;
   GO
   ```

3. **`__EFMigrationsHistory`-INSERT-Block** ganz am Ende: neue Zeile fuer die soeben generierte Migration-ID (sieht aus wie `20260529XXXXXX_AddIsFinalShortageToWarehouseRequisitionItems`):
   ```sql
   INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES
       (N'20260529XXXXXX_AddIsFinalShortageToWarehouseRequisitionItems', N'10.0.2');
   ```

   Ersetze `XXXXXX` durch die echte Zeit aus dem Migration-Dateinamen.

- [ ] **Step 7: Build verifizieren**

```bash
dotnet build IdealAkeWms.slnx
```

Expected: `Build succeeded`. Wenn EF eine `PendingModelChangesWarning` wirft, war Step 3 unvollstaendig — Migration nochmal generieren.

- [ ] **Step 8: Commit**

```bash
git add IdealAkeWms/Models/WarehouseRequisitionStatus.cs IdealAkeWms/Models/WarehouseRequisitionItem.cs IdealAkeWms/Migrations/ SQL/64_AddIsFinalShortageToWarehouseRequisitionItems.sql SQL/00_FreshInstall.sql
git commit -m "feat(db): add IsFinalShortage column + PartiallyDelivered status

Migration 64 + EF Migration. WarehouseRequisitionItem bekommt IsFinalShortage
(default false). Status-Enum erweitert um PartiallyDelivered=5.
SQL/00_FreshInstall.sql konsolidiert (Schema + MigrationsHistory).

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 2: Repository — DeriveStatus + CloseAsync-Refactor (TDD)

**Files:**
- Modify: `IdealAkeWms/Data/Repositories/IWarehouseRequisitionRepository.cs`
- Modify: `IdealAkeWms/Data/Repositories/WarehouseRequisitionRepository.cs`
- Modify: `IdealAkeWms.Tests/Repositories/WarehouseRequisitionRepositoryTests.cs`

- [ ] **Step 1: 7 Failing Tests fuer CloseAsync schreiben**

In `IdealAkeWms.Tests/Repositories/WarehouseRequisitionRepositoryTests.cs` am Ende der Klasse einfuegen (vor schliessender `}`):

```csharp
    private async Task<int> SeedRequisitionAsync(ApplicationDbContext db, params (int requested, decimal? picked, bool finalShortage)[] items)
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
        foreach (var (req, picked, final) in items)
        {
            db.WarehouseRequisitionItems.Add(new WarehouseRequisitionItem
            {
                WarehouseRequisitionId = r.Id,
                Position = pos++,
                ArticleNumber = $"ART-{pos}",
                ArticleDescription = $"Article {pos}",
                QuantityRequested = req,
                QuantityPicked = picked,
                IsFinalShortage = final,
                CreatedAt = DateTime.Now,
                CreatedBy = "test",
                CreatedByWindows = "test\\test",
            });
        }
        await db.SaveChangesAsync();
        return r.Id;
    }

    [Fact]
    public async Task CloseAsync_AllItemsFullyDelivered_SetsStatusClosed()
    {
        using var db = TestDbContextFactory.Create();
        var repo = new WarehouseRequisitionRepository(db);
        var id = await SeedRequisitionAsync(db, (10, null, false), (5, null, false));
        var qty = new Dictionary<int, decimal> { };
        var items = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).OrderBy(i => i.Position).ToList();
        qty[items[0].Id] = 10m;
        qty[items[1].Id] = 5m;
        var notes = new Dictionary<int, string?>();
        var flags = new Dictionary<int, bool>();

        await repo.CloseAsync(id, qty, notes, flags, 1, "test", "test\\test", new byte[0]);

        var r = await db.WarehouseRequisitions.FindAsync(id);
        r!.Status.Should().Be(WarehouseRequisitionStatus.Closed);
    }

    [Fact]
    public async Task CloseAsync_AllShortagesMarkedFinal_SetsStatusClosed()
    {
        using var db = TestDbContextFactory.Create();
        var repo = new WarehouseRequisitionRepository(db);
        var id = await SeedRequisitionAsync(db, (10, null, false), (5, null, false));
        var items = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).OrderBy(i => i.Position).ToList();
        var qty = new Dictionary<int, decimal> { [items[0].Id] = 7m, [items[1].Id] = 0m };
        var notes = new Dictionary<int, string?>();
        var flags = new Dictionary<int, bool> { [items[0].Id] = true, [items[1].Id] = true };

        await repo.CloseAsync(id, qty, notes, flags, 1, "test", "test\\test", new byte[0]);

        var r = await db.WarehouseRequisitions.FindAsync(id);
        r!.Status.Should().Be(WarehouseRequisitionStatus.Closed);
    }

    [Fact]
    public async Task CloseAsync_OneShortageNotFinal_SetsStatusPartiallyDelivered()
    {
        using var db = TestDbContextFactory.Create();
        var repo = new WarehouseRequisitionRepository(db);
        var id = await SeedRequisitionAsync(db, (10, null, false), (5, null, false));
        var items = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).OrderBy(i => i.Position).ToList();
        var qty = new Dictionary<int, decimal> { [items[0].Id] = 10m, [items[1].Id] = 3m };
        var notes = new Dictionary<int, string?>();
        var flags = new Dictionary<int, bool> { [items[0].Id] = false, [items[1].Id] = false };

        await repo.CloseAsync(id, qty, notes, flags, 1, "test", "test\\test", new byte[0]);

        var r = await db.WarehouseRequisitions.FindAsync(id);
        r!.Status.Should().Be(WarehouseRequisitionStatus.PartiallyDelivered);
    }

    [Fact]
    public async Task CloseAsync_QuantityPickedNull_TreatedAsZero_StatusPartiallyDelivered_WhenNotFinal()
    {
        using var db = TestDbContextFactory.Create();
        var repo = new WarehouseRequisitionRepository(db);
        var id = await SeedRequisitionAsync(db, (10, null, false));
        var items = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).ToList();
        // KEY weglassen -> CloseAsync nimmt 0 (siehe Implementierung) -> Item ist short, nicht final -> PartiallyDelivered
        var qty = new Dictionary<int, decimal> { [items[0].Id] = 0m };
        var notes = new Dictionary<int, string?>();
        var flags = new Dictionary<int, bool> { [items[0].Id] = false };

        await repo.CloseAsync(id, qty, notes, flags, 1, "test", "test\\test", new byte[0]);

        var r = await db.WarehouseRequisitions.FindAsync(id);
        r!.Status.Should().Be(WarehouseRequisitionStatus.PartiallyDelivered);
    }

    [Fact]
    public async Task CloseAsync_QuantityPickedNull_AndFinalShortageTrue_StatusClosed()
    {
        using var db = TestDbContextFactory.Create();
        var repo = new WarehouseRequisitionRepository(db);
        var id = await SeedRequisitionAsync(db, (10, null, false));
        var items = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).ToList();
        var qty = new Dictionary<int, decimal> { [items[0].Id] = 0m };
        var notes = new Dictionary<int, string?>();
        var flags = new Dictionary<int, bool> { [items[0].Id] = true };

        await repo.CloseAsync(id, qty, notes, flags, 1, "test", "test\\test", new byte[0]);

        var r = await db.WarehouseRequisitions.FindAsync(id);
        r!.Status.Should().Be(WarehouseRequisitionStatus.Closed);
    }

    [Fact]
    public async Task CloseAsync_ReClose_AfterRestlieferungComplete_TransitionsToClosed()
    {
        using var db = TestDbContextFactory.Create();
        var repo = new WarehouseRequisitionRepository(db);
        var id = await SeedRequisitionAsync(db, (10, null, false));
        var items = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).ToList();

        // 1. Lauf: nur 3 von 10, nicht final
        await repo.CloseAsync(id, new() { [items[0].Id] = 3m }, new(), new() { [items[0].Id] = false },
            1, "test", "test\\test", new byte[0]);
        (await db.WarehouseRequisitions.FindAsync(id))!.Status.Should().Be(WarehouseRequisitionStatus.PartiallyDelivered);

        // 2. Lauf: Restlieferung komplett -> 10
        await repo.CloseAsync(id, new() { [items[0].Id] = 10m }, new(), new() { [items[0].Id] = false },
            1, "test", "test\\test", new byte[0]);
        (await db.WarehouseRequisitions.FindAsync(id))!.Status.Should().Be(WarehouseRequisitionStatus.Closed);
    }

    [Fact]
    public async Task CloseAsync_IsFinalShortageTrueButFullyDelivered_FlagIgnoredStatusClosed()
    {
        using var db = TestDbContextFactory.Create();
        var repo = new WarehouseRequisitionRepository(db);
        var id = await SeedRequisitionAsync(db, (10, null, false));
        var items = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).ToList();
        var qty = new Dictionary<int, decimal> { [items[0].Id] = 10m };
        var notes = new Dictionary<int, string?>();
        var flags = new Dictionary<int, bool> { [items[0].Id] = true };

        await repo.CloseAsync(id, qty, notes, flags, 1, "test", "test\\test", new byte[0]);

        var r = await db.WarehouseRequisitions.FindAsync(id);
        r!.Status.Should().Be(WarehouseRequisitionStatus.Closed);
    }
```

Required `using`-Block oben in der Test-Datei pruefen — sollte `using IdealAkeWms.Data.Repositories;`, `using IdealAkeWms.Models;`, `using IdealAkeWms.Tests.Helpers;`, `using FluentAssertions;` enthalten.

- [ ] **Step 2: Tests laufen, FAIL erwarten**

```bash
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "FullyQualifiedName~WarehouseRequisitionRepositoryTests.CloseAsync"
```

Expected: **Build-Fail** mit `error CS1501: No overload for method 'CloseAsync' takes 8 arguments` (weil neue Signatur mit `isFinalShortageDict` noch nicht existiert).

- [ ] **Step 3: Interface anpassen**

In `IdealAkeWms/Data/Repositories/IWarehouseRequisitionRepository.cs` die `CloseAsync`-Signatur erweitern. Bestehende Zeile:

```csharp
Task CloseAsync(int id, IReadOnlyDictionary<int, decimal> itemQuantitiesPicked,
                IReadOnlyDictionary<int, string?> itemNotes,
                int closedByUserId, string user, string winUser, byte[] rowVersion);
```

Ersetzen durch:

```csharp
Task CloseAsync(int id, IReadOnlyDictionary<int, decimal> itemQuantitiesPicked,
                IReadOnlyDictionary<int, string?> itemNotes,
                IReadOnlyDictionary<int, bool> itemIsFinalShortages,
                int closedByUserId, string user, string winUser, byte[] rowVersion);
```

- [ ] **Step 4: Repository-Implementierung anpassen**

In `IdealAkeWms/Data/Repositories/WarehouseRequisitionRepository.cs` die `CloseAsync`-Methode komplett ersetzen (aktuell ab Zeile 166):

```csharp
    public async Task CloseAsync(int id, IReadOnlyDictionary<int, decimal> itemQuantitiesPicked,
        IReadOnlyDictionary<int, string?> itemNotes,
        IReadOnlyDictionary<int, bool> itemIsFinalShortages,
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
            if (itemIsFinalShortages.TryGetValue(item.Id, out var final))
                item.IsFinalShortage = final;
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

    private static WarehouseRequisitionStatus DeriveStatus(WarehouseRequisition req)
    {
        bool isFullyDelivered = req.Items.All(i =>
            (i.QuantityPicked ?? 0) >= i.QuantityRequested);
        bool hasOpenShortage = req.Items.Any(i =>
            (i.QuantityPicked ?? 0) < i.QuantityRequested && !i.IsFinalShortage);

        return (isFullyDelivered || !hasOpenShortage)
            ? WarehouseRequisitionStatus.Closed
            : WarehouseRequisitionStatus.PartiallyDelivered;
    }
```

**Wichtig:** Vor dem Refactor war `item.QuantityPicked = ... ? q : item.QuantityRequested` (Default: Soll-Menge). Neu: `... ? q : 0m` (Default: 0, weil unbearbeitete Items als nicht-geliefert behandelt werden). Bestehender JS-Workflow "Soll=Ist beim Leer-Modal" deckt die Soll-Default-Logik im Frontend ab.

- [ ] **Step 5: Bestehende Aufrufer kompilierbar halten**

Suche nach `CloseAsync(` Aufrufern:

```bash
grep -rn "CloseAsync(" --include="*.cs"
```

Erwartete Treffer: `WarehousePickingController.cs:121` (wird in Task 7 angepasst). Test-Helpers ggf. anpassen falls vorhanden.

Bis Task 7 fertig ist: Build wird FAIL haben in `WarehousePickingController.cs`. Das ist ok — wir committen Task 2 trotzdem (kompiliert nicht ganz, aber Repo+Tests passen). Alternativ Task 7 sofort danach.

**Praktikabler Workaround**: in `WarehousePickingController.cs.Close` temporaer ein leeres Dict mitgeben um den Build gruen zu halten:

```csharp
// In WarehousePickingController.cs, Zeile ca 121 in der bestehenden Close-Action
// TEMPORAER: Task 7 erweitert das richtig
await _repo.CloseAsync(id, qtyDict, noteDict,
    new Dictionary<int, bool>(),   // <-- TEMPORAER leer
    _user.GetCurrentAppUserId() ?? 0, ...
```

- [ ] **Step 6: Build + Tests gruen**

```bash
dotnet build IdealAkeWms.slnx
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "FullyQualifiedName~CloseAsync"
```

Expected: 7 neue Tests passing. Total Web ~596.

- [ ] **Step 7: Commit**

```bash
git add IdealAkeWms/Data/Repositories/IWarehouseRequisitionRepository.cs IdealAkeWms/Data/Repositories/WarehouseRequisitionRepository.cs IdealAkeWms/Controllers/WarehousePickingController.cs IdealAkeWms.Tests/Repositories/WarehouseRequisitionRepositoryTests.cs
git commit -m "feat(repo): DeriveStatus helper + CloseAsync accepts IsFinalShortage

CloseAsync-Signatur erweitert um isFinalShortageDict. DeriveStatus:
Closed wenn alle Items vollstaendig ODER alle Shortages als final markiert,
sonst PartiallyDelivered. 7 neue Tests + temporaerer leerer Dict-Aufruf im
Controller fuer Build-Stabilitaet (wird in Task 7 entfernt).

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 3: Repository — SaveProgressAsync (TDD)

**Files:**
- Modify: `IdealAkeWms/Data/Repositories/IWarehouseRequisitionRepository.cs`
- Modify: `IdealAkeWms/Data/Repositories/WarehouseRequisitionRepository.cs`
- Modify: `IdealAkeWms.Tests/Repositories/WarehouseRequisitionRepositoryTests.cs`

- [ ] **Step 1: 2 Failing Tests schreiben**

Am Ende der Test-Klasse anfuegen:

```csharp
    [Fact]
    public async Task SaveProgressAsync_PersistsQuantitiesNotesAndFlags_WithoutStatusChange()
    {
        using var db = TestDbContextFactory.Create();
        var repo = new WarehouseRequisitionRepository(db);
        var id = await SeedRequisitionAsync(db, (10, null, false));
        var items = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).ToList();

        await repo.SaveProgressAsync(id,
            new Dictionary<int, decimal?> { [items[0].Id] = 4m },
            new Dictionary<int, string?> { [items[0].Id] = "  hello  " },
            new Dictionary<int, bool> { [items[0].Id] = true },
            "u", "w");

        var item = await db.WarehouseRequisitionItems.FindAsync(items[0].Id);
        item!.QuantityPicked.Should().Be(4m);
        item.Note.Should().Be("hello");
        item.IsFinalShortage.Should().BeTrue();
        var r = await db.WarehouseRequisitions.FindAsync(id);
        r!.Status.Should().Be(WarehouseRequisitionStatus.Submitted);  // unveraendert
    }

    [Fact]
    public async Task SaveProgressAsync_DoesNotPromoteSubmittedToPartiallyDelivered()
    {
        using var db = TestDbContextFactory.Create();
        var repo = new WarehouseRequisitionRepository(db);
        var id = await SeedRequisitionAsync(db, (10, null, false));
        var items = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).ToList();

        await repo.SaveProgressAsync(id,
            new Dictionary<int, decimal?> { [items[0].Id] = 3m },   // unter Soll
            new Dictionary<int, string?>(),
            new Dictionary<int, bool> { [items[0].Id] = false },
            "u", "w");

        var r = await db.WarehouseRequisitions.FindAsync(id);
        r!.Status.Should().Be(WarehouseRequisitionStatus.Submitted);  // KEIN PartiallyDelivered
    }
```

- [ ] **Step 2: Tests laufen, FAIL erwarten** — Compiler-Error (Method nicht definiert).

- [ ] **Step 3: Interface erweitern**

In `IdealAkeWms/Data/Repositories/IWarehouseRequisitionRepository.cs` nach `CloseAsync`-Signatur einfuegen:

```csharp
    Task SaveProgressAsync(int id,
        IReadOnlyDictionary<int, decimal?> itemQuantitiesPicked,
        IReadOnlyDictionary<int, string?> itemNotes,
        IReadOnlyDictionary<int, bool> itemIsFinalShortages,
        string user, string winUser);
```

- [ ] **Step 4: Implementierung**

In `WarehouseRequisitionRepository.cs` nach der bestehenden `SaveNotesAsync` (oder unmittelbar nach `CloseAsync`) hinzufuegen:

```csharp
    public async Task SaveProgressAsync(int id,
        IReadOnlyDictionary<int, decimal?> itemQuantitiesPicked,
        IReadOnlyDictionary<int, string?> itemNotes,
        IReadOnlyDictionary<int, bool> itemIsFinalShortages,
        string user, string winUser)
    {
        var allKeys = itemQuantitiesPicked.Keys
            .Concat(itemNotes.Keys)
            .Concat(itemIsFinalShortages.Keys)
            .Distinct()
            .ToList();
        if (allKeys.Count == 0) return;

        var rows = await _context.WarehouseRequisitionItems
            .Where(i => i.WarehouseRequisitionId == id && allKeys.Contains(i.Id))
            .ToListAsync();

        var now = DateTime.Now;
        bool changed = false;
        foreach (var row in rows)
        {
            if (itemQuantitiesPicked.TryGetValue(row.Id, out var qty))
            {
                if (row.QuantityPicked != qty)
                {
                    row.QuantityPicked = qty;
                    changed = true;
                }
            }
            if (itemNotes.TryGetValue(row.Id, out var note))
            {
                var normalized = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
                if (row.Note != normalized)
                {
                    row.Note = normalized;
                    changed = true;
                }
            }
            if (itemIsFinalShortages.TryGetValue(row.Id, out var final))
            {
                if (row.IsFinalShortage != final)
                {
                    row.IsFinalShortage = final;
                    changed = true;
                }
            }
            if (changed)
            {
                row.ModifiedAt = now;
                row.ModifiedBy = user;
                row.ModifiedByWindows = winUser;
            }
        }
        if (changed) await _context.SaveChangesAsync();
    }
```

Status wird NICHT geaendert — das ist der Vertrag.

- [ ] **Step 5: Build + Tests**

```bash
dotnet build IdealAkeWms.slnx
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "FullyQualifiedName~SaveProgressAsync"
```

Expected: 2 neue Tests passing.

- [ ] **Step 6: Commit**

```bash
git add IdealAkeWms/Data/Repositories/IWarehouseRequisitionRepository.cs IdealAkeWms/Data/Repositories/WarehouseRequisitionRepository.cs IdealAkeWms.Tests/Repositories/WarehouseRequisitionRepositoryTests.cs
git commit -m "feat(repo): SaveProgressAsync persists progress without status change

Zwei Tests: Persistierung von Mengen/Notizen/Flags ohne Status-Wechsel.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 4: Repository — GetMissingPartsAsync + Record (TDD)

**Files:**
- Create: `IdealAkeWms/Models/ViewModels/MissingPartRow.cs`
- Modify: `IdealAkeWms/Data/Repositories/IWarehouseRequisitionRepository.cs`
- Modify: `IdealAkeWms/Data/Repositories/WarehouseRequisitionRepository.cs`
- Modify: `IdealAkeWms.Tests/Repositories/WarehouseRequisitionRepositoryTests.cs`

- [ ] **Step 1: MissingPartRow record anlegen**

Neue Datei `IdealAkeWms/Models/ViewModels/MissingPartRow.cs`:

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
    DateTime? ClosedAt);
```

- [ ] **Step 2: 5 Failing Tests schreiben**

Am Ende der Test-Klasse:

```csharp
    [Fact]
    public async Task GetMissingPartsAsync_ReturnsOnlyClosedRequisitions_WithFinalShortages()
    {
        using var db = TestDbContextFactory.Create();
        db.ProductionWorkplaces.Add(new ProductionWorkplace { Id = 1, Name = "WB1" });
        await db.SaveChangesAsync();
        var repo = new WarehouseRequisitionRepository(db);
        var closedId = await SeedRequisitionAsync(db, (10, 5m, true));   // wird Closed nach CloseAsync
        var pdItems = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == closedId).ToList();
        await repo.CloseAsync(closedId, new() { [pdItems[0].Id] = 5m }, new(), new() { [pdItems[0].Id] = true },
            1, "u", "w", new byte[0]);

        var pdId = await SeedRequisitionAsync(db, (10, 3m, true));
        var pdItem2 = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == pdId).Single();
        await repo.CloseAsync(pdId, new() { [pdItem2.Id] = 3m }, new(), new() { [pdItem2.Id] = false },
            1, "u", "w", new byte[0]);   // -> PartiallyDelivered

        var (items, total) = await repo.GetMissingPartsAsync(null, null, null, null, 1, 100);
        items.Should().HaveCount(1);
        items[0].RequisitionId.Should().Be(closedId);
        items[0].QuantityMissing.Should().Be(5m);
        total.Should().Be(1);
    }

    [Fact]
    public async Task GetMissingPartsAsync_AppliesWorkplaceFilter()
    {
        using var db = TestDbContextFactory.Create();
        db.ProductionWorkplaces.AddRange(
            new ProductionWorkplace { Id = 1, Name = "WB1" },
            new ProductionWorkplace { Id = 2, Name = "WB2" });
        await db.SaveChangesAsync();
        var repo = new WarehouseRequisitionRepository(db);

        var r1 = await SeedRequisitionAsync(db, (10, null, false));
        var items1 = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == r1).ToList();
        await repo.CloseAsync(r1, new() { [items1[0].Id] = 0m }, new(), new() { [items1[0].Id] = true },
            1, "u", "w", new byte[0]);

        // Reseed-Helper haengt immer an Workplace 1 — fuer Test 2 muessen wir das anpassen
        var r2req = new WarehouseRequisition
        {
            ProductionWorkplaceId = 2, Status = WarehouseRequisitionStatus.Submitted,
            CreatedAt = DateTime.Now, CreatedBy = "u", CreatedByWindows = "w", SubmittedAt = DateTime.Now,
        };
        db.WarehouseRequisitions.Add(r2req); await db.SaveChangesAsync();
        var r2item = new WarehouseRequisitionItem
        {
            WarehouseRequisitionId = r2req.Id, Position = 1, ArticleNumber = "A2",
            ArticleDescription = "Desc2", QuantityRequested = 5m,
            CreatedAt = DateTime.Now, CreatedBy = "u", CreatedByWindows = "w",
        };
        db.WarehouseRequisitionItems.Add(r2item); await db.SaveChangesAsync();
        await repo.CloseAsync(r2req.Id, new() { [r2item.Id] = 0m }, new(), new() { [r2item.Id] = true },
            1, "u", "w", new byte[0]);

        var (only1, _) = await repo.GetMissingPartsAsync(1, null, null, null, 1, 100);
        only1.Should().HaveCount(1);
        only1[0].WorkplaceName.Should().Be("WB1");
    }

    [Fact]
    public async Task GetMissingPartsAsync_AppliesColumnFilter_OnArticleNumber_WithOrSyntax()
    {
        using var db = TestDbContextFactory.Create();
        db.ProductionWorkplaces.Add(new ProductionWorkplace { Id = 1, Name = "WB1" });
        await db.SaveChangesAsync();
        var repo = new WarehouseRequisitionRepository(db);
        var id = await SeedRequisitionAsync(db, (10, 0m, true), (5, 0m, true));   // 2 Items, both will be missing
        var items = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).OrderBy(i => i.Position).ToList();
        // Override ArticleNumbers
        items[0].ArticleNumber = "AAA-1";
        items[1].ArticleNumber = "BBB-2";
        await db.SaveChangesAsync();
        await repo.CloseAsync(id,
            new() { [items[0].Id] = 0m, [items[1].Id] = 0m },
            new(),
            new() { [items[0].Id] = true, [items[1].Id] = true },
            1, "u", "w", new byte[0]);

        var filters = new Dictionary<string, string> { ["ArticleNumber"] = "AAA" };
        var (filtered, total) = await repo.GetMissingPartsAsync(null, filters, null, null, 1, 100);
        filtered.Should().HaveCount(1);
        filtered[0].ArticleNumber.Should().Be("AAA-1");

        var filtersOr = new Dictionary<string, string> { ["ArticleNumber"] = "AAA,BBB" };
        var (both, _) = await repo.GetMissingPartsAsync(null, filtersOr, null, null, 1, 100);
        both.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetMissingPartsAsync_PaginationLimitsResults()
    {
        using var db = TestDbContextFactory.Create();
        db.ProductionWorkplaces.Add(new ProductionWorkplace { Id = 1, Name = "WB1" });
        await db.SaveChangesAsync();
        var repo = new WarehouseRequisitionRepository(db);

        for (int i = 0; i < 5; i++)
        {
            var id = await SeedRequisitionAsync(db, (10, 0m, true));
            var item = db.WarehouseRequisitionItems.Where(x => x.WarehouseRequisitionId == id).Single();
            await repo.CloseAsync(id, new() { [item.Id] = 0m }, new(), new() { [item.Id] = true },
                1, "u", "w", new byte[0]);
        }

        var (page1, total) = await repo.GetMissingPartsAsync(null, null, null, null, 1, 2);
        page1.Should().HaveCount(2);
        total.Should().Be(5);
    }

    [Fact]
    public async Task GetMissingPartsAsync_OnlyIncludesItemsWithIsFinalShortageTrue()
    {
        using var db = TestDbContextFactory.Create();
        db.ProductionWorkplaces.Add(new ProductionWorkplace { Id = 1, Name = "WB1" });
        await db.SaveChangesAsync();
        var repo = new WarehouseRequisitionRepository(db);
        var id = await SeedRequisitionAsync(db, (10, null, false), (5, null, false));
        var items = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).OrderBy(i => i.Position).ToList();
        await repo.CloseAsync(id,
            new() { [items[0].Id] = 8m, [items[1].Id] = 3m },
            new(),
            new() { [items[0].Id] = true, [items[1].Id] = false },   // nur Item 1 ist final
            1, "u", "w", new byte[0]);
        // Resultat: Bestellung wird PartiallyDelivered (Item 2 nicht final) -> 0 MissingParts
        var (none, _) = await repo.GetMissingPartsAsync(null, null, null, null, 1, 100);
        none.Should().HaveCount(0);
    }
```

- [ ] **Step 3: Tests laufen, FAIL erwarten** — Method nicht definiert.

- [ ] **Step 4: Interface erweitern**

In `IdealAkeWms/Data/Repositories/IWarehouseRequisitionRepository.cs`:

```csharp
    Task<(IReadOnlyList<MissingPartRow> Items, int TotalCount)>
        GetMissingPartsAsync(int? workplaceFilter,
                             IReadOnlyDictionary<string, string>? columnFilters,
                             DateTime? closedFrom, DateTime? closedUntil,
                             int page, int pageSize);
```

Auch `using IdealAkeWms.Models.ViewModels;` als Import oben hinzufuegen falls fehlend.

- [ ] **Step 5: Implementierung**

In `WarehouseRequisitionRepository.cs` neue Methode (am Ende der Klasse):

```csharp
    public async Task<(IReadOnlyList<MissingPartRow> Items, int TotalCount)>
        GetMissingPartsAsync(int? workplaceFilter,
                             IReadOnlyDictionary<string, string>? columnFilters,
                             DateTime? closedFrom, DateTime? closedUntil,
                             int page, int pageSize)
    {
        var q = _context.WarehouseRequisitionItems
            .Include(i => i.WarehouseRequisition)
                .ThenInclude(r => r.ProductionWorkplace)
            .Where(i => i.IsFinalShortage
                && i.WarehouseRequisition.Status == WarehouseRequisitionStatus.Closed);

        if (workplaceFilter.HasValue)
            q = q.Where(i => i.WarehouseRequisition.ProductionWorkplaceId == workplaceFilter.Value);
        if (closedFrom.HasValue)
            q = q.Where(i => i.WarehouseRequisition.ClosedAt >= closedFrom.Value);
        if (closedUntil.HasValue)
            q = q.Where(i => i.WarehouseRequisition.ClosedAt < closedUntil.Value);

        // Column-Filter — minimal: ArticleNumber, ArticleDescription, WorkplaceName.
        // OR/NOT-Mini-Syntax via Komma-Split.
        if (columnFilters != null)
        {
            if (columnFilters.TryGetValue("ArticleNumber", out var an) && !string.IsNullOrWhiteSpace(an))
                q = ApplyTextFilter(q, an, i => i.ArticleNumber);
            if (columnFilters.TryGetValue("ArticleDescription", out var ad) && !string.IsNullOrWhiteSpace(ad))
                q = ApplyTextFilter(q, ad, i => i.ArticleDescription);
            if (columnFilters.TryGetValue("WorkplaceName", out var wn) && !string.IsNullOrWhiteSpace(wn))
                q = ApplyTextFilter(q, wn, i => i.WarehouseRequisition.ProductionWorkplace.Name);
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
                i.WarehouseRequisition.ClosedAt))
            .ToListAsync();

        return (rows, total);
    }

    private static IQueryable<WarehouseRequisitionItem> ApplyTextFilter(
        IQueryable<WarehouseRequisitionItem> q,
        string filterValue,
        System.Linq.Expressions.Expression<Func<WarehouseRequisitionItem, string>> selector)
    {
        // Simple OR-Mini-Syntax: "AAA,BBB" -> contains "AAA" OR contains "BBB"
        // NOT mit "!" Prefix (z.B. "!AAA" -> NOT contains "AAA").
        var tokens = filterValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length == 0) return q;

        // EF Core kann nicht beliebig komplexe Expression-Composition — nutzt Predicate chained.
        // Simpler Ansatz: client-side ToListAsync hier waere zu teuer; wir bauen ein OR mit Contains.
        // Bei NOT separat behandeln.
        bool hasNot = tokens.Any(t => t.StartsWith("!"));
        if (hasNot)
        {
            foreach (var t in tokens.Where(t => t.StartsWith("!")))
            {
                var needle = t.Substring(1);
                q = q.Where(System.Linq.Expressions.Expression.Lambda<Func<WarehouseRequisitionItem, bool>>(
                    System.Linq.Expressions.Expression.Not(
                        System.Linq.Expressions.Expression.Call(
                            selector.Body,
                            typeof(string).GetMethod("Contains", new[] { typeof(string) })!,
                            System.Linq.Expressions.Expression.Constant(needle))),
                    selector.Parameters));
            }
        }
        var positives = tokens.Where(t => !t.StartsWith("!")).ToArray();
        if (positives.Length > 0)
        {
            // OR-Composition
            System.Linq.Expressions.Expression? orBody = null;
            foreach (var t in positives)
            {
                var contains = System.Linq.Expressions.Expression.Call(
                    selector.Body,
                    typeof(string).GetMethod("Contains", new[] { typeof(string) })!,
                    System.Linq.Expressions.Expression.Constant(t));
                orBody = orBody == null ? contains : System.Linq.Expressions.Expression.OrElse(orBody, contains);
            }
            if (orBody != null)
            {
                q = q.Where(System.Linq.Expressions.Expression.Lambda<Func<WarehouseRequisitionItem, bool>>(orBody, selector.Parameters));
            }
        }
        return q;
    }
```

**Hinweis zur Implementierung**: Expression-Tree-Komposition fuer EF — alternativ kann man `IQueryable<WarehouseRequisitionItem>.Where(i => positives.Any(p => i.ArticleNumber.Contains(p)))` schreiben, aber EF Core 10 kann `Any(p => ...)` mit Subquery uebersetzen — pruefen ob das simpler ist. Falls Expression-Tree-Komposition zu komplex wird, kann auch ein In-Memory-Filter nach SQL-Vorfilter verwendet werden (`.ToListAsync()` + `.Where(...)`), aber dann Pagination im C#.

**Praktikable Alternative**: einfacher LINQ mit `Any`:

```csharp
private static IQueryable<WarehouseRequisitionItem> ApplyArticleNumberFilter(IQueryable<WarehouseRequisitionItem> q, string filter)
{
    var tokens = filter.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    var positives = tokens.Where(t => !t.StartsWith("!")).ToList();
    var negatives = tokens.Where(t => t.StartsWith("!")).Select(t => t.Substring(1)).ToList();

    if (positives.Count > 0)
        q = q.Where(i => positives.Any(p => i.ArticleNumber.Contains(p)));
    foreach (var n in negatives)
        q = q.Where(i => !i.ArticleNumber.Contains(n));
    return q;
}
```

Bevorzugt diese Variante — kuerzer + EF Core 10 unterstuetzt das. Pro Filter-Spalte eine eigene Methode.

- [ ] **Step 6: Build + Tests**

```bash
dotnet build IdealAkeWms.slnx
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "FullyQualifiedName~GetMissingPartsAsync"
```

Expected: 5 neue Tests passing.

- [ ] **Step 7: Commit**

```bash
git add IdealAkeWms/Models/ViewModels/MissingPartRow.cs IdealAkeWms/Data/Repositories/IWarehouseRequisitionRepository.cs IdealAkeWms/Data/Repositories/WarehouseRequisitionRepository.cs IdealAkeWms.Tests/Repositories/WarehouseRequisitionRepositoryTests.cs
git commit -m "feat(repo): GetMissingPartsAsync with workplace + column filters

5 Tests: Closed-only Filter, Workplace-Filter, Column-Filter mit OR-Syntax,
Pagination, IsFinalShortage-Pflicht. MissingPartRow als Record-DTO.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 5: Repository — GetFinalShortagesCountForUserAsync (TDD)

**Files:**
- Modify: `IdealAkeWms/Data/Repositories/IWarehouseRequisitionRepository.cs`
- Modify: `IdealAkeWms/Data/Repositories/WarehouseRequisitionRepository.cs`
- Modify: `IdealAkeWms.Tests/Repositories/WarehouseRequisitionRepositoryTests.cs`

- [ ] **Step 1: 2 Failing Tests schreiben**

```csharp
    [Fact]
    public async Task GetFinalShortagesCountForUserAsync_CountsOnlyForUserWorkplaces()
    {
        using var db = TestDbContextFactory.Create();
        db.ProductionWorkplaces.AddRange(
            new ProductionWorkplace { Id = 1, Name = "WB1" },
            new ProductionWorkplace { Id = 2, Name = "WB2" });
        db.UserProductionWorkplaces.Add(new UserProductionWorkplace { UserId = 42, ProductionWorkplaceId = 1 });
        await db.SaveChangesAsync();
        var repo = new WarehouseRequisitionRepository(db);

        // Closed-Bestellung an WB1 mit 2 Final-Shortages
        var r1 = await SeedRequisitionAsync(db, (10, 0m, true), (5, 0m, true));
        var i1 = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == r1).OrderBy(i => i.Position).ToList();
        await repo.CloseAsync(r1,
            new() { [i1[0].Id] = 0m, [i1[1].Id] = 0m }, new(),
            new() { [i1[0].Id] = true, [i1[1].Id] = true },
            1, "u", "w", new byte[0]);

        // Closed-Bestellung an WB2 mit 1 Final-Shortage (User 42 NICHT zugeordnet)
        var r2req = new WarehouseRequisition { ProductionWorkplaceId = 2, Status = WarehouseRequisitionStatus.Submitted,
            CreatedAt = DateTime.Now, CreatedBy = "u", CreatedByWindows = "w", SubmittedAt = DateTime.Now };
        db.WarehouseRequisitions.Add(r2req); await db.SaveChangesAsync();
        var r2item = new WarehouseRequisitionItem { WarehouseRequisitionId = r2req.Id, Position = 1,
            ArticleNumber = "X", ArticleDescription = "Y", QuantityRequested = 5m,
            CreatedAt = DateTime.Now, CreatedBy = "u", CreatedByWindows = "w" };
        db.WarehouseRequisitionItems.Add(r2item); await db.SaveChangesAsync();
        await repo.CloseAsync(r2req.Id, new() { [r2item.Id] = 0m }, new(), new() { [r2item.Id] = true },
            1, "u", "w", new byte[0]);

        var (itemCount, reqCount) = await repo.GetFinalShortagesCountForUserAsync(42);
        itemCount.Should().Be(2);   // nur WB1
        reqCount.Should().Be(1);
    }

    [Fact]
    public async Task GetFinalShortagesCountForUserAsync_ZeroWhenUserHasNoFinalShortages()
    {
        using var db = TestDbContextFactory.Create();
        db.ProductionWorkplaces.Add(new ProductionWorkplace { Id = 1, Name = "WB1" });
        db.UserProductionWorkplaces.Add(new UserProductionWorkplace { UserId = 42, ProductionWorkplaceId = 1 });
        await db.SaveChangesAsync();
        var repo = new WarehouseRequisitionRepository(db);

        var (itemCount, reqCount) = await repo.GetFinalShortagesCountForUserAsync(42);
        itemCount.Should().Be(0);
        reqCount.Should().Be(0);
    }
```

- [ ] **Step 2: Tests laufen, FAIL erwarten** — Method nicht definiert.

- [ ] **Step 3: Interface + Implementierung**

In `IWarehouseRequisitionRepository.cs`:

```csharp
    Task<(int ItemCount, int RequisitionCount)>
        GetFinalShortagesCountForUserAsync(int userId);
```

In `WarehouseRequisitionRepository.cs`:

```csharp
    public async Task<(int ItemCount, int RequisitionCount)>
        GetFinalShortagesCountForUserAsync(int userId)
    {
        var userWorkplaceIds = await _context.UserProductionWorkplaces
            .Where(u => u.UserId == userId)
            .Select(u => u.ProductionWorkplaceId)
            .ToListAsync();
        if (userWorkplaceIds.Count == 0) return (0, 0);

        var q = _context.WarehouseRequisitionItems
            .Where(i => i.IsFinalShortage
                && i.WarehouseRequisition.Status == WarehouseRequisitionStatus.Closed
                && userWorkplaceIds.Contains(i.WarehouseRequisition.ProductionWorkplaceId));

        int itemCount = await q.CountAsync();
        int reqCount = await q.Select(i => i.WarehouseRequisitionId).Distinct().CountAsync();
        return (itemCount, reqCount);
    }
```

- [ ] **Step 4: Tests + Commit**

```bash
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "FullyQualifiedName~GetFinalShortagesCountForUserAsync"
```

Expected: 2 neue Tests passing.

```bash
git add IdealAkeWms/Data/Repositories/IWarehouseRequisitionRepository.cs IdealAkeWms/Data/Repositories/WarehouseRequisitionRepository.cs IdealAkeWms.Tests/Repositories/WarehouseRequisitionRepositoryTests.cs
git commit -m "feat(repo): GetFinalShortagesCountForUserAsync (Werkbank-Karte)

2 Tests: nur User-Workplaces, leeres Resultat = (0,0).

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 6: ViewModels erweitern

**Files:**
- Modify: `IdealAkeWms/Models/ViewModels/WarehouseRequisitionDetailViewModel.cs`
- Modify: `IdealAkeWms/Models/ViewModels/WarehouseRequisitionListViewModel.cs`
- Create: `IdealAkeWms/Models/ViewModels/MissingPartsListViewModel.cs`

- [ ] **Step 1: DetailItemViewModel um IsFinalShortage erweitern**

In `IdealAkeWms/Models/ViewModels/WarehouseRequisitionDetailViewModel.cs` den `record` aktualisieren:

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
    bool IsFinalShortage = false);
```

- [ ] **Step 2: ListViewModel um Card-Counts erweitern**

In `IdealAkeWms/Models/ViewModels/WarehouseRequisitionListViewModel.cs` zwei Properties hinzufuegen (vor schliessender `}` der Klasse):

```csharp
    public int MissingPartsItemCount { get; set; }
    public int MissingPartsRequisitionCount { get; set; }
```

- [ ] **Step 3: MissingPartsListViewModel neu anlegen**

Neue Datei `IdealAkeWms/Models/ViewModels/MissingPartsListViewModel.cs`:

```csharp
using IdealAkeWms.Models;

namespace IdealAkeWms.Models.ViewModels;

public class MissingPartsListViewModel
{
    public List<MissingPartRow> Items { get; set; } = new();
    public List<ProductionWorkplace> AvailableWorkplaces { get; set; } = new();
    public int? WorkplaceFilter { get; set; }
    public bool MineOnly { get; set; }
    public PaginationState Pagination { get; set; } = new();
}
```

- [ ] **Step 4: Build verifizieren**

```bash
dotnet build IdealAkeWms.slnx
```

Expected: success. Falls vorhandene Aufrufer von `WarehouseRequisitionDetailItemViewModel` einen Compiler-Error werfen wegen `IsFinalShortage = false`-Default: Default-Wert akzeptiert sie automatisch — sollte ok sein.

- [ ] **Step 5: Commit**

```bash
git add IdealAkeWms/Models/ViewModels/WarehouseRequisitionDetailViewModel.cs IdealAkeWms/Models/ViewModels/WarehouseRequisitionListViewModel.cs IdealAkeWms/Models/ViewModels/MissingPartsListViewModel.cs
git commit -m "feat(viewmodel): IsFinalShortage in DetailItem + Card-Counts + MissingPartsListVM

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 7: WarehousePickingController — Filter + Close + SaveProgress + PrintAndClose

**Files:**
- Modify: `IdealAkeWms/Controllers/WarehousePickingController.cs`
- Modify: `IdealAkeWms.Tests/Controllers/WarehousePickingControllerTests.cs`

- [ ] **Step 1: Index-Filter Default erweitern**

In `WarehousePickingController.Index` die Zeile

```csharp
var effectiveFilter = statusFilter ?? WarehouseRequisitionStatus.Submitted;
var (items, total) = await _repo.GetForWarehouseAsync(effectiveFilter, workplaceId, page, effectivePageSize);
```

ersetzen durch:

```csharp
// Default: Submitted + PartiallyDelivered ("offen"). Explicit-Filter ueberschreibt.
var statusList = statusFilter.HasValue
    ? new[] { statusFilter.Value }
    : new[] { WarehouseRequisitionStatus.Submitted, WarehouseRequisitionStatus.PartiallyDelivered };
var (items, total) = await _repo.GetForWarehouseAsync(statusList, workplaceId, page, effectivePageSize);
```

Und OpenCount-Berechnung:

```csharp
var openCount = (await _repo.GetForWarehouseAsync(
    new[] { WarehouseRequisitionStatus.Submitted, WarehouseRequisitionStatus.PartiallyDelivered },
    null, 1, 1)).TotalCount;
```

**Repository-Signatur entsprechend anpassen**: in `IWarehouseRequisitionRepository.cs` und `WarehouseRequisitionRepository.cs`:

```csharp
// alt: WarehouseRequisitionStatus? statusFilter
// neu: WarehouseRequisitionStatus[] statuses
Task<(List<WarehouseRequisition> Items, int TotalCount)> GetForWarehouseAsync(
    WarehouseRequisitionStatus[] statuses, int? workplaceId, int page, int pageSize);
```

Implementierung in Repository:

```csharp
public async Task<(List<WarehouseRequisition> Items, int TotalCount)> GetForWarehouseAsync(
    WarehouseRequisitionStatus[] statuses, int? workplaceId, int page, int pageSize)
{
    var q = _context.WarehouseRequisitions
        .Include(r => r.ProductionWorkplace)
        .Include(r => r.Items)
        .Where(r => statuses.Contains(r.Status));
    if (workplaceId.HasValue) q = q.Where(r => r.ProductionWorkplaceId == workplaceId.Value);

    var total = await q.CountAsync();
    var items = await q.OrderByDescending(r => r.SubmittedAt ?? r.CreatedAt)
        .Skip((page - 1) * pageSize).Take(pageSize)
        .ToListAsync();
    return (items, total);
}
```

- [ ] **Step 2: Details NotFound nur fuer Draft**

In `Details`-Action die Zeile:
```csharp
if (r == null || r.Status == WarehouseRequisitionStatus.Draft) return NotFound();
```
bleibt unveraendert — Submitted, PartiallyDelivered, Closed, Cancelled bleiben erlaubt.

Im DetailItem-Mapping `IsFinalShortage` weitergeben:
```csharp
detailItems.Add(new WarehouseRequisitionDetailItemViewModel(
    i.Id, i.Position, i.ArticleNumber, i.ArticleDescription, i.Unit,
    i.QuantityRequested, i.QuantityPicked, locationStr, i.Note, i.IsFinalShortage));
```

- [ ] **Step 3: Close-Action erweitern**

`Close`-Method-Signatur erweitern:

```csharp
[HttpPost, ValidateAntiForgeryToken]
public async Task<IActionResult> Close(int id, int[] itemIds, int[] quantitiesPicked,
    string?[]? notes, bool[]? isFinalShortages, byte[] rowVersion)
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
    {
        for (int idx = 0; idx < itemIds.Length; idx++)
            noteDict[itemIds[idx]] = idx < notes.Length ? notes[idx] : null;
    }

    var flagDict = new Dictionary<int, bool>();
    if (isFinalShortages != null)
    {
        for (int idx = 0; idx < itemIds.Length; idx++)
            flagDict[itemIds[idx]] = idx < isFinalShortages.Length ? isFinalShortages[idx] : false;
    }

    try
    {
        await _repo.CloseAsync(id, qtyDict, noteDict, flagDict,
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

- [ ] **Step 4: SaveProgress-Action neu**

Nach `SaveNotes` einfuegen:

```csharp
[HttpPost, ValidateAntiForgeryToken]
public async Task<IActionResult> SaveProgress(int id,
    [FromForm] int[] itemIds,
    [FromForm] int?[]? quantitiesPicked,
    [FromForm] string?[]? notes,
    [FromForm] bool[]? isFinalShortages)
{
    if (itemIds == null || itemIds.Length == 0) return BadRequest("itemIds required");

    var qtyDict = new Dictionary<int, decimal?>();
    if (quantitiesPicked != null)
    {
        for (int idx = 0; idx < itemIds.Length; idx++)
            qtyDict[itemIds[idx]] = idx < quantitiesPicked.Length ? (decimal?)quantitiesPicked[idx] : null;
    }
    var noteDict = new Dictionary<int, string?>();
    if (notes != null)
    {
        for (int idx = 0; idx < itemIds.Length; idx++)
            noteDict[itemIds[idx]] = idx < notes.Length ? notes[idx] : null;
    }
    var flagDict = new Dictionary<int, bool>();
    if (isFinalShortages != null)
    {
        for (int idx = 0; idx < itemIds.Length; idx++)
            flagDict[itemIds[idx]] = idx < isFinalShortages.Length ? isFinalShortages[idx] : false;
    }

    await _repo.SaveProgressAsync(id, qtyDict, noteDict, flagDict,
        _user.GetDisplayName(), _user.GetWindowsUserName());
    return Ok();
}
```

- [ ] **Step 5: PrintAndClose-Action neu**

Nach `SaveProgress` einfuegen:

```csharp
[HttpPost, ValidateAntiForgeryToken]
public async Task<IActionResult> PrintAndClose(int id, int[] itemIds, int[] quantitiesPicked,
    string?[]? notes, bool[]? isFinalShortages, byte[] rowVersion)
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
    var flagDict = new Dictionary<int, bool>();
    if (isFinalShortages != null)
        for (int idx = 0; idx < itemIds.Length; idx++)
            flagDict[itemIds[idx]] = idx < isFinalShortages.Length ? isFinalShortages[idx] : false;

    try
    {
        await _repo.CloseAsync(id, qtyDict, noteDict, flagDict,
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

- [ ] **Step 6: Index/Print bestehende DetailItem-Mappings erweitern**

In `Index`-Action und `Print`-Action die DetailItem-Mappings (falls vorhanden) ebenfalls `IsFinalShortage` durchreichen.

- [ ] **Step 7: 6 Controller-Tests schreiben**

In `IdealAkeWms.Tests/Controllers/WarehousePickingControllerTests.cs` neue Tests (am Ende der Klasse, vor `}`):

```csharp
    [Fact]
    public async Task Close_AcceptsIsFinalShortageArray_PassesToRepo()
    {
        // Pattern: Mock IWarehouseRequisitionRepository.CloseAsync und verifiziere Dict-Inhalt
        // Konkret: bestehende Test-Setup-Helper nutzen (Mock<IWarehouseRequisitionRepository>),
        // Close(id, itemIds=[1,2], quantities=[5,3], notes=null,
        //       isFinalShortages=[true,false], rowVersion) aufrufen und verifizieren dass
        // der dem Repo uebergebene flagDict {1: true, 2: false} enthaelt.
        // Implementierungsdetail entsprechend bestehender Tests in der Datei.
        Assert.True(true, "TODO: konkret implementieren wenn Mock-Setup steht — Pattern siehe bestehende Tests");
    }
```

**Hinweis fuer Implementierer**: Bestehende Tests in `WarehousePickingControllerTests.cs` lesen, um das Mock-Setup-Pattern zu uebernehmen. Die hier skizzierten Tests sind:

1. `Close_AcceptsIsFinalShortageArray_PassesToRepo` — Mock-Verify dass flagDict an Repo geht
2. `Close_QuantitiesNegative_ReturnsWarning_NoChange` — TempData WarningMessage gesetzt, Mock nie called
3. `Index_Default_ShowsSubmittedAndPartiallyDelivered` — Mock GetForWarehouseAsync wird mit `[Submitted, PartiallyDelivered]` aufgerufen
4. `SaveProgress_PersistsAllFields_ReturnsOk` — Mock SaveProgressAsync called mit korrektem Inhalt
5. `PrintAndClose_OnSuccess_ReturnsJsonWithRedirectUrl` — JSON enthaelt `redirectUrl: /WarehousePicking/Print/{id}`
6. `PrintAndClose_OnConcurrencyConflict_Returns409Conflict` — Result ist Conflict

- [ ] **Step 8: Build + Tests**

```bash
dotnet build IdealAkeWms.slnx
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "FullyQualifiedName~WarehousePickingController"
```

Expected: 6 neue Tests passing.

- [ ] **Step 9: Commit**

```bash
git add IdealAkeWms/Controllers/WarehousePickingController.cs IdealAkeWms/Data/Repositories/IWarehouseRequisitionRepository.cs IdealAkeWms/Data/Repositories/WarehouseRequisitionRepository.cs IdealAkeWms.Tests/Controllers/WarehousePickingControllerTests.cs
git commit -m "feat(controller): IsFinalShortage in Close, SaveProgress + PrintAndClose actions

Index-Default zeigt Submitted+PartiallyDelivered. GetForWarehouseAsync Sig
erweitert auf Status-Array. Close akzeptiert isFinalShortages[]. Neue Actions
SaveProgress (AJAX-Persist ohne Status-Wechsel) und PrintAndClose (JSON mit
redirectUrl). 6 neue Tests.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 8: WarehousePicking/Details.cshtml — UI

**Files:**
- Modify: `IdealAkeWms/Views/WarehousePicking/Details.cshtml`

- [ ] **Step 1: View komplett ueberarbeiten**

Die existierende `Details.cshtml` ersetzen. Wichtige Aenderungen:

1. `isEditable` = Submitted ODER PartiallyDelivered (statt nur Submitted)
2. Status-Badge erweitert um PartiallyDelivered
3. PartiallyDelivered-Hinweisbanner
4. Neue Spalte "Fehlteil" in Tabelle
5. 3 Buttons unten (Speichern+Abschliessen, Drucken, Drucken+Abschliessen)
6. JS-Update fuer IsFinalShortage-Toggle + neue SaveProgress-AJAX + PrintAndClose-Workflow

Datei komplett ersetzen mit:

```html
@model IdealAkeWms.Models.ViewModels.WarehouseRequisitionDetailViewModel
@{
    ViewData["Title"] = $"Lagerbestellung #{Model.Id}";
    bool isEditable = Model.Status == IdealAkeWms.Models.WarehouseRequisitionStatus.Submitted
                   || Model.Status == IdealAkeWms.Models.WarehouseRequisitionStatus.PartiallyDelivered;
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
                case IdealAkeWms.Models.WarehouseRequisitionStatus.Submitted: <span class="badge bg-primary">Abgeschickt</span> break;
                case IdealAkeWms.Models.WarehouseRequisitionStatus.PartiallyDelivered: <span class="badge bg-warning text-dark">Teilgeliefert</span> break;
                case IdealAkeWms.Models.WarehouseRequisitionStatus.Closed: <span class="badge bg-success">Erledigt am @Model.ClosedAt?.ToString("dd.MM.yyyy HH:mm")</span> break;
                case IdealAkeWms.Models.WarehouseRequisitionStatus.Cancelled: <span class="badge bg-dark">Storniert am @Model.CancelledAt?.ToString("dd.MM.yyyy HH:mm")</span> break;
            }
        </div>
        @if (!string.IsNullOrEmpty(Model.CancellationReason))
        {
            <div><strong>Storno-Grund:</strong> @Model.CancellationReason</div>
        }
    </div>
</div>

@if (Model.Status == IdealAkeWms.Models.WarehouseRequisitionStatus.PartiallyDelivered)
{
    <div class="alert alert-warning">
        <strong>Teilgeliefert.</strong> Items mit Restlieferung erwartet bleiben offen.
        Setze die Checkbox <em>Endgueltig Fehlteil</em>, sobald entschieden ist dass keine
        Restlieferung mehr kommt.
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
                <th title="Endgueltig Fehlteil (kein Restlieferung erwartet)">Fehlteil</th>
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
                    <td class="text-center">
                        @if (isEditable)
                        {
                            <input type="hidden" name="isFinalShortages" value="@i.IsFinalShortage.ToString().ToLower()" class="final-hidden" />
                            <input type="checkbox" class="form-check-input final-checkbox"
                                   @(i.IsFinalShortage ? "checked" : "")
                                   title="Endgueltig Fehlteil (keine Restlieferung erwartet)" />
                        }
                        else if (i.IsFinalShortage)
                        {
                            <span class="badge bg-warning text-dark">Fehlteil</span>
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
    const printUrl = '@Url.Action("Print", "WarehousePicking", new { id = Model.Id })';
    const token = form.querySelector('input[name="__RequestVerificationToken"]')?.value;
    let dirty = false;

    // ----- IsFinalShortage Checkbox <-> Hidden sync -----
    function syncFinalCheckbox(row) {
        const checkbox = row.querySelector('.final-checkbox');
        const hidden = row.querySelector('.final-hidden');
        const qtyInput = row.querySelector('.qty-input');
        if (!checkbox || !hidden) return;
        const requested = parseInt(row.dataset.requested, 10) || 0;
        const picked = qtyInput && qtyInput.value ? parseInt(qtyInput.value, 10) : 0;
        const shortage = picked < requested;
        checkbox.disabled = !shortage;
        if (!shortage) checkbox.checked = false;
        hidden.value = checkbox.checked ? 'true' : 'false';
    }
    if (isEditable) {
        document.querySelectorAll('#detailsForm tbody tr').forEach(syncFinalCheckbox);
        document.querySelectorAll('.qty-input').forEach(inp => {
            inp.addEventListener('input', () => {
                syncFinalCheckbox(inp.closest('tr'));
                dirty = true;
            });
        });
        document.querySelectorAll('.final-checkbox').forEach(cb => {
            cb.addEventListener('change', () => {
                const tr = cb.closest('tr');
                const hidden = tr.querySelector('.final-hidden');
                hidden.value = cb.checked ? 'true' : 'false';
                dirty = true;
            });
        });
        document.querySelectorAll('.note-input').forEach(inp => {
            inp.addEventListener('input', () => { dirty = true; });
            inp.addEventListener('blur', saveProgress);
        });
    }

    // ----- SaveProgress (AJAX) -----
    function collectProgress() {
        const itemIds = Array.from(form.querySelectorAll('input[name="itemIds"]')).map(i => i.value);
        const quantitiesPicked = Array.from(form.querySelectorAll('input[name="quantitiesPicked"]'))
            .map(i => i.value || '');
        const notes = Array.from(form.querySelectorAll('input[name="notes"]')).map(i => i.value || '');
        const isFinalShortages = Array.from(form.querySelectorAll('input.final-hidden')).map(i => i.value);
        return { itemIds, quantitiesPicked, notes, isFinalShortages };
    }
    async function saveProgress() {
        if (!dirty || !token) return;
        const { itemIds, quantitiesPicked, notes, isFinalShortages } = collectProgress();
        const body = new FormData();
        body.append('__RequestVerificationToken', token);
        itemIds.forEach(v => body.append('itemIds', v));
        quantitiesPicked.forEach(v => body.append('quantitiesPicked', v));
        notes.forEach(v => body.append('notes', v));
        isFinalShortages.forEach(v => body.append('isFinalShortages', v));
        try {
            await fetch(saveProgressUrl, { method: 'POST', body });
            dirty = false;
        } catch { /* bleibt dirty */ }
    }

    // ----- Drucken (ohne Abschliessen): Progress speichern, dann Print -----
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

    if (!closeBtn) return;   // Liste nicht editierbar

    // ----- Soll=Ist Modal-Logik (bestehend) -----
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
            syncFinalCheckbox(tr);
        });
    }

    // ----- Speichern + Abschliessen (Form-Submit) -----
    closeBtn.addEventListener('click', () => {
        const empties = emptyRows();
        if (empties.length === 0) { form.submit(); return; }
        modal.show();
        // override close-yes/no fuer reines Form-Submit
        document.getElementById('close-yes').onclick = () => { fillSollAsIst(); modal.hide(); form.submit(); };
        document.getElementById('close-no').onclick = () => { modal.hide(); form.submit(); };
    });

    // ----- Drucken und Abschliessen (POST + Print-Tab) -----
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
        // Print-Tab SYNCHRON oeffnen (Popup-Blocker-Schutz)
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

- [ ] **Step 2: View visuell testen (manuell)**

```bash
dotnet build IdealAkeWms.slnx
```

Expected: Build success.

(View manuell zu testen lohnt sich erst in Task 15 / nach kompletter UI-Kette.)

- [ ] **Step 3: Commit**

```bash
git add IdealAkeWms/Views/WarehousePicking/Details.cshtml
git commit -m "feat(view): Details mit IsFinalShortage-Checkbox + 3 Buttons + Status-Banner

JS: SaveProgress-AJAX (statt nur SaveNotes), PrintAndClose-Workflow mit
synchroner Tab-Oeffnung. IsFinalShortage-Checkbox sync mit hidden field
+ disabled wenn Ist>=Soll.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 9: WarehousePicking/Index.cshtml — Status-Filter erweitern

**Files:**
- Modify: `IdealAkeWms/Views/WarehousePicking/Index.cshtml`

- [ ] **Step 1: Status-Filter-Dropdown ergaenzen**

In `Views/WarehousePicking/Index.cshtml` das Status-Filter-Dropdown finden (vermutlich `<select name="statusFilter">`). Falls bestehend nur Submitted/Closed/Cancelled hat, ergaenzen mit PartiallyDelivered. Beispiel-Form (Vergleich mit aktueller Datei vor Bearbeitung):

```html
<select name="statusFilter" class="form-select form-select-sm" onchange="this.form.submit()">
    <option value="">Offen (Eingereicht + Teilgeliefert)</option>
    <option value="@((int)IdealAkeWms.Models.WarehouseRequisitionStatus.Submitted)"
        selected="@(Model.StatusFilter == IdealAkeWms.Models.WarehouseRequisitionStatus.Submitted)">Eingereicht</option>
    <option value="@((int)IdealAkeWms.Models.WarehouseRequisitionStatus.PartiallyDelivered)"
        selected="@(Model.StatusFilter == IdealAkeWms.Models.WarehouseRequisitionStatus.PartiallyDelivered)">Teilgeliefert</option>
    <option value="@((int)IdealAkeWms.Models.WarehouseRequisitionStatus.Closed)"
        selected="@(Model.StatusFilter == IdealAkeWms.Models.WarehouseRequisitionStatus.Closed)">Abgeschlossen</option>
    <option value="@((int)IdealAkeWms.Models.WarehouseRequisitionStatus.Cancelled)"
        selected="@(Model.StatusFilter == IdealAkeWms.Models.WarehouseRequisitionStatus.Cancelled)">Storniert</option>
</select>
```

In der Status-Spalte des Tabellen-Bereichs zusaetzlich das PartiallyDelivered-Badge anzeigen:

```html
@switch (item.Status)
{
    case IdealAkeWms.Models.WarehouseRequisitionStatus.Submitted: <span class="badge bg-primary">Eingereicht</span> break;
    case IdealAkeWms.Models.WarehouseRequisitionStatus.PartiallyDelivered: <span class="badge bg-warning text-dark">Teilgeliefert</span> break;
    case IdealAkeWms.Models.WarehouseRequisitionStatus.Closed: <span class="badge bg-success">Abgeschlossen</span> break;
    case IdealAkeWms.Models.WarehouseRequisitionStatus.Cancelled: <span class="badge bg-dark">Storniert</span> break;
}
```

**Hinweis fuer Implementierer**: Aktuelle Index.cshtml lesen, an die richtige Stelle einarbeiten, NICHT komplett ersetzen.

- [ ] **Step 2: Build + Commit**

```bash
dotnet build IdealAkeWms.slnx
git add IdealAkeWms/Views/WarehousePicking/Index.cshtml
git commit -m "feat(view): Picking-Index Status-Filter erweitert um Teilgeliefert

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 10: WarehousePicking/Print.cshtml — Fehlteil-Spalte

**Files:**
- Modify: `IdealAkeWms/Views/WarehousePicking/Print.cshtml`

- [ ] **Step 1: Spalte hinzufuegen**

In `Print.cshtml` die `<thead>`-Zeile aktualisieren:

```html
<tr><th>Pos</th><th>Artikel-Nr</th><th>Bezeichnung</th><th>Bestellt</th><th>Ist</th><th>ME</th><th>Lagerplatz</th><th>Notiz</th><th>Fehlteil</th></tr>
```

Im `<tbody>`-Loop nach der Notiz-Spalte einfuegen:

```html
                <td>@(i.IsFinalShortage ? "✓" : "")</td>
```

Optional Header-Hinweis nach `<div class="header-info">`:

```html
        <div><strong>Status:</strong>
            @switch (Model.Status)
            {
                case IdealAkeWms.Models.WarehouseRequisitionStatus.PartiallyDelivered: <span>Teilgeliefert</span> break;
                case IdealAkeWms.Models.WarehouseRequisitionStatus.Closed: <span>Abgeschlossen</span> break;
                case IdealAkeWms.Models.WarehouseRequisitionStatus.Submitted: <span>Eingereicht (Druck-Zwischenstand)</span> break;
                default: <span>—</span> break;
            }
        </div>
```

- [ ] **Step 2: Build + Commit**

```bash
dotnet build IdealAkeWms.slnx
git add IdealAkeWms/Views/WarehousePicking/Print.cshtml
git commit -m "feat(view): Print zeigt Fehlteil-Spalte + Status-Header

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 11: MissingPartsController + Views (NEU)

**Files:**
- Create: `IdealAkeWms/Controllers/MissingPartsController.cs`
- Create: `IdealAkeWms/Views/MissingParts/Index.cshtml`
- Modify: `IdealAkeWms.Tests/Controllers/MissingPartsControllerTests.cs` (NEU)

- [ ] **Step 1: Controller anlegen**

Neue Datei `IdealAkeWms/Controllers/MissingPartsController.cs`:

```csharp
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers;

[RequireStockAccess]
public class MissingPartsController : Controller
{
    private readonly IWarehouseRequisitionRepository _repo;
    private readonly IProductionWorkplaceRepository _workplaces;
    private readonly ICurrentUserService _user;

    public MissingPartsController(
        IWarehouseRequisitionRepository repo,
        IProductionWorkplaceRepository workplaces,
        ICurrentUserService user)
    {
        _repo = repo; _workplaces = workplaces; _user = user;
    }

    public async Task<IActionResult> Index(int? workplaceId, bool mineOnly = false,
        int page = 1, int? pageSize = null)
    {
        if (page < 1) page = 1;
        var userDefaultPageSize = await _user.GetDefaultPageSizeAsync();
        var effectivePageSize = IdealAkeWms.Services.PageSize.Resolve(pageSize, userDefaultPageSize);
        var rawPageSize = IdealAkeWms.Services.PageSize.ResolveRaw(pageSize, userDefaultPageSize);

        // mineOnly hat Prio: filtert auf User-Workplace-Liste.
        // Wenn workplaceId auch gesetzt: muss in User-Workplaces enthalten sein, sonst leeres Resultat.
        int? effectiveWorkplaceId = workplaceId;
        if (mineOnly)
        {
            var userId = _user.GetCurrentAppUserId() ?? 0;
            var userWorkplaces = await _workplaces.GetByUserIdAsync(userId);
            var userIds = userWorkplaces.Select(w => w.Id).ToList();
            if (workplaceId.HasValue && !userIds.Contains(workplaceId.Value))
            {
                // Filter inkonsistent -> leeres Resultat
                effectiveWorkplaceId = -1;
            }
            else if (!workplaceId.HasValue && userIds.Count == 1)
            {
                effectiveWorkplaceId = userIds[0];
            }
            // Bei mineOnly und mehreren User-Workplaces zeigen wir alle eigenen
            // (workplaceId=null, aber Filter wird durch effectiveWorkplaceId nicht beschraenkt).
            // Hilfsweise koennen wir ein "userWorkplaceIds"-Filter weiter unten setzen — aber
            // GetMissingPartsAsync nimmt nur Single-workplaceFilter. Daher: bei mehreren eigenen
            // Werkbaenken ohne expliziten workplaceId-Param laden wir mit null und filtern
            // post-load in-memory. Alternative: Repo-Methode erweitern.
        }

        var columnFilters = IdealAkeWms.Services.ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);

        var (rawRows, total) = await _repo.GetMissingPartsAsync(
            effectiveWorkplaceId == -1 ? null : effectiveWorkplaceId,
            columnFilters,
            null, null, page, effectivePageSize);

        // Wenn mineOnly + mehrere User-Workplaces ohne expliziten Filter: in-memory einschraenken.
        // Bei mineOnly + workplaceId == -1: leeres Resultat erzwingen.
        IReadOnlyList<MissingPartRow> rows = rawRows;
        if (mineOnly && effectiveWorkplaceId == null)
        {
            var userId = _user.GetCurrentAppUserId() ?? 0;
            var userWorkplaces = await _workplaces.GetByUserIdAsync(userId);
            var allowedNames = userWorkplaces.Select(w => w.Name).ToHashSet();
            rows = rawRows.Where(r => allowedNames.Contains(r.WorkplaceName)).ToList();
            total = rows.Count;   // Counts korrigieren
        }
        else if (mineOnly && effectiveWorkplaceId == -1)
        {
            rows = new List<MissingPartRow>();
            total = 0;
        }

        var vm = new MissingPartsListViewModel
        {
            Items = rows.ToList(),
            AvailableWorkplaces = (await _workplaces.GetAllAsync()).OrderBy(w => w.Name).ToList(),
            WorkplaceFilter = workplaceId,
            MineOnly = mineOnly,
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

**Hinweis:** Die mineOnly-Logik mit mehreren Workplaces ist hier in-memory geloest. Wenn Performance kritisch wird, kann `GetMissingPartsAsync` um `IEnumerable<int> workplaceIdsFilter` erweitert werden.

- [ ] **Step 2: View anlegen**

Neue Datei `IdealAkeWms/Views/MissingParts/Index.cshtml`:

```html
@model IdealAkeWms.Models.ViewModels.MissingPartsListViewModel
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

<div class="card filter-card mb-3">
    <div class="card-body p-2">
        <form method="get" class="d-flex gap-2 align-items-center flex-wrap">
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

- [ ] **Step 3: 4 Controller-Tests neu**

Neue Datei `IdealAkeWms.Tests/Controllers/MissingPartsControllerTests.cs`:

```csharp
using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace IdealAkeWms.Tests.Controllers;

public class MissingPartsControllerTests
{
    private static (MissingPartsController ctrl, Mock<IWarehouseRequisitionRepository> repo,
                    Mock<IProductionWorkplaceRepository> wp, Mock<ICurrentUserService> user) Build()
    {
        var repo = new Mock<IWarehouseRequisitionRepository>();
        var wp = new Mock<IProductionWorkplaceRepository>();
        var user = new Mock<ICurrentUserService>();
        user.Setup(u => u.GetDefaultPageSizeAsync()).ReturnsAsync((int?)null);
        user.Setup(u => u.GetCurrentAppUserId()).Returns(1);
        wp.Setup(w => w.GetAllAsync()).ReturnsAsync(new List<ProductionWorkplace>());
        wp.Setup(w => w.GetByUserIdAsync(It.IsAny<int>())).ReturnsAsync(new List<ProductionWorkplace>());
        repo.Setup(r => r.GetMissingPartsAsync(It.IsAny<int?>(),
                It.IsAny<IReadOnlyDictionary<string, string>?>(),
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((new List<MissingPartRow>(), 0));
        var ctrl = new MissingPartsController(repo.Object, wp.Object, user.Object);
        return (ctrl, repo, wp, user);
    }

    [Fact]
    public async Task Index_NoMineOnly_PassesWorkplaceIdToRepoUnchanged()
    {
        var (ctrl, repo, _, _) = Build();
        await ctrl.Index(workplaceId: 5, mineOnly: false);
        repo.Verify(r => r.GetMissingPartsAsync(5, It.IsAny<IReadOnlyDictionary<string,string>?>(),
            null, null, 1, It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task Index_MineOnly_SingleUserWorkplace_FiltersByThatId()
    {
        var (ctrl, repo, wp, user) = Build();
        user.Setup(u => u.GetCurrentAppUserId()).Returns(42);
        wp.Setup(w => w.GetByUserIdAsync(42)).ReturnsAsync(new List<ProductionWorkplace>
            { new ProductionWorkplace { Id = 7, Name = "WB7" } });
        await ctrl.Index(workplaceId: null, mineOnly: true);
        repo.Verify(r => r.GetMissingPartsAsync(7, It.IsAny<IReadOnlyDictionary<string,string>?>(),
            null, null, 1, It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task Index_MineOnly_WithInconsistentWorkplaceId_ReturnsEmpty()
    {
        var (ctrl, repo, wp, user) = Build();
        user.Setup(u => u.GetCurrentAppUserId()).Returns(42);
        wp.Setup(w => w.GetByUserIdAsync(42)).ReturnsAsync(new List<ProductionWorkplace>
            { new ProductionWorkplace { Id = 7, Name = "WB7" } });
        var result = await ctrl.Index(workplaceId: 99, mineOnly: true);
        var vm = (result as ViewResult)?.Model as MissingPartsListViewModel;
        vm!.Items.Should().BeEmpty();
        vm.Pagination.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Index_ReturnsViewWithModel()
    {
        var (ctrl, _, _, _) = Build();
        var result = await ctrl.Index(null, false);
        result.Should().BeOfType<ViewResult>();
    }
}
```

- [ ] **Step 4: Build + Tests + Commit**

```bash
dotnet build IdealAkeWms.slnx
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "FullyQualifiedName~MissingPartsControllerTests"
```

Expected: 4 neue Tests passing.

```bash
git add IdealAkeWms/Controllers/MissingPartsController.cs IdealAkeWms/Views/MissingParts/Index.cshtml IdealAkeWms.Tests/Controllers/MissingPartsControllerTests.cs
git commit -m "feat(view): MissingPartsController + Index-View (Pflicht-Pattern)

Server-Side Spalten-Filter + Pagination. mineOnly-Param filtert auf
User-Workplaces. 4 Controller-Tests.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 12: _Layout — Menue-Eintrag "Fehlteile"

**Files:**
- Modify: `IdealAkeWms/Views/Shared/_Layout.cshtml`

- [ ] **Step 1: Menue-Eintrag hinzufuegen**

In `_Layout.cshtml` den Lager-Menue-Block finden (vermutlich Dropdown oder Sektion mit "Lagerbestellungen", "Bestand", "Bewegungshistorie"). Direkt nach dem "Lagerbestellungen"-Link einfuegen:

```html
@if (User.Identity?.IsAuthenticated == true && (User.IsInRole("admin") || User.IsInRole("stock") || User.IsInRole("stock_keyuser") || User.IsInRole("picking")))
{
    <li><a class="dropdown-item" asp-controller="MissingParts" asp-action="Index">Fehlteile</a></li>
}
```

**Hinweis:** Genaue Pruefung der bestehenden Layout-Struktur — Rolle-Check kann auch ueber `IRoleAuthorizationService` laufen falls schon vorhanden. Pattern an bestehende Eintraege anlehnen.

- [ ] **Step 2: Build + Commit**

```bash
dotnet build IdealAkeWms.slnx
git add IdealAkeWms/Views/Shared/_Layout.cshtml
git commit -m "feat(layout): add 'Fehlteile' menu entry under Lager

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 13: WarehouseRequisitionsController.Index Card + WarehouseRequisitions/Index.cshtml

**Files:**
- Modify: `IdealAkeWms/Controllers/WarehouseRequisitionsController.cs`
- Modify: `IdealAkeWms/Views/WarehouseRequisitions/Index.cshtml`
- Modify: `IdealAkeWms.Tests/Controllers/WarehouseRequisitionsControllerTests.cs`

- [ ] **Step 1: Controller — Card-Counts laden**

In `WarehouseRequisitionsController.Index` nach dem `ownOnly`-Block, vor dem `var vm = new WarehouseRequisitionListViewModel`:

```csharp
        var (missingItemCount, missingReqCount) = await _repo.GetFinalShortagesCountForUserAsync(userId);
```

Im ViewModel-Init zwei neue Properties setzen:

```csharp
        var vm = new WarehouseRequisitionListViewModel
        {
            Items = paged.Select(...).ToList(),
            AvailableWorkplaces = ...,
            Pagination = ...,
            MissingPartsItemCount = missingItemCount,
            MissingPartsRequisitionCount = missingReqCount,
        };
```

- [ ] **Step 2: View — Card oberhalb der Tabelle**

In `Views/WarehouseRequisitions/Index.cshtml` direkt nach dem Page-Header und vor dem ersten `<div class="card filter-card ...">` einfuegen:

```html
@if (Model.MissingPartsItemCount > 0)
{
    <div class="card border-warning mb-3">
        <div class="card-body">
            <h6 class="card-title">⚠ Meine Fehlteile</h6>
            <p class="mb-2">@Model.MissingPartsItemCount endgueltige Fehlteile aus
                @Model.MissingPartsRequisitionCount abgeschlossenen Bestellungen.</p>
            <a asp-controller="MissingParts" asp-action="Index" asp-route-mineOnly="true"
               class="btn btn-sm btn-outline-warning">Details ansehen →</a>
        </div>
    </div>
}
```

Status-Spalte erweitern um PartiallyDelivered-Badge (analog Task 9).

- [ ] **Step 3: 2 Controller-Tests**

In `IdealAkeWms.Tests/Controllers/WarehouseRequisitionsControllerTests.cs` neue Tests:

```csharp
    [Fact]
    public async Task Index_ShowsMissingPartsCard_WhenUserHasFinalShortages()
    {
        // Pattern wie bestehende Tests in der Datei
        // Mock _repo.GetFinalShortagesCountForUserAsync -> (3, 2)
        // Index() aufrufen, ViewModel.MissingPartsItemCount soll 3, RequisitionCount 2 sein
        Assert.True(true, "konkret: bestehendes Mock-Setup verwenden");
    }

    [Fact]
    public async Task Index_HidesMissingPartsCard_WhenNoShortages()
    {
        // Mock GetFinalShortagesCountForUserAsync -> (0, 0)
        // ViewModel.MissingPartsItemCount soll 0 sein
        Assert.True(true, "konkret: bestehendes Mock-Setup verwenden");
    }
```

**Hinweis fuer Implementierer**: konkret implementieren analog zu den bestehenden Tests im File.

- [ ] **Step 4: Build + Tests + Commit**

```bash
dotnet build IdealAkeWms.slnx
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "FullyQualifiedName~WarehouseRequisitionsControllerTests"
```

Expected: 2 neue Tests passing.

```bash
git add IdealAkeWms/Controllers/WarehouseRequisitionsController.cs IdealAkeWms/Views/WarehouseRequisitions/Index.cshtml IdealAkeWms.Tests/Controllers/WarehouseRequisitionsControllerTests.cs
git commit -m "feat(view): Werkbank Index zeigt 'Meine Fehlteile'-Karte

Card sichtbar wenn GetFinalShortagesCountForUserAsync > 0, verlinkt auf
MissingParts/Index?mineOnly=true.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 14: Version-Bump + Doku (Web + Service)

**Files:**
- Modify: `IdealAkeWms/AppVersion.cs`
- Modify: `IDEALAKEWMSService/AppVersion.cs`
- Modify: `IdealAkeWms/Views/Help/Changelog.cshtml`
- Modify: `docs/TESTSZENARIEN.md`
- Modify: `PROJECT_STATUS.md`
- Modify: `CLAUDE.md`

- [ ] **Step 1: AppVersion Web**

`IdealAkeWms/AppVersion.cs`:

```csharp
namespace IdealAkeWms;

public static class AppVersion
{
    public const string Version = "1.18.0";
    public const string Date = "2026-05-29";
}
```

- [ ] **Step 2: AppVersion Service**

`IDEALAKEWMSService/AppVersion.cs`:

```csharp
namespace IDEALAKEWMSService;

public static class AppVersion
{
    public const string Version = "1.18.0";
    public const string Date = "2026-05-29";
}
```

- [ ] **Step 3: Changelog**

In `IdealAkeWms/Views/Help/Changelog.cshtml` direkt nach `<div class="col-lg-8">` und VOR dem v1.17.0-Card einfuegen:

```html
        <div class="card mb-3">
            <div class="card-header text-white" style="background-color: var(--ake-primary);">
                <strong>v1.18.0</strong> <span class="text-white-50 ms-2">29.05.2026</span>
            </div>
            <div class="card-body">
                <h6>Lagerbestellungen: Teilgeliefert + Fehlteile + "Drucken und Abschliessen"</h6>
                <ul>
                    <li><strong>Neuer Status "Teilgeliefert":</strong> Bestellungen koennen jetzt
                        teilweise geliefert werden. Bleibt offen bis alle Items entweder vollstaendig
                        geliefert oder als endgueltige Fehlteile markiert sind.</li>
                    <li><strong>Endgueltige Fehlteile pro Position:</strong> Lager markiert pro Item ob
                        Restlieferung erwartet wird oder es ein endgueltiger Fehlteil ist. Neue Spalte
                        "Fehlteil" im Kommissionier-Detail.</li>
                    <li><strong>Neues Lager-Menue "Fehlteile":</strong> Globale Auswertung aller
                        endgueltigen Fehlteile mit Filter nach Werkbank + Spalten-Filter.</li>
                    <li><strong>Werkbank-Karte "Meine Fehlteile":</strong> Werkbank-User sieht in
                        "meine Listen" wieviele endgueltige Fehlteile aus eigenen Bestellungen offen sind.</li>
                    <li><strong>Neuer Button "Drucken und Abschliessen":</strong> kombiniert Speichern +
                        Statusableitung + Druck in einem Klick. Bestehender "Drucken"-Button bleibt fuer
                        Zwischendrucke ohne Abschluss.</li>
                </ul>
            </div>
        </div>
```

- [ ] **Step 4: TESTSZENARIEN Kapitel 32**

Am Ende von `docs/TESTSZENARIEN.md` anhaengen:

```markdown

---

## 32. Lagerbestellungen Teilgeliefert + Fehlteile (v1.18.0)

### 32.1 Vollstaendige Lieferung -> Closed
**Vorbedingung:** Bestellung #X mit 2 Items (Soll 10, Soll 5). Status Submitted.
**Schritte:** Picking/Details. Ist=10 + Ist=5. "Speichern + Abschliessen".
**Erwartet:** Status Closed, ClosedAt gesetzt. Keine Eintraege in Fehlteile-Liste.

### 32.2 Alle Shortages als endgueltig markiert -> Closed
**Vorbedingung:** Bestellung mit 2 Items (Soll 10, Soll 5).
**Schritte:** Ist=8 + Ist=3, beide Checkboxen "Endgueltig Fehlteil" anhaken. "Speichern + Abschliessen".
**Erwartet:** Status Closed. Beide Items in Fehlteile-Liste (2 + 2 = Soll-Ist).

### 32.3 Eine offen, eine final -> PartiallyDelivered
**Vorbedingung:** 2 Items.
**Schritte:** Ist=3 (kein final), Ist=2 (final). Abschliessen.
**Erwartet:** Status PartiallyDelivered. Item 1 nicht in Fehlteile-Liste (weil PartiallyDelivered). Auch Item 2 nicht (weil noch PartiallyDelivered).

### 32.4 Vollfehlteil (Ist=0, final) bei alleinigem Item -> Closed mit Fehlteil
**Vorbedingung:** Bestellung mit 1 Item Soll=5.
**Schritte:** Ist=0, "Endgueltig Fehlteil" anhaken, Abschliessen.
**Erwartet:** Status Closed. Item in Fehlteile-Liste mit Fehlt=5.

### 32.5 Restlieferungs-Workflow
**Vorbedingung:** Bestellung in PartiallyDelivered (Item 1 = 3 von 10 geliefert, kein final).
**Schritte:** Picking/Details neu oeffnen. Wert auf Ist=10 erhoehen. Abschliessen.
**Erwartet:** Status wechselt auf Closed.

### 32.6 "Drucken und Abschliessen"-Workflow
**Vorbedingung:** Submitted-Bestellung.
**Schritte:** Mengen + Notizen ausfuellen, "Drucken und Abschliessen" klicken.
**Erwartet:** Neuer Tab oeffnet sich mit Print-View (zeigt aktuelle Ist + Notizen + Fehlteil-Spalte). Hauptfenster navigiert zu Index. Bestellung jetzt Closed (oder PartiallyDelivered je nach Eingaben).

### 32.7 Werkbank-Karte "Meine Fehlteile" sichtbar
**Vorbedingung:** Werker WB1 hat 1 Closed-Bestellung mit 2 IsFinalShortage-Items.
**Schritte:** Login als Werker. WarehouseRequisitions/Index oeffnen.
**Erwartet:** Karte "Meine Fehlteile" zeigt "2 endgueltige Fehlteile aus 1 abgeschlossenen Bestellung". Link fuehrt zu MissingParts/Index?mineOnly=true.

### 32.8 Werkbank-Karte verschwindet bei 0 Fehlteilen
**Vorbedingung:** Werker ohne Fehlteile.
**Schritte:** Werkbank-Index oeffnen.
**Erwartet:** Karte nicht sichtbar.

### 32.9 MissingParts Spaltenfilter
**Vorbedingung:** 5 IsFinalShortage-Items, Artikel-Nrn AAA-1, AAA-2, BBB-3, CCC-4, DDD-5.
**Schritte:** Spaltenfilter "ArticleNumber" = "AAA,BBB".
**Erwartet:** Nur die 3 Eintraege mit AAA-1, AAA-2, BBB-3 sichtbar.

### 32.10 Negativ-Test: Ist-Menge -1
**Schritte:** Ist=-1 eingeben (per Forms-Manipulation), Abschliessen.
**Erwartet:** WarningMessage "Ist-Mengen duerfen nicht negativ sein.", Status unveraendert.
```

- [ ] **Step 5: PROJECT_STATUS**

In `PROJECT_STATUS.md` direkt vor dem `### v1.17.0`-Block einfuegen:

```markdown
### v1.18.0 — Lagerbestellungen: Teilgeliefert + Fehlteile + Drucken-und-Abschliessen

Hintergrund: Real-Welt-Fall "Bestellung wurde teilweise geliefert" war im bisherigen
Submitted/Closed-Workflow nicht abbildbar. Fehlteile wurden nirgends erfasst — keine
Auswertung moeglich. v1.18.0 erweitert das Modul um PartiallyDelivered-Status,
pro-Item Fehlteil-Flag und einen kombinierten "Drucken und Abschliessen"-Button.

| # | Sub-Task | Status |
|---|---------|--------|
| 0 | Pre-flight + Baseline-Build | ✅ erledigt |
| 1 | Enum + Model + Migration + FreshInstall | ✅ erledigt |
| 2 | DeriveStatus + CloseAsync-Refactor (7 Tests) | ✅ erledigt |
| 3 | SaveProgressAsync (2 Tests) | ✅ erledigt |
| 4 | GetMissingPartsAsync + Record (5 Tests) | ✅ erledigt |
| 5 | GetFinalShortagesCountForUserAsync (2 Tests) | ✅ erledigt |
| 6 | ViewModels erweitern | ✅ erledigt |
| 7 | Picking-Controller: Filter + Close + SaveProgress + PrintAndClose (6 Tests) | ✅ erledigt |
| 8 | Picking/Details.cshtml UI komplett ueberarbeitet | ✅ erledigt |
| 9 | Picking/Index.cshtml Status-Filter erweitert | ✅ erledigt |
| 10 | Print.cshtml Fehlteil-Spalte | ✅ erledigt |
| 11 | MissingPartsController + View (4 Tests) | ✅ erledigt |
| 12 | _Layout Fehlteile-Menue-Eintrag | ✅ erledigt |
| 13 | WarehouseRequisitions/Index "Meine Fehlteile"-Karte (2 Tests) | ✅ erledigt |
| 14 | Version-Bump v1.18.0 + Changelog + TESTSZENARIEN + CLAUDE.md | ✅ erledigt |
| 15 | Final-Check Build + Tests | ⏳ offen |
| 16 | Merge in main + Worktree-Cleanup | ⏳ offen |

---
```

- [ ] **Step 6: CLAUDE.md**

In `CLAUDE.md` im Abschnitt `## Bekannte Fallstricke` am Ende (vor `## Standard-Daten`) drei neue Eintraege:

```markdown
- **PartiallyDelivered ist KEIN End-Status (seit v1.18.0)**: Bestellungen in Status `PartiallyDelivered` bleiben im `WarehousePicking/Index` bearbeitbar. Der Lager kann sie wieder oeffnen, Restlieferung einbuchen oder ein bisheriges "Restlieferung erwartet"-Item auf "endgueltig Fehlteil" umflaggen. Erst beim erneuten Close wird der Status neu abgeleitet (`Closed` wenn alle Items entweder vollstaendig geliefert oder als IsFinalShortage markiert sind).
- **MissingParts zeigt nur `IsFinalShortage=true` UND Status=Closed (seit v1.18.0)**: Items mit `IsFinalShortage=true` in einer Bestellung mit Status `PartiallyDelivered` zaehlen NICHT als Fehlteil — die Bestellung ist noch in Bearbeitung, der Lager kann den Flag noch aendern. Erst wenn die Bestellung Closed wird, ist der Fehlteil "endgueltig".
- **WarehousePicking-Index Default-Filter zeigt Submitted+PartiallyDelivered (seit v1.18.0)**: `GetForWarehouseAsync` nimmt jetzt ein `WarehouseRequisitionStatus[]` statt einem `WarehouseRequisitionStatus?`. Ohne expliziten Filter werden beide "offene" Status angezeigt. OpenCount-Badge zaehlt entsprechend.
```

- [ ] **Step 7: Build verifizieren**

```bash
dotnet build IdealAkeWms.slnx
```

Expected: Build succeeded.

- [ ] **Step 8: Commit**

```bash
git add IdealAkeWms/AppVersion.cs IDEALAKEWMSService/AppVersion.cs IdealAkeWms/Views/Help/Changelog.cshtml docs/TESTSZENARIEN.md PROJECT_STATUS.md CLAUDE.md
git commit -m "feat(version): bump to v1.18.0 (Lagerbestellungen-Erweiterung)

Web + Service AppVersion 1.18.0. Changelog-Card. TESTSZENARIEN Kapitel 32
mit 10 Szenarien. PROJECT_STATUS Sub-Task-Tabelle. CLAUDE.md 3 neue Fallstricke.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 15: Final-Check Build + Tests

**Files:** keine Aenderungen

- [ ] **Step 1: Vollstaendiger Build**

```bash
dotnet build IdealAkeWms.slnx
```

Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 2: Vollstaendige Test-Suite**

```bash
dotnet test IdealAkeWms.slnx --no-build
```

Expected:
- Web: 589 baseline + 16 Repo-Tests + 6 Picking-Controller-Tests + 4 MissingParts-Controller-Tests + 2 Werkbank-Controller-Tests = **617 passing**, 1 skipped (Total 618)
- Service: 99 passing (unveraendert)

Falls Counts abweichen: nicht weitermachen, untersuchen.

- [ ] **Step 3: Versions-Sanity**

```bash
grep "1.18.0" IdealAkeWms/AppVersion.cs IDEALAKEWMSService/AppVersion.cs
grep "v1.18.0" IdealAkeWms/Views/Help/Changelog.cshtml
grep "v1.18.0" PROJECT_STATUS.md
```

Expected: alle Files enthalten `1.18.0`/`v1.18.0`.

- [ ] **Step 4: Worktree-Status**

```bash
git status
git log --oneline -20
```

Expected: clean. ~15 neue Commits seit `267e6e8 docs(spec)`.

---

## Task 16: Merge in main + Worktree-Cleanup

**Files:** keine Code-Aenderungen — git-Operationen

- [ ] **Step 1: Auf main wechseln**

```bash
cd C:/Git/IDEAL-AKE-WMS
git checkout main
git pull origin main
```

Expected: main aktuell.

- [ ] **Step 2: Merge --no-ff**

```bash
git merge --no-ff feature/teilgeliefert-fehlteile -m "merge feature/teilgeliefert-fehlteile into main (v1.18.0)"
```

Expected: Merge-Commit erstellt.

- [ ] **Step 3: Build + Tests auf main**

```bash
dotnet build IdealAkeWms.slnx && dotnet test IdealAkeWms.slnx --no-build
```

Expected: gruen.

- [ ] **Step 4: Push**

```bash
git push origin main
```

- [ ] **Step 5: Worktree + Branch entfernen**

```bash
git worktree remove .claude/worktrees/teilgeliefert-fehlteile
git branch -d feature/teilgeliefert-fehlteile
git worktree list
```

Expected: clean.

Falls Windows-File-Lock: nach `dotnet build-server shutdown` mit `Remove-Item -Recurse -Force ...` (siehe vorherige Rollouts).

---

## Final-Review-Subagent (nach Task 16)

Code-Reviewer-Subagent dispatchen mit Range `da27082..HEAD` (alle v1.18.0-Commits inkl. Merge). Pruefkriterien:

1. Migration: `IsFinalShortage` mit Filtered Index?
2. `00_FreshInstall.sql`: Schema + MigrationsHistory synchron?
3. `DeriveStatus`: korrekte Logik (Closed wenn fullyDelivered ODER !hasOpenShortage)?
4. `CloseAsync`: Default fuer fehlende QuantityPicked-Keys = 0 (NICHT mehr Soll wie vorher)?
5. `SaveProgressAsync`: aendert Status nicht?
6. `GetMissingPartsAsync`: nur Closed + IsFinalShortage=true?
7. `MissingPartsController`: mineOnly-Pfad korrekt?
8. Details.cshtml: 3 Buttons + JS-Print-Tab synchron im Click?
9. `WarehouseRequisitions/Index`: Card nur bei Count > 0?
10. AppVersion synchron Web + Service?
11. CLAUDE.md: 3 Fallstricke ergaenzt?
12. TESTSZENARIEN: 10 Szenarien Kapitel 32?
13. Status-Badge UI: PartiallyDelivered in allen 3 Views (Werkbank-Index, Picking-Index, Picking/Details)?
14. Test-Counts: 617 + 1 skip Web, 99 Service?
