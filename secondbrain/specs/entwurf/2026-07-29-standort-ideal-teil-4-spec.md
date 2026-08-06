---
type: spec
title: "IDEAL-Standort Teil 4 — Beschichtungsauftrag"
slug: 2026-07-29-standort-ideal-teil-4-spec
status: Entwurf
created: 2026-08-06
updated: 2026-08-06
source_backlog: "[[2026-07-29-Standort-IDEAL]]"
depends_on: "[[2026-07-29-standort-ideal-teil-1-spec]]"
task: ""
worktree: ""
branch: ""
affected_code:
  - IdealAkeWms/Controllers/FaHierarchyBeschichtungController.cs (neu, Name provisorisch)
  - IdealAkeWms/Services/BeschichtungsauftragService.cs (neu)
  - IdealAkeWms/Views/FaHierarchyBeschichtung/Index.cshtml (neu)
  - IdealAkeWms/Views/FaHierarchyBeschichtung/Print.cshtml (neu)
  - IdealAkeWms/Models/AppSettingKeys.cs
  - docs/TESTSZENARIEN.md
  - secondbrain/tests/testszenarien-index.md
open_questions:
  - "Layout-/Kopfdaten-Vorlage fuer den Dienstleister-Ausdruck (Corporate Design, Pflichtfelder) noch nicht vorgelegt"
  - "Rollen/Zugriff: bestehende Rolle wiederverwenden oder neue IDEAL-Rolle?"
  - "Verhaeltnis zum bestehenden AKE-Konzept Lackierteil/Beschichtung (HasCoatingParts/IsCoatingDone, CoatingDateCalculator) — bewusst getrennt (IDEAL-eigene Domain) oder soll spaeter zusammengefuehrt werden?"
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

Druckbares Dokument, das die zu beschichtenden Teile beim Transport zum externen
Beschichtungs-Dienstleister begleitet — analog zur bestehenden AKE-Lackierteil-Logik, aber auf
IDEAL-Datenbasis (`FaHierarchyNode`/`FaHierarchyOrderInfo`, Teil 1).

## Umfang (In-Scope / Out-of-Scope)

**In-Scope:** Filter `Beschichtet = -1` (Sage-Boolean, bereits in Teil 1 als `bit` gemappt),
Druckdokument mit Dienstleister-Kopfdaten, Farbausfuehrung (`RAL`), Kontaktdaten
(`Dienstleister`), Liefer-/Retourdatum (`Start_Beschichtung`/`Beschichten_Retour`), Teile-Tabelle
(Masse `Breite`/`Hoehe`/`Tiefe`).

**Out-of-Scope:** keine automatische Zuordnung/Erkennung wie beim bestehenden AKE-
`CoatingDetectionService` (`LackierteilKategorieName`) — IDEAL liefert das Flag bereits fertig aus
Sage; `ProductionOrders`/AKE unveraendert.

## Fachliche Anforderungen

- Filter exakt `Beschichtet = -1` (nicht `= 1` — Sage-VB6-Konvention, bereits in Teil 1 korrekt
  gemappt auf `bit`, hier nur konsumiert).
- Kopf: `ABNr`, `Pos`, `MontageAbteilung`, `HauptFA`, `Start_Beschichtung`, `Dienstleister`,
  `RAL`, `Beschichten_Retour` (alle aus `FaHierarchyOrderInfo`).
- Positionstabelle: `HauptFA`, `HauptArtnr`, `Artnr`, `Matchcode`, `Sollmenge`, `Beschichtet`,
  `Breite`, `Hoehe`, `Tiefe` (aus `FaHierarchyNode`).

## Technischer Loesungsentwurf

`BeschichtungsauftragService` liest ueber die Teil-1-Repositories, filtert/aggregiert, liefert ein
Druck-ViewModel. Layout orientiert sich am bestehenden `PrintService`-Muster.

## Migrations-/SQL-Auswirkungen

Keine — reine Lesefunktion.

## Audit-Feld-Auswirkungen

Keine neuen Entitaeten.

## Akzeptanzkriterien

1. Liste/Druck zeigt ausschliesslich Positionen mit `Beschichtet = -1`.
2. Kopfdaten (Dienstleister, RAL, Termine) stimmen mit `FaHierarchyOrderInfo` ueberein.
3. Kombinationsgeraete werden korrekt nach Montage-Abteilung getrennt dargestellt.
4. AKE-Verhalten (bestehende Lackierteil-/Beschichtungslogik) unveraendert.

## Test-Szenarien

Neues Kapitel „IDEAL Teil 4 — Beschichtungsauftrag": Testfall mit mehreren beschichteten
Positionen unter einer Struktur; Druck-Layout-Abnahme gegen die Vorlage (sobald geliefert);
Negativfall (`Beschichtet = 0`) erscheint nicht in der Liste.

## Deploy

- **Web-App:** ja.
- **Service:** nein.
- **Migration:** nein.
- **Publish-Befehle:** `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  (provisorisch).

## Offene Rueckfragen

1. Layout-/Kopfdaten-Vorlage fuer den Dienstleister-Ausdruck fehlt noch (Notiz referenziert nur
   ein Konzept-Dokument, kein finales Layout).
2. Rollen/Zugriff fuer diese Liste.
3. Bewusste Trennung von der bestehenden AKE-Lackierteil-Logik — spaetere Zusammenfuehrung
   gewuenscht oder dauerhaft getrennte Domains?

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

1. →
2. →
3. →
