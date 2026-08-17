---
type: aufgabe
title: "IDEAL Teil 7 — Materialisierung nach ProductionOrders (Epic-Umsetzung)"
status: InUmsetzung
created: 2026-08-14
updated: 2026-08-14
spec: "[[2026-07-29-standort-ideal-teil-7-spec]]"
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---

# IDEAL Teil 7 — Epic-Umsetzung

Läuft im **bestehenden** Bündel-Worktree (gemeinsam mit Teile 1–5 v1.31.0, kein neuer Worktree).
Fünf Etappen A–E, ein Merge am Ende. Spec: [[2026-07-29-standort-ideal-teil-7-spec]].

## Etappen-Status

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| A | Schema-Inversion + Migration + Backfill + FreshInstall + tote AgentJobs | **erledigt** | `fe7299b` |
| B | Guard (Choke-Point) + Einwegtor/Umschalt-Seite + Audit-SyncLog + Runbook | **erledigt** | `aed9cb5..39f7813` |
| C | Materialisierungs-Sync + drei Sync-Regeln (unit-getestete Planer) | offen | — |
| D | Lookup-Härtung + `GetAllByFaAndOperationAsync` + FA-Zusatzinfos-Review + Auto-Erledigt-Sperre | offen | — |
| E | Doku, Testszenarien, Brain-Update, Version-Bump | offen | — |

## Umsetzungsnotizen

### Etappe A — Schema-Inversion (2026-08-14, `fe7299b`)

- **Modell** `ProductionOrder.cs`: `SubOrderNumber` (`[Required][StringLength(100)]`),
  `ParentSubOrderNumber` (`string?`), `SageMissingSince` (`DateTime?`). Kommentare erklären die
  Invariante `SubOrderNumber == OrderNumber` im flachen AKE-Modus.
- **DbContext**: Unique-Index von `OrderNumber` → `SubOrderNumber`; `OrderNumber` bleibt als
  **nicht**-eindeutiger Index (Zeile 417 war `HasIndex(OrderNumber).IsUnique()`).
- **EF-Migration** `20260814105526_InvertProductionOrderHierarchy`: Up() **manuell korrigiert** —
  die von EF generierte Fassung setzte `SubOrderNumber` mit `defaultValue: ""` und legte sofort
  den Unique-Index an; auf befüllten AKE-Daten hätte `dotnet ef database update` am Unique-Index
  (alle `""`) gebrochen. Jetzt: nullable anlegen → `UPDATE SET SubOrderNumber = OrderNumber` →
  `AlterColumn NOT NULL` → Index-Tausch.
- **SQL/90** (Platzhalter „SQL/87" der Spec war belegt — 75–89 existieren, nächste frei = **90**):
  idempotent mit `COL_LENGTH`- + `is_nullable`- + `sys.indexes`/`sys.key_constraints`-Guards.
  Räumt die alte Eindeutigkeit auf `OrderNumber` in **beiden** Formen ab — `UQ_`-Constraint
  (FreshInstall-DB) **und** `IX_`-Unique-Index (EF-migrierte DB). Batches getrennt (Spalten →
  Backfill → NOT NULL → Index-Tausch → History-Insert).
- **FreshInstall** an beiden Stellen: 3 Spalten in `CREATE TABLE`, `UQ_…_OrderNumber`-Constraint
  entfernt (Endzustand = kein UQ-Constraint, Unique-Index auf `SubOrderNumber`), Index-Sektion
  (OrderNumber jetzt nicht-eindeutig + neuer Unique auf SubOrderNumber), MigrationId-Insert.
- **Toter AgentJob**: `SQL/AgentJobs/01_Import_Produktionsauftraege.sql` → `_archiv/` mit
  „AUSSER BETRIEB"-Kopf; `02_Import_Artikel.sql` bewusst belassen (trägt bereits
  `DEPRECATED seit v1.17.0`-Header, nicht irreführend). Sweep-Nachweis in `_archiv/README.md`.
- **Produktiv-Insert-Prüfung:** kein `new ProductionOrder`/`ProductionOrders.Add` im Produktivcode —
  Inserts laufen ausschließlich über `SageImportService` (raw-SQL, setzt `SubOrderNumber=OrderNumber`
  via `SageProductionOrderSql.BuildUpsert(true)`, sobald die Spalte existiert). Invariante hält.
- **Build** 0 Fehler; **Tests** Web 1174 (+1 skip) + Service 221 grün.
- **Kein** Version-Bump in Etappe A (erst Etappe E). Guard/Umschalt-Seite/Sync-Regeln folgen in B–D.

### Etappe B — Guard + Einwegtor + Runbook + Audit (2026-08-17, `aed9cb5..39f7813`)

Build + `dotnet test` grün im Worktree: **Web 1185** (+1 vorbestehend übersprungen, gesamt 1186) +
**Service 221**, 0 Fehler (+11 neue Tests ggü. Etappe A). Plan:
`docs/superpowers/plans/2026-08-17-ideal-teil7-etappe-b.md` (im Worktree). Vier Commits: Guard-Kern
(`aed9cb5`), Umschalt-Seite + read-only-Maske + Audit (`0ae43d0`), Runbook + README (`9de5b1a`),
Testszenarien TS-63 + Test-Fix (`39f7813`).

**Architektur (S7-6 „einziger Choke-Point" erfüllt):**
- **Reiner Planer** `HierarchischeStrukturGuard.Evaluate(setting, current, requested, dataExists)`
  (Muster `ProductionOrderReconciler`, keine DB, unit-getestet — AK 12). Sperrbedingung
  datengetrieben: `dataExists = EXISTS(ProductionOrders WHERE OrderNumber <> SubOrderNumber)`.
- **Durchsetzung als Decorator** `GuardedServiceSettingRepository` auf
  `IServiceSettingRepository.UpsertAsync/DeleteAsync` — die **einzige** Datenzugriffs-Naht, durch die
  alle vier ServiceSettings-Schreibwege (SaveSettings/Create/Edit/Delete) laufen. Kein Caller kann
  den Guard umgehen; DI in `Program.cs` (concrete `ServiceSettingRepository` → Guarded-Wrapper).
- **Schreibpfad live** (Repo-`HierarchicalDataExistsAsync`, `AnyAsync`), **Anzeige gecacht**
  (`HierarchischeStrukturStatus`-Singleton, Refresh am App-Start + nach jedem Flip; Etappe-C-Sync
  ruft `RefreshAsync()` am Sync-Ende — Naht steht) — exakt Freigabe-Antwort 3.
- **Umschalt-Seite** `HierarchieUmstellungController` + `Views/HierarchieUmstellung/Index.cshtml`
  ([RequireAdminAccess]): Bootstrap-Modal mit dem **wortgleichen** Bestätigungstext (AK 4),
  Deaktivieren solange offen (AK 3), Sperr-Anzeige mit Runbook-Verweis wenn Daten vorliegen.
- **Generische Maske read-only** (AK 2): Master-Zeile in `Views/ServiceSettings/Index.cshtml` zeigt
  nur Badge + Link, **kein** Schreib-Bedienelement (kein `<input name=settings[...]>` → nie
  gepostet). Ein technischer POST wird vom Decorator abgelehnt; `SaveSettings` fängt die
  `HierarchischeStrukturGuardException`, meldet + protokolliert (SyncLog `HierarchieUmstellung`).
- **Audit** (ADR 0010): SyncLog-Service `HierarchieUmstellung`, Master-Flip + Auto-Erledigt-
  Abschaltung + abgelehnte Schreibversuche protokolliert (Windows-Login via `ICurrentUserService`).
- **Runbook** `docs/RUNBOOK-FA-HIERARCHIE-RUECKBAU.md` (Backup/Tabellen/Bereinigung/Datenverlust-
  Warnung, „nicht über die Anwendung umkehrbar") + README-Abschnitt „Runbooks"; die Guard-
  Fehlermeldung referenziert denselben Pfad (AK 15).

**Zuschnitt-Entscheidung Schalter-Inventur (S7-8, hier verbindlich benannt):** Die abhängigen
Schalter sind **genau zwei** — Master `ProduktionsauftragHierarchisch` + Auto-Erledigt
`Sync:FaZusatzinfoAutoErledigtEnabled`. **Kein dritter Gate-Schalter**; die übrigen
`OrderNumber`-abhängigen Codepfade werden in **Etappe D** gehärtet (Lookup-Sweep), nicht per
Schalter gesperrt. Etappe B legt **nur den Master-Key** an (Katalog + Seed + read-only-UI). Der
Auto-Erledigt-Key wird vom Guard bereits **benannt und geschützt** (`HierarchischeStrukturKeys`),
seine Katalog-Eintragung + die Fold-2-Verdrahtung in `FaZusatzinfoSyncService` bleiben **Etappe D**
(dort bekommt der Key seinen Leser — kein leserloser Katalog-Key in B). Der Controller entschärft
Auto-Erledigt beim Master-Flip bereits defensiv (No-op, solange der Key fehlt).

**Abweichung ggü. Spec-Rumpf (dokumentiert):** Die im Spec-`affected_code` genannte
`StandortEinstellungenController` (Teil-6-Maske) **existiert nicht** — die tatsächliche Etappe 6 des
Bündels lieferte „Listen-Spaltenauswahl", keine Standort-Maske. Damit entfällt der zweite UI-
Schreibweg; nur die generische ServiceSettings-Maske ist read-only zu behandeln (so umgesetzt). Das
Testszenario „Teil-6-Maske" ist durch „generische Maske bietet kein Schreib-Bedienelement" ersetzt
(war ohnehin die Nachbesserungs-2-Fassung).

**Offen für Etappe C/D/E:** Materialisierungs-Sync + `RefreshAsync()`-Aufruf am Sync-Ende (C);
Auto-Erledigt-Katalog-Key + Fold-2-Verdrahtung + datengetriebener Skip + hierarchische Tests (D);
Version-Bump/Changelog/Brain-Dauerwissen (E). Status Teil 7 bleibt **InUmsetzung**, kein qa-agent,
kein Merge, kein Push.

## Offene Punkte / Deploy-kritisch (aus der Spec, für Schranke 2 / Deploy)

- **DB-Backup vor Deploy zwingend** (Kern-Tabelle `ProductionOrders`, daten-konvertierend).
- **Harte Deploy-Vorbedingung (AK 13):** serverseitig `sysjobs` prüfen, ob ein aktiver Agent-Job
  `ProductionOrders` schreibt → deaktivieren; das Archivieren der Repo-Datei entfernt keinen Job.
- **Irreversibilität:** Etappe A invertiert das Schema unabhängig vom Master-Schalter. Abbruch nach
  A lässt die DB invertiert (inhärent, im Runbook zu benennen — folgt in Etappe B).
- **Kopplung:** selber Branch wie Teile 1–5 (v1.31.0, testbereit) — Merge bringt beides zusammen.
- Schalter-Inventur (S7-8): welche „weiteren abhängigen Schalter" neben Master + Auto-Erledigt —
  vor Etappe B/D explizit benennen.
