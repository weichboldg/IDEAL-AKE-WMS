---
type: changelog
version: 1.89.0
date: 2026-11-08
---
# v1.89.0 — Kommissionieraufträge: Nachbesserung Q9–Q11

Bündel `feature/2026-08-07-ideal-teile-1-5`, Lauf [[2026-11-08-nachbesserungen-vor-buendel-merge-goal-lauf]]. Spec [[2026-11-08-kommissionierung-auftraege-kopf-zeilen-spec]].
Commits `bc8be58c`, `53858c6c`. Nicht gemergt (Schranke 2).

## Kernpunkte

- Q9 Zeilenstatus nach dem Kommissionieren einheitlich „gepickt“ (`PickingOrderListItem.ZeilenStatusText`, eine Stelle auch für `Bom.cshtml`).
- Q10 zweiter Standardbereich wird abgelehnt; Wechsel = abwählen + setzen; Zwei-Schritt-Speichern und `InTransactionAsync` entfernt.
- Q11 `PickingOrders.AltbestandPruefen` (**SQL/141**, Migration `20261108071811_AddPickingOrderAltbestandPruefen`): Einmal-Skript markiert offene FAs mit Menge ≠ 1 und listet sie; Erzeugung überspringt sie bis „Altbestand geprüft“ im Leitstand.

## Deploy-Risiken

**SQL/141 vor dem Einmal-Skript** (Skript bricht sonst ab). Deploy-Reihenfolge im Skriptkopf: SQL/133 + SQL/141 → Trockenlauf → Freigabe → Ausführung → Leitstand. Web + Service (Version).
