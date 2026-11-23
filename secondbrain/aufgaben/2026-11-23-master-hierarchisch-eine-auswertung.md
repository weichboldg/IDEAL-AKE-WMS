---
type: aufgabe
status: offen
created: 2026-11-23
quelle: QA v1.99.61 Controller-Gate, Hinweis H-3
---
# Master `ProduktionsauftragHierarchisch` an einer Stelle auswerten

Die Auswertung des Masters ist uneinheitlich (Stand v1.99.61, Bündel-Worktree):
- `IsTrue` in ProductionOrders, Tracking, FaCompletion, FaWorklist, PickingLeitstand, BdeTerminal, ProductionWorkplaces, BdeTeileliste: akzeptiert auch `"1"`/`"on"`, ohne Trim.
- `OperationConfigsController.cs:84`: nur `"true"`, ohne Trim.
- `RequireHierarchischeStrukturAktiv`, `NavigationSicht`, `FaBuchungsText.IstHierarchisch`: nur `"true"` nach Trim.

Folge: Stünde der Master auf `"1"`, zeigte die FA-Liste hierarchisch an, die FA-Struktur wäre gesperrt.
Ob ein Schreibweg andere Werte als `"true"`/`"false"` schreiben kann, ist ungeprüft – vor Umsetzung alle Schreibwege belegen.

Vorschlag: alle Stellen auf `FaBuchungsText.IstHierarchisch` (bzw. eine gemeinsame Methode) umstellen, Wächter-Test gegen neue Eigenauswertungen.
Bezug: [[2026-11-23-v1-99-61-fa-struktur-titel-und-gate]].
