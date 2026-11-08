---
type: changelog
version: 1.30.3
date: 2026-11-08
---
# v1.30.3 — AKE-Hotfix: OSEON-Sync neu ausgeschriebener Auftrag + Stücklisten-Druck + Werkbank Kommissionierung

Umsetzung von [[2026-11-07-oseon-sync-neu-ausgeschriebener-auftrag-spec]] **direkt auf `main`** (Freigabe-Nachtrag 3),
Aufgabe [[2026-11-07-oseon-hotfix-main]]. Baut auf dem Zahnrad-Hotfix [[2026-10-08-v1-30-2-ake-hotfix-zahnrad-lagerbestellung]]
auf (Merge `e386bfce`, QA-Befund B2: Versionskollision 1.30.2 → dieser Hotfix wird 1.30.3). **Keine Migration, kein SQL.**

## Kernpunkte

- OSEON-Tracking: Auftrags-MERGE über `OseonOrderNumber` statt `OseonId`; schreibt OSEON einen Auftrag neu aus (neue ID,
  gleiche Nummer), wird der bestehende Datensatz aktualisiert, AGs bleiben (vorher Fehler 2601, Sync stand).
  Helper `OseonOrderSelection` (höchste ID je Nummer), SQL-Wächter `ROW_NUMBER` in beiden MERGE-Quellen, ältere ID wird nie
  zurückgeschrieben (QA-B1). Neue Counts `neu_ausgeschrieben`, `mehrfach_in_oseon`, `doppelt_nach_kollation`,
  `aeltere_id_uebersprungen` + Warnzeilen im Aktivitäts-Protokoll.
- Z1 `PrintBom.cshtml`: oberer Druckrand 28 mm. Z2 Druck übernimmt die Sortierung der Stückliste. Z3 Spalte „Werkbank“ in der
  Kommissionierung (server-filterbar, Spaltenauswahl).
- TS-107.1–107.6 (Kapitel 107, Nummer wie im IDEAL-Bündel).

## Deploy-Risiken

Dienst **und** Web aus `main` (enthält auch v1.30.2 Zahnrad). Dienst stoppen → publishen → starten; Web App-Pool recyceln,
Strg+F5 (Inline-JS `Bom.cshtml`). Bündel holt den Stand per Vorwärts-Merge (Mensch); Konflikte dort erwartbar in
`AppVersion.cs`, `Changelog.cshtml`, `docs/TESTSZENARIEN.md`, `Views/Picking/Index.cshtml`, `PickingController` — Bündel-Seite
behalten, Kapitel 107 dort prüfen (evtl. Platzhalter).
