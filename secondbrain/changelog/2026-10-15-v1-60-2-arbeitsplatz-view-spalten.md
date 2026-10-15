---
type: changelog
version: 1.60.2
date: 2026-10-15
---
# v1.60.2 — IDEAL: Arbeitsplatz-View mit eigenen Spalten

[[2026-10-14-ideal-sage-views-umstellung-spec]] (Nachbesserung zu [[2026-10-15-v1-60-1-artikel-view-wms-spalten]]).
**Direkt im Bündel**, Start `63524941`, Commits `73b37e55`, Doku-Nachzug `d3de9022` (P3b nach QA). **Keine Migration.**

**Warum:** Die Werkbank-Anlage aus `vw_IDEAL_AKE_WMS_Arbeitsgaenge` scheiterte mit „Ungueltiger Spaltenname“ — die View hat
nur `Arbeitsgangnummer`, `Bezeichnung`, `AG` und keinen Mandant/Aktiv-Filter.

## Kernpunkte

- `SageArbeitsplatzSql`: Quelle `KHKPpsArbeitsplaetze` = bisheriges SQL zeichengleich (Wächter-Test); jede andere Quelle =
  View-Spalten ohne Filter. Entscheidung über den Objektnamen.
- README: Spaltennamen vorher per `sys.columns`, P3b (EXCEPT in beide Richtungen) Pflicht. TS-93.3b.
- [[fallstricke]] §16 Nachtrag: View-Spaltennamen nie aus Annahmen — alle vier Views. Tests Web 2016, Service 288.

## Deploy-Risiken

Service (Web nur Versionsanzeige). Vor dem Umstellen P3b. Mit dem Default `KHKPpsArbeitsplaetze` keine Änderung.
