---
type: spec
title: "Artikelinfo und Lager-/Glasbestellung: Bestand nur des gesuchten Artikels (statt Teiltext-Treffer)"
slug: 2026-11-19-artikelinfo-bestand-exakt-bug-spec
status: InUmsetzung
created: 2026-11-19
updated: 2026-11-19
source_backlog: "[[2026-11-17-artikelinfo-bestand-teiltextsuche-bug]]"
task: ""
jira: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: beide
typ: bug
version_vorschlag: "nächste freie Patch-Version (aktuell 1.99.44)"
testszenario_vorschlag: "TS-168 (TS-165 reserviert, TS-166 vergeben, TS-167 von [[2026-11-19-updater-warntext-treffer-exitcode-spec]] vorgeschlagen; Dev-Lauf prüft erneut)"
affected_code:
  - "IdealAkeWms/Controllers/ArticlesController.cs:242-243 (Info: GetCurrentStockAsync(filterArticle: article.ArticleNumber))"
  - "IdealAkeWms/Models/ViewModels/ArticleInfoViewModel.cs:14 (TotalStock summiert alle Zeilen) und :26 (AvailableStock)"
  - "IdealAkeWms/Views/Articles/Info.cshtml:165-168 (Hinweistext unter „Bedarfe“, Q2-B2)"
  - "IdealAkeWms/Controllers/WarehousePickingController.cs:120 (Details) und :367 (Print)"
  - "IdealAkeWms/Controllers/Api/WarehouseRequisitionsApiController.cs:324 (GET api/warehouserequisitions/stock)"
  - "IdealAkeWms/Data/Repositories/IStockMovementRepository.cs:8-12 (nur XML-Doku: Teiltextsuche)"
  - "IdealAkeWms/Views/Help/Index.cshtml:517-523 (Abschnitt „Verfuegbar und Bedarfe“)"
  - "IdealAkeWms.Tests/Controllers/ArticlesControllerTests.cs, WarehousePickingControllerTests.cs, WarehouseRequisitionsApiControllerTests.cs"
open_questions:
  - "1. (Fachbereich: Lager) Reihenfolge im Lagerplatz-Text der Lager-/Glasbestellung: neu Hauptlagerplatz, dann Menge absteigend (wie Stückliste/Kommissionierliste) übernehmen (Empfehlung) – oder die bisherige Reihenfolge Hauptlagerplatz, dann Lagerplatz-Code beibehalten?"
  - "2. (Fachbereich: Lager) Kommissionierwagen im Lagerplatz-Text der Lager-/Glasbestellung: wie heute mit anzeigen (Empfehlung für diesen Bugfix) – oder künftig ausblenden wie in der Kommissionier-Stückliste?"
  - "3. Hinweistext unter „Bedarfe“ in der Artikelinfo (Q2-B2): Formulierungsvorschlag aus „Fachliche Anforderungen“ F4 so übernehmen?"
epic: false
etappen: []
deploy:
  web: true
  service: false
  migration: false
freigabe_entscheidung: "Rückfragen 1–3 Gerald 2026-11-19 20:10 (1 Reihenfolge wie bisher nach Code, 2 Kommissionierwagen im Lagerplatz-Text ausblenden, 3 F4 übernehmen); freigegeben 20:13"
freigabe_von: "Gerald Weichbold"
freigabe_am: "2026-11-19"
---

# Artikelinfo und Lager-/Glasbestellung: Bestand nur des gesuchten Artikels

## Ziel / Nutzen (das Warum)

Die Artikelinfo zeigt einen falschen Gesamtbestand. Sie addiert den Bestand **fremder** Artikel dazu, deren Artikelnummer, Bezeichnung
oder Matchcode die gesuchte Artikelnummer als Teiltext enthält. Gesamtbestand, „Verfügbarer Bestand“ und der Meldebestand-Hinweis
(„Unterschritten“) sind dann falsch. Das kann eine Bestellung verhindern, die nötig wäre. Gemessen auf AKE-DEV am 2026-11-17 (QA-Befund
Q2-B1 in [[2026-11-16-bestellvorschlaege-nachbesserung-spec]]): EK002995 zeigt in der Artikelinfo 40, die Bestellvorschläge und Sage
zeigen 12. Der Lagerplatz `LL;1;25;0` erscheint dreimal (12/24/4). Betroffen sind 4 Artikel mit Meldebestand
(5 Paare über den Matchcode: EK007964←EK008429, EK000214←EK005215, EK002995←EK008535+EK009966, EK000420←EK013503).

Derselbe Fehler steckt im Lagerplatz-Text der Lager-/Glasbestellung (Details, Druck, Entwurf-Bearbeitung). Dort stehen Lagerplätze und
Mengen fremder Artikel bei der Position. Wer danach kommissioniert, kann an den falschen Lagerplatz geschickt werden.

Dazu kommt Q2-B2: Unter „Bedarfe“ steht „kommissionierte Mengen sind schon ausgebucht und zählen nicht mehr“. An der AKE wird beim
Kommissionieren in den Wagen **umgebucht**. Diese Mengen zählen weiter als Bestand und als FA-Bedarf, bis sie zum FA ausgebucht werden.

> [!note] Bug-Datensatz
> Für diesen Lauf ist laut Auftrag nur diese Spec zu schreiben. Ein Eintrag in `bugs/` wurde **nicht** angelegt; die Koordination legt
> ihn bei Bedarf an und verlinkt ihn hier.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Artikelinfo (`/Articles/Info`): Bestand je Lagerplatz, Gesamtbestand, Verfügbarer Bestand, Meldebestand-Hinweis nur aus Bewegungen des
  gefundenen Artikels.
- Lagerplatz-Text der Lager-/Glasbestellung: `WarehousePickingController.Details` (:120), `.Print` (:367),
  `WarehouseRequisitionsApiController.Stock` (:324, genutzt von `Views/WarehouseRequisitions/Edit.cshtml:141`).
- Hinweistext unter „Bedarfe“ (`Info.cshtml:165-168`) und ein Satz in der Hilfe (`Help/Index.cshtml:517-523`).
- XML-Doku an `IStockMovementRepository.GetCurrentStockAsync`: `filterArticle` ist eine Teiltextsuche für Suchmasken.

**Out-of-Scope (bewusst nicht ändern)**
- `GetCurrentStockAsync` selbst und die Bestandsübersicht `/StockOverview` (`StockOverviewController.cs:61-63`): Dort ist die
  Teiltextsuche gewollt (Eingabefeld „Artikelnr. oder Bezeichnung“, `Views/StockOverview/Index.cshtml:20`).
- Die übrigen Aufrufer von `GetCurrentStockAsync` filtern nur nach Lagerplatz, nicht nach Artikel, und sind nicht betroffen:
  `StockMovementsController.cs:497-498, 518-519, 576-577, 603-604`, `StockMovementRepository.cs:518`.
- `GetMovementHistoryAsync(filterArticle)` (`StockMovementRepository.cs:322-327`): einziger Aufrufer ist die Suchmaske
  `StockMovementsController.cs:75` – Teiltext gewollt.
- Die übrigen `ArticleNumber.Contains(...)`-Filter (`ArticleRepository.cs:23/104`, `ProductionOrderRepository.cs:114/211`,
  `PartRequisitionRepository.cs:80`, `BfAuftragRepository.cs:49`, OSEON) sind Suchfelder, keine Bestandsrechnung.
- Die N+1-Abfrage je Position in Details/Print (eine Bestandsabfrage pro Position) bleibt wie heute; Bündeln wäre eine eigene Verbesserung.
- Zusammenführen der zwei Bestandsimplementierungen `GetCurrentStockAsync` / `GetStockByArticleNumbersAsync` (Altbestand, QA Q2,
  `2026-11-16-bestellvorschlaege-nachbesserung-spec.md:1010`).

## Fachliche Anforderungen

- **F1 Artikelinfo exakt:** Lagerplatz-Tabelle, „Gesamt“, Gesamtbestand, Verfügbarer Bestand und das Abzeichen „Unterschritten“ enthalten
  nur Bestand des angezeigten Artikels. Es zählen wie bisher alle Lagerplätze, auch Kommissionierwagen, inaktive und nicht buchbare Plätze.
  Dann zeigt die Artikelinfo denselben Gesamtbestand wie die Spalte *Bestand WMS* in den Bestellvorschlägen
  (`PurchaseProposalsController.cs:94`, `mitKommissionierwagen: true`).
- **F2 Lagerplatz-Text exakt:** In Details, Druck und Entwurf-Bearbeitung der Lager-/Glasbestellung stehen nur Lagerplätze mit positivem
  Bestand **des Positionsartikels**. Das Format bleibt `Code (Menge:N3)`, kommagetrennt. Groß/Klein und nachlaufende Leerzeichen in
  der Positions-Artikelnummer spielen keine Rolle (die Position speichert die Eingabe `body.ArticleNumber`,
  `WarehouseRequisitionsApiController.cs:98`; gefunden wurde der Artikel per SQL-Gleichheit, `ArticleRepository.cs:77`).
- **F3 Reihenfolge im Lagerplatz-Text:** **Entscheidung Gerald 2026-11-19 20:10: Hauptlagerplatz, dann Code (bisherige Reihenfolge).** Ursprünglich laut Rückfrage 1. Empfehlung: Hauptlagerplatz zuerst, dann Menge absteigend, dann Code. So
  liefert es `GetStockByArticleNumbersAsync` schon (`StockMovementRepository.cs:482-488`), ohne zusätzlichen Code. Bisher galt
  Hauptlagerplatz, dann Code (`StockMovementRepository.cs:188-192`).
- **F3a Kommissionierwagen im Lagerplatz-Text:** **Entscheidung Gerald 2026-11-19 20:10: ausblenden** (`mitKommissionierwagen: false` nur für den Lagerplatz-Text; Gesamtbestand unverändert mit Wagen). Ursprünglich laut Rückfrage 2. Empfehlung: wie heute mit anzeigen (`mitKommissionierwagen: true`),
  denn der Bugfix soll nur die fremden Artikel entfernen.
- **F4 Hinweistext (Q2-B2)**, Vorschlag (Rückfrage 3), gilt für beide Standorte:
  „Bedarfsmeldung und Lagerbestellung werden nicht zusätzlich zum FA-Bedarf gezählt (je Auftrag bzw. Artikel gilt der größere Wert).
  Ware im Kommissionierwagen zählt im Gesamtbestand und im FA-Bedarf, bis sie zum Fertigungsauftrag ausgebucht ist; erst die
  Ausbuchung senkt beide.“
  Beleg der Regel: Der FA-Rest ist BOM − Ausbuchungen mit `ProductionOrderId`, nur `MovementType.Ausbuchung`
  (`IStockMovementRepository.cs:60-65`). Der Gesamtbestand enthält Wagenplätze (`GetCurrentStockAsync` hat keinen Wagen-Ausschluss,
  `StockMovementRepository.cs:22-33`). QA hat gemessen, dass Bestand und Rest bei einer Wagen-Ausbuchung mit FA gemeinsam sinken
  (`2026-11-16-bestellvorschlaege-nachbesserung-spec.md:1050`). Anwenderdoku `docs/anwender/08-bestellvorschlaege.md:30` sagt dasselbe.

## Technischer Loesungsentwurf

Kein neuer Repository-Code. Für die Rechnung gibt es zwei vorhandene Bausteine (ponytail-Leiter, Sprosse 2):

1. **Artikelinfo – Nachfilter nach `ArticleId`** (`ArticlesController.cs:242-243`):
   `GetCurrentStockAsync(filterArticle: article.ArticleNumber)` bleibt, das Ergebnis wird auf `s.ArticleId == article.Id` gefiltert.
   Das ist der richtige Baustein, weil:
   - `StockOverviewItem` die Felder liefert, die die View braucht (`StorageLocationDescription`, `Unit`; `Info.cshtml:245-251`).
     `StockLocationInfo` aus `GetStockByArticleNumbersAsync` hat keine Lagerplatz-Bezeichnung.
   - Die Zeilen werden je `(ArticleId, StorageLocationId)` zusammengeführt (`StockMovementRepository.cs:134-136`). Die Zeilen des
     gefundenen Artikels sind also vollständig, egal welche Fremdzeilen der Teiltext noch trifft.
   - Der eigene Artikel steckt sicher im Teiltext-Ergebnis, weil die Suche mit der gespeicherten Artikelnummer selbst läuft
     (`article.ArticleNumber` aus der DB, `ArticlesController.cs:222/234`). Der Vergleich über die Id hängt nicht an Groß/Klein oder Leerzeichen.
   - Die Regel bleibt dieselbe wie bei den Bestellvorschlägen. Der Gleichheitstest
     `StockMovementRepositoryAggregationTests.GetStockByArticleNumbersAsync_MitKommissionierwagen_GleichArtikelinfo_Q2` (:228-247)
     deckt das weiter ab.
2. **Lagerplatz-Text – `GetStockByArticleNumbersAsync(new List<string> { artnr }, mitKommissionierwagen: true)`**
   an den drei Stellen `WarehousePickingController.cs:120`, `:367`, `WarehouseRequisitionsApiController.cs:324`:
   - Die Abfrage vergleicht in SQL auf Gleichheit (`articleNumbers.Contains(sm.Article.ArticleNumber)`, `StockMovementRepository.cs:408/438`).
     Groß/Klein und nachlaufende Leerzeichen behandelt dort die CI-Kollation (fallstricke §48, §26).
   - Weil genau **eine** Nummer abgefragt wird, gehören alle zurückgegebenen Einträge zu diesem Artikel. Der Aufrufer nimmt
     `dict.Values.SelectMany(x => x)` und schlägt **nicht** per Schlüssel nach. Der Schlüssel ist die DB-Schreibweise und kann von der
     Positions-Schreibweise abweichen (vgl. `KommissionierListenService.cs:73-75`, `PickingApiController.cs:39`).
   - Text: `Where(q > 0)` → `$"{Code} ({Quantity:N3})"`, so wie heute. Reihenfolge laut F3 (Empfehlung: unverändert aus der Methode).
     Bei „bisherige Reihenfolge“ sortiert der Aufrufer `OrderBy(IsPrimaryStorageLocation ? 0 : 1).ThenBy(Code)`.
   - `mitKommissionierwagen` laut F3a. Bei „Wagen ausblenden“ entfällt nur das Argument `true`.
   - Leere/`null`-Artikelnummer: `GetStockByArticleNumbersAsync` liefert bei leerer Liste ein leeres Dictionary (:400-401). Bei
     `articleNumber` = Leerstring oder `null` im API (`[FromQuery] string articleNumber`) vorher abbrechen und `locations = ""` liefern.
     Heute liefert `GetCurrentStockAsync` mit leerem Filter den **gesamten** Bestand aller Artikel (`StockMovementRepository.cs:27`) –
     dieser Pfad entfällt damit auch.
   - Der Text wird an drei Stellen gleich gebaut. Eine gemeinsame private/statische Hilfsfunktion ist erlaubt, aber nicht nötig.
3. **Schutz vor Wiederholung:** XML-Doku an `IStockMovementRepository.GetCurrentStockAsync`: „`filterArticle` = Teiltextsuche über
   Artikelnummer, Bezeichnung und Matchcode (Suchmasken). Für den Bestand eines bestimmten Artikels nicht verwenden, sondern nach
   `ArticleId` filtern bzw. `GetStockByArticleNumbersAsync`.“ Dazu ein Fallstrick-Eintrag (Dev-Lauf, Hauptcheckout).
4. **Text Q2-B2:** `Info.cshtml:165-168` durch den freigegebenen Wortlaut aus F4 ersetzen. In der Hilfe (`Help/Index.cshtml:517-523`)
   einen Satz ergänzen: Gesamtbestand = nur dieser Artikel, alle Lagerplätze einschließlich Kommissionierwagen; Wagenware zählt bis zur
   Ausbuchung zum FA in Bestand und FA-Bedarf.

Muster: Repository-Pattern unverändert ([[0001-repository-pattern-mit-decorator-fuer-caching]]). Der Sage-Decorator
`SageBookingEnqueueingStockMovementRepository` erbt von `StockMovementRepository` (`SageBookingEnqueueingStockMovementRepository.cs:21`,
`Program.cs:78`) und ist von reinen Lesemethoden nicht berührt. Keine neue Listen-View.

## Migrations-/SQL-Auswirkungen

Keine. Kein Schema, keine Migration, kein `SQL/XX_*.sql`, `00_FreshInstall.sql` unverändert, `SQL/AgentJobs/` nicht betroffen.

## Einstellungen

keine

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| – | – | – | – | – | – | – | – |

## Audit-Feld-Auswirkungen

Keine. Nur lesende Pfade; es wird nichts geschrieben.

## Rollen / Zugriff

Unverändert. Artikelinfo: `[RequireMasterDataReadAccess]` (`ArticlesController.cs:10`). Lager-/Glasbestellung:
`[RequireLagerProcessingAccess]` + `[RequireLagerbestellungAktiv]` (`WarehousePickingController.cs:10-11`), API
`[RequirePickingOrStockOrLagerbestellungAccess]` + `[RequireLagerbestellungAktiv]` (`WarehouseRequisitionsApiController.cs:11-12`).
Kein neuer Filter, keine Änderung an `controller.md` oder `RoleOverview.cshtml`.

## Akzeptanzkriterien

1. Artikelinfo EK002995 (AKE-DEV): Gesamtbestand = *Bestand WMS* in `/PurchaseProposals` für denselben Artikel (laut QA 12, nicht 40).
   `LL;1;25;0` steht genau einmal in der Lagerplatz-Tabelle.
2. Artikelinfo der anderen drei gemessenen Artikel (EK007964, EK000214, EK000420): Kein Lagerplatz/keine Menge der Fremdartikel
   EK008429, EK005215, EK013503 (bzw. EK008535, EK009966 bei EK002995). Die Summe = Summe nur der eigenen Bewegungen (Prüfabfrage A).
3. „Verfügbarer Bestand“ = neuer Gesamtbestand − „Angerechnet“. Das Abzeichen „Unterschritten“ erscheint genau dann, wenn
   Meldebestand > 0 und neuer Gesamtbestand < Meldebestand.
4. Ein Artikel ohne Teiltext-Fremdtreffer zeigt in der Artikelinfo dieselben Werte wie vor dem Fix (Regression).
5. Kommissionierwagen-Bestand des Artikels zählt in der Artikelinfo weiter mit (unverändert).
6. Lager-/Glasbestellung Details: Eine Position mit einem betroffenen Artikel zeigt nur Lagerplätze dieses Artikels mit positivem Bestand,
   Format `Code (Menge)`, Reihenfolge laut Rückfrage 1.
7. Dasselbe im Druck (`/WarehousePicking/Print/{id}`) und in der Entwurf-Bearbeitung (Zelle „Bestand“ über `api/warehouserequisitions/stock`).
8. Wagenplätze im Lagerplatz-Text: laut Rückfrage 2 (Empfehlung: wie heute sichtbar).
9. Positions-Artikelnummer in anderer Groß/Klein-Schreibweise als im Artikelstamm: Lagerplatz-Text trotzdem korrekt (nicht leer).
10. `GET api/warehouserequisitions/stock?articleNumber=` (leer) liefert `locations` leer und keine Gesamtbestandsliste.
11. Bestandsübersicht `/StockOverview` mit Teiltext (z. B. „EK0029“) findet weiter alle passenden Artikel (Teiltextsuche unverändert).
12. Hinweistext unter „Bedarfe“ = freigegebener Wortlaut (Rückfrage 3), auf AKE und IDEAL gleich; der Satz „kommissionierte Mengen sind
    schon ausgebucht“ steht nicht mehr da.
13. Automatische Tests (s. u.) grün; Build grün.

## Test-Szenarien

**Prüfabfrage A (vor dem Test, je DEV-Standort; nur lesend):** findet Artikel, deren Artikelinfo heute durch Fremdartikel gestört ist.

```sql
SELECT a.ArticleNumber, b.ArticleNumber AS Fremdartikel,
       CASE WHEN b.Matchcode LIKE '%' + a.ArticleNumber + '%' THEN 'Matchcode'
            WHEN b.Description LIKE '%' + a.ArticleNumber + '%' THEN 'Bezeichnung'
            ELSE 'Artikelnummer' END AS TrefferUeber
FROM Articles a
JOIN Articles b ON b.Id <> a.Id
 AND (b.ArticleNumber LIKE '%' + a.ArticleNumber + '%'
   OR b.Description  LIKE '%' + a.ArticleNumber + '%'
   OR b.Matchcode    LIKE '%' + a.ArticleNumber + '%')
WHERE EXISTS (SELECT 1 FROM StockMovements sm WHERE sm.ArticleId = b.Id)
  AND EXISTS (SELECT 1 FROM StockMovements sm WHERE sm.ArticleId = a.Id)
ORDER BY a.ArticleNumber;
```

Für AKE belegt durch QA (5 Paare, s. o.). Für IDEAL ist nicht gemessen, ob es Treffer gibt. Fehlen dort Treffer, wird der Fix an IDEAL
nur als Regression geprüft (AK 4, 11, 12).

**Neues Kapitel TS-168 (Vorschlag; „Artikelinfo und Lagerbestellung: Bestand nur des gesuchten Artikels“)** in `docs/TESTSZENARIEN.md`,
danach Zeile in [[testszenarien-index]]. Standort: DEV AKE (Datenlage) und DEV IDEAL (Regression).

- **TS-168.1 Artikelinfo EK002995 (AKE):** Vorbedingung Prüfabfrage A zeigt das Paar. Artikelinfo öffnen → Gesamtbestand = *Bestand WMS*
  in den Bestellvorschlägen; `LL;1;25;0` einmal; Verfügbar = Gesamt − Angerechnet; Abzeichen „Unterschritten“ passend (AK 1, 3).
- **TS-168.2 Weitere Treffer:** EK007964, EK000214, EK000420 – keine Fremd-Lagerplätze (AK 2).
- **TS-168.3 Regression Artikelinfo:** ein Artikel ohne Treffer in Prüfabfrage A, Werte wie vor dem Deploy (Screenshot vorher) (AK 4);
  ein Artikel mit Wagenbestand → Wagen in der Tabelle und im Gesamtbestand (AK 5).
- **TS-168.4 Lagerbestellung Details/Druck:** Lagerbestellung mit Position EK002995 (oder einem anderen Treffer) anlegen und absenden →
  Details und Druck zeigen nur eigene Lagerplätze, Reihenfolge laut Rückfrage 1, Wagen laut Rückfrage 2 (AK 6–8).
- **TS-168.5 Entwurf bearbeiten:** dieselbe Position im Entwurf → Zelle „Bestand“ wie in TS-168.4 (AK 7).
- **TS-168.6 Negativ:** Position mit Artikelnummer in Kleinbuchstaben anlegen (z. B. `ek002995`) → Lagerplatz-Text korrekt (AK 9).
  `api/warehouserequisitions/stock?articleNumber=` im Browser → `{"locations":""}` (AK 10).
- **TS-168.7 Bestandsübersicht:** `/StockOverview` Filter „EK0029“ → weiterhin mehrere Artikel (AK 11).
- **TS-168.8 Hinweistext:** Artikelinfo auf AKE und IDEAL → neuer Wortlaut unter „Bedarfe“ (AK 12).

Bestehende Szenarien anfassen: TS-154.12 (Artikelinfo-Aufschlüsselung) und TS-41 (Artikelinfo) per Querverweis auf TS-168 ergänzen,
falls sie den alten Hinweistext zitieren (Dev-Lauf prüft).

**Automatische Tests (Dev-Lauf):**
- `ArticlesControllerTests`: neuer Test „Info zählt nur Zeilen mit `ArticleId == article.Id`“ (Mock liefert Zeilen von Artikel 1 und 2,
  erwartet `TotalStock` und `StockByLocation` nur von Artikel 1). Der Helfer `Stock(decimal)` (:70-74) setzt heute keine `ArticleId`;
  die bestehenden Tests mit `Article.Id = 1` und `TotalStock`-Erwartung (z. B. :89-122, :178-202) brauchen `ArticleId = 1`, sonst
  filtert der Fix ihre Zeilen weg.
- `WarehousePickingControllerTests` (:34, :123, :164) und `WarehouseRequisitionsApiControllerTests`: Mocks von `GetCurrentStockAsync`
  auf `GetStockByArticleNumbersAsync(It.IsAny<List<string>>(), true)` umstellen. Neuer Test: zurückgegebener Schlüssel in anderer
  Schreibweise als die Position → Text trotzdem gefüllt. Neuer Test: leere `articleNumber` im API → leere `locations`, kein Repository-Aufruf.
- Repository-Tests bleiben unverändert (`GetCurrentStockAsync` wird nicht geändert).

## Etappen (nur bei epic: true)

Entfällt (kein Epic).

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | – | – | – |

## Deploy

*Vorläufig, der QA-Lauf bestätigt gegen den echten Diff.*

- **Web-App:** ja (Controller, View, Hilfe).
- **Service:** nein (`IDEALAKEWMSService` nutzt `GetStockByArticleNumbersAsync` nur in `StockCheckService.cs:43`, unverändert).
- **Migration:** nein.
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`):
  `@{ Id='v<Version>-ArtikelinfoBestandExakt'; Version='<Version>'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen';
  Text='Artikelinfo: Gesamtbestand zaehlt nur noch den angezeigten Artikel (vorher teils Fremdartikel ueber Matchcode/Bezeichnung). Zahlen und Meldebestand-Hinweis koennen sinken. Lager/Einkauf informieren.' }`
  – sonst keine.
- **Publish-Befehle:**
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
- Version: nächste freie Patch-Version (aktuell 1.99.44), in **beiden** `AppVersion.cs`; Anwender-Changelog (sichtbare Änderung
  „Gesamtbestand in der Artikelinfo korrigiert“), Brain-Changelog, Fallstrick, Testindex.

## Offene Rueckfragen

1. (Fachbereich: Lager) Reihenfolge im Lagerplatz-Text der Lager-/Glasbestellung: neu Hauptlagerplatz, dann Menge absteigend (wie
   Stückliste/Kommissionierliste) übernehmen (**Empfehlung**, kein Zusatzcode) – oder die bisherige Reihenfolge Hauptlagerplatz, dann
   Lagerplatz-Code beibehalten?
2. (Fachbereich: Lager) Kommissionierwagen im Lagerplatz-Text der Lager-/Glasbestellung: wie heute mit anzeigen (**Empfehlung** für
   diesen Bugfix, keine Verhaltensänderung außer dem Fehler) – oder künftig ausblenden wie in der Kommissionier-Stückliste?
3. Hinweistext unter „Bedarfe“ in der Artikelinfo (Q2-B2): Formulierungsvorschlag aus F4 so übernehmen?

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. (Fachbereich: Lager) Reihenfolge Lagerplatz-Text: neu (Hauptlagerplatz, Menge absteigend) oder bisherige (Hauptlagerplatz, Code)? → **wie bisher: Hauptlagerplatz, dann Lagerplatz-Code** (Gerald 2026-11-19 20:10, abweichend von der Empfehlung)
2. (Fachbereich: Lager) Kommissionierwagen im Lagerplatz-Text: wie heute anzeigen oder ausblenden? → **ausblenden** (wie Kommissionier-Stückliste; Gerald 2026-11-19 20:10, abweichend von der Empfehlung). Gesamtbestand und FA-Bedarf zählen Wagen weiter (F1/F4 unverändert).
3. Hinweistext „Bedarfe“ (Q2-B2): Vorschlag F4 übernehmen? → **ja, F4 übernehmen** (Gerald 2026-11-19 20:10)
