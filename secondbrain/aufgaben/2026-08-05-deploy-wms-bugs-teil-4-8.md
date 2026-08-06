---
type: aufgabe
title: Umsetzung WMS Bugs & Improvements Teil 4,5,7,8 (ein Worktree; Teil 6 ausgeschlossen)
status: Testbereit
created: 2026-08-06
spec: "[[2026-08-05-wms-bugs-improvements-teil-4-spec]]"
worktree: ".claude/worktrees/2026-08-05-wms-bugs-improvements-4-8"
branch: "feature/2026-08-05-wms-bugs-improvements-4-8"
---

# Umsetzung WMS Bugs & Improvements Teil 4,5,7,8

Freigegebene Teil-Specs im **gemeinsamen** Worktree (Auftrag: „im gleichen worktree, erleichtert
den Test"). **Teil 6 ist ausgeschlossen** — Freigabe-Antworten unbeantwortet, Kritische Pruefung
„ZURUECKGESTELLT". Reihenfolge nach Groesse: 4 → 5 → 8 → 7.

- [[2026-08-05-wms-bugs-improvements-teil-4-spec]] — 3 No-Op-Spaltenfilter entfernen (Web)
- [[2026-08-05-wms-bugs-improvements-teil-5-spec]] — Einbuchung Standardmenge 1 (Web)
- [[2026-08-05-wms-bugs-improvements-teil-8-spec]] — FA-Worklist Bezeichnung-1-Default-Filter (Web + Migration)
- [[2026-08-05-wms-bugs-improvements-teil-7-spec]] — Kommentar + DUMMY-Artikel (Web + Service + Migration + Seed)

## Migrationsnummern (main bei SQL/83)
- Teil 8 → `SQL/84_AddUserDefaultFilterFaWorklistDescription1.sql` (EF-Migration)
- Teil 7 → `SQL/85_AddWarehouseRequisitionComment.sql` (EF-Migration, Schema)
- Teil 7 → `SQL/86_SeedDummyArticle.sql` (reiner Daten-Seed, KEIN EF/History-Eintrag)

## Teil 4 — No-Op-Spaltenfilter entfernen
- [ ] `data-filterable`/`data-col-key` an `<th>` fuer `datetime`, `quantity`, `movement-type` in
      `StockMovements/Index.cshtml` entfernen
- [ ] Folge-Aufgabe „Klassen-Audit andere Server-Spaltenfilter-Tabellen" notieren
=> bitte Filter wieder einfüguen, aber korrekt. die funktionalität muss gegeben sein.
## Teil 5 — Standardmenge 1
- [x] `Quantity = 1` im Objekt-Initializer der `Inbound()`-GET-Action (NICHT am ViewModel-Property)

## Teil 8 — Default-Filter Bezeichnung 1
- [x] `User.DefaultFilterFaWorklistDescription1` (`string?`, StringLength 200) + Migration 84 + FreshInstall
- [x] Profile (GET/POST) + UsersController (GET/POST) + Views laden/speichern
- [x] `FaWorklistController.Index`: Redirect-mit-Parameter (`colf_description1` + Sentinel `df1=1`)
=> hier gab es ein missverständnis, der DefaultFilterFaWorklistDescription1 kann erhalten bleiben.
=> bitte für die BOM noch einen DefaultFilterBomDescription1 erstellen, dieser muss in der bom die artikelbeschreibung 1 filtern. 
## Teil 7 — Kommentar + DUMMY
Phase A (Kommentar):
- [ ] `WarehouseRequisition.Comment` (`string?`, 1000) + Migration 85 + FreshInstall
- [ ] `SaveCommentAsync` + Comment-Endpunkt (CheckOwnershipAndDraft) + Edit-UI (textarea)
- [ ] `WarehouseRequisitionListItemViewModel.Comment` (beide Call-Sites) + Picking-Liste-Spalte (data-col-key) + Details
- [ ] Submit-Mail HTML+Text (Service) — Storno-Mail unveraendert
Phase B (DUMMY):
- [ ] `SQL/86_SeedDummyArticle.sql` + FreshInstall-Seed-Verankerung (idempotent)
- [ ] `Article.DummyArticleNumber = "DUMMY"` + Default-Seed-Bezeichnung als Konstante
- [ ] „nicht gefunden"-UX + `POST .../items/dummy` (Pflicht-Bezeichnung, != Default)
- [ ] Duplikat-Guard fuer DUMMY ueberspringen, Glas-Guard umgehen
=> dummy artikel wurde nicht geseedet?
## Abschluss
- [x] docs/TESTSZENARIEN.md (TS-2.26-2.31, TS-43.5, TS-46.9-46.13) + secondbrain/tests/testszenarien-index.md
- [x] Version-Bump v1.30.0 (beide AppVersion.cs) + Changelog.cshtml + Brain-Changelog
      [[2026-08-06-v1-30-0-wms-bugs-teil-4-5-7-8]]
- [x] QA gruen (Web 1075/1 skip, Service 197), Deploy-Abschnitte finalisiert, alle 4 Specs Testbereit
- [x] feature-map, Bug-Record [[2026-08-05-bewegungshistorie-spaltenfilter-noop-bug]] behoben-testbereit,
      Folge-Aufgabe [[2026-08-06-audit-server-spaltenfilter-noop]]

## Ergebnis (Testbereit, 2026-08-06)
Teil 4,5,7,8 umgesetzt; **Teil 6 ausgeschlossen** (Freigabe-Antworten unbeantwortet). `dotnet build`
0 Fehler, `dotnet test` Web 1075 passed/1 skipped + Service 197 passed (+13 Tests Teil 7).
Migrationen: 84 (User-Default-Filter), 85 (Bestellung-Kommentar), Seed 86 (DUMMY, kein History-Eintrag).
Version **v1.30.0** (bewusst nach v1.29.0 Teil 1-3, Merge-Reihenfolge beachten).
Offen: **Schranke 2** (Mensch: Manual-UAT je Teil, dann Merge; SQL 84/85/86 vor Publish, Service-Publish
wegen Teil-7-Mail). Worktree/Branch bleiben stehen.

## Korrekturen aus Nutzer-Feedback (2026-08-06)

Umgesetzt auf dem **kombinierten Test-Branch** `feature/2026-08-05-wms-bugs-improvements-teil-1-2-3`
(nicht mehr im 4-8-Worktree — der 4-8-Branch ist dort bereits eingemergt, dort wird getestet):

- **Teil 4 → „Filter wieder einfügen, aber funktionsfähig":** Statt die No-Op-Filter zu entfernen,
  echte Server-Handler in `ApplyMovementColumnFilter` ergänzt. **Bewegungsart** (matcht deutschen
  Anzeigenamen → Enum-`IN`) und **Datum/Zeit** (Tag `dd.MM.yyyy` / Monat `MM.yyyy` / Jahr `yyyy` als
  Zeitraum, OR über Tokens via Expression-Combiner) funktionieren jetzt. **Menge** bewusst ohne
  Text-Filter (kein sinnvoller Handler; Nutzer-Wahl „Bewegungsart und Datum"). `th`-Marker wieder
  gesetzt. 7 Repo-Tests. Spec-/Bug-Record/Changelog/TS-2.26 angepasst.
- **Teil 8 → „für die BOM einen DefaultFilterBomDescription1":** `DefaultFilterFaWorklistDescription1`
  bleibt. Neu `User.DefaultFilterBomDescription1` (**Migration 87** + `SQL/87` + FreshInstall) +
  Profile/Admin + `PickingController`→`BomViewModel`; `Bom.cshtml` belegt per
  `window.setColumnFilter('description1', …)` beim Öffnen vor (Client-Mode, wie Artikelgruppe). TS-5.10.
- **Teil 7 → „DUMMY-Artikel wurde nicht geseedet?":** Kein Code-Bug. Die App wendet beim Start nur
  **EF-Migrationen** an (`db.Database.Migrate()`); `SQL/86_SeedDummyArticle.sql` ist ein **reiner
  Daten-Seed (keine EF-Migration)** und läuft daher **nicht** automatisch. Auf bestehender DB
  `SQL/86` **einmal manuell** ausführen (bei Frisch-Install via `00_FreshInstall.sql` enthalten).
  Seed + FreshInstall-Verankerung sind korrekt (alle NOT-NULL-Spalten bedient).

Build nach Korrekturen: Web **1092 passed / 1 skipped**, Service **197 passed**. Migration **87**
zusätzlich. Weiterhin **Schranke 2** offen.

## UAT-Fix #2 (2026-08-06, QA-Re-Verify) — Migration 88 gefilterter Unique-Index

Zweiter realer UAT-Fund (nach der Pflicht-Bezeichnungs-Korrektur/TS-46.14 oben): am echten SQL
Server warf die **zweite** DUMMY-Position je Bestellung `SqlException 2601` am Unique-Index
`IX_WarehouseRequisitionItems_(WarehouseRequisitionId, ArticleNumber)` — der App-Layer-Duplikat-
Guard-Skip reichte nicht, weil der DB-Index alle DUMMY-Positionen (gleiche `ArticleNumber='DUMMY'`)
weiterhin blockte. InMemory erzwingt Unique-Indizes nicht, daher unsichtbar in Unit-Tests
(bekannter Fallstrick).

Fix: Unique-Index gefiltert (`WHERE [ArticleNumber] <> 'DUMMY'`), Migration `20260806120650_
AllowMultipleDummyRequisitionItems` (Nr. **88**), `SQL/88_AllowMultipleDummyRequisitionItems.sql`
(idempotent) + `SQL/00_FreshInstall.sql` (Schema + `MigrationId`) nachgezogen. QA-Re-Verify auf dem
kombinierten Branch (HEAD `1535f41`): Build 0 Fehler, Web **1093 passed/1 skipped**, Service
**197 passed**, `dotnet ef migrations has-pending-model-changes` → „No changes". Details siehe
[[2026-08-05-wms-bugs-improvements-teil-7-spec]] „## QA-Re-Verify (2026-08-06, kombinierter Branch,
UAT-Fix #2 — Migration 88)". Deploy-Reihenfolge jetzt: SQL 85 → 86 → 88 vor Publish. Weiterhin
**Schranke 2** offen — Multi-DUMMY nur am echten SQL Server final abnehmbar (TS-46.12).

## Ad-hoc Layout-Tweak (2026-08-06, Nutzer-Wunsch, kein Spec-Bezug)
Erste Spalte der **FA-Abarbeitungsliste** (`FaWorklist/Index`) und **FA-Vervollständigen**
(`FaCompletion/Index`) an die **FA-Liste** (`ProductionOrders/Index`) angeglichen: BOM als **Button
links** (`btn-sm btn-outline-primary`, Stückliste-Icon), FA-Nr **fett** statt Link, enaio via
`_EnaioDmsBadges` + Vault-Icon — statt der `_FaDocumentLinks`-Icon-Leiste. `_FaDocumentLinks` selbst
unberührt (weiter im FaCompletion/Edit-Kopf). Reine View-Änderung, Build grün, Manual-UAT (Optik).
