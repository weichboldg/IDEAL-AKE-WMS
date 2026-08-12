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
Spec-Disziplin. Release-Details je Version: `changelog/` (v1.0.0 – v1.30.0).

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
| UI-Nachtrag (Etappe 6) — per-Benutzer-Spaltenauswahl an den 4 IDEAL-Listen + `table-filter.js`-Sort-Fix (je `<tbody>` separat) | **Testbereit** (v1.31.0, Commit `37e8752`) | [[2026-08-12-listen-spaltenauswahl-spec]] | `wwwroot/js/column-preferences.js` + `#view-config`/`#column-config` je View, `viewKey` in `Models/ViewModels/ColumnDefinitions.cs` `GetByViewKey`, `wwwroot/js/table-filter.js` (`sortTable`) |
| UI-Nachtrag (Etappe 7) — FA-Struktur seitenweite Tree-Table (OSEON-Stil) + Kontrast-Fix (WCAG AA) + 4 Knoten-Icons + Baum-Spaltenfilter + column-prefs | **Testbereit** (v1.31.0, Commits `961749f`+`767f06f`) | [[2026-08-12-fa-struktur-darstellung-spec]] | `Views/FaHierarchy/*`, `wwwroot/js/fa-hierarchy-tree.js`, `Services/FaNodeClassifier.cs`, `viewKey` `FaHierarchyStructure` in `ColumnDefinitions` |

> **Status:** UMGESETZT + QA-gruen im Worktree `feature/2026-08-07-ideal-teile-1-5` (Commit
> `93e54c4`), wartet auf **Schranke 2** (Manual-UAT am IDEAL-Testsystem + Merge durch den Menschen).
> Merge-Commit noch offen. Offene UAT-Punkte + Deploy-Handgriffe: [[2026-08-07-ideal-teile-1-5]].

## Offen / nicht gemerged

| Vorhaben | Status | Quelle |
|---|---|---|
| **Produktiv-Deploy v1.25.0 + v1.26.0 inkl. Manual-UAT** | **TESTBEREIT — gemergt, Abnahme am Zielsystem offen** | [[2026-07-deploy-v1-25-0]] |
| IDEAL-Anpassungen (Sub-FA-Granularitaet, `SubOrderNumber` unique) neu nachbilden | Backlog — Branch `feature/ideal-anpassungen-v1` wurde nie gemergt und 2026-07-27 geloescht | `backlog/2026-07-27-ideal-anpassungen-neu-nachbilden.md` |
| `[ValidateAntiForgeryToken]` auf `AccountController.Logout` wiederherstellen | offen, wartet auf IIS-Bestaetigung des Antiforgery-Wurzel-Fix | [[2026-07-deploy-v1-25-0]] |
| Weitere Cleanup-Jobs nach dem 5-Schritte-Rezept | offen | `../docs/superpowers/specs/2026-07-15-cleanup-jobs-service-design.md` |
| `OverridePrePickingDays` (Werkbank) in die Terminberechnung einbeziehen | **GEMERGED** — v1.27.0, Variante A, in `main` (Merge-Commit `0548449`, 2026-07-28); Deploy steht aus. Regel in `Services/PrePickingDaysResolver.cs`, Fallstrick aufgeloest → [[fallstricke]] | [[2026-07-28-override-prepickingdays]] |
| **Sage-Lagerbuchungen (ausgehend WMS→Sage, Material Zugang/Entnahme)** | **GEMERGED** — v1.28.0 in `main` (Merge-Commit `d74d3f2`, 2026-08-05, Build+Tests gruen auf main). Queue + `SageBookingWorker`, Decorator-Enqueue, Migration 82/83. **Offen:** Deploy + Manual-UAT (extern blockiert durch Sage-SData-Serverdefekt, siehe Deploy-Notiz); `git push`; Worktree/Branch nach Deploy-Verifikation aufraeumen | [[2026-07-29-sage-lagerbuchungen-spec]], [[2026-08-03-deploy-v1-28-0-sage-lagerbuchungen]] |
| Server-seitiger Druck (Vollausbau): `PrintService` mit echtem Drucker testen, Kommissionier-Druck an den Arbeitsplatz-Drucker binden | Grundstruktur vorhanden | Alt-Notiz „Offene Aufgaben" |

### Ideen-Backlog (aus PROJECT_STATUS „Zukuenftige Funktionen", nie begonnen)

Unbewertet uebernommen — keine Spec, keine Zusage:

- Meldebestand-Mail nach Artikelgruppe oder Lagerhalle aufsplitten
- Lagerplaetze in Sage anlegen, wenn im WMS neue entstehen (heute nur Sage → WMS)
- ~~Bestandsbuchung per SQL in die Sage-DB zurueckschreiben~~ → als **SData-Lagerbuchung** umgesetzt
  (v1.28.0, [[2026-07-29-sage-lagerbuchungen-spec]]); Umbuchung/BDE-Kanal/Serien-Chargen bleiben offen
- XML-Bestandsbuchung nach OSEON
- Artikel-Zusatzinfos (Einheiten) synchronisieren
