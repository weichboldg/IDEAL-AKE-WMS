---
type: changelog
version: 1.70.0
date: 2026-10-28
---
# v1.70.0 — Aktivitäts-Protokoll: Spaltenfilter über alle Einträge, deutsche Aktivitätsnamen

Spec [[2026-10-16-aktivitaetsprotokoll-filter-und-namen-spec]], Bug [[2026-10-28-aktivitaetsprotokoll-spaltenfilter-nach-paging-bug]].
Basis `2c1e3a6d` (v1.69.0), Commit `badbedcc`. **Keine Migration**, kein Index, nur Web (Dienst nur Version). TS-102.

## Kernpunkte

- Spaltenfilter laufen in SQL vor Count/Skip/Take (`SyncLogRepository.ApplyColumnFilter`, Muster Bewegungshistorie): Trefferzahl,
  Seitenleiste und CSV-Export zählen nur Treffer; Kappungshinweis nur bei mehr als 5000 Treffern.
- Zeitpunkt tageweise (Tag/Monat/Jahr, Vergleich, Bereich); Uhrzeit-Teilsuche entfällt bewusst.
- Deutsche Anzeigenamen in `SyncLogServices.DisplayNames`; Auswahlfeld alphabetisch, Wert/Tooltip technisch, Filter trifft beide Namen.
  Altnamen außerhalb von `SyncLogServices.All` erscheinen technisch.
- Seiten-Guard `page < 1 → 1`. Hilfe: Rolle nur `admin`, Namensliste deutsch.

## Deploy-Risiken

- Groß/Klein in den Text-Filtern hängt jetzt an der Kollation der `SyncLogs`-Spalten (P-2), vorher im Speicher immer egal.
- „Meldung enthält“ ist ein `LIKE '%x%'` ohne Index (Scan); Tabellengröße prüft P-3.
