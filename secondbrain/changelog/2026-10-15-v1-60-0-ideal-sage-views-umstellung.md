---
type: changelog
version: 1.60.0
date: 2026-10-15
---
# v1.60.0 — IDEAL: Sage-Zugriffe auf die neuen WMS-Views

[[2026-10-14-ideal-sage-views-umstellung-spec]]. **Direkt im Bündel** `feature/2026-08-07-ideal-teile-1-5`, Start
`d4108f1a`, Commits `b53b99dd`, `2524bb03`. **Keine Migration.** Umsetzung [[2026-10-15-ideal-sage-views-umstellung-umsetzung]].

**Warum:** Gerald hat in Sage vier Views für das WMS angelegt; Sage legt damit fest, was das WMS sieht, ohne
WMS-Codeänderung. Die Artikel-View stellt außerdem die 00er-Artikel ab.

## Kernpunkte

- FA-Struktur: Default/Fallback `[vw_IDEAL_AKE_WMS_FAListe]`/`[…FAInfos]`, Existenzprüfung vor dem Lesen; fehlt eine
  View: Fehler + Fehlermail mit klarer Meldung statt `SqlException`.
- Werkbank-Anlage: Quelle per `Sync:ProductionWorkplaceSyncViewName` (Default `KHKPpsArbeitsplaetze`); fehlt sie:
  Warnung + Skip, keine Mail.
- Artikel: `Sync:ArticlesViewName` (leer = bisheriger Select zeichengleich in `SageArticleSql.LegacySql`), fehlt die
  View: Warnung + Skip, kein Rückfall, nie Löschen.
- Einmal-Skript `SQL/Einmalig/2026-10-15_IDEAL-Sage-Views_Bereinigung00erArtikel.sql` (nur angelegt).
- Tests Web 2016, Service 281. TS-93. [[fallstricke]] §16 (View „Arbeitsgaenge“ liefert Arbeitsplätze).

## Deploy-Risiken

Web **und** Service (Web zuerst — der Web-Start legt die neuen Keys an). Vorher Prüfabfragen P1–P5b auf Sage
(Gerald). Spaltennamen der Artikel-View **unverifiziert** bis P4. Das 00er-Skript ist **daten-destruktiv**, nur IDEAL,
DB-Backup vor dem Lauf, Reihenfolge laut README/Spec. Nach der Bereinigung `Sync:ArticlesViewName` nicht mehr leeren.
