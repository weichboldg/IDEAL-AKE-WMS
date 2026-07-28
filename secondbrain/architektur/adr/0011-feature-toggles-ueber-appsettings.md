---
type: adr
id: 0011
title: Fachmodule werden ueber AppSettings-Feature-Toggles freigeschaltet, standardmaessig aus
status: accepted
date: 2026-04-03
supersedes: ""
superseded_by: ""
---

> **Nachtraeglich erfasst** (2026-07-27). Bestandsdokumentation; das Muster begann mit
> `TeileverfolgungAktiv`/`LeitstandAktiv` und wurde seither fuer jedes neue Modul verwendet.

## Kontext und Problem

Das WMS laeuft an zwei Standorten (AKE und IDEAL) mit unterschiedlichem Funktionsumfang und wird
im laufenden Produktionsbetrieb aktualisiert. Neue Module (Leitstand, BDE, FA-Vervollstaendigung,
Bestellwesen, Windows-SSO) sollen deployt werden koennen, **bevor** die Organisation sie nutzt —
Stammdaten, Schulung und Prozesse brauchen Vorlauf. Ein Modul, das mit dem Deploy sofort im Menue
erscheint und halb befuellte Listen zeigt, erzeugt Support-Last und Misstrauen.

## Betrachtete Optionen

- **Feature-Branches bis zur Freischaltung offen halten** — kein Schalter im Code, aber lange
  divergierende Branches und riskante Merges (die Erfahrung mit `feature/ideal-anpassungen-v1`
  belegt das).
- **Compile-Time-Schalter / getrennte Builds pro Standort** — saubere Trennung, aber zwei
  Artefakte zu pflegen und zu deployen.
- **Runtime-Toggle in der Datenbank** — ein Artefakt, Freischaltung durch den Admin ohne Deploy.

## Entscheidung

Fachmodule haengen an einem Schalter in der `AppSettings`-Tabelle (Key/Value/Description,
gepflegt unter `/Settings`). Konvention:

- **Default `false`** (Opt-in) — ein Modul erscheint erst, wenn der Admin es einschaltet.
  Beispiele: `TeileverfolgungAktiv`, `LeitstandAktiv`, `BestellungenAktiv`, `BdeAktiv`,
  `FaCompletionAktiv`, `WindowsAuthAktiv`.
- **Begruendete Ausnahme `LagerbestellungAktiv`: Default `true`** — das Modul war bereits
  produktiv im Einsatz, als der Master-Schalter nachtraeglich eingezogen wurde; bestehende
  Systeme durften nicht dunkel werden. Die Read-Semantik ist deshalb explizit
  „nur `"false"` sperrt" (`!string.Equals(raw, "false", OrdinalIgnoreCase)`) und **nicht** das
  `?.Equals("true") == true`-Muster, das auf false defaultet.
- Der Schalter wirkt an **drei** Stellen: Menue-Sichtbarkeit im Layout, Controller-Zugriff, und
  ggf. abhaengige UI-Elemente (z. B. der Lagerbestellungs-Button in der Stueckliste).
- Durchsetzung am Controller erfolgt ueber ein eigenes Filter-Attribut, **kumulativ** zum
  Rollen-Filter — nicht als Ersatz (siehe [[0006-rollenkonzept-statische-keys-mit-admin-wildcard]]).
  Referenz: `RequireLagerbestellungAktivAttribute` — MVC → Redirect auf Home + WarningMessage,
  API → 404.
- Manche Toggles sind nicht bool, sondern „leer = inaktiv" (`LackierteilKategorieName`,
  `DefaultLagerbestellempfaengerId`, `BdeDefaultArbeitsgang`).

Abgegrenzt davon: die **Service**-Schalter (`Sync:*`) liegen nicht in `AppSettings`, sondern im
typisierten `ServiceSettings`-Katalog — siehe
[[0008-servicesettings-db-first-mit-typisiertem-katalog]].

## Konsequenzen

**Positiv**
- Deploy und Inbetriebnahme sind entkoppelt; ein Release kann fertigen, aber ruhenden Code
  enthalten.
- Standort-Unterschiede ohne getrennte Builds.
- Im Stoerfall ist ein Modul in Sekunden abschaltbar, ohne Rollback.

**Negativ / Risiken**
- Ruhender Code wird im Alltag nicht durchlaufen und veraltet unbemerkt — er ist nur so gut wie
  die Testszenarien, die ihn abdecken.
- Der Toggle muss an allen drei Stellen konsistent gelesen werden; eine vergessene Stelle
  bedeutet entweder einen Menuepunkt ins Leere oder eine erreichbare URL trotz „aus".
- Die Kombinatorik der Zustaende (Toggle × Rolle) waechst und ist nicht vollstaendig testbar.
- Die Read-Semantik ist bei Default-true-Schaltern eine echte Falle — siehe [[fallstricke]].
