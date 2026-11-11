---
type: spec
title: "Wareneingang Teil 2: Sage-Wareneingangsbeleg (ELL)"
slug: 2026-11-08-wareneingang-teil-2-sage-we-beleg-rahmenabrufe-spec
status: InUmsetzung
created: 2026-11-08
updated: 2026-11-11
freigabe_von: "Gerald Weichbold (Chat Koordination, 2026-11-11 20:40 „beide freigeben“)"
freigabe_am: 2026-11-11
source_backlog: "[[2026-11-08-wareneingang-teil-2-sage-we-beleg-rahmenabrufe]]"
depends_on: "[[2026-11-08-wareneingang-aus-sage-bestellungen-spec]]"
folge_backlog: "[[2026-11-11-wareneingang-teil-3-rahmenabrufe]]"
version: "nach 1.99.1 (Dev-Lauf legt fest)"
sql_nummern: "eine Migration in E1: nächste freie Nummer bei der Umsetzung, derzeit 143 (142 = AddDeployScriptRuns, v1.99.0), Freigabe-Antwort 1"
testszenario: "TS-116 (Erweiterung 116.10 ff., ersetzt den Platzhalter 116.7)"
task: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: AKE
affected_code:
  - "IDEALAKEWMSService/Services/Sdata/EinkaufBestellungenSyncService.cs:104,146 (SyncedAt vor dem SData-Lesen nehmen)"
  - "IdealAkeWms/Controllers/PurchaseOrdersController.cs:114-128 (ZeilenAsync, erfasst)"
  - "IdealAkeWms/Controllers/PurchaseOrdersController.Receive.cs:44-73 (Prüfschleife: Platzregel an :64, Lieferschein-Pflicht), :75-107 (Receipt: Status, SageWeVorgang, Lieferschein), :114-136 (BuildReceiveAsync)"
  - "IdealAkeWms/Models/ViewModels/PurchaseOrderReceiveViewModel.cs:4-14 (Kopf: Lieferschein, Sage-WE an), :20 (Rest-Regel), :22-24 (NichtBuchbar)"
  - "IdealAkeWms/Data/Repositories/PurchaseOrderReceiptRepository.cs:10,19-25 (GetErfasstJeBelPosAsync)"
  - "IdealAkeWms/Models/PurchaseOrderReceipt.cs:17-31 (neue Felder SageWe*, Lieferschein)"
  - "IdealAkeWms/Data/ApplicationDbContext.cs:1413-1430 (Konfiguration PurchaseOrderReceipts)"
  - "IdealAkeWms/Models/StorageLocation.cs:52,63,71 (Platzregel liest SageBuchungErlaubt, SageLagerkennung, SageLagerplatzId; keine Änderung)"
  - "IdealAkeWms/Services/SageBookingEnqueueDecision.cs:64-68 (neuer Ausschluss-Parameter)"
  - "IdealAkeWms/Data/Repositories/SageBookingEnqueueingStockMovementRepository.cs:65-67 (Decorator liest Ausschluss)"
  - "IdealAkeWms/Data/Repositories/SageBookingQueueRepository.cs:139-155 (EnqueueMissingAsync, EF-Filter)"
  - "IDEALAKEWMSService/Workers/SageBookingWorker.cs:58-80 (eigener, eigens geschalteter Schritt Sage-WE); Muster Lookup vor Requeue :119-144, :173-195"
  - "IdealAkeWms/Services/SdataUrlBuilder.cs:42-44 (BuildResourceUrl wiederverwenden)"
  - "NEU IDEALAKEWMSService/Services/SageWareneingangClient.cs + SageWareneingangPayloadBuilder.cs (StandardBelegService, Muster SageLagerbuchungClient.cs:33-75)"
  - "NEU IDEALAKEWMSService/Services/SageWareneingangService.cs (ein Sende-Lauf, ISyncLogger letzter Ctor-Parameter)"
  - "NEU nur E5/P-W9: IDEALAKEWMSService/Services/Sdata/SdataWareneingangLookupReader.cs (Muster SdataBuchungLookupReader.cs:16-45, Marker wie SageBookingCorrelation.cs:13-18)"
  - "IdealAkeWms/Services/SyncLogger/SyncLogServices.cs:24-37 (neuer Name SageWareneingang + DisplayName)"
  - "IdealAkeWms/Models/ServiceSettingDefinitions.cs:66,74,76,169-179 (SageWareneingangBelegAktiv; Verwendung MaxRetries/StuckMinutes)"
  - "IdealAkeWms/Models/AppSettingKeys.cs + AppSettingDefinitions.cs:85-87 (WareneingangLieferscheinPflicht, Muster B(...))"
  - "NEU Liste Sage-Wareneingänge: Controller-Action + View (Muster SageBookingQueueController.cs:15-16,110-112)"
  - "IdealAkeWms/Views/PurchaseOrders/Receive.cshtml (Lieferschein-Feld, Sage-WE-Status, Platz-Hinweis)"
  - "SQL/143_AddPurchaseOrderReceiptSageWe.sql (Nummer bei Umsetzung prüfen), SQL/00_FreshInstall.sql"
  - "scripts/deploy/nachlauf.psd1 (SQL/143, zwei Einstellung-Einträge, eine Info)"
  - "docs/TESTSZENARIEN.md (TS-116.10 ff.), README.md (AppSettings + ServiceSettings), Views/Help/Index.cshtml + Changelog.cshtml, beide AppVersion.cs"
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
  - "E1 Sage-WE-Grundlage: Receipt-Felder + Migration (SQL/143), Ausschluss aus Queue (Decision, Decorator, Nacherfassung), Rest-Regel EBB, SyncedAt vor dem Lesen – ohne Wirkung, solange kein Receipt einen Status hat"
  - "E2 Lieferschein-Nr. des Lieferanten in der WE-Maske + AppSetting WareneingangLieferscheinPflicht"
  - "E3 Liste 'Wareneingänge an Sage' (Listen-View-Pattern) + 'Erneut senden' von Hand + Status-Zeile in der Maske"
  - "E4 Sender ohne Auto-Retry: Setting SageWareneingangBelegAktiv, Status beim Buchen, Platzregel, StandardBelegService-Client, Worker-Schritt; abschließend der eine vereinbarte Schreibtest über DEV"
  - "E5 (blockiert bis P-W9 belegt) Auto-Retry mit Sage-Prüfung vor jedem Wiederholen"
  - "E6 Abschluss: Version, Hilfe, Changelog, README, nachlauf.psd1, TS-116.10 ff."
deploy:
  web: true
  service: true
  migration: true
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
---

# Wareneingang Teil 2: Sage-Wareneingangsbeleg (ELL)

> [!note] Umfang geändert 2026-11-11 (Freigabe-Antwort 6)
> Rahmenabrufe (EBZ) sind aus dieser Spec herausgenommen; alles dazu steht jetzt in [[2026-11-11-wareneingang-teil-3-rahmenabrufe]].
> Dateiname und `slug` behalten bewusst das alte Kürzel `…-sage-we-beleg-rahmenabrufe-spec`, damit die Wikilinks (Backlog, Dashboard,
> Teil 1) nicht brechen. Massgeblich ist der Titel.

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
Die Lieferschein-Nummer des Lieferanten geht mit an Sage (Freigabe-Antwort 5).
~~Ziel 2: Auch Rahmenabrufe (EBZ) werden angezeigt und gebucht.~~ *Überholt (Freigabe-Antwort 6): verschoben nach
[[2026-11-11-wareneingang-teil-3-rahmenabrufe]].*

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- ~~F1 Rahmenabrufe (EBZ)~~ – *überholt, entfällt (Freigabe-Antwort 6), siehe [[2026-11-11-wareneingang-teil-3-rahmenabrufe]].*
- F2 Sage-Wareneingangsbeleg (Kennzeichen **ELL**, siehe P-W7) per `StandardBelegService` hinter `SageWareneingangBelegAktiv` (Default aus),
  **ein Beleg je WE-Buchung** (Freigabe-Antwort 2), nur auf Sage-Plätze (Freigabe-Antwort 3).
- F3 Ausschluss solcher Wareneingänge aus der Sage-Lagerbuchungs-Queue, an **beiden** Stellen: Decorator und Nacherfassung.
- F4 Status und Fehler je Wareneingang, Liste mit „Erneut senden“; automatisches Wiederholen mit Sage-Prüfung **nur**, wenn P-W9 die
  Korrelation belegt (Freigabe-Antwort 4).
- F5 Rest-Regel so anpassen, dass ein an Sage übergebener Wareneingang nicht zweimal abgezogen wird.
- F6 Lieferschein-Nummer des Lieferanten in der WE-Maske, an Sage als `EKLieferschein`; Pflicht per Einstellung (Freigabe-Antwort 5).

**Out-of-Scope** (Backlog „Nicht im Umfang“): Etikettendruck beim Eingang, Erfassung von Charge/Seriennummer, Rechnungsprüfung.
Ebenfalls nicht enthalten: IDEAL, weil dort kein SData-Endpunkt besteht (Teil 1, Antwort 7). Rahmenabrufe (EBZ):
[[2026-11-11-wareneingang-teil-3-rahmenabrufe]]. Ohne belegtes P-W9 auch kein automatisches Wiederholen (dann nur „Erneut senden“ von Hand).

## Fachliche Anforderungen

### F1 Rahmenabrufe (EBZ) – überholt
*Entfällt (Freigabe-Antwort 6, Gerald 2026-11-11).* Anforderung, Befunde (Datenbasis, Vorgangsmengen je Rahmen, vorhandener Bezug über
`BelPosId`), Entwurf, Akzeptanzkriterien, Testszenarien und die nie entschiedenen Rückfragen 6-8 samt P-W8 stehen vollständig in
[[2026-11-11-wareneingang-teil-3-rahmenabrufe]]. Der Abgleich bleibt in dieser Spec bei EBB (`EinkaufBestellungenSyncService.cs:32,112`).

### F2 Sage-Wareneingangsbeleg (ELL)
- Hinter dem ServiceSetting `SageWareneingangBelegAktiv` (Default aus). Steht es aus, verhält sich alles wie in Teil 1 (auch die Platzwahl).
- **Entscheidung beim Buchen, nicht beim Senden:** Ob ein Wareneingang als Sage-WE geht, wird beim Absenden der Maske festgelegt (Status am
  Receipt). Ein späteres Umschalten ändert bereits gebuchte Wareneingänge nicht, sie gehen also nie doppelt (Queue **und** WE) an Sage.
- **Platzregel (Freigabe-Antwort 3):** Bei eingeschaltetem Setting sind nur Ziel-Plätze buchbar, die
  - in Sage vorhanden sind: `SageLagerkennung` und `SageLagerplatzId` gesetzt (`StorageLocation.cs:63,71`; vom Lagerplatz-Abgleich befüllt, bei
    manuellen Plätzen null, `StorageLocation.cs:54-71`; dieselbe Prüfung wie `SageLagerbuchungPayloadBuilder.cs:79-87`), **und**
  - für Sage-Buchungen freigegeben sind: `SageBuchungErlaubt = true` (`StorageLocation.cs:51-52`, Anzeigename „Sage-Buchung erlaubt“; dasselbe
    Flag, das Decorator und Nacherfassung lesen, `SageBookingEnqueueingStockMovementRepository.cs:60`, `SageBookingQueueRepository.cs:147`).
  - Jeder andere Platz ergibt einen Zeilenfehler in der Prüfschleife (`PurchaseOrdersController.Receive.cs:60-65`, neue Bedingung nach der
    Platzwahl `:64`): „Ziel-Platz <Code> ist kein Sage-Platz mit Sage-Buchung (nicht in Sage vorhanden oder ‚Sage-Buchung erlaubt‘ aus) –
    Sage-Wareneingang nicht möglich. Bitte einen Sage-Platz wählen.“ Wie heute wird dann **nichts** gebucht (`:71-73`).
  - Die Regel gilt für alle Zeilen der Buchung, auch für `NurInSage`-Zeilen.
- Bei eingeschaltetem Setting setzt das Buchen je Zeile den Status:
  - `Offen`: Platzregel erfüllt und keine Chargen-/Nachweispflicht.
  - `NurInSage`: `Chargenpflicht` oder `Nachweispflicht` (Teil 1, Antwort 10; Freigabe-Antwort 9: solche Artikel gibt es derzeit nicht, die
    Regel bleibt als Absicherung). Die Zeile wird nicht gesendet und geht auch nicht in die Queue. Hinweis an der Zeile:
    „Charge/Seriennummer: Wareneingang in Sage buchen“.
- **Ein Beleg je WE-Buchung (Freigabe-Antwort 2):** Alle `Offen`-Zeilen eines Absendens bilden einen ELL-Beleg. Ein Absenden betrifft immer
  genau eine Bestellung (`Receive(int belId, …)`, `PurchaseOrdersController.Receive.cs:44`), also genau einen `SourceBelegHandle`.
  Gruppenschlüssel ist die neue Spalte `SageWeVorgang` (Guid, je Absenden neu). Die Zeitgleichheit von `now` (`:75,93`) wird bewusst nicht
  genutzt. Zwei Absenden = zwei Belege, auch bei derselben Bestellung.
- **Beleg:** `POST …/CommonWawiServices/{Dataset}/$service/StandardBelegService` ([[sage-sdata-commonwawiservices]] Z. 70-73).
  - Kopf: `Erfassungsart = 4100`, `IsBelegTransform = true`, `BelegTransformArt = 1` (Belegübernahme), `BelegKennzeichen = "ELL"`
    (Konstante, kein Setting; Teil 1 „Einstellungen“ letzter Absatz), `TransformSettings[].SourceBelegHandle` = `BelId` der Bestellung.
  - `EKLieferschein` = die eingegebene Lieferschein-Nummer (F6); leer = Feld nicht senden. Das Feld ist ein Belegkopf-Feld des
    StandardBelegService ([[sage-sdata-commonwawiservices]] Z. 29), Länge 20 (`EkLieferschein(20)`, [[sage-sdata-masterdata]] Z. 119).
  - Positionen mit `BelegPositionLagermengen{PlatzId, Kurzbezeichnung, Menge/MengeBasis}`.
  - Menge in der Basiseinheit (B-2).
  - Welche Handles (`PositionHandle`, `VorgangPositionHandle`) bei einer Übernahme zu belegen sind und was `Kurzbezeichnung` erwartet
    (Lagerkennung oder Platz-Kurzbezeichnung), ist **unbelegt**. Das klärt der Schreibtest (E4, Abschnitt „Prüfschritte“). Bis dahin baut der
    Dev-Lauf den Payload-Builder nach der besten Lesart der Schema-Doku und legt ihn erst mit dem Schreibtest-Ergebnis fest.
- Korrelation: `Memo` des Belegkopfs = `WE#<ersteReceiptId>#` (Trennzeichen-Regel wie `SM#<id>#`, `SageBookingCorrelation.cs:13-18`,
  [[fallstricke]] Z. 860-861). Ob Sage das Memo am ELL-Beleg speichert und ob es per SData auffindbar ist, ist unbelegt (P-W9).
- **Senden ohne blindes Wiederholen:** Vor dem POST wird der Status auf `Gesendet` gesetzt und `SageWeVersuche` um 1 erhöht (Muster AK10,
  `SageBookingWorker.cs:197-198`). Danach gilt:
  - Erfolg: Status `Uebergeben`, dazu `SageWeBelegnummer` (`BelegnummerFormatiert` aus der Response, [[sage-sdata-commonwawiservices]] Z. 30)
    und `SageWeAm`.
  - Fehler: Status `Fehler`, `SageWeFehler` (Response-Text, gekappt).
  - Wie es nach `Fehler` oder einem hängenden `Gesendet` weitergeht, regelt F4 „Wiederholen“ (zwei Varianten, abhängig von P-W9).
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
  - Spalten: gebucht am, Bestellung, Pos., Artikel, Menge, Ziel-Platz, Lieferschein, Status (Text), Versuche, Sage-Beleg, Fehler.
  - Filterkarte: Status, Bestellung, Zeitraum.
  - Server-Spaltenfilter, Pagination über `PageSize.Resolve`.
  - Lesen mit `RequireStockReadAccess`, „Erneut senden“ (nur bei `Fehler`) mit `RequireStockKeyUserAccess`, wie
    `SageBookingQueueController.cs:13-16,110-112` (Rolle wie bei der Sage-Queue, Rückfrage 4).
  - „Erneut senden“ setzt die ganze `SageWeVorgang`-Gruppe auf `Offen` und schreibt Audit-Felder. `SageWeVersuche` bleibt stehen (Zähler für
    die Prüfung vor dem Senden, Variante A).
- In der Maske „Wareneingang zu Bestellung“ steht je Position eine Zeile „Sage-Wareneingang: <Status>“ für ihre Receipts. Die Spalte
  „im WMS erfasst“ bleibt die Gesamtsumme.

**Wiederholen (Freigabe-Antwort 4) – zwei Varianten, P-W9 entscheidet, welche gebaut wird:**

- **Variante B – Standard, solange P-W9 nicht belegt ist (E3/E4):** kein automatisches Wiederholen.
  - `Fehler` bleibt `Fehler`, bis jemand mit `stock_keyuser` „Erneut senden“ klickt.
  - Ein `Gesendet`, das länger als `Sync:SageLagerbuchungStuckMinutes` hängt, wird zu `Fehler` mit dem Text „Ergebnis unklar – bitte in Sage
    prüfen, ob ein Wareneingangsbeleg mit Memo WE#<id># existiert“. Es wird nicht neu gesendet.
  - Die Maske „Erneut senden“ zeigt den Hinweis: „Vorher in Sage nachsehen, ob der Beleg (Memo WE#<id>#, Lieferschein <Nr>) schon existiert –
    sonst entsteht ein zweiter Wareneingang.“
- **Variante A – nur wenn P-W9 belegt ist (E5):** Der Dienst wiederholt selbst und prüft **vor jedem** Wiederholen in Sage, ob der Beleg schon
  existiert. Muster ist der Lookup vor dem Requeue der Lagerbuchungen (`SageBookingWorker.cs:119-144` für hängende, `:173-195` vor dem erneuten
  Senden).
  - Kandidaten je Takt: Gruppen mit `Fehler` und `SageWeVersuche < Sync:SageLagerbuchungMaxRetries` sowie Gruppen, die länger als
    `Sync:SageLagerbuchungStuckMinutes` auf `Gesendet` stehen; außerdem `Offen`-Gruppen mit `SageWeVersuche > 0` (nach „Erneut senden“).
  - Lookup per SData (`SdataWareneingangLookupReader`, Muster `SdataBuchungLookupReader.cs:16-45`): Beleg mit Memo `WE#<id>#` gefunden →
    `Uebergeben` (Belegnummer aus dem Lookup, falls geliefert), **kein** POST.
  - Nicht gefunden → erneut senden (Status `Gesendet`, `SageWeVersuche + 1`).
  - Lookup wirft (Netz, HTTP, Konfiguration) → **nicht** senden, Status bleibt, Warnung im Aktivitäts-Protokoll (Count `uebersprungen`). Der
    Lookup liefert bei Fehlern nie `false` (Begründung `SdataBuchungLookupReader.cs:12-15`).
  - Versuche erschöpft → `Fehler` mit „Maximale Versuche (n) erreicht – Erneut senden von Hand“. „Erneut senden“ läuft danach ebenfalls über
    den Lookup.
  - Bekannte Grenze: Ein Sage-Ausfall von wenigen Takten (Takt `Sync:SageLagerbuchungIntervalSeconds`, Default 20 s,
    `SageBookingWorker.cs:60-61`) kann die Versuche aufbrauchen; dann bleibt „Erneut senden“. Ein Backoff wird nicht gebaut.
- Ohne belegtes P-W9 wird Variante A **nicht** gebaut; E5 wird in der Etappen-Tabelle als Blocker festgehalten.

### F5 Rest-Regel (Teil 1, Antwort 2, präzisiert)
- **EBB:** Rest = offen laut Sage − Σ Receipts, die zum Zeitpunkt des Abgleichs noch **nicht** in Sage enthalten waren. Das sind alle außer
  `Uebergeben` mit `SageWeAm` ≤ `SyncedAt` der Cache-Zeile. Ohne diese Ausnahme zieht das WMS einen übergebenen Wareneingang ein zweites Mal ab.
  Teil 1 nannte dieses Risiko bei Antwort 2 („würde doppelt abziehen – im UAT prüfen“).
- ~~**EBZ:** Rest = Abrufmenge − Σ alle Receipts~~ – *überholt (Freigabe-Antwort 6), siehe [[2026-11-11-wareneingang-teil-3-rahmenabrufe]].*
- `SyncedAt` wird künftig **vor** dem SData-Lesen genommen (heute `EinkaufBestellungenSyncService.cs:104`, nach dem Lesen). Sonst gilt eine
  Übergabe, die während des Lesens fertig wird, fälschlich als im Sage-Stand enthalten.
- Die Regel liegt an **einer** Stelle (`PurchaseOrderReceiveRow.Rest`, `PurchaseOrderReceiveViewModel.cs:20`). Das Repository liefert dafür je
  `BelPosId` die Gesamtsumme und die Summe „nicht in Sage enthalten bis Stand X“, in einer Abfrage ohne N+1.

### F6 Lieferschein-Nummer des Lieferanten (Freigabe-Antwort 5)
- Ein Eingabefeld „Lieferschein-Nr. (Lieferant)“ im **Kopf** der Maske „Wareneingang zu Bestellung“, einmal je Absenden (eine Lieferung = ein
  Lieferschein). Getrimmt, höchstens 20 Zeichen (Sage `EkLieferschein(20)`, [[sage-sdata-masterdata]] Z. 119); länger = Fehler
  „Lieferschein-Nr. höchstens 20 Zeichen (Sage-Feld EKLieferschein)“.
- Gespeichert in der neuen Spalte `PurchaseOrderReceipt.Lieferschein` an **jedem** Receipt dieses Absendens (keine Kopf-Tabelle, YAGNI).
- Pflicht nur, wenn das AppSetting `WareneingangLieferscheinPflicht` an ist (Default aus). Leer bzw. nur Leerzeichen bei Pflicht →
  ModelState-Fehler „Bitte die Lieferschein-Nummer des Lieferanten eingeben.“, nichts gebucht (Prüfung vor dem Buchen,
  `PurchaseOrdersController.Receive.cs:69-73`).
- Feld und Pflicht gelten unabhängig vom Sage-WE-Schalter: Die Nummer ist am Wareneingang auch ohne Sage-WE nachvollziehbar. **An Sage geht sie
  nur im Sage-WE** (`EKLieferschein`, F2), nie über die Lagerbuchungs-Queue.
- Bei Fehlern bleibt die Eingabe stehen (wie die Zeilen-Eingaben, `PurchaseOrdersController.Receive.cs:113-119`): neues Feld
  `PurchaseOrderReceiveViewModel.Lieferschein`, Post-Parameter `string? lieferschein`.
- Die Liste „Wareneingänge an Sage“ zeigt die Spalte (F4).

## Technischer Loesungsentwurf

- ~~**E1 Lesen EBZ**~~ – *überholt (Freigabe-Antwort 6): zwei `eq`-Abfragen EBB/EBZ, Counts, EBZ-Mengen – verschoben nach
  [[2026-11-11-wareneingang-teil-3-rahmenabrufe]]. Die Etappen sind neu nummeriert.*
- **E1 Grundlage:**
  - Neue Felder an `PurchaseOrderReceipt`: `SageWeStatus` (int, NOT NULL, DEFAULT 0 **nur im SQL/Migration**, nicht `HasDefaultValue`,
    [[fallstricke]] Z. 1717), `SageWeVorgang` (uniqueidentifier NULL, Index), `SageWeAm` (datetime2 NULL), `SageWeBelegnummer` (nvarchar(30) NULL),
    `SageWeFehler` (nvarchar(1000) NULL), `SageWeVersuche` (int, NOT NULL, DEFAULT 0 nur im SQL), `Lieferschein` (nvarchar(20) NULL).
  - `SageWeVersuche` kommt schon in E1, auch wenn nur Variante A ihn auswertet: eine Migration statt einer zweiten in E5; Variante B zeigt ihn in
    der Liste.
  - Ausschluss F3 an Decision, Decorator und `EnqueueMissingAsync`. Rest-Regel F5, `SyncedAt` vor dem Lesen.
  - Solange kein Code einen Status ≠ 0 setzt, ändert sich nichts. Die Etappe ist deshalb ohne Wirkung mergebar.
- **E2 Lieferschein (F6):** Feld im Kopf von `Receive.cshtml`, Post-Parameter, Prüfung, Speichern am Receipt. AppSetting
  `WareneingangLieferscheinPflicht`: Konstante in `AppSettingKeys` und Eintrag `B(...)` in `AppSettingDefinitions.All`, Gruppe `GLager`, direkt
  nach `WareneingangBestellungAktiv` (`AppSettingDefinitions.cs:85-87`), `abhaengig: AppSettingKeys.WareneingangBestellungAktiv`. Gelesen wird
  über `IAppSettingRepository.GetValueAsync` (wie `RequireWareneingangBestellungAktivAttribute.cs:25`). Der Drift-Guard
  `AppSettingDefinitionsTests` bekommt den Key. Hinweis: `AppSettingKeys.FesteWerte` gibt es nicht mehr; feste Werte stehen seit v1.77.0 im
  Katalog (`AppSettingDefinitions.cs:218`), ein Bool ist dort `SettingType.Bool` und wird in `/Settings` als Schalter gezeigt
  (`_SettingField.cshtml:41-49`).
- **E3 Liste:** neue Action in `PurchaseOrdersController` (Feature-Gate `RequireWareneingangBestellungAktiv` kumulativ) oder ein eigener kleiner
  Controller. Der Dev-Lauf wählt den kürzeren Weg. Referenz für Pattern und Rechte: `SageBookingQueueController`.
- **E4 Sender (Variante B):**
  - Status beim Buchen (`Offen`/`NurInSage`, neue `SageWeVorgang` je Absenden) und Platzregel F2 in `PurchaseOrdersController.Receive.cs`; das
    Setting liest die Web-App wie der Decorator über die ServiceSettings (`SageBookingEnqueueingStockMovementRepository.cs:51`).
  - `SageWareneingangPayloadBuilder` (rein, testbar; Kopf inkl. `EKLieferschein` und `Memo`) und `SageWareneingangClient`. Der Client ist eine
    Kopie von `SageLagerbuchungClient.cs:33-75` mit URL über `SdataUrlBuilder.BuildResourceUrl(…, "$service/StandardBelegService")`
    (`SdataUrlBuilder.cs:42-44`). Endpunkt und Zugang wie `SageBookingWorker.cs:99-107` (dieselben `SageLagerbuchung:Username/Password`).
  - `SageWareneingangService.RunOnceAsync`: gruppiert `Offen` nach `SageWeVorgang`, sendet und bewertet; hängendes `Gesendet` → `Fehler`
    „Ergebnis unklar“ (F4 Variante B). Eigener Lauf im Aktivitäts-Protokoll (`SyncLogServices.SageWareneingang`, DisplayName
    „Sage-Wareneingang“). Counts: `gesendet`, `uebergeben`, `fehler`, `unklar`.
  - Aufruf als **eigener** Schritt im `SageBookingWorker`-Takt, gegated nur durch `SageWareneingangBelegAktiv`, getrennt vom
    `SageLagerbuchungAktiv`-Block (`SageBookingWorker.cs:66-67`). So bleibt EINE Instanz zuständig (Idempotenz-Voraussetzung, [[fallstricke]]
    Z. 855-858), und es gibt keinen zweiten BackgroundService.
  - DI-Auflösungstest wie bei den übrigen Diensten.
  - Abschluss der Etappe: der eine vereinbarte Schreibtest über DEV (Abschnitt „Prüfschritte“), danach Payload festlegen und Ergebnis eintragen.
- **E5 Auto-Retry (Variante A, nur mit belegtem P-W9):**
  - `SdataWareneingangLookupReader` über `ISageSdataReadClient.QueryAllAsync` mit Ressource, Memo-Feld und `like`-Muster aus P-W9
    (`where <Memo> like '%WE#<id>#%'`, `count: 1`), wirft bei jedem Fehler. Ein Direkt-SQL-Lookup wie `SageBuchungLookupReader.cs:14-15` wird
    **nicht** gebaut (AKE liest Sage über SData; ein zweiter Weg nur, wenn P-W9 zeigt, dass SData das Memo nicht filtern kann – dann E5 nicht
    bauen und als Blocker festhalten).
  - Wiederhol-Schleife in `SageWareneingangService` nach F4 Variante A, Counts zusätzlich `bestaetigt_lookup`, `uebersprungen`.
  - Unit-Tests mit gefälschtem Lookup: gefunden, nicht gefunden, wirft, Versuche erschöpft.
- **Verworfene Alternative:** Die WE-Belege über `SageBookingQueueItems` mit einer Spalte „Art“ zu führen hätte Retry, Requeue-UI und
  Recovery mitgenutzt. Dagegen spricht: Die Queue ist 1:1 je `StockMovement` (UNIQUE, SQL/82-83), ein Beleg umfasst mehrere Bewegungen, und
  der Sender hinge am Schalter `SageLagerbuchungAktiv`. Der Backlog verlangt außerdem Status je Receipt (Z. 13).
- Oberfläche: Skill `frontend-design` explizit aufrufen; Bootstrap 5, WCAG AA, Status immer als Text. Für Fehlerzeilen `text-danger-emphasis`
  (Teil 1 Q-1/Q-8).

## Migrations-/SQL-Auswirkungen

| Etappe | Migration | Skript | Inhalt | daten-destruktiv |
|---|---|---|---|---|
| E1 | `AddPurchaseOrderReceiptSageWe` | `SQL/143_AddPurchaseOrderReceiptSageWe.sql` (nächste freie Nummer, Freigabe-Antwort 1) | 7 Spalten an `PurchaseOrderReceipts`: `SageWeStatus` int NOT NULL DEFAULT 0, `SageWeVorgang` uniqueidentifier NULL, `SageWeAm` datetime2 NULL, `SageWeBelegnummer` nvarchar(30) NULL, `SageWeFehler` nvarchar(1000) NULL, `SageWeVersuche` int NOT NULL DEFAULT 0, `Lieferschein` nvarchar(20) NULL; Index `SageWeVorgang`, Index `SageWeStatus` | nein (additiv; Altbestand = 0 = Verhalten Teil 1) |
| E2–E6 | – | – | – | – |

- ADR 0004: Model → `dotnet ef migrations add` → idempotentes SQL mit `COL_LENGTH`/`sys.indexes`-Guard (DDL eigener Batch, Muster
  `SQL/140_AddArtikelMengeneinheiten.sql:9-15`) → `__EFMigrationsHistory`-Insert eigener Batch → `SQL/00_FreshInstall.sql` an **beiden**
  Stellen (Spalten in der Tabellendefinition `PurchaseOrderReceipts` **und** `MigrationId`).
- Nummer (Freigabe-Antwort 1): die nächste freie Nummer bei der Umsetzung, derzeit **143** – im Bündel-Worktree liegen `SQL/140`, `141` und
  `142_AddDeployScriptRuns.sql` (v1.99.0, `scripts/deploy/nachlauf.psd1:80-81`). Der Dev-Lauf prüft vor dem Anlegen erneut; ändert sich die
  Nummer, zieht er sie in Spec, Manifest und Etappen-Tabelle nach. `114` bleibt ungenutzt.
- `SQL/AgentJobs/*`: nicht betroffen (keine Pflichtspalte an einer AgentJob-Tabelle, kein Folge-MERGE).
- Das neue ServiceSetting legt der Web-Start aus dem Katalog an (`Program.cs:309-324`, idempotent). Das neue AppSetting braucht keine Zeile
  (`seed: false`, der Leser nimmt den Default „false“, `AppSettingDefinitions.cs:7-10`). Kein eigenes SQL, keine Pruefung im Manifest.
- Die Web-App nicht lokal aus dem Worktree starten, solange die Migration pending ist ([[fallstricke]] Z. 865-874).

## Einstellungen

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| `SageWareneingangBelegAktiv` (ServiceSetting, Gruppe „Sage-Lagerbuchung“), Anzeigename **„Wareneingänge als Sage-Wareneingangsbeleg buchen“** | Bool (Schalter) | an / aus (feste Werte, Schalter; Server validiert über den Katalog) | aus | An: Wareneingänge zu Bestellungen werden beim Buchen für Sage vorgemerkt und vom Dienst als **ein** Wareneingangsbeleg je WE-Buchung (ELL, Belegübernahme aus der Bestellung, platzgenau, mit Lieferschein-Nr.) in Sage angelegt. Sie gehen **nicht** zusätzlich als Lagerbuchung-Zugang an Sage. Als Ziel sind dann nur Sage-Plätze mit „Sage-Buchung erlaubt“ wählbar. Aus: Verhalten wie Teil 1 (Lagerbuchung-Queue; der Einkauf schließt die Position in Sage). Umschalten wirkt nur auf neue Buchungen. | Maske „Wareneingang zu Bestellung“ (Status und Platzregel beim Buchen); Dienst: Sage-Buchungs-Worker, Lauf „Sage-Wareneingang“; Liste „Wareneingänge an Sage“ | AKE (IDEAL aus, kein SData-Endpunkt) | „Schreibt Wareneingangsbelege nach Sage: Bestellung wird beliefert, Bestand in Sage gebucht. Nur nach erfolgreichem Schreibtest am AKE-Test einschalten. Der Einkauf darf diese Positionen danach nicht mehr von Hand schließen. Wareneingänge auf Plätze ohne Sage-Buchung sind gesperrt.“ |
| `WareneingangLieferscheinPflicht` (AppSetting, Gruppe „Lager und Buchungen“, abhängig von `WareneingangBestellungAktiv`), Anzeigename **„Wareneingang: Lieferschein-Nr. Pflicht“** | Bool (Schalter, `SettingType.Bool`) | an / aus (feste Werte, Schalter `_SettingField.cshtml:41-49`; Server validiert über den Katalog) | aus (= Feld optional); `seed: false` | An: In „Wareneingang zu Bestellung“ muss die Lieferschein-Nummer des Lieferanten eingegeben werden, sonst wird nichts gebucht. Aus: Feld optional. | Lager → Wareneingang zu Bestellung (Feld „Lieferschein-Nr. (Lieferant)“) | AKE (IDEAL erst mit SData-Endpunkt, wie `WareneingangBestellungAktiv`, `AppSettingDefinitions.cs:87`) | keiner (keine Außenwirkung; an Sage geht die Nummer nur über `SageWareneingangBelegAktiv`) |
| `Sync:SageLagerbuchungMaxRetries` (ServiceSetting, vorhanden) – **nur Verwendungstext geändert, nur wenn E5 gebaut wird** | Int | unverändert | 5 (unverändert) | zusätzlich: höchste Zahl automatischer Sende-Versuche je Sage-Wareneingangsbeleg | ergänzen: „Dienst: Sage-Buchungs-Worker; Sage-Wareneingang (Auto-Retry)“ (`ServiceSettingDefinitions.cs:74,177`) | AKE | keiner |
| `Sync:SageLagerbuchungStuckMinutes` (ServiceSetting, vorhanden) – **nur Verwendungstext geändert** | Int | unverändert | 10 (unverändert) | zusätzlich: ab wann ein Sage-Wareneingang auf `Gesendet` als hängend gilt (Variante B: → `Fehler` „Ergebnis unklar“; Variante A: Lookup) | ergänzen: „Dienst: Sage-Buchungs-Worker; Sage-Wareneingang“ (`ServiceSettingDefinitions.cs:76,179`) | AKE | keiner |

- `SageWareneingangBelegAktiv` in `ServiceSettingDefinitions` an **beiden** Stellen: Beschreibung (Block ab `:169`) und
  Anzeigename/Verwendung/Standort/Warnhinweis (Block ab `:66`). Der Drift-Guard-Test `ServiceSettingDefinitionsTests` bekommt den Key.
- Der Key kommt **erst in E4** in den Katalog. Bis dahin gibt es keinen Schalter, der Belege zurückhält, ohne sie zu senden.
- `WareneingangLieferscheinPflicht`: Konstante in `AppSettingKeys`, `B(...)`-Eintrag in `AppSettingDefinitions.All` (E2), Drift-Guard
  `AppSettingDefinitionsTests`. Kein neuer Wertebereich-Mechanismus: das frühere `AppSettingKeys.FesteWerte` ist durch den Katalog ersetzt
  (`AppSettingDefinitions.cs:218`).
- Für Wiederholversuche und „hängend“ gibt es bewusst **keine** neuen Keys; der Sage-WE-Schritt läuft im selben Worker und nutzt die
  vorhandenen Lagerbuchungs-Grenzen.
- Für das Kennzeichen ELL gibt es keine Einstellung (Konstante, von Sage vorgegeben). Sonst ändert sich kein Setting.

## Audit-Feld-Auswirkungen

- `PurchaseOrderReceipt` ist `AuditableEntity`. Status, `SageWeVorgang` und `Lieferschein` werden beim Buchen im selben Insert gesetzt (Created*
  wie heute, `PurchaseOrdersController.Receive.cs:103`). Jeder spätere Statuswechsel durch den Dienst (Senden, Fehler, „unklar“, in E5 auch
  Lookup-Bestätigung und Wiederholen) setzt `ModifiedAt`, `ModifiedBy` und `ModifiedByWindows` = `system:sage-wareneingang` (Muster
  `SageBookingQueueRepository.cs:12,186-191`).
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

- ~~**P-W8 Abrufmenge**~~ – *überholt (Freigabe-Antwort 6/7), Abfrage steht unverändert in [[2026-11-11-wareneingang-teil-3-rahmenabrufe]].*
- **P-W9 Korrelation am ELL-Beleg (nach dem Schreibtest; entscheidet zwischen Variante A und B, blockiert nur E5):**
  - (a) Sage-SQL `ake_TEST2026`, nur `SELECT`, zuerst
    `SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'KHKEKBelege' ORDER BY ORDINAL_POSITION;`
    dann, mit den gefundenen Namen für Memo und Belegnummer,
    `SELECT TOP 5 BelID, Belegkennzeichen, <Belegnummer>, <Memo> FROM dbo.KHKEKBelege WITH (NOLOCK) WHERE Belegkennzeichen = 'ELL' AND <Memo> LIKE '%WE#%' ORDER BY BelID DESC;`
    → Steht das gesendete Memo unverändert am Beleg?
  - (b) SData, nur `GET` (so liest der Dienst später): `EinkaufBelegeInfo/$schema` → Name des Memo-Felds (und des BelId-Felds, siehe P-W7);
    dann `EinkaufBelegeInfo?format=json&orderBy=<BelId-Feld>&count=5&select=<BelId-Feld>,Belegkennzeichen,<Memo>&where=<Memo> like '%WE#<id>#%'`
    mit der Memo-Kennung des Schreibtests. → Findet die Abfrage **genau** den Test-Beleg? Das prüft zugleich die zwei ungeprüften Punkte des
    vorhandenen Lookups: führendes `%` und das als `%23` kodierte `#` (`SdataBuchungLookupReader.cs:31-33`), und ob die Info-Ressource auch
    erledigte Belege liefert (P-W7: „Info-Ressourcen liefern eventuell nur offene Belege“).
  - **Belegt** = (a) und (b) positiv → Variante A, E5 wird gebaut. Sonst Variante B bleibt, E5 als Blocker. Ergebnis hier eintragen.
- **Schreibtest am AKE-Test – freigegeben (Freigabe-Antwort 10, Gerald 2026-11-11), wird im Dev-Lauf am Ende von E4 ausgeführt:**
  - **Regel dieser Spec:** Agenten schreiben nie nach Sage – einzige Ausnahme ist dieser **eine** vereinbarte Test-Beleg, über DEV gegen den
    Sage-AKE-Test (`sagetest01`, Datenbank/Dataset `ake_TEST2026`). Kein anderer POST, keine Wiederholung, kein Schreiben gegen ein anderes
    Sage-System. Antwortet Sage mit einem Fehler, prüft der Dev-Lauf **lesend**, dass kein Beleg entstanden ist, trägt das Ergebnis ein und
    fragt Gerald, bevor er einen zweiten Versuch macht. Der Rückbau in Sage ist Sache von Gerald bzw. dem Einkauf, nie des Agenten.
  - Auf DEV gilt während des Tests: `SageLagerbuchungAktiv` **aus** (sonst sendet der Worker auch Queue-Einträge an den Sage-Test – außerhalb der
    Freigabe), `SageWareneingangBelegAktiv` an, `SData:*` zeigt auf `sagetest01`/`ake_TEST2026`. Danach `SageWareneingangBelegAktiv` wieder aus.
  1. **Vorbereitung (lesend):** In `ake_TEST2026` eine Test-Bestellung EBB mit zwei Positionen wählen (anlegen nur durch Gerald/Einkauf).
     Bedingungen: Artikel ohne Chargen-/Nachweispflicht, gleiche Bestell-, Basis- und Lagereinheit, Ziel-Platz mit Sage-Bezug und „Sage-Buchung
     erlaubt“. Aus `EinkaufPositionenInfo` (`select=BelId,BelPosId,VorPosId,PositionGGBestellt,PositionGGGeliefert,PlatzID,Kurzbezeichnung`) und
     in der Sage-Oberfläche festhalten: `BelId`, `BelPosId`, `VorPosId`, Mengen, Platz-Bestand vorher. Einen DEV-Abgleich laufen lassen, damit
     die Bestellung im Cache steht.
  2. **Buchen (der eine Schreibvorgang):** In der DEV-Maske „Wareneingang zu Bestellung“ **nur Position 1** mit Teilmenge auf den Sage-Platz
     buchen, Lieferschein-Nr. `WMS-TEST-1`. Der Dienst sendet genau einen `POST …/CommonWawiServices/ake_TEST2026;1/$service/StandardBelegService`
     (`Erfassungsart 4100`, `IsBelegTransform true`, `BelegTransformArt 1`, `BelegKennzeichen "ELL"`, `TransformSettings[{SourceBelegHandle: <BelId>}]`,
     Pflicht-Kopffelder laut Schema, `EKLieferschein "WMS-TEST-1"`, `Memo "WE#<id>#"`, Position 1 mit
     `BelegPositionLagermengen[{PlatzId, Kurzbezeichnung, MengeBasis}]`). Request ohne Zugangsdaten mitschneiden (Dienst-Log).
  3. **Ergebnis prüfen (lesend):** Die Response trägt `BelegnummerFormatiert`, `ResultNumber`/`ErrorText` und je Position `ResultText`.
     - In Sage ist ein ELL-Beleg entstanden, mit Lieferschein `WMS-TEST-1`.
     - Position 1: `GGGeliefert` ist um die Teilmenge gestiegen.
     - Position 2 ist unverändert offen und **nicht** in den ELL-Beleg übernommen.
     - Der Platz-Bestand ist um die Teilmenge gestiegen, und zwar auf dem gesendeten Platz.
     - `EinkaufPositionenInfo` zeigt die verkleinerte offene Menge. P-W9 (a) und (b) ausführen.
     - Im WMS: Status `Uebergeben` mit Belegnummer, kein Eintrag in `SageBookingQueueItems` für die Bewegung.
     - ~~Optional: Überlieferung, EBZ-Abruf~~ – entfällt: jeder weitere Versuch wäre ein zweiter Schreibvorgang (Regel oben); EBZ siehe Teil 3.
  4. **Rückbau:** Gerald bzw. der Einkauf löscht oder storniert den ELL-Beleg in Sage. Danach prüft der Dev-Lauf **lesend**, dass `GGGeliefert`
     und der Platz-Bestand wieder auf dem Stand vor dem Test sind, und trägt das ein.
  5. **Eintragen:** Request (ohne Zugangsdaten), Response, Befunde, P-W9 und Rückbau kommen in einen Abschnitt „Schreibtest-Ergebnis“ dieser Spec
     (Hauptcheckout). Weicht der Payload vom Entwurf ab, legt E4 ihn danach fest; ein dafür nötiger zweiter Schreibvorgang braucht Geralds Ja.

## Akzeptanzkriterien

*Neu nummeriert 2026-11-11; die früheren AK 1-5 (Rahmenabrufe) stehen in [[2026-11-11-wareneingang-teil-3-rahmenabrufe]].*

1. (E1) Mit Receipts im Status `NichtNoetig` sind Queue-Einträge, Rest und Anzeige identisch zu Teil 1 (bestehende Tests grün).
2. (E1) Eine Einbuchung mit Receipt-Status ≠ `NichtNoetig` erzeugt **keinen** Queue-Eintrag, weder im Decorator noch über
   `EnqueueMissingAsync` (je ein Test). Bei `NichtNoetig` erzeugt sie einen Eintrag (Sage-Platz, Schalter an).
3. (E1) Rest-Regel EBB: Ein Receipt `Uebergeben` mit `SageWeAm` ≤ `SyncedAt` wird nicht abgezogen. Mit `SageWeAm` > `SyncedAt` oder
   einem anderen Status wird er abgezogen. Unit-Test mit allen Fällen. `SyncedAt` ist der Zeitpunkt vor dem SData-Lesen.
4. (E1) `SQL/143` (bzw. die bei der Umsetzung vergebene Nummer) läuft zweimal ohne Fehler (zweiter Lauf ohne Ausgabe). Bestehende Receipts
   haben danach `SageWeStatus = 0`, `SageWeVersuche = 0`, `Lieferschein = NULL`.
5. (E2) Eine eingegebene Lieferschein-Nr. steht nach dem Buchen an **jedem** Receipt dieses Absendens (getrimmt). Mehr als 20 Zeichen → Fehler,
   nichts gebucht, Eingaben bleiben stehen.
6. (E2) `WareneingangLieferscheinPflicht` aus: Buchen ohne Lieferschein-Nr. gelingt. An: leer oder nur Leerzeichen → Meldung „Bitte die
   Lieferschein-Nummer des Lieferanten eingeben.“, nichts gebucht. Gilt unabhängig von `SageWareneingangBelegAktiv`.
7. (E2) `/Settings` zeigt `WareneingangLieferscheinPflicht` in „Lager und Buchungen“ als Schalter mit Anzeigename, Wirkung, Verwendung,
   Standard „aus“ und Standort; `AppSettingDefinitionsTests` grün.
8. (E3) Die Liste „Wareneingänge an Sage“ hat Pagination, Filterkarte und Server-Spaltenfilter (auch für „Lieferschein“). Ein Benutzer mit
   `stock_read` sieht sie ohne „Erneut senden“; ein direkter POST wird abgewiesen.
9. (E3) „Erneut senden“ ist nur bei `Fehler` möglich. Die ganze Gruppe geht auf `Offen`, Modified* ist gesetzt, die Maske zeigt den Hinweis
   „Vorher in Sage nachsehen …“.
10. (E4) Mit `SageWareneingangBelegAktiv = false` erhält ein neuer Wareneingang `NichtNoetig`, jeder aktive Platz ist wie in Teil 1 wählbar, und
    es wird nichts an `StandardBelegService` gesendet.
11. (E4) Mit `true` bekommen zwei Positionen, die in einem Absenden gebucht werden, Status `Offen` und dieselbe `SageWeVorgang`. Der nächste
    Worker-Takt sendet genau einen Beleg, danach steht `Uebergeben` mit Sage-Belegnummer da. Auch nach 15 Minuten gibt es keinen
    Queue-Eintrag.
12. (E4) Zwei Absenden zur selben Bestellung ergeben zwei verschiedene `SageWeVorgang` und zwei Belege (Unit-Test am Service).
13. (E4) Der Payload-Builder setzt `EKLieferschein` = eingegebene Nummer, lässt das Feld bei leerer Nummer weg und setzt `Memo` = `WE#<ersteReceiptId>#`
    (Unit-Test).
14. (E4) Eine Chargen- bzw. nachweispflichtige Position bekommt `NurInSage`, wird nicht gesendet und hat keinen Queue-Eintrag. Die Maske
    zeigt den Hinweis.
15. (E4) Platzregel bei eingeschaltetem Setting: Ein Ziel-Platz mit `SageBuchungErlaubt = false` **oder** ohne `SageLagerkennung`/
    `SageLagerplatzId` ergibt den Zeilenfehler mit Platz-Code und Erklärung; **nichts** aus diesem Absenden wird gebucht (je ein Test für beide
    Fälle und für den erlaubten Platz).
16. (E4, Variante B) Sage-Fehler-Response ergibt `Fehler` mit Text und wird nicht automatisch neu gesendet. Ein hängendes `Gesendet` ergibt nach
    `Sync:SageLagerbuchungStuckMinutes` den Status `Fehler` „Ergebnis unklar …“ und wird nicht neu gesendet.
17. (E4) Das Aktivitäts-Protokoll zeigt den Lauf „Sage-Wareneingang“ mit deutschen Counts. Der Name steht in `SyncLogServices.All`, und
    `ISyncLogger` ist der letzte Ctor-Parameter.
18. (E4) `/ServiceSettings` zeigt den Schalter mit Anzeigename, Wirkung, Standort AKE und dem Warnhinweis.
19. (E4) Der Abschnitt „Schreibtest-Ergebnis“ ist eingetragen: genau ein ELL-Beleg am AKE-Test, Lieferschein und Platz korrekt, Position 2
    unberührt, Rückbau durch Gerald/Einkauf lesend bestätigt, P-W9 (a)/(b) mit Ergebnis.
20. (E5, nur mit belegtem P-W9, Variante A) Vor **jedem** automatischen Wiederholen läuft der Sage-Lookup: gefunden → `Uebergeben` ohne POST;
    nicht gefunden → erneut gesendet, `SageWeVersuche + 1`; Lookup wirft → kein POST, Status unverändert, Warnung im Protokoll; Versuche
    erschöpft → `Fehler` „Maximale Versuche …“. Dasselbe für hängendes `Gesendet` und für „Erneut senden“. Unit-Test je Fall.
21. (E5 nicht gebaut) Ohne belegtes P-W9 gibt es im Code kein automatisches Wiederholen (AK 16 gilt), und die Etappen-Tabelle führt E5 als
    Blocker mit Verweis auf P-W9.

## Test-Szenarien

Erweiterung von **TS-116** in `docs/TESTSZENARIEN.md`. Der Platzhalter TS-116.7 aus Teil 1 wird durch 116.10 ff. ersetzt; der Dev-Lauf
bestätigt die nächste freie Unternummer. Danach `secondbrain/tests/testszenarien-index.md` nachziehen. Die früheren Szenarien „Rahmenabrufe
lesen/buchen“ stehen in [[2026-11-11-wareneingang-teil-3-rahmenabrufe]].

**Sage-Schreibregel für alle Szenarien:** Ein Agent führt nur den einen vereinbarten Schreibtest aus (Abschnitt „Prüfschritte“). TS-116.12 und
TS-116.14 schreiben nach Sage und laufen deshalb **nur durch Gerald bzw. den Einkauf** am AKE-Test, nie durch einen Agenten.

- **TS-116.10 Ausschluss aus der Queue (nur Testsystem):**
  - Vorbedingung: Setting an (nach E4), `SageLagerbuchungAktiv` an, Sage-Platz mit „Sage-Buchung erlaubt“.
  - Wareneingang buchen: kein Eintrag in `SageBookingQueueItems`, auch nicht nach 15 Minuten (`SELECT * FROM SageBookingQueueItems WHERE StockMovementId = <id>`).
  - Gegenprobe Setting aus: Eintrag entsteht (AK 1, 2, 10).
- **TS-116.11 Lieferschein-Nr. (ohne Sage):**
  - Vorbedingung: `SageWareneingangBelegAktiv` aus.
  - Pflicht aus: ohne Nummer buchen → gebucht; mit „ LS-4711 “ buchen → an allen Receipts „LS-4711“ (`SELECT Id, Lieferschein FROM PurchaseOrderReceipts ORDER BY Id DESC`).
  - Pflicht an (`/Settings` → Lager und Buchungen): ohne Nummer → Meldung, nichts gebucht, Mengen bleiben stehen.
  - Negativ: 21 Zeichen → Fehler, nichts gebucht (AK 5-7).
- **TS-116.12 Sage-Wareneingangsbeleg (nur AKE-Test, nach dem Schreibtest, durch Gerald/Einkauf):**
  - Zwei Positionen in einem Absenden mit Lieferschein-Nr. buchen: genau ein ELL-Beleg in Sage mit dieser Lieferschein-Nr., Status `Uebergeben`
    mit Belegnummer an beiden Receipts.
  - Eine weitere Teilmenge in einem zweiten Absenden: ein zweiter Beleg.
  - Nach dem nächsten Abgleich ist der Rest korrekt, ohne doppelten Abzug.
  - Danach Rückbau in Sage wie im Schreibtest (AK 3, 11, 12).
- **TS-116.13 Platzregel und Sonderfälle (Testsystem, ohne Sage-Schreiben):**
  - Setting an; Ziel-Platz mit „Sage-Buchung erlaubt“ aus → Zeilenfehler mit Platz-Code, nichts gebucht.
  - Manueller Platz ohne Sage-Referenz → derselbe Fehler. Gegenprobe Setting aus: beide Plätze buchbar (AK 10, 15).
  - Chargenpflicht ergibt `NurInSage`, kein Senden, kein Queue-Eintrag (derzeit keine solchen Artikel; nur per Unit-Test, AK 14).
- **TS-116.14 Fehler und Wiederholen (nur AKE-Test, durch Gerald/Einkauf):**
  - SData-Kennwort auf DEV falsch → `Fehler`, in der Liste sichtbar, Versuche = 1.
  - Variante B: nach 2 Takten weiter `Fehler` (kein Auto-Retry). Hinweis vor „Erneut senden“ prüfen; „Erneut senden“ als stock_keyuser
    funktioniert, als stock_read wird es abgewiesen (AK 8, 9, 16).
  - Variante A (nur wenn E5 gebaut): Kennwort korrigieren → nächster Takt prüft in Sage, sendet, `Uebergeben`. Zweiter Fall: Beleg in Sage
    vorhanden, Receipt künstlich auf `Fehler` gesetzt (`UPDATE PurchaseOrderReceipts SET SageWeStatus = 4 WHERE SageWeVorgang = '<guid>'`)
    → Lookup findet ihn, `Uebergeben` **ohne** zweiten Beleg (AK 20).

## Etappen (nur bei epic: true)

Ein Worktree, ein Merge am Ende (Schranke 2 nach allen gebauten Etappen). Jede Etappe baut und testet grün und endet mit einem Commit. Den
Branch während der Arbeit mit `scripts/sync-worktree.ps1 -Slug <slug>` auf main-Stand halten. Die Version für das ganze Epic setzt E6, nach
1.99.1. E1-E3, E5 und E6 schreiben nicht nach Sage. E4 endet mit dem **einen** vereinbarten Schreibtest über DEV (Regel im Abschnitt
„Prüfschritte“). E5 hängt an P-W9; ist P-W9 nicht belegt, wird E5 als Blocker festgehalten und das Epic läuft mit Variante B (nur „Erneut senden“
von Hand) weiter. Gelingt der Schreibtest nicht, bleibt das Epic mit E1-E3 + E6 mergebar: der Setting-Key kommt erst mit E4 in den Katalog, E1
bleibt damit ohne Wirkung.

| # | Etappe | Abhängigkeit | SQL | Status | Commit |
|---|--------|--------------|-----|--------|--------|
| ~~–~~ | ~~Rahmenabrufe (früher E1)~~ – überholt, Freigabe-Antwort 6, siehe [[2026-11-11-wareneingang-teil-3-rahmenabrufe]] | – | – | entfällt | |
| 1 | Sage-WE-Grundlage: Receipt-Felder (7 Spalten inkl. `SageWeVersuche`, `Lieferschein`) + Migration, `SageWareneingangStatus`, Ausschluss in Decision/Decorator/`EnqueueMissingAsync`, Rest-Regel EBB, `SyncedAt` vor dem Lesen; Tests | – | 143 | erledigt | `f45412d4` |
| 2 | Lieferschein-Nr. in der WE-Maske (Kopf-Feld, Prüfung, Speichern) + AppSetting `WareneingangLieferscheinPflicht` im Katalog; Tests | E1 | – | erledigt | `aa4655f8` |
| 3 | Liste „Wareneingänge an Sage“ (Listen-View-Pattern) + „Erneut senden“ von Hand mit Hinweis + Status-Zeile in Receive | E1 | – | erledigt | `f7dc750e` |
| 4 | Sender Variante B: Setting in den Katalog, Status beim Buchen (Offen/NurInSage), Platzregel, Payload-Builder (inkl. `EKLieferschein`, `Memo`), Client, `SageWareneingangService` (hängend → „unklar“), Worker-Schritt, SyncLog-Name, DI-Test; danach Schreibtest über DEV und Eintrag „Schreibtest-Ergebnis“ | E1-E3; Schreibtest braucht DEV mit Worktree-Stand und die Test-Bestellung (Gerald/Einkauf); Fehler beim Schreibtest → Ergebnis eintragen, Gerald fragen, E4 ohne festgelegten Payload als Blocker | – | Code erledigt, **Schreibtest wartet auf Geralds OK** | `053cb28d`, `a449e814` |
| 5 | Auto-Retry Variante A: `SdataWareneingangLookupReader`, Prüfung vor jedem Wiederholen, Versuche-Grenze; Tests | **blockiert bis P-W9 (a)+(b) belegt** (nach dem Schreibtest); sonst Blocker eintragen, nicht bauen | – | blockiert | |
| 6 | Abschluss: beide `AppVersion.cs` (nach 1.99.1), Anwender-Changelog, Hilfe (Sage-WE, Lieferschein, Platzregel, entfallende Arbeitsanweisung), README AppSettings + ServiceSettings, `scripts/deploy/nachlauf.psd1`, TS-116.10 ff. | E1-E4 (+E5, falls gebaut) | – | offen | |

Brain-Update (Hauptcheckout): [[feature-map]], [[datenmodell]] (Receipt-Spalten), [[services]] (Lauf „Sage-Wareneingang“, Gate),
[[integrationen]] (StandardBelegService genutzt), [[sage-sdata-commonwawiservices]] Z. 12 („Im WMS genutzt“ ergänzen, nach E4),
[[controller]] (Liste + Rechte), [[glossar]] („Sage-Wareneingangsbeleg (ELL)“, „Lieferschein-Nr. (Lieferant)“), [[fallstricke]]:
- ~~EBZ-Vorgangsmengen gelten je Rahmen, nicht je Abruf.~~ – *überholt, gehört zu Teil 3.*
- Eine Ausschlussregel muss in Decorator **und** `EnqueueMissingAsync` stehen.
- Rest darf übergebene Wareneingänge nicht doppelt abziehen.
- Wiederholen nach Sage-Schreibfehlern nur mit belegter Korrelation (Lookup vor jedem Senden), sonst von Hand.

Jeweils mit Warum. Changelog-Notiz und Testindex.

## Deploy

*Vorläufig (Spec-Agent). Der qa-agent bestätigt gegen den echten Diff.*

- **Web-App:** ja (Receive mit Lieferschein und Platzregel, Liste, Decision/Decorator, Repository, Settings-Kataloge).
- **Service:** ja (`SyncedAt` im Abgleich, `EnqueueMissingAsync` läuft im Dienst, Worker-Schritt Sage-WE, ggf. Lookup E5).
- **Migration:** ja, E1 (`SQL/143`, Nummer bei Umsetzung prüfen), additiv.
- **Reihenfolge Testsystem:**
  1. DB-Backup (Standard).
  2. SQL-Skript.
  3. Web publizieren.
  4. Service publizieren.
  5. Einen Abgleich laufen lassen.
  6. `SageWareneingangBelegAktiv` erst nach dokumentiertem Schreibtest und nur am AKE-Testsystem einschalten.
  7. Den Einkauf informieren, dass übergebene Positionen nicht mehr von Hand geschlossen werden.
- **Sage-Schreibtest im Dev-Lauf:** genau ein Test-Beleg über DEV gegen `sagetest01`/`ake_TEST2026` (Freigabe-Antwort 10), Ablauf und Regel
  im Abschnitt „Prüfschritte“. Kein weiterer Sage-Schreibvorgang durch Agenten; Rückbau durch Gerald bzw. den Einkauf.
- **Manifest-Einträge `scripts/deploy/nachlauf.psd1` (ADR 0022/0023), Version = Epic-Version aus E6:**
  - Schema, im Block „Schema“ direkt **nach** `SQL/142` (Deploy-Reihenfolge):
    `@{ Id='SQL/143'; Version='<Version>'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'; Datei='143_AddPurchaseOrderReceiptSageWe.sql'; MigrationId='<Zeitstempel>_AddPurchaseOrderReceiptSageWe'; Voraussetzung='additiv'; Quelle='<changelog-slug>' }`
  - Einstellung, Block „Nachher“:
    `@{ Id='v<Version>-SageWe'; Version='<Version>'; Typ='Einstellung'; Firma='AKE'; Phase='Nachher'; Ausfuehrung='anzeigen'; Text='SageWareneingangBelegAktiv (/ServiceSettings) erst nach dokumentiertem Schreibtest und Abnahme TS-116.12 einschalten; IDEAL bleibt aus (kein SData-Endpunkt). Danach sind nur Sage-Plaetze mit Sage-Buchung erlaubt als Ziel buchbar.'; Quelle='<changelog-slug>' }`
  - Einstellung, Block „Nachher“:
    `@{ Id='v<Version>-Lieferschein'; Version='<Version>'; Typ='Einstellung'; Firma='AKE'; Phase='Nachher'; Ausfuehrung='anzeigen'; Text='WareneingangLieferscheinPflicht (/Settings > Lager und Buchungen) nach Absprache mit Lager und Einkauf setzen (Standard aus = Lieferschein-Nr. optional).'; Quelle='<changelog-slug>' }`
  - Info, Block „Nachher“:
    `@{ Id='v<Version>-EinkaufWe'; Version='<Version>'; Typ='Info'; Firma='AKE'; Phase='Nachher'; Ausfuehrung='anzeigen'; Text='Einkauf informieren: Wareneingaenge mit Status An Sage uebergeben nicht mehr von Hand schliessen; Liste Wareneingaenge an Sage; Fehler nach Pruefung in Sage erneut senden.'; Quelle='<changelog-slug>' }`
  - Texte nur ASCII (`nachlauf.psd1:2`).
  - **Keine** Pruefung, kein Daten-/Einmal-Skript: das ServiceSetting legt der Web-Start an (`Program.cs:309-324`), das AppSetting braucht keine
    Zeile (`seed: false`). Der Drift-Test `NachlaufManifestDriftTests` muss mit `SQL/143` grün sein.
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

1. SQL-Nummer 114 oder 142? → **Empfehlung: die nächste freie Nummer bei der Umsetzung, derzeit 143, weil 142 seit v1.99.0 mit `SQL/142_AddDeployScriptRuns` belegt ist (Gerald 2026-11-11).**
2. Ein Sage-Beleg je Absenden oder je Position? → **Je Absenden: ein Sage-WE-Beleg je WE-Buchung (Gerald 2026-11-11).**
3. Ziel-Platz ohne Sage-Bezug: sperren oder ohne Sage-WE buchen? → **Buchbar sind nur Ziel-Plätze, die in Sage vorhanden sind und bei denen die Sage-Buchung aktiv ist. Alle anderen werden bei eingeschaltetem Sage-WE gesperrt, mit Hinweistext (Gerald 2026-11-11).**
4. Kein Auto-Retry, „Erneut senden“ von Hand mit RequireStockKeyUserAccess? → **Abweichend von der Empfehlung: Der Dienst soll selbst erneut versuchen, aber vor jedem Wiederholen automatisch in Sage prüfen, ob der Beleg schon gebucht ist (Check, keine Doppelbuchung). Voraussetzung ist eine belastbare Korrelation (P-W9, Memo/Referenz am Sage-Beleg). Solange P-W9 sie nicht belegt, gibt es keinen Auto-Retry, dann gilt „erneut senden“ von Hand (Gerald 2026-11-11).**
5. Lieferschein-Nummer des Lieferanten erfassen? → **Ja: Lieferscheinnummer des Lieferanten in der WE-Maske eingeben lassen und mitsenden (`EKLieferschein`). Abweichend von der Empfehlung (Gerald 2026-11-11). Pflichtfeld per Einstellung wählbar (Standard: nicht Pflicht; Einstellung mit Auswahlliste und Beschreibung nach ADR 0020) (Gerald 2026-11-11).**
6. Sage-WE auch für Rahmenabrufe? → **Entfällt: Rahmenabrufe (F1) kommen aus dieser Spec heraus und werden ein eigener nächster Schritt (Gerald 2026-11-11).**
7. Abrufmenge: MengeBasis oder Menge? → **Entfällt mit 6 (Rahmenabrufe später).**
8. Abrufe mit Rest 0 ausblenden; Grenze „in Sage direkt gebucht“ akzeptiert? → **Entfällt mit 6 (Rahmenabrufe später).**
9. Charge/Seriennummer: NurInSage, kein WE, kein Queue-Zugang? → **Chargen-/seriennummernpflichtige Artikel gibt es derzeit nicht (Gerald 2026-11-11). Die Regel bleibt als Absicherung wie empfohlen (NurInSage).**
10. Schreibtest-Freigabe, Ausführender, Rückbau? → **Freigegeben: Schreibtest über DEV gegen den Sage-AKE-Test (sagetest01, ake_TEST2026); Rückbau des Test-Belegs in Sage durch Gerald bzw. den Einkauf (Gerald 2026-11-11).**
