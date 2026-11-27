---
type: aufgabe
title: Vorgemerkt für den nächsten goal-run
status: NEU
spec: "[[2026-11-25-kommissionierauftraege-nachtrag-nachts-spec]]"
specs:
  - "[[2026-11-25-kommissionierauftraege-nachtrag-nachts-spec]]"
  - "[[2026-11-24-lagerplatz-anlage-wms-nach-sage-spec]]"
  - "[[2026-11-20-bde-liste-isolierungsfraese-pfad-material-spec]]"
assignee: agent
blocked_by: []
created: 2026-11-25
updated: 2026-11-25
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# Vorgemerkt für den nächsten goal-run

Gerald 2026-11-25: „für den nächsten goal run vormerken“. Bündel-Worktree weiterverwenden (Stand nach
[[2026-11-25-goal-lauf-beschichtung-sage-buchungen]]: v1.99.69, SQL bis 167).

| Spec | Status | Hinweis |
|---|---|---|
| [[2026-11-25-kommissionierauftraege-nachtrag-nachts-spec]] | Freigegeben (Gerald 2026-11-25) | Migration (Entwurf `SQL/168`), TS-191, Version 1.99.70 vorläufig – Nummern beim Start neu prüfen. Web + Dienst. |
| [[2026-11-24-lagerplatz-anlage-wms-nach-sage-spec]] | Freigegeben (Gerald 2026-11-25) | Keine Migration, neue ADR 0036 (Nummer beim Start prüfen), TS-189, Web + Dienst. Plätze zuerst, dann Lager. Vor Abnahme: Gerald legt Testlager in `ake_TEST2026` an + GRANTs; QA darf Sage-Test-SQL (qa-agent.md, ecc50041), Schalter nur nach V3-Beleg. IDEAL-Prüfabfragen offen (erst vor Einschalten an IDEAL). |
| [[2026-11-20-bde-liste-isolierungsfraese-pfad-material-spec]] | Freigegeben (Gerald 2026-11-25, offene Punkte wie Rumpf-Vorschlag) | Fräsen-Pfad `Articles.CamPath` + Schalter `FraesenPfadAktiv` + Rolle `fraesenpfad_pflegen`, Blechdicke `USER_OSBlechdicke`, Lagerbestell-Zeilen in der Teileliste, IDEAL. Spec nennt SQL 164/165 und v1.99.49 – **überholt** (Bündel bei SQL 167/v1.99.69, 168 für den Nachtrag oben) → Nummern beim Start neu vergeben. Einmal-Skript Erstimport aus `WICAM-Pfade.xlsx` (Backlog-Anhang). Fachbereichs-Antworten (Walter 2/7/14/15–17, Andi 4/11, Benjamin Blechdicke-Format) können nachkommen. |

Start (interaktiv, `/goal` vorher setzen): `/goal-run secondbrain/specs/freigegeben/2026-11-25-kommissionierauftraege-nachtrag-nachts-spec.md secondbrain/specs/freigegeben/2026-11-24-lagerplatz-anlage-wms-nach-sage-spec.md secondbrain/specs/freigegeben/2026-11-20-bde-liste-isolierungsfraese-pfad-material-spec.md`
Worktree: `.claude/worktrees/2026-08-07-ideal-teile-1-5`.

Hintergrund: AKE-Prod-Update v1.99.63 / Hotfix v1.99.64 ([[2026-11-24-v1-99-64-sdata-group-by-ake-prod]]), Leitstand-Knopf
„Fehlende Kommissionieraufträge erzeugen“ einmalig 39 Aufträge / 6 180 Zeilen (2026-11-24 17:16); 4 FAs mit „Altbestand prüfen“
(2643162, 2643163, 2644100, 2644222) warten auf den Leitstand.

## Verlauf
- 2026-11-25 vorgemerkt (Claude).
- 2026-11-25 Isolierungsfräse-Spec dazu vorgemerkt (Claude, auf Zuruf Gerald).
- 2026-11-25 Lagerplatz-Anlage WMS→Sage freigegeben und vorgemerkt (Claude).
