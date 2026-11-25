---
type: aufgabe
title: Vorgemerkt für den nächsten goal-run
status: NEU
spec: "[[2026-11-25-kommissionierauftraege-nachtrag-nachts-spec]]"
specs:
  - "[[2026-11-25-kommissionierauftraege-nachtrag-nachts-spec]]"
  - "[[2026-11-24-lagerplatz-anlage-wms-nach-sage-spec]]"
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

Start (interaktiv, `/goal` vorher setzen): `/goal-run secondbrain/specs/freigegeben/2026-11-25-kommissionierauftraege-nachtrag-nachts-spec.md secondbrain/specs/freigegeben/2026-11-24-lagerplatz-anlage-wms-nach-sage-spec.md`
Worktree: `.claude/worktrees/2026-08-07-ideal-teile-1-5`.

Hintergrund: AKE-Prod-Update v1.99.63 / Hotfix v1.99.64 ([[2026-11-24-v1-99-64-sdata-group-by-ake-prod]]), Leitstand-Knopf
„Fehlende Kommissionieraufträge erzeugen“ einmalig 39 Aufträge / 6 180 Zeilen (2026-11-24 17:16); 4 FAs mit „Altbestand prüfen“
(2643162, 2643163, 2644100, 2644222) warten auf den Leitstand.

## Verlauf
- 2026-11-25 vorgemerkt (Claude).
- 2026-11-25 Lagerplatz-Anlage WMS→Sage freigegeben und vorgemerkt (Claude).
