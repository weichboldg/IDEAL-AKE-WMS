# Produktionsauftragsliste: 5 neue Baugruppen-Flags (VK/VL/VE/VT/VA) — Design Spec

**Datum:** 2026-05-11
**Branch:** `feature/sage-lagerbestand-sync` (Phase-2-Bundle, weiter gefüllt)
**Status:** Approved → Plan
**Phase:** Funktionserweiterung Produktionsauftragsliste. **Kein AppVersion-Bump** — wird in der bestehenden v1.10.0-Changelog-Card mit ergänzt.

---

## 1. Problemstellung

Die Produktionsauftragsliste hat bereits zwei manuell setzbare Status-Checkboxen pro Auftrag: **Glas** (`HasGlass`) und **Zukauf** (`HasExternalPurchase`). Damit kennzeichnen Kommissionierer Aufträge, die zusätzliche Baugruppen-Vorbereitung benötigen.

Der Workflow braucht fünf weitere identische Flags für die Hauptbaugruppen-Kategorien, die intern als VK/VL/VE/VT/VA bekannt sind:

| Abk. | Volltext  | Code-Property        |
|------|-----------|----------------------|
| VK   | Kälte     | `HasCooling`         |
| VL   | Lüfter    | `HasFan`             |
| VE   | Elektro   | `HasElectric`        |
| VT   | Türen     | `HasDoors`           |
| VA   | Aufbau    | `HasSuperstructure`  |

## 2. Ziele

1. **Funktionsgleichheit zu Glas/Zukauf:** dieselbe DB-Spalte (BIT NOT NULL DEFAULT 0), dasselbe Toggle-API, dasselbe Rechtekonzept, dieselbe Filter-Spalte.
2. **Platzsparende UI:** Kurz-Header `VK` / `VL` / `VE` / `VT` / `VA` mit Tooltip-Volltext. Tabelle hat danach 7 Checkbox-Spalten in Folge — Header-Abkürzungen sind essentiell.
3. **Kein Sage-Import-Konflikt:** Werte bleiben App-verwaltet (Picking setzt manuell), Sage-Sync überschreibt sie nicht.

## 3. Out-of-Scope

- **Filter-Logik-Erweiterung über Pattern hinaus** — Spalten bekommen `data-filterable`-Marker wie Glas/Zukauf, der bestehende `table-filter.js` übernimmt den Rest. Kein Custom-Filter.
- **Sage-Sync der neuen Felder** — explizit ausgeschlossen (siehe AgentJob-Kommentar-Update in Sektion 7).
- **Berechtigung anpassen** — Toggle bleibt auf `[RequirePickingAccess]` wie das bestehende API.
- **Bulk-Aktion zum gleichzeitigen Setzen mehrerer Aufträge** — nicht angefragt, nicht implementiert.
- **Reihenfolge der Checkbox-Spalten benutzerveränderbar machen** — `UserViewPreferences` erlaubt das ohnehin schon clientseitig. Default-Reihenfolge: Glas → Zukauf → VK → VL → VE → VT → VA → Status (an bestehendes Layout angehängt).

## 4. Datenmodell

### 4.1 Entity-Erweiterung

`IdealAkeWms/Models/ProductionOrder.cs` bekommt 5 neue Properties nach dem bestehenden `HasExternalPurchase`-Block (vor `HasCoatingParts`):

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

**Display(Name)** enthält bewusst Abkürzung + Volltext (`"VK Kälte"`), damit Razor-`@Html.DisplayNameFor` und Audit-Logs den vollen Kontext zeigen.

### 4.2 ViewModel-Erweiterung

`IdealAkeWms/Models/ViewModels/ProductionOrderViewModel.cs` spiegelt die 5 Properties:

```csharp
public bool HasCooling { get; set; }
public bool HasFan { get; set; }
public bool HasElectric { get; set; }
public bool HasDoors { get; set; }
public bool HasSuperstructure { get; set; }
```

Wo immer das Repository ein `ProductionOrder` auf `ProductionOrderViewModel` projiziert, müssen die 5 Felder mit projiziert werden (Repo-Mapping suchen + erweitern).

### 4.3 DB-Schema

`BIT NOT NULL DEFAULT 0` für alle 5 Spalten — identisch zu `HasGlass` / `HasExternalPurchase`.

- **EF Migration:** Name `AddProductionOrderAssemblyFlags`.
- **SQL Script:** `SQL/59_AddProductionOrderAssemblyFlags.sql` mit `OBJECT_ID`-Guard pro Spalte (idempotent, 5 separate Batches mit `GO`).
- **`SQL/00_FreshInstall.sql`:** aktualisiert um die 5 neuen Spalten in der Tabellen-Definition + `__EFMigrationsHistory`-Eintrag.

## 5. API

### 5.1 Toggle-Endpoint erweitern

`IdealAkeWms/Controllers/ProductionOrdersApiController.cs`:

- `AllowedToggleFields` HashSet erweitern um die 5 neuen Property-Namen.
- Die if/else-Chain in `ToggleField` um 5 weitere Branches erweitern.

Bevorzugte Refactor-Option, weil if/else-Chain sonst auf 8 Branches wächst: **Reflection oder Dictionary-Lookup**. Aber: für Sicherheit (`AllowedToggleFields`-Whitelist) und Lesbarkeit ist die direkte if/else-Chain OK. **Entscheidung: bei if/else bleiben** — 8 Branches sind noch lesbar, Refactor wäre Scope-Creep.

```csharp
private static readonly HashSet<string> AllowedToggleFields = [
    "HasGlass", "HasExternalPurchase", "IsCoatingDone",
    "HasCooling", "HasFan", "HasElectric", "HasDoors", "HasSuperstructure"
];

// In ToggleField:
else if (request.Field == "HasCooling")        order.HasCooling = request.Value;
else if (request.Field == "HasFan")            order.HasFan = request.Value;
else if (request.Field == "HasElectric")       order.HasElectric = request.Value;
else if (request.Field == "HasDoors")          order.HasDoors = request.Value;
else if (request.Field == "HasSuperstructure") order.HasSuperstructure = request.Value;
```

Keine Permission-Änderung — `[RequirePickingAccess]` gilt für den ganzen Controller.

## 6. View

### 6.1 ColumnDefinitions erweitern

`IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs` — 5 neue `ColumnDef`-Einträge **nach `purchase` und vor `status`**:

```csharp
new ColumnDef("cooling",        "VK", Locked: false, DefaultWidth: 40),
new ColumnDef("fan",            "VL", Locked: false, DefaultWidth: 40),
new ColumnDef("electric",       "VE", Locked: false, DefaultWidth: 40),
new ColumnDef("doors",          "VT", Locked: false, DefaultWidth: 40),
new ColumnDef("superstructure", "VA", Locked: false, DefaultWidth: 40),
```

Default-Width 40px reicht für 2-Zeichen-Header + Checkbox.

### 6.2 Index.cshtml — Thead

Nach dem `purchase`-`<th>` (Zeile 117), vor dem `status`-`<th>` (Zeile 118):

```cshtml
<th style="width: 40px;" data-filterable data-col-key="cooling"        title="VK Kälte">VK</th>
<th style="width: 40px;" data-filterable data-col-key="fan"            title="VL Lüfter">VL</th>
<th style="width: 40px;" data-filterable data-col-key="electric"       title="VE Elektro">VE</th>
<th style="width: 40px;" data-filterable data-col-key="doors"          title="VT Türen">VT</th>
<th style="width: 40px;" data-filterable data-col-key="superstructure" title="VA Aufbau">VA</th>
```

Standard-Pattern: `data-filterable` macht die Spalte filterbar, `title` zeigt Tooltip-Volltext bei Hover.

### 6.3 Index.cshtml — Tbody-Row

Nach dem `HasExternalPurchase`-`<td>` (Zeile 228-231), vor dem Status-`<td>` (Zeile 232+):

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

### 6.4 Index.cshtml — Column-Preferences-JSON

Im JSON-Array (ca. Zeile 482-492) nach `purchase`-Eintrag, vor `status`-Eintrag:

```javascript
{ "key": "cooling",        "label": "VK", "locked": false, "defaultWidth": 40 },
{ "key": "fan",            "label": "VL", "locked": false, "defaultWidth": 40 },
{ "key": "electric",       "label": "VE", "locked": false, "defaultWidth": 40 },
{ "key": "doors",          "label": "VT", "locked": false, "defaultWidth": 40 },
{ "key": "superstructure", "label": "VA", "locked": false, "defaultWidth": 40 },
```

**Wichtig:** Das `label` im JSON ist der angezeigte Name in der Column-Preferences-Offcanvas (z. B. "Spalten anpassen"-Dialog). Hier reicht die Abkürzung, da der User im Tabellen-Kontext mit den Spalten arbeitet und Abkürzungen erkennt. **Alternative überlegt:** Volltext-Label "VK Kälte" — entschieden gegen, weil das Offcanvas dann mehrzeilig wird. Konsistenz mit Header gewinnt.

### 6.5 Toggle-JS bleibt unverändert

Der bestehende `.toggle-field`-Listener (Zeilen 517+) reagiert auf jeden Checkbox mit `data-field`-Attribut. **Keine Änderung nötig.** Neue Checkboxes hängen sich automatisch ein.

## 7. Sage-AgentJob Kommentar-Update

`SQL/AgentJobs/01_Import_Produktionsauftraege.sql` — Header-Kommentar erweitern (Zeile 18, 23):

```sql
-- Felder die NICHT ueberschrieben werden (App-verwaltet):
--   IsDone, PickingStatus, HasGlass, HasExternalPurchase,
--   HasCooling, HasFan, HasElectric, HasDoors, HasSuperstructure
--   CreatedAt, CreatedBy, CreatedByWindows

-- Felder der Zieltabelle die bei INSERT nicht befuellt werden (haben Defaults):
--   PickingStatus    → NULL
--   HasGlass         → 0 (DEFAULT)
--   HasExternalPurchase → 0 (DEFAULT)
--   HasCooling/HasFan/HasElectric/HasDoors/HasSuperstructure → 0 (DEFAULT)
```

Die eigentliche MERGE-Logik ändert sich nicht — sie listet die neuen Spalten nicht und überschreibt sie damit nicht. **Verifizieren** beim Implementieren, dass MERGE keine `*`-Liste nutzt.

## 8. Tests

Bestehende Test-Suite hat keine Tests für `ToggleField` (gegrept, keine Treffer). Pragmatisch: **keine neuen Unit-Tests** für dieses Feature — wir folgen dem Pattern. Optional könnte ein Smoke-Test für die Whitelist-Validierung geschrieben werden, aber nicht in v1.

**InMemory-DB-Compatibility:** EF InMemory unterstützt BIT-Defaults nicht 1:1 — neue Felder müssen in `TestApplicationDbContext` als `false` default funktionieren. Da `bool`-Property in C# automatisch `false` initialisiert, kein Problem.

## 9. Manuelle Test-Szenarien (für `docs/TESTSZENARIEN.md`)

Eingeordnet als TS-1.x (Produktionsauftragsliste). Exakte Nummerierung im Plan-Schritt an bestehende Konvention anpassen.

### TS-1.x-A — Neuer Auftrag hat alle 5 Flags auf false
**Vorbedingung:** Frischer Sync von Sage importiert einen neuen Auftrag.
**Schritte:** Produktionsauftragsliste öffnen.
**Erwartet:** Spalten VK/VL/VE/VT/VA zeigen unchecked Checkboxes.

### TS-1.x-B — Toggle setzt Wert in DB
**Schritte:** Auftrag → VK-Checkbox aktivieren → Page-Reload.
**Erwartet:** VK-Checkbox bleibt nach Reload aktiviert. (Toggle persistiert.)

### TS-1.x-C — Toggle für alle 5 Flags unabhängig
**Schritte:** Auftrag → nur VE aktivieren → Reload.
**Erwartet:** Nur VE ist aktiv, andere bleiben false.

### TS-1.x-D — Tooltip-Volltext bei Hover
**Schritte:** Maus über Header `VK` halten.
**Erwartet:** Browser-Tooltip zeigt "VK Kälte". Analog für VL/VE/VT/VA.

### TS-1.x-E — Filter-Funktion auf neuen Spalten
**Schritte:** Spalten-Filter aktivieren auf VK → nur Aufträge mit VK=true sichtbar.
**Erwartet:** Filter-Pattern aus Glas/Zukauf greift identisch — leere/befüllte Filterung wie gewohnt.

### TS-1.x-F — Berechtigungsabhängigkeit
**Vorbedingung:** User OHNE `picking`-Rolle (z. B. nur `tracking`).
**Schritte:** Produktionsauftragsliste öffnen, falls Zugriff erlaubt → Checkbox VK ansehen.
**Erwartet:** Checkbox ist `disabled` (analog zu `HasGlass`-Verhalten). Toggle-API würde 302→AccessDenied liefern, da `[RequirePickingAccess]`.

### TS-1.x-G — Sage-Sync überschreibt nicht
**Vorbedingung:** Auftrag mit VK=true, VL=true gesetzt.
**Schritte:** Sage-Import-Job manuell triggern (oder warten auf nächsten Sync).
**Erwartet:** VK/VL bleiben nach Sync `true`. Andere Felder (DeliveryDate, Quantity, ...) werden ggf. aus Sage aktualisiert, aber VK/VL/VE/VT/VA NICHT.

### TS-1.x-H — Column-Preferences-Offcanvas
**Schritte:** "Spalten anpassen" öffnen.
**Erwartet:** 5 neue Spalten VK/VL/VE/VT/VA sind in der Liste, jeweils sichtbar/ausblendbar wie Glas/Zukauf.

### TS-1.x-I — Fresh-Install
**Schritte:** Neue DB aus `SQL/00_FreshInstall.sql` aufsetzen.
**Erwartet:** ProductionOrders-Tabelle hat alle 5 neuen Spalten direkt nach dem Setup.

## 10. Risiken

### 10.1 Mapping-Lücke im Repository
Wenn das Repository Order → ViewModel manuell mapped (nicht via AutoMapper), müssen die 5 Felder explizit dort ergänzt werden. **Mitigation:** Beim Plan-Schritt das Repo durchsuchen und Mapping-Stelle finden.

### 10.2 Spalten-Reihenfolge konsistent halten
`ColumnDefinitions.cs`, `<thead>`, `<tbody>` und Column-Preferences-JSON müssen alle dieselbe Reihenfolge der neuen Spalten haben. **Mitigation:** Plan-Schritt 4 verlangt synchrone Änderung in 4 Stellen, mit Selbst-Check.

### 10.3 EF Migration und SQL-Script-Sync
EF Migration generiert C#-Migration; SQL-Skript ist separat. **Mitigation:** Plan-Schritt verlangt beide UND `00_FreshInstall.sql`-Update. Standard-Pattern für dieses Repo.

### 10.4 Sage-Sync schreibt versehentlich
MERGE-Anweisung im AgentJob könnte versehentlich `*` benutzen oder die neuen Spalten in eine UPDATE-SET-Klausel inkludieren. **Mitigation:** Plan-Schritt verlangt Verifikation der MERGE-Logik (`grep MERGE`).

### 10.5 Tabellen-Breite
Mit 5 neuen Spalten wird die Tabelle breiter. `table-responsive` mit Horizontal-Scroll greift, aber Mobile-Use wird schwieriger. **Akzeptiert** — Produktionsauftragsliste ist primär Desktop-Feature.

## 11. Ablauf

1. Plan-Doc schreiben: `docs/superpowers/plans/2026-05-11-production-order-assembly-flags.md`.
2. Plan mit User abstimmen.
3. Implementation in 5 Tasks (Schema, ViewModel+API, View, Doku, TESTSZENARIEN).
4. Manuelle Verifikation gemäß TS-1.x-A bis I.
5. Bestehenden v1.10.0-Changelog-Eintrag erweitern.
