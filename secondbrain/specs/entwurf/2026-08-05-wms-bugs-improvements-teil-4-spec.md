---
type: spec
title: "Bewegungshistorie: Spaltenfilter Bewegungsart/Datum/Menge filtert nicht (stiller No-Op) — Bugfix"
slug: 2026-08-05-wms-bugs-improvements-teil-4-spec
status: Testbereit
created: 2026-08-05
updated: 2026-08-06
source_backlog: "[[2026-08-05-WmsBugs&Improvements]]"
depends_on: ""
task: "[[2026-08-05-deploy-wms-bugs-teil-4-8]]"
worktree: ".claude/worktrees/2026-08-05-wms-bugs-improvements-4-8"
branch: "feature/2026-08-05-wms-bugs-improvements-4-8"
affected_code:
  - IdealAkeWms/Views/StockMovements/Index.cshtml
  - IdealAkeWms/Data/Repositories/StockMovementRepository.cs
  - docs/TESTSZENARIEN.md
  - secondbrain/tests/testszenarien-index.md
open_questions: []
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

Der **Spaltenfilter** der Spalte „Bewegungsart" in der Bewegungshistorie (`/StockMovements/Index`)
**filtert nicht**: Gibt man „ausbuchung" ein, werden trotzdem alle Bewegungsarten (auch
Einbuchungen) angezeigt. Der Anwender glaubt, die Liste eingegrenzt zu haben, sieht aber weiterhin
die Vollmenge — eine gefährliche Fehl-Wahrnehmung, die genau die im Backlog gemeldete Verwirrung
auslöst. Der Bug ist **am Code bestätigt** (siehe Abschnitt „Kritische Pruefung" und Bug-Record
[[2026-08-05-bewegungshistorie-spaltenfilter-noop-bug]]). Diese Spec behebt ihn und beseitigt
zugleich die beiden gleichartigen stillen No-Op-Spaltenfilter auf „Datum/Zeit" und „Menge".

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Bugfix: Die drei wirkungslosen Spaltenfilter (`movement-type`, `datetime`, `quantity`) aus der
  Bewegungshistorie-Tabelle **entfernen**, sodass keine leere/wirkungslose Filter-Eingabe mehr
  angeboten wird.
- Testszenarien (`docs/TESTSZENARIEN.md` + `secondbrain/tests/testszenarien-index.md`) nachziehen.

**Out-of-Scope**
- Kein serverseitiger Handler-Ausbau in `ApplyMovementColumnFilter` (bewusst verworfen, siehe
  Lösungsentwurf).
- Klassen-Audit der übrigen Server-Spaltenfilter-Tabellen der App (als Folge-Aufgabe festgehalten,
  siehe unten).
- Keine Änderung an Pagination-Defaults, Sortierung oder dem Bewegungsart-Dropdown der Filterkarte.

## Ist-Zustand (Code-Referenzen) — bestätigter Bug

Der Bug ist am aktuellen main-Stand verifiziert (Details und Anwalt-des-Teufels-Durchsicht im
Abschnitt „Kritische Pruefung" unten); zusammengefasst:

1. `IdealAkeWms/Views/StockMovements/Index.cshtml:70` — die Tabelle ist
   `data-server-column-filter="true"` (Server-Mode-Spaltenfilter, Zustand ausschließlich aus der
   URL).
2. `Index.cshtml:73-79` — **sieben** `<th>` sind als filterbar markiert (`data-filterable`
   `data-col-key`): `datetime`, `article`, `quantity`, `storage-location`, `movement-type`,
   `user`, `production-order`.
3. `IdealAkeWms/Data/Repositories/StockMovementRepository.cs:507-537`
   (`ApplyMovementColumnFilter`) — das `switch (key)` behandelt nur **vier** Keys: `article`,
   `storage-location`, `user`, `production-order`. Alle übrigen Keys fallen in den Default-Zweig
   `_ => q` (Zeile 535) und werden unverändert durchgereicht — ein **stiller No-Op**.
4. **Folge:** Die Spaltenfilter für `movement-type`, `datetime` und `quantity` liefern die
   Vollmenge statt zu filtern. Bei „Bewegungsart" heißt das: Eingabe „ausbuchung" zeigt weiterhin
   Einbuchungen (ein echter Contains-Filter täte das nicht — der Beweis für den No-Op).
5. Für Bewegungsart existiert bereits das **exakte** Dropdown „Bewegungsart" der Filterkarte
   (`Index.cshtml:36-45`, `filterMovementType`, Enum-Wert); für Datum die
   `dateFrom`/`dateTo`-Filterkarte. Der zusätzliche Text-Spaltenfilter ist redundant.

Bug-Record: [[2026-08-05-bewegungshistorie-spaltenfilter-noop-bug]] (`affected_code`,
Reproduktion, Root Cause).

## Fachliche Anforderungen

- In der Bewegungshistorie darf die Spalte „Bewegungsart" **keinen** eigenen Text-Spaltenfilter
  mehr in der Kopfzeile anbieten. Die Filterung nach Bewegungsart erfolgt ausschließlich über das
  bestehende Dropdown „Bewegungsart" der Filterkarte und liefert **exakt** die gewählte Art.
- Analog dürfen die Spalten „Datum/Zeit" und „Menge" **keinen** wirkungslosen Text-Spaltenfilter
  mehr anbieten. Datum wird über die `dateFrom`/`dateTo`-Filterkarte eingegrenzt.
- Die weiterhin funktionierenden Spaltenfilter (`article`, `storage-location`, `user`,
  `production-order`) bleiben unverändert wirksam.

## Technischer Lösungsentwurf

**Gewählt (minimal-riskant): die drei kaputten Spaltenfilter entfernen.** In
`IdealAkeWms/Views/StockMovements/Index.cshtml:73,75,77` an den `<th>` für `datetime`, `quantity`
und `movement-type` die Attribute `data-filterable` und `data-col-key` entfernen (die Spalten
bleiben als reine Anzeige-Spalten erhalten, nur der Filter-Input in der Kopfzeile entfällt). Damit
markiert die Tabelle nur noch die vier Spalten als filterbar, die `ApplyMovementColumnFilter` auch
tatsächlich bedient — kein stiller No-Op mehr. Kein `.cs`-Code muss angefasst werden; der
Default-Zweig `_ => q` bleibt als harmlose Absicherung bestehen.

**Verworfen (Alternative „Handler ergänzen"):** Man könnte in `ApplyMovementColumnFilter` je einen
`switch`-Zweig für `movement-type`, `datetime`, `quantity` nachziehen. Bewusst verworfen, weil (a)
für Bewegungsart und Datum bereits bessere dedizierte Filter existieren (doppelter Mechanismus), (b)
ein Text-Contains auf „Bewegungsart" unscharf wäre („ausbuchung" träfe per Contains auch
„Sage-Ausbuchung"), (c) Text-Contains auf einer Menge kaum sinnvoll ist, und (d) jeder zusätzliche
Handler mehr Code und Testfläche schafft als das Problem rechtfertigt.

**Folge-Aufgabe (Klassen-Audit):** „Spalte als `data-filterable` markiert, aber kein Handler im
Server-`switch`" ist ein **Muster**, kein Einzelfall (hier 3× in einer Tabelle). Als eigene
Folge-Aufgabe sind die **übrigen** Server-Spaltenfilter-Tabellen der App auf denselben stillen
No-Op zu prüfen (jede `data-col-key`-Spalte braucht einen `switch`-Zweig, sonst filtert sie nicht).
Wird bei Umsetzung als Aufgabe in `secondbrain/aufgaben/` festgehalten.

## Migrations-/SQL-Auswirkungen

Keine.

## Audit-Feld-Auswirkungen

Keine.

## Rollen- und Zugriffsfilter-Auswirkungen

Keine.

## Akzeptanzkriterien

1. In der Bewegungshistorie bietet die Spalte „Bewegungsart" **keinen** eigenen (leeren/
   wirkungslosen) Text-Spaltenfilter in der Kopfzeile mehr an. Die Filterung nach Bewegungsart
   erfolgt über das bestehende Dropdown „Bewegungsart" und liefert **ausschließlich** Zeilen der
   gewählten Art — Wahl „Ausbuchung" zeigt **keine** Einbuchungen.
2. In der Bewegungshistorie bieten die Spalten „Datum/Zeit" und „Menge" **keinen** wirkungslosen
   Text-Spaltenfilter mehr an. Die Datumseingrenzung erfolgt über die `dateFrom`/`dateTo`-
   Filterkarte.
3. Die Spaltenfilter für „Artikel", „Lagerplatz", „Benutzer" und „Fertigungsauftrag" filtern
   weiterhin korrekt (Regressionsschutz).

## Test-Szenarien

Manuelles Szenario „Bewegungsart-Filter wirkt exakt" (Nachtrag `docs/TESTSZENARIEN.md` Kapitel 2 +
`secondbrain/tests/testszenarien-index.md`):

**Vorbedingung:** Bewegungshistorie mit gemischten Bewegungsarten (mind. je eine Ein- und eine
Ausbuchung für denselben Artikel).

1. `/StockMovements/Index` öffnen. **Erwartet:** In den Spaltenköpfen „Bewegungsart", „Datum/Zeit"
   und „Menge" gibt es **kein** Filter-Eingabefeld mehr; die Spaltenfilter „Artikel", „Lagerplatz",
   „Benutzer", „Fertigungsauftrag" sind weiterhin vorhanden.
2. In der Filterkarte Dropdown „Bewegungsart" = „Ausbuchung" wählen, filtern. **Erwartet:** Es
   werden **ausschließlich** Ausbuchungs-Zeilen angezeigt; **keine** Einbuchung erscheint.
3. Dropdown „Bewegungsart" = „Einbuchung" wählen, filtern. **Erwartet:** ausschließlich
   Einbuchungen.
4. **Regression:** Spaltenfilter „Artikel" mit einer bekannten Artikelnummer setzen. **Erwartet:**
   nur Zeilen dieses Artikels. Analog „Lagerplatz"/„Benutzer"/„Fertigungsauftrag".

**Negativfall:** Es darf keine Möglichkeit mehr geben, im Spaltenkopf „Bewegungsart" Text
einzugeben, der die Liste unverändert (Vollmenge) zurückliefert.

## Deploy

**Finalisiert durch QA (2026-08-06) — aus dem echten Diff des gemeinsamen Worktrees
`feature/2026-08-05-wms-bugs-improvements-4-8`, nicht der provisorischen Spec-Agent-Schätzung.**

- **Web-App:** ja — einzige geänderte Anwendungsdatei ist `IdealAkeWms/Views/StockMovements/Index.cshtml`
  (drei `<th>` verlieren `data-filterable`/`data-col-key`).
- **Service:** nein — kein Diff unter `IDEALAKEWMSService/`.
- **Migration:** nein — kein neues Schema, keine neue Migration.
- **Publish-Befehl (aus dem Worktree, VOR dem Merge):**

```
dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
```

Fluss: Publish **aus dem Worktree** → Testsystem → manueller Test (unten) → dann Merge. Nach dem
Merge nur dann erneut aus `main` publishen, wenn der Merge tatsächlich getestete Dateien mit
parallelen `main`-Änderungen kombiniert hat (bei diesem reinen View-Fix unwahrscheinlich, aber vor
dem Merge-Schritt gegenprüfen — vier Teil-Specs teilen sich denselben Worktree/Branch).

## QA-Nachweis (2026-08-06)

Verifiziert im Worktree `C:\Git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-05-wms-bugs-improvements-4-8`
(Branch `feature/2026-08-05-wms-bugs-improvements-4-8`, HEAD `a1d4d76`), gemeinsam mit Teil 5/7/8:

- **Build:** `dotnet build IdealAkeWms.slnx` → **0 Fehler** (9 Vorbestehende Warnungen, keine neuen).
- **Tests:** `dotnet test` →
  - `IdealAkeWms.Tests`: **1075 bestanden, 1 übersprungen, 0 fehlgeschlagen** (1076 gesamt).
  - `IDEALAKEWMSService.Tests`: **197 bestanden, 0 fehlgeschlagen**.
- **Code-Review (inline, kein Task-Subagent im QA-Environment verfügbar):** Diff exakt gegen den
  Lösungsentwurf geprüft — `datetime`/`quantity`/`movement-type` verlieren `data-filterable`/
  `data-col-key`, die vier weiterhin funktionierenden Spaltenfilter (`article`, `storage-location`,
  `user`, `production-order`) bleiben unverändert. Kein `.cs`-Eingriff, wie geplant.
- **Migrationen:** keine (bestätigt — 0 neue `.cs`-Migrationsdateien für diesen Teil).
- **`docs/TESTSZENARIEN.md`** (Worktree) ergänzt: TS-2.26 – TS-2.28 (Kapitel 2 „Lager").
- **`secondbrain/tests/testszenarien-index.md`** (Hauptcheckout) nachgezogen (Kapitel 2).

## Manuelle Test-Checkliste (Schranke 2)

Am Testsystem nach Publish durchzuführen — Referenz: `docs/TESTSZENARIEN.md` TS-2.26 – TS-2.28.

1. **TS-2.26:** `/StockMovements/Index` öffnen — in den Spaltenköpfen „Bewegungsart", „Datum/Zeit",
   „Menge" darf **kein** Filter-Eingabefeld mehr erscheinen; „Artikel", „Lagerplatz", „Benutzer",
   „Fertigungsauftrag" bleiben vorhanden.
2. **TS-2.27:** Filterkarte → Dropdown „Bewegungsart" = „Ausbuchung" wählen, filtern → **nur**
   Ausbuchungen, keine Einbuchung. Danach „Einbuchung" wählen → nur Einbuchungen.
3. **TS-2.28 (Regression):** Spaltenfilter „Artikel" mit bekannter Artikelnummer setzen → nur
   passende Zeilen. Analog „Lagerplatz"/„Benutzer"/„Fertigungsauftrag".

## Offene Rückfragen

Keine — der Bug ist am Code bestätigt und die Lösungsentscheidung (Spaltenfilter entfernen) ist
getroffen (siehe „Kritische Pruefung" → Empfehlung und „Finalisierung").

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

## Finalisierung (2026-08-05)

Die Nachbesserung aus „Kritische Pruefung" ist eingearbeitet — die Spec ist von einer
„Verifikations-Spec (kein Bug)" auf einen **echten Bugfix** umgeschrieben:

- **Titel, Ziel/Nutzen, Umfang, Ist-Zustand, Fachliche Anforderungen** auf den bestätigten Bug
  umgestellt (Spaltenfilter `movement-type`/`datetime`/`quantity` = stille No-Ops, liefern die
  Vollmenge).
- **Entscheidung getroffen (keine Rückfrage):** die drei kaputten Spaltenfilter **entfernen** —
  `data-filterable`/`data-col-key` an den `<th>` für `movement-type`, `datetime`, `quantity` in
  `Index.cshtml` entfernen (Empfehlung B aus B1). Alternative „Handler ergänzen" ist als verworfene
  Option im Lösungsentwurf dokumentiert. Minimal-riskant, kein `.cs`-Eingriff nötig.
- **Akzeptanzkriterien** (S1) ergänzt: Bewegungsart-Filter über das Dropdown liefert
  ausschließlich Zeilen der gewählten Art (Wahl „Ausbuchung" zeigt keine Einbuchungen); keine
  wirkungslosen Spaltenfilter auf Datum/Menge mehr; Regressionsschutz für die vier weiterhin
  wirksamen Spaltenfilter.
- **Bug-Record angelegt:** [[2026-08-05-bewegungshistorie-spaltenfilter-noop-bug]] (Symptom, Repro,
  Root Cause, `affected_code`, `severity: mittel`, `status: offen`; wechselseitig mit dieser Spec
  und dem Backlog verlinkt).
- **`deploy.web: true`** gesetzt, Deploy-Abschnitt + Publish-Befehl (Web) ergänzt.
- **Folge-Aufgabe** (S2 / Klassen-Audit „andere Server-Spaltenfilter-Tabellen auf denselben No-Op
  prüfen") im Lösungsentwurf als eigene Aufgabe festgehalten.
- `open_questions` auf `[]` getrimmt; Test-Szenarien vom „Diagnose-Schritt" auf echte
  Fix-Testszenarien umgestellt.

**BEREIT ZUR FREIGABE** (Schranke 1 durch den Menschen — der `status` bleibt bewusst auf `Entwurf`,
der Freigabe-Antworten-Block ist unverändert).

## Korrektur nach Umsetzung (2026-08-06, Nutzer-Feedback)

Der Mensch hat nach der Umsetzung entschieden, die Spaltenfilter **nicht zu entfernen**, sondern
**funktionsfähig** zu machen (Notiz in [[2026-08-05-deploy-wms-bugs-teil-4-8]]: „bitte Filter wieder
einfügen, aber korrekt. die funktionalität muss gegeben sein."; auf Rückfrage: **„Bewegungsart und
Datum"**). Damit ist die ursprüngliche Lösungsentscheidung („die drei Filter entfernen") überholt.

**Neue, umgesetzte Lösung** (auf dem kombinierten Branch
`feature/2026-08-05-wms-bugs-improvements-teil-1-2-3`):
- `ApplyMovementColumnFilter` bekam echte Handler:
  - `movement-type`: matcht den deutschen Anzeigenamen (Contains) → `MovementType`-`IN`.
  - `datetime`: Tag/Monat/Jahr als Zeitraum (OR über Tokens via Expression-Combiner), SQL-seitig,
    negierbar.
  - `quantity`/„Menge": **kein** Text-Filter (bewusst — Nutzer-Wahl „Bewegungsart und Datum").
- `Index.cshtml`: `data-filterable data-col-key` für `datetime` + `movement-type` wieder gesetzt;
  „Menge" bleibt ohne Filter.
- Tests: `StockMovementRepositoryMovementFilterTests` (7). Testszenario TS-2.26 auf das funktionale
  Verhalten umgeschrieben. Bug-Record entsprechend aktualisiert.

Die S2-Folge-Aufgabe ([[2026-08-06-audit-server-spaltenfilter-noop]]) bleibt gültig: das Muster
„`th` als filterbar markiert, aber kein Handler" ist projektweit zu prüfen.
