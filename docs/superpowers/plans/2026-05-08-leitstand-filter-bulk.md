# Leitstand: Filter-Persistenz + Bulk-Freigabe — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Leitstand (`Views/ProductionOrders/Index.cshtml`) erhält (a) generische Persistenz der Spalten-Filter über Reloads via sessionStorage und (b) eine Bulk-Freigabe-UI mit Checkboxen pro Zeile und Sticky-Action-Bar — wiederverwendet den existierenden `BulkRelease`-Server-Endpoint.

**Architecture:** Drei Layer:
- `wwwroot/js/table-filter.js` — generische Save/Restore + `table-filter-applied`-CustomEvent (alle Tabellen mit `data-view-key`).
- `Views/ProductionOrders/Index.cshtml` — Checkbox-Spalte, Bulk-Bar, Bulk-Picker-Modal, View-spezifische JS-Logik.
- `wwwroot/css/site.css` — Sticky-Bar + Checkbox-Spalten-Styles.

**Branch:** `feature/sage-lagerbestand-sync` (kein neuer Branch — Phase-2-Bundle).

**Spec:** `docs/superpowers/specs/2026-05-08-leitstand-filter-bulk-design.md`.

**Commit-Konvention:** `feat(leitstand): ...` / `feat(table-filter): ...` / `style(leitstand): ...` / `docs: ...`. Co-Authored-By trailer.

**Files:**
- Modify: `IdealAkeWms/wwwroot/js/table-filter.js`
- Modify: `IdealAkeWms/wwwroot/css/site.css`
- Modify: `IdealAkeWms/Views/ProductionOrders/Index.cshtml`
- Modify: `docs/TESTSZENARIEN.md`
- Modify: `IdealAkeWms/Views/Help/Changelog.cshtml`

---

## Task 1: Filter-Persistenz in `table-filter.js`

**Files:**
- Modify: `IdealAkeWms/wwwroot/js/table-filter.js`

Generische sessionStorage-Persistenz, gegated durch `data-view-key`. Plus globaler Reset-Helper für `[data-clear-table-filters]`-Links. Plus `table-filter-applied` CustomEvent.

- [ ] **Step 1: Persist-Helpers + Custom-Event ergänzen**

In `IdealAkeWms/wwwroot/js/table-filter.js`, ganz oben in der IIFE (nach den Variablen-Deklarationen wie `_table`, `_filterRow`, etc., aber vor der `init`-Funktion), folgenden Block einfügen:

```javascript
// ----- Filter-Persistenz (sessionStorage, gegated durch data-view-key) -----
function getViewKey() {
    return _table && _table.getAttribute('data-view-key');
}

function saveFiltersToStorage() {
    var viewKey = getViewKey();
    if (!viewKey) return;
    try {
        var filters = window.getActiveFilters ? window.getActiveFilters() : {};
        var key = 'tableFilters:' + viewKey;
        if (Object.keys(filters).length === 0) {
            sessionStorage.removeItem(key);
        } else {
            sessionStorage.setItem(key, JSON.stringify(filters));
        }
    } catch (e) {
        // sessionStorage nicht verfuegbar (Privacy/Quota) — lautlos no-op
    }
}

function restoreFiltersFromStorage() {
    var viewKey = getViewKey();
    if (!viewKey || !_filterRow) return;
    try {
        var key = 'tableFilters:' + viewKey;
        var raw = sessionStorage.getItem(key);
        if (!raw) return;
        var filters = JSON.parse(raw);
        if (!filters || typeof filters !== 'object') {
            sessionStorage.removeItem(key);
            return;
        }
        Object.keys(filters).forEach(function (colKey) {
            var input = _filterRow.querySelector('input[data-col-key="' + colKey + '"]');
            if (input) input.value = filters[colKey];
        });
    } catch (e) {
        try { sessionStorage.removeItem('tableFilters:' + viewKey); } catch (e2) { /* */ }
    }
}

function dispatchFilterAppliedEvent() {
    var viewKey = getViewKey();
    document.dispatchEvent(new CustomEvent('table-filter-applied', {
        detail: { viewKey: viewKey }
    }));
}
```

- [ ] **Step 2: Save + Event in `applyFilters()` einhängen**

Suche die Funktion `applyFilters` (Zeile ca. 145-150 — nach `window.getActiveFilters`). Am ENDE der Funktion (nach dem letzten `forEach`/`if`-Block, kurz vor dem schließenden `}`), einfügen:

```javascript
saveFiltersToStorage();
dispatchFilterAppliedEvent();
```

- [ ] **Step 3: Restore in `init()` einhängen**

Suche `function init()` (ca. Zeile 20). Innerhalb von `init`, am Ende der Filter-Row-Erzeugung — also NACH dem `thead.appendChild(_filterRow);` (ca. Zeile 84) und VOR dem Sortier-Block (`_headers.forEach` ab Zeile 87) — einfügen:

```javascript
restoreFiltersFromStorage();
applyFilters();  // einmalig anwenden, falls Werte restored wurden
```

WICHTIG: `applyFilters()` muss aufgerufen werden, damit nach Restore die Tabelle gefiltert ist (programmatisches `input.value = ...` löst kein Input-Event aus).

- [ ] **Step 4: Globaler Reset-Link-Handler**

Am Ende der IIFE (vor dem schließenden `})();` ganz unten), GLOBALER Click-Handler für `[data-clear-table-filters]`:

```javascript
document.addEventListener('click', function (e) {
    var link = e.target.closest('[data-clear-table-filters]');
    if (!link) return;
    var viewKey = link.getAttribute('data-clear-table-filters');
    if (!viewKey) return;
    try {
        sessionStorage.removeItem('tableFilters:' + viewKey);
    } catch (err) {
        // lautlos no-op
    }
    // Default-Navigation des Links läuft normal weiter
});
```

- [ ] **Step 5: Build verifizieren**

```pwsh
dotnet build IdealAkeWms/IdealAkeWms.csproj --nologo
```

Expected: 0 Fehler. (JS ist statische Datei — Build-Schritt prüft nur dass kein Razor/CS-Fehler entsteht.)

- [ ] **Step 6: Manuelles Smoke (Filter-Persistenz)**

```pwsh
dotnet run --project IdealAkeWms/IdealAkeWms.csproj
```

Im Browser:
1. Leitstand öffnen (`/ProductionOrders`).
2. In der Spalten-Filter-Zeile mehrere Filter setzen (z.B. Werkbank: "WB1").
3. F5 (Reload). → Filter müssen automatisch wieder angewendet sein.
4. Auf "Zurücksetzen" klicken (Filter-Card oben). → URL-Filter UND Spalten-Filter müssen leer sein.

**Hinweis:** Schritt 4 funktioniert erst nach Task 2 vollständig (das `data-clear-table-filters`-Attribut wird dort am Link gesetzt). Hier nur Schritt 1-3 testen.

- [ ] **Step 7: Commit**

```pwsh
git add IdealAkeWms/wwwroot/js/table-filter.js
git commit -m "feat(table-filter): add sessionStorage filter persistence and table-filter-applied event" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 2: Index.cshtml — Checkbox-Spalte + Reset-Hook

**Files:**
- Modify: `IdealAkeWms/Views/ProductionOrders/Index.cshtml`

Neue Checkbox-Spalte ganz links der Tabelle. Plus `data-clear-table-filters`-Attribut auf "Zurücksetzen"-Link.

- [ ] **Step 1: Reset-Link erweitern**

Suche in `IdealAkeWms/Views/ProductionOrders/Index.cshtml` Zeile 59 (`<a asp-action="Index" class="btn btn-outline-secondary">Zurücksetzen</a>`). Ersetzen durch:

```cshtml
<a asp-action="Index" class="btn btn-outline-secondary" data-clear-table-filters="ProductionOrders">Zurücksetzen</a>
```

- [ ] **Step 2: Header-`<th>` für SelectAll**

Suche die Header-Row (Zeile ca. 73-100, beginnt mit `<thead>` → `<tr>` → `<th style="width: 90px;" data-filterable data-col-key="order-number">FA Nr.</th>`).

VOR dem ersten existierenden `<th>` (FA Nr.), neuen `<th>` einfügen — aber nur wenn Bulk-Feature aktiv ist:

```cshtml
@if (Model.LeitstandAktiv && Model.CanManagePickingRelease)
{
    <th style="width: 32px;" class="bulk-checkbox-col">
        <input type="checkbox" id="bulkSelectAll" title="Alle sichtbaren auswählen" />
    </th>
}
<th style="width: 90px;" data-filterable data-col-key="order-number">FA Nr.</th>
```

- [ ] **Step 3: Body-`<td>` für Row-Checkbox**

Suche die Body-Row (Zeile ca. 105-115 — die `<tr>` die die FA-Daten anzeigt, beginnt mit `<tr ...>` und enthält `<td>` für FA-Nr. usw.).

Identifiziere die ERSTE `<td>` der Body-Row (FA-Nr.-Spalte). VOR dieser `<td>` einfügen:

```cshtml
@if (Model.LeitstandAktiv && Model.CanManagePickingRelease)
{
    <td class="bulk-checkbox-col">
        @if (!item.IsDone)
        {
            <input type="checkbox" class="bulk-row-checkbox"
                   data-id="@item.Id"
                   data-released="@(item.IsReleasedForPicking ? "true" : "false")"
                   data-has-article="@(string.IsNullOrEmpty(item.ArticleNumber) ? "false" : "true")" />
        }
    </td>
}
```

- [ ] **Step 4: `colCount` in "Keine Aufträge"-Zeile erhöhen**

Suche die Zeile ca. 304 mit `var colCount = 17;` und den darauffolgenden `colCount++`-If-Statements. Direkt NACH `var colCount = 17;` ergänzen:

```cshtml
if (Model.LeitstandAktiv && Model.CanManagePickingRelease) colCount++;
```

WICHTIG: Diese Zeile gibt es bereits VOR diesem Schritt für die Freigabe-Spalte (Zeile 306). Stelle sicher, dass die NEUE `colCount++`-Anweisung als ZUSÄTZLICHE Zeile hinzugefügt wird — nicht die bestehende dupliziert. Beide bleiben.

- [ ] **Step 5: Build verifizieren**

```pwsh
dotnet build IdealAkeWms/IdealAkeWms.csproj --nologo
```

Expected: 0 Fehler.

- [ ] **Step 6: Manuelles Smoke**

Web-App starten (oder Hot-Reload). Leitstand öffnen.

Erwartet:
- Erste Spalte zeigt Checkboxen (nur für nicht-erledigte FAs).
- Header hat eine SelectAll-Checkbox (noch ohne Funktion — kommt in Task 3).
- "Zurücksetzen" hat das `data-clear-table-filters="ProductionOrders"`-Attribut (DevTools prüfen). Klick auf Reset leert auch sessionStorage (jetzt vollständig testbar).

- [ ] **Step 7: Commit**

```pwsh
git add IdealAkeWms/Views/ProductionOrders/Index.cshtml
git commit -m "feat(leitstand): add bulk-selection checkbox column and clear-filter hook on reset link" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 3: Bulk-Action-Bar — Markup + Selection-Logik

**Files:**
- Modify: `IdealAkeWms/Views/ProductionOrders/Index.cshtml`

Sticky-Bar VOR `<div class="table-responsive">`. Plus JS für Selection-Tracking, SelectAll, Listening auf `table-filter-applied`. Form-Submit hängt `<input name="ids" />` für jede markierte Zeile an.

- [ ] **Step 1: Bulk-Bar-Markup**

Suche `<div class="table-responsive">` (ca. Zeile 65). DIREKT DAVOR einfügen:

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

- [ ] **Step 2: Bulk-Selection-JS einfügen**

Suche im View den existing `@section Scripts { ... }`-Block (ca. Zeile 415-440). Falls vorhanden — innerhalb des Blocks, am Ende. Falls nicht — direkt vor `</body>`-Schluss, neuen Block einfügen.

Innerhalb der existierenden `DOMContentLoaded`-Listener (oder eigenem Block), folgendes Modul einfügen:

```javascript
// Bulk-Selection für Leitstand-Freigabe
(function () {
    var bar = document.getElementById('bulkActionBar');
    if (!bar) return;  // Bulk-Feature inaktiv (kein Leitstand oder keine Berechtigung)

    var selectAll = document.getElementById('bulkSelectAll');
    var counter = document.getElementById('bulkSelectedCount');
    var btnRelease = document.getElementById('btnBulkRelease');
    var btnUnrelease = document.getElementById('btnBulkUnrelease');
    var formRelease = document.getElementById('bulkReleaseForm');
    var formUnrelease = document.getElementById('bulkUnreleaseForm');

    function getRowCheckboxes() {
        return Array.prototype.slice.call(document.querySelectorAll('.bulk-row-checkbox'));
    }
    function isVisible(cb) {
        var tr = cb.closest('tr');
        return tr && tr.style.display !== 'none';
    }
    function getVisibleCheckboxes() {
        return getRowCheckboxes().filter(isVisible);
    }
    function getCheckedCheckboxes() {
        return getRowCheckboxes().filter(function (cb) { return cb.checked; });
    }

    function recompute() {
        var checked = getCheckedCheckboxes();
        var total = checked.length;
        var releasedCount = checked.filter(function (cb) { return cb.dataset.released === 'true'; }).length;
        var unreleasedCount = total - releasedCount;
        var allHaveArticle = checked.every(function (cb) { return cb.dataset.hasArticle === 'true'; });

        counter.textContent = total;
        bar.style.display = total > 0 ? 'flex' : 'none';

        // Freigeben: alle Markierten sind nicht-freigegeben UND alle haben Artikelnummer
        btnRelease.disabled = !(total > 0 && releasedCount === 0 && allHaveArticle);
        // Zurücknehmen: alle Markierten sind freigegeben (Artikelnummer egal)
        btnUnrelease.disabled = !(total > 0 && unreleasedCount === 0);

        // SelectAll Tri-State
        var visible = getVisibleCheckboxes();
        var visibleChecked = visible.filter(function (cb) { return cb.checked; }).length;
        if (visibleChecked === 0) {
            selectAll.checked = false;
            selectAll.indeterminate = false;
        } else if (visibleChecked === visible.length) {
            selectAll.checked = true;
            selectAll.indeterminate = false;
        } else {
            selectAll.checked = false;
            selectAll.indeterminate = true;
        }
    }

    function bulkSyncSelection() {
        // Filter hat Reihen versteckt — unsichtbare Checkboxen unchecken
        getRowCheckboxes().forEach(function (cb) {
            if (cb.checked && !isVisible(cb)) cb.checked = false;
        });
        recompute();
    }

    // Row-Checkbox change
    document.addEventListener('change', function (e) {
        if (e.target.classList && e.target.classList.contains('bulk-row-checkbox')) {
            recompute();
        }
    });

    // SelectAll click
    selectAll.addEventListener('click', function () {
        var newState = selectAll.checked;  // bereits durch Browser umgeschaltet
        getVisibleCheckboxes().forEach(function (cb) { cb.checked = newState; });
        recompute();
    });

    // Filter-Änderung → Auswahl synchronisieren
    document.addEventListener('table-filter-applied', function (e) {
        if (e.detail && e.detail.viewKey === 'ProductionOrders') {
            bulkSyncSelection();
        }
    });

    // Form-Submit-Handler — ids[] anhängen
    function attachIds(form) {
        // Vorherige ids[]-Inputs entfernen
        form.querySelectorAll('input[data-bulk-id]').forEach(function (n) { n.remove(); });
        getCheckedCheckboxes().forEach(function (cb) {
            var input = document.createElement('input');
            input.type = 'hidden';
            input.name = 'ids';
            input.value = cb.dataset.id;
            input.setAttribute('data-bulk-id', '1');
            form.appendChild(input);
        });
    }
    formRelease.addEventListener('submit', function () { attachIds(formRelease); });
    formUnrelease.addEventListener('submit', function () { attachIds(formUnrelease); });

    // Initial-State (für den Fall dass Server-State Pre-checked Checkboxen liefert — aktuell nie der Fall)
    recompute();
})();
```

- [ ] **Step 3: Build verifizieren**

```pwsh
dotnet build IdealAkeWms/IdealAkeWms.csproj --nologo
```

Expected: 0 Fehler.

- [ ] **Step 4: Manuelles Smoke**

Web-App starten. Leitstand öffnen.

Erwartet:
- Mehrere Checkboxen markieren → Bulk-Bar erscheint, Counter zählt korrekt.
- "Markierte freigeben" disabled wenn schon freigegebene markiert sind oder wenn FA ohne Artikelnummer markiert ist.
- "Markierte zurücknehmen" disabled wenn nicht-freigegebene markiert sind.
- SelectAll: Klick markiert alle sichtbaren. Beim Filter-Anwenden werden unsichtbare Checkboxen automatisch demarkiert.
- "Markierte freigeben" Klick → POST an BulkRelease, Server gibt Aufträge frei, redirect mit returnUrl. Filter überleben (dank Task 1).
- ⚠️ Falls `KommissionierungMitZuweisung=true` aktiv ist: die Submit-Logik fehlt noch (Task 4). In diesem Fall im Smoke das Setting kurz auf `false` schalten oder Task 4 abwarten.

- [ ] **Step 5: Commit**

```pwsh
git add IdealAkeWms/Views/ProductionOrders/Index.cshtml
git commit -m "feat(leitstand): add sticky bulk-action bar with selection logic and filter sync" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 4: Bulk-Picker-Modal (PickerAssignment-Pfad)

**Files:**
- Modify: `IdealAkeWms/Views/ProductionOrders/Index.cshtml`

Wenn `KommissionierungMitZuweisung=true`, muss vor dem Bulk-Freigeben ein Picker zugewiesen werden. Eigener Modal (state-isoliert vom Single-Modal).

- [ ] **Step 1: Bulk-Modal-Markup**

Suche im View die Zeile mit `@if (Model.PickerAssignmentEnabled) { ... var activePickers = ...` (ca. Zeile 320). Innerhalb des bestehenden If-Blocks, NACH dem schließenden `</div>` des existierenden `releaseModal` (das endet mit dem Modal-Block), aber VOR dem schließenden `}` des If-Blocks, neuen Modal einfügen:

```cshtml
@if (Model.LeitstandAktiv && Model.CanManagePickingRelease)
{
    <!-- Bulk Release Modal -->
    <div class="modal fade" id="bulkReleaseModal" tabindex="-1" aria-labelledby="bulkReleaseModalLabel" aria-hidden="true">
        <div class="modal-dialog">
            <div class="modal-content">
                <form asp-action="BulkRelease" method="post" id="bulkReleaseModalForm">
                    @Html.AntiForgeryToken()
                    <input type="hidden" name="release" value="true" />
                    <input type="hidden" name="returnUrl" value="@Context.Request.Path@Context.Request.QueryString" />
                    <div id="bulkReleaseModalIds"></div>
                    <div class="modal-header">
                        <h5 class="modal-title" id="bulkReleaseModalLabel">Sammel-Freigabe</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Schliessen"></button>
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
}
```

WICHTIG: Dieser If-Block ist **separat** vom existierenden `@if (Model.PickerAssignmentEnabled)`-Block (der hält den Single-Modal). Das Markup wird auch ohne PickerAssignment gerendert, aber die JS-Logik in Step 2 öffnet den Modal NUR bei aktivem PickerAssignment. Begründung: Die `activePickers`-Variable ist nur unter dem PickerAssignment-If verfügbar, also positioniere den Bulk-Modal **innerhalb** dieses Blocks. Korrektur:

Das Markup muss INNERHALB des bestehenden `@if (Model.PickerAssignmentEnabled)`-Blocks platziert werden, NACH dem existierenden Modal-`</div>`. Entferne den eigenen `@if (Model.LeitstandAktiv && Model.CanManagePickingRelease)`-Wrapper aus dem oberen Snippet und positioniere den Modal direkt im PickerAssignment-Block:

```cshtml
@if (Model.PickerAssignmentEnabled)
{
    var activePickers = ViewBag.ActivePickers as List<IdealAkeWms.Models.User> ?? new List<IdealAkeWms.Models.User>();

    <!-- Existing Single Release Modal — bleibt unverändert -->
    <div class="modal fade" id="releaseModal" ...>
        ...
    </div>

    @if (Model.LeitstandAktiv && Model.CanManagePickingRelease)
    {
        <!-- NEW: Bulk Release Modal -->
        <div class="modal fade" id="bulkReleaseModal" ...>
            ... (siehe oberes Snippet ohne den äußeren Wrapper-If) ...
        </div>
    }
}
```

- [ ] **Step 2: JS — Bulk-Release-Klick öffnet Modal statt direktem Submit**

Modifiziere das in Task 3 angelegte JS-Modul an drei Stellen:

**(a) `pickerAssignmentEnabled`-Variable an den Anfang des Moduls hochziehen.** Direkt nach `var bar = document.getElementById('bulkActionBar'); if (!bar) return;` einfügen:

```javascript
var pickerAssignmentEnabled = @(Model.PickerAssignmentEnabled.ToString().ToLower());
```

**(b) `attachIds()` mit Early-Return für Release-Form ergänzen.** In der existierenden `attachIds`-Funktion aus Task 3, ALS ERSTE ZEILE im Funktionskörper:

```javascript
// Bei aktivem PickerAssignment lädt Release-Submit den Modal — keine ids ins eigentliche Form
if (pickerAssignmentEnabled && form === formRelease) return;
```

**(c) Picker-Modal-Logik am Ende des Moduls einfügen** (vor dem letzten `recompute();`-Aufruf):

```javascript
// PickerAssignment-Pfad: Release-Submit öffnet Modal statt zu submitten
if (pickerAssignmentEnabled) {
    var bulkModal = document.getElementById('bulkReleaseModal');
    var bulkModalIds = document.getElementById('bulkReleaseModalIds');
    var bulkModalCount = document.getElementById('bulkReleaseModalCount');
    var bulkPickerSelect = document.getElementById('bulkAssignedPickerIdSelect');
    var bulkModalSubmit = document.getElementById('bulkReleaseModalSubmit');

    if (bulkModal && bulkModalIds && bulkModalCount && bulkPickerSelect && bulkModalSubmit) {
        formRelease.addEventListener('submit', function (e) {
            e.preventDefault();
            var checked = getCheckedCheckboxes();
            bulkModalIds.innerHTML = '';
            checked.forEach(function (cb) {
                var input = document.createElement('input');
                input.type = 'hidden';
                input.name = 'ids';
                input.value = cb.dataset.id;
                bulkModalIds.appendChild(input);
            });
            bulkModalCount.textContent = checked.length;
            bulkPickerSelect.value = '';
            bulkModalSubmit.disabled = true;
            new bootstrap.Modal(bulkModal).show();
        });

        bulkPickerSelect.addEventListener('change', function () {
            bulkModalSubmit.disabled = !bulkPickerSelect.value;
        });
    }
}
```

**Begründung der Trennung (a/b/c):** Beide Listener auf `formRelease.submit` laufen — der attachIds-Listener (Task 3) und der Modal-Open-Listener (hier). Mit dem Early-Return aus (b) macht der Task-3-Listener bei aktivem PickerAssignment nichts mehr. Der Modal-Open-Listener ruft `preventDefault` und öffnet stattdessen den Modal. Das eigentliche `BulkRelease`-POST kommt dann beim Modal-Submit (`bulkReleaseModalForm`). Form-Identitäten getrennt, kein Capture-Phase-Trick nötig.

- [ ] **Step 3: Build verifizieren**

```pwsh
dotnet build IdealAkeWms/IdealAkeWms.csproj --nologo
```

Expected: 0 Fehler.

- [ ] **Step 4: Manuelles Smoke (PickerAssignment-Pfad)**

In den Settings `KommissionierungMitZuweisung = true` setzen (oder DB direkt). Web-App neustarten. Leitstand öffnen.

Erwartet:
- 3 nicht-freigegebene Aufträge markieren. "Markierte freigeben" → Modal öffnet sich, zeigt "3 Aufträge werden freigegeben."
- Dropdown "Kommissionierer zuweisen" hat alle aktiven Picker.
- "Freigeben"-Button im Modal disabled bis Picker gewählt ist.
- Picker wählen → Submit aktiv. Klick → POST an BulkRelease mit `ids[]` und `assignedPickerId`. Alle 3 sind freigegeben mit demselben Picker.
- Setting wieder zurücksetzen falls Test-Setting.

- [ ] **Step 5: Commit**

```pwsh
git add IdealAkeWms/Views/ProductionOrders/Index.cshtml
git commit -m "feat(leitstand): add bulk-release modal for picker-assignment flow" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 5: CSS — Sticky-Bar + Checkbox-Spalte

**Files:**
- Modify: `IdealAkeWms/wwwroot/css/site.css`

- [ ] **Step 1: CSS-Block ergänzen**

Am Ende von `IdealAkeWms/wwwroot/css/site.css`:

```css
/* ===== Bulk-Action-Bar (Leitstand) ===== */
.bulk-action-bar {
    position: sticky;
    top: 0;
    z-index: 5;
    background: var(--bs-body-bg, #ffffff);
    padding: 0.5rem 0.75rem;
    margin-bottom: 0.5rem;
    border: 1px solid var(--bs-border-color, #dee2e6);
    border-radius: 0.375rem;
    /* display: none/flex wird per JS gesetzt */
    align-items: center;
    flex-wrap: wrap;
    gap: 0.5rem;
}

.bulk-checkbox-col {
    width: 32px;
    text-align: center;
    vertical-align: middle;
}

.bulk-checkbox-col input[type="checkbox"] {
    cursor: pointer;
    transform: scale(1.2);
}
```

- [ ] **Step 2: Build + manuelles Smoke**

```pwsh
dotnet build IdealAkeWms/IdealAkeWms.csproj --nologo
```

Expected: 0 Fehler.

Web-App im Browser:
- Bulk-Bar bleibt beim Scrollen oben (sticky).
- Checkboxen sind zentriert, leicht vergrößert für bessere Touch-Bedienbarkeit.

- [ ] **Step 3: Commit**

```pwsh
git add IdealAkeWms/wwwroot/css/site.css
git commit -m "style(leitstand): bulk-action bar sticky styling and checkbox column" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 6: TESTSZENARIEN.md + Changelog

**Files:**
- Modify: `docs/TESTSZENARIEN.md`
- Modify: `IdealAkeWms/Views/Help/Changelog.cshtml`

- [ ] **Step 1: TS-4.9 aktualisieren (existierendes Spec → jetzt implementiert)**

Suche in `docs/TESTSZENARIEN.md` den Abschnitt `### TS-4.9 — Leitstand-Massenfreigabe` (ca. Zeile 808). Ersetze den GESAMTEN Block durch:

```markdown
### TS-4.9 — Leitstand-Massenfreigabe (Bulk-Freigabe)

**Vorbedingungen:**
- AppSetting `LeitstandAktiv = true`.
- AppSetting `KommissionierungMitZuweisung = false`.
- Benutzer hat Rolle `leitstand` oder `admin`.
- Mehrere FAs vorhanden, davon mindestens 3 mit Artikelnummer und nicht freigegeben, eine ohne Artikelnummer.

**Schritte:**
1. Leitstand-Liste oeffnen.
2. Checkboxen fuer 4 FAs setzen — 3 mit Artikelnummer, 1 ohne.
3. Sticky-Bar oben zeigt "4 markiert".

**Erwartetes Verhalten:**
- "Markierte freigeben"-Button ist DISABLED — weil eine markierte FA keine Artikelnummer hat.
- Nach Demarkierung der FA ohne Artikelnummer (jetzt 3 markiert) ist der Button aktiv.
- Klick auf "Markierte freigeben" → 3 FAs werden freigegeben. SuccessMessage "3 Auftrag/Aufträge freigegeben."
- Spalten-Filter (falls gesetzt) bleiben nach Reload aktiv.

**Negativfall:**
- Mischauswahl (freigegebene + nicht-freigegebene) → beide Bulk-Buttons disabled.

---

### TS-4.9a — Leitstand-Bulk-Zurücknehmen

**Vorbedingungen:**
- AppSetting `LeitstandAktiv = true`.
- Mehrere FAs sind freigegeben.

**Schritte:**
1. Leitstand-Liste oeffnen.
2. Checkboxen fuer 3 freigegebene FAs setzen.
3. "Markierte zurücknehmen" anklicken.

**Erwartetes Verhalten:**
- 3 FAs sind nicht mehr freigegeben (Status zurückgesetzt).
- SuccessMessage "3 Freigabe(n) zurückgenommen."
- Picker-Zuweisungen wurden entfernt.

---

### TS-4.9b — Leitstand-Bulk-Freigabe mit Picker-Zuweisung

**Vorbedingungen:**
- AppSetting `LeitstandAktiv = true`.
- AppSetting `KommissionierungMitZuweisung = true`.
- Mehrere Picker sind aktiv.
- Mindestens 5 FAs mit Artikelnummer, nicht freigegeben.

**Schritte:**
1. Leitstand-Liste oeffnen.
2. Checkboxen fuer 5 FAs setzen.
3. "Markierte freigeben" anklicken.

**Erwartetes Verhalten:**
- Modal "Sammel-Freigabe" öffnet sich, zeigt "5 Aufträge werden freigegeben."
- "Freigeben"-Button im Modal ist disabled.
- Nach Picker-Auswahl wird der Button aktiv.
- Submit → alle 5 FAs sind freigegeben mit dem gewählten Picker.

---

### TS-4.9c — Leitstand SelectAll respektiert Filter

**Vorbedingungen:**
- AppSetting `LeitstandAktiv = true`.
- Leitstand zeigt 50+ FAs.

**Schritte:**
1. Leitstand-Liste oeffnen.
2. In der Spalten-Filter-Zeile "Werkbank" auf einen Wert filtern, sodass nur ~8 Zeilen sichtbar sind.
3. Header-Checkbox (SelectAll) anklicken.

**Erwartetes Verhalten:**
- Nur die 8 sichtbaren FAs werden markiert. Counter zeigt "8 markiert".
- Nach Filter-Entfernen sind die 8 weiterhin markiert; die anderen 42 bleiben unmarkiert.

---

### TS-4.9d — Filter-Persistenz nach Freigabe

**Vorbedingungen:**
- AppSetting `LeitstandAktiv = true`.

**Schritte:**
1. Leitstand-Liste oeffnen.
2. In der Spalten-Filter-Zeile mehrere Filter setzen (z.B. Werkbank: "WB1").
3. Eine FA per Single-Klick "Freigeben" → Page reload.

**Erwartetes Verhalten:**
- Spalten-Filter bleiben nach Reload aktiv.
- "Zurücksetzen" leert sowohl URL-Filter als auch Spalten-Filter.

**Negativfall:**
- Browser-Tab schließen und neu öffnen → Filter sind weg (sessionStorage erwartetes Verhalten).
```

- [ ] **Step 2: Index oben aktualisieren**

Im selben File, ganz oben im Index-Block (ca. Zeile 22-30), die Zeile für Bereich 4 anpassen:

```markdown
| 4. Fertigungsauftraege | [→](#4-fertigungsauftraege) | TS-4.1 – TS-4.10 |
```

→

```markdown
| 4. Fertigungsauftraege | [→](#4-fertigungsauftraege) | TS-4.1 – TS-4.10 (inkl. TS-4.9a/b/c/d Bulk-Freigabe + Filter-Persistenz) |
```

- [ ] **Step 3: Stand-Datum aktualisieren**

In `docs/TESTSZENARIEN.md` Zeile 3 (`**Stand:** 2026-04-30 (v1.8.3)`) ersetzen durch:

```markdown
**Stand:** 2026-05-08 (v1.10.0)
```

- [ ] **Step 4: Changelog v1.10.0 ergänzen**

In `IdealAkeWms/Views/Help/Changelog.cshtml`, im **existing** v1.10.0-Card, am Ende der `<ul>` (nach dem Performance-Bullet aus dem letzten Plan):

```cshtml
<li><strong>Leitstand-Bulk-Freigabe:</strong> Mehrere Aufträge per Checkbox markieren und gemeinsam freigeben oder zurücknehmen. Sticky-Action-Bar zeigt Auswahl-Status, Header-Checkbox markiert alle sichtbaren Zeilen (respektiert Filter). Mit Picker-Zuweisung öffnet sich ein Modal für die Picker-Auswahl.</li>
<li><strong>Spalten-Filter-Persistenz:</strong> Spalten-Filter bleiben nach Page-Reload erhalten (per sessionStorage, pro Browser-Tab). Wirkt für alle Tabellen mit View-Identifier (Leitstand, Bestand, Bewegungshistorie, OSEON-Tracking u.a.). "Zurücksetzen"-Button leert auch die persistierten Filter.</li>
```

- [ ] **Step 5: Build + Tests**

```pwsh
dotnet build --nologo
dotnet test --nologo --no-build
```

Expected: alles grün (565 Tests).

- [ ] **Step 6: Commit**

```pwsh
git add docs/TESTSZENARIEN.md IdealAkeWms/Views/Help/Changelog.cshtml
git commit -m "docs: testszenarien + changelog for leitstand bulk-release and filter persistence" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Manuelle End-to-End-Verifikation (vor Merge)

Alle Szenarien aus TS-4.9 / TS-4.9a / TS-4.9b / TS-4.9c / TS-4.9d durchspielen.

Zusätzlich:
- **Andere Tabellen (Filter-Persistenz):** Bestand-Übersicht öffnen, Filter setzen, Detail aufrufen, zurück. Filter bleiben.
- **OSEON-Tracking** (anderes `data-view-key`): Filter setzen, Reload. Bleiben.
- **Tabellen ohne `data-view-key`:** Falls vorhanden — Filter werden NICHT persistiert. Sicherstellen, dass auch keine Konsolen-Fehler auftreten.

---

## Self-Review-Notiz

**Spec-Coverage:**
- 4.1-4.6 (Filter-Persistenz) → Task 1 + Task 2 (Reset-Hook).
- 5.1-5.4 (Bulk-UI) → Task 2 + Task 3.
- 5.5 (PickerAssignment-Pfad) → Task 4.
- 5.6/5.7 (Race / Persistenz-Auswahl) → keine Code-Änderung, dokumentiert in TESTSZENARIEN.
- 6 (CSS) → Task 5.
- 7 (Tests) → keine Backend-Tests; manuelle TESTSZENARIEN in Task 6.
- 8 (Test-Szenarien) → Task 6.

**Reihenfolge ist wichtig:**
1. Task 1 zuerst — generic, Foundation für Task 3 (Custom-Event).
2. Task 2 vor Task 3 — Checkbox-Spalte muss da sein bevor SelectAll-JS sie sucht.
3. Task 3 vor Task 4 — Basis-Bulk-JS bevor Picker-Pfad ergänzt wird.
4. Task 5 (CSS) kann parallel zu Task 3/4 — bewusst nach hinten gelegt damit Funktionalität zuerst grün ist.
5. Task 6 zuletzt — alle Code-Änderungen müssen abgeschlossen sein.

**No-Placeholder-Check:** keine TBDs, alle Code-Snippets sind vollständig.

**Commit-Frequency:** 6 Commits — einer pro Task. Klein genug für Review, gross genug um logisch zu sein.
