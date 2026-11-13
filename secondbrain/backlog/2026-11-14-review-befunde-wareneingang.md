---
type: backlog
created: 2026-11-14
quelle: "[[2026-11-14-wochenend-review-test-anwenderdoku]]"
status: neu
modul: Wareneingang Teil 1–3 (Wareneingang zu Bestellung, Sage-WE-Beleg, E5, Rahmenabrufe, Barcode/CSV, Überlieferung/Hinweise)
stand: "Bündel v1.99.22, 90bbbbd7, feature/2026-08-07-ideal-teile-1-5"
---
# Review-Befunde Wareneingang (Teil A2, lesend)

Gegenstand: `git diff main...HEAD`, Bündel-Worktree. Nichts behoben (Schranke 1). Specs: [[2026-11-08-wareneingang-aus-sage-bestellungen-spec]],
[[2026-11-08-wareneingang-teil-2-sage-we-beleg-rahmenabrufe-spec]], [[2026-11-11-wareneingang-teil-3-rahmenabrufe-spec]],
[[2026-11-12-wareneingang-barcode-lieferschein-csv-export-spec]]. Pfade relativ zum Worktree.

## WE-1 · hoch · Doppeltes Absenden bucht doppelt, mit Sage-WE zwei Belege in Sage

- **Fundstelle:** `IdealAkeWms/Controllers/PurchaseOrdersController.Receive.cs:190-293` (POST `Receive`), `IdealAkeWms/Views/PurchaseOrders/Receive.cshtml:72`, `:246`.
- **Befund:** Der POST hat keinen Idempotenz-Schlüssel. Jeder Aufruf erzeugt `Guid.NewGuid()` als neuen `SageWeVorgang` (`Receive.cs:256`) und neue
  `StockMovement`s. Der Knopf „Einbuchen“ wird beim Absenden nicht gesperrt. Im View und in `wwwroot/js/site.js` gibt es keinen Submit-Schutz
  (Suche nach `submit`-Handlern: keine Treffer). Die Rest-Prüfung schützt nur gegen eine Überlieferung ohne Häkchen. Bei einer Teillieferung oder
  mit gesetztem Häkchen „Überlieferung“ besteht auch die zweite Anfrage die Prüfung.
- **Szenario:** Bestellung mit Rest 100, Ist-Menge 30. Ein Doppelklick auf „Einbuchen“ (oder: Netz am Handgerät hängt, die Seite wird neu gesendet)
  erzeugt zwei Einbuchungen zu je 30. Ist der Sage-WE an, entstehen zwei Vorgänge, also zwei ELL-Belege in Sage. Die E5-Suche vor dem Senden greift
  nicht, denn jeder Vorgang hat seine eigene Kennung `WE#<id>#` und gilt als Erstversand. Bei ausgeschaltetem Sage-WE entstehen zwei
  Lagerbuchungen in der Queue.
- **Vorschlag:** Einmal-Token als Hidden-Feld beim GET erzeugen und als `SageWeVorgang` übernehmen, mit UNIQUE-Prüfung oder vorheriger
  Existenzprüfung: Ein zweiter POST mit demselben Token meldet „bereits gebucht“. Zusätzlich den Knopf beim Absenden sperren, das ist aber nur
  Komfort. Manual-UAT: Doppelklick am Handgerät.

## WE-2 · mittel · Mehrzeilige Buchung ist nicht atomar

- **Fundstelle:** `PurchaseOrdersController.Receive.cs:257-290` (Schleife mit `_movements.AddAsync` je Zeile),
  `IdealAkeWms/Data/Repositories/SageBookingEnqueueingStockMovementRepository.cs:43` → `Repository.cs:33-38` (`SaveChangesAsync` je Aufruf).
- **Befund:** Jede Position wird mit eigenem `SaveChanges` gespeichert. Die Zusage „Gebucht wird nur, wenn alle eingegebenen Zeilen gültig sind“
  (`Receive.cshtml:241`, Controller-Kommentar „keine Teilbuchung“) deckt nur die Prüfung ab, nicht das Speichern.
- **Szenario A:** Das Speichern von Position 3 scheitert (Deadlock, Timeout, DB-Fehler). Position 1–2 sind gebucht, der Anwender sieht eine
  Fehlerseite statt „nichts gebucht“. Erneutes Absenden bucht 1–2 ein zweites Mal (siehe WE-1).
- **Szenario B:** Der SageBookingWorker (eigener Prozess, Takt 20 s, `SageWareneingangService.cs:62-64`) liest den Vorgang zwischen zwei
  `SaveChanges`. Dann geht ein ELL-Beleg nur mit den ersten Positionen raus, die restlichen Positionen desselben Vorgangs folgen als zweiter Beleg.
  Das widerspricht F2 („EIN Vorgang = ein ELL-Beleg“). Das Zeitfenster ist klein, aber real.
- **Vorschlag:** Alle Bewegungen in einer Transaktion speichern, also ein `SaveChanges` für alle Zeilen. Die Enqueue-Entscheidung des Decorators
  danach je Bewegung treffen, oder die Schleife in `BeginTransactionAsync` einschließen. Test: dritte Zeile mit erzwungenem Fehler → 0 Bewegungen.

## WE-3 · niedrig · Meldungen erscheinen in „Sage-Wareneingänge“ doppelt

- **Fundstelle:** `IdealAkeWms/Views/PurchaseOrders/SageWareneingaenge.cshtml:15-22`. `_Layout.cshtml:435-447` gibt dieselben TempData ebenfalls aus.
- **Befund:** Derselbe Fehler wurde in Teil 1 für `Index`/`Receive` behoben (QA Q-2, Kommentar `Receive.cshtml:13`). Die in Teil 2 neu
  hinzugekommene Liste hat ihn wieder.
- **Szenario:** „Erneut senden“ oder „Als übergeben markieren“ → die Erfolgs- bzw. Warnmeldung steht zweimal oben.
- **Vorschlag:** Den Block `:15-22` entfernen.

## WE-4 · niedrig · Barcode-Duplikatprüfung ohne Sperre

- **Fundstelle:** `PurchaseOrdersController.Receive.cs:239` (`BarcodeVergebenAsync`), `PurchaseOrderReceiptRepository.cs:134-139`, Index nicht
  eindeutig (`SQL/146_AddWareneingangBarcodeCsv.sql:38`, `ApplicationDbContext.cs:1463`).
- **Befund:** Erst prüfen, dann einfügen, ohne Sperre. Zwei gleichzeitige Buchungen mit demselben Aufkleber werden beide gebucht. Ursache ist meist
  WE-1. Ein UNIQUE-Index ist nicht möglich, weil alle Positionen eines Vorgangs denselben Barcode tragen.
- **Szenario:** Doppelklick mit Barcode → zwei Vorgänge mit gleichem Barcode. Die zweite CSV endet als „Datei existiert bereits“. Das wird
  gemeldet, aber erst nach der Sage-Übergabe.
- **Vorschlag:** Mit WE-1 erledigt (Token). Sonst Barcode-Eindeutigkeit über `WareneingangCsvExporte.Barcode` (UNIQUE) beim Buchen absichern.

## WE-5 · Hinweis · Link „Lagerbestellung Nr. …“ führt evtl. auf „Zugriff verweigert“

- **Fundstelle:** `Receive.cshtml:152` → `WarehousePickingController` mit `[RequireLagerProcessingAccess]` (`WarehousePickingController.cs:10`).
  Die Maske selbst verlangt `RequireStockAccess` (`Receive.cs:190`).
- **Befund:** Der Hinweis „Offener Bedarf“ verlinkt in einen Bereich mit anderer Rolle. Prüfen, ob jede Rolle mit Wareneingang-Recht auch
  `RequireLagerProcessingAccess` hat (Zugriffsmatrix [[controller]]). Der Link auf Bedarfsmeldungen (`PartRequisitions`,
  `RequirePickingOrStockAccess`) passt.
- **Vorschlag:** Link nur zeigen, wenn der Nutzer das Recht hat, sonst nur die Nummer als Text.

## WE-6 · Hinweis · „Sage-Wareneingänge“ lädt ohne Zeitraum alle Wareneingänge in den Speicher

- **Fundstelle:** `PurchaseOrdersController.SageWe.cs:61-87`, `PurchaseOrderReceiptRepository.cs:84-95`.
- **Befund:** Ohne Von/Bis werden alle Receipts außer `NichtNoetig` mit Artikel und Platz geladen. Spaltenfilter und Paging laufen danach in C#.
  Das ist für Spaltenfilter auf gerenderten Text so gewollt (ADR 0005). Die Menge wächst aber mit jedem Wareneingang unbegrenzt.
- **Vorschlag:** Standard-Zeitraum, z. B. die letzten 90 Tage, oder Status „nicht übergeben“ als Default-Filter, mit Hinweistext.

## Bereits bekannt (nicht neu gezählt)

- Q-6 Teil 2: `NurInSage` wird nach der Sage-Buchung weiter abgezogen (`ErfasstJeBelPos.Abzuziehen`, `PurchaseOrderReceiptRepository.cs:22-23`).
  Bewusst offen, solange es keine Chargenartikel gibt.
- Restfenster zwischen Statusprüfung und Speichern im E5-Lauf (`SageWareneingangService.cs:105-113`, `ponytail`-Kommentar, nur mit rowversion schließbar).

## Geprüft ohne Befund

- **Sage-Schreibweg:** Der einzige POST-Client ist `SageWareneingangClient.SendAsync` (`IDEALAKEWMSService/Services/SageWareneingangClient.cs:47-67`),
  aufgerufen nur in `SageWareneingangService.RunOnceAsync:149`. Dieser läuft nur aus `SageBookingWorker.cs:73-75` hinter
  `SageWareneingangBelegAktiv` (Default aus, nicht die Safe-Variante, Lesefehler = Takt-Fehler statt Q-2). Bei ausgeschaltetem Schalter
  `AusgeschaltetAsync` → „Klären“, nie nachsenden. Web: `SageWeErneutSenden` ist bei ausgeschaltetem Schalter gesperrt (`SageWe.cs:106`).
  Kein Kennwort im Log.
- **Idempotenz Senden:** Der Status steht vor dem POST auf `Gesendet` (`:145-146`). Ein hängender Eintrag wird zu „Fehler/unklar“, die Suche läuft
  vor jedem erneuten Senden (Memo, beim automatischen Wiederholen zusätzlich das Sicherungsnetz). Scheitert die Suche, wird nicht gesendet.
  Mehrfachtreffer zählen die Versuche hoch. Die Wiederholgrenze stimmt: drei Wiederholungen nach dem Erstversand.
  Antwort ohne Belegnummer = Fehler (`Ergebnis`). Kennung gespeichert, System-Kürzel berücksichtigt.
- SData-Lookups nur mit where und orderBy (`SdataWareneingangLookupReader.cs:44-65`), Datum als `@yyyy-MM-dd@`, Lieferschein escaped.
- Ausschluss aus der Lagerbuchungs-Queue in Decorator **und** `EnqueueMissingAsync` (`SageBookingQueueRepository.cs:154`).
- Audit-Felder bei allen Statuswechseln (Dienst `system:sage-wareneingang`/`system:wareneingang-csv`, Web Benutzer). Lokalzeit (kein `UtcNow`).
- Zugriffsschutz: Klasse `RequireStockReadAccess` + Gate `WareneingangBestellungAktiv` (404), Buchen `RequireStockAccess`,
  Handgriffe `RequireStockKeyUserAccess` + `[ValidateAntiForgeryToken]`. Knopf-Sichtbarkeit (`CanTransferStockAsync`) = Filter.
- Listen-Muster: Server-Spaltenfilter, alle `<th>` mit `data-col-key`, Pagination (Index, Sage-Wareneingänge). Die Maske ist bewusst ohne
  Spaltenfilter (eine Bestellung).
- Rest-Regel an einer Stelle (`ErfasstJeBelPos.Rest/Abzuziehen`), EBZ zieht alles ab, `SyncedAt` wird vor dem Lesen genommen.
- CSV: atomar `.tmp` → `.csv` ohne Überschreiben, BOM, Quoting, Wiederholgrenze, Kollision/ungültig nie automatisch.
- Migrationen 112/113/143/146/149: `OBJECT_ID`/`COL_LENGTH`-Guards, History separat, FreshInstall an beiden Stellen, AgentJobs nicht betroffen.
  ServiceSettings im Katalog (`ServiceSettingDefinitions`). Tests vorhanden (`PurchaseOrdersReceiveTests`, `SageWareneingangServiceTests`,
  `WareneingangCsvServiceTests`). Es fehlt ein Test für WE-1/WE-2.
- Tippfehler in UI-Texten: keine gefunden.
