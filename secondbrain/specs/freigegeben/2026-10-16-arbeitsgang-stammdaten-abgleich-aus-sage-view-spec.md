---
type: spec
title: "IDEAL: Arbeitsgang-Stammdaten mit der Sage-View vw_IDEAL_AKE_WMS_Arbeitsgaenge abgleichen (eigener Sync)"
slug: 2026-10-16-arbeitsgang-stammdaten-abgleich-aus-sage-view-spec
status: Testbereit
created: 2026-10-16
updated: 2026-10-16
source_backlog: "[[2026-10-16-arbeitsgang-stammdaten-abgleich-aus-sage-view]]"
task: "[[2026-10-16-arbeitsgang-stammdaten-abgleich-aus-sage-view-umsetzung]]"
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
affected_code:
  - "Pfade relativ zum Bündel-Worktree .claude/worktrees/2026-08-07-ideal-teile-1-5 (Stand v1.60.2, gelesen 2026-10-16)"
  - "IDEALAKEWMSService/Services/OperationConfigSyncService.cs + IOperationConfigSyncService.cs — NEU: Abgleich View -> OperationConfigs (nur anlegen, Kennzeichnung 'nicht in Sage'); reine Entscheidung als static Plan(...) im selben File (unit-testbar); Muster ProductionWorkplaceSyncService.cs:46-223"
  - "IDEALAKEWMSService/Services/SageArbeitsplatzReader.cs:16-55 — WIEDERVERWENDET (liest bei View-Quelle genau Arbeitsgangnummer, Bezeichnung, AG, :90-93); nur Log-Text :33 neutral formulieren ('Lauf wird uebersprungen' statt 'Werkbank-Anlage wird uebersprungen'); einzige Aenderung an Werkbank-Code, kein Verhalten, kein Test matcht den alten Text (Grep 'Werkbank-Anlage wird' *.cs: nur :33)"
  - "IDEALAKEWMSService/Services/FaHierarchySql.cs:42 ValidateAndQuote, :116 SourceExistsAsync, :124 SourceMissingMessage — wiederverwendet, unveraendert"
  - "IDEALAKEWMSService/Workers/SyncWorker.cs — NEU Block nach der Werkbank-Anlage (:377-389), EIN Gate Sync:OperationConfigSyncEnabled (Default false), RunResilientAsync (:495-503)"
  - "IDEALAKEWMSService/Program.cs:59/:69 — DI: AddScoped<IOperationConfigSyncService, OperationConfigSyncService> neben IProductionWorkplaceSyncService"
  - "IdealAkeWms/Services/SyncLogger/SyncLogServices.cs:29/:46 — NEU Konstante OperationConfigSync + Eintrag in All"
  - "IdealAkeWms/Models/ServiceSettingDefinitions.cs:43-46 — NEU Sync:OperationConfigSyncEnabled (Bool, false) + Sync:OperationConfigSyncViewName (String, [vw_IDEAL_AKE_WMS_Arbeitsgaenge]) + Sync:OperationConfigSyncFremdsystemRelevant (Bool, false; Antwort 4), Kategorie FA-Hierarchie, standortneutrale Beschreibungen <= 500 Zeichen"
  - "IdealAkeWms/Models/Standort/StandortSettingsCatalog.cs:47 — NEU Feld Sync:OperationConfigSyncViewName in GroupMandantViews (Muster Sync:ProductionWorkplaceSyncViewName); Schalter und Fremdsystem-relevant-Default bleiben in /ServiceSettings (Muster Kommentar :58-59)"
  - "IdealAkeWms/Models/OperationConfig.cs:11-35 — NEU string Source (= OperationConfigSource.Manual, NVARCHAR(20)) + DateTime? SageMissingSince; NEU static class OperationConfigSource { Sage, ExternalSystem, Manual } im selben File (Muster StorageLocationSource.cs:3-7, FaWorkStep.cs:3-9)"
  - "IdealAkeWms/Data/ApplicationDbContext.cs:883-893 — Source HasMaxLength(20).IsRequired().HasDefaultValue(OperationConfigSource.Manual) (Muster :196, ohne Index)"
  - "IdealAkeWms/Migrations/<Timestamp>_AddOperationConfigSourceAndSageMissingSince.cs (+ Designer, Snapshot) — NEU, Timestamp nach 20261012140548; AddColumn Source (Default 'Manual') + AddColumn SageMissingSince + EIN migrationBuilder.Sql (Bestandsbefuellung)"
  - "SQL/96_AddOperationConfigSourceAndSageMissingSince.sql — NEU (COL_LENGTH-Guards, Bestandsbefuellung wortgleich zur Migration, History-Insert eigener Batch; 96 frei: Buendel hoechstes SQL/95, main hoechstes SQL/88, gemessen 2026-10-16)"
  - "SQL/00_FreshInstall.sql:1225-1237 (CREATE TABLE: zwei Spalten), nach :1526 (Seed-Zeilen :1509-1522 -> ExternalSystem, eigener Batch), nach :2441 (MigrationId, nur wenn beide Spalten existieren)"
  - "IdealAkeWms/Program.cs:490-499 — OSEON-Seed setzt Source = OperationConfigSource.ExternalSystem (Bedingung :471 unveraendert)"
  - "IdealAkeWms/Controllers/OperationConfigsController.cs:24-28 (Ctor + IServiceSettingRepository), :34-41 (ColumnMap + 'source'), NEU FormatSource neben FormatOffset :31, :43-68 (Index: NotInSage + MarkerActive), :100-109 (Add: Source = Manual); Update :131-136 unveraendert (setzt nur Name/Offset/Relevanz/Modified*), geschrieben wird ueber das geaenderte Repository.UpdateAsync"
  - "IdealAkeWms/Data/Repositories/OperationConfigRepository.cs:105-109 — UpdateAsync schreibt kuenftig NUR DisplayName, DueDateOffsetDays, IsExternalSystemRelevant, ModifiedAt, ModifiedBy, ModifiedByWindows (Property-IsModified statt _context.OperationConfigs.Update(config) :107); einziger Produktiv-Aufrufer OperationConfigsController.cs:138 ueber CachedOperationConfigRepository.cs:61 (Invalidierung bleibt); Tests :116, :271 aendern nur Offset/Relevanz"
  - "IdealAkeWms/Models/ViewModels/OperationConfigIndexViewModel.cs:6-18 — NEU MarkerActive, NotInSage"
  - "IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs:541-562 — NEU ViewConfig OperationConfigs + Eintrag in GetByViewKey (ADR 0005 Nachtrag 2026-08-12, Muster Beschichtungsauftraege :498-517)"
  - "IdealAkeWms/Views/OperationConfigs/Index.cshtml — Spalte Quelle (th :57-61 + td), Zahnrad-Host direkt vor .table-responsive :53, #view-config/#column-config, column-preferences.js vor table-filter.js :193-194; Hinweis-Alert nach :18-24 (Muster DuplicateNames), Badge in der Kuerzel-Zelle :68-73, colspan :112, Legende :168-187"
  - "IdealAkeWms/Views/Help/Index.cshtml:528-546 (Arbeitsgang-Stammdaten) — Abschnitt Abgleich mit Sage (standortneutral) + Spalte Quelle + Kennzeichnung + Betriebshinweis Loeschen"
  - "IdealAkeWms/Views/Help/Changelog.cshtml, IdealAkeWms/AppVersion.cs:5 + IDEALAKEWMSService/AppVersion.cs — Version (naechste freie, gemessen v1.60.2 -> v1.61.0)"
  - "README.md:766-773 (ServiceSettings-Gruppen: Sage-Quellen + neuer Sync), secondbrain/codebase/services.md (Sync-Tabelle :93-105, Katalog :160-184, SyncLog-Services :287-291)"
  - "Tests: IDEALAKEWMSService.Tests/Services/OperationConfigSyncServiceTests.cs (NEU, Muster ProductionWorkplaceSyncServiceTests.cs:20-110), IdealAkeWms.Tests/Models/ServiceSettingDefinitionsTests.cs:106 (InlineData), IdealAkeWms.Tests/Controllers/OperationConfigsControllerTests.cs (Ctor + Kennzeichnung + Quelle), IdealAkeWms.Tests/Repositories/OperationConfigRepositoryTests.cs (NEU Test UpdateAsync laesst Source/SageMissingSince unveraendert, Muster :105-121, geteilte InMemory-DB ueber TestApplicationDbContextFactory.cs:12-23), IdealAkeWms.Tests/Services/SyncLoggerTests.cs:185 (Muster), IdealAkeWms.Tests/Views/OperationConfigsViewTests.cs (NEU, Muster BeschichtungsauftraegeViewTests.cs:28-60), Migrations-Drift-Test (Muster OperationConfigsMigrationSqlTests.cs:58 JederUpBatch_StehtWortgleichInSql95)"
  - "docs/TESTSZENARIEN.md (neues Kapitel, naechstes freies; gemessen TS-93 :9962 -> TS-94) + secondbrain/tests/testszenarien-index.md"
  - "UNVERAENDERT (Regressionsanker): ProductionWorkplaceSyncService.cs, WorkOperationStructureDetectionService.cs, OperationConfigRepository.cs:43-103 + :111-127 (Lesewege, Lookups, Nicht-konfiguriert-Liste, Add, Delete, Exists; geaendert nur UpdateAsync :105-109), ArbeitsgangStatusRegel.cs:24-29, Program.cs:471 (Seed-Bedingung 'leere Tabelle')"
open_questions:
  - "1 ERLEDIGT (Gerald 2026-10-16: kein Master-Gate) — Master-Gate ProduktionsauftragHierarchisch zusaetzlich?"
  - "2 ERLEDIGT (Gerald 2026-10-16: loescht selbst per UI, Betriebshinweis 'erst nach dem ersten Lauf') — Reihenfolge Loeschen der 7 AKE-Zeilen"
  - "3 ERLEDIGT (Gerald 2026-10-16: Spalte Quelle; Ausgestaltung mit Antwort 6 bestaetigt) — Migration fuer die Kennzeichnung"
  - "4 ERLEDIGT (Gerald 2026-10-16: ja, standortneutral) — AKE-Einsatz"
  - "5 ERLEDIGT (Gerald 2026-10-16: Ja) — Wechsel Quelle Manuell -> Sage: Wenn ein manuell angelegtes Kuerzel spaeter in der Sage-View erscheint, setzt der Abgleich Source = Sage (einzige erlaubte Aenderung an bestehenden Zeilen, Audit-Modified 'OperationConfigSync', Info im Protokoll)? Empfehlung: ja."
  - "6 ERLEDIGT (Gerald 2026-10-16: Ja) — Ausgestaltung Quelle bestaetigen (Nachtrag 3 'von Gerald zu bestaetigen'): Spalte Source (englisch, Werte Sage | ExternalSystem | Manual; UI 'Quelle' mit Sage | Fremdsystem | Manuell) statt OperationConfigs.Quelle mit deutschen Werten (Sprachregel CLAUDE.md), und SageMissingSince bleibt zusaetzlich (Zustand 'nicht in Sage seit'). Empfehlung: ja."
  - "7 ERLEDIGT (Gerald 2026-10-16: Ja, Regel so lassen, keine Altzeilen auf Manuell; P5 je Standort vor dem Deploy) — Konflikt Bestandsregel <-> Code: CreatedBy = 'system' ist kein Seed-Beleg — SQL/95 B8 (:65-66) hat ALLEN Zeilen von vor v1.54.0 'system' eingetragen. Folge: praktisch der gesamte Bestand beider Standorte (auch die 7 AKE-Zeilen auf IDEAL) wird Fremdsystem und nie als 'nicht in Sage' gekennzeichnet. Regel trotzdem so (P5 vorher)? Empfehlung: ja."
epic: false
etappen: []
deploy:
  web: true            # QA 2026-10-16 bestaetigt (IdealAkeWms/ geaendert: Controller, View, Hilfe, Katalog, Program.cs-Seed, Model)
  service: true        # QA 2026-10-16 bestaetigt (IDEALAKEWMSService/: neuer OperationConfigSyncService, SyncWorker-Block, DI, AppVersion)
  migration: true      # QA 2026-10-16 bestaetigt (neu: Migrations/20261016090641_AddOperationConfigSourceAndSageMissingSince + SQL/96)
freigabe_entscheidung: ""
freigabe_von: "Gerald Weichbold"
freigabe_am: "2026-10-16"
# Flache Schluessel mit Absicht: Obsidians Property-Editor kann verschachtelte
# YAML-Objekte NICHT bearbeiten - und genau diesen Block fuellt der Mensch aus.
---

> **Umsetzungsort (Vorschlag):** im Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`
> (Zweig `feature/2026-08-07-ideal-teile-1-5`). Grund: `OperationConfig`/`OperationConfigsController` (v1.54.0) und das
> View-Muster (`FaHierarchySql.SourceExistsAsync`, View-Weg im `SageArbeitsplatzReader`, v1.60.x) existieren nur dort —
> `main` hat kein `SQL/9x`-Skript (Glob `SQL/9*_*.sql` auf `main`: 0 Treffer). Alle Code-Belege dieser Spec: Bündel
> **v1.60.2** (`IdealAkeWms/AppVersion.cs:5`). Version, TS-Kapitel, SQL-Nummer: nächste freie zum Umsetzungszeitpunkt
> (gemessen 2026-10-16: v1.60.2, höchstes Kapitel TS-93 `docs/TESTSZENARIEN.md:9962`, höchstes Skript
> `SQL/95_RenameOperationConfigsAndAddAudit.sql`, letzte Migration `20261012140548_RenameOperationConfigsAndAddAudit`).
>
> Benachbart, aber eigenständig: [[2026-10-09-arbeitsgang-stammdaten-vereinheitlichen-spec]] (Tabelle `OperationConfigs`,
> Reiter, Liste „nicht konfiguriert“, Offset-Lookup) — diese Spec baut darauf auf und ändert dort nichts außer der
> Anzeige im Reiter. View-Herkunft und Spalten: [[2026-10-14-ideal-sage-views-umstellung-spec]].

> [!important] Freigabe-Nachtrag 2026-10-16 (Antworten Gerald, geht dem Rumpf vor — Spec muss nachgezogen werden)
> 1. **Kein** Master-Gate; nur `Sync:OperationConfigSyncEnabled`.
> 2. Die 7 AKE-Zeilen auf IDEAL löscht Gerald **selbst per UI**; Betriebshinweis „erst nach dem ersten Lauf“ (Seed
>    `Program.cs:471` bei leerer Tabelle) bleibt in Deploy/Hilfe.
> 3. **Spalte „Quelle“ statt bzw. neben `SageMissingSince`** (Gerald: „Quelle – Sage, Fremdsystem“). Vorschlag Koordination,
>    vom spec-agent auszuarbeiten und von Gerald zu bestätigen:
>    - `OperationConfigs.Quelle` (nvarchar, Werte `Sage` | `Fremdsystem` | `Manuell`), angezeigt als Spalte im Reiter.
>    - Abgleich legt mit `Quelle = Sage` an; Anlegen in der UI = `Manuell`; OSEON-Seed = `Fremdsystem`.
>    - Bestand (Migration): Seed-Zeilen (`CreatedBy = 'system'`) -> `Fremdsystem`, übrige -> `Manuell` (Prüfabfrage vorher).
>    - „nicht in Sage“ wird nur für Zeilen ausgewiesen, die **nicht** `Fremdsystem` sind — damit sind die OSEON-Kürzel bei
>      AKE nicht gekennzeichnet (löst Rückfrage 4b). Ob dafür zusätzlich `SageMissingSince` nötig ist oder die
>      Kennzeichnung aus dem letzten Lauf anders gespeichert wird, begründet der spec-agent.
>    - Ob ein manuell angelegtes Kürzel, das später in Sage erscheint, auf `Sage` wechselt (einzige erlaubte Änderung an
>      bestehenden Zeilen) — Rückfrage an Gerald.
> 4. **AKE-Einsatz ja:** standortneutral; Fremdsystem-relevant für neue Kürzel als Setting (Default false), Kennzeichnung
>    rein informativ, AKE-View mit denselben Spalten (Gerald), Ausschlussliste je Standort prüfen.

> [!note] Rework 2026-10-16 (spec-agent)
> Nachtrag 1–4 ist in Rumpf, `affected_code`, Akzeptanzkriterien, Tests und Deploy eingearbeitet. Neu offen: Rückfragen 5–7
> (Wechsel Manuell → Sage, Bestätigung der Quelle-Ausgestaltung, Konflikt der Bestandsregel mit `SQL/95`). Wo eine Regel von
> Rückfrage 5 abhängt, steht das am Ort. *(Stand 2026-10-16: Rückfragen 5–7 von Gerald beantwortet, alle Weichen aufgelöst.)*

## Ziel / Nutzen (das Warum)

Am IDEAL-Test (2026-10-15) standen 15 IDEAL-Kürzel (BE, EG, EM, IF, ISO, KA, LS, PG, PL, PR, PU, SM, SW, SÄ, VM) unter
„Nicht konfigurierte Kürzel“, die Tabelle enthielt nur 7 AKE-Zeilen (BR, RE, RO, RS, SL, ST, ZB). Die Sage-View
`vw_IDEAL_AKE_WMS_Arbeitsgaenge` ist für IDEAL der Arbeitsgang-Katalog ([[fallstricke]] §16 „Die View … ist der
Arbeitsgang-KATALOG“). Ein eigener Abgleich sorgt dafür, dass jedes Kürzel aus der View in den Arbeitsgang-Stammdaten
steht (auch eines, das noch an keinem FA hängt), und macht Einträge sichtbar, die Sage nicht (mehr) kennt. Der Offset
bleibt Handarbeit; manuelle Pflege wird nie überschrieben. Eine neue Spalte **Quelle** zeigt je Eintrag, woher er stammt
(Sage-Abgleich, Fremdsystem/OSEON-Seed, manuell). Der Abgleich ist **standortneutral** (Antwort 4): AKE kann ihn mit einer
eigenen Sage-View gleicher Spalten einschalten.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Neuer Service `OperationConfigSyncService` mit eigenem Lauf im Aktivitäts-Protokoll (`SyncLogServices.OperationConfigSync`),
  eigener Block im `SyncWorker`, drei neue ServiceSettings (Schalter, View-Name, Fremdsystem-relevant-Default).
- Anlegen fehlender Kürzel in `OperationConfigs` (nur anlegen; einzige Ausnahme an Bestandszeilen: Wechsel Quelle
  Manuell → Sage, Antwort 5).
- Neue Spalte `OperationConfigs.Source` („Quelle“: Sage | Fremdsystem | Manuell) mit Bestandsbefüllung; gesetzt von Abgleich,
  UI-Anlage und OSEON-Seed; als Spalte im Reiter mit Server-Spaltenfilter.
- Kennzeichnung „nicht in Sage“ im Reiter *BDE-Stammdaten › Arbeitsgänge* + Meldung im Protokoll, nur für Quelle ≠
  Fremdsystem; dafür zusätzlich die nullable Spalte `OperationConfigs.SageMissingSince`.
- Spaltenauswahl (Zahnrad) für die Tabelle des Reiters (ADR [[0005-listen-view-pattern-mit-server-side-spaltenfilter]],
  Pflichtbestandteil seit Nachtrag 2026-08-12) — der Reiter hat sie bisher nicht (`Index.cshtml:193-194` lädt nur
  `table-filter.js`; kein `OperationConfigs` in `ColumnDefinitions.GetByViewKey`, `ColumnDefinitions.cs:541-562`).
- View-Name zusätzlich in den Standorteinstellungen (Gruppe „Mandant / Sage-Views“).
- Hilfe, Changelog, README, services.md, TS-Kapitel.

**Out-of-Scope**
- Überschreiben oder Löschen von Einträgen durch den Sync (ausdrücklich nie).
- Werkbank-Anlage (`ProductionWorkplaceSyncService`) und deren Quelle `Sync:ProductionWorkplaceSyncViewName` — unverändert.
  **Einzige Ausnahme:** der Log-Text im gemeinsam genutzten `SageArbeitsplatzReader.cs:33` („— Werkbank-Anlage wird
  uebersprungen.“) wird neutral („— Lauf wird uebersprungen.“), weil der Reader jetzt zwei Läufe bedient. Nur Text, kein
  Verhalten; kein Test prüft den alten Text (Grep `Werkbank-Anlage wird` über `*.cs` im Bündel: einziger Treffer `:33`).
  Die Frage aus dem Befund (87 Werkbänke mit `ArbeitsschrittCode` vs. ~30 View-Zeilen, „Gegenrichtung von P3b“) betrifft
  die Werkbank-Anlage, nicht diesen Abgleich, und bleibt dort.
- OSEON-Verhalten, Offset-Lookups (`OperationConfigRepository.cs:61-77`), Liste „nicht konfiguriert“
  (`OperationConfigRepository.cs:82-97`) — unverändert; die Karte bleibt (Backlog Punkt 6). Im Repository ändert sich
  **nur** `UpdateAsync` (`:105-109`, FA 6 „UI-Bearbeiten“), alle übrigen Methoden bleiben.
- Das Löschen der 7 AKE-Zeilen auf IDEAL ist **Handarbeit per UI** durch Gerald (Antwort 2), kein Code und kein Skript;
  Betriebshinweis „erst nach dem ersten Abgleich-Lauf“ in Deploy und Hilfe.
- Spaltenfilter auf „nicht in Sage“, Auswertung „automatisch angelegt, Offset ungeprüft“ — nicht verlangt.
- Quelle in der UI ändern (kein Eingabefeld; die Quelle setzt nur der Code).
- Die übrigen Listen ohne Spaltenauswahl ([[2026-10-14-spaltenauswahl-pflicht-fehlende-listen]]) — dieser Reiter fällt nicht unter
  deren Kriterium „lädt die Skripte, hat aber keine Spaltenauswahl“ (er lädt `column-preferences.js` gar nicht) und wird hier
  mit erledigt, weil die Tabelle ohnehin eine Spalte bekommt. Die Sammel-Spec dort soll ihn nicht doppelt anfassen.

## Fachliche Anforderungen

### Entschieden (Gerald 2026-10-16, Backlog „Antworten / Befunde“ und Punkt 1, Freigabe-Nachtrag 1–4)

- **E1 Ort:** eigener Sync „Arbeitsgang-Abgleich“ im `SyncWorker`, **nicht** in der Werkbank-Anlage. ServiceSettings:
  Schalter `Sync:OperationConfigSyncEnabled` (Bool, Default `false`) und View-Name `Sync:OperationConfigSyncViewName`
  (String, Default `[vw_IDEAL_AKE_WMS_Arbeitsgaenge]`, abgesichert über `FaHierarchySql.ValidateAndQuote`; fehlende View
  = Warnung + Skip). Default aus → nach dem Deploy an beiden Standorten keine Wirkung; eingeschaltet wird von Hand.
  **Kein** Master-Gate `ProduktionsauftragHierarchisch` (Antwort 1).
- **E5 Standortneutral (Antwort 4):** AKE darf den Abgleich einschalten — mit einer eigenen Sage-View derselben Spalten
  `Arbeitsgangnummer, Bezeichnung, AG`, eingetragen in `Sync:OperationConfigSyncViewName`. Fremdsystem-relevant für neu
  angelegte Kürzel kommt aus dem dritten Key `Sync:OperationConfigSyncFremdsystemRelevant` (Bool, Default `false`; AKE stellt
  ggf. `true` ein). Die Kennzeichnung „nicht in Sage“ ist rein informativ (kein Löschen, keine Mail). Die Ausschlussliste
  `Sync:ProductionWorkplaceSyncAusschlussliste` gilt je Standort-DB und ist vor dem Einschalten zu prüfen.
- **E6 Quelle (Antwort 3 / Nachtrag 3, Ausgestaltung siehe FA 6, bestätigt mit Antwort 6):** Spalte „Quelle“ mit Sage |
  Fremdsystem | Manuell. Abgleich → Sage, UI-Anlage → Manuell, OSEON-Seed → Fremdsystem. „Nicht in Sage“ nur für Quelle ≠
  Fremdsystem.
- **E2 Automatisch anlegen** in `OperationConfigs` (Frage 1).
- **E3 Anzeigename = `Bezeichnung`** beim Anlegen. Datenbefund IDEAL-Test: je `AG` genau 1 Arbeitsplatz und 1
  Bezeichnung. Die Regel „nur bei eindeutiger Bezeichnung, sonst leer + Meldung“ bleibt als Absicherung (Frage 2).
- **E4 Die 7 AKE-Zeilen (BR, RE, RO, RS, SL, ST, ZB) werden auf IDEAL gelöscht** (Frage 3). `SL` und `ST` stehen auch in
  der IDEAL-View (SL = „Schweißen Laser“, ST = „Stanzen“) und sollen danach mit den IDEAL-Namen neu entstehen.
  Gerald löscht per UI, **erst nach dem ersten Abgleich-Lauf** (Antwort 2): Bei leerer Tabelle legt der Seed `Program.cs:471`
  beim nächsten Web-Start 12 OSEON-Zeilen an (`:473-487`, u. a. `SL` „Schlosser“ +5), die der Abgleich nie überschreibt.

Hinweis zum Bestand: Die 7 Zeilen entsprechen nicht dem Code-Seed (`Program.cs:473-487` seedet 12 Kürzel, darunter kein
`BR`); woher sie stammen, ist für diese Spec unerheblich.

### FA 1 — Quelle lesen

- Spalten der View laut `sys.columns` (Gerald 2026-10-15): **`Arbeitsgangnummer`, `Bezeichnung`, `AG`** (varchar). Keine
  anderen Spaltennamen ([[fallstricke]] §16 „Spaltennamen einer Sage-View nie aus einer Antwort ableiten“).
- **Wiederverwendung statt neuem Reader:** `ISageArbeitsplatzReader.GetActiveAsync(quotedSource, mandant)` liest bei jeder
  Quelle außer `KHKPpsArbeitsplaetze` genau `SELECT Arbeitsgangnummer, Bezeichnung, AG FROM {quotedSource}`
  (`SageArbeitsplatzReader.cs:90-93`) und prüft vorher `FaHierarchySql.SourceExistsAsync` (`:31-35`). DTO-Zuordnung:
  `Bezeichnung1` = `Bezeichnung`, `Arbeitsschritt` = `AG` (`:43-47`). Als `mandant` wird `Sync:ProductionWorkplaceSyncMandant`
  übergeben (wirkt nur, falls jemand die Tabelle als Quelle einstellt, `:37-38`; bei der View ohne Wirkung, `:50-53`).
- View-Name: `ServiceSettings.GetValueSafeAsync("Sync:OperationConfigSyncViewName")`, leer/null → Konstante
  `DefaultViewName = "[vw_IDEAL_AKE_WMS_Arbeitsgaenge]"` (= Katalog-Default; Test vergleicht beide, Muster
  `ProductionWorkplaceSyncServiceTests.cs:58-59`), dann `FaHierarchySql.ValidateAndQuote`.

### FA 2 — Abgleich (nur anlegen)

Schlüssel = `AG` **getrimmt**, Vergleich **`StringComparer.OrdinalIgnoreCase`** gegen `OperationConfig.OperationName.Trim()` —
dieselbe Regel wie `ExistsAsync` (`OperationConfigRepository.cs:122-127`), `GetDueDateOffsetsAsync` (`:67-77`) und die Liste
„nicht konfiguriert“ (`:84-85`). Damit laufen „angelegt“, „Offset gefunden“ und „konfiguriert“ nie auseinander, unabhängig
von der DB-Kollation.

Je Kürzel der View (gruppiert nach getrimmtem `AG`, `OrdinalIgnoreCase`):
1. `AG` leer → `uebersprungen`.
2. `AG` auf der Ausschlussliste → `uebersprungen`, still (siehe FA 3).
3. `AG` länger als 100 Zeichen (Spalte `OperationName` NVARCHAR(100), `ApplicationDbContext.cs:887`) → `uebersprungen` +
   Warnung (kein Insert, der am SQL Server scheiterte).
4. Kürzel existiert schon (getrimmt, case-insensitiv) → `vorhanden`; **keine** Änderung an `DisplayName`,
   `DueDateOffsetDays`, `IsExternalSystemRelevant` oder Audit-Feldern (E2/Backlog Punkt 3). Einzige Ausnahme
   (Antwort 5): `Source == Manual` → `Source = Sage` + Audit-Modified (FA 6, Count `quelle_sage`). `Source ==
   ExternalSystem` bleibt immer unverändert, auch wenn das Kürzel in Sage steht (z. B. `SL`/`ST` auf IDEAL).
5. Sonst **neu anlegen** mit:
   - `OperationName` = getrimmtes `AG`. Bei Schreibvarianten in der View (`ka`/`KA`) die ordinal kleinste Schreibweise
     (deterministisch) + eine Warnung „Kürzel in Sage in mehreren Schreibweisen: …“.
   - `DisplayName` = die getrimmte `Bezeichnung`, wenn alle nicht-leeren Bezeichnungen der Gruppe ordinal gleich sind;
     keine → `null`; mehrere → `null` + Warnung „Kürzel X: mehrere Bezeichnungen (A / B) — Anzeigename leer gelassen“
     (`name_mehrdeutig`). Bezeichnung > 200 Zeichen (`ApplicationDbContext.cs:888`) → `null` + Warnung.
   - `DueDateOffsetDays = 0` — das Model hat keinen Initialisierer, Default `int` = 0 (`OperationConfig.cs:28`); explizit
     setzen, damit es lesbar ist.
   - ~~`IsExternalSystemRelevant = false` — explizit~~ *(überholt durch Antwort 4)* → `IsExternalSystemRelevant` = Wert von
     `Sync:OperationConfigSyncFremdsystemRelevant` (`ServiceSettings.GetBoolSafeAsync(config, key, false, ct)`,
     `IDEALAKEWMSService/Common/ServiceSettings.cs:50`; einmal je Lauf gelesen), **immer explizit gesetzt**, denn der
     Model-Default ist `true` (`OperationConfig.cs:34`). Wirkung nur in OSEON-Teileverfolgung/-Reporting
     (`OperationConfig.cs:30-33`, Legende `Index.cshtml:184-186`): auf IDEAL ohne Folgen (Backlog Punkt 2), auf AKE steuert
     der Key, ob neue Sage-Kürzel in der OSEON-Ampel zählen.
   - `Source = OperationConfigSource.Sage`.
   - `SageMissingSince = null`.
   - Audit siehe Abschnitt Audit-Feld-Auswirkungen.
6. Jeder Insert mit eigenem `SaveChangesAsync`; `DbUpdateException` (Unique-Index `IX_OperationConfigs_OperationName`,
   z. B. parallele Handanlage oder akzent-insensitive Kollation, Prüfabfrage P3) → Entity abhängen, Warnung, `fehler`,
   weiter mit dem nächsten Kürzel. Muster `ProductionWorkplaceSyncService.cs:142-159`.
7. **Änderungen an Bestandszeilen** (Kennzeichnen, Zurücksetzen FA 4, Quelle-Wechsel FA 6) ebenfalls **je Eintrag** mit eigenem
   `SaveChangesAsync`. Wurde der Eintrag zwischen Laden und Speichern gelöscht (Löschen im Reiter während des Laufs),
   wirft EF `DbUpdateConcurrencyException` (0 Zeilen betroffen): abfangen → Entity abhängen (`Entry(e).State = Detached`),
   Warnung „Eintrag X inzwischen gelöscht — übersprungen“, `fehler` +1, **keine Mail**, weiter mit dem nächsten Eintrag; der
   Lauf endet mit `FinishSuccess`. Grund: ohne Catch würde daraus `FinishFailed` + Fehlermail über `RunResilientAsync`
   (`SyncWorker.cs:495-503`) — die Kennzeichnung soll nie eine Mail auslösen (Antwort 4). Andere `DbUpdateException` hier
   ebenso behandeln wie bei Inserts (FA 2.6).

**Folge (gewollt, zur Kenntnis):** Ein automatisch angelegtes Kürzel steht mit Offset 0 in den Stammdaten. Rechnerisch ändert
sich nichts — ohne Eintrag gilt ebenfalls 0 (`ArbeitsgangStatusRegel.cs:24-29`) —, aber das Kürzel verschwindet aus
„Nicht konfigurierte Kürzel“ im Reiter und aus dem Hinweis „nicht konfiguriert“ im FA-Baum (`FaArbeitsgangStatus.cs:83-84`).
Das entspricht der Begründung zu Frage 1 („Offset bleibt Handarbeit, aber kein Kürzel fehlt mehr“).

### FA 3 — Ausschlussliste

`Sync:ProductionWorkplaceSyncAusschlussliste` (Default `STO,XXX,x01,BS,BS2,FRE,AKE`) bezieht sich auf **Arbeitsschritt-Kürzel**
— im Code das `AG`/`USER_ArbeitsSchritt` der Quelle: Prüfung `ausschluss.Contains(code)` mit `code = row.Arbeitsschritt.Trim()`
(`ProductionWorkplaceSyncService.cs:53-55, :124-126`), dieselbe Liste in der Struktur-Erkennung
(`WorkOperationStructureDetectionService.cs:63-65`); laut Katalog „gelten als BEKANNT (keine Unbekannt-Meldung)“
(`ServiceSettingDefinitions.cs:46`). Für diesen Abgleich daher:
- ausgeschlossene Kürzel werden **nicht angelegt** und **nicht** als „nicht in Sage“ gekennzeichnet oder gemeldet;
- Parsing wie beim Geschwister: Trenner `,`/`;`, getrimmt, leer entfernt, `OrdinalIgnoreCase`.
- **Abweichung vom Geschwister (bewusst):** liefert der Settings-Read `null` (Key fehlt oder transienter DB-Fehler,
  `ServiceSettings.cs:72-77`), gilt der **Katalog-Default**, nicht die leere Liste wie in `ProductionWorkplaceSyncService.cs:53`.
  Grund: dieser Sync löscht nie — ein einmal fälschlich angelegtes `STO` bliebe dauerhaft. Ein bewusst leer gesetzter Wert
  (`""`) bleibt leer. Nebeneffekt: Die Ausschlussliste ist so unter InMemory testbar (Default greift).

### FA 4 — Kennzeichnung „nicht in Sage“

**Speicherort (entschieden mit Antwort 3, Ausgestaltung bestätigt mit Antwort 6):** Quelle in `OperationConfig.Source` (FA 6) **plus**
nullable Spalte `OperationConfig.SageMissingSince` (`DateTime?`), beide geschrieben nur vom Service bzw. bei der Anlage, gelesen
vom Web. Die folgende Variantentabelle stammt aus der ersten Fassung; ihre Begründung für einen gespeicherten Zustand gilt
weiter, ergänzt um die Abwägung „Quelle allein“ darunter.

| Variante | Bewertung |
|---|---|
| **Gewählt: Service schreibt `SageMissingSince`** | Ein Zeitstempel statt Bool, selbstheilend (NULL beim Wiederauftauchen) — Präzedenz `ProductionOrder.SageMissingSince` ([[fallstricke]] §9 „`SageMissingSince` ist ein Zeitstempel“). Web braucht keinen Sage-Zugriff. Detailzeilen dedupliziert sich über die Spalte (nur Übergänge werden gemeldet). Kosten: eine additive Migration. |
| Web liest die View selbst | Die Web-App hat **keine** Sage-Verbindung: `appsettings.json:2-5` kennt nur `DefaultConnection`, `OseonConnection`, `EnaioDmsConnection`; `GetConnectionString` im Web nur für WMS und OSEON (`Program.cs:21,26`, `BomRepository.cs:18`). Neuer Connection-String + Raw-SQL-Pfad im Web + Sage-Recht für den App-Pool-Account — verworfen. |
| Web leitet aus `ProductionWorkplace.ArbeitsschrittCode` ab (ohne Migration) | Werkbänke sind nicht die View: 87 Werkbänke mit Kürzel vs. ~30 View-Zeilen (Befund Gerald 2026-10-15), Werkbank-Quelle ist auf IDEAL ggf. noch die Tabelle (`ServiceSettingDefinitions.cs:45` Default `KHKPpsArbeitsplaetze`). Wäre eine Ableitung aus einem Namen — verworfen. |
| Stand im Aktivitäts-Protokoll / in `ServiceSettings` / `AppSettings` | Protokoll: Detailzeilen gecappt und nach 180 Tagen bereinigt (ADR [[0010-aktivitaets-protokoll-mit-isolierten-dbcontexts]]), parsen wäre brüchig. Settings-Tabellen sind Konfiguration, kein Zustand (ADR [[0008-servicesettings-db-first-mit-typisiertem-katalog]], [[0011-feature-toggles-ueber-appsettings]]). Verworfen. |
| Keine Kennzeichnung, nur Protokoll | Ohne Migration; erfüllt aber Backlog Punkt 4 („im Reiter kennzeichnen“) nicht. ~~Fallback, falls Rückfrage 3 = nein.~~ *(überholt: Antwort 3 = Spalte)* |

**Braucht es `SageMissingSince` neben der Quelle? — Ja (eine Spalte mehr, begründet):**

| Variante | Bewertung |
|---|---|
| **Gewählt: `Source` + `SageMissingSince`** | `Source` ist die **Herkunft** (ändert sich höchstens einmal, Manuell → Sage), `SageMissingSince` der **Zustand** des letzten Laufs. Deckt alle Fälle ohne Sage-Verbindung im Web: manuelles Kürzel nicht in Sage, **Sage-Kürzel, das später aus der View verschwindet**, Wiederauftauchen (selbstheilend), Meldung nur bei Übergängen (keine Warnung je Lauf). Unabhängig von der Antwort auf Rückfrage 5. |
| Nur `Source`, „nicht in Sage“ = `Source == Manual` | Spart eine Spalte, aber (a) ein aus Sage verschwundenes Kürzel (`Source == Sage`) wäre nie gekennzeichnet — die Quelle ist Herkunft, kein Zustand, und darf nicht auf „Manuell“ zurückfallen; (b) nur korrekt, wenn Rückfrage 5 = ja (sonst trüge ein manuelles Kürzel, das in Sage steht, dauerhaft das Badge); (c) jede Meldung käme in jedem Lauf, weil kein Übergang erkennbar ist. Verworfen. |
| Vierter Quelle-Wert „SageEntfernt“ | Mischt Herkunft und Zustand in einer Spalte, verliert „seit wann“ und die Rückkehr zur Herkunft (Manuell oder Sage?). Verworfen. |

**Regeln im Service** (nur nach erfolgreichem, nicht leerem Read):
- Menge `inSage` = alle getrimmten `AG` der View (vor Ausschluss, `OrdinalIgnoreCase`).
- **Kennzeichnungs-Kandidaten** = Einträge mit `Source != ExternalSystem` und Kürzel nicht auf der Ausschlussliste.
- Je Kandidat: Kürzel ∉ `inSage` und `SageMissingSince == null` → `SageMissingSince = now`,
  Audit-Modified setzen, in die Warnung „neu nicht in Sage: …“ aufnehmen. Schon gesetzt → unverändert (Zeitstempel bleibt,
  keine erneute Meldung).
- Kürzel ∈ `inSage` und `SageMissingSince != null` → `null`, Audit-Modified, Info „wieder in Sage: …“ (`wieder_in_sage`).
- Count `nicht_in_sage` = Anzahl aktuell gekennzeichneter Einträge (jeder Lauf) — so steht der Stand ohne Detailzeilen-Flut
  in jedem Lauf.
- Ausschluss-Kürzel **und** Einträge mit `Source == ExternalSystem`: Kennzeichnung nie setzen; eine vorhandene wird
  zurückgesetzt (z. B. nachträglich auf die Liste genommen) — ohne „wieder in Sage“-Meldung, Audit-Modified wie oben.
  Damit tragen die OSEON-Kürzel bei AKE nie das Badge (Nachtrag 3, löst Rückfrage 4b).
- Zuerst der Quelle-Wechsel (FA 6, Antwort 5), dann die Kennzeichnung — ein gewechselter Eintrag ist ∈ `inSage`.
- Speichern je Eintrag mit Concurrency-Catch (FA 2.7): ein inzwischen gelöschter Eintrag wird übersprungen und als `fehler`
  gezählt, nicht als `nicht_in_sage`.

**Anzeige im Web** (Reiter Arbeitsgänge):
- Nur wenn `Sync:OperationConfigSyncEnabled` = `"true"` (über `IServiceSettingRepository.GetValueAsync`,
  `IServiceSettingRepository.cs:9`; Bool-Normalisierung `"true"`/`"false"` laut ADR 0008). Schalter aus → keine
  Kennzeichnung, auch wenn alte Zeitstempel stehen (AKE-Regression, und kein veralteter Stand nach dem Abschalten).
- Gekennzeichnet ist ein Eintrag genau dann, wenn `MarkerActive && SageMissingSince != null && Source !=
  ExternalSystem` — die Quelle-Bedingung steht auch im Web, damit ein Fremdsystem-Eintrag selbst mit einem (z. B. per SQL)
  gesetzten Zeitstempel kein Badge trägt. Eine Regel, eine Stelle: `static bool IsNotInSage(OperationConfig c)` im Controller,
  genutzt von `NotInSage` und der Zelle.
- Badge in der Kürzel-Zelle hinter `<strong>@c.OperationName</strong>` (`Index.cshtml:69`): Text **„nicht in Sage“**,
  `badge text-bg-warning` (schwarz auf Gelb, im Bestand vielfach genutzt), `title="Nicht in Sage seit dd.MM.yyyy HH:mm
  (Arbeitsgang-Abgleich)"`. Bedeutung trägt der Text, nicht die Farbe (WCAG AA).
- Weil die Tabelle paginiert ist: Hinweis-Alert oberhalb der Karte „Nicht konfigurierte Kürzel“ (Muster `DuplicateNames`,
  `Index.cshtml:18-24`, oberhalb der Tabelle wegen [[fallstricke]] §19): „**Nicht in Sage:** BR, RE, … — diese Einträge
  stehen nicht in der Sage-View (Arbeitsgang-Abgleich). Sie bleiben erhalten; bitte prüfen und ggf. löschen.“ Liste aus
  `all` (alle Zeilen, nicht nur die Seite), keine zweite filterbare Tabelle ([[fallstricke]] §3).
- Legende (`Index.cshtml:168-187`) um „nicht in Sage“ (nur Quelle Sage/Manuell, nie Fremdsystem) und „Quelle“ (drei Werte,
  wer sie setzt) ergänzen.
- Spaltenfilter: `ColumnMap["name"]` bleibt `OperationName` (`OperationConfigsController.cs:37`). Das Badge ist Zusatz, kein
  Zelltext für den Filter — sonst bräche die exakte Suche `=KA`. Bewusste, dokumentierte Ausnahme von „Getter = gerenderter
  Text“ ([[fallstricke]] §3 „Universal-Filter-Pattern“).
- **Spalte „Quelle“** (immer sichtbar, unabhängig vom Schalter — Herkunft ist Stammdatum, kein Abgleich-Zustand): `<th
  data-filterable data-col-key="source">Quelle</th>` zwischen „Fremdsystem-relevant“ und der Aktionsspalte; Zelle reiner
  Text `FormatSource(c.Source)` (`Sage` → „Sage“, `ExternalSystem` → „Fremdsystem“, `Manual` → „Manuell“, unbekannter Wert →
  der Rohwert, nicht still „Manuell“). `ColumnMap["source"] = c => FormatSource(c.Source)` — Getter = gerenderter Text, damit
  `=Manuell` und `!Fremdsystem` wie angezeigt treffen. Textspalte ⇒ kein Eintrag im Register `ColumnFilterTypes` (nur Zahl/Datum,
  ADR 0005 Nachtrag 2026-10-13). Muster für die Darstellung: `StorageLocationsController.cs:37`
  (`["source"] = l => l.Source == StorageLocationSource.Sage ? "Sage" : "Manuell"`). `colspan` der Leerzeile (`Index.cshtml:112`)
  5 → 6.
- **Spaltenauswahl (Zahnrad, ADR 0005 Pflicht):** Muster `Views/Beschichtungsauftraege/Index.cshtml:41-44, :98-120` und
  `ColumnDefinitions.cs:498-517`:
  - leerer Toolbar-Host `<div class="d-flex justify-content-end mb-1"></div>` **direkt** vor `.table-responsive`
    (`Index.cshtml:53`; [[fallstricke]] §19) — die Hinweis-Alerts und die Karte „Nicht konfigurierte Kürzel“ bleiben oberhalb;
  - `#view-config` `{ "viewKey": "OperationConfigs", "supportsReorder": true, "supportsSortDefault": false }` (der `data-view-key`
    steht schon, `Index.cshtml:54`);
  - `#column-config` und `ColumnDefinitions.OperationConfigs` mit denselben Keys in derselben Reihenfolge wie die `<th>`:
    `name` (Kürzel, `Locked: true`), `display`, `offset`, `relevant`, `source`, `actions` (Label leer, `Locked: true`, Muster
    `ColumnDefinitions.cs:28`); Eintrag `"OperationConfigs" => OperationConfigs` in `GetByViewKey` — fehlt er, antwortet die
    Präferenz-API mit 400 und speichert still nichts (ADR 0005 Nachtrag 2026-08-12);
  - `column-preferences.js` **vor** `table-filter.js` (`Index.cshtml:193-194`).
  - Aktionsspalte ohne `data-filterable`, darum nicht in `ColumnMap` (wie bisher, `OperationConfigsController.cs:34-41`).
- Das Formular „Neuen Arbeitsgang anlegen“ bekommt **kein** Quelle-Feld.
- **Verzögerung bis 10 Minuten:** `GetAllAsync` ist im Web 10 min gecacht (`CachedOperationConfigRepository.cs:10, :31-39`),
  der Service kann den Web-Cache nicht invalidieren ([[fallstricke]] §9 „Master-Anzeige-Cache lebt im Web“).
  `GetUnconfiguredOperationsAsync` ist ungecacht (`:65`). Nach einem Lauf kann „Nicht konfigurierte Kürzel“ also schon leer
  sein, während die Tabelle die neuen Zeilen erst nach Ablauf zeigt. Akzeptiert, in Hilfe und Testschritten genannt.

### FA 5 — Meldungen im Protokoll (ADR 0010)

Lauf `OperationConfigSync`. Counts (deutsch): `neu`, `vorhanden`, `uebersprungen`, `name_mehrdeutig`, `nicht_in_sage`,
`wieder_in_sage`, `fehler`, `quelle_sage`. Detailzeilen je Lauf höchstens: eine Info
„angelegt: KA (Kanten), …“, eine Warnung „neu nicht in Sage: …“, eine Info „wieder in Sage: …“, eine Info
„Quelle Manuell → Sage: …“, je Mehrdeutigkeit/Schreibvariante/Überlänge/Insert-Fehler/inzwischen gelöschtem Eintrag eine Warnung; Gesamtdeckel 100
Detailzeilen (Rest nur gezählt). `RunResilientAsync`/Mail-Verhalten unverändert: die Kennzeichnung ist rein informativ
(Antwort 4), sie löst nie eine Mail aus.

| Fall | Protokoll | Mail | Mutation |
|---|---|---|---|
| View-Name ungültig | Error + `FinishFailed`, dann `throw` → Fehlermail über `RunResilientAsync` (`SyncWorker.cs:495-503`) | ja (wenn `ErrorNotification:*` an) | keine |
| Quelle fehlt / kein Leserecht | Warnung `SourceMissingMessage` + „Lauf übersprungen“, `FinishSuccess` mit Suffix „Quelle nicht vorhanden“ (Muster `ProductionWorkplaceSyncService.cs:76-89`) | nein | keine |
| Quelle liefert 0 Zeilen | Warnung „Sage-Quelle … liefert keine Zeilen — keine Anlage, keine Kennzeichnung“, Suffix „Quelle leer“ | nein | keine (sonst würden alle Einträge „nicht in Sage“) |
| Sage-Verbindung scheitert | Error + `FinishFailed` + `throw` (Muster `:92-98`) | ja | keine |
| `WorkerSettings:SyncDryRun` | Counts gerechnet, Suffix „[DryRun]“ | — | keine |
| Insert scheitert (Unique-Index, FA 2.6) | Warnung je Kürzel, `fehler` +1, Lauf weiter, `FinishSuccess` | nein | übrige Kürzel normal |
| Eintrag während des Laufs gelöscht (Kennzeichnen/Zurücksetzen/Quelle-Wechsel, FA 2.7) | `DbUpdateConcurrencyException` abgefangen, Entity abgehängt, Warnung „Eintrag X inzwischen gelöscht — übersprungen“, `fehler` +1, Lauf weiter, `FinishSuccess` | nein | übrige Einträge normal |

### FA 6 — Quelle (`OperationConfig.Source`, Antwort 3 / Nachtrag 3)

*Begriff:* „Quelle fehlt/leer“ in FA 5, AK 10/11 und TS-94.8 meint die **Sage-Quelle** (View) des Laufs; „Quelle“ ohne Zusatz
in FA 6, AK 21–25 und im Reiter die **Herkunft** eines Eintrags (`Source`). Meldetexte des Service sagen „Sage-Quelle“.

**Name und Speicherform (bestätigt mit Antwort 6).** Der Nachtrag nennt die Spalte `Quelle` mit den Werten `Sage |
Fremdsystem | Manuell`. Nach der Sprachregel (CLAUDE.md: Code/Spalten englisch, UI deutsch) und dem Hausmuster wird daraus:

| Ebene | Festlegung | Beleg / Muster |
|---|---|---|
| Property / Spalte | `public string Source { get; set; } = OperationConfigSource.Manual;`, `NVARCHAR(20) NOT NULL DEFAULT 'Manual'` | `StorageLocation.cs:36-38` (`[StringLength(20)] [Display(Name = "Quelle")] string Source = StorageLocationSource.Manual`), `ApplicationDbContext.cs:196`, `SQL/55_AddStorageLocationSyncFields.sql:5-9` |
| Werte (DB/Code) | `static class OperationConfigSource { const string Sage = "Sage"; const string ExternalSystem = "ExternalSystem"; const string Manual = "Manual"; }` | `StorageLocationSource.cs:3-7`, `FaWorkStep.cs:3-9` (`FaWorkStepSources`); `ExternalSystem` analog zu `IsExternalSystemRelevant` (UI „Fremdsystem-relevant“, `OperationConfig.cs:30-34`) |
| UI-Text | Spaltenkopf „Quelle“, Werte „Sage“ / „Fremdsystem“ / „Manuell“ über `OperationConfigsController.FormatSource` | `StorageLocationsController.cs:37`, `ArticleCategoriesController.cs:42` |

Warum String-Konstanten statt `enum : byte` (wie `HolidaySource.cs:3-7`, `ApplicationDbContext.cs:715`): Von den fünf
`Source`-Spalten im Modell sind vier `NVARCHAR` mit Klartext (`StorageLocations` `:196`, `FaWorkSteps` `FaWorkStep.cs:29`,
`CachedBomHeaders` `:987`, `ArticleCategories` `ArticleCategoriesController.cs:42`), nur `Holidays` ist ein Byte-Enum. Klartext
macht die Bestandsbefüllung und die Prüfabfragen (P4/P5) ohne Zahlencodes lesbar. Kein Index (Tabelle klein, Filter in C#).
`HasDefaultValue("Manual")` mit gleichem CLR-Initialisierer ist bestehende Praxis (`StorageLocation`), EF sendet den Wert
trotzdem immer mit (Initialisierer ≠ `null`).

**Wer setzt den Wert** — sonst schreibt niemand `Source` (Schreibwege laut Grep `new OperationConfig` / `OperationConfigs.Add`:
`OperationConfigsController.cs:100`, `OperationConfigRepository.cs:101`, `Program.cs:490`, dazu neu der Sync):

| Weg | Wert | Begründung |
|---|---|---|
| Abgleich, Neuanlage (FA 2.5) | `Sage` | Kürzel stammt aus der Sage-View. |
| UI-Anlage `OperationConfigsController.Add` (`:100-109`) | `Manual` | explizit gesetzt (nicht nur über den Initialisierer), damit der Pfad lesbar ist. |
| OSEON-Seed `Program.cs:490-499` (feuert nur bei leerer Tabelle, `:471`) | `ExternalSystem` | Die 12 Kürzel sind laut Kommentar `:470` und Liste `:473-487` OSEON-Arbeitsgänge, nicht aus Sage und nicht von Hand gepflegt; nur so tragen sie bei AKE nie „nicht in Sage“ (Nachtrag 3). Folge auf IDEAL: Feuert der Seed dort versehentlich (leere Tabelle + Web-Start), sind die OSEON-Zeilen **nicht** gekennzeichnet — darum bleibt der Betriebshinweis „erst nach dem ersten Lauf löschen“ (Antwort 2), und TS-94/Testprotokoll prüfen nach dem Recycle mit P4, dass keine Seed-Zeilen entstanden sind. |
| FreshInstall-Seed `SQL/00_FreshInstall.sql:1509-1522` | `ExternalSystem` | dieselben 12 Zeilen; Umsetzung siehe Migrationsabschnitt. |
| UI-Bearbeiten `Update` (`:116-141`) | unverändert | ~~lädt frisch über `FindAsync` …; `Source`/`SageMissingSince` bleiben, wie sie in der DB stehen.~~ *(überholt, Prüfbefund S1: `UpdateAsync` ruft `_context.OperationConfigs.Update(config)` `OperationConfigRepository.cs:107` → EF schreibt **alle** Spalten mit dem Stand beim Laden zurück; ein zwischenzeitlich vom Service gesetztes `SageMissingSince`/`Source` würde still zurückgedreht.)* **Neu:** Der Controller bleibt (lädt über `GetByIdAsync` → `FindAsync`, `OperationConfigRepository.cs:50-53`, ungecacht `CachedOperationConfigRepository.cs:63`; setzt `DisplayName`/Offset/Relevanz/`Modified*`, `:131-136`), aber `UpdateAsync` schreibt nur noch diese sechs Spalten (siehe „Repository-Update“ unten). |

**Repository-Update (Prüfbefund S1, Entscheidung Koordination 2026-10-16, bestätigt Gerald 2026-10-16: umbauen statt nur dokumentieren).**
`OperationConfigRepository.UpdateAsync` (`:105-109`) schreibt künftig **nur** `DisplayName`, `DueDateOffsetDays`,
`IsExternalSystemRelevant`, `ModifiedAt`, `ModifiedBy`, `ModifiedByWindows`. Kein `_context.OperationConfigs.Update(config)`
mehr; stattdessen den Eintrag tracken (falls `Detached`: `Attach`) und genau diese sechs Properties mit
`Entry(config).Property(...).IsModified = true` markieren, alle übrigen (`OperationName`, `Source`, `SageMissingSince`,
`Created*`) mit `IsModified = false`. Die Liste der sechs Namen steht als eine Konstante im Repository (eine Stelle).
- **Aufrufer (Grep `\.UpdateAsync\(` im Bündel, alle `*.cs`):** Produktiv genau einer —
  `OperationConfigsController.cs:138` (`Update`, setzt nur die sechs Felder, `:131-136`) über den Decorator
  `CachedOperationConfigRepository.cs:61` (`await _inner.UpdateAsync(config); InvalidateCache();` — Invalidierung bleibt
  unverändert). Übrige `IOperationConfigRepository`-Nutzer (`BdeApiController.cs:29`, `FaHierarchyController.cs:37`,
  `OseonReportingController.cs:15`, `TrackingController.cs:20`, `OseonGroupViewModelBuilder.cs:11`) rufen `UpdateAsync` nicht
  auf (die beiden `UpdateAsync`-Treffer in `TrackingController.cs:262, :291` gehören zu `_workOperationRepository`). Tests:
  `OperationConfigRepositoryTests.cs:116` und `:271` ändern nur `DueDateOffsetDays`/`IsExternalSystemRelevant` — bleiben grün.
  Der Service nutzt das Repository nicht (eigener `ApplicationDbContext`, Technischer Lösungsentwurf). Damit verliert kein Pfad
  Felder; ein künftiger Aufrufer, der `OperationName` ändern will, muss das Repository bewusst erweitern.
- **Aufgabenteilung:** Das Web schreibt über `UpdateAsync` nur die Pflegefelder; `Source` und `SageMissingSince` schreibt nur der
  Service (Neuanlage, Kennzeichnung, Quelle-Wechsel) bzw. `Add`/Seed bei der Anlage. So kann ein gleichzeitiges Bearbeiten im
  Reiter eine Kennzeichnung nicht mehr zurückdrehen, und der Folgelauf meldet keinen Übergang doppelt.
- Umgekehrte Richtung (Service überschreibt eine gleichzeitige Web-Pflege): der Service schreibt bei Bestandszeilen nur
  `SageMissingSince`/`Source`/`Modified*` (FA 2.4, FA 4) — gemeinsam ist nur `Modified*` („letzter Schreiber“, ADR 0003).

**Wechsel Manuell → Sage (entschieden mit Antwort 5 = ja):** Steht ein Eintrag mit `Source == Manual` in
`inSage`, setzt der Lauf `Source = Sage` und Audit-Modified (`ModifiedAt = DateTime.Now`, `ModifiedBy = ModifiedByWindows =
"OperationConfigSync"`), Count `quelle_sage`, eine Info-Zeile. Sonst keine Feldänderung (`DisplayName`, Offset, Relevanz
bleiben). Es ist die **einzige** Änderung des Abgleichs an bestehenden Zeilen außer der Kennzeichnung. Kein Wechsel in die
Gegenrichtung (Sage → Manuell beim Verschwinden: dafür `SageMissingSince`), kein Wechsel von/zu `ExternalSystem`. DryRun:
gezählt, nicht geschrieben. ~~Bei Antwort 5 = nein entfällt der Wechsel ersatzlos (AK 23 entfällt, Count `quelle_sage` entfällt);
ein manuelles Kürzel, das in Sage steht, ist dann einfach ∈ `inSage` und ungekennzeichnet.~~ *(überholt: Antwort 5 = ja)*

**Bestandsbefüllung (Nachtrag 3, Konflikt mit Antwort 7 entschieden: Regel so lassen, keine Altzeile auf `Manual`):** Regel „`CreatedBy = 'system'` → `ExternalSystem`, übrige →
`Manual`“, als eine idempotente Anweisung:
```sql
UPDATE [dbo].[OperationConfigs] SET [Source] = N'ExternalSystem' WHERE [CreatedBy] = N'system' AND [Source] = N'Manual';
```
(alle Zeilen stehen nach dem `ADD … DEFAULT 'Manual'` auf `Manual`). Belegter Haken: `CreatedBy = 'system'` heißt nicht nur
„Seed“. `SQL/95` B8 (`SQL/95_RenameOperationConfigsAndAddAudit.sql:63-66`, wortgleich Migration
`20261012140548_RenameOperationConfigsAndAddAudit.cs:56-58`) hat **jeder** Zeile, die vor v1.54.0 bestand, `'system'`
eingetragen — die Tabelle hatte bis dahin keine Audit-Felder, auch von Hand angelegte Zeilen nicht. Echte Handanlagen tragen
seit v1.54.0 den Anzeigenamen des Benutzers (`OperationConfigsController.cs:107`), der Sync `OperationConfigSync`. Weil das
Bündel v1.54.0 und dieses Release in **einem** Merge ausliefert, ist auf Produktion beider Standorte praktisch der gesamte
Bestand `'system'` → `ExternalSystem`. Auf IDEAL betrifft das die 7 AKE-Zeilen (BR, RE, RO, RS, SL, ST, ZB): sie tragen danach
**kein** Badge „nicht in Sage“. Die Spec setzt die Regel wie entschieden um und legt die Folge offen; P5 zeigt das Ergebnis
**vor** dem Deploy je Standort.

## Technischer Lösungsentwurf

- **Leiter:** vorhandener Reader (`SageArbeitsplatzReader`), vorhandene Namensprüfung (`FaHierarchySql`), vorhandene Tabelle,
  vorhandener Reiter, vorhandenes Quelle-Muster (`StorageLocationSource`), vorhandenes Zahnrad-Muster; neu sind nur der
  Service, drei Katalog-Einträge, zwei Spalten, eine Tabellenspalte, ein Badge und die Spaltenauswahl. Kein eigener Planner-Typ:
  die Entscheidung steht als `internal static Plan(...)` im Service-File (pure, unit-testbar), die Ausführung daneben —
  Hausmuster „Entscheidung in reinem Helper, Ausführung im Pfad“ ([[services]] „Reine Entscheidungs-Helper“) ohne zusätzliche
  Klasse.
- **Service** `OperationConfigSyncService : IOperationConfigSyncService`, `Task<SyncResult> RunAsync(bool dryRun,
  CancellationToken ct)`; Ctor `(ApplicationDbContext ctx, IConfiguration config, ISageArbeitsplatzReader reader,
  ILogger<OperationConfigSyncService> logger, ISyncLogger syncLogger)` — `ISyncLogger` **letzter** Parameter (ADR 0010,
  [[fallstricke]] §5). `const string ServiceUser = "OperationConfigSync"` (Muster `ProductionWorkplaceSyncService.cs:14`).
  Ablauf wie dort: `BeginRunAsync` → Settings → `ValidateAndQuote` (im try nach `BeginRunAsync`) → Bestand `OperationConfigs`
  laden (tracked) → Read → Leer-Guard → `Plan` → Apply (je Eintrag speichern, FA 2.6/2.7) → `FinishSuccessAsync`. Der Bestand
  wird **vor** dem Sage-Read geladen: kostet nichts (kleine Tabelle) und gibt dem Unit-Test eine deterministische Naht für den
  Fall „Eintrag während des Laufs gelöscht“ (Reader-Mock löscht im Callback, siehe Unit-Tests).
  Speichern über das Change-Tracking (Properties am geladenen Entity setzen, kein `Update()`) — EF schreibt so nur die
  geänderten Spalten. Datenzugriff direkt über `ApplicationDbContext` wie die Geschwister-Syncs im Service
  ([[fallstricke]] §5 „Service referenziert das Web-Projekt“); das Web-Repository (Caching-Decorator) ist im Service nicht
  registriert (`IDEALAKEWMSService`: 0 Treffer für `OperationConfig`).
- **`Plan(rows, existing, ausschluss, sourceSwitch)`** liefert: neue Einträge (Kürzel, Anzeigename), Warnungen, zu
  kennzeichnende und zurückzusetzende Ids, Ids für den Quelle-Wechsel (Antwort 5), Counts. `existing` trägt `Id`,
  `OperationName`, `Source`, `SageMissingSince`. Keine DB, keine Zeit (der Zeitstempel kommt beim Apply); der Relevanz-Wert
  aus `Sync:OperationConfigSyncFremdsystemRelevant` wird erst beim Apply auf die neuen Entities gesetzt. ~~Bei Antwort 5 =
  nein entfällt der Parameter `sourceSwitch` samt Zweig.~~ *(überholt: Antwort 5 = ja — `sourceSwitch` bleibt)*
- **SyncWorker:** neuer Block direkt nach der Werkbank-Anlage (`SyncWorker.cs:377-389`), Gate nur
  `ServiceSettings.GetBoolSafeAsync(_configuration, "Sync:OperationConfigSyncEnabled", false, stoppingToken)`, Schrittname
  „Arbeitsgang-Abgleich (Sage-View)“, eigener Scope, `RunResilientAsync`. Reihenfolge fachlich egal (der Service liest
  `OperationConfigs` sonst nirgends: 0 Treffer in `IDEALAKEWMSService/`). **Kein** Master-Read (Antwort 1).
  ~~bei Antwort 1 = ja zusätzlich der Master-Read wie `:377`~~ *(überholt: Antwort 1 = nein)*.
- **Katalog** (`ServiceSettingDefinitions.cs`, Kategorie „FA-Hierarchie“ neben den Keys `:43-46`; Beschreibungen
  **standortneutral**, Antwort 4 — kein Präfix „IDEAL:“):
  - `Sync:OperationConfigSyncEnabled`, Bool, `"false"`: „Arbeitsgang-Abgleich (beide Standorte) — legt fehlende
    Arbeitsgang-Kürzel aus der Sage-View in den Arbeitsgang-Stammdaten an (Quelle Sage, Offset 0, nie überschreiben/löschen)
    und kennzeichnet Einträge der Quelle Sage/Manuell, die nicht in der View stehen (nur Hinweis). Ausschlussliste wie
    Werkbank-Anlage, je Standort prüfen.“
  - `Sync:OperationConfigSyncViewName`, String, `"[vw_IDEAL_AKE_WMS_Arbeitsgaenge]"`: „Sage-View des Arbeitsgang-Abgleichs
    (Spalten Arbeitsgangnummer, Bezeichnung, AG; Whitelist + QUOTENAME). Default = IDEAL-View; AKE trägt die eigene View
    gleicher Spalten ein. Fehlt die View: Warnung + Skip, keine Mail.“
  - `Sync:OperationConfigSyncFremdsystemRelevant`, Bool, `"false"`: „Wert ‚Fremdsystem-relevant‘ für Kürzel, die der
    Arbeitsgang-Abgleich NEU anlegt (bestehende Einträge nie geändert). Wirkt nur in OSEON-Teileverfolgung/-Reporting:
    IDEAL false, AKE je nach Bedarf true.“ Key-Name wie in Rückfrage 4 vorgeschlagen und mit „Ja“ beantwortet (deutsches
    Wort im Key hat Präzedenz: `Sync:ProductionWorkplaceSyncAusschlussliste`, `:46`).
  - Alle drei Beschreibungen ≤ 500 Zeichen ([[fallstricke]] §16 „Seed-Beschreibung > Spaltenlänge = Startup-Crash“; Test
    `All_FieldsFitServiceSettingsColumns`, `ServiceSettingDefinitionsTests.cs:51`).
- **Seed-Verhalten (`IdealAkeWms/Program.cs:453-468`):** Beim Web-Start wird jeder Katalog-Key angelegt, **wenn er fehlt**,
  mit Default-Wert; bestehende Zeilen bleiben unberührt (`:457`). Für bestehende Installationen heißt das:
  - **AKE und IDEAL** bekommen beim ersten Web-Start nach dem Deploy `Sync:OperationConfigSyncEnabled = false`,
    `Sync:OperationConfigSyncViewName = [vw_IDEAL_AKE_WMS_Arbeitsgaenge]` und `Sync:OperationConfigSyncFremdsystemRelevant =
    false`. Wegen `false` passiert nichts — kein Sage-Read, kein Schreiben, kein Protokoll-Lauf. Will AKE den Abgleich nutzen:
    View-Name auf die AKE-View, Fremdsystem-relevant nach Bedarf, Ausschlussliste prüfen, dann Schalter an.
  - Anders als bei `Sync:ProductionWorkplaceSyncViewName` (Default bewusst die alte Tabelle, weil ein View-Default IDEAL beim
    Deploy umgeschaltet hätte, [[fallstricke]] §16 „Warum der Default die alte Tabelle bleibt“) ist der View-Default hier
    sicher: Der Key ist neu, es gibt kein bisheriges Verhalten, das umschaltet, und der Schalter steht auf aus.
  - Ein später geänderter Katalog-Default wirkt **nicht** auf bestehende DBs (nur fehlende Keys werden angelegt) — Werte
    dann von Hand umstellen.
  - Läuft der Service vor dem ersten Web-Start (Keys fehlen): Gate-Read liefert den Code-Default `false` → Skip. Unkritisch.
  - Der benachbarte OperationConfig-Seed (`Program.cs:470-502`) feuert nur bei **leerer** Tabelle (Betriebshinweis
    Antwort 2) und setzt künftig `Source = ExternalSystem` (FA 6).
- **Standorteinstellungen:** `Sync:OperationConfigSyncViewName` in `StandortSettingsCatalog.Fields` Gruppe
  `GroupMandantViews` mit `ViewNameHint` (Muster `:47`); Schalter und Fremdsystem-relevant-Default nur in `/ServiceSettings`
  (Muster `:58-59`).
- **Web:** `OperationConfigsController` bekommt `IServiceSettingRepository`; `Index` setzt `MarkerActive` und
  `NotInSage` (aus `all` über `IsNotInSage`, sortiert `OrdinalIgnoreCase`). ViewModel um `bool MarkerActive` und
  `List<OperationConfig> NotInSage` erweitert. `ColumnMap` um `source`, `FormatSource` neben `FormatOffset` (`:31`), `Add`
  setzt `Source = OperationConfigSource.Manual`. `ColumnDefinitions.OperationConfigs` + `GetByViewKey` (FA 4 „Spaltenauswahl“). Zugriffsschutz unverändert `[RequireAdminAccess]` (`OperationConfigsController.cs:18`). **frontend-design-Skill
  im Dev-Lauf Pflicht** (View-Änderung); Bootstrap-Bestand, keine neue Bibliothek.
- **Umlaut-Kürzel `SÄ` / Kollation:** Vergleich in C# (`OrdinalIgnoreCase`, `"sä"` = `"SÄ"`); der Unique-Index arbeitet mit der
  DB-Kollation. Akzent-insensitive Kollation (`_AI`) ließe `SÄ` und `SA` kollidieren → Insert-Fehler wird gemeldet (FA 2.6),
  nicht still. `AG` ist varchar: SqlClient wandelt über die Codepage der Spalten-Kollation; ob `Ä` dort korrekt ankommt, zeigt
  P1/P2 (nur Sage). Indiz, kein Beleg: Das Kürzel `SÄ` stand am IDEAL-Test bereits korrekt in „nicht konfiguriert“ (Quelle
  `WorkOperations.OperationNumber`, Backlog).

## Migrations-/SQL-Auswirkungen

*(Antwort 3 = Spalte; Ausgestaltung FA 6, bestätigt mit Antwort 6)*

**SQL-Nummer 96 — belegt frei (gemessen 2026-10-16):** Bündel-Worktree `SQL/9*.sql` = `90_…` bis
`95_RenameOperationConfigsAndAddAudit.sql` (höchste Nummer 95); Hauptcheckout `main` `SQL/*.sql` endet bei
`88_AllowMultipleDummyRequisitionItems.sql`; kein `96_*.sql` unter `.claude/worktrees/**`; außer dieser Spec reserviert keine
Notiz in `secondbrain/` eine `SQL/96`–`99` (Grep `SQL/9[6-9]`). Letzte Migration im Bündel `20261012140548_RenameOperationConfigsAndAddAudit`
(`IdealAkeWms/Migrations/202610*.cs`: nur `20261012073551_…` und `20261012140548_…`). Vor dem Anlegen erneut messen.

- **Model:** `OperationConfig.Source` (`string`, Initialisierer `OperationConfigSource.Manual`) und
  `OperationConfig.SageMissingSince` (`DateTime?`); Mapping `entity.Property(e => e.Source).HasMaxLength(20).IsRequired()
  .HasDefaultValue(OperationConfigSource.Manual);` in `ApplicationDbContext.cs:883-893` (Muster `:196`, ohne Index).
- **Migration** `dotnet ef migrations add AddOperationConfigSourceAndSageMissingSince` (Timestamp nach `20261012140548`).
  Up: `AddColumn<string>("Source", maxLength: 20, nullable: false, defaultValue: "Manual")`,
  `AddColumn<DateTime>("SageMissingSince", nullable: true)`, danach **ein** `migrationBuilder.Sql(...)` mit der
  Bestandsbefüllung (FA 6) — eigener Aufruf = eigener Batch, referenziert die soeben angelegte Spalte (Muster B7/B8 in
  `20261012140548_RenameOperationConfigsAndAddAudit.cs:47-58`). Down: `DropColumn` beider Spalten (EF entfernt die
  Default-Constraint mit). Keine Index-Operation, keine Umbenennung ⇒ strukturierte Operationen zulässig, keine Guard-Pflicht
  aus [[fallstricke]] §9/§24. Prüfen: `has-pending-model-changes` leer; `migrations script` zeigt genau zwei `ADD` und das
  `UPDATE`.
- **`SQL/96_AddOperationConfigSourceAndSageMissingSince.sql`** (Muster `SQL/93_AddProductionWorkplaceSageFields.sql:9-43`,
  `SQL/55_AddStorageLocationSyncFields.sql:5-9`), jede DDL im eigenen Batch:
  1. Guard-Batch: `OBJECT_ID(N'[dbo].[OperationConfigs]', N'U') IS NULL` → `PRINT 'WARNUNG: Tabelle OperationConfigs fehlt —
     zuerst SQL/95'` und `SET NOEXEC ON` (Muster `SQL/95…:22-27`, `SET NOEXEC OFF` am Skriptende) — eine DB vor SQL/95 hat
     noch `OseonOperationConfigs`; ohne Halt würde der History-Batch die Migration als erledigt eintragen.
  2. `IF COL_LENGTH('dbo.OperationConfigs','Source') IS NULL ALTER TABLE [dbo].[OperationConfigs] ADD [Source] NVARCHAR(20)
     NOT NULL CONSTRAINT [DF_OperationConfigs_Source] DEFAULT N'Manual';` (+ `PRINT`).
  3. `IF COL_LENGTH('dbo.OperationConfigs','SageMissingSince') IS NULL ALTER TABLE … ADD [SageMissingSince] DATETIME2 NULL;`
  4. Bestandsbefüllung — **wortgleich** zum `migrationBuilder.Sql` der Migration (Drift-Wächter), idempotent über
     `AND [Source] = N'Manual'` + `PRINT` mit `@@ROWCOUNT`.
  5. History-Insert mit der exakten `MigrationId` (eigener Batch).
  Constraint-Name: Skript benannt (`DF_OperationConfigs_Source`, wie `DF_StorageLocations_Source` in `SQL/55:8`), EF vergibt
  einen Systemnamen. Ein späteres `DROP` muss den Namen daher über `sys.default_constraints` suchen (Muster
  `SQL/65_ReplaceIsFinalShortageWithShortageStatus.sql:51`, `SQL/76_…:26`) — in den Kommentar des Skripts.
- **`SQL/00_FreshInstall.sql`** an **drei** Stellen (zwei Pflichtstellen + Seed):
  1. Schema: `[Source] NVARCHAR(20) NOT NULL CONSTRAINT [DF_OperationConfigs_Source] DEFAULT N'Manual'` und
     `[SageMissingSince] DATETIME2 NULL` in `CREATE TABLE [dbo].[OperationConfigs]` (`:1225-1237`).
  2. Seed: Die 12 OSEON-Zeilen (`:1509-1522`) sollen `ExternalSystem` tragen. Das `INSERT` selbst bleibt unverändert; direkt
     nach dem Seed-Block (`:1526`) ein **eigener** Batch `IF COL_LENGTH(N'dbo.OperationConfigs', N'Source') IS NOT NULL
     EXEC(N'<Bestandsbefüllung>');` — dieselbe Anweisung wie Migration/SQL/96, per `EXEC`, weil der Batch sonst auf einer
     bestehenden Tabelle ohne Spalte schon beim Kompilieren mit „Invalid column name“ scheitert. Präzedenz für `EXEC` in
     Skripten: `SQL/65…:51`, `SQL/76…:26`. Wirkt nur auf Zeilen mit `CreatedBy = 'system'` (Seed/Altbestand), also idempotent.
  3. History-Insert nach `:2441` **nur wenn** `COL_LENGTH('dbo.OperationConfigs','Source') IS NOT NULL AND
     COL_LENGTH('dbo.OperationConfigs','SageMissingSince') IS NOT NULL` — der Tabellenblock legt nichts an, wenn noch die alte
     Tabelle steht (`:1223, :1241-1242`); ein bedingungsloser History-Eintrag ließe EF die Spalten nie anlegen.
- **Daten:** additiv, nicht destruktiv; die Bestandsbefüllung ändert nur die neue Spalte. DB-Backup vor Deploy als Routine
  (Bündel-Deploy enthält SQL/95 mit Schema-Umbenennung). `SQL/AgentJobs/`: nicht betroffen (0 Treffer für `OperationConfigs`).
- **Drift-Wächter:** neuer Test nach Muster `OperationConfigsMigrationSqlTests.cs:58` (`JederUpBatch_StehtWortgleichInSql95`)
  und `:95` (FreshInstall + `MigrationId`): (a) der `migrationBuilder.Sql`-Text steht wortgleich in `SQL/96` und im
  FreshInstall-`EXEC`; (b) `MigrationId` 1× in `SQL/96`, 1× in FreshInstall. Den bestehenden SQL/95-Test **nicht** erweitern.
- ~~Model nur `SageMissingSince`, Migration `AddOperationConfigSageMissingSince`, `SQL/96_AddOperationConfigSageMissingSince.sql`
  mit einer Spalte~~ *(überholt durch Antwort 3: Quelle + SageMissingSince in einer Migration)*.

## Audit-Feld-Auswirkungen

`OperationConfig` erbt `AuditableEntity` (`OperationConfig.cs:11`, ADR [[0003-auditableentity-als-entity-basis]]).
- Anlage: `CreatedAt = DateTime.Now`, `CreatedBy = CreatedByWindows = "OperationConfigSync"` (Service-Name, Muster
  `ProductionWorkplaceSyncService.cs:140`). Der Backlog-Vorschlag „IDEALAKEWMSService“ wird durch das Hausmuster ersetzt
  (Name = Protokoll-Dienst, damit Protokoll und Datensatz zusammenpassen).
- Kennzeichnung setzen/zurücksetzen ist ein Update: `ModifiedAt = DateTime.Now`, `ModifiedBy = ModifiedByWindows =
  "OperationConfigSync"`. Folge: Die letzte manuelle Änderung ist danach nicht mehr in `Modified*` sichtbar (ADR 0003: nur
  „letzter Schreiber“). Akzeptiert; `Modified*` wird im Reiter nicht angezeigt.
- Quelle-Wechsel Manuell → Sage (Antwort 5) ist ebenfalls ein Update mit denselben `Modified*`-Werten.
- Bestehende Einträge ohne Übergang: keinerlei Änderung, auch kein Audit-Update.
- Manuelle Pflege im Reiter unverändert (`OperationConfigsController.cs:100-109, :131-136`); `Add` setzt zusätzlich nur
  `Source = Manual`. `Update` schreibt über das geänderte `OperationConfigRepository.UpdateAsync` nur `DisplayName`, Offset,
  Relevanz und `Modified*` (FA 6 „Repository-Update“) — `Source`, `SageMissingSince`, `OperationName` und `Created*` werden von
  diesem Weg nie geschrieben, auch nicht mit einem veralteten Stand.
- Bestandsbefüllung (Migration/`SQL/96`/FreshInstall) setzt **kein** `Modified*`: Sie ist Schema-Einführung, keine fachliche
  Änderung, und `Modified*` soll den letzten echten Schreiber behalten (Präzedenz: `SQL/95` B8 füllt nur `Created*`,
  `:63-66`). OSEON-Seed `Program.cs:490-499`: Audit wie bisher `"system"`.

## Akzeptanzkriterien

1. `ServiceSettingDefinitions.All` enthält `Sync:OperationConfigSyncEnabled` (Bool, `"false"`),
   `Sync:OperationConfigSyncViewName` (String, `"[vw_IDEAL_AKE_WMS_Arbeitsgaenge]"`) und
   `Sync:OperationConfigSyncFremdsystemRelevant` (Bool, `"false"`); alle drei in `All_ContainsDocumentedServiceReadKey`;
   `All_FieldsFitServiceSettingsColumns` grün; keine der drei Beschreibungen beginnt mit „IDEAL:“.
2. `SyncLogServices.OperationConfigSync = "OperationConfigSync"` steht in `SyncLogServices.All`; der Dienst ist im Filter des
   Aktivitäts-Protokolls auswählbar.
3. Schalter aus (Default): Im Sync-Zyklus wird `IOperationConfigSyncService` nicht aufgelöst, kein Sage-Read, keine Änderung an
   `OperationConfigs`, kein Lauf `OperationConfigSync` im Protokoll.
4. Schalter an, View vorhanden: Jedes getrimmte `AG` der View, das nicht ausgeschlossen ist und nicht (getrimmt,
   case-insensitiv) existiert, steht nach einem Lauf genau einmal in `OperationConfigs` mit Offset 0,
   `IsExternalSystemRelevant` = Wert von `Sync:OperationConfigSyncFremdsystemRelevant` (Default/fehlend → `false`),
   `Source = "Sage"`, `SageMissingSince = null`, `DisplayName` = Bezeichnung, Audit `OperationConfigSync`.
5. Existierende Einträge (auch abweichend in Groß/Klein oder mit Leerzeichen) bleiben in `DisplayName`, `DueDateOffsetDays`,
   `IsExternalSystemRelevant` und Audit-Feldern unverändert; der Sync löscht nie. `Source` bleibt unverändert, außer im Fall
   von AK 23.
6. Kürzel der Ausschlussliste werden weder angelegt noch gekennzeichnet; fehlt der Listen-Wert (`null`), gilt der
   Katalog-Default.
7. Mehrere unterschiedliche Bezeichnungen für ein `AG`: Eintrag wird angelegt, `DisplayName` leer, Warnung mit beiden Namen,
   Count `name_mehrdeutig` = 1.
8. Eintrag mit Quelle Sage oder Manuell nicht in der View → `SageMissingSince` gesetzt, eine Warnung „neu nicht in Sage“ im
   Lauf; im Folgelauf keine erneute Warnung, Zeitstempel unverändert; Count `nicht_in_sage` in jedem Lauf.
9. Eintrag taucht wieder auf → `SageMissingSince = null`, Info „wieder in Sage“, Count `wieder_in_sage`.
10. Quelle fehlt: Warnung „Sage-Quelle … nicht vorhanden oder keine Leseberechtigung — Lauf übersprungen“, Lauf erfolgreich,
    keine Mail, keine Mutation, nachfolgende Sync-Blöcke laufen.
11. Quelle liefert 0 Zeilen: Warnung, keine Anlage, keine Kennzeichnung.
12. Ungültiger View-Name (z. B. `vw;DROP`): Fehler im Protokoll + Fehlermail, kein SQL gegen Sage, übrige Blöcke laufen.
13. DryRun: Counts wie im Echtlauf, keine Änderung an `OperationConfigs`.
14. Insert-Fehler am Unique-Index bricht den Lauf nicht ab: Warnung, Count `fehler`, übrige Kürzel werden angelegt.
15. Reiter Arbeitsgänge bei Schalter an: gekennzeichnete Einträge tragen das Text-Badge „nicht in Sage“ mit Datum im `title`; ein
    Hinweis oberhalb der Tabelle listet alle gekennzeichneten Kürzel (seitenübergreifend). Kontrast Badge ≥ 4,5:1.
16. Reiter bei Schalter aus: weder Badge noch Hinweis, auch wenn `SageMissingSince` gefüllt ist.
17. Spaltenfilter `Kürzel` mit `=KA` trifft weiterhin genau `KA`, auch wenn `KA` gekennzeichnet ist.
18. Migration/SQL: `SQL/96` zweimal hintereinander fehlerfrei (zweiter Lauf: Bestandsbefüllung 0 Zeilen); auf einer DB vor
    SQL/95 hält das Skript mit Warnung an und trägt **keine** History-Zeile ein; FreshInstall legt beide Spalten an, die 12
    Seed-Zeilen tragen `ExternalSystem`, die `MigrationId` steht nur bei vorhandenen Spalten; `has-pending-model-changes` leer;
    Drift-Wächter grün (Befüllungs-Anweisung wortgleich in Migration, `SQL/96`, FreshInstall).
19. AKE-Regression: nach Deploy mit Default-Werten sind Liste „nicht konfiguriert“, OSEON-Teileverfolgung und -Reporting
    unverändert; der Reiter zeigt zusätzlich nur die Spalte „Quelle“ (Bestand „Fremdsystem“) und das Zahnrad, kein Badge, keinen
    Hinweis; die drei neuen Keys stehen mit Default in `/ServiceSettings`.
20. Hilfe (Arbeitsgang-Stammdaten), Anwender-Changelog, README (ServiceSettings-Gruppen), services.md, TS-Kapitel und
    Testindex sind nachgezogen.
21. **Bestandsbefüllung:** Nach der Migration hat jede Zeile mit `CreatedBy = 'system'` `Source = 'ExternalSystem'`, jede andere
    `Source = 'Manual'`; keine Zeile hat `NULL`; `Modified*` unverändert (P5 vorher = P4 nachher).
22. **Quelle je Schreibweg:** UI-Anlage → `Manual`; Abgleich-Neuanlage → `Sage`; OSEON-Seed (`Program.cs`, leere Tabelle) →
    `ExternalSystem`; UI-Bearbeiten ändert `Source` und `SageMissingSince` nicht (auch nicht bei zwischenzeitlicher Änderung
    durch den Service, AK 28).
23. **Quelle-Wechsel Manuell → Sage (Antwort 5):** Eintrag `Source = Manual`, Kürzel in der View → nach dem Lauf `Source = Sage`, `Modified*` =
    `OperationConfigSync`, Info „Quelle Manuell → Sage: …“, Count `quelle_sage`; übrige Felder unverändert; im Folgelauf keine
    erneute Meldung. Einträge `Source = ExternalSystem` wechseln nie, auch wenn das Kürzel in der View steht.
24. **Fremdsystem nie gekennzeichnet:** Eintrag `Source = ExternalSystem`, Kürzel nicht in der View → kein `SageMissingSince`,
    nicht in „neu nicht in Sage“, nicht in `nicht_in_sage`; im Reiter kein Badge, nicht im Hinweis — auch wenn
    `SageMissingSince` per SQL gesetzt wurde (Web-Regel `IsNotInSage`); der Service setzt einen solchen Wert beim nächsten
    Lauf zurück.
25. **Spalte Quelle im Reiter:** zeigt „Sage“ / „Fremdsystem“ / „Manuell“ (unbekannter Wert roh); Server-Spaltenfilter
    `=Manuell` trifft genau die manuellen Einträge, `!Fremdsystem` alle anderen; CSV-Export enthält die Spalte mit dem
    angezeigten Text.
26. **Spaltenauswahl:** Das Zahnrad erscheint rechts über der Tabelle (nicht in einem Hinweis-Alert); Ausblenden/Umordnen von
    „Quelle“ übersteht einen Reload (Präferenz gespeichert, API antwortet nicht 400); „Kürzel“ und Aktionsspalte sind nicht
    ausblendbar. `<th>`-Keys, `#column-config` und `ColumnDefinitions.OperationConfigs` sind gleich (Drift-Test).
27. **Fremdsystem-relevant-Key:** mit `Sync:OperationConfigSyncFremdsystemRelevant = true` legt der Abgleich neue Kürzel mit
    `IsExternalSystemRelevant = true` an; bestehende Einträge bleiben unverändert.
28. **Repository-Update schreibt nur Pflegefelder:** `OperationConfigRepository.UpdateAsync` ändert in der DB ausschließlich
    `DisplayName`, `DueDateOffsetDays`, `IsExternalSystemRelevant`, `ModifiedAt`, `ModifiedBy`, `ModifiedByWindows`. Wurden
    `Source`/`SageMissingSince` zwischen Laden und Speichern in der DB geändert (Service-Lauf), stehen nach dem Speichern die
    **neuen** DB-Werte, nicht die beim Laden gelesenen; ein im Objekt geänderter `Source`/`OperationName` wird nicht geschrieben.
    Der Cache wird nach dem Speichern weiter invalidiert (`CachedOperationConfigRepository.cs:61`); bestehende Tests
    `OperationConfigRepositoryTests.cs:105-121` (`UpdateAsync_ModifiesConfig`) und `:251-276`
    (`Decorator_CachtOffsets_UndInvalidiertNachSchreiben`) bleiben grün.
29. **Eintrag während des Laufs gelöscht:** Scheitert das Speichern einer Kennzeichnung, eines Zurücksetzens oder eines
    Quelle-Wechsels mit `DbUpdateConcurrencyException`, bricht der Lauf nicht ab: Warnung „Eintrag X inzwischen gelöscht —
    übersprungen“, `fehler` +1, übrige Einträge werden verarbeitet, Lauf `FinishSuccess`, **keine** Fehlermail.

## Test-Szenarien

### Unit-Tests (InMemory-tauglich)

`OperationConfigSyncServiceTests` (Reader gemockt, `FakeSyncLogger`, `TestDbContextFactory`; Muster
`ProductionWorkplaceSyncServiceTests.cs:22-37`) und direkte Tests von `Plan(...)`:
- Neuanlage mit Defaults + Audit (AK 4); `DefaultViewName` = Katalog-Default und Reader-Aufruf mit
  `"[vw_IDEAL_AKE_WMS_Arbeitsgaenge]"` (Muster `:48-60`).
- Vorhanden mit `" ka "` → kein Insert, Felder und `Modified*` unverändert (AK 5).
- Ausschluss `STO`/`x01` (Katalog-Default greift, weil der Settings-Read unter Test `null` liefert) (AK 6).
- `SÄ` neu, vorhandenes `sä` → kein Insert (Umlaut + Groß/Klein in C#).
- Zwei Zeilen gleiches `AG`, gleiche Bezeichnung → Name gesetzt; unterschiedliche → leer + Warnung (AK 7, E3).
- Schreibvarianten `ka`/`KA` in der View → ein Eintrag, Warnung.
- `AG` leer / > 100 Zeichen → übersprungen (+ Warnung bei Überlänge).
- Kennzeichnen, nicht erneut melden, zurücksetzen (AK 8, 9); Ausschluss-Eintrag wird nie gekennzeichnet.
- Neuanlage trägt `Source = Sage`, `SageMissingSince = null` (AK 4); Relevanz-Key fehlt → `false`, Key `"true"` (Testwert über
  die Settings-Quelle des Musters) → `true` (AK 27).
- `Source = ExternalSystem` nicht in der View → nie gekennzeichnet; mit vorbelegtem `SageMissingSince` → zurückgesetzt, ohne
  „wieder in Sage“ (AK 24).
- `Source = Sage` verschwindet aus der View → gekennzeichnet; taucht wieder auf → zurückgesetzt (AK 8, 9; der Fall, für den
  `SageMissingSince` bleibt).
- Quelle-Wechsel (Antwort 5): `Manual` + in der View → `Sage`, `Modified*` gesetzt, `quelle_sage` = 1, Folgelauf 0;
  `ExternalSystem` + in der View → unverändert; DryRun → gezählt, nicht geschrieben (AK 23).
- **Eintrag während des Laufs gelöscht (AK 29):** Service-Kontext und ein zweiter Kontext auf **derselben** InMemory-DB über
  `TestApplicationDbContextFactory` (`IdealAkeWms.Tests/Helpers/TestApplicationDbContextFactory.cs:12-23`, vom Service-Testprojekt
  referenziert, `IDEALAKEWMSService.Tests.csproj:33`; `TestDbContextFactory.Create` hängt je Aufruf eine Guid an, `:13`). Bestand:
  `ZZA` (Manual) und `ZZB` (Manual), beide nicht in der View. Der Reader-Mock löscht im Callback `ZZA` über den zweiten Kontext und
  liefert dann eine nicht leere View (der Service hat den Bestand vorher geladen, Technischer Lösungsentwurf). *Erwartet:* keine
  Exception, Warnung „ZZA inzwischen gelöscht“, `fehler` = 1, `ZZB` gekennzeichnet, `nicht_in_sage` = 1, `FinishSuccess`. Stützt
  sich darauf, dass EF-InMemory beim Update einer fehlenden Zeile `DbUpdateConcurrencyException` wirft — im Dev-Lauf Red-Green:
  ohne Catch muss der Test rot sein.
- Quelle fehlt → Warnung, `FinishSuccess`, keine Mutation, keine Exception (AK 10, Muster `:62-81`).
- Quelle leer → keine Kennzeichnung (AK 11).
- DryRun → keine Mutation, Counts gesetzt (AK 13).
- `OperationConfigsControllerTests`: Schalter `"true"` + gesetzter Zeitstempel → `MarkerActive`, `NotInSage` enthält den
  Eintrag; Schalter `"false"` → `NotInSage` leer (AK 15, 16); Schalter `"true"` + Zeitstempel + `Source = ExternalSystem` →
  nicht in `NotInSage` (AK 24); `Add` → `Source = Manual`; `Update` auf einem `Sage`-Eintrag mit Zeitstempel → beide
  unverändert (AK 22); `FormatSource` für die drei Werte + unbekannten Wert, `ColumnMap["source"]` = `FormatSource` (AK 25).
- `OperationConfigRepositoryTests` **„UpdateAsync lässt Source und SageMissingSince unverändert“** (AK 28, InMemory-tauglich):
  zwei Kontexte über `TestApplicationDbContextFactory` (gleiche InMemory-DB). Kontext A: `repo.GetByIdAsync` lädt `KA`
  (`Source = Manual`, `SageMissingSince = null`, tracked). Kontext B: setzt `Source = Sage`, `SageMissingSince = <t>` und speichert.
  Kontext A: Offset/Anzeigename/`Modified*` ändern, zusätzlich `config.Source = ExternalSystem` im Objekt, `UpdateAsync`. Kontext C
  (frisch): Offset/Anzeigename neu, `Source = Sage`, `SageMissingSince = <t>`. Warum InMemory reicht: der InMemory-Provider
  übernimmt beim Update nur Properties mit `IsModified` und behält für die übrigen den gespeicherten Wert — mit dem alten
  `Update(config)` (alle Properties modified) wird der Test rot. Im Dev-Lauf Red-Green nachweisen (erst gegen den alten Code
  rot, dann grün); wäre er wider Erwarten auch alt grün, ist die Aussage nicht InMemory-prüfbar → AK 28 dann in TS-94.5
  (Manual-UAT) prüfen und das im QA-Nachweis vermerken.
- `OperationConfigsViewTests` (Quelltext-Wächter, Muster `BeschichtungsauftraegeViewTests.cs:28-39, :56-60`): `<th>`-Keys =
  `#column-config`-Keys = `ColumnDefinitions.OperationConfigs`; `GetByViewKey("OperationConfigs")` liefert die Definition;
  `ColumnMap.Keys` = Definition ohne `actions`; Zahnrad-Host direkt vor `.table-responsive`; `column-preferences.js` vor
  `table-filter.js` (AK 26).
- Migrations-Drift-Test (Muster `OperationConfigsMigrationSqlTests.cs:58, :95`): Befüllungs-Anweisung wortgleich in Migration,
  `SQL/96` und FreshInstall-`EXEC`; `MigrationId` je 1× (AK 18).
- Seed-Wert: Die Liste in `Program.cs:473-499` ist Top-Level-Code und nicht direkt testbar — der Seed-Wert `ExternalSystem` wird
  in TS-94.13 manuell geprüft (kein eigener Extraktions-Umbau nur für den Test).
- `ServiceSettingDefinitionsTests` InlineData für alle drei Keys (AK 1); `SyncLogServices.All` enthält den Dienst (AK 2, Muster
  `SyncLoggerTests.cs:184-185`).

### Nur Manual-UAT (nicht InMemory)

Sage-Read der View, `OBJECT_ID`-Prüfung, echter Settings-Read (Schalter, Ausschlussliste, View-Name), Unique-Index am
SQL Server, Kollation/Umlaut, Migration/`SQL/96`/FreshInstall, Fehlermail, SyncWorker-Gate mit DB-Wert
([[fallstricke]] §8 „Raw-SQL-Pfade sind nicht InMemory-testbar“, „`SyncWorkerTests` sichern nur die Invariante“; §16 „Was nur
Manual-UAT prüfen kann“). Diese AKs (3, 6 Teil Settings, 10, 12, 14, 18, 19, 21, 22 Teil Seed, 26 Teil Speichern) stehen im
TS-Kapitel ausdrücklich als Manual-UAT. AK 28 zusätzlich als Sichtprobe am SQL Server (TS-94.5, Bearbeiten eines
gekennzeichneten Eintrags). AK 29 nur Unit-Test: Das Zeitfenster (Löschen im Reiter genau zwischen Laden und Speichern eines
Laufs) ist manuell nicht zuverlässig herstellbar.

### Prüfabfragen (vom Menschen auszuführen, Ergebnisse unbelegt bis dahin)

**P1 — Sage, Spalten/Typen/Kollation der View** (IDEAL-Test, später live):
```sql
SELECT c.name, TYPE_NAME(c.user_type_id) AS typ, c.max_length, c.is_nullable, c.collation_name
FROM sys.columns c
WHERE c.object_id = OBJECT_ID(N'dbo.vw_IDEAL_AKE_WMS_Arbeitsgaenge')
ORDER BY c.column_id;
```
Erwartet: genau `Arbeitsgangnummer`, `Bezeichnung`, `AG`; `Bezeichnung`/`AG` varchar. `max_length` von `Bezeichnung` > 200 →
FA 2 (Name leer + Warnung) greift.

**P2 — Sage, Kürzel-Katalog und Umlaut:**
```sql
SELECT LTRIM(RTRIM(AG)) AS AG_getrimmt, COUNT(*) AS Zeilen,
       COUNT(DISTINCT LTRIM(RTRIM(Bezeichnung))) AS Namen,
       MIN(Bezeichnung) AS Name1, MAX(Bezeichnung) AS Name2, MAX(LEN(AG)) AS Laenge
FROM dbo.vw_IDEAL_AKE_WMS_Arbeitsgaenge
GROUP BY LTRIM(RTRIM(AG))
ORDER BY 1;

SELECT AG, CAST(AG AS varbinary(20)) AS Bytes, Bezeichnung
FROM dbo.vw_IDEAL_AKE_WMS_Arbeitsgaenge
WHERE AG LIKE 'S%';
```
Liefert die erwartete Neuanlage-Liste (minus Ausschluss, minus Bestand) und ob `BR`, `RE`, `RO`, `RS`, `ZB` wirklich fehlen.
`SÄ` in CP1252 = Bytes `0x53C4`.

**P3 — WMS-DB IDEAL, Kollation:**
```sql
SELECT DATABASEPROPERTYEX(DB_NAME(), 'Collation') AS DbKollation,
       (SELECT collation_name FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.OperationConfigs') AND name = N'OperationName') AS SpaltenKollation;
```
`_AI` → `SÄ`/`SA` kollidieren im Unique-Index (Warnung, AK 14); `_CS` → C#-Prüfung verhindert Dubletten.

**P4 — WMS-DB IDEAL, Bestand und Einstellungen (vor und nach jedem Testschritt):**
```sql
SELECT Id, OperationName, DisplayName, DueDateOffsetDays, IsExternalSystemRelevant,
       Source, SageMissingSince, CreatedBy, CreatedAt, ModifiedBy, ModifiedAt
FROM dbo.OperationConfigs ORDER BY OperationName;

SELECT [Key], [Value] FROM dbo.ServiceSettings
WHERE [Key] LIKE N'Sync:OperationConfigSync%' OR [Key] = N'Sync:ProductionWorkplaceSyncAusschlussliste';
```
(`Source`/`SageMissingSince` erst nach Migration abfragbar; davor P5.)

**P5 — WMS-DB je Standort (AKE und IDEAL, Test und live), VOR dem Deploy: Vorschau der Bestandsbefüllung (Antwort 7):**

Welche Variante gilt, zeigt `SELECT OBJECT_ID(N'dbo.OperationConfigs', N'U') AS Neu, OBJECT_ID(N'dbo.OseonOperationConfigs', N'U') AS Alt;`
(genau eine der beiden ist nicht `NULL`; beide gesetzt = `SQL/95` B1 bricht ab, `SQL/95…:13-14`).

**P5a — Stand nach `SQL/95` (Testsysteme mit Bündel-Stand v1.54.0+):**
```sql
SELECT OperationName, DisplayName, CreatedBy, CreatedAt, ModifiedBy, ModifiedAt,
       CASE WHEN CreatedBy = N'system' THEN N'ExternalSystem (Fremdsystem)' ELSE N'Manual (Manuell)' END AS QuelleNachMigration
FROM dbo.OperationConfigs
ORDER BY QuelleNachMigration, OperationName;
```

**P5b — Live vor `SQL/95` (alte Tabelle `OseonOperationConfigs` ohne Audit-Spalten, `SQL/34_AddOseonOperationConfig.sql:8-15`):**
```sql
SELECT OperationName, DisplayName, DueDateOffsetDays, IsOseonRelevant
FROM dbo.OseonOperationConfigs
ORDER BY OperationName;
```
Jede Zeile wird `ExternalSystem`: `SQL/95` B8 (`SQL/95_RenameOperationConfigsAndAddAudit.sql:63-66`) setzt im selben Deploy-Fenster
`CreatedBy = 'system'` für **alle** Zeilen, danach greift die Bestandsbefüllung aus `SQL/96`. Ergebnis P5b = Liste der künftigen
Fremdsystem-Einträge.
Erwartet (unbelegt bis zur Ausführung): auf beiden Standorten überwiegend bzw. ausschließlich `ExternalSystem`, weil `SQL/95`
allen Altzeilen `'system'` einträgt. ~~Steht dort eine Zeile, die fachlich manuell gepflegt ist und künftig „nicht in Sage“
gekennzeichnet werden soll, gehört das in die Antwort zu Rückfrage 7 (kein stilles Nachkorrigieren per SQL).~~ *(überholt:
Antwort 7 = ja — Regel bleibt, keine Altzeile wird auf `Manual` gesetzt; P5 ist Vorschau und Protokollnachweis je Standort vor
dem Deploy.)*

### Neues Kapitel `docs/TESTSZENARIEN.md` (nächstes freies, gemessen TS-94) — Skizze

**Vorbedingungen:** Bündel-Stand mit dieser Spec auf dem Testsystem (Web + Service + `SQL/96` bzw. App-Start-Migration); Admin-
Login; Zugriff auf `/ServiceSettings`, Standorteinstellungen, Aktivitäts-Protokoll; P1–P3 ausgeführt, P5 **vor** dem Deploy.

- **TS-94.1 — Deploy mit Defaults (AKE und IDEAL).** Web starten. *Erwartet:* `/ServiceSettings` zeigt
  `Sync:OperationConfigSyncEnabled = false`, `Sync:OperationConfigSyncViewName = [vw_IDEAL_AKE_WMS_Arbeitsgaenge]`,
  `Sync:OperationConfigSyncFremdsystemRelevant = false`; zwei Zyklen abwarten → kein Lauf `OperationConfigSync`; P4 in allen
  Altspalten unverändert, `Source` wie von P5 vorhergesagt, `SageMissingSince` überall leer; Reiter ohne Badge/Hinweis, mit
  Spalte „Quelle“ (AK 3, 16, 19, 21).
- **TS-94.2 — Erster Lauf (IDEAL-Test).** Schalter an. *Erwartet:* Lauf `OperationConfigSync` mit `neu` = Erwartung aus P2,
  Info-Zeile mit angelegten Kürzeln; neue Zeilen Offset 0, Fremdsystem-relevant „Nein“, Quelle „Sage“, Anzeigename aus Sage,
  Audit `OperationConfigSync` (AK 4).
- **TS-94.3 — Bestand bleibt.** Vorher `KA` (oder ein anderes vorhandenes Kürzel) per UI Offset `+2`, Anzeigename „Test“.
  Nächster Lauf. *Erwartet:* Werte unverändert, `vorhanden` zählt es (AK 5).
- **TS-94.4 — Ausschluss.** Ausschlussliste um ein View-Kürzel ergänzen, das noch nicht angelegt ist (oder Eintrag vorher
  löschen). *Erwartet:* nicht angelegt, nicht gekennzeichnet, `uebersprungen` +1 (AK 6). Liste zurückstellen.
- **TS-94.5 — Kennzeichnung.** Eintrag `ZZTEST` per UI anlegen (Quelle „Manuell“, P4). Nächster Lauf + bis 10 min
  (Web-Cache). *Erwartet:* Badge „nicht in Sage“ an `ZZTEST`, Hinweis oben listet ihn, Warnung „neu nicht in Sage: ZZTEST“;
  Folgelauf ohne neue Warnung, Count `nicht_in_sage` stabil (AK 8, 15, 22). Dann `ZZTEST` im Reiter bearbeiten (Offset `+1`,
  speichern). *Erwartet:* Badge bleibt; P4: `SageMissingSince` und `Source` unverändert, nur Offset und `Modified*` neu (AK 28).
  `ZZTEST` löschen.
- **TS-94.6 — Schalter aus.** *Erwartet:* Badge und Hinweis weg (AK 16), kein Lauf; Spalte „Quelle“ bleibt.
- **TS-94.7 — Spaltenfilter.** Kürzel-Filter `=SL` (gekennzeichnet oder nicht). *Erwartet:* genau eine Zeile (AK 17).
- **TS-94.13 — Quelle anzeigen und filtern.** Reiter öffnen. *Erwartet:* Spalte „Quelle“ mit „Sage“ (vom Abgleich), „Manuell“
  (per UI), „Fremdsystem“ (Bestand laut P5); Filter `=Manuell` → nur manuelle; `!Fremdsystem` → alle anderen; CSV-Export
  enthält „Quelle“ mit Klartext (AK 25). Eintrag bearbeiten (Offset ändern) → Quelle unverändert (AK 22).
- **TS-94.14 — Fremdsystem nie gekennzeichnet.** Einen Fremdsystem-Eintrag, dessen Kürzel nicht in der View steht (P2/P4),
  beobachten; nächster Lauf, Reiter erst nach bis zu 10 min prüfen (Web-Cache `GetAllAsync`, FA 4). *Erwartet:* kein Badge,
  nicht im Hinweis, nicht in „neu nicht in Sage“ (AK 24).
- **TS-94.15 — Quelle-Wechsel (Antwort 5).** Ein View-Kürzel, das noch nicht existiert (oder nach Löschen), per
  UI anlegen → Quelle „Manuell“. Nächster Lauf, Reiter erst nach bis zu 10 min prüfen (Web-Cache, FA 4; P4 zeigt den Wechsel
  sofort). *Erwartet:* Quelle „Sage“, Info „Quelle Manuell → Sage: …“, `quelle_sage` = 1,
  Offset/Anzeigename wie per UI eingegeben; Folgelauf ohne Meldung (AK 23). Gegenprobe: Fremdsystem-Eintrag `SL`/`ST`
  (falls noch vorhanden) bleibt „Fremdsystem“.
- **TS-94.16 — Spaltenauswahl.** Zahnrad über der Tabelle rechts; „Quelle“ ausblenden, Reload → bleibt ausgeblendet; wieder
  einblenden, Reihenfolge ändern, Reload → bleibt; „Kürzel“ lässt sich nicht ausblenden (AK 26). Browser-Konsole/Netzwerk:
  kein 400 auf `/api/user-view-preferences/OperationConfigs`.
- **TS-94.17 — Fremdsystem-relevant-Key.** `Sync:OperationConfigSyncFremdsystemRelevant = true`, ein View-Kürzel löschen,
  nächster Lauf. *Erwartet:* neu angelegt mit Fremdsystem-relevant „Ja“; übrige Einträge unverändert (AK 27). Key zurück auf
  `false`.
- **TS-94.18 — Seeds (nur Wegwerf-DB, nie auf Test/Live mit Daten).** (a) `SQL/00_FreshInstall.sql` auf leerer DB → P4: 12
  Zeilen, alle `ExternalSystem`. (b) Alle `OperationConfigs` löschen, Web starten → 12 Seed-Zeilen mit Quelle „Fremdsystem“
  (AK 18, 22).
- **Negativfälle:** **TS-94.8** View-Name `[vw_GibtEsNicht]` → Warnung + Skip, keine Mail, andere Blöcke laufen (AK 10).
  **TS-94.9** View-Name `vw;DROP` → Fehler + Fehlermail, kein Sage-SQL (AK 12). **TS-94.10** `WorkerSettings:SyncDryRun = true`
  → Counts, keine Änderung (AK 13). **TS-94.11** `SQL/96` zweimal → nur `PRINT`, Bestandsbefüllung im zweiten Lauf 0 Zeilen,
  keine Fehler (AK 18). **TS-94.12** Nur falls P3 `_AI` zeigt: Eintrag `SA` per UI anlegen, dann `SÄ` aus der View → Warnung,
  `fehler` +1, Lauf fertig (AK 14). **TS-94.19** `SQL/96` auf einer Wegwerf-DB mit `OseonOperationConfigs` (vor SQL/95) →
  Warnung, keine Spalte, **keine** History-Zeile (AK 18).

### Testprotokoll-Schritte IDEAL-Test (Reihenfolge laut Antwort 2: löschen erst nach dem ersten Lauf)

Die Erwartungen unten setzen die Bestandsregel aus FA 6 voraus (Antwort 7) und richten sich **je Zeile nach dem
P5-Ergebnis** (Prüfbefund S4), nicht nach festen Zahlen — auf dem IDEAL-Test ist `SQL/95` schon gelaufen; eine danach per UI neu
angelegte Zeile ist `Manual`. Regel für jede Bestandszeile:
- P5 `ExternalSystem` → nie gekennzeichnet, `vorhanden`, Quelle „Fremdsystem“.
- P5 `Manual` **und** Kürzel ∉ P2 → nach dem Lauf gekennzeichnet (Badge, Hinweis, Warnung „neu nicht in Sage“).
- P5 `Manual` **und** Kürzel ∈ P2 → `vorhanden`, nicht gekennzeichnet; Quelle wechselt nach AK 23 auf `Sage` (`quelle_sage`).
- `nicht_in_sage` = Anzahl der Zeilen „`Manual` und ∉ P2“ (erwartet 0, wenn P5 alle 7 als `ExternalSystem` zeigt).
Die Kennzeichnung wird zusätzlich mit `ZZTEST` (TS-94.5) geprüft.

| # | Schritt | Erwartet |
|---|---|---|
| 1 | P1, P2 (Sage), P3, P4 und **P5a** (WMS; IDEAL-Test ist nach `SQL/95`) ausführen, Ergebnisse ins Protokoll | P1: drei Spalten. P2: Liste enthält BE, EG, EM, IF, ISO, KA, LS, PG, PL, PR, PU, SM, SW, SÄ, VM sowie SL, ST; P4: 7 Zeilen BR, RE, RO, RS, SL, ST, ZB (Stand 2026-10-15); P5a: Quelle je Zeile (erwartet alle `ExternalSystem`, unbelegt) — **diese Liste ist die Erwartungsbasis für Schritt 3, 4, 6, 7** |
| 2 | Deploy (DB-Backup, `SQL/96` bzw. App-Start-Migration, Web, dann Service), Web-Start | TS-94.1; P4: `Source` = P5a je Zeile, Altspalten unverändert |
| 3 | `Sync:OperationConfigSyncEnabled = true`, einen Zyklus abwarten | Lauf `OperationConfigSync` erfolgreich; jedes P2-Kürzel ohne Bestandszeile angelegt mit Quelle `Sage`, `SÄ` korrekt geschrieben (P4); Bestandszeilen (z. B. SL/ST) `vorhanden`, Name/Offset unverändert; Kennzeichnung je Bestandszeile nach der Regel oben (P5a `ExternalSystem` → nie; `Manual` und ∉ P2 → gekennzeichnet); `nicht_in_sage` = Anzahl „`Manual` und ∉ P2“ laut P5a/P2 |
| 4 | Reiter Arbeitsgänge öffnen (ggf. 10 min Cache abwarten) | „Nicht konfigurierte Kürzel“ ohne die neu angelegten; Spalte „Quelle“: neu angelegte „Sage“, Bestandszeilen wie P5a („Fremdsystem“/„Manuell“); Badge und Hinweis genau an den Zeilen „`Manual` und ∉ P2“ (keine, wenn P5a nur `ExternalSystem` zeigt); Zahnrad über der Tabelle |
| 5 | Gerald löscht die 7 AKE-Zeilen per UI (Antwort 2) | Tabelle hat danach nur IDEAL-Zeilen (nie leer → Seed `Program.cs:471` greift nicht) |
| 6 | Einen Zyklus abwarten | `neu` = Anzahl gelöschter Zeilen, deren Kürzel in P2 steht (erwartet 2: SL „Schweißen Laser“, ST „Stanzen“), mit IDEAL-Namen und Quelle `Sage`; `nicht_in_sage` = Anzahl verbliebener Zeilen „`Manual` und ∉ P2“ (erwartet 0) |
| 7 | Web-App-Pool recyceln, Reiter öffnen | keine OSEON-Seed-Zeilen (B, BG, MS …) aufgetaucht: P4 zeigt keine `ExternalSystem`-Zeile außer P5a-`ExternalSystem`-Zeilen, die in Schritt 5 bewusst nicht gelöscht wurden (erwartet: keine) |
| 8 | Folgelauf | `neu = 0`, keine Warnungen, Counts stabil |
| 9 | TS-94.3 bis TS-94.17, TS-94.8–12 | wie oben |

~~Falls Antwort 2 = „vor dem ersten Abgleich löschen“: Schritt 5 vor Schritt 3, zwischen Löschen und erstem Lauf **kein**
Web-Neustart/App-Pool-Recycle und kein Deploy; Schritt 6 entfällt; nach Schritt 3 mit P4 prüfen, dass keine OSEON-Zeilen
(B, BG, BG-SaP1, MS, A-BT …) stehen.~~ *(überholt: Antwort 2 — Löschen nach dem ersten Lauf)*

~~Schritt 3/4 alt: „BR, RE, RO, RS, ZB gekennzeichnet … Badge an den 5 AKE-Kürzeln; Hinweis listet sie“~~ *(überholt durch
Nachtrag 3 und Antwort 7: Bestand → `ExternalSystem`, nie gekennzeichnet)*

### AKE-Regression (Testsystem AKE oder IDEAL mit Schalter aus)

Vorher P5 auf der AKE-DB (P5a auf einem Testsystem nach `SQL/95`, P5b auf Live vor `SQL/95`). Deploy, Web-Start, zwei Zyklen: TS-94.1; zusätzlich OSEON-Teileverfolgung und -Reporting
stichprobenartig wie vor dem Deploy (TS-16.5, TS-7.10 sinngemäß) — `IsExternalSystemRelevant` und Offsets unverändert (P4
vorher/nachher); Reiter Arbeitsgänge ohne Badge/Hinweis, Spalte „Quelle“ zeigt den Bestand als „Fremdsystem“ (bzw. laut P5);
`/ServiceSettings` zeigt die drei Keys mit Default. Nicht Teil dieses Tests, aber dokumentiert für einen späteren AKE-Einsatz
(Antwort 4): AKE-View mit `Arbeitsgangnummer, Bezeichnung, AG` in `Sync:OperationConfigSyncViewName`, Ausschlussliste der
AKE-DB prüfen, Fremdsystem-relevant-Key nach Bedarf, dann Schalter an — OSEON-Kürzel tragen als `ExternalSystem` kein Badge.

## Etappen (nur bei epic: true)

Entfällt (kein Epic, ein Dev-Lauf). Empfohlene Commit-Reihenfolge im Lauf:
1. Model (`Source`, `OperationConfigSource`, `SageMissingSince`) + Mapping + Migration + `SQL/96` + FreshInstall (3 Stellen) +
   `Program.cs`-Seed + Drift-Test + `OperationConfigRepository.UpdateAsync` (nur Pflegefelder) mit Repository-Test (AK 28).
2. Katalog (3 Keys) + `SyncLogServices` + Service + `Plan` + Concurrency-Catch + Tests + SyncWorker + DI + Reader-Logtext.
3. Web **Spalte Quelle + Kennzeichnung**: Controller (`Add`, `FormatSource`, `ColumnMap["source"]`, `IsNotInSage`,
   `MarkerActive`/`NotInSage`), ViewModel, View (`<th>`/`<td>` Quelle, Badge, Hinweis-Alert, Legende, `colspan`), Standort-Katalog
   + Controller-Tests.
4. **Spaltenauswahl (Zahnrad) — eigener Commit**, getrennt von der Spalte Quelle: `ColumnDefinitions.OperationConfigs` +
   `GetByViewKey`, Toolbar-Host vor `.table-responsive`, `#view-config`/`#column-config`, `column-preferences.js` vor
   `table-filter.js`, `OperationConfigsViewTests` (Drift-Test, AK 26). Grund: fachlich vom Abgleich unabhängig (Prüfbefund H5)
   und so einzeln rückbaubar bzw. von der Sammel-Spec [[2026-10-14-spaltenauswahl-zahnrad-sammel-spec]] abgrenzbar.
5. Hilfe/Changelog/Version/README/TS.

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | entfällt | – | – |

## Deploy

*Finalisiert vom QA-Lauf 2026-10-16 gegen den echten Diff `d3de9022..8773ba4f`:* `IdealAkeWms/` geändert → **Web ja**;
`IDEALAKEWMSService/` geändert → **Dienst ja**; neue Datei `IdealAkeWms/Migrations/20261016090641_AddOperationConfigSourceAndSageMissingSince.cs`
+ `SQL/96_AddOperationConfigSourceAndSageMissingSince.sql` → **Migration ja**. `SQL/AgentJobs/` unberührt, kein Skript unter
`SQL/Einmalig/`. Ablauf des Menschen: im Worktree publizieren → Testsystem → testen → mergen.
**Reihenfolge:** P5 je Standort → DB-Backup → `SQL/95` (falls noch ausstehend, Bündel) → `SQL/96` (oder Migration beim Web-Start)
→ Web → Dienst. **Nach dem Merge** nur dann erneut aus `main` publizieren, wenn der Merge getestete Dateien mit parallelen
`main`-Änderungen zusammengeführt hat.

*(Ursprünglich: Vorläufig — der QA-Lauf bestätigt gegen den echten Diff.)*

- **Web-App:** ja (Reiter mit Spalte Quelle und Zahnrad, Hilfe, Katalog-Seed der drei Keys beim Start, OSEON-Seed mit Quelle,
  Migration beim Start).
- **Service:** ja (neuer Sync, SyncWorker-Block).
- **Migration:** ja: `AddOperationConfigSourceAndSageMissingSince` / `SQL/96_AddOperationConfigSourceAndSageMissingSince.sql`,
  zwei Spalten additiv + Bestandsbefüllung `Source` (nicht destruktiv, nur die neue Spalte). Voraussetzung `SQL/95` (gleicher
  Bündel-Deploy). Reihenfolge: **P5 je Standort** (live: P5b, weil `SQL/95` dort noch aussteht) → DB-Backup (Routine; bei SQL/95 im selben Fenster Pflicht) → `SQL/95` →
  `SQL/96` → Web → Service. Kein lokaler `dotnet run` gegen `AKESQL20` vor dem Deploy ([[fallstricke]] §8 „Lokaler
  `dotnet run` der Web-App ist ein Deploy gegen AKESQL20“).
- **Nach dem Deploy:** AKE nichts einstellen (Schalter bleibt aus). IDEAL: Schalter erst nach P1–P3 einschalten, Ablauf laut
  Testprotokoll. **Betriebshinweis (Antwort 2):** die 7 AKE-Zeilen auf IDEAL erst **nach** dem ersten Abgleich-Lauf per UI
  löschen — eine leere Tabelle lässt den Seed `Program.cs:471` beim nächsten Web-Start 12 OSEON-Zeilen (Quelle Fremdsystem,
  nie gekennzeichnet) anlegen. Späterer AKE-Einsatz: View-Name, Ausschlussliste, Fremdsystem-relevant prüfen, dann Schalter.
- **Publish-Befehle** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`

**Umsetzungspunkte Doku:** Hilfe `Views/Help/Index.cshtml:528-546` (Abschnitt „Abgleich mit Sage“ — standortneutral: was
angelegt wird, dass nie überschrieben/gelöscht wird, Spalte „Quelle“ mit den drei Werten und wer sie setzt, Bedeutung „nicht in
Sage“ und dass Fremdsystem-Einträge nie gekennzeichnet werden, Quelle-Wechsel Manuell → Sage, Ausschlussliste je Standort,
Fremdsystem-relevant-Key, Verzögerung bis 10 min, Betriebshinweis „erst nach dem ersten Lauf löschen“, wie einschalten);
Anwender-Changelog; README `:766-773` (Gruppe Sage-Quellen + neuer Sync, drei Keys); `secondbrain/codebase/services.md`
(Sync-Tabelle, Katalog, SyncLog-Services); `secondbrain/codebase/datenmodell.md` (`OperationConfigs.Source`,
`SageMissingSince`); [[feature-map]]; [[fallstricke]] ggf. „Ausschlussliste fällt bei `null` auf Katalog-Default (Sync löscht
nie)“ und „`CreatedBy = 'system'` auf `OperationConfigs` ist kein Seed-Beleg (`SQL/95` B8 hat den Altbestand so befüllt)“;
Brain-Changelog.

## Offene Rueckfragen

1. **ERLEDIGT — Antwort Gerald 2026-10-16: kein Master-Gate.** *(Ursprünglicher Text:)* **Master-Gate
   `ProduktionsauftragHierarchisch` zusätzlich?** Richtigstellung: Das ist kein AppSettings-Toggle (ADR 0011),
   sondern ein **ServiceSetting** und Einwegtor (`ServiceSettingDefinitions.cs:41`, gelesen im Worker `SyncWorker.cs:357, :377,
   :397`). **Empfehlung: nein.** Gründe: (a) der eigene Schalter mit Default `false` hält AKE schon heraus; (b) die View gibt es
   nur in der IDEAL-Sage — auf AKE wäre selbst ein versehentlich eingeschalteter Abgleich „Warnung + Skip“; (c) der Abgleich
   schreibt nur Stammdaten und hängt nicht an hierarchischen FA-Daten — die Werkbank-Anlage hat das Doppel-Gate, weil sie ohne
   Master bei AKE fremde Werkbänke anlegte (`SyncWorker.cs:374`), dieser Grund fehlt hier; (d) Präzedenz für einen nur einfach
   gegateten IDEAL-View-Sync: FA-Struktur-Import `Sync:HierarchicalFaEnabled` (`SyncWorker.cs:337`); (e) IDEAL kann die
   Stammdaten so schon vor dem Umlegen des Einwegtors pflegen.
2. **ERLEDIGT — Antwort Gerald 2026-10-16: löscht selbst per UI, Betriebshinweis „erst nach dem ersten Lauf“ bleibt.**
   *(Ursprünglicher Text:)* **Reihenfolge beim Löschen der 7 AKE-Zeilen (Konflikt Entscheidung E4 ↔ Code).** Der Seed `Program.cs:471` legt beim
   nächsten Web-Start 12 OSEON-Zeilen an, sobald `OperationConfigs` **leer** ist — darunter `EG`, `ST`, `SL` mit OSEON-Namen und
   -Offsets (`SL` +5, `:473-487`); der Abgleich überschreibt sie danach nie. „Löschen vor dem ersten Abgleich“ leert die Tabelle;
   jeder Web-Start dazwischen (Deploy dieses Release, App-Pool-Recycle) seedet. **Empfehlung:** erst Schalter an, ersten Lauf
   abwarten, dann die 7 löschen; der nächste Lauf legt `SL`/`ST` mit IDEAL-Namen an — gleiches Ergebnis, Tabelle nie leer,
   und der Test prüft die Kennzeichnung gleich mit echten Daten. Alternative: wie entschieden vorher löschen, dann ohne
   Web-Neustart sofort einschalten und mit P4 kontrollieren.
3. **ERLEDIGT — Antwort Gerald 2026-10-16: Spalte „Quelle“ (Sage, Fremdsystem); ausgearbeitet in FA 6, Ausgestaltung mit
   Antwort 6 und Bestandsregel mit Antwort 7 bestätigt.** ~~ERLEDIGT bis auf Bestätigung der Ausgestaltung — … offen als
   Rückfrage 6 (Ausgestaltung) und 7 (Bestandsregel).~~ *(Ursprünglicher Text:)*
   **Migration für die Kennzeichnung (`OperationConfigs.SageMissingSince`, `SQL/96`) — einverstanden?** Begründung FA 4: Die
   Web-App hat keine Sage-Verbindung, die Werkbänke sind kein Ersatz für die View, Protokoll/Settings sind kein Zustandsspeicher.
   **Empfehlung: ja.** Bei „nein“: keine Kennzeichnung im Reiter, nur die Protokoll-Warnung (dann ohne Deduplizierung: Warnung
   in jedem Lauf), AK 8/9/15/16/18 entfallen.
4. **ERLEDIGT — Antwort Gerald 2026-10-16: ja; eingearbeitet als E5, FA 2.5, FA 4, Katalog, AKE-Regression.**
   *(Ursprünglicher Text:)* **AKE-Einsatz (Hinweis Gerald 2026-10-16: „kann sehr wohl sein, dass wir ihn [bei AKE] aktivieren“; ergaenzt von der
   Koordination).** Die Spec ist bisher IDEAL-gerahmt (Gruende 1a/1b gelten so nicht mehr). Was muss standortneutral sein?
   (a) **Fremdsystem-relevant fuer neu angelegte Kuerzel:** bei AKE (OSEON) ist `true` naheliegend, bei IDEAL `false`.
   Empfehlung: neuer String/Bool-Key `Sync:OperationConfigSyncFremdsystemRelevant` (Default false) statt fest `false`.
   (b) **Kennzeichnung „nicht in Sage“:** bei AKE stehen OSEON-Kuerzel (B, BG, MS, A-BT …) ggf. in keiner Sage-View und
   waeren alle gekennzeichnet. Empfehlung: Kennzeichnung bleibt rein informativ (kein Loeschen, keine Mail); Spec prueft,
   ob das bei AKE stoert, und nennt es im Hilfetext. (c) **View bei AKE:** eigene View mit denselben Spalten
   `Arbeitsgangnummer, Bezeichnung, AG` (Gerald), Name per `Sync:OperationConfigSyncViewName`. (d) **Ausschlussliste**
   `Sync:ProductionWorkplaceSyncAusschlussliste` ist je Standort-DB eingestellt — bei AKE pruefen.
   Zu 1: Ein Master-Gate wuerde AKE (`ProduktionsauftragHierarchisch = false`) den Abgleich unmoeglich machen —
   Empfehlung „nein“ gilt damit erst recht.
5. **ERLEDIGT — Antwort Gerald 2026-10-16: Ja; eingearbeitet in FA 2.4, FA 4, FA 5, FA 6, `Plan`, Audit, AK 23, Unit-Test,
   TS-94.15, Testprotokoll.** *(Ursprünglicher Text:)* **Wechsel Quelle Manuell → Sage (neu, aus Nachtrag 3).** Legt jemand ein Kürzel per UI an (Quelle Manuell) und erscheint es
   später in der Sage-View: Setzt der Abgleich dann `Source = Sage`? **Empfehlung (Koordination): ja** — als **einzige**
   erlaubte Änderung an bestehenden Zeilen außer der Kennzeichnung; Audit `Modified*` = `OperationConfigSync`, Info-Zeile,
   Count `quelle_sage`; Offset, Anzeigename, Relevanz bleiben. Gründe: Die Quelle zeigt danach, dass Sage das Kürzel führt
   (wer die Stammdaten prüft, sieht es); ohne Wechsel bliebe die Spalte bei jedem vorab angelegten Kürzel dauerhaft
   „Manuell“, obwohl es aus Sage kommt. Kein Wechsel von/zu Fremdsystem, keine Rückkehr Sage → Manuell. Bei „nein“ entfallen
   der Zweig in FA 6, AK 23, TS-94.15 und der Count; sonst ändert sich nichts (die Kennzeichnung hängt nicht daran, FA 4).
6. **ERLEDIGT — Antwort Gerald 2026-10-16: Ja zu (a) und (b) (`Source` englisch + `SageMissingSince` zusätzlich).**
   *(Ursprünglicher Text:)* **Ausgestaltung der Quelle bestätigen (Nachtrag 3: „von Gerald zu bestätigen“).** (a) Spalten-/Property-Name `Source`,
   DB-Werte `Sage` | `ExternalSystem` | `Manual`, UI „Quelle“ mit „Sage“ | „Fremdsystem“ | „Manuell“ — statt
   `OperationConfigs.Quelle` mit deutschen Werten wie im Nachtrag. Grund: Sprachregel (CLAUDE.md: Code englisch, UI deutsch)
   und das bestehende Muster `StorageLocations.Source` (`StorageLocationSource.cs:3-7`, `StorageLocation.cs:36-38`). (b)
   `SageMissingSince` bleibt **zusätzlich** — ohne gespeicherten Zustand kann das Web ein aus Sage verschwundenes Sage-Kürzel
   nicht erkennen und Meldungen nicht auf Übergänge begrenzen (Abwägung FA 4). **Empfehlung: ja zu (a) und (b).**
7. **ERLEDIGT — Antwort Gerald 2026-10-16: Ja, Regel so lassen, keine Altzeilen auf Manuell; P5 je Standort vor dem Deploy.
   Die unten genannte Alternative (einmaliges `UPDATE … SET Source = 'Manual'` in `SQL/Einmalig/`) entfällt.**
   *(Ursprünglicher Text:)* **Konflikt Bestandsregel ↔ Code (neu, belegt).** Nachtrag 3: „Seed-Zeilen (`CreatedBy = 'system'`) → Fremdsystem, übrige →
   Manuell“. `CreatedBy = 'system'` kennzeichnet aber nicht nur Seed-Zeilen: `SQL/95` B8 (`:63-66`, Migration
   `20261012140548_…cs:56-58`) setzt `'system'` für **jede** Zeile, die vor v1.54.0 bestand — auch von Hand angelegte (die
   Tabelle hatte bis dahin keine Audit-Felder). Da v1.54.0 und dieses Release im selben Bündel-Merge ausgeliefert werden, wird
   auf beiden Produktivsystemen praktisch der **gesamte** Bestand `ExternalSystem` und nie „nicht in Sage“ gekennzeichnet; auf
   IDEAL betrifft das die 7 AKE-Zeilen (Testprotokoll ist darauf umgestellt). Eine Unterscheidung „Seed vs. von Hand“ gibt
   der Datenbestand nicht her (die 7 IDEAL-Zeilen weichen sogar vom Code-Seed ab, z. B. `BR`). **Empfehlung: Regel so
   lassen** — der Altbestand stammt nicht aus Sage, AKE-Kürzel sollen kein Badge tragen, die IDEAL-Zeilen löscht Gerald ohnehin;
   P5 zeigt je Standort vor dem Deploy, was entsteht. Alternative, falls eine Altzeile gekennzeichnet werden soll: nach dem
   Deploy ein einmaliges, je Standort abgestimmtes `UPDATE … SET Source = 'Manual' WHERE OperationName IN (…)` in
   `SQL/Einmalig/` (kein Code).

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. Master-Gate `ProduktionsauftragHierarchisch` zusätzlich (Empfehlung: nein) → **Nein (Gerald 2026-10-16).**
2. Reihenfolge Löschen der 7 AKE-Zeilen (Empfehlung: nach dem ersten Lauf löschen) → **Gerald löscht die 7 Zeilen selbst per UI (Gerald 2026-10-16). Betriebshinweis bleibt: erst nach dem ersten Abgleich-Lauf löschen, sonst Seed `Program.cs:471` bei leerer Tabelle.**
3. Migration `SageMissingSince` / `SQL/96` für die Kennzeichnung (Empfehlung: ja) → **Gerald 2026-10-16: Spalte eher „Quelle“ (Sage, Fremdsystem). Ausgestaltung siehe Freigabe-Nachtrag (Vorschlag Koordination, von Gerald zu bestätigen).**
4. AKE-Einsatz: Fremdsystem-relevant als Setting, Kennzeichnung informativ, eigene AKE-View (Empfehlung: ja) → **Ja (Gerald 2026-10-16).**
5. Wechsel Quelle Manuell → Sage, wenn das Kürzel später in Sage erscheint (Empfehlung: ja, einzige erlaubte Änderung, Audit setzen) → **Ja (Gerald 2026-10-16).**
6. Ausgestaltung Quelle: `Source` englisch (Sage | ExternalSystem | Manual, UI „Quelle“ deutsch) + `SageMissingSince` zusätzlich (Empfehlung: ja) → **Ja (Gerald 2026-10-16).**
7. Bestandsregel `CreatedBy = 'system'` → Fremdsystem trotz `SQL/95`-Befüllung des gesamten Altbestands (Empfehlung: Regel so lassen, P5 vorher) → **Ja, Regel so lassen, keine Altzeilen auf Manuell; P5 je Standort vor dem Deploy (Gerald 2026-10-16).**

## Kritische Pruefung (2026-10-16)

Geprueft gegen Bündel-Worktree v1.60.2 (nur gelesen). Stichproben bestätigt: Seed `Program.cs:470-502` (12 Zeilen,
`CreatedBy = "system"`, Bedingung `!db.OperationConfigs.Any()`), Katalog-Seed `:453-468` (nur fehlende Keys),
`db.Database.Migrate()` `:186` läuft **vor** beiden Seeds (Spalte `Source` existiert beim OSEON-Seed), `SQL/95` B8 `:63-66`
(`'system'` nur `WHERE CreatedBy IS NULL` = gesamter Bestand zum SQL/95-Zeitpunkt), FreshInstall `:1223-1242` / `:1503-1526` /
`:2438-2441`, `CachedOperationConfigRepository.cs:10, :31-39, :65` (10 min, Nicht-konfiguriert ungecacht),
`SyncWorker.cs:374-389` (Werkbank-Block mit Doppel-Gate), `SageArbeitsplatzReader.cs:31-35`, `ServiceSettings.cs:72-77`
(`GetValueSafeAsync` → `null` bei Fehler). SQL-Nummer 96: Bündel endet bei `95_…`, `main` bei `88_…`, keine andere Spec in
`specs/`, `backlog/`, `aufgaben/` nennt `SQL/96`–`99`. Die Sammel-Spec [[2026-10-14-spaltenauswahl-zahnrad-sammel-spec]] nennt
`OperationConfigs` nicht — keine Doppelarbeit beim Zahnrad.

### BLOCKER

- **B1 — Rückfragen 5, 6, 7 unbeantwortet** (Freigabe-Antworten Zeilen 5–7 enden mit leerem Pfeil). Ohne sie ist der Rumpf
  an drei Stellen bedingt („nur bei Antwort 5 = ja“, AK 23, TS-94.15, Count `quelle_sage`; Spaltenname `Source` vs. `Quelle`;
  Bestandsregel/Testprotokoll-Erwartungen). Frage an Gerald: bitte 5, 6 und 7 je mit **Ja/Nein** (bei 7 ggf. mit Liste der
  Altzeilen, die `Manual` werden sollen) beantworten. Danach den Rumpf von den „nur bei Antwort 5“-Weichen befreien (der
  Dev-Lauf soll keine Bedingungen auswerten).

### SOLLTE

- **S1 — Lost Update durch `Update` im Web (Aussage „Update fasst Source/SageMissingSince nicht an“ stimmt nur halb).**
  `OperationConfigRepository.UpdateAsync` ruft `_context.OperationConfigs.Update(config)` (`OperationConfigRepository.cs:107`) → EF markiert **alle** Spalten als geändert und schreibt `Source` und
  `SageMissingSince` mit dem Stand von `GetByIdAsync` zurück. Setzt der Service zwischen Laden und Speichern im Web
  `SageMissingSince` oder wechselt `Source` (Antwort 5), wird das still zurückgedreht; der nächste Lauf setzt es erneut und
  **meldet den Übergang doppelt**. Vorschlag: in FA 6 / Audit als akzeptiertes Restrisiko (Fenster Millisekunden, selbstheilend)
  benennen und AK 22 auf „Update ändert die Werte nicht fachlich“ präzisieren — **oder** `UpdateAsync` auf
  „nur geänderte Properties“ umstellen (Tracking statt `Update()`); dann Regression für den bestehenden Reiter prüfen.
  Empfehlung: dokumentieren, nicht umbauen.
- **S2 — Fehlerverhalten beim Speichern der Kennzeichnung / des Quelle-Wechsels fehlt.** FA 2.6 regelt nur Inserts.
  Löscht jemand im Reiter einen Eintrag, während der Lauf ihn kennzeichnet, liefert `SaveChangesAsync`
  `DbUpdateConcurrencyException` (0 Zeilen betroffen) — ohne Regel wird daraus `FinishFailed` + Fehlermail, was AK 10/FA 5
  („Kennzeichnung löst nie eine Mail aus“) widerspricht. Vorschlag: Kennzeichnung/Rücksetzen/Quelle-Wechsel je Eintrag
  speichern (oder gesammelt mit Catch), `DbUpdateConcurrencyException` → Entity abhängen, Warnung „Eintrag X inzwischen
  gelöscht“, `fehler` +1, Lauf weiter; ein AK + Unit-Test (InMemory wirft `DbUpdateConcurrencyException` bei Update einer
  gelöschten Zeile).
- **S3 — P5 auf Produktion so nicht ausführbar.** Auf AKE-/IDEAL-**Live** ist `SQL/95` noch nicht gelaufen: es gibt nur
  `dbo.OseonOperationConfigs` **ohne** `CreatedBy` — die Abfrage scheitert, der Kommentar allein hilft dem Menschen nicht.
  Vorschlag: P5 in zwei Varianten ausschreiben — (a) Test-Systeme (nach SQL/95): wie jetzt; (b) Live vor SQL/95:
  `SELECT OperationName, DisplayName FROM dbo.OseonOperationConfigs ORDER BY OperationName;` mit dem Satz „jede Zeile wird
  `ExternalSystem` (SQL/95 B8 setzt alle auf `'system'`)“.
- **S4 — Testprotokoll-Erwartungen an P5 koppeln.** Schritte 3/4 erwarten fest „7× Fremdsystem, BR/RE/RO/RS/ZB nicht
  gekennzeichnet, `nicht_in_sage = 0`“. Auf dem IDEAL-**Test** ist SQL/95 schon gelaufen; wurde eine der 7 Zeilen danach per UI
  neu angelegt, ist sie `Manual` und wird gekennzeichnet. Vorschlag: in Schritt 3/4 „laut P5“ statt fester Zahlen, mit der
  Regel „Zeile `Manual` und Kürzel ∉ P2 → Badge erwartet“.
- **S5 — Geteilten Reader-Logtext nicht stillschweigend ändern.** `SageArbeitsplatzReader.cs:33` loggt „Werkbank-Anlage wird
  uebersprungen“; die Spec will ihn neutral formulieren. Das ist eine Änderung an Code der Werkbank-Anlage, die laut Out-of-Scope
  „unverändert“ bleibt. Vorschlag: in Out-of-Scope ausdrücklich als einzige Ausnahme nennen (nur Log-Text, kein Verhalten) und
  prüfen, dass kein Test auf den alten Text matcht.

### HINWEIS

- **H1 — Doppel-Seed-Falle bleibt still.** Wird die Tabelle auf IDEAL je leer (Löschen vor dem ersten Lauf, Testdaten-Reset),
  entstehen 12 OSEON-Zeilen mit `ExternalSystem`, die der Abgleich nie kennzeichnet und nie überschreibt (u. a. `SL` „Schlosser“
  +5). Abgedeckt nur durch Betriebshinweis + Testschritt 7. Die Seed-Bedingung bleibt laut [[2026-10-09-arbeitsgang-stammdaten-vereinheitlichen-spec]]
  Antwort 3 unverändert — bewusst, aber nicht gemeldet. Eine Warnung im Abgleich-Lauf („Einträge Quelle Fremdsystem auf einem
  Standort mit eingeschaltetem Abgleich: …“) wäre billig; nur als Idee, nicht Teil dieser Spec.
- **H2 — Cache-Asymmetrie** (FA 4) ist korrekt beschrieben: `GetAllAsync`/`GetAllAsDictionaryAsync`/`GetDueDateOffsetsAsync`
  10 min, `GetUnconfiguredOperationsAsync` ungecacht; Web-eigene Schreibwege invalidieren (`:60-62`), der Service nicht.
  Folge auch für Badge/Hinweis (Daten aus `all`) — TS-94.5 nennt die 10 min, TS-94.14/15 sollten es ebenfalls nennen.
- **H3 — `IServiceSettingRepository` ist nicht gecacht** (`Program.cs:84-87`, nur `GuardedServiceSettingRepository`) — der
  Schalter wirkt im Reiter sofort; AK 16 ist ohne Wartezeit prüfbar.
- **H4 — Teilmenge aus Sage = Massen-Kennzeichnung.** Liefert die View wegen eines Sage-Filterfehlers nur einen Teil, werden
  alle übrigen Sage-/Manuell-Einträge gekennzeichnet (nur der Leer-Guard schützt). Rein informativ und selbstheilend — akzeptabel.
- **H5 — Größe:** ca. 30 Dateien über Model, Migration (3), SQL/96, FreshInstall, Program.cs, Service+Interface, Worker, DI,
  Katalog, SyncLogServices, Standort-Katalog, Controller, ViewModel, ColumnDefinitions, View, Hilfe, Changelog, 2× AppVersion,
  README, ~6 Testdateien, TESTSZENARIEN. Ein Dev-Lauf ist machbar, aber am oberen Rand. Falls es eng wird, ist das Zahnrad
  (AK 26, TS-94.16) der natürliche Schnitt — es ist vom Abgleich fachlich unabhängig.
- **H6 — Version/TS/SQL-Nummer** sind „nächste freie zum Umsetzungszeitpunkt“; parallel stehen weitere Specs für das Bündel an
  (Zahnrad-Sammel, Briefkopf, Scan-Handler). Vor dem Anlegen erneut messen — steht in der Spec, nur zur Erinnerung.

**Empfehlung:** NACHBESSERUNG NOETIG: Rückfragen 5–7 offen; S2 (Concurrency beim Kennzeichnen → ungewollte Fehlermail) und S3 (P5 auf Live nicht ausführbar) vor dem Dev-Lauf in den Rumpf.

### Nachbesserung erledigt (2026-10-16)

- **B1 — ERLEDIGT (Nachzieh-Lauf 2026-10-16):** Freigabe-Antworten 5–7 = Ja (Gerald 2026-10-16). Weichen aufgelöst (FA 2.4,
  FA 4, FA 5, FA 6, `Plan`/`sourceSwitch`, Audit, AK 23, Unit-Test, TS-94.15, Testprotokoll Regel/Schritt 9, P5, Hilfe-Punkte),
  Nein-Zweige als überholt markiert, Hinweis-Callout „Nachbesserung 2026-10-16“ entfernt, open_questions 3/5–7 ERLEDIGT.
  ~~**B1 — NICHT erledigt.**~~ *(Stand vor dem Nachzieh-Lauf:)* Die Freigabe-Antworten 5–7 sind in der Datei leer (Pfeil ohne Antwort, gelesen 2026-10-16); die
  Weichen „nur bei Antwort 5 = ja“ und die Bindungen an Rückfrage 6/7 bleiben deshalb stehen, open_questions 5–7 bleiben offen.
  Hinweis-Callout „Nachbesserung 2026-10-16“ oben. Sobald Gerald 5–7 beantwortet: Weichen auflösen (FA 2.4, FA 4, FA 5, FA 6,
  `Plan`, Audit, AK 23, Unit-Test, TS-94.15, Testprotokoll Regel/Schritt 9).
- **S1** — umgebaut statt dokumentiert (Vorgabe Koordination): neuer Abschnitt „Repository-Update“ in FA 6 (Aufrufer belegt),
  FA-6-Tabellenzeile „UI-Bearbeiten“ als überholt markiert und ersetzt, Audit-Abschnitt, Out-of-Scope (nur `UpdateAsync` ändert
  sich), `affected_code` (neuer Eintrag `OperationConfigRepository.cs:105-109`, Regressionsanker angepasst, Repository-Test),
  AK 22 ergänzt, **neues AK 28**, Unit-Test „UpdateAsync lässt Source und SageMissingSince unverändert“ (InMemory, zwei Kontexte),
  TS-94.5 um Bearbeiten eines gekennzeichneten Eintrags ergänzt, Commit 1.
- **S2** — FA 2.7 (Speichern je Eintrag, `DbUpdateConcurrencyException` → abhängen, Warnung, `fehler` +1, keine Mail), FA 4
  Regeln, FA 5 Detailzeilen + zwei neue Tabellenzeilen, Technischer Lösungsentwurf (Bestand vor dem Read laden = Testnaht),
  **neues AK 29**, Unit-Test „Eintrag während des Laufs gelöscht“; Manual-UAT-Abschnitt begründet, warum nur Unit-Test.
- **S3** — P5 in P5a (nach `SQL/95`, `dbo.OperationConfigs`) und P5b (Live vor `SQL/95`, `dbo.OseonOperationConfigs`, Satz zu
  `SQL/95` B8) plus Vorab-Abfrage, welche Variante gilt; Deploy-Reihenfolge und AKE-Regression verweisen darauf.
- **S4** — Testprotokoll IDEAL: Einleitung mit Regel je Bestandszeile (P5a `ExternalSystem` / `Manual` ∉ P2 / `Manual` ∈ P2),
  Schritte 1–4, 6, 7 auf P5a/P2 bezogen statt fester Zahlen.
- **S5** — Out-of-Scope nennt den Reader-Logtext (`SageArbeitsplatzReader.cs:33`) als einzige Ausnahme; Grep belegt, dass kein
  Test den alten Text prüft; `affected_code`-Eintrag ergänzt.
- **H2** — TS-94.14 und TS-94.15 nennen die bis zu 10 min Web-Cache.
- **H5 (zusätzlich, Auftrag 4)** — Zahnrad bleibt im Umfang, ist in „Etappen / Commit-Reihenfolge“ als eigener Commit 4 getrennt
  von der Spalte Quelle (Commit 3).

## Umsetzung (2026-10-16)

Direkt im Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Start `d3de9022` (v1.60.2, Arbeitsbaum sauber),
Ziel **v1.61.0**, **`SQL/96`** (vor dem Anlegen gemessen: Bündel höchstes `SQL/95`, main `SQL/88`). Commits in Spec-Reihenfolge:

| # | Inhalt | Commit |
|---|---|---|
| 1 | Model `Source`/`SageMissingSince`, Migration `20261016090641_AddOperationConfigSourceAndSageMissingSince` (nur AddColumn/DropColumn + 1 Sql), `SQL/96` mit Guard, FreshInstall 3 Stellen, OSEON-Seed `ExternalSystem`, `UpdateAsync` nur Pflegefelder, Drift-Test | `6993c774` |
| 2 | 3 Katalog-Keys, `SyncLogServices.OperationConfigSync`, `OperationConfigSyncService` (+ `Plan`, Speichern je Eintrag mit Concurrency-Catch), SyncWorker-Block (EIN Gate), DI, Standort-Katalog, Reader-Logtext | `bc134c15` |
| 3 | Reiter: Spalte Quelle, Kennzeichnung „nicht in Sage“, Legende, `Add` = Manual | `7042efcb` |
| 4 | Zahnrad/Spaltenauswahl (eigener Commit) | `459cb7b5` |
| 5 | Hilfe, Changelog, AppVersion 1.61.0 (Web + Dienst), README, TS-94 | `9731b38a` |

**Nachweis (vor QA):**
- `dotnet build IdealAkeWms.slnx` → 0 Fehler, 28 Warnungen (gleich viele wie vor Commit 5; keine aus geänderten Dateien).
- `dotnet test` → Web **2035 bestanden, 1 übersprungen**, 0 Fehler; Service **306 bestanden**, 0 Fehler.
- `dotnet ef migrations has-pending-model-changes` → „No changes have been made to the model since the last migration.“
- **AK 28 Red-Green:** `UpdateAsync_LaesstSourceUndSageMissingSinceUnveraendert` gegen das alte `Update(config)` **rot**
  („Expected saved.OperationName to be "KA", but "KX" differs“), danach grün → AK 28 ist InMemory-prüfbar; TS-94.5 bleibt Sichtprobe.
- **AK 29 Red-Green:** `Run_EintragWaehrendDesLaufsGeloescht_WarnungFehlerKeinAbbruch` ohne Catch **rot**
  (`DbUpdateConcurrencyException: Attempted to update or delete an entity that does not exist in the store.`), mit Catch grün.

**Abweichungen/Entscheidungen im Lauf (klein, im Rahmen der Spec):**
- Settings eines Laufs als `internal record OperationConfigSyncSettings` + `internal RunAsync(dryRun, settings, ct)`: macht
  AK 27 (`FremdsystemRelevant = true`) und die bewusst leere Ausschlussliste ohne Settings-DB testbar; öffentliches `RunAsync` liest
  die Settings wie das Geschwister.
- Protokollzeilen werden auf 1000 Zeichen gekürzt (`SyncLogs.Message NVARCHAR(1000)`; längere Zeilen gingen sonst still verloren,
  `SyncRun.WriteEntryAsync` schluckt den Fehler).
- Meldetexte mit Umlauten wie in der Spec („gelöscht — übersprungen“); das Geschwister nutzt ASCII.
- `fallstricke.md` §24 um drei Einträge ergänzt (Seed-Beleg, `Update()` schreibt alle Spalten, Ausschlussliste-Fallback).

## QA (2026-10-16, qa-agent)

Geprüft im Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Diff `d3de9022..9731b38a` (5 Commits), danach QA-Commit
`8773ba4f`. Review synchron in diesem Lauf, nicht delegiert. Keine Sage-/DB-Abfragen, kein lokaler Start.

### 1. Build und Tests

| Stand | Build | IdealAkeWms.Tests | IDEALAKEWMSService.Tests |
|---|---|---|---|
| `9731b38a` (vor QA) | 0 Fehler, 28 Warnungen | 2035 bestanden, 1 übersprungen, 0 Fehler (2036) | 306 bestanden, 0 übersprungen, 0 Fehler |
| `8773ba4f` (nach QA-Fix) | 0 Fehler, 24 Warnungen (inkrementell, keine aus geänderten Dateien) | 2036 bestanden, 1 übersprungen, 0 Fehler (2037) | 307 bestanden, 0 übersprungen, 0 Fehler |

Neuer QA-Test `Run_ManuellGekennzeichnetInSage_WaehrendDesLaufsGeloescht_KeinFalschesWiederInSage` vor dem Fix **rot**
(„Expected wieder_in_sage to be 0, but found 1“), danach grün.

### 2. Code-Review — Befunde und Behandlung

Geprüft: `OperationConfigSyncService.cs` (ganz, inkl. `Plan`, `Change`, `TrySaveAsync`), `IOperationConfigSyncService.cs`,
`SyncWorker.cs` (neuer Block), `Program.cs` (Dienst-DI, Web-Seed), `SageArbeitsplatzReader.cs:33`, `OperationConfigRepository.UpdateAsync`,
`CachedOperationConfigRepository.cs:61-63`, `OperationConfigsController` (Index/Add/Update/ColumnMap/FormatSource/IsNotInSage),
ViewModel, `Views/OperationConfigs/Index.cshtml`, `wwwroot/js/csv-export.js:78-97` (Zellwert), `ColumnDefinitions`,
`ServiceSettingDefinitions`, `StandortSettingsCatalog`, `SyncLogServices`, `OperationConfig.cs`, `ApplicationDbContext`, Migration +
Snapshot, `SQL/96`, `SQL/00_FreshInstall.sql` (3 Stellen), Hilfe, Changelog, beide `AppVersion.cs`, README, TS-94, Testindex, alle
Testdateien des Diffs.

| # | Schwere | Befund | Behandlung |
|---|---|---|---|
| Q1 | klein | `OperationConfigSyncService.cs` (Schleife `plan.Unmark`): Ein Eintrag `Source = Manual` mit `SageMissingSince` und Kürzel in der View steht in `SourceToSage` **und** `Unmark`. Wird er während des Laufs gelöscht, scheitert der Wechsel (abgehängt, `fehler` +1); das Zurücksetzen lief danach auf dem Detached-Objekt — `SaveChanges` schreibt nichts, `TrySaveAsync` meldet Erfolg → `wieder_in_sage` +1 und Info „wieder in Sage: X“ für einen gelöschten Eintrag (falsche Meldung, keine Datenwirkung). | **Gefixt** in `8773ba4f`: `Unmark` überspringt abgehängte Einträge (`_ctx.Entry(..).State != Detached`); Red-Green-Test ergänzt. `Mark`/`Clear` können nicht mit `SourceToSage` überlappen (Mark ⇒ ∉ View, Clear ⇒ ExternalSystem/Ausschluss). |
| Q2 | klein | AK 28 Attach-Pfad (`UpdateAsync` mit nicht getracktem Objekt) war ungetestet; beide vorhandenen Tests laden getrackt. | **Test ergänzt** in `8773ba4f`: `UpdateAsync_Detached_SchreibtNurPflegefelder` — grün; `OperationName`, `Source`, `SageMissingSince`, `CreatedBy` bleiben. Produktiv lädt `Update` immer getrackt (`GetByIdAsync` → `FindAsync`, Decorator ungecacht `:63`). |
| Q3 | Hinweis | CSV-Export der Kürzel-Spalte enthält bei gekennzeichneten Zeilen „KA nicht in Sage“ — `csv-export.js:76-93` nimmt bewusst „Text inkl. Badges“. Hausverhalten, nicht spec-widrig (AK 25 betrifft die Spalte Quelle). Spaltenfilter unberührt (`ColumnMap["name"]` = `OperationName`). | Nur dokumentiert. |
| Q4 | Hinweis | `SyncResult.Updated` zählt `plan.Mark/Unmark` statt der tatsächlich gespeicherten Änderungen — wirkt nur auf die Logzeile des Workers, nicht auf die Protokoll-Counts. | Nur dokumentiert. |

Ohne Befund geprüft (Schwerpunkte des Auftrags):
- **Nur anlegen / einzige Bestandsänderung:** Bestandszeilen werden nur über `Change` angefasst, und nur `Source = Sage` (nur bei
  `Manual`), `SageMissingSince` und `Modified*`; kein `Remove`, kein Schreiben von `DisplayName`/Offset/Relevanz. Change-Tracking
  statt `Update()` → nur geänderte Spalten.
- **View-Spalten:** Reader unverändert (nur Logtext `:33`), liest bei View-Quelle `Arbeitsgangnummer, Bezeichnung, AG`; der Dienst
  nutzt nur `Arbeitsschritt` (= AG) und `Bezeichnung1`.
- **Kennzeichnung nie bei ExternalSystem, nur bei Schalter an:** `Plan` überspringt ExternalSystem/Ausschluss (vorhandener
  Zeitstempel → `Clear`, ohne Meldung); Web `IsNotInSage` + `MarkerActive` (`GetValueAsync == "true"`, `null` → aus); Badge-Bedingung
  in der View und `NotInSage` nutzen dieselbe Methode.
- **Concurrency je Eintrag ohne Mail:** `TrySaveAsync` fängt `DbUpdateConcurrencyException`/`DbUpdateException`, hängt ab, Warnung,
  `fehler` +1, `FinishSuccess`; nur ungültiger View-Name und Sage-Verbindungsfehler werfen (→ Mail über `RunResilientAsync`).
- **UpdateAsync:** `EditableProperties` (6 Namen, eine Stelle); `IsModified` je Property, Detached → `Attach` (jetzt getestet, Q2).
- **Migration:** nur 2× `AddColumn` + 2× `DropColumn` + 1 `Sql`; Befüllung wortgleich in `SQL/96` und FreshInstall-`EXEC`
  (Drift-Test grün). `SQL/96`: `OBJECT_ID`-Guard mit `SET NOEXEC ON`, `COL_LENGTH`-Guards, jede DDL eigener Batch, Befüllung eigener
  Batch, History eigener Batch, `SET NOEXEC OFF` am Ende. FreshInstall: Schema (2 Spalten), Seed-`EXEC` mit `COL_LENGTH`-Guard,
  `MigrationId` nur bei vorhandenen Spalten. Snapshot passt (`ValueGeneratedOnAdd`/`HasDefaultValue("Manual")`).
- **Seed** `Program.cs` → `ExternalSystem`, Audit weiter `system`; UI-`Add` → `Manual`; Sync-Anlage → `Sage`, `CreatedBy = OperationConfigSync`.
- **ADR 0010:** `ISyncLogger` letzter Ctor-Parameter; `SyncLogServices.OperationConfigSync` in `All`; deutsche Counts-Keys.
  **ADR 0008:** drei Keys im Katalog, Beschreibungen ≤ 500 und standortneutral, Default-Konstanten gegen den Katalog getestet.
  **ADR 0005:** `<th>`-Keys = `#column-config` = `ColumnDefinitions.OperationConfigs` = `ColumnMap` + `actions` (Drift-Test),
  Toolbar-Host direkt vor `.table-responsive`, `column-preferences.js` vor `table-filter.js`, `GetByViewKey`-Eintrag. **ADR 0003:**
  `Created*` bei Anlage, `Modified*` bei jedem Bestands-Update des Dienstes; Bestandsbefüllung ohne `Modified*` (wie spezifiziert).
- **Hausfehlerklassen:** keine Werte in `<script>` (die beiden JSON-Blöcke sind statisch); kein `hidden` + `d-*`; kein stiller
  Fallback (`FormatSource` unbekannt → Rohwert; Ausschlussliste `null` → Katalog-Default ist Spec-Entscheidung FA 3); keine
  parallelen Arrays; keine reine Client-Validierung; Regel „nicht in Sage“ steht im Dienst (`Plan`) und im Web (`IsNotInSage`) —
  bewusst redundant (Spec FA 4), beide schließen `ExternalSystem` aus, der Dienst räumt Ausschluss-Kürzel zusätzlich ab.
- **WCAG:** `badge text-bg-warning` = Bootstrap 5.3 schwarz auf `#ffc107` (≈ 12:1); Bedeutung trägt der Text „nicht in Sage“.
- **Hilfe/Changelog/Version/README/TS-94/Testindex:** vorhanden (1.61.0 in beiden `AppVersion.cs`; TS-94 in `docs/TESTSZENARIEN.md`;
  Testindex Zeile 94 im Hauptcheckout).

### 3. Beweisart je Akzeptanzkriterium

| AK | Beweis | Beleg / Grund |
|---|---|---|
| 1 | Unit-Test | `ServiceSettingDefinitionsTests` (InlineData ×3, `All_FieldsFitServiceSettingsColumns`), `Run_OhneEinstellung_…StimmenMitKatalog` (Defaults, kein „IDEAL:“) |
| 2 | Unit-Test (+ Filter-Sicht Manual-UAT) | `SyncLoggerTests.All_contains_OperationConfigSync` |
| 3 | Manual-UAT | SyncWorker-Gate mit DB-Wert (TS-94.1) |
| 4 | Unit-Test | `Run_NeuesKuerzel_WirdMitDefaultsAngelegt` |
| 5 | Unit-Test | `Run_VorhandenMitLeerzeichenUndKleinschreibung_KeinInsert_KeineAenderung`, `Run_UmlautKuerzel_…` |
| 6 | Unit-Test (Logik) + Manual-UAT (Settings-Read) | `Run_Ausschlussliste_KatalogDefaultGreift_…`, `Run_AusschlusslisteBewusstLeer_GiltLeer`; TS-94.4 |
| 7 | Unit-Test | `Plan_Bezeichnung_EindeutigGesetzt_MehrdeutigLeerMitWarnung` |
| 8 | Unit-Test | `Run_KennzeichnenNurEinmalMelden_DannZuruecksetzen` |
| 9 | Unit-Test | dito |
| 10 | Unit-Test (Logik) + Manual-UAT (`OBJECT_ID` am Sage-Server, Folgeblöcke) | `Run_QuelleFehlt_WarnungSkip_OhneMutation`; TS-94.8 |
| 11 | Unit-Test | `Run_QuelleLeer_KeineAnlageKeineKennzeichnung` |
| 12 | Unit-Test (kein SQL, throw) + Manual-UAT (Fehlermail) | `Run_UngueltigerViewName_FehlerOhneSql`; TS-94.9 |
| 13 | Unit-Test | `Run_DryRun_ZaehltOhneMutation` |
| 14 | Manual-UAT | Unique-Index nur am SQL Server (InMemory erzwingt ihn nicht); TS-94.12 |
| 15 | Unit-Test (ViewModel) + Manual-UAT (Badge/Hinweis gerendert, Kontrast) | `Index_NichtInSage_NurBeiSchalterAn_NieFremdsystem`; TS-94.5 |
| 16 | Unit-Test + Manual-UAT | dito (Schalter false/null); TS-94.6 |
| 17 | Manual-UAT (gerendert) | `ColumnMap["name"]` unverändert `OperationName`; TS-94.7 |
| 18 | Unit-Test (Drift) + Manual-UAT (Skripte am SQL Server) | `OperationConfigSourceMigrationSqlTests` (4 Tests); `has-pending-model-changes` leer (Dev-Lauf); TS-94.11/18/19 |
| 19 | Manual-UAT | AKE-Regression, TS-94.1 |
| 20 | Sichtprüfung QA | Hilfe, Changelog, README, TS-94, Testindex vorhanden; services.md/datenmodell im Hauptcheckout (Aufrufer) |
| 21 | Manual-UAT | raw-SQL-Bestandsbefüllung; P5 vorher / P4 nachher |
| 22 | Unit-Test (UI-Anlage, Update) + Manual-UAT (Seed) | `Add_SetztQuelleManuell`, `Update_LaesstQuelleUndZeitstempelUnveraendert`, `Run_NeuesKuerzel_…`; Seed TS-94.18 |
| 23 | Unit-Test | `Run_ManuellInSage_WechseltAufSage_FremdsystemNie` |
| 24 | Unit-Test | `Run_Fremdsystem_NieGekennzeichnet_…`, `Index_NichtInSage_…` (BR ExternalSystem mit Zeitstempel) |
| 25 | Unit-Test (Filter, Format) + Manual-UAT (CSV im Browser) | `Index_Spaltenfilter_Quelle_WieAngezeigt`, `FormatSource_DreiWerte_UnbekanntRoh`; TS-94.13 |
| 26 | Unit-Test (Drift) + Manual-UAT (Speichern, Browser-JS) | `OperationConfigsViewTests` (3 Tests); TS-94.16 |
| 27 | Unit-Test | `Run_FremdsystemRelevantTrue_SetztFlagNurBeiNeuanlage` |
| 28 | Unit-Test (Red-Green) + Manual-UAT-Sichtprobe | `UpdateAsync_LaesstSourceUndSageMissingSinceUnveraendert`, `UpdateAsync_Detached_SchreibtNurPflegefelder` (QA); TS-94.5 |
| 29 | Unit-Test (Red-Green) | `Run_EintragWaehrendDesLaufsGeloescht_…`, `Run_ManuellGekennzeichnetInSage_WaehrendDesLaufsGeloescht_…` (QA) |

Nicht als grün gezählt: AK 3, 14, 17, 19, 21 vollständig Manual-UAT; Teile von 2, 6, 10, 12, 15, 16, 18, 22, 25, 26, 28.

### 4. Geänderte Bestandstests

| Test | Änderung | Einordnung |
|---|---|---|
| `OperationConfigsControllerTests.Ctrl()` | Ctor um `_settings.Object` (Mock `IServiceSettingRepository`) ergänzt | reine Typ-/Compile-Anpassung |
| `ServiceSettingDefinitionsTests.All_ContainsDocumentedServiceReadKey` | drei `InlineData` ergänzt | additiv (AK 1), keine Bestandszeile geändert |
| `SyncLoggerTests` | neuer Test `All_contains_OperationConfigSync` | additiv (AK 2) |
| `OperationConfigRepositoryTests` | zwei neue Tests (Dev + QA) | additiv (AK 28); `UpdateAsync_ModifiesConfig` und Decorator-Test unverändert und grün |

Kein Bestandstest in Fixture oder Erwartung verändert → kein Befund „Sonstiges“.

### 5. CLAUDE.md-Checkliste

Migration + `SQL/96` mit Guard ✔ · FreshInstall Schema + `MigrationId` (+ Seed-`EXEC`) ✔ · `SQL/AgentJobs/` nicht betroffen ✔ ·
Audit-Felder ✔ · Version beide `AppVersion.cs` 1.61.0 + Anwender-Changelog ✔ · README ✔ · `docs/TESTSZENARIEN.md` TS-94 ✔ +
Testindex ✔ · Brain-Changelog/feature-map/services.md/datenmodell: im Hauptcheckout durch den Aufrufer (nicht Teil des QA-Commits) ·
keine neue Rolle · kein Einmal-Skript.

**Ergebnis:** grün, keine kritischen/wichtigen Befunde; zwei kleine Befunde im Lauf behoben (`8773ba4f`). Status → **Testbereit**.

## Manuelle Test-Checkliste (Schranke 2)

1. **Vor dem Deploy:** P5 je Standort (Testsystem P5a, Live P5b) ausführen und protokollieren; IDEAL-Test zusätzlich P1, P2 (Sage), P3, P4.
2. DB-Backup; im Worktree publizieren (Web + Dienst, Befehle im Abschnitt Deploy); `SQL/96` bzw. Migration beim Web-Start; Web, dann Dienst.
3. TS-94.1: `/ServiceSettings` zeigt die drei Keys mit Default; zwei Zyklen ohne Lauf `OperationConfigSync`; Reiter mit Spalte „Quelle“ und Zahnrad, ohne Badge/Hinweis; P4 `Source` = P5.
4. TS-94.2: Schalter an, ein Zyklus → neue Kürzel mit Quelle Sage, Offset 0, Fremdsystem-relevant Nein, `SÄ` korrekt (P4).
5. TS-94.3 Bestand bleibt; TS-94.4 Ausschluss.
6. TS-94.5 `ZZTEST`: Badge + gelber Hinweis (nach ≤ 10 min), Warnung nur einmal; Bearbeiten → `Source`/`SageMissingSince` unverändert (P4); danach löschen.
7. TS-94.6 Schalter aus → Badge/Hinweis sofort weg; wieder an.
8. TS-94.7 Filter `=SL` → genau eine Zeile; TS-94.13 Quelle-Filter `=Manuell`, `!Fremdsystem`, CSV-Export (Spalte Quelle als Klartext).
9. TS-94.14 Fremdsystem-Eintrag nie gekennzeichnet; TS-94.15 Quelle-Wechsel Manuell → Sage.
10. TS-94.16 Zahnrad: „Quelle“ ausblenden/umordnen übersteht Reload, kein 400 auf `/api/user-view-preferences/OperationConfigs`; Badge-Lesbarkeit am Terminal prüfen.
11. TS-94.17 Fremdsystem-relevant-Key `true` → Neuanlage „Ja“; zurück auf `false`.
12. Negativfälle TS-94.8 (View fehlt → Warnung, keine Mail), TS-94.9 (`vw;DROP` → Fehlermail), TS-94.10 (DryRun), TS-94.11 (`SQL/96` zweimal), TS-94.12 (nur bei `_AI`), TS-94.19 (`SQL/96` vor SQL/95, Wegwerf-DB).
13. Testprotokoll IDEAL-Test Schritte 5–8: die 7 AKE-Zeilen **erst nach dem ersten Lauf** löschen; Folgelauf legt SL/ST mit IDEAL-Namen an; App-Pool-Recycle → keine OSEON-Seed-Zeilen.
14. TS-94.18 Seeds nur auf Wegwerf-DB.
15. AKE-Regression (Schalter aus): OSEON-Teileverfolgung/-Reporting stichprobenartig wie vorher, P4 vorher/nachher.
