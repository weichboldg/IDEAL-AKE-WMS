---
type: aufgabe
status: offen
created: 2026-11-16
quelle: Gerald, Chat 2026-11-16 (/goal + Dev-Lauf-Auftrag)
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# /goal-Lauf 2026-11-16: Beschichtungslogik, FA-Struktur über SData, Arbeitsgang-Mapping

Auftrag Gerald 2026-11-16: drei freigegebene Specs nacheinander im Bündel-Worktree (Basis v1.99.23, `8bfd8c73`). Specs liegen unter
`specs/entwurf/` mit `status: Freigegeben` – laut Auftrag gelten sie als freigegeben. Rumpf = Auftrag, Nachträge gehen vor.
Grenzen: nichts an Sage schreiben, keine Einstellungen auf Test/Prod, kein Merge, kein Push. Publish erst am Ende.

Nummern beim Start geprüft: Version 1.99.23 → 1.99.24 bleibt für [[2026-11-14-sicherheit-anmeldung-und-installer-spec]] reserviert;
letzte SQL 150 (151 reserviert Sicherheit, 152 frei/ungenutzt); letzte TS-138 (139 reserviert); letzter ADR 0026 (0027 reserviert).

## Übersicht

| Spec | Typ | SQL | Version | Status | Commits | Blocker |
|---|---|---|---|---|---|---|
| [[2026-11-16-beschichtungslogik-je-standort-spec]] | Epic | – | 1.99.25 | InUmsetzung | | E2, E4: H1 P-I1 (Gerald) fehlt · E5: H2 (P-A5, P-A3, P-A6) und H3 (Mengen-Vergleich) fehlen; Rückfragen 19/20 offen |
| [[2026-11-16-fa-struktur-ideal-ueber-sdata-spec]] | Epic | – | 1.99.26 | wartet | | Umschalten auf SData gesperrt bis DPS-Antwort/P-FS3 und Rückfragen 4/5 (Freigabe-Antwort 1) |
| [[2026-11-16-arbeitsgang-mapping-sage-arbeitsgangnummer-spec]] | Spec | – | 1.99.27 | **Blocker, nicht begonnen** | – | P-AG1..5 (Gerald) nicht in der Spec eingetragen (Freigabe-Antworten 1/2) |

## Etappen-Stand

Beschichtungslogik: E1 · E3 · E6 (für gebaute Etappen) bauen; E2/E4/E5 Blocker.
FA-Struktur SData: E1, E2, E3, E4, E5 (Vergleich/lesend, Umschalten gesperrt), E6.

## Fragen an Gerald

- P-I1 (IDEAL Schalterstand) eintragen → gibt E2/E4 Beschichtung frei.
- P-A3, P-A5, P-A6 und H3 (Mengen-Vergleich; Aussage 16.11. „B-1 sollte passen“ ist eingetragen, Prüfschritt bleibt) → gibt E5 frei.
- Rückfragen 19/20 Beschichtung beantworten.
- P-AG1..5 eintragen → Arbeitsgang-Mapping startbar.

## Protokoll

- 2026-11-16: Lauf gestartet, Vorprüfung durchgeführt (siehe Übersicht).
