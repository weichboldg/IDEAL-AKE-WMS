---
type: referenz
updated: 2026-08-12
---
# Bekannte Fallstricke

Uebertragen aus CLAUDE.md (Nacherfassung 2026-07-27). Jeder Eintrag nennt die Falle **und** das
Warum — die Begruendung ist der eigentliche Wert, weil sie verhindert, dass jemand die Regel
„wegoptimiert". Vollstaendige Vorfassung: `../../docs/CLAUDE-full-backup-2026-07.md`.

Verwandt: [[0004-migrations-und-sql-disziplin]], [[0005-listen-view-pattern-mit-server-side-spaltenfilter]],
[[0009-app-status-in-satelliten-tabellen-neben-sage-master]], [[0010-aktivitaets-protokoll-mit-isolierten-dbcontexts]].

---

## 1. Fremdsystem-Datenfelder (Sage / OSEON / enaio)

### Artikelnummer vs Ressourcenummer
`Artikelnummer` = Geraete-Artikelnummer, `Ressourcenummer` = Bauteil-Artikelnummer. Fuer
Bauteil-Operationen **immer** `Ressourcenummer`.
**Warum:** Beide Felder stehen nebeneinander in der BOM-Zeile und sehen gleich aus. Wer die
Geraete-Nummer verwendet, bucht Bestand auf das Endprodukt statt auf das Bauteil — ein Fehler,
der erst bei der Inventur auffaellt. Genau das war der Bug vom 16.02.2026:
`PickingItem.BomArticleNumber` speicherte `bom.Artikelnummer`, dadurch fand `TransferPicked` den
Artikel nicht und schrieb **keine** `StockMovement` — die Kommissionierung lief scheinbar durch,
ohne zu buchen. (Nach so einem Fix muessen bestehende `PickingItems` ggf. geloescht werden, damit
sie neu initialisiert werden.) Feldsemantik der View: [[integrationen]].

### BOM-`Menge` ist die Menge in der Baugruppe, nicht im Geraet
**Warum:** Wer `Menge` als Gesamtbedarf liest, kommissioniert bei mehrstufigen Stuecklisten zu
wenig oder zu viel. Die Gesamtmenge ergibt sich erst aus der Multiplikation ueber den Baum.

### Baugruppen-Picking bucht NUR die Baugruppe
Ist eine Zeile eine Baugruppe (`IsBaugruppe`), wird bei der Kommissionierung ausschliesslich auf die
Baugruppe gebucht — die darunterliegenden Teile werden **ignoriert**.
**Warum:** Die Baugruppe ist bereits vormontiert und liegt als eigener Bestand vor. Wuerde man
zusaetzlich die Einzelteile buchen, waere der Bestand doppelt abgebucht. Wer die Picking-Logik
anfasst, muss diese Verzweigung erhalten.

### Sage VB6-Booleans sind BIT mit `-1` fuer TRUE
Sage-Tabellen (`KHKArtikel`, `KHKArtikelvarianten`, …) speichern Boolean-Spalten als BIT, aber mit
`-1` fuer TRUE. Filter immer als `IstBestellartikel = -1 AND Aktiv = -1`.
**Warum:** VB6-Legacy von Sage. `= 1` liefert stillschweigend **null** Zeilen — der Sync laeuft
„erfolgreich" und importiert nichts.

### Artikelgruppe BOM vs Articles
SAGE liefert `"940 - Kleinmaterial"`, `Articles` speichert `"940"`. Beim Matching
`.split(' - ')[0].trim()`.
**Warum:** Zwei Quellen mit unterschiedlicher Formatierung derselben fachlichen Angabe. Ohne
Normalisierung matcht keine Artikelgruppen-Regel (Glas-Filter, Empfaenger-Mapping,
Meldebestand-Farbe).

### OSEON `pa.ID` ist bigint
`long` verwenden, nicht `int`.
**Warum:** Die IDs sind bereits jenseits von `int.MaxValue`; ein `int`-Cast wirft zur Laufzeit
oder trunkiert.

### enaio `object1.id` ist int
`Convert.ToInt64(reader.GetValue(0))` verwenden.
**Warum:** Der Reader liefert den nativen Typ; ein direkter `GetInt64` wirft
`InvalidCastException`.

### enaio DMS-Sync hat kein Delta
Die `angelegt`-Spalte in enaio ist statisch (Bulk-Import 2013). Deshalb Full-Sync statt Delta;
MERGE verhindert Duplikate. `EnaioDmsSyncService.cs` liest ALLE Werkstattauftraege/Zeichnungen
ohne Datumsfilter.
**Warum:** Ein Delta auf `angelegt` wuerde nach dem ersten Lauf nie wieder etwas finden, weil das
Datum sich bei Aenderungen nicht bewegt.

### enaio-Quelle ist die View `vw_IDEAL-AKE_Fertigungsauftraege`
FA-Zuordnung kommt aus dieser View mit `WaNummerTrimmed` als FA-Nummer (frueher `feld44` bzw.
`left(feld43,7)`). `DocumentType` kann `Werkstattauftrag`, `Werkstattauftrag+Zeichnung` oder
`Zeichnung` sein. Icon + Sortierung **zentral**: Partial `Views/Shared/_EnaioDmsBadges.cshtml`,
Sortier-Vorrang in `EnaioDmsDocumentRepository.GetByOrderNumbersAsync`.
**Warum:** Vier Views (ProductionOrders, PickingLeitstand, FaWorklist, FaCompletion) zeigen
dieselben Badges. Inline-Kopien driften garantiert auseinander — dann zeigt eine Liste eine andere
Reihenfolge als die andere, und Anwender halten das fuer fehlende Dokumente.

---

## 2. FA-Status, Satelliten-Tabellen, Vorbau

### `IsDone` vs `IsDonePicking` — die Lese-Seite ist die Falle
`Picking/ToggleDone` schreibt `PickingStatus.IsDonePicking`; Sage-`IsDone` darf die App nie
beschreiben. **Alle** Stellen, die „FA erledigt" anzeigen oder filtern, muessen
`IsDone || IsDonePicking` pruefen — FA-Liste, Leitstand, Picking-Worklist, `FaCompletion/Index`,
`FaWorklist/Index`, und service-seitig `BomCacheSyncService.ReadOpenOrdersInWindowAsync` (raw SQL)
sowie `FaWorkStepDetectionService` (**beide** Kandidaten-Queries).
**Warum:** Weil der Sage-Sync `IsDone` ueberschreiben wuerde, muss der App-Status getrennt liegen
(siehe [[0009-app-status-in-satelliten-tabellen-neben-sage-master]]). Von v1.11 bis v1.21.0 hat
**keine** Query das Flag gelesen — der „Abschliessen"-Button war ueber ein Jahr wirkungslos, und
komm-erledigte FAs belegten die BomCache-Plaetze und verdraengten echte offene FAs aus der
Artikelinfo.

### `.Include(o => o.PickingStatus)` ist Voraussetzung
`ProductionOrderRepository.GetAllOrderedAsync()` muss `PickingStatus` per `.Include` laden.
**Warum:** Ohne Include ist `o.PickingStatus` null, das Flag wird nie ausgewertet, und der Filter
verhaelt sich wie „nie erledigt" — ohne Fehlermeldung.

### `IsCancelled` in ALLEN Offen-Queries
`ProductionOrder.IsCancelled` verhaelt sich wie `IsDone`: verwaiste FAs (in Sage geloescht) werden
storniert und muessen aus offenen Sichten verschwinden. **Jede** Offen-Query fuehrt
`!IsCancelled` — EF **und** raw SQL.
**Warum:** Ein storniertes FA, das weiter in Listen und im BomCache-Fenster steht, verbraucht
Kapazitaet und laesst Werker an Auftraegen arbeiten, die es nicht mehr gibt.

### FA-Reconciliation nie im AgentJob
Das Stornieren verwaister FAs laeuft im Service (`ProductionOrderReconciler.Plan`) mit Guard
(leerer Sage-Read → Skip) und Cap (`ReconcileMaxCancelPerRun` → Skip + Fehlermail), hinter Flag
`Sync:ProductionOrderReconcileEnabled` (Default false).
**Warum:** Ein guard-loses `WHEN NOT MATCHED BY SOURCE` im AgentJob wuerde bei einem leeren oder
teilweisen View-Read **alle** offenen FAs stornieren. Der Guard und der Cap sind genau dieser
Schutz — deshalb liegt die Entscheidung in unit-getestetem C#, nicht in SQL.

### `FaWorkSteps`: `IsRemoved` statt Delete, Detection ist nur-hinzufuegend
Genau eine Zeile je FA+WorkStep (UNIQUE). Manuelle Abwahl setzt `IsRemoved=1`; der Detection-Sync
darf solche Zeilen **nie** re-adden. Manuelles Wieder-Hinzufuegen reaktiviert
(`IsRemoved=0`, `Source='Manual'`).
**Warum:** Manuelle Abwahl ist eine fachliche Entscheidung eines Menschen. Wuerde die Erkennung
sie ueberschreiben, kaeme der abgewaehlte Arbeitsgang bei jedem Sync-Lauf zurueck — der Werker
wuerde gegen die Maschine ankaempfen.

### Zwei Completion-Flags auf `FaWorkStep`: `IsSpecComplete` vs `Status`
`IsSpecComplete` (FA-Vervollstaendigung, „Vollstaendig definiert", Planer) blendet **nichts** aus.
`Status` (`FaWorkStepStatus` Offen/InBearbeitung/Fertig, Abarbeitungsliste, Werker) ist das
**einzige** Flag mit Ausblende-Logik — und nur `Fertig` blendet aus, `InBearbeitung` bleibt
sichtbar. Schreibpfade: `ToggleSpecComplete` → `SetIsSpecCompleteAsync` vs
`/api/fa-work-steps/set-status` → `SetStatusAsync`.
**Warum:** Zwei verschiedene Personen mit zwei verschiedenen Fragen („ist geplant?" vs „ist
gebaut?"). Wuerde das Planer-Flag ausblenden, verschwaende der FA aus der Werker-Liste, bevor
jemand ihn gebaut hat.

### FaWorkStep-Detection haengt nicht am BomCache-ContentHash-Pfad
`FaWorkStepDetectionService` laeuft als **eigener idempotenter Schritt** direkt nach dem
BomCache-Sync (`Sync:FaWorkStepDetectionEnabled`, eigener Protokoll-Eintrag).
**Warum:** Der BomCache-Sync skippt Artikel mit unveraendertem `ContentHash`. Haenge die Erkennung
dort ein, bekaemen neue FAs zu bereits gecachten Artikeln **nie** eine Erkennung.

### „Ohne Treffer" / „nicht im Cache" heisst nicht „Fehler"
Kann am Cache-Fenster oder am Cap liegen (Info bei Detection, **Warning** bei BomCache-Cap).
**Warum:** Wer das als Fehler liest, sucht im Code statt in der Konfiguration.

### Leitstand-Spalten VK–VA sind statisch
Der WorkStep-Katalog ist erweiterbar, der Leitstand zeigt bewusst nur die 5 Spalten
VK/VL/VE/VT/VA (Filter-Keys cooling/fan/electric/doors/superstructure). Neue Katalog-AGs
erscheinen dort **nicht** automatisch.
**Warum:** YAGNI-Entscheid der Spec. Eine sechste Spalte braucht View + ViewModel + Pivot — das
ist bewusst sichtbarer Aufwand, statt eine generische Pivot-Mechanik zu bauen, die niemand
gebraucht hat.

### Leitstand VK–VA zeigen den Erledigt-Status, nicht „anwendbar"
Datenquelle ist `GetWorkStepDetailPivotAsync` (orderId → Code → `FaWorkStepPivotCell`), gesetzt
wird ueber `/api/fa-work-steps/set-status`. Fehlender Code = AG nicht anwendbar = **leere Zelle**.
„Anwendbar" wird im Leitstand nicht mehr gesetzt.
**Warum:** Leitstand und Abarbeitungsliste zeigen dasselbe Flag — sonst haetten Leitstand und
Werkstatt zwei widersprechende Wahrheiten ueber denselben Arbeitsgang.

### FA-Abarbeitungsliste ist arbeitsgang-zentriert, nicht werkbank-zentriert
`FaWorklistController.Index` filtert auf **einen** WorkStep (`?workStepId=`) ueber alle
Werkbaenke; die Werkbank ist ein optionaler Zusatzfilter (`?workbenches=`, Komma-OR-Semantik via
`WorkbenchFilter.Matches`). Vorauswahl aus `User.DefaultWorkStepId` bzw. `User.DefaultWorkbenches`.
**Warum:** Ein Vorbau-Arbeitsgang wird von einem Team gemacht, nicht von einer Werkbank. Das
frueher gepflegte Werkbank-AG-Mapping bleibt Stammdatum, treibt die Liste aber nicht mehr.

### Text-Merkmale gibt es nur bei FaAttributes, nie bei ArticleAttributes
Der Enum `AttributeType` ist zwischen Article- und FA-Merkmalen **geteilt** und hat `Text = 2`,
aber der Freitextwert lebt ausschliesslich in `FaAttributeValue.TextValue`.
`ArticleAttributeValue` hat **kein** TextValue.
**Warum:** Der geteilte Enum verleitet dazu, `Text` auch in der ArticleAttributes-UI anzubieten —
der Wert haette dann nirgends einen Speicherort und ginge still verloren.

### UI-Label „FA-Vorbau-AG", Code bleibt `WorkStep`
Nur Anzeige-Labels wurden umbenannt; Controller, Routen (`/WorkSteps/...`) und Entity bleiben.
BDE- und OSEON-Arbeitsgaenge sind ein **anderer** Kontext und behalten „Arbeitsgang".
**Warum:** Drei verschiedene Dinge hiessen „Arbeitsgang". Das Label trennt sie fuer Anwender,
ohne ein Rename-Grossprojekt durch Code, DB und Routen zu ziehen.

### FA-Zusatzinfos: Auto-Erledigt ist einweg und gecappt
Sage-Status **verpackt/abgeholt** setzt `PickingStatus.IsDonePicking` (nie Sage-`IsDone`).
Statusquelle ist der **frische** `row.Status`, nicht `info.SageStatus`. Status-Rueckfall oeffnet
nicht wieder; manuelles Oeffnen haelt nur bis zum naechsten Lauf. Ueber
`Sync:FaZusatzinfoAutoDoneMaxPerRun` → kein Write + Warn + Fehlermail.
**Warum:** Ein automatischer Zustandswechsel auf Basis eines Fremdsystem-Feldes ist gefaehrlich —
ein View-Defekt koennte hunderte FAs schliessen. Der Cap ist die Reissleine, der Einweg-Charakter
verhindert Ping-Pong zwischen Mensch und Sync.

### Beschichtungstermin: Backward-Compat bei leerer Kategorie
Ist `LackierteilKategorieName` leer, gilt der Beschichtungstermin fuer **alle** Auftraege; ist er
gesetzt, nur fuer FAs mit `HasCoatingParts`. Formel zentral in `CoatingDateCalculator.Compute(...)`.
**Warum:** Vor Einfuehrung der Kategorie galt der Termin generell. Ein Default „nur mit Flag"
haette bestehende Installationen still um alle Beschichtungstermine gebracht.

---

## 3. Views, Forms, JavaScript

### Hidden + Checkbox mit gleichem `name` funktioniert nicht
Loesung: der Hidden-Input traegt den `name`, die Checkbox hat **keinen** `name` und synct per
`onchange`-Handler den Hidden-Wert.
**Warum:** Der Model-Binder nimmt bei zwei Feldern gleichen Namens den ersten Wert — die
Checkbox-Stellung geht verloren. Betrifft u. a. die `/ServiceSettings`-Bool-Toggles.

### `data-col-key` ist Pflicht auf allen `<th>` in filterable-tables
Bei neuen Spalten: Key in `ColumnDefinitions.cs` definieren **und** in der View verwenden.
**Warum:** Filter- und Spalten-Preferences-Logik adressiert Spalten ueber diesen Key. Fehlt er,
ist die Spalte nicht filterbar und nicht konfigurierbar — ohne Fehlermeldung.

### `column-preferences.js` MUSS vor `table-filter.js` eingebunden werden
**Warum:** `column-preferences.js` dispatcht das `column-preferences-ready`-Event, auf das
`table-filter.js` wartet. Umgekehrte Reihenfolge = Event vor dem Listener = stille Fehlfunktion.

### Neuer `viewKey` ohne `ColumnDefinitions.GetByViewKey`-Registrierung → Prefs-API 400
Bringt eine Liste eine Spaltenauswahl (`#view-config`/`#column-config` + `column-preferences.js`),
muss ihr `viewKey` in `ColumnDefinitions.GetByViewKey` eingetragen sein. Fehlt der Eintrag,
antwortet `UserViewPreferencesApiController` mit **400** und speichert **still nichts** — die
Spaltenwahl geht bei jedem Reload verloren, ohne Fehlermeldung fuer den Anwender.
**Warum:** Der API-Controller mappt den `viewKey` ueber `GetByViewKey` auf den gueltigen
Spaltenkatalog; ein unbekannter Key ist fuer ihn ununterscheidbar von einer manipulierten Anfrage
und wird abgewiesen. Genau das ist schon einmal bei `FaWorklist` passiert und war 2026-08-12
(Etappe 6 des IDEAL-Buendels) an allen vier IDEAL-Listen zunaechst offen. Vierter
Pflichtbestandteil des Listen-View-Patterns — siehe
[[0005-listen-view-pattern-mit-server-side-spaltenfilter]], Abschnitt „Spaltenpraeferenzen".

### `table-filter.js` sortierte bis 2026-08-12 nur das erste `<tbody>` einer Tabelle
`sortTable()` fasste bis Commit `37e8752` (v1.31.0) nur das **erste** `<tbody>` an. Bei gruppierten
Tabellen mit mehreren `<tbody>` (z. B. die HauptFA-Gruppen der IDEAL-Kommissionierlisten) sortierte
also sichtbar nur die erste Gruppe, der Rest blieb unsortiert — ein offensichtlich falsches
Ergebnis. Behoben: `sortTable()` sortiert jetzt **je `<tbody>` separat** (bei Ein-`<tbody>`-Tabellen
verhaltensgleich, daher AKE-Listen-regressionssicher).
**Warum hier festgehalten:** Damit dieselbe Fehlerklasse bei kuenftigen gruppierten Tabellen sofort
erkannt wird — wer Client-Sortierung auf eine Tabelle mit mehreren `<tbody>` setzt, muss pruefen,
dass wirklich alle Gruppen sortiert werden, nicht nur die erste.

### Jede DOM-Iteration im Tree-Table braucht `:scope >` — sonst greift sie in Kindtabellen
`querySelectorAll` ist **immer** eine Descendant-Abfrage. `tbody.querySelectorAll('tr')` findet
deshalb auch die Zeilen einer Tabelle, die **innerhalb** einer Zelle dieses `tbody` steckt. Die
gruppierten IDEAL-Listen haben genau das: die Kombigeraete-Varianten-Tabelle in
`Views/ProductionOrders/Index.cshtml` und die `fa-head-table` je Struktur in
`Views/FaHierarchy/Index.cshtml`.
**Regel:** In allen Skripten, die ueber `tbody`/`tr`/`td`/`th` einer filterbaren oder gruppierten
Tabelle laufen, wird die Iteration mit `:scope > …` auf **direkte Kinder** skopiert. Ausnahme nur,
wo das Ziel legitim tiefer sitzt (z. B. ein Filter-Input in einem Flex-Wrapper) — dann die Ausnahme
**im Code begruenden**.
**Warum:** Zwei Fehlerbilder, beide ohne Fehlermeldung und beide am 2026-09-10 real vorgefunden:
`sortTable` verschob per `appendChild` die Zeilen der Varianten-Tabelle in das aeussere Gruppen-`tbody`
(innere Tabelle wird **leer**, Fremdzeilen mit 7 Zellen in einer 26-spaltigen Tabelle), und
`column-preferences.js` leerte beim Ausblenden einer Spalte mit Index ≤ 6 eine Zelle der inneren
Tabelle bzw. vertauschte sie beim Umordnen. Beides ist „Wert unter falscher Ueberschrift" — die
teuerste Klasse, weil sie plausibel aussieht.
Der `td[colspan]`-Filter schuetzt **nicht**: Er sortiert die Traegerzeile aus, nicht deren Kindzeilen.
Abgesichert durch den Quelltext-Waechter `IdealAkeWms.Tests/Helpers/TableScriptScopedSelectorTests.cs`
— der prueft nur die Schreibweise, nicht das Verhalten; die Wirkung bleibt Manual-UAT (TS-72).

> [!warning] Die Fehlannahme, an der das lange haengen blieb
> **`appendChild` haengt an das Element, auf dem es gerufen wird — nicht an das `tbody`, in dem die
> Zeile stand.** Der Satz „`appendChild` haelt jede Zeile in ihrem eigenen `tbody`" klingt wie ein
> gepruefter Satz und ist falsch. Er hat vier Pruefstellen passiert (Umsetzer-Bericht, Task-Review,
> Koordinator-Record, Mensch-Freigabe), bevor jemand bei anderer Gelegenheit genauer hinsah.
> Ebenso falsch war die Entlastung „`compareRows` liefert fuer die inneren Zeilen ohnehin `0`" — das
> gilt nur, wenn der Spaltenindex **ueber** der Spaltenzahl der inneren Tabelle liegt.

### CSS: Spezifitaet gilt nur zwischen Regeln am **gleichen** Element — sonst entscheidet Vererbung
Eine eigene Klasse auf einer Tabellenzelle (`.meine-klasse { color: … }`, Spezifitaet 0,1,0) verliert
gegen Bootstraps `.table > :not(caption) > * > *` (0,1,1) — **unabhaengig von der Ladereihenfolge**,
weil Bootstrap spezifischer ist, nicht nur frueher. Das Projekt loest das durchgehend mit
**Kindketten**: `.fa-liste-group-head > td.fa-liste-group-header`,
`.fa-liste-group > tr > td.fa-liste-repeat-hidden`, `.fa-tree-table > tbody > tr > td`.
**Warum:** Wer eine Zellfarbe per einfacher Klasse setzt, bekommt eine Regel, die korrekt gesetzt
wird und **nichts tut**. Build und Testsuite sehen das nicht — eine wirkungslose CSS-Regel ist
gruen. **Eine CSS-Aenderung ohne Sichtpruefung am Bildschirm ist bauartbedingt unverifiziert.**
Nicht zu verwechseln: `table-striped`, `table-danger` und `table-secondary` sitzen am `<tr>`. Ihre
`color` erreicht die Zelle nur per **Vererbung**, und jede direkte Deklaration am `<td>` schlaegt
Vererbung — dort ist also **keine** Kindkette noetig, und wer die Spezifitaeten vergleicht,
vergleicht das Falsche.

### Eine filterbare Tabelle pro gerenderter Seite
`table-filter.js` / `column-preferences.js` sind Single-Table.
**Warum:** Beide Skripte greifen auf „die" Tabelle zu. Zwei filterbare Tabellen auf einer Seite
konkurrieren um denselben Zustand. Deshalb rendert z. B. BdeMasterData pro Tab-Request nur eine.

### `defaultHidden`: gespeicherte Prefs muessen je Spalte gewinnen
`ColumnDef.DefaultHidden` + `defaultHidden` im inline `#column-config`-JSON;
`column-preferences.js` faellt in `buildDefaultSettings()` **und** `mergeWithDefaults()` auf
`visible: !c.defaultHidden` zurueck.
**Warum:** Ein hart auf `true` gesetzter Default im Merge waere der Bug: neue Spalten wuerden bei
jedem User mit gespeicherten Prefs sichtbar aufpoppen.

### Bootstrap-Table-Styles ueberschreiben Custom-CSS
`!important` noetig.
**Warum:** Bootstrap-Selektoren sind spezifischer als die Projekt-Klassen. Betrifft u. a. die
CI-Farbe der Kommissionierungs-Spalte in der FA-Liste.

### BOM-Baum hat Vorrang vor dem Spaltenfilter
In `Bom.cshtml` kombiniert `updateBomVisibility()` Baum-Zustand **und** Filter: eine Zeile ist nur
sichtbar, wenn die Parent-Baugruppe aufgeklappt ist **und** der Filter passt. Auch
`window.setColumnFilter` ist dort ueberschrieben, damit ein Default-Filter aus dem Benutzerprofil
den Baum-Zustand respektiert.
**Warum:** Ohne die Kombination zeigte Expand/Collapse auch nicht-passende Kinder an — der Filter
wirkte scheinbar zufaellig. Wer im BOM Sichtbarkeit anfasst, muss durch diese Funktion gehen und
nicht direkt `style.display` setzen.

### Razor: `v@Namespace.Class` wird als E-Mail geparst
`v@(Namespace.Class)` verwenden.
**Warum:** Razor erkennt `x@y.z` als E-Mail-Adresse und rendert es literal statt als Ausdruck.

### Select2-Textformat
Die API liefert `"ArticleNumber - Description"`; Parsing mit `.split(' - ')[0]` — **kein**
Em-Dash.
**Warum:** Der optisch aehnliche Em-Dash matcht nicht; das Parsing liefert dann den ganzen String
als Artikelnummer.

### QR-Code Komma-Suffix
FA-Nummer kann ein Komma-Suffix tragen → `.split(',')[0]`.
**Warum:** Der ungetrimmte Wert findet keinen FA.

### Scanner-Endlosschleife durch `confirm()`
Nach einem Scan-Fehler kein `confirm()` — Bootstrap-Modal verwenden.
**Warum:** Der Scanner feuert weiter, waehrend der modale Browser-Dialog blockiert; jeder neue
Scan erzeugt einen neuen Dialog.

### iOS Safari + `getUserMedia` nur im synchronen Gesture-Stack
`navigator.mediaDevices.getUserMedia()` direkt im Click-Handler aufrufen, **vor** Modal-Show/await.
Pattern: erst `await requestCameraPermission()` (Pre-Warm im Click), dann Modal + Scanner-Init.
**Warum:** iOS Safari erteilt die Kamera-Permission nur innerhalb der User-Gesture. Nach einem
`await` gilt der Stack als verlassen und die Anfrage wird verweigert.

### Event-Delegation fuer AJAX-nachgeladenen Inhalt
Im OseonIndex-Inline-JS binden Sub-Row-Handler ueber
`document.addEventListener('click', …)` + `e.target.closest('.oseon-tree-sub')`, nicht per
`forEach(row.addEventListener(...))`.
**Warum:** Direkt gebundene Handler erreichen nur die beim Init vorhandenen Zeilen; spaeter per
AJAX eingefuegte Rows bleiben tot.

### Radio-3-State (Doppelklick → None) ist selbstgebaut
In `Details.cshtml` via `mousedown`-Snapshot des `checked`-Status + `click`-Handler, der den Radio
wieder unchecked setzt.
**Warum:** Bootstrap-/HTML-Radios koennen per Design nicht zurueck auf „keine Auswahl".

### Model-Binder: `int[]` **skippt** leere Strings
Leere `name="quantitiesPicked"`-Inputs kommen **nicht** als 0 an — sie verschwinden und
verschieben die Array-Indices. Loesung in `WarehousePicking/Details.cshtml`:
`normalizeEmptyQuantitiesToZero()` vor jedem Submit, `collectProgress()` sendet `"0"`.
**Warum:** Ein paralleler Mapping-Loop (`qtyDict[itemIds[idx]] = quantitiesPicked[idx]`) schreibt
dann Mengen auf die **falschen** Positionen — Datenfehler ohne jede Fehlermeldung.

### Decimal/Culture-Bug in Form-Inputs
`decimal` mit `ToString(InvariantCulture)` aus einer `DECIMAL(18,4)`-Spalte ergibt `"4.0000"`; der
Default-Binder parst mit deutscher Request-Culture → **40000**. Loesung in Lagerbestellungen:
`type="number" step="1"` + `int[]`-Binding.
**Warum:** Punkt ist im Deutschen Tausendertrenner. Bei Mengen-Eingaben also integer verwenden
oder explizit `InvariantCulture` parsen.

### Programmatisches `input.value = …` loest kein `input`-Event aus
Kalender-Klicks, „Filter entfernen" und `window.setColumnFilter` rufen **seit v1.24.0**
`applyColumnFilterNow()` — **kein** synthetisches `input`-Event mehr faken.
**Warum:** Seit v1.24.0 navigiert der Server-Mode erst bei ENTER; ein synthetisches input-Event
wuerde dort gar nichts mehr ausloesen. Der direkte Aufruf wirkt in beiden Modi.

### Server-Filter-Mode: kein clientseitiges `applyFilters()` beim Init
**Warum:** Der Server hat bereits gefiltert und paginiert. Ein zusaetzlicher Client-Filter laeuft
auf DOM-Zellentext und versteckt insbesondere Datumsspalten („dd.MM.yyyy KWxx"), die er nicht
versteht — die Liste erscheint leer, obwohl Daten da sind.

### Android-Softkeyboard: `enterkeyhint="search"` auf Server-Filter-Inputs
Nicht entfernen.
**Warum:** Ohne das Attribut zeigt Android bei mehreren Feldern eine „Weiter"-Taste, die nur ins
naechste Feld springt statt ein `keydown`-Enter zu feuern — Filtern ist auf Android unmoeglich.

### Universal-Filter-Pattern: Getter liefert **gerenderten** Text
ColumnMap-Getter muessen den angezeigten Zellentext liefern (Badges, Ja/Nein, Datumsformate),
Apply **vor** Pagination, `TotalCount` aus der gefilterten Menge. Bei SQL-paginierten Repos
Query **und** Count identisch filtern (Expression-Trees, **kein** `EF.Functions.Like` wegen
InMemory-Tests).
**Warum:** Der Anwender filtert nach dem, was er sieht. Filtert der Server auf den Rohwert, ist
das Ergebnis fuer den Anwender unerklaerlich. Divergierende Query/Count ergeben falsche
Seitenzahlen.

### Pagination-AllCap 5000
`PageSize.Resolve` mapped „Alle" (Sentinel 0) auf `PageSize.AllCap = 5000`;
`PaginationState.IsCappedAtAll` triggert den Banner-Hinweis.
**Warum:** „Alle" muss eine Grenze haben, aber ein **stiller** Cap taeuscht Vollstaendigkeit vor —
deshalb der sichtbare Hinweis.

### Notiz-Autosave vor „Drucken": Tab synchron oeffnen
In `WarehousePicking/Details` wird der Tab synchron mit `about:blank` geoeffnet und **erst nach**
`await saveNotes()` zur Print-URL navigiert.
**Warum:** Ein `window.open` nach einem await gilt als nicht-user-initiiert und wird vom
Popup-Blocker verworfen.

### Lagerbestellungs-Druck: drei Stellen synchron halten
Bei neuen Spalten immer `ColumnDefinitions.WarehousePickingDetails`, das inline `#column-config`
in `Views/WarehousePicking/Details.cshtml` **und** `WarehousePickingPrintLayout.CellText`.
Reihenfolge im Controller: Filtern → Sortieren → sichtbare Spalten. `pos` + `article-number` sind
locked.
**Warum:** Der Druck soll die GUI spiegeln. Die Sortierung repliziert bewusst die
Vergleichslogik aus `table-filter.js` (de-Zahl vs de-String) — sonst druckt das Papier eine andere
Reihenfolge als der Bildschirm, und niemand traut dem Ausdruck mehr.

### BOM-Button fuer Lagerbestellung: eigene Checkbox-Klasse, vor dem read-only-Return
`Views/Picking/Bom.cshtml` nutzt `.warehouse-select` (nicht `.picking-checkbox`) und laeuft im JS
**vor** `if (readOnly) return;`. Der Sticky-Balken `#bomBulkActionBar` bedient beide Bulk-Sets,
`bomSyncBulkBar()` ist null-safe.
**Warum:** Die `.picking-checkbox`-Elemente existieren nur bei `!ReadOnly`. Der
Lagerbestellungs-Button muss aber auch in der read-only Vorbau-BOM greifen.

---

## 4. Authentifizierung, Session, Antiforgery

Siehe auch [[0002-dual-auth-session-login-plus-windows-sso]].

### Windows-Auth und AD-LDAP sind nicht InMemory-testbar
Unit-getestet ist die Entscheidungslogik der `WindowsAutoLoginMiddleware` (via
`IChallengeIssuer`-Abstraktion + Fake-`IUserRepository`) und `UserAgentHelper.IsWindowsDesktop`.
Der echte 401-Negotiate-Handshake und die LDAP-Strecke bleiben Manual-UAT
(`../../docs/TESTSZENARIEN.md` Kap. 40).
**Warum:** Beides braucht eine echte Domaene und den IIS. Gruene Tests sind hier **kein** Beweis,
dass SSO funktioniert.

### `AddNegotiate()` ist unter IIS in-process falsch
Korrekt ist `AddAuthentication(IISServerDefaults.AuthenticationScheme)`. Beide IIS-Auth-Modi
(windows **und** anonymous) muessen aktiv bleiben.
**Warum:** Die IIS-Integration setzt `HttpContext.User` selbst; `AddNegotiate()` ist fuer
Kestrel/HTTP.sys. Anonymous haelt den Formular-Fallback offen — wird es abgeschaltet, sind
Nicht-Domaenen-Geraete ausgesperrt.

### LoginRedirect zaehlt Account-Pfade **einzeln** auf
Die inline LoginRedirect-Middleware in `Program.cs` schliesst **nicht** pauschal `/account/*` aus,
sondern nur `/account/login`, `/account/logout`, `/account/windowslogin` (+ `/api/*`, statische
Pfade, alles mit `.`). Jede neue anonym erreichbare Account-Action muss hier ergaenzt werden.
**Warum:** Genau das war der eigentliche Grund, warum der Button „Mit Windows anmelden" wirkungslos
blieb: der anonyme `GET /Account/WindowsLogin` wurde **vor** dem Action-Aufruf auf
`/Account/Login?returnUrl=…` umgeleitet — die Action, die das ForceSso-Cookie setzt, lief nie.
Symptom im Log: `WindowsLogin 302 → Login 200` ohne dazwischenliegendes `GET /`.

### ForceSso muss **beide** Middleware-Sperren durchhalten
`ForceSsoCookie` ueberschreibt (1) die `NoAutoLogin`-Sperre in `ShouldTryAsync` und (2) den
`AutoLoginTried`-Gate im Challenge-Zweig.
**Warum:** Sonst ist der Button nach einem Logout oder nach einem ersten fehlgeschlagenen
Auto-Versuch wieder wirkungslos. Bei Aenderungen an der Challenge-Bedingung beide mitdenken.

### Kein `AutoLoginTried`-Cookie setzen, wenn nicht gechallenged wird
**Warum:** Bei Nicht-Windows-UA ohne ForceSso wuerde das Cookie einen spaeteren Button-Force
blockieren. Ebenso: `ForceSsoCookie` an **jedem** SSO-Pfad-Ausgang loeschen, sonst bleibt ein
verwaister Force zurueck.

### Logout ohne `[ValidateAntiForgeryToken]` (Mobile-400)
Direkt nach dem SSO wird die Seite unter der Windows-Identitaet gerendert; der Antiforgery-Token
ist daran gebunden. Desktop-Domaenen-Browser senden NTLM beim POST automatisch mit, Mobile-Browser
**nicht** → Token-Identitaet ≠ Request-Identitaet → **400** (danach 405, weil ein Mobile-Reload
den POST zu GET macht).
**Warum:** Der Token laesst sich nicht gleichzeitig fuer Desktop (NTLM-Resend) und Mobile (anonym)
passend binden, solange er an die schwankende Windows-Identitaet koppelt. Logout ist
Low-CSRF-Risk. **Nicht** dasselbe wie der DataProtection-Key-Fall (ephemere Keys nach Recycle →
Token nicht entschluesselbar → auch 400, aber andere Ursache, geloest durch persistente Keys).

### Antiforgery-Wurzel-Fix: Identitaet normalisieren
`NormalizeUserForSession` setzt `HttpContext.User` auf anonym — sobald eine App-Session existiert
**und** auf allen `/account/*`-Pfaden. Danach sind alle Token konsistent anonym gebunden.
**Warum:** Das Problem war nie auf Logout beschraenkt — **jeder** POST unter SSO konnte 400en.
Ungefaehrlich ist das Strippen, weil die App fuer Autorisierung ausschliesslich Session +
`RequireXxx`-Filter nutzt (kein `[Authorize]`, kein `User.IsInRole`). Preis: beim
**Formular**-Login ist `GetWindowsUserName()` `"SYSTEM"` (bewusst akzeptiert).

---

## 5. Windows-Service, Syncs, Protokoll

Siehe auch [[0010-aktivitaets-protokoll-mit-isolierten-dbcontexts]] und
[[0008-servicesettings-db-first-mit-typisiertem-katalog]].

### `SyncLogger` nutzt `IDbContextFactory`, nicht den Scope-DbContext
**Warum:** Diagnose-Logs duerfen nicht in Sync-Transaktionen mitrollen — sonst fehlen sie genau
im Fehlerfall.

### `SyncLog.Timestamp` ist `DateTime.Now`, nicht `UtcNow`
`SyncRun.WriteEntryAsync` setzt den Timestamp nicht explizit; der Model-Default (Lokalzeit)
greift. Die UI zeigt ihn ohne Konversion.
**Warum:** Wer `UtcNow` setzt, produziert Eintraege, die in der DESC-Sortierung zwei Stunden
„frueher" erscheinen und unter aelteren verschwinden — der Bug v1.15.0–v1.15.2 (gefixt in
v1.15.3, Commit `a2a3275`). Bei einer echten Multi-Timezone-Umstellung konsistent umstellen, nicht
halb.

### UI „Aktivitaets-Protokoll" vs Tabelle `SyncLogs`
Bewusste Asymmetrie: UI-Label neu, DB-Tabelle/Klassen/Interfaces behalten `SyncLog*`. Route bleibt
`/SyncLog/Index`, Filter-Parameter `?colf_Service=…`.
**Warum:** Ein Rename durch DB, Code und Routen haette hohen Diff-Aufwand ohne fachlichen Gewinn.

### `ISyncLogger` ist der **letzte** Konstruktor-Parameter (nach `ILogger<T>`)
Und: Connection-String-Validierung liegt **innerhalb** des try-Blocks nach `BeginRunAsync`.
**Warum:** Einheitliche Reihenfolge macht die Services vergleichbar. Liegt die Validierung vor
`BeginRunAsync`, feuert `FinishFailedAsync` bei Config-Fehlern nicht — der Lauf bleibt ewig
„offen".

### Neuer Service-Name muss in `SyncLogServices.All`
**Warum:** Sonst kennt der Protokoll-Filter den Namen nicht und der Lauf ist in der UI nicht
auffindbar.

### Service referenziert das Web-Projekt (seit BDE Phase 2.3)
`IDEALAKEWMSService` referenziert `IdealAkeWms` direkt. Neue Service-Features nutzen den shared
`ApplicationDbContext` und Web-Repositories statt Dapper-Duplikate.
**Warum:** Bis Phase 2.2 gab es zwei Welten (eigene DTOs + raw SQL im Service). Der
BdeAutoPauseWorker brauchte `ApplicationDbContext`, `BdeShiftCalendarService` und den
`BdeBookingStatus`-Enum — Duplizieren haette drei Wahrheiten erzeugt.

### Sync-Bloecke sind einzeln gekapselt (`RunResilientAsync`)
Ein Fehler killt die anderen Syncs nicht mehr.
**Warum:** Vorher liess ein `throw;` in `SyncProductionOrdersAsync` **alle** Folge-Syncs des
Zyklus ausfallen — ein Sage-Hickup legte damit auch OSEON, enaio und die Mails still.

### BDE Auto-Pause setzt `EndedAt` auf das exakte Schichtende
Nicht auf `DateTime.Now`.
**Warum:** Sonst haengt die gebuchte Dauer am Worker-Tick (`Sync:BdeAutoPauseIntervalMinutes`) —
die Buchung waere bis zu einer Stunde zu lang.

### Lagerbestand-Nullsetzen: `sagePresentKeys` aus den **rohen** Zeilen
`sagePresentKeys` wird aus `rawSageRows` **vor** dem Dedup gebaut. `managedStock` muss Netto-0-Paare
und Umbuchungs-Quellseiten-Keys ausfiltern. Guard: leerer Sage-Read → Skip (keine Mail); Cap
`Sync:LagerbestandNullsetzenMaxPerRun` ueberschritten → Skip + Fehlermail, **kein** throw.
**Warum:** Sage liefert 0-Bestand-Zeilen gar nicht mehr — ein reiner Delta-Vergleich wuerde sie
nie auf 0 korrigieren. Baut man die Keys **nach** dem Dedup, wuerden mehrdeutige Duplikat-Paare,
die in Sage sehr wohl existieren, faelschlich genullt. Der Cap schuetzt vor Sage-Teil-Reads.

### `SubOrderNumber` nur schreiben, wenn die Spalte existiert
`SageImportService.SyncProductionOrdersAsync` prueft per `COL_LENGTH`
(`SageProductionOrderSql.BuildUpsert`).
**Warum:** Das IDEAL-Schema hat `SubOrderNumber NOT NULL + UNIQUE`, die andere Linie kennt die
Spalte nicht. Quick-Fix-Charakter: laeuft der Nicht-IDEAL-Import gegen eine IDEAL-DB, matcht der
`OrderNumber`-Upsert-Key ggf. bestehende Zeilen. Sauberer Zielzustand bleibt die
Branch-Zusammenfuehrung.

### Cleanup: Stichtag aus `DateTime.Now`, self-cleaning
`CleanupWorker` (24h-Takt, DryRun-bewusst). `N <= 0` = deaktiviert = **still**, kein
Protokoll-Eintrag. Erst loeschen, dann `FinishSuccess`.
**Warum:** Stichtag konsistent mit der Timestamp-Speicherung (Lokalzeit). Der eigene Lauf-Eintrag
ist neuer als der Stichtag, deshalb loescht er sich nicht selbst.

### Mails immer `multipart/alternative` mit nackter URL im TextBody
`IMailService.SendAsync` nimmt ein optionales `string? textBody` (vor `ct`).
**Warum:** Reines `HtmlBody` fuehrt in Outlook/Copilot zur `[URL]Text`-Rohtext-Darstellung von
`<a>`-Links. Der Plain-Text-Teil braucht die **nackte** URL ohne Markup — Clients linkifizieren
selbst. Moq-Setups auf `SendAsync` brauchen einen zusaetzlichen `It.IsAny<string?>()` vor dem
`CancellationToken`.

---

## 6. BDE-Buchungslogik

### Mehrfach-Buchungs-Regel wird im Service erzwungen, nicht per UNIQUE-Index
Ohne Konfiguration: ein Operator hat eine aktive Buchung, ein Arbeitsgang hat eine aktive
Buchung. `BdeMehrfachBuchungProOperator` und `BdeMehrfachBuchungProArbeitsgang` lockern das
**unabhaengig** voneinander. Die Indexes `IX_BdeBookings_*_Active` sind seit Phase 2.2 nicht mehr
UNIQUE.
**Warum:** Ein UNIQUE-Index kann nicht konfigurierbar sein. Die Regel musste schaltbar werden,
also wanderte sie in den Service.

### `Paused` hat `EndedAt` gesetzt
Fortsetzung erzeugt eine **neue** Buchung mit `ParentBookingId`. Die Cockpit-Query
`WHERE EndedAt IS NULL` zeigt daher nur Running.
**Warum:** Zeit-Split braucht abgeschlossene Intervalle. Wer „pausiert" als offene Buchung
modelliert, kann die Pausendauer nicht herausrechnen.

### `SaveChangesAsync` zwischen Schliessen und Neu-Anlegen
Bei Auto-Close-und-New-Start: Helfer `FinishAndSaveAsync`, eingebettet in
`BeginTransactionAsync()`.
**Warum:** Sonst sieht die DB fuer einen Moment zwei aktive Buchungen und die
Enforcement-Pruefung schlaegt gegen die eigene Aenderung an.

### Deaktivierter Operator mit offener Buchung
Offene Buchungen bleiben sichtbar; der Schichtleiter muss sie manuell schliessen.
**Warum:** Automatisches Schliessen wuerde eine erfundene Endzeit in die Zeitwirtschaft
schreiben.

### BDE-Sperre bei verpackt/abgeholt
Guard `EnsureOrderNotPackedAsync` in `StartPlannedAsync` (nach Werkbank-Gate, **vor** der
Auto-Close-Transition) und `ResumeAsync` (nur wenn `parent.WorkOperationId.HasValue` —
Activity-Resume bleibt frei). Kein Guard in Beenden/Pausieren/Mengen. `IsDoneBde` blockt nichts.
Ohne Satellit keine Sperre. `GetOpenByWorkplaceIdAsync(workplaceId, excludePackedOrders = false)`
— Default false erhaelt Tracking/ByWorkplace, nur der BDE-Aufrufer setzt true.
**Warum:** Wer nicht mehr starten darf, muss eine **laufende** Buchung noch beenden koennen —
sonst haengen Zeiten offen. Der Default-false-Parameter schuetzt die bestehenden Aufrufer;
Moq-Setups muessen beide Argumente explizit angeben.

---

## 7. Lager, Bestand, Bestellwesen

### Kommissionierwagen (`IsPickingTransport`) — wo gefiltert wird und wo nicht
**Gefiltert** in: Stueckliste-Quell-Dropdown, Bestand, Meldebestand-Farbcodierung,
`StockCheckService`. **Nicht gefiltert** in: Bestandsuebersicht-Liste, Bewegungshistorie. Das
Ziel-Dropdown zeigt **nur** Wagen.
**Warum:** Ein Wagen ist Transport, kein Lagerbestand — er darf Verfuegbarkeits- und
Meldebestandsrechnungen nicht verfaelschen. Fuer Nachvollziehbarkeit muss er in Uebersicht und
Historie aber sichtbar bleiben.

### `IsActive` vs `IstBuchbar`
Zwei unabhaengige Flags auf `StorageLocation`. `IsActive` ist Sage-controlled (Phase-1-Sync),
`IstBuchbar` ist user-controlled. Buchungs-Dropdowns filtern auf **beide**;
Bestand-Aggregation und Sage-Korrektur-Buchungen ignorieren `IstBuchbar`. Default: Manual=true,
Sage=false.
**Warum:** Sage entscheidet, ob der Platz existiert; das WMS entscheidet, ob dort gebucht werden
darf. Wuerde die Aggregation `IstBuchbar` beachten, verschwaende vorhandener Bestand aus der
Bilanz.

### Picking Source-Lagerplatz-Fallback ist NAN
Auto-Suggest beruecksichtigt nur Plaetze, die im Dropdown erscheinen koennen (`IstBuchbar=true`,
kein Wagen). Sage-Plaetze mit Bestand aber `IstBuchbar=false` werden **nicht** vorgeschlagen; auch
gespeicherte, inzwischen nicht mehr buchbare `SourceStorageLocationId`s werden ignoriert und
ersetzt.
**Warum:** Ein Vorschlag, den der Anwender im Dropdown nicht auswaehlen kann, ist schlimmer als
keiner.

### Hauptlagerplatz: Lock-Semantik ohne Source-Flag
`SagePrimaryStorageLocation` nicht leer ⇒ Wert stammt aus Sage ⇒ in der App gesperrt (der
Edit-POST ignoriert die eingehende `PrimaryStorageLocationId` **serverseitig**); leer ⇒
app-editierbar. Sync: Sage liefert Wert → Rohcode + FK-Lookup, kein Match → FK null + Warn +
Count `hauptlagerplatz_fehlt`; Sage leer → FK **unberuehrt**.
**Warum:** Der Rohcode ist gleichzeitig der Herkunftsnachweis — ein zusaetzliches Source-Flag
waere redundant und koennte divergieren. „Sage leer lasst FK unberuehrt" bewahrt die manuelle
App-Wahl. Sortierung „Hauptlagerplatz zuerst" liegt zentral in
`StockMovementRepository.GetCurrentStockAsync` / `GetStockByArticleNumbersAsync`; das Badge ⭐
gibt es nur in `StockOverview/Index`.

### `MovementType`-Erweiterung trifft 6 Stellen
Bei jeder neuen `MovementType` die Aggregations-Logik in `StockMovementRepository` (5 Stellen) und
`PickingTransferService` aktualisieren.
**Warum:** Die kollabierten Switches (`Ausbuchung ? -Quantity : Quantity`) behandeln unbekannte
Werte **still falsch** — der Bestand wird lautlos verkehrt gerechnet.

### `PartiallyDelivered` ist kein End-Status
Solche Bestellungen bleiben in `WarehousePicking/Index` bearbeitbar; der Status wird beim erneuten
Close neu abgeleitet. `GetForWarehouseAsync` nimmt ein `WarehouseRequisitionStatus[]` und zeigt
ohne expliziten Filter beide offenen Status.
**Warum:** Restlieferungen sind der Normalfall, nicht die Ausnahme. Ein End-Status haette den
Lager gezwungen, eine neue Bestellung anzulegen.

### `ShortageStatus`-Enum statt `IsFinalShortage`-Bool
`None=0` / `WillBeRestocked=1` / `NoRestock=2`. Order wird `PartiallyDelivered` bei
`WillBeRestocked`, sonst `Closed`. MissingParts-Tabs: `WillBeRestocked` = „Offene Fehlteile",
`NoRestock` = „Wird nicht nachgeliefert".
**Warum:** Ein Bool konnte „kommt nach" nicht von „gar nichts vermerkt" unterscheiden. Die
Migration ist **daten-destruktiv** — `Down()` verliert die Unterscheidung None/WillBeRestocked
(Backup vor Deploy).

### `Note` vs `NoteEinkauf`
Property heisst `Note`, UI-Label aber „Notiz Lager". Die zweite Notiz heisst in Code und UI
`NoteEinkauf` / „Notiz EK". Form-Parameter-Reihenfolge: `notes` vor `notesEinkauf` vor
`shortageStatuses`.
**Warum:** Das Rename Note→NoteLager wurde bewusst **nicht** gemacht (grosser Diff, keine
semantische Notwendigkeit im DB-Layer). Wer den Namen fuer die Einkaufs-Notiz haelt, schreibt in
die falsche Spalte.

### `MissingPartsController` filtert per Default auf eigene Werkbaenke
`mineOnly=true`. Die Lager-Sicht laeuft ueber den separaten `MissingPartsLagerController`. User
ohne Workplace-Zuordnung sehen eine **leere** Liste mit Info-Banner — kein automatischer Fallback
auf alle Fehlteile. Das Werkbank-Dropdown zeigt nur eigene Werkbaenke (`GetByUserIdAsync`).
**Warum:** Ein stiller Fallback auf „alle" haette dem Werker eine fremde Arbeitsliste als seine
eigene praesentiert.

### `ProductionWorkplace.OverridePrePickingDays`: Vorrang vor dem globalen Wert, `0` zaehlt
*(Historie: Das Feld war von der Werkbank-Einfuehrung bis v1.26.0 **wirkungslos** — gepflegt,
gespeichert, angezeigt, aber von keiner Terminberechnung gelesen. Aufgeloest in v1.27.0,
Spec [[2026-07-28-override-prepickingdays]], Variante A.)*

Die Vorkommissioniertage einer FA-Zeile kommen **nicht** direkt aus dem AppSetting
`VorkommissionierTage`, sondern aus `PrePickingDaysResolver.Resolve(workplaceOverride, global)`:
ist an der Werkbank ein Wert hinterlegt, gewinnt dieser — **inklusive `0`**. `0` heisst „BG-Termin
= Kommissioniertermin", ein **leeres** Feld heisst „globaler Standard". Wer die Unterscheidung auf
`> 0` statt `HasValue` umbaut, killt den Null-Vorlauf still.
**Warum der Resolver:** dieselbe Terminlogik liegt in drei Controllern
(`ProductionOrders`, `PickingLeitstand`, `FaWorklist`). Steht die Prioritaetsregel dort dreimal,
driftet der BG-Termin derselben FA je nach Liste auseinander — genau das darf nicht passieren
(und weil `CoatingDateCalculator` auf dem BG-Termin aufsetzt, driftet der Beschichtungstermin mit).
Neue Stelle, die Vorkommissioniertage braucht → **Resolver aufrufen, nicht das Setting lesen**.
**Fallstrick UI:** Spaltenkopf-Tooltips duerfen die Tage-Zahl nicht mehr als feste Zahl nennen —
sie gilt nur noch fuer Zeilen ohne Override (deshalb der generische Header-Text + das
Pro-Zeile-Badge an der Werkbank-Zelle).

### `StorageLocation.Code`: DB 50 Zeichen, manuell 12
DB-Spalte ist `NVARCHAR(50)`; manuelle Codes bleiben per `IValidatableObject.Validate` auf 12
Zeichen (Barcode-Lesbarkeit), Sage-Codes nutzen den vollen Platz.
**Warum:** Sage-Codes sind laenger als das, was der Handscanner zuverlaessig liest — die
Beschraenkung gilt daher nur fuer selbst angelegte Plaetze.

### Glas-Bestellung: Typ steht nur bei der Anlage fest, Enforcement zweifach
`WarehouseRequisition.Type` (Lager=1/Glas=2) — kein Typwechsel, keine gemischten Auftraege.
Die Artikelsuche filtert per `?type=` **und** die AddItem-API validiert serverseitig. Zentrale
Regel in `GlasArticleGroupFilter` (`NormalizeGroup`, `IsAllowedForType`); `GemeinsameArtikelgruppen`
(Default `EUZ`) immer beidseitig erlaubt.
**Warum:** Client-Filter allein reicht nie — ein manipulierter Request wuerde sonst Glas in eine
Lagerbestellung schmuggeln. **Test-Fallstrick:** `Type` hat `HasDefaultValue(Lager)`; in Tests
nie `(WarehouseRequisitionType)0` setzen, 0 ist kein gueltiger Enum-Wert.

### Feature-Toggle mit Default **true** braucht die richtige Read-Semantik
`LagerbestellungAktiv`: `!string.Equals(raw, "false", OrdinalIgnoreCase)` — null/`"true"` = aktiv,
nur `"false"` sperrt. **Nicht** das `?.Equals("true") == true`-Muster.
**Warum:** Letzteres defaultet auf false und haette alle bestehenden Installationen beim Upgrade
dunkel geschaltet. Siehe [[0011-feature-toggles-ueber-appsettings]].

### Quick-Add aus der BOM: Typ-Ableitung und Redirect-Semantik
`POST /api/warehouserequisitions/quick-add` leitet den Typ aus der Artikelgruppe ab (Glas-Gruppe →
Glas, gemeinsame/EUZ → Lager), legt je Typ **einen** Draft pro Lauf an (offener Draft wird
wiederverwendet), Werkbank = **erste** zugeordnete User-Werkbank (keine → BadRequest). Ungueltige
Items → `skipped`; alles skipped → BadRequest. Genau ein Typ betroffen → Redirect auf
`Edit/{id}`, gemischt → Uebersicht.
**Warum:** Ohne Draft-Wiederverwendung haette jeder Klick eine neue Bestellung erzeugt.

---

## 8. Tests, EF, SQL Server

### InMemory-DB unterstuetzt kein `rowversion`
Tests nutzen `TestApplicationDbContext`.
**Warum:** Der InMemory-Provider kennt keine Concurrency-Token; ohne den Test-Kontext scheitert
jedes `SaveChanges` auf einer Entitaet mit RowVersion.

### InMemory-DB erzwingt keine UNIQUE-Indexe
Duplicate-Detection braucht einen App-Layer-Guard, wenn sie testbar sein soll.
**Warum:** Ein Test, der sich auf den Index verlaesst, ist gruen und die Produktion wirft — oder
umgekehrt.

### SQL Server parst Batches vorab
Tabellen in einem separaten Batch (`GO`) erstellen, `OBJECT_ID`-Guard verwenden.
**Warum:** Der Parser prueft den gesamten Batch, bevor er ihn ausfuehrt — Referenzen auf eine
Tabelle, die im selben Batch erst entsteht, sind Syntaxfehler.

### `EF PendingModelChangesWarning`
Nach Model-Aenderungen immer `dotnet ef migrations add` ausfuehren.
**Warum:** EF vergleicht Modell-Snapshot und Modell beim Start und verweigert sonst den Dienst.

### FreshInstall braucht **zwei** Ergaenzungen pro Migration
(1) Schema-Objekte im konsolidierten Schema, (2) die `MigrationId` im
`__EFMigrationsHistory`-INSERT-Block am Ende.
**Warum:** Fehlt (1), scheitert FreshInstall direkt. Fehlt (2), scheitert der **erste App-Start
danach** — EF replayt die Migration gegen ein Schema, in dem die Objekte bereits existieren. Der
zweite Fall ist der gemeinere, weil er erst beim Kunden auffaellt. Siehe
[[0004-migrations-und-sql-disziplin]].

### `AppSettings` ist **kein** `AuditableEntity`
Nur `Key` (PK), `Value`, `Description`.
**Warum:** Konfigurations-Key-Value-Ablage, keine fachliche Entitaet. Siehe
[[0003-auditableentity-als-entity-basis]].

### `ServiceSettings.GetIntSafeAsync` liest aus der DB, nicht aus `IConfiguration`
In InMemory-Tests fallen Caps deshalb immer auf ihren Default (z. B. 100) — Cap-Tests muessen
entsprechend viele Datensaetze seeden.
**Warum:** Wer den Cap per Testkonfiguration setzen will, wundert sich, dass der Test nicht
greift.

### Raw-SQL-Pfade sind nicht InMemory-testbar
Betrifft u. a. `BomCacheSyncService.ReadOpenOrdersInWindowAsync`, das Reconcile-UPDATE, den
Artikel-/Bestand-Sync und die Protokollzeilen darin. Testbar sind die daneben liegenden
Entscheidungs-Helper (`ProductionOrderReconciler`, `LagerbestandZeroingPlanner`,
`ActivityLogCleanupPlanner`, `BomCacheCoverage`).
**Warum:** Der `bebcca0`-Test prueft z. B. nur die **ungenutzte** EF-Methode und faengt den
raw-SQL-Pfad nicht ab. Gruene Tests sind hier kein Beweis — bei Aenderungen an der Auswahl immer
**beide** Stellen pruefen und Manual-UAT fahren.

### `SyncWorkerTests` sichern nur die Invariante
„Ohne DB laufen true-Default-Gates, false-Default-Gates skippen, kein Crash." Die wertabhaengige
„laeuft-wenn-in-DB-enabled"-Wirkung ist Manual-UAT.
**Warum:** Damit niemand aus gruenen Worker-Tests schliesst, dass die Toggles wirken.

### Sage-Lagerbuchung-Enqueue ist NICHT transaktional mit der Buchung (v1.28.0)
`Repository<T>.AddAsync` ruft sofort `SaveChangesAsync` — der `StockMovement` ist also bereits
committed, wenn der Enqueue-Decorator danach den Queue-Eintrag schreibt (zweite Transaktion). Der
Decorator faengt Enqueue-Fehler daher **ab und wirft nie** (sonst sieht der Anwender „Buchung
fehlgeschlagen", obwohl sie gespeichert ist → Doppelbuchung). Ein Crash zwischen beiden SaveChanges
liesse einen Queue-Eintrag verpassen — dagegen laeuft im `SageBookingWorker` ein
**Reconciliation-Sweep** (Ein-/Ausbuchungen auf Sage-Plaetzen ohne Queue-Eintrag, kurzes
15-Min-Rueckblickfenster).
**Warum kurzes Fenster:** Ein grosses Fenster wuerde Buchungen aus einer Toggle-Aus-Phase
nachtraeglich senden — der Sweep soll nur echte Enqueue-Fehler heilen, nicht bewusst nicht gemeldete
Buchungen resurrektieren.

### Sage-Lagerbuchung: Idempotenz nur bei genau EINER Worker-Instanz
Der Baustein „Status VOR dem HTTP-Call auf `Gesendet`" verhindert einen zweiten **automatischen**
Send im naechsten Tick — aber nur bei einer einzigen laufenden `SageBookingWorker`-Instanz. Bei
Doppel-Deploy/Failover auf derselben Queue senden beide → Doppelbuchung. Vor einem **erneuten**
Senden (Requeue / haengender `Gesendet`) prueft der Worker per Read-Lookup gegen Sage
`KHKLagerplatzbuchungen` (Korrelation `SM#<id>#` im `Memo`), ob die Buchung dort schon existiert.
**Warum Delimiter `#`:** `SM#12#` darf nicht Praefix von `SM#123#` sein, sonst trifft der LIKE-Lookup
fuer Bewegung 12 faelschlich Bewegung 123. **Dev-Lauf/UAT:** exakte Korrelationsspalte (`Memo` vs.
`Referenz`) am Sage-Testsystem bestaetigen — eine Zeile in `SageBuchungLookupReader`.

### Lokaler `dotnet run` der Web-App ist ein Deploy gegen AKESQL20
`Program.cs` ruft beim Start `db.Database.Migrate()` — und **beide** `appsettings*.json` (auch
`appsettings.Development.json`) zeigen auf `AKESQL20.ake.at/IDEAL_AKE_WMS`. Ein lokaler Start aus einem
Feature-Worktree versucht also, **alle noch nicht angewendeten Migrationen des Zweigs auf die
Produktions-DB** zu bringen. Konkreter Vorfall (2026-09-14, PDF-Erzeugung): ein Start zum Erfassen einer
Log-Zeile erreichte `Migrate()`; pending waren `InvertProductionOrderHierarchy` + `AddProductionOrderMatchcode`.
Gerettet hat nur, dass `AddColumn SubOrderNumber` dort scheiterte (Spalte existiert) und EF Core 10 die
gesamte Migrations-Menge in **einer** Transaktion zurueckrollt — read-only nachgeprueft: History-Top
unveraendert, keine der neuen Spalten vorhanden. **Regel:** Die Web-App aus einem Worktree **nicht**
lokal starten, solange der Zweig Migrationen traegt, die auf AKESQL20 nicht per SQL-Skript + History-Insert
eingespielt sind. Start-Nachweise (Log-Zeilen, Start-Proben) sind Manual-UAT auf dem Zielsystem.
Wer lokal starten muss: `DefaultConnection` per User-Secret/Umgebungsvariable auf eine eigene DB umbiegen
— **vor** dem Start pruefen, nie hinterher.

### `dotnet run` (Development) scheitert schon in `Build()` — ValidateOnBuild trifft die DbContextFactory
Seit `a9475e2` (ADR 0010) steht `AddDbContextFactory<ApplicationDbContext>` **nach** `AddDbContext<…>`.
`AddDbContext` registriert `DbContextOptions` **scoped**, `AddDbContextFactory` (Singleton) findet sie
dann per `TryAdd` schon vor und konsumiert die scoped Options aus einem Singleton. Unter IIS (Production)
laeuft das, weil dort weder `ValidateScopes` noch `ValidateOnBuild` aktiv sind; in **Development** bricht
`builder.Build()` mit „Cannot consume scoped service `DbContextOptions` from singleton
`IDbContextFactory`" (und dasselbe fuer `ISyncLogger`). Fix, wenn er gebraucht wird: `AddDbContext(...,
optionsLifetime: ServiceLifetime.Singleton)` — bewusst **nicht** nebenbei in einem Feature-Lauf, weil es
die Options-Lebensdauer aller Scopes aendert. Bis dahin gilt der Fallstrick oben: gar nicht lokal starten.

## 9. IDEAL — hierarchische Produktionsauftraege (Teil 7)

### `ProductionOrder.OrderNumber` ist nach der Schema-Inversion NICHT mehr unique
Bis v1.31.0 war `OrderNumber` der eindeutige FA-Schluessel (Unique-Index). Teil 7 (v1.32.0) invertiert
das: die Eindeutigkeit liegt jetzt auf **`SubOrderNumber`** (= Sage BelID), `OrderNumber` (= Sage
StrukturID/HauptFA) ist nur noch ein nicht-eindeutiger Index. Im **flachen AKE-Modus** gilt weiter die
Invariante `OrderNumber == SubOrderNumber` (Backfill) — Verhalten identisch. Im **hierarchischen
IDEAL-Modus** teilen HauptFA + alle Sub-FAs dieselbe `OrderNumber`.
**Warum das eine Falle ist:** jeder `FirstOrDefault(o => o.OrderNumber == x)` / `WHERE OrderNumber = @x`
greift dann willkuerlich eine Zeile bzw. trifft die ganze Gruppe. Der `OrderNumber`-Sweep (Teil 7
Etappe D, Katalog in [[2026-07-29-standort-ideal-teil-7]]) hat **7 kritische Eindeutigkeits-Lookups**
identifiziert (`GetByOrderNumberAsync`, `GetByFaAndOperationAsync`, `SageProductionOrderSql`-EXISTS/UPDATE,
`SageImportService`-Storno/Reaktivierung). Diese sind am **AKE korrekt** (Invariante), am **IDEAL
deaktiviert** — ihre Umstellung auf `SubOrderNumber`/mengenwertig ist **Teil 8**. Wer einen neuen
`OrderNumber`-Lookup schreibt: entscheiden, ob **Gruppen-Lookup** (alle Sub-FAs, bleibt `OrderNumber`)
oder **Eindeutigkeits-Lookup** (dann `SubOrderNumber` bzw. `GetAllByFaAndOperationAsync`). Details:
[[0012-fa-hierarchie-einweg-migrationstor]].

### `SageMissingSince` ist ein Zeitstempel, NICHT `IsCancelled`
„Aus der Sage-Struktur verschwunden" (Sync-Regel 2 der Materialisierung) und „storniert" sind fachlich
verschieden. Der Materialisierungs-Sync loescht nie, sondern setzt `SageMissingSince` (selbstheilend:
NULL beim Wiederauftauchen). **Warum kein Bool/kein `IsCancelled`:** ein Bool haengt bei Wiederauftauchen
fest, und die Vermischung mit „storniert" waere spaeter nicht mehr aufloesbar.

### Der Master-Anzeige-Cache lebt im Web, der Materialisierungs-Sync im Service (getrennte Prozesse)
`HierarchischeStrukturStatus` (gecachter „ist gesperrt"-Zustand) ist ein Web-Singleton; der
Materialisierungs-Sync laeuft im Windows-Service und kann ihn **nicht** direkt auffrischen.
**Loesung:** die Umschalt-Seite refresht den Cache bei jedem GET (admin-only, niederfrequent) — der
Schreibpfad (Guard-Decorator) prueft ohnehin **live**. Eine kurzzeitig veraltete Anzeige ist harmlos
(Freigabe-Antwort 3). Nicht versuchen, den Web-Singleton aus dem Service-Prozess zu setzen.

### EF-Migration UND idempotentes SQL-Skript brauchen BEIDE dieselben Guards
`db.Database.Migrate()` (App-Start, `Program.cs`) fuehrt die **EF-Migration** aus — **nicht** das
handgeschriebene `SQL/XX_*.sql`. Beide Pfade existieren parallel (ADR 0004): das SQL-Skript fuer den
manuellen/Produktiv-Deploy, die EF-Migration fuer den App-Start. **Fallstrick:** Guards nur ins
SQL-Skript zu schreiben reicht nicht. Konkreter Vorfall (Teil 7, 2026-08-17): die EF-Migration
`20260814105526_InvertProductionOrderHierarchy` machte ein **ungeschuetztes**
`migrationBuilder.DropIndex("IX_ProductionOrders_OrderNumber")`. Auf einer **FreshInstall-DB** ist die
OrderNumber-Eindeutigkeit aber ein `UQ_ProductionOrders_OrderNumber`-**Constraint**, kein
EF-benannter `IX_`-Unique-Index → `db.Database.Migrate()` brach mit **SqlError 3701** („Index ...
nicht vorhanden"). Das idempotente `SQL/90` raeumte laengst **beide** Formen ab (UQ_-Constraint +
IX_-Unique-Index), die EF-Migration nur eine. **Warum das durch QA rutschte:** Build+Tests laufen
InMemory und fuehren **keine** raw-SQL-DDL aus — der Index-Tausch ist Manual-UAT (App-Neustart gegen
eine echte DB). **Regel:** Wenn das SQL-Skript `sys.indexes`/`sys.key_constraints`-Guards braucht,
braucht die EF-Migration dieselben — via `migrationBuilder.Sql(@"IF EXISTS ... DROP ...")` statt der
strukturierten `DropIndex`/`CreateIndex`-Operationen. `GO` gehoert NICHT in `migrationBuilder.Sql`
(Client-Direktive; jeder `.Sql(...)`-Aufruf ist bereits ein eigener Batch).

**Nachtrag (2026-08-18): Nicht nur `DropIndex`/`CreateIndex`, auch `AlterColumn` erzeugt ungeschuetzte
Index-DDL.** Der OrderNumber-Fix (`abcd718`) legte in derselben Migration einen **zweiten** 3701 frei:
`migrationBuilder.AlterColumn<string>("SubOrderNumber", nullable:false)`. Weil der **Model-Snapshot**
einen Unique-Index auf `SubOrderNumber` traegt, umschliesst EFs SQL-Server-Generator jeden `AlterColumn`
auf einer indizierten Spalte **automatisch** mit `DROP INDEX [IX_...] ` (ungeschuetzt!) → `ALTER COLUMN`
→ `CREATE ... INDEX [IX_...]`. Dieser Auto-DROP lief **vor** dem weiter unten guarded angelegten Index →
3701 auf jeder frischen/zurueckgerollten DB. **Regel-Verschaerfung:** Sobald eine Spalte einen Index
traegt, gehoert **auch** ihr NOT-NULL-/Typ-Wechsel als `migrationBuilder.Sql(@"IF EXISTS (... is_nullable
= 1) ALTER TABLE ... ALTER COLUMN ...")` geschrieben — **nicht** via `AlterColumn` —, damit EF keinen
Index-Tausch generiert. Faustregel: In einer Migration, die Indizes von Hand (raw SQL) verwaltet, darf
**keine** strukturierte `AlterColumn`/`DropIndex`/`CreateIndex`-Operation auf denselben Spalten stehen —
sie zieht den Auto-Index-Tausch nach. **Offline-Verifikation** (ohne echte DB): `dotnet ef migrations
script <von> <bis>` erzeugt genau die Start-SQL — dort auf ungeschuetzte `DROP INDEX` grepen. Das haette
beide 3701 vor dem Merge gezeigt.

## 10. IDEAL — BOM-Bridge (Stueckliste ueber die Repository-Schnittstelle, v1.36.0)

Kontext: [[0013-bom-bridge-repository-schnittstelle-statt-cache-kopie]],
Spec [[2026-09-08-bom-schnittstellen-bridge-hierarchisch-spec]].

**`BomItem` darf KEINE neue Property bekommen.** `BomRepository` mappt die AKE-View per
`SqlQueryRaw<BomItem>`; EF erwartet fuer **jede** Property eine Spalte im Ergebnis — eine zusaetzliche
Property ohne Spalte bricht die AKE-Stueckliste mit „required column ... not present". **Warum das
nicht auffaellt:** InMemory-Tests fuehren kein raw SQL aus. Hierarchische Zusatzfelder leben deshalb in
der Ableitung `FaHierarchyBomItem : BomItem`; Verbraucher pruefen `bom is FaHierarchyBomItem`.

**`Baugruppe` ist ein String (Eltern-Referenz), kein Bool.** Im AKE-Cache traegt `BomItem.Baugruppe`
die Artikelnummer der **uebergeordneten** Baugruppe; `IsBaugruppe` ist eine im Controller/Builder
**abgeleitete** Eigenschaft (Zeile wird von einer anderen Zeile als `Baugruppe` referenziert). Die
Baum-Darstellung nutzt `Baugruppe` **nicht** — Eltern/Kind kommt ausschliesslich aus `Position`
(`parentPos` = alles vor dem letzten Punkt). Hierarchisch: `Baugruppe` = `Artnr` des unmittelbaren
Elternknotens, `IsBaugruppe` = `SubFA != 0`.

**Kopf-Artikel vs. Zeilen-Artikel nicht verwechseln.** Im AKE-Cache ist `Artikelnummer` der **Kopf**
(wessen Stueckliste) und `Ressourcenummer` die **Komponente**. Die IDEAL-View liefert die Komponente
als `Artnr` (= `KHKPpsFaBelegePositionen.RessourceNummer`). Die superseded Alt-Spec hatte
`Artnr → Artikelnummer` gemappt und `Ressourcenummer` fuer fehlend erklaert — genau diese Verwechslung.
Wurzel-Kopf ist `HauptArtnr` (Wurzelzeile), sonst `Artnr` des Vater-Sub-FA.

**`FullStructure`-Positionen muessen ein rekursiver Pfad ab der Wurzel sein (`3.7.2`), kein flacher
Praefix.** `TreeLevel` (= Anzahl Punkte) und `parentPos` (= Praefix bis zum letzten Punkt) in
`Bom.cshtml`/`PickingController`/`ReadOnlyBomBuilder` setzen voraus, dass **jeder Praefix die Position
einer tatsaechlich vorhandenen Zeile** ist. Ein Praefix `VaterSubFA.Position` (`1043421.7`) ergaebe als
Elternwert eine FA-Nummer, die keine Zeile ist → Gruppierung bricht. Die Sage-Position bleibt separat
sichtbar (`SagePosition`); Geschwister-Kollisionen werden mit `~n` eindeutig gemacht, markiert und
protokolliert — nie still ueberschrieben. Waisen haengen als `W<n>` (ohne Punkt = Top-Level) an.

**Menge je Stueck vs. Auftragsmenge.** AKE-Cache/-View fuehren `Menge` je Stueck, die Verbraucher
rechnen `× order.Quantity` hoch; IDEAL-`Sollmenge` ist bereits die Auftragsgesamtmenge. Deshalb traegt
`BomQueryResult.MengeIstAuftragsmenge` die Semantik und `BomQuantityResolver` ist die **einzige**
Multiplikationsstelle. Wer eine neue Mengen-Stelle baut, ruft den Resolver — nie `bom.Menge * Quantity`.

**Master-Weiche im DI: lazy, sonst Zyklus.** `BomRepository` injiziert `IBomCacheRepository`, das auf
`BomRepositoryMasterSwitch` zeigt; die Weiche loest ihre Ziele (`CachedBomRepository` →
`BomRepository`) deshalb **per Delegate erst beim Aufruf** auf. Die Kette ist nur zyklusfrei, weil
`CachedBomRepository` den **konkreten** `BomRepository` injiziert (nicht `IBomRepository`). Wer diesen
Konstruktor auf `IBomRepository` umstellt, baut eine Endlosrekursion — der DI-Aufloesungstest schuetzt
davor. Beide Interfaces zeigen auf **dieselbe** scoped Weiche-Instanz.

**Artikelinfo hierarchisch: „Geraet" = Eltern-Sub-FA.** `GetDeviceArticleNumbersByComponentAsync`
liefert im hierarchischen Modus **SubOrderNumbers** der Eltern-Sub-FAs (Strings), nicht
Geraete-Artikelnummern; `GetComponentMengePerDeviceAsync` summiert `Sollmenge` je Eltern-Sub-FA.
`ArticlesController` loest sie ueber `GetBySubOrderNumbersAsync` auf und summiert **ohne** `× Quantity`.
Ein Aufrufer, der den flachen Zwei-Schritt-Weg (Artikelnummer → `GetByArticleNumbersAsync`) wiederverwendet,
bekommt still ein leeres Ergebnis.

**Klasse-D-Gates sind DB-first, aber ueber einen Reader.** `IHierarchicalModeReader` kapselt
`ServiceSettings.GetBoolSafeAsync("ProduktionsauftragHierarchisch")`; `ServiceSettings` liest **nur** aus
der DB (kein Config-Fallback) und liefert bei Verbindungsfehler den Default — ohne den Reader waeren
die Gates ohne SQL Server nicht testbar. Der zufaellige Schutz `ProductionDate IS NOT NULL` in
`CoatingDetection`/`BomCacheSync` bleibt Zufall; `SyncSpecificArticleNumbersAsync` (aus
`SageImportService`) hat ihn nicht — deshalb der Gate an **allen vier** Einstiegen.

**Neue Spalte in einer `filterable-table` braucht ZWEI Registrierungen.** `ColumnDefinitions.<View>`
(C#) dient der Prefs-API nur zur **viewKey-Validierung**; die **Client-Wahrheit** ist der Inline-Block
`<script id="column-config">` in der View — `column-preferences.js` liest Spaltenliste,
`defaultHidden` und `defaultWidth` ausschliesslich von dort. Fehlt eine Spalte dort, laesst
`applyColumnOrder()` sie beim Re-Append aus → sie rutscht **vor** alle registrierten Spalten, erscheint
nicht im Zahnrad, `DefaultHidden` bleibt wirkungslos und index-basierte Karten (Print-`colNames`)
verschieben sich. Aufgefallen beim BOM-Bridge-Review (Task 5, 2026-09-08). **Regel:** neue `<th
data-col-key>` immer in **beiden** Stellen eintragen, unter denselben Razor-Gates.

## 11. PDF-Erzeugung (Headless Edge, Spec 2026-08-06)

### `msedge.exe` ist unter Windows ein Launcher — der Prozess-Exit sagt nichts ueber das PDF
Gemessen am 2026-09-14 (Edge 152.0.4191.66, `System.Diagnostics.Process`): der mit
`--headless --print-to-pdf=…` gestartete `msedge.exe` endet nach **200–400 ms mit Exit-Code 0**; das
PDF schreiben **Kindprozesse 1,3–1,8 s spaeter**. `WaitForExitAsync()` + „Exit-Code pruefen" (so stand es
im Spec-Entwurf) meldet deshalb immer „PDF fehlt". Keine Flag-Variante aendert das (`--headless=new`,
`--no-sandbox`, versionierter Pfad); `--single-process` und `--no-startup-window` erzeugen **gar kein**
PDF. **Regel:** auf die **fertige Datei** warten (existiert, exklusiv oeffenbar, endet auf `%%EOF`),
nicht auf den Prozess; den Exit-Code nur protokollieren. Timeout-Kill: die Kinder sind Waisen —
`Kill(entireProcessTree)` auf dem Launcher greift ins Leere; `EdgeProcessRunner` beendet stattdessen
`msedge`-Prozesse im Startzeit-Fenster des Laufs (ponytail-Kommentar dort nennt die Grenze).
**Bewusst in Kauf genommene Kehrseite (v1.39.0):** Das Fenster ist zeit-, nicht PID-basiert. Laeuft ein
zweiter PDF-Auftrag innerhalb von ~3 s an und der erste laeuft in seinen Timeout, beendet der Kill des
ersten **auch** den noch gesunden Edge des zweiten — der meldet dann ebenfalls „konnte nicht erzeugt
werden". Bei `MaxConcurrent = 2` und zwei gleichzeitigen Klicks (TS-74.5) real erreichbar, in der Praxis
selten (Timeout = 30 s). Upgrade-Pfad, falls es stoert: Parent-PID via `NtQueryInformationProcess` statt
Startzeit-Fenster. **Aufrufer-Abbruch** (Werker schliesst den Tab, `HttpContext.RequestAborted`) beendet
den rendernden Edge seit v1.39.0 ebenfalls (zweiter `catch (OperationCanceledException)`), damit keine
Waise ausserhalb der `MaxConcurrent`-Rechnung weiterlaeuft; der `OperationCanceledException` propagiert
dann (kein PDF noetig, Semaphor + Laufverzeichnis werden im `finally` freigegeben).

### Ohne eigenes `--user-data-dir` haengt sich der Aufruf an eine laufende Edge-Instanz
Laeuft unter demselben Benutzer bereits ein Edge (Dev-Rechner!), startet `msedge.exe --headless …` ohne
eigenes Profil **keinen** neuen Browser, sondern reicht den Aufruf an die laufende Instanz durch und
endet mit Exit 0 — es entsteht **nie** ein PDF. Deshalb legt jeder Lauf sein Profil im GUID-
Laufverzeichnis (`%TEMP%\IdealAkeWms-Pdf\<guid>\profile`) an, das im `finally` mitgeloescht wird.
Auf dem IIS (App-Pool-Identitaet ohne interaktives Edge) tritt der Fall nicht auf — der Dev-Test
wuerde ohne diese Regel aber gruen luegen bzw. haengen.

## 12. IDEAL — Matchcode am Artikelstamm (Article.Matchcode, v1.40.0)

### `BuildExtraInfoOrContains` nimmt nur einen `MemberExpression`-Selektor — keine Subquery
Der Helper in `ProductionOrderRepository` baut das Null-Guard-Praedikat, indem er den Selektor-Body zu
`MemberExpression` castet und dessen `.Expression` (das Elternobjekt, z. B. `o.ExtraInfo`) zieht. Ein
Selektor wie `o => o.Matchcode` funktioniert (Body ist ein Member auf `o`), eine **korrelierte Subquery**
`o => _context.Set<Article>()...FirstOrDefault()` ist aber ein `MethodCallExpression` und laesst den Cast
scheitern. **Regel:** Wer einen berechneten (gejointen) Wert als FA-Listen-Spaltenfilter braucht, fuehrt
ihn NICHT durch `BuildExtraInfoOrContains`, sondern ueber die bestehende C#-Postfilter-Maschinerie
(`FaListComputedColumnKeys` in `ProductionOrdersController` — dort filtert der Postfilter das bereits
projizierte `item.<Feld>`). So macht es der Matchcode-Filter seit v1.40.0: Anzeige aus dem Article-Join
fuellen, dann in C# filtern. Beim Aufsetzen die Freigabe-Antwort/Spec nicht ungeprueft uebernehmen — die
Spec 2026-09-13 behauptete faelschlich, der Helper akzeptiere beliebige Selektoren.

### `EF.Functions.Like` in einem `patterns.Any(p => ...)`-Lambda wirft unter EF-InMemory
Nicht nur das nested-`Any`-Lambda mit Contains (bekannt), sondern **jedes** `EF.Functions.Like` in einer
InMemory-ausgefuehrten Query wirft `InvalidOperationException` — der InMemory-Provider hat keine
Client-Eval-Implementierung dafuer. Das betrifft auch die schon laenger bestehenden
Article-Spaltenfilter (`article-number`/`description`/…) — sie waren nie InMemory-getestet. **Muster
(aus v1.40.0, Vorbild `ProductionOrderRepository.BuildLeitstandQuery`):** Query-Bau in eine
`internal`-Methode ziehen (`InternalsVisibleTo` ist gesetzt) und ueber `ToQueryString()` gegen einen
**nie geoeffneten** SqlServer-Context pruefen (der SQL-Text enthaelt dann `LIKE`/`IS NOT NULL`), statt
zu versuchen, die Methode InMemory auszufuehren. Fuer neue Suchpraedikate, die InMemory laufen sollen:
plain `.Contains(term)` und `.Any(a => a.X == y && a.Z.Contains(term))` — beides laeuft unter InMemory
UND SQL Server. Und: ein Multi-Token-Filter, der als `positives.Any(nested subquery)` gebaut wird,
uebersetzt InMemory ebenfalls nicht — stattdessen je Token eine gefilterte Query bauen und per `Union`
zusammenfuehren (`{x∈q:P1} ∪ … = {x∈q:P1∨…}`, OR-Semantik bleibt, Dedup ist gewollt; so in
`WarehouseRequisitionRepository.ApplyMissingPartsTextFilter` geloest).

### Bewusste Inkonsistenz: FA-Struktur-Baum liest `FaHierarchyNode.Matchcode`, die Listen `Article.Matchcode`
Seit v1.40.0 speisen sich die fuenf FA-Zeilen-Listen aus `Article.Matchcode` (Equi-Join), der
FA-Struktur-Baum (`/FaHierarchy`) liest weiter `FaHierarchyNode.Matchcode` (direkt aus der Strukturtabelle).
Solange beide aus demselben Sage-Feld stammen, ist das unsichtbar. Weicht der positionsbevorzugte
View-Matchcode je vom Artikelstamm-Matchcode ab („Position bevorzugt, KHKArtikel als Fallback" in der
IDEAL-View), zeigen Baum und Listen fuer denselben Knoten unterschiedliche Werte — **so gewollt**
(Artikel gewinnt in den Listen), gehoert aber als „kein Fehler" in den UAT-Vermerk. Backlog-Kandidat, den
Baum spaeter ebenfalls auf den Artikelstamm umzustellen: [[2026-09-15-matchcode-nachlese]].

### `Articles` ist eine gefilterte Projektion von `KHKArtikel` — gefertigte Endgeraete fehlen evtl.
`SageImportService.SyncArticlesAsync` befuellt `Articles` nur mit Artikeln, die in
`KHKPpsRessourcenPositionen` vorkommen ODER `IstBestellartikel = -1 AND Aktiv = -1` sind. Ein gefertigtes
Endgeraet (HauptFA-Artikel) ist Position in keiner Stueckliste und typischerweise kein Bestellartikel — es
kann durch **beide** Zweige fallen und in `Articles` fehlen. Dann liefert der Matchcode-Join `NULL`. Das
ist ein **vorbestehender** Mangel (die Artikelinfo findet solche Artikel heute auch nicht), den der
Matchcode nur sichtbar macht. Deshalb der Fehltreffer-Zaehler (AK 14): der erste echte Lauf beantwortet
die Coverage-Frage selbst. Ist die Zahl im Betrieb > 0, ist der Weg **den `Articles`-Sync zu erweitern**
(gefertigte Artikel aufnehmen), nicht den Matchcode am Auftrag zu duplizieren — eigene Aufgabe
([[2026-09-15-matchcode-nachlese]]).

## 13. IDEAL — FA-Arbeitsgang-Erkennung aus der Struktur (Arbeitsschritte, v1.41.0)

### Dieselbe `Arbeitsschritte`-Zeile zaehlt in ZWEI Scopes — Absicht, kein Doppelzaehl-Fehler
Die Arbeitsgang-Ableitung (`FaWorkStepStructureDetectionService`) sammelt je materialisiertem Sub-FA die
Token aus dem **DirectChildren-Scope** = eigene Zeile + direkte Kinder (`VaterFA = SubFA`) — dasselbe
Muster wie die BOM-Bridge (ADR 0013, Design D). Ist ein direktes Kind **selbst** ein materialisierter
Sub-FA, traegt dessen `Arbeitsschritte`-Zeile in **zwei** Kontexten bei: einmal als Kind im Scope des
**Elternteils** (Arbeitsgaenge zur Fertigung/zum Einbau dieser Baugruppen-Position) und einmal als
**eigene** Zeile bei seiner eigenen Verarbeitung (Arbeitsgaenge seines eigenen Fertigungsauftrags).
**Warum kein Fehler:** dieselbe Sage-Spalte beschreibt an dieser Stelle zwei verschiedene, beide gueltige
fachliche Sachverhalte (Position im Elternkontext vs. eigener Auftrag) — die mechanische Konsequenz der
bestaetigten DirectChildren-Regel. Ein Enkel-Token dagegen landet **nur** beim Elternteil, **nicht** beim
Grossvater (TS-76.2, unit-getestet). Beim ersten echten Datenlauf mit dem Fachbereich gegenpruefen, ob der
Doppel-Kontext gewollt ist — nicht erst im Betrieb auffallen lassen.

### Das Token IST der Code — `SearchString` ist ausschliesslich der AKE-Mechanismus
AKE muss raten (`FaWorkStepDetectionService`: `SearchString.Contains` ueber Bezeichnungen). IDEAL bekommt
die Arbeitsschritte je Position **geliefert** — der Abgleich ist **exakt** ueber `WorkStep.Code`
(case-insensitiv/getrimmt), nie ueber `SearchString`. Der Befund „0 von 13 Token matchen den Katalog" beim
Spec-Schreiben war **kein** Quell-Artefakt, sondern real: der Katalog enthielt nur die fuenf AKE-Codes
(`VA/VE/VK/VL/VT`) mit beschreibenden SearchStrings — kurze Operations-Kuerzel wie `KA`/`LS` koennen dort
**nie** matchen. **Konsequenz fuer den Betrieb:** der Katalog ist **vor** dem Nutzen zu pflegen
(Zwei-Lauf-Ablauf), aber **nach** dem Deploy — der erste Lauf meldet die real vorkommenden Kuerzel
(melden statt anlegen, ADR 0014), ein Mensch legt je Kuerzel `Code = Token` an. Umlaut zeichengleich
(`Code = "SÄ"`, nicht `SAE`), sonst bleibt das Token dauerhaft „unbekannt" — aber **gemeldet**, nicht
still verschluckt.

### Melde-Zustand `IUnknownWorkStepTokenState` ist In-Memory — Neustart meldet einmal erneut
Wie `IUnknownWorkplaceState` (Fallstrick-Klasse S1) ist die Singleton bewusst nur im Speicher: die
Sammelmail geht nur bei **Aenderung der Token-Menge** raus, aber nach jedem Dienst-Neustart ist der
„zuletzt gemeldete Stand" leer → im Fenster zwischen Deploy und Katalogpflege geht die Mail einmal erneut
raus, auch ohne echte Mengenaenderung. Bewusst akzeptiert (einmal zu viel ist harmlos, ein
persistenter Zustand waere Overkill).

## 14. Stueckliste (BOM) — Client-Mode-Dropdown ueber verstecktem Input (v1.42.0)

### Ein Dropdown auf einer Client-Mode-Filterspalte: Input als Quell-Element behalten, Select2 nur davor
`Bom.cshtml` ist die bewusste **Client-Mode-Ausnahme** von ADR 0005 (unpaginiert, durch den Baum
vorgefiltert). Die ganze Filtermechanik haengt am `<input data-col-key="…">` je `data-filterable`-Spalte:
`getActiveFilters()` liest **nur `_filterRow.querySelectorAll('input')`** (col-key → value),
`setColumnFilter(colKey, value)` setzt genau diesen Input, `updateBomVisibility()` und die
Default-Filter-Vorbelegung fahren darueber. **Warum das zaehlt:** Wer die Freitext-Spalte „Komm.-Ziel"
auf ein Select2-Dropdown umstellt und dabei den Input **ersetzt**, reisst alle vier Mechaniken ab
(getActiveFilters findet kein `input` mehr, setColumnFilter/Default/Storage-Restore laufen ins Leere).
**Loesung (v1.42.0):** den `<input>` als **verstecktes Quell-Element** behalten (`display:none`), das
`<select multiple>`+Select2 nur **davor** rendern und beim `change` den komma-verbundenen Wert **in den
Input schreiben** + `updateBomVisibility()` direkt rufen (nicht per synthetischem `input`-Event — das
feuert bei `input.value = …` nicht, siehe Universal-Filter-Fallstrick). Vorbelegung umgekehrt: Select2
liest den (per Default gesetzten) Input-Wert. So bleibt der ganze Client-Filter-Unterbau unveraendert.

### `getActiveFilters()` liefert col-key-Schluessel — die Druck-`colNames`-Map muss darauf passen
Der Druck-Handler (`Bom.cshtml`, `btnPrintBom`) baute den „Filter"-Hinweis fuer `PrintBom` aus einer
`colNames`-Map mit **numerischen** Schluesseln (1..8), waehrend `getActiveFilters()` **col-key-Strings**
liefert (`procurement`, `kommissionieren`, …). Folge: `colNames[col]` traf nie → der Ausdruck zeigte den
rohen col-key statt des Klartextnamens. Beim Ergaenzen der hierarchischen Druck-Spalten die Map auf
**col-key→Label** umgestellt (deckt zugleich die AKE-Spalten korrekt ab). **Merke:** `PrintBomItem` ist
ein eigenes DTO — die hierarchischen Felder (`Kommissionieren`/`Hauptlagerplatz`/`Ebene`) fehlen dort und
im Print-Mapping, obwohl `BomItemViewModel` (Bildschirm) sie laengst hat; der Druck kennt hierarchische
Spalten **strukturell** nicht, bis DTO + Mapping + `ShowCol`-Kopf-/Zellbloecke ergaenzt sind.

## 15. IDEAL — Der Arbeitsbereich ist ein Zielort, keine Werkbank (Rückbau v1.43.0)

### Zwei getrennte Sage-Vokabulare, im August verwechselt
`FaHierarchyNode` trägt aus der Struktur-View **zwei** getrennte Felder, die beide „wo/womit" beschreiben
und leicht verwechselt werden:
- **`Arbeitsbereich`** (Sage `USER_OSAbteilung`, Werte wie `K-02`, `S-01`, `H4-04`) = **Zielort**, wohin
  das Teil kommt, begrifflich aus Lagerorten. **Keine Werkbank.**
- **`Arbeitsschritte`** (Sage `USER_ArbeitsSchritt`, Werte wie `KA`, `SW`, `LS`) = **Arbeitsgänge**; die
  Werkbank ist der Sage-Arbeitsplatz (`KHKPpsArbeitsplaetze`), eine **je Arbeitsgang**, nicht je Auftrag.

**Der Fehler (August 2026, v1.37.0):** `FaMaterializationSyncService` leitete
`ProductionOrder.ProductionWorkplaceId` (die **Werkbank**) aus dem **Arbeitsbereich** ab — unter der
ausdrücklich vorläufigen Annahme „Werkbank = Arbeitsbereich", weil die Sage-Arbeitsplatz-Stammdaten damals
fehlten. **Warum es plausibel schien:** beide sind kurze Codes aus Sage, beide „verorten" die Position;
ohne die Arbeitsplatz-Stammdaten war der Unterschied nicht sichtbar. **Warum es falsch ist:** ein Teil
läuft durch **mehrere** Werkbänke (je Arbeitsgang eine) — eine einzelne Werkbank am Auftrag kann das nicht
abbilden; und ein Zielort ist ohnehin keine Werkbank.

**Rückbau v1.43.0** ([[2026-09-21-adr-0014-arbeitsbereich-ist-zielort-spec]]): die Ableitung samt
`IUnknownWorkplaceState`, `ApplyWorkplace`, Sammelmeldung/-mail und Werkbank-Countern entfernt — **bevor**
sie je produktiv war (IDEAL hatte noch keine `ProductionWorkplaces`). Die Werkbank lebt künftig je
Arbeitsgang ([[2026-09-21-ideal-bde-arbeitsgaenge-aus-struktur-spec]]). `FaHierarchyNode.Arbeitsbereich`
selbst bleibt (Struktur-Cache, Kommissionierlisten-Filter).

### Gewollte Nebenfolge: manuelle Werkbank überlebt jetzt den Sync
Die entfernte Z1-Ausnahme existierte gerade, damit der Sync das Feld bei **jedem** Update überschreibt
(„Sage führend"). Nach dem Rückbau fasst der Sync `ProductionWorkplaceId` **gar nicht mehr** an → eine
per `FaCompletionController.SetWorkplace` **von Hand** gesetzte Werkbank bleibt erhalten. Das ist gewollt
(AK 10 der Rückbau-Spec) und AKE-neutral (die Ableitung lief dort nie).
