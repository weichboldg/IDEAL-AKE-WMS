---
type: codebase-karte
updated: 2026-07-27
---
# Controller

Alle Controller liegen in `../../IdealAkeWms/Controllers/` (reine API-Controller zusaetzlich in
`Controllers/Api/`). Geschwister: [[module]] · [[services]] · [[datenmodell]] · [[integrationen]]

## Zwei Controller-Muster

**MVC-Controller** (erben `Controller`): Validierung ueber `ModelState`, Erfolg ueber
`TempData["SuccessMessage"]` + `RedirectToAction`, Audit-Felder beim Update aus
`ICurrentUserService`. TempData kennt nur `SuccessMessage` (alert-success) und `WarningMessage`
(alert-warning) — **kein** `ErrorMessage`, Fehler laufen ueber ModelState.

**API-Controller** (erben `ControllerBase`, `[ApiController]`, `[Route("api/...")]`): Rueckgabe
`Ok()` / `NotFound()` / `BadRequest()`, keine Views.

Listen-Views folgen zusaetzlich verbindlich dem Pattern aus
[[0005-listen-view-pattern-mit-server-side-spaltenfilter]].

## Controller nach Fachbereich

| Bereich | Controller | Views |
|---|---|---|
| Anmeldung | `AccountController` (Login, Logout, WindowsLogin, Profile) | `Views/Account/` |
| Anmeldung (v1.99.28) | `AccountController.Login`: Regel R (nur mit gespeichertem Kennwort, Ausnahme `IsDevelopment()` mit Hinweis), Regel P (Schritt „Neues Kennwort festlegen“ bei `MussKennwortAendern`, zustandslos, `StartSession`); `UsersController` Create/Edit: Erstkennwort eingeben/erzeugen (`ApplyPassword`, Banner einmal), Index-Hinweis ohne Kennwort/offene Erstkennwörter – ADR [[0027-formular-anmeldung-nur-mit-kennwort-erstkennwort-mit-pflichtwechsel]] | `Views/Account/Login.cshtml`, `Views/Users/Create|Edit|Index.cshtml` |
| Start / Hilfe | `HomeController` (Dashboard-Kacheln), `HelpController` (inkl. Changelog) | `Views/Home/`, `Views/Help/` |
| Lager | `StockOverviewController`, `StockMovementsController`, `StockApiController` | `Views/StockOverview/`, `Views/StockMovements/` |
| Lager (Bestellungen) | `PurchaseOrdersController` (v1.84.0: Index = Vorschau, Receive GET/POST = Wareneingang; Gate `[RequireWareneingangBestellungAktiv]` → 404) | `Views/PurchaseOrders/` |
| Fertigungsauftraege | `ProductionOrdersController` (slim FA-Liste), `ProductionOrdersApiController` | `Views/ProductionOrders/` |
| Leitstand | `PickingLeitstandController`, `PickingStatusApiController` | `Views/PickingLeitstand/` |
| Kommissionierung | `PickingController` (inkl. `Bom`, `PrintBom`), `Api/PickingApiController`, `Api/PhotoController` | `Views/Picking/` |
| FA-Vorbau | `FaCompletionController`, `FaWorklistController`, `FaWorkStepsApiController`, `WorkStepsController`, `FaAttributesController` | `Views/FaCompletion/`, `Views/FaWorklist/`, `Views/WorkSteps/`, `Views/FaAttributes/` |
| IDEAL FA-Hierarchie (Teile 1–5, v1.31.0 — hinter Feature-Toggles, Default aus) | `FaHierarchyController` (Baumanzeige, Teil 2; seit v1.45.0 zusätzlich GET `WorkOperations?hauptFa=` → Partial für das Arbeitsgänge-Modal, erbt den Class-Level-Read-Filter), `FaHierarchyKommissionierListenController` (Teil 3), `FaHierarchyBeschichtungController` (Teil 4), `FaHierarchyVormontageController` (Teil 5) | `Views/FaHierarchy/`, `Views/FaHierarchyKommissionierListen/`, `Views/FaHierarchyBeschichtung/`, `Views/FaHierarchyVormontage/` |
| Bedarfsmeldungen | `PartRequisitionsController`, `Api/PartRequisitionsApiController` | `Views/PartRequisitions/` |
| Lager-/Glasbestellung | `WarehouseRequisitionsController`, `Api/WarehouseRequisitionsApiController`, `WarehousePickingController` (Lager-Worklist + `Print`), `MissingPartsController` (Werkbank-Sicht), `MissingPartsLagerController` (Lager-Sicht) | `Views/WarehouseRequisitions/`, `Views/WarehousePicking/`, `Views/MissingParts/`, `Views/MissingPartsLager/` |
| OSEON | `TrackingController` (Teileverfolgung), `OseonReportingController` | `Views/Tracking/`, `Views/OseonReporting/` |
| BDE | `BdeTerminalController`, `BdeApiController`, `BdeCockpitController`, `BdeBookingsController`, `BdeMasterDataController`, `BdeShiftCalendarController`, `BdeStatusApiController` | `Views/BdeTerminal/`, `Views/BdeCockpit/`, `Views/BdeBookings/`, `Views/BdeMasterData/`, `Views/BdeShiftCalendar/` |
| Stammdaten (operativ) | `ArticlesController` (inkl. `Info`), `ArticlesApiController` (seit v1.68.0 auch `GET /api/articles/by-matchcode`, kein Rollen-Filter wie `by-number`), `StorageLocationsController`, `ProductionWorkplacesController`, `OrderRecipientsController`, `ArticleCategoriesController`, `ArticleAttributesController` | je eigener View-Ordner |
| Stammdaten (admin) | `UsersController` (inkl. `CreateAdUser`, `RoleOverview`, `ResetViewPreferences`), `RolesController`, `WorkstationsController`, `SettingsController`, `ServiceSettingsController`, `StandortEinstellungenController` (IDEAL Teil 6 — kuratierte Standort-Maske), `SyncLogController` | je eigener View-Ordner |
| Benutzer-Prefs | `Api/UserViewPreferencesApiController` | — |

## Zugriffsschutz

Filter-Attribute liegen in `../../IdealAkeWms/Filters/`. `admin` ist Wildcard und ueberspringt
jede Pruefung. Konzept + Begruendung: [[0006-rollenkonzept-statische-keys-mit-admin-wildcard]];
Rollen-Bedeutungen: [[glossar]].

| Filter-Attribut | Rollen | Angewendet auf |
|----------------|--------|---------------|
| `[RequireMasterDataAccess]` | admin, masterdata | Action-Level fuer Edit-Actions in 6 Stammdaten-Controllern (Articles, StorageLocations, ProductionWorkplaces, OrderRecipients, ArticleCategories, ArticleAttributes). Class-Level ist `[RequireMasterDataReadAccess]`. |
| `[RequireMasterDataReadAccess]` | admin, masterdata_read, masterdata | Class-Level derselben 6 operativen Stammdaten-Controller. Edit-Actions verschaerfen mit `[RequireMasterDataAccess]`. |
| `[RequireAdminAccess]` | admin | UsersController, RolesController, WorkstationsController, SettingsController, SyncLogController, BdeShiftCalendarController, ServiceSettingsController, StandortEinstellungenController, OperationConfigsController *(v1.54.0, Reiter „Arbeitsgänge“ der BDE-Stammdaten, bewusst ohne `[RequireBdeActive]`)* |
| `[RequirePickingAccess]` | admin, picking | ProductionOrdersApiController, PickingController (Actions ausser Index) |
| `[RequireFaCompletionAccess]` | admin, fa_completion | FaCompletionController |
| `[RequirePickingOrFaCompletionAccess]` | admin, picking ODER fa_completion | FaWorkStepsApiController (`/api/fa-work-steps/toggle`) |
| `[RequireVorbauAccess]` | admin, vorbau | FaWorklistController (seit v1.22.0) |
| `[RequireVorbauOrPickingOrLeitstandAccess]` | admin, vorbau ODER picking ODER leitstand | FaWorkStepsApiController (`/api/fa-work-steps/set-status`) — Erledigt-Status aus Abarbeitungsliste (vorbau) UND Leitstand-VK-VA (picking/leitstand) |
| `[RequirePickingOrVorbauOrFaCompletionAccess]` | admin, picking ODER vorbau ODER fa_completion | PickingController.PrintBom (Druck aus read-only Stueckliste) |
| `[RequireTrackingAccess]` | admin, tracking | TrackingController |
| `[RequireStockAccess]` | admin, stock, stock_keyuser, picking | StockMovementsController (Schreib-Actions: Ein-/Aus-/Umbuchung) |
| `[RequireStockReadAccess]` | admin, stock, stock_keyuser, picking, stock_read | StockOverviewController (class-level), PurchaseOrdersController (class-level, v1.84.0), StockMovementsController.Index — read-only Bestaende + Bewegungshistorie (seit v1.25.0) |
| `[RequireLagerProcessingAccess]` | admin, stock, stock_keyuser | WarehousePickingController, MissingPartsLagerController (picker explizit ausgeschlossen) |
| `[RequireStockKeyUserAccess]` | admin, stock_keyuser, picking | StockMovementsController (Lagerplatz ausbuchen/umbuchen) |
| `[RequirePickingOrTrackingOrLeitstandAccess]` | admin, picking ODER tracking ODER leitstand **ODER vorbau** (seit v1.25.0, Filter-Name unveraendert; Code: `CanPick \|\| CanViewTracking \|\| CanManagePickingRelease \|\| HasVorbauAccess`) | ProductionOrdersController (slim Index), FaHierarchyController (class-level), SageSdataFaController (class-level, v1.71.0; plus Gate `SageSdataFaAnsichtAktiv` inline) |
| `[RequirePickingOrLeitstandAccess]` | admin, picking ODER leitstand | PickingLeitstandController (class-level) |
| `[RequirePickingOrStockAccess]` | picking ODER stock | PartRequisitionsController |
| `[RequirePickingOrStockOrLagerbestellungAccess]` | admin, picking, stock, stock_keyuser, lagerbestellung ODER glasbestellung | WarehouseRequisitionsController, WarehouseRequisitionsApiController |
| `[RequireStockOrLagerbestellungAccess]` | admin, stock, stock_keyuser, picking, lagerbestellung ODER glasbestellung | MissingPartsController |
| `[RequireLagerbestellungAktiv]` | *(kein Rollen-Filter — AppSetting-Gate)* | WarehouseRequisitionsController, WarehouseRequisitionsApiController, MissingPartsController, MissingPartsLagerController, WarehousePickingController (class-level, **kumulativ** zum Rollen-Filter). Master-Schalter `LagerbestellungAktiv` (Default true, nur `"false"` sperrt): MVC → Redirect Home + WarningMessage, API → 404 |
| `[RequireLeitstandAccess]` | admin, leitstand | historisch ProductionOrdersController; ab v1.12.0 ueber Composite-Filter auf PickingLeitstandController |
| `[RequireReportingAccess]` | admin, reporting | OseonReportingController |
| `[RequireBdeUserAccess]` | admin, bde_user, bde_shiftlead, bde_admin | BdeTerminalController, BdeApiController |
| `[RequireBdeShiftleadAccess]` | admin, bde_shiftlead, bde_admin | BdeCockpitController, BdeBookingsController (Index), BdeMasterDataController |
| `[RequireBdeAdminAccess]` | admin, bde_admin | BdeBookingsController (Edit/Cancel), BdeMasterDataController (Terminals) |
| `[RequireBeschichtungsauftragAccess]` | admin, **`beschichtungsauftrag`** *(neue Rolle, v1.31.0)*, seit v1.51.0 auch `beschichtungsauftrag_schreiben` | FaHierarchyBeschichtungController, BeschichtungsauftraegeController (class-level, Lesen). Kumulativ dazu das AppSetting-Gate `[RequireFaHierarchyBeschichtungAktiv]`. |
| `[RequireBeschichtungsauftragWriteAccess]` | admin, **`beschichtungsauftrag_schreiben`** *(neue Rolle, v1.51.0)* | BeschichtungsauftraegeController — action-level an jeder schreibenden Action (Create, Bearbeiten, PositionenHinzufuegen, Bemerkung, Status, Reklamation GET+POST). Read/Edit-Split (ADR 0006); Waechter `BeschichtungsauftraegeControllerTests.JedeSchreibendeAction_HatSchreibfilter_UndPostsAntiforgery`. |
| `[RequireFaHierarchyKommissionierlistenAktiv]` | *(kein Rollen-Filter — AppSetting-Gate)* | FaHierarchyKommissionierListenController (class-level), **kumulativ** zu `[RequireLagerProcessingAccess]`. Toggle `FaHierarchyKommissionierlistenAktiv` (Default false): MVC → Redirect Home + WarningMessage. Invert-default-Attribut-Muster (v1.31.0). |
| `[RequireFaHierarchyBeschichtungAktiv]` | *(kein Rollen-Filter — AppSetting-Gate)* | FaHierarchyBeschichtungController, BeschichtungsauftraegeController (class-level), **kumulativ** zu `[RequireBeschichtungsauftragAccess]`. Toggle `FaHierarchyBeschichtungAktiv` (Default false). |
| `[RequireEinkaufAccess]` | admin, **`einkauf`** *(neue Rolle, v1.99.20)* | PurchaseProposalsController (class-level, nur Lesen – keine POST-Action, Wächter-Test). Kumulativ dazu `[RequireEinkaufAktiv]`. |
| `[RequireEinkaufAktiv]` | *(kein Rollen-Filter — AppSetting-Gate)* | PurchaseProposalsController (class-level), kumulativ zu `[RequireEinkaufAccess]`. Toggle `EinkaufAktiv` (Default false, nur exakt "true" schaltet frei; sonst Startseite mit Warnung). |
| `[RequireFaHierarchyVormontageAktiv]` | *(kein Rollen-Filter — AppSetting-Gate)* | FaHierarchyVormontageController (class-level), **kumulativ** zu `[RequireVorbauAccess]`. Toggle `FaHierarchyVormontageAktiv` (Default false). |
| *(kein Filter)* | jeder eingeloggte User | UserViewPreferencesApiController (Login-Check, kein Rollen-Filter) |

**Sonderfaelle**
- Admin-Reset fuer View-Einstellungen laeuft ueber `UsersController.ResetViewPreferences`
  (`[RequireMasterDataAccess]`).
- Der Ordner `Filters/` enthaelt einige Attribute mehr als diese Tabelle listet (u. a.
  `RequireBdeActiveAttribute`, `RequirePickingOrVorbauAccessAttribute`,
  `RequirePickingOrTrackingAccessAttribute`, `RequireStockOrPickingOrTrackingAccessAttribute`) —
  teils historisch, teils punktuell verwendet. Der Ordner ist die Wahrheit, diese Tabelle die
  Landkarte.

> **Pflege-Hinweis:** Bei jeder Filter-Aenderung ziehen **drei** Stellen nach: das Attribut, diese
> Tabelle und die hand-gepflegte Anwender-Uebersicht
> `../../IdealAkeWms/Views/Users/RoleOverview.cshtml`. Siehe [[fallstricke]].

## IDEAL FA-Hierarchie — Zugriff je Controller (Teile 1–5, v1.31.0)

Vier neue Web-Controller (reine Lesepfade) fuer den Standort IDEAL, alle **hinter Feature-Toggles
mit Default aus** — bei ausgeschalteten Toggles verhaelt sich das System wie bisher (AKE
unveraendert). Access-Filter Class-Level (Read); Edit gibt es nicht (Lesepfade). Feature-Gate
**kumulativ** zum Rollen-Filter. Spec [[2026-07-29-standort-ideal-uebersicht]].

| Controller | Access-Filter (Read) | Kumulatives Feature-Toggle-Gate |
|---|---|---|
| `FaHierarchyController` (Teil 2, Baumanzeige `/FaHierarchy`) | `[RequirePickingOrTrackingOrLeitstandAccess]` *(bestehend, wiederverwendet)* | **keiner** — nur AppSetting `FaHierarchyMaxTiefe` als Tiefen-Cap (kein Ein/Aus-Toggle). Route nur per URL erreichbar (kein Nav-Link, Stand v1.31.0). |
| `FaHierarchyKommissionierListenController` (Teil 3) | `[RequireLagerProcessingAccess]` *(bestehend)* | `[RequireFaHierarchyKommissionierlistenAktiv]` → `FaHierarchyKommissionierlistenAktiv` (Default false) |
| `FaHierarchyBeschichtungController` (Teil 4) | `[RequireBeschichtungsauftragAccess]` → **neue Rolle `beschichtungsauftrag`** | `[RequireFaHierarchyBeschichtungAktiv]` → `FaHierarchyBeschichtungAktiv` (Default false) |
| `BeschichtungsauftraegeController` (Beschichtungsauftrag-Ausbau Teil 4, v1.51.0 — `/Beschichtungsauftraege`: Liste, Detail, Print, `Pdf/{id}`) | `[RequireBeschichtungsauftragAccess]`; schreibende Actions zusaetzlich `[RequireBeschichtungsauftragWriteAccess]` → **neue Rolle `beschichtungsauftrag_schreiben`** | `[RequireFaHierarchyBeschichtungAktiv]` (derselbe Schalter wie die Vorschlagsliste). Je Request genau EIN Aufruf des `BeschichtungsauftragWorkflow`, danach Redirect (der Workflow verwirft nach Speicherfehler den Change-Tracker). |
| `FaHierarchyVormontageController` (Teil 5) | `[RequireVorbauAccess]` *(bestehend)* | `[RequireFaHierarchyVormontageAktiv]` → `FaHierarchyVormontageAktiv` (Default false) |
| `HierarchieUmstellungController` (Teil 7, v1.32.0 — Einweg-Umschalt-Seite `/HierarchieUmstellung`) | `[RequireAdminAccess]` | **kein Toggle** — der einzige sanktionierte Schreibweg auf den Master `ProduktionsauftragHierarchisch`. Die generische `ServiceSettingsController`-Maske zeigt den Master nur read-only; der Guard-Decorator (`GuardedServiceSettingRepository`) ist der harte Choke-Point. Siehe [[0012-fa-hierarchie-einweg-migrationstor]]. |

**Neue Rolle `beschichtungsauftrag`** (v1.31.0, Teil 4) an den drei Pflichtstellen gefuehrt:
`RoleKeys.Beschichtungsauftrag`, `RequireBeschichtungsauftragAccessAttribute` +
`ICurrentUserService.HasBeschichtungsauftragAccessAsync`, `Views/Users/RoleOverview.cshtml`. **Nicht**
im `Program.cs`-Seed (analog `vorbau`/`lagerbestellung`). Die drei Aktiv-Toggles sind AppSettings
(ADR 0011), nicht ServiceSettings — reine Web-/Anzeige-Schalter.

**Neue Rolle `beschichtungsauftrag_schreiben`** (v1.51.0, [[2026-10-06-beschichtungsauftrag-ausbau-teil-4-spec]]) an den drei
Pflichtstellen: `RoleKeys.BeschichtungsauftragSchreiben` + `RequireBeschichtungsauftragWriteAccessAttribute` +
`ICurrentUserService.HasBeschichtungsauftragWriteAccessAsync`, diese Datei, `Views/Users/RoleOverview.cshtml`. Die Schreibrolle
**schliesst Lesen ein** (`HasBeschichtungsauftragAccessAsync` prueft admin, `beschichtungsauftrag`, `beschichtungsauftrag_schreiben`),
damit eine Zuweisung genuegt. Kein Seed — der Admin legt die Rolle mit genau diesem Key unter `/Roles` an (Deploy-Schritt).
Die Vorschlagsliste (`FaHierarchyBeschichtungController`) bleibt Lesen; den Knopf „Aus Vorschlagsliste erstellen" rendert
sie nur bei Schreibrecht, der POST geht an `BeschichtungsauftraegeController.Create`.

## Abgeloeste Routen (Stub-Redirects)

`/ProductionOrders/ToggleRelease|BulkRelease|SetPriority|ChangeAssignedPicker` sind seit v1.12.0
301-Redirects auf die entsprechenden `/PickingLeitstand/...`-Endpoints.

## Toggle-APIs

| Endpoint | Schreibt |
|---|---|
| `/api/picking-status/toggle` | `ProductionOrderPickingStatus`-Flags |
| `/api/fa-work-steps/toggle` | `FaWorkStep.IsRemoved` (AG an-/abwaehlen) |
| `/api/fa-work-steps/set-status` | `FaWorkStep.Status` (3-State Offen/InBearbeitung/Fertig) |
| `/api/bde-status/toggle` | `ProductionOrderBdeStatus.IsDoneBde` |
| `/api/warehouserequisitions/quick-add` | Draft-Bestellung aus der Stueckliste |

Warum die Flags in Satelliten liegen: [[0009-app-status-in-satelliten-tabellen-neben-sage-master]].

## Sage-SData-Stammdaten (v1.73.0)

- `CreditorsController`, `DebitorsController`: Klasse `[RequireMasterDataReadAccess]`, Create/Edit/SetActive `[RequireMasterDataAccess]`, kein Löschen;
  Sage-Felder bei `Source = Sage` schreibgeschützt (serverseitig). Keine neue Rolle.
- `ArticleAttributesController`: Fachsperre für Sage-Merkmale (Name, Typ, Optionen, Werte, Löschen; Reihenfolge/Aktiv pflegbar).
- `BdeMasterDataController`: Reiter `sage` + POST `SageZuordnen`/`SageZuordnungLoesen` (BDE-Filter wie bisher), Parameter `onlyUebergabeOffen`.
- `StorageLocationsController.Index`: Parameter `onlyUebergabeOffen`, Spalten Lagerort/Sperre/Übergabe.

## Kommissionierauftraege (v1.88.0) und FA an Lagerbuchungen (v1.87.0)

- `PickingController`: `Index` = Auftragsliste, `Auftrag/{id}` (Stueckliste im Auftragsmodus), `AuftragUmbuchen`, `AuftragAbschliessen`,
  `AuftragDrucken` – `[RequirePickingAccess]`; `Bom/{faId}` nur lesend mit Auftragsliste.
- `PickingLeitstandController.AuftraegeNachziehen` – `[RequireLeitstandAccess]`; Freigabe/Ruecknahme/Prio/Kommissionierer schreiben auf Auftraege durch.
- `PickingAreasController` (Kommissionierbereiche + Regeln) – Klasse `[RequireMasterDataReadAccess]`, Schreiben `[RequireMasterDataAccess]`. Keine neue Rolle.
- `StockApiController` `fa-lookup`/`fa-search` (v1.87.0) unter dem Klassenfilter `RequireStockOrPickingOrTrackingAccess`. FA-Links in Lager-Tools
  folgen dem FA-Leserecht `FaBuchungsText.CanViewProductionOrdersAsync` (= Regel von `RequirePickingOrTrackingOrLeitstandAccess`, ein Ort).

## Wareneingang Teil 2 (v1.99.4)

- `PurchaseOrdersController.SageWareneingaenge` (GET): Liste „Wareneingänge an Sage“, Klassen-Filter `RequireStockReadAccess` + Feature-Gate
  `RequireWareneingangBestellungAktiv`. `SageWeErneutSenden` (POST): `RequireStockKeyUserAccess` (admin, stock_keyuser, picking), nur Status Fehler.
  `Receive` (POST): neuer Parameter `lieferschein`; Platzregel und Sage-WE-Status, wenn `SageWareneingangBelegAktiv` an. Keine neue Rolle.

## Wareneingang-CSV (v1.99.11, [[2026-11-12-wareneingang-barcode-lieferschein-csv-export-spec]])

- `PurchaseOrdersController.CsvErneutSchreiben(Guid vorgang)` (POST, AntiForgery): `RequireStockKeyUserAccess` (admin, stock_keyuser, picking) wie
  `SageWeErneutSenden`; zusaetzlich Klassen-Filter `RequireStockReadAccess` + Feature-Gate `RequireWareneingangBestellungAktiv`. Setzt die
  Ausgangszeile `WareneingangCsvExporte` nur im Status Fehler auf Offen (Fehlertext leer, `Modified*` aus `ICurrentUserService`); sonst
  `WarningMessage`. Knopf in der Liste „Sage-Wareneingaenge“ nur mit `CanErneutSenden` und CSV-Fehler. Keine neue Rolle.
- `Receive` (POST): neuer Parameter `barcode`, ausgewertet nur bei AppSetting `WareneingangBarcodeAktiv` (sonst ignoriert); Pruefung Zeichen/Laenge/
  reservierte Namen, Sage-WE aus = Fehler, Duplikat ohne Gross/Klein (`IPurchaseOrderReceiptRepository.BarcodeVergebenAsync`). Recht unveraendert
  `RequireStockAccess`.


**Neue Rolle `einkauf`** (v1.99.20, [[2026-11-08-einkauf-bestellvorschlaege-spec]]) an den drei Pflichtstellen: `RoleKeys.Einkauf` +
`RequireEinkaufAccessAttribute` + `ICurrentUserService.CanAccessEinkaufAsync` (admin oder einkauf), diese Datei, `Views/Users/RoleOverview.cshtml`.
Kein Seed, kein `00_FreshInstall`-Eintrag – angelegt über die Update-Skript-Prüfung `Pruefung/RolleEinkauf` (Reparatur nach dem
Beschichtungsauftrag-Muster); Zuweisung an den Einkauf ist Hand-Schritt unter `/Roles`. Seite `/PurchaseProposals` (Menü „Einkauf → Bestellvorschläge“).

## Nachtrag 2026-11-14 (Wochenend-Lauf): Controller ohne Eintrag

- `SageTestController` (Register „Sage-Test“, v1.99.14, [[2026-11-13-sdata-teil-2-userfelder-stueckliste-spec]]) – Klasse `[RequireAdminAccess]`,
  nur `[HttpGet]`: `Index` (Stand/Verbindung), `Feldliste`, `Stueckliste` (live je Artikel/FA). Liest ueber `ISageSdataReadClient`, schreibt nie
  nach Sage. Zugangsdaten ueber `SdataZugangsdaten` ([[0024-sdata-kennwort-verschluesselt-in-den-service-einstellungen]]). Kein Feature-Gate
  (Gate `SageSdataFaAnsichtAktiv` seit v1.99.14 entfernt, SQL/147).
- `SageSdataFaController` (FA-Struktur live aus SData, v1.71.0) – Klasse `[RequireAdminAccess]`, `Index(belId)`; seit v1.99.14 ohne eigenes Gate
  und ohne eigenen Menuepunkt (Aufruf aus dem Register Sage-Test).
- `PurchaseOrdersController` ist auf drei Dateien verteilt: `PurchaseOrdersController.cs` (Offene Bestellungen), `.Receive.cs` (Wareneingang zu
  Bestellung, `Receive`), `.SageWe.cs` (Liste „Sage-Wareneingänge“ `SageWareneingaenge`, Erneut senden, Klären → als übergeben markieren,
  CSV erneut schreiben). Filter siehe die Abschnitte Wareneingang oben.
- `PurchaseProposalsController` (`/PurchaseProposals`, v1.99.20/21) – `[RequireEinkaufAccess]` + `[RequireEinkaufAktiv]` (siehe oben), nur Lesen +
  CSV; Rechenstelle `PurchaseProposalCalculator`.
