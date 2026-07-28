---
type: adr
id: 0003
title: Alle fachlichen Entitaeten erben Audit-Felder von AuditableEntity
status: accepted
date: 2026-02-16
supersedes: ""
superseded_by: ""
---

> **Nachtraeglich erfasst** (2026-07-27). Bestandsdokumentation einer der aeltesten Konventionen
> des Projekts.

## Kontext und Problem

Das WMS ist ein Produktionssystem, in dem Buchungen, Freigaben und Statuswechsel von mehreren
Personen und zusaetzlich von automatischen Syncs geschrieben werden. Bei Rueckfragen im Lager
(„wer hat das ausgebucht?", „warum ist der FA erledigt?") muss nachvollziehbar sein, wer wann
geschrieben hat — inklusive der Unterscheidung zwischen App-Benutzer und Windows-Konto bzw.
Service.

## Betrachtete Optionen

- **Audit-Felder pro Tabelle nach Bedarf** — minimaler Schema-Overhead, aber Luecken genau dort,
  wo sie spaeter weh tun; jede Tabelle diskutiert die Frage neu.
- **Separate Audit-/History-Tabelle (Event-Log)** — vollstaendige Historie, aber deutlich mehr
  Infrastruktur und Schreiblast als der Anwendungsfall hergibt.
- **Gemeinsame Basisklasse mit Audit-Feldern** — einheitlich, billig, ausreichend fuer
  „wer war zuletzt dran".

## Entscheidung

`../../../IdealAkeWms/Models/AuditableEntity.cs` ist die Basis fuer fachliche Entitaeten und
liefert: `Id`, `CreatedAt`, `CreatedBy`, `CreatedByWindows`, `ModifiedAt?`, `ModifiedBy?`,
`ModifiedByWindows?`.

Regeln:
- `CreatedBy` / `ModifiedBy` = App-Benutzername aus `ICurrentUserService`.
- `CreatedByWindows` / `ModifiedByWindows` = Windows-Konto (bzw. `"SYSTEM"`, wenn nicht
  ermittelbar; Service-Syncs schreiben ihren Service-Namen, z. B. `FaZusatzinfoSync`).
- Bei **jedem** Update sind `ModifiedAt` / `ModifiedBy` / `ModifiedByWindows` zu setzen — das ist
  ein fester Punkt der Aenderungs-Checkliste in CLAUDE.md.

**Bewusste Ausnahme:** `AppSetting` erbt **nicht** von `AuditableEntity` — die Tabelle hat nur
`Key` (PK), `Value`, `Description`. Sie ist eine Konfigurations-Key-Value-Ablage, keine
fachliche Entitaet.

## Konsequenzen

**Positiv**
- Jede fachliche Zeile beantwortet „wer, wann, womit" ohne Zusatzabfrage.
- Neue Entitaeten bekommen Audit gratis; kein Review-Streit pro Tabelle.

**Negativ / Risiken**
- Nur „letzter Schreiber", keine Historie. Wer echte Verlaeufe braucht (z. B. Bestandsbewegungen,
  BDE-Buchungen), modelliert das explizit als eigene Bewegungs-/Buchungszeile.
- Das Setzen der Modified-Felder ist Konvention, nicht erzwungen — vergessene Zuweisungen fallen
  nur im Review auf.
- Beim Formular-Login ist `GetWindowsUserName()` seit dem Antiforgery-Wurzel-Fix `"SYSTEM"`
  (die `/account/*`-Identitaets-Normalisierung verhindert das Live-Lesen). Bewusst akzeptiert;
  `ModifiedBy` (App-User) bleibt korrekt. Siehe [[0002-dual-auth-session-login-plus-windows-sso]].
