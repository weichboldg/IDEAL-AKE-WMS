---
type: uebersicht
title: "Kommissionierauftrag: Bugfix Filter + Nachbesserungen Maske „Liste“ (nach QA v1.99.89)"
status: Entwurf
created: 2026-11-26
updated: 2026-11-26
source_backlog: "[[2026-11-26-kommissionierauftrag-liste-nachbesserungen]]"
standort: beide
teile:
  - "[[2026-11-26-kommissionierauftrag-filter-springt-auf-bom-spec]]"
  - "[[2026-11-26-kommissionierauftrag-liste-nachbesserungen-spec]]"
open_questions: []
---

# Übersicht: Kommissionierauftrag – Bugfix Filter und Nachbesserungen „Liste“

Quellen: [[2026-11-26-kommissionierauftrag-filter-springt-auf-bom]] und [[2026-11-26-kommissionierauftrag-liste-nachbesserungen]],
beide aus der QA von [[2026-11-26-beschichtung-summe-leitstand-hauptfa-kommissionierauftrag-liste-teil-3-spec]]. Codestand der
Fundstellen: Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5` (AppVersion 1.99.89).

## Reihenfolge

| # | Spec | Zweck (eine Zeile) | Standort | Version / TS (Vorschlag) | SQL |
|---|---|---|---|---|---|
| 1 | [[2026-11-26-kommissionierauftrag-filter-springt-auf-bom-spec]] | Filtern/Zurücksetzen bleiben auf dem Kommissionierauftrag, auch ohne Umschalter (Bug [[2026-11-26-kommissionierauftrag-filter-springt-auf-bom-bug]]) | beide | v1.99.90 / TS-209 bzw. 210 | keine |
| 2 | [[2026-11-26-kommissionierauftrag-liste-nachbesserungen-spec]] (depends_on 1) | R-1 natürliche Mehrfachwerte, R-2 Scan ohne Groß/Klein, R-3 Warnung bei unmöglichem PDF, R-4 eine Regel „offen“, R-5 ohne `?? 0m`, „—“ statt NAN im Druck | beide | v1.99.91 / TS-209 | keine |

Beide ändern `Views/Picking/Bom.cshtml` und `PickingController.cs`; deshalb in einem Lauf nacheinander im Bündel-Worktree (Bugfix
zuerst, keine Rückfragen offen). Spec 2 wartet auf die Antworten 1–4; der Bugfix ist davon unabhängig und allein freigebbar.
Versionen und TS-Nummern sind Vorschläge zum Schreibzeitpunkt; der Dev-Lauf prüft beide erneut.

**SQL-Nummernblock:** keiner – beide Specs ohne Schema-, Daten- oder Einmal-Skript. Manifest: keine Einträge.
