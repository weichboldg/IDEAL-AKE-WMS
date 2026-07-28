# IdealAkeWms — Constitution

Warehouse-, Produktions- und Betriebsdatensystem der IDEAL-AKE-Gruppe (Standorte AKE GmbH und
IDEAL), im Produktivbetrieb in Lager und Fertigung.

**Tech-Stack:** ASP.NET Core 10.0 MVC + EF Core 10.0 (SQL Server `AKESQL20.ake.at`, DB
`IDEAL_AKE_WMS`) · Repository-Pattern + DI · Windows-Service fuer Hintergrundjobs · Bootstrap 5 +
jQuery + eigenes JS · Serilog · xUnit/FluentAssertions/Moq/EF-InMemory.

## Build, Test, Run

```bash
dotnet build IdealAkeWms.slnx          # alles
dotnet test                            # alle Tests (Web + Service)
dotnet test IdealAkeWms.Tests          # nur Web
dotnet test IDEALAKEWMSService.Tests   # nur Service
dotnet run --project IdealAkeWms       # Web lokal
dotnet run --project IDEALAKEWMSService # Service lokal
dotnet ef migrations add <Name> --project IdealAkeWms
```

**Build + Tests gruen sind Mindestbedingung, nicht Beweis genug.** Grosse Teile des Systems sind
nicht InMemory-testbar (raw SQL, LDAP, Negotiate-Handshake, Fremdsystem-Reads) — dort gilt
Manual-UAT. Siehe `secondbrain/architektur/fallstricke.md`, Abschnitt „Tests, EF, SQL Server“.

## Brain-first (nicht verhandelbar)

**Vor jeder Aufgabe `secondbrain/` lesen — Einstieg `secondbrain/README.md`.
Keine Aufgabe ist fertig ohne Brain-Update.**

Das Brain ist die Single Source of Truth. Diese Datei ist nur die Verfassung; sie enthaelt
absichtlich keine Tabellen mehr, sondern Verweise:

| Frage | Wo |
|---|---|
| Warum ist etwas so gebaut? | `secondbrain/architektur/adr/` (MADR, fortlaufend nummeriert) |
| Welche Stolperfallen gibt es — und warum? | `secondbrain/architektur/fallstricke.md` |
| Wo liegt was im Code? | `secondbrain/codebase/{module,controller,services,datenmodell,integrationen}.md` |
| Wer darf was? (Rollen, Filter, Zugriffsmatrix) | `secondbrain/codebase/controller.md` + `secondbrain/glossar/glossar.md` |
| Was heisst dieser Begriff? | `secondbrain/glossar/glossar.md` |
| Was existiert schon, in welchem Status? | `secondbrain/feature-map.md` |
| Welches Testszenario gehoert dazu? | `secondbrain/tests/testszenarien-index.md` → `docs/TESTSZENARIEN.md` |
| Alle Service-Keys (`Sync:*`, `Worker*`, `Cleanup:*`, Mails) | `secondbrain/codebase/services.md` |
| Fachliche Feature-Toggles (`AppSettings`, `/Settings`) | `README.md` → „AppSettings" |
| Release-Historie je Version | `secondbrain/changelog/` (ein Eintrag je Version) |
| Offene Arbeit, Deploy-Checklisten | `secondbrain/aufgaben/` |
| Anwender-/Betriebsdoku, Installation, IIS | `README.md` |

Vollstaendige Vorfassung dieser Datei (alle Tabellen und Fallstricke im Original):
`docs/CLAUDE-full-backup-2026-07.md`.

**Additiv schreiben:** `architektur/`, `codebase/`, `glossar/` werden ergaenzt, nie ueberschrieben.
Neue Entscheidung = neuer ADR; ADRs werden nie umgeschrieben, nur superseded.

## Skill-Workflow-Kette (verpflichtend)

1. **Unklare Anforderung** → `superpowers:brainstorming`.
2. **Vor jeder nicht-trivialen Code-Aenderung** → Plan ueber `superpowers:writing-plans`.
   *Trivial* = klar abgegrenzter Einzel-Fix (Typo, eine Konstante, Kommentar). Alles mit
   Architektur-, Datenmodell- oder Mehr-Datei-Impact braucht einen Plan.
3. **Ausfuehrung agentenbasiert** → `superpowers:subagent-driven-development` bzw.
   `superpowers:executing-plans`. Unabhaengige Tasks **parallel** via
   `superpowers:dispatching-parallel-agents`; sequenziell nur bei echten Dependencies.
4. **Debugging** → `superpowers:systematic-debugging` statt Symptom-Patching.
5. **Vor Plan-Abschluss / Commit / PR** → `superpowers:verification-before-completion` **und**
   Code-Review (`code-review` bzw. `superpowers:requesting-code-review`).

**Pipeline-Rollen:** `task-scout` (unverarbeitete Backlog-Dateien finden) → `spec-agent` (Spec nach
`secondbrain/specs/entwurf/`) → Umsetzung im Worktree → `qa-agent` (darf als Einziger
`status: Testbereit` setzen, nur mit gruenem Beweis).
Status-Automat: NEU → SPEZIFIZIERT → **Schranke 1 (Mensch)** → FREIGEGEBEN → IN_UMSETZUNG →
TESTBEREIT → **Schranke 2 (Mensch: manueller Test, dann Merge)** → GEMERGED.

## Harte Regeln

**Isolation per Worktree.** Groessere Aenderungen (Multi-Task-Rollouts, Phasen-Refactors, neue
Module mit Spec+Plan) laufen **immer** in einem eigenen Worktree, nie direkt auf `main`:
`powershell -ExecutionPolicy Bypass -File scripts/new-worktree.ps1 -Slug <slug>` bzw.
`git worktree add .claude/worktrees/<slug> -b <branch>`. Trivial-Schwelle: 1–2 Datei-Fixes,
Doku-Tweaks, einzelne Konstanten. Laeuft die Session schon in einem Worktree, dort weiterarbeiten.
**Worktree/Branch nie autonom aufraeumen** — auch nach dem Merge erst auf explizite Freigabe.

**Migrations- und SQL-Disziplin.** Jede Schema-Aenderung dreifach, in dieser Reihenfolge:
Model → `dotnet ef migrations add` → idempotentes `SQL/XX_<Name>.sql` mit `OBJECT_ID`-Guard (DDL in
eigenem Batch) → `__EFMigrationsHistory`-Insert in separatem Batch → `SQL/00_FreshInstall.sql`
**an zwei Stellen** nachziehen (Schema-Objekte **und** `MigrationId`). Aendert sich ein
Pflichtfeld oder ein Folge-MERGE, ziehen die Skripte in `SQL/AgentJobs/` **im selben
Wartungsfenster** mit. Daten-destruktive Migrationen als solche kennzeichnen und „DB-Backup vor
Deploy" dokumentieren. Details: ADR `0004-migrations-und-sql-disziplin`.

**Audit-Felder.** Fachliche Entitaeten erben `AuditableEntity`. Bei **jedem** Update
`ModifiedAt`, `ModifiedBy`, `ModifiedByWindows` setzen (aus `ICurrentUserService`; Service-Syncs
schreiben ihren Service-Namen). Details: ADR `0003-auditableentity-als-entity-basis`.

**Muster-Pflichten.**
- *Listen-Views*: Pagination (`PageSize.Resolve` + `PaginationState` + `_Pagination`-Partial, kein
  hartcodierter Take/Cap), Filterkarte, und **Spaltenfilter fuer jede Tabelle** — Standard ist
  Server-Mode (`data-server-column-filter="true"`, alle `<th>` mit `data-col-key`,
  `ColumnFilterHelper.ReadFromQuery`). Client-Mode nur fuer kleine, unpaginierte, vorgefilterte
  Ansichten. Datumsspalten in C# **nach** der Termin-Berechnung filtern. Referenz:
  `ProductionOrdersController.Index` + `Views/ProductionOrders/Index.cshtml`. Details: ADR `0005-listen-view-pattern-mit-server-side-spaltenfilter`.
- *Controller*: MVC → `ModelState` + `TempData["SuccessMessage"]` + `RedirectToAction`;
  API → `ControllerBase` + `[ApiController]` + `Ok()`/`NotFound()`/`BadRequest()`. TempData kennt
  nur `SuccessMessage` und `WarningMessage` — **kein** `ErrorMessage`.
- *Datenzugriff*: nur ueber Repository-Interfaces; Caching als Decorator. Details: ADR `0001-repository-pattern-mit-decorator-fuer-caching`.
- *Zugriffsschutz*: `RequireXxxAccess`-Filter, Read/Edit-Split (Class-Level read, Action-Level
  edit), Feature-Gates **kumulativ** dazu. Details: ADR `0006-rollenkonzept-statische-keys-mit-admin-wildcard` +
  ADR `0011-feature-toggles-ueber-appsettings`.
- *Hintergrund-Services*: Aktivitaets-Protokoll ist Pflicht (`ISyncLogger` als **letzter**
  Ctor-Parameter, Service-Name in `SyncLogServices.All`, deutsche Counts-Keys). Details: ADR `0010-aktivitaets-protokoll-mit-isolierten-dbcontexts`.
- *Service-Konfiguration*: neuer Key **immer** in `ServiceSettingDefinitions.All` (Drift-Guard-Test).
  Details: ADR `0008-servicesettings-db-first-mit-typisiertem-katalog`.

**Sprachregel.** Code, Variablen, Klassen, Routen auf **Englisch**; UI-Texte auf **Deutsch**.
Bestehende bewusste Asymmetrien zwischen Code- und UI-Namen nicht „aufraeumen" — sie sind im
Glossar begruendet.

**Einfachheit und Verifikation.** Root Cause statt Symptom, minimale Code-Auswirkung, kein
Over-Engineering. Niemals eine Aufgabe als erledigt melden ohne Beweis (Build, Tests, View).

**Testszenarien-Pflicht.** Zu **jedem** Feature und **jedem** Bugfix ein vollstaendiges manuelles
Szenario liefern (Vorbedingungen, Schritte, erwartetes Verhalten, Negativfaelle) **und**
`docs/TESTSZENARIEN.md` synchronisieren — das Dokument ist die Single Source of Truth der Abnahme.
Danach `secondbrain/tests/testszenarien-index.md` nachziehen.

## Checkliste nach Aenderungen

- [ ] Migration erstellt + `SQL/XX_*.sql` mit `OBJECT_ID`-Guard?
- [ ] `SQL/00_FreshInstall.sql` an **beiden** Stellen aktualisiert (Schema + `MigrationId`)?
- [ ] `SQL/AgentJobs/*` betroffen? Im selben Wartungsfenster mitziehen?
- [ ] Audit-Felder gesetzt (`ModifiedAt`, `ModifiedBy`, `ModifiedByWindows`)?
- [ ] Version in **beiden** `AppVersion.cs` (Web + Service) hochgezaehlt + Anwender-Changelog
      `Views/Help/Changelog.cshtml` ergaenzt?
- [ ] **Brain-Changelog** (zusaetzlich zum Anwender-Changelog) in `secondbrain/changelog/`
      (`YYYY-MM-DD-vX-Y-Z-slug.md`) —
      Kernpunkte, Migrationsnummern, Deploy-Risiken, Merge-Commit?
- [ ] **Brain: `secondbrain/feature-map.md`** aktualisiert (Status, Spec, Code-Einstieg)?
- [ ] Offene Folgearbeit als Aufgabe in `secondbrain/aufgaben/` festgehalten (statt in Prosa)?
- [ ] `README.md` (Betrieb/AppSettings) und Hilfeseite gepflegt?
      *(`PROJECT_STATUS.md` ist seit 2026-07 eingefroren — nicht mehr pflegen.)*
- [ ] Testszenarien in `docs/TESTSZENARIEN.md` ergaenzt + `secondbrain/tests/testszenarien-index.md`
      nachgezogen?
- [ ] **Brain: Dauerwissen** — ggf. neuer ADR, `fallstricke.md` (mit **Warum**), `codebase/*`,
      `glossar/*`?
- [ ] Neue Rolle/neuer Filter? Dann **drei** Stellen: Attribut,
      `secondbrain/codebase/controller.md`, `Views/Users/RoleOverview.cshtml`.
- [ ] `superpowers:verification-before-completion` + Code-Review durchlaufen?
