---
type: spec
title: "Wareneingang Teil 2: Sage-Wareneingangsbeleg (ELL)"
slug: 2026-11-08-wareneingang-teil-2-sage-we-beleg-rahmenabrufe-spec
status: Testbereit
created: 2026-11-08
updated: 2026-11-12
freigabe_von: "Gerald Weichbold (Chat Koordination, 2026-11-11 20:40 „beide freigeben“)"
freigabe_am: 2026-11-11
source_backlog: "[[2026-11-08-wareneingang-teil-2-sage-we-beleg-rahmenabrufe]]"
depends_on: "[[2026-11-08-wareneingang-aus-sage-bestellungen-spec]]"
folge_backlog: "[[2026-11-11-wareneingang-teil-3-rahmenabrufe]]"
version: "1.99.9"
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
| 4 | Sender Variante B: Setting in den Katalog, Status beim Buchen (Offen/NurInSage), Platzregel, Payload-Builder (inkl. `EKLieferschein`, `Memo`), Client, `SageWareneingangService` (hängend → „unklar“), Worker-Schritt, SyncLog-Name, DI-Test; danach Schreibtest über DEV und Eintrag „Schreibtest-Ergebnis“ | E1-E3; Schreibtest braucht DEV mit Worktree-Stand und die Test-Bestellung (Gerald/Einkauf); Fehler beim Schreibtest → Ergebnis eintragen, Gerald fragen, E4 ohne festgelegten Payload als Blocker | – | erledigt, Schreibtest 2026-11-12 erfolgreich (Beleg 2027-398675) | `053cb28d`, `a449e814`, `0d265005` |
| 5 | Auto-Retry Variante A: `SdataWareneingangLookupReader`, Prüfung vor jedem Wiederholen, Versuche-Grenze; Tests | **blockiert bis P-W9 (a)+(b) belegt** (nach dem Schreibtest); sonst Blocker eintragen, nicht bauen | – | **blockiert** (P-W9 (a)/(b) offen: Mensch; Agenten lesen weder AKESQL20 noch SData mit Zugangsdaten) | |
| 6 | Abschluss: beide `AppVersion.cs` (nach 1.99.1), Anwender-Changelog, Hilfe (Sage-WE, Lieferschein, Platzregel, entfallende Arbeitsanweisung), README AppSettings + ServiceSettings, `scripts/deploy/nachlauf.psd1`, TS-116.10 ff. | E1-E4 (+E5, falls gebaut) | – | erledigt (v1.99.4) | `2d09489a` |

Brain-Update (Hauptcheckout): [[feature-map]], [[datenmodell]] (Receipt-Spalten), [[services]] (Lauf „Sage-Wareneingang“, Gate),
[[integrationen]] (StandardBelegService genutzt), [[sage-sdata-commonwawiservices]] Z. 12 („Im WMS genutzt“ ergänzen, nach E4),
[[controller]] (Liste + Rechte), [[glossar]] („Sage-Wareneingangsbeleg (ELL)“, „Lieferschein-Nr. (Lieferant)“), [[fallstricke]]:
- ~~EBZ-Vorgangsmengen gelten je Rahmen, nicht je Abruf.~~ – *überholt, gehört zu Teil 3.*
- Eine Ausschlussregel muss in Decorator **und** `EnqueueMissingAsync` stehen.
- Rest darf übergebene Wareneingänge nicht doppelt abziehen.
- Wiederholen nach Sage-Schreibfehlern nur mit belegter Korrelation (Lookup vor jedem Senden), sonst von Hand.

Jeweils mit Warum. Changelog-Notiz und Testindex.

## Deploy

**Stand v1.99.9 (qa-agent 2026-11-12, echter Diff `bb788f0e..4c1b5523`, Nachtrag System-Kürzel):** `IDEALAKEWMSService/` geändert (Worker,
`SageWareneingangService`, Payload-Builder, Lookup-Reader) → **Dienst ja**; `IdealAkeWms/` geändert (Modelle, DbContext, Repository, Katalog,
Validator, `ServiceSettingsController`, Liste, Hilfe, Changelog) → **Web ja**; neue Datei `IdealAkeWms/Migrations/20261112141208_AddSageKennungen.cs`
→ **Migration ja** (`SQL/144_AddSageKennungen.sql`, additiv, nicht daten-destruktiv). Web und Dienst **gemeinsam** deployen (der Dienst schreibt die
neuen Spalten). Nachlauf in `scripts/deploy/nachlauf.psd1`: `SQL/144` (automatisch, nach `SQL/143`) und `v1.99.9-SystemKuerzel` (anzeigen: Kürzel je
Testsystem setzen – DEV, AT, ATI, IT –, Produktion **leer** lassen). Reihenfolge: DB-Backup, Update-Skript (führt `SQL/144` aus), Web, Dienst,
danach am Testsystem das Kürzel in /ServiceSettings setzen, **bevor** `SageWareneingangBelegAktiv`/`SageLagerbuchungAktiv` dort an sind.
Publish: `C:\git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish` (**Version 1.99.9**, Commit `4c1b5523`, beide Komponenten,
`SQL\` + `deploy\`, `publish-info.txt`). Nach dem Merge nur dann aus main neu publizieren, wenn der Merge getestete Dateien mit parallelen
main-Änderungen zusammengeführt hat.

**Stand v1.99.7 (qa-agent 2026-11-12, echter Diff `67322b85..af0a75f2` + QA-Korrektur `bb788f0e`):** `IDEALAKEWMSService/` geändert
(`SageWareneingangService`, neu `SdataWareneingangLookupReader`, `SageBookingWorker`, `Program.cs`) → **Dienst ja**; `IdealAkeWms/` geändert (Controller,
Repository, Enum `Klaeren = 6`, `ServiceSettingDefinitions`, Views, Hilfe, Changelog) → **Web ja**; keine Datei unter `*/Migrations/` → **keine neue
Migration** (`SageWeStatus` ist int). Nachlauf: `v1.99.7-SageWeWiederholen` (Einstellungen bestätigen) und `v1.99.7-EinkaufKlaeren` (Info) in
`scripts/deploy/nachlauf.psd1`; die zwei ServiceSettings legt der Web-Start an (auf DEV nach dem Deploy mit 3/15 vorhanden). Publish:
`C:\git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish` (**Version 1.99.8**, Commit `bb788f0e`, enthält auch v1.99.8
Kommissionierung Matchcode; beide Komponenten, `SQL\` + `deploy\`). Reihenfolge: Backup, Update-Skript (führt `SQL/143` aus, falls offen), Web,
Dienst, Nachlauf-Hinweise. Liegen beim Deploy offene/fehlerhafte Sage-WE bei ausgeschaltetem Schalter, stehen sie nach dem ersten Takt auf „Klären“
(Q-2, gewollt). Nach dem Merge nur dann aus main neu publizieren, wenn der Merge getestete Dateien mit parallelen main-Änderungen zusammengeführt hat.

**Stand v1.99.6 (qa-agent 2026-11-12, echter Diff `c155b1b2..67322b85`):** `IDEALAKEWMSService/` geändert (`PositionsMemo` mit Eingabe) → **Dienst ja**;
`IdealAkeWms/` geändert (Receive-Feld, Validierung, Liste, Version, Changelog, Hilfe) → **Web ja**; keine Datei unter `*/Migrations/` → **keine neue
Migration** (`StockMovement.Note` nvarchar(500) besteht seit `SQL/57`). Kein Nachlauf-Eintrag nötig (kein SQL, keine Einstellung, kein manueller Schritt).
Publish: `C:\git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish` (Version 1.99.6, Commit `67322b85`, beide Komponenten,
`SQL\` + `deploy\`). Reihenfolge unverändert wie unten (Backup, Update-Skript mit `SQL/143` falls offen, Web, Dienst). Nach dem Merge nur dann aus
main neu publizieren, wenn der Merge getestete Dateien mit parallelen main-Änderungen zusammengeführt hat.

**Stand v1.99.5 (qa-agent 2026-11-12, echter Diff `bd9b58d0..c155b1b2`):** `IDEALAKEWMSService/` geändert (Payload `Artikelmemo`) → **Dienst ja**;
`IdealAkeWms/` nur `AppVersion.cs` + `Changelog.cshtml` → **Web ja** (Versionsgleichheit, Changelog); keine Datei unter `*/Migrations/` → **keine
neue Migration** (`SQL/143` aus v1.99.4 bleibt Teil des Pakets, falls das Zielsystem noch vor v1.99.4 steht). Kein Nachlauf-Eintrag nötig.
Publish: `C:\git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish` (Version 1.99.5, Commit `c155b1b2`, beide Komponenten,
`SQL\` + `deploy\`). Reihenfolge unverändert wie unten (Backup, Update-Skript mit `SQL/143` falls offen, Web, Dienst). Nach dem Merge nur dann aus
main neu publizieren, wenn der Merge getestete Dateien mit parallelen main-Änderungen zusammengeführt hat.

*Vom qa-agent 2026-11-12 gegen den echten Diff `d2d4ed46..152ab6f1` bestätigt (Abschnitt „QA-Nachweis“ unten): `IdealAkeWms/` geändert → Web ja;
`IDEALAKEWMSService/` geändert → Dienst ja; neue Datei `IdealAkeWms/Migrations/20261111200627_AddPurchaseOrderReceiptSageWe.cs` → Migration ja.
Publish liegt fertig in `C:\git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish` (Version 1.99.4, Commit `152ab6f1`, beide
Komponenten, `SQL\` + `deploy\` dabei). Nach dem Merge nur dann aus main neu publizieren, wenn der Merge getestete Dateien mit parallelen
main-Änderungen zusammengeführt hat.*

- **Web-App:** ja (Receive mit Lieferschein und Platzregel, Liste, Decision/Decorator, Repository, Settings-Kataloge).
- **Service:** ja (`SyncedAt` im Abgleich, `EnqueueMissingAsync` läuft im Dienst, Worker-Schritt Sage-WE, Client `SageWareneingangClient`). E5 nicht gebaut.
- **Migration:** ja, `SQL/143_AddPurchaseOrderReceiptSageWe.sql` / `20261111200627_AddPurchaseOrderReceiptSageWe`, additiv, nicht daten-destruktiv.
- **Paket:** `publish` im Worktree (v1.99.4, Commit `bd9b58d0`; Binärstand = `152ab6f1`, danach nur `nachlauf.psd1` geändert und nach
  `publish\deploy` nachkopiert). Auf dem Zielserver `deploy\Update-IdealAkeWms.ps1` starten; es führt `SQL/143` laut Manifest aus.
- **Reihenfolge Testsystem:**
  1. DB-Backup (Standard).
  2. SQL-Skript `SQL/143` (automatisch über das Update-Skript).
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
    `@{ Id='v<Version>-SageWe'; Version='<Version>'; Typ='Einstellung'; Firma='AKE'; Phase='Nachher'; Ausfuehrung='anzeigen'; Text='SageWareneingangBelegAktiv (/ServiceSettings) erst nach dokumentiertem Schreibtest und Abnahme TS-116.14 einschalten; IDEAL bleibt aus (kein SData-Endpunkt). Danach sind nur Sage-Plaetze mit Sage-Buchung erlaubt als Ziel buchbar.'; Quelle='<changelog-slug>' }`
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

## Umsetzung (goal-Lauf 2026-11-11/12)

Buendel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Version **1.99.4**, SQL **143** (beim Start geprueft). Commits: E1 `f45412d4`,
E2 `aa4655f8`, E3 `f7dc750e`, E4 `053cb28d` + `a449e814` (Belegjahr nicht senden) + `0d265005` (Schreibtest-Fix), E6 `2d09489a`. E5 nicht gebaut.

- **E1:** 7 Spalten + 2 Indizes (`SQL/143_AddPurchaseOrderReceiptSageWe.sql`, Migration `20261111200627_AddPurchaseOrderReceiptSageWe`, FreshInstall an
  beiden Stellen; zweimal gelaufen auf einer Leer-DB, zweiter Lauf ohne Ausgabe, Altbestand 0/0/NULL). Enum `SageWareneingangStatus`. Ausschluss:
  `SageBookingEnqueueDecision.ShouldEnqueue(..., hatSageWareneingang)`, Decorator liest den mitgespeicherten Receipt, `EnqueueMissingAsync` eigene
  EF-Bedingung. Rest-Regel an EINER Stelle: `ErfasstJeBelPos.Abzuziehen(SyncedAt)` (eine Abfrage je Maske); `SyncedAt` vor dem SData-Lesen.
- **E2:** Feld „Lieferschein-Nr. (Lieferant)“ im Kopf, Post-Parameter `lieferschein`, Pruefung (>20 / Pflicht), an jedem Receipt; AppSetting
  `WareneingangLieferscheinPflicht` (Katalog `B(...)`, abhaengig von `WareneingangBestellungAktiv`, Drift-Zaehler 55 -> 56).
- **E3:** `PurchaseOrdersController.SageWareneingaenge` (Listen-View-Pattern, Filterkarte Status/Bestellung/Zeitraum, Server-Spaltenfilter, Register
  `PurchaseOrders/SageWareneingaenge`) + `SageWeErneutSenden` (`RequireStockKeyUserAccess`, ganze Gruppe Fehler -> Offen, Fehlertext leer,
  Versuche bleiben); Statuszeile „Sage-Wareneingang: n× …“ in der Maske; Menue Lager -> „Wareneingänge an Sage“.
- **E4 (Variante B):** `SageWareneingang` (Web: Schalter-Key, Platzregel, Memo), Status beim Buchen (`Offen` mit EINEM `SageWeVorgang` je Absenden,
  `NurInSage` bei Charge/Nachweis), Platzregel als Zeilenfehler; Dienst: `SageWareneingangPayloadBuilder`, `SageWareneingangClient`
  (`$service/StandardBelegService`), `SageWareneingangService` (Status vor dem POST auf Gesendet + Versuch, Erfolg -> Uebergeben + Belegnummer,
  Fehler bleibt, haengend -> Fehler „Ergebnis unklar“, kein Protokoll-Lauf ohne Arbeit), eigener Schritt im `SageBookingWorker` nur ueber
  `SageWareneingangBelegAktiv`; SyncLog `SageWareneingang` / „Sage-Wareneingang“; ServiceSetting im Katalog (Anzeigename, Verwendung, Standort, Warnhinweis).
- **E6:** Version 1.99.4, Changelog-Karte, Hilfe (Abschnitt Wareneingang), README (AppSetting + Sage-Abschnitt), `nachlauf.psd1` (`SQL/143`,
  `v1.99.4-SageWe`, `v1.99.4-Lieferschein`, `v1.99.4-EinkaufWe`), TS-116.11–116.17.

**Nachweis:** `dotnet build IdealAkeWms.slnx` 0 Fehler; `dotnet test` Web **2639/2640** (1 uebersprungen, Bestand), Dienst **522/522**; neue Tests
`SageWareneingangGrundlageTests` (AK 1-3), `PurchaseOrdersReceiveTests` (AK 5-7, 10, 11, 14, 15), `SageWareneingaengeListeTests` (AK 8, 9),
`SageWareneingangServiceTests` (AK 11-13, 16, 17, echte Sage-Antwort). Bewusst geaenderte Bestandstests: `PurchaseOrdersReceiveTests`/
`PurchaseOrdersControllerTests` (neuer Ctor-Parameter `IServiceSettingRepository`, `GetErfasstJeBelPosAsync` liefert `ErfasstJeBelPos`),
`AppSettingDefinitionsTests` (Zaehler 56), `NachlaufManifestDriftTests` (`SQL/143` in der Reihenfolge).

## Schreibtest-Ergebnis (2026-11-12, freigegeben von Gerald im Chat nach Vorlage von Beleg und Aufruf)

- **Vorbereitung (lesend):** Bestellung **2027-168715** (BelId 362313, Lieferant K30601 Gasperlmair Glas), zwei Positionen: 404456 EK001640
  (4 Stk) und 404457 EK001641 (2 Stk), beide Platz `GL;0;0;0` (PlatzId 2872, WMS-Platz 2035 mit „Sage-Buchung erlaubt“), keine Charge, Einheit Stk.
  Auswahl aus dem DEV-Cache (Stand Abgleich 2026-11-11 20:47).
- **Buchen:** AKE-DEV mit Worktree-Stand (Dienst gestoppt), `SageWareneingangBelegAktiv` an, ueber die echte Maske (HTTP mit Windows-Anmeldung):
  nur Pos. 404456, **1 Stk**, Lieferschein `WMS-TEST-1` → Receipt #4 `Offen`, kein Queue-Eintrag. Request vorab mit dem echten Builder erzeugt und
  Gerald gezeigt; `Belegjahr` vorher entfernt (optional; Bestellnummer 2027-… = Geschaeftsjahr ≠ Kalenderjahr).
- **Request** (`POST https://sagetest01.ake.at:5493/sdata/ol/CommonWawiServices/ake_TEST2026;1/$service/StandardBelegService`, ohne Zugangsdaten):
  `{"Erfassungsart":4100,"BelegTransformArt":1,"IsBelegTransform":true,"BelegKennzeichen":"ELL","TransformSettings":[{"SourceBelegHandle":362313}],
  "Belegdatum":"2026-11-11","A0Konto":"K30601","InteressentAdresse":false,"EKLieferschein":"WMS-TEST-1","Memo":"WE#4#","MemoSpecified":true,
  "StandardBelegPositionen":[{"PositionHandle":404456,"Positionstyp":1,"Artikelnummer":"EK001640","AuspraegungsHandle":0,"Mengenart":1,"MengeBasis":1.0,
  "BelegPositionLagermengen":[{"PlatzId":2872,"Kurzbezeichnung":"GL;0;0;0","MengeBasis":1.0}]}]}`
- **Senden:** genau ein Versuch (08:28:51), danach Schalter sofort wieder aus. **Response HTTP 200 als SData-Atom-XML** (nicht JSON):
  `Handle 363225`, `VorgangHandle 66927`, **`BelegnummerFormatiert 2027-398675`**, `ResultNumber 200`, `ResultText Ok`, `ErrorText` leer; eine
  Position `Handle 406344`, `VorgangPositionHandle 246324`.
- **Befund 1 (behoben `0d265005`):** Der Client las nur JSON und setzte den Wareneingang auf `Fehler` („nicht lesbar“), obwohl Sage den Beleg angelegt
  hatte. Es wurde nichts wiederholt (Variante B). Fix: Atom-XML und JSON lesen (erster `ErrorText`/`BelegnummerFormatiert` = Belegkopf);
  `ResultNumber 200` = Ok. Die echte Antwort ist Testfall (`SageWareneingangServiceTests.Antwort_SdataAtomXml_Schreibtest_…`). Receipt #4 auf DEV
  danach von Hand auf `Uebergeben`/2027-398675 gesetzt (Audit „Dev-Lauf: Korrektur nach Schreibtest“).
- **Befund 2 (behoben `0d265005`):** Warnung ins Aktivitaets-Protokoll > 1000 Zeichen (`SyncLogs.Message`) → Protokoll-Eintrag scheiterte; Text gekappt.
- **Ergebnis in Sage (lesend ueber den WMS-Abgleich „Offene Bestellungen“, 2026-11-12 08:45):** Pos. 404456 geliefert 0 → **1**, offen 4 → **3**;
  Pos. 404457 unveraendert offen 2 (nicht uebernommen). Maske: Rest Pos. 1 = **3** (Uebergeben mit `SageWeAm` ≤ `SyncedAt` nicht doppelt abgezogen),
  Statuszeile „1× an Sage übergeben“; Liste zeigt 2027-398675, Memo `WE#4#`, `WMS-TEST-1`. Kein Eintrag in `SageBookingQueueItems`.
- **Payload festgelegt:** so wie oben (`PositionHandle` = BelPosId, ohne `VorgangPositionHandle`, `Kurzbezeichnung` = Lagerkennung) – Sage hat genau
  die gelistete Position mit der Menge auf dem gesendeten Platz uebernommen.
- **Offen fuer Gerald (lesend in Sage):** Lieferschein `WMS-TEST-1` und Platz-Bestand am Beleg 2027-398675 sichten; **P-W9 (a)** Sage-SQL (Memo am
  ELL-Beleg) und **(b)** SData-Lookup `EinkaufBelegeInfo … where <Memo> like '%WE#4#%'` – Agenten duerfen AKESQL20 nicht und SData nicht mit
  Zugangsdaten von Hand abfragen. **Rueckbau** des Belegs 2027-398675 durch Gerald bzw. den Einkauf; danach Abgleich, Pos. 404456 wieder offen 4.
- **E5 bleibt blockiert** bis P-W9 (a)+(b) belegt.

## QA-Nachweis (qa-agent, 2026-11-12)

Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Branch `feature/2026-08-07-ideal-teile-1-5`. Geprüfter Bereich `d2d4ed46..2d09489a`
(E1 `f45412d4`, E2 `aa4655f8`, E3 `f7dc750e`, E4 `053cb28d`/`a449e814`/`0d265005`, E6 `2d09489a`) plus die QA-Fixes `152ab6f1` und `bd9b58d0`.
Review selbst in diesem Lauf durchgeführt (kein Unteragent).

### 1. Build und Tests
- `dotnet build IdealAkeWms.slnx`: 0 Fehler (28 Warnungen, Bestand).
- `dotnet test` (vor und nach den QA-Fixes gleich):
  - `IdealAkeWms.Tests`: 2639 bestanden, 1 übersprungen (Bestand), 0 Fehler, gesamt 2640.
  - `IDEALAKEWMSService.Tests`: 522 bestanden, 0 übersprungen, 0 Fehler.
- `NachlaufManifestDriftTests`: 14/14 grün, auch nach `bd9b58d0`.

### 2. Code-Review: Befunde und Behandlung
| # | Schwere | Fundstelle | Befund | Behandlung |
|---|---|---|---|---|
| Q-1 | mittel | `IDEALAKEWMSService/Services/SageWareneingangClient.cs` `Auswerten` (vorher :87-97) | **Stiller Fallback:** Eine HTTP-2xx-Antwort ohne `BelegnummerFormatiert` und ohne `ErrorText` galt im JSON-Zweig als Erfolg. Dasselbe galt für einen leeren Body (`"{}"`) und im XML-Zweig für ein leeres `<ErrorText/>` ohne Belegnummer. Folge: `Uebergeben` ohne Belegnummer. Die Rest-Regel hätte den Wareneingang ab dem nächsten Abgleich nicht mehr abgezogen, obwohl unbekannt ist, ob Sage ihn hat. Zudem werteten der XML- und der JSON-Zweig nach verschiedenen Regeln aus. | **Behoben `152ab6f1`:** eine gemeinsame Regel `Ergebnis(...)`. Erfolg nur, wenn `ErrorText` leer ist **und** eine Belegnummer vorliegt. Sonst `Fehler` mit „Antwort von Sage ohne Belegnummer – Ergebnis unklar, bitte in Sage prüfen.“ (Variante B, ein Mensch prüft). Testfälle in `Antwort_Auswerten_…` ergänzt. Die echte Schreibtest-Antwort bleibt ein Erfolg. |
| Q-2 | niedrig | `IDEALAKEWMSService/Workers/SageBookingWorker.cs:66-70`; `PurchaseOrdersController.SageWe.cs` (`SageWeStatusText`) | Wird `SageWareneingangBelegAktiv` ausgeschaltet, bleiben gebuchte `Offen`-Wareneingänge dauerhaft `Offen`. Sie werden weder gesendet noch in die Queue gestellt, und die Liste zeigt „offen (wird gesendet)“. Das ist spec-konform („Entscheidung beim Buchen“, F2), wird aber nicht gemeldet. | Offen, Entscheidung Gerald. Vorschlag: Satz in der Hilfe oder Listenhinweis „Schalter aus – offene werden erst nach Wiedereinschalten gesendet“. Kein Datenrisiko. |
| Q-3 | niedrig | `SageBookingWorker.cs:64-70` | Der Sage-WE-Schritt steht im selben `try` wie `ProcessOnceAsync`. Wirft die Lagerbuchung, entfällt der WE-Schritt für diesen Takt. | Akzeptiert (nur ein Takt Verzögerung, kein Datenrisiko). |
| Q-4 | niedrig | `SageWareneingangService.cs` (Payload-Fehlerzweig) | Bei einem Payload-Fehler (z. B. hat der Platz zwischen Buchen und Senden den Sage-Bezug verloren) setzt der Dienst `Fehler` und zählt `fehler`. Eine Warnung im Aktivitäts-Protokoll gibt es nicht. Der Text steht an der Zeile in der Liste. | Akzeptiert (in der Liste sichtbar). |
| Q-5 | niedrig | `scripts/deploy/nachlauf.psd1` Eintrag `v1.99.4-SageWe` | Der Text verwies auf „Abnahme TS-116.12“. In `docs/TESTSZENARIEN.md` ist TS-116.12 aber der Lieferschein-Test; die Sage-WE-Abnahme ist TS-116.14. Der Abschnitt „Test-Szenarien“ dieser Spec nummeriert noch 116.10–116.14. Maßgeblich ist `docs/TESTSZENARIEN.md` mit 116.11–116.17, weil 116.10 seit v1.95.0 vergeben ist. | **Behoben `bd9b58d0`** (Manifest) und im Deploy-Abschnitt oben. Die Nummern im Abschnitt „Test-Szenarien“ der Spec bleiben als Entwurfsstand. |
| Q-6 | Hinweis | Rest-Regel F5, `ErfasstJeBelPos.Abzuziehen` | `NurInSage`-Wareneingänge werden immer abgezogen, auch nachdem der Einkauf sie in Sage gebucht hat. Dann würde doppelt abgezogen. Das ist spec-konform (F5: „alle außer Uebergeben ≤ SyncedAt“). Solche Artikel gibt es derzeit nicht (Freigabe-Antwort 9). | Vermerkt für Teil 3 bzw. für den Fall, dass Chargenartikel dazukommen. |
| Q-7 | Hinweis (Brain) | Frontmatter dieser Spec | `freigabe_von`/`freigabe_am` standen doppelt (zweites Mal leer). Doppelte YAML-Schlüssel machen das Frontmatter ungültig. | Leere Dubletten entfernt. |
| Q-8 | Hinweis (DEV) | `D:\Dev\scripts\dev-absicherung-ake.sql` / `-ideal.sql` | Die DEV-Absicherung setzt den neuen schreibenden Schalter `SageWareneingangBelegAktiv` nicht zwangsweise auf `false`. Nach dem Deploy steht er auf beiden DEV-DBs auf `false`. | Nicht geändert (außerhalb des Repos). Empfehlung an Gerald: in die `@k`-Liste der Absicherung aufnehmen. |

Ohne Befund geprüft:
- **ADR 0004:** Model → Migration → `SQL/143`. Die Guards nutzen `COL_LENGTH` bzw. `sys.indexes`, die DDL steht in eigenen Batches, die History ebenfalls in einem eigenen Batch, und die Constraint-Namen sind gleich. `00_FreshInstall.sql` ist an beiden Stellen nachgezogen: Spalten und Indizes in der Tabellendefinition, die `MigrationId` mit `COL_LENGTH`-Bedingung. Kein `HasDefaultValue`. `SQL/AgentJobs` ist nicht betroffen.
- **Audit-Felder:** Beim Insert werden `Created*` gesetzt. Statuswechsel im Dienst laufen über `Setze()` mit `Modified*` = `system:sage-wareneingang`. „Erneut senden“ setzt `Modified*` aus `ICurrentUserService`; auf DEV gemessen.
- **Queue-Ausschluss an beiden Stellen:**
  - `SageBookingEnqueueDecision.ShouldEnqueue(…, hatSageWareneingang)` mit dem Decorator `SageBookingEnqueueingStockMovementRepository.cs:66-68`.
  - Eigene EF-Bedingung in `EnqueueMissingAsync` (`SageBookingQueueRepository.cs:152-153`) über die Navigation `StockMovement.PurchaseOrderReceipt` (`StockMovement.cs:59`).
- **Rest-Regel an einer Stelle:** `ErfasstJeBelPos.Abzuziehen(SyncedAt)` holt die Daten in einer Abfrage ohne N+1. `SyncedAt` wird vor dem Lesen genommen (`EinkaufBestellungenSyncService.cs:61-62`). `Rest` liest nur `ImWmsAbzuziehen`.
- **Listen-View-Pattern:**
  - `PageSize.Resolve`, `PaginationState` und `_Pagination`, ohne Take-Cap.
  - Filterkarte mit Status, Bestellung und Zeitraum.
  - `data-server-column-filter="true"`, jede `<th>` mit `data-col-key`.
  - Typ-Register `PurchaseOrders/SageWareneingaenge` in `ColumnFilterSyntax.cs`.
- **Rechte:** Auf Klassenebene `RequireStockReadAccess` und das Feature-Gate. `SageWeErneutSenden` trägt `[RequireStockKeyUserAccess]`, `[HttpPost]` und `[ValidateAntiForgeryToken]`. Die Sichtbarkeit des Knopfs (`CanTransferStockAsync`) entspricht genau dem Filter.
- **ADR 0020:**
  - `SageWareneingangBelegAktiv`: Bool-Schalter mit Anzeigename, Verwendung, Standort und Warnhinweis.
  - `WareneingangLieferscheinPflicht`: `B(...)`, `seed: false`, abhängig von `WareneingangBestellungAktiv`.
  - `Sync:SageLagerbuchungStuckMinutes`: nur der Verwendungstext ist ergänzt.
  - Server-Validierung über den Katalog (Bool).
- **Aktivitäts-Protokoll:** `ISyncLogger` ist der letzte Ctor-Parameter (Test). `SyncLogServices.All` und `DisplayNames` sind ergänzt. Ein Lauf ohne Arbeit schreibt keinen Protokoll-Eintrag.
- **Idempotenz:**
  - Vor dem POST wird der Status auf `Gesendet` gesetzt, der Versuchszähler um 1 erhöht und gespeichert (`SaveChanges`).
  - Ein hängendes `Gesendet` wird zu `Fehler` „Ergebnis unklar“, ohne neues Senden.
  - Der Client fängt alle Ausnahmen außer Abbruch.
  - Ein DB-Fehler nach erfolgreichem POST lässt `Gesendet` stehen. Das wird später zu „unklar“, nie zu einem zweiten POST.
- **XML-Antwort:** `XDocument.Parse` mit Auswertung über `LocalName`. Die echte Schreibtest-Antwort ist Testfall. Eine DTD ist bei `XDocument.Parse` standardmäßig verboten; die `XmlException` wird gefangen.
- **AK 21 (kein Auto-Retry):** `RunOnceAsync` wählt nur `Offen`. `Fehler` wird nur über `SageWeErneutSendenAsync` (von Hand) wieder `Offen`. Das Dienst-Log vom 2026-11-12 enthält genau **einen** `POST …/StandardBelegService` (08:28:46). E5 steht als Blocker in der Etappen-Tabelle.
- **Defektklassen:**
  - Keine neuen Werte in `<script>`, kein `hidden` zusammen mit `d-*`.
  - Kein stilles `?? 0` (außer Q-1, behoben).
  - Lieferschein ist ein einzelner Parameter, keine Parallel-Arrays.
  - Länge und Pflicht prüft der Server. `required`/`maxlength` sind nur Komfort; auf DEV mit Leerzeichen gemessen.

### 3. Beweisart je Akzeptanzkriterium
| AK | Beweis | Stand |
|---|---|---|
| 1 | automatisiert (Bestandstests grün, `Decorator_SageWe_…` NichtNoetig) | grün |
| 2 | automatisiert für Decision und Decorator; `EnqueueMissingAsync` nur InMemory (die SQL-Übersetzung der Navigation ist ungeprüft) → TS-116.11 | teilweise, Rest Manual-UAT |
| 3 | automatisiert (`RestRegel_…`, Repository-Test) + DEV: Bestellung 2027-168715 Pos. 1 offen 3, erfasst 1, Rest **3** (nicht 2) | grün |
| 4 | DEV: `SQL/143` zweimal gegen `IDEAL_WMS_DEV` (bereits migriert), beide Läufe Exit 0 **ohne Ausgabe**; Altbestand siehe Umsetzung (Leer-DB) | grün (Manual) |
| 5 | automatisiert + DEV (siehe 6) | grün |
| 6 | automatisiert + DEV (Leerzeichen serverseitig abgewiesen) | grün |
| 7 | automatisiert + DEV `/Settings` (Schalter, Text, Standard, Standort, „Wirkt nur, wenn …“) | grün |
| 8 | automatisiert (Attribute) + DEV (Liste, Filterkarte, Server-Spaltenfilter „Lieferschein“); Benutzer mit nur `stock_read` nicht geprüft | teilweise, Rest Manual-UAT |
| 9 | automatisiert + DEV („Erneut senden“, Hinweis, Modified*) | grün |
| 10 | automatisiert + DEV (NichtNoetig, kein Queue-Eintrag) | grün |
| 11 | automatisiert (Service mit Fake-Client) + Schreibtest (nur 1 Position); zwei Positionen echt = TS-116.14 | teilweise, Rest Manual-UAT (Gerald) |
| 12, 13 | automatisiert | grün |
| 14 | nur Unit-Test (keine Chargenartikel vorhanden) | grün (automatisiert) |
| 15 | automatisiert; auf DEV **nicht möglich** (Schalter darf nicht an) → TS-116.13 | teilweise, Rest Manual-UAT |
| 16 | automatisiert; echt = TS-116.16 (Gerald) | teilweise, Rest Manual-UAT |
| 17 | automatisiert (DI, Name, Ctor) + DEV `SyncLogs` Service `SageWareneingang` aus dem Schreibtest (Counts deutsch) | grün |
| 18 | DEV `/ServiceSettings` (Anzeigename, Wirkung, Standort AKE, Warnhinweis) | grün (Manual) |
| 19 | Abschnitt „Schreibtest-Ergebnis“ vorhanden; **Rückbau und P-W9 (a)/(b) stehen aus** (Gerald) | offen, nicht grün |
| 20 | entfällt (E5 nicht gebaut) | – |
| 21 | Code-Review + Dienst-Log (genau ein POST) | grün |

### 4. Geänderte Bestandstests
- `PurchaseOrdersControllerTests`: nur Typen (`ErfasstJeBelPos`, neuer Ctor-Parameter `IServiceSettingRepository`), Kompilierfix.
- `PurchaseOrdersReceiveTests`:
  - Ctor und Typ angepasst (Kompilierfix).
  - Fixture erweitert: Platz 9 bekommt den Sage-Bezug, die Plätze 10 und 11 kommen dazu. Das ist nur für die neuen AK-10/15-Tests nötig.
  - Die Bestandstests laufen ohne Decorator und mit Sage-WE aus. Ihr Verhalten ist unverändert.
  - Die Assertion `erfasst` vergleicht jetzt `.Gesamt` (Typ).
  - Ergebnis: Typänderung und Fixture-Erweiterung, kein Befund.
- `AppSettingDefinitionsTests`: Zähler 55 → 56, gewollt (AK 7).
- `NachlaufManifestDriftTests`: `SQL/143` in die Reihenfolge aufgenommen, gewollt (E6).

### 5. CLAUDE.md-Checkliste
Erfüllt:
- Migration und `SQL/143` mit Guard.
- FreshInstall an beiden Stellen.
- AgentJobs nicht betroffen.
- Audit-Felder.
- Version 1.99.4 in beiden `AppVersion.cs`.
- Anwender-Changelog und Hilfe.
- Brain-Changelog `2026-11-11-v1-99-4-wareneingang-sage-we-beleg`.
- `feature-map`, `fallstricke`, `services`, `datenmodell`, `controller` und `glossar` ergänzt (`acfffcd1`).
- README.
- `docs/TESTSZENARIEN.md` TS-116.11–116.17 und Testindex-Zeile „116 (Teil 2)“.
- Keine neue Rolle, kein Einmal-Skript.
- Manifest: `SQL/143` und drei Nachher-Einträge (`v1.99.4-SageWe`, `v1.99.4-Lieferschein`, `v1.99.4-EinkaufWe`). Damit sind alle Deploy-Schritte der Spec abgedeckt.

### 6. DEV-Deploy und Testlauf
- **Standort:** laut Frontmatter AKE. Nach Vorgabe wurden AKE **und** IDEAL deployt, je Web und Dienst, über `dev-deploy.ps1` in einer Shell mit Administratorrechten.
- **Läufe:** Lauf 1 mit `2d09489a`, Lauf 2 nach dem Fix `152ab6f1`. Alle vier Läufe mit Exit 0 und „HTTP 200, Fusszeile v1.99.4“. `bd9b58d0` ändert nur das Manifest; DEV hat damit denselben Code.
- **Migration:** `20261111200627_AddPurchaseOrderReceiptSageWe` steht in `AKE_WMS_DEV` und in `IDEAL_WMS_DEV`.
- **Schalter danach:** `SageWareneingangBelegAktiv = false` und `SageLagerbuchungAktiv = false` auf beiden DEV-DBs. Die Absicherung hat auf AKE auch die 5 zuvor eingeschalteten `Sync:*Enabled` (Articles, ArtikelLieferant, Debitoren, EinkaufBestellungen, Kreditoren) auf `false` gesetzt. Wiedereinschalten macht der Koordinator.
- **Receipt #4** (`Uebergeben`, 2027-398675) ist unverändert.

| Schritt | Ausgeführt | Ergebnis | Urteil |
|---|---|---|---|
| TS-116.11 Queue-Ausschluss | – | Braucht beide Schalter an (verboten). | NICHT MÖGLICH |
| TS-116.12 Lieferschein | Bestellung 2027-168975 (BelId 363221). Pflicht an (AppSetting per SQL, nach Ablauf des 2-Minuten-Cache wirksam). Leeres Feld: Browser-`required` sperrt. Drei Leerzeichen: Server meldet „Bitte die Lieferschein-Nummer des Lieferanten eingeben.“, nichts gebucht, Menge Pos. 1 bleibt stehen. Dann „ LS-4711 “ mit Pos. 1 und 2. | Receipts #5/#6 mit `LS-4711` (getrimmt), `SageWeStatus 0`, `SageWeVorgang NULL`, kein Eintrag in `SageBookingQueueItems`; Rest danach 0. 21 Zeichen lässt der Browser wegen `maxlength=20` nicht zu; den Server-Zweig belegt der Unit-Test. Pflicht danach wieder entfernt (Zeile gelöscht = Standard aus). | OK |
| TS-116.13 Platzregel | – | Braucht `SageWareneingangBelegAktiv` an (verboten). | NICHT MÖGLICH |
| TS-116.14 | – | Nur Gerald/Einkauf (schreibt nach Sage). | NICHT MÖGLICH (Regel) |
| TS-116.15 Liste | Als Admin `/PurchaseOrders/SageWareneingaenge` geöffnet. Spaltenfilter Lieferschein `xyz` → „Keine Wareneingänge“. `?colf_lieferschein=wms-test&status=Uebergeben&bestellung=168715` → 1 Treffer (2027-398675, Memo `WE#4#`). Für „Erneut senden“ Receipt #5 per SQL künstlich auf `Fehler` mit Gruppe gesetzt; Knopf mit Hinweis „Vorher in Sage prüfen (Memo WE#5#)“ geklickt. | Status 1 (Offen), Versuche bleiben 1, Fehlertext leer, `Modified*` = Gerald Weichbold / `AKENB\gerald.weichbold`. Danach #5 zurück auf `NichtNoetig` (Schalter aus, also kein Senden). Rolle `stock_read` nicht geprüft (kein Testkonto). | OK / Rolle NICHT MÖGLICH |
| TS-116.16 | – | Nur Gerald/Einkauf. | NICHT MÖGLICH (Regel) |
| TS-116.17 Statuszeile | `/PurchaseOrders/Receive?belId=362313` | Unter EK001640 steht „Sage-Wareneingang: 1× an Sage übergeben“; Rest 3. | OK |
| `/Settings`, `/ServiceSettings` | Sichtprüfung | Beide Einträge mit Text, Standard und Standort; Warnhinweis am Dienst-Schalter. | OK |
| Dienst-Log / SyncLogs | `D:\Dev\AKE\Service\logs\service-20261112.log`, `dbo.SyncLogs` | Genau ein POST (Schreibtest 08:28:46). `[ERR]` um 08:28:51 ist der bekannte Befund 2 (behoben `0d265005`). Lauf „SageWareneingang“ ist protokolliert. | OK |

### 7. Publish
- Ordner: `C:\git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish`.
- Inhalt: `IDEALAKEWMSWeb`, `IDEALAKEWMSWebService`, `SQL` (mit `143_…`) und `deploy` (nach `bd9b58d0` nachkopiert, identisch zu `scripts/deploy`).
- `publish-info.txt`: Version 1.99.4, Commit `bd9b58d0ef1883a056f42093a73c96fcaa2aeb6f`, Branch `feature/2026-08-07-ideal-teile-1-5`.
- Zu deployen sind beide Komponenten, mit Migration.

### Manuelle Test-Checkliste (offen, Mensch)
1. **Rückbau** des Sage-Belegs 2027-398675 in `ake_TEST2026` (Gerald/Einkauf). Danach Abgleich: Pos. 404456 ist wieder offen 4 (AK 19).
2. Am Beleg 2027-398675 Lieferschein `WMS-TEST-1` und den Platz-Bestand auf `GL;0;0;0` sichten (AK 19).
3. **P-W9 (a)** Sage-SQL (Memo am ELL-Beleg) und **(b)** SData-Lookup `… like '%WE#4#%'` ausführen. Das Ergebnis entscheidet über E5.
4. TS-116.11 auf dem Testsystem: Queue-Ausschluss mit beiden Schaltern an; auch nach 15 Minuten kein Queue-Eintrag. Das prüft auch die SQL-Übersetzung von `EnqueueMissingAsync` (AK 2).
5. TS-116.13 Platzregel mit Schalter an: Ein Platz mit „Sage-Buchung erlaubt“ aus und ein manueller Platz ergeben je einen Zeilenfehler, nichts wird gebucht (AK 15).
6. TS-116.14 (Gerald/Einkauf): Zwei Positionen in einem Absenden ergeben einen Beleg, ein zweites Absenden einen zweiten. Der Rest wird nicht doppelt abgezogen. Danach Rückbau (AK 11, 12).
7. TS-116.16 (Gerald/Einkauf): Falsches Kennwort ergibt `Fehler`, es gibt keinen Auto-Retry, danach „Erneut senden“ (AK 16).
8. TS-116.15 mit einem Benutzer, der nur `stock_read` hat: Der Knopf fehlt, ein direkter POST endet auf AccessDenied (AK 8).
9. Q-2 entscheiden (Hinweis, wenn der Schalter aus ist und noch offene Belege existieren). Für Q-8 die DEV-Absicherung um `SageWareneingangBelegAktiv` ergänzen.
10. Testsystem-Lauf mit dem Paket aus `publish` (Update-Skript, `SQL/143`, Nachher-Einträge), danach Schranke 2.

## Freigabe-Nachtrag 2026-11-12 (Gerald, Chat): Memo je Position, v1.99.5

Nach dem Schreibtest: Memo am Belegkopf und Lieferschein-Nr. sind in Sage angekommen (Gerald). Zusaetzlich je Position ein Memo
(`StandardBelegPosition.Artikelmemo`), Inhalt laut Vorschlag: `WE#<Wareneingangs-Id># LS <Lieferschein> Platz <Lagerkennung>` (LS-Teil entfaellt
ohne Lieferschein). Zweiter Test-Beleg am AKE-Test nur nach Geralds OK (Beleginhalt und Aufruf vorher zeigen), Rueckbau durch Gerald.

## Umsetzung Nachtrag v1.99.5 und zweiter Schreibtest (2026-11-12)

Commit `c155b1b2` (auf `bd9b58d0`): `SageWeBelegPosition.Artikelmemo` = `SageWareneingangPayloadBuilder.PositionsMemo(receiptId, lieferschein,
lagerkennung)` → `WE#<id># LS <Lieferschein> Platz <Lagerkennung>` (ohne Lieferschein ohne „LS …“). Version 1.99.5, Changelog-Karte, TS-116.18.
Build 0 Fehler; `dotnet test` Web 2639/2640 (1 uebersprungen, Bestand), Dienst 522/522.

**Zweiter Schreibtest (OK Gerald im Chat nach Vorlage von Request und Aufruf):** AKE-DEV v1.99.5 (Dienst gestoppt), ueber die Maske Bestellung
2027-168715 Pos. 404457 EK001641 **1 Stk** auf `GL;0;0;0`, Lieferschein `WMS-TEST-2` → Wareneingang #7. Request wie im ersten Test plus
`"Artikelmemo": "WE#7# LS WMS-TEST-2 Platz GL;0;0;0"`, Kopf `"EKLieferschein": "WMS-TEST-2", "Memo": "WE#7#"`, `Belegdatum 2026-11-12`.
Genau ein Versuch (09:08:27), Schalter sofort wieder aus. Antwort Atom-XML `BelegnummerFormatiert 2027-398676`, `ResultNumber 200`, `Ok` – diesmal
direkt im WMS als **Uebergeben** mit Belegnummer ausgewertet (Fix `0d265005` im echten Ablauf belegt). Abgleich 09:14 (lesend): Pos. 404457
geliefert 0 → **1**, offen 2 → **1**; Pos. 404456 unveraendert (geliefert 1 aus Beleg 2027-398675, noch nicht zurueckgebaut).
Erster Test laut Gerald: Kopf-Memo und Lieferschein-Nr. in Sage angekommen.

**Offen fuer Gerald:** Positions-Memo am Beleg 2027-398676 in Sage sichten (TS-116.18); Rueckbau 2027-398675 und 2027-398676; P-W9 (a)/(b).

## QA-Nachweis Nachtrag v1.99.5 (qa-agent, 2026-11-12)

Umfang: nur der Freigabe-Nachtrag „Memo je Position“, Diff `bd9b58d0..c155b1b2` (6 Dateien, +25/−3). Build und Tests über den ganzen Worktree.

### 1. Build und Tests (HEAD `c155b1b2`)
- `dotnet build IdealAkeWms.slnx`: 0 Fehler, 25 Warnungen (Bestand).
- `dotnet test`: `IDEALAKEWMSService.Tests` 522 bestanden / 0 übersprungen / 0 Fehler; `IdealAkeWms.Tests` 2639 bestanden / 1 übersprungen
  (Bestand) / 0 Fehler.

### 2. Code-Review
Geprüft: `SageWareneingangPayload.cs` (neues `SageWeBelegPosition.Artikelmemo`, `PositionsMemo`, Aufruf in `Build`), Serialisierung
`SageWareneingangClient.Serialize` (System.Text.Json, Eigenschaftsname unverändert `Artikelmemo`), Vertrag `StandardBelegPosition.Artikelmemo`
in [[sage-sdata-commonwawiservices]] (Feld vorhanden, kein `ArtikelmemoSpecified` im Vertrag), Test, Changelog-Karte, TS-116.18.
- Kein Befund im Code: `platz!.SageLagerkennung!` ist durch `SageWareneingang.IstSageWePlatz(platz)` davor abgesichert (gleiche Annahme wie
  die bestehende `Kurzbezeichnung`-Zeile). Kopf-Memo und Positions-Memo nutzen dieselbe Quelle `SageWareneingang.Memo(id)` (eine Regel, eine
  Stelle); `WE#<id>#` mit schließendem `#` verhindert Präfix-Treffer (`WE#1#` ≠ `WE#10#`). Lieferschein wird getrimmt, leer/Leerzeichen → ohne „LS“
  (Test belegt beide Zweige). Keine Einstellung, kein Skript-Block, kein stiller Fallback.
- **B-1 (Brain, behoben im Hauptcheckout):** Testindex-Zeile „116 (Teil 2)“ nannte nur 116.11–116.17 → auf 116.18 erweitert. Brain-Changelog
  für v1.99.5 fehlte → [[2026-11-12-v1-99-5-wareneingang-memo-je-position]] angelegt. `feature-map` stand noch auf „InUmsetzung, QA ausstehend“ →
  auf Testbereit mit v1.99.5 gezogen.
- **H-1 (Hinweis, nicht behoben):** `AppVersion.Date` steht in Web und Dienst weiter auf `2026-11-11`, die Changelog-Karte nennt 12.11.2026
  (gleiches Muster schon bei v1.99.4). Kosmetisch; beim nächsten Bump mitziehen.
- **H-2 (Hinweis):** Ob Sage das Positions-Memo tatsächlich speichert, belegt nur TS-116.18 in Sage (Beleg 2027-398676). Gesendet wurde es
  nachweislich (Dienst-Log 09:08:26, `"Artikelmemo":"WE#7# LS WMS-TEST-2 Platz GL;0;0;0"`).

### 3. Beweisart
- Payload enthält `Artikelmemo` im vereinbarten Format, mit/ohne Lieferschein: automatisiert (`SageWareneingangServiceTests`, neue Assertions).
- Sage übernimmt das Memo an der Position: **Manual-UAT** (TS-116.18, nur Gerald/Einkauf), nicht grün gezählt.

### 4. Geänderte Bestandstests
- `SageWareneingangServiceTests` (Payload-Test): nur zwei Assertions **ergänzt**, keine bestehende geändert. Verhalten bewusst erweitert durch den
  Freigabe-Nachtrag v1.99.5. Kein Befund.

### 5. Checkliste
Keine Migration, kein SQL, kein FreshInstall, keine Audit-Felder, keine Einstellung (ADR 0020 entfällt). Version 1.99.5 in beiden `AppVersion.cs`,
Anwender-Changelog ja, Brain-Changelog nachgezogen (B-1), `docs/TESTSZENARIEN.md` TS-116.18 + Fußzeile, Testindex nachgezogen (B-1).
Nachlauf: kein Deploy-Schritt → kein Eintrag in `nachlauf.psd1` nötig.

### 6. DEV-Deploy und Testlauf
- Standort laut Frontmatter AKE; nach Vorgabe AKE **und** IDEAL deployt, je Web + Dienst aus dem Worktree, Shell mit Administratorrechten.
- `dev-deploy.ps1 -Standort AKE`: Exit 0, „HTTP 200, Fusszeile v1.99.5“. `-Standort IDEAL`: Exit 0, „HTTP 200, Fusszeile v1.99.5“.
- Schalter danach (beide DEV-DBs): `SageWareneingangBelegAktiv`, `SageLagerbuchungAktiv`, `SageLagerbuchungUmbuchungAktiv` = false. Die
  Absicherung setzt auf AKE die `Sync:*Enabled` wieder auf false; Wiedereinschalten macht der Koordinator.

| Schritt | Ausgeführt | Ergebnis | Urteil |
|---|---|---|---|
| Fußzeile/Changelog | `http://localhost:8081/Help/Changelog` im Browser | Karte „v1.99.5 12.11.2026“, Fußzeile „IDEAL-AKE WMS v1.99.5“ | OK |
| Receipts #4/#7 | `sqlcmd` AKE_WMS_DEV | #4 `WMS-TEST-1` Status 3 2027-398675 Versuche 1; #7 `WMS-TEST-2` Status 3 2027-398676 Versuche 1; unverändert | OK |
| Kein weiterer Sage-POST | Dienst-Logs AKE/IDEAL 2026-11-12 | genau zwei POSTs (08:28:46, 09:08:26 = die zwei freigegebenen Schreibtests), danach keiner; 0 neue `SageBookingQueueItems` | OK |
| Payload im echten Lauf | Dienst-Log 09:08:26 | `"Artikelmemo":"WE#7# LS WMS-TEST-2 Platz GL;0;0;0"` im Request | OK |
| TS-116.18 Memo in Sage sichten | – | Liest den Beleg in Sage; Sage gehört nicht in den Agentenlauf (Gerald/Einkauf) | NICHT MÖGLICH (Regel) |

### 7. Publish
- Ordner `C:\git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish` vorher geleert, neu: `IDEALAKEWMSWeb`,
  `IDEALAKEWMSWebService`, `SQL`, `deploy`, `publish-info.txt` (Version 1.99.5, Commit `c155b1b2c0bd73157096a0d124cbc0264eb31348`,
  Branch `feature/2026-08-07-ideal-teile-1-5`, Erstellt 2026-11-12 09:17).
- Zu deployen: Dienst (wirksam) und Web (Version/Changelog), keine neue Migration.

**Urteil:** Testbereit (v1.99.5).

### Manuelle Test-Checkliste (offen, Mensch) – ergänzt die Liste aus v1.99.4 oben
1. TS-116.18: Beleg 2027-398676 in Sage öffnen; Position EK001641 trägt „WE#7# LS WMS-TEST-2 Platz GL;0;0;0“.
2. Rückbau 2027-398675 und 2027-398676 (Gerald/Einkauf), danach Abgleich: Pos. 404456 und 404457 wieder offen.
3. P-W9 (a)/(b) wie oben (entscheidet E5).
4. Alle offenen Punkte der v1.99.4-Checkliste (TS-116.11, .13, .14, .15 mit `stock_read`, .16, Q-2/Q-8).
5. Testsystem-Lauf mit dem Paket aus `publish` (v1.99.5), danach Schranke 2.

## P-W9 (a) Ergebnis – Gerald 2026-11-12, Sage-SQL `ake_TEST2026`, nur lesend

`KHKEKBelege`, `Belegkennzeichen = 'ELL'`, Memo `LIKE '%WE#%'` → genau die zwei Test-Belege:

| BelID | Belegnummer | Belegjahr | Belegdatum | Memo (Kopf) | Lieferschein-Feld | Lieferant |
|---|---|---|---|---|---|---|
| 363225 | 398675 | 2027 | 2026-11-11 | `WE#4#` | `WMS-TEST-1` | K30601 Gasperlmair Glas |
| 363226 | 398676 | 2027 | 2026-11-12 | `WE#7#` | `WMS-TEST-2` | K30601 Gasperlmair Glas |

- Das Kopf-Memo steht **unverändert** am Beleg und ist über `LIKE` eindeutig auffindbar. Die Kennung `WE#<id>#` erscheint in der Zeile noch
  ein zweites Mal, in einer weiteren Spalte. Welche Spalte das ist, ist nicht belegt; Gerald hat die Spaltennamen nicht mitgeschickt.
- Die Lieferscheinnummer ist am Kopf angekommen.
- Position (TS-116.18, Positions-Memo `WE#7# LS WMS-TEST-2 Platz GL;0;0;0`): noch nicht geprüft.
- **(a) positiv.** Offen: (b) SData-Lookup über `EinkaufBelegeInfo`, danach Rückbau der beiden Belege.

### (b) Ergebnis – Dev-Lauf 2026-11-12, SData nur `GET`, Sage-AKE-Test

Die Lesezugriffe liefen mit dem Ziel aus der DEV-Konfiguration des AKE-DEV-Dienstes (`SdataLeseZielLeser`; Basis
`https://sagetest01.ake.at:5493/`, Dataset `ake_TEST2026;1`). Die Abfragen gingen über den echten Lese-Client: `SageSdataReadClient.BuildQueryUrl`
bzw. `QueryAllAsync` mit Basic-Auth und `Accept: application/json`. Es gab keinen POST. Zugangsdaten sind hier nicht festgehalten.

1. **`EinkaufBelegeInfo/$schema` → HTTP 401**, sowohl mit `Accept: application/xml` als auch mit `*/*`. Der API-Benutzer darf das Schema nicht
   lesen; dieselben Zugangsdaten bekommen auf die Datenabfragen HTTP 200. Die Feldnamen stammen deshalb aus dem vollständigen Datensatz
   (`…/EinkaufBelegeInfo?format=json&count=1&where=BelID eq 363225`, gekürzt):
   `"$key":"BelID:=363225;Mandant:=1;"`, `"BelID":363225`, `"Belegkennzeichen":"ELL"`, `"Belegart":"Wareneingang"`, `"Belegjahr":2027`,
   `"Belegnummer":398675`, `"Memo":"WE#4#"`, `"Status":0`, `"StatusStatistik":202`, `"Lieferschein":"WMS-TEST-1"`,
   `"ReferenzBelID":362313`.
   → **BelId-Feld `BelID`, Memo-Feld `Memo`.** Die Lieferschein-Nr. steht zusätzlich im Feld `Lieferschein`. `ReferenzBelID` ist die
   Bestellung (362313), aus der der Beleg übernommen wurde.
2. **Lookup, so wie der Dienst kodiert** (führendes `%` → `%25`, `#` → `%23`):

   | Anfrage (ohne Basis-Pfad `…/sdata/ol/CommonWawiServicesResources/ake_TEST2026;1/`) | Treffer |
   |---|---|
   | `EinkaufBelegeInfo?format=json&orderBy=BelID&count=5&startIndex=0&select=BelID,Belegkennzeichen,Memo&where=Memo like '%25WE%237%23%25'` | genau 1: `BelID=363226`, `ELL`, `Memo=WE#7#` (`$totalResults` 1) |
   | dto. `where=Memo like '%25WE%234%23%25'` | genau 1: `BelID=363225`, `ELL`, `Memo=WE#4#` (`$totalResults` 1) |
   | dto. `where=Memo like '%25WE%231%23%25'` (Gegenprobe: Präfix darf nicht auf `WE#1x#` springen) | 0 |

   → Führendes `%` und `%23` funktionieren. Damit sind die zwei ungeprüften Punkte aus `SdataBuchungLookupReader.cs:31-33` belegt. Das
   schließende `#` verhindert Präfix-Treffer schon durch den Aufbau der Kennung.
3. **Erledigte und alte Belege:** Die Info-Ressource liefert sie. Eine Seite (`count=3`) mit `where=Belegkennzeichen eq 'ELL' and Status ne 0`
   meldet `$totalResults` 89 950. Die ersten Treffer sind BelID 23961 (Belegjahr 2015, `Status` 2) und BelID 43055 (2016, `Status` 2). Mit
   `Belegjahr lt 2026` sind es 78 558 Treffer. Die Annahme aus P-W7 („nur offene Belege“) trifft für `EinkaufBelegeInfo` also **nicht** zu.
   Was `Status` 2 in Sage bedeutet (weitergeführt oder erledigt?), ist nicht belegt; für den Lookup spielt es keine Rolle.
   - **Fallstrick für den Lookup-Bau:** `QueryAllAsync` blättert alle Seiten. Ein breiter Filter wie nur `Belegkennzeichen eq 'ELL'` (rund
     90 000 Belege, 5 je Seite) lief im Test über 10 Minuten und wurde abgebrochen. Der Lookup filtert daher immer auf die Memo-Kennung.
     Sinnvoll ist zusätzlich `Belegkennzeichen eq 'ELL' and …`.

**(b) positiv → P-W9 belegt ((a) und (b)) → Variante A ist baubar, E5 kann gebaut werden.**

Vorgaben für E5:
- Lookup `EinkaufBelegeInfo`, `select=BelID,Belegnummer,Belegjahr,Memo`,
  `where=Belegkennzeichen eq 'ELL' and Memo like '%WE#<Id>#%'`.
- Bei genau einem Treffer: `Uebergeben` mit Belegnummer und **ohne POST**.
- Bei 0 Treffern: wiederholen.
- Bei mehr als einem Treffer: `Fehler` mit Meldung, nicht still behandeln.

Restrisiko: Das Kopf-Memo ist in Sage von Hand änderbar. Wird es überschrieben, findet der Lookup den Beleg nicht mehr, und ein Wiederholen
legt einen zweiten Wareneingang an. Ob die Kennung zusätzlich gegen `Lieferschein` + `ReferenzBelID` geprüft wird, entscheidet Gerald.
Der Rückbau von 2027-398675/398676 steht weiter aus (Gerald).

## Freigabe-Nachtrag 2026-11-12 (Gerald, Chat): Positionsmemo-Feld im Wareneingang, v1.99.6

Positions-Memo am Beleg 2027-398676 in Sage uebernommen (Gerald). Neu: je Position ein Eingabefeld „Positionsmemo“ in „Wareneingang zu Bestellung“.
- Inhalt in Sage (`Artikelmemo`): **Eingabe vorne, automatische Kennung dahinter**, getrennt mit „ | “, z. B. `Kante beschädigt | WE#7# LS WMS-TEST-2
  Platz GL;0;0;0`; leeres Feld = nur die Kennung wie in v1.99.5.
- **Immer speichern**, auch ohne Sage-Wareneingang (wie die Lieferschein-Nr.); an Sage nur im Sage-Wareneingang.
- Umsetzung (Dev-Lauf): gespeichert in der vorhandenen Notiz der Bewegung (`StockMovement.Note`, nvarchar(500), Anzeige Bewegungshistorie; die
  Sage-Lagerbuchung sendet die Notiz nicht, `SageLagerbuchungPayloadBuilder` setzt nur `Standardtext`) – keine Migration. Eingabe hoechstens 200 Zeichen.

### Umsetzung Nachtrag v1.99.6 (2026-11-12)

Commit im Buendel (siehe `git log`, „v1.99.6 Positionsmemo-Feld“): `PurchaseOrderReceiveInput.Memo` (je Position, hoechstens 200 Zeichen, sonst
Zeilenfehler und nichts gebucht), gespeichert als `StockMovement.Note` (getrimmt, leer = null; immer, auch ohne Sage-WE); Maske: Spalte
„Positionsmemo“; Liste „Wareneingänge an Sage“: Spalte „Positionsmemo“ (Spaltenfilter); Sage-Payload: `PositionsMemo(..., eingabe)` →
`<Eingabe> | WE#<id># LS … Platz …`. Keine Migration. Version 1.99.6, Changelog, Hilfe, TS-116.19. Build 0 Fehler; Web 2641/2642 (1 uebersprungen,
Bestand), Dienst 522/522. Kein weiterer Schreibtest (Inhalt vor der bereits in Sage belegten Kennung; Sichtung in Sage = TS-116.19, Gerald).

## QA-Nachweis Nachtrag v1.99.6 (qa-agent, 2026-11-12)

Umfang: nur der Freigabe-Nachtrag „Positionsmemo-Feld“, Diff `c155b1b2..23ea96ca` (13 Dateien, +73/−9) plus QA-Korrektur `67322b85`. Build und
Tests über den ganzen Worktree.

### 1. Build und Tests (HEAD `23ea96ca`, nach der QA-Korrektur erneut auf `67322b85`)
- `dotnet build IdealAkeWms.slnx`: 0 Fehler (Warnungen Bestand: NuGet-Advisories).
- `dotnet test`: `IDEALAKEWMSService.Tests` 522 bestanden / 0 übersprungen / 0 Fehler; `IdealAkeWms.Tests` 2641 bestanden / 1 übersprungen
  (Bestand) / 0 Fehler. Beide Läufe identisch.

### 2. Code-Review
Geprüft: `PurchaseOrdersController.Receive.cs` (Prüfkette, Speichern `Note`), `PurchaseOrderReceiveViewModel` (`MemoMaxLaenge`, `Memo`),
`Receive.cshtml` (neue Spalte), `SageWareneingaenge.cshtml` + `SageWeColumnMap` (Spalte, Filter), `SageWareneingangPayload.cs` (`PositionsMemo`),
Tests, Changelog, Hilfe, TS-116.19. Alle Leser/Schreiber von `StockMovement.Note` per grep über Web, Dienst und `SQL/`:
- Schreiber: `LagerbestandSyncService.cs:185,253` (eigene Bewegungen „Sage-Korrektur…“) und neu `PurchaseOrdersController.Receive.cs:113`. Kein Code
  liest `Note`, um Bewegungen zu unterscheiden.
- Leser: Bewegungshistorie (`StockMovementRepository.cs:388`, `Views/StockMovements/Index.cshtml:141`, reine Anzeige), Liste „Wareneingänge an Sage“
  (neu), Sage-WE-Payload (neu). `SageLagerbuchungPayloadBuilder` liest `Note` **nicht** (keine Fundstelle) → die Sage-Lagerbuchung ändert sich nicht.
  `SQL/110:65` gruppiert `LEFT(Note,15)` nur als Diagnose-Ausgabe eines Einmal-Backfills, keine Logik.
- Server-Validierung vorhanden (`Receive.cs:70`, Zeilenfehler, nichts gebucht), `maxlength` im Browser nur Zusatz. Bindung `zeilen[@i].Memo`
  indiziert, Zuordnung über `BelPosId` (`Receive.cs:154`), kein positionsweises Verschieben. Kein `@Html.Raw`, kein Wert in `<script>`, kein
  `hidden`+`d-*`, kein stiller Fallback (leer → `null`). Regel „Eingabe | Kennung“ an einer Stelle (`PositionsMemo`).
- Oberfläche: Feld `form-control` mit `min-height:44px` wie „Ist-Menge“, `aria-label` je Position, Bootstrap-Standardfarben (Kontrast wie
  Bestand). Liste: `<th data-filterable data-col-key="memo-position">` und Eintrag in `SageWeColumnMap`, `colCount` 13→14 nachgezogen; Textspalte
  braucht keinen Eintrag in `ColumnFilterTypes` (Standard Text).
- **F-1 (behoben, `67322b85`):** Changelog-Karte und TS-116.19 versprachen die Notiz „in der Liste ‚Wareneingänge an Sage‘“ ohne Einschränkung.
  Die Liste zeigt nur Sage-Wareneingänge (Status ≠ „nicht nötig“; auf DEV mit Schalter aus nachgemessen: Receipts #8/#9 nicht gelistet). Beide Texte
  präzisiert; TS-116.19 nennt die Bewegungshistorie als Ort ohne Sage-WE.
- **H-1 (Hinweis):** Ein Memo an einer Zeile ohne Ist-Menge wird nicht gespeichert (Zeile wird nicht gebucht) – folgerichtig, kein Befund.
- **H-2 (Hinweis):** Ein langes Memo ohne Leerzeichen dehnt die Notiz-Spalte der Bewegungshistorie (auf DEV mit 199 × „M“ gesehen); echte Texte
  umbrechen. Nicht behoben.
- **H-3 (Hinweis, offen aus v1.99.5):** `AppVersion.Date` weiter `2026-11-11`.
- **H-4 (Manual-UAT):** Ob Sage `Artikelmemo` mit bis zu ~260 Zeichen (200 + „ | “ + Kennung) vollständig speichert, ist nicht belegt → TS-116.19.

### 3. Beweisart
- Memo gespeichert als Notiz, getrimmt, leer = null, auch ohne Sage-WE: automatisiert (`PurchaseOrdersReceiveTests.Positionsmemo_AlsNotizDerBewegung…`)
  und auf DEV AKE im Browser (echte Modellbindung).
- Mehr als 200 Zeichen → Zeilenfehler, nichts gebucht: automatisiert (`Positionsmemo_LaengerAls200…`) und auf DEV AKE.
- Payload „Eingabe | Kennung“, leer = Kennung: automatisiert (`SageWareneingangServiceTests`).
- Sage zeigt das Positions-Memo: **Manual-UAT** (TS-116.19, nur Gerald/Einkauf), nicht grün gezählt.

### 4. Geänderte Bestandstests
- `SageWareneingangServiceTests` (Payload-Test): fünf Zeilen **ergänzt**, keine bestehende Assertion geändert; Verhalten bewusst erweitert durch
  den Nachtrag v1.99.6. `PurchaseOrdersReceiveTests`: nur zwei neue Tests. Kein Befund.

### 5. Checkliste
Keine Migration, kein SQL, kein FreshInstall, keine neuen Audit-Felder (Bewegung wird wie bisher mit `Created*` angelegt), keine Einstellung (ADR 0020
entfällt). Version 1.99.6 in beiden `AppVersion.cs`, Anwender-Changelog + Hilfe ja, Brain-Changelog [[2026-11-12-v1-99-6-wareneingang-positionsmemo]]
angelegt, `feature-map` und Testindex nachgezogen, `docs/TESTSZENARIEN.md` TS-116.19 + Fußzeile. Nachlauf: kein Deploy-Schritt.

### 6. DEV-Deploy und Testlauf
- Standort laut Frontmatter AKE; AKE **und** IDEAL deployt, je Web + Dienst aus dem Worktree, Shell mit Administratorrechten; zweimal (auf
  `23ea96ca` und nach der QA-Korrektur auf `67322b85`), jedes Mal Exit 0, „HTTP 200, Fusszeile v1.99.6“.
- Schalter danach (beide DEV-DBs): `SageWareneingangBelegAktiv`, `SageLagerbuchungAktiv`, `SageLagerbuchungUmbuchungAktiv` = false. Sync-Schalter
  auf AKE setzt der Koordinator.

| Schritt | Ausgeführt | Ergebnis | Urteil |
|---|---|---|---|
| Maske zeigt Spalte | `http://localhost:8081/PurchaseOrders/Receive?belId=363221` (Bestellung 2027-168975) | Spalte „Positionsmemo“ zwischen Ist-Menge und Status, Feld je buchbarer Zeile | OK |
| Buchen mit Memo (199 Zeichen) | Pos. 3, Menge 1, GL;0;0;0 | Receipt #8, `SageWeStatus` 0 (nicht nötig), `Note` 199 Zeichen | OK |
| 201 Zeichen | Pos. 4, Wert per Skript gesetzt (umgeht `maxlength`) | Meldung „Nichts gebucht: bitte die markierten Zeilen korrigieren.“, Zeilenfehler „Positionsmemo höchstens 200 Zeichen.“; keine neue Bewegung | OK |
| Trim | Pos. 4 „␣␣Kante beschädigt␣␣“ | Receipt #9, `Note` = „Kante beschädigt“ | OK |
| Keine Sage-Wirkung | `SageBookingQueueItems` letzte 30 min; Dienst-Log AKE | 0 neue Queue-Zeilen; letzter Sage-POST 09:08:26 (freigegebener Schreibtest v1.99.5), danach keiner | OK |
| Bewegungshistorie | `/StockMovements` | Notiz „Kante beschädigt“ bzw. 199 × M an den beiden Einbuchungen | OK |
| Liste Spalte + Filter | Notiz an Receipt #7 per SQL testweise gesetzt, `/PurchaseOrders/SageWareneingaenge?colf_memo-position=QA`, danach zurück auf NULL | Spalte zeigt „QA-Memo Test“, Filter liefert 1 von 2 | OK |
| TS-116.19 in Sage | – | Schreibt nach Sage / liest Sage | NICHT MÖGLICH (Regel) |

Screenshots waren nicht möglich (Browser-Erweiterung: Zeitüberschreitung bei der Aufnahme); Belege über Seitentext und `sqlcmd`.
Testdaten auf DEV AKE: Receipts #8 (BelPos 406335) und #9 (BelPos 406336) der Bestellung 2027-168975, Status „nicht nötig“.

### 7. Publish
- Ordner `C:\git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish` vorher geleert, neu: `IDEALAKEWMSWeb`,
  `IDEALAKEWMSWebService`, `SQL`, `deploy`, `publish-info.txt` (Version 1.99.6, Commit `67322b85692cff04dcd691b77b66f66432ab08e2`,
  Branch `feature/2026-08-07-ideal-teile-1-5`, Erstellt 2026-11-12 09:46).
- Zu deployen: Web und Dienst, keine neue Migration.

**Urteil:** Testbereit (v1.99.6).

### Manuelle Test-Checkliste (offen, Mensch) – ergänzt die Listen aus v1.99.4/v1.99.5 oben
1. TS-116.19 am AKE-Test mit Sage-WE (Gerald/Einkauf): Positionsmemo „Kante beschädigt“ buchen, Beleg in Sage öffnen → Positions-Memo
   „Kante beschädigt | WE#<Id># LS … Platz …“, vollständig (auch mit 200 Zeichen, H-4); leeres Feld = nur Kennung.
2. Liste „Wareneingänge an Sage“ mit Sage-WE: Spalte „Positionsmemo“ zeigt den Text; Spaltenfilter.
3. Bedienung am Lager-Terminal (Tablet): Feld gut treffbar, Tabelle querscrollbar.
4. Offene Punkte aus v1.99.4/v1.99.5 (Rückbau der Test-Belege, P-W9, TS-116.11 ff.).
5. Testsystem-Lauf mit dem Paket aus `publish` (v1.99.6), danach Schranke 2.
- **Position (TS-116.18), Gerald 2026-11-12, `KHKEKBelegePositionen`, nur lesend:**
  - BelID 363226, Position 406347 (EK001641, Deckglas, 1,00 Stk, 42,63): Das Positions-Memo `WE#7# LS WMS-TEST-2 Platz GL;0;0;0` ist
    **angekommen**.
  - BelID 363225, Position 406344 (EK001640, 1,00 Stk, 23,43): an derselben Stelle NULL. Erwartet, weil der erste Test vor v1.99.5 (Positions-Memo) lief.
  - Spaltennamen nicht mitgeliefert, die Zuordnung zur Spalte ist daher unbelegt.

## Freigabe-Nachtrag 2026-11-12 (Gerald): E5 Variante A, Sicherungsnetz, Q-2, Sage-Protokolle im Menü – v1.99.6

Gilt vor dem QA-Abschnitt. Grundlage: P-W9 (a) und (b) positiv (oben).
1. **E5 Variante A bauen** (automatisches Wiederholen im Dienst), vor jedem erneuten Senden:
   1. Suche über das Memo: `Belegkennzeichen eq 'ELL' and Memo like '%WE#<Id>#%'`, kodiert wie im Dienst. Genau ein Treffer → `Uebergeben`
      (BelID/Belegnummer übernehmen), kein POST. Mehr als ein Treffer → `Fehler` mit Meldung.
   2. **Sicherungsnetz bei 0 Treffern:** zweite, eng gefilterte Suche nach ELL-Belegen mit `ReferenzBelID` = Bestellung des Wareneingangs,
      Belegdatum ab dem Datum der WE-Buchung und, wenn eine Lieferscheinnummer erfasst ist, `Lieferschein` = diese Nummer.
      - Treffer → **neuer Status „Klären“**, kein automatisches Senden, Hinweis „möglicher Beleg in Sage gefunden, Memo fehlt/geändert“ mit
        Belegnummer(n). Weiter nur per „Erneut senden“ von Hand (Rolle wie bisher) oder „als übergeben markieren“ mit Belegnummer.
      - Kein Treffer → erneut senden.
   3. Nie ein breiter Filter (nur ELL oder nur Status): Eine Testabfrage lief über 10 Minuten (P-W9 (b)).
   4. Wiederholgrenzen: Anzahl und Abstand als Einstellung (ADR 0020), danach `Fehler`.
2. **Q-2:** Ist `SageWareneingangBelegAktiv` aus und gibt es noch offene Einträge, zeigen die Liste und die Einstellung einen deutlichen Hinweis
   („Sage-Wareneingang ist aus – n Einträge offen, werden nicht gesendet“). Beim Wiedereinschalten werden sie nicht automatisch nachgeschickt,
   nur über „Erneut senden“.
3. **Sage-Protokolle im Menü (Wunsch Gerald):** Das Menü „Lager“ bekommt einen eigenen, durch Trennlinien abgesetzten Block mit beiden
   Sage-Protokollen und einheitlichen Namen:
   - „Sage-Lagerbuchungen“ (bisher `SageBookingQueue`, Name bleibt);
   - „Sage-Wareneingänge“ (bisher „Wareneingänge an Sage“, `PurchaseOrders/SageWareneingaenge`; Seitentitel und Hilfe angleichen).
   Sichtbarkeit je Eintrag wie bisher (Feature-Gate, Rechte). Heute: `Views/Shared/_Layout.cshtml:115` und `:121`.
4. **DEV-Absicherung:** `D:\Dev\scripts\dev-absicherung-ake.sql` und `-ideal.sql` setzen zusätzlich `SageWareneingangBelegAktiv = false`
   (Schlüssel im Code belegen; fehlt er, wird er als false angelegt). Das liegt außerhalb des Repos und ist kein Repo-Commit.
5. Manifest-Einträge für neue Einstellungen, Version 1.99.6, Tests auf DEV: Memo gefunden; 0 Treffer ohne Sicherungsnetz-Treffer; „Klären“.
   Die Sage-Suchen laufen lesend gegen den Sage-AKE-Test; kein weiterer Schreibtest nach Sage.
   Test „Memo gefunden“ live: Den DEV-Eintrag WE 7 bzw. 4 auf `Fehler` setzen; die Wiederholung muss BelID 363226 bzw. 363225 finden und darf nicht senden. Dafür lässt Gerald die zwei Test-Belege **bis nach diesem Test** in Sage stehen. Der Rückbau folgt danach.

### Umsetzung Nachtrag E5/Q-2/Menü – v1.99.7 (Dev-Lauf 2026-11-12, /goal-Lauf [[2026-11-12-buendel-v1-99-8-goal-lauf]])

**Version 1.99.7 statt 1.99.6** (1.99.6 = Positionsmemo, schon belegt). Keine Migration (`SageWeStatus` ist int, neuer Wert `Klaeren = 6`).
Commits im Bündel: `f8d74399` (Code), `af0a75f2` (TS-116.20–116.24).
1. **E5 Variante A** (`IDEALAKEWMSService/Services/SageWareneingangService.cs`, neu `Services/Sdata/SdataWareneingangLookupReader.cs`):
   - Automatisch wiederholt werden nur `Fehler` mit mindestens einem echten Sende-Versuch (`SageWeVersuche ≥ 1`; ein Payload-Fehler ohne POST nie),
     höchstens `Sync:SageWareneingangMaxWiederholungen`-mal (Standard 3, 0 = nie) und frühestens `Sync:SageWareneingangWiederholAbstandMinuten`
     (Standard 15) nach der letzten Änderung. Hängendes `Gesendet` → `Fehler` „Ergebnis unklar“ → wird danach mit Suche wiederholt.
   - Vor **jedem** erneuten Senden (auch nach „Erneut senden“ von Hand, erkennbar an `SageWeVersuche > 0`) Suche
     `Belegkennzeichen eq 'ELL' and Memo like '%WE#<Id>#%'`: 1 Treffer → `Uebergeben` (Belegnummer `Belegjahr-Belegnummer`), kein POST;
     >1 → `Fehler` „Mehrere Sage-Belege …“ (zählt als Versuch, damit es nicht endlos sucht). Erstversand ohne Suche.
   - **Sicherungsnetz** nur beim automatischen Wiederholen und 0 Memo-Treffern: `… and ReferenzBelID eq <BelId> and Belegdatum ge @<WE-Datum>@`
     `[and Lieferschein eq '<LS>']`; Treffer mit einer **anderen** WMS-Kennung `WE#n#` zählen nicht (andere WMS-Wareneingänge). Treffer → **Klären**
     mit „Möglicher Beleg in Sage gefunden, Memo fehlt/geändert: <Belege>“. Bei „Erneut senden“ von Hand kein Sicherungsnetz (der Mensch hat geprüft).
   - Scheitert die Suche: kein Senden, `Fehler` mit Text, Versuche unverändert.
   - Klären: weiter nur „Erneut senden“ (Fehler/Klären → Offen) oder neu **„Als übergeben markieren“** mit Belegnummer (Pflicht, ≤ 30 Zeichen,
     `RequireStockKeyUserAccess`, `SageWeAm` = jetzt).
2. **Q-2:** Ist `SageWareneingangBelegAktiv` aus, setzt der Worker-Takt Offen/Fehler auf **Klären** („war ausgeschaltet … nicht automatisch
   nachgeschickt“, alter Fehlertext angehängt) – so wird beim Wiedereinschalten nichts automatisch gesendet, unabhängig davon, wie der Schalter
   umgestellt wurde. Hinweis in Liste und /ServiceSettings: „Sage-Wareneingang ist aus – n Einträge offen, werden nicht gesendet …“ (n = Offen +
   Fehler + Klären). „Erneut senden“ ist bei ausgeschaltetem Schalter gesperrt (Hinweis).
3. **Menü:** eigener Block mit Trennlinie: „Sage-Lagerbuchungen“, „Sage-Wareneingänge“ (Feature-Gate wie bisher); Seitentitel, Hilfe, Receive-Hinweis
   umbenannt. Manifest-Einträge `v1.99.7-SageWeWiederholen`, `v1.99.7-EinkaufKlaeren`.
4. **DEV-Absicherung** außerhalb des Repos: siehe Lauf-Notiz (Q-8 erledigt).
5. **Tests:** `SageWareneingangServiceTests` (+10: Memo gefunden, 0/0 → senden, Sicherungsnetz → Klären inkl. fremder Kennung, mehrere Treffer,
   Suche scheitert, Grenze/0/ohne Versuch, Hand-Senden ohne Sicherungsnetz, Erstversand ohne Suche, Q-2, Filter-Strings),
   `SageWareneingaengeListeTests` (+4: Klären erneut senden, Q-2 gesperrt + Hinweis, Als übergeben, Attribute). Build 0 Fehler; Web 2646/2647
   (1 übersprungen, Bestand), Dienst 532/532.
6. **Live-Test „Memo gefunden“** (nur lesend, 2026-11-12 11:38): DEV-Dienst gestoppt, WE 4/7 auf `Fehler`, Lauf mit Sende-Client, der **immer
   wirft** → Aktivitäts-Protokoll „Sage-Wareneingang WE#4#: Beleg 2027-398675 in Sage gefunden, nicht erneut gesendet“ (dto. WE#7#/2027-398676),
   Zähler `in Sage gefunden=2`, **0 Sendeversuche**; Ausgangsstand wiederhergestellt. Live-Lesungen vorher: Memo-Suche WE#4#/WE#7# je genau
   363225/363226; Sicherungsnetz 362313 ab 2026-11-11 LS `WMS-TEST-1` → 363225 (eigene Kennung, für eine andere Id „fremd“).
   **Nicht live:** „0 Treffer ohne Sicherungsnetz“ (würde senden – Auftrag: kein POST) und „Klären“ (bräuchte einen Sage-Beleg ohne Kennung =
   Schreiben) – nur automatisiert; Klären in Sage = TS-116.21 (Gerald/Einkauf).
7. **SData-Datum:** `Belegdatum ge @2026-11-11@` wirkt; `ge '2026-11-12'` liefert **ohne Fehler 0 Treffer** (Live 2026-11-12) → [[fallstricke]] §31.

**Befund (nicht gebaut):** Fundmeldung „Beleg in Sage gefunden“ steht als Warnung im Aktivitäts-Protokoll (bewusst sichtbar); die
EF-Warnung „Take ohne OrderBy“ der Vorgangs-Abfragen ist Bestand (v1.99.4).

## QA-Nachweis Nachtrag v1.99.7 (qa-agent, 2026-11-12)

Umfang: nur „Freigabe-Nachtrag 2026-11-12: E5 Variante A, Sicherungsnetz, Q-2, Sage-Protokolle im Menü“ und „Umsetzung Nachtrag E5/Q-2/Menü – v1.99.7“.
Diff `67322b85..af0a75f2` (23 Dateien, +648/−64) plus QA-Korrektur `bb788f0e`. Build/Tests/Deploy über den ganzen Worktree (HEAD enthält v1.99.8
`557ea63b`, andere Spec, nicht geprüft; AppVersion daher 1.99.8).

### 1. Build und Tests
- HEAD `557ea63b`: `dotnet build IdealAkeWms.slnx` 0 Fehler (28 Warnungen, Bestand); `dotnet test`: `IdealAkeWms.Tests` 2646 bestanden /
  1 übersprungen (Bestand) / 0 Fehler; `IDEALAKEWMSService.Tests` 532 / 0 / 0.
- Nach der QA-Korrektur, HEAD `bb788f0e`: Build 0 Fehler; `IdealAkeWms.Tests` 2646 / 1 / 0; `IDEALAKEWMSService.Tests` **534** / 0 / 0 (+2 Fälle
  des neuen Theory-Tests, vor der Korrektur rot: „Expected client.Gesendet to be empty, but found at least one item“).

### 2. Code-Review: Befunde und Behandlung
Geprüft: `SageWareneingangService` (RunOnceAsync, PruefenAsync, AusgeschaltetAsync, Setze), `SdataWareneingangLookupReader` (Filter, Kodierung,
`FremdeKennung`), `SageBookingWorker`, `ServiceSettings.GetBool*`, `PurchaseOrdersController.SageWe` (beide Aktionen), `PurchaseOrderReceiptRepository`,
`ServiceSettingDefinitions` + `SettingValidator:38` (Min/Max), Views `SageWareneingaenge`, `ServiceSettings/Index`, `_Layout`, `Receive`, Hilfe,
Changelog, README, `nachlauf.psd1`, Tests.

Doppelbuchung – alle Wege zu `_client.SendAsync` (einziger Aufrufer `SageWareneingangService.cs`):
- Erstversand (Offen, Versuche 0): keine Suche; Versuche steigt vor dem POST, also nur nie gesendete Receipts → kein zweiter Beleg.
- Hängendes `Gesendet` → `Fehler` „unklar“ (Versuche ≥ 1) → automatisch nur mit Memo-Suche + Sicherungsnetz; gefunden = übergeben ohne POST.
- „Erneut senden“ von Hand → Offen mit Versuche > 0 → Memo-Suche (ohne Sicherungsnetz, bewusst). Payload-Fehler ohne POST (Versuche 0) → ohne Suche
  gesendet, korrekt (nie gesendet).
- Suche scheitert (jede Ausnahme außer Abbruch) → `Fehler`, kein POST (`PruefenAsync` catch); der Lookup wirft bei HTTP-Fehlern.
- Q-2: Schalter aus → Worker ruft nur `AusgeschaltetAsync` (kein Senden); Klären wird nie automatisch aufgegriffen (Abfragen nur Offen/Fehler).
- IDEAL: Schalter immer aus; Receipts entstehen dort mit „nicht nötig“ → `AusgeschaltetAsync` findet nichts, kein Protokoll (DEV IDEAL: 0 Einträge
  `SageWareneingang` seit dem Deploy). Keine Nebenwirkung.
- Endlosschleifen: „mehrere Treffer“ zählt als Versuch (Grenze greift). Suche scheitert / Payload-Fehler nach früherem Versuch zählen nicht → Wiederholung
  alle `Abstand` Minuten ohne Ende, aber nie ein POST (nur enge GETs) – H-1.

Befunde:
- **F-1 (Mittel, behoben `bb788f0e`): Wettlauf Web/Dienst während der Sage-Suche → möglicher zweiter Beleg.** Der Dienst lädt eine `Fehler`-Gruppe,
  sucht in Sage (Sekunden) und speichert danach `Gesendet` über den geladenen Stand (kein Concurrency-Token). Markiert der Mensch in dieser Zeit „Als
  übergeben“ (etwa weil der Einkauf den Beleg ohne Kennung von Hand angelegt hat), überschrieb der Dienst das und sendete trotzdem (bzw. überschrieb mit
  „Klären“). Nachgewiesen mit neuem Test `E5_WaehrendDerSucheVonHandAlsUebergeben_NichtGesendet_NichtUeberschrieben` (zwei Kontexte auf derselben
  InMemory-DB; vorher rot). Korrektur: nach `PruefenAsync` den Status der Gruppe neu lesen; geändert → überspringen, Warnung im Protokoll. Restfenster
  im Millisekundenbereich bis zum Speichern (`ponytail:`-Kommentar; ganz schließen nur mit rowversion = Migration).
- **F-2 (Mittel, behoben `bb788f0e`): Q-2 bei Lesefehler.** Der Worker las den Schalter mit `GetBoolSafeAsync` (DB-Fehler → `false`); ein kurzer
  Lesefehler hätte bei eingeschaltetem Sage-WE alle Offen/Fehler auf „Klären“ gesetzt (mit falschem Text „war ausgeschaltet“). Jetzt `GetBoolAsync`: der
  Fehler landet im Takt-catch, nächster Takt. Kein automatisierter Test (Worker-Schleife), Beleg per Review; DEV nach Deploy: Q-2 greift bei echtem „false“.
- **H-1 (Hinweis):** Scheitert die Suche dauerhaft (Sage weg) oder gibt es nach einem früheren Versuch einen Payload-Fehler, wiederholt der Dienst alle
  15 min ohne Grenze (Spec: „Versuche unverändert“, so gewollt). Nur GETs, kein POST; je Takt ein Protokoll-Eintrag. Nicht geändert.
- **H-2 (Hinweis):** Schalterlesen an zwei Stellen: Web `SageWareneingang.IstAn` trimmt, Dienst `GetBoolAsync` nicht. Wert kommt aus einer Bool-Auswahl
  (ADR 0020); Abweichung nur bei handeditiertem Wert mit Leerzeichen. Nicht geändert.
- **H-3 (Hinweis):** Die zwei neuen Int-Grenzen haben keinen eigenen Test; Server-Prüfung über den generischen `SettingValidator:38` (für andere Keys
  getestet), Browser-`max` live gesehen. Abstand-Einstellung ohne Warntext (nur Takt, keine eigene Außenwirkung) – vertretbar.
- **H-4 (Hinweis):** Das Repository zählt die drei „nicht übergeben“-Status ausgeschrieben statt über `SageWareneingang.NichtUebergeben` (gleicher Inhalt).
  Nicht geändert.
- Muster-Prüfung ohne Befund: kein Wert in `<script>`, kein `@Html.Raw`, kein `hidden`+`d-*`; Belegnummer serverseitig Pflicht + ≤ 30 (`maxlength`/`required`
  nur Zusatz); Bindung über Route `vorgang` (kein Array); Audit `Modified*` in beiden Repository-Methoden und im Dienst; „Klären“ als Text und Farbe.
- Brain-Changelog v1.99.7 fehlte (der Nachlauf verweist mit `Quelle` darauf) → vom QA angelegt: [[2026-11-12-v1-99-7-wareneingang-e5-auto-retry]];
  `feature-map` nachgezogen.

### 3. Beweisart je Punkt des Nachtrags
| Punkt | Beweis |
|---|---|
| 1.1 Memo-Suche, 1 Treffer → übergeben ohne POST | automatisiert (`E5_MemoGefunden_…`) + **live** (Umsetzung Punkt 6, 0 Sendeversuche; nicht wiederholt) |
| 1.1 mehr als 1 Treffer → Fehler | automatisiert |
| 1.2 Sicherungsnetz → Klären, fremde Kennung zählt nicht | automatisiert; SData-Filter live gelesen (Umsetzung 6); Klären mit echtem Sage-Beleg ohne Kennung = **Manual-UAT** TS-116.21 |
| 1.2 0/0 → erneut senden | automatisiert; live nicht (würde senden) |
| Suche scheitert → nicht senden | automatisiert; live = Manual-UAT TS-116.22 |
| 1.3 enge Filter | automatisiert (Filter-Strings) + Review |
| 1.4 Grenzen als Einstellung | automatisiert (Grenze/0); /ServiceSettings live (Anzeige, Browser lehnt 11 ab, DB unverändert); Server-Grenze über generischen Validator |
| F-1 Wettlauf | automatisiert (neu, QA) |
| 2 Q-2 Klären + Hinweis + Sperre | automatisiert + **live DEV AKE** |
| „Als übergeben markieren“ | automatisiert + **live DEV AKE** (echte Modellbindung) |
| 3 Menü-Block | **live DEV AKE + IDEAL**; Feature-Gate aus = Manual-UAT (Review: einfaches `@if`) |
| 4 DEV-Absicherung | live: nach jedem Deploy `SageWareneingangBelegAktiv`, `SageLagerbuchungAktiv`, `SageLagerbuchungUmbuchungAktiv` = false (beide DBs) |
| Sage-seitige Wirkung | Manual-UAT, nicht grün gezählt |

### 4. Geänderte Bestandstests
- `SageWareneingangServiceTests`: `Fehler_WirdNichtAutomatischWiederholt_AK16` → `Fehler_WirdNichtVorAblaufDesAbstandsWiederholt_AK16` – Verhalten
  **bewusst geändert** durch Nachtrag Punkt 1 (Variante A ersetzt B). Übrige Bestandstests: nur neue Parameter (`Wdh`, Lookup) = Typ-Änderung.
- `SageWareneingaengeListeTests`: Fixture um einen Klären-Eintrag ergänzt, Zählung 3 → 4, Schalter-Zeile „true“ – folgt aus Nachtrag Punkt 1/2.
  Kein Befund.

### 5. Checkliste
Keine Migration/kein SQL/kein FreshInstall (Enum-Wert in int-Spalte). Audit-Felder ja. Version 1.99.7 (Spec), Branch weiter auf 1.99.8; beide
`AppVersion.cs`, Anwender-Changelog, Hilfe, README ja. `docs/TESTSZENARIEN.md` TS-116.20–116.24 + Fußzeile; Testindex Zeile 116 nachgezogen.
ADR 0020: beide Int-Einstellungen mit Grenzen (freie Zahl, keine Auswahlliste), Server-Prüfung, Anzeigename, Wirkung, Verwendung, Default, Standort;
Warntext bei `MaxWiederholungen` (schreibt nach Sage). Nachlauf: zwei Einträge `v1.99.7-*` (ASCII) vorhanden.

### 6. DEV-Deploy und Testlauf
- Standort laut Frontmatter AKE; AKE **und** IDEAL deployt, Web + Dienst, Shell mit Administratorrechten, je zweimal (auf `557ea63b` und nach der
  QA-Korrektur auf `bb788f0e`); jedes Mal Exit 0, „HTTP 200, Fusszeile v1.99.8“ (= AppVersion des Branches). Danach die 5 AKE-Sync-Schalter wieder true.

| Schritt | Ausgeführt | Ergebnis | Urteil |
|---|---|---|---|
| Einstellungen angelegt | `ServiceSettings` beide DBs | `Sync:SageWareneingangMaxWiederholungen` = 3, `…WiederholAbstandMinuten` = 15 | OK |
| TS-116.23 Q-2 im Dienst | Receipt #8 per SQL auf Fehler (Versuche 1, Text „QA-Test“), Schalter aus, ein Takt | 11:50:53 Status 6 „Klären“, Text „… war ausgeschaltet … Vorher: QA-Test“, `ModifiedBy` `system:sage-wareneingang`; SyncLogs Warnung + `klaeren=1` | OK |
| TS-116.23 Hinweis Liste | `/PurchaseOrders/SageWareneingaenge` | „⚠ Sage-Wareneingang ist aus – 1 Eintrag offen, werden nicht gesendet …“, Status „Klären“, beide Knöpfe | OK |
| TS-116.23 Erneut senden gesperrt | Knopf geklickt | „Nicht eingereiht: Der Sage-Wareneingang ist ausgeschaltet …“, Status unverändert | OK |
| TS-116.21 Als übergeben | Beleg-Nr. „QA-2027-TEST“ | „1 Position(en) als an Sage übergeben markiert … Es wird nichts gesendet.“, Status übergeben, `Modified*` = Gerald/AKENB, Hinweis weg | OK |
| TS-116.23 Hinweis /ServiceSettings | #8 kurz auf Klären | Warnung mit Link „Zu den Sage-Wareneingängen“ | OK |
| TS-116.22 Grenzen | /ServiceSettings, Wiederholungen = 11, Speichern | Browser: „Wert muss kleiner als oder gleich 10 sein“, DB unverändert 3/15; Anzeigename, Wirkung, Verwendung, Standard, Standort, Warntext sichtbar | OK |
| TS-116.24 Menü | AKE und IDEAL, Menü „Lager“ | Trennlinie, „Sage-Lagerbuchungen“, „Sage-Wareneingänge“, Trennlinie; Seitentitel „Sage-Wareneingänge“; IDEAL: „Keine Sage-Wareneingänge.“, kein Hinweis | OK |
| TS-116.24 Feature-Gate aus | – | Gate auf DEV nicht umgeschaltet | NICHT MÖGLICH (offen, Mensch) |
| Keine Sage-Wirkung | Dienst-Log AKE, `SageBookingQueueItems` | letzter POST `StandardBelegService` 09:08:26 (freigegebener Schreibtest), danach keiner; 0 neue Queue-Zeilen | OK |
| TS-116.20 live | – | im Dev-Lauf erledigt (Umsetzung 6), laut Auftrag nicht wiederholt | – |
| TS-116.21 Klären mit Sage-Beleg, TS-116.22 Sage nicht erreichbar, 0/0 → senden | – | schreibt nach Sage bzw. braucht Sage-Beleg ohne Kennung | NICHT MÖGLICH (Regel) |

Ausgangsstand wiederhergestellt: Receipt #8 wieder Status 0, Versuche 0, Vorgang/Fehler/Beleg/Am/Modified* NULL (vorher so gelesen). #4/#7 unverändert
(übergeben, 2027-398675/398676). Sage-Schalter aus.

### 7. Publish
- `C:\git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish` vorher geleert, neu: `IDEALAKEWMSWeb`, `IDEALAKEWMSWebService`, `SQL`,
  `deploy`, `publish-info.txt` (Version 1.99.8, Commit `bb788f0e8da5239cc0c9bbdf4e96ef7f54355e81`, Branch `feature/2026-08-07-ideal-teile-1-5`,
  Erstellt 2026-11-12 11:54). Zu deployen: Web und Dienst, keine neue Migration.

**Urteil:** Testbereit (v1.99.7, im Paket v1.99.8).

### Manuelle Test-Checkliste (offen, Mensch) – ergänzt die Listen aus v1.99.4–v1.99.6 oben
1. TS-116.21 am AKE-Test (Gerald/Einkauf): ELL-Beleg aus derselben Bestellung ohne WMS-Kennung, WMS-Eintrag auf Fehler (Versuche 1, > 15 min)
   → „Klären“ mit Belegnummer, kein Senden; danach „Als übergeben markieren“.
2. TS-116.22: SData-Adresse falsch → „Sage-Prüfung … fehlgeschlagen … nicht gesendet“, Versuche unverändert; MaxWiederholungen = 0 → keine Wiederholung.
3. Ein echter Fehler mit 0/0 Treffern am AKE-Test → genau ein neuer Beleg (schreibt nach Sage, nur Gerald).
4. TS-116.24 mit `WareneingangBestellungAktiv` aus: im Block nur „Sage-Lagerbuchungen“.
5. Prod-Deploy: Offene/fehlerhafte Sage-WE bei Schalter aus stehen danach auf „Klären“ (Q-2) – Einkauf informieren (Nachlauf).
6. Rückbau der Sage-Test-Belege 2027-398675/398676 (Gerald; „Memo gefunden“ ist erledigt).
7. Testsystem-Lauf mit dem Paket aus `publish` (v1.99.8), danach Schranke 2.
## Hinweis Koordination 2026-11-12: Rückbau entfällt – Kennung `WE#<Id>#` ist nur je WMS-Datenbank eindeutig
- Gerald 2026-11-12: Die Test-Belege 2027-398675 (`WE#4#`) und 2027-398676 (`WE#7#`) im Sage-AKE-Test werden **nicht** zurückgebaut.
- **Befund:** Die Kennung `WE#<Id>#` nutzt die Id aus der WMS-Datenbank. Gegen dieselbe Sage-Testdatenbank (`ake_TEST2026`) schreiben bzw.
  suchen künftig mehrere WMS-Datenbanken: DEV-AKE, AKE-Test, AKE Test-IDEAL; nach einem DEV-Restore beginnen die Ids neu. Ein
  Wareneingang mit Id 4 oder 7 in einem dieser Systeme würde in E5 den alten Test-Beleg finden, als „übergeben“ gelten und **nie gesendet**
  werden. In der Produktion schreibt je Sage-Mandant genau ein WMS, dort ist das unkritisch.
- **Vorschlag (Entscheidung Gerald offen):** Die Kennung bekommt ein System-Kürzel als Einstellung, z. B. `WE#AT#7#` für AKE-Test,
  `WE#DEV#7#` für DEV, ohne Kürzel = heutiges Format für Prod. Suche und Sicherungsnetz nutzen dasselbe Format. Bis dahin auf DEV und den
  Testsystemen den Sage-WE nur bewusst einschalten.

## Freigabe-Nachtrag 2026-11-12 14:20 (Gerald): System-Kürzel in der Sage-Kennung

Gilt vor dem QA-Abschnitt; setzt den Hinweis direkt darüber um. Version: nächste freie im Bündel.
1. Neue Einstellung (ADR 0020) „System-Kürzel für Sage-Kennungen“, z. B. `DEV`, `AT` (AKE-Test), `ATI` (AKE Test-IDEAL), `IT` (IDEAL-Test).
   **Leer = heutiges Format** `WE#<Id>#` (Produktion). Erlaubt sind nur Großbuchstaben und Ziffern, höchstens 5 Zeichen, serverseitig geprüft.
2. Mit Kürzel lautet die Kennung `WE#<Kürzel>#<Id>#` im Kopf- und im Positions-Memo. Die Suche in E5 und das Sicherungsnetz („Belege mit der
   Kennung eines anderen WMS-Wareneingangs zählen nicht“) verwenden dasselbe Format. Ein `WE#7#` ohne Kürzel gilt bei gesetztem Kürzel als fremd.
3. Gilt nur für neue Sendungen. Bereits übergebene Einträge behalten ihre Kennung; die Suche für einen Eintrag nutzt die Kennung, mit der er
   gesendet wurde. Das Feld dafür beim Senden speichern.
4. Prüfen, ob die Sage-Lagerbuchungen (`SM#<id>#`, `SageBookingCorrelation`) dasselbe Problem haben. Wenn ja, gleiches Kürzel verwenden.
   Sonst im Bericht vermerken.
5. Update-Skript/Manifest: Info-Eintrag „Kürzel je Testsystem setzen, Produktion leer lassen“. Die DEV-Absicherung setzt auf DEV das Kürzel `DEV`.
6. Tests: Kennung mit und ohne Kürzel, Suche findet `WE#7#` bei Kürzel `DEV` nicht.

## Umsetzung Nachtrag 14:20 (v1.99.9)

Dev-Lauf 2026-11-12 im Bündel `feature/2026-08-07-ideal-teile-1-5`. Commits `115e8a09` (Code, Migration, SQL/144, Manifest, Tests),
`c47f0136` (Version 1.99.9, Changelog, Hilfe, README, TS-116.25–116.30). Brain-Changelog [[2026-11-12-v1-99-9-sage-system-kuerzel]].
1. **Einstellung** `Sync:SageSystemKuerzel` (String, Default leer, Kategorie „Sage-Lagerbuchung“, Standort beide, Anzeigename „System-Kürzel für
   Sage-Kennungen“, Warnhinweis „In der Produktion LEER lassen … wirkt nur auf neue Sendungen“). Serverprüfung über ein neues, generisches
   `SettingDefinition.Muster` im `SettingValidator` (String-Fall): `^([A-Z][A-Z0-9]{0,4})?$`.
   **Abweichung (bewusst, enger als der Nachtrag):** das Kürzel muss mit einem **Buchstaben** beginnen. Ein reines Ziffern-Kürzel `12` ergäbe
   `WE#12#7#`, und die Suche eines Systems ohne Kürzel nach `%WE#12#%` träfe genau diesen Beleg – die Regel wäre wieder gebrochen.
2. **EINE Formatfunktion** `SageSystemKuerzel.Kennung(praefix, kuerzel, id)` im Web-Projekt (`IdealAkeWms/Services/SageSystemKuerzel.cs`); der Dienst
   referenziert das Web-Projekt (`IDEALAKEWMSService.csproj:28`). `SageWareneingang.Memo(id, kuerzel)` und `SageBookingCorrelation.Marker(id, kuerzel)`
   nutzen sie. Kopf- und Positions-Memo: `WE#<K>#<Id>#`, ohne Kürzel unverändert `WE#<Id>#`.
3. **Gespeicherte Kennung:** neue Spalten `PurchaseOrderReceipts.SageWeKennung` und `SageBookingQueueItems.SageKennung` (je nvarchar(20) NULL;
   längste Kennung `WE#` + 5 + `#` + 10 Ziffern + `#` = 20), **eine** Migration `20261112141208_AddSageKennungen`, **SQL/144_AddSageKennungen.sql**
   (COL_LENGTH-Guards, History separat), FreshInstall an beiden Stellen. Regel an einem Ort: `SageWareneingang.Kennung(r, kuerzel)` =
   gespeicherte Kennung; sonst nie gesendet (`SageWeVersuche == 0`) → aktuelles Kürzel; sonst (vor v1.99.9 gesendet) → altes Format.
   Der Dienst speichert die Kennung beim Wechsel auf `Gesendet` (vor `Versuche++`). E5-Suche, „Ergebnis unklar“-Text, Sicherungsnetz und die
   Spalte „Memo“ der Liste (Web liest das Kürzel aus /ServiceSettings, gleiche Funktion) nutzen sie. Lagerbuchung analog
   `SageBookingCorrelation.Kennung(item, kuerzel)` (nie gesendet = `SentAt` leer), gespeichert in `MarkSentAsync(id, kennung)`.
4. **Sicherungsnetz/FremdeKennung:** Regex `WE#(?:[A-Z][A-Z0-9]{0,4}#)?\d+#`; jede Kennung, die nicht exakt die eigene ist, ist fremd
   (`WE#7#` bei eigener `WE#DEV#7#`, `WE#AT#7#`, `WE#DEV#70#`). LIKE-Richtungen per Test belegt: `%WE#7#%` trifft `WE#DEV#7#` nicht und umgekehrt,
   `%WE#AT#7#%` trifft weder `WE#AT#17#` noch `WE#ATI#7#`, `%WE#7#%` nicht `WE#17#`/`WE#70#`.
5. **SM#-Prüfung – Ergebnis: dasselbe Problem besteht, umgesetzt.** Beleg (Stand vor der Änderung, `bb788f0e`): `SageBookingWorker.cs:161`
   Recovery hängender `Gesendet` (`lookup.ExistsAsync(item.StockMovementId)` → Treffer = `MarkConfirmedAsync` „Recovery: Buchung in Sage bereits
   vorhanden“) und `:214` Requeue (`item.SentAt != null` → Treffer = `MarkConfirmedAsync` „Requeue: …“, `continue` ohne Senden); Lookup
   `Memo like '%SM#<id>#%'` über `KHKLagerplatzbuchungen` (`SageBuchungLookupReader.cs:15`) bzw. SData `LagerplatzbuchungenInfo`
   (`SdataBuchungLookupReader.Where`). Die Id ist `StockMovement.Id` der jeweiligen WMS-DB – bucht ein anderes WMS gegen dieselbe
   Sage-Testdatenbank eine Bewegung mit gleicher Id, gilt die eigene als bestätigt und wird **nie gebucht**. Daher `SM#<K>#<id>#`,
   `ISageBuchungLookupReader.ExistsAsync(string marker)` (View, SData, Weiche), Spalte `SageKennung` in derselben Migration.
6. **Dienst liest das Kürzel ohne Safe-Variante** (`ServiceSettings.GetValueAsync` + `SageSystemKuerzel.Pruefen`): DB-Lesefehler oder ungültiger
   Wert (von Hand in der DB, z. B. `dev` oder `%`) → Ausnahme, Takt abgebrochen, nichts gesendet, Fehlermail wie bei anderen Takt-Fehlern.
   Grund: Senden ohne Kürzel wäre genau die Verwechslung; ein `%` wäre in der LIKE-Suche ein Platzhalter.
7. **Manifest** `nachlauf.psd1`: Schema-Eintrag `SQL/144` (automatisch, additiv, nach SQL/143) und Einstellungs-Eintrag `v1.99.9-SystemKuerzel`
   („Kürzel je Testsystem setzen (DEV, AT, ATI, IT), Produktion LEER lassen“). Drift-Test `SchemaReihenfolge` um SQL/144 ergänzt.
8. **DEV-Absicherung** (außerhalb des Repos, nicht ausgeführt): `D:\Dev\scripts\dev-absicherung-ake.sql` und `-ideal.sql` setzen bzw. legen
   `Sync:SageSystemKuerzel = 'DEV'` an; Backups `*.bak-20261112b`.
9. **Tests:** Dienst 564/564 (vorher 534; +30: Kennung mit/ohne Kürzel, gespeicherte geht vor, LIKE-Matrix 9 Fälle, FremdeKennung 7 Fälle,
   Erstversand mit/ohne Kürzel speichert Kennung, Wiederholung sucht gespeicherte statt aktueller, Altbestand altes Format, **Kürzel DEV findet
   fremden `WE#<Id>#` nicht und sendet**, SM# Marker/Kennung/LIKE/Payload/SData-Where). Web 2660 bestanden / 1 übersprungen (Bestand) / 0
   (vorher 2646; +14: Validator-Grenzen 12 Fälle, Katalog/Pruefen, Liste Memo). Bestandstests nur Signatur angepasst (Kennung statt Id).
   `dotnet build IdealAkeWms.slnx` 0 Fehler, 24 Warnungen (Bestand).
10. **TS-Kapitel:** TS-116.25–116.30 als Erweiterung von TS-116 (Nachtrag derselben Spec, Sage-WE-Kapitel; SM# gehört zum selben Nachtrag).
    **Nur Manual-UAT:** Wirkung in Sage (116.26, 116.29 schreiben nach Sage – nur Gerald), Maske /ServiceSettings (116.25), 116.27 nur mit
    werfendem Sende-Client. Keine Sage-Aufrufe in diesem Lauf.

**Befunde (nicht gebaut):** Kein `pattern`-Attribut im Eingabefeld – die Prüfung ist rein serverseitig (Fehlermeldung beim Speichern). Ein
ungültiger Wert in der DB löst je Takt (20 s) eine Fehlermail aus, solange er steht (bestehendes Verhalten des Takt-catch).

## QA-Nachweis Nachtrag v1.99.9 (qa-agent, 2026-11-12)

Geprüft: Commits `115e8a09`, `c47f0136` (Diff gegen `bb788f0e`), QA-Korrektur `4c1b5523`. Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`.

### 1. Build und Tests (nach QA-Korrektur, HEAD `4c1b5523`)
- `dotnet build IdealAkeWms.slnx`: 0 Fehler, 28 Warnungen (Bestand, u. a. CS8601/CS8321).
- `dotnet test`: **IdealAkeWms.Tests 2661 bestanden / 1 übersprungen (Bestand) / 0 fehlgeschlagen**; **IDEALAKEWMSService.Tests 564 / 0 / 0**.
  Vor der QA-Korrektur 2660/1/0 und 564/0/0; +1 Test `SaveSettings_SystemKuerzel_GetrimmtGespeichert_KleinbuchstabenAbgelehnt`.

### 2. Code-Review (selbst, in diesem Lauf)
Geprüft: `SageSystemKuerzel` (Format/Prüfung), `SageWareneingang.Kennung/Memo/UnklarText`, `SageBookingCorrelation.Kennung/Marker/LikePattern`,
`SageWareneingangService.RunOnceAsync`/`PruefenAsync` (Hängend-Text, E5-Suche, Sicherungsnetz, Speichern der Kennung vor `Versuche++`),
`SageWareneingangPayloadBuilder.Build`/`PositionsMemo`, `SageBookingWorker` (Recovery, Payload, Requeue, `MarkSentAsync`),
`SageBuchungLookupReader`, `SdataBuchungLookupReader.Where`, `BuchungLookupQuelleSwitch`, `SdataWareneingangLookupReader.WhereMemo/FremdeKennung`,
`SageBookingQueueRepository.MarkSentAsync/RequeueAsync`, `PurchaseOrderReceiptRepository.SageWeErneutSendenAsync`, `ServiceSettingsController.SaveSettings`,
`SettingValidator`, Liste `PurchaseOrdersController.SageWe`, Migration, `SQL/144`, `00_FreshInstall.sql`, `nachlauf.psd1`.
- **Kennungsformat an EINER Stelle:** `SageSystemKuerzel.Kennung`; `SageWareneingang.Memo` und `SageBookingCorrelation.Marker` rufen nur sie. OK.
- **Gespeicherte Kennung (Suche/Anzeige/Sicherungsnetz/Recovery/Requeue):** Regel je eine Funktion (`SageWareneingang.Kennung`,
  `SageBookingCorrelation.Kennung`): gespeichert > nie gesendet (aktuelles Kürzel) > Altbestand (altes Format). Alle Leser (E5-Suche, Unklar-Text,
  Sicherungsnetz, Liste, Payload Kopf und Position, Recovery, Requeue, MarkSent) gehen über diese Funktionen. Die „nie gesendet“-Merkmale setzt kein
  Rücksetzweg zurück (`RequeueAsync` setzt nur `AttemptCount=0`, nicht `SentAt`, `SageBookingQueueRepository.cs:127-138`; „Erneut senden“ lässt
  `SageWeVersuche` stehen, `PurchaseOrderReceiptRepository.cs:73-85`) – ein Altbestand fällt also nie auf das neue Kürzel. OK.
- **FremdeKennung-Regex** `WE#(?:[A-Z][A-Z0-9]{0,4}#)?\d+#` mit exaktem Vergleich gegen die eigene Kennung: `WE#7#` bei eigener `WE#DEV#7#` fremd,
  `WE#DEV#70#` fremd, eigene nicht fremd; Kürzel-Kennungen werden als Ganzes erfasst (keine Teiltreffer). OK (7 Testfälle).
- **Validator `^([A-Z][A-Z0-9]{0,4})?$` vs. Nachtrag „Großbuchstaben und Ziffern“:** enger als freigegeben (Buchstabe vorne Pflicht). Begründung trägt:
  ein Ziffern-Kürzel `12` erzeugt `WE#12#7#`, und ein System ohne Kürzel sucht für Receipt 12 `%WE#12#%` – trifft genau diesen fremden Beleg.
  **Bewertung: akzeptiert**; der Hinweistext der Maske nennt die Regel. Für Gerald zur Kenntnis.
- **SQL/144:** COL_LENGTH-Guards je Spalte in eigenem Batch, History separat, Vorbedingungs-Guard mit `NOEXEC`. Auf beiden DEV-DBs nach der
  EF-Migration erneut ausgeführt: rc 0, keine Ausgabe = idempotent. **FreshInstall** an beiden Stellen (Schema beider Tabellen + MigrationId mit
  COL_LENGTH-Bedingung). **Migration** rein additiv (2× nullable `AddColumn`). **Manifest** `SQL/144` + `v1.99.9-SystemKuerzel`, Drift-Test grün.
- **Befund QA-1 (behoben, `4c1b5523`):** `ServiceSettingsController.SaveSettings` speicherte bei `SettingType.String` den **Rohwert** statt des geprüften,
  getrimmten Werts (` ATI ` wäre mit Leerzeichen in die DB gegangen; der Validator-Kommentar sagt „gespeichert wird der getrimmte Wert“). Ohne
  funktionale Folge (Dienst, Format und Liste trimmen), aber Zustand und Doku widersprachen sich. Fix: String **mit Muster** speichert `Kanonisch`;
  Test ergänzt; auf DEV belegt (` ATI ` → `[ATI]`).
- **Bewertung der offenen Umsetzer-Befunde:**
  - *Dienst liest das Kürzel ohne Safe-Variante:* **richtig so** (Senden ohne Kürzel wäre genau die Verwechslung; `%` wäre LIKE-Platzhalter).
    Nebenwirkung zur Kenntnis: beide Schritte liegen im selben `try` (`SageBookingWorker.cs:64-74`) – wirft der Lagerbuchungs-Schritt, entfällt in diesem
    Takt auch der Sage-WE-Schritt bzw. Q-2; bei ungültigem Wert würde der WE-Schritt ohnehin werfen. Ein ungültiger Wert ist nur am Katalog vorbei
    möglich: von Hand in der DB **oder** über die Freitext-Aktionen `ServiceSettings/Create|Edit` (`ServiceSettingsController.cs:211,233` prüfen keine
    Katalog-Keys – Bestandslücke für alle Einstellungen, nicht aus diesem Diff; die Fehlermail je Takt macht es sichtbar).
  - *Kein `pattern`-Attribut im Eingabefeld:* **akzeptiert** – ADR 0020 verlangt die Serverprüfung, die Fehlermeldung ist sichtbar (DEV belegt); ein
    Client-`pattern` wäre eine zweite Implementierung derselben Regel.
- Weitere Defektklassen: keine `<script>`-Einbettung, kein `hidden`+`d-*`, kein stiller Fallback (Kürzel-Lesen im Web nur für die Anzeige), keine
  parallelen Arrays, Regel nicht doppelt implementiert (die FremdeKennung-Erkennung erfasst bewusst jede Form).
- Hinweis (kein Befund): Die Beschreibung von `Sync:SageWareneingangMaxWiederholungen` nennt weiter `WE#<Id>#` ohne Kürzel – textlich ungenau, harmlos.

### 3. Beweisart je Punkt des Nachtrags
| Punkt | Beweis |
|---|---|
| 1 Einstellung + Serverprüfung | automatisiert (`SettingValidatorTests` 12 Fälle, `ServiceSettingsControllerTests` neu) **und** DEV-Maske (TS-116.25 OK) |
| 2 Format Kopf/Position, Suche, Sicherungsnetz | automatisiert (`SageWareneingangServiceTests`, LIKE-Matrix als String-Contains); die echte LIKE-/SData-Auswertung in Sage ist **Manual-UAT** (116.26/116.27) |
| 3 nur neue Sendungen, gespeicherte Kennung | automatisiert (Erstversand speichert, Wiederholung sucht gespeicherte, Altbestand altes Format); Spalten in SQL Server und Liste auf DEV belegt (116.28 Anzeige) |
| 4 SM# gleiches Problem | Analyse belegt (Umsetzung Punkt 5); Marker/Kennung/Payload/Where automatisiert; Worker-Pfad Recovery/Requeue ohne Unit-Test, Wirkung **Manual-UAT** 116.29 |
| 5 Manifest + DEV-Absicherung | Drift-Test grün; `Sync:SageSystemKuerzel = DEV` auf beiden DEV-DBs nach Deploy belegt |
| 6 Tests mit/ohne Kürzel, `WE#7#` bei DEV nicht gefunden | automatisiert (`KuerzelDev_FremderBelegOhneKuerzel_…`, `OhneKuerzel_Erstversand_AltesFormat_Gespeichert`) |

### 4. Geänderte Bestandstests
`SageBookingCorrelationTests`, `SageWareneingangServiceTests` (Fake `NachMemoAsync(string)`, `PositionsMemo("WE#7#",…)`, `WhereMemo("WE#7#")`,
`FremdeKennung(…, "WE#7#")`), `BuchungLookupSdataTests` (`ExistsAsync`/`Where` mit Marker statt Id): **nur Signatur/Typ**, erwartete Werte unverändert
(`WE#7#`, `SM#42#`). `NachlaufManifestDriftTests`: `SQL/144` in der Reihenfolge = gewollte Erweiterung (Nachtrag Punkt 5). Kein „Sonstiges“.

### 5. Checkliste / ADR 0020
Migration + SQL/144 + FreshInstall 2× + Manifest: OK. Audit: keine neue Entität; Kennung wird in bestehenden Schreibwegen gesetzt. Version 1.99.9 in
beiden `AppVersion.cs`, Anwender-Changelog, Hilfe, README, Brain-Changelog [[2026-11-12-v1-99-9-sage-system-kuerzel]], TS-116.25–116.30 in
`docs/TESTSZENARIEN.md` und im Testindex. ADR 0020: Freitext mit festem Format (Auswahlliste nicht möglich), Serverprüfung, Anzeigename, Wirkung,
Verwendung, Default, Standort, Warnhinweis vorhanden (DEV-Maske gesehen). OK.

### 6. DEV-Deploy und Test
- `dev-deploy.ps1 -Standort AKE` und `-Standort IDEAL`, Quelle Worktree (HEAD `4c1b5523`), elevated: je **Exit 0**, „HTTP 200, Fusszeile v1.99.9“,
  Web + Dienst. Dienst-Logs: `SageBookingWorker gestartet. Version 1.99.9` auf beiden, keine `[ERR]`.
- `SQL/144` auf `AKE_WMS_DEV` und `IDEAL_WMS_DEV` erneut ausgeführt: rc 0, No-op; je Spalte `COL_LENGTH = 40`, MigrationId vorhanden.
- DEV-Absicherung: `Sync:SageSystemKuerzel = DEV` auf beiden; `SageLagerbuchungAktiv`, `SageWareneingangBelegAktiv`, `ErrorNotification:Enabled`,
  `Notifications:MeldebestandEnabled` = false. Auf Anweisung des Aufrufers danach die 5 AKE-DEV-Lese-Syncs wieder `true`
  (`Sync:ArticlesEnabled`, `ArtikelLieferantEnabled`, `KreditorenEnabled`, `DebitorenEnabled`, `EinkaufBestellungenEnabled`).

| Schritt | Ausgeführt / Ergebnis | Status |
|---|---|---|
| 116.25 Maske | AKE-DEV /ServiceSettings: Anzeigename, Wirkung, „Verwendet in“, „Standard: leer; Standort: beide“, Warnhinweis „In der Produktion LEER lassen …“ sichtbar. `dev`, `12`, `ABCDEF`, `D%` je abgelehnt („… ist nicht erlaubt – erlaubt: leer oder 1 bis 5 Zeichen …“), DB blieb `DEV`; ` ATI ` gespeichert als `ATI`, danach `DEV` gespeichert | OK |
| 116.26 neuer Sage-WE mit DEV | schreibt nach Sage | NICHT MÖGLICH (nur Gerald) |
| 116.27 fremder Beleg | braucht werfenden Sende-Client; laut Auftrag kein Harness | NICHT MÖGLICH (automatisiert belegt) |
| 116.28 Anzeige | Liste „Sage-Wareneingänge“ AKE-DEV mit vorübergehend gesetzten Daten: Receipt 4 `SageWeKennung='WE#AT#4#'` → Memo `WE#AT#4#`; Receipt 7 `Versuche=0` → `WE#DEV#7#`; zurückgesetzt (NULL/1) → `WE#4#`, `WE#7#` (Altbestand) | OK |
| 116.28 Suche nach Kürzelwechsel | braucht Sage-Lesen bei eingeschaltetem Sage-WE | NICHT MÖGLICH (Mensch) |
| 116.29 SM# mit Kürzel | schreibt nach Sage | NICHT MÖGLICH (nur Gerald) |
| 116.30 leer | automatisiert | OK (Test) |

### 7. Publish
`C:\git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish`: `IDEALAKEWMSWeb` + `IDEALAKEWMSWebService` (Ordner vorher geleert),
`SQL\` (mit `144_AddSageKennungen.sql`), `deploy\`, `publish-info.txt` (Version 1.99.9, Commit `4c1b55238e7e333db4212a69dc86b5cf8752059d`). Beide Exit 0.

### Manuelle Checkliste Nachtrag v1.99.9 (offen, Mensch)
1. Testsystem-Lauf mit dem Paket aus `publish` (v1.99.9): Update-Skript führt `SQL/144` aus; danach je Testsystem das Kürzel setzen (AT, ATI, IT),
   **bevor** Sage-WE bzw. Lagerbuchung dort eingeschaltet sind.
2. TS-116.26 (nur Gerald, Sage-Test): neuer Sage-WE mit Kürzel – Kopf- und Positions-Memo `WE#<K>#<Id>#` in Sage, `SageWeKennung` gespeichert.
3. TS-116.27: fremder Beleg `WE#4#`/`WE#7#` wird bei Kürzel nicht gefunden.
4. TS-116.28 Suche: Eintrag aus 2. auf Fehler, Kürzel ändern → Suche mit der gespeicherten Kennung findet den eigenen Beleg, kein zweiter Beleg.
5. TS-116.29 (nur Gerald): Lagerbuchung mit `SM#<K>#<id>#`, Requeue findet die eigene Buchung; Negativ `dev` von Hand in der DB → Dienst sendet nichts.
6. Kenntnisnahme: das Kürzel muss mit einem Buchstaben beginnen (enger als der Nachtrag, Begründung oben).
7. Schranke 2: Merge des Bündels.
