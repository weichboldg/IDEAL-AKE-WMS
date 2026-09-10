---
type: bug
title: "table-filter.js: querySelectorAll('tbody') und ('thead tr:first-child th') sind nicht auf direkte Kinder skopiert — verschachtelte Tabellen werden mitsortiert"
status: behoben (wartet Manual-UAT)
severity: hoch
created: 2026-09-10
fixed_in: "Worktree-Commit b9bf342 (Sweep ueber die Fehlerklasse) + Quelltext-Waechter IdealAkeWms.Tests/Helpers/TableScriptScopedSelectorTests.cs; Abnahme TS-72. Erreicht main erst mit dem Buendel-Merge."
affected_code:
  - "IdealAkeWms/wwwroot/js/column-preferences.js — setColVisibility, reorderDomColumns (ZWEITER, aktiver Fund: /ProductionOrders und /FaHierarchy)"
  - "IdealAkeWms/wwwroot/js/table-filter.js — sortTable: `_table.querySelectorAll('tbody')`; getPhysicalIndex: `_table.querySelectorAll('thead tr:first-child th')`"
  - "IdealAkeWms/Views/ProductionOrders/Index.cshtml:194-215 (verschachtelte HeadVariants-Tabelle im Kombigeraete-Block, <tbody> bei 201)"
spec: "[[2026-09-10-fa-liste-ausbau-matchcode-spec]]"
---

> [!danger] Schwere am 2026-09-10 von „gering" auf „hoch" korrigiert — und das ist die eigentliche
> Lehre dieses Records
> Die erste Fassung nannte den Fehler kosmetisch („nur eine unerwartete Reihenfolge"), mit der
> Begruendung, `appendChild` halte jede Zeile in ihrem eigenen `tbody`. **Das war falsch.**
> `appendChild` haengt die Zeile an das **iterierte** `tbody` — nicht an ihr aktuelles Elternelement.
> Die inneren Zeilen werden damit aus der Varianten-Tabelle **herausgerissen**.
> Drei Beteiligte (Umsetzer, Reviewer, Koordinator) haben dasselbe falsche Modell akzeptiert; gefunden
> hat es der naechste Umsetzer, weil er beim Bau eines anderen Moduls noch einmal genau hinsah.

## Symptom

In der FA-Liste `/ProductionOrders` im **hierarchischen** Modus zerstoert der **erste** Klick auf
einen sortierbaren Spaltenkopf die Darstellung einer **Kombigeraete**-Gruppe:

Die Zeilen der verschachtelten Varianten-Tabelle werden aus ihr **entfernt** und als missgebildete
Sieben-Zellen-Zeilen in das aeussere, 25-spaltige Gruppen-`tbody` verschoben. Die Varianten-Tabelle
steht danach leer da, und in der Gruppe stehen Zeilen, die niemand dort erwartet.

Betroffen ist damit genau der Fall, den dieses Epic mehrfach als heikel markiert hat — die
Kombigeraete-Anzeige, in der schon einmal ein stiller Fehler steckte (die Varianten-Tabelle blieb beim
Zuklappen stehen, behoben in `be92ade`).

## Ursache

`sortTable` iteriert `_table.querySelectorAll('tbody')`. Der Selektor ist **nicht auf direkte
Kinder beschraenkt** und erfasst deshalb auch das `<tbody>` der verschachtelten
`HeadVariants`-Tabelle, die im Kombigeraete-Block einer Gruppe steckt.

Entscheidend ist, dass `tbody.querySelectorAll('tr')` ebenfalls eine **Descendant**-Abfrage ist:

```js
_table.querySelectorAll('tbody').forEach(function (tbody) {
    var dataRows = Array.from(tbody.querySelectorAll('tr'))          // <- faengt die INNEREN Zeilen mit
        .filter(function (r) { return !r.querySelector('td[colspan]'); });
    dataRows.sort(compareRows);
    dataRows.forEach(function (row) {
        tbody.appendChild(row);                                      // <- VERSCHIEBT sie nach aussen
    });
});
```

Die Varianten-**Zeile** selbst traegt `<td colspan>` und wird vom Filter korrekt aussortiert. Ihre
**Kindzeilen** tragen sieben blanke `<td>` ohne `colspan` — sie passieren den Filter und landen in
`dataRows`. `tbody.appendChild(row)` haengt sie dann an das **iterierte** (aeussere) `tbody`.

> [!warning] Der Denkfehler, den dieser Record festhalten soll
> „`appendChild` haelt jede Zeile in ihrem eigenen `tbody`" — das klingt plausibel und ist falsch.
> `appendChild` haengt an **das Element, auf dem es gerufen wird**. Wird ueber das aeussere `tbody`
> iteriert, wandern alle gefundenen Zeilen dorthin, auch die aus einer verschachtelten Tabelle.
> Eine zweite Fehlannahme derselben Runde lautete, `compareRows` liefere fuer die inneren Zeilen
> ohnehin `0`. Auch das gilt **nur** fuer `colIndex >= 7`: Bei `order-number`=1, `customer`=3,
> `prio`=4, `ab-nummer`=5, `montage-abteilung`=6 existieren in den inneren Zeilen Zellen, deren Text
> verglichen wird. Die Sortierung ist also nicht einmal folgenlos — aber das ist nebensaechlich
> gegenueber dem Verschieben.

**Dieselbe Descendant-Falle in `applyFilters`** (`:290`, `_tbody.querySelectorAll('tr')`): Dort wird
nur `display` gesetzt, also nichts verschoben — aber innere Zeilen werden nach Kriterien der
Aussenspalten ein- und ausgeblendet. Fuer `/ProductionOrders` heute folgenlos, weil die View
`data-server-column-filter="true"` traegt und `applyFilters` dort nicht laeuft; fuer kuenftige
Client-Mode-Listen mit verschachtelten Tabellen aber dieselbe Ursache.

## Zweiter Fund: `column-preferences.js` — aktiv, und vorher unbekannt

Der Sweep (auf ausdrueckliche Vorgabe des Menschen: **die Klasse erheben, nicht nur das Symptom
fixen**) hat eine **zweite, bereits wirksame** Fundstelle derselben Ursache gefunden:

`setColVisibility` und `reorderDomColumns` iterierten `'tbody tr'` — ebenfalls Descendant — und
fassten damit die Zellen der **verschachtelten** Tabelle an.

**Betroffen waren zwei Ansichten:**
- `/ProductionOrders` (Kombigeraete-Varianten-Tabelle, 7 Spalten)
- `/FaHierarchy` (`fa-head-table` je Struktur, 6 Spalten, `supportsReorder: true`)

**Wirkung ohne jeden Sortierklick:** Blendet der Anwender eine Spalte mit physischem Index ≤ 6 aus,
wird eine Zelle der inneren Tabelle **geleert**; beim Umordnen der ersten Spalten werden deren Zellen
**vertauscht**. Also wieder „Wert unter falscher Ueberschrift" — diesmal durch eine voellig normale
Bedienhandlung.

**Entscheidend fuer die Abnahme: getroffen wird die Zelle mit derselben POSITIONSNUMMER, nicht die
gleichnamige Spalte.** `setColVisibility(physicalIdx, …)` arbeitet rein numerisch (`cells[physicalIdx]`);
der Spaltenschluessel wird **vorher** gegen das **aeussere** `thead` aufgeloest. Die innere Zeile
liefert dann ihre **eigenen** sechs bzw. sieben Zellen — derselbe Index, voellig andere Bedeutung:

| Ansicht | ausgeblendete Aussenspalte | getroffene innere Zelle |
|---|---|---|
| `/ProductionOrders` **mit** Aktionsspalte | „Kunde" = Index 3 | 4. Zelle = **FE-Termin** |
| `/ProductionOrders` **ohne** Aktionsspalte (kein Komm./Vorbau-Recht) | „Kunde" = Index 2 | 3. Zelle = **KO-Termin** |
| `/FaHierarchy` | „Bezeichnung" = Index 2 | 3. Zelle = **Status** |

Die erste Berichtsfassung behauptete, es treffe die *gleichnamige* innere Spalte. Das war falsch und
haette den Abnahmeschritt unbrauchbar gemacht — „eine Zelle verschwindet" kann man nicht pruefen,
„die vierte Zelle, FE-Termin, verschwindet" schon. Gefunden hat es der Umsetzer bei der Ueberarbeitung
seines **eigenen** Berichts; im Review an Code und Markup nachgerechnet und in allen drei Zeilen
bestaetigt.

> [!danger] Dieser zweite Fund ist SCHWERER als der erste — er heilt nicht
> Der Sortier-Fehler verschiebt nur DOM: Ein Neuladen stellt die Anzeige wieder her.
> Hier **nicht.** Jede Sichtbarkeitsaenderung wird per `PUT /api/user-view-preferences/{viewKey}`
> **serverseitig gespeichert**; beim naechsten Laden ruft `loadSettings() → applySettings()` dieselbe
> `setColVisibility` erneut auf. Der Schaden wird also **reproduziert**, nicht geheilt — pro Benutzer
> persistent, bis die Praeferenz zurueckgesetzt wird. Im Review am Persistenzpfad verifiziert.

**Warum es niemandem aufgefallen ist** — und das ist die lehrreiche Haelfte: Die fuenf
`defaultHidden`-Spalten der FA-Liste liegen bei Index **13, 14, 15, 16, 17, 25**; der Schaden tritt
nur bei **≤ 6** ein. `/FaHierarchy` hat gar keine `defaultHidden`-Spalte. Der Fehler war also nie
*unsichtbar*, sondern nur *unwahrscheinlich*: Wer „Kunde" ausblendet, haette ihn sofort gesehen.
Ein Einzelfix an `sortTable` haette diese Stelle unberuehrt gelassen — **der Sweep war der Grund,
dass sie gefunden wurde.**

Nachgerechnet und bestaetigt im Review; Abnahme-Schritt ist **TS-72.2**.

Derselbe Selektor-Fallstrick steckt in `getPhysicalIndex`
(`querySelectorAll('thead tr:first-child th')` zieht die sieben inneren `<th>` mit ein). Dort ist er
**derzeit** wirkungslos, weil diese `<th>` kein `data-col-key` tragen — also eine latente, nicht
eine aktive Fehlfunktion.

## Einordnung

**Vorbestehend, nicht durch die Matchcode-/K1-Aufgabe verursacht.** Die `tbody`-Schleife kam mit
`37e8752` (Etappe 6, der Fix gegen „nur die erste Gruppe wird sortiert"), die verschachtelte Tabelle
mit `86cdc26`. Beide stammen aus **demselben Epic** wie dieser Befund, liegen aber vor ihm.

**Schwere: hoch.** Die Varianten-Tabelle verliert sichtbar ihren Inhalt, und in der Gruppe erscheinen
Zeilen mit sieben Zellen in einer 25-spaltigen Tabelle — also genau der Fall „ein Wert steht unter der
falschen Ueberschrift", den dieses Projekt als teuer einstuft. Persistente Daten gehen nicht verloren
(ein Neuladen stellt die Anzeige wieder her), aber der Anwender sieht eine falsche Darstellung und hat
keinen Hinweis darauf, dass ein Klick sie verursacht hat.

**Muss vor Schranke 2 behoben werden.** Der Befund ist vorbestehend im Sinne von „nicht von der
Matchcode-Aufgabe verursacht" — aber beide Ursachen stammen aus **diesem, noch ungemergten** Epic.
Einen bekannten Anzeigefehler im Abnahme-Zweig stehen zu lassen, waere etwas anderes als einen
Altlast-Befund zu parken.

`Views/Tracking/Index.cshtml` hat ebenfalls ein verschachteltes `<tbody>`, ist aber **nicht**
betroffen: Die Tabelle traegt kein `th[data-filterable]`, `init()` bricht bei
`_headers.length === 0` ab. Die dreistufige Arbeitsgang-Reihenfolge kann also nicht verwuerfelt
werden.

## Wie der Fehler gefunden wurde — und warum das der wichtigere Teil ist

**Nicht durch ein Review.** Er kam heraus, weil der Umsetzer eines **anderen** Tasks
(`fa-liste-wiederholung.js`, Wiederholungswerte unterdruecken) bei **seinem eigenen** Modul noch
einmal genau hinsah: Er verwendete `:scope > tr` und `:scope > [data-repeat-key]` statt
Descendant-Selektoren — **defensiv, bevor die Ursache bekannt war** — und stiess beim Begruenden
dieser Entscheidung darauf, dass die Nachbarfunktion in `table-filter.js` genau das nicht tut.

**Vier Stellen haben dieselbe plausible Fehlannahme vorher durchgewunken:**
1. Der Umsetzer des Sortier-Hooks schrieb sie in seinen Bericht („`compareRows` liefert dort 0").
2. Das Task-Review prueffte den Punkt ausdruecklich nach — und bestaetigte die Entlastung mit einer
   eigenen, falschen Begruendung („`appendChild` haelt jede Zeile im eigenen `tbody`").
3. Der Koordinator uebernahm sie und schrieb „Schwere: gering, rein kosmetisch" in diesen Record.
4. Der Mensch las den Bericht und stimmte dem Parken zu.

Jede dieser Stellen hat **gearbeitet**, nicht geschlampt: Es wurde gelesen, gegengeprueft,
argumentiert und eine Schwere begruendet. Genau das macht den Fall lehrreich.

> [!warning] Die Lehre
> **Eine plausible Begruendung fuehlt sich an wie Verifikation, ist aber keine.**
> „`appendChild` haelt jede Zeile in ihrem eigenen `tbody`" klingt wie ein gepruefter Satz. Er war
> nie gemessen — niemand hat die DOM-Semantik nachgeschlagen oder den Klick ausgefuehrt. Die Kette
> brach erst, als jemand aus einem **anderen** Anlass dieselbe Stelle mit frischem Blick ansah.
>
> Dieselbe Runde enthielt zwei verwandte Faelle: zwei Bug-Records (B-1/H-1), die auf unzuverlaessigen
> Screenshots beruhten, und eine Testzuschreibung, die einem Verdrahtungstest Datenverhalten
> zuschrieb. Dreimal dasselbe Muster — **ein Beleg, der als Pruefung gelesen wird, aber keiner war.**
>
> Praktische Folge fuer kuenftige Laeufe: Bei einer Entlastung („harmlos, weil X") ist **X** der
> Pruefgegenstand, nicht die Entlastung. Und wer DOM-Semantik als Begruendung anfuehrt, belegt sie
> am Verhalten oder benennt sie als Annahme.

## Loesungsvorschlag

Die Selektoren auf direkte Kinder skopieren — `:scope > tbody` bzw. das Aequivalent fuer den
`thead`-Selektor. **Beide Stellen gemeinsam**, weil es derselbe systemische Fehler ist.

Betroffen sind **drei** Selektoren, und sie gehoeren gemeinsam angefasst, weil es derselbe
systemische Fehler ist:
1. `sortTable`: `tbody.querySelectorAll('tr')` → `:scope > tr` (**der schaedliche**)
2. `applyFilters`: `_tbody.querySelectorAll('tr')` → `:scope > tr`
3. `getPhysicalIndex`: `querySelectorAll('thead tr:first-child th')` — latent, heute wirkungslos,
   weil die inneren `<th>` kein `data-col-key` tragen; eine innere Tabelle mit `data-col-key` wuerde
   es kippen.

`:scope` ist im Projekt etabliert (`column-preferences.js:213`, `fa-hierarchy-tree.js:41`), und das in
diesem Lauf neu entstandene `fa-liste-wiederholung.js` nutzt es von Anfang an defensiv — der Umsetzer
dort hatte den richtigen Instinkt, bevor der Befund bekannt war.

> [!important] Eigener Task, eigener Regressionsnachweis — aber VOR dem Merge
> `table-filter.js` haengt an rund 30 Listen. Der Befund kam **nicht** in den Commit `0f14850`, weil
> jener Griff seinen Nachweis schon fuer den `table-sorted`-Hook und
> [[2026-08-20-fehlerprotokoll-anzeige-epic-ab]] B-2 verbraucht hatte — die Buendelungsregel verbietet
> das Nachschieben ohne neuen Nachweis, sie verlangt aber keinen Verzicht auf die Behebung.
> Der Nachweis wird **je Tabelle** erhoben, nicht je View-Datei. Zwei Praezisierungen dazu aus dem
> Review: `Views/Tracking/Index.cshtml` ist **nicht** angebunden (kein `th[data-filterable]`,
> `init()` bricht ab), und `init()` bindet ohnehin nur die **erste** `.filterable-table` je Seite —
> auf `/BdeMasterData` sind Tabelle 2 und 3 gar nicht betroffen.

## Test

FA-Liste im hierarchischen Modus, eine Gruppe mit **Kombigeraet** (mehrere Auftragskoepfe,
`IsAmbiguous`): Varianten-Tabelle ansehen, Anzahl und Reihenfolge der Varianten notieren, dann auf
„Kunde" klicken.

**Vor dem Fix (reproduziert den Fehler):** Die Varianten-Tabelle ist anschliessend **leer**, und in
der Gruppe stehen zusaetzliche Zeilen mit sieben Zellen.
**Nach dem Fix:** Die Varianten-Tabelle bleibt vollstaendig und in ihrer Reihenfolge; nur die
Sub-FA-Zeilen der Gruppe sortieren sich.

Gegenproben, je Tabelle zu fuehren:
- Eine Liste mit genau einem `<tbody>` (z. B. `/Articles`, `/StockOverview`, `/Users`): Sortierung
  unveraendert.
- Eine gruppierte Liste **ohne** verschachtelte Tabelle (z. B.
  `/FaHierarchyKommissionierListen`): jede Gruppe sortiert in sich, keine Zeile wandert ueber eine
  Gruppengrenze.
- Flachmodus der FA-Liste (Master `false`): unveraendert.

## Bezug

[[2026-08-12-tabellen-sortierung-nur-erste-gruppe-bug]] (der Etappe-6-Fix, der die Schleife einfuehrte)
· [[2026-08-20-fehlerprotokoll-anzeige-epic-ab]] (B-2, im selben Commit behoben)
· [[2026-09-10-fa-liste-ausbau-matchcode-umsetzung]]
