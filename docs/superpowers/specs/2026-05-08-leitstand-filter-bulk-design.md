# Leitstand: Filter-Persistenz + Bulk-Freigabe — Design Spec

**Datum:** 2026-05-08
**Branch:** `feature/sage-lagerbestand-sync` (gleiches Phase-2-Bundle)
**Status:** Approved → Plan
**Phase:** Internal Phase-2 Improvement, kein AppVersion-Bump.

---

## 1. Problemstellung

Im Leitstand (`Views/ProductionOrders/Index.cshtml`) gehen beim Klick auf "Freigeben" alle Spalten-Filter verloren — die Seite lädt neu und der DOM-State der `table-filter`-Filter-Zeile wird nicht persistiert. Server-side Form-Filter (FA-Nr, Artikelnummer, Kunde) bleiben dagegen erhalten, weil `returnUrl` schon QueryString mitnimmt.

Zusätzlich fehlt eine UI-Möglichkeit, mehrere Aufträge gleichzeitig freizugeben — `BulkRelease` existiert serverseitig, aber kein Frontend ruft es auf.

## 2. Ziele

1. **Filter-Persistenz**: Spalten-Filter überleben Page-Reload. Generisch für alle Tabellen mit `data-view-key`.
2. **Bulk-Freigabe**: Checkboxen pro Zeile + Sticky-Action-Bar oben mit "Markierte freigeben" und "Markierte zurücknehmen". Reuse von `BulkRelease`-Action.

## 3. Out-of-Scope

- Ajax-Submit (kein No-Reload-UX). Reload bleibt, ist aber für den User unsichtbar weil Filter sofort wieder da sind.
- localStorage statt sessionStorage. Bewusst sessionStorage — Filter sollen nicht ewig hängen bleiben.
- Bulk-Priority-Set, Bulk-Picker-Reassign. Nur Bulk-Release/Unrelease wie vom User angefragt.
- Neue Server-Actions. `BulkRelease` existiert bereits.

## 4. Filter-Persistenz — Design

### 4.1 Scope

`table-filter.js` (generisch). Aktiv für alle Tabellen, die `data-view-key` haben. Tabellen ohne View-Key persistieren NICHT (anonyme Tabellen ohne stabile Identität).

### 4.2 Speicher

`sessionStorage` mit Key `tableFilters:<viewKey>` und JSON-Body `{ "<colKey>": "<value>", ... }`.

Beispiel:
```
Key:   tableFilters:ProductionOrders
Value: {"order-number":"FA-12","status":"freigegeben"}
```

### 4.3 Speicher-Trigger

Nach jedem `applyFilters()`-Aufruf (= nach jedem `input`-Event in der Filter-Zeile). Save schreibt ALLE aktuellen Filter-Werte. Leere Werte werden weggelassen, damit der gespeicherte JSON klein bleibt. Wenn nach dem Save kein Filter aktiv ist, wird der Storage-Eintrag entfernt.

**Anmerkung:** Programmatisches Setzen von `input.value` (im Restore) löst KEIN `input`-Event aus (Browser-Spec) — daher kein Save-Loop beim Restore.

### 4.3a Cross-Cut-Event für Filter-Reaktionen

`applyFilters()` dispatched nach jedem Lauf ein `CustomEvent('table-filter-applied', { detail: { viewKey } })` auf `document`. Damit kann View-spezifisches JS (z.B. das Bulk-Bar-Sync auf der Leitstand-Seite) reagieren ohne harte Kopplung. Generisches Feature, keine Sonderbehandlung pro Tabelle.

### 4.4 Restore

Am Ende von `init()` in `table-filter.js`, NACH dem Aufbau der Filter-Zeile, aber VOR der ersten `applyFilters()`-Ausführung. Restore liest sessionStorage und setzt jedes Filter-Input auf den gespeicherten Wert. Anschließend einmal `applyFilters()` aufrufen.

Kompatibilität mit der existierenden `column-preferences-ready`-Initialisierungs-Reihenfolge: Restore passiert ÜBER `init()`, das bereits korrekt nach Column-Preferences eingehängt ist.

### 4.5 Reset

Der "Zurücksetzen"-Link auf der Leitstand-Seite (`<a asp-action="Index">`) soll bei Klick zusätzlich den sessionStorage-Eintrag für diese View löschen, sonst wäre der "Zurücksetzen" inkonsistent (URL leer, aber Spalten-Filter wiederhergestellt).

Implementierung: ein Markier-Attribut `data-clear-table-filters="ProductionOrders"` auf dem Link. Globaler Click-Handler in `table-filter.js` (oder Layout-JS) löscht beim Klick `tableFilters:ProductionOrders`. Generisch — andere Views können denselben Mechanismus nutzen.

### 4.6 Edge Cases

- **JSON-Parse-Fehler beim Restore**: Storage-Eintrag stillschweigend ignorieren und löschen.
- **colKey fehlt im aktuellen DOM**: Wert wird stillschweigend ignoriert (Spalte wurde umbenannt oder entfernt).
- **sessionStorage nicht verfügbar (Privacy-Modus, Quota)**: try/catch um beide Operationen, im Fehler-Fall lautlos no-op weil das Feature nicht kritisch ist.

## 5. Bulk-Freigabe — Design

### 5.1 Sichtbarkeit

Checkbox-Spalte und Bulk-Bar nur dann rendern, wenn `Model.LeitstandAktiv && Model.CanManagePickingRelease` (gleiche Bedingung wie die existierende Freigabe-Spalte).

### 5.2 DOM-Struktur

**Header-Row** bekommt eine neue erste Spalte (vor "FA Nr."):
```cshtml
<th style="width: 32px;" class="bulk-checkbox-col">
    <input type="checkbox" id="bulkSelectAll" title="Alle sichtbaren auswählen" />
</th>
```

**Body-Row** (nur für `!item.IsDone` — bei Done-Aufträgen kein Checkbox, leere td):
```cshtml
<td class="bulk-checkbox-col">
    @if (!item.IsDone)
    {
        <input type="checkbox" class="bulk-row-checkbox"
               data-id="@item.Id"
               data-released="@(item.IsReleasedForPicking ? "true" : "false")"
               data-has-article="@(string.IsNullOrEmpty(item.ArticleNumber) ? "false" : "true")" />
    }
</td>
```

**Filter-Row**: bekommt ein leeres `<th>` für die Checkbox-Spalte (bestehende Filter-Zeile-Logik baut sich anhand der `data-filterable`-Attribute auf — Checkbox-Spalte hat keines, also wird automatisch ein Leer-`<th>` dort gerendert).

`colCount` für die "Keine Aufträge gefunden"-Zeile muss um +1 erhöht werden.

### 5.3 Sticky Bulk-Bar

Direkt über dem Tabellen-Container (`.table-responsive`). **Wichtig:** Bar wird als direktes Kind des Page-Layout-Containers platziert (nicht innerhalb scrollbarer/overflow-clippender Wrapper), sonst funktioniert `position: sticky` nicht.

```cshtml
@if (Model.LeitstandAktiv && Model.CanManagePickingRelease)
{
    <div id="bulkActionBar" class="bulk-action-bar" style="display:none;">
        <span class="me-3"><strong id="bulkSelectedCount">0</strong> markiert</span>
        <form asp-action="BulkRelease" method="post" id="bulkReleaseForm" class="d-inline me-2">
            @Html.AntiForgeryToken()
            <input type="hidden" name="release" value="true" />
            <input type="hidden" name="returnUrl" value="@Context.Request.Path@Context.Request.QueryString" />
            <button type="submit" id="btnBulkRelease" class="btn btn-sm btn-outline-secondary" disabled>
                Markierte freigeben
            </button>
        </form>
        <form asp-action="BulkRelease" method="post" id="bulkUnreleaseForm" class="d-inline">
            @Html.AntiForgeryToken()
            <input type="hidden" name="release" value="false" />
            <input type="hidden" name="returnUrl" value="@Context.Request.Path@Context.Request.QueryString" />
            <button type="submit" id="btnBulkUnrelease" class="btn btn-sm btn-outline-success" disabled>
                Markierte zurücknehmen
            </button>
        </form>
    </div>
}
```

Beim Submit reichert JS jedes der beiden Formulare mit `<input type="hidden" name="ids" value="...">` pro markierter Zeile an (= das `data-id` aus der Checkbox).

### 5.4 Selektions-Logik (JS)

**Beim Klick auf eine Row-Checkbox** (`change`-Event):
- Recompute `selectedReleased`, `selectedUnreleased`, `selectedTotal` (Zähler über `.bulk-row-checkbox:checked`).
- Update `#bulkSelectedCount`.
- Enable/disable:
  - `#btnBulkRelease`: enabled wenn `selectedTotal > 0 && selectedReleased === 0` (alle Markierten sind nicht-freigegeben) UND alle haben `data-has-article="true"`.
  - `#btnBulkUnrelease`: enabled wenn `selectedTotal > 0 && selectedUnreleased === 0` (alle Markierten sind freigegeben).
- `#bulkActionBar` show/hide via `display: none` / `flex` je nach `selectedTotal`.

**SelectAll-Tri-State** (Standard-Bulk-UI-Pattern):
- Berechnung nach jedem Selection-Change:
  - 0 sichtbare Checkboxen checked → `#bulkSelectAll.checked = false`, `indeterminate = false`.
  - Alle sichtbaren checked → `#bulkSelectAll.checked = true`, `indeterminate = false`.
  - Sonst → `#bulkSelectAll.checked = false`, `indeterminate = true`.

**Beim Klick auf #bulkSelectAll**:
- Wenn vorher unchecked oder indeterminate → alle sichtbaren `.bulk-row-checkbox` checken.
- Wenn vorher checked → alle sichtbaren unchecken.
- Recompute (siehe oben).
- "Sichtbar" = `checkbox.closest('tr').style.display !== 'none'`.

**Wenn ein Filter sich ändert** (Filter-Row-Input):
- Bulk-JS lauscht auf das `table-filter-applied`-Event (siehe 4.3a) und ruft eigenes `bulkSyncSelection()`:
  - Iteriert alle `.bulk-row-checkbox`. Wenn `<tr>` jetzt unsichtbar (`display === 'none'`) und Checkbox checked → unchecken (User-Erwartung: was unsichtbar ist, ist nicht Teil der Auswahl).
  - Recompute Counter + Buttons + SelectAll-Tri-State.
- Keine harte Kopplung von `table-filter.js` auf Leitstand-JS — alles über das CustomEvent.

### 5.5 Bulk-Freigeben mit PickerAssignment

Wenn `Model.PickerAssignmentEnabled`:

**Entscheidung: Zwei separate Modals statt Multi-Mode.**

Begründung: Den existierenden `releaseModal` zur Laufzeit zwischen Single- und Bulk-Modus zu schalten (Form-Action ändern, Hidden-Inputs swappen, beim Cancel zurücksetzen) ist state-bug-anfällig — wenn ein User Bulk öffnet, cancelt, dann per-row klickt, könnten Stale-Inputs mitsubmittet werden. Markup-Duplikation (~30 Zeilen) ist der bessere Trade-off.

**Plan:**

1. **`releaseModal` (existing) bleibt komplett unverändert.** Single-Pfad funktioniert wie heute.
2. **Neuer `bulkReleaseModal`** (eigenes Markup, eigenes Form mit fest verdrahteter `asp-action="BulkRelease"`):
   ```cshtml
   <div class="modal fade" id="bulkReleaseModal" tabindex="-1">
       <div class="modal-dialog">
           <div class="modal-content">
               <form asp-action="BulkRelease" method="post" id="bulkReleaseModalForm">
                   @Html.AntiForgeryToken()
                   <input type="hidden" name="release" value="true" />
                   <input type="hidden" name="returnUrl" value="@Context.Request.Path@Context.Request.QueryString" />
                   <div id="bulkReleaseModalIds"></div> <!-- JS füllt mit <input name="ids" /> -->
                   <div class="modal-header">
                       <h5 class="modal-title">Sammel-Freigabe</h5>
                       <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                   </div>
                   <div class="modal-body">
                       <p><strong id="bulkReleaseModalCount">0</strong> Aufträge werden freigegeben.</p>
                       <div class="mb-3">
                           <label for="bulkAssignedPickerIdSelect" class="form-label">Kommissionierer zuweisen</label>
                           <select name="assignedPickerId" id="bulkAssignedPickerIdSelect" class="form-select" required>
                               <option value="">— bitte wählen —</option>
                               @foreach (var p in activePickers)
                               {
                                   <option value="@p.Id">@p.Name</option>
                               }
                           </select>
                       </div>
                   </div>
                   <div class="modal-footer">
                       <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Abbrechen</button>
                       <button type="submit" id="bulkReleaseModalSubmit" class="btn btn-primary" disabled>Freigeben</button>
                   </div>
               </form>
           </div>
       </div>
   </div>
   ```
3. **JS-Klick-Handler auf `#btnBulkRelease`** (nur wenn `PickerAssignmentEnabled`):
   - `preventDefault`.
   - `#bulkReleaseModalIds` leeren, dann pro markierter Row-Checkbox ein `<input type="hidden" name="ids" value="X" />` einfügen.
   - `#bulkReleaseModalCount.textContent` setzen.
   - Bootstrap-Modal öffnen.
4. **Submit-Aktivierung**: Picker-Select `change`-Handler enabled `#bulkReleaseModalSubmit` wenn ein Picker gewählt ist (gleicher Pattern wie existing Single-Modal).
5. **`#btnBulkUnrelease` braucht keinen Modal** — Zurücknehmen erfordert keine Picker-Zuweisung. Per Standard-Form-Submit, JS hängt nur die `ids` an.

**Single-Pfad bleibt unverändert.** Per-row-Buttons nutzen weiter den existierenden `releaseModal` über `.open-release-modal`-Klasse mit `data-order-id`.

### 5.6 Race Conditions / Datenfrische

Wenn ein anderer User einen Auftrag ändert während die aktuelle Auswahl gehalten wird, behandelt `BulkRelease` das defensiv: Aufträge ohne `ArticleNumber` werden geskippt, der Counter im SuccessMessage zählt nur tatsächlich verarbeitete. Bestehende Server-Logik bleibt unverändert.

### 5.7 Persistenz der Auswahl über Reload

NICHT persistieren. Die Auswahl ist intentional ephemer — nach Bulk-Freigabe sind die freigegebenen Items keine sinnvollen Bulk-Targets mehr.

## 6. CSS

Ein neuer Block in `site.css`:
```css
.bulk-action-bar {
    position: sticky;
    top: 0;
    z-index: 5;
    background: var(--bs-body-bg);
    padding: 0.5rem 0.75rem;
    margin-bottom: 0.5rem;
    border: 1px solid var(--bs-border-color);
    border-radius: 0.375rem;
    display: flex;
    align-items: center;
    flex-wrap: wrap;
    gap: 0.5rem;
}

.bulk-checkbox-col {
    width: 32px;
    text-align: center;
}
```

`display: none` versus `display: flex` wird per JS gesetzt, deshalb nur das `flex`-Layout wenn sichtbar.

## 7. Tests

**Backend**: keine neuen Tests. `BulkRelease` ist bereits getestet. Frontend-Änderungen sind reine View-/JS-Anpassungen, keine Controller-Änderung.

**Frontend**: keine JS-Test-Infrastruktur im Projekt. Manuelle Verifikation nach Plan.

## 8. Manuelle Test-Szenarien

(Werden nach Plan-Implementierung in `docs/TESTSZENARIEN.md` ergänzt.)

1. **Filter-Persistenz Leitstand**:
   - Auf Leitstand-Seite mehrere Spalten-Filter setzen (z. B. "Status: freigegeben", "Werkbank: WB1"). Klick auf "Freigeben" eines Auftrags. Filter müssen nach Reload weiterhin aktiv sein.
2. **Filter-Persistenz auf anderen Tabellen**:
   - Lagerbestand-Übersicht öffnen, Filter setzen, anderen Auftrag aufrufen, zurück zur Übersicht — Filter bleiben.
3. **Filter-Reset**:
   - Klick auf "Zurücksetzen" leert sowohl URL-Filter als auch Spalten-Filter.
4. **Bulk-Freigeben (ohne Picker-Assignment)**:
   - 5 nicht-freigegebene Aufträge markieren. "Markierte freigeben" klicken. SuccessMessage "5 Auftrag/Aufträge freigegeben". Zeilen jetzt grün/freigegeben. Filter bleibt aktiv.
5. **Bulk-Zurücknehmen**:
   - 3 freigegebene Aufträge markieren. "Markierte zurücknehmen". SuccessMessage "3 Freigabe(n) zurückgenommen".
6. **Gemischte Auswahl**:
   - Markiere 2 freigegebene + 2 nicht-freigegebene. Beide Bulk-Buttons müssen disabled sein.
7. **Bulk-Freigeben mit Picker-Assignment**:
   - Setting `KommissionierungMitZuweisung=true`. 5 Aufträge markieren, "Markierte freigeben" klicken → Modal öffnet sich, Picker auswählen, Submit. Alle 5 sind freigegeben mit demselben Picker zugewiesen.
8. **SelectAll respektiert Filter**:
   - Spalten-Filter "Werkbank: WB1" setzt → Tabelle zeigt 8 Zeilen statt 50. Header-Checkbox klicken → nur die 8 sichtbaren werden markiert.
9. **Filter ändert Auswahl**:
   - 10 markieren. Filter "Status: freigegeben" anwenden — nur sichtbare bleiben markiert, Counter zeigt korrekt.

## 9. Risiken

- **table-filter.js generisch ändern**: Risiko, dass Tabellen mit `data-view-key` aber ungewohntem Filter-Verhalten regredieren. Mitigation: Save/Restore sind selbst-neutral (no-op bei Fehlern, opt-in via View-Key).
- **`table-filter-applied`-Event-Reichweite**: Andere Code-Stellen könnten künftig auf das Event lauschen. Aktuell nur Bulk-Bar. Mitigation: Event ist namespaced (`table-filter-applied`), `detail.viewKey` erlaubt View-spezifische Filterung.
- **Pre-existing Single-Freigabe bleibt unverändert**: User kann Bulk- und Single-Freigabe parallel benutzen — Backend ist symmetrisch (beide nutzen identische Felder, nur Endpoint unterscheidet sich). Single-Modal und Bulk-Modal sind voneinander unabhängig.
- **SelectAll und versteckte Reihen**: Header-Checkbox wirkt nur auf sichtbare Reihen. User muss verstehen, dass gefilterte Reihen nicht miterfasst werden. Mitigation: Tooltip "Alle sichtbaren auswählen" auf der Header-Checkbox.

## 10. Ablauf

1. Plan schreiben (`docs/superpowers/plans/2026-05-08-leitstand-filter-bulk.md`).
2. Plan mit User abstimmen.
3. Subagent-Driven-Development implementiert Plan.
4. Manuelle Verifikation gemäß Test-Szenarien.
5. `docs/TESTSZENARIEN.md` ergänzen.
6. Changelog v1.10.0 erweitern.
