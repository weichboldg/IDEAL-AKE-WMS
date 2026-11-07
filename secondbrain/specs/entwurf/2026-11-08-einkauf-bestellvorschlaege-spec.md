---
type: spec
title: Einkauf – Bestellvorschläge für Nachbestellartikel (Epic)
slug: 2026-11-08-einkauf-bestellvorschlaege-spec
status: Entwurf
created: 2026-11-07
updated: 2026-11-07
source_backlog: "[[2026-11-07-einkauf-bestellvorschlaege]]"
buendel: "[[2026-11-08-buendel-uebersicht]]"
depends_on: "[[2026-11-08-wareneingang-aus-sage-bestellungen-spec]]"
task: ""
worktree: ""
branch: ""
version: "1.87.0"
sql_block: "128–132 (reserviert; genutzt 128, 129, 130; 131–132 Reserve)"
testszenario: "TS-119"
standort: AKE
affected_code:
  - "IdealAkeWms/Models/RoleKeys.cs (neu: Einkauf = \"einkauf\")"
  - "IdealAkeWms/Services/ICurrentUserService.cs + CurrentUserService.cs (neu: CanAccessEinkaufAsync, Muster :138)"
  - "IdealAkeWms/Filters/RequireEinkaufAccessAttribute.cs (neu, Rolle) + RequireEinkaufAktivAttribute.cs (neu, AppSetting-Gate, Default aus)"
  - "IdealAkeWms/Models/AppSettingKeys.cs + AppSettingDefinitions.cs (5 neue Keys, Gruppe „Einkauf“)"
  - "IdealAkeWms/Controllers/PurchaseProposalsController.cs (neu) + Views/PurchaseProposals/Index.cshtml (neu)"
  - "IdealAkeWms/Services/PurchaseProposalCalculator.cs (neu, reine Rechenregel an EINEM Ort)"
  - "IdealAkeWms/Data/Repositories/IPurchaseProposalRepository.cs + PurchaseProposalRepository.cs (neu, Datensammlung)"
  - "IdealAkeWms/Data/Repositories/StockMovementRepository.cs (nur gelesen: GetCurrentStockAsync :16-193)"
  - "IdealAkeWms/Views/Shared/_Layout.cshtml (neuer Hauptmenüpunkt „Einkauf“, nach „Bestellungen“ :243-267)"
  - "IdealAkeWms/Views/Users/RoleOverview.cshtml (Rolle einkauf)"
  - "IdealAkeWms/Models/ArticlePurchaseSetting.cs (neu, Satellit, E3) + ApplicationDbContext"
  - "IdealAkeWms/Models/ArticleSageDetail.cs (+ Basismengeneinheit, Lagermengeneinheit, UmrechnungsFaktorLME, E4)"
  - "IDEALAKEWMSService/Services/Sdata/SdataArtikelReader.cs (SelectArtikel :45-49 + Details :265-282 um die drei Felder, E4)"
  - "IdealAkeWms/Models/PurchaseProposalTransfer.cs (neu, Übergabe-Protokoll, E5)"
  - "IdealAkeWms/Services/SageSdata/SageDispoStapelClient.cs (neu, POST $service/DispositionStapel*Service, E5)"
  - "Reader offener Sage-Bestellpositionen aus [[2026-11-08-wareneingang-aus-sage-bestellungen-spec]] (nur verwendet, E2)"
  - "Migrationen + SQL/128_AddArticlePurchaseSettings.sql, SQL/129_AddArticleSageDetailUnits.sql, SQL/130_AddPurchaseProposalTransfers.sql, SQL/00_FreshInstall.sql"
  - "IdealAkeWms/AppVersion.cs + IDEALAKEWMSService/AppVersion.cs (1.87.0), Views/Help/Changelog.cshtml, Views/Help/Index.cshtml, README.md, docs/TESTSZENARIEN.md (TS-119)"
open_questions:
  - "1. Offene Bedarfe: zählen offene Lagerbestellungen (Status Submitted voll, PartiallyDelivered nur Rest mit „wird nachgeliefert“) und offene Bedarfsmeldungen (Status Offen) beide als Bedarf? (Empfehlung: ja, beide; Default der Einstellung EinkaufOffeneBedarfe = beide)"
  - "2. Vorschlagsverfahren-Default: „Feste Bestellmenge“ (Vielfaches der Sage-Bestellmenge) oder „Auffüllen auf Meldebestand + Bestellmenge“? (Empfehlung: Feste Bestellmenge)"
  - "3. USER_Mindestbestand: was bedeutet das Feld fachlich (Sicherheitsbestand? Altfeld?) – rechnen oder nur anzeigen? (Empfehlung: in diesem Epic nur Anzeigespalte, nicht rechnen)"
  - "4. Bestand: zählen alle WMS-Lagerplätze außer Kommissionierwagen, auch inaktive und nicht buchbare? (Empfehlung: ja, wie Bestandsaggregation und Meldebestand-Regel, fallstricke §7)"
  - "5. Dispo-Stapel: ein fester WMS-Stapel (Kennung aus Einstellung, wiederverwendet) oder ein Stapel je Lieferant? (Empfehlung: ein fester WMS-Stapel – Lieferant steht je Position, Stapel-Löschen wirkt nicht)"
  - "6. Übergabe-Weg: synchron aus der Web-App mit eigenem Protokoll oder über Outbox + neuen Dienst-Worker? (Empfehlung: synchron aus der Web-App, Protokolltabelle, kein neuer Worker)"
  - "7. Direkte Sage-Bestellung (StandardBelegService, Erfassungsart 4100) aus dem WMS: in diesem Epic oder eigene Folge-Spec? (Empfehlung: eigene Folge-Spec; hier nur Dispo-Stapel)"
  - "8. Rolle: eine Rolle „einkauf“ für Liste, Kennzeichen und Übergabe (ohne getrennte Leserolle)? (Empfehlung: ja, eine Rolle; admin per Wildcard)"
  - "9. Rundung: nur auf die Mindestbestellmenge des Lieferanten aufrunden (eine VPE/ein Bestellvielfaches liegt nicht im WMS)? (Empfehlung: ja; VPE erst nach Prüfabfrage P3 als Folgearbeit)"
  - "10. Betrieb AKE: werden vor der Abnahme Sync:Quelle:Artikel = SData und Sync:ArtikelLieferantEnabled = true gesetzt? Ohne beide fehlen Bestellmenge, Lieferant, WBZ und Sage-verfügbar. (Empfehlung: ja, Deploy-Voraussetzung)"
epic: true
etappen:
  - "E1 Grundgerüst: Rolle einkauf, Gate EinkaufAktiv, Hauptmenü „Einkauf“, Rechenregel, Liste Bestellvorschläge nur lesen (ohne offene Bestellungen), CSV – keine Migration"
  - "E2 Offene Sage-Bestellungen einrechnen (Reader aus Teil B) – keine Migration; hängt an Teil B"
  - "E3 Kennzeichen „nicht disponieren“ + „ignorieren bis“ – SQL/128"
  - "E4 Einheiten + Rundung auf Mindestbestellmenge, Einheiten-Wächter – SQL/129; Rundung unverifiziert bis P1/P2"
  - "E5 Übergabe an den Sage-Dispo-Stapel hinter Schalter (Default aus), Menge/Lieferant anpassen, Protokoll – SQL/130; hängt an Rückfragen 5/6 und Prüfschritten P4/P5"
  - "E6 Abschluss: TS-119, Hilfe, README, Changelog, Brain"
deploy:
  web: true
  service: true
  migration: true
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
---

# Einkauf – Bestellvorschläge für Nachbestellartikel (Epic)

Bündel [[2026-11-08-buendel-uebersicht]], Teil C. Hängt ab von [[2026-11-08-wareneingang-aus-sage-bestellungen-spec]] (Teil B, Reader
offener Sage-Bestellungen) und nutzt die Lieferantenanzeige aus [[2026-11-08-artikeldetails-lieferanten-anzeigen-spec]] (Teil A).
Datenbasis: [[2026-11-06-sage-sdata-stammdaten-sync-spec]] (G7 „eigene Folge-Spec, hier nur Datenbasis“, Z. 943/970).

## Ziel / Nutzen (das Warum)

Sage rechnet bei AKE keine Bestellvorschläge; die „Erweiterte Disposition“ ist nicht lizenziert, die Sage-Disposition wird kaum genutzt
(Entscheidung Gerald 2026-11-07 16:26). Der Einkauf erkennt Nachbestellbedarf heute nur über die Meldebestand-Mail und die
Farbcodierung der Bestandsübersicht. Das Epic gibt dem Einkauf eine eigene Arbeitsliste „Einkauf → Bestellvorschläge“: je Artikel mit
Sage-Meldebestand eine Projektion aus WMS-Bestand, offenen Sage-Bestellungen und offenen WMS-Bedarfen, eine vorgeschlagene Menge beim
Hauptlieferanten und – in der letzten fachlichen Etappe, hinter einem Schalter – die Übergabe in den Sage-Dispositionsstapel, aus dem der
Einkauf in Sage die Bestellungen erzeugt. **Die Vorschlagslogik liegt im WMS, Sage bekommt nur das Ergebnis** (verbindlich).

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Neuer Hauptmenüpunkt **„Einkauf“**, erster Unterpunkt **„Bestellvorschläge“** (verbindlich).
- Rechenregel (eine Klasse) und Liste nach Listen-View-Pattern, CSV über den vorhandenen CSV-Knopf.
- Offene Sage-Bestellungen als Zugang (Reader aus Teil B), offene Lagerbestellungen/Bedarfsmeldungen als Abgang (Einstellung).
- WMS-Kennzeichen „nicht disponieren“ und „ignorieren bis“ je Artikel (Satellit).
- Einheiten-Wächter und Aufrunden auf die Mindestbestellmenge.
- Übergabe ausgewählter Zeilen an den Sage-**Dispositionsstapel** (`DispositionStapelService` + `DispositionStapelPositionenService`),
  Schalter Default **aus**, Warnhinweis Außenwirkung, Protokoll je Position.

**Out-of-Scope**
- Direkte Sage-Bestellung (`StandardBelegService`, Erfassungsart 4100) – Rückfrage 7, Empfehlung eigene Folge-Spec.
- Stufe 2 der Notiz: verbrauchsbasierte Meldebestände, Reichweite, Sicherheitsbestand nach ABC/XYZ, FA-Stücklisten-Bedarfe.
- „Auffüllen auf Höchstbestand“: Im belegten Sage-Schema gibt es kein Höchstbestand-Feld (Artikel-Felder und `ArtikelVariantenExtended`-
  Zusatzfelder vollständig gelistet in [[sage-sdata-artikel-felder]] Z. 12-49; dort nur `Meldebestand`, `Bestellmenge`, `Bestellzeitpunkt`,
  `Dispoformel`, `Dispofaktor`). Die Bedeutung von `Dispoformel`/`Dispofaktor` ist ungeprüft und wird nicht verwendet.
- Vorschau-Horizont / Zeitraster (Bestellrhythmusverfahren): offene Bestellungen zählen unabhängig vom Liefertermin (Bestellpunktverfahren).
- Änderungen an Lagerbestellungen, Bedarfsmeldungen und Fehlteilen (Menü „Bestellungen“ bleibt unverändert; Abgrenzung unten).
- Änderung des Meldebestands in Sage (`apiArtikel` PUT, ungeprüft laut [[sage-sdata-wms-schnittstellenkatalog]] Z. 34).
- IDEAL: Die Sage-Zusatzfelder werden dort nicht gelesen (Artikelquelle SData gesperrt bis P-R1, `ArtikelQuelleSwitch.cs:41-49`); das Gate
  bleibt an IDEAL aus.

**Abgrenzung Menü „Bestellungen“:** Bedarfsmeldungen (`PartRequisition`, Meldung aus der Stückliste an den Einkauf per Mail) und
Lagerbestellungen (`WarehouseRequisition`, Produktion bestellt beim Lager; Typ Lager/Glas, `WarehouseRequisitionType.cs:7-11`) bleiben
dort. Dieses Epic **liest** sie nur als offene Bedarfe. Den Lager/Glas-Typ plant das Kommissionierungs-Epic; hier wird nicht nach Typ
gefiltert, weil nur Artikel mit Meldebestand > 0 in die Liste kommen.

## Fachliche Anforderungen

### F1 Nachbestellartikel (verbindlich, Entscheidung 2026-11-07 16:30)
Nachbestellartikel = WMS-Artikel mit **Meldebestand > 0 in Sage** und **ohne** WMS-Kennzeichen „nicht disponieren“ (ab E3).
Beleg Feld: Meldebestand steht im WMS als `Article.ReorderLevel` (`Article.cs:42-43`, Anzeige „Meldebestand“). Geschrieben **nur** vom
Artikel-Abgleich: View-Weg `SageArticleSql.cs:18` (`v.Meldebestand AS ReorderLevel`) → `SageImportService.cs:449/474/516`; SData-Weg
`SdataArtikelReader.cs:255` (`ArtikelVariantenExtended.Meldebestand`, 0 → null). Sage-„0“ wird auf beiden Wegen zu `null`
(`SageImportHelpers.cs:12-14`). Also gilt: `ReorderLevel > 0` ⇔ Sage-Meldebestand > 0.
Einschränkung (belegt): Im WMS stehen an AKE nur Ressourcenummern aus Stücklisten und `IstBestellartikel AND Aktiv`
(`SdataArtikelReader.cs:32-35`, `WhereBestellartikel` :58). Ein Sage-Artikel mit Meldebestand außerhalb dieser Menge erscheint nicht.

> [!warning] Brain-Korrektur
> Backlog und [[feature-map]] Z. 25 nennen `Article.MinimumStock` – dieses Feld gibt es nicht (kein Treffer im Code); das Feld heißt
> `Article.ReorderLevel`.

### F2 Projektion je Artikel
```
verfügbar = WMS-Bestand
          + offen bestellt (Sage, offene EK-Positionen, ab E2)
          + im Dispo-Stapel (WMS-Stapel in Sage, ab E5)
          − offene Bedarfe (gemäß Einstellung EinkaufOffeneBedarfe)
Vorschlag, wenn verfügbar ≤ Meldebestand  (und nicht „nicht disponieren“, nicht „ignoriert bis ≥ heute“)
```
- **WMS-Bestand** (verbindlich „Bestand = WMS-Bestand“): Summe `CurrentQuantity` je Artikel aus `StockMovementRepository.GetCurrentStockAsync`
  (`StockMovementRepository.cs:16-193`), **ohne** Plätze mit `IsPickingTransport` (Feld im Ergebnis, :52/:74). Begründung Wagen-Ausschluss:
  [[fallstricke]] §7 „Kommissionierwagen“. Artikel ohne Bewegung = 0. Lagerplatz-Umfang: Rückfrage 4.
  **Nicht** `StockCheckService` wiederverwenden: dessen SQL zählt eine Umbuchung zwischen zwei regulären Plätzen als Zugang (das erste
  `WHEN sm.MovementType = 2 …` trifft, der Quellabzug wird nie erreicht, `StockCheckService.cs:41-42`) und lässt Artikel ohne Bewegung
  weg (`INNER JOIN`, :58). Befund wird gemeldet, nicht in diesem Epic behoben.
- **Sage verfügbar** (Vergleichsspalte, verbindlich): `ArticleSageDetail.LagerbestandVerfuegbar` (`ArticleSageDetail.cs:57`), Stand =
  letzter Artikel-Abgleich (`ArticleSageDetail.ModifiedAt`, sonst `CreatedAt`). Der Satellit wird **nur** bei `Sync:Quelle:Artikel = SData`
  geschrieben (`SageImportService.cs:534-539`, `ArticleSageDetail.cs:7-9`); Default ist `View` (`ServiceSettingDefinitions.cs:80`). Fehlt die
  Zeile: Zelle „–“ und Banner (Rückfrage 10).
- **Offen bestellt** (E2): offene Menge der offenen Sage-EK-Positionen je Artikel aus dem Reader von Teil B (`EinkaufPositionenInfo`,
  1.167 offene Positionen am AKE-Test, [[sage-sdata-wms-schnittstellenkatalog]] Z. 41). Feldnamen, Mengeneinheit und „offen“-Definition
  übernimmt dieses Epic **unverändert** von Teil B; nicht neu herleiten. Dazu „nächster Liefertermin“.
- **Offene Bedarfe** (Einstellung `EinkaufOffeneBedarfe`, Rückfrage 1):
  - Lagerbestellungen: Kopf `Status = Submitted` → `QuantityRequested`; `Status = PartiallyDelivered` → je Position mit
    `ShortageStatus = WillBeRestocked` der Rest `QuantityRequested − (QuantityPicked ?? 0)`. Belege: `WarehouseRequisition.cs:11`,
    `WarehouseRequisitionItem.cs:20-21/44`, Status-Ableitung `WarehouseRequisitionRepository.cs:240-250`, Rest-Formel wie
    `WarehouseRequisitionRepository.cs:439`. `Draft`, `Closed`, `Cancelled` zählen nicht. Belegt: Abschluss bucht **keinen** Bestand
    (`CloseAsync` :195-238 setzt nur Mengen/Status); daher ist eine offene Lagerbestellung noch nicht im WMS-Bestand abgezogen.
  - Bedarfsmeldungen: `Status = Offen` (`PartRequisitionStatus.cs:5`) → `Quantity` (`PartRequisition.cs:22`). Erfüllt wird sie bei der
    Einbuchung (`StockMovementsController.cs:142-152` → `PartRequisitionRepository.cs:108-120`).
  - Zuordnung über `ArticleNumber` (beide Tabellen haben keinen Artikel-FK: `PartRequisition.cs:11`, `WarehouseRequisitionItem.cs:12`),
    getrimmt, Groß/Klein egal. Weicht `Unit` von `Article.Unit` ab (beide nicht leer), wird die Zeile **nicht** eingerechnet und im
    Sammelbanner gezählt („n Bedarfe mit abweichender Einheit nicht eingerechnet“) – melden statt still behandeln.
- **Im Dispo-Stapel** (E5): `MengeBasis` der Positionen des WMS-Stapels, live gelesen aus `DispositionStapelPositionenInfo`
  (Feldnamen: Prüfschritt P4). Nur gelesen, wenn `EinkaufDispoUebergabeAktiv = true`.

### F3 Vorschlagsmenge (Einstellung `EinkaufVorschlagsverfahren`, Rückfrage 2)
`Fehlmenge = Meldebestand − verfügbar` (≥ 0, da Vorschlag nur bei verfügbar ≤ Meldebestand); `B = ArticleSageDetail.Bestellmenge`
(`ArticleSageDetail.cs:59`, aus `ArtikelVariantenExtended.Bestellmenge`, `SdataArtikelReader.cs:279`).
- **FesteMenge:** `B × max(1, ⌈Fehlmenge / B⌉)` – mindestens eine Bestellmenge, so viele, dass nach Lieferung verfügbar > Meldebestand
  wird (bei Gleichstand genau eine).
- **Auffuellen:** `Meldebestand + B − verfügbar`.
- `B` fehlt oder ≤ 0: Vorschlag = `Fehlmenge`, bei Fehlmenge 0 → Meldebestand; Zeile mit Hinweis „ohne Sage-Bestellmenge“.
- Danach (E4) Aufrunden auf die Mindestbestellmenge des gewählten Lieferanten, nur bei passender Einheit (F5).
- Alle Mengen in Lagermengeneinheit (`Article.Unit` = Sage `Lagermengeneinheit`, `SdataArtikelReader.cs:254`; WMS-Bestand ebenso).

### F4 Lieferant, WBZ, Liefertermin
- **Hauptlieferant:** `ArticleSupplier` mit `IstHauptlieferant = true` (`ArticleSupplier.cs:28-29`) → `Creditor` (Name, `SageKto`,
  `Creditor.cs:15-18/74-77`); Bestellnummer `ArticleSupplier.SageBestellnummer` (:18), Mindestbestellmenge (:21), WBZ (:25, Tage).
  Geschrieben nur vom Lese-Abgleich `ArtikelLieferantSyncService` (Gate `Sync:ArtikelLieferantEnabled`, Default false,
  `ServiceSettingDefinitions.cs:194`; Doku `ArtikelLieferantSyncService.cs:17-23`).
- Fehlt eine `ArticleSupplier`-Zeile, aber `ArticleSageDetail.Hauptlieferant` (Kto-Text, :62) ist gesetzt: Kto anzeigen, Hinweis
  „Kreditor fehlt im WMS“, Zeile nicht übergebbar.
- **Letzter Lieferant** `ArticleSageDetail.LetzterLieferant` (:63), **ABC** (:54) – Anzeige.
- **WBZ** = WBZ des gewählten Lieferanten, sonst `ArticleSageDetail.Wiederbeschaffungszeit` (:65). Einheit Tage, belegt am AKE-Test:
  Bestelltermin 13.11. + WBZ 14 → Sage-Liefertermin 27.11. ([[sdata-befund-ake-test-2026-10-30]] §14 B1/B2).
- **Wunschtermin** = heute + WBZ (Kalendertage), nur Anzeige; Sage setzt den Liefertermin der Stapelposition selbst (B2).
- Lieferant änderbar auf einen anderen `ArticleSupplier` desselben Artikels (E5).

### F5 Einheiten-Wächter (E4)
- Mindestbestellmenge rundet nur, wenn `ArticleSupplier.Einkaufsmengeneinheit` (:22) leer oder gleich `Article.Unit` ist **und**
  Prüfabfrage P2 die Einheit der Mindestbestellmenge bestätigt hat. Sonst: keine Rundung, Hinweis in der Zeile. Bis P2 beantwortet ist,
  gilt die Rundung als **unverifiziert** (Code-Kommentar `TODO(P2)`).
- Übergabe an Sage (`MengeBasis`, Basismengeneinheit) nur, wenn `Basismengeneinheit = Lagermengeneinheit` (Felder neu in E4); sonst
  Zeile gesperrt mit Hinweis „Einheit Lager ≠ Basis – in Sage bestellen“. Umrechnung über `UmrechnungsFaktorLME` erst, wenn P1 die
  Richtung belegt (Folgearbeit).

### F6 Kennzeichen je Artikel (E3)
- „Nicht disponieren“ (verbindlich): Artikel erscheint nicht als Vorschlag; im Filter „alle Artikel mit Meldebestand“ sichtbar mit
  Kennzeichen, dort zurücknehmbar.
- „Ignorieren bis <Datum>“: Vorschlag ausgeblendet bis einschließlich Datum; Datum ≥ heute Pflicht (Server prüft).
- Optional Bemerkung (500 Zeichen).

### F7 Übergabe an den Sage-Dispositionsstapel (E5, verbindlich „Übergabe an den Dispo-Stapel“)
Bedeutung belegt: Der Dispositionsstapel ist in Sage das Ergebnis der Disposition, aus dem der Einkauf Bestellungen erzeugt
([[sage-sdata-commonwawiservices]] Z. 99-105; Backlog „Sage 100 Disposition“). Am AKE-Test geprüft (§14): Stapel anlegen (B1, Typ 0
Manuell), Position anlegen (B2, `MengeBasis 200`, Lieferant K31013 fix → `PosHandle 383`; Sage ergänzt Lieferanten-Bestellnummer,
Einzelpreis, Liefertermin), Position löschen (B3) wirkt, **Stapel löschen (B4) wirkt nicht** → Stapel wiederverwenden.
- Nur sichtbar und serverseitig erlaubt bei `EinkaufDispoUebergabeAktiv = true` (Default false) – zusätzlich zu Gate und Rolle.
- Zeilen auswählen, Menge (> 0, Dezimal kulturfest) und Lieferant (aus `ArticleSupplier` des Artikels) anpassen, „An Sage übergeben“ mit
  Bestätigungsdialog (Warntext wie Einstellung).
- Server prüft je Zeile neu: Rolle, Gate, Schalter, Menge > 0, Lieferant gehört zum Artikel und hat `SageKto`, Einheiten-Wächter (F5),
  keine Protokollzeile im Status `Gesendet` für diesen Artikel, Artikel nicht schon im WMS-Stapel (Lesen P4). Abgewiesene Zeilen →
  Sammelmeldung mit Grund je Artikel (`TempData["WarningMessage"]`), übergebene → `SuccessMessage` mit Anzahl.
- Ablauf je Zeile: Stapel sicherstellen (`DispositionStapelService`, nur wenn in `DispositionStapelInfo` nicht vorhanden; Typ Manuell = 0,
  `DispoMode` Wawi = 0) → Protokollzeile **vor** dem Aufruf auf `Gesendet` → `DispositionStapelPositionenService` (`OperationMode` Neu = 0,
  `Artikelnummer`, `AuspraegungHandle = 0` wie `SageLagerbuchungPayloadBuilder.cs:57`, `MengeBasis`, `MengeBasisFix`, `Lieferant = SageKto`,
  `IsLieferantFix = true`) → `Bestaetigt` mit `PosHandle` oder `Fehler` mit `ResultText`/`ErrorText`. Je Service `ResultNumber`
  auswerten (Befund §14 Folgerung 3: Fehlercodes uneinheitlich).
- Timeout/unklare Antwort: Status bleibt `Gesendet`, Zeile zeigt „unklar – im Sage-Stapel prüfen“; kein automatischer Neuversuch. Die
  nächste Liste liest den Stapel live (F2) – ist die Position dort, setzt der Server die Protokollzeile beim nächsten Laden auf `Bestaetigt`.
- Zugangsdaten und Ziel wie die SData-Leseansicht: `SData:BaseUrl`, `SData:Application`, `SData:Dataset` (ServiceSettings),
  `SageLagerbuchung:Username/Password` (Konfiguration), TLS-Policy `SageTlsPolicy` – Muster `SageSdataFaController.cs:48-53`.
  URL über `SdataUrlBuilder.BuildResourceUrl(…, "$service/DispositionStapelPositionenService")` (`SdataUrlBuilder.cs:26-39`, Vertrag
  `CommonWawiServices`). POST/Fehlerbehandlung nach `SageLagerbuchungClient.cs:33-75` (Body ins Log gekappt, nie Zugangsdaten).

### F8 Liste „Einkauf → Bestellvorschläge“
- Spalten: Lieferant (Name + Kto), Artikel, Bezeichnung, Bestand WMS, Sage verfügbar, offen bestellt (E2), nächster Liefertermin (E2),
  im Dispo-Stapel (E5), offene Bedarfe, verfügbar, Meldebestand, Bestellmenge, Vorschlag, Einheit, Mindestbestellmenge (E4),
  Bestellnummer Lieferant, WBZ, Wunschtermin, letzter Lieferant, ABC, USER_Mindestbestand (Anzeige, Rückfrage 3), Status/Hinweis.
- Standardsortierung Lieferant, dann Artikelnummer („je Lieferant gruppiert“ ohne eigene Gruppenzeilen – CSV und Spaltenfilter
  bleiben einfach).
- Filterkarte: Lieferant (Auswahl aus Kreditoren mit Artikel-Lieferant-Zeilen), Artikelgruppe, Umfang „nur Vorschläge“ (Default) / „alle
  Artikel mit Meldebestand“, Dringlichkeit „kritisch (verfügbar < 0)“ / „nachbestellen (≤ Meldebestand)“ / alle.
- Dringlichkeit als Text-Badge („kritisch“, „nachbestellen“), Farbe nur zusätzlich (WCAG AA, Farbe nie alleiniger Bedeutungsträger).
- Banner: Sage-Zusatzfelder fehlen (Anzahl Artikel ohne `ArticleSageDetail`), Artikel-Lieferanten fehlen, Bedarfe mit abweichender
  Einheit, offene Bestellungen nicht lesbar (E2, Sage-Fehler → Liste ohne diese Spalte, Banner mit Fehlertext, kein stilles 0).

## Technischer Lösungsentwurf

- **Rechenregel an einem Ort:** `PurchaseProposalCalculator` (statisch, rein, keine DB) nimmt je Artikel ein Eingabe-Record (Bestand,
  offen bestellt, im Stapel, Bedarfe, Meldebestand, Bestellmenge, Mindestbestellmenge, Einheiten, Kennzeichen, Verfahren, heute) und
  liefert Projektion, Vorschlag, Dringlichkeit, Hinweise. Unit-Tests decken F2/F3/F5/F6 ab.
- **Datensammlung:** `PurchaseProposalRepository` lädt in wenigen Abfragen: Artikel mit `ReorderLevel > 0` + `SageDetail`
  (`Article.cs:58`) + Satellit (E3); Lieferanten (`ArticleSuppliers` + `Creditors`); offene Bedarfe (zwei gruppierte Abfragen); Bestand über
  `IStockMovementRepository.GetCurrentStockAsync()` und Gruppierung in C# (Wagen raus). Bei der Lieferanten-Abfrage die Methode aus Teil A
  wiederverwenden, falls Teil A eine je-Artikel-Liste liefert; sonst eine eigene Abfrage hier (keine zweite Kopie anlegen, wenn A sie hat).
- **Listen-View-Pattern (ADR 0005):** Die Liste ist berechnet → Spaltenfilter **in C# nach der Berechnung** über
  `ColumnFilterHelper.ReadFromQuery` + `ColumnFilterSyntax.Matches` (`ColumnFilterSyntax.cs:345`), dann `PageSize.Resolve` +
  `PaginationState` + `_Pagination`. `data-server-column-filter="true"`, `data-col-key` auf jedem `<th>`, viewKey in
  `ColumnDefinitions.GetByViewKey` registrieren ([[fallstricke]] §3). CSV kommt über `csv-export.js` ohne Zusatzcode
  (`csv-export.js:1-8`, `pageSize=0`, Kappung 5000 mit Warnung). Referenz `ProductionOrdersController.Index`.
- **Controller** `PurchaseProposalsController` (MVC): Class-Level `[RequireEinkaufAccess]` + `[RequireEinkaufAktiv]` (kumulativ, ADR 0006/0011);
  `Index` (GET), `SetFlags` (POST, E3), `Transfer` (POST, E5) mit `[ValidateAntiForgeryToken]`; Redirect + `TempData["SuccessMessage"|"WarningMessage"]`.
- **Gate-Attribut** nach Invert-Default-Muster (nur exakt `"true"` aktiviert, MVC → Redirect Home + WarningMessage), wie
  `RequireFaHierarchyKommissionierlistenAktiv` ([[controller]] Z. 78).
- **Menü** in `_Layout.cshtml` als eigenes Dropdown „Einkauf“ nach „Bestellungen“ (:243-267), sichtbar bei `EinkaufAktiv && CanAccessEinkaufAsync`.
- **Sage-Schreibclient (E5):** `SageDispoStapelClient` im Web-Projekt (`Services/SageSdata/`), typisierter `HttpClient` wie
  `ISageSdataReadClient` (`Program.cs:158-163`, Zertifikats-Callback). Kein neuer Dienst-Worker (Rückfrage 6). Lesen der Stapel-Infos über
  `ISageSdataReadClient.QueryAllAsync` (`SageSdataReadClient.cs:164`, Vertrag `CommonWawiServicesResources` :127).
- **E4 Dienst:** `SdataArtikelReader.SelectArtikel` um `Basismengeneinheit,UmrechnungsFaktorLME` erweitern (Feldnamen laut Schema
  [[sage-sdata-artikel-felder]] Z. 19; `Lagermengeneinheit` wird schon gelesen, :46, aber nicht im Satelliten gespeichert) und in
  `Details` (:265-282) mappen. Achtung [[fallstricke]] „Sage-SData-Lesen“: falscher Feldname im `select` = HTTP 500 → Feldnamen vor dem
  Commit mit P1 am AKE-Test bestätigen.
- **Abhängigkeit Teil B (E2):** Der Reader offener Bestellpositionen kommt aus Teil B. Ist er beim Start von E2 nicht im Branch, wird E2
  als Blocker in der Etappen-Tabelle vermerkt und E3/E4 laufen weiter.

## Migrations-/SQL-Auswirkungen

| Nr. | Etappe | Inhalt | Guard |
|---|---|---|---|
| — | E1, E2 | keine | – |
| **128** | E3 | `SQL/128_AddArticlePurchaseSettings.sql`: Tabelle `ArticlePurchaseSettings` (`Id`, `ArticleId` UNIQUE FK → `Articles` ON DELETE CASCADE, `DoNotReorder` bit NOT NULL DEFAULT 0, `IgnoreUntil` date NULL, `Remark` nvarchar(500) NULL, Audit-Spalten) | `IF OBJECT_ID(N'dbo.ArticlePurchaseSettings', N'U') IS NULL`, Index-Guard über `sys.indexes` |
| **129** | E4 | `SQL/129_AddArticleSageDetailUnits.sql`: `ArticleSageDetails` + `Basismengeneinheit` nvarchar(5) NULL, `Lagermengeneinheit` nvarchar(5) NULL, `UmrechnungsFaktorLME` decimal(18,6) NULL (Längen/Typen laut `$schema` P-S1 vor dem Commit prüfen) | `IF COL_LENGTH(N'dbo.ArticleSageDetails', N'Basismengeneinheit') IS NULL` je Spalte |
| **130** | E5 | `SQL/130_AddPurchaseProposalTransfers.sql`: Tabelle `PurchaseProposalTransfers` (`Id`, `ArticleId` FK, `CreditorId` FK, `Quantity` decimal(18,3), `Unit` nvarchar(20), `Stapelkennung` nvarchar(n nach P5), `Status` int (Muster `SageBookingQueueStatus`: Offen/Gesendet/Bestaetigt/Fehler), `SagePosHandle` int NULL, `SentAt` datetime2 NULL, `LastError` nvarchar(1000) NULL, `ResponseRaw` nvarchar(4000) NULL, Audit-Spalten); Index (`ArticleId`, `Status`) | `IF OBJECT_ID(N'dbo.PurchaseProposalTransfers', N'U') IS NULL` |
| 131–132 | – | Reserve | – |

Je Nummer: Model → `dotnet ef migrations add` → SQL-Skript mit Guard (DDL eigener Batch) → `__EFMigrationsHistory`-Insert in separatem
Batch → `SQL/00_FreshInstall.sql` an **beiden** Stellen (Schema + `MigrationId`). Status-Spalten ohne EF-`HasDefaultValue` (Sentinel-
Fallstrick, [[fallstricke]] „Sage-SData-Lesen“), DEFAULT nur in SQL/Migration. Nicht daten-destruktiv (nur neue Tabellen/Spalten).
`SQL/AgentJobs/` nicht betroffen (kein Pflichtfeld an `Articles`, kein Folge-MERGE). Die SQL-Nummern prüft der Dev-Lauf gegen den Stand
des Bündels nach ([[2026-11-08-buendel-uebersicht]]).

## Einstellungen

Alle in `AppSettingDefinitions.All` (neue Gruppe „Einkauf“, Drift-Guard `AppSettingDefinitionsTests`), Keys in `AppSettingKeys`. Kein Seed
(fehlende Zeile = Default).

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| `EinkaufAktiv` | Bool (Schalter, nur exakt „true“ aktiviert) | an / aus | aus | Schaltet Hauptmenü „Einkauf“ und alle Seiten unter `/PurchaseProposals` frei (kumulativ zur Rolle `einkauf`). | Menü, `PurchaseProposalsController` | AKE | – |
| `EinkaufVorschlagsverfahren` | Choice (Auswahlliste) | `FesteMenge` = „Feste Bestellmenge (Vielfaches der Sage-Bestellmenge)“; `Auffuellen` = „Auffüllen auf Meldebestand + Bestellmenge“ | `FesteMenge` (Rückfrage 2) | Bestimmt die Vorschlagsmenge (F3). | Bestellvorschläge | AKE | – |
| `EinkaufOffeneBedarfe` | MultiChoice (Auswahl, leer erlaubt) | `Lagerbestellungen`, `Bedarfsmeldungen` | beide (Rückfrage 1) | Welche offenen WMS-Bedarfe die verfügbare Menge mindern; leer = keine (wirkt anders als „nicht gesetzt“ = beide). | Bestellvorschläge | AKE | – |
| `EinkaufDispoUebergabeAktiv` | Bool (Schalter, nur exakt „true“) | an / aus | aus | Zeigt „An Sage übergeben“ und erlaubt den POST; liest den WMS-Dispo-Stapel live für die Spalte „im Dispo-Stapel“. Wirkt nur mit `EinkaufAktiv`. | Bestellvorschläge (E5) | AKE | „Schreibt Bestellvorschläge in den Sage-Dispositionsstapel (AKE-Sage). Positionen lassen sich nur in Sage entfernen; das Löschen eines Stapels über die Schnittstelle wirkt nicht.“ |
| `EinkaufDispoStapelkennung` | String (frei, Server prüft Länge 1…n nach P5 und Zeichen `[A-Za-z0-9_-]`) | Sage-Stapelkennung | `WMS` | Kennung des einen WMS-Stapels in Sage (Rückfrage 5); wird angelegt, wenn nicht vorhanden, und wiederverwendet. | Bestellvorschläge (E5) | AKE | „Eine neue Kennung legt beim nächsten Übergeben einen weiteren Stapel in Sage an; der alte bleibt bestehen.“ |

Unverändert gelesen (keine Änderung): ServiceSettings `Sync:Quelle:Artikel`, `Sync:ArtikelLieferantEnabled`, `SData:*`, `SageTlsPolicy`.

## Audit-Feld-Auswirkungen

- `ArticlePurchaseSetting` und `PurchaseProposalTransfer` erben `AuditableEntity` (ADR 0003). Anlage: `CreatedAt/By/ByWindows`; jedes Update
  (Kennzeichen ändern, Status `Gesendet` → `Bestaetigt`/`Fehler`, Nachziehen nach Stapel-Lesen) setzt `ModifiedAt`, `ModifiedBy`,
  `ModifiedByWindows` aus `ICurrentUserService`.
- `ArticleSageDetail` (E4): neue Felder schreibt nur der Dienst wie bisher (`ArticleSageDetailsWriter`, Schreiber-Name unverändert).
- Keine Änderung an `Articles`, `PartRequisitions`, `WarehouseRequisition*` (nur gelesen).

## Rollen und Zugriff

- **Neue Rolle `einkauf`** (Rückfrage 8): `RoleKeys.Einkauf`, `CurrentUserService.CanAccessEinkaufAsync()` = admin oder einkauf
  (Muster `CurrentUserService.cs:138`). **Drei Pflichtstellen:** Attribut `RequireEinkaufAccessAttribute`, [[controller]] (Filter-Tabelle +
  Zugriffsmatrix), `Views/Users/RoleOverview.cshtml`. Kein Seed: der Admin legt die Rolle mit Key `einkauf` unter `/Roles` an
  (Deploy-Schritt, Muster `beschichtungsauftrag_schreiben`, [[controller]] Z. 122).
- Gate `[RequireEinkaufAktiv]` kumulativ (Class-Level). Übergabe zusätzlich serverseitig an `EinkaufDispoUebergabeAktiv` gebunden.
- Wächter-Test: jede POST-Action hat Antiforgery und beide Filter (Muster `BeschichtungsauftraegeControllerTests.JedeSchreibendeAction_…`).

## Akzeptanzkriterien

1. Ohne `EinkaufAktiv = true` ist kein Menü „Einkauf“ sichtbar und `/PurchaseProposals` leitet auf Home mit Warnung um – auch für admin.
2. Mit Gate an sieht nur admin oder Rolle `einkauf` den Menüpunkt „Einkauf → Bestellvorschläge“; andere erhalten beim Direktaufruf den
   Zugriffsfehler des Rollenfilters.
3. Die Liste zeigt im Umfang „nur Vorschläge“ genau die Artikel mit `ReorderLevel > 0`, deren `verfügbar ≤ Meldebestand` ist (F2), ohne
   „nicht disponieren“ und ohne aktives „ignorieren bis“.
4. „Bestand WMS“ entspricht der Summe der Bestandsübersicht des Artikels **ohne** Kommissionierwagen-Plätze; ein Artikel ohne Bewegung
   zeigt 0. Eine Umbuchung zwischen zwei regulären Plätzen ändert den Wert nicht.
5. Offene Lagerbestellungen (Submitted; PartiallyDelivered nur Rest „wird nachgeliefert“) und offene Bedarfsmeldungen mindern
   „verfügbar“ gemäß `EinkaufOffeneBedarfe`; Draft/Closed/Cancelled bzw. Erfüllt/Storniert nicht. Abweichende Einheiten werden nicht
   gerechnet und im Banner gezählt.
6. Vorschlagsmenge: Verfahren `FesteMenge` liefert ein Vielfaches der Sage-Bestellmenge (min. 1×), `Auffuellen` liefert
   `Meldebestand + Bestellmenge − verfügbar`; ohne Bestellmenge Fehlmenge mit Hinweis.
7. „Sage verfügbar“ zeigt `LagerbestandVerfuegbar` aus dem Satelliten mit Stand-Datum; fehlt der Satellit, „–“ und Banner mit Anzahl.
8. Lieferant = Hauptlieferant aus `ArticleSuppliers`; fehlt der Kreditor, erscheint der Kto aus `ArticleSageDetail.Hauptlieferant` mit
   Hinweis, die Zeile ist nicht übergebbar.
9. (E2) „offen bestellt“ und „nächster Liefertermin“ kommen aus dem Teil-B-Reader; ein Sage-Lesefehler zeigt einen Banner mit Fehlertext,
   die Spalte bleibt leer statt 0, die Projektion ist als „ohne offene Bestellungen“ gekennzeichnet.
10. (E3) „Nicht disponieren“ und „ignorieren bis“ lassen sich setzen und zurücknehmen; ein Datum vor heute wird abgewiesen; Audit-Felder
    sind gesetzt.
11. (E4) Rundung auf Mindestbestellmenge greift nur bei gleicher Einheit; sonst Hinweis, keine Rundung. Artikel mit
    Basismengeneinheit ≠ Lagermengeneinheit sind für die Übergabe gesperrt (Hinweis sichtbar).
12. (E5) Mit `EinkaufDispoUebergabeAktiv = false` gibt es keinen Übergabe-Knopf und der POST wird serverseitig abgewiesen.
13. (E5) Mit Schalter an legt „An Sage übergeben“ je ausgewählter Zeile eine Position im Stapel `EinkaufDispoStapelkennung` an (Lieferant
    fix, Menge wie angepasst); das Protokoll zeigt `Bestaetigt` mit Sage-PosHandle, in Sage ist die Position mit Bestellnummer/Preis/
    Liefertermin sichtbar.
14. (E5) Ein Artikel, der schon im WMS-Stapel steht oder eine Protokollzeile `Gesendet` hat, wird abgewiesen (Sammelmeldung mit Grund);
    danach zählt „im Dispo-Stapel“ in die Projektion, der Artikel wird nicht erneut vorgeschlagen, solange die Menge reicht.
15. (E5) Sage-Fehler (HTTP oder `ResultNumber` ≠ Erfolg) ergibt Protokollstatus `Fehler` mit Text; keine Zugangsdaten in Log/Meldung.
16. Spaltenfilter (Server-Mode), Pagination und CSV funktionieren; CSV enthält nur sichtbare Spalten in Bildschirmreihenfolge.
17. Dringlichkeit ist als Text erkennbar (nicht nur Farbe); Kontrast WCAG AA.
18. Build und alle Tests grün; neue Unit-Tests für `PurchaseProposalCalculator` (F2/F3/F5/F6), Drift-Guard für die 5 Keys, Wächter-Test für
    Filter/Antiforgery.

## Test-Szenarien

Neues Kapitel **TS-119 „Einkauf – Bestellvorschläge“** in `docs/TESTSZENARIEN.md`, Index [[testszenarien-index]] nachziehen. Umgebung AKE-DEV
mit `Sync:Quelle:Artikel = SData`, `Sync:ArtikelLieferantEnabled = true`, Sage AKE-Test (sagetest01, `ake_TEST2026;1`). Referenzartikel
`00722001` (Sage: Bestand 200, Meldebestand 85, Hauptlieferant K31013, WBZ 14 – [[sage-sdata-wms-schnittstellenkatalog]] Z. 34).

- **TS-119.1 Gate/Rolle:** Gate aus → kein Menü, Direktaufruf Redirect. Gate an, Benutzer ohne Rolle → kein Menü, Direktaufruf verweigert.
  Rolle `einkauf` → Menü „Einkauf → Bestellvorschläge“.
- **TS-119.2 Projektion:** Testartikel mit Meldebestand > 0; WMS-Bestand über Einbuchung knapp über Meldebestand → kein Vorschlag; Ausbuchung
  unter Meldebestand → Vorschlag, Dringlichkeit „nachbestellen“; Ausbuchung ins Minus (falls erlaubt) → „kritisch“.
- **TS-119.3 Wagen/Umbuchung:** Umbuchung regulär → regulär ändert „Bestand WMS“ nicht; Umbuchung auf Kommissionierwagen mindert ihn.
- **TS-119.4 Bedarfe:** Lagerbestellung absenden (Submitted) → „offene Bedarfe“ steigt; mit Fehlteil „wird nachgeliefert“ abschließen → nur Rest
  zählt; Bedarfsmeldung offen → zählt, nach Einbuchung mit Erfüllung → zählt nicht. Einstellung leer → keine Bedarfe. Negativ: Bedarf mit
  anderer Einheit → nicht gerechnet, Banner.
- **TS-119.5 Verfahren:** gleicher Artikel unter `FesteMenge` und `Auffuellen` – Mengen nachrechnen; Artikel ohne Sage-Bestellmenge → Hinweis.
- **TS-119.6 Lieferant/Satellit fehlt:** Artikel ohne `ArticleSageDetail` → „–“ und Banner; Kreditor fehlt → Kto mit Hinweis, nicht übergebbar.
- **TS-119.7 (E2) Offene Bestellungen:** Artikel mit offener Sage-Bestellung → „offen bestellt“ und Liefertermin, Vorschlag fällt ggf. weg;
  SData-URL falsch → Banner, Spalte leer.
- **TS-119.8 (E3) Kennzeichen:** „nicht disponieren“ setzen → Vorschlag weg, im Umfang „alle“ sichtbar, zurücknehmen; „ignorieren bis“ morgen →
  weg, Datum gestern → abgewiesen; Audit-Spalten per SQL prüfen.
- **TS-119.9 (E4) Einheiten/Rundung:** Lieferant mit Mindestbestellmenge > Vorschlag → aufgerundet (nur gleiche Einheit); Artikel mit
  Basis ≠ Lager → Übergabe gesperrt.
- **TS-119.10 (E5) Übergabe:** Schalter aus → kein Knopf, POST per Werkzeug abgewiesen. Schalter an: 00722001 mit K31013 übergeben → Protokoll
  `Bestaetigt`, in Sage Stapel `WMS` mit Position, Bestellnummer/Preis/Termin ergänzt; erneut übergeben → abgewiesen „bereits im Dispo-Stapel“;
  Spalte „im Dispo-Stapel“ gefüllt. Position in Sage löschen → Spalte leert sich. Negativ: falsches Passwort → `Fehler` ohne Zugangsdaten im Text.
  **Nach dem Test die Position in Sage löschen** (Stapel bleibt, B4).
- **TS-119.11 Liste:** Spaltenfilter (Text, Zahl), Pagination, CSV (nur sichtbare Spalten), Sortierung Lieferant/Artikel, Kontrast/Text-Badge.

## Etappen (nur bei epic: true)

Ein Worktree (Bündel-Worktree laut [[2026-11-08-buendel-uebersicht]]), je Etappe ein Commit, ein Merge am Ende. Branch während der Arbeit
mit `scripts/sync-worktree.ps1 -Slug <slug>` auf main-Stand halten. Jede Etappe baut und testet grün für sich; eine offene Rückfrage oder
Prüfabfrage blockiert nur ihre Etappe, die übrigen laufen weiter (Blocker in der Tabelle vermerken).

| # | Etappe | Abhängigkeit | Status | Commit |
|---|--------|--------------|--------|--------|
| 1 | Grundgerüst: Rolle `einkauf` (3 Stellen), Gate `EinkaufAktiv`, Menü „Einkauf“, `PurchaseProposalCalculator` + Tests, Repository, Liste nur lesen (Bestand, Sage verfügbar, Bedarfe, Meldebestand, Bestellmenge, Vorschlag, Lieferant), Einstellungen `EinkaufAktiv`/`EinkaufVorschlagsverfahren`/`EinkaufOffeneBedarfe`, Version 1.87.0 in beiden `AppVersion.cs` – keine Migration | Rückfragen 1, 2, 4, 8 (Defaults wie empfohlen bauen, falls Antwort fehlt: als Blocker vermerken) | offen | |
| 2 | Offene Sage-Bestellungen einrechnen (Reader aus Teil B), Spalten „offen bestellt“, „nächster Liefertermin“, Fehler-Banner – keine Migration | **Teil B** im Branch ([[2026-11-08-wareneingang-aus-sage-bestellungen-spec]]) | offen | |
| 3 | Satellit `ArticlePurchaseSettings` (nicht disponieren, ignorieren bis, Bemerkung), Aktionen + Filter „alle Artikel mit Meldebestand“ – **SQL/128** | – | offen | |
| 4 | Einheiten: `ArticleSageDetails` + Basis-/Lager-ME, `UmrechnungsFaktorLME` (Dienst-Reader), Mindestbestellmenge-Rundung, Einheiten-Wächter – **SQL/129** | Prüfabfrage P1 (Feldnamen vor Commit), P2 (Einheit Mindestbestellmenge; ohne Antwort: Rundung als `TODO(P2)` unverifiziert, Wächter strikt), Rückfrage 9 | offen | |
| 5 | Übergabe an den Sage-Dispo-Stapel: Schalter `EinkaufDispoUebergabeAktiv` (aus) + `EinkaufDispoStapelkennung`, Auswahl/Menge/Lieferant, `SageDispoStapelClient`, Stapel-Lesen, Protokoll `PurchaseProposalTransfers` – **SQL/130** | Rückfragen 5, 6, 7; Prüfschritte P4, P5 (ohne P4/P5: Etappe als Blocker, nicht raten) | offen | |
| 6 | Abschluss: TS-119 + Index, Hilfeseite (konkrete Bedienschritte, Formel, Schalter-Warnung), README (AppSettings), Anwender- + Brain-Changelog v1.87.0, feature-map, controller.md, codebase/services/datenmodell, ggf. fallstricke | – | offen | |

## Prüfabfragen für den Menschen (nur lesend, AKE-Test; P5 ist ein Schreibtest mit Rücksetzen)

Abhängige Aussagen sind bis zur Antwort **unverifiziert**.
- **P1 Einheiten Artikel** (für E4): `GET {Basis}/sdata/ol/CommonWawiServicesResources/ake_TEST2026;1/Artikel?select=Artikelnummer,Basismengeneinheit,Lagermengeneinheit,UmrechnungsFaktorLME&where=IstBestellartikel eq -1 and Aktiv eq -1&orderBy=Artikelnummer&count=500&format=json`
  (Header `Accept: application/json`). Frage: Liefert Sage die drei Felder (kein HTTP 500)? Wie viele Artikel haben Basis ≠ Lager? Für
  einen solchen Artikel: ist `UmrechnungsFaktorLME` „Basis je Lager-ME“ oder umgekehrt (Abgleich mit der Sage-Maske)?
- **P2 Einheit Mindestbestellmenge** (für E4): `GET …/ArtikelLieferantInfo?select=Lieferant,Artikelnummer,Mindestbestellmenge,Einkaufsmengeneinheit,UmrechnungsfaktorEK&where=Artikelnummer eq '00722001'&orderBy=Artikelnummer,Lieferant&format=json`
  und Vergleich mit der Sage-Lieferantenmaske: Ist die Mindestbestellmenge in Einkaufs- oder Lagermengeneinheit?
- **P3 VPE** (Folgearbeit, Rückfrage 9): `GET …/ArtikelLieferantInfo/$schema` – gibt es ein Verpackungseinheiten-/Bestellvielfaches-Feld (in
  RealTimeData heißt es `UmrechnungsfaktorEK/VPEK`, [[sage-sdata-realtimedata]] Z. 124)?
- **P4 Dispo-Stapel lesen** (für E5): `GET …/DispositionStapelPositionenInfo/$schema` und `GET …/DispositionStapelPositionenInfo?where=Stapelkennung eq 'WMSTEST01'&orderBy=…&format=json`
  – Feldnamen für Stapelkennung, Artikelnummer, MengeBasis, Lieferant, Handle; Filter auf Stapelkennung möglich? Zusätzlich in der
  Sage-Oberfläche: Was geschieht mit einer Stapelposition, wenn daraus eine Bestellung erzeugt wird (verschwindet sie aus dem Stapel?).
- **P5 Schreibtest Stapel** (für E5, Freigabe Gerald, Muster §14): Maximale Länge `Stapelkennung` ($schema `DispositionStapelService`);
  zweite Position in einen bestehenden Stapel mit `OperationMode` Neu (ohne erneutes Stapel-Anlegen) – geht das? Wirkung von
  `MengeBasisFix = true` (bleibt die Menge bei einem Sage-Dispo-Lauf erhalten?). Danach Position löschen (B3-Muster).
- **P6** (gehört Teil B): Einheit und Offen-Definition der Menge aus `EinkaufPositionenInfo` – hier nur übernommen.

## Deploy

*Vorläufig – der qa-agent bestätigt gegen den echten Diff.*
- **Web-App:** ja (Controller, Views, Menü, Einstellungen, Sage-Schreibclient, Migrationen 128–130).
- **Service:** ja (E4: `SdataArtikelReader` liest drei weitere Felder).
- **Migration:** ja – `SQL/128`, `SQL/129`, `SQL/130` in Reihenfolge, DB vor Web und Dienst; nicht daten-destruktiv.
- **Voraussetzungen AKE:** Rolle `einkauf` unter `/Roles` anlegen und zuweisen; `Sync:Quelle:Artikel = SData` und
  `Sync:ArtikelLieferantEnabled = true` aktiv und mindestens ein Lauf erfolgreich (Rückfrage 10); Kreditoren-Abgleich gelaufen. Danach
  `EinkaufAktiv = true`. `EinkaufDispoUebergabeAktiv` erst nach TS-119.10 auf dem Testsystem und ausdrücklicher Freigabe einschalten.
- **IDEAL:** Deploy unschädlich, Gate bleibt aus.
- **Publish-Befehle** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`

## Zusatzfunde (gemeldet, nicht Teil dieses Epics)

- `StockCheckService` (Meldebestand-Mail) zählt Umbuchungen zwischen zwei regulären Plätzen als Zugang (`StockCheckService.cs:41-42`, CASE
  bricht vor dem Quellabzug ab; gleiche Logik im `HAVING` :66-67) und übersieht Artikel ohne Bewegung (`INNER JOIN` :58). Folge: zu hoher
  Bestand, Mail bleibt aus. Eigener Bug-Datensatz empfohlen.
- Backlog/[[feature-map]] nennen `Article.MinimumStock`; das Feld heißt `Article.ReorderLevel`.
- Backlog nennt eine VPE in `ArticleSuppliers`; die Tabelle hat keine (`ArticleSupplier.cs:11-29`).

## Offene Rueckfragen

1. **Offene Bedarfe:** Zählen offene Lagerbestellungen (Submitted voll, PartiallyDelivered nur Rest „wird nachgeliefert“) und offene
   Bedarfsmeldungen (Offen) beide als Bedarf? Risiko: Hat der Einkauf für eine Bedarfsmeldung schon bestellt, steht sie zusätzlich in
   „offen bestellt“ und gleicht sich aus; doppelt mindernd wirkt sie nicht. **Empfehlung:** ja, beide; Default der Einstellung = beide.
2. **Vorschlagsverfahren-Default:** „Feste Bestellmenge“ oder „Auffüllen auf Meldebestand + Bestellmenge“? **Empfehlung:** Feste
   Bestellmenge – die Sage-Bestellmenge ist gepflegt (00722001) und entspricht der klassischen Sage-Logik Meldebestand + Bestellmenge.
3. **USER_Mindestbestand:** Fachliche Bedeutung (Sicherheitsbestand? Altfeld?) – rechnen oder nur anzeigen? **Empfehlung:** in diesem Epic nur
   Anzeigespalte; Bedeutung klärt Gerald, Rechnen ggf. in Stufe 2.
4. **Bestand-Umfang:** Alle WMS-Plätze außer Kommissionierwagen, auch inaktive und nicht buchbare? **Empfehlung:** ja, wie Bestandsaggregation
   ([[fallstricke]] §7 „IsActive vs IstBuchbar“) – sonst fehlt vorhandener Bestand und es wird zu viel bestellt.
5. **Dispo-Stapel:** ein fester WMS-Stapel oder je Lieferant? **Empfehlung:** ein fester Stapel (Kennung `WMS` aus Einstellung) – Lieferant
   steht je Position, und weil Stapel-Löschen nicht wirkt (B4), würden sich Stapel je Lieferant ansammeln.
6. **Übergabe-Weg:** synchron aus der Web-App mit Protokolltabelle oder Outbox + neuer Dienst-Worker? **Empfehlung:** synchron – der Einkauf
   braucht das Ergebnis je Zeile sofort, die Mengen sind klein (Dutzende), die Outbox (`SageStammdatenOutboxItem.cs:5-11`) ist für
   Stammdaten gebaut und hat keinen Worker; Status `Gesendet` vor dem Aufruf + Stapel-Lesen ersetzen den automatischen Neuversuch.
7. **Direktbestellung** (`StandardBelegService`, 4100) in diesem Epic? **Empfehlung:** nein, eigene Folge-Spec hinter eigenem Schalter;
   Gerald hat „Sage bekommt nur das Ergebnis (Dispo-Stapel)“ entschieden.
8. **Rolle:** eine Rolle `einkauf` für Liste, Kennzeichen und Übergabe? **Empfehlung:** ja; der Übergabe-Schalter begrenzt die Außenwirkung,
   eine getrennte Leserolle ist derzeit ohne Bedarf.
9. **Rundung:** nur auf die Mindestbestellmenge aufrunden (VPE/Bestellvielfaches liegt nicht im WMS)? **Empfehlung:** ja; VPE nach P3 als
   Folgearbeit.
10. **Betrieb AKE:** Werden vor der Abnahme `Sync:Quelle:Artikel = SData` und `Sync:ArtikelLieferantEnabled = true` gesetzt (Default View
    bzw. false)? Ohne beides fehlen Bestellmenge, Lieferant, WBZ und Sage-verfügbar, die Liste zeigt nur Banner. **Empfehlung:** ja, als
    Deploy-Voraussetzung.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. Offene Bedarfe (Lagerbestellungen + Bedarfsmeldungen) →
2. Vorschlagsverfahren-Default →
3. USER_Mindestbestand →
4. Bestand-Umfang →
5. Dispo-Stapel fest oder je Lieferant →
6. Übergabe-Weg →
7. Direktbestellung →
8. Rolle einkauf →
9. Rundung →
10. Betrieb AKE (SData-Artikel + Artikel-Lieferanten) →
