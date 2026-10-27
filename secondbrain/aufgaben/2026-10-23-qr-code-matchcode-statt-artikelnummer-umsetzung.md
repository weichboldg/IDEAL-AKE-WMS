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

## Offen nach UAT

- An IDEAL nach TS-101 `QrErstesFeld` = `Matchcode` stellen, AKE bleibt `Artikelnummer`.
- Backlog-Punkt Bestandsübersicht (FA-Übernahme hart an, Antwort 4) ist eigene Notiz.
