# OSEON-Abfrage: Artikel-Filter + Spalten-Sortierung — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Zwei UX-Verbesserungen auf der OSEON-Teileverfolgung-Seite (`Views/Tracking/OseonIndex.cshtml`): (1) Artikel-Filter zeigt nur die matchenden Sub-Aufträge innerhalb ihrer Kundenauftrag-Gruppe, nicht mehr die ganze Baugruppe; (2) Sub-Aufträge sind innerhalb ihrer Gruppe per Spalten-Klick sortierbar (client-seitig, 7 Spalten).

**Architecture:** Drei Schichten:
- `TrackingController.OseonIndex` — Filter-Schicht: Sub-Aufträge bei aktivem `filterArticle` reduzieren, Counter "X / Y fertig" weiter aus voller Sub-Liste berechnen.
- `Views/Tracking/OseonIndex.cshtml` — `data-sort-*`-Attribute pro Sub-Row, sortable `<th>`-Marker mit Sort-Indikator.
- Inline-JS in derselben View — Tri-State-Click-Handler, Sort-Algorithmus mit Operations-als-Block-Move, nulls-last-Compare.

**Branch:** `feature/sage-lagerbestand-sync` (Phase-2-Bundle).

**Spec:** `docs/superpowers/specs/2026-05-08-oseon-search-sort-design.md`.

**Commit-Konvention:** `fix(oseon): ...` / `feat(oseon): ...` / `style(oseon): ...` / `docs: ...`. Co-Authored-By trailer.

**Files:**
- Modify: `IdealAkeWms/Controllers/TrackingController.cs`
- Modify: `IdealAkeWms/Views/Tracking/OseonIndex.cshtml`
- Modify: `IdealAkeWms/wwwroot/css/site.css`
- Modify: `docs/TESTSZENARIEN.md`
- Modify: `IdealAkeWms/Views/Help/Changelog.cshtml`

---

## Task 1: Controller — Artikel-Filter auf Sub-Auftrag-Ebene

**Files:**
- Modify: `IdealAkeWms/Controllers/TrackingController.cs`

Bei aktivem `filterArticle` werden Sub-Aufträge VOR dem `groups.Add(...)` auf matchende reduziert. Counter-Stats (`TotalSubOrders`, `FinishedSubOrders`) weiter aus dem VOLLEN Sub-Set berechnet, damit "(2 / 99 fertig)" als Kontext erhalten bleibt.

- [ ] **Step 1: Filter-Logik einfügen**

In `IdealAkeWms/Controllers/TrackingController.cs`, in der `OseonIndex`-Action, im `foreach`-Block (ca. Zeile 209-316), suche das Ende der inneren `foreach`-Schleife (die `subOrders`-Liste pro Gruppe baut). Direkt VOR den existierenden Zeilen mit `var worstColor = subOrders.Count > 0 ? ... : ...;` (ca. Zeile 297), einfügen:

```csharp
// Stats aus dem VOLLEN Sub-Set fuer "X/Y fertig"-Counter
var totalSubsInGroup = subOrders.Count;
var finishedSubsInGroup = subOrders.Count(s => s.OseonStatus is 90 or 95);

// Bei aktivem Artikel-Filter Sub-Auftraege auf Treffer reduzieren.
// Worst-Color/Status weiter aus VOLLEM Set — die Kundenauftrag-Gruppe behaelt ihren Status-Kontext.
var displaySubs = subOrders;
if (!string.IsNullOrWhiteSpace(filterArticle))
{
    var artTerm = filterArticle.Trim();
    displaySubs = subOrders
        .Where(s => s.ArticleNumber != null
                    && s.ArticleNumber.Contains(artTerm, StringComparison.OrdinalIgnoreCase))
        .ToList();
}
```

- [ ] **Step 2: ViewModel-Build umstellen**

Direkt darunter, in der existierenden `groups.Add(new OseonOrderGroupViewModel { ... })`-Anweisung (ca. Zeile 306-315), TWO Aenderungen:

1. `TotalSubOrders = subOrders.Count` → `TotalSubOrders = totalSubsInGroup`
2. `FinishedSubOrders = subOrders.Count(s => s.OseonStatus is 90 or 95)` → `FinishedSubOrders = finishedSubsInGroup`
3. `SubOrders = subOrders` → `SubOrders = displaySubs`

Das Endresultat sollte etwa so aussehen:

```csharp
groups.Add(new OseonOrderGroupViewModel
{
    CustomerOrderNumber = g.Key,
    WorstColor = worstColor,
    TotalSubOrders = totalSubsInGroup,
    FinishedSubOrders = finishedSubsInGroup,
    GroupStatusText = OseonStatusHelper.GetStatusText(worstStatus),
    GroupStatusBadgeClass = OseonStatusHelper.GetStatusBadgeClass(worstStatus),
    SubOrders = displaySubs
});
```

WICHTIG: `worstColor` und `worstStatus` weiter aus `subOrders` (= VOLLES Set) berechnen — sie repraesentieren den Status der Gruppe als Ganzes, nicht der gefilterten Auswahl.

- [ ] **Step 3: Build + Tests**

```pwsh
dotnet build --nologo
dotnet test --nologo --no-build
```

Expected: 0 errors. Tests 565/565 grün — keine Test-Auswirkung erwartet (keine Test deckt Sub-Filter ab, da Action keine Unit-Tests hat).

- [ ] **Step 4: Commit**

```pwsh
git add IdealAkeWms/Controllers/TrackingController.cs
git commit -m "fix(oseon): article filter narrows sub-orders, not just groups" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 2: View — `data-sort-*`-Attribute + Sortable-Header

**Files:**
- Modify: `IdealAkeWms/Views/Tracking/OseonIndex.cshtml`

Sub-Auftrag-Rows bekommen pro sortierbarer Spalte ein `data-sort-<key>`-Attribut. Header-`<th>`-Elemente bekommen `data-sortable` + `data-sort-key` + Sort-Indikator-Span.

- [ ] **Step 1: Header-`<th>` Sortable-Marker**

Suche im View die Header-Row (ca. Zeile 102-111). Ersetze den existierenden `<thead>`-Inhalt:

```cshtml
<thead>
    <tr>
        <th style="width: 30px;" data-col-key="expand"></th>
        <th data-col-key="order-number">Auftrag</th>
        <th data-col-key="article-number">Artikelnr.</th>
        <th data-col-key="description">Bezeichnung</th>
        <th data-col-key="workbench">Werkbank</th>
        <th data-col-key="status">Status</th>
        <th data-col-key="progress">Soll / Ist</th>
        <th data-col-key="end-date">Endtermin</th>
    </tr>
</thead>
```

durch:

```cshtml
<thead>
    <tr>
        <th style="width: 30px;" data-col-key="expand"></th>
        <th data-col-key="order-number" data-sortable data-sort-key="oseon-order-number">
            Auftrag <span class="oseon-sort-indicator"></span>
        </th>
        <th data-col-key="article-number" data-sortable data-sort-key="article-number">
            Artikelnr. <span class="oseon-sort-indicator"></span>
        </th>
        <th data-col-key="description" data-sortable data-sort-key="description">
            Bezeichnung <span class="oseon-sort-indicator"></span>
        </th>
        <th data-col-key="workbench" data-sortable data-sort-key="workplace">
            Werkbank <span class="oseon-sort-indicator"></span>
        </th>
        <th data-col-key="status" data-sortable data-sort-key="status" title="Sortiert nach Status-Phase (Unvollstaendig → Fertig)">
            Status <span class="oseon-sort-indicator"></span>
        </th>
        <th data-col-key="progress" data-sortable data-sort-key="progress" title="Sortiert nach Soll-Menge">
            Soll / Ist <span class="oseon-sort-indicator"></span>
        </th>
        <th data-col-key="end-date" data-sortable data-sort-key="end-date">
            Endtermin <span class="oseon-sort-indicator"></span>
        </th>
    </tr>
</thead>
```

- [ ] **Step 2: Sub-Row `data-sort-*`-Attribute**

Suche im View die Sub-Auftrag-Row (ca. Zeile 161, beginnt mit `<tr class="oseon-tree-sub @subRowClass" data-parent="@groupKey" data-sub="@subKey" style="display: none; cursor: pointer;">`). Ersetze diese Zeile durch:

```cshtml
<tr class="oseon-tree-sub @subRowClass"
    data-parent="@groupKey"
    data-sub="@subKey"
    data-sort-oseon-order-number="@(sub.OseonOrderNumber ?? "")"
    data-sort-article-number="@(sub.ArticleNumber ?? "")"
    data-sort-description="@(sub.Description1 ?? "")"
    data-sort-workplace="@(sub.WorkplaceName ?? "")"
    data-sort-status="@sub.OseonStatus"
    data-sort-progress="@sub.QuantityTarget.ToString(System.Globalization.CultureInfo.InvariantCulture)"
    data-sort-end-date="@(sub.DueDate?.ToString("yyyy-MM-dd") ?? "")"
    style="display: none; cursor: pointer;">
```

WICHTIG: `data-sort-progress` MUSS `InvariantCulture` verwenden, sonst rendert Razor `12,5` (de-AT) und `parseFloat` truncated zu `12`.

- [ ] **Step 3: Build verifizieren**

```pwsh
dotnet build IdealAkeWms/IdealAkeWms.csproj --nologo
```

Expected: 0 errors.

- [ ] **Step 4: Commit**

```pwsh
git add IdealAkeWms/Views/Tracking/OseonIndex.cshtml
git commit -m "feat(oseon): add data-sort-* attributes per sub-row and sortable header markers" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 3: JS — Tri-State-Sort-Handler

**Files:**
- Modify: `IdealAkeWms/Views/Tracking/OseonIndex.cshtml`

Click-Handler auf sortierbaren Header-`<th>`. Tri-State pro Spalte (asc → desc → default). Sort-Algorithmus moved Sub-Rows + ihre Operations-Rows als Block.

- [ ] **Step 1: Sort-Modul am Ende des existing DOMContentLoaded-Handlers**

Suche im `@section Scripts`-Block den existierenden `document.addEventListener('DOMContentLoaded', function () { ... });`-Block. Innerhalb dieses Blocks, am ENDE (vor dem schließenden `});`), folgendes Modul ergänzen:

```javascript
// ===== OSEON Sub-Auftrag Spalten-Sort =====
var sortState = { key: null, direction: null }; // null = default-Sort (oseon-order-number asc)

function sortValueFromRow(row, sortKey) {
    return row.getAttribute('data-sort-' + sortKey) || '';
}

function compareSortValues(a, b, sortKey, direction) {
    var aEmpty = a === '' || a === null;
    var bEmpty = b === '' || b === null;
    // Nulls/Empty always last — unabhaengig von asc/desc
    if (aEmpty && bEmpty) return 0;
    if (aEmpty) return 1;
    if (bEmpty) return -1;

    var numericKeys = ['status', 'progress'];
    var numeric = numericKeys.indexOf(sortKey) >= 0;
    var av = numeric ? parseFloat(a) : a.toLowerCase();
    var bv = numeric ? parseFloat(b) : b.toLowerCase();

    if (numeric && (isNaN(av) || isNaN(bv))) {
        if (isNaN(av) && isNaN(bv)) return 0;
        return isNaN(av) ? 1 : -1;
    }

    if (av < bv) return direction === 'asc' ? -1 : 1;
    if (av > bv) return direction === 'asc' ? 1 : -1;
    return 0;
}

function sortOseonSubs(sortKey, direction) {
    var groupRows = document.querySelectorAll('tr.oseon-tree-group');
    groupRows.forEach(function (groupRow) {
        var groupKey = groupRow.getAttribute('data-group');
        var tbody = groupRow.parentElement;
        var subs = Array.prototype.slice.call(
            tbody.querySelectorAll('tr.oseon-tree-sub[data-parent="' + groupKey + '"]'));
        if (subs.length <= 1) return;

        // Pro Sub: zugehoerige Operations-Rows sammeln
        var blocks = subs.map(function (subRow) {
            var subKey = subRow.getAttribute('data-sub');
            var ops = Array.prototype.slice.call(
                tbody.querySelectorAll('tr.oseon-tree-op[data-parent-sub="' + subKey + '"]'));
            return { subRow: subRow, ops: ops };
        });

        // Sortieren
        blocks.sort(function (a, b) {
            var av = sortValueFromRow(a.subRow, sortKey);
            var bv = sortValueFromRow(b.subRow, sortKey);
            return compareSortValues(av, bv, sortKey, direction);
        });

        // DOM neu einsortieren — Sub direkt nach Group, Ops direkt nach Sub
        var insertAfter = groupRow;
        blocks.forEach(function (block) {
            insertAfter.after(block.subRow);
            insertAfter = block.subRow;
            block.ops.forEach(function (opRow) {
                insertAfter.after(opRow);
                insertAfter = opRow;
            });
        });
    });
}

function updateSortIndicators(activeKey, direction) {
    document.querySelectorAll('th[data-sortable]').forEach(function (th) {
        var indicator = th.querySelector('.oseon-sort-indicator');
        if (!indicator) return;
        if (th.getAttribute('data-sort-key') === activeKey && direction) {
            indicator.textContent = direction === 'asc' ? '▲' : '▼';
        } else {
            indicator.textContent = '';
        }
    });
}

document.querySelectorAll('th[data-sortable]').forEach(function (th) {
    th.style.cursor = 'pointer';
    th.addEventListener('click', function () {
        var clickedKey = th.getAttribute('data-sort-key');
        var nextDirection;
        if (sortState.key !== clickedKey) {
            // Neue Spalte → starte mit asc
            nextDirection = 'asc';
        } else if (sortState.direction === 'asc') {
            nextDirection = 'desc';
        } else if (sortState.direction === 'desc') {
            nextDirection = null; // 3. Klick → default
        } else {
            nextDirection = 'asc';
        }

        if (nextDirection === null) {
            sortState = { key: null, direction: null };
            sortOseonSubs('oseon-order-number', 'asc');
            updateSortIndicators(null, null);
        } else {
            sortState = { key: clickedKey, direction: nextDirection };
            sortOseonSubs(clickedKey, nextDirection);
            updateSortIndicators(clickedKey, nextDirection);
        }
    });
});
```

WICHTIG: Das Modul gehört INNERHALB des bestehenden `DOMContentLoaded`-Listeners (sonst sind `expandedGroups`/`expandedSubs`-Variablen nicht im Scope, und der existing Code muss vorher ausgeführt sein).

- [ ] **Step 2: Build verifizieren**

```pwsh
dotnet build IdealAkeWms/IdealAkeWms.csproj --nologo
```

Expected: 0 errors.

- [ ] **Step 3: Tests**

```pwsh
dotnet test --nologo --no-build
```

Expected: 565/565 grün — reine View-/JS-Aenderung.

- [ ] **Step 4: Commit**

```pwsh
git add IdealAkeWms/Views/Tracking/OseonIndex.cshtml
git commit -m "feat(oseon): tri-state column sort for sub-orders within their customer-order group" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 4: CSS — Sort-Indikator + Sortable-Cursor

**Files:**
- Modify: `IdealAkeWms/wwwroot/css/site.css`

- [ ] **Step 1: CSS-Block ergänzen**

Am Ende von `IdealAkeWms/wwwroot/css/site.css`:

```css
/* ===== OSEON Sub-Auftrag Spalten-Sort ===== */
#oseonTree thead th[data-sortable] {
    user-select: none;
}

#oseonTree thead th[data-sortable]:hover {
    background-color: rgba(67, 166, 226, 0.08);  /* leichter --ake-secondary Schatten */
}

.oseon-sort-indicator {
    display: inline-block;
    margin-left: 0.25rem;
    font-size: 0.75em;
    color: var(--ake-secondary);
    min-width: 0.7em;
}
```

- [ ] **Step 2: Build + manuelles Smoke**

```pwsh
dotnet build IdealAkeWms/IdealAkeWms.csproj --nologo
```

Expected: 0 errors.

Web-App im Browser:
- Cursor wird Pointer beim Hover auf sortierbaren Spalten-Headern.
- Sort-Indikator (▲/▼) erscheint nach Click.
- Hover-Effekt zeigt Sortierbarkeit.

- [ ] **Step 3: Commit**

```pwsh
git add IdealAkeWms/wwwroot/css/site.css
git commit -m "style(oseon): sortable header cursor and indicator styling" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 5: TESTSZENARIEN + Changelog

**Files:**
- Modify: `docs/TESTSZENARIEN.md`
- Modify: `IdealAkeWms/Views/Help/Changelog.cshtml`

- [ ] **Step 1: TS-7-Index anpassen**

In `docs/TESTSZENARIEN.md` Zeile 29 (`| 7. OSEON Teileverfolgung | [→](#7-oseon-teileverfolgung) | TS-7.1 – TS-7.10 |`) ersetzen durch:

```markdown
| 7. OSEON Teileverfolgung | [→](#7-oseon-teileverfolgung) | TS-7.1 – TS-7.10 (inkl. TS-7.6a/b/c Artikel-Filter + Sortierung) |
```

- [ ] **Step 2: Neue Test-Szenarien einfügen**

Suche in `docs/TESTSZENARIEN.md` den Abschnitt `### TS-7.6 — Artikelnummern-Suche mit QR-Scan` (ca. Zeile 1326). Direkt NACH dem `---` am Ende dieses Szenarios (vor `### TS-7.7`), neue Szenarien einfügen:

```markdown
### TS-7.6a — OSEON Artikel-Filter zeigt nur matchende Sub-Auftraege

**Vorbedingungen:**
- Ein Kundenauftrag mit vielen Sub-Auftraegen vorhanden, einer der Subs hat Artikel `87015207`.
- Der Kundenauftrag enthaelt z. B. 99 Subs gesamt.

**Schritte:**
1. OSEON-Teileverfolgung oeffnen.
2. Im Filter `Artikelnummer` den Suchbegriff `87015207` eingeben, "Filtern" klicken.
3. Den gefundenen Kundenauftrag aufklappen.

**Erwartetes Verhalten:**
- Kundenauftrag-Header zeigt weiterhin `(2 / 99 fertig)` (volle Stats — Kontext bleibt erhalten).
- Aufgeklappt erscheint nur der eine matchende Sub-Auftrag mit Artikelnummer `87015207`.
- Andere 98 Subs sind ausgeblendet.
- Status-Badge der Gruppe zeigt weiterhin den schlechtesten Status der GESAMTEN Gruppe (nicht nur des matchenden Subs).

---

### TS-7.6b — OSEON Sub-Sortierung per Spalten-Klick

**Vorbedingungen:**
- Ein Kundenauftrag mit mehreren Sub-Auftraegen, unterschiedliche Artikelnummern und Endtermine.

**Schritte:**
1. OSEON-Teileverfolgung oeffnen.
2. Den Kundenauftrag aufklappen → Sub-Auftraege sind nach OseonOrderNumber asc sortiert (Default).
3. Auf Spalten-Header `Artikelnr.` klicken.
4. Erneut auf `Artikelnr.` klicken.
5. Erneut (3. Mal) auf `Artikelnr.` klicken.

**Erwartetes Verhalten:**
- 1. Klick: Sub-Auftraege alphabetisch nach Artikelnummer aufsteigend (▲-Indikator). Operations bleiben unter ihrem Sub.
- 2. Klick: absteigend (▼-Indikator).
- 3. Klick: zurueck zu Default-Sort (OseonOrderNumber asc, kein Indikator).
- Andere Kundenauftraege werden in ihren eigenen Gruppen ebenfalls sortiert (nicht uebergreifend).

---

### TS-7.6c — OSEON Endtermin-Sort: nulls last

**Vorbedingungen:**
- Mindestens 3 Sub-Auftraege im selben Kundenauftrag — einer davon ohne Endtermin.

**Schritte:**
1. OSEON-Teileverfolgung oeffnen.
2. Den Kundenauftrag aufklappen.
3. Auf Spalten-Header `Endtermin` klicken (asc).
4. Erneut auf `Endtermin` klicken (desc).

**Erwartetes Verhalten:**
- Asc: naechste Termine oben, Sub OHNE Endtermin am Ende.
- Desc: spaeteste Termine oben, Sub OHNE Endtermin trotzdem am Ende (nicht oben).
- Keine DOM-Glitches, Operations folgen ihrem Sub.

---

### TS-7.6d — OSEON Filter + Sort kombiniert

**Vorbedingungen:**
- Wie TS-7.6a.

**Schritte:**
1. Artikel-Filter setzen (= 1 matchender Sub pro Gruppe sichtbar).
2. Auf Spalten-Header `Artikelnr.` klicken.

**Erwartetes Verhalten:**
- Sortierung wirkt nur auf die sichtbaren Subs (1 pro Gruppe).
- Kein JS-Fehler, kein DOM-Glitch.

---
```

- [ ] **Step 3: Changelog v1.10.0 ergänzen**

In `IdealAkeWms/Views/Help/Changelog.cshtml`, im **existing** v1.10.0-Card, am Ende der `<ul>` (nach den vorherigen Bullets):

```cshtml
<li><strong>OSEON Artikel-Filter:</strong> Bei Artikel-Filterung erscheinen nur noch die matchenden Sub-Auftraege innerhalb ihres Kundenauftrags. Vorher wurde die gesamte Baugruppe angezeigt — jetzt sieht man nur noch den gesuchten Artikel im Kontext seiner Kundenauftrag-Gruppe.</li>
<li><strong>OSEON Spalten-Sortierung:</strong> Sub-Auftraege koennen per Klick auf einen Spalten-Header sortiert werden (Artikelnr., Bezeichnung, Werkbank, Status, Soll/Ist, Endtermin). Drei-Klick-Zyklus: aufsteigend → absteigend → Default. Sortierung wirkt nur innerhalb der Kundenauftrag-Gruppen, Operations folgen ihrem Sub.</li>
```

- [ ] **Step 4: Build + Tests**

```pwsh
dotnet build --nologo
dotnet test --nologo --no-build
```

Expected: alles grün (565 Tests).

- [ ] **Step 5: Commit**

```pwsh
git add docs/TESTSZENARIEN.md IdealAkeWms/Views/Help/Changelog.cshtml
git commit -m "docs: testszenarien + changelog for oseon article-filter and column-sort" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Manuelle End-to-End-Verifikation (vor Merge)

Alle Szenarien aus TS-7.6a / TS-7.6b / TS-7.6c / TS-7.6d durchspielen.

Plus:
- **Filter+Expand-State**: Filter setzen → matchender Sub sichtbar. Expand des Subs → Operations sichtbar.
- **Sort+Expand-State**: Subs aufklappen, sortieren → Sub bleibt expanded, Operations folgen mit.
- **column-preferences-Interaktion**: Spalten via Offcanvas umordnen → Sort funktioniert weiter (Sort identifiziert Spalte ueber `data-sort-key`, nicht ueber Index).

---

## Self-Review-Notiz

**Spec-Coverage:**
- Section 4 (Artikel-Filter) → Task 1.
- Section 5.1-5.4 (Sortable Spalten + Indikator + State) → Task 2 (Markup) + Task 3 (JS) + Task 4 (CSS).
- Section 5.5 (Sort-Algorithmus) → Task 3 mit nulls-last + NaN-defensive.
- Section 5.6 (Edge Cases / Interaktionen) → manuelle Test-Szenarien in Task 5.
- Section 7 (Tests) → keine Backend-Tests; manuelle TESTSZENARIEN in Task 5.

**Reihenfolge ist wichtig:**
1. Task 1 zuerst — Filter ist unabhängig, kein Sort-Impact.
2. Task 2 vor Task 3 — Markup muss da sein bevor JS es liest.
3. Task 3 vor Task 4 — Funktionalität bevor CSS-Polish.
4. Task 5 zuletzt — alle Code-Änderungen abgeschlossen.

**No-Placeholder-Check:** keine TBDs, alle Code-Snippets sind vollständig.

**Commit-Frequency:** 5 Commits — einer pro Task.
