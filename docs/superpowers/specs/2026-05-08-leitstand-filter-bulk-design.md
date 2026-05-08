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

Direkt über dem Tabellen-Container (`.table-responsive`):

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

**Beim Klick auf #bulkSelectAll**:
- Für alle `.bulk-row-checkbox` deren `<tr>` aktuell sichtbar ist (`tr.style.display !== 'none'`): Checkbox-State = SelectAll-State.
- Recompute (siehe oben).

**Wenn ein Filter sich ändert** (Filter-Row-Input):
- Existierender `applyFilters` versteckt Zeilen via `display:none`. Nicht-sichtbare Row-Checkboxen sollten **automatisch deselektiert** werden (User-Erwartung: was unsichtbar ist, kann nicht in Bulk-Action). Nach `applyFilters()` einmal `bulkSyncSelection()` aufrufen, der unsichtbare Checkboxen unchecked setzt und Counter aktualisiert.

### 5.5 Bulk-Freigeben mit PickerAssignment

Wenn `Model.PickerAssignmentEnabled`:

Der existierende `releaseModal` wird wiederverwendet. Statt `<input name="id" />` braucht der Modal im Bulk-Mode mehrere `<input name="ids" />`. Lösung:

1. Modal-Form bekommt zusätzlich ein bereits vorhandenes `<input type="hidden" name="id" id="releaseModalOrderId" />` UND ein neues `<div id="releaseModalBulkIds"></div>` Container.
2. JS-Handler beim Klick auf `#btnBulkRelease`:
   - Bei `PickerAssignmentEnabled`: Default-Submit verhindern (`preventDefault`), Modal öffnen, `<form action>` auf `BulkRelease` setzen, `releaseModalOrderId` clearen, `releaseModalBulkIds` mit hidden `<input name="ids" value="X" />` für jede markierte Zeile füllen, ein verstecktes `<input name="release" value="true">` hinzufügen.
   - Beim Modal-Submit läuft alles in einem Request an `BulkRelease`.
3. Single-Klick-Pfad bleibt unverändert: existing per-row Form mit `<input name="id" />` wird per JS-Handler `.open-release-modal` weiter wie vor genutzt — der setzt `releaseModalOrderId` und Modal-`action` auf `ToggleRelease`.

Der Modal hält also intern beide Modi ("single" / "bulk") und hängt sein `<form action>` und seinen Hidden-Inputs entsprechend dem letzten Trigger an.

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
- **Modal-Mehrzweck**: Single- und Bulk-Pfad teilen sich denselben Modal. Mitigation: klare State-Markierung (Modal-Form-action wird beim Öffnen gesetzt, nicht im Markup fix verdrahtet).
- **Pre-existing Single-Freigabe bleibt**: User kann Bulk und Single-Freigabe parallel benutzen — Backend ist symmetrisch (beide nutzen identische Felder, nur Endpoint unterscheidet sich).

## 10. Ablauf

1. Plan schreiben (`docs/superpowers/plans/2026-05-08-leitstand-filter-bulk.md`).
2. Plan mit User abstimmen.
3. Subagent-Driven-Development implementiert Plan.
4. Manuelle Verifikation gemäß Test-Szenarien.
5. `docs/TESTSZENARIEN.md` ergänzen.
6. Changelog v1.10.0 erweitern.
