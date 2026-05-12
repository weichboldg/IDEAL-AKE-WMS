# StorageLocation `IstBuchbar`-Flag — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** User-gesteuertes `IstBuchbar`-Flag auf `StorageLocation` einbauen, damit der Admin Lagerplaetze fuer UI-Buchungs-Dropdowns explizit freischalten kann. Sage-importierte Plaetze sind by default nicht buchbar.

**Architecture:** Neues Bool-Feld `IstBuchbar` (Default true im Code, Default 1 in DB). Migration setzt existing Sage-Records auf 0. Sage-Sync setzt INSERT auf false, UPDATE laesst es unangetastet. Repository-Filter `GetActive*Async` um `&& IstBuchbar` erweitert. UI: Toggle in Edit-Maske (immer editierbar), Spalte+Toggle in Index, "nicht buchbar"-Badge in Bestandsuebersicht.

**Tech Stack:** .NET 10, EF Core 10, SQL Server, xUnit + FluentAssertions + InMemory-DB. Bewaehrte Patterns aus Phase 1+2 wiederverwendet (sync user, audit fields, repository pattern).

**Branch:** `feature/sage-lagerbestand-sync` — direkt auf den existierenden Phase-2-Branch committen, kein neuer Branch.

**Spec:** [docs/superpowers/specs/2026-05-07-storagelocation-istbuchbar-design.md](../specs/2026-05-07-storagelocation-istbuchbar-design.md)

**Commit-Konvention:** `feat(lagerplatz): ...` / `test(lagerplatz): ...` / `docs: ...`. Co-Authored-By trailer.

---

## Task 1: Schema-Erweiterung + Migration 58

**Files:**
- Modify: `IdealAkeWms/Models/StorageLocation.cs`
- Modify: `IdealAkeWms/Data/ApplicationDbContext.cs`
- Create: `IdealAkeWms/Migrations/<timestamp>_AddStorageLocationIstBuchbar.cs` (per `dotnet ef`)
- Create: `SQL/58_AddStorageLocationIstBuchbar.sql`
- Modify: `SQL/00_FreshInstall.sql`

- [ ] **Step 1: Property auf StorageLocation hinzufuegen**

In `IdealAkeWms/Models/StorageLocation.cs`, nach `IsActive`, einfuegen:

```csharp
[Display(Name = "Buchbar")]
public bool IstBuchbar { get; set; } = true;
```

- [ ] **Step 2: EF-Konfiguration erweitern**

In `IdealAkeWms/Data/ApplicationDbContext.cs`, im `modelBuilder.Entity<StorageLocation>`-Block, nach `entity.HasIndex(e => e.Source);` ergaenzen:

```csharp
entity.Property(e => e.IstBuchbar).HasDefaultValue(true);
entity.HasIndex(e => e.IstBuchbar);
```

- [ ] **Step 3: EF-Migration generieren**

```pwsh
dotnet ef migrations add AddStorageLocationIstBuchbar --project IdealAkeWms
```

Expected: zwei neue Dateien `*_AddStorageLocationIstBuchbar.cs` + `.Designer.cs`. Migration enthaelt `AddColumn` fuer `IstBuchbar` (bit, NOT NULL, default true) plus `CreateIndex IX_StorageLocations_IstBuchbar`.

- [ ] **Step 4: SQL/58-Skript erstellen**

Datei `SQL/58_AddStorageLocationIstBuchbar.sql`:

```sql
-- SQL/58_AddStorageLocationIstBuchbar.sql
-- Phase-2-Erweiterung: User-gesteuertes IstBuchbar-Flag.
-- Default 1 (alle bestehenden Manual-Plaetze bleiben buchbar).
-- Initial-UPDATE setzt existing Sage-Records auf 0 — NUR beim ersten Migrations-Lauf,
-- geschuetzt durch Migrations-History-Guard.

IF COL_LENGTH('dbo.StorageLocations', 'IstBuchbar') IS NULL
BEGIN
    ALTER TABLE dbo.StorageLocations
        ADD IstBuchbar BIT NOT NULL CONSTRAINT DF_StorageLocations_IstBuchbar DEFAULT 1;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
    WHERE name = 'IX_StorageLocations_IstBuchbar'
      AND object_id = OBJECT_ID('dbo.StorageLocations'))
BEGIN
    CREATE INDEX IX_StorageLocations_IstBuchbar ON dbo.StorageLocations(IstBuchbar);
END
GO

-- Initial-Setup: existing Sage-Records auf 0 — NUR beim ersten Lauf.
IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory
    WHERE MigrationId = '<TIMESTAMP>_AddStorageLocationIstBuchbar')
BEGIN
    UPDATE dbo.StorageLocations SET IstBuchbar = 0 WHERE Source = 'Sage';
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory
    WHERE MigrationId = '<TIMESTAMP>_AddStorageLocationIstBuchbar')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    SELECT '<TIMESTAMP>_AddStorageLocationIstBuchbar', '10.0.2';
END
GO
```

`<TIMESTAMP>` durch echten Timestamp aus dem in Step 3 generierten Migrations-Dateinamen ersetzen.

- [ ] **Step 5: FreshInstall.sql aktualisieren**

In `SQL/00_FreshInstall.sql`:
- StorageLocations-CREATE-TABLE: `[IstBuchbar] BIT NOT NULL DEFAULT 1` ergaenzen (nach `[IsActive]`).
- Indizes-Block: `CREATE INDEX IX_StorageLocations_IstBuchbar ON [dbo].[StorageLocations](IstBuchbar);` einfuegen.
- `__EFMigrationsHistory`-Insert-Liste am Ende: neuen MigrationId-Eintrag.

KEIN UPDATE-Statement noetig (FreshInstall = leere DB).

- [ ] **Step 6: Build + bestaetige Tests**

```pwsh
dotnet build --nologo
dotnet test --nologo
```

Expected: `0 Fehler`. Bestehende 555 Tests sollten alle gruen sein — `IstBuchbar` Default `true` bedeutet, dass kein existing Test einen Lagerplatz seedet, der durch fehlendes `IstBuchbar` schief liegt.

- [ ] **Step 7: Commit**

```pwsh
git add IdealAkeWms/Models/StorageLocation.cs IdealAkeWms/Data/ApplicationDbContext.cs IdealAkeWms/Migrations/ SQL/58_AddStorageLocationIstBuchbar.sql SQL/00_FreshInstall.sql
git commit -m "feat(lagerplatz): add IstBuchbar column with sage-default-false initial migration" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 2: Repository-Filter erweitern + Tests

**Files:**
- Modify: `IdealAkeWms/Data/Repositories/StorageLocationRepository.cs`
- Modify: `IdealAkeWms.Tests/Repositories/StorageLocationRepositoryTests.cs`

- [ ] **Step 1: Tests schreiben (failing)**

In `IdealAkeWms.Tests/Repositories/StorageLocationRepositoryTests.cs` zwei neue Tests am Ende der Klasse (vor den private Helpers):

```csharp
[Fact]
public async Task GetActiveOrderedExcludingPickingTransport_FiltersNonBookable()
{
    using var ctx = TestDbContextFactory.Create();
    ctx.StorageLocations.AddRange(
        New("AKTIV-BUCH",     isActive: true,  isPickingTransport: false, istBuchbar: true),
        New("AKTIV-NICHT",    isActive: true,  isPickingTransport: false, istBuchbar: false),
        New("INAKTIV-BUCH",   isActive: false, isPickingTransport: false, istBuchbar: true)
    );
    await ctx.SaveChangesAsync();
    var repo = new StorageLocationRepository(ctx);

    var result = await repo.GetActiveOrderedExcludingPickingTransportAsync();

    result.Should().ContainSingle();
    result[0].Code.Should().Be("AKTIV-BUCH");
}

[Fact]
public async Task GetActivePickingTransportLocations_FiltersNonBookable()
{
    using var ctx = TestDbContextFactory.Create();
    ctx.StorageLocations.AddRange(
        New("WAGEN-BUCH",   isActive: true, isPickingTransport: true,  istBuchbar: true),
        New("WAGEN-NICHT",  isActive: true, isPickingTransport: true,  istBuchbar: false)
    );
    await ctx.SaveChangesAsync();
    var repo = new StorageLocationRepository(ctx);

    var result = await repo.GetActivePickingTransportLocationsAsync();

    result.Should().ContainSingle().Which.Code.Should().Be("WAGEN-BUCH");
}
```

Existing `New(...)` Helper-Methode am Ende der Klasse erweitern, sodass sie `istBuchbar` als Parameter akzeptiert (mit Default `true` fuer Rueckwaertskompatibilitaet zu existing Tests):

```csharp
private static StorageLocation New(string code, bool isActive, bool isPickingTransport, bool istBuchbar = true) => new()
{
    Code = code,
    BarcodeValue = code,
    IsActive = isActive,
    IsPickingTransport = isPickingTransport,
    IstBuchbar = istBuchbar,
    Source = StorageLocationSource.Manual,
    CreatedBy = "tester",
    CreatedByWindows = "tester"
};
```

WICHTIG: Wenn die existing Klasse keinen `New`-Helper hat (nur inline-Construction), erst lesen und entsprechend anpassen.

- [ ] **Step 2: Tests laufen — beide FAIL erwartet**

```pwsh
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "FiltersNonBookable" --nologo
```

Expected: 2 FAIL — Repository filtert noch nicht auf `IstBuchbar`, daher kommen alle aktiven Plaetze zurueck.

- [ ] **Step 3: Repository-Filter erweitern**

In `IdealAkeWms/Data/Repositories/StorageLocationRepository.cs`:

```csharp
public async Task<List<StorageLocation>> GetActiveOrderedExcludingPickingTransportAsync()
{
    return await _dbSet
        .Where(sl => sl.IsActive && !sl.IsPickingTransport && sl.IstBuchbar)
        .OrderBy(sl => sl.Code)
        .ToListAsync();
}

public async Task<List<StorageLocation>> GetActivePickingTransportLocationsAsync()
{
    return await _dbSet
        .Where(sl => sl.IsActive && sl.IsPickingTransport && sl.IstBuchbar)
        .OrderBy(sl => sl.Code)
        .ToListAsync();
}
```

`GetAllOrderedAsync` (Stammdaten-View) bleibt unveraendert — zeigt alle Plaetze.

- [ ] **Step 4: Tests laufen — beide PASS**

```pwsh
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "StorageLocationRepositoryTests" --nologo
```

Expected: alle existing + die zwei neuen PASS. Achte besonders darauf, dass die existing `New()`-Aufrufe (mit nur 2 Parametern) noch durch den Default-Parameter `istBuchbar=true` kompilieren.

- [ ] **Step 5: Vollstaendiger Test-Lauf**

```pwsh
dotnet test --nologo
```

Expected: alles gruen, +2 neue Tests.

- [ ] **Step 6: Commit**

```pwsh
git add IdealAkeWms/Data/Repositories/StorageLocationRepository.cs IdealAkeWms.Tests/Repositories/StorageLocationRepositoryTests.cs
git commit -m "feat(lagerplatz): filter booking-dropdowns on IstBuchbar" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 3: Sage-Sync respektiert IstBuchbar + Tests

**Files:**
- Modify: `IDEALAKEWMSService/Services/LagerplatzSyncService.cs`
- Modify: `IDEALAKEWMSService.Tests/Services/LagerplatzSyncServiceTests.cs`

- [ ] **Step 1: Tests schreiben (failing)**

In `IDEALAKEWMSService.Tests/Services/LagerplatzSyncServiceTests.cs` drei neue Tests anhaengen:

```csharp
[Fact]
public async Task Run_NewSageRecord_SetsIstBuchbarFalse()
{
    var (svc, reader, ctx, _) = Build();
    reader.Records = new() { new("HALLE-1", "NEU-1", "Neuer Sage-Platz") };

    await svc.RunAsync();

    var loc = ctx.StorageLocations.Single();
    loc.IstBuchbar.Should().BeFalse();
}

[Fact]
public async Task Run_ExistingSageRecord_PreservesIstBuchbarOnUpdate()
{
    var (svc, reader, ctx, _) = Build();
    ctx.StorageLocations.Add(new StorageLocation
    {
        Code = "EXIST-1", Zone = "HALLE-1", Description = "Alte Bezeichnung",
        BarcodeValue = "EXIST-1", Source = StorageLocationSource.Sage, IsActive = true,
        IstBuchbar = true,   // User hat es freigeschaltet
        CreatedAt = DateTime.Now.AddDays(-30), CreatedBy = "system:sync", CreatedByWindows = "x"
    });
    await ctx.SaveChangesAsync();
    reader.Records = new() { new("HALLE-1", "EXIST-1", "Neue Bezeichnung") };   // Description-Diff

    await svc.RunAsync();

    var loc = ctx.StorageLocations.Single();
    loc.Description.Should().Be("Neue Bezeichnung");   // Update wurde gemacht
    loc.IstBuchbar.Should().BeTrue();                   // User-Toggle bleibt
}

[Fact]
public async Task Run_ConflictPath_DoesNotTouchManualIstBuchbar()
{
    var (svc, reader, ctx, _) = Build();
    ctx.StorageLocations.Add(new StorageLocation
    {
        Code = "CONFLICT-1", Zone = "MANUAL-ZONE", Description = "Manuell",
        BarcodeValue = "CONFLICT-1", Source = StorageLocationSource.Manual, IsActive = true,
        IstBuchbar = true,   // Manual-Default
        CreatedAt = DateTime.Now.AddDays(-30), CreatedBy = "tester", CreatedByWindows = "tester"
    });
    await ctx.SaveChangesAsync();
    reader.Records = new() { new("HALLE-1", "CONFLICT-1", "Sage-Bezeichnung") };

    await svc.RunAsync();

    var loc = ctx.StorageLocations.Single();
    loc.IstBuchbar.Should().BeTrue();
    loc.Source.Should().Be(StorageLocationSource.Manual);
}
```

- [ ] **Step 2: Tests laufen — Test 1 FAIL erwartet (Service setzt IstBuchbar nicht), Test 2+3 PASS (Service touched IstBuchbar nicht)**

```pwsh
dotnet test IDEALAKEWMSService.Tests/IDEALAKEWMSService.Tests.csproj --filter "FullyQualifiedName~LagerplatzSyncService" --nologo
```

Expected: 1 FAIL (Test 1), Tests 2+3 PASS direkt.

- [ ] **Step 3: Service-INSERT-Branch erweitern**

In `IDEALAKEWMSService/Services/LagerplatzSyncService.cs`, im INSERT-Block (suche `_ctx.StorageLocations.Add(new StorageLocation`), `IstBuchbar = false` ergaenzen:

```csharp
_ctx.StorageLocations.Add(new StorageLocation
{
    Code = code,
    Zone = zone,
    Description = description,
    BarcodeValue = code,
    Source = StorageLocationSource.Sage,
    IsActive = true,
    IstBuchbar = false,                       // NEU: Sage-Plaetze sind by default nicht buchbar
    Capacity = null,
    IsPickingTransport = false,
    CreatedAt = DateTime.Now,
    CreatedBy = SyncUser,
    CreatedByWindows = Environment.MachineName
});
```

UPDATE-Branch bleibt unveraendert — `IstBuchbar` wird nicht im Diff geprueft und nicht im Update gesetzt. User-Toggle bleibt Master.

- [ ] **Step 4: Tests laufen — alle PASS**

```pwsh
dotnet test IDEALAKEWMSService.Tests/IDEALAKEWMSService.Tests.csproj --filter "FullyQualifiedName~LagerplatzSyncService" --nologo
```

Expected: alle PASS, +3 neue Tests.

- [ ] **Step 5: Commit**

```pwsh
git add IDEALAKEWMSService/Services/LagerplatzSyncService.cs IDEALAKEWMSService.Tests/Services/LagerplatzSyncServiceTests.cs
git commit -m "feat(lagerplatz): sage-sync sets IstBuchbar=false on insert, preserves on update" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 4: Edit-Maske + Server-Side-Schutz erweitern

**Files:**
- Modify: `IdealAkeWms/Controllers/StorageLocationsController.cs` (Edit-POST)
- Modify: `IdealAkeWms/Views/StorageLocations/Edit.cshtml`
- Modify: `IdealAkeWms.Tests/Controllers/StorageLocationsControllerTests.cs`

- [ ] **Step 1: Tests schreiben (failing)**

In `IdealAkeWms.Tests/Controllers/StorageLocationsControllerTests.cs` zwei neue Tests anhaengen:

```csharp
[Fact]
public async Task Edit_Post_SourceSage_AcceptsIstBuchbarToggle()
{
    using var ctx = TestDbContextFactory.Create();
    var existing = new StorageLocation
    {
        Code = "SAGE-1", Zone = "HALLE-1", Description = "Sage",
        BarcodeValue = "SAGE-1", Source = StorageLocationSource.Sage,
        IsActive = true, IstBuchbar = false,   // initial nicht freigeschaltet
        CreatedBy = "x", CreatedByWindows = "x"
    };
    ctx.StorageLocations.Add(existing);
    await ctx.SaveChangesAsync();

    var repo = new StorageLocationRepository(ctx);
    var userSvc = new Mock<ICurrentUserService>();
    userSvc.Setup(x => x.GetDisplayName()).Returns("admin");
    userSvc.Setup(x => x.GetWindowsUserName()).Returns("admin");
    var ctrl = new StorageLocationsController(repo, userSvc.Object);

    var posted = new StorageLocation
    {
        Id = existing.Id,
        Code = "HACKED",         // Sage-Field — sollte ignoriert werden
        IstBuchbar = true,        // User-Field — sollte uebernommen werden
        Capacity = 5,
        IsPickingTransport = false
    };

    await ctrl.Edit(existing.Id, posted);

    var saved = ctx.StorageLocations.Single();
    saved.Code.Should().Be("SAGE-1");        // Sage-Field unveraendert
    saved.IstBuchbar.Should().BeTrue();      // User-Toggle uebernommen
}

[Fact]
public async Task Edit_Post_SourceManual_AcceptsIstBuchbarToggle()
{
    using var ctx = TestDbContextFactory.Create();
    var existing = new StorageLocation
    {
        Code = "MAN-1", Zone = "Z1", Description = "Manuell",
        BarcodeValue = "MAN-1", Source = StorageLocationSource.Manual,
        IsActive = true, IstBuchbar = true,
        CreatedBy = "x", CreatedByWindows = "x"
    };
    ctx.StorageLocations.Add(existing);
    await ctx.SaveChangesAsync();

    var repo = new StorageLocationRepository(ctx);
    var userSvc = new Mock<ICurrentUserService>();
    userSvc.Setup(x => x.GetDisplayName()).Returns("admin");
    userSvc.Setup(x => x.GetWindowsUserName()).Returns("admin");
    var ctrl = new StorageLocationsController(repo, userSvc.Object);

    var posted = new StorageLocation
    {
        Id = existing.Id,
        Code = "MAN-1",
        Description = "Manuell-Updated",
        Zone = "Z1",
        IsActive = true,
        IstBuchbar = false,         // User stillgelegt
        IsPickingTransport = false
    };

    await ctrl.Edit(existing.Id, posted);

    var saved = ctx.StorageLocations.Single();
    saved.Description.Should().Be("Manuell-Updated");
    saved.IstBuchbar.Should().BeFalse();
}
```

- [ ] **Step 2: Tests laufen — beide FAIL erwartet (Controller setzt IstBuchbar noch nicht)**

```pwsh
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "AcceptsIstBuchbarToggle" --nologo
```

Expected: beide FAIL.

- [ ] **Step 3: Controller-Edit-POST erweitern**

In `IdealAkeWms/Controllers/StorageLocationsController.cs`, im Edit-POST: in **beiden** Branches (Sage und Manual) `existing.IstBuchbar = location.IstBuchbar;` ergaenzen.

Bestehende Logik (Phase 1):

```csharp
if (existing.Source == StorageLocationSource.Sage)
{
    existing.Capacity = location.Capacity;
    existing.IsPickingTransport = location.IsPickingTransport;
    // IsActive bleibt sync-controlled
}
else
{
    existing.Code = location.Code;
    existing.Description = location.Description;
    existing.Zone = location.Zone;
    existing.Capacity = location.Capacity;
    existing.IsPickingTransport = location.IsPickingTransport;
    existing.IsActive = location.IsActive;
    existing.BarcodeValue = location.Code;
}
```

Erweitern zu:

```csharp
if (existing.Source == StorageLocationSource.Sage)
{
    existing.Capacity = location.Capacity;
    existing.IsPickingTransport = location.IsPickingTransport;
    existing.IstBuchbar = location.IstBuchbar;       // NEU: user-controlled, auch fuer Sage
    // IsActive bleibt sync-controlled
}
else
{
    existing.Code = location.Code;
    existing.Description = location.Description;
    existing.Zone = location.Zone;
    existing.Capacity = location.Capacity;
    existing.IsPickingTransport = location.IsPickingTransport;
    existing.IsActive = location.IsActive;
    existing.IstBuchbar = location.IstBuchbar;       // NEU
    existing.BarcodeValue = location.Code;
}
```

- [ ] **Step 4: Tests laufen — PASS**

```pwsh
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "StorageLocationsControllerTests" --nologo
```

Expected: alle existing + die zwei neuen PASS.

- [ ] **Step 5: View aktualisieren**

In `IdealAkeWms/Views/StorageLocations/Edit.cshtml`, zwischen dem `IsActive`-Switch (Phase-1) und den weiteren Feldern den neuen Toggle einfuegen:

```cshtml
<div class="mb-3">
    <div class="form-check form-switch">
        <input asp-for="IstBuchbar" class="form-check-input" type="checkbox" role="switch" />
        <label asp-for="IstBuchbar" class="form-check-label">F&uuml;r Buchungen freigegeben</label>
    </div>
    <small class="form-text text-muted">
        Wenn deaktiviert, ist der Lagerplatz in Buchungs-Dropdowns
        (Einbuchung/Ausbuchung/Umbuchung/Picking) nicht ausw&auml;hlbar.
        Bestand auf nicht-buchbaren Pl&auml;tzen wird in der Bestands&uuml;bersicht weiterhin angezeigt.
    </small>
</div>
```

WICHTIG: Lies zuerst die existing Edit.cshtml um die genaue Position zu finden — direkt nach dem IsActive-Block, vor Capacity/IsPickingTransport. Bei Sage-Records: KEIN `disabled`-Attribut (User darf togglen), KEIN `readonly`-Banner-Hinweis fuer dieses Feld.

- [ ] **Step 6: Build + alle Tests**

```pwsh
dotnet build --nologo
dotnet test --nologo --no-build
```

Expected: alles gruen.

- [ ] **Step 7: Commit**

```pwsh
git add IdealAkeWms/Controllers/StorageLocationsController.cs IdealAkeWms/Views/StorageLocations/Edit.cshtml IdealAkeWms.Tests/Controllers/StorageLocationsControllerTests.cs
git commit -m "feat(lagerplatz): user-controlled IstBuchbar toggle in edit-form for both Sage and Manual" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 5: Index-Liste — Spalte + Toggle

**Files:**
- Modify: `IdealAkeWms/Controllers/StorageLocationsController.cs` (Index-Action)
- Modify: `IdealAkeWms/Views/StorageLocations/Index.cshtml`

- [ ] **Step 1: Index-Action erweitern**

In `IdealAkeWms/Controllers/StorageLocationsController.cs`, die `Index`-Action erweitern um den `onlyBookable`-Parameter:

```csharp
public async Task<IActionResult> Index(bool showInactive = false, bool onlyBookable = false)
{
    var all = await _storageLocationRepository.GetAllOrderedAsync();
    var query = all.AsQueryable();
    if (!showInactive)
        query = query.Where(l => l.IsActive);
    if (onlyBookable)
        query = query.Where(l => l.IstBuchbar);

    ViewBag.ShowInactive = showInactive;
    ViewBag.OnlyBookable = onlyBookable;
    ViewBag.HasInactive = all.Any(l => !l.IsActive);
    ViewBag.HasNonBookable = all.Any(l => !l.IstBuchbar);
    return View(query.ToList());
}
```

- [ ] **Step 2: View aktualisieren — Toggle**

In `IdealAkeWms/Views/StorageLocations/Index.cshtml`, im existing Toggle-Bereich (showInactive — Phase 1) den neuen Toggle direkt daneben/darunter ergaenzen.

Lies zuerst den existing Toggle-Block. Erweitere zu zwei Toggles in einem Form:

```cshtml
@if (ViewBag.HasInactive == true || ViewBag.HasNonBookable == true)
{
    <form method="get" class="d-flex gap-3 mb-3 flex-wrap">
        @if (ViewBag.HasInactive == true)
        {
            <div class="form-check form-switch">
                <input class="form-check-input" type="checkbox" id="showInactiveToggle" name="showInactive" value="true"
                       @(ViewBag.ShowInactive == true ? "checked" : "")
                       onchange="this.form.submit()" />
                <label class="form-check-label" for="showInactiveToggle">Auch inaktive Lagerpl&auml;tze zeigen</label>
            </div>
        }
        @if (ViewBag.HasNonBookable == true)
        {
            <div class="form-check form-switch">
                <input class="form-check-input" type="checkbox" id="onlyBookableToggle" name="onlyBookable" value="true"
                       @(ViewBag.OnlyBookable == true ? "checked" : "")
                       onchange="this.form.submit()" />
                <label class="form-check-label" for="onlyBookableToggle">Nur buchbare Pl&auml;tze zeigen</label>
            </div>
        }
        @* Hidden-Inputs damit Toggle-Kombi beim Submit erhalten bleibt *@
        @if (ViewBag.HasInactive != true && ViewBag.ShowInactive == true)
        {
            <input type="hidden" name="showInactive" value="true" />
        }
        @if (ViewBag.HasNonBookable != true && ViewBag.OnlyBookable == true)
        {
            <input type="hidden" name="onlyBookable" value="true" />
        }
    </form>
}
```

WICHTIG: Falls die existing `<form>` im Phase-1-Style (nur showInactive) anders strukturiert ist, behutsam erweitern statt komplett ersetzen. Lies zuerst die existing Datei.

- [ ] **Step 3: View aktualisieren — Buchbar-Spalte**

In `Index.cshtml`, in der `<thead>`, neue `<th>` mit `data-col-key="bookable"` "Buchbar" einfuegen (Position: nach "Quelle"-Spalte aus Phase 1, vor "Aktiv"-Spalte):

```cshtml
<th data-col-key="bookable">Buchbar</th>
```

In jeder `<tbody>`-Zeile entsprechende `<td>`:

```cshtml
<td data-col-key="bookable">
    @if (item.IstBuchbar)
    {
        <span class="badge bg-success">Ja</span>
    }
    else
    {
        <span class="badge bg-secondary">Nein</span>
    }
</td>
```

`colspan` der Empty-State-Row entsprechend anpassen (falls vorhanden — von Phase 1 evtl. dynamisch berechnet).

- [ ] **Step 4: ColumnDefinitions pruefen**

```pwsh
```

Falls `StorageLocations` in `IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs` registriert ist: neuen `bookable`-Key in der Spaltenliste ergaenzen. Falls nicht (per Phase-2-Befund: nicht registriert): keine Aenderung noetig.

- [ ] **Step 5: Build + manueller Smoke-Test**

```pwsh
dotnet build IdealAkeWms/IdealAkeWms.csproj --nologo
```

Expected: 0 Fehler. Manuell Web-App starten, Stammdaten -> Lagerplaetze. Nach erstem Sage-Sync sollten ein paar Plaetze "Nein" zeigen, alle Manual-Plaetze "Ja". Beide Toggles unabhaengig kombinierbar.

- [ ] **Step 6: Commit**

```pwsh
git add IdealAkeWms/Controllers/StorageLocationsController.cs IdealAkeWms/Views/StorageLocations/Index.cshtml IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs
git commit -m "feat(lagerplatz): show bookable column + filter toggle on storage-locations index" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

(`ColumnDefinitions.cs` nur einbeziehen wenn tatsaechlich geaendert.)

---

## Task 6: Bestandsuebersicht — "nicht buchbar"-Badge

**Files:**
- Modify: `IdealAkeWms/Models/ViewModels/StockOverviewViewModel.cs`
- Modify: `IdealAkeWms/Data/Repositories/StockMovementRepository.cs` (Projektion)
- Modify: `IdealAkeWms/Views/StockOverview/Index.cshtml`
- Modify: `IdealAkeWms.Tests/Repositories/StockMovementRepositoryAggregationTests.cs` (neuer Test)

- [ ] **Step 1: ViewModel erweitern**

In `IdealAkeWms/Models/ViewModels/StockOverviewViewModel.cs`, im `StockOverviewItem` direkt nach `StorageLocationIsActive` ergaenzen:

```csharp
public bool StorageLocationIstBuchbar { get; set; } = true;
```

Default `true` als Safe-Fallback (falls je ein Code-Pfad das Feld nicht setzt → kein faelschliches "nicht buchbar"-Badge).

- [ ] **Step 2: Test schreiben (failing)**

In `IdealAkeWms.Tests/Repositories/StockMovementRepositoryAggregationTests.cs` neuer Test:

```csharp
[Fact]
public async Task GetCurrentStockAsync_ProjectsIstBuchbar()
{
    using var ctx = TestDbContextFactory.Create();
    SeedArticleAndLocation(ctx, articleId: 1, locationId: 1);
    // Lagerplatz auf nicht-buchbar setzen
    var loc = ctx.StorageLocations.Find(1)!;
    loc.IstBuchbar = false;
    ctx.StockMovements.Add(NewMovement(articleId: 1, locationId: 1, qty: 5m, MovementType.Einbuchung));
    await ctx.SaveChangesAsync();
    var repo = new StockMovementRepository(ctx);

    var result = await repo.GetCurrentStockAsync(includeZeroStock: false);

    result.Should().ContainSingle();
    result[0].StorageLocationIstBuchbar.Should().BeFalse();
}
```

- [ ] **Step 3: Test laufen — FAIL**

```pwsh
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "ProjectsIstBuchbar" --nologo
```

Expected: FAIL — Property kommt als Default `true` zurueck weil sie noch nicht in der Projection ist.

- [ ] **Step 4: Repository-Projektion erweitern**

In `IdealAkeWms/Data/Repositories/StockMovementRepository.cs`, alle Stellen wo `StorageLocationIsActive` projiziert wird (analog Phase 2 Task 17), `StorageLocationIstBuchbar` mit-projizieren.

Bekannte Stellen (aus dem Repository-Inhalt zu ermitteln per Grep — typischerweise 3-4 Aggregate-Methoden):

```pwsh
```

In jeder Stelle, wo `StorageLocationIsActive = first.IsActive` oder `StorageLocationIsActive = sm.StorageLocation.IsActive` oder analog steht, daneben `StorageLocationIstBuchbar = first.IstBuchbar` (oder analog) ergaenzen.

Auch in den GroupBy-Tuple-Konstruktionen, wo `IsActive` mitgenommen wird, `IstBuchbar` mitfuehren.

WICHTIG: Lies die bestehende Stellen sorgfaeltig — die Projektion ist in mehreren Methoden (`GetCurrentStockAsync`, `GetStockByProductionOrderAsync` etc.) dupliziert.

- [ ] **Step 5: Test laufen — PASS**

```pwsh
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "ProjectsIstBuchbar" --nologo
```

Expected: PASS.

- [ ] **Step 6: View — Badge "nicht buchbar"**

In `IdealAkeWms/Views/StockOverview/Index.cshtml` finde den Block, wo das existing "inaktiv"-Badge gerendert wird (Phase 2 Task 17). Direkt darunter neuen Badge:

```cshtml
@if (item.StorageLocationIstBuchbar == false && item.CurrentQuantity > 0)
{
    <span class="badge bg-secondary ms-1" title="Lagerplatz ist nicht fuer Buchungen freigegeben">nicht buchbar</span>
}
```

WICHTIG: `bg-secondary` (grau) bewusst anders als `bg-warning` (gelb) des "inaktiv"-Badges — die zwei Konzepte muessen visuell trennbar sein.

- [ ] **Step 7: Build + alle Tests**

```pwsh
dotnet build --nologo
dotnet test --nologo --no-build
```

Expected: alles gruen.

- [ ] **Step 8: Commit**

```pwsh
git add IdealAkeWms/Models/ViewModels/StockOverviewViewModel.cs IdealAkeWms/Data/Repositories/StockMovementRepository.cs IdealAkeWms/Views/StockOverview/Index.cshtml IdealAkeWms.Tests/Repositories/StockMovementRepositoryAggregationTests.cs
git commit -m "feat(lagerplatz): show non-bookable badge on stock-overview" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 7: Doku — Phase-2-Changelog erweitern + Hilfeseite + CLAUDE.md

**Files:**
- Modify: `IdealAkeWms/Views/Help/Changelog.cshtml`
- Modify: `IdealAkeWms/Views/Help/Index.cshtml`
- Modify: `CLAUDE.md`

KEIN AppVersion-Bump — Phase 2 ist noch nicht released, IstBuchbar gehoert thematisch dazu.

- [ ] **Step 1: Phase-2-Changelog-Eintrag erweitern**

In `IdealAkeWms/Views/Help/Changelog.cshtml`, im **existing** v1.10.0-Card (oberster Block aus Phase 2): einen Bullet-Point ergaenzen, **direkt unter** dem Bestand-Sync-Block:

```cshtml
<li><strong>Buchbar-Flag (User-gesteuert):</strong> Lagerpl&auml;tze haben jetzt ein zus&auml;tzliches Flag, um sie f&uuml;r UI-Buchungen freizuschalten oder zu sperren. Sage-importierte Pl&auml;tze sind by default <strong>nicht buchbar</strong> — der Admin schaltet die ben&ouml;tigten Pl&auml;tze in der Lagerpl&auml;tze-Liste frei. Bestand auf nicht-buchbaren Pl&auml;tzen wird weiterhin korrigiert (Sage-Master), nur die UI-Buchungs-Dropdowns blenden sie aus.</li>
```

Position: am Ende der existing v1.10.0-`<ul>`-Liste, sodass der Bullet als letzter Punkt erscheint.

- [ ] **Step 2: Hilfeseite-Eintrag**

In `IdealAkeWms/Views/Help/Index.cshtml`, im **existing** "Lagerplatz-Sync mit Sage"-Card (Phase 1) oder im "Lagerbestand-Sync mit Sage"-Card (Phase 2) — wo immer es passt — einen neuen Abschnitt einfuegen:

```cshtml
<h6>Buchbar-Flag (User-gesteuert)</h6>
<p>Jeder Lagerplatz hat zus&auml;tzlich zum Sage-Aktiv-Status ein <strong>Buchbar</strong>-Flag, das vom Admin pro Platz gesetzt werden kann (auch f&uuml;r Sage-Pl&auml;tze).</p>
<ul>
    <li>Sage-importierte Lagerpl&auml;tze sind <strong>standardm&auml;&szlig;ig nicht buchbar</strong>. Der Admin schaltet die ben&ouml;tigten Pl&auml;tze in der Lagerpl&auml;tze-Liste frei (Stammdaten &rarr; Lagerpl&auml;tze &rarr; Bearbeiten).</li>
    <li>Manuell angelegte Lagerpl&auml;tze sind by default buchbar.</li>
    <li>Nicht-buchbare Pl&auml;tze sind in den Buchungs-Dropdowns (Einbuchung/Ausbuchung/Umbuchung/Picking) <strong>ausgeblendet</strong>.</li>
    <li>Bestand auf nicht-buchbaren Pl&auml;tzen wird in der Bestands&uuml;bersicht <strong>weiterhin angezeigt</strong>, mit Badge "nicht buchbar".</li>
    <li>Sage-Bestand-Korrekturen (Phase 2) <strong>respektieren</strong> nicht-buchbare Pl&auml;tze nicht — Sage bleibt Master f&uuml;r Bestand. Nur die UI-Buchbarkeit ist user-gesteuert.</li>
    <li>Der Sync respektiert das Flag — beim Update eines Sage-Lagerplatzes wird der User-Toggle nicht ver&auml;ndert.</li>
</ul>
```

- [ ] **Step 3: CLAUDE.md erweitern**

In `CLAUDE.md`, im "Bekannte Fallstricke"-Abschnitt einen neuen Eintrag ergaenzen:

```markdown
- **IsActive vs IstBuchbar**: Zwei unabhaengige Status-Flags auf StorageLocation. `IsActive` ist Sage-controlled (Phase-1-Sync setzt es), `IstBuchbar` ist user-controlled. Buchungs-Dropdowns filtern auf BEIDE; Bestand-Aggregation und Sage-Korrektur-Buchungen ignorieren `IstBuchbar`. Default: Manual=true (buchbar), Sage=false (nicht buchbar — Admin schaltet manuell frei).
```

- [ ] **Step 4: Build + Tests**

```pwsh
dotnet build --nologo
dotnet test --nologo --no-build
```

Expected: alles gruen. Test-Anzahl unveraendert — Doku-Aenderungen.

- [ ] **Step 5: Commit**

```pwsh
git add IdealAkeWms/Views/Help/Changelog.cshtml IdealAkeWms/Views/Help/Index.cshtml CLAUDE.md
git commit -m "docs: extend Phase-2 changelog and help with IstBuchbar flag info" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Manuelle Verifikation (vor Merge)

- **Migration-Re-Run-Schutz:** SQL/58 zweimal manuell gegen die DB ausfuehren (z.B. via SQL Server Management Studio). Beim zweiten Mal: weder ALTER (COL_LENGTH-Guard greift) noch UPDATE (History-Guard greift) noch INSERT (History-Guard greift). User-Toggles bleiben erhalten.
- **UI-Smoke-Test:** Sage-Lagerplatz oeffnen, IstBuchbar togglen → in Inbound/Outbound/Transfer/OutboundAll/Picking-Dropdowns verifizieren ob er erscheint/verschwindet.
- **Bestand-Sync-Verhalten:** Bestand auf nicht-buchbarem Sage-Lagerplatz veraendern → Sage-Sync (Phase 2) muss eine Korrektur-Buchung schreiben (unabhaengig vom IstBuchbar-Status).
- **Index-Toggle-Kombi:** Beide Toggles ("Auch inaktive zeigen" + "Nur buchbare zeigen") unabhaengig schaltbar, Kombi-Filter funktioniert.
- **NAN-Lagerplatz:** Default Manual + IstBuchbar=true → bleibt fuer Negativ-Buchungen verfuegbar. Kein Special-Handling.

---

## Self-Review-Notiz

Plan deckt alle Spec-Sektionen:
- Datenmodell (Schema + Migration mit History-Guard) → Task 1
- Repository-Filter → Task 2
- Sage-Sync-Verhalten → Task 3
- Edit-Maske + Server-Side-Schutz → Task 4
- Index-Liste (Spalte + Toggle) → Task 5
- Bestandsuebersicht-Badge → Task 6
- Doku → Task 7

Test-Mapping:
- Spec-Test 1+2 (Repository) → Task 2
- Spec-Test 3+4 (Controller) → Task 4
- Spec-Test 5+6+7 (Sync-Service) → Task 3
- Spec-Test 8 (ViewModel-Projektion) → Task 6
- Manuelle Verifikation → unten dokumentiert

Total: 7 Tasks, ~8 neue Tests.
