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
| AKE-Hotfix: Stuecklisten-Druck per POST (404.15) + Lagerbestellung Ist-Menge bestaetigen (Pflichtpruefung, Autosave NULL) | **Gemerged** (v1.30.1, Merge-Commit `f89c0124`, 2026-10-05; auf AKE deployt als `8cc23829`) | [[2026-09-28-ake-hotfix-stueckliste-druck-lagerbestellung-ist-spec]] | `PickingController.PrintBom`/`PrintBomPost`, `WarehouseRequisitionRepository.CloseAsync`, `Views/WarehousePicking/Details.cshtml`, `SQL/Einmalig/` ([[0015-einmal-datenskripte-ausserhalb-der-nummerierten-sql-reihe]]) |

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
| Server-seitiger Druck (Vollausbau): `PrintService` mit echtem Drucker testen, Kommissionier-Druck an den Arbeitsplatz-Drucker binden | Grundstruktur vorhanden | Alt-Notiz „Offene Aufgaben" |

### Ideen-Backlog (aus PROJECT_STATUS „Zukuenftige Funktionen", nie begonnen)

Unbewertet uebernommen — keine Spec, keine Zusage:

- Meldebestand-Mail nach Artikelgruppe oder Lagerhalle aufsplitten
- Lagerplaetze in Sage anlegen, wenn im WMS neue entstehen (heute nur Sage → WMS)
- ~~Bestandsbuchung per SQL in die Sage-DB zurueckschreiben~~ → als **SData-Lagerbuchung** umgesetzt
  (v1.28.0, [[2026-07-29-sage-lagerbuchungen-spec]]); Umbuchung/BDE-Kanal/Serien-Chargen bleiben offen
- XML-Bestandsbuchung nach OSEON
- Artikel-Zusatzinfos (Einheiten) synchronisieren
