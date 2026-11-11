---
typ: notiz
spec: "[[2026-10-16-stueckliste-matchcode-lagerplatz-limit-spec]]"
status: Testbereit
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# Umsetzung: Stückliste — Matchcode + Lagerplatz-Limit

Spec [[2026-10-16-stueckliste-matchcode-lagerplatz-limit-spec]] · **direkt im Bündel**, Start `8773ba4f` (v1.61.0),
Ziel v1.62.0, TS-95, keine Migration. Deploy: nur Web.

## Plan (kompakt — Details auf Datei:Zeile-Ebene in der Spec, Variante B laut Freigabe-Nachtrag)

1. **M** — `FaHierarchyBomItem.Matchcode` + Mapping; `BomItemViewModel`/`PrintBomItem.Matchcode`; Lookup + `(bom as FaHierarchyBomItem)?.Matchcode ?? lookup`
   an `PickingController.Bom`, `RenderPrintBomAsync`, `ReadOnlyBomBuilder` (Ctor + Tests); `Bom.cshtml` th/td/#column-config/emptyColspan/colNames;
   `ColumnDefinitions.Bom` matchcode + warehouse-order; `PrintBom.cshtml` ShowCol. Tests (Kopf ≠ Komponente, B-Fallback).
2. **L** — Sortierung `StockMovementRepository` (+ Test); Zelle mit Limit 3, Badge, `d-none`-Volltext, `data-csv-value`, `data-filterable`;
   Popover-Init vor `if (readOnly) return;`, Escape; `site.css`. frontend-design vorher.
3. **Doku** — Hilfe, Changelog, 2× AppVersion 1.62.0, TS-95; Brain.

## Stand

- 2026-10-16: Umsetzung gestartet.
- 2026-10-16: feature-complete (`c5d9b2b7` M, `c9bc382f` + `41c3c14e` L, `6a452892` Doku); Build + 2042 Web- / 307 Service-Tests grün.
- 2026-10-16: QA: W-1 (feste Zellpositionen im Bulk-Bestell-Fallback) behoben `9177db47`, TS `d75680f4` → **Testbereit**. Build + 2043 Web- / 307 Service-Tests grün. Changelog [[2026-10-16-v1-62-0-stueckliste-matchcode-lagerplatz-limit]].
- Schranke 2 mit dem Bündel: TS-95, Manuelle Test-Checkliste in der Spec (18 Punkte).
