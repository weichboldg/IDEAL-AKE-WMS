---
type: spec
title: "IDEAL-Leitstand: nur HauptFA-Zeilen anzeigen, Freigabe-Haken am HauptFA (Schalter „Sub-FAs anzeigen“)"
slug: 2026-11-26-beschichtung-summe-leitstand-hauptfa-kommissionierauftrag-liste-teil-2-spec
status: Gemerged
created: 2026-11-26
updated: 2026-11-28
source_backlog: "[[2026-11-26-beschichtung-summe-leitstand-hauptfa-kommissionierauftrag-liste]]"
uebersicht: "[[2026-11-26-beschichtung-summe-leitstand-hauptfa-kommissionierauftrag-liste-uebersicht]]"
depends_on: "[[2026-11-26-beschichtung-summe-leitstand-hauptfa-kommissionierauftrag-liste-teil-1-spec]]"
task: ""
jira: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: IDEAL
affected_code:
  - IdealAkeWms/Data/Repositories/IProductionOrderRepository.cs + ProductionOrderRepository.cs (nurHauptFa und CountGruppenOhneHauptFaAsync gibt es seit v1.99.82 - wiederverwenden; NEU nur der optionale Parameter faNrUeberSubFa = true an GetForLeitstandGroupedAsync/BuildLeitstandQuery Z. 54-180 und eine Batch-Methode für die Zeilen-Ids der Seiten-Aufträge)
  - IdealAkeWms/Controllers/PickingLeitstandController.cs (Index Z. 73-292, Parameter mitSubFa, Zusammenfassung vor den Memory-Filtern Z. 240-254)
  - IdealAkeWms/Services/LeitstandHauptFaZusammenfassung.cs (NEU, reine statische Funktion, Regel je Spalte Z1-Z6 laut Antwort 6; VK-VA-Regel eigene, nicht AgAggregieren)
  - IdealAkeWms/Models/ViewModels/PickingLeitstandViewModel.cs (NurHauptFa, MitSubFa, GruppenOhneHauptFa; PickingLeitstandRowContext.NurHauptFa; PickingLeitstandItem.SubFaAnzahl)
  - IdealAkeWms/Views/PickingLeitstand/Index.cshtml (Filterkarte Schalter, flache Darstellung, Zählzeile, Banner)
  - IdealAkeWms/Views/PickingLeitstand/_PickingLeitstandRow.cshtml + Skript in Index.cshtml (Knopf „Alle Sub-FAs fertigmelden“ in der Zeilen-Aktion, nur HauptFA-Zeile; zusammengefasste Zellen zeigen die Zusammenfassung, schreiben die HauptFA-Zeile, Antwort 7; bei SubFaBestimmt Neuladen nach Speichern + Hinweis, Antwort 8; gesperrt nur W4 und W3 (LackNurSubFa), Antwort 9)
  - IdealAkeWms/wwwroot/js/fa-work-step-status.js (nur additiv: CustomEvent wms:fa-status-gespeichert nach resp.ok, Antwort 8; FA-Abarbeitungsliste unverändert)
  - IdealAkeWms.Tests (Repository InMemory faNrUeberSubFa, Unit-Tests Zusammenfassung, Controller; BESTEHENDE brechen und werden angepasst (Kritische Prüfung S-3) - Moq-Setups/Verifies von GetForLeitstandGroupedAsync mit 8 It.IsAny in PickingLeitstandControllerTests.cs:172/:230/:246/:1050 und ProductionOrdersControllerTests.cs:118/:678 (CS0854, optionaler Parameter in Expression Tree), Gruppen-Tests vm.Groups PickingLeitstandControllerTests.cs:185/:242/:269 rufen Index(..., mitSubFa: true))
  - IdealAkeWms/Views/Help/Index.cshtml (Abschnitt „Leitstand (Kommissionier-Freigabe)“: Lack-T bei „Lackteile nur an Sub-FAs“ über „Sub-FAs anzeigen“ erledigen (B-1), Eigenwert der HauptFA nur mit Schalter sichtbar (H-1), HauptFA erledigt = Auftrag weg (S-1)), Changelog
  - scripts/deploy/nachlauf.psd1
  - docs/TESTSZENARIEN.md
open_questions:
  - "1 (entschieden Gerald 2026-11-26: Freigabe-Haken und Freigeben nur noch am HauptFA, Sub-FAs werden nicht gesondert freigegeben; P-1/P-2 entfallen) Freigabe-Haken am HauptFA: Der Code zeigt ihn bereits an der HauptFA-Zeile (seit v1.46.0). Wo fehlte er konkret (Seite, HauptFA-Nummer, Benutzer)? Bitte die Prüfabfragen P-1 bis P-3 ausführen - liegt ein Daten- oder Rechteproblem vor, wird es hier ergänzt."
  - "2 (entschieden Gerald 2026-11-26: ja, Standard aus, wird eigentlich nicht benötigt) Schalter „Sub-FAs anzeigen“ in der Filterkarte (Standard aus) für die heutige Gruppenansicht mit Glas/Zukauf/Lack-T/VK-VA je Sub-FA - gewünscht?"
  - "3 (entschieden Gerald 2026-11-26: ja, eine Zeile je HauptFA ohne Gruppenkopf; auch mit Schalter kein Freigabe-Haken an Sub-FA-Zeilen) Darstellung im Nur-HauptFA-Modus: eine Zeile je HauptFA ohne Gruppenkopf, Knopf „Alle Sub-FAs fertigmelden“ wandert in die Zeilen-Aktionen der HauptFA-Zeile - einverstanden?"
  - "4 (entschieden Gerald 2026-11-26: Filter auf die HauptFA-Zeile; Suche über Sub-FA-Nummer nur mit eingeschaltetem Schalter) Filter im Nur-HauptFA-Modus wirken auf die HauptFA-Zeile; Ausnahme FA-Nr.: eine Sub-FA-Nummer findet ihren HauptFA - einverstanden?"
  - "5 (entschieden Gerald 2026-11-26, abweichend von der Empfehlung: die HauptFA-Zeile zeigt die Werte des HauptFA inkl. aller Sub-FAs zusammengefasst) Spalten Lack-T, Glas, Zukauf, VK-VA zeigen an der HauptFA-Zeile nur deren eigene Werte (keine Zusammenfassung der Sub-FAs) - einverstanden?"
  - "6 (entschieden Gerald 2026-11-26: Tabelle Z1-Z6 ja, aber VK-VA gemischt = In Bearbeitung; alle fertig -> fertig, alle offen -> offen, sonst In Bearbeitung) Zusammenfassungsregel je Spalte (Tabelle Z1-Z6): Lack-T vorhanden = irgendeine Zeile, Lack-T erledigt = alle Zeilen mit Lackierteilen; Glas/Zukauf = irgendeine Zeile; VK-VA = schlechtester Stand (alle fertig -> fertig, irgendeine in Bearbeitung -> in Bearbeitung, sonst offen); stornierte Sub-FAs zählen nicht, erledigte zählen mit; alle übrigen Spalten bleiben Werte der HauptFA-Zeile - einverstanden?"
  - "7 (entschieden Gerald 2026-11-26: Variante c, ein Klick an der HauptFA-Zeile schreibt nur die HauptFA-Zeile; Sub-FA-Werte bleiben, Anzeige bleibt zusammengefasst) Bedienbarkeit der zusammengefassten Zellen (Lack-T-Haken, Glas, Zukauf, VK-VA-Auswahl schreiben heute je Zeile): nur Anzeige (gesperrt, Bearbeiten über „Sub-FAs anzeigen“), oder schreibt ein Klick an der HauptFA-Zeile auf alle Zeilen des Auftrags, oder nur auf die HauptFA-Zeile?"
  - "8 (entschieden Gerald 2026-11-26: (ii) bedienbar, nach dem Speichern Neuladen und Hinweis „Sub-FA bestimmt den Wert weiter“; nur W4 bleibt gesperrt, weil an der HauptFA-Zeile nichts schreibbar ist) Folge von Antwort 7 (c): Wo Sub-FAs den zusammengefassten Wert bestimmen, ist der Klick an der HauptFA-Zeile nach dem Neuladen nicht sichtbar oder wirkungslos (Fälle W1-W4 im Abschnitt Klickverhalten, z. B. Glas abwählen, während eine Sub-FA Glas trägt). Wie soll die Zelle dort aussehen: (i) gesperrt mit Text „bestimmt durch Sub-FA …“, (ii) bedienbar, nach dem Speichern sofort neu berechnet und mit Hinweis „Sub-FA bestimmt den Wert weiter“, oder (iii) bedienbar ohne besondere Behandlung?"
  - "9 (entschieden Gerald 2026-11-26: (b) nur die W3-Zelle sperren mit „Lackteile nur an Sub-FAs“) Folge von Antwort 8: W3 (Lack-T erledigt an einer HauptFA-Zeile ohne eigene Lackteile) wirkt nie - die Zeile zählt in Z2 nicht, der Materialisierungslauf kann den Wert zurücksetzen. (a) wie Antwort 8 bedienbar mit Neuladen und Hinweis, oder (b) nur diese Zelle sperren mit Text „Lackteile nur an Sub-FAs – ändern mit „Sub-FAs anzeigen““? (Dev-Lauf baut bis zur Antwort (a))"
epic: false
etappen: []
deploy:
  web: true
  service: false
  migration: false
freigabe_entscheidung: "Antworten 1-9 + Kritische Pruefung (Gerald 2026-11-26): eine Zeile je HauptFA, Schalter Sub-FAs anzeigen, Freigabe nur HauptFA, W3 gesperrt (9b), HauptFA erledigt ohne Banner"
freigabe_von: Gerald
freigabe_am: 2026-11-26
---

## Ziel / Nutzen (das Warum)

An der IDEAL wird nur am HauptFA freigegeben und kommissioniert ([[2026-09-25-kommissionierung-nur-hauptfa-spec]], v1.46.0).
Der Leitstand zeigt im hierarchischen Modus trotzdem jede Sub-FA als eigene Zeile. Die Leitstand-Bediener suchen zwischen vielen
Sub-FA-Zeilen ohne Bedienelement die eine HauptFA-Zeile mit dem Freigabe-Haken. Nur HauptFA-Zeilen machen die Seite kurz und die
Freigabe eindeutig.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope (nur hierarchischer Modus, Master `ProduktionsauftragHierarchisch = true`)**

- `/PickingLeitstand` zeigt standardmäßig nur HauptFA-Zeilen, eine Zeile je HauptFA ohne Gruppenkopf (Antwort 3).
- Die HauptFA-Zeile zeigt in Lack-T, Glas, Zukauf und VK–VA die **Zusammenfassung aus HauptFA und allen Sub-FAs** (Antwort 5,
  Regel je Spalte siehe „Zusammenfassungsregel“, bestätigt mit Antwort 6). Ein Klick an der HauptFA-Zeile schreibt nur die HauptFA-Zeile
  (Antwort 7); bestimmen Sub-FAs den Wert mit, lädt die Seite nach dem Speichern neu und die Zelle zeigt den Hinweis „Sub-FA bestimmt
  den Wert weiter“ (Antwort 8).
- Schalter „Sub-FAs anzeigen“ in der Filterkarte, Standard aus (Antwort 2), stellt die heutige Gruppenansicht her.
- Banner, wenn Aufträge keine HauptFA-Zeile haben (sonst würden sie still verschwinden).
- Freigabe-Haken und „Freigeben“ nur an der HauptFA-Zeile, in beiden Ansichten (Antworten 1 und 3). Das ist Bestand seit v1.46.0;
  diese Spec sichert ihn mit Akzeptanzkriterien und Tests ab, es kommt keine neue Freigabe-Logik dazu.

**Out-of-Scope**

- Flacher Modus (AKE): unverändert, Code-Pfad `PickingLeitstandController.cs:294-` wird nicht berührt.
- FA-Liste `/ProductionOrders`: nutzt `GetForLeitstandGroupedAsync(..., nurHauptFa: true)` seit v1.99.82
  (`ProductionOrdersController.cs:250-252`) und behält ihre FA-Nr.-Suche über Sub-FA-Nummern (F2 der Spec
  [[2026-11-26-fa-liste-nur-hauptfa-und-fa-struktur-neu-teil-1-spec]]). Der neue Parameter hat den Default, der das heutige
  Verhalten der FA-Liste erhält. Die FA-Liste bekommt **keine** Zusammenfassung (Antwort 5 betrifft nur den Leitstand).
- Freigabe-Logik, Kaskade „Alle Sub-FAs fertigmelden“, Kommissionieraufträge: nur Ort des Knopfs ändert sich (Antwort 3).
- Backlog [[2026-09-25-leitstand-subfa-readonly-stueckliste]] (schreibgeschützte Sub-FA-Stückliste): betrifft nur noch die
  Ansicht „Sub-FAs anzeigen“, bleibt eigener Eintrag.
- **HauptFA erledigt = Auftrag erledigt (Kritische Prüfung S-1, bestätigt Gerald 2026-11-26):** Ist die HauptFA-Zeile erledigt,
  fertig kommissioniert oder storniert (`showDone`-Filter `ProductionOrderRepository.cs:158-159`), verschwindet der Auftrag ohne Banner,
  auch wenn Sub-FAs offen sind; sichtbar mit „Erledigte anzeigen“. Das Banner (L5) zählt nur Aufträge **ganz ohne** HauptFA-Zeile
  (`:184-190`). Bewusste Abgrenzung, gleich wie Q-4 der FA-Liste.
- Kein Sammel-Schreibweg für Lack-T erledigt an Sub-FAs (Kritische Prüfung B-1, Gerald: „bei 9 (b) bleiben“): Bei „Lackteile nur
  an Sub-FAs“ (W3) wird über „Sub-FAs anzeigen“ erledigt gemeldet; Hilfetext und Manifest-Info sagen das ausdrücklich.

## Fachliche Anforderungen

### Ist-Stand (belegt; Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, AppVersion 1.99.81, nachgeprüft gegen 1.99.84)

> [!note] Nachprüfung 2026-11-26 (Stand v1.99.84)
> Seit v1.99.82 (Spec [[2026-11-26-fa-liste-nur-hauptfa-und-fa-struktur-neu-teil-1-spec]]) gibt es im Repository bereits
> `nurHauptFa` und `CountGruppenOhneHauptFaAsync`. Die Zeilennummern des Repositorys unten (`:54-92`, `:99-156`, `:107-111`) sind
> damit **überholt**; aktuell: `GetForLeitstandGroupedAsync` `ProductionOrderRepository.cs:54-93` (Parameter `nurHauptFa` `:62`),
> `BuildLeitstandQuery` `:100-180`, Nur-HauptFA-Zweig `:106-120` (`Where(ProductionOrder.IsHauptFa)`, FA-Nr. der Karte über
> Subquery auf **alle** Zeilen des Auftrags `:111-119`), Spaltenfilter `order-number` ebenso über den Auftrag `:168-174`,
> `CountGruppenOhneHauptFaAsync` `:184-190`. Der Leitstand ruft die Methode noch **ohne** `nurHauptFa` auf
> (`PickingLeitstandController.cs:208-209`). Controller- und View-Zeilen unten stimmen mit v1.99.84 überein.

- Hierarchischer Zweig: `GetForLeitstandGroupedAsync` lädt alle Zeilen der Seiten-Gruppen, je Gruppe nach `SubOrderNumber`
  sortiert (`Data/Repositories/ProductionOrderRepository.cs:54-92`). Paginiert wird über Gruppen (`:69-75`); `TotalRowCount` zählt
  Sub-FAs (`:67`). Filterbasis `BuildLeitstandQuery(..., hierarchical: true)` (`:99-156`); FA-Nr. trifft `OrderNumber` **oder**
  `SubOrderNumber` (`:107-111`).
- Dieselbe Methode nutzt die FA-Liste (`Controllers/ProductionOrdersController.cs:246`).
- View: ein `<tbody>` je Gruppe mit Kopfzeile „HauptFA …“, Badge „n Sub-FAs“ (`group.Items.Count`) und Knopf „Alle Sub-FAs
  fertigmelden“ (`Views/PickingLeitstand/Index.cshtml:199-234`, Knopf `:216-226`, Dialog `:372-409`, Skript `:460-`). Zählzeile
  „x Aufträge · y Sub-FAs“ (`:113-120`). Der flache Zweig rendert `Model.Items` ohne Gruppenkopf (`:242-256`).
- **Freigabe-Haken am HauptFA gibt es schon:** `_PickingLeitstandRow.cshtml:17` `isHauptFaRow = Model.Hierarchical && !item.IsSubFa`;
  Bulk-Checkbox `:20-31` bei `LeitstandAktiv && CanManagePickingRelease && !item.IsDone && (!Hierarchical || isHauptFaRow)`;
  Freigabe-Knopf `:241-285` mit derselben Bedingung, „Freigeben“ zusätzlich nur mit Artikelnummer (`:260`). `item.IsSubFa` kommt aus
  `ProductionOrder.IsSubFaOf` (`PickingLeitstandController.cs:163`), Definition `IsHauptFa = SubOrderNumber == "" || SubOrderNumber == OrderNumber`
  (`Models/ProductionOrder.cs:29-40`).
- Die HauptFA-Zeile entsteht an der IDEAL nur, wenn der Strukturknoten mit `SubFA = HauptFA` materialisiert ist
  (`IDEALAKEWMSService/Services/FaMaterializationSyncService.cs:76-83`, `:149-153`: `SubOrderNumber = n.SubFA`). Laut Spec
  [[2026-11-18-ideal-kommissionierliste-filter-spalten-leitstand-spec]] A5 ist der Wurzelknoten `SubFA = HauptFA`. Fehlt sie, gibt es
  in der Gruppe keinen Haken.
- *(überholt durch Antwort 1 - der Befund wird nicht weiter verfolgt, P-1/P-2 entfallen)* Möglichkeiten, warum der Haken am
  HauptFA fehlt (aus dem Code, am Gerät unbelegt): `LeitstandAktiv` aus (`Controller :193-194`;
  Freigabe-Antwort 3 der Spec 2026-11-18: „Leitstand an IDEAL wird aktiviert“), Benutzer ohne Leitstand-Recht
  (`CanManagePickingRelease`, `:198`), HauptFA erledigt (`IsDone || IsDonePicking`, `:140`), keine HauptFA-Zeile. → P-1 bis P-3.
- Leitstand-Zugriff: Klasse `[RequirePickingOrLeitstandAccess]` (`PickingLeitstandController.cs:12`), Freigabe-Aktionen
  `[RequireLeitstandAccess]` (`:377`, `:465`).
- **Sub-FAs sind serverseitig nicht freigebbar** (Antworten 1 und 3 sind damit schon abgesichert): `ToggleRelease` weist Sub-FAs
  ab (`PickingLeitstandController.cs:384-390`, „Kommissionierung erfolgt nur am HauptFA“), `BulkRelease` überspringt sie mit
  Sammelmeldung (`:526`, `:549`). In der View fehlen Bulk-Haken, Stückliste und Freigabe-Knopf an Sub-FA-Zeilen
  (`_PickingLeitstandRow.cshtml:23`, `:35`, `:244`).

**Herkunft der Spaltenwerte, die Antwort 5 zusammenfassen soll** (alle je `ProductionOrder`-Zeile, d. h. je HauptFA- bzw. Sub-FA-Zeile):

- `MapItem` füllt Glas, Zukauf, Lack-T, Lack-T erledigt, Freigabe, Prio, Kommissionierer, Stückliste kleiner aus
  `ProductionOrderPickingStatus` der Zeile und VK–VA aus dem Arbeitsgang-Pivot der Zeile (`PickingLeitstandController.cs:125-164`,
  Laden `:218-220` über `GetWorkStepDetailPivotAsync`/`GetByProductionOrderIdsAsync` mit einer Id-Liste,
  `IFaWorkStepRepository.cs:26`, `IProductionOrderPickingStatusRepository.cs:17`).
- **Lack-T an der IDEAL:** `FaMaterializationCoating.Derive` setzt das Flag je Sub-FA aus „selbst beschichtet oder direktes Blatt-Kind
  beschichtet“; Kinder mit eigenem Sub-FA und Enkel zählen nur an ihrem eigenen Sub-FA
  (`IDEALAKEWMSService/Services/FaMaterializationCoating.cs:9-30`), geschrieben über `SetCoatingPartsAsync`
  (`FaMaterializationSyncService.cs:216-228`). Die HauptFA-Zeile trägt also heute **nicht** die Lackteile ihrer Sub-FAs.
- **Glas / Zukauf** werden nur von Hand gesetzt: Toggle `/api/picking-status/toggle` (`PickingStatusApiController.cs:14`,
  `ProductionOrderPickingStatusRepository.cs:56`); der Import legt die Statuszeile nur an (`IDEALAKEWMSService/Services/SageImportService.cs:202`).
  Suche `HasGlass|HasExternalPurchase` in `*.cs`/`*.sql` des Worktrees: sonst nur Modell, Migrationen, Leitstand/FA-Liste und Tests.
- **VK–VA** sind `FaWorkStep`-Status je Zeile (`FaWorkStepPivotCell(FaWorkStepId, Status)`, `IFaWorkStepRepository.cs:9`;
  `FaWorkStepStatus` Offen/InBearbeitung/Fertig, `Models/FaWorkStepStatus.cs:8-13`), angelegt von `FaWorkStepDetectionService`
  (`IDEALAKEWMSService/Services/FaWorkStepDetectionService.cs:121`). Ob es an der IDEAL `FaWorkSteps` an Sub-FA-Zeilen gibt, ist
  **unbelegt** → Prüfabfrage P-4 (nur Information, blockiert nichts).
- Die Zellen sind **bedienbar**: Lack-T-erledigt, Glas, Zukauf als Checkbox auf `/api/picking-status/toggle` mit `data-id` der Zeile,
  VK–VA als Auswahl je `FaWorkStepId` (`_PickingLeitstandRow.cshtml:102-156`). Eine zusammengefasste Zelle hat keine einzelne Zeile
  mehr, auf die sie schreiben kann → Rückfrage 7 (entschieden: schreibt die HauptFA-Zeile).
- Spaltenfilter `glass`, `purchase`, `coating-part`, `status` haben serverseitig keinen Auswerter (`ApplyLeitstandColumnFilter`
  `ProductionOrderRepository.cs:325-355`, unbekannter Key = No-Op; VK–VA laufen als Memory-Filter über `LeitstandMemoryText`
  `PickingLeitstandController.cs:748-785`). Daran ändert diese Spec nichts.

**Gibt es die Zusammenfassung schon? (Antwort 5: „Logik haben wir glaube ich schon bei der Kommissionierliste so“)**

- **Kommissionierliste:** fasst **Positionen** je HauptFA zusammen, „unabhängig von `SubFA`“ - Summe `Sollmenge` je
  `(HauptFA, Artnr, Ziel)` (`Services/KommissionierListenService.cs:217-218`, `:248-262`). Ebenso umfasst die Kommissionierung am
  HauptFA die ganze Struktur (`FaHierarchyBomRepository.cs:55-59`, `FullStructure` an der Wurzel). Das ist dieselbe Idee („HauptFA
  inkl. aller Sub-FAs“), aber für Stücklistenpositionen, nicht für die Leitstand-Spalten Lack-T/Glas/Zukauf/VK–VA. Wiederverwendbar ist
  nur der Grundsatz, kein Code.
- **FA-Liste v1.99.82:** zeigt an der HauptFA-Zeile nur Kopf- und Wurzelwerte (`ProductionOrdersController.StrukturFelderSetzen`
  `:517-542`), Lack-T = eigenes Flag der HauptFA-Zeile (`MapItem` `:189`, `:337`). **Keine** Zusammenfassung der Sub-FAs (Suche
  `zusammenf|aggreg|irgendein|schlechtest` in beiden FA-Liste-Specs: kein Treffer).
- **Wiederverwendbare Status-Regel:** `BdeTeilelisteService.AgAggregieren` (`Services/BdeTeilelisteService.cs:509-527`, Antwort 11 der
  BDE-Teileliste-Spec): „schlechtester Stand gewinnt“ = alle erledigt → erledigt, irgendeine läuft → läuft, sonst offen; Zeilen ohne
  Arbeitsgang zählen nicht. Sie arbeitet auf `FaArbeitsschrittSymbol`/`FaAgStatus`, nicht auf `FaWorkStepStatus`; übernommen wird die
  **Regel**, als eigene kleine Funktion auf `FaWorkStepStatus` (drei Werte, kein Soll-Termin). *(überholt durch Antwort 6: die
  Leitstand-Regel weicht ab - gemischt fertig/offen ist „In Bearbeitung“, bei `AgAggregieren` „offen“. Übernommen wird nur „Zeilen ohne
  Arbeitsgang zählen nicht“; `AgAggregieren` bleibt unverändert.)*
- **Speichern der Zellen heute** (für Antwort 7 wichtig): Checkboxen senden den **Zielwert** (`value: this.checked`) an
  `/api/picking-status/toggle`, ohne die Seite neu zu laden; nur bei Fehler springt der Haken zurück (`Views/PickingLeitstand/Index.cshtml:525-553`,
  Endpunkt setzt `req.Value`, `PickingStatusApiController.cs:31-47`). VK–VA senden `{ faWorkStepId, status }` an
  `/api/fa-work-steps/set-status`, ebenfalls ohne Neuladen (`wwwroot/js/fa-work-step-status.js:13-29`); dieses Skript teilt sich der
  Leitstand mit der FA-Abarbeitungsliste (`:1-2`) - nicht ändern *(überholt durch Antwort 8: eine additive Ereignis-Zeile, siehe
  „Klickverhalten“)*. Die Zusammenfassung wird also erst beim nächsten Seitenaufbau neu
  berechnet.
- `SetCoatingPartsAsync` setzt `IsCoatingDone` zurück, wenn das Lack-Flag einer Zeile false wird (`FaMaterializationSyncService.cs:200-203`,
  „kapselt den IsCoatingDone-Reset“).
- **Fazit:** Es gibt keinen Code, der Lack-T/Glas/Zukauf/VK–VA je HauptFA zusammenfasst. Neu ist eine reine statische Funktion
  (Lösungsentwurf Schritt 3); Daten kommen aus den vorhandenen Batch-Methoden mit einer erweiterten Id-Liste.

### Soll

| ID | Anforderung |
|---|---|
| L1 | Hierarchischer Modus, Schalter „Sub-FAs anzeigen“ aus (Standard, Antwort 2): eine Zeile je HauptFA (`IsHauptFa`). Paginierung und Zähler über HauptFA-Zeilen. Zählzeile „x Aufträge (nur HauptFA)“. |
| L2 | Filter (Filterkarte, Spaltenfilter, Datums-/VK-VA-/Matchcode-Filter in C#) wirken auf die HauptFA-Zeile, und zwar auf den **angezeigten** Wert (bei VK–VA also auf die Zusammenfassung, L9). **FA-Nr. ohne Schalter trifft nur die HauptFA-Nummer**, eine Sub-FA-Nummer findet nichts; die Suche über Sub-FA-Nummern gibt es nur mit eingeschaltetem Schalter (Antwort 4) - dort wie heute (Zeile trifft `OrderNumber` oder `SubOrderNumber`, `ProductionOrderRepository.cs:124-128`, `:332-334`). Ist der FA-Nr.-Filter (Karte oder Spalte) ohne Schalter gesetzt und gibt es 0 Treffer, zeigt die Seite `alert-info` „Keine HauptFA mit dieser Nummer – Sub-FA-Nummern findet „Sub-FAs anzeigen““ mit Link auf dieselbe URL plus `mitSubFa=true` (Kritische Prüfung S-2: der QR-Scan der FA-Nr. sendet sofort ab, `Index.cshtml:512-515`, vom Werkstattpapier kommt oft eine Sub-FA-Nummer; nur Hinweis, keine Gegenentscheidung zu Antwort 4). *(überholt: „Ausnahme FA-Nr.: auch eine Sub-FA-Nummer findet ihren HauptFA“.)* |
| L3 | Darstellung ohne Gruppenkopf (flacher Zweig der View); der Knopf „Alle Sub-FAs fertigmelden“ steht in den Zeilen-Aktionen (`row-actions`) der HauptFA-Zeile, nur mit Picking-Recht und nur bei `SubFaAnzahl > 0` (Kritische Prüfung S-4), gleicher Dialog und gleiche Aktion `CascadeDone` (Antwort 3). |
| L4 | Freigabe-Haken, „Freigeben“/„Zurücknehmen“, Prioritätsfeld, Kommissionierer, KO-Links nur an der HauptFA-Zeile, sichtbar und bedienbar wie heute. Sub-FA-Zeilen haben **in keiner Ansicht** (auch nicht mit Schalter) Bulk-Haken oder Freigabe-Knopf; der Server lehnt eine Sub-FA-Freigabe weiter ab (Antworten 1 und 3; Bestand, siehe Ist-Stand - nicht ändern, nur absichern). |
| L5 | Banner (melden statt still), wenn offene Aufträge keine HauptFA-Zeile haben: „n Aufträge ohne HauptFA-Zeile – nur mit „Sub-FAs anzeigen“ sichtbar, Freigabe dort nicht möglich.“ mit Link auf die Ansicht mit Sub-FAs. Gezählt mit demselben `showDone`-Filter (`CountGruppenOhneHauptFaAsync`, vorhanden). |
| L6 | Schalter „Sub-FAs anzeigen“ an: heutige Gruppenansicht unverändert, Sub-FA-Zeilen mit ihren **eigenen** Werten (Antwort 2). Der Schalter steht in der URL (`mitSubFa=true`), Pagination und „Zurücksetzen“ nehmen ihn mit bzw. setzen ihn zurück. |
| L7 | Flacher Modus (AKE) und FA-Liste unverändert. |
| L8 | *(überholt durch Antwort 5: „Spalten Lack-T, Glas, Zukauf, VK-VA an der HauptFA-Zeile zeigen deren eigene Werte“)* → ersetzt durch L9. |
| L9 | Ohne Schalter zeigt die HauptFA-Zeile in Lack-T (und damit Beschicht.-Termin), Glas, Zukauf und VK–VA die **Zusammenfassung über alle Zeilen ihres Auftrags** (HauptFA-Zeile + alle Sub-FA-Zeilen mit derselben `OrderNumber`) nach der Zusammenfassungsregel unten (Antwort 5). Die Zusammenfassung wird **vor** den Memory-Filtern gebildet und unabhängig von den Listenfiltern über alle Zeilen des Auftrags gerechnet. |
| L10 | Zusammengefasste Zellen bleiben bedienbar; ein Klick an der HauptFA-Zeile schreibt **nur die HauptFA-Zeile** (eigene `ProductionOrderPickingStatus`-Zeile bzw. eigener `FaWorkStep`), Sub-FA-Werte bleiben unverändert, die Anzeige bleibt die Zusammenfassung (Antwort 7, Variante c). Verhalten je Fall siehe „Klickverhalten“: wo Sub-FAs den Wert bestimmen (W1, W2, W5), bedienbar mit Neuladen nach dem Speichern und Hinweis „Sub-FA bestimmt den Wert weiter“ (Antwort 8, Variante ii); gesperrt nur W4 (kein `FaWorkStep` an der HauptFA-Zeile, Text „Arbeitsgang nur an Sub-FAs …“) und W3 (Lack-T erledigt an HauptFA-Zeile ohne eigene Lackteile, Text „Lackteile nur an Sub-FAs …“, Antwort 9 b). *(überholt: „W1-W3, W5 bedienbar … nur W4 gesperrt“; überholt: „für die Fälle W1-W4 gilt Rückfrage 8“; überholt: „Bis zur Antwort baut der Dev-Lauf die Empfehlung (nur Anzeige, gesperrt …)“.)* |
| L11 | Die FA-Nr.-Zelle der HauptFA-Zeile zeigt als Text, wie viele Sub-FAs eingeflossen sind („+ n Sub-FAs“, nur wenn n > 0), damit erkennbar ist, dass die Werte zusammengefasst sind (nicht nur Farbe). n = nicht stornierte Sub-FA-Zeilen **inklusive erledigter** (Basis der Zusammenfassungsregel), nicht die gefilterten Gruppenzeilen wie das heutige Badge (`Index.cshtml:213`). Die FA-Nr.-Zelle trägt `data-csv-value` mit der FA-Nummer, damit „+ n Sub-FAs“ nicht in der CSV-Spalte FA-Nr. landet (`csv-export.js:85`) (Kritische Prüfung S-4). |

### Zusammenfassungsregel je Spalte (Antwort 5; bestätigt mit Antwort 6, VK–VA geändert)

Basis: alle `ProductionOrder`-Zeilen mit der `OrderNumber` des HauptFA, **ohne stornierte** (`IsCancelled`), **mit erledigten** (sonst
würde ein fertiger Sub-FA seinen „fertig“-Stand aus der Zusammenfassung nehmen). Unabhängig von `showDone` und den Listenfiltern.

| ID | Spalte (`data-col-key`) | Regel an der HauptFA-Zeile | Begründung / Vorbild |
|---|---|---|---|
| Z1 | Lack-T vorhanden (`HasCoatingParts`, steuert auch die Zelle `coating-part` und `showBeschicht` für `coating-date`) | irgendeine Zeile hat Lackierteile | Flag „hat …“ = irgendeine |
| Z2 | Lack-T erledigt (`IsCoatingDone`) | alle Zeilen **mit** Lackierteilen erledigt | schlechtester Stand; Zeilen ohne Lackteile zählen nicht (wie `AgAggregieren` Zeilen ohne Arbeitsgang) |
| Z3 | Glas (`glass`, `HasGlass`) | irgendeine Zeile gesetzt | Flag „hat …“ = irgendeine |
| Z4 | Zukauf (`purchase`, `HasExternalPurchase`) | irgendeine Zeile gesetzt | wie Z3 |
| Z5 | VK, VL, VE, VT, VA (`cooling` … `superstructure`) | je Kürzel nur Zeilen, die den Arbeitsgang haben: alle Fertig → Fertig; alle Offen → Offen; sonst (irgendeine Fertig oder InBearbeitung, nicht alle Fertig) → InBearbeitung. Gemischt Fertig/Offen = **InBearbeitung** (Antwort 6). Hat keine Zeile den Arbeitsgang: Zelle leer wie heute | Antwort 6 (Gerald); *(überholt: „Regel `AgAggregieren`, gemischt = Offen“)* |
| Z6 | Beschicht.-Termin (`coating-date`) | Termin wie heute aus `BeschichtungResolver.Bestimmen` mit dem Kopf (`PickingLeitstandController.cs:183-188`), aber mit Z1 als `hasCoatingParts` | Termin hängt am Auftragskopf, nur die Sichtbarkeit am Flag |
| – | Status, Komm./Teilkomm., Kommissionieraufträge, Altbestand, Stückliste kleiner, Freigabe, Prio, Kommissionierer | Werte der HauptFA-Zeile | Freigabe und Kommissionierung gibt es nur am HauptFA (L4) |
| – | Stk., Kunde, Artikelnummer, Matchcode, Bezeichnung 1/2, Werkbank, Kältemittel, Ventil, Ausführung, Maschine, Sage-Status, BG-/Komm.-/Fert.-/Liefertermin, Übergeordnete Sub-FA | Werte der HauptFA-Zeile wie heute | beschreiben den Auftrag; eine Summe über verschiedene Artikel ist sinnlos |

Mit Schalter „Sub-FAs anzeigen“ gilt die Zusammenfassung **nicht**: jede Zeile (auch die HauptFA-Zeile) zeigt ihre eigenen Werte wie heute,
damit die Gruppenansicht die Einzelwerte zeigt, aus denen die Zusammenfassung entsteht.

### Klickverhalten an der HauptFA-Zeile (Antwort 7, Variante c)

Heute speichert jede Zelle ohne Neuladen; der Haken bzw. die Auswahl zeigt bis zum nächsten Seitenaufbau den **geklickten** Wert, erst dann
wird neu zusammengefasst (Ist-Stand „Speichern der Zellen heute“). Daraus folgt je Fall:

| Fall | Spalte | Lage | Klick schreibt | Benutzer sieht sofort | nach Neuladen | Bewertung |
|---|---|---|---|---|---|---|
| K1 | alle | Auftrag ohne Sub-FAs, oder Sub-FAs tragen den Wert nicht anders als der HauptFA | HauptFA-Zeile | geklickten Wert | denselben Wert | stimmig, wie heute |
| K2 | Glas / Zukauf | keine Sub-FA gesetzt, HauptFA setzen oder abwählen | HauptFA-Zeile | geklickten Wert | denselben Wert | stimmig |
| W1 | Glas / Zukauf | eine Sub-FA hat Glas gesetzt; Benutzer **wählt ab** | HauptFA `HasGlass = false` (war oft schon false) | leeren Haken | wieder **gesetzt** (Sub-FA bestimmt Z3) | **widersprüchlich** - Abwählen ist an der HauptFA-Zeile unmöglich |
| W2 | Lack-T erledigt | HauptFA hat eigene Lackteile, eine Sub-FA mit Lackteilen ist offen; Benutzer **setzt** erledigt | HauptFA `IsCoatingDone = true` | gesetzten Haken | wieder **leer** (Z2: nicht alle erledigt) | **widersprüchlich** - Wirkung unsichtbar |
| W3 | Lack-T erledigt | HauptFA-Zeile hat **keine** eigenen Lackteile, nur Sub-FAs (Haken erscheint wegen Z1) | HauptFA `IsCoatingDone = true` an einer Zeile ohne Lackteile | gesetzten Haken | **unverändert** (Zeile zählt in Z2 nicht); beim nächsten Materialisierungslauf ggf. zurückgesetzt (`SetCoatingPartsAsync`-Reset) | **wirkungslos** → mit Antwort 9 (b) gesperrt, Text „Lackteile nur an Sub-FAs – ändern mit „Sub-FAs anzeigen““ |
| W4 | VK–VA | HauptFA-Zeile hat den Arbeitsgang **nicht**, nur Sub-FAs | nichts - es gibt keinen `FaWorkStep` der HauptFA-Zeile | - | - | **technisch nicht schreibbar**: Auswahl gesperrt, zeigt die Zusammenfassung Z5 der Sub-FA-Zeilen (keine Entscheidung, sondern Folge von (c); gesperrt nach Antwort 8, neben W3 nach Antwort 9); Hinweistext „Arbeitsgang nur an Sub-FAs - ändern mit „Sub-FAs anzeigen““ |
| W5 | VK–VA | HauptFA hat den Arbeitsgang, Sub-FAs weichen ab (z. B. HauptFA → Fertig, Sub-FA Offen) | HauptFA `FaWorkStep.Status` | gewählten Wert („Fertig“) | **„In Bearbeitung“** (Z5) | erwartbar, aber für den Benutzer überraschend; mit Antwort 8 Neuladen + Hinweis |

Die Spalten „Benutzer sieht sofort“ und „nach Neuladen“ beschreiben das Speichern ohne Neuladen von heute. Mit Antwort 8 lädt die Seite in
W1, W2 und W5 sofort neu (W3 und W4 sind gesperrt); der Benutzer sieht dann direkt den Wert aus „nach Neuladen“ mit dem Hinweis „Sub-FA bestimmt den Wert weiter“.

*(überholt durch Antwort 8: „Bis zur Antwort baut der Dev-Lauf die Empfehlung (i): … sonst gesperrt … mit Kurztext „bestimmt durch
Sub-FA“ … `fa-work-step-status.js` wird dafür nicht geändert“.)*

**Entschieden mit Antwort 8, Variante (ii), und Antwort 9 (b):** W1, W2 und W5 bleiben **bedienbar**. *(überholt: „W1, W2, W3 und W5
bleiben bedienbar“.)* Sie schreiben nur die HauptFA-Zeile (Antwort 7).
Nach erfolgreichem Speichern wird die Seite neu geladen, damit sofort der neu zusammengefasste Wert steht.

- **„Sub-FA bestimmt“** heißt: Nicht jeder Wert, den man an der HauptFA-Zeile setzen kann, erscheint danach auch als Zusammenfassung
  (Checkbox: beide Werte; VK–VA: alle drei Status). Die reine Funktion rechnet dafür die Regel Z1-Z5 mit jedem möglichen HauptFA-Wert
  durch und liefert je Zelle das Flag `SubFaBestimmt`.
- **Hinweis:** Zellen mit `SubFaBestimmt` tragen sichtbar den Text „Sub-FA bestimmt den Wert weiter“ (Kurztext an der Zelle plus `title`,
  nicht nur Farbe). Der Hinweis steht bei jedem Seitenaufbau, nicht nur nach einem Klick; damit sieht der Benutzer nach dem Neuladen,
  warum sein Klick den angezeigten Wert nicht oder nicht ganz geändert hat (W1: Haken wieder gesetzt, W2: wieder leer,
  W5: „In Bearbeitung“).
- **Neuladen nur bei `SubFaBestimmt`** (`data-neu-laden="true"` an der Zelle): In K1/K2 zeigt die Zelle den geklickten Wert ohnehin
  richtig, dort bleibt es beim heutigen Speichern ohne Neuladen. `location.reload()` behält URL, Filter, Seite und Schalter; die
  Scrollposition geht verloren (bekannte Grenze, kein eigener Mechanismus). Bei Fehler wie heute zurück auf den alten Wert + Meldung, kein
  Neuladen.
- **W4 bleibt gesperrt** (Begründung: an der HauptFA-Zeile gibt es keinen `FaWorkStep` für diesen Arbeitsgang, also
  nichts, was Variante (c) schreiben könnte; einen anzulegen wäre ein neuer Schreibweg, den keine Antwort vorsieht). Angezeigt wird die
  Zusammenfassung Z5 der Sub-FA-Zeilen in der gesperrten Auswahl, mit Text „Arbeitsgang nur an Sub-FAs – ändern mit „Sub-FAs anzeigen““.
- **W3 gesperrt (Antwort 9 b):** Die HauptFA-Zeile hat selbst keine Lackteile, Lack-T kommt nur über Sub-FAs (Z1). Dann ist der
  Lack-T-erledigt-Haken gesperrt. Er zeigt die Zusammenfassung Z2 der Sub-FA-Lackteilzeilen, mit Text und `title` „Lackteile nur an
  Sub-FAs – ändern mit „Sub-FAs anzeigen““. Kein Hinweis „Sub-FA bestimmt den Wert weiter“, kein Neuladen. Kriterium: Die eigene
  `HasCoatingParts` der HauptFA-Zeile ist false und die zusammengefasste ist true; die Funktion liefert dafür das Flag `LackNurSubFa`.
  Begründung: `IsCoatingDone` an dieser Zeile zählt in Z2 nicht. Der Materialisierungslauf kann den Wert außerdem zurücksetzen
  (`FaMaterializationSyncService.cs:216-228`). *(überholt: „W3 … → Rückfrage 9. Bis zur Antwort gilt Antwort 8 wörtlich: bedienbar mit
  Neuladen und Hinweis.“)*
- **Skripte:** Die Checkboxen speichern über das Leitstand-eigene Skript (`Index.cshtml:525-553`), das Neuladen kommt dort in den
  Erfolgszweig. Für VK–VA braucht es eine additive Zeile im gemeinsamen `fa-work-step-status.js`: nach `resp.ok` ein
  `CustomEvent('wms:fa-status-gespeichert', { bubbles: true })` am Select. Die Leitstand-Seite lädt bei diesem Ereignis neu, wenn
  `sel.closest('[data-neu-laden]')` trifft (Attribut an der `<td>`, damit `_FaWorkStepStatusSelect` (`:3`) unverändert bleibt); die FA-Abarbeitungsliste hört nicht darauf, bei ihr ändert sich nichts. Das gemeinsame Skript wird
  **einmal** und nur additiv angefasst (Grundsatz aus dem Gedächtnis: gemeinsames JS nur einmal anfassen). Ein Neuladen ohne dieses
  Ereignis, etwa direkt beim `change`, würde den laufenden `fetch` abbrechen.

## Technischer Loesungsentwurf

Ponytail-Leiter: vorhandene Gruppen-Abfrage mit vorhandenem `nurHauptFa`, vorhandene Batch-Methoden für Pivot und Statuszeilen,
vorhandener flacher View-Zweig, vorhandener Dialog. Neu sind nur ein Parameter, eine kleine Repository-Methode und eine reine Funktion.

1. **Repository** *(überholt: „`nurHauptFa` und `CountGruppenOhneHauptFaAsync` neu bauen“ - beides existiert seit v1.99.82, siehe
   Ist-Stand; wiederverwenden)*. Neu:
   - `GetForLeitstandGroupedAsync` und `BuildLeitstandQuery` bekommen `bool faNrUeberSubFa = true` (Default = heutiges Verhalten,
     FA-Liste unverändert). Bei `nurHauptFa && !faNrUeberSubFa` entfallen die Auftrags-Subqueries für die FA-Nr.
     (`ProductionOrderRepository.cs:111-119` Karte, `:168-174` Spalte); der FA-Nr.-Filter läuft dann normal auf der HauptFA-Zeile
     (`:124-128`, `:332-334`), trifft also nur noch die HauptFA-Nummer (L2, Antwort 4).
   - `Task<List<(int Id, string OrderNumber)>> GetZeilenIdsJeAuftragAsync(IReadOnlyCollection<string> orderNumbers)`: alle
     nicht stornierten Zeilen der Aufträge (eine Abfrage, `AsNoTracking`, `WHERE OrderNumber IN (...) AND IsCancelled = 0`),
     unabhängig von `showDone` (Basis der Zusammenfassungsregel). Bei mehr als 2100 Parametern in Blöcken wie
     `GetWorkStepPivotAsync` (`IFaWorkStepRepository.cs:19`, „Chunked in 1000er-Bloecken“).
2. **Controller `Index`:** Parameter `bool mitSubFa = false` als **letzter** Parameter (Positionsaufrufe in Tests, z. B.
   `_controller.Index(null, null, "Mustermann", false, 1, null)`, bleiben gültig; Kritische Prüfung S-3). Im hierarchischen Zweig (`:208-209`)
   `nurHauptFa: !mitSubFa, faNrUeberSubFa: false` (mit Schalter ist `nurHauptFa` aus, der Parameter wirkt dann nicht).
   Bei `nurHauptFa`:
   - Zeilen-Ids aller Aufträge der geladenen Gruppen über `GetZeilenIdsJeAuftragAsync`; die **eine** Id-Liste (HauptFA + Sub-FA)
     an die vorhandenen Aufrufe `GetWorkStepDetailPivotAsync` und `GetByProductionOrderIdsAsync` (`:218-220`) geben - kein
     zusätzlicher Round-Trip je Auftrag.
   - Nach `MapItem` je HauptFA-Zeile `LeitstandHauptFaZusammenfassung.Anwenden(item, statusZeilen, pivots)` (Schritt 3), dann
     Beschichtungstermin mit dem zusammengefassten `HasCoatingParts` neu bestimmen (Z6). Erst danach die Memory-Filter (`:240-254`),
     damit VK–VA-Filter auf den angezeigten Wert wirken (L2).
   - `vm.Items = listGroups.SelectMany(g => g.Items)`, `Hierarchical = true` bleibt (FA-Nr.-Zelle, `isHauptFaRow`), neu
     `vm.NurHauptFa = true`, `vm.MitSubFa = mitSubFa`, `vm.GruppenOhneHauptFa = await CountGruppenOhneHauptFaAsync(showDone)`.
3. **Reine Funktion `Services/LeitstandHauptFaZusammenfassung.cs`** (statisch, DB-frei, unit-testbar): bekommt die HauptFA-Zeile
   (`PickingLeitstandItem`) und die Statuszeilen/Pivots **aller** Zeilen ihres Auftrags, setzt `HasCoatingParts`, `IsCoatingDone`,
   `HasGlass`, `HasExternalPurchase`, `WorkSteps` (Z1-Z5) und `SubFaAnzahl` (L11), dazu je Zelle das Flag `SubFaBestimmt` nach dem
   Kriterium im Abschnitt „Klickverhalten“ (Antwort 8). *(überholt: „je Spalte ein Flag „bedienbar““.)* VK–VA: je Kürzel Regel Z5
   (Antwort 6); die zusammengefasste Zelle trägt die `FaWorkStepId` der HauptFA-Zeile, damit ein Klick nur die HauptFA-Zeile schreibt
   (Antwort 7); fehlt sie, 0 und gesperrt (W4). Dazu das Flag `LackNurSubFa` für W3 (Antwort 9 b). *(überholt: „W4, einziger
   gesperrter Fall“.)* Checkboxen behalten
   `data-id` = Id der HauptFA-Zeile. Keine zweite Sub-FA-Definition:
   „Sub-FA“ = Zeile derselben `OrderNumber`, die nicht die HauptFA-Zeile ist (`ProductionOrder.IsSubFaOf`).
4. **View `Index.cshtml`:** Filterkarte Checkbox „Sub-FAs anzeigen“ (`name="mitSubFa"`, Muster „Erledigte anzeigen“ `:51-55`), nur bei
   `Model.Hierarchical`. Zweigwahl `Model.Hierarchical && !Model.NurHauptFa` → Gruppen, sonst flacher Zweig. Zählzeile L1, Banner L5
   (`alert-warning`, Text, nicht nur Farbe). `data-hierarchical` nur bei Gruppenansicht `true`, damit `fa-liste-gruppierung.js` nichts
   sucht. Spaltenkopf-`title` von Lack-T, Glas, Zukauf, VK–VA im Nur-HauptFA-Modus um „zusammengefasst aus HauptFA und Sub-FAs“ ergänzen.
   Hinweis bei FA-Nr.-Filter ohne Treffer (L2, S-2): eine Bedingung `NurHauptFa && FA-Nr.-Filter gesetzt && keine Items`. FA-Nr.-Zelle
   mit `data-csv-value` (L11).
5. **Row-Partial:** Knopf `btn-cascade-done` (gleiche Klasse, `data-order-number`) in der `row-actions`-Zelle bei
   `Model.Hierarchical && isHauptFaRow && Model.CanPick && Model.NurHauptFa` und `SubFaAnzahl > 0` des Items (neues Feld im
   `PickingLeitstandRowContext`; `SubFaAnzahl > 0` nach S-4). Das Skript
   `:460-` sucht nur `.btn-cascade-done` und braucht keine Änderung; `e.stopPropagation()` ist im flachen Zweig harmlos.
   Zusammengefasste Zellen nach L10 (Antworten 7 und 8): Wert = Zusammenfassung, `data-id`/`FaWorkStepId` = HauptFA-Zeile, bedienbar.
   Bei `SubFaBestimmt`: `data-neu-laden="true"` an der `<td>`, sichtbarer Kurztext „Sub-FA bestimmt den Wert weiter“ und `title`
   „Zusammenfassung aus HauptFA und n Sub-FAs“. Gesperrt nur W4 (`_FaWorkStepStatusSelect` mit `Disabled = true`, Text
   „Arbeitsgang nur an Sub-FAs – ändern mit „Sub-FAs anzeigen““) und W3 bei `LackNurSubFa` (Lack-T-erledigt-Checkbox `disabled`, Text
   „Lackteile nur an Sub-FAs – ändern mit „Sub-FAs anzeigen““, Antwort 9 b). Skript: Checkbox-Handler `Index.cshtml:525-553` lädt im
   Erfolgszweig neu, wenn die Zelle `data-neu-laden` trägt; für VK–VA ein Listener auf `wms:fa-status-gespeichert` plus die eine
   additive Ereigniszeile in `fa-work-step-status.js` (Abschnitt „Klickverhalten“). *(überholt: „gesperrt … nur, wo das Flag
   „bedienbar“ false ist, mit Kurztext „bestimmt durch Sub-FA“ … (Rückfrage 8, Empfehlung (i))“; überholt: „Empfehlung: bei
   `SubFaAnzahl > 0` alle Zellen gesperrt“.)* Freigabe-Bedingungen `:23`, `:35`, `:244`
   bleiben unverändert (L4).
6. Frontend: Skill `frontend-design` ausdrücklich aufrufen. Bootstrap-Muster der Filterkarte, Trefferflächen ≥ 24 px, Kontrast AA;
   gesperrte Zellen nicht nur über Ausgrauen kenntlich machen (Text im `title`, Zählhinweis L11).
7. **Hilfe `Views/Help/Index.cshtml`**, Abschnitt „Leitstand (Kommissionier-Freigabe)“, ausdrücklich: (a) Zeigt Lack-T „Lackteile nur an
   Sub-FAs“, wird Lack-T erledigt über „Sub-FAs anzeigen“ an den Sub-FA-Zeilen gesetzt (B-1). (b) Der eigene Wert der HauptFA-Zeile ist
   nur mit „Sub-FAs anzeigen“ sichtbar. Beispiel: Lack-T erledigt an der HauptFA gesetzt, Zelle zeigt wegen offener Sub-FA trotzdem leer;
   Zurücknehmen nur dort (H-1). (c) Ist die HauptFA erledigt, fertig kommissioniert oder storniert, verschwindet der Auftrag, auch mit offenen
   Sub-FAs; sichtbar mit „Erledigte anzeigen“ (S-1). (d) Sub-FA-Nummern findet die FA-Nr.-Suche nur mit „Sub-FAs anzeigen“.

## Migrations-/SQL-Auswirkungen

Keine Migration, kein Schema. Nur LINQ-Abfragen. `SQL/00_FreshInstall.sql`, `SQL/AgentJobs/` unberührt.

Prüfabfragen für Rückfrage 1 (WMS-DB IDEAL, Testsystem zuerst; nur lesen). *P-1 und P-2 sind durch Antwort 1 überholt (kein
Befund mehr zu klären); P-3 bleibt als Manifest-Prüfung.*

```sql
-- P-1: Leitstand an?
SELECT [Key], [Value] FROM dbo.AppSettings WHERE [Key] IN (N'LeitstandAktiv', N'KommissionierungMitZuweisung');

-- P-2: HauptFA-Zeile des betroffenen Auftrags (Nummer einsetzen) - fehlt sie, gibt es keinen Haken
SELECT po.Id, po.OrderNumber, po.SubOrderNumber, po.ArticleNumber, po.IsDone, po.IsCancelled,
       ps.IsDonePicking, ps.IsReleasedForPicking
FROM dbo.ProductionOrders po
LEFT JOIN dbo.ProductionOrderPickingStatus ps ON ps.ProductionOrderId = po.Id
WHERE po.OrderNumber = N'<HauptFA>'
ORDER BY CASE WHEN po.SubOrderNumber = N'' OR po.SubOrderNumber = po.OrderNumber THEN 0 ELSE 1 END, po.SubOrderNumber;

-- P-3: offene Aufträge ohne HauptFA-Zeile (gleiche Abfrage wie die Manifest-Prüfung unten).
-- Zeilenfilter wie showDone = false im Banner (ProductionOrderRepository.cs:158-159, :184-190), inkl. IsDonePicking (Kritische Prüfung S-1).
SELECT COUNT(DISTINCT po.OrderNumber) AS OhneHauptFa
FROM dbo.ProductionOrders po
LEFT JOIN dbo.ProductionOrderPickingStatus ps ON ps.ProductionOrderId = po.Id
WHERE po.IsDone = 0 AND po.IsCancelled = 0 AND ISNULL(ps.IsDonePicking, 0) = 0
  AND NOT EXISTS (SELECT 1 FROM dbo.ProductionOrders h
                  WHERE h.OrderNumber = po.OrderNumber
                    AND (h.SubOrderNumber = N'' OR h.SubOrderNumber = h.OrderNumber));
```

P-4 (Information zu Z5, WMS-DB IDEAL, nur lesen; blockiert nichts): Haben Sub-FA-Zeilen an der IDEAL überhaupt VK–VA-Arbeitsgänge?
Ist das Ergebnis 0, zeigt Z5 praktisch den Wert der HauptFA-Zeile.

```sql
-- P-4: aktive FaWorkSteps an Sub-FA-Zeilen, je Code
SELECT ws.Code, COUNT(*) AS Anzahl
FROM dbo.FaWorkSteps f
JOIN dbo.WorkSteps ws ON ws.Id = f.WorkStepId
JOIN dbo.ProductionOrders po ON po.Id = f.ProductionOrderId
WHERE f.IsRemoved = 0
  AND po.SubOrderNumber <> N'' AND po.SubOrderNumber <> po.OrderNumber
GROUP BY ws.Code;
```

Spalten von P-4 belegt: `WorkSteps.Code` (`SQL/00_FreshInstall.sql:534-536`), `FaWorkSteps.ProductionOrderId`/`WorkStepId`/`IsRemoved`
(`:596-607`).

Spalten belegt: `IsDone`/`IsCancelled` (`ProductionOrderRepository.cs:142`), `ProductionOrderPickingStatus.ProductionOrderId`/
`IsReleasedForPicking` (`SQL/00_FreshInstall.sql:431-436` laut Spec 2026-11-18). Das Leitstand-Recht des Benutzers in `/Users`
prüfen (Rolle `leitstand`, Glossar).

## Einstellungen

keine. „Sub-FAs anzeigen“ ist ein Filter der Seite (URL-Parameter), keine Einstellung. `LeitstandAktiv` bleibt unverändert.

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| – | – | – | – | – | – | – | – |

## Audit-Feld-Auswirkungen

Keine neuen Schreibwege. Freigabe, Priorität, Kaskade schreiben wie bisher (`SetReleaseAsync`/`SetReleaseBatchAsync`, `CascadeDone`)
mit ihren vorhandenen Audit-Feldern. Die Zusammenfassung (L9) ist reine Anzeige und schreibt nichts. Bedienbare Zellen schreiben nur die
HauptFA-Zeile über die vorhandenen Endpunkte (`/api/picking-status/toggle`, `/api/fa-work-steps/set-status`) mit deren Audit-Feldern
(Antwort 7, Variante c) - kein neuer Schreibweg. Gesperrte Zellen schreiben nichts.

## Akzeptanzkriterien

1. IDEAL, Leitstand ohne Schalter: je HauptFA genau eine Zeile; keine Sub-FA-Zeile, kein Gruppenkopf. Pagination zählt HauptFA.
2. HauptFA-Zeile (offen, Benutzer mit Leitstand-Recht, `LeitstandAktiv` an) hat Bulk-Haken und „Freigeben“; „Markierte freigeben“
   gibt sie frei und legt Kommissionieraufträge an wie heute.
3. Ohne Schalter: FA-Nr.-Filter (Karte und Spalte) mit einer Sub-FA-Nummer zeigt **keine** Zeile; mit der HauptFA-Nummer die HauptFA-Zeile.
   Mit Schalter: dieselbe Sub-FA-Nummer zeigt die Gruppe mit der passenden Sub-FA-Zeile (wie heute). *(überholt: „FA-Nr.-Filter mit einer
   Sub-FA-Nummer zeigt die HauptFA-Zeile ihres Auftrags“.)* Ohne Schalter und ohne Treffer zeigt die Seite den Hinweis „Keine HauptFA mit
   dieser Nummer – Sub-FA-Nummern findet „Sub-FAs anzeigen““; der Link öffnet dieselbe Suche mit Schalter und zeigt die Gruppe (S-2).
   Mit Treffern oder ohne FA-Nr.-Filter kein Hinweis.
4. Artikelnummer-Filter mit der Artikelnummer einer Sub-FA (nicht des HauptFA) zeigt ohne Schalter nichts (Filter auf HauptFA-Zeile, Antwort 4).
5. „Alle Sub-FAs fertigmelden“ in den Zeilen-Aktionen öffnet denselben Dialog mit denselben Zahlen; Fertigmelden wirkt wie heute.
   An einer HauptFA ohne Sub-FAs (n = 0) fehlt der Knopf (S-4).
6. Schalter „Sub-FAs anzeigen“ an: Darstellung identisch zu heute (Gruppen, Sub-FA-Zeilen mit eigenen Werten, Badge, Kopf-Knopf).
   Seitenwechsel behält den Schalter, „Zurücksetzen“ schaltet ihn aus.
7. Schalter an: Sub-FA-Zeilen haben weder Bulk-Haken noch „Freigeben“; ein direkt abgesetzter `ToggleRelease`-POST mit einer Sub-FA-Id
   ändert nichts und meldet „Kommissionierung erfolgt nur am HauptFA …“ (Antworten 1 und 3).
8. Auftrag ohne HauptFA-Zeile (Testdaten): Banner nennt die Anzahl; mit Schalter ist die Gruppe sichtbar. HauptFA-Zeile „Als erledigt
   markieren“ (fertig kommissioniert) bei offenen Sub-FAs: Der Auftrag verschwindet ohne Banner, die Banner-Zahl ändert sich nicht;
   mit „Erledigte anzeigen“ ist er wieder da (S-1, Out-of-Scope). Manifest-Prüfung P-3 und Banner liefern auf denselben Daten dieselbe
   Zahl.
9. AKE (flacher Modus): Leitstand unverändert, kein Schalter, kein Banner, keine Zusammenfassung.
10. FA-Liste `/ProductionOrders` an der IDEAL unverändert (v1.99.82-Verhalten: eine Zeile je HauptFA, FA-Nr. findet auch über Sub-FA-Nummer,
    Lack-T = eigener Wert).
11. Benutzer nur mit Picking-Recht: keine Freigabe-Elemente (wie heute), Kaskaden-Knopf sichtbar.
12. Zusammenfassung ohne Schalter (Z1-Z5), je an einem HauptFA mit zwei Sub-FAs geprüft:
    a) nur eine Sub-FA hat Lackierteile → HauptFA-Zeile zeigt Lack-T und den Beschicht.-Termin; b) diese Sub-FA Lack-T erledigt, eine
    zweite mit Lackteilen nicht → Lack-T nicht erledigt; beide erledigt → erledigt; c) Glas nur an einer Sub-FA → Glas gesetzt; Zukauf ebenso;
    d) VK an einer Sub-FA Fertig, an der anderen Offen → **InBearbeitung** (Antwort 6); eine InBearbeitung → InBearbeitung; alle Fertig →
    Fertig; alle Offen → Offen; keine Zeile mit VK → Zelle leer; e) eine stornierte Sub-FA mit Glas zählt nicht; eine erledigte Sub-FA mit VK Fertig zählt mit.
13. FA-Nr.-Zelle der HauptFA-Zeile zeigt „+ n Sub-FAs“ bei n > 0, nichts bei n = 0 (L11). n = nicht stornierte Sub-FA-Zeilen inkl.
    erledigter (eine erledigte Sub-FA zählt mit, eine stornierte nicht). Der CSV-Export schreibt in die Spalte FA-Nr. nur die FA-Nummer,
    ohne „+ n Sub-FAs“ (S-4).
14. VK-Spaltenfilter „fertig“ ohne Schalter trifft genau die HauptFA-Zeilen, deren **zusammengefasster** VK-Wert Fertig ist (L2).
15. Klick an der HauptFA-Zeile (Antwort 7): In K1/K2 (z. B. HauptFA ohne Sub-FAs; Glas, wenn keine Sub-FA Glas trägt) ändert der Klick nur
    die Statuszeile bzw. den `FaWorkStep` der HauptFA-Zeile (DB-Prüfung: Sub-FA-Zeilen unverändert, `ModifiedBy` der HauptFA-Zeile gesetzt);
    der Klick lädt die Seite **nicht** neu, kein Hinweis „Sub-FA bestimmt den Wert weiter“; nach manuellem Neuladen zeigt die Zelle den
    geklickten Wert. *(überholt: „zusammengefasste Zellen einer HauptFA mit Sub-FAs sind gesperrt“.)*
16. Fälle W1, W2 und W5 (Antwort 8, Variante ii) *(überholt: „W1-W3 und W5“)*: Die Zelle ist bedienbar und trägt den sichtbaren Hinweis „Sub-FA bestimmt den Wert
    weiter“, auch schon vor dem Klick. Nach dem Klick speichert sie nur die HauptFA-Zeile (DB-Prüfung wie AK 15) und lädt die Seite
    sofort neu, mit gleichen Filtern, gleicher Seite und gleichem Schalter. Danach zeigt sie: W1 Glas/Zukauf abgewählt bei Sub-FA mit Glas → wieder
    gesetzt; W2 Lack-T erledigt bei offener Sub-FA-Lackteilzeile → wieder leer; W5 VK an HauptFA „Fertig“, Sub-FA Offen → „In
    Bearbeitung“. W3 (Antwort 9 b, HauptFA-Zeile ohne eigene Lackteile, Sub-FA mit Lackteilen): Lack-T-erledigt-Haken gesperrt, zeigt
    Z2 der Sub-FA-Lackteilzeilen, Text „Lackteile nur an Sub-FAs – ändern mit „Sub-FAs anzeigen““, kein Hinweis „Sub-FA bestimmt …“; die
    Sperre ist reine Oberfläche, diese Spec baut am Endpunkt `/api/picking-status/toggle` keine Server-Prüfung ein. *(überholt: „W3 … → unverändert
    (vorbehaltlich Rückfrage 9)“.)* W4 (VK nur an Sub-FAs): Auswahl
    gesperrt und zeigt die Zusammenfassung der Sub-FAs mit Text „Arbeitsgang nur an Sub-FAs – ändern mit „Sub-FAs anzeigen““. Speicherfehler
    (z. B. Server-Antwort 400): alter Wert zurück, Meldung, kein Neuladen. FA-Abarbeitungsliste: Status ändern wie bisher, kein Neuladen.
    *(überholt: „Fälle W1-W5 (Empfehlung (i) bis Rückfrage 8): … gesperrt, Text „bestimmt durch Sub-FA“ …“.)*
17. Unit-Tests `LeitstandHauptFaZusammenfassung`: alle Fälle aus AK 12 einzeln, „nur HauptFA-Zeile“ (= eigener Wert, `SubFaBestimmt`
    false), das Flag `SubFaBestimmt` für K1, K2 (false) und W1, W2, W5 (true), `LackNurSubFa` für W3 (true; false, sobald die HauptFA-Zeile
    eigene Lackteile hat), W4 = `FaWorkStepId` 0 / gesperrt. *(überholt: „das Flag „bedienbar“ für K1, K2, W1-W5“; überholt: „W1, W2,
    W3, W5 (true)“.)*
18. Repository-Test InMemory: `nurHauptFa` + `faNrUeberSubFa: false` trifft mit Sub-FA-Nummer nichts, mit HauptFA-Nummer die HauptFA-Zeile;
    `faNrUeberSubFa` Default (`true`) unverändert (bestehende Tests `FaListeNurHauptFaTests` grün); `GetZeilenIdsJeAuftragAsync` liefert
    HauptFA- und Sub-FA-Zeilen ohne stornierte.
    Bestehende Tests (S-3): Die Moq-Setups und Verifies von `GetForLeitstandGroupedAsync` (`PickingLeitstandControllerTests.cs:172`,
    `:230`, `:246`, `:1050`, `ProductionOrdersControllerTests.cs:118`, `:678`) sind auf den neuen Parameter angepasst; optionale Argumente
    sind in Expression Trees nicht erlaubt (CS0854). Tests, die `vm.Groups` auswerten (`PickingLeitstandControllerTests.cs:185`, `:242`,
    `:269`), rufen `Index(..., mitSubFa: true)` und sichern damit AK 6. Ein neuer Controller-Test prüft im Verify
    `nurHauptFa: true, faNrUeberSubFa: false` ohne Schalter. `mitSubFa` ist letzter Parameter von `Index`; Positionsaufrufe bleiben
    unverändert.
19. Build und alle Tests grün.
20. Hilfe „Leitstand (Kommissionier-Freigabe)“ enthält die vier Punkte aus Lösungsentwurf Schritt 7: Lack-T bei „Lackteile nur an Sub-FAs“
    über den Schalter (B-1), Eigenwert der HauptFA nur mit Schalter sichtbar (H-1), HauptFA erledigt = Auftrag weg (S-1), Sub-FA-Nummern-Suche
    nur mit Schalter. Die Manifest-Info nennt B-1 ausdrücklich.

## Test-Szenarien

Neues Kapitel in `docs/TESTSZENARIEN.md` (Nummer beim Dev-Start prüfen; nach Teil 1), Verweis auf TS-79 (Kommissionierung nur am HauptFA).

- **Vorbedingung:** IDEAL-Testsystem, Master hierarchisch, `LeitstandAktiv` an, zwei Benutzer (Leitstand-Recht, nur Picking-Recht),
  mindestens drei offene HauptFA mit Sub-FAs, davon einer nicht freigegeben; ein HauptFA mit zwei Sub-FAs, an denen (in der Ansicht
  „Sub-FAs anzeigen“) Glas, Zukauf, Lack-T erledigt und VK unterschiedlich gesetzt werden können; ein HauptFA ohne Sub-FAs.
- **x.1** Leitstand öffnen → nur HauptFA-Zeilen, „+ n Sub-FAs“ in der FA-Nr.-Zelle (AK 1, 13).
- **x.2** HauptFA anhaken, „Markierte freigeben“ (AK 2).
- **x.3** FA-Nr.-Filter mit Sub-FA-Nummer ohne und mit Schalter; FA-Nr. mit HauptFA-Nummer; Artikelfilter mit Sub-FA-Artikel (AK 3, 4).
  Sub-FA-Nummer ohne Schalter, auch per QR-Scan: Hinweis „Keine HauptFA mit dieser Nummer …“, Link → Gruppe mit Schalter (AK 3, S-2).
- **x.4** Kaskade aus der Zeile; an HauptFA ohne Sub-FAs kein Knopf (AK 5). CSV-Export: Spalte FA-Nr. ohne „+ n Sub-FAs“ (AK 13).
- **x.5** Schalter „Sub-FAs anzeigen“, Seite 2, Zurücksetzen (AK 6); an Sub-FA-Zeilen kein Freigabe-Haken (AK 7).
- **x.6** Zusammenfassung: mit Schalter Einzelwerte an den Sub-FAs setzen, Schalter aus, Werte an der HauptFA-Zeile prüfen - Fälle a) bis e)
  (AK 12, d mit gemischt Fertig/Offen → „In Bearbeitung“); VK-Spaltenfilter (AK 14).
- **x.6b** Klick an der HauptFA-Zeile: HauptFA ohne Sub-FAs Glas setzen, Seite neu laden, Wert bleibt; per Schalter prüfen, dass keine
  Sub-FA-Zeile sich geändert hat, Seite lädt dabei nicht von selbst neu (AK 15). Fälle W1, W2, W5 anlegen *(überholt: „W1, W2, W3, W5“)*: Hinweis „Sub-FA bestimmt den
  Wert weiter“ sichtbar, Klick → Seite lädt sofort neu (Filter/Seite bleiben), angezeigter Wert laut AK 16; per Schalter prüfen, dass nur
  die HauptFA-Zeile geschrieben wurde. W4: Auswahl gesperrt mit Text „Arbeitsgang nur an Sub-FAs …“. W3: HauptFA ohne eigene Lackteile, Sub-FA mit
  Lackteilen → Lack-T-erledigt-Haken gesperrt mit Text „Lackteile nur an Sub-FAs …“; Sub-FA-Lackteile per Schalter erledigt setzen,
  Schalter aus → Haken gesetzt, weiter gesperrt. Negativ: Speicherfehler (z. B.
  Sitzung abgelaufen) → alter Wert, Meldung, kein Neuladen; FA-Abarbeitungsliste Status ändern → kein Neuladen (AK 16).
  *(überholt: „Fälle W1-W5 anlegen und Sperre plus Text „bestimmt durch Sub-FA“ prüfen“.)*
- **x.7** Banner mit Auftrag ohne HauptFA-Zeile (AK 8, ggf. nur per Testdaten). HauptFA mit offenen Sub-FAs „Als erledigt markieren“ →
  Auftrag weg, Banner-Zahl unverändert, mit „Erledigte anzeigen“ wieder da (AK 8, S-1).
- **x.8** Hilfe-Abschnitt „Leitstand (Kommissionier-Freigabe)“ öffnen: vier Punkte aus AK 20 vorhanden.
- **Negativ:** AKE-Leitstand (AK 9); FA-Liste IDEAL (AK 10); Benutzer ohne Leitstand-Recht (AK 11); direkter `ToggleRelease`-POST mit
  Sub-FA-Id (AK 7, Entwicklerwerkzeuge oder Testfall im Controller-Test).

## Etappen (nur bei epic: true)

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | (kein Epic) | – | – |

## Deploy

*Final (qa-agent 2026-11-26) aus dem echten Diff `e67e9f6f..ebb16bd7` (Commits 5d7639dc, a89d98cc, ebb16bd7):*

- **Web-App:** ja (`IdealAkeWms/` Controller, Repository, Service, Views, `fa-work-step-status.js`)
- **Service:** nein (nur `IDEALAKEWMSService/AppVersion.cs` auf 1.99.88 — fachlich kein Service-Deploy nötig; das Paket enthält beide Komponenten)
- **Migration:** nein (keine Datei unter `*/Migrations/`, kein `SQL/`)
- **Publish erledigt:** `C:\Git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish` (IDEALAKEWMSWeb + IDEALAKEWMSWebService + SQL + deploy, `publish-info.txt` Version 1.99.88, Commit ebb16bd7). Ablauf: Publish → Testsystem → Test → Merge. Nach dem Merge nur dann neu aus `main` publishen, wenn der Merge getestete Dateien mit parallelen main-Änderungen zusammengeführt hat.
- Reihenfolge: kein Backup/DB-Schritt nötig; Web aktualisieren, danach Nachlauf (Prüfung P-3 + Info) laut Manifest.
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`, beide vorhanden, Version 1.99.88):

| Id | Version | Typ | Firma | Phase | Ausführung | Text |
|---|---|---|---|---|---|---|
| `Pruefung/LeitstandOhneHauptFaZeile` | (Dev-Lauf) | Pruefung | IDEAL | Nachher | anzeigen | P-3 als `COUNT` (mit `IsDonePicking`-Filter, gleiche Zahl wie das Banner, S-1). „Offene Aufträge ohne HauptFA-Zeile: im Leitstand nur mit „Sub-FAs anzeigen“ sichtbar und nicht freigebbar. Materialisierung prüfen.“ |
| `v<Version>-LeitstandNurHauptFa` | (Dev-Lauf) | Info | IDEAL | Nachher | anzeigen | „Der Leitstand zeigt nur noch HauptFA-Zeilen. Lack-T, Glas, Zukauf und VK–VA fassen dort HauptFA und alle Sub-FAs zusammen. Sub-FAs über den Schalter „Sub-FAs anzeigen“ in der Filterkarte; die FA-Nr.-Suche nach Sub-FA-Nummern nur dort. Steht bei Lack-T „Lackteile nur an Sub-FAs“, wird Lack-T erledigt nur über „Sub-FAs anzeigen“ gesetzt. Ist die HauptFA erledigt, verschwindet der Auftrag auch mit offenen Sub-FAs. „Alle Sub-FAs fertigmelden“ steht jetzt in der Zeile.“ |

- **Publish-Befehle:** `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb` (im Worktree).

## Offene Rueckfragen

> [!info] Stand 2026-11-26
> Rückfragen 1-5 sind durch die Freigabe-Antworten entschieden (Text unten bleibt als Verlauf): 1 Freigabe nur am HauptFA, 2 Schalter
> ja/Standard aus, 3 eine Zeile je HauptFA ohne Gruppenkopf und keine Sub-FA-Freigabe, 4 Filter auf HauptFA-Zeile und Sub-FA-Nummern-Suche
> nur mit Schalter, 5 **abweichend von der Empfehlung** Zusammenfassung HauptFA inkl. Sub-FAs. Neu offen: 6 und 7.
> Nachtrag: 6 entschieden (Tabelle ja, VK–VA gemischt = In Bearbeitung), 7 entschieden (Variante c, nur HauptFA-Zeile schreiben). Neu offen: 8.
> Nachtrag 2: 8 entschieden (ii) - bedienbar, Neuladen nach dem Speichern, Hinweis „Sub-FA bestimmt den Wert weiter“; nur W4 bleibt
> gesperrt (kein `FaWorkStep` an der HauptFA-Zeile). Neu offen: 9 (W3).
> Nachtrag 3: 9 entschieden (b) - W3 gesperrt mit „Lackteile nur an Sub-FAs“. Keine Rückfrage mehr offen.
> Nachtrag 4: Die Kritische Prüfung ist eingearbeitet. B-1 (bei 9 b bleiben, Hilfe und Manifest) und S-1 (HauptFA erledigt = Auftrag weg,
> Out-of-Scope) hat Gerald beantwortet. S-2 bis S-4 und H-1 sind in L2/L3/L11, Lösungsentwurf 2/4/5/7, AK 3/5/8/13/18/20 und x.3/x.4/x.7/x.8
> übernommen. Keine Rückfrage offen.

1. **Freigabe-Haken am HauptFA – was fehlte genau?** Der Haken und „Freigeben“ stehen seit v1.46.0 an der HauptFA-Zeile
   (`_PickingLeitstandRow.cshtml:20-31`, `:241-285`). Sie fehlen nur, wenn `LeitstandAktiv` aus ist, der Benutzer kein
   Leitstand-Recht hat, der HauptFA erledigt ist oder es keine HauptFA-Zeile gibt. Bitte Seite, HauptFA-Nummer und Benutzer nennen und
   P-1 bis P-3 ausführen. **Empfehlung:** Ergibt sich ein Daten- oder Rechteproblem, wird es behoben bzw. als Nachtrag in diese Spec
   aufgenommen; sonst bleibt es bei L4 (Bestand absichern, AK 2).
2. **Schalter „Sub-FAs anzeigen“?** Ohne Sub-FA-Zeilen sind Glas, Zukauf, Lack-T und VK-VA je Sub-FA im Leitstand nicht mehr
   bedienbar (nur noch in der FA-Liste und der FA-Struktur). **Empfehlung:** ja, Checkbox in der Filterkarte, Standard aus; die
   heutige Gruppenansicht bleibt damit erreichbar, Kosten ein URL-Parameter.
3. **Darstellung.** **Empfehlung:** eine Zeile je HauptFA ohne Gruppenkopf (ein Kopf über einer einzigen Zeile ist nur Lärm); der Knopf
   „Alle Sub-FAs fertigmelden“ zieht in die Zeilen-Aktionen der HauptFA-Zeile. Alternative: Gruppenkopf behalten, Badge ohne Zahl.
4. **Filter.** **Empfehlung:** alle Filter auf die HauptFA-Zeile (was man sieht, wird gefiltert); einzige Ausnahme FA-Nr.: eine
   Sub-FA-Nummer (z. B. vom Werkstattpapier gescannt) findet ihren HauptFA. Alternative: Gruppe erscheint, wenn irgendeine Sub-FA
   passt (dann treffen z. B. Artikel- und Datumsfilter unsichtbare Zeilen).
5. **Sub-FA-Werte an der HauptFA-Zeile zusammenfassen?** (z. B. Lack-T, wenn irgendeine Sub-FA Lackteile hat.) **Empfehlung:** nein,
   die HauptFA-Zeile zeigt ihre eigenen Werte; die Einzelwerte bleiben über den Schalter erreichbar. Eine Zusammenfassung wäre eine neue
   Regel je Spalte.
6. **Zusammenfassungsregel je Spalte bestätigen** (Folge von Antwort 5). Code für eine Zusammenfassung von Lack-T/Glas/Zukauf/VK–VA je
   HauptFA gibt es nicht; die Kommissionierliste fasst Stücklisten**positionen** je HauptFA zusammen, nicht diese Spalten (Ist-Stand,
   „Gibt es die Zusammenfassung schon?“). Vorschlag siehe Tabelle Z1-Z6: Lack-T vorhanden, Glas, Zukauf = irgendeine Zeile; Lack-T erledigt =
   alle Zeilen mit Lackteilen; VK–VA = schlechtester Stand wie BDE-Teileliste (alle fertig → fertig, irgendeine in Bearbeitung → in
   Bearbeitung, sonst offen); stornierte Sub-FAs zählen nicht, erledigte zählen mit; alle anderen Spalten (Status, Termine, Artikel,
   Freigabe …) bleiben Werte der HauptFA-Zeile; dazu „+ n Sub-FAs“ in der FA-Nr.-Zelle (L11). Offen ist vor allem VK–VA bei „gemischt
   fertig/offen“: offen (Vorschlag, wie BDE-Teileliste) oder in Bearbeitung? **Empfehlung:** Tabelle wie vorgeschlagen.
7. **Bedienbarkeit der zusammengefassten Zellen.** Lack-T-erledigt, Glas, Zukauf und VK–VA sind heute je Zeile bedienbar
   (`_PickingLeitstandRow.cshtml:102-156`). Eine zusammengefasste Zelle gehört zu keiner einzelnen Zeile. Varianten: (a) nur Anzeige,
   gesperrt mit Hinweistext, ändern über „Sub-FAs anzeigen“ - einfachster Weg, aber dann wird der Schalter doch gebraucht (Antwort 2:
   „eigentlich nicht benötigt“); (b) ein Klick an der HauptFA-Zeile schreibt den Wert auf **alle** Zeilen des Auftrags (neuer
   Sammel-Schreibweg mit Audit-Feldern; bei VK–VA je vorhandenem Arbeitsgang); (c) ein Klick schreibt nur die HauptFA-Zeile - die
   Anzeige kann dann trotz Klick unverändert bleiben (z. B. Glas bleibt gesetzt, weil eine Sub-FA es trägt), das verwirrt.
   **Empfehlung:** (a) für alle vier Spalten; (b) bei Bedarf als eigener Backlog-Eintrag. Ein HauptFA ohne Sub-FAs bleibt in allen
   Varianten bedienbar wie heute.
8. **Zellen, in denen Sub-FAs den Wert bestimmen (Folge von Antwort 7 (c)).** Weil ein Klick nur die HauptFA-Zeile schreibt, die Anzeige
   aber zusammengefasst bleibt und erst beim Neuladen neu berechnet wird, gibt es Fälle, in denen der Klick nach dem Neuladen verschwindet
   oder nichts bewirkt (Tabelle „Klickverhalten“): W1 Glas/Zukauf abwählen, während eine Sub-FA sie trägt (Haken kommt wieder); W2 Lack-T
   erledigt setzen, während eine Sub-FA-Lackteilzeile offen ist (Haken verschwindet wieder); W3 Lack-T erledigt an einer HauptFA-Zeile ohne
   eigene Lackteile (wirkungslos); W5 VK–VA an der HauptFA wählen, während Sub-FAs abweichen (zeigt danach „In Bearbeitung“). W4 (Arbeitsgang
   nur an Sub-FAs) ist ohnehin nicht schreibbar und gesperrt. Mit der Regel aus Antwort 6 heißt das praktisch: Sobald eine Sub-FA den
   Arbeitsgang VK–VA hat oder Glas/Zukauf trägt, kann die HauptFA-Zeile den angezeigten Wert nicht mehr frei bestimmen. Varianten:
   (i) gesperrt mit Kurztext „bestimmt durch Sub-FA“, ändern über „Sub-FAs anzeigen“; (ii) bedienbar, nach dem Speichern wird die Seite
   neu geladen und die Zelle zeigt sofort den zusammengefassten Wert mit Hinweis „Sub-FA bestimmt den Wert weiter“; (iii) bedienbar ohne
   Sonderbehandlung (Klick wirkt erst beim Neuladen, ggf. scheinbar nicht). **Empfehlung:** (i) - nie ein Klick, der still verpufft;
   Kriterium „jeder wählbare Wert an der HauptFA-Zeile erscheint auch als Zusammenfassung“. Der Dev-Lauf baut (i), bis die Antwort da ist.
9. **W3: Lack-T erledigt an einer HauptFA-Zeile ohne eigene Lackteile.** Der Haken erscheint dort nur wegen Sub-FAs (Z1). Ein Klick
   setzt `IsCoatingDone` an einer Zeile, die in Z2 nicht zählt. Die Anzeige ändert sich also auch nach dem Neuladen nie. Der nächste
   Materialisierungslauf kann den Wert wieder zurücksetzen (`FaMaterializationSyncService.cs:216-228`). Damit ist das ein Klick, der nie
   wirkt, auch mit Hinweis. Antwort 8 (ii) sagt „bedienbar“; der Dev-Lauf baut das so, bis die Antwort da ist. Varianten:
   (a) wie Antwort 8, bedienbar mit Neuladen und Hinweis; (b) nur diese Zelle wie W4 sperren, mit Text „Lackteile nur an Sub-FAs – ändern
   mit „Sub-FAs anzeigen““. **Empfehlung:** (b), weil der Hinweis „bestimmt weiter“ hier falsch wäre (die HauptFA bestimmt gar nicht mit).

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. Freigabe-Haken am HauptFA – Befund und Ergebnis P-1 bis P-3 → **nur noch am haupt fa weil wir ja nur noch hauptfa freigeben und anzeigen wollen** (Gerald 2026-11-26, Chat)
2. Schalter „Sub-FAs anzeigen“ (Standard aus)? → **ja wird eigentlich nicht benötigt werden** (Gerald 2026-11-26, Chat)
3. Eine Zeile je HauptFA ohne Gruppenkopf, Kaskaden-Knopf in der Zeile? → **ja - subfas dürfen nicht gesondert freigegeben werden. Nur noch hauptfa** (Gerald 2026-11-26, Chat)
4. Filter auf HauptFA-Zeile, FA-Nr. findet auch über Sub-FA-Nummer? → **nur wenn checkbox welche eben eigentlich hinfällig ist** (Gerald 2026-11-26, Chat)
5. Keine Zusammenfassung der Sub-FA-Werte an der HauptFA-Zeile? → **alles was für diesen hauptfa inkl subfa (logik haben wir glaube ich schon bei der kommissionierliste so)** (Gerald 2026-11-26, Chat)
6. Zusammenfassungsregel je Spalte wie Tabelle Z1-Z6 (VK–VA gemischt fertig/offen = offen)? → **Tabelle ja, aber VK–VA gemischt fertig/offen = „In Bearbeitung“ (nicht offen)** (Gerald 2026-11-26, Chat)
7. Bedienbarkeit zusammengefasster Zellen: (a) nur Anzeige / (b) auf alle Zeilen schreiben / (c) nur HauptFA-Zeile? → **(c) Klick schreibt nur die HauptFA-Zeile** (Gerald 2026-11-26, Chat)
8. Zellen, in denen Sub-FAs den Wert bestimmen (W1-W5): (i) gesperrt mit „bestimmt durch Sub-FA“ / (ii) bedienbar mit Neuladen und Hinweis / (iii) bedienbar ohne Sonderbehandlung? → **(ii) bedienbar, nach dem Speichern Neuladen und Hinweis „Sub-FA bestimmt den Wert weiter“** (Gerald 2026-11-26, Chat)
9. W3 Lack-T erledigt an HauptFA ohne eigene Lackteile: (a) wie Antwort 8 bedienbar / (b) nur diese Zelle sperren mit „Lackteile nur an Sub-FAs“? → **(b) nur diese Zelle sperren mit „Lackteile nur an Sub-FAs“** (Gerald 2026-11-26, Chat)

## Kritische Pruefung (2026-11-26)

Geprüft gegen den Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5` (AppVersion 1.99.84, HEAD 7268dcb6). Alle neun
Freigabe-Antworten sind beantwortet. Rumpf, L-Tabelle, Klickverhalten, Lösungsentwurf und AK 12-18 sind nach den Antworten 5-9
nachgezogen. Zwischen Antwortblock und Rumpf habe ich keinen Widerspruch gefunden. Stichproben belegt: `nurHauptFa`-Zweig
`ProductionOrderRepository.cs:108-120`/`:168-174`, `CountGruppenOhneHauptFaAsync` `:184-190`, Aufruf ohne `nurHauptFa`
`PickingLeitstandController.cs:208-209`, Zellen `_PickingLeitstandRow.cshtml:102-156`, Checkbox-Handler ohne Neuladen
(`Index.cshtml`, `.toggle-field`-Block), `fa-work-step-status.js` mit genau zwei Einbindungen (`FaWorklist/Index.cshtml:217`,
`PickingLeitstand/Index.cshtml:457`).

### BLOCKER

- **B-1 Lack-T erledigt an Sub-FAs geht nur noch über den Schalter. Ist das der Normalfall?** Belegt: `IsCoatingDone = true`
  schreibt nur der Toggle `/api/picking-status/toggle` (`ProductionOrderPickingStatusRepository.cs:58`). Der Toggle wird nur in
  `Views/PickingLeitstand` verwendet (Suche `picking-status/toggle` in `Views`/`wwwroot/js`: nur Leitstand und Changelog). Sonst setzen
  nur Resets den Wert (`:188`, `IDEALAKEWMSService/Services/CoatingDetectionService.cs:204`). Lack-T wird je Sub-FA abgeleitet
  (`FaMaterializationCoating.cs:9-30`). Hat die HauptFA-Zeile keine eigenen Lackteile, ist der Haken an der HauptFA-Zeile nach
  Antwort 9 (b) gesperrt (W3). Dann setzt man „Lack-T erledigt“ im ganzen WMS nur noch über „Sub-FAs anzeigen“. Dasselbe gilt für
  Glas/Zukauf einzelner Sub-FAs. Antwort 2 sagt aber: „wird eigentlich nicht benötigt werden“. Ist W3 der häufige Fall, bleibt die
  tägliche Lack-T-Pflege hinter einem Schalter, den Gerald für überflüssig hält.
  **Frage an Gerald:** Bitte vor der Freigabe P-5 auf der WMS-DB IDEAL ausführen (Testsystem, nur lesen). Ist „nur Sub-FAs (W3)“ der
  überwiegende Fall: Soll es bei 9 (b) bleiben, also Pflege über den Schalter (dann Hilfetext und Manifest-Info ausdrücklich so)? Oder
  soll der Haken an der HauptFA-Zeile in W3 **alle** Sub-FA-Lackteilzeilen des Auftrags erledigt setzen? Das wäre Variante 7 (b) nur
  für Lack-T, mit neuem Sammel-Schreibweg und Audit-Feldern, und eine neue Entscheidung.

```sql
-- P-5 (Vorschlag Pruefung, WMS-DB IDEAL, nur lesen): offene Auftraege mit Lackteilen an Sub-FAs - hat die HauptFA-Zeile eigene?
SELECT CASE WHEN hps.HasCoatingParts = 1 THEN N'HauptFA hat eigene Lackteile (W2/K1)'
            ELSE N'nur Sub-FAs (W3, gesperrt)' END AS Fall,
       COUNT(*) AS Auftraege
FROM dbo.ProductionOrders h
JOIN dbo.ProductionOrderPickingStatus hps ON hps.ProductionOrderId = h.Id
WHERE (h.SubOrderNumber = N'' OR h.SubOrderNumber = h.OrderNumber)
  AND h.IsDone = 0 AND h.IsCancelled = 0 AND hps.IsDonePicking = 0
  AND EXISTS (SELECT 1 FROM dbo.ProductionOrders s
              JOIN dbo.ProductionOrderPickingStatus sps ON sps.ProductionOrderId = s.Id
              WHERE s.OrderNumber = h.OrderNumber AND s.Id <> h.Id AND s.IsCancelled = 0 AND sps.HasCoatingParts = 1)
GROUP BY CASE WHEN hps.HasCoatingParts = 1 THEN N'HauptFA hat eigene Lackteile (W2/K1)' ELSE N'nur Sub-FAs (W3, gesperrt)' END;
```

### SOLLTE

- **S-1 HauptFA erledigt: Der ganze Auftrag verschwindet still, mit seinen offenen Sub-FAs.** Belegt: `showDone = false` filtert
  die HauptFA-Zeile über `IsDone`/`IsCancelled`/`IsDonePicking` (`ProductionOrderRepository.cs:158-159`). Das Banner zählt nur Aufträge
  **ganz ohne** HauptFA-Zeile, denn `mitHauptFa` wird ungefiltert über alle Zeilen gebildet (`:186`). Heute bleiben in der Gruppenansicht
  die offenen Sub-FA-Zeilen mit Lack-T/VK–VA sichtbar, auch nach „Als erledigt markieren“ an der HauptFA-Zeile. Dieser Knopf setzt
  `IsDonePicking` (`PickingController.cs:197-207`) und steht in jeder Zeile (`_PickingLeitstandRow.cshtml:218`). Nach dieser Spec sind
  die offenen Sub-FAs ohne Meldung weg. Die FA-Liste hat dasselbe als Q-4 festgehalten: DEV 1035235/1037045/1039413/1043153, dort
  „Mensch bestätigt fachlich“, noch offen.
  **Vorschlag:** Out-of-Scope ausdrücklich um diesen Satz ergänzen: „Ist die HauptFA-Zeile erledigt, fertig kommissioniert oder
  storniert, verschwindet der Auftrag ohne Banner, auch wenn Sub-FAs offen sind; sichtbar mit „Erledigte anzeigen“.“ Gerald soll ihn
  für den Leitstand bestätigen, oder das Banner zählt diese Aufträge mit.
  Dazu passt P-3 / Manifest `Pruefung/LeitstandOhneHauptFaZeile` nicht zum Banner. P-3 filtert die Zeilen nur über
  `IsDone = 0 AND IsCancelled = 0` und ohne `IsDonePicking`, `CountGruppenOhneHauptFaAsync(false)` zieht `IsDonePicking` mit ab. Die
  Zahlen können also auseinanderlaufen. Vorschlag: P-3 um
  `LEFT JOIN dbo.ProductionOrderPickingStatus ps ON ps.ProductionOrderId = po.Id … AND ISNULL(ps.IsDonePicking, 0) = 0` ergänzen.
- **S-2 Sub-FA-Nummer ohne Schalter: stilles „Keine … gefunden“ (melden statt still).** Antwort 4 lässt die Suche nach einer
  Sub-FA-Nummer ohne Schalter leer ausgehen. Sie entsteht auch per QR-Scan: Der Scanner der FA-Nr. sendet das Formular sofort ab
  (`Index.cshtml:512-515`). Vom Werkstattpapier gescannt ist das eine Sub-FA-Nummer, und der Bediener sieht nur eine leere Liste.
  **Vorschlag:** Neues AK: Ist der FA-Nr.-Filter gesetzt und sind ohne Schalter 0 Treffer da, zeigt die Seite `alert-info`
  „Keine HauptFA mit dieser Nummer – Sub-FA-Nummern findet „Sub-FAs anzeigen““ mit Link auf dieselbe URL und `mitSubFa=true`.
  Kosten: eine Bedingung im View. Das ist keine Gegenentscheidung zu Antwort 4, nur ein Hinweis.
- **S-3 Bestehende Tests brechen, und die Spec nennt sie nicht.** Ein weiterer optionaler Parameter an `GetForLeitstandGroupedAsync`
  bricht alle Moq-Setups/Verifies mit 8 `It.IsAny`. Optionale Argumente sind in Expression Trees nicht erlaubt (CS0854):
  `PickingLeitstandControllerTests.cs:172`, `:230`, `:246`, `:1050`, `ProductionOrdersControllerTests.cs:118`, `:678`. Dazu werten
  Controller-Tests `vm.Groups` aus (`PickingLeitstandControllerTests.cs:185`, `:242`, `:269`). Mit Default `mitSubFa = false` liefert
  der Controller künftig `Items`, die Tests werden also rot.
  **Vorschlag:** affected_code und AK 18/19 ergänzen. Diese Tests werden auf den neuen Parameter angepasst. Tests, die die
  Gruppenansicht prüfen, rufen `Index(..., mitSubFa: true)` auf, damit sie „Schalter an = heute“ (AK 6) absichern. Ein neuer Test prüft
  `nurHauptFa: true, faNrUeberSubFa: false` im Verify. `mitSubFa` gehört als **letzter** Parameter an `Index`, sonst verschieben sich
  die Positionsaufrufe (z. B. `_controller.Index(null, null, "Mustermann", false, 1, null)`).
- **S-4 „+ n Sub-FAs“ und den Kaskaden-Knopf genauer festlegen.** n zählt nach der Basis der Zusammenfassungsregel nicht stornierte
  Zeilen **inklusive erledigter**. Das heutige Badge zählt dagegen die gefilterten `group.Items` (`Index.cshtml:213`), das kann der
  Bediener verwechseln. Der Kaskaden-Knopf erscheint nach L3 an jeder HauptFA-Zeile, auch ohne Sub-FAs.
  **Vorschlag:** AK 13 bekommt den Zusatz „n = nicht stornierte Sub-FA-Zeilen inkl. erledigter“. L3 bekommt den Zusatz „Knopf nur bei
  n > 0“. Die FA-Nr.-Zelle bekommt `data-csv-value` mit der FA-Nummer, sonst landet „+ 2 Sub-FAs“ in der CSV-Spalte FA-Nr.
  (`csv-export.js:85`, `:98`).

### HINWEIS

- **H-1 Verdeckter Eigenwert der HauptFA-Zeile (W2).** In W2 steht `IsCoatingDone = true` an der HauptFA-Zeile, die Zelle zeigt aber
  „leer“. Zurücknehmen geht an der HauptFA-Zeile nicht, ein Klick setzt erneut true. Das ist Folge von Antwort 7 (c) und akzeptabel.
  Es sollte in den Hilfetext: „Eigenwert der HauptFA nur mit „Sub-FAs anzeigen“ sichtbar“.
- **H-2 `GetByProductionOrderIdsAsync` arbeitet ohne Blöcke** (`ProductionOrderPickingStatusRepository.cs:23-30`, `ids.Contains`).
  Der Detail-Pivot dagegen ist in Blöcke geteilt (`IFaWorkStepRepository.cs:22-26`). Die Id-Liste wächst um die Sub-FA- und erledigten
  Zeilen. Bei aktiven Memory-Filtern (alle Gruppen geladen, `PickingLeitstandController.cs:206-209`) hat das Risiko schon heute
  dieselbe Größenordnung, neu ist es also nicht. Im QA-Lauf mit einem VK-Spaltenfilter auf DEV IDEAL einmal ausprobieren.
- **H-3 Version/TS:** Die Übersicht nennt v1.99.83 / TS-204, der Worktree steht schon auf 1.99.84. Der Dev-Lauf nimmt die nächste
  freie Nummer (voraussichtlich v1.99.85) und klärt die Reihenfolge mit Teil 3.
- **H-4 Ist-Stand, Absatz „Hierarchischer Zweig“:** Dort stehen noch die alten Repository-Zeilen (`:54-92`, `:107-111`). Die Notiz
  darüber kennzeichnet sie als überholt. Der Dev-Lauf soll die aktuellen Zeilen aus der Notiz nehmen.
- **H-5 Umfang/Größe/Deploy:** etwa 10 Anwendungsdateien plus 3-4 Testdateien, keine Migration, nur Web. Das passt in einen Dev-Lauf,
  ein Split ist nicht nötig. Deploy (web ja, service/migration nein) ist plausibel. Die Test-Checkliste x.1-x.7 kann man abarbeiten.
  Für x.6/x.6b braucht es per Schalter angelegte Testdaten. Dabei entstehen echte Statusänderungen auf dem Testsystem, nicht auf Prod.

NACHBESSERUNG NOETIG: B-1 (Lack-T erledigt an Sub-FAs nur noch über „Sub-FAs anzeigen“ – Häufigkeit mit P-5 klären, 9 (b) bestätigen); S-1 Abgrenzung „HauptFA erledigt“ bestätigen.

**Antworten Gerald 2026-11-26 (Chat) zur Kritischen Prüfung:**
- B-1: „Bei 9 (b) bleiben“ – W3-Zelle gesperrt mit „Lackteile nur an Sub-FAs“, erledigt wird über „Sub-FAs anzeigen“; kein
  Sammel-Schreibweg. Hilfetext und Manifest-Info sagen das ausdrücklich.
- S-1: „Ja, passt so“ – ist die HauptFA-Zeile erledigt, fertig kommissioniert oder storniert, ist der Auftrag für den Leitstand
  erledigt; kein Banner (bewusste Abgrenzung, gleich wie Q-4 der FA-Liste).

## QA-Nachweis (qa-agent, 2026-11-26)

Geprüft: Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Branch `feature/2026-08-07-ideal-teile-1-5`, HEAD `ebb16bd7`,
Commit-Bereich `e67e9f6f..ebb16bd7` = 5d7639dc (Teil 2), a89d98cc (Teil 1 Nachtrag N-1), ebb16bd7 (Fix aus der abgebrochenen QA,
hier mitgeprüft). Im Hauptcheckout lagen keine unfertigen Brain-Änderungen der abgebrochenen QA (beide Spec-Dateien sauber).

### 1. Build und Tests
- `dotnet build IdealAkeWms.slnx`: 0 Fehler, 24 Warnungen (NuGet-Advisories, vorbestehend).
- `dotnet test`: IdealAkeWms.Tests 3820 bestanden / 1 übersprungen / 0 Fehler (3821); IDEALAKEWMSService.Tests 848 / 0 / 0.

### 2. Code-Review (selbst, in diesem Lauf)
Gelesen: kompletter Diff der drei Commits plus Umfeld (`PickingLeitstandController.Index` inkl. Memory-Filter und `LeitstandMemoryText`,
`BuildLeitstandQuery`/`ApplyLeitstandColumnFilter`, `_Pagination` + `site.js`-Navigation, `csv-export.js:85`, `CascadeDonePreview`,
`BeschichtungsauftraegeController.Pdf` = rendert dieselbe View `Print`).
Fehlerklassen geprüft: keine Werte in `<script>` eingebettet (`mitSubFaUrl` nur als `href`, Razor-encodiert); kein `hidden` an
`d-*`-Elementen; keine stillen Fallbacks (W4 = `FaWorkStepId 0` wird sichtbar gesperrt, nicht verschluckt); keine Parallel-Arrays; die
einzige reine Client-Sperre (W3) ist laut AK 16 ausdrücklich nur Oberfläche; eine Regel an einem Ort (`LeitstandHauptFaZusammenfassung.Regel`;
die Memory-Filter lesen den bereits zusammengefassten Wert, weil `Anwenden` in `MapItem` vor den Filtern läuft). ebb16bd7: Razor rendert
`data-*`-Attribute auch bei `null` (`data-neu-laden=""`), daher Selektor `[data-neu-laden="true"]` — korrekt, auf DEV belegt (K2 lädt nicht neu).
Befunde:
- **F-1 (Hinweis, vorbestehend, nicht behoben):** Kaskaden-Dialog nennt bei 1064052 „39 Sub-FA(s)“, die Zeile „+ 38 Sub-FAs“.
  `CascadeDonePreview` zählt mit `CountByOrderNumberAsync` alle Zeilen der `OrderNumber` inkl. HauptFA-Zeile (DEV: 1 H + 38 S). Nicht
  durch diesen Diff entstanden, steht jetzt aber direkt neben „+ n“ → Folgeaufgabe vorgeschlagen, nicht mitgebaut.
- **F-2 (Hinweis, vorbestehend):** Spaltenfilter Glas/Zukauf (`data-filterable`, Keys `glass`/`purchase`) haben weder SQL-Prädikat
  (`ApplyLeitstandColumnFilter` → `_ => null`) noch Memory-Filter; sie wirken nicht. L2 „Filter auf den angezeigten Wert“ ist für
  Glas/Zukauf damit nicht erfüllbar, aber auch nicht verschlechtert.
- **F-3 (Hinweis, Oberfläche):** Der Kurztext „Sub-FA bestimmt den Wert weiter“ macht die VK-Spalte im Nur-HauptFA-Modus breit. Kosmetik.
Keine blockierenden Befunde.

### 3. Nachweisart je AK
| AK | Nachweis |
|---|---|
| 1, 13 (Anzeige) | Controller-Test `Index_Hierarchical_OhneSchalter_NurHauptFa_FlacheZeilen_Zusammengefasst` + DEV (OK); CSV `data-csv-value` nur Code |
| 2, 5 (Ausführen), 7 (POST), 8 (Banner mit Daten), 11 | Manual-UAT |
| 3, 18 | `FaListeNurHauptFaTests` (InMemory) + DEV (OK) |
| 4 | Manual-UAT |
| 6 | Controller-Test `Index_Hierarchical_MitSchalter_Gruppenansicht_OhneZusammenfassung` + DEV (OK) |
| 9 | DEV AKE (OK) |
| 10 | bestehende `FaListeNurHauptFaTests` grün |
| 12, 17 | `LeitstandHauptFaZusammenfassungTests` (rein, DB-frei) |
| 14 | DEV (OK); Memory-Filter-Pfad nicht unit-getestet |
| 15, 16 | Browser-JS → Manual-UAT, auf DEV belegt (K2, W1, W5; W3/Fehlerfall offen) |
| 20 | Hilfe-View im Diff geprüft, Manual-UAT |

### 4. Geänderte bestehende Tests
- `PickingLeitstandControllerTests`: Moq-Setups/Verifies um `It.IsAny<bool>()` ergänzt = Typ-Anpassung (CS0854); drei `Index(...)`-Aufrufe
  mit `mitSubFa: true` = beabsichtigte Verhaltensänderung (AK 6/18).
- `ProductionOrdersControllerTests`: nur Moq-Signatur = Typ-Anpassung.
- `BeschichtungsauftraegeControllerTests`: `PositionenGesamt` entfällt = beabsichtigt (Teil 1, Nachtrag N-1).
- `FaListeNurHauptFaTests`: nur neue Tests. Keine Kategorie „Sonstiges“.

### 5. Checkliste CLAUDE.md / 5a ADR 0020
Keine Migration, kein SQL (bestätigt). Version 1.99.88 in beiden `AppVersion.cs`, Anwender-Changelog ergänzt. TS-208 in
`docs/TESTSZENARIEN.md` + Testindex (Hauptcheckout). Hilfe „Leitstand (Kommissionier-Freigabe)“ ergänzt. Manifest-Einträge
`Pruefung/LeitstandOhneHauptFaZeile` und `v1.99.88-LeitstandNurHauptFa` in `scripts/deploy/nachlauf.psd1` vorhanden. Keine Einstellung → ADR 0020 entfällt.

### 6. DEV-Deploy und Testlauf
- Standort laut Frontmatter IDEAL; wegen N-1 (Teil 1, `standort: beide`) auf **AKE und IDEAL** deployt (`dev-deploy.ps1 -Standort AKE|IDEAL
  -Quelle <worktree>`): je Exit 0, „HTTP 200, Fusszeile v1.99.88“ (AKE http://localhost:8081, IDEAL http://localhost:8082).
- Testdaten IDEAL_WMS_DEV (aus der abgebrochenen QA): 1059463 (Subs mit Glas/Zukauf = W1), 1060414 (HauptFA + Subs mit Lackteilen = W2),
  1064052 (VK HauptFA Fertig, Sub Offen = W5), 1065036 (VK nur an Subs = W4).

| Schritt | Ergebnis | Status |
|---|---|---|
| 208.1 Leitstand IDEAL | „4 Aufträge (nur HauptFA)“, keine Gruppenköpfe, „+ 67/35/38/36 Sub-FAs“ | OK |
| 208.3 FA-Nr. Sub-FA `1065208` ohne Schalter | 0 Zeilen, Hinweis „Keine HauptFA mit dieser Nummer …“; Link → `…&mitSubFa=true` zeigt Gruppe 1064052 mit Zeile 1065208 | OK |
| 208.3 Artikelfilter mit Sub-FA-Artikel (AK 4) | nicht geprüft | NICHT MOEGLICH (Zeit) → Mensch |
| 208.4 Kaskaden-Knopf in der Zeile | vorhanden, Dialog öffnet (abgebrochen, nicht ausgeführt); 39 vs. „+ 38“ siehe F-1 | OK mit Hinweis |
| 208.5 Schalter | Gruppenansicht wie bisher; „Zurücksetzen“ = `/PickingLeitstand` ohne Parameter; Seitenwechsel behält Query (`site.js navigate`, nur Code) | OK |
| 208.6 Zusammenfassung | W1 Glas/Zukauf gesetzt + Hinweis; W2 Lack-T Hinweis; W5 VK „In Bearbeitung“ + Hinweis; W4 VK gesperrt „Arbeitsgang nur an Sub-FAs …“ | OK |
| 208.6 VK-Filter `colf_cooling=bearbeitung` | trifft 1064052 + 1065036 (zusammengefasster Wert; 1064052 selbst Fertig) | OK |
| 208.6b K2 Glas an 1064052 setzen und zurück | POST 200, **kein** Neuladen; DB nur Zeile 199 (`ModifiedBy` gesetzt) | OK |
| 208.6b W1 Glas an 1059463 abwählen | POST 200 + sofort GET `/PickingLeitstand`; danach wieder gesetzt; DB nur Zeile 131 | OK |
| 208.6b W5 VK 1064052 → Fertig | POST `set-status` 200 + Neuladen; Anzeige „In Bearbeitung“; DB nur FaWorkStep 3, Sub 200 unverändert | OK |
| 208.6b W3 | kein offener DEV-Auftrag mit Lack nur an Subs im Leitstand | NICHT MOEGLICH → Mensch |
| 208.6b Speicherfehler | nicht provoziert (löst `alert()` aus) | NICHT MOEGLICH → Mensch |
| 208.7 Banner | P-3 auf DEV = 0, kein Banner (konsistent) | Banner mit Daten NICHT MOEGLICH → Mensch |
| 208.2, 208.8, Negativ ToggleRelease/Picking-Recht | nicht im Browser ausgeführt (Freigabe legt KO an; Benutzerwechsel) | NICHT MOEGLICH → Mensch |
| Negativ AKE | Leitstand AKE ohne Schalter und ohne „nur HauptFA“ | OK |

### 7. Publish
`C:\Git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish` — IDEALAKEWMSWeb + IDEALAKEWMSWebService (Release, Ordner
vorher geleert), `SQL\`, `deploy\`, `publish-info.txt` (Version 1.99.88, Commit ebb16bd72caa92f6616f37f680380b7d9146d66c). Kein Paket-ZIP (Auftrag).

### Manuelle Test-Checkliste (Mensch)
1. TS-208.2 Massenfreigabe an einer HauptFA-Zeile, KO prüfen (Regression TS-79).
2. TS-208.3 Artikelfilter mit Artikelnummer einer Sub-FA → keine Zeile (AK 4); QR-Scan einer Sub-FA-Nummer → Hinweis.
3. TS-208.4 Kaskade ausführen; Zahl im Dialog (zählt HauptFA-Zeile mit, F-1) bewerten; HauptFA ohne Sub-FAs → kein Knopf; CSV FA-Nr. ohne „+ n“.
4. TS-208.5 Seite 2 mit Schalter; Sub-FA-Zeilen ohne Freigabe-Haken.
5. TS-208.6b W3 gesperrt mit Text; Speicherfehler → alter Wert, kein Neuladen; FA-Abarbeitungsliste Status ändern → kein Neuladen.
6. TS-208.7 Banner mit Auftrag ohne HauptFA-Zeile; Manifest-Prüfung P-3 = Bannerzahl; HauptFA „Als erledigt markieren“ → Auftrag weg.
7. TS-208.8 Hilfe-Abschnitt; Negativ: Benutzer nur Picking-Recht, `ToggleRelease`-POST mit Sub-FA-Id.
8. Gesamtlauf TS-208 am Testsystem IDEAL mit echten Aufträgen.
