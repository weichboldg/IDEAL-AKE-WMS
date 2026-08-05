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

1. →keine repro
2. →der bug liegt nicht in der anzeige, sondern im spaltenfilter.
![[Pasted image 20260805150518.png]]
![[Pasted image 20260805150605.png]]
gebe ich hier ausbuchung ein, wird auch einbuchung gefunden, aber ich glaube immer
3. →siehe 2

## Kritische Pruefung (2026-08-05)

Anwalt-des-Teufels-Durchsicht. Die Freigabe-Antworten **verlagern das Problem** und **bestaetigen
einen echten, bisher uebersehenen Bug** — die Spec in ihrer jetzigen Form (Verifikation, „kein Bug")
ist dadurch ueberholt. Verifiziert an `Views/StockMovements/Index.cshtml` und
`StockMovementRepository.ApplyMovementColumnFilter`.

### BLOCKER — vor der Freigabe zu klaeren

**B1 — Antwort 2 refutiert die gesamte Spec-Praemisse: es GIBT einen Bug, aber im Spaltenfilter, nicht in der Anzeige. Und er ist groesser als beschrieben.**
Am Code bestaetigt:
- Die Bewegungshistorie ist `data-server-column-filter="true"` und markiert **sieben** Spalten als
  filterbar (`Index.cshtml:73-79`): `datetime`, `article`, `quantity`, `storage-location`,
  **`movement-type`**, `user`, `production-order`.
- `ApplyMovementColumnFilter` (`StockMovementRepository.cs:511-536`) behandelt aber nur **vier**
  Keys: `article`, `storage-location`, `user`, `production-order`. Alles andere faellt in den
  Default-Zweig `_ => q` — ein **stiller No-Op**.
- **Folge:** Der Spaltenfilter „Bewegungsart" (`movement-type`) filtert **gar nicht**. Egal was man
  eintippt („ausbuchung", „xyz"), es kommt **die gesamte, ungefilterte Liste** zurueck (Ein- + Aus-
  + Um- + Sage-Buchungen) — exakt das beobachtete „gebe ich ausbuchung ein, wird auch einbuchung
  gefunden … immer". Ein korrekter Filter (Name-Contains) wuerde „Einbuchung" gerade **nicht**
  liefern; dass er es tut, beweist den No-Op.
- **Zusatzbefund (vom Menschen noch nicht bemerkt):** Auch die Spaltenfilter **`datetime` (Datum/Zeit)**
  und **`quantity` (Menge)** haben keinen Handler → ebenfalls stille No-Ops. Drei der sieben als
  filterbar markierten Spalten filtern nicht.
- Ein Filter, der **stumm die Vollmenge** liefert, statt zu filtern, ist gefaehrlich: der Anwender
  glaubt, auf „Ausbuchung" eingegrenzt zu haben, sieht aber alles — genau die Fehl-Entscheidung, die
  das Backlog vermeiden will.

  **Frage an den Menschen / Auftrag:** Diese Spec muss von „Verifikation, kein Bug, web:false" auf
  einen **echten Bugfix** umgeschrieben werden (Bug-Record anlegen, `deploy.web: true`). Zu
  entscheiden ist **pro betroffener Spalte**: (a) den fehlenden Server-Handler ergaenzen, oder
  (b) die `data-filterable`/`data-col-key`-Markierung entfernen, weil es bereits einen besseren
  dedizierten Filter gibt.
  **Empfehlung:**
  - `movement-type`: **(b) entfernen** — das Dropdown „Bewegungsart" (oben, `filterMovementType`)
    filtert bereits exakt nach Enum-Wert; ein zusaetzlicher Text-Spaltenfilter waere ohnehin
    unscharf („ausbuchung" wuerde per Contains auch „Sage-Ausbuchung" treffen). Ein doppelter,
    kaputter Mechanismus gehoert weg.
  - `datetime`: **(b) entfernen** — die Filterkarte hat bereits `dateFrom`/`dateTo`.
  - `quantity`: entweder (a) einen numerischen Handler oder (b) entfernen — Text-Contains auf Menge
    ist selten sinnvoll.

### SOLLTE — macht den Dev-Lauf sicherer

**S1 — Testbares Fix-Kriterium ergaenzen.** Nach der Entscheidung ein hartes Akzeptanzkriterium:
„In der Bewegungshistorie liefert der Bewegungsart-Filter **ausschliesslich** Zeilen der gewaehlten
Bewegungsart (Eingabe/Wahl ‚Ausbuchung' zeigt **keine** Einbuchungen)" — und, falls (a) gewaehlt wird,
je ein Kriterium fuer `datetime`/`quantity`. Aktuell hat die Spec bewusst **keine** solchen Kriterien
(sie ging von „kein Bug" aus).

**S2 — Klassen-Audit statt Punkt-Fix.** „Spalte als `data-filterable` markiert, aber kein Handler
im Server-`switch`" ist ein **Muster**, kein Einzelfall (hier gleich 3×). Als Teil des Fixes bzw. als
Folge-Aufgabe: die **anderen** Server-Spaltenfilter-Tabellen der App auf denselben stillen No-Op
pruefen (jede `data-col-key`-Spalte muss einen `switch`-Zweig haben, sonst Filter ohne Wirkung).

### HINWEIS — Beobachtung ohne Handlungszwang

**H1 — Antwort 1 („keine repro") schliesst korrekt den urspruenglichen Verdacht** (Ausbuchungen werden
nicht *angezeigt*). Der Code-Befund der Spec dazu war richtig — es ist **kein** Anzeige-/Pagination-
Bug. Der reale Bug liegt eine Ebene daneben (Filter-Handler).

**H2 — Die beiden Screenshots** (`Pasted image …`) konnten hier nicht maschinell gelesen werden; der
Befund ist aber allein aus Antwort-2-Text **und** dem Code eindeutig belegt, sodass sie fuer die
Diagnose nicht noetig sind. Beim Umschreiben zum Bug-Record koennen sie als Repro-Beleg beigelegt werden.

### Empfehlung

**NACHBESSERUNG NOETIG: Der Bug ist real und am Code bestaetigt (Bewegungsart-Spaltenfilter — sowie
`datetime`/`quantity` — sind stille No-Ops, liefern die Vollmenge). Die Spec von „Verifikation/kein
Bug" auf einen Bugfix umstellen (Bug-Record, `deploy.web: true`), pro Spalte Fix vs. Entfernen
entscheiden, und ein Filter-Wirksamkeits-Akzeptanzkriterium ergaenzen.**
