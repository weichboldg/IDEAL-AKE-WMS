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
| `CachedOperationConfigRepository` *(v1.54.0, ersetzt die Zeile darueber)* | `OperationConfigRepository` | Arbeitsgang-Stammdaten: OSEON-Dictionary (case-sensitiv), `GetDueDateOffsetsAsync` (case-insensitiv, eigener Cache-Key), Invalidierung nach dem Schreiben |

**Repositories nach Bereich:**

| Bereich | Repositories |
|---|---|
| Fertigungsauftraege | `ProductionOrderRepository`, `ProductionOrderPickingStatusRepository`, `ProductionOrderBdeStatusRepository` |
| Vorbau | `FaWorkStepRepository`, `FaAttributeRepository`, `WorkStepRepository` |
| Stueckliste | `BomRepository` (Sage-View + OSEON-Fallback, AKE), `BomCacheRepository` (persistenter Cache, AKE), `FaHierarchyBomRepository` (IDEAL: beide Interfaces aus `FaHierarchyNode`, v1.36.0), `BomRepositoryMasterSwitch` (Weiche fuer `IBomRepository` **und** `IBomCacheRepository`, entscheidet pro Aufruf am Master, lazy Delegates — ersetzt den Guard `HierarchicalBomGuardRepository`), `PickingRepository` — siehe [[0013-bom-bridge-repository-schnittstelle-statt-cache-kopie]] |
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
| `PrintService.cs` | Server-seitiger Druck (`rundll32 mshtml.dll,PrintHTML`; aktuell von keinem Controller konsumiert) |
| `PdfRenderStatus.cs` | **Singleton**, Start-Probe (`Refresh()` in `Program.cs` nach `builder.Build()`): `msedge.exe`-Pfad aus `PdfRender:EdgePath` (gesetzt + fehlend ⇒ nicht verfuegbar) → Registry App Paths → Standardpfade → PATH; `IsAvailable` steuert den PDF-Knopf (v1.39.0, [[2026-08-06-pdf-erzeugung-fahierarchy-druck-spec]]) |
| `PdfRenderService.cs` (`IPdfRenderService`) | **Singleton** (Waechter `PdfRenderServiceDiResolutionTests`): HTML rein, `byte[]` raus. `SemaphoreSlim(PdfRender:MaxConcurrent)` wartet statt abzuweisen; GUID-Laufverzeichnis `%TEMP%\IdealAkeWms-Pdf\<guid>\` (`in.html`/`out.pdf`/`profile`), `finally`-Loeschen + verwaiste Laeufe (> Timeout+5 min) beim naechsten Lauf; Laufzeit-Log `PDF erzeugt: … ms`; `PdfRenderException` (`NotConfigured`/`Failed`) traegt die Nutzermeldung |
| `EdgeProcessRunner.cs` (`IEdgeProcessRunner`) | **Singleton**, einzige nicht InMemory-testbare Grenze (`EdgeProcessRunnerSmokeTests` ruft echtes Edge). Wartet auf die **fertige Datei** (exklusiv oeffenbar + `%%EOF`), nicht auf den Prozess — `msedge.exe` ist ein Launcher, siehe [[fallstricke]] §11; Timeout-Kill ueber das Startzeit-Fenster |
| `RazorViewRenderer.cs` (`IRazorViewRenderer`) | Scoped; rendert eine Razor-View + Model zu String (`ICompositeViewEngine.FindView` + `ViewContext`), damit dieselbe `Print.cshtml` an Browser **und** PDF geht |
| `PdfFileNameBuilder.cs` | reine Funktion `<Dokumentart>_<HauptFA>_<yyyyMMdd-HHmm>.pdf` |
| `NaturalPositionComparer.cs` | Natuerliche Sortierung von BOM-Positionen |
| `SyncLogger/` | `ISyncLogger`, `ISyncRun`, `SyncLogServices` (Service-Namen-Konstanten), `SyncLogger`, `SyncRun` → [[0010-aktivitaets-protokoll-mit-isolierten-dbcontexts]] |

### Fachlogik

| Datei | Zweck |
|---|---|
| `PickingTransferService.cs` | Umbuchung bei der Kommissionierung (**betroffen von jeder `MovementType`-Erweiterung**) |
| `ReadOnlyBomBuilder.cs` | Read-only Stueckliste (Vorbau, FA-Vervollstaendigung); seit v1.62.0 mit `IArticleRepository` + `ILogger` fuer die Matchcode-Spalte (`MatchcodeLookup`, IDEAL zuerst Strukturknoten) |
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
| `BdeScanResolver.cs` (`IBdeScanResolver`) | IDEAL Teil 8: werkbank-gescopte Auflösung eines gescannten FA-Segments auf einen buchbaren Sub-FA. `ResolveAsync` → Exact/Ambiguous/NotInScope/NotFound; Kandidat = Sub-FA (Normal per `WorkOperation`, NurFA per `ProductionOrder`). Aufgerufen von `BdeApiController.ResolveScan` (`GET /api/bde/resolve-scan`), Terminal-JS disambiguiert damit mehrdeutige `OrderNumber` nach der Schema-Inversion (Teil 7) |
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
| `BomCacheSyncService.cs` | Stueckliste → `CachedBomHeader`/`CachedBomItem` (**raw SQL**); **beide** Einstiege (`SyncBomCacheAsync`, `SyncSpecificArticleNumbersAsync` aus `SageImportService`) | `Sync:BomCacheEnabled` + **Klasse-D-Gate** (Master `true` → Skip, v1.36.0) |
| `CoatingDetectionService.cs` | BOM-Cache → `HasCoatingParts` (AKE-Heuristik: Artikelkategorie = `LackierteilKategorieName`) | `Sync:CoatingDetectionEnabled` + **Klasse-D-Gate**; seit v1.99.25 nur bei effektiver Kennzeichen-Quelle Artikelkategorie (`IHierarchicalModeReader.GetBeschichtungQuellenAsync` → `BeschichtungResolver.ResolveQuellen`), sonst Counts `uebersprungen_quelle_fastruktur`. Der Sage-Import-Hook (`SageImportService.NeueAuftraegeHookAsync`) braucht seit v1.99.25 ebenfalls `CoatingDetectionEnabled`. FA-Materialisierung schreibt das Flag nur bei Quelle FaStruktur, mit Audit `ModifiedBy = "FaMaterialization"` ([[0028-beschichtungsquelle-je-achse-ueber-appsettings-mit-automatisch]]) |
| `FaWorkStepDetectionService.cs` | BOM-Cache → `FaWorkSteps` (nur-hinzufuegend; AKE-Heuristik: `Bezeichnung`-Contains) | `Sync:FaWorkStepDetectionEnabled` + **Klasse-D-Gate** |
| `ProductionWorkplaceSyncService.cs` | Sage `KHKPpsArbeitsplaetze` → `ProductionWorkplace` (**legt Werkbaenke an**, Sage fuehrend fuer Nummer/Name/Kuerzel; Abweichung nachziehen+melden; Reader `SageArbeitsplatzReader`, raw ADO.NET); seit v1.63.0 kennzeichnet `SageMissingSince` (Nummer nicht mehr in der Quelle, rohe Zeilen, Empty-Guard, selbstheilend, nie loeschen; Counts `nicht_in_sage`, `nicht_in_sage_neu`, `wieder_in_sage`) | Doppel-Gate `ProduktionsauftragHierarchisch` + `Sync:ProductionWorkplaceSyncEnabled` (beide im `SyncWorker`) — v1.44.0 |
| `OperationConfigSyncService.cs` | Sage-View (`Sync:OperationConfigSyncViewName`, Spalten `Arbeitsgangnummer, Bezeichnung, AG`) → `OperationConfigs` (**legt nur an**: Offset 0, Quelle `Sage`; nie ueberschreiben/loeschen; einzige Bestandsaenderung Quelle `Manual` → `Sage`; Kennzeichnung `SageMissingSince` fuer Quelle ≠ `ExternalSystem`; je Eintrag speichern, `DbUpdateConcurrencyException` = Warnung + `fehler`). Reader `SageArbeitsplatzReader` wiederverwendet, reine Entscheidung `Plan(...)` | EIN Gate `Sync:OperationConfigSyncEnabled` (kein Master), Block nach der Werkbank-Anlage — v1.61.0, standortneutral |
| `WorkOperationStructureDetectionService.cs` | Struktur (`FaHierarchyNode.Arbeitsschritte`) → **`WorkOperation`** (nur-hinzufuegend; IDEAL: exakt ueber `ProductionWorkplace.ArbeitsschrittCode`, DirectChildren-Scope; unbekannt gemeldet, ausgeschlossen still; seit v1.63.0 Katalog ohne `SageMissingSince`, Kuerzel an mehreren Werkbaenken → an der bevorzugten angelegt, Count `arbeitsschritt_mehrere_werkbaenke`) | Doppel-Gate `ProduktionsauftragHierarchisch` + `Sync:WorkOperationStructureDetectionEnabled` (beide im `SyncWorker`, kein interner Gate) — v1.44.0, war `FaWorkStepStructureDetectionService`/v1.41.0 |

**Klasse-D-Gate (BOM-Bridge, v1.36.0):** `Common/IHierarchicalModeReader.cs` kapselt den DB-first-Read
`ServiceSettings.GetBoolSafeAsync("ProduktionsauftragHierarchisch")`; die drei Dienste (vier Einstiege)
ueberspringen im hierarchischen Modus **ohne DB-Zugriff** und protokollieren
`uebersprungen_hierarchisch`. Grund: die Heuristiken wuerden auf IDEAL-Daten Fehldaten erzeugen; IDEAL
liefert `Beschichtet`/`Arbeitsschritte` explizit. Konstruktor-Signatur: Reader steht **vor** `ILogger`,
`ISyncLogger` bleibt letzter Parameter. Siehe [[0013-bom-bridge-repository-schnittstelle-statt-cache-kopie]].
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
| `OseonOrderSelection.cs` | OSEON-Tracking-Sync: je Auftragsnummer (OrdinalIgnoreCase) eine Zeile = höchste OSEON-ID, AGs nur aus dieser Zeile je (Nummer, Position) einmal, Mehrfachlieferungen für die Meldung (v1.30.2, [[2026-11-07-oseon-sync-neu-ausgeschriebener-auftrag-spec]]) |

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
| `Sync:ArticlesViewName` | String | `""` | Quelle des Artikel-Syncs: leer = bisheriger Select (`SageArticleSql.LegacySql`, AKE), gesetzt = View (IDEAL `[vw_IDEAL_AKE_WMS_Artikel]`, erst nach P4). Fehlt die View: Warnung + Skip, keine Mail, kein Rueckfall — v1.60.0 |
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
| `Sync:WorkOperationStructureDetectionEnabled` | Bool | `false` | IDEAL: WorkOperation-Arbeitsgang-Erkennung aus der Struktur (Match ueber `ProductionWorkplace.ArbeitsschrittCode`; nach FA-Materialisierung + Werkbank-Anlage, nur bei Master) — v1.44.0, war `Sync:FaWorkStepStructureDetectionEnabled` |
| `Sync:ProductionWorkplaceSyncEnabled` | Bool | `false` | IDEAL: Werkbank-Anlage aus Sage `KHKPpsArbeitsplaetze` (nur bei Master) — v1.44.0 |
| `Sync:ProductionWorkplaceSyncViewName` | String | `KHKPpsArbeitsplaetze` | IDEAL: Sage-Quelle der Werkbank-Anlage (Tabelle oder View); Umstellung auf `[vw_IDEAL_AKE_WMS_Arbeitsgaenge]` nach P3. Fehlt die Quelle: Warnung + Skip, keine Mail — v1.60.0 |
| `Sync:ProductionWorkplaceSyncMandant` | Int | `1` | IDEAL: Mandant-Filter der Werkbank-Anlage (nicht das SData-Dataset) — v1.44.0 |
| `Sync:ProductionWorkplaceSyncAusschlussliste` | String | `STO,XXX,x01,BS,BS2,FRE,AKE` | IDEAL: Kuerzel, die NICHT als Werkbank angelegt und NICHT als unbekannt gemeldet werden — v1.44.0; gilt seit v1.61.0 auch fuer den Arbeitsgang-Abgleich (dort bei Read `null` Katalog-Default, nicht leer) |
| `Sync:OperationConfigSyncEnabled` | Bool | `false` | Arbeitsgang-Abgleich Sage-View → Arbeitsgang-Stammdaten (beide Standorte, EIN Gate) — v1.61.0 |
| `Sync:OperationConfigSyncViewName` | String | `[vw_IDEAL_AKE_WMS_Arbeitsgaenge]` | Sage-View des Abgleichs (Spalten `Arbeitsgangnummer, Bezeichnung, AG`); fehlt sie: Warnung + Skip — v1.61.0 |
| `Sync:OperationConfigSyncFremdsystemRelevant` | Bool | `false` | „Fremdsystem-relevant“ fuer NEU angelegte Kuerzel (IDEAL false, AKE nach Bedarf) — v1.61.0 |

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

## IDEAL Teil 7 — Materialisierung + Einweg-Migrationstor (v1.32.0)

Details/Entscheidung: [[0012-fa-hierarchie-einweg-migrationstor]]; Changelog [[2026-08-17-v1-32-0-ideal-teil-7]].

**Web (`IdealAkeWms/Services/HierarchischeStruktur/` + `Data/Repositories/`):**
- `HierarchischeStrukturGuard` (reiner Planer, unit-getestet) + `HierarchischeStrukturKeys`
  (geschuetzte Keys: Master `ProduktionsauftragHierarchisch` + `Sync:FaZusatzinfoAutoErledigtEnabled`).
- `GuardedServiceSettingRepository` — Decorator auf `IServiceSettingRepository.UpsertAsync/DeleteAsync`,
  **einziger Choke-Point** (DI-registriert um das echte `ServiceSettingRepository`, ADR 0008-Muster).
- `HierarchischeStrukturStatus` — Singleton, gecachter „ist gesperrt"-Anzeigezustand (Refresh am Start +
  bei GET der Umschalt-Seite; Schreibpfad prueft live). Siehe [[fallstricke]] §9 (Cache cross-process).
- `IProductionOrderRepository.HierarchicalDataExistsAsync()` (`AnyAsync(OrderNumber != SubOrderNumber)`).
- `IWorkOperationRepository.GetAllByFaAndOperationAsync` (mengenwertige Naht, AK 10; Einzel-Lookup loggt
  Mehrfachtreffer, Aufrufer-Umstellung = Teil 8).

**Service (`IDEALAKEWMSService/Services/`):**
- `FaMaterializationPlanner` (reiner Entscheidungs-Helper, unit-getestet) — 3 Sync-Regeln + Empty-Source-Guard.
- `FaMaterializationSyncService` — Orchestrator (gated auf Master, nach FA-Hierarchie-Sync im `SyncWorker`,
  liest lokale `FaHierarchyNodes`, EF-Apply mit App-Feld-Erhalt, Sammelmail via `IMailService`).
  **v1.43.0 (ADR-0014-Korrektur):** die Werkbank-Ableitung aus dem `Arbeitsbereich` (`ApplyWorkplace`,
  `IUnknownWorkplaceState`, Werkbank-Counter/-Mail) ist **zurückgebaut** — Arbeitsbereich = Zielort, keine
  Werkbank ([[2026-09-21-adr-0014-arbeitsbereich-ist-zielort-spec]]). Bleibt: Coating-Ableitung (Z1),
  Statuszeilen-Eager-Create, `SageMissingSince`.
- `FaZusatzinfoSyncService.SyncAsync(...)` erweitert um `bool autoErledigtEnabled` (dreistufige
  Auto-Erledigt-Sperre, AK 11).

**SyncLog-Services (`SyncLogServices`):** neu `HierarchieUmstellung` (Audit Master-Flip/Ablehnung),
`FaMaterialization` (Counts `angelegt/vermisst_neu/wieder_da/umhaengung_konflikt`),
`WorkOperationStructureDetection` (v1.44.0, war `FaWorkStepStructureDetection`/v1.41.0; Counts
`neu/uebersprungen/arbeitsschritt_mehrdeutig/arbeitsschritt_unbekannt/arbeitsschritt_unbekannt_auftraege`),
`ProductionWorkplaceSync` (v1.44.0, Counts `neu/aktualisiert/abweichung/uebersprungen/problematisch`),
`OperationConfigSync` (v1.61.0, Counts `neu/vorhanden/uebersprungen/name_mehrdeutig/nicht_in_sage/wieder_in_sage/quelle_sage/fehler`).

**Arbeitsgang-Erkennung Struktur (v1.44.0, [[2026-09-21-ideal-bde-arbeitsgaenge-aus-struktur-spec]]; ersetzt v1.41.0):**
- `WorkOperationStructureDetectionService` (2c-Umbau des frueheren `FaWorkStepStructureDetectionService`,
  v1.41.0 nie deployt) — leitet **`WorkOperation`**-Zeilen aus `FaHierarchyNode.Arbeitsschritte` ab,
  Match **exakt** ueber `ProductionWorkplace.ArbeitsschrittCode` (statt `WorkStep.Code`).
  `OperationNumber`=Kuerzel, `Name`/`ProductionWorkplaceId` aus der Werkbank kopiert; Eindeutigkeit
  `(ProductionOrderId, OperationNumber)`, Nur-hinzufuegen, kein Katalog-FK (kein Zwei-Lauf-Ablauf).
  Unbekannte Kuerzel: melden (S1 via `IUnknownArbeitsschrittTokenState`); ausgeschlossene (Ausschlussliste
  `Sync:ProductionWorkplaceSyncAusschlussliste`): still; mehrdeutige (mehrere Werkbaenke gleicher Code):
  gemeldet, kein Insert. Doppel-Gate im `SyncWorker` (nach FA-Materialisierung UND Werkbank-Anlage).
- `ProductionWorkplaceSyncService` — legt Werkbaenke aus Sage `KHKPpsArbeitsplaetze` an (Sage fuehrend),
  siehe Sync-Services-Tabelle oben. Der Existenz-Check in `BdeDefaultWorkOperationService` prueft seit
  v1.44.0 ueber `OperationNumber == "01"` (nicht ueber den Namen), damit eine NurFA-Buchung nie still auf
  einen echten, namensgleichen Arbeitsgang bucht.

**Neue ServiceSettings-Keys:** `ProduktionsauftragHierarchisch` (Bool, Default false, Kategorie
FA-Hierarchie — **Einwegtor**, guard-geschuetzt, in `/ServiceSettings` read-only) +
`Sync:FaZusatzinfoAutoErledigtEnabled` (Bool, Default true = AKE unveraendert; im hierarchischen Modus
nicht einschaltbar, Master-Flip schaltet ab).

## Sage-SData-Abgleich (v1.73.0, Epic [[2026-11-06-sage-sdata-stammdaten-sync-spec]])

Quellen-Weiche je Abgleich: [[0019-quellen-weiche-view-sdata-je-abgleich]]. Ordner `IDEALAKEWMSService/Services/Sdata/`.

| Bereich | Schnittstelle | SQL-Reader (unverändert) | SData-Reader | Weiche / Schalter |
|---|---|---|---|---|
| Lagerplätze | `ISageLagerplatzReader` | `SageLagerplatzReader` | `SdataLagerplatzReader` | `LagerplatzQuelleSwitch` / `Sync:Quelle:Lagerplaetze` |
| Lagerbestand | `ISageBestandReader` | `SageBestandReader` | `SdataBestandReader` | `BestandQuelleSwitch` / `Sync:Quelle:Lagerbestand` |
| Artikel | `ISageArtikelReader` (neu, aus `SageImportService`) | `SageArtikelReader` | `SdataArtikelReader` | `ArtikelQuelleSwitch` (gesperrt P-R1) / `Sync:Quelle:Artikel` |
| FA-Struktur | `ISageFaStrukturReader` (neu, aus `FaHierarchySyncService`) | `SageFaStrukturReader` | `SdataFaStrukturReader` (Kanten + Positionen; seit v1.99.26 Knotenfelder `FelderSetzen`, Köpfe `FaKoepfeLesenAsync` über `dtsFaBelegsuche`) | `FaStrukturQuelleSwitch` (Hybrid `HybridAsync`, RealTimeData nur im Vergleich `RealTimeDataErlaubt`; Umschalten gesperrt bis P-FS3, `SageQuelleSperre`) / `Sync:Quelle:FaStruktur` |
| Werkbank + Arbeitsgang | `ISageArbeitsplatzReader` | `SageArbeitsplatzReader` | `SdataArbeitsplatzReader` | `ArbeitsplatzQuelleSwitch` / `Sync:Quelle:Arbeitsplaetze` |
| Lagerbuchungs-Lookup | `ISageBuchungLookupReader` | `SageBuchungLookupReader` | `SdataBuchungLookupReader` | `BuchungLookupQuelleSwitch` / `Sync:Quelle:LagerbuchungLookup` |

- Basis: `SageQuelleWeiche` (`GenutzteQuelle`, `SdataHinweise`, `SdataGesperrtBis`), `SageQuelleSchalter`, `SdataVergleich`, `SdataLeseZielLeser`,
  `SdataRegistrierung.Add…Quelle()` (aus `Program.cs`). Client: `ISageSdataReadClient.QueryAllAsync` + `SdataLeseException` (`IdealAkeWms/Services/SageSdata/`).
- Lauf `SageQuellenvergleich` („Sage-Quellenvergleich (View/SData)“). Bereichsläufe schreiben Info „Quelle: …“.
- Neue Abgleiche (ohne View-Weg, Schalter Default false, je `RunResilientAsync`):
  - `PpsMitarbeiterSyncService` (`Sync:PpsMitarbeiterEnabled`, Schreiber `PpsMitarbeiterSync`, Cache `SagePpsMitarbeiter`; Counts gelesen, uebersprungen, nicht_zugeordnet, bde_ohne_sage_nummer, sage_nummer_unbekannt).
  - `StillstandsgruendeSyncService` (`Sync:StillstandsgruendeEnabled`, `StillstandsgruendeSync`; Counts gelesen, neu, aktualisiert, unveraendert, uebersprungen, nicht_in_sage, nicht_in_sage_neu, wieder_in_sage).
  - `KontokorrentSyncService` (`Sync:KreditorenEnabled`/`Sync:DebitorenEnabled`, `KontokorrentSync`, Läufe Kreditoren/Debitoren, `where KtoArt eq 'K'|'D'`; Counts gelesen, neu, aktualisiert, verknuepft, unveraendert, uebersprungen, nicht_in_sage, nicht_in_sage_neu, wieder_in_sage, moegliche_dubletten).
  - `ArtikelLieferantSyncService` (`Sync:ArtikelLieferantEnabled`, `ArtikelLieferantSync`, nach Artikel; Counts gelesen, neu, aktualisiert, unveraendert, entfernt, hauptlieferant, artikel_fehlt, kreditor_fehlt, uebersprungen).
  - `EinkaufBestellungenSyncService` (v1.84.0, `Sync:EinkaufBestellungenEnabled`, Lauf „Offene Bestellungen (Sage)“ = `SyncLogServices.EinkaufBestellungen`, nach Artikel-Lieferanten; nur `EinkaufPositionenInfo where Belegkennzeichen eq 'EBB'`; **seit v1.99.15 zwei Abfragen EBB + EBZ** (Rahmenabrufe, [[2026-11-11-wareneingang-teil-3-rahmenabrufe-spec]]), Empty-Guard je Belegart, zusätzliche Counts `positionen_ebb`, `positionen_ebz`; Counts positionen, koepfe, uebernommen, belegart_ausgeschlossen, artikel_fehlt, kreditor_fehlt, lagerplatz_fehlt, einheit_abweichend, lieferant_gesperrt, uebersprungen; Empty-Guard). **Seit v1.99.33** nach dem Empty-Guard `KoepfeAsync`: GET `dtsEKBelegMst.Sage.Wawi` (`KHKEKBelege_USER_AB`, `KHKEKBelege_USER_Kom`, `where` nur `BelID` in 50er-OR-Blöcken, `orderBy BelID`), Zuordnung über `BelID`; Counts `ab_nummer`, `kommission`, `kopf_ohne_treffer`, `kopf_zu_lang`; Feld fehlt / Lesefehler (Werte vom letzten Lauf) / zu lang → Warnung.
- G10: Lagerplatz-/Lagerbestand-Abgleich mailen Lesefehler nur bei Quelle SData/Vergleich (View wie bisher ohne Mail).
- Umbuchung: `SageLagerbuchungUmbuchungAktiv` (Default false); einzige Typliste `SageBookingEnqueueDecision` (`BookableTypes`, `Buchungsart`,
  `IstUmbuchungMitFaBezug`); Nacherfassung `EnqueueMissingAsync(..., umbuchungAktiv)`.
- Outbox `SageStammdatenOutbox` ohne Worker (G5); `LagerplatzSyncService` übernimmt manuelle Plätze mit Übergabe Offen/Fehler bei gleichem Code.

## Sage-Wareneingang (v1.99.4, [[2026-11-08-wareneingang-teil-2-sage-we-beleg-rahmenabrufe-spec]])

- `SageWareneingangService.RunOnceAsync` (Dienst), aufgerufen als eigener Schritt im `SageBookingWorker`-Takt, Gate **nur**
  `SageWareneingangBelegAktiv` (ServiceSetting, Default aus, AKE). Eine Gruppe (`SageWeVorgang`) = ein POST `$service/StandardBelegService`
  (`SageWareneingangClient`, Payload `SageWareneingangPayloadBuilder`, Belegübernahme EBB → ELL). Status vor dem POST auf Gesendet (+1 Versuch),
  Erfolg → Uebergeben + Belegnummer, Fehler bleibt (Variante B, kein Auto-Retry), haengend nach `Sync:SageLagerbuchungStuckMinutes` → Fehler
  „Ergebnis unklar“. Aktivitaets-Protokoll `SageWareneingang` („Sage-Wareneingang“), Counts `gesendet`, `uebergeben`, `fehler`, `unklar`; kein
  Lauf-Eintrag ohne Arbeit. Antwort kommt als SData-Atom-XML (Schreibtest 2026-11-12).
- **v1.99.9 `Sync:SageSystemKuerzel`** (String, Default leer, Kategorie „Sage-Lagerbuchung“, beide Standorte; Format `^([A-Z][A-Z0-9]{0,4})?$`,
  Serverprüfung `SettingDefinition.Muster`): System-Kürzel der Memo-Kennungen `WE#<K>#<Id>#` (Sage-Wareneingang) und `SM#<K>#<id>#`
  (Sage-Lagerbuchung). Leer = altes Format (Produktion). Gelesen vom `SageBookingWorker` je Takt **ohne** Safe-Fallback
  (`SageSystemKuerzel.Pruefen`, ungültig → Takt bricht ab) und von der Liste „Sage-Wareneingänge“ (Spalte Memo). Verwendete Kennung gespeichert in
  `PurchaseOrderReceipts.SageWeKennung` / `SageBookingQueueItems.SageKennung` (SQL/144); Regel `SageWareneingang.Kennung` /
  `SageBookingCorrelation.Kennung`. DEV-Absicherung setzt `DEV`. [[2026-11-12-v1-99-9-sage-system-kuerzel]]

- **v1.99.31 Anstoß „Offene Bestellungen“ nach Übergabe** ([[2026-11-16-offene-bestellungen-sofort-neu-lesen-einkauf-kw-filter-spec]]):
  `RunOnceAsync` gibt die Zahl übergebener Belege zurück (0 ohne Arbeit); der `SageBookingWorker` summiert sie und ruft nach dem
  Sage-WE-Schritt `AnstossOffeneBestellungenAsync` → `EinkaufBestellungenSyncService.TryRunExclusiveAsync(dryRun, anlass)`. Entscheidung
  `SageBookingWorker.AnstossEntscheidung(offen, minuten, gateAn, letzter, jetzt)`: 0 Belege nichts; `minuten <= 0` oder Gate aus verwirft; sonst
  sofort bzw. nach Ablauf des Fensters. Key **`Sync:EinkaufBestellungenNachWeMinuten`** (Int, Default 2, 0–60, Kategorie „Sage-Abgleich
  (SData)“, Standort AKE). Gates `Sync:EinkaufBestellungenEnabled` + `WorkerSettings:SyncDryRun`. **Sperre:** `static SemaphoreSlim` in
  `EinkaufBestellungenSyncService` – `RunAsync` wartet, `TryRunExclusiveAsync` liefert false (Anstoß bleibt offen). Info-Zeile „Anlass: …“ im
  Lauf „Offene Bestellungen (Sage)“; Fehler → Warnungslauf unter `SageWareneingang`, Reset, der reguläre Lauf holt nach.

## Lieferverzug-Mails (v1.99.10, [[2026-11-12-offene-bestellungen-mahnmail-lieferverzug-spec]])

- Lauf im `SyncWorker` direkt nach „Offene Bestellungen (Sage)“, Gate `Lieferverzug:MailAktiv` (Default aus), `RunResilientAsync("Lieferverzug-Mails")`.
  Aktivitaets-Protokoll `SyncLogServices.LieferverzugMail` („Lieferverzug-Mails“). Quelle nur der Cache `SagePurchaseOrderLines`, schreibt nie nach Sage.
- Dateien (`IDEALAKEWMSService/Services/`): `LieferverzugPlaner.cs` (reiner Helper: Zeitfenster Mo–Fr ab Stunde, Faelligkeit `heute >= Termin + N` und
  Rest > 0, Statuskette Kreditor-Mails aus → kein Kreditor → gesperrt → inaktiv → abgeschaltet → keine/ungueltige Adresse → Stand > 24 h → Wiederholung
  je (BelPosId, Liefertermin) → Testbetrieb/gesendet, Grenze je Lauf alle-oder-keine), `LieferverzugMailHtml.cs` (Kreditor-Mail DE+EN, Einkauf-Liste,
  CI je `Lieferverzug:Standort`, Text-Teil), `LieferverzugMailService.cs` (Ablauf, `LesenAsync` = Einstellungen DB-first, Protokollzeilen nur nach
  `true` vom Mailservice, sofort gespeichert). Counts `faellig`, `kreditoren`, `einkauf_gesendet`, `kreditor_gesendet`, `testbetrieb_umgeleitet`,
  `kreditor_geplant` (DryRun), `ohne_adresse`, `abgeschaltet`, `gesperrt`, `kein_kreditor`, `inaktiv`, `nicht_dran`, `ausgesetzt`, `nicht_gesendet`,
  `fehler`. Kein Lauf-Eintrag ohne Arbeit oder ausserhalb des Zeitfensters.
- „Einmal am Tag“ = irgendeine `PurchaseOrderReminders`-Zeile mit `SentAt` heute (nicht Arbeitsspeicher). Wiederholung zaehlt nur Zeilen des aktuellen
  Modus (`Testbetrieb` = Einstellung): Testbetrieb simuliert die Wiederholung, der Echtbetrieb beginnt danach frisch (Grenze je Lauf faengt die Welle).
- Rest-Regel: `ErfasstJeBelPos.Rest(mengeOffen, syncedAt)` / `RestAus` – dieselbe Funktion wie `PurchaseOrderReceiveRow.Rest`. Dafuer ist
  `IPurchaseOrderReceiptRepository` jetzt auch im Dienst registriert.
- `IMailService.SendMitOptionenAsync(MailNachricht)` (Absender, Reply-To, eingebettete Bilder per CID, liefert `false` bei stillem Ueberspringen); die
  alte `SendAsync` delegiert an denselben Kern. **`MailSettings:PickupDirectory`** (appsettings, nur DEV/QA): Mails als `.eml` in den Ordner statt SMTP,
  hat Vorrang vor `SmtpHost`. Logo `Branding/IDEAL_AKE_Logo_rgb_klein.jpg` als EmbeddedResource `IdealAkeLogo.jpg` im Dienst-Projekt.
- Adressregel EINMAL: `IdealAkeWms/Services/MailAdressen.cs` (`IstGueltig` = nackte Adresse, `Liste` = Komma/Semikolon/Zeilenumbruch) – genutzt von
  `SettingValidator` (neuer Kenner `SettingDefinition.Mail` = `SettingMail.Eine|Liste`), `CreditorsController` (Erinnerungsadresse) und dem Planer.

| Key | Typ | Default | Zweck |
|---|---|---|---|
| `Lieferverzug:MailAktiv` | Bool | `false` | Hauptschalter |
| `Lieferverzug:TageNachTermin` | Int 0–60 | `3` | Kalendertage nach Liefertermin |
| `Lieferverzug:Uhrzeit` | Int 0–23 | `7` | Versand ab Stunde, Mo–Fr |
| `Lieferverzug:EinkaufAktiv` | Bool | `true` | Sammelliste an den Einkauf |
| `Lieferverzug:EinkaufEmpfaenger` | String (Mail-Liste) | (leer) | Empfaenger der Sammelliste und der Testbetrieb-Mails |
| `Lieferverzug:KreditorAktiv` | Bool | `false` | Erinnerung an Kreditoren |
| `Lieferverzug:Testbetrieb` | Bool | `true` | Kreditor-Mails an den Einkauf umleiten |
| `Lieferverzug:KreditorWiederholTage` | Int 1–60 | `7` | Wiederholabstand je Position |
| `Lieferverzug:KreditorTageNachTermin` | Int 0–60 | `3` | ab v1.99.19: Kreditor-Schwelle; wirksam = Maximum mit `TageNachTermin` |
| `Lieferverzug:MaxAlterTage` | Int 0–3650 | `180` | ab v1.99.19: ältere Positionen (EBB+EBZ) nicht an Kreditoren, sondern „Zu klären“ in der Sammelliste; 0 = keine Grenze |
| `Lieferverzug:KreditorMitRahmenabrufen` | Bool | `false` | ab v1.99.19: EBZ-Positionen auch an Kreditoren (sonst nur Sammelliste) |
| `Lieferverzug:KreditorMaxJeLauf` | Int 1–500 | `30` | Grenze echter Kreditor-Mails je Lauf (Warnung + Fehlermail) |
| `Lieferverzug:KreditorAbsender` | String (eine Mail) | (leer = `MailSettings:FromAddress`) | Absender der Kreditor-Mail (Freigabe-Antwort 7) |
| `Lieferverzug:AntwortAdresse` | String (eine Mail) | (leer = erste Einkauf-Adresse) | Reply-To der Kreditor-Mail |
| `Lieferverzug:Standort` | Choice AKE/IDEAL | `AKE` | CI (Hausfarbe, Firmenadresse) |

## Wareneingang-CSV (v1.99.11, [[2026-11-12-wareneingang-barcode-lieferschein-csv-export-spec]])

- `WareneingangCsvService.RunOnceAsync(pfad, wiederholung)` (Dienst), eigener Schritt im `SageBookingWorker`-Takt **nach** dem Sage-WE-Schritt, in
  eigenem `try`. Gate: derselbe gelesene Wert `SageWareneingangBelegAktiv` **und** `WareneingangCsvAktiv` (Safe-Lesen, Fehler = aus) **und**
  `WareneingangCsvPfad` nicht leer. Legt je Sage-WE-Vorgang mit Barcode, dessen Receipts alle `Uebergeben` sind, eine Zeile in
  `WareneingangCsvExporte` an (`system:wareneingang-csv`), schreibt höchstens 10 Dateien je Takt (`.tmp` → `File.Move` auf `.csv`, nie
  überschreiben), wiederholt Schreibfehler nach Abstand bis zur Grenze; Kollision/ungültiger Barcode nie automatisch. **Seit v1.99.33** Name über `Dateiname(barcode, start, belegt)`: `<Barcode>.csv`, sonst
  `<Barcode>_n.csv` (Start = Zeilen mit gleichem Barcode und `Datei`, belegt = `Datei` anderer Zeilen oder Datei im Ordner); `KollisionText`
  entsteht nicht mehr, Altzeilen bleiben „nie automatisch“. Aktivitäts-Protokoll
  `SyncLogServices.WareneingangCsv` („Wareneingang-CSV“), Counts `geschrieben`, `fehler`, Warnung je Fehler, kein Eintrag ohne Arbeit.
- Gemeinsame Regeln Web/Dienst: `IdealAkeWms/Services/WareneingangCsv.cs` (Keys, Barcode-Regel, Pfad-Muster, Texte, `StatusText`, `Hinweise`).

| Key | Typ | Default | Wirkung |
|---|---|---|---|
| `WareneingangCsvAktiv` | Bool | `false` | CSV-Übergabe an (wirkt nur mit `SageWareneingangBelegAktiv`), AKE |
| `WareneingangCsvPfad` | String (Muster: leer oder UNC) | (leer = nichts) | Zielordner `\\server\freigabe[\ordner]` |
| `Sync:WareneingangCsvMaxWiederholungen` | Int 0–10 | `3` | automatische Wiederholungen nach Schreibfehler |
| `Sync:WareneingangCsvWiederholAbstandMinuten` | Int 5–1440 | `15` | Mindestabstand Fehler → Wiederholung |


## Nachtrag 2026-11-14 (Wochenend-Lauf): Einkauf und Artikel-Lieferanten seit v1.99.19

- **Letzte Bestellung (v1.99.21, SQL/148):** `IDEALAKEWMSService/Services/Sdata/ArtikelLieferantSyncService.cs`, Methode
  `LetzteBestellungAsync` nach dem Hauptschritt des Laufs „Artikel-Lieferanten“ (Gate unveraendert `Sync:ArtikelLieferantEnabled`, kein eigener
  Schalter). Liest SData `EinkaufPositionenAllInfo` (EBB und EBZ, `Belegdatum` ab heute minus 3 Jahre, `orderBy=BelPosId`, alle Seiten),
  juengster Beleg je Artikel + `A0Empfaenger` (Datum↓, BelId↓, BelPosId↓), speichert `MengeBasis` in `ArticleSuppliers.LetzteBestellmenge`/
  `LetzteBestellungAm`/`LetzteBestellungBeleg`. Kein Treffer = Werte geleert; Lesefehler oder 0 EBB = Warnung, Werte bleiben. Counts
  `letzte_bestellung_*`. Laufzeit am AKE-Test ca. 5–6 min laenger (Q-3 in [[2026-11-13-v1-99-21-einkauf-letzte-bestellmenge]]).
  **Seit v1.99.30 abgeloest:** eigener `IDEALAKEWMSService/Services/Sdata/LetzteBestellungSyncService.cs` (SyncLog-Service `LetzteBestellung`,
  Aufruf im `SyncWorker` nach dem Artikel-Lieferanten-Lauf, `RunWennFaelligAsync`), nur zu `Sync:LetzteBestellungUhrzeitNacht` (3) /
  `Sync:LetzteBestellungUhrzeitMittag` (12) je einmal pro Tag und Fenster, Gate weiter `Sync:ArtikelLieferantEnabled`. `MengeBasis <= 0` zaehlt
  nicht. Bestellungen bei Lieferanten ohne Artikel-Lieferant-Zeile → `ArticleLastOrdersUnassigned`. Counts `positionen`, `positionen_ebz`,
  `menge_null`, `gefunden`, `aktualisiert`, `geleert`, `lesefehler` ([[2026-11-16-v1-99-30-bestellvorschlaege-nachbesserung]]).
- **Bedarfsregel (v1.99.30, Web):** `IdealAkeWms/Services/PlannedConsumptionService.cs` (`IPlannedConsumptionService.GetJeArtikelAsync`) – EINE
  Stelle fuer Artikelinfo und Bestellvorschlaege: offene FAs (`!IsDone && !IsCancelled && !IsDonePicking`) mit Stuecklisten-Treffer
  (`IBomCacheRepository.GetComponentMengePerDeviceAsync`, flach Menge × Quantity, hierarchisch Sollmenge), Rest = Soll − Ausbuchungen zum FA
  (`IStockMovementRepository.GetAusbuchungenJeFaAsync`), je FA max(Rest, Bedarfsmeldungen), Artikel max(FA-Teil, Lagerbestellungen);
  `EinkaufOffeneBedarfe` steuert Lagerbestellungen/Bedarfsmeldungen. `ArtikelBedarf.AngerechnetBis`/`NaechsterBedarf` fuer die Dringlichkeit.
- **Bestellvorschlaege (v1.99.20, Web):** `IdealAkeWms/Services/PurchaseProposalCalculator.cs` – eine Rechenstelle: Fehlmenge F aus WMS-Bestand,
  Sage verfuegbar, offenen Bedarfen (`EinkaufOffeneBedarfe`), offen bestellt (Cache `SagePurchaseOrderLines`, Rest-Regel `ErfasstJeBelPos.Rest`)
  und Meldebestand; Menge V = (⌊F/L⌋+1)×L mit L = letzte Bestellmenge, ohne Bestellung in 3 Jahren Fallback `ArticleSageDetail.Bestellmenge`
  (`HinweisFallback`), Aufrunden auf `Mindestbestellmenge` nur bei vergleichbarer Einheit und `UmrechnungsfaktorEK = 1`. Kein Schreibweg,
  kein Dienst-Lauf. E5 (Dispo-Stapel, schreibt an Sage) blockiert.
- **Lieferverzug Lief.-Art.-Nr. (v1.99.19):** `LieferverzugMailService`/`LieferverzugMailHtml` lesen `ArticleSupplier.SageBestellnummer` je
  Artikel + Kreditor der Bestellung; ohne Lauf „Artikel-Lieferanten“ bleibt die Spalte leer. Abschnitt „Zu klären“ = `ZuKlaeren` des Plans
  (Altersgrenze `Lieferverzug:MaxAlterTage`), Count `zu_klaeren`.
- **Offene Bestellungen (Sage):** `IDEALAKEWMSService/Services/Sdata/EinkaufBestellungenSyncService.cs` (EBB + seit v1.99.15 EBZ); Belegnummer
  seit v1.99.22 bis 45 Zeichen (SQL/149).

## BDE-Teileliste (v1.99.32)

- Web: `IdealAkeWms/Services/BdeTeilelisteService.cs` (Lesen, Melden mit applock, Rücknahme, Filter je Werker als `UserViewPreference`
  `BdeTL:W{wp}:O{op}`); `BeschichtungResolver.BeschichtetPosition` für die Spalte „Beschichtet“.
- Dienst: `WorkOperationStructureDetectionService` überspringt Blatt-Kürzel von Teileliste-Werkbänken, Count `teileliste_blatt_uebersprungen`.
  Kein neuer ServiceSetting-Key.

## OperationConfigSync: Sage-Arbeitsgangnummer (v1.99.27)

- Schreibt `SageArbeitsgangnummer` nur aus der View-Quelle (Tabelle/SData: keine Nummer); Handwert nie. Neue Counts `nummer_gesetzt`,
  `nummer_mehrdeutig`, `abweichung_hand`, `ohne_kuerzel`. Beschreibungen `Sync:ProductionWorkplaceSyncViewName` (liefert Arbeitsgänge) und
  `Sync:OperationConfigSyncEnabled` korrigiert.

## Blechfertigung (v1.99.35, [[2026-11-13-blechfertigung-bf-auftraege-wicam-csv-spec]])

- **`BfCsvService`** (Dienst, Schritt im `SageBookingWorker`, eigener try, nicht an `SageWeAn` gebunden): schreibt je `Freigegeben` + CSV offen
  bzw. fälliger Wiederholung `<Nummer>.tmp` → `<Nummer>.csv` (`overwrite:false`), höchstens 10 je Takt. Lauf `SyncLogServices.BfAuftragCsv`
  „Blechfertigung: WiCAM-CSV“, Counts `geschrieben`, `fehler` (Kollision zählt als Fehler).
- **`BfAuftragEmailService`** (Dienst, `SyncWorker`): Mail je abgeschicktem Auftrag an `BfEmpfaengergruppeId`; Lauf `BfAuftragEmail` „Blechfertigung: Mail“.
- **Keys** (Kategorie „Blechfertigung WiCAM“, `ServiceSettingDefinitions.All`): `BfCsvAktiv`, `BfCsvPfad` (UNC), `Sync:BfCsvMaxWiederholungen` (3),
  `Sync:BfCsvWiederholAbstandMinuten` (15), `BfCsvCreateNewOrder`, `BfCsvOrderPriority`, `BfCsvOrderRelease`, `BfCsvRemark1..3`,
  `Sync:BfAuftragEmailEnabled`; dazu `Sync:BlechteileFelderNachlesen` (Artikel-Abgleich, V-B). AppSettings: `BlechfertigungAktiv`,
  `BfEmpfaengergruppeId`, `BfStatuswerte`, `BfSchachtelterminArbeitstage`, `BfFeTerminArbeitstage`.

## FA-Hierarchie-Abgleich (v1.99.37, [[2026-11-17-fa-struktur-abgleich-statt-full-replace-spec]], ADR [[0032-fa-struktur-abgleich-mit-lebenslauf-statt-full-replace]])

- **`FaHierarchySyncService`** gleicht ab statt zu ersetzen; Planer `FaStrukturAbgleich` (rein). Sage-Zustand verschwundener FAs über
  `ISageFaStrukturReader.FaStatusLesenAsync` (immer SQL `KHKPpsFaBelege`, auch bei Quelle SData; ohne Mandant-Filter bis P-AB4).
  Positionen über `PositionenAbgleichenAsync` (statt `PositionenErsetzenAsync`), danach `AufraeumenAsync`. `IConfiguration` vor `ISyncLogger` im Ctor.
- **Counts:** `neu`, `geaendert`, `wieder_da`, `verschwunden_beendet`/`_inaktiv`/`_unklar`, `entfernt_position`, `entfernt_in_sage`,
  `schluessel_doppelt`, `verschwunden_gesperrt`, `sage_abfrage_fehler`, `positionen_neu`, `positionen_entfernt`, `aufgeraeumt`.
- **Fehlermails** („FaHierarchy-Sync: …“): leerer View-Read, doppelte Schlüssel, Read ohne bisherige HauptFA, Sage-Abfrage fehlgeschlagen,
  ungeklärt verschwundene Zeilen, FA-Positionen nicht abgeglichen.
- **Key:** `Sync:FaStrukturAufbewahrungTage` (Kategorie FA-Hierarchie, Int, Default 90, 1–3650).
- **Konsumenten:** `FaMaterializationSyncService` und `WorkOperationStructureDetectionService` lesen nur `VerschwundenSeit == null`;
  `BdeTeilelisteService` setzt `TeilelisteZeile.Verschwunden` (nicht meldbar, `MeldeZeileAsync` weist ab). Schild `Views/Shared/_FaStrukturBadge.cshtml`.

## Sage-FA-Arbeitsgänge anlegen (v1.99.38, IDEAL, [[2026-11-17-arbeitsgaenge-aus-wms-in-sage-fa-anlegen-spec]])

- **`SageFaArbeitsgaenge`** (Web, `IdealAkeWms/Services/SageFaArbeitsgaenge.cs`): Vorschau je FA × Kürzel (`BuildVorschau`), Statusregeln,
  Queue-Status (`QueueStatus`, `IstEinreihbar`), Keys und Texte (`UnklarText`, `AusText`, `BearbeiterFehlt`).
- **`SageFaArbeitsgangService`** (Dienst, Schritt `ProcessSageFaArbeitsgaengeAsync` im `SageBookingWorker` nach BF-CSV, eigener try):
  hängende „Gesendet“ → Fehler unklar; je FA (höchstens 50 je Takt) Sage lesen über `ISageFaStandLookup`/`SdataFaStandLookup`, dann je AG
  POST über `SageFaArbeitsgangClient` (`$service/FertigungsauftragService`, ServiceOption 4). Lauf `SyncLogServices.SageFaArbeitsgaenge`
  „Sage-FA-Arbeitsgänge“, Counts „Angelegt“/„Übersprungen“/„Fehler“, Fehlermail über `ISyncErrorNotifier`.
- **Keys** (Kategorie „Sage-FA-Arbeitsgänge (IDEAL)“): `SageFaArbeitsgaengeAnlegenAktiv` (Bool, aus), `Sync:SageFaArbeitsgaengeBearbeiter`
  (leer = nichts senden), `Sync:SageFaArbeitsgaengeMaxWiederholungen` (3, 0–10), `Sync:SageFaArbeitsgaengeWiederholAbstandMinuten` (15, 5–1440);
  Hänge-Grenze `Sync:SageLagerbuchungStuckMinutes` (geteilt).

## Sage-Benutzerfelder (v1.99.40, [[2026-11-18-sage-userfelder-als-einstellung-spec]], ADR [[0033-sage-benutzerfelder-als-einstellung]])

- **Keys und Katalog:** `Sync:Userfeld:<Abgleich>:<Standardname>` (27 Keys), Katalog `IdealAkeWms/Services/SageSdata/SageUserfelder.cs`, Kategorien „Sage-Benutzerfelder: …“.
- **Laden:**
  - Dienst: `ServiceSettings.UserfelderSafeAsync` (eine Abfrage `LIKE 'Sync:Userfeld:%'`; bei Fehler gelten die Defaults).
  - Web: `SageUserfelder.AusAsync`.
- **Verwender:**
  - `KontokorrentSyncService` (Kreditoren/Debitoren)
  - `SdataArtikelReader` (Varianten + Blechteile-Nachlese)
  - `EinkaufBestellungenSyncService` (Kopf, Präfix `KHKEKBelege_`)
  - `SageArbeitsplatzReader` (Tabellen-SQL)
  - `SdataFaStrukturReader` (Präfix `KHKPpsFaBelege_` + Artikel)
- **404-Behandlung:** `SyncWorker.RunResilientAsync` erkennt HTTP 404 (`SdataLeseException.Status`) irgendwo in der Ausnahme-Kette, meldet es und überspringt den Schritt 24 h (`ResourceFehltPause`).

## Entnahme-Platz und Bestandsregel (v1.99.53, [[2026-11-23-lagerbestellung-verbrauchsbuchung-bei-lieferung-spec]])

- `Services/QuellLagerplatzVorschlag.Waehle` – unverändert aus `PickingController.QuellVorschlag` verschoben: Hauptlagerplatz mit Bestand > 0,
  sonst Platz mit dem meisten Bestand, ohne Bestand `NAN`. Genutzt von Kommissionierung und Lagerbestellung. **Nicht** für den Wareneingang
  (dort immer Hauptlagerplatz, v1.99.52).
- `Services/AusbuchungBestandsregel.PruefeAsync` – eine Regel für Warenausgang und Lagerbestellung: `NegativeBuchungErlaubt` → Fehlbestand auf
  `NegativeBuchungLagerplatz`, sonst Zeile abweisen.
- Kein Dienst-Code geändert; die Sage-Entnahme läuft über den bestehenden Decorator → Queue → `SageBookingWorker`.

## Sage-Buchungen Gesamtsicht und Sage-Referenz (v1.99.66–v1.99.69, Lauf [[2026-11-25-goal-lauf-beschichtung-sage-buchungen]])

- `Services/SageBuchungenGesamtsicht.cs` (Web, **statisch**, DB-frei, v1.99.66, [[2026-11-23-sage-buchungen-gesamtsicht-spec]]): Enums
  `SageBuchungAnzeige` (Wartet, Gesendet, Uebergeben, Fehler, Klaeren) und `SageBuchungHerkunft` (Einbuchung, Ausbuchung, Umbuchung,
  Lagerbestellung, Wareneingang); `Anzeige(SageBookingQueueStatus)`/`Anzeige(SageWareneingangStatus)` (NichtNoetig/NurInSage = null, nicht anzeigen),
  `Herkunft(StockMovement)` (Lagerbestellung vor Bewegungsart), `ErfasstVon` (Nacherfassung mit „(nacherfasst)“), `ImZeitraum` (bis = ganzer Tag),
  `Build`, `MarkiereErsteJeVorgang` (WE-Knöpfe nur an der ersten Zeile eines Vorgangs), `Spalten` (Zelltexte für View **und** Server-Spaltenfilter).
  `StandardTage = 30`.
- Repository-Signaturen seit v1.99.66: `ISageBookingQueueRepository.GetForMonitoringAsync(DateTime von, DateTime? bis)` (Zeitraum + alle offenen,
  statt Status-Parameter) und `IPurchaseOrderReceiptRepository.GetSageWareneingaengeAsync(string? bestellung, DateTime von, DateTime? bis)`
  (ohne NichtNoetig/NurInSage, Übergebene nur im Zeitraum, Status filtert die Seite). `PurchaseOrdersController.SageWeListeAsync` entfällt.
- `IDEALAKEWMSService/Services/SageReferenz.cs` (v1.99.68, statisch, [[2026-11-24-sage-buchung-referenz-benutzer-abteilung-spec]]):
  `Kurz(kennung, abteilung, benutzer)` = `<Kennung> <Abteilung> <Benutzer>`, `ReferenzMaxLaenge = 31` (`KHKLagerplatzbuchungen.Referenz`
  varchar(31)), am Ende gekürzt (erst Benutzer, dann Abteilung, Kennung nie); `Voll(kennung, benutzer, abteilung)` = `WMS: <Kennung> <Benutzer>[ Abteilung:<x>]`.
- Lagerbuchungs-Payload (`SageLagerbuchungPayloadBuilder`): je Zeile `Referenz` = `Kurz`, `Memo` = `Voll`; das **Kopf-Memo bleibt unverändert**
  `SageBookingCorrelation.Memo(kennung)` (Korrelation/Lookup). Benutzer und Abteilung kommen aus der Bewegung (`CreatedBy`, `Abteilung`), nie aus dem
  Queue-Eintrag – Requeue/Recovery/Nacherfassung senden denselben Text.
- Sage-WE-Kopf-Memo (`SageWareneingangPayload`): `WMS: WE#…# <Benutzer>` über `SageReferenz.Voll`, Benutzer aus `PurchaseOrderReceipt.CreatedBy`;
  `WE#…#` bleibt enthalten, Memo-Suche und Wiedererkennung gültig.
- Offen (P-2, nur Mensch am Sage-Testsystem): ob Sage die gesendete Zeilen-`Referenz` übernimmt (heute füllt Sage sie aus dem Kopf-Memo) und das Zeilen-Memo zeigt.

## Beschichtungsauftrag-Workflow (v1.99.65/v1.99.67, [[2026-11-25-beschichtungsauftrag-vorschlag-ohne-auswahl-spec]])

- `BeschichtungsauftragWorkflow.Entfernbar(status, verpackt)`: eine Regel für Server und Detailseite – Entfernen im Entwurf immer, in Übergeben nur
  mit gespeichertem Haken „offen“. `AnlegenAsync` mit `auswahl` null/leer = alle freien Positionen, offen.
- `PositionBezeichnung` (internal) seit v1.99.67 mit VaterFA: `Artnr (Pos. n, VaterFA v)`, ohne FA `(ohne FA)`. `BearbeitenAsync(…, ziel)` lehnt
  Übergeben mit 0 verbleibenden Positionen vor dem Speichern ab.


## Kommissionier-Nachtrag (v1.99.70, [[2026-11-25-kommissionierauftraege-nachtrag-nachts-spec]])

- `IDEALAKEWMSService/Services/KommissionierNachtragService.cs`, SyncLog-Service `KommissionierNachtrag` („Kommissionier-Nachtrag“), Aufruf im
  `SyncWorker` vor dem Lagerbestand-Schritt über `RunWennFaelligAsync(dryRun, jetzt)`: einmal je Tag ab `Sync:KommissionierNachtragUhrzeit`
  (Int 0–23, Default 2), Gate `Sync:KommissionierNachtragEnabled` (Bool, Default an), Termin über den Startmerker „Run gestartet“
  (`LetzteBestellungPlaner.Faellig`), verpasster Termin wird nachgeholt, DryRun läuft nie.
- Je freigegebenem, offenem HauptFA ein eigener DI-Scope mit `PickingOrderWorkflow.ErzeugeOffeneAsync` (dieselbe Logik wie Freigabe und Leitstand-Knopf);
  Fehler eines FA = Warnzeile + Zähler `fehler`, der Lauf geht weiter. Counts `fa_geprueft`, `fa_mit_nachtrag`, `auftraege_angelegt`, `zeilen_angelegt`,
  `ohne_rest`, `altbestand_pruefen`, `ohne_stueckliste`, `fehler`, `stueckliste_kleiner`. Meldungen auf 1000 Zeichen gekürzt.
- `ICurrentUserService` im Dienst = `IDEALAKEWMSService/Common/DienstBenutzer.cs` (Name `KommissionierNachtrag`, Rollenabfragen werfen
  `NotSupportedException`); Registrierung über die statische `KommissionierNachtragService.Registrieren`.
- `AddKommissionierauftraege` (`IdealAkeWms/Services/PickingOrderRegistrierung.cs`): EINE Registrierung des Kommissionierauftrags-Graphen
  (Workflow, Repositories, BOM) für Web-`Program.cs`, Dienst und `BomDiResolutionTests` ([[fallstricke]] §65).

## Lager-/Lagerplatz-Anlage in Sage (v1.99.71, [[2026-11-24-lagerplatz-anlage-wms-nach-sage-spec]], ADR [[0036-lagerplatz-anlage-in-sage-per-direkt-sql-hinter-austauschbarer-schnittstelle]])

- Schritt im `SageBookingWorker` (`ProcessSageLagerplatzAnlageAsync`, eigenes try nach dem FA-AG-Schritt): nur bei `SageLagerplatzAnlageAktiv`
  (Bool, Default false, **nicht** Safe gelesen); Stufe `SageLagerAnlageLagertechniken` (Choice `Aus`/`Mitschnitt`/`Alle`, Default `Aus`);
  Hängend-Grenze `Sync:SageLagerbuchungStuckMinutes`. Mail nur beim Wechsel des technischen Fehlertexts (`MeldenBeimWechsel`, Merker
  `_lpTechnischGemeldet`).
- `IDEALAKEWMSService/Services/SageLagerplatzAnlageService.cs` (`RunOnceAsync` → technischer Fehlertext oder null), SyncLog-Service
  `SageLagerplatzAnlage` („Lagerplätze → Sage“), ServiceActor `system:sage-lagerplatz-anlage`. Erst Lager (Outbox `StorageArea`), dann Plätze
  (`StorageLocation`); Plätze eines nicht bestätigten Lagers warten und sind keine Arbeit; Lauf erst beim ersten Ergebnis geöffnet, je Lauf einmal
  Info `Sage-Datenbank: <Server>/<DB>`. Counts `angelegt`, `vorhanden`, `fehler`, `uebersprungen`, `lager_angelegt`, `lager_vorhanden`, `lager_fehler`.
- Schreibstelle `ISageLagerplatzAnlage` / `SqlSageLagerplatzAnlage` (SQL wörtlich aus der Spec, `SageConnection`, Mandant fest 1,
  `IstTechnisch` = Fehlerklassen S3). Austauschstelle gegen einen künftigen SData-Service.
- Web: `StorageLocationsController.CreateInSage` (GET/POST, `RequireMasterDataAccess`), `ErneutSenden`, `LagerErneutSenden`; Helfer
  `IdealAkeWms/Services/SageLagerplatzAnlage.cs` (Keys, Stufen, Techniken, Code, Grenzen 200/9999); Repository `AddSageAnlageAsync`
  (eine Transaktion bei relationaler DB), `ErneutSendenAsync`, `GetOffeneLagerUebergabenAsync`.
- `LagerplatzSyncService.SyncStorageAreasAsync`: Lager mit Lager-Outbox ≠ Bestätigt werden nicht als fehlend markiert ([[fallstricke]] §66).
