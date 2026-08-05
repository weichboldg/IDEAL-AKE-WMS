---
type: spec
title: "Bewegungshistorie: Verifikation \"Ausbuchungen standardmaessig nicht sichtbar\" (Bug?)"
slug: 2026-08-05-wms-bugs-improvements-teil-4-spec
status: Entwurf
created: 2026-08-05
updated: 2026-08-05
source_backlog: "[[2026-08-05-WmsBugs&Improvements]]"
depends_on: ""
task: ""
worktree: ""
branch: ""
affected_code:
  - IdealAkeWms/Controllers/StockMovementsController.cs
  - IdealAkeWms/Data/Repositories/StockMovementRepository.cs
  - IdealAkeWms/Views/StockMovements/Index.cshtml
  - docs/TESTSZENARIEN.md
  - secondbrain/tests/testszenarien-index.md
open_questions:
  - "Der Code-Read (Controller, Repository, ViewModel, View) zeigt KEINEN Default-Filter, der Ausbuchungen ohne explizite Filterwahl ausblendet - filterMovementType ist standardmaessig null, die Query liefert dann alle MovementTypes DESC sortiert. Ist das Symptom am aktuellen main-Stand reproduzierbar? Bitte konkrete Repro-Schritte (Artikel/FA/Datum) oder einen Screenshot nachreichen, sonst kann kein Bug-Record eroeffnet werden."
  - "Falls das Symptom real ist, aber nicht codebasiert erklaerbar: koennte es an einer grossen Anzahl neuerer Einbuchungen liegen, die Ausbuchungen bei Page 1 (Default-Sortierung DESC nach Timestamp, PageSize 25/50) aus dem sichtbaren Bereich verdraengen? Das waere kein Bug, sondern ein Pagination-/Wahrnehmungseffekt - trifft das die beobachtete Situation?"
  - "Falls tatsaechlich reproduzierbar: auf welchem Weg wurden die \"nicht sichtbaren\" Ausbuchungen erzeugt (Ausbuchung/Outbound-Formular, Lagerplatz-ausbuchen/OutboundAll, Picking-Transfer, Sage-Ausbuchung)? Das wuerde die Suche auf einen spezifischen, hier nicht geprueften Schreibpfad eingrenzen."
epic: false
etappen: []
deploy:
  web: false
  service: false
  migration: false
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
---

## Ziel / Nutzen (das Warum)

Laut Backlog werden Ausbuchungen in der Bewegungshistorie standardmäßig nicht angezeigt und
erscheinen erst, wenn explizit nach Bewegungsart „Ausbuchung" gefiltert wird. Diese Spec
dokumentiert die durchgeführte Code-Verifikation dieses Verdachts (wie vom Auftrag verlangt,
„Verifiziere den Ist-Zustand am Code") und legt das weitere Vorgehen fest.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Vollständige Nachverfolgung des Lese-Pfads „Bewegungshistorie ohne Filter" vom Controller bis
  zur View, um einen möglichen Default-Filter zu finden, der Ausbuchungen ausblendet.
- Dokumentation des Befunds und Festlegung, ob ein Bug-Record eröffnet wird.

**Out-of-Scope**
- Kein Code-Fix in dieser Spec, solange der Bug nicht reproduzierbar bestätigt ist (per
  Auftrag: „nur ja, wenn verifiziert" einen Bug-Record anlegen — hier ist das Ergebnis der
  Verifikation „am Code nicht nachvollziehbar", siehe unten).
- Keine Änderung an Pagination-Defaults oder Sortierung ohne bestätigten Bug.

## Ist-Zustand (Code-Referenzen) — Ergebnis der Verifikation

Vollständiger Lese-Pfad geprüft, **kein** Default-Filter gefunden, der Ausbuchungen ausblendet:

1. `IdealAkeWms/Controllers/StockMovementsController.cs:40-45` (`Index`-Signatur): 
   `MovementType? filterMovementType` ist optionaler Parameter, **kein** Default-Wert außer
   `null` (kein `= MovementType.Einbuchung` oder ähnliches).
2. `IdealAkeWms/Models/ViewModels/MovementHistoryViewModel.cs:10`: `FilterMovementType` ist
   `MovementType?` ohne Initialisierung — Default `null`.
3. `IdealAkeWms/Data/Repositories/StockMovementRepository.cs:286-287`:
   ```
   if (filterMovementType.HasValue)
       query = query.Where(sm => sm.MovementType == filterMovementType.Value);
   ```
   Ohne `filterMovementType` bleibt dieser `Where`-Zweig komplett aus — die Query liefert **alle**
   `MovementType`-Werte.
4. Keine weitere Einschränkung in `GetMovementHistoryAsync` (Zeilen 250-334), die
   `MovementType.Ausbuchung` gezielt ausschließt. Auch `ApplyMovementColumnFilter`
   (Zeilen 507-537) kennt keinen `movement-type`-Spaltenfilter-Zweig (fällt in `_ => q`,
   No-Op) — ein serverseitiger Spaltenfilter auf „Bewegungsart" existiert im Server-Mode also gar
   nicht als eigener Mechanismus (nur das klassische Dropdown `filterMovementType`).
5. `Views/StockMovements/Index.cshtml`: das Dropdown „Bewegungsart" (Zeilen 36-45) hat als
   erste Option `-- Alle --` mit leerem `value` und **keinem** `selected`-Default auf
   „Ausbuchung" — die Option „Alle" ist beim ersten Aufruf ohne Query-Parameter aktiv.
6. Server-Mode-Spaltenfilter (`table-filter.js`) liest seinen Zustand ausschließlich aus der URL
   (`?colf_*`), nicht aus `sessionStorage`/Cookies — ein „vergessener" Filter aus einer früheren
   Sitzung kann sich also **nicht** unbemerkt in einen neuen, parameterlosen Aufruf der Seite
   übertragen (bestätigt in `secondbrain/codebase/module.md`, Abschnitt „table-filter.js").

**Befund:** Am geprüften Code (Controller, Repository, ViewModel, View, JS-Kontrakt) gibt es
**keinen** Mechanismus, der `MovementType.Ausbuchung` bei einem parameterlosen Aufruf von
`/StockMovements/Index` systematisch ausblendet. Die Standard-Sortierung ist
`OrderByDescending(sm => sm.Timestamp)` (Zeile 308) mit Pagination (`Skip`/`Take`) — bei einer
Werkstatt mit deutlich mehr Ein- als Ausbuchungen könnten neuere Einbuchungen ältere Ausbuchungen
auf spätere Seiten verdrängen, was **subjektiv** wie „Ausbuchungen werden nicht angezeigt" wirken
kann, aber kein Filter-Bug wäre, sondern ein Pagination-/Erwartungseffekt.

**Kein Bug-Record wurde angelegt**, weil der Auftrag ausdrücklich verlangt, den Ist-Zustand am
Code zu verifizieren und nur bei bestätigtem Befund einen Bug-Record zu erstellen. Der
Code-Befund stützt den im Backlog beschriebenen Verdacht nicht. Diese Spec dokumentiert die
Verifikation und stellt offene Rückfragen, um entweder (a) den Verdacht mit konkreten
Reproduktionsschritten zu bestätigen (dann wird in einer Folge-Iteration ein Bug-Record + Fix-Spec
nachgezogen) oder (b) ihn als Wahrnehmungseffekt einzuordnen und zu schließen.

## Fachliche Anforderungen

Keine Umsetzung in dieser Spec — reine Verifikations-/Klärungs-Spec. Bei Bestätigung des Bugs
(siehe offene Rückfragen) wird eine Folge-Spec mit Bug-Record erstellt.

## Technischer Lösungsentwurf

Nicht anwendbar (kein Fix ohne bestätigten Bug). Falls die offenen Rückfragen den Bug bestätigen,
sind die naheliegendsten Ansatzpunkte für eine Folge-Untersuchung:
- Konkreter Schreibpfad, über den die „unsichtbaren" Ausbuchungen erzeugt wurden (siehe offene
  Rückfrage 3) — z. B. falls `MovementType.SageAusbuchung` gemeint war statt `Ausbuchung`
  (unterschiedliche Enum-Werte, unterschiedliches Dropdown-Label „Sage-Ausbuchung").
- Prüfung, ob eine benutzerspezifische `UserViewPreference` (Spalten-Sichtbarkeit) die
  „Bewegungsart"-Spalte selbst ausblendet (würde die Zeile weiterhin in der Tabelle lassen, nur
  die Spalte fehlt — optisch anders, aber ggf. verwechselbar mit „Zeile fehlt").

## Migrations-/SQL-Auswirkungen

Keine (keine Code-Änderung in dieser Spec).

## Audit-Feld-Auswirkungen

Keine.

## Rollen- und Zugriffsfilter-Auswirkungen

Keine.

## Akzeptanzkriterien

1. Diese Spec dokumentiert nachvollziehbar den geprüften Code-Pfad und den Befund „kein
   Default-Filter gefunden" — erfüllt durch den Abschnitt „Ist-Zustand" oben.
2. Bei Vorliegen konkreter Reproduktionsschritte (offene Rückfrage 1) wird in einer Folge-Spec
   entweder ein Bug-Record + Fix erstellt, oder der Verdacht wird im Backlog/Brain als
   „geprüft, kein Bug" geschlossen.

## Test-Szenarien

Kein neues automatisiertes oder manuelles Testszenario in dieser Spec — stattdessen ein
**Diagnose-Schritt** für den Menschen, um die offenen Rückfragen zu beantworten:
1. Bewegungshistorie ohne jeden Filter öffnen (frischer Browser-Tab, keine URL-Parameter).
2. Prüfen: Werden Zeilen mit Bewegungsart „Ausbuchung" angezeigt (ggf. auf Folgeseiten
   blättern)?
3. Falls nein: welche Artikel/FA/Lagerplatz/Datum betrifft es, und über welchen Weg wurde die
   Ausbuchung ursprünglich gebucht?

Bei Bestätigung des Bugs: Nachtrag in `docs/TESTSZENARIEN.md` Kapitel 2 und
`secondbrain/tests/testszenarien-index.md` in einer Folge-Spec.

## Deploy

- **Web-App:** nein (keine Code-Änderung in dieser Spec).
- **Service:** nein.
- **Migration:** nein.
- **Publish-Befehle:** entfällt.

## Offene Rückfragen

1. Der Code-Read (Controller, Repository, ViewModel, View) zeigt **keinen** Default-Filter, der
   Ausbuchungen ohne explizite Filterwahl ausblendet — `filterMovementType` ist standardmäßig
   `null`, die Query liefert dann alle `MovementType`-Werte DESC sortiert. Ist das Symptom am
   aktuellen main-Stand reproduzierbar? Bitte konkrete Repro-Schritte (Artikel/FA/Datum) oder
   einen Screenshot nachreichen, sonst kann kein Bug-Record eröffnet werden.
2. Falls das Symptom real ist, aber nicht codebasiert erklärbar: könnte es an einer großen Anzahl
   neuerer Einbuchungen liegen, die Ausbuchungen bei Seite 1 (Default-Sortierung DESC nach
   Timestamp, PageSize 25/50) aus dem sichtbaren Bereich verdrängen? Das wäre kein Bug, sondern
   ein Pagination-/Wahrnehmungseffekt — trifft das die beobachtete Situation?
3. Falls tatsächlich reproduzierbar: auf welchem Weg wurden die „nicht sichtbaren" Ausbuchungen
   erzeugt (Ausbuchung/`Outbound`-Formular, Lagerplatz-ausbuchen/`OutboundAll`,
   Picking-Transfer, Sage-Ausbuchung)? Das würde die Suche auf einen spezifischen, hier nicht
   geprüften Schreibpfad eingrenzen.

## Freigabe-Antworten (Mensch füllt aus — Schranke 1)

1. →
2. →
3. →
