---
typ: notiz
spec: "[[2026-10-23-qr-code-matchcode-statt-artikelnummer-spec]]"
status: Testbereit
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# Umsetzung: QR-Code Matchcode statt Artikelnummer

Spec [[2026-10-23-qr-code-matchcode-statt-artikelnummer-spec]], direkt im Bündel. Start `a9462036` (v1.67.2), Ziel **v1.68.0**, **TS-101**.
Keine Migration. Deploy: Web (Dienst nur Version).

## Plan (kompakt)

1. Einstellung `QrErstesFeld`: Key, Seed `Program.cs`, `/Settings`-Gruppe, Wertprüfung `SaveSettings`, body-Attribut im Layout.
2. `GetByMatchcodeExactAsync(matchcode, limit)` → Liste (Trim, ToUpper, DUMMY aus); Endpunkt `by-matchcode`; Artikelinfo „mehrdeutig“.
3. `barcode-scanner.js`: Matchcode-Zweig vor dem bisherigen Wrapper-Rumpf (`_processScannedValueSync`), Meldungen, `nichtGefunden` erhalten.
4. Tests (Repository, Controller, API, Settings, Quelltext-Wächter), Hilfe, README, TS-101, Changelog, v1.68.0.

## Stand

- 2026-10-27: umgesetzt, Commit `ca48b22d` (wip feature-complete).
- 2026-10-27: **Testbereit** (qa-agent): Build 0 Fehler, Web 2195 bestanden / 1 übersprungen, Dienst 328, keine Befunde mittel/hoch
  (Q1–Q6 niedrig/Doku, siehe Spec „QA-Nachweis v1.68.0“). Wartet auf Schranke 2 (Manual-UAT TS-101 + Bündel-Merge).

- 2026-10-27: v1.69.0 (Freigabe-Nachtrag 2: Auswahllisten `AppSettingKeys.FesteWerte`, `QrTrennzeichen`), Commit `2c1e3a6d`.
  **Testbereit** (qa-agent): Build 0 Fehler, Web 2201 / 1 übersprungen, Dienst 328, Node 57; Q7–Q12 niedrig/Prozess.

## Offen nach UAT

- An IDEAL nach TS-101 `QrErstesFeld` = `Matchcode` stellen, AKE bleibt `Artikelnummer`.
- `QrTrennzeichen` = `,` erst nach der Prüfabfrage „Lagerplatz-Codes mit Komma“ (TS-101, QA Q9).
- Backlog-Punkt Bestandsübersicht (FA-Übernahme hart an, Antwort 4) ist eigene Notiz.
