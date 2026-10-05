---
type: changelog
version: 1.35.0
date: 2026-09-07
---
# v1.35.0 — IDEAL: FA-Liste und verwandte Ansichten hierarchiefähig

Umsetzung der freigegebenen **Epic**-Spec [[2026-08-18-fa-liste-hierarchie-anzeige-spec]] (Etappen A–E)
im **Bündel-**Worktree `feature/2026-08-07-ideal-teile-1-5` (gemeinsam mit Teilen 1–8; kein
Zwischen-Merge, Schranke 2 fürs ganze Bündel). Umsetzungsnotiz [[2026-08-18-fa-liste-hierarchie-anzeige]].

**Dritte Fehlerklasse nach Teil 7:** Die Materialisierung (Teil 7) stellte 130 Sub-FAs als echte
`ProductionOrders` ins System — technisch sichtbar, aber unbedienbar (dieselbe `OrderNumber` mehrfach,
`SubOrderNumber`/Elternzeiger nirgends, Suche liefert die ganze Gruppe). Diese Spec macht die Hierarchie
in den sechs betroffenen Ansichten **sichtbar und bedienbar** — ohne die Arbeitslisten in einen
Struktur-Browser zu verwandeln (der bleibt `/FaHierarchy`). **Nur bei Master
`ProduktionsauftragHierarchisch = true`; bei `false` bit-identisch zu AKE.**

## Umgesetzt (Etappen A–E)

- **A — Anzeige-Fundament + ProductionOrders-Referenz** (`30b7a8c`/`9315186`/`abd2620`): gruppierte,
  master-gated Repo-Abfrage `GetForLeitstandGroupedAsync` (Gruppe = `OrderNumber`, Pagination über
  Gruppen, Z2 SubOrderNumber, Z3b showDone), `LeitstandOrderRow` +Sub/Parent/SageMissingSince;
  `ColumnDefinitions` +`parent-sub-order-number` (DefaultHidden); neues JS-Modul
  `fa-liste-gruppierung.js` (Chevron-Collapse + Auto-Expand F2, kein Eingriff in `table-filter.js`);
  ProductionOrders als Referenz-View (`_ProductionOrderRow.cshtml`, Z4-Zeile, SageMissingSince-Badge).
- **B — 5 weitere Views** (`b150579` FaCompletion · `5b15caf` PickingLeitstand · `960d5c8` Picking ·
  `10921e2` FaWorklist · `7bba52b` Tracking/Index): je eigenes Row-Partial + Master-Gate. **Befunde:**
  Picking/FaCompletion/FaWorklist sind ProductionOrder-abgeleitet (In-Memory-GroupBy, Z2), nicht das
  Repo-grouped-Muster; **Picking** ist eine Prioritäts-Warteschlange → Gruppen-Reihenfolge dringlichste
  zuerst, innerhalb Prio (bewusst nicht Z2); **PickingLeitstand** Bulk-Select über mehrere `<tbody>`
  gehärtet (eingeklappte Gruppen werden abgewählt, neues Event `fa-liste-group-toggled`);
  **Tracking/Index** ist real 3-Ebenen (HauptFA → Sub-FA → AG) → OSEON-Baum-Muster übernommen, eager
  (lokale Daten, kein Lazy nötig), zweistufiger Fortschritt.
- **C — Kaskade „Alle Sub-FAs fertigmelden"** (`a305d6f`): auf der PickingLeitstand-Gruppenkopfzeile.
  Träger **`IsDoneBde`** (`ProductionOrderBdeStatus`), **nur diese Aktion kaskadiert** (BL1-Ausgang der
  Spec). Setzt IsDoneBde für ALLE ProductionOrders der HauptFA-`OrderNumber` in **einem SaveChanges**
  (atomar, gemeinsamer Zeitstempel), **ein** `ILogger`-Eintrag je Kaskade. Bestätigungsdialog nennt
  Gesamtzahl + „mit offener Buchung" (jede `EndedAt==null && !IsCancelled`, inkl. Paused/AutoPaused).
  **Nur `true`, keine Gruppen-Rücknahme**; Zeilen unverändert.
- **D — Z4-Sweep + Z1-Test** (`d9de60c`): Erhebung bestätigt — die 6 Listen tragen die Z4-Zeile aus A/B,
  Home-Kacheln zählen nicht; einzige Nicht-Listen-Stelle `Articles/Info` → „N Aufträge · M Sub-FAs offen".
  Z1-Regressionstest (Service): Sync lässt `IsDoneBde`/`IsDone`/`IsDonePicking` unberührt. **Z3
  vertagt** (Mensch, 2026-09-07) → Backlog [[2026-09-07-invariante-haupt-fertig-sub-erkennen]]
  (Deploy-Fork Service; Fall ausgeschlossen; „offen anlegen" ohnehin erfüllt).
- **E — Testszenarien + Version + Changelogs** (`dde9a17`): TS-69 (69.1–69.20) in
  `docs/TESTSZENARIEN.md`; Version 1.34.0 → **1.35.0** (Web+Service); Anwender-Changelog.

## Migration / Deploy

- **Migration: keine.** Alle Felder (`SubOrderNumber`/`ParentSubOrderNumber`/`SageMissingSince`/
  `IsDoneBde`) existieren aus Teil 7; `ColumnDefinitions`-Erweiterung ist `viewKey`-agnostisch.
- **Deploy: `web: true`, `service: false`, `migration: false`.** Der Service ist nur test-seitig berührt
  (Z1-Regressionstest); der Z3-Sync-Log wurde vertagt, damit `service:false` bleibt.
- **Kontext:** Teil des noch nicht gemergten Bündels (Teile 1–8 + diese Spec) — Schranke 2 für alles
  zusammen, ein Merge.

## Tests

Web-Suite **1243 grün**, Service-Suite **232 grün**. Neue Tests: gruppierte Repo-Abfrage (3),
Controller-Hierarchie je View (~2 je View), Kaskade-Repo (3) + Offene-Buchung-Zählung (1) +
Kaskade-Controller (4), Z4 Articles/Info (1), Z1-Sync-Invariante (1). Der manuelle Rest ist die
UI-/Bedien-Sichtprüfung am laufenden IDEAL-System (TS-69, „Kapitel mit besonderem Gewicht").

## Merge-Commit

_(offen — Schranke 2, Mensch; Bündel Teile 1–8 + FA-Liste-Hierarchie in einem Merge)_
