# Sage Lagerbestand-Sync (Phase 2) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Sage-Lagerbestand pro (Artikel, Lagerplatz) periodisch ins WMS spiegeln. Bei Abweichung wird eine Korrektur-`StockMovement` mit neuem `MovementType=SageEinbuchung`/`SageAusbuchung` erzeugt — WMS-Bewegungshistorie bleibt vollstaendig erhalten, Sage ist Master fuer den absoluten Bestand.

**Architecture:** Worker-Service `LagerbestandSyncService` im `IDEALAKEWMSService` liest Sage via `ISageBestandReader` (raw SQL, mockbar in Tests) und erzeugt Korrektur-Movements via `ApplicationDbContext`. Strikte `Source=Sage`-Filterung + Active-Filter auf Ziel-Lagerplaetzen. Aggregations-Logik in 6+ existing Sites muss um die neuen Enum-Werte erweitert werden — Aggregations-Audit ist explizite, kritische Plan-Task.

**Tech Stack:** .NET 10, EF Core 10 (SQL Server), xUnit + FluentAssertions + Moq + InMemory-DB fuer Tests, Serilog, ASP.NET Core MVC. Bestehende Pattern aus Phase 1 wiederverwendet (SyncWorker mit ServiceSetting-Toggles, repository pattern, `system:sync` Audit, FakeReader fuer Tests).

**Branch:** `feature/sage-lagerbestand-sync` — beim Plan-Start abzweigen von der aktuellen Phase-1-Branch (`feature/sage-lagerplatz-sync`).

**Spec:** [docs/superpowers/specs/2026-05-06-sage-lagerbestand-sync-design.md](../specs/2026-05-06-sage-lagerbestand-sync-design.md)

**Commit-Konvention:** `feat(lagerbestand): ...` / `test(lagerbestand): ...` / `fix(lagerbestand): ...` / `docs: ...`. Co-Authored-By trailer wie in recent commits.

---

## Phase 1 — Schema, Enum, Aggregations-Audit

### Task 1: MovementType-Erweiterung + StockMovement.Note + Migration 57

**Files:**
- Modify: `IdealAkeWms/Models/MovementType.cs`
- Modify: `IdealAkeWms/Models/StockMovement.cs`
- Modify: `IdealAkeWms/Data/ApplicationDbContext.cs` (StockMovement-Konfiguration: Note-Property)
- Create: `IdealAkeWms/Migrations/<timestamp>_AddStockMovementNoteAndSageMovementTypes.cs`
- Create: `SQL/57_AddStockMovementNoteAndSageMovementTypes.sql`
- Modify: `SQL/00_FreshInstall.sql`

- [ ] **Step 1: Branch anlegen**

```pwsh
git checkout -b feature/sage-lagerbestand-sync
```

- [ ] **Step 2: MovementType-Enum erweitern**

Datei `IdealAkeWms/Models/MovementType.cs` ersetzen mit:

```csharp
namespace IdealAkeWms.Models;

public enum MovementType
{
    Einbuchung = 0,
    Ausbuchung = 1,
    Umbuchung = 2,
    SageEinbuchung = 3,    // Sage-Korrektur Plus: WMS war zu niedrig
    SageAusbuchung = 4     // Sage-Korrektur Minus: WMS war zu hoch
}
```

- [ ] **Step 3: StockMovement.Note hinzufuegen**

In `IdealAkeWms/Models/StockMovement.cs`, NACH `SourceStorageLocationId` (Zeile 38) und VOR den Navigation-Properties einfuegen:

```csharp
[StringLength(500)]
[Display(Name = "Notiz")]
public string? Note { get; set; }
```

- [ ] **Step 4: EF-Konfiguration in ApplicationDbContext**

In `IdealAkeWms/Data/ApplicationDbContext.cs`, im StockMovement-Block (nahe der bestehenden Zeile mit `entity.Property(e => e.WindowsUser).HasMaxLength(200).IsRequired();`) ergaenzen:

```csharp
entity.Property(e => e.Note).HasMaxLength(500);
```

- [ ] **Step 5: EF-Migration generieren**

```pwsh
dotnet ef migrations add AddStockMovementNoteAndSageMovementTypes --project IdealAkeWms
```

Expected: zwei neue Dateien `*_AddStockMovementNoteAndSageMovementTypes.cs` + `.Designer.cs`. Migration enthaelt `AddColumn` fuer `Note` (nvarchar(500), nullable). Keine MovementType-Schema-Aenderung (bleibt int).

- [ ] **Step 6: SQL/57-Skript erstellen**

```sql
-- SQL/57_AddStockMovementNoteAndSageMovementTypes.sql
-- Phase: Sage Lagerbestand-Sync — neues Note-Feld auf StockMovements.
-- MovementType-Erweiterung (3=SageEinbuchung, 4=SageAusbuchung) ist nur C#-Enum-Erweiterung,
-- die DB-Spalte (int) braucht keine Schema-Aenderung.

IF COL_LENGTH('dbo.StockMovements', 'Note') IS NULL
BEGIN
    ALTER TABLE dbo.StockMovements
        ADD Note NVARCHAR(500) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory
    WHERE MigrationId = '<TIMESTAMP>_AddStockMovementNoteAndSageMovementTypes')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    SELECT '<TIMESTAMP>_AddStockMovementNoteAndSageMovementTypes', '10.0.2';
END
GO
```

Den `<TIMESTAMP>`-Platzhalter durch den tatsaechlichen Timestamp aus dem in Step 5 generierten Migration-Dateinamen ersetzen.

- [ ] **Step 7: FreshInstall.sql aktualisieren**

In `SQL/00_FreshInstall.sql`:
- Im `[StockMovements]`-CREATE-TABLE die Spalte `[Note] NVARCHAR(500) NULL` ergaenzen (nach den ModifiedBy-Spalten oder am Tabellen-Ende, vor PK/FK-Constraints).
- MigrationId-Eintrag in der `__EFMigrationsHistory`-Insert-Liste am Ende der Datei ergaenzen.

- [ ] **Step 8: Build verifizieren**

```pwsh
dotnet build --nologo
```

Expected: `0 Fehler`. Tests laufen NICHT — die Aggregations-Sites sind noch nicht angepasst, Tests koennten muten falsch reagieren auf neue Enum-Werte.

- [ ] **Step 9: Bestaetige bestehende Tests laufen NOCH**

```pwsh
dotnet test --nologo
```

Expected: alle bisherigen 531 Tests gruen. Wichtig: Keine Aggregations-Stelle behandelt heute SageEinbuchung/SageAusbuchung, aber kein Test erzeugt sie auch — also keine Regression.

- [ ] **Step 10: Commit**

```pwsh
git add IdealAkeWms/Models/MovementType.cs IdealAkeWms/Models/StockMovement.cs IdealAkeWms/Data/ApplicationDbContext.cs IdealAkeWms/Migrations/ SQL/57_AddStockMovementNoteAndSageMovementTypes.sql SQL/00_FreshInstall.sql
git commit -m "feat(lagerbestand): add SageEinbuchung/SageAusbuchung enum + StockMovement.Note column" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 2: Aggregations-Audit + Updates + Tests

**Files:**
- Modify: `IdealAkeWms/Data/Repositories/StockMovementRepository.cs` (mehrere Aggregations-Stellen)
- Modify: `IdealAkeWms/Services/PickingTransferService.cs`
- Possibly modify: `IDEALAKEWMSService/Services/StockCheckService.cs` (siehe CLAUDE.md-Fallstricke)
- Create: `IdealAkeWms.Tests/Repositories/StockMovementRepositoryAggregationTests.cs`
- Create: `IdealAkeWms.Tests/Services/PickingTransferServiceAggregationTests.cs`

- [ ] **Step 1: Vollstaendigen Grep + Audit-Tabelle**

```pwsh
```

Such-Pattern: `MovementType\.` und `MovementType ==`. Erstelle eine Mapping-Tabelle aller Stellen, die das Vorzeichen oder die Behandlung pro Enum-Wert entscheiden.

Bekannte Sites laut Spec:
| Datei | Zeile | Pattern | Fix erforderlich |
|---|---|---|---|
| `StockMovementRepository.cs` | 61-62 | `Einbuchung ? + : Umbuchung ? + : -` (else=Ausbuchung) | Ja: SageEinbuchung als +, SageAusbuchung als − |
| `StockMovementRepository.cs` | 75 | `Where MovementType == Umbuchung && SourceStorageLocationId != null` | Nein: Sage-Korrekturen sind nie Umbuchungen |
| `StockMovementRepository.cs` | 182 | `Ausbuchung ? -Quantity : Quantity` (kollabierter Switch) | Ja: SageAusbuchung als −, SageEinbuchung als + |
| `StockMovementRepository.cs` | 274-275 | `MovementTypeName`-Mapping | Ja: SageEinbuchung -> "Sage-Einbuchung", SageAusbuchung -> "Sage-Ausbuchung" |
| `StockMovementRepository.cs` | 307-308 | `Einbuchung ? + : Umbuchung ? + : -` | Ja |
| `StockMovementRepository.cs` | 317 | `Where MovementType == Umbuchung` | Nein |
| `StockMovementRepository.cs` | 361-362 | `Einbuchung ? + : Umbuchung ? + : -` | Ja |
| `StockMovementRepository.cs` | 368 | `Where MovementType == Umbuchung` | Nein |
| `PickingTransferService.cs` | 198-199 | `Einbuchung ? + : Umbuchung ? + : -` | Ja |
| `PickingTransferService.cs` | 209 | `Where MovementType == Umbuchung` | Nein |

Plus: pruefe `IDEALAKEWMSService/Services/StockCheckService.cs` — falls dort Aggregations-Logik existiert, ergaenzen.

Falls der Grep weitere Stellen findet, die NICHT in dieser Tabelle sind: ergaenze sie und behandle sie analog.

- [ ] **Step 2: Tests schreiben (failing) — `StockMovementRepositoryAggregationTests`**

```csharp
// IdealAkeWms.Tests/Repositories/StockMovementRepositoryAggregationTests.cs
using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Tests.Repositories;

public class StockMovementRepositoryAggregationTests
{
    [Fact]
    public async Task GetCurrentStockAsync_AppliesSageEinbuchungAsPlus()
    {
        using var ctx = TestDbContextFactory.Create();
        SeedArticleAndLocation(ctx, articleId: 1, locationId: 1);
        ctx.StockMovements.AddRange(
            NewMovement(articleId: 1, locationId: 1, qty: 10m, MovementType.Einbuchung),
            NewMovement(articleId: 1, locationId: 1, qty: 3m, MovementType.SageEinbuchung)
        );
        await ctx.SaveChangesAsync();
        var repo = new StockMovementRepository(ctx);

        var result = await repo.GetCurrentStockAsync(includeZeroStock: false);

        result.Should().ContainSingle();
        result[0].CurrentQuantity.Should().Be(13m);
    }

    [Fact]
    public async Task GetCurrentStockAsync_AppliesSageAusbuchungAsMinus()
    {
        using var ctx = TestDbContextFactory.Create();
        SeedArticleAndLocation(ctx, articleId: 1, locationId: 1);
        ctx.StockMovements.AddRange(
            NewMovement(articleId: 1, locationId: 1, qty: 10m, MovementType.Einbuchung),
            NewMovement(articleId: 1, locationId: 1, qty: 4m, MovementType.SageAusbuchung)
        );
        await ctx.SaveChangesAsync();
        var repo = new StockMovementRepository(ctx);

        var result = await repo.GetCurrentStockAsync(includeZeroStock: false);

        result.Should().ContainSingle();
        result[0].CurrentQuantity.Should().Be(6m);
    }

    [Fact]
    public async Task GetStockByProductionOrderAsync_HandlesSageMovements()
    {
        // Test gegen die :182-Stelle (kollabierter Switch).
        using var ctx = TestDbContextFactory.Create();
        SeedArticleAndLocation(ctx, articleId: 1, locationId: 1);
        ctx.StockMovements.AddRange(
            NewMovementWithOrder(articleId: 1, locationId: 1, qty: 10m, MovementType.Einbuchung, orderNumber: "FA-100"),
            NewMovementWithOrder(articleId: 1, locationId: 1, qty: 3m, MovementType.SageEinbuchung, orderNumber: "FA-100"),
            NewMovementWithOrder(articleId: 1, locationId: 1, qty: 2m, MovementType.SageAusbuchung, orderNumber: "FA-100")
        );
        await ctx.SaveChangesAsync();
        var repo = new StockMovementRepository(ctx);

        var result = await repo.GetStockByProductionOrderAsync("FA-100");

        result.Should().ContainSingle();
        result[0].CurrentQuantity.Should().Be(11m);
    }

    [Fact]
    public async Task GetMovementsAsync_MovementTypeName_MapsSageValues()
    {
        using var ctx = TestDbContextFactory.Create();
        SeedArticleAndLocation(ctx, articleId: 1, locationId: 1);
        ctx.StockMovements.AddRange(
            NewMovement(articleId: 1, locationId: 1, qty: 5m, MovementType.SageEinbuchung),
            NewMovement(articleId: 1, locationId: 1, qty: 2m, MovementType.SageAusbuchung)
        );
        await ctx.SaveChangesAsync();
        var repo = new StockMovementRepository(ctx);

        var (items, _) = await repo.GetMovementsAsync(
            page: 1, pageSize: 10,
            filterArticle: null, filterStorageLocation: null, filterMovementType: null,
            filterFrom: null, filterTo: null, filterUser: null, filterProductionOrder: null);

        items.Should().HaveCount(2);
        items.Should().Contain(i => i.MovementTypeName == "Sage-Einbuchung");
        items.Should().Contain(i => i.MovementTypeName == "Sage-Ausbuchung");
    }

    [Fact]
    public async Task GetMovementsAsync_ReturnsNote()
    {
        using var ctx = TestDbContextFactory.Create();
        SeedArticleAndLocation(ctx, articleId: 1, locationId: 1);
        ctx.StockMovements.Add(new StockMovement
        {
            ArticleId = 1, StorageLocationId = 1,
            Quantity = 5m, MovementType = MovementType.SageEinbuchung,
            Note = "Sage-Korrektur: WMS=0, Sage=5, Diff=+5",
            Timestamp = DateTime.Now,
            WindowsUser = "system:sync",
            CreatedAt = DateTime.Now,
            CreatedBy = "system:sync", CreatedByWindows = "MACHINE"
        });
        await ctx.SaveChangesAsync();
        var repo = new StockMovementRepository(ctx);

        var (items, _) = await repo.GetMovementsAsync(
            page: 1, pageSize: 10,
            filterArticle: null, filterStorageLocation: null, filterMovementType: null,
            filterFrom: null, filterTo: null, filterUser: null, filterProductionOrder: null);

        items.Should().ContainSingle();
        items[0].Note.Should().Be("Sage-Korrektur: WMS=0, Sage=5, Diff=+5");
    }

    private static void SeedArticleAndLocation(IdealAkeWms.Data.ApplicationDbContext ctx, int articleId, int locationId)
    {
        ctx.Articles.Add(new Article
        {
            Id = articleId, ArticleNumber = $"A-{articleId:000}",
            Description = "Test", Unit = "Stk",
            CreatedBy = "tester", CreatedByWindows = "tester"
        });
        ctx.StorageLocations.Add(new StorageLocation
        {
            Id = locationId, Code = $"L-{locationId:000}", BarcodeValue = $"L-{locationId:000}",
            IsActive = true, IsPickingTransport = false,
            Source = StorageLocationSource.Sage,
            CreatedBy = "tester", CreatedByWindows = "tester"
        });
    }

    private static StockMovement NewMovement(int articleId, int locationId, decimal qty, MovementType type) => new()
    {
        ArticleId = articleId, StorageLocationId = locationId,
        Quantity = qty, MovementType = type,
        Timestamp = DateTime.Now,
        WindowsUser = "tester",
        CreatedAt = DateTime.Now,
        CreatedBy = "tester", CreatedByWindows = "tester"
    };

    private static StockMovement NewMovementWithOrder(int articleId, int locationId, decimal qty, MovementType type, string orderNumber)
    {
        var m = NewMovement(articleId, locationId, qty, type);
        m.ProductionOrder = orderNumber;
        return m;
    }
}
```

- [ ] **Step 3: Tests schreiben — `PickingTransferServiceAggregationTests`**

```csharp
// IdealAkeWms.Tests/Services/PickingTransferServiceAggregationTests.cs
using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace IdealAkeWms.Tests.Services;

public class PickingTransferServiceAggregationTests
{
    [Fact]
    public async Task GetAvailableStockAtSourceAsync_HandlesSageMovements()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.Articles.Add(new Article
        {
            Id = 1, ArticleNumber = "A-1", Description = "Test", Unit = "Stk",
            CreatedBy = "tester", CreatedByWindows = "tester"
        });
        ctx.StorageLocations.Add(new StorageLocation
        {
            Id = 1, Code = "L-1", BarcodeValue = "L-1",
            IsActive = true, IsPickingTransport = false,
            Source = StorageLocationSource.Sage,
            CreatedBy = "tester", CreatedByWindows = "tester"
        });
        ctx.StockMovements.AddRange(
            new StockMovement { ArticleId = 1, StorageLocationId = 1, Quantity = 10m, MovementType = MovementType.Einbuchung,    Timestamp = DateTime.Now, WindowsUser = "x", CreatedAt = DateTime.Now, CreatedBy = "x", CreatedByWindows = "x" },
            new StockMovement { ArticleId = 1, StorageLocationId = 1, Quantity = 3m,  MovementType = MovementType.SageEinbuchung, Timestamp = DateTime.Now, WindowsUser = "x", CreatedAt = DateTime.Now, CreatedBy = "x", CreatedByWindows = "x" },
            new StockMovement { ArticleId = 1, StorageLocationId = 1, Quantity = 2m,  MovementType = MovementType.SageAusbuchung, Timestamp = DateTime.Now, WindowsUser = "x", CreatedAt = DateTime.Now, CreatedBy = "x", CreatedByWindows = "x" }
        );
        await ctx.SaveChangesAsync();

        var service = new PickingTransferService(ctx, NullLogger<PickingTransferService>.Instance);
        var result = await service.GetAvailableStockAtSourceAsync(articleId: 1, sourceStorageLocationId: 1);

        result.Should().Be(11m);   // 10 + 3 - 2
    }
}
```

- [ ] **Step 4: Tests laufen — alle FAIL erwartet (Sage-Werte werden falsch behandelt)**

```pwsh
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "AggregationTests" --nologo
```

Expected: alle Tests FAIL. Insbesondere:
- `GetCurrentStockAsync_AppliesSageEinbuchungAsPlus`: Erwartet 13, bekommt 7 (SageEinbuchung wurde als − behandelt) ODER bekommt 10 (SageEinbuchung wurde ignoriert).
- `GetMovementsAsync_MovementTypeName_MapsSageValues`: bekommt "Ausbuchung" statt "Sage-Einbuchung" (else-branch).

- [ ] **Step 5: Aggregations-Logik in StockMovementRepository.cs:61-62 fix**

Im Block der `GetCurrentStockAsync`-Destination-Aggregation (Zeile 60-65):

Suche das Pattern:
```csharp
sm.MovementType == MovementType.Einbuchung ? sm.Quantity :
sm.MovementType == MovementType.Umbuchung ? sm.Quantity :
                                            -sm.Quantity
```

Ersetze mit:
```csharp
sm.MovementType == MovementType.Einbuchung || sm.MovementType == MovementType.SageEinbuchung
    ? sm.Quantity :
sm.MovementType == MovementType.Umbuchung
    ? sm.Quantity :
sm.MovementType == MovementType.Ausbuchung || sm.MovementType == MovementType.SageAusbuchung
    ? -sm.Quantity :
0m
```

- [ ] **Step 6: Aggregations-Logik in StockMovementRepository.cs:182 fix**

Im Block:
```csharp
var qty = sm.MovementType == MovementType.Ausbuchung ? -sm.Quantity : sm.Quantity;
```

Ersetze mit:
```csharp
var qty = sm.MovementType switch
{
    MovementType.Einbuchung => sm.Quantity,
    MovementType.SageEinbuchung => sm.Quantity,
    MovementType.Umbuchung => sm.Quantity,
    MovementType.Ausbuchung => -sm.Quantity,
    MovementType.SageAusbuchung => -sm.Quantity,
    _ => 0m
};
```

- [ ] **Step 7: MovementTypeName-Mapping in StockMovementRepository.cs:274-275 fix**

Suche das Pattern:
```csharp
MovementTypeName = sm.MovementType == MovementType.Einbuchung ? "Einbuchung" :
                   sm.MovementType == MovementType.Umbuchung ? "Umbuchung" : "Ausbuchung",
```

Ersetze mit:
```csharp
MovementTypeName = sm.MovementType == MovementType.Einbuchung ? "Einbuchung" :
                   sm.MovementType == MovementType.Ausbuchung ? "Ausbuchung" :
                   sm.MovementType == MovementType.Umbuchung ? "Umbuchung" :
                   sm.MovementType == MovementType.SageEinbuchung ? "Sage-Einbuchung" :
                   sm.MovementType == MovementType.SageAusbuchung ? "Sage-Ausbuchung" :
                   "Unbekannt",
```

Auch: das `Note`-Feld in der Projection ergaenzen, sodass es im ViewModel ankommt:
```csharp
Note = sm.Note,
```

- [ ] **Step 8: Aggregations-Logik in StockMovementRepository.cs:307-308 fix**

Gleiches Pattern wie Step 5 — den Switch erweitern. Identische 5-Branch-Form.

- [ ] **Step 9: Aggregations-Logik in StockMovementRepository.cs:361-362 fix**

Gleiches Pattern wie Step 5 — den Switch erweitern.

- [ ] **Step 10: Aggregations-Logik in PickingTransferService.cs:198-199 fix**

Gleiches Pattern wie Step 5 — den Switch erweitern.

- [ ] **Step 11: StockCheckService pruefen (Service-Projekt)**

```pwsh
```

Falls dort eine MovementType-basierte Aggregation existiert, mit dem gleichen 5-Branch-Switch updaten. Falls keine: explizit im Self-Review notieren.

- [ ] **Step 12: ViewModels-Pruefung**

`StockMovementListItem` (oder analoges ViewModel, das von `GetMovementsAsync` zurueckgegeben wird) muss eine `Note?`-Eigenschaft haben:

```pwsh
```

Falls fehlend: ergaenzen.

- [ ] **Step 13: Tests laufen — alle PASS**

```pwsh
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "AggregationTests" --nologo
dotnet test --nologo
```

Expected: alle bisherigen + die neuen Aggregations-Tests gruen. Erwartete Anzahl: 531 + 6 (new aggregation tests).

- [ ] **Step 14: Commit**

```pwsh
git add IdealAkeWms/Data/Repositories/StockMovementRepository.cs IdealAkeWms/Services/PickingTransferService.cs IdealAkeWms/Models/ViewModels/ IdealAkeWms.Tests/Repositories/StockMovementRepositoryAggregationTests.cs IdealAkeWms.Tests/Services/PickingTransferServiceAggregationTests.cs
git commit -m "feat(lagerbestand): extend aggregation logic for SageEinbuchung/SageAusbuchung" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

(Falls `StockCheckService` ebenfalls geaendert wurde: ergaenzen.)

---

### Task 3: GetCurrentStockByArticleAndLocationAsync (neue Repo-Methode)

**Files:**
- Modify: `IdealAkeWms/Data/Repositories/IStockMovementRepository.cs`
- Modify: `IdealAkeWms/Data/Repositories/StockMovementRepository.cs`
- Modify: `IdealAkeWms.Tests/Repositories/StockMovementRepositoryAggregationTests.cs` (Test ergaenzen)

- [ ] **Step 1: Test schreiben (failing)**

In `IdealAkeWms.Tests/Repositories/StockMovementRepositoryAggregationTests.cs` neuen Test ergaenzen:

```csharp
[Fact]
public async Task GetCurrentStockByArticleAndLocationAsync_AggregatesAllMovementTypes()
{
    using var ctx = TestDbContextFactory.Create();
    SeedArticleAndLocation(ctx, articleId: 1, locationId: 1);
    SeedArticleAndLocation(ctx, articleId: 2, locationId: 1);
    SeedArticleAndLocation(ctx, articleId: 1, locationId: 2);
    ctx.StockMovements.AddRange(
        NewMovement(articleId: 1, locationId: 1, qty: 10m, MovementType.Einbuchung),
        NewMovement(articleId: 1, locationId: 1, qty: 3m,  MovementType.SageEinbuchung),
        NewMovement(articleId: 1, locationId: 1, qty: 4m,  MovementType.Ausbuchung),
        NewMovement(articleId: 2, locationId: 1, qty: 5m,  MovementType.Einbuchung),
        NewMovement(articleId: 1, locationId: 2, qty: 7m,  MovementType.Einbuchung),
        NewMovement(articleId: 1, locationId: 2, qty: 2m,  MovementType.SageAusbuchung)
    );
    await ctx.SaveChangesAsync();
    var repo = new StockMovementRepository(ctx);

    var stock = await repo.GetCurrentStockByArticleAndLocationAsync();

    stock.Should().HaveCount(3);
    stock[(1, 1)].Should().Be(9m);   // 10 + 3 - 4
    stock[(2, 1)].Should().Be(5m);
    stock[(1, 2)].Should().Be(5m);   // 7 - 2
}
```

- [ ] **Step 2: Test laufen — FAIL (Methode existiert nicht)**

```pwsh
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "GetCurrentStockByArticleAndLocationAsync" --nologo
```

- [ ] **Step 3: Interface erweitern**

In `IdealAkeWms/Data/Repositories/IStockMovementRepository.cs`, Signatur ergaenzen (in der Naehe der existing `GetCurrentStockAsync`):

```csharp
Task<Dictionary<(int ArticleId, int StorageLocationId), decimal>> GetCurrentStockByArticleAndLocationAsync();
```

- [ ] **Step 4: Implementation**

In `IdealAkeWms/Data/Repositories/StockMovementRepository.cs`, am Ende der Klasse:

```csharp
public async Task<Dictionary<(int ArticleId, int StorageLocationId), decimal>> GetCurrentStockByArticleAndLocationAsync()
{
    var movements = await _context.StockMovements
        .AsNoTracking()
        .ToListAsync();

    var dict = new Dictionary<(int, int), decimal>();

    foreach (var sm in movements)
    {
        decimal effect = sm.MovementType switch
        {
            MovementType.Einbuchung => sm.Quantity,
            MovementType.SageEinbuchung => sm.Quantity,
            MovementType.Umbuchung => sm.Quantity,   // Ziel-Lagerplatz
            MovementType.Ausbuchung => -sm.Quantity,
            MovementType.SageAusbuchung => -sm.Quantity,
            _ => 0m
        };

        var key = (sm.ArticleId, sm.StorageLocationId);
        dict[key] = dict.GetValueOrDefault(key, 0m) + effect;

        // Umbuchung-Quell-Seite: -Quantity am SourceStorageLocationId
        if (sm.MovementType == MovementType.Umbuchung && sm.SourceStorageLocationId.HasValue)
        {
            var srcKey = (sm.ArticleId, sm.SourceStorageLocationId.Value);
            dict[srcKey] = dict.GetValueOrDefault(srcKey, 0m) - sm.Quantity;
        }
    }

    return dict;
}
```

- [ ] **Step 5: Test laufen — PASS**

```pwsh
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "GetCurrentStockByArticleAndLocationAsync" --nologo
```

- [ ] **Step 6: Build + Tests**

```pwsh
dotnet build --nologo
dotnet test --nologo --no-build
```

Expected: alles gruen.

- [ ] **Step 7: Commit**

```pwsh
git add IdealAkeWms/Data/Repositories/IStockMovementRepository.cs IdealAkeWms/Data/Repositories/StockMovementRepository.cs IdealAkeWms.Tests/Repositories/StockMovementRepositoryAggregationTests.cs
git commit -m "feat(lagerbestand): add GetCurrentStockByArticleAndLocationAsync for sync-service" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Phase 2 — Sage-Reader

### Task 4: SageBestandReader (Interface + DTO + Real Impl + DI)

**Files:**
- Create: `IDEALAKEWMSService/Services/ISageBestandReader.cs`
- Create: `IDEALAKEWMSService/Services/SageBestandReader.cs`
- Modify: `IDEALAKEWMSService/Program.cs`

Hinweis: Wie Phase-1-`SageLagerplatzReader` — keine Tests gegen die echte Sage-DB. Mock-Grenze fuer Sync-Service-Tests.

- [ ] **Step 1: DTO + Interface**

```csharp
// IDEALAKEWMSService/Services/ISageBestandReader.cs
namespace IDEALAKEWMSService.Services;

/// <summary>DTO from SAGE — null-able weil LEFT JOIN + Aggregation ggf. NULL liefern kann.</summary>
public record SageBestandDto(string? Artikelnummer, string? Lagerplatz, decimal? Bestand);

public interface ISageBestandReader
{
    Task<List<SageBestandDto>> GetAllAsync(CancellationToken ct = default);
}
```

- [ ] **Step 2: Real Implementation**

```csharp
// IDEALAKEWMSService/Services/SageBestandReader.cs
using Microsoft.Data.SqlClient;

namespace IDEALAKEWMSService.Services;

public class SageBestandReader : ISageBestandReader
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SageBestandReader> _logger;

    public SageBestandReader(IConfiguration configuration, ILogger<SageBestandReader> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<List<SageBestandDto>> GetAllAsync(CancellationToken ct = default)
    {
        var sageConnection = _configuration.GetConnectionString("SageConnection")
            ?? throw new InvalidOperationException("SageConnection nicht konfiguriert.");

        // ANNAHME: KHKLagerplatzbestaende.Bestand ist decimal/numeric.
        // Convert.ToDecimal toleriert auch money/numeric/float.
        // KHKArtikel.Mandant = 1 — analog Artikel-Sync (User-Query-Annahme).
        const string sql = """
            SELECT
                A.Artikelnummer,
                LP.Kurzbezeichnung AS Lagerplatz,
                SUM(LB.Bestand) AS Bestand
            FROM [dbo].[KHKArtikel] AS A
            LEFT JOIN [dbo].[KHKArtikelVarianten] AS AV ON A.Artikelnummer = AV.Artikelnummer
            LEFT JOIN [dbo].[KHKLagerplatzbestaende] AS LB ON A.Artikelnummer = LB.Artikelnummer
            LEFT JOIN [dbo].[KHKLagerplaetze] AS LP ON LB.PlatzID = LP.PlatzID
            WHERE A.Mandant = 1
              AND LB.Bestand IS NOT NULL
            GROUP BY A.Artikelnummer, LP.Kurzbezeichnung, LB.Lagerkennung
            """;

        var result = new List<SageBestandDto>();

        await using var conn = new SqlConnection(sageConnection);
        await conn.OpenAsync(ct);
        await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 120 };
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            result.Add(new SageBestandDto(
                Artikelnummer: reader.IsDBNull(0) ? null : reader.GetString(0),
                Lagerplatz:    reader.IsDBNull(1) ? null : reader.GetString(1),
                Bestand:       reader.IsDBNull(2) ? (decimal?)null : Convert.ToDecimal(reader.GetValue(2))
            ));
        }

        _logger.LogInformation("Sage liefert {Count} Bestand-Tupel.", result.Count);
        return result;
    }
}
```

- [ ] **Step 3: DI Registration**

In `IDEALAKEWMSService/Program.cs`, im Services-Block (nach `ISageLagerplatzReader`):

```csharp
builder.Services.AddScoped<ISageBestandReader, SageBestandReader>();
```

- [ ] **Step 4: Build verifizieren**

```pwsh
dotnet build IDEALAKEWMSService/IDEALAKEWMSService.csproj --nologo
```

- [ ] **Step 5: Commit**

```pwsh
git add IDEALAKEWMSService/Services/ISageBestandReader.cs IDEALAKEWMSService/Services/SageBestandReader.cs IDEALAKEWMSService/Program.cs
git commit -m "feat(lagerbestand): add SageBestandReader with raw SQL against KHKLagerplatzbestaende" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Phase 3 — Sync-Service (TDD)

### Task 5: LagerbestandSyncService Skeleton + Test 1 (Insert-Plus)

**Files:**
- Create: `IDEALAKEWMSService/Services/ILagerbestandSyncService.cs`
- Create: `IDEALAKEWMSService/Services/LagerbestandSyncService.cs`
- Create: `IDEALAKEWMSService.Tests/Helpers/FakeSageBestandReader.cs`
- Create: `IDEALAKEWMSService.Tests/Services/LagerbestandSyncServiceTests.cs`

- [ ] **Step 1: FakeSageBestandReader**

```csharp
// IDEALAKEWMSService.Tests/Helpers/FakeSageBestandReader.cs
using IDEALAKEWMSService.Services;

namespace IDEALAKEWMSService.Tests.Helpers;

public class FakeSageBestandReader : ISageBestandReader
{
    public List<SageBestandDto> Records { get; set; } = new();
    public Exception? ThrowOnRead { get; set; }

    public Task<List<SageBestandDto>> GetAllAsync(CancellationToken ct = default)
    {
        if (ThrowOnRead != null)
            throw ThrowOnRead;
        return Task.FromResult(Records);
    }
}
```

- [ ] **Step 2: Result-Record + Interface**

```csharp
// IDEALAKEWMSService/Services/ILagerbestandSyncService.cs
namespace IDEALAKEWMSService.Services;

public record LagerbestandSyncResult(
    int Tuples, int CorrectionsPlus, int CorrectionsMinus,
    int NoChange, int Skipped, int Errors,
    bool DryRun);

public interface ILagerbestandSyncService
{
    Task<LagerbestandSyncResult> RunAsync(bool dryRun, CancellationToken ct = default);
}
```

- [ ] **Step 3: Erster Test (Insert-Plus)**

```csharp
// IDEALAKEWMSService.Tests/Services/LagerbestandSyncServiceTests.cs
using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using IDEALAKEWMSService.Services;
using IDEALAKEWMSService.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace IDEALAKEWMSService.Tests.Services;

public class LagerbestandSyncServiceTests
{
    private const string SyncUser = "system:sync";

    private static (LagerbestandSyncService service, FakeSageBestandReader reader,
                    IdealAkeWms.Data.ApplicationDbContext ctx, SyncLogRepository syncLogs)
        Build()
    {
        var ctx = TestDbContextFactory.Create();
        var reader = new FakeSageBestandReader();
        var syncLogs = new SyncLogRepository(ctx);
        var stockRepo = new StockMovementRepository(ctx);
        var service = new LagerbestandSyncService(ctx, reader, stockRepo, syncLogs, NullLogger<LagerbestandSyncService>.Instance);
        return (service, reader, ctx, syncLogs);
    }

    private static void SeedArticle(IdealAkeWms.Data.ApplicationDbContext ctx, int id, string number)
    {
        ctx.Articles.Add(new Article
        {
            Id = id, ArticleNumber = number,
            Description = "Test", Unit = "Stk",
            CreatedBy = "tester", CreatedByWindows = "tester"
        });
    }

    private static void SeedSageLocation(IdealAkeWms.Data.ApplicationDbContext ctx, int id, string code, bool isActive = true)
    {
        ctx.StorageLocations.Add(new StorageLocation
        {
            Id = id, Code = code, BarcodeValue = code,
            Source = StorageLocationSource.Sage, IsActive = isActive,
            IsPickingTransport = false,
            CreatedBy = "tester", CreatedByWindows = "tester"
        });
    }

    [Fact]
    public async Task Run_EmptyWms_SagePositive_InsertsSageEinbuchung()
    {
        var (svc, reader, ctx, syncLogs) = Build();
        SeedArticle(ctx, id: 1, number: "A-1");
        SeedSageLocation(ctx, id: 1, code: "L-1");
        await ctx.SaveChangesAsync();
        reader.Records = new() { new("A-1", "L-1", 5m) };

        var result = await svc.RunAsync(dryRun: false);

        result.CorrectionsPlus.Should().Be(1);
        result.CorrectionsMinus.Should().Be(0);
        result.NoChange.Should().Be(0);
        result.Skipped.Should().Be(0);
        result.Tuples.Should().Be(1);

        var movements = ctx.StockMovements.ToList();
        movements.Should().ContainSingle();
        movements[0].MovementType.Should().Be(MovementType.SageEinbuchung);
        movements[0].Quantity.Should().Be(5m);
        movements[0].WindowsUser.Should().Be(SyncUser);
        movements[0].Note.Should().Contain("Diff=+5");

        var summary = (await syncLogs.GetRecentAsync("Lagerbestand", null, 10)).FirstOrDefault();
        summary.Should().NotBeNull();
        summary!.Level.Should().Be(SyncLogLevel.Info);
    }
}
```

- [ ] **Step 4: Test laufen — Compile-Error erwartet**

```pwsh
dotnet test IDEALAKEWMSService.Tests/IDEALAKEWMSService.Tests.csproj --filter "FullyQualifiedName~LagerbestandSyncService" --nologo
```

Expected: Compile-Error (`LagerbestandSyncService` nicht definiert).

- [ ] **Step 5: Minimal-Implementation**

```csharp
// IDEALAKEWMSService/Services/LagerbestandSyncService.cs
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IDEALAKEWMSService.Services;

public class LagerbestandSyncService : ILagerbestandSyncService
{
    private const string ServiceName = "Lagerbestand";
    private const string SyncUser = "system:sync";

    private readonly ApplicationDbContext _ctx;
    private readonly ISageBestandReader _reader;
    private readonly IStockMovementRepository _stockRepo;
    private readonly ISyncLogRepository _syncLogs;
    private readonly ILogger<LagerbestandSyncService> _logger;

    public LagerbestandSyncService(
        ApplicationDbContext ctx,
        ISageBestandReader reader,
        IStockMovementRepository stockRepo,
        ISyncLogRepository syncLogs,
        ILogger<LagerbestandSyncService> logger)
    {
        _ctx = ctx;
        _reader = reader;
        _stockRepo = stockRepo;
        _syncLogs = syncLogs;
        _logger = logger;
    }

    public async Task<LagerbestandSyncResult> RunAsync(bool dryRun, CancellationToken ct = default)
    {
        int tuples = 0, plus = 0, minus = 0, noChange = 0, skipped = 0, errors = 0;

        List<SageBestandDto> sageRows;
        try
        {
            sageRows = await _reader.GetAllAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Sage-Connection fehlgeschlagen.");
            await _syncLogs.AddAsync(new SyncLog
            {
                Service = ServiceName,
                Level = SyncLogLevel.Error,
                Message = $"Sage-Connection fehlgeschlagen: {ex.Message}"
            });
            return new LagerbestandSyncResult(0, 0, 0, 0, 0, 1, dryRun);
        }

        // Pre-loading
        var articleByNumber = await _ctx.Articles
            .ToDictionaryAsync(a => a.ArticleNumber, a => a.Id, StringComparer.OrdinalIgnoreCase, ct);
        var locationByCode = await _ctx.StorageLocations
            .ToDictionaryAsync(
                l => l.Code,
                l => (l.Id, l.Source, l.IsActive),
                StringComparer.OrdinalIgnoreCase, ct);
        var wmsStock = await _stockRepo.GetCurrentStockByArticleAndLocationAsync();

        foreach (var dto in sageRows)
        {
            tuples++;

            if (string.IsNullOrWhiteSpace(dto.Artikelnummer) || string.IsNullOrWhiteSpace(dto.Lagerplatz))
            {
                skipped++;
                continue;
            }

            if (!articleByNumber.TryGetValue(dto.Artikelnummer, out var articleId))
            {
                await _syncLogs.AddAsync(new SyncLog
                {
                    Service = ServiceName, Level = SyncLogLevel.Warning,
                    Message = $"Artikel {dto.Artikelnummer} nicht im WMS, uebersprungen.",
                    Reference = dto.Artikelnummer
                });
                skipped++;
                continue;
            }

            // Insert in DB nur wenn nicht DryRun
            var wmsBestand = wmsStock.GetValueOrDefault((articleId, /* placeholder */ 0), 0m);
            // (Vollstaendig in Task 6+)
            var sageBestand = dto.Bestand ?? 0m;
            var delta = sageBestand - wmsBestand;

            if (delta == 0m) { noChange++; continue; }

            // TODO: Lagerplatz-Lookup, Korrektur-Insert (Task 6, 7, 8)
            // Vorlaeufig fuer Task 5 nur Insert-Plus-Pfad mit fixem Lagerplatz-Lookup
            if (!locationByCode.TryGetValue(dto.Lagerplatz, out var loc) || loc.Source != "Sage" || !loc.IsActive)
            {
                skipped++;
                continue;
            }

            wmsBestand = wmsStock.GetValueOrDefault((articleId, loc.Id), 0m);
            delta = sageBestand - wmsBestand;
            if (delta == 0m) { noChange++; continue; }

            if (!dryRun)
            {
                _ctx.StockMovements.Add(new StockMovement
                {
                    ArticleId = articleId,
                    StorageLocationId = loc.Id,
                    Quantity = Math.Abs(delta),
                    MovementType = delta > 0 ? MovementType.SageEinbuchung : MovementType.SageAusbuchung,
                    Note = $"Sage-Korrektur: WMS={wmsBestand}, Sage={sageBestand}, Diff={(delta > 0 ? "+" : "")}{delta}",
                    Timestamp = DateTime.Now,
                    UserId = null,
                    WindowsUser = SyncUser,
                    CreatedAt = DateTime.Now,
                    CreatedBy = SyncUser,
                    CreatedByWindows = Environment.MachineName
                });
            }

            if (delta > 0) plus++; else minus++;
        }

        if (!dryRun) await _ctx.SaveChangesAsync(ct);

        var prefix = dryRun ? "[DryRun] " : "";
        await _syncLogs.AddAsync(new SyncLog
        {
            Service = ServiceName,
            Level = SyncLogLevel.Info,
            Message = $"{prefix}Sync OK: {tuples} Tupel, {plus} Plus, {minus} Minus, {noChange} ohne Aenderung, {skipped} uebersprungen, {errors} Fehler."
        });

        return new LagerbestandSyncResult(tuples, plus, minus, noChange, skipped, errors, dryRun);
    }
}
```

Hinweis: Diese Implementierung ist Skeleton — der Lagerplatz-Lookup ist hier doppelt (einmal als Platzhalter, einmal real). Tasks 6-10 werden sukzessive Tests hinzufuegen, die die Logik klaeren und vereinfachen. Konsolidierung in Task 7.

- [ ] **Step 6: Test laufen — sollte PASS**

```pwsh
dotnet test IDEALAKEWMSService.Tests/IDEALAKEWMSService.Tests.csproj --filter "Run_EmptyWms_SagePositive" --nologo
```

Expected: 1/1 PASS.

- [ ] **Step 7: Commit**

```pwsh
git add IDEALAKEWMSService/Services/ILagerbestandSyncService.cs IDEALAKEWMSService/Services/LagerbestandSyncService.cs IDEALAKEWMSService.Tests/Helpers/FakeSageBestandReader.cs IDEALAKEWMSService.Tests/Services/LagerbestandSyncServiceTests.cs
git commit -m "feat(lagerbestand): LagerbestandSyncService skeleton with insert-plus path + test" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 6: Insert-Minus + No-Change + Decimal-Diff + Bestand-NULL Tests

**Files:**
- Modify: `IDEALAKEWMSService.Tests/Services/LagerbestandSyncServiceTests.cs`
- Modify: `IDEALAKEWMSService/Services/LagerbestandSyncService.cs` (Konsolidierung des duplizierten Lookup-Codes)

- [ ] **Step 1: Tests anhaengen**

```csharp
[Fact]
public async Task Run_WmsHigherThanSage_InsertsSageAusbuchung()
{
    var (svc, reader, ctx, _) = Build();
    SeedArticle(ctx, id: 1, number: "A-1");
    SeedSageLocation(ctx, id: 1, code: "L-1");
    ctx.StockMovements.Add(new StockMovement
    {
        ArticleId = 1, StorageLocationId = 1,
        Quantity = 10m, MovementType = MovementType.Einbuchung,
        Timestamp = DateTime.Now.AddDays(-1),
        WindowsUser = "tester",
        CreatedAt = DateTime.Now.AddDays(-1),
        CreatedBy = "tester", CreatedByWindows = "tester"
    });
    await ctx.SaveChangesAsync();
    reader.Records = new() { new("A-1", "L-1", 7m) };

    var result = await svc.RunAsync(dryRun: false);

    result.CorrectionsMinus.Should().Be(1);
    result.CorrectionsPlus.Should().Be(0);
    var corrections = ctx.StockMovements
        .Where(m => m.MovementType == MovementType.SageAusbuchung).ToList();
    corrections.Should().ContainSingle();
    corrections[0].Quantity.Should().Be(3m);
    corrections[0].Note.Should().Contain("Diff=-3");
}

[Fact]
public async Task Run_WmsEqualsSage_NoCorrection()
{
    var (svc, reader, ctx, _) = Build();
    SeedArticle(ctx, id: 1, number: "A-1");
    SeedSageLocation(ctx, id: 1, code: "L-1");
    ctx.StockMovements.Add(new StockMovement
    {
        ArticleId = 1, StorageLocationId = 1, Quantity = 5m,
        MovementType = MovementType.Einbuchung,
        Timestamp = DateTime.Now,
        WindowsUser = "tester", CreatedAt = DateTime.Now,
        CreatedBy = "tester", CreatedByWindows = "tester"
    });
    await ctx.SaveChangesAsync();
    reader.Records = new() { new("A-1", "L-1", 5m) };

    var result = await svc.RunAsync(dryRun: false);

    result.NoChange.Should().Be(1);
    result.CorrectionsPlus.Should().Be(0);
    result.CorrectionsMinus.Should().Be(0);
    ctx.StockMovements.Where(m => m.MovementType >= MovementType.SageEinbuchung)
        .Should().BeEmpty();
}

[Fact]
public async Task Run_DecimalDiff_PreservesFraction()
{
    var (svc, reader, ctx, _) = Build();
    SeedArticle(ctx, id: 1, number: "A-1");
    SeedSageLocation(ctx, id: 1, code: "L-1");
    ctx.StockMovements.Add(new StockMovement
    {
        ArticleId = 1, StorageLocationId = 1, Quantity = 5.7m,
        MovementType = MovementType.Einbuchung,
        Timestamp = DateTime.Now, WindowsUser = "tester",
        CreatedAt = DateTime.Now,
        CreatedBy = "tester", CreatedByWindows = "tester"
    });
    await ctx.SaveChangesAsync();
    reader.Records = new() { new("A-1", "L-1", 6.0m) };

    var result = await svc.RunAsync(dryRun: false);

    result.CorrectionsPlus.Should().Be(1);
    var c = ctx.StockMovements.Single(m => m.MovementType == MovementType.SageEinbuchung);
    c.Quantity.Should().Be(0.3m);
}

[Fact]
public async Task Run_SageBestandNull_TreatsAsZero()
{
    var (svc, reader, ctx, _) = Build();
    SeedArticle(ctx, id: 1, number: "A-1");
    SeedSageLocation(ctx, id: 1, code: "L-1");
    ctx.StockMovements.Add(new StockMovement
    {
        ArticleId = 1, StorageLocationId = 1, Quantity = 4m,
        MovementType = MovementType.Einbuchung,
        Timestamp = DateTime.Now, WindowsUser = "tester",
        CreatedAt = DateTime.Now,
        CreatedBy = "tester", CreatedByWindows = "tester"
    });
    await ctx.SaveChangesAsync();
    reader.Records = new() { new("A-1", "L-1", null) };

    var result = await svc.RunAsync(dryRun: false);

    result.CorrectionsMinus.Should().Be(1);
    var c = ctx.StockMovements.Single(m => m.MovementType == MovementType.SageAusbuchung);
    c.Quantity.Should().Be(4m);
}
```

- [ ] **Step 2: Service-Code aufraeumen (duplizierten Lookup eliminieren)**

In `LagerbestandSyncService.cs` den `foreach`-Block umstrukturieren — der Skeleton aus Task 5 hatte einen doppelten Lookup. Saubere Form:

```csharp
foreach (var dto in sageRows)
{
    tuples++;

    if (string.IsNullOrWhiteSpace(dto.Artikelnummer) || string.IsNullOrWhiteSpace(dto.Lagerplatz))
    {
        skipped++;
        continue;
    }

    if (!articleByNumber.TryGetValue(dto.Artikelnummer, out var articleId))
    {
        await _syncLogs.AddAsync(new SyncLog
        {
            Service = ServiceName, Level = SyncLogLevel.Warning,
            Message = $"Artikel {dto.Artikelnummer} nicht im WMS, uebersprungen.",
            Reference = dto.Artikelnummer
        });
        skipped++;
        continue;
    }

    if (!locationByCode.TryGetValue(dto.Lagerplatz, out var loc))
    {
        await _syncLogs.AddAsync(new SyncLog
        {
            Service = ServiceName, Level = SyncLogLevel.Warning,
            Message = $"Lagerplatz {dto.Lagerplatz} nicht im WMS, uebersprungen.",
            Reference = dto.Lagerplatz
        });
        skipped++;
        continue;
    }

    if (loc.Source != StorageLocationSource.Sage)
    {
        await _syncLogs.AddAsync(new SyncLog
        {
            Service = ServiceName, Level = SyncLogLevel.Warning,
            Message = $"Lagerplatz {dto.Lagerplatz} ist Manual-Quelle, uebersprungen.",
            Reference = dto.Lagerplatz
        });
        skipped++;
        continue;
    }

    if (!loc.IsActive)
    {
        await _syncLogs.AddAsync(new SyncLog
        {
            Service = ServiceName, Level = SyncLogLevel.Warning,
            Message = $"Lagerplatz {dto.Lagerplatz} ist deaktiviert, uebersprungen.",
            Reference = dto.Lagerplatz
        });
        skipped++;
        continue;
    }

    var wmsBestand = wmsStock.GetValueOrDefault((articleId, loc.Id), 0m);
    var sageBestand = dto.Bestand ?? 0m;
    var delta = sageBestand - wmsBestand;

    if (delta == 0m) { noChange++; continue; }

    if (!dryRun)
    {
        _ctx.StockMovements.Add(new StockMovement
        {
            ArticleId = articleId,
            StorageLocationId = loc.Id,
            Quantity = Math.Abs(delta),
            MovementType = delta > 0 ? MovementType.SageEinbuchung : MovementType.SageAusbuchung,
            Note = $"Sage-Korrektur: WMS={wmsBestand}, Sage={sageBestand}, Diff={(delta > 0 ? "+" : "")}{delta}",
            Timestamp = DateTime.Now,
            UserId = null,
            WindowsUser = SyncUser,
            CreatedAt = DateTime.Now,
            CreatedBy = SyncUser,
            CreatedByWindows = Environment.MachineName
        });
    }

    if (delta > 0) plus++; else minus++;
}
```

- [ ] **Step 3: Tests laufen — alle PASS**

```pwsh
dotnet test IDEALAKEWMSService.Tests/IDEALAKEWMSService.Tests.csproj --filter "FullyQualifiedName~LagerbestandSyncService" --nologo
```

Expected: 5/5 PASS (Test 1 von Task 5 + 4 neue).

- [ ] **Step 4: Commit**

```pwsh
git add IDEALAKEWMSService/Services/LagerbestandSyncService.cs IDEALAKEWMSService.Tests/Services/LagerbestandSyncServiceTests.cs
git commit -m "feat(lagerbestand): minus-correction + no-change + decimal-diff + null-bestand handling" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 7: Skip-Paths Tests (Unknown Article / Unknown Location / Manual / Inactive)

**Files:**
- Modify: `IDEALAKEWMSService.Tests/Services/LagerbestandSyncServiceTests.cs`

- [ ] **Step 1: Tests anhaengen**

```csharp
[Fact]
public async Task Run_UnknownArticle_SkipsAndLogsWarning()
{
    var (svc, reader, ctx, syncLogs) = Build();
    SeedSageLocation(ctx, id: 1, code: "L-1");
    await ctx.SaveChangesAsync();
    reader.Records = new() { new("UNKNOWN-ARTICLE", "L-1", 5m) };

    var result = await svc.RunAsync(dryRun: false);

    result.Skipped.Should().Be(1);
    result.CorrectionsPlus.Should().Be(0);
    ctx.StockMovements.Should().BeEmpty();

    var warnings = await syncLogs.GetRecentAsync("Lagerbestand", SyncLogLevel.Warning, 10);
    warnings.Should().Contain(x => x.Message.Contains("UNKNOWN-ARTICLE"));
}

[Fact]
public async Task Run_UnknownLocation_SkipsAndLogsWarning()
{
    var (svc, reader, ctx, syncLogs) = Build();
    SeedArticle(ctx, id: 1, number: "A-1");
    await ctx.SaveChangesAsync();
    reader.Records = new() { new("A-1", "UNKNOWN-LOC", 5m) };

    var result = await svc.RunAsync(dryRun: false);

    result.Skipped.Should().Be(1);
    ctx.StockMovements.Should().BeEmpty();
    var warnings = await syncLogs.GetRecentAsync("Lagerbestand", SyncLogLevel.Warning, 10);
    warnings.Should().Contain(x => x.Message.Contains("UNKNOWN-LOC"));
}

[Fact]
public async Task Run_ManualLocation_SkipsAndLogsWarning()
{
    var (svc, reader, ctx, syncLogs) = Build();
    SeedArticle(ctx, id: 1, number: "A-1");
    ctx.StorageLocations.Add(new StorageLocation
    {
        Id = 1, Code = "MAN-1", BarcodeValue = "MAN-1",
        Source = StorageLocationSource.Manual, IsActive = true,
        IsPickingTransport = false,
        CreatedBy = "tester", CreatedByWindows = "tester"
    });
    await ctx.SaveChangesAsync();
    reader.Records = new() { new("A-1", "MAN-1", 5m) };

    var result = await svc.RunAsync(dryRun: false);

    result.Skipped.Should().Be(1);
    ctx.StockMovements.Should().BeEmpty();
    var warnings = await syncLogs.GetRecentAsync("Lagerbestand", SyncLogLevel.Warning, 10);
    warnings.Should().Contain(x => x.Message.Contains("Manual"));
}

[Fact]
public async Task Run_InactiveSageLocation_SkipsAndLogsWarning()
{
    var (svc, reader, ctx, syncLogs) = Build();
    SeedArticle(ctx, id: 1, number: "A-1");
    SeedSageLocation(ctx, id: 1, code: "L-1", isActive: false);
    await ctx.SaveChangesAsync();
    reader.Records = new() { new("A-1", "L-1", 5m) };

    var result = await svc.RunAsync(dryRun: false);

    result.Skipped.Should().Be(1);
    ctx.StockMovements.Should().BeEmpty();
    var warnings = await syncLogs.GetRecentAsync("Lagerbestand", SyncLogLevel.Warning, 10);
    warnings.Should().Contain(x => x.Message.Contains("deaktiviert"));
}
```

- [ ] **Step 2: Tests laufen — sollte direkt PASS**

```pwsh
dotnet test IDEALAKEWMSService.Tests/IDEALAKEWMSService.Tests.csproj --filter "FullyQualifiedName~LagerbestandSyncService" --nologo
```

Expected: 9/9 PASS (Skip-Logik wurde bereits in Task 6 implementiert).

- [ ] **Step 3: Commit**

```pwsh
git add IDEALAKEWMSService.Tests/Services/LagerbestandSyncServiceTests.cs
git commit -m "test(lagerbestand): skip-paths for unknown article + location + manual + inactive" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 8: Aggregation gegen mehrere Vor-Movements + Audit-Felder

**Files:**
- Modify: `IDEALAKEWMSService.Tests/Services/LagerbestandSyncServiceTests.cs`

- [ ] **Step 1: Tests anhaengen**

```csharp
[Fact]
public async Task Run_AggregatesMultiplePreMovements_BeforeComputingDelta()
{
    var (svc, reader, ctx, _) = Build();
    SeedArticle(ctx, id: 1, number: "A-1");
    SeedSageLocation(ctx, id: 1, code: "L-1");
    SeedSageLocation(ctx, id: 2, code: "L-2");
    ctx.StockMovements.AddRange(
        new StockMovement { ArticleId = 1, StorageLocationId = 1, Quantity = 10m, MovementType = MovementType.Einbuchung,    Timestamp = DateTime.Now, WindowsUser = "x", CreatedAt = DateTime.Now, CreatedBy = "x", CreatedByWindows = "x" },
        new StockMovement { ArticleId = 1, StorageLocationId = 1, Quantity = 4m,  MovementType = MovementType.Ausbuchung,    Timestamp = DateTime.Now, WindowsUser = "x", CreatedAt = DateTime.Now, CreatedBy = "x", CreatedByWindows = "x" },
        new StockMovement { ArticleId = 1, StorageLocationId = 1, Quantity = 2m,  MovementType = MovementType.Umbuchung,
                            SourceStorageLocationId = 2,
                            Timestamp = DateTime.Now, WindowsUser = "x", CreatedAt = DateTime.Now, CreatedBy = "x", CreatedByWindows = "x" }
    );
    await ctx.SaveChangesAsync();
    // Effektiver Bestand auf L-1: 10 - 4 + 2 = 8
    reader.Records = new() { new("A-1", "L-1", 6m) };

    var result = await svc.RunAsync(dryRun: false);

    result.CorrectionsMinus.Should().Be(1);
    var c = ctx.StockMovements.Single(m => m.MovementType == MovementType.SageAusbuchung);
    c.Quantity.Should().Be(2m);   // 8 -> 6, Korrektur -2
}

[Fact]
public async Task Run_CorrectionMovement_HasExpectedAuditFields()
{
    var (svc, reader, ctx, _) = Build();
    SeedArticle(ctx, id: 1, number: "A-1");
    SeedSageLocation(ctx, id: 1, code: "L-1");
    await ctx.SaveChangesAsync();
    reader.Records = new() { new("A-1", "L-1", 5m) };

    await svc.RunAsync(dryRun: false);

    var c = ctx.StockMovements.Single();
    c.WindowsUser.Should().Be(SyncUser);
    c.CreatedBy.Should().Be(SyncUser);
    c.UserId.Should().BeNull();
    c.ProductionOrder.Should().BeNull();
    c.Note.Should().NotBeNull();
    c.Note.Should().Contain("WMS=0");
    c.Note.Should().Contain("Sage=5");
    c.Timestamp.Should().BeCloseTo(DateTime.Now, TimeSpan.FromMinutes(1));
}
```

- [ ] **Step 2: Tests laufen — sollten direkt PASS**

```pwsh
dotnet test IDEALAKEWMSService.Tests/IDEALAKEWMSService.Tests.csproj --filter "FullyQualifiedName~LagerbestandSyncService" --nologo
```

Expected: 11/11 PASS.

- [ ] **Step 3: Commit**

```pwsh
git add IDEALAKEWMSService.Tests/Services/LagerbestandSyncServiceTests.cs
git commit -m "test(lagerbestand): aggregation against pre-movements + audit-field assertions" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 9: DryRun-Test

**Files:**
- Modify: `IDEALAKEWMSService.Tests/Services/LagerbestandSyncServiceTests.cs`

- [ ] **Step 1: Test anhaengen**

```csharp
[Fact]
public async Task Run_DryRun_DoesNotInsertButLogsCounts()
{
    var (svc, reader, ctx, syncLogs) = Build();
    SeedArticle(ctx, id: 1, number: "A-1");
    SeedSageLocation(ctx, id: 1, code: "L-1");
    await ctx.SaveChangesAsync();
    reader.Records = new() { new("A-1", "L-1", 5m) };

    var result = await svc.RunAsync(dryRun: true);

    result.CorrectionsPlus.Should().Be(1);
    result.DryRun.Should().BeTrue();
    ctx.StockMovements.Should().BeEmpty();   // KEIN Insert

    var summary = (await syncLogs.GetRecentAsync("Lagerbestand", SyncLogLevel.Info, 10)).First();
    summary.Message.Should().StartWith("[DryRun]");
}
```

- [ ] **Step 2: Test laufen — sollte direkt PASS**

```pwsh
dotnet test IDEALAKEWMSService.Tests/IDEALAKEWMSService.Tests.csproj --filter "Run_DryRun_" --nologo
```

Expected: 1/1 PASS (DryRun-Logik bereits in Task 5/6 implementiert).

- [ ] **Step 3: Commit**

```pwsh
git add IDEALAKEWMSService.Tests/Services/LagerbestandSyncServiceTests.cs
git commit -m "test(lagerbestand): DryRun mode skips insert but logs counts" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 10: Sage-Connection-Fehler + Sage-Duplicate-Tests

**Files:**
- Modify: `IDEALAKEWMSService.Tests/Services/LagerbestandSyncServiceTests.cs`
- Modify: `IDEALAKEWMSService/Services/LagerbestandSyncService.cs` (Dedup-Logik einbauen)

- [ ] **Step 1: Tests anhaengen**

```csharp
[Fact]
public async Task Run_SageReaderThrows_LogsErrorAndDoesNotCrash()
{
    var (svc, reader, ctx, syncLogs) = Build();
    reader.ThrowOnRead = new InvalidOperationException("Sage offline");

    var result = await svc.RunAsync(dryRun: false);

    result.Errors.Should().Be(1);
    ctx.StockMovements.Should().BeEmpty();

    var errors = await syncLogs.GetRecentAsync("Lagerbestand", SyncLogLevel.Error, 10);
    errors.Should().ContainSingle();
    errors[0].Message.Should().Contain("Sage offline");
}

[Fact]
public async Task Run_SageDuplicateTuple_SkipsAllAndLogsWarning()
{
    var (svc, reader, ctx, syncLogs) = Build();
    SeedArticle(ctx, id: 1, number: "A-1");
    SeedSageLocation(ctx, id: 1, code: "L-1");
    SeedSageLocation(ctx, id: 2, code: "L-2");
    await ctx.SaveChangesAsync();
    // Sage liefert (A-1, L-1) zweimal — z.B. aus zwei verschiedenen Lagerorten mit gleichem Lagerplatz-Code
    reader.Records = new()
    {
        new("A-1", "L-1", 5m),
        new("A-1", "L-1", 7m),
        new("A-1", "L-2", 3m)   // dieser sollte normal verarbeitet werden
    };

    var result = await svc.RunAsync(dryRun: false);

    // Nur der eindeutige (A-1, L-2)-Tupel wird verarbeitet
    result.CorrectionsPlus.Should().Be(1);
    var corrections = ctx.StockMovements
        .Where(m => m.MovementType == MovementType.SageEinbuchung).ToList();
    corrections.Should().ContainSingle();
    corrections[0].StorageLocationId.Should().Be(2);

    var warnings = await syncLogs.GetRecentAsync("Lagerbestand", SyncLogLevel.Warning, 10);
    warnings.Should().Contain(x => x.Message.Contains("mehrfach"));
}
```

- [ ] **Step 2: Tests laufen — Connection-Fehler PASS, Duplicate-Test FAIL**

```pwsh
dotnet test IDEALAKEWMSService.Tests/IDEALAKEWMSService.Tests.csproj --filter "FullyQualifiedName~LagerbestandSyncService" --nologo
```

Expected: 12 PASS, 1 FAIL (Sage-Duplicate-Logik fehlt).

- [ ] **Step 3: Sage-Duplicate-Detection einbauen**

In `LagerbestandSyncService.RunAsync`, NACH dem `_reader.GetAllAsync(ct)`-Try/Catch und VOR dem Pre-Loading-Block, einfuegen:

```csharp
// Sage-Duplikate erkennen: gleiche (Artikelnummer, Lagerplatz) aus mehreren Lagerorten
var dupGroups = sageRows
    .Where(r => !string.IsNullOrWhiteSpace(r.Artikelnummer) && !string.IsNullOrWhiteSpace(r.Lagerplatz))
    .GroupBy(r => (r.Artikelnummer!.Trim(), r.Lagerplatz!.Trim()),
             new TupleKeyComparer())
    .Where(g => g.Count() > 1)
    .ToList();

foreach (var group in dupGroups)
{
    await _syncLogs.AddAsync(new SyncLog
    {
        Service = ServiceName,
        Level = SyncLogLevel.Warning,
        Message = $"Sage liefert (Artikel '{group.Key.Item1}', Lagerplatz '{group.Key.Item2}') mehrfach. Tupel uebersprungen.",
        Reference = group.Key.Item2
    });
}

var dupKeys = dupGroups.Select(g => g.Key).ToHashSet(new TupleKeyComparer());
sageRows = sageRows
    .Where(r => string.IsNullOrWhiteSpace(r.Artikelnummer) || string.IsNullOrWhiteSpace(r.Lagerplatz)
             || !dupKeys.Contains((r.Artikelnummer!.Trim(), r.Lagerplatz!.Trim())))
    .ToList();
```

Plus eine private Comparer-Klasse am Ende der Klasse:

```csharp
private sealed class TupleKeyComparer : IEqualityComparer<(string, string)>
{
    public bool Equals((string, string) x, (string, string) y) =>
        string.Equals(x.Item1, y.Item1, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(x.Item2, y.Item2, StringComparison.OrdinalIgnoreCase);

    public int GetHashCode((string, string) obj) =>
        StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Item1) ^
        StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Item2);
}
```

- [ ] **Step 4: Tests laufen — alle 13 PASS**

```pwsh
dotnet test IDEALAKEWMSService.Tests/IDEALAKEWMSService.Tests.csproj --filter "FullyQualifiedName~LagerbestandSyncService" --nologo
```

Expected: 13/13 PASS.

- [ ] **Step 5: Vollstaendigen Test-Lauf**

```pwsh
dotnet test --nologo
```

Erwartete Anzahl: 531 + 13 (sync) + 6 (aggregation) + 1 (GetCurrentStockByArticleAndLocation) = ca. 551 — alle gruen.

- [ ] **Step 6: Commit**

```pwsh
git add IDEALAKEWMSService/Services/LagerbestandSyncService.cs IDEALAKEWMSService.Tests/Services/LagerbestandSyncServiceTests.cs
git commit -m "feat(lagerbestand): sage-duplicate detection + connection-error handling" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Phase 4 — Worker Integration

### Task 11: SyncWorker Integration + ServiceSetting

**Files:**
- Modify: `IDEALAKEWMSService/Workers/SyncWorker.cs`
- Modify: `IDEALAKEWMSService/Program.cs` (DI fuer LagerbestandSyncService)
- Modify: `IDEALAKEWMSService/appsettings.json`

- [ ] **Step 1: DI Registration**

In `IDEALAKEWMSService/Program.cs`, im Services-Block (nach `ILagerplatzSyncService`):

```csharp
builder.Services.AddScoped<ILagerbestandSyncService, LagerbestandSyncService>();
```

Plus: `IStockMovementRepository` muss im Service-Projekt registriert sein (wird vom LagerbestandSyncService injiziert). Falls nicht vorhanden, ergaenzen:

```csharp
builder.Services.AddScoped<IStockMovementRepository, StockMovementRepository>();
```

- [ ] **Step 2: Tracking-Field im SyncWorker**

In `IDEALAKEWMSService/Workers/SyncWorker.cs`, oben bei den `_lastXyzRun`-Feldern ergaenzen:

```csharp
private DateTime? _lastLagerbestandRun;
```

- [ ] **Step 3: ShouldRunLagerbestand-Helper**

Am Ende der `SyncWorker`-Klasse (analog `ShouldRunAutoPauseAsync`):

```csharp
private async Task<bool> ShouldRunLagerbestandAsync(CancellationToken ct)
{
    if (!_configuration.GetValue<bool>("Sync:LagerbestandEnabled", false))
        return false;

    var overrideMinutes = _configuration.GetValue<int>("Sync:LagerbestandIntervalMinutes", 0);
    if (overrideMinutes <= 0)
        return true;   // nutzt Worker-Standard-Intervall (15 Min)

    if (_lastLagerbestandRun == null) return true;
    return DateTime.Now - _lastLagerbestandRun.Value >= TimeSpan.FromMinutes(overrideMinutes);
}
```

- [ ] **Step 4: Worker-Block einfuegen**

In `IDEALAKEWMSService/Workers/SyncWorker.cs`, am Ende des `try`-Blocks (NACH dem Lagerplatz-Sync-Block, VOR dem `catch`):

```csharp
// ---------------------------------------------------------------
// Lagerbestand-Sync (Sage Bestand-Korrektur)
// ---------------------------------------------------------------
if (await ShouldRunLagerbestandAsync(stoppingToken))
{
    try
    {
        _logger.LogInformation("Lagerbestand-Sync startet (DryRun={DryRun})...", dryRun);
        using var lbScope = _scopeFactory.CreateScope();
        var lbSync = lbScope.ServiceProvider.GetRequiredService<ILagerbestandSyncService>();
        var lbResult = await lbSync.RunAsync(dryRun, stoppingToken);
        _logger.LogInformation(
            "Lagerbestand-Sync: {Tuples} Tupel, {Plus} Plus, {Minus} Minus, {NoChange} ohne Aenderung, {Skipped} uebersprungen, {Errors} Fehler.",
            lbResult.Tuples, lbResult.CorrectionsPlus, lbResult.CorrectionsMinus,
            lbResult.NoChange, lbResult.Skipped, lbResult.Errors);
        _lastLagerbestandRun = DateTime.Now;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Lagerbestand-Sync ist fehlgeschlagen.");
    }
}
```

- [ ] **Step 5: Default in appsettings.json**

In `IDEALAKEWMSService/appsettings.json`, im `Sync`-Block (nach `LagerplaetzeEnabled`):

```json
"LagerbestandEnabled": false,
"LagerbestandIntervalMinutes": 0,
```

- [ ] **Step 6: Build verifizieren**

```pwsh
dotnet build --nologo
```

Expected: `0 Fehler`.

- [ ] **Step 7: Commit**

```pwsh
git add IDEALAKEWMSService/Program.cs IDEALAKEWMSService/Workers/SyncWorker.cs IDEALAKEWMSService/appsettings.json
git commit -m "feat(lagerbestand): integrate sync into SyncWorker, gated by Sync:LagerbestandEnabled" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Phase 5 — Web-UI

### Task 12: Bewegungshistorie UI Updates

**Files:**
- Modify: `IdealAkeWms/Views/StockMovements/Index.cshtml`
- Modify: `IdealAkeWms/wwwroot/css/site.css`
- Possibly modify: ViewModels falls Note nicht durchgereicht wird (sollte bereits in Task 2 erledigt sein)

- [ ] **Step 1: Filter-Dropdown ergaenzen**

In `IdealAkeWms/Views/StockMovements/Index.cshtml`, im Filter-Dropdown (vorhandene `<option>`-Liste, Zeile ~39-41):

```cshtml
<option value="0" selected="@(Model.FilterMovementType == MovementType.Einbuchung)">Einbuchung</option>
<option value="1" selected="@(Model.FilterMovementType == MovementType.Ausbuchung)">Ausbuchung</option>
<option value="2" selected="@(Model.FilterMovementType == MovementType.Umbuchung)">Umbuchung</option>
<option value="3" selected="@(Model.FilterMovementType == MovementType.SageEinbuchung)">Sage-Einbuchung</option>
<option value="4" selected="@(Model.FilterMovementType == MovementType.SageAusbuchung)">Sage-Ausbuchung</option>
```

- [ ] **Step 2: Badge-Klassen-Switch erweitern**

In `IdealAkeWms/Views/StockMovements/Index.cshtml`, im Badge-Klassen-Mapping (Zeile ~111-112):

Ersetze:
```cshtml
var badgeClass = item.MovementType == MovementType.Einbuchung ? "badge-einbuchung" :
                 item.MovementType == MovementType.Umbuchung ? "badge-umbuchung" : "badge-ausbuchung";
```

Mit:
```cshtml
var badgeClass = item.MovementType switch
{
    MovementType.Einbuchung => "badge-einbuchung",
    MovementType.Umbuchung => "badge-umbuchung",
    MovementType.Ausbuchung => "badge-ausbuchung",
    MovementType.SageEinbuchung => "badge-sage-einbuchung",
    MovementType.SageAusbuchung => "badge-sage-ausbuchung",
    _ => "badge-ausbuchung"
};
```

- [ ] **Step 3: Note-Spalte in der Tabelle ergaenzen**

In `Index.cshtml`, in der `<thead>` neue `<th>` an passender Stelle (vor "Aktionen"):

```cshtml
<th data-col-key="note">Notiz</th>
```

Im `<tbody>` pro Zeile entsprechende `<td>`:

```cshtml
<td data-col-key="note">@(item.Note ?? "")</td>
```

- [ ] **Step 4: CSS-Klassen ergaenzen**

In `IdealAkeWms/wwwroot/css/site.css`, am Ende ergaenzen:

```css
/* Sage-Korrektur-Buchungen — visuell unterschiedlich von manuellen Buchungen */
.badge-sage-einbuchung {
    background-color: var(--ake-primary);
    color: white;
}

.badge-sage-ausbuchung {
    background-color: var(--ake-secondary);
    color: white;
}
```

- [ ] **Step 5: Build + manueller Smoke-Test**

```pwsh
dotnet build IdealAkeWms/IdealAkeWms.csproj --nologo
dotnet test --nologo
```

Expected: alles gruen. Web-App starten, Bewegungshistorie aufrufen, neue Filter-Optionen sichtbar.

- [ ] **Step 6: Commit**

```pwsh
git add IdealAkeWms/Views/StockMovements/Index.cshtml IdealAkeWms/wwwroot/css/site.css
git commit -m "feat(lagerbestand): show Sage* movement types in history with badges + Note column" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 13: SyncLogController KnownServices Update

**Files:**
- Modify: `IdealAkeWms/Controllers/SyncLogController.cs`

- [ ] **Step 1: KnownServices ergaenzen**

In `IdealAkeWms/Controllers/SyncLogController.cs`, das `KnownServices`-Array um `"Lagerbestand"` ergaenzen:

```csharp
private static readonly string[] KnownServices = new[]
{
    "Lagerplatz", "Lagerbestand", "OseonTracking", "Article", "ProductionOrder", "EnaioDms", "BomCache", "Holiday"
};
```

- [ ] **Step 2: Build + Tests**

```pwsh
dotnet build --nologo
dotnet test --nologo --no-build
```

- [ ] **Step 3: Commit**

```pwsh
git add IdealAkeWms/Controllers/SyncLogController.cs
git commit -m "feat(lagerbestand): add Lagerbestand to SyncLog known-services filter" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Phase 6 — Doku

### Task 14: AppVersion + Changelog + Hilfeseite + PROJECT_STATUS + CLAUDE.md

**Files:**
- Modify: `IdealAkeWms/AppVersion.cs`
- Modify: `IDEALAKEWMSService/AppVersion.cs`
- Modify: `IdealAkeWms/Views/Help/Changelog.cshtml`
- Modify: `IdealAkeWms/Views/Help/Index.cshtml`
- Modify: `PROJECT_STATUS.md`
- Modify: `CLAUDE.md`

- [ ] **Step 1: AppVersion auf v1.10.0**

In beiden `AppVersion.cs`-Dateien:

```csharp
public const string Version = "1.10.0";
public const string Date = "2026-05-06";
```

(Datum auf den Implementations-Tag setzen.)

- [ ] **Step 2: Changelog-Eintrag**

In `IdealAkeWms/Views/Help/Changelog.cshtml`, oberhalb des v1.9.0-Cards:

```cshtml
<div class="card mb-3">
    <div class="card-header text-white" style="background-color: var(--ake-primary);">
        <strong>v1.10.0</strong> <span class="text-white-50 ms-2">06.05.2026</span>
    </div>
    <div class="card-body">
        <h6>Sage Lagerbestand-Sync (Phase 2)</h6>
        <ul>
            <li><strong>Automatischer Bestand-Abgleich</strong> mit Sage. Bei Abweichung werden Korrektur-Buchungen mit neuen Bewegungsarten <strong>Sage-Einbuchung</strong> und <strong>Sage-Ausbuchung</strong> erzeugt — die WMS-Bewegungshistorie bleibt vollst&auml;ndig erhalten.</li>
            <li>Aktivieren via ServiceSetting <code>Sync:LagerbestandEnabled</code>. Optional eigenes Intervall via <code>Sync:LagerbestandIntervalMinutes</code> (Default: nutzt Worker-Standard-15-Min).</li>
            <li>Strikte Filterung: nur <strong>Source=Sage</strong>-Lagerpl&auml;tze werden korrigiert. Manuelle Lagerpl&auml;tze, fehlende Artikel oder unbekannte/inaktive Lagerpl&auml;tze werden uebersprungen + im <a asp-controller="SyncLog" asp-action="Index">Sync-Protokoll</a> als Warning erfasst.</li>
            <li>Neues optionales <strong>Notiz</strong>-Feld auf Buchungen — bei Sage-Korrekturen automatisch gef&uuml;llt mit "Sage-Korrektur: WMS=X, Sage=Y, Diff=Z". Sichtbar als neue Spalte in der Bewegungshistorie.</li>
            <li>Bewegungshistorie hat zwei neue Filter-Optionen f&uuml;r die neuen Bewegungsarten.</li>
        </ul>
    </div>
</div>
```

- [ ] **Step 3: Hilfeseite-Eintrag**

In `IdealAkeWms/Views/Help/Index.cshtml`, NACH dem Phase-1-Abschnitt "Lagerplatz-Sync mit Sage", neuen Abschnitt einfuegen:

```cshtml
<div class="card mb-3">
    <div class="card-header" style="background-color: var(--ake-primary); color: white;">
        Lagerbestand-Sync mit Sage
    </div>
    <div class="card-body">
        <p>Erg&auml;nzend zum Lagerplatz-Sync (Phase 1) gleicht Phase 2 die <strong>Lagerbest&auml;nde pro Artikel und Lagerplatz</strong> mit Sage ab. Aktiviert wird das in den Service-Einstellungen unter <code>Sync:LagerbestandEnabled</code>.</p>
        <h6>Was wird synchronisiert</h6>
        <ul>
            <li>Sage liefert pro (Artikel, Lagerplatz) den aktuellen Bestand. Wenn der WMS-Bestand abweicht, wird eine Korrektur-Buchung erzeugt:
                <ul>
                    <li><strong>Sage-Einbuchung</strong> wenn Sage h&ouml;heren Bestand hat (WMS war zu niedrig)</li>
                    <li><strong>Sage-Ausbuchung</strong> wenn Sage niedrigeren Bestand hat (WMS war zu hoch)</li>
                </ul>
            </li>
            <li>Korrektur-Buchungen erscheinen in der <strong>Bewegungshistorie</strong> mit eigenem Badge und der Spalte <strong>Notiz</strong>, in der die Berechnung dokumentiert ist.</li>
            <li>WMS-Bewegungshistorie wird <strong>nicht</strong> gel&ouml;scht oder veraendert — Sage-Korrekturen sind eigene Eintr&auml;ge.</li>
        </ul>
        <h6>Vor erstem Aktivieren — DryRun-Empfehlung</h6>
        <p>Beim ersten Sync nach Aktivierung k&ouml;nnen <strong>tausende Korrektur-Buchungen</strong> entstehen — je nachdem, wie sehr der WMS-Bestand vom Sage-Stand abweicht. Empfohlener Workflow:</p>
        <ol>
            <li><code>Sync:LagerbestandEnabled = true</code> setzen.</li>
            <li><strong>Wichtig:</strong> <code>WorkerSettings:SyncDryRun = true</code> aktivieren.</li>
            <li>Service neu starten, einen Sync-Lauf abwarten.</li>
            <li>Im <a asp-controller="SyncLog" asp-action="Index">Sync-Protokoll</a> den Lagerbestand-Service filtern. Der Summen-Eintrag enth&auml;lt <code>[DryRun] Sync OK: ...</code> mit den geplanten Korrekturen.</li>
            <li>Stichproben aus den Warnings pruefen.</li>
            <li>Falls plausibel: <code>WorkerSettings:SyncDryRun = false</code>, n&auml;chster Sync schreibt die Korrekturen.</li>
        </ol>
        <h6>Phase-1-Voraussetzung</h6>
        <p>Phase 2 korrigiert <strong>nur Lagerpl&auml;tze mit Quelle Sage</strong>. Manuell angelegte Lagerpl&auml;tze (auch wenn der Code zuf&auml;llig zu Sage passt) werden uebersprungen. Vor Phase-2-Aktivierung empfehlen wir, die Phase-1-Konfliktliste durchzuarbeiten.</p>
        <h6>Frequenz konfigurieren</h6>
        <p>Default-Intervall ist das gemeinsame <code>WorkerSettings:SyncIntervalMinutes</code> (15 Min). Falls Phase 2 seltener laufen soll (z.B. nur 1x pro Stunde), <code>Sync:LagerbestandIntervalMinutes</code> auf gewuenschten Wert setzen (in Minuten). Wert 0 = Standard-Intervall verwenden.</p>
    </div>
</div>
```

- [ ] **Step 4: PROJECT_STATUS.md**

Roadmap-Eintrag ergaenzen:

```markdown
- v1.10.0 (2026-05-06) — Sage Lagerbestand-Sync (Phase 2). Bestand-Abgleich mit Korrektur-Buchungen.
```

Plus Aenderungen-Sektion analog zu v1.9.0.

- [ ] **Step 5: CLAUDE.md**

Im ServiceSettings-Block die neuen Zeilen ergaenzen:

```markdown
| `Sync:LagerbestandEnabled` | `false` | Sage-Lagerbestand-Sync aktiv (Phase 2) |
| `Sync:LagerbestandIntervalMinutes` | `0` | Eigenes Intervall in Min (0 = nutzt SyncIntervalMinutes) |
```

Plus: im "Bekannte Fallstricke"-Abschnitt einen neuen Eintrag:

```markdown
- **MovementType-Aggregation**: Bei jeder neuen `MovementType`-Erweiterung muss die Aggregations-Logik in `StockMovementRepository` (5 Stellen) und `PickingTransferService` aktualisiert werden. Insbesondere die kollabierten Switches (z.B. `Ausbuchung ? -Quantity : Quantity`) sind gefaehrlich, weil sie unbekannte Werte still falsch behandeln.
```

- [ ] **Step 6: Build + alle Tests**

```pwsh
dotnet build --nologo
dotnet test --nologo --no-build
```

Expected: alles gruen. Erwartete Test-Anzahl ca. 551.

- [ ] **Step 7: Commit**

```pwsh
git add IdealAkeWms/AppVersion.cs IDEALAKEWMSService/AppVersion.cs IdealAkeWms/Views/Help/Changelog.cshtml IdealAkeWms/Views/Help/Index.cshtml PROJECT_STATUS.md CLAUDE.md
git commit -m "docs: v1.10.0 — Sage Lagerbestand-Sync release notes + help page" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Manuelle Verifikation (vor Merge)

- **DryRun gegen echte Sage-DB:** `Sync:LagerbestandEnabled=true` + `SyncDryRun=true`. Worker-Log + SyncLog auf Plausibilitaet pruefen.
- **Sage-Schema verifizieren:** `KHKLagerplatzbestaende.Bestand` ist `decimal`/`numeric`? Andernfalls den `Convert.ToDecimal`-Pfad nochmal pruefen.
- **Bewegungshistorie**: nach erstem echten Sync die neuen Eintraege optisch verifizieren (Badge, Note-Spalte, Filter-Dropdown).
- **Bestandsuebersicht**: Sage-Korrekturen fliessen in `GetCurrentStockAsync` ein → Bestand pro Artikel/Lagerplatz stimmt mit Sage.

---

## Self-Review-Notiz

Der Plan deckt die Spec-Sektionen vollstaendig:
- Datenmodell (MovementType + Note + Migration) → Task 1
- Aggregations-Audit + neue Repo-Methode → Tasks 2-3
- SageBestandReader → Task 4
- LagerbestandSyncService mit allen 14 Spec-Tests → Tasks 5-10
- Worker-Integration + ServiceSetting → Task 11
- UI-Updates (Filter, Badge, Note-Spalte) → Task 12
- SyncLogController-Update → Task 13
- Doku → Task 14

Test-Mapping:
- Spec-Test 1 (Insert-Plus) → Task 5
- Spec-Tests 2/3/4/9 (Insert-Minus, No-Change, Decimal-Diff, Bestand-NULL) → Task 6
- Spec-Tests 5/6/7/8 (4 Skip-Pfade) → Task 7
- Spec-Test 10 + 14 (Aggregation, Audit-Felder) → Task 8
- Spec-Test 11 (DryRun) → Task 9
- Spec-Tests 12/13 (Connection-Fehler, Sage-Duplicate) → Task 10
- Spec-Tests 15-18 (Aggregations-Audit-Tests) → Task 2
