---
type: codebase-karte
updated: 2026-07-27
---
# Services, Repositories, Decorators

Geschwister: [[module]] · [[controller]] · [[datenmodell]] · [[integrationen]]

## Repositories — `../../IdealAkeWms/Data/Repositories/`

Jeder Datenzugriff laeuft ueber ein Interface (`IXxxRepository`). Begruendung und
Decorator-Muster: [[0001-repository-pattern-mit-decorator-fuer-caching]].

**Cache-Decorators** (umschliessen die echte Implementierung, im DI registriert):

| Decorator | umschliesst | Zweck |
|---|---|---|
| `CachedBomRepository` | `BomRepository` | 5 min MemoryCache auf die teure Stuecklisten-Abfrage |
| `CachedSettingRepository` | `AppSettingRepository` | AppSettings werden auf fast jeder Seite gelesen |
| `CachedHolidayRepository` | `HolidayRepository` | Feiertage fuer Arbeitstags-Rechnung |
| `CachedOseonOperationConfigRepository` | `OseonOperationConfigRepository` | AG-Konfiguration fuer die Ampel |

**Repositories nach Bereich:**

| Bereich | Repositories |
|---|---|
| Fertigungsauftraege | `ProductionOrderRepository`, `ProductionOrderPickingStatusRepository`, `ProductionOrderBdeStatusRepository` |
| Vorbau | `FaWorkStepRepository`, `FaAttributeRepository`, `WorkStepRepository` |
| Stueckliste | `BomRepository` (Sage-View + OSEON-Fallback), `BomCacheRepository` (persistenter Cache), `PickingRepository` |
| Lager | `StockMovementRepository`, `StorageLocationRepository`, `ArticleRepository`, `ArticleCategoryRepository`, `ArticleAttributeRepository` |
| Bestellwesen | `WarehouseRequisitionRepository`, `PartRequisitionRepository`, `OrderRecipientRepository` |
| BDE | `BdeBookingRepository`, `BdeOperatorRepository`, `BdeActivityRepository`, `BdeTerminalRepository`, `WorkOperationRepository` |
| OSEON / enaio | `OseonProductionOrderRepository`, `OseonOperationConfigRepository`, `EnaioDmsDocumentRepository` |
| Benutzer / Konfiguration | `UserRepository`, `RoleRepository`, `WorkstationRepository`, `ProductionWorkplaceRepository`, `AppSettingRepository`, `ServiceSettingRepository`, `UserViewPreferenceRepository`, `SyncLogRepository`, `HolidayRepository` |

Wichtige Verhaltensregeln, die in Repositories stecken (Details in [[fallstricke]]):
- `ProductionOrderRepository.GetAllOrderedAsync()` **muss** `PickingStatus` per `.Include` laden.
- Alle Offen-Queries fuehren `IsDone || IsDonePicking` und `!IsCancelled`.
- `StockMovementRepository.GetCurrentStockAsync` / `GetStockByArticleNumbersAsync` sortieren
  zentral „Hauptlagerplatz zuerst" — ~9 Anzeige-Stellen erben das.
- `EnaioDmsDocumentRepository.GetByOrderNumbersAsync` sortiert Dokument-Links zentral nach
  Typ-Vorrang.
- `FaWorkStepRepository.GetWorkStepDetailPivotAsync` liefert den Leitstand-Pivot
  (orderId → Code → `FaWorkStepPivotCell`, 1000er-Chunking).

## Web-Services — `../../IdealAkeWms/Services/`

### Querschnitt / Infrastruktur

| Datei | Zweck |
|---|---|
| `CurrentUserService.cs` (`ICurrentUserService`) | Aktueller App-User, Rollen-Checks (`HasMasterDataAccessAsync`, `CanProcessLagerAsync`, `CanAccessLagerbestellungAsync`, …), `GetWindowsUserName()`, `GetDefaultPageSizeAsync()` |
| `PageSize.cs` | `Resolve` (User-Default → System-Default), `AllCap = 5000` |
| `ColumnFilterHelper.cs` | `ReadFromQuery`, Token-Parsing (OR `,`, NOT `!`), In-Memory `Apply<T>` |
| `PasswordService.cs` | Passwort-Hashing fuer lokale Konten |
| `ActiveDirectoryService.cs` (`IActiveDirectoryService`) | LDAP-Abfrage der Berechtigungsgruppe fuer den AD-Benutzer-Picker (`[SupportedOSPlatform("windows")]`) |
| `UserAgentHelper.cs` | `IsWindowsDesktop` — UA-Gate der SSO-Challenge |
| `WindowsAccountHelper.cs` | SAM-Namen normalisieren |
| `PrintService.cs` | Server-seitiger Druck |
| `NaturalPositionComparer.cs` | Natuerliche Sortierung von BOM-Positionen |
| `SyncLogger/` | `ISyncLogger`, `ISyncRun`, `SyncLogServices` (Service-Namen-Konstanten), `SyncLogger`, `SyncRun` → [[0010-aktivitaets-protokoll-mit-isolierten-dbcontexts]] |

### Fachlogik

| Datei | Zweck |
|---|---|
| `PickingTransferService.cs` | Umbuchung bei der Kommissionierung (**betroffen von jeder `MovementType`-Erweiterung**) |
| `ReadOnlyBomBuilder.cs` | Read-only Stueckliste (Vorbau, FA-Vervollstaendigung) |
| `CoatingDateCalculator.cs` | Beschichtungstermin — zentrale Formel fuer Leitstand **und** Abarbeitungsliste |
| `BusinessDayService.cs` | Arbeitstags-Rechnung (Kommissionier-/Vorkommissionier-/Beschichtungstage) |
| `WorkbenchFilter.cs` | Komma-OR-Semantik des Werkbank-Filters (`Matches`) |
| `GlasArticleGroupFilter.cs` | `NormalizeGroup`, `IsAllowedForType` — die zentrale Glas/Lager-Regel |
| `FaZusatzinfoStatus.cs` | `IstVerpacktOderAbgeholt` (Trim + OrdinalIgnoreCase) — gemeinsame Status-Wahrheit |
| `WarehousePickingPrintLayout.cs` | `CellText` — **eine** Quelle fuer Filter, Sort und Druck-Zelltext |
| `OseonDueDateCalculator.cs`, `OseonTrafficLightService.cs`, `Oseon/OseonGroupViewModelBuilder.cs`, `Helpers/OseonStatusHelper.cs` | Termine, Ampel und Baumaufbau der Teileverfolgung |
| `BdeBookingService.cs` | Buchungs-Lebenszyklus: Start/Resume/Pause/Finish, Mehrfach-Regeln, Guards |
| `BdeTimeSplitService.cs` | Zeit-Split bei parallelen Buchungen |
| `BdeShiftCalendarService.cs` | Schichten, Feiertage, Schichtende |
| `BdeDefaultWorkOperationService.cs` | Default-AG im vereinfachten Modus |
| `HolidayImportService.cs` | Feiertags-Import (date.nager.at) |

## Service-Projekt — `../../IDEALAKEWMSService/Services/`

### Sync-Services (jeder mit eigenem Aktivitaets-Protokoll-Lauf)

| Datei | Quelle → Ziel | Gate |
|---|---|---|
| `SageImportService.cs` | Sage → `ProductionOrders`, `Articles`, inkl. Hauptlagerplatz + FA-Reconciliation | `Sync:ProductionOrdersEnabled`, `Sync:ArticlesEnabled` |
| `FaZusatzinfoSyncService.cs` | Sage-View → `ProductionOrderExtraInfo` (+ Auto-Erledigt) | `Sync:FaZusatzinfoEnabled` |
| `LagerplatzSyncService.cs` | Sage → `StorageLocations` | `Sync:LagerplaetzeEnabled` |
| `LagerbestandSyncService.cs` | Sage → Korrektur-Buchungen + Nullsetzen verwaister Paare | `Sync:LagerbestandEnabled` |
| `OseonSyncService.cs` | OSEON → Auftraege, AGs, Artikelkategorien | `Sync:OseonTrackingEnabled`, `Sync:OseonArticleCategoryEnabled` |
| `EnaioDmsSyncService.cs` | enaio-View → `EnaioDmsDocuments` (Full-Sync, kein Delta) | `Sync:EnaioDmsEnabled` |
| `BomCacheSyncService.cs` | Stueckliste → `CachedBomHeader`/`CachedBomItem` (**raw SQL**) | `Sync:BomCacheEnabled` |
| `CoatingDetectionService.cs` | BOM-Cache → `HasCoatingParts` | `Sync:CoatingDetectionEnabled` |
| `FaWorkStepDetectionService.cs` | BOM-Cache → `FaWorkSteps` (nur-hinzufuegend) | `Sync:FaWorkStepDetectionEnabled` |
| `HolidaySyncService.cs` | date.nager.at → `Holidays` | `Sync:FeiertagSyncEnabled` |
| `PartRequisitionEmailService.cs` | Bedarfsmeldungen → Mail | `Sync:PartRequisitionEmailEnabled` |
| `WarehouseRequisitionEmailService.cs` | Lager-/Glasbestellungen → Mail | `Sync:WarehouseRequisitionEmailEnabled` |
| `BdeAutoPauseService.cs` | Schichtende → Buchungen pausieren | `BdeSchichtkalenderAktiv` |
| `ActivityLogCleanupService.cs` | Alte `SyncLogs` loeschen | `Cleanup:AktivitaetsprotokollAufbewahrungTage` |

### Reine Entscheidungs-Helper (unit-getestet — hier liegt die pruefbare Logik)

| Datei | Entscheidet |
|---|---|
| `ProductionOrderReconciler.cs` | Welche FAs storniert/reaktiviert werden (Guard bei leerem Read, Cap) |
| `LagerbestandZeroingPlanner.cs` | Welche (Artikel, Lagerplatz)-Paare auf 0 gesetzt werden |
| `ActivityLogCleanupPlanner.cs` | Stichtag der Protokoll-Bereinigung |
| `BomCacheCoverage.cs` | Auswertung „FA im Fenster / gecacht / ohne BOM" |
| `SageProductionOrderSql.cs` | `BuildUpsert` inkl. `COL_LENGTH`-Check fuer `SubOrderNumber` |
| `SageImportHelpers.cs` | Feld-Normalisierungen des Sage-Imports |

Dieses Muster ist Absicht: die raw-SQL-Strecken sind nicht InMemory-testbar, also liegt die
**Entscheidung** in einem reinen Helper daneben und nur die **Ausfuehrung** im SQL-Pfad.
Siehe [[fallstricke]] Abschnitt 8.

### Reader / Zulieferer

`SageBestandReader`, `SageLagerplatzReader`, `SageZusatzinfoReader` (je mit Interface) kapseln die
Fremdsystem-Reads. `MailService` (`IMailService`) versendet **immer** multipart/alternative.
`SyncErrorNotifier` (`ISyncErrorNotifier`) verschickt Fehlermails und wirft selbst nie.

### Workers

| Worker | Takt |
|---|---|
| `SyncWorker` | `WorkerSettings:SyncIntervalMinutes` (Default 15); Bloecke einzeln resilient gekapselt; `Sync:LagerbestandIntervalMinutes` erlaubt einen eigenen Takt |
| `NotificationWorker` | mit dem Sync-Takt |
| `CleanupWorker` | 24 h |

Konfigurations-Zugriff laeuft ueber `Common/ServiceSettings.cs` — **DB-first**, siehe
[[0008-servicesettings-db-first-mit-typisiertem-katalog]].

## Service-Einstellungen — vollstaendiger Katalog

**Single Source of Truth ist der Code**: `../../IdealAkeWms/Models/ServiceSettingDefinitions.cs`
(`.All`, **36 Keys**, Stand 2026-07-27 gegen den Code verifiziert). Gepflegt werden sie unter
`/ServiceSettings`; **die DB gewinnt**, appsettings-Werte sind nur Default-Referenz. Ein neuer Key
MUSS in den Katalog — sonst schlaegt der Drift-Guard-Test fehl.

### Sync-Bloecke

| Key | Typ | Default | Zweck |
|---|---|---|---|
| `Sync:ProductionOrdersEnabled` | Bool | `true` | Sage-FA-Import aktiv |
| `Sync:ArticlesEnabled` | Bool | `true` | Sage-Artikel-Import aktiv |
| `Sync:OseonArticleCategoryEnabled` | Bool | `false` | OSEON-Artikelkategorie-Sync (nach Artikel-Import) |
| `Sync:OseonTrackingEnabled` | Bool | `false` | OSEON-Tracking- + Werkbank-Sync |
| `Sync:EnaioDmsEnabled` | Bool | `false` | enaio DMS-Sync |
| `Sync:PartRequisitionEmailEnabled` | Bool | `false` | Bedarfsmeldungs-Mails |
| `Sync:WarehouseRequisitionEmailEnabled` | Bool | `false` | Lager-/Glasbestellungs-Mails |
| `Sync:LagerplaetzeEnabled` | Bool | `false` | Sage-Lagerplatz-Stammdaten |
| `Sync:LagerbestandEnabled` | Bool | `false` | Sage-Lagerbestand (Korrektur-Buchungen) |
| `Sync:LagerbestandIntervalMinutes` | Int | `0` | Eigenes Intervall; 0 = Worker-Standard |
| `Sync:LagerbestandNullsetzenMaxPerRun` | Int | `100` | **Cap**: mehr verwaiste Paare → kein Nullsetzen + Fehlermail |
| `Sync:ProductionOrderReconcileEnabled` | Bool | `false` | Verwaiste FAs stornieren (Opt-in) |
| `Sync:ReconcileMaxCancelPerRun` | Int | `100` | **Cap**: mehr Storno-Kandidaten → kein Storno + Fehlermail |
| `Sync:FaZusatzinfoEnabled` | Bool | `false` | FA-Zusatzinfos. **Achtung:** setzt FAs mit Sage-Status verpackt/abgeholt automatisch auf Komm-Erledigt — vorher DryRun |
| `Sync:FaZusatzinfoAutoDoneMaxPerRun` | Int | `100` | **Cap**: mehr Auto-Erledigt-Kandidaten → kein Setzen + Warn + Fehlermail |
| `Sync:CoatingDetectionEnabled` | Bool | `false` | Lackierteil-Erkennung als eigener Job |
| `Sync:FaWorkStepDetectionEnabled` | Bool | `false` | FA-Arbeitsgang-Erkennung aus dem BOM-Cache |

### BOM-Cache

| Key | Typ | Default | Zweck |
|---|---|---|---|
| `Sync:BomCacheEnabled` | Bool | `false` | BOM-Cache-Sync aktiv |
| `Sync:BomCacheWeeks` | Int | `8` | Wochen Fertigungstermin in die Zukunft |
| `Sync:BomCacheMaxOrders` | Int | `200` | Max. Auftraege im Cache (Cap → Warnung im Protokoll) |
| `Sync:BomCacheMaxAgeHours` | Int | `24` | Sicherheitsnetz: Re-Sync wenn Eintrag aelter |

### BDE, Feiertage, Bereinigung, Worker

| Key | Typ | Default | Zweck |
|---|---|---|---|
| `Sync:BdeAutoPauseIntervalMinutes` | Int | `60` | Takt der BDE-Auto-Pause |
| `Sync:FeiertagSyncEnabled` | Bool | `false` | Feiertags-Sync (Nager.Date) |
| `Sync:FeiertagCountryCode` | String | `AT` | Laendercode (ISO-3166 alpha-2) |
| `Sync:FeiertagRegion` | String | (leer) | Optionale Region, z. B. `AT-3` |
| `Sync:FeiertagJahreVoraus` | Int | `2` | Folgejahre im Voraus |
| `Cleanup:AktivitaetsprotokollAufbewahrungTage` | Int | `180` | Protokoll-Retention; **0 = nie loeschen** (still) |
| `WorkerSettings:SyncIntervalMinutes` | Int | `15` | Takt des SyncWorker |
| `WorkerSettings:NotificationCheckIntervalMinutes` | Int | `60` | Takt der Meldebestand-Pruefung |
| `WorkerSettings:SyncDryRun` | Bool | `false` | DryRun: rechnen und protokollieren, nicht schreiben |

### Mails

| Key | Typ | Default | Zweck |
|---|---|---|---|
| `ErrorNotification:Enabled` | Bool | `false` | Fehlermail bei Sync-Fehlern |
| `ErrorNotification:Recipients` | String | (leer) | Empfaenger der Fehlermails (kommagetrennt) |
| `Notifications:MeldebestandEnabled` | Bool | `true` | Meldebestand-Mail aktiv |
| `Notifications:MeldebestandSubject` | String | „Meldebestand unterschritten — IDEAL AKE WMS" | Betreff |
| `Notifications:Recipients` | String | (leer) | Feste Empfaenger (zusaetzlich zu Usern mit `NotifyOnReorderLevel`) |
| `Notifications:AppBaseUrl` | String | (leer) | Basis-URL fuer Links in Mails |

### Bewusst NICHT im Katalog (appsettings-only)

`ConnectionStrings:*`, `MailSettings:*` (SMTP) und `Security:AdDomain` — Verbindungs- und
Domaenengeheimnisse gehoeren nicht in eine per UI editierbare Tabelle. Begruendung:
[[0008-servicesettings-db-first-mit-typisiertem-katalog]].

> **Deploy-Pflicht:** Nach jedem Deploy `/ServiceSettings` einmal durchgehen — appsettings-Werte
> greifen nicht mehr, jeder gewuenschte Sync muss hier aktiv sein
> (`../../docs/TESTSZENARIEN.md` Kap. 51).

Die fachlichen Feature-Toggles der Web-App (`AppSettings`-Tabelle, `/Settings`) sind eine
**andere** Gruppe — Liste in `../README.md` → „AppSettings", Konzept in
[[0011-feature-toggles-ueber-appsettings]].

## `SageBookingWorker` — vierter BackgroundService (v1.28.0)

Neben `SyncWorker`, `NotificationWorker`, `CleanupWorker` ein **vierter**, unabhaengiger
`BackgroundService` (`IDEALAKEWMSService/Program.cs`) — bewusst getrennt, weil er als einziger
**sekundengetaktet** laeuft (die drei anderen sind minutenskaliert) und ein Sage-Ausfall bei den
Buchungen die anderen Sync-Bloecke nicht ausbremsen darf. Verarbeitet die `SageBookingQueueItems`:
Reconciliation-Sweep → Recovery haengender `Gesendet` (Sage-Memo-Lookup) → offene senden (`Gesendet`
VOR dem HTTP-Call) → Fehler-Cap-Mail. Details [[integrationen]] / [[2026-07-29-sage-lagerbuchungen-spec]].

**Neue ServiceSettings-Keys** (Kategorie „Sage-Lagerbuchung", DB-first): `SageLagerbuchungAktiv`
(Bool, Default false), `SData:BaseUrl`, `SData:Dataset`, `Sync:SageLagerbuchungIntervalSeconds` (20),
`Sync:SageLagerbuchungBatchSize` (50), `Sync:SageLagerbuchungMaxRetries` (5),
`Sync:SageLagerbuchungMaxErrorsPerRun` (50), `Sync:SageLagerbuchungStuckMinutes` (10),
`SageLagerbuchungSslZertifikatPruefen` (Bool, Default true — TLS-Zertifikatspruefung des Sage-Clients,
nur fuer Testsysteme abschaltbar). Credentials appsettings-only (`SageLagerbuchung:Username/Password`).
Neuer `SyncLogServices.SageLagerbuchung`.

> **TLS-Schalter:** `SageLagerbuchungSslZertifikatPruefen` wirkt **nur** auf den
> `ISageLagerbuchungClient` (`ConfigurePrimaryHttpMessageHandler`), kein globaler `ServicePointManager`.
> Der Zertifikats-Callback liest den Wert **zur Laufzeit** (nicht bei DI-Registrierung) → Aenderung
> ohne Dienst-Neustart. Fail-safe (`SageTlsPolicy.ShouldVerifyCertificate`): fehlend/unparsebar →
> geprueft. Bei `false` Warnung im Worker-Start-Log + `/ServiceSettings` + `/SageBookingQueue`.

> **Ein-Instanz-Voraussetzung:** Der Idempotenz-Baustein („Status auf `Gesendet` vor dem Call")
> schuetzt nur bei **genau einer** laufenden Worker-Instanz — kein Doppel-Deploy/Failover auf
> derselben Queue. Siehe [[fallstricke]].
