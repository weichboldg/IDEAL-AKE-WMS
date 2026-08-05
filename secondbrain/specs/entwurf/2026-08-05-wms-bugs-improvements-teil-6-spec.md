---
type: spec
title: "Lagerplatz ausbuchen: FA-Spalte in der Bestandstabelle + funktionsfaehige FA-Ausbuchung"
slug: 2026-08-05-wms-bugs-improvements-teil-6-spec
status: Entwurf
created: 2026-08-05
updated: 2026-08-05
source_backlog: "[[2026-08-05-WmsBugs&Improvements]]"
depends_on: ""
task: ""
worktree: ""
branch: ""
affected_code:
  - IdealAkeWms/Controllers/StockMovementsController.cs
  - IdealAkeWms/Data/Repositories/StockMovementRepository.cs
  - IdealAkeWms/Data/Repositories/IStockMovementRepository.cs
  - IdealAkeWms/Models/ViewModels/OutboundAllViewModel.cs
  - IdealAkeWms/Models/ViewModels/StockOverviewViewModel.cs (StockOverviewItem, ggf. neues Feld)
  - IdealAkeWms/Views/StockMovements/OutboundAll.cshtml
  - docs/TESTSZENARIEN.md
  - secondbrain/tests/testszenarien-index.md
open_questions:
  - "Kernfrage UX: Was bedeutet \"Aktivierung von FA Ausbuchung\" konkret? (A) Das bestehende Textfeld \"Fertigungsauftrag\" auf OutboundAllConfirm soll als ECHTER Filter wirken - es werden dann nur die Artikel ausgebucht, die (laut FA-getaggten Bewegungen) zu dieser FA gehoeren, statt wie heute ALLE Artikel am Lagerplatz. (B) Es soll je Zeile eine Checkbox/FA-Auswahl geben, mit der der Anwender manuell waehlt, welche Zeilen ausgebucht werden. Ohne diese Entscheidung ist der Fix nicht eindeutig spezifizierbar."
  - "Woher soll die FA-Spalte je Zeile ihren Wert nehmen, wenn ein Artikel/Lagerplatz-Kombination durch MEHRERE unterschiedliche FA-getaggte Bewegungen entstanden ist (z. B. zwei Einbuchungen mit unterschiedlicher FA-Nummer, die sich am selben Lagerplatz zum selben Artikel summieren)? Anzeige als kommaseparierte Liste, oder nur die juengste/haeufigste FA?"
  - "Ist die vorgeschlagene Datenquelle (Kandidaten aus GetProductionOrdersAtLocationAsync-aehnlicher Logik, aber PRO ARTIKEL statt aggregiert) mit der Bugfix-Spec Teil 1 (tatsaechlicher Bestand statt Bewegungssaldo) fachlich konsistent zu halten, oder duerfen beide Teile unabhaengig voneinander in beliebiger Reihenfolge gemergt werden?"
epic: false
etappen: []
deploy:
  web: true
  service: false
  migration: false
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
---

## Ziel / Nutzen (das Warum)

Auf der Seite „Lagerplatz ausbuchen" (`/StockMovements/OutboundAll`) soll der Lagermitarbeiter
erkennen können, zu welchem Fertigungsauftrag die auf dem Lagerplatz liegenden Artikel gehören,
und gezielt nur die Artikel eines bestimmten FA ausbuchen können. Aktuell zeigt die Tabelle keine
FA-Spalte, und das vorhandene FA-Textfeld beeinflusst die Auswahl der auszubuchenden Artikel
nicht — es wird beim Bestätigen ohnehin **immer** der gesamte Bestand des Lagerplatzes
ausgebucht.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
1. Anzeige einer FA-Spalte in der Bestandstabelle von `OutboundAll` (welcher FA-Tag steckt hinter
   den Bewegungen, die zu diesem Artikel/Lagerplatz-Bestand geführt haben).
2. Aktivierung der FA-Ausbuchung: das FA-Feld wirkt tatsächlich als Auswahl-/Filterkriterium
   dafür, welche Zeilen ausgebucht werden — Details siehe offene Rückfrage 1 (Varianten A/B).

**Out-of-Scope**
- Keine Änderung an der reinen „Alles ausbuchen"-Funktion für den Fall, dass **kein** FA-Wert
  gewählt/eingetragen ist — dieser Pfad bleibt wie heute (kompletter Lagerplatz-Bestand).
- Keine Änderung an `LocationTransfer`/`LocationTransferConfirm` (Lagerplatz-Umbuchung) — das
  Backlog nennt ausdrücklich nur „Lagerplatz ausbuchen".
- Keine Vermischung mit dem Bugfix aus Teil 1 (Bewegungssaldo vs. tatsächlicher Bestand) — beide
  Specs sind unabhängig mergbar, siehe offene Rückfrage 3.

## Fachliche Anforderungen

1. Die Tabelle auf `OutboundAll` zeigt zusätzlich zur bestehenden Spalte (Artikelnummer,
   Bezeichnung, Einheit, Bestand) eine Spalte „Fertigungsauftrag" mit dem/den FA-Tag(s), die zu
   diesem Artikel/Lagerplatz-Bestand gehören (leer, falls keine Bewegung mit FA-Tag existiert).
2. Trägt der Anwender im FA-Feld einen Wert ein (bzw. wählt eine Zeile aus — siehe offene
   Rückfrage 1), werden beim Bestätigen **nur** die entsprechend zugeordneten Artikel
   ausgebucht, nicht der gesamte Lagerplatz-Bestand.
3. Bleibt das FA-Feld leer, bleibt das bisherige Verhalten (kompletter Lagerplatz-Bestand wird
   ausgebucht) erhalten — kein Bruch der Rückwärtskompatibilität für den heute genutzten
   Hauptanwendungsfall.

## Ist-Zustand (Code-Referenzen)

`IdealAkeWms/Views/StockMovements/OutboundAll.cshtml:60-106`: Tabelle zeigt Spalten
Artikelnummer/Bezeichnung/Einheit/Bestand — **keine** FA-Spalte. Das Formular
(Zeilen 90-104) hat ein Textfeld `name="productionOrder"`, das per
`OutboundAllConfirm(int storageLocationId, string? productionOrder)`
(`IdealAkeWms/Controllers/StockMovementsController.cs:364-405`) verarbeitet wird:

```csharp
var allStock = await _stockMovementRepository.GetCurrentStockAsync(
    filterStorageLocationId: storageLocationId);
var itemsToOutbound = allStock.Where(s => s.CurrentQuantity > 0).ToList();
...
foreach (var item in itemsToOutbound)
{
    var movement = new StockMovement
    {
        ArticleId = item.ArticleId,
        Quantity = item.CurrentQuantity,
        StorageLocationId = storageLocationId,
        ProductionOrder = productionOrder,   // <- wird NUR als Metadaten-Tag der NEUEN Buchung verwendet
        MovementType = MovementType.Ausbuchung,
        ...
    };
    await _stockMovementRepository.AddAsync(movement);
}
```

**Befund:** `productionOrder` beeinflusst **ausschließlich** das Tag der neu erzeugten
Ausbuchungs-Bewegungen, **nicht** die Auswahl `itemsToOutbound` — die Schleife bucht immer
**alle** Artikel mit positivem Bestand am Lagerplatz aus, unabhängig vom eingetragenen FA-Wert.
Das erklärt exakt die Backlog-Beobachtung „Feld vorhanden, aber nicht funktionsfähig".

`OutboundAllViewModel` (`IdealAkeWms/Models/ViewModels/OutboundAllViewModel.cs:1-11`) und
`StockOverviewItem` (`IdealAkeWms/Models/ViewModels/StockOverviewViewModel.cs:17-32`) haben
**kein** Feld für einen FA-Bezug je Zeile.

Vorbild für „FA-Werte zu einem Lagerplatz ermitteln" existiert bereits, aber nur **aggregiert
über den ganzen Lagerplatz**, nicht pro Artikel-Zeile:
`StockMovementRepository.GetProductionOrdersAtLocationAsync`
(`IdealAkeWms/Data/Repositories/StockMovementRepository.cs:448-470`) — liefert eine
`List<string>` aller distinct `ProductionOrder`-Werte am Lagerplatz (nur für Artikel mit
aktuell positivem Bestand), aktuell nur für Kommissionierwagen genutzt
(`StockMovementsController.cs:352-358`, `vm.ProductionOrder = string.Join("; ", waNumbers)`).

## Technischer Lösungsentwurf

**Für Anforderung 1 (FA-Spalte):**
- Neue Repository-Methode (Name z. B. `GetProductionOrdersPerArticleAtLocationAsync` oder
  Erweiterung der bestehenden `OutboundAll`-Datenbeschaffung), die je `(ArticleId,
  StorageLocationId)`-Paar am gegebenen Lagerplatz die zugehörigen `ProductionOrder`-Tag-Werte
  liefert (analog `GetProductionOrdersAtLocationAsync`, aber gruppiert nach `ArticleId` statt
  über den ganzen Lagerplatz aggregiert).
- `StockOverviewItem` bzw. eine für `OutboundAll` spezifische Projektion um ein Feld
  `ProductionOrders` (`string?`, ggf. kommasepariert bei mehreren Werten — siehe offene
  Rückfrage 2) ergänzen.
- `OutboundAll.cshtml`: neue Tabellenspalte „Fertigungsauftrag".

**Für Anforderung 2 (FA-Ausbuchung aktivieren) — abhängig von offener Rückfrage 1:**
- **Variante A (Textfeld als Filter):** `OutboundAllConfirm` filtert `itemsToOutbound`
  zusätzlich auf Artikel, deren ermittelter FA-Tag (aus derselben Datenquelle wie die neue
  Spalte) den eingegebenen Wert enthält — analog der bestehenden `Contains`-Semantik in
  `GetStockByProductionOrderAsync`. Minimal-invasiv, keine neue UI-Interaktion nötig.
- **Variante B (Checkbox-Auswahl je Zeile):** `OutboundAll.cshtml` bekommt Checkboxen je Zeile
  (Default: alle angehakt, wenn kein FA-Filter aktiv), `OutboundAllConfirm` erhält eine Liste
  ausgewählter `ArticleId`s (`List<int>? selectedArticleIds`) und bucht nur diese aus. Mehr
  Kontrolle für den Anwender, aber größerer UI-Umbau.
- Empfehlung: **Variante A** zuerst umsetzen (kleinerer, klar abgegrenzter Fix, der die
  Backlog-Formulierung „Feld vorhanden aber nicht funktionsfähig → aktivieren" direkt löst);
  Variante B als mögliche spätere Erweiterung, falls Variante A in der Praxis nicht ausreicht.
  **Letztverbindlich ist die Antwort des Menschen auf offene Rückfrage 1.**

## Migrations-/SQL-Auswirkungen

Keine — beide Varianten sind reine Lese-/Filter-Logik auf der bestehenden `StockMovement`-Tabelle,
keine neue Spalte, kein neues Schema-Objekt.

## Audit-Feld-Auswirkungen

Keine Änderung an bestehenden Schreibpfaden — `OutboundAllConfirm` setzt Audit-Felder bereits
korrekt (`CreatedAt`/`CreatedBy`/`CreatedByWindows` aus `ICurrentUserService`); nur die
**Auswahlmenge** der auszubuchenden Zeilen ändert sich, nicht die Art, wie jede Zeile geschrieben
wird.

## Rollen- und Zugriffsfilter-Auswirkungen

Keine Änderung. `OutboundAll`/`OutboundAllConfirm` bleiben unter
`[RequireStockKeyUserAccess]` (admin, stock_keyuser, picking) — konsistent mit dem bisherigen
„Lagerplatz ausbuchen"-Feature (Rolle `stock_keyuser`).

## Listen-View-Pattern-Pflichten

`OutboundAll` ist keine paginierte Listen-View im Sinne von ADR 0005 (Einzelplatz-Detailansicht,
unpaginiert) — bleibt wie bisher eine begründete Ausnahme. Keine neuen Pagination-/
Server-Spaltenfilter-Pflichten durch diese Änderung.

## Akzeptanzkriterien

1. Auf `OutboundAll` zeigt die Bestandstabelle je Zeile den/die zugehörigen FA-Tag(s), falls
   vorhanden, sonst eine leere Zelle.
2. Wird das FA-Feld leer gelassen und bestätigt, bleibt das heutige Verhalten unverändert
   (kompletter Lagerplatz-Bestand wird ausgebucht) — Regressionsschutz.
3. Wird das FA-Feld mit einem Wert befüllt, der zu einer Teilmenge der Artikel am Lagerplatz
   passt, werden **nur** diese Artikel ausgebucht (Variante A) bzw. nur die vom Anwender
   ausgewählten Zeilen (Variante B) — je nach Klärung der offenen Rückfrage 1.
4. Wird das FA-Feld mit einem Wert befüllt, der zu **keinem** Artikel am Lagerplatz passt, wird
   entweder gar nichts ausgebucht (mit passender Warnmeldung) oder eine leere Auswahl verhindert
   das Absenden — kein stiller Fallback auf „alles ausbuchen".

## Test-Szenarien

Neues Szenario in `docs/TESTSZENARIEN.md` Kapitel 2 (Lager):
- **Vorbedingung:** Lagerplatz mit zwei Artikeln, Artikel A mit FA-Tag `1234567` eingebucht,
  Artikel B ohne FA-Tag eingebucht.
- **Schritte:** `/StockMovements/OutboundAll?storageLocationId=<id>` öffnen.
- **Erwartetes Verhalten:** Tabelle zeigt FA-Spalte, Artikel A zeigt `1234567`, Artikel B zeigt
  leere Zelle.
- **Schritte (Filter):** FA-Feld mit `1234567` befüllen, bestätigen.
- **Erwartetes Verhalten:** Nur Artikel A wird ausgebucht, Artikel B bleibt unverändert am
  Lagerplatz.
- **Negativfall (Regression):** FA-Feld leer lassen, bestätigen → beide Artikel werden
  ausgebucht (wie bisher).

`secondbrain/tests/testszenarien-index.md` Kapitel 2 entsprechend ergänzen.

## Deploy

- **Web-App:** ja.
- **Service:** nein.
- **Migration:** nein.
- **Publish-Befehle:**

```
dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
```

## Offene Rückfragen

1. Kernfrage UX: Was bedeutet „Aktivierung von FA Ausbuchung" konkret? (A) Das bestehende
   Textfeld „Fertigungsauftrag" auf `OutboundAllConfirm` soll als **echter Filter** wirken — es
   werden dann nur die Artikel ausgebucht, die (laut FA-getaggten Bewegungen) zu dieser FA
   gehören, statt wie heute ALLE Artikel am Lagerplatz. (B) Es soll je Zeile eine
   Checkbox/FA-Auswahl geben, mit der der Anwender manuell wählt, welche Zeilen ausgebucht
   werden. Ohne diese Entscheidung ist der Fix nicht eindeutig spezifizierbar.
2. Woher soll die FA-Spalte je Zeile ihren Wert nehmen, wenn eine Artikel/Lagerplatz-Kombination
   durch **mehrere** unterschiedliche FA-getaggte Bewegungen entstanden ist (z. B. zwei
   Einbuchungen mit unterschiedlicher FA-Nummer, die sich am selben Lagerplatz zum selben
   Artikel summieren)? Anzeige als kommaseparierte Liste, oder nur die jüngste/häufigste FA?
3. Ist die vorgeschlagene Datenquelle (Kandidaten analog `GetProductionOrdersAtLocationAsync`,
   aber pro Artikel statt lagerplatzweit aggregiert) mit der Bugfix-Spec
   [[2026-08-05-wms-bugs-improvements-teil-1-spec]] (tatsächlicher Bestand statt Bewegungssaldo)
   fachlich konsistent zu halten, oder dürfen beide Teile unabhängig voneinander in beliebiger
   Reihenfolge gemergt werden?

## Freigabe-Antworten (Mensch füllt aus — Schranke 1)

1. →
2. →
3. →

klären wir später - bitte im backlog belassen.

## Kritische Pruefung (2026-08-05)

**ZURUECKGESTELLT — keine Freigabe-Pruefung durchgefuehrt.** Der Mensch hat diese Anforderung
bewusst vertagt („klären wir später - bitte im backlog belassen", Command-Notiz „wir werden diese
anforderung später angehen"). Die drei Freigabe-Antworten sind **unbeantwortet** (nur Pfeile) — eine
Anwalt-des-Teufels-Freigabe-Pruefung waere gegenstandslos, weil es noch keine Entscheidungen zu
pruefen gibt. Die Spec bleibt als Entwurf in `specs/entwurf/` liegen und wird **nicht** finalisiert.

**Damit beim spaeteren Wiederaufgreifen nichts verloren geht** (kein Ersatz fuer eine echte Pruefung,
nur ein Merkzettel):
- **Kern-Entscheidung zuerst (open_question 1):** Was heisst „Aktivierung von FA-Ausbuchung"? —
  Variante **A** (das FA-Textfeld auf `OutboundAllConfirm` wirkt als **echter Filter**: nur die zu
  dieser FA gehoerenden Artikel werden ausgebucht, statt heute ALLE am Platz) vs. Variante **B**
  (Checkbox/Auswahl **je Zeile**). Ohne diese Wahl ist der Fix nicht spezifizierbar — das ist der
  erste Punkt, den der Mensch beim Wiederaufnehmen beantworten muss.
- **Mehrdeutige FA je Zeile (open_question 2):** wenn eine Artikel/Lagerplatz-Kombination aus
  mehreren FA-getaggten Bewegungen stammt — Anzeige als Liste oder nur juengste/haeufigste FA?
- **Konsistenz mit Teil 1:** Die FA-Spalten-Datenquelle beruehrt dieselbe „Bestand vs.
  Bewegungssaldo"-Frage wie Teil 1 (Bugfix FA-Hinweis). Beim Wiederaufgreifen mit dem dann
  finalisierten Teil-1-Verhalten (Variante B / nur Ist-Bestand) abgleichen.
- **Cross-Feature-Merkposten:** `OutboundAll` erzeugt `Ausbuchung`en ueber
  `IStockMovementRepository.AddAsync` — seit v1.28.0 haengt daran der Sage-Lagerbuchungs-Enqueue.
  Wenn die FA-Filterung (Variante A) die Menge der ausgebuchten Zeilen aendert, aendert sich damit
  auch die Menge der Sage-Meldungen; bei der Umsetzung mitdenken (kein Bypass des Repository-Pfads).

### Empfehlung

**ZURUECKGESTELLT (bewusst, durch den Menschen). Nicht freigeben; im Backlog/Entwurf belassen, bis
die Kern-Entscheidung (Variante A vs. B) getroffen ist. Danach normale /review-Runde nachholen.**