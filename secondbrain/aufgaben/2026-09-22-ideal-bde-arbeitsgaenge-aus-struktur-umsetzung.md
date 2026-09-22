---
typ: notiz
spec: "[[2026-09-21-ideal-bde-arbeitsgaenge-aus-struktur-spec]]"
status: InUmsetzung
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# Umsetzung: IDEAL BDE-Arbeitsgänge (WorkOperation) aus Struktur + Werkbank-Anlage aus Sage (EPIC)

Spec [[2026-09-21-ideal-bde-arbeitsgaenge-aus-struktur-spec]] · **bestehender Bündel-Worktree** ·
**EPIC, zwei Etappen** (A = Baustein a; B = b+c). Supersedes v1.41.0-FaWorkStep-Spec.
`depends_on` ADR-0014-Rückbau (Testbereit im Worktree ✓).

## Pre-Flight-Verdikt (2026-09-22)

- status Freigegeben, open_questions [], freigabe_entscheidung/_von/_am gesetzt.
- Kritische Prüfung 2. Durchgang: **B6 (fehlender Unique-Index) ausgeräumt** — `affected_code` führt den
  eindeutigen gefilterten Index `IX_ProductionWorkplaces_SageArbeitsplatznummer WHERE ... IS NOT NULL`
  jetzt explizit auf. Kein offener Blocker.
- `depends_on` ADR-0014-Rückbau umgesetzt (Testbereit, Commit `0aceee8`). ✓
- Worktree-Stand: AppVersion **1.43.0**, SQL max **92** → SQL/**93** frei (Spec bestätigt).

## Etappen-Scope DIESER Lauf: NUR Etappe A (Baustein a), danach STOPP + melden

Etappe A = `ProductionWorkplace`-Anlage aus Sage `KHKPpsArbeitsplaetze`:
1. `ProductionWorkplace.cs`: `SageArbeitsplatznummer` (string?, NVARCHAR(31), **Verknüpfungsschlüssel**,
   String/getrimmt/Ordinal, **eindeutiger gefilterter Index** `WHERE IS NOT NULL`) + `ArbeitsschrittCode`
   (string?, MaxLength 20, getrimmt).
2. Migration `AddProductionWorkplaceSageFields` + `SQL/93_*.sql` (2 COL_LENGTH-Guards + Unique-Filter-Index
   mit eigenem IF-NOT-EXISTS) + FreshInstall (beide Stellen).
3. **`ProductionWorkplaceSyncService`** (NEU, raw Sage-Read `KHKPpsArbeitsplaetze`): Filter (Mandant,
   `Aktiv=-1`, `USER_ArbeitsSchritt` gefüllt+getrimmt, nicht auf Ausschlussliste) → Upsert je Zeile
   (kein Treffer=anlegen `BdeAktiv=false` / Abweichung=überschreiben+melden / gleich=nichts /
   Unique-Verstoß=je Zeile abfangen+melden+weiter); S1-Sammelmail; SyncLog `ProductionWorkplaceSync`.
4. `ServiceSettingDefinitions`: `Sync:ProductionWorkplaceSyncEnabled` (Bool false),
   `...Mandant` (Int 1), `...Ausschlussliste` (String `STO,XXX,x01,BS,BS2,FRE,AKE`).
5. `StandortSettingsCatalog`: 2 Felder (Mandant + Ausschlussliste, neue Gruppe).
6. `SyncLogServices`: Konstante `ProductionWorkplaceSync`.
7. `SyncWorker`: neuer **Doppel-Gate**-Block (Master + Toggle), vor Baustein b.
8. `Program.cs`: DI.
9. Views `/ProductionWorkplaces` (Index/Edit/Create): read-only Anzeige + neue Spalten + Spaltenfilter
   (frontend-design PFLICHT).

**NICHT dieser Lauf:** Etappe B (b+c) — WorkOperation-Umbau, Umbenennungen, BdeDefault-Fix, TS-77.
**Kein Testbereit** (Epic-Ende erst nach Etappe B). Etappe A = eigener buildbarer Commit, dann STOPP.

## Arbeitsstand

- 2026-09-22: Spec Freigegeben → InUmsetzung. Baustein-(a)-Entwurf gelesen. Bau startet.
- 2026-09-22: **Etappe A (Baustein a) vollständig gebaut — STOPP wie beauftragt.** Build grün
  (0 Fehler), volle Testsuite grün (Web 1391 + 1 skip, Service **269** = 263 + 6 neue).
  Zwei baubare Commits im Worktree:
  - `8be03982` Backend: Model-Felder + Migration `20260922083259`/`SQL/93` + FreshInstall (beide Stellen)
    + `ProductionWorkplaceSyncService` + `ISageArbeitsplatzReader`/`SageArbeitsplatzReader`
    + `IUnknownArbeitsplatzState` (neu, weil `IUnknownWorkplaceState` im ADR-0014-Rückbau gelöscht wurde)
    + 3 ServiceSettings + `StandortSettingsCatalog` (Gruppe „Werkbänke") + `SyncLogServices`-Konstante
    + `Program.cs`-DI + `SyncWorker`-Doppel-Gate (`ProduktionsauftragHierarchisch` + `Sync:ProductionWorkplaceSyncEnabled`)
    + 6 Unit-Tests (`ProductionWorkplaceSyncServiceTests`).
  - `be0695fb` Views: `ProductionWorkplaces/Index` neue Spalten Sage-Nr. + Arbeitsschritt inkl.
    Server-Spaltenfilter (ADR 0005, ColumnMap-Getter `sage-nr`/`arbeitsschritt`);
    `Edit` zeigt bei Sage-geführten Werkbänken Bezeichnung/Sage-Nr./Arbeitsschritt **read-only**
    (manuelle AKE-Werkbänke bleiben editierbar — kein Sync überschreibt sie); `Create`-Hinweis.
    ViewModel: `SageArbeitsplatznummer`/`ArbeitsschrittCode` (read-only) + `IstSageGefuehrt`.
- Verifiziert: Migration `20260922083259_AddProductionWorkplaceSageFields` = höchste; SQL/93 = nächste
  freie Nummer; ADR-0014-Rückbau im Worktree (keine Werkbank-aus-Arbeitsbereich-Ableitung mehr in
  `FaMaterializationSyncService`, `IUnknownWorkplaceState` gelöscht) → `depends_on` erfüllt.
- **Bewusst NICHT gemacht** (Epic-Regel, erst nach Etappe B): Versions-Bump 1.43.0→1.44.0
  (beide `AppVersion.cs`) + Anwender-Changelog, `qa-agent`, `status: Testbereit`, Brain-Changelog,
  feature-map, Testszenarien (TS-77) — das ist Etappe-B-Abschluss. Code-Kommentare referenzieren
  bereits „v1.44.0" als Zielversion (Vorwärts-Verweis, harmlos).
- 2026-09-22: **Etappe B (Bausteine b+c) vollständig gebaut** — Commit `920aabb7` (v1.44.0). Build grün
  (0 Fehler), volle Testsuite grün (Web 1392 + 1 skip, Service 268).
  - Baustein b: `FaWorkStepStructureDetectionService` → `WorkOperationStructureDetectionService`
    (Ziel `WorkOperation`, Katalog `ProductionWorkplace.ArbeitsschrittCode`, `OperationNumber`=Kürzel,
    `ProductionWorkplaceId`/`Name` kopiert). Neu: Kürzel-Mehrdeutigkeit gemeldet, Ausschlussliste-Kürzel
    still (weder AG noch Meldung), `FaWorkStepSources.Struktur` entfernt. Umbenennungen: Interface,
    `IUnknownWorkStepTokenState`→`IUnknownArbeitsschrittTokenState`, Key
    `Sync:WorkOperationStructureDetectionEnabled`, `SyncLogServices.WorkOperationStructureDetection`,
    SyncWorker/Program.cs. Tests neu (13, inkl. Mehrdeutigkeit AK 11).
  - Baustein c: `BdeDefaultWorkOperationService`-Existenz-Check über `OperationNumber "01"` (AK 16, +Test);
    `BdeScanResolver` unverändert (AK 15 — Diff zeigt keine Änderung); Terminal-Routing schon über
    `ProductionWorkplaceId`.
  - Epic-Abschluss: Version 1.43.0→1.44.0 (beide AppVersion), Anwender-Changelog, README/Hilfe-Key,
    docs/TESTSZENARIEN TS-77 + TS-76 superseded.
  - Brain (Hauptcheckout): Etappen-Tabelle B erledigt, v1.41-Spec + feature-map superseded, Brain-Changelog
    `2026-09-22-v1-44-0-*`, services.md, fallstricke §16 (drei Vokabulare) + §13 superseded-Hinweis,
    testszenarien-index TS-77.
- **Offen:** qa-agent (grüner Beweis + Deploy-Abschnitt) → `status: Testbereit`. Danach wartet das
  ganze Bündel auf **Schranke 2** (Mensch: Manual-UAT + EIN Merge). Manual-UAT-Schwerpunkte: AK 10
  (Ausschlussliste), AK 20 (Doppelanlage/UNIQUE), Sage-Reads, Terminal-Anzeige — DryRun vor dem scharf
  schalten (Baustein a legt ~60 Werkbänke an).
