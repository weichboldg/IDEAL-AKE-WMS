---
type: changelog
version: 1.96.0
date: 2026-11-11
---
# v1.96.0 — Übernahme Korrekturen aus AKE v1.30.2–1.30.4 ins Bündel

Bündel `feature/2026-08-07-ideal-teile-1-5`: Merge-Commit `02a0a173` (Eltern `a26be406` = v1.95.0 und main `1753c79e`), Testfix `5492f0db`.
Eigener Lauf laut [[2026-11-10-vorwaerts-merge-main-ins-buendel]] (Auftrag Gerald 2026-11-11). Nicht nach main gemergt, nicht gepusht.

## Kernpunkte

- v1.30.4 [[2026-11-08-kommissionierfotos-ohne-anmeldung-abrufbar-spec]]: Fotos unter `App_Data\Fotos\Kommissionierung`, Auslieferung über
  `PhotoController.GetFile` (alte URL), `LoginRedirectMiddleware` (Ausnahme nur echte wwwroot-Dateien), Löschen nur eigener FA, Verschiebung des
  Altbestands beim Start.
- v1.30.3 [[2026-11-07-oseon-sync-neu-ausgeschriebener-auftrag-spec]]: OSEON-MERGE über Auftragsnummer (`OseonOrderSelection`), Stücklisten-Druck
  28 mm oben und in Sortierreihenfolge. Die Werkbank-Spalte hatte das Bündel schon (Kommissionieraufträge v1.88.0).
- v1.30.2 Zahnrad Lagerbestellung: im Bündel schon über den zentralen Toolbar-Host (v1.65.0) gelöst; `BomZahnradHostTests` aus main nicht
  übernommen (ersetzt durch `TableToolbarHostTests`).
- Testszenarien: Kapitel 107 und 124 im Bündel; die Update-Skript-Spec wechselt auf TS-125.
- Konfliktentscheidungen je Datei: siehe Aufgabe, Abschnitt „Ergebnis“.

## Deploy-Risiken

Web und Dienst (Dienst wegen OSEON-MERGE, v1.30.3). Keine Migration. Vor dem Web-Start je Site `App_Data\Fotos\Kommissionierung` anlegen und dem
Pool-Konto Ändern geben, Fotos zählen, Log „verschoben: n, uebersprungen: m“ abgleichen (Manifest-Eintrag in
[[2026-11-09-update-skript-je-zielsystem-mit-nachlauf-spec]]). Rücksprung auf eine Bündelversion < v1.96.0 zeigt keine Fotos mehr.
**DEV-Hinweis:** `dev-deploy.ps1` schaltet alle `Sync:*Enabled` aus – auf AKE-DEV die 5 Schalter von Gerald danach wieder setzen (2026-11-11 erledigt).

## QA

qa-agent 2026-11-11 grün (Build 0 Fehler, Web 2592/2593, Dienst 512/512), DEV AKE + IDEAL v1.96.0, Publish `<Worktree>\publish` Commit `5492f0db`.
Offen für den Menschen: Druckvorschau 28 mm, TS-107.5 absteigend/mit Filter, OSEON am AKE-Test, Kachel-Löschen/Kamera am Terminal, TS-120.15 im Browser.
