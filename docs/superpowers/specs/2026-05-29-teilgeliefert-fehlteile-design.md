# Lagerbestellungen: Teilgeliefert-Status + Fehlteile-Liste + "Drucken und Abschliessen" — Design

**Datum:** 2026-05-29
**Status:** Draft
**Branch:** `feature/teilgeliefert-fehlteile`
**Worktree:** `.claude/worktrees/teilgeliefert-fehlteile`
**Version-Ziel:** v1.18.0

---

## 1. Ziel

Das Lager bekommt drei zusammenhaengende Erweiterungen am Modul "Lagerbestellungen":

1. **Neuer Status `PartiallyDelivered`** — Bestellungen koennen teilweise geliefert werden und bleiben dann offen fuer Restlieferung.
2. **Endgueltige Fehlteile** werden pro Item explizit markiert und in einer dedizierten Fehlteile-Liste sichtbar (Lager-Auswertung + Werkbank-Sicht).
3. **"Drucken und Abschliessen"-Button** — kombiniert Speichern, Status-Ableitung und Druck in einem Workflow.

Begruendung: Bisher gibt es nur bin-State Submitted/Closed. Real-Welt-Fall "Bestellung wurde teilweise geliefert, der Rest kommt nach" ist nicht abbildbar. Fehlteile werden nicht erfasst → keine Auswertung moeglich.

## 2. Scope

**In-Scope:**

- Enum-Erweiterung `WarehouseRequisitionStatus.PartiallyDelivered = 5`
- Neue Spalte `WarehouseRequisitionItem.IsFinalShortage` (bool, default false)
- EF-Migration + SQL-Skript + `00_FreshInstall.sql`-Sync
- Status-Ableitung im Repository (`DeriveStatus`-Helper)
- `CloseAsync` erweitert: nimmt `IsFinalShortage`-Dictionary, leitet Status ab
- `SaveProgressAsync` neue Repo-Methode (persistiert Mengen+Notizen+Flags ohne Status-Wechsel; `SaveNotesAsync` bleibt als Wrapper)
- `GetMissingPartsAsync` Repo-Methode (paginiert + Spalten-Filter)
- `GetFinalShortagesCountForUserAsync` Repo-Methode (Karte in Werkbank-Sicht)
- Neuer `MissingPartsController` + Views (Pflicht-Pattern Pagination/Filter)
- Picking/Details-View: pro-Row Checkbox "Endgueltig Fehlteil", 3 Buttons, Status-Banner
- Neue Controller-Action `PrintAndClose` mit JS-Popup-Pattern (synchron im User-Gesture)
- `WarehousePicking/Index`: Default-Filter zeigt Submitted+PartiallyDelivered, neuer Status im Dropdown
- `WarehouseRequisitions/Index`: Karte "Meine Fehlteile" + neuer Status-Badge
- Print.cshtml: zusaetzliche Spalte/Hinweis fuer IsFinalShortage
- Doku (Changelog, PROJECT_STATUS, TESTSZENARIEN Kapitel 32, CLAUDE.md)
- Version-Bump v1.18.0 (Web + Service)

**Out-of-Scope:**

- Stock-Movement-Aenderungen (`CloseAsync` macht heute KEINE Bestandsbuchungen — `_stock` in WarehousePickingController dient nur der Anzeige aktueller Bestaende)
- Restlieferungs-Historie als separate Tabelle (siehe §13 Future)
- E-Mail-Versand bei Status-Wechsel zu PartiallyDelivered (kann nachgezogen werden falls gewuenscht)
- Werkbank-Edit auf teilgelieferten Bestellungen (Werkbank bekommt nur Sicht, kein Edit)

## 3. Entscheidungen aus Brainstorming

| Frage | Entscheidung |
|-------|--------------|
| Restlieferung-Modell | Pro-Item-Flag `IsFinalShortage`. Lager entscheidet je Item ob "Restlieferung erwartet" oder "endgueltig Fehlteil" |
| Fehlteil-Definition | Nur Items mit `IsFinalShortage=true` AUS Status=Closed-Bestellungen. PartiallyDelivered-Items mit Flag=true zaehlen NICHT (Bestellung noch in Bearbeitung) |
| Fehlteile-Sicht | Eigenes Lager-Menue `MissingParts/Index` + Werkbank-Karte in `WarehouseRequisitions/Index` (filtert auf eigene Werkbaenke) |
| Buttons | Drei: "Speichern + Abschliessen", "Drucken", "Drucken und Abschliessen" |
| Worktree | `.claude/worktrees/teilgeliefert-fehlteile`, Branch `feature/teilgeliefert-fehlteile` |

## 4. Datenmodell

### 4.1 WarehouseRequisitionStatus

```csharp
public enum WarehouseRequisitionStatus : byte
{
    Draft              = 1,
    Submitted          = 2,
    Closed             = 3,
    Cancelled          = 4,
    PartiallyDelivered = 5   // NEU
}
```

`PartiallyDelivered` ist KEIN End-Status. Bestellungen in diesem Zustand bleiben fuer das Lager bearbeitbar (im Picking/Index sichtbar). Sobald alle Items entweder vollstaendig geliefert oder als IsFinalShortage markiert sind, wechselt der Status auf Closed.

### 4.2 WarehouseRequisitionItem

Neue Property:

```csharp
public bool IsFinalShortage { get; set; }   // default false
```

Semantik:
| QuantityPicked | IsFinalShortage | Interpretation |
|----------------|----------------|----------------|
| >= QuantityRequested | * | vollstaendig geliefert (Flag irrelevant) |
| < QuantityRequested | false | Restlieferung erwartet (treibt Status auf PartiallyDelivered) |
| < QuantityRequested | true | endgueltiger Fehlteil (zaehlt in Fehlteile-Liste sobald Bestellung Closed) |
| NULL | * | wie 0 behandelt — wenn IsFinalShortage=true → Vollfehlteil; sonst Restlieferung erwartet |

## 5. Status-Ableitung

Zentraler Helper im Repository:

```csharp
private static WarehouseRequisitionStatus DeriveStatus(WarehouseRequisition req)
{
    bool isFullyDelivered = req.Items.All(i =>
        (i.QuantityPicked ?? 0) >= i.QuantityRequested);
    bool hasOpenShortage = req.Items.Any(i =>
        (i.QuantityPicked ?? 0) < i.QuantityRequested && !i.IsFinalShortage);

    return (isFullyDelivered || !hasOpenShortage)
        ? WarehouseRequisitionStatus.Closed
        : WarehouseRequisitionStatus.PartiallyDelivered;
}
```

Aufrufer: ausschliesslich `CloseAsync`. Wird NIE auf `Submitted` zurueckgesetzt (Storno geht via `CancelAsync`).

## 6. Repository-API

```csharp
// Geaendert:
Task CloseAsync(int id,
    IReadOnlyDictionary<int, decimal> itemQuantitiesPicked,
    IReadOnlyDictionary<int, string?> itemNotes,
    IReadOnlyDictionary<int, bool> itemIsFinalShortages,   // NEU
    int closedByUserId, string user, string winUser, byte[] rowVersion);

// Neu:
Task SaveProgressAsync(int id,
    IReadOnlyDictionary<int, decimal?> itemQuantitiesPicked,
    IReadOnlyDictionary<int, string?> itemNotes,
    IReadOnlyDictionary<int, bool> itemIsFinalShortages,
    string user, string winUser);

Task<(IReadOnlyList<MissingPartRow> Items, int TotalCount)>
    GetMissingPartsAsync(int? workplaceFilter,
                        IReadOnlyDictionary<string, string>? columnFilters,
                        DateTime? closedFrom, DateTime? closedUntil,
                        int page, int pageSize);

Task<(int ItemCount, int RequisitionCount)>
    GetFinalShortagesCountForUserAsync(int userId);

// Bestehend, bleibt:
Task SaveNotesAsync(...);   // wird intern auf SaveProgressAsync mit empty qty/flag delegiert
                            // — Backwards-Compat fuer Autosave-on-Blur
```

Neuer Record:
```csharp
public record MissingPartRow(
    int RequisitionId, int ItemId, int Position,
    string WorkplaceName, string ArticleNumber, string ArticleDescription,
    decimal QuantityRequested, decimal QuantityPicked, decimal QuantityMissing,
    string? Unit, string? Note,
    string CreatedBy, DateTime? ClosedAt);
```

## 7. Controller-Aenderungen

### 7.1 WarehousePickingController

| Action | Aenderung |
|--------|-----------|
| `Index` | `effectiveFilter` Default: `Submitted + PartiallyDelivered` zusammen. `GetForWarehouseAsync` Signatur wird zu `WarehouseRequisitionStatus[] statuses`. OpenCount zaehlt beide Status |
| `Details` | NotFound NUR fuer Draft. Closed/Cancelled bleiben Lese-Modus, Submitted/PartiallyDelivered editierbar |
| `Close` | Akzeptiert zusaetzlich `bool[] isFinalShortages` Form-Array. Bildet flagDict wie qtyDict |
| `SaveProgress` (NEU) | POST, ValidateAntiForgeryToken. Wie SaveNotes, aber persistiert auch Mengen + Flags. Returnt `Ok()` |
| `SaveNotes` (bestehend) | Bleibt unveraendert (delegiert intern auf SaveProgressAsync mit nur Notes-Dict). Backwards-Compat fuer Print-on-Blur |
| `PrintAndClose` (NEU) | POST → `CloseAsync` → returnt JSON `{redirectUrl: "/WarehousePicking/Print/{id}"}`. Frontend hat den Print-Tab bereits synchron im Click-Handler mit `about:blank` geoeffnet (siehe §10 JS-Pattern). Nach `await fetch(PrintAndClose)` wird `printTab.location.href = redirectUrl` gesetzt |
| `Print` | unveraendert |
| `Cancel` | unveraendert |

### 7.2 MissingPartsController (NEU)

```csharp
[RequireStockAccess]
public class MissingPartsController : Controller
{
    // Index(int? workplaceId, int page = 1, int? pageSize = null, ...)
}
```

Pflicht-Pattern: PageSize.Resolve + PaginationState, `data-server-column-filter="true"` Table, `ColumnFilterHelper.ReadFromQuery`. Datum-Spalte "Abgeschlossen am" in C# nach Format `dd.MM.yyyy KWxx` (lowercase) gefiltert.

### 7.3 WarehouseRequisitionsController.Index

Werkbank-Sicht: zusaetzlicher Aufruf `GetFinalShortagesCountForUserAsync(userId)` — Ergebnis fliesst in ViewModel als `(MissingPartsItemCount, MissingPartsRequisitionCount)`. View zeigt Karte wenn Count > 0.

## 8. Views

### 8.1 WarehousePicking/Details

**Neue Spalte rechts** in Item-Tabelle: Checkbox "Endgueltig Fehlteil". Disabled solange `Ist >= Soll` (Inline-JS toggelt). Tooltip erklaert Semantik.

**Status-Badge** im Header zeigt aktuellen Status. Hinweis-Banner falls PartiallyDelivered:

```
<div class="alert alert-warning">
  ⚠ Diese Bestellung ist teilgeliefert. Items mit Restlieferung erwartet bleiben offen.
</div>
```

**3 Action-Buttons** unten:

```
[ Speichern + Abschließen ]  [ Drucken ]  [ Drucken und Abschließen ]
   class="btn btn-primary"   class="btn   class="btn btn-primary"
                              btn-outline-
                              secondary"
```

Bei Status=Closed werden Inputs disabled, Buttons reduzieren sich auf "Drucken".

### 8.2 WarehousePicking/Print

Bestehender Print zeigt schon Ist + Notiz. **Neu hinzu**: Spalte "Fehlteil" (zeigt "✓" wenn `IsFinalShortage=true`). Optional Header-Hinweis "Status: Teilgeliefert" oder "Status: Abgeschlossen".

### 8.3 MissingParts/Index (NEU)

| Spalte (data-col-key) | Inhalt |
|----|----|
| Bestell-ID | #1234 (Link zu Picking/Details Lese-Modus) |
| Werkbank | r.ProductionWorkplace.Name |
| Artikel-Nr | item.ArticleNumber |
| Bezeichnung | item.ArticleDescription |
| Soll | QuantityRequested |
| Geliefert | QuantityPicked |
| Fehlt | (Soll - Geliefert) |
| Notiz | item.Note |
| Erfasst von | r.CreatedBy |
| Abgeschlossen am | r.ClosedAt formatted |

`<table data-server-column-filter="true" data-view-key="MissingParts">`.
Globaler Werkbank-Filter via `?workplaceId=` Query.
Pagination 25/50/100/Alle (Cap 5000).

### 8.4 WarehouseRequisitions/Index Karte "Meine Fehlteile"

Oberhalb der Bestellungs-Tabelle, falls Count > 0:

```html
<div class="card border-warning mb-3">
  <div class="card-body">
    <h6 class="card-title">⚠ Meine Fehlteile</h6>
    <p>@Model.MissingPartsItemCount endgueltige Fehlteile aus
       @Model.MissingPartsRequisitionCount abgeschlossenen Bestellungen.</p>
    <a href="/MissingParts?workplaceId=@firstUserWorkplaceId" class="btn btn-outline-warning">
      Details ansehen →
    </a>
  </div>
</div>
```

**Filter-Param `mineOnly`** (Pflicht-Param fuer Werkbank-Sicht):

- `MissingPartsController.Index` akzeptiert `bool mineOnly = false` (Default false → globale Lager-Sicht, alle Werkbaenke)
- Card-Link setzt explizit `?mineOnly=true` → Backend filtert auf alle Werkbaenke des aktuellen Users (`_workplaces.GetByUserIdAsync(userId)` IDs als IN-Filter)
- Auch der `workplaceId`-Filter bleibt unabhaengig: Lager kann global einen einzelnen Workplace waehlen via `?workplaceId=X`
- Beide Filter sind kombinierbar (`?mineOnly=true&workplaceId=X` filtert auf X falls in User-Workplaces, sonst leeres Resultat)

### 8.5 Status-Badge fuer WarehouseRequisitionStatus

Neuer Badge-Stil (in `_StatusBadge.cshtml` oder Razor-Helper):
- Draft → grau "Entwurf"
- Submitted → blau "Eingereicht"
- **PartiallyDelivered → orange "Teilgeliefert"** (NEU)
- Closed → gruen "Abgeschlossen"
- Cancelled → rot "Storniert"

Wird im Werkbank-Index, Picking-Index und Picking/Details verwendet.

## 9. Migration

EF Core Migration: `AddIsFinalShortageToWarehouseRequisitionItems`

- Spalte `IsFinalShortage BIT NOT NULL DEFAULT 0`
- Filtered Index fuer Performance:
  ```sql
  CREATE INDEX IX_WarehouseRequisitionItems_IsFinalShortage
      ON [dbo].[WarehouseRequisitionItems]([IsFinalShortage])
      WHERE [IsFinalShortage] = 1;
  ```
- Idempotente SQL-Skripte `SQL/<NN>_AddIsFinalShortageToWarehouseRequisitionItems.sql` mit OBJECT_ID-/`sys.columns`-Guards
- `SQL/00_FreshInstall.sql` konsolidiert: Spalte in der `WarehouseRequisitionItems`-Tabellendefinition + MigrationId im `__EFMigrationsHistory`-INSERT-Block

## 10. Workflow "Drucken und Abschliessen" — JS-Pattern

Popup-Blocker erlaubt `window.open` nur im synchronen User-Gesture. Pattern wie bei Lagerbestellung-Notiz-Autosave ([siehe CLAUDE.md Fallstrick "Lagermitarbeiter-Notiz-Autosave"](../../../../CLAUDE.md)):

```javascript
async function printAndClose() {
    // 1. Tab SOFORT synchron oeffnen — sonst blockt iOS/Chrome
    const printTab = window.open('about:blank', '_blank');

    // 2. Formular sammeln, POST via fetch
    const formData = new FormData(document.getElementById('detailsForm'));
    const response = await fetch('/WarehousePicking/PrintAndClose/' + reqId, {
        method: 'POST',
        body: formData
    });

    if (!response.ok) {
        printTab.close();
        alert('Fehler beim Abschliessen.');
        return;
    }

    const data = await response.json();   // {redirectUrl: "..."}
    printTab.location.href = data.redirectUrl;

    // 3. Hauptfenster auf Index
    window.location.href = '/WarehousePicking';
}
```

## 11. Error-Handling

| Szenario | Verhalten |
|----------|-----------|
| `QuantityPicked < 0` | TempData WarningMessage + redirect (bestehend) |
| `QuantityPicked > QuantityRequested` | Erlaubt (Lager hat zu viel geliefert) |
| `IsFinalShortage=true` mit `Ist >= Soll` | Backend ignoriert (DeriveStatus pruefend), Frontend disabled Checkbox |
| `QuantityPicked NULL` + `IsFinalShortage=true` | Vollfehlteil (wie 0) |
| `QuantityPicked NULL` + `IsFinalShortage=false` | Restlieferung erwartet (treibt Status PartiallyDelivered) |
| `DbUpdateConcurrencyException` | TempData WarningMessage + Redirect, bestehende Behandlung |
| PrintAndClose POST-Fail | JS schliesst Print-Tab, zeigt Alert |
| Werkbank versucht eigene PartiallyDelivered-Bestellung zu editieren | Existierender `WarehouseRequisitionsController.Edit` greift `Draft`-Status-Guard schon, nichts zu tun |

## 12. Testing

### 12.1 Unit-Tests Repository

`IdealAkeWms.Tests.Repositories.WarehouseRequisitionRepositoryTests` erweitern:

1. `CloseAsync_AllItemsFullyDelivered_SetsStatusClosed`
2. `CloseAsync_AllShortagesMarkedFinal_SetsStatusClosed`
3. `CloseAsync_OneShortageNotFinal_SetsStatusPartiallyDelivered`
4. `CloseAsync_QuantityPickedNull_TreatedAsZero_StatusPartiallyDelivered_WhenNotFinal`
5. `CloseAsync_QuantityPickedNull_AndFinalShortageTrue_StatusClosed`
6. `CloseAsync_ReClose_AfterRestlieferungComplete_TransitionsToClosed`
7. `CloseAsync_IsFinalShortageTrueButFullyDelivered_FlagIgnored`
8. `SaveProgressAsync_PersistsQuantitiesNotesAndFlags_WithoutStatusChange`
9. `SaveProgressAsync_DoesNotPromoteSubmittedToPartiallyDelivered`
10. `GetMissingPartsAsync_ReturnsOnlyClosedRequisitions_WithFinalShortages_NotPartiallyDelivered`
11. `GetMissingPartsAsync_AppliesWorkplaceFilter`
12. `GetMissingPartsAsync_AppliesColumnFilter_OnArticleNumber_WithOrSyntax`
13. `GetMissingPartsAsync_DateFilter_OnClosedAt_InCSharpAfterFormat`
14. `GetMissingPartsAsync_PaginationCappedAt5000`
15. `GetFinalShortagesCountForUserAsync_CountsOnlyForUserWorkplaces`
16. `GetFinalShortagesCountForUserAsync_ZeroWhenUserHasNoFinalShortages`

### 12.2 Controller-Tests

`WarehousePickingControllerTests` erweitern:
- `Close_AcceptsIsFinalShortageArray_PassesToRepo`
- `Close_QuantitiesNegative_ReturnsWarning_NoChange`
- `Index_Default_ShowsSubmittedAndPartiallyDelivered`
- `SaveProgress_PersistsAllFields_ReturnsOk`
- `PrintAndClose_OnSuccess_ReturnsJsonWithRedirectUrl`
- `PrintAndClose_OnConcurrencyConflict_Returns409`

`MissingPartsControllerTests` (NEU):
- `Index_RequiresStockAccess`
- `Index_NoMineOnly_ShowsAll`
- `Index_MineOnly_FiltersByUserWorkplaces`
- `Index_ColumnFilter_OnArticleNumber_Works`

`WarehouseRequisitionsControllerTests` ergaenzen:
- `Index_ShowsMissingPartsCard_WhenUserHasFinalShortages`
- `Index_HidesMissingPartsCard_WhenNoShortages`

**Erwartete Test-Counts:** Web ~602 (+12 neu), Service unveraendert.

### 12.3 Manuelle TESTSZENARIEN Kapitel 32

1. **Vollstaendige Lieferung → Closed**
2. **Alle Shortages markiert als endgueltig → Closed**
3. **Eine Short-Position ohne Final-Flag → PartiallyDelivered**
4. **Vollfehlteil (Ist=0, IsFinalShortage=true) → Closed, Item in Fehlteile-Liste**
5. **Restlieferung-Workflow: PartiallyDelivered → Ergaenzung im Picking/Details → Closed**
6. **"Drucken und Abschliessen"-Button: Print-Tab oeffnet sich, enthaelt aktuelle Ist + Notiz**
7. **Werkbank-Karte "Meine Fehlteile" sichtbar wenn count > 0**
8. **Fehlteile-Liste-Spaltenfilter (Artikel-Nr mit Komma-OR, Datum-Format)**
9. **Negativ-Test: QuantityPicked = -1 → WarningMessage, kein Status-Wechsel**
10. **Re-Open einer PartiallyDelivered-Bestellung — RowVersion-Concurrency-Konflikt**

## 13. Doku

- `AppVersion.cs` (Web + Service) → `"1.18.0"`, Date `2026-05-29`
- `Views/Help/Changelog.cshtml` → v1.18.0-Card prependen
- `PROJECT_STATUS.md` → neue Sub-Task-Tabelle v1.18.0
- `docs/TESTSZENARIEN.md` → neues Kapitel 32 (10 Szenarien)
- `CLAUDE.md` → neue Fallstricke:
  - "PartiallyDelivered ist KEIN End-Status — Bestellung bleibt im WarehousePicking/Index editierbar bis explizit Closed"
  - "MissingParts/Index zeigt nur IsFinalShortage=true AUS Status=Closed. PartiallyDelivered-Items mit IsFinalShortage=true sind noch in-flight und zaehlen nicht als finalen Fehlteil"
  - "WarehouseRequisitionsController.Index laedt fuer die Werkbank-Karte zusaetzlich GetFinalShortagesCountForUserAsync — kann bei vielen Bestellungen zur Performance-Falle werden, deshalb scalar-Count (keine vollen Items-Includes)"

## 14. Open Points / Risiken

- **Werkbank ohne Workplace-Zuordnung**: `_workplaces.GetByUserIdAsync(userId)` liefert leere Liste → Karte zeigt 0 → unsichtbar. Default-Verhalten ok.
- **Bestellungen ohne IsFinalShortage-Spalte vor Migration**: nicht moeglich, da NOT NULL DEFAULT 0 — alle Altdaten bekommen automatisch false. Auch Closed-Bestellungen mit `QuantityPicked < QuantityRequested` (bisheriger Workflow) bekommen `IsFinalShortage=false` → tauchen in Fehlteile-Liste NICHT auf (false-Flag). Diese Bestellungen muessten manuell editiert werden falls historische Fehlteile sichtbar sein sollen — Out-of-Scope.
- **PrintAndClose-Tests mit echtem JS** nicht moeglich (kein E2E im Repo). Controller-Test deckt Backend-Verhalten ab, Print-Tab-Pattern wird manuell getestet (TESTSZENARIEN 32.6).
- **`SaveNotesAsync` als Wrapper** — bestehende Autosave-on-Blur in `Details.cshtml` aufrufende JS funktioniert weiter ohne Frontend-Change. Spec verlangt aber, dass die Autosave-JS auch IsFinalShortage und Mengen mitsendet → also Frontend muss aktualisiert werden um `SaveProgressAsync`-Endpoint zu nutzen.

## 15. Reihenfolge der Tasks (grob — Detail-Tasks in writing-plans)

1. Pre-Flight Baseline (Build + Tests)
2. `WarehouseRequisitionItem.IsFinalShortage` Property + EF-Migration + SQL-Skript + FreshInstall-Sync
3. `WarehouseRequisitionStatus.PartiallyDelivered = 5` Enum-Erweiterung
4. `DeriveStatus`-Helper + `CloseAsync`-Refactor (TDD)
5. `SaveProgressAsync` Repo-Methode + `SaveNotesAsync` als Wrapper (TDD)
6. `GetMissingPartsAsync` + `GetFinalShortagesCountForUserAsync` Repo (TDD)
7. WarehousePicking/Details-View: IsFinalShortage-Checkbox + Status-Banner + 3 Buttons
8. `PrintAndClose` Controller-Action + JS-Popup-Pattern
9. `MissingPartsController` + `MissingParts/Index`-View (Pflicht-Pattern)
10. `WarehouseRequisitions/Index`: Karte "Meine Fehlteile"
11. `WarehousePicking/Index`: Status-Default + Filter-Dropdown
12. `Print.cshtml`: Fehlteil-Spalte
13. Frontend: Autosave-JS auf SaveProgressAsync umstellen
14. Version v1.18.0 + Changelog + TESTSZENARIEN Kap 32 + PROJECT_STATUS + CLAUDE.md
15. Final-Check + Merge in main + Worktree-Cleanup

## 16. Future (Out-of-Scope, dokumentiert)

**Option C aus Brainstorming**: separate Audit-Tabelle `WarehouseRequisitionItemDelivery` mit einer Zeile pro Teil-Lieferung (QuantityDelivered, DeliveredAt, DeliveredBy, Note). Vollstaendige Liefer-Historie statt einer einzigen fortgeschriebenen `QuantityPicked`-Zahl. Sinnvoll falls spaeter:
- Auswertung "wer hat wann wieviel geliefert"
- Restlieferungs-Statistik pro Lieferant/Artikel
- Konfliktloesung bei Mehrfach-Bearbeitung der gleichen Bestellung

Nicht jetzt eingebaut, da aktueller Use-Case nur das Ergebnis braucht, nicht die History.
