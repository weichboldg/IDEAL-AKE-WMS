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
- [x] Plan geschrieben — 16 Tasks, 4 Bloecke, Worktree-Commit `940bab2`
- [x] **Block 1 — Matchcode KOMPLETT** (Tasks 1-4, Stand `4a0563a`)
  - Task 1 `461d3e5`: Spalte `NVARCHAR(200)` + Migration `20260910081155` + `SQL/91` + FreshInstall (beide Stellen)
  - Task 2 `cbabdd9`: Materialisierung in Anlege- UND Update-Pfad (F1), 2 Tests
  - Task 3 `f4e9a6d`: Anzeige in **fuenf** Listen (nicht sechs — `OseonTracking` faellt raus, siehe A1)
  - Task 4 `48e1c87` + Fix `4a0563a`: serverseitiger Filter null-sicher (F7), Negation + Komma-OR getestet
  - Nachweis: Web 1288 gruen + 1 uebersprungen, Service 265 gruen, Build 0 Fehler
- [x] **Block 2 — Kopfdaten je Zeile KOMPLETT** (Tasks 5-8, Stand `9c95fb0`)
  - Task 5 `9b2a258`+`13693b7`: K1-Felder je Zeile, AKE unveraendert. Fix: `??`-Fallback bei `Customer`
    entfernt — die Leere bei Kombigeraeten hing an einer **Datenannahme** statt am Code.
  - Task 6 `6ec37f8`+`cddb5b0`: C#-Postfilter. Fix: **Critical** — der Postfilter brach den
    Kunde-Spaltenfilter im **Flachmodus** (AKE) und schaltete dort die Paginierung ab.
  - Task 7 `a2c7861`+`20686a2`+`7b15d8a`: Freitext-Kunde ueber denselben Postfilter. Der geplante
    Repository-Umbau entfiel — Task 6 hatte das Ziel sicherer erreicht.
  - Task 8 `9c95fb0`: vier Spalten, **bedingt auf `Model.Hierarchical`** (AK 20), `colCount` je Modus.
- [x] **Block 3 — KOMPLETT** (Tasks 9-11 + eingeschobener Sweep, Stand `fda7c7a`)
  - Task 9 `0f14850`: `table-sorted`-Hook **und Befund B-2 behoben** — ein Griff in die gemeinsam
    genutzte Datei, ein Regressionsnachweis, wie vorgegeben.
  - **Eingeschobener Sweep** `b9bf342`+`94463f9`+`9dfce86`: Selektor-Skopierung ueber die Fehlerklasse.
    Fand einen **zweiten, persistenten** Fehler, den niemand kannte → [[2026-09-10-table-filter-selektoren-nicht-skopiert-bug]]
  - Task 10 `203e23a`+`a422e44`: Wiederholungswerte unterdrueckt. Fix: **Critical** — die CSS-Regel
    verlor gegen Bootstrap, das Feature war am Bildschirm ein No-op.
  - Task 11 `fda7c7a`: HauptFA-Zeile gekennzeichnet, Kopfzeile auf Kunde + Leittermin verschlankt.
  - Nachweis: Web **1309 erfolgreich + 1 uebersprungen**, Service 265, Build 0 Fehler.
- [ ] **Block 4 — Freigabe-Kaskade mit Pflicht-Picker (Tasks 12-14) — HALT, wartet auf Freigabe**
  > [!important] Bewusster Halt des Menschen (2026-09-10)
  > Die Kaskade gibt mit **einem Klick bis zu 39 Auftraege** frei und weist einen Kommissionierer zu —
  > das einzige Stueck dieses Blocks **mit Wirkung in der Halle**. Es soll nicht am Ende einer langen
  > Sitzung entstehen.
- [ ] Version/Changelog/TS-Index + QA (Task 15/16) — **Auflage:** die belastbaren Zahlen in den
      Brain-Changelog (Web 1309+1 bzw. der Stand bei Abschluss, Service 265, Build 0 Fehler)

### Umfangs-Korrektur in Block 1 (am Code belegt)

Der Plan nahm an, alle fuenf Listen teilen sich die Leseschicht
`LeitstandOrderRow → MapItem → ProductionOrderListItem`. **Stimmt nur fuer `ProductionOrders` und
`PickingLeitstand`.** `FaCompletion`, `FaWorklist` und `Picking` haben je eigene `*ListItem`-Klassen,
eigene Controller-Mappings und eigene In-Memory-Spaltenfilter-Maps. Der Umsetzer hat das gemeldet statt
geraten — ohne die Erweiterung haette es nicht kompiliert. Folge fuer kuenftige Spalten in diesen
Listen: **fuenf Pipelines pflegen, nicht eine.**

### Nachtrag zu den Nebenbefunden

**N4 — vierter Fall der `#column-config`-Drift-Klasse.** `FaCompletion` hat im inline
`#column-config` eine `workbench`-Spalte, die in `ColumnDefinitions.FaCompletion` fehlt.
Vorbestehend. Damit sind **vier** Faelle belegt (`warehouse-order`, B-2, N1, N4) — das stuetzt den
geplanten Drift-Guard-Test (Punkt 23 der [[2026-09-08-bom-bridge-nachlese]]) deutlich besser als die
bisherige Vermutung „`warehouse-order` ist vermutlich nicht der einzige".

**N5 — die Begruendung der „kein `EF.Functions.Like`"-Regel wackelt.** Die Regel stammt aus der
Materialisierungs-/Listen-Praxis mit der Begruendung „InMemory uebersetzt `Like` nicht". Beim Bauen des
Matchcode-Filters kam heraus: Ein bestehender Like-Pfad (hierarchischer Freitext-Kundenfilter) laeuft
empirisch **auch unter InMemory** durch. Die Regel wurde hier bewusst trotzdem eingehalten, aber ihre
Begruendung ist nicht lueckenlos. Gehoert in [[fallstricke]] geklaert — eine Regel, deren
Rechtfertigung nicht stimmt, wird irgendwann aus dem falschen Grund gebrochen.
