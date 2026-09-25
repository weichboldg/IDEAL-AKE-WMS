---
typ: feature
---
# Kommissionierer-Warteschlange: Zwei-Ebenen-Gruppierung nach HauptFA wird überflüssig

**Aufgenommen 2026-09-25.** Herkunft: Kritische Prüfung von
[[2026-09-25-kommissionierung-nur-hauptfa-spec]], Hinweis H1.

## Ausgangslage

Mit [[2026-09-25-kommissionierung-nur-hauptfa-spec]] ist nur noch die **HauptFA** freigebbar. Die drei
Warteschlangen-Abfragen (`GetReleasedForPickingAsync`, `GetReleasedForPickingCountAsync`,
`GetMaxPickingPriorityAsync`) liefern dann ausschließlich HauptFAs.

Die Warteschlange `/Picking` gruppiert im hierarchischen Modus trotzdem weiter zweistufig nach
`OrderNumber` (`PickingController.Index`, Bündel-Worktree ca. Z. 197-230: `GroupBy(i => i.OrderNumber)`,
`PickingListGroup`, Paginierung über Gruppen, `HierarchicalRowCount`). Jede Gruppe enthält damit genau
**eine** Zeile. Gruppen-Kopfzeile, Auf-/Zuklappen und die Zählung nach Gruppen sind funktional leer.

## Zu klären

- Hierarchischen Zweig in `PickingController.Index` und in der View auf die flache Liste zurückführen?
  Oder die Gruppierung behalten, weil die Kopfzeile noch Information trägt (Kunde, Termin)?
- Welche Spalten (`SubOrderNumber`, `ParentSubOrderNumber`, `SageMissingSince`-Badge) bleiben in der
  Warteschlange sinnvoll, wenn nur HauptFAs erscheinen?
- Paginierung: nach Zeilen statt nach Gruppen (Standard-Muster ADR 0005).

## Bewusst nicht Teil von [[2026-09-25-kommissionierung-nur-hauptfa-spec]]

Dort nur im Testprotokoll vermerkt: Die Warteschlange zeigt je Gruppe nur noch eine Zeile. Gruppen mit
mehreren Sub-FAs sind nicht mehr zu erwarten.
