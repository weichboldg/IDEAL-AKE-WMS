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
| Start / Hilfe | `HomeController` (Dashboard-Kacheln), `HelpController` (inkl. Changelog) | `Views/Home/`, `Views/Help/` |
| Lager | `StockOverviewController`, `StockMovementsController`, `StockApiController` | `Views/StockOverview/`, `Views/StockMovements/` |
| Fertigungsauftraege | `ProductionOrdersController` (slim FA-Liste), `ProductionOrdersApiController` | `Views/ProductionOrders/` |
| Leitstand | `PickingLeitstandController`, `PickingStatusApiController` | `Views/PickingLeitstand/` |
| Kommissionierung | `PickingController` (inkl. `Bom`, `PrintBom`), `Api/PickingApiController`, `Api/PhotoController` | `Views/Picking/` |
| FA-Vorbau | `FaCompletionController`, `FaWorklistController`, `FaWorkStepsApiController`, `WorkStepsController`, `FaAttributesController` | `Views/FaCompletion/`, `Views/FaWorklist/`, `Views/WorkSteps/`, `Views/FaAttributes/` |
| IDEAL FA-Hierarchie (Teile 1–5, v1.31.0 — hinter Feature-Toggles, Default aus) | `FaHierarchyController` (Baumanzeige, Teil 2; seit v1.45.0 zusätzlich GET `WorkOperations?hauptFa=` → Partial für das Arbeitsgänge-Modal, erbt den Class-Level-Read-Filter), `FaHierarchyKommissionierListenController` (Teil 3), `FaHierarchyBeschichtungController` (Teil 4), `FaHierarchyVormontageController` (Teil 5) | `Views/FaHierarchy/`, `Views/FaHierarchyKommissionierListen/`, `Views/FaHierarchyBeschichtung/`, `Views/FaHierarchyVormontage/` |
| Bedarfsmeldungen | `PartRequisitionsController`, `Api/PartRequisitionsApiController` | `Views/PartRequisitions/` |
| Lager-/Glasbestellung | `WarehouseRequisitionsController`, `Api/WarehouseRequisitionsApiController`, `WarehousePickingController` (Lager-Worklist + `Print`), `MissingPartsController` (Werkbank-Sicht), `MissingPartsLagerController` (Lager-Sicht) | `Views/WarehouseRequisitions/`, `Views/WarehousePicking/`, `Views/MissingParts/`, `Views/MissingPartsLager/` |
| OSEON | `TrackingController` (Teileverfolgung), `OseonReportingController` | `Views/Tracking/`, `Views/OseonReporting/` |
| BDE | `BdeTerminalController`, `BdeApiController`, `BdeCockpitController`, `BdeBookingsController`, `BdeMasterDataController`, `BdeShiftCalendarController`, `BdeStatusApiController` | `Views/BdeTerminal/`, `Views/BdeCockpit/`, `Views/BdeBookings/`, `Views/BdeMasterData/`, `Views/BdeShiftCalendar/` |
| Stammdaten (operativ) | `ArticlesController` (inkl. `Info`), `ArticlesApiController`, `StorageLocationsController`, `ProductionWorkplacesController`, `OrderRecipientsController`, `ArticleCategoriesController`, `ArticleAttributesController` | je eigener View-Ordner |
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
| `[RequireAdminAccess]` | admin | UsersController, RolesController, WorkstationsController, SettingsController, SyncLogController, BdeShiftCalendarController, ServiceSettingsController, StandortEinstellungenController |
| `[RequirePickingAccess]` | admin, picking | ProductionOrdersApiController, PickingController (Actions ausser Index) |
| `[RequireFaCompletionAccess]` | admin, fa_completion | FaCompletionController |
| `[RequirePickingOrFaCompletionAccess]` | admin, picking ODER fa_completion | FaWorkStepsApiController (`/api/fa-work-steps/toggle`) |
| `[RequireVorbauAccess]` | admin, vorbau | FaWorklistController (seit v1.22.0) |
| `[RequireVorbauOrPickingOrLeitstandAccess]` | admin, vorbau ODER picking ODER leitstand | FaWorkStepsApiController (`/api/fa-work-steps/set-status`) — Erledigt-Status aus Abarbeitungsliste (vorbau) UND Leitstand-VK-VA (picking/leitstand) |
| `[RequirePickingOrVorbauOrFaCompletionAccess]` | admin, picking ODER vorbau ODER fa_completion | PickingController.PrintBom (Druck aus read-only Stueckliste) |
| `[RequireTrackingAccess]` | admin, tracking | TrackingController |
| `[RequireStockAccess]` | admin, stock, stock_keyuser, picking | StockMovementsController (Schreib-Actions: Ein-/Aus-/Umbuchung) |
| `[RequireStockReadAccess]` | admin, stock, stock_keyuser, picking, stock_read | StockOverviewController (class-level), StockMovementsController.Index — read-only Bestaende + Bewegungshistorie (seit v1.25.0) |
| `[RequireLagerProcessingAccess]` | admin, stock, stock_keyuser | WarehousePickingController, MissingPartsLagerController (picker explizit ausgeschlossen) |
| `[RequireStockKeyUserAccess]` | admin, stock_keyuser, picking | StockMovementsController (Lagerplatz ausbuchen/umbuchen) |
| `[RequirePickingOrTrackingOrLeitstandAccess]` | admin, picking ODER tracking ODER leitstand | ProductionOrdersController (slim Index) |
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
| `[RequireBeschichtungsauftragAccess]` | admin, **`beschichtungsauftrag`** *(neue Rolle, v1.31.0)* | FaHierarchyBeschichtungController (class-level, Teil 4). Kumulativ dazu das AppSetting-Gate `[RequireFaHierarchyBeschichtungAktiv]`. |
| `[RequireFaHierarchyKommissionierlistenAktiv]` | *(kein Rollen-Filter — AppSetting-Gate)* | FaHierarchyKommissionierListenController (class-level), **kumulativ** zu `[RequireLagerProcessingAccess]`. Toggle `FaHierarchyKommissionierlistenAktiv` (Default false): MVC → Redirect Home + WarningMessage. Invert-default-Attribut-Muster (v1.31.0). |
| `[RequireFaHierarchyBeschichtungAktiv]` | *(kein Rollen-Filter — AppSetting-Gate)* | FaHierarchyBeschichtungController (class-level), **kumulativ** zu `[RequireBeschichtungsauftragAccess]`. Toggle `FaHierarchyBeschichtungAktiv` (Default false). |
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
| `FaHierarchyVormontageController` (Teil 5) | `[RequireVorbauAccess]` *(bestehend)* | `[RequireFaHierarchyVormontageAktiv]` → `FaHierarchyVormontageAktiv` (Default false) |
| `HierarchieUmstellungController` (Teil 7, v1.32.0 — Einweg-Umschalt-Seite `/HierarchieUmstellung`) | `[RequireAdminAccess]` | **kein Toggle** — der einzige sanktionierte Schreibweg auf den Master `ProduktionsauftragHierarchisch`. Die generische `ServiceSettingsController`-Maske zeigt den Master nur read-only; der Guard-Decorator (`GuardedServiceSettingRepository`) ist der harte Choke-Point. Siehe [[0012-fa-hierarchie-einweg-migrationstor]]. |

**Neue Rolle `beschichtungsauftrag`** (v1.31.0, Teil 4) an den drei Pflichtstellen gefuehrt:
`RoleKeys.Beschichtungsauftrag`, `RequireBeschichtungsauftragAccessAttribute` +
`ICurrentUserService.HasBeschichtungsauftragAccessAsync`, `Views/Users/RoleOverview.cshtml`. **Nicht**
im `Program.cs`-Seed (analog `vorbau`/`lagerbestellung`). Die drei Aktiv-Toggles sind AppSettings
(ADR 0011), nicht ServiceSettings — reine Web-/Anzeige-Schalter.

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
