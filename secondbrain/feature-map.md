---
type: feature-map
updated: 2026-08-12
---
# Feature-Landkarte

Erst-Befuellung 2026-07-27 aus `PROJECT_STATUS.md` (inzwischen ins Brain aufgeloest) und den
Spec-Titeln in `../docs/superpowers/specs/`. Nach jedem GEMERGED ergaenzt der Merge-Nachlauf hier
eine Zeile.

**Status-Lesart:** `Gemerged` = in `main`. Alles bis **v1.26.0** ist gemerged und nach
`origin/main` gepusht (bis v1.25.0 Merge-Commit `3b127f2`, v1.26.0 FA-Zusatzinfos `47bd69f`).
v1.27.0–v1.28.0 gemerged; **v1.29.0 + v1.30.0** (WMS Bugs & Improvements Teil 1–5,7,8) gemerged
(Merge-Commit `65e3901`, 2026-08-06). **Produktiv-Deploy und Manual-UAT stehen weiterhin aus**
→ [[2026-07-deploy-v1-25-0]]. Specs ohne Pfadangabe existieren nicht — das Feature entstand vor der
Spec-Disziplin. Release-Details je Version: `changelog/` (v1.0.0 – v1.30.3; AKE-Hotfixes v1.30.1–v1.30.3 auf `main`).

## Lager und Bestand

| Feature | Status | Spec | Code-Einstieg |
|---|---|---|---|
| Lagerbewegungen (Ein-/Aus-/Umbuchung) | Gemerged | — | `../IdealAkeWms/Controllers/StockMovementsController.cs` |
| Lagerplatz ausbuchen/umbuchen (en bloc) | Gemerged | — | `StockMovementsController` (Rolle `stock_keyuser`) |
| Bestandsuebersicht + Bewegungshistorie | Gemerged | — | `StockOverviewController`, `StockMovementRepository` |
| Meldebestand-Warnung + Farbcodierung | Gemerged | — | `Article.MinimumStock`, `StockCheckService` (Service) |
| Barcode/QR-Scanner | Gemerged | — | `../IdealAkeWms/wwwroot/js/barcode-scanner.js` |
| Foto-Upload bei Kommissionierung | Gemerged | — | `Controllers/Api/PhotoController.cs` |
| Einbuchung: FA-Autofill Lagerplatz-Hinweis | Gemerged (v1.11) | `2026-05-11-einbuchung-fa-autofill-design.md` | `StockMovementsController`, `StockApiController` |
| StorageLocation `IstBuchbar` (user-controlled) | Gemerged (v1.9.x) | `2026-05-07-storagelocation-istbuchbar-design.md` | `Models/StorageLocation.cs` |
| StorageLocation-Code auf 50 Zeichen | Gemerged (v1.14.0) | — | `Models/StorageLocation.cs` (manuell weiter 12) |
| Hauptlagerplatz am Artikel | Gemerged (v1.25.0) | `2026-07-07-hauptlagerplatz-design.md` | `Models/Article.cs`, `StockMovementRepository` |
| Rolle `stock_read` (read-only Bestand) | Gemerged (v1.25.0) | `2026-07-07-vorbau-bom-button-stock-read-role-design.md` | `Filters/RequireStockReadAccessAttribute.cs` |
| FA-Hinweis auf Ist-Bestand (Bugfix, `onlyActualStock`) | Gemerged (v1.29.0) | [[2026-08-05-wms-bugs-improvements-teil-1-spec]] | `StockMovementRepository.GetStockByProductionOrderAsync` |
| Mehrfach-Einbuchung (`/StockMovements/InboundBulk`) | Gemerged (v1.29.0) | [[2026-08-05-wms-bugs-improvements-teil-2-spec]] | `StockMovementsController.InboundBulk`, `Views/StockMovements/InboundBulk.cshtml` |
| WA-Scan-Button Bewegungshistorie (Trennzeichen-Kuerzung) | Gemerged (v1.29.0) | [[2026-08-05-wms-bugs-improvements-teil-3-spec]] | `Views/StockMovements/Index.cshtml` |
| Bewegungshistorie: Bewegungsart-/Datum-Spaltenfilter funktionsfaehig (Bugfix) | Gemerged (v1.30.0) | [[2026-08-05-wms-bugs-improvements-teil-4-spec]] | `StockMovementRepository.ApplyMovementColumnFilter`, `Views/StockMovements/Index.cshtml` |
| Einbuchung Standardmenge 1 | Gemerged (v1.30.0) | [[2026-08-05-wms-bugs-improvements-teil-5-spec]] | `StockMovementsController.Inbound` (GET) |
| Lager-/Glasbestellung: Kommentar (Kopf) + DUMMY-Artikel | Gemerged (v1.30.0) | [[2026-08-05-wms-bugs-improvements-teil-7-spec]] | `WarehouseRequisition.Comment`, `WarehouseRequisitionsApiController` (`/comment`, `/items/dummy`) |

## Fertigungsauftraege, Leitstand, Kommissionierung

| Feature | Status | Spec | Code-Einstieg |
|---|---|---|---|
| FA-Liste + KW-Filter | Gemerged | — | `Controllers/ProductionOrdersController.cs` |
| Kommissionierung / Stueckliste (BOM) inkl. Baum, Filter, Druck | Gemerged | — | `Controllers/PickingController.cs`, `Views/Picking/Bom.cshtml` |
| BOM-Datenquelle Sage-View + OSEON-Fallback | Gemerged | — | `Data/Repositories/BomRepository.cs` → [[0007-bom-quelle-sage-view-mit-oseon-fallback]] |
| BOM-Cache + Lackierteil-Erkennung | Gemerged | `2026-04-07-bom-cache-und-lackierteil-erkennung.md` | `IDEALAKEWMSService/Services/BomCacheSyncService.cs`, `CoatingDetectionService.cs` |
| Leitstand: Freigabe + Priorisierung | Gemerged (v1.2.0) | `2026-04-03-leitstand-design.md` | `Controllers/PickingLeitstandController.cs` |
| Leitstand: Filter + Bulk-Freigabe | Gemerged | `2026-05-08-leitstand-filter-bulk-design.md` | `PickingLeitstandController` |
| Kommissionierer-Zuweisung bei Freigabe | Gemerged (v1.4.0) | — | `PickingStatusApiController`, `KommissionierungMitZuweisung` |
| Baugruppen-Flags VK/VL/VE/VT/VA | Gemerged (v1.11.0), abgeloest v1.22.0 | `2026-05-11-production-order-assembly-flags-design.md` | ersetzt durch `FaWorkSteps` |
| ProductionOrder-Split (App-Status in Satelliten) | Gemerged (v1.11.0) | `2026-05-12-production-order-split-phase-1/2/4-design.md`, `-roadmap.md` | → [[0009-app-status-in-satelliten-tabellen-neben-sage-master]] |
| Leitstand als eigenes Hauptmenue | Gemerged (v1.14.0) | `2026-04-03-navigation-restructuring-design.md` | `Views/Shared/_Layout.cshtml` |
| FA-Abschliessen wirkt wieder (Lese-Seite) | Gemerged (v1.21.1) | — | Fallstrick „IsDone vs IsDonePicking" in [[fallstricke]] |
| FA-Reconciliation (verwaiste FAs stornieren) | Gemerged (v1.25.0) | `2026-07-07-fa-reconciliation-design.md` | `IDEALAKEWMSService/Services/ProductionOrderReconciler.cs` |
| FA-Zusatzinfos (Sage) inkl. Auto-Erledigt + BDE-Sperre | Gemerged (v1.26.0), **Deploy/UAT offen** | `2026-07-22-pa-zusatzinfos-design.md` | `Models/ProductionOrderExtraInfo.cs`, `FaZusatzinfoSyncService.cs` |

## FA-Vervollstaendigung und Vorbau

| Feature | Status | Spec | Code-Einstieg |
|---|---|---|---|
| FA-Vervollstaendigung (Vormontage-Gruppen) | Gemerged (v1.13.0), Toggle ab v1.14.0 | `2026-05-15-production-order-split-phase-4-design.md` | `Controllers/FaCompletionController.cs`, Gate `FaCompletionAktiv` |
| FA-Vorbau: AG-Katalog + Abarbeitungsliste | Gemerged (v1.22.0) | `2026-06-12-fa-vervollstaendigung-erweiterung-design.md` | `Controllers/FaWorklistController.cs`, `Models/FaWorkStep.cs` |
| FA-AG-Auto-Erkennung aus BOM-Cache | Gemerged (v1.22.0) | s. o. | `IDEALAKEWMSService/Services/FaWorkStepDetectionService.cs` |
| Werkbank-Filter (Komma) + Bezeichnung 1/2 | Gemerged (v1.23.0) | `2026-06-19-faworklist-werkbankfilter-bezeichnung-design.md` | `Services/WorkbenchFilter.cs`, `User.DefaultWorkbenches` |
| FA-Abarbeitungsliste: personalisierter Bezeichnung-1-Default-Filter | Gemerged (v1.30.0) | [[2026-08-05-wms-bugs-improvements-teil-8-spec]] | `User.DefaultFilterFaWorklistDescription1`, `FaWorklistController.Index` (Redirect+`df1`) |
| Stueckliste (BOM): personalisierter Bezeichnung-1-Default-Filter | Gemerged (v1.30.0) | [[2026-08-05-wms-bugs-improvements-teil-8-spec]] | `User.DefaultFilterBomDescription1`, `Views/Picking/Bom.cshtml` (`setColumnFilter`) |
| Erkennungs-/BOM-Cache-Protokoll aufgegliedert | Gemerged (v1.23.0) | `2026-06-25-fa-detection-protokoll-aufgliederung-design.md` | `BomCacheCoverage.cs` |
| FaWorkStep 3-Wert-Status + Beschichtungstermin-Spalte | Gemerged (v1.24.0) | `2026-06-25-faworkstep-3state-coating-filter-design.md` | `Models/FaWorkStepStatus.cs`, `Services/CoatingDateCalculator.cs` |
| Vorbau-BOM-Button + read-only Stueckliste | Gemerged (v1.25.0) | `2026-07-07-vorbau-bom-button-stock-read-role-design.md` | `Views/Picking/Bom.cshtml`, `ReadOnlyBomBuilder.cs` |

## Bestellwesen

| Feature | Status | Spec | Code-Einstieg |
|---|---|---|---|
| Bedarfsmeldungen aus der Stueckliste | Gemerged (v1.1.0) | `2026-04-02-bedarfsmeldungen-design.md` | `Controllers/PartRequisitionsController.cs`, Gate `BestellungenAktiv` |
| Lagerbestellung aus der Produktion | Gemerged (v1.8.4) | `2026-04-30-lagerbestellung-aus-produktion-design.md` | `Controllers/WarehouseRequisitionsController.cs` |
| Notiz je Position + INT-Mengen | Gemerged (v1.14.0) | — | `Views/WarehousePicking/Details.cshtml` (Culture-Fallstrick) |
| Teilgeliefert + Fehlteile + Drucken-und-Abschliessen | Gemerged (v1.18.0) | `2026-05-29-teilgeliefert-fehlteile-design.md`, `2026-05-29-missingparts-include-partially-delivered-design.md` | `WarehouseRequisitionStatus`, `WarehousePickingController` |
| ShortageStatus 3-State + 2-Tab-Fehlteile | Gemerged (v1.19.0) | `2026-05-29-shortage-status-3state-design.md` | `Models/ShortageStatus.cs` (Migration 65, destruktiv) |
| Notiz EK + Werkbank-eigene Fehlteile-Sicht | Gemerged (v1.19.0) | `2026-05-29-note-einkauf-mineonly-lager-view-design.md` | `MissingPartsController`, `MissingPartsLagerController` |
| Rolle `lagerbestellung` + Artikelinfo fuer masterdata_read | Gemerged (v1.23.0) | `2026-06-19-lagerbestellung-rolle-artikelinfo-design.md` | `Filters/RequireStockOrLagerbestellungAccessAttribute.cs` |
| Lagerbestellungs-Druck spiegelt GUI (Spalten/Sort/Filter) | Gemerged (v1.23.0) | `2026-06-19-warehousepicking-print-spalten-sort-design.md` | `Services/WarehousePickingPrintLayout.cs` |
| Glas-Bestellung als eigener Bestelltyp | Gemerged (v1.25.0) | `2026-07-03-glas-bestellung-design.md` | `Models/WarehouseRequisitionType.cs`, `Services/GlasArticleGroupFilter.cs` |
| Lagerbestellung aus der Stueckliste + Master-Schalter | Gemerged (v1.25.0) | `2026-07-09-lagerbestellung-aus-bom-design.md` | `Api/WarehouseRequisitionsApiController` (`quick-add`), `Filters/RequireLagerbestellungAktivAttribute.cs` |
| AKE-Hotfix: Stuecklisten-Druck per POST (404.15) + Lagerbestellung Ist-Menge bestaetigen (Pflichtpruefung, Autosave NULL) | **Gemerged** (v1.30.1, Merge-Commit `f89c0124`, 2026-10-05; auf AKE deployt als `8cc23829`) | [[2026-09-28-ake-hotfix-stueckliste-druck-lagerbestellung-ist-spec]] | `PickingController.PrintBom`/`PrintBomPost`, `WarehouseRequisitionRepository.CloseAsync`, `Views/WarehousePicking/Details.cshtml`, `SQL/Einmalig/` ([[0015-einmal-datenskripte-ausserhalb-der-nummerierten-sql-reihe]]) |
| AKE-Hotfix: Zahnrad (Spaltenauswahl) auf der Lagerbestellung-Detailseite sichtbar | **Gemerged** (v1.30.2, Merge-Commit `e386bfce`, 2026-11-08) | [[2026-10-08-v1-30-2-ake-hotfix-zahnrad-lagerbestellung]] | `Views/WarehousePicking/Details.cshtml`, `IdealAkeWms.Tests/Views/BomZahnradHostTests.cs` |
| AKE-Hotfix: OSEON-Sync neu ausgeschriebener Auftrag (MERGE über Auftragsnummer) + Stücklisten-Druck Rand/Sortierung + Werkbank in der Kommissionierung | **Testbereit** (v1.30.3, direkt auf `main`, `1c7fc333`; Ersatznachweis QA Lauf 2 akzeptiert, Gerald 2026-11-08) | [[2026-11-07-oseon-sync-neu-ausgeschriebener-auftrag-spec]] | `IDEALAKEWMSService/Services/OseonSyncService.cs`, `OseonOrderSelection.cs`, `PickingController.RenderPrintBomAsync`, `Views/Picking/Index.cshtml` |
| Sicherheits-Hotfix: Kommissionierfotos nur mit Anmeldung + Kommissionier-Recht, Löschen nur eigener FA | **Gemerged** (v1.30.4, Merge `df54dee8` 2026-11-10; Deploy AKE+IDEAL nur Web ausstehend) | [[2026-11-08-kommissionierfotos-ohne-anmeldung-abrufbar-spec]] | `PhotoController.GetFile`, `PhotoStorage`, `LoginRedirectMiddleware` |

## OSEON

| Feature | Status | Spec | Code-Einstieg |
|---|---|---|---|
| Teileverfolgung (3-Ebenen-Baum + Ampel) | Gemerged | — | `Controllers/TrackingController.cs`, `Services/OseonTrafficLightService.cs` |
| AG-Konfiguration (Soll-Termine + Relevanz) | Gemerged | — | `Models/OseonOperationConfig.cs` |
| Delta-Sync ueber `LastChangedInOseon` | Gemerged | — | `IDEALAKEWMSService/Services/OseonSyncService.cs` |
| Print- + Tracking-Verbesserungen | Gemerged | `2026-04-17-print-tracking-improvements-design.md` | `Services/PrintService.cs` |
| Suche + Sortierung | Gemerged | `2026-05-08-oseon-search-sort-design.md` | `TrackingController` |
| Artikel-Filter-Fix | Gemerged (v1.8.3) | `2026-04-30-oseon-tracking-article-filter-fix-design.md` | `OseonGroupViewModelBuilder` |
| Reporting — AG-Uebersicht | Gemerged (v1.8.3) | `2026-04-30-oseon-reporting-ag-uebersicht-design.md` | `Controllers/OseonReportingController.cs` |
| iOS-Fix + Lazy-Load-Refactor | Gemerged (v1.16.0) | `2026-05-28-oseon-tracking-ios-fix-design.md` | `wwwroot/js/oseon-tracking-lazy.js`, `barcode-scanner.js` |

## BDE (Betriebsdatenerfassung)

| Feature | Status | Spec | Code-Einstieg |
|---|---|---|---|
| BDE Phase 1 — Terminal, Cockpit, Korrekturen | Gemerged (v1.8.0) | `2026-04-14-bde-phase-1-design.md` | `Controllers/BdeTerminalController.cs`, `Services/BdeBookingService.cs` |
| BDE-Settings | Gemerged (v1.8.1) | `2026-04-16-bde-settings-design.md` | `AppSettingKeys` (`Bde*`) |
| Phase 2.1 — Werkbank-Erweiterungen | Gemerged (v1.8.2) | `2026-04-20-bde-phase-2-1-werkbank-erweiterungen-design.md` | `ProductionWorkplace`, `BdeDefaultWorkOperationService` |
| Phase 2.2 — Mehrfachanmeldung + Zeit-Split | Gemerged (v1.8.2) | `2026-04-21-bde-phase-2-2-mehrfachanmeldung-zeit-split-design.md` | `Services/BdeTimeSplitService.cs` |
| Phase 2.3 — Schichtkalender + Auto-Pause + Feiertags-Sync | Gemerged (v1.8.2) | `2026-04-27-bde-phase-2-3-schichtkalender-auto-pause-design.md` | `Services/BdeShiftCalendarService.cs`, `BdeAutoPauseService.cs` |

## Stammdaten, Berechtigungen, Listen

| Feature | Status | Spec | Code-Einstieg |
|---|---|---|---|
| Rollenbasierte Zugriffskontrolle (RBAC) | Gemerged | `2026-03-20-rollenkonzept-design.md` | `Models/RoleKeys.cs`, `Filters/` → [[0006-rollenkonzept-statische-keys-mit-admin-wildcard]] |
| Navigation umstrukturiert | Gemerged (v1.3.0) | `2026-04-03-navigation-restructuring-design.md` | `Views/Shared/_Layout.cshtml` |
| Artikelkategorien + Merkmale | Gemerged | `2026-04-04-artikelkategorien-merkmale-design.md` | `Controllers/ArticleCategoriesController.cs`, `ArticleAttributesController.cs` |
| Anpassbare Tabellenansichten (Spalten) | Gemerged (v1.7.0) | `2026-04-10-customizable-view-preferences-design.md` | `wwwroot/js/column-preferences.js`, `Models/UserViewPreference.cs` |
| Listen-Pagination + User-Default | Gemerged (v1.14.0) | — | `Services/PageSize.cs`, `Views/Shared/_Pagination.cshtml` |
| Server-Side Spaltenfilter | Gemerged (v1.14.0) | — | `Services/ColumnFilterHelper.cs` → [[0005-listen-view-pattern-mit-server-side-spaltenfilter]] |
| Feingranulare Berechtigungen (Read/Edit-Split) | Gemerged (v1.20.0) | `2026-06-03-finegrained-permissions-design.md` | `Filters/RequireMasterDataReadAccessAttribute.cs`, `Views/Users/RoleOverview.cshtml` |
| Universal-Filter-Rollout (alle Tabellen filterbar) | Gemerged (v1.21.0) | `2026-06-10-universal-filter-rollout-design.md` | `wwwroot/js/table-filter.js`, `ColumnDefinitions.cs` |
| Windows-Authentifizierung + AD-Benutzer | Gemerged (v1.23.0) | `2026-06-18-windows-auth-ad-users-design.md` | `Middleware/WindowsAutoLoginMiddleware.cs` → [[0002-dual-auth-session-login-plus-windows-sso]] |
| Windows-Auth UA-Gate + ForceSso-Button | Gemerged (v1.25.0) | `2026-07-09-windows-auth-ua-gate-design.md` | `Services/UserAgentHelper.cs`, `AccountController.WindowsLogin` |
| ENTER-Spaltenfilter + Android-Fix | Gemerged (v1.24.0) | `2026-06-25-faworkstep-3state-coating-filter-design.md` | `wwwroot/js/table-filter.js` (`enterkeyhint`) |

## Integrationen und Service

| Feature | Status | Spec | Code-Einstieg |
|---|---|---|---|
| enaio DMS-Integration | Gemerged | — | `IDEALAKEWMSService/Services/EnaioDmsSyncService.cs` |
| Feiertag-Import (date.nager.at) | Gemerged | — | `HolidaySyncService.cs`, `Services/BusinessDayService.cs` |
| Sage Lagerplatz-Sync (Phase 1) | Gemerged (v1.9.0) | `2026-05-05-sage-lagerplatz-sync-design.md` | `LagerplatzSyncService.cs` |
| Sage Lagerbestand-Sync (Phase 2) | Gemerged (v1.10.0) | `2026-05-06-sage-lagerbestand-sync-design.md` | `LagerbestandSyncService.cs` |
| Artikel-Sync-Erweiterung (UNION + Meldebestand) | Gemerged (v1.17.0) | `2026-05-28-article-sync-erweiterung-design.md` | `SageImportService.SyncArticlesAsync` |
| Aktivitaets-Protokoll fuer alle Sync-Services | Gemerged (v1.15.0) | `2026-05-26-synclog-pflicht-alle-syncs-design.md` | `Services/SyncLogger/` → [[0010-aktivitaets-protokoll-mit-isolierten-dbcontexts]] |
| Protokoll auch fuer Non-Sync-Services | Gemerged (v1.15.1) | `2026-05-27-activity-log-non-sync-services-design.md` | `SyncLogServices.All` |
| Service-Resilienz + Fehlermail | Gemerged (v1.25.0) | — | `SyncWorker.RunResilientAsync`, `SyncErrorNotifier.cs` |
| Typisierte, vollstaendige Service-Einstellungen | Gemerged (v1.25.0) | `2026-07-08-service-settings-typed-catalog-design.md` | `Models/ServiceSettingDefinitions.cs` → [[0008-servicesettings-db-first-mit-typisiertem-katalog]] |
| Lagerbestand-Nullsetzen verwaister Paare | Gemerged (v1.25.0) | `2026-07-09-lagerbestand-nullsetzen-verwaist-design.md` | `LagerbestandZeroingPlanner.cs` |
| Cleanup-Jobs im Service (Protokoll-Bereinigung) | Gemerged (v1.25.0) | `2026-07-15-cleanup-jobs-service-design.md` | `Workers/CleanupWorker.cs`, `ActivityLogCleanupService.cs` |

## IDEAL-Standort (hierarchische Produktionsauftraege)

Zweiter Standort **IDEAL** (eigenes Deployment, gemeinsamer Codestamm mit AKE) auf Basis
hierarchischer FAs (Haupt-FA → Sub-FA → Sub-Sub-FA). Teile 1–5 als **ein** Buendel in einem Worktree
gebaut, **ein** Merge am Ende (Schranke 2). Alles additiv, `ProductionOrders` unangetastet, AKE
unveraendert — **hinter Feature-Toggles, Default aus**. Migration additiv `SQL/89` (zwei neue
Tabellen). Zum selben v1.31.0-Buendel gehoeren zwei reine UI-Nachtraege (Etappe 6 = Spaltenauswahl
an den 4 Listen + Sort-Fix; Etappe 7 = FA-Struktur-Tree-Table) — **keine** neue Migration. Die
BDE-/Materialisierungs-Erweiterung (fruehere „Teil 6/7/8") ist **nicht** in diesem Buendel (eigene
Epics). Uebergreifende Spec: [[2026-07-29-standort-ideal-uebersicht]]; Aufgaben-/UAT-Notiz
[[2026-08-07-ideal-teile-1-5]].

| Feature | Status | Spec | Code-Einstieg |
|---|---|---|---|
| Teil 1 — Struktur-Fundament (Import Sage-Views → `FaHierarchyNode`/`FaHierarchyOrderInfo`, Repos + Cache-Decorator, Sync-Service) | **Testbereit** (v1.31.0) | [[2026-07-29-standort-ideal-teil-1-spec]] | `Models/FaHierarchyNode.cs`, `Models/FaHierarchyOrderInfo.cs`, `IDEALAKEWMSService/Services/FaHierarchySyncService.cs`, `Services/FaHierarchySql.cs`; ServiceSettings `Sync:HierarchicalFaEnabled`/`Sync:FaHierarchyListeViewName`/`Sync:FaHierarchyInfosViewName` |
| Teil 2 — Struktur-/Baumanzeige (rekursiv, Tiefen-Cap + Zyklenschutz) | **Testbereit** (v1.31.0) | [[2026-07-29-standort-ideal-teil-2-spec]] | `Controllers/FaHierarchyController.cs`, `Services/FaHierarchyTreeBuilder.cs`; `[RequirePickingOrTrackingOrLeitstandAccess]`, AppSetting `FaHierarchyMaxTiefe` (kein Nav-Link, nur URL) |
| Teil 3 — Kommissionierlisten + gemeinsamer Listen-/Druck-Baustein | **Testbereit** (v1.31.0) | [[2026-07-29-standort-ideal-teil-3-spec]] | `Controllers/FaHierarchyKommissionierListenController.cs`, `Services/FaHierarchyListBuilder.cs`, `IBarcodeService`; `[RequireLagerProcessingAccess]` + Toggle `FaHierarchyKommissionierlistenAktiv` (Default aus) |
| Teil 4 — Beschichtungsauftrag (erweitert Baustein, ohne PDF) | **Testbereit** (v1.31.0) | [[2026-07-29-standort-ideal-teil-4-spec]] | `Controllers/FaHierarchyBeschichtungController.cs`, `Services/BeschichtungsauftragService.cs`; **neue Rolle `beschichtungsauftrag`** (`[RequireBeschichtungsauftragAccess]`) + Toggle `FaHierarchyBeschichtungAktiv` (Default aus) |
| Teil 5 — Vormontage-Listen (zwei Sichten, Sicht 2 = Matchcode-Aggregat) | **Testbereit** (v1.31.0) | [[2026-07-29-standort-ideal-teil-5-spec]] | `Controllers/FaHierarchyVormontageController.cs`; `[RequireVorbauAccess]` + Toggle `FaHierarchyVormontageAktiv` (Default aus) |
| UI-Nachtrag (Etappe 6) — per-Benutzer-Spaltenauswahl an den 4 IDEAL-Listen + `table-filter.js`-Sort-Fix (je `<tbody>` separat) | **Testbereit** (v1.31.0, Commit `3894e3d`) | [[2026-08-12-listen-spaltenauswahl-spec]] | `wwwroot/js/column-preferences.js` + `#view-config`/`#column-config` je View, `viewKey` in `Models/ViewModels/ColumnDefinitions.cs` `GetByViewKey`, `wwwroot/js/table-filter.js` (`sortTable`) |
| UI-Nachtrag (Etappe 7) — FA-Struktur seitenweite Tree-Table (OSEON-Stil) + Kontrast-Fix (WCAG AA) + 4 Knoten-Icons + Baum-Spaltenfilter + column-prefs | **Testbereit** (v1.31.0, Commits `a372638`+`a864745`) | [[2026-08-12-fa-struktur-darstellung-spec]] | `Views/FaHierarchy/*`, `wwwroot/js/fa-hierarchy-tree.js`, `Services/FaNodeClassifier.cs`, `viewKey` `FaHierarchyStructure` in `ColumnDefinitions` |
| UAT-Anpassung (Etappe 8, 2026-08-13) — SubFA=0-Blattannahme aufgehoben (alle Positionen mit Flag, Anomalie-Banner weg, Kommissionier+Vormontage) + Kommissionier-Summiert (je HauptFA/Artnr/Sollmenge/Ziel, KW auf `KO_Termin`) + Vormontage-Summiert-Wochenbezug (KW auf `FE_Termin`, HauptFA ignoriert) | **Testbereit** (v1.31.0, Commit `f07ea34`) | Teil-3/5-Nachtraege in [[2026-07-29-standort-ideal-teil-3-spec]] / [[2026-07-29-standort-ideal-teil-5-spec]] | `KommissionierListenService`/`VormontageService` (`leafOnly:false`), neue `.../Summiert`-Views, `Services/IsoWeekRange.cs` |

> **Status:** UMGESETZT + QA-gruen im Worktree `feature/2026-08-07-ideal-teile-1-5` (zuletzt
> `34ca168`, qa-agent 2026-08-18: Build gruen, Web 1219/1 skip/0 Fehler, Service 231/0 Fehler),
> wartet auf **Schranke 2** (Manual-UAT am IDEAL-Testsystem + Merge durch den Menschen).
> Merge-Commit noch offen. Offene UAT-Punkte + Deploy-Handgriffe: [[2026-08-07-ideal-teile-1-5]].

### Teil 7 — Schema-Inversion + Einweg-Migrationstor + Materialisierung (v1.32.0, eigener Epic, selber Branch)

Ab Teil 7 wird die **Kern-Tabelle `ProductionOrders`** angefasst: Sub-FAs werden aus der Struktur zu
echten Auftraegen materialisiert (rueckmeldefaehig). Laeuft im **selben** Worktree wie Teile 1–5,
Migration **`SQL/90`** (daten-konvertierend, DB-Backup zwingend). Architektur:
[[0012-fa-hierarchie-einweg-migrationstor]]; Umsetzungsnotiz [[2026-07-29-standort-ideal-teil-7]];
Changelog [[2026-08-17-v1-32-0-ideal-teil-7]].

| Etappe | Status | Code-Einstieg |
|---|---|---|
| A — Schema-Inversion (`SubOrderNumber` unique, `ParentSubOrderNumber`, `SageMissingSince`, Index-Tausch, Backfill) | **erledigt** (`7440726`) | `Models/ProductionOrder.cs`, Migration `20260814105526`, `SQL/90` |
| B — Einweg-Migrationstor `ProduktionsauftragHierarchisch` (Guard-Planer + Decorator-Choke-Point, Umschalt-Seite, Audit, Runbook) | **erledigt** (`e727329..0a8175a`) | `Services/HierarchischeStruktur/*`, `Data/Repositories/GuardedServiceSettingRepository.cs`, `Controllers/HierarchieUmstellungController.cs`, `docs/RUNBOOK-FA-HIERARCHIE-RUECKBAU.md` |
| C — Materialisierungs-Sync (3 Sync-Regeln, `SageMissingSince` selbstheilend, Sammelmail) | **erledigt** (`ae9be6c..285909e`) | `IDEALAKEWMSService/Services/FaMaterializationPlanner.cs` + `FaMaterializationSyncService.cs`; SyncLog `FaMaterialization` |
| D — Lookup-Härtung (`GetAllByFaAndOperationAsync` + Multi-Hit-Log) + dreistufige Auto-Erledigt-Sperre + Reconcile-Test + `OrderNumber`-Sweep (7 kritisch → Teil 8) | **erledigt** (`b9e2482..d9a4d1e`) | `Data/Repositories/WorkOperationRepository.cs`, `IDEALAKEWMSService/Services/FaZusatzinfoSyncService.cs`, Key `Sync:FaZusatzinfoAutoErledigtEnabled` |
| E — Version-Bump v1.32.0 + Anwender-Changelog + Brain (ADR 0012, fallstricke §9, services/controller-Karte) | **erledigt** | `AppVersion.cs` (Web+Service), `Views/Help/Changelog.cshtml` |

> **Status Teil 7:** QA-gruen im Worktree (`status: Testbereit`, qa-agent 2026-08-17, HEAD `db9ecab`,
> Web 1186 (+1 skip) + Service 231 gruen). Master **default aus** — Umstellung ist ein bewusster
> spaeterer Schritt am Zielsystem. **Mapping datenabhaengig** (Wurzel `HauptFA==SubFA`, Node-Dezimal
> `NULL→0`) → erster echter Datenlauf, siehe manuelle Test-Checkliste in
> [[2026-07-29-standort-ideal-teil-7]]. Wartet auf **Schranke 2** (Manual-UAT + Merge). Merge bringt
> Teile 1–5 **und** Teil 7 zusammen (Schranke 2 fuers ganze Buendel, kein Zwischen-Merge).
>
> **Post-QA-Fix #2 (`2b305ba`, 2026-08-18):** beim echten App-Start freigelegter zweiter `SqlError 3701`
> — EF-`AlterColumn` auf `SubOrderNumber` erzeugte ungeschützten Auto-`DROP INDEX`; durch geschütztes
> Raw-SQL ersetzt (deckungsgleich SQL/90), offline via `dotnet ef migrations script` verifiziert.
> [[fallstricke]] §9 Nachtrag.

### Teil 8 — Sub-FA-Rückmeldung / BDE-Disambiguierung (v1.33.0, eigener Epic, selber Branch)

Macht Sub-FAs am **BDE-Terminal** bedienbar: nach der Inversion (Teil 7) ist `OrderNumber` mehrdeutig,
ein Scan kann mehrere Sub-FAs treffen. Einstieg `Services/BdeScanResolver.cs` +
`BdeApiController.ResolveScan` (`GET /api/bde/resolve-scan`) + `wwwroot/js/bde-terminal.js`.

| Etappe | Status | Code-Einstieg |
|---|---|---|
| 1 — Verifikation (Teil-7-Naht, OSEON-Urteil → Etappe 5 entfällt) | **erledigt** (`d95cb63`) | `docs/TEIL-8-BDE-ETAPPE-1-VERIFIKATION.md` |
| 2 — Serverseitige Auflösungslogik `BdeScanResolver` (Exact/Ambiguous/NotInScope/NotFound, werkbank-gescopt) + 14 Unit-Tests | **erledigt** (`992d7e1`) | `Services/BdeScanResolver.cs`, `Program.cs` |
| 3 — Scan-Auswahl-UI Normal-Modus + `resolve-scan`-Endpoint | **erledigt** (`14d3c18`) | `Controllers/BdeApiController.cs`, `Views/BdeTerminal/Index.cshtml`, `wwwroot/js/bde-terminal.js`, `wwwroot/css/bde.css` |
| 4 — NurFA-Fix (kein last-wins) + Teileverfolgung bestätigt | **erledigt** (`5c0735d`) | `wwwroot/js/bde-terminal.js`; `Controllers/TrackingController.cs` (nur Filter, unverändert) |
| 5 — OSEON-Seite | **entfällt** (Etappe-1-Urteil) | — |
| 6 — Version v1.33.0 + Anwender-Changelog + Testszenarien TS-66 | **erledigt** (`b8847d2`) | `AppVersion.cs` (Web+Service), `Views/Help/Changelog.cshtml`, `docs/TESTSZENARIEN.md` |

> **Status Teil 8:** alle Etappen erledigt (5 entfällt), Web-Suite **1205 grün**. Wartet auf **qa-agent**
> (setzt `Testbereit`), danach **Schranke 2** zusammen mit Teilen 1–5 + Teil 7 (ein Merge, kein
> Zwischen-Merge). **Testdaten-Vorbedingung Schranke 2:** hierarchische Rückmeldedaten (mehrere Sub-FAs
> derselben `OrderNumber` an einer Werkbank) im IDEAL-Testsystem. Detail [[2026-07-29-standort-ideal-teil-8]].

### Teil 6 — Standorteinstellungen-Maske (v1.34.0, kein Epic, selber Branch)

Kuratierte, gruppierte Admin-Maske über die standortbezogenen Werte (AppSettings + ServiceSettings),
kein zweiter Speicherort. Einstieg `Controllers/StandortEinstellungenController.cs` +
`Views/StandortEinstellungen/Index.cshtml` + `Services/Standort/StandortSettingsWriter.cs` (atomarer
Zwei-Backend-Write) + `Models/Standort/StandortSettingsCatalog.cs` (kuratierte Feldliste, Allow-List).

| Aspekt | Status | Detail |
|---|---|---|
| Maske + Gruppen (Firmendaten/Mandant-Views/Toggles/Import) | **umgesetzt** (`a99705f`) | admin-only, Nav-Link, Bootstrap-Konsistenz `/ServiceSettings` |
| Atomarer Zwei-Backend-Write | **umgesetzt** | `IStandortSettingsWriter`, eine EF-Transaktion (`IsRelational`-Guard), Cache nach Commit, kein Partial-Save |
| Master read-only + Allow-List-Schutz (AK 2) | **umgesetzt** | Badge + Link `/HierarchieUmstellung`, nie im POST, kein Guard-Aufruf |
| Firmendaten neu (`Firmenname`/`Firmenanschrift`) | **umgesetzt** | AppSettings-Keys, kein Seed/Migration (Fallback-Regel) |

> **Status Teil 6:** `Testbereit` (qa-agent, 2026-08-18) — Build gruen, Web **1214/1215 grün** (+1
> vorbestehender Skip), Service **231/231 grün**, TS-67 deckt AK 1–6 vollstaendig ab, Code-Review ohne
> Findings. Wartet auf **Schranke 2** zusammen mit dem ganzen Bündel (ein Merge). Changelog
> [[2026-08-18-v1-34-0-ideal-teil-6-standorteinstellungen]], Detail [[2026-07-29-standort-ideal-teil-6]].

### Bündel-Nachlese — BOM-Guard hierarchisch (vor Merge vorgezogen, 2026-08-18)

UAT-Fund: nach Master-Flip endete der **BOM-Knopf** in HTTP 500 (fest verdrahtete AKE-Stücklisten-View
existiert auf IDEAL nicht). **Minimal-Fix vorgezogen** → das ganze Bündel ging von `Testbereit` **zurück
auf `InUmsetzung`**, danach **erneute QA** (Build + beide Suiten). Einstieg
`Data/Repositories/HierarchicalBomGuardRepository.cs` (äußerster `IBomRepository`-Decorator, keyt am
Master, kein try/catch) + `Views/Picking/Bom.cshtml` (Hinweis → `/FaHierarchy`). Nebenbei mitgenommen:
`supportsSortDefault=true` für die 3 flachen IDEAL-Listen (Baum bleibt false). **Kein Versions-Bump.**
Spec [[2026-08-18-bom-guard-hierarchisch-spec]], Aufgabe [[2026-08-18-bom-guard-hierarchisch]].
**Abgelöst durch die BOM-Bridge (v1.36.0, siehe nächster Abschnitt)** — der Guard ist im Code entfernt
(`HierarchicalBomGuardRepository` + TS-68 → TS-70). Die ursprünglich geplante Volllösung
[[2026-08-18-ake-view-abhaengigkeiten-hierarchisch-spec]] ist `Ueberholt` (superseded).

> **Status:** `Testbereit` (qa-agent, 2026-08-18, Worktree `34ca168`) — Build gruen, Web
> **1219/1 skip/0 Fehler**, Service **231/0 Fehler**. Code-Review ohne Findings: alle 4
> `IBomRepository`-Aufrufer (Picking-BOM/Druck, FaWorklist/FaCompletion via
> `ReadOnlyBomBuilder` → gemeinsam `Views/Picking/Bom.cshtml`) laufen ueber denselben Guard,
> DI-Factory ohne Selbstreferenz. Damit ist das **gesamte Bündel** (Teile 1–5 + Teil 7 + Teil 8 +
> Teil 6 + dieser Guard) wieder auf `Testbereit` — wartet **gemeinsam** auf Schranke 2 (ein Merge,
> kein Zwischen-Merge).

### FA-Liste + verwandte Ansichten hierarchiefähig (v1.35.0, Epic, selber Branch)

Dritte Fehlerklasse nach Teil 7: die materialisierten Sub-FAs (130) sind sichtbar, aber unbedienbar
(gleiche `OrderNumber` mehrfach, keine `SubOrderNumber`/Elternzeiger, Suche liefert die ganze Gruppe).
Macht die Hierarchie in **6 Ansichten** sichtbar/bedienbar — master-gated, AKE bit-identisch. Einstieg
`Data/Repositories/ProductionOrderRepository.cs` (`GetForLeitstandGroupedAsync`),
`wwwroot/js/fa-liste-gruppierung.js`, die 6 `_*Row.cshtml`-Partials, `PickingLeitstandController`
(Kaskade). Spec [[2026-08-18-fa-liste-hierarchie-anzeige-spec]], Aufgabe/Detail
[[2026-08-18-fa-liste-hierarchie-anzeige]], Changelog [[2026-09-07-v1-35-0-ideal-fa-liste-hierarchie]].

| Etappe | Status | Code-Einstieg |
|---|---|---|
| A — Anzeige-Fundament + ProductionOrders-Referenz + Z4-Erhebung | **erledigt** (`43b6bde`/`e8d21d0`/`5ee8177`) | `ProductionOrderRepository.GetForLeitstandGroupedAsync`, `ColumnDefinitions` +`parent-sub-order-number`, `fa-liste-gruppierung.js`, `Views/ProductionOrders/_ProductionOrderRow.cshtml` |
| B — 5 weitere Views (FaCompletion/PickingLeitstand/Picking/FaWorklist/Tracking) | **erledigt** (`4a74ea6`/`bf6863c`/`d7df8a9`/`837a1f6`/`087c311`) | je `_*Row.cshtml` + Master-Gate; Tracking/Index = 3-Ebenen-Baum (OSEON-Muster), Picking = Prio-Queue |
| C — Kaskade „Alle Sub-FAs fertigmelden" (Leitstand-Kopfzeile) | **erledigt** (`8806016`) | `PickingLeitstandController.CascadeDone/Preview`, `ProductionOrderBdeStatusRepository.SetIsDoneBdeForOrderNumberAsync`, `BdeBookingRepository.CountSubFasWithOpenBooking…` |
| D — Z4-Sweep (Articles/Info) + Z1-Regressionstest; **Z3 vertagt** | **erledigt** (`e9eb089`) | `ArticlesController`/`Views/Articles/Info.cshtml`, `FaMaterializationSyncServiceTests` (Z1); Z3 → [[2026-09-07-invariante-haupt-fertig-sub-erkennen]] |
| E — Testszenarien TS-69 + v1.35.0 + Changelogs | **erledigt** (`ea3932d`) | `docs/TESTSZENARIEN.md`, `AppVersion.cs` (Web+Service), `Views/Help/Changelog.cshtml` |

> **Status FA-Liste-Hierarchie:** alle Etappen A–E erledigt, Web **1243 grün / 1 Skip** / Service
> **232 grün** (qa-agent-Lauf 2026-09-07, frisch verifiziert). **`status: Testbereit`** gesetzt —
> wartet jetzt zusammen mit dem ganzen Bündel (Teile 1–8 + dieser Epic, ein Merge) auf **Schranke 2**
> (manueller Test + Merge durch den Menschen). Deploy: `web:true`, `service:false`,
> `migration:false` (aus dem echten Diff bestätigt). **Z3** (Invariante Haupt-fertig⇒Sub-fertig
> erkennen/melden) bewusst ins Backlog ausgelagert (Deploy-Fork Service; Fall ausgeschlossen).

### BOM-Bridge — Stückliste über die Repository-Schnittstelle (v1.36.0, selber Branch)

Ersetzt den Minimal-Guard: im hierarchischen Modus liefert `FaHierarchyBomRepository` (beide
Interfaces `IBomRepository`/`IBomCacheRepository`) die Stückliste direkt aus `FaHierarchyNode` — kein
Cache-Umweg, AKE bit-identisch (eigene Klasse). HauptFA → `FullStructure` (alle Ebenen, rekursiver
Positions-Pfad `3.7.2`, Sage-Position separat sichtbar, Kollisionen markiert), Sub-FA →
`DirectChildren`; Mengen = Sollmenge (`MengeIstAuftragsmenge`, `BomQuantityResolver`); Klasse-D-
Heuristiken (Coating/WorkStep/BomCache-Sync) hart abgeschaltet (`IHierarchicalModeReader`); Artikelinfo
HauptFA + Sub-FA; Menü „Kommissionierung" bündelt Picking-Workflow + Kommissionierlisten. Einstieg
`Data/Repositories/FaHierarchyBomRepository.cs`, `BomRepositoryMasterSwitch.cs`, `Program.cs` (Weiche),
`Models/BomKey.cs`/`BomScope.cs`, `Services/BomQuantityResolver.cs`, `IDEALAKEWMSService/Common/IHierarchicalModeReader.cs`.
Spec [[2026-09-08-bom-schnittstellen-bridge-hierarchisch-spec]], Aufgabe
[[2026-09-08-bom-schnittstellen-bridge-hierarchisch-umsetzung]], ADR
[[0013-bom-bridge-repository-schnittstelle-statt-cache-kopie]], Plan `docs/superpowers/plans/2026-09-08-bom-bridge.md` (Worktree).

| Task | Status | Code-Einstieg |
|---|---|---|
| 1 — Typen/Flag/Signaturwechsel | **erledigt** (`ea84284`) | `BomKey`, `BomScope`, `BomQueryResult.MengeIstAuftragsmenge`, `BomQuantityResolver`, 3 Aufrufer |
| 2 — `FaHierarchyBomRepository` | **erledigt** (`7a93c2f` + Fix `e2cceae`) | DirectChildren/FullStructure, Kollision `~n`, Waisen `W<n>`, Reverse-Lookup |
| 3 — Master-Weiche, Guard raus, Mapping | **erledigt** (`8877ef5`) | `BomRepositoryMasterSwitch`, `Program.cs:91-104`, `PickingController.Bom`/`ReadOnlyBomBuilder` |
| 4 — Klasse-D-Gates (Service) | **erledigt** (`e75bcf3`) | `IHierarchicalModeReader`, Coating/WorkStep/BomCache×2 |
| 5 — Views (Bom.cshtml, Artikelinfo, Nav) | **erledigt** (`62685c0`/`9f0f210`/`92de2b8`) | `Views/Picking/Bom.cshtml` (+`#column-config`), `ColumnDefinitions.Bom`, `ArticlesController`/`Info.cshtml`, `_Layout` |
| 6 — v1.36.0, TS-70 (TS-68 abgelöst), Hilfe, DI-Test | **erledigt** (`5b83ea0` + Fixwelle `5226352`) | `AppVersion.cs` ×2, `Help/Changelog.cshtml`, `Help/Index.cshtml`, `docs/TESTSZENARIEN.md`, `BomDiResolutionTests` |

> **Status:** `Testbereit` (qa-agent, 2026-09-08, Worktree-HEAD `5226352`) — Build 0 Fehler, Web
> **1266 grün + 1 skip**, Service **236 grün**, AK 1–16 gegen den echten Diff abgeglichen, Guard-Grep
> leer. Wartet mit dem ganzen Bündel (Teile 1–8 + FA-Liste-Hierarchie + BOM-Bridge, ein Merge) auf
> **Schranke 2** (manueller Test + Merge durch den Menschen). Deploy: `web:true`, `service:true`
> (Klasse-D-Gates + Konstruktor-Signaturänderung), `migration:false`.

### Materialisierung: fachliche Felder (v1.37.0, selber Branch)

Spec [[2026-08-20-materialisierung-fachliche-felder-spec]], Umsetzung
[[2026-09-09-materialisierung-fachliche-felder-umsetzung]], Entscheidung
[[0014-werkbank-datenhoheit-sage-fuehrend-mit-abweichungsmeldung]], Nachlese
[[2026-09-09-materialisierung-nachlese]], Changelog
[[2026-09-09-v1-37-0-ideal-materialisierung-fachliche-felder]].

Die Materialisierung schrieb bisher nur sieben Felder; seit die FA-Liste dieselben Auftraege zeigt,
blieben Kunde, Werkbank, Termine und Lack-Kennzeichen leer. Drei-Klassen-Trennung: **K1** (Kunde,
Konstruktions-/Fertigungs-/Liefertermin, Prio, AB-Nr., Montage-Abteilung) haengt am HauptFA und wird
**nur angezeigt**, nie materialisiert; **K2** (Werkbank, abgeleitetes Lack-Kennzeichen) wird
geschrieben; **K3** (Kommissionier-Status) bleibt leer.

| Baustein | Stand | Code-Einstieg |
|---|---|---|
| Statuszeilen eager anlegen (behebt den Abnahme-Fehler) | **erledigt** (`7ac06cb`) | `FaMaterializationSyncService.EnsureStatusRowsAsync` |
| Lack-Ableitung „selbst oder direktes Kind" | **erledigt** (`3b77f66`) | `FaMaterializationCoating.Derive` (rein, DB-frei) |
| Werkbank aus `Arbeitsbereich`, Variante B + drei Meldefaelle | **erledigt** (`3371ff1`) | `FaMaterializationSyncService.ApplyWorkplace`, `IUnknownWorkplaceState` |
| Lack-Kennzeichen schreiben (inkl. `IsCoatingDone`-Reset) | **erledigt** (`61e2d57`) | `ProductionOrderPickingStatusRepository.SetCoatingPartsAsync` |
| Kombigeraet-Erkennung, einmal je Lauf | **erledigt** (`8531a29`) | `FaHierarchySyncService.LogAmbiguousHauptFasAsync` |
| Kopfdaten gebuendelt laden | **erledigt** (`232c066`) | `FaHierarchyOrderInfoRepository.GetByHauptFaKeysAsync` |
| Kunde-Freitext + Spaltenfilter ueber den Auftragskopf | **erledigt** (`f5fe6f7` + `c63a0d9`) | `ProductionOrderRepository.BuildLeitstandQuery` / `ApplyLeitstandColumnFilter` |
| K1 im Anzeige-Modell, Termin-Kaskade auf den Gruppenwert | **erledigt** (`6418652` + `1a33b20`) | `ProductionOrdersController.MapItem`, `ProductionOrderListGroup` |
| Gruppen-Kopfzeile + Varianten-Tabelle | **erledigt** (`b6d3c0e`) | `Views/ProductionOrders/Index.cshtml` |
| v1.37.0, Changelog, Hilfe, TS-71 | **erledigt** (`7d5e067`) | `AppVersion.cs` ×2, `docs/TESTSZENARIEN.md` |
| Varianten-Tabelle klappt mit der Gruppe zu (Gesamt-Review) | **erledigt** (`c958edc`) | `Views/ProductionOrders/Index.cshtml` (`fa-liste-node-row`) |

> **Fünf stille Fehler im Lauf gefunden und behoben** — zwei davon erst beim Schreiben von Tests:
> fehlende Statuszeilen, weggefilterte Blattebene in der Lack-Ableitung, eine Entprellung, die ein
> wiederkehrendes Problem nie wieder gemeldet haette, ein **geratener** Beschichtungstermin bei
> leerem Kopfwert, und eine Kombigeraet-Varianten-Tabelle, die beim Zuklappen der Gruppe stehen
> blieb (Gesamt-Review). Keiner haette eine Fehlermeldung erzeugt.

> **Deploy-Vorbedingung:** Die Werkbaenke zu den vorkommenden Arbeitsbereichen **vor** dem Deploy
> anlegen — `ProductionWorkplaceId` ist ein Fremdschluessel, sonst bleibt die Werkbank im ersten Lauf
> leer (Zwei-Lauf-Ablauf). Deploy `web:true`, `service:true`, `migration:false`.

> **Status:** `Testbereit` (qa-agent, 2026-09-09, Worktree-HEAD `c958edc`) — Build 0 Fehler, Web
> **1284 grün + 1 skip**, Service **263 grün**, AK 1–13 gegen den echten Diff `64eac53..c958edc`
> abgeglichen, kein `SetValues`/`_ctx.Update`, keine Migration/SQL-Datei im Diff, beide
> `AppVersion.cs` auf 1.37.0, TS-71 (13 Szenarien) vorhanden. Wartet mit dem ganzen Bündel (Teile
> 1–8 + FA-Liste-Hierarchie + BOM-Bridge, ein Merge) auf **Schranke 2** (manueller Test + Merge
> durch den Menschen).
>
> ⚠️ **Teil-Rückbau (v1.43.0):** Die **Werkbank-Ableitung aus dem Arbeitsbereich** (K2-Werkbank, AK 9/10)
> wurde zurückgebaut — Arbeitsbereich = Zielort, keine Werkbank. Siehe v1.43.0 unten +
> [[2026-09-21-adr-0014-arbeitsbereich-ist-zielort-spec]]. K1/Lack/Statuszeilen/K3 bleiben gültig.

### FA-Liste ausbauen: Zeilenwerte, HauptFA-Zeile, Freigabe-Kaskade, Matchcode (v1.38.0, selber Branch)

Spec [[2026-09-10-fa-liste-ausbau-matchcode-spec]], Umsetzung
[[2026-09-10-fa-liste-ausbau-matchcode-umsetzung]], Bugfix (eingeschobener Sweep)
[[2026-09-10-table-filter-selektoren-nicht-skopiert-bug]], Changelog
[[2026-09-10-v1-38-0-ideal-fa-liste-ausbau-matchcode]]. Baut auf v1.37.0 (Materialisierung) auf,
ist aber eine eigene Spec — diese wird nicht wieder aufgerissen.

Vier fachlich unabhaengige Bloecke: Matchcode-Spalte (hausweit, materialisiert), Kopfdaten je Zeile
(C#-Postfilter statt SQL-Join), HauptFA als normale Zeile (Kopfzeile verschlankt), Freigabe-Kaskade
am Leitstand (widerruft die fruehere „keine Kaskade"-Festlegung aus v1.35.0).

| Baustein | Stand | Code-Einstieg |
|---|---|---|
| Spalte `ProductionOrder.Matchcode` + Migration 91 | **erledigt** (`6c1e2c0`) | `ProductionOrder.cs`, `SQL/91_AddProductionOrderMatchcode.sql` |
| Matchcode materialisiert (Anlege- + Update-Pfad, F1) | **erledigt** (`41fe040`) | `FaMaterializationSyncService.RunAsync` |
| Matchcode-Anzeige in fünf FA-Zeilen-Ebene-Listen | **erledigt** (`8cd2856`) | `ColumnDefinitions.cs`, je View `#column-config` + `<th>` |
| Matchcode-Filter null-sicher (F7), ohne `EF.Functions.Like` | **erledigt** (`b050910` + Fix `ab27ded`) | `ProductionOrderRepository.cs` (`BuildExtraInfoOrContains`) |
| K1-Kopfdaten je Zeile im Anzeige-Modell | **erledigt** (`b879eea` + `0ca28ce`) | `ProductionOrdersController.MapItem` |
| Kunde-/K1-Postfilter C# statt SQL-Join (inkl. Flachmodus-Fix) | **erledigt** (`93900a8` + `decbe56` + `3691cc3`) | `ProductionOrdersController.Index`, `PickingLeitstandController.Index` |
| Vier Zeilenspalten bedingt auf `Model.Hierarchical` | **erledigt** (`5809092`) | `Views/ProductionOrders/Index.cshtml` + `_ProductionOrderRow.cshtml` |
| Selektor-Sweep (zweiter Anzeigefehler, eingeschoben) | **erledigt** (`79ad96f` + `7093029`) | `table-filter.js`, `column-preferences.js`, `TableScriptScopedSelectorTests` |
| Wiederholungswerte unterdrückt, client-seitig | **erledigt** (`7e54a77` + Fix `1662ae9`) | `fa-liste-wiederholung.js`, `site.css` (`.fa-liste-repeat-hidden`) |
| HauptFA-Zeile gekennzeichnet, Kopfzeile verschlankt | **erledigt** (`a5e0922`) | `Views/ProductionOrders/_ProductionOrderRow.cshtml` |
| `SetReleaseForOrderNumberAsync` (Kaskade, atomar) | **erledigt** (`901e9db`) | `ProductionOrderPickingStatusRepository.cs` |
| `CascadeReleasePreview`/`CascadeRelease`, Picker-Pflicht unverändert | **erledigt** (`692956f`) | `PickingLeitstandController.cs` |
| Knopf + Dialog in der Gruppen-Kopfzeile | **erledigt** (`daf7168`) | `Views/PickingLeitstand/Index.cshtml` |
| v1.38.0, Changelog, Hilfe, TS-73 | **erledigt** (`24864e6`) | `AppVersion.cs` ×2, `docs/TESTSZENARIEN.md` |

> **AKE-Regression als harte Nebenbedingung:** Bei Master `false` bleibt die FA-Liste bit-identisch
> — die vier K1-Zeilenspalten sind **strukturell** (nicht nur inhaltlich) an `Model.Hierarchical`
> gebunden, der Matchcode bleibt dort leer, bis die hausinterne Sage-View-Erweiterung liefert
> (kein Fehler, eigener Backlog-Punkt).

> **Deploy:** `web:true`, `service:true` (Materialisierung ändert sich, nicht optional),
> `migration:true` (Migration 91, additiv, kein Backfill-Zwang — Bestandszeilen füllen sich erst
> beim nächsten Materialisierungs-Lauf).

> **Status:** `Testbereit` (qa-agent, 2026-09-10, Worktree-HEAD `24864e6`) — Build 0 Fehler, Web
> **1322 grün + 1 skip**, Service **265 grün**, AK 1–20 gegen den echten Diff `c958edc..24864e6`
> abgeglichen, genau eine neue Migration im Diff, TS-73 (20 Szenarien) + TS-72 (5 Szenarien,
> Selektor-Sweep) vorhanden. Wartet mit dem ganzen Bündel (Teile 1–8 + FA-Liste-Hierarchie +
> BOM-Bridge + Materialisierung, ein Merge) auf **Schranke 2** (manueller Test + Merge durch den
> Menschen). Nicht-blockierender Befund: Wiederholungswert-Unterdrückung in gestreiften/farbig
> markierten Zeilen nicht explizit in TS-73 benannt — in die manuelle Checkliste der Spec
> aufgenommen.

### PDF-Erzeugung fuer FaHierarchy-Druckdokumente (v1.39.0, Querschnitts-Baustein, selber Branch)

Spec [[2026-08-06-pdf-erzeugung-fahierarchy-druck-spec]], Umsetzung
[[2026-08-06-pdf-erzeugung-fahierarchy-druck-umsetzung]], Changelog
[[2026-09-14-v1-39-0-ideal-pdf-erzeugung-fahierarchy-druck]], Plan
`docs/superpowers/plans/2026-09-14-pdf-erzeugung-fahierarchy-druck.md`. Herausgeloest aus Teil 4
(Befund B-3): Infrastruktur, kein Merkmal eines einzelnen Dokuments.

Ein Dienst rendert aus dem bestehenden Print-HTML per Headless-Edge ein PDF je HauptFA; Teil 3
(Kommissionierliste) und Teil 4 (Beschichtungsauftrag) haengen sich an. Vormontage (Teil 5) bewusst
aussen vor (hat keine Druckansicht) → Backlog [[2026-09-14-vormontage-druckansicht]].

| Baustein | Stand | Code-Einstieg |
|---|---|---|
| `IPdfRenderService` (Singleton, Semaphor) + `IEdgeProcessRunner` | **erledigt** | `Services/PdfRenderService.cs`, `Services/EdgeProcessRunner.cs` |
| `PdfRenderStatus` (Start-Probe, Knopf-Sichtbarkeit) | **erledigt** | `Services/PdfRenderStatus.cs`, `Program.cs` (Registrierung + Refresh nach `Build()`) |
| `RazorViewRenderer` + `PdfFileNameBuilder` | **erledigt** | `Services/RazorViewRenderer.cs`, `Services/PdfFileNameBuilder.cs` |
| `Pdf`-Action + PDF-Knopf je Gruppe (beide Listen) | **erledigt** | `FaHierarchyKommissionierListenController`/`FaHierarchyBeschichtungController` + je `Index.cshtml` |
| Filter-Hinweis „Gefilterte Ansicht" im Druck-/PDF-Kopf | **erledigt** | beide `Print.cshtml` (`IsFiltered`) |

Migration: keine. `appsettings.json`-Sektion `PdfRender` (EdgePath/TimeoutSeconds/MaxConcurrent).
Betriebs-Vorbedingung Edge auf dem Web-Server (in die Buendel-Deploy-Notiz gezogen).

> **Status:** `Testbereit` (qa-agent, 2026-09-14). Build 0 Fehler, echter Edge-Smoke-Test erzeugt ein
> PDF; genaue Testzahlen im Spec-QA-Abschnitt. TS-74.1–74.14 vorhanden. Wartet mit dem ganzen Buendel
> auf **Schranke 2**. Gemessene Spec-Abweichung (msedge.exe-Launcher) in [[fallstricke]] §11.

### Matchcode im Artikelstamm (Article.Matchcode, v1.40.0, selber Branch)

Spec [[2026-09-13-matchcode-artikelstamm-spec]], Umsetzung [[2026-09-13-matchcode-artikelstamm-umsetzung]],
Changelog [[2026-09-15-v1-40-0-matchcode-artikelstamm]], Plan
`docs/superpowers/plans/2026-09-15-matchcode-artikelstamm.md`. Nachlese
[[2026-09-15-matchcode-nachlese]].

Der Matchcode ist artikelbezogen → wandert von `ProductionOrder.Matchcode` (v1.38.0, nie gemergt, sauber
zurueckgebaut) an `Article.Matchcode` und wird damit in jeder artikelbezogenen Suche sichtbar. **Widerruft
den Ort**, nicht die Anzeige/Suchbarkeit der fuenf FA-Listen (die lesen ihn jetzt aus `Article` per Join).

| Baustein | Stand | Code-Einstieg |
|---|---|---|
| `Article.Matchcode` (NVARCHAR(200), Migration 20260915094646) + Batch-Lookup | **erledigt** | `Article.cs`, `ArticleRepository.GetMatchcodesByArticleNumbersAsync`, `Services/MatchcodeLookup.cs` |
| Rueckbau `ProductionOrder.Matchcode` (Migration remove→add) | **erledigt** | Migration `AddProductionOrderMatchcode` entfernt; `FaMaterializationSyncService` (2 Zeilen weg) |
| Fuenf FA-Listen aus Article-Join + AK-14-Fehltreffer-Zaehler + Matchcode-Postfilter | **erledigt** | `ProductionOrderRepository.ProjectLeitstandRows`, `ProductionOrdersController` (`FaListComputedColumnKeys`), je Controller |
| Suche hausweit (Artikelstamm/Bestand/Fehlteile/Bedarfsmeldungen/FA-Fertigmeldung) | **erledigt** | `ArticleRepository`, `StockMovementRepository`, `PartRequisitionRepository`, `WarehouseRequisitionRepository`, `FaCompletionController` |
| Artikelinfo Anzeige + Exakt-Matchcode-Fallback | **erledigt** | `ArticlesController.Info` (`GetByMatchcodeExactAsync`) |
| Sage-Sync `KHKArtikel.Matchcode` | **erledigt** | `SageImportService.SyncArticlesAsync` (CAST 200, MAX je Artikel) |
| Typeahead ab 3 Zeichen (AK 15) | **erledigt** | `_Select2ArticlePartial`, `InboundBulk`, `WarehouseRequisitions/Edit` |

Deploy: Web + Service + Migration. OSEON/BOM-Komponentenebene bewusst aussen vor
([[2026-09-15-matchcode-nachlese]]).

> **Status:** `Testbereit` (qa-agent, 2026-09-15). Build 0 Fehler; genaue Testzahlen im Spec-QA-Abschnitt.
> TS-75.1–75.9 vorhanden. Wartet mit dem ganzen Buendel auf **Schranke 2**. B-1 (Coverage IDEAL) per
> Freigabe-Entscheidung „melden statt still" ausgeraeumt (AK 2 + AK 14). Gemessene Spec-Korrekturen
> (A1 Postfilter, A6 keine Articles-ViewConfig) in der Umsetzungsnotiz; Dauerwissen [[fallstricke]] §12.

### FA-Arbeitsgang-Erkennung aus der Struktur (v1.41.0, selber Branch) — **SUPERSEDED durch v1.44.0**

> **Superseded (2026-09-22):** fachlich ersetzt durch das Epic
> [[2026-09-21-ideal-bde-arbeitsgaenge-aus-struktur-spec]] (v1.44.0, siehe Abschnitt unten). Der hier
> gebaute `FaWorkStepStructureDetectionService` wurde im selben Buendel-Worktree zu
> `WorkOperationStructureDetectionService` umgebaut (Ziel `WorkOperation` statt `FaWorkStep`), bevor
> v1.41.0 je deployt war — **es wird nichts aus v1.41.0 einzeln gemergt/deployt.**

Spec [[2026-09-08-arbeitsgaenge-aus-arbeitsschritte-spec]], Umsetzung
[[2026-09-16-arbeitsgaenge-aus-arbeitsschritte-umsetzung]], Changelog
[[2026-09-16-v1-41-0-ideal-fa-arbeitsgang-struktur]]. **Voraussetzung fuer Teil-8-UAT** (seit dem
Klasse-D-Gate v1.36.0 bekamen IDEAL-Auftraege gar keine `FaWorkSteps`). Dritte Struktur-Ableitung der
Familie (nach Werkbank aus `Arbeitsbereich` und Lack aus `Beschichtet`).

| Baustein | Stand | Code-Einstieg |
|---|---|---|
| Eigener `FaWorkStepStructureDetectionService` (kein parametrisierter Bestandsservice) | **erledigt** | `IDEALAKEWMSService/Services/FaWorkStepStructureDetectionService.cs` |
| DirectChildren-Scope + exakt `WorkStep.Code`-Match + Nur-hinzufuegen | **erledigt** | `DetectAsync` |
| Unbekannte Token melden (S1-Sammelmail), nicht anlegen | **erledigt** | `IUnknownWorkStepTokenState` (eigene Singleton) |
| `FaWorkStepSources.Struktur`, Service-Key, SyncLog, Doppel-Gate im SyncWorker, 2× DI | **erledigt** | `FaWorkStep.cs`, `ServiceSettingDefinitions.cs`, `SyncLogServices.cs`, `SyncWorker.cs`, `Program.cs` |
| 14 Unit-Tests + v1.41.0 + Changelog/Hilfe/README/TS-76 | **erledigt** | `FaWorkStepStructureDetectionServiceTests`, `AppVersion.cs` ×2, `docs/TESTSZENARIEN.md` |

> **Deploy:** Web + Service, **keine Migration**. Zwei-Lauf-Ablauf (S4): **Deploy zuerst**, erster
> DryRun-Lauf meldet die Kuerzel, Mensch legt je Kuerzel einen `WorkStep` mit `Code = Token` auf
> `/WorkSteps` an, naechster Lauf legt die Arbeitsgaenge an. Keine Vorab-Erhebung noetig. Dauerwissen
> [[fallstricke]] §13 (Doppel-Kontext).
>
> **Status:** `Testbereit` (qa-agent, 2026-09-16). Build 0 Fehler; Web 1391+1skip/0 Fehler,
> Service 277/0 Fehler (+14 neu). AK 1–12 gegen den echten Diff abgeglichen, TS-76 (10 Szenarien)
> geprueft; `testszenarien-index.md` fehlte trotz Notiz — vom qa-agent nachgetragen. Wartet mit dem
> ganzen Buendel auf **Schranke 2**.

### IDEAL: BDE-Arbeitsgänge (WorkOperation) aus Struktur + Werkbank-Anlage aus Sage (v1.44.0, Epic, selber Branch)

Spec [[2026-09-21-ideal-bde-arbeitsgaenge-aus-struktur-spec]] (Epic, zwei Etappen), Umsetzung
[[2026-09-22-ideal-bde-arbeitsgaenge-aus-struktur-umsetzung]]. **Ersetzt v1.41.0** (Abschnitt oben).
Setzt den ADR-0014-Rueckbau (v1.43.0) voraus. Kern: ein Arbeitsgang IST bei IDEAL ein Sage-Arbeitsplatz,
und ein Sage-Arbeitsplatz IST die Werkbank — das BDE-Terminal bucht gegen `WorkOperation`, nicht
`FaWorkStep`.

| Baustein | Stand | Code-Einstieg |
|---|---|---|
| (a) `ProductionWorkplace`-Anlage aus Sage `KHKPpsArbeitsplaetze` (Sage fuehrend, Nummer/Name/Kuerzel) | **erledigt** `8a6fd0c4` | `ProductionWorkplaceSyncService`, `SageArbeitsplatzReader`, Migration `93`, Felder `SageArbeitsplatznummer`/`ArbeitsschrittCode` (eindeutiger gefilterter Index) |
| (a) 3 ServiceSettings (Enabled/Mandant/Ausschlussliste) + Standorteinstellungen-Gruppe + SyncWorker-Doppel-Gate + UI read-only | **erledigt** `8a6fd0c4`/`bc25ea21` | `ServiceSettingDefinitions.cs`, `StandortSettingsCatalog.cs`, `SyncWorker.cs`, `Views/ProductionWorkplaces/*` |
| (b) 2c-Umbau `FaWorkStepStructureDetectionService` → `WorkOperationStructureDetectionService` (Ziel `WorkOperation`, Katalog `ProductionWorkplace.ArbeitsschrittCode`) | **erledigt** `6ba4e903` | `WorkOperationStructureDetectionService.cs`, `IUnknownArbeitsschrittTokenState`, Key `Sync:WorkOperationStructureDetectionEnabled` |
| (b) Kuerzel-Mehrdeutigkeit gemeldet, Ausschlussliste-Kuerzel weder AG noch Meldung, `FaWorkStepSources.Struktur` entfernt | **erledigt** `6ba4e903` | `WorkOperationStructureDetectionService.DetectAsync` |
| (c) Existenz-Check-Fix `BdeDefaultWorkOperationService` (OperationNumber "01" statt Name); `BdeScanResolver` UNVERAENDERT; Terminal-Routing schon ueber `ProductionWorkplaceId` | **erledigt** `6ba4e903` | `BdeDefaultWorkOperationService.cs` |
| Tests + v1.44.0 + Changelog/Hilfe/README/TS-77 | **erledigt** | `WorkOperationStructureDetectionServiceTests` (13), `ProductionWorkplaceSyncServiceTests` (6), `BdeDefaultWorkOperationServiceTests` (+AK16), `AppVersion.cs` ×2, `docs/TESTSZENARIEN.md` |

> **Deploy:** Web + Service + **Migration 93** (zwei nullable Spalten + eindeutiger gefilterter Index,
> nicht daten-destruktiv). Erster scharfer Baustein-(a)-Lauf legt bei IDEAL ~60 Werkbaenke neu an —
> **DryRun vor dem scharf schalten** (Doppel-Gate: Master + je Toggle). Reihenfolge der Toggles:
> erst `Sync:ProductionWorkplaceSyncEnabled` (Katalog), dann `Sync:WorkOperationStructureDetectionEnabled`.
>
> **Status:** wartet auf qa-agent → `Testbereit`, dann mit dem ganzen Buendel auf **Schranke 2**.
> AK 10 (Ausschlussliste), AK 16-Terminal, AK 20 (Doppelanlage/UNIQUE) und alle Sage-Reads sind
> **Manual-UAT** (nicht InMemory-testbar) — siehe TS-77.

### FA-Struktur: erkannte Arbeitsgänge je (Sub-)FA im Modal (v1.45.0, selber Branch)

Spec [[2026-09-23-fa-struktur-arbeitsgaenge-anzeige-spec]], Umsetzung
[[2026-09-24-fa-struktur-arbeitsgaenge-anzeige-umsetzung]], Changelog
[[2026-09-24-v1-45-0-fa-struktur-arbeitsgaenge-anzeige]]. Setzt v1.44.0 (Struktur-Erkennung) voraus. Read-only.

| Baustein | Stand | Code-Einstieg |
|---|---|---|
| Existenz-Check beim Seitenaufbau (Knopf nur bei ≥ 1 AG) + Modal-Endpunkt je HauptFA | **erledigt** `49298f35`/`6064263f` | `FaHierarchyController.Index`/`WorkOperations`, `IWorkOperationRepository.GetOrderNumbersWithWorkOperationsAsync`/`GetByOrderNumberWithWorkplaceAsync` (gleicher Schlüssel `OrderNumber == HauptFA`) |
| Modal (alle Sub-FAs inkl. Lücken + Zählzeile), Laden beim Öffnen | **erledigt** `6064263f` | `Views/FaHierarchy/_FaWorkOperationsModal.cshtml`, `FaWorkOperationsViewModel.Create`, `wwwroot/js/fa-hierarchy-tree.js` (`openWorkOperations`) |
| Tests + v1.45.0 + Changelog/Hilfe/TS-78; Review-Fix Knopf/Modal-Schlüssel | **erledigt** `821d82a3` | `WorkOperationRepositoryFaStructureTests` (4), `FaWorkOperationsViewModelTests` (1) |

> **Deploy:** nur Web, keine Migration. **Status:** QA → Schranke 2 mit dem ganzen Bündel.

### Kommissionierung nur am HauptFA — „Alle Ziele“, Rückbau Freigabe-Kaskade (v1.46.0, selber Branch)

Spec [[2026-09-25-kommissionierung-nur-hauptfa-spec]], Umsetzung
[[2026-09-25-kommissionierung-nur-hauptfa-umsetzung]], Changelog
[[2026-09-25-v1-46-0-kommissionierung-nur-hauptfa]]. Anlass [[2026-09-13-kommissionierung-nur-hauptfa]].
Folgenotizen: [[2026-09-25-picking-warteschlange-gruppierung-rueckbau]], [[2026-09-25-leitstand-subfa-readonly-stueckliste]].

| Baustein | Stand | Code-Einstieg |
|---|---|---|
| Eine Sub-FA-Definition + Warteschlange nur HauptFA + Bulk-Skip beidseitig | **erledigt** `a30e7647`/`ffb1572e` | `ProductionOrder.IsHauptFa`/`IsSubFa`/`IsSubFaOf`, `ProductionOrderPickingStatusRepository` (4 Queue-Abfragen, `SetReleaseBatchAsync`), `BomScopes.ForOrder`, `HierarchicalDataExistsAsync` |
| Server-Guards + Zeilen Leitstand/FA-Liste auf HauptFA beschränkt | **erledigt** `cba1be97`/`b9991917` | `PickingController.Bom`, `PickingLeitstandController.ToggleRelease`/`BulkRelease`, `_PickingLeitstandRow`/`_ProductionOrderRow` |
| Rückbau Freigabe-Kaskade (aus [[2026-09-10-fa-liste-ausbau-matchcode-spec]] Block 4) | **erledigt** `ee583ed7` | grep `CascadeRelease\|SetReleaseForOrderNumber\|CountReleasedByOrderNumber` = 0 |
| „Alle Ziele“ `!(leer)` im Komm.-Ziel-Filter + Druck-Leerzustand | **erledigt** `6a6adee6`/`4467b894` | `Views/Picking/Bom.cshtml` (`bomMatchesFilter`, `setupKommissionierzielDropdown`, `btnPrintBom`) |
| Normalisierung Profil-Standardfilter + Relevanzregel extrahiert | **erledigt** `f40053d3`/`855f7310` | `KommissionierzielFilterWert.Normalize`, `KommissionierRelevanzFilter.IsRelevant` |
| v1.46.0, Changelog, Hilfe, TS-79, TS-73-Rückbau-Vermerk | **erledigt** `50582f81`/`c61cb796`/`30c9c20e` | `docs/TESTSZENARIEN.md` TS-79 |

> **Deploy:** nur Web, keine Migration. **Einmaliger SQL-Lauf nur im Testsystem** (freigegebene Sub-FAs
> zurücksetzen, `SELECT COUNT(*)` vorab) — siehe Spec. **Status:** Testbereit (QA 2026-09-25, Build +
> 1684 Tests grün) → Schranke 2 mit dem ganzen Bündel.
> Razor/JS-Verhalten („Alle Ziele“, Exklusivität, Druck-Leerzustand) und die SQL-Übersetzung von
> `IsHauptFa` sind **Manual-UAT** (TS-79).

### Kommissionierliste: Summiert als Standard, Druck/PDF nur sichtbare Spalten (v1.47.0, selber Branch)

Spec [[2026-09-25-kommissionierliste-summierung-pdf-spec]], Umsetzung
[[2026-09-28-kommissionierliste-summierung-pdf-umsetzung]], Changelog
[[2026-09-28-v1-47-0-kommissionierliste-summierung-pdf]]. Anlass [[2026-09-23-kommissionierliste-summierung-pdf]].
Setzt v1.46.0 ([[2026-09-25-kommissionierung-nur-hauptfa-spec]]) voraus. Überholt AK N2d von [[2026-07-29-standort-ideal-teil-3-spec]].

| Baustein | Stand | Code-Einstieg |
|---|---|---|
| Summiert: KW optional, Matchcode/Hauptlagerplatz, Gruppen-Paging je HauptFA, Umschalter-Query | **erledigt** `06d611c7` | `KommissionierListenService.AggregateSummiert`, `ToggleSharedQueryKeys`/`BuildToggleQuery` |
| ViewKey `FaHierarchyKommissionierSummiert` (6 Spalten) registriert | **erledigt** `e4c440cf` | `ColumnDefinitions.FaHierarchyKommissionierSummiert` |
| Druck/PDF: sichtbare Spalten beim Klick (`visibleColumns`), Server-Rückfall auf Präferenz; `PrintSummiert`/`PdfSummiert` | **erledigt** `02817d79`/`ddd85403` | `FaHierarchyKommissionierListenController.ResolveVisibleColumnsAsync`, `wwwroot/js/print-visible-columns.js`, `Views/FaHierarchyKommissionierListen/PrintSummiert.cshtml` |
| Menü → Summiert; Summiert gruppiert, 6 Spalten | **erledigt** `ddd85403` | `_Layout.cshtml`, `Summiert.cshtml` |
| Drift-Guard Vier-Stellen-Konsistenz | **erledigt** `980108b8` | `IdealAkeWms.Tests/Views/KommissionierSpaltenKonsistenzTests.cs` |
| v1.47.0, Changelog, Hilfe, TS-80 | **erledigt** `e380315c` | `docs/TESTSZENARIEN.md` TS-80 |

> **Deploy:** nur Web, keine Migration. **Vor der Abnahme:** Einheiten-Prüfschritt TS-80.1 auf der
> Sage-DB IDEAL ([[fallstricke]] §18). **Status:** Testbereit (QA 2026-09-28, Build + 1435 Web-Tests +
> 268 Service-Tests grün) → Schranke 2 mit dem ganzen Bündel. Druck-/PDF-Spalten, Gruppen-Darstellung
> und Umschalter sind **Manual-UAT** (TS-80).

### AKE-Hotfix 1.30.1 uebernommen — Vorwaerts-Merge main → Buendel (v1.47.1, selber Branch)

Merge-Commit `b877e05a` (Buendel), Quelle `main` `f89c0124`. Spec [[2026-09-28-ake-hotfix-stueckliste-druck-lagerbestellung-ist-spec]]
(Abschnitt „Uebertrag ins Buendel"), Changelog [[2026-10-05-v1-47-1-buendel-ake-hotfix-uebernommen]], Merge-Protokoll
[[2026-10-05-vorwaerts-merge-ake-hotfix-ins-buendel]]. **Buendel-Stand jetzt v1.47.1.**

| Baustein | Stand | Code-Einstieg |
|---|---|---|
| Stuecklisten-Druck per POST, nach der Leerzustand-Sperre | **uebernommen** `b877e05a` | `Bom.cshtml` (`btnPrintBom`), `PickingController.RenderPrintBomAsync` (Buendel-Rumpf) |
| Lagerbestellung Ist-Menge bestaetigen (Pflichtpruefung, Autosave NULL, Laengen-Guard) | **uebernommen** `b877e05a` | `WarehouseRequisitionRepository.CloseAsync`, `WarehousePickingController`, `Details.cshtml` |
| Pflicht-Nachzug 4 Buendel-Tests `GetMissingPartsAsync_*` (`decimal?`) | **erledigt** `b877e05a` | `WarehouseRequisitionRepositoryTests.cs` |

> **Status:** im Buendel, wartet mit dem ganzen Buendel auf Schranke 2 (TS-5.11, TS-18.10 am IDEAL-System).
> Einmal-Skript `SQL/Einmalig/…` auf IDEAL erst NACH dem Buendel-Deploy, genau einmal.

### Bugfix B-ABN-1: Komm.-Ziel-Auswahl der Stückliste übersteht F5 (v1.47.2, selber Branch)

Befund der Browser-Abnahme [[2026-10-06-ts-79-browser-testbericht]] zu [[2026-09-25-kommissionierung-nur-hauptfa-spec]],
Changelog [[2026-10-06-v1-47-2-stueckliste-kommissionierziel-f5]]. Commit `1df7665c`, nur `Views/Picking/Bom.cshtml`
(Speichern über `window.setColumnFilter`, Sitzung vor Profil-Standard). Wächter `BomKommissionierzielPersistenzTests`.
**Bündel-Stand jetzt v1.47.2.**

> **Status:** behoben, QA-Review ohne Critical/Important; Abnahme offen (TS-79.6 / Browser-Schritt B12 wiederholen).

### Stückliste: Komm.-Ziel-Filter + gespeicherter Standardfilter + Badge (v1.42.0, selber Branch)

Spec [[2026-09-18-stueckliste-kommissionierziel-filter-spec]], Umsetzung
[[2026-09-21-stueckliste-kommissionierziel-filter-umsetzung]], Changelog
[[2026-09-21-v1-42-0-stueckliste-kommissionierziel-filter]]. Anlass: [[2026-09-13-kommissionierung-nur-hauptfa]]
(Vollstruktur an der HauptFA-Stückliste).

| Baustein | Stand | Code-Einstieg |
|---|---|---|
| Feld `User.DefaultFilterBomKommissionierziel` + Migration 92 + FreshInstall | **erledigt** | `User.cs`, `SQL/92_*.sql` |
| Durchreichung VM/Controller/BomViewModel (1:1 Vorlage `DefaultFilterBomDescription1`) | **erledigt** | `AccountController`/`UsersController`/`PickingController` |
| Einstellungsfeld nur bei Master `true` (neue `IServiceSettingRepository`-Verdrahtung in beiden Controllern) | **erledigt** | `Users/Edit.cshtml`, `Account/Profile.cshtml`, `HierarchicalMaster` |
| Select2-Dropdown (nur hierarchisch, DISTINCT-Items, OR) über verstecktem Input | **erledigt** | `Bom.cshtml` `setupKommissionierzielDropdown` |
| Badge + Ein-Klick-Reset für ALLE VIER Standardfilter + benannter Leerzustand + Auto-Aufklappen | **erledigt** | `Bom.cshtml` `renderDefaultFilterBadges`/`checkKommissionierzielEmptyState`/`expandAncestorsOfMatching` |
| Druck-Durchschlag (PrintBomItem+Mapping, ShowCol-Blöcke, colNames col-key→Label, „Gefilterte Ansicht") | **erledigt** | `PrintBom.cshtml`, `BomViewModels.cs`, `PickingController` Print-Mapping |
| v1.42.0 + Changelog + TS-70-Abschnitt + Controller-Test-Fixes | **erledigt** | `AppVersion.cs` ×2, `docs/TESTSZENARIEN.md` |

> **Deploy:** Web + Migration 92 (nullable Users-Spalte), kein Service. Dauerwissen [[fallstricke]] §14
> (Client-Mode-Dropdown über verstecktem Input; getActiveFilters liefert col-keys).
>
> **Status:** `Testbereit` (qa-agent, 2026-09-21 — Build/Test gruen, AK-Abgleich gegen echten Diff
> `1bef63d~1..95b611b`). Client-Mode = Bildschirm-Interaktion Manual-UAT, wartet mit dem Buendel auf
> Schranke 2.
> Wartet mit dem ganzen Bündel auf **Schranke 2**.

### Rückbau: Werkbank-aus-Arbeitsbereich-Ableitung (v1.43.0, ADR-0014-Korrektur, selber Branch)

Spec [[2026-09-21-adr-0014-arbeitsbereich-ist-zielort-spec]], Umsetzung
[[2026-09-22-adr-0014-arbeitsbereich-ist-zielort-umsetzung]], Changelog
[[2026-09-22-v1-43-0-adr-0014-arbeitsbereich-rueckbau]]. **Vor** der BDE-Spec umzusetzen.
Der `Arbeitsbereich` ist ein **Zielort**, keine Werkbank → die v1.37-Ableitung (nie produktiv)
zurückgebaut, bevor die echten IDEAL-Werkbänke angelegt werden.

| Baustein | Stand | Code-Einstieg |
|---|---|---|
| Werkbank-Ableitungsblock aus `FaMaterializationSyncService` entfernt (Lookup/`ApplyWorkplace`/Mail/Counter/Ctor-Param) | **erledigt** | `FaMaterializationSyncService.cs` |
| `MaterializationSourceOrder.Arbeitsbereich` entfernt | **erledigt** | `FaMaterializationPlanner.cs` |
| `IUnknownWorkplaceState` + 2 Test-Dateien gelöscht, `Program.cs`-DI entfernt | **erledigt** | (`IUnknownWorkStepTokenState` bleibt) |
| AK 10: manuelle Werkbank (`FaCompletion.SetWorkplace`) überlebt den Sync | **erledigt** (Verhaltensänderung, gewollt) | — |
| v1.43.0 + Changelog + TS-71-Rückbau (71.6–71.9 markiert, 71.14/71.15 neu) + ADR-0014-Nachtrag + [[fallstricke]] §15 | **erledigt** | `AppVersion.cs` ×2 |

> **Deploy:** Web + Service, keine Migration. **Status:** `Testbereit` (qa-agent, 2026-09-22 —
> Build/Test grün, AK-Abgleich gegen echten Diff `2200c47..cddb4f9`). Build 0 Fehler, **Web 1391 +1
> skip, Service 263** (−14 gelöschte Workplace-Tests). Wartet mit dem Bündel auf **Schranke 2**.

### Beschichtungsauftrag summiert als Standard (v1.48.0, per Fast-Forward im Bündel)

Spec [[2026-10-06-beschichtungsauftrag-ausbau-teil-1-spec]] (Teil 1 von 4, [[2026-10-06-beschichtungsauftrag-ausbau-uebersicht]]),
Umsetzung [[2026-10-09-beschichtungsauftrag-ausbau-teil-1-umsetzung]], Changelog [[2026-10-09-v1-48-0-beschichtungsauftrag-summiert]].
Zweig `feature/2026-10-06-beschichtungsauftrag-ausbau-teil-1`, abgezweigt vom Bündel @ `f102fbf5` (v1.47.5); 2026-10-09 per Fast-Forward ins Bündel übernommen (`cc86d373`).

| Baustein | Stand | Code-Einstieg |
|---|---|---|
| Aggregation HauptFA + Artnr, Maße-Invariante, Gruppen-Paging, Kopf je HauptFA | **erledigt** `850258e3` | `BeschichtungsauftragService.AggregateSummiert`/`BuildSummiertAsync`/`SummiertColumnMap` |
| Actions `Summiert`/`PrintSummiert`/`PdfSummiert`, gemeinsame Druckspalten-Auflösung | **erledigt** `578aec6b` | `FaHierarchyBeschichtungController`, `Services/PrintVisibleColumns.cs`, `ColumnDefinitions.FaHierarchyBeschichtungSummiert` |
| Views, Umschalter, Kopf-Partials, Navigation → Summiert, Wächter | **erledigt** `c226a465` | `Views/FaHierarchyBeschichtung/Summiert.cshtml`, `PrintSummiert.cshtml`, `_BeschichtungKopf*.cshtml` |
| v1.48.0, Changelog, Hilfe, TS-81; Test Umschalter-Query | **erledigt** `8d9cefd2`, `cc86d373` | `docs/TESTSZENARIEN.md` TS-81 |

> **Deploy:** nur Web, keine Migration. **Status:** Testbereit (QA 2026-10-09, Build + 1474 Web- + 272 Service-Tests grün)
> → Schranke 2 mit dem Bündel. Vor der Abnahme Prüfschritt Mengeneinheit TS-81.1.

### Briefkopf auf dem Beschichtungsauftrag (v1.49.0, direkt im Bündel)

Spec [[2026-10-06-beschichtungsauftrag-ausbau-teil-2-spec]] (Teil 2 von 4), Umsetzung
[[2026-10-09-beschichtungsauftrag-ausbau-teil-2-umsetzung]], Changelog [[2026-10-09-v1-49-0-briefkopf-beschichtungsauftrag]].

| Baustein | Stand | Code-Einstieg |
|---|---|---|
| Vorlage laden/bereinigen/einbetten, Ersatz-Kopf, Grenzen; Einstellung „Briefkopf-Vorlage (Pfad)" | **erledigt** `1c560d30`, `020bf8af` | `Services/BriefkopfVorlageService.cs`, `AppSettingKeys.BriefkopfVorlagePfad`, `StandortSettingsCatalog` |
| Kopf-Partials in Einzel- und Summiert-Druck (nur Seite 1), Empfänger-Regel | **erledigt** `1cda91dc` | `Views/Shared/_PrintBriefkopf*.cshtml`, `PrintBriefkopfModel` |
| Muster `Branding/briefkopf.html` + Logo, README, Hilfe, v1.49.0, TS-82 | **erledigt** `742f7855` | `Branding/` (Repo-Wurzel, nicht im Publish) |

> **Deploy:** nur Web, keine Migration; **manuell** `Branding\` neben dem Publish-Ordner + Einstellung setzen (ohne:
> Ersatz-Kopf). **Status:** Testbereit (QA + Nachlauf 2026-10-09, Build + 1521 Web- + 272 Service-Tests grün) → Schranke 2
> mit dem Bündel. Offen: [[2026-10-09-briefkopf-seitenzaehlung]], [[2026-10-09-briefkopf-csp-und-bereinigung]].

### Hardware-Barcodescanner: Tastatur-Scan mit Präfix (v1.66.0, direkt im Bündel)

Spec [[2026-10-15-scan-handler-hardware-scanner-spec]], Umsetzung [[2026-10-15-scan-handler-hardware-scanner-umsetzung]],
Changelog [[2026-10-21-v1-66-0-hardware-barcodescanner]].

| Baustein | Stand | Code-Einstieg |
|---|---|---|
| Kern + DOM: Präfix `~09` (Variante B), Zeitgrenze, Replay, Ereignis `wms:hardware-scan`, Regel (b)/(d)/Hinweis | **erledigt** `cb49f262` | `wwwroot/js/hardware-scanner.js` |
| Ziel-Register + Regel (a) über `processScannedValue` (alle 14 Scan-Seiten automatisch) | **erledigt** `cb49f262` | `barcode-scanner.js` `_hwScanTargets` |
| BDE-Terminal: `data-hw-scan-ziel` an `scanOperator`/`scanFaAg` | **erledigt** `cb49f262` | `Views/BdeTerminal/Index.cshtml` |
| Standort-Schalter (3 AppSettings, Gruppe „Hardware-Scanner“), Geräte-Abschalter „Mein Profil“ | **erledigt** `cb49f262` | `StandortSettingsCatalog`, `_Layout`, `Account/Profile.cshtml` |
| Hilfe, Changelog, README, v1.66.0, TS-99 | **erledigt** `cb49f262` | `Views/Help/Index.cshtml` |
| QA-Fix: ausgeblendetes Fokusfeld ist kein freies Feld (BDE nach Anmeldung) | **erledigt** `789d0a78` | `hardware-scanner.js freiesFeld` |
| v1.66.1 N-1: offenes Kamera-Fenster der Stückliste-Artikelerfassung nimmt den Scan an (opt-in `data-hw-scan-kamera-ziel`) | **erledigt** `c72be07e` | `barcode-scanner.js hwScanKameraZiel`, `Picking/Bom.cshtml` |
| v1.66.1 N-2: Zeichenabstand 10–500 mit Meldung, leer = 50 (Server `StandortField.Min/Max/EmptyResetsToDefault`, Client `HwScanCore.abstand`) | **erledigt** `c72be07e` | `StandortEinstellungenController.Save` |
| QA-Fix v1.66.1: Kamera-Treffer eines gestoppten Scanners ignorieren (Race Hardware-Scan/laufender Decode) | **erledigt** `1d78e7f5` | `barcode-scanner.js startCameraScanner` |
| v1.66.2 (Nachtrag 3): Kamera-Fenster als Scan-Ziel an allen 22 Kamera-Knöpfen der Seiten mit mehr als einem Scan-Feld; Wächter mit vollständiger Liste | **erledigt** `543edbe2`, `845e38c9` (qa) | `data-hw-scan-kamera-ziel` in 10 Views, `HardwareScannerTests` |
| v1.67.1 (Nachtrag 4): Fokus-Eingabe-Geräte (Netum) – Präfix am eingefügten Text, Feld „Handscanner“ im Kamera-Fenster, Scanner-Diagnose in Mein Profil | **erledigt** `eb4b28ea`, `a2651019` (qa) | `HwScanCore.createText`, `hardware-scanner.js` (input/beforeinput), [[2026-10-23-v1-67-1-hardware-scanner-fokus-eingabe]] |
| v1.67.2 (Nachtrag 5): Scannen ohne Feld, Pilot nur Einbuchung – unsichtbares Scan-Feld, Zuordnung nach Inhalt (Artikel-QR / Lagerplatz / Artikelnummer), Profil-Schalter | **erledigt** `20b573de`, `a9462036` (qa) | `HwScanCore.zuordnung`, `hwScanOhneFeld` (barcode-scanner.js), `#hwScanOhneFeld` in Inbound, [[2026-10-23-v1-67-2-einbuchung-scannen-ohne-feld]] |
| v1.68.1 (Nachtrag 6): sichtbares Scan-Feld „Scan bereit“ (grün/grau) statt unsichtbarem, Diagnose mit aktivem Element und Fokuswechsel | **erledigt** `69bb4fd1`, `8540c4d6` (qa) | `#hwScanOhneFeld`, `.hw-scan-ohne-feld`, [[2026-10-27-v1-68-1-scan-feld-sichtbar]] |
| v1.71.1 (Nachtrag 7): ein Tipp aktiviert das Scan-Feld – kein `inputmode` im Markup, `inputmode="none"` erst 300 ms nach Geste, grün nur nach Tipp, Skript-Fokus grau, volle Kartenbreite | **Testbereit** (QA 2026-11-03, `9ca87f7c` + QA-Fix `6969620a` Fokusring 8:1 + Waechter Setz-Ort; DEV AKE+IDEAL v1.71.1), wartet Schranke 2 (Netum TS-99.19 a7–h7) | `hardware-scanner.js` Block „Scannen ohne Feld“, `.hw-scan-bereit`, [[2026-11-03-v1-71-1-scan-feld-tipp-aktiviert]] |
| v1.72.0: Lagerplatz-Etikett 93 × 55 mm (Rollendrucker) als Standort-Schalter (Gruppe „Druck“, Default A4) | **Testbereit** `48349786` (QA 2026-11-06) | `Views/StorageLocations/PrintLabels.cshtml` (`labels-rolle`), `AppSettingKeys.LagerplatzEtikettRolle93x55`, [[2026-11-06-lagerplatz-etikett-rolle-und-vormontage-felder-spec]] |
| v1.72.0: Vormontage-Listen neue Spalten Arbeitsbereich, HauptFA (Summiert), Kunde, FE-Termin, Bemerkung PN, Fertigungsinfo PN; Summiert Vorfilter je Position | **Testbereit** `43a3988f` + QA `d91b1d1d` (2026-11-06) | `VormontageService.BuildPositionsSpalten`/`BuildEinzelnColumnMap`, [[2026-11-06-v1-72-0-lagerplatz-etikett-rolle-vormontage-felder]] |
| v1.73.0 (Epic): Sage-Abgleiche auf SData, Quellen-Weiche View/SData/Vergleich je Abgleich (Default View), E0–E4b, E6a–E7 | **Testbereit** (QA-Fix `c8c6b210`, QA-Wiederholung 2026-11-06) `7fdcf656`…`90a3555f` | `IDEALAKEWMSService/Services/Sdata/`, [[0019-quellen-weiche-view-sdata-je-abgleich]], [[2026-11-06-sage-sdata-stammdaten-sync-spec]] |
| v1.73.0: Kreditoren/Debitoren-Stamm mit Pflege-Oberfläche + Lese-Abgleich Kontokorrent, Artikel-Lieferanten (E8a–c) | **Testbereit** `276a9077`, `30ee0f1d`, `9bdbeaed` | `CreditorsController`, `DebitorsController`, `KontokorrentSyncService`, [[2026-11-06-v1-73-0-sage-sdata-stammdaten-sync]] |
| v1.73.0: Übergabe WMS→Sage vorbereitet (Status + Outbox, ohne Senden) für Lagerplätze/BDE-Mitarbeiter (E9) | **Testbereit** `e6821035` | `SageStammdatenOutbox`, `LagerplatzSyncService` |
| v1.75.0: Fehlteile „Nur meine Werkbänke“ – Trefferzahl und Tab-Badges über alle Seiten (Filter in SQL vor Zählen/Paging) | **Testbereit** `e3071344`, `1f55f35b` | `WarehouseRequisitionRepository.GetMissingPartsAsync(workplaceIds)`, `MissingPartsController`, [[2026-11-07-fehlteile-nur-meine-werkbaenke-trefferzahl-spec]] |
| v1.76.0: Bestandsübersicht übernimmt die FA aus dem Artikel-QR nur mit `QrMitFaNummer` | **Testbereit** `2fafd073` | `StockOverviewController` (`ViewBag.QrMitFaNummer`), [[2026-11-07-bestandsuebersicht-qr-fa-uebernahme-fest-spec]] |
| v1.77.0 (Epic): Einstellungen mit festem Wertebereich als Auswahl, Wirkungsbeschreibung, Serverprüfung, Startprüfung (ADR 0020) | **Testbereit** `b220fcba`…`bd178dc4` | `AppSettingDefinitions`, `ServiceSettingDefinitions`, `SettingValidator`, `_SettingField.cshtml`, `Einstellungspruefung`, [[2026-11-07-einstellungen-auswahlfelder-und-beschreibung-spec]] |
| v1.78.0: EAN von Einkaufsartikeln scannen (`/api/articles/by-ean`, Auswahlfenster bei 2–5 Treffern) | **Testbereit** `8534da23`, `8bf76aca` (QA E2/E3) | `ArticlesApiController.GetByEan`, `barcode-scanner.js` (`_eanAuswahl`), [[2026-11-07-ean-einkaufsartikel-scan-spec]] |
| v1.79.0: Bereinigungen – Sperr-/TLS-Hinweis im Feld, Ausweich-Lagerplatz ohne Zeile sichtbar, Rückmelde-Schalter entfernt (SQL/108), „Übergabe an Sage“ = „nicht nötig“, Worker-Tests ohne Zeitfenster | **Testbereit** `0af3a94e` (QA 2026-11-07) | `_SettingField.cshtml`, `SettingItem.Aus`, `SageUebergabeStatusText`, `WorkerTestLauf`, [[2026-11-08-bereinigungen-nach-buendel-2026-11-07-spec]] |
| v1.80.0: Artikelinfo – Karte „Lieferanten“ (Haupt-/letzter Lieferant mit Kreditor-Link, Artikel-Lieferanten aus Sage), Hinweise statt leerer Karte, Verlinkung aus Liste/Bearbeiten | **Testbereit** `ac92a15c`, `04742c26` (QA 2026-11-08) | `ArticlesController.Info`, `ArticleSupplierRow`, `Views/Articles/Info.cshtml`, [[2026-11-08-artikeldetails-lieferanten-anzeigen-spec]] |
| v1.81.0: Karte „Meine Fehlteile“ je Bestelltyp (Link mit Typ), Werkbank-Auswahl im Modus „Nur meine Werkbänke“, Haken abschaltbar | **Testbereit** `747c1ced` (QA 2026-11-08) | `WarehouseRequisitionRepository.GetShortageCountsForUserAsync(type)`, `MissingPartsController` (`ForeignWorkplaceSelected`), [[2026-11-08-fehlteile-karte-meine-fehlteile-lager-glas-spec]] |
| v1.84.0 (Epic): Wareneingang zu Sage-Bestellungen – Abgleich offene EBB-Positionen (SData), Vorschau „Offene Bestellungen“ (X Wochen), Wareneingang zu Bestellung | **Testbereit** `cf1f8547`…`0a941d63`, QA-Fixes `d9621029`, `26e6a278` (QA 2026-11-08) | `EinkaufBestellungenSyncService`, `PurchaseOrdersController`, `SagePurchaseOrderLines`, `PurchaseOrderReceipts`, [[2026-11-08-wareneingang-aus-sage-bestellungen-spec]] |
| v1.85.0: Artikel – Sage-Karten auch auf Bearbeiten, Mengeneinheiten (SQL/140), Kreditor-Karte „Artikel dieses Lieferanten“ (Server-Filter + Pagination), Listenspalten Haupt-/letzter Lieferant | **Testbereit** `5e3340ce`, `1dac3bcd` (QA 2026-11-08) | `_ArtikelSageKarten`, `CreditorsController.Details`, `ColumnDefinitions.Articles`, [[2026-11-08-artikeldetails-lieferanten-anzeigen-spec]] |
| v1.86.0: Offene Bestellungen – überfällig rot mit Legende, Sperre Basis ≠ Lager, Hinweis nicht buchbarer Ziel-Platz | **Testbereit** `405e9af5` (QA 2026-11-08) | `PurchaseOrders/Index.cshtml`, `EinkaufBestellungenSyncService`, [[2026-11-08-wareneingang-aus-sage-bestellungen-spec]] |
| v1.87.0 (Epic): Lagerbuchungen – FA verknüpft (`StockMovements.ProductionOrderId`, SQL/109/110), Prüfkarte + Pflicht-Einstellung, FA-Link in Historie/Bestandsübersicht | **Testbereit** `f38e82ac`…`7ab2fcb5` (QA 2026-11-08) | `FindForBookingAsync`, `FaBuchungsText`, `fa-feld.js`, `_FaLink`, [[2026-11-08-lagerbuchungen-fa-feld-und-verlinkung-spec]] |
| v1.88.0 (Epic): Kommissionieraufträge Kopf/Zeilen – Nummer KO-JJJJ-NNNNN, Kommissionierbereiche + Regeln, Erzeugen bei Freigabe/Nachziehen, Ist-Menge, Folgeauftrag, Druck (SQL/133, Einmal-Skript Altbestand; E7 Sage gesperrt) | **Testbereit** `3500c2fe`…`15a8e45f`, Reparatur `1ed5c72c` (QA 2026-11-08) | `PickingOrderWorkflow`, `PickingTransferService`, `PickingAreasController`, `/Picking`, [[2026-11-08-kommissionierung-auftraege-kopf-zeilen-spec]] |
| v1.89.0: Kommissionieraufträge Nachbesserung Q9–Q11 – Zeilenstatus „gepickt“, zweiter Standard abgelehnt, „Altbestand prüfen“ (SQL/141) | **Testbereit** `bc8be58c`, `53858c6c` (QA 2026-11-08) | `PickingOrderWorkflow.ErzeugeOffeneAsync`/`AltbestandGeprueftAsync`, `PickingAreasController.Uebernehmen`, [[2026-11-08-kommissionierung-auftraege-kopf-zeilen-spec]] |
| v1.90.0: Lager-Tools FA-Prüfkarte je FA-Pflicht (`fa-lookup?art=` → `pflicht`), Link-Kontrast | **Testbereit** `5d4df4be` (QA 2026-11-08) | `StockApiController.FaLookup`, `fa-feld.js`, [[2026-11-08-lagerbuchungen-fa-feld-und-verlinkung-spec]] |
| v1.91.0: Offene Bestellungen – Legende im Zeilenton (N-1) | **Testbereit** `bacf3b82` (QA 2026-11-08) | `Views/PurchaseOrders/Index.cshtml`, [[2026-11-08-wareneingang-aus-sage-bestellungen-spec]] |
| v1.92.0 (Bug): Checkbox + Hidden-„false“ – OSEON-Relevanzfilter, BDE-Edit Final/Löschen, eine Final-Zeile je Buchung | **Testbereit** `cf869e97`, Reparatur `9c9046c4` (QA + Nach-QA 2026-11-08) | `Tracking/OseonIndex.cshtml`, `BdeBookingsController.Edit`, [[2026-11-08-oseon-relevanzfilter-schaltet-sich-ab-spec]] |
| v1.93.0 (Bug): Meldebestand-Mail mit der Bestandsregel der Web-App, Artikel ohne Bewegung | **Testbereit** `897f7a03` (QA 2026-11-08) | `IDEALAKEWMSService/Services/StockCheckService.cs`, [[2026-11-08-meldebestand-mail-bestand-falsch-berechnet-spec]] |
| v1.94.0 (Bug): Menüleiste bricht um (kein Seiten-Querscrollen), Fehlteile-Link Kontrast; SQL/108 Nachtrag `StammdatenADGruppe` | **Testbereit** `2efdb023`, `6987e2bd` (QA 2026-11-08) | `Views/Shared/_Layout.cshtml`, [[2026-11-08-seiten-querscrollen-navigationsleiste-spec]] |
| v1.95.0: Nachbesserung Vorab-Abnahme – Spaltenauswahl Offene Bestellungen (+ Drift-Wächter viewKey), FA-Link findet erledigte FAs, Hilfe QR/FA, Leitstand-Hinweis „kein Kommissionierer“ | **Testbereit** `a26be406` (QA Reparatur 1, 2026-11-08) | `ColumnDefinitions.PurchaseOrders`, `_FaLink`, `_KeinKommissioniererHinweis`, [[2026-11-08-buendel-dev-v1-94-browser-testbericht]] |
| v1.96.0: Vorwärts-Merge main → Bündel – AKE-Hotfixes v1.30.2 (Zahnrad), v1.30.3 (OSEON-MERGE über Auftragsnummer, Druck Rand/Sortierung, Werkbank), v1.30.4 (Kommissionierfotos in `App_Data`) | **Testbereit** `5492f0db` (Merge `02a0a173`, QA 2026-11-11, DEV AKE+IDEAL v1.96.0) | [[2026-11-10-vorwaerts-merge-main-ins-buendel]], [[2026-11-11-v1-96-0-vorwaerts-merge-main-hotfixes]] |
| v1.97.0: Auftrag drucken in Sortierreihenfolge der Ansicht (alle Zeilen, POST mit Positionsliste) | **Testbereit** `9db618c0` (QA 2026-11-11, DEV AKE+IDEAL v1.97.0) | `PickingController.AuftragDrucken`/`SortiereZeilen`, [[2026-11-08-kommissionierung-auftraege-kopf-zeilen-spec]], [[2026-11-11-v1-97-0-auftrag-drucken-sortierung]] |
| v1.98.0: Update-Skript je Zielsystem mit Nachlauf-Manifest, Ordner/Rechte-Pruefung, Installationsskript standortneutral (Betrieb, keine Anwender-Aenderung) | **Testbereit** (Buendel `550f10d5`, QA 2026-11-11, DEV AKE+IDEAL v1.98.0; wartet Schranke 2) | `scripts/deploy/` (`Update-IdealAkeWms.ps1`, `nachlauf.psd1`, `ziele.psd1`, `IdealAkeWms.Deploy.psm1`, `Install-IdealAkeWms.ps1`), `NachlaufManifestDriftTests`, ADR [[0022-update-je-zielsystem-per-skript-mit-nachlauf-manifest]], [[2026-11-09-update-skript-je-zielsystem-mit-nachlauf-spec]], [[2026-11-11-v1-98-0-update-skript-je-zielsystem]] |
| v1.98.1: Update-Skript Url aus `ziele.psd1` vor IIS-Bindung (F3), Dienst-Stopp mit 60-s-Limit ohne Kill (F7) | **Testbereit** (Buendel `e1ecc0c0`, QA 2026-11-11, DEV AKE+IDEAL v1.98.1, Paket `publish\` v1.98.1; wartet Schranke 2) | `scripts/deploy/Update-IdealAkeWms.ps1`, `ziele.psd1`, `IdealAkeWms.Deploy.psm1` (`Test-Url`), `NachlaufManifestDriftTests`, [[2026-11-09-update-skript-je-zielsystem-mit-nachlauf-spec]], [[2026-11-11-v1-98-1-update-skript-url-und-dienst-stopp]] |
| v1.99.0/v1.99.1: Update-Skript prueft (Typ `Pruefung`) und fuehrt Daten-/Einmal-Skripte mit Bestaetigung aus, Marker `DeployScriptRuns` | **Testbereit** v1.99.12 (QA 2026-11-12, Buendel `56906459`: Pruefung/MailPickup; v1.99.2 `81b95ac2`, v1.99.1 `f79ba893`, v1.99.0 `55ec218a`), wartet Schranke 2 | `scripts/deploy/Update-IdealAkeWms.ps1`, `nachlauf.psd1`, `SQL/142_AddDeployScriptRuns.sql`, `Models/DeployScriptRun.cs`, `Program.cs` (Seed Kommissionierbereiche), `NachlaufManifestDriftTests`, `DeployScriptRunsMigrationSqlTests`, [[2026-11-11-update-skript-prueft-und-fuehrt-aus-mit-bestaetigung-spec]], [[0023-daten-und-einmal-skripte-fuehrt-das-update-skript-mit-bestaetigung-aus]], [[2026-11-11-v1-99-0-update-skript-prueft-und-fuehrt-aus]] |
| v1.99.3: Vormontage-Listen (IDEAL) Spalte „Abteilung“ je HauptFA (Kombigerät → „Kombigerät“) | **Testbereit** (Buendel `d2d4ed46`, QA 2026-11-11, wartet Schranke 2) | `Services/VormontageService.cs` (`FormatAbteilung`, `KopfWerte.Abteilung`), `Views/FaHierarchyVormontage/Index.cshtml`/`Summiert.cshtml`, `ColumnDefinitions`, `VormontageNeueSpaltenTests`, [[2026-11-11-vormontage-spalte-abteilung-hauptfa-spec]] |
| v1.99.4: Wareneingang Teil 2 – Sage-Wareneingangsbeleg (ELL) je WE-Buchung, Lieferschein-Nr., Liste „Wareneingänge an Sage“ (E1–E4, E6; E5 blockiert bis P-W9) | **Testbereit** (Buendel, v1.99.4 QA `152ab6f1`/`bd9b58d0`; v1.99.5 Memo je Position `c155b1b2`, QA 2026-11-12; v1.99.6 Positionsmemo-Feld `23ea96ca`, QA `67322b85`; v1.99.7 E5 Auto-Retry mit Sage-Prüfung/Klären/Q-2/Menü `f8d74399`, QA `bb788f0e`; v1.99.9 System-Kürzel WE#/SM# `115e8a09`/`c47f0136`, QA `4c1b5523` (SQL/144); Schreibtests 2027-398675 und 2027-398676) | `PurchaseOrdersController.Receive/.SageWe`, `SageWareneingang`, Dienst `SageWareneingangService`/`-Client`/`-PayloadBuilder` (`PositionsMemo`), `SQL/143`, [[2026-11-08-wareneingang-teil-2-sage-we-beleg-rahmenabrufe-spec]], [[2026-11-11-v1-99-4-wareneingang-sage-we-beleg]], [[2026-11-12-v1-99-5-wareneingang-memo-je-position]], [[2026-11-12-v1-99-6-wareneingang-positionsmemo]], [[2026-11-12-v1-99-7-wareneingang-e5-auto-retry]], [[2026-11-12-v1-99-9-sage-system-kuerzel]] |
| v1.99.8: Kommissionierung – Spalte „Matchcode“ (FA-Artikel, Artikelstamm) in der Auftragsliste + Matchcode im Kopf des Auftragsdrucks | **Testbereit** (Buendel `557ea63b`, QA 2026-11-12, DEV AKE+IDEAL v1.99.8, Paket `publish` v1.99.8; Befunde N-1/N-2 niedrig offen) | `PickingController.Index`/`RenderAuftragDruckenAsync`, `MatchcodeLookup`, `Views/Picking/Index.cshtml`, `PrintPicking.cshtml`, [[2026-11-08-kommissionierung-auftraege-kopf-zeilen-spec]], [[2026-11-12-v1-99-8-kommissionierung-matchcode]] |
| v1.99.10: Offene Bestellungen – taegliche Lieferverzug-Mail (Einkauf-Sammelliste, Kreditor-Mail DE+EN mit Testbetrieb, Wiederholung, Grenze), Kreditor-Felder „Keine Erinnerungsmails“/„Adresse fuer Erinnerungen“, `MailSettings:PickupDirectory` | **Testbereit** (QA 2026-11-12; Buendel `37c831a4`/`dae22a49`, SQL/145, TS-128; wartet Schranke 2) | `IDEALAKEWMSService/Services/Lieferverzug{Planer,MailHtml,MailService}.cs`, `SyncWorker`, `MailService.SendMitOptionenAsync`, `IdealAkeWms/Services/MailAdressen.cs`, [[2026-11-12-offene-bestellungen-mahnmail-lieferverzug-spec]] |
| v1.99.11: Wareneingang zu Bestellung – Barcode des Lieferschein-Aufklebers (`WareneingangBarcodeAktiv`) und CSV-Übergabe `<Barcode>.csv` auf einen UNC-Pfad nach der Sage-Übergabe (Ausgangstabelle, Dienst-Schritt im `SageBookingWorker`, Spalten Barcode/CSV, „CSV erneut schreiben“), Scan-Modus `'raw'` korrigiert | **Testbereit** (QA 2026-11-12; Buendel `895646a5`/`0dc0cea0`, SQL/146, TS-129; wartet Schranke 2) | `IdealAkeWms/Services/WareneingangCsv.cs`, `IDEALAKEWMSService/Services/WareneingangCsvService.cs`, `SageBookingWorker`, `PurchaseOrdersController.Receive`/`.SageWe` (`CsvErneutSchreiben`), `Models/WareneingangCsvExport.cs`, `barcode-scanner.js` (`textScanType`), [[2026-11-12-wareneingang-barcode-lieferschein-csv-export-spec]] |
| v1.99.13: Bugfix AKE-Regression – Spaltenfilter-Wert in Anführungszeichen nicht an `;`/`,` getrennt (C#+JS ein Kern), FA-Hinweis Einbuchung „Bestand am Platz“ (Variante A), Bedarfsmeldungen in Ortszeit, Auftragsdruck mit Quell-Vorschlag, Wagen-FA-Liste zerlegt | **Testbereit** (QA 2026-11-13, DEV AKE+IDEAL Browser-Teil belegt; Buendel `7cc8e684`/`a8dd9d2e`, TS-130; wartet Schranke 2) | `ColumnFilterSyntax.SplitParts`/`column-filter-syntax.js splitParts`, `Views/StockMovements/Inbound.cshtml`, `PartRequisitionRepository`, `PartRequisitionEmailService`, `PickingController.QuellVorschlag`, `StockMovementRepository.GetProductionOrdersAtLocationAsync`, [[2026-11-12-befunde-ake-regression-dev-spec]], [[2026-11-13-v1-99-13-bugfix-ake-regression]] |
| v1.99.14: SData Teil 2 – Register „Sage-Test“ (nur Admin: Verbindungstest, Feldliste, Stückliste live je Artikel/FA, FA-Struktur live), Gate `SageSdataFaAnsichtAktiv` entfernt, SData-Kennwort verschlüsselt (`dp1:`) in den Service-Einstellungen mit appsettings-Rückfall, Lesetest Benutzerfelder AKE-Test | **Testbereit** (QA 2026-11-13 nach Reparaturversuch 1; Buendel `7f3b1a66`/`cd38fbaa`/`0b1cc9a2`/`7048071a`, SQL/147, TS-131; wartet Schranke 2) | `Controllers/SageTestController.cs`, `Services/SageSdata/SdataZugangsdaten.cs`, `SageTestAufbereitung.cs`, `SageSdataReadClient`, `SettingType.Geheim`, `SageQuelleSchalter`, `SageBookingWorker`, [[2026-11-13-sdata-teil-2-userfelder-stueckliste-spec]], [[0024-sdata-kennwort-verschluesselt-in-den-service-einstellungen]], [[sage-sdata-benutzerfelder-lesetest]], [[2026-11-13-v1-99-14-sdata-teil-2-sage-test]] |
| v1.99.15: Wareneingang Teil 3 – Rahmenabrufe (EBZ) im Abgleich, in Offene Bestellungen, im Wareneingang und in den Lieferverzug-Mails; Rest = Abrufmenge minus alle WMS-Wareneingänge; Sage-WE für Abrufe vorerst `NichtNoetig` (E2 Schreibtest offen) | **Testbereit** E0+E1+E3 (QA 2026-11-13; Buendel `5c3538b3`/`47c95931`/`de4a85ac`, QA `e92894c9`, TS-132; E2 Schreibtest wartet auf Gerald; wartet Schranke 2) | `EinkaufBestellungenSyncService`, `PurchaseOrderReceiptRepository` (`Abzuziehen(l)`), `Services/SageWareneingang.cs` (`RahmenabrufMitSageWe`, `GiltFuer`), `PurchaseOrdersController`, `LieferverzugMailService`/`Html`, [[2026-11-11-wareneingang-teil-3-rahmenabrufe-spec]], [[2026-11-13-v1-99-15-wareneingang-teil-3-rahmenabrufe]] |
| v1.99.16: Bugfix Update-Skript – Wiederholungssperre des Altbestand-Skripts im Trockenlauf mit `TREFFER: 0` (keine Fehlwarnung nach dem Scharflauf), Auswahl-Schleifen brechen bei leerer Eingabe ab (`-NurPruefen` ohne Endlosschleife), DEV-Ziele in `ziele.psd1`, Drift-Regel Sperre | **Testbereit** (QA 2026-11-13; Buendel `cb204c96`, TS-133) | `scripts/deploy/Update-IdealAkeWms.ps1`, `IdealAkeWms.Deploy.psm1` (`Read-Ja`), `Install-IdealAkeWms.ps1`, `ziele.psd1`, `SQL/Einmalig/2026-11-08_v1.88.0_Kommissionierauftraege_Altbestand.sql`, `NachlaufManifestDriftTests` (`SperrBloecke`), [[2026-11-13-updater-zweiter-trockenlauf-altbestand-spec]], [[2026-11-13-v1-99-16-updater-zweiter-trockenlauf]] |
| v1.99.17: Kommissionier-Prio – zurückgenommene Prio zählt ins MAX (Variante B), keine Doppelten mehr bei Einzel- und Sammelfreigabe; `Pruefung/DoppeltePrio` im Nachlauf zeigt Altbestand | **Testbereit** (QA 2026-11-13, keine Befunde; Buendel `73911686`, TS-134; wartet Schranke 2) | `ProductionOrderPickingStatusRepository.GetMaxPickingPriorityAsync`, `PickingLeitstandController`, `nachlauf.psd1`, [[2026-11-13-doppelte-kommissionier-prio-nach-ruecknahme-spec]], [[2026-11-13-v1-99-17-doppelte-kommissionier-prio]] |
| v1.99.18: Zeitstempel in Lokalzeit – alle UTC-Schreiber in Web (24) und Dienst (20, `@Jetzt`) umgestellt, Karte Sage-Zusatzfelder ohne `ToLocalTime()`, Wächter-Test mit fester Ausnahmeliste, ADR 0025; Altdaten unverändert | **Testbereit** (QA 2026-11-13, F1 TS-Text behoben `d9e4c89e`, Hinweise H-1/H-2; Buendel `99399be4`/`c703c1e3`/`4716888b`, TS-135; Dienst-Läufe Manual-UAT offen; wartet Schranke 2) | `IdealAkeWms.Tests/Helpers/ZeitstempelKonventionTests.cs`, `SageProductionOrderSql`, `SageImportService`, `OseonSyncService`, `CoatingDetectionService`, SData-Writer, Controller Users/Roles/OrderRecipients/Tracking, [[2026-11-13-utc-schreiber-auf-lokalzeit-spec]], [[0025-zeitstempel-in-lokalzeit-des-schreibenden-rechners]], [[2026-11-13-v1-99-18-zeitstempel-lokalzeit]] |
| v1.99.19: Lieferverzug-Mail Nachbesserung – Kreditor-Schwelle (Max beider Schwellen), Altersgrenze `MaxAlterTage` 180 mit Abschnitt „Zu klären“, Schalter `KreditorMitRahmenabrufen` (Default aus), Spalte Lieferanten-Artikelnummer (`ArticleSupplier.SageBestellnummer`) | **Testbereit** (Nach-QA 2026-11-13; Buendel `c31c06b0`/`def97b36`, QA `65496165`, TS-128.16–128.26; P-L1-Limit entscheidet Gerald; wartet Schranke 2) | `LieferverzugPlaner`, `LieferverzugMailService`/`Html`, `ServiceSettingDefinitions`, `nachlauf.psd1`, [[2026-11-13-lieferverzug-nachbesserung-keyuser-spec]], [[2026-11-13-v1-99-19-lieferverzug-nachbesserung-keyuser]] |
| v1.99.20: Einkauf – Bestellvorschläge (Epic): Liste Nachbestellartikel `/PurchaseProposals`, Rolle `einkauf`, Gate `EinkaufAktiv` (Default aus), offen bestellt aus Sage-Cache, Aufrunden Mindestbestellmenge mit strengem Einheiten-Wächter; E3 letzte Bestellmenge + E5 Dispo-Stapel blockiert | **Testbereit** E0/E1/E2/E4/E6 (QA 2026-11-13; Buendel `f5209692`/`31ae0d69`/`9b63dfc6`/`f5f6bd5b`, QA `cf3f41cb`, TS-136; E3 blockiert A3, E5 blockiert; wartet Schranke 2) | `PurchaseProposalsController`, `PurchaseProposalCalculator`, `PurchaseProposalRepository`, `RequireEinkaufAccess`/`RequireEinkaufAktiv`, [[2026-11-08-einkauf-bestellvorschlaege-spec]], [[2026-11-13-v1-99-20-einkauf-bestellvorschlaege]] |
| v1.99.21: Einkauf Bestellvorschläge E3 – letzte Bestellmenge je Artikel/Lieferant (SData `EinkaufPositionenAllInfo`, EBB+EBZ, 3 Jahre), Regeln n × L und anderer Lieferant, Fallback Sage-Bestellmenge mit Hinweis/Banner | **Testbereit** (QA 2026-11-13; Buendel `ca2f49ed`, SQL/148, TS-136; Q-3 Laufzeit je Zyklus ~287 s → Gerald; wartet Schranke 2) | `ArtikelLieferantSyncService.LetzteBestellungAsync`, `ArticleSupplier.LetzteBestellmenge/-Am/-Beleg`, `PurchaseProposalCalculator`, [[2026-11-08-einkauf-bestellvorschlaege-spec]], [[2026-11-13-v1-99-21-einkauf-letzte-bestellmenge]] |
| v1.99.22: Belegnummer 45 Zeichen (Bestellungs-Cache, Erinnerungen, Wareneingänge, Sage-WE-Beleg), Wareneingang zu Bestellung mit Spalte „Überlieferung“ und Hinweis auf offene Lagerbestellungen/Bedarfsmeldungen | **Testbereit** (QA 2026-11-13; Buendel `4689922c`, QA `90bbbbd7`, SQL/149, TS-137; wartet Schranke 2) | `Views/PurchaseOrders/Receive.cshtml`, `PurchaseOrdersController`, `SagePurchaseOrderLine.BelegnummerMaxLaenge`, [[2026-11-08-einkauf-bestellvorschlaege-spec]] (Freigabe-Nachtrag 3), [[2026-11-13-v1-99-22-belegnummer-45-wareneingang]] |
| v1.99.23: Schutz gegen Doppel-Absenden (Abschluss „Teilweise“, Leitstand-Freigabe mit Zielzustand, Wareneingang mit Absendekennung, AG-Fertigmeldung mit Applock, globale Knopf-Sperre), Unterpositionen im KO-Auftrag sichtbar, CSV ohne Tausendertrenner, feste Kultur de-DE | **Testbereit** (QA 2026-11-14, `1a73bc6a`, SQL/150, TS-138; wartet Schranke 2) | `Services/Nummernkreis.cs`, `PickingOrderWorkflow.AbschliessenAsync`, `PickingLeitstandController.ToggleRelease`, `PurchaseOrdersController.Receive.cs`, `BdeBookingService.ReportCompletionAsync`, `wwwroot/js/site.js`, `BomViewModel.VaterPosition`, `csv-export.js`, `Program.cs`, [[2026-11-14-schutz-doppel-absenden-und-unsichtbare-positionen-spec]], [[2026-11-14-v1-99-23-schutz-doppel-absenden]], ADR [[0026-schreibaktionen-senden-die-absicht-der-server-prueft-am-schreibort]] |
| Betriebshandbuch für Administratoren (`docs/betrieb/` 00–10 + `AGENTS.md` KI-Leitfaden), Stand v1.99.23, jede Aussage belegt | **Entwurf zur Durchsicht** (Bündel `8bfd8c73`, nicht gemergt; rund 60 „zu prüfen“) | `docs/betrieb/00-index.md`, [[2026-11-14-betriebshandbuch-goal-lauf]] (Befunde BH-1 bis BH-7, Geheimnis-Scan) |
| v1.99.25: Beschichtungsquelle je Standort – Auswahl-Einstellungen Kennzeichen/Termin, ein Resolver, Dienst schreibt Lack-Flag nur aus der gewählten Quelle | **Testbereit** – E1–E6 gebaut (`2336dced`, `b4972700`, `eff0c1a4`; Auslieferung bis 1.99.34), QA 2026-11-17 grün (nicht gemergt) | [[2026-11-16-beschichtungslogik-je-standort-spec]], `IdealAkeWms/Services/Beschichtung/BeschichtungResolver.cs`, ADR [[0028-beschichtungsquelle-je-achse-ueber-appsettings-mit-automatisch]] |
| v1.99.26: FA-Struktur IDEAL über SData – orderBy-Pflicht, Stückliste live für beendete Haupt-FAs, Vergleich aller Knotenfelder, Hybrid gebaut | **InUmsetzung** (QA mit IDEAL-Daten 2026-11-16: R1 FA-Kopf-Felder heißen `KHKPpsFaBelege_USER_OS*`, Code liest leer; R2/R4 offen) – E1–E6 gebaut; Umschalten auf SData gesperrt bis P-FS3/Rückfragen 4/5 (Bündel `5163df17`, nicht gemergt) | [[2026-11-16-fa-struktur-ideal-ueber-sdata-spec]], `FaStrukturQuelleSwitch.cs`, `SdataFaStrukturReader.cs`, ADR [[0029-fa-struktur-ideal-knoten-aus-sdata-fainfos-aus-view-hybrid]] |
| v1.99.28: Sicherheit – Formular-Anmeldung nur mit Kennwort, Erstkennwort mit Pflichtwechsel (`User.MussKennwortAendern`, SQL/152), admin-Seed `wms-admin`, Cookies `SameAsRequest`, FreshInstall Abschnitt 15 repariert + Wächter-Test, Installer ohne SData-Zugang / FA-Abgleich aus in der DB, SMTP-Warnung | **InUmsetzung** – feature-complete im Bündel (`6447db0a`, `4559e0a2`, nicht gemergt), QA durch Koordination ausstehend; vor Deploy P-1/P-2b/MailSettings (Gerald) | [[2026-11-14-sicherheit-anmeldung-und-installer-spec]], `AccountController.Login`, `Services/AdminSeed.cs`, `UsersController.ApplyPassword`, `scripts/deploy/Install-IdealAkeWms.ps1`, ADR [[0027-formular-anmeldung-nur-mit-kennwort-erstkennwort-mit-pflichtwechsel]], [[2026-11-16-v1-99-28-sicherheit-anmeldung-installer]] |
| v1.99.29: Wareneingang-CSV für enaio – ohne Kopfzeile (9 Felder nach Position), Erfasser = Windows-Kontoname ohne Domäne (`StockMovement.User.WindowsUserName` über `ExtractSam`), sonst `User.Name`, ohne UserId `CreatedBy`; nur die CSV | **Testbereit** (QA 2026-11-16) – im Bündel (`f12f46f7`, nicht gemergt), wartet Schranke 2; keine Migration, TS-153 | [[2026-11-14-wareneingang-csv-erfasser-samaccountname-spec]], `IDEALAKEWMSService/Services/WareneingangCsvService.cs` (`CsvInhalt`, `RunOnceAsync`), `WindowsAccountHelper.ExtractSam`, [[2026-11-16-v1-99-29-wareneingang-csv-enaio]] |
| v1.99.30: Bestellvorschläge Nachbesserung (Epic) – eigener Lauf letzte Bestellung (3/12 Uhr), Bestellungen ohne Artikel-Lieferant, EINE Bedarfsregel für Liste und Artikelinfo (FA-Rest, max-Regeln), Dringlichkeit heute + WBZ, Mengenregel A, Einheiten-Ersatzlösung, Matchcode + Link Artikelinfo | **Testbereit** (E1–E7 + E9, `fd9f49db`, QA-Fix `54f04740`, nicht gemergt; QA Q2 Kommissionierwagen offen; E8 Rahmenvertrag übersprungen P-N6/P-N9; E7 Umrechnung blockiert P-N1/2/3/4/7); SQL/155, TS-154 | `PlannedConsumptionService`, `PurchaseProposalsController`, `PurchaseProposalCalculator`, `LetzteBestellungSyncService`, `ArticlesController.Info`, [[2026-11-16-bestellvorschlaege-nachbesserung-spec]], [[2026-11-16-v1-99-30-bestellvorschlaege-nachbesserung]] |
| v1.99.31: Offene Bestellungen – sofort neu lesen nach Sage-WE (Entprellung `Sync:EinkaufBestellungenNachWeMinuten`, Sperre gegen parallelen Vollersatz), Einkauf → Offene Bestellungen + Sage-Wareneingänge (offen) nur lesend, Spaltenfilter Liefertermin vorbelegt Mo..So (jahresgenau, Feld „Wochen“ entfällt), KW in der Zelle, Checkbox „Überfällige anzeigen“ je Benutzer | **Testbereit** (`d3a00101`, nicht gemergt) | `SageBookingWorker.AnstossEntscheidung`, `EinkaufBestellungenSyncService.TryRunExclusiveAsync`, `EinkaufPurchaseOrdersController`, `PurchaseOrdersController.ListeAsync`/`SageWeListeAsync`, [[2026-11-16-offene-bestellungen-sofort-neu-lesen-einkauf-kw-filter-spec]], [[2026-11-17-v1-99-31-offene-bestellungen-sofort-neu-lesen]] |
| v1.99.32: BDE-Teileliste Blechfertigung – Werkbank-Option, Teil-/Baugruppenmeldung mit fachlichem Schlüssel, Rücknahme, Filter je Werker, Erkennung V1 | **Testbereit** 2026-11-17 (Bündel `6704fe2a`…`c7c968fe` + QA-F1 `5ced750f`, nicht gemergt; wartet Schranke 2) | [[2026-11-13-bde-vereinfacht-teileliste-blechfertigung-spec]], `IdealAkeWms/Services/BdeTeilelisteService.cs`, ADR [[0030-teilmeldung-mit-fachlichem-schluessel-neben-workoperation]] |
| v1.99.27 (ausgeliefert mit 1.99.32): Arbeitsgang-Mapping über Sage-Arbeitsgangnummer – Sage-AG + Handwert an OperationConfig, Prüfliste a/b/b2/c | **Testbereit** 2026-11-17 (Bündel `5960b6a5`, nicht gemergt; wartet Schranke 2) | [[2026-11-16-arbeitsgang-mapping-sage-arbeitsgangnummer-spec]], `IDEALAKEWMSService/Services/OperationConfigSyncService.cs`, ADR [[0031-sage-arbeitsgangnummer-an-operationconfig-sagewert-plus-handwert]] |
| v1.99.33: Wareneingang – Sammellieferschein (gleicher Barcode bei gleichem Lieferanten und gleicher LS-Nr., CSV `<Barcode>_n.csv`, nie überschreiben) und AB-Nummer/Kommission aus dem Sage-Bestellkopf (RealTimeData `dtsEKBelegMst`, nur GET über `BelID`) in Offene Bestellungen und im Kopf der Einbuchung | **Testbereit** (`ed919976`, nicht gemergt) – im Bündel (`32338503`, `19ce63ef`, QA-Fix `ed919976`, SQL/156, TS-156); Prod-Einschalten erst nach enaio E-1 | [[2026-11-17-wareneingang-sammellieferschein-und-bestellkopf-spec]], `IDEALAKEWMSService/Services/WareneingangCsvService.cs` (`Dateiname`), `EinkaufBestellungenSyncService.KoepfeAsync`, `PurchaseOrdersController.SammellieferscheinGrund`, [[2026-11-17-v1-99-33-wareneingang-sammellieferschein-bestellkopf]] |
| v1.99.34: Beschichtung an der AKE – Vorschlagsliste und Beschichtungsauftrag aus der WA-Stückliste (Lackierteil-Kategorie, offene WA mit Kennzeichen), Menge über `BomQuantityResolver`, Schlüssel Artnr + PositionText (SQL/157), natürliche Sortierung, Sammelmeldung, „(IDEAL)“ aus den Namen | **Testbereit** (`eff0c1a4`, QA 2026-11-17 grün, nicht gemergt; Befunde F1–F3 niedrig) – Etappe E5 | [[2026-11-16-beschichtungslogik-je-standort-spec]], `Services/Beschichtung/BeschichtungFlacheQuelle.cs`, `BeschichtungsauftragRepository.GetFlacheLackierteileAsync`, [[2026-11-17-v1-99-34-beschichtung-ake-stueckliste]] |
| v1.99.35: Blechfertigung – BF-Aufträge (Warenkorb BG/FT mit Mengenregel, Schachtler-Freigabe mit Arbeitstag-Terminen, WiCAM-CSV durch den Dienst, Fertig/Storno, Mail an Empfängergruppe), Datenquelle V-B (SData-Nachlese), SQL/158, Rolle `schachtler` | **InUmsetzung** – feature-complete 2026-11-17 (`09063984`, `ea21bddd`, `37e83d85`, `89ce487a`, `9f2a0802`, nicht gemergt); qa-agent ausstehend; CSV-Layout Platzhalter bis P-1 | [[2026-11-13-blechfertigung-bf-auftraege-wicam-csv-spec]], `IdealAkeWms/Services/BfAuftragWorkflow.cs`, `IDEALAKEWMSService/Services/BfCsvService.cs`, [[2026-11-17-v1-99-35-blechfertigung-bf-auftraege]] |
| v1.99.37: FA-Struktur IDEAL als Abgleich mit Lebenslauf statt Full-Replace (beendete FAs bleiben mit Schild sichtbar, BDE-Teileliste nicht meldbar, Aufräumen nach Erledigt/90 Tagen, Wächter leerer Read/Dubletten, R4 Artikeltyp vom Artikel) | In Umsetzung/QA (Bündel `7a1f7301` `94e327e7` `026d0d32` `354344d8` `8ea8e98d` `2abad047`+`b1f83b1a` `0963a0a5` `7fa7603d`+`96049d0d`, SQL/159, TS-158, nicht gemergt; Goal-Lauf [[2026-11-17-goal-lauf-sammel-abend]]; Mandant-Filter P-AB4 offen) | [[2026-11-17-fa-struktur-abgleich-statt-full-replace-spec]], ADR [[0032-fa-struktur-abgleich-mit-lebenslauf-statt-full-replace]] | `IDEALAKEWMSService/Services/FaStrukturAbgleich.cs`, `FaHierarchySyncService.cs`, `SageFaStrukturReader.FaStatusLesenAsync`, `Views/Shared/_FaStrukturBadge.cshtml` |
| v1.99.38: Arbeitsgänge aus dem WMS in Sage-FAs anlegen (IDEAL) – Vorschau je FA × Kürzel mit Sage-Stand (E0), Warteschlange + Dienst-Schritt FertigungsauftragService ServiceOption 4 hinter Schalter (aus) (E1a) | **Testbereit** – QA 2026-11-17 (`825b8f58` `52a44b71` `e5ce82da` + QA-Fix `ab75c336`, SQL/160, TS-159, nicht gemergt); E1b Schreibtest blockiert (Freigabe Gerald); F3 behoben v1.99.39 `69a39a8b` (Nachlesen nach dem Senden); vor E1b offen: Feldnamen P-1/P-2/P-4 | [[2026-11-17-arbeitsgaenge-aus-wms-in-sage-fa-anlegen-spec]], [[2026-11-17-v1-99-38-arbeitsgaenge-in-sage-fa-anlegen]] | `IdealAkeWms/Controllers/SageFaArbeitsgaengeController.cs`, `IdealAkeWms/Services/SageFaArbeitsgaenge.cs`, `IDEALAKEWMSService/Services/SageFaArbeitsgangService.cs`/`SageFaArbeitsgangClient.cs` |
| v1.99.39: Nachbesserung Abend – F3 Erfolg erst nach Nachlesen der AG in Sage, Update-Skript-Prüfung `Pruefung/ArtikelQuelleSData` (AKE), Listen: Seite hinter der letzten → letzte Seite (globaler Result-Filter) | **Testbereit** – QA 2026-11-17 (Bündel `69a39a8b` `ebff14e0` `78014b9a` `8d419875` + QA-Fix `2ae5d1da`, keine SQL, TS-159 Nachtrag, TS-160 auf DEV AKE+IDEAL grün, nicht gemergt); offen: Befund N1 (kein Fund nach 2xx wird automatisch wiederholt, Entscheidung Gerald), Publish Paket 4 | [[2026-11-17-v1-99-39-nachbesserung-abend]], [[2026-11-17-goal-lauf-nachbesserung-abend]] | `IDEALAKEWMSService/Services/SageFaArbeitsgangService.cs`, `IdealAkeWms/Filters/PaginationSeiteKlemmenFilter.cs`, `scripts/deploy/nachlauf.psd1` |
| v1.99.40: Sage-Benutzerfelder (USER_*) als Service-Einstellung (27 Keys `Sync:Userfeld:*`, Default = bisheriger Name, leer = „nicht konfiguriert“), 404 einer SData-Resource 24 h pausiert | **Testbereit** (qa-agent 2026-11-18) – Code `127f7e3f` + QA-Fix `4190c887` (Artikel-Varianten nennen die Einstellung) + Datum `c39126c3`, Paket 5 `publishpaket5-v1.99.40`, keine SQL, TS-161; DEV AKE+IDEAL v1.99.40; Manual-UAT Sage-Läufe offen, nicht gemergt | [[2026-11-18-sage-userfelder-als-einstellung-spec]], [[2026-11-18-v1-99-40-sage-userfelder-als-einstellung]] | `IdealAkeWms/Services/SageSdata/SageUserfelder.cs`, `IDEALAKEWMSService/Common/ServiceSettings.cs`, `IDEALAKEWMSService/Workers/SyncWorker.cs` |
| v1.99.43: IDEAL-Kommissionierliste – Ziele mehrfach + je Benutzer gemerkt (Einstellung Standard-Ziele K-01,K-02), Spalten Abteilung (kommagetrennt) und Lagerplätze (ohne Wagen, Batch), Leitstand-Gate (Schalter Seed an, Banner, PDF-Warnung) | **Testbereit** (QA 2026-11-18, `77c7c57e`…`6c9d4a5e` + QA `3a6372a5`), keine Migration, TS-164; nicht gemergt | [[2026-11-18-ideal-kommissionierliste-filter-spalten-leitstand-spec]], [[2026-11-18-v1-99-43-kommissionierliste-filter-spalten-leitstand]] | `IdealAkeWms/Services/KommissionierListenService.cs`, `IdealAkeWms/Controllers/FaHierarchyKommissionierListenController.cs`, `IdealAkeWms/Views/FaHierarchyKommissionierListen/_KommissionierZiele.cshtml`, `_KommissionierGate.cshtml` |
| v1.99.45: Sammellauf – Artikelinfo-Bestand exakt (Teiltext → ArticleId), BF-Warenkorb-Vorschläge beim Tippen, Update-Skript-Meldungen nach Muster (Exit-Code getrennt), Startseite + Menü aus einer Quelle (Kategorien, Berechtigung, Artikelinfo im Menü, OSEON nur mit Teileverfolgung) | **umgesetzt**, wartet qa-agent (`a1ba945d`…`947bb014`), kein SQL, TS-167–170, ADR 0034; nicht gemergt | [[2026-11-19-v1-99-45-sammellauf]], [[2026-11-19-goal-lauf-sammel-v1-99-45]] | `IdealAkeWms/Services/Navigation/NavigationKatalog.cs`, `IdealAkeWms/Views/Home/Index.cshtml`, `IdealAkeWms/Controllers/ArticlesController.cs`, `IdealAkeWms/wwwroot/js/bf-artikelsuche.js`, `scripts/deploy/Update-IdealAkeWms.ps1` |
| v1.99.46: Startseite – Kacheln wieder mit Symbolen (`NavZiel.SymbolPfad`, 9 alte Symbole, aria-hidden) | **Testbereit** (`6e91fef2` + QA-Fix `2f3e98cc`), kein SQL, TS-174; nicht gemergt | [[2026-11-20-v1-99-46-dashboard-kacheln-symbole]], [[2026-11-20-bugfix-dashboard-kacheln-symbole-fehlen-spec]] | `IdealAkeWms/Services/Navigation/NavigationKatalog.cs`, `IdealAkeWms/Views/Home/Index.cshtml` |
| v1.99.47: Lagerplatz ausbuchen mit FA-Filter (Spalte FA, Rest „bleibt am Platz/Wagen“, gemischte Zeilen G1–G3 im Dialog, Serverprüfung Zeilen + Stückzahl, IDEAL Haupt-FA ⊇ Sub-FA) | **Testbereit** (`29577a3a`, `d28a6488` + QA-Fix `0b6fff36`), kein SQL, TS-173 + TS-2.4; nicht gemergt | [[2026-11-20-v1-99-47-lagerplatz-ausbuchen-fa-filter]], [[2026-08-05-wms-bugs-improvements-teil-6-spec]] | `IdealAkeWms/Controllers/StockMovementsController.cs` (`OutboundAll`, `OutboundAllConfirm`, `LadeFaAsync`), `IdealAkeWms/Services/FaBuchungsText.cs` (`PruefeZeile`), `StockMovementRepository.GetProductionOrdersByArticleAtLocationAsync`, `Views/StockMovements/OutboundAll.cshtml` |
| v1.99.50: Startseite – Kachel-Symbole nachgeschärft (Variante B: 21 neue Symbole, keine Doppelbelegung, Wächter `KachelSymbole_SindEindeutig`) | **Testbereit** (`af6eadd7`), kein SQL, TS-175; nicht gemergt | [[2026-11-23-bugfix-dashboard-symbole-nachschaerfen-spec]] | `Services/Navigation/NavigationKatalog.cs` |
| v1.99.51: Startseite – Reiter „Favoriten“ (Stern je Kachel, je Benutzer in `UserViewPreferences` `Startseite.Favoriten`, nur sichtbare Kacheln, Hinweis + „Nicht verfügbare entfernen“) | **Testbereit** (`4802462c`, `563adecd`), kein SQL, TS-176; nicht gemergt | [[2026-11-23-dashboard-favoriten-reiter-spec]] | `Controllers/HomeController.cs`, `Views/Home/Index.cshtml`, `NavigationErgebnis.Favoriten` |
| v1.99.52: Wareneingang zu Bestellung – Bestellnummer ohne Jahr (Endungssuche), Fokus-Zeile + Streifen, Zahnrad (`PurchaseOrderReceive`), Hauptlagerplatz als Ziel-Vorschlag | **Testbereit** (`e51e80c9`, `9937d357`), kein SQL, TS-177, nur AKE; nicht gemergt | [[2026-11-23-wareneingang-zu-bestellung-bedienung-spec]] | `Controllers/PurchaseOrdersController.Receive.cs`, `Views/PurchaseOrders/Receive.cshtml` |
| v1.99.53: Lagerbestellung – Entnahme-Platz je Position (Regel wie Kommissionierung) + Verbrauchsbuchung beim Abschließen (WMS-Ausbuchung, Sage-Entnahme über Queue), Schalter `LagerbestellungVerbrauchsbuchungAktiv` | **Testbereit** (`f7b26ebc` + QA-Fix `a93bd910`), SQL/166, TS-178; nicht gemergt | [[2026-11-23-lagerbestellung-verbrauchsbuchung-bei-lieferung-spec]] | `Controllers/WarehousePickingController.cs`, `Services/QuellLagerplatzVorschlag.cs`, `Services/AusbuchungBestandsregel.cs` |
| v1.99.54: Mengenfelder: Pfeil/Spinner ±1, Mausrad ohne Wirkung | **Testbereit** (`96b60901`), TS-179; nicht gemergt | [[2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung-teil-1-spec]] | `wwwroot/js/site.js` |
| v1.99.55: Tabellen: Umsortieren in 7 Listen | **Testbereit** (`f2fb8b43`), TS-180; nicht gemergt | [[2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung-teil-2-spec]] | `Models/ViewModels/ColumnDefinitions.cs`, `#view-config` der 7 Views |
| v1.99.56: Beschichtungsauftrag: Status-Knopf speichert Eingaben mit | **Testbereit** (`01a6429a`), TS-181; nicht gemergt | [[2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung-teil-3-spec]] | `Controllers/BeschichtungsauftraegeController.cs` (`Bearbeiten`), `Views/Beschichtungsauftraege/Details.cshtml`, `wwwroot/js/beschichtung-bemerkung.js` |
| v1.99.57: Lagerbestellung: Hinweis nennt Bestand auf nicht wählbaren Plätzen | **Testbereit** (`8bac07bd`), TS-182; nicht gemergt | [[2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung-teil-4-spec]] | `Controllers/WarehousePickingController.cs` (`VorschlagHinweis`) |
| v1.99.58: Kommissionierauftrag: eigene Spalte „Menge IST“ | **Testbereit** (`0f853eed` + QA-Fix `b79a4b1a`), TS-183; nicht gemergt | [[2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung-teil-5-spec]] | `Views/Picking/Bom.cshtml`, `ColumnDefinitions.Bom` |
| v1.99.62: Alle Tabellen – Streifen, Hover CI-Hellblau, Fokus-Zeile, dunkle Links/Farbtexte/Symbole (Ausnahme `table-plain`) | **Testbereit** (`7fbdd2b7` + Doku `d53cff1e`, QA `88116275`), TS-187; nicht gemergt, Paket 16 | `site.css` (globale `.table:not(.table-plain)`-Regeln), `TabellenZeilenfarbeTests`, [[2026-11-23-tabellen-streifen-hover-ci-hellblau-spec]], ADR [[0035-tabellen-zeilenfarben-global-ueber-bootstrap-zustandsvariablen]] |
| v1.99.63: Update-Skript – SQL/151 fehlende Indizes IDEAL-Prod als Haltepunkt-Reparatur, Duplikat-Vorprüfung | **Testbereit** (`9dbc6ef2`, QA bestanden, Brain `84164928`), TS-188; nicht gemergt, Paket 17 | `SQL/151_RepairFreshInstallIndexes.sql`, `scripts/deploy/nachlauf.psd1` (`Pruefung/FreshInstallIndizes*`); [[2026-11-24-v1-99-63-sql-151-fresh-install-indizes]] |
| v1.99.65: Beschichtungsauftrag – Erstellen ohne Auswahl (alle freien, offen), Rückfrage beim Übergeben (entfernen oder beibehalten), Entfernen in Übergeben nur für nicht verpackte, Verpackt/Entfernen schließen sich aus | **Testbereit** (`8ffa4c4a`, QA `186e63dd`), kein SQL, TS-162.21–162.25; nicht gemergt | [[2026-11-25-beschichtungsauftrag-vorschlag-ohne-auswahl-spec]]; `Services/BeschichtungsauftragWorkflow.cs` (`AnlegenAsync`, `Entfernbar`, `BearbeitenAsync`), `Views/Beschichtungsauftraege/Details.cshtml`, `wwwroot/js/beschichtung-bemerkung.js`; [[2026-11-25-v1-99-65-beschichtungsauftrag-vorschlag-ohne-auswahl]] |
| v1.99.66: Sage-Buchungen – Lagerbuchungen und Sage-Wareneingänge auf einer Seite (Herkunft, gemeinsamer Anzeigestatus, „Status im Weg“, 30-Tage-Zeitraum + offene immer); löst die Listen „Sage-Lagerbuchungen“ (v1.28.0) und „Sage-Wareneingänge“ (v1.99.4) ab, alte Routen 301 | **Testbereit** (`0725fc3e`, Foto `2c20cfea`, QA `56851a66`), kein SQL, TS-189; nicht gemergt | [[2026-11-23-sage-buchungen-gesamtsicht-spec]]; `Services/SageBuchungenGesamtsicht.cs`, `Controllers/SageBookingQueueController.cs` (`Index`), `Filters/RequireStockReadOrEinkaufAccessAttribute.cs`, `Views/SageBookingQueue/Index.cshtml`; [[2026-11-25-v1-99-66-sage-buchungen-gesamtsicht]] |
| v1.99.67: Beschichtungsauftrag – VaterFA in der Positionsbezeichnung, kein Übergeben mit 0 Positionen („Entwurf stornieren“ im Fenster), Knöpfe nach Validierungsabbruch frei | **Testbereit** (`fcc894c6`, QA `2b4dceaf`), kein SQL, TS-162.26–162.29; nicht gemergt | [[2026-11-25-beschichtungsauftrag-vorschlag-ohne-auswahl-spec]] (Nachtrag); `BeschichtungsauftragWorkflow.PositionBezeichnung`, `BearbeitenAsync(…, ziel)`; [[2026-11-25-v1-99-67-beschichtungsauftrag-vaterfa-kein-uebergeben-ohne-positionen]] |
| v1.99.68: Sage-Übergabe mit Referenz (Kennung + Abteilung + Benutzer, ≤ 31) und Zeilen-Memo, Sage-WE-Kopf-Memo mit Benutzer, Auswahl „Abteilung“ bei Ausbuchung/Lagerplatz ausbuchen | **Testbereit** (`19244bfa`, QA 2026-11-25), **SQL/167**, TS-190; nicht gemergt; offen P-2 (Sage-Testsystem) | [[2026-11-24-sage-buchung-referenz-benutzer-abteilung-spec]]; `IDEALAKEWMSService/Services/SageReferenz.cs`, `SageLagerbuchungPayloadBuilder.cs`, `SageWareneingangPayload.cs`, `Controllers/StockMovementsController.cs` (`AbteilungPruefenAsync`), `Models/StockMovement.cs` (`Abteilung`); [[2026-11-25-v1-99-68-sage-buchung-referenz-benutzer-abteilung]] |
| v1.99.69: Leerer Beschichtungsauftrag-Entwurf wieder speicherbar (`[Bind(Prefix="mengen")]`), Sage-Buchungen WE-Zeilen nur mit Modul (auch Einkauf) | **Testbereit** (`eff16911`, QA 2026-11-25), kein SQL, TS-162.30, TS-189.10; nicht gemergt | [[2026-11-25-beschichtungsauftrag-vorschlag-ohne-auswahl-spec]], [[2026-11-23-sage-buchungen-gesamtsicht-spec]]; `BeschichtungsauftraegeController.Bearbeiten`, `SageBookingQueueController.Index` (`zeigtWe`), `DictionaryIntKeyBindingTests`; [[2026-11-25-v1-99-69-leerer-entwurf-und-we-nur-mit-modul]] |
| v1.99.70: Kommissionier-Nachtrag nachts (Dienst-Lauf), Knöpfe „Fehlende Kommissionieraufträge“ nur Admin, „Altbestand geprüft“ erzeugt sofort, Marker/Hinweis „Stückliste kleiner“ | **Testbereit** (QA bis Schlusslauf 2026-11-25 bestanden) (`fa1b754b`), **SQL/168**, TS-191; nicht gemergt | [[2026-11-25-kommissionierauftraege-nachtrag-nachts-spec]]; `IDEALAKEWMSService/Services/KommissionierNachtragService.cs`, `IdealAkeWms/Services/PickingOrderRegistrierung.cs` (`AddKommissionierauftraege`), `PickingOrderRestmengen.Ueberdeckt`, `PickingLeitstandController` (`AuftraegeNachziehen`, `AltbestandGeprueft`); [[2026-11-25-v1-99-70-kommissionierauftraege-nachtrag-nachts]] |
| v1.99.71: Lager und Lagerplätze im WMS erfassen, Dienst legt sie per Direkt-SQL über die Outbox in Sage an (Schalter `SageLagerplatzAnlageAktiv` aus, Stufe `SageLagerAnlageLagertechniken` Aus), Spalte „Übergabe-Fehler“, „Erneut senden“ | **Testbereit** (QA bis Schlusslauf 2026-11-25 bestanden) (`5b6fa696`), kein SQL, TS-192, ADR 0036; nicht gemergt | [[2026-11-24-lagerplatz-anlage-wms-nach-sage-spec]]; `StorageLocationsController.CreateInSage`, `IdealAkeWms/Services/SageLagerplatzAnlage.cs`, `IDEALAKEWMSService/Services/SageLagerplatzAnlageService.cs`, `SqlSageLagerplatzAnlage.cs`; [[2026-11-25-v1-99-71-lagerplatz-anlage-wms-nach-sage]], [[0036-lagerplatz-anlage-in-sage-per-direkt-sql-hinter-austauschbarer-schnittstelle]] |
| v1.99.72: BDE-Teileliste – Blechdicke, Material/Blechdicke in Summiert, Fräsen-Pfad je Artikel (`Articles.CamPath`, Link + „Pfad kopieren“, Standard ausgeblendet in Artikel-Liste, Bestände, Lagerbestellung, Teileliste), Pflege in Artikelinfo/Artikel bearbeiten (Rolle `fraesenpfad_pflegen`), Lagerbestell-Positionen an der Isolierungsfräse (Werkbank-Option, Termin Abschicken + N Arbeitstage), CSV ändert Filter nicht | **Testbereit** (QA bis Schlusslauf 2026-11-25 bestanden) (`d49a3f39`), **SQL/164, SQL/165**, TS-193, ADR 0038; nicht gemergt | [[2026-11-20-bde-liste-isolierungsfraese-pfad-material-spec]]; `Services/BdeTeilelisteService.cs`, `Helpers/FraesenPfad.cs`, `Views/Shared/_FraesenPfad.cshtml`, `ArticlesController.FraesenPfad` |
| v1.99.73: BDE-Masken Etappe 1 – Teileliste Einzeln mit Spalte AG-Status (Symbole wie FA-Struktur, Teilmeldung = erledigt), Baugruppen-Teile als Zeilen (Knopf am Matchcode, Filter-Treffer im Teil), BDE mobil kompakt (Karten < 992 px, 44 px, Filter eingeklappt) | **Testbereit** (QA bis Schlusslauf 2026-11-25 bestanden) (`219a69c3`), keine Migration, TS-194; Epic InUmsetzung (Etappen 2–5 offen); nicht gemergt | [[2026-11-25-bde-masken-vereinheitlichung-spec]]; `Services/FaAgStatusService.cs`, `Services/BdeTeilelisteService.cs` (`TeilmeldungenAnwenden`, `TeilelisteZeile.Kinder`), `Views/Shared/_FaAgSymbole.cshtml`, `Views/BdeTeileliste/Index.cshtml`, `wwwroot/css/bde.css` (`.bde-tl-seite`) |
| v1.99.74: BDE-Masken Etappe 2 – Teile Summiert: Aufträge je Matchcode aufklappbar (FE-Termin aufsteigend, eigener Haken), Summen-Haken meldet alle offenen, aggregierte AG-Ampel je Kürzel | **Testbereit** (QA bis Schlusslauf 2026-11-25 bestanden) (`eecc47ac`), keine Migration, TS-195; Epic InUmsetzung (Etappen 3–5 offen); nicht gemergt | [[2026-11-25-bde-masken-vereinheitlichung-spec]]; `Services/BdeTeilelisteService.cs` (`Summieren`, `AgAggregieren`), `Views/BdeTeileliste/Summiert.cshtml`, `Views/BdeTeileliste/_TeilStatus.cshtml`, `wwwroot/js/bde-teileliste.js` |
| v1.99.75: BDE-Masken Etappe 3 – Teilfertigmeldung: Melden-Fenster Teilfertig/Fertig, Vorgabe Restmenge, „gemeldet X von Soll“ mit Meldungsliste, Rücknahme je Meldung, Block/Summen-Haken mit Rest, Baugruppe nicht final, Admin-Spalte Art; `BdeStandardsichtTeile` angelegt (ohne Leser) | **Testbereit** (QA bis Schlusslauf 2026-11-25 bestanden) (`94be5444`), **SQL/169**, TS-196, ADR 0037; Epic InUmsetzung (Etappen 4a, 4b, 5 offen); nicht gemergt | [[2026-11-25-bde-masken-vereinheitlichung-spec]]; `Services/BdeTeilelisteService.cs` (`MengenFuerArtOk`, `TeilelisteZeile.Meldungen/Abschluss/Rest`), `Controllers/BdeTeilelisteController.cs` (`Melden`, `art`), `Views/BdeTeileliste/_TeilStatus.cshtml`, `wwwroot/js/bde-teileliste.js`, `Services/BdeBookingService.cs` (`ReportCompletionCoreAsync` `istFinal`) |
| v1.99.76: BDE-Masken Etappe 4a – BDE-Seite mit Reitern Teile Einzeln/Summiert/FA, FA-Tabelle mit Anstempeln Rüsten/Produktion (auch AKE), Werkbank-Auswahl BDE-Standardsicht, Wirksamkeit an einem Ort ohne NurFA | **Testbereit** (QA bis Schlusslauf 2026-11-25 bestanden) (`878aa76d`), keine Migration, TS-197; Epic InUmsetzung (Etappen 4b, 5 offen); nicht gemergt | [[2026-11-25-bde-masken-vereinheitlichung-spec]]; `Services/WerkbankKuerzelFamilie.cs`, `Services/BdeFaAngebot.cs`, `Controllers/BdeTeilelisteController.cs` (`SichtAufloesen`, `Anstempeln`), `Views/BdeTeileliste/Fa.cshtml`, `Views/BdeTeileliste/_Kopf.cshtml`, `Views/ProductionWorkplaces/_BdeTeilelisteField.cshtml` |
| v1.99.77: BDE-Masken Etappe 4b – FA-Sicht: Meine Buchungen (laufend/pausiert, Zeiten und Details), Dialoge als gemeinsame Partial, Scan mit Markierung, Tätigkeit, Fertig melden an NurAg-Werkbänken | **Testbereit** (QA bis Schlusslauf 2026-11-25 bestanden) (`d61ea22c`), keine Migration, TS-198; Epic InUmsetzung; nicht gemergt | [[2026-11-25-bde-masken-vereinheitlichung-spec]]; `Services/BdeKopfBuchungen.cs`, `Views/BdeTeileliste/_KopfBuchungen.cshtml`, `Views/Shared/_BdeDialoge.cshtml`, `wwwroot/js/bde-fa-sicht.js`, `Controllers/BdeTeilelisteController.cs` (`markiert`) |
| v1.99.78: BDE-Masken Etappe 5 – Tageshistorie als eigene Seite (Buchungen + alle Teilmeldungen des Tages, Knopf + Menü), `BdeTerminal/Index` leitet immer auf die BDE-Seite, alte Terminal-View und `bde-terminal.js` gelöscht, `BdeTerminalMaxHauptFa` entfernt, Betriebsdoku BDE am Handy | **Testbereit** (QA bis Schlusslauf 2026-11-25 bestanden) (`fbe29238`), keine Migration, TS-199; alle Etappen des Epics umgesetzt, Epic InUmsetzung bis QA; nicht gemergt | [[2026-11-25-bde-masken-vereinheitlichung-spec]]; `Services/BdeTageshistorie.cs`, `Views/BdeTeileliste/Tageshistorie.cshtml`, `Controllers/BdeTeilelisteController.cs` (`Tageshistorie`), `Controllers/BdeTerminalController.cs` (`Index`) |
| v1.99.79: Sammel-Fixes Schlusslauf goal-lauf (F-1…F-37: Scan „läuft bereits“, Terminal-Prüfung der JSON-Aktionen, Tageshistorie-Rückkehr, Sub-FA/Laufzeit „Meine Buchungen“, 50014 mit PlatzID, Lagerkennung-Meldung, Mandant-Join, Blechdicke „–“, Kontrast) | **Testbereit** (QA bis Schlusslauf 2026-11-25 bestanden) (`f1f0e4f2`, `a4feb6af`), keine Migration, TS-200; nicht gemergt | [[2026-11-25-kommission-lagerplatz-bde-goal-lauf]]; `Services/BdeScanResolver.cs` (`Running`), `Controllers/BdeTerminalController.cs` (`FremdesTerminalAsync`), `IDEALAKEWMSService/Services/SqlSageLagerplatzAnlage.cs` |
| v1.99.80: Doku-Nachzug v1.99.70–79 – Hilfeseite BDE neu (BDE-Seite, Tageshistorie, Handy, Teilfertig), Anwender 01/03/10/11/17, Betrieb 00/03–09, `AGENTS.md` (Lagerplatz-Anlage Direkt-SQL, Schalter-Liste, Diagnose-Einstiege) | **Testbereit** (qa-agent 2026-11-25, `67868774` + QA-Korrektur `3c5a97df`, Publish 1.99.80), keine Migration, TS-201; nicht gemergt | [[2026-11-25-v1-99-80-doku-nachzug]], Mail-Entwurf [[2026-11-26-mail-wms-neuerungen-v1-99-70-80]]. **Doku-Einstiege:** Kommissionier-Nachtrag `docs/anwender/01` + `docs/betrieb/08` A-6.5; Lagerplatz-Anlage `docs/anwender/19` + `docs/betrieb/05` § 4b + A-8.7; Isolierungsfräse/Fräsen-Pfad `docs/anwender/17` + `03` § 8 + `docs/betrieb/04` § 5.6; BDE-Masken `docs/anwender/11` + `17` + `docs/betrieb/08` A-8.8/A-8.9 + `09` § 4.1 |
| v1.99.44: Sammel-Bugfix – Ziel-Kästchen Kommissionierliste, Menü „Empfänger“ auch bei Lager-/Glasbestellung und Blechfertigung, Prüfung `FaStrukturSchluesselDoppelt` nur RTRIM, Rollenname „Beschichtungsauftrag“ | **umgesetzt**, wartet qa-agent (`0fe4cbcf`…`2552e4f4`), kein SQL, TS-166; nicht gemergt | [[2026-11-19-v1-99-44-sammel-bugfix]], [[2026-11-19-goal-lauf-sammel-bugfix-v1-99-44]] | `IdealAkeWms/Views/FaHierarchyKommissionierListen/_KommissionierZiele.cshtml`, `IdealAkeWms/Views/Shared/_Layout.cshtml:374-384`, `scripts/deploy/nachlauf.psd1` |
| v1.99.42: Nachbesserungen – WiCAM-CSV exakt nach BF00000002 (Lagerplatz `,`, Hauptlagerplatz, Kürzel, Arbeitsschritte), Benutzer-Kürzel (Freigabe-Sperre ohne), Beschichtungsauftrag Kreditor nachtragen + Einmal-Skript + ruhiger Druck mit „Seite x von y“, Userfeld-Hinweis vorn, Sage-AG 2xx ohne Fund = unklar + bestätigen | **Testbereit** (qa-agent 2026-11-18) – `1a972e72`…`0af91b73` + QA-Fix `b5463336`, SQL/162+163, TS-163, Paket 7 `publish/paket7-v1.99.42`; DEV AKE+IDEAL v1.99.42; Manual-UAT offen, nicht gemergt | [[2026-11-13-blechfertigung-bf-auftraege-wicam-csv-spec]], [[2026-11-18-beschichtungsauftrag-ausbau-checkboxen-versand-druck-spec]], [[2026-11-18-sage-userfelder-als-einstellung-spec]], [[2026-11-17-arbeitsgaenge-aus-wms-in-sage-fa-anlegen-spec]], [[2026-11-18-v1-99-42-nachbesserungen]] | `IdealAkeWms/Services/BfCsv.cs`, `IdealAkeWms/Controllers/UsersController.cs`, `IdealAkeWms/Services/BeschichtungsauftragWorkflow.cs`, `IdealAkeWms/Services/SageUserfelder.cs`, `IDEALAKEWMSService/Services/SageFaArbeitsgangService.cs` |
| v1.99.41: Beschichtungsauftrag-Ausbau – Haken „Verpackt“ im Vorschlag (nur angehakte in den Auftrag) und am Auftrag, Positionen ohne FA, Status Versendet (Übergeben → Erledigt entfällt), Lackierer aus Kreditoren (IsCoater), Druck/PDF je Position mit Brief-Anschrift, Bemerkung-Absicherung | **Testbereit** (qa-agent 2026-11-18) – E1–E6 `5c042c3d`…`cf11a777` + QA-Fix `9b6b3e0d`, SQL/161, TS-162, Paket 6 `publish/paket6-v1.99.41`; DEV AKE+IDEAL v1.99.41; Manual-UAT + P-2/P-3/P-4 offen, nicht gemergt | [[2026-11-18-beschichtungsauftrag-ausbau-checkboxen-versand-druck-spec]], [[2026-11-18-v1-99-41-beschichtungsauftrag-ausbau]] | `IdealAkeWms/Services/BeschichtungsauftragWorkflow.cs`, `IdealAkeWms/Controllers/BeschichtungsauftraegeController.cs`, `IdealAkeWms/Views/Beschichtungsauftraege/Print.cshtml` |
| v1.99.36: Beschichtung AKE Nachbesserung (Kopf je WA, Maße mm aus Sage-Artikel, kommissionierte ausblenden + Häkchen, Hinweis, Sortierung, Modultext) und BDE-Kontrast (dunkle Schrift nur `.bde-terminal`) | In Umsetzung/QA (Bündel `8e0a346e`, `e40ecfcb`, nicht gemergt; Goal-Lauf [[2026-11-17-goal-lauf-sammel-abend]]) | [[2026-11-16-beschichtungslogik-je-standort-spec]] Nachtrag 4, [[2026-11-13-bde-vereinfacht-teileliste-blechfertigung-spec]] Nachtrag Kontrast | `Services/Beschichtung/BeschichtungFlacheQuelle.cs` (`KopfInfosAsync`, `Projektion`), `Views/FaHierarchyBeschichtung/_KommissioniertSchalter.cshtml`, `wwwroot/css/bde.css` |
| Wareneingang Teil 2 (Sage-WE-Beleg ELL, Rahmenabrufe EBZ) | **Entwurf** (Schranke 1, E4 blockiert: Schreibtest) | noch kein Code, [[2026-11-08-wareneingang-teil-2-sage-we-beleg-rahmenabrufe-spec]] |

> **Deploy:** Web; Dienst nur wegen `AppVersion.cs`. Keine Migration. Default aus; vor dem Einschalten Geräte einrichten (DE-Layout,
> Präfix, Enter, Tasten-Ausgabe).
> **Status:** **Testbereit** v1.66.2 (qa-agent 2026-10-22) — wartet auf Schranke 2 (Manual-UAT TS-99 inkl. 99.14–99.16 + Bündel-Merge).
> Offen zur Entscheidung: doppelter Klick-Handler am OSEON-Artikelknopf (QA Q3-2, vorbestehend).

### FA-Struktur live aus Sage (v1.71.0, direkt im Bündel)

Spec [[2026-10-30-sage-sdata-fa-ansicht-spec]], Umsetzung [[2026-10-30-sage-sdata-fa-ansicht-umsetzung]],
Changelog [[2026-10-30-v1-71-0-sage-sdata-fa-ansicht]]. TS-103. Folgeaufgabe [[2026-10-30-sage-sdata-artikelstueckliste-soll-spalte]].

| Baustein | Status | Code-Einstieg |
|---|---|---|
| Lese-Client SData RealTimeData (nur GET, or-Blöcke à 50, TLS je Anfrage) | **erledigt** `8c09ab87` | `Services/SageSdata/SageSdataReadClient.cs` |
| Baumaufbau Haupt-FA → Sub-FAs → Positionen, Meldungen statt stiller Zuordnung | **erledigt** `8c09ab87` | `SageSdataFaTreeBuilder.Build` |
| Seite „FA-Struktur (Sage live)“, Gate `SageSdataFaAnsichtAktiv`, Diagnose | **erledigt** `8c09ab87`, QA-Fix `0fe5e7c7` | `SageSdataFaController.Index`, `Views/SageSdataFa/Index.cshtml` |

> **Deploy:** Web; Dienst nur wegen `AppVersion.cs`. Keine Migration. `appsettings.Production.json` mit `SageLagerbuchung` im Web-Ordner.
> **Status:** **Testbereit** v1.71.0 (qa-agent 2026-10-30) — wartet auf Schranke 2 (Manual-UAT TS-103 am AKE-Testsystem + Bündel-Merge).

### Aktivitäts-Protokoll: Spaltenfilter über alle Einträge, deutsche Namen (v1.70.0, direkt im Bündel)

Spec [[2026-10-16-aktivitaetsprotokoll-filter-und-namen-spec]], Umsetzung [[2026-10-16-aktivitaetsprotokoll-filter-und-namen-umsetzung]],
Bug [[2026-10-28-aktivitaetsprotokoll-spaltenfilter-nach-paging-bug]], Changelog [[2026-10-28-v1-70-0-aktivitaetsprotokoll-filter-namen]]. TS-102.

| Baustein | Status | Code-Einstieg |
|---|---|---|
| Spaltenfilter in SQL vor Count/Skip/Take, Zeitpunkt tageweise, Seiten-Guard | **erledigt** `badbedcc` | `SyncLogRepository.ApplyColumnFilter`, `GetPagedAsync` |
| Deutsche Anzeigenamen an einer Stelle, Auswahlfeld alphabetisch, Filter über Anzeige- oder technischen Namen | **erledigt** `badbedcc` | `SyncLogServices.DisplayNames`/`DisplayName` |

> **Deploy:** Web; Dienst nur wegen `AppVersion.cs`. Keine Migration, kein Index. Vor Abnahme P-2 (Kollation) und P-3.
> **Status:** **Testbereit** v1.70.0 (qa-agent 2026-10-28, Nachbesserung `d7dc3d03`) — wartet auf Schranke 2 (Manual-UAT TS-102 + Bündel-Merge).

### QR-Code: Matchcode statt Artikelnummer (v1.68.0, direkt im Bündel)

Spec [[2026-10-23-qr-code-matchcode-statt-artikelnummer-spec]], Umsetzung [[2026-10-23-qr-code-matchcode-statt-artikelnummer-umsetzung]],
Changelog [[2026-10-27-v1-68-0-qr-matchcode]]. TS-101.

| Baustein | Status | Code-Einstieg |
|---|---|---|
| AppSetting `QrErstesFeld` (Artikelnummer/Matchcode), Seed, `/Settings` Allgemein, Wertprüfung, body-Attribut | **erledigt** `ca48b22d` | `SettingsController.SaveSettings`, `_Layout.cshtml` |
| Eine Matchcode-Suche (getrimmt, ohne Groß/Klein, Liste) für Endpunkt und Artikelinfo (mehrdeutig = Meldung) | **erledigt** `ca48b22d` | `ArticleRepository.GetByMatchcodeExactAsync`, `GET /api/articles/by-matchcode` |
| Matchcode-Zweig im globalen `processScannedValue`-Wrapper (alle Scan-Wege), drei Meldungen | **erledigt** `ca48b22d` | `barcode-scanner.js` |
| v1.69.0 (Nachtrag 2): Auswahllisten für Einstellungen mit festen Werten, neue Einstellung `QrTrennzeichen` (`;` \| `,`) für alle QR-Zerlegungen | **erledigt** `2c1e3a6d` | `AppSettingKeys.FesteWerte`, `qrTrennzeichen()` in `barcode-scanner.js`, [[2026-10-27-v1-69-0-qr-trennzeichen]] |

> **Deploy:** Web; Dienst nur wegen `AppVersion.cs`. Keine Migration. Standard `Artikelnummer` = bisheriges Verhalten.
> **Status:** **Testbereit** v1.69.0 (qa-agent 2026-10-27) — wartet auf Schranke 2 (Manual-UAT TS-101 inkl. 101.15–101.21 + Bündel-Merge).

### Vault- und enaio-Links konfigurierbar (v1.67.0, direkt im Bündel)

Spec [[2026-10-21-vault-links-per-schalter-spec]], Umsetzung [[2026-10-21-vault-links-per-schalter-umsetzung]],
Changelog [[2026-10-22-v1-67-0-vault-enaio-links-konfigurierbar]]. TS-100.

| Baustein | Status | Code-Einstieg |
|---|---|---|
| Eine Regel für Schalter (nur `false` sperrt), Vorlage mit Platzhalter, leerer Wert, URL-Kodierung | **erledigt** `b1cb87e1` | `Helpers/LinkVorlage.Build` |
| Vault: zehn Stellen über ein Partial, Artikelinfo mit Text; Stücklisten-Knopf bleibt | **erledigt** `b1cb87e1` | `Views/Shared/_VaultLink.cshtml` |
| enaio: Badges aus `EnaioLinkVorlage`, bei Schalter aus keine Badges | **erledigt** `b1cb87e1` | `Views/Shared/_EnaioDmsBadges.cshtml` |
| Laufzeit-Seed der vier AppSettings (AKE-Adressen), Gruppen „Vault (Zeichnungen)“ / „enaio (Dokumente)“ | **erledigt** `b1cb87e1` | `Program.cs`, `StandortSettingsCatalog` |
| enaio-Sync-Quelle `Sync:EnaioDmsViewName` (bis drei Teile, Prüfung vor dem `try`) | **erledigt** `b1cb87e1` | `EnaioDmsSyncService.ResolveSource`, `FaHierarchySql.ValidateAndQuote(…, maxSegments)` |
| Hilfe, README, TS-100, v1.67.0 | **erledigt** `9fe7553f`, `4bfa4576` (qa) | `Views/Help/Index.cshtml` |

> **Deploy:** Web und Dienst, keine Migration. AKE: nichts tun (Seed). IDEAL: Vault-Links beim Einspielen ausschalten; enaio
> entscheidet Gerald.
> **Status:** **Testbereit** (qa-agent 2026-10-22) — wartet auf Schranke 2 (Manual-UAT TS-100 + Bündel-Merge).

### Spaltenauswahl/Zahnrad: zentraler Toolbar-Host, Gruppenkopf, Speicherfehler (v1.65.0, direkt im Bündel)

Spec [[2026-10-14-spaltenauswahl-zahnrad-sammel-spec]], Umsetzung [[2026-10-14-spaltenauswahl-zahnrad-sammel-umsetzung]],
Changelog [[2026-10-21-v1-65-0-spaltenauswahl-zahnrad-toolbar-host]].

| Baustein | Stand | Code-Einstieg |
|---|---|---|
| A: `window.tableToolbarHost` (Marker `data-table-toolbar`, Fallback `="auto"` je Wrapper), Zahnrad + CSV darüber, 13 Views markiert | **erledigt** `f1108e94` | `wwwroot/js/site.js`, `column-preferences.js insertGearButton`, `csv-export.js attach` |
| B: Gruppenkopf/Leerzustand bleiben (`:scope > td[colspan]`-Skip) | **erledigt** `f1108e94` | `column-preferences.js setColVisibility` |
| C + Z1/Z2: Hinweis bei Speicher-/Lade-/Zurücksetzen-Fehler, `pagehide`-Flush mit keepalive | **erledigt** `f1108e94` | `column-preferences.js showPrefsMessage` |
| D: Anzeigename „Vorschlagsliste Beschichtung (…)“ | **erledigt** `f1108e94` | `ColumnDefinitions.cs` |
| Hilfe, Changelog, v1.65.0, TS-98 | **erledigt** `c27f3b6d` | `Views/Help/Index.cshtml` |

> **Deploy:** nur Web, keine Migration. Wirkt an **beiden** Standorten (AKE: Zahnrad wandert aus dem Filterkasten).
> **Status:** Testbereit (QA 2026-10-21, Build + 2138 Web- / 316 Service-Tests grün) → Schranke 2 mit dem Bündel.

### BDE-Terminal: Liste nach Haupt-FA, Scan ohne AG, Prio (v1.64.0, direkt im Bündel)

Spec [[2026-10-19-bde-terminal-liste-scan-ohne-ag-prio-spec]], Umsetzung [[2026-10-19-bde-terminal-liste-scan-ohne-ag-prio-umsetzung]],
Changelog [[2026-10-19-v1-64-0-bde-terminal-liste-scan-ohne-ag-prio]], Backlog [[2026-10-19-bde-terminal-liste-und-scan-ohne-ag]].

| Baustein | Stand | Code-Einstieg |
|---|---|---|
| Gruppen-Sortierung Prio/Termin, NotInScope Sub-FA, negative Mengen M1–M6, FA-Baum bevorzugte Werkbank, `BdeTerminalMaxHauptFa` | **erledigt** `63bf006c` | `BdeTerminalAuftraege.Ordnen`, `BdeScanResolver`, `BdeBookingService.IstNegativ`, `FaArbeitsschrittSymbole.NamenLookup`, `BdeTerminalController.Index` |
| Terminal-UI: Gruppen, „Weitere“, Markierung `aria-pressed`, Panel unter Karte 2, Scan ohne AG | **erledigt** `96e90402` | `wwwroot/js/bde-terminal.js` (`renderGroups`, `openGroup`, `markSelection`), `bde.css` |
| Hilfe, Changelog, README, v1.64.0, TS-97 | **erledigt** `5613e5a2` | `Views/Help/Index.cshtml` |
| QA-Fix Q1–Q3: Haken-Zeichen CSS, kein Fokus nach Scan, Markierung jenseits der Grenze | **erledigt** `a9558301` | `bde.css`, `bde-terminal.js` |

> **Deploy:** nur Web, keine Migration (AppSetting per Startup-Seed). **Status:** Testbereit (QA 2026-10-19, Build + 2111 Web- /
> 316 Service-Tests grün) → Schranke 2 mit dem Bündel.

### BDE Stufe 1: Werkbank-Familie, veraltete Sage-Werkbänke, nur AG-Fertigmeldung (v1.63.0, direkt im Bündel)

Spec [[2026-10-19-bde-stufe-1-werkbaenke-ag-fertigmeldung-spec]], Umsetzung [[2026-10-19-bde-stufe-1-werkbaenke-ag-fertigmeldung-umsetzung]],
Changelog [[2026-10-19-v1-63-0-bde-stufe-1-werkbaenke-ag-fertigmeldung]], Backlog [[2026-10-19-bde-lastenheft-abgleich]] (Stufe 1),
[[2026-10-19-werkbank-katalog-veraltete-sage-arbeitsplaetze]].

| Baustein | Stand | Code-Einstieg |
|---|---|---|
| Schema `SageMissingSince`, `BdeNurAgFertigmeldung` (SQL/97, FreshInstall) | **erledigt** `4416aac8` | `Models/ProductionWorkplace.cs` |
| (b) Kennzeichnung „nicht in Sage“ + Erkennung ohne gekennzeichnete; (a) mehrere Werkbänke → bevorzugte | **erledigt** `13681983` | `ProductionWorkplaceSyncService`, `WorkOperationStructureDetectionService`, `Services/WerkbankKuerzelFamilie.cs` |
| (a) Terminal/Scan/FA-Baum über Kürzel-Familie | **erledigt** `27fdec11` | `BdeApiController.GetAvailableOperations`, `BdeScanResolver`, `WorkOperationRepository.GetOpenByWorkplaceIdsAsync` |
| FA-a9 Familien-Prüfung, Start-Sperre, (c) Fertigmeldung | **erledigt** `8b51af5a` | `BdeBookingService.ReportCompletionAsync`/`EnsureWorkOperationInFamilyAsync`, `BdeTerminalController.ReportCompletion`, `bde-terminal.js` |
| FA-a8 Kürzel-Pflege, Stammdaten-Spalte, Option | **erledigt** `3efba9a9` | `ProductionWorkplacesController`, `ProductionWorkplaceRepository.UpdateAsync` |
| Hilfe, Changelog, README, v1.63.0, TS-96 | **erledigt** `501008f6` | `Views/Help/Index.cshtml` |
| QA-Fix Q1: `StartProductionForOrder` prüft vor Default-AG-Anlage | **erledigt** `b3daf566` | `BdeTerminalController.StartProductionForOrder` |

> **Deploy:** Web + Dienst + SQL/97 (additiv). IDEAL vor Scharfschalten DryRun + P1–P3/P6. **Status:** Testbereit (QA 2026-10-19,
> Build + 2088 Web- / 316 Service-Tests grün) → Schranke 2 mit dem Bündel.

### Stückliste: Matchcode + Lagerplatz-Limit (v1.62.0, direkt im Bündel)

Spec [[2026-10-16-stueckliste-matchcode-lagerplatz-limit-spec]], Umsetzung [[2026-10-16-stueckliste-matchcode-lagerplatz-limit-umsetzung]],
Changelog [[2026-10-16-v1-62-0-stueckliste-matchcode-lagerplatz-limit]], Bug [[2026-10-16-stueckliste-lagerplatz-zelle-tabellenbreite-bug]].

| Baustein | Stand | Code-Einstieg |
|---|---|---|
| M: Spalte Matchcode (IDEAL Struktur, sonst Artikelstamm; AKE Artikelstamm), Druck, ColumnDefinitions (+ warehouse-order) | **erledigt** `c5d9b2b7` | `FaHierarchyBomItem.Matchcode`, `PickingController.Bom`, `ReadOnlyBomBuilder` |
| L: Reihenfolge zentral, Zelle max. 3 + „+n weitere“-Popover, Spalte filterbar, CSV volle Liste | **erledigt** `c9bc382f`, `41c3c14e` | `StockMovementRepository.GetStockByArticleNumbersAsync`, `Views/Picking/Bom.cshtml` |
| Hilfe, Changelog, v1.62.0, TS-95 | **erledigt** `6a452892` | `Views/Help/Index.cshtml` |
| QA-Fix W-1: Bulk-Bestell-Fallback per col-key statt fester Zellpositionen | **erledigt** `9177db47`, TS `d75680f4` | `Views/Picking/Bom.cshtml` |
| Bugfix v1.64.1: keine Seiten-Querscrollleiste (`.table-responsive` relativ), genau ein Scrollbalken (Sentinel unter dem Wrapper, ResizeObserver) — global für alle Listen. Bug [[2026-10-19-tabelle-seitenbreite-doppelter-scrollbalken-bug]] | **Testbereit** `4179e38d`, QA `d52bc44d` | `wwwroot/css/site.css` `.table-responsive`, `wwwroot/js/site.js` `initStickyScrollbar` |

> **Deploy:** nur Web, keine Migration. **Status:** Testbereit (QA 2026-10-16, Build + 2043 Web- / 307 Service-Tests grün) →
> Schranke 2 mit dem Bündel.

### Arbeitsgang-Stammdaten mit der Sage-View abgleichen (v1.61.0, direkt im Bündel)

Spec [[2026-10-16-arbeitsgang-stammdaten-abgleich-aus-sage-view-spec]], Umsetzung [[2026-10-16-arbeitsgang-stammdaten-abgleich-aus-sage-view-umsetzung]],
Changelog [[2026-10-16-v1-61-0-arbeitsgang-abgleich-sage-view]].

| Baustein | Stand | Code-Einstieg |
|---|---|---|
| `Source` + `SageMissingSince`, Migration/`SQL/96`/FreshInstall, OSEON-Seed `ExternalSystem`, `UpdateAsync` nur Pflegefelder | **erledigt** `6993c774` | `Models/OperationConfig.cs`, `OperationConfigRepository.UpdateAsync` |
| Arbeitsgang-Abgleich (Dienst, EIN Gate, 3 Keys, SyncLog) | **erledigt** `bc134c15` | `IDEALAKEWMSService/Services/OperationConfigSyncService.cs` |
| Reiter: Spalte Quelle + Kennzeichnung „nicht in Sage“ | **erledigt** `7042efcb` | `OperationConfigsController`, `Views/OperationConfigs/Index.cshtml` |
| Zahnrad/Spaltenauswahl im Reiter | **erledigt** `459cb7b5` | `ColumnDefinitions.OperationConfigs` |
| Hilfe, Changelog, v1.61.0, README, TS-94 | **erledigt** `9731b38a` | `Views/Help/Index.cshtml` |
| QA-Fix: kein falsches „wieder in Sage“ nach gelöschtem Eintrag, Test Detached-Pfad | **erledigt** `8773ba4f` | `OperationConfigSyncService` |

> **Deploy:** Web + Dienst, Migration `SQL/96` (additiv, Voraussetzung `SQL/95`); P5 je Standort vorher. Schalter aus →
> AKE unverändert bis auf Spalte Quelle und Zahnrad. **Status:** Testbereit (QA 2026-10-16, Build + 2036 Web- / 307 Service-Tests
> grün) → Schranke 2 mit dem Bündel.

### IDEAL: Sage-Zugriffe auf die neuen WMS-Views (v1.60.0, direkt im Bündel)

Spec [[2026-10-14-ideal-sage-views-umstellung-spec]], Umsetzung [[2026-10-15-ideal-sage-views-umstellung-umsetzung]],
Changelog [[2026-10-15-v1-60-0-ideal-sage-views-umstellung]].

| Baustein | Stand | Code-Einstieg |
|---|---|---|
| FA-Views Default + Existenzprüfung (Fehler + Mail) | **erledigt** `b53b99dd` | `FaHierarchySyncService`, `FaHierarchySql.SourceExistsAsync` |
| Werkbank-Quelle per Key (Default `KHKPpsArbeitsplaetze`) | **erledigt** `b53b99dd` | `ProductionWorkplaceSyncService`, `SageArbeitsplatzReader` |
| Artikel-Quelle per Key (leer = alter Select) | **erledigt** `b53b99dd` | `SageArticleSql`, `SageImportService.SyncArticlesAsync` |
| 00er-Bereinigung (Einmal-Skript), README, v1.60.0, TS-93 | **erledigt** `2524bb03` | `SQL/Einmalig/2026-10-15_IDEAL-Sage-Views_Bereinigung00erArtikel.sql` |
| Fix v1.60.1: Artikel-View mit WMS-Spaltennamen, Hash-Wächter LegacySql | **erledigt** `63524941` (Changelog [[2026-10-15-v1-60-1-artikel-view-wms-spalten]]) | `SageArticleSql.BuildViewSql` |
| Fix v1.60.2: Arbeitsplatz-View mit eigenen Spalten (ohne Mandant/Aktiv) | **erledigt** `73b37e55` (Changelog [[2026-10-15-v1-60-2-arbeitsplatz-view-spalten]]) | `SageArbeitsplatzSql` (`SageArbeitsplatzReader.cs`) |

> **Deploy:** Web + Service, keine Migration; Prüfabfragen P1–P7 (Gerald), Umstellung per Einstellung, Skript nur IDEAL.
> **Status:** Testbereit (QA 2026-10-15) → Schranke 2 mit dem Bündel.

### Briefkopf: Fußzeile nach Firmen-Word-Vorlage (v1.59.0, direkt im Bündel)

Spec [[2026-10-13-briefkopf-fusszeile-word-vorlage-spec]], Umsetzung [[2026-10-14-briefkopf-fusszeile-word-vorlage-umsetzung]],
Changelog [[2026-10-14-v1-59-0-briefkopf-fusszeile]].

| Baustein | Stand | Code-Einstieg |
|---|---|---|
| Fußbereich `<footer class="briefkopf-fuss">` laden/prüfen/einbetten (`Fuss`, `HatFuss`) | **erledigt** `c2d54f35` | `Services/BriefkopfVorlageService.cs` |
| Fußzeile auf jeder Druckseite (V3), „Seite X von Y" bei genau einem Dokument | **erledigt** `c2d54f35` | `Views/Shared/_PrintBriefkopfFuss.cshtml`, `_PrintBriefkopfStyles.cshtml`, drei Druck-Views |
| Muster nach Word-Vorlage, README, Hilfe, v1.59.0, TS-92 | **erledigt** `c2d54f35` | `Branding/briefkopf.html` |

> **Deploy:** nur Web, keine Migration; IT ergänzt den Fußbereich in der Server-Vorlage (ohne: Druck wie bisher).
> `Branding\` im Programmordner, nicht `wwwroot\`. **Status:** Testbereit (QA 2026-10-14, Fix `d4108f1a`) → Schranke 2 mit dem Bündel.

### Beschichtungsauftrag als eigenes Objekt (v1.50.0, direkt im Bündel, ohne Oberfläche)

Spec [[2026-10-06-beschichtungsauftrag-ausbau-teil-3-spec]] (Teil 3 von 4), Umsetzung
[[2026-10-12-beschichtungsauftrag-ausbau-teil-3-umsetzung]], Changelog [[2026-10-12-v1-50-0-beschichtungsauftrag-objekt]].

| Baustein | Stand | Code-Einstieg |
|---|---|---|
| Modelle, DbContext, Repository (Vergabe-Abfrage), DI, TimeProvider | **erledigt** `1410fc5c` | `Models/Beschichtungsauftrag*.cs`, `Data/Repositories/BeschichtungsauftragRepository.cs` |
| Workflow: Nummernkreis, Statusautomat, Doppelvergabe, Anlegen/Bearbeiten/Hinzufügen, Reklamation | **erledigt** `1410fc5c`, QA-Fix `a5b27550` | `Services/BeschichtungsauftragWorkflow.cs` |
| Migration 94 dreifach (EF, `SQL/94`, FreshInstall zwei Stellen) + Drift-Wächter | **erledigt** `51c6bc79` | `SQL/94_AddBeschichtungsauftrag.sql`, `BeschichtungsauftragMigrationSqlTests` |
| v1.50.0, Changelog, TS-83 | **erledigt** `a1f7fcfd` | `docs/TESTSZENARIEN.md` TS-83 |

> **Deploy:** Web + **Migration 94** (SQL-Skript vor dem Recycle), kein Service. **Status:** Testbereit (QA + Nachlauf
> 2026-10-12, Build + 1568 Web- + 272 Service-Tests grün) → Schranke 2 mit dem Bündel. Oberfläche (Liste, Detail, Druck,
> Schreibrolle) folgt mit Teil 4; ein Workflow-Aufruf je Request.

### Beschichtungsaufträge bedienen (v1.51.0, direkt im Bündel)

Spec [[2026-10-06-beschichtungsauftrag-ausbau-teil-4-spec]] (Teil 4 von 4), Umsetzung
[[2026-10-12-beschichtungsauftrag-ausbau-teil-4-umsetzung]], Changelog [[2026-10-12-v1-51-0-beschichtungsauftraege-oberflaeche]].

| Baustein | Stand | Code-Einstieg |
|---|---|---|
| Schreibrolle `beschichtungsauftrag_schreiben`, Schreibfilter, Vergabe-Anzeige, PDF-Name mit Nummer | **erledigt** `3b5149af` | `Filters/RequireBeschichtungsauftragWriteAccessAttribute.cs`, `Services/BeschichtungsauftragVergabeAnzeige.cs` |
| Controller + ViewModels (Liste, Detail, Entwurf, Positionen, Status, Reklamation, Druck/PDF) | **erledigt** `8d60cb10`, QA-Fix `5764c664` | `Controllers/BeschichtungsauftraegeController.cs` |
| Views + Gruppenkopf Vorschlagsliste + Navigation + Briefkopf für den Auftrag | **erledigt** `17bbf3be` | `Views/Beschichtungsauftraege/*`, `Views/FaHierarchyBeschichtung/_VorschlagAuftraege.cshtml` |
| v1.51.0, Changelog, Hilfe, Rollenübersicht, TS-84, README | **erledigt** `5af265f4`, `91c77105` | `docs/TESTSZENARIEN.md` TS-84 |

> **Deploy:** nur Web, keine Migration (setzt Migration 94 aus Teil 3 voraus); Rolle `beschichtungsauftrag_schreiben`
> anlegen + zuweisen. **Status:** Testbereit (QA 2026-10-12, Build + 1619 Web- + 272 Service-Tests grün) → Schranke 2
> mit dem Bündel.

### Arbeitsgänge als Symbole im FA-Struktur-Baum (v1.52.0, Teil 1, direkt im Bündel)

Spec [[2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-1-spec]] (Teil 1 von 3), Umsetzung [[2026-10-12-fa-struktur-arbeitsgaenge-symbole-teil-1-umsetzung]], Changelog
[[2026-10-12-v1-52-0-fa-struktur-arbeitsgaenge-symbole]].

| Baustein | Stand | Code-Einstieg |
|---|---|---|
| Tokenisierung, Namens-Lookup, Zuordnung je Baumzeile, eine Katalog-Abfrage | **erledigt** `f1a447cb` | `Models/ViewModels/FaHierarchyTreeViewModel.cs` (`FaArbeitsschrittSymbole`), `FaHierarchyController.Index` |
| Spalte „Arbeitsgänge" im Baum + Symbol-Stil + Spalten-Wächter | **erledigt** `1c86978d`, QA-Fix `d6d6beb0` | `Views/FaHierarchy/_FaHierarchyNode.cshtml`, `site.css` `.fa-ag-symbol` |
| v1.52.0, Changelog, Hilfe, TS-85 | **erledigt** `8f15833c` | `docs/TESTSZENARIEN.md` TS-85 |

> **Deploy:** nur Web, keine Migration. **Status:** Testbereit (QA 2026-10-12, Build + 1636 Web- + 272 Service-Tests grün)
> → Schranke 2 mit dem Bündel. Status-Farben folgen mit Teil 2, BDE-Anzeige mit Teil 3.

### CSV-Export für alle Tabellen (v1.53.0, direkt im Bündel)

Spec [[2026-10-06-csv-export-alle-tabellen-spec]], Umsetzung [[2026-10-12-csv-export-alle-tabellen-umsetzung]], Changelog
[[2026-10-12-v1-53-0-csv-export-alle-tabellen]].

| Baustein | Stand | Code-Einstieg |
|---|---|---|
| Layout-Skript, Sortiervergleich, Pagination-/Stücklisten-Attribute, Wächter + Node-Kern | **erledigt** `2e6752a1`, QA-Fix `9ad794b1` | `wwwroot/js/csv-export.js`, `table-filter.js` `tableFilterCompareValues` |
| v1.53.0, Changelog, Hilfe, TS-86 | **erledigt** `400a7520`, `b43edb25`, `87db96c8` | `docs/TESTSZENARIEN.md` TS-86 |

> **Deploy:** nur Web, keine Migration. **Status:** Testbereit (QA + Nachlauf 2026-10-12, Build + 1767 Web- + 272
> Service-Tests grün) → Schranke 2 mit dem Bündel.

### Arbeitsgang-Stammdaten vereinheitlicht (v1.54.0, direkt im Bündel)

Spec [[2026-10-09-arbeitsgang-stammdaten-vereinheitlichen-spec]], Umsetzung [[2026-10-12-arbeitsgang-stammdaten-vereinheitlichen-umsetzung]], Changelog
[[2026-10-12-v1-54-0-arbeitsgang-stammdaten]]. Löst „AG-Konfiguration (Soll-Termine + Relevanz)“ (`Models/OseonOperationConfig.cs`)
ab; Vorbedingung für FA-Struktur-Symbole Teil 2/3.

| Baustein | Stand | Code-Einstieg |
|---|---|---|
| Umbenennung Code + Schema, Audit, Migration (guarded), `SQL/95`, FreshInstall, Drift-Wächter | **erledigt** `d4bb711a`, QA-Fix `d56a340e` | `Models/OperationConfig.cs`, `Migrations/20261012140548_RenameOperationConfigsAndAddAudit.cs`, `SQL/95_*` |
| Offset-Lookup, Liste „nicht konfiguriert“, Controller, Reiter-Partial, Redirect, Begriffe | **erledigt** `5ac5ea16` | `Data/Repositories/OperationConfigRepository.cs`, `Controllers/OperationConfigsController.cs` |
| v1.54.0, Changelog, Hilfe, TS-87 | **erledigt** `b5123529` | `docs/TESTSZENARIEN.md` TS-87 |

> **Deploy:** Web + **SQL/95** (DB-Backup, App-Pool vorher stoppen). **Status:** Testbereit (QA + Nachlauf 2026-10-12,
> Build + 1795 Web- + 272 Service-Tests grün) → Schranke 2 mit dem Bündel.

### Status je Arbeitsgang-Kürzel im FA-Struktur-Baum (v1.55.0, Teil 2, direkt im Bündel)

Spec [[2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-2-spec]] (Teil 2 von 3), Umsetzung [[2026-10-13-fa-struktur-arbeitsgaenge-symbole-teil-2-umsetzung]], Changelog
[[2026-10-13-v1-55-0-fa-struktur-arbeitsgaenge-status]]. Baut auf Teil 1 (v1.52.0) und den Arbeitsgang-Stammdaten (v1.54.0) auf.

| Baustein | Stand | Code-Einstieg |
|---|---|---|
| FA 4a: leerer Offset in der Stammdaten-Pflege abgewiesen | **erledigt** `539be27f` | `Controllers/OperationConfigsController.cs` (`OffsetFehlt`) |
| Gebündelte Abfragen (WorkOperations der Seite, erledigt/läuft) | **erledigt** `e33cdc83` | `WorkOperationRepository.GetForFaStatusAsync` / `GetBdeStatusAsync` |
| Reine Status-Ableitung (Zuordnung W-2/W-3, Gate, W-4, Soll/Offset, Kombigerät) | **erledigt** `7690d281` | `Models/ViewModels/FaArbeitsgangStatus.cs` |
| Controller-Gate, Status-Chips + Zeichen, Sammelhinweis, Kontextzeilen ohne `opacity` | **erledigt** `6d85810b` | `FaHierarchyController.Index`, `_FaHierarchyNode.cshtml`, `site.css` |
| v1.55.0, Changelog, Hilfe, TS-88 (+ TS-87.3) | **erledigt** `99a3b09e` | `docs/TESTSZENARIEN.md` TS-88 |

> **Deploy:** nur Web, keine Migration. Produktiv ohne Wirkung, solange `BdeAktiv` aus ist. **Status:** Testbereit (QA 2026-10-13, Build + 1826 Web- + 272 Service-Tests grün, Fixes `57b762a9`, `627c992a`) → Schranke 2 mit dem Bündel.

### Menü „Beschichtung“ gebündelt + Kurzanleitungen in der Hilfe (v1.56.0, direkt im Bündel)

Spec [[2026-10-13-menue-beschichtung-und-hilfe-kurzanleitungen-spec]], Umsetzung [[2026-10-13-menue-beschichtung-und-hilfe-kurzanleitungen-umsetzung]], Changelog
[[2026-10-13-v1-56-0-menue-beschichtung-hilfe]].

| Baustein | Stand | Code-Einstieg |
|---|---|---|
| Dropdown „Beschichtung“, Titel Vorschlagsliste, Hilfe-Karte + Kurzanleitungen, Wächter | **erledigt** `14d1600d` | `Views/Shared/_Layout.cshtml`, `Views/Help/Index.cshtml`, `BeschichtungsauftraegeViewTests` |
| v1.56.0, Changelog, TS-89, Menüweg TS-81/84 | **erledigt** `699586af` | `docs/TESTSZENARIEN.md` TS-89 |

> **Deploy:** Web (Service nur Versionsanzeige), keine Migration. **Status:** Testbereit (QA 2026-10-13, Build + 1831 Web- + 272 Service-Tests grün, Fix `d3746373`) → Schranke 2 mit dem Bündel. Folgeaufgabe [[2026-10-13-spalteneinstellungen-anzeigename-vorschlagsliste]].

### BDE-Terminal: offene Aufträge mit Arbeitsgang-Status (v1.57.0, FA-Struktur Teil 3, direkt im Bündel)

Spec [[2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-3-spec]] (Teil 3 von 3), Umsetzung [[2026-10-13-fa-struktur-arbeitsgaenge-symbole-teil-3-umsetzung]], Changelog
[[2026-10-13-v1-57-0-bde-terminal-arbeitsgang-status]].

| Baustein | Stand | Code-Einstieg |
|---|---|---|
| Gemeinsame Status-Regel + Darstellung, Terminal-Termin/Sortierung, API mit Status + Fremdbuchung „läuft“ | **erledigt** `4c5d4e66` | `Services/ArbeitsgangStatusRegel.cs`, `BdeApiController.GetAvailableOperations` |
| Terminal-Blöcke je Auftrag, v1.57.0, Hilfe, TS-90 | **erledigt** `d4f326ef` | `wwwroot/js/bde-terminal.js` `renderOrderBlocks`, `bde.css` |

> **Deploy:** nur Web, keine Migration. **Status:** Testbereit (QA 2026-10-13, Build + 1845 Web- + 272 Service-Tests grün) → Schranke 2 mit dem Bündel; FA-Struktur-Symbole Teil 1–3 damit komplett.

### Spaltenfilter-Suchsyntax — ein Parser für alle Listen (Epic, v1.58.0 Etappe A, direkt im Bündel)

Spec [[2026-09-23-suchsyntax-spaltenfilter-erweitern-spec]], Umsetzung [[2026-10-13-suchsyntax-spaltenfilter-erweitern-umsetzung]], Changelog
[[2026-10-13-v1-58-0-suchsyntax-spaltenfilter-etappe-a]].

| Baustein | Stand | Code-Einstieg |
|---|---|---|
| A: Grammatik-Kern C#/JS, SQL-Bauer, Apply über Kern, gemeinsame Testtabelle | **erledigt** `3fe70103` | `Services/ColumnFilterSyntax.cs`, `wwwroot/js/column-filter-syntax.js` |
| A: FA-Liste (SQL-Bauer, Freitext-Escaping, Sub-FA, Datum/Prio) | **erledigt** `fe8f531c` | `ProductionOrderRepository.ApplyLeitstandColumnFilter` |
| A: table-filter.js, Syntax-Hilfe, Hilfeseite, v1.58.0, TS-91.A | **erledigt** `6dc74c9b`, `e5d0467b`, QA `c33f010b`, Fix `10ccf1f5` | `wwwroot/js/table-filter.js` |
| B: übrige Stellen (Leitstand-C#, Artikel, Bewegungen, BDE, Fehlteile, OSEON gelöscht, Stückliste, FA-Baum), Typmarkierung + Register + Drift-Wächter, Rückbau Zwischenzustand A, v1.58.1, TS-91.B | **erledigt** `0e82363b`, `3816542d`, `404a1b49`, QA-Fix `ead16fc4` (HasAny mit Listentypen) | `Services/ColumnFilterSyntax.cs` (`ColumnFilterTypes`) | (Changelog [[2026-10-13-v1-58-1-suchsyntax-spaltenfilter-etappe-b]]) | `Services/ColumnFilterSyntax.cs` (`ColumnFilterTypes`), `ColumnFilterTypesDriftTests` |

> **Deploy:** nur Web, keine Migration; vorher P-1 (beide Server), P-2 (Kollation DB + je Spalte, Pflicht), P-3. **Status:** Etappe A + B umgesetzt, **QA Epic 2026-10-13 bestanden → Testbereit** (wartet auf Schranke 2: TS-91.A + TS-91.B, dann Merge des Bündels).

## Offen / nicht gemerged

| Vorhaben | Status | Quelle |
|---|---|---|
| **Produktiv-Deploy v1.25.0 + v1.26.0 inkl. Manual-UAT** | **TESTBEREIT — gemergt, Abnahme am Zielsystem offen** | [[2026-07-deploy-v1-25-0]] |
| IDEAL-Anpassungen (Sub-FA-Granularitaet, `SubOrderNumber` unique) neu nachbilden | Backlog — Branch `feature/ideal-anpassungen-v1` wurde nie gemergt und 2026-07-27 geloescht | `backlog/2026-07-27-ideal-anpassungen-neu-nachbilden.md` |
| `[ValidateAntiForgeryToken]` auf `AccountController.Logout` wiederherstellen | offen, wartet auf IIS-Bestaetigung des Antiforgery-Wurzel-Fix | [[2026-07-deploy-v1-25-0]] |
| Weitere Cleanup-Jobs nach dem 5-Schritte-Rezept | offen | `../docs/superpowers/specs/2026-07-15-cleanup-jobs-service-design.md` |
| `OverridePrePickingDays` (Werkbank) in die Terminberechnung einbeziehen | **GEMERGED** — v1.27.0, Variante A, in `main` (Merge-Commit `0548449`, 2026-07-28); Deploy steht aus. Regel in `Services/PrePickingDaysResolver.cs`, Fallstrick aufgeloest → [[fallstricke]] | [[2026-07-28-override-prepickingdays]] |
| **Sage-Lagerbuchungen (ausgehend WMS→Sage, Material Zugang/Entnahme)** | **GEMERGED** — v1.28.0 in `main` (Merge-Commit `d74d3f2`, 2026-08-05, Build+Tests gruen auf main). Queue + `SageBookingWorker`, Decorator-Enqueue, Migration 82/83. **Offen:** Deploy + Manual-UAT (extern blockiert durch Sage-SData-Serverdefekt, siehe Deploy-Notiz); `git push`; Worktree/Branch nach Deploy-Verifikation aufraeumen | [[2026-07-29-sage-lagerbuchungen-spec]], [[2026-08-03-deploy-v1-28-0-sage-lagerbuchungen]] |
| ↳ Nachtrag 2026-11-25: Liste „Sage-Lagerbuchungen“ und „Sage-Wareneingänge“ sind seit v1.99.66 die eine Seite „Sage-Buchungen“; seit v1.99.68 mit Referenz/Benutzer/Abteilung | **Testbereit** im Bündel, nicht gemergt | [[2026-11-23-sage-buchungen-gesamtsicht-spec]], [[2026-11-24-sage-buchung-referenz-benutzer-abteilung-spec]] |
| Server-seitiger Druck (Vollausbau): `PrintService` mit echtem Drucker testen, Kommissionier-Druck an den Arbeitsplatz-Drucker binden | Grundstruktur vorhanden | Alt-Notiz „Offene Aufgaben" |

### Ideen-Backlog (aus PROJECT_STATUS „Zukuenftige Funktionen", nie begonnen)

Unbewertet uebernommen — keine Spec, keine Zusage:

- Meldebestand-Mail nach Artikelgruppe oder Lagerhalle aufsplitten
- Lagerplaetze in Sage anlegen, wenn im WMS neue entstehen (heute nur Sage → WMS)
- ~~Bestandsbuchung per SQL in die Sage-DB zurueckschreiben~~ → als **SData-Lagerbuchung** umgesetzt
  (v1.28.0, [[2026-07-29-sage-lagerbuchungen-spec]]); Umbuchung/BDE-Kanal/Serien-Chargen bleiben offen
- XML-Bestandsbuchung nach OSEON
- Artikel-Zusatzinfos (Einheiten) synchronisieren
