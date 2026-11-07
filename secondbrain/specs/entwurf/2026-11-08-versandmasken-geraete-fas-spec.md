---
type: spec
title: "Versand-Dispo: offene Kundenauftraege aus Sage, Ladungen disponieren (je WMS, gleiche Maske AKE und IDEAL)"
slug: 2026-11-08-versandmasken-geraete-fas-spec
status: Entwurf
created: 2026-11-07
updated: 2026-11-07
source_backlog: "[[2026-11-07-versandmasken-geraete-fas]]"
buendel: "[[2026-11-08-buendel-uebersicht]]"
task: ""
worktree: ""
branch: ""
standort: beide
version: "1.85.0"
sql_nummern: "117-121"
testszenario: "TS-117"
affected_code:
  - "NEU IdealAkeWms/Models/SageVerkaufPosition.cs (Cache offener AB-Positionen, KEIN AuditableEntity - Praezedenz FaHierarchyOrderInfo.cs:5-11) - Etappe 1"
  - "NEU IDEALAKEWMSService/Services/ISageVerkaufReader.cs + SageVerkaufReader.cs (SQL auf Sage-DB, Spalten nach P-V2) + VerkaufAuftraegeSyncService.cs (Full-Refresh, ISyncLogger letzter Ctor-Parameter) - Etappe 1"
  - "IDEALAKEWMSService/Workers/SyncWorker.cs Z. 408-473 (Muster Gate je Lauf, RunResilientAsync): neuer Block Sync:VerkaufAuftraegeEnabled - Etappe 1"
  - "IdealAkeWms/Services/SyncLogger/SyncLogServices.cs (Konstante + All + DisplayNames 'Verkauf-Auftraege') - Etappe 1"
  - "IdealAkeWms/Models/ServiceSettingDefinitions.cs Z. 87-88 (Anzeige-Metadaten) + Z. 192-193 (Bool-Muster): Sync:VerkaufAuftraegeEnabled - Etappe 1; Z. 78-83 + 183-188 + Z. 127 (Choice): Sync:Quelle:Verkauf - Etappe 2"
  - "NEU IdealAkeWms/Controllers/VersandController.cs (Action OffenePositionen, Listen-View-Pattern) + Views/Versand/OffenePositionen.cshtml + ColumnDefinitions-Eintrag - Etappe 1"
  - "IdealAkeWms/Models/RoleKeys.cs Z. 23-26 (Muster): Versand, VersandSchreiben; NEU Filters/RequireVersandAccessAttribute.cs + RequireVersandWriteAccessAttribute.cs; Services/ICurrentUserService.cs + CurrentUserService.cs Z. 138/141 (Muster) - Etappe 1"
  - "IdealAkeWms/Views/Users/RoleOverview.cshtml + secondbrain/codebase/controller.md (Rollen-Dreierregel) - Etappe 1"
  - "NEU IDEALAKEWMSService/Services/Sdata/SdataVerkaufReader.cs + VerkaufQuelleSwitch.cs; SdataRegistrierung.cs Z. 28-39 (Muster) AddVerkaufQuelle; SageQuelleSchalter.cs Z. 16-24 (+ Verkauf); IdealAkeWms/Services/SageSdata/SageQuelleSperre.cs - Etappe 2"
  - "IdealAkeWms/Models/Creditor.cs Z. 66-68 (Muster IsCoater): + IsSpedition; Controllers/CreditorsController.cs Z. 50/75/227; Views/Creditors/_Form.cshtml Z. 34-35, Index.cshtml Z. 93/119/194, Details.cshtml Z. 79; ColumnDefinitions.cs Z. 619 - Etappe 3"
  - "NEU IdealAkeWms/Models/VersandLadung.cs + VersandLadungPosition.cs + VersandLadungStatus.cs + VersandLieferterminArt.cs; Data/ApplicationDbContext.cs; Repository + Interface; VersandController (Index, Create, Edit, Positionen zuordnen, Status); Views/Versand/* - Etappe 4"
  - "IdealAkeWms/Models/AppSettingDefinitions.cs Z. 44-47 (S-Helper multiline): VersandVerkehrskuerzel - Etappe 4"
  - "VersandController.Auswertung + Druck-View Ladeliste - Etappe 5"
  - "VersandController.OffenePositionen + Index: FA-Spalte aus FaHierarchyOrderInfos (IDEAL) - Etappe 6"
  - "SQL/117_AddSageVerkaufPositionen.sql, SQL/118_AddVersandRoles.sql, SQL/119_AddCreditorIsSpedition.sql, SQL/120_AddVersandLadungen.sql, SQL/121 reserviert (Etappe 8), SQL/00_FreshInstall.sql je an zwei Stellen"
  - "IdealAkeWms/Views/Shared/_Layout.cshtml (Menue Versand), Views/Help/Index.cshtml, Views/Help/Changelog.cshtml, beide AppVersion.cs (1.85.0), README.md (AppSettings), docs/TESTSZENARIEN.md TS-117"
open_questions:
  - "1 Pruefabfragen P-V1 bis P-V3 ausfuehren und Ergebnis in die Spec eintragen (Spaltennamen VK-Kopf/-Position in SData und Sage-SQL, Definition 'offen'). Etappe 1 und 2 bauen auf genau diesen Namen."
  - "2 Belegarten der Versandliste: VVA (AB), VVN (Nachlieferauftrag), VVG (Garantieauftrag) - VV2 (VorKonsi) ausschliessen?"
  - "3 Was ist eine offene AB-Position: nicht erledigt UND geliefert < bestellt (Teillieferungen bleiben mit Restmenge sichtbar)?"
  - "4 Darf eine AB-Position mit Teilmengen auf mehrere Ladungen verteilt werden?"
  - "5 Duerfen Positionen verschiedener Kunden in einer Ladung stehen - nur mit Kennzeichen Sammelgut?"
  - "6 Lieferdatum-Arten: Fix / bis / KW / offen (Excel 'x') - vollstaendig?"
  - "7 Status der Ladung: geplant -> vergeben -> verladen; Zuruecksetzen erlaubt; Loeschen nur im Status geplant?"
  - "8 Verkehrskuerzel: wer pflegt die Liste, welche Startwerte (Hebe, Avis, Termin ...)?"
  - "9 Frachtkosten: netto, nur EUR, keine Fremdwaehrung?"
  - "10 Wer sieht und pflegt FK-Kunde, FK-Spedition und Ergebnis?"
  - "11 Zwei neue Rollen versand (lesen) und versand_schreiben (disponieren) - oder Zugriff ueber eine bestehende Rolle?"
  - "12 Kunden-Bestellnummer: aus Sage vorbelegen (Feld nach P-V4) oder nur im WMS eingeben?"
  - "13 FA-Bezug (Etappe 6): in diesem Epic fuer IDEAL bauen (ueber FA-Infos ABNr/Pos, P-V5); AKE erst nach P-V6 bzw. DPS-Mapping?"
  - "14 Sage-Rueckschreibung (Etappe 8): soll 'verladen' ueberhaupt nach Sage, und wohin (Lieferschein ueber StandardBelegService oder Benutzerfeld)?"
  - "15 Lieferadresse in der Ladung: Snapshot aus Sage, im WMS aenderbar (Excel 'Entladeort') - oder immer Sage?"
epic: true
etappen:
  - "1 Lesende Versandliste: offene AB-Positionen aus Sage (SQL-Reader, Cache, Lauf, Liste, Rollen) - SQL 117, 118"
  - "2 SData-Reader + Quellen-Weiche Sync:Quelle:Verkauf (AKE) - keine SQL"
  - "3 Kreditor-Kennzeichen Spedition - SQL 119"
  - "4 Dispo: Ladungen mit AB-Positionen, Kosten, Verkehrskuerzel, Status - SQL 120"
  - "5 Auswertung je Spedition/Kunde/Zeitraum + Druck Ladeliste - keine SQL"
  - "6 FA-Bezug IDEAL (FA-Infos ABNr/Pos) - haengt an P-V5; AKE blockiert bis P-V6/DPS"
  - "7 Abschluss: Hilfe, Changelog, Version 1.85.0, TS-117, README"
  - "8 Sage-Rueckschreibung hinter Schalter (Default aus) - BLOCKIERT bis Rueckfrage 14 + DPS-Freigabe; SQL 121 reserviert"
deploy:
  web: true
  service: true
  migration: true
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
---

> [!important] Verbindlicher Rahmen (Gerald, 2026-11-07 16:17/16:19, Buendel-Auftrag 2026-11-08)
> - Dispo-Maske **je WMS-Installation** (AKE `akenet01`, IDEAL `idealweb01`, je eigene DB): gleicher Code, gleiche Maske; jede Installation
>   liest **ihre** Sage und speichert **ihre** Dispo-Daten. Keine Zentraldispo.
> - **Kein** Knopf „Beide Haeuser“, keine standortuebergreifende Ansicht, keine API zwischen den beiden WMS (nur vorgemerkt, siehe Out-of-Scope).
> - Version **1.85.0**, SQL-Nummern **117–121**, Testszenario **TS-117**.
> - Schreibende Sage-Aufrufe nur als spaetere Etappe hinter Schalter (Default aus, Warnhinweis Aussenwirkung).

## Ziel / Nutzen (das Warum)

Der Versand disponiert heute in einer Excel-Liste (Anhang-Beschreibung in der Backlog-Notiz: Laden, Zustellung, Kunde, Land, Entladeort,
AB-Nummer, Bestellnummer, Infos, Spedition, Preis Kunde, Preis Spedition, Ergebnis). Kunde, Lieferadresse, AB-Nummer und Liefertermin
stehen bereits in Sage und werden abgetippt. Das Epic liefert in **beiden** Haeusern dieselbe Maske: zuerst eine **lesende Liste der offenen
Kundenauftrags-Positionen** aus Sage (was muss raus), dann die **Dispo** (Ladung mit Ladedatum, Lieferdatum, Spedition, Frachtkosten,
Ergebnis) und eine **Auswertung** je Spedition/Kunde/Zeitraum.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Lese-Abgleich offene VK-Positionen (AB) aus der Sage des eigenen Hauses in eine Cache-Tabelle; Liste mit Filter, Spaltenfilter, CSV.
- SData-Lesepfad fuer AKE hinter der Quellen-Weiche (ADR 0019); IDEAL bleibt auf SQL (kein SData-Endpunkt, Backlog Z. 97).
- Kreditor-Kennzeichen „Spedition“ (wie „Beschichter“).
- Dispo-Ladungen (Kopf + 1..n AB-Positionen), Verkehrskuerzel, Kosten, Status, Auswertung, Druck der Ladeliste.
- FA-Bezug als Zusatzinformation, soweit belegbar (IDEAL).
- Vorbereitete, gesperrte Etappe fuer Sage-Rueckschreibung.

**Out-of-Scope**
- Knopf/Ansicht „Beide Haeuser“, Lesen der anderen Installation, gemeinsame Auswertung (Gerald 16:19: nicht umsetzen, nur vormerken -
  Wiedereinstieg = Backlog-Notiz Z. 90-96).
- **Warenausgangspruefung** (Fragebogen, Fotos, enaio) - gehoert in [[2026-11-08-qualitaetsmasken-fragebogen-foto-enaio-spec]]; hier nur der
  Andockpunkt (siehe unten).
- **Sage-Picklisten / Sage-Kommissionierung** (`PicklistItemsEditService`, `PicklistCreateLieferscheinService`, am AKE-Test leer -
  [[sage-sdata-wms-schnittstellenkatalog]] Z. 42) und jede Kommissionierung im WMS: beruehrt
  [[2026-11-08-kommissionierung-auftraege-kopf-zeilen-spec]] (dessen Kopf-Typ kann spaeter einen Versand-Typ aufnehmen). Hier nicht gebaut.
- Lieferschein-PDF (`ExportPrintBelegeService`), Etiketten, Tourenplanung.
- Lesen der AKE-FA-Benutzerfelder `USER_Ladetermin`/`USER_IstVerladen` (nur RealTimeData-Paket `AKE_InterneFA`, laut DPS nicht freigegeben -
  [[sage-sdata-realtimedata]] Z. 181, [[sage-sdata-commonwawiservicesresources]] Z. 10-12).

## Fachliche Anforderungen

**F1 Offene AB-Positionen (Etappe 1).** Je Position eine Zeile: Lieferdatum (Sage-Liefertermin, KW), Kunde (Konto, Matchcode/Name),
Lieferadresse (abweichende Lieferanschrift `A1*`, sonst Kundenadresse), AB-Nummer + Position, Belegart, Artikel, Bezeichnung, Menge bestellt,
geliefert, offen, Tour, Versand, Lieferbedingung, Liefersperre. Filterkarte: Liefertermin von/bis, KW, Kunde, Tour, Belegart, „nur ohne Ladung“
(ab Etappe 4). Spaltenfilter je Spalte, Pagination, CSV. Kopfzeile zeigt Stand des letzten Abgleichs („Stand: <SyncedAt>“) und eine Warnung,
wenn der letzte Lauf fehlgeschlagen ist oder der Abgleich ausgeschaltet ist.

**F2 Dispo-Ladung (Etappe 4)**, Felder in Geralds Reihenfolge: Ladedatum · Lieferdatum (Art Fix/bis/KW/offen + Datum; KW aus dem Datum) · Kunde ·
Lieferadresse · AB-Nummer(n) · Bestellnummer Kunde · Verkehrskuerzel (Mehrfachauswahl + Freitext) · FK-Kunde · Spedition · FK-Spedition ·
Ergebnis (= FK-Kunde − FK-Spedition, berechnet, nicht gespeichert). Dazu Status, Sammelgut-Kennzeichen, Bemerkung.
- Anlegen aus der Liste F1 (Positionen markieren → „Ladung anlegen“) oder leer; Positionen nachtraeglich zuordnen/entfernen.
- Kopf wird aus der ersten Position vorbelegt (Kunde, Lieferadresse, Liefertermin), danach im WMS aenderbar (Rueckfrage 15).
- Eine Ladung umfasst 1..n AB-Positionen auch verschiedener ABs („431253+431254“, „431330 P.2“, Sammelgut-„Liste“).
- Spedition = Auswahl aus aktiven Kreditoren mit Kennzeichen Spedition (Etappe 3); Server prueft beim Speichern.

**F3 Auswertung (Etappe 5):** Summen FK-Kunde, FK-Spedition, Ergebnis und Anzahl Ladungen je Spedition bzw. je Kunde fuer einen Zeitraum
(Ladedatum), nur Status verladen (Vorschlag, mit Rueckfrage 7 bestaetigt). CSV ueber die Tabelle; Druck der Ladeliste je Ladedatum.

**F4 FA-Bezug (Etappe 6):** Spalte „FA“ (HauptFA) und „FA fertig“ an Position und Ladung, wo eine Zuordnung belegt ist. Nur informativ.

## Technischer Loesungsentwurf

### Belegte Ausgangslage (Datei:Zeile, gemessen im Buendel-Worktree v1.78.0)

| Befund | Beleg |
|---|---|
| Es gibt **keinen** VK-Lesecode im WMS: `KHKVK`, `VKBeleg`, `Verkauf` kommen in `*.cs/*.sql/*.cshtml` nur im Paging-Guard vor | Suche ueber den Worktree; einzige Treffer `SageSdataReadClient.cs:137-138` + `SageSdataPagingTests.cs` |
| `VerkaufPositionenAllInfo` darf nie ohne `where` gelesen werden (Guard); die Liste nutzt `VerkaufPositionenInfo` (972 offene, kein Guard) | `SageSdataReadClient.cs:137-138`, [[sage-sdata-wms-schnittstellenkatalog]] Z. 18 |
| Paging-Leser fuer CommonWawiServicesResources: wirft bei jedem Fehler, `orderBy` Pflicht, `startIndex` 0-basiert | `SageSdataReadClient.cs:18-26, 164-206`; [[fallstricke]] „Sage-SData-Lesen“ |
| Quellen-Weiche View/SData/Vergleich, Registrierung je Bereich | `SageQuelleWeiche.cs:22-118`, `SdataRegistrierung.cs:28-39`, `SageQuelleSchalter.cs:16-24` |
| Lese-Abgleiche laufen je eigenem Gate im SyncWorker | `SyncWorker.cs:408-473` |
| Feldliste von `VerkaufBelegekopfInfo`/`VerkaufPositionenInfo` steht **nur** in der Backlog-Notiz (Recherche 2026-11-07), nicht in `codebase/sage-sdata/*`; der Katalog nennt nur Anzahlen | [[sage-sdata-wms-schnittstellenkatalog]] Z. 42 → **unbelegt bis P-V1** |
| VK-Positions-Benutzerfeld `FABelId` [FA-Nummer] existiert im Vertrag `apiBelegerfassung` (`eptVKBelegPosition.udf`), dazu `MemoVerladung`, `Verpackungsstatus`, `Versand`; Status dort „Schema“, nicht gelesen | [[sage-sdata-api-vertraege]] Z. 61-65 |
| `PpsFaBelegeVkBelegSucheInfo` ist im Katalog als „FA ↔ VK-Beleg“ gefuehrt - nicht geprueft; die Backlog-Notiz prueft nur `PpsFaBelegeInfo` | [[sage-sdata-wms-schnittstellenkatalog]] Z. 29 → **P-V6** |
| FA ↔ AB gibt es im WMS **nur bei IDEAL**: `FaHierarchyOrderInfos` (ABNr, Pos, Kunde, Verladetermin_Vsl, Bemerkung_Uhrzeit), gefuellt nur bei `Sync:HierarchicalFaEnabled` | `FaHierarchyOrderInfo.cs:12-42`, `FaHierarchySql.cs:155-163`, `SyncWorker.cs:337`; View-Typ ABNr = int (`vw_IDEAL-AKE_Kommissionierung_FAInfos.sql:14`), WMS-Spalte nvarchar(200) (`SQL/89_AddFaHierarchy.sql:59-63`) |
| AKE-FAs tragen **keine** AB-Nummer: `ProductionOrder` (Customer, DeliveryDate …) und `ProductionOrderExtraInfo` (Kaeltemittel, Ventil, AusfuehrungEZ, Maschine, SageStatus) | `ProductionOrder.cs:12-110`, `ProductionOrderExtraInfo.cs:15-36` |
| Kreditor-Kennzeichen als Vorbild fuer „Spedition“ | `Creditor.cs:66-68`, `CreditorsController.cs:50/75/227` |
| Snapshot statt FK auf Sage-Cache (Cache wird je Lauf ersetzt) | `Beschichtungsauftrag.cs:5-11` |
| Rollenpaar Lesen/Schreiben als Vorbild | `RoleKeys.cs:23-26`, `CurrentUserService.cs:138/141`, Seed-Muster `SQL/78_AddStockReadRole.sql:12-14` |
| CSV kommt in Server-Mode-Tabellen automatisch | `wwwroot/js/csv-export.js:1-8` |
| AppSetting-Katalog (ADR 0020) mit String-/Multiline-Helfer | `AppSettingDefinitions.cs:5-12, 44-47` |

### Etappe 1 - Lesen und Liste
- **Reader** `ISageVerkaufReader` (Service) mit `SageVerkaufReader` = Direkt-SQL auf die Sage-DB des Hauses (Muster `SageArticleSql.cs`), liefert
  offene Positionen der Belegarten nach Rueckfrage 2 mit Kopf-Feldern (denormalisiert, eine Zeile je Position). Tabellen-/Spaltennamen
  **ausschliesslich** aus P-V2, Filter „offen“ aus P-V3. Kein Spaltenname wird vor P-V2 in Code geschrieben.
- **Lauf** `VerkaufAuftraegeSyncService` (Gate `Sync:VerkaufAuftraegeEnabled`, Lauf-Name „Verkauf-Auftraege“): Full-Refresh in einer
  Transaktion (Loeschen + Einfuegen), `SyncedAt` je Zeile. **0 Zeilen aus der Quelle = Warnung, Cache bleibt unveraendert** (eine Quelle mit
  ~400 offenen Belegen ist nie leer - [[fallstricke]] Z. 1720-1724). Zu lange Werte: Zeile mit Warnung uebernehmen, Feld leer statt gekuerzt
  ([[fallstricke]] Z. 1731-1733). Counts-Keys deutsch: „Positionen“, „Belege“, „Warnungen“.
- **Cache** `SageVerkaufPositionen` (kein AuditableEntity, reine Cache-Tabelle wie `FaHierarchyOrderInfos`): Schluessel `SageBelPosId`
  (UNIQUE), `SageBelId`, Belegart, AB-Nummer (Text, formatiert), Belegjahr, Position, Kundenkonto, Kundenname/-matchcode, Lieferadresse
  (Name1, Name2, Strasse, PLZ, Ort, Land), Liefertermin Kopf, Liefertermin Position, Lieferwoche, Tour, Versand, Lieferbedingung,
  Liefersperre, Artikelnummer, Bezeichnung, Menge bestellt, Menge geliefert, `SyncedAt`. Laengen nach P-V2.
- **Web** `VersandController.OffenePositionen` - Listen-View-Pattern (ADR 0005, Referenz `ProductionOrdersController.Index`):
  `PageSize.Resolve` + `PaginationState` + `_Pagination`, Filterkarte, Server-Spaltenfilter (`data-server-column-filter="true"`, alle `<th>`
  mit `data-col-key`, `ColumnFilterHelper.ReadFromQuery`), Spaltenauswahl ueber `ColumnDefinitions`. Datumsfilter in SQL (gespeicherte
  Werte, nichts berechnet). Menue „Versand“.
- **Rollen** `versand` (Klasse, lesen) und `versand_schreiben` (Actions, schreiben; schliesst lesen ein) - vorbehaltlich Rueckfrage 11.

### Etappe 2 - SData fuer AKE
`SdataVerkaufReader` ueber `ISageSdataReadClient.QueryAllAsync` (Vertrag `CommonWawiServicesResources`, `VerkaufBelegekopfInfo` +
`VerkaufPositionenInfo`, Feldnamen aus P-V1, `orderBy` gesetzt, `select` nur mit belegten Feldern - [[fallstricke]] Z. 1711).
`VerkaufQuelleSwitch : SageQuelleWeiche` unter `ISageVerkaufReader`, Bereich `Verkauf` in `SageQuelleSchalter`, Registrierung
`AddVerkaufQuelle` + DI-Test. IDEAL: `SageQuelleSperre` sperrt `SData` (kein Endpunkt) → wirkt wie View mit Warnung.

### Etappe 3 - Spedition
`Creditor.IsSpedition` (bool, NOT NULL, DEFAULT 0 nur in SQL/Migration - [[fallstricke]] Z. 1717), Formular-Checkbox, Filter „nur
Speditionen“, Spalte, Details. Vom Kreditoren-Abgleich nie ueberschrieben (wie `IsCoater`, `ServiceSettingDefinitions.cs:192`).

### Etappe 4 - Dispo
- `VersandLadung : AuditableEntity`: Ladedatum (date?), LieferterminArt (enum Fix|Bis|KW|Offen), Liefertermin (date?), Kundenkonto,
  Kundenname, Lieferadresse (Name, Strasse, PLZ, Ort, Land), IstSammelgut, KundenBestellnummer, Verkehrskuerzel (Text, Kuerzel mit `;`
  getrennt, nur Werte aus `VersandVerkehrskuerzel`), VerkehrsInfo (Freitext), SpeditionCreditorId (FK Creditors, Restrict), FkKunde und
  FkSpedition (decimal(18,2)?), Status (enum Geplant|Vergeben|Verladen), VerladenAt/VerladenByUserId, Bemerkung, `RowVersion`.
- `VersandLadungPosition : AuditableEntity`: VersandLadungId (FK, Cascade), Herkunftsschluessel `SageBelId`/`SageBelPosId` + Snapshot
  (AB-Nummer, Position, Artikel, Bezeichnung) - **kein FK** auf den Cache (Full-Refresh ersetzt die Zeilen, Muster `Beschichtungsauftrag.cs:8-9`),
  Menge (decimal(18,4)). Restmenge in F1 = offen − Summe Menge in nicht verladenen Ladungen (Rueckfrage 4). Verschwindet eine Position aus dem
  Cache (in Sage geliefert/erledigt), bleibt der Snapshot; die Ladung zeigt „nicht mehr offen in Sage“ (Melden statt still behandeln).
- Liste `VersandController.Index` (Ladungen) nach ADR 0005, Spalten in Geralds Reihenfolge; Kosten-Spalten nur mit Kostenrecht (Rueckfrage 10).
- Gleichzeitiges Bearbeiten: `RowVersion` + `DbUpdateConcurrencyException` → `WarningMessage` (Muster Beschichtungsauftrag).

### Etappe 6 - FA-Bezug
IDEAL: Zuordnung AB-Position → `FaHierarchyOrderInfos` ueber (ABNr, Pos) nach dem in P-V5 belegten Format (ABNr in der View `int`, in Sage
evtl. formatiert mit Praefix/Jahr - nicht annehmen). Mehrere Treffer (Kombigeraet) = alle anzeigen, nicht einen waehlen (Muster
`ProductionOrdersController.cs:314-329`). „FA fertig“ aus der HauptFA-Zeile in `ProductionOrders`. AKE: kein Weg belegt → Etappe fuer AKE
blockiert bis P-V6 bzw. DPS-Mapping `USER_FABelId`.

### Etappe 8 - Sage-Rueckschreibung (vorbereitet, blockiert)
Nur nach Rueckfrage 14 und DPS-Freigabe: Outbox (SQL 121) + Worker nach Muster `SageBookingWorker` (v1.28.0), Ziel je Entscheidung
(`StandardBelegService` 4000 Lieferschein aus Auftrag bzw. `UpdateBelegpositionEntityService` Benutzerfeld - [[sage-sdata-wms-schnittstellenkatalog]] Z. 41-42).
Schalter `Sync:VersandSageRueckmeldungEnabled` Default aus. Die /goal-Runde baut diese Etappe **nicht**, sondern traegt sie als Blocker ein.

### Andockpunkte (nicht doppelt bauen)
- **Warenausgangspruefung** ([[2026-11-08-qualitaetsmasken-fragebogen-foto-enaio-spec]]): Schluessel fuer die Pruefung ist die `VersandLadung.Id`
  (und je Position `SageBelPosId`). Das QM-Epic liest diese Schluessel; dieses Epic baut keine Pruef-Spalte und keinen Platzhalter. Falls das
  QM-Epic eine Status-Abfrage anbietet, wird die Spalte „geprueft“ dort als Folgeschritt ergaenzt.
- **Kommissionierung** ([[2026-11-08-kommissionierung-auftraege-kopf-zeilen-spec]]): AB-Position an FAs und Picklisten gehoeren dorthin; der
  Cache `SageVerkaufPositionen` ist wiederverwendbar, falls das Epic VK-Positionen braucht - nicht zweimal aus Sage lesen.
- **AB-Nummer/Position an FAs** ([[2026-10-06-kommissionierliste-ab-nummer-position-spec]]): nutzt dieselbe Quelle `FaHierarchyOrderInfos`.
- **Rueckmelde-Epic** ([[2026-11-02-sage-pps-fa-bde-abgleich-spec]]): FA-Status/Fertigmeldung; Etappe 6 liest nur den WMS-Stand.

### Pruefabfragen (lesend; Ergebnis in die Spec eintragen)
**P-V1 (SData, AKE-Test, nur GET)** - Feldnamen bestaetigen:
```
GET {SData:BaseUrl}/sdata/ol/CommonWawiServicesResources/{SData:Dataset}/$schema
    → Felder von VerkaufBelegekopfInfo und VerkaufPositionenInfo notieren (Name, Typ, Laenge)
GET …/VerkaufBelegekopfInfo?format=json&orderBy=BelID&count=3
GET …/VerkaufPositionenInfo?format=json&orderBy=BelID&count=3
```
Zu bestaetigen (Namen aus der Recherche, bis dahin unbelegt): Schluessel Kopf/Position (BelID? BelPosID?), `Belegkennzeichen`,
`BelegnummerFormatiert`, `A0Empfaenger`, `A0Matchcode`, `A0Name1`, `A1Name1/2`, `A1Strasse`, `A1PLZ`, `A1Ort`, `A1Land`, `BelegLiefertermin`,
`Tour`, `Versand`, `Lieferbedingung`, `Liefersperre`, `Teillieferungen`; Position: Positionsnummer, Artikelnummer, Bezeichnung,
`PositionGGBestellt`, `PositionGGGeliefert`, `Liefertermin`, `Lieferwoche`. Liefert `orderBy=BelID` HTTP 500, nennt `$diagnoses` den echten Namen.

**P-V2 (SQL, Sage-DB AKE und Sage-DB IDEAL)** - Tabellen und Spalten:
```sql
SELECT name FROM sys.objects WHERE name LIKE 'KHKVK%' AND type IN ('U','V') ORDER BY name;
SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME IN ('KHKVKBelege', 'KHKVKBelegePositionen')   -- Namen erwartet, nicht belegt; ggf. aus der ersten Abfrage ersetzen
ORDER BY TABLE_NAME, ORDINAL_POSITION;
```
**P-V3 (SQL, Sage-DB AKE-Test, nach P-V2)** - „offen“ gegen SData abgleichen (Erwartung laut Recherche: Kopf VVA 395, VVG 17, VVN 4, VV2 1;
972 Positionen):
```sql
SELECT b.Belegkennzeichen, COUNT(DISTINCT b.BelID) AS Belege, COUNT(*) AS Positionen
FROM KHKVKBelege b JOIN KHKVKBelegePositionen p ON p.BelID = b.BelID
WHERE b.Belegkennzeichen IN ('VVA','VVN','VVG','VV2')
  AND /* Kriterium offen aus P-V2, z. B. Erledigt-Kennzeichen = 0 und geliefert < bestellt */ 1 = 1
GROUP BY b.Belegkennzeichen;
```
**P-V4 (SQL, Sage-DB AKE)** - wo steht die Kunden-Bestellnummer? Eine AB aus der Excel-Liste nehmen (z. B. 431253) und mit der
Excel-Spalte „Bestellnummer“ vergleichen:
```sql
SELECT * FROM KHKVKBelege WHERE Belegnummer = 431253;   -- Spalten Referenznummer, Referenzzeichen, USER_* sichten
```
**P-V5 (IDEAL: WMS-DB und Sage-DB IDEAL)** - AB-Format fuer Etappe 6:
```sql
-- WMS-DB IDEAL
SELECT TOP 20 ABNr, Pos, HauptFA FROM dbo.FaHierarchyOrderInfos ORDER BY ABNr DESC;
-- Sage-DB IDEAL: dieselben AB-Nummern in KHKVKBelege suchen (Belegnummer / BelegnummerFormatiert / Belegjahr)
```
**P-V6 (SData, AKE-Test)** - FA ↔ VK ohne DPS-Mapping?
```
GET …/PpsFaBelegeVkBelegSucheInfo?format=json&orderBy=BelID&count=3
```
Liefert die Ressource je FA eine VK-BelID/-BelPosID, ist der AKE-FA-Bezug ohne `USER_FABelId` lesbar.

## Migrations-/SQL-Auswirkungen

Je Schema-Aenderung: Model → `dotnet ef migrations add` → idempotentes `SQL/<Nr>_*.sql` (DDL mit `OBJECT_ID`-/`COL_LENGTH`-Guard in eigenem
Batch, `__EFMigrationsHistory`-Insert in separatem Batch) → `SQL/00_FreshInstall.sql` an **zwei** Stellen (Schema + `MigrationId`). ADR 0004.

| Nr | Etappe | Inhalt | Daten-destruktiv |
|---|---|---|---|
| 117 | 1 | `SageVerkaufPositionen` (Cache, UNIQUE `SageBelPosId`, Index Liefertermin, Kundenkonto, AB-Nummer) | nein |
| 118 | 1 | Rollen-Zeilen `versand`, `versand_schreiben` (Seed `IF NOT EXISTS`, Muster `SQL/78_AddStockReadRole.sql`; keine EF-Migration noetig, falls 78 auch keine hatte - Dev-Lauf prueft) | nein |
| 119 | 3 | `Creditors.IsSpedition bit NOT NULL DEFAULT 0` | nein |
| 120 | 4 | `VersandLadungen`, `VersandLadungPositionen` (FK Cascade auf Ladung, FK Restrict auf Creditors, Index Ladedatum/Status/SageBelPosId) | nein |
| 121 | 8 | reserviert (Outbox Sage-Rueckschreibung), nur bei Freigabe von Etappe 8 | - |

Keine Auswirkung auf `SQL/AgentJobs/*` (keine bestehende Tabelle mit Folge-MERGE betroffen; Dev-Lauf bestaetigt per Suche). Nummern sind fuer
das Buendel reserviert; der Dev-Lauf prueft vor dem Anlegen die naechste freie Nummer.

## Einstellungen

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| `Sync:VerkaufAuftraegeEnabled` (ServiceSettings) | Bool (Schalter) | an/aus | aus | Liest offene Kundenauftrags-Positionen (Belegarten laut Rueckfrage 2) aus der Sage des Hauses in den Versand-Cache; ersetzt den Cache je Lauf vollstaendig. Liest nur. | Dienst: Lauf „Verkauf-Auftraege“; Versand → Offene Positionen | beide | – |
| `Sync:Quelle:Verkauf` (ServiceSettings, Etappe 2) | Auswahl | `View` (Sage-SQL), `SData`, `Vergleich` | `View` | Quelle des Laufs „Verkauf-Auftraege“ (ADR 0019); `Vergleich` liest beide, schreibt aus SQL, Abweichungen im Lauf „Sage-Quellenvergleich“. IDEAL: `SData` gesperrt (kein Endpunkt) → wirkt wie View + Warnung. AKE: gesperrt bis P-V1 erledigt. | Dienst: Lauf „Verkauf-Auftraege“ | AKE frei nach P-V1; IDEAL gesperrt | bestehender Hinweis `WarnSData` (`ServiceSettingDefinitions.cs:78-83`) |
| `VersandVerkehrskuerzel` (AppSettings, Etappe 4) | Text mehrzeilig, je Zeile ein Kuerzel | 1-20 Zeichen je Zeile, keine Doppelten (Server prueft) | `Hebe`, `Avis`, `Termin` (vorbehaltlich Rueckfrage 8) | Auswahlliste der Verkehrskuerzel in der Ladung. Entfernte Kuerzel bleiben an bestehenden Ladungen sichtbar, sind aber nicht mehr waehlbar. | Versand → Ladung bearbeiten | beide | – |
| `Sync:VersandSageRueckmeldungEnabled` (ServiceSettings, Etappe 8, nur bei Freigabe) | Bool | an/aus | aus | Sendet verladene Ladungen an Sage (Ziel laut Rueckfrage 14). | Dienst: Versand-Rueckmeldung | AKE (IDEAL erst mit SData) | „Schreibt in Sage (Lieferschein/Benutzerfeld). Nur nach Test am AKE-Test einschalten.“ |

Je Key Anzeigename, Gruppe, Wirkung, Verwendung, Default, Standort, Warnhinweis im Katalog (`ServiceSettingDefinitions.All` bzw.
`AppSettingDefinitions.All`) - Drift-Guard-Tests. Keine Feature-Gate-AppSetting: der Zugang laeuft ueber die Rollen (ohne Rollenvergabe ist das
Menue unsichtbar); ein Gate waere ohne zweiten Zweck.

## Audit-Feld-Auswirkungen

- `VersandLadung`, `VersandLadungPosition`: erben `AuditableEntity`; jedes Update setzt `ModifiedAt`, `ModifiedBy`, `ModifiedByWindows` aus
  `ICurrentUserService`; Statuswechsel „verladen“ zusaetzlich `VerladenAt`/`VerladenByUserId`.
- `Creditor.IsSpedition`: bestehende Audit-Pflege des Kreditoren-Edits (`CreditorsController.cs:227` Umfeld) - unveraendert mitlaufen.
- `SageVerkaufPositionen`: **kein** AuditableEntity (Cache, Full-Refresh), nur `SyncedAt` - Praezedenz `FaHierarchyOrderInfo.cs:10`.
- Rollen-Seed: `CreatedAt/CreatedBy/CreatedByWindows` wie `SQL/78_AddStockReadRole.sql:12-14`.

## Akzeptanzkriterien

Etappe 1
1. Bei `Sync:VerkaufAuftraegeEnabled = aus` laeuft kein Lauf „Verkauf-Auftraege“; die Liste zeigt einen Hinweis „Abgleich ausgeschaltet“.
2. Bei `an` enthaelt `SageVerkaufPositionen` nach einem Lauf genau die offenen Positionen der freigegebenen Belegarten; Anzahl = Ergebnis P-V3.
3. Liefert die Quelle 0 Zeilen, bleibt der Cache unveraendert und der Lauf endet mit Warnung.
4. Ein Wert laenger als die Zielspalte kippt den Lauf nicht; das Feld bleibt leer, das Protokoll nennt AB/Position und Feld.
5. Die Liste zeigt je Position Lieferdatum, KW, Kunde, Lieferadresse, AB-Nummer/Pos, Belegart, Artikel, Mengen (bestellt/geliefert/offen),
   Tour, Versand, Liefersperre und den Stand des letzten Laufs.
6. Filterkarte (Termin von/bis, KW, Kunde, Tour, Belegart), Spaltenfilter je Spalte (Server-Mode), Pagination und CSV wirken auf alle Seiten.
7. Ohne Rolle `versand`/`versand_schreiben`/`admin` ist das Menue unsichtbar und `/Versand/OffenePositionen` liefert Zugriff verweigert.

Etappe 2
8. `Sync:Quelle:Verkauf = Vergleich` am AKE-Test schreibt aus SQL und protokolliert Abweichungen im Lauf „Sage-Quellenvergleich“.
9. `SData` an IDEAL wirkt wie `View` mit Warnung im Protokoll und Hinweis in `/ServiceSettings`.
10. Ungueltiger Wert fuer `Sync:Quelle:Verkauf` wird beim Speichern abgelehnt.

Etappe 3
11. Kreditor-Formular hat „Spedition“; Liste filtert „nur Speditionen“; der Kreditoren-Abgleich aendert das Kennzeichen nie.

Etappe 4
12. Aus markierten Positionen entsteht eine Ladung mit vorbelegtem Kunde, Lieferadresse, Liefertermin; Positionen tragen Snapshot + Menge.
13. Spedition ist nur aus aktiven Speditionen waehlbar; ein manipulierter POST mit anderem Kreditor wird abgelehnt.
14. Verkehrskuerzel nur aus `VersandVerkehrskuerzel`; Freitext zusaetzlich moeglich.
15. Ergebnis = FK-Kunde − FK-Spedition wird angezeigt, nicht gespeichert; leere Kosten → Ergebnis leer.
16. Restmenge in der Liste F1 beruecksichtigt Mengen in nicht verladenen Ladungen; „nur ohne Ladung“ blendet voll zugeordnete Positionen aus.
17. Eine Position, die nicht mehr im Cache ist, zeigt in der Ladung „nicht mehr offen in Sage“.
18. Statuswechsel nach Rueckfrage 7; Loeschen nur im erlaubten Status; gleichzeitige Bearbeitung → Warnmeldung, kein stilles Ueberschreiben.
19. Nur `versand_schreiben`/`admin` legt an, bearbeitet, wechselt Status; Kosten sichtbar nach Rueckfrage 10.

Etappe 5
20. Auswertung summiert FK-Kunde, FK-Spedition, Ergebnis und Anzahl je Spedition bzw. Kunde fuer den Zeitraum; Summen stimmen mit der Ladungsliste ueberein.
21. Druck der Ladeliste je Ladedatum zeigt die Spalten in Geralds Reihenfolge.

Etappe 6
22. IDEAL: Position mit passender ABNr/Pos in `FaHierarchyOrderInfos` zeigt HauptFA und „fertig“; Kombigeraet zeigt alle HauptFA.
23. AKE: Spalte FA ist ausgeblendet bzw. leer mit Hinweis „FA-Bezug fuer AKE noch nicht verfuegbar“.

Alle Etappen
24. Keine Ansicht und kein Aufruf liest Daten der anderen Installation; es gibt keinen Knopf „Beide Haeuser“.
25. Kein Code dieses Epics (ausser Etappe 8 nach Freigabe) ruft einen schreibenden Sage-Service auf.

## Test-Szenarien

Neues Kapitel **TS-117 „Versand-Dispo“** in `docs/TESTSZENARIEN.md` + `secondbrain/tests/testszenarien-index.md`. Ablauf je Haus (AKE-DEV und
IDEAL-DEV):
- **TS-117.1 Abgleich:** Schalter aus → kein Lauf, Hinweis. Schalter an → Lauf im Aktivitaets-Protokoll mit deutschen Counts; Anzahl gegen P-V3.
  Negativ: Sage nicht erreichbar → Fehlerlauf, Cache unveraendert, Liste zeigt alten Stand mit Warnung.
- **TS-117.2 Liste offene Positionen:** Filter, KW, Spaltenfilter, Pagination, CSV (Spalten = sichtbare Spalten). Negativ: User ohne Rolle.
- **TS-117.3 Quelle (nur AKE):** View/Vergleich/SData; IDEAL: SData → Warnung. Ungueltiger Wert abgelehnt.
- **TS-117.4 Spedition:** Kennzeichen setzen, Filter, Abgleich laeuft → Kennzeichen bleibt.
- **TS-117.5 Ladung:** aus zwei ABs anlegen, Sammelgut, Teilmenge, Verkehrskuerzel, Kosten/Ergebnis, Status bis verladen, Position verschwindet
  aus Sage. Negativ: Spedition ohne Kennzeichen per POST, Doppelbearbeitung, Lesrolle versucht Speichern.
- **TS-117.6 Auswertung + Druck.**
- **TS-117.7 FA-Bezug (IDEAL):** AB mit bekanntem HauptFA, Kombigeraet.

## Etappen (nur bei epic: true)

Ein Worktree, ein Merge am Ende; je Etappe ein Commit mit gruenem Build/Test. Branch waehrend der Arbeit mit
`scripts/sync-worktree.ps1 -Slug <slug>` auf main-Stand halten. QA + Schranke 2 erst nach allen Etappen.

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| 1 | Lesende Versandliste: SQL-Reader, Cache (SQL 117), Lauf + Gate, Liste nach ADR 0005, Rollen (SQL 118). **Haengt an P-V2/P-V3** (Spaltennamen, „offen“) und Rueckfragen 2, 3, 11 | offen | |
| 2 | SData-Reader + Weiche `Sync:Quelle:Verkauf` (AKE), DI-Test. **Haengt an P-V1**; ohne P-V1 nur Weiche + Sperre bauen, Reader als Blocker | offen | |
| 3 | Kreditor-Kennzeichen Spedition (SQL 119) | offen | |
| 4 | Dispo-Ladungen (SQL 120), Verkehrskuerzel-Setting, Kosten, Status, Restmenge. **Haengt an Rueckfragen 4-10, 12, 15** | offen | |
| 5 | Auswertung + Druck Ladeliste | offen | |
| 6 | FA-Bezug IDEAL. **Haengt an P-V5 + Rueckfrage 13**; AKE-Teil Blocker bis P-V6/DPS | offen | |
| 7 | Abschluss: Hilfe, Anwender-Changelog, Version 1.85.0 (beide `AppVersion.cs`), README AppSettings, TS-117, Brain-Karten | offen | |
| 8 | Sage-Rueckschreibung hinter Schalter (SQL 121). **BLOCKIERT** bis Rueckfrage 14 + DPS-Freigabe - im /goal-Lauf nicht bauen, als Blocker festhalten | blockiert | |

## Deploy

*Vorlaeufig - der QA-Lauf bestaetigt am echten Diff.* Reihenfolge je Haus: DB-Skripte 117-120 (vor Web/Service), Service stoppen, Service
publish, Web publish, Service starten. Nach dem Deploy `Sync:VerkaufAuftraegeEnabled` bewusst einschalten (Default aus); Rollen vergeben.
Testsystem AKE-DEV und IDEAL-DEV (standort: beide).

- **Web-App:** ja (Versand-Controller/Views, Kreditoren, Rollen, Settings-Katalog)
- **Service:** ja (Lauf „Verkauf-Auftraege“, Reader, Weiche)
- **Migration:** ja (117, 119, 120; 118 Seed)
- **Publish-Befehle:**
  - `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  - `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`

## Offene Rueckfragen

1. **Pruefabfragen P-V1 bis P-V3** ausfuehren und Ergebnis eintragen (Spaltennamen VK-Kopf/-Position in SData und Sage-SQL, Kriterium „offen“).
   Etappe 1 und 2 bauen auf genau diesen Namen. *Empfehlung:* vor Schranke 1 erledigen; P-V2 auf beiden Sage-DBs.
2. **Belegarten:** VVA, VVN, VVG - VV2 (VorKonsi) ausschliessen? *Empfehlung:* VVA/VVN/VVG, VV2 nicht (Konsignation ist kein Versand an den Kunden).
3. **„Offene Position“:** nicht erledigt und geliefert < bestellt, Teillieferungen mit Restmenge? *Empfehlung:* ja; gegen SData-Zahl 972 pruefen (P-V3).
4. **Teilmengen:** eine AB-Position auf mehrere Ladungen? *Empfehlung:* ja, mit Menge je Zuordnung und Restmenge in der Liste (Sage kennt `Teillieferungen`).
5. **Mehrere Kunden je Ladung:** *Empfehlung:* erlaubt; ohne Kennzeichen Sammelgut gelbe Warnung beim Speichern, keine Sperre.
6. **Lieferdatum-Arten:** Fix / bis / KW / offen (Excel „x“)? *Empfehlung:* genau diese vier.
7. **Status:** geplant → vergeben → verladen, Zuruecksetzen mit Schreibrecht, Loeschen nur „geplant“? *Empfehlung:* ja; Auswertung nur „verladen“.
8. **Verkehrskuerzel:** Pflege und Startwerte? *Empfehlung:* AppSetting-Liste (Admin pflegt), Start Hebe, Avis, Termin; plus Freitext je Ladung.
9. **Kosten:** netto, nur EUR? *Empfehlung:* ja, decimal(18,2), keine Waehrungsspalte.
10. **Kostenrecht:** wer sieht/pflegt FK-Kunde, FK-Spedition, Ergebnis? *Empfehlung:* nur `versand_schreiben` + admin; Leserolle ohne Kosten-Spalten.
11. **Rollen:** zwei neue Rollen `versand` / `versand_schreiben`? *Empfehlung:* ja (Muster Beschichtungsauftrag); keine bestehende Rolle passt fachlich.
12. **Kunden-Bestellnummer:** aus Sage vorbelegen (Feld nach P-V4) oder nur Eingabe? *Empfehlung:* Eingabe in der Ladung jetzt; Vorbelegung, falls P-V4 ein
    Feld belegt (dann in Etappe 1 in den Cache aufnehmen).
13. **FA-Bezug:** IDEAL in diesem Epic, AKE spaeter? *Empfehlung:* IDEAL ja, wenn P-V5 passt; AKE erst nach P-V6 bzw. DPS-Mapping `USER_FABelId`.
14. **Sage-Rueckschreibung:** soll „verladen“ nach Sage, wohin? *Empfehlung:* nicht in diesem Epic; Etappe 8 bleibt blockiert bis Entscheidung + DPS-Freigabe.
15. **Lieferadresse:** Snapshot aus Sage, im WMS aenderbar? *Empfehlung:* ja (Excel „Entladeort“), mit Hinweis „weicht von Sage ab“.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. Pruefabfragen P-V1 bis P-V3 (Ergebnis) →
2. Belegarten VVA/VVN/VVG ohne VV2 →
3. Definition offene Position →
4. Teilmengen auf mehrere Ladungen →
5. Mehrere Kunden je Ladung / Sammelgut →
6. Lieferdatum-Arten →
7. Status-Ablauf und Loeschen →
8. Verkehrskuerzel Pflege + Startwerte →
9. Kosten netto/EUR →
10. Kostenrecht →
11. Rollen versand / versand_schreiben →
12. Kunden-Bestellnummer →
13. FA-Bezug IDEAL/AKE →
14. Sage-Rueckschreibung →
15. Lieferadresse aenderbar →
