# Customizable View Preferences — Design Spec

**Datum:** 2026-04-10
**Status:** Approved
**Views:** ProductionOrders, Picking (Index), OseonTracking, BOM (Picking/Bom)

---

## Zusammenfassung

Benutzer sollen ihre Tabellenansichten individuell anpassen koennen: Spalten ein-/ausblenden, Spaltenbreiten per Drag aendern, Spaltenreihenfolge per Drag & Drop aendern (nur flache Tabellen), und Standard-Sortierung festlegen. Einstellungen werden pro Benutzer und pro View in der Datenbank gespeichert. Admins koennen Einstellungen im Benutzerstamm zuruecksetzen.

---

## Prerequisite: table-filter.js Refactoring

### Problem

`table-filter.js` verwendet ueberall **numerische `data-col` Indices**. Sortierung, Filter und Hard-coded Default-Sorts (z.B. `triggerSort(10, 'asc')` in ProductionOrders) referenzieren Spalten per Index. Wenn Spalten ausgeblendet, umsortiert oder bedingt gerendert werden, zeigen die Indices auf falsche Spalten.

Zusaetzlich: ProductionOrders verwendet 1-basierte Indices, Picking 0-basierte — inkonsistent.

### Loesung

- `data-col` (numerisch) wird durch `data-col-key` (String-basiert) ersetzt in **allen** Views und in `table-filter.js`
- Beispiel: `data-col="10"` wird zu `data-col-key="commissioningDate"`
- `table-filter.js` wird refactored: Filter-Inputs, Sortierung und globale Funktionen (`getActiveFilters()`, `setColumnFilter()`, `triggerSort()`) arbeiten mit String-Keys
- Hard-coded Sorts werden umgestellt: `triggerSort('commissioningDate', 'asc')`
- `table-filter.js` ermittelt den physischen Index einer Spalte dynamisch zur Laufzeit per `data-col-key` Lookup

---

## Feature-Matrix

| View | Hide/Show | Resize | Reorder | Sort-Default | Locked Columns |
|------|-----------|--------|---------|-------------|----------------|
| ProductionOrders (Index) | Ja | Ja | Ja | Ja | OrderNumber (FA-Nummer) |
| Picking (Index) | Ja | Ja | Ja | Ja | OrderNumber (FA-Nummer) |
| OseonTracking (OseonIndex) | Ja | Ja | Nein | Nein | CustomerOrderNumber (Kunden-Auftrags-Nr) |
| BOM (Picking/Bom) | Ja | Ja | Nein | Nein | Position |

**Begruendung eingeschraenkter Scope fuer OSEON + BOM:**
- OseonIndex ist eine 3-Ebenen-Baumstruktur (Kundenauftrag → Subauftrag → Arbeitsgang) — Spalten-Reorder wuerde die Expand/Collapse-Logik und visuelle Hierarchie brechen
- BOM (Picking/Bom) ist ebenfalls eine Baumstruktur mit verschachtelten Zeilen, Checkboxen und Transfer-AJAX — Reorder wuerde Struktur kaputtmachen
- Beide Views haben serverseitige Sortierung — kein clientseitiger Sort-Default noetig

---

## Datenmodell

### Neue Entity: `UserViewPreference`

```
UserViewPreference : AuditableEntity
├── UserId          int, FK → Users (required)
├── ViewKey         string (max 50), z.B. "ProductionOrders", "Picking", "OseonTracking", "Bom"
├── SettingsJson    nvarchar(max)
└── Unique Constraint: (UserId, ViewKey)
```

Erbt von `AuditableEntity` (Id, CreatedAt, CreatedBy, CreatedByWindows, ModifiedAt, ModifiedBy, ModifiedByWindows).

### JSON-Struktur (SettingsJson)

```json
{
  "columns": [
    { "key": "OrderNumber", "visible": true, "width": 120, "order": 0 },
    { "key": "ArticleNumber", "visible": true, "width": null, "order": 1 },
    { "key": "CoatingDate", "visible": false, "width": 90, "order": 8 }
  ],
  "defaultSortColumn": "ProductionDate",
  "defaultSortDirection": "asc"
}
```

Regeln:
- Spalten die **nicht** im JSON stehen → System-Default (sichtbar, Standard-Breite, Standard-Reihenfolge)
- `width: null` → System-Default-Breite
- `visible` fehlt → `true`
- `defaultSortColumn` / `defaultSortDirection` nur bei Views mit Sort-Default-Support (ProductionOrders, Picking)
- Fuer OSEON + BOM: nur `columns` (visible + width), kein `order`, kein Sort
- **Unbekannte Keys werden ignoriert**: Wenn ein gespeicherter Spalten-Key nicht in der aktuellen column-config existiert (z.B. bedingte Spalte deaktiviert), wird der Eintrag uebersprungen — kein Fehler, kein Datenverlust

### Admin-Reset

- Zeile fuer User+ViewKey loeschen → User sieht wieder System-Default
- Reset-Optionen im Benutzerstamm (Users/Edit)

---

## Backend

### Repository: `IUserViewPreferenceRepository`

```csharp
Task<UserViewPreference?> GetByUserAndViewAsync(int userId, string viewKey);
Task SaveAsync(int userId, string viewKey, string settingsJson);       // Upsert
Task DeleteByUserAndViewAsync(int userId, string viewKey);             // Reset einzelne View
Task DeleteAllByUserAsync(int userId);                                 // Reset alle Views
Task<List<UserViewPreference>> GetAllByUserAsync(int userId);          // Fuer Admin-Anzeige
```

### API-Controller: `Api/UserViewPreferencesApiController`

```
GET    /api/user-view-preferences/{viewKey}     → 200 + JSON oder 204 (kein Setting)
PUT    /api/user-view-preferences/{viewKey}     → 200 (Upsert, Body: SettingsJson)
DELETE /api/user-view-preferences/{viewKey}     → 200 (Reset einzelne View)
```

- Arbeitet immer mit dem **eingeloggten User** aus Session (`ICurrentUserService`)
- `viewKey` wird gegen erlaubte Liste validiert: `ProductionOrders`, `Picking`, `OseonTracking`, `Bom`
- `SettingsJson` max. 64KB
- Kein Rollen-Filter — jeder eingeloggte User darf seine eigenen Einstellungen aendern. Login-Check via `ICurrentUserService.IsLoggedIn()`, nicht eingeloggte Requests erhalten 401

### Admin-Reset im UsersController

```
POST /Users/{id}/ResetViewPreferences?viewKey=ProductionOrders   → Reset einzelne View
POST /Users/{id}/ResetViewPreferences                             → Reset alle Views (ohne viewKey)
```

- Geschuetzt mit `[RequireMasterDataAccess]`
- `TempData["SuccessMessage"]` nach Reset
- Redirect zurueck auf Users/Edit

---

## Spalten-Konfiguration (serverseitig)

### Statische Klasse: `ColumnDefinitions`

```csharp
public static class ColumnDefinitions
{
    public static readonly ViewConfig ProductionOrders = new("ProductionOrders", supportsReorder: true, supportsSortDefault: true)
    {
        Columns =
        {
            new("OrderNumber", "FA-Nummer", locked: true, defaultWidth: 120),
            new("Quantity", "Stk.", locked: false, defaultWidth: 60),
            new("CustomerName", "Kunde", locked: false, defaultWidth: 150),
            // ... alle Spalten
        }
    };

    public static readonly ViewConfig Picking = new("Picking", supportsReorder: true, supportsSortDefault: true)
    {
        Columns =
        {
            new("Priority", "Prio", locked: false, defaultWidth: 60),
            new("OrderNumber", "FA-Nr.", locked: true, defaultWidth: 120),
            new("ArticleNumber", "Artikelnummer", locked: false, defaultWidth: 150),
            new("Description", "Bezeichnung", locked: false, defaultWidth: 200),
            new("CustomerName", "Kunde", locked: false, defaultWidth: 150),
            new("Quantity", "Stk.", locked: false, defaultWidth: 60),
            new("CommissioningDate", "Komm.-Termin", locked: false, defaultWidth: 110),
            new("Status", "Status", locked: false, defaultWidth: 100),
            // Bedingt: Picker (nur wenn PickerAssignmentEnabled)
        }
    };

    public static readonly ViewConfig OseonTracking = new("OseonTracking", supportsReorder: false, supportsSortDefault: false)
    {
        Columns =
        {
            new("OrderNumber", "Auftrag", locked: false, defaultWidth: null),
            new("CustomerOrderNumber", "Kunden-Auftrags-Nr", locked: true, defaultWidth: null),
            new("ArticleNumber", "Artikelnr.", locked: false, defaultWidth: null),
            new("Description", "Bezeichnung", locked: false, defaultWidth: null),
            new("Workplace", "Werkbank", locked: false, defaultWidth: null),
            new("Status", "Status", locked: false, defaultWidth: null),
            new("QuantityTarget", "Soll/Ist", locked: false, defaultWidth: null),
            new("DueDate", "Endtermin", locked: false, defaultWidth: null),
        }
    };

    public static readonly ViewConfig Bom = new("Bom", supportsReorder: false, supportsSortDefault: false)
    {
        Columns =
        {
            new("Position", "Pos.", locked: true, defaultWidth: 60),
            new("ArticleNumber", "Artikelnummer", locked: false, defaultWidth: 150),
            new("Description", "Bezeichnung", locked: false, defaultWidth: 200),
            new("Quantity", "Menge", locked: false, defaultWidth: 80),
            new("Unit", "Einheit", locked: false, defaultWidth: 60),
            new("Category", "Kategorie", locked: false, defaultWidth: 120),
            new("Stock", "Bestand", locked: false, defaultWidth: 80),
            new("StorageLocation", "Lagerplatz", locked: false, defaultWidth: 120),
        }
    };
}

public record ColumnDef(string Key, string Label, bool Locked = false, int? DefaultWidth = null);
public record ViewConfig(string ViewKey, bool SupportsReorder, bool SupportsSortDefault)
{
    public List<ColumnDef> Columns { get; init; } = new();
}
```

- Einzige Quelle der Wahrheit fuer Spalten-Keys, Labels, Locked-Status, Default-Breiten
- Views referenzieren diese Definition
- Bedingte Spalten (z.B. `@if(Model.LeitstandAktiv)`) werden weiterhin serverseitig gerendert — nur gerenderte Spalten erscheinen in der `column-config` JSON im HTML

---

## Frontend

### Neue Datei: `wwwroot/js/column-preferences.js`

Generisches Modul das sich an jede Tabelle mit `data-view-key` haengt.

### Spalten-Definition im HTML

Jede View rendert eine JSON-Definition:

```html
<script type="application/json" id="column-config">
[
  { "key": "OrderNumber", "label": "FA-Nummer", "locked": true, "defaultWidth": 120 },
  { "key": "ArticleNumber", "label": "Artikelnummer", "locked": false, "defaultWidth": 150 }
]
</script>
<script type="application/json" id="view-config">
{ "viewKey": "ProductionOrders", "supportsReorder": true, "supportsSortDefault": true }
</script>
```

### Zahnrad-Dialog (Offcanvas)

- Zahnrad-Icon (`bi-gear`) rechts oben ueber der Tabelle, neben bestehenden Page-Header-Buttons
- Oeffnet Bootstrap 5 Offcanvas von rechts (`.offcanvas-end`), ca. 350px breit
- Inhalt:
  - **Spalten-Liste** mit Drag-Handles (nur bei supportsReorder) + Checkboxen zum Ein-/Ausblenden
  - Locked-Spalten: Checkbox deaktiviert, kein Drag-Handle, Hinweis "Pflichtspalte"
  - **Standard-Sortierung** (nur bei supportsSortDefault): Dropdown (Spalte) + Richtung (Auf/Ab)
  - **"Zuruecksetzen"**-Button: `btn-outline-danger`, loescht per DELETE die Einstellungen, laedt Seite neu

### Rechtsklick-Kontextmenue

- Rechtsklick auf `<th>` → positioniertes Dropdown:
  - "Spalte ausblenden" (nur bei nicht-locked Spalten)
  - "Alle Spalten anzeigen"
  - "Spalten-Einstellungen..." (oeffnet Offcanvas)
- Vanilla JS, absolut positioniertes `<div>` im Bootstrap-Dropdown-Stil
- Schliesst bei Klick ausserhalb oder Escape-Taste
- **Deaktiviert auf Touch-Geraeten** (Long-Press zu fehleranfaellig)

### Spaltenbreiten-Resize

- Jeder `<th>` bekommt Resize-Handle am rechten Rand (3px vertikaler Strich)
- Unsichtbar im Ruhezustand, sichtbar bei Hover
- Cursor: `col-resize`
- Mousedown → Mousemove → Mouseup: setzt `style.width` auf `<th>` und zugehoerige `<td>`
- Doppelklick auf Resize-Handle → Reset auf Default-Breite
- Aktiver Drag: blauer Strich (`--ake-secondary`) als Feedback
- **Deaktiviert auf Touch-Geraeten**

### Spalten-Reihenfolge (Drag & Drop)

- Nur bei Views mit `supportsReorder: true` (ProductionOrders, Picking)
- Drag & Drop der `<th>` Elemente — ganze Spalte (Header + Zeilen) wird verschoben
- Vanilla JS mit `dragstart`, `dragover`, `drop` Events
- Locked-Spalten sind nicht draggable und nicht als Drop-Target vor ihnen verfuegbar
- **Auf Touch-Geraeten:** Reorder nur ueber Offcanvas-Panel (Drag auf Liste, nicht auf Tabelle)

### Laden & Speichern

```
Seitenaufbau:
1. Razor rendert Tabelle mit System-Default-Reihenfolge
2. column-preferences.js liest column-config JSON + GET /api/user-view-preferences/{viewKey}
3. JS wendet Einstellungen an: Spalten ausblenden (display:none), umsortieren, Breiten setzen
4. JS wendet Default-Sort an (triggert table-filter.js triggerSort())
5. table-filter.js initialisiert Filter-Row (nur sichtbare Spalten)

Aenderung:
1. User aendert etwas (Hide/Show, Resize, Reorder, Sort-Default)
2. Debounce 1.5 Sekunden
3. PUT /api/user-view-preferences/{viewKey} mit aktuellem State
```

### Integration mit table-filter.js

- **Lade-Reihenfolge**: Beide Scripts werden geladen, aber Initialisierung ist sequentiell:
  1. `table-filter.js` wird geladen → registriert globale Funktionen (`triggerSort`, `setColumnFilter` etc.)
  2. `column-preferences.js` wird geladen → wartet auf `DOMContentLoaded`
  3. `column-preferences.js` Init: holt Preferences per API, wendet Spalten-Einstellungen an (Hide, Reorder, Resize)
  4. `column-preferences.js` dispatcht Custom Event `column-preferences-ready`
  5. `table-filter.js` Init: wartet auf `column-preferences-ready` Event (oder startet sofort wenn kein `data-view-key` vorhanden), baut Filter-Row nur fuer sichtbare Spalten
  6. `column-preferences.js` triggert Default-Sort via `window.triggerSort()`
- Ausgeblendete Spalten: `display: none` auf `<th>` und `<td>` per `data-col-key` Selektor
- `table-filter.js` ignoriert unsichtbare Spalten (Filter-Inputs werden mit ausgeblendet)

---

## CSS

### Resize-Handles

```css
th .col-resize-handle {
    position: absolute; right: 0; top: 0; bottom: 0;
    width: 3px; cursor: col-resize;
    opacity: 0; transition: opacity 0.15s;
}
th:hover .col-resize-handle { opacity: 0.5; background: var(--ake-secondary); }
th .col-resize-handle.active { opacity: 1; background: var(--ake-secondary); }
```

### Kontextmenue

```css
.column-context-menu {
    position: absolute; z-index: 1060;
    background: white; border: 1px solid rgba(0,0,0,.15);
    border-radius: .375rem; box-shadow: 0 .5rem 1rem rgba(0,0,0,.15);
    min-width: 180px;
}
```

### Offcanvas-Anpassungen

- Drag-Handle Icon: `bi-grip-vertical`, Farbe `--ake-secondary`
- Locked-Spalten: Checkbox `disabled`, Text `text-muted`, kein Drag-Handle

### Mobile

- Zahnrad-Icon bleibt sichtbar, Offcanvas funktioniert nativ auf Mobile
- Resize-Handles: `display: none` auf Touch-Geraeten (`@media (hover: none)`)
- Kontextmenue: deaktiviert auf Touch (`'ontouchstart' in window` Check)

---

## View-Aenderungen

### Alle 4 Views bekommen:

1. `data-view-key="..."` auf dem `<table>` Element
2. `data-col-key="OrderNumber"` auf jedem `<th>` und `<td>` (ersetzt numerisches `data-col`)
3. `<script type="application/json" id="column-config">` Block
4. `<script type="application/json" id="view-config">` Block
5. Einbindung von `column-preferences.js` nach `table-filter.js`
6. Zahnrad-Icon im Tabellen-Header-Bereich

### Bedingte Spalten

Spalten die serverseitig nicht gerendert werden (z.B. `@if(Model.LeitstandAktiv)`) existieren fuer das Preference-System nicht. Sie erscheinen weder im `column-config` JSON noch in der Tabelle. Wenn sich die Berechtigung aendert (z.B. Leitstand wird aktiviert), erscheint die Spalte mit System-Default-Einstellungen.

### Admin-Bereich (Users/Edit)

Neuer Abschnitt "Ansichts-Einstellungen" am Ende des Edit-Formulars:

```
Ansichts-Einstellungen
──────────────────────────────────────
Fertigungsauftraege      [Zuruecksetzen]
Kommissionierliste       [Zuruecksetzen]
OSEON Teileverfolgung    [Zuruecksetzen]
Stueckliste (BOM)        [Zuruecksetzen]

[Alle Ansichten zuruecksetzen]
```

- Nur Views anzeigen fuer die der User tatsaechlich Einstellungen gespeichert hat
- Buttons loesen `POST /Users/{id}/ResetViewPreferences?viewKey=...` aus
- `TempData["SuccessMessage"]` Feedback

---

## Migration

### SQL-Script: `SQL/41_AddUserViewPreferences.sql`

```sql
IF OBJECT_ID(N'dbo.UserViewPreferences', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[UserViewPreferences] (
        [Id]                INT IDENTITY(1,1) NOT NULL,
        [UserId]            INT NOT NULL,
        [ViewKey]           NVARCHAR(50) NOT NULL,
        [SettingsJson]      NVARCHAR(MAX) NOT NULL,
        [CreatedAt]         DATETIME2 NOT NULL,
        [CreatedBy]         NVARCHAR(200) NULL,
        [CreatedByWindows]  NVARCHAR(200) NULL,
        [ModifiedAt]        DATETIME2 NULL,
        [ModifiedBy]        NVARCHAR(200) NULL,
        [ModifiedByWindows] NVARCHAR(200) NULL,
        CONSTRAINT [PK_UserViewPreferences] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserViewPreferences_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE,
        CONSTRAINT [UQ_UserViewPreferences_User_View] UNIQUE ([UserId], [ViewKey])
    );
END
GO
```

- `ON DELETE CASCADE`: Wenn User geloescht wird, werden Einstellungen automatisch entfernt
- EF Migration parallel dazu erstellen

---

## Nicht im Scope

- Kein Admin-definiertes Default-Profil (nur System-Default / Hardcoded)
- Keine Profil-Wechsel-Funktion
- Keine Filter-Voreinstellungen (nur Sortierung)
- Kein Export/Import von Einstellungen
- Kein Reorder / Sort-Default fuer Baum-Views (OSEON, BOM)
- Touch-Geraete: kein Resize, kein Rechtsklick-Menue (Offcanvas funktioniert)
- Keine weiteren Views (StockOverview, PartRequisitions etc.) — spaeter erweiterbar
