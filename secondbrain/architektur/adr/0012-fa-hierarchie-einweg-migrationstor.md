---
type: adr
id: 0012
title: FA-Hierarchie ist eine einmalige Schema-Inversion hinter einem datengetriebenen Einweg-Migrationstor
status: accepted
date: 2026-08-17
supersedes: ""
superseded_by: ""
---

## Kontext und Problem

Der Standort IDEAL fertigt mehrstufig (Haupt-FA → Sub-FA → …). Damit Sub-FAs rückmeldefähig werden
(Arbeitsgänge, Teileverfolgung, BDE — alle arbeiten ausschließlich gegen `ProductionOrders`), müssen
sie als echte `ProductionOrders` materialisiert werden. Das bricht eine tragende Invariante des
AKE-Betriebs: `OrderNumber` war **unique** (eine Zeile je FA). Im hierarchischen Modus teilen Haupt-FA
und alle Sub-FAs dieselbe `OrderNumber`.

Zwei Dinge sind gefährlich: (1) die Schema-Änderung trifft die **zentralste Tabelle** des Systems,
und (2) eine einmal materialisierte Hierarchie lässt sich nicht gefahrlos zurückdrehen — an den
Sub-FA-Zeilen hängen dann Rückmeldungen. Die Entscheidung muss verhindern, dass jemand versehentlich
umstellt, und gleichzeitig AKE zu **100 %** unverändert lassen.

## Betrachtete Optionen

- **Schaltbares Schema** (`SubOrderNumber` mal unique, mal nicht) — technisch unmöglich/instabil; ein
  Unique-Index ist nicht „halb an".
- **Zweiter Import-Pfad** (Struktur separat nach `ProductionOrders` importieren) — läuft mit dem
  Struktur-Cache auseinander, doppelte Datenpflege.
- **Betriebsschalter** (jederzeit an/aus) — suggeriert Reversibilität, die es nach dem ersten Import
  nicht mehr gibt; verleitet zum Zurückschalten mit Datenverlust.
- **Einmalige, unbedingte Schema-Inversion + einmaliges Migrationstor + abgeleitete
  Materialisierung** (gewählt).

## Entscheidung

1. **Schema-Inversion ist einmalig und unbedingt** (nicht schaltbar): `SubOrderNumber` wird
   NOT-NULL/unique, `OrderNumber` verliert die Eindeutigkeit, `ParentSubOrderNumber` (echter
   Elternzeiger) und `SageMissingSince` (Zeitstempel) kommen dazu. Backfill setzt
   `SubOrderNumber = OrderNumber` → im flachen AKE-Modus gilt die **Invariante
   `OrderNumber == SubOrderNumber`**, Verhalten identisch zu vorher. Migration daten-konvertierend
   (DB-Backup zwingend), Index-Tausch mit `sys.indexes`-Guard idempotent.

2. **Der Master `ProduktionsauftragHierarchisch` ist ein Migrationstor, kein Betriebsschalter
   (EINWEG).** Die Sperrbedingung ist **datengetrieben**, nicht schaltergetrieben:
   `EXISTS(ProductionOrders WHERE OrderNumber <> SubOrderNumber)`. Vor dem ersten hierarchischen
   Import frei änderbar; danach nicht mehr über die Anwendung deaktivierbar. Rückweg nur als bewusste
   Datenbereinigung außerhalb der App (`docs/RUNBOOK-FA-HIERARCHIE-RUECKBAU.md`). Formulierung
   durchgängig „nicht über die Anwendung umkehrbar", nicht „unmöglich".

3. **Der Wächter ist ein reiner, unit-getesteter Planer, durchgesetzt als Repository-Decorator
   (einziger Choke-Point).** `HierarchischeStrukturGuard.Evaluate(key, current, requested,
   dataExists)` entscheidet ohne DB; `GuardedServiceSettingRepository` sitzt auf
   `IServiceSettingRepository.UpsertAsync/DeleteAsync` — die einzige Datenzugriffs-Naht, durch die
   **alle** ServiceSettings-Schreibwege laufen. Kein Caller (auch keine künftige API) kann das Tor
   umgehen; die generische Einstellungs-Maske zeigt den Master nur read-only, geschrieben wird nur
   über die dedizierte Umschalt-Seite. Live-Prüfung auf dem Schreibpfad, **gecachter** Zustand für die
   Anzeige (Performance: kein `EXISTS` je Request).

4. **`ProductionOrders` ist abgeleitet, nicht importiert.** Ein Materialisierungs-Sync transformiert
   die Struktur (`FaHierarchyNode`) nach `ProductionOrders` mit drei Sync-Regeln (anlegen / nie
   löschen + `SageMissingSince` selbstheilend / Umhängung melden), als reiner Planer mit
   Empty-Source-Guard. So laufen Struktur und Aufträge nicht auseinander, und die Transformation ist
   ohne DB testbar.

5. **`OrderNumber`-Lookups werden gehärtet, nicht flächig umgestellt.** Eindeutigkeits-Lookups, die
   mehrdeutig werden könnten, bekommen eine **mengenwertige Naht** (z. B.
   `GetAllByFaAndOperationAsync`); der bestehende Einzel-Lookup bleibt verhaltensgleich und
   protokolliert Mehrfachtreffer. Die Umstellung der Aufrufer ist Teil 8 — so bleibt Teil 7 für sich
   mergebar. Auto-Erledigt (Fold 2) wird dreistufig gesperrt (datengetrieben + Schalter + Test), damit
   ein „verpackt" am Haupt-FA nicht die ganze Gruppe schließt.

## Konsequenzen

- **Gut:** AKE byte-identisch (Invariante + alle Toggles aus); die Einwegtür ist technisch
  erzwungen, nicht nur per UI versteckt; die Kernlogik (Guard + Sync-Regeln) ist reiner, testbarer
  Code; die Schema-Inversion passiert genau einmal.
- **Schlecht/Risiko:** die Migration ist daten-konvertierend auf der zentralsten Tabelle (Backup +
  Agent-Job-Prüfung Pflicht); nach Etappe A ist die DB invertiert, auch wenn der Master nie umgelegt
  wird (inhärent); die 7 kritischen `OrderNumber`-UPDATE/Upsert-Pfade sind erst mit Teil 8 vollständig
  hierarchie-fest — bis dahin am IDEAL deaktiviert.
- **Verweise:** baut auf [[0008-servicesettings-db-first-mit-typisiertem-katalog]] (Guard als
  Decorator um das Katalog-Repo), [[0010-aktivitaets-protokoll-mit-isolierten-dbcontexts]] (Audit
  `HierarchieUmstellung`/`FaMaterialization`), [[0004-migrations-und-sql-disziplin]] (SQL/90 +
  FreshInstall). Details/Deploy: Changelog [[2026-08-17-v1-32-0-ideal-teil-7]].
