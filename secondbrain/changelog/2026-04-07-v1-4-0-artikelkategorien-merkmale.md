---
type: changelog
version: 1.4.0
date: 2026-04-07
---
# v1.4.0 — Artikelkategorien + Artikelmerkmale

- Neue Stammdaten-Seite fuer **Artikelkategorien** — manuell anlegbar oder automatisch per
  OSEON-Sync.
- **Artikelmerkmale:** frei definierbare Merkmale (Boolean/Dropdown) je Artikel, in der
  Artikel-Uebersicht als filterbare Spalten.
- Artikel-Bearbeitung mit Kategorie-Dropdown und dynamischen Merkmal-Feldern.
- Neue Spalte „Kategorie" in der Stueckliste.
- OSEON-Artikelkategorie-Sync als Service-Toggle.

> Der Enum `AttributeType` wird spaeter mit den FA-Merkmalen geteilt — der Typ `Text` (v1.22.0) gilt
> jedoch **nur** dort, weil `ArticleAttributeValue` kein Textfeld hat. Siehe [[fallstricke]].
