---
type: changelog
version: 1.79.0
date: 2026-11-08
---
# v1.79.0 — Bereinigungen nach dem Bündel 2026-11-07

Spec [[2026-11-08-bereinigungen-nach-buendel-2026-11-07-spec]], Lauf [[2026-11-08-buendel-2026-11-08-goal-lauf]]. Commit `0af3a94e`.
**Keine Migration.** Daten-Skript `SQL/108_RemoveAppSettingsRueckmeldungOhneLeser.sql` (löscht zwei AppSettings-Zeilen ohne Leser). TS-111.

## Kernpunkte

- F1: Sperr- und TLS-Hinweis auf `/ServiceSettings` innerhalb der `.setting-row` ihres Felds (`ViewData["SperrGrund"]`, `["TlsWarnung"]` im Partial).
- F2: MasterData-Key ohne Zeile wird gegen die Stammdaten geprüft (`NegativeBuchungLagerplatz` = `NAN` fehlt → „ungültig (Standard, keine Zeile)“).
- F3: Worker-Tests warten auf ein Signal statt auf ein festes 150/200-ms-Fenster (`WorkerTestLauf`), 20/20 Läufe grün.
- F4: Altwerte durch einmaliges Speichern beider Masken je Standort normalisieren (README „Altwerte normalisieren“); kein Code.
- F5: `OseonRueckmeldungAktiv`/`SageRueckmeldungAktiv` aus Katalog, FreshInstall, README entfernt; `SQL/108` löscht die Zeilen.
- F6: `SageUebergabeStatus.NichtNoetig` zeigt „nicht nötig“ (gedämpft) statt leer; Spaltenfilter findet den Text.

## Deploy-Risiken

- `SQL/108` löscht Daten (ohne fachliche Wirkung); vorher P-4 notieren.
- F4: Schalter mit Lesart „nur `true`“ und Altwert `1` wirken heute **aus** – das Speichern schreibt `false` (Freigabe-Antwort 1:
  wirksamer Zustand). Liste der betroffenen Schalter je Standort im QA-Abschnitt der Spec.
