---
type: changelog
version: 1.99.73
date: 2026-11-25
---
# v1.99.73 — BDE-Teileliste Einzeln: AG-Status, Baugruppen-Teile als Zeilen, Handy-Ansicht

Spec [[2026-11-25-bde-masken-vereinheitlichung-spec]], Etappe 1 (übernimmt die Mobil-Spec [[2026-11-18-bde-vereinfacht-mobil-kompakt-spec]] F1–F6).
Commit `219a69c3` (Worktree `feature/2026-08-07-ideal-teile-1-5`). **Keine Migration**, nur Web (Dienst: nur Versionsnummer). TS-194. Nicht gemergt.

## Kernpunkte

- Neue Spalte **AG-Status** in Teile Einzeln: Symbole wie in der FA-Struktur. Status-Ladung an einem Ort `Services/FaAgStatusService.cs`
  (aus `FaHierarchyController` gezogen), Markup `Views/Shared/_FaAgSymbole.cshtml`. Zweite Quelle Teilmeldung über
  `BdeTeilelisteService.TeilmeldungenAnwenden` (nur Teileliste): nicht stornierte Teilmeldung = erledigt, Teileliste-Kürzel ohne Arbeitsgang =
  offen/überfällig. Spalte serverseitig filterbar.
- Baugruppen: Knopf „Teile (n) ▸“ am Matchcode, Kinder als eigene Tabellenzeilen (`TeilelisteZeile.Kinder`), nur Anzeige; Spaltenfilter trifft
  Kinder (Baugruppe aufgeklappt, Kind „◆ Treffer“); `bde-teileliste.js` hält Kinder nach dem Sortieren unter der Baugruppe.
- Handy (< 992 px, Scope `.bde-tl-seite` in `bde.css`): Filterkarte eingeklappt mit Kurzfassung, Kartenansicht (`data-label` je Zelle, auch Summiert),
  44-px-Ziele (Haken, Pagination, Filter), Hinweise in `<details>`.
- Manifest-Info `v1.99.73-BdeTeilelisteAgStatusMobilInfo` (IDEAL, nachher).

## Deploy-Risiken

- Keine Schema-Änderung. Browser-Cache am Terminal ggf. leeren (neue `bde.css`/`bde-teileliste.js`).
- Regression FA-Struktur (Status-Ladung verschoben) und BDE-Terminal/Cockpit (gemeinsame `bde.css`) in TS-194.5 prüfen.
- CSV-Export der Teileliste enthält jetzt auch Kinderzeilen (gemeinsamer `csv-export.js`, bewusst unverändert).
