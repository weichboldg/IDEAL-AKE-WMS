# OSEON-Abfrage: Artikel-Filter + Spalten-Sortierung — Design Spec

**Datum:** 2026-05-08
**Branch:** `feature/sage-lagerbestand-sync` (gleiches Phase-2-Bundle)
**Status:** Approved → Plan
**Phase:** Internal Phase-2 Improvement, kein AppVersion-Bump.

---

## 1. Problemstellung

**1.1 Artikel-Filter zu großzügig:** In `Views/Tracking/OseonIndex.cshtml` filtert der `Artikelnummer`-Filter aktuell nur die **Kundenauftrag-Gruppen**, nicht die einzelnen Sub-Aufträge innerhalb der Gruppen. Beispiel: User sucht Artikel `87015207` → 14 Kundenaufträge werden gefunden, aber beim Aufklappen zeigt jede Gruppe **alle** 99 Sub-Aufträge (98 davon irrelevant). User erwartet, dass nur die matchenden Sub-Aufträge sichtbar sind.

**1.2 Keine Sortierung:** Die OSEON-Tabelle hat aktuell keine Sortier-Funktion. User möchte z. B. Sub-Aufträge innerhalb einer Kundenauftrag-Gruppe nach Artikelnummer sortieren können.

## 2. Ziele

1. **Artikel-Filter-Schärfung**: Bei aktivem `filterArticle` werden nur Sub-Aufträge angezeigt, deren `ArticleNumber` den Suchbegriff enthält. Kundenauftrag-Gruppen bleiben als Kontext sichtbar (Header-Zeile + "X / Y fertig"-Counter), zeigen aber nur matchende Subs.
2. **Spalten-Sortierung**: User klickt auf einen Spalten-Header → Sub-Aufträge werden innerhalb ihrer Kundenauftrag-Gruppe nach dieser Spalte sortiert. Client-seitig, kein Server-Round-Trip.

## 3. Out-of-Scope

- **Workplace-Filter**: hat das gleiche "alle Subs der Gruppe" Verhalten, wird aber **nicht** in dieser Iteration angefasst (User-Anforderung war Artikel-spezifisch). Notiz für späteren Follow-up.
- **Sortierung der Kundenauftrag-Gruppen**: Top-Level-Reihenfolge bleibt wie bisher (Worst-Color desc, dann CustomerOrderNumber asc).
- **Sortier-Persistenz**: Sort-State überlebt Page-Reload **nicht**. Bewusst — sonst kollidiert es mit dem Default-Sort und macht die UI verwirrend.
- **Bezeichnung als Filter-Feld**: User filtert nur per Artikelnummer, nicht per Description.
- **Multi-Column-Sort**: nur eine Sortier-Spalte gleichzeitig.

## 4. Artikel-Filter — Design

### 4.1 Wo filtert wer?

**Aktueller Stand** ([OseonProductionOrderRepository.cs:108-115](IdealAkeWms/Data/Repositories/OseonProductionOrderRepository.cs#L108-L115)):
- `pagedGroupKeys` = Kundenauftrag-Numbers, deren mindestens 1 Sub-Auftrag den Filter matcht.
- `items` = **alle** Sub-Aufträge dieser Kundenauftrag-Gruppen (= 99 Subs pro Gruppe in obigem Beispiel).

### 4.2 Lösung — Filterung im Controller-ViewModel-Build

Schicht: `TrackingController.OseonIndex` (nicht im Repository), Begründung:
- Repository-Signature bleibt unverändert (Schnittstelle stabil, andere Aufrufer nicht impacted).
- "X / Y fertig"-Counter berechnen wir aus dem **vollen** Sub-Set vor der Filterung — der Counter behält damit den Kontext "98 von 99 nicht relevant für deine Suche".

**Implementierung** in [TrackingController.cs:209+](IdealAkeWms/Controllers/TrackingController.cs#L209):

Nach dem inneren `foreach`, der die `subOrders`-Liste pro Gruppe baut, ABER VOR dem `groups.Add(...)` aufruft:

```csharp
// Stats aus dem VOLLEN Sub-Set (für "X/Y fertig"-Counter)
var totalSubsInGroup = subOrders.Count;
var finishedSubsInGroup = subOrders.Count(s => s.OseonStatus is 90 or 95);

// Bei aktivem Artikel-Filter Sub-Aufträge auf Treffer reduzieren
var displaySubs = subOrders;
if (!string.IsNullOrWhiteSpace(filterArticle))
{
    var artTerm = filterArticle.Trim();
    displaySubs = subOrders
        .Where(s => s.ArticleNumber != null
                    && s.ArticleNumber.Contains(artTerm, StringComparison.OrdinalIgnoreCase))
        .ToList();
}

// Worst-Color etc. weiter aus VOLLEM Set berechnen (Color steht für die Gruppe als Ganzes)
var worstColor = subOrders.Count > 0 ? subOrders.Max(s => s.Color) : TrafficLightColor.Gray;
var worstStatus = subOrders.Count > 0 ? GetWorstStatus(subOrders.Select(s => s.OseonStatus)) : 0;

groups.Add(new OseonOrderGroupViewModel
{
    CustomerOrderNumber = g.Key,
    WorstColor = worstColor,
    TotalSubOrders = totalSubsInGroup,        // VOLL (z. B. 99)
    FinishedSubOrders = finishedSubsInGroup,  // VOLL (z. B. 2)
    GroupStatusText = OseonStatusHelper.GetStatusText(worstStatus),
    GroupStatusBadgeClass = OseonStatusHelper.GetStatusBadgeClass(worstStatus),
    SubOrders = displaySubs                   // GEFILTERT (z. B. 1)
});
```

### 4.3 UX-Auswirkungen

- Counter zeigt weiter `(2 / 99 fertig)` bei der Kundenauftrag-Header-Zeile — informativ, nicht irreführend.
- Aufgeklappt: nur 1 Sub-Auftrag (der matchende) wird gerendert.
- Wenn keine Subs matchen (Edge-Case bei sehr exotischer Filter-Kombo): Group-Header ohne Subs wäre kosmetisch hässlich, aber per Definition kann das nicht auftreten — die Gruppe wäre dann gar nicht in `pagedGroupKeys` enthalten.

### 4.4 Edge Cases

- `filterArticle` mit Whitespace: trim erledigt. Empty-after-trim: kein Filter aktiv.
- `ArticleNumber` der Sub-Order ist `null`: ausgeschlossen durch `s.ArticleNumber != null`.
- Case: `OrdinalIgnoreCase` matchen — User tippt evtl. lowercase, Daten sind oft uppercase.

## 5. Sortierung — Design

### 5.1 Sortierbare Spalten

Click-Targets im `<thead>`:

| Spalte | Sort-Key | Sort-Wert (Sub-Row Daten) |
|--------|----------|--------------------------|
| Auftrag | `oseon-order-number` | `OseonOrderNumber` (string) |
| Artikelnr. | `article-number` | `ArticleNumber` (string, leer bei null) |
| Bezeichnung | `description` | `Description1` (string) |
| Werkbank | `workplace` | `WorkplaceName` (string) |
| Status | `status` | `OseonStatus` (numeric) — sortiert wertneutral, Status-Codes sind ordinal (10..95) |
| Soll/Ist | `progress` | `QuantityTarget` (numeric) — primär das Soll |
| Endtermin | `end-date` | `DueDate` (yyyy-MM-dd ISO-format für stabile lex.-Sortierung; null = leer) |

Nicht sortierbar: Expand-Spalte (`expand`), Traffic-Light (kein eigener Header-Text).

### 5.2 Sort-State

3-State pro Click auf einen Spalten-Header:
- 1. Klick: ascending (▲) — JS sortiert nach gewählter Spalte aufsteigend.
- 2. Klick: descending (▼) — JS sortiert absteigend.
- 3. Klick: unsortiert — JS sortiert nach Default-Spalte (`oseon-order-number` asc), Indikator wird entfernt. Default ist also nicht "kein Sort", sondern "Sort wieder auf Default-Schlüssel angewandt".

Nur eine Spalte ist gleichzeitig aktiv — beim Wechsel zur neuen Spalte wird der alte Indikator entfernt und der State der alten Spalte verworfen (sie startet beim nächsten Klick wieder bei "asc").

### 5.3 Sort-Verhalten

**Scope:** Sub-Aufträge **innerhalb ihrer Kundenauftrag-Gruppe**. Die Top-Level-Reihenfolge der Kundenauftrag-Gruppen bleibt unverändert.

**Operationen-Rows (Ebene 2)** ziehen mit ihrem Sub-Auftrag mit — sie haben eine fixe Reihenfolge (PositionNumber asc, server-side) und werden nicht selbst sortiert.

**Implementierung (rein client-side):**
- Pro Kundenauftrag-Gruppe: alle Sub-Auftrag-Rows + ihre Operations-Rows als logische Einheit in einem Array sammeln.
- Array nach Sort-Key sortieren.
- DOM-Rows in neuer Reihenfolge in den `<tbody>` re-appenden (`appendChild` verschiebt, nicht klont).

Um die Sort-Werte der Sub-Rows zugänglich zu machen, werden im Razor-Markup `data-sort-*`-Attribute pro Sub-Row gesetzt:

```cshtml
<tr class="oseon-tree-sub @subRowClass"
    data-parent="@groupKey"
    data-sub="@subKey"
    data-sort-oseon-order-number="@sub.OseonOrderNumber"
    data-sort-article-number="@(sub.ArticleNumber ?? "")"
    data-sort-description="@(sub.Description1 ?? "")"
    data-sort-workplace="@(sub.WorkplaceName ?? "")"
    data-sort-status="@sub.OseonStatus"
    data-sort-progress="@sub.QuantityTarget"
    data-sort-end-date="@(sub.DueDate?.ToString("yyyy-MM-dd") ?? "")"
    style="display: none; cursor: pointer;">
```

Operations-Rows brauchen keine `data-sort-*` — sie folgen ihrem Sub.

### 5.4 Sort-Indikator

Auf jedem sortierbaren `<th>`:
- Cursor wird `pointer`.
- Ein Span `<span class="oseon-sort-indicator"></span>` (klein, am Ende des Header-Texts) zeigt `▲` / `▼` / leer.
- CSS-Hover-Effekt zeigt Sortierbarkeit an.

### 5.5 Sort-Algorithmus

```javascript
function sortOseonSubs(sortKey, direction) {
    var groups = document.querySelectorAll('tr.oseon-tree-group');
    groups.forEach(function (groupRow) {
        var groupKey = groupRow.getAttribute('data-group');
        var tbody = groupRow.parentElement;
        var subs = Array.from(tbody.querySelectorAll('tr.oseon-tree-sub[data-parent="' + groupKey + '"]'));
        if (subs.length <= 1) return;

        // Pro Sub: zugehörige Operations-Rows sammeln
        var blocks = subs.map(function (subRow) {
            var subKey = subRow.getAttribute('data-sub');
            var ops = Array.from(tbody.querySelectorAll('tr.oseon-tree-op[data-parent-sub="' + subKey + '"]'));
            return { subRow: subRow, ops: ops };
        });

        // Sortieren
        blocks.sort(function (a, b) {
            var av = sortValueFromRow(a.subRow, sortKey);
            var bv = sortValueFromRow(b.subRow, sortKey);
            return compare(av, bv, sortKey, direction);
        });

        // DOM neu einsortieren — Sub-Row direkt nach Group-Row, Ops direkt nach Sub
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

function sortValueFromRow(row, sortKey) {
    return row.getAttribute('data-sort-' + sortKey) || '';
}

function compare(a, b, sortKey, direction) {
    var numericKeys = ['status', 'progress'];
    var numeric = numericKeys.indexOf(sortKey) >= 0;
    var av = numeric ? parseFloat(a) || 0 : a.toLowerCase();
    var bv = numeric ? parseFloat(b) || 0 : b.toLowerCase();
    if (av < bv) return direction === 'asc' ? -1 : 1;
    if (av > bv) return direction === 'asc' ? 1 : -1;
    return 0;
}
```

`Endtermin` wird als ISO-yyyy-MM-dd-String sortiert — funktioniert lexikographisch korrekt für Datumssortierung. Leere Werte landen am Anfang bei asc (sortieren als leerer String).

### 5.6 Edge Cases / Interaktionen

- **Filter ändert sich während Sortierung aktiv ist**: Server-Reload bringt neue Tabelle, Sort-State ist verloren. Default-Reihenfolge wieder aktiv. (Wir persistieren bewusst nicht.)
- **Expand/Collapse während Sortierung aktiv**: bestehender JS-State `expandedGroups` / `expandedSubs` bleibt korrekt — Toggle-Logik basiert auf `data-group` / `data-sub`-Attributen, nicht auf DOM-Reihenfolge.
- **Sub-Aufträge ohne Operations**: nur die Sub-Row wird sortiert. Funktioniert.
- **Sortierung bei nur 1 Sub pro Gruppe**: kein Effekt, früher Return.
- **Wechsel der Sort-Spalte**: alter Indikator entfernt, neuer gesetzt.

## 6. Bestehende Daten-Modelle / Code-Punkte

- [TrackingController.cs:185-340](IdealAkeWms/Controllers/TrackingController.cs#L185-L340) — `OseonIndex`-Action.
- [OseonOrderGroupViewModel](IdealAkeWms/Models) — bleibt strukturell unverändert.
- [OseonIndex.cshtml:99-220](IdealAkeWms/Views/Tracking/OseonIndex.cshtml#L99-L220) — Tabelle + Sub-Row-Markup.

Keine DB-Schema-Änderung. Keine neue Migration. Keine neuen Endpoints.

## 7. Tests

**Backend:** keine neuen automatisierten Tests. Die ViewModel-Filterung im Controller ist trivial Logik (One-Liner LINQ-Where), wird durch manuelle Test-Szenarien abgedeckt.

**Frontend:** Projekt hat keine JS-Test-Infrastruktur. Manuelle Verifikation via TESTSZENARIEN.

## 8. Manuelle Test-Szenarien (für TESTSZENARIEN.md)

(Werden im Plan-Schritt ergänzt.)

1. **TS-7.x — OSEON-Artikel-Filter zeigt nur matchende Subs**
   - Vorbedingung: Mehrere Kundenaufträge mit vielen Subs, einer der Subs hat Artikel `X`.
   - Aktion: Artikel-Filter mit Suchbegriff für `X` setzen.
   - Erwartung: Kundenauftrag-Header zeigt "(2 / 99 fertig)" (volle Stats), aufgeklappt erscheint nur die eine matchende Sub.
2. **TS-7.x — OSEON-Sort: Spalten-Klick sortiert Subs**
   - Aktion: Auftrag mit mehreren Subs aufklappen, Klick auf Spalten-Header "Artikelnr.".
   - Erwartung: Sub-Aufträge in der Gruppe alphabetisch nach Artikelnummer sortiert (asc, ▲-Indikator). Operations bleiben unter ihrem Sub.
3. **TS-7.x — OSEON-Sort 3-State**
   - Aktion: 3× auf denselben Spalten-Header klicken.
   - Erwartung: 1. Klick asc, 2. Klick desc, 3. Klick zurück zur Default-Reihenfolge (OseonOrderNumber asc), Indikator weg.
4. **TS-7.x — OSEON-Sort + Filter kombiniert**
   - Aktion: Artikel-Filter setzen → 1 Sub pro Gruppe sichtbar. Klick auf Sort-Header.
   - Erwartung: Sortierung wirkt nur auf die sichtbaren Subs (1 pro Gruppe, also kein Effekt). Kein Fehler, kein DOM-Glitch.

## 9. Risiken

- **Sub-Reordering bricht Operations-Tracking**: Mitigation: Operations werden anhand `data-parent-sub` gefunden und mit ihrer Sub-Row als Block verschoben.
- **Performance bei sehr großen Subs-Listen**: Sort über DOM-Move ist O(n log n) Vergleiche + O(n) DOM-Operationen. Bei 99 Subs × 14 Gruppen = 1386 Subs, kein Performance-Problem.
- **`data-sort-*`-Attribute bei Razor-Encoding**: Razor encoded HTML automatisch — Spezialzeichen in Bezeichnungen wie `&` / `<` werden korrekt escaped.

## 10. Ablauf

1. Plan schreiben (`docs/superpowers/plans/2026-05-08-oseon-search-sort.md`).
2. Plan mit User abstimmen.
3. Subagent-Driven-Development implementiert Plan.
4. Manuelle Verifikation gemäß Test-Szenarien.
5. `docs/TESTSZENARIEN.md` ergänzen.
6. Changelog v1.10.0 erweitern.
