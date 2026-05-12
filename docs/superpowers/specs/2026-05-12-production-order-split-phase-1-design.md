# ProductionOrder-Split — Phase 1 Big-Bang Schema-Refactor — Design Spec

**Datum:** 2026-05-12
**Branch:** `refactor/production-order-split` (eigener WorkTree)
**Status:** Approved → Plan
**Phase:** Phase 1 von 5 (siehe Roadmap). AppVersion-Bump auf `1.11.0`.
**Roadmap-Referenz:** `docs/superpowers/specs/2026-05-12-production-order-split-roadmap.md`

---

## 1. Problemstellung

Die `ProductionOrders`-Tabelle hat heute **28 Spalten** und mischt drei Concerns (Sage-Master, Picking-Workflow, Baugruppen-Flags). Jede Iteration weitet die Tabelle aus, ViewModel und Repository laden volle Datensätze auch für Teil-Use-Cases. Phase 1 räumt das durch atomaren Big-Bang-Refactor in einem Wartungsfenster auf: 14 alte Spalten werden in 5 neue Tabellen migriert und in `ProductionOrders` gedroppt.

## 2. Ziele

1. **Schlanke `ProductionOrders`** — nur Sage-Master + Cross-Cutting (`ProductionWorkplaceId`, `IsDone`).
2. **5 neue Tabellen** mit FK auf FA:
   - `ProductionOrderPickingStatus` (1:1, eager)
   - `ProductionOrderBdeStatus` (1:1, eager)
   - `ProductionOrderAssemblyGroups` (1:N, exakt 5 Zeilen/FA: `VK`/`VL`/`VE`/`VT`/`VA`, eager)
   - `ProductionOrderAssemblyGroupSpecs` (1:N zu AssemblyGroup, in Phase 1 leer)
   - `ProductionWorkplaceAssemblyGroups` (Junction Werkbank → GroupKey, in Phase 1 leer)
3. **Datenmigration** via Batched-INSERT (5000 Zeilen/Batch). 100 % der bisherigen FA-Status-Werte landen in den neuen Tabellen.
4. **Drop** der 14 App-verwalteten Spalten aus `ProductionOrders`.
5. **Toggle-API in 3 separate Endpoints** aufgeteilt (siehe 6.2).
6. **Sage-AgentJob** mit Folge-MERGEs für die neuen Tabellen (eager-create für jeden neu importierten FA).
7. App, Repositories, ViewModel, View, JS lesen/schreiben ausschließlich aus den neuen Tabellen.

## 3. Out-of-Scope (Phase 1)

- **`AssemblyGroupSpecs`-UI** — Tabelle wird leer angelegt, Pflege erst in Phase 4 (`FaCompletion`).
- **`ProductionWorkplaceAssemblyGroups`-Pflege** — Junction-Schema wird leer angelegt, Pflege erst in Phase 5 (`Workstation/Specs`).
- **View-Split (Index → Picking-Leitstand)** — kommt erst in Phase 2.
- **Neue Rolle `fa_completion`** — erst Phase 4.
- **Backward-Compat-Schicht** — atomar, kein Dual-Write.
- **OSEON, WorkOperations, BOM** — bleiben unberührt.

## 4. Neue Tabellen-Architektur

### 4.1 `ProductionOrders` nach Refactor

Verbleibende Spalten:

| Spalte | Typ | Quelle |
|---|---|---|
| `Id` | INT IDENTITY | App |
| `OrderNumber` | NVARCHAR(100) UNIQUE | Sage |
| `Quantity` | DECIMAL(18,3) | Sage |
| `Customer` | NVARCHAR(500) | Sage |
| `ArticleNumber` | NVARCHAR(100) | Sage |
| `Description1` | NVARCHAR(500) | Sage |
| `Description2` | NVARCHAR(500) | Sage |
| `ProductionDate` | DATETIME2 | Sage |
| `DeliveryDate` | DATETIME2 | Sage |
| `ProductionWorkplaceId` | INT NULL | Cross-cutting (Werkbank-Zuweisung) |
| `IsDone` | BIT | Sage-Master-Abschluss |
| Audit-Felder | — | App |

**Entfernt** (in Migration kopiert + Spalten gedroppt):

- Picking-Block (7): `PickingStatus`, `PickingPriority`, `IsReleasedForPicking`, `ReleasedAt`, `ReleasedBy`, `AssignedPickerId`, `AssignedPickerName`
- Coating-Block (4): `HasGlass`, `HasExternalPurchase`, `HasCoatingParts`, `IsCoatingDone`
- Baugruppen-Block (5): `HasCooling`, `HasFan`, `HasElectric`, `HasDoors`, `HasSuperstructure`

= 16 Spalten verschoben → roadmap zählt 14, weil `HasCoatingParts` und `IsCoatingDone` fachlich in den Picking-Status wandern. Tatsächlich sind es 16 physische Spalten-Drops. (Diskrepanz zur Roadmap-Zahl ist kosmetisch — Plan listet alle 16 namentlich auf.)

### 4.2 `ProductionOrderPickingStatus`

1:1 zu FA, **eager-created** bei Migration und im Sage-AgentJob.

```
Id                      INT IDENTITY  PK
ProductionOrderId       INT           FK ProductionOrders.Id ON DELETE CASCADE, UNIQUE
PickingStatus           NVARCHAR(50)  NULL
PickingPriority         INT           NULL
IsReleasedForPicking    BIT           NOT NULL DEFAULT 0
ReleasedAt              DATETIME2     NULL
ReleasedBy              NVARCHAR(200) NULL
AssignedPickerId        INT           NULL  FK Users.Id ON DELETE SET NULL
AssignedPickerName      NVARCHAR(200) NULL
HasGlass                BIT           NOT NULL DEFAULT 0
HasExternalPurchase     BIT           NOT NULL DEFAULT 0
HasCoatingParts         BIT           NOT NULL DEFAULT 0   -- sync-calculated
IsCoatingDone           BIT           NOT NULL DEFAULT 0   -- user-toggleable
IsDonePicking           BIT           NOT NULL DEFAULT 0   -- neuer Flag, ergänzt IsDone
CreatedAt/By/ByWindows, ModifiedAt/By/ByWindows
```

**Indexes:**
- `UQ_ProductionOrderPickingStatus_ProductionOrderId` UNIQUE (`ProductionOrderId`)
- `IX_ProductionOrderPickingStatus_IsReleasedForPicking` (`IsReleasedForPicking`) INCLUDE (`PickingPriority`) — ersetzt heutigen `IX_ProductionOrders_IsReleasedForPicking_IsDone` (IsDone bleibt in `ProductionOrders` und wird per JOIN gefiltert).
- `IX_ProductionOrderPickingStatus_AssignedPickerId` (`AssignedPickerId`) WHERE `AssignedPickerId IS NOT NULL`.

### 4.3 `ProductionOrderBdeStatus`

1:1 zu FA, eager-created.

```
Id                  INT IDENTITY  PK
ProductionOrderId   INT           FK ProductionOrders.Id ON DELETE CASCADE, UNIQUE
IsDoneBde           BIT           NOT NULL DEFAULT 0
CreatedAt/By/ByWindows, ModifiedAt/By/ByWindows
```

**Indexes:**
- `UQ_ProductionOrderBdeStatus_ProductionOrderId` UNIQUE (`ProductionOrderId`)

Phase-3-Erweiterungen (Werkbank-Spezifika, KPIs) hängen sich später an.

### 4.4 `ProductionOrderAssemblyGroups`

1:N zu FA. **Exakt 5 Zeilen pro FA** (`VK`, `VL`, `VE`, `VT`, `VA`), eager-created.

```
Id                  INT IDENTITY  PK
ProductionOrderId   INT           FK ProductionOrders.Id ON DELETE CASCADE
GroupKey            NVARCHAR(10)  NOT NULL  -- VK/VL/VE/VT/VA
IsApplicable        BIT           NOT NULL DEFAULT 0
IsCompleted         BIT           NOT NULL DEFAULT 0
CompletedAt         DATETIME2     NULL
CompletedBy         NVARCHAR(200) NULL
CreatedAt/By/ByWindows, ModifiedAt/By/ByWindows
```

**Indexes:**
- `UQ_ProductionOrderAssemblyGroups_PO_Key` UNIQUE (`ProductionOrderId`, `GroupKey`)
- `IX_ProductionOrderAssemblyGroups_GroupKey_IsApplicable` (`GroupKey`, `IsApplicable`) — fuer Phase-5-Filter "alle FAs mit Gruppe X applicable".

**Migration-Mapping:**
- `HasCooling` → `GroupKey='VK'`, `IsApplicable = HasCooling`
- `HasFan` → `GroupKey='VL'`, `IsApplicable = HasFan`
- `HasElectric` → `GroupKey='VE'`, `IsApplicable = HasElectric`
- `HasDoors` → `GroupKey='VT'`, `IsApplicable = HasDoors`
- `HasSuperstructure` → `GroupKey='VA'`, `IsApplicable = HasSuperstructure`

`IsCompleted = 0` für alle 5N migrierten Zeilen (User pflegt erst in Phase 4).

### 4.5 `ProductionOrderAssemblyGroupSpecs`

1:N zu `ProductionOrderAssemblyGroups`. In Phase 1 **leer angelegt**.

```
Id                  INT IDENTITY  PK
AssemblyGroupId     INT           FK ProductionOrderAssemblyGroups.Id ON DELETE CASCADE
ArticleId           INT           NULL  FK Articles.Id ON DELETE SET NULL   -- Q10
Description         NVARCHAR(500) NOT NULL
Quantity            DECIMAL(18,3) NULL
Notes              NVARCHAR(MAX)  NULL
SortOrder           INT           NOT NULL DEFAULT 0
CreatedAt/By/ByWindows, ModifiedAt/By/ByWindows
```

**Indexes:**
- `IX_ProductionOrderAssemblyGroupSpecs_AssemblyGroupId` (`AssemblyGroupId`)
- `IX_ProductionOrderAssemblyGroupSpecs_ArticleId` (`ArticleId`) WHERE `ArticleId IS NOT NULL`

### 4.6 `ProductionWorkplaceAssemblyGroups`

Junction-Tabelle Werkbank → GroupKey. In Phase 1 **leer angelegt**.

```
Id                          INT IDENTITY  PK
ProductionWorkplaceId       INT           FK ProductionWorkplaces.Id ON DELETE CASCADE
GroupKey                    NVARCHAR(10)  NOT NULL
CreatedAt/By/ByWindows, ModifiedAt/By/ByWindows
```

**Indexes:**
- `UQ_ProductionWorkplaceAssemblyGroups_WP_Key` UNIQUE (`ProductionWorkplaceId`, `GroupKey`)

## 5. EF-Modell + Navigation Properties

### 5.1 `ProductionOrder.cs` — Slim-Version

Entferne 16 Properties (siehe 4.1). Verbleibende Nav-Properties:

```csharp
public ICollection<WorkOperation> WorkOperations { get; set; } = new List<WorkOperation>();
public ProductionWorkplace? ProductionWorkplace { get; set; }
public ProductionOrderPickingStatus? PickingStatus { get; set; }      // 1:1, navigationsweise
public ProductionOrderBdeStatus? BdeStatus { get; set; }              // 1:1
public ICollection<ProductionOrderAssemblyGroup> AssemblyGroups { get; set; } = new List<ProductionOrderAssemblyGroup>();
```

`ProductionWorkplaceId` bleibt als Cross-Cutting-FK direkt auf `ProductionOrder`.

**Bisheriges `PickingStatus`-String-Property** wird durch Navigation-Property mit gleichem Namen ersetzt — Konsumenten müssen auf `o.PickingStatus?.PickingStatus` umstellen. Begründung: Bewusst gleiche Property-Bezeichnung, damit jede Code-Stelle, die `o.PickingStatus` liest, zwangsweise vom Compiler markiert wird (Type-Mismatch `string?` → `ProductionOrderPickingStatus?`).

### 5.2 `ApplicationDbContext` — DbSets + Beziehungen

- `DbSet<ProductionOrderPickingStatus> ProductionOrderPickingStatuses`
- `DbSet<ProductionOrderBdeStatus> ProductionOrderBdeStatuses`
- `DbSet<ProductionOrderAssemblyGroup> ProductionOrderAssemblyGroups`
- `DbSet<ProductionOrderAssemblyGroupSpec> ProductionOrderAssemblyGroupSpecs`
- `DbSet<ProductionWorkplaceAssemblyGroup> ProductionWorkplaceAssemblyGroups`

Relationships (alle in `OnModelCreating`):

```csharp
modelBuilder.Entity<ProductionOrderPickingStatus>(entity =>
{
    entity.ToTable("ProductionOrderPickingStatus");
    entity.HasKey(e => e.Id);
    entity.HasIndex(e => e.ProductionOrderId).IsUnique();
    entity.HasOne(e => e.ProductionOrder)
        .WithOne(p => p.PickingStatus)
        .HasForeignKey<ProductionOrderPickingStatus>(e => e.ProductionOrderId)
        .OnDelete(DeleteBehavior.Cascade);
    entity.HasOne(e => e.AssignedPicker)
        .WithMany()
        .HasForeignKey(e => e.AssignedPickerId)
        .OnDelete(DeleteBehavior.SetNull);
    entity.HasIndex(e => e.IsReleasedForPicking)
        .HasDatabaseName("IX_ProductionOrderPickingStatus_IsReleasedForPicking");
});

modelBuilder.Entity<ProductionOrderAssemblyGroup>(entity =>
{
    entity.ToTable("ProductionOrderAssemblyGroups");
    entity.HasIndex(e => new { e.ProductionOrderId, e.GroupKey }).IsUnique();
    entity.HasOne(e => e.ProductionOrder)
        .WithMany(p => p.AssemblyGroups)
        .HasForeignKey(e => e.ProductionOrderId)
        .OnDelete(DeleteBehavior.Cascade);
});

modelBuilder.Entity<ProductionOrderAssemblyGroupSpec>(entity =>
{
    entity.HasOne(e => e.AssemblyGroup).WithMany(g => g.Specs)
        .HasForeignKey(e => e.AssemblyGroupId).OnDelete(DeleteBehavior.Cascade);
    entity.HasOne(e => e.Article).WithMany()
        .HasForeignKey(e => e.ArticleId).OnDelete(DeleteBehavior.SetNull);  // Q10
});
```

## 6. Toggle-API — Separate Endpoints (Q9)

### 6.1 Heute (zu entfernen)

`POST /api/productionorders/toggle-field` mit Field-Whitelist `[HasGlass, HasExternalPurchase, IsCoatingDone, HasCooling, HasFan, HasElectric, HasDoors, HasSuperstructure]` → eine SetStatement-Kaskade in [`ProductionOrdersApiController.cs:40-69`](IdealAkeWms/Controllers/ProductionOrdersApiController.cs#L40-L69).

### 6.2 Nach Refactor

Drei eigenständige Controller mit eigenen Routen und Field-Whitelists. **Kein interner Dispatch-Dictionary** — jeder Endpoint hat seine eigene `if`-Kaskade über die jeweiligen Felder.

#### `POST /api/picking-status/toggle`

```csharp
[Route("api/picking-status")]
[ApiController]
[RequirePickingAccess]
public class PickingStatusApiController : ControllerBase
{
    private static readonly HashSet<string> AllowedFields = [
        "HasGlass", "HasExternalPurchase", "IsCoatingDone", "IsDonePicking"
    ];

    [HttpPost("toggle")]
    public async Task<IActionResult> Toggle([FromBody] PickingStatusToggleRequest req) { ... }
}

public class PickingStatusToggleRequest
{
    public int ProductionOrderId { get; set; }
    public string Field { get; set; } = string.Empty;
    public bool Value { get; set; }
}
```

#### `POST /api/assembly-groups/toggle-applicable`

```csharp
[Route("api/assembly-groups")]
[ApiController]
[RequirePickingAccess]
public class AssemblyGroupsApiController : ControllerBase
{
    private static readonly HashSet<string> AllowedGroupKeys = ["VK", "VL", "VE", "VT", "VA"];

    [HttpPost("toggle-applicable")]
    public async Task<IActionResult> ToggleApplicable([FromBody] AssemblyGroupToggleRequest req) { ... }
}

public class AssemblyGroupToggleRequest
{
    public int ProductionOrderId { get; set; }
    public string GroupKey { get; set; } = string.Empty;
    public bool Value { get; set; }
}
```

#### `POST /api/bde-status/toggle`

```csharp
[Route("api/bde-status")]
[ApiController]
[RequirePickingAccess]
public class BdeStatusApiController : ControllerBase
{
    private static readonly HashSet<string> AllowedFields = ["IsDoneBde"];

    [HttpPost("toggle")]
    public async Task<IActionResult> Toggle([FromBody] BdeStatusToggleRequest req) { ... }
}

public class BdeStatusToggleRequest
{
    public int ProductionOrderId { get; set; }
    public string Field { get; set; } = string.Empty;
    public bool Value { get; set; }
}
```

**Permission Phase 1:** alle drei `[RequirePickingAccess]`. Phase 4 erweitert `assembly-groups/toggle-applicable` zusätzlich für `fa_completion`-Rolle (kein Phase-1-Scope).

**Validierung pro Endpoint:**
- `404 NotFound` wenn FA nicht existiert.
- `400 BadRequest` wenn `Field` / `GroupKey` nicht in Whitelist.
- `404 NotFound` wenn AssemblyGroup-Zeile für FA + GroupKey fehlt (sollte durch eager-create nie passieren; defensive Antwort).
- Audit-Felder (`ModifiedAt`/`By`/`ByWindows`) auf der Status-Zeile setzen, nicht auf FA.

### 6.3 View-JS Dispatcher

Checkboxen in `Views/ProductionOrders/Index.cshtml` bekommen 3 neue `data-*` Attribute:

```html
<!-- HasGlass -->
<input type="checkbox" class="toggle-field"
       data-id="@item.Id"
       data-endpoint="/api/picking-status/toggle"
       data-field="HasGlass" ... />

<!-- VK Kälte (AssemblyGroup) -->
<input type="checkbox" class="toggle-field"
       data-id="@item.Id"
       data-endpoint="/api/assembly-groups/toggle-applicable"
       data-group-key="VK"
       data-field="IsApplicable" ... />
```

Inline-JS-Handler in der View dispatcht auf `data-endpoint` und baut die Body-Payload entsprechend:

```javascript
document.querySelectorAll('.toggle-field').forEach(function (cb) {
    cb.addEventListener('change', function () {
        var id = parseInt(this.getAttribute('data-id'));
        var endpoint = this.getAttribute('data-endpoint');
        var value = this.checked;
        var groupKey = this.getAttribute('data-group-key');
        var field = this.getAttribute('data-field');

        var body;
        if (endpoint === '/api/assembly-groups/toggle-applicable') {
            body = { productionOrderId: id, groupKey: groupKey, value: value };
        } else {
            body = { productionOrderId: id, field: field, value: value };
        }

        fetch(endpoint, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(body)
        }).then(function (resp) {
            if (!resp.ok) {
                cb.checked = !value;
                alert('Fehler beim Speichern.');
            }
        }).catch(function () {
            cb.checked = !value;
            alert('Fehler beim Speichern.');
        });
    });
});
```

Begründung gegen Dispatch-Dict im Backend: separate Routes geben eigene Logs, eigene Permission-Erweiterungen (Phase 4), kleinere Test-Surfaces.

## 7. Repositories

### 7.1 Neue Repositories

- `IProductionOrderPickingStatusRepository` + Impl
  - `Task<ProductionOrderPickingStatus?> GetByProductionOrderIdAsync(int id);`
  - `Task SetFieldAsync(int productionOrderId, string field, bool value, string modifiedBy, string modifiedByWindows);`
  - `Task SetReleaseAsync(int productionOrderId, bool released, int? priority, string? releasedBy);`
  - `Task SetAssignedPickerAsync(int productionOrderId, int? pickerId, string? pickerName);`
  - `Task<List<ProductionOrderPickingStatus>> GetReleasedForPickingAsync();` — Include `ProductionOrder`.
  - `Task<int> GetReleasedForPickingCountAsync();`
- `IProductionOrderBdeStatusRepository` + Impl
  - `Task<ProductionOrderBdeStatus?> GetByProductionOrderIdAsync(int id);`
  - `Task SetIsDoneBdeAsync(int productionOrderId, bool value, string modifiedBy, string modifiedByWindows);`
- `IProductionOrderAssemblyGroupRepository` + Impl
  - `Task<List<ProductionOrderAssemblyGroup>> GetByProductionOrderIdAsync(int id);`
  - `Task<ProductionOrderAssemblyGroup?> GetByPoAndKeyAsync(int productionOrderId, string groupKey);`
  - `Task SetIsApplicableAsync(int productionOrderId, string groupKey, bool value, string modifiedBy, string modifiedByWindows);`
  - `Task<Dictionary<int, Dictionary<string, bool>>> GetIsApplicablePivotAsync(IEnumerable<int> orderIds);` — für Index-View, gibt `(orderId → (groupKey → isApplicable))` zurück.

### 7.2 `IProductionOrderRepository` — Anpassungen

- `GetReleasedForPickingAsync()`, `GetReleasedForPickingByPickerAsync()`, `GetReleasedForPickingCountAsync()` werden in `IProductionOrderPickingStatusRepository` verschoben (sie filtern jetzt auf der Status-Tabelle).
- `SetCoatingFlagsAsync(Dictionary<int, bool>)` wandert nach `IProductionOrderPickingStatusRepository.SetCoatingPartsAsync(...)` (Spalte ist jetzt dort). Die Reset-auf-`IsCoatingDone=false`-Logik bleibt erhalten.
- `Search`, `GetAllOrderedAsync`, `GetByOrderNumberAsync`, `GetByArticleNumbersAsync`, `GetOpenOrdersInWindowAsync`, `GetOpenOrdersAsync` bleiben auf `ProductionOrder` (Sage-Master).

### 7.3 Pivot-Aggregation für die Index-Liste (Risiko 12.7)

Heute: `ProductionOrder` hat 5 Bool-Spalten direkt → simple Projection.
Nach Refactor: Liste joint auf 5 Zeilen pro FA. Die Pivot-Methode liefert ein In-Memory-Dictionary, das im Controller pro `item` gemappt wird:

```csharp
var orderIds = orders.Select(o => o.Id).ToList();
var groupPivot = await _assemblyGroupRepository.GetIsApplicablePivotAsync(orderIds);
// ...
item.HasCooling = groupPivot.TryGetValue(o.Id, out var groups) && groups.GetValueOrDefault("VK");
item.HasFan = groups?.GetValueOrDefault("VL") ?? false;
// ...
```

`ProductionOrderViewItem` behält die 5 Bool-Properties als View-only-Helper (keine DB-Spalten mehr). Damit bleiben Razor-Markup und filterable-table-Column-Keys stabil — nur die Datenquelle ändert sich.

**Round-4-Korrektur — Chunking gegen 2100-Parameter-Limit:** SQL Server erlaubt max. 2100 Parameter pro Query. Bei Auflistungen ohne Pagination kann `WHERE ProductionOrderId IN (...)` mit > 2000 IDs scheitern. `GetIsApplicablePivotAsync` MUSS daher die Input-IDs in **Chunks zu 1000** aufteilen und die Ergebnisse merge'n:

```csharp
public async Task<Dictionary<int, Dictionary<string, bool>>> GetIsApplicablePivotAsync(IEnumerable<int> productionOrderIds)
{
    var ids = productionOrderIds.Distinct().ToList();
    var result = new Dictionary<int, Dictionary<string, bool>>();
    const int chunkSize = 1000;
    for (int offset = 0; offset < ids.Count; offset += chunkSize)
    {
        var chunk = ids.Skip(offset).Take(chunkSize).ToList();
        var rows = await _db.Set<ProductionOrderAssemblyGroup>()
            .Where(g => chunk.Contains(g.ProductionOrderId))
            .Select(g => new { g.ProductionOrderId, g.GroupKey, g.IsApplicable })
            .ToListAsync();
        foreach (var r in rows)
        {
            if (!result.TryGetValue(r.ProductionOrderId, out var d))
            {
                d = new Dictionary<string, bool>();
                result[r.ProductionOrderId] = d;
            }
            d[r.GroupKey] = r.IsApplicable;
        }
    }
    return result;
}
```

Heutige Index-View hat Server-Pagination (max ~50 Items pro Seite) — Chunking ist defensive Versicherung gegen zukünftige Konsumer ohne Pagination.

## 8. Migration

### 8.1 Migration-Strategie: SQL-Skript als single source, EF lädt es automatisch (Round 5)

**Round-5-Korrektur:** Round 4 hatte die EF-Migration leer gelassen — Praxis-Test ergab: für Dev/Test ist das unpraktisch, weil App-Start gegen ein nicht-migriertes Schema rennt und 500-Errors wirft. Saubere Variante:

- **`SQL/60_ProductionOrderSplit.sql`** bleibt single source of truth für Schema + Daten + Drop. Idempotent, Wiederanlauf-fest.
- **EF-Migration `AddProductionOrderSplit.Up()`** lädt das Skript zur Laufzeit aus `bin/.../Migrations/Scripts/60_ProductionOrderSplit.sql`, splittet bei `GO` und ruft `migrationBuilder.Sql()` pro Batch. In `IdealAkeWms.csproj` ist die SQL-Datei als Linked-File mit `CopyToOutputDirectory=PreserveNewest` und `CopyToPublishDirectory=PreserveNewest` eingebunden.
- **Production-Cutover-Pfad bleibt als Option:** DBA kann das Skript manuell vor App-Start in SSMS ausführen. Section G inserted den `__EFMigrationsHistory`-Eintrag → EF sieht die Migration als applied und überspringt `Up()`.
- **Fallback bei fehlendem SQL-File:** Wenn das File aus dem Output-Verzeichnis fehlt (defekter Deploy), kehrt `Up()` einfach `return` zurück — kein Crash, App startet, aber Schema bleibt unmigriert. Im Log nicht direkt sichtbar; manuelle Verifikation per `SELECT * FROM __EFMigrationsHistory` empfohlen.

**Beim App-Start:** EF prüft die `__EFMigrationsHistory`-Tabelle.
- Wenn Migration nicht angewendet: `Up()` lädt SQL-Datei und führt Batches aus.
- Wenn Migration schon vorab manuell ausgeführt (Section G hat History-Eintrag inserted): `Up()` wird übersprungen.

**Trade-Off:** Bei Produktiv-DBs mit großen FA-Mengen (>50k) sollte DBA das Skript manuell während des Wartungsfensters fahren — dann hängt App-Start nicht an minutenlanger Daten-Kopie. Bei Dev/Test (wenige Hundert FAs) läuft Auto-Migration in Sekunden durch.

**Reihenfolge im SQL-Skript** (siehe 8.2): Section A Schema → Section B/C/D Daten → Section E Verifikation → Section F Drop → Section G History-Eintrag.

### 8.2 SQL-Skript `60_ProductionOrderSplit.sql`

Nächste freie Nummer: **60** (zuletzt vergeben: `59_AddProductionOrderAssemblyFlags.sql`).

Struktur (alle Sections idempotent mit `IF NOT EXISTS`-Guards):

```sql
-- Section A: Tabellen anlegen (idempotent)
IF OBJECT_ID(N'dbo.ProductionOrderPickingStatus', N'U') IS NULL BEGIN ... END
GO
IF OBJECT_ID(N'dbo.ProductionOrderBdeStatus', N'U') IS NULL BEGIN ... END
GO
IF OBJECT_ID(N'dbo.ProductionOrderAssemblyGroups', N'U') IS NULL BEGIN ... END
GO
IF OBJECT_ID(N'dbo.ProductionOrderAssemblyGroupSpecs', N'U') IS NULL BEGIN ... END
GO
IF OBJECT_ID(N'dbo.ProductionWorkplaceAssemblyGroups', N'U') IS NULL BEGIN ... END
GO

-- Section B: Batched Daten-Kopie nach PickingStatus
DECLARE @batchSize INT = 5000;
DECLARE @lastId INT = 0;
DECLARE @rows INT = 1;
WHILE @rows > 0
BEGIN
    BEGIN TRANSACTION;
    INSERT INTO dbo.ProductionOrderPickingStatus (
        ProductionOrderId, PickingStatus, PickingPriority,
        IsReleasedForPicking, ReleasedAt, ReleasedBy,
        AssignedPickerId, AssignedPickerName,
        HasGlass, HasExternalPurchase, HasCoatingParts, IsCoatingDone,
        IsDonePicking,
        CreatedAt, CreatedBy, CreatedByWindows)
    SELECT TOP (@batchSize)
        p.Id, p.PickingStatus, p.PickingPriority,
        p.IsReleasedForPicking, p.ReleasedAt, p.ReleasedBy,
        p.AssignedPickerId, p.AssignedPickerName,
        p.HasGlass, p.HasExternalPurchase, p.HasCoatingParts, p.IsCoatingDone,
        0,  -- IsDonePicking: bei Migration noch nicht gesetzt
        GETDATE(), 'Migration_60', SYSTEM_USER
    FROM dbo.ProductionOrders p
    WHERE p.Id > @lastId
      AND NOT EXISTS (SELECT 1 FROM dbo.ProductionOrderPickingStatus s WHERE s.ProductionOrderId = p.Id)
    ORDER BY p.Id;
    SET @rows = @@ROWCOUNT;
    IF @rows > 0
        SET @lastId = (SELECT MAX(ProductionOrderId) FROM dbo.ProductionOrderPickingStatus);
    COMMIT TRANSACTION;
END
PRINT 'PickingStatus-Migration: Zeilen vorhanden = ' + CAST((SELECT COUNT(*) FROM dbo.ProductionOrderPickingStatus) AS NVARCHAR);
GO

-- Section C: Batched BdeStatus (analog, ohne Spalten-Kopie)
-- Section D: Batched AssemblyGroups (5 INSERTs/FA via UNION ALL)
-- Section E: Verifikation (Erwartung dokumentieren, kein Throw)
-- Section F: Spalten droppen (DEFAULT-Constraints zuerst!)
```

**Section F — Drop-Reihenfolge:**
1. Drop `DF_*`-Default-Constraints für jede Bit-Spalte (`DF_ProductionOrders_HasCooling` etc.).
2. Drop FK `FK_ProductionOrders_AssignedPicker`.
3. Drop Index `IX_ProductionOrders_IsReleasedForPicking_IsDone`, `IX_ProductionOrders_AssignedPickerId`.
4. Drop Columns: `PickingStatus, PickingPriority, IsReleasedForPicking, ReleasedAt, ReleasedBy, AssignedPickerId, AssignedPickerName, HasGlass, HasExternalPurchase, HasCoatingParts, IsCoatingDone, HasCooling, HasFan, HasElectric, HasDoors, HasSuperstructure`.

Jeder Drop mit `IF EXISTS (SELECT 1 FROM sys.columns ...)`-Guard (Wiederanlauf-fest).

### 8.3 `__EFMigrationsHistory` als Section G im SQL-Skript

Letzte Section des Skripts (nach Drop-Sektion F). Macht das Standalone-SQL self-contained — EF Migrate-Pipeline muss diese Migration nicht ausführen.

```sql
IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '<TIMESTAMP>_AddProductionOrderSplit')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('<TIMESTAMP>_AddProductionOrderSplit', '10.0.0');
END
GO
```

**Beim App-Start:** EF prüft die History-Tabelle, findet den Eintrag, ruft `Up()` **nicht** auf (Body ist ohnehin leer). Migration gilt als applied.

### 8.4 `00_FreshInstall.sql` Update

- `ProductionOrders`-Tabelle (Section 8, Zeile 214) auf Slim-Version reduzieren (alle 16 Spalten entfernen).
- Neue Sections nach `ProductionOrders` einfügen: `9a. ProductionOrderPickingStatus`, `9b. ProductionOrderBdeStatus`, `9c. ProductionOrderAssemblyGroups`, `9d. ProductionOrderAssemblyGroupSpecs`, `9e. ProductionWorkplaceAssemblyGroups`. Nummerierung der nachfolgenden Sections (`9. PickingItems` etc.) bleibt — wir nutzen `9a..9e` als Sub-Sections.

## 9. Sage-AgentJob — Folge-MERGEs (Q3)

`SQL/AgentJobs/01_Import_Produktionsauftraege.sql` bekommt nach dem bestehenden MERGE (Zeile 30-73) **drei zusätzliche MERGEs**, die für neu importierte FAs die eager-create-Zeilen anlegen:

### Section "Picking-Status eager-create"

```sql
MERGE [IDEAL_AKE_WMS].[dbo].[ProductionOrderPickingStatus] AS s
USING (SELECT Id FROM [IDEAL_AKE_WMS].[dbo].[ProductionOrders]) AS src ON s.ProductionOrderId = src.Id
WHEN NOT MATCHED BY TARGET THEN
    INSERT (ProductionOrderId, IsReleasedForPicking, HasGlass, HasExternalPurchase,
            HasCoatingParts, IsCoatingDone, IsDonePicking,
            CreatedAt, CreatedBy, CreatedByWindows)
    VALUES (src.Id, 0, 0, 0, 0, 0, 0, GETDATE(), 'Sage_Schnittstelle', SYSTEM_USER);
```

### Section "BDE-Status eager-create"

```sql
MERGE [IDEAL_AKE_WMS].[dbo].[ProductionOrderBdeStatus] AS s
USING (SELECT Id FROM [IDEAL_AKE_WMS].[dbo].[ProductionOrders]) AS src ON s.ProductionOrderId = src.Id
WHEN NOT MATCHED BY TARGET THEN
    INSERT (ProductionOrderId, IsDoneBde, CreatedAt, CreatedBy, CreatedByWindows)
    VALUES (src.Id, 0, GETDATE(), 'Sage_Schnittstelle', SYSTEM_USER);
```

### Section "AssemblyGroups eager-create (5/FA)"

```sql
MERGE [IDEAL_AKE_WMS].[dbo].[ProductionOrderAssemblyGroups] AS s
USING (
    SELECT p.Id AS ProductionOrderId, k.GroupKey
    FROM [IDEAL_AKE_WMS].[dbo].[ProductionOrders] p
    CROSS JOIN (VALUES ('VK'),('VL'),('VE'),('VT'),('VA')) k(GroupKey)
) AS src ON s.ProductionOrderId = src.ProductionOrderId AND s.GroupKey = src.GroupKey
WHEN NOT MATCHED BY TARGET THEN
    INSERT (ProductionOrderId, GroupKey, IsApplicable, IsCompleted,
            CreatedAt, CreatedBy, CreatedByWindows)
    VALUES (src.ProductionOrderId, src.GroupKey, 0, 0, GETDATE(), 'Sage_Schnittstelle', SYSTEM_USER);
```

Alle drei MERGEs sind idempotent — `NOT MATCHED BY TARGET` triggert nur für neue FAs / fehlende Zeilen. Der bestehende `WHEN MATCHED`-Pfad bleibt unberührt; die Status-Tabellen werden vom AgentJob nicht **aktualisiert**, nur erst-angelegt.

## 10. View + ViewModel

### 10.1 `ProductionOrderViewItem` — bleibt strukturell stabil

Die 5 Baugruppen-Bool-Properties + Picking-Felder bleiben als **View-only-Helper** auf `ProductionOrderViewItem`. Datenquelle ändert sich nur im Controller-Mapping (Pivot statt Direkt-Zugriff). Damit bleibt das Razor-Markup minimal-impacted.

### 10.2 `ProductionOrdersController.Index` — Mapping-Block

Im Bereich `IdealAkeWms/Controllers/ProductionOrdersController.cs:281-333` ändert sich das `.Select(o => ...)`-Mapping:

```csharp
// Vor dem Select: Pivots laden
var orderIds = orders.Select(o => o.Id).ToList();
var groupPivot = await _assemblyGroupRepository.GetIsApplicablePivotAsync(orderIds);
var pickingStatuses = (await _pickingStatusRepository.GetByProductionOrderIdsAsync(orderIds))
    .ToDictionary(s => s.ProductionOrderId);

var viewItems = orders.Select(o =>
{
    var ps = pickingStatuses.GetValueOrDefault(o.Id);
    var grp = groupPivot.GetValueOrDefault(o.Id) ?? new Dictionary<string, bool>();

    var item = new ProductionOrderViewItem
    {
        Id = o.Id,
        OrderNumber = o.OrderNumber,
        // ... (Sage-Master-Felder unverändert)
        IsDone = o.IsDone,
        PickingStatus = ps?.PickingStatus,
        HasGlass = ps?.HasGlass ?? false,
        HasExternalPurchase = ps?.HasExternalPurchase ?? false,
        HasCoatingParts = ps?.HasCoatingParts ?? false,
        IsCoatingDone = ps?.IsCoatingDone ?? false,
        IsReleasedForPicking = ps?.IsReleasedForPicking ?? false,
        PickingPriority = ps?.PickingPriority,
        ReleasedAt = ps?.ReleasedAt,
        ReleasedBy = ps?.ReleasedBy,
        AssignedPickerId = ps?.AssignedPickerId,
        AssignedPickerName = ps?.AssignedPickerName,
        HasCooling = grp.GetValueOrDefault("VK"),
        HasFan = grp.GetValueOrDefault("VL"),
        HasElectric = grp.GetValueOrDefault("VE"),
        HasDoors = grp.GetValueOrDefault("VT"),
        HasSuperstructure = grp.GetValueOrDefault("VA"),
        WorkplaceName = o.ProductionWorkplace?.Name
    };
    // BeschichtungTermin-Logik unverändert
    return item;
}).ToList();
```

### 10.3 View — Checkbox-Markup-Update

In `Views/ProductionOrders/Index.cshtml:222-256` jede Checkbox um `data-endpoint` (+ `data-group-key` bei Baugruppen) erweitern. Konkrete Diffs im Plan.

In `Views/ProductionOrders/Index.cshtml:548-567` den Inline-JS-Handler durch den dispatching Handler ersetzen (siehe 6.3).

### 10.4 `Views/Picking/*.cshtml` und `PickingController`

`PickingController.Index` mappt Released-FAs in `PickingListItem` (Zeile 91-114). Die Felder `PickingPriority`, `PickingStatus`, `AssignedPickerId`, `AssignedPickerName` werden jetzt von `PickingStatusRepository.GetReleasedForPickingAsync()` geliefert (Repo-Methode gibt `(ProductionOrder, PickingStatus)`-Paar zurück oder Include-projiziert). `PickingController.SetPickingStatus` (Zeile 357-373) schreibt in `ProductionOrderPickingStatus` statt direkt auf den FA.

**Round-4 explicit: IsDone-Setter umstellen.** Heute setzt der `PickingController` an zwei Stellen direkt das FA-Master-IsDone-Flag, was nach Refactor falsch ist (FA.IsDone = Sage-Master). Beide Stellen müssen auf `PickingStatusRepository.SetIsDonePickingAsync` umgestellt werden:

- [`PickingController.cs:134`](IdealAkeWms/Controllers/PickingController.cs#L134) `order.IsDone = !order.IsDone;` → `await _pickingStatusRepo.ToggleIsDonePickingAsync(order.Id);`
- [`PickingController.cs:367`](IdealAkeWms/Controllers/PickingController.cs#L367) `order.IsDone = true;` → `await _pickingStatusRepo.SetIsDonePickingAsync(order.Id, true);`

Toggle-API-Mapping (Spec 6.2) deckt `IsDonePicking` als Field-Name im `/api/picking-status/toggle`-Endpoint ab.

`Views/Picking/Bom.cshtml` und `Views/Picking/Index.cshtml` lesen die Felder weiter via `PickingListItem` — kein View-Diff.

### 10.4a `CoatingDetectionService` (Service-Projekt)

[`IDEALAKEWMSService/Services/CoatingDetectionService.cs:147-158`](IDEALAKEWMSService/Services/CoatingDetectionService.cs#L147-L158) setzt heute `ProductionOrder.HasCoatingParts` und ggf. `IsCoatingDone` direkt auf der FA-Tabelle (über `ProductionOrderRepository.SetCoatingFlagsAsync`). Nach Refactor:

```csharp
// Alt: await _orderRepo.SetCoatingFlagsAsync(orderIdToHasCoatingParts);
await _pickingStatusRepo.SetCoatingPartsAsync(orderIdToHasCoatingParts);
```

`SetCoatingPartsAsync` muss in `IProductionOrderPickingStatusRepository` definiert werden mit derselben Semantik wie heute: wenn `HasCoatingParts` von `true` auf `false` wechselt, wird `IsCoatingDone` ebenfalls auf `false` zurückgesetzt (Fallstrick CLAUDE.md #11).

DI in Service: `CoatingDetectionService`-Konstruktor erweitern um `IProductionOrderPickingStatusRepository`; Service-`Program.cs` (im Service-Projekt) Repository registrieren.

### 10.5 `ProductionOrdersController.ToggleRelease` (Leitstand)

Liest und schreibt `IsReleasedForPicking`, `PickingPriority`, `ReleasedAt`, `ReleasedBy`, `AssignedPickerId`, `AssignedPickerName` auf `ProductionOrderPickingStatus`. Action-Signatur bleibt unverändert. Implementierung läuft jetzt über `IProductionOrderPickingStatusRepository`.

`BulkRelease`, `SetPriority` analog.

## 11. Tests

### 11.1 Neue Repository-Tests

- `ProductionOrderPickingStatusRepositoryTests`
  - `SetFieldAsync_HasGlass_PersistsValue_AndAuditFields`
  - `SetFieldAsync_UnknownField_ThrowsArgumentException`
  - `SetCoatingPartsAsync_FlipsToFalse_ResetsIsCoatingDone` (Fallstrick #11)
  - `GetReleasedForPickingAsync_OrdersByPriorityThenDate`
- `ProductionOrderBdeStatusRepositoryTests`
  - `SetIsDoneBdeAsync_PersistsValue_AndAuditFields`
- `ProductionOrderAssemblyGroupRepositoryTests`
  - `GetByPoAndKeyAsync_ReturnsRow`
  - `SetIsApplicableAsync_UnknownGroupKey_ThrowsArgumentException`
  - `GetIsApplicablePivotAsync_ReturnsDictPerOrderPerKey`

### 11.2 Neue API-Tests

- `PickingStatusApiControllerTests`
  - `Toggle_HappyPath_ReturnsOk`
  - `Toggle_UnknownField_Returns400`
  - `Toggle_UnknownOrder_Returns404`
- `AssemblyGroupsApiControllerTests`
  - `ToggleApplicable_HappyPath_VK_ReturnsOk`
  - `ToggleApplicable_UnknownGroupKey_Returns400`
  - `ToggleApplicable_UnknownOrder_Returns404`
- `BdeStatusApiControllerTests`
  - `Toggle_HappyPath_ReturnsOk`
  - `Toggle_UnknownField_Returns400`

### 11.3 AgentJob-Integration-Test

`ProductionOrderEagerCreateAgentJobTests` — verwendet `TestApplicationDbContext`:
1. Lege einen `ProductionOrder` ohne dazugehörige Status-Zeilen an.
2. Führe das Eager-Create-MERGE-SQL aus (via `_context.Database.ExecuteSqlRaw(...)` mit dem Skript-Block aus 9).
3. Assert: 1× PickingStatus, 1× BdeStatus, 5× AssemblyGroups vorhanden (alle mit `IsApplicable=0`).
4. Re-Run: kein Duplikat (Idempotenz).

**Open:** InMemory-DB unterstützt kein MERGE. Empfehlung-Default: dieser eine Test markiert mit `[Trait("Category", "SqlServerOnly")]` und in CI ausgeschlossen, lokal gegen Stage-DB ausgeführt. Alternative: das MERGE durch eine äquivalente LINQ-basierte `EnsureEagerCreatedAsync()`-Methode im Repository ersetzen, die sowohl im AgentJob (SQL) als auch im Test (EF) das Ergebnis liefert. **Recommended default:** SQL-only-Test mit Stage-DB-Connection-String, weil der AgentJob ohnehin als rohes Skript läuft und wir genau das verifizieren wollen.

### 11.4 Vorhandene Tests — Anpassungen

- `ProductionOrdersControllerPickerTests` — passt heutige Tests auf neue Repositories an (Mocking `IProductionOrderPickingStatusRepository` statt Direkt-Property-Setzen auf `ProductionOrder`).
- `PickingApiControllerTests` — `SetPickingStatus`-Path geht jetzt über `PickingStatusRepository`.
- `LocationTransferTests` und andere, die `o.IsReleasedForPicking` direkt lesen, ggf. mocken.

Erwartung: ~25 vorhandene Tests müssen ihre Setup-Daten umstellen (FA + zugehörige Status-Zeile statt nur FA).

## 12. Risiken

### 12.1 Datenmigration verliert FA-Status
**Mitigation:** DB-Backup vor Start (Roadmap 5.6). Batched-INSERT mit `NOT EXISTS`-Idempotenz-Guards. Verifikations-Counts (SUM/COUNT pro alter Spalte == COUNT(`IsApplicable=1`) in neuer Tabelle) als `PRINT` im Skript. Bei Mismatch: Skript abbrechen, Backup restore.

### 12.2 Wartungsfenster-Dauer (Roadmap 5.2)
**Mitigation:** Batches à 5000 + dokumentierte Pre-Migration-Schritte. Realistisches Fenster < 50k FAs: 45 Minuten.

### 12.3 Toggle-API-Refactor bricht User-Workflow
**Mitigation:** View-JS-Diff im selben Deploy. Plan-Task 8 verifiziert manuell jede der 8 Checkbox-Spalten in der FA-Liste. Smoke-Test nach App-Start ist Pflicht (Roadmap 5.6.9).

### 12.4 JOIN-Performance auf FA-Liste (Roadmap 12.7)
**Mitigation:** Pivot-Query via `IX_ProductionOrderAssemblyGroups_GroupKey_IsApplicable` und `UQ_*`-Index. Test-Setup vor Produktiv-Deploy: Index-Liste mit echten Datenmengen (>500 FAs) im Stage-System laden, Antwortzeit < 1 s erwartet.

### 12.5 EF-Migration-Snapshot-Konflikt nach Rebase
**Mitigation:** Roadmap 12.8 dokumentiert `dotnet ef migrations remove --force` + `add` neu. Im Plan-Task 1 wird der Rebase-Pfad als Pre-Check verlangt.

### 12.6 AgentJob-Idempotenz-Bug
**Mitigation:** MERGE mit `NOT MATCHED BY TARGET`-Pfad ohne `WHEN MATCHED` (Status-Zeilen werden nie überschrieben). Eager-Create-Test in 11.3 verifiziert Idempotenz explizit (zwei Aufrufe → keine Duplikate, keine Mutation).

### 12.7 `ProductionOrders.PickingStatus`-Property-Rename
**Risiko:** Compiler-Fehler in ungemoctken Code-Stellen, die `o.PickingStatus` als `string?` lesen. **Mitigation:** **Beabsichtigt** — durch Property-Rename zwingt der Build jeden Aufrufer, sich umzustellen. Grep über die Codebase zeigt heute 12 Stellen (Repo, Controller, View-Mapping), alle in dieser Phase betroffen.

### 12.8 Rollback-Fenster ist eng (Roadmap 5.5)
**Mitigation:** Plan-Task "Smoke-Test" verifiziert 1× FA-Freigabe, 1× Toggle pro Tabelle, 1× Picking, 1× BDE-Buchung **vor** AgentJob-Reaktivierung. Backup-Restore-Window 30 Minuten dokumentiert. Forward-Fix-Strategie für danach.

### 12.9 InMemory-DB Test-Coverage für MERGE-AgentJob
**Mitigation:** Siehe 11.3, `SqlServerOnly`-Trait + Stage-DB-Lauf. **Open:** Falls Stage-DB nicht verfügbar, alternative LINQ-Repo-Methode mit gleicher Semantik.

### 12.10 `IsApplicable`-Default `false` widerspricht heutigem Verhalten
Heute: `HasCooling` etc. werden vom User per Checkbox auf `true` gesetzt — gespeicherte FAs haben die alten Werte. Migration kopiert sie korrekt. **Neue FAs nach AgentJob:** alle 5 Gruppen `IsApplicable=false`, User toggelt nach Bedarf. Verhalten identisch zu heute (Defaults sind `0` in DB).

### 12.11 Old endpoint `/api/productionorders/toggle-field` hard-removed
Der bisherige Endpoint wird in Phase 1 entfernt (`ProductionOrdersApiController.ToggleField` gelöscht). User mit Stale-Browser-Cache (vor Deploy geöffnete Tabs) treffen 404 → Checkbox-Klick ohne sichtbare Wirkung. **Mitigation:** `asp-append-version` versionsbusted `site.js` und View-Inline-JS — neue Tab-Loads holen die frische JS-Datei mit `data-endpoint`-Routing. Stale-Tab-Fall ist akzeptiert; User wird bei nächstem Page-Reload automatisch korrigiert. Optional verbessert: Old-Endpoint kurzzeitig (3-7 Tage) als 410 Gone mit Refresh-Hint-Header anbieten — **nicht in Phase 1 umgesetzt**, weil Hard-Cutover-Risiko niedrig.

### 12.12 AgentJob-vs-Service-Sync-Ordering
`CoatingDetectionService` (Service-Worker) schreibt nach Refactor in `ProductionOrderPickingStatus.HasCoatingParts`. Wenn der Service vor dem AgentJob läuft, treffen die Updates auf nicht-existente Status-Zeilen. **Mitigation:** Cutover-Schritt 10 erzwingt AgentJob-Smoke-Run **vor** Wartungsfenster-Ende. Service-Worker startet erst nach App-Start (= nach AgentJob-Smoke). `SyncIntervalMinutes=15` gibt zusätzlichen Puffer.

## 13. Manuelle Test-Szenarien (für TESTSZENARIEN.md)

Im Plan-Task "Doku" werden 4 neue Szenarien dem TS-3-Block (FA-Liste) angehängt:

1. **TS-3.x — Toggle-Routing nach Refactor**: Verifiziert dass jede der 8 Checkbox-Spalten den richtigen Endpoint trifft (HasGlass → picking-status, VK → assembly-groups, etc.). Browser-DevTools-Network-Tab kontrollieren.
2. **TS-3.x — Migrations-Verifikation Post-Cutover**: SQL-Query `SELECT COUNT(*) FROM ProductionOrderPickingStatus = SELECT COUNT(*) FROM ProductionOrders` und `SELECT COUNT(*) FROM ProductionOrderAssemblyGroups = 5 * COUNT(ProductionOrders)`.
3. **TS-3.x — AgentJob-Eager-Create**: Neuen FA in Sage anlegen, AgentJob laufen lassen, alle 7 Status-Zeilen müssen existieren.
4. **TS-3.x — Leitstand-Freigabe nach Refactor**: Freigabe-Toggle, Picker-Assignment, Priorität setzen — alle Werte landen in `ProductionOrderPickingStatus`.

## 14. Deploy-Reihenfolge (aus Roadmap 5.6, Round-4-korrigiert)

1. Wartungsfenster-Kommunikation (24-48h Vorlauf).
2. App-Stop.
3. **DB-Backup** (vollständig).
4. SQL Agent Job deaktivieren (`01_Import_Produktionsauftraege` und ggf. `02_Import_Artikel`, weil sie auf `ProductionOrders` referenzieren).
5. **`SQL/60_ProductionOrderSplit.sql` manuell ausführen** (Schema + Daten + Drop + `__EFMigrationsHistory`-Eintrag in einer Datei). Daten-Kopie + Verifikations-PRINTs laufen hier.
6. Verifikations-Counts prüfen — Erwartung: `ProductionOrderPickingStatus.COUNT == ProductionOrders.COUNT` und `ProductionOrderAssemblyGroups.COUNT == 5 × ProductionOrders.COUNT`. Bei Mismatch: STOP, Backup-Restore.
7. Neue App-Version + neuer AgentJob `01_Import_Produktionsauftraege` deployen (AgentJob bleibt noch deaktiviert).
8. App-Start. `db.Database.Migrate()` findet den History-Eintrag, ruft die leere `Up()` der EF-Migration nicht aus — kein App-Start-Hänger.
9. **App-Smoke-Test (vor AgentJob-Reaktivierung):**
   - Index-Liste lädt < 2 s.
   - 1× FA freigeben → Eintrag in `ProductionOrderPickingStatus.IsReleasedForPicking=1`.
   - 1× Toggle pro Endpoint (HasGlass, IsCoatingDone, IsDonePicking, VK, VL, VE, VT, VA, IsDoneBde).
   - 1× Picking-Status setzen (`SetPickingStatus`).
   - 1× Stückliste öffnen.
10. **AgentJob-Smoke-Test (Round-4-Pflicht, vor Wartungsfenster-Ende):**
    - In Sage einen Test-FA anlegen (oder bereits vorhandenen Sage-WA für Test markieren).
    - AgentJob `01_Import_Produktionsauftraege` **manuell einmal ausführen**.
    - SQL-Check: für den neuen FA müssen je 1 `PickingStatus` + 1 `BdeStatus` + 5 `AssemblyGroups` existieren.
    - Bei Fehler: Job kann angepasst werden, App bleibt online, kein User-Impact.
11. AgentJob im Scheduler reaktivieren.
12. Wartungsfenster beenden.

Backup-Restore-Window endet 30 Minuten nach App-Start (Schritt 8). Danach Forward-Fix only. **AgentJob-Smoke (Schritt 10) ist innerhalb dieser Window-Phase Pflicht** — sonst wird ein AgentJob-Bug erst nach Stunden bei produktivem Sage-Run sichtbar.

**Pre-Cutover-Ordering-Note:** Nach Wartungsfenster läuft der `CoatingDetectionService` (Service-Worker) seinen ersten Sync. Erwartung: AgentJob hat für alle FAs die `PickingStatus`-Zeile angelegt (Schritt 10), bevor `CoatingDetectionService` `SetCoatingPartsAsync` aufruft. Andernfalls würde der Coating-Service auf nicht-existierende Status-Zeilen schreiben. Mitigation: Service-Worker-`SyncIntervalMinutes` ist 15 → AgentJob ist seit ≥1 Run aktiv, bevor `CoatingDetectionService` triggert.

## 15. Versionierung + Doku

- `IdealAkeWms/AppVersion.cs`: `1.10.0` → `1.11.0`, Date `2026-05-12`.
- `IdealAkeWmsService/AppVersion.cs`: gleich.
- `Views/Help/Changelog.cshtml`: neues Card `v1.11.0` mit Schwerpunkten "ProductionOrder Schema-Refactor (Phase 1)", "Toggle-API in 3 Endpoints aufgeteilt", "Neue Tabellen für Picking-/BDE-/AssemblyGroup-Status".
- `Views/Help/Index.cshtml`: Hinweis auf neue Daten-Architektur, Links zu Schema-Spec.
- `CLAUDE.md`: neuer Fallstrick `ProductionOrder Status-Aufteilung (seit v1.11.0)` mit Beispiel.
- `docs/TESTSZENARIEN.md`: 4 neue Szenarien im TS-3-Block.

## 16. Code-Punkte-Referenz

- [ProductionOrder.cs:1-105](IdealAkeWms/Models/ProductionOrder.cs#L1-L105) — Entity-Definition heute.
- [ProductionOrdersController.cs:281-333](IdealAkeWms/Controllers/ProductionOrdersController.cs#L281-L333) — Inline-Mapping-Block.
- [ProductionOrdersController.cs:39-110](IdealAkeWms/Controllers/ProductionOrdersController.cs#L39-L110) — ToggleRelease/BulkRelease/SetPriority.
- [ProductionOrdersApiController.cs:40-69](IdealAkeWms/Controllers/ProductionOrdersApiController.cs#L40-L69) — heutiger Toggle-Field-Handler.
- [PickingController.cs:80-114](IdealAkeWms/Controllers/PickingController.cs#L80-L114) — Picking-Index-Mapping.
- [PickingController.cs:354-373](IdealAkeWms/Controllers/PickingController.cs#L354-L373) — SetPickingStatus.
- [ProductionOrderRepository.cs:48-71](IdealAkeWms/Data/Repositories/ProductionOrderRepository.cs#L48-L71) — Released-Picking-Queries.
- [ProductionOrderRepository.cs:89-120](IdealAkeWms/Data/Repositories/ProductionOrderRepository.cs#L89-L120) — SetCoatingFlagsAsync.
- [Index.cshtml:222-256](IdealAkeWms/Views/ProductionOrders/Index.cshtml#L222-L256) — Checkbox-Markup.
- [Index.cshtml:548-567](IdealAkeWms/Views/ProductionOrders/Index.cshtml#L548-L567) — JS-Handler.
- [ApplicationDbContext.cs:338-378](IdealAkeWms/Data/ApplicationDbContext.cs#L338-L378) — Entity-Config für ProductionOrder.
- [01_Import_Produktionsauftraege.sql](SQL/AgentJobs/01_Import_Produktionsauftraege.sql) — Sage-AgentJob.
- [00_FreshInstall.sql:214-261](SQL/00_FreshInstall.sql#L214-L261) — FreshInstall-Section ProductionOrders.

## 17. Offene Entscheidungen

- **AgentJob-Test-Strategie** (11.3) — Stage-DB vs. EF-Repo-Methode mit gleicher Semantik. **Recommended default:** Stage-DB-only-Test mit `SqlServerOnly`-Trait.

Keine weiteren TBDs. Alle Schema- und API-Entscheidungen sind aus der Roadmap Q1-Q11 abgeleitet.

---

**Hinweis:** Phase-2 Detail-Spec (View-Split) wird nach erfolgreichem Phase-1-Cutover + 5 Tagen Live-Verifikation geschrieben.
