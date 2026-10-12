---
type: spec
title: "Arbeitsgang-Stammdaten vereinheitlichen (OSEON + IDEAL) — Reiter „Arbeitsgänge“ in den BDE-Stammdaten"
slug: 2026-10-09-arbeitsgang-stammdaten-vereinheitlichen-spec
status: Testbereit
created: 2026-10-12
updated: 2026-10-12
source_backlog: "[[2026-10-09-arbeitsgang-stammdaten-vereinheitlichen]]"
task: "[[2026-10-12-arbeitsgang-stammdaten-vereinheitlichen-umsetzung]]"
worktree: ""
branch: ""
affected_code:
  - "IdealAkeWms/Models/OseonOperationConfig.cs (Z.5-33) -> Models/OperationConfig.cs: Klasse OperationConfig, Property IsOseonRelevant -> IsExternalSystemRelevant, erbt AuditableEntity (eigenes Id entfaellt, kommt aus der Basis), Doku neutral ('Offset in Arbeitstagen zum Bezugstermin') (Antwort 1 = U, Antwort 2 = ja)"
  - "IdealAkeWms/Data/ApplicationDbContext.cs — DbSet Z.35 (OseonOperationConfigs -> OperationConfigs), Mapping Z.881-889: ToTable('OperationConfigs'), KEIN HasColumnName (Spalte heisst wie die Property), Unique-Index bleibt HasIndex(OperationName).IsUnique() (EF-Name IX_OperationConfigs_OperationName), NEU vier Audit-Mappingzeilen HasMaxLength(200) wie ApplicationDbContext.cs:88-91"
  - "IdealAkeWms/Migrations/<Timestamp>_RenameOperationConfigsAndAddAudit.cs (+ .Designer.cs, ApplicationDbContextModelSnapshot.cs) — NEU: per dotnet ef migrations add erzeugt, Up()/Down() danach VOLLSTAENDIG durch guarded migrationBuilder.Sql ersetzt (siehe Migrations-/SQL-Auswirkungen)"
  - "SQL/95_RenameOperationConfigsAndAddAudit.sql — NEU (Nummer in Buendel und main frei, gemessen 2026-10-12)"
  - "SQL/00_FreshInstall.sql — Tabellenblock Z.1220-1236 (neue Namen + Audit-Spalten + IX statt UQ), Seed Z.1491-1510 (neue Namen + Audit-Werte), History-Block nach Z.2422 (neue MigrationId)"
  - "IdealAkeWms/Data/Repositories/OseonOperationConfigRepository.cs + CachedOseonOperationConfigRepository.cs + IOseonOperationConfigRepository — Umbenennung (IOperationConfigRepository/OperationConfigRepository/CachedOperationConfigRepository); NEU GetDueDateOffsetsAsync() (Offset-Lookup fuer FA-Struktur Teil 2/3, case-insensitiv, getrimmt); GetUnconfiguredOperationNamesAsync (Z.55-67) ersetzt durch GetUnconfiguredOperationsAsync() mit Quelle (OSEON + WMS-Arbeitsgaenge, ohne '01'); OSEON-Pfad GetAllAsDictionaryAsync (Z.46-50, case-sensitiv) UNVERAENDERT"
  - "IdealAkeWms/Program.cs — DI Z.125-126 (Umbenennung), Seed Z.470-499 (Umbenennung + CreatedAt/CreatedBy/CreatedByWindows = 'system'; Seed-Bedingung unveraendert, Antwort 3)"
  - "IdealAkeWms/Controllers/OperationConfigsController.cs — NEU, Klasse [RequireAdminAccess], KEIN [RequireBdeActive] (Muster BdeShiftCalendarController.cs:11); Actions Index/Add/Update/Delete aus SettingsController.cs:108-178 uebernommen"
  - "IdealAkeWms/Controllers/SettingsController.cs — Z.17/24/30 Repository-Abhaengigkeit entfernen, Z.120-178 POST-Actions entfernen, Z.110-118 GET OperationConfig -> 302-Redirect (Antwort 5)"
  - "IdealAkeWms/Views/OperationConfigs/Index.cshtml — NEU (aus Views/Settings/OperationConfig.cshtml, das entfaellt): Listen-View-Pattern, neue Begriffe, Legende je Fremdsystem, Karte 'Nicht konfigurierte Kuerzel'"
  - "IdealAkeWms/Views/Shared/_BdeMasterDataTabs.cshtml — NEU: Reiterleiste fuer BdeMasterData/Index und OperationConfigs/Index; BDE-Reiter nur bei BdeAktiv + CanManageBdeShiftlead, Reiter 'Arbeitsgaenge' nur fuer Admin"
  - "IdealAkeWms/Views/BdeMasterData/Index.cshtml — Z.13-26 Reiterleiste durch Partial ersetzen"
  - "IdealAkeWms/Views/Shared/_Layout.cshtml — Admin-Block des Stammdaten-Dropdowns Z.308-319 (Stand v1.52.0): Eintrag 'Arbeitsgaenge' (Antwort 4)"
  - "IdealAkeWms/Views/Settings/Index.cshtml Z.10-16 (Knopf), Views/OseonReporting/OperationsOverview.cshtml Z.27 (Link) + Z.154 (Text), Views/Tracking/OseonIndex.cshtml Z.55 (Label), Views/Help/Index.cshtml Z.745, Z.762-763, Z.1431-1433 — Ziel-Link und Begriff 'Fremdsystem-relevant'"
  - "Umbenennungs-Folgen (nur Name, Verhalten gleich): Controllers/TrackingController.cs Z.20/32/312-322/381-389, Controllers/OseonReportingController.cs Z.15/23/146-151, Services/Oseon/OseonGroupViewModelBuilder.cs Z.11/17/34/43-51, Data/Repositories/OseonProductionOrderRepository.cs Z.213-215/256, Data/Repositories/OseonReportingQueryResult.cs Z.8, Data/Repositories/IOseonProductionOrderRepository.cs Z.30 (Kommentar), Models/ViewModels/OseonTrackingViewModel.cs Z.73-77 (OseonOperationConfigViewModel entfaellt -> NEU Models/ViewModels/OperationConfigIndexViewModel.cs)"
  - "UNVERAENDERT (Regressionsanker): Services/OseonDueDateCalculator.cs, IDEALAKEWMSService/Services/WorkOperationStructureDetectionService.cs, Models/ViewModels/OseonTrackingViewModel.cs Z.70 (ViewModel-Property IsOseonRelevant), Views/Tracking/_OseonGroupDetails.cshtml Z.69-82, wwwroot/css/site.css Z.698"
  - "IdealAkeWms.Tests — OseonOperationConfigRepositoryTests.cs -> OperationConfigRepositoryTests.cs, OseonProductionOrderRepositoryReportingTests.cs, OseonReportingControllerTests.cs, TrackingControllerTests.cs (Umbenennung); NEU Tests fuer Offset-Lookup, Nicht-konfiguriert-Liste, Controller-Zugriff/Audit; NEU Drift-Waechter Repositories/OperationConfigsMigrationSqlTests.cs (Muster BeschichtungsauftragMigrationSqlTests.cs:31)"
  - "docs/TESTSZENARIEN.md (neues Kapitel TS-86, Stand v1.52.0, + TS-1.7, TS-7.9, TS-7.10, TS-16.5, 36.5 nachziehen) + secondbrain/tests/testszenarien-index.md"
  - "8 Eindeutigkeit -> konvergieren auf IX_OperationConfigs_OperationName (Gerald 2026-10-12, Freigabe-Block Nr. 8)"
open_questions: []
beantwortete_rueckfragen:
  - "1 Datenmodell -> U: Tabelle OseonOperationConfigs -> OperationConfigs, Spalte IsOseonRelevant -> IsExternalSystemRelevant, PK/Index umbenannt; keine externen Leser, keine Kompatibilitaets-View (Gerald 2026-10-12, Nachtrag)"
  - "2 Audit-Felder -> ja, in derselben Migration; Bestand CreatedBy/CreatedByWindows = 'system', CreatedAt = Migrationszeitpunkt (Gerald 2026-10-12)"
  - "3 Seed -> Code unveraendert (nur neue Namen + Audit), IDEAL-Bereinigung per UI nach P3 (Gerald 2026-10-12)"
  - "4 Navigation -> Eintrag 'Arbeitsgaenge' im Admin-Block des Stammdaten-Dropdowns, unabhaengig von BdeAktiv (Gerald 2026-10-12)"
  - "5 /Settings/OperationConfig -> GET 302 auf die neue Seite, POST-Actions entfallen (Gerald 2026-10-12)"
  - "6 Fremdsystem-relevant -> wirkt nur fuer OSEON, Legende sagt das (Gerald 2026-10-12)"
  - "7 '01' -> nicht in 'nicht konfiguriert' (Gerald 2026-10-12)"
epic: false
etappen: []
deploy:
  web: true
  service: false      # Service liest die Tabelle nicht (0 Treffer in IDEALAKEWMSService/); geht mit, wenn das Buendel komplett ausgerollt wird
  migration: true     # Antwort 1 = U + Antwort 2 = ja: Rename + Audit in EINER Migration, SQL/95
freigabe_entscheidung: "Freigegeben wie Antworten 1-8: Variante U (Tabelle/Spalte/Index umbenennen, guarded sp_rename, Konvergenz auf IX_), Audit-Felder in derselben Migration SQL/95, Reiter Arbeitsgaenge in BDE-Stammdaten nur Admin und unabhaengig von BdeAktiv, OperationConfig-Weiterleitung, Fremdsystem-relevant nur OSEON, keine externen Leser. Umsetzung direkt im Buendel."
freigabe_von: "Gerald Weichbold"
freigabe_am: 2026-10-12
# Flache Schluessel mit Absicht: Obsidians Property-Editor kann verschachtelte
# YAML-Objekte NICHT bearbeiten - und genau diesen Block fuellt der Mensch aus.
---

> **Umsetzungsort (Vorgabe Koordination 2026-10-12):** direkt im Bündel-Worktree
> `.claude/worktrees/2026-08-07-ideal-teile-1-5` auf Zweig `feature/2026-08-07-ideal-teile-1-5` (kein eigener
> Worktree, ein Lauf zur Zeit). `worktree`/`branch` im Frontmatter bleiben leer — der Ort steht hier fest.
> Version, TS-Kapitel und SQL-Nummer: **nächste freie im Bündel zum Umsetzungszeitpunkt**. Gemessener Stand
> 2026-10-12: Bündel **v1.50.0** (`IdealAkeWms/AppVersion.cs:5`), höchstes Kapitel **TS-83**
> (`docs/TESTSZENARIEN.md:9403`), höchstes Skript **`SQL/94_AddBeschichtungsauftrag.sql`**, letzte Migration
> `20261012073551_AddBeschichtungsauftrag`. `main` steht bei v1.30.1 mit `SQL/88_*` als höchstem Skript; die
> Nummern 89–94 sind im Bündel belegt → **95 ist derzeit in beiden Zweigen frei** (vor Vergabe in beiden Zweigen
> erneut prüfen). Code-Belege dieser Spec: Bündel v1.50.0 (Spec-Auftrag nannte v1.49.0; der Stand ist inzwischen
> weitergezogen, die zitierten Stellen sind gegen v1.50.0 gelesen).
>
> **Nachmessung Rework 2026-10-12 (Antworten 1–7):** Die Koordination nannte HEAD `5764c664` / v1.51.0; der Bündel-
> Worktree steht laut Koordination inzwischen auf `d6d6beb0` / **v1.52.0** (`IdealAkeWms/AppVersion.cs:5` = `"1.52.0"`,
> FA-Struktur Teil 1). Neu gemessen: höchstes Kapitel **TS-85** (`docs/TESTSZENARIEN.md:9522`) → nächstes **TS-86**;
> höchstes Skript weiterhin `SQL/94_AddBeschichtungsauftrag.sql`, `main` weiterhin `SQL/88_*` → **SQL/95 in beiden
> Zweigen frei**; letzte Migration weiterhin `20261012073551_AddBeschichtungsauftrag` (Teil 1 brachte keine). Gegen
> v1.52.0 nachgeprüft und in dieser Fassung aktualisiert: `Program.cs` (DI `:125-126`, `Migrate()` `:186`, Seed
> `:470-499`), `_Layout.cshtml` (BDE-Menü `:249`, Admin-Block Stammdaten `:308-319`), alle Treffer für
> `OseonOperationConfigs`/`IsOseonRelevant`, `SQL/00_FreshInstall.sql`, EF-Snapshot. Übrige Zeilenangaben stammen aus
> v1.50.0 und können um wenige Zeilen verschoben sein — der Dev-Lauf sucht per Bezeichner, nicht per Zeilennummer.
> Wird vor dieser Spec eine andere Bündel-Spec mit Migration/Skript umgesetzt (z. B. FA-Struktur Teil 2/3 — sie hängen
> aber von dieser ab), Nummern erneut messen.
>
> **Diese Spec ist Vorbedingung** für [[2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-2-spec]] und
> [[2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-3-spec]] (beide lesen nur den Offset-Lookup aus FA 6). Deren
> `depends_on` zeigt derzeit noch auf die Backlog-Notiz — Umstellen ist Sache jener Specs, nicht dieser.

## Ziel / Nutzen (das Warum)

Die Termin-Offsets je Arbeitsgang-Kürzel werden heute nur für OSEON gepflegt (`/Settings/OperationConfig`,
„Arbeitsgang-Konfiguration (OSEON)“). Die FA-Struktur (Teil 2) und das BDE-Terminal (Teil 3) brauchen denselben
Offset für IDEAL-Kürzel (Bezug `FE_Termin`) und für AKE-BDE (Bezug `ProductionDate`). Statt einer zweiten Pflege
wird die vorhandene Tabelle zur **gemeinsamen Arbeitsgang-Stammtabelle**: ein Ort (BDE-Stammdaten › Reiter
„Arbeitsgänge“), neutrale Begriffe, eine Liste „nicht konfiguriert“ für beide Quellen und eine schmale
Lese-Schnittstelle für Teil 2/3. Das OSEON-Verhalten (Teileverfolgung, Reporting, Fälligkeit) bleibt fachlich
identisch.

Fachlicher Rahmen = Entscheidungen Gerald 2026-10-09 in [[2026-10-09-arbeitsgang-stammdaten-vereinheitlichen]]
(1 Reiter unabhängig von `BdeAktiv`, 2 Ort BDE-Stammdaten / Recht Admin, 3 ohne Eintrag Offset 0 + Liste
„nicht konfiguriert“, Werkbank-Zuordnung bleibt, 4 „Offset (Arbeitstage) zum Bezugstermin“ + Legende je
Fremdsystem, 5 „Fremdsystem-relevant“). Diese Spec verhandelt sie nicht neu.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Generalisierung der Tabelle `OseonOperationConfigs` in Code **und** Schema (Antwort 1 = U): Tabelle, Spalte, PK und
  Unique-Index umbenannt, eine Migration + `SQL/95` + `00_FreshInstall.sql`; keine Kompatibilitäts-View (Nachtrag
  Antwort 1: keine externen Leser).
- Neuer Pflege-Ort: eigener Controller mit Reiter „Arbeitsgänge“ in der Reiterleiste der BDE-Stammdaten, Recht
  Admin, nicht an `BdeAktiv` gebunden; Navigation; Weiterleitung der alten URL.
- Neue UI-Begriffe überall dort, wo das Flag bzw. der Offset benannt wird (Stammdaten, Tracking-Toggle,
  Reporting-Hinweise, Hilfe).
- Liste „nicht konfiguriert“ aus **zwei** Quellen: OSEON-AG-Namen (wie heute) und WMS-Arbeitsgänge
  (`WorkOperation.OperationNumber`, IDEAL-Kürzel).
- Offset-Lookup `GetDueDateOffsetsAsync()` für FA-Struktur Teil 2/3.
- Audit-Felder (`AuditableEntity`, Antwort 2) in **derselben** Migration wie die Umbenennung.

**Out-of-Scope**
- Statusanzeige/Überfällig-Logik in FA-Struktur und Terminal (Teil 2/3).
- Jede Verhaltensänderung an OSEON-Teileverfolgung, OSEON-Reporting, `OseonDueDateCalculator`
  (`Services/OseonDueDateCalculator.cs`) — auch der Name bleibt (Teil 2 AK 18 erwartet „kein Diff“).
- `WorkOperationStructureDetectionService` (Service): unverändert. Er legt Arbeitsgänge weiter über
  `ProductionWorkplace.ArbeitsschrittCode` an (`WorkOperationStructureDetectionService.cs:78-88`, `:168-181`) und
  liest die Arbeitsgang-Stammdaten nicht (Suchbeleg: 0 Treffer für `OseonOperationConfig`/`IsOseonRelevant` in
  `IDEALAKEWMSService/`). Der „Abgleich gegen die Stammdaten“ aus der Notiz wird als Liste „nicht konfiguriert“
  im Web umgesetzt, nicht im Sync.
- ~~Umbenennen der Tabelle in der DB (nur bei Rückfrage 1 = U)~~ — *überholt durch Antwort 1 = U, jetzt In-Scope.*
  Weiterhin Out-of-Scope: eine Kompatibilitäts-View unter dem alten Namen (Nachtrag Antwort 1); das Umschreiben
  historischer Skripte/Migrationen (`SQL/34`, `SQL/44`, `Migrations/2026*`, ADR 0004); das Umbenennen der OSEON-Tracking-ViewModels
  (`OseonOperationViewModel.IsOseonRelevant`, `OseonTrackingViewModel.cs:70` — OSEON-eigenes ViewModel, nicht die
  Entität).
- Spaltenauswahl (Zahnrad) für die neue Tabelle — die übrigen BDE-Stammdaten-Reiter haben auch keine
  (`Views/BdeMasterData/Index.cshtml` ohne `#column-config`; keine `ColumnDefinitions`-Einträge für
  `BdeMasterData*`).

**Größe:** ein Dev-Lauf (Umbenennung ist compiler-geführt, neue Seite ist eine Verschiebung der vorhandenen
`OperationConfig.cshtml`; die Migration ist eine Handvoll guarded SQL-Batches, dreifach geschrieben). Kein Epic
vorgeschlagen. Empfohlene Commit-Reihenfolge im Lauf: (1) Modell + Migration + `SQL/95` + FreshInstall + Drift-Test,
(2) Umbenennung der Verwender, (3) Controller/Views/Navigation, (4) Doku/Hilfe/TS.

## Fachliche Anforderungen

### FA 1 — Datenmodell: allgemeine Arbeitsgang-Stammtabelle

**Ist (belegt):** Entität `OseonOperationConfig` (`Models/OseonOperationConfig.cs:5-33`): `OperationName`
(NVARCHAR(100), Pflicht, eindeutig), `DisplayName` (NVARCHAR(200)), `DueDateOffsetDays` (int, Kommentar „relativ
zum Stanztermin“, `:22-26`), `IsOseonRelevant` (bit, Default true, `:28-32`). Kein `AuditableEntity`. Mapping
`ApplicationDbContext.cs:882-889` mit explizitem `ToTable("OseonOperationConfigs")` (`:884`) und Unique-Index auf
`OperationName` (`:888`). **Wichtig für jede Schema-Variante:** die Eindeutigkeit existiert je nach
Installationsweg in **zwei Formen** — EF-Migration legt den Index `IX_OseonOperationConfigs_OperationName` an
(`Migrations/20260330142035_AddOseonOperationConfig.cs:29-33`), `SQL/34_AddOseonOperationConfig.sql:15` und
`SQL/00_FreshInstall.sql:1232` dagegen den Constraint `UQ_OseonOperationConfigs_OperationName`. Genau diese
Doppelform hat 2026-08-17 schon einmal den App-Start gebrochen ([[fallstricke]] §9 „EF-Migration UND
idempotentes SQL-Skript brauchen BEIDE dieselben Guards“).

**Varianten (Rückfrage 1) — entschieden: U** (Antwort 1, Gerald 2026-10-12, abweichend von der damaligen
Empfehlung H). Die Tabelle bleibt als Entscheidungsgrundlage stehen; H und L sind **verworfen**.

| | **U — Schema umbenennen (gewählt)** | ~~H — nur Code umbenennen~~ (verworfen) | ~~L — nur UI-Texte~~ (verworfen) |
|---|---|---|---|
| Was | Tabelle, Spalte `IsOseonRelevant`, Index/Constraint per Migration umbenennen + SQL + FreshInstall (2 Stellen) | Klasse `OperationConfig`, Property `IsExternalSystemRelevant` mit `HasColumnName("IsOseonRelevant")`, DbSet/Repository/ViewModel umbenennen; Tabelle bleibt `OseonOperationConfigs` | Code bleibt `OseonOperationConfig`/`IsOseonRelevant`, nur Labels + neue Methode |
| Schema-Migration | ja (Rename Table/Column/Index) | nein (Audit-Spalten nur bei Rückfrage 2) | nein |
| Risiko App-Start | **hoch**: EF-`RenameIndex` trifft auf FreshInstall-/Skript-DBs einen `UQ_`-Constraint statt `IX_`-Index → SqlError (§9); muss komplett als guarded `migrationBuilder.Sql` mit `sp_rename` je Form geschrieben werden | keines aus dem Umbenennen (Tabellen-/Spaltenname unverändert) | keines |
| Raw-SQL/externe Leser | brechen: `SQL/34`, `SQL/44_MoveTablesToDboSchema.sql:24` (historisch, bleiben), Prüf-Abfragen in Teil-2-P4 und dieser Spec, unbekannte Ad-hoc-/Reporting-Abfragen von außen (nicht prüfbar) | unberührt | unberührt |
| AgentJobs | nicht betroffen (0 Treffer in `SQL/AgentJobs/`) | nicht betroffen | nicht betroffen |
| Snapshot beim späteren Bündel-Merge | zusätzliche Rename-Operationen im Bündel-Snapshot; der Snapshot divergiert ohnehin schon (Migrationen 89–94 nur im Bündel) | nur Entitätsname im Snapshot (falls überhaupt eine Migration entsteht) | keiner |
| Merge-Fläche zu `main` | groß | mittel: dieselben ~16 Dateien wie U, aber nur Bezeichner | klein |
| Dauerwissen | Name = Bedeutung | Asymmetrie Code↔Tabelle, in [[datenmodell]] zu dokumentieren | „Oseon“ im Code für eine allgemeine Tabelle — genau die Klasse „Bedeutung aus dem Namen geraten“, die hier teuer war |

> [!quote]- Überholt: damalige Empfehlung H (vor Antwort 1)
> **Empfehlung H.** Das Umbenennen im Code beseitigt die irreführende Benennung dort, wo Agenten und Entwickler
> lesen, und kostet nur compiler-geführte Bezeichner-Änderungen. Das Schema-Umbenennen (U) bringt keinen
> fachlichen Nutzen, aber genau das bekannte Index/Constraint-Startrisiko (§9) plus nicht prüfbare externe Leser.
> L wäre die faulste Sprosse, lässt aber „Oseon“ als Namen einer IDEAL-Stammtabelle stehen. Präzedenz für eine
> dokumentierte Code↔UI-Asymmetrie: `WorkStep` / „FA-Vorbau-AG“ ([[fallstricke]] §2).
>
> *Überholt durch Antwort 1 = U. Die „nicht prüfbaren externen Leser“ hat der Nachtrag zu Antwort 1 ausgeräumt
> („Keine externen Leser auf die Tabelle“); das Startrisiko §9 wird unten mit Gegenmaßnahmen abgesichert.*

**Neue Namen (verbindlich, Antwort 1 = U):**

| Ebene | heute (belegt) | neu |
|---|---|---|
| Tabelle | `dbo.OseonOperationConfigs` (`SQL/00_FreshInstall.sql:1225`, `ApplicationDbContext.cs:884`, Snapshot `ApplicationDbContextModelSnapshot.cs:1804`) | `dbo.OperationConfigs` |
| Spalte | `IsOseonRelevant` (`SQL/00_FreshInstall.sql:1230`, Snapshot `:1791`) | `IsExternalSystemRelevant` (passend zu `WorkOperation.IsExternalSystem`, `Models/WorkOperation.cs:30-31`) |
| Primärschlüssel | `PK_OseonOperationConfigs` (EF `20260330142035_AddOseonOperationConfig.cs:26`, `SQL/34:14`, `00_FreshInstall.sql:1231`) | `PK_OperationConfigs` (EF-Namensschema `PK_<Tabelle>`) |
| Eindeutigkeit `OperationName`, EF-Form | Index `IX_OseonOperationConfigs_OperationName` (`20260330142035_AddOseonOperationConfig.cs:29-33`) | Index `IX_OperationConfigs_OperationName` (EF-Default für `HasIndex(OperationName).IsUnique()`) |
| Eindeutigkeit `OperationName`, Skript-/FreshInstall-Form | Constraint `UQ_OseonOperationConfigs_OperationName` (`SQL/34:15`, `00_FreshInstall.sql:1232`) | **entfällt** — wird in denselben Unique-**Index** `IX_OperationConfigs_OperationName` überführt (siehe unten) |
| Audit (neu) | — | `CreatedAt`, `CreatedBy`, `CreatedByWindows`, `ModifiedAt`, `ModifiedBy`, `ModifiedByWindows` |
| Klasse / DbSet | `OseonOperationConfig` / `OseonOperationConfigs` | `OperationConfig` (`Models/OperationConfig.cs`, erbt `AuditableEntity`) / `OperationConfigs` |
| Repository | `IOseonOperationConfigRepository`, `OseonOperationConfigRepository`, `CachedOseonOperationConfigRepository` | `IOperationConfigRepository`, `OperationConfigRepository`, `CachedOperationConfigRepository` |
| Controller / View | `SettingsController.OperationConfig` + `Views/Settings/OperationConfig.cshtml` | `OperationConfigsController` + `Views/OperationConfigs/Index.cshtml` |
| ViewModel | `OseonOperationConfigViewModel` (`OseonTrackingViewModel.cs:73-77`) | `OperationConfigIndexViewModel` (`Models/ViewModels/OperationConfigIndexViewModel.cs`) |
| Migration / Skript | — | `<Timestamp>_RenameOperationConfigsAndAddAudit` / `SQL/95_RenameOperationConfigsAndAddAudit.sql` |

Suchbeleg: keine bestehende Klasse `OperationConfig`/`OperationConfigsController` und keine Tabelle
`OperationConfigs` im Bündel. `DueDateOffsetDays`, `OperationName`, `DisplayName` bleiben.

**Technische Festlegung — Eindeutigkeit konvergiert auf eine Form** (zur Bestätigung als Rückfrage 8, weil Antwort 1
wörtlich „Index-/Constraint-Umbenennung“ sagt): Heute hängt die Form am Installationsweg
(EF-DB: Unique-Index `IX_`, Skript-/FreshInstall-DB: Unique-Constraint `UQ_`). Die Migration benennt den `IX_`-Index
um und **ersetzt** den `UQ_`-Constraint durch den Unique-Index `IX_OperationConfigs_OperationName` (Index zuerst
anlegen, dann Constraint droppen — keine Lücke in der Eindeutigkeit; Duplikate sind ausgeschlossen, solange der
Constraint besteht). Danach gibt es auf **jeder** DB genau eine Form mit genau dem Namen, den das EF-Modell erwartet.
Verworfen: `UQ_…` → `UQ_OperationConfigs_OperationName` nur umbenennen — billiger, ließe aber die Doppelform (Ursache
von [[fallstricke]] §9) für jede künftige EF-Index-Operation auf dieser Tabelle stehen; ebenso verworfen: den
Constraint auf den `IX_`-Namen umbenennen — gleicher Name, andere Objektart; ein späteres EF-`DropIndex` scheiterte
dann mit „explizites DROP INDEX auf Constraint-Index nicht erlaubt“ statt mit 3701.
Die unbenannten System-Defaults (`DEFAULT 0`/`DEFAULT 1`, `SQL/34:12-13`, `00_FreshInstall.sql:1229-1230`) auf
Skript-/FreshInstall-DBs bleiben unangetastet; `sp_rename` der Spalte nimmt den Default mit. Sie sind dem EF-Modell
unbekannt und wirkungslos, weil EF beide Werte immer schreibt.

**Risiken der Variante U → Gegenmaßnahme → Nachweis:**

| # | Risiko | Gegenmaßnahme | AK |
|---|---|---|---|
| R1 | EF-Default-Generierung (`RenameIndex`/`RenameTable`/`DropPrimaryKey`) trifft auf Skript-/FreshInstall-DBs den `UQ_`-Constraint statt des `IX_`-Index → SqlError beim App-Start ([[fallstricke]] §9) | `Up()`/`Down()` vollständig als guarded `migrationBuilder.Sql` mit `sp_rename` je Form; keine strukturierte Operation in der Migration | 15, 19, 21 |
| R2 | EF erkennt Klassen- **und** Tabellen-Umbenennung als „Tabelle weg, neue Tabelle da“ → generiertes `DropTable`/`CreateTable` = Datenverlust | Generierten Inhalt von `Up()`/`Down()` komplett verwerfen; Drift-Test verbietet `DropTable(`/`CreateTable(` in der Migration; `migrations script` ohne `DROP TABLE`/`CREATE TABLE` | 19, 21 |
| R3 | Snapshot-Drift → `PendingModelChangesWarning` verweigert den Start ([[fallstricke]] §8) | Snapshot/Designer bleiben **so, wie `migrations add` sie erzeugt**; nur `Up()`/`Down()` werden ersetzt; `has-pending-model-changes` leer | 15, 19 |
| R4 | `SQL/95`, Migration und FreshInstall ergeben unterschiedliche Namen (zwei Deploy-Wege, zwei Schemas) | dieselben Batches in Migration und `SQL/95`; FreshInstall legt direkt den Zielzustand an; Drift-Test vergleicht die Namen in allen drei Dateien + Snapshot | 19, 20 |
| R5 | Falsche `MigrationId` im Skript → EF replayt beim Start (Altlast `SQL/34:46` `20260330120000_…` vs. EF/FreshInstall `20260330142035_…`, `00_FreshInstall.sql:2293`) | `MigrationId` exakt aus dem erzeugten Dateinamen; Drift-Test prüft sie in `SQL/95` (1×) und FreshInstall (2×) | 19 |
| R6 | Anomalie: alte **und** neue Tabelle existieren (z. B. FreshInstall versehentlich auf bestehender DB) → stilles Weiterarbeiten auf der leeren neuen Tabelle | Batch 1 bricht mit `THROW` und Klartext ab (melden statt still); FreshInstall legt `OperationConfigs` nur an, wenn **keine** der beiden Tabellen existiert, sonst `PRINT`-Warnung | 22 |
| R7 | Alte Web-Binaries laufen nach dem Skript weiter (oder ein lokaler `dotnet run` mit neuem Stand migriert die Produktiv-DB, [[fallstricke]] §8 „Lokaler `dotnet run` …“) | Deploy-Reihenfolge: App-Pool stoppen → Backup → `SQL/95` → neue Web-Binaries → Start; kein lokaler Start gegen `AKESQL20` vor dem Deploy | Deploy, S9 |
| R8 | Raw-SQL/Prüfabfragen mit altem Namen brechen | Inventur unten (keine Raw-SQL-Stelle im Anwendungscode, Service, AgentJobs); Prüfabfragen dieser Spec in beiden Fassungen; FA-Struktur Teil 2 P4 gemeldet | 7a |
| R9 | Rename im Test nicht abgedeckt (InMemory führt kein DDL aus) | Manual-UAT gegen drei DB-Formen (S9) + `SQL/95` zweimal | 20, 22 |
| R10 | Eine DB trägt nur die Altlast-ID `20260330120000_AddOseonOperationConfig` (`SQL/34:43-46`, Guard per `LIKE`). Heute bräche dort schon der Start (EF replayt `20260330142035_…` gegen die vorhandene Tabelle); **nach** `SQL/95` liefe dieser Replay dagegen durch und legte still eine leere `OseonOperationConfigs` neben `OperationConfigs` an | Prüfschritt P1 **vor** dem Deploy: fehlt `20260330142035_AddOseonOperationConfig` in der History → Deploy auf dieser DB anhalten und klären (nicht still nachtragen) | P1 |

### FA 2 — Verwender von `IsOseonRelevant` (vollständig, Bündel v1.50.0)

Suche `IsOseonRelevant` über den ganzen Bündel-Worktree, ohne `Migrations/`, `docs/`, `secondbrain_alt/`:
**16 Dateien** (nachgemessen v1.52.0: unverändert). Dazu die EF-Migrationsdateien
(`20260330142035_AddOseonOperationConfig.cs:22`, deren Designer und alle späteren `*.Designer.cs`) — historisch,
werden nicht editiert; `ApplicationDbContextModelSnapshot.cs:1776-1805` erhält die neuen Namen **nur** durch
`dotnet ef migrations add` (nicht von Hand). Treffer in `wwwroot/js`: **0**. Treffer in `IDEALAKEWMSService/` und
`SQL/AgentJobs/`: **0**.

| # | Datei:Zeile | Art | Änderung (Antwort 1 = U) |
|---|---|---|---|
| 1 | `Models/OseonOperationConfig.cs:32` | Definition | → `OperationConfig.IsExternalSystemRelevant` (Spalte gleichen Namens), Doku neutral |
| 2 | `Controllers/SettingsController.cs:122,141,151,159` | Pflege-Actions | entfallen hier, wandern in `OperationConfigsController` (Parameter `isExternalSystemRelevant`) |
| 3 | `Controllers/TrackingController.cs:319,386` | Relevanzfilter OSEON-Liste/Details | nur Umbenennung, Logik gleich |
| 4 | `Controllers/OseonReportingController.cs:148` | AG-Auswahlliste Reporting | nur Umbenennung |
| 5 | `Services/Oseon/OseonGroupViewModelBuilder.cs:44` (Entität), `:69,:78` (ViewModel) | Ampel/Fertig-Logik | `:44` umbenennen; `:69,:78` lesen das ViewModel → unverändert |
| 6 | `Data/Repositories/OseonProductionOrderRepository.cs:256` | Reporting überspringt nicht relevante | nur Umbenennung |
| 7 | `Data/Repositories/IOseonProductionOrderRepository.cs:30` | XML-Kommentar | Text anpassen |
| 8 | `Program.cs:495` (v1.52.0) | Seed | Umbenennung + Audit-Werte `"system"` (Antwort 2); Seed-Bedingung `:471` unverändert (Antwort 3) |
| 9 | `Models/ViewModels/OseonTrackingViewModel.cs:70` | **ViewModel**-Property `OseonOperationViewModel.IsOseonRelevant` | unverändert (OSEON-ViewModel) |
| 10 | `Views/Tracking/_OseonGroupDetails.cshtml:69,80` | liest #9 | unverändert |
| 11 | `Views/Settings/OperationConfig.cshtml:64,66,143-146` | Pflege-View | Datei entfällt, Inhalt → `Views/OperationConfigs/Index.cshtml` |
| 12 | `SQL/34_AddOseonOperationConfig.sql:13,24` | historisches Skript | unverändert (ADR 0004: Skripte werden nicht umgeschrieben) |
| 13 | `SQL/00_FreshInstall.sql:1230,1494` | Konsolidat | Spalte → `IsExternalSystemRelevant`, Tabelle → `OperationConfigs`, Audit-Spalten, IX statt UQ (siehe Migrations-Abschnitt) |
| 14 | `IdealAkeWms.Tests/Controllers/OseonReportingControllerTests.cs:68` | Test | Umbenennung |
| 15 | `IdealAkeWms.Tests/Repositories/OseonProductionOrderRepositoryReportingTests.cs:47` | Test | Umbenennung |
| 16 | `IdealAkeWms.Tests/Repositories/OseonOperationConfigRepositoryTests.cs:12,46,74,75,115,120` | Test | Umbenennung (Datei → `OperationConfigRepositoryTests.cs`) |

Weitere Verwender der **Entität/des Repositorys** ohne `IsOseonRelevant`: `Program.cs:125-126` (DI, v1.52.0),
`OseonReportingQueryResult.cs:8`, `OseonProductionOrderRepository.cs:213-215` (Offset-Max fürs Datumsfenster),
`OseonTrackingViewModel.cs:73-77` (`OseonOperationConfigViewModel`, nur von der Settings-View genutzt),
`TrackingControllerTests.cs:19-28` (Mock). Text-Verweise: `docs/TESTSZENARIEN.md:3741` (TS-16.5),
`README.md:105` (Skriptliste, bleibt).

### FA 3 — Pflege-Ort: Reiter „Arbeitsgänge“ in den BDE-Stammdaten

**Ist (belegt):** `BdeMasterDataController` trägt auf Klassenebene `[RequireBdeActive]` + `[RequireBdeShiftleadAccess]`
(`BdeMasterDataController.cs:10-11`). Der Filter leitet bei `BdeAktiv` ≠ `true` mit „BDE ist nicht aktiviert.“ auf
Home um (`Filters/RequireBdeActiveAttribute.cs:24-39`). Das BDE-Menü erscheint nur bei `bdeAktiv && canUseBde`
(`Views/Shared/_Layout.cshtml:45,244`), der Eintrag „Stammdaten“ darin nur für `canManageBdeShiftlead` (`:252-257`).
Die Reiter sind Links mit `?tab=` (`Views/BdeMasterData/Index.cshtml:13-26`). Heutige Pflege:
`SettingsController` mit `[RequireAdminAccess]` (`SettingsController.cs:10`; Filter prüft `IsAdminAsync`,
`RequireAdminAccessAttribute.cs:23`).

**Lösung (Empfehlung, technisch entschieden):** eigener Controller `OperationConfigsController` mit
`[RequireAdminAccess]` auf Klassenebene und **ohne** `[RequireBdeActive]`. Präzedenz im Bündel:
`BdeShiftCalendarController` ist ein BDE-Admin-Bereich mit nur `[RequireAdminAccess]` (`BdeShiftCalendarController.cs:11`).
Verworfen:
- *`[RequireBdeActive]` je Action statt Klasse* — 7 Actions umzustellen (`BdeMasterDataController.cs:66-345`), jede
  künftige Action muss daran denken; der sichere Default ginge verloren.
- *Ausnahme-Attribut (`[SkipBdeActive]` + Prüfung im Filter)* — ändert einen Filter, den fünf Controller teilen
  (`BdeBookings`, `BdeApi`, `BdeCockpit`, `BdeMasterData`, `BdeTerminal`), für einen Einzelfall.
- *Action in `BdeMasterDataController`* — scheitert am Klassen-Filter.

Damit die Seite „in den BDE-Stammdaten“ steht: die Reiterleiste wird ein Partial
`Views/Shared/_BdeMasterDataTabs.cshtml` (Parameter: aktiver Reiter), eingebunden in `BdeMasterData/Index.cshtml`
und `OperationConfigs/Index.cshtml`, Seitentitel beider Seiten „BDE-Stammdaten“. Sichtbarkeit je Reiter:
Operatoren/Aktivitäten/Terminals nur bei `BdeAktiv = true` **und** `CanManageBdeShiftleadAsync()`;
„Arbeitsgänge“ nur bei `IsAdminAsync()`. Ein Admin bei BDE aus sieht also nur den Reiter „Arbeitsgänge“; ein
Schichtleiter bei BDE an sieht die drei BDE-Reiter, aber nicht „Arbeitsgänge“.

**Navigation (Antwort 4):** zusätzlich ein Eintrag „Arbeitsgänge“ im Admin-Block des Stammdaten-Dropdowns
(`_Layout.cshtml:308-319`, Stand v1.52.0; Block hinter `@if (isAdmin)` `:308`), sichtbar unabhängig von `BdeAktiv`,
weil das BDE-Menü bei `BdeAktiv = false` gar nicht gerendert wird (`:249`) — sonst wäre die Seite laut Entscheidung 1
an keinem Standort per Menü erreichbar.

**Alte URL (Antwort 5):** `GET /Settings/OperationConfig` → `RedirectToAction("Index", "OperationConfigs")`
(302; ein 301 würde vom Browser dauerhaft gecacht und wäre bei einer Rücknahme lästig). Die drei POST-Actions
(`SettingsController.cs:120-178`) entfallen — ihre Formulare existieren nicht mehr. Der Knopf
„Arbeitsgang-Konfiguration“ in `/Settings` (`Views/Settings/Index.cshtml:10-16`) zeigt auf die neue Seite und
heißt „Arbeitsgänge (BDE-Stammdaten)“. Links/Bookmarks: einziger weiterer Link im Code
`Views/OseonReporting/OperationsOverview.cshtml:27` → auf die neue Seite, Text „BDE-Stammdaten › Arbeitsgänge“;
Hilfe `Views/Help/Index.cshtml:745` und `:1431-1433` (heute „Einstellungen → AG-Konfiguration“) → neuer Ort.

**Rolle:** keine neue Rolle, kein neuer Filter (`RequireAdminAccess` vorhanden). In [[controller]] Z.53 die
Controller-Liste von `[RequireAdminAccess]` um `OperationConfigsController` ergänzen. `RoleOverview.cshtml` bleibt
(nur Admin, keine Rollenänderung). Hinweis: TS-7.10 nennt heute „Rolle `masterdata` oder `admin`“
(`docs/TESTSZENARIEN.md:2490`), der Code lässt nur Admin zu; die View prüfte zusätzlich
`HasMasterDataAccessAsync` für `canEdit` (`OperationConfig.cshtml:5`) — für Nicht-Admins nie erreichbar. Die neue
View lässt diese Prüfung weg; TS-7.10 wird auf „admin“ korrigiert.

### FA 4 — Begriffe (UI deutsch)

| Stelle | heute | neu |
|---|---|---|
| Spalte/Schalter Relevanz (Stammdaten) | „OSEON-relevant“ (`OperationConfig.cshtml:42,146,167`) | „Fremdsystem-relevant“ |
| Offset-Spalte/Feld | „Offset (AT)“ / „Offset (Arbeitstage)“ (`:41,137`) | „Offset (Arbeitstage) zum Bezugstermin“ (Spaltenkopf darf „Offset (AT)“ mit `title`/Legende bleiben) |
| Seitenüberschrift | „Arbeitsgang-Konfiguration (OSEON)“ (`:8`) | „BDE-Stammdaten“ + Reiter „Arbeitsgänge“ |
| Hilfetext unter dem Formular | „Negativ = vor Stanztermin …“ (`:139`) | „Negativ = vor dem Bezugstermin, positiv = danach“ |
| Tracking-Toggle | „OSEON-Relevanz“ (`Views/Tracking/OseonIndex.cshtml:55`) | „Fremdsystem-Relevanz“ |
| Reporting leer | „Keine OSEON-relevanten Arbeitsgänge …“ (`OperationsOverview.cshtml:154`) | „Keine fremdsystem-relevanten Arbeitsgänge …“ |
| Hilfe | `Help/Index.cshtml:745, 762-763, 1431-1433` | neuer Ort + neuer Begriff |

**Legende (Karte rechts, ersetzt `OperationConfig.cshtml:158-169`):**
- **Offset (Arbeitstage) zum Bezugstermin** — Arbeitstage (Mo–Fr ohne gepflegte Feiertage) relativ zum
  Bezugstermin; `-1` = ein Arbeitstag davor, `0` = am Bezugstermin, `+2` = zwei Arbeitstage danach.
  Bezugstermin je Fremdsystem: **OSEON:** Stanztermin (OSEON-Endtermin) · **IDEAL:** FE-Termin · **AKE-BDE:**
  Fertigungstermin (`ProductionDate`).
- **Kein Eintrag** — Offset 0 (Soll = Bezugstermin); das Kürzel erscheint unter „Nicht konfigurierte Kürzel“.
- **Fremdsystem-relevant** — (Antwort 6) „Wirkt derzeit nur in der OSEON-Teileverfolgung (Fertig-/Ampel-Berechnung)
  und im OSEON-Reporting; für IDEAL und AKE-BDE ohne Wirkung.“ Default beim Anlegen bleibt „an“.

Der Begriff „Fremdsystem-relevant“ wird nur als UI-Text geführt; Code und Spalte heißen `IsExternalSystemRelevant`.

### FA 5 — Liste „nicht konfiguriert“ und Abgleich IDEAL

**Ist (belegt):** `GetUnconfiguredOperationNamesAsync` (`OseonOperationConfigRepository.cs:55-67`) liefert
`DISTINCT OseonWorkOperations.Name` ohne Konfigurationseintrag; einziger Aufrufer ist das Add-Formular der
Settings-Seite (`SettingsController.cs:115`, Dropdown „… (aus OSEON)“, `OperationConfig.cshtml:115-126`). Der
Vergleich läuft als SQL-`IN` → Groß/Klein hängt an der DB-Collation (Prüfschritt P2).

**Soll:** neue Repository-Methode `GetUnconfiguredOperationsAsync()` (Ersatz für `GetUnconfiguredOperationNamesAsync`)
liefert Einträge `(Kürzel, Quelle)`:
- Quelle **„OSEON“**: `DISTINCT OseonWorkOperations.Name` (wie heute);
- Quelle **„WMS-Arbeitsgang“**: `DISTINCT WorkOperations.OperationNumber` — das ist der Schlüssel, den Teil 2
  (Kürzel des Struktur-Tokens = `OperationNumber`, `WorkOperationStructureDetectionService.cs:171`) und Teil 3
  (`OperationNumber`) nachschlagen; **ohne** `"01"` (Antwort 7; Default-Marker,
  `BdeDefaultWorkOperationService.cs:28,37`; [[fallstricke]] §16 „Existenz-Check des Default-AG über
  OperationNumber "01"“).
- Abgleich gegen die Stammdaten **in C#**, getrimmt und `StringComparer.OrdinalIgnoreCase` (gleiche Regel wie der
  Offset-Lookup FA 6) — damit „nicht konfiguriert“ und „Offset gefunden“ nie auseinanderlaufen. Ein Kürzel aus
  beiden Quellen erscheint einmal mit beiden Quellen.
- Schreibende Pfade auf `WorkOperations` (Suchbeleg `new WorkOperation`/`WorkOperations.Add`, ohne Tests/Doku):
  nur `WorkOperationStructureDetectionService.cs:168` (IDEAL-Kürzel) und `BdeDefaultWorkOperationService.cs:34-47`
  (`"01"`); kein Insert in `SQL/`/`SQL/AgentJobs/`. Welche `OperationNumber`-Werte auf den produktiven DBs
  tatsächlich stehen, ist nur per Prüfschritt P4 feststellbar.

**Anzeige:** Karte „Nicht konfigurierte Kürzel“ **oberhalb** der Tabellen-Toolbar (nicht zwischen Toolbar und
`.table-responsive`, [[fallstricke]] §19), als einfache Liste (keine zweite filterbare Tabelle, [[fallstricke]]
§3 „Eine filterbare Tabelle pro gerenderter Seite“): Kürzel + Quelle; leer → „Alle bekannten Kürzel sind
konfiguriert.“ Das Add-Formular bietet dieselben Kürzel im Dropdown an (Label „KA (WMS-Arbeitsgang)“ bzw.
„B (OSEON)“), manuelle Eingabe bleibt (vorhandenes Skript `OperationConfig.cshtml:173-200` übernehmen).

**Doppelte Kürzel nach Normalisierung** (nur möglich bei case-sensitiver Collation oder führenden Leerzeichen
in Altdaten — Prüfschritt P2): Seite zeigt einen Warnhinweis mit den betroffenen Kürzeln (melden statt still);
Add lehnt ein Kürzel ab, das getrimmt und case-insensitiv schon existiert (heute `ExistsAsync` über SQL-`==`,
`OseonOperationConfigRepository.cs:91-95`, collation-abhängig → durch C#-Vergleich ersetzen).

**Kollisionen OSEON-Seed ↔ IDEAL-Kürzel (aus Teil-2-P4):** Jede Site hat ihre eigene DB; auf der IDEAL-DB stehen
die 12 OSEON-Seed-Kürzel, sobald die Web-App dort einmal mit leerer Tabelle gestartet ist (`Program.cs:470`). Ein
IDEAL-Kürzel gleichen Namens (z. B. `EG`, `BG`) bekäme still den OSEON-Offset (`BG` = +2). Erkennung über
Prüfschritt P3; Behandlung (Antwort 3): Seed-Code bleibt, auf der IDEAL-DB nach P3 kollidierende Einträge per UI auf
IDEAL-Werte setzen, übrige OSEON-Einträge per UI löschen oder stehen lassen (wirkungslos ohne OSEON-Daten) — kein Skript.

### FA 6 — Schnittstelle für FA-Struktur Teil 2/3 (Offset-Lookup)

Erfüllt exakt [[2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-2-spec]] FA 4 „Schnittstelle zur Vorbedingung“:

```csharp
// IOperationConfigRepository
Task<IReadOnlyDictionary<string, int>> GetDueDateOffsetsAsync();
```

- **ein** Aufruf je Seitenaufbau liefert alle Offsets; Schlüssel = `OperationName.Trim()`, Wert =
  `DueDateOffsetDays`; Dictionary mit `StringComparer.OrdinalIgnoreCase` (Lookup mit `ka` trifft `KA`; Aufrufer
  trimmen ihren Schlüssel selbst oder die Methode-Doku sagt es).
- „kein Eintrag“ = fehlender Schlüssel, unterscheidbar von Wert 0.
- wertet „Fremdsystem-relevant“ **nicht** aus (alle Zeilen).
- bei doppeltem normalisiertem Schlüssel gewinnt die Zeile mit kleinster `Id` (deterministisch; gemeldet über FA 5).
- gecacht im Decorator (ADR [[0001-repository-pattern-mit-decorator-fuer-caching]]): eigener Cache-Key, in
  `InvalidateCache` (`CachedOseonOperationConfigRepository.cs:40-44`) mit entfernt; 10 Minuten wie die übrigen
  Keys (`:10`).
- Der Bezugstermin und die Rechenregel (`OseonDueDateCalculator.Calculate`) bleiben beim Aufrufer.

### FA 7 — OSEON-Verhalten bleibt identisch

Festgehaltener Ist-Zustand, der **nicht** angeglichen wird:
- Teileverfolgung: Dictionary aus `GetAllAsDictionaryAsync` mit Default-Comparer, also **case-sensitiv**
  (`OseonOperationConfigRepository.cs:46-50`), Lookup `TryGetValue(op.Name, …)` (`OseonGroupViewModelBuilder.cs:43`);
  ohne Eintrag: relevant (`:44`) und Soll = `DueDate` (Offset 0, `:50-52`). Relevanz-Mengen für die DB-Filter
  aus demselben Dictionary (`TrackingController.cs:312-322`, `:381-389`), in SQL als `Contains`
  (`OseonProductionOrderRepository.cs:109`, collation-abhängig).
- Reporting: eigenes Dictionary, **case-insensitiv** (`OseonProductionOrderRepository.cs:214`); Zeilen ohne
  Konfiguration werden gezählt und **übersprungen** (`:250-254`, Controller `OseonReportingController.cs:74`),
  nicht relevante übersprungen (`:256`).
- Der neue, case-insensitive Lookup (FA 6) ist eine **getrennte Methode**; `GetAllAsDictionaryAsync` und die
  Reporting-Abfrage bleiben bitgleich im Verhalten (nur Bezeichner).

## Technischer Lösungsentwurf

- **Muster:** Repository + Caching-Decorator (ADR 0001), MVC-Controller mit `TempData["SuccessMessage"]` /
  `TempData["WarningMessage"]` + `RedirectToAction` (wie `SettingsController.cs:120-178`), Listen-View-Pattern
  (ADR [[0005-listen-view-pattern-mit-server-side-spaltenfilter]]) im Stil der BDE-Stammdaten-Reiter.
- **Leiter (ponytail):** vorhandene Tabelle statt neuer (Sprosse 2), vorhandene View verschieben statt neu bauen,
  vorhandener Admin-Filter statt neuer Rolle, Partial für die Reiterleiste statt Duplikat, Calculator unverändert.
- ~~**Umbenennung (H):** … `has-pending-model-changes` muss „keine Änderungen“ melden, solange Rückfrage 2 = nein …~~
  *Überholt durch Antwort 1 = U / Antwort 2 = ja.*
- **Umbenennung (U):** Code-Bezeichner mechanisch per IDE/Compiler (Verhalten unverändert), Schema über **eine**
  Migration nach dem Ablauf im Abschnitt „Migrations-/SQL-Auswirkungen“. `OperationConfig` erbt `AuditableEntity`;
  das eigene `Id` der Klasse entfällt (die Basis bringt es, `Models/AuditableEntity.cs:5`), `HasKey(e => e.Id)` bleibt.
- **Controller `OperationConfigsController`:** `Index(int page = 1, int? pageSize = null)` mit
  `PageSize.Resolve`/`ResolveRaw` + `PaginationState` (Muster `BdeMasterDataController.cs:66-122`),
  `ColumnFilterHelper.ReadFromQuery` + `Apply` mit ColumnMap `name`/`display`/`offset`/`relevant` (Getter liefern den
  **gerenderten** Text: Offset mit Vorzeichen wie angezeigt, Relevanz „Ja“/„Nein“ — [[fallstricke]] §3 „Universal-
  Filter-Pattern“); `Add`/`Update`/`Delete` als POST mit `[ValidateAntiForgeryToken]`. Ctor: Repository,
  `ICurrentUserService`, `IAppSettingRepository` (für `BdeAktiv` in der Reiterleiste — oder im Partial per
  `@inject` wie `_Layout.cshtml:2`).
- **View:** Tabelle `filterable-table` mit `data-server-column-filter="true"`, `data-view-key="OperationConfigs"`,
  `data-col-key` auf **jedem** `<th>` (inkl. Aktionsspalte, Muster `BdeMasterData/Index.cshtml:43-47`),
  `column-preferences.js` vor `table-filter.js` (§3), `_Pagination`-Partial. Inline-Edit je Zeile wie heute
  (Formular per `form=`-Attribut; Relevanz: Hidden trägt den `name`, Checkbox ohne `name` mit `onchange` — §3
  „Hidden + Checkbox“). Lösch-Bestätigung mit `confirm()` wie heute; Kürzel per Razor-Encoding, nie
  `@Html.Raw` in JS-Literal (§17).
- **frontend-design-Skill ist im Dev-Lauf Pflicht** (Views + Layout); Bootstrap-Bestand, WCAG AA, Relevanz nicht nur
  über Farbe.

## Migrations-/SQL-Auswirkungen

> [!quote]- Überholt: Fallunterscheidung H ohne/mit Audit (vor Antworten 1 und 2)
> Die frühere Fassung unterschied „H ohne Audit“ (keine Migration), „H mit Audit“ (additive Migration
> `AddOperationConfigAudit`, `SQL/95_AddOperationConfigAudit.sql`, `COL_LENGTH('dbo.OseonOperationConfigs', …)`) und
> „U (falls gewählt)“. Durch Antwort 1 = U und Antwort 2 = ja gilt nur noch der Ablauf unten; Name und Inhalt von
> Migration/Skript sind entsprechend geändert.

**Eine** Migration `<Timestamp>_RenameOperationConfigsAndAddAudit` (Timestamp nach `20261012073551`) mit
Umbenennung **und** Audit-Spalten (Antwort 2: „Mit U in derselben Migration“), gespiegelt in
`SQL/95_RenameOperationConfigsAndAddAudit.sql` und `SQL/00_FreshInstall.sql`. Nicht daten-destruktiv (Umbenennen +
additive Spalten + Index-Tausch ohne Lücke), trotzdem **DB-Backup vor Deploy**.

### Ablauf im Dev-Lauf (gegen Snapshot-Drift und §9)

1. Modell ändern: Klasse `OperationConfig : AuditableEntity` (eigenes `Id` entfernen), Property
   `IsExternalSystemRelevant`, DbSet `OperationConfigs`, Mapping `ToTable("OperationConfigs")`, `HasIndex(OperationName)
   .IsUnique()` bleibt (ohne `HasDatabaseName` → EF-Name `IX_OperationConfigs_OperationName`), vier Audit-Zeilen
   `HasMaxLength(200)` (+ `IsRequired()` für `CreatedBy`/`CreatedByWindows`) wie `ApplicationDbContext.cs:88-91`.
2. `dotnet ef migrations add RenameOperationConfigsAndAddAudit --project IdealAkeWms`. Ergebnis: `.cs`, `.Designer.cs`
   und `ApplicationDbContextModelSnapshot.cs` tragen die neuen Namen. **Snapshot und Designer werden danach nicht mehr
   angefasst.**
3. Den generierten Inhalt von `Up()` **und** `Down()` vollständig löschen (je nach Differ-Erkennung stehen dort
   `RenameTable`/`RenameColumn`/`RenameIndex`/`DropPrimaryKey`/`AddPrimaryKey`/`AddColumn` — oder sogar
   `DropTable`+`CreateTable`, weil Klasse **und** Tabelle umbenannt sind = Datenverlust) und durch die Batches unten als
   einzelne `migrationBuilder.Sql(@"…")`-Aufrufe ersetzen (ein Aufruf = ein Batch, **kein** `GO`, [[fallstricke]] §9).
4. `dotnet ef migrations has-pending-model-changes --project IdealAkeWms` → muss „keine Änderungen“ melden (der Snapshot
   aus Schritt 2 entspricht dem Modell; das Ersetzen von `Up()` ändert daran nichts — sonst `PendingModelChangesWarning`,
   [[fallstricke]] §8).
5. `dotnet ef migrations script 20261012073551_AddBeschichtungsauftrag <neue MigrationId> --project IdealAkeWms` und
   die Ausgabe auf `DROP TABLE`, `CREATE TABLE`, `DROP INDEX` sowie `sp_rename` **ohne** vorangehendes `IF` prüfen
   (Offline-Verifikation nach [[fallstricke]] §9) — erwartet: keine Treffer außer den guarded Batches.

### Batches (identisch in `Up()` und `SQL/95`; im Skript je Batch `GO`)

```sql
-- B1 Tabelle (Anomalie melden statt still)
IF OBJECT_ID(N'dbo.OseonOperationConfigs', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.OperationConfigs', N'U') IS NOT NULL
    THROW 50001, N'OseonOperationConfigs und OperationConfigs existieren beide - manuell klaeren, keine automatische Umbenennung.', 1;
IF OBJECT_ID(N'dbo.OseonOperationConfigs', N'U') IS NOT NULL
    EXEC sp_rename N'dbo.OseonOperationConfigs', N'OperationConfigs';

-- B2 Primaerschluessel (beide DB-Formen heissen gleich)
IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = N'PK_OseonOperationConfigs' AND parent_object_id = OBJECT_ID(N'dbo.OperationConfigs'))
    EXEC sp_rename N'dbo.PK_OseonOperationConfigs', N'PK_OperationConfigs', N'OBJECT';

-- B3 Spalte
IF COL_LENGTH(N'dbo.OperationConfigs', N'IsOseonRelevant') IS NOT NULL AND COL_LENGTH(N'dbo.OperationConfigs', N'IsExternalSystemRelevant') IS NULL
    EXEC sp_rename N'dbo.OperationConfigs.IsOseonRelevant', N'IsExternalSystemRelevant', N'COLUMN';

-- B4 EF-Form: Unique-Index umbenennen (MUSS vor B5 laufen)
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OseonOperationConfigs_OperationName' AND object_id = OBJECT_ID(N'dbo.OperationConfigs'))
    EXEC sp_rename N'dbo.OperationConfigs.IX_OseonOperationConfigs_OperationName', N'IX_OperationConfigs_OperationName', N'INDEX';

-- B5 Skript-/FreshInstall-Form: Ziel-Index anlegen, falls noch keiner da ist
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OperationConfigs_OperationName' AND object_id = OBJECT_ID(N'dbo.OperationConfigs'))
    CREATE UNIQUE INDEX [IX_OperationConfigs_OperationName] ON [dbo].[OperationConfigs] ([OperationName]);

-- B6 ... und erst danach den alten UQ-Constraint entfernen
IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = N'UQ_OseonOperationConfigs_OperationName' AND parent_object_id = OBJECT_ID(N'dbo.OperationConfigs'))
    ALTER TABLE [dbo].[OperationConfigs] DROP CONSTRAINT [UQ_OseonOperationConfigs_OperationName];

-- B7 Audit-Spalten nullable anlegen (je Spalte COL_LENGTH-Guard)
IF COL_LENGTH(N'dbo.OperationConfigs', N'CreatedAt') IS NULL         ALTER TABLE [dbo].[OperationConfigs] ADD [CreatedAt] DATETIME2 NULL;
IF COL_LENGTH(N'dbo.OperationConfigs', N'CreatedBy') IS NULL         ALTER TABLE [dbo].[OperationConfigs] ADD [CreatedBy] NVARCHAR(200) NULL;
IF COL_LENGTH(N'dbo.OperationConfigs', N'CreatedByWindows') IS NULL  ALTER TABLE [dbo].[OperationConfigs] ADD [CreatedByWindows] NVARCHAR(200) NULL;
IF COL_LENGTH(N'dbo.OperationConfigs', N'ModifiedAt') IS NULL        ALTER TABLE [dbo].[OperationConfigs] ADD [ModifiedAt] DATETIME2 NULL;
IF COL_LENGTH(N'dbo.OperationConfigs', N'ModifiedBy') IS NULL        ALTER TABLE [dbo].[OperationConfigs] ADD [ModifiedBy] NVARCHAR(200) NULL;
IF COL_LENGTH(N'dbo.OperationConfigs', N'ModifiedByWindows') IS NULL ALTER TABLE [dbo].[OperationConfigs] ADD [ModifiedByWindows] NVARCHAR(200) NULL;

-- B8 Bestand befuellen (eigener Batch: referenziert die Spalten aus B7)
UPDATE [dbo].[OperationConfigs] SET [CreatedAt] = GETDATE() WHERE [CreatedAt] IS NULL;
UPDATE [dbo].[OperationConfigs] SET [CreatedBy] = N'system' WHERE [CreatedBy] IS NULL;
UPDATE [dbo].[OperationConfigs] SET [CreatedByWindows] = N'system' WHERE [CreatedByWindows] IS NULL;

-- B9 Pflichtspalten NOT NULL (Guard is_nullable, wie fallstricke §9 Nachtrag; Spalten tragen keinen Index)
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.OperationConfigs') AND name = N'CreatedAt' AND is_nullable = 1)
    ALTER TABLE [dbo].[OperationConfigs] ALTER COLUMN [CreatedAt] DATETIME2 NOT NULL;
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.OperationConfigs') AND name = N'CreatedBy' AND is_nullable = 1)
    ALTER TABLE [dbo].[OperationConfigs] ALTER COLUMN [CreatedBy] NVARCHAR(200) NOT NULL;
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.OperationConfigs') AND name = N'CreatedByWindows' AND is_nullable = 1)
    ALTER TABLE [dbo].[OperationConfigs] ALTER COLUMN [CreatedByWindows] NVARCHAR(200) NOT NULL;
```

Festlegungen dazu: Bestand `CreatedAt` = **Migrationszeitpunkt** (`GETDATE()`, Server-Ortszeit — die App schreibt
`DateTime.Now`), `CreatedBy` = `CreatedByWindows` = `'system'` (Antwort 2; Muster Seed `Program.cs:197-198`),
`Modified*` bleiben `NULL`. Keine bleibenden Default-Constraints auf den Audit-Spalten — das Schema entspricht danach
genau dem EF-Modell. Typen/Längen wie die übrigen `AuditableEntity`-Tabellen (`SQL/94_AddBeschichtungsauftrag.sql:35-37`,
`ApplicationDbContext.cs:88-91`); der Dev-Lauf gleicht sie mit dem generierten Code aus Schritt 2 ab.

**`Down()`** (nur Migration): guarded Rückweg — Audit-Spalten droppen (`COL_LENGTH`-Guard), Index → `IX_OseonOperationConfigs_OperationName`,
Spalte → `IsOseonRelevant`, PK → `PK_OseonOperationConfigs`, Tabelle → `OseonOperationConfigs`. Bewusst **ohne**
Rückbau zum `UQ_`-Constraint (der Rückweg endet in der EF-Form).

**`SQL/95_RenameOperationConfigsAndAddAudit.sql`:** Kopf-Kommentar wie `SQL/94:6` („Objektnamen exakt wie die
EF-Migration …“), B1–B9 je in eigenem Batch (`GO`), danach **separater** Batch
`IF NOT EXISTS (… WHERE MigrationId = '<neue MigrationId>') INSERT … VALUES ('<neue MigrationId>', '10.0.2')` mit der
**exakten** ID aus dem Dateinamen von Schritt 2 (Muster `SQL/94:110-113`). Warnung, bewusst nicht nachgeahmt:
`SQL/34_AddOseonOperationConfig.sql:43-46` prüft per `LIKE` und schreibt `20260330120000_AddOseonOperationConfig`, EF und
FreshInstall führen `20260330142035_AddOseonOperationConfig` (`00_FreshInstall.sql:2293`) — siehe R10.

**`SQL/00_FreshInstall.sql` an zwei Stellen (+ Seed):**
- (a) Schema `:1220-1236`: Abschnittstitel `16b. OperationConfigs`; Guard `IF OBJECT_ID(N'[dbo].[OperationConfigs]', N'U')
  IS NULL AND OBJECT_ID(N'[dbo].[OseonOperationConfigs]', N'U') IS NULL` → `CREATE TABLE [dbo].[OperationConfigs]` mit
  `Id`, `OperationName`, `DisplayName`, `DueDateOffsetDays` (`DEFAULT 0` wie heute), `IsExternalSystemRelevant`
  (`DEFAULT 1` wie heute), den sechs Audit-Spalten (Typen wie B7/B9, ohne Default) und `CONSTRAINT [PK_OperationConfigs]`;
  **kein** `UQ_`-Constraint mehr, stattdessen im selben Abschnitt (eigener Batch, `sys.indexes`-Guard)
  `CREATE UNIQUE INDEX [IX_OperationConfigs_OperationName]`. Existiert nur die alte Tabelle → `PRINT`-Warnung „SQL/95
  ausführen“ statt Anlage (R6). Kopfzeile `:4` sagt ohnehin: bestehende Installationen über die Einzelskripte.
- Seed `:1491-1510`: `INSERT INTO [dbo].[OperationConfigs] (…, [IsExternalSystemRelevant], [CreatedAt], [CreatedBy],
  [CreatedByWindows])` mit `GETDATE(), N'system', N'system'` je Zeile; Kürzel/Offsets unverändert (Antwort 3).
- (b) History: nach `:2421-2422` dasselbe `IF NOT EXISTS … INSERT` mit der neuen `MigrationId` (zweimal im Text:
  Prüfung + Insert, wie beim Drift-Test von Teil 3).

**Drift-Wächter-Test** `IdealAkeWms.Tests/Repositories/OperationConfigsMigrationSqlTests.cs` (Muster
`BeschichtungsauftragMigrationSqlTests.cs:31-53` im Bündel, eingeführt mit
[[2026-10-06-beschichtungsauftrag-ausbau-teil-3-spec]]): liest Migration `.cs`, `SQL/95`, `00_FreshInstall.sql` und
`ApplicationDbContextModelSnapshot.cs` und prüft
- alle drei SQL-tragenden Dateien enthalten `OperationConfigs`, `PK_OperationConfigs`, `IX_OperationConfigs_OperationName`,
  `IsExternalSystemRelevant` und die sechs Audit-Spaltennamen;
- der Snapshot enthält `b.ToTable("OperationConfigs", "dbo")`, `"IsExternalSystemRelevant"` und keinen Eintrag
  `"IdealAkeWms.Models.OseonOperationConfig"` mehr;
- die Migration enthält **keine** strukturierte Operation: kein `RenameTable(`, `RenameColumn(`, `RenameIndex(`,
  `DropPrimaryKey(`, `AddPrimaryKey(`, `AddColumn(`, `AlterColumn(`, `DropTable(`, `CreateTable(`, `DropIndex(`,
  `CreateIndex(`;
- `00_FreshInstall.sql` enthält weder `CREATE TABLE [dbo].[OseonOperationConfigs]` noch
  `INSERT INTO [dbo].[OseonOperationConfigs]` noch `UQ_OperationConfigs`;
- `MigrationId` 1× als Literal in `SQL/95`, 2× in FreshInstall.
Was der Test **nicht** beweist: dass die Batches auf SQL Server laufen und idempotent sind → Manual-UAT S9.

### Raw-SQL- und Textstellen mit altem Namen (Inventur Bündel v1.52.0)

Suche `OseonOperationConfigs` / `IsOseonRelevant` über den Bündel-Worktree:

| Fundort | Art | Behandlung |
|---|---|---|
| Anwendungscode `IdealAkeWms/**/*.cs`, `*.cshtml`, `wwwroot/js` | **keine** Raw-SQL-Stelle (kein `FromSql*`/`ExecuteSql*`/`SqlQuery` mit dem Namen); alle Zugriffe sind EF-LINQ über das DbSet (`OseonOperationConfigRepository.cs:30-93`, `OseonProductionOrderRepository.cs:213`, `Program.cs:471/490`); einziges Namens-Literal ist das Mapping `ApplicationDbContext.cs:884` | Umbenennung per Compiler; Mapping → `"OperationConfigs"` |
| `IDEALAKEWMSService/` | 0 Treffer | — |
| `SQL/AgentJobs/` | 0 Treffer | — |
| `IdealAkeWms.Tests/` | nur EF/InMemory (`OseonOperationConfigRepositoryTests.cs`, `OseonProductionOrderRepositoryReportingTests.cs:47-212`, `OseonReportingControllerTests.cs:68-119`, `TrackingControllerTests.cs`), kein Raw-SQL | Umbenennung |
| `SQL/34_AddOseonOperationConfig.sql:2-46` | historisches Skript | **unverändert** (ADR 0004) |
| `SQL/44_MoveTablesToDboSchema.sql:24` | historisches Skript | **unverändert** |
| `SQL/00_FreshInstall.sql:1221-1234, 1492-1494` | Konsolidat | auf neue Namen (oben) |
| `IdealAkeWms/Migrations/20260330142035_*`, `20260420123329_SetDefaultSchemaDbo.cs:101-104, 285-288`, alle `*.Designer.cs` | historische Migrationen | **unverändert** |
| `docs/TESTSZENARIEN.md:3741` (TS-16.5) | Text | → „Arbeitsgang-Stammdaten“ (siehe Test-Szenarien) |
| `README.md:105` | Skriptliste | bleibt (nennt Skript 34); `SQL/95` in die Liste ergänzen |
| `docs/superpowers/**` (u. a. `specs/2026-04-30-oseon-reporting-ag-uebersicht-design.md:124` mit SQL-Fragment), `docs/*-backup-2026-07.md`, `secondbrain_alt/**` | historische Doku/Junction | **unverändert** (`secondbrain_alt` nie anfassen) |
| Brain: [[2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-2-spec]] P4 (`:753-758`, `FROM dbo.OseonOperationConfigs`, `IsOseonRelevant`) sowie Fließtext `:188, :501, :746, :863, :868` | Prüfabfrage einer anderen Spec | **nur gemeldet** — Umstellung ist Sache jener Spec; nach dem Deploy dieser Spec liefert die alte Abfrage „Ungültiger Objektname“ |

- **`SQL/AgentJobs/`:** nicht betroffen (0 Treffer für `OseonOperationConfig`/`IsOseonRelevant`; Suchbeleg auch 0 für `WorkOperation`).
- **Service:** liest die Tabelle nicht (0 Treffer in `IDEALAKEWMSService/`, auch nicht über Web-Repositories wie
  `OseonProductionOrderRepository`/`OseonGroupViewModelBuilder`) und ruft kein `Migrate()` auf → kein Schema-Vertrag.

## Audit-Feld-Auswirkungen

Heute keine (`OseonOperationConfig` erbt nicht von `AuditableEntity`, `Models/OseonOperationConfig.cs:5`; Update
`SettingsController.cs:157-161` setzt nichts). ADR [[0003-auditableentity-als-entity-basis]] verlangt sie für
fachliche Entitäten — und die Offsets bestimmen künftig „überfällig“ an Terminal und FA-Struktur, „wer hat den
Offset geändert“ wird also fachlich relevant. **Entschieden: ja** (Antwort 2, in derselben Migration wie die
Umbenennung). Damit:
- `OperationConfig` erbt `AuditableEntity` (`Models/AuditableEntity.cs:3-12`);
- `Add`: `CreatedAt = DateTime.Now`, `CreatedBy = GetDisplayName()`, `CreatedByWindows = GetWindowsUserName()`
  (Muster `SettingsController.cs:64-66`);
- `Update`: `ModifiedAt`/`ModifiedBy`/`ModifiedByWindows` analog (Muster `BdeMasterDataController.cs:190-192`);
- Seed `Program.cs:490-496` (v1.52.0): `CreatedAt = DateTime.Now`, `CreatedBy = CreatedByWindows = "system"` (Muster
  `Program.cs:197-198`); ohne diese Zuweisung schriebe EF `string.Empty` (`AuditableEntity.cs:7-8`);
- Bestandszeilen: `CreatedAt` = Migrationszeitpunkt, `CreatedBy`/`CreatedByWindows` = `'system'`, `Modified*` = `NULL`
  (Batches B7–B9 oben); FreshInstall-Seed analog mit `GETDATE()`/`N'system'`;
- Delete bleibt physisch (wie heute, `OseonOperationConfigRepository.cs:81-89`).

## Rollen / Zugriffsfilter

- `OperationConfigsController`: `[RequireAdminAccess]` (Klasse), kein Feature-Gate, insbesondere **nicht**
  `[RequireBdeActive]` (Entscheidung 1).
- `BdeMasterDataController`: unverändert (`[RequireBdeActive]` + `[RequireBdeShiftleadAccess]`, Terminals zusätzlich
  `[RequireBdeAdminAccess]`).
- `SettingsController`: unverändert `[RequireAdminAccess]`; Redirect-Action ebenfalls nur Admin.
- Keine neue Rolle/kein neuer Filter → Drei-Stellen-Regel nicht ausgelöst; nur [[controller]] Z.53 ergänzen.

## Listen-View-Pattern (ADR 0005)

Neue Tabelle „Arbeitsgänge“: Server-Mode-Spaltenfilter, `data-col-key` auf allen `<th>`, Pagination über
`PageSize.Resolve` + `PaginationState` + `_Pagination` (kein Take/Cap), Filter über die Spaltenfilter (die BDE-
Stammdaten-Reiter haben keine eigene Filterkarte — Konsistenz mit den Nachbarreitern geht vor; die Karte
„Nicht konfigurierte Kürzel“ ist keine Filterkarte). **Keine** Spaltenauswahl/Zahnrad → kein
`ColumnDefinitions.GetByViewKey`-Eintrag nötig (§3 greift nur mit `#column-config`); kommt später ein Zahnrad dazu,
gilt §3 (Registrierung) und §19 (Host direkt vor `.table-responsive`). Keine Datumsspalten.

## Akzeptanzkriterien

1. Admin, `BdeAktiv = false`: `/OperationConfigs` öffnet die Seite „BDE-Stammdaten“ mit genau einem Reiter
   „Arbeitsgänge“; `/BdeMasterData` leitet weiter wie heute mit „BDE ist nicht aktiviert.“ auf Home.
2. Admin, `BdeAktiv = true`: Reiterleiste zeigt Operatoren, Aktivitäten, Terminals, Arbeitsgänge; Wechsel in beide
   Richtungen funktioniert, der aktive Reiter ist markiert.
3. `bde_shiftlead` (ohne admin), `BdeAktiv = true`: BDE-Stammdaten ohne Reiter „Arbeitsgänge“; Direktaufruf
   `/OperationConfigs` (GET und POST) → AccessDenied. Dasselbe für `masterdata`-Benutzer.
4. Navigation (Antwort 4): Admin findet „Arbeitsgänge“ im Stammdaten-Dropdown bei `BdeAktiv = false` und
   `true`; Nicht-Admins sehen den Eintrag nicht.
5. `GET /Settings/OperationConfig` antwortet mit **302** auf `/OperationConfigs` (Antwort 5); die POST-Routen
   `AddOperationConfig`/`UpdateOperationConfig`/`DeleteOperationConfig` existieren nicht mehr; der Knopf in
   `/Settings` und der Banner-Link im OSEON-Reporting führen auf die neue Seite.
6. Anlegen: leeres Kürzel → `WarningMessage`; Kürzel wird getrimmt gespeichert; Kürzel, das getrimmt und
   case-insensitiv existiert (`ka` bei vorhandenem `KA`), → `WarningMessage`, kein Insert. Ändern und Löschen wie
   heute mit `SuccessMessage`.
7. Begriffe: In `IdealAkeWms/Views/` kommt weder „OSEON-relevant“ noch „OSEON-Relevanz“ mehr vor (grep); Spalte
   und Schalter heißen „Fremdsystem-relevant“, das Offset-Feld „Offset (Arbeitstage) zum Bezugstermin“; die
   Legende nennt Stanztermin (OSEON), FE-Termin (IDEAL), Fertigungstermin/`ProductionDate` (AKE-BDE) und sagt zu
   „Fremdsystem-relevant“, dass es nur in OSEON-Teileverfolgung und OSEON-Reporting wirkt (Antwort 6).
7a. Alte Namen: In `IdealAkeWms/` (ohne `Migrations/`) und `IdealAkeWms.Tests/` kommen `OseonOperationConfig` und
   `IsOseonRelevant` nur noch als OSEON-**ViewModel**-Property `OseonOperationViewModel.IsOseonRelevant`
   (`OseonTrackingViewModel.cs:70`, `_OseonGroupDetails.cshtml:69,80`, `OseonGroupViewModelBuilder.cs:69,78`) und im
   Drift-Test als verbotene/alte Namen vor (grep); `SQL/34`, `SQL/44` und alte Migrationen sind unverändert (`git diff`).
8. „Nicht konfigurierte Kürzel“ zeigt (a) OSEON-AG-Namen ohne Eintrag mit Quelle „OSEON“, (b) `WorkOperation`-
   Kürzel ohne Eintrag mit Quelle „WMS-Arbeitsgang“, (c) kein `"01"` (Antwort 7); Abgleich getrimmt und
   case-insensitiv (Unit-Test: `WorkOperation` `ka` bei Eintrag `KA` → nicht gelistet). Nach Anlegen eines Eintrags
   verschwindet das Kürzel aus der Liste.
9. `GetDueDateOffsetsAsync()` (Unit-Tests InMemory): Eintrag `KA`/+3 → `["ka"] == 3` und `[" KA "]` nach
   Trim `== 3`; Eintrag mit 0 → Schlüssel vorhanden, Wert 0; Kürzel ohne Eintrag → `ContainsKey == false`; nicht
   relevante Einträge sind enthalten.
10. Decorator: zweiter Aufruf von `GetDueDateOffsetsAsync()` ohne DB-Zugriff; nach `Add`/`Update`/`Delete` liefert
    der nächste Aufruf den neuen Stand (Unit-Test am Decorator).
11. OSEON-Regression Teileverfolgung: `GetAllAsDictionaryAsync` bleibt case-sensitiv; AG ohne Eintrag gilt als
    relevant mit Soll = OSEON-Endtermin; Sub-Auftrag nur mit nicht relevanten AGs = „Fertig“ (TS 30.8). Bestehende
    Tests grün (nur Bezeichner geändert).
12. OSEON-Regression Reporting: Zeilen ohne Eintrag werden gezählt und übersprungen (Banner TS-16.5), Namensabgleich
    bleibt case-insensitiv, AG-Auswahlliste nur fremdsystem-relevante Einträge. Bestehende Tests grün.
13. `git diff` gegen den Startpunkt zeigt **keine** Änderung an `Services/OseonDueDateCalculator.cs` und
    `IDEALAKEWMSService/Services/WorkOperationStructureDetectionService.cs`.
14. Audit (Antwort 2): Anlegen setzt `CreatedAt/By/ByWindows`, Ändern setzt `ModifiedAt/By/ByWindows` aus
    `ICurrentUserService` (Unit-Test am Controller); der Seed setzt `CreatedBy = CreatedByWindows = "system"`
    (Unit-/Code-Review); Bestandszeilen haben nach der Migration `CreatedBy = CreatedByWindows = 'system'`,
    `CreatedAt` ≈ Migrationszeitpunkt, `Modified*` = `NULL` (Manual-UAT, P1 „nach dem Deploy“).
15. Migration: genau **eine** neue Migration `<Timestamp>_RenameOperationConfigsAndAddAudit`; `dotnet ef migrations
    has-pending-model-changes` meldet nichts; `dotnet ef migrations script 20261012073551_AddBeschichtungsauftrag <neu>`
    enthält kein `DROP TABLE`, `CREATE TABLE`, `DROP INDEX` und kein `sp_rename` ohne vorangehendes `IF`.
16. Listen-View: Spaltenfilter auf Kürzel/Anzeigename/Offset/Relevanz filtern serverseitig vor der Pagination;
    `PageSize` „Alle“ und Seitenwechsel funktionieren; jede `<th>` trägt `data-col-key`.
17. Doppelte Kürzel nach Normalisierung (Testdaten per InMemory) → Warnhinweis auf der Seite; Offset-Lookup nimmt
    die Zeile mit kleinster `Id`.
18. Hilfeseite und `docs/TESTSZENARIEN.md` beschreiben den neuen Ort und die neuen Begriffe; `dotnet build` und
    `dotnet test` grün.
19. Drift-Wächter `OperationConfigsMigrationSqlTests` grün: Migration, `SQL/95`, FreshInstall und Snapshot tragen
    dieselben neuen Namen (`OperationConfigs`, `PK_OperationConfigs`, `IX_OperationConfigs_OperationName`,
    `IsExternalSystemRelevant`, sechs Audit-Spalten); `MigrationId` 1× in `SQL/95`, 2× in FreshInstall; die Migration
    enthält keine strukturierte Schema-Operation; FreshInstall legt weder `OseonOperationConfigs` noch `UQ_OperationConfigs` an.
20. Schema-Gleichheit (Manual-UAT S9): auf (a) einer EF-migrierten DB (IX-Form), (b) einer Skript-/alten
    FreshInstall-DB (UQ-Form) und (c) einer neuen FreshInstall-DB liefert die Abfrage P6 **dieselbe** Ergebnismenge:
    Tabelle `OperationConfigs`, Spalte `IsExternalSystemRelevant`, `PK_OperationConfigs`, **ein** Unique-Index
    `IX_OperationConfigs_OperationName` (kein `UQ_`-Constraint), sechs Audit-Spalten mit der Nullbarkeit des EF-Modells;
    der App-Start danach läuft ohne Fehler, Bestandszeilen sind erhalten (Anzahl vor = nachher).
21. Ohne strukturierte Operationen: die Migration funktioniert auf beiden Alt-Formen (a) und (b) — insbesondere kein
    SqlError 3701/15248 („Objekt nicht gefunden“) beim App-Start-`Migrate()` (`Program.cs:186`).
22. Idempotenz und Anomalie: `SQL/95` läuft zweimal hintereinander fehlerfrei (zweiter Lauf ändert nichts); läuft die
    App danach an, wendet EF die Migration nicht erneut an (History-Eintrag vorhanden). Existieren `OseonOperationConfigs`
    **und** `OperationConfigs` gleichzeitig, bricht `SQL/95` bzw. `Migrate()` mit der Meldung aus B1 ab, ohne etwas
    umzubenennen.

## Test-Szenarien

Neues Kapitel **TS-<nächste freie; Stand v1.52.0: TS-86> — Arbeitsgang-Stammdaten (BDE-Stammdaten › Arbeitsgänge)**:

- **Vorbedingungen:** Testsystem mit Admin-, `bde_shiftlead`- und `masterdata`-Benutzer; mindestens ein
  OSEON-Auftrag mit einem AG-Namen ohne Eintrag (AKE) bzw. eine `WorkOperation` mit Kürzel ohne Eintrag (IDEAL);
  `BdeAktiv` umschaltbar.
- **S1 Erreichbarkeit BDE aus:** `BdeAktiv = false`, Admin → Stammdaten › Arbeitsgänge → Seite mit nur diesem
  Reiter. `/BdeMasterData` → Home + Warnung.
- **S2 Erreichbarkeit BDE an:** Admin sieht vier Reiter; Schichtleiter drei, `/OperationConfigs` → AccessDenied.
- **S3 Pflege:** Kürzel `KA` mit Offset +3, Anzeigename „Kanten“, Fremdsystem-relevant aus → anlegen, ändern, löschen;
  Meldungen grün. Negativ: leer, `ka` bei vorhandenem `KA` → gelbe Warnung.
- **S4 Nicht konfiguriert:** IDEAL-Kürzel ohne Eintrag erscheint mit Quelle „WMS-Arbeitsgang“, OSEON-Name mit
  „OSEON“; `01` erscheint nicht (Antwort 7); nach Anlegen verschwindet das Kürzel.
- **S5 Alte URL:** `/Settings/OperationConfig` → neue Seite; Knopf in `/Settings` und Banner-Link im OSEON-Reporting
  ebenso.
- **S6 Begriffe:** Legende mit drei Bezugsterminen; Tracking-Toggle „Fremdsystem-Relevanz“.
- **S7 OSEON unverändert (AKE):** TS-7.9, TS-16.1–16.5, 30.8 mit denselben Daten wie vor dem Deploy → gleiche
  Ampeln, gleiche Soll-Termine, gleiche Reporting-Zeilen.
- **S8 Audit:** nach S3 `SELECT CreatedBy, CreatedByWindows, ModifiedBy, ModifiedAt FROM dbo.OperationConfigs
  WHERE OperationName = 'KA'` zeigt den Benutzer; Bestandszeilen (z. B. `ST`) zeigen `CreatedBy = 'system'`,
  `ModifiedAt = NULL`.
- **S9 Migration / Umbenennung (Testsystem, je DB-Form, vor dem Produktiv-Deploy):**
  1. Drei DBs bereitstellen: (a) EF-migrierte Kopie (P1 zeigt `IX_OseonOperationConfigs_OperationName`), (b)
     Skript-/alte FreshInstall-Kopie (P1 zeigt `UQ_OseonOperationConfigs_OperationName`), (c) leere DB per neuem
     `00_FreshInstall.sql`. Für (a)/(b) vorher Zeilenanzahl notieren (P1 „vor dem Deploy“).
  2. (a)/(b): einmal über `SQL/95` (danach zweiter `SQL/95`-Lauf ohne Fehler, ohne Änderung), einmal ohne Skript nur per
     App-Start (`Migrate()`); (c): nur App-Start.
  3. Erwartet je DB: App startet, `/OperationConfigs` zeigt die Einträge, P6 liefert auf allen drei DBs dasselbe
     Ergebnis (AK 20), Zeilenanzahl unverändert, Bestandszeilen `CreatedBy = 'system'`.
  4. Negativ: auf einer Wegwerf-Kopie zusätzlich eine leere `dbo.OperationConfigs` neben `OseonOperationConfigs`
     anlegen → `SQL/95` bricht mit der B1-Meldung ab, nichts ist umbenannt (AK 22).
- **Negativfälle:** Nicht-Admin per Direkt-URL (GET/POST) → AccessDenied; Spaltenfilter ohne Treffer → leere
  Tabelle mit Hinweis, kein Fehler; keine `OseonWorkOperations` und keine `WorkOperations` → Liste leer mit
  Text „Alle bekannten Kürzel sind konfiguriert.“

**Bestehende Szenarien nachziehen:** TS-1.7 (Admin sieht trotz BDE aus „Arbeitsgänge“ unter Stammdaten — der
BDE-User-Teil bleibt), TS-7.9 (Label „Fremdsystem-Relevanz“), TS-7.10 (Pfad BDE-Stammdaten › Arbeitsgänge, Rolle
**admin**, Bezug „Bezugstermin“), TS-16.5 (Text „OseonOperationConfig“ → „Arbeitsgang-Stammdaten“, Link-Ziel), 36.5
(vierter Reiter, nur Admin). Danach [[testszenarien-index]] nachziehen.

## Prüfschritte (Mensch, vor Abnahme — Fakten, die nur die DBs kennen)

Auf **AKE-** und **IDEAL-WMS-DB** ausführen; Ergebnisse ins Deploy-Protokoll.

> [!warning] Zwei Fassungen wegen der Umbenennung
> **Vor** dem Deploy (bzw. vor `SQL/95`) heißen Tabelle und Spalte noch `dbo.OseonOperationConfigs` /
> `IsOseonRelevant` → Block „vor dem Deploy“. **Danach** gelten `dbo.OperationConfigs` / `IsExternalSystemRelevant`
> → Block „nach dem Deploy“; die alte Fassung liefert dann „Ungültiger Objektname“. P4 und P5 betreffen die Tabelle
> nicht und gelten unverändert. P1 (inkl. MigrationId-Prüfung, R10) **muss** vor dem Deploy laufen.

**Vor dem Deploy (alter Name):**

```sql
-- P1 Bestand + Form der Eindeutigkeit + MigrationId (zeigt, welche Batches der Migration greifen; R10)
SELECT COUNT(*) AS Anzahl FROM dbo.OseonOperationConfigs;
SELECT Id, OperationName, DisplayName, DueDateOffsetDays, IsOseonRelevant
FROM dbo.OseonOperationConfigs ORDER BY OperationName;
SELECT i.name, i.is_unique, i.is_unique_constraint
FROM sys.indexes i WHERE i.object_id = OBJECT_ID(N'dbo.OseonOperationConfigs') AND i.name IS NOT NULL;
SELECT MigrationId FROM dbo.__EFMigrationsHistory WHERE MigrationId LIKE '%AddOseonOperationConfig';

-- P2 Collation + Dubletten nach Normalisierung
SELECT DATABASEPROPERTYEX(DB_NAME(), 'Collation') AS DbCollation, c.collation_name AS SpaltenCollation
FROM sys.columns c WHERE c.object_id = OBJECT_ID(N'dbo.OseonOperationConfigs') AND c.name = N'OperationName';
SELECT UPPER(LTRIM(RTRIM(OperationName))) AS Norm, COUNT(*) AS Anzahl
FROM dbo.OseonOperationConfigs GROUP BY UPPER(LTRIM(RTRIM(OperationName))) HAVING COUNT(*) > 1;

-- P3 Kollision OSEON-Seed <-> WMS-Kuerzel (aus Teil-2-P4)
SELECT c.OperationName, c.DueDateOffsetDays, wo.Anzahl
FROM dbo.OseonOperationConfigs c
JOIN (SELECT OperationNumber, COUNT(*) AS Anzahl FROM dbo.WorkOperations GROUP BY OperationNumber) wo
  ON UPPER(LTRIM(RTRIM(wo.OperationNumber))) = UPPER(LTRIM(RTRIM(c.OperationName)));
SELECT c.OperationName, wp.Name AS Werkbank
FROM dbo.OseonOperationConfigs c
JOIN dbo.ProductionWorkplaces wp ON UPPER(LTRIM(RTRIM(wp.ArbeitsschrittCode))) = UPPER(LTRIM(RTRIM(c.OperationName)));

-- P4 Welche WMS-Kuerzel gibt es (Quelle der Liste "nicht konfiguriert"), inkl. '01'
SELECT OperationNumber, COUNT(*) AS Anzahl, MIN(CreatedBy) AS AngelegtVon
FROM dbo.WorkOperations GROUP BY OperationNumber ORDER BY Anzahl DESC;

-- P5 BdeAktiv je Site (Entscheidung 1 sagt: nirgends aktiv - DB-Wert vom Spec-Lauf nicht geprueft)
SELECT [Key], [Value] FROM dbo.AppSettings WHERE [Key] = N'BdeAktiv';
```

Auswertung P1: steht `20260330142035_AddOseonOperationConfig` **nicht** in der History (nur `20260330120000_…`) →
Deploy auf dieser DB anhalten und klären (R10). Index-Zeile `IX_…` = EF-Form (B4 greift), `UQ_…` mit
`is_unique_constraint = 1` = Skript-Form (B5+B6 greifen).

**Nach dem Deploy (neuer Name):**

```sql
-- P1 Bestand (Anzahl muss der Zahl von vorher entsprechen) + Audit-Bestand
SELECT COUNT(*) AS Anzahl FROM dbo.OperationConfigs;
SELECT Id, OperationName, DisplayName, DueDateOffsetDays, IsExternalSystemRelevant,
       CreatedAt, CreatedBy, CreatedByWindows, ModifiedAt, ModifiedBy, ModifiedByWindows
FROM dbo.OperationConfigs ORDER BY OperationName;
SELECT MigrationId FROM dbo.__EFMigrationsHistory WHERE MigrationId LIKE '%RenameOperationConfigsAndAddAudit';

-- P2 Collation + Dubletten nach Normalisierung
SELECT DATABASEPROPERTYEX(DB_NAME(), 'Collation') AS DbCollation, c.collation_name AS SpaltenCollation
FROM sys.columns c WHERE c.object_id = OBJECT_ID(N'dbo.OperationConfigs') AND c.name = N'OperationName';
SELECT UPPER(LTRIM(RTRIM(OperationName))) AS Norm, COUNT(*) AS Anzahl
FROM dbo.OperationConfigs GROUP BY UPPER(LTRIM(RTRIM(OperationName))) HAVING COUNT(*) > 1;

-- P3 Kollision OSEON-Seed <-> WMS-Kuerzel
SELECT c.OperationName, c.DueDateOffsetDays, wo.Anzahl
FROM dbo.OperationConfigs c
JOIN (SELECT OperationNumber, COUNT(*) AS Anzahl FROM dbo.WorkOperations GROUP BY OperationNumber) wo
  ON UPPER(LTRIM(RTRIM(wo.OperationNumber))) = UPPER(LTRIM(RTRIM(c.OperationName)));
SELECT c.OperationName, wp.Name AS Werkbank
FROM dbo.OperationConfigs c
JOIN dbo.ProductionWorkplaces wp ON UPPER(LTRIM(RTRIM(wp.ArbeitsschrittCode))) = UPPER(LTRIM(RTRIM(c.OperationName)));

-- P6 Schema-Nachweis (AK 20): muss auf jeder DB-Form dieselbe Ergebnismenge liefern;
--    alte Namen duerfen nicht mehr auftauchen
SELECT OBJECT_ID(N'dbo.OseonOperationConfigs', N'U') AS AlteTabelle_mussNULLsein,
       OBJECT_ID(N'dbo.OperationConfigs', N'U')      AS NeueTabelle;
SELECT c.name, TYPE_NAME(c.user_type_id) AS Typ, c.max_length, c.is_nullable
FROM sys.columns c WHERE c.object_id = OBJECT_ID(N'dbo.OperationConfigs') ORDER BY c.name;
SELECT i.name, i.type_desc, i.is_unique, i.is_unique_constraint, i.is_primary_key
FROM sys.indexes i WHERE i.object_id = OBJECT_ID(N'dbo.OperationConfigs') AND i.name IS NOT NULL ORDER BY i.name;
-- erwartet: IX_OperationConfigs_OperationName (is_unique=1, is_unique_constraint=0) + PK_OperationConfigs; kein UQ_
```

Unverifiziert bis P1–P6 gelaufen sind: dass die IDEAL-DB die OSEON-Seed-Zeilen enthält, dass die Collation
case-insensitiv ist, welche Kürzel kollidieren, ob `01` auf einer DB vorkommt, welche Eindeutigkeits-Form die
Produktiv-DBs tragen und ob dort die richtige `MigrationId` steht.

## Etappen (nur bei epic: true)

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | entfällt (kein Epic) | – | – |

## Deploy

**Final (QA 2026-10-12, gegen den echten Diff `87db96c8..b5123529`; QA-Nachlauf gegen `b5123529..d56a340e` ändert
nichts an web/service/migration — nur Controller, View, `SQL/95`, `SQL/00_FreshInstall.sql`, Test, Doku):** geändert sind `IdealAkeWms/` (Modell, DbContext,
Migration `20261012140548_RenameOperationConfigsAndAddAudit` + Designer + Snapshot, Controller, Views, Layout, Repository,
`Program.cs`), `SQL/95` + `SQL/00_FreshInstall.sql`, Tests, Doku. In `IDEALAKEWMSService/` nur `AppVersion.cs` (1.53.0 → 1.54.0).
→ `deploy.web: true`, `deploy.migration: true` (neue Datei unter `Migrations/` + `SQL/95`), `deploy.service: false` (nur
Versions-Bump; geht bei Bündel-Komplett-Rollout mit). Die Punkte unten gelten unverändert; ergänzend:

- **Reihenfolge je Site (verbindlich):** P1–P3 „vor dem Deploy“ (P1-Auswertung R10) → **App-Pool stoppen** → **DB-Backup**
  → `SQL/95` im SSMS **vollständig** (nicht nur eine Markierung) ausführen und die Ausgabe lesen: **erscheinen die
  B1-Meldung „… existieren beide …“ und „ABBRUCH: …“, hier anhalten** — das Skript hat dann per `SET NOEXEC ON` nichts
  weiter ausgeführt und keine History-Zeile geschrieben (I-1 behoben in `d56a340e`); Ursache klären, keine neuen
  Binaries ausrollen → sonst neue Web-Binaries kopieren → App-Pool starten
  (Recycle; `Migrate()` findet die History-Zeile) → P1–P3 + P6 „nach dem Deploy“, App-Start-Log ohne Migrationsfehler.
- **Publish (nur Web), aus dem Bündel-Worktree:**
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  (Service nur bei Komplett-Rollout des Bündels: `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`.)
- Nach dem Merge nur dann erneut aus `main` publizieren, wenn der Merge getestete Dateien mit parallelen `main`-Änderungen
  kombiniert hat.

- **Web-App:** ja (Modell, Migration, Controller, Views, Layout, Repository).
- **Service:** nicht zwingend — liest die Tabelle nicht (0 Treffer für `OseonOperationConfig`/`IsOseonRelevant` in
  `IDEALAKEWMSService/`, auch nicht indirekt über die Web-Repositories `OseonProductionOrderRepository`/
  `OseonGroupViewModelBuilder`) und ruft kein `Migrate()` auf. Ein alter Service-Binary trägt zwar das alte Modell
  (Service referenziert das Web-Projekt, [[fallstricke]] §5), fragt die Tabelle aber nie ab; EF prüft das Schema nicht
  beim Start. Wird das Bündel ohnehin komplett ausgerollt, geht der Service mit derselben Version mit.
- **Migration:** ja — `SQL/95_RenameOperationConfigsAndAddAudit.sql` = EF-Migration `<Timestamp>_RenameOperationConfigsAndAddAudit`.
  Nicht daten-destruktiv, aber Schema-Umbenennung → **DB-Backup vor Deploy**. Alte Web-Binaries brechen nach dem
  Skript (sie fragen `OseonOperationConfigs` ab), neue Web-Binaries vor dem Skript migrieren beim Start selbst
  (`Program.cs:186`) — beides ist zu vermeiden bzw. bewusst zu wählen.
- **Reihenfolge je Site (AKE-DB und IDEAL-DB getrennt):**
  1. Prüfschritte „vor dem Deploy“ P1–P3 laufen lassen und protokollieren; P1-Auswertung (R10) — bei fehlender
     `20260330142035_AddOseonOperationConfig` anhalten.
  2. IIS-App-Pool der Web-App stoppen (keine alte Instanz darf nach dem Skript weiterlaufen); sicherstellen, dass
     kein lokaler/anderer Web-Prozess gegen diese DB läuft ([[fallstricke]] §8 „Lokaler `dotnet run` … ist ein Deploy“).
  3. DB-Backup.
  4. `SQL/95_RenameOperationConfigsAndAddAudit.sql` ausführen (bei Bündel-Deploy: in Nummernreihenfolge nach den
     übrigen noch offenen Bündel-Skripten bis `SQL/94`).
  5. Neue Web-Binaries ausrollen, App-Pool starten (Recycle). `Migrate()` findet die Migration in der History und tut
     nichts.
  6. Prüfschritte „nach dem Deploy“ P1–P3 + P6; App-Start-Log ohne Migrationsfehler.
  Alternative ohne Skript: Schritt 4 entfällt, der App-Start migriert mit denselben Batches — dann Schritte 2/3 genauso.
- **Daten je Site:**
  - **AKE:** keine fachliche Datenänderung; bestehende OSEON-Einträge bleiben (nur umbenannte Tabelle/Spalte +
    Audit-Bestand `system`), nur Ort/Begriffe ändern sich. Nach dem Deploy S7 (OSEON-Regression).
  - **IDEAL:** P1–P4 laufen lassen; OSEON-Seed-Zeilen nach Antwort 3 behandeln (kollidierende auf IDEAL-Werte setzen,
    übrige löschen oder stehen lassen — per UI, kein Skript); IDEAL-Kürzel aus „Nicht konfigurierte Kürzel“ mit
    Offsets anlegen, **bevor** FA-Struktur Teil 2/3 abgenommen werden.
- **`SQL/AgentJobs/`:** nichts mitzuziehen (0 Treffer).
- **Publish-Befehle** (im Bündel-Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`

## Brain-Pflichten (Dev-Lauf, in den Hauptcheckout)

- [[glossar]] — neue Einträge: **Arbeitsgang-Stammdaten** (Tabelle `OperationConfigs`, bis v1.52.0
  `OseonOperationConfigs`; Klasse `OperationConfig`; Kürzel → Offset + Relevanz; OSEON-AG-Name und WMS-`OperationNumber`
  teilen sich den Schlüssel), **Fremdsystem-relevant** (UI-Begriff für `IsExternalSystemRelevant`, früher
  `IsOseonRelevant`; wirkt nur für OSEON), **Bezugstermin** (OSEON Stanztermin, IDEAL `FE_Termin`, AKE-BDE
  `ProductionDate`), **Offset (Arbeitstage)**; Eintrag „Arbeitsgang“ (Z.22) um den Verweis ergänzen.
- [[datenmodell]] Z.92 + [[integrationen]] Z.93 + [[services]] Z.21/33: neue Tabellen-/Spalten-/Klassennamen,
  Audit-Felder, Hinweis „umbenannt mit Migration `…_RenameOperationConfigsAndAddAudit` / `SQL/95`“.
- [[controller]] Z.53: `OperationConfigsController` bei `[RequireAdminAccess]`.
- [[fallstricke]]: „Arbeitsgang-Offset: zwei Lookups mit unterschiedlicher Groß/Klein-Regel (OSEON-Tracking
  case-sensitiv, Lookup für Teil 2/3 case-insensitiv) — bewusst“; „Reiter in einer gegateten Controller-Familie:
  eigener Controller statt Filter-Ausnahme“; im Abschnitt §9 ein Nachtrag: „Tabellen-Umbenennung: EF generiert
  `RenameIndex`/`DropPrimaryKey` bzw. bei Klassen- **und** Tabellen-Rename `DropTable`/`CreateTable` — Up()/Down()
  komplett durch guarded `sp_rename` ersetzen; UQ-/IX-Doppelform beim Umbenennen auf die EF-Form konvergieren“.
- [[feature-map]] Z.93 (AG-Konfiguration → Arbeitsgang-Stammdaten), changelog, Hilfe, Anwender-Changelog.

## Abweichungen von der Backlog-Notiz (Befund dieses Laufs)

- Seed steht bei `Program.cs:469-498` (Notiz: 463-481, Stand v1.48.0).
- `IsOseonRelevant` steht in 16 Dateien (ohne Migrationen/Doku), nicht 10; der Notiz-Wert zählte offenbar nur
  Produktionscode.
- Der Namensvergleich ist **nur** im Tracking-Pfad case-sensitiv (`OseonOperationConfigRepository.cs:46-50`); das
  Reporting vergleicht case-insensitiv (`OseonProductionOrderRepository.cs:214`).
- Abgleich IDEAL: der Struktur-Sync liest die Stammdaten nicht und soll es laut Entscheidung 3 auch nicht — der
  „Abgleich“ ist die Liste „nicht konfiguriert“ im Web.
- TS-7.10 nennt `masterdata` als zugelassen, der Code nur Admin (`SettingsController.cs:10`).
- `SQL/34` schreibt eine andere `MigrationId` als EF/FreshInstall (`:46` vs. `00_FreshInstall.sql:2293`) —
  Altlast, hier nur als Warnung für das neue Skript.

## Offene Rueckfragen

Rückfragen 1–7 sind beantwortet (Freigabe-Antworten, Gerald 2026-10-12) und in Rumpf, `affected_code`, AK,
Testszenarien und Deploy eingearbeitet; ihr ursprünglicher Text bleibt als Nachweis stehen. Offen ist nur Rückfrage 8.

1. *Eingearbeitet (Antwort 1, 2026-10-12): U, keine Kompatibilitäts-View.* **Datenmodell — U, H oder L?** (FA 1) Empfehlung **H**: Code sprechend, Schema unverändert; U bringt das bekannte
   `IX_`/`UQ_`-Startrisiko (§9) ohne fachlichen Nutzen, L lässt „Oseon“ als Namen einer allgemeinen Tabelle stehen.
2. *Eingearbeitet (Antwort 2, 2026-10-12): ja, in derselben Migration.* **Audit-Felder (`AuditableEntity`)?** Empfehlung **ja**: ADR 0003, und Offsets steuern künftig „überfällig“ —
   wer sie geändert hat, ist fachlich relevant. Kostet eine additive Migration (`SQL/95`), Bestand
   `CreatedBy = 'system'`. Bei „nein“ entfällt jede Migration (bei H).
3. *Eingearbeitet (Antwort 3, 2026-10-12): wie empfohlen.* **Seed auf IDEAL?** `Program.cs:469-498` und `00_FreshInstall.sql:1491-1509` legen die 12 OSEON-Kürzel auf jeder
   leeren Tabelle an. Ein Web-seitiger „OSEON aktiv“-Schalter existiert nicht: in Frage kämen nur der
   Service-Schlüssel `Sync:OseonTrackingEnabled` (Default `false`, `ServiceSettingDefinitions.cs:22`) oder
   `TeileverfolgungAktiv` (`_Layout.cshtml:38`) — beide stehen auf einer frischen DB beim ersten Start auf aus, ein
   Gate würde also auch eine frische AKE-DB ohne Seed lassen; zudem greift ein Gate nur bei leerer Tabelle, eine schon
   geseedete IDEAL-DB änderte es nicht. Empfehlung: **Code unverändert lassen**; auf der IDEAL-DB nach P3
   kollidierende Einträge auf IDEAL-Werte setzen, übrige OSEON-Einträge per UI löschen oder stehen lassen
   (wirkungslos ohne OSEON-Daten).
4. *Eingearbeitet (Antwort 4, 2026-10-12): wie empfohlen.* **Navigation:** zusätzlicher Eintrag „Arbeitsgänge“ im Admin-Block des Stammdaten-Dropdowns, sichtbar
   unabhängig von `BdeAktiv`? Empfehlung **ja** — ohne ihn ist die Seite bei BDE aus nur per URL erreichbar
   (`_Layout.cshtml:244`).
5. *Eingearbeitet (Antwort 5, 2026-10-12): GET → 302, POST entfällt.* **`/Settings/OperationConfig`:** Weiterleitung oder Rückbau? Empfehlung: **GET → 302-Weiterleitung** auf die neue
   Seite, POST-Actions entfallen, Knopf in `/Settings` zeigt auf „Arbeitsgänge (BDE-Stammdaten)“. Rückbau des
   Redirects frühestens ein Release später.
6. *Eingearbeitet (Antwort 6, 2026-10-12): wie empfohlen.* **Was bedeutet „Fremdsystem-relevant“ für IDEAL und AKE-BDE?** Heute werten nur OSEON-Pfade das Flag aus
   (FA 2 #3–#6); Teil 2 wertet es ausdrücklich nicht aus (Teil-2-Spec FA 4). Empfehlung: **keine Wirkung** außerhalb
   OSEON; die Legende sagt das; Default beim Anlegen bleibt „an“ (`OseonOperationConfig.cs:32`).
7. *Eingearbeitet (Antwort 7, 2026-10-12): wie empfohlen.* **AKE-Standard-AG `01` in „nicht konfiguriert“?** Empfehlung **nein**: `01` ist der Default-Marker des BDE-NurFA-
   Pfads (`BdeDefaultWorkOperationService.cs:28,37`), kein fachliches Kürzel; ohne Eintrag rechnet er mit Offset 0
   (Teil 3, Antwort 7). Wer für `01` doch einen Offset will, legt ihn manuell an — der Lookup findet ihn.
8. **NEU (Rework 2026-10-12) — Eindeutigkeit: konvergieren oder nur umbenennen?** Antwort 1 spricht von
   „Index-/Constraint-Umbenennung“. Der Rumpf (FA 1 „Technische Festlegung“, Batches B4–B6) benennt den EF-Index
   `IX_OseonOperationConfigs_OperationName` um, **ersetzt** aber den Skript-/FreshInstall-Constraint
   `UQ_OseonOperationConfigs_OperationName` durch den Unique-Index `IX_OperationConfigs_OperationName` (erst Index
   anlegen, dann Constraint droppen), statt ihn nur in `UQ_OperationConfigs_OperationName` umzubenennen. Weicht das
   vom Wortlaut ab, deshalb hier zur Bestätigung. Empfehlung **konvergieren**: danach hat jede DB genau die Form und
   den Namen des EF-Modells, die Doppelform als Ursache von [[fallstricke]] §9 ist für diese Tabelle beseitigt; Kosten:
   ein `CREATE UNIQUE INDEX` + `DROP CONSTRAINT` auf einer Tabelle mit wenigen Dutzend Zeilen, ohne Lücke in der
   Eindeutigkeit. Bei „nur umbenennen“ ändern sich: B5/B6 werden zu einem guarded
   `sp_rename N'dbo.UQ_OseonOperationConfigs_OperationName', N'UQ_OperationConfigs_OperationName', N'OBJECT'`;
   FreshInstall (a) behält `CONSTRAINT [UQ_OperationConfigs_OperationName] UNIQUE`; AK 19/20 und P6 erwarten je nach
   DB-Form `IX_` **oder** `UQ_`, Drift-Test und R1 bleiben sonst gleich.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. Datenmodell U / H / L → **U – Tabelle umbenennen (Gerald 2026-10-12, abweichend von der Empfehlung H).** Migration mit Index-/Constraint-Umbenennung; Startrisiko §9 (`IX_`/`UQ_`-Namen) im Rumpf absichern. **Nachtrag (Gerald 2026-10-12): Keine externen Leser auf die Tabelle – keine Kompatibilitaets-View.**
2. Audit-Felder (AuditableEntity) ja / nein → **Ja (Gerald 2026-10-12)**; Bestand `CreatedBy = system`. Mit U in derselben Migration moeglich.
3. Seed auf IDEAL (Code unveraendert + Bereinigung per UI?) → **Wie empfohlen (Gerald 2026-10-12).**
4. Menueeintrag „Arbeitsgaenge“ im Stammdaten-Dropdown (Admin) → **Wie empfohlen (Gerald 2026-10-12).**
5. /Settings/OperationConfig: Weiterleitung oder Rueckbau → **GET → 302-Weiterleitung, POST entfaellt (Gerald 2026-10-12).**
6. Bedeutung „Fremdsystem-relevant“ fuer IDEAL / AKE-BDE → **Wie empfohlen (Gerald 2026-10-12).**
7. AKE-Standard-AG „01“ in „nicht konfiguriert“ → **Wie empfohlen (Gerald 2026-10-12).**
8. Eindeutigkeit OperationName: konvergieren (UQ_ → IX_OperationConfigs_OperationName, Empfehlung) oder nur umbenennen (UQ_OperationConfigs_OperationName) → **Konvergieren auf IX_OperationConfigs_OperationName (Gerald 2026-10-12), wie Rumpf B4–B6.**

## Umsetzung (2026-10-12, Dev-Lauf)

Direkt im Bündel `feature/2026-08-07-ideal-teile-1-5` (Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`; `worktree`/
`branch` im Frontmatter bleiben laut Umsetzungsort-Hinweis leer), Start `87db96c8` (v1.53.0). Plan
`docs/superpowers/plans/2026-10-12-arbeitsgang-stammdaten-vereinheitlichen.md`. Aufgabe
[[2026-10-12-arbeitsgang-stammdaten-vereinheitlichen-umsetzung]]. **Version 1.54.0, TS-87, SQL/95 (frei in Bündel und main:
Bündel max 94, main max 88), Migration `20261012140548_RenameOperationConfigsAndAddAudit`.**

| Commit | Inhalt |
|---|---|
| `d4bb711a` | Umbenennung Code (Klasse/DbSet/Repository/Decorator/Tests) + Modell (`AuditableEntity`, Audit-Mapping, Seed `system`); Migration per `migrations add` erzeugt (EF schlug **DropTable+CreateTable** vor = R2), `Up()`/`Down()` vollständig durch 9 + 5 guarded `migrationBuilder.Sql` ersetzt (aus EINER Batch-Liste zusammen mit `SQL/95` generiert); `SQL/95`; FreshInstall (Tabellenblock, Unique-Index, Seed, History); Drift-Wächter `OperationConfigsMigrationSqlTests` |
| `5ac5ea16` | `GetDueDateOffsetsAsync`, `GetUnconfiguredOperationsAsync` (OSEON + WMS-Arbeitsgang, ohne `01`), `ExistsAsync` in C# getrimmt/case-insensitiv, Decorator mit Offsets-Key (Invalidierung **nach** dem Schreiben); `OperationConfigsController`, `_BdeMasterDataTabs`, `Views/OperationConfigs/Index`, Layout-Eintrag, `/Settings/OperationConfig` → 302, Begriffe (Reporting, Tracking, Hilfe); Tests |
| `b5123529` | v1.54.0, Anwender-Changelog, README (SQL/95), TS-87 + Nachzüge TS-1.7/7.9/7.10/16.5/36.5 — **feature-complete** |

**Offline-Nachweise (Ablauf Schritt 4/5):** `dotnet ef migrations has-pending-model-changes` → „No changes have been made
to the model since the last migration.“; `dotnet ef migrations script 20261012073551_AddBeschichtungsauftrag
20261012140548_RenameOperationConfigsAndAddAudit` → `DROP TABLE` 0, `CREATE TABLE` 0, `DROP INDEX` 0, `sp_rename` ohne
vorangehendes `IF` 0, `CREATE UNIQUE INDEX` 1 (geguarded, B5). `git diff 87db96c8` an `OseonDueDateCalculator.cs`,
`WorkOperationStructureDetectionService.cs`, `SQL/34`, `SQL/44`, `20260330142035_AddOseonOperationConfig.cs`: leer (AK 13,
7a). Alte Namen nur noch im OSEON-ViewModel (`OseonTrackingViewModel.cs:70`, `_OseonGroupDetails.cshtml`,
`OseonGroupViewModelBuilder.cs` VM-Zeilen) und im Drift-Test.

**Beweis (vor QA):** `dotnet build IdealAkeWms.slnx` 0 Fehler; `dotnet test IdealAkeWms.slnx` Web **1794 bestanden,
1 übersprungen** (vorher 1767), Service **272**. `docs/TESTSZENARIEN.md` Index-Blob LF.

### Entscheidungen im Dev-Lauf (Rulings)

- **R1 Kein Zahnrad:** Der Rumpf (Out-of-Scope, ADR-0005-Abschnitt) schließt die Spaltenauswahl aus wie bei den
  Nachbar-Reitern; der Auftrag nannte §3/§19 als Regeln — sie greifen ohne `#column-config` nicht. Alle `<th>` tragen
  `data-col-key` (auch Aktionsspalte, CSV-Export v1.53.0).
- **R2 FreshInstall-Seed geguarded:** verschachteltes `IF OBJECT_ID(...)` um das Seed-`INSERT` — legt 16b wegen vorhandener
  Alt-Tabelle nichts an (R6), bräche sonst der Seed-Batch.
- **R3 MigrationId-Zählung:** `SQL/95` nennt die ID wie FreshInstall **zweimal** als Literal (Existenzprüfung + Insert, Muster
  `SQL/94`); der Spec-Satz „1× in SQL/95“ ist so ausgelegt (Drift-Test prüft 2×).
- **R4 Decorator invalidiert nach dem Schreiben** (vorher davor) — sonst kann ein paralleler Leser zwischen Invalidieren und
  `SaveChanges` den alten Stand 10 Minuten cachen.
- **R5 Reiter-Sichtbarkeit** im Partial per `@inject` (`IAppSettingRepository`, `ICurrentUserService`) statt Controller-
  Abhängigkeit — dieselbe Regel auf beiden Seiten, Controller bleibt schlank.
- **R6 Duplikat-Hinweis** aus `GetAllAsync` im Controller (keine eigene Repository-Methode).

### Zusatzfunde (nicht mitgebaut)

- **Z1** `README.md`-Skriptliste endet bei `SQL/80`; `SQL/81`–`94` fehlen dort (vorbestehend). Nur `SQL/95` ergänzt.
- **Z2** [[2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-2-spec]] P4 fragt noch `dbo.OseonOperationConfigs`/`IsOseonRelevant`
  ab — nach dem Deploy „Ungültiger Objektname“ (in dieser Spec schon gemeldet, Umstellung ist Sache jener Spec).

## QA (2026-10-12)

Unabhängiger QA-Lauf (qa-agent) im Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Zweig
`feature/2026-08-07-ideal-teile-1-5`, Diff `87db96c8..b5123529` (`d4bb711a`, `5ac5ea16`, `b5123529`), 45 Dateien. Review
selbst in diesem Lauf durchgeführt (kein Unteragent). Keine App gestartet, keine DB berührt, kein Code geändert.

**Urteil: NICHT Testbereit — ein offener Befund „Important“ (I-1).** Status bleibt `InUmsetzung`. Alles andere ist grün;
nach Behebung von I-1 genügt ein kurzer Nach-QA (SQL/95 + Drift-Test + TS-87.9-Negativfall).

### 1. Build und Tests (selbst ausgeführt)

- `dotnet build IdealAkeWms.slnx` → **0 Fehler**, 12 Warnungen (alle vorbestehend: NU1902 MailKit/MimeKit, CS8602
  `TrackingController.cs:332` — nicht in geänderten Zeilen, CS8321 `FormatDateWithKw` in drei fremden Views).
- `dotnet test IdealAkeWms.slnx` →
  - `IdealAkeWms.Tests`: **1794 bestanden, 1 übersprungen, 0 Fehler** (1795 gesamt)
  - `IDEALAKEWMSService.Tests`: **272 bestanden, 0 übersprungen, 0 Fehler**
- Offline-EF (selbst ausgeführt, ohne DB):
  - `dotnet ef migrations has-pending-model-changes --project IdealAkeWms` → „No changes have been made to the model since
    the last migration.“
  - `dotnet ef migrations script 20261012073551_AddBeschichtungsauftrag 20261012140548_RenameOperationConfigsAndAddAudit`
    → `DROP TABLE` 0, `CREATE TABLE` 0, `DROP INDEX` 0, 5× `sp_rename`, jedes direkt hinter einem `IF`; 1× `CREATE UNIQUE
    INDEX` (B5, geguarded); History-Insert mit exakter ID.
  - Designer-Modell (`…RenameOperationConfigsAndAddAudit.Designer.cs`) und `ApplicationDbContextModelSnapshot.cs` ab
    `HasAnnotation("ProductVersion"…)` zeilengleich (3203 = 3203 Zeilen, 0 Differenzen) → Snapshot/Designer stammen
    konsistent aus `migrations add`; nur `Up()`/`Down()` ersetzt.
- `docs/TESTSZENARIEN.md` im Commit-Blob `HEAD`: **0 CRLF** (python, byte-genau; 536 133 Bytes).

### 2. Code-Review — Befunde und Behandlung

**I-1 (Important) — `SQL/95` bricht im Anomaliefall nicht ab, sondern läuft weiter und trägt die Migration als angewendet
ein.** `SQL/95_RenameOperationConfigsAndAddAudit.sql:12-16` (B1) wirft per `THROW` nur den **Batch** ab. SSMS (und `sqlcmd`
ohne `-b`/`:on error exit`) führt die folgenden `GO`-Batches trotzdem aus: B5 (`:34-36`) legt einen Unique-Index auf der
*neuen, leeren* `OperationConfigs` an, B7–B9 (`:44-65`) fügen ihr die Audit-Spalten hinzu, und der History-Batch
(`:68-74`) trägt `20261012140548_RenameOperationConfigsAndAddAudit` ein. Folge: `Migrate()` hält die Umbenennung für
erledigt; startet die App, arbeitet sie auf der leeren neuen Tabelle (Seed `Program.cs:471` füllt sie mit den 12
OSEON-Defaults), die echten Offsets bleiben in `OseonOperationConfigs` liegen — genau das „stille Weiterarbeiten“, das R6
verhindern soll. AK 22 („bricht `SQL/95` … ab“) und TS-87.9-Negativfall prüfen nur „nichts umbenannt“ und würden das nicht
bemerken. Der `Migrate()`-Weg ist korrekt (eine Transaktion, `THROW` → Rollback, keine History-Zeile).
*Behandlung:* offen, Dev-Lauf. Vorschlag (eine Stelle, Migration bleibt unverändert, Drift-Test bleibt grün, weil nur
`SQL/95` ergänzt wird): in B1 von `SQL/95` nach der Anomalie-Prüfung zusätzlich `SET NOEXEC ON;` (bzw. Muster
`IF … BEGIN RAISERROR(…,16,1); SET NOEXEC ON; END` vor dem `THROW`) und am Skriptende `SET NOEXEC OFF;` — dann kompiliert
SSMS die Folgebatches nur noch, führt sie aber nicht aus; alternativ den History-Batch zusätzlich an
`OBJECT_ID(N'dbo.OseonOperationConfigs', N'U') IS NULL` binden. TS-87.9-Negativfall um „keine History-Zeile
`…RenameOperationConfigsAndAddAudit`, `OperationConfigs` unverändert“ ergänzen.

**M-1 (Minor, vorbestehend, mitgewandert) — stiller Fallback auf Offset 0 und fehlende Server-Längenprüfung.**
`OperationConfigsController.cs:72` und `:102` binden `int dueDateOffsetDays` ohne `ModelState`-Prüfung; das Feld ist
nicht `required` (`Views/OperationConfigs/Index.cshtml:79`, `:149`). Ein geleertes Offset-Feld in der Zeile speichert
still `0`. `maxlength` 100/200 gilt nur im Browser; ein manipulierter POST mit > 100 Zeichen endet in einer
`DbUpdateException` (500) statt einer Warnung. Identisch zur alten Settings-Seite (`OperationConfig.cshtml:61,138` bei
`87db96c8`), also keine Regression — mit Teil 2/3 bestimmt der Offset aber „überfällig“. *Behandlung:* nicht blockierend,
als Folgearbeit empfohlen (`required` + `ModelState.IsValid` → `WarningMessage`, Länge serverseitig prüfen).

**M-2 (Minor) — FreshInstall trägt die neue MigrationId auch ein, wenn 16b wegen vorhandener Alt-Tabelle nichts
angelegt hat.** `SQL/00_FreshInstall.sql:1221-1248` druckt im R6-Zweig nur eine Warnung, der History-Block
(`:2439-2440`) schreibt die ID trotzdem → auf einer solchen DB würde `Migrate()` die Umbenennung überspringen. FreshInstall
ist laut Kopf nur für leere DBs; `SQL/95` repariert den Zustand unabhängig von der History. *Behandlung:* hingenommen,
Hinweis für die Doku.

**M-3 (Minor) — Reichweite des Drift-Wächters.** `OperationConfigsMigrationSqlTests` beweist: neue Namen stehen in
Migration/`SQL/95`/FreshInstall (Textsuche; `"OperationConfigs"` ist allerdings auch Teilstring von
`"OseonOperationConfigs"` und daher schwach), keine strukturierte Operation in der Migration (14 `Sql(`-Aufrufe), jeder
Up-Batch wortgleich in `SQL/95`, Snapshot nur mit neuer Entität, FreshInstall ohne Alt-`CREATE`/`INSERT`/`UQ_`, MigrationId
2×/2×. Er beweist **nicht**: Reihenfolge B4 → B5 → B6 in `SQL/95`, `GO` zwischen den Batches, den `Down()`-Weg, die
FreshInstall-Index-Batch-Logik, Lauffähigkeit/Idempotenz auf SQL Server und das Abbruchverhalten (I-1). Reihenfolge und
`GO` habe ich per Lesen bestätigt (`SQL/95:12-64`, je Batch ein `GO`); Rest = Manual-UAT TS-87.9. *Behandlung:* hingenommen.

**M-4 (Minor, Brain) — Pflichten noch offen:** `secondbrain/tests/testszenarien-index.md` hat noch keine Zeile TS-87;
Brain-Changelog `changelog/2026-10-12-v1-54-0-…` fehlt; `feature-map.md` ohne Eintrag. (Glossar, `fallstricke` §24,
`codebase/{controller,datenmodell,integrationen,services}` sind nachgezogen.) *Behandlung:* beim Brain-Commit der
Koordination nachziehen; QA schreibt laut Auftrag nur in diese Spec.

**Geprüft ohne Befund** (mit Fundstelle):
- *Migration* `Migrations/20261012140548_RenameOperationConfigsAndAddAudit.cs`: Up = 9 Sql-Aufrufe B1–B9 in Spec-Reihenfolge
  (B4 Rename vor B5 Create vor B6 Drop → keine Lücke in der Eindeutigkeit), je Aufruf ein Batch ohne `GO`, `THROW` in B1,
  B8 eigener Batch nach B7, B9 mit `is_nullable`-Guard; Down D1–D5 geguarded, endet bewusst in der EF-Form. Drei DB-Formen
  durchgespielt: (a) EF-Form `IX_`: B1/B2/B3/B4 greifen, B5/B6 no-op; (b) Skript-Form `UQ_`: B1/B2/B3, B4 no-op, B5 legt
  `IX_…` neben `UQ_…` an (zulässig), B6 droppt `UQ_`; (c) neue FreshInstall: Zielzustand + History → EF tut nichts.
  `sp_rename` innerhalb der EF-Transaktion ist zulässig, die nachfolgenden `COL_LENGTH`/`sys.indexes`-Guards sehen die
  Umbenennung in derselben Transaktion. Unbenannte `DEFAULT 0/1`-Constraints (nur Skript-/FreshInstall-Form, `SQL/34:12-13`)
  wandern mit `sp_rename` mit; EF-Form hat keine (`20260330142035_AddOseonOperationConfig.cs:22`). Wirkungslos (EF schreibt
  beide Werte immer) und von P6 nicht abgefragt — P6 bleibt formgleich.
- *SQL/95*: Batches wortgleich zur Migration (Drift-Test + Lesen), `GO` je Batch, History separat mit exakter ID; Ruling R3
  (ID 2× als Literal) ist sachgerecht.
- *FreshInstall*: 16b mit Doppel-Guard + R6-Warnung, Unique-Index als eigener Batch mit `sys.indexes`-Guard, Seed mit
  verschachteltem `IF OBJECT_ID` (Ruling R2 korrekt — deferred name resolution, INSERT läuft nie ohne Tabelle), Audit-Werte
  `GETDATE()`/`N'system'`, History-Zeile.
- *Modell/Mapping*: `OperationConfig : AuditableEntity`, `ToTable("OperationConfigs")`, Unique-Index ohne
  `HasDatabaseName`, Audit-Längen 200 + `IsRequired` (`ApplicationDbContext.cs:883-894`); Seed `Program.cs:490-499` setzt
  `system`.
- *OSEON-Regression (AK 11–13)*: `GetAllAsDictionaryAsync` unverändert case-sensitiv (`OperationConfigRepository.cs:61-65`),
  Reporting-Dictionary unverändert `OrdinalIgnoreCase` (`OseonProductionOrderRepository.cs:214`), Tracking/Builder/Reporting
  nur Bezeichner (`TrackingController.cs:319,386`, `OseonGroupViewModelBuilder.cs:44`, `OseonReportingController.cs:148`,
  `OseonProductionOrderRepository.cs:256`). `git diff 87db96c8..HEAD` an `OseonDueDateCalculator.cs`,
  `WorkOperationStructureDetectionService.cs`, `SQL/34`, `SQL/44`, `20260330142035_*`, `20260420123329_SetDefaultSchemaDbo.cs`:
  leer. ViewModel-Property `OseonOperationViewModel.IsOseonRelevant` bewusst geblieben.
- *Repository/Decorator (AK 8–10, 17)*: Offsets `OrderBy(Id)` + `TryAdd` → kleinste Id gewinnt, Schlüssel getrimmt,
  `OrdinalIgnoreCase`, alle Zeilen; Liste „nicht konfiguriert“ in C# getrimmt/case-insensitiv gegen dieselbe Normalisierung,
  `"01"` ausgeschlossen, Null-`OperationNumber` abgefangen; `ExistsAsync` in C#. Decorator: eigener Key, Invalidierung nach
  dem Schreiben (Ruling R4 korrekt; bei Exception bleibt der Cache gültig, weil nichts geschrieben wurde). Bewusste, in FA 5
  festgelegte Asymmetrie: für OSEON-Namen zählt die Liste case-insensitiv, die Teileverfolgung case-sensitiv — nur bei
  Groß/Klein-Abweichung relevant, P2 klärt die Collation.
- *Controller/Zugriff (AK 1–6, 14, 16)*: Klasse `[RequireAdminAccess]`, kein `[RequireBdeActive]`, alle POST mit
  `[ValidateAntiForgeryToken]`; Audit Add/Update aus `ICurrentUserService`; `SettingsController.OperationConfig()` →
  `RedirectToAction` (302, nicht permanent), Repository-Abhängigkeit und die drei POSTs entfernt. Server-Spaltenfilter vor
  der Pagination, Getter liefern gerenderten Text (`+2`, `Ja`/`Nein`).
- *Views*: `_BdeMasterDataTabs.cshtml` liest `BdeAktiv` mit derselben Regel wie `RequireBdeActiveAttribute.cs:26-27`;
  BDE-Reiter nur bei `BdeAktiv && CanManageBdeShiftlead`, „Arbeitsgänge“ nur Admin; `aria-current`. Layout-Eintrag im
  `@if (isAdmin)`-Block (`_Layout.cshtml:314-315`). `Views/OperationConfigs/Index.cshtml`: alle `<th>` mit `data-col-key`
  (inkl. Aktionsspalte), Server-Mode, `_Pagination`, kein `@Html.Raw`, Lösch-Bestätigung über `data-confirm-name` statt
  Razor-in-JS-String (die alte Seite hatte genau das, `OperationConfig.cshtml:82`), kein `hidden` + `d-*`, Hidden+Checkbox
  für Relevanz, `aria-label` an allen Zeilen-Eingaben, Relevanz nicht nur über Farbe. Ohne `column-preferences.js`
  initialisiert `table-filter.js` sofort (`table-filter.js:648`); CSV-Export liest Inputs/Checkboxen (`csv-export.js:87-91`).
- *Begriffe (AK 7, 7a)*: `grep "OSEON-relevant|OSEON-Relevanz"` in `IdealAkeWms/Views/` → 0 Treffer;
  `grep "OseonOperationConfig|IsOseonRelevant|GetUnconfiguredOperationNamesAsync"` in `IdealAkeWms`, `IdealAkeWms.Tests`,
  `IDEALAKEWMSService` (ohne `Migrations/`) → nur `OseonTrackingViewModel.cs:70`, `OseonGroupViewModelBuilder.cs:69,78`,
  `_OseonGroupDetails.cshtml:69,80` (ViewModel) und der Drift-Test.
- *Doku*: TS-87.1–87.10 + Nachzüge TS-1.7/7.9/7.10/16.5/36.5, Hilfe (`Help/Index.cshtml`), Anwender-Changelog, README
  (`SQL/95`), beide `AppVersion.cs` = 1.54.0.
- Defektklassen der Checkliste: Script-Embedding (keins), `hidden`+`d-*` (keins), stille Fallbacks (M-1), Parallel-Arrays
  (keine), Browser-only-Validierung (M-1), eine Regel an mehreren Stellen (Normalisierung trim+OrdinalIgnoreCase dreimal in
  C# — `GetDueDateOffsetsAsync`, `GetUnconfiguredOperationsAsync`, `ExistsAsync` + Dubletten im Controller; gleich
  implementiert, kein SQL/JS-Zwilling).

### 3. Beweisart je AK

| AK | Beweis | Art |
|---|---|---|
| 1 | `Zugriff_NurAdmin_OhneBdeAktivGate…` (Attribute) + Reiter-Sichtbarkeit | Unit (Attribut) + **Manual-UAT TS-87.1** |
| 2 | Razor-Partial | **Manual-UAT TS-87.2** |
| 3 | Attribut-Test; Filter-Laufzeit | Unit + **Manual-UAT TS-87.2** |
| 4 | Layout-Eintrag (Review) | **Manual-UAT TS-87.1/87.2** |
| 5 | `AlteUrl_Settings_OperationConfig_302_PostsEntfallen` (non-permanent, POSTs fehlen); echte 302 + Links | Unit + **Manual-UAT TS-87.5** |
| 6 | `Add_LeeresKuerzel_Warnung`, `Add_Getrimmt_MitAudit_DubletteCaseInsensitivAbgelehnt`, `Update_…`, `Delete_…` | Unit (+ TS-87.3) |
| 7 | `Begriffe_KeinOseonRelevantMehrInViews_AK7`; Legende | Unit + **Manual-UAT TS-87.6** |
| 7a | grep (dieser Lauf) | Review |
| 8 | `GetUnconfiguredOperationsAsync_*` (3 Tests); echte Collation | Unit + **Manual-UAT TS-87.4** |
| 9 | `GetDueDateOffsetsAsync_CaseInsensitiv_Getrimmt_Null_Fehlend_NichtRelevantEnthalten` | Unit |
| 10 | `Decorator_CachtOffsets_UndInvalidiertNachSchreiben` | Unit |
| 11 | `GetAllAsDictionaryAsync_BleibtCaseSensitiv` + Bestandstests | Unit + **Manual-UAT TS-87.7** |
| 12 | `OseonProductionOrderRepositoryReportingTests`, `OseonReportingControllerTests` (nur Bezeichner) | Unit + **Manual-UAT TS-87.7** |
| 13 | `git diff` leer (dieser Lauf) | Review |
| 14 | Controller-Audit-Tests; Seed per Review; Bestandszeilen auf SQL Server | Unit + **Manual-UAT TS-87.3/87.9** |
| 15 | `has-pending-model-changes`, `migrations script` (dieser Lauf, offline) | Offline-Tool |
| 16 | `Index_Spaltenfilter_Offset_UndRelevanz_WieAngezeigt` (Filter vor Pagination, TotalCount); `data-col-key` per Review; „Alle“/Seitenwechsel im Browser | Unit + **Manual-UAT TS-87.8** |
| 17 | `Index_MeldetDubletten_UndListetNichtKonfigurierte`, `…KleinsteIdGewinnt` (InMemory erzwingt Unique nicht — Altdaten simuliert); Banner-Rendering | Unit + **Manual-UAT** |
| 18 | Doku-Review; Build/Test grün | Review + Build |
| 19 | `OperationConfigsMigrationSqlTests` (5 Tests, Reichweite siehe M-3) | Unit (Text) |
| 20 | P6 auf drei DB-Formen | **Manual-UAT TS-87.9** — nicht grün gezählt |
| 21 | `Migrate()` auf Form (a)/(b) | **Manual-UAT TS-87.9** — nicht grün gezählt |
| 22 | `SQL/95` 2× + Anomalie | **Manual-UAT TS-87.9** — nicht grün gezählt; **I-1 offen** |

### 4. Geänderte Bestandstests

| Test | Änderung | Einstufung |
|---|---|---|
| `Controllers/OseonReportingControllerTests.cs` | Typ/DbSet/Property umbenannt | nur Typ (Compile-Fix) |
| `Controllers/TrackingControllerTests.cs` | Mock-Typ umbenannt | nur Typ |
| `Repositories/OseonProductionOrderRepositoryReportingTests.cs` | Typ/DbSet/Property umbenannt, Fixture-Werte unverändert | nur Typ |
| `Repositories/OseonOperationConfigRepositoryTests.cs` → `OperationConfigRepositoryTests.cs` | 8 Tests nur Bezeichner | nur Typ |
| dort `GetUnconfiguredOperationNamesAsync_FindsNamesWithoutConfig` | entfernt, ersetzt durch `GetUnconfiguredOperationsAsync_ZweiQuellen_…` (enthält dieselben Zusicherungen BG/ZB gelistet, ST nicht, plus Quellen/`01`/Groß-Klein) | Verhalten bewusst geändert — FA 5 / AK 8 (Methode ersetzt) |

Kein „sonstiger“ Fall, keine nachgebogene Fixture.

### 5. CLAUDE.md-Checkliste

- [x] Migration + `SQL/95` mit Guards, DDL je Batch, History separat, exakte ID
- [x] `00_FreshInstall.sql` an beiden Stellen (16b Schema + Index-Batch, Seed, `MigrationId`)
- [x] `SQL/AgentJobs/` nicht betroffen (0 Treffer)
- [x] Audit-Felder (Add/Update/Seed/Bestand)
- [x] Version beide `AppVersion.cs` 1.54.0 + Anwender-Changelog; [ ] Brain-Changelog fehlt (M-4)
- [x] `docs/TESTSZENARIEN.md` TS-87 + Nachzüge; [ ] Testindex TS-87 fehlt (M-4)
- [x] Keine neue Rolle/kein neuer Filter; `controller.md` ergänzt
- [x] Keine Einmal-Skripte (`SQL/Einmalig/` nicht berührt)

### 6. Manuelle Checkliste für den Menschen (nach Behebung von I-1)

1. Prüfschritte P1–P5 „vor dem Deploy“ auf AKE- und IDEAL-DB; bei fehlender `20260330142035_AddOseonOperationConfig`
   anhalten (R10).
2. Testsystem: drei DB-Kopien (a) EF-Form `IX_`, (b) Skript-Form `UQ_`, (c) leere DB + neues `00_FreshInstall.sql`;
   Zeilenzahl (a)/(b) notieren.
3. (a) und (b): `SQL/95` ausführen, dann ein zweites Mal (keine Fehler, keine Änderung); je eine weitere Kopie nur per
   App-Start migrieren lassen; (c) nur App-Start. P6 auf allen drei: identisch, kein `UQ_`, Zeilenzahl gleich,
   Bestand `CreatedBy = 'system'` (TS-87.9).
4. Wegwerf-Kopie mit beiden Tabellen: `SQL/95` → B1-Meldung **und** danach keine History-Zeile
   `…RenameOperationConfigsAndAddAudit`, `OperationConfigs` unverändert (I-1-Nachweis); App-Start auf dieser Kopie bricht
   mit derselben Meldung ab.
5. Publish Web aus dem Worktree, Testsystem: App-Pool stoppen → Backup → `SQL/95` → Binaries → Start.
6. TS-87.1: `BdeAktiv = false`, Admin → Stammdaten › Arbeitsgänge → nur Reiter „Arbeitsgänge“; `/BdeMasterData` → Home +
   „BDE ist nicht aktiviert.“
7. TS-87.2: `BdeAktiv = true`, Admin vier Reiter, Wechsel in beide Richtungen; `bde_shiftlead` und `masterdata`: kein
   Reiter/Menüeintrag, `/OperationConfigs` GET + POST → AccessDenied.
8. TS-87.3: `KA`/„Kanten“/+3/relevant aus anlegen, ändern, löschen; Negativ leer, `ka`, ` KA `; SQL-Abfrage Audit.
   Zusätzlich (M-1): Offset-Feld einer Zeile leeren und speichern → beobachten (derzeit still 0).
9. TS-87.4: IDEAL-Kürzel „WMS-Arbeitsgang“, OSEON-Name „OSEON“, beide Quellen kombiniert, kein `01`, verschwindet nach Anlage.
10. TS-87.5: `/Settings/OperationConfig` → 302 (Netzwerk-Reiter), Knopf in `/Settings`, Link im OSEON-Reporting-Banner.
11. TS-87.6: Legende (drei Bezugstermine, Relevanz nur OSEON), Tracking-Schalter „Fremdsystem-Relevanz“.
12. TS-87.7 (AKE): TS-7.9, TS-16.1–16.5, 30.8 mit gleichen Daten → gleiche Ampeln, Soll-Termine, Reporting-Zeilen.
13. TS-87.8: Spaltenfilter `+2`, `nein`, Seitengröße „Alle“, Seitenwechsel, CSV-Export mit allen Spalten.
14. TS-87.10: Filter ohne Treffer → Hinweiszeile; manipulierte Id → 404; ohne Daten → „Alle bekannten Kürzel sind konfiguriert.“
15. IDEAL-DB: P3 auswerten, kollidierende Seed-Kürzel per UI auf IDEAL-Werte, IDEAL-Kürzel aus „Nicht konfigurierte
    Kürzel“ anlegen (vor Abnahme FA-Struktur Teil 2/3).

### Fehlerbericht (Status bleibt InUmsetzung)

- **Was:** Anomalie-Abbruch von `SQL/95` (R6/AK 22) wirkt nur auf Batch B1.
- **Erster Fehler:** `SQL/95_RenameOperationConfigsAndAddAudit.sql:12-16` `THROW` beendet nur den Batch; `:68-74` trägt die
  MigrationId trotzdem ein.
- **Vermutete Ursache:** Batch-Semantik von SSMS/`sqlcmd` (Fehler in einem `GO`-Batch stoppt die folgenden nicht); in der
  EF-Migration (eine Transaktion) tritt das Problem nicht auf, darum blieb es im Spiegel-Skript unbemerkt.
- **Fix-Versuche durch QA:** keine (Auftrag: keine Code-Fixes). Nach Fix: Nach-QA auf `SQL/95` + Drift-Test + Checkliste Punkt 4.
- *Nachtrag:* behoben in `d56a340e`, siehe „QA-Nachlauf (2026-10-12)“.

### QA-Nachlauf (2026-10-12)

Gezielter Nach-QA (qa-agent, Review selbst in diesem Lauf, kein Unteragent) auf Fix-Commit `d56a340e` (Diff
`b5123529..d56a340e`, 6 Dateien, +48/−4) im Bündel-Worktree; Worktree sauber, HEAD = `d56a340e`. Keine App gestartet,
keine DB berührt, kein Code geändert. Migration/Designer/Snapshot im Fix unverändert (`git diff --stat -- '*/Migrations/*'` leer).

**Urteil: Testbereit.** I-1 ist geschlossen, kein offenes Critical/Important. Status → `Testbereit`.

**Build und Tests (selbst ausgeführt, HEAD `d56a340e`):**
- `dotnet build IdealAkeWms.slnx` → 0 Fehler, 12 Warnungen (dieselben vorbestehenden wie im ersten QA-Lauf).
- `dotnet test IdealAkeWms.slnx` → `IdealAkeWms.Tests` **1795 bestanden, 1 übersprungen, 0 Fehler** (1796; +1 neuer Test
  `Sql95_KonvergenzReihenfolge_UndHaltNachAnomalie`); `IDEALAKEWMSService.Tests` **272 bestanden, 0 übersprungen, 0 Fehler**.
- `OperationConfigsMigrationSqlTests` gefiltert: 6/6 grün. `docs/TESTSZENARIEN.md` und `SQL/95` im Commit-Blob: 0 CRLF.

**I-1 — geschlossen.** `SQL/95_RenameOperationConfigsAndAddAudit.sql:22-27` (B1a) prüft nach B1, ob
`dbo.OseonOperationConfigs` noch existiert, druckt „ABBRUCH: …“ und setzt `SET NOEXEC ON`; `:87` setzt am Ende
`SET NOEXEC OFF`. Durchgespielt:
- *Anomalie, SSMS bzw. `sqlcmd` ohne `-b`:* B1-`THROW` beendet nur Batch B1, B1a läuft, alte Tabelle steht noch → `PRINT`
  wird ausgeführt (steht vor dem `SET`), danach werden B2–B9 und der History-Batch nur noch kompiliert, nicht ausgeführt →
  nichts umbenannt, keine Audit-Spalten an der leeren `OperationConfigs`, **keine History-Zeile** → `Migrate()` läuft beim
  App-Start in denselben B1-`THROW` (Rollback). `SET NOEXEC OFF` wird auch bei aktivem NOEXEC wirksam (dasselbe Muster
  `IF … BEGIN PRINT …; SET NOEXEC ON; END` … `SET NOEXEC OFF` verwenden die von SSDT erzeugten Publish-Skripte); die
  Sitzung ist nach dem Volllauf wieder normal.
- *`sqlcmd -b`:* bricht schon bei B1 ab, B1a läuft nie, keine History-Zeile — korrekt.
- *Normalfall (Form a/b):* B1 benennt um → B1a: `OBJECT_ID` = NULL → tut nichts. *Zweiter Lauf* und *FreshInstall-Form (c)*:
  alte Tabelle fehlt → B1a tut nichts. B1a blockiert den Gutfall nie.
- *Zusatznutzen:* scheitert `sp_rename` in B1 aus anderem Grund (Rechte, Sperre), hält B1a ebenfalls an.
- B1a steht nur im Skript; der Drift-Wächter prüft Migration ⊂ `SQL/95`, nicht umgekehrt → bleibt grün (gelaufen).
- TS-87.9-Negativfall (`docs/TESTSZENARIEN.md:9670-9672`) prüft jetzt „ABBRUCH“, keine Audit-Spalten, **keine** History-Zeile.

**M-2 — geschlossen.** `SQL/00_FreshInstall.sql:2439-2441`: `IF NOT EXISTS (… History …) AND OBJECT_ID(N'[dbo].[OseonOperationConfigs]', N'U') IS NULL`.
Beide Prädikate sind reine Leseabfragen ohne Seiteneffekt und ohne Bindungsabhängigkeit voneinander; das fehlende
Kurzschluss-AND von T-SQL ist hier belanglos (anders als beim Seed `:1504`, wo das `INSERT` deshalb verschachtelt ist).
Der Zeilenkommentar endet am Zeilenende, das `INSERT` bleibt Rumpf des `IF`. Im R6-Fall (alte Tabelle da) fehlt die ID →
`Migrate()` holt die Umbenennung nach — konsistent mit 16b (`:1223`, `:1241-1242`).

**M-3 — geschlossen (im vorgeschlagenen Umfang).** `OperationConfigsMigrationSqlTests.cs:45` prüft Namen mit
`(?<!Oseon)\b{name}\b` (Lookbehind redundant zu `\b`, schadet nicht; `PK_…`/`IX_…` werden durch `\b` nicht fälschlich
getroffen, weil `_` ein Wortzeichen ist). `:69-85` neu: B4 < B5 < B6, `THROW 50001` < `SET NOEXEC ON;` < erstes
`PK_OseonOperationConfigs` (B2), History < letztes `SET NOEXEC OFF;`, ≥ 12 `GO`. Gedanklich mutiert: B1a entfernt →
`IndexOf` = −1 → rot; B1a hinter B2 verschoben → rot. Weiterhin nicht bewiesen (bleibt Manual-UAT TS-87.9): Laufzeit auf
SQL Server, Idempotenz, `Down()`, die Bedingung in B1a selbst.

**M-1 — teilweise geschlossen, Rest Minor (nicht blockierend).**
- Länge serverseitig: `OperationConfigsController.cs:80-84` (Add: Kürzel > 100 oder getrimmter Anzeigename > 200) und
  `:111-115` (Update: Anzeigename > 200) → `WarningMessage` + Redirect; Grenzen = Mapping `ApplicationDbContext.cs:887-888`
  und `maxlength` der View. Gemessen wird dieselbe getrimmte Form, die gespeichert wird. Erledigt.
- Offset: `required` an beiden Feldern (`Views/OperationConfigs/Index.cshtml:79-80`, `:149`); die Zeilenfelder hängen per
  `form="form-update-@c.Id"` am Formular, abgeschickt wird per `<button type="submit" form=…>` (`:91`) → die
  Browser-Validierung greift. **Rest N-3 (Minor):** serverseitig binden `Add`/`Update` (`OperationConfigsController.cs:72`,
  `:107`) weiter `int dueDateOffsetDays` ohne `ModelState`-Prüfung — ein POST ohne/mit leerem Offset (manipuliert oder
  Browser ohne Constraint-Validation) speichert still `0`. Defektklasse „nur im Browser validiert“; Normalbedienung ist
  abgedeckt. *Behandlung:* hingenommen, Folgearbeit empfohlen (`int?` + Warnung bei `null`), sinnvoll vor Teil 2/3.

**Neue Befunde dieses Nachlaufs:**
- **N-1 (Minor, Erwartung im Test) — zusätzliche Kompilierfehler im Anomaliefall.** Unter `SET NOEXEC ON` werden die
  Folgebatches weiter kompiliert. Hat die störende leere `dbo.OperationConfigs` keine Audit-Spalten, meldet B8
  (`SQL/95:64-66`, `UPDATE … SET [CreatedAt] …`) beim Kompilieren voraussichtlich „Ungültiger Spaltenname“ (Msg 207);
  ausgeführt wird nichts. TS-87.9 nennt nur B1-Meldung + „ABBRUCH“ — der Tester soll diese roten Folgemeldungen nicht als
  Fehlschlag werten. *Behandlung:* hingenommen; in der Checkliste unten vermerkt.
- **N-2 (Minor, Bedienung) — Teilausführung hinterlässt NOEXEC.** Wird in SSMS nur ein markierter Teil bis vor `:87`
  ausgeführt, bleibt die Sitzung auf `NOEXEC ON`; anschließende Prüfabfragen (P6) im selben Fenster liefern dann nur
  „Befehle erfolgreich abgeschlossen“ ohne Ergebnis. *Behandlung:* hingenommen; Deploy-Abschnitt sagt jetzt „vollständig
  ausführen“, Checkliste nennt neues Abfragefenster.
- **M-4 (Brain) — weiterhin offen:** `secondbrain/tests/testszenarien-index.md` ohne TS-87, kein Brain-Changelog
  `changelog/…-v1-54-0-…`, `feature-map.md` ohne Eintrag (gemessen per `grep`/`ls` im Hauptcheckout). Kein Code-Befund;
  vor dem Merge durch die Koordination nachziehen.

**Regressionen:** keine gefunden. Der Fix berührt nur `OperationConfigsController` Add/Update (zwei zusätzliche
Frühausstiege vor jedem Schreiben), zwei `required`-Attribute, B1a, eine History-Bedingung, einen Test und TS-87.9.
Defektklassen: kein Script-Embedding, kein `hidden`+`d-*`, keine Parallel-Arrays; stille Fallbacks/Browser-only → N-3;
eine Regel an mehreren Stellen → Längen 100/200 jetzt in Mapping, View und Controller gleich.

**Geänderte Bestandstests:** `OperationConfigsMigrationSqlTests` Namens-Test (`:45`) — Zusicherung **verschärft**
(Wortgrenze statt Teilstring), Anlass M-3, keine Fixture angepasst; neuer Test `Sql95_KonvergenzReihenfolge_UndHaltNachAnomalie`.
Kein Fall „sonstig“.

**Beweisart (Änderung gegenüber Tabelle 3):** AK 22 bleibt **Manual-UAT TS-87.9** (nicht grün gezählt); die
Halt-Reihenfolge ist jetzt textlich im Drift-Wächter belegt, die SQL-Server-Semantik von `NOEXEC` nur auf der DB-Kopie
prüfbar. Alle übrigen Zeilen unverändert.

**Deploy:** unverändert `web: true`, `service: false`, `migration: true` (Fix fügt keine Migrationsdatei hinzu); der
Reihenfolge-Punkt im Deploy-Abschnitt ist auf den neuen Halt angepasst.

**Manuelle Checkliste — es gilt Abschnitt 6 oben, mit diesen Präzisierungen:**
1. Punkt 4 (Anomalie, Wegwerf-Kopie mit beiden Tabellen): `SQL/95` in SSMS **vollständig** ausführen. Erwartet: B1-Meldung
   „… existieren beide …“ und „ABBRUCH: …“; mögliche Folgemeldungen „Ungültiger Spaltenname“ aus B8 sind Kompilierfehler
   ohne Wirkung (N-1). Danach in einem **neuen** Abfragefenster:
   `SELECT * FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20261012140548_RenameOperationConfigsAndAddAudit'` → 0 Zeilen;
   `SELECT name FROM sys.columns WHERE object_id = OBJECT_ID('dbo.OperationConfigs')` → keine Audit-Spalten;
   `SELECT name FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.OperationConfigs')` → kein neuer
   `IX_OperationConfigs_OperationName`; `OseonOperationConfigs` unverändert vorhanden. App-Start auf dieser Kopie →
   Abbruch mit der B1-Meldung.
2. Zusatz zu Punkt 4: dieselbe Kopie mit `sqlcmd -b -i SQL\95_RenameOperationConfigsAndAddAudit.sql` → Exit-Code ≠ 0 nach
   B1, ebenfalls keine History-Zeile.
3. Punkt 3 (Form a/b je 2×): weder im ersten noch im zweiten Lauf darf eine „ABBRUCH“-Zeile erscheinen.
4. Punkt 8 (TS-87.3) statt „derzeit still 0“: Offset-Feld leeren → der Browser verhindert das Speichern
   (Pflichtfeld-Hinweis). Kürzel mit 101 Zeichen über die Entwicklertools (`maxlength` entfernt) anlegen → Warnung
   „Kürzel höchstens 100 …“, kein 500.
5. FreshInstall (c): History enthält `20261012140548_RenameOperationConfigsAndAddAudit` (die alte Tabelle fehlt dort).

### Abschluss Dev-Lauf (2026-10-12)

- N-3 (leerer Offset per manipuliertem POST → 0) als Aufgabe [[2026-10-12-operationconfig-offset-leer-pruefen]] festgehalten, nicht nach der QA ungeprüft nachgebaut.
- N-1/N-2 (Kompiliermeldungen unter NOEXEC im Anomaliefall; Teilausführung in SSMS lässt NOEXEC an) stehen im Deploy-Abschnitt und im Brain-Changelog.
- M-4 erledigt mit dem Brain-Commit (Testindex, Changelog, feature-map).
