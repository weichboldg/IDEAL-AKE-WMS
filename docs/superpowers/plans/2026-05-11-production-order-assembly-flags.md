# Produktionsauftragsliste: 5 neue Baugruppen-Flags (VK/VL/VE/VT/VA) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fünf weitere manuell setzbare Checkbox-Flags in der Produktionsauftragsliste — funktional identisch zu den bestehenden Glas/Zukauf-Spalten, mit Kurz-Header (VK/VL/VE/VT/VA) und Volltext-Tooltip ("VK Kälte" etc.).

**Mapping:**

| Abk. | Volltext  | Code-Property        | Column-Key       |
|------|-----------|----------------------|------------------|
| VK   | Kälte     | `HasCooling`         | `cooling`        |
| VL   | Lüfter    | `HasFan`             | `fan`            |
| VE   | Elektro   | `HasElectric`        | `electric`       |
| VT   | Türen     | `HasDoors`           | `doors`          |
| VA   | Aufbau    | `HasSuperstructure`  | `superstructure` |

**Architecture:** 4 Schichten:
- **Schema** — Entity `ProductionOrder` bekommt 5 `bool`-Properties; EF Migration + SQL-Script; FreshInstall + AgentJob-Kommentar aktualisiert.
- **API** — `ProductionOrdersApiController.ToggleField` erweitert um 5 Property-Branches; Whitelist erweitert.
- **ViewModel + Repo-Mapping** — ViewModel spiegelt 5 Properties; Repo-Projektion ergänzt.
- **View** — `ColumnDefinitions.cs` + `Index.cshtml` (thead + tbody + column-preferences-JSON) bekommen 5 neue Spalten nach `purchase`, vor `status`.

**Branch:** `feature/sage-lagerbestand-sync` (Phase-2-Bundle, weiter angereichert).
**Version:** **Bleibt v1.10.0** — Bundle-Release. Bestehende Changelog-Card v1.10.0 wird erweitert.

**Spec:** `docs/superpowers/specs/2026-05-11-production-order-assembly-flags-design.md`.

**Commit-Konvention:** `feat(productionorders): ...` / `docs: ...`. Co-Authored-By trailer.

**Files:**
- Modify: `IdealAkeWms/Models/ProductionOrder.cs`
- Modify: `IdealAkeWms/Models/ViewModels/ProductionOrderViewModel.cs`
- Modify: `IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs`
- Modify: `IdealAkeWms/Controllers/ProductionOrdersApiController.cs`
- Modify: `IdealAkeWms/Data/Repositories/ProductionOrderRepository.cs` (oder wo immer das ViewModel-Mapping erfolgt — beim Implementieren suchen)
- Modify: `IdealAkeWms/Views/ProductionOrders/Index.cshtml`
- Modify: `IdealAkeWms/Views/Help/Changelog.cshtml`
- Modify: `IdealAkeWms/Views/Help/Index.cshtml` (falls Production-Orders-Hilfe existiert; sonst skip)
- New: `IdealAkeWms/Migrations/2026MMDDhhmmss_AddProductionOrderAssemblyFlags.cs` (von EF generiert)
- New: `SQL/59_AddProductionOrderAssemblyFlags.sql`
- Modify: `SQL/00_FreshInstall.sql`
- Modify: `SQL/AgentJobs/01_Import_Produktionsauftraege.sql` (Kommentar-Header)
- Modify: `docs/TESTSZENARIEN.md`

---

## Task 1: Schema (Entity + Migration + SQL-Script)

**Files:**
- Modify: `IdealAkeWms/Models/ProductionOrder.cs`
- New: `IdealAkeWms/Migrations/*_AddProductionOrderAssemblyFlags.cs`
- New: `SQL/59_AddProductionOrderAssemblyFlags.sql`
- Modify: `SQL/00_FreshInstall.sql`
- Modify: `SQL/AgentJobs/01_Import_Produktionsauftraege.sql`

- [ ] **Step 1: Entity erweitern**

In `IdealAkeWms/Models/ProductionOrder.cs` nach Zeile 50 (nach `HasExternalPurchase`) und VOR `HasCoatingParts` (Zeile 52+):

```csharp
[Display(Name = "VK Kälte")]
public bool HasCooling { get; set; }

[Display(Name = "VL Lüfter")]
public bool HasFan { get; set; }

[Display(Name = "VE Elektro")]
public bool HasElectric { get; set; }

[Display(Name = "VT Türen")]
public bool HasDoors { get; set; }

[Display(Name = "VA Aufbau")]
public bool HasSuperstructure { get; set; }
```

- [ ] **Step 2: EF Migration generieren**

```pwsh
dotnet ef migrations add AddProductionOrderAssemblyFlags --project IdealAkeWms
```

Erwartet: Migration-File unter `IdealAkeWms/Migrations/` mit 5 `AddColumn`-Operations für die ProductionOrders-Tabelle. Verify, dass jede Spalte `nullable: false, defaultValue: false` bekommt.

- [ ] **Step 3: SQL-Script anlegen**

`SQL/59_AddProductionOrderAssemblyFlags.sql`:

```sql
-- 59_AddProductionOrderAssemblyFlags.sql
-- Feature: VK/VL/VE/VT/VA Checkbox-Flags auf ProductionOrders (Kaelte, Luefter, Elektro, Tueren, Aufbau)

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.ProductionOrders') AND name = N'HasCooling')
BEGIN
    ALTER TABLE [dbo].[ProductionOrders] ADD [HasCooling] BIT NOT NULL CONSTRAINT [DF_ProductionOrders_HasCooling] DEFAULT (0);
    PRINT 'Spalte HasCooling zu ProductionOrders hinzugefügt.';
END
ELSE
BEGIN
    PRINT 'Spalte HasCooling existiert bereits.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.ProductionOrders') AND name = N'HasFan')
BEGIN
    ALTER TABLE [dbo].[ProductionOrders] ADD [HasFan] BIT NOT NULL CONSTRAINT [DF_ProductionOrders_HasFan] DEFAULT (0);
    PRINT 'Spalte HasFan zu ProductionOrders hinzugefügt.';
END
ELSE
BEGIN
    PRINT 'Spalte HasFan existiert bereits.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.ProductionOrders') AND name = N'HasElectric')
BEGIN
    ALTER TABLE [dbo].[ProductionOrders] ADD [HasElectric] BIT NOT NULL CONSTRAINT [DF_ProductionOrders_HasElectric] DEFAULT (0);
    PRINT 'Spalte HasElectric zu ProductionOrders hinzugefügt.';
END
ELSE
BEGIN
    PRINT 'Spalte HasElectric existiert bereits.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.ProductionOrders') AND name = N'HasDoors')
BEGIN
    ALTER TABLE [dbo].[ProductionOrders] ADD [HasDoors] BIT NOT NULL CONSTRAINT [DF_ProductionOrders_HasDoors] DEFAULT (0);
    PRINT 'Spalte HasDoors zu ProductionOrders hinzugefügt.';
END
ELSE
BEGIN
    PRINT 'Spalte HasDoors existiert bereits.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.ProductionOrders') AND name = N'HasSuperstructure')
BEGIN
    ALTER TABLE [dbo].[ProductionOrders] ADD [HasSuperstructure] BIT NOT NULL CONSTRAINT [DF_ProductionOrders_HasSuperstructure] DEFAULT (0);
    PRINT 'Spalte HasSuperstructure zu ProductionOrders hinzugefügt.';
END
ELSE
BEGIN
    PRINT 'Spalte HasSuperstructure existiert bereits.';
END
GO
```

- [ ] **Step 4: `SQL/00_FreshInstall.sql` aktualisieren**

In `SQL/00_FreshInstall.sql` die `CREATE TABLE [dbo].[ProductionOrders]`-Anweisung suchen und die 5 neuen Spalten direkt nach `[HasExternalPurchase] BIT NOT NULL DEFAULT (0)` einfügen:

```sql
[HasCooling] BIT NOT NULL DEFAULT (0),
[HasFan] BIT NOT NULL DEFAULT (0),
[HasElectric] BIT NOT NULL DEFAULT (0),
[HasDoors] BIT NOT NULL DEFAULT (0),
[HasSuperstructure] BIT NOT NULL DEFAULT (0),
```

Außerdem: Im `__EFMigrationsHistory`-INSERT-Block am Ende der Datei einen neuen Eintrag für die Migration ergänzen (Migration-ID aus Step 2).

Falls die Datei das schon konsolidiert für ältere Migrations macht (z. B. nur den letzten ID einfügt): an bestehende Konvention halten.

- [ ] **Step 5: AgentJob-Kommentar erweitern**

In `SQL/AgentJobs/01_Import_Produktionsauftraege.sql` den Header-Kommentar erweitern:

```sql
-- Felder die NICHT ueberschrieben werden (App-verwaltet):
--   IsDone, PickingStatus, HasGlass, HasExternalPurchase,
--   HasCooling, HasFan, HasElectric, HasDoors, HasSuperstructure,
--   HasCoatingParts, IsCoatingDone,
--   CreatedAt, CreatedBy, CreatedByWindows
```

Und:

```sql
-- Felder der Zieltabelle die bei INSERT nicht befuellt werden (haben Defaults):
--   PickingStatus    → NULL
--   HasGlass         → 0 (DEFAULT)
--   HasExternalPurchase → 0 (DEFAULT)
--   HasCooling/HasFan/HasElectric/HasDoors/HasSuperstructure → 0 (DEFAULT)
```

**Verify:** Die eigentliche MERGE-Logik darf die neuen Spalten NICHT in `UPDATE SET` oder `INSERT`-Spaltenliste haben. Wenn doch (oder wenn `SELECT *` benutzt wird): MERGE-Statement so anpassen, dass die neuen Spalten NICHT überschrieben werden (analog zu HasGlass-Behandlung — vermutlich keine Änderung nötig, weil HasGlass auch nicht in der MERGE-Liste steht).

- [ ] **Step 6: Build + EF-Sanity-Check + Tests**

```pwsh
dotnet build --nologo
dotnet ef migrations list --project IdealAkeWms --no-build
dotnet test --nologo --no-build
```

Expected: 0 errors, neue Migration in der Liste, alle Tests grün.

- [ ] **Step 7: Commit**

```pwsh
git add IdealAkeWms/Models/ProductionOrder.cs IdealAkeWms/Migrations/ SQL/59_AddProductionOrderAssemblyFlags.sql SQL/00_FreshInstall.sql SQL/AgentJobs/01_Import_Produktionsauftraege.sql
git commit -m @'
feat(productionorders): add assembly flags HasCooling/HasFan/HasElectric/HasDoors/HasSuperstructure schema

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
'@
```

---

## Task 2: ViewModel + API + Repo-Mapping

**Files:**
- Modify: `IdealAkeWms/Models/ViewModels/ProductionOrderViewModel.cs`
- Modify: `IdealAkeWms/Controllers/ProductionOrdersApiController.cs`
- Modify: `IdealAkeWms/Data/Repositories/ProductionOrderRepository.cs` (oder ggf. andere Mapping-Stelle)

- [ ] **Step 1: ViewModel erweitern**

In `IdealAkeWms/Models/ViewModels/ProductionOrderViewModel.cs` direkt nach `public bool HasExternalPurchase { get; set; }` (Zeile 42):

```csharp
public bool HasCooling { get; set; }
public bool HasFan { get; set; }
public bool HasElectric { get; set; }
public bool HasDoors { get; set; }
public bool HasSuperstructure { get; set; }
```

- [ ] **Step 2: Repo-Mapping finden + erweitern**

```pwsh
# Mapping-Stelle finden
```

Mit Grep nach `HasGlass = ` ODER `HasGlass =` im ganzen Codebase suchen. Erwartet: mindestens eine Projektion in einem Repository (vermutlich `ProductionOrderRepository.cs`), die ein `ProductionOrder` auf `ProductionOrderViewModel` mappt.

In **jeder** gefundenen Stelle (außer `ProductionOrder.cs` und `ProductionOrderViewModel.cs`) die 5 neuen Felder direkt nach `HasExternalPurchase = ...,` ergänzen:

```csharp
HasCooling = po.HasCooling,
HasFan = po.HasFan,
HasElectric = po.HasElectric,
HasDoors = po.HasDoors,
HasSuperstructure = po.HasSuperstructure,
```

(Variable `po` ggf. anders benannt — an Stelle anpassen.)

- [ ] **Step 3: API Toggle-Endpoint erweitern**

In `IdealAkeWms/Controllers/ProductionOrdersApiController.cs`:

**3a.** Zeile 12 — `AllowedToggleFields` HashSet erweitern:

```csharp
private static readonly HashSet<string> AllowedToggleFields = [
    "HasGlass", "HasExternalPurchase", "IsCoatingDone",
    "HasCooling", "HasFan", "HasElectric", "HasDoors", "HasSuperstructure"
];
```

**3b.** Zeile 47-52 — if/else-Chain in `ToggleField` erweitern. Direkt vor `await _repository.UpdateAsync(order);` (Zeile 54):

```csharp
else if (request.Field == "HasCooling")
    order.HasCooling = request.Value;
else if (request.Field == "HasFan")
    order.HasFan = request.Value;
else if (request.Field == "HasElectric")
    order.HasElectric = request.Value;
else if (request.Field == "HasDoors")
    order.HasDoors = request.Value;
else if (request.Field == "HasSuperstructure")
    order.HasSuperstructure = request.Value;
```

- [ ] **Step 4: Build + Tests**

```pwsh
dotnet build --nologo
dotnet test --nologo --no-build
```

Expected: 0 errors, alle Tests grün (565/565 + ggf. zusätzliche von Vorgänger-Tasks).

- [ ] **Step 5: Commit**

```pwsh
git add IdealAkeWms/Models/ViewModels/ProductionOrderViewModel.cs IdealAkeWms/Controllers/ProductionOrdersApiController.cs IdealAkeWms/Data/Repositories/ProductionOrderRepository.cs
git commit -m @'
feat(productionorders): expose assembly flags via viewmodel and toggle api

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
'@
```

---

## Task 3: View (ColumnDefinitions + Index.cshtml)

**Files:**
- Modify: `IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs`
- Modify: `IdealAkeWms/Views/ProductionOrders/Index.cshtml`

- [ ] **Step 1: ColumnDefinitions erweitern**

In `IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs` nach dem `purchase`-Eintrag (Zeile 39) und vor dem `status`-Eintrag (Zeile 40):

```csharp
new ColumnDef("cooling",        "VK", Locked: false, DefaultWidth: 40),
new ColumnDef("fan",            "VL", Locked: false, DefaultWidth: 40),
new ColumnDef("electric",       "VE", Locked: false, DefaultWidth: 40),
new ColumnDef("doors",          "VT", Locked: false, DefaultWidth: 40),
new ColumnDef("superstructure", "VA", Locked: false, DefaultWidth: 40),
```

- [ ] **Step 2: View-Thead erweitern**

In `IdealAkeWms/Views/ProductionOrders/Index.cshtml` Zeile 117 (`<th ... data-col-key="purchase">Zukauf</th>`) — direkt DANACH und vor Zeile 118 (`<th ... data-col-key="status">Status</th>`):

```cshtml
<th style="width: 40px;" data-filterable data-col-key="cooling"        title="VK Kälte">VK</th>
<th style="width: 40px;" data-filterable data-col-key="fan"            title="VL Lüfter">VL</th>
<th style="width: 40px;" data-filterable data-col-key="electric"       title="VE Elektro">VE</th>
<th style="width: 40px;" data-filterable data-col-key="doors"          title="VT Türen">VT</th>
<th style="width: 40px;" data-filterable data-col-key="superstructure" title="VA Aufbau">VA</th>
```

- [ ] **Step 3: View-Tbody erweitern**

In derselben Datei Zeile 228-231 (das `<td>` mit `HasExternalPurchase`-Checkbox) — direkt DANACH und vor Zeile 232 (Status-`<td>`):

```cshtml
<td class="text-center">
    <input type="checkbox" class="form-check-input toggle-field" data-id="@item.Id" data-field="HasCooling"
           title="VK Kälte" @(item.HasCooling ? "checked" : "") @(!Model.CanPick ? "disabled" : "") />
</td>
<td class="text-center">
    <input type="checkbox" class="form-check-input toggle-field" data-id="@item.Id" data-field="HasFan"
           title="VL Lüfter" @(item.HasFan ? "checked" : "") @(!Model.CanPick ? "disabled" : "") />
</td>
<td class="text-center">
    <input type="checkbox" class="form-check-input toggle-field" data-id="@item.Id" data-field="HasElectric"
           title="VE Elektro" @(item.HasElectric ? "checked" : "") @(!Model.CanPick ? "disabled" : "") />
</td>
<td class="text-center">
    <input type="checkbox" class="form-check-input toggle-field" data-id="@item.Id" data-field="HasDoors"
           title="VT Türen" @(item.HasDoors ? "checked" : "") @(!Model.CanPick ? "disabled" : "") />
</td>
<td class="text-center">
    <input type="checkbox" class="form-check-input toggle-field" data-id="@item.Id" data-field="HasSuperstructure"
           title="VA Aufbau" @(item.HasSuperstructure ? "checked" : "") @(!Model.CanPick ? "disabled" : "") />
</td>
```

- [ ] **Step 4: View-Column-Preferences-JSON erweitern**

In derselben Datei das JSON-Array (ca. Zeile 487-492). Nach dem `purchase`-Eintrag und vor dem `status`-Eintrag:

```javascript
{ "key": "cooling",        "label": "VK", "locked": false, "defaultWidth": 40 },
{ "key": "fan",            "label": "VL", "locked": false, "defaultWidth": 40 },
{ "key": "electric",       "label": "VE", "locked": false, "defaultWidth": 40 },
{ "key": "doors",          "label": "VT", "locked": false, "defaultWidth": 40 },
{ "key": "superstructure", "label": "VA", "locked": false, "defaultWidth": 40 },
```

Achtung: das JSON-Array steht in Razor-Syntax — Kommas korrekt setzen.

- [ ] **Step 5: Konsistenz-Check (manuell)**

Selbst-Check: Anzahl `<th>` mit `data-col-key` in der View muss == Anzahl Einträge in `ColumnDefinitions.cs` + bedingte (release/picker). Falls Off-by-One: Wo fehlt der neue Eintrag?

Grep helps:
```pwsh
# Sollten 5 mal "cooling", "fan", "electric", "doors", "superstructure" finden
# (1× in ColumnDefinitions, 1× th, 1× td via data-field, 1× JSON, 1× evtl Migration = je nach Datei)
```

- [ ] **Step 6: Build + Tests**

```pwsh
dotnet build --nologo
dotnet test --nologo --no-build
```

Expected: 0 errors, alle Tests grün.

- [ ] **Step 7: Commit**

```pwsh
git add IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs IdealAkeWms/Views/ProductionOrders/Index.cshtml
git commit -m @'
feat(productionorders): add VK/VL/VE/VT/VA columns to order list with tooltips

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
'@
```

---

## Task 4: Changelog + Hilfeseite

**Files:**
- Modify: `IdealAkeWms/Views/Help/Changelog.cshtml`
- Modify: `IdealAkeWms/Views/Help/Index.cshtml` (falls passender Abschnitt existiert)

**Kein AppVersion-Bump** — bestehende v1.10.0-Card wird erweitert.

- [ ] **Step 1: Changelog v1.10.0 erweitern**

In `IdealAkeWms/Views/Help/Changelog.cshtml`, im **bestehenden** v1.10.0-Card-Block, an die `<ul>` einen Eintrag anhängen (vor dem schließenden `</ul>`):

```cshtml
<li><strong>Produktionsauftragsliste &ndash; 5 neue Baugruppen-Flags:</strong>
    Zus&auml;tzliche Checkbox-Spalten <strong>VK</strong> (K&auml;lte), <strong>VL</strong> (L&uuml;fter),
    <strong>VE</strong> (Elektro), <strong>VT</strong> (T&uuml;ren), <strong>VA</strong> (Aufbau).
    Funktional identisch zu Glas/Zukauf: manuell setzbar (Picking-Rolle erforderlich),
    Sage-Sync &uuml;berschreibt die Werte nicht. Header zeigt die Abk&uuml;rzung,
    Volltext als Tooltip beim Hover.</li>
```

- [ ] **Step 2: Hilfeseite — Produktionsauftrags-Abschnitt erweitern (falls vorhanden)**

In `IdealAkeWms/Views/Help/Index.cshtml` nach Abschnitt zu Produktionsauftragsliste suchen. Wenn vorhanden, einen Hinweis-Absatz einfügen:

```cshtml
<dt>Manuelle Status-Flags</dt>
<dd>
    Pro Auftrag k&ouml;nnen folgende Flags manuell gesetzt werden (Picking-Rolle erforderlich, Sage-Sync &auml;ndert sie nicht):
    <ul class="mt-1 mb-0">
        <li><strong>Glas</strong> &mdash; Glas-Bauteile vorbereiten</li>
        <li><strong>Zukauf</strong> &mdash; Zukauf-Teile bestellt/erhalten</li>
        <li><strong>VK</strong> &mdash; K&auml;lte-Baugruppe</li>
        <li><strong>VL</strong> &mdash; L&uuml;fter-Baugruppe</li>
        <li><strong>VE</strong> &mdash; Elektro-Baugruppe</li>
        <li><strong>VT</strong> &mdash; T&uuml;ren-Baugruppe</li>
        <li><strong>VA</strong> &mdash; Aufbau-Baugruppe</li>
    </ul>
</dd>
```

Falls kein passender Abschnitt zu Produktionsauftragsliste existiert: **skip this step** und nur Changelog-Eintrag committen.

- [ ] **Step 3: Build + Tests**

```pwsh
dotnet build --nologo
dotnet test --nologo --no-build
```

Expected: 0 errors, alle Tests grün.

- [ ] **Step 4: Commit**

```pwsh
git add IdealAkeWms/Views/Help/Changelog.cshtml IdealAkeWms/Views/Help/Index.cshtml
git commit -m @'
docs: extend v1.10.0 changelog and help with assembly flags

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
'@
```

---

## Task 5: TESTSZENARIEN-Update

**Files:**
- Modify: `docs/TESTSZENARIEN.md`

Neue Szenarien zur Produktionsauftragsliste-Sektion (TS-1.x).

- [ ] **Step 1: Bestehende TS-Nummerierung prüfen**

`docs/TESTSZENARIEN.md` lesen und prüfen, welche TS-1.x-Slots bereits belegt sind. Neue Szenarien fortlaufend nummerieren (z. B. TS-1.6 bis TS-1.14 wenn TS-1.5 das letzte existierende ist). Das Inhaltsverzeichnis entsprechend updaten.

- [ ] **Step 2: Szenarien einfügen**

Am Ende der Produktionsauftrags-Sektion (vor dem nächsten `---`-Trenner), folgenden Block ergänzen (Nummerierung anpassen):

```markdown
### TS-1.x-A — Neuer Auftrag hat alle 5 Flags auf false

**Vorbedingungen:**
- Frisch importierter Auftrag aus Sage.

**Schritte:**
1. Produktionsauftragsliste oeffnen.

**Erwartetes Verhalten:**
- Spalten **VK**, **VL**, **VE**, **VT**, **VA** zeigen unchecked Checkboxes fuer den neuen Auftrag.

---

### TS-1.x-B — Toggle VK persistiert ueber Page-Reload

**Vorbedingungen:**
- Ein Auftrag in der Liste sichtbar, User hat picking-Rolle.

**Schritte:**
1. VK-Checkbox des Auftrags aktivieren.
2. Page neu laden (F5).

**Erwartetes Verhalten:**
- VK-Checkbox bleibt nach Reload aktiviert.
- Andere Flags (VL/VE/VT/VA/Glas/Zukauf) bleiben unveraendert.

---

### TS-1.x-C — Alle 5 Flags unabhaengig toggeln

**Schritte:**
1. Nur VE aktivieren → Reload → nur VE checked.
2. Zusaetzlich VL aktivieren → Reload → VE und VL checked.
3. VE deaktivieren → Reload → nur VL checked.

**Erwartetes Verhalten:**
- Flags sind voneinander unabhaengig, kein Seiteneffekt.

---

### TS-1.x-D — Tooltip-Volltext bei Header-Hover

**Schritte:**
1. Maus ueber Header `VK` halten (ohne Klick).
2. Wiederholen fuer VL, VE, VT, VA.

**Erwartetes Verhalten:**
- Browser-Tooltip erscheint mit Volltext: "VK Kaelte", "VL Luefter", "VE Elektro", "VT Tueren", "VA Aufbau".

---

### TS-1.x-E — Spalten-Filter auf neuen Spalten

**Schritte:**
1. Filter-Funktion in der Liste aktivieren.
2. Spalte VK auf "checked" filtern.

**Erwartetes Verhalten:**
- Nur Auftraege mit VK=true sind sichtbar (Filter-Pattern aus Glas/Zukauf greift identisch).

---

### TS-1.x-F — Berechtigung: Nicht-Picking-User sieht disabled Checkboxes

**Vorbedingungen:**
- User OHNE picking-Rolle, aber MIT tracking-Rolle (hat Zugang zur Liste).

**Schritte:**
1. Login als Tracking-User.
2. Produktionsauftragsliste oeffnen.

**Erwartetes Verhalten:**
- VK/VL/VE/VT/VA-Checkboxes sind `disabled` (analog zu Glas/Zukauf).
- Klick darauf hat keine Wirkung. Toggle-API wuerde 302→AccessDenied liefern.

---

### TS-1.x-G — Sage-Sync ueberschreibt die Flags NICHT

**Vorbedingungen:**
- Auftrag mit VK=true, VL=true gesetzt.

**Schritte:**
1. Sage-Import-Job manuell anstossen ODER auf naechsten Sync warten.
2. Produktionsauftragsliste neu laden.

**Erwartetes Verhalten:**
- VK und VL bleiben `true`.
- Andere Felder aus Sage (Lieferdatum, Stueckzahl, ...) sind ggf. aktualisiert, aber VK/VL/VE/VT/VA-Flags wurden nicht ueberschrieben.

---

### TS-1.x-H — Column-Preferences-Offcanvas

**Schritte:**
1. "Spalten anpassen"-Button anklicken (oder Offcanvas-Trigger).

**Erwartetes Verhalten:**
- Die 5 neuen Spalten VK/VL/VE/VT/VA erscheinen in der Liste, koennen ein-/ausgeblendet werden wie Glas/Zukauf.
- User-Reihenfolge-Aenderungen werden gespeichert und nach Reload wiederhergestellt.

---

### TS-1.x-I — Fresh-Install enthaelt die Spalten

**Vorbedingungen:**
- Frische DB aus `SQL/00_FreshInstall.sql`.

**Schritte:**
1. `SELECT name FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ProductionOrders') AND name IN ('HasCooling', 'HasFan', 'HasElectric', 'HasDoors', 'HasSuperstructure');`

**Erwartetes Verhalten:**
- Query liefert alle 5 Zeilen — Spalten existieren in der frisch installierten DB.

---
```

- [ ] **Step 3: Commit**

```pwsh
git add docs/TESTSZENARIEN.md
git commit -m @'
docs: testszenarien for production order assembly flags (VK/VL/VE/VT/VA)

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
'@
```

---

## Manuelle End-to-End-Verifikation (vor Merge)

Alle Szenarien TS-1.x-A bis I im Browser durchspielen.

Zusätzlich:
- **Visueller Smoke-Test**: Tabelle laden, Spalten-Reihenfolge prüfen: ... Lack-T → Glas → Zukauf → **VK → VL → VE → VT → VA** → Status → ...
- **Spalten-Breite**: VK/VL/VE/VT/VA-Spalten sind je ~40px breit, Checkbox zentriert.
- **Migration-Apply auf Test-DB**: Migration in Test-Umgebung anwenden, Schema-Check, anschließend FreshInstall-Verifikation in separater DB.
- **Sage-Import Smoke**: Test-Import durchlaufen lassen, Flags an einem Test-Auftrag setzen, Re-Import → Flags bleiben.

---

## Self-Review-Notiz

**Spec-Coverage:**
- Section 4 (Datenmodell) → Task 1 + Task 2.
- Section 5 (API) → Task 2.
- Section 6 (View) → Task 3.
- Section 7 (AgentJob-Kommentar) → Task 1 Step 5.
- Section 9 (Test-Szenarien) → Task 5.
- Changelog + Hilfeseite → Task 4.

**Reihenfolge ist wichtig:**
1. Task 1 zuerst — Schema muss da sein, sonst kompiliert keiner der View/API-Codes.
2. Task 2 vor Task 3 — ViewModel muss die Felder haben, bevor View darauf zugreift.
3. Task 3 vor Task 4 — Code-Funktionalität fertig, dann Doku.
4. Task 5 zuletzt — alle Szenarien validieren das fertige System.

**Mögliche Stolperfallen für den Implementierungs-Agent:**

- **Repo-Mapping-Stelle finden**: Es gibt evtl. mehrere Stellen, wo `ProductionOrder` → `ProductionOrderViewModel` projiziert wird (Liste, Detail, Bulk). Alle erfassen via `Grep "HasGlass = "`.
- **EF Migration Stamp**: `dotnet ef migrations add` schreibt Dateiname mit Zeitstempel. Diesen Pfad für git add benutzen.
- **`SQL/00_FreshInstall.sql`-Format**: Datei könnte konsolidiert sein und neue Migrations als reines `INSERT INTO __EFMigrationsHistory` hinzufügen. Andererseits könnte sie die `CREATE TABLE`-Anweisung direkt anpassen. Beide Stellen prüfen.
- **AgentJob MERGE prüfen**: Stelle sicher, dass die MERGE-Anweisung keine `*`-Liste oder volle Spalten-Liste nutzt, die die neuen Flags inkludieren würde. Vermutlich keine Änderung an MERGE-Logik nötig — nur Kommentar.

**No-Placeholder-Check:** Keine TBDs. Alle Code-Snippets vollständig.

**Commit-Frequency:** 5 Commits — einer pro Task.

**Branch-Bundling:** `feature/sage-lagerbestand-sync` (gleiches Phase-2-Bundle, weiter befüllt).
