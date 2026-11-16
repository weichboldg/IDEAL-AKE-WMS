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
| [[2026-11-16-beschichtungslogik-je-standort-spec]] | Epic | – | 1.99.25 | InUmsetzung (QA Teilumfang E1/E3/E6 grün) | `c61b7a52` E1, `318abf1f` E3, `4a635534` E6 Teil, `3f247f11` QA-Befunde | E2, E4: H1 P-I1 (Gerald) fehlt · E5: H2 (P-A5, P-A3, P-A6) und H3 (Mengen-Vergleich) fehlen; Rückfragen 19/20 offen |
| [[2026-11-16-fa-struktur-ideal-ueber-sdata-spec]] | Epic | – | 1.99.26 | **Testbereit** (qa-agent) | `21b3fb65` E1, `28dfdc4c` E2, `a3214677` E3, `24ad7354` E4, `9fa447f1` E5, `5163df17` E6 | Umschalten auf SData gesperrt bis DPS-Antwort/P-FS3 und Rückfragen 4/5 (Freigabe-Antwort 1) |
| [[2026-11-16-arbeitsgang-mapping-sage-arbeitsgangnummer-spec]] | Spec | – | 1.99.27 | **Blocker, nicht begonnen** | – | P-AG1..5 (Gerald) nicht in der Spec eingetragen (Freigabe-Antworten 1/2) |

## Etappen-Stand

Beschichtungslogik: E1 · E3 · E6 (für gebaute Etappen) bauen; E2/E4/E5 Blocker.
FA-Struktur SData: E1, E2, E3, E4, E5 (Vergleich/lesend, Umschalten gesperrt), E6.

## Fragen an Gerald

- P-I1 (IDEAL Schalterstand) eintragen → gibt E2/E4 Beschichtung frei.
- P-A3, P-A5, P-A6 und H3 (Mengen-Vergleich; Aussage 16.11. „B-1 sollte passen“ ist eingetragen, Prüfschritt bleibt) → gibt E5 frei.
- Rückfragen 19/20 Beschichtung beantworten.
- P-AG1..5 eintragen → Arbeitsgang-Mapping startbar.
- FA-Struktur SData: P-FS1..8 und DPS-Antwort (P-FS3) → erst dann Sperre lösen; dabei QA-Befund Q1 beachten (beim Entsperren
  RealTimeData-Regel mit lockern bzw. Hinweis zur Warnung machen, `SdataFaStrukturReader.cs:171-175`).
- FA-Struktur SData manuell: TS-146/147 mit IDEAL-Daten (1069595, 1059463, 1065143, 80141025) auf einem System mit IDEAL-Sage; TS-148/149 im
  Vergleich auf dem Testsystem (Laufzeit, P-FS5-Stichprobe); TS-150.2/150.3. IDEAL DEV zeigt SData auf den AKE-Testmandanten.
- Schranke 2: Manual-UAT + Merge des Bündels (Beschichtung bleibt InUmsetzung bis H1–H3).

## Protokoll

- 2026-11-16: Lauf gestartet, Vorprüfung durchgeführt (siehe Übersicht).
- 2026-11-16: Beschichtung E1/E3/E6 (Teil) gebaut; QA Teilumfang grün auf DEV AKE+IDEAL (TS-140, TS-144; Dienst-Schritte ohne Sync-Gates
  nicht möglich), Status bleibt InUmsetzung. Befunde Q-1..Q-6 behoben (`3f247f11`), Tests Web 2913/0/1, Dienst 631/0/0. Brain: ADR 0028,
  fallstricke §39, Changelog v1.99.25, feature-map, Glossar, Codekarte.
- 2026-11-16: FA-Struktur SData gestartet (E1–E6, Vergleich only, Umschalten gesperrt).
- 2026-11-16: FA-Struktur SData E1–E6 gebaut (`21b3fb65`…`5163df17`), Tests Web 2920/0/1, Dienst 652/0/0. QA: **Testbereit**; DEV AKE+IDEAL
  v1.99.26, nur GET-Lesezugriffe, keine Einstellung geändert; Befunde Q1–Q5 niedrig/Info, nicht behoben. Brain: ADR 0029, fallstricke §40,
  Changelog v1.99.26, Glossar, Codekarte, G11-Nachtrag (`fd9e2f68`).
- 2026-11-16: Arbeitsgang-Mapping nicht begonnen (Blocker P-AG1..5).
- 2026-11-16: **Publish** letzter Stand `<Worktree>\publish\` – v1.99.26, Commit `5163df17`, Web + Dienst, keine Migration. Kein Merge, kein Push,
  nichts an Sage geschrieben.

