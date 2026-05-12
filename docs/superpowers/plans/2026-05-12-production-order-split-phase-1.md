# ProductionOrder-Split — Phase 1 Big-Bang Schema-Refactor — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `ProductionOrders` (heute 28 Spalten, 3 Concerns) atomar in 5 neue Tabellen splitten: `ProductionOrderPickingStatus` (1:1), `ProductionOrderBdeStatus` (1:1), `ProductionOrderAssemblyGroups` (1:N, 5 Zeilen/FA), `ProductionOrderAssemblyGroupSpecs` (1:N, Phase 1 leer), `ProductionWorkplaceAssemblyGroups` (Junction, Phase 1 leer). 16 alte Spalten gedroppt. Toggle-API in 3 separate Endpoints aufgeteilt. Sage-AgentJob bekommt eager-create-MERGEs. App-/Repository-/View-/JS-Layer atomar umgestellt.

**Spec:** `docs/superpowers/specs/2026-05-12-production-order-split-phase-1-design.md` (committed in `50ef89e`).

**Roadmap:** `docs/superpowers/specs/2026-05-12-production-order-split-roadmap.md`.

**Branch:** `refactor/production-order-split` (eigener WorkTree, KEIN Phase-2-Bundle).

**AppVersion:** `1.10.0` → `1.11.0`, Datum `2026-05-12`.

**SQL-Skript-Nummer:** `60` (frei verifiziert via `Glob SQL/*.sql` — zuletzt belegt: `59_AddProductionOrderAssemblyFlags.sql`).

**Commit-Konvention:** `refactor(productionorders): ...` / `feat(productionorders): ...` / `docs: ...`. Co-Authored-By trailer im HEREDOC.

**Architecture:** 6 Schichten:
- **Entity** — 5 neue Entities + ApplicationDbContext-Config, 16 Properties auf `ProductionOrder` entfernt.
- **EF-Migration + SQL** — generierte Migration hand-editiert (Datenkopie VOR DropColumn). Idempotentes `60_ProductionOrderSplit.sql` mit batched-INSERT-Loop (5000/Batch).
- **Sage-AgentJob** — 3 Folge-MERGEs (eager-create für neue FAs).
- **Repositories** — 3 neue Repos (`IProductionOrderPickingStatusRepository`, `IProductionOrderBdeStatusRepository`, `IProductionOrderAssemblyGroupRepository`); `IProductionOrderRepository` von App-verwalteten Spalten befreit.
- **Controller + ViewModel + Inline-Mapping** — `ProductionOrdersController.Index` / `PickingController.Index|SetPickingStatus` lesen über die neuen Repos. Neue API-Controller `PickingStatusApiController` / `AssemblyGroupsApiController` / `BdeStatusApiController`. Alter `ProductionOrdersApiController.ToggleField` entfernt.
- **View + JS** — Checkbox-Markup um `data-endpoint` + `data-group-key` erweitert. Inline-Handler in `Views/ProductionOrders/Index.cshtml` dispatcht.

**Critical sequencing constraints:**
1. **Tasks 1-2 MÜSSEN in einer Migration kompilieren** — sobald `ProductionOrder.cs` 16 Properties verliert, brechen Repos/Controller/Views. Tasks 3-7 sind die Code-Reparatur. Im Worktree zwischen Task 1 (Entity-Drop) und Task 7 (View-Update) bewusst rote Builds dulden — Task 8 (Tests) ist der erste grüne Build.
2. **`Status`-Property-Rename ist gewollt:** `ProductionOrder.PickingStatus` (heute `string?`) wird Navigation-Property `ProductionOrderPickingStatus?`. Der Compiler zwingt jeden Konsumenten zur Umstellung (Spec 12.7).
3. **EF-Migration HAND-EDITIEREN:** Generierter Migration-Up bekommt nach den `CreateTable`s und VOR den `DropColumn`s den Daten-Copy-Block via `migrationBuilder.Sql(@"...")`. Sonst geht Datenmigration in der Lücke verloren.

**Files (Gesamtübersicht):**

**New:**
- `IdealAkeWms/Models/ProductionOrderPickingStatus.cs`
- `IdealAkeWms/Models/ProductionOrderBdeStatus.cs`
- `IdealAkeWms/Models/ProductionOrderAssemblyGroup.cs`
- `IdealAkeWms/Models/ProductionOrderAssemblyGroupSpec.cs`
- `IdealAkeWms/Models/ProductionWorkplaceAssemblyGroup.cs`
- `IdealAkeWms/Data/Repositories/IProductionOrderPickingStatusRepository.cs`
- `IdealAkeWms/Data/Repositories/ProductionOrderPickingStatusRepository.cs`
- `IdealAkeWms/Data/Repositories/IProductionOrderBdeStatusRepository.cs`
- `IdealAkeWms/Data/Repositories/ProductionOrderBdeStatusRepository.cs`
- `IdealAkeWms/Data/Repositories/IProductionOrderAssemblyGroupRepository.cs`
- `IdealAkeWms/Data/Repositories/ProductionOrderAssemblyGroupRepository.cs`
- `IdealAkeWms/Controllers/PickingStatusApiController.cs`
- `IdealAkeWms/Controllers/AssemblyGroupsApiController.cs`
- `IdealAkeWms/Controllers/BdeStatusApiController.cs`
- `IdealAkeWms/Migrations/<TIMESTAMP>_AddProductionOrderSplit.cs` (EF-generiert, hand-editiert)
- `IdealAkeWms/Migrations/<TIMESTAMP>_AddProductionOrderSplit.Designer.cs` (EF-generiert)
- `IdealAkeWms/Migrations/ApplicationDbContextModelSnapshot.cs` (EF-aktualisiert)
- `SQL/60_ProductionOrderSplit.sql`
- `IdealAkeWms.Tests/Repositories/ProductionOrderPickingStatusRepositoryTests.cs`
- `IdealAkeWms.Tests/Repositories/ProductionOrderBdeStatusRepositoryTests.cs`
- `IdealAkeWms.Tests/Repositories/ProductionOrderAssemblyGroupRepositoryTests.cs`
- `IdealAkeWms.Tests/Controllers/PickingStatusApiControllerTests.cs`
- `IdealAkeWms.Tests/Controllers/AssemblyGroupsApiControllerTests.cs`
- `IdealAkeWms.Tests/Controllers/BdeStatusApiControllerTests.cs`
- `IdealAkeWms.Tests/Integration/ProductionOrderEagerCreateAgentJobTests.cs` (SqlServerOnly-Trait, siehe Task 8)

**Modify:**
- `IdealAkeWms/Models/ProductionOrder.cs` (16 Properties entfernt, 4 Nav-Properties addiert)
- `IdealAkeWms/Data/ApplicationDbContext.cs` (5 DbSets + Relationships)
- `IdealAkeWms/Data/Repositories/IProductionOrderRepository.cs` (Released-Methoden raus, SetCoatingFlags raus)
- `IdealAkeWms/Data/Repositories/ProductionOrderRepository.cs` (Released-Methoden raus, SetCoatingFlags raus)
- `IdealAkeWms/Controllers/ProductionOrdersController.cs` (Inline-Mapping + ToggleRelease/BulkRelease/SetPriority)
- `IdealAkeWms/Controllers/ProductionOrdersApiController.cs` (`ToggleField` entfernt, Search bleibt)
- `IdealAkeWms/Controllers/PickingController.cs` (Index-Mapping + SetPickingStatus)
- `IdealAkeWms/Models/ViewModels/ProductionOrderViewModel.cs` (Felder bleiben strukturell, Datenherkunft ändert sich)
- `IdealAkeWms/Models/ViewModels/PickingListItem.cs` (sofern strukturelle Anpassung nötig — siehe Task 6)
- `IdealAkeWms/Views/ProductionOrders/Index.cshtml` (Checkbox-Markup + JS-Dispatcher)
- `IdealAkeWms/AppVersion.cs`
- `IdealAkeWmsService/AppVersion.cs`
- `IdealAkeWms/Views/Help/Changelog.cshtml`
- `IdealAkeWms/Views/Help/Index.cshtml`
- `IdealAkeWms.Tests/...` (~25 vorhandene Tests, siehe Task 8)
- `SQL/00_FreshInstall.sql`
- `SQL/AgentJobs/01_Import_Produktionsauftraege.sql`
- `CLAUDE.md`
- `docs/TESTSZENARIEN.md`

---

## Task 1: Entity-Layer + ApplicationDbContext

**Files:**
- Modify: `IdealAkeWms/Models/ProductionOrder.cs`
- New: `IdealAkeWms/Models/ProductionOrderPickingStatus.cs`
- New: `IdealAkeWms/Models/ProductionOrderBdeStatus.cs`
- New: `IdealAkeWms/Models/ProductionOrderAssemblyGroup.cs`
- New: `IdealAkeWms/Models/ProductionOrderAssemblyGroupSpec.cs`
- New: `IdealAkeWms/Models/ProductionWorkplaceAssemblyGroup.cs`
- Modify: `IdealAkeWms/Data/ApplicationDbContext.cs`

Nach diesem Task ist der Build ROT — Konsumenten der 16 entfernten Properties brechen. Das ist beabsichtigt (Spec 12.7).

- [ ] **Step 1: `ProductionOrderPickingStatus.cs` anlegen**

```csharp
using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

public class ProductionOrderPickingStatus : AuditableEntity
{
    public int ProductionOrderId { get; set; }
    public ProductionOrder ProductionOrder { get; set; } = null!;

    [StringLength(50)]
    [Display(Name = "Kommissionierung")]
    public string? PickingStatus { get; set; }

    [Display(Name = "Priorität")]
    public int? PickingPriority { get; set; }

    [Display(Name = "Freigegeben")]
    public bool IsReleasedForPicking { get; set; }

    [Display(Name = "Freigegeben am")]
    public DateTime? ReleasedAt { get; set; }

    [StringLength(200)]
    [Display(Name = "Freigegeben von")]
    public string? ReleasedBy { get; set; }

    public int? AssignedPickerId { get; set; }
    public User? AssignedPicker { get; set; }

    [StringLength(200)]
    [Display(Name = "Kommissionierer")]
    public string? AssignedPickerName { get; set; }

    [Display(Name = "Glas")]
    public bool HasGlass { get; set; }

    [Display(Name = "Zukauf")]
    public bool HasExternalPurchase { get; set; }

    /// <summary>Sync-calculated. Reset von IsCoatingDone wenn auf false flippt (Fallstrick #11).</summary>
    public bool HasCoatingParts { get; set; }

    /// <summary>User-toggleable.</summary>
    public bool IsCoatingDone { get; set; }

    /// <summary>Neu in v1.11.0 — Picking-spezifischer Done-Marker, ergaenzt FA-Master-IsDone.</summary>
    public bool IsDonePicking { get; set; }
}
```

- [ ] **Step 2: `ProductionOrderBdeStatus.cs` anlegen**

```csharp
using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

public class ProductionOrderBdeStatus : AuditableEntity
{
    public int ProductionOrderId { get; set; }
    public ProductionOrder ProductionOrder { get; set; } = null!;

    [Display(Name = "BDE abgeschlossen")]
    public bool IsDoneBde { get; set; }
}
```

- [ ] **Step 3: `ProductionOrderAssemblyGroup.cs` anlegen**

```csharp
using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

public class ProductionOrderAssemblyGroup : AuditableEntity
{
    public int ProductionOrderId { get; set; }
    public ProductionOrder ProductionOrder { get; set; } = null!;

    /// <summary>VK / VL / VE / VT / VA</summary>
    [Required]
    [StringLength(10)]
    public string GroupKey { get; set; } = string.Empty;

    public bool IsApplicable { get; set; }

    /// <summary>Phase 4 — bleibt in Phase 1 immer false.</summary>
    public bool IsCompleted { get; set; }

    public DateTime? CompletedAt { get; set; }

    [StringLength(200)]
    public string? CompletedBy { get; set; }

    public ICollection<ProductionOrderAssemblyGroupSpec> Specs { get; set; } =
        new List<ProductionOrderAssemblyGroupSpec>();
}
```

- [ ] **Step 4: `ProductionOrderAssemblyGroupSpec.cs` anlegen**

```csharp
using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

public class ProductionOrderAssemblyGroupSpec : AuditableEntity
{
    public int AssemblyGroupId { get; set; }
    public ProductionOrderAssemblyGroup AssemblyGroup { get; set; } = null!;

    public int? ArticleId { get; set; }
    public Article? Article { get; set; }

    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    public decimal? Quantity { get; set; }

    public string? Notes { get; set; }

    public int SortOrder { get; set; }
}
```

- [ ] **Step 5: `ProductionWorkplaceAssemblyGroup.cs` anlegen**

```csharp
using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

public class ProductionWorkplaceAssemblyGroup : AuditableEntity
{
    public int ProductionWorkplaceId { get; set; }
    public ProductionWorkplace ProductionWorkplace { get; set; } = null!;

    [Required]
    [StringLength(10)]
    public string GroupKey { get; set; } = string.Empty;
}
```

- [ ] **Step 6: `ProductionOrder.cs` schlanken**

Die Spec 4.1 listet die 16 Drops. Nach Refactor enthaelt `ProductionOrder.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

public class ProductionOrder : AuditableEntity
{
    [Required(ErrorMessage = "FA Nummer ist erforderlich")]
    [StringLength(100)]
    [Display(Name = "FA Nummer")]
    public string OrderNumber { get; set; } = string.Empty;

    [Display(Name = "Stückzahl")]
    public decimal Quantity { get; set; }

    [StringLength(200)]
    [Display(Name = "Kunde")]
    public string? Customer { get; set; }

    [StringLength(100)]
    [Display(Name = "Artikelnummer")]
    public string? ArticleNumber { get; set; }

    [StringLength(500)]
    [Display(Name = "Bezeichnung 1")]
    public string? Description1 { get; set; }

    [StringLength(500)]
    [Display(Name = "Bezeichnung 2")]
    public string? Description2 { get; set; }

    [Display(Name = "Fertigungstermin")]
    [DataType(DataType.Date)]
    public DateTime? ProductionDate { get; set; }

    [Display(Name = "Liefertermin")]
    [DataType(DataType.Date)]
    public DateTime? DeliveryDate { get; set; }

    [Display(Name = "Erledigt")]
    public bool IsDone { get; set; }

    [Display(Name = "Werkbank")]
    public int? ProductionWorkplaceId { get; set; }
    public ProductionWorkplace? ProductionWorkplace { get; set; }

    public ICollection<WorkOperation> WorkOperations { get; set; } = new List<WorkOperation>();

    // Phase 1 — neue Nav-Properties (siehe Spec 5.1)
    public ProductionOrderPickingStatus? PickingStatus { get; set; }
    public ProductionOrderBdeStatus? BdeStatus { get; set; }
    public ICollection<ProductionOrderAssemblyGroup> AssemblyGroups { get; set; }
        = new List<ProductionOrderAssemblyGroup>();
}
```

**WICHTIG:** Die Properties `PickingStatus` (heute `string?`), `PickingPriority`, `IsReleasedForPicking`, `ReleasedAt`, `ReleasedBy`, `AssignedPickerId`, `AssignedPicker`, `AssignedPickerName`, `HasGlass`, `HasExternalPurchase`, `HasCoatingParts`, `IsCoatingDone`, `HasCooling`, `HasFan`, `HasElectric`, `HasDoors`, `HasSuperstructure` sind ALLE entfernt. `PickingStatus` als Nav-Property mit gleichem Namen (Spec 5.1) ist bewusst — Type-Mismatch erzwingt Migration.

- [ ] **Step 7: `ApplicationDbContext.cs` — DbSets**

Im `ApplicationDbContext` Konstruktor-Bereich (vor `OnModelCreating`):

```csharp
public DbSet<ProductionOrderPickingStatus> ProductionOrderPickingStatuses { get; set; } = null!;
public DbSet<ProductionOrderBdeStatus> ProductionOrderBdeStatuses { get; set; } = null!;
public DbSet<ProductionOrderAssemblyGroup> ProductionOrderAssemblyGroups { get; set; } = null!;
public DbSet<ProductionOrderAssemblyGroupSpec> ProductionOrderAssemblyGroupSpecs { get; set; } = null!;
public DbSet<ProductionWorkplaceAssemblyGroup> ProductionWorkplaceAssemblyGroups { get; set; } = null!;
```

- [ ] **Step 8: `ApplicationDbContext.OnModelCreating` — alten `ProductionOrder`-Block bereinigen**

Im bestehenden `modelBuilder.Entity<ProductionOrder>(entity => ...)`-Block (ApplicationDbContext.cs Zeile 337-378) entfernen:
- `entity.Property(e => e.PickingStatus).HasMaxLength(50);`
- `entity.Property(e => e.ReleasedBy).HasMaxLength(200);`
- `entity.HasIndex(e => new { e.IsReleasedForPicking, e.IsDone })` (Index wandert in PickingStatus-Tabelle)
- `entity.Property(e => e.AssignedPickerName).HasMaxLength(200);`
- `entity.HasIndex(e => e.AssignedPickerId)` (wandert in PickingStatus)
- `entity.HasOne(e => e.AssignedPicker)` (wandert in PickingStatus)

Bleibend: `OrderNumber`, `Customer`, `ArticleNumber`, `Description1/2`, `CreatedBy/ByWindows`, `ModifiedBy/ByWindows`, `UQ_OrderNumber`-Index, `ArticleNumber`-Index, `IsDone`-Index, `ProductionWorkplaceId`-Index + FK auf `ProductionWorkplace`.

- [ ] **Step 9: `ApplicationDbContext.OnModelCreating` — neue Entity-Konfigurationen**

Direkt nach dem `ProductionOrder`-Block einfügen (Spec 5.2):

```csharp
modelBuilder.Entity<ProductionOrderPickingStatus>(entity =>
{
    entity.ToTable("ProductionOrderPickingStatus");
    entity.HasKey(e => e.Id);
    entity.Property(e => e.PickingStatus).HasMaxLength(50);
    entity.Property(e => e.ReleasedBy).HasMaxLength(200);
    entity.Property(e => e.AssignedPickerName).HasMaxLength(200);
    entity.Property(e => e.CreatedBy).HasMaxLength(200).IsRequired();
    entity.Property(e => e.CreatedByWindows).HasMaxLength(200).IsRequired();
    entity.Property(e => e.ModifiedBy).HasMaxLength(200);
    entity.Property(e => e.ModifiedByWindows).HasMaxLength(200);

    entity.HasIndex(e => e.ProductionOrderId).IsUnique()
        .HasDatabaseName("UQ_ProductionOrderPickingStatus_ProductionOrderId");
    entity.HasIndex(e => e.IsReleasedForPicking)
        .HasDatabaseName("IX_ProductionOrderPickingStatus_IsReleasedForPicking");
    entity.HasIndex(e => e.AssignedPickerId)
        .HasFilter("[AssignedPickerId] IS NOT NULL")
        .HasDatabaseName("IX_ProductionOrderPickingStatus_AssignedPickerId");

    entity.HasOne(e => e.ProductionOrder)
        .WithOne(p => p.PickingStatus)
        .HasForeignKey<ProductionOrderPickingStatus>(e => e.ProductionOrderId)
        .OnDelete(DeleteBehavior.Cascade);

    entity.HasOne(e => e.AssignedPicker)
        .WithMany()
        .HasForeignKey(e => e.AssignedPickerId)
        .OnDelete(DeleteBehavior.SetNull);
});

modelBuilder.Entity<ProductionOrderBdeStatus>(entity =>
{
    entity.ToTable("ProductionOrderBdeStatus");
    entity.HasKey(e => e.Id);
    entity.Property(e => e.CreatedBy).HasMaxLength(200).IsRequired();
    entity.Property(e => e.CreatedByWindows).HasMaxLength(200).IsRequired();
    entity.Property(e => e.ModifiedBy).HasMaxLength(200);
    entity.Property(e => e.ModifiedByWindows).HasMaxLength(200);

    entity.HasIndex(e => e.ProductionOrderId).IsUnique()
        .HasDatabaseName("UQ_ProductionOrderBdeStatus_ProductionOrderId");

    entity.HasOne(e => e.ProductionOrder)
        .WithOne(p => p.BdeStatus)
        .HasForeignKey<ProductionOrderBdeStatus>(e => e.ProductionOrderId)
        .OnDelete(DeleteBehavior.Cascade);
});

modelBuilder.Entity<ProductionOrderAssemblyGroup>(entity =>
{
    entity.ToTable("ProductionOrderAssemblyGroups");
    entity.HasKey(e => e.Id);
    entity.Property(e => e.GroupKey).HasMaxLength(10).IsRequired();
    entity.Property(e => e.CompletedBy).HasMaxLength(200);
    entity.Property(e => e.CreatedBy).HasMaxLength(200).IsRequired();
    entity.Property(e => e.CreatedByWindows).HasMaxLength(200).IsRequired();
    entity.Property(e => e.ModifiedBy).HasMaxLength(200);
    entity.Property(e => e.ModifiedByWindows).HasMaxLength(200);

    entity.HasIndex(e => new { e.ProductionOrderId, e.GroupKey }).IsUnique()
        .HasDatabaseName("UQ_ProductionOrderAssemblyGroups_PO_Key");
    entity.HasIndex(e => new { e.GroupKey, e.IsApplicable })
        .HasDatabaseName("IX_ProductionOrderAssemblyGroups_GroupKey_IsApplicable");

    entity.HasOne(e => e.ProductionOrder)
        .WithMany(p => p.AssemblyGroups)
        .HasForeignKey(e => e.ProductionOrderId)
        .OnDelete(DeleteBehavior.Cascade);
});

modelBuilder.Entity<ProductionOrderAssemblyGroupSpec>(entity =>
{
    entity.ToTable("ProductionOrderAssemblyGroupSpecs");
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Description).HasMaxLength(500).IsRequired();
    entity.Property(e => e.Quantity).HasColumnType("decimal(18,3)");
    entity.Property(e => e.Notes).HasColumnType("nvarchar(max)");
    entity.Property(e => e.CreatedBy).HasMaxLength(200).IsRequired();
    entity.Property(e => e.CreatedByWindows).HasMaxLength(200).IsRequired();
    entity.Property(e => e.ModifiedBy).HasMaxLength(200);
    entity.Property(e => e.ModifiedByWindows).HasMaxLength(200);

    entity.HasIndex(e => e.AssemblyGroupId)
        .HasDatabaseName("IX_ProductionOrderAssemblyGroupSpecs_AssemblyGroupId");
    entity.HasIndex(e => e.ArticleId)
        .HasFilter("[ArticleId] IS NOT NULL")
        .HasDatabaseName("IX_ProductionOrderAssemblyGroupSpecs_ArticleId");

    entity.HasOne(e => e.AssemblyGroup)
        .WithMany(g => g.Specs)
        .HasForeignKey(e => e.AssemblyGroupId)
        .OnDelete(DeleteBehavior.Cascade);
    entity.HasOne(e => e.Article)
        .WithMany()
        .HasForeignKey(e => e.ArticleId)
        .OnDelete(DeleteBehavior.SetNull);
});

modelBuilder.Entity<ProductionWorkplaceAssemblyGroup>(entity =>
{
    entity.ToTable("ProductionWorkplaceAssemblyGroups");
    entity.HasKey(e => e.Id);
    entity.Property(e => e.GroupKey).HasMaxLength(10).IsRequired();
    entity.Property(e => e.CreatedBy).HasMaxLength(200).IsRequired();
    entity.Property(e => e.CreatedByWindows).HasMaxLength(200).IsRequired();
    entity.Property(e => e.ModifiedBy).HasMaxLength(200);
    entity.Property(e => e.ModifiedByWindows).HasMaxLength(200);

    entity.HasIndex(e => new { e.ProductionWorkplaceId, e.GroupKey }).IsUnique()
        .HasDatabaseName("UQ_ProductionWorkplaceAssemblyGroups_WP_Key");

    entity.HasOne(e => e.ProductionWorkplace)
        .WithMany()
        .HasForeignKey(e => e.ProductionWorkplaceId)
        .OnDelete(DeleteBehavior.Cascade);
});
```

- [ ] **Step 10: Build (erwartet ROT — viele Konsumenten brechen)**

```pwsh
dotnet build --nologo
```

Erwartete Fehler:
- `ProductionOrdersApiController.ToggleField` — Properties `HasGlass`/`HasCooling`/etc. existieren nicht mehr → Fehler.
- `ProductionOrdersController.Index` Mapping (Zeile 281-333) — gleicher Fehler.
- `ProductionOrdersController.ToggleRelease/BulkRelease/SetPriority` — `order.IsReleasedForPicking` etc. weg.
- `PickingController.Index` Mapping (Zeile 91-115) — `o.PickingPriority`, `o.PickingStatus` (jetzt Nav-Property!), `o.AssignedPickerId`, `o.AssignedPickerName` brechen.
- `PickingController.SetPickingStatus` (Zeile 357-373) — `order.PickingStatus = status` → Type-Mismatch.
- `ProductionOrderRepository.GetReleasedForPickingAsync/ByPickerAsync/CountAsync/SetCoatingFlagsAsync` — alle brechen.
- Tests (`ProductionOrdersControllerPickerTests`, `PickingApiControllerTests`, `LocationTransferTests` u.a.) brechen wegen Setup-Daten.

Diese Fehler werden in Tasks 4-8 systematisch repariert. **Diesen Task NICHT mit grünem Build abschliessen** — Commit erfolgt trotz roter Compiler, weil der Refactor atomar ist.

- [ ] **Step 11: Commit (rot)**

```pwsh
git add IdealAkeWms/Models/ProductionOrder.cs IdealAkeWms/Models/ProductionOrderPickingStatus.cs IdealAkeWms/Models/ProductionOrderBdeStatus.cs IdealAkeWms/Models/ProductionOrderAssemblyGroup.cs IdealAkeWms/Models/ProductionOrderAssemblyGroupSpec.cs IdealAkeWms/Models/ProductionWorkplaceAssemblyGroup.cs IdealAkeWms/Data/ApplicationDbContext.cs
git commit -m @'
refactor(productionorders): introduce 5 status entities and slim ProductionOrder

Spec 4-5. Build is intentionally red until Tasks 4-7 finish the consumer
migration. PickingStatus property renamed to navigation (spec 12.7).

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
'@
```

---

## Task 2: EF Migration + SQL Data-Copy Skript

**Files:**
- New: `IdealAkeWms/Migrations/<TIMESTAMP>_AddProductionOrderSplit.cs` (EF-generiert, dann HAND-EDITIERT)
- New: `IdealAkeWms/Migrations/<TIMESTAMP>_AddProductionOrderSplit.Designer.cs` (EF-generiert)
- Modify: `IdealAkeWms/Migrations/ApplicationDbContextModelSnapshot.cs` (EF-aktualisiert)
- New: `SQL/60_ProductionOrderSplit.sql`
- Modify: `SQL/00_FreshInstall.sql`

- [ ] **Step 1: Pre-Check — Rebase-Konflikt-Vermeidung (Spec 12.5)**

```pwsh
git status
git log --oneline -3
dotnet ef migrations list --project IdealAkeWms --no-build
```

Erwartet: Letzte Migration ist `AddProductionOrderAssemblyFlags` (oder neuer, falls main vorgerueckt). Wenn der Snapshot bereits eine andere Pending-Migration zeigt, vor dem `add` mit `dotnet ef migrations remove --force --project IdealAkeWms` aufräumen — siehe Spec 12.5.

- [ ] **Step 2: EF-Migration generieren**

```pwsh
dotnet ef migrations add AddProductionOrderSplit --project IdealAkeWms
```

Erwartet: 3 neue Dateien unter `IdealAkeWms/Migrations/`. EF erkennt anhand des aktualisierten `DbContext` automatisch alle 5 `CreateTable`s, 16 `DropColumn`s und alle Indexe. **NICHT** den generierten Code blind committen.

- [ ] **Step 3: Migration hand-editieren — Datenkopie VOR DropColumn**

Den `Up()`-Body öffnen. EF generiert in dieser Reihenfolge:
1. `migrationBuilder.CreateTable(...)` × 5
2. `migrationBuilder.DropForeignKey(...)` für `FK_ProductionOrders_AssignedPicker`
3. `migrationBuilder.DropIndex(...)` für `IX_ProductionOrders_IsReleasedForPicking_IsDone`, `IX_ProductionOrders_AssignedPickerId`
4. `migrationBuilder.DropColumn(...)` × 16
5. `migrationBuilder.CreateIndex(...)` für neue Indexe

Die Datenkopie muss zwischen Schritt 1 und Schritt 4 geschaltet werden — am sichersten **nach allen CreateTable, nach DropForeignKey + DropIndex, aber VOR DropColumn**. EF generiert das alles linear; den Datenkopie-Block via `migrationBuilder.Sql(@"...")` an die richtige Stelle einfügen:

```csharp
// === DATEN-MIGRATION ZWISCHEN CREATE TABLES UND DROP COLUMNS ===
// Spec 8 — batched-INSERT 5000/Batch, idempotent via NOT EXISTS, audit "Migration_60".
migrationBuilder.Sql(@"
DECLARE @batchSize INT = 5000;
DECLARE @rows INT = 1;
DECLARE @lastId INT = 0;

-- PickingStatus
WHILE @rows > 0
BEGIN
    INSERT INTO dbo.ProductionOrderPickingStatus (
        ProductionOrderId, PickingStatus, PickingPriority,
        IsReleasedForPicking, ReleasedAt, ReleasedBy,
        AssignedPickerId, AssignedPickerName,
        HasGlass, HasExternalPurchase, HasCoatingParts, IsCoatingDone, IsDonePicking,
        CreatedAt, CreatedBy, CreatedByWindows)
    SELECT TOP (@batchSize)
        p.Id, p.PickingStatus, p.PickingPriority,
        p.IsReleasedForPicking, p.ReleasedAt, p.ReleasedBy,
        p.AssignedPickerId, p.AssignedPickerName,
        p.HasGlass, p.HasExternalPurchase, p.HasCoatingParts, p.IsCoatingDone, 0,
        GETDATE(), 'Migration_60', SYSTEM_USER
    FROM dbo.ProductionOrders p
    WHERE p.Id > @lastId
      AND NOT EXISTS (SELECT 1 FROM dbo.ProductionOrderPickingStatus s WHERE s.ProductionOrderId = p.Id)
    ORDER BY p.Id;
    SET @rows = @@ROWCOUNT;
    IF @rows > 0
        SET @lastId = (SELECT MAX(ProductionOrderId) FROM dbo.ProductionOrderPickingStatus);
END

-- BdeStatus
SET @rows = 1; SET @lastId = 0;
WHILE @rows > 0
BEGIN
    INSERT INTO dbo.ProductionOrderBdeStatus (
        ProductionOrderId, IsDoneBde,
        CreatedAt, CreatedBy, CreatedByWindows)
    SELECT TOP (@batchSize)
        p.Id, 0, GETDATE(), 'Migration_60', SYSTEM_USER
    FROM dbo.ProductionOrders p
    WHERE p.Id > @lastId
      AND NOT EXISTS (SELECT 1 FROM dbo.ProductionOrderBdeStatus s WHERE s.ProductionOrderId = p.Id)
    ORDER BY p.Id;
    SET @rows = @@ROWCOUNT;
    IF @rows > 0
        SET @lastId = (SELECT MAX(ProductionOrderId) FROM dbo.ProductionOrderBdeStatus);
END

-- AssemblyGroups (5 INSERTs/FA via UNION ALL je Batch)
SET @rows = 1; SET @lastId = 0;
WHILE @rows > 0
BEGIN
    ;WITH NextBatch AS (
        SELECT TOP (@batchSize) p.Id,
               p.HasCooling, p.HasFan, p.HasElectric, p.HasDoors, p.HasSuperstructure
        FROM dbo.ProductionOrders p
        WHERE p.Id > @lastId
          AND NOT EXISTS (SELECT 1 FROM dbo.ProductionOrderAssemblyGroups g WHERE g.ProductionOrderId = p.Id)
        ORDER BY p.Id
    )
    INSERT INTO dbo.ProductionOrderAssemblyGroups (
        ProductionOrderId, GroupKey, IsApplicable, IsCompleted,
        CreatedAt, CreatedBy, CreatedByWindows)
    SELECT Id, GroupKey,
           CASE GroupKey
               WHEN 'VK' THEN HasCooling
               WHEN 'VL' THEN HasFan
               WHEN 'VE' THEN HasElectric
               WHEN 'VT' THEN HasDoors
               WHEN 'VA' THEN HasSuperstructure
           END AS IsApplicable,
           0 AS IsCompleted,
           GETDATE(), 'Migration_60', SYSTEM_USER
    FROM NextBatch
    CROSS JOIN (VALUES ('VK'),('VL'),('VE'),('VT'),('VA')) k(GroupKey);
    SET @rows = @@ROWCOUNT / 5;
    IF @rows > 0
        SET @lastId = (SELECT MAX(ProductionOrderId) FROM dbo.ProductionOrderAssemblyGroups);
END

-- Verifikations-Counts (kein THROW, nur Log)
DECLARE @poCount INT = (SELECT COUNT(*) FROM dbo.ProductionOrders);
DECLARE @psCount INT = (SELECT COUNT(*) FROM dbo.ProductionOrderPickingStatus);
DECLARE @bdeCount INT = (SELECT COUNT(*) FROM dbo.ProductionOrderBdeStatus);
DECLARE @grpCount INT = (SELECT COUNT(*) FROM dbo.ProductionOrderAssemblyGroups);
PRINT 'Migration 60: ProductionOrders=' + CAST(@poCount AS NVARCHAR);
PRINT 'Migration 60: ProductionOrderPickingStatus=' + CAST(@psCount AS NVARCHAR) + ' (Erwartet=' + CAST(@poCount AS NVARCHAR) + ')';
PRINT 'Migration 60: ProductionOrderBdeStatus=' + CAST(@bdeCount AS NVARCHAR) + ' (Erwartet=' + CAST(@poCount AS NVARCHAR) + ')';
PRINT 'Migration 60: ProductionOrderAssemblyGroups=' + CAST(@grpCount AS NVARCHAR) + ' (Erwartet=' + CAST(5 * @poCount AS NVARCHAR) + ')';
");
```

Den Block direkt nach den `CreateTable`-Calls und VOR den `DropColumn`-Calls einfügen. **Manuell in der Datei verifizieren**, dass die Reihenfolge im Up() stimmt — sonst geht die Datenkopie nach dem Drop und alles ist `NULL`.

Im `Down()` reicht der EF-generierte Rollback (Tables droppen + 16 Columns wiederherstellen). Daten-Rollback ist nicht Teil von Phase 1 — Restore via DB-Backup (Roadmap 5.5).

- [ ] **Step 4: SQL-Skript `60_ProductionOrderSplit.sql` anlegen**

`SQL/60_ProductionOrderSplit.sql` ist eigenständig idempotent (Reapply-fest). Struktur nach Spec 8.2:

```sql
-- =============================================
-- 60_ProductionOrderSplit.sql
-- Phase 1 Big-Bang Refactor: ProductionOrders → 5 Tabellen
-- Idempotent, kann mehrfach ausgefuehrt werden.
-- HINWEIS: App startet mit db.Database.Migrate() → EF-Migration uebernimmt.
--          Dieses Skript ist fuer manuellen DBA-Lauf im Wartungsfenster
--          (Spec 14, Roadmap 5.6).
-- =============================================

-- SECTION A: TABELLEN ANLEGEN ----------------------------------------------

IF OBJECT_ID(N'dbo.ProductionOrderPickingStatus', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ProductionOrderPickingStatus] (
        [Id]                    INT IDENTITY(1,1) NOT NULL,
        [ProductionOrderId]     INT               NOT NULL,
        [PickingStatus]         NVARCHAR(50)      NULL,
        [PickingPriority]       INT               NULL,
        [IsReleasedForPicking]  BIT               NOT NULL CONSTRAINT DF_ProductionOrderPickingStatus_IsReleasedForPicking DEFAULT 0,
        [ReleasedAt]            DATETIME2         NULL,
        [ReleasedBy]            NVARCHAR(200)     NULL,
        [AssignedPickerId]      INT               NULL,
        [AssignedPickerName]    NVARCHAR(200)     NULL,
        [HasGlass]              BIT               NOT NULL CONSTRAINT DF_ProductionOrderPickingStatus_HasGlass DEFAULT 0,
        [HasExternalPurchase]   BIT               NOT NULL CONSTRAINT DF_ProductionOrderPickingStatus_HasExternalPurchase DEFAULT 0,
        [HasCoatingParts]       BIT               NOT NULL CONSTRAINT DF_ProductionOrderPickingStatus_HasCoatingParts DEFAULT 0,
        [IsCoatingDone]         BIT               NOT NULL CONSTRAINT DF_ProductionOrderPickingStatus_IsCoatingDone DEFAULT 0,
        [IsDonePicking]         BIT               NOT NULL CONSTRAINT DF_ProductionOrderPickingStatus_IsDonePicking DEFAULT 0,
        [CreatedAt]             DATETIME2         NOT NULL DEFAULT GETDATE(),
        [CreatedBy]             NVARCHAR(200)     NOT NULL,
        [CreatedByWindows]      NVARCHAR(200)     NOT NULL,
        [ModifiedAt]            DATETIME2         NULL,
        [ModifiedBy]            NVARCHAR(200)     NULL,
        [ModifiedByWindows]     NVARCHAR(200)     NULL,
        CONSTRAINT [PK_ProductionOrderPickingStatus] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_ProductionOrderPickingStatus_ProductionOrderId] UNIQUE ([ProductionOrderId]),
        CONSTRAINT [FK_ProductionOrderPickingStatus_ProductionOrder]
            FOREIGN KEY ([ProductionOrderId]) REFERENCES [dbo].[ProductionOrders]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProductionOrderPickingStatus_AssignedPicker]
            FOREIGN KEY ([AssignedPickerId]) REFERENCES [dbo].[Users]([Id]) ON DELETE SET NULL
    );
    CREATE INDEX [IX_ProductionOrderPickingStatus_IsReleasedForPicking]
        ON [dbo].[ProductionOrderPickingStatus]([IsReleasedForPicking]) INCLUDE ([PickingPriority]);
    CREATE INDEX [IX_ProductionOrderPickingStatus_AssignedPickerId]
        ON [dbo].[ProductionOrderPickingStatus]([AssignedPickerId])
        WHERE [AssignedPickerId] IS NOT NULL;
    PRINT 'Tabelle ProductionOrderPickingStatus erstellt.';
END
GO

IF OBJECT_ID(N'dbo.ProductionOrderBdeStatus', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ProductionOrderBdeStatus] (
        [Id]                INT IDENTITY(1,1) NOT NULL,
        [ProductionOrderId] INT               NOT NULL,
        [IsDoneBde]         BIT               NOT NULL CONSTRAINT DF_ProductionOrderBdeStatus_IsDoneBde DEFAULT 0,
        [CreatedAt]         DATETIME2         NOT NULL DEFAULT GETDATE(),
        [CreatedBy]         NVARCHAR(200)     NOT NULL,
        [CreatedByWindows]  NVARCHAR(200)     NOT NULL,
        [ModifiedAt]        DATETIME2         NULL,
        [ModifiedBy]        NVARCHAR(200)     NULL,
        [ModifiedByWindows] NVARCHAR(200)     NULL,
        CONSTRAINT [PK_ProductionOrderBdeStatus] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_ProductionOrderBdeStatus_ProductionOrderId] UNIQUE ([ProductionOrderId]),
        CONSTRAINT [FK_ProductionOrderBdeStatus_ProductionOrder]
            FOREIGN KEY ([ProductionOrderId]) REFERENCES [dbo].[ProductionOrders]([Id]) ON DELETE CASCADE
    );
    PRINT 'Tabelle ProductionOrderBdeStatus erstellt.';
END
GO

IF OBJECT_ID(N'dbo.ProductionOrderAssemblyGroups', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ProductionOrderAssemblyGroups] (
        [Id]                INT IDENTITY(1,1) NOT NULL,
        [ProductionOrderId] INT               NOT NULL,
        [GroupKey]          NVARCHAR(10)      NOT NULL,
        [IsApplicable]      BIT               NOT NULL CONSTRAINT DF_ProductionOrderAssemblyGroups_IsApplicable DEFAULT 0,
        [IsCompleted]       BIT               NOT NULL CONSTRAINT DF_ProductionOrderAssemblyGroups_IsCompleted DEFAULT 0,
        [CompletedAt]       DATETIME2         NULL,
        [CompletedBy]       NVARCHAR(200)     NULL,
        [CreatedAt]         DATETIME2         NOT NULL DEFAULT GETDATE(),
        [CreatedBy]         NVARCHAR(200)     NOT NULL,
        [CreatedByWindows]  NVARCHAR(200)     NOT NULL,
        [ModifiedAt]        DATETIME2         NULL,
        [ModifiedBy]        NVARCHAR(200)     NULL,
        [ModifiedByWindows] NVARCHAR(200)     NULL,
        CONSTRAINT [PK_ProductionOrderAssemblyGroups] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_ProductionOrderAssemblyGroups_PO_Key] UNIQUE ([ProductionOrderId], [GroupKey]),
        CONSTRAINT [FK_ProductionOrderAssemblyGroups_ProductionOrder]
            FOREIGN KEY ([ProductionOrderId]) REFERENCES [dbo].[ProductionOrders]([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_ProductionOrderAssemblyGroups_GroupKey_IsApplicable]
        ON [dbo].[ProductionOrderAssemblyGroups]([GroupKey], [IsApplicable]);
    PRINT 'Tabelle ProductionOrderAssemblyGroups erstellt.';
END
GO

IF OBJECT_ID(N'dbo.ProductionOrderAssemblyGroupSpecs', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ProductionOrderAssemblyGroupSpecs] (
        [Id]                INT IDENTITY(1,1) NOT NULL,
        [AssemblyGroupId]   INT               NOT NULL,
        [ArticleId]         INT               NULL,
        [Description]       NVARCHAR(500)     NOT NULL,
        [Quantity]          DECIMAL(18,3)     NULL,
        [Notes]             NVARCHAR(MAX)     NULL,
        [SortOrder]         INT               NOT NULL CONSTRAINT DF_ProductionOrderAssemblyGroupSpecs_SortOrder DEFAULT 0,
        [CreatedAt]         DATETIME2         NOT NULL DEFAULT GETDATE(),
        [CreatedBy]         NVARCHAR(200)     NOT NULL,
        [CreatedByWindows]  NVARCHAR(200)     NOT NULL,
        [ModifiedAt]        DATETIME2         NULL,
        [ModifiedBy]        NVARCHAR(200)     NULL,
        [ModifiedByWindows] NVARCHAR(200)     NULL,
        CONSTRAINT [PK_ProductionOrderAssemblyGroupSpecs] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_ProductionOrderAssemblyGroupSpecs_AssemblyGroup]
            FOREIGN KEY ([AssemblyGroupId]) REFERENCES [dbo].[ProductionOrderAssemblyGroups]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProductionOrderAssemblyGroupSpecs_Article]
            FOREIGN KEY ([ArticleId]) REFERENCES [dbo].[Articles]([Id]) ON DELETE SET NULL
    );
    CREATE INDEX [IX_ProductionOrderAssemblyGroupSpecs_AssemblyGroupId]
        ON [dbo].[ProductionOrderAssemblyGroupSpecs]([AssemblyGroupId]);
    CREATE INDEX [IX_ProductionOrderAssemblyGroupSpecs_ArticleId]
        ON [dbo].[ProductionOrderAssemblyGroupSpecs]([ArticleId]) WHERE [ArticleId] IS NOT NULL;
    PRINT 'Tabelle ProductionOrderAssemblyGroupSpecs erstellt.';
END
GO

IF OBJECT_ID(N'dbo.ProductionWorkplaceAssemblyGroups', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ProductionWorkplaceAssemblyGroups] (
        [Id]                    INT IDENTITY(1,1) NOT NULL,
        [ProductionWorkplaceId] INT               NOT NULL,
        [GroupKey]              NVARCHAR(10)      NOT NULL,
        [CreatedAt]             DATETIME2         NOT NULL DEFAULT GETDATE(),
        [CreatedBy]             NVARCHAR(200)     NOT NULL,
        [CreatedByWindows]      NVARCHAR(200)     NOT NULL,
        [ModifiedAt]            DATETIME2         NULL,
        [ModifiedBy]            NVARCHAR(200)     NULL,
        [ModifiedByWindows]     NVARCHAR(200)     NULL,
        CONSTRAINT [PK_ProductionWorkplaceAssemblyGroups] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_ProductionWorkplaceAssemblyGroups_WP_Key] UNIQUE ([ProductionWorkplaceId], [GroupKey]),
        CONSTRAINT [FK_ProductionWorkplaceAssemblyGroups_Workplace]
            FOREIGN KEY ([ProductionWorkplaceId]) REFERENCES [dbo].[ProductionWorkplaces]([Id]) ON DELETE CASCADE
    );
    PRINT 'Tabelle ProductionWorkplaceAssemblyGroups erstellt.';
END
GO

-- SECTION B/C/D: DATEN-KOPIE (batched, idempotent via NOT EXISTS) ----------
-- (Block analog Step 3 oben — kompletter @batchSize=5000-WHILE-Loop fuer alle 3 Tabellen)
-- siehe Migration-Up() in IdealAkeWms/Migrations/<TIMESTAMP>_AddProductionOrderSplit.cs

-- SECTION E: VERIFIKATIONS-COUNTS ------------------------------------------
DECLARE @poCount INT = (SELECT COUNT(*) FROM dbo.ProductionOrders);
PRINT 'Skript 60: ProductionOrders=' + CAST(@poCount AS NVARCHAR);
PRINT 'Skript 60: ProductionOrderPickingStatus=' + CAST((SELECT COUNT(*) FROM dbo.ProductionOrderPickingStatus) AS NVARCHAR);
PRINT 'Skript 60: ProductionOrderBdeStatus=' + CAST((SELECT COUNT(*) FROM dbo.ProductionOrderBdeStatus) AS NVARCHAR);
PRINT 'Skript 60: ProductionOrderAssemblyGroups=' + CAST((SELECT COUNT(*) FROM dbo.ProductionOrderAssemblyGroups) AS NVARCHAR) + ' (Erwartet=' + CAST(5 * @poCount AS NVARCHAR) + ')';
GO

-- SECTION F: SPALTEN DROPPEN (DEFAULT-Constraints zuerst, FK + Index zuerst)
-- Spec 8.2 Drop-Reihenfolge.
-- F.1 DEFAULT-Constraints loeschen
DECLARE @sql NVARCHAR(MAX);
DECLARE constraint_cursor CURSOR FOR
    SELECT 'ALTER TABLE [dbo].[ProductionOrders] DROP CONSTRAINT [' + dc.name + '];'
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
    WHERE dc.parent_object_id = OBJECT_ID(N'dbo.ProductionOrders')
      AND c.name IN ('HasGlass','HasExternalPurchase','HasCooling','HasFan','HasElectric','HasDoors','HasSuperstructure','HasCoatingParts','IsCoatingDone','IsReleasedForPicking');
OPEN constraint_cursor;
FETCH NEXT FROM constraint_cursor INTO @sql;
WHILE @@FETCH_STATUS = 0
BEGIN
    EXEC sp_executesql @sql;
    FETCH NEXT FROM constraint_cursor INTO @sql;
END
CLOSE constraint_cursor; DEALLOCATE constraint_cursor;
GO

-- F.2 FK + Index droppen
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ProductionOrders_AssignedPicker')
    ALTER TABLE [dbo].[ProductionOrders] DROP CONSTRAINT [FK_ProductionOrders_AssignedPicker];
GO
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ProductionOrders_IsReleasedForPicking_IsDone' AND object_id = OBJECT_ID(N'dbo.ProductionOrders'))
    DROP INDEX [IX_ProductionOrders_IsReleasedForPicking_IsDone] ON [dbo].[ProductionOrders];
GO
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ProductionOrders_AssignedPickerId' AND object_id = OBJECT_ID(N'dbo.ProductionOrders'))
    DROP INDEX [IX_ProductionOrders_AssignedPickerId] ON [dbo].[ProductionOrders];
GO

-- F.3 16 Spalten droppen
DECLARE @cols TABLE (name NVARCHAR(128));
INSERT INTO @cols VALUES
    ('PickingStatus'),('PickingPriority'),('IsReleasedForPicking'),
    ('ReleasedAt'),('ReleasedBy'),('AssignedPickerId'),('AssignedPickerName'),
    ('HasGlass'),('HasExternalPurchase'),('HasCoatingParts'),('IsCoatingDone'),
    ('HasCooling'),('HasFan'),('HasElectric'),('HasDoors'),('HasSuperstructure');

DECLARE @colName NVARCHAR(128);
DECLARE col_cursor CURSOR FOR SELECT name FROM @cols;
OPEN col_cursor;
FETCH NEXT FROM col_cursor INTO @colName;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.ProductionOrders') AND name = @colName)
    BEGIN
        SET @sql = N'ALTER TABLE [dbo].[ProductionOrders] DROP COLUMN [' + @colName + '];';
        EXEC sp_executesql @sql;
        PRINT 'Spalte ' + @colName + ' aus ProductionOrders entfernt.';
    END
    FETCH NEXT FROM col_cursor INTO @colName;
END
CLOSE col_cursor; DEALLOCATE col_cursor;
GO

-- SECTION G: __EFMigrationsHistory ----------------------------------------
-- Migration-ID aus dem Generierten EF-File uebernehmen (Step 2)
IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = N'<TIMESTAMP>_AddProductionOrderSplit')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES (N'<TIMESTAMP>_AddProductionOrderSplit', N'10.0.0');
END
GO
```

**WICHTIG:** Die `<TIMESTAMP>`-Platzhalter durch die echte Migration-ID aus Step 2 ersetzen (Format: `YYYYMMDDhhmmss_AddProductionOrderSplit`).

- [ ] **Step 5: `SQL/00_FreshInstall.sql` aktualisieren**

In `SQL/00_FreshInstall.sql` Sektion 8 (Zeile 214-261, ProductionOrders) auf Slim-Version reduzieren:

Innerhalb des `CREATE TABLE [dbo].[ProductionOrders]`-Blocks die folgenden 16 Spalten-Definitionen ENTFERNEN:
- `[PickingStatus]`, `[HasGlass]`, `[HasExternalPurchase]`, `[HasCooling]`, `[HasFan]`, `[HasElectric]`, `[HasDoors]`, `[HasSuperstructure]`, `[HasCoatingParts]`, `[IsCoatingDone]`, `[IsReleasedForPicking]`, `[PickingPriority]`, `[ReleasedAt]`, `[ReleasedBy]`, `[AssignedPickerId]`, `[AssignedPickerName]`
- Die FK-Definition `FK_ProductionOrders_AssignedPicker` ENTFERNEN.

Slim-Version Spec 4.1:

```sql
CREATE TABLE [dbo].[ProductionOrders] (
    [Id]                      INT IDENTITY(1,1) NOT NULL,
    [OrderNumber]             NVARCHAR(100)     NOT NULL,
    [Quantity]                DECIMAL(18,3)     NULL,
    [Customer]                NVARCHAR(500)     NULL,
    [ArticleNumber]           NVARCHAR(100)     NULL,
    [Description1]            NVARCHAR(500)     NULL,
    [Description2]            NVARCHAR(500)     NULL,
    [ProductionDate]          DATETIME2         NULL,
    [DeliveryDate]            DATETIME2         NULL,
    [IsDone]                  BIT               NOT NULL DEFAULT 0,
    [ProductionWorkplaceId]   INT               NULL,
    [CreatedAt]               DATETIME2         NOT NULL DEFAULT GETDATE(),
    [CreatedBy]               NVARCHAR(200)     NOT NULL,
    [CreatedByWindows]        NVARCHAR(200)     NOT NULL,
    [ModifiedAt]              DATETIME2         NULL,
    [ModifiedBy]              NVARCHAR(200)     NULL,
    [ModifiedByWindows]       NVARCHAR(200)     NULL,
    CONSTRAINT [PK_ProductionOrders] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [UQ_ProductionOrders_OrderNumber] UNIQUE ([OrderNumber]),
    CONSTRAINT [FK_ProductionOrders_ProductionWorkplaces_ProductionWorkplaceId]
        FOREIGN KEY ([ProductionWorkplaceId]) REFERENCES [dbo].[ProductionWorkplaces]([Id]) ON DELETE SET NULL
);
```

Direkt nach dem `PRINT 'Tabelle ProductionOrders erstellt.';` (Zeile 259) — VOR `END/GO` und der `PickingItems`-Sektion (Zeile 264) — fünf neue Sub-Sektionen `8a`, `8b`, `8c`, `8d`, `8e` mit den `CREATE TABLE`-Blocks aus Step 4 einfügen (jeweils mit `IF NOT EXISTS`-Guard).

Im `__EFMigrationsHistory`-INSERT-Block (am Ende der Datei) den neuen MigrationId-Eintrag ergänzen.

- [ ] **Step 6: Build + Migration-Liste verifizieren (immer noch ROT erwartet)**

```pwsh
dotnet build --nologo
dotnet ef migrations list --project IdealAkeWms --no-build
```

Erwartet: Build immer noch rot (Konsumenten in Tasks 4-7 nicht repariert). `migrations list` zeigt `AddProductionOrderSplit` ganz unten als "(Pending)".

- [ ] **Step 7: Commit**

```pwsh
git add IdealAkeWms/Migrations/ SQL/60_ProductionOrderSplit.sql SQL/00_FreshInstall.sql
git commit -m @'
refactor(productionorders): ef migration + sql script for 5-table split

Spec 8. Migration hand-edited to copy data BEFORE DropColumn via
migrationBuilder.Sql(). 60_ProductionOrderSplit.sql is idempotent with
NOT EXISTS guards and batched 5000-row INSERT loops.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
'@
```

---

## Task 3: Sage AgentJob — Folge-MERGEs (eager-create)

**Files:**
- Modify: `SQL/AgentJobs/01_Import_Produktionsauftraege.sql`

- [ ] **Step 1: Header-Kommentar aktualisieren**

In `SQL/AgentJobs/01_Import_Produktionsauftraege.sql` Zeilen 1-28 (Kommentar-Block) auf neue Architektur aktualisieren. Die Listen mit `HasGlass, HasCooling, …` aus dem alten Header entfernen — diese Spalten existieren in `ProductionOrders` nicht mehr.

```sql
-- =============================================
-- SQL Server Agent Job: Produktionsauftraege aus Sage importieren
-- Ziel:    [IDEAL_AKE_WMS].[dbo].[ProductionOrders]
-- Quelle:  [ake].[dbo].[vw_AKE_Kommissionierung_WAListe]
--
-- Beschreibung:
--   Importiert neue Werkstaettigungsauftraege aus der Sage-View und aktualisiert
--   bestehende Auftraege wenn sich Sage-Daten geaendert haben (Fertigungstermin,
--   Liefertermin, Stueckzahl, Bezeichnung etc.).
--   Empfohlenes Intervall: taeglich oder stuendlich per SQL Server Agent.
--
-- Felder die aktualisiert werden (kommen aus Sage):
--   Quantity, Customer, ArticleNumber, Description1, Description2,
--   ProductionDate, DeliveryDate
--   ModifiedAt, ModifiedBy, ModifiedByWindows
--
-- Felder die NICHT ueberschrieben werden (App-verwaltet):
--   IsDone, ProductionWorkplaceId, CreatedAt, CreatedBy, CreatedByWindows
--
-- Status-Tabellen (eager-create fuer neue FAs — siehe Sektionen unten):
--   ProductionOrderPickingStatus   (1 Zeile/FA, alle Bool=0)
--   ProductionOrderBdeStatus       (1 Zeile/FA, IsDoneBde=0)
--   ProductionOrderAssemblyGroups  (5 Zeilen/FA: VK/VL/VE/VT/VA, IsApplicable=0)
-- =============================================
```

- [ ] **Step 2: Bestehenden Top-MERGE bereinigen**

Im Top-MERGE (Zeile 30-73) den `WHEN NOT MATCHED BY TARGET ... INSERT`-Block prüfen. Heute steht in der INSERT-Spaltenliste auch `IsDone`. Das bleibt. Alle anderen App-Spalten sind ja schon nicht enthalten. **Keine inhaltliche Aenderung an MERGE-Body.**

- [ ] **Step 3: Folge-MERGE "Picking-Status eager-create" anhaengen**

Direkt nach Zeile 73 (Ende des bestehenden MERGE-Statements) eine Leerzeile und folgenden Block einfügen:

```sql
-- =============================================
-- Folge-MERGE 1: ProductionOrderPickingStatus eager-create
-- Legt fuer JEDEN ProductionOrder genau eine Status-Zeile an, falls noch nicht vorhanden.
-- Idempotent durch NOT MATCHED BY TARGET (Status wird nie ueberschrieben).
-- =============================================
MERGE [IDEAL_AKE_WMS].[dbo].[ProductionOrderPickingStatus] AS s
USING (SELECT Id FROM [IDEAL_AKE_WMS].[dbo].[ProductionOrders]) AS src
    ON s.ProductionOrderId = src.Id
WHEN NOT MATCHED BY TARGET THEN
    INSERT (ProductionOrderId, IsReleasedForPicking,
            HasGlass, HasExternalPurchase, HasCoatingParts, IsCoatingDone, IsDonePicking,
            CreatedAt, CreatedBy, CreatedByWindows)
    VALUES (src.Id, 0, 0, 0, 0, 0, 0,
            GETDATE(), 'Sage_Schnittstelle', SYSTEM_USER);
```

- [ ] **Step 4: Folge-MERGE "BDE-Status eager-create" anhaengen**

```sql
-- =============================================
-- Folge-MERGE 2: ProductionOrderBdeStatus eager-create
-- =============================================
MERGE [IDEAL_AKE_WMS].[dbo].[ProductionOrderBdeStatus] AS s
USING (SELECT Id FROM [IDEAL_AKE_WMS].[dbo].[ProductionOrders]) AS src
    ON s.ProductionOrderId = src.Id
WHEN NOT MATCHED BY TARGET THEN
    INSERT (ProductionOrderId, IsDoneBde, CreatedAt, CreatedBy, CreatedByWindows)
    VALUES (src.Id, 0, GETDATE(), 'Sage_Schnittstelle', SYSTEM_USER);
```

- [ ] **Step 5: Folge-MERGE "AssemblyGroups eager-create (5/FA)" anhaengen**

```sql
-- =============================================
-- Folge-MERGE 3: ProductionOrderAssemblyGroups eager-create (5/FA)
-- CROSS JOIN auf VALUES erzeugt fuer jeden FA × jedem GroupKey eine Source-Zeile.
-- Idempotent: UQ_PO_Key-Index verhindert Duplikate, NOT MATCHED triggert nur fuer Lueckschuesse.
-- =============================================
MERGE [IDEAL_AKE_WMS].[dbo].[ProductionOrderAssemblyGroups] AS s
USING (
    SELECT p.Id AS ProductionOrderId, k.GroupKey
    FROM [IDEAL_AKE_WMS].[dbo].[ProductionOrders] p
    CROSS JOIN (VALUES ('VK'),('VL'),('VE'),('VT'),('VA')) k(GroupKey)
) AS src
    ON s.ProductionOrderId = src.ProductionOrderId AND s.GroupKey = src.GroupKey
WHEN NOT MATCHED BY TARGET THEN
    INSERT (ProductionOrderId, GroupKey, IsApplicable, IsCompleted,
            CreatedAt, CreatedBy, CreatedByWindows)
    VALUES (src.ProductionOrderId, src.GroupKey, 0, 0,
            GETDATE(), 'Sage_Schnittstelle', SYSTEM_USER);
```

- [ ] **Step 6: Manuell verifizieren**

Datei in Editor öffnen, Top-zu-Bottom durchlesen: Top-MERGE → Folge-MERGE 1 → 2 → 3. Jeder MERGE-Block endet mit `;`. Keine `GO` zwischen den MERGEs (SQL-Agent-Job läuft batch-weise).

- [ ] **Step 7: Commit**

```pwsh
git add SQL/AgentJobs/01_Import_Produktionsauftraege.sql
git commit -m @'
feat(productionorders): agent job eager-creates 3 status rows per new FA

Spec 9. Adds 3 follow-up MERGEs after the existing ProductionOrders MERGE:
PickingStatus (1/FA), BdeStatus (1/FA), AssemblyGroups (5/FA: VK/VL/VE/VT/VA).
All idempotent via NOT MATCHED BY TARGET — never overwrites user data.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
'@
```

---

## Task 4: Repositories — 3 neue + ProductionOrderRepository-Cleanup

**Files:**
- New: `IdealAkeWms/Data/Repositories/IProductionOrderPickingStatusRepository.cs`
- New: `IdealAkeWms/Data/Repositories/ProductionOrderPickingStatusRepository.cs`
- New: `IdealAkeWms/Data/Repositories/IProductionOrderBdeStatusRepository.cs`
- New: `IdealAkeWms/Data/Repositories/ProductionOrderBdeStatusRepository.cs`
- New: `IdealAkeWms/Data/Repositories/IProductionOrderAssemblyGroupRepository.cs`
- New: `IdealAkeWms/Data/Repositories/ProductionOrderAssemblyGroupRepository.cs`
- Modify: `IdealAkeWms/Data/Repositories/IProductionOrderRepository.cs`
- Modify: `IdealAkeWms/Data/Repositories/ProductionOrderRepository.cs`
- Modify: `IdealAkeWms/Program.cs` (DI-Registration)

- [ ] **Step 1: `IProductionOrderPickingStatusRepository.cs`**

```csharp
using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public interface IProductionOrderPickingStatusRepository
{
    Task<ProductionOrderPickingStatus?> GetByProductionOrderIdAsync(int productionOrderId);
    Task<Dictionary<int, ProductionOrderPickingStatus>> GetByProductionOrderIdsAsync(IEnumerable<int> productionOrderIds);
    Task SetFieldAsync(int productionOrderId, string field, bool value, string modifiedBy, string modifiedByWindows);
    Task SetReleaseAsync(int productionOrderId, bool released, int? priority, string? releasedBy, string modifiedBy, string modifiedByWindows);
    Task SetAssignedPickerAsync(int productionOrderId, int? pickerId, string? pickerName, string modifiedBy, string modifiedByWindows);
    Task SetPickingStatusTextAsync(int productionOrderId, string? statusText, string modifiedBy, string modifiedByWindows);
    Task SetPriorityAsync(int productionOrderId, int? priority, string modifiedBy, string modifiedByWindows);
    Task SetCoatingPartsAsync(Dictionary<int, bool> orderIdToHasCoatingParts);
    Task<List<ProductionOrder>> GetReleasedForPickingAsync();
    Task<List<ProductionOrder>> GetReleasedForPickingByPickerAsync(int pickerId);
    Task<int> GetReleasedForPickingCountAsync();
}
```

- [ ] **Step 2: `ProductionOrderPickingStatusRepository.cs`**

```csharp
using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class ProductionOrderPickingStatusRepository : IProductionOrderPickingStatusRepository
{
    private static readonly HashSet<string> ToggleableFields = [
        "HasGlass", "HasExternalPurchase", "IsCoatingDone", "IsDonePicking"
    ];

    private readonly ApplicationDbContext _context;

    public ProductionOrderPickingStatusRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<ProductionOrderPickingStatus?> GetByProductionOrderIdAsync(int productionOrderId)
        => _context.ProductionOrderPickingStatuses
            .FirstOrDefaultAsync(s => s.ProductionOrderId == productionOrderId);

    public async Task<Dictionary<int, ProductionOrderPickingStatus>> GetByProductionOrderIdsAsync(IEnumerable<int> productionOrderIds)
    {
        var ids = productionOrderIds.ToList();
        var rows = await _context.ProductionOrderPickingStatuses
            .Where(s => ids.Contains(s.ProductionOrderId))
            .ToListAsync();
        return rows.ToDictionary(s => s.ProductionOrderId);
    }

    public async Task SetFieldAsync(int productionOrderId, string field, bool value, string modifiedBy, string modifiedByWindows)
    {
        if (!ToggleableFields.Contains(field))
            throw new ArgumentException($"Field '{field}' is not toggleable.", nameof(field));

        var row = await _context.ProductionOrderPickingStatuses
            .FirstOrDefaultAsync(s => s.ProductionOrderId == productionOrderId)
            ?? throw new InvalidOperationException($"PickingStatus row missing for FA {productionOrderId}.");

        switch (field)
        {
            case "HasGlass": row.HasGlass = value; break;
            case "HasExternalPurchase": row.HasExternalPurchase = value; break;
            case "IsCoatingDone": row.IsCoatingDone = value; break;
            case "IsDonePicking": row.IsDonePicking = value; break;
        }

        row.ModifiedAt = DateTime.Now;
        row.ModifiedBy = modifiedBy;
        row.ModifiedByWindows = modifiedByWindows;
        await _context.SaveChangesAsync();
    }

    public async Task SetReleaseAsync(int productionOrderId, bool released, int? priority, string? releasedBy,
        string modifiedBy, string modifiedByWindows)
    {
        var row = await _context.ProductionOrderPickingStatuses
            .FirstOrDefaultAsync(s => s.ProductionOrderId == productionOrderId)
            ?? throw new InvalidOperationException($"PickingStatus row missing for FA {productionOrderId}.");

        row.IsReleasedForPicking = released;
        if (released)
        {
            row.ReleasedAt = DateTime.UtcNow;
            row.ReleasedBy = releasedBy;
            if (priority.HasValue) row.PickingPriority = priority;
        }
        else
        {
            row.AssignedPickerId = null;
            row.AssignedPickerName = null;
        }

        row.ModifiedAt = DateTime.UtcNow;
        row.ModifiedBy = modifiedBy;
        row.ModifiedByWindows = modifiedByWindows;
        await _context.SaveChangesAsync();
    }

    public async Task SetAssignedPickerAsync(int productionOrderId, int? pickerId, string? pickerName,
        string modifiedBy, string modifiedByWindows)
    {
        var row = await _context.ProductionOrderPickingStatuses
            .FirstOrDefaultAsync(s => s.ProductionOrderId == productionOrderId)
            ?? throw new InvalidOperationException($"PickingStatus row missing for FA {productionOrderId}.");

        row.AssignedPickerId = pickerId;
        row.AssignedPickerName = pickerName;
        row.ModifiedAt = DateTime.Now;
        row.ModifiedBy = modifiedBy;
        row.ModifiedByWindows = modifiedByWindows;
        await _context.SaveChangesAsync();
    }

    public async Task SetPickingStatusTextAsync(int productionOrderId, string? statusText,
        string modifiedBy, string modifiedByWindows)
    {
        var row = await _context.ProductionOrderPickingStatuses
            .FirstOrDefaultAsync(s => s.ProductionOrderId == productionOrderId)
            ?? throw new InvalidOperationException($"PickingStatus row missing for FA {productionOrderId}.");

        row.PickingStatus = statusText;
        row.ModifiedAt = DateTime.Now;
        row.ModifiedBy = modifiedBy;
        row.ModifiedByWindows = modifiedByWindows;
        await _context.SaveChangesAsync();
    }

    public async Task SetPriorityAsync(int productionOrderId, int? priority,
        string modifiedBy, string modifiedByWindows)
    {
        var row = await _context.ProductionOrderPickingStatuses
            .FirstOrDefaultAsync(s => s.ProductionOrderId == productionOrderId)
            ?? throw new InvalidOperationException($"PickingStatus row missing for FA {productionOrderId}.");

        row.PickingPriority = priority;
        row.ModifiedAt = DateTime.Now;
        row.ModifiedBy = modifiedBy;
        row.ModifiedByWindows = modifiedByWindows;
        await _context.SaveChangesAsync();
    }

    public async Task SetCoatingPartsAsync(Dictionary<int, bool> orderIdToHasCoatingParts)
    {
        if (orderIdToHasCoatingParts == null || orderIdToHasCoatingParts.Count == 0) return;

        var ids = orderIdToHasCoatingParts.Keys.ToList();
        var rows = await _context.ProductionOrderPickingStatuses
            .Where(s => ids.Contains(s.ProductionOrderId))
            .ToListAsync();

        foreach (var row in rows)
        {
            if (!orderIdToHasCoatingParts.TryGetValue(row.ProductionOrderId, out var newFlag)) continue;

            var changed = row.HasCoatingParts != newFlag;
            row.HasCoatingParts = newFlag;

            // Fallstrick #11: HasCoatingParts → false ⇒ IsCoatingDone reset
            if (!newFlag && row.IsCoatingDone)
            {
                row.IsCoatingDone = false;
                changed = true;
            }

            if (changed)
            {
                row.ModifiedAt = DateTime.Now;
                // ModifiedBy bleibt leer — Sync-Job, kein User
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task<List<ProductionOrder>> GetReleasedForPickingAsync()
    {
        return await _context.ProductionOrderPickingStatuses
            .Where(s => s.IsReleasedForPicking && !s.ProductionOrder.IsDone)
            .OrderBy(s => s.PickingPriority.HasValue ? 0 : 1)
            .ThenBy(s => s.PickingPriority)
            .ThenBy(s => s.ProductionOrder.ProductionDate)
            .Include(s => s.ProductionOrder)
                .ThenInclude(p => p.ProductionWorkplace)
            .Select(s => s.ProductionOrder)
            .ToListAsync();
    }

    public async Task<List<ProductionOrder>> GetReleasedForPickingByPickerAsync(int pickerId)
    {
        return await _context.ProductionOrderPickingStatuses
            .Where(s => s.IsReleasedForPicking && !s.ProductionOrder.IsDone && s.AssignedPickerId == pickerId)
            .OrderBy(s => s.PickingPriority.HasValue ? 0 : 1)
            .ThenBy(s => s.PickingPriority)
            .ThenBy(s => s.ProductionOrder.ProductionDate)
            .Include(s => s.ProductionOrder)
                .ThenInclude(p => p.ProductionWorkplace)
            .Select(s => s.ProductionOrder)
            .ToListAsync();
    }

    public Task<int> GetReleasedForPickingCountAsync()
        => _context.ProductionOrderPickingStatuses
            .CountAsync(s => s.IsReleasedForPicking && !s.ProductionOrder.IsDone);
}
```

- [ ] **Step 3: `IProductionOrderBdeStatusRepository.cs` + Impl**

```csharp
// IProductionOrderBdeStatusRepository.cs
using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public interface IProductionOrderBdeStatusRepository
{
    Task<ProductionOrderBdeStatus?> GetByProductionOrderIdAsync(int productionOrderId);
    Task SetIsDoneBdeAsync(int productionOrderId, bool value, string modifiedBy, string modifiedByWindows);
}
```

```csharp
// ProductionOrderBdeStatusRepository.cs
using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class ProductionOrderBdeStatusRepository : IProductionOrderBdeStatusRepository
{
    private readonly ApplicationDbContext _context;

    public ProductionOrderBdeStatusRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<ProductionOrderBdeStatus?> GetByProductionOrderIdAsync(int productionOrderId)
        => _context.ProductionOrderBdeStatuses
            .FirstOrDefaultAsync(s => s.ProductionOrderId == productionOrderId);

    public async Task SetIsDoneBdeAsync(int productionOrderId, bool value, string modifiedBy, string modifiedByWindows)
    {
        var row = await _context.ProductionOrderBdeStatuses
            .FirstOrDefaultAsync(s => s.ProductionOrderId == productionOrderId)
            ?? throw new InvalidOperationException($"BdeStatus row missing for FA {productionOrderId}.");

        row.IsDoneBde = value;
        row.ModifiedAt = DateTime.Now;
        row.ModifiedBy = modifiedBy;
        row.ModifiedByWindows = modifiedByWindows;
        await _context.SaveChangesAsync();
    }
}
```

- [ ] **Step 4: `IProductionOrderAssemblyGroupRepository.cs` + Impl**

```csharp
// IProductionOrderAssemblyGroupRepository.cs
using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public interface IProductionOrderAssemblyGroupRepository
{
    Task<List<ProductionOrderAssemblyGroup>> GetByProductionOrderIdAsync(int productionOrderId);
    Task<ProductionOrderAssemblyGroup?> GetByPoAndKeyAsync(int productionOrderId, string groupKey);

    /// <summary>Pivot fuer Index-View: orderId → (groupKey → isApplicable). Spec 7.3.</summary>
    Task<Dictionary<int, Dictionary<string, bool>>> GetIsApplicablePivotAsync(IEnumerable<int> productionOrderIds);

    Task SetIsApplicableAsync(int productionOrderId, string groupKey, bool value, string modifiedBy, string modifiedByWindows);
}
```

```csharp
// ProductionOrderAssemblyGroupRepository.cs
using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class ProductionOrderAssemblyGroupRepository : IProductionOrderAssemblyGroupRepository
{
    private static readonly HashSet<string> AllowedGroupKeys = ["VK", "VL", "VE", "VT", "VA"];

    private readonly ApplicationDbContext _context;

    public ProductionOrderAssemblyGroupRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProductionOrderAssemblyGroup>> GetByProductionOrderIdAsync(int productionOrderId)
        => await _context.ProductionOrderAssemblyGroups
            .Where(g => g.ProductionOrderId == productionOrderId)
            .ToListAsync();

    public Task<ProductionOrderAssemblyGroup?> GetByPoAndKeyAsync(int productionOrderId, string groupKey)
        => _context.ProductionOrderAssemblyGroups
            .FirstOrDefaultAsync(g => g.ProductionOrderId == productionOrderId && g.GroupKey == groupKey);

    public async Task<Dictionary<int, Dictionary<string, bool>>> GetIsApplicablePivotAsync(IEnumerable<int> productionOrderIds)
    {
        var ids = productionOrderIds.ToList();
        if (ids.Count == 0) return new Dictionary<int, Dictionary<string, bool>>();

        var rows = await _context.ProductionOrderAssemblyGroups
            .Where(g => ids.Contains(g.ProductionOrderId))
            .Select(g => new { g.ProductionOrderId, g.GroupKey, g.IsApplicable })
            .ToListAsync();

        return rows
            .GroupBy(r => r.ProductionOrderId)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(r => r.GroupKey, r => r.IsApplicable));
    }

    public async Task SetIsApplicableAsync(int productionOrderId, string groupKey, bool value,
        string modifiedBy, string modifiedByWindows)
    {
        if (!AllowedGroupKeys.Contains(groupKey))
            throw new ArgumentException($"GroupKey '{groupKey}' is not in whitelist.", nameof(groupKey));

        var row = await _context.ProductionOrderAssemblyGroups
            .FirstOrDefaultAsync(g => g.ProductionOrderId == productionOrderId && g.GroupKey == groupKey)
            ?? throw new InvalidOperationException($"AssemblyGroup row missing for FA {productionOrderId} / {groupKey}.");

        row.IsApplicable = value;
        row.ModifiedAt = DateTime.Now;
        row.ModifiedBy = modifiedBy;
        row.ModifiedByWindows = modifiedByWindows;
        await _context.SaveChangesAsync();
    }
}
```

- [ ] **Step 5: `IProductionOrderRepository.cs` und `ProductionOrderRepository.cs` bereinigen**

In `IProductionOrderRepository.cs`: Folgende Methoden ENTFERNEN (verschoben):
- `Task<List<ProductionOrder>> GetReleasedForPickingAsync();`
- `Task<List<ProductionOrder>> GetReleasedForPickingByPickerAsync(int pickerId);`
- `Task<int> GetReleasedForPickingCountAsync();`
- `Task SetCoatingFlagsAsync(Dictionary<int, bool> orderIdToHasCoatingParts);`

In `ProductionOrderRepository.cs`: gleiche Methoden-Bodies ENTFERNEN. Übrig bleiben `GetAllOrderedAsync`, `GetOpenOrdersAsync`, `GetByOrderNumberAsync`, `SearchAsync`, `GetByArticleNumbersAsync`, `GetOpenOrdersInWindowAsync` und die Repository-Basis-CRUD.

- [ ] **Step 6: DI-Registration in `Program.cs`**

In `IdealAkeWms/Program.cs` neben der bestehenden `services.AddScoped<IProductionOrderRepository, ProductionOrderRepository>();` einfügen:

```csharp
builder.Services.AddScoped<IProductionOrderPickingStatusRepository, ProductionOrderPickingStatusRepository>();
builder.Services.AddScoped<IProductionOrderBdeStatusRepository, ProductionOrderBdeStatusRepository>();
builder.Services.AddScoped<IProductionOrderAssemblyGroupRepository, ProductionOrderAssemblyGroupRepository>();
```

Konsumenten der entfernten Methoden (`CoatingDetectionService`?, `ProductionOrdersController`, `PickingController`) sind hier noch ROT — werden in Task 5 + 6 repariert.

- [ ] **Step 7: Konsumenten von `SetCoatingFlagsAsync` finden + umstellen**

```pwsh
# Find CoatingDetectionService oder andere caller
```

Mit Grep nach `SetCoatingFlagsAsync` suchen. Jeden Aufruf umstellen auf `_pickingStatusRepository.SetCoatingPartsAsync(...)`. Falls Konstruktor-DI nicht passt, Konstruktor des betroffenen Services erweitern.

- [ ] **Step 8: Build (immer noch ROT erwartet — Controllers + View)**

```pwsh
dotnet build --nologo
```

Erwartete verbleibende Fehler nur in Controllers + Views. Wenn weitere Fehler in Services auftauchen: pro Service `SetCoatingFlagsAsync` → `SetCoatingPartsAsync` umstellen.

- [ ] **Step 9: Commit**

```pwsh
git add IdealAkeWms/Data/Repositories/ IdealAkeWms/Program.cs
git commit -m @'
refactor(productionorders): 3 new status repositories + cleanup

Spec 7. Adds IProductionOrderPickingStatusRepository (release/picker/coating
mutations + pivot lookups), IProductionOrderBdeStatusRepository,
IProductionOrderAssemblyGroupRepository (pivot for index view).

Removes GetReleasedForPicking*, SetCoatingFlagsAsync from
IProductionOrderRepository — those now live on PickingStatusRepository.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
'@
```

---

## Task 5: API-Controllers — 3 neue Endpoints, alter entfernt

**Files:**
- New: `IdealAkeWms/Controllers/PickingStatusApiController.cs`
- New: `IdealAkeWms/Controllers/AssemblyGroupsApiController.cs`
- New: `IdealAkeWms/Controllers/BdeStatusApiController.cs`
- Modify: `IdealAkeWms/Controllers/ProductionOrdersApiController.cs` (ToggleField entfernen, Search bleibt)

- [ ] **Step 1: `PickingStatusApiController.cs`**

```csharp
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers;

[Route("api/picking-status")]
[ApiController]
[RequirePickingAccess]
public class PickingStatusApiController : ControllerBase
{
    private static readonly HashSet<string> AllowedFields = [
        "HasGlass", "HasExternalPurchase", "IsCoatingDone", "IsDonePicking"
    ];

    private readonly IProductionOrderPickingStatusRepository _pickingStatus;
    private readonly IProductionOrderRepository _productionOrders;
    private readonly ICurrentUserService _currentUser;

    public PickingStatusApiController(
        IProductionOrderPickingStatusRepository pickingStatus,
        IProductionOrderRepository productionOrders,
        ICurrentUserService currentUser)
    {
        _pickingStatus = pickingStatus;
        _productionOrders = productionOrders;
        _currentUser = currentUser;
    }

    [HttpPost("toggle")]
    public async Task<IActionResult> Toggle([FromBody] PickingStatusToggleRequest req)
    {
        if (!AllowedFields.Contains(req.Field))
            return BadRequest($"Ungültiges Feld: {req.Field}");

        var order = await _productionOrders.GetByIdAsync(req.ProductionOrderId);
        if (order == null) return NotFound();

        var row = await _pickingStatus.GetByProductionOrderIdAsync(req.ProductionOrderId);
        if (row == null) return NotFound("PickingStatus-Zeile fehlt (sollte durch AgentJob eager-created sein).");

        await _pickingStatus.SetFieldAsync(
            req.ProductionOrderId, req.Field, req.Value,
            _currentUser.GetDisplayName(), _currentUser.GetWindowsUserName());

        return Ok();
    }
}

public class PickingStatusToggleRequest
{
    public int ProductionOrderId { get; set; }
    public string Field { get; set; } = string.Empty;
    public bool Value { get; set; }
}
```

- [ ] **Step 2: `AssemblyGroupsApiController.cs`**

```csharp
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers;

[Route("api/assembly-groups")]
[ApiController]
[RequirePickingAccess]
public class AssemblyGroupsApiController : ControllerBase
{
    private static readonly HashSet<string> AllowedGroupKeys = ["VK", "VL", "VE", "VT", "VA"];

    private readonly IProductionOrderAssemblyGroupRepository _groups;
    private readonly IProductionOrderRepository _productionOrders;
    private readonly ICurrentUserService _currentUser;

    public AssemblyGroupsApiController(
        IProductionOrderAssemblyGroupRepository groups,
        IProductionOrderRepository productionOrders,
        ICurrentUserService currentUser)
    {
        _groups = groups;
        _productionOrders = productionOrders;
        _currentUser = currentUser;
    }

    [HttpPost("toggle-applicable")]
    public async Task<IActionResult> ToggleApplicable([FromBody] AssemblyGroupToggleRequest req)
    {
        if (!AllowedGroupKeys.Contains(req.GroupKey))
            return BadRequest($"Ungültiger GroupKey: {req.GroupKey}");

        var order = await _productionOrders.GetByIdAsync(req.ProductionOrderId);
        if (order == null) return NotFound();

        var row = await _groups.GetByPoAndKeyAsync(req.ProductionOrderId, req.GroupKey);
        if (row == null) return NotFound("AssemblyGroup-Zeile fehlt (sollte durch AgentJob eager-created sein).");

        await _groups.SetIsApplicableAsync(
            req.ProductionOrderId, req.GroupKey, req.Value,
            _currentUser.GetDisplayName(), _currentUser.GetWindowsUserName());

        return Ok();
    }
}

public class AssemblyGroupToggleRequest
{
    public int ProductionOrderId { get; set; }
    public string GroupKey { get; set; } = string.Empty;
    public bool Value { get; set; }
}
```

- [ ] **Step 3: `BdeStatusApiController.cs`**

```csharp
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers;

[Route("api/bde-status")]
[ApiController]
[RequirePickingAccess]
public class BdeStatusApiController : ControllerBase
{
    private static readonly HashSet<string> AllowedFields = ["IsDoneBde"];

    private readonly IProductionOrderBdeStatusRepository _bdeStatus;
    private readonly IProductionOrderRepository _productionOrders;
    private readonly ICurrentUserService _currentUser;

    public BdeStatusApiController(
        IProductionOrderBdeStatusRepository bdeStatus,
        IProductionOrderRepository productionOrders,
        ICurrentUserService currentUser)
    {
        _bdeStatus = bdeStatus;
        _productionOrders = productionOrders;
        _currentUser = currentUser;
    }

    [HttpPost("toggle")]
    public async Task<IActionResult> Toggle([FromBody] BdeStatusToggleRequest req)
    {
        if (!AllowedFields.Contains(req.Field))
            return BadRequest($"Ungültiges Feld: {req.Field}");

        var order = await _productionOrders.GetByIdAsync(req.ProductionOrderId);
        if (order == null) return NotFound();

        var row = await _bdeStatus.GetByProductionOrderIdAsync(req.ProductionOrderId);
        if (row == null) return NotFound("BdeStatus-Zeile fehlt.");

        await _bdeStatus.SetIsDoneBdeAsync(
            req.ProductionOrderId, req.Value,
            _currentUser.GetDisplayName(), _currentUser.GetWindowsUserName());

        return Ok();
    }
}

public class BdeStatusToggleRequest
{
    public int ProductionOrderId { get; set; }
    public string Field { get; set; } = string.Empty;
    public bool Value { get; set; }
}
```

- [ ] **Step 4: `ProductionOrdersApiController.ToggleField` entfernen**

In `IdealAkeWms/Controllers/ProductionOrdersApiController.cs`:
- Den gesamten `[HttpPost("toggle-field")] ToggleField`-Block (Zeilen 40-69) ENTFERNEN.
- Die Klasse `ToggleFieldRequest` (Zeilen 72-77) ENTFERNEN.
- Das statische `AllowedToggleFields`-HashSet (Zeilen 12-15) ENTFERNEN.

Die `Search`-Action (Zeilen 24-38) BLEIBT unverändert.

Resultat:

```csharp
using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;

namespace IdealAkeWms.Controllers;

[Route("api/productionorders")]
[ApiController]
[RequirePickingAccess]
public class ProductionOrdersApiController : ControllerBase
{
    private readonly IProductionOrderRepository _repository;

    public ProductionOrdersApiController(IProductionOrderRepository repository)
    {
        _repository = repository;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string? q, [FromQuery] int limit = 20)
    {
        var orders = await _repository.SearchAsync(q, limit);

        var result = orders.Select(o => new
        {
            id = o.Id,
            text = string.IsNullOrEmpty(o.ArticleNumber)
                ? $"{o.OrderNumber} - {o.Customer}"
                : $"{o.OrderNumber} - {o.ArticleNumber} - {o.Customer}"
        });

        return Ok(result);
    }
}
```

- [ ] **Step 5: Build (Controller-Files sollten jetzt grün sein)**

```pwsh
dotnet build --nologo
```

Erwartet: API-Controller-Files compilen. `ProductionOrdersController` + `PickingController` immer noch rot (kommt in Task 6).

- [ ] **Step 6: Commit**

```pwsh
git add IdealAkeWms/Controllers/PickingStatusApiController.cs IdealAkeWms/Controllers/AssemblyGroupsApiController.cs IdealAkeWms/Controllers/BdeStatusApiController.cs IdealAkeWms/Controllers/ProductionOrdersApiController.cs
git commit -m @'
feat(productionorders): split toggle api into 3 endpoints

Spec 6. /api/picking-status/toggle handles HasGlass/HasExternalPurchase/
IsCoatingDone/IsDonePicking. /api/assembly-groups/toggle-applicable handles
VK/VL/VE/VT/VA. /api/bde-status/toggle handles IsDoneBde. Old
/api/productionorders/toggle-field removed (atomic cutover).

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
'@
```

---

## Task 6: Controllers + Inline-Mapping — ProductionOrdersController + PickingController

**Files:**
- Modify: `IdealAkeWms/Controllers/ProductionOrdersController.cs` (Index-Mapping ca. Zeile 281-333, ToggleRelease/BulkRelease/SetPriority Zeile 39-200)
- Modify: `IdealAkeWms/Controllers/PickingController.cs` (Index-Mapping Zeile 91-115, SetPickingStatus Zeile 357-373)
- Modify: `IdealAkeWms/Models/ViewModels/PickingListItem.cs` (falls notwendig)

Konstruktor von beiden Controllern bekommt `IProductionOrderPickingStatusRepository`, `IProductionOrderAssemblyGroupRepository` zusätzlich injiziert.

- [ ] **Step 1: `ProductionOrdersController` Konstruktor erweitern**

In `IdealAkeWms/Controllers/ProductionOrdersController.cs` Konstruktor (ca. Zeile 18-37) drei neue Konstruktor-Parameter und Felder hinzufügen:

```csharp
private readonly IProductionOrderPickingStatusRepository _pickingStatusRepository;
private readonly IProductionOrderAssemblyGroupRepository _assemblyGroupRepository;
// ... bestehende Felder

public ProductionOrdersController(
    IProductionOrderRepository productionOrderRepository,
    IProductionOrderPickingStatusRepository pickingStatusRepository,
    IProductionOrderAssemblyGroupRepository assemblyGroupRepository,
    /* ... bestehende Parameter */)
{
    _productionOrderRepository = productionOrderRepository;
    _pickingStatusRepository = pickingStatusRepository;
    _assemblyGroupRepository = assemblyGroupRepository;
    // ... bestehende Assignments
}
```

- [ ] **Step 2: `Index`-Action — Pivot-Lookups + Mapping umstellen**

Vor dem `.Select(o => ...)`-Block (Zeile 281) zwei Bulk-Lookups einfügen:

```csharp
var orderIds = orders.Select(o => o.Id).ToList();
var groupPivot = await _assemblyGroupRepository.GetIsApplicablePivotAsync(orderIds);
var pickingStatuses = await _pickingStatusRepository.GetByProductionOrderIdsAsync(orderIds);
```

Das `.Select(o => ...)`-Mapping (Zeile 281-333) komplett ersetzen:

```csharp
var viewItems = orders.Select(o =>
{
    var ps = pickingStatuses.GetValueOrDefault(o.Id);
    var grp = groupPivot.GetValueOrDefault(o.Id) ?? new Dictionary<string, bool>();

    var item = new ProductionOrderViewItem
    {
        Id = o.Id,
        OrderNumber = o.OrderNumber,
        Quantity = o.Quantity,
        Customer = o.Customer,
        ArticleNumber = o.ArticleNumber,
        Description1 = o.Description1,
        Description2 = o.Description2,
        ProductionDate = o.ProductionDate,
        DeliveryDate = o.DeliveryDate,
        IsDone = o.IsDone,
        PickingStatus = ps?.PickingStatus,
        HasGlass = ps?.HasGlass ?? false,
        HasExternalPurchase = ps?.HasExternalPurchase ?? false,
        HasCooling = grp.GetValueOrDefault("VK"),
        HasFan = grp.GetValueOrDefault("VL"),
        HasElectric = grp.GetValueOrDefault("VE"),
        HasDoors = grp.GetValueOrDefault("VT"),
        HasSuperstructure = grp.GetValueOrDefault("VA"),
        HasCoatingParts = ps?.HasCoatingParts ?? false,
        IsCoatingDone = ps?.IsCoatingDone ?? false,
        WorkplaceName = o.ProductionWorkplace?.Name,
        IsReleasedForPicking = ps?.IsReleasedForPicking ?? false,
        PickingPriority = ps?.PickingPriority,
        ReleasedAt = ps?.ReleasedAt,
        ReleasedBy = ps?.ReleasedBy,
        AssignedPickerId = ps?.AssignedPickerId,
        AssignedPickerName = ps?.AssignedPickerName
    };

    // BeschichtungTermin-Block unveraendert (HasCoatingParts kommt aus ps)
    if (o.ProductionDate.HasValue)
    {
        item.KommissionierTermin = _businessDayService.SubtractBusinessDays(
            o.ProductionDate.Value, kommissionierTage, holidays);
        item.VorkommissionierTermin = _businessDayService.SubtractBusinessDays(
            item.KommissionierTermin.Value, vorkommissionierTage, holidays);
        if (!coatingFeatureActive || (ps?.HasCoatingParts ?? false))
        {
            var rawBeschichtung = _businessDayService.SubtractBusinessDays(
                item.VorkommissionierTermin.Value, beschichtungTage, holidays);
            item.BeschichtungTermin = _businessDayService.FindPreviousPickupDay(rawBeschichtung, pickupDays);
        }
    }

    return item;
}).ToList();
```

- [ ] **Step 3: `ToggleRelease`-Action (Zeile 39-101) auf neue Repos umstellen**

Komplett ersetzen:

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
[RequireLeitstandAccess]
public async Task<IActionResult> ToggleRelease(int id, int? assignedPickerId, string? returnUrl)
{
    var order = await _productionOrderRepository.GetByIdAsync(id);
    if (order == null) return NotFound();

    var ps = await _pickingStatusRepository.GetByProductionOrderIdAsync(id);
    if (ps == null) return NotFound("PickingStatus-Zeile fehlt.");

    if (!ps.IsReleasedForPicking && string.IsNullOrEmpty(order.ArticleNumber))
    {
        TempData["WarningMessage"] = $"FA {order.OrderNumber} kann nicht freigegeben werden — keine Artikelnummer vorhanden.";
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
        return RedirectToAction(nameof(Index));
    }

    var pickerAssignmentEnabled = (await _settingRepository.GetValueAsync(AppSettingKeys.KommissionierungMitZuweisung))
        ?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

    if (!ps.IsReleasedForPicking && pickerAssignmentEnabled && !assignedPickerId.HasValue)
    {
        TempData["WarningMessage"] = "Bitte einen Kommissionierer zuweisen.";
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
        return RedirectToAction(nameof(Index));
    }

    var newReleased = !ps.IsReleasedForPicking;
    int? newPriority = ps.PickingPriority;
    if (newReleased && !newPriority.HasValue)
    {
        var existing = await _pickingStatusRepository.GetReleasedForPickingAsync();
        var maxPrio = existing
            .Where(o => o.Id != id)
            .Select(o => o.PickingStatus?.PickingPriority ?? 0)
            .DefaultIfEmpty(0)
            .Max();
        newPriority = maxPrio + 1;
    }

    await _pickingStatusRepository.SetReleaseAsync(
        id, newReleased, newPriority, _currentUserService.GetDisplayName(),
        _currentUserService.GetDisplayName(), _currentUserService.GetWindowsUserName());

    if (newReleased && assignedPickerId.HasValue)
    {
        var picker = await _userRepository.GetByIdAsync(assignedPickerId.Value);
        await _pickingStatusRepository.SetAssignedPickerAsync(
            id, assignedPickerId, picker?.Name,
            _currentUserService.GetDisplayName(), _currentUserService.GetWindowsUserName());
    }

    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
    return RedirectToAction(nameof(Index));
}
```

HINWEIS: `GetReleasedForPickingAsync()` gibt `List<ProductionOrder>` zurück, mit `Include(p => p.PickingStatus)`. Zugriff auf Priority erfolgt also über `o.PickingStatus?.PickingPriority`.

- [ ] **Step 4: `BulkRelease`-Action analog umstellen**

Das `foreach (var id in ids)` (ca. Zeile 147+) ersetzt Direkt-Setzer auf `order.IsReleasedForPicking` etc. durch:

```csharp
foreach (var id in ids)
{
    var order = await _productionOrderRepository.GetByIdAsync(id);
    if (order == null) continue;

    var ps = await _pickingStatusRepository.GetByProductionOrderIdAsync(id);
    if (ps == null) continue;

    if (release && string.IsNullOrEmpty(order.ArticleNumber))
    {
        skipped.Add(order.OrderNumber);
        continue;
    }

    int? newPriority = release ? (ps.PickingPriority ?? (++maxPrio)) : ps.PickingPriority;
    await _pickingStatusRepository.SetReleaseAsync(
        id, release, newPriority, displayName, displayName, windowsUser);

    if (release && assignedPickerId.HasValue)
    {
        await _pickingStatusRepository.SetAssignedPickerAsync(
            id, assignedPickerId, pickerName, displayName, windowsUser);
    }
    processed++;
}
```

Rest der `BulkRelease`-Action (Skipped-Logging, TempData-Message, Redirect) unverändert.

- [ ] **Step 5: `SetPriority`-Action umstellen**

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
[RequireLeitstandAccess]
public async Task<IActionResult> SetPriority(int id, int? priority)
{
    var order = await _productionOrderRepository.GetByIdAsync(id);
    if (order == null) return NotFound();

    await _pickingStatusRepository.SetPriorityAsync(
        id, priority,
        _currentUserService.GetDisplayName(), _currentUserService.GetWindowsUserName());
    return Ok();
}
```

- [ ] **Step 6: `PickingController` Konstruktor + Index-Mapping**

In `IdealAkeWms/Controllers/PickingController.cs` Konstruktor um `IProductionOrderPickingStatusRepository _pickingStatusRepository` erweitern und DI registrieren.

Im `Index`-Action-Body (ca. Zeile 75-122) den Call:

```csharp
releasedOrders = await _productionOrderRepository.GetReleasedForPickingByPickerAsync(currentUserId.Value);
// bzw.
releasedOrders = await _productionOrderRepository.GetReleasedForPickingAsync();
```

ersetzen durch:

```csharp
releasedOrders = await _pickingStatusRepository.GetReleasedForPickingByPickerAsync(currentUserId.Value);
// bzw.
releasedOrders = await _pickingStatusRepository.GetReleasedForPickingAsync();
```

Das Mapping (Zeile 91-115) auf neue Nav-Properties umstellen:

```csharp
var items = releasedOrders.Select(o =>
{
    var ps = o.PickingStatus;  // Nav-Property (Include() im Repo)
    var item = new PickingListItem
    {
        Id = o.Id,
        PickingPriority = ps?.PickingPriority,
        OrderNumber = o.OrderNumber,
        ArticleNumber = o.ArticleNumber,
        Description1 = o.Description1,
        Customer = o.Customer,
        Quantity = o.Quantity,
        ProductionDate = o.ProductionDate,
        PickingStatus = ps?.PickingStatus,
        AssignedPickerId = ps?.AssignedPickerId,
        AssignedPickerName = ps?.AssignedPickerName
    };

    if (o.ProductionDate.HasValue)
    {
        item.KommissionierTermin = _businessDayService.SubtractBusinessDays(
            o.ProductionDate.Value, kommissionierTage, holidays);
    }

    return item;
}).ToList();
```

- [ ] **Step 7: `PickingController.SetPickingStatus` umstellen**

Ersetzen (Zeile 354-376):

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
[RequirePickingAccess]
public async Task<IActionResult> SetPickingStatus(int productionOrderId, string status)
{
    var order = await _productionOrderRepository.GetByIdAsync(productionOrderId);
    if (order == null) return NotFound();

    await _pickingStatusRepository.SetPickingStatusTextAsync(
        productionOrderId, status,
        _currentUserService.GetDisplayName(), _currentUserService.GetWindowsUserName());

    // Kommissionierung abgeschlossen → FA automatisch erledigt setzen (auf Sage-Master)
    if (status == "abgeschlossen")
    {
        order.IsDone = true;
        order.ModifiedAt = DateTime.Now;
        order.ModifiedBy = _currentUserService.GetDisplayName();
        order.ModifiedByWindows = _currentUserService.GetWindowsUserName();
        await _productionOrderRepository.UpdateAsync(order);
    }

    return Ok();
}
```

- [ ] **Step 8: `ProductionOrderViewItem` und `PickingListItem` prüfen**

In `IdealAkeWms/Models/ViewModels/ProductionOrderViewModel.cs` und `IdealAkeWms/Models/ViewModels/PickingListItem.cs` sicherstellen, dass alle in den Mapping-Blocks oben verwendeten Properties (HasGlass, HasCooling, IsReleasedForPicking, AssignedPickerId, etc.) noch deklariert sind. **Strukturell bleiben die ViewModels gleich** — nur die Datenherkunft im Controller ändert sich (Spec 7.3, 10.1).

- [ ] **Step 9: Build (jetzt sollte alles im Web-Projekt grün sein)**

```pwsh
dotnet build --nologo
```

Erwartet: 0 errors im Web-Projekt. Tests-Projekt noch rot (Setup-Daten kaputt — Task 8).

- [ ] **Step 10: Commit**

```pwsh
git add IdealAkeWms/Controllers/ProductionOrdersController.cs IdealAkeWms/Controllers/PickingController.cs IdealAkeWms/Models/ViewModels/
git commit -m @'
refactor(productionorders): controllers consume new status repositories

Spec 10.2-10.5. ProductionOrdersController.Index uses pivot lookups.
ToggleRelease/BulkRelease/SetPriority write to ProductionOrderPickingStatus
via repository. PickingController.Index includes PickingStatus nav prop and
maps from it. SetPickingStatus persists to PickingStatus row, keeps IsDone
auto-flip on Sage master.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
'@
```

---

## Task 7: View + JS Dispatcher

**Files:**
- Modify: `IdealAkeWms/Views/ProductionOrders/Index.cshtml` (Checkbox-Markup ca. Zeile 221-256, JS-Handler ca. Zeile 548-567)

- [ ] **Step 1: Checkbox-Markup um `data-endpoint` erweitern**

In `IdealAkeWms/Views/ProductionOrders/Index.cshtml` Zeile 222-256. Jede Checkbox bekommt `data-endpoint`, AssemblyGroup-Checkboxes zusätzlich `data-group-key`:

```cshtml
<td class="text-center">
    @if (item.HasCoatingParts)
    {
        <input type="checkbox" class="form-check-input toggle-field"
               data-id="@item.Id"
               data-endpoint="/api/picking-status/toggle"
               data-field="IsCoatingDone"
               title="Lackierteile erledigt"
               @(item.IsCoatingDone ? "checked" : "") @(!Model.CanPick ? "disabled" : "") />
    }
</td>
<td class="text-center">
    <input type="checkbox" class="form-check-input toggle-field"
           data-id="@item.Id"
           data-endpoint="/api/picking-status/toggle"
           data-field="HasGlass"
           @(item.HasGlass ? "checked" : "") @(!Model.CanPick ? "disabled" : "") />
</td>
<td class="text-center">
    <input type="checkbox" class="form-check-input toggle-field"
           data-id="@item.Id"
           data-endpoint="/api/picking-status/toggle"
           data-field="HasExternalPurchase"
           @(item.HasExternalPurchase ? "checked" : "") @(!Model.CanPick ? "disabled" : "") />
</td>
<td class="text-center">
    <input type="checkbox" class="form-check-input toggle-field"
           data-id="@item.Id"
           data-endpoint="/api/assembly-groups/toggle-applicable"
           data-group-key="VK"
           title="VK Kälte" @(item.HasCooling ? "checked" : "") @(!Model.CanPick ? "disabled" : "") />
</td>
<td class="text-center">
    <input type="checkbox" class="form-check-input toggle-field"
           data-id="@item.Id"
           data-endpoint="/api/assembly-groups/toggle-applicable"
           data-group-key="VL"
           title="VL Lüfter" @(item.HasFan ? "checked" : "") @(!Model.CanPick ? "disabled" : "") />
</td>
<td class="text-center">
    <input type="checkbox" class="form-check-input toggle-field"
           data-id="@item.Id"
           data-endpoint="/api/assembly-groups/toggle-applicable"
           data-group-key="VE"
           title="VE Elektro" @(item.HasElectric ? "checked" : "") @(!Model.CanPick ? "disabled" : "") />
</td>
<td class="text-center">
    <input type="checkbox" class="form-check-input toggle-field"
           data-id="@item.Id"
           data-endpoint="/api/assembly-groups/toggle-applicable"
           data-group-key="VT"
           title="VT Türen" @(item.HasDoors ? "checked" : "") @(!Model.CanPick ? "disabled" : "") />
</td>
<td class="text-center">
    <input type="checkbox" class="form-check-input toggle-field"
           data-id="@item.Id"
           data-endpoint="/api/assembly-groups/toggle-applicable"
           data-group-key="VA"
           title="VA Aufbau" @(item.HasSuperstructure ? "checked" : "") @(!Model.CanPick ? "disabled" : "") />
</td>
```

- [ ] **Step 2: Inline-JS-Handler durch Dispatcher ersetzen**

In derselben Datei Zeile 547-567 — den `document.querySelectorAll('.toggle-field')...`-Block ersetzen:

```javascript
// Toggle-Field per AJAX — dispatcht ueber data-endpoint
document.querySelectorAll('.toggle-field').forEach(function (cb) {
    cb.addEventListener('change', function () {
        var id = parseInt(this.getAttribute('data-id'));
        var endpoint = this.getAttribute('data-endpoint');
        var field = this.getAttribute('data-field');
        var groupKey = this.getAttribute('data-group-key');
        var value = this.checked;

        var body;
        if (endpoint === '/api/assembly-groups/toggle-applicable') {
            body = { productionOrderId: id, groupKey: groupKey, value: value };
        } else if (endpoint === '/api/picking-status/toggle' || endpoint === '/api/bde-status/toggle') {
            body = { productionOrderId: id, field: field, value: value };
        } else {
            console.error('Unknown toggle-field endpoint:', endpoint);
            cb.checked = !value;
            return;
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

- [ ] **Step 3: Visuelle Konsistenz-Pruefung (manuell)**

Anzahl Checkbox-Spalten (Lackier-T, Glas, Zukauf, VK, VL, VE, VT, VA) → 8. Jede Checkbox hat `data-endpoint`. AssemblyGroups haben zusätzlich `data-group-key`. Keine alte `data-field="HasCooling"` mehr (die wurden zu `data-group-key="VK"`).

```pwsh
# Sanity grep im View-File
```

Suche im File nach `data-endpoint=` — sollte 8x finden.
Suche nach `data-group-key=` — sollte 5x finden (VK/VL/VE/VT/VA).
Suche nach `'/api/productionorders/toggle-field'` — sollte 0x finden (alter Endpoint weg).

- [ ] **Step 4: Build + dotnet test**

```pwsh
dotnet build --nologo
dotnet test --nologo --no-build
```

Erwartet: Build grün. Tests immer noch rot (Task 8 reparieren).

- [ ] **Step 5: Commit**

```pwsh
git add IdealAkeWms/Views/ProductionOrders/Index.cshtml
git commit -m @'
feat(productionorders): view toggle dispatcher routes to 3 endpoints

Spec 6.3, 10.3. Checkboxes carry data-endpoint and (for assembly groups)
data-group-key. JS handler reads attributes and builds the correct body
payload per endpoint. Old /api/productionorders/toggle-field call removed
in the same diff (atomic cutover).

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
'@
```

---

## Task 8: Tests — neue Repo + API Tests + bestehende migrieren

**Files:**
- New: `IdealAkeWms.Tests/Repositories/ProductionOrderPickingStatusRepositoryTests.cs`
- New: `IdealAkeWms.Tests/Repositories/ProductionOrderBdeStatusRepositoryTests.cs`
- New: `IdealAkeWms.Tests/Repositories/ProductionOrderAssemblyGroupRepositoryTests.cs`
- New: `IdealAkeWms.Tests/Controllers/PickingStatusApiControllerTests.cs`
- New: `IdealAkeWms.Tests/Controllers/AssemblyGroupsApiControllerTests.cs`
- New: `IdealAkeWms.Tests/Controllers/BdeStatusApiControllerTests.cs`
- New: `IdealAkeWms.Tests/Integration/ProductionOrderEagerCreateAgentJobTests.cs` (SqlServerOnly)
- Modify: `IdealAkeWms.Tests/Controllers/ProductionOrdersControllerPickerTests.cs`
- Modify: `IdealAkeWms.Tests/Controllers/PickingApiControllerTests.cs`
- Modify: weitere ~20 vorhandene Tests (siehe Spec 11.4)

- [ ] **Step 1: Bestehende kaputte Tests identifizieren**

```pwsh
dotnet test --nologo --no-build
```

Output durchforsten — erwartet ~25 Tests rot (Setup-Daten setzen `order.HasGlass = true` etc., was nicht mehr compilet). Liste sammeln, dann gruppenweise abarbeiten.

- [ ] **Step 2: Helper für Setup — `TestDataHelper.CreateOrderWithStatuses`**

In `IdealAkeWms.Tests/Helpers/` (oder neu anlegen) eine Helper-Funktion einführen:

```csharp
public static class TestDataHelper
{
    public static (ProductionOrder Order, ProductionOrderPickingStatus PickingStatus,
                   ProductionOrderBdeStatus BdeStatus, List<ProductionOrderAssemblyGroup> Groups)
        CreateOrderWithStatuses(ApplicationDbContext context, string orderNumber, decimal qty = 1, bool releaseForPicking = false)
    {
        var order = new ProductionOrder
        {
            OrderNumber = orderNumber, Quantity = qty,
            CreatedBy = "test", CreatedByWindows = "test"
        };
        context.ProductionOrders.Add(order);
        context.SaveChanges();

        var ps = new ProductionOrderPickingStatus
        {
            ProductionOrderId = order.Id,
            IsReleasedForPicking = releaseForPicking,
            CreatedBy = "test", CreatedByWindows = "test"
        };
        var bde = new ProductionOrderBdeStatus
        {
            ProductionOrderId = order.Id,
            CreatedBy = "test", CreatedByWindows = "test"
        };
        var groups = new[] { "VK", "VL", "VE", "VT", "VA" }
            .Select(k => new ProductionOrderAssemblyGroup
            {
                ProductionOrderId = order.Id, GroupKey = k,
                CreatedBy = "test", CreatedByWindows = "test"
            }).ToList();

        context.ProductionOrderPickingStatuses.Add(ps);
        context.ProductionOrderBdeStatuses.Add(bde);
        context.ProductionOrderAssemblyGroups.AddRange(groups);
        context.SaveChanges();

        return (order, ps, bde, groups);
    }
}
```

- [ ] **Step 3: `ProductionOrderPickingStatusRepositoryTests.cs`**

Tests nach Spec 11.1:

```csharp
public class ProductionOrderPickingStatusRepositoryTests
{
    [Fact]
    public async Task SetFieldAsync_HasGlass_PersistsValue_AndAuditFields() { /* ... */ }

    [Fact]
    public async Task SetFieldAsync_UnknownField_ThrowsArgumentException() { /* ... */ }

    [Fact]
    public async Task SetCoatingPartsAsync_FlipsToFalse_ResetsIsCoatingDone() { /* Fallstrick #11 */ }

    [Fact]
    public async Task GetReleasedForPickingAsync_OrdersByPriorityThenDate() { /* ... */ }
}
```

Komplette Bodies analog zum bestehenden `ProductionOrderRepositoryTests`-Pattern. Setup via Helper aus Step 2.

- [ ] **Step 4: `ProductionOrderBdeStatusRepositoryTests.cs`** (1 Test, Spec 11.1)

```csharp
public class ProductionOrderBdeStatusRepositoryTests
{
    [Fact]
    public async Task SetIsDoneBdeAsync_PersistsValue_AndAuditFields() { /* ... */ }
}
```

- [ ] **Step 5: `ProductionOrderAssemblyGroupRepositoryTests.cs`** (Spec 11.1)

```csharp
public class ProductionOrderAssemblyGroupRepositoryTests
{
    [Fact]
    public async Task GetByPoAndKeyAsync_ReturnsRow() { /* ... */ }

    [Fact]
    public async Task SetIsApplicableAsync_UnknownGroupKey_ThrowsArgumentException() { /* ... */ }

    [Fact]
    public async Task GetIsApplicablePivotAsync_ReturnsDictPerOrderPerKey() { /* ... */ }
}
```

- [ ] **Step 6: API-Controller-Tests**

`PickingStatusApiControllerTests`, `AssemblyGroupsApiControllerTests`, `BdeStatusApiControllerTests` jeweils mit Tests nach Spec 11.2:

- HappyPath_ReturnsOk
- UnknownField bzw. UnknownGroupKey_Returns400
- UnknownOrder_Returns404

Permission-Check via Filter ist Integration-Sache — die Tests fokussieren auf Controller-Logik. Für den Permission-Check separater Helper-Test oder `[Trait("Category","Auth")]`.

- [ ] **Step 7: AgentJob-Integration-Test (SqlServerOnly)** (Spec 11.3, Open Decision 17)

`IdealAkeWms.Tests/Integration/ProductionOrderEagerCreateAgentJobTests.cs`:

```csharp
[Trait("Category", "SqlServerOnly")]
public class ProductionOrderEagerCreateAgentJobTests
{
    [SkippableFact]
    public async Task EagerCreate_ThreeMergesProduce7Rows_PerNewOrder()
    {
        // Skip if no Stage-DB connection
        var conn = Environment.GetEnvironmentVariable("WMS_STAGE_CONN");
        Skip.If(string.IsNullOrEmpty(conn), "Stage DB not configured");

        // 1. Cleanup: drop test FA if exists
        // 2. INSERT ProductionOrder
        // 3. Execute 3 MERGEs aus 01_Import_Produktionsauftraege.sql
        // 4. Assert 1×PickingStatus, 1×BdeStatus, 5×AssemblyGroups
        // 5. Re-Run MERGEs → still 1+1+5 (idempotency)
    }
}
```

**Open:** Diese Test-Klasse wird `[Trait("Category","SqlServerOnly")]` markiert und im CI-Filter ausgeschlossen. Lokal manuell mit `dotnet test --filter "Category=SqlServerOnly"` + gesetztem `WMS_STAGE_CONN`. Wenn Stage-DB-Infrastruktur fehlt → diesen Test nur als Dokumentation lassen und im Cutover-Skript Sektion 14.9 manuell via SSMS validieren.

- [ ] **Step 8: Bestehende Tests reparieren**

Pro Test-Klasse Setup-Code prüfen:

```pwsh
# Grep nach den entfernten Properties in Tests
```

Suche im `IdealAkeWms.Tests`-Projekt nach `.HasGlass = `, `.IsReleasedForPicking = `, `.PickingPriority = `, `.AssignedPickerId = ` etc. Jede gefundene Stelle:

1. Wenn es Setup ist (`order.HasGlass = true; context.SaveChanges();`): durch Helper-Aufruf ersetzen, oder zugehörige Status-Zeile manuell erzeugen.
2. Wenn es Assertion ist (`Assert.True(order.HasGlass)`): umstellen auf `Assert.True(pickingStatus.HasGlass)` mit Re-Load der Zeile.

Erwartete Stellen (aus Spec-Grep-Recommendation): ~25 Tests.

Tests, die `IProductionOrderRepository.GetReleasedForPickingAsync()` mockten, jetzt `IProductionOrderPickingStatusRepository.GetReleasedForPickingAsync()` mocken.

- [ ] **Step 9: Build + Tests grün**

```pwsh
dotnet build --nologo
dotnet test --nologo --no-build --filter "Category!=SqlServerOnly"
```

Erwartet: 0 errors, 0 failing tests (SqlServerOnly-Tests ausgeschlossen). Falls noch rot: per Test einzeln durchgehen.

- [ ] **Step 10: Commit**

```pwsh
git add IdealAkeWms.Tests/
git commit -m @'
test(productionorders): repo + api tests for 5-table split; migrate existing

Spec 11. Adds 3 new repo test classes, 3 new api controller test classes,
1 SqlServerOnly integration test for agent job eager-create (skipped in CI
unless WMS_STAGE_CONN env var is set). Migrates ~25 existing tests to set
up Status rows alongside ProductionOrder.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
'@
```

---

## Task 9: Version + Changelog + Help + CLAUDE.md + TESTSZENARIEN

**Files:**
- Modify: `IdealAkeWms/AppVersion.cs`
- Modify: `IdealAkeWmsService/AppVersion.cs`
- Modify: `IdealAkeWms/Views/Help/Changelog.cshtml`
- Modify: `IdealAkeWms/Views/Help/Index.cshtml`
- Modify: `CLAUDE.md`
- Modify: `docs/TESTSZENARIEN.md`

- [ ] **Step 1: AppVersion-Bump auf 1.11.0**

`IdealAkeWms/AppVersion.cs`:

```csharp
namespace IdealAkeWms;

public static class AppVersion
{
    public const string Version = "1.11.0";
    public const string Date = "2026-05-12";
}
```

`IdealAkeWmsService/AppVersion.cs` identisch:

```csharp
namespace IDEALAKEWMSService;

public static class AppVersion
{
    public const string Version = "1.11.0";
    public const string Date = "2026-05-12";
}
```

- [ ] **Step 2: Changelog v1.11.0-Card einfügen**

In `IdealAkeWms/Views/Help/Changelog.cshtml` vor der bestehenden v1.10.0-Card (Zeile 10) eine neue Card einfügen:

```cshtml
<div class="card mb-3">
    <div class="card-header text-white" style="background-color: var(--ake-primary);">
        <strong>v1.11.0</strong> <span class="text-white-50 ms-2">12.05.2026</span>
    </div>
    <div class="card-body">
        <h6>ProductionOrder Schema-Refactor (Phase 1)</h6>
        <ul>
            <li><strong>Aufteilung der ProductionOrders-Tabelle:</strong> Die fr&uuml;here Tabelle wurde in 5 spezialisierte Tabellen aufgeteilt &mdash;
                <code>ProductionOrderPickingStatus</code> (1:1), <code>ProductionOrderBdeStatus</code> (1:1),
                <code>ProductionOrderAssemblyGroups</code> (1:N, 5 Zeilen pro FA f&uuml;r VK/VL/VE/VT/VA),
                <code>ProductionOrderAssemblyGroupSpecs</code> (1:N, Phase 1 leer),
                <code>ProductionWorkplaceAssemblyGroups</code> (Junction, Phase 1 leer).
                Sage-Master bleibt in <code>ProductionOrders</code> mit nur noch ~12 Spalten.</li>
            <li><strong>Toggle-API in 3 Endpoints aufgeteilt:</strong>
                <code>/api/picking-status/toggle</code> (Glas/Zukauf/Lackierteile/Picking-Done),
                <code>/api/assembly-groups/toggle-applicable</code> (VK/VL/VE/VT/VA),
                <code>/api/bde-status/toggle</code> (BDE-Done). Alter Endpoint <code>/api/productionorders/toggle-field</code> entfernt.</li>
            <li><strong>Sage-AgentJob:</strong> Importiert nach wie vor in <code>ProductionOrders</code> und legt automatisch f&uuml;r jeden neuen Auftrag die 7 Status-Zeilen an (1+1+5). Bestehende Status-Werte werden vom Sync nicht ueberschrieben.</li>
            <li><strong>UI unver&auml;ndert sichtbar:</strong> Produktionsauftragsliste, Picking-Liste, Leitstand-Freigabe und Spalten-Filter funktionieren wie vorher. Datenherkunft wechselt unter der Haube von Direkt-Spalten auf JOIN-Lookups mit Pivot-Aggregation.</li>
        </ul>
    </div>
</div>
```

Den `<li>`-Block ueber den 5 Baugruppen-Flags in der v1.10.0-Card BELASSEN — der dokumentiert die alte Implementierung und ist historisch korrekt.

- [ ] **Step 3: Help/Index.cshtml — Architektur-Hinweis**

In `IdealAkeWms/Views/Help/Index.cshtml` im Produktionsauftrags-Abschnitt einen Absatz erg&auml;nzen:

```cshtml
<dt>Datenarchitektur (seit v1.11.0)</dt>
<dd>
    Die Auftrags-Status-Felder (Glas, Zukauf, VK/VL/VE/VT/VA, Lackier-T, Freigabe, Picker-Zuweisung)
    sind seit Mai 2026 in eigene Tabellen ausgelagert (<code>ProductionOrderPickingStatus</code> und
    <code>ProductionOrderAssemblyGroups</code>). Funktional &auml;ndert sich f&uuml;r den User nichts.
    Bei R&uuml;ckfragen zur Datenstruktur: siehe Schema-Spec im internen Wiki.
</dd>
```

Falls kein passender Produktionsauftrags-Abschnitt: skip.

- [ ] **Step 4: CLAUDE.md Fallstrick erg&auml;nzen**

In `CLAUDE.md` im Bereich "Bekannte Fallstricke" einen Eintrag einf&uuml;gen (alphabetisch nahe `MovementType-Aggregation`):

```markdown
- **ProductionOrder Status-Aufteilung (seit v1.11.0)**: `ProductionOrder` enthaelt nur noch Sage-Master + `IsDone` + `ProductionWorkplaceId`. Picking-/BDE-/Baugruppen-Status leben in 5 separaten Tabellen (`ProductionOrderPickingStatus`, `ProductionOrderBdeStatus`, `ProductionOrderAssemblyGroups`, `ProductionOrderAssemblyGroupSpecs`, `ProductionWorkplaceAssemblyGroups`). Die Property `ProductionOrder.PickingStatus` ist jetzt eine **Navigation-Property** (nicht mehr `string?`). Toggle-API in 3 Endpoints: `/api/picking-status/toggle`, `/api/assembly-groups/toggle-applicable`, `/api/bde-status/toggle`. Sage-AgentJob legt fuer jeden neuen FA eager 7 Status-Zeilen an (1 Picking, 1 BDE, 5 AssemblyGroups).
```

- [ ] **Step 5: TESTSZENARIEN.md — 4 neue Szenarien**

In `docs/TESTSZENARIEN.md` im TS-3-Block (FA-Liste) folgende Szenarien anh&auml;ngen (Nummerierung passend zu vorhandenem Stand fortsetzen — bestehende Nummer pr&uuml;fen):

```markdown
### TS-3.x — Toggle-Routing nach Refactor

**Vorbedingungen:**
- App auf v1.11.0, eingeloggt mit picking-Rolle.

**Schritte:**
1. Produktionsauftragsliste &ouml;ffnen, Browser-DevTools-Network-Tab aktivieren.
2. Glas-Checkbox togglen.
3. VK-Checkbox togglen.
4. Lackierteile-Checkbox togglen (falls sichtbar).

**Erwartetes Verhalten:**
- Glas-Toggle → POST `/api/picking-status/toggle` mit `{ productionOrderId, field: "HasGlass", value }`, 200 OK.
- VK-Toggle → POST `/api/assembly-groups/toggle-applicable` mit `{ productionOrderId, groupKey: "VK", value }`, 200 OK.
- Lackierteile-Toggle → POST `/api/picking-status/toggle` mit `field: "IsCoatingDone"`, 200 OK.
- Kein Request an alten `/api/productionorders/toggle-field`.

---

### TS-3.x — Migrations-Verifikation Post-Cutover

**Vorbedingungen:**
- SQL-Skript `60_ProductionOrderSplit.sql` (oder EF-Migration) ist gelaufen.

**Schritte:**
1. SSMS gegen Produktiv-DB:
   ```sql
   SELECT COUNT(*) AS POs FROM dbo.ProductionOrders;
   SELECT COUNT(*) AS PSs FROM dbo.ProductionOrderPickingStatus;
   SELECT COUNT(*) AS BDEs FROM dbo.ProductionOrderBdeStatus;
   SELECT COUNT(*) AS Grps FROM dbo.ProductionOrderAssemblyGroups;
   ```

**Erwartetes Verhalten:**
- `POs == PSs == BDEs`.
- `Grps == 5 * POs`.

---

### TS-3.x — AgentJob-Eager-Create

**Vorbedingungen:**
- Sage-View hat einen NEU angelegten FA, der noch nicht in `ProductionOrders` liegt.

**Schritte:**
1. SQL Agent Job `01_Import_Produktionsauftraege` manuell ausfuehren.
2. SSMS: pruefen `SELECT * FROM dbo.ProductionOrderPickingStatus WHERE ProductionOrderId = <neuer FA>`.
3. gleiches fuer `ProductionOrderBdeStatus`, `ProductionOrderAssemblyGroups`.

**Erwartetes Verhalten:**
- 1 Zeile in PickingStatus (alle Booleans = 0).
- 1 Zeile in BdeStatus (`IsDoneBde = 0`).
- 5 Zeilen in AssemblyGroups (`GroupKey IN ('VK','VL','VE','VT','VA')`, alle `IsApplicable = 0`).
- AgentJob 2× hintereinander ausfuehren → keine Duplikate.

---

### TS-3.x — Leitstand-Freigabe nach Refactor

**Vorbedingungen:**
- Leitstand-Rolle eingeloggt, FA mit ArticleNumber existiert.

**Schritte:**
1. FA-Liste &ouml;ffnen, Freigabe-Toggle aktivieren.
2. Picker-Zuweisung im Modal w&auml;hlen.
3. Priorit&auml;t setzen.
4. SSMS pr&uuml;fen: `SELECT IsReleasedForPicking, PickingPriority, AssignedPickerId, ReleasedBy FROM dbo.ProductionOrderPickingStatus WHERE ProductionOrderId = <Test-FA>`.

**Erwartetes Verhalten:**
- Alle Werte in `ProductionOrderPickingStatus` korrekt gesetzt.
- `ProductionOrders`-Tabelle enth&auml;lt keine dieser Spalten mehr.
- Audit-Felder `ModifiedAt`/`ModifiedBy` auf der Status-Zeile, nicht auf FA.

---
```

- [ ] **Step 6: Build + Tests**

```pwsh
dotnet build --nologo
dotnet test --nologo --no-build --filter "Category!=SqlServerOnly"
```

Erwartet: 0 errors, 0 failing tests.

- [ ] **Step 7: Commit**

```pwsh
git add IdealAkeWms/AppVersion.cs IdealAkeWmsService/AppVersion.cs IdealAkeWms/Views/Help/Changelog.cshtml IdealAkeWms/Views/Help/Index.cshtml CLAUDE.md docs/TESTSZENARIEN.md
git commit -m @'
docs: v1.11.0 changelog + claude.md fallstrick + 4 testszenarien

Spec 15. Bumps AppVersion to 1.11.0 / 2026-05-12 in both web and service.
Adds new changelog card for the schema refactor, help page hint about new
data architecture, CLAUDE.md fallstrick describing the navigation-property
rename and 7-row eager-create, and 4 manual test scenarios for cutover
verification.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
'@
```

---

## Task 10: Cutover-Rehearsal-Checklist (Non-executable)

**Files:**
- Keine. Dieser Task ist eine Checkliste f&uuml;r den menschlichen Operator am Go-Live-Tag (Spec 14, Roadmap 5.6).

Der implementierende Agent f&uuml;hrt diesen Task **NICHT aus** — er listet die Schritte als Plan-Output, der vom Operator am Cutover-Tag manuell abgearbeitet wird.

- [ ] **Step 1: Wartungsfenster ank&uuml;ndigen (T-48h)**

Mail an Picking-/Tracking-/Leitstand-User-Gruppe: 30-60 min Downtime an Tag X, Uhrzeit Y, Grund "Datenbank-Refactor". Backup-Plan-Window 30 min nach App-Start.

- [ ] **Step 2: App stoppen (T0)**

IIS-Site `IdealAkeWms` stoppen. Service `IDEALAKEWMSService` stoppen. **Verify:** keine aktiven Connections in `sys.dm_exec_sessions`.

- [ ] **Step 3: Vollst&auml;ndiges DB-Backup (T0)**

```sql
BACKUP DATABASE [IDEAL_AKE_WMS]
TO DISK = 'D:\Backups\IDEAL_AKE_WMS_v1.10_pre-split.bak'
WITH COMPRESSION, INIT, NAME = 'Pre v1.11 ProductionOrder Split';
```

Backup-Dauer notieren. Restore-Test in 1-2 Min auf Stage validieren? (Optional.)

- [ ] **Step 4: SQL Agent Jobs deaktivieren (T+1min)**

In SSMS: `01_Import_Produktionsauftraege` + `02_Import_Artikel` auf "Disabled" stellen. Verify: kein Job l&auml;uft (`sys.dm_exec_jobs`).

- [ ] **Step 5: Migration ausf&uuml;hren (T+2min)**

Option A: `SQL/60_ProductionOrderSplit.sql` per SSMS manuell. PRINTs lesen, Counts erwarten.
Option B: App-Start → `db.Database.Migrate()` macht es. (Spec 14.8 empfiehlt B als Default.)

- [ ] **Step 6: Verifikations-Counts pr&uuml;fen (T+~20min)**

```sql
SELECT COUNT(*) FROM dbo.ProductionOrders;
SELECT COUNT(*) FROM dbo.ProductionOrderPickingStatus;
SELECT COUNT(*) FROM dbo.ProductionOrderBdeStatus;
SELECT COUNT(*) FROM dbo.ProductionOrderAssemblyGroups;
```

Erwartung: PS == BDE == PO, Groups == 5×PO.

- [ ] **Step 7: Neue App-Version deployen (T+~25min)**

`dotnet publish` Output auf den IIS-Server kopieren (Standard-Deployment). Auch neuen `01_Import_Produktionsauftraege.sql` auf SQL-Agent-Job-Host kopieren (oder den Job-Script-Body in SSMS aktualisieren).

- [ ] **Step 8: App-Start + Smoke-Test (T+~30min)**

App starten. Login als Admin.

Smoke-Test-Checkliste (Spec 14.9):
1. FA-Liste l&auml;dt < 2 s.
2. 1× FA freigeben → SSMS `SELECT IsReleasedForPicking FROM ProductionOrderPickingStatus` zeigt 1.
3. 1× HasGlass-Toggle.
4. 1× IsCoatingDone-Toggle.
5. 1× IsDonePicking-Toggle.
6. 1× VK-Toggle → SSMS pr&uuml;fen `ProductionOrderAssemblyGroups` Zeile mit `GroupKey='VK', IsApplicable=1`.
7. 1× VL, VE, VT, VA jeweils.
8. 1× IsDoneBde-Toggle (falls UI vorhanden — sonst skip Phase 1).
9. 1× Picking-Status setzen via PickingController → SSMS pr&uuml;fen `ProductionOrderPickingStatus.PickingStatus`.
10. 1× St&uuml;ckliste &ouml;ffnen → l&auml;dt ohne Fehler.

- [ ] **Step 9: Agent Jobs reaktivieren (T+~45min)**

In SSMS: `01_Import_Produktionsauftraege` + `02_Import_Artikel` "Enabled". Manuellen Job-Start ausl&ouml;sen → Verify keine Fehler im SQL Agent History. Verify: 1 neuer FA in Sage f&uuml;hrt zu 7 Status-Zeilen (siehe TS-3.x AgentJob-Eager-Create).

- [ ] **Step 10: Wartungsfenster schliessen (T+~50min)**

User informieren: System wieder verf&uuml;gbar. Backup-Restore-Window endet 30 Minuten sp&auml;ter (T+~80min). Danach Forward-Fix-Only-Strategie.

- [ ] **Step 11: 5-Tage-Monitoring (T+5d)**

Spec-Anhang: Nach 5 Tagen Live-Verifikation pr&uuml;fen
- Keine `NullReferenceException` im Log auf `Status` lookups.
- AgentJob hat keine Lücken in PickingStatus/BdeStatus/AssemblyGroups.
- Performance der FA-Liste vergleichbar mit pre-Refactor.

Wenn OK → Phase-2-Detail-Spec schreiben (View-Split / Picking-Leitstand-Trennung).

---

## Manuelle End-to-End-Verifikation (vor Merge in main)

Vor `git merge feature/* → main` und vor dem Wartungsfenster:

- **Build + Tests (CI):** `dotnet build --nologo && dotnet test --nologo --no-build --filter "Category!=SqlServerOnly"` → alles gr&uuml;n.
- **EF Migration auf Stage-DB:** Stage-Kopie der Produktiv-DB nehmen, App gegen Stage starten, `Migrate()` laufen lassen, Stichproben-FA pr&uuml;fen (PS/BDE/Groups vorhanden, alte Spalten weg).
- **Stage-Smoke-Test:** Schritte 1-10 aus Task 10.8 auf Stage durchspielen.
- **Performance-Check:** FA-Liste mit >500 FAs auf Stage öffnen → Antwortzeit < 1 s (Risiko 12.4).
- **AgentJob auf Stage:** Job manuell laufen lassen, neue FA in Stage-Sage anlegen (oder Source-View simulieren), Verify 7 Status-Zeilen.
- **Rollback-Probe auf Stage:** Backup wiederherstellen aus Step 3 → Stage zurück auf v1.10, App-Start, Smoke. (Nur einmal, dokumentiert).

---

## Self-Review — Spec-Coverage

Jede Spec-Sektion (4-15) → Task-Nummer-Mapping:

| Spec-Sektion | Inhalt | Task |
|---|---|---|
| 4.1 — Slim ProductionOrders | 16 Properties weg, Sage-Master bleibt | **Task 1, Step 6** + **Task 2, Step 5** |
| 4.2 — PickingStatus-Schema | CREATE TABLE | **Task 1, Step 1** (Entity) + **Task 2, Step 4** (SQL) |
| 4.3 — BdeStatus-Schema | CREATE TABLE | **Task 1, Step 2** + **Task 2, Step 4** |
| 4.4 — AssemblyGroups-Schema (1:N, 5/FA) | CREATE TABLE | **Task 1, Step 3** + **Task 2, Step 4** |
| 4.5 — AssemblyGroupSpecs (leer) | CREATE TABLE | **Task 1, Step 4** + **Task 2, Step 4** |
| 4.6 — WorkplaceAssemblyGroups (leer) | CREATE TABLE | **Task 1, Step 5** + **Task 2, Step 4** |
| 5.1 — ProductionOrder Nav-Props | PickingStatus / BdeStatus / AssemblyGroups | **Task 1, Step 6** |
| 5.2 — DbContext Configs | 5 DbSets + Relationships | **Task 1, Steps 7-9** |
| 6.1 — alter Toggle-Field entfernen | ProductionOrdersApiController.ToggleField weg | **Task 5, Step 4** |
| 6.2 — 3 neue API-Controller | PickingStatus / AssemblyGroups / BdeStatus | **Task 5, Steps 1-3** |
| 6.3 — View-JS-Dispatcher | data-endpoint Routing | **Task 7, Steps 1-2** |
| 7.1 — 3 neue Repositories | + DI-Registration | **Task 4, Steps 1-6** |
| 7.2 — ProductionOrderRepository-Cleanup | Released-Methoden raus | **Task 4, Step 5** |
| 7.3 — Pivot-Aggregation Index | GetIsApplicablePivotAsync | **Task 4, Step 4** (Impl) + **Task 6, Step 2** (Use) |
| 8.1 — EF Migration | hand-edited Up() | **Task 2, Steps 2-3** |
| 8.2 — SQL-Skript 60 | idempotent, batched, drop-reihenfolge | **Task 2, Step 4** |
| 8.3 — __EFMigrationsHistory | separater Insert | **Task 2, Step 4** (Section G) + Step 5 |
| 8.4 — FreshInstall Update | Slim PO + 5 neue Sektionen | **Task 2, Step 5** |
| 9 — Sage-AgentJob Folge-MERGEs | 3 MERGEs nach Top-Merge | **Task 3, Steps 3-5** |
| 10.1 — ProductionOrderViewItem stabil | View-only Bools | **Task 6, Step 8** |
| 10.2 — ProductionOrdersController.Index | Pivot-Mapping | **Task 6, Steps 2** |
| 10.3 — View-Markup Checkbox | data-endpoint/data-group-key | **Task 7, Step 1** |
| 10.4 — PickingController + Views | Index + SetPickingStatus | **Task 6, Steps 6-7** |
| 10.5 — ToggleRelease/BulkRelease/SetPriority | nutzt PickingStatusRepository | **Task 6, Steps 3-5** |
| 11.1 — Neue Repo-Tests | 3 Test-Klassen | **Task 8, Steps 3-5** |
| 11.2 — Neue API-Tests | 3 Controller-Test-Klassen | **Task 8, Step 6** |
| 11.3 — AgentJob-Integration | SqlServerOnly | **Task 8, Step 7** |
| 11.4 — Bestehende Tests | Setup migrieren | **Task 8, Step 8** |
| 12 — Risiken | dokumentiert; Mitigations in den jew. Tasks | s. Task 2/8/10 |
| 13 — Test-Szenarien | 4 Szenarien | **Task 9, Step 5** |
| 14 — Deploy-Reihenfolge | Cutover-Checklist | **Task 10** |
| 15 — Versionierung + Doku | AppVersion + Changelog + Help + CLAUDE.md + TESTSZENARIEN | **Task 9, Steps 1-5** |
| 17 — Offene Entscheidung | AgentJob-Test-Strategie | **Task 8, Step 7** als `[Trait("Category","SqlServerOnly")]`, Default skip in CI |

**Open Decisions remaining at plan-time:**

- **Open:** AgentJob-Integration-Test-Infrastruktur — falls keine Stage-DB-Connection im CI verfügbar ist, läuft `ProductionOrderEagerCreateAgentJobTests` ausschliesslich lokal mit gesetztem `WMS_STAGE_CONN`. Default in Plan: `[Trait("Category","SqlServerOnly")]`, in CI ausgeschlossen, Cutover-Tag manuell via TS-3.x AgentJob-Eager-Create-Szenario validiert. **Wenn** der Operator Stage-Infrastruktur bereitstellt, Trait optional rausnehmen + WMS_STAGE_CONN als Secret konfigurieren.

**Reihenfolge ist wichtig:**

1. Task 1 zuerst — Entity-Layer ist Fundament. Build wird ROT — beabsichtigt.
2. Task 2 baut die Migration mit Daten-Copy auf. SQL-Skript ist Cutover-Tool.
3. Task 3 macht den AgentJob für neue FAs nach Cutover bereit.
4. Task 4 baut die Repository-Schicht. Build bleibt rot (Controllers brechen).
5. Task 5 macht die neuen API-Endpoints scharf, entfernt den alten.
6. Task 6 stellt die Inline-Mappings + Controllers um — **erster Punkt, ab dem das Web-Projekt grün baut**.
7. Task 7 macht das Frontend bereit (data-endpoint + JS).
8. Task 8 repariert die Tests — **erster grüner `dotnet test`**.
9. Task 9 Doku + Version.
10. Task 10 ist der Cutover-Drehbuch — **NICHT vom Agent ausgeführt**.

**No-Placeholder-Check:** Keine TBDs außer der einen `Open`-Markierung in Task 8 Step 7. Alle Code-Snippets vollständig (bis auf `<TIMESTAMP>` für den EF-Migration-Filename, der erst zur Build-Zeit feststeht).

**Commit-Frequency:** 9 Commits + Cutover-Tag (kein Code). Task 10 erzeugt keinen Commit — die Checkliste ist die Dokumentation.

**Branch-Strategie:** `refactor/production-order-split` (eigener WorkTree), Merge nach `main` direkt nach Stage-Smoke und vor dem Wartungsfenster.
