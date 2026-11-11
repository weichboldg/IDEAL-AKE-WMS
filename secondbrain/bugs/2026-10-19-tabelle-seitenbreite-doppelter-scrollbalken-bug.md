---
type: bug
title: "Tabellen: Seite breiter als das Fenster (Select2 in .table-responsive) und zwei blaue Scrollbalken übereinander"
status: Testbereit
fixed_in: "v1.64.1"
severity: mittel
created: 2026-10-19
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
affected_code:
  - "IdealAkeWms/wwwroot/css/site.css:213-222 (.table-responsive, jetzt position: relative)"
  - "IdealAkeWms/wwwroot/js/site.js:44-73 (initStickyScrollbar: Sentinel, IntersectionObserver, Breiten-Sync)"
related:
  - "[[2026-10-16-stueckliste-lagerplatz-zelle-tabellenbreite-bug]]"
  - "[[2026-10-16-stueckliste-matchcode-lagerplatz-limit-spec]]"
---

# Tabellen: Seite breiter als das Fenster, zwei Scrollbalken

Folge-Befund zu [[2026-10-16-stueckliste-lagerplatz-zelle-tabellenbreite-bug]] (v1.62.0, Spec
[[2026-10-16-stueckliste-matchcode-lagerplatz-limit-spec]]). Direkt im Bündel `feature/2026-08-07-ideal-teile-1-5`, Commit
`4179e38d`, **v1.64.1**. Die Vorgabe lautete v1.62.1. Das Bündel stand aber schon auf v1.64.0, eine 1.62.1 hätte die
Versionsnummer zurückgesetzt.

## Symptom (Browser-Befund Gerald, 2026-10-19, DevTools)

IDEAL-Test, `Picking/Bom/131` (FA 1059463), v1.62.0, Fenster 943 px:

1. **Seite zu breit:** `document.documentElement.scrollWidth` 3641 px, `body.scrollWidth` 943 px. Der rechte Rand der Seite liegt am
   rechten Rand von `select.source-location-select.select2-hidden-accessible`.
2. **Zwei blaue Balken:** Der native Balken der `.table-responsive` (15 px) und darunter `.table-sticky-scrollbar` (14 px) sind
   gleichzeitig sichtbar.

## Ursache

**Fehler 1.** Select2 versteckt das Original-`<select>` mit `position: absolute` (Klasse `select2-hidden-accessible`). Der
Bezugsrahmen eines absolut positionierten Elements ist der nächste positionierte Vorfahre. `td` und `.table-responsive` waren
`position: static` (`site.css:213-218` alt), also war es `<html>`. `overflow-x: auto` beschneidet nur Nachfahren, deren
Bezugsrahmen innerhalb des Scroll-Containers liegt. Deshalb zogen diese Selects das Dokument auf die volle Tabellenbreite (#bomTable
3689 px). Gegenprobe Gerald: `.table-responsive{position:relative}` in DevTools → `scrollWidth` 943 px.
Nicht betroffen: `.col-resize-handle`, weil `column-preferences.js:765-767` das `th` auf `position: relative` setzt, und
`.select2-selection__clear`, dessen Bezugsrahmen der `.select2-container` ist.

**Fehler 2.** `site.js` (alt, Zeilen 44-58) hängte den Sentinel für den IntersectionObserver **in** den Wrapper
(`wrapper.appendChild(sentinel)`), mit `width: 100%` an Position `scrollLeft = 0`. Der IntersectionObserver beschneidet mit dem
Overflow-Clip der Vorfahren. Wird die Tabelle nach rechts gescrollt, wandert der Sentinel horizontal aus dem sichtbaren Bereich,
`isIntersecting` wird `false` und `is-hidden` fällt weg (`site.js:57`). Dann steht der mitlaufende Balken zusätzlich zum nativen
da, gerade am Tabellenende. Bei `scrollLeft = 0` war die Regel korrekt.

**Nebenbefund**, gleiche Funktion: Die Spacer-Breite wurde nur beim Start, bei `window.resize` und nach 500 ms gesetzt
(`site.js:61-69` alt). „Alle aufklappen“ (`Bom.cshtml:990-997`) verbreitert die Tabelle später, also endete der mitlaufende
Balken vor dem Tabellenende. Das „Scrollen bleibt synchron“ aus dem Auftrag hielt deshalb nicht bis rechts außen.

## Fix (v1.64.1, `4179e38d`)

- `site.css:213-222`: `.table-responsive { position: relative; }`, global für alle Listen.
- `site.js:44-53`: Der Sentinel steht jetzt **unter** dem Wrapper (`wrapper.parentNode.insertBefore(sentinel, stickyBar)`). Er scrollt
  nicht mehr horizontal mit.
  **Regel:** Der mitlaufende Balken ist genau dann sichtbar, wenn der Bereich direkt unter der Tabelle, also ihr nativer Balken,
  nicht im Fenster liegt, unabhängig von `scrollLeft`.
  **Warum der mitlaufende Balken weichen muss und nicht der native:** Der native Balken ist immer da und gehört zur Tabelle. Er
  funktioniert auch ohne JS und im Druck. Der mitlaufende Balken ist nur eine Krücke für lange Tabellen.
- `site.js:65-73`: `ResizeObserver` auf Wrapper und Tabelle statt `resize`-Listener und 500-ms-Timeout. Der Spacer folgt jeder
  Breitenänderung (Fenster, Filterzeile, Aufklappen). Synchronisiert wird weiter in beide Richtungen (`site.js:31-42`, unverändert).

## Unverändert (belegt)

| Funktion | Warum nicht betroffen | Fundstelle |
|---|---|---|
| Sticky-Tabellenkopf | Gibt es nicht: kein `position: sticky` an `thead`/`th`. Sticky sind nur `.table-sticky-scrollbar` (Geschwister **nach** dem Wrapper) und `.bulk-action-bar` (über der Tabelle). `overflow-x: auto` macht den Wrapper ohnehin schon zum Scroll-Container, `position: relative` ändert an sticky nichts. | `site.css:978-982`, `site.css:1196-1199` |
| `.table-sticky-scrollbar` | Liegt außerhalb des Wrappers (`insertBefore(stickyBar, wrapper.nextSibling)`) | `site.js:27` |
| Spalten-Resize | Griff positioniert am `th` (relativ gesetzt) | `column-preferences.js:765-770`, `site.css:1129-1130` |
| Zahnrad / Spaltenauswahl | Knopf vor dem Wrapper eingefügt (`tableWrapper.parentElement.insertBefore`) | `column-preferences.js:369-388` |
| Kontextmenü Spalten, Datums-Popup Filter | an `document.body` gehängt, Position aus `pageX` bzw. `getBoundingClientRect` | `column-preferences.js:724`, `table-filter.js:440-446` |
| Select2-Dropdowns | Bom: `dropdownParent: $('body')`. `_Select2ArticlePartial`: `.mb-3` nur in `.input-group`, sonst Standard `body`. InboundBulk: Standard `body`. Kein Dropdown liegt im Scrollbereich. | `Bom.cshtml:737-766`, `_Select2ArticlePartial.cshtml:34`, `InboundBulk.cshtml:146-161` |
| Lagerplatz-Popover | `container: 'body'` (v1.62.0) | `Bom.cshtml` Popover-Init |
| Druckansicht | keine `@media print`-Regel an `.table-responsive`. `position: relative` ohne Versatz verschiebt nichts. | `site.css` |
| Stapel-Reihenfolge | `position: relative` ohne `z-index` bildet keinen neuen Stapelkontext | — |

**Views mit `.table-responsive` + Select2 (gleicher Mechanismus, alle mit dem globalen Fix erledigt):** `Picking/Bom.cshtml`
(Quell-Lagerplatz), `FaCompletion/Edit.cshtml:289/339` (Artikel in der Tabelle), `StockMovements/InboundBulk.cshtml:70`
(Zeilen-Artikel). `StockMovements/Inbound.cshtml` hat Select2 nur außerhalb der Tabelle. Andere absolut positionierte Kinder in
Tabellen hängen schon an einem positionierten Vorfahren: `.col-resize-handle` (th), `.photo-delete` (`.photo-thumbnail`),
`.kpi-card-active::before`.

## Tests

- `ResponsiveCssTests.Wrapper_IsContainingBlockForAbsoluteChildren`: `.table-responsive` enthält `position: relative`.
- `ResponsiveCssTests.Js_StickyScrollbarSentinel_LiegtAusserhalbDesScrollbereichs`: kein `wrapper.appendChild(sentinel)`,
  Sentinel vor `stickyBar` eingefügt, `ResizeObserver` vorhanden. Ein Quelltext-Wächter, denn die Sichtbarkeit selbst
  (IntersectionObserver) braucht einen Browser → **Manual-UAT** TS-95.17–95.20.
- Build 0 Fehler (28 Warnungen, Bestand). Web **2113 bestanden**, 1 übersprungen, darunter `BomZahnradHostTests` und
  `BomLagerplatzZelleTests`. Service **316**.

## Manual-UAT (TS-95.17–95.20, `docs/TESTSZENARIEN.md`)

1. `Bom/131` „Alle aufklappen“: keine Querscrollleiste der Seite. DevTools:
   `document.documentElement.scrollWidth === document.documentElement.clientWidth` → `true`.
2. Tabelle per blauem Balken ganz nach rechts: genau ein blauer Balken, auch am Tabellenende und in der rechten Endstellung. Der
   mitlaufende Balken reicht bis zur letzten Spalte.
3. Quell-Lagerplatz-Dropdown in der letzten sichtbaren Zeile öffnen: nicht abgeschnitten, keine senkrechte Scrollleiste in der
   Tabelle.
4. Gegenprobe: `FaCompletion/Edit` (Stückliste, Artikel-Select2) und `StockMovements/InboundBulk`.

Deploy: nur Web (statische Dateien), keine Migration. Cache unkritisch: `_Layout.cshtml:11/414` lädt `site.css`/`site.js` mit
`asp-append-version="true"`.

## QA (2026-10-20)

Unabhängiges Review durch den qa-agent im eigenen Lauf. Geprüft wurde Commit `4179e38d` (Basis `a9558301`) im Worktree
`.claude/worktrees/2026-08-07-ideal-teile-1-5`, dazu der Code rund um den Diff: `site.js` `initStickyScrollbar`/`syncWidths`,
`site.css` `.table-responsive`/`.table-sticky-scrollbar`, `column-preferences.js`, `table-filter.js`, `csv-export.js`,
`_Select2ArticlePartial` und alle 60 Views mit `.table-responsive`. **Ergebnis: Testbereit**, mit einem kleinen Fix-Commit `d52bc44d`.

### Beweis Build/Test (selbst ausgeführt, nach dem Fix wiederholt)

- `dotnet build IdealAkeWms.slnx`: 0 Fehler, 28 Warnungen (Bestand).
- `dotnet test IdealAkeWms.slnx`: Web **2113 bestanden, 1 übersprungen, 0 Fehler** (2114). Service **316 bestanden, 0 Fehler**.
- Syntaxprüfung `site.js` (`node vm.createScript`) ok.
- EOL im Git-Index: `docs/TESTSZENARIEN.md`, `site.js`, `site.css`, `ResponsiveCssTests.cs` und
  `secondbrain/tests/testszenarien-index.md` stehen alle auf `i/lf`. Die Arbeitskopie hat CRLF durch `core.autocrlf=true`, das ist normal.

### Befunde

| # | Schwere | Befund | Behandlung |
|---|---|---|---|
| Q1 | mittel | `.table-sticky-scrollbar.is-hidden` ist `display: none` (`site.css:1023-1025`). Ein ausgeblendetes Scroll-Element kann dabei seine Scroll-Position verlieren. Erst seit dem korrigierten Sentinel blendet die Bar auch bei `scrollLeft > 0` aus. Nach dem Hochscrollen stünde der Balken dann auf 0, obwohl die Tabelle rechts steht. Der nächste Zug am Balken ließe die Tabelle zurückspringen. Das widerspricht TS-95.18 („auf derselben Position“). | **Behoben** in `d52bc44d`: Der IntersectionObserver-Callback setzt beim Wiedereinblenden `stickyBar.scrollLeft = wrapper.scrollLeft` (`site.js` Z. 57-60). In Browsern, die die Position behalten, ist das ein No-op. Ein Quelltext-Wächter kann den Unterschied nicht erkennen, deshalb gilt Manual-UAT TS-95.18 (Punkt 2 unten). |
| Q2 | niedrig | Margin-Collapse: Einige Wrapper haben einen eigenen Außenabstand, nämlich `BdeBookings/Edit.cshtml:71` `mb-3`, `Picking/Bom.cshtml:545` `mb-3` (Bedarfsmeldungs-Modal), `FaHierarchy/Index:219` und `ProductionOrders/Index:178` `mb-1`. Dort fällt `margin-top: -1px` des Sentinels mit dem `margin-bottom` zusammen, und der Sentinel liegt `mb − 1px` unter dem nativen Balken statt an dessen Unterkante. Folge: In einem Scrollfenster von höchstens 15 px (nur bei `mb-3`) sind kurz beide Balken sichtbar. Bei `mb-1` sind es 3 px, das fällt nicht auf. | Nicht geändert. Eine Korrektur würde den Abstand unter der Tabelle verändern, der jetzt erhalten bleibt (Wrapper-mb + 1px − 1px). Der Effekt ist kosmetisch und nur vorübergehend sichtbar. Bei Bedarf `mb-3` vom Wrapper auf das Elternelement verlegen. Siehe Manual-Test Punkt 6. |
| Q3 | niedrig | Hat das Folgeelement ein `margin-top`, kollabieren die Margins nicht mehr. Der 1px hohe Sentinel trennt sie, der Abstand ist dann `mb + mt` statt `max(mb, mt)`. Bisher entfiel das Zusammenfallen ohnehin, sobald die Bar sichtbar war. | Hingenommen, rein kosmetisch. Kein auffälliger Fall gefunden. |
| Q4 | Info, positiv | Verhaltensänderung: Tabellen in Modals und versteckten Tabs wurden beim Start mit Breite 0 gemessen (`no-overflow`) und danach nie neu vermessen. Jetzt meldet der `ResizeObserver` die Größe beim Einblenden. Dadurch kann dort erstmals ein mitlaufender Balken erscheinen, z. B. im Bedarfsmeldungs-Modal der Stückliste oder in den Tabs von `FaCompletion/Edit`. | Gewollt und korrekt. Wird in Manual-Test Punkt 6 geprüft. |
| Q5 | Info | `ResizeObserver` erzeugt keine Endlosschleife. `syncWidths` ändert nur den Spacer und Klassen der Bar, die nicht beobachtet werden. Am Wrapper setzt es `has-overflow`, dafür gibt es keine CSS-Regel. Wechselt dadurch die Seiten-Scrollleiste, folgt eine zweite Runde, die stabil endet. Höchstens erscheint die harmlose Konsolenmeldung „ResizeObserver loop completed…“, und kein globaler `onerror`-Handler meldet sie weiter (grep: nur `photo-upload.js`). Ohne `firstElementChild` ist der Fall abgesichert (`BdeMasterData/Index:25`: dort steht nur ein Razor-Kommentar). Wird eine Tabelle per AJAX ersetzt, beobachtet niemand mehr die neue Tabelle direkt. Die Höhenänderung des Wrappers löst den Abgleich trotzdem aus. Wrapper, die erst nach `DOMContentLoaded` entstehen, bekommen wie bisher keine Bar. | Kein Handlungsbedarf. |
| Q6 | Info | Die Sichtbarkeitsregel ist korrekt. Der Sentinel ist netto 0 px hoch (1px Höhe, −1px Margin) und liegt an der Unterkante des nativen Balkens, unabhängig von `scrollLeft`. Bei 1px Höhe heißt `threshold: 0.1` praktisch „irgendein Teil sichtbar“. Ein flackernder Layout-Sprung entsteht nicht: Am Seitenende schrumpft das Dokument beim Ausblenden um 13 px, der Sentinel bleibt aber sichtbar (Fensterunterkante = `y_s + 1 + Rest`). Bei mehreren Tabellen bekommt jede eigene Bar, eigenen Sentinel und eigene Observer. Bei Wrappern mit eigener `max-height` funktioniert die Regel jetzt richtig. Bisher lag der Sentinel dort im inneren Scrollbereich. | — |

### Belegtabelle „Unverändert“ gegengeprüft

Alle Zeilen stimmen. Geprüft:

- `site.css:978-1025` (`.table-sticky-scrollbar`: sticky, z-index 10, Geschwister nach dem Wrapper) und `site.css:1196-1199`
  (`.bulk-action-bar`).
- `site.js:27`.
- `column-preferences.js:369-388`: Das Zahnrad hängt am `previousElementSibling` des Wrappers. Der Sentinel steht **danach**, die
  Host-Regel aus `Bom.cshtml:136-143` bleibt erfüllt. Gleiches gilt für `csv-export.js:239`.
- `column-preferences.js:724` und `:764-766`, `table-filter.js:440-446`.
- `Bom.cshtml:737-766` (`dropdownParent: $('body')`), `Bom.cshtml:1119` (Komm.-Ziel-Select2 in der Filterzeile, Standard `body`),
  `Bom.cshtml:1455` (Popover `container: 'body'`).
- `_Select2ArticlePartial.cshtml:34`: `.mb-3` nur bei `.input-group`. `FaCompletion/Edit` hat kein `.input-group`, also `body`.
- `InboundBulk.cshtml:146-161`: kein `dropdownParent`, also `body`.
- `site.css` enthält keine `@media print`-Regel für `.table-responsive`.

Die Zeilenangaben `site.js:44-73` im Frontmatter verschieben sich durch `d52bc44d` um 3 Zeilen.

### Was `position: relative` jetzt abschneiden könnte (Suche in allen Views und JS)

- **Bootstrap-Dropdowns** (`.dropdown-menu`, `data-bs-toggle="dropdown"`): kommen nur in `_Layout.cshtml` (Navigation) vor,
  keine in Tabellen. Sie wären ohnehin am `.dropdown` verankert (relativ).
- **Tooltips** (`PartRequisitions/Index:136/151/230`, `Articles/Info:113`): Bootstrap 5.3.3 hängt sie bei `container: false` an
  `document.body`. Nicht betroffen.
- **Autocomplete/Datalist**: `WarehouseRequisitions/Edit:78-79` liegt außerhalb der Tabelle in einer eigenen Karte. Die native
  `<datalist>` (`FaWorklist/Index:61`, `ServiceSettings/Index:188`) wird vom Browser gezeichnet.
- **`.visually-hidden`** (absolut positioniert) in Tabellen: `Beschichtungsauftraege/Details:244/301`, `Reklamation:22`,
  `PartRequisitions/Index:156`, `FaHierarchy/_FaHierarchyNode:102`, `column-filter-syntax.js:123`. Diese hingen bisher ebenfalls
  an `<html>`, derselbe Mechanismus wie beim Select2-`<select>`. Jetzt sind sie eingefangen, das ist eine Verbesserung.
- In Tabellen gibt es kein `stretched-link`, `btn-check`, `position-absolute` oder `sticky-top`.
- Die absolut positionierten Regeln in `site.css` haben alle einen eigenen Bezugsrahmen: `.footer`, `.photo-delete`,
  `.date-filter-popup` (body), `.column-context-menu` (body), `.col-resize-handle` (th), `.kpi-card-active::before`.

**Ergebnis:** Kein Element, das bisher gewollt über den Wrapper hinausragte, wird jetzt abgeschnitten.

### Beweisart

| Prüfpunkt | Nachweis |
|---|---|
| `.table-responsive` hat `position: relative` | automatisiert, `ResponsiveCssTests.Wrapper_IsContainingBlockForAbsoluteChildren` (Quelltext-Wächter) |
| Sentinel außerhalb des Wrappers, ResizeObserver | automatisiert, `ResponsiveCssTests.Js_StickyScrollbarSentinel_LiegtAusserhalbDesScrollbereichs` (Quelltext-Wächter) |
| Keine Seiten-Querscrollleiste, genau ein Balken, Position nach Wiedereinblenden, Dropdown nicht abgeschnitten | **Manual-UAT** TS-95.17–95.20 (Browser-Layout, IntersectionObserver), nicht grün gezählt |

**Geänderte bestehende Tests:** keine. `ResponsiveCssTests` hat nur zwei neue Tests bekommen.

### Checkliste

- Version v1.64.1 steht in beiden `AppVersion.cs`. Anwender-Changelog und Brain-Changelog
  [[2026-10-20-v1-64-1-tabelle-seitenbreite-ein-scrollbalken]] sind vorhanden. **Bewertung der Versionsnummer:** v1.64.1 ist
  richtig. Das Bündel steht schon auf 1.64.0. Mit 1.62.1 wäre die Versionsnummer zurückgegangen.
- Keine Migration, kein SQL, keine AgentJobs, keine Audit-Felder.
- `docs/TESTSZENARIEN.md` TS-95.17–95.20 und der Index (Zeile 95) sind nachgezogen. `feature-map.md:726` steht auf Testbereit.

### Deploy

- deploy.web: ja. Betroffen sind `site.css`/`site.js` (statisch), die Changelog-View und AppVersion.
- deploy.service: Am Dienst ändert sich nur die Versionsnummer (`IDEALAKEWMSService/AppVersion.cs`), fachlich nichts. Er geht mit dem Bündel
  raus, ein eigener Neustart ist dafür nicht nötig.
- deploy.migration: nein.
- Publish aus dem Worktree: `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`.
- Nach dem Merge nur dann neu aus main publizieren, wenn der Merge getestete Dateien mit parallelen main-Änderungen
  zusammengeführt hat.

### Manual-Test-Checkliste

1. TS-95.17: `Bom/131`, „Alle aufklappen“, Fenster etwa 950 px breit. In der Konsole:
   `document.documentElement.scrollWidth === document.documentElement.clientWidth` → `true`.
2. TS-95.18: Tabelle über den mitlaufenden Balken ganz nach rechts ziehen, dann nach unten scrollen. Es darf nur der native Balken
   bleiben. Wieder hochscrollen: Der mitlaufende Balken steht **rechts** (Prüfpunkt für `d52bc44d`). Ein kleiner Zug daran
   bewegt die Tabelle nur ein Stück, ohne Sprung nach links.
3. TS-95.19: Quell-Lagerplatz-Dropdown in der letzten Zeile und ganz rechts öffnen. Außerdem Lagerplatz-Popover, Datums-Popup,
   Rechtsklick-Spaltenmenü, Spaltenbreite ziehen und Druckansicht prüfen.
4. TS-95.20: `FaCompletion/Edit` (Tab Stückliste, Artikel-Select2), `StockMovements/InboundBulk` und die
   Fertigungsauftrags-Liste prüfen.
5. Zahnrad und CSV-Knopf stehen weiter über der Tabelle (Stückliste, Fertigungsaufträge).
6. Modal und Tab (Q2/Q4): In der Stückliste das Bedarfsmeldungs-Modal mit vielen Positionen öffnen. Der mitlaufende Balken darf
   nur erscheinen, solange der native nicht sichtbar ist. Auf `BdeBookings/Edit` (Wrapper `mb-3`) langsam zum Tabellenende
   scrollen. Auf höchstens etwa 15 px dürfen kurz beide Balken sichtbar sein, das ist Befund Q2 und hingenommen.
