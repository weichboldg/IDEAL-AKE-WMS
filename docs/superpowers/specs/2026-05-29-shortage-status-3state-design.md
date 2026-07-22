# Shortage-Status 3-State + 2-Tab MissingParts — Design v1.19.0

**Datum:** 2026-05-29
**Status:** Draft
**Branch:** `bugfix/missingparts-include-pd` (semantisch jetzt Feature-Branch)
**Worktree:** `.claude/worktrees/missingparts-include-pd`
**Version-Ziel:** v1.19.0
**Vorgaenger im selben Branch:** v1.18.1-Hotfix-Commits (`f3deba6`, `fb4d21e`) werden Teil von v1.19.0. v1.18.1 ist nie produktiv geworden — die v1.18.1-Card und PROJECT_STATUS-Notiz werden in v1.19.0 integriert/ersetzt.

---

## 1. Ziel

Der Lagermitarbeiter soll pro Item explizit zwischen drei Klassifizierungen waehlen koennen:
- **None** — kein Fehlteil (Default wenn Ist≥Soll)
- **Fehlteil** (= wird nachgeliefert) — Restlieferung erwartet
- **Wird nicht nachgeliefert** — endgueltige Eskalation

Dazu wird das bestehende `IsFinalShortage`-Bool durch ein `ShortageStatus`-Enum (3 Werte) ersetzt. UI verwendet 2 echte Radio-Buttons je Item (mutually exclusive). MissingParts-Liste bekommt 2 Tabs (eine je Klassifizierung). Werkbank-Karte zeigt beide Counts mit eigenen Tab-Links.

## 2. Hintergrund

v1.18.0 fuehrte `IsFinalShortage` (bool) ein mit einer Checkbox "Endgueltig Fehlteil". v1.18.1-Hotfix erweiterte den MissingParts-Filter, sodass `IsFinalShortage=true` auch in PartiallyDelivered-Bestellungen erscheint. Der User erkannte beim Test, dass:
- die Label-Semantik unklar ist ("Fehlteil" sollte eigentlich "wird nachgeliefert" bedeuten)
- die implizite Unterscheidung "Picked<Requested AND !IsFinalShortage = Restlieferung erwartet" zu vage ist
- es zwei klar getrennte Workflows gibt: Restlieferungs-Pipeline (Routine) vs. Eskalation (Aktion noetig)

Die Loesung ist eine echte 3-State-Klassifizierung mit getrennten Views.

## 3. Entscheidungen aus Brainstorming

| Frage | Entscheidung |
|-------|--------------|
| Mutual Exclusion | Radio-Logik (3 Zustaende, max. einer aktiv) |
| Default bei Ist=0 | Auto-Set "Fehlteil" (WillBeRestocked) |
| MissingParts-Layout | Zwei Tabs (Offene Fehlteile / Wird nicht nachgeliefert) |
| Werkbank-Karte | Eine Karte, zwei Zeilen mit je eigenem Tab-Link |
| Versionsstrategie | Auf v1.18.1-Branch weiterbauen → Release als v1.19.0 |
| Bestehende v1.18.1-Commits | Bleiben in der Historie, werden Teil von v1.19.0 |

## 4. Datenmodell

### 4.1 Neues Enum

`IdealAkeWms/Models/ShortageStatus.cs`:

```csharp
namespace IdealAkeWms.Models;

public enum ShortageStatus : byte
{
    None = 0,
    WillBeRestocked = 1,
    NoRestock = 2
}
```

### 4.2 WarehouseRequisitionItem

`IsFinalShortage` (bool) wird ENTFERNT. Ersetzt durch:

```csharp
public ShortageStatus ShortageStatus { get; set; } = ShortageStatus.None;
```

### 4.3 Migration `ReplaceIsFinalShortageWithShortageStatus`

EF-Migration mit Daten-Konvertierung:

```csharp
public partial class ReplaceIsFinalShortageWithShortageStatus : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<byte>(
            name: "ShortageStatus",
            table: "WarehouseRequisitionItems",
            type: "tinyint",
            nullable: false,
            defaultValue: (byte)0);

        migrationBuilder.Sql(@"
            UPDATE [dbo].[WarehouseRequisitionItems]
            SET [ShortageStatus] = CASE
                WHEN [IsFinalShortage] = 1 THEN 2
                WHEN ([QuantityPicked] IS NULL OR [QuantityPicked] < [QuantityRequested]) THEN 1
                ELSE 0
            END;
        ");

        migrationBuilder.Sql(@"
            DROP INDEX IF EXISTS IX_WarehouseRequisitionItems_IsFinalShortage
                ON [dbo].[WarehouseRequisitionItems];
        ");

        migrationBuilder.Sql(@"
            DECLARE @c NVARCHAR(200) = (
                SELECT dc.name FROM sys.default_constraints dc
                INNER JOIN sys.columns c ON dc.parent_object_id = c.object_id
                    AND dc.parent_column_id = c.column_id
                WHERE c.object_id = OBJECT_ID('[dbo].[WarehouseRequisitionItems]')
                  AND c.name = 'IsFinalShortage');
            IF @c IS NOT NULL EXEC('ALTER TABLE [dbo].[WarehouseRequisitionItems] DROP CONSTRAINT [' + @c + ']');
        ");
        migrationBuilder.DropColumn(name: "IsFinalShortage", table: "WarehouseRequisitionItems");

        migrationBuilder.Sql(@"
            CREATE INDEX IX_WarehouseRequisitionItems_ShortageStatus_WillBeRestocked
                ON [dbo].[WarehouseRequisitionItems]([ShortageStatus])
                WHERE [ShortageStatus] = 1;
            CREATE INDEX IX_WarehouseRequisitionItems_ShortageStatus_NoRestock
                ON [dbo].[WarehouseRequisitionItems]([ShortageStatus])
                WHERE [ShortageStatus] = 2;
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP INDEX IF EXISTS IX_WarehouseRequisitionItems_ShortageStatus_WillBeRestocked ON [dbo].[WarehouseRequisitionItems];");
        migrationBuilder.Sql("DROP INDEX IF EXISTS IX_WarehouseRequisitionItems_ShortageStatus_NoRestock ON [dbo].[WarehouseRequisitionItems];");
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
        migrationBuilder.DropColumn(name: "ShortageStatus", table: "WarehouseRequisitionItems");
    }
}
```

Plus idempotentes `SQL/65_ReplaceIsFinalShortageWithShortageStatus.sql` + `SQL/00_FreshInstall.sql`-Konsolidierung (Schema, Index, MigrationsHistory). Da Migration 64 (`AddIsFinalShortageToWarehouseRequisitionItems`) noch im FreshInstall steht, muss sie dort entfernt oder durch die kombinierte Endgestalt ersetzt werden — letzteres ist sauberer (FreshInstall sollte nur das Endschema enthalten).

### 4.4 Daten-Konvertierungs-Tabelle

| Vorher (v1.18.x) | Bedingung | Nachher (v1.19.0) |
|---|---|---|
| `IsFinalShortage = true` | egal | `NoRestock (2)` |
| `IsFinalShortage = false` | `QuantityPicked IS NULL OR Picked < Requested` | `WillBeRestocked (1)` |
| `IsFinalShortage = false` | `QuantityPicked >= QuantityRequested` | `None (0)` |

**Garantie**: bestehende PartiallyDelivered-Bestellungen bleiben PartiallyDelivered, bestehende Closed-Bestellungen bleiben Closed, bestehende "endgueltige Fehlteile" bleiben in der Eskalations-Liste sichtbar.

## 5. Status-Ableitung

```csharp
private static WarehouseRequisitionStatus DeriveStatus(WarehouseRequisition req)
{
    bool isFullyDelivered = req.Items.All(i => (i.QuantityPicked ?? 0) >= i.QuantityRequested);
    bool hasWaitingRestock = req.Items.Any(i => i.ShortageStatus == ShortageStatus.WillBeRestocked);

    return (isFullyDelivered || !hasWaitingRestock)
        ? WarehouseRequisitionStatus.Closed
        : WarehouseRequisitionStatus.PartiallyDelivered;
}
```

**Verhaltens-Matrix:**

| Item | Picked | ShortageStatus | Treibt auf |
|------|--------|---------------|------------|
| A | >= Requested | None | Closed |
| B | < Requested | None | Closed (kein hasWaitingRestock) |
| C | < Requested | WillBeRestocked | **PartiallyDelivered** |
| D | < Requested | NoRestock | Closed |

Sonderfall B: Picked<Requested und ShortageStatus=None — Lager hat das Item nicht klassifiziert. System schliesst es trotzdem ab. Wenn der Lager Klassifizierung erzwingen will, koennte spaeter eine Validierung dazukommen — out-of-scope fuer v1.19.0.

## 6. Repository-API

### 6.1 Aenderungen

```csharp
Task CloseAsync(int id,
    IReadOnlyDictionary<int, decimal> itemQuantitiesPicked,
    IReadOnlyDictionary<int, string?> itemNotes,
    IReadOnlyDictionary<int, ShortageStatus> itemShortageStatuses,
    int closedByUserId, string user, string winUser, byte[] rowVersion);

Task SaveProgressAsync(int id,
    IReadOnlyDictionary<int, decimal?> itemQuantitiesPicked,
    IReadOnlyDictionary<int, string?> itemNotes,
    IReadOnlyDictionary<int, ShortageStatus> itemShortageStatuses,
    string user, string winUser);

Task<(IReadOnlyList<MissingPartRow> Items, int TotalCount)>
    GetMissingPartsAsync(
        ShortageStatus filterStatus,
        int? workplaceFilter,
        IReadOnlyDictionary<string, string>? columnFilters,
        DateTime? closedFrom, DateTime? closedUntil,
        int page, int pageSize);

Task<(int WaitingItemCount, int WaitingRequisitionCount,
      int NoRestockItemCount, int NoRestockRequisitionCount)>
    GetShortageCountsForUserAsync(int userId);
```

### 6.2 Filter-Logik in GetMissingPartsAsync

```csharp
.Where(i => i.ShortageStatus == filterStatus
    && (i.WarehouseRequisition.Status == WarehouseRequisitionStatus.Closed
        || i.WarehouseRequisition.Status == WarehouseRequisitionStatus.PartiallyDelivered));
```

`Cancelled` bleibt ausgeschlossen.

### 6.3 SaveNotesAsync bleibt unveraendert

Bestehender Autosave-on-Blur-Endpoint nutzt nur Note-Updates. Backward-compatible — kein Frontend-Change auf dieser Schicht.

## 7. Controller

### 7.1 WarehousePickingController

- `Close` Form-Param: `int[] shortageStatuses` (Werte 0/1/2). Index-Sync mit `itemIds`.
- `SaveProgress` analog.
- `PrintAndClose` analog.
- `Details`/`Print`-Mapping: `IsFinalShortage` → `ShortageStatus` durchreichen.

### 7.2 MissingPartsController

Neuer Param:

```csharp
public async Task<IActionResult> Index(
    ShortageStatus tab = ShortageStatus.WillBeRestocked,
    int? workplaceId = null,
    bool mineOnly = false,
    int page = 1, int? pageSize = null)
```

**Tab-Normalisierung**: wenn `tab == ShortageStatus.None` → auf `WillBeRestocked` normalisieren (defensive: ungueltige Eingabe → Default-Tab).

Controller ruft zusaetzlich `GetMissingPartsAsync` einmal pro Tab fuer den Tab-Header-Count (oder eine kombinierte Count-Methode am Repository).

### 7.3 WarehouseRequisitionsController.Index

Bisheriger Aufruf `GetFinalShortagesCountForUserAsync` → `GetShortageCountsForUserAsync` mit 4-Tuple. ViewModel-Properties entsprechend ersetzen.

## 8. ViewModels

- `WarehouseRequisitionDetailItemViewModel` — `bool IsFinalShortage` → `ShortageStatus ShortageStatus`
- `MissingPartRow` — neue Property `ShortageStatus Status`
- `MissingPartsListViewModel` — neue Felder: `ShortageStatus ActiveTab`, `int WaitingTotalCount`, `int NoRestockTotalCount`
- `WarehouseRequisitionListViewModel` — 2 Properties ersetzen durch 4: `MissingPartsWaitingItemCount`, `MissingPartsWaitingRequisitionCount`, `MissingPartsNoRestockItemCount`, `MissingPartsNoRestockRequisitionCount`

## 9. Views

### 9.1 WarehousePicking/Details.cshtml

Spalte "Fehlteil" wird zu **"Fehlteil-Status"**. Im editierbaren Modus 2 Radio-Buttons + 1 Hidden-Input je Row:

```html
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
```

Bei Closed/Cancelled (read-only) wird nur ein Status-Badge angezeigt.

**JS-Verhalten:**

- `syncShortageRadios(row)` synchronisiert Hidden-Input mit Radio-State, abhaengig von Quantity-Shortage-Check
- Bei Ist=0: Default `WillBeRestocked` aktivieren
- Bei Ist≥Soll: beide Radios disabled + None (0)
- **3-State-Radio (Doppelklick → None)**: `mousedown`-Snapshot + `click`-Vergleich; wenn Klick auf bereits-aktiven Radio → unchecked + Hidden auf 0
- `input`-Event auf Mengen-Input triggert syncShortageRadios + dirty-Flag

### 9.2 MissingParts/Index.cshtml

Bootstrap `nav-tabs` oben mit 2 Tabs:

```html
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
```

Tabelle-Spalten bleiben gleich (Bestell-ID, Werkbank, Artikel-Nr, Bezeichnung, Soll, Geliefert, Fehlt, Notiz, Erfasst von, Abgeschlossen am).

Page-Title: "Fehlteile" bzw. "Meine Fehlteile" (mineOnly).

### 9.3 WarehouseRequisitions/Index.cshtml — Werkbank-Karte

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

### 9.4 WarehousePicking/Print.cshtml

Spalte "Fehlteil" zeigt jetzt Status-Text:

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

## 10. Tests

### 10.1 Repository

**Migration bestehender Tests** (mechanisch):
- `Dictionary<int, bool>` → `Dictionary<int, ShortageStatus>`
- `[id] = true` → `[id] = ShortageStatus.NoRestock`
- `[id] = false` mit Picked<Requested-Setup → `[id] = ShortageStatus.WillBeRestocked`
- `[id] = false` mit vollstaendiger Lieferung → `[id] = ShortageStatus.None`

**Neue Tests:**
1. `CloseAsync_AllItemsWillBeRestocked_SetsStatusPartiallyDelivered`
2. `CloseAsync_AllShortagesNoRestock_SetsStatusClosed`
3. `CloseAsync_MixedShortageStatuses_SetsStatusPartiallyDelivered`
4. `CloseAsync_ShortageStatusNoneWithShortage_SetsStatusClosed`
5. `GetMissingPartsAsync_TabWillBeRestocked_ReturnsOnlyMatchingItems`
6. `GetMissingPartsAsync_TabNoRestock_ReturnsOnlyMatchingItems`
7. `GetShortageCountsForUserAsync_ReturnsBothCounts`
8. `GetShortageCountsForUserAsync_OnlyForUserWorkplaces`

### 10.2 Controller

1. `WarehousePicking_Close_BindsShortageStatusesIntArray`
2. `WarehousePicking_SaveProgress_PersistsShortageStatuses`
3. `MissingParts_Index_DefaultTab_WillBeRestocked`
4. `MissingParts_Index_TabNone_NormalizedToWillBeRestocked`
5. `MissingParts_Index_ViewModelHasBothCounts`
6. `WarehouseRequisitions_Index_CardShowsBothCounts`
7. `WarehouseRequisitions_Index_CardHidden_WhenBothCountsZero`

**Erwartete Test-Counts:** Web ~628 (619 baseline + ~9 neue), Service 99 unveraendert.

### 10.3 TESTSZENARIEN Kapitel 33

1. **33.1** Default-Fehlteil bei Ist=0
2. **33.2** Manueller Wechsel auf "Wird nicht nachgeliefert"
3. **33.3** Doppelklick auf aktiven Radio → zurueck zu None
4. **33.4** Ist=Soll → beide Radios disabled
5. **33.5** Bestellung mit allen "Fehlteil" → PartiallyDelivered
6. **33.6** Bestellung mit allen "Wird nicht nachgeliefert" → Closed
7. **33.7** Tab "Offene Fehlteile" zeigt nur WillBeRestocked
8. **33.8** Tab "Wird nicht nachgeliefert" zeigt nur NoRestock
9. **33.9** Werkbank-Karte zeigt beide Counts + Tab-Links
10. **33.10** Migration: vorhandene v1.18.x PartiallyDelivered-Bestellung bleibt nach v1.19.0-Migration PD

## 11. Doku

- `IdealAkeWms/AppVersion.cs` + `IDEALAKEWMSService/AppVersion.cs` → `1.19.0` / `2026-05-29`
- `IdealAkeWms/Views/Help/Changelog.cshtml` — v1.18.1-Card **entfernen** (war nie produktiv), v1.19.0-Card prependen mit beiden Aspekten (Filter-Erweiterung + 3-State-Logik)
- `PROJECT_STATUS.md` — v1.18.1-Block **entfernen**, neuen v1.19.0-Block einfuegen mit Hintergrund + Sub-Task-Tabelle
- `CLAUDE.md` Fallstricke:
  - v1.18.1-Fallstrick zu MissingParts **entfernen**
  - Neu: "ShortageStatus-Enum (seit v1.19.0)" — 3 Zustaende, Default None
  - Neu: "Migration v1.19.0 daten-destruktiv" — IsFinalShortage-Spalte wird gedroppt, Backup vor Deploy
  - Neu: "Radio-3-State JS-Pattern" — Doppelklick → None via `mousedown`/`click`-Sequenz, Bootstrap default unterstuetzt das nicht
- `docs/TESTSZENARIEN.md` — Kapitel 32 bleibt (historisch). Kapitel 33 neu (10 Szenarien).
- v1.18.1-Spec + -Plan-Dateien bleiben im Worktree erhalten (sie dokumentieren die Vor-Stufe).

## 12. Branch-Management

Branch bleibt `bugfix/missingparts-include-pd` aus pragmatischen Gruenden (Renaming aendert Refs in PRs etc.). Beim finalen Merge `--no-ff` mit Message `merge bugfix/missingparts-include-pd into main (v1.19.0)`.

## 13. Out-of-Scope

- Validierungs-Pflicht: System zwingt Lager NICHT, Picked<Requested-Items zu klassifizieren. Bei None bleibt es Closed mit ungeklaerter Differenz. Falls spaeter Pflicht gewuenscht: separater Spec.
- Workflow-Eskalations-Aktionen (z.B. automatischer Sage-Bestellauftrag bei NoRestock): out-of-scope.
- E-Mail-Benachrichtigungen bei Klassifizierungs-Wechsel: out-of-scope.
- UI-Spalten-Filter pro Tab im MissingParts/Index (bestehender Server-Side-Filter bleibt — Tab-Param ist zusaetzlich): out-of-scope.

## 14. Risiken

- **Daten-destruktive Migration**: Up() droppt `IsFinalShortage`-Spalte. Down() rekonstruiert sie aber verliert die `None`/`WillBeRestocked`-Unterscheidung. **Backup vor Deploy auf Produktion**.
- **Branch-Historie**: 2 vorab gemachte v1.18.1-Commits (`f3deba6`, `fb4d21e`) sind funktional sinnvoll (Filter-Erweiterung) — werden nicht zurueckgesetzt, sondern bleiben Teil der v1.19.0-Historie. Die v1.18.1-Doku (Changelog, PROJECT_STATUS) wird aber im Rahmen von v1.19.0 ueberarbeitet.
- **Frontend-Migration**: Inline-JS in Details.cshtml wird substantiell umgebaut. Bestehende Autosave-on-Blur-Logik muss erhalten bleiben (sendet `shortageStatuses[]` mit dem aktuellen Hidden-Wert mit).
- **3-State-Radio UX**: Doppelklick-zu-None ist eine ungewoehnliche Interaktion. Tooltip + ggf. zusaetzlicher "Zuruecksetzen"-Mini-Button als Alternative — Entscheidung in Plan.
