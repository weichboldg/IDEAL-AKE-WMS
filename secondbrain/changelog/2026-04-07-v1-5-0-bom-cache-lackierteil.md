---
type: changelog
version: 1.5.0
date: 2026-04-07
---
# v1.5.0 — BOM-Cache + Lackierteil-Erkennung

- **BOM-Cache:** der Service legt die Stuecklisten der wichtigsten offenen Auftraege in der
  WMS-Datenbank ab (max. 200 Auftraege, 8 Wochen voraus) — Stueckliste-Aufrufe werden deutlich
  schneller → [[0007-bom-quelle-sage-view-mit-oseon-fallback]].
- **Hash-basierte Aenderungserkennung** (SHA256): der Cache wird nur ueberschrieben, wenn sich der
  Inhalt wirklich geaendert hat. *(Genau dieser Skip-Pfad ist der Grund, warum die spaetere
  FA-Arbeitsgang-Erkennung NICHT daran haengen darf.)*
- **Lackierteil-Erkennung:** Auftraege werden automatisch markiert, wenn ihre Stueckliste
  Lackierteile enthaelt (Stammdaten-Einstellung `LackierteilKategorieName`).
- Neue Spalte „Lack-T" in der FA-Liste (klickbare Checkbox, nur bei Lackierteil-Auftraegen).
- **Beschichtungstermin bedingt:** nur noch fuer Auftraege mit Lackierteilen — backward-kompatibel,
  leeres Setting = wie vorher fuer alle.
- Neue gemeinsame Helper `ConnectionStrings` und `BulkCopyHelper` im Service.
