---
type: changelog
version: 1.60.1
date: 2026-10-15
---
# v1.60.1 — IDEAL: Artikel-View mit WMS-Spaltennamen

[[2026-10-14-ideal-sage-views-umstellung-spec]] (Nachbesserung zu [[2026-10-15-v1-60-0-ideal-sage-views-umstellung]]).
**Direkt im Bündel**, Start `2524bb03`, Commit `63524941`. **Keine Migration.**

**Warum:** Der Artikel-Sync aus `vw_IDEAL_AKE_WMS_Artikel` scheiterte mit „Ungueltiger Spaltenname“ — die View liefert
laut P4 die WMS-Namen (`ArticleNumber`, `Description`, …), v1.60.0 las die Sage-Namen des alten Selects.

## Kernpunkte

- `SageArticleSql.BuildViewSql` auf die WMS-Namen; `LegacySql` unverändert, jetzt dauerhaft per SHA-256-Wächter gesichert.
- Einmal-Skript: Rückweg-Kommentar nennt `UQ_`- und `IX_`-Indexnamen.
- TS-93.4b, [[fallstricke]] §16 (Spaltennamen nie aus Antworten ableiten). Tests Web 2016, Service 282.

## Deploy-Risiken

Nur Service-Code geändert (Web nur Version/Changelog). Vor dem nächsten Artikel-Lauf mit View: P4a (Dubletten), P4b
(Artikelgruppe als Code), P4c (Nummern 1:1 in `KHKArtikel`) und P4e (Hauptlagerplatz = `KHKLagerplaetze.Kurzbezeichnung`) prüfen. AKE unberührt (Key leer).
