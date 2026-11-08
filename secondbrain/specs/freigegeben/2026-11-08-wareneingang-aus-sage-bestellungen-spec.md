---
type: spec
title: Wareneingang auf Basis der Sage-Bestellungen (offene Bestellungen lesen, Vorschau X Wochen, Wareneingang zu Bestellung)
slug: 2026-11-08-wareneingang-aus-sage-bestellungen-spec
status: Testbereit
created: 2026-11-07
updated: 2026-11-08
source_backlog: "[[2026-11-07-wareneingang-aus-sage-bestellungen]]"
buendel: "[[2026-11-08-buendel-uebersicht]]"
version: "1.84.0"
sql_nummern: "112, 113 (114 nur E5, 115–116 frei)"
testszenario: "TS-116"
task: "[[2026-11-08-buendel-2026-11-08-goal-lauf-2]]"
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
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
freigabe_von: "Gerald Weichbold"
freigabe_am: "2026-11-07"
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

## E0-Ergebnis (Dev, 2026-11-08, nur lesend am AKE-Test) — BLOCKER

> [!danger] Blocker: drei Abweichungen von der Spec-Annahme (Freigabe-Nachtrag: „Abweichung = Blocker melden, nicht raten“)
> E1–E4 sind **nicht** gebaut. Die Mengenformel (F1 „offen“, F2-Spalten, F3 Teil/Über/vollständig) und die Belegnummer-Suche hängen an
> den Befunden B-1 bis B-3. Gerald entscheidet; danach Spec nachziehen (Rework) und E1 starten.

Ausgeführt: SData GET mit `orderBy` (Browser-Sitzung, `format=json`), Sage-SQL `ake_TEST2026` nur `SELECT` (Definition der Sage-View
`dbo.vwDPSBS_CWS_Mde_EinkaufpositionenInfo`, `INFORMATION_SCHEMA`).

**P-W1 Positionen `EinkaufPositionenInfo` (belegt, 91 Felder, $totalResults 1.167).** Schlüssel `BelId`, `BelPosId`, `VorPosId` (int).
Vorhanden wie angenommen: `Belegkennzeichen`, `Belegart`, `BelegnummerFormatiert`, `A0Empfaenger`, `A0Matchcode`, `A0Name1`, `Artikelnummer`,
`AuspraegungID`, `Bezeichnung1/2`, `Chargenpflicht`/`Nachweispflicht` (short), `Menge`, `Mengeneinheit`, `MengeBasis`, `MengeBasiseinheit`,
`MengeEKEinheit`, `MengeEKUmrechnung`, `PlatzID`, `Kurzbezeichnung`, `Platzbezeichnung`, `Lagermengeneinheit`, `UmrechnungsFaktorLME`,
`PositionGGBestellt`, `PositionGGGeliefert` (decimal), `Liefertermin` (dateTime), `BelegLiefertermin` (dateTime, in der JSON-Feldliste der Positionen als string, Nachtrag 2026-11-08 zu QA Q-3), `Liefersperre` (short). Die Positionen tragen die Kopfdaten
(Kennzeichen, Lieferant, Belegnummer) bereits mit. Das `$schema` nennt **keine** `maxLength`; Längen aus Sage-SQL: `Artikelnummer` 50,
`Bezeichnung1/2` 50, `Mengeneinheit`/`MengeBasiseinheit` 5, `A0Empfaenger` 20, `A0Name1`/`A0Matchcode` 50, `Belegkennzeichen` 3.
**P-W2 Kopf `EinkaufBelegekopfInfo`:** Felder wie angenommen, Name = `A0Name1`/`A0Matchcode`; `BelegnummerFormatiert` = `<Belegjahr>-<Belegnummer>`
(z. B. `2027-168975`).
**P-W4:** nur `EBB` = „Bestellung“ (1.059 Positionen) und `EBZ` = „Rahmenabruf“ (108 Positionen); `EBD`, `EBA`, `EWE` je 0. Die View filtert
selbst auf offene Positionen (`VP.GGBestellt > VP.GGGeliefert AND VP.GGErfuellt = 0`, `V.GGErfuellt = 0`) → „Info = nur offene“ bestätigt.

**Abweichungen (Blocker):**
- **B-1 Mengen sind Vorgangs-, nicht Belegmengen (P-W3).** `PositionGGBestellt`/`PositionGGGeliefert` kommen aus `KHKEKVorgaengepositionen`
  (`VP.GGBestellt`, `VP.GGGeliefert`), nicht aus der Belegposition. Bei `EBB` passt das (1.059 Positionen = 1.059 `VorPosId`,
  `PositionGGBestellt = MengeBasis` in allen Fällen). Bei `EBZ` (Rahmenabruf) teilen sich **108 Belegpositionen nur 32 Vorgangspositionen**
  (bis 18 Abrufe je Vorgangsposition), z. B. EK013128: 14 Abrufe mit `Menge` 1.200–2.600, alle mit `GGBestellt 34.800 / GGGeliefert 33.604`.
  „offen = bestellt − geliefert“ je Belegposition würde den Rest des Rahmens je Abruf mehrfach zeigen und als Rest beim Buchen anbieten.
  **Entscheidung nötig:** EBZ ausschließen, je `VorPosId` zusammenfassen, oder Abrufmenge `Menge` als „bestellt“ nehmen (dann fehlt „geliefert“
  je Abruf – kein Feld dafür im Vertrag).
- **B-2 Einheit.** Die GG-Mengen sind in der **Basiseinheit** (`= MengeBasis`, `MengeBasiseinheit`), nicht in der Lagermengeneinheit wie in F1
  angenommen. 8 EBB-Positionen haben `Mengeneinheit ≠ Lagermengeneinheit` bei `UmrechnungsFaktorLME = 1` (z. B. EK012566 `RLL` vs. `Stk`,
  EK023609 `PKG` vs. `Stk`). Welche Einheit der WMS-Bestand und die Sage-Lagerbuchung haben, ist für diese Fälle nicht belegt.
- **B-3 Kopf nicht eindeutig; „Liefersperre“ ist die Kreditor-Sperre.** `EinkaufBelegekopfInfo` liefert **mehrere Zeilen je `BelId`**
  (484 Zeilen, 19 Belegnummern mehrfach, bis 9×, z. B. `2026-166281` 4× dieselbe `BelId`) – der Vertrag ist kein Kopf je Beleg; die Zahlen
  „376 EBB / 108 EBZ Köpfe“ der Recherche sind Zeilen, keine Belege. `Liefersperre` = `KHKKontokorrent.IstGesperrt` des Lieferanten (View-
  Definition), keine Sperre der Bestellung (Rückfrage 4 behandelte sie als Beleg-Liefersperre). Folge: Köpfe nicht lesen, sondern aus den
  Positionen ableiten (distinct `BelId`) – technisch einfach, aber Abweichung vom Lösungsentwurf.

**Nicht ausgeführt:** P-W5-Vergleich mit einem gedruckten Beleg (Mensch), P-W6 (nach E1), P-W7 (E5, Folge-Spec).

**Empfehlung zur Entscheidung:** B-1 – Stufe 1 nur `EBB`, `EBZ` (Rahmenabrufe) als eigene Frage; B-2 – Mengen in Basiseinheit anzeigen
und buchen, Positionen mit `Mengeneinheit ≠ Lagermengeneinheit` sichtbar markieren und nicht buchbar, bis geklärt; B-3 – Kopfdaten aus den
Positionen, `Liefersperre` als „Lieferant gesperrt“ (Warnbanner) anzeigen.

## Entscheidung E0-Blocker (Gerald 2026-11-07 18:02, geht dem Rumpf und den Freigabe-Antworten vor)
- **B-1:** Stufe 1 (E1–E4) **nur `EBB`** (normale Bestellungen). `EBZ` (Rahmenabrufe) **nicht** lesen/anzeigen. Später (eigene Etappe bzw.
  Folge-Spec): offen je Abruf = Abrufmenge `Menge` − im WMS zu diesem Abruf erfasste Menge (Sage liefert kein „geliefert“ je Abruf).
- **B-2:** Mengen in der **Basiseinheit** anzeigen und buchen. Positionen mit `Mengeneinheit ≠ Lagermengeneinheit` sichtbar markieren und
  **nicht buchbar**, bis die Einheitenfrage geklärt ist (Hinweistext in der Zeile).
- **B-3:** Kopfdaten **aus den Positionen ableiten** (distinct `BelId`), `EinkaufBelegekopfInfo` nicht als Kopfquelle verwenden.
  `Liefersperre` als Warnbanner **„Lieferant gesperrt“** anzeigen (keine Belegsperre; Buchen bleibt möglich – Rückfrage 4 entsprechend).
- Danach E1–E4 wie geplant; E5 bleibt Folge-Spec.

## Umsetzungsnotizen (Dev, 2026-11-08)

- **Nach Entscheidung E0-Blocker:** Abgleich liest nur `EinkaufPositionenInfo` mit `where Belegkennzeichen eq 'EBB'` (Kopf nicht gelesen,
  Kopfdaten aus den Positionen); `MengeBestellt/Geliefert` = `PositionGGBestellt/-Geliefert` (Basiseinheit), `MengeOffen` gespeichert (Spaltenfilter);
  `EinheitAbweichend` = `Mengeneinheit ≠ Lagermengeneinheit` → in der Maske ohne Eingabefeld „nicht buchbar, bis geklärt“; `LieferantGesperrt` =
  Sage `Liefersperre` → Warnbanner, Buchen erlaubt.
- **Abweichung Lösungsentwurf:** Vorschau filtert im Speicher über alle Cache-Zeilen vor Zählen/Paging (wie `WarehouseRequisitionsController`,
  ~1.000 Zeilen) statt in SQL – ADR 0005 erfüllt (Filter vor Count/Paging). Lieferanten-Filter = Auswahl aus den Lieferanten im Cache (Kto + Name)
  statt aller Kreditoren.
- **Ist-Menge als Text** (Komma oder Punkt, `BeschichtungsauftraegeController.TryMenge`), weil die App keine feste Request-Kultur hat
  ([[fallstricke]] „Die Anwendung hat keine feste Request-Kultur“).
- Rückfrage 1 (Doppelbestand) bleibt organisatorisch: Einkauf schließt die Position in Sage ohne Lagerbuchung (Hilfe-Text).

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
| 1 | Lesen offene Bestellungen: Model + Migration + `EinkaufBestellungenSyncService` + SyncWorker-Block + Schalter + SyncLog-Name + DI-Test + Unit-Tests (Zuordnung, Belegart-Filter, Empty-Guard, Fehler wirft) | **P-W1, P-W2, P-W3, P-W4** und Rückfrage 3. Fehlen sie: Etappe als Blocker festhalten, E2/E3 nicht beginnen | 112 | erledigt (E0 + Entscheidung E0-Blocker) | `cf1f8547` |
| 2 | Maske „Offene Bestellungen“ (Listen-View-Pattern, Filterkarte, AppSettings `WareneingangBestellungAktiv` + `OffeneBestellungenVorschauWochen`, Menü, Hinweis ohne Lauf) | E1 | – | erledigt | `0fdfa39d` |
| 3 | Wareneingang zu Bestellung: `PurchaseOrderReceipt` + Migration, Maske `Receive`, Buchen über `IStockMovementRepository`, Spalte „im WMS erfasst“ in E2 | E2; Rückfragen 1, 2, 4, 5, 6, 10 | 113 | erledigt | `3bba01f6` |
| 4 | Abschluss: beide `AppVersion.cs` 1.84.0, `Views/Help/Changelog.cshtml`, Hilfe (Kapitel Wareneingang zu Bestellung mit konkreten Schritten), README (AppSettings/ServiceSettings), TS-116 in `docs/TESTSZENARIEN.md` | E3 | – | erledigt | `0a941d63` |
| 5 | Sage-Wareneingangsbeleg per `StandardBelegService` hinter `SageWareneingangBelegAktiv` (Default aus), Ausschluss aus Queue in `SageBookingEnqueueDecision` + Nacherfassung, Status je Receipt, Worker | **blockiert:** Rückfragen 1, 9, 13, P-W7, Schreibtest AKE-Test. Ohne diese: nicht bauen, als Blocker festhalten; Epic ist mit E1–E4 merge-fähig | 114 | blockiert | |

Brain-Update (Hauptcheckout, nicht Worktree): [[feature-map]], [[datenmodell]] (zwei Tabellen), [[services]] (Abgleich + Gate), [[controller]]
(Controller + Rechte), [[glossar]] („Wareneingang zu Bestellung“, „offen laut Sage“), Changelog-Notiz, Testindex; bei Antwort auf Rückfrage 1 ein
Eintrag in [[fallstricke]] (Doppelbuchung Bestellung/Lagerbuchung, mit Warum).

## Deploy

*Final (qa-agent, 2026-11-07), aus dem echten Diff `04742c26..26e6a278` (41 Dateien).*

- **deploy.web:** ja – 28 Dateien unter `IdealAkeWms/` (Controller, Views, Menü, Settings-Katalog, Modelle, Repositories, AppVersion 1.84.0).
- **deploy.service:** ja – `IDEALAKEWMSService/Program.cs`, `Workers/SyncWorker.cs` (Abgleich `EinkaufBestellungenSyncService`), `AppVersion.cs`. E5 ist nicht gebaut (blockiert).
- **deploy.migration:** ja – neu `IdealAkeWms/Migrations/20261107172542_AddSagePurchaseOrderLines` und `20261107173341_AddPurchaseOrderReceipts`,
  dazu `SQL/112_AddSagePurchaseOrderLines.sql`, `SQL/113_AddPurchaseOrderReceipts.sql`, `SQL/00_FreshInstall.sql`. Additiv, nicht daten-destruktiv;
  kein Einmal-Skript (`SQL/Einmalig/` unberührt).
- **Reihenfolge Testsystem:** 1. DB-Backup (Standard) → 2. `SQL/112`, `SQL/113` → 3. Web publizieren → 4. Service publizieren →
  5. `Sync:EinkaufBestellungenEnabled` erst nach P-W6-Kontrolle einschalten → 6. `WareneingangBestellungAktiv` zuletzt.
- **Publish-Befehle (im Worktree):**
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`
- Ablauf Mensch: Publish aus dem Worktree → Testsystem → Test → Merge. Nach dem Merge nur dann erneut aus `main` publizieren, wenn der Merge
  getestete Dateien mit parallelen `main`-Änderungen zusammengeführt hat.
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

## Freigabe-Nachtrag 2026-11-07 (Gerald, geht dem Rumpf vor)
- Umsetzung **heute im Bündel-Worktree** `.claude/worktrees/2026-08-07-ideal-teile-1-5` (braucht SData-Lese-Client, Kreditoren, ADR-0020-Katalog).
- **Neue Etappe E0 vor E1:** Prüfabfragen P-W1–P-W5 (SData `$schema` je Ressource bzw. Stichprobe, nur GET) und P-W3 (Mengen-Semantik, Sage-SQL
  AKE-Test nur SELECT) führt der Dev selbst lesend aus und trägt die Ergebnisse in die Spec ein. Vorbefund der Koordination (2026-11-07,
  Backlog „Recherche Koordination“): Felder `PositionGGBestellt`/`PositionGGGeliefert`, `PlatzID`, `Kurzbezeichnung`, `Liefertermin`,
  Belegkennzeichen EBB 376 / EBZ 108 (Kopf), EBD nur in den Auskunft-Ressourcen; `orderBy` Pflicht (BelID bzw. BelPosID).
  Weicht ein Ergebnis von der Spec-Annahme ab: als Blocker melden, nicht raten. P-W6/P-W7 wie in der Spec.
- E5 (Sage-Wareneingangsbeleg) bleibt Folge-Spec (Antwort 13); nichts schreibt an Sage.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. Doppelbuchung Bestand – wie schließt der Einkauf in Stufe 1 die Position in Sage, wie läuft WE heute? → **wie empfohlen (Gerald 2026-11-07): Der Einkauf schließt die Bestellposition in Sage ohne Lagerbuchung, bis E5 das ablöst; geht das nicht, nur Plätze mit `SageBuchungErlaubt = 0`. Gerald informiert den Einkauf vor der produktiven Nutzung.**
2. Anzeige „Rest offen“ (offen laut Sage minus im WMS erfasst?) → **wie empfohlen (Gerald 2026-11-07)**
3. Belegarten EBB + EBZ, nicht EBD? → **wie empfohlen (Gerald 2026-11-07)**
4. Überlieferung erlauben mit Bestätigung oder sperren; Buchen trotz Liefersperre? → **wie empfohlen (Gerald 2026-11-07)**
5. Ziel-Lagerplatz vorbelegen und änderbar? → **wie empfohlen (Gerald 2026-11-07)**
6. Scannen – was steht auf dem Papier? → **wie empfohlen (Gerald 2026-11-07)**
7. IDEAL aus bis SData-Endpunkt (G2), kein View-Reader? → **wie empfohlen (Gerald 2026-11-07)**
8. Etikettendruck außerhalb dieses Epics? → **wie empfohlen (Gerald 2026-11-07)**
9. DPS-MDE-Modul – Antwort/Wer fragt? → **wie empfohlen (Gerald 2026-11-07): E1–E4 unabhängig vom DPS-MDE-Modul; Frage an DPS stellt Gerald.**
10. Charge/Seriennummer nur Hinweis, in E5 nicht automatisch? → **wie empfohlen (Gerald 2026-11-07)**
11. Cache je Zyklus ohne Live-Nachladen genügt? → **wie empfohlen (Gerald 2026-11-07)**
12. Keine neue Rolle (Read/Stock wie vorgeschlagen)? → **wie empfohlen (Gerald 2026-11-07)**
13. E5 im Epic oder Folge-Spec? → **wie empfohlen (Gerald 2026-11-07)**

## QA-Nachweis (qa-agent, 2026-11-07, Goal-Modus, Review-Umfang `04742c26..0a941d63`)

> [!warning] Ergebnis: **nicht Testbereit**, Status bleibt `InUmsetzung`.
> Build, Tests, Migrationen und der DEV-Lauf sind grün. Q-1 (Kontrast) und Q-2 (doppeltes Banner) verletzen die Oberflächen-Vorgaben dieser Spec
> und sind schnell behoben. Für Q-3 fehlt ein Beleg. Nach dem Fix reicht ein QA-Nachlauf für die Views. Ein neuer DEV-Datenlauf ist nur nötig,
> wenn der Fix den Controller oder den Abgleich berührt.

### 1 Build und Tests (Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, HEAD `0a941d63`)
- `dotnet build IdealAkeWms.slnx`: 0 Fehler, 28 Warnungen (alle schon vorher vorhanden).
- `dotnet test`: IdealAkeWms.Tests 2472 bestanden / 1 übersprungen / 0 Fehler (gesamt 2473); IDEALAKEWMSService.Tests 503 / 0 / 0.

### 2 Code-Review (selbst durchgeführt, synchron): Befunde
| # | Schwere | Befund | Behandlung |
|---|---|---|---|
| Q-1 | mittel | `Receive.cshtml`: Der Zeilenfehler steht als `small text-danger` in einer `table-danger`-Zeile, also #dc3545 auf #f8d7da mit ca. **3,4:1**. WCAG AA verlangt für kleine Schrift 4,5:1. Auf DEV sichtbar („Menge muss größer als 0 sein.“). Das Projekt-CSS überschreibt die Farben nicht. | offen, Dev: in der Fehlerzeile z. B. `text-danger-emphasis` statt `text-danger` |
| Q-2 | niedrig | `Index.cshtml` und `Receive.cshtml` geben `TempData["SuccessMessage"/"WarningMessage"]` selbst aus, `_Layout.cshtml:425-435` ebenfalls. Dadurch erscheint **jede Meldung zweimal**. Auf DEV belegt mit „2 Position(en) eingebucht …“ und „gehört zu 2 Bestellungen“. | offen, Dev: die TempData-Blöcke in beiden neuen Views entfernen |
| Q-3 | mittel (Beleg fehlt) | `EinkaufBestellungenSyncService.Select` enthält `BelegLiefertermin`. Das E0-Ergebnis P-W1 nennt dieses Feld nicht ausdrücklich, es stammt aus der Koordinations-Recherche. Ein falscher Feldname im `select` ergibt HTTP 500 ([[fallstricke]]), dann endet jeder Lauf mit Fehler und Mail. AK 5 verlangt nur belegte Namen; `Vertrag_NurBelegteFelder_NieAllInfo_AK5` prüft das nicht. | offen, Dev/Mensch: `EinkaufPositionenInfo/$schema` am AKE-Test prüfen und in E0 eintragen, sonst aus `select` streichen. QA hat Sage bewusst nicht abgefragt. |
| Q-4 | niedrig | Zielgrößen am Handgerät unter 44 px: Knopf „Wareneingang“ (Index, 32 px), `form-select-sm` (ca. 31 px), Häkchen „Überlieferung bestätigt“ (16 px plus Label), Ist-Menge (38 px). Nur „Einbuchen“ hat 44 px. | offen: mit Q-1/Q-2 beheben oder bewusst akzeptieren |
| Q-5 | niedrig | Unterschiedliche Genauigkeit: `PurchaseOrderReceipts.Quantity` ist decimal(18,4), `StockMovements.Quantity` decimal(18,3). Eine Eingabe mit 4 Nachkommastellen ergibt eine Bewegung, die vom Wareneingang abweicht. Der Server prüft die Nachkommastellen nicht. | Hinweis; Fix: im `Receive`-POST mehr als 3 Nachkommastellen ablehnen |
| Q-6 | niedrig | Stille Fallbacks im Abgleich: `PositionGGBestellt ?? 0m`, `PositionGGGeliefert ?? 0m`. Die Sage-View liefert nur Zeilen mit `GGBestellt > GGGeliefert`, Null ist also praktisch ausgeschlossen. Käme es doch vor, würde still „offen = −geliefert“ gerechnet. | Hinweis: nach der Hausregel „melden statt still“ zählen und als Warnung melden |
| Q-7 | Hinweis | Ist der Sage-Platz einem WMS-Platz mit `IstBuchbar = 0` zugeordnet, wird nichts vorbelegt und es fehlt ein Hinweis, warum (auf DEV mit HR;0;0;0 gesehen). Die Hinweisspalte der Vorschau zeigt „Artikel nicht im WMS“ nicht. Die Client-Anzeige meldet bei Menge 0 „Teillieferung“; der Server lehnt korrekt ab. | Hinweis, kein Fix zwingend |

Ohne Befund geprüft:
- Migrationsdisziplin nach ADR 0004: Model, zwei Migrationen, `SQL/112`/`113` mit `OBJECT_ID`/`sys.indexes`-Guards, DDL je eigener Batch,
  History-Insert in eigenem Batch, FreshInstall an beiden Stellen (Schema hinter `StockMovements`, Z. 275 < 2341, und MigrationId), Objektnamen wie in EF.
- Audit: `PurchaseOrderReceipt` und `StockMovement` setzen Created* aus `ICurrentUserService`; der Cache bleibt wie vorgesehen ohne Audit.
- Buchung nur über `IStockMovementRepository.AddAsync` (Decorator), der Receipt hängt als Navigation im selben `SaveChanges`.
- Validierung läuft serverseitig vor jeder Buchung, keine Teilbuchung; das JS zeigt nur an.
- Bindung `zeilen[i]` ist lückenlos: Jede Zeile schreibt `BelPosId`, auch die nicht buchbaren.
- Keine Werte in `<script>`, kein `@Html.Raw`, kein `hidden` zusammen mit `d-*`.
- Feature-Gate `RequireWareneingangBestellungAktiv` gilt zusätzlich zur Rolle, das Menü ist gleich gegatet.
- Read/Edit-Split: `RequireStockReadAccess` an der Klasse, `RequireStockAccess` an beiden `Receive`-Actions.
- `ISyncLogger` ist der letzte Ctor-Parameter, der Name steht in `SyncLogServices.All` mit DisplayName; das ServiceSetting steht im Katalog (Drift-Guard).
- Mengeneingabe kulturfest über `TryMenge`: „2,5“ und „2.5“ ergeben beide 2,5.

**Bewertung der Dev-Abweichungen:**
- (a) Filter im Speicher vor Count/Paging ist zulässig. ADR 0005 verlangt Filtern vor dem Paging, nicht in SQL; es sind ca. 1.000 Zeilen,
  Präzedenz `WarehouseRequisitionsController`. Wächst der Cache deutlich, den Filter in SQL verlagern.
- (b) Die Lieferanten-Auswahl aus dem Cache statt aus allen Kreditoren ist besser als die Spec: Sie zeigt nur Lieferanten mit offenen Positionen,
  auch solche ohne WMS-Kreditor.
- (c) Ist-Menge als Text ist richtig, weil so aus „2.5“ nicht 25 werden kann.

### 3 Nachweisart je AK
| AK | Nachweis |
|---|---|
| 1 | automatisiert `Gate_Aus_KeinLauf_AK1`; auf DEV mit Schalter aus kein `SyncLogs`-Eintrag `EinkaufBestellungen` |
| 2 | automatisiert mit Fake-Client; **Manual-UAT** gegen AKE-Test-SData |
| 3 | automatisiert; Manual-UAT im Lauf-Protokoll |
| 4 | automatisiert (Empty-Guard, Lesefehler); Fehlermail ist Manual-UAT |
| 5 | **nicht bewiesen** (Q-3) |
| 6 | automatisiert `FeatureGate_NurTrue_Sonst404_AK6`; auf DEV IDEAL (Schalter aus) liefern beide Seiten 404, Menülinks fehlen |
| 7 | automatisiert, auf DEV OK |
| 8 | automatisiert nur der Katalog; Speichern über `/Settings` ist **Manual-UAT** (auf DEV nicht ausgeführt) |
| 9 | automatisiert, auf DEV OK |
| 10 | automatisiert (Attribute, `CanReceive`); **Manual-UAT** mit echtem stock_read-Benutzer |
| 11 | automatisiert; Hinweistext im Code belegt |
| 12 | automatisiert, auf DEV OK (Eingabe, Hardware-Scan, doppelte Nummer) |
| 13 | automatisiert (InMemory), auf DEV per SQL belegt |
| 14 | **Manual-UAT** (`SageLagerbuchungAktiv` bleibt auf DEV aus); im Code derselbe Decorator-Pfad wie bei der freien Einbuchung |
| 15 / 16 / 17 | automatisiert, auf DEV OK |
| 18 | E5 nicht gebaut (Folge-Spec) |

### 4 Geänderte bestehende Tests
- `AppSettingDefinitionsTests`: Anzahl 52 → 54. Das Verhalten ändert sich absichtlich durch die zwei neuen AppSettings dieser Spec. Weitere bestehende Tests wurden nicht geändert.

### 5 Checkliste und ADR 0020
- Migration, SQL und FreshInstall wie in Abschnitt 2 geprüft; `SQL/AgentJobs` nicht betroffen.
- Version 1.84.0 in beiden `AppVersion.cs`, Changelog, Hilfe und README nachgezogen.
- TS-116 in `docs/TESTSZENARIEN.md`, Zeile 116 im Testindex vorhanden.
- ADR 0020: Bool- und Int-Setting (1–26 über `I(min,max)`), Service-Schalter mit Anzeigename, Verwendung und Standort. Ein Warnhinweis ist nicht nötig, der Abgleich liest nur.

### 6 DEV-Deploy und Testlauf
- `SQL/112` und `SQL/113` je zweimal auf AKE_WMS_DEV und IDEAL_WMS_DEV ausgeführt. Lauf 1 meldet „erstellt“/„eingetragen“, Lauf 2 gibt nichts aus, Exit 0 (idempotent).
- `dev-deploy.ps1 -Standort AKE` und `-Standort IDEAL` (Quelle Worktree, elevated): beide Exit 0, „HTTP 200, Fusszeile v1.84.0“.
- Testdaten AKE_WMS_DEV: 7 Cache-Zeilen (BelPosId 990001–990021) mit diesen Fällen: Lieferant gesperrt, überfällig, ohne Termin, +40 Tage,
  Artikel fehlt, Einheit RLL ≠ Stk, Belegnummer doppelt. Dazu vorübergehend `WareneingangBestellungAktiv=true` und `HardwareScannerAktiv=true`.

| Schritt | Ergebnis | Status |
|---|---|---|
| IDEAL mit Schalter aus | 404, keine Menüpunkte | OK |
| Vorschau 2 Wochen | 6 von 7 Zeilen, +40 Tage fehlt; Markierungen als Text | OK |
| `wochen=8` + Lieferant | 3 Zeilen | OK |
| Spaltenfilter `offen >2` / `erfasst >0` mit Paging | 5 bzw. 2 Treffer | OK |
| Belegnummer doppelt | Meldung erscheint | OK (doppelt angezeigt, Q-2) |
| Hardware-Scan `~09QA-2026-900001` | Bestellung geöffnet | OK |
| Kopf, Lieferant gesperrt | Banner sichtbar, Buchen möglich | OK |
| Vorbelegung Ziel-Platz | bei buchbarem Platz OK; bei nicht buchbarem keine Vorbelegung (Q-7) | OK |
| „2,5“ und eine Zeile mit 0 | nichts gebucht, Fehler an der Zeile | OK (Q-1) |
| 12 > Rest ohne Häkchen und „2.5“ | nichts gebucht, Bestätigung verlangt | OK |
| mit Häkchen | 2 eingebucht; DB: 2 Einbuchungen mit Audit, 2 Receipts (Lieferart 3 und 2), Queue unverändert | OK |
| Artikel fehlt / Einheit abweichend | „nicht buchbar“ mit Grund | OK |
| „im WMS erfasst“ | 12 / 2,5, Rest 0 / 0,5 | OK |
| Telefonbreite | Fenster nicht verkleinerbar; im Code `table-responsive` mit Querscrollen | NICHT MOEGLICH |
| stock_read | Windows-Auth, QA kann den Benutzer nicht wechseln | NICHT MOEGLICH |
| Dienst-Abgleich | `Sync:*Enabled` bleibt auf DEV aus | NICHT MOEGLICH |

**Zurückgesetzt:** Auf AKE_WMS_DEV sind 2 Receipts, 2 `StockMovements` (19387/19388) und 7 Cache-Zeilen gelöscht und beide AppSettings entfernt (Default aus).
Kontrolle danach: 0 Receipts, 0 Cache-Zeilen, max StockMovement 19382, Queue 16, beide Service-Schalter false. Die Tabellen und Migrationen
112/113 bleiben auf beiden DEV-DBs bestehen.

### Manuelle Test-Checkliste (offen, nach dem Fix von Q-1 bis Q-3)
1. Q-3: `BelegLiefertermin` im `$schema` am AKE-Test belegen.
2. Testsystem: Backup, dann `SQL/112`, `SQL/113`, danach Web und Service.
3. TS-116.2: Abgleich an. Counts und Zeilenzahl EBB prüfen; eine Bestellung mit Mengen, **tagesgenauem Liefertermin** und Ziel-Platz gegen Sage vergleichen.
4. P-W6: `SELECT COUNT(*) FROM SagePurchaseOrderLines WHERE SagePlatzId IS NOT NULL AND StorageLocationId IS NULL;` muss 0 ergeben.
5. TS-116.2 negativ: falsches SData-Kennwort ergibt einen Fehlerlauf mit Mail, der Cache bleibt unverändert.
6. TS-116.3: `/Settings` weist 0, 27 und „abc“ ab und speichert 4.
7. TS-116.4: Ein stock_read-Benutzer sieht die Vorschau ohne Knopf; `Receive` weist ihn ab.
8. TS-116.5: Platz mit `SageBuchungErlaubt = 1` und `SageLagerbuchungAktiv = true` (nur Testsystem) erzeugt einen Queue-Eintrag je Bewegung.
9. Netum-Scanner mit einer gedruckten Bestellung (P-W5).
10. Handgerät/Telefon: Receive bedienen (Q-4).
11. Antwort 1 organisatorisch: Der Einkauf schließt Positionen in Sage ohne Lagerbuchung.

## QA-Wiederholung (qa-agent, 2026-11-07, Goal-Modus, Review-Umfang `0a941d63..d9621029`)

> [!warning] Ergebnis: **nicht Testbereit**, Status bleibt `InUmsetzung`.
> Q-1 bis Q-7 sind behoben bzw. belegt. Neuer Befund **Q-8**: Derselbe Kontrastfehler wie Q-1 steckt noch in der Statusanzeige „Überlieferung“
> (Client-Skript). Der Fix ist eine Klasse in einer Zeile; danach reicht ein Blick auf die Receive-Seite, kein neuer Datenlauf.

### 1 Build und Tests (HEAD `d9621029`)
- `dotnet build IdealAkeWms.slnx`: 0 Fehler, 28 Warnungen (unverändert).
- `dotnet test`: IdealAkeWms.Tests 2473 bestanden / 1 übersprungen / 0 Fehler (gesamt 2474, +1 durch den Q-5-Fall); IDEALAKEWMSService.Tests 503 / 0 / 0.

### 2 Review des Fix-Diffs (selbst, synchron)
| # | Prüfung | Ergebnis |
|---|---|---|
| Q-1 | `Receive.cshtml:144` `text-danger-emphasis` (#58151c auf #f8d7da, ca. 11:1) | behoben |
| Q-2 | TempData-Blöcke aus `Index`/`Receive` entfernt, `_Layout.cshtml:425-435` zeigt sie; `grep TempData[` in `Views/PurchaseOrders` leer | behoben, auf DEV genau ein Banner |
| Q-3 | E0 P-W1 nennt `BelegLiefertermin` jetzt mit Herkunft (JSON-Feldliste `EinkaufPositionenInfo`, AKE-Test). QA hat Sage nicht abgefragt und übernimmt den Beleg des Dev. | belegt (Beleg des Dev) |
| Q-4 | Wareneingang-Knopf, Auswahl, Ist-Menge, Häkchenzeile, Belegnummer, Öffnen mit `min-height:44px`; auf DEV gemessen ca. 44 px | behoben |
| Q-5 | `PurchaseOrdersController.Receive.cs:63` `decimal.Round(menge, 3) != menge` nach der >0-Prüfung, vor der Platzprüfung; Test `[InlineData(1001, "1,2345", …)]` | behoben |
| Q-6 | `EinkaufBestellungenSyncService.cs:126` zählt `mengeFehlt`, Warnung im Lauf, Counts-Key `menge_fehlt` (deutsch, snake_case wie die übrigen) | behoben; nur Code-Review, auf DEV nicht auslösbar (Sync aus) |
| Q-7 | JS zeigt bei Menge ≤ 0 „ungültig“ | behoben (Anzeige) |
| **Q-8** | **neu, mittel:** `Receive.cshtml:167` setzt bei `v > rest` weiter `text-danger` auf die Statusanzeige. In der Fehlerzeile „Überlieferung … bitte bestätigen“ (`table-danger`) steht damit „Überlieferung“ als `small fw-semibold` in #dc3545 auf #f8d7da = ca. **3,4:1** (AA verlangt 4,5:1). Auf DEV sichtbar (Pos. 2, Eingabe 12 > Rest 5, ohne Häkchen). Gleiche Defektklasse wie Q-1, dort nur an der Fehlerzeile behoben. | **offen**, Dev: in Z. 167 `text-danger` → `text-danger-emphasis` (auch auf Weiß ausreichend) |

Sonst ohne Befund: keine Werte in `<script>`, kein `@Html.Raw`, kein `hidden` mit `d-*`; die neue Serverregel läuft vor jeder Buchung (keine Teilbuchung).
Nebenbefund, nicht dieser Spec zuzurechnen: Bei 1520 px Fensterbreite hat jede Seite eine Querscrollleiste, weil die Navigationsleiste überläuft (auch Dashboard).

### 4 Geänderte bestehende Tests
- `PurchaseOrdersReceiveTests.NichtBuchbar_Gruende_AK15_AK16`: ein `InlineData` ergänzt (Q-5). Der Test gehört zu dieser Spec; Verhalten absichtlich erweitert (AK 15/16). Sonst keine.

### 6 DEV-Deploy und Testlauf
- `dev-deploy.ps1 -Standort AKE` und `-Standort IDEAL`, Quelle Worktree, elevated: beide Exit 0, „HTTP 200, Fusszeile v1.84.0“.
- Testdaten AKE_WMS_DEV: 2 Cache-Zeilen (BelPosId 990101/990102, Bestellung `QA2-2026-900100`, Artikel 2, Platz 2 „Test“, `SageBuchungErlaubt = 0`),
  vorübergehend `WareneingangBestellungAktiv=true`; App-Pool `AKE_WMS_DEV` recycelt, damit der Schalter greift.

| Schritt | Ergebnis | Status |
|---|---|---|
| Pos. 1 „1,2345“, Pos. 2 „12“ ohne Häkchen | nichts gebucht, ein Banner „Nichts gebucht …“; Pos. 1 „Höchstens 3 Nachkommastellen …“, Pos. 2 „Überlieferung (12 > Rest 5) bitte bestätigen.“ | OK |
| Kontrast Fehlerzeile | Fehlertext dunkelrot, gut lesbar | OK |
| Kontrast Statusanzeige Pos. 2 | „Überlieferung“ hellrot auf rosa | **FEHLER (Q-8)** |
| 44-px-Ziele (Receive, Index-Knopf) | Felder, Auswahl, Öffnen, Häkchenzeile, „Wareneingang“ je ca. 44 px | OK |
| Pos. 1 „2“ gebucht | genau ein Banner „1 Position(en) eingebucht (davon 1 Teil-, 0 Überlieferung).“; erfasst 2, Rest 8; Queue unverändert 16 | OK |
| Q-6-Warnung im Lauf | `Sync:*Enabled` bleibt aus | NICHT MOEGLICH |

**Zurückgesetzt:** 1 Receipt, `StockMovements` 19389, 2 Cache-Zeilen und das AppSetting gelöscht, App-Pool erneut recycelt. Kontrolle: 0 Receipts,
0 Cache-Zeilen, max StockMovement 19382, Queue 16, `SageLagerbuchungAktiv` und `Sync:EinkaufBestellungenEnabled` false. IDEAL_WMS_DEV nur deployt, keine Testdaten.

### Manuelle Test-Checkliste (offen)
1. Q-8 beheben, dann QA-Nachlauf nur für die Receive-Seite.
2. Punkte 2–11 der Checkliste oben unverändert (Punkt 1 Q-3 ist durch den Nachtrag in E0 erledigt).
3. Q-6: auf dem Testsystem im Lauf-Protokoll prüfen, dass Count `menge_fehlt` = 0 ist.

## QA-Nachlauf Q-8 (qa-agent, 2026-11-07, Goal-Modus, Review-Umfang `d9621029..26e6a278`)

> [!success] Ergebnis: **Testbereit.** Q-8 ist behoben und auf DEV AKE gemessen. Keine neuen Befunde.

### 1 Build und Tests (Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, HEAD `26e6a278`)
- `dotnet build IdealAkeWms.slnx`: 0 Fehler, 28 Warnungen (unverändert).
- `dotnet test`: IdealAkeWms.Tests 2473 bestanden / 1 übersprungen / 0 Fehler (gesamt 2474); IDEALAKEWMSService.Tests 503 / 0 / 0.

### 2 Review des Fix-Diffs (selbst, synchron)
| # | Prüfung | Ergebnis |
|---|---|---|
| Q-8 | `Receive.cshtml:167`: `text-danger` → `text-danger-emphasis`, sonst nichts geändert (1 Zeile). `grep text-danger` in `Views/PurchaseOrders` zeigt nur noch `text-danger-emphasis` (Z. 144, 167). Der Kommentar steht vor dem `;` und ist gültiges JS. | behoben |

Keine Befunde darüber hinaus. Geprüft: der Fix-Diff und die Skriptumgebung (`Receive.cshtml:157-171`): keine Werte in `<script>`, kein `@Html.Raw`, kein `hidden` mit `d-*`.

### 4 Geänderte bestehende Tests
- Keine im Fix-Diff.

### 6 DEV-Deploy und Testlauf
- `dev-deploy.ps1 -Standort AKE -Quelle <Worktree>`, elevated: Exit 0, „HTTP 200, Fusszeile v1.84.0“ (= AppVersion Web und Service).
- Ausgelieferter Stand: `D:\Dev\AKE\Web\IdealAkeWms.dll` enthält `(v > rest ? 'text-danger-emphasis' : '')` 1×, die alte Fassung mit `'text-danger'` 0×.
- Testzeile AKE_WMS_DEV: 1 Cache-Zeile (BelPosId 990201, BelId 990200, Bestellung `QA3-2026-900200`, Artikel 2, Platz 2 „Test“, `SageBuchungErlaubt = 0`, bestellt/offen 5).
  `WareneingangBestellungAktiv` stand schon vor dem Lauf auf `true` (abweichend vom Reset-Vermerk der QA-Wiederholung) und blieb unverändert.

| Schritt | Ergebnis | Status |
|---|---|---|
| Pos. 1 „12“ ohne Häkchen, Einbuchen | nichts gebucht, ein Banner „Nichts gebucht …“, Zeile `table-danger`, Status „Überlieferung“ + „Überlieferung (12 > Rest 5) bitte bestätigen.“ | OK |
| Kontrast Statusanzeige „Überlieferung“ | dunkelrot sichtbar; `--bs-danger-text-emphasis` #58151c auf `.table-danger` #f8d7da = **10,2:1** (auf Weiß 13,7:1; vorher #dc3545 = 3,4:1) | OK |
| DB nach dem Versuch | 0 Receipts, max StockMovement 19382 (unverändert) | OK |

**Zurückgesetzt:** Cache-Zeile 990201 gelöscht. Kontrolle: 0 Cache-Zeilen, 0 Receipts, max StockMovement 19382, kein `Sync:*Enabled` an,
`SageLagerbuchungAktiv` nicht gesetzt (Default aus). `WareneingangBestellungAktiv=true` wie vorgefunden. IDEAL_WMS_DEV in diesem Nachlauf nicht neu deployt (Fix betrifft nur eine View, Stand dort `d9621029`).

### Manuelle Test-Checkliste (final, offen für den Menschen)
1. Testsystem: Backup, dann `SQL/112`, `SQL/113`, danach Web und Service (Reihenfolge siehe Deploy).
2. TS-116.2: Abgleich an. Counts und Zeilenzahl EBB prüfen; eine Bestellung mit Mengen, tagesgenauem Liefertermin und Ziel-Platz gegen Sage vergleichen; Count `menge_fehlt` = 0 (Q-6).
3. P-W6: `SELECT COUNT(*) FROM SagePurchaseOrderLines WHERE SagePlatzId IS NOT NULL AND StorageLocationId IS NULL;` muss 0 ergeben.
4. TS-116.2 negativ: falsches SData-Kennwort ergibt einen Fehlerlauf mit Mail, der Cache bleibt unverändert.
5. TS-116.3: `/Settings` weist 0, 27 und „abc“ ab und speichert 4.
6. TS-116.4: Ein stock_read-Benutzer sieht die Vorschau ohne Knopf; `Receive` weist ihn ab.
7. TS-116.5: Platz mit `SageBuchungErlaubt = 1` und `SageLagerbuchungAktiv = true` (nur Testsystem) erzeugt einen Queue-Eintrag je Bewegung.
8. Netum-Scanner mit einer gedruckten Bestellung (P-W5).
9. Handgerät/Telefon: Receive bedienen (Q-4, 44-px-Ziele, Querscrollen).
10. Antwort 1 organisatorisch: Der Einkauf schließt Positionen in Sage ohne Lagerbuchung.
11. Optional DEV IDEAL vor dem Test erneut aus dem Worktree deployen, damit beide DEV-Sites `26e6a278` zeigen.

## Nachbesserung nach Sichtprüfung (Gerald 2026-11-07 19:13, Koordination) – offen für den nächsten Lauf
- **„Offene Bestellungen“: gleiches Farbkonzept wie die übrigen Listen.** Auf AKE-DEV v1.84.0 sind die Zeilen gelb/beige hinterlegt
  (Screenshot Koordination 19:11) statt im üblichen Listenstil (Bootstrap-Streifen wie Kreditoren, Lagerplätze, FA-Liste). Zeilenfarben nur
  mit Bedeutung und nach bestehender Konvention: überfällig = rot (wie FA-Struktur/Teileverfolgung, `table-danger` am `<tr>`), sonst
  neutral; Legende bzw. Tooltip wie in den anderen Listen. Vor dem Bau am Code belegen, welche Klasse die Zeilen heute färbt und welche
  Konvention die FA-Liste nutzt ([[fallstricke]] „CSS: Spezifität …“); Sichtprüfung am Bildschirm Pflicht.
- Zur Prüfung (Koordination, ungeprüft): Ziel-Lagerplatz in der Wareneingangsmaske nicht vorbelegt (Bestellung 2027-168705, EK023579);
  Einheit `PKG` ohne Markierung – prüfen, ob Lagermengeneinheit gleich ist oder die B-2-Sperre nicht greift.

## Umsetzung Nachbesserung nach Sichtprüfung (Dev, 2026-11-08, Lauf 2) – v1.86.0, Commit `405e9af5`

- **Farbkonzept:** Vorschau `Views/PurchaseOrders/Index.cshtml` – nur überfällige Zeilen `table-danger` (statt `table-warning`/beige), sonst das
  Streifenmuster wie Kreditoren/Lagerplätze; Legende „rot hinterlegt = überfällig“, Text „überfällig“ bleibt in der Hinweis-Spalte (Farbe nie allein).
  Knopf „Wareneingang“ gefüllt (`btn-primary`), weil der Umriss-Knopf auf Rot unter 4,5:1 fällt. **Sichtprüfung am Bildschirm: QA (TS-116.8).**
- **Einheit PKG (EK023579 / Bestellung 2027-168705):** Bestell-, Basis- und Lagereinheit sind PKG → buchbar, korrekt. Beim Prüfen gefunden:
  EK003604 (Bestellung PKG, **Basis Stk**, Lager PKG) war nicht gesperrt, obwohl die Menge in der Basiseinheit gebucht wird. Sperre jetzt
  „Bestell- **oder** Basiseinheit ≠ Lagereinheit“ (`EinkaufBestellungenSyncService`), Zeilentext nennt alle drei Einheiten.
- **Ziel-Lagerplatz-Vorbelegung:** leer, weil der Sage-Platz (PlatzID 11) dem WMS-Platz `HR;0;0;0` zugeordnet ist, der `IstBuchbar = 0` hat und
  daher nicht in der Auswahl steht. Keine stille Leere mehr: Hinweis „Sage-Platz … ist im WMS nicht buchbar – bitte Ziel wählen“. Ob `HR;0;0;0`
  buchbar sein soll, ist Stammdatenpflege (Gerald), kein Code.
- Tests: `EinheitAbweichend_BestellOderBasisUngleichLager_Nachbesserung_v1_86` (3 Fälle), View-Guard `Nachbesserung_v1_86_…`; TS-116.8/116.9.

## QA Nachbesserung v1.86.0 (qa-agent, 2026-11-07, Bündel 2026-11-08 Lauf 2) – **Testbereit**

Geprüft wurde der Diff `git diff 1dac3bcd 405e9af5`. Build und Tests liefen auf dem ganzen Worktree mit HEAD `405e9af5`. Den Review habe ich selbst in diesem Lauf gemacht, nicht delegiert.

### 1 Build und Tests

`dotnet build IdealAkeWms.slnx`: 0 Fehler, 28 Warnungen (die bekannten).

| Testprojekt | bestanden | übersprungen | Fehler | gesamt |
|---|---|---|---|---|
| IdealAkeWms.Tests | 2482 | 1 | 0 | 2483 |
| IDEALAKEWMSService.Tests | 506 | 0 | 0 | 506 |

### 2 Review – Befunde und Behandlung

| # | Schwere | Befund | Behandlung |
|---|---|---|---|
| N-1 | Niedrig | Das Legenden-Muster `badge text-bg-danger` ist kräftig rot (#dc3545). Die überfälligen Zeilen sind `table-danger`, also hellrot (#f8d7da). Das Muster sieht damit anders aus als der Zeilenton (Screenshot 2). Bedeutung und Kontrast stimmen: Weiß auf #dc3545 hat 4,5:1, und der Text „überfällig“ steht zusätzlich in der Spalte „Hinweis“. | Optional: Muster im Zeilenton, z. B. `bg-danger-subtle text-danger-emphasis border border-danger-subtle`. Nicht blockierend. |
| N-2 | Info | Die neue Einheitensperre wirkt erst **nach dem nächsten Abgleichlauf**, weil das Flag `EinheitAbweichend` im Cache gespeichert ist. Auf AKE-DEV (Cache 19:31, noch alte Regel) sind 7 Zeilen gesperrt. Nach der neuen Regel wären es 31 (SQL-Gegenprobe: Bestell- oder Basiseinheit ≠ Lagereinheit). Darunter ist EK003604 (BelPosId 406071), das heute noch `0` hat. | Auf dem Testsystem nach dem Deploy erst einen Abgleich laufen lassen, dann TS-116.9 prüfen. Danach sind etwa viermal so viele Positionen gesperrt. Das ist fachlich gewollt. |
| N-3 | Hinweis (nicht neu) | Auf `/PurchaseOrders` und `/Receive` erscheint bei 1520 px zusätzlich ein seitenweiter Querscrollbalken. Er kommt von der Menüleiste: Der Benutzername „Gerald Weich…“ wird abgeschnitten. Dieser Diff verursacht ihn nicht. | Nur vermerkt (allgemeines Navbar-Verhalten). |

**Ohne Befund geprüft:**

- **Sperre** (`EinkaufBestellungenSyncService.cs:131-132`): Der Vergleich läuft nach `Text()` (Trim, leer→null) mit `OrdinalIgnoreCase`. Fehlt die Basiseinheit bei vorhandener Lagereinheit, wird gesperrt. Das ist die vorsichtige Richtung, es wird nichts still gebucht.
- **Feld `MengeBasiseinheit`:** Es ist eine Einheiten-Zeichenkette (`nvarchar(5)`), im Select gelesen (`:34`) und im Cache gespeichert (`:143`). Auf DEV belegt: EK003604 hat `PKG/Stk/PKG`.
- **Durchsetzung am Server** (`PurchaseOrdersController.Receive.cs:60`): `z.NichtBuchbar` hat Vorrang vor allen anderen Prüfungen. Der Ziel-Platz muss außerdem in `erlaubtePlaetze` liegen (`:64`). Ein nicht buchbarer Platz wird also auch serverseitig abgewiesen.
- **Ziel-Hinweis** (`Receive.cshtml:115`): Er erscheint nur, wenn ein WMS-Platz zugeordnet ist, der nicht in `Model.Lagerplaetze` steht. Er schließt sich mit dem vorhergehenden Zweig „Sage-Platz nicht im WMS“ aus. `ZielLagerplatz` kommt aus `GetAllOrderedAsync` (alle Plätze) und zeigt den WMS-Code.
- **Farbkonzept:** `table-danger` statt `table-warning`, nur bei `Ueberfaellig`. Sonst gilt `table-striped` wie bei Kreditoren und Lagerplätzen. Der Text „überfällig“ steht in der Hinweis-Spalte, die Farbe ist also nicht allein Bedeutungsträger.
- **Kontrast:** Text #212529 auf #f8d7da ≈ 13:1. `btn-primary` (Navy, Weiß) auf Rot ist gut lesbar.
- **Sonstiges:** keine Werte in `<script>`, kein `Html.Raw`, kein `hidden` zusammen mit `d-*`. Keine neue Einstellung, ADR 0020 ist nicht betroffen. Keine Migration.

### 3 Nachweisart

| Punkt | Nachweis |
|---|---|
| Sperre Bestell- oder Basiseinheit ≠ Lagereinheit | Service-Test `EinheitAbweichend_BestellOderBasisUngleichLager_Nachbesserung_v1_86` mit 3 Fällen (PKG/PKG/PKG frei, PKG/Stk/PKG gesperrt, PKG/Stk/Stk gesperrt). Mit echten SData-Daten: Manual-UAT, weil auf DEV kein Lauf erlaubt ist. |
| Farbkonzept, Legende, Kontrast | Quelltext-Test `Nachbesserung_v1_86_Farbkonzept_…` und **Sichtprüfung DEV** (Screenshots unten) |
| Hinweis bei nicht buchbarem Ziel-Platz | Quelltext-Test und DEV (2027-168705) |

### 4 Geänderte bestehende Tests

- `EinkaufBestellungenSyncTests.Pos(...)`: neuer optionaler Parameter `basis`, Default = `me` wie bisher. Das ist eine reine Fixture-Erweiterung, das Verhalten der bestehenden Tests bleibt gleich. Keine Einstufung als „sonstige“.

### 5 Checkliste und Deploy-Nachtrag

- Version 1.86.0 steht in beiden `AppVersion.cs`. Anwender-Changelog und Hilfe sind ergänzt.
- TS-116.8 und 116.9 stehen in `docs/TESTSZENARIEN.md` (Worktree). Den Testindex habe ich in diesem Lauf nachgezogen.
- **Offen (nicht blockierend):** Brain-Changelog v1.86.0.

**Deploy v1.86.0** (Nachtrag zum Abschnitt „Deploy“, aus dem Diff `1dac3bcd..405e9af5`):

- **Web:** ja. Betroffen sind die Views `PurchaseOrders/Index|Receive`, das ViewModel, der Modell-Kommentar, Hilfe, Changelog und AppVersion.
- **Service:** ja. Betroffen sind `EinkaufBestellungenSyncService` und AppVersion.
- **Migration:** nein.
- **Reihenfolge:** wie oben. Nach dem Service-Deploy den Abgleich einmal laufen lassen, damit die neue Sperre greift (N-2).

### 6 DEV-Deploy und Testlauf

**Deploy** mit `dev-deploy.ps1 -Quelle <Worktree>`, elevated:

| Standort | Exit | Rückmeldung |
|---|---|---|
| AKE | 0 | „HTTP 200, Fusszeile v1.86.0“ |
| IDEAL | 0 | „HTTP 200, Fusszeile v1.86.0“ |

Die Fußzeile entspricht der Branch-AppVersion.

**Daten AKE_WMS_DEV:** Der Cache hat 1.059 Positionen (Stand 07.11.2026 19:31), davon 1.017 überfällig. `WareneingangBestellungAktiv` stand auf true und blieb so.

Die Screenshots liegen unter `C:\Users\GERALD~1.WEI\AppData\Local\Temp\claude-chrome-screenshots-cF2Dyv\`.

| Schritt | Ergebnis | Status |
|---|---|---|
| 116.8 Vorschau ohne Filter | Überfällige Zeilen hellrot (`table-danger`). Die Legende „rot hinterlegt = überfällig …“ steht über der Tabelle. Kein Gelb oder Beige mehr. Screenshot `screenshot-1794077622087-2.jpg` | OK |
| 116.8 künftige Positionen | Filter Liefertermin `>07.11.2026`: 4 Zeilen im neutralen Streifenmuster (weiß/grau), Knopf „Wareneingang“ gefüllt. Screenshots `screenshot-1794077645576-4.jpg`, `screenshot-1794077682504-5.jpg` | OK |
| 116.8 Hinweis-Spalte | 2027-168705: Zeile rot, Hinweis „überfällig“, Knopf `btn-primary` gut lesbar. Screenshot `screenshot-1794077715100-6.jpg` | OK |
| Kontrast | Zeilentext auf #f8d7da ≈ 13:1, Legende 4,5:1, Knopf Weiß auf Navy. Die Farbe trägt die Bedeutung nicht allein. | OK (Farbton der Legende: N-1) |
| 116.9 EK023579 (2027-168705) | PKG/PKG/PKG ohne Markierung, Eingabefeld vorhanden. Ziel steht auf „— wählen —“ mit dem Hinweis „Sage-Platz HR;0;0;0 ist im WMS nicht buchbar – bitte Ziel wählen“ (HR;0;0;0 = Id 4, `IstBuchbar = 0`). Screenshot `screenshot-1794077731065-7.jpg` | OK |
| 116.9 EK003604 (2027-168942) | Testdatum: `EinheitAbweichend = 1` per SQL an BelPosId 406071 gesetzt, also das, was der nächste Lauf schreibt. Pos. 1 zeigt „nicht buchbar – Einheit abweichend (Bestellung PKG, Basis Stk, Lager PKG) – nicht buchbar, bis geklärt“ ohne Eingabefeld. Pos. 2 (EK015029) zeigt den Ziel-Hinweis „Sage-Platz SL;10;1;0 ist im WMS nicht buchbar“. | Anzeige OK. Setzen durch den echten Lauf: NICHT MÖGLICH (kein Sync auf DEV) |
| Buchung | Nicht ausgeführt, kein Formular übermittelt. | – |

**Zurückgesetzt und geprüft:**

- BelPosId 406071 steht wieder auf `EinheitAbweichend = 0`. Damit sind wie vorgefunden 7 Zeilen gesperrt.
- 0 Receipts, max StockMovement 19382 (unverändert).
- Kein `Sync:*Enabled` an, `SageLagerbuchungAktiv` nicht gesetzt.

### Manuelle Test-Checkliste (offen für den Menschen)

1. Testsystem: v1.86.0 Web und Service deployen, dann **einen Abgleich laufen lassen**.
   - TS-116.9 an EK003604 prüfen: Die Position muss gesperrt sein.
   - Die Zahl der gesperrten Positionen im Aktivitäts-Protokoll mit dem vorigen Lauf vergleichen (N-2).
2. TS-116.8 am Fertigungsterminal bzw. Handgerät: Sind Rot und Legende auch bei schlechtem Licht unterscheidbar?
3. Stammdatenpflege: Soll `HR;0;0;0` buchbar sein? Bei Ja `IstBuchbar` setzen, dann wird der Ziel-Platz vorbelegt.
4. N-1 optional entscheiden: Legende im Zeilenton.
5. Weiterhin offen sind die Punkte 1–11 der „Manuellen Test-Checkliste (final)“ aus dem QA-Nachlauf Q-8.


## Entscheidungen HR / N-1 (Gerald 2026-11-08 07:52)
- **`HR;0;0;0` wird buchbar** – Stammdatenänderung (`IstBuchbar = 1`), macht Gerald je Testsystem/produktiv; kein Code nötig. Danach wird
  der Ziel-Lagerplatz für Sage-Platz 11 vorbelegt (Testschritt TS-116 erneut).
- **N-1:** Legende „rot hinterlegt“ im **Zeilenton** (`table-danger`-Hellrot mit dunklem Text statt `badge text-bg-danger`) – Nachbesserung im
  nächsten Lauf, Kontrast ≥ 4,5:1, Sichtprüfung.
- **DPS-MDE (Rückfrage 9), Gerald 2026-11-08 07:57:** DPS hat ein MDE-Wareneingangsmodul, **bei uns nicht im Einsatz/lizenziert** → keine
  Doppelentwicklung; E5 hängt nur noch an P-W7 und einem freigegebenen Schreibtest am AKE-Test.

## Umsetzung Nachbesserung v1.91.0 (Dev 2026-11-08, Auftrag Gerald; nur N-1)

Commit `bacf3b82`. `Views/PurchaseOrders/Index.cshtml`: Legende `badge bg-danger-subtle text-danger-emphasis border border-danger-subtle`
(Hellrot #f8d7da wie `table-danger`, Text #58151c ≈ 12:1). Wächter in `PurchaseOrdersControllerTests`. TS-116.8 angepasst. HR ist
Stammdatenpflege (Gerald), kein Code.

## QA v1.91.0 (2026-11-08) – Ergebnis: Testbereit

qa-agent. Prüfbereich: Commit `bacf3b82` (nur N-1).

### 1. Build, Tests, Deploy
- Build 0 Fehler; Tests Web 2542/1/0, Dienst 507/0/0 (Worktree HEAD `2efdb023`).
- AKE-DEV (standort AKE) läuft auf v1.94.0 aus dem Worktree; Fußzeile gemessen.

### 2. Code-Review
Eine Zeile in `Views/PurchaseOrders/Index.cshtml`: Klassen von `text-bg-danger` auf `bg-danger-subtle text-danger-emphasis` umgestellt. Bootstrap-Bordmittel, kein neues CSS. Die Bedeutung trägt weiter der Text („rot hinterlegt“, Spalte „Hinweis“). **Befunde: keine.**

### 3. Beweisart und Tests
- Kontrast: Sichtprüfung mit gemessenem Wert (unten).
- Der View-Wächter in `PurchaseOrdersControllerTests` ist ergänzt (neue Asserts, keine geänderte Erwartung). Keine weiteren geänderten Bestandstests.

### 4. Checkliste
Keine Migration, keine Einstellung. Version 1.91.0, Anwender- und Brain-Changelog (`2026-11-08-v1-91-0-offene-bestellungen-legende`) vorhanden. TS-116.8 angepasst.

### 5. DEV-Testlauf (AKE http://localhost:8081/PurchaseOrders, 1520 px)
| Schritt | Ergebnis | |
|---|---|---|
| 116.8 Legende „rot hinterlegt“ | Hintergrund rgb(248,215,218) = gleicher Ton wie die überfälligen Zeilen (`tr.table-danger td` gemessen: rgb(248,215,218)); Text rgb(88,21,28) → **10,22:1**, fett, 10,5 px | OK |
| 116.8 Zeilen | überfällige Zeilen hellrot, Text schwarz 15,72:1; Seite ohne Querscrollen (scrollWidth 1505 ≤ 1520) | OK |

Screenshot (temporär): `C:/Users/GERALD~1.WEI/AppData/Local/Temp/claude/8eae370df6a92eae/8d5dd4d6-9b10-461a-954c-3abd2e65c8bf/scratchpad/nav/ake-purchaseorders-1520.png`.

### Deploy (aus dem echten Diff dieses Nachtrags)
web: ja · service: ja (nur Version) · migration: nein (dieser Nachtrag; SQL 112/113 der Spec bleiben gültig).
```
dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService
```
Nach dem Merge nur dann aus main neu publizieren, wenn der Merge getestete Dateien mit parallelen main-Änderungen kombiniert hat.

### Manuelle Checkliste (offen)
1. Testsystem `/PurchaseOrders`: Die Legende hat den Ton der überfälligen Zeilen und ist am Terminal lesbar.

## Nachbesserung Abnahme 2026-11-08 (v1.95.0, Ausnahme Schranke 1 Gerald)

Quelle: Vorab-Abnahme auf DEV, [[2026-11-08-buendel-dev-v1-94-browser-testbericht]], Befund 1 (B5.1): Offene Bestellungen ohne Zahnrad, `GET /api/user-view-preferences/PurchaseOrders`
→ 400 „Invalid view key“.
- `ColumnDefinitions.PurchaseOrders` + Eintrag in `GetByViewKey`; `Views/PurchaseOrders/Index.cshtml`: Toolbar-Host `data-table-toolbar`,
  `#view-config`/`#column-config` (Muster Kreditoren). Gesperrt: Liefertermin, Bestellung, Artikel, Aktionen.
- Drift-Wächter `ViewKeyDriftGuardTests`: jede View mit `#view-config` muss ihren viewKey registriert haben; für Offene Bestellungen zusätzlich
  th = `#column-config` = Katalog.
- TS-116.10. Commit `897c5466`.

## QA Nachbesserung Abnahme v1.95.0 (2026-11-08) – Ergebnis: Testbereit

qa-agent. Prüfbereich: `git diff 9c9046c4..897c5466` im Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`.

### 1. Build und Tests (selbst ausgeführt, Worktree HEAD `897c5466`)
- `dotnet build IdealAkeWms.slnx`: 0 Fehler (28 Warnungen, Bestand).
- `dotnet test`: IdealAkeWms.Tests 2571 bestanden / 1 übersprungen / 0 Fehler; IDEALAKEWMSService.Tests 507 / 0 / 0.
- DEV nicht neu deployt (Auftrag: DEV steht bereits auf v1.95.0, `dev-deploy.ps1` würde die fünf AKE-Sync-Schalter abschalten). Fußzeile gemessen: AKE `localhost:8081` „IDEAL-AKE WMS v1.95.0“; IDEAL `localhost:8082` zeigt den Stand v1.95.0 (Hilfe-Text aus diesem Commit sichtbar).
- Sync-Schalter AKE-DEV am Ende unverändert **true**: `Sync:ArticlesEnabled`, `Sync:ArtikelLieferantEnabled`, `Sync:KreditorenEnabled`, `Sync:DebitorenEnabled`, `Sync:EinkaufBestellungenEnabled`.

### 2. Code-Review (Diff `9c9046c4..897c5466`, 13 Dateien, selbst gelesen)
Geprüft: `ColumnDefinitions.PurchaseOrders` gegen `th[data-col-key]` und `#column-config` (13 Schlüssel, gleiche Reihenfolge, 13 `td` je Zeile), Muster Kreditoren (`Views/Creditors/Index.cshtml:104-200`), `column-preferences.js` (Reset per `DELETE` ohne `confirm`), `_FaLink.cshtml` und `StockApiController.FaLinkAsync` gegen `ProductionOrderRepository.cs:141-142` (`!showDone` blendet `IsDone`, `IsCancelled`, `IsDonePicking` aus), alle Nutzer des Partials (`StockMovements/Index.cshtml:140`, `StockOverview/Index.cshtml:83`), `PickingLeitstand/Index.cshtml:254-353` (`activePickers` nur im Block `PickerAssignmentEnabled`, `ViewBag.ActivePickers` aus `UserRepository.GetActivePickersAsync` = `IsActive && IsPicker`), Hilfe-Stellen, `AppSettingDefinitions.cs:99-100`. JSON-Blöcke statisch, kein `@Html.Raw`; keine `hidden`/`d-*`-Kombination, kein stiller Fallback, keine Parallel-Arrays, keine reine Client-Validierung.

**Befunde (Befund 1): keine.** Anmerkung: Die Aktionsspalte erscheint im Zahnrad-Menü als leere gesperrte Zeile „(Pflichtspalte)“, wie bei Kreditoren.

### 3. Beweisart
| Punkt | Beweis |
|---|---|
| Katalogeintrag, th = `#column-config` = Katalog, Toolbar-Host | automatisch: `ViewKeyDriftGuardTests` (neu; Theory über alle Views mit `#view-config` + Fakt Offene Bestellungen) |
| Zahnrad sichtbar, Ausblenden bleibt nach Neuladen, Pflichtspalten gesperrt | Manual-UAT, auf DEV erledigt (unten) |

### 4. Geänderte Bestandstests
Keine. Nur neue Dateien (`ViewKeyDriftGuardTests.cs`, `AbnahmeNachbesserungV195Tests.cs`).

### 5. Checkliste
Keine Migration, kein SQL, keine Einstellung (ADR 0020 nicht berührt). Version 1.95.0 in beiden `AppVersion.cs`, Anwender-Changelog v1.95.0, Brain-Changelog [[2026-11-08-v1-95-0-nachbesserung-vorab-abnahme]]. TS-116.10 in `docs/TESTSZENARIEN.md`; Testindex Zeile 116 durch QA nachgezogen (fehlte).

### 6. DEV-Testlauf AKE (http://localhost:8081, Benutzer Gerald Weichbold, UserId 14)
| Schritt | Ergebnis | |
|---|---|---|
| Ausgangszustand | `UserViewPreferences` ViewKey `PurchaseOrders`: 0 Zeilen | – |
| 116.10 Zahnrad rechts über der Tabelle | sichtbar neben CSV | OK |
| Menü öffnen | Liefertermin, Bestellung, Artikel, Aktionen gesperrt „(Pflichtspalte)“, übrige frei | OK |
| „Hinweis“ ausblenden, neu laden | Spalte bleibt ausgeblendet; DB `SettingsJson` `"key":"hinweis","visible":false`; Log `PUT … 200` | OK |
| „Hinweis“ wieder einblenden | Log `PUT … 200` | OK |
| Ausgangszustand herstellen („Auf Standard zurücksetzen“) | Log `DELETE /api/user-view-preferences/PurchaseOrders responded 200`, danach `GET … 204`; DB 0 Zeilen | OK |

Beleg API: `D:\Dev\AKE\Web\logs\idealakewms-20261108.log` 11:24:24 `GET … 204` (in der Abnahme 400), 11:25:56 `DELETE … 200`, 11:25:56/57 `GET … 204`. Die Statuscodes 503 im Netzwerkmitschnitt der Browser-Erweiterung sind ein Artefakt; Server-Log und DB zeigen 200/204.

Screenshots: `C:/Users/GERALD~1.WEI/AppData/Local/Temp/claude-chrome-screenshots-cF2Dyv/screenshot-1794133470690-28.jpg` (Zahnrad), `C:/Users/GERALD~1.WEI/AppData/Local/Temp/claude-chrome-screenshots-cF2Dyv/screenshot-1794133479768-29.jpg` (Menü), `C:/Users/GERALD~1.WEI/AppData/Local/Temp/claude-chrome-screenshots-cF2Dyv/screenshot-1794133528277-30.jpg` (nach Neuladen ohne „Hinweis“), `C:/Users/GERALD~1.WEI/AppData/Local/Temp/claude-chrome-screenshots-cF2Dyv/screenshot-1794133565601-32.jpg` (Standard wiederhergestellt).

### Deploy (aus dem echten Diff `9c9046c4..897c5466`)
web: ja · service: ja (nur Version) · migration: nein.
```
dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService
```
Nach dem Merge nur dann aus main neu publizieren, wenn der Merge getestete Dateien mit parallelen main-Änderungen kombiniert hat.

### Manuelle Checkliste (offen)
1. Testsystem AKE `/PurchaseOrders`: Zahnrad, eine Spalte aus- und einblenden, „Auf Standard zurücksetzen“.
2. Zweiter Benutzer: Auswahl ist je Benutzer getrennt.
