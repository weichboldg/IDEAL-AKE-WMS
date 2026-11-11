---
type: changelog
version: 1.62.0
date: 2026-10-16
---
# v1.62.0 — Stückliste: Matchcode und Lagerplatz-Limit

[[2026-10-16-stueckliste-matchcode-lagerplatz-limit-spec]], Umsetzung
[[2026-10-16-stueckliste-matchcode-lagerplatz-limit-umsetzung]]. **Direkt im Bündel**, Start `8773ba4f` (v1.61.0),
Commits `c5d9b2b7`, `c9bc382f`, `41c3c14e`, `6a452892`, QA-Fix `9177db47` + `d75680f4`. **Keine Migration.** Behebt
[[2026-10-16-stueckliste-lagerplatz-zelle-tabellenbreite-bug]].

## Kernpunkte

- Spalte **Matchcode** (V1–V5 + Druck): IDEAL `FaHierarchyNode.Matchcode`, leer → Artikelstamm; AKE Artikelstamm (Variante B).
- **Lagerplatz**: höchstens 3 Plätze, Rest per „+n weitere“-Popover (hover/focus, `container: body`, Escape); Reihenfolge
  zentral Hauptlagerplatz → Menge absteigend → Code; Spalte filterbar; CSV und Druck volle Liste.
- `ColumnDefinitions.Bom`: matchcode, warehouse-order, `SupportsReorder: true` (Wert wird nirgends gelesen).
- QA-Fix W-1: Bulk-Bestell-Fallback las Zellen über feste Positionen (die neue Spalte verschob sie) → per col-key.
- QA ohne kritische Befunde; Build + 2043 Web- / 307 Service-Tests grün.
- [[fallstricke]] §7, §12, §14 Nachträge (inkl. „keine festen `cells[n]`“).

## Deploy-Risiken

Nur Web. Wirkt an beiden Standorten. Reihenfolge der Plätze ändert sich auch im Druck (gewollt).
