---
typ: notiz
spec: "[[2026-09-10-fa-liste-ausbau-matchcode-spec]]"
status: InUmsetzung
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# Umsetzung: FA-Liste ausbauen (Zeilenwerte, HauptFA-Zeile, Freigabe-Kaskade, Matchcode)

Spec [[2026-09-10-fa-liste-ausbau-matchcode-spec]] · bestehender Buendel-Worktree, **kein** neuer,
**nicht** von `main` abgezweigt. Vier fachlich unabhaengige Bloecke, 20 Akzeptanzkriterien.

## Schritt 0 — Spec-Nachbesserung (erledigt, Commit `6ebe058`)

Fuenf Widersprueche zwischen Rumpf und Freigabe-Antworten bereinigt; Anzeige-Spec nachgezogen
(`CascadeRelease` widerruft „die Kaskade hat genau einen Ort"). Verschiebung nach `freigegeben/`
abgeschlossen (`68c2c4f`). Details im Commit und im Kopf-Callout der Spec.

## Vorab-Verifikationen (vor jeder Code-Aenderung, 2026-09-10)

### A — Wird die Wurzel materialisiert? **JA — aber es ist eine Daten-, keine Code-Eigenschaft**

Beweiskette:
1. `FaMaterializationSyncService` filtert die Quellmenge mit **`.Where(n => n.SubFA != 0)`**
   (Zeile 79) und bildet `SubOrderNumber = n.SubFA.ToString()` (Zeile 172).
2. Die Wurzel ist der Knoten mit **`VaterFA == null`** (`FaHierarchyTreeBuilder` Zeile 76).
3. Am Testsystem existiert in `/ProductionOrders` — einer Liste ausschliesslich **materialisierter**
   `ProductionOrder`-Zeilen — eine Zeile mit Sub-FA-Nummer `1035235`, also der HauptFA-Nummer selbst
   (eigene Messung 2026-09-10, nicht aus einem Bildschirmfoto: DOM-Ablesung der sortierten
   Zellwerte).

⇒ Die Wurzel traegt `SubFA = eigene Nummer != 0` und ist materialisiert. **Punkt 2 der Spec ist
gebaubar**, ohne Anpassung der Materialisierung.

> [!warning] Einschraenkung, die in die UAT gehoert
> `SubFA` kommt **unveraendert aus der Sage-View** (`FaHierarchySyncService` Zeile 225,
> `SubFA = Int32(r, oSubFA)`). Dass die Wurzel `!= 0` traegt, ist damit eine **Eigenschaft der
> Daten**, nicht eine vom Code erzwungene Invariante. Kaeme je eine Struktur, deren Wurzel
> `SubFA = 0` hat, fiele sie **still** aus der Materialisierung — und die HauptFA-Zeile fehlte ohne
> Fehlermeldung. Gleiche Klasse wie der TreeBuilder-Quirk „`VaterFA == 0` ist weder Wurzel noch
> Waise" (Punkt 19 der [[2026-09-08-bom-bridge-nachlese]]).

### B — Zeilenebene von `Picking` und `OseonTracking`: **beide FA-Ebene, Scope bestaetigt**

| viewKey | Befund | Matchcode |
|---|---|---|
| `ProductionOrders`, `PickingLeitstand`, `FaCompletion`, `FaWorklist` | ADR-0005-Listen, FA-Zeilen | **ja** |
| `Picking` | FA-Ebene **bestaetigt** — die Kommissionierliste traegt `order-number`, `customer`, `picking-date`, `parent-sub-order-number`; es sind FA/Sub-FA-Zeilen, keine Komponenten | **ja** |
| `OseonTracking` | FA-Ebene, registrierter ViewConfig — **aber ein Baum** (`id="oseonTree"`), **keine** `filterable-table`, `SupportsReorder: false`, `SupportsSortDefault: false`, **eigener** Sortiermechanismus (`data-sortable`/`data-sort-key`, die Variante mit den vorberechneten Maps) | **ja, aber ueber den Baum-Pfad** — nicht ueber `table-filter.js` |

⇒ Die „sechs Listen" der Spec sind korrekt. **Zusatzaufwand:** `OseonTracking` ist mechanisch ein
anderer Pfad als die fuenf Tabellen; die Matchcode-Zelle dort ist kein Copy-Paste.

### Nebenbefunde (NICHT in dieser Spec beheben)

**N1 — `Tracking`/`TrackingByWorkplace` sind nicht registriert.**
`Views/Tracking/Index.cshtml` (Zeile 193) und `Views/Tracking/ByWorkplace.cshtml` tragen
`data-view-key="Tracking"` bzw. `"TrackingByWorkplace"`, aber **`ColumnDefinitions.GetByViewKey`
kennt beide nicht** (`_ => null`). Das ist genau die Drift-Klasse aus Punkt 23 der
[[2026-09-08-bom-bridge-nachlese]] — gehoert in deren Sweep, nicht hierher. Zusammen mit dem dort
schon notierten Befund **B-2** (stiller Abbruch der Sortierung bei nicht aufloesbarer Spalte) ist das
bereits der **dritte** Fall derselben Klasse.

**N2 — Zwei Zeitbasen in `ProductionOrderPickingStatus.ModifiedAt`.**
Verifiziert: `SetReleaseAsync` und `SetReleaseBatchAsync` stempeln **`DateTime.UtcNow`**,
`SetAssignedPickerAsync` und `SetIsDoneBdeForOrderNumberAsync` dagegen **`DateTime.Now`**. In
derselben Tabelle landen damit ueber verschiedene Methoden zwei verschiedene Zeitbasen — in
Oesterreich ein bis zwei Stunden Unterschied je nach Sommerzeit. Wer Audit-Zeitstempel einer Zeile
vergleicht oder nach Zeit sortiert, vergleicht Aepfel mit Birnen, **ohne dass etwas auffaellt**.
Wieder dieselbe Klasse: kein Fehler, der sich meldet.
*Dieser Lauf macht es nicht schlimmer* — die neue `SetReleaseForOrderNumberAsync` gehoert zur
Freigabe-Familie und nimmt `UtcNow` (Plan-Entscheidung D7). Die Bereinigung ist ein eigener,
kleiner Auftrag: erst entscheiden, welche Basis gilt (UTC, analog dem Rest der Anwendung pruefen),
dann alle Schreibpfade dieser Tabelle darauf ziehen — **mit** Blick auf bereits gespeicherte Werte.

**N3 — `CascadeDone` haengt an `CanPick`, die neue Kaskade an `CanManagePickingRelease`.**
Kein Fehler, sondern Absicht (Fertigmeldung ist ein Picking-Recht, Freigabe ein Leitstand-Recht) —
aber die beiden Knoepfe in derselben Gruppen-Kopfzeile haben damit **unterschiedliche**
Sichtbarkeitsbedingungen. Das ist fachlich richtig und sieht beim Testen nach einem Fehler aus:
Je nach Rolle sieht man einen, beide oder keinen Knopf. **Gehoert in die manuelle Checkliste**, sonst
wird es als Mangel gemeldet.

## Arbeitsstand

- [x] Schritt 0 — Spec nachgebessert, Anzeige-Spec nachgezogen
- [x] Vorab-Verifikation A (Wurzel materialisiert) + B (Zeilenebene Picking/OseonTracking)
- [ ] Plan schreiben
- [ ] Block 1 — Matchcode (Model, Migration SQL/91, Materialisierung, Anzeige 6 Listen)
- [ ] Block 2 — Kopfdaten je Zeile + Kunde-Postfilter
- [ ] Block 3 — HauptFA-Zeile + schlanke Kopfzeile
- [ ] Block 4 — Freigabe-Kaskade mit Pflicht-Picker
- [ ] Testszenarien + QA
