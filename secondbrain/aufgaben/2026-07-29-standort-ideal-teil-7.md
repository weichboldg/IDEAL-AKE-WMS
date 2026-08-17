---
type: aufgabe
title: "IDEAL Teil 7 — Materialisierung nach ProductionOrders (Epic-Umsetzung)"
status: Testbereit
created: 2026-08-14
updated: 2026-08-17
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
| C | Materialisierungs-Sync + drei Sync-Regeln (unit-getestete Planer) | **erledigt** | `dc297d9..bc7e3d6` |
| D | Lookup-Härtung + `GetAllByFaAndOperationAsync` + FA-Zusatzinfos-Review + Auto-Erledigt-Sperre | **erledigt** | `fb07512..660a01b` |
| E | Doku, Testszenarien, Brain-Update, Version-Bump | **erledigt** | `5cf802e` (+ Brain auf main) |

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

### Etappe C — Materialisierungs-Sync + drei Sync-Regeln (2026-08-17, `dc297d9..bc7e3d6`)

Build + `dotnet test` grün im Worktree: **Web 1185** (+1 skip) + **Service 228**, 0 Fehler (+7 neue
Planer-Tests). Plan: `docs/superpowers/plans/2026-08-17-ideal-teil7-etappe-c.md`. Vier Commits:
Planer + SyncLog-Service (`dc297d9`), Orchestrator (`0aaa758`), SyncWorker/DI/Refresh (`375dbc3`),
Testszenarien TS-64 (`bc7e3d6`).

**Architektur (AK 5/6/7/12):**
- **Reiner Planer** `FaMaterializationPlanner.Plan(source, existing)` (Muster
  `ProductionOrderReconciler`, keine DB, 7 Unit-Tests). Drei Regeln: (1) neuer Sub-FA → `ToCreate`;
  (2) verschwundener FA → `ToMarkMissing` (nie löschen) + `ToClearMissing` bei Wiederauftauchen
  (selbstheilend); (3) Umhängung (`ParentSubOrderNumber` weicht ab) → `ReparentConflict` (melden,
  nicht übernehmen). **Empty-Source-Guard** (leere Struktur → Skip, kein Massen-Markieren, analog
  Reconciler). `MissingToNotify` = neu-vermisste **ohne** `IsKnownDone` (Melderegel konservativ, S7-2b).
- **Orchestrator** `FaMaterializationSyncService` (Service): liest lokale `FaHierarchyNodes`
  (`SubFA != 0`), mappt int→string, wendet Plan per EF an (App-Felder IsDone/PickingStatus/BdeStatus/
  Workplace/Storno/ExtraInfo **nie** überschrieben; nur Quell-Felder + Parent ohne Konflikt), setzt/
  klärt `SageMissingSince`, schickt **eine** Sammelmail pro Lauf (`IMailService`, Empfänger
  `ErrorNotification:Recipients`, gated `ErrorNotification:Enabled`), protokolliert (`ISyncLogger`,
  Service `FaMaterialization`, Counts `angelegt/vermisst_neu/wieder_da/umhaengung_konflikt`). Audit
  `ModifiedBy=IDEALAKEWMSService`.
- **SyncWorker**: gated auf `ProduktionsauftragHierarchisch=true`, läuft **nach** dem FA-Hierarchie-
  Sync (Quelle = frisch importierte Nodes). Bei Master=false läuft der Sync nicht (AKE unverändert).
- **Anzeige-Refresh cross-process-sicher**: der Sync läuft im Service-Prozess und kann den Web-Cache
  (`HierarchischeStrukturStatus`) nicht direkt aktualisieren. Statt Timer refresht die Umschalt-Seite
  (`HierarchieUmstellungController.Index`) den Cache **bei jedem GET** (admin-only, niederfrequent) →
  Anzeige stets aktuell, egal in welchem Prozess der Sync lief. Erfüllt Freigabe-Antwort 3 sauberer
  als „Refresh am Sync-Ende".

**Datenabhängige Annahmen (Schranke 2 / erster echter Datenlauf, NICHT im Code auflösbar):**
- **Mapping**: `OrderNumber=HauptFA`, `SubOrderNumber=SubFA`, `ParentSubOrderNumber=VaterFA`. Wurzel
  (`VaterFA=NULL`) wird Hauptauftrag (`OrderNumber==SubOrderNumber`) **nur wenn** `HauptFA==root.SubFA`
  (Sage-Standardannahme). Parent-Kette hängt an `child.VaterFA==parent.SubFA` (Tree-Builder-Konvention).
- **Melderegel schärfen**: behält die IDEAL-View fertige Aufträge oder blendet sie aus? Bis geklärt
  bewusst konservativ (Sammelmeldung, nur nicht-erledigte).
- **Feld-Mapping** `Quantity=Sollmenge`, `ArticleNumber=Artnr`, `Description1/2=Bezeichnung1/2` —
  Kunde/Termine (aus `FaHierarchyOrderInfo` je HauptFA) sind **noch nicht** gemappt (bewusst schlank;
  bei Bedarf in D/E oder als Folgeaufgabe nachziehen).

**Offen C→D/E:** D = Lookup-Sweep (`OrderNumber`) + `GetAllByFaAndOperationAsync` + FA-Zusatzinfos-
Fold-2-Sperre (Auto-Erledigt-Key + datengetriebener Skip) + hierarchische Reconcile/Fold-2-Tests;
E = Doku/Version-Bump/Brain. Status Teil 7 bleibt **InUmsetzung**, kein qa-agent, kein Merge, kein Push.

### Etappe D — Lookup-Härtung + Auto-Erledigt-Sperre (2026-08-17, `fb07512..660a01b`)

Build + `dotnet test` grün im Worktree: **Web 1186** (+1 skip) + **Service 231**, 0 Fehler. Plan:
`docs/superpowers/plans/2026-08-17-ideal-teil7-etappe-d.md`. Vier Commits: mengenwertige Naht (`fb07512`),
Auto-Erledigt-Sperre (`ca72434`), Reconcile-Test (`6a00ba7`), TS-65 (`660a01b`).

- **AK 10 — mengenwertige Naht:** `IWorkOperationRepository.GetAllByFaAndOperationAsync` (liefert alle
  Sub-FA-Arbeitsgänge); der bestehende `GetByFaAndOperationAsync` bleibt **verhaltensgleich** (erste
  Zeile) und **protokolliert** Mehrfachtreffer (ILogger optional im Ctor, `_logger?.LogWarning`).
  Aufrufer-Umstellung bleibt Teil 8.
- **AK 11 — dreistufige Auto-Erledigt-Sperre in `FaZusatzinfoSyncService`:**
  - *Stage 1 (datengetrieben, greift immer):* `fold2Allowed = autoErledigtEnabled && matches.Count == 1`.
    Bei Mehrfachtreffer (hierarchisch: HauptFA + Sub-FAs teilen die OrderNumber) wird Fold 2
    übersprungen + SyncLog-Warnung (WA + Trefferzahl).
  - *Stage 2 (Schalter):* neuer Key `Sync:FaZusatzinfoAutoErledigtEnabled` (Default **true** = AKE
    unverändert). Vom **Etappe-B-Guard** gegen Einschalten gesperrt, solange hierarchische Daten
    existieren; der Master-Flip schaltet ihn ab (Controller aus Etappe B). SyncWorker liest + reicht
    ihn als `SyncAsync(dryRun, cap, autoErledigt, ct)`-Param durch (Trailing-Default → bestehende
    2-arg-Aufrufer/Tests unverändert).
  - *Stage 3 (Test):* zwei Sub-FAs derselben OrderNumber, „abgeholt" am HauptFA setzt Geschwister
    nicht (`erledigt-gesetzt=0`) + Schalter-aus-Test. **Bestehender** Test
    `AutoDone_MultiFaPerWa_OnlyOpenOneIsSet` (kodierte das alte, unsichere „open-one-set"-Verhalten)
    auf den neuen kompletten Skip umgestellt — AKE bleibt unberührt (dort `matches.Count == 1`).
- **AK 12 — hierarchischer Reconcile-Test:** `ProductionOrderReconciler.Plan` mit doppelter
  OrderNumber → Gruppen-Semantik (HauptFA in Sage → nichts stornieren; weg → ganze Gruppe Kandidat).

**AK 8 — OrderNumber-Sweep (adversariales Review, Untergrenze 14 = Sweep, keine feste Liste).**
Beide Quellprojekte durchsucht (ohne Migrations/Tests/bin/obj). **7 kritische** Eindeutigkeits-
Lookups (Teil-8-Umstellungskandidaten), alles andere bleibt bewusst auf `OrderNumber`:

| Datei:Zeile | Semantik | Urteil |
|---|---|---|
| `ProductionOrderRepository.cs:101-103` (`GetByOrderNumberAsync`) | Single-Row | **kritisch** → SubOrderNumber/mengenwertig (Teil 8) |
| `IProductionOrderRepository.cs:36` (Vertrag `Task<ProductionOrder?>`) | Single-Row | **kritisch** → Signatur mengenwertig (Teil 8) |
| `WorkOperationRepository.cs:78-84` (`GetByFaAndOperationAsync`) | Single-Join | **kritisch** → **Naht in Teil 7 geliefert (AK 10)**, Umstellung Teil 8 |
| `SageProductionOrderSql.cs:21` (Upsert-EXISTS) | Upsert-Key | **kritisch** → SubOrderNumber (Teil 8) |
| `SageProductionOrderSql.cs:34` (UPDATE WHERE OrderNumber) | mehrzeiliges UPDATE | **kritisch** → SubOrderNumber (Teil 8) |
| `SageImportService.cs:320` (Reconcile-Storno UPDATE) | mehrzeiliges UPDATE | **kritisch** (Teil 8) |
| `SageImportService.cs:284` (Reconcile-Reaktivierung UPDATE) | mehrzeiliges UPDATE | **kritisch** (Teil 8) |

Unkritisch (Gruppen-Lookup, bleibt OrderNumber, bewusst): `BdeBookingService.cs:89-97`
(GroupBy→List), `FaZusatzinfoSyncService.cs:92/96` (GroupBy, jetzt mit Fold-2-Sperre),
`ProductionOrderReconciler.cs:51/57` (Set-Membership; kritischer Effekt liegt im UPDATE #6/#7).
Unkritisch (Anzeige/Enrichment): `EnaioDmsDocumentRepository.cs:22-25/30-42` (+ 5 Anzeige-Aufrufer),
30+ Filter/Suche/Sortierung/Projektion. Schema-Config `ApplicationDbContext.cs:421/422` (Inversion
korrekt). Korrekt-by-design: `HierarchicalDataExistsAsync`, `FaMaterialization*`, alle Oseon-`*OrderNumber`
(separate Entität). **Wichtig für Teil 8/Deploy:** die vier SQL-UPDATE/Upsert-Stellen
(`SageProductionOrderSql`, `SageImportService` Storno/Reaktivierung) sind am **AKE**-Standort noch
korrekt (OrderNumber==SubOrderNumber), am IDEAL aber deaktiviert (`Sync:ProductionOrdersEnabled=false`,
Reconcile aus). Erst wenn IDEAL einen dieser Pfade aktivieren will, ist die Umstellung Pflicht.

**Offen D→E:** E = Doku (README/Hilfe), Version-Bump (ein Bump fürs Bündel), Anwender-Changelog,
Brain-Dauerwissen (ggf. ADR/fallstricke/codebase/glossar). Status Teil 7 bleibt **InUmsetzung**,
kein qa-agent, kein Merge, kein Push.

### Etappe E — Doku + Version-Bump + Brain (2026-08-17, `5cf802e` + Brain auf main)

- **Version-Bump `v1.32.0`** in beiden `AppVersion.cs` (Web+Service, Date 2026-08-17) + Anwender-
  Changelog `Views/Help/Changelog.cshtml` (hierarchische Produktionsaufträge: Umstellung/Einmaltür,
  Materialisierung, Auto-Erledigt-Schutz — anwenderverständlich). Build grün.
- **Brain-Dauerwissen (Hauptcheckout):** neuer **ADR [[0012-fa-hierarchie-einweg-migrationstor]]**;
  Brain-Changelog [[2026-08-17-v1-32-0-ideal-teil-7]]; `feature-map.md` Teil-7-Abschnitt (Etappen A–E);
  `fallstricke.md` §9 (OrderNumber nicht mehr unique + Warum; `SageMissingSince` Zeitstempel;
  Cache cross-process); `codebase/services.md` + `codebase/controller.md` (neue Services/Keys/SyncLog +
  `HierarchieUmstellungController`). Testszenarien wurden pro Etappe geliefert (TS-63/64/65).
- README-AppSettings: **kein** Eintrag nötig — die neuen Keys sind Service-Keys (`/Settings`), im
  Brain `services.md` dokumentiert; der Runbook-Link steht seit Etappe B im README.

**Teil 7 ist damit vollständig umgesetzt (A–E).** Nächster Schritt: **qa-agent** (build+test grün,
Testszenarien, Deploy-Abschnitt) — darf dann `status: Testbereit` setzen. Bis dahin `InUmsetzung`.

## QA-Abnahme (2026-08-17, qa-agent, Worktree-HEAD `5cf802e`)

**Status: Testbereit.** Build + Tests grün, alle 15 AK gegen den echten Code verifiziert (nicht nur
gegen die Umsetzungsnotizen), Deploy-Abschnitt finalisiert. Kein Merge, kein Push — Schranke 2 steht
noch aus.

**Build:** `dotnet build IdealAkeWms.slnx` im Worktree → 0 Fehler, 8 Warnungen (NU1902 MailKit/MimeKit,
vorbestehend, nicht Teil-7-bezogen).

**Tests (frisch gelaufen, nicht aus alten Notizen übernommen):**
- `dotnet test IdealAkeWms.Tests --no-build`: **1186 erfolgreich, 1 übersprungen (vorbestehend,
  `ProductionOrderEagerCreateAgentJobTests`), 0 Fehler**, gesamt 1187.
- `dotnet test IDEALAKEWMSService.Tests --no-build`: **231 erfolgreich, 0 übersprungen, 0 Fehler**.
- Deckt sich exakt mit dem in der QA-Bestellung erwarteten Stand (Web 1186 inkl. Etappe-D-Zuwachs,
  Service 231).

**Code-Stichprobe gegen die Akzeptanzkriterien (Datei:Zeile, nicht nur Doku-Behauptung):**
- AK 1/9 (AKE unverändert): Backfill-Invariante `SubOrderNumber = OrderNumber` in
  `SQL/90_InvertProductionOrderHierarchy.sql` Schritt 2; kein produktiver `ProductionOrders.Add`
  außerhalb `SageImportService`/`SageProductionOrderSql` (bereits in Etappe A verifiziert).
- AK 2 (kein Schreib-Bedienelement + Ablehnung): `GuardedServiceSettingRepository.cs` als einzige
  Schreib-Naht auf `IServiceSettingRepository.UpsertAsync/DeleteAsync`; generische Maske zeigt Key
  `ProduktionsauftragHierarchisch` nur als Badge+Link (kein `<input name=settings[...]>`).
- AK 8 (Sweep, keine feste Liste): 7 kritische Fundstellen tabellarisch mit Einzelurteil in Etappe D
  (Zeilen 181–204 dieser Notiz), Rest bewusst unkritisch begründet.
- AK 10 (mengenwertige Naht): `WorkOperationRepository.cs:96` protokolliert Mehrfachtreffer im
  bestehenden `GetByFaAndOperationAsync` per `ILogger.LogWarning`, `GetAllByFaAndOperationAsync`
  (Zeile 101) liefert alle Treffer — Aufrufer-Verhalten unverändert (Teil 8 übernimmt Umstellung).
- AK 11 (dreistufige Auto-Erledigt-Sperre): `FaZusatzinfoSyncService.cs:121`
  `fold2Allowed = autoErledigtEnabled && matches.Count == 1` + SyncLog-Warnung bei Mehrfachtreffer
  (Zeile 122–125), Fold-2-Check nur bei `fold2Allowed` (Zeile 176) — Stage 1 datengetrieben bestätigt.
  Schalter `Sync:FaZusatzinfoAutoErledigtEnabled` (Default `true`) unter Guard-Schutz.
- AK 14 (Index-Tausch idempotent): `SQL/90` räumt **beide** Altformen ab (`UQ_ProductionOrders_
  OrderNumber`-Constraint per `sys.key_constraints`-Guard **und** `IX_ProductionOrders_OrderNumber`
  als alter Unique-Index per `sys.indexes`-Guard), legt neuen nicht-eindeutigen Index auf
  `OrderNumber` sowie den neuen Unique-Index auf `SubOrderNumber` jeweils mit
  `NOT EXISTS`-Guard an — bei zweitem Lauf werden alle vier DDL-Schritte übersprungen.
- AK 15 (Runbook verlinkt + referenziert): `README.md:368` verlinkt
  `docs/RUNBOOK-FA-HIERARCHIE-RUECKBAU.md`; `HierarchischeStrukturGuard.cs:27` referenziert denselben
  Pfad in der Ablehnungs-Fehlermeldung.
- `SQL/00_FreshInstall.sql`: neue Spalten (Zeilen 261–263) + Unique-Index auf `SubOrderNumber`
  (Zeile 1110–1111) + `MigrationId`-Insert (Zeile 2297–2298) vorhanden; alter
  `UQ_ProductionOrders_OrderNumber`-Constraint **nicht** mehr vorhanden (grep-negativ bestätigt).
- Toter AgentJob: `SQL/AgentJobs/01_Import_Produktionsauftraege.sql` in `_archiv/` mit
  Sweep-Begründung in `_archiv/README.md`; `02_Import_Artikel.sql` bewusst am Ort belassen
  (bereits deprecated markiert).

**Testszenarien:** `docs/TESTSZENARIEN.md` (Worktree) enthält TS-63 (10 Fälle, Etappe B),
TS-64 (8 Fälle, Etappe C), TS-65 (6 Fälle, Etappe D) — alle 15 AK sind auf mindestens ein TS-Szenario
rückführbar. `secondbrain/tests/testszenarien-index.md` (Hauptcheckout) bereits nachgezogen (Zeilen
88–90 Kapitelübersicht, Zeilen 105–106 „nur manuell prüfbar"-Begründungstabelle) — bestätigt, keine
Nacharbeit nötig.

**Deploy-Abschnitt:** geprüft und finalisiert (Spec-Frontmatter `deploy.web/service/migration` = alle
`true`, gegen `git diff --stat main...5cf802e` bestätigt: 85 Dateien in `IdealAkeWms/`+
`IDEALAKEWMSService/`, 2 neue EF-Migrationen). Publish-Befehle im Deploy-Abschnitt jetzt auf den
Worktree-Pfad zugeschnitten, mit Hinweis auf Re-Publish erst nach Merge falls nötig. Reihenfolge
(Backup → `sysjobs`-Prüfung AK 13 → Service stoppen → Migration → Service-Neustart → Web-Publish)
unverändert stimmig, Irreversibilität von Etappe A weiterhin im Deploy-Abschnitt benannt.

**Nicht automatisiert prüfbar (Manual-UAT, für Schranke 2):** siehe TS-63/64/65-Vorbedingungen sowie
die „Datenabhängigen Annahmen" in der Etappe-C-Notiz oben (Mapping `HauptFA==root.SubFA`,
Parent-Kette `child.VaterFA==parent.SubFA`, Melderegel-Schärfung anhand des ersten echten Sage-
Datenlaufs) — inhärent nicht am InMemory-Code verifizierbar, siehe manueller Test-Checkliste unten.

### Manueller Test-Checkliste (für den Menschen, Schranke 2)

1. **Vor allem:** DB-Backup ziehen (Kern-Tabelle `ProductionOrders`, daten-konvertierende Migration).
2. Serverseitig prüfen (AK 13): `sysjobs`/`sysjobsteps`-Abfrage aus dem Deploy-Abschnitt gegen
   `AKESQL20` — aktive Jobs, die `ProductionOrders` schreiben, deaktivieren.
3. Windows-Service stoppen, Migration `SQL/90_InvertProductionOrderHierarchy.sql` einspielen (oder
   `dotnet ef database update`), danach Service neu starten und Web-Publish (Befehle siehe
   Deploy-Abschnitt der Spec).
4. **Regression AKE (AK 1/9):** nach dem Deploy mit Master weiterhin `false` — bestehende Listen
   (`ProductionOrders`, Picking-Leitstand, FA-Worklist, Tracking) auf unveränderte Zeilenzahl/Inhalte
   prüfen.
5. **TS-63.1–63.3:** `/HierarchieUmstellung` aufrufen, Bestätigungsdialog-Wortlaut prüfen, Master
   einschalten/wieder ausschalten (solange keine hierarchischen Daten vorliegen).
6. **TS-63.4–63.6:** eine Testzeile mit `OrderNumber <> SubOrderNumber` einspielen (DB-Kopie),
   Web-App neu starten, prüfen: Umschalt-Seite zeigt „gesperrt" + Runbook-Verweis, generische
   `/ServiceSettings`-Maske bietet kein Schreib-Bedienelement für den Master, ein technischer POST
   wird abgelehnt und im Aktivitäts-Protokoll (`HierarchieUmstellung`) protokolliert.
7. **TS-63.7–63.9:** Audit-Eintrag beim Flip prüfen; Runbook-Link in `README.md` und in der
   Guard-Fehlermeldung stichprobenartig öffnen.
8. **TS-64 (echter Datenlauf, sobald Master aktiv + `Sync:HierarchicalFaEnabled=true`):** neuen
   Sub-FA anlegen → materialisiert (Regel 1); einen materialisierten Sub-FA aus der Quelle entfernen
   → `SageMissingSince` gesetzt statt Löschung, wieder auftauchen lassen → zurückgesetzt (Regel 2,
   TS-64.2/64.3); mehrere gleichzeitig verschwundene FAs → genau **eine** Sammelmail (TS-64.4);
   `VaterFA` eines materialisierten Sub-FA ändern → NICHT übernommen, nur protokolliert (Regel 3,
   TS-64.5). **Dabei klären (TS-64.8):** Wurzel-Mapping (`HauptFA==root.SubFA`) und ob die IDEAL-View
   fertige Aufträge behält oder ausblendet — ggf. Melderegel danach schärfen.
9. **TS-65.2/65.3:** am hierarchischen Testbestand ein „verpackt/abgeholt" an einem HauptFA mit
   mehreren Sub-FAs auslösen → Geschwister bleiben unangetastet, SyncLog-Warnung vorhanden;
   `Sync:FaZusatzinfoAutoErledigtEnabled` lässt sich bei vorhandenen hierarchischen Daten nicht auf
   `true` setzen.
10. Anwender-Changelog (`/Help/Changelog`) und Versionsnummer (v1.32.0) im UI stichprobenartig prüfen.
11. Erst nach erfolgreichem manuellem Test: Merge-Entscheidung (Schranke 2) — Hinweis: dieser Branch
    bringt gleichzeitig die bereits testbereiten Teile 1–5 (v1.31.0) mit.

## Post-QA Manual-UAT-Fund + Fix (2026-08-17) — EF-Migration Index-Tausch guarded

Beim ersten **App-Start** gegen eine echte DB brach `db.Database.Migrate()` (Program.cs:159) mit
**SqlError 3701** ab: `DROP INDEX [IX_ProductionOrders_OrderNumber] ... nicht vorhanden`. **Root Cause
(systematic-debugging):** die EF-Migration `20260814105526_InvertProductionOrderHierarchy` machte
einen **ungeschützten** `DropIndex("IX_ProductionOrders_OrderNumber")`. Auf einer FreshInstall-DB ist
die OrderNumber-Eindeutigkeit ein `UQ_ProductionOrders_OrderNumber`-**Constraint**, kein EF-benannter
`IX_`-Unique-Index → nichts zum Droppen → 3701. Das idempotente `SQL/90` räumte längst **beide**
Formen ab (UQ_-Constraint + IX_-Unique-Index), die EF-Migration nur eine. Der Pfad, der am Start läuft,
ist die **EF-Migration**, nicht das SQL-Skript — deshalb griff der SQL/90-Guard nicht.

**Fix** (Worktree-Commit, nach dem QA-Lauf): die drei Index-Operationen der EF-Migration-`Up()` durch
`migrationBuilder.Sql(@"IF EXISTS/IF NOT EXISTS ...")`-Blöcke ersetzt, deckungsgleich + idempotent mit
SQL/90 (Schritt 4a UQ_+IX_ droppen, 4b non-unique `IX_OrderNumber`, 4c unique `IX_SubOrderNumber`).
Spalten-Ops (AddColumn/Backfill/NOT NULL) unverändert (nicht die Fehlerstelle; EF wickelt die Migration
transaktional ab → der Fehlschlag rollte zurück, DB war unverändert). Build + Web-Tests 1186 grün; die
**DDL-Verifikation ist der erneute App-Start** (nicht InMemory-testbar). Migration-ID/Version unverändert
(v1.32.0, die Migration war nirgends erfolgreich angewandt). Dauerwissen: [[fallstricke]] §9 („EF-Migration
UND idempotentes SQL-Skript brauchen BEIDE dieselben Guards").

**Testbereit bleibt gültig** — der Fund ist genau die Manual-UAT-Klasse („Build+Tests grün ist
Mindestbedingung, nicht Beweis genug"); der Fix stellt her, was die QA (build+test-basiert) annahm.

## Offene Punkte / Deploy-kritisch (aus der Spec, für Schranke 2 / Deploy)

- **DB-Backup vor Deploy zwingend** (Kern-Tabelle `ProductionOrders`, daten-konvertierend).
- **Harte Deploy-Vorbedingung (AK 13):** serverseitig `sysjobs` prüfen, ob ein aktiver Agent-Job
  `ProductionOrders` schreibt → deaktivieren; das Archivieren der Repo-Datei entfernt keinen Job.
- **Irreversibilität:** Etappe A invertiert das Schema unabhängig vom Master-Schalter. Abbruch nach
  A lässt die DB invertiert (inhärent, im Runbook zu benennen — folgt in Etappe B).
- **Kopplung:** selber Branch wie Teile 1–5 (v1.31.0, testbereit) — Merge bringt beides zusammen.
- Schalter-Inventur (S7-8): welche „weiteren abhängigen Schalter" neben Master + Auto-Erledigt —
  vor Etappe B/D explizit benennen.
