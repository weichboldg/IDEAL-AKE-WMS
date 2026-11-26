---
type: backlog
typ: feature
status: offen
created: 2026-11-26
quelle: Gerald im Chat 2026-11-26 (Brainstorming, Entwurf freigegeben)
---
# Liefertermin offener Bestellungen: Artikelinfo + Lagerbestellung-Details

## Wunsch
Das Lager soll sehen, wann eine Lieferung für einen Artikel kommen sollte.

## Abgestimmter Entwurf (Gerald 2026-11-26)
- **Datenquelle:** vorhandener Cache `SagePurchaseOrderLines` (offene EBB + EBZ, `ArticleId`, `Liefertermin`, `MengeOffen`,
  Lieferant, `Belegnummer`; `Models/SagePurchaseOrderLine.cs`). Kein neuer Sync, keine Migration erwartet.
- **Artikelinfo** (`ArticlesController.Info`, `Views/Articles/Info.cshtml`): neue Karte „Offene Bestellungen“ –
  Beleg, Lieferant, offene Menge, Liefertermin; nach Termin sortiert; überfällig zusätzlich als Text „überfällig“ (Farbe nie allein).
  Ohne offene Bestellung: Hinweis statt leerer Karte (Muster wie Karte „Lieferanten“).
- **Lager: Eingehende Listen → Details** (`Views/WarehousePicking/Details.cshtml`): Infospalte „Liefertermin“ über die
  Spaltenauswahl ein-/ausblendbar, **Standard ausgeblendet**; je Position der nächste Termin, bei weiteren offenen
  Bestellungen Zusatz „(+N)“, z. B. „12.12.2026 (+1)“; Link auf die Artikelinfo.

## Verworfen
- Alle Termine in der Zelle (Tabelle zu breit), Live-Lesen aus Sage (Cache existiert).

## Für die Spec offen
- Standort: Cache je Standort/Mandant – gilt das für AKE und IDEAL gleichermaßen? (belegen, nicht annehmen)
- Einheit der offenen Menge (Basiseinheit lt. Cache) vs. Einheit der Lagerbestellung.
- Zählt eine überfällige Bestellung als „nächster Termin“ in der Spalte? (Vorschlag: ja, mit „überfällig“).
