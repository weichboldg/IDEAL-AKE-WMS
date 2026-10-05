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

## Skill `ponytail:ponytail` (nicht verhandelbar)

**Bei jeder Aufgabe geladen und verwendet** — unabhaengig vom Aufgabentyp, ohne Ausloeser-Bedingung
und ohne Ausnahme. Gilt gleichrangig neben Brain-first, nicht nur bei bestimmten Aenderungsarten
(anders als z.B. `frontend-design`, das nur bei UI-Aenderungen greift).

**Was er tut:** Er laesst den Agenten wie den faulsten erfahrenen Entwickler im Raum denken — *der
beste Code ist der, den man nie geschrieben hat.* Vor dem Schreiben haelt er an der ersten Sprosse,
die traegt:

```
1. Muss das ueberhaupt existieren?   -> nein: weglassen (YAGNI)
2. Gibt es das im Codebestand schon? -> wiederverwenden statt neu schreiben
3. Kann die Standardbibliothek das?  -> nehmen
4. Gibt es ein natives Plattform-Mittel? -> nehmen
5. Kann eine vorhandene Abhaengigkeit das? -> nehmen
6. Geht es in einer Zeile?           -> eine Zeile
7. Erst dann: das Minimum, das funktioniert
```

Die Leiter laeuft **nachdem** er das Problem verstanden hat, nicht statt dessen: erst den
betroffenen Code lesen und den echten Ablauf verfolgen, dann die Sprosse waehlen.
**Faul in der Loesung, nie im Lesen.**

Quelle: https://github.com/DietrichGebert/ponytail (MIT). Installiert als Claude-Code-Plugin
(`/plugin marketplace add DietrichGebert/ponytail`, dann `/plugin install ponytail@ponytail`).
Befehle: `/ponytail [lite|full|ultra|off]`, `/ponytail-review` (Diff auf Over-Engineering),
`/ponytail-audit` (ganzes Repo), `/ponytail-debt` (aufgeschobene `ponytail:`-Abkuerzungen ernten).

**Warum er hier passt:** Die besten Entscheidungen dieses Projekts folgen genau dieser Leiter —
`HierarchischeStrukturStatus` als Vorlage statt eines neuen Status-Halters, das
`BomDiResolutionTests`-Muster statt eines neuen Testansatzes, das OSEON-Baummuster statt einer
zweiten Baumimplementierung, der BOM-Guard statt der Volloesung.

**Wo die Hausregeln Vorrang haben — ausdruecklich, damit „faul" nicht falsch gelesen wird:**
ponytail schneidet ohnehin nie Validierung, Fehlerbehandlung, Sicherheit oder Barrierefreiheit weg.
Hier kommen drei Dinge dazu, die **ebenfalls nie** der Leiter zum Opfer fallen:
- **Melden statt still behandeln.** Sichtbare Banner, Sammelmeldungen und Invarianten-Warnungen
  sind hier Pflicht, nicht Beiwerk — sie haben in diesem Projekt mehrfach falsche Annahmen
  aufgedeckt (`SubFA = 0`, Umhaeng-Konflikte, unbekannte Arbeitsbereiche).
- **Testszenarien-Pflicht und Brain-Update** gelten unveraendert. „Weniger Code" heisst nicht
  „weniger Nachweis".
- **Dauerwissen festhalten** (ADR, `fallstricke.md`) bleibt Pflicht — eine gesparte Zeile Code
  rechtfertigt keine gesparte Zeile Begruendung.

## Skill-Workflow-Kette (verpflichtend)

1. **Unklare Anforderung** → `superpowers:brainstorming`.
2. **Vor jeder nicht-trivialen Code-Aenderung** → Plan ueber `superpowers:writing-plans`.
   *Trivial* = klar abgegrenzter Einzel-Fix (Typo, eine Konstante, Kommentar). Alles mit
   Architektur-, Datenmodell- oder Mehr-Datei-Impact braucht einen Plan.
3. **Ausfuehrung** → `superpowers:executing-plans` bzw. `superpowers:subagent-driven-development`.
   Subagenten fuer grosse, wirklich unabhaengige Teilaufgaben, dann parallel via
   `superpowers:dispatching-parallel-agents`; nicht fuer kleine Schritte und nie zum Gegenpruefen der
   eigenen Arbeit. Grund: Die aktuellen Modelle delegieren von sich aus bereitwillig, und jede
   Ebene kostet Kontext und Kontingent.
4. **Debugging** → `superpowers:systematic-debugging` statt Symptom-Patching.
5. **Unabhaengiges Code-Review durch den `qa-agent`**, bevor etwas Testbereit wird. Es laeuft
   synchron in dessen Lauf und endet mit festgehaltenen Befunden in der Spec. Zweimal ging ein
   Review verloren, weil es an einen Hintergrundprozess bzw. weiteren Unteragenten abgegeben
   wurde — ein Review ohne zurueckgekehrtes Ergebnis gilt als nicht durchgefuehrt.
   Allgemeine Selbstpruef-Anweisungen ("pruefe nochmal", "Verifikationsschritt anhaengen") entfallen
   bewusst: Die aktuellen Modelle pruefen ihre Arbeit von sich aus, zusaetzliche Aufforderungen
   erzeugen laut Anthropic nur Mehrfachpruefung. Die Beweispflicht (Build- und Testausgaben,
   manuelle Checkliste) bleibt — sie ist ein Ergebnis fuer den Menschen, keine Selbstpruefung.

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

**Das Brain wird NICHT verzweigt.** `secondbrain/` liegt zwar im Repo, gehoert aber nicht zum
Zweig: Es beschreibt die Arbeit, statt Teil von ihr zu sein. Deshalb gilt bei jedem Worktree-Lauf
eine harte Trennung der Schreibziele:
- **In den Worktree** (Zweig-Inhalt): Anwendungscode, `SQL/`, `docs/`, Tests, Versions-Bump.
- **Immer in den HAUPTCHECKOUT `C:\Git\IDEAL-AKE-WMS\secondbrain\`**: Spec-Aenderungen (Status,
  worktree/branch, QA-Nachweis, Deploy-Abschnitt), Aufgaben-Notizen, codebase-Karte, Testindex,
  changelog, ADRs.

Grund: Das Obsidian des Menschen und das HOME-Dashboard zeigen auf den Hauptcheckout. Landen
Statuswechsel im Worktree, ist das Cockpit genau waehrend der Arbeit blind, und ein verworfener
Zweig nimmt das Wissen mit. Nebeneffekt: Brain-Merge-Konflikte koennen so gar nicht entstehen.

`scripts/new-worktree.ps1` blendet `secondbrain/` per sparse-checkout aus dem Worktree aus. Ist
dort wider Erwarten doch ein `secondbrain/`-Ordner sichtbar: NICHT hineinschreiben, sondern in den
Hauptcheckout schreiben und den Fund melden.

**Frontend-Arbeit: `frontend-design`-Skill ist PFLICHT.** Sobald ein Lauf Views, CSS oder
Oberflaechen-JavaScript aendert, wird der Skill `frontend-design` **explizit aufgerufen** — nicht
auf automatische Ausloesung verlassen. Er liefert das Handwerk fuer Typografie, Kontrast, Abstaende
und visuelle Hierarchie.
Zwei Leitplanken dazu, die Vorrang haben:
- **Konsistenz vor Eigenstaendigkeit.** Die Anwendung ist ein Werkzeug in der Fertigung, kein
  Schaustueck. Bootstrap 5, bestehende Muster und vorhandene Partials gewinnen, wo der Skill zu
  einem eigenen Stil draengt. Keine neue UI-, Icon- oder Chart-Bibliothek ohne ADR.
- **Kontrast ist Funktion, nicht Geschmack.** Zielwert WCAG AA (4,5:1 Fliesstext, 3:1 grosse
  Schrift und Bedienelemente). Die Oberflaechen laufen an Terminals in der Fertigung, teils bei
  schlechtem Licht. Farbe darf nie alleiniger Bedeutungstraeger sein.

**Brain-Notizen sind Obsidian-Markdown.** Alles unter `secondbrain/` wird in Obsidian gelesen —
daher gilt beim Schreiben von Notizen (Skill `obsidian-markdown` fuer Details):
- Vault-interne Verweise **immer** als Wikilink `[[Notizname]]` (ohne Pfad, ohne `.md`), nie als
  Pfad-String. Dataview verknuepft nur ueber echte Wikilinks — ein Pfad-String laesst das
  HOME-Dashboard den Eintrag faelschlich als offen zeigen.
- Wikilinks im **Frontmatter** in Anfuehrungszeichen: `source_backlog: "[[2026-07-28-foo]]"`
  (sonst bricht YAML an den Klammern).
- **Namenskollision vermeiden:** Specs enden auf `-spec`, Bug-Records auf `-bug`. Heissen Backlog-
  Notiz und Spec gleich, loest der Wikilink auf die Spec selbst auf statt auf den Backlog-Eintrag.
- Vault-Dateien **nicht** auf Dateisystem-Ebene umbenennen/verschieben, ohne die Wikilinks
  nachzuziehen — Obsidians Auto-Update greift nur bei Aenderungen in der App.
- Frontmatter valides YAML, externe Links als `[text](url)`, Hervorhebungen/Callouts nur wo sie
  Inhalt tragen.

**Sprachregel.** Code, Variablen, Klassen, Routen auf **Englisch**; UI-Texte auf **Deutsch**.
Bestehende bewusste Asymmetrien zwischen Code- und UI-Namen nicht „aufraeumen" — sie sind im
Glossar begruendet.

**Einfachheit und Verifikation.** Root Cause statt Symptom, minimale Code-Auswirkung, kein
Over-Engineering. Keine Aufgabe als erledigt melden ohne Beweis (Build, Tests, View). Umfang:
liefern, was verlangt ist — Zusatzfunde melden, nicht mitbauen.

**Belegen statt deuten.** Aussagen ueber Code und Daten mit Fundstelle belegen (Datei:Zeile,
Abfrage). Die Bedeutung eines Feldes, einer Tabelle oder Spalte nie aus dem Namen ableiten, sondern
nachsehen, wo es geschrieben und gelesen wird. Negative Aussagen ("gibt es nicht", "null-sicher")
brauchen denselben Beleg ueber alle Wege, Schreibwege eingeschlossen. Was nur in Sage oder einem
anderen Fremdsystem steht, wird nicht angenommen, sondern als Pruefabfrage fuer den Menschen
formuliert. Grund: Fast jede Korrekturrunde im September 2026 ging auf einen Schluss aus einem Namen
oder eine ungepruefte Negativaussage zurueck.

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
- [ ] Unabhaengiges Code-Review (`qa-agent`) mit festgehaltenen Befunden in der Spec?
