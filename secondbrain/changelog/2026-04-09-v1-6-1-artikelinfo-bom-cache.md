---
type: changelog
version: 1.6.1
date: 2026-04-09
---
# v1.6.1 — Artikelinfo: Fertigungsauftraege aus dem BOM-Cache

- Neue Tabelle „Teil enthalten in folgenden Fertigungsauftraegen" in der Artikelinfo: zeigt offene
  FAs, in denen der Artikel als Bauteil vorkommt.
- Datenquelle ist der persistente BOM-Cache (`CachedBomItem` → `CachedBomHeader` →
  `ProductionOrder`) → [[0007-bom-quelle-sage-view-mit-oseon-fallback]].
- **Konsequenz:** die Vollstaendigkeit dieser Liste haengt am Cache-Fenster und -Cap. Was nicht
  gecacht ist, erscheint hier nicht.
