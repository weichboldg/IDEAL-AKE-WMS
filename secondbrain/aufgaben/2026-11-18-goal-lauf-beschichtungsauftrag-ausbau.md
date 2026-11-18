---
type: aufgabe
status: in_arbeit
created: 2026-11-18
spec: "[[2026-11-18-beschichtungsauftrag-ausbau-checkboxen-versand-druck-spec]]"
---
# /goal-Lauf 2026-11-18: Beschichtungsauftrag-Ausbau (Haken, freie Positionen, Versendet, Kreditor, Druck)

Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Start bei c39126c3 (v1.99.40). Nummern beim Start: v1.99.41, SQL/161, TS-162.
Freigabe: Antworten 1–17 (R1–R5 Gerald 11:48, 6–17 wie Empfehlung 12:55).
Grenzen: nichts an Sage schreiben, keine Einstellungen auf Test/Prod, `SageFaArbeitsgaengeAnlegenAktiv` bleibt aus, Publish nur im Worktree (Paket 6), Pakete 1–5 unverändert, kein Merge, kein Push.

| Spec | Typ | SQL | Version | Status | Commits | Blocker |
|---|---|---|---|---|---|---|
| [[2026-11-18-beschichtungsauftrag-ausbau-checkboxen-versand-druck-spec]] | Epic (E1–E6) | 161 | 1.99.41 | InUmsetzung | | P-1/P-2/P-3 (Mensch) |

## Plan (kompakt)
- Kein neuer Konstruktor-Parameter am Workflow: Artikel- und Kreditor-Lesen über `IBeschichtungsauftragRepository` (ADR 0001).
- `AnlegenAsync(hauptFa, AnlageAuswahl)`; `AnlegenAsync(hauptFa)` bleibt als „alle freien“ (Tests).
- E1 Schema/Status → E2 Bearbeiten bis vor Versendet + freie Positionen → E3 Vorschlag mit Haken → E4 Kreditor → E5 Druck → E6 Doku. Je Etappe ein Commit.

## Protokoll
- Start: Spec freigegeben, Vorprüfung ok (alle 17 Antworten, keine Blocker). Status InUmsetzung.
