---
type: aufgabe
status: in-arbeit
created: 2026-11-25
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# /goal-Lauf 2026-11-25: Kommissionier-Nachtrag, Lagerplatz-Anlage Sage, BDE Isolierfräse, BDE-Masken-Epic

Auftrag Gerald 2026-11-25 (`/goal-run` mit vier Specs, Zielbedingung inkl. sauberer Tests + Definition of Done).
Reihenfolge des Auftrags. Start-Stand Worktree: v1.99.69 (eff16911), höchstes SQL 167 (Lücke 164/165 für die
Isolierfräse-Spec reserviert), höchstes TS 190, höchste ADR 0035, letztes Paket 19.

| Spec | Typ | SQL-Block | Version | TS | Status | Commits | Blocker |
|---|---|---|---|---|---|---|---|
| [[2026-11-25-kommissionierauftraege-nachtrag-nachts-spec]] | Spec | 168 | 1.99.70 | 191 | **Testbereit** | fa1b754b, Fotos+Publish 6e426113 | Nachtlauf auf DEV nicht beobachtbar (Q4) |
| [[2026-11-24-lagerplatz-anlage-wms-nach-sage-spec]] | Spec | – (ADR 0036) | 1.99.71 | 192 (statt 189, vergeben) | offen | | |
| [[2026-11-20-bde-liste-isolierungsfraese-pfad-material-spec]] | Spec | 164, 165 (reserviert) | 1.99.72 | 193 | offen | | |
| [[2026-11-25-bde-masken-vereinheitlichung-spec]] | Epic (1, 2, 3, 4a, 4b, 5) | 169 (statt 168) | 1.99.73–1.99.78 | 194–199 | offen | | |

ADR des Epics: 0037 (statt 0036, an die Lagerplatz-Spec vergeben).

## Vorprüfung
- Alle vier: status Freigegeben (Gerald 2026-11-25), worktree leer bzw. = Bündel, Freigabe-Antworten vollständig.
- Lagerplatz: Prüfbefunde eingearbeitet (b4b29db0), Sage-Test-SQL freigegeben (qa-agent.md ecc50041).
- Epic: B-1 entschieden (freigabe_entscheidung „Teile-Reiter nur mit BdeTeileliste“), S-1/S-3 nachgezogen (b474c4c5).
- Worktree sauber bei Start.

## Protokoll
- 2026-11-25 Start, Specs auf InUmsetzung.
- Kommissionier-Nachtrag v1.99.70: fa1b754b (Brain 639fbd0f), SQL/168 + Migration 20261125161741_AddPickingStatusStuecklisteKleiner, TS-191.1–191.16 + 191.11a, Build 0 Fehler/keine neuen Warnungen, Tests Web 3551/1 skip/0, Dienst 801/0. Befund: Tabelle heißt ProductionOrderPickingStatus (Spec korrigiert). Deploy-Hinweis: erster Dienststart legt offene Nachträge sofort an (Manifest-Info). QA läuft.
- v1.99.70 QA bestanden: DEV AKE+IDEAL v1.99.70, SQL/168 auf beiden DEV-DBs, Fotos 01-19/01-20/01-21 (6e426113). Befunde Q1–Q3 niedrig (belassen), Q4 mittel: DEV-Absicherung schaltet `Sync:KommissionierNachtragEnabled` (Muster `Sync:%Enabled`) ab → Nachtlauf auf DEV nicht beobachtbar, 191.1–191.7/191.9 NICHT MÖGLICH, bleibt Mensch. Prod-Default true = Sofortlauf beim ersten Dienststart. QA änderte kurz die eigene Rolle (UserRoles 40, DEV AKE) und setzte zurück.
