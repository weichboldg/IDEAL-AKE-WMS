---
type: changelog
version: 1.99.72
date: 2026-11-25
---
# v1.99.72 — BDE-Teileliste: Fräsen-Pfad, Blechdicke, Lagerbestell-Positionen an der Isolierungsfräse

Spec [[2026-11-20-bde-liste-isolierungsfraese-pfad-material-spec]]. Commit `d49a3f39` (Worktree `feature/2026-08-07-ideal-teile-1-5`).
**SQL/164** (Migration `20261125173002_AddArticleCamPath`), **SQL/165** (Migration `20261125173021_AddBdeTeilelisteLagerbestellungen`), Web **und**
Dienst (Version in beiden `AppVersion.cs`; Dienst-Nachlese liest `USER_OSBlechdicke`). TS-193. ADR [[0038-teilmeldung-auch-fuer-lagerbestell-positionen]]. Nicht gemergt.

## Kernpunkte

- `Articles.CamPath` (NVARCHAR(500), reines WMS-Feld): Prüfen/Normalisieren in `Helpers/FraesenPfad.cs` (nur UNC, `file://`-URI), Baustein
  `Views/Shared/_FraesenPfad.cshtml` + ein delegierter Handler „Pfad kopieren“ in `site.js`. Spalte „Fräsen-Pfad“ in Artikel-Liste, Bestandsübersicht
  (neue Spaltenauswahl), Lager-Detailseite, Besteller-Seite (neue Spaltenauswahl `WarehouseRequisitionItems`, `loadStock` liest `data-article`),
  Teileliste Einzeln/Summiert – überall Standard ausgeblendet (drei Quellen gleich).
- Pflege: POST `Articles/FraesenPfad` mit `[RequireFraesenPfadAktiv]` + `[RequireFraesenPfadPflegenAccess]` (admin, masterdata, **neue Rolle
  `fraesenpfad_pflegen`**); Edit-POST fasst `CamPath` nie an. Gate `FraesenPfadAktiv` (aus).
- BDE-Teileliste: Blechdicke sichtbar (Zahlenfilter), Summiert mit Material/Blechdicke, Bemerkung PN/Fertigungsinfo PN über Spaltenauswahl.
- Lagerbestell-Positionen: Werkbank-Option `ProductionWorkplaces.BdeTeilelisteLagerbestellungen` (nur mit BDE-Teileliste), Positionen abgeschickter/
  teilgelieferter Lagerbestellungen mit Kürzel im Arbeitsschritte-Merkmal, Termin Abschicken + `BdeTeilelisteLagerbestellungArbeitstage` (10)
  Arbeitstage, Melden als `BdeTeilMeldung` mit `WarehouseRequisitionItemId` (Formular-Schlüssel `LB|<Id>`), nichts gebucht. Teilmeldungen-Liste:
  Spalte „Lagerbestellung“.
- CSV-Seiteneffekt behoben: `Index`-GET speichert Filter nur bei `Sec-Fetch-Mode: navigate`.
- Einmal-Skript `SQL/Einmalig/2026-11-25_v1.99.72_FraesenPfad_Erstimport.sql` (IDEAL, Trockenlauf/SCHARF, nur `CamPath IS NULL`).

## Deploy-Risiken

- SQL/165 baut `UX_BdeTeilMeldungen_Schluessel` neu (Filter `[IsCancelled] = 0 AND [WarehouseRequisitionItemId] IS NULL`) – Web und Dienst gestoppt.
  Reihenfolge 163 → 164 → 165 → 166. Additiv, nicht daten-destruktiv.
- Einmal-Skript erst nach SQL/164; Belegestation-Block (Rückfrage 16) vor SCHARF klären. Rolle `fraesenpfad_pflegen` braucht zusätzlich
  `masterdata_read` (Manifest-Prüfung `Pruefung/FraesenpfadOhneStammdatenLesen`).
- `FraesenPfadAktiv` bleibt aus, bis der Erstimport gelaufen ist (Manifest-Einstellung `1.99.72-FraesenPfadAktiv`).
