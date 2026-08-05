---
type: bug
title: "FA-Lagerplatz-Hinweis (\"FA … liegt bereits\") wertet Lagerbewegungen statt tatsächlichen Bestand aus"
status: offen
severity: mittel
created: 2026-08-05
affected_code:
  - IdealAkeWms/Data/Repositories/StockMovementRepository.cs (GetStockByProductionOrderAsync)
  - IdealAkeWms/Controllers/StockApiController.cs (GetStockByOrder)
  - IdealAkeWms/Controllers/StockOverviewController.cs (Index, FA-Filter-Zweig)
  - IdealAkeWms/Views/StockMovements/Inbound.cshtml (faStorageHint-Skript)
  - IdealAkeWms/Views/Tracking/OseonIndex.cshtml (Lagerbestand-Modal)
source_backlog: "[[2026-08-05-WmsBugs&Improvements]]"
spec: "[[2026-08-05-wms-bugs-improvements-teil-1-spec]]"
---

## Symptom

Bei der Einbuchung zeigt das Feld „Fertigungsauftrag" einen Hinweis „FA `<Nummer>` liegt
bereits: ..." mit einer Liste von Lagerplätzen/Mengen, auch wenn am genannten Lagerplatz
tatsächlich **kein** Bestand mehr vorhanden ist (z. B. weil der Artikel zwischenzeitlich wieder
ausgebucht wurde, ohne bei der Ausbuchung erneut die FA-Nummer einzutragen). Der Hinweis
täuscht damit einen Bestand vor, der real nicht mehr existiert, und kann Anwender zu falschen
Lagerplatz-Entscheidungen verleiten.

Derselbe fehlerhafte Datenpfad wird zusätzlich verwendet von:
- der Bestandsübersicht (`/StockOverview?filterProductionOrder=...`), wenn nach FA gefiltert
  wird,
- dem „Lagerbestand"-Modal in der OSEON-Teileverfolgung (`Views/Tracking/OseonIndex.cshtml`).

## Reproduktion

1. Artikel A per Einbuchung mit FA-Nummer `1234567` auf Lagerplatz `X` einbuchen (Menge > 0).
2. Denselben Artikel A wieder vollständig von Lagerplatz `X` ausbuchen — dabei das Feld
   „Fertigungsauftrag" auf dem Ausbuchungs-Formular **leer lassen** (das Feld ist optional,
   siehe `IdealAkeWms/Views/StockMovements/Outbound.cshtml`).
3. Erneut zur Einbuchung wechseln und FA-Nummer `1234567` in das Feld „Fertigungsauftrag"
   eintragen (`change`-Event, z. B. durch Tab/Klick weg vom Feld).
4. Ergebnis: Der Hinweis „FA `1234567` liegt bereits: Lagerplatz `X` — `<Menge>` Artikel A" wird
   weiterhin angezeigt, obwohl der tatsächliche Bestand von Artikel A auf Lagerplatz `X` (über
   **alle** Bewegungen, unabhängig vom FA-Tag) bei 0 liegt.

## Root Cause

`StockMovementRepository.GetStockByProductionOrderAsync(string productionOrder)`
(`IdealAkeWms/Data/Repositories/StockMovementRepository.cs:190-248`) berechnet **keinen**
tatsächlichen Lagerbestand, sondern ausschließlich den Netto-Saldo der Bewegungen, deren Feld
`StockMovement.ProductionOrder` die gesuchte FA-Nummer **enthält** (`Contains`-Filter,
Zeile 196). Für jede gefundene Bewegung wird ihr Mengen-Vorzeichen nach `MovementType`
aufsummiert (Einbuchung/SageEinbuchung/Umbuchung positiv, Ausbuchung/SageAusbuchung negativ,
Zeilen 209-217) und nach `(ArticleId, StorageLocationId)` gruppiert.

Das Problem: Wird eine Gegenbewegung (insbesondere eine Ausbuchung) **ohne** das FA-Tag gebucht
— was auf allen drei Buchungs-Formularen (Einbuchung, Ausbuchung, Umbuchung) möglich ist, weil
`StockMovementCreateViewModel.ProductionOrder` optional ist (`IdealAkeWms/Models/ViewModels/
StockMovementCreateViewModel.cs:20-22`, kein `[Required]`) — fließt diese Gegenbewegung **nicht**
in die Summe ein. Der berechnete „Bestand je FA" bleibt dadurch dauerhaft positiv, selbst wenn der
tatsächliche physische Bestand am Lagerplatz (Summe **aller** Bewegungen, unabhängig vom FA-Tag —
vgl. `GetCurrentStockAtLocationAsync`, Zeilen 426-446, bzw. `GetCurrentStockAsync`, Zeilen 13-188)
längst bei 0 oder darunter liegt.

Kurz: Die Methode beantwortet „was wurde jemals unter diesem FA-Tag bewegt (Netto)?" statt „was
liegt aktuell tatsächlich am Lagerplatz, den dieser FA berührt hat?" — genau die im Backlog
beschriebene Verwechslung von Bewegungen und Bestand.

## Fix / Verweis

Siehe [[2026-08-05-wms-bugs-improvements-teil-1-spec]] für den Lösungsentwurf (Kandidaten-Locations
aus FA-getaggten Bewegungen ermitteln, tatsächlichen Bestand dort aber über **alle** Bewegungen —
nicht nur FA-getaggte — berechnen, nur Zeilen mit realem Bestand > 0 zurückgeben).
