---
typ: feature
---
# Leitstand: schreibgeschützte Stückliste auf Sub-FA-Zeilen (wie FA-Liste)

**Aufgenommen 2026-09-25.** Herkunft: Final-Review der Umsetzung von
[[2026-09-25-kommissionierung-nur-hauptfa-spec]] (v1.46.0), Minor 2.

## Ausgangslage

Seit v1.46.0 zeigen Sub-FA-Zeilen im **Kommissionier-Leitstand** keinen Stückliste-Link mehr — der
interaktive Link (`Picking/Bom`) ist dort bewusst entfernt, weil Kommissionierung nur am HauptFA erfolgt.
Die **FA-Liste** bietet an derselben Stelle für Benutzer mit Vorbau-Recht den schreibgeschützten Link
`FaWorklist/Bom` an (`Views/ProductionOrders/_ProductionOrderRow.cshtml`, `else if (Model.HasVorbauAccess)`).

Im Leitstand fehlt diese Rückfall-Anzeige: Leitstand-Personal mit Vorbau-Recht kann die Stückliste einer
Sub-FA von dort aus nicht mehr ansehen (nur über die FA-Liste). Die Spec verlangt das nicht (AK 19 bezog
sich auf die FA-Liste; der Leitstand hatte nie einen FaWorklist-Link) — aber ein Anwender erwartet in beiden
Listen dasselbe Verhalten.

## Umsetzungsskizze

- `PickingLeitstandViewModel` (bzw. RowContext) um `HasVorbauAccess` ergänzen (Quelle wie in
  `ProductionOrdersController`).
- `Views/PickingLeitstand/_PickingLeitstandRow.cshtml` (Stückliste-Zelle, ca. Z. 35): Fallback
  `else if (Model.HasVorbauAccess)` → `FaWorklist/Bom`, exakt wie in der FA-Liste.
- Testszenario-Ergänzung zu TS-79 (Unberührt/FaWorklist).

Klein, eigenständig mergebar. Keine Migration.
