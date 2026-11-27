---
type: changelog
version: 1.99.74
date: 2026-11-25
---
# v1.99.74 — BDE-Teileliste Summiert: Aufträge aufklappbar, Summen-Haken, aggregierte Ampel

Spec [[2026-11-25-bde-masken-vereinheitlichung-spec]], Etappe 2. Commit `eecc47ac` (Worktree `feature/2026-08-07-ideal-teile-1-5`).
**Keine Migration**, nur Web (Dienst: nur Versionsnummer). TS-195. Nicht gemergt.

## Kernpunkte

- Summiert: Knopf „Aufträge (n)“ am Matchcode klappt die Einzelaufträge auf (FE-Termin aufsteigend, ohne Termin zuletzt), je mit eigenem Haken,
  AG-Status und Status (Partial `_TeilStatus`, aus `Index.cshtml` gezogen).
- Summen-Haken setzt alle Auftrags-Haken der Gruppe; Abwählen eines Auftrags nimmt ihn zurück; Zähler ohne Doppelzählung, der Server entdoppelt.
- Neue Spalte AG-Status in Summiert: `BdeTeilelisteService.AgAggregieren` je Kürzel, schlechtester Stand über `ArbeitsgangStatusRegel.Ableiten`.
- Manifest-Info `v1.99.74-BdeTeilelisteSummiertInfo` (IDEAL, nachher).

## Deploy-Risiken

- Keine Schema-Änderung. Browser-Cache am Terminal ggf. leeren (`bde-teileliste.js`).
- CSV-Export Summiert enthält die Unterzeilen (bewusst, wie Etappe 1).
