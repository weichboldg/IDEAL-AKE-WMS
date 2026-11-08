---
typ: notiz
spec: "[[2026-11-07-oseon-sync-neu-ausgeschriebener-auftrag-spec]]"
bug: "[[2026-11-07-oseon-sync-neu-ausgeschriebener-auftrag-bug]]"
status: Erledigt
worktree: ""
branch: main
version: "1.30.3"
---
# Umsetzung: AKE-Hotfix v1.30.3 — OSEON-Sync neu ausgeschriebener Auftrag + Z1–Z3

Spec [[2026-11-07-oseon-sync-neu-ausgeschriebener-auftrag-spec]] · **direkt im Hauptcheckout auf `main`**
(Freigabe-Nachtrag 3, Gerald 2026-11-07), kein Worktree. Keine Migration, kein SQL.

## Stand

- [x] Vorbedingung: Zweig `main`. Arbeitsverzeichnis war nicht sauber (nur `secondbrain/` + `.claude/`/`CLAUDE.md`);
  auf Geralds Anweisung die offenen `secondbrain/`-Änderungen vorab committet (`36a7ed96`). `.claude/` und
  `CLAUDE.md` bleiben uncommittet und werden vom Hotfix nicht berührt.
- [x] OSEON: `OseonOrderSelection` (neu, Helper + `OseonRawRow` als `internal sealed`), MERGE über
  `OseonOrderNumber`, `OseonId` im UPDATE, `OUTPUT` alt/neu, SQL-Wächter `ROW_NUMBER` in beiden MERGE-Quellen,
  Counts + Warnzeilen.
- [x] Z1 `Views/Picking/PrintBom.cshtml` `@page` oben 28 mm (vorher 8 mm).
- [x] Z2 `PickingController.RenderPrintBomAsync` sortiert nach Reihenfolge der `visiblePositions`;
  `Views/Picking/Bom.cshtml` schickt die Liste auch bei aktiver Sortierung.
- [x] Z3 `Views/Picking/Index.cshtml` + `PickingListItem.WorkplaceName` + `ColumnMap["workbench"]`.
- [x] Version 1.30.2 (Web + Service), Anwender-Changelog, `docs/TESTSZENARIEN.md` Kapitel 107.
- [x] Build + Tests grün.
- [x] qa-agent Lauf 1: NICHT Testbereit (B1 behoben, B2 → Zahnrad zuerst mergen, dann v1.30.3).
- [x] WIP-Commit Hotfix-Code `066b136f` auf main (Gerald: Weg 2, damit der Zahnrad-Merge möglich ist).
- [x] Zahnrad-Hotfix in main gemergt (`e386bfce`, Freigabe Gerald 2026-11-08), Konflikte gelöst → v1.30.3, Build + Tests grün.
- [x] qa-agent Lauf 2 (erhöhte Shell). Ersatznachweis von Gerald akzeptiert (2026-11-08) → Testbereit. 2026-11-08 gelaufen: Code ohne Befund, Browser-Ersatzlauf OK, aber DEV-Deploy AKE
  gescheitert (`AKE_WMS_DEV` auf Bündel-Schema, U1/U2), daher nicht Testbereit; siehe Spec „QA Lauf 2“.
- [x] (`1c7fc333`, Freigabe Gerald 2026-11-08) Commit erst nach Rückfrage bei Gerald (Auftrag: „Kein Commit ohne Rückfrage").

## Folgearbeit

- Bündel `feature/2026-08-07-ideal-teile-1-5` holt den Fix über `scripts/sync-worktree.ps1` (Mensch). Konfliktstellen
  dort erwartbar: `AppVersion.cs`, `Changelog.cshtml`, `docs/TESTSZENARIEN.md` (Kapitel 107 existiert dort evtl. schon
  als Platzhalter), `Views/Picking/Index.cshtml` (Bündel hat dort Spaltenauswahl-Umbau), `Bom.cshtml`/`PrintBom.cshtml`.
- Prüfabfrage IDEAL-Testinstanz (Ergebnis Gerald): siehe Spec, QA-Abschnitt.
