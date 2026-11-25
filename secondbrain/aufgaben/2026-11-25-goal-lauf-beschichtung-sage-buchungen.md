---
type: aufgabe
status: in-arbeit
created: 2026-11-25
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# /goal-Lauf 2026-11-25: Beschichtungsauftrag-Bugfix, Sage-Buchungen Gesamtsicht, Sage-Referenz

Auftrag Gerald 2026-11-25 („passt“): Reihenfolge Bugfix → Gesamtsicht → Referenz. Alle drei Freigegeben
(Gerald 2026-11-24 / 2026-11-25). Start-Stand Worktree: v1.99.64, höchstes SQL 166, höchstes TS 188.

| Spec | Typ | SQL-Block | Version | Status | Commits | Blocker |
|---|---|---|---|---|---|---|
| [[2026-11-25-beschichtungsauftrag-vorschlag-ohne-auswahl-spec]] | Spec (Bugfix) | – | 1.99.65 | **Testbereit** | 8ffa4c4a, QA 186e63dd | |
| [[2026-11-23-sage-buchungen-gesamtsicht-spec]] | Spec | – | 1.99.66 | feature-complete, QA läuft | 0725fc3e | |
| [[2026-11-24-sage-buchung-referenz-benutzer-abteilung-spec]] | Spec | 167 | 1.99.67 | offen | | |

## Vorprüfung
- Alle drei: status Freigegeben, worktree/branch = Bündel, Freigabe-Antworten vollständig, kein Epic.
- Worktree sauber (keine uncommitteten Änderungen) bei Start.

## Protokoll
- Bugfix v1.99.65: Umsetzung 8ffa4c4a, Build 0 Fehler, Tests Web 3504/1 skip/0 fail, Service 790/0. Abweichungen vom Rumpf wegen Antworten R-1/R-2 (keine harte Übergeben-Sperre, Entfernen in Übergeben für offene). Befund F1: „entfernen“ bei nur offenen Positionen → Übergeben mit 0 Positionen (erlaubt, Versendet sperrt) – Gerald fragen.
- Bugfix v1.99.65 QA bestanden (186e63dd): DEV AKE+IDEAL v1.99.65, Befunde B-1 (Positionsbezeichnung an IDEAL nicht eindeutig, VaterFA?) und B-2 (Fenster-Knopf bleibt bei Browser-Validierung gesperrt) niedrig, offen für Gerald.
- Gesamtsicht v1.99.66: Umsetzung 0725fc3e, Build 0 Fehler, Tests Web 3519/1 skip/0, Service 790/0. TS-189.1–189.14. Abweichungen: Einkauf sieht WE-Zeilen nur mit EinkaufAktiv (wie alte Liste); Herkunft-Filter ohne WE-Recht ignoriert; Foto 05-05 noch alt.
