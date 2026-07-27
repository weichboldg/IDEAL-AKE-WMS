# FA-Vervollstaendigung-Erweiterung (v1.22.0) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Arbeitsgaenge-Katalog + FA-zu-AG-Tabelle (Sync-Erkennung aus BOM-Cache) + konfigurierbare Vorbau-Merkmale + FA-Abarbeitungsliste je Werkbank — ersetzt das fixe AssemblyGroups-Kachel-System.

**Architecture:** 7 neue Tabellen ersetzen `ProductionOrderAssemblyGroups`/`...Specs` (daten-erhaltende Migration). Erkennung laeuft als eigener idempotenter Sync-Schritt NACH dem BomCache (NICHT am ContentHash-Insert-Pfad). Leitstand behaelt 5 statische Spalten, liest aber das neue Pivot. Spec: [2026-06-12-fa-vervollstaendigung-erweiterung-design.md](../specs/2026-06-12-fa-vervollstaendigung-erweiterung-design.md) — bei Detailfragen IMMER die Spec lesen.

**Tech Stack:** ASP.NET Core 10 MVC + EF Core 10 (SQL Server), xUnit + FluentAssertions + Moq + EF InMemory, Windows-Service (SyncWorker).

**WICHTIGE RAHMENBEDINGUNGEN:**
- Ausfuehrung in NEUEM Worktree `gegen main` NACH Merge von `bugfix/missingparts-include-pd` (v1.19–v1.21.1): `git worktree add .claude/worktrees/fa-vorbau -b feature/fa-vorbau`
- KEIN `EF.Functions.Like` in Repos/Services (InMemory-Tests, siehe CLAUDE.md BdeBookings-Fallstrick) — `ToLower().Contains` verwenden
- Jede neue Listen-View: Pflicht-Pattern (Pagination + Server-Spaltenfilter + `@section Scripts` mit `column-preferences.js` VOR `table-filter.js`)
- AgentJob-Aenderung (Task 15) ist deploy-kritisch → Cutover-Doc

---

### Task 0: Pre-Flight

- [ ] **Step 1:** Pruefen dass `bugfix/missingparts-include-pd` in main gemerged ist (`git log main --oneline -5` enthaelt v1.21.1-Commits). Falls NEIN: STOPP, User fragen.
- [ ] **Step 2:** Worktree anlegen: `git worktree add .claude/worktrees/fa-vorbau -b feature/fa-vorbau` (vom main-HEAD), dorthin wechseln.
- [ ] **Step 3:** Baseline: `dotnet build IdealAkeWms.slnx` (0 Fehler) + `dotnet test IdealAkeWms.slnx --no-build` — Zahlen notieren (erwartet ~674 Web / 99 Service gruen).

---

### Task 1: Models + DbContext + EF-Migration (inkl. Daten-Konvertierung)

**Files:**
- Create: `IdealAkeWms/Models/WorkStep.cs`, `FaWorkStep.cs`, `FaWorkStepSpec.cs`, `FaAttributeDefinition.cs`, `FaAttributeOption.cs`, `FaAttributeWorkStep.cs`, `FaAttributeValue.cs`, `ProductionWorkplaceWorkStep.cs`
- Delete: `IdealAkeWms/Models/ProductionOrderAssemblyGroup.cs`, `ProductionOrderAssemblyGroupSpec.cs`
- Modify: `IdealAkeWms/Data/ApplicationDbContext.cs`
- Migration: `dotnet ef migrations add FaWorkStepsAndAttributes`

- [ ] **Step 1: Models schreiben** (alle in `namespace IdealAkeWms.Models`, erben `AuditableEntity`):

```csharp
// WorkStep.cs
using System.ComponentModel.DataAnnotations;
namespace IdealAkeWms.Models;

public class WorkStep : AuditableEntity
{
    [Required(ErrorMessage = "Code ist erforderlich")]
    [StringLength(20)]
    [Display(Name = "Code")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Name ist erforderlich")]
    [StringLength(100)]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Suchbegriffe (kommasepariert)")]
    public string? SearchString { get; set; }

    [Display(Name = "Reihenfolge")]
    public int SortOrder { get; set; }

    [Display(Name = "Aktiv")]
    public bool IsActive { get; set; } = true;
}
```

```csharp
// FaWorkStep.cs
namespace IdealAkeWms.Models;

public static class FaWorkStepSources
{
    public const string Sync = "Sync";
    public const string Manual = "Manual";
}

public class FaWorkStep : AuditableEntity
{
    public int ProductionOrderId { get; set; }
    public ProductionOrder ProductionOrder { get; set; } = null!;

    public int WorkStepId { get; set; }
    public WorkStep WorkStep { get; set; } = null!;

    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? CompletedBy { get; set; }

    public string Source { get; set; } = FaWorkStepSources.Manual; // NVARCHAR(20)

    /// <summary>Manuell abgewaehlt — Sync darf NICHT re-adden.</summary>
    public bool IsRemoved { get; set; }

    public ICollection<FaWorkStepSpec> Specs { get; set; } = new List<FaWorkStepSpec>();
}
```

```csharp
// FaWorkStepSpec.cs — Felder identisch zu bisherigem ProductionOrderAssemblyGroupSpec
using System.ComponentModel.DataAnnotations;
namespace IdealAkeWms.Models;

public class FaWorkStepSpec : AuditableEntity
{
    public int FaWorkStepId { get; set; }
    public FaWorkStep FaWorkStep { get; set; } = null!;

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

```csharp
// FaAttributeDefinition.cs — AttributeType-Enum aus ArticleAttributeDefinition.cs WIEDERVERWENDEN
using System.ComponentModel.DataAnnotations;
namespace IdealAkeWms.Models;

public class FaAttributeDefinition : AuditableEntity
{
    [Required(ErrorMessage = "Name ist erforderlich")]
    [StringLength(200)]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Typ")]
    public AttributeType AttributeType { get; set; }

    [Display(Name = "Reihenfolge")]
    public int SortOrder { get; set; }

    [Display(Name = "Aktiv")]
    public bool IsActive { get; set; } = true;

    public ICollection<FaAttributeOption> Options { get; set; } = new List<FaAttributeOption>();
    public ICollection<FaAttributeWorkStep> WorkSteps { get; set; } = new List<FaAttributeWorkStep>();
}
```

```csharp
// FaAttributeOption.cs
using System.ComponentModel.DataAnnotations;
namespace IdealAkeWms.Models;

public class FaAttributeOption : AuditableEntity
{
    public int FaAttributeDefinitionId { get; set; }
    public FaAttributeDefinition Definition { get; set; } = null!;

    [Required]
    [StringLength(200)]
    [Display(Name = "Wert")]
    public string Value { get; set; } = string.Empty;

    [Display(Name = "Reihenfolge")]
    public int SortOrder { get; set; }

    [Display(Name = "Aktiv")]
    public bool IsActive { get; set; } = true;
}
```

```csharp
// FaAttributeWorkStep.cs — N:M Junction (nur CreatedAt/By wie UserRole — an bestehender
// UserRole-Klasse orientieren; falls UserRole AuditableEntity erbt, hier genauso)
namespace IdealAkeWms.Models;

public class FaAttributeWorkStep
{
    public int Id { get; set; }
    public int FaAttributeDefinitionId { get; set; }
    public FaAttributeDefinition Definition { get; set; } = null!;
    public int WorkStepId { get; set; }
    public WorkStep WorkStep { get; set; } = null!;
}
```

```csharp
// FaAttributeValue.cs
namespace IdealAkeWms.Models;

public class FaAttributeValue : AuditableEntity
{
    public int ProductionOrderId { get; set; }
    public ProductionOrder ProductionOrder { get; set; } = null!;

    public int FaAttributeDefinitionId { get; set; }
    public FaAttributeDefinition Definition { get; set; } = null!;

    public int? SelectedOptionId { get; set; }
    public FaAttributeOption? SelectedOption { get; set; }

    public bool? BooleanValue { get; set; }
}
```

```csharp
// ProductionWorkplaceWorkStep.cs — N:M Junction
namespace IdealAkeWms.Models;

public class ProductionWorkplaceWorkStep
{
    public int Id { get; set; }
    public int ProductionWorkplaceId { get; set; }
    public ProductionWorkplace ProductionWorkplace { get; set; } = null!;
    public int WorkStepId { get; set; }
    public WorkStep WorkStep { get; set; } = null!;
}
```

- [ ] **Step 2: DbContext** — alte DbSets (`ProductionOrderAssemblyGroups`, `ProductionOrderAssemblyGroupSpecs`) entfernen, neue ergaenzen + `OnModelCreating`:

```csharp
public DbSet<WorkStep> WorkSteps => Set<WorkStep>();
public DbSet<FaWorkStep> FaWorkSteps => Set<FaWorkStep>();
public DbSet<FaWorkStepSpec> FaWorkStepSpecs => Set<FaWorkStepSpec>();
public DbSet<FaAttributeDefinition> FaAttributeDefinitions => Set<FaAttributeDefinition>();
public DbSet<FaAttributeOption> FaAttributeOptions => Set<FaAttributeOption>();
public DbSet<FaAttributeWorkStep> FaAttributeWorkSteps => Set<FaAttributeWorkStep>();
public DbSet<FaAttributeValue> FaAttributeValues => Set<FaAttributeValue>();
public DbSet<ProductionWorkplaceWorkStep> ProductionWorkplaceWorkSteps => Set<ProductionWorkplaceWorkStep>();
```

```csharp
// In OnModelCreating (Stil der bestehenden Bloecke uebernehmen):
modelBuilder.Entity<WorkStep>(e =>
{
    e.HasIndex(w => w.Code).IsUnique();
});
modelBuilder.Entity<FaWorkStep>(e =>
{
    e.HasIndex(f => new { f.ProductionOrderId, f.WorkStepId }).IsUnique();
    e.HasIndex(f => f.WorkStepId);
    e.HasIndex(f => new { f.ProductionOrderId, f.IsRemoved });
    e.Property(f => f.Source).HasMaxLength(20);
    e.Property(f => f.CompletedBy).HasMaxLength(200);
    e.HasOne(f => f.ProductionOrder).WithMany().HasForeignKey(f => f.ProductionOrderId)
        .OnDelete(DeleteBehavior.Cascade);
    e.HasOne(f => f.WorkStep).WithMany().HasForeignKey(f => f.WorkStepId)
        .OnDelete(DeleteBehavior.Restrict);
});
modelBuilder.Entity<FaWorkStepSpec>(e =>
{
    e.HasIndex(s => s.FaWorkStepId);
    e.HasOne(s => s.FaWorkStep).WithMany(f => f.Specs).HasForeignKey(s => s.FaWorkStepId)
        .OnDelete(DeleteBehavior.Cascade);
    e.HasOne(s => s.Article).WithMany().HasForeignKey(s => s.ArticleId)
        .OnDelete(DeleteBehavior.SetNull);
});
modelBuilder.Entity<FaAttributeOption>(e =>
{
    e.HasOne(o => o.Definition).WithMany(d => d.Options).HasForeignKey(o => o.FaAttributeDefinitionId)
        .OnDelete(DeleteBehavior.Cascade);
});
modelBuilder.Entity<FaAttributeWorkStep>(e =>
{
    e.HasIndex(x => new { x.FaAttributeDefinitionId, x.WorkStepId }).IsUnique();
    e.HasOne(x => x.Definition).WithMany(d => d.WorkSteps).HasForeignKey(x => x.FaAttributeDefinitionId)
        .OnDelete(DeleteBehavior.Cascade);
    e.HasOne(x => x.WorkStep).WithMany().HasForeignKey(x => x.WorkStepId)
        .OnDelete(DeleteBehavior.Cascade);
});
modelBuilder.Entity<FaAttributeValue>(e =>
{
    e.HasIndex(v => new { v.ProductionOrderId, v.FaAttributeDefinitionId }).IsUnique();
    e.HasOne(v => v.ProductionOrder).WithMany().HasForeignKey(v => v.ProductionOrderId)
        .OnDelete(DeleteBehavior.Cascade);
    e.HasOne(v => v.Definition).WithMany().HasForeignKey(v => v.FaAttributeDefinitionId)
        .OnDelete(DeleteBehavior.Cascade);
    e.HasOne(v => v.SelectedOption).WithMany().HasForeignKey(v => v.SelectedOptionId)
        .OnDelete(DeleteBehavior.Restrict);
});
modelBuilder.Entity<ProductionWorkplaceWorkStep>(e =>
{
    e.HasIndex(x => new { x.ProductionWorkplaceId, x.WorkStepId }).IsUnique();
    e.HasOne(x => x.ProductionWorkplace).WithMany().HasForeignKey(x => x.ProductionWorkplaceId)
        .OnDelete(DeleteBehavior.Cascade);
    e.HasOne(x => x.WorkStep).WithMany().HasForeignKey(x => x.WorkStepId)
        .OnDelete(DeleteBehavior.Cascade);
});
```

ACHTUNG: Alle bisherigen Code-Referenzen auf die geloeschten Models kompilieren jetzt NICHT mehr — das ist erwartet und wird in Tasks 3–12 behoben. Fuer diesen Task: betroffene Dateien (Repos `ProductionOrderAssemblyGroup*Repository`, `AssemblyGroupsApiController`, FaCompletion-Teile, DI-Registrierungen in `Program.cs`, betroffene Tests) per Compilerfehler-Liste TEMPORAER mitloeschen bzw. auskommentieren ist VERBOTEN — stattdessen Task 1 mit Task 3+6 gemeinsam in einem Branch-Zustand abschliessen (siehe Step 5: Commit erst wenn Build gruen; die Tasks 1+3+6 bilden zusammen den ersten kompilierbaren Stand, Reihenfolge: Models → Repos → API → dann Migration generieren).

- [ ] **Step 3: Migration generieren** (erst NACH Task 3+6, wenn Build gruen): `dotnet ef migrations add FaWorkStepsAndAttributes --project IdealAkeWms`
- [ ] **Step 4: Daten-Konvertierung in `Up()` einfuegen** — VOR den `DropTable`-Aufrufen der alten Tabellen, via `migrationBuilder.Sql(...)`:

```csharp
// 1) Seed WorkSteps
migrationBuilder.Sql(@"
INSERT INTO WorkSteps (Code, Name, SearchString, SortOrder, IsActive, CreatedAt, CreatedBy, CreatedByWindows)
SELECT v.Code, v.Name, NULL, v.SortOrder, 1, GETDATE(), 'Migration', 'Migration'
FROM (VALUES ('VK','Kuehlung',1),('VL','Lueftung',2),('VE','Elektro',3),('VT','Tueren',4),('VA','Aufbau',5)) v(Code, Name, SortOrder)
WHERE NOT EXISTS (SELECT 1 FROM WorkSteps w WHERE w.Code = v.Code);");

// 2) AssemblyGroups -> FaWorkSteps (IsApplicable=1 ODER Spec-Traeger; Specs an inaktiven
//    Gruppen kommen mit IsRemoved=1)
migrationBuilder.Sql(@"
INSERT INTO FaWorkSteps (ProductionOrderId, WorkStepId, IsCompleted, CompletedAt, CompletedBy, Source, IsRemoved, CreatedAt, CreatedBy, CreatedByWindows)
SELECT g.ProductionOrderId, w.Id, g.IsCompleted, NULL, NULL, 'Manual',
       CASE WHEN g.IsApplicable = 1 THEN 0 ELSE 1 END,
       GETDATE(), 'Migration', 'Migration'
FROM ProductionOrderAssemblyGroups g
JOIN WorkSteps w ON w.Code = g.GroupKey
WHERE (g.IsApplicable = 1
       OR EXISTS (SELECT 1 FROM ProductionOrderAssemblyGroupSpecs s WHERE s.AssemblyGroupId = g.Id))
  AND NOT EXISTS (SELECT 1 FROM FaWorkSteps f
                  WHERE f.ProductionOrderId = g.ProductionOrderId AND f.WorkStepId = w.Id);");

// 3) Specs kopieren
migrationBuilder.Sql(@"
INSERT INTO FaWorkStepSpecs (FaWorkStepId, ArticleId, Description, Quantity, Notes, SortOrder, CreatedAt, CreatedBy, CreatedByWindows, ModifiedAt, ModifiedBy, ModifiedByWindows)
SELECT f.Id, s.ArticleId, s.Description, s.Quantity, s.Notes, s.SortOrder,
       s.CreatedAt, s.CreatedBy, s.CreatedByWindows, s.ModifiedAt, s.ModifiedBy, s.ModifiedByWindows
FROM ProductionOrderAssemblyGroupSpecs s
JOIN ProductionOrderAssemblyGroups g ON g.Id = s.AssemblyGroupId
JOIN WorkSteps w ON w.Code = g.GroupKey
JOIN FaWorkSteps f ON f.ProductionOrderId = g.ProductionOrderId AND f.WorkStepId = w.Id;");

// 4) Seed Merkmale + Optionen
migrationBuilder.Sql(@"
INSERT INTO FaAttributeDefinitions (Name, AttributeType, SortOrder, IsActive, CreatedAt, CreatedBy, CreatedByWindows)
SELECT v.Name, v.AttrType, v.SortOrder, 1, GETDATE(), 'Migration', 'Migration'
FROM (VALUES ('Verdampfergroesse',1,1),('Leitungsausgang',1,2),('Verdampfergehaeuse',1,3),('Ventil aussenliegend',0,4)) v(Name, AttrType, SortOrder)
WHERE NOT EXISTS (SELECT 1 FROM FaAttributeDefinitions d WHERE d.Name = v.Name);

INSERT INTO FaAttributeOptions (FaAttributeDefinitionId, Value, SortOrder, IsActive, CreatedAt, CreatedBy, CreatedByWindows)
SELECT d.Id, v.Value, v.SortOrder, 1, GETDATE(), 'Migration', 'Migration'
FROM (VALUES
 ('Verdampfergroesse','UKW 2/1',1),('Verdampfergroesse','UKW 3/1',2),('Verdampfergroesse','UKW 4/1',3),
 ('Verdampfergroesse','UKW 5/1 (Euro 4)',4),('Verdampfergroesse','UKW 6/1',5),('Verdampfergroesse','Euro 2',6),
 ('Verdampfergroesse','Euro 3',7),('Verdampfergroesse','Caleo 80',8),('Verdampfergroesse','Caleo 120',9),
 ('Verdampfergroesse','Breite 60',10),
 ('Leitungsausgang','Standard',1),('Leitungsausgang','RG',2),('Leitungsausgang','Links',3),('Leitungsausgang','Links RG',4),
 ('Verdampfergehaeuse','2/1 Standard',1),('Verdampfergehaeuse','3/1 RG',2),('Verdampfergehaeuse','Sonder',3)
) v(DefName, Value, SortOrder)
JOIN FaAttributeDefinitions d ON d.Name = v.DefName
WHERE NOT EXISTS (SELECT 1 FROM FaAttributeOptions o WHERE o.FaAttributeDefinitionId = d.Id AND o.Value = v.Value);");

// 5) Seed Rolle vorbau (Muster aus Migration AddMasterDataReadRole / SQL/67 ablesen
//    und exakt uebernehmen: Spaltenliste der Roles-Tabelle inkl. IsSystem, SortOrder!)
migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM Roles WHERE [Key] = 'vorbau')
INSERT INTO Roles ([Key], Name, Description, AdGroup, IsSystem, SortOrder, CreatedAt, CreatedBy, CreatedByWindows)
VALUES ('vorbau', 'Vorbau', 'FA-Abarbeitungsliste: Vorbau-Arbeitsgaenge einsehen und abhaken', NULL, 1,
        (SELECT MAX(SortOrder) + 1 FROM Roles), GETDATE(), 'Migration', 'Migration');");
```

- [ ] **Step 5: Build + bestehende Suite** (zusammen mit Task 3+6 gruen) → Commit `feat(db): WorkSteps/FaWorkSteps/FaAttribute-Modell + Konvertierungs-Migration` (ein Commit fuer Tasks 1+3+6 ist OK, alternativ logisch splitten wenn Build je Zwischenstand gruen).

---

### Task 2: SQL/68 + FreshInstall

**Files:**
- Create: `SQL/68_FaWorkStepsAndAttributes.sql`
- Modify: `SQL/00_FreshInstall.sql`

- [ ] **Step 1:** `SQL/68_FaWorkStepsAndAttributes.sql`: idempotentes Skript (OBJECT_ID-Guards, Tabellen in separaten GO-Batches) mit EXAKT denselben Objekten wie die EF-Migration: 8 CREATE TABLE (Spalten/Indizes/FKs wie Task 1 Step 2), die 5 Konvertierungs-/Seed-Bloecke aus Task 1 Step 4, DROP der alten Tabellen (mit OBJECT_ID-Guard), `__EFMigrationsHistory`-INSERT in separatem Batch (MigrationId aus dem generierten Migrations-Dateinamen).
- [ ] **Step 2:** `SQL/00_FreshInstall.sql`: alte CREATE-Bloecke `ProductionOrderAssemblyGroups`/`...Specs` (Zeilen ~313–374) ENTFERNEN, neue Tabellen + Seeds (WorkSteps, FaAttributeDefinitions/Options, Rolle vorbau) einfuegen, MigrationId im History-Block ergaenzen. **Beide Stellen — bekannter Fallstrick.**
- [ ] **Step 3:** Commit `feat(sql): SQL/68 + FreshInstall fuer FaWorkSteps-Modell`

---

### Task 3: Repositories WorkStep + FaWorkStep (TDD)

**Files:**
- Create: `IdealAkeWms/Data/Repositories/IWorkStepRepository.cs` + `WorkStepRepository.cs`
- Create: `IdealAkeWms/Data/Repositories/IFaWorkStepRepository.cs` + `FaWorkStepRepository.cs`
- Delete: `IProductionOrderAssemblyGroupRepository.cs`, `ProductionOrderAssemblyGroupRepository.cs`, `IProductionOrderAssemblyGroupSpecRepository.cs`, `ProductionOrderAssemblyGroupSpecRepository.cs` (Spec-CRUD wandert in FaWorkStepRepository)
- Modify: `IdealAkeWms/Program.cs` (DI: alte Registrierungen raus, neue rein)
- Test: `IdealAkeWms.Tests/Repositories/FaWorkStepRepositoryTests.cs` (neu)

**Interfaces:**

```csharp
public interface IWorkStepRepository
{
    Task<List<WorkStep>> GetAllAsync();                 // inkl. inaktive (Stammdaten-Liste)
    Task<List<WorkStep>> GetActiveAsync();              // SortOrder-sortiert
    Task<WorkStep?> GetByIdAsync(int id);
    Task<WorkStep?> GetByCodeAsync(string code);
    Task AddAsync(WorkStep step);
    Task UpdateAsync(WorkStep step);
    Task<bool> IsInUseAsync(int id);                    // FaWorkSteps/Mappings referenzieren?
    Task<bool> DeleteAsync(int id);                     // false wenn IsInUse (App-Guard)
}

public interface IFaWorkStepRepository
{
    Task<List<FaWorkStep>> GetByProductionOrderIdAsync(int productionOrderId, bool includeRemoved = false); // Include WorkStep + Specs
    /// <summary>Pivot orderId -> (WorkStep.Code -> aktiv d.h. IsRemoved=0). Chunked in 1000er-Bloecken (SQL-2100-Limit).</summary>
    Task<Dictionary<int, Dictionary<string, bool>>> GetWorkStepPivotAsync(List<int> productionOrderIds);
    /// <summary>Legt Zeile an bzw. reaktiviert (IsRemoved=0) oder setzt IsRemoved=1. Source=Manual bei User-Aktion.</summary>
    Task SetActiveAsync(int productionOrderId, int workStepId, bool active, string modifiedBy, string modifiedByWindows);
    Task SetIsCompletedAsync(int faWorkStepId, bool value, string modifiedBy, string modifiedByWindows); // setzt CompletedAt/By bzw. null
    Task<FaWorkStep?> GetByIdAsync(int id);
    // Spec-CRUD (1:1 Nachfolger des alten Spec-Repos):
    Task<FaWorkStepSpec?> GetSpecByIdAsync(int id);
    Task AddSpecAsync(FaWorkStepSpec spec);
    Task UpdateSpecAsync(FaWorkStepSpec spec);
    Task DeleteSpecAsync(int id);
}
```

- [ ] **Step 1: Failing Tests** (`TestDbContextFactory.Create()`, Muster `ProductionOrderRepositoryTests`):

```csharp
[Fact]
public async Task SetActive_ReactivatesRemovedRow_InsteadOfDuplicate()
{
    using var ctx = TestDbContextFactory.Create();
    var repo = new FaWorkStepRepository(ctx);
    ctx.ProductionOrders.Add(new ProductionOrder { Id = 1, OrderNumber = "FA1" });
    ctx.WorkSteps.Add(new WorkStep { Id = 10, Code = "VL", Name = "Lueftung" });
    ctx.FaWorkSteps.Add(new FaWorkStep { ProductionOrderId = 1, WorkStepId = 10, IsRemoved = true, Source = "Sync" });
    await ctx.SaveChangesAsync();

    await repo.SetActiveAsync(1, 10, active: true, "tester", "win\\tester");

    var rows = ctx.FaWorkSteps.Where(f => f.ProductionOrderId == 1 && f.WorkStepId == 10).ToList();
    rows.Should().ContainSingle();
    rows[0].IsRemoved.Should().BeFalse();
    rows[0].Source.Should().Be("Manual");
}

[Fact]
public async Task GetWorkStepPivot_ReturnsCodeFlags_OnlyForActiveRows()
{
    using var ctx = TestDbContextFactory.Create();
    var repo = new FaWorkStepRepository(ctx);
    ctx.ProductionOrders.Add(new ProductionOrder { Id = 1, OrderNumber = "FA1" });
    ctx.WorkSteps.AddRange(new WorkStep { Id = 10, Code = "VL", Name = "L" }, new WorkStep { Id = 11, Code = "VK", Name = "K" });
    ctx.FaWorkSteps.AddRange(
        new FaWorkStep { ProductionOrderId = 1, WorkStepId = 10, IsRemoved = false },
        new FaWorkStep { ProductionOrderId = 1, WorkStepId = 11, IsRemoved = true });
    await ctx.SaveChangesAsync();

    var pivot = await repo.GetWorkStepPivotAsync(new List<int> { 1 });

    pivot[1].GetValueOrDefault("VL").Should().BeTrue();
    pivot[1].GetValueOrDefault("VK").Should().BeFalse();
}

[Fact]
public async Task SetIsCompleted_SetsAuditAndCompletedFields()
{
    using var ctx = TestDbContextFactory.Create();
    var repo = new FaWorkStepRepository(ctx);
    ctx.ProductionOrders.Add(new ProductionOrder { Id = 1, OrderNumber = "FA1" });
    ctx.WorkSteps.Add(new WorkStep { Id = 10, Code = "VL", Name = "L" });
    var row = new FaWorkStep { ProductionOrderId = 1, WorkStepId = 10 };
    ctx.FaWorkSteps.Add(row);
    await ctx.SaveChangesAsync();

    await repo.SetIsCompletedAsync(row.Id, true, "tester", "win\\tester");

    var reloaded = await ctx.FaWorkSteps.FindAsync(row.Id);
    reloaded!.IsCompleted.Should().BeTrue();
    reloaded.CompletedAt.Should().NotBeNull();
    reloaded.CompletedBy.Should().Be("tester");
}
```

- [ ] **Step 2: FAIL verifizieren** (`dotnet test --filter "FullyQualifiedName~FaWorkStepRepositoryTests"` — kompiliert erst nach Step 3, das ist der erwartete erste FAIL).
- [ ] **Step 3: Implementierung.** Pivot mit Chunking-Pattern aus dem ALTEN `ProductionOrderAssemblyGroupRepository.GetIsApplicablePivotAsync` (vor dem Loeschen lesen — `git show HEAD:...` falls schon geloescht): 1000er-Bloecke ueber `productionOrderIds`, Query `ctx.FaWorkSteps.Where(f => chunk.Contains(f.ProductionOrderId)).Select(f => new { f.ProductionOrderId, f.WorkStep.Code, f.IsRemoved })`, Ergebnis zu Dictionary aggregieren (`!IsRemoved` als Wert). `SetActiveAsync`: vorhandene Zeile (egal ob IsRemoved) laden → reaktivieren/deaktivieren + `Source=Manual` + Audit; sonst neue Zeile `Source=Manual`. `SetIsCompletedAsync`: `CompletedAt = value ? DateTime.Now : null`, `CompletedBy = value ? modifiedBy : null` + ModifiedAt/By/ByWindows.
- [ ] **Step 4: DI in Program.cs**: alte zwei Registrierungen ersetzen durch `AddScoped<IWorkStepRepository, WorkStepRepository>()` + `AddScoped<IFaWorkStepRepository, FaWorkStepRepository>()`.
- [ ] **Step 5: PASS + Commit** (ggf. gemeinsam mit Task 1, siehe dort).

---

### Task 4: Repositories FaAttribute (TDD)

**Files:**
- Create: `IdealAkeWms/Data/Repositories/IFaAttributeRepository.cs` + `FaAttributeRepository.cs`
- Modify: `IdealAkeWms/Program.cs` (DI)
- Test: `IdealAkeWms.Tests/Repositories/FaAttributeRepositoryTests.cs`

```csharp
public interface IFaAttributeRepository
{
    Task<List<FaAttributeDefinition>> GetAllAsync();                       // Include Options + WorkSteps
    Task<List<FaAttributeDefinition>> GetActiveForWorkStepsAsync(List<int> workStepIds); // via FaAttributeWorkSteps, distinct, SortOrder
    Task<FaAttributeDefinition?> GetByIdAsync(int id);
    Task AddDefinitionAsync(FaAttributeDefinition def);
    Task UpdateDefinitionAsync(FaAttributeDefinition def);
    Task<bool> DeleteDefinitionAsync(int id);                              // false wenn Values existieren
    Task AddOptionAsync(FaAttributeOption option);
    Task UpdateOptionAsync(FaAttributeOption option);
    Task<bool> DeleteOptionAsync(int id);                                  // false wenn Values referenzieren (dann nur IsActive=false setzen)
    Task SetWorkStepsAsync(int definitionId, List<int> workStepIds);       // Junction-Sync (add/remove Delta)
    Task<List<FaAttributeValue>> GetValuesByProductionOrderIdAsync(int productionOrderId); // Include Definition + SelectedOption
    Task UpsertValueAsync(int productionOrderId, int definitionId, int? selectedOptionId, bool? booleanValue,
        string modifiedBy, string modifiedByWindows);                      // null/null loescht die Wert-Zeile ("leer")
}
```

- [ ] **Step 1: Failing Tests** (3 Stueck):

```csharp
[Fact]
public async Task UpsertValue_CreatesUpdatesAndClearsRow()
{
    using var ctx = TestDbContextFactory.Create();
    var repo = new FaAttributeRepository(ctx);
    ctx.ProductionOrders.Add(new ProductionOrder { Id = 1, OrderNumber = "FA1" });
    var def = new FaAttributeDefinition { Id = 5, Name = "Verdampfergroesse", AttributeType = AttributeType.Dropdown };
    var opt = new FaAttributeOption { Id = 50, FaAttributeDefinitionId = 5, Value = "UKW 3/1" };
    ctx.FaAttributeDefinitions.Add(def); ctx.FaAttributeOptions.Add(opt);
    await ctx.SaveChangesAsync();

    await repo.UpsertValueAsync(1, 5, 50, null, "t", "w");
    ctx.FaAttributeValues.Should().ContainSingle(v => v.SelectedOptionId == 50);

    await repo.UpsertValueAsync(1, 5, null, null, "t", "w"); // "leer" -> Zeile weg
    ctx.FaAttributeValues.Should().BeEmpty();
}

[Fact]
public async Task DeleteOption_ReturnsFalse_WhenValuesReference()
{
    using var ctx = TestDbContextFactory.Create();
    var repo = new FaAttributeRepository(ctx);
    ctx.FaAttributeDefinitions.Add(new FaAttributeDefinition { Id = 5, Name = "X", AttributeType = AttributeType.Dropdown });
    ctx.FaAttributeOptions.Add(new FaAttributeOption { Id = 50, FaAttributeDefinitionId = 5, Value = "A" });
    ctx.ProductionOrders.Add(new ProductionOrder { Id = 1, OrderNumber = "FA1" });
    ctx.FaAttributeValues.Add(new FaAttributeValue { ProductionOrderId = 1, FaAttributeDefinitionId = 5, SelectedOptionId = 50 });
    await ctx.SaveChangesAsync();

    (await repo.DeleteOptionAsync(50)).Should().BeFalse();
    ctx.FaAttributeOptions.Should().ContainSingle();
}

[Fact]
public async Task GetActiveForWorkSteps_FiltersByJunctionAndActive()
{
    using var ctx = TestDbContextFactory.Create();
    var repo = new FaAttributeRepository(ctx);
    ctx.WorkSteps.Add(new WorkStep { Id = 10, Code = "VK", Name = "K" });
    ctx.FaAttributeDefinitions.AddRange(
        new FaAttributeDefinition { Id = 5, Name = "Zugeordnet", IsActive = true },
        new FaAttributeDefinition { Id = 6, Name = "Inaktiv", IsActive = false },
        new FaAttributeDefinition { Id = 7, Name = "NichtZugeordnet", IsActive = true });
    ctx.FaAttributeWorkSteps.AddRange(
        new FaAttributeWorkStep { FaAttributeDefinitionId = 5, WorkStepId = 10 },
        new FaAttributeWorkStep { FaAttributeDefinitionId = 6, WorkStepId = 10 });
    await ctx.SaveChangesAsync();

    var result = await repo.GetActiveForWorkStepsAsync(new List<int> { 10 });
    result.Should().ContainSingle(d => d.Name == "Zugeordnet");
}
```

- [ ] **Step 2: FAIL → Step 3: Implementierung → Step 4: PASS.** DI registrieren.
- [ ] **Step 5: Commit** `feat(repo): FaAttributeRepository (Definitionen, Optionen, Werte, AG-Zuordnung)`

---

### Task 5: Werkbank-AG-Mapping (TDD)

**Files:**
- Modify: `IdealAkeWms/Data/Repositories/IProductionWorkplaceRepository.cs` + `ProductionWorkplaceRepository.cs`
- Modify: `IdealAkeWms/Controllers/ProductionWorkplacesController.cs` (Edit GET/POST)
- Modify: `IdealAkeWms/Views/ProductionWorkplaces/Edit.cshtml`
- Test: bestehende `ProductionWorkplace*Tests` erweitern (+1 Repo-Test)

- [ ] **Step 1: Failing Test:**

```csharp
[Fact]
public async Task SetWorkSteps_SyncsJunctionRows()
{
    using var ctx = TestDbContextFactory.Create();
    var repo = new ProductionWorkplaceRepository(ctx);
    ctx.ProductionWorkplaces.Add(new ProductionWorkplace { Id = 1, Name = "A1" });
    ctx.WorkSteps.AddRange(new WorkStep { Id = 10, Code = "VK", Name = "K" }, new WorkStep { Id = 11, Code = "VL", Name = "L" });
    ctx.ProductionWorkplaceWorkSteps.Add(new ProductionWorkplaceWorkStep { ProductionWorkplaceId = 1, WorkStepId = 10 });
    await ctx.SaveChangesAsync();

    await repo.SetWorkStepsAsync(1, new List<int> { 11 }); // 10 raus, 11 rein

    ctx.ProductionWorkplaceWorkSteps.Should().ContainSingle(x => x.WorkStepId == 11);
}
```

- [ ] **Step 2: FAIL → Step 3:** Repo-Methoden `Task<List<int>> GetWorkStepIdsAsync(int workplaceId)` + `Task SetWorkStepsAsync(int workplaceId, List<int> workStepIds)` (Delta-Sync: fehlende adden, ueberzaehlige entfernen).
- [ ] **Step 4:** Controller Edit-GET laedt `AllWorkSteps` (aktive) + `SelectedWorkStepIds` ins bestehende ViewModel; Edit-POST nimmt `int[] workStepIds` entgegen und ruft `SetWorkStepsAsync`. View: Checkbox-Liste im Muster der bestehenden Edit-Form:

```html
<div class="mb-3">
    <label class="form-label">Vorbaugruppen / Arbeitsgaenge dieser Werkbank</label>
    @foreach (var ws in Model.AllWorkSteps)
    {
        <div class="form-check">
            <input class="form-check-input" type="checkbox" name="workStepIds" value="@ws.Id"
                   id="ws_@ws.Id" @(Model.SelectedWorkStepIds.Contains(ws.Id) ? "checked" : "") />
            <label class="form-check-label" for="ws_@ws.Id">@ws.Code — @ws.Name</label>
        </div>
    }
</div>
```

- [ ] **Step 5: PASS + Vollsuite + Commit** `feat(workplace): Werkbank-AG-Mapping (mehrfach)`

---

### Task 6: Toggle-APIs + Rolle vorbau (TDD)

**Files:**
- Create: `IdealAkeWms/Controllers/FaWorkStepsApiController.cs`
- Delete: `IdealAkeWms/Controllers/AssemblyGroupsApiController.cs` (+ Tests dazu)
- Create: `IdealAkeWms/Filters/RequireVorbauAccessAttribute.cs`
- Modify: `IdealAkeWms/Models/RoleKeys.cs` (+ `public const string Vorbau = "vorbau";`)
- Modify: `IdealAkeWms/Services/ICurrentUserService.cs` + `CurrentUserService.cs` (+ `HasVorbauAccessAsync()`: admin ODER vorbau — Muster der bestehenden Has*-Methoden exakt uebernehmen)
- Test: `IdealAkeWms.Tests/Controllers/FaWorkStepsApiControllerTests.cs`

- [ ] **Step 1:** Filter-Attribut — exakt das v1.20-Pattern (Vorlage `RequireMasterDataReadAccessAttribute.cs`):

```csharp
public class RequireVorbauAccessAttribute : TypeFilterAttribute
{
    public RequireVorbauAccessAttribute() : base(typeof(RequireVorbauAccessFilter)) { }
}

public class RequireVorbauAccessFilter : IAsyncActionFilter
{
    private readonly ICurrentUserService _currentUserService;
    public RequireVorbauAccessFilter(ICurrentUserService currentUserService) => _currentUserService = currentUserService;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!await _currentUserService.HasVorbauAccessAsync())
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
            return;
        }
        await next();
    }
}
```

- [ ] **Step 2: Failing Tests** (Muster der alten `AssemblyGroupsApiControllerTests` VOR dem Loeschen lesen):
  - `Toggle_CreatesRow_ForValidWorkStepCode` (POST → FaWorkStep aktiv)
  - `Toggle_ReturnsBadRequest_ForUnknownCode`
  - `ToggleCompleted_SetsIsCompleted`
- [ ] **Step 3: Controller:**

```csharp
[ApiController]
[Route("api/fa-work-steps")]
public class FaWorkStepsApiController : ControllerBase
{
    private readonly IFaWorkStepRepository _faWorkStepRepository;
    private readonly IWorkStepRepository _workStepRepository;
    private readonly ICurrentUserService _currentUserService;

    public FaWorkStepsApiController(IFaWorkStepRepository faWorkStepRepository,
        IWorkStepRepository workStepRepository, ICurrentUserService currentUserService)
    {
        _faWorkStepRepository = faWorkStepRepository;
        _workStepRepository = workStepRepository;
        _currentUserService = currentUserService;
    }

    public record ToggleRequest(int ProductionOrderId, string WorkStepCode, bool Value);
    public record ToggleCompletedRequest(int FaWorkStepId, bool Value);

    [HttpPost("toggle")]
    [RequirePickingOrFaCompletionAccess] // wie alter assembly-groups-Endpoint
    public async Task<IActionResult> Toggle([FromBody] ToggleRequest req)
    {
        var step = await _workStepRepository.GetByCodeAsync(req.WorkStepCode);
        if (step == null || !step.IsActive)
            return BadRequest(new { error = $"Unbekannter Arbeitsgang: {req.WorkStepCode}" });

        await _faWorkStepRepository.SetActiveAsync(req.ProductionOrderId, step.Id, req.Value,
            _currentUserService.GetDisplayName(), _currentUserService.GetWindowsUserName());
        return Ok();
    }

    [HttpPost("toggle-completed")]
    [RequireVorbauAccess]
    public async Task<IActionResult> ToggleCompleted([FromBody] ToggleCompletedRequest req)
    {
        var row = await _faWorkStepRepository.GetByIdAsync(req.FaWorkStepId);
        if (row == null) return NotFound();

        await _faWorkStepRepository.SetIsCompletedAsync(req.FaWorkStepId, req.Value,
            _currentUserService.GetDisplayName(), _currentUserService.GetWindowsUserName());
        return Ok();
    }
}
```

- [ ] **Step 4: PASS + Commit** (ggf. gemeinsam mit Task 1/3).

---

### Task 7: Stammdaten-View "Arbeitsgaenge" (TDD)

**Files:**
- Create: `IdealAkeWms/Controllers/WorkStepsController.cs`, `IdealAkeWms/Views/WorkSteps/Index.cshtml` + `Create.cshtml` + `Edit.cshtml`
- Modify: `IdealAkeWms/Views/Shared/_Layout.cshtml` (Stammdaten-Dropdown, masterdata_read-Block)
- Test: `IdealAkeWms.Tests/Controllers/WorkStepsControllerTests.cs`

- [ ] **Step 1:** Vorlage lesen: `ArticleCategoriesController` + Views (einfachster Stammdaten-CRUD-Fall mit v1.21-Filter). Controller: Class-Level `[RequireMasterDataReadAccess]`, Create/Edit/Delete-Actions `[RequireMasterDataAccess]`. Index mit Pflicht-Pattern: `PageSize.Resolve`, `ColumnFilterHelper.ReadFromQuery` + `Apply` vor Skip/Take, ColumnMap:

```csharp
private static readonly Dictionary<string, Func<WorkStep, string?>> ColumnMap = new(StringComparer.OrdinalIgnoreCase)
{
    ["code"] = w => w.Code,
    ["name"] = w => w.Name,
    ["search-string"] = w => w.SearchString,
    ["sort-order"] = w => w.SortOrder.ToString(),
    ["active"] = w => w.IsActive ? "Ja" : "Nein",
};
```

- [ ] **Step 2: Failing Tests:** `Index_ColumnFilter_FiltersAcrossAllRows` (3 WorkSteps, `?colf_code=VL` → 1 Item) + `Delete_Blocked_WhenInUse` (Repo liefert IsInUse → TempData WarningMessage, Redirect).
- [ ] **Step 3: FAIL → Implementierung → PASS.** Views: Tabelle `filterable-table` + `data-view-key="WorkSteps"` + `data-server-column-filter="true"`, alle th `data-col-key` + `data-filterable` (ausser actions), `@section Scripts` (column-preferences VOR table-filter), Edit-Buttons hinter `await _user.HasMasterDataAccessAsync()` (View-Inject-Muster aus ArticleCategories/Index).
- [ ] **Step 4:** Layout: Eintrag "Arbeitsgaenge" im masterdata_read-Block (neben Artikelkategorien).
- [ ] **Step 5: Vollsuite + Commit** `feat(view): Stammdaten Arbeitsgaenge (WorkSteps CRUD + Suchstring)`

---

### Task 8: Stammdaten-View "FA-Merkmale" (TDD)

**Files:**
- Create: `IdealAkeWms/Controllers/FaAttributesController.cs`, `IdealAkeWms/Views/FaAttributes/Index.cshtml` + `Edit.cshtml` (+ ggf. Create)
- Modify: `IdealAkeWms/Views/Shared/_Layout.cshtml`
- Test: `IdealAkeWms.Tests/Controllers/FaAttributesControllerTests.cs`

- [ ] **Step 1:** Vorlage VOLLSTAENDIG lesen: `ArticleAttributesController` + `Views/ArticleAttributes/*` (gleiches Definition/Option-Muster). Uebernehmen mit diesen Deltas:
  1. Entities/Repo: `FaAttributeDefinition`/`FaAttributeOption` via `IFaAttributeRepository`
  2. KEINE SyncSource/SyncFieldName-Felder
  3. ZUSAETZLICH in Edit: Checkbox-Liste "Zugeordnete Arbeitsgaenge" (aktive WorkSteps, `int[] workStepIds` → `SetWorkStepsAsync`) — Markup-Muster wie Task 5 Step 4
  4. Options-Delete nutzt `DeleteOptionAsync` und zeigt bei `false` WarningMessage "Option wird verwendet — nur deaktivieren moeglich"
  5. view-key `FaAttributes`, Route/Menue-Label "FA-Merkmale"
- [ ] **Step 2: Failing Tests:** `Index_ColumnFilter_FiltersAcrossAllRows` + `EditPost_SyncsWorkStepAssignment` (Mock-Repo verifiziert `SetWorkStepsAsync`-Aufruf mit erwarteter Id-Liste).
- [ ] **Step 3: FAIL → Implementierung → PASS → Vollsuite.**
- [ ] **Step 4: Commit** `feat(view): Stammdaten FA-Merkmale (Definitionen + Optionen + AG-Zuordnung)`

---

### Task 9: FaWorkStepDetectionService (TDD)

**Files:**
- Create: `IDEALAKEWMSService/Services/IFaWorkStepDetectionService.cs` + `FaWorkStepDetectionService.cs`
- Modify: `IdealAkeWms/Services/SyncLogger/SyncLogServices.cs` (+ Konstante `FaWorkStepDetection = "FaWorkStepDetection"` + in `All` aufnehmen)
- Modify: `IDEALAKEWMSService/Worker.cs` bzw. SyncWorker (Aufruf NACH BomCache-Sync, gated `Sync:FaWorkStepDetectionEnabled`, Default false) — bestehende Einbindung von `BomCacheSyncService` als Muster
- Modify: `IDEALAKEWMSService/appsettings.json` (+ `"FaWorkStepDetectionEnabled": false` im Sync-Block) + ServiceSettings-UI falls die anderen Sync-Flags dort gepflegt werden (pruefen: `ServiceSettingsController`)
- Test: `IDEALAKEWMSService.Tests/Services/FaWorkStepDetectionServiceTests.cs`

**Kern-Logik** (DbContext via shared `ApplicationDbContext`/`IDbContextFactory` — Muster `BdeAutoPauseWorker`; ISyncLogger als LETZTER Ctor-Parameter nach ILogger, Connection-Validation im try nach BeginRunAsync — Konventionen v1.15.2):

```csharp
public async Task<SyncResult> DetectAsync(bool dryRun, CancellationToken ct = default)
{
    await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.FaWorkStepDetection, ct);
    try
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(ct);

        // 1) Aktive WorkSteps mit Suchbegriffen
        var steps = await db.WorkSteps
            .Where(w => w.IsActive && w.SearchString != null && w.SearchString != "")
            .ToListAsync(ct);

        int added = 0, skipped = 0;
        foreach (var step in steps)
        {
            var terms = step.SearchString!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(t => t.ToLowerInvariant()).Distinct().ToList();
            if (terms.Count == 0) continue;

            // 2) Artikel deren BOM-Items einen Begriff enthalten (pro Begriff eine Query —
            //    ToLower().Contains statt EF.Functions.Like wegen InMemory-Tests).
            //    WICHTIG: exakte Property-/Nav-Namen aus Models/CachedBomItem.cs +
            //    CachedBomHeader.cs ablesen (Bezeichnung1/Bezeichnung2/Header.Artikelnummer)
            //    und hier anpassen falls abweichend.
            var matchedArticles = new HashSet<string>();
            foreach (var term in terms)
            {
                var arts = await db.CachedBomItems
                    .Where(i => (i.Bezeichnung1 != null && i.Bezeichnung1.ToLower().Contains(term))
                             || (i.Bezeichnung2 != null && i.Bezeichnung2.ToLower().Contains(term)))
                    .Select(i => i.Header.Artikelnummer)
                    .Distinct()
                    .ToListAsync(ct);
                foreach (var a in arts) matchedArticles.Add(a);
            }
            if (matchedArticles.Count == 0) continue;

            // 3) Offene FAs zu diesen Artikeln ohne vorhandene Zeile (auch keine IsRemoved!)
            var matchedFaCount = await db.ProductionOrders
                .Where(o => !o.IsDone && o.ArticleNumber != null && matchedArticles.Contains(o.ArticleNumber))
                .CountAsync(ct);
            var candidates = await db.ProductionOrders
                .Where(o => !o.IsDone && o.ArticleNumber != null && matchedArticles.Contains(o.ArticleNumber))
                .Where(o => !db.FaWorkSteps.Any(f => f.ProductionOrderId == o.Id && f.WorkStepId == step.Id))
                .Select(o => o.Id)
                .ToListAsync(ct);

            skipped += matchedFaCount - candidates.Count; // Zeile existiert bereits (aktiv oder IsRemoved)
            foreach (var poId in candidates)
            {
                if (!dryRun)
                    db.FaWorkSteps.Add(new FaWorkStep
                    {
                        ProductionOrderId = poId, WorkStepId = step.Id,
                        Source = FaWorkStepSources.Sync,
                        CreatedAt = DateTime.Now, CreatedBy = "FaWorkStepDetection", CreatedByWindows = "FaWorkStepDetection",
                    });
                added++;
            }
        }

        if (!dryRun) await db.SaveChangesAsync(ct);

        await run.FinishSuccessAsync(new Dictionary<string, int> { ["neu"] = added, ["uebersprungen"] = skipped },
            messageSuffix: dryRun ? "[DryRun]" : null, ct: ct);
        return new SyncResult(added, 0, 0);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Fehler bei FA-Arbeitsgang-Erkennung.");
        await run.LogErrorAsync(ex.Message, ct: ct);
        await run.FinishFailedAsync(ex.Message, ct: ct);
        throw;
    }
}
```

- [ ] **Step 1: Failing Tests** (InMemory + FakeSyncLogger, Muster bestehender Service-Tests):
  - `Detect_AddsFaWorkStep_WhenTermMatchesBezeichnung` (Item "Axialluefter 230V" + Term "luefter" → Zeile mit Source=Sync)
  - `Detect_MatchesSecondTerm_OfCommaList` (SearchString "Luefter,Ventilator", Item "Ventilator XY")
  - `Detect_DoesNotReAdd_WhenRowIsRemoved` (vorhandene IsRemoved-Zeile → kein Insert, keine Aenderung)
  - `Detect_SkipsClosedOrders_AndOrdersWithoutBomCache`
  - `Detect_DryRun_WritesNothing`
- [ ] **Step 2: FAIL → Step 3: Implementierung → Step 4: PASS.**
- [ ] **Step 5:** Worker-Einbindung + Settings + DI (Muster BomCache: gleiche Stelle im SyncWorker-Loop, direkt danach).
- [ ] **Step 6: Vollsuite + Commit** `feat(service): FaWorkStepDetection — AG-Erkennung aus BOM-Cache`

---

### Task 10: FaCompletion-Controller-Umbau (TDD)

**Files:**
- Modify: `IdealAkeWms/Controllers/FaCompletionController.cs` (komplett auf neue Repos)
- Modify: `IdealAkeWms/Models/ViewModels/FaCompletionListViewModel.cs` + `FaCompletionEditViewModel.cs`
- Test: `IdealAkeWms.Tests/Controllers/FaCompletionControllerTests.cs` (bestehende anpassen + neue)

- [ ] **Step 1:** Bestand lesen (Controller + beide ViewModels + Tests). Umbau:
  - `Index`: Pivot via `IFaWorkStepRepository.GetWorkStepPivotAsync`; Zaehler = aktive Zeilen / davon IsCompleted / Spec-Anzahl (ueber `GetByProductionOrderIdAsync` bzw. aggregierte Query); Badge-Flag `HasNoWorkplace = o.ProductionWorkplaceId == null`
  - `Edit` GET: laedt `GetByProductionOrderIdAsync(id)` (aktive Zeilen) als Kacheln; `AllWorkSteps` (aktiv, fuer "AG hinzufuegen"-Dropdown); `AvailableWorkplaces` (aktive ProductionWorkplaces) + aktuelle `ProductionWorkplaceId`; je Kachel die zugeordneten Merkmale (`GetActiveForWorkStepsAsync`) + aktuelle Werte (`GetValuesByProductionOrderIdAsync`)
  - NEU `SetWorkplace` POST (`int id, int? workplaceId`): schreibt `ProductionOrders.ProductionWorkplaceId` + Audit, Redirect zurueck zu Edit
  - NEU `SaveAttributeValue` POST (`int id, int definitionId, int? optionId, bool? boolValue`): `UpsertValueAsync`, Redirect Edit (oder Ok() fuer AJAX — Form-POST reicht, kein AJAX im ersten Wurf)
  - `AddSpec`/`EditSpec`/`DeleteSpec`: auf `FaWorkStepSpec` + `IFaWorkStepRepository`-Spec-Methoden umgestellt (`FaWorkStepId` statt `AssemblyGroupId`)
  - `ToggleIsCompleted`: ruft `SetIsCompletedAsync(faWorkStepId, ...)`
- [ ] **Step 2: Failing Tests:** `Edit_LoadsActiveFaWorkStepsWithAttributes`, `SetWorkplace_UpdatesProductionOrder`, `SaveAttributeValue_UpsertsValue` (Mock-Verify), bestehende Spec-CRUD-Tests auf neue Typen umgestellt.
- [ ] **Step 3: FAIL → Implementierung → PASS → Vollsuite.**
- [ ] **Step 4: Commit** `feat(fa-completion): Controller auf FaWorkSteps + Merkmale + Werkbank umgebaut`

---

### Task 11: FaCompletion-Views-Umbau

**Files:**
- Modify: `IdealAkeWms/Views/FaCompletion/Edit.cshtml` + `Index.cshtml`

- [ ] **Step 1:** `Edit.cshtml`:
  - Kopfbereich NEU: Werkbank-Form (Dropdown `workplaceId` aus `AvailableWorkplaces`, vorausgewaehlt; Submit → `SetWorkplace`); Hinweis-Badge wenn leer: `<span class="badge bg-warning text-dark">Keine Werkbank zugewiesen</span>`
  - Kachel-Tabs: iterieren ueber `Model.FaWorkSteps` (statt 5 fixe); Toggle-Checkbox `data-endpoint="/api/fa-work-steps/toggle"` + `data-work-step-code="@step.Code"` (Inline-JS des Toggle-Handlers entsprechend anpassen — Request-Body `{ productionOrderId, workStepCode, value }`)
  - "AG hinzufuegen": Dropdown der noch nicht aktiven WorkSteps + Button → POST auf neuen Controller-Action `AddWorkStep(int id, int workStepId)` der `SetActiveAsync(id, workStepId, true, ...)` ruft (in Task 10 ergaenzen falls nicht geschehen)
  - Pro Kachel Merkmal-Block VOR der Spec-Tabelle:

```html
@foreach (var attr in tab.Attributes)
{
    <form asp-action="SaveAttributeValue" method="post" class="row g-2 align-items-center mb-2">
        @Html.AntiForgeryToken()
        <input type="hidden" name="id" value="@Model.ProductionOrderId" />
        <input type="hidden" name="definitionId" value="@attr.DefinitionId" />
        <label class="col-sm-3 col-form-label">@attr.Name</label>
        <div class="col-sm-6">
            @if (attr.AttributeType == AttributeType.Dropdown)
            {
                <select name="optionId" class="form-select" onchange="this.form.submit()">
                    <option value="">(leer)</option>
                    @foreach (var opt in attr.Options.Where(o => o.IsActive || o.Id == attr.SelectedOptionId))
                    {
                        <option value="@opt.Id" selected="@(opt.Id == attr.SelectedOptionId)">@opt.Value</option>
                    }
                </select>
            }
            else
            {
                <select name="boolValue" class="form-select" onchange="this.form.submit()">
                    <option value="">(leer)</option>
                    <option value="true" selected="@(attr.BooleanValue == true)">JA</option>
                    <option value="false" selected="@(attr.BooleanValue == false)">NEIN</option>
                </select>
            }
        </div>
    </form>
}
```

  - Spec-Tabelle: Form-Felder von `AssemblyGroupId` auf `FaWorkStepId` umbenannt
- [ ] **Step 2:** `Index.cshtml`: Spalten/Zaehler unveraendert (Pivot kommt aus neuem Repo), zusaetzlich Badge-Spalte "Werkbank" mit Warn-Badge wenn leer.
- [ ] **Step 3:** Build + manueller Smoke (View rendert), Vollsuite, Commit `feat(view): FaCompletion Edit/Index auf FaWorkSteps + Merkmale`

---

### Task 12: Leitstand-Umstellung (TDD)

**Files:**
- Modify: `IdealAkeWms/Controllers/PickingLeitstandController.cs` (Zeile ~90: Pivot-Aufruf; Mapping Zeilen ~122-127)
- Modify: `IdealAkeWms/Views/PickingLeitstand/Index.cshtml` (Toggle-Checkboxen VK-VA, Zeilen ~250-282)
- Test: `PickingLeitstandControllerTests` anpassen

- [ ] **Step 1:** Controller: `_assemblyGroupRepository.GetIsApplicablePivotAsync` → `_faWorkStepRepository.GetWorkStepPivotAsync` (DI-Feld tauschen). Mapping bleibt identisch (`grp.GetValueOrDefault("VK")` etc. — Pivot liefert weiterhin Code→bool).
- [ ] **Step 2:** View: die 5 Toggle-Checkboxen von `data-endpoint="/api/assembly-groups/toggle-applicable"` + `data-group-key` auf `data-endpoint="/api/fa-work-steps/toggle"` + `data-work-step-code` umstellen; den Inline-JS-Dispatcher (Zeile ~591) auf den neuen Endpoint + Request-Shape anpassen (`{ productionOrderId, workStepCode, value }`).
- [ ] **Step 3:** Tests: bestehende Pivot-Mapping-Tests auf neues Repo-Mock umstellen; FAIL→PASS.
- [ ] **Step 4: Vollsuite + Commit** `refactor(leitstand): VK-VA-Spalten lesen FaWorkSteps-Pivot`

---

### Task 13: FA-Abarbeitungsliste (TDD)

**Files:**
- Create: `IdealAkeWms/Controllers/FaWorklistController.cs`, `IdealAkeWms/Views/FaWorklist/Index.cshtml`, `IdealAkeWms/Models/ViewModels/FaWorklistViewModel.cs`
- Modify: `IdealAkeWms/Views/Shared/_Layout.cshtml` (Menue, Task 15 buendelt)
- Test: `IdealAkeWms.Tests/Controllers/FaWorklistControllerTests.cs`

- [ ] **Step 1: ViewModel:**

```csharp
public class FaWorklistViewModel
{
    public int? SelectedWorkplaceId { get; set; }
    public List<ProductionWorkplace> AvailableWorkplaces { get; set; } = new();
    public List<WorkStep> MappedWorkSteps { get; set; } = new();          // Spalten dieser Werkbank
    public List<FaAttributeDefinition> AttributeColumns { get; set; } = new(); // aktive Defs der gemappten AGs
    public bool ShowDone { get; set; }
    public List<FaWorklistRow> Items { get; set; } = new();
    public Dictionary<string, List<EnaioDmsDocumentLink>> EnaioDmsLinks { get; set; } = new();
    public PaginationState Pagination { get; set; } = new();
}

public class FaWorklistRow
{
    public int ProductionOrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string? ArticleNumber { get; set; }
    public decimal Quantity { get; set; }
    public DateTime? VorkommissionierTermin { get; set; }  // BG-Termin
    public DateTime? KommissionierTermin { get; set; }
    public DateTime? ProductionDate { get; set; }
    public Dictionary<int, string?> AttributeValues { get; set; } = new();        // DefinitionId -> Anzeigetext
    public Dictionary<int, FaWorklistCell> WorkStepCells { get; set; } = new();   // WorkStepId -> Zelle
    public int OrphanWorkStepCount { get; set; }           // offene AGs ausserhalb des Mappings
}

public class FaWorklistCell
{
    public int FaWorkStepId { get; set; }
    public bool IsCompleted { get; set; }
}
```

- [ ] **Step 2: Controller-Logik** (`[RequireVorbauAccess]` class-level; Feature-Gate `FaCompletionAktiv` wie FaCompletionController — dessen Gate-Code als Muster):
  1. `workplaceId` (Pflicht; ohne Auswahl: nur Dropdown rendern, leere Liste), `showDone`, `page`, `pageSize`, `ColumnFilterHelper.ReadFromQuery`
  2. Gemappte WorkStepIds der Werkbank (`GetWorkStepIdsAsync`), deren WorkSteps + `AttributeColumns` (`GetActiveForWorkStepsAsync`)
  3. Offene FAs (`!IsDone`) mit `ProductionWorkplaceId == workplaceId`, die mind. eine aktive FaWorkStep-Zeile mit WorkStepId ∈ Mapping haben; `showDone=false`: zusaetzlich mind. eine davon `!IsCompleted`
  4. Termine via `BusinessDayService` (KommissionierTage/VorkommissionierTage — exakt der Code aus `ProductionOrdersController.Index` Zeilen 87-135 als Vorlage, OHNE Beschichtung)
  5. Merkmal-Werte bulk laden, Anzeigetext: Dropdown → Option.Value, Boolean → "JA"/"NEIN", fehlend → ""
  6. `OrphanWorkStepCount` = aktive, nicht erledigte FaWorkSteps des FA mit WorkStepId ∉ Mapping
  7. ColumnMap (gerenderte Texte!) + Apply VOR Pagination, Termine vorher berechnen (Fallstrick Termine-vor-Pagination):

```csharp
private static string FormatDateForFilter(DateTime? d) // KW-Format wie PickingControllerTests-Vorlage
    => d == null ? string.Empty : $"{d:dd.MM.yyyy} KW{System.Globalization.ISOWeek.GetWeekOfYear(d.Value)}".ToLowerInvariant();

// ColumnMap-Keys: order-number, article-number, quantity, bg-date, picking-date, production-date
// + je Merkmal-Spalte dynamisch "attr-{DefinitionId}" -> AttributeValues-Text
```

  8. enaio-Links: `GetByOrderNumbersAsync` (wie ProductionOrdersController Zeile ~156)
- [ ] **Step 3: Failing Tests:**
  - `Index_FiltersByWorkplaceAndMapping` (2 FAs auf Werkbank, nur einer mit gemapptem AG → 1 Row)
  - `Index_HidesFullyCompleted_UnlessShowDone`
  - `Index_CountsOrphanWorkSteps`
  - `Index_ColumnFilter_FiltersAcrossAllRows` (Filter auf order-number)
- [ ] **Step 4: FAIL → Implementierung → PASS.**
- [ ] **Step 5: View:** Pflicht-Pattern komplett (`page-header`, filter-card mit Werkbank-Dropdown + showDone-Checkbox, `table-responsive`, `filterable-table data-view-key="FaWorklist" data-server-column-filter="true"`, alle th `data-col-key`, Datums-th `data-date-filter`, `_Pagination`, Scripts-Section). Zellen:

```html
@foreach (var ws in Model.MappedWorkSteps)
{
    <td class="text-center">
        @if (item.WorkStepCells.TryGetValue(ws.Id, out var cell))
        {
            <input type="checkbox" class="form-check-input worklist-complete"
                   data-fa-work-step-id="@cell.FaWorkStepId" @(cell.IsCompleted ? "checked" : "") />
        }
    </td>
}
<!-- FA-Nr-Zelle: -->
<td>
    <a asp-action="Bom" asp-route-id="@item.ProductionOrderId">@item.OrderNumber</a>
    @if (item.OrphanWorkStepCount > 0)
    {
        <span class="badge bg-warning text-dark ms-1" title="Offene Arbeitsgaenge ausserhalb dieser Werkbank">+@item.OrphanWorkStepCount weitere AG</span>
    }
</td>
```

  Inline-JS: delegierter change-Handler auf `.worklist-complete` → `fetch('/api/fa-work-steps/toggle-completed', { method:'POST', headers Content-Type json + RequestVerificationToken falls der API-Controller [ValidateAntiForgeryToken] nutzt (an FaWorkStepsApiController ausrichten — API-Controller im Projekt nutzen das NICHT, also weglassen), body: JSON.stringify({ faWorkStepId, value: checked }) })`; bei Fehler Checkbox zuruecksetzen + Alert-Banner (Muster Leitstand-Inline-JS).
- [ ] **Step 6: Vollsuite + Commit** `feat(view): FA-Abarbeitungsliste je Werkbank`

---

### Task 14: Read-only Stueckliste (TDD)

**Files:**
- Modify: `IdealAkeWms/Controllers/FaWorklistController.cs` (+ `Bom`-Action)
- Modify: `IdealAkeWms/Controllers/PickingController.cs` (Bom-Action: ViewModel-Flag setzen, Logik extrahieren falls noetig)
- Modify: `IdealAkeWms/Models/ViewModels/BomViewModel.cs` (+ `public bool ReadOnly { get; set; }`)
- Modify: `IdealAkeWms/Views/Picking/Bom.cshtml`
- Test: `FaWorklistControllerTests` (+1)

- [ ] **Step 1:** `PickingController.Bom`-Kernlogik in eine wiederverwendbare Stelle bringen: einfachster Weg — `FaWorklistController.Bom(int id)` ruft einen neuen shared Service ODER dupliziert den Lade-Code NICHT, sondern: `BomViewModel`-Aufbau aus PickingController in eine `internal static`/Service-Methode `BomViewModelBuilder` extrahieren ist Overkill — pragmatisch: `FaWorklistController.Bom` laedt wie `PickingController.Bom` (BomRepository + Order), setzt aber `ReadOnly = true` und ueberspringt die Picking-spezifischen Teile (gespeicherte SourceStorageLocationIds, Lagerplatz-Suggests). Beim Implementieren `PickingController.Bom` (Zeilen 204-280+) GENAU lesen und nur die BOM-Anzeige-Teile uebernehmen; `return View("~/Views/Picking/Bom.cshtml", vm);`
- [ ] **Step 2:** View-Conditionals: `@if (!Model.ReadOnly) { ... }` um (a) Picking-Checkbox-Spalte, (b) Quell-/Ziel-Lagerplatz-Dropdowns + Umbuchen-Button, (c) Foto-Upload, (d) Bedarfsmeldungs-Modal + Buttons. Filter, Baugruppen-Toggle, Druck-Button bleiben. Im Inline-JS die Picking-Initialisierung hinter `const readOnly = @Json.Serialize(Model.ReadOnly);` + early-return legen.
- [ ] **Step 3: Failing Test:** `Bom_ReturnsReadOnlyViewModel` (vorbau-Kontext, ViewModel.ReadOnly == true). FAIL → Implementierung → PASS.
- [ ] **Step 4: Vollsuite + Commit** `feat(view): Stueckliste read-only fuer Abarbeitungsliste`

---

### Task 15: Menue + RoleOverview + AgentJob + Cutover-Doc

**Files:**
- Modify: `IdealAkeWms/Views/Shared/_Layout.cshtml`
- Modify: `IdealAkeWms/Views/Users/RoleOverview.cshtml` (hand-gepflegt!)
- Modify: `SQL/AgentJobs/01_Import_Produktionsauftraege.sql`
- Create: `docs/superpowers/cutover/2026-06-XX-fa-vorbau-cutover.md`

- [ ] **Step 1: Layout:** Menue-Block "Fertigungsauftraege": FA-Liste (bestehender Eintrag hierher), "FA-Vervollstaendigen" (von altem Standort verschieben, Gate `FaCompletionAktiv` + fa_completion wie bisher), "FA-Abarbeitungsliste" (Gate `FaCompletionAktiv` + `await CurrentUserService.HasVorbauAccessAsync()`). Bestehende Sichtbarkeits-Helper-Muster im Layout exakt uebernehmen.
- [ ] **Step 2: RoleOverview:** Zeile fuer `vorbau` ergaenzen (Beschreibung + Module: FA-Abarbeitungsliste, Stueckliste read-only).
- [ ] **Step 3: AgentJob:** Folge-MERGE 3 (Zeilen 127-143, `ProductionOrderAssemblyGroups` CROSS JOIN) ERSATZLOS entfernen; Kommentar-Hinweis einfuegen: `-- AssemblyGroups-MERGE entfernt in v1.22.0 (FaWorkSteps via Detection-Sync, siehe Spec 2026-06-12)`.
- [ ] **Step 4: Cutover-Doc** (Muster `docs/superpowers/cutover/2026-05-12-production-order-split-phase-1-cutover.md`): Reihenfolge im Wartungsfenster: (1) DB-Backup, (2) AgentJob-Skript auf neuem Stand einspielen/Job pausieren, (3) App-Deploy (Migration laeuft via `db.Database.Migrate()`), (4) AgentJob reaktivieren, (5) Smoke: FA-Import-Job 1x manuell, FaCompletion oeffnen, Leitstand-Toggles testen, (6) `Sync:FaWorkStepDetectionEnabled` aktivieren + Suchstrings pflegen.
- [ ] **Step 5: Build + Vollsuite + Commit** `feat(menu): Fertigungsauftraege-Block + vorbau-Rolle + AgentJob-Cutover`

---

### Task 16: Doku + Version v1.22.0

**Files:**
- Modify: `IdealAkeWms/AppVersion.cs` + `IDEALAKEWMSService/AppVersion.cs` → `1.22.0`
- Modify: `IdealAkeWms/Views/Help/Changelog.cshtml` (neue Card: Arbeitsgaenge-Katalog, FA-zu-AG mit Auto-Erkennung, Vorbau-Merkmale, Abarbeitungsliste, Rolle vorbau, Menue-Umbau)
- Modify: `IdealAkeWms/Views/Help/Index.cshtml` (bzw. Hilfe-Struktur pruefen): NEUE Hilfe-Abschnitte mit konkreten Bedien-Details (Pflicht laut Projektregel): "Arbeitsgaenge pflegen" (Suchstring-Syntax: kommasepariert, Contains, case-insensitive), "FA-Merkmale pflegen", "FA-Vervollstaendigen neu" (Werkbank, AG hinzufuegen/abwaehlen, Merkmale), "FA-Abarbeitungsliste" (Werkbank-Filter, Erledigt-Haken, +N-Badge, read-only Stueckliste)
- Modify: `CLAUDE.md`: Zugriffsschutz-Tabelle (+RequireVorbauAccess), Rollen-Tabelle (+vorbau), AppSettings unveraendert, Service-Settings (+`Sync:FaWorkStepDetectionEnabled`), Fallstricke: (1) "FaWorkSteps ersetzt ProductionOrderAssemblyGroups (v1.22.0)" — IsRemoved-Semantik, Detection nur-hinzufuegen, AgentJob-MERGE entfernt; (2) "Detection haengt NICHT am ContentHash-Pfad"; (3) Leitstand-Spalten statisch trotz Katalog
- Modify: `docs/TESTSZENARIEN.md`: neues Kapitel (End-to-End: Suchstring pflegen → Detection-Lauf → FA-Vervollstaendigen prueft erkannte AGs + Merkmale erfassen + Werkbank setzen → Werkbank-Mapping pflegen → Abarbeitungsliste filtern + abhaken + Orphan-Badge + read-only Stueckliste → FaCompletion zeigt Erledigt; Negativfaelle: IsRemoved-Re-Add-Sperre, Option-Loesch-Guard)
- Modify: `PROJECT_STATUS.md`

- [ ] **Step 1:** Alle Dateien anpassen (Formate der Bestandsdateien exakt uebernehmen).
- [ ] **Step 2:** Build + Vollsuite + Commit `docs: Version v1.22.0 + Changelog + Hilfe + CLAUDE.md + TESTSZENARIEN`

---

### Task 17: Final-Check + Final-Review

- [ ] **Step 1:** `dotnet build IdealAkeWms.slnx` → 0 Fehler; `dotnet test IdealAkeWms.slnx --no-build` → alle gruen (Baseline + ~20 neue).
- [ ] **Step 2:** Sanity-Greps: keine Referenz mehr auf `ProductionOrderAssemblyGroup`/`AssemblyGroupSpec`/`assembly-groups` (ausser Changelog/Spec-Historie); `SQL/68` MigrationId == generierter Migrationsname; FreshInstall enthaelt alle 8 Tabellen + Seeds + History-Eintrag.
- [ ] **Step 3:** Final-Review-Subagent (read-only) ueber die komplette Range: Spec-Abdeckung Sektion fuer Sektion, Count-/Filter-Konsistenz der Worklist, Migration-Idempotenz, Cutover-Doc-Vollstaendigkeit.

### Task 18: PAUSE — User-Test + Merge (NICHT autonom)

- [ ] User testet manuell (TESTSZENARIEN-Kapitel). Cutover-Doc fuer Produktions-Deploy beachten (AgentJob!). Merge in main + Worktree-Cleanup NUR nach expliziter User-Bestaetigung (Memory-Regel `feedback_worktree_cleanup_ask_first`).
