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

### Sage-SData liefert ohne `Accept`-Header Atom-XML — `format=json` allein reicht nicht (v1.71.0)
`GET …/RealTimeData/…?…&format=json` aus einem `HttpClient` ohne `Accept`-Header kam als **Atom-XML** (`<feed>…<sdata:payload>`)
zurück, obwohl Sage den Parameter sah (er steht im echoten `<id>`). Postman/Chrome senden eigene `Accept`-Header, daher lieferten
die Lesetests JSON und die Fixtures sahen richtig aus. **Warum es weh tat:** Der erste echte Aufruf scheiterte mit
„'<' is an invalid start of a value“; ohne den QA-Fix Q-1 wäre es still als „nicht gefunden“ durchgegangen.
**Regel:** Jeder SData-Lese-Client setzt `Accept: application/json` (`SageSdataReadClient`, Fix `51c3203d`, belegt am
AKE-Sage-Test 2026-10-30: 835/836/846 alle 200 mit JSON). Fixtures aus Postman/Browser belegen nur den Inhalt, nicht das
Verhalten des eigenen HTTP-Clients — mindestens ein echter Aufruf mit dem eigenen Client gehört vor „Testbereit“.

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

### MERGE-Schluessel = eindeutiger Index; OSEON schreibt Auftraege neu aus (v1.30.2)
Ein Sync-MERGE muss `ON` genau ueber das Merkmal laufen, das in der Zieltabelle eindeutig ist — bei
`OseonProductionOrders` die `OseonOrderNumber`, nicht die `OseonId`. Die MERGE-Quelle muss nach **DB-Kollation**
eindeutig sein: C#-Gruppierung `OrdinalIgnoreCase` plus `ROW_NUMBER() … WHERE rn = 1` als SQL-Waechter.
**Warum:** OSEON schreibt einen Auftrag neu aus (gleiche `AuftragsNr`, neue `ID`). Der MERGE ueber `OseonId` fand
die neue ID nicht, wollte einfuegen und scheiterte am UNIQUE-Index der Nummer (Fehler 2601). Der ganze Lauf brach
ab, `MAX(LastChangedInOseon)` rueckte nicht vor, und derselbe Fehler kam in jedem Lauf wieder (Delta-Haenger).
Ein eindeutiger Index auf `OseonId` waere keine Loesung — er verlagert denselben Fehler an eine andere Stelle.
Die C#-Gruppierung bildet die Kollation nur nach; Zeichen mit Gewicht 0 sieht nur SQL.
→ [[2026-11-07-oseon-sync-neu-ausgeschriebener-auftrag-spec]], [[2026-11-07-oseon-sync-neu-ausgeschriebener-auftrag-bug]].

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
**Zweite zulässige Lösung (v1.81.0/v1.92.0):** Hidden-„false“ **hinter** die gleichnamige Checkbox (angehakt kommt `true,false` → `true`). Vor der Checkbox ist es immer falsch – so verloren OSEON „Fremdsystem-Relevanz“ und BDE-Edit „Final“/„Löschen“ still ihren Wert ([[2026-11-08-oseon-relevanzfilter-schaltet-sich-ab-spec]]). Binder-Nachweis im Projekt: `CheckboxHiddenFeldV192Tests`. Wird ein Wert dadurch erstmals wirksam, die Folgeregel prüfen (BDE: UNIQUE-Index „eine Final-Zeile“ → Formularfehler statt 500).

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
`sortTable()` fasste bis Commit `3894e3d` (v1.31.0) nur das **erste** `<tbody>` an. Bei gruppierten
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

### Spalte ausblenden darf Ein-Zellen-Zeilen (`<td colspan>`) nicht anfassen (v1.65.0)
`column-preferences.js setColVisibility` blendet je Zeile `cells[physicalIdx]` aus. Gruppenkopf- und Leerzustandszeilen haben
genau **eine** `<td colspan>`. War die physisch erste Spalte ausgeblendet (etwa die gesperrte `hauptfa` nach hinten gezogen, dann die
neue erste Spalte aus), traf es genau diese Zelle. Die ganze Zeile verschwand, samt PDF-Link und Varianten-Tabelle.
**Regel:** Zeilen mit `:scope > td[colspan]` werden übersprungen. `:scope >` ist Pflicht, weil Varianten- und Kopfdaten-Zeilen eine
Kindtabelle tragen und ein Descendant-`td[colspan]` dort fälschlich greifen würde. Annahme, im Bestand belegt: Keine Datenzeile hat
eine `colspan`-Zelle neben anderen Zellen. Wer so eine Zeile einführt, muss den Skip verfeinern. Wächter
`TableScriptScopedSelectorTests.ColumnPreferences_SetColVisibility_SkipsColspanRows`.

### Hardware-Scanner: unsichtbares Fokusfeld ist kein Ziel, leeres Int-Feld der Standorteinstellungen bleibt (v1.66.0)
`hardware-scanner.js` fügt einen Scan ins fokussierte Textfeld ein (Regel b), bevor es das einzige `[data-hw-scan-ziel]` sucht (Regel d).
**Warum `freiesFeld` `getClientRects().length` prüft:** Je nach Browser behält ein per `d-none` ausgeblendetes Feld den Fokus. Am
BDE-Terminal ist das `scanOperator` nach der Anmeldung. Ohne die Prüfung ging der FA-Scan dort hinein, das synthetische Enter startete die
Personalnummer-Suche, und es kam eine unsichtbare Meldung oder ein falscher Operator-Wechsel (QA-Befund B-1, `789d0a78`).
**Regel:** Wer „ist das Feld bedienbar?“ fragt, prüft auch die Sichtbarkeit, nicht nur `disabled`/`readOnly`.
**Leeres Int-Feld:** `StandortEinstellungenController.Save` lässt ein leeres Int-Feld unverändert. Keys ohne Seed (hier
`HardwareScannerMaxZeichenabstandMs`) haben keine Zeile, und die Maske schickt `""`. Die alte Validierung hätte **jedes** Speichern der
Maske blockiert. Folge: Ein einmal gespeicherter Wert lässt sich in der Maske nicht mehr auf „leer = Default“ zurücksetzen (QA N-2).
[[2026-10-15-scan-handler-hardware-scanner-spec]].
**Nachtrag v1.66.1:** Für den Zeichenabstand ist die Folge behoben. `StandortField` trägt `Min`/`Max`/`EmptyResetsToDefault`. Leer
speichern schreibt `""`, der Lesende nimmt den Default. Außerhalb von 10–500 erscheint eine Meldung, und nichts wird gespeichert. Andere
Int-Felder bleiben bei leer unverändert. **Regel:** Wer einen Wert mit Default ohne Seed einführt, setzt `EmptyResetsToDefault` und
prüft denselben Bereich auch im Client: Die DB kann per SQL oder einen alten Stand trotzdem einen Wert außerhalb enthalten
(`HwScanCore.abstand`).

### Default-an-Schalter und Vorlagen in den Standorteinstellungen brauchen eine geseedete Zeile; Abschalten nie über CSS-Klassen (v1.67.0)
`StandortEinstellungenController` zeigt ein Bool nur bei `true` als an. Beim Speichern schickt die Maske **alle** Felder und schreibt
Bool als `"true"`/`"false"`, Text roh.
**Warum der Seed Pflicht ist:** Ein Default-an-Schalter ohne DB-Zeile (Lesart „nur `false` sperrt“) stünde nach dem **nächsten
Speichern irgendeines** Standortwerts auf `false`. Eine Default-Vorlage ohne Zeile würde zu `""`. AKE hätte die Vault-/enaio-Links
verloren, ohne dass jemand sie angefasst hat. Darum legt `Program.cs` `VaultLinksAktiv`, `VaultLinkVorlage`, `EnaioLinksAktiv` und
`EnaioLinkVorlage` beim Start an, wenn sie fehlen.
**Regel:** Wer in `StandortSettingsCatalog` einen Bool mit Default an oder einen Text mit Default-Wert aufnimmt, seedet die Zeile.
Default-aus-Schalter (nur `"true"` aktiviert) brauchen keinen Seed.
**Stücklisten-Knopf:** `_FaDocumentLinks` gibt dem Knopf „Stückliste öffnen“ dieselbe Klasse `vault-link` wie dem Vault-Link.
`.vault-link` und `.dms-link` sind reine Optik-Klassen. Abgeschaltet wird **nur serverseitig** im Partial (`_VaultLink`,
`_EnaioDmsBadges`: kein `<a>`), nie über CSS `display:none` oder `querySelectorAll('.vault-link')`; das würde den Stücklisten-Knopf
mit verstecken. Ein Wächter in `VaultEnaioLinksTests` sichert beides.
[[2026-10-21-vault-links-per-schalter-spec]].

---

### Matchcode ist nicht eindeutig; asynchrone Scan-Übersetzung gehört in den Wrapper (v1.68.0)

- **Matchcode:** nie `FirstOrDefault`. `Articles.Matchcode` hat weder Index noch Constraint, Sync und Handpflege trimmen nicht. Es gibt genau eine
  Suche, `ArticleRepository.GetByMatchcodeExactAsync(matchcode, limit)`: Trim + ToUpper auf beiden Seiten, Liste. Der Aufrufer unterscheidet
  keins / eindeutig / mehrdeutig und meldet „mehrdeutig“ sichtbar.
  **Warum:** Die Artikelinfo wählte bei zwei gleichen Matchcodes still einen Artikel. P1 an IDEAL ist heute leer, Sage garantiert aber nichts.
- **Scan-Übersetzung (Matchcode → Artikelnummer) im globalen `processScannedValue`-Wrapper, nicht in der inneren Funktion.**
  **Warum:** Der Wrapper feuert direkt nach der inneren Auswertung synchron den `onScanned`-Callback (Filterseiten-Submit, Sammel-
  Wareneingang). Eine asynchrone Übersetzung weiter innen käme zu spät, der Callback liefe mit dem unübersetzten Wert. Den passenden
  `_textInputScanCallback` beim Scan reservieren und nur bei Treffer wieder setzen.

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

### Mails pruefen ohne Versand: `MailSettings:PickupDirectory`, nie echte Adressen in Tests (v1.99.10)
Ist `MailSettings:PickupDirectory` gesetzt, schreibt `MailService` jede Mail als `.eml` in den Ordner statt sie zu senden – **vor** dem
`SmtpHost`-Zweig. Tests nutzen einen Fake-`IMailService` bzw. einen Temp-Pickup-Ordner und Adressen unter `.invalid`.
**Warum:** Die Lieferverzug-Mail geht an externe Lieferanten; eine Mail an einen Kreditor laesst sich nicht zurueckholen. DEV/QA muss Inhalt,
Reply-To, Absender und CID-Logo pruefen koennen, ohne dass ein SMTP-Relay sie zustellt. In Produktion darf der Key nicht gesetzt sein – dann geht
gar nichts raus (der Lauf protokolliert trotzdem „gesendet“, weil das Ablegen erfolgreich war).

### Lieferverzug-Wiederholung zaehlt nur Zeilen des aktuellen Modus (v1.99.10)
`PurchaseOrderReminders.Testbetrieb` trennt die Historie: im Testbetrieb wiederholt der Planer nach den Testbetrieb-Zeilen, im Echtbetrieb nur
nach echten Kreditor-Zeilen. „Heute schon gesendet“ prueft dagegen **jede** Zeile von heute (auch Einkauf).
**Warum:** Zaehlten Testbetrieb-Zeilen gar nicht, bekaeme der Einkauf im Testbetrieb jeden Tag alle Kreditor-Mails; zaehlten sie fuer den
Echtbetrieb, waeren beim Umschalten alle Lieferanten „schon erinnert“ und die erste echte Erinnerung kaeme bis zu 7 Tage zu spaet. Die erste
echte Welle faengt `Lieferverzug:KreditorMaxJeLauf` ab.

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

### Stuecklisten-Reihenfolge der Bestandsplaetze: Hauptlagerplatz, Menge absteigend, Code (seit v1.62.0)

`StockMovementRepository.GetStockByArticleNumbersAsync` sortiert zentral: Hauptlagerplatz (`Article.PrimaryStorageLocationId`)
zuerst, dann Menge absteigend, bei Gleichstand Code. Wirkt auf Stuecklisten-Zelle, Popover, CSV und Druck. **Warum zentral:**
ein Ort fuer alle vier Ausgaben; die uebrigen Aufrufer (Auto-Vorschlag, `PickingApiController.SearchSourceLocations`,
`MissingPartsLagerController`) sortieren selbst und sind unberuehrt. An IDEAL fehlt vielen Artikeln der WMS-Hauptlagerplatz —
dann gilt nur „Menge absteigend“, kein Fehler ([[2026-10-16-stueckliste-matchcode-lagerplatz-limit-spec]]).

## 8. Tests, EF, SQL Server

> Seit v1.99.23: Deadlock/UNIQUE-Fehler über die ganze Ausnahme-Kette prüfen (`Nummernkreis.IstSqlFehler`), siehe §38.

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

### `Contains` ueber eine lange Liste: EF 10 uebersetzt zu EINEM `OPENJSON`-Parameter — gemessen, nicht angenommen
SQL Server erlaubt hoechstens **2100 Parameter** je Befehl. Eine `set.Contains(x)`-Abfrage ueber alle
sichtbaren Strukturen einer Seite (Seitengroesse „Alle" = bis 5000) waere als IN-Liste mit Einzelparametern
tot. **Gemessen 2026-09-24** (`WorkOperationRepositoryFaStructureTests.OrderNumbersWithWorkOperationsQuery_
TranslatesContainsAsSingleParameter`, SqlServer-Provider mit der Konfiguration aus `Program.cs`, nie
verbunden): 5000 Schluessel → `DECLARE @set nvarchar(max) = N'[...]'` + `IN (SELECT … FROM OPENJSON(@set))`
— **ein** Parameter. **Warum als Test statt Annahme:** EF 10 hat die Uebersetzung parametrisierter
Collections gegenueber EF 8/9 geaendert (Mehrfach-Parameter-Modus fuer kleinere Listen); welcher Weg bei
grossen Listen greift, haengt an Provider-Version und Optionen. Wer `UseSqlServer(...)`-Optionen (z. B.
Compatibility-Level oder Parameter-Uebersetzungsmodus) aendert, muss diesen Test gruen halten — er ist der
Waechter fuer jede Listen-`Contains`-Abfrage mit Seitengroessen-Bezug.

### Parallele Formular-Arrays: `int?[]` traegt leere Felder, `int[]` nicht — gemessen, nicht angenommen
Formulare mit parallelen Arrays (`itemIds[]` + `quantitiesPicked[]`) muessen leere Eingaben an ihrem Index
behalten, sonst verschiebt sich die Zuordnung. **Gemessen 2026-09-29** (`NullableIntArrayBindingTests`,
echter `ParameterBinder` + `FormValueProvider` aus `AddControllers()`): `int?[]` bindet `["5","","7"]` zu
`[5, null, 7]`. Bis v1.30.0 hat die Lagerbestellung das Problem anders „geloest" — der Client schickte
leere Felder als `'0'` — und damit jede nicht gezaehlte Position still als 0 gespeichert (Autosave) bzw.
gebucht (Abschluss). **Warum als Fallstrick:** Der Workaround sah wie eine technische Notwendigkeit aus
(Kommentar „Binder skippt leere Strings") und war in Wahrheit eine fachliche Falschbuchung. **Regel:**
Mengen, die „nicht erfasst" kennen muessen, als `int?[]`/`decimal?` binden; nie im Client auf `0`
normalisieren. **Aber:** Ein **nicht parsbarer** Wert (z. B. `1.5` fuer `int?`) wird vom Binder **verworfen**, nicht als
`null` gebunden — gemessen `["5","1.5","7"]` → `[5, 7]` + `ModelState`-Fehler. Wer parallele Arrays bindet, muss
deshalb `ModelState.IsValid` **und** gleiche Laengen pruefen, sonst verrutschen alle Folgewerte still
(Lagerbestellung: Guard in `Close`/`PrintAndClose` seit v1.30.1). **Testmittel:** `Microsoft.AspNetCore.TestHost` ist **nicht** Teil von
`Microsoft.AspNetCore.App` (eigenes NuGet) — Bindung deshalb ueber `ParameterBinder` gegen die
Parameter-Metadaten der echten Action testen (Vorlage `IdealAkeWms.Tests/ModelBinding/`).

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
Index-DDL.** Der OrderNumber-Fix (`b1153eb`) legte in derselben Migration einen **zweiten** 3701 frei:
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

### Stueckliste: Matchcode an IDEAL aus der Struktur, sonst Artikelstamm (seit v1.62.0)

Die Stueckliste zeigt an IDEAL `FaHierarchyNode.Matchcode` (ueber `FaHierarchyBomItem.Matchcode`, getrimmt, leer = null), sonst
`Article.Matchcode` ueber `MatchcodeLookup` mit der **Ressourcenummer** (nie die Kopf-Artikelnummer). AKE nur Artikelstamm.
**Warum Variante B** (Gerald 2026-10-16): Stueckliste und Kommissionierliste/Vormontage/Beschichtung sollen an IDEAL denselben
Kurznamen zeigen; die FA-Listen bleiben beim Artikelstamm (bewusste Inkonsistenz wie oben). `BomItem` bekam **keine** Property
(`SqlQueryRaw<BomItem>`, §10). Drei Projektionsstellen: `PickingController.Bom`, `RenderPrintBomAsync`, `ReadOnlyBomBuilder`.

## 13. IDEAL — FA-Arbeitsgang-Erkennung aus der Struktur (Arbeitsschritte, v1.41.0) — **teilweise überholt (v1.44.0)**

> **Stand v1.44.0 (§16):** Ziel-Tabelle und Katalog haben sich geändert — nicht mehr `FaWorkStep` +
> `WorkStep.Code`-Katalog + Zwei-Lauf-Ablauf, sondern **`WorkOperation`** + Katalog
> `ProductionWorkplace.ArbeitsschrittCode` (aus Sage angelegt, **kein** manueller Katalog, kein
> Zwei-Lauf-Ablauf). Der **DirectChildren-Scope** (erster Unterabschnitt unten) gilt unverändert weiter;
> die Katalog-/`SearchString`-Absätze beschreiben nur noch die abgelöste v1.41.0-Mechanik. Details §16.

### Dieselbe `Arbeitsschritte`-Zeile zaehlt in ZWEI Scopes — Absicht, kein Doppelzaehl-Fehler (gilt weiter)
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

> [!warning] Entschieden 2026-10-07 (v1.47.4): der Doppel-Kontext war NICHT gewollt
> Der Fachbereich hat die Gegenpruefung gemacht: Ein Kuerzel auf einer Zeile, die **selbst ein Sub-FA** ist,
> gehoert **nur** zu diesem Sub-FA (Beispiel `VM` an der Mittelholmblende, nicht am Korpus). Scope jetzt:
> eigene Zeile + direkte **Blatt**-Kinder (`SubFA == 0`) — `WorkOperationStructureDetectionService`,
> `kids.Where(k => k.SubFA == 0)`. **Warum es weh tat:** Am BDE-Terminal standen dann zwei Arbeitsgaenge mit
> identischer Beschriftung (`OrderNumber / OperationNumber`, `OrderNumber` = HauptFA) zur Wahl, und der am
> Vater gehoerte zu keinem echten Arbeitsschritt — eine Buchung darauf landete am falschen Auftrag.
> **Lehre:** Eine „mechanische Konsequenz einer bestaetigten Regel" ist noch keine fachliche Bestaetigung —
> was als „zur Sichtung vormerken" dokumentiert wurde, gehoert vor dem ersten Terminal-Einsatz entschieden.
> **Altbestand:** Die Erkennung loescht nie; die vor v1.47.4 angelegten Vater-Arbeitsgaenge entfernt das
> Einmal-Skript `SQL/Einmalig/2026-10-07_v1.47.4_RemoveParentWorkOperationsFromChildSubFaTokens.sql`
> (nur ungebuchte; erst NACH dem Service-Deploy, sonst legt der alte Lauf sie wieder an).
> Spec: FREIGABE-NACHTRAG (2026-10-07) in [[2026-09-21-ideal-bde-arbeitsgaenge-aus-struktur-spec]].

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

### Volle Liste in einer begrenzten Zelle: `d-none` fuer Filter, `data-csv-value` fuer CSV (seit v1.62.0)

Die Lagerplatz-Zelle zeigt hoechstens 3 Plaetze + „+n weitere“ (Popover). Filter (`table-filter.js`), Sortierung und die
Leerzustand-/Aufklapp-Pruefungen der Stueckliste lesen `cell.textContent` — das schliesst versteckte Elemente ein. **Darum**
steht die volle Liste als `<ul class="d-none">` in derselben Zelle (kein Eingriff in den gemeinsamen Filter-Kern) und die CSV
liest `data-csv-value` am `<td>` (`csv-export.js` vor dem Zelltext). Grenze: „weitere“ ist ebenfalls Zelltext. Das Popover
haengt am `body` (`container: 'body'`), sonst beschneidet `.table-responsive` es; seine Initialisierung steht **vor**
`if (readOnly) return;`, damit die Read-only-Stuecklisten es auch haben (Waechter `BomLagerplatzZelleTests` misst die
Anweisung per Regex — ein `IndexOf` traf zuerst einen Kommentar mit demselben Text).

### Stuecklisten-Zellen nie ueber feste Positionen lesen (seit v1.62.0)

`Bom.cshtml` liest Zellen per `bomColKeyToIndex` (col-key), nie per `cells[n]`. **Warum:** Die neue Spalte Matchcode verschob
den Bulk-Bestell-Fallback (`cells[4]/[6]/[8]`) — „Menge“ las den Matchcode (NaN). Spalten kommen bedingt (hierarchisch, ReadOnly,
Gates) und per Zahnrad umsortiert hinzu; nur der col-key ist stabil. Waechter `BomLagerplatzZelleTests.Zellzugriff_NurPerColKey_KeinFesterIndex`.

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

## 16. IDEAL — DREI Sage-Vokabulare, und der BDE-Arbeitsgang lebt in `WorkOperation` (v1.44.0)

### Drei leicht verwechselbare Sage-Vokabulare — unterschiedliche Quelle, unterschiedliches Zielfeld
Bei IDEAL treffen drei kurze, alle aus Sage stammende Codes aufeinander, die **keine** hierarchische
Beziehung zueinander haben:

| Begriff | Sage-Quelle | Bedeutung | WMS-Zielfeld |
|---|---|---|---|
| **Arbeitsbereich** (`K-02`, `S-01`) | `USER_OSAbteilung` | **Zielort** (wohin das Teil kommt) | `ProductionOrder.ProductionWorkplaceId` via Materialisierung — **aktuell nicht abgeleitet**, siehe §15 |
| **Arbeitsschritt** (`KA`, `SW`, `LS`) | `FaHierarchyNode.Arbeitsschritte` | **Arbeitsgang** am Teil (Struktur-Token) | Match-Token für `WorkOperation` |
| **Werkbank** = Sage-**Arbeitsplatz** (`2300` „Kanterei W1") | `KHKPpsArbeitsplaetze` | **Arbeitsplatz**, an dem der Arbeitsgang läuft | `ProductionWorkplace` (Anlage-Ziel), `WorkOperation.ProductionWorkplaceId` |

**Merke:** Arbeitsbereich ≠ Werkbank (das war der August-Fehler, §15). Und der **Arbeitsschritt** ist der
Klebstoff: das Kürzel `USER_ArbeitsSchritt` steht **sowohl** am Sage-Arbeitsplatz (`KHKPpsArbeitsplaetze`,
→ `ProductionWorkplace.ArbeitsschrittCode`) **als auch** als Struktur-Token (`FaHierarchyNode.Arbeitsschritte`)
— darüber matcht Baustein (b) Token→Werkbank.

### Der BDE-Arbeitsgang ist `WorkOperation`, nicht `FaWorkStep` — das war der Kern-Irrtum von v1.41.0
Das BDE-Terminal (`BdeApiController`, `BdeScanResolver`, `WorkOperationRepository.GetOpenByWorkplaceIdAsync`)
bucht ausschließlich gegen **`WorkOperation`** und filtert über `WorkOperation.ProductionWorkplaceId`.
`FaWorkStep` (die FA-**Abarbeitungsliste**) hat **keinen** Fremdschlüssel dorthin und erscheint am
Terminal **nie**. v1.41.0 legte struktur-abgeleitete Arbeitsgänge als `FaWorkStep` an → am Terminal
unsichtbar. v1.44.0 legt sie als `WorkOperation` an (`OperationNumber` = Kürzel, `ProductionWorkplaceId`
= Id der per Kürzel gematchten Werkbank). **Kein Katalog-FK** an `WorkOperation` → der Zwei-Lauf-Ablauf
von v1.41.0 entfällt; sobald Baustein (a) die Werkbank kennt, legt Baustein (b) den Arbeitsgang im selben
Zyklus an.

### `ProductionWorkplaceId` wird beim Anlegen materialisiert — Umzug routet Altbestand nicht um
`WorkOperation.ProductionWorkplaceId` wird **beim Insert** gesetzt (Id der gematchten Werkbank). Ordnet
Sage ein Kürzel später einer anderen Werkbank zu, routen **bereits angelegte, offene** Arbeitsgänge
weiter zur **alten** Werkbank; nur neue Sub-FAs gehen zur neuen. Bewusst (Stammdaten ändern sich selten,
jede Abweichung wird gemeldet), kein Fehler.

### Existenz-Check des Default-AG über `OperationNumber "01"`, nie über den Namen
`BdeDefaultWorkOperationService.FindOrCreateDefaultAsync` prüft, ob schon ein Default-AG existiert. Echte,
struktur-abgeleitete Arbeitsgänge tragen `Name = ProductionWorkplace.Name`; der Default trägt `Name =
BdeDefaultArbeitsgang`. Sind beide zufällig gleich (jemand setzt `BdeDefaultArbeitsgang` = Werkbank-Name),
fände ein **Name**-Vergleich den echten AG und bucht eine NurFA-Buchung still darauf. Seit v1.44.0 geht
der Check über `OperationNumber == "01"` (der Default-Marker) — echte AGs nutzen das Kürzel als
`OperationNumber`, das trennt sauber (AK 16).

### Seed-Beschreibung > Spaltenlänge = Startup-Crash (SqlException 2628), von InMemory nicht gefangen
`ServiceSettingDefinitions.All` wird beim App-Start in `ServiceSettings` geseedet; die Spalten sind
`Key`/`Category` **NVARCHAR(100)**, `Value`/`Description` **NVARCHAR(500)** (`ApplicationDbContext`). Eine
zu lange `Description` (hier 569 Zeichen bei `Sync:WorkOperationStructureDetectionEnabled`, v1.44.0) lässt
das Seeding mit **`SqlException 2628` (String truncation)** abbrechen — die Web-App startet nicht. EF
**InMemory erzwingt keine Längen**, also fällt es in keinem Unit-Test auf, sondern erst am echten SQL
Server. Abgesichert seit v1.44.0 durch `ServiceSettingDefinitionsTests.All_FieldsFitServiceSettingsColumns`
(reiner Katalog-Längen-Check gegen die bekannten Spaltengrenzen). **Merke:** neue/geänderte
ServiceSetting-Beschreibung immer ≤500 Zeichen halten.

### Was nur Manual-UAT prüfen kann (nicht InMemory): Ausschlussliste, eindeutiger Index, Sage-Read
Wie überall in dieser Familie: der Sage-Read (`KHKPpsArbeitsplaetze`, raw ADO.NET), die Wirkung der
Ausschlussliste (Liste in der ServiceSettings-DB, nicht über `IConfiguration` injizierbar) und der
eindeutige gefilterte Index auf `SageArbeitsplatznummer` (InMemory erzwingt UNIQUE nicht) sind **nicht**
InMemory-testbar. Deren AKs (10, 20) stehen in TS-77 ausdrücklich als **Manual-UAT** — nicht unter die
grünen Unit-Tests zählen.

### Die View `vw_IDEAL_AKE_WMS_Arbeitsgaenge` ist der Arbeitsgang-KATALOG, nicht die FA-Arbeitsgaenge (seit v1.60.0)
Die View ersetzt die Tabelle `KHKPpsArbeitsplaetze` und liefert die IDEAL-Arbeitsgaenge. Bei IDEAL ist ein
Arbeitsgang ein Sage-Arbeitsplatz und der ist im WMS die Werkbank (`ProductionWorkplace`, `AG` ->
`ArbeitsschrittCode`; Begriffsklaerung [[2026-09-21-ideal-bde-arbeitsgaenge-aus-struktur-spec]], Gerald 2026-10-15).
Die BDE-Arbeitsgaenge je FA (`WorkOperation`, siehe oben) entstehen daraus erst ueber die Struktur-Erkennung —
die View hat keinen FA-Bezug. Sie ist deshalb die Quelle der Werkbank-Anlage (`Sync:ProductionWorkplaceSyncViewName`),
nie ein direkter Schreibweg nach `WorkOperation`.
**Warum der Default die alte Tabelle bleibt:** Der Web-Start seedet einen NEUEN Key mit seinem Default in jede
bestehende DB (`Program.cs:453-468`). Ein View-Default haette IDEAL beim Deploy umgeschaltet, bevor P3 belegt,
dass die View die Arbeitsplatznummern identisch formatiert — sonst legt der Abgleich (Ordinal auf
`SageArbeitsplatznummer`) neue Werkbaenke an. Umstellen von Hand, erst nach P3.
[[2026-10-14-ideal-sage-views-umstellung-spec]]

### Spaltennamen einer Sage-View nie aus einer Antwort ableiten — P4 vor dem Umstellen (v1.60.1)
v1.60.0 las Geralds Antwort „die Spalten sollten gleich sein“ als „gleich wie der alte Sage-Select“ und baute den
View-Select mit `Artikelnummer`, `Bezeichnung1` … Die View `vw_IDEAL_AKE_WMS_Artikel` liefert aber schon die
**WMS-Namen** (`ArticleNumber`, `Description` …) — der erste Lauf scheiterte mit „Ungueltiger Spaltenname“. **Warum
das zaehlt:** Die View-Definition liegt nicht im Repo; „gleich“ ist mehrdeutig (gleich wie Sage oder wie das WMS).
Spaltennamen kommen nur aus `INFORMATION_SCHEMA.COLUMNS` (Pruefabfrage P4); vorher die Quelle nicht umstellen. Der
laute Fehlerpfad (Fehler + Mail, keine Mutation) hat hier gegriffen — deshalb nie einen stillen Rueckfall einbauen.
**Nachtrag v1.60.2 — gilt fuer alle vier WMS-Views:** Dasselbe passierte bei `vw_IDEAL_AKE_WMS_Arbeitsgaenge` (angenommen:
Spalten und Mandant/Aktiv wie `KHKPpsArbeitsplaetze`; tatsaechlich nur `Arbeitsgangnummer`, `Bezeichnung`, `AG`). Regel:
**View-Spaltennamen nie aus Annahmen** (weder aus einer Antwort noch aus der Tabelle, die die View ersetzt) — vor jeder
Umstellung `sys.columns`/`INFORMATION_SCHEMA.COLUMNS` fuer FAListe, FAInfos, Arbeitsgaenge **und** Artikel holen und mit den
gelesenen Spalten abgleichen (Liste in der Spec, Abschnitt „Nachbesserung v1.60.2“).

## 17. IDEAL — Kommissionierung nur am HauptFA (v1.46.0)

### Es gibt genau EINE Sub-FA-Definition: `ProductionOrder.IsHauptFa`
`IsHauptFa` (`Expression<Func<ProductionOrder,bool>>`: `SubOrderNumber == "" || SubOrderNumber == OrderNumber`)
ist die einzige Quelle. Abgeleitet: `IsSubFa` (kompiliert, für geladene Entitäten), `IsSubFaOf(order, sub)`
(für Projektionen wie `LeitstandOrderRow`; `null` wie `""`) und `.Where(ProductionOrder.IsHauptFa)` in
EF-Abfragen. **Warum:** Vor v1.46.0 gab es mindestens fünf eigene Fassungen mit abweichender Leer-Behandlung
(`_ProductionOrderRow` `!IsNullOrEmpty && ==`, `Bom.cshtml` `!IsNullOrEmpty && !=`, `BomScopes.ForOrder`
`string.Equals`, `HierarchicalDataExistsAsync` `Order != Sub`) — `""` war je nach Stelle HauptFA oder Sub-FA.
Ein Test-Fixture (`ReadOnlyBomBuilderTests`) hatte sich unbemerkt auf „`""` = Sub-FA“ verlassen.
**Merke:** Wer „ist das eine Sub-FA?“ fragt, nimmt `IsSubFa`/`IsSubFaOf`/`IsHauptFa` — nie wieder einen
eigenen Vergleich schreiben. **Grenze:** InMemory-Tests belegen nur C#-Semantik; SQL Server vergleicht
case-insensitiv und ignoriert Leerzeichen am Ende — folgenlos, weil beide Spalten aus demselben Wert stammen.

### Komm.-Ziel-Zelle der Stückliste darf keinen Platzhalter bekommen
`Views/Picking/Bom.cshtml` rendert `<td>@item.Kommissionieren</td>` — leer bleibt leer. Der Filterwert
`!(leer)` („Alle Ziele“) prüft in `bomMatchesFilter` `text.trim() !== ''` auf dem **gerenderten Zelltext**.
**Warum eine Falle:** Setzt jemand für leere Werte „–“ o. ä. in die Zelle, gilt jede Zeile als
kommissionier-relevant — „Alle Ziele“ filtert dann still nichts mehr aus. Dieselbe Semantik hat
`KommissionierRelevanzFilter.IsRelevant` (C#, Kommissionierliste) — beide gemeinsam ändern.

### `!(leer)` ist ein Vorgriff auf die Suchsyntax — `bomMatchesFilter` ist ihre VIERTE Umsetzung
Die Filter-Mini-Syntax lebt in `table-filter.js`, `ColumnFilterHelper.Apply`, den SQL-Ausdrücken je Liste
**und** in `bomMatchesFilter` (Bom.cshtml). Der `!(leer)`-Sonderfall steht nur dort (vor der `!`-Ausschluss-
Syntax, sonst hieße `!(leer)` „enthält nicht ‚(leer)‘“ und zeigte alles). **Merke:** Beim Bau von
[[2026-09-23-suchsyntax-spaltenfilter-erweitern]] muss `bomMatchesFilter` samt Sonderfall in den gemeinsamen
Parser aufgehen. Gespeicherte Profilwerte normalisiert `KommissionierzielFilterWert.Normalize` beim Speichern
(`!(leer)` exklusiv; Hinweis nur, wenn Einzelziele verworfen wurden).

### Bootstrap-`d-*` schlägt das `hidden`-Attribut — global in `site.css` korrigiert (v1.46.0)
`.d-flex{display:flex!important}` steht in `bootstrap.min.css` **nach** `[hidden]{display:none!important}`
(gleiche Spezifität) — ein Element mit `class="d-flex …" hidden` bleibt sichtbar. So war die gelbe
Komm.-Ziel-Leerzustand-Box der Stückliste seit v1.42.0 **immer** zu sehen (nachgeholtes Code-Review
2026-09-25). **Lösung:** `[hidden] { display: none !important; }` in `wwwroot/css/site.css` (lädt nach
Bootstrap). **Merke:** Wer ein Element per `hidden` steuert, darf sich darauf jetzt verlassen — die Regel
nicht entfernen; alternativ `d-none` togglen.

### Freitext nie per `'@Html.Raw(...)'` in ein JS-Literal
Benutzer-Standardfilter (bis 200 Zeichen, admin-editierbar für andere Benutzer) liefen in `Bom.cshtml` roh in
einfache Anführungszeichen — ein Apostroph (`KA'02`) brach den Init-Handler, ein präparierter Wert wäre
ausgeführt worden (Stored XSS). **Muster:** `@Html.Raw(System.Text.Json.JsonSerializer.Serialize(wert ?? ""))`
ohne umgebende Quotes. Offene Restfundstelle: [[2026-09-25-js-escaping-locationtransfer]].

## 18. IDEAL — Kommissionierliste summiert (v1.47.0)

### `FaHierarchyNode` hat KEIN Mengeneinheit-Feld — „nur gleiche Einheiten summieren“ ist nicht prüfbar
Die Summiert-Ansicht der Kommissionierlisten (`KommissionierListenService.AggregateSummiert`) summiert
`Sollmenge` je `(HauptFA, Artnr, Kommissionieren)`. `FaHierarchyNode` — Projektion der IDEAL-Sage-View
**`vw_IDEAL-AKE_Kommissionierung_FAListe`** (`FaHierarchyNode.cs`, `FaHierarchySyncService.cs`) — trägt
**keine** Einheit; `Sollmenge` ist eine nackte `decimal`. **Nicht verwechseln:** `KHKPpsRessourcenPositionen`
ist die **AKE**-Stücklistenquelle (`SageImportService`, `SQL/AgentJobs/02_Import_Artikel.sql`), nicht die
IDEAL-Quelle — die ursprüngliche Rückfrage in [[2026-09-25-kommissionierliste-summierung-pdf-spec]] zielte
darauf und wurde korrigiert.
**Warum trotzdem keine Einheiten-Prüfung gebaut ist:** Die Mengeneinheit ist in Sage eine
**Artikeleigenschaft** (`KHKArtikel.Lagermengeneinheit`, im WMS `Article.Unit`), und der Summierschlüssel
enthält die Artikelnummer — zwei Zeilen desselben Schlüssels sind derselbe Artikel mit derselben Einheit.
Das gilt **nur**, solange die Positionstabelle hinter der View keine eigene, vom Artikel abweichende
Positionseinheit führt. **Belegpflicht vor der Abnahme (TS-80.1):** auf der Sage-DB IDEAL
`EXEC sp_helptext 'vw_IDEAL-AKE_Kommissionierung_FAListe'` (welche Tabelle liefert `Sollmenge`?) und
`SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '<Positionstabelle>' AND COLUMN_NAME
LIKE '%einheit%'`. Kein Treffer ⇒ strukturell erledigt. Treffer ⇒ **melden, nicht still mitlösen** — die
Spalte gehört dann in den Summierschlüssel (eigener Nacharbeitspunkt). Bis zum Beleg gilt dieser Eintrag
unter dieser Bedingung. Das Risiko ist nicht neu: dieselbe Summierung besteht seit v1.31.0.

## 19. Spaltenauswahl (Zahnrad) — Platzierung (v1.47.3)

### Zahnrad-Host muss direkter Vorgänger von `.table-responsive` sein
`column-preferences.js` `insertGearButton` hängt das Zahnrad an `tableWrapper.previousElementSibling`, sofern
das ein `DIV` ist — egal, ob dieses Element sichtbar ist oder was es sonst bedeutet. Nur wenn es keinen
Vorgänger gibt, baut es sich einen eigenen Container.
**Warum das eine Falle ist:** Die Regel steht nur im Kommentar am Toolbar-Host (`Views/Picking/Bom.cshtml`).
Mit v1.42 ([[2026-09-18-stueckliste-kommissionierziel-filter-spec]]) kamen `#bomDefaultFilterBadges` und
`#bomKzEmptyState` (`hidden`) **zwischen** Host und Tabelle; das Zahnrad landete im versteckten Leerzustand und
war unsichtbar (Befund [[2026-10-06-stueckliste-zahnrad-bug-matchcode]]). Durch den globalen
`[hidden]{display:none!important}` (§17) war der Hinweis wirklich unsichtbar, kein Restschimmer.
**Regel:** Neue Hinweise, Badges, Alerts **über** den Host setzen, nie zwischen Host und `.table-responsive`.
Ein bedingtes/verstecktes DIV direkt vor der Tabelle ohne Host verschluckt das Zahnrad ebenso.
Wächter nur für die Stückliste: `BomZahnradHostTests` (Quelltext). Weitere Fundstelle derselben Klasse, nicht
behoben: `Views/WarehousePicking/Details.cshtml` (`#incomplete-alert` mit `d-none` direkt vor der Tabelle;
`textContent = …` überschreibt beim Einblenden zusätzlich das Zahnrad).
**Nachtrag v1.47.5:** Lagerbestellung-Details behoben — Host direkt vor der Tabelle, `#incomplete-alert`
darüber; Wächter `WarehousePickingDetailsZahnradHostTests` (gleicher Helfer wie `BomZahnradHostTests`). Auf `main` als
AKE-Hotfix v1.30.2 (`0a6fb3cd`), Wächter-Datei wortgleich, damit der Vorwärts-Merge sie konfliktfrei übernimmt.
**Nachtrag v1.65.0 — Regel „direkt vor `.table-responsive`“ überholt** ([[2026-10-14-spaltenauswahl-zahnrad-sammel-spec]]):
Die Nachbar-Heuristik steckte doppelt in `insertGearButton` und `csv-export.js attach`. Ersetzt durch `site.js`
`window.tableToolbarHost(table)` in dieser Reihenfolge: (1) Element mit `data-table-toolbar` direkt vor dem Wrapper, (2) ein **expliziter**
Marker der View irgendwo im Dokument, (3) sonst ein neuer Host direkt vor dem Wrapper mit `data-table-toolbar="auto"`.
**Warum der Fallback `="auto"` heißt und Stufe 2 ihn nicht findet:** `BdeMasterData/Index` hat drei `filterable-table` auf einer Seite.
Bei rein dokumentweiter Suche fänden die zweite und dritte Tabelle den Host der ersten, alle CSV-Knöpfe stünden über Reiter 1.
**Regel jetzt:** Hinweise dürfen vor oder hinter einem Marker stehen. Einen expliziten Marker gibt es höchstens einmal je View, nie an
einem `hidden`/`d-none`-Tag. Brauchen zwei Tabellen einer Seite je einen eigenen Marker, muss Stufe 2 relativ zur Tabelle suchen
(`ponytail:`-Kommentar in `site.js`). Wächter `TableToolbarHostTests` (ersetzt alle `*ZahnradHostTests`).

## 20. IDEAL — Kind-Sub-FA zählt nur für sich (Lack-Flag, v1.47.5)

### Ein beschichteter Kind-Sub-FA markiert seinen Vater-Sub-FA NICHT
`FaMaterializationCoating.Derive` setzt `HasCoatingParts` eines Sub-FA, wenn er **selbst** `Beschichtet` ist
oder ein direktes **Blatt**-Kind (`SubFA == 0`) beschichtet ist. Eine Kind-Zeile mit eigenem Sub-FA zählt nur
für ihren eigenen Sub-FA. Bis v1.47.4 zählte jedes direkte Kind — ein lackierter Kind-Sub-FA (z. B. eine Blende)
kennzeichnete damit auch den Korpus.
**Warum:** Zum Lackierer gehen die Teile, nicht der Korpus (Entscheidung Gerald 2026-10-07). Das falsche Flag
erzeugte am Vater einen Beschichtungstermin und eine „Beschichtung erledigt“-Checkbox in FA-Liste,
Leitstand und FA-Abarbeitungsliste (`ProductionOrdersController.cs:209/226`, `CoatingDateCalculator` über
`PickingLeitstandController.cs:172`, `FaWorklistController.cs:273`) — Planungsrauschen ohne echten Arbeitsschritt.
Gleiche Regel und gleicher Ursprung wie die Arbeitsgang-Erkennung in §-Eintrag „Entschieden 2026-10-07
(v1.47.4)“ ([[2026-10-07-v1-47-4-arbeitsgang-erkennung-kind-sub-fa]]): Was in der Struktur ein eigener Sub-FA
ist, wird nie dem Vater zugeschlagen. **Neue Ableitungen aus der FA-Struktur** (Kennzeichen, Kürzel,
Mengen) müssen diese Abgrenzung von Anfang an übernehmen.
**Altbestand:** kein Skript nötig — die Materialisierung schreibt das Flag in **jedem** Lauf für alle
materialisierten Sub-FAs neu (`FaMaterializationSyncService` → `SetCoatingPartsAsync`). Kippt es auf `false`,
setzt die Repository-Methode `IsCoatingDone` mit zurück (Fallstrick #11). Nicht neu berechnet wird: im
DryRun, bei übersprungenem Plan, bei einem Abbruch des Laufs vor dem Schreibschritt und für Sub-FAs, die nicht
mehr in `FaHierarchyNodes` stehen (vermisst, nicht in `matKeys`) — dort bleibt das alte Flag stehen. Der Beschichtungsauftrag (`FaHierarchyBeschichtung`) liest `FaHierarchyNode.Beschichtet`
direkt und ist nicht betroffen.
Spec: FREIGABE-NACHTRAG (2026-10-07) in [[2026-08-20-materialisierung-fachliche-felder-spec]].

## 21. Git/Windows — `docs/TESTSZENARIEN.md` landet als CRLF im Commit (2026-10-08)

### Vor jedem Commit dieser Datei den Index-Blob byte-genau prüfen
Mit `core.autocrlf=true` (System-Gitconfig) liegt der Arbeitsbaum in CRLF vor. Für die meisten Dateien normalisiert
`git add` wieder auf LF — für `docs/TESTSZENARIEN.md` **nicht**: Zweimal (v1.47.5 `7e78f9a6`, AKE-Hotfix v1.30.2 vor
dem Commit abgefangen) landete die ganze Datei als CRLF im Index. Ursache nicht geklärt (kein NUL, kein einzelnes
CR, kein `.gitattributes`).
**Warum es weh tut:** Ein Vollwechsel der Zeilenenden macht aus 30 Zeilen Diff 18.000 — Review am Merge-Diff
blind, `git blame` für das ganze Abnahmedokument verloren, Vollkonflikt bei jeder Parallel-Änderung.
**Regel:** Nach `git add docs/TESTSZENARIEN.md` prüfen:
`git show :docs/TESTSZENARIEN.md | python -c "import sys;print(sys.stdin.buffer.read().count(b'

'))"` muss `0`
ergeben, sonst die Datei auf LF umschreiben und neu stagen. **Nicht** mit `grep -c $'
$'` messen — das zählt unter
Git Bash auch bei LF-Blobs jede Zeile (Fehlbefund 2026-10-08: v1.47.3-Commits fälschlich als CRLF gemeldet).
Dauerhafte Lösung (nicht umgesetzt, eigene Entscheidung): `.gitattributes` mit `*.md text eol=lf`.

## 22. Beschichtungsaufträge-Oberfläche (v1.51.0) — Tabellen-JS und Dezimal-Eingaben

### `table-filter.js` und `column-preferences.js` bedienen genau EINE Tabelle je Seite
Beide Skripte halten die Tabelle in einer Modul-Variablen (`_table` in `table-filter.js`) und lesen genau ein
`#column-config`/`#view-config`. Eine zweite `.filterable-table` auf derselben Seite bekommt weder Filterzeile noch
Zahnrad — still, ohne Fehlermeldung.
**Warum es weh tut:** Die Pflicht „Spaltenfilter für jede Tabelle" (ADR 0005) lässt sich so auf Seiten mit zwei
Tabellen nicht erfüllen; wer es trotzdem versucht, bekommt eine scheinbar gefilterte zweite Tabelle, die nicht reagiert.
**Regel:** Je Seite eine filterbare Haupttabelle. Kleine Auswahltabellen daneben (Positionsauswahl im Detail,
Reklamations-Teileauswahl in [[2026-10-06-beschichtungsauftrag-ausbau-teil-4-spec]]) bleiben bewusst ohne Filter und
ohne `filterable-table`-Klasse. Mehrere Tabellen erst nach Umbau der Skripte auf Instanzen (eigene Entscheidung).

### Die Anwendung hat keine feste Request-Kultur — Dezimal-Eingaben selbst lesen
> [!warning] Überholt seit v1.99.23
> `Program.cs` setzt jetzt eine feste App-Kultur `de-DE` (Thread-Default + `UseRequestLocalization` mit genau einer Kultur), siehe §38 und
> [[2026-11-14-schutz-doppel-absenden-und-unsichtbare-positionen-spec]]. Der Text unten beschreibt den Stand bis v1.99.22; `TryMenge`
> (Komma und Punkt) bleibt richtig, die Server-Kultur entscheidet nicht mehr.

`Program.cs` konfiguriert weder `UseRequestLocalization` noch eine Thread-Kultur (Grep `Culture|Localization`: kein
Treffer). Das Model-Binding parst `decimal` deshalb mit der Kultur des Servers (IIS-Konto) — „2.5" aus einem
`type="number"`-Feld wird unter `de-AT` zu 25, „2,5" unter `en-US` zu 25.
**Warum es weh tut:** Mengen landen um Faktor 10/100/1000 falsch in der DB, ohne Validierungsfehler.
**Regel:** Neue Dezimal-Eingaben als `string` binden und kulturunabhängig lesen (Komma → Punkt,
`CultureInfo.InvariantCulture`), Muster `BeschichtungsauftraegeController.TryMenge`; Anzeige ohne Tausenderpunkt
(`0.###`), sonst wird „1.000,5" falsch gelesen. Bestehende Formulare mit `asp-for` auf `decimal` (z. B.
`StockMovements/*`, `Articles/*`) hängen an der Server-Kultur — nicht geprüft, offener Befund der Teil-4-Spec.

## 23. CSV-Export und Agenten-Werkzeuge (v1.53.0)

### Jede Tabellenspalte mit Überschrift braucht `data-col-key`
Der CSV-Export ordnet im Server-Mode die Zellen der nachgeladenen Gesamtliste über `data-col-key` den sichtbaren Spalten zu.
Eine Spalte mit Überschrift ohne Key ist nicht zuordenbar — bis zum QA-Fund 2026-10-12 fiel sie still weg (Lagerbewegungen
ohne „Menge", Artikelgruppen-Zuordnung ohne Gruppen-Spalten); jetzt bricht der Export mit Meldung ab.
**Regel:** neue `<th>` mit Text in einer `filterable-table` immer mit `data-col-key`; reine Aktions-/Haken-Spalten ohne Text
oder mit `data-csv-export="false"`. Wächter `CsvExportCoverageTests.JedeUeberschriebeneSpalte_HatEinenSchluessel`.

### Eine Listen-Action darf im GET-Pfad nichts schreiben
Der Export ruft sie ein zweites Mal mit `pageSize=0` (ADR 0005, Nachtrag 2026-10-12). Prüfung aller 37 Actions: sauber.
Neue Listen: kein Zähler-/„zuletzt gesehen"-/Materialisierungs-Schreiben in `Index`.

### Datei-Schreibwerkzeug der Agenten löst `\uXXXX` auf
Beim Schreiben von JS/C# mit dem Write-Werkzeug wurde `'\uFEFF'` zu einem **unsichtbaren** BOM-Zeichen im Quelltext (ebenso
`\u00a0`). Funktioniert, verschwindet aber beim nächsten Editor, der Steuerzeichen bereinigt. **Regel:** nach dem Schreiben
byte-genau prüfen (`EF BB BF`, `C2 A0` zählen) und als sichtbares Escape einsetzen; Backslash im Skript über `chr(92)` bauen,
nicht über Heredoc-Escapes. *Wieder passiert v1.99.11:* ein doppelter Backslash in einem Bash-Heredoc kam als einfacher an (UNC-Pfade in Testdaten), `\uFEFF` im Write-Werkzeug als echtes BOM – Testdaten mit UNC-Pfaden über das Edit-Werkzeug schreiben und mit `od -c` prüfen.

### Razor: `@` im Hilfetext ist Code
`<code>@</code>` bricht den Build (RZ1005) — `@@` schreiben. Eine Befehlskette `build | grep Fehler && commit` committet
trotzdem, weil `grep` die Fehlerzeile findet: vor dem Commit auf „0 Fehler" prüfen (Zwischen-Commit `400a7520`).

## 24. Arbeitsgang-Stammdaten (v1.54.0) — Tabellen-Umbenennung und zwei Lookups

### Tabellen-Umbenennung: EF-Vorschlag nie uebernehmen, guarded sp_rename je DB-Form
Bei Klassen- **und** Tabellen-Rename erzeugt `migrations add` `DropTable`+`CreateTable` (Datenverlust); bei reinem Rename
`RenameIndex`/`DropPrimaryKey`, die auf Skript-/FreshInstall-DBs den `UQ_`-Constraint statt des `IX_`-Index treffen (§9).
**Regel:** Snapshot/Designer so lassen, wie `migrations add` sie erzeugt; `Up()`/`Down()` komplett durch guarded
`migrationBuilder.Sql` ersetzen (ein Aufruf = ein Batch), Batches fuer Migration und `SQL/XX` aus EINER Quelle schreiben;
Doppelform UQ/IX beim Umbenennen auf die EF-Form konvergieren (erst Index anlegen, dann Constraint droppen). Pruefen offline:
`has-pending-model-changes` leer, `migrations script` ohne `DROP TABLE`/`CREATE TABLE`/ungeguardetes `sp_rename`.
Muster: [[2026-10-09-arbeitsgang-stammdaten-vereinheitlichen-spec]], Drift-Waechter `OperationConfigsMigrationSqlTests` (vergleicht die Batches wortgleich).

### Zwei Offset-Lookups mit unterschiedlicher Gross/Klein-Regel — bewusst
`GetAllAsDictionaryAsync` (OSEON-Teileverfolgung) bleibt **case-sensitiv**, `GetDueDateOffsetsAsync` (FA-Struktur Teil 2/3,
Liste „nicht konfiguriert“) ist **case-insensitiv + getrimmt**. Grund: OSEON-Verhalten sollte bitgleich bleiben. Wer den
OSEON-Pfad angleichen will, braucht eine eigene Entscheidung (Ampeln koennten sich aendern).

### Reiter in einer gegateten Controller-Familie: eigener Controller statt Filter-Ausnahme
Die BDE-Stammdaten haengen am Klassenfilter `[RequireBdeActive]`; der Reiter „Arbeitsgänge“ muss ohne BDE gehen. Loesung:
eigener `OperationConfigsController` (`[RequireAdminAccess]`) + gemeinsames Reiter-Partial `_BdeMasterDataTabs`, nicht
`[RequireBdeActive]` je Action oder ein Ausnahme-Attribut im geteilten Filter (fuenf Controller).

### `CreatedBy = 'system'` ist auf `OperationConfigs` kein Seed-Beleg (seit v1.61.0)

`SQL/95` B8 hat jeder Zeile, die vor v1.54.0 bestand, `CreatedBy = 'system'` eingetragen — auch von Hand gepflegten. Die
Bestandsbefüllung der Quelle (`SQL/96`, Regel „`'system'` → `ExternalSystem`“, Antwort 7 der Spec
[[2026-10-16-arbeitsgang-stammdaten-abgleich-aus-sage-view-spec]]) macht deshalb praktisch den gesamten Altbestand zu
„Fremdsystem“, der nie „nicht in Sage“ gekennzeichnet wird. **Warum so gelassen:** Der Altbestand stammt nicht aus Sage, und die
Datenlage unterscheidet Seed und Handanlage nicht. Wer künftig aus `CreatedBy` eine Herkunft ableiten will: nicht tun — dafür
gibt es jetzt `Source`.

### `Update(entity)` schreibt alle Spalten — bei geteilten Tabellen nur Pflegefelder markieren (seit v1.61.0)

`_context.Set.Update(config)` markiert **jede** Property als geändert und schreibt den Stand beim Laden zurück. Schreibt ein
zweiter Weg (hier der Arbeitsgang-Abgleich: `Source`, `SageMissingSince`) zwischen Laden und Speichern, dreht das Web dessen
Werte still zurück, und der nächste Lauf meldet den Übergang doppelt. **Darum** markiert `OperationConfigRepository.UpdateAsync`
nur die Pflegefelder (`IsModified` je Property, eine Konstante). Der Test dafür braucht zwei Kontexte auf **derselben**
InMemory-DB (`TestApplicationDbContextFactory`); er war gegen das alte `Update()` rot.

### Ausschlussliste fällt im Arbeitsgang-Abgleich bei `null` auf den Katalog-Default (seit v1.61.0)

Die Werkbank-Anlage nimmt bei einem gescheiterten Settings-Read eine **leere** Ausschlussliste — dort harmlos, weil sie
abgleicht. Der Arbeitsgang-Abgleich löscht nie: ein einmal fälschlich angelegtes `STO` bliebe für immer. **Darum** gilt dort
bei `null` der Katalog-Default (`OperationConfigSyncService.DefaultAusschlussliste`, Test vergleicht mit dem Katalog); ein
bewusst leer gesetzter Wert bleibt leer.

## 25. FA-Struktur-Status (v1.55.0) — Zeilenzustände und Statusfarben

### Zeilenzustände nie über `opacity` kennzeichnen
`opacity` wirkt auf **alle** Kinder einer Zeile: Text, Badges und Statusfarben fallen gemeinsam unter WCAG AA (gedimmte
Kontextzeilen beim Baum-Spaltenfilter, Teil-1-QA M1). Seit v1.55.0 trägt die Kontextzeile Form statt Blässe (kursiv +
Randstreifen `#495057` an der ersten Zelle). Neue Zeilenzustände: Grundfarbe oder Form ändern, Deckkraft bleibt 1.

### Status-Chips mit eigenem Grund — Kontrast unabhängig vom Zeilenzustand
Die Arbeitsgang-Chips (`.fa-ag-*`) setzen Text-, Grund- und Randfarbe selbst. So gilt der gemessene Kontrast in normaler,
Hover-, hervorgehobener, Treffer- und Kontextzeile gleich, und ein neuer Zeilen-Hintergrund braucht keine Neuberechnung je
Status. Wer einen Status ohne eigenen Grund einführt, muss alle Zeilengründe nachmessen.

### FA-Baum-Status nur bei BDE an — produktiv derzeit unsichtbar
`FaHierarchyController.Index` fragt Status, Feiertage und Offsets nur bei `BdeAktiv = true` ab. Solange BDE an keinem
Standort an ist, zeigt Produktion keine Status — Abnahme und Fehlersuche am Testsystem mit BDE an.

### Arbeitsgang-Status nur über `ArbeitsgangStatusRegel` (seit v1.57.0)
FA-Baum (Teil 2) und BDE-Terminal (Teil 3) leiten den Status über **dieselbe** Regel ab (`Services/ArbeitsgangStatusRegel.cs`:
Vorrang erledigt → läuft (+ überfällig) → überfällig → offen; Offset getrimmt, ohne Eintrag 0); Klasse/Zeichen kommen aus
`FaAgStatusDarstellung`, die Farben aus den CSS-Variablen `--fa-ag-*` in `site.css`. Wer den Status irgendwo anders zeigt, ruft
diese drei Stellen — keine Kopie, sonst laufen FA-Baum und Terminal auseinander. Bewusste Abweichung: Das Terminal fällt bei
leerem `FE_Termin` auf `ProductionDate` zurück (Antwort 9), der FA-Baum nicht.

### Terminal zeigt fremd gebuchte Arbeitsgänge (seit v1.57.0)
`GetAvailableOperations` blendet nur noch **eigene** laufende und fertig gemeldete Arbeitsgänge aus; von anderen Werkern
gebuchte erscheinen als „läuft“. Die Exklusivität sichert allein `BdeBookingService.StartPlannedAsync` (Rüsten immer,
Produktion nur bei `BdeMehrfachBuchungProArbeitsgang` = aus). Wer die Startprüfung lockert, öffnet damit Doppelbuchungen aus der Liste.

## 26. Spaltenfilter-Suchsyntax (v1.58.0/v1.58.1) — ein Kern, Typ an zwei Stellen

### Spaltenfilter nie selbst auswerten — Kern nutzen
Die Bedeutung eines Filterwerts steht nur in `Services/ColumnFilterSyntax.cs` und `wwwroot/js/column-filter-syntax.js`, beide gegen
`IdealAkeWms.Tests/Js/column-filter-cases.json` geprüft ([[0017-ein-parser-fuer-die-spaltenfilter-syntax]]). Neue Listen geben nur
Getter/Selektoren und den Typ an den Kern (`ColumnFilterHelper.Apply`, `ColumnFilterSql.Predicate`, `ColumnFilterSyntax.Matches`) —
kein eigenes `split(',')`, kein `EF.Functions.Like`, kein `tokens.Any(...)`. **Warum:** Vor v1.58 gab es sechs Umsetzungen mit eigener
Bedeutung (Fehlteile werteten `!` je Wert), und `_`/`%` gingen unmaskiert in `LIKE`.

### Typ an `<th>` UND im Register
Zahl-/Datumsspalte = `<th data-filter-type="number">` bzw. `<th data-date-filter>` **und** Eintrag in `ColumnFilterTypes.Lists`
(View-Pfad) **und** `ColumnFilterTypes.For("<View-Pfad>")` an `Apply`/Postfilter. Fehlt eine Stelle, markiert der Browser einen
Vergleich anders als der Server filtert — ohne Meldung (QA-Befund I-1 aus Etappe A). Der Drift-Wächter schlägt an.

### Getter rendern wie die Zelle
Vergleiche lesen die Zahl/das Datum aus dem **gerenderten** Text (deutsches Format). Ein Getter mit `InvariantCulture` („2.5“) wird
als 25 gelesen. Getter typisierter Spalten deshalb exakt wie die Zelle formatieren (Beispiel FaWorklist/FaCompletion `quantity`).

### Groß/Klein in SQL macht die Kollation — Tests unter InMemory brauchen passende Schreibweise
Der SQL-Bauer setzt kein `LOWER` (Index für „beginnt mit“). Die CI-Kollation ist damit Pflicht (Prüfschritt P-2, Datenbank und je
Spalte, beide Server). EF-InMemory vergleicht ordinal und der Parser schreibt klein — Testdaten für SQL-Filter in Kleinschreibung
anlegen (10 Bestandstests wurden so angepasst).

### §17 erledigt
Der `!(leer)`-Sonderfall in `bomMatchesFilter` (§17, „VIERTE Umsetzung“) ist seit v1.58.1 aufgegangen: die Stückliste nutzt den Kern,
`!(leer)` ist dort ein normaler Filterwert.

## 27. Fußzeile auf jeder Druckseite (v1.59.0) — nur Bauweise V3 trägt

### Warum nicht in den `@page`-Rand (V1)
Ein `position: fixed`-Element mit negativem `bottom`, das in den `@page`-Unterrand ragen soll, zeichnet Chromium im
Headless-PDF **oben** auf jeder Seite über den Inhalt; der Hintergrund-Balken fehlt. Fixed-Elemente leben im Seitenbereich,
nicht im Rand (gemessen 2026-10-14, Edge 155).

### Warum kein Platzhalter im `tfoot` (V2)
Ein wiederholter `tfoot`-Platzhalter schützt nur Tabellenzeilen. Alles **nach** der Tabelle (Bemerkung, Banner) lief unter
die Fußzeile (−12,5 mm). AK „nichts unter der Fußzeile" ist damit nicht haltbar.

### Was trägt (V3)
`@page`-Unterrand klein (5 mm), Fußzeile `fixed; bottom: 0` mit fester Höchsthöhe und `overflow: hidden`, und jedes
Dokument (`.fa-group`) bekommt `padding-bottom` mit `box-decoration-break: clone` — das Polster wiederholt sich auf jedem
Seitenfragment, also auch nach Bemerkungen. Die Fußzeile darf nie höher als das Polster minus Abstand sein, sonst
überdeckt sie Inhalt → deshalb hart gekappt (17,75 mm bei 19,75 mm Polster).

### `:last-child` bricht, sobald hinter den Dokumenten etwas steht
Die Druck-Views hatten `.fa-group:last-child { page-break-after: auto }`. Hängt hinter der letzten Gruppe ein weiteres
Element (hier der `<footer>`), greift `:last-child` nicht mehr → leere letzte Seite. Darum `.fa-group:last-of-type` (zählt
nur `div`) — und hinter die Gruppen nie ein `div` hängen.

### Seitenzähler und Sammeldruck
`counter(pages)` zählt die ganze Druckseite, nicht ein Dokument. Bei mehreren HauptFA in einem Druck wäre „Seite 3 von 7"
falsch → Zähler nur bei genau einem Dokument ([[2026-10-09-briefkopf-seitenzaehlung]] bleibt dafür offen). Die
`@page`-Margin-Box erscheint auch mit `--no-pdf-header-footer`.

## 28. BDE — Kürzel-Familie, Werkbank-Prüfung beim Buchen, „nicht in Sage“ (v1.63.0)

### Die Familie ist EINE Regel — Angebot, Scan, FA-Baum und Buchungs-Prüfung müssen sie teilen
Seit v1.63.0 sieht und bucht eine Werkbank alle offenen `WorkOperation` der Werkbänke mit demselben `ArbeitsschrittCode`
(`Services/WerkbankKuerzelFamilie.cs`). Benutzt in `BdeApiController.GetAvailableOperations`, `BdeScanResolver.BuildNormalCandidatesAsync`,
`FaHierarchyController` (Gate `BdeRelevanteIds`) und `BdeBookingService.EnsureWorkOperationInFamilyAsync`.
**Warum:** Was das Terminal anbietet und was der Server beim Start annimmt, darf nicht auseinanderlaufen — sonst zeigt das
Terminal Knöpfe, die mit „gehört nicht zu dieser Werkbank“ scheitern, oder (vor v1.63.0) ein manipulierter POST bucht jeden
AG an jeder BDE-aktiven Werkbank. **Regel:** Jede neue Stelle, die „AGs dieser Werkbank“ braucht, ruft den Helfer; nie
`wo.ProductionWorkplaceId == workplaceId` neu schreiben. Ausnahme bewusst: `GetOpenByWorkplaceIdAsync` (Tracking je Werkbank)
bleibt an der bevorzugten Werkbank. Vergleich in C# (Trim + OrdinalIgnoreCase), nicht in SQL (§26 Kollation).

### Ohne Kürzel = nur die Werkbank selbst — darauf beruht „AKE unverändert“
`ArbeitsschrittCode` schreibt nur die Werkbank-Anlage (IDEAL, Master an) bzw. seit v1.63.0 die Maske an Werkbänken **ohne**
Sage-Nummer. AKE-Werkbänke haben keins → Familie `{Id}`. **Warum es weh tut:** Pflegt jemand bei AKE Kürzel ein, ändert sich
das Terminal-Angebot sofort (gewollt, aber dann nicht mehr „AKE wie vorher“). Prüfabfrage P5 der Spec vor dem Deploy.

### Die Buchungs-Prüfung muss VOR jedem Schreiben stehen
`StartPlannedAsync`/`ResumeAsync` schließen in den Regeln 1–4 eigene Rüst-/Tätigkeitsbuchungen und speichern sofort. Steht die
Familien-Prüfung dahinter, beendet ein abgewiesener Start trotzdem die laufende Tätigkeit des Werkers. **Regel:** Reihenfolge
BDE-Gate → (Resume: Parent laden) → Start-Sperre → Familie → verpackt → Kollision → Regeln 1–4.
Im NurFA-Modus prüft dieselbe Methode `ProductionOrder.ProductionWorkplaceId == Terminal-Werkbank` (Antwort 12), weil der
Default-AG „01“ an der Werkbank des ersten Starters hängt. Pausierte **Tätigkeiten** (ohne AG) bleiben ungeprüft.
**Auch Endpunkte, die vor der Buchung etwas ANLEGEN, prüfen vorher:** `StartProductionForOrder` legte den Default-AG
„01“ an, bevor geprüft wurde — der neue AG hing dann an der Terminal-Werkbank und bestand FA-a9 (QA-Befund Q1, `b3daf566`).

### Fortsetzen an fremder Werkbank: Liste zeigt es, Server weist ab (gewollt)
`PausedBookings` filtert nur nach Werker (werkbank-übergreifend); seit v1.63.0 weist `ResumeAsync` außerhalb der Familie ab.
**Bewusst nicht** gefiltert: Meldung statt stilles Ausblenden. Wer die Liste filtern will, entscheidet das neu.

### „Nicht in Sage“: Präsenz aus den ROHEN Quellzeilen, Empty-Guard, nie löschen
`ProductionWorkplaceSyncService` kennzeichnet `SageMissingSince` über die getrimmte Nummer **aller** gelesenen Zeilen — auch
solcher, die der Pflicht-/Ausschluss-Filter verwirft (sie stehen in Sage). Liefert die Quelle 0 Zeilen: nichts kennzeichnen,
Warnung (sonst wäre jede Sage-Werkbank „nicht in Sage“). `ProductionWorkplaceRepository.UpdateAsync` schreibt seit v1.63.0 nur
Pflegefelder (§24-Muster), damit das Web die Kennzeichnung nicht mit dem Ladestand zurücksetzt.
**Folge beim ersten Lauf:** Kürzel, die nur noch an gekennzeichneten Werkbänken hängen, erscheinen in der Struktur-Erkennung als
„unbekannt“ (inkl. Sammelmail) — gewollt; vorher DryRun und P2 gegen P3 abgleichen.

### Fertigmeldung = Buchung mit StartedAt == EndedAt
Die Option „nur AG-Fertigmeldung“ speichert eine `Production/Finished`-Buchung mit Dauer 0 + `IsFinal`-Menge. Alle
„erledigt“-Abfragen greifen ohne Änderung; die Zeitaufteilung blendet Dauer 0 aus (`BdeTimeSplitService.cs:43`). **Wer**
Auswertungen über Ist-Zeiten baut: Werkbänke mit dieser Option liefern Ist-Zeit 0 — kein Datenfehler.

## 29. Scannen ohne Feld am Netum (v1.71.1) — Skript-Fokus startet keine Eingabemethode

### Grünes Feld ist kein Beweis für Scanbereitschaft
Die Netum-„Fokus-Eingabe“ schreibt den Scan über die Android-Eingabemethode (IME) ins fokussierte Feld. Die IME startet nur, wenn ein
**Benutzer** ein Feld antippt, das eine Tastatur zulässt. Ein `focus()` aus dem Skript setzt zwar `document.activeElement` und `:focus`
(das Feld wurde grün), startet aber **keine** Eingabemethode – der Scan kommt nicht an. `inputmode="none"` im Markup verhindert den Start
auch beim Tipp.
**Warum das wichtig ist:** v1.68.1 zeigte nach Skript-Fokus grün, am Gerät als „bereit“ gelesen, obwohl nichts ankam (Freigabe-Nachtrag 7,
Befund 1/2). Deshalb: Bereitschaft nur nach einer Geste anzeigen (`pointerdown` → `focus`), `inputmode="none"` erst ~300 ms danach setzen
(Tastatur klappt ein, die IME-Verbindung bleibt) und bei `blur` wieder entfernen. Ein wiederholter oder verzögerter `focus()` bzw. `pageshow`
hilft nicht; ganz ohne Tipp ist das aus Chrome nicht erreichbar. Geräte mit Tasten-Ereignissen (Tastatur-Emulation, z. B. Zebra DataWedge
Keystroke) brauchen keinen Tipp – darum bleibt der Skript-Fokus, zeigt aber grau.
Code: `hardware-scanner.js` Block „Scannen ohne Feld“, CSS `.hw-scan-bereit`.

## 30. Update-Skript (v1.98.0, [[2026-11-09-update-skript-je-zielsystem-mit-nachlauf-spec]], ADR [[0022-update-je-zielsystem-per-skript-mit-nachlauf-manifest]])

- **Kopieren nie mit `/MIR`/`/PURGE`.** Fotos (`App_Data`, frueher `wwwroot\Fotos`), `Branding\`, `logs\` und die Konfiguration
  (`appsettings*.json`, `web.config`) liegen im Programmordner. **Warum wichtig:** Ein Spiegeln loescht alles, was nicht im Paket ist –
  Kommissionierfotos und die produktiven Verbindungen waeren weg.
- **DB-Ermittlung braucht die Umgebung aus `web.config` bzw. der Dienst-Registry (`Environment` = `DOTNET_ENVIRONMENT=...`).**
  **Warum wichtig:** Liest ein Lauf nur `appsettings.json`, nimmt er die Repo-Verbindung (AKESQL20) statt `appsettings.DEV.json` – auf DEV
  wuerde er die Produktiv-DB anfassen.
- **`ziele.psd1`: doppelte Pfade nur je Firma pruefen.** IDEAL-Prod und AKE-Prod haben denselben Web-Pfad, liegen aber auf verschiedenen
  Servern. **Warum wichtig:** Ein globaler Doppel-Check wuerde die gueltige Datei ablehnen.
- **PS 5.1: `Read-Host` wirft bei geschlossener Eingabe (Pipe zu Ende)** statt `$null` zu liefern; Variablennamen sind nicht
  Gross/Klein-empfindlich (`$t` ueberschreibt `$T`). **Warum wichtig:** Testlaeufe per Pipe brachen mit „Methode fuer NULL“ ab; Eingaben
  laufen ueber `Read-Eingabe` (Modul).
- **Git Bash wandelt `/save`, `/Q` usw. in Pfade um** (`icacls ... /save` wurde zu `C:/Program Files/Git/save`). **Warum wichtig:**
  Test-Vorbereitungen mit Windows-Schaltern scheitern still; `MSYS_NO_PATHCONV=1` setzen oder aus PowerShell aufrufen.
- **v1.99.0: StrictMode 2 wirft bei `.Eigenschaft` auf `$null` oder leerem Array** (`(Get-Command x | Select -First 1).Source`,
  `($leer.Name)`; unter PS 5.1 gemessen 2026-11-11). **Warum wichtig:** Fehlte `sqlcmd` im PATH, brach das Skript am IDEAL-Test roh ab,
  statt den Fallback-Pfad zu nehmen; `Select-Object -ExpandProperty` bzw. `@($x | ForEach-Object { $_.Name })` verwenden.
- **v1.99.0: `"$Name?"` liest in PS 5.1 die Variable `Name?`** (das `?` gehoert zum Namen); unter StrictMode Fehler. **Warum wichtig:** Die
  Rueckfrage nach der Version brach genau dort ab, wo der Mensch eingreifen sollte; `${Name}?` schreiben (Drift-Test-Waechter).
- **v1.99.0: Ein Seed, der nur im SQL-Skript steht, fehlt nach `Migrate()`.** Systeme, die die Migration per EF statt per Skript
  bekamen, haben die Zeilen nie (Kommissionierbereiche am IDEAL-Test). **Warum wichtig:** Ohne Standardbereich legt die App keine
  Kommissionierauftraege an; Seeds gehoeren in den Startup (Muster „wenn leer, anlegen“) oder als Pruefung ins Manifest.
- **v1.99.0: Eine offene Transaktion waehrend einer Eingabe haelt Sperren**, bei grossen DELETEs bis zur Tabellensperre. **Warum wichtig:**
  `uebernehmen`/`verwerfen` laeuft deshalb vor dem Start bei gestopptem Web und Dienst; ein Scharflauf nach `SCHARF` committet sofort.
- **v1.99.0: PS 5.1 wertet den Cast vor dem Methodenaufruf aus.** `([Delegate] { … }).GetNewClosure()` wirft „keine Methode
  GetNewClosure“; richtig ist `[Delegate]({ … }.GetNewClosure())`. Eine Funktion mit einem Element liefert einen Skalar, kein Array
  (`.Count` wirft unter StrictMode) – `@(...)` um den Aufruf. **Warum wichtig:** Beides fiel erst im DEV-Lauf auf (TS-126), nicht im Parser.

- **v1.99.16: Auch die Wiederholungssperre eines Fluss-A-Skripts muss im Trockenlauf `TREFFER: 0` ausgeben.** Fluss A startet nach dem
  Commit einen zweiten Trockenlauf; ein `RAISERROR … RETURN` ohne Trockenlauf-Zweig liefert Exit 1 ohne `TREFFER`, und die Prüfung
  `$null -ne 0` warnte mit leerem Wert ([[2026-11-13-updater-zweiter-trockenlauf-altbestand-spec]]). **Warum wichtig:** Nach jedem
  erfolgreichen Scharflauf stand eine Fehlwarnung; der Drift-Test (`SperrBloecke`) prüft jetzt jede `IF EXISTS … RAISERROR`-Sperre –
  mit BEGIN/CASE/END-Zählung, weil eine kurze Regex am inneren `END` des Trockenlauf-Zweigs endet und die Rotprobe nicht fängt.
- **v1.99.16: Jede Auswahl-Schleife braucht einen Ausgang für leere Eingabe.** `Read-Eingabe` liefert bei geschlossener Eingabe `''`;
  `while (-not $System)` fragte dann endlos („Ungueltig.“ 2670× in 8 s). **Warum wichtig:** `-NurPruefen` ohne Konsole (Pipe, Aufgabe)
  hing statt abzubrechen; Leer = Abbruch ohne Änderung, Drift-Test `DeploySkripte_AuswahlSchleifen_BrechenBeiLeererEingabeAb`.

## 31. Sage-Wareneingangsbeleg (v1.99.4, [[2026-11-08-wareneingang-teil-2-sage-we-beleg-rahmenabrufe-spec]])

- **Eine Ausschlussregel muss im Decorator UND in `EnqueueMissingAsync` stehen.** Die Nacherfassung nutzt die Decision nicht, sondern einen eigenen
  EF-Filter. **Warum wichtig:** Fehlt die Bedingung dort, holt der 15-Minuten-Sweep einen Sage-WE-Wareneingang als Zugang nach – der Bestand in Sage
  stiege doppelt.
- **Rest darf uebergebene Wareneingaenge nicht doppelt abziehen.** Abgezogen wird nur, was zum Abgleich-Stand noch nicht in Sage war
  (`Uebergeben` mit `SageWeAm` ≤ `SyncedAt` nicht); `SyncedAt` wird vor dem Lesen genommen. **Warum wichtig:** Sonst zeigt die Maske nach dem
  naechsten Abgleich einen zu kleinen Rest und meldet Ueberlieferungen, die keine sind.
- **Wiederholen nach Sage-Schreibfehlern nur mit belegter Korrelation (Lookup vor jedem Senden), sonst von Hand.** **Warum wichtig:** Ein Fehler
  kann nach dem Anlegen des Belegs entstehen (Timeout, nicht lesbare Antwort) – blindes Wiederholen legt einen zweiten Wareneingang an.
- **SData-Services antworten ohne `format=json` als Atom-XML; `ResultNumber 200` heisst Ok.** **Warum wichtig:** Im Schreibtest 2026-11-12 wertete
  ein reiner JSON-Leser den angelegten Beleg als Fehler. Erst der Status „Fehler statt Wiederholen“ verhinderte einen zweiten Beleg.
- **`SyncLogs.Message` ist nvarchar(1000).** Lange Sage-Antworten vor dem Protokollieren kappen. **Warum wichtig:** Sonst scheitert der
  Protokoll-Eintrag (SqlException „String or binary data would be truncated“), und genau die Fehlermeldung fehlt im Aktivitaets-Protokoll.
- **SData-`where` mit Datum nur als `@yyyy-MM-dd@`** (v1.99.7). `Belegdatum ge '2026-11-12'` liefert ohne Fehler 0 Treffer, `ge @2026-11-11@`
  die richtigen Belege (Live-Test `EinkaufBelegeInfo` 2026-11-12). **Warum wichtig:** Ein stilles „0 Treffer“ im Sicherungsnetz hieße „kein Beleg
  in Sage“ → der Dienst sendet erneut → zweiter Wareneingang.
- **Lookups gegen Info-Ressourcen immer eng filtern** (Memo-Kennung bzw. `ReferenzBelID` + Datum), nie nur `Belegkennzeichen` oder `Status`.
  `QueryAllAsync` blättert bis `$totalResults`; nur `Belegkennzeichen eq 'ELL'` sind ~90 000 Belege und liefen über 10 Minuten (P-W9 (b)).
  `EinkaufBelegeInfo` liefert auch erledigte und alte Belege (Status 2, ab 2015). **Warum wichtig:** Der Worker-Takt hinge sonst minutenlang und
  blockierte die Lagerbuchungen derselben Instanz.
- **Ausgeschalteter Sage-Wareneingang: Liegengebliebenes geht auf „Klären“** (Worker-Takt, v1.99.7), statt beim Wiedereinschalten automatisch
  nachzusenden. **Warum wichtig:** `ServiceSettings` haben keinen Änderungszeitpunkt; nur so wird nichts nachgeschickt, egal ob der Schalter
  über die Maske, ein Skript oder die DEV-Absicherung umgestellt wurde.
- **Memo-Kennungen (`WE#…#`, `SM#…#`) sind nur je WMS-Datenbank eindeutig – auf Testsystemen ein System-Kürzel setzen** (`Sync:SageSystemKuerzel`,
  v1.99.9). Gespeichert wird die Kennung, mit der gesendet wurde; das Kürzel beginnt mit einem Buchstaben. **Warum wichtig:** DEV, AKE-Test und
  AKE Test-IDEAL schreiben in dieselbe Sage-Testdatenbank, und nach einem Restore beginnen die Ids neu. Die Suche vor dem erneuten Senden fände
  den Beleg eines anderen WMS, hielte den eigenen Eintrag für „übergeben“/„bestätigt“ und sendete **nie**. Ohne gespeicherte Kennung fände die
  Suche nach einer Kürzel-Änderung die eigene Buchung nicht mehr → zweiter Beleg. Ein Ziffern-Kürzel (`12`) ergäbe `WE#12#7#`, das die Suche
  `%WE#12#%` eines Systems ohne Kürzel trifft.

## Sage-SData-Lesen (v1.73.0, [[2026-11-06-sage-sdata-stammdaten-sync-spec]])

- **`startIndex` ist 0-basiert.** Am AKE-Test liefert `startIndex=1` die erste Zeile nicht (LagerorteInfo 35 von 36; Lookup mit einem Treffer leer).
  **Warum wichtig:** Ein Reader mit 1 wirft „unvollständig gelesen“ oder – ohne diesen Guard – verliert still eine Zeile (QA B1, Fix `c8c6b210`).
- **Falscher Feldname ohne `select` kommt still als `null`; mit `select` als HTTP 500.** Unbelegte Felder nie ins `select`; Abgleiche, die
  deaktivieren/nullen, prüfen Pflichtfelder und werfen (`SdataBestandReader.Abbilden`). Spec-Feldlisten gegen das P-S1-Schema prüfen
  (z. B. kein `Aktiv` in `PpsStillstandsbegruendungenInfo`, kein `IstSperrlager` in `LagerorteInfo`).
- **Liefert eine Quelle das Schlüssel-/Gruppierfeld gar nicht, ausdrücklich signalisieren** (`ArbeitsschrittGeliefert`), sonst wird alles „nicht in Sage“.
- **Bool-Filter-DTOs ohne Default `true`** – ein fehlendes JSON-Feld würde still als „eingelastet/aktiv“ gelesen.
- **`SyncLogs.Message` hat 1000 Zeichen**; zu lange Meldungen gehen beim Schreiben still verloren → Diagnosen kappen.
- **EF `HasDefaultValue` auf bool/int-Status verschluckt explizite `false`/`0` beim Insert** (Sentinel) → DEFAULT nur in Migration/SQL.
- **Artikel-Bearbeitung postet alle aktiven Merkmale; `SaveValuesAsync` löscht „kein Bool/keine Option“** → Merkmale ohne Formularfeld (Text, Sage) serverseitig ausnehmen.
- **`PickingTransferService` schreibt `StockMovements` am Sage-Decorator vorbei** → Umbuchungen nur über die Nacherfassung (15 min); Regeln (z. B. FA-Bezug) deshalb in Decorator UND `EnqueueMissingAsync`.
- **`ne`/`gt` auf Textfeldern liefert am AKE-Test immer 0 Treffer** (Koordination 2026-11-07, `ArtikelVariantenExtended`, nur lesend):
  `Hauptlieferant ne null` → 0, `ne ''` → 0, `gt ''` → 0; dagegen `Hauptlieferant like 'K%'` → 23.509, `eq 'K31013'` → 121, ungefiltert 279.529,
  `00722001` hat `Hauptlieferant`/`LetzterLieferant` = `K31013`. **Warum wichtig:** Ein „nicht leer“-Filter liefert still 0 Zeilen statt eines
  Fehlers (Artikel-Lieferanten-Lauf 07.11. 09:20: `ArtikelVariantenExtended` 0 Zeilen in 13 ms, `hauptlieferant=0`). Nicht-leer-Filter clientseitig
  oder über `like`; 0 Zeilen bei einer Quelle, die nie leer sein kann, als Fehler/Warnung behandeln.
  *Umgesetzt `28afd9cc`:* beide `ne ''`-Filter auf `like '_%'` (mind. ein Zeichen, ohne Präfix-Annahme; `PpsRessourcenPositionenInfo` 240.552 = alle).
  Numerische `eq` (z. B. `IstBestellartikel eq -1`) sind unauffällig (43.144).
- **Kontokorrent liefert weder Name noch Adresse** (liegen in `Adressen`, Verweis `Kontokorrent.Adresse`). *Überholt 2026-11-07:* `Adressen` und
  `Ansprechpartner` sind am AKE-Test belegt und werden nachgelesen. Bleibt: **Hausadresse = `Liefer*`**, `Post*` ist das Postfach (fast leer);
  **`Ansprechpartner.Vorname/Nachname` können `nil` sein** – dann das Feld `Ansprechpartner` nehmen. **Warum wichtig:** sonst leere Namen bzw.
  leere Adressen bei fast allen Konten.
- **Sage-Spalten sind teils länger als die WMS-Spalten** (`LieferPLZ` 24 → `PostalCode` 20, `EMail` 255 → `Email` 200). **Warum wichtig:** Ein
  einziger zu langer Wert kippt `SaveChanges` und damit den ganzen Lauf bei jedem Zyklus (QA Q1, Fehler 2628). Je Feld gegen die Spaltenlänge
  prüfen; zu lang = nicht schreiben + Warnung (nicht kürzen, eine gekürzte PLZ/E-Mail ist falsch) – `KontokorrentSyncService.AdressFelder`.
- **Sage-Matchcodes sind nicht eindeutig** (AKE-Test: 2.287 Kreditoren-Konten / 2.280 Matchcodes). **Warum wichtig:** Ein Matchcode taugt nie als
  Schlüssel oder für ein Feld mit UNIQUE-Index (`ShortName`) – der Lauf würde beim Speichern am Index scheitern. Schlüssel ist immer `Kto`.
- **Quellensperren (offene Prüfabfrage) an EINEM Ort: `SageQuelleSperre`** (Web-Projekt), gelesen von der Dienst-Weiche und von `/ServiceSettings`.
  **Warum wichtig:** Stand die Sperre nur im Dienst (`SdataGesperrtBis`), zeigte die Maske `SData` als gültig, der Lauf las aber still View –
  Protokoll und Maske liefen auseinander (B6). Artikel ist nur gesperrt, wenn `Sync:ArticlesViewName` gesetzt ist (IDEAL); nicht lesbar = gesperrt.

## 32. Wareneingang-CSV auf UNC-Pfad (v1.99.11, [[2026-11-12-wareneingang-barcode-lieferschein-csv-export-spec]])

- **`initTextInputScanner(…, 'raw')` war bis v1.99.11 ein `'article'`-Scan** (`barcode-scanner.js`, Abbildung jetzt in `textScanType`).
  **Warum wichtig:** Ein roher Code wurde am QR-Trennzeichen `;` abgeschnitten, 8/12–14 Ziffern gingen in die EAN-Auflösung, im Matchcode-Modus in
  die Matchcode-Suche – das Barcode-Feld hätte falsche Dateinamen bzw. „nicht gefunden“ bekommen. Neuer Scan-Modus = Abbildung in `textScanType`
  ergänzen; die Artikel-Wrapper greifen nur bei `scanType === 'article'`.
- **Atomar ist nur das Umbenennen auf derselben Freigabe** (`.tmp` und `.csv` im selben Ordner, `File.Move(…, overwrite: false)`).
  **Warum wichtig:** Ein Leser, der `*.csv` abholt, darf nie eine halbe Datei sehen; ein Umbenennen über Freigaben hinweg ist ein Kopieren.
  `overwrite: false` + `File.Exists` vorher: eine fremde oder schon verarbeitete Datei wird nie ersetzt.
- **Dienstkonto `LocalSystem` greift als Computerkonto `DOMÄNE\SERVER$` auf die Freigabe zu.** **Warum wichtig:** Rechte für einen Benutzer
  reichen dann nicht – jede Datei endet als „Zugriff verweigert“. Recht „Ändern“ auf Freigabe **und** NTFS (anlegen, schreiben, umbenennen, eigene
  `.tmp` löschen); der Web-App-Pool ist ein anderes Konto, deshalb prüft `/ServiceSettings` nur die Form des Pfads.
- **Die CSV entsteht nur aus `Uebergeben`; `NichtNoetig` (Sage-WE beim Buchen aus) und `NurInSage` warten nie.** **Warum wichtig:** Ein
  „wartet“ für diese Wareneingänge wäre eine falsche Zusage – deshalb ist das Barcode-Feld bei ausgeschaltetem Sage-WE gesperrt und der Server
  weist einen trotzdem geposteten Barcode ab.
- **„Nie automatisch wiederholen“ hängt an festen Fehlertexten** (`WareneingangCsv.NieWiederholen`: Kollision, ungültiger Barcode).
  **Warum wichtig:** Wer eine Kollision mit eigenem Text meldet statt über die Konstante, lässt den Dienst eine vorhandene Datei alle 15 Minuten erneut
  prüfen und als Fehler zählen; Texte nur über die Konstanten setzen.
- **Seit v1.99.29 ohne Kopfzeile: die Spaltenreihenfolge ist der Vertrag mit enaio** ([[2026-11-14-wareneingang-csv-erfasser-samaccountname-spec]]).
  Eine neue Spalte nur am Ende anhängen und vorher mit enaio abstimmen; nie umsortieren. **Warum wichtig:** Der Import ordnet nach Position zu –
  eine eingeschobene Spalte verschiebt still alle folgenden Werte, kein Fehler, nur falsche Daten in enaio.
- **Erfasser kommt vom Benutzer der Bewegung, nicht aus `CreatedByWindows`/`StockMovement.WindowsUser`.** **Warum wichtig:** Diese Felder halten
  die Windows-Identität der Sitzung (Terminal-Konto, bei Formular-Anmeldung `""`/`SYSTEM`), nicht den buchenden Benutzer. Ohne das
  `Include(StockMovement).ThenInclude(User)` in `RunOnceAsync` fällt der Erfasser still auf `CreatedBy` zurück – der Test `RunOnce_IncludeWirkt_*`
  schützt davor.
- **Seit v1.99.33 (Sammellieferschein): die laufende Nummer kommt aus der DB, nicht aus dem Ordner** ([[2026-11-17-wareneingang-sammellieferschein-und-bestellkopf-spec]]).
  Start = Zeilen mit gleichem Barcode **und** `Datei`; belegt = `Datei` jeder anderen Zeile **oder** Datei im Ordner. **Warum wichtig:** enaio
  holt `B.csv` ab und räumt den Ordner leer – eine Nummer nur aus dem Ordner würde den Namen `B.csv` ein zweites Mal vergeben, enaio hielte die
  zweite Lieferung für die erste. Fremde Barcodes zählen bei „belegt“ mit, weil Barcode `B_1` den Namen `B_1.csv` schon tragen kann.
- **Der Inhalt bleibt beim Suffix gleich: Feld 1 = Barcode ohne `_n`.** **Warum wichtig:** enaio legt nach dem Barcode ab, nicht nach dem
  Dateinamen; ein Barcode mit Suffix im Inhalt fände den Aufkleber nicht. Prod erst nach Bestätigung E-1 durch enaio (Manifest-Info).
- **Grenze (ponytail im Code):** Gelingt das Umbenennen, scheitert aber `SaveChanges`, liegt die Datei im Ordner ohne `Datei` in der DB; der
  nächste Takt sieht sie über `File.Exists` als belegt und nimmt die nächste Nummer – also eine doppelte Datei, nie eine überschriebene.

## 33. SData-Kennwort verschlüsselt in den Service-Einstellungen (v1.99.14, [[2026-11-13-sdata-teil-2-userfelder-stueckliste-spec]], ADR [[0024-sdata-kennwort-verschluesselt-in-den-service-einstellungen]])

- **Der Dienst entschlüsselt mit dem Schlüsselring der WEB-App** (`DataProtection:KeysPath` im Dienst = Pfad der Web-App, `ApplicationName`
  gleich, Dienstkonto braucht Leserecht). **Warum wichtig:** Ein eigener oder fehlender Schlüsselring im Dienst macht `dp1:`-Werte
  unentschlüsselbar – dann sendet der Dienst bewusst NICHTS an Sage (kein stiller Rückfall auf appsettings), Lagerbuchung und Abgleiche stehen.
  Das Update-Skript stellt Ordner und Recht her (Platzhalter `{DataProtection}`).
- **Nicht entschlüsselbar ≠ leer.** Leerer `SData:Username` → appsettings-Rückfall mit Warnung (Übergang); gesetzter Benutzer mit
  unentschlüsselbarem Kennwort → Fehler, kein Rückfall. **Warum wichtig:** Ein Rückfall bei Entschlüsselungsfehler würde einen kaputten oder
  absichtlich gewechselten Zugang mit dem alten Klartext-Kennwort überdecken.
- **Typ `Geheim` statt Text für jedes Geheimnis in `ServiceSettings`.** **Warum wichtig:** Textfeld (`value=`), Orphan-Liste,
  `Einstellungspruefung`-Befund und Freitext-Pflege zeigen bzw. protokollieren sonst den gespeicherten Wert; nur der Typ `Geheim` schaltet alle
  vier Stellen auf „gesetzt/nicht gesetzt“.
- **Ressourcen-`$schema` liefert am AKE-Test 401, das Vertrags-`$schema` 200** (Lesetest [[sage-sdata-benutzerfelder-lesetest]]).
  **Warum wichtig:** Ein Werkzeug, das nur das Ressourcen-Schema liest, meldet „keine Felder“ – die Feldliste schneidet die Ressource deshalb aus
  dem Vertrags-Schema heraus. `SpecificUserfields`/`CustomFields` gehen nur als `include`, als eigene Ressource antworten sie 500.

## 34. Rahmenabrufe EBZ (v1.99.15, [[2026-11-11-wareneingang-teil-3-rahmenabrufe-spec]])

- **EBZ-Vorgangsmengen gelten je Rahmen, nicht je Abruf.** `PositionGGBestellt/Geliefert` einer Abruf-Position sind die Summen der
  Rahmenposition (P-W8: 2400 Stk Abruf, Vorgang 34800/33604). **Warum wichtig:** Rest aus den Vorgangsmengen wäre für jeden Abruf falsch –
  der Abgleich setzt für EBZ `MengeBestellt = MengeOffen = MengeBasis`, `MengeGeliefert = 0`.
- **Rest EBZ zieht ALLE WMS-Wareneingänge ab, auch `Uebergeben`** (`ErfasstJeBelPos.Abzuziehen(SagePurchaseOrderLine)`). **Warum wichtig:**
  Sage reduziert die offene Menge je Abruf nie; ein Abzug nur bis zum letzten Abgleich (wie bei EBB) ließe gelieferte Abrufe nach dem nächsten
  Abgleich wieder offen erscheinen. Grenze: in Sage direkt gebuchte Abruf-Lieferungen zieht das WMS nicht ab (Hilfeseite). Die alten
  `DateTime`-Überladungen sind entfernt, damit kein Aufrufer die Belegart-Prüfung umgeht.
- **Empty-Guard je Belegart.** **Warum wichtig:** Liefert eine der beiden Abfragen still 0 Zeilen (Filterfehler), würde ein gemeinsamer Guard
  den halben Cache löschen.
- **Sage-WE für Abrufe hängt an der Konstante `SageWareneingang.RahmenabrufMitSageWe`** (nicht an einer Einstellung). Erst nach dem einen
  Schreibtest (E2, Erfolg nur mit `ReferenzBelID` = Abruf-BelId) auf `true`; sonst würde das E5-Sicherungsnetz einen Abruf-Wareneingang ohne
  Kennung nicht finden und eine Wiederholung einen zweiten Beleg anlegen.

## 35. Kommissionier-Prio: MAX zählt zurückgenommene FAs mit (v1.99.17, [[2026-11-13-doppelte-kommissionier-prio-nach-ruecknahme-spec]])

- **`GetMaxPickingPriorityAsync` filtert bewusst NICHT auf `IsReleasedForPicking`.** Bei „Freigabe zurücknehmen“ bleibt die Prio stehen
  (und weiterlaufende Kommissionieraufträge behalten sie). **Warum wichtig:** Zählte das MAX nur aktuell freigegebene FAs, bekam ein neu
  freigegebener FA die Zahl eines zurückgenommenen – nach dessen erneuter Freigabe standen zwei FAs auf derselben Prio (DEV 2614167/2614168
  beide 32, sogar im selben Sammellauf). Folge der Regel: Die vorgeschlagene Zahl kann über jeder im Leitstand sichtbaren liegen (gewollt,
  Hilfeseite). Manuelle Doppelvergabe bleibt erlaubt; Altbestand zeigt `Pruefung/DoppeltePrio`.

## 36. Zeitstempel in Lokalzeit, UTC nur mit Eintrag in der Ausnahmeliste (v1.99.18, ADR [[0025-zeitstempel-in-lokalzeit-des-schreibenden-rechners]])

- **Neue Schreiber nehmen `DateTime.Now` (Web) bzw. einen Parameter `@Jetzt` mit `DateTime.Now` des Dienstrechners (raw SQL) – nie
  `UtcNow`/`GETUTCDATE()`/`SYSUTCDATETIME()`.** **Warum wichtig:** Die Anzeigen rechnen nicht um; UTC-Zeilen stehen 1–2 h zu früh, und
  dieselbe Spalte mischte bis v1.99.18 UTC und Ortszeit (Seeds per SQL mit `GETDATE`, `Program.cs` mit `UtcNow`) – Altdaten sind deshalb
  nicht sicher umrechenbar und bleiben, wie sie sind. `ZeitstempelKonventionTests` schlägt bei jedem neuen UTC-Aufruf außerhalb der
  Ausnahmeliste fehl (und bei einer veralteten Ausnahme).
- **Eine Zeit je Lauf/Aufruf.** **Warum wichtig:** Mehrere `Now`-Aufrufe in einer Sammelfreigabe oder einem Sync-Lauf ergeben unterschiedliche
  Stempel für zusammengehörige Zeilen; `ReleasedAt = ModifiedAt` bzw. `ReportedAt = ModifiedAt` setzt einen Wert voraus.

## 37. Lieferverzug und Einkauf: Zahlen aus Sage erst messen (v1.99.19/v1.99.20, [[2026-11-13-lieferverzug-nachbesserung-keyuser-spec]], [[2026-11-08-einkauf-bestellvorschlaege-spec]])

- **`AppSettingKeys` enthält nur Einstellungs-Keys als String-Konstanten.** Der Drift-Test verlangt jede `const string` dort als eigene Einstellung;
  feste Auswahlwerte gehören in eine verschachtelte Klasse (`AppSettingKeys.EinkaufBedarfWerte`). **Warum:** sonst rot ohne erkennbaren Bezug.
- **„Letzte Bestellung“ über SData ist nach `BelPosId` sortiert, nicht nach Datum** – der jüngste Beleg lässt sich nur über alle Seiten bestimmen,
  und ein 1-Jahres-Fenster findet am AKE-Test nur 55 % der letzten Bestellungen (E0-A3). **Warum:** ein Fenster wirkt vollständig, ist es aber nicht.
- **Gleiche Einheit heißt nicht Faktor 1:** `ArtikelLieferant` hat Stk/Stk mit `UmrechnungsfaktorEK` 2 oder 5 (und 0). Wer auf die Mindestbestellmenge
  aufrundet, prüft Einheit **und** Faktor (E0-A2). **Warum:** sonst wird in der falschen Einheit gerundet.
- **`EinkaufPositionenInfo` hat keine Lieferanten-Artikelnummer** (91 Felder, GET 2026-11-13) – Quelle ist `ArticleSupplier.SageBestellnummer`, die nur
  mit `Sync:ArtikelLieferantEnabled` gefüllt wird. Die Partner-Ressource heißt `eptEinkaufsbelege.100036671.DPSBS_DMS_Connector` (ohne Suffix 404).
- **Erste Welle Kreditor-Mails:** Auch mit `MaxAlterTage` 180 lagen auf AKE-DEV 96–99 Kreditoren über `KreditorMaxJeLauf` 30 („alle oder keine“) –
  vor dem Einschalten Zahl prüfen.
- **Bedarfe nie doppelt und nie an zwei Stellen rechnen (v1.99.30, [[2026-11-16-bestellvorschlaege-nachbesserung-spec]]).** Bedarfsmeldung und
  Lagerbestellung beschreiben oft denselben Bedarf wie der FA – Summieren zieht ihn doppelt ab (Till: Vorschläge zu hoch). Deshalb je FA
  max(FA-Rest, Meldung), dann max(FA-Teil, Lagerbestellungen), und zwar nur in `PlannedConsumptionService`; Artikelinfo und Liste lesen dort
  (Gleichheitstest `PurchaseProposalsNachbesserungE4Tests`). **Warum:** Die Artikelinfo hatte eine eigene Rechnung (Soll ohne Ausbuchungen) und
  zeigte damit andere Zahlen als der Einkauf – zwei Wahrheiten zum selben Artikel.
- **Nur `Ausbuchung` mindert den FA-Rest.** Eine Einbuchung mit FA-Nummer (Rücklage) darf den Rest nicht erhöhen bzw. senken – sonst sinkt der
  Bedarf beim Zurücklegen. **Warum:** `StockMovement.ProductionOrderId` hängt an beiden Richtungen.
- **Gleiche Einheit heißt auch bei `UmrechnungsFaktorLME` nicht Faktor 1, und `EinheitAbweichend` heißt nicht „nicht rechenbar“.** Die
  Cache-Mengen offener Bestellungen sind in der Basiseinheit; weicht nur die Bestelleinheit ab, ist die Menge brauchbar (EK005564). Über
  Einheitengrenzen wird bis P-N1/2/3/4/7 nichts umgerechnet, nur gemeldet. **Warum:** Richtung und Bedeutung der Faktoren sind in Sage nicht
  belegt; ein geratener Faktor 1 sieht plausibel aus und ist unbemerkt falsch.
- **`sqlcmd` setzt `QUOTED_IDENTIFIER` OFF** – `00_FreshInstall.sql` bricht dann mit Meldung 1934 an gefilterten Indizes ab (Rückgabewert trotzdem
  „gelaufen“ ohne `-b`). Migrationstest immer mit `sqlcmd -I`.

## 38. Doppel-Absenden, verpackter Deadlock, feste Kultur (v1.99.23, [[2026-11-14-schutz-doppel-absenden-und-unsichtbare-positionen-spec]], ADR [[0026-schreibaktionen-senden-die-absicht-der-server-prueft-am-schreibort]])

- **Ein Deadlock kommt verpackt an.** EF wirft ihn als `InvalidOperationException` → `DbUpdateException` → `SqlException 1205`; eine Prüfung
  nur von `ex` und `ex.InnerException` übersieht ihn (bis v1.99.22 `Nummernkreis.IstDeadlock`) – Folge HTTP 500 statt Wiederholung (N-2).
  **Regel:** `Nummernkreis.IstSqlFehler(ex, …)` läuft die ganze Kette ab; dasselbe für UNIQUE (2601/2627).
- **Wiederholversuch = neue Welt.** Wer in `MitVersuchenAsync` neu liest, muss auch den Status neu prüfen – die Vorprüfung vor der Schleife
  gilt nach einem verlorenen Gleichstand nicht mehr (K-1: zweiter Folgeauftrag). **Warum:** der Gewinner hat inzwischen geschrieben.
- **Umschalter sind gefährlich.** Ein Knopf, der „dreh um“ sendet, macht auf einer veralteten Seite das Gegenteil (K-6: Freigabe-Klick
  stornierte Aufträge). **Regel:** Zielzustand mitsenden, Server meldet „bereits …“ (ADR 0026).
- **Knopf-Sperre nie sofort `disabled`.** Ein im `submit`-Ereignis gesperrter Knopf fehlt mit Name/Wert in den Formulardaten; `site.js` sperrt
  per `setTimeout(…, 0)` und prüft dort `e.defaultPrevented` (später registrierte Listener, `confirm`-Abbruch). `form.submit()` aus Skripten
  löst kein `submit` aus und ist nicht gesperrt. Formulare, die bewusst mehrfach senden: `data-mehrfach-erlaubt`.
- **Restfenster:** zwei verschiedene Wareneingangs-Seiten mit demselben Barcode gleichzeitig (WE-4, Barcode-Prüfung ohne Sperre); zwei
  gleichzeitige Rücknahmen → RowVersion-Konflikt als 500 (K-7).
- **Feste Kultur `de-DE`, nicht `de-AT`.** Unter ICU ist der Tausendertrenner von `de-AT` U+00A0 – der Server-Spaltenfilter
  (`ColumnFilterSyntax`) liest nur den Punkt als Tausender, und der CSV-Export schrieb `-10 026,000` als Text. **Regel:** Kultur bleibt eine
  Code-Konstante; der Windows-Dienst hat sie (noch) nicht (Freigabe-Antwort 2).
- **CSV schreibt Zahlen ohne Tausendertrenner** (`csv-export.js cleanValue`): mit Dezimalteil immer, ganze Zahlen nur in Spalten mit
  `data-filter-type="number"` – sonst würden Positionen wie `1.234` zu `1234` (Freigabe-Antwort 6).
- **Unterpositionen ohne Vater-Zeile:** Im Kommissionierauftrag fehlen Baugruppen anderer Aufträge; eine Zeile mit `TreeLevel > 0` startete
  versteckt und wurde nie sichtbar (N-1). **Regel:** versteckt nur, wenn `BomViewModel.VaterPosition` einen vorhandenen Vorfahren findet.

## 39. Beschichtungsquelle: wer schreibt HasCoatingParts (v1.99.25, [[2026-11-16-beschichtungslogik-je-standort-spec]], ADR [[0028-beschichtungsquelle-je-achse-ueber-appsettings-mit-automatisch]])

- **Nur ein Schreiber je Quelle.** `BeschichtungKennzeichenQuelle` entscheidet, ob die Lackierteil-Erkennung (Artikelkategorie) oder die
  FA-Materialisierung (FaStruktur) `HasCoatingParts` setzt. **Warum:** Schrieben beide, kippten die Flags hin und her, und jeder Wegfall setzt
  `IsCoatingDone` zurück (§11). Deshalb prüft jeder Lauf die effektive Quelle selbst (`ResolveQuellen`).
- **Import-Hook braucht seit v1.99.25 `Sync:CoatingDetectionEnabled`.** Vorher lief die Erkennung im Sage-Import-Hook immer, sobald
  `Sync:BomCacheEnabled` an war. Steht `CoatingDetectionEnabled` auf `false` (Default, `ServiceSettingDefinitions.cs:288`), bekommen neue
  Aufträge kein Lack-Flag mehr. Das Update-Skript meldet es (`Pruefung/LackierteilErkennungImportHook`).
- **Unpassender Bestandswert sperrt `/Settings`.** Nach einem Umschalten des Masters kann ein gespeicherter Quellwert unpassend werden; dann
  speichert `/Settings` nichts, bis er korrigiert ist (Meldung nennt das Feld) – gleiches Muster wie die BDE-Pflichtregel.
- **Offen (Spec, Rückfrage 19):** Das BomCache-Fenster lässt komm-erledigte WAs aus, die Erkennung prüft sie aber und setzt ohne Cache-Treffer
  `HasCoatingParts`/`IsCoatingDone` zurück – heutiges Verhalten, nicht gemessen.

## 40. FA-Struktur IDEAL über SData (v1.99.26, [[2026-11-16-fa-struktur-ideal-ueber-sdata-spec]], ADR [[0029-fa-struktur-ideal-knoten-aus-sdata-fainfos-aus-view-hybrid]])

- **SData mit `where` ohne `orderBy` → HTTP 500**, auch bei RealTimeData. **Warum:** Sage paginiert serverseitig nur sortiert. Seit v1.99.26
  wirft `SageSdataReadClient.BuildUrl` vor dem Request eine `ArgumentException` – neue Abfragen immer mit `orderBy`.
- **`PpsFaBelegeInfo` liefert beendete Haupt-FAs nicht** (FA 1059463), `dtsFaBelegsuche` schon. **Warum wichtig:** ohne Wurzel fehlt die
  ganze Struktur bzw. die Stückliste live bleibt leer – deshalb Kopf über die Belegsuche.
- **Ressourcenliste (Artikel) ≠ FA-Stückliste.** Artikel 80141025 hat 6 Ressourcen, FA 1059463 aber 13 Positionen (L12). Die Artikel-Ansicht
  in „Stückliste live“ heißt deshalb „Ressourcenliste (Vorlage)“ – nie als FA-Stückliste lesen.
- **RealTimeData im Dienst nur im Modus „Vergleich“** – Regel nur in `FaStrukturQuelleSwitch.RealTimeDataErlaubt`, Sperre des Umschaltens nur
  in `SageQuelleSperre` lockern (P-FS3/DPS, Rückfragen 4/5). **Warum:** Blattfelder (Beschichtet, Maße, EKBedarf) haben in SData keine
  Quelle; ein frühes Umschalten setzt `Beschichtet` an Blättern auf false und ändert die Beschichtungslogik.

## 41. Anmeldung nur mit Kennwort, Erstinstallation (v1.99.28, [[2026-11-14-sicherheit-anmeldung-und-installer-spec]], ADR [[0027-formular-anmeldung-nur-mit-kennwort-erstkennwort-mit-pflichtwechsel]])

- **Meldung 207 („Ungültiger Spaltenname“) verwirft den ganzen Batch – nicht nur die Zeile.** **Warum:** SQL Server übersetzt den Batch bis
  `GO` vor der Ausführung; eine unbekannte Spalte in einer bestehenden Tabelle bricht beim Übersetzen ab (bei einer *fehlenden Tabelle* greift
  dagegen die verzögerte Namensauflösung). Am 04.11.2026 fehlten deshalb auf IDEAL-Prod sehr wahrscheinlich **alle** Indizes aus
  „15. Indexes“ samt FK (lokal nachgestellt: 0 von ~30). Wächter: `FreshInstallIndexSpaltenTests` prüft jede Index-Spalte in
  `00_FreshInstall.sql` gegen das EF-Modell.
- **Regel R/P sitzen nur im Formular-Login** (`AccountController.Login`). Windows-SSO (`WindowsAutoLoginMiddleware`) prüft weder Kennwort noch
  Haken – ein AD-Benutzer mit offenem Erstkennwort ist per SSO trotzdem drin. **Warum:** SSO ist die Anmeldung, das Kennwort ist nur der
  Formular-Weg (ADR [[0002-dual-auth-session-login-plus-windows-sso]]).
- **Lokale QA ohne Kennwort geht nur unter `dotnet run` (Development).** Auf DEV-IIS (Production) gilt Regel R – QA-Testbenutzer brauchen dort
  Kennwörter. **Warum:** die Ausnahme hängt bewusst an `IsDevelopment()`, nicht an einem Schalter.
- **`Pruefung/BenutzerOhneKennwort` sperrt Prod in der Vorprüfung** – admin zählt nicht (der Web-Start stellt ihn auf `wms-admin` + Haken).
  Seit v1.99.28 sind SQL-Prüfungen auch in Phase Vorher erlaubt (nur `anzeigen`, `NachlaufManifestDriftTests`).
- **Dienst-`appsettings.json` hat keine `Sync`-Schalter mehr** (außer `Feiertag*`, gebunden an `HolidaySyncOptions`). **Warum:** der Dienst liest
  `Sync:*`, `WorkerSettings:*`, `ErrorNotification:*` nur aus `dbo.ServiceSettings`; Datei-Werte waren wirkungslos und täuschten (BH-4). Der
  Installer schaltet den FA-Abgleich deshalb nach dem Aufwärmen **in der DB** ab.
- **RealTimeData-Feldnamen tragen das Tabellen-Präfix.** In `dtsFaBelegsuche` heißen die Kopf-Benutzerfelder `KHKPpsFaBelege_USER_OS*`.
  **Warum gefährlich:** mit den Namen ohne Präfix im `select` antwortet Sage 200 und lässt die Felder still weg – kein Fehler, nur leere Werte
  (QA IDEAL 2026-11-16, R1). Testantworten immer mit den echten Feldnamen aus der Feldliste bauen.

## 42. Offene Bestellungen: KW-Filter, Cache-Vollersatz, View aus zweitem Controller (v1.99.31, [[2026-11-16-offene-bestellungen-sofort-neu-lesen-einkauf-kw-filter-spec]])

- **Der KW-Klick im Kalender filtert per Text-Enthält – die Zelle muss „KWnn“ enthalten.** **Warum:** `table-filter.js` schreibt `KWnn` ins
  Feld; in einer Datumsspalte ist das kein Datum, sondern ein `Contains`-Teil auf dem gerenderten Text (`ColumnMap`). Ohne KW im Zelltext findet
  er nichts. Grenze: jahreslos und Teilstring – `KW4` trifft auch KW40–49 (im Test mit KW4/KW47 nachgestellt). Die Vorbelegung nutzt deshalb
  einen echten Datumsbereich `Mo...So`, der jahresgenau ist.
- **Cache-Vollersatz nie parallel.** **Warum:** `EinkaufBestellungenSyncService` ersetzt `SagePurchaseOrderLines` je Lauf komplett; regulärer
  Lauf und Anstoß nach Sage-WE kommen aus zwei Workern. Eine prozessweite `static SemaphoreSlim` serialisiert (regulär wartet, Anstoß
  überspringt und bleibt offen). Gilt nur innerhalb eines Dienst-Prozesses – zwei Dienst-Instanzen auf derselben DB wären nicht geschützt.
- **Eine View aus einem zweiten Controller rendern:** Tag-Helper ohne `asp-controller` nehmen den **aufrufenden** Controller. Formulare und
  Links auf Aktionen des Original-Controllers (Receive, SageWe-Aktionen) brauchen `asp-controller="PurchaseOrders"`, sonst zeigen sie auf
  `/EinkaufPurchaseOrders/Receive` (404). Filter-Formular und Pagination bleiben dagegen beim Aufrufer. Wächter:
  `PurchaseOrdersControllerTests.Einkauf_Attribute_UndReceiveBleibtGeschuetzt_F2_2_F2_4`.
- **Einen Controller als Dienst eines anderen nutzen** (`AddScoped<PurchaseOrdersController>`): dessen Class-Filter laufen nicht; Zugriff prüfen
  allein die Attribute des aufrufenden Controllers. `ControllerContext` vor dem Aufruf setzen (sonst `HttpContext`/`Url` null).
- **AB-Nummer/Kommission aus dem Belegkopf (v1.99.33): `where` nur auf `BelID`, nie auf USER-Aliasse** ([[2026-11-17-wareneingang-sammellieferschein-und-bestellkopf-spec]]).
  `dtsEKBelegMst.Sage.Wawi` liefert die Felder als `KHKEKBelege_USER_AB`/`KHKEKBelege_USER_Kom` (Präfix = Tabelle). **Warum:** Filtern auf
  einen USER-Alias ist in RealTimeData nicht belegt; die BelIDs kennt der Abgleich ohnehin. Fehlt der Schlüssel in **jeder** Zeile, ist der
  Feldname falsch (Warnung „Sage liefert … nicht“) – nicht still leer lassen. Testantworten mit den echten Feldnamen bauen (§41 sinngemäß).
- **Lesefehler am Kopf ≠ Lauf-Fehler.** Scheitert nur das Kopf-Lesen, ersetzt der Vollersatz trotzdem alle Positionen; AB-Nummer/Kommission
  kommen dann aus dem alten Cache je `BelID`. **Warum:** Sonst löschte ein Netzaussetzer am Zusatzfeld die Werte aller Bestellungen, und der
  Lauf ohne Kopf bräche den Wareneingang ab, der nur die Positionen braucht.

## 43. Beschichtungstermin nur über den Resolver (v1.99.25 E2/E4, Auslieferung mit v1.99.32, [[2026-11-16-beschichtungslogik-je-standort-spec]], ADR [[0028-beschichtungsquelle-je-achse-ueber-appsettings-mit-automatisch]])

- **Termin, Tooltip und Aktiv-Kennung nur über `BeschichtungResolver`** (`LadenAsync` + `Bestimmen`). **Warum:** FA-Liste, Leitstand und
  Abarbeitungsliste hatten je eine eigene Kopie der Formel und der Kategorie-Prüfung – der Termin konnte je Liste abweichen. Der Wächtertest
  `BeschichtungTerminQuelleWaechterTests` verbietet direkte Zugriffe auf `LackierteilKategorieName`/`BeschichtungTage`/`BeschichtungAbholtage`/
  `Start_Beschichtung` in Controllern und Views (Ausnahmen: Beschichtungsauftrag-Kopf, Hilfe).
- **Hierarchisch nur mit eindeutigem Kopf** (`EindeutigeKoepfeAsync`): Ein Kombigerät hat mehrere Köpfe; einen davon still zu nehmen zeigte einen
  falschen Termin – dann bleibt der Termin leer.
- **P-I1:** PROD IDEAL hat den Master aus (wirkt wie AKE, unverändert); nur TEST/DEV IDEAL zeigen die Änderung.

## 44. BDE-Teileliste: Schlüssel, Doppelschutz, Erkennung (v1.99.32, [[2026-11-13-bde-vereinfacht-teileliste-blechfertigung-spec]], ADR [[0030-teilmeldung-mit-fachlichem-schluessel-neben-workoperation]])

- **Node-Id ist kein Schlüssel.** Warum: Der Struktur-Sync erneuert `FaHierarchyNode`-Ids – Meldungen hängen am fachlichen Schlüssel
  (HauptFA, VaterFA, SubFA, Position, Artnr, Kürzel).
- **EXISTS nicht im Aggregat** (SQL Server Meldung 130, so in der P-TL3-Abfrage der Spec passiert). Warum: SQL Server lehnt Unterabfragen
  in Aggregaten ab – Gebucht-Kennzeichen erst je Kandidat in eine Temp-Tabelle rechnen, dann `SUM(CASE …)` (so im Einmal-Skript
  `SQL/Einmalig/2026-11-17_v1.99.32_RemoveParentWorkOperationsFromLeafTokensTeileliste.sql`).
- **Doppelschutz nicht InMemory-testbar.** Warum: Gefilterter UNIQUE und `sp_getapplock` greifen nur auf SQL Server – N1 bleibt Manual-UAT.
- **Erkennung liest nur die Werkbank-Option.** Warum: `BdeNurFaMeldung` ist eine Web-AppSetting, der Dienst kennt sie nicht; nur
  `ProductionWorkplaces.BdeTeileliste` steuert das Überspringen.
- **Kürzel nur an Leerzeichen trennen** – im Dienst wie im Einmal-Skript (`STRING_SPLIT(...,' ')`). Warum: Eine abweichende Trennung liefert
  andere Treffer als die Erkennung.

## 45. Arbeitsgangnummer ist kein Arbeitsplatz (v1.99.27/1.99.32, [[2026-11-16-arbeitsgang-mapping-sage-arbeitsgangnummer-spec]], ADR [[0031-sage-arbeitsgangnummer-an-operationconfig-sagewert-plus-handwert]])

- **`vw_IDEAL_AKE_WMS_Arbeitsgaenge` liefert Arbeitsgangnummern** (P-AG1/P-AG3). Warum: Der alte Einstellungstext sagte „Arbeitsplätze“, und
  auf IDEAL TEST legte die Werkbank-Anlage 26 Werkbänke mit Arbeitsgangnummern als `SageArbeitsplatznummer` an – eine spätere Rückmeldung
  an Sage hätte den falschen Arbeitsplatz gebucht.
- **Spalte 0 von `SageArbeitsplatzDto` je Quelle verschieden:** View = Arbeitsgang, Tabelle/SData = Arbeitsplatz. Warum: Das DTO ist geteilt –
  Nummern nur aus der View übernehmen (Guard im `OperationConfigSyncService`).
- **Sage-Wert nicht in `EditableProperties`.** Warum: Ein Web-Speichern darf den Sync-Stand nicht überschreiben (Muster wie `Source`/
  `SageMissingSince`); der Handwert gehört nur dem Web, der Sage-Wert nur dem Sync.

## 46. AKE-Stücklisten-Positionen sind Text (v1.99.34, [[2026-11-16-beschichtungslogik-je-standort-spec]] Nachtrag 3)

- **Position nie als `int` parsen.** Die AKE-Stückliste führt mehrstufige Positionen (`51.5.2`, `10.140.40.10.10` an S1402262, bis 5 Ebenen /
  15 Zeichen laut P-A9). Warum: P-A6 (2026-11-17) widerlegte „Lackierteil-Positionen ganzzahlig“; ein `int.TryParse` hätte Zeilen still
  verloren oder zusammengelegt. Deshalb `BeschichtungsauftragPosition.PositionText` (SQL/157), `Position` bleibt an der AKE NULL.
- **Reihenfolge nur über `NaturalPositionComparer`.** Warum: alphabetisch steht `5.10` vor `5.2`; die Kommissionierung sortiert schon so.
- **Schlüssel = Artnr + PositionText, normiert wie `PickingOrderRestmengen.Schluessel`** (Artnr trim+upper, Position trim, nie
  Zahl-Normierung). Warum: `5.2` und `5.20` sind verschiedene Zeilen. Altzeilen ohne Text gelten an der AKE nicht als vergeben – die
  Manifest-Prüfung `Pruefung/BeschichtungPositionOhneTextFlach` meldet sie.
- **Flache Quelle filtert nicht nach `IsDonePicking`.** Warum: Beschichtung läuft oft nach der Kommissionierung; offen heißt nur
  `!IsDone && !IsCancelled` (A13).
- **InMemory vergleicht Artikelnummern mit Groß/Klein**, SQL Server nicht. Die Zuordnung im Speicher ist deshalb `OrdinalIgnoreCase`; der
  Unit-Test kann die SQL-Kollation nicht belegen (TS-145).

## 47. Byte-genaue Dateiausgabe: Kodierung und Git-Zeilenenden (v1.99.35, [[2026-11-13-blechfertigung-bf-auftraege-wicam-csv-spec]])

- **Windows-1252 immer mit `EncoderFallback.ExceptionFallback`** (`BfCsv.Kodierung()`), nie `Encoding.GetEncoding(1252)` pur. Warum: der
  Standard-Fallback ersetzt nicht darstellbare Zeichen still durch `?` – WiCAM bekäme einen falschen Matchcode ohne jede Meldung. Die Freigabe
  prüft vorher, der Dienst meldet den Rest als CSV-Fehler (Haus-Regel „melden statt still behandeln“).
- **Byte-Vorlagen für Tests als `binary` in `.gitattributes`** (`IDEALAKEWMSService.Tests/TestData/.gitattributes`: `*.csv binary`). Warum: mit
  `core.autocrlf` legte Git die CRLF-Vorlage mit LF ab; der Byte-Vergleich hätte auf einem frischen Checkout anders ausgesehen als lokal.

## 48. FA-Struktur-Schlüssel: C#-Vergleich folgt der Spalten-Kollation; verschwundene Zeilen bleiben stehen (v1.99.37, [[2026-11-17-fa-struktur-abgleich-statt-full-replace-spec]])

- **Schlüssel immer über `FaStrukturAbgleich.Norm` vergleichen** (nachlaufende Leerzeichen weg, Groß/Klein egal, leer = `null`). Warum: SQL
  Server vergleicht `nvarchar` mit der CI-Kollation und ignoriert nachlaufende Leerzeichen. Ein Ordinal-Vergleich in C# hält `"BG "` und `"bg"`
  für zwei Zeilen; der UNIQUE-Index lehnt die zweite dann beim Speichern ab und der ganze Lauf scheitert.
- **Seit v1.99.37 löscht der Lauf nicht mehr alles:** `FaHierarchyNodes`/`FaHierarchyOrderInfos`/`ProductionOrderPositions` enthalten in Sage
  beendete oder ungeklärte Zeilen. Wer neu liest (Einmal-Skripte, Berichte, neue Konsumenten), filtert `VerschwundenSeit IS NULL`, wenn er nur
  aktive Struktur will. Warum: sonst entstehen aus beendeten FAs neue Arbeitsgänge oder WMS-Aufträge (ADR 0032).
- **„Owner gelesen“ heißt: seine Stückliste kam im Read, nicht: die Nummer kam irgendwo vor** (QA 2026-11-17, Commit `1f0361dd`). Nur
  `VaterFA ?? HauptFA` gelesener Knoten bzw. `BelID` gelesener Positionen zählen. Warum: Ein beendeter Sub-FA steht weiter als Knoten in der
  Stückliste seines offenen Vaters (`SubFA`), und eine beendete Haupt-FA kann in FAInfos stehen, obwohl FAListe sie nicht mehr liefert. Zählt
  das als „gelesen“, gilt die fehlende eigene Stückliste als „Position in Sage entfernt“ und wird ohne Sage-Abfrage gelöscht.

## 49. Sage-Schreibpayload ohne belegtes `$template`: Erfolg nur mit Handle, unklar nie automatisch (v1.99.38, [[2026-11-17-arbeitsgaenge-aus-wms-in-sage-fa-anlegen-spec]])

- **Die Feldnamen für `FertigungsauftragService` ServiceOption 4 sind nur aus dem `$schema` von IDEAL Prod abgeleitet** und stehen als
  Konstanten mit `TODO(P-1..P-4)` in `SageFaArbeitsgangClient`. Vor dem ersten Einschalten per GET `$template`/`$schema` am Ziel prüfen und den
  Schreibtest an EINEM FA machen. Warum: Ein HTTP 200 allein beweist nicht, dass die Position angelegt wurde, wenn ein Feldname nicht passt.
- **Darum gilt 2xx nur mit Handle in der Antwort als Erfolg; ohne Handle, bei Timeout oder hängendem „Gesendet“ = „Antwort unklar“** (Text-Präfix
  `SageFaArbeitsgaenge.UnklarText`), nie automatisch wiederholt. Warum: Sage kann angelegt haben; eine Wiederholung ohne Prüfung legt die AG
  doppelt an, und das WMS löscht in Sage nichts. Erneut einreihen nur nach Blick in Sage; der Dienst liest den FA vor jedem Senden ohnehin.
- **Nachtrag v1.99.39 (QA F3, [[2026-11-17-v1-99-39-nachbesserung-abend]]):** Die Handle-Regel ist ersetzt. Der Request schickt `Handle` = BelID
  selbst mit, ein Handle in der Antwort beweist also nichts. Jetzt gilt: 2xx ohne `ErrorText` = vorläufig, **angelegt erst nach Nachlesen**
  der AG am FA (GET, AG-Nummer = `RessourceNummer` der AG-Position); kein Fund = Fehler mit Text, Lesefehler = unklar. Warum: Nur der Sage-Stand
  nach dem Schreiben belegt das Ergebnis; ein falscher Erfolg würde nie korrigiert, weil der Queue-Status in der Vorschau vor dem Live-Stand gewinnt.

## 50. Seite hinter der letzten: Controller rechnen Skip/Take vor der Trefferzahl (v1.99.39, [[2026-11-17-v1-99-39-nachbesserung-abend]])

- **Alle Listen-Controller rechnen `Skip((page-1)*size)` aus dem URL-Wert, bevor die Trefferzahl bekannt ist** (Repository liefert Items und
  Count in einem Aufruf); `PaginationState.FirstItemNumber` rechnet ungeklemmt. Folge: `page=2` bei 8 Treffern = leere Tabelle mit
  „Eintraege 26–8 von 8“. Behoben **zentral** mit dem globalen Result-Filter `Filters/PaginationSeiteKlemmenFilter` (nur GET): findet den
  `PaginationState` im Model (Property) oder im ViewBag und leitet auf die letzte Seite um (0 Treffer: 1), übrige Query-Parameter bleiben.
  Warum Redirect statt Klemmen im `PaginationState`: Klemmen nur der Anzeige ergäbe „1–8 von 8“ über einer leeren Tabelle; Klemmen vor der
  Abfrage hieße 40 Controller anfassen. **Neue Liste:** Seitenparameter muss `page` heißen und der State im Model (Property) oder im ViewBag
  stehen, sonst greift der Filter nicht.

## 51. Sage-Benutzerfelder nie als String-Literal in eine Abfrage (v1.99.40, [[2026-11-18-v1-99-40-sage-userfelder-als-einstellung]])

- **Problem:** USER_-Felder legt jeder Sage-Mandant selbst an; AKE und IDEAL unterscheiden sich. Fehlt einem Mandanten ein fest eingebautes Feld,
  bricht der **ganze** Abgleich ab (HTTP 500 „Ungültiger Spaltenname“), nicht nur das eine Feld. So geschah es am IDEAL-Test mit
  `USER_EKMindestbestellwert` (Kreditoren).
- **Regel:** Ein neues Feld wird ein Eintrag in `SageUserfelder.Katalog` (Default = Name bei AKE).
  - Im Code nur über `Feld()`/`Konfiguriert()` abfragen.
  - Intern den logischen Namen behalten.
  - Werte bei abweichendem Namen über `[JsonExtensionData] Weitere` + `SageUserfelder.Text` lesen.

  Siehe ADR [[0033-sage-benutzerfelder-als-einstellung]].
- **Test:** `SageUserfelderTests.Default_selects_unveraendert_wie_v1_99_39` hält die AKE-Abfragen zeichengleich. Wer sie absichtlich ändert, zieht den Test mit.

## 52. Positionen ohne FA nie in die Vergaben; der Vorschlags-Haken ist kein Zustand (v1.99.41, [[2026-11-18-v1-99-41-beschichtungsauftrag-ausbau]])

- **Problem 1:** Eine Position ohne FA (`OhneFa = 1`) hat keinen Herkunftsschlüssel aus der Struktur (HauptFA/VaterFA/SubFA/Position bleiben
  leer bzw. 0). Liefe sie durch `GetVergabenAsync`/`VergabenJeSchluessel`, kollidierten alle freien Positionen eines HauptFA miteinander:
  derselbe Artikel ließe sich kein zweites Mal vergeben, und die Vorschlagsliste zählte falsch.
- **Regel:** Jede Abfrage über Vergaben filtert `!OhneFa` (Repository `GetVergabenAsync`, Workflow `EigeneSchluessel`,
  `GetHinzufuegbarePositionenAsync`). Wer eine neue Vergabe-Abfrage schreibt, nimmt den Filter mit.
  Folge (gemeldet, nicht gebaut): Ein Auftrag **nur** aus Positionen ohne FA steht nicht im Gruppenkopf der Vorschlagsliste.
- **Problem 2:** Der Haken in der Vorschlagsliste ist eine Auswahl, kein gespeicherter Zustand (Rückfrage 8). Der Server wertet nur, was gepostet
  wird: `schluessel` (Liste), `artnr` (Summiert, Server löst zu den **jetzt** freien Positionen auf), `freiArtikelId`/`freiMenge`.
  Ist ein Teil ungültig, wird **nichts** angelegt (keine Nummer verbraucht); nie still den gültigen Rest anlegen.
- **Test:** `BeschichtungsauftragWorkflowTests.FreiePosition_GleicherArtikelInZweiAuftraegen…`, `BeschichtungsauftraegeControllerTests.Create_TeilweiseUngueltig_GanzAbgelehnt_NenntVergabe`.

## 53. Erklärende Hinweise vor die Fremdmeldung, nie dahinter (v1.99.42, [[2026-11-18-v1-99-42-nachbesserungen]])

- **Problem:** `SageUserfelder.Erklaeren` hängte „Einstellung … prüfen“ an die SData-Meldung an. `SdataLeseException.Message` ist auf 600 Zeichen
  gekappt, die SQL-Fehlermeldung von Sage („Ungültiger Spaltenname …“ samt Abfragetext) ist länger – der Hinweis fiel im Aktivitäts-Protokoll
  weg, nur bei kurzen Meldungen (Kreditoren) sah man ihn. Befund 13:25 am Testsystem IDEAL :88.
- **Regel:** Der eigene Hinweis steht **vorn**; erklärt wird aus der ungekappten `Diagnose` (`Erklaeren(Exception)`), und eine schon erklärte
  Meldung wird nicht doppelt erklärt. Wer eine Meldung kappt oder kürzt, setzt das Wichtige an den Anfang.
- **Test:** `SageUserfelderTests.Erklaeren_LangeSqlDiagnose_HinweisVorn_AusDiagnose_NieDoppelt`.

## 54. In einem Bootstrap-`fieldset` steht direkt nach der `<legend>` der sichtbare Inhalt, kein verstecktes Feld (v1.99.44, [[2026-11-19-v1-99-44-sammel-bugfix]])

- **Problem:** Bootstrap-Reboot setzt `legend { float:left; width:100% }` und hebt das Fließen nur mit `legend + * { clear:left }` auf. Ein
  `<input type="hidden">` direkt nach der legend schluckt diese Regel; der Container fließt neben die 100-%-Legende und wird auf eine schmale
  Spalte gequetscht (Kommissionier-Ziele v1.99.43, Text „K-“/„01“ umgebrochen).
- **Regel:** Versteckte Felder hinter den Inhalt oder ans Ende des fieldset; vor die legend geht nicht (sie muss erstes Kind sein).
- **Test:** `KommissionierSpaltenKonsistenzTests.KommissionierZiele_NachLegendFolgtDerContainer`.

## 55. Rollen seit v1.51.0 kommen nur über das Update-Skript in die Datenbank (v1.99.44, [[2026-11-19-v1-99-44-sammel-bugfix]])

- **Problem:** `beschichtungsauftrag`, `beschichtungsauftrag_schreiben`, `einkauf`, `schachtler` haben weder Seed (`Program.cs:237-251`) noch
  SQL-Skript, sondern nur eine Haltepunkt-Prüfung mit Reparatur in `scripts/deploy/nachlauf.psd1`. `dev-deploy.ps1` führt das Manifest nicht
  aus – auf DEV fehlen die Rollen, die Seiten sind dort nur für Admins erreichbar.
- **Regel:** Eine neue Rolle bekommt eine Manifest-Prüfung mit Reparatur (Muster `Pruefung/RolleSchachtler`) **und** den Rollen-Dreiklang
  (Attribut, [[controller]], `RoleOverview.cshtml`). Auf DEV nach dem Deploy die Reparatur einmal laufen lassen.

## 56. `GetCurrentStockAsync(articleNumber)` sucht per Teiltext (v1.99.45, [[2026-11-19-v1-99-45-sammellauf]])

- **Problem:** Der Parameter ist ein Suchbegriff (Contains über Nummer), kein Schlüssel. Die Artikelinfo zeigte so Bestände von Artikeln mit
  ähnlicher Nummer mit; der Lagerplatz-Text der Lagerbestellung ebenso.
- **Regel:** Für **einen** Artikel das Ergebnis nach `ArticleId` filtern oder `GetStockByArticleNumbersAsync` (exakt) nehmen. Die Teiltextsuche
  bleibt für Bestandslisten mit Suchfeld (Grund: dort ist sie gewollt).

## 57. Update-Skript: Ergebnis und Exit-Code sind zwei Angaben (v1.99.45, [[2026-11-19-v1-99-45-sammellauf]])

- **Problem:** Ein mit Fehler beendeter Trockenlauf hieß „ohne Zeile TREFFER (Exit 1)“ – man las daraus „nichts gefunden“.
- **Regel:** Neue Meldungen nach `<Fall>: [<Vorgang> ]<Ergebnis>[ -> <Folge>][. <Handlung>]`; Exit-Code nur ≠ 0, immer als eigenes
  „mit Fehler beendet (Exit n)“. Muster in `docs/betrieb/03-installation-update-rollback.md` Abschnitt 5; Wächter `NachlaufManifestDriftTests`.

## 58. Neuer Menüpunkt = Katalogeintrag + Regel + Layout-Link (v1.99.45, [[0034-navigation-menue-und-kacheln-aus-einer-quelle]])

- **Problem:** Menü und Startseite hatten getrennte Bedingungen und liefen auseinander (Kachel auf 403, fehlende Kacheln).
- **Regel:** `NavZiel` in `NavZiele` (mit Kategorie, wenn es eine Kachel sein soll), Fall im `switch` von `NavigationSicht` (gleiche Rolle wie
  der Filter der Action), Link im Layout in eigenem `@if (nav.Zeigt(NavZiele.X))`. Keine Rechte/Schalter mehr direkt im Layout oder in Home.
- **Nachtrag v1.99.46:** Kachel-`NavZiel` braucht zusätzlich `SymbolPfad` (d-String eines 16×16-SVG, nur `nonzero`-Pfade ohne `fill-rule`,
  Konstante `Sym…` im selben File; Wächter `JedeKachel_HatEinSymbol`). **Warum:** v1.99.45 hat die Symbole weggelassen, obwohl die Spec sie
  vorsah, und kein Test fing es – QA las es als „bewusste Vereinfachung“ ([[2026-11-20-dashboard-kacheln-symbole-fehlen-bug]]).
- **Nachtrag QA v1.99.46:** Ein Pfad, der „im Projekt steht“, ist nicht automatisch heil. `file-earmark-pdf`
  (`FaHierarchyKommissionierListen/Summiert.cshtml:112`) und `gear` (`Settings/Index.cshtml:14-15`) sind unvollständige Bootstrap-Pfade und
  rendern schon an der Quelle als volle Fläche (bei 14–16 px fällt es kaum auf, bei 32 px schon). **Warum:** Der Wächter prüft nur die Zeichen
  des Pfads, nicht das Bild. Neue Kachel-Symbole deshalb einmal groß rendern (Headless-Edge-Screenshot einer statischen Seite genügt), bevor sie
  in den Katalog kommen.

## 59. `@Json.Serialize` in einem doppelt gequoteten HTML-Attribut (v1.99.47, [[2026-08-05-wms-bugs-improvements-teil-6-spec]])

- **Problem:** `data-x="@Json.Serialize(obj)"` liefert `IHtmlContent`; Razor schreibt es **roh** ins Attribut. Das erste `"` des JSON beendet
  das Attribut, `JSON.parse(el.dataset.x)` wirft. In v1.99.47 war dadurch „Lagerplatz ausbuchen“ mit FA komplett tot (Knopf, Modal, Wagen).
- **Regel:** Im Attribut `@Json.Serialize(obj).ToString()` (ein `string` – Razor kodiert `"` zu `&quot;`, der Browser dekodiert zurück).
  Im `<script>`-Block dagegen `@Json.Serialize(wert)` ohne Anführungszeichen drumherum, nie Razor-Text in einem JS-String.
  Wächter `JsonImAttributWaechterTests`.
- **Warum:** Kein Controller-/InMemory-Test rendert die View; der Fehler ist nur im Browser sichtbar. Gefunden erst im QA-Lauf auf DEV
  (Konsole: `SyntaxError … in JSON at position 1`).

## 60. Stern/Knopf in einer Link-Karte: nie ein interaktives Element im `<a>` (v1.99.51, [[2026-11-23-dashboard-favoriten-reiter-spec]])

- **Problem:** Die Startseiten-Kachel war ein `<a>` um die ganze Karte. Ein Favoriten-Stern (Formular + Knopf) darin wäre ungültiges HTML;
  Tastatur und Screenreader springen falsch, ein Klick auf den Stern öffnet die Seite.
- **Regel:** Link als `stretched-link` im Titel, Formular/Knopf als **Geschwister** mit `position:absolute` und `z-index` > 1. Der Fokusring
  der Karte sitzt dann auf `.dashboard-link:focus-visible::after`, weil der Link die Karte nicht mehr umschließt. Favoriten nur über
  `nav.Favoriten(keys)` rendern (nie an `NavigationSicht` vorbei), sonst zeigt ein Favorit nach Rechte- oder Modulentzug mehr als die Startseite.
  Wächter `Startseite_KeinFormularOderKnopfInnerhalbEinesLinks`.
- **Nebenbei:** Razor-Funktionen in `@{ }`, die einen TagHelper (z. B. `<form asp-…>`) enthalten, müssen `async Task` sein und mit `await`
  aufgerufen werden (Compiler-Fehler MVC1006).
- **Warum:** Kein Controller-Test rendert die View; die Verschachtelung fällt nur im Browser oder per Quelltext-Wächter auf.

## 61. Markierung an Bootstrap-Tabellenzeilen: über `--bs-table-bg-state`, nicht über `background-color` (v1.99.52, [[2026-11-23-wareneingang-zu-bestellung-bedienung-spec]])

- **Problem:** Bootstrap 5 malt den Zellhintergrund als `box-shadow` (`--bs-table-bg-state`/`-type`). Eine eigene `background-color` oder ein
  eigener `box-shadow` an der Zelle überdeckt `table-danger`, Streifen und Hover – Fehlerzeilen wären nicht mehr rot.
- **Regel:** Aktiv-/Fokus-Zeile über `--bs-table-bg-state` setzen; ein zusätzlicher Balken als zweiter Schatten **zusammen mit** dem
  Bootstrap-Hintergrund-Schatten. Balkenfarbe `--bs-primary-text-emphasis` (#052c65): `--bs-primary` erreicht auf der aktiven
  `table-danger`-Zeile nur ca. 2,7:1 (< 3:1 für Bedienelemente).
- **Warum:** Der Wareneingang zeigt Fehlerzeilen rot; die neue Fokus-Zeile (v1.99.52) darf diese Bedeutung nicht wegnehmen.
- **Ergänzung v1.99.62** ([[0035-tabellen-zeilenfarben-global-ueber-bootstrap-zustandsvariablen]]):
  - `background-color` am **`<tr>`** ist in Bootstrap 5.3 **unsichtbar** – die Zellen malen deckend darüber (`--bs-table-bg` = Weiß). Die alte Regel
    `.table tbody tr:hover` war deshalb jahrelang wirkungslos, Tests grün. Ebenso vermutlich die OSEON-Ebenenfarben am `tr` (jetzt `table-plain`).
  - `--bs-table-active-bg` **nie global** setzen: färbt die Gruppen-Kopfzeilen `tr.table-active` (Kommissionierlisten, Beschichtung, Vormontage).
    Fokus-Zeile hat deshalb eine eigene Variable `--wms-table-focus-bg`.
  - Keine Kontextklasse an `<table>` selbst: die globale `.table`-Variable (gleiche Spezifität, später) überschreibt dann Bootstraps Wert.
  - **Warum:** Die Fehler sind alle pixel-, nicht testsichtbar – Sichtprüfung je Zustand ist Pflicht (TS-187).

## 62. WMS-Ausbuchung auf einem Sage-Platz ohne Sage-Übergabe wird vom Lagerbestand-Abgleich zurückgebucht (v1.99.53, [[2026-11-23-lagerbestellung-verbrauchsbuchung-bei-lieferung-spec]])

- **Problem:** Plätze mit `Source = Sage` setzt der Lagerbestand-Abgleich auf den Sage-Bestand (`LagerbestandSyncService`). Eine WMS-`Ausbuchung`
  dort, deren Entnahme nicht an Sage geht (ServiceSetting `SageLagerbuchungAktiv` aus, oder Platz ohne `SageBuchungErlaubt`/Sage-Referenz laut
  `SageWareneingang.IstSageWePlatz`), ist nach dem nächsten Lauf still verschwunden.
- **Regel:** WMS-seitige Verbrauchs-/Entnahmebuchungen (Lagerbestellung v1.99.53) bieten Sage-Plätze nur an, wenn die Entnahme an Sage geht
  (`WarehousePickingController.EntnahmeErlaubt`), und weisen sie serverseitig mit klarer Meldung ab. Gilt für jeden neuen Buchungsweg.
- **Warum:** Sage bleibt die Quelle für Sage-Plätze; sonst stimmen WMS-Bestand und Bewegungshistorie nicht mehr überein, ohne dass es jemand merkt.

## 63. Sage-SData mit `select`: jedes `orderBy`-Feld muss auch im `select` stehen (AKE-Prod 2026-11-24, [[2026-11-24-hotfix-deploy-ake-test-startkennwort]])

- **Problem:** AKE-Prod-SData baut bei gesetztem `select` ein `GROUP BY <select-Felder>` in die Paging-CTE
  (`WITH PGCTE AS (SELECT "Ressourcenummer", ROW_NUMBER() OVER (ORDER BY "Ressourcenummer" asc, "BelPosID" asc) ... GROUP BY Ressourcenummer)`).
  Ein `orderBy`-Feld ausserhalb des `select` ergibt dann HTTP 500 („... ist in der Auswahlliste ungültig, da sie nicht in einer Aggregatfunktion
  und nicht in der GROUP BY-Klausel enthalten ist“). Getroffen: Artikel-Sync `PpsRessourcenPositionenInfo` (select `Ressourcenummer`, orderBy
  `+BelPosID`) und Artikel-Lieferanten `ArtikelVariantenExtended` (select `Artikelnummer,Hauptlieferant`, orderBy `+AuspraegungID`, SyncLogs Id 4883802).
- **Tückisch:** Der AKE-Test (`sagetest01`, `ake_TEST2026;1`) gruppiert NICHT – dieselbe Abfrage lief dort am 2026-11-13 mit 240.552 Zeilen in
  49 Seiten (DEV-Dienstlog `service-20261113.log`). Der Fehler ist am Testsystem nicht nachstellbar.
- **Folge des Gruppierens:** Bei `select` ohne eindeutigen Schlüssel liefert Prod nur die verschiedenen Wertkombinationen (DISTINCT), nicht jede Zeile.
  Wer echte Zeilen braucht (Mengen summieren, Positionen zählen), muss den Schlüssel ins `select` nehmen.
- **Regel:** `orderBy`-Felder ⊆ `select`-Felder, oder ohne `select` lesen. **Zentral umgesetzt (`f420e23e`):**
  `SageSdataReadClient.BuildQueryUrl` hängt über `MitSortfeldern` jedes fehlende Sortfeld an das `select` an (Vergleich ohne Gross/Klein, Trim,
  `asc`/`desc` abgeschnitten) – gilt für `QueryAllAsync`; `QueryPageAsync` liest ohne `select`. Der RealTimeData-Weg (`GetAsync`, eigener URL-Bau)
  ist NICHT abgedeckt – dort heute überall `BelID` in Feldliste und Sortierung. Mit eindeutigem Sortschlüssel im `select` ist die Gruppierung
  wirkungslos und `$totalResults` zählt dieselben Zeilen, die geliefert werden. Deshalb Sortierung immer mit eindeutigem Schlüssel lassen
  (der Einzel-Fix `c505688f` wurde mit `c476a628` zurückgenommen).
- **Warum:** Ein einzelnes Sage-Verhalten auf Prod liess zwei Abgleiche komplett fehlschlagen, obwohl sie am Test grün waren.

## 64. `Dictionary<int,…>` als Action-Parameter braucht `[Bind(Prefix=…)]` (v1.99.69, [[2026-11-25-beschichtungsauftrag-vorschlag-ohne-auswahl-spec]])

- **Problem:** Ein Action-Parameter `Dictionary<int, string>? mengen` ohne festes Präfix bindet nur, solange das Formular Felder `mengen[<id>]` sendet.
  Fehlen sie (Beschichtungsauftrag-Entwurf mit 0 Positionen), fällt der Binder auf das **leere Präfix** zurück, liest die übrigen Formularschlüssel
  (`id`, `rowVersion`, `ziel`, …) als int-Schlüssel und setzt einen ModelState-Fehler unter dem Schlüssel `""`. Die Action meldet dann
  „Eingabe nicht gespeichert: ungültiger Wert in .“ – jedes Speichern des leeren Entwurfs scheiterte (QA-Befund N-1, Altbestand seit v1.99.41).
- **Beleg:** `IdealAkeWms.Tests/ModelBinding/DictionaryIntKeyBindingTests.cs` (echter `ParameterBinder` + `FormValueProvider`, ohne TestServer):
  zwei Formulare ohne Felder → Fehler unter `""`, eins mit Feldern → bindet. Gleicher Testweg wie `NullableIntArrayBindingTests` (§8, `int?[]`).
- **Fix:** `[Bind(Prefix = "mengen")] Dictionary<int, string>? mengen` in `BeschichtungsauftraegeController.Bearbeiten`.
- **Latent:** `StockMovementsController.OutboundAllConfirm(… Dictionary<int,string>? stueckGezeigt …)` hat dasselbe Muster; im Weg ohne FA-Filter
  sendet das Formular keine `stueckGezeigt[..]`-Felder. Heute folgenlos, weil die Action `ModelState.IsValid` nicht prüft – wer dort eine
  ModelState-Prüfung ergänzt, legt den Fehler frei. Folgearbeit [[2026-11-25-folgearbeit-goal-lauf-sage-beschichtung]].
- **Regel:** Jeder `Dictionary<int,…>`- (und `int[]`-Index-)Parameter, dessen Felder im Formular fehlen dürfen, bekommt `[Bind(Prefix = "<name>")]`.
- **Warum:** Der Fehler zeigt sich nur im Randfall „keine Zeilen“ und mit einer Meldung ohne Feldnamen; Tests mit direktem Action-Aufruf binden nicht
  und sehen ihn nie. Nur die echte Bindung belegt ihn.

## 65. Web-Workflow im Dienst: gemeinsame Registrierung, `DienstBenutzer`, 1000-Zeichen-Meldungen (v1.99.70, [[2026-11-25-kommissionierauftraege-nachtrag-nachts-spec]])

- **`ICurrentUserService` im Dienst nur als `DienstBenutzer`.** Der Web-`CurrentUserService` liefert ohne `HttpContext` `"SYSTEM"` und widerspräche
  ADR [[0003-auditableentity-als-entity-basis]] (Dienste schreiben ihren Namen). `DienstBenutzer` gibt den Lauf-Namen zurück; Rollenabfragen
  (`IsAdminAsync` …) **werfen** `NotSupportedException`. **Warum:** Ein Web-Workflow, der im Dienst still eine Rolle prüft, bekäme sonst ein
  erfundenes „ja/nein“ – lieber laut scheitern (Test `KommissionierNachtragServiceTests.DI_…`).
- **Ein DI-Graph, eine Registrierung.** Der Kommissionierauftrags-Workflow hängt an BOM-, Repository- und Cache-Diensten. Web, Dienst und
  `BomDiResolutionTests` registrieren ihn deshalb gemeinsam über `AddKommissionierauftraege`. **Warum:** Getrennte Listen driften auseinander,
  und eine fehlende Abhängigkeit zeigt sich erst zur Laufzeit im Dienst nachts (`ValidateOnBuild` im Test belegt den Graphen).
- **`SyncLogs.Message` hat 1000 Zeichen und wird beim Schreiben nicht gekürzt** (siehe oben, Diagnosen). Meldungen mit Fremdtext (Ausnahmen,
  Artikellisten des Markers) kürzt der Lauf selbst auf 1000. **Warum:** Sonst fehlt genau die Warnzeile, die den Fehler eines FA erklärt.

## 66. Direkt-SQL nach Sage: Fehlerklasse entscheidet über Blockade und Mail (v1.99.71, [[2026-11-24-lagerplatz-anlage-wms-nach-sage-spec]])

- **Problem:** Ein Direkt-SQL-Schreibweg kennt zwei Arten Fehler: Sage ist weg (Timeout, Deadlock, Recht, Netz) – oder genau dieser Datensatz
  passt nicht (THROW, NULL-Pflichtfeld, Konflikt). Behandelt man alles gleich, blockiert entweder ein kaputter Platz die ganze Warteschlange
  oder jeder Takt eines Netzausfalls schreibt einen Protokoll-Lauf und eine Mail.
- **Regel:** Abschließende Liste technischer Fehler an **einer** Stelle (`SqlSageLagerplatzAnlage.IstTechnisch`): technisch = Eintrag zurück auf
  `Offen`, Rest des Takts überspringen, Lauf und Mail nur bei **neuem** Text; alles andere = fachlich am Eintrag (`Fehler`, keine Mail, weiter).
  Neue Fehlernummer = erst einordnen, dann ergänzen (Test der Fehlerklassen in `SageLagerplatzAnlageServiceTests`).
- **Regel:** Hängend `Gesendet` darf hier erneut gesendet werden, **weil** der Befehl in einer Transaktion unter `UPDLOCK, HOLDLOCK` prüft und
  anlegt (idempotent) – anderswo gilt weiter „nie blind neu senden“. Wer den SQL-Text ändert, muss die Idempotenz erhalten.
- **Regel:** Ein im WMS erfasstes Lager fehlt im Sage-Feed, bis der Dienst es angelegt hat. Der Lagerplatz-Abgleich darf es bis zur Bestätigung
  nicht als fehlend markieren (sonst inaktiv, aus der Auswahl verschwunden).
- **Warum:** Direkt-SQL hat keine Sage-Geschäftslogik als Puffer; jeder Fehler landet roh beim WMS. Nur eine harte Trennung hält die Warteschlange
  lauffähig und die Admin-Mailbox lesbar.

## 67. `BdeTeilMeldungen` trägt zwei Arten Meldung: jeder Leseweg muss `WarehouseRequisitionItemId` filtern (v1.99.72, [[2026-11-20-bde-liste-isolierungsfraese-pfad-material-spec]])

- **Problem:** Lagerbestell-Meldungen stehen mit `HauptFA = 0`, `SubFA = 0`, `Position = NULL` in derselben Tabelle wie FA-Teilmeldungen. Ein
  FA-Leseweg ohne Filter zählt sie als Waisen oder als „bereits gemeldet“ für einen FA 0.
- **Regel:** FA-Wege filtern `WarehouseRequisitionItemId IS NULL`, Lagerbestell-Wege `IS NOT NULL`; der gefilterte UNIQUE-Index des FA-Schlüssels
  trägt denselben Filter (SQL/165). Neuer Leseweg = Art ausdrücklich wählen.
- **Regel:** Der Formular-Schlüssel `LB|<Id>` wird vor `TeilSchluessel.Parse` abgetrennt; ein ungültiger Schlüssel weist die ganze Anfrage ab.
- **Warum:** Ein Platzhalter-Wert (0) ist kein Filter – „HauptFA 0 kommt nie vor“ war schon bei `SubFA = 0` falsch (ADR [[0038-teilmeldung-auch-fuer-lagerbestell-positionen]]).

## 68. Kartenansicht per CSS: `display` an Zeilen schlägt Bootstraps `.collapse` (v1.99.73, [[2026-11-25-bde-masken-vereinheitlichung-spec]])

- **Problem:** Die Baugruppen-Teile der BDE-Teileliste sind `<tr class="collapse">`. Eine Kartenregel wie `.bde-tl-seite tr { display: flex }`
  hat höhere Spezifität (0,1,1 + Scope) als Bootstraps `.collapse:not(.show) { display: none }` (0,2,0 nur bei gleichem Scope) und macht damit
  **alle zugeklappten Teile sichtbar** – die Liste sieht am Handy „voll“ aus, obwohl nichts aufgeklappt ist (Mobil-Spec Befund B7).
- **Regel:** Karten-`display` nur an `tr:not(.collapse)` und `tr.collapse.show` setzen, nie an `tr` allgemein. Zellen-`display` **ohne**
  `!important` (B6): die Spaltenauswahl blendet Zellen per Inline-Stil `display:none` aus, `!important` würde sie wieder zeigen.
- **Warum:** Sichtbarkeit steuern hier zwei Mechanismen (Bootstrap-Collapse per Klasse, Zahnrad per Inline-Stil); eine Layout-Regel, die
  `display` setzt, überschreibt beide still. Der Fehler zeigt sich nur unter der Kartenschwelle und nur mit zugeklappten Kinderzeilen –
  kein Test sieht ihn (TS-194.4 Schritt 5 prüft ihn von Hand).

## 69. Mehrere Teilmeldungen je Zeile: „gemeldet“ heißt nur noch „abschließend gemeldet“ (v1.99.75, [[2026-11-25-bde-masken-vereinheitlichung-spec]])

- **Problem:** Seit Etappe 3 hat eine Teileliste-Zeile beliebig viele nicht stornierte Meldungen (Teilfertig, `IstAbschliessend = 0`) und höchstens
  eine abschließende. Ein Leseweg, der wie bisher „gibt es eine Meldung?“ fragt (`FirstOrDefault`, `Any`, `ToDictionary` je Schlüssel), hält eine
  Teilmeldung für den Abschluss, wirft bei doppeltem Schlüssel oder zählt Waisen je Meldung statt je Schlüssel.
- **Regel:** Neuer Leseweg auf `BdeTeilMeldungen` wählt ausdrücklich: **alle** Meldungen (Summe, Liste) oder **nur `IstAbschliessend`**
  (fertig, Doppelschutz, Ausblenden, AG-Status erledigt). Gruppieren statt `ToDictionary`. Zusätzlich gilt §67 (`WarehouseRequisitionItemId`).
- **Regel:** `IstAbschliessend` immer ausdrücklich setzen – kein EF-`HasDefaultValue` (bool-Sentinel), der DB-Default 1 ist nur für den Bestand.
- **Warum:** Der UNIQUE-Index schützt nur noch den Abschluss; Teilmeldungen fängt kein Index mehr ab. Fehler dieser Art fallen erst mit echten
  Teilmeldungen auf, der Bestand (alle = 1) verdeckt sie (ADR [[0037-teilfertigmeldung-nur-abschliessende-meldung-eindeutig]]).

## 70. `maxlength` kürzt still – Längengrenzen meldet der Server (v1.99.79, [[2026-11-24-lagerplatz-anlage-wms-nach-sage-spec]])

- **Problem:** `maxlength="10"` am Feld Lagerkennung schnitt eine 11-stellige Eingabe beim Tippen/Einfügen auf 10 Zeichen ab; das WMS legte
  dann ein anderes Lager an, als der Benutzer gemeint hatte, ohne Meldung (QA v1.99.71, F-7).
- **Regel:** Fachliche Längengrenzen nicht per `maxlength` erzwingen, wenn ein Kürzen die Bedeutung ändert (Codes, Kennungen). Der Server prüft,
  `asp-validation-for` zeigt die Meldung am Feld.
- **Warum:** Melden statt still behandeln – ein gekürzter Code ist ein falscher Code.

## 71. Moq liefert für `Task<List<T>>` null – ein entfernter `?? new List` bricht Tests, nicht Produktion (v1.99.79)

- **Problem:** Nach dem Entfernen des stillen Fallbacks in `PickingController` (F-1, Repository liefert per `ToListAsync` nie null) fielen drei
  `PickingControllerTests` mit `ArgumentNullException`: `Mock<IPickingOrderRepository>` ohne Setup gibt für `Task<List<T>>` `null` zurück
  (Moq `DefaultValue.Empty` füllt nur Arrays/Schnittstellen wie `IEnumerable<T>`, nicht `List<T>`).
- **Regel:** Den Mock aufsetzen (`ReturnsAsync(new List<T>())`), nicht den Produktionscode mit `??` „absichern“.
- **Warum:** Der Fallback verdeckte im Code einen Zustand, den es nur im Test gibt; die Negativaussage „nie null“ ist über den Repository-Weg belegt.
