---
type: changelog
version: 1.99.26
date: 2026-11-16
---
# v1.99.26 — FA-Struktur IDEAL über SData (Vergleich, Umschalten gesperrt)

Bündel `feature/2026-08-07-ideal-teile-1-5`, Commits `21b3fb65` (E1), `28dfdc4c` (E2), `a3214677` (E3), `24ad7354` (E4), `9fa447f1` (E5),
`5163df17` (E6). Spec [[2026-11-16-fa-struktur-ideal-ueber-sdata-spec]], ADR [[0029-fa-struktur-ideal-knoten-aus-sdata-fainfos-aus-view-hybrid]],
Lauf [[2026-11-16-goal-lauf-beschichtung-fa-sdata-arbeitsgaenge]]. Nicht gemergt.

## Kernpunkte

- `orderBy` ist bei jeder SData-Abfrage mit `where` Pflicht (`SageSdataReadClient.BuildUrl`); Belegsuche im FA-Controller sortiert nach `BelID`.
- „Stückliste live“: FA-Modus auch für beendete Haupt-FAs; Artikel-Modus heißt „Ressourcenliste (Vorlage), nicht FA-Stückliste“.
- Dienst, Modus „Vergleich“: beendete Haupt-FAs und alle Knotenfelder (FA-Kopf `USER_OS*` über `dtsFaBelegsuche`, Blatt/Stammfelder über
  Artikel) werden mit der View verglichen. Geschrieben wird weiter nur aus der View.
- Modus SData als Hybrid (Knoten SData + FAInfos View) gebaut, **bleibt gesperrt** bis P-FS3 (DPS) und Rückfragen 4/5; wirkt gesperrt wie View
  mit Warnung (Auflage Gerald 2026-11-16, Abweichung zu AK 10).

## Migration / SQL

Keine, keine neuen Keys. Letzte SQL-Nummer unverändert. Web und Dienst gemeinsam deployen.

## Deploy-Risiken

- Default View: ohne Umstellen ändert sich nichts. Manifest `v1.99.26-IDEAL-FaStrukturVergleich` (Einstellung, IDEAL) und `v1.99.26-Info`.
- Im Vergleich mehr Sage-Aufrufe je Lauf (nicht gemessen, P-FS6); erwartete Abweichungen: Artikelgruppe (nur Code) und Blattfelder ohne Quelle.
- DEV AKE nie mit main deployen.
