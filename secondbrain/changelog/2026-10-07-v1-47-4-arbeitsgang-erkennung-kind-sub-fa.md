---
type: changelog
version: 1.47.4
date: 2026-10-07
---
# v1.47.4 — IDEAL: Kuerzel einer Sub-FA-Zeile nur noch am eigenen Sub-FA (Arbeitsgang-Erkennung)

Bugfix im Buendel `feature/2026-08-07-ideal-teile-1-5` nach dem FREIGABE-NACHTRAG (2026-10-07) zu
[[2026-09-21-ideal-bde-arbeitsgaenge-aus-struktur-spec]]. Anlass: Befund W-1 aus
[[2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-2-spec]] (Antwort 7, Keyuser/Gerald). **Keine Migration.**

## Kernpunkte

- `WorkOperationStructureDetectionService`: Scope je Sub-FA = eigene Zeile + direkte **Blatt**-Kinder
  (`SubFA == 0`). Ein direktes Kind mit eigenem Sub-FA traegt nur noch zu seinem eigenen Auftrag bei
  (vorher zusaetzlich zum Vater → zwei gleich beschriftete Arbeitsgaenge am BDE-Terminal).
- Blatt-Kuerzel (z. B. `LS KA`) bleiben unveraendert am Vater-Sub-FA rueckmeldbar.
- Tests: `Detect_GrandchildToken_LandsOnParentNotGrandparent` nachgezogen, neu
  `Detect_ChildSubFaToken_NotOnParent` und `Detect_LeafChildToken_StillOnParent`. TS-77.18/77.19.
- Einmal-Skript `SQL/Einmalig/2026-10-07_v1.47.4_RemoveParentWorkOperationsFromChildSubFaTokens.sql`
  ([[0015-einmal-datenskripte-ausserhalb-der-nummerierten-sql-reihe]]) — **nicht ausgefuehrt**.

## Deploy-Risiken

- **Reihenfolge:** Backup → Windows-Service (+Web) v1.47.4 → DANN Einmal-Skript genau einmal (vorher
  liefe der alte Erkennungslauf und legte die geloeschten Vater-Arbeitsgaenge wieder an).
- Gebuchte/rueckgemeldete Vater-Arbeitsgaenge werden nur gemeldet (Meldeliste des Skripts), nicht
  geloescht — mit dem Fachbereich klaeren.
- Wirkt mit dem Buendel-Deploy (IDEAL). AKE unberuehrt (keine `FaHierarchyNodes`).

## Commits

Buendel `abd3e16a` (Service + Tests), `8fd1aa3d` (Version, Changelog, TS, Einmal-Skript). Merge: mit dem Buendel.
