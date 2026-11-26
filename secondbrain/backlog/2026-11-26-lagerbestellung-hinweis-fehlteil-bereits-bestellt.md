---
type: backlog
typ: feature
status: offen
created: 2026-11-26
quelle: Gerald im Chat 2026-11-26
---
# Lagerbestellung: Hinweis „bereits als Fehlteil bestellt“ mit Liefertermin

## Wunsch (Gerald, wörtlich sinngemäß)
Wenn der Werker eine neue Lagerbestellung erfasst und einen Artikel bestellt, der für ihn schon ein **Fehlteil** ist, soll eine
Info erscheinen: „Wurde bereits bestellt mit Lagerbestellung *xy*, ist als Fehlteil; geplantes Lieferdatum des Artikels:
*Lieferdatum (nächstmögliches)*“.

## Bezug
- Baut auf v1.99.85 auf ([[2026-11-26-liefertermin-offene-bestellungen-artikelinfo-lagerbestellung-spec]]): nächster Liefertermin
  offener Sage-Bestellungen je Artikel inkl. Rest-Regel (`WarehousePickingController.LieferterminZelle`, Cache `SagePurchaseOrderLines`)
  – wiederverwenden, nicht neu rechnen.
- Fehlteil = `WarehouseRequisitionItem.ShortageStatus == WillBeRestocked` (gesetzt beim Abschließen durch das Lager,
  `WarehousePickingController.cs:323`); Listen „Fehlteile“ (`MissingPartsController`, `MissingPartsLagerController`,
  Repository `WarehouseRequisitionRepository.cs:487`).
- Erfassung: `WarehouseRequisitionsController` (Entwurf `CreateDraft`/`Edit`, Positions-Hinzufügen per Artikelsuche).

## Entschieden
- **„Für ihn“ = derselbe Benutzer/dieselbe Werkbank wie „Meine Fehlteile“** (bestehende Logik übernehmen; Gerald im Chat 2026-11-26).
- **Zeitpunkt: sofort bei der Eingabe**, direkt nachdem der Artikel der Liste hinzugefügt wurde (nicht erst beim Abschicken; Gerald im Chat 2026-11-26).

## Für die Spec zu klären (belegen, nicht annehmen)
- „Für ihn“: gleiche **Werkbank** (Fehlteil-Listen filtern nach Werkbank-Zuordnung) oder gleicher Benutzer? Vorschlag: die Werkbank
  der neuen Liste, wie „Nur meine Werkbänke“.
- Wann zählt ein Fehlteil als offen (Liste noch nicht nachgeliefert/erledigt)? Regel der Fehlteil-Liste übernehmen.
- Zeitpunkt der Info: beim Hinzufügen der Position (sofort) und/oder beim Abschicken; nur Hinweis, **kein** Blockieren.
- Mehrere Fehlteil-Listen zum Artikel: alle Nummern nennen oder die jüngste + „(+N)“.
- Kein Liefertermin (Cache leer/IDEAL, keine offene Bestellung): Text ohne Datum bzw. „kein offener Liefertermin“.
- Glasbestellung gleich behandeln?
