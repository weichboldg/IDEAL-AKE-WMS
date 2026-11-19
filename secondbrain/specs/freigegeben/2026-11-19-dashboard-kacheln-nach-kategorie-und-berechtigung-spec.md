---
type: spec
title: "Dashboard: Kacheln für alle Funktionen nach Kategorie, Sichtbarkeit aus EINER Quelle mit dem Menü"
slug: 2026-11-19-dashboard-kacheln-nach-kategorie-und-berechtigung-spec
status: Testbereit
created: 2026-11-19
updated: 2026-11-19
source_backlog: "[[2026-11-19-dashboard-kacheln-neue-features-nach-berechtigung]]"
task: ""
jira: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: beide
version_vorschlag: "1.99.47 (1.99.45 Updater-Spec, 1.99.46 Bugfix BF-Warenkorb-Artikelsuche; Dev-Lauf prüft erneut)"
testszenario_vorschlag: "TS-170 (TS-165 BDE mobil, TS-167 Updater, TS-168 Artikelinfo, TS-169 BF-Warenkorb; Dev-Lauf prüft erneut)"
affected_code:
  - "IdealAkeWms/Views/Home/Index.cshtml:1-281 (komplett: heute handgeschriebene Kacheln mit eigenen @if-Ketten, wird Schleife über den Katalog)"
  - "IdealAkeWms/Views/Home/_ArtikelinfoTile.cshtml:1-16 (entfällt, Artikelinfo wird Katalogeintrag)"
  - "IdealAkeWms/Controllers/HomeController.cs:20-41 (ViewBag-Rechte/-Schalter entfallen; :22 DisplayName wird in keiner View gelesen)"
  - "IdealAkeWms/Views/Shared/_Layout.cshtml:49-100 (Rechte-/Schalter-Block), :101-407 (jede Menübedingung → nav.Zeigt(NavZiele.X)), :118, :136, :333, :341, :353-354, :376-377, :395 (Inline-Abfragen)"
  - "NEU IdealAkeWms/Services/Navigation/NavigationKatalog.cs (NavZiel, NavZiele, Kategorien, NavigationSicht mit EINER Regel je Ziel)"
  - "IdealAkeWms/Program.cs:170 (Umfeld: AddScoped<NavigationSicht> neben ICurrentUserService)"
  - "IdealAkeWms/wwwroot/css/site.css:352-372, :880-890 (.dashboard-card: Fokusring, Kategorie-Überschrift)"
  - "IdealAkeWms.Tests/Views/BeschichtungsauftraegeViewTests.cs:112-132 (Wächter prüft heutige Bedingung wortgleich – anpassen)"
  - "IdealAkeWms.Tests/Views/OrderRecipientsMenuTests.cs:29-41 (Wächter prüft heutige Bedingung – wird Regeltest im Katalog)"
  - "NEU IdealAkeWms.Tests/Services/NavigationSichtTests.cs, NavigationFilterKongruenzTests.cs, IdealAkeWms.Tests/Views/NavigationQuelltextTests.cs"
  - "IdealAkeWms/Views/Help/Index.cshtml:471, :1713 (Dashboard-Erwähnungen) + neuer Abschnitt; Views/Help/Changelog.cshtml"
  - "docs/TESTSZENARIEN.md (neu TS-170; Querverweis TS-21.2 Z. 3979 und Artikelinfo Z. 355)"
open_questions:
  - "1. Kategorien und Reihenfolge: Lager · Bestellungen · Kommissionierung · Produktion/BDE · Blechfertigung · Beschichtung · Einkauf/Wareneingang · Stammdaten · Administration – also der Vorschlag aus der Notiz plus „Bestellungen“ (wie das Menü-Dropdown)? Empfehlung: ja."
  - "2. Kachel-Auswahl laut Tabelle „Katalog“ (Spalte Kachel): Protokoll- und Detailpflege-Seiten (Sage-Lagerbuchungen, Sage-Wareneingänge, BDE-Stammdaten, Schichtkalender, Artikelkategorien u. Ä.) nur im Menü? Empfehlung: ja, sonst rund 55 Kacheln für Admins."
  - "3. Artikelinfo: Die Kachel folgt künftig dem Filter am Ziel (masterdata_read). Picker ohne masterdata_read verlieren sie (heute führt sie bei ihnen auf „Zugriff verweigert“). Artikelinfo kommt zusätzlich als Eintrag ins Menü „Stammdaten“. Empfehlung: ja. Wer Pickern die Artikelinfo öffnen will, legt dafür einen eigenen Backlog-Eintrag an (Rechteänderung)."
  - "4. Kacheltitel = Menütitel (z. B. „FA-Liste“ statt „Fertigungsaufträge“, „Bewegungshistorie“ statt „Bewegungen“), damit es nur eine Quelle gibt? Empfehlung: ja."
  - "5. „OSEON AG-Übersicht“ (Reporting) hat heute keinen Modulschalter, nur die Rolle reporting. Auch an IDEAL (ohne OSEON) wird sie jedem mit dieser Rolle angezeigt. Bleibt das so? Empfehlung: ja, unverändert (Menü-Regel). Die Prüfabfrage B zeigt, ob an IDEAL überhaupt jemand die Rolle hat."
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: false
freigabe_entscheidung: "1–4 wie Empfehlung, 5 abweichend: OSEON AG-Übersicht nur bei aktivem OSEON-Modul (20:40); freigegeben 20:38"
freigabe_von: "Gerald Weichbold"
freigabe_am: "2026-11-19"
---

## Ziel / Nutzen (das Warum)

Seit v1.31 sind viele Funktionen dazugekommen (FA-Struktur, Vormontage, Kommissionierlisten, Beschichtung, Blechfertigung, Einkauf,
Wareneingang, Lager-/Glasbestellung …). Das Dashboard kennt davon keine einzige. Außerdem prüft es die Rechte **anders als das Menü
und anders als die Filter am Ziel**: Heute zeigt es Kacheln, die auf „Zugriff verweigert“ führen, und es fehlen Kacheln, die der
Benutzer öffnen dürfte. Ziel: eine Startseite, die jede freigeschaltete Funktion nach Kategorie zeigt. Ihre Sichtbarkeit kommt
aus **derselben Regel wie das Menü**, damit Menü und Dashboard nicht mehr auseinanderlaufen. Dieses Auseinanderlaufen hat zuletzt
den Bugfix „Empfänger“-Menü in v1.99.44 ausgelöst.

### Ist-Stand (belegt, Worktree `2026-08-07-ideal-teile-1-5`, HEAD `f1747e10`, v1.99.44)

**Dashboard** `Views/Home/Index.cshtml` – Rechte kommen per ViewBag aus `HomeController.cs:23-41`:

| Gruppe (Z.) | Kacheln | Bedingung | Abweichung von Filter/Menü |
|---|---|---|---|
| Lager (18-66) | Einbuchung, Ausbuchung, Bestände, Bewegungen | `canPick` | **Zu wenig:** Nutzer mit stock, stock_keyuser oder stock_read sehen nichts. Das Menü zeigt das Lager ab `CanAccessStockRead` (`_Layout.cshtml:101`), der Filter ist `RequireStockAccess`/`RequireStockReadAccess` (`StockMovementsController.cs:59,106`, `StockOverviewController.cs:11`). |
| Fertigung & Kommissionierung (68-104) | Kommissionierung, Fertigungsaufträge, Artikelinfo | `canPick` | Fertigungsaufträge **zu wenig** (Menü/Filter: `CanViewProductionOrdersAsync`, `FaBuchungsText.cs:55-56`, also picking, tracking, leitstand oder vorbau). Artikelinfo **zu viel:** `ArticlesController.cs:10` verlangt `RequireMasterDataReadAccess`, ein Picker ohne masterdata_read landet auf AccessDenied. |
| Artikel (107-113) | Artikelinfo | `!canPick && hasMasterDataReadAccess` | – |
| Leitstand (115-135) | Kommissionierung | wie Menü `_Layout.cshtml:205` | – |
| Teileverfolgung (137-157) | OSEON | wie Menü `:306` | – |
| BDE (159-212) | Terminal, Cockpit, Buchungen | wie Menü `:318-331` | – |
| Reporting (214-234) | OSEON AG-Übersicht | wie Menü `:341` | – |
| Stammdaten (236-257) | Artikel, Lagerplätze | **ohne Bedingung** | **Zu viel:** Der Filter ist `RequireMasterDataReadAccess` (`ArticlesController.cs:10`, `StorageLocationsController.cs:11`). |
| Stammdaten (258-280) | Benutzer, Arbeitsplätze | `hasMasterDataAccess` (Rolle masterdata) | **Zu viel:** Der Filter ist `RequireAdminAccess` (`UsersController.cs:10`, `WorkstationsController.cs:10`). |

**Es fehlen** u. a.: Mehrfach-Einbuchung, Umbuchung, Lagerplatz aus-/umbuchen, FA-Struktur, FA-Vervollständigen,
FA-Abarbeitungsliste, Vormontage, Kommissionierlisten, Beschichtung (Vorschlagsliste, Aufträge), BF-Warenkorb, BF-Aufträge,
Bedarfsmeldungen, Lagerbestellungen, Fehlteile (Werkbank/Lager), Eingehende Listen, Offene Bestellungen, Wareneingang zu Bestellung,
Bestellvorschläge, Einkauf-Bestellungen, Werkbänke, Rollen, Einstellungen, Aktivitäts-Protokoll, Arbeitsgänge → Sage.

**Menü** `Views/Shared/_Layout.cshtml`: Rechte und Schalter werden in `:49-100` gesammelt. Dazu kommen Inline-Abfragen an `:118`
(WareneingangBestellungAktiv), `:136` (CanTransferStock), `:333` (IsAdmin), `:341` (CanReportOperations), `:353-354`,
`:376-377` (BlechfertigungAktiv) und `:395` (ServiceSetting `ProduktionsauftragHierarchisch`). Die Schalter lesen nach zwei
Mustern: „nur exakt "true"“ (z. B. `:57-58,64,68,72-90`) bzw. bei `LagerbestellungAktiv` „nur "false" sperrt“ (`:59-60`, ADR
[[0011-feature-toggles-ueber-appsettings]]).

**Menü gegen Filter** (Abgleich mit [[controller]] und den Filter-Klassen in `Filters/`): Ich habe jeden Menüeintrag geprüft. Keiner
zeigt mehr, als der Filter am Ziel erlaubt. Teils ist das Menü **strenger** als der Controller: Leitstand (`LeitstandAktiv`) und
Teileverfolgung (`TeileverfolgungAktiv`) haben kein Controller-Gate (`PickingLeitstandController.cs:12`,
`TrackingController.cs:12`), Bedarfsmeldungen haben kein `BestellungenAktiv`-Gate (`PartRequisitionsController.cs:10`).
Inline-Gates statt Attribut haben `FaWorklistController.cs:90-92` (FaCompletionAktiv) und `SageFaArbeitsgaengeController` (Master,
[[controller]]). **Einziger Menüfehler:** Das Dropdown „Bestellungen“ kann leer erscheinen. Bedingungen: `BestellungenAktiv`=true,
`LagerbestellungAktiv`=false, Benutzer nur mit Rolle lagerbestellung oder glasbestellung (`:255` wahr, `:262` und `:266` falsch).

**Folgerung:** Das Menü ist die richtige Regelquelle. Das Dashboard übernimmt sie, statt eine eigene zu führen.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Ein Navigationskatalog (C#) als **einzige** Quelle für die Sichtbarkeit jedes Menüziels. `_Layout.cshtml` und `Home/Index.cshtml`
  lesen ausschließlich daraus.
- Dashboard neu: Kacheln nach Kategorie, leere Kategorien ausgeblendet, Hinweis bei gar keiner Kachel.
- Menü: Bedingungen auf den Katalog umstellen. Markup, Reihenfolge, Trennlinien, Badge und die Sonderregel
  „Kommissionierung“ (Dropdown oder einfacher Link) bleiben unverändert. **Neu:** Ein Dropdown erscheint nur, wenn mindestens ein
  Eintrag sichtbar ist (behebt den leeren Fall „Bestellungen“). Neu ist außerdem der Eintrag „Artikelinfo“ im Dropdown
  „Stammdaten“ (Rückfrage 3).
- Automatisierte Tests: Regel-Matrix, Filter-Kongruenz, Quelltext-Wächter.
- Hilfe, Anwender-Changelog, Anwenderdoku, Testszenario, Brain.

**Out-of-Scope**
- Rechte- oder Filteränderungen (keine neue Rolle, kein neuer Filter, kein neues Gate am Controller). Fehlende Controller-Gates
  (Leitstand, Teileverfolgung, Bedarfsmeldungen) bleiben wie sie sind. Hier ist das Menü strenger, das ist sicher. Diese Lücken
  sind ein Zusatzfund für einen eigenen Backlog-Eintrag.
- Zähler oder Badges auf Kacheln (z. B. freigegebene Kommissionieraufträge). Erst einbauen, wenn jemand sie fordert.
- Benutzerdefinierte Startseite oder Favoriten.
- Seiten ohne Menüeintrag (`HierarchieUmstellung`, `SageTest`, `SageSdataFa`, `BdeTeileliste`). Die Teileliste braucht eine
  Werkbank und leitet ohne sie zum Terminal (`BdeTeilelisteController.cs:84-93`), sie ist also kein eigener Einstieg.

## Fachliche Anforderungen

F1 **Eine Quelle.** Für jedes Ziel (Controller/Action) gibt es genau eine Sichtbarkeitsregel. Menü und Dashboard fragen nur diese
Regel ab. Eine Kachel ist sichtbar, wenn ihr Ziel sichtbar ist **und** sie eine Kategorie hat.

F2 **Regel = heutige Menü-Regel.** Ausnahme Artikelinfo, siehe Rückfrage 3. Die Regel prüft immer zwei Dinge: den Modulschalter mit
derselben Lese-Semantik wie heute und das Recht über dieselbe `ICurrentUserService`-Methode, die der Filter am Ziel aufruft
(Filter-Aufrufe belegt: `Filters/*.cs`, z. B. `RequireStockAccessAttribute.cs:23`).

F3 **Nie mehr als der Filter.** Keine sichtbare Kachel und kein sichtbarer Menüeintrag führt auf „Zugriff verweigert“ oder auf die
Weiterleitung „Modul deaktiviert“.

F4 **Kategorien** in fester Reihenfolge (Rückfrage 1). Kacheln stehen in Katalogreihenfolge. Eine Kategorie ohne sichtbare Kachel
wird gar nicht gerendert, auch nicht ihre Überschrift.

F5 **Leerer Zustand:** Hat ein Benutzer keine einzige Kachel, zeigt die Seite einen Hinweis (`alert alert-info`): „Für Ihren
Benutzer sind keine Funktionen freigeschaltet. Bitte wenden Sie sich an den Administrator.“

F6 **Katalog** (Vorschlag, Rückfragen 2 und 4; Regel = Menüzeile heute; Filter = Filter am Ziel):

| Key | Titel | Ziel | Regel (heute `_Layout.cshtml`) | Filter am Ziel | Kachel-Kategorie | Standort-Hinweis |
|---|---|---|---|---|---|---|
| Einbuchung | Einbuchung | StockMovements/Inbound | CanAccessStock (:108) | `RequireStockAccess` StockMovementsController.cs:106 | Lager | beide |
| MehrfachEinbuchung | Mehrfach-Einbuchung | StockMovements/InboundBulk | CanAccessStock (:108) | :183 | Lager | beide |
| Ausbuchung | Ausbuchung | StockMovements/Outbound | CanAccessStock (:108) | :298 | Lager | beide |
| Umbuchung | Umbuchung | StockMovements/Transfer | CanAccessStock (:108) | :389 | Lager | beide |
| Bestaende | Bestände | StockOverview/Index | CanAccessStockRead (:101) | StockOverviewController.cs:11 | Lager | beide |
| Bewegungshistorie | Bewegungshistorie | StockMovements/Index | CanAccessStockRead (:101) | StockMovementsController.cs:59 | Lager | beide |
| Artikelinfo | Artikelinfo | Articles/Info | **neu** HasMasterDataRead (Rückfrage 3) | ArticlesController.cs:10 | Lager | beide |
| LagerplatzAusbuchen | Lagerplatz ausbuchen | StockMovements/OutboundAll | CanTransferStock (:136) | `RequireStockKeyUserAccess` :482 | Lager | beide |
| LagerplatzUmbuchen | Lagerplatz umbuchen | StockMovements/LocationTransfer | CanTransferStock (:136) | :561 | Lager | beide |
| SageLagerbuchungen | Sage-Lagerbuchungen | SageBookingQueue/Index | CanAccessStockRead (:131) | SageBookingQueueController.cs:15 | – (nur Menü) | beide |
| Bedarfsmeldungen | Bedarfsmeldungen | PartRequisitions/Index | BestellungenAktiv ∧ (CanPick ∨ CanAccessStock) (:262) | PartRequisitionsController.cs:10 | Bestellungen | beide |
| Lagerbestellungen | Lagerbestellungen | WarehouseRequisitions/Index | LagerbestellungAktiv(≠"false") ∧ (CanPick ∨ CanAccessStock ∨ Lager ∨ Glas) (:255,:266) | WarehouseRequisitionsController.cs:10-11 | Bestellungen | beide |
| MeineFehlteile | Meine Fehlteile | MissingParts/Index | wie Lagerbestellungen (:269) | MissingPartsController.cs:10-11 | Bestellungen | beide |
| LagerEingehendeListen | Lager: Eingehende Listen | WarehousePicking/Index | LagerbestellungAktiv ∧ CanProcessLager (:270-273) | WarehousePickingController.cs:10-11 | Bestellungen | beide |
| LagerFehlteile | Lager: Fehlteile | MissingPartsLager/Index | wie oben (:274) | MissingPartsLagerController.cs:10-11 | Bestellungen | beide |
| Kommissionierung | Kommissionierung | Picking/Index | CanPick (:220,:236) | PickingController.cs:120 | Kommissionierung | beide |
| Kommissionierlisten | Kommissionierlisten | FaHierarchyKommissionierListen/Summiert | FaHierarchyKommissionierlistenAktiv ∧ CanProcessLager (:220,:248) | FaHierarchyKommissionierListenController.cs:33-34 | Kommissionierung | IDEAL (Schalter) |
| LeitstandKommissionierung | Leitstand Kommissionierung | PickingLeitstand/Index | LeitstandAktiv ∧ (CanPick ∨ CanManagePickingRelease) (:205) | PickingLeitstandController.cs:12 | Kommissionierung | beide |
| FaListe | FA-Liste | ProductionOrders/Index | CanViewProductionOrders (:92) | ProductionOrdersController.cs:13 | Produktion/BDE | beide |
| FaStruktur | FA-Struktur | FaHierarchy/Index | CanViewProductionOrders (:97,:147) | FaHierarchyController.cs:24 | Produktion/BDE | beide (kein Schalter, wie Menü) |
| FaVervollstaendigen | FA-Vervollständigen | FaCompletion/Index | FaCompletionAktiv ∧ CanFaCompletion (:98) | FaCompletionController.cs:20 (Gate im Filter, RequireFaCompletionAccessAttribute.cs:28-30) | Produktion/BDE | beide |
| FaAbarbeitungsliste | FA-Abarbeitungsliste | FaWorklist/Index | FaCompletionAktiv ∧ HasVorbau (:99) | FaWorklistController.cs:21 + inline :90-92 | Produktion/BDE | beide |
| Vormontage | Vormontage | FaHierarchyVormontage/Index | FaHierarchyVormontageAktiv ∧ HasVorbau (:171) | FaHierarchyVormontageController.cs:29-30 | Produktion/BDE | IDEAL (Schalter) |
| BdeTerminal | Terminal | BdeTerminal/Index | BdeAktiv ∧ CanUseBde (:318) | BdeTerminalController.cs:14-15 | Produktion/BDE | beide |
| BdeCockpit | Cockpit | BdeCockpit/Index | + CanManageBdeShiftlead (:326) | BdeCockpitController.cs:6-7 | Produktion/BDE | beide |
| BdeBuchungen | Buchungen | BdeBookings/Index | + CanManageBdeShiftlead (:326) | BdeBookingsController.cs:12-13 | Produktion/BDE | beide |
| BdeStammdaten | Stammdaten (BDE) | BdeMasterData/Index | + CanManageBdeShiftlead (:331) | BdeMasterDataController.cs:10-11 | – | beide |
| BdeSchichtkalender | Schichtkalender | BdeShiftCalendar/Index | BdeAktiv ∧ CanUseBde ∧ IsAdmin (:333) | BdeShiftCalendarController.cs:11 | – | beide |
| TeileverfolgungRueckmeldungen | Rückmeldungen | Tracking/Index | TeileverfolgungAktiv ∧ CanViewTracking (:306) | TrackingController.cs:12 | – | AKE (OSEON) |
| TeileverfolgungOseon | OSEON Aufträge | Tracking/OseonIndex | wie oben | TrackingController.cs:12 | Produktion/BDE | AKE (OSEON) |
| OseonAgUebersicht | OSEON AG-Übersicht | OseonReporting/OperationsOverview | CanReportOperations (:341) | OseonReportingController.cs:11 | Produktion/BDE | AKE (Rückfrage 5) |
| BfWarenkorb | BF-Warenkorb | BfAuftraege/Warenkorb | BlechfertigungAktiv ∧ CanBfBestellen (:90-91) | BfAuftraegeController.cs:18-19 | Blechfertigung | IDEAL (Schalter) |
| BfAuftraege | BF-Aufträge | BfAuftraege/Index | wie oben | BfAuftraegeController.cs:18-19 | Blechfertigung | IDEAL (Schalter) |
| BeschichtungVorschlagsliste | Vorschlagsliste | FaHierarchyBeschichtung/Summiert | FaHierarchyBeschichtungAktiv ∧ HasBeschichtungsauftragAccess (:157) | FaHierarchyBeschichtungController.cs:28-29 | Beschichtung | beide (seit v1.99.36) |
| Beschichtungsauftraege | Aufträge | Beschichtungsauftraege/Index | wie oben | BeschichtungsauftraegeController.cs:28-29 | Beschichtung | beide |
| OffeneBestellungenLager | Offene Bestellungen | PurchaseOrders/Index | WareneingangBestellungAktiv ∧ CanAccessStockRead (:118-123) | PurchaseOrdersController.cs:17-18 | Einkauf/Wareneingang | AKE (Spec-Standort) |
| WareneingangZuBestellung | Wareneingang zu Bestellung | PurchaseOrders/Receive | + CanAccessStock (:124-126) | PurchaseOrdersController.Receive.cs:14 | Einkauf/Wareneingang | AKE |
| SageWareneingaengeLager | Sage-Wareneingänge | PurchaseOrders/SageWareneingaenge | WareneingangBestellungAktiv ∧ CanAccessStockRead (:132-134) | Klassenfilter :17-18 | – | AKE |
| Bestellvorschlaege | Bestellvorschläge | PurchaseProposals/Index | EinkaufAktiv ∧ CanAccessEinkauf (:87-88) | PurchaseProposalsController.cs:15-16 | Einkauf/Wareneingang | beide |
| OffeneBestellungenEinkauf | Offene Bestellungen (Einkauf) | EinkaufPurchaseOrders/Index | wie oben | EinkaufPurchaseOrdersController.cs:14-15 | Einkauf/Wareneingang | beide |
| SageWareneingaengeEinkauf | Sage-Wareneingänge (offen) | EinkaufPurchaseOrders/SageWareneingaenge | wie oben | :14-15 | – | beide |
| Artikel | Artikel | Articles/Index | HasMasterDataRead (:356) | ArticlesController.cs:10 | Stammdaten | beide |
| Lagerplaetze | Lagerplätze | StorageLocations/Index | HasMasterDataRead | StorageLocationsController.cs:11 | Stammdaten | beide |
| Werkbaenke | Werkbänke | ProductionWorkplaces/Index | HasMasterDataRead | ProductionWorkplacesController.cs:12 | Stammdaten | beide |
| Artikelkategorien, Artikelmerkmale, FaVorbauAg, Kommissionierbereiche, FaMerkmale, Kreditoren, Debitoren | (Menütitel) | je /Index | HasMasterDataRead (:367-373) | je `RequireMasterDataReadAccess` (Klassenebene) | – | beide |
| Empfaenger | Empfänger | OrderRecipients/Index | HasMasterDataRead ∧ (BestellungenAktiv ∨ LagerbestellungAktiv ∨ BlechfertigungAktiv) (:376-377) | OrderRecipientsController.cs:10 | – | beide |
| ArtikelgruppenZuordnung | Artikelgruppen-Zuordnung | OrderRecipients/ArticleGroupMappings | HasMasterDataRead ∧ BestellungenAktiv (:381) | OrderRecipientsController.cs:10 | – | beide |
| Benutzer | Benutzer | Users/Index | IsAdmin (:386) | UsersController.cs:10 | Administration | beide |
| Arbeitsplaetze | Arbeitsplätze | Workstations/Index | IsAdmin | WorkstationsController.cs:10 | Administration | beide |
| Rollen | Rollen | Roles/Index | IsAdmin | RolesController.cs:10 | Administration | beide |
| Arbeitsgaenge | Arbeitsgänge | OperationConfigs/Index | IsAdmin | OperationConfigsController.cs:19 | Administration | beide |
| ArbeitsgaengeSage | Arbeitsgänge → Sage | SageFaArbeitsgaenge/Index | IsAdmin ∧ Master `ProduktionsauftragHierarchisch` (:395) | SageFaArbeitsgaengeController.cs:18 + inline Gate | Administration | IDEAL (Master) |
| Einstellungen | Einstellungen | Settings/Index | IsAdmin | SettingsController.cs:13 | Administration | beide |
| ServiceEinstellungen | Service-Einstellungen | ServiceSettings/Index | IsAdmin | ServiceSettingsController.cs:19 | – | beide |
| Standorteinstellungen | Standorteinstellungen | StandortEinstellungen/Index | IsAdmin | StandortEinstellungenController.cs:20 | Administration | beide |
| AktivitaetsProtokoll | Aktivitäts-Protokoll | SyncLog/Index | IsAdmin | SyncLogController.cs:11 | Administration | beide |

Die Admin-Einträge liegen im Menü im Dropdown „Stammdaten“. Admin hat immer Stammdaten-Lesen (`CurrentUserService.cs:88-89`),
deshalb ist das unverändert konsistent.

F7 **Standort AKE/IDEAL.** Es gibt keinen Standort-Schalter im Code (`AppSettingKeys.cs`: nur Modulschalter). Der Standort ergibt
sich aus den Schalterwerten der jeweiligen DB und aus dem Master `ProduktionsauftragHierarchisch` (ServiceSettings,
`HierarchischeStrukturKeys.cs:10`). Die Spalte „Standort-Hinweis“ gibt nur die fachliche Herkunft an (Specs:
Wareneingang = AKE, Blechfertigung/BDE-Teileliste/Arbeitsgänge → Sage = IDEAL, Beschichtung/Einkauf = beide). Welche Schalter auf
DEV/Test/Prod tatsächlich gesetzt sind, steht nur in der DB. Deshalb gibt es **Prüfabfrage A**, je DB auszuführen, und das
Ergebnis kommt ins Testprotokoll:

```sql
SELECT [Key], [Value] FROM dbo.AppSettings
WHERE [Key] IN ('TeileverfolgungAktiv','LeitstandAktiv','BestellungenAktiv','LagerbestellungAktiv','BdeAktiv','FaCompletionAktiv',
  'FaHierarchyKommissionierlistenAktiv','FaHierarchyBeschichtungAktiv','FaHierarchyVormontageAktiv','WareneingangBestellungAktiv',
  'EinkaufAktiv','BlechfertigungAktiv')
ORDER BY [Key];
SELECT [Key], [Value] FROM dbo.ServiceSettings WHERE [Key] = 'ProduktionsauftragHierarchisch';
```

**Prüfabfrage B** (Rollenbelegung, für Rückfrage 5 und die Testbenutzer):

```sql
SELECT r.[Key], COUNT(ur.UserId) AS Benutzer
FROM dbo.Roles r LEFT JOIN dbo.UserRoles ur ON ur.RoleId = r.Id
GROUP BY r.[Key] ORDER BY r.[Key];
```

Unverifiziert, bis A/B auf AKE- und IDEAL-DEV gelaufen sind: welche Kategorien dort im Normalbetrieb erscheinen.

## Technischer Loesungsentwurf

Ponytail-Leiter: Die Regeln existieren schon, als Menübedingungen und als `ICurrentUserService`-Methoden, die auch die Filter
aufrufen. Ich ziehe sie an **einen** Ort und lasse Menü und Dashboard dort abfragen. Es gibt keine neue Rechte-Logik, kein
Interface mit nur einer Implementierung, keinen datengetriebenen Menü-Renderer (das Menü-Markup mit Trennern, Badge und
Kommissionierung-Sonderfall bleibt handgeschrieben, wo es heute steht).

1. **`Services/Navigation/NavigationKatalog.cs`** (eine Datei):
   - `sealed record NavZiel(string Key, string Titel, string Controller, string Action, string? Kategorie, string? Beschreibung, string? SymbolPfad)`
     (`Kategorie == null` = nur Menü).
   - `static class NavZiele`: je Ziel ein `static readonly NavZiel`, dazu `Alle` (Katalogreihenfolge = Kachelreihenfolge) und
     `Kategorien` (feste Reihenfolge, Rückfrage 1).
   - `sealed class NavigationSicht(ICurrentUserService, IAppSettingRepository, IServiceSettingRepository)`, **scoped** registriert
     (`Program.cs` neben `:170`). `Task<NavigationErgebnis> GetAsync()` berechnet die Flags einmal je Request und cacht das Ergebnis
     im Feld: Rollen über die bestehenden `Can…/Has…`-Methoden plus `CanViewProductionOrdersAsync` (`FaBuchungsText.cs:55`); die
     Schalter über **einen** privaten Helfer mit den zwei Lese-Semantiken („exakt true“, „nur false sperrt“ für
     `LagerbestellungAktiv`); den Master nur lesen, wenn `IsAdmin` (wie Kommentar `_Layout.cshtml:394`). Die Regel je Ziel steht in
     **einem** `switch` über `NavZiel.Key`.
   - `NavigationErgebnis`: `bool Zeigt(NavZiel)`, `bool ZeigtEines(params NavZiel[])` (für Dropdowns),
     `IReadOnlyList<(string Kategorie, IReadOnlyList<NavZiel> Kacheln)> Kacheln` (nur nicht-leere Kategorien) und `bool LeitstandAktiv`
     (für das Badge `_Layout.cshtml:63,225,241`).
   - Der Aufwand je Request bleibt gleich: Die Rollen sind gecacht (`CurrentUserService.cs:177-194`), die AppSettings laufen über den
     Cache-Decorator (`Program.cs:86`). Layout und Dashboard teilen dieselbe Instanz.
2. **`_Layout.cshtml`**: `@inject IdealAkeWms.Services.Navigation.NavigationSicht Navigation`. Der Block `:49-100` wird zu
   `var nav = await Navigation.GetAsync();`. Jede Bedingung, auch die Inline-Abfragen `:118,:136,:333,:341,:353-354,:376-377,:395`,
   wird `nav.Zeigt(NavZiele.X)` bzw. für Dropdowns `nav.ZeigtEines(...)`. Die Kommissionierung-Sonderregel (`:216-254`) bleibt,
   ihre Bedingungen werden `nav.Zeigt(NavZiele.Kommissionierung)` und `nav.Zeigt(NavZiele.Kommissionierlisten)`. Die
   Scanner-/QR-Einstellungen im Kopf (`:16-33`) bleiben unberührt. Neu ist der Link „Artikelinfo“ im Dropdown „Stammdaten“ nach
   „Artikel“ (Rückfrage 3).
3. **`Home/Index.cshtml`**: `@inject` derselben Klasse, Schleife über `nav.Kacheln`. Je Kategorie ein `<section>` mit Überschrift,
   darin `row g-4` und je Kachel das bestehende Kartenmuster (`col-md-6 col-lg-3`, `card dashboard-card h-100`, Link um die ganze
   Karte). Ist die Liste leer, kommt der Hinweis aus F5. `_ArtikelinfoTile.cshtml` entfällt. `HomeController.Index` wird
   `return View();`, die ViewBag-Zuweisungen `:22-41` entfallen.
4. **Frontend:** Die Umsetzung **muss** den Skill `frontend-design` ausdrücklich aufrufen (CLAUDE.md). Es gelten die Leitplanken
   aus CLAUDE.md: Konsistenz vor Eigenständigkeit, nur Bootstrap 5 und das bestehende `.dashboard-card` (`site.css:352-372`),
   keine Icon-Bibliothek. Symbole bleiben Inline-SVG wie heute (`Index.cshtml:76-79`), mit `aria-hidden="true"`.
   WCAG AA: Titel `--ake-primary` #053153 und Beschreibung `.text-muted` dürfen bleiben. Laut meiner Rechnung, im QA-Lauf zu
   messen, erreichen `--ake-green` #82BF6E (~2,2:1), `--ake-orange` #EF7D00 (~2,8:1) und `--ake-secondary` #43A6E2 (~2,7:1) auf
   Weiß nicht einmal 3:1. Sie sind deshalb nur als Randfarbe oder dekoratives Symbol erlaubt, nie für Text oder ein
   bedeutungstragendes Zeichen. Das heutige „+“/„−“ (`Index.cshtml:27,38`) wird ein dekoratives SVG. Die Bedeutung trägt immer der
   Titeltext. Dazu kommen ein sichtbarer Fokusring (`a:focus-visible .dashboard-card` in `site.css`), Trefferfläche = ganze Karte
   (≥ 44 px), Überschriften ohne Sprung (Seite `h2`, Kategorie `h3` in `h6`-Optik, Kachel `h4` in `h5`-Optik). Maßstab für
   Kontrast und Trefferflächen ist der Nachtrag v1.99.36 ([[2026-11-17-v1-99-36-beschichtung-ake-nachbesserung-bde-kontrast]]).
5. **Tests** (xUnit/FluentAssertions/Moq, Muster `BfAuftraegeFilterTests.cs:23-35`, `CurrentUserServiceIsAdminTests.cs`,
   `Helpers/FakeSession.cs`, `OrderRecipientsMenuTests.cs:21-27`):
   - **T1 `NavigationSichtTests`**: echte `CurrentUserService` (FakeSession + gemocktes `IRoleRepository`), je einzelne Rolle aus
     `RoleKeys` (per Reflexion über die Konstanten) plus „keine Rolle“, kombiniert mit {alle Schalter an, alle aus, je ein Schalter
     aus, `LagerbestellungAktiv` fehlend/"false"}. Der Test prüft: Jede Kachel ist auch im Menü sichtbar (`Kacheln ⊆ Zeigt`), keine
     leere Kategorie, Stichproben-Regeln (Empfänger, Artikelgruppen-Zuordnung – ersetzt `OrderRecipientsMenuTests.cs:29-41` –,
     Beschichtung, Kommissionierlisten, Einkauf, BF, Arbeitsgänge → Sage nur mit Master und Admin). Zusätzlich liefert
     `GetAsync()` zweimal dasselbe Ergebnis und ruft z. B. `CanPickAsync` nur einmal auf.
   - **T2 `NavigationFilterKongruenzTests`** (F3): Je `NavZiel` sucht der Test den Controllertyp
     `IdealAkeWms.Controllers.{Controller}Controller` und die GET-Action (Pflicht: existiert, sonst Tippfehler). Er sammelt alle
     `TypeFilterAttribute` von Klasse und Methode (auch über Partial-Dateien wie `PurchaseOrdersController.*.cs`), instanziiert die
     Filter per `ActivatorUtilities` mit denselben Diensten wie T1 und prüft für dieselbe Matrix: **Ziel sichtbar ⇒ jeder Filter ruft
     `next`**. Inline-Gates (`FaWorklistController.cs:90-92`, `SageFaArbeitsgaengeController`) führt der Test als ausdrückliche Liste
     mit Begründung.
   - **T3 `NavigationQuelltextTests`** (Layout): Im Navbar-Block (`<ul class="navbar-nav me-auto flex-wrap">` bis vor `user-info`)
     kommt kein `CurrentUserService.`, kein `AppSettings.GetValueAsync` und kein `ServiceSettings.GetValueAsync` vor. Jeder Link
     `asp-controller=… asp-action=…` gehört zu genau einem `NavZiel`, und das nächste umschließende `@if` enthält `NavZiele.<Key>`.
     Jedes `NavZiel` steht mindestens einmal als Link im Menü. Daraus folgt: Kacheln ⊆ Menüziele.
   - **T4 Home-Wächter**: `Home/Index.cshtml` enthält kein `ViewBag.` und kein literales `asp-controller=`. `HomeController` hat
     keine `ViewBag`-Zuweisung.
   - **Anpassen:** `BeschichtungsauftraegeViewTests.cs:112-132` (Bedingungstext wird `nav.Zeigt(NavZiele.BeschichtungVorschlagsliste)`
     bzw. `ZeigtEines`), `OrderRecipientsMenuTests.cs` (wird zu T1-Fällen, Datei entfällt oder prüft nur noch den Link).
     `DebitorsViewTests.cs:42`, `CreditorsViewTests.cs:46` und `LayoutQuerscrollenV194Tests.cs:21-24` bleiben gültig, weil das
     Link-Markup unverändert bleibt.

Betroffene Module: [[controller]] (Start/Hilfe), [[module]].

## Migrations-/SQL-Auswirkungen

Keine. Keine Migration, kein `SQL/XX`, kein `00_FreshInstall`, keine AgentJobs. Nächste freie SQL-Nummer bleibt 164 (zuletzt
`SQL/163_AddBfPositionArbeitsschritte.sql`).

## Einstellungen

keine. Es gibt keine neue und keine geänderte Einstellung. Die bestehenden Modulschalter werden mit unveränderter Semantik gelesen.

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| – | – | – | – | – | – | – | – |

## Audit-Feld-Auswirkungen

Keine. Reine Lesepfade, es wird nichts geschrieben.

## Akzeptanzkriterien

1. `_Layout.cshtml` fragt im Menü keine Rolle und keinen Schalter selbst ab. Jeder Menülink hängt an `nav.Zeigt(NavZiele.X)` (T3).
2. `Home/Index.cshtml` rendert ausschließlich Katalog-Kacheln. Es gibt kein ViewBag-Recht und keinen hartcodierten Link (T4).
3. Für jede Rolle und Schalterkombination der Matrix ist jede sichtbare Kachel auch als Menüeintrag sichtbar (T1).
4. Kein sichtbarer Menüeintrag und keine sichtbare Kachel scheitert am Filter des Ziels (T2). Manuell: Kein Klick in TS-170 führt
   auf „Zugriff verweigert“ oder auf die Meldung „… ist deaktiviert“.
5. Modulschalter aus → Kachel **und** Menüeintrag des Moduls fehlen (je Schalter in TS-170.3).
6. Rolle fehlt → Kachel und Menüeintrag fehlen (TS-170.2).
7. Eine Kategorie ohne sichtbare Kachel wird mitsamt Überschrift nicht gerendert. Ein Menü-Dropdown ohne sichtbaren Eintrag wird
   nicht gerendert (Fall „Bestellungen“ aus dem Ist-Stand).
8. Die Kategorien erscheinen in der Reihenfolge aus Rückfrage 1, die Kacheln in Katalogreihenfolge. Kacheltitel = Menütitel
   (Rückfrage 4).
9. Behobene Abweichungen: Ein Benutzer nur mit stock oder stock_read sieht die Lager-Kacheln seines Rechts. Ein Benutzer nur mit
   masterdata sieht keine Kacheln Benutzer/Arbeitsplätze. Ein Benutzer ohne masterdata_read sieht weder Stammdaten-Kacheln noch
   Artikelinfo.
10. Ein Benutzer ohne jede sichtbare Kachel sieht den Hinweis aus F5.
11. Menü ansonsten unverändert: Einträge, Reihenfolge, Trennlinien, Badge an „Kommissionierung“, Dropdown-/Link-Regel
    Kommissionierung. Einzige Neuerung ist „Artikelinfo“ im Dropdown „Stammdaten“.
12. Barrierefreiheit: Text-Kontrast ≥ 4,5:1, Fokusring per Tastatur sichtbar, Karte als Ganzes klickbar (≥ 44 px), Symbole
    `aria-hidden`, Bedeutung nie nur über Farbe. Bei 390 px eine Spalte, kein Querscrollen.
13. Je Request wird die Navigation nur einmal berechnet (T1: zweiter `GetAsync` ruft keinen Dienst erneut auf).
14. Build und alle Tests grün (Web + Service).

## Test-Szenarien

Neues Kapitel **TS-170 „Dashboard-Kacheln nach Kategorie und Berechtigung (v1.99.47)“** in `docs/TESTSZENARIEN.md`. Danach eine Zeile
in [[testszenarien-index]]. Querverweis aus TS-21.2 (Leitstand-Sektion, Z. 3979) und dem Artikelinfo-Szenario (Z. 355). Standort:
DEV **AKE** und DEV **IDEAL**.

**Vorbedingungen:** Prüfabfragen A und B je DEV-DB ausgeführt, Ergebnis notiert. Dazu Testbenutzer mit je genau einer Rolle: admin,
picking, stock, stock_read, masterdata, masterdata_read, lagerbestellung, einkauf, schachtler, beschichtungsauftrag, vorbau, bde_user,
bde_shiftlead, reporting, sowie ein Benutzer ohne Rolle. Für den Pflichtwechsel-Ablauf siehe TS-Anmeldung.

| Nr | Schritt | Erwartet |
|---|---|---|
| 170.1 | admin, alle Schalter laut A wie vorgefunden: Startseite öffnen | Nur Kategorien mit Kacheln, Reihenfolge laut AK 8. Jede Kachel einmal anklicken: Die Zielseite öffnet sich. Jede Kachel steht auch im Menü. |
| 170.2 | Je Einzelrollen-Benutzer anmelden, Startseite und Menü vergleichen | Kachelmenge = die Menüeinträge mit Kategorie laut Katalog (F6). Kein Klick auf „Zugriff verweigert“. picking: Lager (ohne Artikelinfo), Kommissionierung, FA-Liste/FA-Struktur … stock_read: nur Bestände, Bewegungshistorie (+ Offene Bestellungen, wenn Wareneingang an). masterdata: Stammdaten-Kacheln + Artikelinfo, **keine** Benutzer/Arbeitsplätze. |
| 170.3 | Negativ Modul aus: nacheinander je Schalter aus A unter `/Settings` umschalten (BdeAktiv, LeitstandAktiv, BestellungenAktiv, LagerbestellungAktiv=false, EinkaufAktiv, BlechfertigungAktiv, FaHierarchy*Aktiv, WareneingangBestellungAktiv, FaCompletionAktiv, TeileverfolgungAktiv), Startseite neu laden | Kachel(n) **und** Menüeintrag des Moduls weg. Wird eine Kategorie dadurch leer, verschwindet auch ihre Überschrift. Schalter danach zurücksetzen. |
| 170.4 | Negativ Rolle fehlt: einem Benutzer die Rolle einkauf entziehen, neu anmelden | Kategorie „Einkauf/Wareneingang“ weg (sofern kein Lager-Recht mit Wareneingang), Menü „Einkauf“ weg. |
| 170.5 | Leere Kategorie / leeres Dropdown: BestellungenAktiv=true, LagerbestellungAktiv=false, Benutzer nur lagerbestellung | Weder Kategorie „Bestellungen“ noch Dropdown „Bestellungen“ (heute leeres Dropdown). |
| 170.6 | Benutzer ohne Rolle | Startseite zeigt nur den Hinweis aus F5, das Menü hat keine Fachpunkte. |
| 170.7 | Direktaufruf als picking ohne masterdata_read: `/Articles/Info` | Weiterhin „Zugriff verweigert“ (keine Rechteänderung), die Kachel fehlt. |
| 170.8 | Standort: auf IDEAL-DEV als admin | Kategorien Blechfertigung, Kommissionierlisten, Vormontage und „Arbeitsgänge → Sage“ nur, wenn A sie als an zeigt. Auf AKE-DEV: OSEON-Kacheln nur bei TeileverfolgungAktiv bzw. Rolle reporting. |
| 170.9 | Barrierefreiheit (1440 px und 390 px): Tastatur Tab durch die Kacheln, Kontrast messen (Titel, Beschreibung, Überschrift) | Fokusring sichtbar, Werte ≥ 4,5:1, eine Spalte bei 390 px, kein Querscrollen. |
| 170.10 | Regression Menü: Kommissionierung-Badge (Leitstand an, freigegebener Auftrag), Kommissionierung als Dropdown (picking + stock + Kommissionierlisten an) bzw. als Link (nur picking) | Wie vor dem Update. |

## Etappen (nur bei epic: true)

Kein Epic.

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | – | – | – |

## Deploy

*Vorläufig, der QA-Lauf bestätigt gegen den echten Diff.*

- **Web-App:** ja (Layout, Dashboard, Katalog, CSS, Hilfe).
- **Service:** nein (kein Service-Code). Die Version wird trotzdem in **beiden** `AppVersion.cs` angehoben (vorläufig 1.99.47).
- **Migration:** nein.
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`):
  `@{ Id='v1.99.47-DashboardKacheln'; Version='1.99.47'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen';
  Text='Startseite: Kacheln fuer alle freigeschalteten Funktionen nach Kategorie, Sichtbarkeit wie im Menue. Kacheln, die bisher auf "Zugriff verweigert" fuehrten, sind weg (z. B. Stammdaten ohne Stammdaten-Recht, Artikelinfo ohne Stammdaten-Lesen). Neuer Menuepunkt Stammdaten > Artikelinfo.' }`
  – sonst keine.
- **Publish-Befehle:** `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
- **Doku-Pflichten:** Anwender-Changelog `Views/Help/Changelog.cshtml`; Hilfe `Views/Help/Index.cshtml` mit neuem Abschnitt
  „Startseite“ (Kategorien, warum eine Kachel fehlt: Modul aus oder Recht fehlt → Administrator), dazu die Stellen `:471` und `:1713`
  angleichen. Neue Anwenderdoku `docs/anwender/18-startseite-und-menue.md` plus Eintrag in `docs/anwender/README.md`. Brain:
  Changelog, [[feature-map]], [[controller]] (Zeile Start/Hilfe → Navigationskatalog). Neuer ADR 0034 „Menü und Startseite aus einem
  Navigationskatalog“ (nächste freie Nummer, letzte ist 0033). [[fallstricke]]: Ein neuer Menüpunkt entsteht nur über `NavZiele` und
  die Regel im `switch`, die Wächter T2/T3 schlagen sonst fehl. README (AppSettings) bleibt unverändert.

**Final (QA 2026-11-19, aus dem echten Diff `f1747e10..f3cdd143`):**
- web: **ja** (Änderungen unter `IdealAkeWms/`).
- service: **ja, nur Versionsnummer** (`IDEALAKEWMSService/AppVersion.cs` → 1.99.45, damit Web und Dienst denselben Stand melden; keine Dienst-Logik geändert).
- migration: **nein** (keine Datei unter `*/Migrations/`, kein neues `SQL/`-Skript, kein Einmal-Skript).
- Paket fertig publiziert: `C:\git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish` (IDEALAKEWMSWeb + IDEALAKEWMSWebService + SQL + deploy), `publish-info.txt` Version 1.99.45, Commit `f3cdd14397d69935c974a25eaaeb045c9d10cacb`.
- Ablauf Mensch: Paket aufs Testsystem → `deploy\Update-IdealAkeWms.ps1` → testen → mergen. Nach dem Merge nur dann aus main neu publizieren, wenn der Merge getestete Dateien mit parallelen main-Änderungen zusammengeführt hat.
- Reihenfolge: kein Schema, kein Daten-Skript → kein zusätzlicher Backup-/Skript-Schritt aus diesem Paket; Web und Dienst wie üblich über das Update-Skript.
- Manifest (`scripts/deploy/nachlauf.psd1`): `v1.99.45-StartseiteKacheln` (Info, beide) vorhanden.

## Offene Rueckfragen

1. Kategorien und Reihenfolge: Lager · Bestellungen · Kommissionierung · Produktion/BDE · Blechfertigung · Beschichtung ·
   Einkauf/Wareneingang · Stammdaten · Administration – also der Vorschlag aus der Notiz plus „Bestellungen“ (wie das Menü-Dropdown
   `_Layout.cshtml:255-279`)? **Empfehlung: ja.**
2. Kachel-Auswahl laut Tabelle F6 (Spalte Kachel-Kategorie): Protokoll- und Detailpflege-Seiten (Sage-Lagerbuchungen,
   Sage-Wareneingänge, BDE-Stammdaten, Schichtkalender, Artikelkategorien u. Ä.) nur im Menü? **Empfehlung: ja**, sonst rund 55
   Kacheln für Admins.
3. Artikelinfo: Die Kachel folgt künftig dem Filter am Ziel (masterdata_read, `ArticlesController.cs:10`). Picker ohne
   masterdata_read verlieren sie, heute führt sie bei ihnen auf „Zugriff verweigert“. Artikelinfo kommt zusätzlich als Eintrag ins
   Menü „Stammdaten“. **Empfehlung: ja.** Wer Pickern die Artikelinfo öffnen will, legt dafür einen eigenen Backlog-Eintrag an
   (Rechteänderung).
4. Kacheltitel = Menütitel (z. B. „FA-Liste“ statt „Fertigungsaufträge“, „Bewegungshistorie“ statt „Bewegungen“), damit es nur eine
   Quelle gibt? **Empfehlung: ja.**
5. „OSEON AG-Übersicht“ (Reporting) hat heute keinen Modulschalter, nur die Rolle reporting (`_Layout.cshtml:341`). Auch an IDEAL
   (ohne OSEON) wird sie jedem mit dieser Rolle angezeigt. Bleibt das so? **Empfehlung: ja, unverändert.** Prüfabfrage B zeigt, ob an
   IDEAL überhaupt jemand die Rolle hat.

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. Kategorien und Reihenfolge inkl. „Bestellungen“ → **ja, Kategorien und Reihenfolge wie vorgeschlagen (inkl. Bestellungen)** (Gerald 2026-11-19 20:40)
2. Kachel-Auswahl laut Tabelle F6, Protokolle/Detailpflege nur im Menü → **ja, Protokoll-/Detailpflege nur im Menü** (Gerald 2026-11-19 20:40)
3. Artikelinfo filterkonform (masterdata_read) + Menüeintrag Stammdaten → **ja, Artikelinfo filterkonform (masterdata_read) + Menüeintrag Stammdaten** (Gerald 2026-11-19 20:40)
4. Kacheltitel = Menütitel → **ja, Kacheltitel = Menütitel** (Gerald 2026-11-19 20:40)
5. OSEON AG-Übersicht ohne Modulschalter unverändert → **abweichend: OSEON AG-Übersicht nur zeigen, wenn das OSEON-Modul aktiv ist (Schalter wie Teileverfolgung OSEON); sonst ausblenden – im Menü gleich** (Gerald 2026-11-19 20:40)

> [!important] Nachzug Gerald 2026-11-19 20:40: Antwort 5 weicht von der Empfehlung ab – OSEON AG-Übersicht (Kachel und Menü) zusätzlich an den OSEON-Modulschalter binden; der Dev-Lauf belegt, welcher Schalter (z. B. `TeileverfolgungAktiv`/`Sync:OseonTrackingEnabled`) das Modul an AKE/IDEAL kennzeichnet.

## QA-Nachweis (qa-agent, 2026-11-19, Sammellauf v1.99.45)

Unabhängiger Lauf, synchron, nicht delegiert. Worktree `C:\git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5`, Branch `feature/2026-08-07-ideal-teile-1-5`.
Geprüfter Commit-Bereich `f1747e10..HEAD` (a1ba945d Spec 1, 980beee9 Spec 2, b524d262 Spec 3, 0df40eeb Spec 4, defa0c05 Release, 947bb014 Doku) plus QA-Fix **f3cdd143**.

### 1. Build und Tests (selbst ausgeführt, nach dem QA-Fix wiederholt)
- `dotnet build IdealAkeWms.slnx`: 0 Fehler, 30 Warnungen (Bestand, u. a. CS8321 FaWorklist, CS8620 BfAuftragWorkflowTests:388).
- `dotnet test`: IdealAkeWms.Tests **3390 bestanden / 1 übersprungen / 0 Fehler** (3391); IDEALAKEWMSService.Tests **790 / 0 / 0**.

### 2. Review (eigener Lauf, Diff samt Umfeld gelesen)
Befunde des Sammellaufs – alle **nicht blockierend**, alle in **f3cdd143** behoben, danach Build und Tests erneut grün, DEV neu deployt:
- **F1 (Spec BF-Warenkorb):** Manifest-Info-Eintrag fehlte, obwohl die Spec genau einen verlangt → `v1.99.45-BfWarenkorbVorschlaege` (Info, IDEAL, Nachher) ergänzt.
- **F2 (Spec Artikelinfo / Release):** Anwender-Changelog legte nahe, Kommissionierwagen zählten in der Artikelinfo nicht mehr – falsch, dort zählen sie weiter (Rückfrage 2 betrifft nur den Lagerplatz-Text der Lager-/Glasbestellung) → Satz korrigiert.
- **F3 (Spec Updater):** `Invoke-Pruefung` gab bei reinen Anzeige-Prüfungen erst die gelbe Zeile „Reparatur nach Bestaetigung“ und danach die Warnung „nur Anzeige“ aus – widersprüchlich → Anzeige-Prüfung kehrt jetzt vor der gelben Zeile zurück (Ergebnis/Exit unverändert, Parse-Check 0 Fehler).

Beobachtungen ohne Befund: Dashboard ohne SVG-Symbole (bewusste Vereinfachung); Kongruenztest T2 prüft Einzelrollen (Regeln monoton, ausreichend); Abschnitt nutzt `aria-label` statt `aria-labelledby` (geringfügig); `limit` von `api/bf-auftraege/artikel` ohne Obergrenze (Bestand vor diesem Diff); `StockLocationInfo.LagerplatzText` sortiert `ThenBy(Code)` kulturabhängig (geringfügig).

Gezielt geprüfte Fehlerklassen: kein `@Html.Raw` in `<script>` (bf-artikelsuche.js baut Optionen per `createElement`); kein `hidden` neben `d-*`; keine stillen Ersatzwerte (leere Artikelnummer → leerer Text ohne Repository-Aufruf); keine Parallel-Array-Bindung; keine reine Browser-Validierung (Suche bleibt serverseitig `SucheArtikelAsync`); Sichtbarkeitsregel je Menüziel an EINER Stelle (`NavigationSicht`, ADR 0034). Menü-zu-Filter-Kongruenz: alte Menübedingungen aus `_Layout.cshtml` gegen die Regeln in `NavigationKatalog.cs` einzeln verglichen – gleichwertig, einzige Neuerung „Artikelinfo“ unter Stammdaten.

### 5. CLAUDE.md-Checkliste
Migration/SQL/FreshInstall/AgentJobs: entfällt (kein Schema). Audit-Felder: entfällt (keine Entität geändert). Version 1.99.45 in beiden `AppVersion.cs` + `Views/Help/Changelog.cshtml`: ja. `docs/TESTSZENARIEN.md` TS-167…170 im Worktree, Testindex im Hauptcheckout Zeilen 167–170: ja. Hilfeseite, Anwender-/Betriebsdoku (Kapitel 18 mit Fotos): ja. 5a Einstellungen (ADR 0020): keine neue oder geänderte Einstellung.

### 6. DEV-Deploy
Nach dem QA-Fix aus dem Worktree, erhöhte Shell, Web und Dienst je Standort: `dev-deploy.ps1 -Standort AKE` Exit 0, „HTTP 200, Fusszeile v1.99.45“; `-Standort IDEAL` Exit 0, Fusszeile v1.99.45. DEV-Schalter nur gelesen, nichts umgestellt, nichts an Sage geschrieben.

### 7. Publish
`C:\git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish\IDEALAKEWMSWeb` und `\IDEALAKEWMSWebService` vorher geleert, beide `dotnet publish -c Release` Exit 0; `SQL\` → `publish\SQL`, `scripts\deploy\` → `publish\deploy`; `publish-info.txt`: Version 1.99.45, Commit `f3cdd14397d69935c974a25eaaeb045c9d10cacb`, Branch feature/2026-08-07-ideal-teile-1-5, Spec 2026-11-19-v1-99-45-sammellauf, Erstellt 2026-11-19 21:19, Komponenten IDEALAKEWMSWeb + IDEALAKEWMSWebService.

### 3. Nachweisart je AK
| AK | Nachweis |
|---|---|
| 1 | Auto T3 `Layout_JederMenuelinkHaengtAnSeinemNavZiel_OhneEigeneRechteabfrage` |
| 2 | Auto T4 `Startseite_OhneViewBagUndOhneHartcodierteLinks` |
| 3, 7 (Kategorie) | Auto T1 `Kacheln_SindImmerAuchImMenue_UndKeineKategorieLeer` |
| 4 | Auto T2 `SichtbaresZiel_LaesstJedenFilterDurch` (Einzelrollen); Klickprobe Manual-UAT |
| 5 | Auto T1 (OSEON, ArbeitsgaengeSage, Empfänger) + DEV: OSEON AG-Übersicht auf AKE (Teileverfolgung an) da, auf IDEAL (aus) nicht |
| 6, 9, 10 | Auto T1 `OhneRolle_KeineKachel`, `Artikelinfo_FolgtStammdatenLesen_NichtKommissionierung`; mit echten Benutzern Manual-UAT |
| 7 (Dropdown leer) | Code-Review; DEV-Fall nicht herstellbar ohne Schalter-/Rollenänderung → Manual-UAT |
| 8 | DEV AKE + IDEAL (Admin): Lager · Bestellungen · Kommissionierung · Produktion/BDE · Blechfertigung (nur IDEAL) · Beschichtung · Einkauf/Wareneingang · Stammdaten · Administration; Titel = Menütitel |
| 11 | Review alt gegen neu gleichwertig; DEV: „Artikelinfo“ im Dropdown Stammdaten |
| 12 | Kontrast aus Bootstrap 5.3.3/site.css berechnet: Kategorie-Überschrift `text-muted` rgba(33,37,41,.75) auf #F5F5F5 **6,47:1**, Beschreibung auf Weiß **6,78:1**, Titel #053153 **≥ 12:1**, Fokusring 3 px #053153 (≥ 3:1). Sicht Fokusring/390 px Manual-UAT (Doku-Fotos 18-04/18-05 aus dem Dev-Lauf vorhanden). Symbole entfallen |
| 13 | Auto T1 `GetAsync_RechnetNurEinmalJeRequest` |
| 14 | grün (s. 1) |

### 4. Geänderte bestehende Tests
- `BeschichtungsauftraegeViewTests`: Bedingungs-Konstante jetzt `nav.ZeigtEines(...)` – **Verhalten absichtlich geändert (AK 1)**.
- `OrderRecipientsMenuTests.cs` gelöscht, ersetzt durch `NavigationSichtTests.Empfaenger_UndArtikelgruppen_FolgenDenNutzernDerEmpfaengergruppen` – **von der Spec vorgesehen** (affected_code „wird Regeltest im Katalog“).
Kein Fall „Sonstiges“.

### 6c. DEV-Schritte TS-170 (Admin, nur Ansicht)
| Schritt | Ergebnis | Status |
|---|---|---|
| Startseite AKE | Kategorien in Reihenfolge, OSEON AG-Übersicht und Bedarfsmeldungen da, keine Blechfertigung | OK |
| Startseite IDEAL | Blechfertigung (BF-Warenkorb, BF-Aufträge), Kommissionierlisten, Vormontage, Arbeitsgänge → Sage da; OSEON, Bedarfsmeldungen, Bestellvorschläge nicht | OK |
| Menü Stammdaten → Artikelinfo | vorhanden | OK |
| Rollen einzeln (stock, stock_read, masterdata, Picker) | braucht andere Benutzer | NICHT MÖGLICH (Mensch) |
| Schalter einzeln aus | Schalter auf DEV nicht umgestellt | NICHT MÖGLICH (Mensch) |
| Fokusring / 390 px Sicht | Screenshot-Werkzeug Timeout | NICHT MÖGLICH (Mensch) |

### Manuelle Checkliste (offen für den Menschen)
1. TS-170.2: Benutzer nur stock, nur stock_read, nur masterdata, Picker ohne masterdata_read – Kacheln und Menü wie AK 9.
2. TS-170.3: je Modulschalter aus → Kachel und Menüeintrag weg; Fall „Bestellungen-Dropdown leer“.
3. Benutzer ohne Rolle → Hinweis F5.
4. Tab durch die Kacheln → Fokusring sichtbar; Handy 390 px eine Spalte, kein Querscrollen.
5. Stichprobe: kein Kachel-/Menüklick führt auf „Zugriff verweigert“/„deaktiviert“.
6. Testsystem-Lauf, dann Merge.
