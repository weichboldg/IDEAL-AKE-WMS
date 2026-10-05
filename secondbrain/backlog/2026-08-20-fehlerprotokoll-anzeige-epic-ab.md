---
typ: bug
---
# Fehlerprotokoll: Anzeige-Epic Etappen A+B, Testlauf 20.08.2026

**Testsystem:** http://idealweb01.ideal.ideal-ake.at:88/ · **Version:** v1.35.0
**Umfang:** lesende Pruefung an `/ProductionOrders` und `/FaHierarchy`, Master an, 130 Sub-FAs in
4 Gruppen. Vollstaendiges Protokoll: [[2026-08-20-testprotokoll-anzeige-epic-ab]].

> [!failure] B-1 und H-1 sind WIDERLEGT (Messung 2026-09-10)
> Beide Befunde stammen aus derselben Agenten-Browsersitzung und aus derselben Quelle: einem
> **unzuverlaessigen Beobachtungskanal** (CDP-Screenshots), nicht aus dem Verhalten der Anwendung.
> Am selben Testsystem gemessen sortiert die gruppierte FA-Liste in **5–7 ms** (130 Zeilen,
> 4 Gruppen, 21 Spalten), und `Artikelnummer` ist sehr wohl sortierbar.
> Details unten im Abschnitt *Messung am Testsystem*. **Keine Arbeit fuer Etappe D** — mit einer
> Ausnahme: dem daraus abgefallenen Befund **B-2** (stiller Abbruch), der unabhaengig gilt.

## B-1 — Sortierklick blockiert die Seite ueber 30 Sekunden [WIDERLEGT, war: SCHWERE HOCH]

> [!note] Verdikt 2026-09-10: Messartefakt, kein Anwendungsfehler.
> Gemessen 5–7 ms statt 30 s. Der Text unten bleibt unveraendert als Beleg, **wie** der Fehlschluss
> entstand — die Hypothese war plausibel und trotzdem falsch.

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

~~**Etappe D** des Anzeige-Epics — Sortier-Performance.~~ **Entfaellt (2026-09-10).** Es gibt kein
Laufzeitproblem; der Sammelpunkt „Client-Sort im Grouped-Modus" in Etappe D bleibt damit ohne
Befund. B-2 geht stattdessen an Punkt 23 der [[2026-09-08-bom-bridge-nachlese]].

**Zur Frage „betrifft es alle sechs Views?" — beantwortet, soweit der Code es hergibt:**
Bei einer Tabelle mit **genau einem** `<tbody>` liefert `querySelectorAll('tbody')` exakt dieses
eine; das Sortierergebnis ist dort beweisbar unveraendert. Das deckt die Mehrheit der AKE-Listen ab.

**Aber:** Eine Zaehlung der `<tbody>`-Vorkommen **je Datei** ist dafuer kein brauchbares Signal.
`BdeMasterData/Index.cshtml` zeigt drei `<tbody>` und ist trotzdem unkritisch — es sind **drei
getrennte Tabellen mit je einem**. Die Regressionsliste muss also **je Tabelle** erhoben werden,
nicht je View-Datei. Gilt fuer jeden kuenftigen Eingriff in `table-filter.js`, auch fuer B-2.

## H-1 — Nebenbeobachtung: Spalte `Artikelnummer` nicht sortierbar [WIDERLEGT]

> [!note] Verdikt 2026-09-10: Fehlbeobachtung.
> Das `th` traegt `data-filterable data-col-key="article-number"`, bekommt Handler und
> Indikator-Span, und der Klick sortiert in 6 ms mit sichtbarem `▲`. Siehe Messung unten.

Der Spaltenkopf `Artikelnummer` traegt kein Sortier-Zeichen und reagiert nicht auf Klick;
sortierbar sind offenbar nur einzelne Spalten (`FA Nr.`, `Komm.`). Ob das so gewollt ist, waere zu
bestaetigen — es faellt auf, weil man es an einer breiten Liste erwartet. **Kein Fehler dieses
Epics**, falls es schon vorher so war.

## Ausdruecklich KEIN Fehler

Die **leeren Spalten** (Kunde, Werkbank, Beschicht., BG-/Fert.-/Liefertermin) sind bekannt und
erklaert: Die Materialisierung schreibt nur sieben Felder. Siehe
[[2026-08-20-materialisierung-fachliche-felder]] — eigener Umfang, nicht Teil dieses Epics.

## Messung am Testsystem (2026-09-10)

Vorgehen: erst den Code gelesen, dann am laufenden System gemessen — `window.triggerSort` und
zusaetzlich der **echte Klick-Pfad** (`MouseEvent` auf das `th`), jeweils mit `performance.now()`
und einem erzwungenen Layout (`offsetHeight`), damit Renderkosten mitzaehlen. Rein lesend.
Datenlage identisch zu Durchgang 1: 130 Zeilen in 4 Gruppen (39/36/34/21), 21 Spalten,
`data-hierarchical="true"`. *(Die Versionsanzeige wurde in diesem Lauf nicht erneut gelesen.)*

| Zeilen | Sortierung | inkl. Layout |
|---|---|---|
| **130** (Istbestand) | **3,7–10,7 ms** | **33–54 ms** |
| 520 | 12,5 ms | 173 ms |
| 2080 | 86 ms | 804 ms |
| 4160 | 148 ms | 1658 ms |

Die groesseren Mengen wurden clientseitig durch Vervielfachen der Zeilen erzeugt (per Reload
verworfen). **Das Wachstum ist im Wesentlichen linear, und die Kosten liegen im Layout, nicht im
Sortieren** — bei 4160 Zeilen entfallen 1,5 s von 1,66 s auf Layout und Rendering eines
21-spaltigen Tabellenkoerpers. Kein Selector-Storm, keine quadratische Stelle. Die Pagination ueber
Gruppen begrenzt diesen Fall ohnehin.

**Warum die Hypothese trotzdem vernuenftig war:** Das OSEON-Muster existiert wirklich, und der
Etappe-6-Fix fasst tatsaechlich seither alle Gruppen an. Nur ist er **linear** gebaut — die Scans
sind `tbody`- bzw. zeilen-skopiert, nichts durchsucht je Zeile die ganze Tabelle. Der
OSEON-Selector-Storm sitzt in einem anderen Codepfad (`OseonIndex.cshtml` hat eine eigene
Baum-Sortierung) und ist dort laengst behoben.

**Gegenindiz, das vorher schon im Code stand:** `ProductionOrders` setzt
`data-fallback-sort-column="picking-date"`. `sortTable` laeuft damit bei **jedem Seitenaufbau** —
und der war in derselben Sitzung unauffaellig. Gleicher Code, gleiche Daten, kein Haenger.

### B-2 — Sortierung bricht still ab, wenn eine Spalte nicht aufloesbar ist [SCHWERE: GERING]

**Unabhaengig von H-1 ein Befund** — auch jetzt, wo H-1 als Fehlbeobachtung erledigt ist.

Belegt am Testsystem: `triggerSort` auf eine Spalte **ohne** `data-filterable` (`coating-part`) und
auf einen gar nicht existierenden Schluessel laeuft in beiden Faellen durch, **ohne Ausnahme, ohne
Konsolenmeldung, ohne Wirkung**. Im Code sind es zwei stille `return`: `table-filter.js:573`
(`th` nicht in `_headers`) und `:317` (`getPhysicalIndex` liefert `-1`).

**Warum das zaehlt:** Eine Spalte mit Klick-Handler und Sortier-Indikator, die auf nichts reagiert
und dazu schweigt, ist **dieselbe Fehlerklasse** wie die `#column-config`-Doppelregistrierung
([[fallstricke]] §10) und wie Punkt 18 der [[2026-09-08-bom-bridge-nachlese]]
(`warehouse-order`-Drift): Konfiguration und Anzeige laufen auseinander, ohne Signal.

**Bezug zum geplanten Sweep:** Punkt 23 derselben Nachlese will die Drift **statisch** absichern
(Key-Mengen je View vergleichen, Bauart `ServiceSettingDefinitions`-Drift-Guard). B-2 ist die
**Laufzeit**-Seite davon: ein `console.warn` macht denselben Bruch sichtbar, wenn er trotz Guard
auftritt. Beide zusammen schliessen die Klasse von zwei Seiten — darum gehoert B-2 dorthin und
nicht in einen eigenen Auftrag.

**Umsetzung bewusst spaeter und gebuendelt:** `table-filter.js` ist gemeinsam genutztes JS. Ein
Eingriff dort braucht denselben Regressionsnachweis wie der Etappe-6-Fix (Ein-`tbody`-Listen
gegenpruefen), deshalb **ein** Griff statt zwei.

### Die eigentliche Lehre — der Beobachtungskanal, nicht die Anwendung

B-1 und H-1 haben dieselbe Wurzel: Die Screenshots der Agentensitzung waren unzuverlaessig. Ein
ausbleibendes Bild sah nach „Seite eingefroren" aus, ein nicht aktualisiertes nach „Spalte reagiert
nicht". Zwei Befunde, eine kaputte Messung.

**Regel daraus:** Ein Befund aus Browser-Fernsteuerung braucht eine **zweite, unabhaengige Ablesung**
— DOM-Zustand oder Zeitmessung — bevor er ein Bug-Record wird. Ein Screenshot ist ein Bild vom
Beobachter, nicht vom Programm. Zeitangaben aus CDP-Timeouts („ueber 30 Sekunden") sind Laufzeiten
**der Automatisierung**; sie gehoeren nie ungepruefte in eine Schwere-Einschaetzung.

## Noch nicht geprueft

Braucht einen Menschen am Testsystem: Flachmodus-Regression (Master umschalten), Bulk-Select am
Leitstand ueber mehrere Gruppen, Rueckmelden in Tracking, Ladezeit-Empfinden, Lesbarkeit am
Terminal.
