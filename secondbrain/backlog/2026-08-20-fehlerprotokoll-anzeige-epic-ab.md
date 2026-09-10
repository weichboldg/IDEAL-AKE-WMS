---
typ: bug
---
# Fehlerprotokoll: Anzeige-Epic Etappen A+B, Testlauf 20.08.2026

**Testsystem:** http://idealweb01.ideal.ideal-ake.at:88/ · **Version:** v1.35.0
**Umfang:** lesende Pruefung an `/ProductionOrders` und `/FaHierarchy`, Master an, 130 Sub-FAs in
4 Gruppen. Vollstaendiges Protokoll: [[2026-08-20-testprotokoll-anzeige-epic-ab]].

## B-1 — Sortierklick blockiert die Seite ueber 30 Sekunden [SCHWERE: HOCH]

**Symptom:** Ein Klick auf den Spaltenkopf `FA Nr.` in der gruppierten FA-Liste laesst die Seite
**ueber 30 Sekunden nicht mehr reagieren**. Danach ist sie wieder bedienbar und die Sortierung ist
korrekt angewandt (`FA Nr. ▲`).

**Reproduzierbar:** zweimal in Folge aufgetreten.

**Datenmenge:** 130 Zeilen in 4 Gruppen — also **klein**. Die groesste Gruppe hat 39 Sub-FAs.

**Beobachtungen:**
- Die Sortierung laeuft **client-seitig** — die URL bleibt unveraendert, kein Sort-Parameter.
  (Die Spalten**filter** sind demgegenueber server-seitig: `?colf_order-number=…`.)
- Tritt nur im **gruppierten** Modus auf (mehrere `<tbody>`).
- Keine Konsolen-Fehler.

## Wichtigster Verdacht: Folge des Sortier-Fix aus Etappe 6

**Der Zusammenhang mit [[2026-08-12-tabellen-sortierung-nur-erste-gruppe-bug]] ist zu pruefen — es
koennte dieselbe Codestelle mit umgekehrtem Vorzeichen sein.**

Damals: `table-filter.js` sortierte per `table.querySelector('tbody')` **nur die erste Gruppe**.
Der Fix iteriert seither ueber **alle** `tbody`-Elemente und sortiert innerhalb jedes einzelnen.

**Genau daraus kann der Hang entstehen:** Wird je Zeile erneut die gesamte Tabelle durchsucht,
waechst der Aufwand mit Gruppen x Zeilen. Der Fix hat die Sortierung **korrekt** gemacht —
moeglicherweise um den Preis, sie **langsam** zu machen. Vorher fiel es nicht auf, weil nur die
erste Gruppe angefasst wurde.

**Die Loesung liegt im eigenen Haus:** `Views/Tracking/OseonIndex.cshtml` traegt einen Kommentar,
der exakt diesen Fall beschreibt — 14 Gruppen x 99 Sub-Auftraege = 1386 vollstaendige
Tabellen-Scans, „sekundenlanger Hang", geloest durch **einen** Scan mit vorberechneten Maps
(`subsByGroup`, `opsBySub`). Dasselbe Muster ist hier anwendbar.

## Warum das ernst ist

Am Fertigungsterminal sieht ein halbminuetig eingefrorenes Fenster nach einem **Absturz** aus. Der
Werker klickt erneut (und verschlimmert es), schliesst die Seite oder meldet einen Fehler. Und die
130 Sub-FAs sind der heutige **Testbestand** — produktiv wird es mehr.

## Zuordnung

**Etappe D** des Anzeige-Epics [[2026-08-18-fa-liste-hierarchie-anzeige-spec]] — dort steht der
Sortier-Vorbehalt bereits als Sammelpunkt („Client-Sort im Grouped-Modus ueber alle Views pruefen").
Dieser Befund konkretisiert ihn: Es ist kein Korrektheits-, sondern ein **Laufzeitproblem**.

**Vor der Umsetzung pruefen, ob es alle sechs Views betrifft** — sie teilen sich `table-filter.js`.
Die AKE-Listen (ein `tbody`) sind vermutlich nicht betroffen, das ist aber zu bestaetigen und
gehoert in den Regressionstest.

## H-1 — Nebenbeobachtung: Spalte `Artikelnummer` nicht sortierbar [SCHWERE: GERING]

Der Spaltenkopf `Artikelnummer` traegt kein Sortier-Zeichen und reagiert nicht auf Klick;
sortierbar sind offenbar nur einzelne Spalten (`FA Nr.`, `Komm.`). Ob das so gewollt ist, waere zu
bestaetigen — es faellt auf, weil man es an einer breiten Liste erwartet. **Kein Fehler dieses
Epics**, falls es schon vorher so war.

## Ausdruecklich KEIN Fehler

Die **leeren Spalten** (Kunde, Werkbank, Beschicht., BG-/Fert.-/Liefertermin) sind bekannt und
erklaert: Die Materialisierung schreibt nur sieben Felder. Siehe
[[2026-08-20-materialisierung-fachliche-felder]] — eigener Umfang, nicht Teil dieses Epics.

## Noch nicht geprueft

Braucht einen Menschen am Testsystem: Flachmodus-Regression (Master umschalten), Bulk-Select am
Leitstand ueber mehrere Gruppen, Rueckmelden in Tracking, Ladezeit-Empfinden, Lesbarkeit am
Terminal.
