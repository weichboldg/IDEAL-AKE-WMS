---
type: spec
title: "Aktivitäts-Protokoll: Spaltenfilter über alle Einträge, deutsche Aktivitätsnamen"
slug: 2026-10-16-aktivitaetsprotokoll-filter-und-namen-spec
status: Testbereit
created: 2026-10-27
updated: 2026-10-28
source_backlog: "[[2026-10-16-aktivitaetsprotokoll-filter-und-namen]]"
task: "[[2026-10-16-aktivitaetsprotokoll-filter-und-namen-umsetzung]]"
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
affected_code:
  - "IdealAkeWms/Data/Repositories/SyncLogRepository.cs:32-50 (GetPagedAsync: Spaltenfilter VOR Count/Skip/Take, neue internal static ApplyColumnFilter nach Muster StockMovementRepository.cs:564-592; Seiten-Guard wie ProductionOrderRepository.cs:44)"
  - "IdealAkeWms/Data/Repositories/ISyncLogRepository.cs:9 (GetPagedAsync bekommt IReadOnlyDictionary<string,string>? columnFilters)"
  - "IdealAkeWms/Controllers/SyncLogController.cs:14, :36-39, :47, :58-66 (Filter an das Repository, In-Memory-Apply und SyncLogColumnMap entfallen, Auswahlliste nach Anzeigename sortiert)"
  - "IdealAkeWms/Services/SyncLogger/SyncLogServices.cs:40-50 (eine Zuordnung DisplayNames + DisplayName(string) mit Rueckfall auf den technischen Namen)"
  - "IdealAkeWms/Views/SyncLog/Index.cshtml:12-15 (Auswahlfeld: Wert technisch, Text deutsch, title technisch), :59 (Zelle: Text deutsch, title technisch)"
  - "IdealAkeWms/Views/Help/Index.cshtml:1940-1958 (Hilfe Aktivitaets-Protokoll: Namensliste, Rolle admin)"
  - "IdealAkeWms.Tests/Repositories/SyncLogRepositoryTests.cs, IdealAkeWms.Tests/Controllers/SyncLogControllerTests.cs, IdealAkeWms.Tests/Services/SyncLoggerTests.cs:180-195 (bzw. neue SyncLogServicesTests)"
  - "IdealAkeWms/AppVersion.cs + IDEALAKEWMSService/AppVersion.cs, Views/Help/Changelog.cshtml (vX.Y.Z, vergibt der /dev-Lauf)"
  - "docs/TESTSZENARIEN.md (TS-NN, vergibt der /dev-Lauf)"
open_questions:
  - "Anzeigenamen-Tabelle (Abschnitt Fachliche Anforderungen, A2) so uebernehmen, inkl. Feiertags-Sync statt Holiday-Sync, FA-Arbeitsgang-Erkennung (BOM-Cache) vs. Arbeitsgang-Erkennung (Struktur) und alphabetischer Sortierung im Auswahlfeld? Empfehlung: ja."
  - "Spaltenfilter Zeitpunkt laeuft kuenftig in SQL wie in der Bewegungshistorie: Tag/Monat/Jahr, Vergleich und Bereich tageweise; eine Suche nach Uhrzeitteilen (z. B. 14:3) wirkt nicht mehr. Einverstanden? Empfehlung: ja (Alternative waere alle Zeilen in den Speicher zu laden - bei jeder Filterung)."
  - "Spaltenfilter Aktivitaet trifft Anzeigename ODER technischen Namen der Konstanten in SyncLogServices.All. Zeilen mit Altnamen, die nicht mehr in All stehen (belegt: FaWorkStepStructureDetection, umbenannt in v1.44.0), findet der Positivfilter nicht. So lassen? Empfehlung: ja - Altzeilen altern ueber die Bereinigung (Default 180 Tage) aus; Pruefabfrage P-3 zeigt, ob es sie gibt."
  - "Zusatzfund Fehlteile (MissingPartsController.cs:104-112): Bei 'nur meine Werkbaenke' mit mehreren Werkbaenken wird die geladene Seite nachgefiltert und die Trefferzahl auf die Seite gesetzt - gleiches Fehlerbild, aber kein Spaltenfilter. Mitbeheben oder eigener Backlog-Eintrag? Empfehlung: eigener Backlog-Eintrag (Repository-Signatur und Fehlteile-Testszenarien aendern sich, nicht klein)."
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: false
freigabe_entscheidung: ""
freigabe_von: "Gerald Weichbold"
freigabe_am: "2026-10-28"
---

# Aktivitäts-Protokoll: Spaltenfilter über alle Einträge, deutsche Aktivitätsnamen

> [!important] Freigabe-Nachtrag 2026-10-28 (geht dem Rumpf vor)
> Antworten 1–4 wie empfohlen (Block „Freigabe-Antworten“): Anzeigenamen-Tabelle A2 mit alphabetischer Sortierung; Zeitpunkt-Filter
> tageweise in SQL; Aktivitäts-Filter nur über `SyncLogServices.All` (Altnamen bleiben technisch sichtbar); Fehlteile **nicht** in
> diesem Lauf, eigener Backlog-Eintrag [[2026-10-28-fehlteile-nur-meine-werkbaenke-trefferzahl]].
> Bündel-Lauf im Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, neue Minor-Version (vergibt der Dev-Lauf, nach v1.69.0).
> P-1 bis P-3 sind Prüfabfragen vor der Abnahme, kein Hindernis für den Dev-Lauf; „Groß/Klein egal“ bleibt bis P-2 unverifiziert.

> [!info] Code-Grundlage
> Alle Fundstellen gemessen am Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5` (Zweig
> `feature/2026-08-07-ideal-teile-1-5`, nach QR-Matchcode v1.68.0, HEAD `ca48b22d`), nur gelesen. Pfade relativ zum
> Worktree. Version `vX.Y.Z` und Szenario-Nummer `TS-NN` vergibt der /dev-Lauf.

> [!note] Standort
> Das Aktivitäts-Protokoll ist standortneutral (AKE und IDEAL, gleicher Code). Welche Aktivitäten Zeilen schreiben, hängt
> vom Standort ab (OSEON nur AKE, FA-Hierarchie/Materialisierung/Werkbank-Anlage nur IDEAL mit Master
> `ProduktionsauftragHierarchisch`, `SyncWorker.cs:337-426`). Die Änderung wirkt an beiden Standorten gleich.

## Ziel / Nutzen (das Warum)

Der Admin soll im Aktivitäts-Protokoll (`/SyncLog`) jeden Lauf finden — auch ältere Einträge. Heute filtert der
Spaltenfilter nur die gerade geladene Seite, Trefferzahl und Seitenleiste bleiben ungefiltert. Ein Eintrag von vorgestern
ist über „Meldung enthält …“ darum praktisch nicht auffindbar, und die Seitenleiste zeigt Seiten, die nach dem Filter leer
sind. Dazu kommt: Das Auswahlfeld „Aktivität“ zeigt technische Namen (`OperationConfigSync`, `ProductionWorkplaceSync`),
der Admin kennt die Läufe aber unter den Namen aus Dienst-Log, Hilfe und Service-Einstellungen („Arbeitsgang-Abgleich“,
„Werkbank-Anlage“). Die Meldung „konnte nichts finden“ aus dem Backlog entsteht aus beidem zusammen.

## Befund (belegt)

**B1 — Filter nach dem Paging.** `SyncLogController.Index` holt mit `_repo.GetPagedAsync(service, level, reference, page,
effectivePageSize)` genau eine Seite (`SyncLogController.cs:36`; Repository zählt und schneidet vor jedem Spaltenfilter,
`SyncLogRepository.cs:43-48`) und filtert diese Seite danach im Speicher (`SyncLogController.cs:38-39`). `TotalCount`
stammt aus dem ungefilterten Count (`SyncLogController.cs:53`). Die Tabelle ist trotzdem als server-gefiltert markiert
(`Views/SyncLog/Index.cshtml:38`), der Browser filtert also selbst nicht (`table-filter.js:10-25`).

**B2 — CSV-Export erbt den Fehler.** Der Export lädt im Server-Mode dieselbe URL (inkl. `colf_*`, Filterkarte) mit
`pageSize=0&page=1` neu (`csv-export.js:195-208`). `PageSize.Resolve(0)` = 5000 (`PageSize.cs:25-29`). Heute heißt das: die
**neuesten 5000 Zeilen** werden geladen und erst dann gefiltert — ältere Treffer fehlen im Export, und der Kappungshinweis
(`data-capped`, `_Pagination.cshtml:26`, `PaginationState.cs:32-33`) richtet sich nach der **ungefilterten** Gesamtzahl, er
erscheint also auch bei 3 Treffern, sobald das Protokoll mehr als 5000 Zeilen hat.

**B3 — Alle Schreibwege nutzen die Konstanten.** Jede Protokollzeile entsteht in `SyncRun.WriteEntryAsync`
(`SyncRun.cs:67-96`; aufgerufen nur aus `SyncRun.cs:25-60` und `SyncLogger.cs:20`). Jeder `BeginRunAsync`-Aufruf übergibt
eine `SyncLogServices`-Konstante (24 Aufrufe im Dienst, z. B. `SyncWorker`-Services, `SageBookingWorker.cs:107`; 3 im Web:
`HierarchieUmstellungController.cs:79, :100`, `ServiceSettingsController.cs:83`). `SyncLogRepository.AddAsync`
(`SyncLogRepository.cs:16-20`) hat keinen Aufrufer außerhalb der Tests, und kein SQL-Skript schreibt in `SyncLogs` (Grep
`INSERT INTO … SyncLogs` in `SQL/`: kein Treffer). Neue Zeilen tragen also nur Namen aus `SyncLogServices.All`
(`SyncLogServices.cs:40-50`, 25 Konstanten). **Altzeilen** können andere Namen tragen: `WorkOperationStructureDetection`
hieß bis v1.44.0 `FaWorkStepStructureDetection` (`SyncLogServices.cs:28`, Kommentar).

**B4 — Timestamp ist Lokalzeit, `datetime2`.** Model-Default `DateTime.Now` (`SyncLog.cs:10`), `WriteEntryAsync` setzt ihn
nicht (`SyncRun.cs:85-87`), Spalte `DATETIME2` (`SQL/00_FreshInstall.sql:1081`). Indizes: `IX_SyncLogs_Timestamp_Desc`,
`IX_SyncLogs_Service_Level` (`SQL/00_FreshInstall.sql:1113-1116`). Keine Spalte trägt eine eigene Kollation im Skript
(`SQL/00_FreshInstall.sql:1079-1087`) — sie erben die DB-Kollation; ob die Server-DBs das auch so haben, prüft P-2.

**B5 — Hilfe veraltet.** Die Hilfe nennt im Auswahlfeld 18 technische Namen (es sind 25) und als Rolle „`masterdata` oder
`admin`“ (`Views/Help/Index.cshtml:1941-1957`). Der Controller ist `[RequireAdminAccess]` (`SyncLogController.cs:11`), seit
v1.20.0 admin-only (`Views/Help/Changelog.cshtml:1745`).

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**

1. Spaltenfilter des Aktivitäts-Protokolls in SQL **vor** Count/Skip/Take; Trefferzahl, Seitenleiste und CSV-Export
   gefiltert.
2. Deutsche Anzeigenamen je `SyncLogServices`-Konstante an **einer** Stelle (`SyncLogServices.cs`), verwendet im
   Auswahlfeld und in der Tabellenspalte; technischer Name als Wert bzw. Tooltip.
3. Seiten-Guard `page < 1 → 1` im neuen `GetPagedAsync` (Hausmuster `ProductionOrderRepository.cs:44`). Grund: Mit SQL
   Server wirft ein negativer `OFFSET` (heute bei `?page=0` aus `(page - 1) * pageSize`, `SyncLogRepository.cs:46`); die
   Methode wird ohnehin neu geschrieben.
4. Hilfe-Abschnitt Aktivitäts-Protokoll: Namensliste und Rollenangabe (B5) richtigstellen, Hinweis „Spaltenfilter durchsucht
   alle Einträge“.
5. Prüfung aller anderen server-gefilterten Listen auf dasselbe Muster (Tabelle unten). Ergebnis: **keine weitere Liste
   betroffen**; nichts mitzubeheben.

**Out-of-Scope**

- Fehlteile „nur meine Werkbänke“ (Rückfrage 4).
- Spaltenauswahl (Zahnrad) für das Aktivitäts-Protokoll: fehlt (`Views/SyncLog/Index.cshtml` ohne `#view-config`/
  `#column-config`, kein `"SyncLog"` in `ColumnDefinitions`) — ist als Z3 in [[2026-10-14-spaltenauswahl-zahnrad-sammel-spec]]
  erfasst und hat einen eigenen Backlog-Eintrag [[2026-10-14-spaltenauswahl-pflicht-fehlende-listen]].
- Filterkarte im Muster `card filter-card` (die View nutzt `<form class="row g-2 mb-3">`, `Views/SyncLog/Index.cshtml:7`) und
  deutsche Stufen-Namen (Info/Warning/Error) — nicht verlangt.
- Inhalt der Protokollzeilen und die Frage, ob auf IDEAL-Test alle Läufe tatsächlich schreiben (Prüfabfrage P-1, kein Code).

## Fachliche Anforderungen

**A1 — Spaltenfilter über alle Einträge.** Jeder Spaltenfilter (Zeitpunkt, Aktivität, Stufe, Referenz, Meldung) wirkt auf
alle Protokollzeilen, die die Filterkarte (Aktivität, Stufe, Referenz) übrig lässt. Trefferzahl und Seitenzahl zählen nur
Treffer. Syntax wie überall seit v1.58.1 ([[0017-ein-parser-fuer-die-spaltenfilter-syntax]]): `;`/`,` ODER, `!` für alle
Teile, `x%`, `%x`, `=x`, `(leer)`; in „Zeitpunkt“ zusätzlich `>`/`<`/`>=`/`<=`, `a...b`, Tag/Monat/Jahr.

**A2 — Anzeigenamen.** Eine Zuordnung in `SyncLogServices.cs`. Die Namen folgen dem Text, mit dem der Dienst den Lauf im
Serilog-Log bzw. in der Fehlermail nennt (`RunResilientAsync`-/`NotifyErrorAsync`-Bezeichnung); wo es den nicht gibt, die
Quelle in der letzten Spalte.

| Konstante (`SyncLogServices.cs`) | Anzeigename | Quelle des Namens |
|---|---|---|
| `Lagerplatz` (:10) | Lagerplatz-Sync | `SyncWorker.cs:318`, `:329` |
| `Lagerbestand` (:11) | Lagerbestand-Sync | `SyncWorker.cs:435`, `:448` |
| `BomCache` (:12) | BOM-Cache-Sync | `SyncWorker.cs:195`, `:205` |
| `OseonTracking` (:13) | OSEON-Tracking-Sync | `SyncWorker.cs:117`, `:121` |
| `OseonWorkplaces` (:14) | Werkbank-Sync (OSEON → Produktionsaufträge) | `SyncWorker.cs:129`; Lauf in `OseonSyncService.cs:506-508` |
| `OseonArticleCategories` (:15) | OSEON-Artikelkategorie-Sync | `SyncWorker.cs:101`, `:105` |
| `EnaioDms` (:16) | enaio DMS-Sync | `SyncWorker.cs:140`, `:144` |
| `Holiday` (:17) | Feiertags-Sync | `ServiceSettingDefinitions.cs:79` („Feiertags-Sync aus Nager.Date aktiv“); der Log-Text `SyncWorker.cs:296` sagt englisch „Holiday-Sync“ |
| `CoatingDetection` (:18) | Lackierteil-Erkennung | `SyncWorker.cs:247`, `:259` |
| `FaWorkStepDetection` (:19) | FA-Arbeitsgang-Erkennung (BOM-Cache) | `SyncWorker.cs:214` („FA-Arbeitsgang-Erkennung (aus BOM-Cache)“), `:221` |
| `ProductionOrder` (:20) | Produktionsaufträge-Sync | `SyncWorker.cs:47`, `:50` |
| `ProductionOrderReconciliation` (:21) | FA-Reconciliation (Storno verwaister FAs) | kein eigener Log-Text; `SageImportService.cs:205` („FA-Reconciliation“), `ServiceSettingDefinitions.cs:32` |
| `Article` (:22) | Artikel-Sync | `SyncWorker.cs:87`, `:89` |
| `FaZusatzinfo` (:23) | FA-Zusatzinfo-Sync | `SyncWorker.cs:64`, `:75` |
| `SageLagerbuchung` (:24) | Sage-Lagerbuchung | `SageBookingWorker.cs:215` |
| `FaHierarchy` (:25) | FA-Hierarchie-Sync | `SyncWorker.cs:339`, `:344` |
| `HierarchieUmstellung` (:26) | FA-Hierarchie-Umstellung | Web-Aktion, kein Dienst: Seitentitel `Views/HierarchieUmstellung/Index.cshtml:3` |
| `FaMaterialization` (:27) | FA-Materialisierung | `SyncWorker.cs:359`, `:363` |
| `WorkOperationStructureDetection` (:28) | Arbeitsgang-Erkennung (Struktur) | `SyncWorker.cs:417`, `:421` („WorkOperation-Arbeitsgang-Erkennung (Struktur)“, Klassenpräfix weggelassen) |
| `ProductionWorkplaceSync` (:29) | Werkbank-Anlage (Sage-Arbeitsplätze) | `SyncWorker.cs:380`, `:384` |
| `OperationConfigSync` (:30) | Arbeitsgang-Abgleich (Sage-View) | `SyncWorker.cs:397`, `:401` |
| `PartRequisitionEmail` (:33) | Bedarfsmeldungs-E-Mail-Versand | `SyncWorker.cs:166` |
| `WarehouseRequisitionEmail` (:34) | Lagerbestellungs-E-Mail-Versand | `SyncWorker.cs:184` |
| `BdeAutoPause` (:35) | BDE-AutoPause | `SyncWorker.cs:274`, `:285` |
| `CleanupActivityLog` (:38, Wert `CleanupAktivitaetsprotokoll`) | Aktivitätsprotokoll-Bereinigung | `CleanupWorker.cs:63` |

Unbekannter Name (Altzeile, B3) → Anzeige = technischer Name. Alle 25 Anzeigenamen sind verschieden.

**A3 — Auswahlfeld „Aktivität“.** Option-Wert = technischer Name (URL `?service=OperationConfigSync` bleibt gültig, Lesezeichen
brechen nicht), Option-Text = Anzeigename, `title` = technischer Name; Reihenfolge alphabetisch nach Anzeigename (Rückfrage 1).

**A4 — Tabellenspalte „Aktivität“.** Zelltext = Anzeigename, `title` = technischer Name. Der CSV-Export übernimmt den Zelltext
(`csv-export.js:78-97`), also den Anzeigenamen.

**A5 — Spaltenfilter „Aktivität“.** Trifft eine Zeile, wenn der Filter den Anzeigenamen **oder** den technischen Namen ihrer
Konstante trifft (Rückfrage 3). Damit findet `abgleich` den Arbeitsgang-Abgleich und `OperationConfig%` dasselbe.

## Technischer Lösungsentwurf

**Muster:** `StockMovementRepository.GetMovementHistoryAsync` (`StockMovementRepository.cs:343-359`) mit
`ApplyMovementColumnFilter` (`:564-592`). Das ist die einzige bestehende Liste, die (1) eine wachsende Protokolltabelle mit
`DateTime Timestamp` in SQL paginiert, (2) den Zeitpunkt per `ColumnFilterSql.DatePredicate` in SQL filtert (`:575-581`) und
(3) eine Spalte mit Anzeigenamen über `ColumnFilterSyntax.MatchingValues` → `IN`/`NOT IN` filtert (`:568-574`, Bewegungsart).
Genau diese drei Fälle hat das Aktivitäts-Protokoll. `ProductionOrdersController.Index` (CLAUDE.md-Referenz) passt nicht: Er
filtert berechnete Termine in C# und lädt dafür ungepagt (`ProductionOrdersController.cs:87-129`) — beim Protokoll ist nichts
berechnet, In-Memory wäre hier reine Volllast.

1. **`ISyncLogRepository.GetPagedAsync`** bekommt `IReadOnlyDictionary<string, string>? columnFilters` (einziger Aufrufer:
   `SyncLogController.cs:36`). Ablauf: Filterkarte wie heute (`SyncLogRepository.cs:35-41`) → je Spaltenfilter
   `ColumnFilterTypes.Of("SyncLog/Index", key)` + `ColumnFilterSyntax.Parse`, leere überspringen → `ApplyColumnFilter` →
   `if (page < 1) page = 1;` → `CountAsync` → `OrderByDescending(Timestamp).Skip.Take`.
   `ColumnFilterTypes.Of("SyncLog/Index"` muss im Code stehen, sonst schlägt
   `ColumnFilterTypesDriftTests.JedeServerListeMitTypen_ReichtIhrRegisterWeiter` an (`ColumnFilterSyntaxTests.cs:253-264`);
   das Register hat den Eintrag schon (`ColumnFilterSyntax.cs:69`, `("Timestamp", D)`).
2. **`internal static ApplyColumnFilter(IQueryable<SyncLog>, string key, ParsedColumnFilter f)`** (internal wie
   `ApplyMovementColumnFilter`, für den `ToQueryString`-Test), Schlüssel ohne Groß/Klein wie die bisherige Map
   (`SyncLogController.cs:59`):
   - `Timestamp`: `ColumnFilterSql.DatePredicate<SyncLog>(f, x => x.Timestamp)`; ohne lesbares Datum positiv leer, negiert
     alle — wörtlich wie `StockMovementRepository.cs:578-580`.
   - `Service`: `MatchingValues(SyncLogServices.All, SyncLogServices.DisplayName, f)` ∪ `MatchingValues(SyncLogServices.All,
     s => s, f)` → `f.Negate ? !set.Contains(x.Service) : set.Contains(x.Service)`; leere Menge positiv = keine Treffer.
     `ponytail:` IN-Liste nur über die bekannten Konstanten; Altnamen bräuchten zusätzlich ein Text-Prädikat auf der Spalte
     (Rückfrage 3).
   - `Level`, `Reference`, `Message`: `ColumnFilterSql.Predicate<SyncLog>(f, x => x.<Spalte>)` (null-sicher, kein `LOWER`,
     EF maskiert `%`/`_`).
   - unbekannter Schlüssel: Abfrage unverändert.
3. **`SyncLogController.Index`**: `ReadFromQuery` vor dem Repository-Aufruf, Filter durchreichen, `Entries = rows`;
   `ColumnFilterHelper.Apply` und `SyncLogColumnMap` (`:58-66`) entfallen ersatzlos. `AvailableServices` =
   `SyncLogServices.All` sortiert nach `DisplayName` (`StringComparer.OrdinalIgnoreCase`; keine feste Request-Kultur,
   [[fallstricke]] §22).
4. **`SyncLogServices`**: `public static IReadOnlyDictionary<string, string> DisplayNames` (Tabelle A2) und
   `public static string DisplayName(string service)` mit Rückfall auf `service`. Kein zweiter Ort, kein Enum, keine DB.
5. **View** `Views/SyncLog/Index.cshtml`: Option `<option value="@s" title="@s">@SyncLogServices.DisplayName(s)</option>`
   (`:14`), Zelle `<td title="@e.Service">@SyncLogServices.DisplayName(e.Service)</td>` (`:59`). Für die View-Änderung ist der
   Skill `frontend-design` Pflicht (CLAUDE.md); Leitplanke Konsistenz — nur Text und `title`, kein neues Element.
6. **Hilfe** `Views/Help/Index.cshtml:1940-1958`: Rolle „nur `admin`“, Namensliste als deutsche Anzeigenamen (technischer
   Name in Klammern), Satz „Die Spaltenfilter durchsuchen alle Einträge, nicht nur die angezeigte Seite“.

**Groß/Klein:** In SQL entscheidet die Kollation, kein `LOWER` ([[fallstricke]] §26). Heute ist die Suche im Speicher
groß/klein-unabhängig (`ColumnFilterSyntax.cs:348`); nach der Umstellung nur noch bei CI-Kollation der `SyncLogs`-Spalten
(P-2). Tests unter EF-InMemory vergleichen ordinal — Testdaten für Text-Filter klein schreiben.

**Last:** `Meldung enthält` wird ein `LIKE '%x%'` auf `NVARCHAR(1000)` ohne Index, also ein Scan über die Tabelle. Bei
Aufbewahrung 180 Tage (`Cleanup:AktivitaetsprotokollAufbewahrungTage`) für eine Admin-Seite vertretbar; Größe misst P-3.

### Andere server-gefilterte Listen (Auftrag 3)

Gesucht: alle `data-server-column-filter="true"` unter `Views/` (38 Tabellen in 35 Views) und je Action, ob der
Spaltenfilter nach dem Paging läuft oder die Trefferzahl ungefiltert ist. „Volle Menge“ = die Quelle ist ungekappt
(`GetAll…`/`int.MaxValue`), gefiltert wird vor `Skip/Take`, Trefferzahl aus der gefilterten Menge.

| Liste (View) | Betroffen | Beleg |
|---|---|---|
| **SyncLog/Index** | **ja** | Seite aus `SyncLogController.cs:36` (`SyncLogRepository.cs:43-48`), danach `Apply` `:39`, `TotalCount` ungefiltert `:53` |
| StockMovements/Index | nein | Filter in SQL `StockMovementRepository.cs:343-352` vor Count `:354` und Skip `:356-359` |
| Articles/Index | nein | `ArticleRepository.cs:39-45`: `BuildArticleQuery` mit Spaltenfiltern (`:64-72`) vor Count/Skip |
| BdeBookings/Index | nein | SQL-Filter in Liste und Count gleich (`BdeBookingRepository.cs:95`, `:107`); berechnete Spalten → SQL ungepagt, C#-Filter, dann Skip (`BdeBookingsController.cs:61-105`) |
| MissingParts/Index | nein (Spaltenfilter) | Spaltenfilter in SQL vor Count/Skip `WarehouseRequisitionRepository.cs:413-427`; **Zusatzfund** Werkbank-Nachfilter `MissingPartsController.cs:104-112` → Rückfrage 4 |
| MissingPartsLager/Index | nein | gleiches Repository (`MissingPartsLagerController.cs:44-45`) |
| ProductionOrders/Index | nein | Postfilter erzwingen ungepagtes SQL `ProductionOrdersController.cs:123-137`, SQL-Filter vor Count/Skip `ProductionOrderRepository.cs:39-49`, `:64-75`; C#-Paging `ProductionOrdersController.cs:376`, `:445` |
| PickingLeitstand/Index | nein | gleiches Muster `PickingLeitstandController.cs:86-98`, `:223-236`, `:294-305` |
| FaWorklist/Index | nein | volle Menge, `Apply` vor Paging `FaWorklistController.cs:289-291`, `:307-331` |
| FaCompletion/Index | nein | volle Menge `FaCompletionController.cs:130-157`, Paging `:174-180`, `:208-212` |
| Picking/Index | nein | volle Menge `PickingController.cs:158-196`, Paging `:211-217`, `:237-241` |
| Tracking/Index | nein | volle Menge `TrackingController.cs:70`, `Apply` `:116-120`, Paging `:139-144`, `:169-173` |
| FaHierarchyBeschichtung/Index, FaHierarchyKommissionierListen/Index, FaHierarchyVormontage/Index | nein | alle Knoten geladen (`BeschichtungsauftragService.cs:100-101`), gemeinsamer Builder filtert vor Gruppierung und Paging `FaHierarchyListBuilder.cs:136-150` (Aufrufe `BeschichtungsauftragService.cs:103`, `KommissionierListenService.cs:106`, `VormontageService.cs:173`) |
| FaHierarchyBeschichtung/Summiert | nein | `BeschichtungsauftragService.cs:161-163`, `Apply` `:219` vor Skip `:224` |
| FaHierarchyKommissionierListen/Summiert | nein | `KommissionierListenService.cs:193` vor Skip `:201` |
| FaHierarchyVormontage/Summiert | nein | `VormontageService.cs:260` vor Skip `:267` |
| Beschichtungsauftraege/Index | nein | `BeschichtungsauftraegeController.cs:84` vor Skip `:88` |
| PartRequisitions/Index | nein | Quelle `int.MaxValue` `PartRequisitionsController.cs:65`, `Apply` `:85`, Skip `:87` |
| SageBookingQueue/Index | nein | Quelle ungekappt `SageBookingQueueRepository.cs:76-87`, `Apply` `SageBookingQueueController.cs:75`, `TotalCount = filtered.Count` `:89` |
| WarehousePicking/Index | nein | Quelle `int.MaxValue` `WarehousePickingController.cs:73`, `Apply` `:79`, Skip `:81` |
| WarehouseRequisitions/Index | nein | `GetForUserAsync` `WarehouseRequisitionsController.cs:68`, `Apply` `:87`, Skip `:89` |
| StockOverview/Index | nein | volle Menge `StockOverviewController.cs:47-60`, `Apply` `:69`, Skip `:72` |
| StorageLocations/Index | nein | `GetAllOrderedAsync` `StorageLocationsController.cs:52`, `Apply` `:62`, Skip `:75` |
| Users/Index | nein | `UsersController.cs:77`, `:82`, `:93` |
| Roles/Index | nein | `RolesController.cs:44`, `:58`, `:67` |
| Workstations/Index | nein | `WorkstationsController.cs:48`, `:53`, `:61` |
| WorkSteps/Index | nein | `WorkStepsController.cs:44`, `:49`, `:58` |
| ProductionWorkplaces/Index | nein | `ProductionWorkplacesController.cs:67`, `:72`, `:80` |
| OrderRecipients/Index | nein | `OrderRecipientsController.cs:43`, `:48`, `:56` |
| OperationConfigs/Index | nein | `OperationConfigsController.cs:66`, `:67`, `:73` |
| ArticleAttributes/Index | nein | `ArticleAttributesController.cs:45`, `:50`, `:52` |
| ArticleCategories/Index | nein | `ArticleCategoriesController.cs:30`, `:48`, `:57` |
| FaAttributes/Index | nein | `FaAttributesController.cs:63`, `:68`, `:77` |
| BdeMasterData/Index (3 Tabellen) | nein | `BdeMasterDataController.cs:73-76`, `Apply` `:84-90`, Skip `:104-110` |

Damit ist die Negativaussage über alle Server-Listen belegt: Nur das Aktivitäts-Protokoll filtert Spalten nach dem Paging.

### Spaltenfilter-Syntax und CSV-Export auf dieser Liste (Auftrag 4)

- **Syntax:** Der Repository-Weg parst über `ColumnFilterSyntax.Parse` mit dem Typ aus dem Register
  (`ColumnFilterTypes.Of("SyncLog/Index", key)`) und baut nur über `ColumnFilterSql` bzw. `MatchingValues` — keine eigene
  Auswertung ([[fallstricke]] §26). Browser-Seite unverändert: `<th data-date-filter data-col-key="Timestamp">`
  (`Views/SyncLog/Index.cshtml:41`) entspricht dem Register (`ColumnFilterSyntax.cs:69`, Drift-Test
  `ColumnFilterSyntaxTests.cs:241-247`).
- **Kollation je Spalte:** Pflicht-Prüfschritt P-2 für `SyncLogs` (`Service`, `Level`, `Message`, `Reference`).
- **CSV-Export:** bekommt die Filter über die URL — `table-filter.js:54-65` schreibt jeden Spaltenfilter als `colf_<key>` in
  die Adresse (und löscht `page`), die Filterkarte steht per GET-Formular als `service`/`level`/`reference` darin
  (`Views/SyncLog/Index.cshtml:7-35`); `csv-export.js:195-208` lädt genau diese URL mit `pageSize=0&page=1` und liest die
  zurückgelieferte Tabelle. Nach der Änderung filtert dieselbe Action in SQL vor `Take(5000)` → der Export enthält **alle**
  Treffer bis 5000, und `IsCappedAtAll` vergleicht die **gefilterte** Trefferzahl (`PaginationState.cs:32-33`) → Hinweis nur,
  wenn wirklich mehr als 5000 Treffer.

## Migrations-/SQL-Auswirkungen

Keine. Kein neues Feld, keine neue Tabelle, kein Index; `SQL/00_FreshInstall.sql` und `SQL/AgentJobs/` unberührt (kein
Agent-Job liest oder schreibt `SyncLogs`, Grep in `SQL/` nur Tabellen-/Index-Anlage `00_FreshInstall.sql:1075-1116`). Kein
`OBJECT_ID`-Guard nötig. Ein Index auf `Message` ist bewusst nicht vorgesehen (LIKE `%x%` nutzt ihn nicht).

**Prüfabfragen für den Menschen (beide Server, AKE und IDEAL; vor der Abnahme):**

- **P-1** (aus dem Backlog, ob die genannten Läufe auf IDEAL-Test schreiben):
  ```sql
  SELECT TOP 30 Timestamp, Service, Level, LEFT(Message, 200) AS Message
  FROM dbo.SyncLogs
  WHERE Service IN ('OperationConfigSync','ProductionWorkplaceSync','WorkOperationStructureDetection',
                    'FaHierarchy','FaMaterialization')
  ORDER BY Timestamp DESC;
  ```
- **P-2** (Kollation; ohne `…_CI_…` ist „Groß/Klein egal“ in den Spaltenfiltern **unverifiziert**):
  ```sql
  SELECT DATABASEPROPERTYEX(DB_NAME(), 'Collation') AS DbCollation;
  SELECT c.name, c.collation_name FROM sys.columns c JOIN sys.tables t ON t.object_id = c.object_id
  WHERE t.name = 'SyncLogs' AND c.collation_name IS NOT NULL;
  ```
  Erwartet überall `…_CI_…`, sonst STOPP und melden.
- **P-3** (Größe und Altnamen, für Last und Rückfrage 3):
  ```sql
  SELECT COUNT(*) AS Zeilen, MIN(Timestamp) AS Aeltester FROM dbo.SyncLogs;
  SELECT Service, COUNT(*) AS Zeilen FROM dbo.SyncLogs GROUP BY Service ORDER BY Service;
  ```
  Jeder `Service`, der nicht in `SyncLogServices.All` steht, ist ein Altname (zeigt technisch, wird vom Positivfilter
  „Aktivität“ nicht getroffen).

## Audit-Feld-Auswirkungen

Keine. `SyncLog` ist keine `AuditableEntity` (`SyncLog.cs:5`), die Änderung liest nur; die Listen-Action schreibt im GET-Pfad
weiterhin nichts (Voraussetzung des CSV-Exports, [[fallstricke]] §23).

## Rollen und Zugriff

Unverändert `[RequireAdminAccess]` auf Klassenebene (`SyncLogController.cs:11`). Keine neue Rolle, kein neuer Filter, kein
Feature-Gate; `Users/RoleOverview.cshtml` und `secondbrain/codebase/controller.md` bleiben. Nur die falsche Rollenangabe der
Hilfe wird korrigiert (B5).

## Listen-View-Pattern

Keine neue Tabelle. Für die bestehende gilt nach der Änderung: Pagination über `PageSize.Resolve` + `PaginationState` +
`_Pagination` (unverändert), Server-Spaltenfilter jetzt **vor** dem Paging mit gefilterter Trefferzahl
([[0005-listen-view-pattern-mit-server-side-spaltenfilter]]), alle `<th>` mit `data-col-key` (`Views/SyncLog/Index.cshtml:41-45`),
Typ-Register gepflegt, CSV-Export automatisch. Offen und bewusst nicht Teil dieser Spec: Spaltenauswahl und
`card filter-card` (Out-of-Scope).

## Akzeptanzkriterien

1. Bei 30 Protokollzeilen und Seitengröße 25 findet ein Spaltenfilter „Meldung“ auf einen Text, der nur in Zeile 28 (Seite 2)
   steht, diese Zeile auf Seite 1; Trefferzahl = 1, eine Seite.
2. `TotalCount` der Seitenleiste ist bei jedem aktiven Spaltenfilter die Anzahl der Treffer, ohne Spaltenfilter wie heute.
3. Spaltenfilter „Referenz“, „Stufe“ und „Meldung“ werden in SQL übersetzt: `ToQueryString` gegen einen SQL-Server-Context
   enthält für `Message` ein `LIKE` und kein `LOWER(`, ein `_` im Filterwert erscheint maskiert (`ESCAPE`, wie
   `ColumnFilterSyntaxTests.cs:282`); für `Service` eine Mengen-Bedingung auf `[Service]`.
4. Spaltenfilter „Zeitpunkt“ `01.10.2026`, `10.2026`, `2026`, `>=01.10.2026`, `01.10.2026...03.10.2026` liefern die Zeilen
   des Tages/Monats/Jahres/Bereichs (Grenzen tageweise); ein unlesbarer Wert liefert positiv keine, negiert alle Zeilen.
5. Spaltenfilter „Aktivität“ `abgleich` liefert die Zeilen von `OperationConfigSync`; `OperationConfig%` ebenso;
   `!abgleich` alle anderen; `werkbank` die von `ProductionWorkplaceSync` und `OseonWorkplaces`.
6. Jede Konstante in `SyncLogServices.All` hat einen nicht leeren Anzeigenamen; alle Anzeigenamen sind verschieden
   (automatischer Test). `DisplayName("Unbekannt")` liefert `"Unbekannt"`.
7. Das Auswahlfeld „Aktivität“ zeigt die Anzeigenamen alphabetisch, jede Option hat als Wert und `title` den technischen
   Namen; `?service=OperationConfigSync` filtert wie bisher und hält die Auswahl.
8. Die Spalte „Aktivität“ zeigt den Anzeigenamen, `title` = technischer Name; eine Zeile mit unbekanntem Namen zeigt den
   technischen Namen.
9. CSV-Export mit aktivem Spaltenfilter enthält alle Treffer (nicht nur die der neuesten 5000 Zeilen); der Kappungshinweis
   erscheint nur, wenn die gefilterte Trefferzahl 5000 übersteigt.
10. Filterkarte und Spaltenfilter wirken zusammen (UND): `?service=Lagerplatz&colf_Message=konflikt` liefert nur
    Lagerplatz-Zeilen mit „konflikt“.
11. `?page=0` und `?page=-3` liefern Seite 1 statt eines Fehlers.
12. `ColumnFilterTypesDriftTests` (alle drei) sowie alle bestehenden `SyncLog*`-Tests sind grün; `SyncLogController` enthält
    keinen In-Memory-Spaltenfilter mehr.
13. Die Hilfe nennt Rolle `admin` und die Anzeigenamen; kein technischer Name ohne Anzeigename.
14. Nicht-Admin erhält auf `/SyncLog` wie bisher keinen Zugriff.

## Test-Szenarien

**Automatisiert (xUnit, EF-InMemory, Testdaten klein geschrieben):**
- `SyncLogRepositoryTests`: Paging mit Spaltenfilter (AK 1/2), Zeitpunkt (AK 4), Aktivität per Anzeige- und technischem Namen
  und Negation (AK 5), Filterkarte + Spaltenfilter (AK 10), Seiten-Guard (AK 11).
- SQL-Übersetzung per `ToQueryString` gegen nie verbundenen SQL-Server-Context (Muster `ArticleColumnFilterSqlTests`,
  `ColumnFilterSyntaxTests.cs:269-285`) für `ApplyColumnFilter` (AK 3).
- `SyncLogControllerTests`: Query mit `colf_Message` über `DefaultHttpContext`, `Entries` und `Pagination.TotalCount` gefiltert;
  `AvailableServices` nach Anzeigename sortiert (AK 2/7).
- `SyncLogServices`: Vollständigkeit/Eindeutigkeit/Rückfall (AK 6).

**Manuell, neues Kapitel `TS-NN — Aktivitäts-Protokoll: Spaltenfilter über alle Einträge, deutsche Aktivitätsnamen (vX.Y.Z)`
in `docs/TESTSZENARIEN.md`** (Index [[testszenarien-index]] nachziehen; Bezug zu Kapitel 27/29/44/54):

- **Vorbedingungen:** Admin-Anmeldung; P-2 erledigt (CI), P-3 ausgeführt (Zeilenzahl > 50, Altnamen notiert); Seitengröße 25.
- **TS-NN.1 Älterer Eintrag per Meldung:** Eine Meldung von Seite 3 oder älter wählen, in „Meldung“ einen Teil davon
  filtern (ENTER). Erwartet: Eintrag auf Seite 1, Trefferzahl stimmt mit `SELECT COUNT(*) … WHERE Message LIKE '%…%'` überein.
- **TS-NN.2 Aktivität per deutschem Namen:** Spaltenfilter „Aktivität“ `abgleich`, dann `!abgleich`, dann `werkbank`.
  Erwartet wie AK 5; Tooltip der Zelle zeigt den technischen Namen.
- **TS-NN.3 Auswahlfeld:** Liste aufklappen. Erwartet: deutsche Namen alphabetisch; „Arbeitsgang-Abgleich (Sage-View)“ wählen →
  URL `service=OperationConfigSync`, nur diese Zeilen; Lesezeichen mit altem `?service=…` funktioniert.
- **TS-NN.4 Zeitpunkt:** `heute als TT.MM.JJJJ`, `MM.JJJJ`, `>=TT.MM.JJJJ`, `TT.MM.JJJJ...TT.MM.JJJJ`. Erwartet: nur Zeilen im
  Zeitraum, Trefferzahl gefiltert.
- **TS-NN.5 CSV:** Mit aktivem Spaltenfilter „CSV“ drücken. Erwartet: Datei enthält genau die Trefferzahl an Zeilen, Spalte
  Aktivität mit deutschem Namen, kein Kappungshinweis bei weniger als 5000 Treffern.
- **TS-NN.6 Groß/Klein:** `FEHLGESCHLAGEN` und `fehlgeschlagen` in „Meldung“. Erwartet: gleiche Treffer (belegt P-2).
- **TS-NN.7 Kombination:** Filterkarte „Stufe = Warning“ + Spaltenfilter „Aktivität“ `lager%`. Erwartet: nur Warnungen der
  Lager-Läufe.
- **Negativfälle:** unlesbares Datum `32.13.2026` in „Zeitpunkt“ → keine Zeile, Feld markiert ungültig; `!32.13.2026` → alle;
  `?colf_Unbekannt=x` → Liste unverändert; `?page=0` → Seite 1; Altname aus P-3 (falls vorhanden) erscheint technisch;
  Anmeldung ohne Admin-Rolle → kein Zugriff.

## Etappen (nur bei epic: true)

Entfällt (`epic: false`).

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | – | – | – |

## Deploy

> [!success] Final (QA-Lauf 2026-10-28, aus `git diff 2c1e3a6d..badbedcc`)
> Geändert: `IdealAkeWms/` (Controller, Repository + Interface, `SyncLogServices`, Views SyncLog/Help/Changelog, `AppVersion.cs`),
> `IDEALAKEWMSService/AppVersion.cs` (nur Versionsstand), `IdealAkeWms.Tests/` (neue Testdatei), `docs/TESTSZENARIEN.md`. Keine Datei
> unter `*/Migrations/`, nichts unter `SQL/`.

- **Web-App:** ja.
- **Service:** ja — nur `IDEALAKEWMSService/AppVersion.cs` 1.69.0 → 1.70.0. Der Dienst bindet `IdealAkeWms` ein (geänderter
  `SyncLogRepository` wird mitkompiliert), nutzt davon aber nur `DeleteOlderThanAsync` (`ActivityLogCleanupService`) – fachlich
  keine Änderung. Publish, damit Web und Dienst denselben Versionsstand melden; kein Wartungsfenster nötig.
- **Migration:** nein. Kein Einmal-Skript.
- **Reihenfolge:** P-2/P-3 auf dem Testsystem → Web publizieren und deployen → Dienst stoppen, publizieren, starten → TS-102 →
  Merge (Schranke 2).
- **Publish-Befehle (im Worktree `C:\Git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5`):**
  ```
  dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
  dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService
  ```
- Nach dem Merge nur dann erneut aus `main` publizieren, wenn der Merge getestete Dateien mit parallelen `main`-Änderungen
  zusammengeführt hat.

## Offene Rueckfragen

1. **Anzeigenamen-Tabelle (A2) übernehmen?** Inkl. „Feiertags-Sync“ statt des englischen Log-Texts „Holiday-Sync“,
   „FA-Arbeitsgang-Erkennung (BOM-Cache)“ vs. „Arbeitsgang-Erkennung (Struktur)“ und alphabetischer Sortierung im Auswahlfeld.
   *Empfehlung: ja* — die Namen stammen aus Dienst-Log bzw. Service-Einstellungen, die zwei Erkennungen sind ohne den Zusatz
   in Klammern nicht unterscheidbar, alphabetisch findet man 25 Einträge schneller als in Code-Reihenfolge.
2. **Zeitpunkt nur tageweise in SQL?** Uhrzeit-Teilsuche (`14:3`) wirkt danach nicht mehr. *Empfehlung: ja* — gleiche
   Bedeutung wie in der Bewegungshistorie (`StockMovementRepository.cs:575-581`); die Alternative lädt bei jeder
   Zeitpunkt-Filterung das ganze Protokoll in den Speicher.
3. **Aktivitäts-Filter nur über bekannte Konstanten?** Altnamen (nicht in `All`) trifft der Positivfilter nicht.
   *Empfehlung: ja* — Altzeilen altern über die Bereinigung aus; P-3 zeigt, ob es überhaupt welche gibt. Falls doch nötig:
   zusätzliches Text-Prädikat auf `Service` (wenige Zeilen).
4. **Fehlteile „nur meine Werkbänke“ (`MissingPartsController.cs:104-112`) mitbeheben?** *Empfehlung: eigener
   Backlog-Eintrag* — kein Spaltenfilter, die Repository-Signatur (`WarehouseRequisitionRepository.cs:390-395`, heute ein
   `int? workplaceFilter`) müsste eine Werkbank-Liste annehmen, und die Fehlteile-Szenarien ändern sich; nicht klein.

## Umsetzung v1.70.0 (Dev-Lauf 2026-10-28)

Basis `2c1e3a6d` (v1.69.0). Commit `badbedcc` (wip feature-complete). **v1.70.0, TS-102.** Keine Migration, kein Index, keine Änderung
an den Schreibwegen im Dienst. Bug-Datensatz [[2026-10-28-aktivitaetsprotokoll-spaltenfilter-nach-paging-bug]].
Vorab `frontend-design` aufgerufen; Leitplanke Konsistenz – nur Text und `title`, kein neues Element, keine neue Farbe.

- **`SyncLogServices`:** `DisplayNames` (Tabelle A2, 25 Einträge) und `DisplayName(service)` mit Rückfall auf den technischen Namen.
- **`SyncLogRepository.GetPagedAsync(…, columnFilters = null)`:** je Spaltenfilter `ColumnFilterSyntax.Parse` mit
  `ColumnFilterTypes.Of("SyncLog/Index", key)`, dann `internal static ApplyColumnFilter` **vor** `if (page < 1) page = 1;` → Count →
  Skip/Take. `Timestamp` über `ColumnFilterSql.DatePredicate` (unlesbar: positiv leer, negiert alle), `Service` über
  `MatchingValues` (Anzeigename ∪ technischer Name) → `IN`/`NOT IN` über `SyncLogServices.All`, `Level`/`Reference`/`Message` über
  `ColumnFilterSql.Predicate`; Schlüssel ohne Groß/Klein, unbekannter Schlüssel ohne Wirkung. Parameter optional, weil
  `ActivityLogCleanupService` dasselbe Interface nutzt (Moq-Tests im Dienst unverändert).
- **`SyncLogController.Index`:** `ReadFromQuery` vor dem Repository-Aufruf, `ColumnFilterHelper.Apply` und `SyncLogColumnMap` entfernt,
  `CurrentPage = Math.Max(page, 1)`, Auswahlliste nach `DisplayName` (`OrdinalIgnoreCase`).
- **View:** Option `value`/`title` technisch, Text deutsch; Zelle Text deutsch, `title` technisch.
- **Hilfe:** Rolle nur `admin`, Namensliste deutsch (technisch in Klammern, alphabetisch), Absatz „Spaltenfilter (seit v1.70.0)“.
  Changelog-Karte v1.70.0, beide `AppVersion.cs`. TS-102 (102.1–102.9).
- **Tests** `IdealAkeWms.Tests/Repositories/SyncLogColumnFilterTests.cs` (12): Meldung Seite 2 → Seite 1 + Trefferzahl (AK 1/2),
  Zeitpunkt Tag/Monat/Jahr/`>=`/Bereich/unlesbar/negiert (AK 4), Aktivität `abgleich`/`OperationConfig%`/`!abgleich`/`werkbank`
  (AK 5), Altname (Antwort 3), Filterkarte + Spaltenfilter (AK 10), `page` 0/-3 (AK 11), unbekannter Schlüssel, SQL-Übersetzung per
  `ToQueryString` (LIKE ohne `LOWER(`, `ESCAPE`, Menge auf `[Service]`, AK 3), Anzeigenamen vollständig/eindeutig/Rückfall (AK 6),
  Controller mit `colf_Message` + Sortierung (AK 2/7), Quelltext-Wächter ohne In-Memory-Filter (AK 12).

**Nachweis:** `dotnet build IdealAkeWms.slnx` 0 Fehler, 28 Warnungen (unverändert). `dotnet test`: Web **2213 bestanden, 1
übersprungen** (vorher 2201), Dienst **328 bestanden**.

**Abweichungen / Hinweise:**
- Kein geänderter Bestandstest; `SyncLogControllerTests.Index_FiltersByServiceAndLevel` unverändert grün.
- Groß/Klein in SQL **unverifiziert bis P-2** (Kollation); InMemory-Tests nutzen klein geschriebene Daten.
- Spec-Datei lag beim Start noch unter `specs/entwurf/` (Status schon Freigegeben) und wurde vom Dev-Lauf nach `specs/freigegeben/`
  verschoben (Anleitung im Antwortblock; Wikilinks gehen über den Namen).
- View nicht gerendert (Web lokal nicht gestartet): Anzeige prüft TS-102.2/102.3/102.9.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. Anzeigenamen-Tabelle A2 inkl. Sortierung übernehmen? → **Ja, wie empfohlen** (Gerald 2026-10-28).
2. Zeitpunkt-Spaltenfilter nur tageweise in SQL? → **Ja, wie empfohlen** (Gerald 2026-10-28).
3. Aktivitäts-Spaltenfilter nur über bekannte Konstanten? → **Ja, so lassen, wie empfohlen** (Gerald 2026-10-28).
4. Fehlteile „nur meine Werkbänke“ mitbeheben oder eigener Backlog-Eintrag? → **Eigener Backlog-Eintrag, wie empfohlen** (Gerald 2026-10-28): [[2026-10-28-fehlteile-nur-meine-werkbaenke-trefferzahl]].

## QA-Nachweis v1.70.0

QA-Lauf 2026-10-28 (qa-agent, unabhängig vom Dev-Lauf), Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, HEAD `badbedcc`,
Diff `git diff 2c1e3a6d..badbedcc` (11 Dateien, +330/−29). Review synchron in diesem Lauf, nicht delegiert.

### 1. Build und Tests (im Worktree ausgeführt)

- `dotnet build IdealAkeWms.slnx`: **0 Fehler, 28 Warnungen** (Bestand, z. B. CS8601 `BdeTerminalAuftraegeTests.cs:23`).
- `dotnet test`:
  - `IdealAkeWms.Tests`: **2213 bestanden, 1 übersprungen, 0 fehlgeschlagen** (gesamt 2214; vorher 2201 → +12 = neue `SyncLogColumnFilterTests`, 11 Methoden, davon eine Theory mit 2 Fällen).
  - `IDEALAKEWMSService.Tests`: **328 bestanden, 0 übersprungen, 0 fehlgeschlagen**.

### 2. Code-Review (Befunde und Behandlung)

Geprüft: `SyncLogRepository.GetPagedAsync`/`ApplyColumnFilter`, `ISyncLogRepository`, `SyncLogController.Index`, `SyncLogServices.DisplayNames`/`DisplayName`,
`Views/SyncLog/Index.cshtml`, `Views/Help/Index.cshtml` (Aktivitäts-Protokoll), `Views/Help/Changelog.cshtml`, beide `AppVersion.cs`, `docs/TESTSZENARIEN.md` TS-102,
neue `SyncLogColumnFilterTests.cs`; dazu der Umgebungscode `ColumnFilterSyntax.cs` (`Parse`, `ColumnFilterSql.Predicate`/`DatePredicate`, `MatchingValues`, Register `:69`),
Muster `StockMovementRepository.cs` (`GetMovementHistoryAsync`, `ApplyMovementColumnFilter`), `PaginationState.cs:32-33`, Drift-Tests `ColumnFilterSyntaxTests.cs:225-265`,
alle Nutzer von `ISyncLogRepository` (Web, Dienst `ActivityLogCleanupService`, Moq-Tests im Dienst).

Ergebnis je Prüfpunkt:
- **Spaltenfilter vor Count/Skip/Take:** ja. Reihenfolge Filterkarte → Spaltenfilter → `page < 1 → 1` → `CountAsync` → `OrderByDescending.Skip.Take` (`SyncLogRepository.cs:38-64`). Wörtlich das Muster der Bewegungshistorie.
- **SQL-Übersetzbarkeit:** `Service` als `set.Contains(x.Service)` / `!set.Contains(...)` über eine lokale `List<string>` (IN-Menge); `Timestamp` über `ColumnFilterSql.DatePredicate` (Konstanten-Grenzen, tageweise); `Level`/`Reference`/`Message` über `ColumnFilterSql.Predicate` (Contains/StartsWith/EndsWith/`==` auf Konstanten, null-sicher, kein `ToLower` auf der Spalte). Belegt per `ToQueryString` (Test `SqlUebersetzung_LikeOhneLower_EscapeUndMengeAufService`). Ausführung gegen echten SQL Server bleibt Manual-UAT.
- **Negation „Aktivität“:** `MatchingValues` liefert ohne Negation (`ColumnFilterSyntax.cs:332-336`), der Aufrufer baut `NOT IN` – korrekt, keine Doppel-Negation. Die Vereinigung Anzeigename ∪ technischer Name ist groß/klein-unabhängig ohne Kollationsbezug (Abgleich im Speicher gegen die Konstanten, `Matches` senkt beide Seiten).
- **Seiten-Guard:** im Repository vor `Skip`; Controller zeigt `CurrentPage = Math.Max(page, 1)` – beide Stellen konsistent.
- **Anzeigenamen gegen A2:** alle 25 Einträge zeichengleich zur Tabelle A2; Schlüssel = `SyncLogServices.All` (Test). Hilfe-Namensliste maschinell gegen `DisplayNames` verglichen: 25/25 identisch, alphabetisch.
- **View/Hilfe:** nur Text und `title`, Razor-kodiert; kein Wert in `<script>`, kein `hidden`/`d-*`, kein neues Element. Hilfe: Rolle nur `admin` (passt zu `[RequireAdminAccess]`), Absatz Spaltenfilter.
- **CSV-Export/Kappung:** Export lädt dieselbe Action mit `pageSize=0` → `Take(5000)` nach dem Filter; `IsCappedAtAll` vergleicht jetzt die gefilterte `TotalCount`. Logik schlüssig, Browser-Ablauf Manual-UAT.
- **Drift-Test ColumnFilterTypes:** `ColumnFilterTypes.Of("SyncLog/Index", key)` steht im Repository; alle drei `ColumnFilterTypesDriftTests` grün. `data-date-filter`/`data-col-key="Timestamp"` unverändert.
- **Defekt-Klassen der Checkliste:** keine stille `?? 0`-Ersetzung; Rückfall `DisplayName` → technischer Name ist spezifiziert (A2) und sichtbar; kein Parallel-Array-Binding; keine reine Browser-Validierung (Server-Mode, Filter ausschließlich serverseitig); eine Regel an einem Ort (ein Parser `ColumnFilterSyntax`, ein Namensort `SyncLogServices`).

Befunde:
- **Q-1 (Hinweis, nicht behoben):** Hilfe und TS-102.4 sagen „Uhrzeit-Teile wie `14:3` wirken nicht“. Tatsächlich liefert ein solcher Wert positiv **keine Zeile** (Teil wird `Contains`, `DatePredicate` = null → `Where(false)`, `SyncLogRepository.cs:77`), negiert alle – wie in der Bewegungshistorie. Formulierung „liefert keine Treffer“ wäre genauer; fachlich gewollt (Antwort 2). Kein Blocker.
- **Q-2 (Hinweis, Bestandsverhalten):** Vergleich/Bereich mit unlesbarem Datum (`>=32.13.2026`) wird beim Parsen verworfen (`Parts` leer → `IsEmpty` → Filter übersprungen), die Liste bleibt ungefiltert; nur der Browser markiert das Feld ungültig. Gleiches Verhalten wie Bewegungshistorie, nicht Teil dieser Spec. Prüfung in der Checkliste (Schritt 9).
- **Q-3 (niedrig, Testlücke):** AK 5 „`werkbank` liefert auch `OseonWorkplaces`“ ist nicht geseedet (Testdaten nur `ProductionWorkplaceSync`). Folgt rein aus `DisplayName` + `MatchingValues` (beide getestet); Manual-UAT TS-102.2 deckt es ab. Nicht nachgezogen (QA schreibt keinen Code).
- **Q-4 (kosmetisch):** `AppVersion.Date` steht in beiden `AppVersion.cs` noch auf `2026-10-27`, die Changelog-Karte nennt 28.10.2026.
- Keine Befunde der Schwere mittel/hoch.

### 3. Grenzen (geprüft am Diff)

- `MissingPartsController` nicht angefasst (kein Treffer in `git diff --name-only`); eigener Backlog [[2026-10-28-fehlteile-nur-meine-werkbaenke-trefferzahl]] vorhanden.
- Keine Migration (keine Datei unter `*/Migrations/`), kein Index, nichts unter `SQL/` (auch nicht `00_FreshInstall.sql`, `AgentJobs/`).
- Keine Änderung an SyncLogs-Schreibwegen im Dienst: einzige Dienst-Datei ist `IDEALAKEWMSService/AppVersion.cs`; `SyncRun`/`SyncLogger` unverändert. Interface-Parameter optional → `ActivityLogCleanupService` und dessen Moq-Tests unverändert grün.
- **Groß/Klein: unverifiziert bis Prüfabfrage P-2.** `ColumnFilterSyntax.Parse` senkt den Filterwert auf Kleinbuchstaben; in SQL vergleicht dann die Spaltenkollation. Bei `…_CS_…` treffen `Level` (`=warning` gegen `Warning`), `Reference` und `Message` mit Großbuchstaben nichts. Die Tests laufen unter EF-InMemory (ordinal) mit klein geschriebenen Daten und beweisen das Verhalten **nicht**. Ausnahme: „Aktivität“ ist kollationsunabhängig (Abgleich im Speicher).

### 4. Beweisart je Akzeptanzkriterium

| AK | Beweis | Test / Grund |
|---|---|---|
| 1 | automatisiert (InMemory) | `Meldung_AufSeite2_ErscheintAufSeite1_TrefferzahlGefiltert` |
| 2 | automatisiert | dto. + `Controller_SpaltenfilterAusQuery_TrefferzahlGefiltert_AuswahlNachAnzeigename` |
| 3 | automatisiert (`ToQueryString`) | `SqlUebersetzung_LikeOhneLower_EscapeUndMengeAufService`; echte Ausführung Manual-UAT |
| 4 | automatisiert (InMemory) | `Zeitpunkt_TagMonatJahrVergleichBereich_…`; Ergebnis auf SQL Server Manual-UAT TS-102.4 |
| 5 | teilweise automatisiert | `Aktivitaet_AnzeigenameOderTechnischerName_UndNegation` (ohne `OseonWorkplaces`, Q-3); Rest Manual-UAT TS-102.2 |
| 6 | automatisiert | `Anzeigenamen_VollstaendigNichtLeerEindeutig_Rueckfall` |
| 7 | teilweise | Sortierung automatisiert (Controller-Test); `value`/`title`/Auswahl halten = gerenderte View → **Manual-UAT** TS-102.3 |
| 8 | Manual-UAT | Rendering/Tooltip TS-102.2, Altname TS-102.8 (Rückfall selbst automatisiert) |
| 9 | Manual-UAT | Browser-JS `csv-export.js`, TS-102.5 |
| 10 | automatisiert (InMemory) | `FilterkarteUndSpaltenfilter_WirkenZusammen` |
| 11 | automatisiert (InMemory) | `SeiteKleiner1_LiefertSeite1` (0, −3); negativer OFFSET auf SQL Server Manual-UAT TS-102.8 |
| 12 | automatisiert | Drift-Tests grün, `Controller_OhneInMemorySpaltenfilter`, `SyncLogControllerTests` grün |
| 13 | Manual-UAT | TS-102.9 (Inhalt zusätzlich im Review maschinell gegen `DisplayNames` verglichen) |
| 14 | Manual-UAT | `[RequireAdminAccess]` unverändert (`SyncLogController.cs:11`), kein Test; TS-102.8 |

Nicht grün gezählt: AK 8, 9, 13, 14 sowie die Manual-Anteile von 3, 5, 7, 11 und jede Groß/Klein-Aussage (P-2).

### 5. Geänderte Bestandstests

Keine. `git diff --stat` zeigt unter `*.Tests/` nur die neue Datei `IdealAkeWms.Tests/Repositories/SyncLogColumnFilterTests.cs`; `SyncLogControllerTests`, `ColumnFilterSyntaxTests`, `ActivityLogCleanupServiceTests` unverändert und grün.

### 6. CLAUDE.md-Checkliste

- Migration / SQL / FreshInstall / AgentJobs: entfällt (keine Schema-Änderung) – bestätigt.
- Audit-Felder: entfällt (`SyncLog` ist keine `AuditableEntity`, nur Lesepfad).
- Version 1.70.0 in **beiden** `AppVersion.cs` + Anwender-Changelog-Karte v1.70.0: ja (Datum siehe Q-4).
- `docs/TESTSZENARIEN.md` TS-102 (102.1–102.9 inkl. Negativfälle) + Stand-Zeile: ja.
- Testindex: TS-102-Zeile fehlte – vom QA-Lauf in [[testszenarien-index]] nachgezogen.
- Bug-Datensatz [[2026-10-28-aktivitaetsprotokoll-spaltenfilter-nach-paging-bug]]: vorhanden.
- Feature-Map-Abschnitt v1.70.0 und Brain-Changelog [[2026-10-28-v1-70-0-aktivitaetsprotokoll-filter-namen]]: zum QA-Zeitpunkt bereits vorhanden – kein Befund.
- Neue Rolle/Filter: keine.

**Nachbesserung nach QA (Dev-Lauf, Commit `d7dc3d03`):** Q-1 Wortlaut Hilfe/TS-102.4 („`14:3`: positiv keine Zeile, mit `!` alle“),
Q-4 `AppVersion.Date` = 2026-10-28 in beiden `AppVersion.cs`. Build 0 Fehler, betroffene Tests (SyncLog/Help/Version, 284) grün.
Q-2 (bestehendes Parser-Verhalten) und Q-3 (Testlücke, manuell TS-102.2) bleiben offen.

## Manuelle Test-Checkliste v1.70.0

Testsystem, nach Deploy laut Abschnitt Deploy. Je Schritt Ergebnis notieren; TS-102 in `docs/TESTSZENARIEN.md` ist die ausführliche Fassung.

1. **P-2 Kollation** auf der Test-DB ausführen (SQL siehe Prüfabfragen). Erwartet `…_CI_…` für DB und alle `SyncLogs`-Textspalten. Sonst **STOPP und melden** – Schritt 8 ist dann nicht abnahmefähig.
2. **P-3** ausführen: Zeilenzahl (> 50 nötig) und Altnamen (Service nicht in der Liste der 25) notieren. **P-1** optional für die Backlog-Frage.
3. Als Admin `/SyncLog`, Seitengröße 25. Versionsanzeige = 1.70.0 (Web und Dienst).
4. **TS-102.1:** Meldung einer Zeile von Seite 3+ in „Meldung“ filtern (ENTER) → Zeile auf Seite 1; Trefferzahl gleich `SELECT COUNT(*) FROM dbo.SyncLogs WHERE Message LIKE '%…%'`; keine leeren Folgeseiten.
5. **TS-102.2:** „Aktivität“ `abgleich`, `OperationConfig%`, `!abgleich`, `werkbank` → wie TS-102.2; bei `werkbank` erscheinen **beide** Werkbank-Läufe (sofern `OseonWorkplaces`-Zeilen existieren, AKE). Tooltip der Zelle = technischer Name.
6. **TS-102.3:** Auswahlfeld – 25 deutsche Namen alphabetisch; Auswahl „Arbeitsgang-Abgleich (Sage-View)“ → URL `service=OperationConfigSync`, Auswahl bleibt markiert; Lesezeichen `/SyncLog?service=OperationConfigSync` gleich.
7. **TS-102.4:** Zeitpunkt `TT.MM.JJJJ` (heute), `MM.JJJJ`, `>=TT.MM.JJJJ`, `TT.MM.JJJJ...TT.MM.JJJJ` → nur Zeilen im Zeitraum, Trefferzahl gefiltert. `14:3` → **keine Treffer** (gewollt, Q-1).
8. **TS-102.6 Groß/Klein:** „Meldung“ `FEHLGESCHLAGEN` vs. `fehlgeschlagen`; Spaltenfilter „Stufe“ `=warning` → nur Warnungen. Gleiche Treffer nur bei CI (Schritt 1).
9. **TS-102.8 Negativfälle:** `32.13.2026` → keine Zeile; `!32.13.2026` → alle; `>=32.13.2026` → Feld markiert ungültig, Liste ungefiltert (Bestandsverhalten, Q-2); `?colf_Unbekannt=x` → unverändert; `?page=0` und `?page=-3` → Seite 1 ohne Fehlerseite; Altname aus P-3 technisch sichtbar, nur von `!…` erfasst.
10. **TS-102.7 Kombination:** Filterkarte „Stufe = Warning“ + „Aktivität“ `lager%` → nur Warnungen von Lagerplatz-/Lagerbestand-Sync und Lagerbestellungs-E-Mail-Versand. Zusätzlich `?service=Lagerplatz&colf_Message=konflikt` (AK 10).
11. **TS-102.5 CSV:** mit aktivem Spaltenfilter „CSV“ → Zeilenzahl = Trefferzahl, Spalte Aktivität deutsch, kein Kappungshinweis bei < 5000 Treffern. Ohne Spaltenfilter bei > 5000 Zeilen erscheint der Hinweis weiterhin.
12. **TS-102.9 Hilfe:** Abschnitt Aktivitäts-Protokoll – Rolle nur `admin`, 25 deutsche Namen mit technischem Namen in Klammern, Absatz „Spaltenfilter (seit v1.70.0)“. Changelog-Karte v1.70.0 vorhanden.
13. **Zugriff (AK 14):** Anmeldung ohne Admin-Rolle → `/SyncLog` verweigert.
14. **Dienst-Regression:** nach Dienst-Neustart schreiben die Läufe weiter Zeilen ins Protokoll (neueste Zeile nach einem Zyklus sichtbar, deutscher Name in der Spalte).
