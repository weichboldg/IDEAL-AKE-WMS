---
type: bug
title: Karte „Meine Fehlteile“ zählt Lager und Glas zusammen; Werkbank-Auswahl im Modus „Nur meine Werkbänke“ gesperrt
status: offen
severity: mittel
created: 2026-11-08
affected_code:
  - "IdealAkeWms/Data/Repositories/WarehouseRequisitionRepository.cs:469-493 (GetShortageCountsForUserAsync)"
  - "IdealAkeWms/Controllers/WarehouseRequisitionsController.cs:92"
  - "IdealAkeWms/Controllers/MissingPartsController.cs:32, 109"
  - "IdealAkeWms/Views/MissingParts/Index.cshtml:70"
source_backlog: "[[2026-11-07-fehlteile-karte-meine-fehlteile-lager-glas]]"
spec: "[[2026-11-08-fehlteile-karte-meine-fehlteile-lager-glas-spec]]"
---

## Symptom

- Auf `/WarehouseRequisitions` zählt die Karte „Meine Fehlteile“ Lager- und Glas-Bestellungen zusammen; der Link führt in eine nach Typ
  gefilterte Liste → Zahl und Liste passen nicht zusammen.
- `/MissingParts` im Modus „Nur meine Werkbänke“: Werkbank-Dropdown ist gesperrt und listet alle Werkbänke.
- Zusatzfund: Das Häkchen „Nur meine Werkbänke“ lässt sich per Klick nicht abschalten (Checkbox sendet nichts, Default `mineOnly = true`).

## Reproduktion

Siehe TS-113 in [[2026-11-08-fehlteile-karte-meine-fehlteile-lager-glas-spec]]. Zeilennummern Stand Bündel-Worktree v1.78.0 (nach v1.75.0).

## Root Cause

- `GetShortageCountsForUserAsync` kennt keinen Bestelltyp (Repository `:469-493`, einziger Aufrufer `WarehouseRequisitionsController.cs:92`).
- Dropdown: `disabled` in `Index.cshtml:70`, Liste aus `GetAllAsync` (`MissingPartsController.cs:109`) statt der eigenen Werkbänke.
- Häkchen: kein verstecktes `false`-Feld, Parameter-Default `true` (`MissingPartsController.cs:32`).

## Fix / Verweis

[[2026-11-08-fehlteile-karte-meine-fehlteile-lager-glas-spec]]
