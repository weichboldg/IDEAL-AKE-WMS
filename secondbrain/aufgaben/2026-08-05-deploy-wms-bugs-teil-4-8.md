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

## Teil 5 — Standardmenge 1
- [ ] `Quantity = 1` im Objekt-Initializer der `Inbound()`-GET-Action (NICHT am ViewModel-Property)

## Teil 8 — Default-Filter Bezeichnung 1
- [ ] `User.DefaultFilterFaWorklistDescription1` (`string?`, StringLength 200) + Migration 84 + FreshInstall
- [ ] Profile (GET/POST) + UsersController (GET/POST) + Views laden/speichern
- [ ] `FaWorklistController.Index`: Redirect-mit-Parameter (`colf_description1` + Sentinel `df1=1`)

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
