# Architektur

Das „Warum" des Projekts. Regel: **Neue Entscheidung = neuer ADR.** ADRs werden nie
umgeschrieben, nur superseded (`superseded_by` im Frontmatter setzen).

- `adr/` — Architecture Decision Records im MADR-Format, fortlaufend nummeriert.
- `muster/` — verbindliche Muster (aktuell in den ADRs 0001/0005 beschrieben).
- [[fallstricke]] — bekannte Stolperfallen, jede mit Begruendung.

## ADR-Index

Die ADRs 0001–0011 sind die **nachtraegliche Erfassung der Bestandsarchitektur** (2026-07-27,
Nacherfassung Schritt 5): Entscheidungen, die vor Einfuehrung des Second Brain getroffen und
gelebt wurden. Sie tragen `status: accepted` und einen entsprechenden Vermerk. Ab hier gilt „nur
noch vorwaerts".

| ADR | Entscheidung | Datum |
|---|---|---|
| [[0001-repository-pattern-mit-decorator-fuer-caching]] | Datenzugriff ueber Repository-Interfaces, Caching als Decorator | 2026-03-10 |
| [[0002-dual-auth-session-login-plus-windows-sso]] | Dual-Auth — Session-Login plus optionales Windows-SSO ueber die IIS-Integration | 2026-06-18 |
| [[0003-auditableentity-als-entity-basis]] | Alle fachlichen Entitaeten erben Audit-Felder von `AuditableEntity` | 2026-02-16 |
| [[0004-migrations-und-sql-disziplin]] | Jede Schema-Aenderung dreifach — EF-Migration, idempotentes SQL, FreshInstall | 2026-03-11 |
| [[0005-listen-view-pattern-mit-server-side-spaltenfilter]] | Einheitliches Listen-View-Pattern mit server-seitigem Spaltenfilter | 2026-05-22 |
| [[0006-rollenkonzept-statische-keys-mit-admin-wildcard]] | Statische Rollen-Keys, Admin-Wildcard, ausschliesslich explizite Zuweisung | 2026-03-20 |
| [[0007-bom-quelle-sage-view-mit-oseon-fallback]] | Stuecklisten primaer aus der Sage-View, Fallback auf OSEON | 2026-03-10 |
| [[0008-servicesettings-db-first-mit-typisiertem-katalog]] | Service-Konfiguration DB-first ueber einen typisierten Katalog | 2026-07-08 |
| [[0009-app-status-in-satelliten-tabellen-neben-sage-master]] | App-Status in Satelliten-Tabellen neben den Sage-Master-Daten | 2026-05-12 |
| [[0010-aktivitaets-protokoll-mit-isolierten-dbcontexts]] | Aktivitaets-Protokoll mit isolierten DbContexts je Zeile | 2026-05-26 |
| [[0011-feature-toggles-ueber-appsettings]] | Fachmodule ueber AppSettings-Toggles freischalten, Default aus | 2026-04-03 |

## Neuen ADR anlegen

1. `_templates/adr.md` kopieren nach `adr/NNNN-<entscheidung-als-slug>.md`.
2. Frontmatter fuellen (`id`, `title` als **Aussage**, `status`, `date`).
3. Kontext → Optionen → Entscheidung → Konsequenzen (positiv **und** negativ/Risiken).
4. Hier in den Index eintragen und aus betroffenen Brain-Seiten verlinken.
