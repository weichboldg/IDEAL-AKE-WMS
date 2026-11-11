---
typ: notiz
spec: "[[2026-10-06-beschichtungsauftrag-ausbau-teil-3-spec]]"
status: Testbereit
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# Umsetzung: Beschichtungsauftrag-Ausbau Teil 3 — Beschichtungsauftrag als eigenes Objekt

Spec [[2026-10-06-beschichtungsauftrag-ausbau-teil-3-spec]] · **direkt im Bündel** (Entscheidung Gerald 2026-10-09),
Start `020bf8af` (v1.49.0). Backend ohne Oberfläche (Modell, Migration, Repository, Workflow, Tests). Deploy: Web +
Migration (SQL-Skript), kein Service.

## Stand

- 2026-10-12: Spec von `specs/entwurf/` nach `specs/freigegeben/` verschoben (Status stand schon auf Freigegeben),
  Umsetzung läuft.
- 2026-10-12: umgesetzt (Commits `1410fc5c`, `51c6bc79`, `a1f7fcfd`), erster QA-Lauf: I-1 (Speicherfehler in der
  Serializable-Transaktion → Commit wirft) + Minors; behoben in `a5b27550`; QA-Nachlauf ohne Critical/Important →
  **Testbereit**. Build + 1568 Web- / 272 Service-Tests grün. Changelog [[2026-10-12-v1-50-0-beschichtungsauftrag-objekt]].
- Schranke 2 mit dem Bündel: TS-83 (Prüfschritte Sage TS-83.1/83.2, SQL/94 + FreshInstall auf Test-DB, Parallelität mit Teil 4).

## Offen / Hinweise für Teil 4

- Ein Workflow-Aufruf je Request: nach einem gescheiterten Speichern verwirft der Workflow den ganzen Context
  (`ChangeTracker.Clear()`, QA M-1).
- Kandidaten-Regel steht zweimal (`GetHinzufuegbarePositionenAsync` und Validierung in `PositionenHinzufuegenAsync`) —
  heute deckungsgleich, bei Änderungen beide anfassen (QA M-7).
- Rolle „Beschichtungsaufträge" (Schreibrecht) + Filter + drei Pflichtstellen kommen mit Teil 4 (Anforderung 9).
