---
typ: backlog
status: erledigt
created: 2026-11-17
herkunft: "QA-Befund Q2-B1 zu [[2026-11-16-bestellvorschlaege-nachbesserung-spec]] (qa-agent 2026-11-17)"
standort: beide
---
# Artikelinfo summiert Bestand fremder Artikel (Teiltext-Suche)

Die Artikelinfo holt den Bestand mit `GetCurrentStockAsync(filterArticle: …)`. Das filtert per `Contains` auf Artikelnummer, Bezeichnung und Matchcode
(`StockMovementRepository.cs:29-32/90-93`), und `TotalStock` (`ArticleInfoViewModel.cs:14`) summiert alle Trefferzeilen – also auch Zeilen anderer
Artikel, deren Matchcode oder Bezeichnung die gesuchte Nummer enthält.

Beleg AKE-DEV (2026-11-17): betroffen sind EK007964, EK000214, EK002995, EK000420 (Artikel mit Meldebestand). EK002995 zeigt in der Artikelinfo einen Gesamtbestand
von 40, Liste und Sage zeigen 12; Lagerplatz LL;1;25;0 steht dreimal da. Falsch sind damit auch „Verfügbarer Bestand“ und der Meldebestand-Hinweis.

Dasselbe Muster steckt in `WarehousePickingController.cs:120/367` und in `WarehouseRequisitionsApiController.cs:324` (Lagerplatz-Text).

Vorschlag: Artikelinfo exakt nach `ArticleId` bzw. über `GetStockByArticleNumbersAsync(…, true)` filtern; die beiden anderen Stellen gleich mitziehen.
Dazu klein (Q2-B2): Der Hinweis „kommissionierte Mengen sind schon ausgebucht“ unter „Bedarfe“ stimmt an der AKE nicht, dort wird in den Wagen
umgebucht – Text angleichen.
