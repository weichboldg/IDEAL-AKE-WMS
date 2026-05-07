# Design — StorageLocation `IstBuchbar`-Flag

**Datum:** 2026-05-07
**Branch:** `feature/sage-lagerbestand-sync` (Phase-2-Erweiterung, kein neuer Branch)
**Status:** Spec, freigegeben
**Vorgaenger:** Phase 2 (Sage Lagerbestand-Sync, v1.10.0) — auf demselben Branch noch ungemerged

## Ziel und Scope

Neues User-gesteuertes Flag `IstBuchbar` auf `StorageLocation`, das die UI-Buchungs-Dropdowns (Einbuchung/Ausbuchung/Umbuchung/Picking) filtert. **Default fuer Sage-importierte Lagerplaetze: nicht buchbar.** Der User kann pro Lagerplatz togglen — auch fuer Sage-Records — und der Sync respektiert diesen Toggle bei UPDATE-Operations.

Strikte Trennung der Concerns:
- `IsActive` (Phase 1, **Sage-controlled**): "existiert in Sage und ist dort aktiv"
- `IstBuchbar` (NEU, **User-controlled**): "darf im WMS fuer Buchungen verwendet werden"

Sage-Bestand-Korrekturen (Phase 2) schreiben weiterhin **unabhaengig** von `IstBuchbar` — der Sage-Master fuer Bestand bleibt unangetastet. Nur die UI-Buchbarkeit ist user-gesteuert.

**Explizit nicht in diesem Feature:**
- AppVersion-Bump (v1.10.0 bleibt — Erweiterung in Phase-2-Changelog).
- Bulk-Toggle-UI ("alle Sage-Plaetze auf einmal aktivieren") — Phase-3-Kandidat, falls Praxis-Use-Case auftaucht.
- Audit-Log fuer IstBuchbar-Aenderungen — `AuditableEntity.ModifiedAt/By` deckt das implizit ab.

## Datenmodell

### `StorageLocation` Erweiterung

```csharp
[Display(Name = "Buchbar")]
public bool IstBuchbar { get; set; } = true;
```

Default `true` im Code (Manual-Lagerplaetze sind by default buchbar). Sage-Sync setzt beim INSERT explizit `false`.

### EF-Konfiguration

In `ApplicationDbContext.cs`, im `StorageLocation`-Block:

```csharp
entity.Property(e => e.IstBuchbar).HasDefaultValue(true);
entity.HasIndex(e => e.IstBuchbar);
```

Index ist single-column-bool — niedrige Selectivity, aber konsistent mit existing `IX_StorageLocations_IsActive` und `IX_StorageLocations_Source`. Falls in Phase 3+ messbar, alle drei zusammen entfernen.

### Migration `58_AddStorageLocationIstBuchbar.sql`

**Wichtig:** Das initiale UPDATE-Statement, das alle existierenden Sage-Records auf `IstBuchbar=0` setzt, muss in einem Migrations-History-Guard liegen, damit es nur **einmal** laeuft. Sonst wuerde eine erneute Skript-Ausfuehrung (z.B. nach manueller Korrektur eines Sage-Plates auf `IstBuchbar=1`) den User-Toggle ueberschreiben.

```sql
-- SQL/58_AddStorageLocationIstBuchbar.sql
-- Phase-2-Erweiterung: User-gesteuertes IstBuchbar-Flag.

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

-- Initial-Setup: existing Sage-Records auf 0 setzen — NUR beim ersten Migrations-Lauf.
-- Schutz gegen versehentliches Ueberschreiben User-modifizierter Toggles bei Skript-Re-Run.
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

`<TIMESTAMP>` durch echten EF-Migrations-Timestamp ersetzen. EF-Migration `AddStorageLocationIstBuchbar` zusaetzlich generieren.

`SQL/00_FreshInstall.sql`: Spalte `IstBuchbar` zur StorageLocations-CREATE-TABLE ergaenzen, neuer Index, MigrationId-Eintrag. Im FreshInstall ist KEIN UPDATE noetig (DB ist leer).

## Sage-Sync-Verhalten (Phase 1 Lagerplatz-Sync)

In `IDEALAKEWMSService/Services/LagerplatzSyncService.cs`:

**INSERT-Branch** (neuer Sage-Lagerplatz importiert): explizit `IstBuchbar = false` setzen.

**UPDATE-Branch** (existierender Sage-Record mit Diff): `IstBuchbar` wird **NICHT** angefasst. Der User-Toggle bleibt Master.

**Diff-Detection** beruehrt `IstBuchbar` nicht — eine Aenderung am Toggle triggert keinen "diff" der Sync sehen wuerde.

**Konflikt-Path** (Sage liefert Code, der als Manual-Source existiert): unveraendert — Manual-Record bleibt komplett unberuehrt, IstBuchbar bleibt user-set.

## Bestand-Korrektur-Sync (Phase 2 Lagerbestand-Sync)

`IDEALAKEWMSService/Services/LagerbestandSyncService.cs`: **keine Aenderung**. Der Bestand-Sync schreibt Korrektur-Buchungen unabhaengig vom `IstBuchbar`-Status — Sage-Master fuer Bestand bleibt unberuehrt. UI-Buchbarkeit hat keinen Einfluss auf Bestand-Aggregation.

## Repository-Anpassung

`IdealAkeWms/Data/Repositories/StorageLocationRepository.cs`:

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

Methodennamen bleiben — semantisch ist "Active" jetzt "active for booking", inkludiert IstBuchbar. Alle Booking-Dropdowns (Einbuchung/Ausbuchung/Umbuchung/OutboundAll/Picking) kriegen automatisch den IstBuchbar-Filter.

`GetCurrentStockByArticleAndLocationAsync` (Phase 2 Sync) bleibt **unveraendert** — schreibt unabhaengig vom IstBuchbar-Status (siehe Bestand-Korrektur-Sync oben).

`GetAllOrderedAsync` und der Lagerplatz-Stammdaten-Index bleiben unveraendert — zeigen alle Plaetze (auch nicht-buchbare).

## Web-UI

### `Views/StorageLocations/Edit.cshtml`

Neuer Bootstrap-Switch **direkt nach** `IsActive`, immer editierbar (auch fuer Sage-Records):

```cshtml
<div class="mb-3">
    <div class="form-check form-switch">
        <input asp-for="IstBuchbar" class="form-check-input" type="checkbox" />
        <label asp-for="IstBuchbar" class="form-check-label">Fuer Buchungen freigegeben</label>
    </div>
    <small class="form-text text-muted">
        Wenn deaktiviert, ist der Lagerplatz in Buchungs-Dropdowns nicht ausw&auml;hlbar.
        Bestand auf nicht-buchbaren Pl&auml;tzen wird in der Bestands&uuml;bersicht weiterhin angezeigt.
    </small>
</div>
```

### `Controllers/StorageLocationsController.cs` Edit-POST

Aktuelle Logik (Phase 1):
- `Source=Sage`: Code/Zone/Description/IsActive werden ignoriert. Capacity, IsPickingTransport werden uebernommen.
- `Source=Manual`: alle Felder uebernommen.

Erweiterung: `IstBuchbar` wird in **beiden** Branches uebernommen — es ist user-controlled, also unabhaengig von der Source.

```csharp
if (existing.Source == StorageLocationSource.Sage)
{
    existing.Capacity = location.Capacity;
    existing.IsPickingTransport = location.IsPickingTransport;
    existing.IstBuchbar = location.IstBuchbar;   // NEU: user-controlled, auch fuer Sage
    // IsActive bleibt sync-controlled
    // Code/Zone/Description bleiben sync-controlled
}
else
{
    // Manual: alles editierbar inkl. IstBuchbar
    existing.Code = location.Code;
    existing.Description = location.Description;
    existing.Zone = location.Zone;
    existing.Capacity = location.Capacity;
    existing.IsPickingTransport = location.IsPickingTransport;
    existing.IsActive = location.IsActive;
    existing.IstBuchbar = location.IstBuchbar;   // NEU
    existing.BarcodeValue = location.Code;
}
```

### `Views/StorageLocations/Index.cshtml`

**Neue Spalte "Buchbar"** mit Badge:

```cshtml
<th data-col-key="bookable">Buchbar</th>
```

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

`colspan` der Empty-State-Row entsprechend anpassen.

**Neuer Toggle "Nur buchbare zeigen"** im Filter-Bereich (analog zum existing "Auch inaktive zeigen"-Toggle aus Phase 1):

```cshtml
<form method="get" class="d-flex gap-3 mb-3">
    <div class="form-check form-switch">
        <input class="form-check-input" type="checkbox" id="showInactiveToggle" name="showInactive" value="true"
               @(ViewBag.ShowInactive == true ? "checked" : "")
               onchange="this.form.submit()" />
        <label class="form-check-label" for="showInactiveToggle">Auch inaktive Lagerpl&auml;tze zeigen</label>
    </div>
    <div class="form-check form-switch">
        <input class="form-check-input" type="checkbox" id="onlyBookableToggle" name="onlyBookable" value="true"
               @(ViewBag.OnlyBookable == true ? "checked" : "")
               onchange="this.form.submit()" />
        <label class="form-check-label" for="onlyBookableToggle">Nur buchbare Pl&auml;tze zeigen</label>
    </div>
</form>
```

Beide Toggles unabhaengig kombinierbar. Controller-Action:

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

`ColumnDefinitions.cs`: Falls StorageLocations dort registriert, neuen `bookable`-Key ergaenzen. Sonst (per Phase 2 Task 15: ist nicht registriert): keine Aenderung noetig.

### `Views/StockOverview/Index.cshtml`

Bei `IstBuchbar=false && CurrentQuantity>0`: zusaetzliches Badge **"nicht buchbar"** mit `bg-secondary` (grau) — bewusst **unterschiedlich** zur existing "inaktiv"-Badge in `bg-warning text-dark` (gelb), damit User die zwei Konzepte sofort visuell trennen kann.

```cshtml
@item.StorageLocationCode
@if (item.StorageLocationIsActive == false && item.CurrentQuantity > 0)
{
    <span class="badge bg-warning text-dark ms-1" title="Lagerplatz ist deaktiviert (Sage)">inaktiv</span>
}
@if (item.StorageLocationIstBuchbar == false && item.CurrentQuantity > 0)
{
    <span class="badge bg-secondary ms-1" title="Lagerplatz ist nicht fuer Buchungen freigegeben">nicht buchbar</span>
}
```

ViewModel `StockOverviewItem` bekommt `bool StorageLocationIstBuchbar` (Default `true` fuer safe fallback). `StockMovementRepository.GetCurrentStockAsync` und `GetStockByProductionOrderAsync` Projection muss das Feld mitliefern (4 Stellen analog `StorageLocationIsActive` aus Phase 2 Task 17).

## Tests

### Repository-Tests (`StorageLocationRepositoryTests`)

1. `GetActiveOrderedExcludingPickingTransportAsync_FiltersNonBookable` — drei Plaetze: aktiv+buchbar / aktiv+nicht-buchbar / inaktiv+buchbar → nur erster zurueck.
2. `GetActivePickingTransportLocationsAsync_FiltersNonBookable` — zwei Wagen: buchbar / nicht-buchbar → nur ersten zurueck.

### Controller-Tests (`StorageLocationsControllerTests`)

3. `Edit_Post_SourceSage_AcceptsIstBuchbarToggle` — Sage-Record, POST mit `IstBuchbar=false` (vorher true) → akzeptiert. Code/Zone/Description bleiben unangetastet.
4. `Edit_Post_SourceManual_AcceptsIstBuchbarToggle` — Manual-Record, IstBuchbar-Toggle akzeptiert.

### Sync-Service-Tests (`LagerplatzSyncServiceTests`)

5. `Run_NewSageRecord_SetsIstBuchbarFalse` — INSERT eines Sage-Lagerplatzes setzt `IstBuchbar=false`.
6. `Run_ExistingSageRecord_PreservesIstBuchbar` — UPDATE eines Sage-Records mit User-modifiziertem `IstBuchbar=true` (und Description-Diff) — nach Sync ist `IstBuchbar=true` UND Description aktualisiert.
7. `Run_ConflictPath_DoesNotTouchIstBuchbar` — Manual-Record mit IstBuchbar=true existiert, Sage liefert gleichen Code → Conflict-Warning, IstBuchbar bleibt true.

### ViewModel/Projection-Tests (`StockMovementRepositoryAggregationTests`)

8. `GetCurrentStockAsync_ProjectsIstBuchbar` — verifies the field flows from DB through projection to ViewModel.

### Manuelle Verifikation (im Plan)

- Migration-Re-Run-Schutz: SQL/58 zweimal manuell gegen die DB ausfuehren — beim zweiten Mal werden weder ALTER noch UPDATE ausgefuehrt (alle Guards greifen).
- UI-Smoke-Test: Sage-Lagerplatz oeffnen, IstBuchbar togglen, in Inbound/Outbound/Transfer/Picking-Dropdowns verifizieren ob er erscheint/verschwindet.
- Bestand-Sync verifizieren: Korrektur-Buchung schreibt auch auf nicht-buchbare Plaetze (sollte sie).

## Doku

- **AppVersion bleibt v1.10.0** — kein Bump, weil Phase 2 nicht gemerged ist und das Feature thematisch dazugehoert.
- **Phase-2-Changelog erweitern** (`Views/Help/Changelog.cshtml`, im v1.10.0-Card): neuer Bullet-Point unter "Sage Lagerbestand-Sync (Phase 2)":
  ```
  Lagerplaetze haben jetzt ein zusaetzliches User-gesteuertes Flag "Buchbar".
  Sage-importierte Lagerplaetze sind standardmaessig nicht buchbar — der Admin
  schaltet die benoetigten Plaetze in der Lagerplaetze-Liste explizit frei.
  Bestand auf nicht-buchbaren Plaetzen wird weiterhin korrigiert (Sage-Master),
  nur die UI-Buchungs-Dropdowns blenden sie aus.
  ```
- **Hilfeseite** (`Views/Help/Index.cshtml`): kurzen Abschnitt unter "Lagerplatz-Sync mit Sage" einfuegen — "Buchbar-Flag (User-gesteuert)" mit:
  - Sage-Plaetze sind by default nicht buchbar — Admin schaltet sie in Lagerplaetze-Liste frei.
  - Nicht-buchbare Plaetze: sichtbar in Bestand, ausgeblendet in Buchungs-Dropdowns.
  - Sync-Verhalten: User-Toggle wird vom Sync respektiert.
- **CLAUDE.md** — neue Bekannte-Fallstricke-Zeile:
  ```
  - **IsActive vs IstBuchbar**: Zwei unabhaengige Status-Flags auf StorageLocation.
    IsActive ist Sage-controlled (Phase-1-Sync setzt es), IstBuchbar ist user-controlled.
    Buchungs-Dropdowns filtern auf BEIDE; Bestand-Aggregation und Sage-Korrektur-Buchungen
    ignorieren IstBuchbar.
  ```

## Branch-Strategie

Alle Aenderungen werden direkt auf `feature/sage-lagerbestand-sync` committed. Kein eigener Branch — das Feature ist eine logische Phase-2-Erweiterung und wird zusammen mit Phase 2 in einem Bundle gemerged.

## Annahmen, die bei Implementierung verifiziert werden

1. EF-Migration wendet UPDATE auf Sage-Records korrekt an, bei einer fresh-install-DB vs. einer mit existing Sage-Records.
2. `data-col-key="bookable"` integriert sich in das existing `column-preferences.js`-Pattern ohne Bruch.
3. Manual-Lagerplatz `NAN` (Default `IstBuchbar=true`) bleibt fuer Negativ-Buchungen verfuegbar — wird durch Default-Verhalten automatisch abgedeckt, kein Special-Case noetig.
