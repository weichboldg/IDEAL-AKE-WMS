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
| UAT-Anpassung (Etappe 8, 2026-08-13) — SubFA=0-Blattannahme aufgehoben (alle Positionen mit Flag, Anomalie-Banner weg, Kommissionier+Vormontage) + Kommissionier-Summiert (je HauptFA/Artnr/Sollmenge/Ziel, KW auf `KO_Termin`) + Vormontage-Summiert-Wochenbezug (KW auf `FE_Termin`, HauptFA ignoriert) | **Testbereit** (v1.31.0, Commit `e83eb1c`) | Teil-3/5-Nachtraege in [[2026-07-29-standort-ideal-teil-3-spec]] / [[2026-07-29-standort-ideal-teil-5-spec]] | `KommissionierListenService`/`VormontageService` (`leafOnly:false`), neue `.../Summiert`-Views, `Services/IsoWeekRange.cs` |

> **Status:** UMGESETZT + QA-gruen im Worktree `feature/2026-08-07-ideal-teile-1-5` (zuletzt
> `1173cf1`, qa-agent 2026-08-18: Build gruen, Web 1219/1 skip/0 Fehler, Service 231/0 Fehler),
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
| A — Schema-Inversion (`SubOrderNumber` unique, `ParentSubOrderNumber`, `SageMissingSince`, Index-Tausch, Backfill) | **erledigt** (`fe7299b`) | `Models/ProductionOrder.cs`, Migration `20260814105526`, `SQL/90` |
| B — Einweg-Migrationstor `ProduktionsauftragHierarchisch` (Guard-Planer + Decorator-Choke-Point, Umschalt-Seite, Audit, Runbook) | **erledigt** (`aed9cb5..39f7813`) | `Services/HierarchischeStruktur/*`, `Data/Repositories/GuardedServiceSettingRepository.cs`, `Controllers/HierarchieUmstellungController.cs`, `docs/RUNBOOK-FA-HIERARCHIE-RUECKBAU.md` |
| C — Materialisierungs-Sync (3 Sync-Regeln, `SageMissingSince` selbstheilend, Sammelmail) | **erledigt** (`dc297d9..bc7e3d6`) | `IDEALAKEWMSService/Services/FaMaterializationPlanner.cs` + `FaMaterializationSyncService.cs`; SyncLog `FaMaterialization` |
| D — Lookup-Härtung (`GetAllByFaAndOperationAsync` + Multi-Hit-Log) + dreistufige Auto-Erledigt-Sperre + Reconcile-Test + `OrderNumber`-Sweep (7 kritisch → Teil 8) | **erledigt** (`fb07512..660a01b`) | `Data/Repositories/WorkOperationRepository.cs`, `IDEALAKEWMSService/Services/FaZusatzinfoSyncService.cs`, Key `Sync:FaZusatzinfoAutoErledigtEnabled` |
| E — Version-Bump v1.32.0 + Anwender-Changelog + Brain (ADR 0012, fallstricke §9, services/controller-Karte) | **erledigt** | `AppVersion.cs` (Web+Service), `Views/Help/Changelog.cshtml` |

> **Status Teil 7:** QA-gruen im Worktree (`status: Testbereit`, qa-agent 2026-08-17, HEAD `5cf802e`,
> Web 1186 (+1 skip) + Service 231 gruen). Master **default aus** — Umstellung ist ein bewusster
> spaeterer Schritt am Zielsystem. **Mapping datenabhaengig** (Wurzel `HauptFA==SubFA`, Node-Dezimal
> `NULL→0`) → erster echter Datenlauf, siehe manuelle Test-Checkliste in
> [[2026-07-29-standort-ideal-teil-7]]. Wartet auf **Schranke 2** (Manual-UAT + Merge). Merge bringt
> Teile 1–5 **und** Teil 7 zusammen (Schranke 2 fuers ganze Buendel, kein Zwischen-Merge).
>
> **Post-QA-Fix #2 (`5d3e723`, 2026-08-18):** beim echten App-Start freigelegter zweiter `SqlError 3701`
> — EF-`AlterColumn` auf `SubOrderNumber` erzeugte ungeschützten Auto-`DROP INDEX`; durch geschütztes
> Raw-SQL ersetzt (deckungsgleich SQL/90), offline via `dotnet ef migrations script` verifiziert.
> [[fallstricke]] §9 Nachtrag.

### Teil 8 — Sub-FA-Rückmeldung / BDE-Disambiguierung (v1.33.0, eigener Epic, selber Branch)

Macht Sub-FAs am **BDE-Terminal** bedienbar: nach der Inversion (Teil 7) ist `OrderNumber` mehrdeutig,
ein Scan kann mehrere Sub-FAs treffen. Einstieg `Services/BdeScanResolver.cs` +
`BdeApiController.ResolveScan` (`GET /api/bde/resolve-scan`) + `wwwroot/js/bde-terminal.js`.

| Etappe | Status | Code-Einstieg |
|---|---|---|
| 1 — Verifikation (Teil-7-Naht, OSEON-Urteil → Etappe 5 entfällt) | **erledigt** (`418f23a`) | `docs/TEIL-8-BDE-ETAPPE-1-VERIFIKATION.md` |
| 2 — Serverseitige Auflösungslogik `BdeScanResolver` (Exact/Ambiguous/NotInScope/NotFound, werkbank-gescopt) + 14 Unit-Tests | **erledigt** (`f86a886`) | `Services/BdeScanResolver.cs`, `Program.cs` |
| 3 — Scan-Auswahl-UI Normal-Modus + `resolve-scan`-Endpoint | **erledigt** (`5fd0070`) | `Controllers/BdeApiController.cs`, `Views/BdeTerminal/Index.cshtml`, `wwwroot/js/bde-terminal.js`, `wwwroot/css/bde.css` |
| 4 — NurFA-Fix (kein last-wins) + Teileverfolgung bestätigt | **erledigt** (`58c26bd`) | `wwwroot/js/bde-terminal.js`; `Controllers/TrackingController.cs` (nur Filter, unverändert) |
| 5 — OSEON-Seite | **entfällt** (Etappe-1-Urteil) | — |
| 6 — Version v1.33.0 + Anwender-Changelog + Testszenarien TS-66 | **erledigt** (`8395394`) | `AppVersion.cs` (Web+Service), `Views/Help/Changelog.cshtml`, `docs/TESTSZENARIEN.md` |

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
| Maske + Gruppen (Firmendaten/Mandant-Views/Toggles/Import) | **umgesetzt** (`c8ae47f`) | admin-only, Nav-Link, Bootstrap-Konsistenz `/ServiceSettings` |
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

> **Status:** `Testbereit` (qa-agent, 2026-08-18, Worktree `1173cf1`) — Build gruen, Web
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
| A — Anzeige-Fundament + ProductionOrders-Referenz + Z4-Erhebung | **erledigt** (`30b7a8c`/`9315186`/`abd2620`) | `ProductionOrderRepository.GetForLeitstandGroupedAsync`, `ColumnDefinitions` +`parent-sub-order-number`, `fa-liste-gruppierung.js`, `Views/ProductionOrders/_ProductionOrderRow.cshtml` |
| B — 5 weitere Views (FaCompletion/PickingLeitstand/Picking/FaWorklist/Tracking) | **erledigt** (`b150579`/`5b15caf`/`960d5c8`/`10921e2`/`7bba52b`) | je `_*Row.cshtml` + Master-Gate; Tracking/Index = 3-Ebenen-Baum (OSEON-Muster), Picking = Prio-Queue |
| C — Kaskade „Alle Sub-FAs fertigmelden" (Leitstand-Kopfzeile) | **erledigt** (`a305d6f`) | `PickingLeitstandController.CascadeDone/Preview`, `ProductionOrderBdeStatusRepository.SetIsDoneBdeForOrderNumberAsync`, `BdeBookingRepository.CountSubFasWithOpenBooking…` |
| D — Z4-Sweep (Articles/Info) + Z1-Regressionstest; **Z3 vertagt** | **erledigt** (`d9de60c`) | `ArticlesController`/`Views/Articles/Info.cshtml`, `FaMaterializationSyncServiceTests` (Z1); Z3 → [[2026-09-07-invariante-haupt-fertig-sub-erkennen]] |
| E — Testszenarien TS-69 + v1.35.0 + Changelogs | **erledigt** (`dde9a17`) | `docs/TESTSZENARIEN.md`, `AppVersion.cs` (Web+Service), `Views/Help/Changelog.cshtml` |

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
| 1 — Typen/Flag/Signaturwechsel | **erledigt** (`f89659f`) | `BomKey`, `BomScope`, `BomQueryResult.MengeIstAuftragsmenge`, `BomQuantityResolver`, 3 Aufrufer |
| 2 — `FaHierarchyBomRepository` | **erledigt** (`e99bcf3` + Fix `3784e4b`) | DirectChildren/FullStructure, Kollision `~n`, Waisen `W<n>`, Reverse-Lookup |
| 3 — Master-Weiche, Guard raus, Mapping | **erledigt** (`611475d`) | `BomRepositoryMasterSwitch`, `Program.cs:91-104`, `PickingController.Bom`/`ReadOnlyBomBuilder` |
| 4 — Klasse-D-Gates (Service) | **erledigt** (`56e8faa`) | `IHierarchicalModeReader`, Coating/WorkStep/BomCache×2 |
| 5 — Views (Bom.cshtml, Artikelinfo, Nav) | **erledigt** (`cd34b08`/`86371ee`/`a3f125e`) | `Views/Picking/Bom.cshtml` (+`#column-config`), `ColumnDefinitions.Bom`, `ArticlesController`/`Info.cshtml`, `_Layout` |
| 6 — v1.36.0, TS-70 (TS-68 abgelöst), Hilfe, DI-Test | **erledigt** (`8d9468d` + Fixwelle `22d31ae`) | `AppVersion.cs` ×2, `Help/Changelog.cshtml`, `Help/Index.cshtml`, `docs/TESTSZENARIEN.md`, `BomDiResolutionTests` |

> **Status:** `Testbereit` (qa-agent, 2026-09-08, Worktree-HEAD `22d31ae`) — Build 0 Fehler, Web
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
| Statuszeilen eager anlegen (behebt den Abnahme-Fehler) | **erledigt** (`ceaf5b3`) | `FaMaterializationSyncService.EnsureStatusRowsAsync` |
| Lack-Ableitung „selbst oder direktes Kind" | **erledigt** (`af25bcd`) | `FaMaterializationCoating.Derive` (rein, DB-frei) |
| Werkbank aus `Arbeitsbereich`, Variante B + drei Meldefaelle | **erledigt** (`ccc1d74`) | `FaMaterializationSyncService.ApplyWorkplace`, `IUnknownWorkplaceState` |
| Lack-Kennzeichen schreiben (inkl. `IsCoatingDone`-Reset) | **erledigt** (`77aaac2`) | `ProductionOrderPickingStatusRepository.SetCoatingPartsAsync` |
| Kombigeraet-Erkennung, einmal je Lauf | **erledigt** (`904e899`) | `FaHierarchySyncService.LogAmbiguousHauptFasAsync` |
| Kopfdaten gebuendelt laden | **erledigt** (`47c6712`) | `FaHierarchyOrderInfoRepository.GetByHauptFaKeysAsync` |
| Kunde-Freitext + Spaltenfilter ueber den Auftragskopf | **erledigt** (`79a174b` + `5022222`) | `ProductionOrderRepository.BuildLeitstandQuery` / `ApplyLeitstandColumnFilter` |
| K1 im Anzeige-Modell, Termin-Kaskade auf den Gruppenwert | **erledigt** (`f160b1d` + `b3d5a15`) | `ProductionOrdersController.MapItem`, `ProductionOrderListGroup` |
| Gruppen-Kopfzeile + Varianten-Tabelle | **erledigt** (`86cdc26`) | `Views/ProductionOrders/Index.cshtml` |
| v1.37.0, Changelog, Hilfe, TS-71 | **erledigt** (`1f038f6`) | `AppVersion.cs` ×2, `docs/TESTSZENARIEN.md` |
| Varianten-Tabelle klappt mit der Gruppe zu (Gesamt-Review) | **erledigt** (`be92ade`) | `Views/ProductionOrders/Index.cshtml` (`fa-liste-node-row`) |

> **Fünf stille Fehler im Lauf gefunden und behoben** — zwei davon erst beim Schreiben von Tests:
> fehlende Statuszeilen, weggefilterte Blattebene in der Lack-Ableitung, eine Entprellung, die ein
> wiederkehrendes Problem nie wieder gemeldet haette, ein **geratener** Beschichtungstermin bei
> leerem Kopfwert, und eine Kombigeraet-Varianten-Tabelle, die beim Zuklappen der Gruppe stehen
> blieb (Gesamt-Review). Keiner haette eine Fehlermeldung erzeugt.

> **Deploy-Vorbedingung:** Die Werkbaenke zu den vorkommenden Arbeitsbereichen **vor** dem Deploy
> anlegen — `ProductionWorkplaceId` ist ein Fremdschluessel, sonst bleibt die Werkbank im ersten Lauf
> leer (Zwei-Lauf-Ablauf). Deploy `web:true`, `service:true`, `migration:false`.

> **Status:** `Testbereit` (qa-agent, 2026-09-09, Worktree-HEAD `be92ade`) — Build 0 Fehler, Web
> **1284 grün + 1 skip**, Service **263 grün**, AK 1–13 gegen den echten Diff `a9ea910..be92ade`
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
| Spalte `ProductionOrder.Matchcode` + Migration 91 | **erledigt** (`461d3e5`) | `ProductionOrder.cs`, `SQL/91_AddProductionOrderMatchcode.sql` |
| Matchcode materialisiert (Anlege- + Update-Pfad, F1) | **erledigt** (`cbabdd9`) | `FaMaterializationSyncService.RunAsync` |
| Matchcode-Anzeige in fünf FA-Zeilen-Ebene-Listen | **erledigt** (`f4e9a6d`) | `ColumnDefinitions.cs`, je View `#column-config` + `<th>` |
| Matchcode-Filter null-sicher (F7), ohne `EF.Functions.Like` | **erledigt** (`48e1c87` + Fix `4a0563a`) | `ProductionOrderRepository.cs` (`BuildExtraInfoOrContains`) |
| K1-Kopfdaten je Zeile im Anzeige-Modell | **erledigt** (`9b2a258` + `13693b7`) | `ProductionOrdersController.MapItem` |
| Kunde-/K1-Postfilter C# statt SQL-Join (inkl. Flachmodus-Fix) | **erledigt** (`6ec37f8` + `cddb5b0` + `20686a2`) | `ProductionOrdersController.Index`, `PickingLeitstandController.Index` |
| Vier Zeilenspalten bedingt auf `Model.Hierarchical` | **erledigt** (`9c95fb0`) | `Views/ProductionOrders/Index.cshtml` + `_ProductionOrderRow.cshtml` |
| Selektor-Sweep (zweiter Anzeigefehler, eingeschoben) | **erledigt** (`b9bf342` + `94463f9`) | `table-filter.js`, `column-preferences.js`, `TableScriptScopedSelectorTests` |
| Wiederholungswerte unterdrückt, client-seitig | **erledigt** (`203e23a` + Fix `a422e44`) | `fa-liste-wiederholung.js`, `site.css` (`.fa-liste-repeat-hidden`) |
| HauptFA-Zeile gekennzeichnet, Kopfzeile verschlankt | **erledigt** (`fda7c7a`) | `Views/ProductionOrders/_ProductionOrderRow.cshtml` |
| `SetReleaseForOrderNumberAsync` (Kaskade, atomar) | **erledigt** (`067f1b1`) | `ProductionOrderPickingStatusRepository.cs` |
| `CascadeReleasePreview`/`CascadeRelease`, Picker-Pflicht unverändert | **erledigt** (`aad4b54`) | `PickingLeitstandController.cs` |
| Knopf + Dialog in der Gruppen-Kopfzeile | **erledigt** (`fef12cf`) | `Views/PickingLeitstand/Index.cshtml` |
| v1.38.0, Changelog, Hilfe, TS-73 | **erledigt** (`deff907`) | `AppVersion.cs` ×2, `docs/TESTSZENARIEN.md` |

> **AKE-Regression als harte Nebenbedingung:** Bei Master `false` bleibt die FA-Liste bit-identisch
> — die vier K1-Zeilenspalten sind **strukturell** (nicht nur inhaltlich) an `Model.Hierarchical`
> gebunden, der Matchcode bleibt dort leer, bis die hausinterne Sage-View-Erweiterung liefert
> (kein Fehler, eigener Backlog-Punkt).

> **Deploy:** `web:true`, `service:true` (Materialisierung ändert sich, nicht optional),
> `migration:true` (Migration 91, additiv, kein Backfill-Zwang — Bestandszeilen füllen sich erst
> beim nächsten Materialisierungs-Lauf).

> **Status:** `Testbereit` (qa-agent, 2026-09-10, Worktree-HEAD `deff907`) — Build 0 Fehler, Web
> **1322 grün + 1 skip**, Service **265 grün**, AK 1–20 gegen den echten Diff `be92ade..deff907`
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
| (a) `ProductionWorkplace`-Anlage aus Sage `KHKPpsArbeitsplaetze` (Sage fuehrend, Nummer/Name/Kuerzel) | **erledigt** `8be03982` | `ProductionWorkplaceSyncService`, `SageArbeitsplatzReader`, Migration `93`, Felder `SageArbeitsplatznummer`/`ArbeitsschrittCode` (eindeutiger gefilterter Index) |
| (a) 3 ServiceSettings (Enabled/Mandant/Ausschlussliste) + Standorteinstellungen-Gruppe + SyncWorker-Doppel-Gate + UI read-only | **erledigt** `8be03982`/`be0695fb` | `ServiceSettingDefinitions.cs`, `StandortSettingsCatalog.cs`, `SyncWorker.cs`, `Views/ProductionWorkplaces/*` |
| (b) 2c-Umbau `FaWorkStepStructureDetectionService` → `WorkOperationStructureDetectionService` (Ziel `WorkOperation`, Katalog `ProductionWorkplace.ArbeitsschrittCode`) | **erledigt** `920aabb7` | `WorkOperationStructureDetectionService.cs`, `IUnknownArbeitsschrittTokenState`, Key `Sync:WorkOperationStructureDetectionEnabled` |
| (b) Kuerzel-Mehrdeutigkeit gemeldet, Ausschlussliste-Kuerzel weder AG noch Meldung, `FaWorkStepSources.Struktur` entfernt | **erledigt** `920aabb7` | `WorkOperationStructureDetectionService.DetectAsync` |
| (c) Existenz-Check-Fix `BdeDefaultWorkOperationService` (OperationNumber "01" statt Name); `BdeScanResolver` UNVERAENDERT; Terminal-Routing schon ueber `ProductionWorkplaceId` | **erledigt** `920aabb7` | `BdeDefaultWorkOperationService.cs` |
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
| Existenz-Check beim Seitenaufbau (Knopf nur bei ≥ 1 AG) + Modal-Endpunkt je HauptFA | **erledigt** `912e6bea`/`71ffeb18` | `FaHierarchyController.Index`/`WorkOperations`, `IWorkOperationRepository.GetOrderNumbersWithWorkOperationsAsync`/`GetByOrderNumberWithWorkplaceAsync` (gleicher Schlüssel `OrderNumber == HauptFA`) |
| Modal (alle Sub-FAs inkl. Lücken + Zählzeile), Laden beim Öffnen | **erledigt** `71ffeb18` | `Views/FaHierarchy/_FaWorkOperationsModal.cshtml`, `FaWorkOperationsViewModel.Create`, `wwwroot/js/fa-hierarchy-tree.js` (`openWorkOperations`) |
| Tests + v1.45.0 + Changelog/Hilfe/TS-78; Review-Fix Knopf/Modal-Schlüssel | **erledigt** `2d06d1f0` | `WorkOperationRepositoryFaStructureTests` (4), `FaWorkOperationsViewModelTests` (1) |

> **Deploy:** nur Web, keine Migration. **Status:** QA → Schranke 2 mit dem ganzen Bündel.

### Kommissionierung nur am HauptFA — „Alle Ziele“, Rückbau Freigabe-Kaskade (v1.46.0, selber Branch)

Spec [[2026-09-25-kommissionierung-nur-hauptfa-spec]], Umsetzung
[[2026-09-25-kommissionierung-nur-hauptfa-umsetzung]], Changelog
[[2026-09-25-v1-46-0-kommissionierung-nur-hauptfa]]. Anlass [[2026-09-13-kommissionierung-nur-hauptfa]].
Folgenotizen: [[2026-09-25-picking-warteschlange-gruppierung-rueckbau]], [[2026-09-25-leitstand-subfa-readonly-stueckliste]].

| Baustein | Stand | Code-Einstieg |
|---|---|---|
| Eine Sub-FA-Definition + Warteschlange nur HauptFA + Bulk-Skip beidseitig | **erledigt** `2291c950`/`23942f73` | `ProductionOrder.IsHauptFa`/`IsSubFa`/`IsSubFaOf`, `ProductionOrderPickingStatusRepository` (4 Queue-Abfragen, `SetReleaseBatchAsync`), `BomScopes.ForOrder`, `HierarchicalDataExistsAsync` |
| Server-Guards + Zeilen Leitstand/FA-Liste auf HauptFA beschränkt | **erledigt** `35a65d36`/`8d8bf8a5` | `PickingController.Bom`, `PickingLeitstandController.ToggleRelease`/`BulkRelease`, `_PickingLeitstandRow`/`_ProductionOrderRow` |
| Rückbau Freigabe-Kaskade (aus [[2026-09-10-fa-liste-ausbau-matchcode-spec]] Block 4) | **erledigt** `36911f2f` | grep `CascadeRelease\|SetReleaseForOrderNumber\|CountReleasedByOrderNumber` = 0 |
| „Alle Ziele“ `!(leer)` im Komm.-Ziel-Filter + Druck-Leerzustand | **erledigt** `e2d6e5d9`/`4fffeae9` | `Views/Picking/Bom.cshtml` (`bomMatchesFilter`, `setupKommissionierzielDropdown`, `btnPrintBom`) |
| Normalisierung Profil-Standardfilter + Relevanzregel extrahiert | **erledigt** `f6911db4`/`7d1cd74e` | `KommissionierzielFilterWert.Normalize`, `KommissionierRelevanzFilter.IsRelevant` |
| v1.46.0, Changelog, Hilfe, TS-79, TS-73-Rückbau-Vermerk | **erledigt** `2290c99c`/`1f128c61`/`787c5c90` | `docs/TESTSZENARIEN.md` TS-79 |

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
| Summiert: KW optional, Matchcode/Hauptlagerplatz, Gruppen-Paging je HauptFA, Umschalter-Query | **erledigt** `ab2beed2` | `KommissionierListenService.AggregateSummiert`, `ToggleSharedQueryKeys`/`BuildToggleQuery` |
| ViewKey `FaHierarchyKommissionierSummiert` (6 Spalten) registriert | **erledigt** `e407596d` | `ColumnDefinitions.FaHierarchyKommissionierSummiert` |
| Druck/PDF: sichtbare Spalten beim Klick (`visibleColumns`), Server-Rückfall auf Präferenz; `PrintSummiert`/`PdfSummiert` | **erledigt** `1fb2b59d`/`1977a819` | `FaHierarchyKommissionierListenController.ResolveVisibleColumnsAsync`, `wwwroot/js/print-visible-columns.js`, `Views/FaHierarchyKommissionierListen/PrintSummiert.cshtml` |
| Menü → Summiert; Summiert gruppiert, 6 Spalten | **erledigt** `1977a819` | `_Layout.cshtml`, `Summiert.cshtml` |
| Drift-Guard Vier-Stellen-Konsistenz | **erledigt** `58d50738` | `IdealAkeWms.Tests/Views/KommissionierSpaltenKonsistenzTests.cs` |
| v1.47.0, Changelog, Hilfe, TS-80 | **erledigt** `e447a12c` | `docs/TESTSZENARIEN.md` TS-80 |

> **Deploy:** nur Web, keine Migration. **Vor der Abnahme:** Einheiten-Prüfschritt TS-80.1 auf der
> Sage-DB IDEAL ([[fallstricke]] §18). **Status:** Testbereit (QA 2026-09-28, Build + 1435 Web-Tests +
> 268 Service-Tests grün) → Schranke 2 mit dem ganzen Bündel. Druck-/PDF-Spalten, Gruppen-Darstellung
> und Umschalter sind **Manual-UAT** (TS-80).

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
> `f04a7e0~1..5f933f1`). Client-Mode = Bildschirm-Interaktion Manual-UAT, wartet mit dem Buendel auf
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
> Build/Test grün, AK-Abgleich gegen echten Diff `ab10b57..0aceee8`). Build 0 Fehler, **Web 1391 +1
> skip, Service 263** (−14 gelöschte Workplace-Tests). Wartet mit dem Bündel auf **Schranke 2**.

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
