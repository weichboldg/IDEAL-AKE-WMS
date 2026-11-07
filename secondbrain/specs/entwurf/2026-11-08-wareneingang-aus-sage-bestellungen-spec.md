---
type: spec
title: Wareneingang auf Basis der Sage-Bestellungen (offene Bestellungen lesen, Vorschau X Wochen, Wareneingang zu Bestellung)
slug: 2026-11-08-wareneingang-aus-sage-bestellungen-spec
status: Entwurf
created: 2026-11-07
updated: 2026-11-07
source_backlog: "[[2026-11-07-wareneingang-aus-sage-bestellungen]]"
buendel: "[[2026-11-08-buendel-uebersicht]]"
version: "1.84.0"
sql_nummern: "112-116 (reserviert; genutzt 112, 113, 114 nur E5)"
testszenario: "TS-116"
task: ""
worktree: ""
branch: ""
standort: AKE
affected_code:
  - "NEU IdealAkeWms/Models/SagePurchaseOrderLine.cs (Cache offene EK-Positionen, kein AuditableEntity, Präzedenz ProductionOrderPosition.cs:10-13)"
  - "NEU IdealAkeWms/Models/PurchaseOrderReceipt.cs (WE-Zeile mit Bestellbezug, AuditableEntity, 1:1 zu StockMovement)"
  - "NEU IdealAkeWms/Data/Repositories/{ISagePurchaseOrderLineRepository,SagePurchaseOrderLineRepository}.cs (wird von Einkauf wiederverwendet)"
  - "NEU IdealAkeWms/Data/Repositories/{IPurchaseOrderReceiptRepository,PurchaseOrderReceiptRepository}.cs"
  - "NEU IDEALAKEWMSService/Services/Sdata/EinkaufBestellungenSyncService.cs (SData-only, Muster ArtikelLieferantSyncService.cs:25-188)"
  - "IDEALAKEWMSService/Workers/SyncWorker.cs (neuer Block nach Artikel-Lieferanten :463-473)"
  - "IDEALAKEWMSService/Program.cs (DI-Registrierung, neben :81-82)"
  - "IdealAkeWms/Services/SyncLogger/SyncLogServices.cs (Name + deutscher DisplayName)"
  - "IdealAkeWms/Models/ServiceSettingDefinitions.cs (Sync:EinkaufBestellungenEnabled; E5: SageWareneingangBelegAktiv)"
  - "IdealAkeWms/Models/AppSettingDefinitions.cs + AppSettingKeys.cs (WareneingangBestellungAktiv, OffeneBestellungenVorschauWochen; Gruppe Lager und Buchungen :71-84)"
  - "NEU IdealAkeWms/Controllers/PurchaseOrdersController.cs + Views/PurchaseOrders/{Index,Receive}.cshtml + ViewModels"
  - "IdealAkeWms/Models/StockMovement.cs (nur Navigation PurchaseOrderReceipt, keine Spalte)"
  - "IdealAkeWms/Data/ApplicationDbContext.cs (zwei DbSets + Konfiguration)"
  - "IdealAkeWms/Views/Shared/_Layout.cshtml (Lager-Menü :104-108)"
  - "E5 (später): IdealAkeWms/Services/SageBookingEnqueueDecision.cs:64-72, Data/Repositories/SageBookingEnqueueingStockMovementRepository.cs:65-67, SageBookingQueue EnqueueMissingAsync (SageBookingWorker.cs:115), NEU Worker Sage-Wareneingangsbeleg"
  - "SQL/112_AddSagePurchaseOrderLines.sql, SQL/113_AddPurchaseOrderReceipts.sql, (E5) SQL/114_AddPurchaseOrderReceiptSageWe.sql, SQL/00_FreshInstall.sql"
  - "docs/TESTSZENARIEN.md (TS-116), README.md (AppSettings/ServiceSettings), Views/Help/Index.cshtml + Changelog.cshtml, beide AppVersion.cs (1.84.0)"
open_questions:
  - "1 Doppelbuchung Bestand: Wie wird in Stufe 1 die Bestellposition in Sage geschlossen, ohne dass der Sage-Bestand ein zweites Mal steigt? (Konflikt: Entscheidung 'Einbuchung inkl. Sage-Lagerbuchungs-Queue wie heute' + Sage-Einkauf bucht WE-Beleg mit Lagerbuchung = doppelt; Lagerbestand-Sync zieht es ins WMS nach)"
  - "2 Anzeige 'Rest offen': offen laut Sage minus im WMS erfasst (Receipts ohne Sage-WE) oder nur offen laut Sage? (hängt an 1)"
  - "3 Belegarten: EBB (Bestellung) und EBZ (Rahmenabruf) einbeziehen, EBD (RMA-Anmeldung) nicht?"
  - "4 Überlieferung erlauben mit Pflicht-Bestätigung oder sperren; Buchen trotz Liefersperre erlaubt?"
  - "5 Ziel-Lagerplatz: Sage-Platz vorbelegen und änderbar lassen?"
  - "6 Scannen: Was steht auf dem Papier (Bestellnummer als Barcode auf Bestellung/Lieferschein, Format)?"
  - "7 IDEAL: bleibt die Funktion bis zum SData-Endpunkt (G2) aus, ohne eigenen View-Reader?"
  - "8 Etikettendruck beim Wareneingang: außerhalb dieses Epics?"
  - "9 DPS-MDE: Hat/lizenziert DPS ein Wareneingangs-Modul (USER_DPSbsMDE*-Felder)? Blockiert E5"
  - "10 Chargen-/seriennummernpflichtige Positionen: Stufe 1 nur Hinweis, Stufe 2 nicht automatisch an Sage?"
  - "11 Aktualität: genügt der Cache je Dienst-Zyklus mit Anzeige 'Stand', ohne Live-Nachladen?"
  - "12 Rollen: keine neue Rolle – Vorschau RequireStockReadAccess, Wareneingang RequireStockAccess?"
  - "13 E5 (Sage-Wareneingangsbeleg) in diesem Epic oder eigene Folge-Spec?"
epic: true
etappen:
  - "E1 Lesen offene Sage-Bestellungen: Cache SagePurchaseOrderLines + SData-Abgleich (SQL/112)"
  - "E2 Maske 'Offene Bestellungen' (Vorschau X Wochen, Listen-View-Pattern, Feature-Schalter, Menü)"
  - "E3 Wareneingang zu Bestellung: Maske Receive + PurchaseOrderReceipts (SQL/113) + Einbuchung über IStockMovementRepository"
  - "E4 Abschluss: Version 1.84.0, Hilfe, Changelog, README, TS-116"
  - "E5 (blockiert) Sage-Wareneingangsbeleg per StandardBelegService hinter SageWareneingangBelegAktiv (SQL/114)"
deploy:
  web: true
  service: true
  migration: true
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
---

# Wareneingang auf Basis der Sage-Bestellungen

Bündel [[2026-11-08-buendel-uebersicht]] (Teil B, eigener Dev-Lauf). Backlog [[2026-11-07-wareneingang-aus-sage-bestellungen]].
Wiederverwendung: SData-Infrastruktur aus [[2026-11-06-sage-sdata-stammdaten-sync-spec]], Kreditoren/Artikel-Lieferanten (v1.73.0),
bestehender Wareneingang `StockMovements/Inbound` + `InboundBulk`. Nachbar-Specs: [[2026-11-08-einkauf-bestellvorschlaege-spec]] (Teil C,
nutzt den Lesezugriff dieses Epics), [[2026-11-08-qualitaetsmasken-fragebogen-foto-enaio-spec]] (Andockpunkt, hier nicht gebaut).

## Ziel / Nutzen (das Warum)

Heute bucht das Lager den Wareneingang frei (`StockMovementsController.Inbound`, `StockMovementsController.cs:86-156`; `InboundBulk`
`:160-252`): Artikel, Menge, Platz. Die Bestellung, gegen die geliefert wird, kennt das WMS nicht. Folgen: keine Vorschau, was wann kommt;
keine Prüfung gegen die bestellte Menge, Teil- und Überlieferungen fallen nicht auf; kein Bezug Bestellung → Buchung.
Ziel: offene Sage-Einkaufsbestellungen ins WMS lesen, dem Lager eine Vorschau „was kommt in den nächsten X Wochen“ geben und den
Wareneingang gegen eine Bestellung erfassen. Die Buchung selbst bleibt die bestehende WMS-Einbuchung (Entscheidung Gerald 2026-11-07).

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- E1: Lesen der offenen EK-Bestellungen aus SData in eine Cache-Tabelle (Dienst, eigener Schalter, Aktivitäts-Protokoll).
  **Dieses Epic baut den einzigen Lesezugriff auf offene Sage-Bestellungen** (Cache `SagePurchaseOrderLines` + `ISagePurchaseOrderLineRepository`).
  Das Einkaufs-Epic [[2026-11-08-einkauf-bestellvorschlaege-spec]] liest „offen bestellt“ je Artikel aus derselben Tabelle und ergänzt dafür
  höchstens eine Aggregat-Methode im selben Repository – keinen zweiten Reader, keine zweite Tabelle, keinen zweiten Dienst-Lauf.
- E2: Maske „Offene Bestellungen“ (Vorschau der nächsten X Wochen, X als AppSetting nach ADR 0020) je Lieferant/Artikel/Ziel-Lagerplatz.
- E3: Maske „Wareneingang zu Bestellung“: Bestellung wählen oder Belegnummer scannen → offene Positionen → Ist-Menge → Einbuchung über die
  bestehende WMS-Buchung (inkl. Sage-Lagerbuchungs-Queue wie heute); Bestellbezug je Buchung speichern; Teil-/Überlieferung erkennbar.
- E5 (spätere Etappe, blockiert): Wareneingangsbeleg in Sage per `StandardBelegService` hinter Schalter, Default aus.

**Out-of-Scope**
- Anlegen/Ändern von Bestellungen in Sage, Bestellvorschläge (Einkaufs-Epic).
- Qualitätsprüfung Wareneingang – nur Schnittstelle benannt (siehe „Andockpunkt Qualitätsprüfung“).
- Etikettendruck beim Eingang (Rückfrage 8), Rechnungsprüfung, Chargen-/Seriennummernerfassung im WMS.
- IDEAL bis zum SData-Endpunkt (Rückfrage 7).
- Lesen der EK-Benutzerfelder (`USER_ABErhalten`, `USER_AB`, `USER_WA` …) – über diesen Vertrag nicht lesbar (Backlog Z. 39,
  [[sage-sdata-commonwawiservices]] Z. 24/26).

## Fachliche Anforderungen

### F1 Lesen offener Bestellungen (E1)
- Quelle SData-Lesevertrag `CommonWawiServicesResources` ([[sage-sdata-wms-schnittstellenkatalog]] Z. 41): Kopf `EinkaufBelegekopfInfo`,
  Positionen `EinkaufPositionenInfo`. **Nie** `EinkaufPositionenAllInfo` (Timeout; im Client gesperrt `SageSdataReadClient.cs:137-138`).
- **Kein View-Weg, keine Quellen-Weiche.** ADR 0019 gilt für Abgleiche mit bestehendem SQL-Reader; für offene Bestellungen gibt es keinen
  (Suche nach `EinkaufPositionen`/`EkBeleg`/`KHKEKBeleg` im Code: einziger Treffer ist die Sperrliste `SageSdataReadClient.cs:138`).
  Präzedenz für einen SData-only-Abgleich: `KontokorrentSyncService.cs:25` („Neuer Lese-Abgleich ohne View-Weg“) und
  `ArtikelLieferantSyncService.cs:17-24`.
- Lesen über `ISageSdataReadClient.QueryAllAsync` (`SageSdataReadClient.cs:25-26`, wirft bei jedem Fehler, Paging 0-basiert
  `:179`), Ziel über `SdataLeseZielLeser.LesenAsync` (wie `ArtikelLieferantSyncService.cs:66`).
- Feldliste (`select`) **nur mit belegten Feldnamen** – ein falscher Name im `select` gibt HTTP 500 ([[fallstricke]] Z. 1711). Die Namen unten
  stammen aus der Recherche der Koordination (Backlog Z. 28-38) und sind **unbelegt**, bis P-W1/P-W2 beantwortet sind:
  - Kopf: `BelID`, `Belegkennzeichen`, `BelegnummerFormatiert`, `A0Empfaenger` (Kreditor-Kto), Name, `Liefertermin`, `Liefersperre`.
  - Position: `BelPosId`, `BelID`, `Artikelnummer`, `AuspraegungID`, `Bezeichnung1/2`, `Menge`, `MengeBasis`, EK-Einheit, Umrechnung,
    `PositionGGBestellt`, `PositionGGGeliefert`, `PlatzID`, `Kurzbezeichnung`, `Liefertermin`, `Chargenpflicht`, `Nachweispflicht`.
- **Bedeutung „offen“** = bestellt − geliefert in **Lagermengeneinheit**. Welche Felder das liefern und in welcher Einheit, ist unbelegt
  (P-W3). Bis dahin darf der Dev-Lauf keine Mengenformel festschreiben, die nicht durch P-W3 gedeckt ist.
- Belegarten: nur Kennzeichen laut Rückfrage 3 (Empfehlung `EBB`, `EBZ`). Filter clientseitig nach dem Lesen der Köpfe, nicht über
  `ne` (liefert auf Textfeldern still 0 Zeilen, [[fallstricke]] Z. 1720-1724).
- Zuordnung beim Lauf (nur lesend, Ergebnis im Cache): Artikel über `Articles.ArticleNumber` (getrimmt, Groß/Klein egal, wie
  `ArtikelLieferantSyncService.cs:86-88`), Kreditor über `Creditors.SageKto` (`Creditor.cs:77`), Ziel-Lagerplatz über
  `StorageLocations.SageLagerplatzId` (`StorageLocation.cs:71`, vom Lagerplatz-Sync befüllt, [[datenmodell]] Z. 144-147).
  Nicht zuordenbare Artikel/Kreditoren/Plätze werden **gezählt und im Lauf als Warnung gemeldet** (je max. 20 Beispiele, Text über
  `SdataSchluessel.Kappen`), die Zeile bleibt im Cache mit leerem Verweis.
- Vollersatz je Lauf in einer Transaktion (Präzedenz `ProductionOrderPositions`, [[datenmodell]] Z. 176). **Empty-Guard:** 0 gelesene
  Positionen bei nicht leerem Cache = Warnung, Cache bleibt unverändert (Muster `ArtikelLieferantSyncService.cs:151-152`). Lesefehler = Lauf
  Fehler + throw (Mail über `RunResilientAsync`).

### F2 Maske „Offene Bestellungen“ (E2)
- Positionsliste aus dem Cache. Standardfilter: Liefertermin ≤ heute + `OffeneBestellungenVorschauWochen` × 7 Tage. **Überfällige**
  (Liefertermin < heute, noch offen) sind immer enthalten und markiert; Positionen **ohne Liefertermin** sind enthalten und als „ohne Termin“
  markiert (nicht still ausgeblendet). Liefertermin der Position, ersatzweise des Kopfes.
- Spalten: Liefertermin, Lieferant (Kreditor-Name, sonst Kto), Bestellung (Belegnummer), Pos., Artikelnummer, Bezeichnung, bestellt, geliefert
  laut Sage, offen laut Sage, im WMS erfasst (ab E3), Einheit, Ziel-Lagerplatz (WMS-Code, sonst Sage-Kurzbezeichnung + Hinweis „nicht im WMS“),
  Kennzeichen (EBB/EBZ), Hinweis Charge/Seriennummer.
- Filterkarte: Lieferant (Auswahl aus Kreditoren), Bestellung, Artikel, Ziel-Lagerplatz, Wochen (Vorgabe aus der Einstellung, je Aufruf
  überschreibbar 1–26), „nur überfällig“. Zeile „Stand: <letzter erfolgreicher Lauf>“ über der Tabelle.
- Je Bestellung Knopf „Wareneingang“ → Maske F3 (nur mit Schreibrecht, nur wenn Feature an).
- Feature aus (`WareneingangBestellungAktiv = false`) → Menüpunkte unsichtbar, Controller liefert 404 (Feature-Gate kumulativ zur Rolle,
  ADR 0011). Abgleich-Schalter aus oder Cache leer → Seite zeigt sichtbaren Hinweis „Abgleich offene Bestellungen ist nicht aktiv / noch
  nicht gelaufen (Dienst-Schalter Sync:EinkaufBestellungenEnabled)“ (Muster `Views/Creditors/Details.cshtml:127`).

### F3 Wareneingang zu Bestellung (E3)
- Einstieg: Bestellung aus F2 oder Scan/Eingabe der Belegnummer (exakter Vergleich getrimmt auf `BelegnummerFormatiert`, Format P-W5).
  Kein Treffer / mehrere Treffer = sichtbare Meldung, nie still die erste ([[fallstricke]] Z. 470-473 sinngemäß).
- Kopf: Lieferant, Belegnummer, Liefertermin, Liefersperre (falls gesetzt: Warnbanner; ob gebucht werden darf, regelt Rückfrage 4 –
  die fachliche Bedeutung von `Liefersperre` an EK-Belegen ist unbelegt).
- Positionen der Bestellung: Artikel, Bezeichnung, bestellt, offen laut Sage, im WMS erfasst, Ziel-Lagerplatz (Auswahl aus
  `GetActiveOrderedExcludingPickingTransportAsync`, vorbelegt mit dem zugeordneten Sage-Platz – Rückfrage 5), Eingabefeld Ist-Menge
  (leer = Position nicht buchen).
- Farbliche **und** textliche Kennzeichnung je Zeile (Farbe nie alleiniger Träger): „vollständig“ (Ist = Rest), „Teillieferung“ (Ist < Rest),
  „Überlieferung“ (Ist > Rest). Rest-Definition nach Rückfrage 2. Überlieferung nach Rückfrage 4 (Empfehlung: Pflicht-Häkchen „Überlieferung
  bestätigt“, sonst Fehler an der Zeile).
- Positionen, deren Artikel nicht im WMS existiert, sind nicht buchbar und zeigen „Artikel nicht im WMS“. Charge/Seriennummer-Pflicht: Hinweis
  „Charge/Seriennummer in Sage erfassen“ (Rückfrage 10).
- Buchen: alle Zeilen zuerst validieren (keine Teilbuchung, Muster `InboundBulk` S2 `StockMovementsController.cs:183-218`), dann je Zeile eine
  `StockMovement` (`MovementType.Einbuchung`, Audit wie `:125-138`) **nur über `IStockMovementRepository.AddAsync`** (registriert als
  `SageBookingEnqueueingStockMovementRepository`, `Program.cs:72`) – sonst erreicht die Buchung Sage nie (Kommentar S1 `:221-223`).
  Die `PurchaseOrderReceipt`-Zeile hängt als Navigation an der `StockMovement` und wird in **demselben** `SaveChanges` gespeichert
  (`Repository.AddAsync` `Repository.cs:33-35` fügt den Graphen hinzu) – kein zweiter Commit (vgl. [[fallstricke]] Z. 843-853).
- `TempData["SuccessMessage"]` „n Positionen eingebucht (davon k Teil-, u Überlieferung)“, Redirect auf dieselbe Bestellung.
- Doppelte Absendung: Antiforgery + PRG; die Spalte „im WMS erfasst“ macht eine zweite Buchung sichtbar.

### F4 Andockpunkt Qualitätsprüfung (nur Schnittstelle)
Die Qualitätsprüfung Wareneingang ([[2026-11-08-qualitaetsmasken-fragebogen-foto-enaio-spec]]) hängt sich an `PurchaseOrderReceipt.Id`
(eine Zeile je gebuchter Bestellposition und Lieferung; Artikel, Kreditor, Menge, Zeitpunkt, Benutzer erreichbar). Dieses Epic baut keinen
Haken, keine Pflichtprüfung und keine Sperre; ob die Prüfung optional oder je Artikelgruppe Pflicht ist, entscheidet die Qualitäts-Spec.

### F5 Sage-Wareneingangsbeleg (E5, später, blockiert)
- Hinter ServiceSetting `SageWareneingangBelegAktiv` (Default aus). Aus = Verhalten von E3 unverändert.
- An: WE-Zeilen mit Bestellbezug gehen **nicht** in die Lagerbuchungs-Queue, sondern als Wareneingangsbeleg per `StandardBelegService`
  (`Erfassungsart=4100`, `BelegTransformArt=1` Belegübernahme aus der Bestellung, Positionen mit `BelegPositionLagermengen{PlatzId,
  Kurzbezeichnung, Menge}`; [[sage-sdata-commonwawiservices]] Z. 27, 70-73). Ausschluss an **einer** Stelle in `SageBookingEnqueueDecision`
  (neuer Parameter analog `hatFaBezug`, `SageBookingEnqueueDecision.cs:62-72`), gelesen vom Decorator
  (`SageBookingEnqueueingStockMovementRepository.cs:65-67`) **und** von der Nacherfassung `EnqueueMissingAsync` (`SageBookingWorker.cs:115`) –
  sonst holt der 15-Minuten-Sweep die Buchung als Zugang nach ([[fallstricke]] Z. 1719).
- Status je `PurchaseOrderReceipt` (Muster `SageUebergabeStatus`, [[datenmodell]] Z. 181), Wiederholungssicherheit über `BacklogPrimaryKeys`
  bzw. Memo-Korrelation (offen, [[sage-sdata-wms-schnittstellenkatalog]] „Was fehlt“ Punkt 5).
- Voraussetzungen: Rückfragen 1, 9, 13 beantwortet; P-W7 belegt; Schreibtest am AKE-Test erfolgreich und zurückgesetzt.

## Technischer Loesungsentwurf

- **Dienst (E1):** `EinkaufBestellungenSyncService` in `IDEALAKEWMSService/Services/Sdata/`, Aufbau 1:1 wie `ArtikelLieferantSyncService`
  (`ISyncLogger` als letzter Ctor-Parameter `:48-49`, Lauf über `BeginRunAsync`, deutsche Count-Keys, `dryRun`). Gate
  `Sync:EinkaufBestellungenEnabled` im `SyncWorker` als eigener `RunResilientAsync`-Block **nach** dem Artikel-Lieferanten-Block
  (`SyncWorker.cs:463-473`), weil er Artikel, Kreditoren und Lagerplätze zuordnet. Kein Master-Gate (standortneutral; IDEAL hat ohne Endpunkt
  den Schalter aus). DI in `Program.cs` neben `:81-82`; DI-Auflösungstest wie für die übrigen SData-Dienste.
- **Count-Keys:** `koepfe`, `positionen`, `uebernommen`, `belegart_ausgeschlossen`, `artikel_fehlt`, `kreditor_fehlt`, `lagerplatz_fehlt`.
- **Web:** ein Controller `PurchaseOrdersController` (Klasse `[RequireStockReadAccess]`, Action `Receive` GET/POST `[RequireStockAccess]`,
  Read/Edit-Split ADR 0006) mit `Index` (F2) und `Receive` (F3). Datenzugriff nur über `ISagePurchaseOrderLineRepository` und
  `IPurchaseOrderReceiptRepository` (ADR 0001). Kein Cache-Decorator (Cache ist die Tabelle).
- **Listen-View-Pattern (ADR 0005) für `Index`:** `PageSize.Resolve` + `PaginationState` + `_Pagination`, Filterkarte, Server-Spaltenfilter
  (`data-server-column-filter="true"`, alle `<th>` mit `data-col-key`, `ColumnFilterHelper.ReadFromQuery` wie
  `StockMovementsController.cs:53`), Spaltenauswahl über den Toolbar-Host, ein Filter-Kern (ADR 0017). Liefertermin ist eine gespeicherte Spalte
  (keine Termin-Berechnung) → Filter in SQL zulässig; Kollation je Textspalte beachten. Referenz `ProductionOrdersController.Index`.
- **`Receive`:** kleine, unpaginierte, auf eine Bestellung vorgefilterte Liste → Client-Spaltenfilter zulässig (ADR 0005).
- **Aggregat „im WMS erfasst“:** Summe `PurchaseOrderReceipt.Quantity` je `BelPosID` (eine Abfrage für alle sichtbaren Zeilen, kein N+1).
- **Menü:** Lager-Dropdown (`_Layout.cshtml:104-108`) „Offene Bestellungen“ und „Wareneingang zu Bestellung“, sichtbar bei Feature an und
  `canAccessStock || canPick` bzw. Lese-Recht.
- **Scanner:** Belegnummer-Feld als Scan-Ziel (`data-hw-scan-ziel`, bestehender `hardware-scanner.js`), kein neues JS-Framework.
- **Oberfläche:** Skill `frontend-design` explizit aufrufen; Bootstrap 5, Kontrast WCAG AA, Teil/Über/vollständig mit Text + Icon.

### Datenmodell
- `SagePurchaseOrderLine` (Tabelle `SagePurchaseOrderLines`, **kein** `AuditableEntity`, `SyncedAt`, Präzedenz `ProductionOrderPosition.cs:10-13`):
  `Id`, `BelPosID` (int, UNIQUE), `BelID` (int, Index), `Belegkennzeichen`, `Belegnummer` (Index), `LieferantKto`, `LieferantName`,
  `CreditorId` int NULL, `Artikelnummer`, `ArticleId` int NULL (Index), `Bezeichnung1/2`, Mengenfelder (decimal(18,4); Namen/Semantik nach P-W3:
  bestellt, geliefert, offen in Lagereinheit, EK-Einheit, Umrechnung), `SagePlatzId` int NULL, `SagePlatzKurzbezeichnung`, `StorageLocationId`
  int NULL, `Liefertermin` date NULL (Index), `Liefersperre` bit NULL, `Chargenpflicht` bit NULL, `Nachweispflicht` bit NULL, `SyncedAt`.
  **Keine FKs** auf Articles/Creditors/StorageLocations (Cache wird je Lauf ersetzt; Verweise sind aufgelöste Werte, Präzedenz
  `BeschichtungsauftragPosition` ohne FK, [[datenmodell]] Z. 165-166). Spaltenlängen aus P-W1/P-W2-Schema.
- `PurchaseOrderReceipt` (Tabelle `PurchaseOrderReceipts`, `AuditableEntity`): `StockMovementId` (FK → StockMovements, Restrict, UNIQUE),
  `BelID`, `BelPosID` (Index), `Belegnummer`, `LieferantKto`, `Quantity` decimal(18,4), `OffenLautSageBeimBuchen` decimal(18,4) NULL,
  `SagePlatzId` int NULL (Vorschlag), `Lieferart` (byte: Vollstaendig=1, Teil=2, Ueber=3). **Snapshot** statt FK auf den Cache – die
  Cache-Zeile verschwindet, sobald Sage die Position schließt; der Bezug muss bleiben.
- `StockMovement` bekommt nur die Navigation `PurchaseOrderReceipt?` (keine Spalte, `StockMovements` bleibt unverändert).

## Migrations-/SQL-Auswirkungen

| Etappe | Migration | Skript | Inhalt | daten-destruktiv |
|---|---|---|---|---|
| E1 | `AddSagePurchaseOrderLines` | `SQL/112_AddSagePurchaseOrderLines.sql` | Tabelle + UNIQUE `BelPosID`, Indizes `BelID`, `Belegnummer`, `ArticleId`, `Liefertermin` | nein |
| E3 | `AddPurchaseOrderReceipts` | `SQL/113_AddPurchaseOrderReceipts.sql` | Tabelle + FK `StockMovementId` Restrict, UNIQUE `StockMovementId`, Index `BelPosID` | nein |
| E5 | `AddPurchaseOrderReceiptSageWe` | `SQL/114_AddPurchaseOrderReceiptSageWe.sql` | Status-/Fehler-/Zeitspalten Sage-WE an `PurchaseOrderReceipts` (DEFAULT nur im SQL, [[fallstricke]] Z. 1717) | nein |

- Reihenfolge je Etappe (ADR 0004): Model → `dotnet ef migrations add` → idempotentes SQL mit `OBJECT_ID`-Guard (DDL eigener Batch) →
  `__EFMigrationsHistory`-Insert eigener Batch → `SQL/00_FreshInstall.sql` an **beiden** Stellen (Schema + `MigrationId`).
- Letzte vorhandene Nummer im Worktree: `SQL/107_AddSageStammdatenOutboxAndUebergabeStatus.sql`; 108–111 gehören anderen Bündel-Specs;
  112–116 reserviert für dieses Epic (Dev-Lauf prüft vor dem Anlegen erneut). 115/116 bleiben frei.
- `SQL/AgentJobs/*`: nicht betroffen (keine Spalte an bestehenden Tabellen, kein Folge-MERGE).
- Settings: neue AppSettings legt der Web-Start aus `AppSettingDefinitions` an (Seed), neue ServiceSettings der Katalog-Seed
  ([[datenmodell]] Z. 140) – kein eigenes SQL.
- Kein lokaler `dotnet run` der Web-App aus dem Worktree, solange Migrationen pending sind ([[fallstricke]] Z. 865-874).

## Einstellungen

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| `WareneingangBestellungAktiv` (AppSetting, Gruppe „Lager und Buchungen“) | Bool (Schalter) | true / false | false | Blendet „Offene Bestellungen“ und „Wareneingang zu Bestellung“ ein; aus = Menüpunkte weg, Seiten 404 | Menü Lager, `PurchaseOrdersController` | AKE (IDEAL erst nach G2) | – |
| `OffeneBestellungenVorschauWochen` (AppSetting, Gruppe „Lager und Buchungen“) | Int, Zahlenfeld mit Grenzen | 1–26, Einheit „Wochen“ | 2 | Standard-Horizont der Vorschau: Positionen mit Liefertermin bis heute + X Wochen; Überfällige und ohne Termin immer dabei | Maske „Offene Bestellungen“ (Filter „Wochen“ vorbelegt) | beide (je Installation) | – |
| `Sync:EinkaufBestellungenEnabled` (ServiceSetting, Gruppe „Sage-Abgleich (SData)“) | Bool | true / false | false | Dienst liest offene EK-Bestellungen (Kopf + Positionen) aus Sage-SData in den Cache; liest nur, schreibt nie nach Sage | Dienst: Lauf „Offene Bestellungen (Sage)“; Masken F2/F3 | AKE (IDEAL ohne Endpunkt aus lassen) | – (Abhängig: SData-Zugang `Sync:Sdata*`, für Zuordnung `Sync:KreditorenEnabled`, Lagerplatz-Sync) |
| `SageWareneingangBelegAktiv` (ServiceSetting, Gruppe „Sage-Lagerbuchung“, **nur E5**) | Bool | true / false | false | An = Wareneingänge zu Bestellungen werden als Sage-Wareneingangsbeleg gebucht statt als Lagerbuchung-Zugang | Wareneingang zu Bestellung, Dienst-Worker Sage-WE | AKE | „Schreibt Wareneingangsbelege nach Sage (Bestellung wird beliefert, Bestand in Sage gebucht). Nur nach Schreibtest am AKE-Test einschalten.“ |

- Server validiert beim Speichern gegen den Katalog (ADR 0020, `AppSettingDefinitions` Helfer `I(...)` mit `min`/`max`, `AppSettingDefinitions.cs:39-42`;
  ServiceSettings über `ServiceSettingDefinitions` inkl. Anzeigename/Verwendung/Warnhinweis wie `ServiceSettingDefinitions.cs:87-89`).
- Drift-Guard-Tests (`AppSettingDefinitionsTests`, `ServiceSettingDefinitionsTests`) bekommen die neuen Keys.
- Belegkennzeichen des Sage-Wareneingangs (E5) wird **keine** Einstellung, sondern eine Konstante nach P-W7 (ein Wert, von Sage vorgegeben).

## Audit-Feld-Auswirkungen

- `SagePurchaseOrderLine`: Cache, kein `AuditableEntity` (Präzedenz `ProductionOrderPosition.cs:10-11`, `SagePpsMitarbeiter.cs:4-6`);
  Nachvollziehbarkeit über `SyncedAt` + Aktivitäts-Protokoll.
- `PurchaseOrderReceipt`: `AuditableEntity`; `CreatedAt/By/ByWindows` aus `ICurrentUserService` (wie `StockMovementsController.cs:134-137`).
  E5: jeder Statuswechsel setzt `ModifiedAt`, `ModifiedBy`, `ModifiedByWindows` = Dienstname (z. B. `system:sage-wareneingang`).
- `StockMovement`: unverändert (Audit wie heute).

## Rollen und Zugriff

- Keine neue Rolle, kein neuer Filter (Rückfrage 12 bestätigt das): `Index` = `[RequireStockReadAccess]` (admin, stock, stock_keyuser,
  picking, stock_read; [[controller]] Z. 62), `Receive` = `[RequireStockAccess]` (admin, stock, stock_keyuser, picking; Z. 61), Feature-Gate
  `WareneingangBestellungAktiv` kumulativ.
- Falls die Antwort auf Rückfrage 12 doch eine neue Rolle verlangt: drei Stellen – Attribut, [[controller]], `Views/Users/RoleOverview.cshtml`
  (+ Seed/SQL).

## Prüfabfragen (Sage, nur lesend, AKE-Test)

Basis: `https://sagetest01.ake.at:5493/sdata/ol/CommonWawiServicesResources/ake_TEST2026;1/` ([[sage-sdata-uebersicht]] Z. 23), Header
`Accept: application/json` ([[fallstricke]] Z. 40). Ergebnis je Abfrage in diese Spec eintragen; abhängige Aussagen gelten bis dahin als **unbelegt**.

- **P-W1 Schema Positionen (blockiert E1):** `EinkaufPositionenInfo/$schema` → exakte Namen, Typen, Längen von `BelPosId`/`BelPosID`, `BelID`,
  `Artikelnummer`, `AuspraegungID`, `Bezeichnung1/2`, `Menge`, `MengeBasis`, EK-Einheit, Umrechnung, `PositionGGBestellt`,
  `PositionGGGeliefert`, `PlatzID`, `Kurzbezeichnung`, `Liefertermin`, `Chargenpflicht`, `Nachweispflicht`.
- **P-W2 Schema Kopf (blockiert E1):** `EinkaufBelegekopfInfo/$schema` → `BelID`, `Belegkennzeichen`, `BelegnummerFormatiert`,
  `A0Empfaenger`, Name-Feld, `Liefertermin`, `Liefersperre`.
- **P-W3 Bedeutung der Mengen (blockiert Mengenformel E1/E3):**
  `EinkaufPositionenInfo?format=json&orderBy=BelPosId&count=10&where=PositionGGGeliefert gt 0&select=BelID,BelPosId,Artikelnummer,Menge,MengeBasis,PositionGGBestellt,PositionGGGeliefert`
  (Feldnamen nach P-W1) → zwei Positionen in Sage öffnen (eine Teillieferung, eine mit EK-Einheit ≠ Lagereinheit) und bestätigen: Welches Feld
  ist bestellt/geliefert, in welcher Einheit, ist „offen“ = bestellt − geliefert in Lagereinheit?
- **P-W4 „Info“ = nur offene, Belegarten:** `EinkaufBelegekopfInfo?format=json&orderBy=BelID&count=1&where=Belegkennzeichen eq 'EBD'` sowie
  `… eq 'EBB'` / `… eq 'EBZ'` (je `$totalResults`) und `EinkaufPositionenInfo?format=json&orderBy=BelPosId&count=5&where=BelID eq <BelID einer
  vollständig gelieferten Bestellung>` → erwartet 0 Zeilen.
- **P-W5 Belegnummer zum Scannen:** `EinkaufBelegekopfInfo?format=json&orderBy=BelID&count=5&select=BelID,Belegkennzeichen,BelegnummerFormatiert`
  → Format mit einer gedruckten Bestellung / einem Lieferanten-Lieferschein vergleichen (Rückfrage 6).
- **P-W6 Ziel-Lagerplatz im WMS:** nach dem ersten Lauf (E1) auf der WMS-DB:
  `SELECT COUNT(*) FROM SagePurchaseOrderLines WHERE SagePlatzId IS NOT NULL AND StorageLocationId IS NULL;` → sollte 0 sein; sonst fehlen
  Plätze im WMS (Lagerplatz-Sync).
- **P-W7 Kennzeichen Sage-Wareneingang (blockiert E5):** `EinkaufBelegeInfo?format=json&orderBy=BelID&count=5&select=BelID,Belegkennzeichen&where=Belegkennzeichen like 'EW%'`
  (Feldnamen vorher per `EinkaufBelegeInfo/$schema` prüfen) → Kennzeichen des Wareneingangsbelegs (Backlog vermutet `EWE`).

## Akzeptanzkriterien

1. (E1) Mit `Sync:EinkaufBestellungenEnabled = false` läuft kein Lauf „Offene Bestellungen (Sage)“; der Cache bleibt unverändert.
2. (E1) Mit Schalter an liest ein Zyklus Köpfe und Positionen; Anzahl Cache-Zeilen = Positionen der zugelassenen Belegarten laut P-W4; Lauf
   „Offene Bestellungen (Sage)“ zeigt die deutschen Count-Keys aus dem Lösungsentwurf.
3. (E1) Nicht zuordenbarer Artikel/Kreditor/Lagerplatz erscheint als Warnung im Lauf mit Beispielen; die Zeile steht trotzdem im Cache.
4. (E1) SData nicht erreichbar → Lauf „Fehler“, Fehlermail, Cache unverändert. 0 Positionen bei nicht leerem Cache → Warnung, Cache unverändert.
5. (E1) Kein Code-Pfad liest `EinkaufPositionenAllInfo`; jeder `select` enthält nur in P-W1/P-W2 belegte Namen.
6. (E2) Mit `WareneingangBestellungAktiv = false` sind beide Menüpunkte unsichtbar und `/PurchaseOrders` sowie `/PurchaseOrders/Receive/…` liefern 404.
7. (E2) Die Vorschau zeigt ohne Filter genau die Positionen mit Liefertermin ≤ heute + X Wochen (X aus der Einstellung) plus alle überfälligen
   und alle ohne Termin, letztere beide sichtbar markiert.
8. (E2) `/Settings` lehnt für `OffeneBestellungenVorschauWochen` 0, 27 und Text ab; 4 wird gespeichert und wirkt beim nächsten Aufruf.
9. (E2) Spaltenfilter (Server-Mode) und Pagination wirken zusammen; Filter „Lieferant“ zeigt nur Positionen dieses Kreditors.
10. (E2) Benutzer nur mit `stock_read` sieht die Vorschau, aber keinen Knopf „Wareneingang“; ein direkter GET/POST auf `Receive` wird abgewiesen.
11. (E2) Ist der Abgleich nie gelaufen, zeigt die Vorschau den Hinweis mit dem Schalternamen statt einer leeren Tabelle ohne Erklärung.
12. (E3) Scan einer existierenden Belegnummer öffnet die Bestellung; unbekannte Nummer = sichtbare Meldung „Bestellung nicht gefunden“.
13. (E3) Buchen von zwei Positionen erzeugt zwei `StockMovements` (Einbuchung, Audit gesetzt) und zwei `PurchaseOrderReceipts` mit derselben
    `StockMovementId`, gespeichert im selben `SaveChanges`.
14. (E3) Liegt der Ziel-Platz auf `SageBuchungErlaubt = 1` und ist `SageLagerbuchungAktiv` an, entsteht je Bewegung ein Eintrag in
    `SageBookingQueueItems` (Verhalten wie freie Einbuchung).
15. (E3) Ist < Rest zeigt „Teillieferung“, Ist = Rest „vollständig“, Ist > Rest „Überlieferung“ – jeweils mit Text, nicht nur Farbe; Überlieferung
    nach Antwort 4.
16. (E3) Eine ungültige Zeile (Menge ≤ 0, Artikel nicht im WMS, kein Lagerplatz) verhindert die Buchung **aller** Zeilen; Meldung an der Zeile.
17. (E3) Nach dem Buchen zeigt die Spalte „im WMS erfasst“ in Vorschau und Receive die gebuchte Menge der Position.
18. (E5, nur wenn gebaut) Mit `SageWareneingangBelegAktiv = false` ist das Verhalten identisch zu E3; mit `true` entsteht für eine
    Bestell-WE-Zeile **kein** Lagerbuchungs-Queue-Eintrag – weder sofort noch nach 15 Minuten Nacherfassung – und genau ein Sage-WE-Beleg.

## Test-Szenarien

Neues Kapitel **TS-116 „Wareneingang zu Sage-Bestellung“** in `docs/TESTSZENARIEN.md`, danach `secondbrain/tests/testszenarien-index.md`.
Vorbedingung allgemein: AKE-Test-SData erreichbar, `Sync:KreditorenEnabled` und Lagerplatz-Sync gelaufen, P-W1–P-W5 beantwortet.
- **TS-116.1 Abgleich aus:** Schalter aus, Zyklus abwarten → kein Lauf; Vorschau zeigt Hinweis (AK 1, 11).
- **TS-116.2 Abgleich an:** Schalter an → Lauf mit Counts; Zeilenzahl gegen P-W4; eine bekannte Bestellung mit Positionen, Mengen, Ziel-Platz
  in der Vorschau gegen Sage vergleichen (AK 2, 3). Negativ: SData-Kennwort falsch → Fehlerlauf + Mail, Cache unverändert (AK 4).
- **TS-116.3 Vorschau X Wochen:** Einstellung 2 → nur ≤ 14 Tage + überfällig + ohne Termin; Einstellung 4 → mehr Zeilen; Negativ 0/27/„abc“
  abgewiesen (AK 7, 8). Filter Lieferant, Spaltenfilter, Seitenwechsel (AK 9).
- **TS-116.4 Rechte und Schalter:** `stock_read`-Benutzer sieht Vorschau ohne Knopf, direkter Aufruf `Receive` abgewiesen; Feature aus → 404 (AK 6, 10).
- **TS-116.5 Wareneingang Teillieferung:** Bestellung scannen (Hardware-Scanner und Eingabe), eine Position mit Ist < Rest buchen → „Teillieferung“,
  Bewegung + Receipt, Queue-Eintrag bei Sage-Platz (AK 12-14, 17).
- **TS-116.6 Überlieferung und Fehler:** Ist > Rest ohne Bestätigung → Fehler, mit Bestätigung gebucht; eine Zeile mit Menge 0 neben einer
  gültigen → nichts gebucht; Position mit Artikel nicht im WMS nicht buchbar; unbekannte Belegnummer (AK 12, 15, 16).
- **TS-116.7 Sage-Wareneingangsbeleg (nur E5):** Schalter aus → wie 116.5; Schalter an → kein Queue-Eintrag (auch nach 15 min), Sage-WE-Beleg
  in Sage sichtbar, danach zurücksetzen (AK 18).

## Etappen (nur bei epic: true)

Ein Worktree, ein Merge am Ende (Schranke 2 nach allen Etappen). Jede Etappe baut und testet grün und endet mit einem Commit. Branch während der
Arbeit mit `scripts/sync-worktree.ps1 -Slug <slug>` auf main-Stand halten. Version 1.84.0 für das ganze Epic (E4 setzt sie).

| # | Etappe | Abhängigkeit | SQL | Status | Commit |
|---|--------|--------------|-----|--------|--------|
| 1 | Lesen offene Bestellungen: Model + Migration + `EinkaufBestellungenSyncService` + SyncWorker-Block + Schalter + SyncLog-Name + DI-Test + Unit-Tests (Zuordnung, Belegart-Filter, Empty-Guard, Fehler wirft) | **P-W1, P-W2, P-W3, P-W4** und Rückfrage 3. Fehlen sie: Etappe als Blocker festhalten, E2/E3 nicht beginnen | 112 | offen | |
| 2 | Maske „Offene Bestellungen“ (Listen-View-Pattern, Filterkarte, AppSettings `WareneingangBestellungAktiv` + `OffeneBestellungenVorschauWochen`, Menü, Hinweis ohne Lauf) | E1 | – | offen | |
| 3 | Wareneingang zu Bestellung: `PurchaseOrderReceipt` + Migration, Maske `Receive`, Buchen über `IStockMovementRepository`, Spalte „im WMS erfasst“ in E2 | E2; Rückfragen 1, 2, 4, 5, 6, 10 | 113 | offen | |
| 4 | Abschluss: beide `AppVersion.cs` 1.84.0, `Views/Help/Changelog.cshtml`, Hilfe (Kapitel Wareneingang zu Bestellung mit konkreten Schritten), README (AppSettings/ServiceSettings), TS-116 in `docs/TESTSZENARIEN.md` | E3 | – | offen | |
| 5 | Sage-Wareneingangsbeleg per `StandardBelegService` hinter `SageWareneingangBelegAktiv` (Default aus), Ausschluss aus Queue in `SageBookingEnqueueDecision` + Nacherfassung, Status je Receipt, Worker | **blockiert:** Rückfragen 1, 9, 13, P-W7, Schreibtest AKE-Test. Ohne diese: nicht bauen, als Blocker festhalten; Epic ist mit E1–E4 merge-fähig | 114 | blockiert | |

Brain-Update (Hauptcheckout, nicht Worktree): [[feature-map]], [[datenmodell]] (zwei Tabellen), [[services]] (Abgleich + Gate), [[controller]]
(Controller + Rechte), [[glossar]] („Wareneingang zu Bestellung“, „offen laut Sage“), Changelog-Notiz, Testindex; bei Antwort auf Rückfrage 1 ein
Eintrag in [[fallstricke]] (Doppelbuchung Bestellung/Lagerbuchung, mit Warum).

## Deploy

*Vorläufig – der QA-Lauf bestätigt gegen den echten Diff.*

- **Web-App:** ja (Controller, Views, Menü, Settings-Katalog, Migrationen).
- **Service:** ja (neuer Abgleich im `SyncWorker`; E5 zusätzlich Worker).
- **Migration:** ja – `SQL/112`, `SQL/113` (E5: `SQL/114`), additiv, kein DB-Backup-Zwang (Backup vor Deploy trotzdem Standard). Reihenfolge:
  SQL-Skripte → Web → Service; danach `Sync:EinkaufBestellungenEnabled` erst nach P-W6-Kontrolle auf dem Testsystem einschalten,
  `WareneingangBestellungAktiv` zuletzt.
- **Publish-Befehle (im Worktree):**
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`
- **Standort:** AKE (DEV AKE). IDEAL: Code wird mit ausgeliefert, Schalter bleiben aus (Rückfrage 7).

## Offene Rueckfragen

1. **Doppelbuchung Bestand (Konflikt benannt):** Entschieden ist „Einbuchung inkl. Sage-Lagerbuchungs-Queue wie heute“ (Backlog Z. 50-51,
   Entscheidung Z. 57). Die WMS-Einbuchung schickt bei Sage-Plätzen einen **Zugang** an Sage (`SageBookingEnqueueDecision.cs:42`), schließt aber
   die Bestellposition in Sage nicht. Bucht der Sage-Einkauf danach wie üblich einen Wareneingangsbeleg mit Lagerbuchung, steigt der Sage-Bestand
   ein zweites Mal, und der Lagerbestand-Sync (`LagerbestandSyncService`, [[services]] Z. 98) zieht die Differenz als Korrektur ins WMS. Wie
   schließt der Einkauf in Stufe 1 die Position in Sage, ohne Bestand zu buchen? Wie läuft der Wareneingang bei AKE heute (wer bucht in Sage,
   ist `Sync:LagerbestandEnabled` an)?
   *Empfehlung:* Entscheidung beibehalten; der Sage-Einkauf setzt Bestellpositionen nach einem WMS-Wareneingang in Sage nur auf erledigt bzw.
   bucht den WE-Beleg ohne Lagerbuchung, bis E5 das ablöst. Diese Arbeitsanweisung in Hilfe und Changelog. Ist das in Sage nicht möglich:
   Wareneingang zu Bestellung nur auf Plätzen mit `SageBuchungErlaubt = 0` freigeben, bis E5 steht.
2. **Anzeige „Rest offen“:** `offen laut Sage − im WMS erfasst` (Receipts ohne Sage-WE) oder nur `offen laut Sage`? Hängt an 1: Schließt der
   Einkauf die Position zeitnah, zählt die WMS-Menge sonst doppelt.
   *Empfehlung:* beide Spalten zeigen; Rest = `offen laut Sage − im WMS erfasst`, solange 1 wie empfohlen gilt. Schließt der Einkauf die
   Position in Sage, fällt sie aus dem Cache; eine Teilschließung (geliefert steigt, Position bleibt offen) würde doppelt abziehen – im UAT prüfen.
3. **Belegarten:** EBB (Bestellung) und EBZ (Rahmenabruf) einbeziehen, EBD (RMA-Anmeldung) und andere nicht?
   *Empfehlung:* EBB + EBZ; Liste als Konstante, Kennzeichen in der Vorschau sichtbar.
4. **Überlieferung und Liefersperre:** Überlieferung erlauben mit Pflicht-Bestätigung je Zeile oder sperren? Darf gegen eine Bestellung mit
   gesetzter `Liefersperre` gebucht werden?
   *Empfehlung:* Überlieferung erlauben mit Häkchen „Überlieferung bestätigt“, im Receipt als `Ueber` gespeichert; Liefersperre nur als
   Warnbanner, Buchen erlaubt (das Lager hat die Ware physisch).
5. **Ziel-Lagerplatz:** Sage-Platz der Position vorbelegen und änderbar lassen?
   *Empfehlung:* ja; Abweichung bleibt über `SagePlatzId` (Vorschlag) und `StockMovement.StorageLocationId` nachvollziehbar. Ohne Zuordnung keine
   Vorbelegung + Hinweis.
6. **Scannen:** Was liegt beim Eingang vor – unsere Bestellnummer als Barcode/QR auf der Bestellung oder dem Lieferanten-Lieferschein, als Text,
   oder nichts? Format siehe P-W5.
   *Empfehlung:* Stufe 1 Eingabe/Scan der Belegnummer (exakt, getrimmt) plus Auswahl über Lieferant; Lieferschein-Scan später.
7. **IDEAL:** Bleibt die Funktion an IDEAL aus, bis der SData-Endpunkt gekauft ist (G2), ohne eigenen View-Reader auf die IDEAL-Sage-DB?
   *Empfehlung:* ja; Schalter an IDEAL aus, kein zweiter Lesepfad (ADR 0019 erst, wenn es einen View-Weg wirklich braucht).
8. **Etikettendruck beim Eingang** (Backlog Z. 23): in diesem Epic?
   *Empfehlung:* nein; bestehende Artikel-/Lagerplatz-Etiketten; Sage-Etikett `PrintEtikettenBasicService` Typ 4 EKPosition als eigene Folge-Spec.
9. **DPS-MDE** (Backlog Z. 45-46): Hat DPS ein lizenziertes/geplantes Wareneingangs-Modul (`USER_DPSbsMDE*`)? Wer fragt bei DPS an?
   *Empfehlung:* E1–E4 unabhängig davon bauen (WMS-eigene Buchung); E5 erst nach DPS-Antwort.
10. **Charge/Seriennummer:** Positionen mit `Chargenpflicht`/`Nachweispflicht` – Stufe 1 nur Hinweis, Stufe 2 nicht automatisch an Sage?
    *Empfehlung:* ja; in E5 solche Zeilen sichtbar als „nur in Sage buchbar“ melden, nicht senden.
11. **Aktualität:** Genügt der Cache je Dienst-Zyklus mit Anzeige „Stand“, oder braucht die Maske „Wareneingang“ ein Live-Nachladen der
    Bestellung aus Sage?
    *Empfehlung:* Cache genügt; Live-Nachladen nur, wenn das UAT veraltete Zeilen zeigt.
12. **Rollen:** keine neue Rolle – Vorschau für `RequireStockReadAccess`, Wareneingang für `RequireStockAccess`?
    *Empfehlung:* ja.
13. **E5 im Epic oder Folge-Spec?**
    *Empfehlung:* Folge-Spec, sobald 1, 9, P-W7 und der Schreibtest erledigt sind; dieses Epic endet mit E4 (Version 1.84.0). E5 bleibt hier als
    beschriebener, blockierter Schritt stehen, damit die Folge-Spec ihn übernimmt.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. Doppelbuchung Bestand – wie schließt der Einkauf in Stufe 1 die Position in Sage, wie läuft WE heute? →
2. Anzeige „Rest offen“ (offen laut Sage minus im WMS erfasst?) →
3. Belegarten EBB + EBZ, nicht EBD? →
4. Überlieferung erlauben mit Bestätigung oder sperren; Buchen trotz Liefersperre? →
5. Ziel-Lagerplatz vorbelegen und änderbar? →
6. Scannen – was steht auf dem Papier? →
7. IDEAL aus bis SData-Endpunkt (G2), kein View-Reader? →
8. Etikettendruck außerhalb dieses Epics? →
9. DPS-MDE-Modul – Antwort/Wer fragt? →
10. Charge/Seriennummer nur Hinweis, in E5 nicht automatisch? →
11. Cache je Zyklus ohne Live-Nachladen genügt? →
12. Keine neue Rolle (Read/Stock wie vorgeschlagen)? →
13. E5 im Epic oder Folge-Spec? →
