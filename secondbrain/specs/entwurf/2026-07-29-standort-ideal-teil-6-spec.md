---
type: spec
title: "IDEAL-Standort Teil 6 — Standorteinstellungen-Maske"
slug: 2026-07-29-standort-ideal-teil-6-spec
status: Entwurf
created: 2026-08-06
updated: 2026-08-06
source_backlog: "[[2026-07-29-Standort-IDEAL]]"
depends_on: "[[2026-07-29-standort-ideal-teil-1-spec]]"
task: ""
worktree: ""
branch: ""
affected_code:
  - IdealAkeWms/Controllers/StandortEinstellungenController.cs (neu, Name provisorisch)
  - IdealAkeWms/Views/StandortEinstellungen/Index.cshtml (neu)
  - IdealAkeWms/Models/ServiceSettingDefinitions.cs
  - IdealAkeWms/Models/AppSettingKeys.cs
  - docs/TESTSZENARIEN.md
  - secondbrain/tests/testszenarien-index.md
open_questions:
  - "Referenz-Notiz [[2026-08-03-standorteinstellungen-maske]] existiert nicht in secondbrain/ideen/ — Inhalt/Umfang dieser Maske ist ausschliesslich aus der Standort-IDEAL-Notiz abgeleitet, nicht aus einer eigenen Spezifikation"
  - "Vollstaendige Liste der zu buendelnden Werte (Firmenname, Adresse, Mandant, View-Namen, Schalter) noch nicht final — welche AppSettings/ServiceSettings-Keys genau darunter fallen"
  - "Verhaeltnis zur generischen /Settings- bzw. /ServiceSettings-Oberflaeche: bleiben die Keys dort zusaetzlich editierbar, oder wird diese Maske der EINZIGE Weg fuer Standort-Keys (Waechter-Konsistenz beachten, siehe Teil 7 Einweg-Tor)?"
  - "Rollen/Zugriff: admin-only (analog Settings) oder eigene Rolle?"
epic: false
etappen: []
deploy:
  web: true
  service: false
  migration: false
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
---

## Ziel / Nutzen (das Warum)

Bis zu diesem Teil laufen alle IDEAL-Schalter (View-Namen, Feature-Toggles, der Master
`ProduktionsauftragHierarchisch` und seine Abhaengigen) als normale Eintraege in der generischen
`ServiceSettings`/`AppSettings`-Oberflaeche — das kostet laut Notiz „Null Zusatzaufwand", ist aber
unuebersichtlich, sobald mehrere standortspezifische Werte (Firmenname, Adresse, Mandant,
View-Namen, Schalter) zusammengehoeren. Teil 6 buendelt diese Werte in einer eigenen,
gruppierten Maske — ein reines UX-/Organisations-Feature ohne neue fachliche Logik.

**Hinweis:** Die in der Backlog-Notiz referenzierte Detail-Notiz
`[[2026-08-03-standorteinstellungen-maske]]` existiert derzeit **nicht** in `secondbrain/ideen/`
(siehe offene Rueckfrage 1) — diese Spec ist deshalb bewusst grob gehalten und nur ein Geruest.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope:** eine neue, gruppierte Admin-Maske, die bestehende `ServiceSettings`-/
`AppSettings`-Keys mit Standortbezug (View-Namen aus Teil 1, Toggles aus Teil 2–5, ggf. Firmen-
/Adressdaten) **liest und schreibt** — keine neue Datenhaltung, nur eine kuratierte Oberflaeche
ueber bestehende Katalog-Infrastruktur (ADR 0008/0011).

**Out-of-Scope:** keine neuen fachlichen Werte, die nicht bereits in Teil 1–5 als Setting-Key
entstanden sind; der Master-Schalter `ProduktionsauftragHierarchisch` (Teil 7) darf **nicht** ueber
diese Maske am Waechter vorbei umgelegt werden koennen (siehe Teil 7 „Migrationstor") — diese Spec
verweist auf den zentralen Waechter, implementiert ihn aber nicht selbst.

## Fachliche Anforderungen

- Gruppierte Darstellung mindestens der Kategorien: Firmenname/Adresse/Mandant (falls als
  Settings-Keys existent), View-Namen (Teil 1: `Sync:IdealFaListeViewName`/
  `Sync:IdealFaInfosViewName`), Feature-Toggles (Teil 2–5), Master + abhaengige Schalter
  (Teil 7 — nur Anzeige/Verlinkung, Schreibzugriff geht durch den zentralen Waechter).
- Wiederverwendung der bestehenden typisierten Katalog-Infrastruktur (Bool-Toggle/Int/String,
  ADR 0008) statt einer Parallel-Implementierung.

## Technischer Loesungsentwurf

Neuer Controller/View, der eine kuratierte Teilmenge der `ServiceSettings`-/`AppSettings`-Keys
gruppiert rendert und ueber die bestehenden Set-Mechanismen schreibt (kein neuer Speicherort).
Fuer den Master-Key ruft der Schreibpfad **denselben** Domaenen-Waechter auf, den Teil 7 einfuehrt
(nicht dupliziert implementieren — siehe dortiger Abschnitt „Der Waechter gehoert in die
Service-/Domaenenschicht, nicht in die Maske").

## Migrations-/SQL-Auswirkungen

Keine — keine neuen Tabellen/Spalten, nur eine kuratierte UI ueber bestehende Settings-Tabellen.

## Audit-Feld-Auswirkungen

Keine neuen Entitaeten. Aenderungen an `ServiceSettings`/`AppSettings` ueber diese Maske
protokollieren sich wie bisher (Settings-Tabellen sind laut Fallstrick explizit **keine**
`AuditableEntity`).

## Akzeptanzkriterien

1. Alle in dieser Maske editierbaren Keys bleiben **weiterhin** korrekt in den bestehenden
   Katalog-Tabellen (`ServiceSettings`/`AppSettings`) — kein Parallel-Speicherort.
2. Ein Aenderungsversuch am Master-Schalter ueber diese Maske durchlaeuft denselben Waechter wie
   die generische Oberflaeche (kein Umgehen der Sperre — siehe Teil 7).
3. AKE-Standort (ohne IDEAL-Keys gesetzt) zeigt eine leere oder deaktivierte Sektion, keinen
   Fehler.

## Test-Szenarien

Neues Kapitel „IDEAL Teil 6 — Standorteinstellungen": Aenderung eines View-Namens ueber die neue
Maske spiegelt sich in `/ServiceSettings` wider und umgekehrt; Versuch, den gesperrten Master
umzulegen, wird abgelehnt und protokolliert (sobald Teil 7 den Waechter liefert — Abhaengigkeit
vermerken).

## Deploy

- **Web-App:** ja.
- **Service:** nein.
- **Migration:** nein.
- **Publish-Befehle:** `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  (provisorisch).

## Offene Rueckfragen

1. Referenz-Notiz `[[2026-08-03-standorteinstellungen-maske]]` existiert nicht — Umfang dieser
   Spec ist ein Geruest, keine vollstaendige Spezifikation.
2. Vollstaendige Key-Liste fuer die Buendelung.
3. Bleibt die generische `/Settings`/`/ServiceSettings`-Oberflaeche fuer dieselben Keys zusaetzlich
   offen, oder wird diese Maske exklusiv?
4. Rollen/Zugriff fuer diese Maske.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

1. →
2. →
3. →
4. →
