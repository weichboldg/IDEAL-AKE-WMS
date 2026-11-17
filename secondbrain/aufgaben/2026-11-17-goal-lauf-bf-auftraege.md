---
type: aufgabe
status: offen
created: 2026-11-17
quelle: Gerald, Chat 2026-11-17 (/goal BF-Aufträge Blechfertigung)
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# /goal-Lauf 2026-11-17: Blechfertigung BF-Aufträge (Warenkorb, Schachtler, CSV an WiCAM)

Auftrag Gerald 2026-11-17: [[2026-11-13-blechfertigung-bf-auftraege-wicam-csv-spec]] im Bündel-Worktree umsetzen (Basis v1.99.34, `eff0c1a4`).
Freigabe-Antworten 1–13 (Gerald 16:33/16:34) und Prüfergebnisse P-2..P-6 gelten. Stufe 1 ohne Sage-Schreiben; Datenquelle V-B; Materialnummer
aus `USER_OSMaterialnummerPN` (fehlt sie: Hinweis im Warenkorb, Position nicht übergeben); CSV-Aufbau vorläufig nach Excel-Vorlage (P-1 offen,
als Testvorlage-Platzhalter gekennzeichnet); Trenner/ANSI/Datumsformate fest; Rolle `schachtler`.
Grenzen: nichts an Sage, keine Einstellungen auf Test/Prod, keine Datei in einen echten WiCAM-Pool (DEV-Pfad lokal), Sage-View nicht ändern
(View-Block 3 macht Gerald), kein Merge, kein Push.

Nummern beim Start geprüft: Version 1.99.34 → **1.99.35**; SQL letzte 157 (156 Wareneingang, 157 PositionText) → **SQL/158**; TS letzte 156 →
**TS-157** (TS-157 stand nur als alter Vorschlag in der Wareneingang-Spec, dort TS-156 gebaut).

## Übersicht

| Spec | Typ | SQL | Version | Status | Commits | Blocker |
|---|---|---|---|---|---|---|
| [[2026-11-13-blechfertigung-bf-auftraege-wicam-csv-spec]] | Spec | 158 | 1.99.35 | InUmsetzung | `09063984`, `ea21bddd`, `37e83d85`, `89ce487a`, `9f2a0802` | P-1 (CSV-Muster), P-7 (Schreibrecht Pool); View-Block 3 (Gerald) Vorbedingung Warenkorb-Test |

## Protokoll

- 2026-11-17: Lauf gestartet, Spec auf InUmsetzung, Umsetzung im Worktree beauftragt.
- 2026-11-17: Commit A (V-B Datenquelle) `09063984` – Build grün, Tests 3132+706 grün.
- 2026-11-17: Commit B (Modell, Migration SQL/158, Web Warenkorb/Liste/Details/Freigabe, Rolle, Mail) `ea21bddd`.
- 2026-11-17: Commit C (CSV-Dienst `BfCsvService`, 9 ServiceSettings, Byte-Vorlage) `37e83d85` + `89ce487a` (`.gitattributes` binary für die
  Vorlage) – Tests 3197 + 714 grün.
- 2026-11-17: Commit D (Fertig einzeln/Liste, Storno, Version 1.99.35, Hilfe, Changelog, README, docs/anwender 14, docs/betrieb, TS-157)
  `9f2a0802` – Build 0 Fehler, Tests 3208 (1 übersprungen) + 714 grün. **feature-complete**; offen: qa-agent, P-1, P-7, View-Block 3 (Gerald).
