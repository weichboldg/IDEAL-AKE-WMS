---
type: bug
title: "AKE-Regression DEV v1.99.12: Filter mit ; nicht genau, Bedarfsmeldung in UTC, Auftragsdruck ohne Lagerplatz, Wagen-Modal mit wachsender FA-Liste"
status: offen
severity: mittel
created: 2026-11-13
affected_code:
  - IdealAkeWms/Services/ColumnFilterSyntax.cs
  - IdealAkeWms/wwwroot/js/column-filter-syntax.js
  - IdealAkeWms/Controllers/Api/PartRequisitionsApiController.cs
  - IdealAkeWms/Data/Repositories/PartRequisitionRepository.cs
  - IDEALAKEWMSService/Services/PartRequisitionEmailService.cs
  - IdealAkeWms/Controllers/PickingController.cs
  - IdealAkeWms/Services/PickingTransferService.cs
  - IdealAkeWms/Data/Repositories/StockMovementRepository.cs
  - IdealAkeWms/Views/Picking/Bom.cshtml
source_backlog: "[[2026-11-12-befunde-ake-regression-dev]]"
spec: "[[2026-11-12-befunde-ake-regression-dev-spec]]"
---

## Symptom

Aus [[2026-11-12-ake-regression-dev-v1-99-browser-testbericht]] (AKE-DEV, v1.99.12):
1. Spaltenfilter Lagerplatz „GL;0;0;0“ trifft 3302 statt 26 Zeilen; `;`/`,` lassen sich nicht suchen (beide Standorte).
2. Bedarfsmeldung „Bestellt am“ 19:06 statt 20:06 (eine Stunde zu früh, im Sommer zwei).
3. Auftragsdruck (`/Picking/AuftragDrucken/41`): Spalte Lagerplatz in allen 185 offenen Zeilen leer.
4. Modal „Kommissionierwagen belegt“: FA-Liste wiederholt sich, wächst, läuft rechts aus dem Modal.

Nicht in diesem Record, weil kein Codefehler: FA-Hinweis Einbuchung (Menge = Platzbestand, so in AK7 von
[[2026-08-05-wms-bugs-improvements-teil-1-spec]] entschieden) und Prio 32 (Regel MAX+1). BV4 (Tausendertrenner) ausgeklammert.

## Reproduktion

1. `/StockOverview`, Filter Spalte Lagerplatz `GL;0;0;0` → ODER-Treffer; `=GL;0;0;0` → ebenfalls ODER („genau gl“ oder „enthält 0“).
2. Offener Kommissionierauftrag → „Bestellen“ → `/PartRequisitions` „Bestellt am“ mit Uhr vergleichen.
3. Auftrag freigeben, ohne Umbuchen „Auftrag drucken“.
4. Wagen mit Bestand fremder FA mehrfach per „Trotzdem buchen“ belegen, dann erneut umbuchen.

## Root Cause

1. `ColumnFilterSyntax.cs:250-254` / `column-filter-syntax.js:78-79` trennen ohne Escape.
2. `PartRequisitionsApiController.cs:106`, `PartRequisitionRepository.cs:115-116`, `:128`, `:130` schreiben `DateTime.UtcNow`,
   `PartRequisitionEmailService.cs:269` `GETUTCDATE()`; Anzeige ohne Umrechnung (Projektkonvention Lokalzeit).
3. `PickingController.cs:678` liest nur `PickingOrderLine.SourceStorageLocationId`, das nur beim Umbuchen gesetzt wird
   (`PickingTransferService.cs:230`).
4. `PickingTransferService.cs:102` verkettet die ganzen FA-Texte am Wagen; `StockMovementRepository.cs:510-532` liefert sie
   unzerlegt; Modal-Absatz `Bom.cshtml:509` ohne Umbruch. Spalte `ProductionOrder` max. 500 Zeichen (`StockMovement.cs:20`).

## Fix / Verweis

[[2026-11-12-befunde-ake-regression-dev-spec]]
