---
type: spec
title: "Wareneingang Teil 2: Sage-Wareneingangsbeleg (ELL) und Rahmenabrufe (EBZ)"
slug: 2026-11-08-wareneingang-teil-2-sage-we-beleg-rahmenabrufe-spec
status: Entwurf
created: 2026-11-08
updated: 2026-11-08
source_backlog: "[[2026-11-08-wareneingang-teil-2-sage-we-beleg-rahmenabrufe]]"
depends_on: "[[2026-11-08-wareneingang-aus-sage-bestellungen-spec]]"
version: "nach 1.94.0 (Dev-Lauf legt fest)"
sql_nummern: "eine Migration in E2: Vorschlag 142 (141 ist an C2 v1.89.0 vergeben; Alternative: reservierte 114), Rückfrage 1"
testszenario: "TS-116 (Erweiterung 116.10 ff., ersetzt den Platzhalter 116.7)"
task: ""
worktree: ""
branch: ""
standort: AKE
affected_code:
  - "IDEALAKEWMSService/Services/Sdata/EinkaufBestellungenSyncService.cs:32-37 (Where/Select/Belegart nur EBB), :104 (SyncedAt-Zeitpunkt nach dem Lesen), :112 (EBB-Filter), :137-147 (Mengen)"
  - "IdealAkeWms/Models/SagePurchaseOrderLine.cs:4-9,28-33 (Kommentar/Bedeutung Mengen je Belegart)"
  - "IdealAkeWms/Controllers/PurchaseOrdersController.cs:38-62 (ColumnMap, Hinweis), :114-128 (ZeilenAsync, erfasst)"
  - "IdealAkeWms/Controllers/PurchaseOrdersController.Receive.cs:21 (Meldungstext 'nur offene normale Bestellungen'), :80-107 (Buchen, Receipt anlegen)"
  - "IdealAkeWms/Models/ViewModels/PurchaseOrderReceiveViewModel.cs:20 (Rest-Regel), :22-24 (NichtBuchbar)"
  - "IdealAkeWms/Data/Repositories/PurchaseOrderReceiptRepository.cs:10,19-25 (GetErfasstJeBelPosAsync)"
  - "IdealAkeWms/Models/PurchaseOrderReceipt.cs (neue Sage-WE-Felder)"
  - "IdealAkeWms/Data/ApplicationDbContext.cs:1413-1430 (Konfiguration PurchaseOrderReceipts)"
  - "IdealAkeWms/Services/SageBookingEnqueueDecision.cs:64-72 (neuer Ausschluss-Parameter)"
  - "IdealAkeWms/Data/Repositories/SageBookingEnqueueingStockMovementRepository.cs:65-67 (Decorator liest Ausschluss)"
  - "IdealAkeWms/Data/Repositories/SageBookingQueueRepository.cs:139-155 (EnqueueMissingAsync, EF-Filter)"
  - "IDEALAKEWMSService/Workers/SageBookingWorker.cs:58-80 (eigener, eigens geschalteter Schritt Sage-WE)"
  - "IdealAkeWms/Services/SdataUrlBuilder.cs:42-44 (BuildResourceUrl wiederverwenden)"
  - "NEU IDEALAKEWMSService/Services/SageWareneingangClient.cs + SageWareneingangPayloadBuilder.cs (StandardBelegService, Muster SageLagerbuchungClient.cs:33-75)"
  - "NEU IDEALAKEWMSService/Services/SageWareneingangService.cs (ein Sende-Lauf, ISyncLogger letzter Ctor-Parameter)"
  - "IdealAkeWms/Services/SyncLogger/SyncLogServices.cs:24-37 (neuer Name SageWareneingang + DisplayName)"
  - "IdealAkeWms/Models/ServiceSettingDefinitions.cs:66-67,170 (SageWareneingangBelegAktiv)"
  - "NEU Liste Sage-Wareneingänge: Controller-Action + View (Muster SageBookingQueueController.cs:15-16,110-112)"
  - "IdealAkeWms/Views/PurchaseOrders/{Index,Receive}.cshtml (Kennzeichnung Rahmenabruf, Sage-WE-Status)"
  - "SQL/141_AddPurchaseOrderReceiptSageWe.sql (oder 114), SQL/00_FreshInstall.sql"
  - "docs/TESTSZENARIEN.md (TS-116.10 ff.), README.md (ServiceSettings), Views/Help/Index.cshtml + Changelog.cshtml, beide AppVersion.cs"
open_questions:
  - "1 SQL-Nummer: reservierte 114 behalten oder ab 142 zählen? (Empfehlung 142)"
  - "2 Ein Sage-Wareneingangsbeleg je Absenden der Wareneingangsmaske (eine Lieferung) oder je Position? (Empfehlung je Absenden)"
  - "3 Ziel-Lagerplatz ohne Sage-Bezug bei eingeschaltetem Sage-WE: Zeile sperren oder ohne Sage-WE buchen? (Empfehlung sperren)"
  - "4 Fehler/hängende Übergabe: kein automatisches Wiederholen, 'Erneut senden' nur von Hand nach Prüfung in Sage, Rolle wie Sage-Queue (RequireStockKeyUserAccess)? (Empfehlung ja)"
  - "5 Lieferschein-Nummer des Lieferanten (Sage-Kopffeld EKLieferschein) im Wareneingang erfassen? (Empfehlung nein in diesem Schritt)"
  - "6 Sage-WE auch für Rahmenabrufe (EBZ → ELL)? (Empfehlung nur, wenn der Schreibtest auch einen Abruf erfolgreich zeigt; sonst EBZ ohne Sage-WE)"
  - "7 Konflikt Abrufmenge: Backlog nennt 'Menge' (Bestelleinheit), Entscheidung B-2 bucht in der Basiseinheit ('MengeBasis'). Welches Feld? (Empfehlung MengeBasis, P-W8 prüft)"
  - "8 Rahmenabrufe mit Rest 0 im WMS: in der Vorschau standardmäßig ausblenden? Und ist akzeptiert, dass in Sage direkt gebuchte Abruf-Lieferungen im WMS nicht abgezogen werden? (Empfehlung ausblenden + akzeptieren)"
  - "9 Chargen-/nachweispflichtige Positionen bei eingeschaltetem Sage-WE: Status 'nur in Sage', kein Sage-WE und auch keine Lagerbuchung aus der Queue? (Empfehlung ja)"
  - "10 Schreibtest am AKE-Test: Freigabe, Ausführender, Rückbau durch wen? (blockiert E4)"
epic: true
etappen:
  - "E1 Rahmenabrufe (EBZ) lesen, anzeigen, buchen – keine Migration, kein Sage-Schreiben"
  - "E2 Sage-WE-Grundlage: Receipt-Statusfelder + Migration (SQL/142), Ausschluss aus Queue (Decision, Decorator, Nacherfassung), Rest-Regel – ohne Wirkung, solange kein Receipt einen Status hat"
  - "E3 Liste 'Wareneingänge an Sage' (Listen-View-Pattern) + 'Erneut senden'"
  - "E4 (blockiert: Schreibtest, P-W7-Gegencheck, P-W9) Sender: Setting SageWareneingangBelegAktiv, Status beim Buchen, StandardBelegService-Client, Worker-Schritt"
  - "E5 Abschluss: Version, Hilfe, Changelog, README, TS-116.10 ff."
deploy:
  web: true
  service: true
  migration: true
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
---

# Wareneingang Teil 2: Sage-Wareneingangsbeleg (ELL) und Rahmenabrufe (EBZ)

Folge-Spec zu [[2026-11-08-wareneingang-aus-sage-bestellungen-spec]] (Teil 1, v1.84.0/v1.86.0, Testbereit). Backlog
[[2026-11-08-wareneingang-teil-2-sage-we-beleg-rahmenabrufe]]. Grundlage aus Teil 1: Antwort 13 (E5 als Folge-Spec), Entscheidung E0-Blocker
B-1 (EBZ später, offen je Abruf = Abrufmenge − im WMS erfasst), B-2 (Basiseinheit), Antwort 1 (Einkauf schließt bis E5 ohne Lagerbuchung),
Antwort 9 + „Entscheidungen HR / N-1“ (DPS-MDE bei uns nicht im Einsatz → eigene Umsetzung, E5 hängt nur an P-W7 und dem Schreibtest).

## Ziel / Nutzen (das Warum)

Heute bucht „Wareneingang zu Bestellung“ den Bestand im WMS und schickt ihn bei Sage-Plätzen als Lagerbuchung-**Zugang** an Sage
(`SageBookingEnqueueDecision.cs:42`). Die Bestellposition in Sage bleibt dabei offen; der Einkauf muss sie von Hand ohne Lagerbuchung schließen
(Teil 1, Antwort 1). Das ist Doppelarbeit, und ein Fehler dabei erhöht den Sage-Bestand doppelt.
Ziel 1 (E5 aus Teil 1): Das WMS legt in Sage selbst den Wareneingangsbeleg an, als Belegübernahme Bestellung → Wareneingang mit platzgenauer
Menge. Damit ist die Position beliefert und der Bestand in Sage in **einem** Schritt gebucht, und es entsteht kein zusätzlicher Queue-Zugang.
Ziel 2: Auch Rahmenabrufe (EBZ) werden angezeigt und gebucht. Sage liefert je Abruf keine gelieferte Menge (Teil 1, B-1); offen rechnet daher das WMS.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- F1 Rahmenabrufe (EBZ): lesen, in „Offene Bestellungen“ und „Wareneingang zu Bestellung“ anzeigen und buchen, Kennzeichnung „Rahmenabruf“.
- F2 Sage-Wareneingangsbeleg (Kennzeichen **ELL**, siehe P-W7) per `StandardBelegService` hinter `SageWareneingangBelegAktiv` (Default aus).
- F3 Ausschluss solcher Wareneingänge aus der Sage-Lagerbuchungs-Queue, an **beiden** Stellen: Decorator und Nacherfassung.
- F4 Status und Fehler je Wareneingang, Liste mit „Erneut senden“.
- F5 Rest-Regel so anpassen, dass ein an Sage übergebener Wareneingang nicht zweimal abgezogen wird.

**Out-of-Scope** (Backlog „Nicht im Umfang“): Etikettendruck beim Eingang, Erfassung von Charge/Seriennummer, Rechnungsprüfung.
Ebenfalls nicht enthalten: IDEAL, weil dort kein SData-Endpunkt besteht (Teil 1, Antwort 7). Ein automatischer Sage-Lookup für hängende
Übergaben ist ebenfalls nicht enthalten (Rückfrage 4, P-W9).

## Fachliche Anforderungen

### F1 Rahmenabrufe (EBZ)
- **Datenbasis:** dieselbe SData-Ressource `EinkaufPositionenInfo` wie in Teil 1 (P-W1: 91 Felder, darunter `Menge`, `MengeBasis`, `VorPosId`,
  `Belegkennzeichen`). EBZ = „Rahmenabruf“, laut P-W4 108 offene Positionen am AKE-Test. Die Sage-View filtert auf **Vorgangsposition**
  (`VP.GGBestellt > VP.GGGeliefert AND VP.GGErfuellt = 0`, Teil 1 E0 P-W4). Ein Abruf bleibt deshalb sichtbar, solange die
  Rahmen-Vorgangsposition offen ist, auch wenn der Abruf selbst schon voll geliefert ist.
- **Mengen je Abruf:** `PositionGGBestellt`/`PositionGGGeliefert` sind bei EBZ Rahmen-Mengen und gelten für bis zu 18 Abrufe (Teil 1 B-1,
  Beispiel EK013128). Für EBZ dürfen sie **nicht** als bestellt/geliefert je Zeile gelten.
  - bestellt = Abrufmenge, offen laut Sage = Abrufmenge, geliefert laut Sage = **nicht bekannt**.
  - Anzeige „–“ mit Tooltip „Sage liefert für Rahmenabrufe keine gelieferte Menge je Abruf“, nie „0“.
  - Welches Feld die Abrufmenge liefert, entscheidet Rückfrage 7: `MengeBasis` (Empfehlung, wegen B-2 Basiseinheit) oder `Menge` (Backlog).
- **Offen je Abruf** = Abrufmenge − Σ `PurchaseOrderReceipt.Quantity` mit derselben `BelPosId` (Backlog Z. 18). Der Bezug Receipt → Abruf
  **besteht schon**: `PurchaseOrderReceipt.BelPosId` (`PurchaseOrderReceipt.cs:22`) wird beim Buchen aus der Cache-Zeile gesetzt
  (`PurchaseOrdersController.Receive.cs:101`), und `IX_PurchaseOrderReceipts_BelPosId` existiert (`ApplicationDbContext.cs:1429`). Für EBZ ist
  die Cache-`BelPosId` die Belegposition des Abrufs. Die Summe liefert `GetErfasstJeBelPosAsync` (`PurchaseOrderReceiptRepository.cs:19-25`).
  **Für den Bezug ist keine Migration nötig.**
- **Bekannte Grenze:** Wird eine Abruf-Lieferung in Sage direkt gebucht, also nicht über das WMS, zieht das WMS sie nicht ab. Der Abruf
  erscheint dann bis zum Ende des Rahmens als offen. Ob das akzeptiert ist, entscheidet Rückfrage 8.
- Abrufe mit Rest ≤ 0 in der Vorschau: Rückfrage 8. Empfehlung: standardmäßig ausblenden, Filter „auch im WMS vollständig erfasste Abrufe“.
- **Kennzeichnung:** Die Hinweis-Spalte (`PurchaseOrdersController.cs:55-62`) bekommt den Text „Rahmenabruf“ (damit über den Spaltenfilter
  filterbar). Der Kopf der Wareneingangsmaske zeigt „Rahmenabruf“ als Text-Badge, Farbe nie allein.
- Einheitensperre `EinheitAbweichend` und Artikel-Zuordnung gelten für EBZ unverändert (`EinkaufBestellungenSyncService.cs:131-132`).
- Die Meldung „nur offene normale Bestellungen“ (`PurchaseOrdersController.Receive.cs:21`) wird angepasst auf „offene Bestellungen und
  Rahmenabrufe“.

### F2 Sage-Wareneingangsbeleg (ELL)
- Hinter dem ServiceSetting `SageWareneingangBelegAktiv` (Default aus). Steht es aus, verhält sich alles wie in Teil 1.
- **Entscheidung beim Buchen, nicht beim Senden:** Ob ein Wareneingang als Sage-WE geht, wird beim Absenden der Maske festgelegt (Status am
  Receipt). Ein späteres Umschalten ändert bereits gebuchte Wareneingänge nicht, sie gehen also nie doppelt (Queue **und** WE) an Sage.
- Bei eingeschaltetem Setting setzt das Buchen je Zeile den Status:
  - `Offen`: Ziel-Platz mit Sage-Bezug (`SageLagerkennung` + `SageLagerplatzId`, Prüfung wie `SageLagerbuchungPayloadBuilder.cs:79-87`) und
    keine Chargen-/Nachweispflicht.
  - `NurInSage`: `Chargenpflicht` oder `Nachweispflicht` (Teil 1, Antwort 10; Rückfrage 9). Die Zeile wird nicht gesendet. Hinweis an der Zeile:
    „Charge/Seriennummer: Wareneingang in Sage buchen“.
  - Ziel-Platz ohne Sage-Bezug: Rückfrage 3. Empfehlung: Die Zeile ist nicht buchbar, Text „Ziel-Platz ohne Sage-Bezug – Sage-Wareneingang
    nicht möglich“.
- **Beleg:** `POST …/CommonWawiServices/{Dataset}/$service/StandardBelegService` ([[sage-sdata-commonwawiservices]] Z. 70-73).
  - Kopf: `Erfassungsart = 4100`, `IsBelegTransform = true`, `BelegTransformArt = 1` (Belegübernahme), `BelegKennzeichen = "ELL"`
    (Konstante, kein Setting; Teil 1 „Einstellungen“ letzter Absatz), `TransformSettings[].SourceBelegHandle` = `BelId` der Bestellung.
  - Positionen mit `BelegPositionLagermengen{PlatzId, Kurzbezeichnung, Menge/MengeBasis}`.
  - Menge in der Basiseinheit (B-2).
  - Welche Handles (`PositionHandle`, `VorgangPositionHandle`) bei einer Übernahme zu belegen sind und was `Kurzbezeichnung` erwartet
    (Lagerkennung oder Platz-Kurzbezeichnung), ist **unbelegt**. Das klärt der Schreibtest. Bis dahin legt der Dev-Lauf kein Payload fest.
- Bündelung: Rückfrage 2. Empfehlung: ein Beleg je Absenden der Maske (= eine Lieferung). Gruppenschlüssel ist die neue Spalte
  `SageWeVorgang` (Guid). Die Zeitgleichheit von `now` (`PurchaseOrdersController.Receive.cs:75,93`) wird bewusst nicht genutzt.
- Korrelation: `Memo` des Belegkopfs = `WE#<ersteReceiptId>#` (Trennzeichen-Regel wie `SM#<id>#`, [[fallstricke]] Z. 860-861). Ob Sage das
  Memo am ELL-Beleg speichert, ist unbelegt (P-W9).
- **Kein blindes Wiederholen:** Vor dem POST wird der Status auf `Gesendet` gesetzt (Muster AK10, `SageBookingWorker.cs:197-198`). Danach gilt:
  - Erfolg: Status `Uebergeben`, dazu `SageWeBelegnummer` (`BelegnummerFormatiert` aus der Response, [[sage-sdata-commonwawiservices]] Z. 30)
    und `SageWeAm`.
  - Fehler: Status `Fehler`, `SageWeFehler` (Response-Text, gekappt).
  - Hängt eine Übergabe länger als `Sync:SageLagerbuchungStuckMinutes` auf `Gesendet`, wird sie zu `Fehler` mit dem Text „Ergebnis unklar –
    bitte in Sage prüfen, ob Beleg WE#… existiert“. Sie wird **nicht** automatisch neu gesendet (Rückfrage 4).
- Nach erfolgreicher Übergabe steigt in Sage `GGGeliefert` der Bestellung. Der nächste Abgleich verkleinert dann „offen laut Sage“. Mit E5
  entfällt die Arbeitsanweisung aus Teil 1, Antwort 1 („Einkauf schließt ohne Lagerbuchung“) für Wareneingänge mit Status `Uebergeben`. Das
  wird in Hilfe und Changelog beschrieben.

### F3 Ausschluss aus der Lagerbuchungs-Queue (eine Regel, zwei Stellen)
- Regel: Eine Bewegung, deren `PurchaseOrderReceipt` einen Sage-WE-Status ≠ `NichtNoetig` hat, geht **nie** in die Queue.
- `SageBookingEnqueueDecision.ShouldEnqueue` bekommt den Parameter `hatSageWareneingang` (analog `hatFaBezug`, `SageBookingEnqueueDecision.cs:62-68`).
- Decorator: `saved.PurchaseOrderReceipt?.SageWeStatus` liegt im Speicher, weil der Receipt als Navigation mitgespeichert wird
  (`PurchaseOrdersController.Receive.cs:99-106`, `SageBookingEnqueueingStockMovementRepository.cs:65-67`).
- Nacherfassung: Der EF-Filter in `EnqueueMissingAsync` (`SageBookingQueueRepository.cs:144-152`) verwendet die Decision **nicht**. Er bildet
  die FA-Regel selbst nach (Z. 151). Deshalb kommt die neue Regel dort als eigene Bedingung dazu:
  `&& !(m.PurchaseOrderReceipt != null && m.PurchaseOrderReceipt.SageWeStatus != NichtNoetig)`. Sonst holt der 15-Minuten-Sweep die Buchung als
  Zugang nach ([[fallstricke]] Z. 1719).
- Mit Status `NichtNoetig` (Setting beim Buchen aus) bleibt das Verhalten wie in Teil 1 (Queue-Zugang bei Sage-Platz, AK 14 Teil 1).

### F4 Status und Liste „Wareneingänge an Sage“
- Status je Receipt (Enum `SageWareneingangStatus`, als int):
  - `NichtNoetig = 0` (Default, Altbestand)
  - `Offen = 1`
  - `Gesendet = 2`
  - `Uebergeben = 3`
  - `Fehler = 4`
  - `NurInSage = 5`
- Das vorhandene `SageUebergabeStatus` (`Creditor.cs:139-145`) reicht nicht, weil `Gesendet` und `NurInSage` fehlen. Es wird nicht
  erweitert, weil es die Stammdaten-Übergabe beschreibt.
- Liste (eine Tabellen-View, Listen-View-Pattern Pflicht):
  - Spalten: gebucht am, Bestellung, Pos., Artikel, Menge, Ziel-Platz, Status (Text), Sage-Beleg, Fehler.
  - Filterkarte: Status, Bestellung, Zeitraum.
  - Server-Spaltenfilter, Pagination über `PageSize.Resolve`.
  - Lesen mit `RequireStockReadAccess`, „Erneut senden“ (nur bei `Fehler`) mit `RequireStockKeyUserAccess`, wie
    `SageBookingQueueController.cs:13-16,110-112` (Rückfrage 4).
  - „Erneut senden“ setzt die ganze `SageWeVorgang`-Gruppe auf `Offen` und schreibt Audit-Felder. Vor dem Klick muss der Benutzer in Sage
    nachsehen, ob der Beleg schon existiert. Die Maske weist darauf hin.
- In der Maske „Wareneingang zu Bestellung“ steht je Position eine Zeile „Sage-Wareneingang: <Status>“ für ihre Receipts. Die Spalte
  „im WMS erfasst“ bleibt die Gesamtsumme.

### F5 Rest-Regel (Teil 1, Antwort 2, präzisiert)
- **EBB:** Rest = offen laut Sage − Σ Receipts, die zum Zeitpunkt des Abgleichs noch **nicht** in Sage enthalten waren. Das sind alle außer
  `Uebergeben` mit `SageWeAm` ≤ `SyncedAt` der Cache-Zeile. Ohne diese Ausnahme zieht das WMS einen übergebenen Wareneingang ein zweites Mal ab.
  Teil 1 nannte dieses Risiko bei Antwort 2 („würde doppelt abziehen – im UAT prüfen“).
- **EBZ:** Rest = Abrufmenge − Σ **alle** Receipts, weil Sage offen je Abruf nicht verkleinert (F1).
- `SyncedAt` wird künftig **vor** dem SData-Lesen genommen (heute `EinkaufBestellungenSyncService.cs:104`, nach dem Lesen). Sonst gilt eine
  Übergabe, die während des Lesens fertig wird, fälschlich als im Sage-Stand enthalten.
- Die Regel liegt an **einer** Stelle (`PurchaseOrderReceiveRow.Rest`, `PurchaseOrderReceiveViewModel.cs:20`). Das Repository liefert dafür je
  `BelPosId` die Gesamtsumme und die Summe „nicht in Sage enthalten bis Stand X“, in einer Abfrage ohne N+1.

## Technischer Loesungsentwurf

- **E1 Lesen EBZ:** zwei `QueryAllAsync`-Aufrufe mit `where Belegkennzeichen eq 'EBB'` bzw. `eq 'EBZ'`. Das ist das belegte `eq`-Muster. `or` im
  `where` ist am Server nicht belegt, `ne` liefert auf Textfeldern still 0 Zeilen ([[fallstricke]] Z. 1720-1724).
  - Der Empty-Guard gilt für die Summe beider Abfragen (`EinkaufBestellungenSyncService.cs:80-85`).
  - Neue Counts-Keys `positionen_ebb` und `positionen_ebz`.
  - `Select` bekommt das Abrufmengen-Feld (Rückfrage 7). Nur in P-W1 belegte Namen.
  - Belegart-Prüfung `:112` wird zur Liste `{EBB, EBZ}`.
  - Für EBZ: `MengeBestellt = MengeOffen = Abrufmenge`, `MengeGeliefert = 0`. Die View zeigt bei EBZ „–“, und der Modell-Kommentar bekommt die
    Bedeutung je Belegart.
  - **Keine neue Cache-Spalte:** `Belegkennzeichen` gibt es schon (`SagePurchaseOrderLine.cs:16`). Die Abgrenzung EBB/EBZ läuft darüber. Kein
    neuer Abgleich, kein neuer Dienst.
- **E2 Grundlage:**
  - Neue Felder an `PurchaseOrderReceipt`: `SageWeStatus` (int, NOT NULL, DEFAULT 0 **nur im SQL/Migration**, nicht `HasDefaultValue`,
    [[fallstricke]] Z. 1717), `SageWeVorgang` (uniqueidentifier NULL, Index), `SageWeAm` (datetime2 NULL), `SageWeBelegnummer` (nvarchar(30) NULL),
    `SageWeFehler` (nvarchar(1000) NULL).
  - Ausschluss F3 an Decision, Decorator und `EnqueueMissingAsync`. Rest-Regel F5.
  - Solange kein Code einen Status ≠ 0 setzt, ändert sich nichts. Die Etappe ist deshalb ohne Wirkung mergebar.
- **E3 Liste:** neue Action in `PurchaseOrdersController` (Feature-Gate `RequireWareneingangBestellungAktiv` kumulativ) oder ein eigener kleiner
  Controller. Der Dev-Lauf wählt den kürzeren Weg. Referenz für Pattern und Rechte: `SageBookingQueueController`.
- **E4 Sender:**
  - `SageWareneingangPayloadBuilder` (rein, testbar) und `SageWareneingangClient`. Der Client ist eine Kopie von `SageLagerbuchungClient.cs:33-75`
    mit URL über `SdataUrlBuilder.BuildResourceUrl(…, "$service/StandardBelegService")` (`SdataUrlBuilder.cs:42-44`). Endpunkt und Zugang wie
    `SageBookingWorker.cs:99-107` (dieselben `SageLagerbuchung:Username/Password`).
  - `SageWareneingangService.RunOnceAsync`: gruppiert `Offen` nach `SageWeVorgang`, sendet und bewertet. Eigener Lauf im Aktivitäts-Protokoll
    (`SyncLogServices.SageWareneingang`, DisplayName „Sage-Wareneingang“). Counts: `gesendet`, `uebergeben`, `fehler`, `unklar`.
  - Aufruf als **eigener** Schritt im `SageBookingWorker`-Takt, gegated nur durch `SageWareneingangBelegAktiv`, getrennt vom
    `SageLagerbuchungAktiv`-Block (`SageBookingWorker.cs:66-67`). So bleibt EINE Instanz zuständig (Idempotenz-Voraussetzung, [[fallstricke]]
    Z. 855-858), und es gibt keinen zweiten BackgroundService.
  - DI-Auflösungstest wie bei den übrigen Diensten.
- **Verworfene Alternative:** Die WE-Belege über `SageBookingQueueItems` mit einer Spalte „Art“ zu führen hätte Retry, Requeue-UI und
  Recovery mitgenutzt. Dagegen spricht: Die Queue ist 1:1 je `StockMovement` (UNIQUE, SQL/82-83), ein Beleg umfasst mehrere Bewegungen, und
  der Sender hinge am Schalter `SageLagerbuchungAktiv`. Der Backlog verlangt außerdem Status je Receipt (Z. 13).
- Oberfläche: Skill `frontend-design` explizit aufrufen; Bootstrap 5, WCAG AA, Status immer als Text. Für Fehlerzeilen `text-danger-emphasis`
  (Teil 1 Q-1/Q-8).

## Migrations-/SQL-Auswirkungen

| Etappe | Migration | Skript | Inhalt | daten-destruktiv |
|---|---|---|---|---|
| E1 | – | – | keine (Bezug Receipt → Abruf über `BelPosId` vorhanden, `Belegkennzeichen` im Cache vorhanden) | – |
| E2 | `AddPurchaseOrderReceiptSageWe` | `SQL/141_AddPurchaseOrderReceiptSageWe.sql` (oder `114`, Rückfrage 1) | 5 Spalten an `PurchaseOrderReceipts` (s. o.), `SageWeStatus` NOT NULL DEFAULT 0, Index `SageWeVorgang`, Index `SageWeStatus` | nein (additiv; Altbestand = 0 = Verhalten Teil 1) |
| E3–E5 | – | – | – | – |

- ADR 0004: Model → `dotnet ef migrations add` → idempotentes SQL mit `COL_LENGTH`/`sys.indexes`-Guard (DDL eigener Batch, Muster
  `SQL/140_AddArtikelMengeneinheiten.sql:9-15`) → `__EFMigrationsHistory`-Insert eigener Batch → `SQL/00_FreshInstall.sql` an **beiden**
  Stellen (Spalten in der Tabellendefinition `PurchaseOrderReceipts` **und** `MigrationId`).
- Nummer: Im Worktree vorhanden sind 110, 112, 113, 133, 140. Frei sind 111, 114 (Teil 1 für E5 reserviert, nicht genutzt) und 134-139.
  **Vorschlag 142.** Begründung: Ein nach 140 ausgeliefertes Skript mit der Nummer 114 übersieht man beim Deploy leicht („bis 140 schon
  gelaufen“). Gerald entscheidet (Rückfrage 1); der Dev-Lauf prüft die Nummer vor dem Anlegen erneut.
- `SQL/AgentJobs/*`: nicht betroffen (keine Pflichtspalte an einer AgentJob-Tabelle, kein Folge-MERGE).
- Das neue ServiceSetting legt der Katalog-Seed an, kein eigenes SQL.
- Die Web-App nicht lokal aus dem Worktree starten, solange die Migration pending ist ([[fallstricke]] Z. 865-874).

## Einstellungen

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| `SageWareneingangBelegAktiv` (ServiceSetting, Gruppe „Sage-Lagerbuchung“), Anzeigename **„Wareneingänge als Sage-Wareneingangsbeleg buchen“** | Bool (Schalter) | true / false | false | An: Wareneingänge zu Bestellungen und, nach Rückfrage 6, Rahmenabrufen werden beim Buchen für Sage vorgemerkt und vom Dienst als Wareneingangsbeleg (ELL, Belegübernahme aus der Bestellung, platzgenau) in Sage angelegt. Sie gehen **nicht** zusätzlich als Lagerbuchung-Zugang an Sage. Aus: Verhalten wie Teil 1 (Lagerbuchung-Queue; der Einkauf schließt die Position in Sage). Umschalten wirkt nur auf neue Buchungen. | Maske „Wareneingang zu Bestellung“ (Status beim Buchen); Dienst: Sage-Buchungs-Worker, Lauf „Sage-Wareneingang“; Liste „Wareneingänge an Sage“ | AKE (IDEAL aus, kein SData-Endpunkt) | „Schreibt Wareneingangsbelege nach Sage: Bestellung wird beliefert, Bestand in Sage gebucht. Nur nach erfolgreichem Schreibtest am AKE-Test einschalten. Der Einkauf darf diese Positionen danach nicht mehr von Hand schließen.“ |

- Eintrag in `ServiceSettingDefinitions` an **beiden** Stellen: Beschreibung `:170` und Anzeigename/Verwendung/Standort/Warnhinweis `:66-67`.
  Der Drift-Guard-Test `ServiceSettingDefinitionsTests` bekommt den Key.
- Der Key kommt **erst in E4** in den Katalog. Bis dahin gibt es keinen Schalter, der Belege zurückhält, ohne sie zu senden.
- Für das Kennzeichen ELL gibt es keine Einstellung (Konstante, von Sage vorgegeben). Sonst ändert sich kein Setting.

## Audit-Feld-Auswirkungen

- `PurchaseOrderReceipt` ist `AuditableEntity`. Der Status beim Buchen wird im selben Insert gesetzt (Created* wie heute,
  `PurchaseOrdersController.Receive.cs:103`). Jeder spätere Statuswechsel durch den Dienst setzt `ModifiedAt`, `ModifiedBy` und
  `ModifiedByWindows` = `system:sage-wareneingang` (Muster `SageBookingQueueRepository.cs:12,186-191`).
- „Erneut senden“ setzt Modified* aus `ICurrentUserService` (Muster `SageBookingQueueController.cs:112`).
- `SagePurchaseOrderLine` (Cache, kein Audit) und `StockMovement` bleiben unverändert.

## Rollen und Zugriff

- Keine neue Rolle, kein neuer Filter.
  - Vorschau und Liste: `RequireStockReadAccess`.
  - Buchen: `RequireStockAccess` (unverändert).
  - „Erneut senden“: `RequireStockKeyUserAccess` (admin, stock_keyuser, picking; [[controller]] Z. 65), Rückfrage 4.
- Feature-Gate `RequireWareneingangBestellungAktiv` kumulativ wie in Teil 1.

## P-W7 Ergebnis (Aufrufer, 2026-11-08, nur lesend am AKE-Test)

Weg: Sage-SQL statt SData. Server `AKESQL20.ake.at`, DB `ake_TEST2026`, nur `SELECT`, `READ UNCOMMITTED`.
`SELECT Belegkennzeichen, MIN(Belegart), COUNT(*) FROM dbo.KHKEKBelege GROUP BY Belegkennzeichen`

| Kennzeichen | Belegart | Anzahl |
|---|---|---|
| EBB | Bestellung | 64002 |
| EBD | RMA-Anmeldung | 293 |
| EBM | Liefererinnerung | 2 |
| EBZ | Rahmenabruf | 3455 |
| ELA | RMA-Retour | 3 |
| ELL | Wareneingang | 98223 |
| ELR | Retour | 428 |
| EPA | Preisanfrage | 1356 |
| ERK | Rahmenvertrag EK | 522 |

- `LIKE 'EW%'` liefert 0 Zeilen.
- **Befund:** Das Kennzeichen des Sage-Wareneingangs ist **ELL**, nicht das im Backlog vermutete `EWE` (Teil 1 hatte EWE = 0 gemessen).
- **Offen als Prüfschritt (Mensch):** Gegencheck über SData. Zuerst `EinkaufBelegeInfo/$schema` (Feldname `BelID` oder `BelId` prüfen; in
  `EinkaufPositionenInfo` heißt er `BelId`), dann
  `EinkaufBelegeInfo?format=json&orderBy=<BelId-Feld>&count=5&select=<BelId-Feld>,Belegkennzeichen&where=Belegkennzeichen eq 'ELL'`.
  Info-Ressourcen liefern eventuell nur offene Belege; 0 Treffer widerlegen ELL daher nicht. Ergebnis hier eintragen.

## Prüfschritte (Mensch) – abhängige Aussagen gelten bis dahin als unbelegt

- **P-W8 Abrufmenge (Rückfrage 7, blockiert E1 nicht):** SData, Feldnamen laut P-W1 belegt:
  `EinkaufPositionenInfo?format=json&orderBy=BelPosId&count=200&where=Belegkennzeichen eq 'EBZ'&select=BelPosId,VorPosId,Menge,MengeBasis,Mengeneinheit,MengeBasiseinheit,Lagermengeneinheit`
  → Zählen, wie viele Zeilen `Menge ≠ MengeBasis` haben und bei wie vielen davon die Einheiten gleich sind. Erwartung: gleiche Einheiten,
  gleiche Menge. Einen Abruf in Sage öffnen und bestätigen, dass `MengeBasis` die Abrufmenge in der Basiseinheit ist.
- **P-W9 Korrelation am ELL-Beleg (nach dem Schreibtest, blockiert E4):** Sage-SQL `ake_TEST2026`, nur `SELECT`, zuerst
  `SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'KHKEKBelege' ORDER BY ORDINAL_POSITION;`
  dann, mit den gefundenen Namen für Memo und Belegnummer,
  `SELECT TOP 5 BelID, Belegkennzeichen, <Belegnummer>, <Memo> FROM dbo.KHKEKBelege WITH (NOLOCK) WHERE Belegkennzeichen = 'ELL' AND <Memo> LIKE 'WE#%' ORDER BY BelID DESC;`
  → Steht das gesendete Memo am Beleg? Wenn ja, ist ein späterer automatischer Lookup möglich (eigene Folge-Aufgabe, nicht diese Spec).
- **Schreibtest am AKE-Test (blockiert E4) – NICHT ausgeführt, braucht Geralds ausdrückliche Freigabe (Rückfrage 10):**
  1. **Vorbereitung:** In `ake_TEST2026` eine Test-Bestellung EBB mit zwei Positionen anlegen bzw. wählen. Bedingungen: Artikel ohne
     Chargen-/Nachweispflicht, gleiche Bestell-, Basis- und Lagereinheit, Ziel-Platz mit Sage-Bezug (WMS `SageLagerkennung`/`SageLagerplatzId`).
     Aus `EinkaufPositionenInfo` (`select=BelId,BelPosId,VorPosId,PositionGGBestellt,PositionGGGeliefert,PlatzID,Kurzbezeichnung`) und in der
     Sage-Oberfläche festhalten: `BelId`, `BelPosId`, `VorPosId`, Mengen, Platz-Bestand vorher.
  2. **Senden:** einmal `POST …/CommonWawiServices/ake_TEST2026;1/$service/StandardBelegService` mit `Erfassungsart 4100`,
     `IsBelegTransform true`, `BelegTransformArt 1`, `BelegKennzeichen "ELL"`, `TransformSettings[{SourceBelegHandle: <BelId>}]`, Pflicht-Kopffeldern
     laut Schema (`Belegdatum`, `A0Konto`, …) und `Memo "WE#TEST1#"`. Dazu **nur Position 1** mit Teilmenge und
     `BelegPositionLagermengen[{PlatzId, Kurzbezeichnung, MengeBasis}]`. Ausprobieren, welche Handles (`PositionHandle`/`VorgangPositionHandle` =
     `BelPosId`/`VorPosId`?) Sage verlangt.
  3. **Ergebnis prüfen:** Die Response trägt `BelegnummerFormatiert`, `ResultNumber`/`ErrorText` und je Position `ResultText`.
     - In Sage ist ein ELL-Beleg entstanden.
     - Position 1: `GGGeliefert` ist um die Teilmenge gestiegen.
     - Position 2 ist unverändert offen und **nicht** in den ELL-Beleg übernommen.
     - Der Platz-Bestand ist um die Teilmenge gestiegen, und zwar auf dem gesendeten Platz.
     - `EinkaufPositionenInfo` zeigt die verkleinerte offene Menge. P-W9 ausführen.
     - Optional: eine Überlieferung probieren (Menge > offen). Wird sie angenommen oder abgelehnt?
     - Optional für Rückfrage 6: ein EBZ-Abruf.
  4. **Rückbau dokumentieren:** den ELL-Beleg in Sage löschen oder stornieren (wer und wie: Rückfrage 10). Dann prüfen, dass `GGGeliefert`
     und der Platz-Bestand wieder auf dem Stand vor dem Test sind. Ist die Test-Bestellung neu angelegt, auch sie löschen.
  5. **Eintragen:** Request (ohne Zugangsdaten), Response, Befunde und Rückbau kommen in einen Abschnitt „Schreibtest-Ergebnis“ dieser Spec.
     Erst danach legt E4 das Payload fest.

## Akzeptanzkriterien

1. (E1) Mit `Sync:EinkaufBestellungenEnabled = true` übernimmt ein Lauf EBB- und EBZ-Positionen. Das Protokoll zeigt `positionen_ebb` und
   `positionen_ebz`; andere Kennzeichen werden nicht übernommen.
2. (E1) Eine EBZ-Zeile zeigt in der Vorschau „Rahmenabruf“ in der Hinweis-Spalte, „–“ bei „geliefert (Sage)“ (nicht „0“) und als offen die
   Abrufmenge laut Rückfrage 7.
3. (E1) Nach dem Buchen von 3 auf einen Abruf mit Abrufmenge 10 zeigt die Maske Rest 7. Nach einem weiteren Abgleich (Sage-Werte
   unverändert) zeigt sie weiter Rest 7.
4. (E1) Ein Abruf mit Rest 0 verhält sich in der Vorschau nach Antwort 8 (Empfehlung: ohne Filter unsichtbar, mit Filter sichtbar).
5. (E1) Scan oder Eingabe der Belegnummer eines Rahmenabrufs öffnet ihn. Kein Treffer zeigt die angepasste Meldung.
6. (E2) Mit Receipts im Status `NichtNoetig` sind Queue-Einträge, Rest und Anzeige identisch zu Teil 1 (bestehende Tests grün).
7. (E2) Eine Einbuchung mit Receipt-Status ≠ `NichtNoetig` erzeugt **keinen** Queue-Eintrag, weder im Decorator noch über
   `EnqueueMissingAsync` (je ein Test). Bei `NichtNoetig` erzeugt sie einen Eintrag (Sage-Platz, Schalter an).
8. (E2) Rest-Regel EBB: Ein Receipt `Uebergeben` mit `SageWeAm` ≤ `SyncedAt` wird nicht abgezogen. Mit `SageWeAm` > `SyncedAt` oder
   einem anderen Status wird er abgezogen. EBZ zieht immer ab. Unit-Test mit allen Fällen.
9. (E2) `SQL/142` (bzw. 114) läuft zweimal ohne Fehler (zweiter Lauf ohne Ausgabe). Bestehende Receipts haben danach `SageWeStatus = 0`.
10. (E3) Die Liste „Wareneingänge an Sage“ hat Pagination, Filterkarte und Server-Spaltenfilter. Ein Benutzer mit `stock_read` sieht sie
    ohne „Erneut senden“; ein direkter POST wird abgewiesen.
11. (E3) „Erneut senden“ ist nur bei `Fehler` möglich. Die ganze Gruppe geht auf `Offen`, Modified* ist gesetzt.
12. (E4) Mit `SageWareneingangBelegAktiv = false` erhält ein neuer Wareneingang `NichtNoetig`, und es wird nichts an `StandardBelegService`
    gesendet.
13. (E4) Mit `true` bekommen zwei Positionen, die in einem Absenden gebucht werden, Status `Offen` und dieselbe `SageWeVorgang`. Der nächste
    Worker-Takt sendet genau einen Beleg (Rückfrage 2), danach steht `Uebergeben` mit Sage-Belegnummer da. Auch nach 15 Minuten gibt es keinen
    Queue-Eintrag.
14. (E4) Eine Chargen- bzw. nachweispflichtige Position bekommt `NurInSage`, wird nicht gesendet und hat keinen Queue-Eintrag. Die Maske
    zeigt den Hinweis.
15. (E4) Ziel-Platz ohne Sage-Bezug bei eingeschaltetem Setting: Verhalten nach Antwort 3 (Empfehlung: Zeile nicht buchbar, nichts gebucht).
16. (E4) Sage-Fehler-Response ergibt `Fehler` mit Text. Ein Timeout bzw. hängendes `Gesendet` ergibt nach `StuckMinutes` den Status `Fehler`
    „Ergebnis unklar …“ und wird nicht neu gesendet.
17. (E4) Das Aktivitäts-Protokoll zeigt den Lauf „Sage-Wareneingang“ mit deutschen Counts. Der Name steht in `SyncLogServices.All`, und
    `ISyncLogger` ist der letzte Ctor-Parameter.
18. (E4) `/ServiceSettings` zeigt den Schalter mit Anzeigename, Wirkung, Standort AKE und dem Warnhinweis.

## Test-Szenarien

Erweiterung von **TS-116** in `docs/TESTSZENARIEN.md`. Der Platzhalter TS-116.7 aus Teil 1 wird durch 116.12 ff. ersetzt; der Dev-Lauf
bestätigt die nächste freie Unternummer. Danach `secondbrain/tests/testszenarien-index.md` nachziehen.
- **TS-116.10 Rahmenabrufe lesen/anzeigen:**
  - Vorbedingung: Abgleich an, P-W8 beantwortet.
  - Ablauf: Lauf starten, Counts EBB/EBZ prüfen, einen Abruf (z. B. aus EK013128) mit Sage vergleichen. Erwartet: Abrufmenge, „–“ bei
    geliefert, Hinweis „Rahmenabruf“ (AK 1, 2).
  - Negativ: Abruf mit abweichender Einheit ist nicht buchbar.
- **TS-116.11 Rahmenabruf buchen:**
  - Teilmenge buchen, danach Rest prüfen; erneut abgleichen, Rest bleibt gleich; Rest bis 0 buchen, danach Ausblenden prüfen (AK 3-5).
  - Negativ: Überlieferung ohne Häkchen wird abgewiesen.
- **TS-116.12 Ausschluss aus der Queue (nur Testsystem):**
  - Setting an (nach E4), `SageLagerbuchungAktiv` an, Sage-Platz.
  - Wareneingang buchen: kein Eintrag in `SageBookingQueueItems`, auch nicht nach 15 Minuten (`SELECT * FROM SageBookingQueueItems WHERE StockMovementId = <id>`).
  - Gegenprobe Setting aus: Eintrag entsteht (AK 6, 7, 12).
- **TS-116.13 Sage-Wareneingangsbeleg (nur AKE-Test, nach Schreibtest):**
  - Zwei Positionen in einem Absenden buchen: ein ELL-Beleg in Sage, Status `Uebergeben` mit Belegnummer.
  - Nach dem nächsten Abgleich ist der Rest korrekt, ohne doppelten Abzug.
  - Danach zurücksetzen wie im Schreibtest (AK 8, 13).
- **TS-116.14 Sonderfälle:**
  - Chargenpflicht ergibt `NurInSage`, kein Senden, kein Queue-Eintrag.
  - Platz ohne Sage-Bezug wird nach Antwort 3 behandelt.
  - SData-Kennwort falsch ergibt `Fehler`, in der Liste sichtbar. „Erneut senden“ als stock_keyuser funktioniert, als stock_read wird es
    abgewiesen (AK 10, 11, 14-16).

## Etappen (nur bei epic: true)

Ein Worktree, ein Merge am Ende (Schranke 2 nach allen gebauten Etappen). Jede Etappe baut und testet grün und endet mit einem Commit. Den
Branch während der Arbeit mit `scripts/sync-worktree.ps1 -Slug <slug>` auf main-Stand halten. Die Version für das ganze Epic setzt E5, nach
1.94.0. E1-E3 und E5 laufen ohne Sage-Schreibzugriff. E4 ist blockiert, bis der Schreibtest dokumentiert ist; dann bleibt das Epic mit
E1-E3 + E5 mergebar, ohne Setting im Katalog und damit ohne Wirkung von E2.

| # | Etappe | Abhängigkeit | SQL | Status | Commit |
|---|--------|--------------|-----|--------|--------|
| 1 | Rahmenabrufe: zwei `eq`-Abfragen, EBZ-Mengen, Kennzeichnung, Rest EBZ, Ausblenden Rest 0, Meldungstext; Tests | Rückfragen 7, 8 (ohne Antwort: `MengeBasis` + ausblenden **nicht** bauen, Etappe als Blocker festhalten) | – | offen | |
| 2 | Sage-WE-Grundlage: Receipt-Felder + Migration, `SageWareneingangStatus`, Ausschluss in Decision/Decorator/`EnqueueMissingAsync`, Rest-Regel EBB, `SyncedAt` vor dem Lesen; Tests | E1; Rückfrage 1 (SQL-Nummer), Rückfrage 2 (Gruppenschlüssel) | 142 (oder 114) | offen | |
| 3 | Liste „Wareneingänge an Sage“ (Listen-View-Pattern) + „Erneut senden“ + Status-Zeile in Receive | E2; Rückfrage 4 | – | offen | |
| 4 | Sender: Setting in den Katalog, Status beim Buchen (Offen/NurInSage/Platzregel), Payload-Builder, Client, `SageWareneingangService`, Worker-Schritt, SyncLog-Name, DI-Test | **blockiert:** Schreibtest dokumentiert (Rückfrage 10), P-W7-Gegencheck, P-W9; Rückfragen 3, 5, 6, 9 | – | blockiert | |
| 5 | Abschluss: beide `AppVersion.cs` (nach 1.94.0), Anwender-Changelog, Hilfe (Rahmenabruf, Sage-WE, entfallende Arbeitsanweisung), README ServiceSettings, TS-116.10 ff. | E1-E3 (+E4, falls gebaut) | – | offen | |

Brain-Update (Hauptcheckout): [[feature-map]], [[datenmodell]] (Receipt-Spalten), [[services]] (Lauf „Sage-Wareneingang“, Gate),
[[integrationen]] (StandardBelegService genutzt), [[sage-sdata-commonwawiservices]] Z. 12 („Im WMS genutzt“ ergänzen, nach E4),
[[controller]] (Liste + Rechte), [[glossar]] („Rahmenabruf“, „Sage-Wareneingangsbeleg (ELL)“), [[fallstricke]]:
- EBZ-Vorgangsmengen gelten je Rahmen, nicht je Abruf.
- Eine Ausschlussregel muss in Decorator **und** `EnqueueMissingAsync` stehen.
- Rest darf übergebene Wareneingänge nicht doppelt abziehen.

Jeweils mit Warum. Changelog-Notiz und Testindex.

## Deploy

*Vorläufig (Spec-Agent). Der qa-agent bestätigt gegen den echten Diff.*

- **Web-App:** ja (Vorschau/Receive, Liste, Decision/Decorator, Repository, Settings-Katalog).
- **Service:** ja (Abgleich EBZ, `EnqueueMissingAsync` läuft im Dienst, Worker-Schritt Sage-WE).
- **Migration:** ja, E2 (`SQL/142` bzw. `114`), additiv.
- **Reihenfolge Testsystem:**
  1. DB-Backup (Standard).
  2. SQL-Skript.
  3. Web publizieren.
  4. Service publizieren.
  5. Einen Abgleich laufen lassen (EBZ im Cache).
  6. `SageWareneingangBelegAktiv` erst nach dokumentiertem Schreibtest und nur am AKE-Testsystem einschalten.
  7. Den Einkauf informieren, dass übergebene Positionen nicht mehr von Hand geschlossen werden.
- **Publish-Befehle (im Worktree):**
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`
- **Standort:** AKE. IDEAL bekommt den Code, die Schalter bleiben aus.

## Offene Rueckfragen

1. **SQL-Nummer:** Teil 1 hat `SQL/114` für E5 reserviert und nicht genutzt. Im Bündel ist 140 die letzte genutzte Nummer, frei sind 111 und
   134-139. Bleibt 114, oder wird ab 142 gezählt?
   *Empfehlung:* 142. Nummern steigen mit der Auslieferung, und ein spätes 114 wird beim Deploy leicht übersehen. 114 wird frei gegeben.
2. **Bündelung der Sage-Belege:** ein ELL-Beleg je Absenden der Wareneingangsmaske (= eine Lieferung, mehrere Positionen) oder je Position?
   *Empfehlung:* je Absenden, mit expliziter Gruppenspalte `SageWeVorgang`. Das ergibt weniger Belege im Einkauf und entspricht dem Lieferschein.
   „Je Position“ wäre einfacher, erzeugt aber viele Einzelbelege.
3. **Ziel-Platz ohne Sage-Bezug** bei eingeschaltetem Sage-WE: Zeile sperren oder ohne Sage-WE buchen (Status `NichtNoetig`, dann gilt
   wieder die Arbeitsanweisung „Einkauf schließt von Hand“)?
   *Empfehlung:* sperren mit Text. Ein gemischter Zustand innerhalb einer Lieferung ist schwer nachzuvollziehen.
4. **Fehler und hängende Übergaben:** kein automatisches Wiederholen und kein Sage-Lookup in dieser Spec. „Erneut senden“ nur von Hand, nach
   Prüfung in Sage, mit `RequireStockKeyUserAccess` wie bei der Sage-Queue?
   *Empfehlung:* ja. Ein automatischer Lookup lohnt erst, wenn P-W9 die Memo-Korrelation belegt (Folge-Aufgabe).
5. **Lieferschein-Nummer des Lieferanten** (Kopffeld `EKLieferschein` im StandardBeleg) im Wareneingang erfassen und mitsenden?
   *Empfehlung:* nein in diesem Schritt (YAGNI). Pflegt der Einkauf sie heute im Sage-Wareneingang regelmäßig, gibt es ein eigenes kleines
   Folge-Feld.
6. **Sage-WE für Rahmenabrufe (EBZ → ELL):** gleich mit aktivieren?
   *Empfehlung:* nur, wenn der Schreibtest auch einen Abruf erfolgreich zeigt (optional in Schritt 3). Sonst gehen EBZ-Wareneingänge mit
   `NichtNoetig` wie in Teil 1.
7. **Konflikt Abrufmenge:** Der Backlog (Z. 18) und die Entscheidung B-1 nennen `Menge`. `Menge` ist in der Bestelleinheit
   (`Mengeneinheit`). Entscheidung B-2 bucht und zeigt in der **Basiseinheit**. Welches Feld gilt als Abrufmenge?
   *Empfehlung:* `MengeBasis`. Für buchbare Zeilen sind Bestell-, Basis- und Lagereinheit ohnehin gleich (Sperre
   `EinkaufBestellungenSyncService.cs:131-132`). P-W8 bestätigt, dass `Menge = MengeBasis` dort gilt.
8. **Rahmenabrufe mit Rest 0 im WMS:** Sage zeigt sie weiter, solange der Rahmen offen ist. Sollen sie in der Vorschau standardmäßig
   ausgeblendet werden? Und ist akzeptiert, dass in Sage direkt (ohne WMS) gebuchte Abruf-Lieferungen im WMS nicht abgezogen werden und der
   Abruf dann offen bleibt?
   *Empfehlung:* ausblenden mit Filter „auch vollständig erfasste Abrufe“; die Grenze akzeptieren und in der Hilfe nennen.
9. **Chargen-/nachweispflichtige Positionen bei eingeschaltetem Sage-WE:** Status `NurInSage`, kein Sage-WE und **auch kein** Queue-Zugang?
   Begründung: Der Einkauf bucht diese Wareneingänge in Sage mit Charge **und** Bestand; ein zusätzlicher Queue-Zugang würde den Bestand
   verdoppeln.
   *Empfehlung:* ja, das setzt Antwort 10 aus Teil 1 fort.
10. **Schreibtest am AKE-Test:** Gibt Gerald ihn frei? Wer führt ihn aus, und wer baut den ELL-Beleg in Sage zurück (Löschen bzw. Storno durch
    den Einkauf)?
    *Empfehlung:* Freigabe für genau einen Test-Beleg nach dem Ablauf oben; Rückbau durch Gerald oder den Einkauf in der Sage-Oberfläche,
    Ergebnis in dieser Spec. Ohne Freigabe bleibt E4 blockiert.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. SQL-Nummer 114 oder 142? →
2. Ein Sage-Beleg je Absenden oder je Position? →
3. Ziel-Platz ohne Sage-Bezug: sperren oder ohne Sage-WE buchen? →
4. Kein Auto-Retry, „Erneut senden“ von Hand mit RequireStockKeyUserAccess? →
5. Lieferschein-Nummer des Lieferanten erfassen? →
6. Sage-WE auch für Rahmenabrufe? →
7. Abrufmenge: MengeBasis oder Menge? →
8. Abrufe mit Rest 0 ausblenden; Grenze „in Sage direkt gebucht“ akzeptiert? →
9. Charge/Seriennummer: NurInSage, kein WE, kein Queue-Zugang? →
10. Schreibtest-Freigabe, Ausführender, Rückbau? →
