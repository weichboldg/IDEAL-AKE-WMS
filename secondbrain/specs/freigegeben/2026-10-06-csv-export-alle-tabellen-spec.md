---
type: spec
title: "CSV-Export fuer alle Tabellen (querschnittlich)"
slug: 2026-10-06-csv-export-alle-tabellen-spec
status: Testbereit
created: 2026-10-06
updated: 2026-10-12
source_backlog: "[[2026-10-06-csv-export-alle-tabellen]]"
task: "[[2026-10-12-csv-export-alle-tabellen-umsetzung]]"
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
affected_code:
  - "NEU IdealAkeWms/wwwroot/js/csv-export.js — gemeinsamer Baustein (Antwort 6/8): Knopf je table.filterable-table, Zeilen holen (Server-Mode per fetch pageSize=0, Client-Mode aus der Live-Tabelle), sichtbare Spalten aus dem DOM, Bildschirm-Sortierung, Zellwert-Regeln, Injection-Schutz, CSV (Semikolon, UTF-8-BOM, CRLF), Download, Cap-Warnung"
  - "IdealAkeWms/Views/Shared/_Layout.cshtml Z. 404-407 — csv-export.js einmal nach site.js (Z. 406) mit asp-append-version einbinden"
  - "IdealAkeWms/Views/Shared/_Pagination.cshtml Z. 25-26 — data-total-count + data-capped (= PaginationState.IsCappedAtAll) am .pagination-bar ergaenzen; Banner Z. 6-12 unveraendert"
  - "IdealAkeWms/wwwroot/js/table-filter.js Z. 352-377 — Wertvergleich aus compareRows (Z. 345-378) als window.tableFilterCompareValues(valA, valB, dir) herausloesen; compareRows ruft ihn auf; Verhalten unveraendert"
  - "IdealAkeWms/Views/Picking/Bom.cshtml Z. 871-911 (updateBomVisibility) — je Datenzeile das Filterergebnis filterVisible (Z. 895-906) zusaetzlich als data-csv-filter-match ablegen (Antwort 12: Export unabhaengig vom Klappzustand)"
  - "Bezugspunkte ohne Aenderung: print-visible-columns.js Z. 16 (Sichtbarkeitsregel), column-preferences.js Z. 162-175 / 227 / 382-389, PageSize.cs Z. 12 + 25-30, PaginationState.cs Z. 32-33"
  - "41 Tabellen in 39 View-Dateien (Inventar im Abschnitt Technischer Loesungsentwurf) — keine Aenderung je View erwartet; nur wo noetig data-csv-export=false bzw. data-csv-value"
  - "IdealAkeWms/Views/Help/Index.cshtml (Abschnitt Listen) + Views/Help/Changelog.cshtml, beide AppVersion.cs (naechste freie Version im Buendel, Stand 1.49.0)"
  - "NEU IdealAkeWms.Tests/Helpers/CsvExportCoverageTests.cs — Quelltext-Waechter nach Muster TableScriptScopedSelectorTests.cs (Z. 45)"
  - "docs/TESTSZENARIEN.md + secondbrain/tests/testszenarien-index.md — neues Kapitel TS-<naechste freie> (Stand: hoechstes TS-82)"
open_questions: []
beantwortete_rueckfragen:
  - "1 alle Seiten (Keyuser 2026-10-07)"
  - "2 wie eingeblendet (Keyuser 2026-10-07)"
  - "3 Format wie empfohlen (Gerald 2026-10-09)"
  - "4 flach (Keyuser 2026-10-07)"
  - "5 Sicherheit wie empfohlen (Gerald 2026-10-09)"
  - "6 Skript im Layout (Gerald 2026-10-09)"
  - "7 Umfang wie empfohlen (Gerald 2026-10-09)"
  - "8 Variante A Browser (Gerald 2026-10-09)"
  - "9 erste 5000 + Warnung (Keyuser 2026-10-07)"
  - "10 Bildschirm-Sortierung (Keyuser 2026-10-07)"
  - "11 Checkbox Ja/Nein, Farbe egal (Keyuser 2026-10-07)"
  - "12 Client-Mode alle Filtertreffer, Stueckliste unabhaengig vom Klappzustand (Gerald 2026-10-09)"
  - "13 Warnsatz ergaenzen, sonst nichts bauen (Gerald 2026-10-09)"
  - "14 Restpunkte a-e wie empfohlen (Gerald 2026-10-09)"
epic: false
etappen: []
deploy:
  web: true
  service: false
  migration: false
freigabe_entscheidung: "Freigegeben wie Antworten 1-14: Variante A (Browser, Skript im Layout), alle Filtertabellen, 5000-Kappung mit Warnung inkl. Sortier-Satz (immer, Auslegung bestaetigt 2026-10-12). Umsetzung direkt im Buendel als letzter Lauf nach Teil 4."
freigabe_von: "Gerald Weichbold"
freigabe_am: 2026-10-12
---

> [!info] Umsetzungsort (Entscheidung Gerald 2026-10-09)
> Direkt im Buendel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5` auf Zweig
> `feature/2026-08-07-ideal-teile-1-5` — **nicht** nach Schranke 2 des IDEAL-Buendels aus `main`
> (fruehere Lesart, ueberholt). Kein eigener Worktree, ein Lauf zur Zeit; `worktree`/`branch` im
> Frontmatter setzt der /dev-Lauf. Version und TS-Nummer: jeweils die **naechste freie im Buendel
> zum Umsetzungszeitpunkt** (Stand 2026-10-12: `IdealAkeWms/AppVersion.cs` Z. 5 = **1.49.0**,
> `docs/TESTSZENARIEN.md` hoechstes Kapitel **TS-82**, Z. 9339 → heute waeren es 1.50.0 / TS-83).

> [!note] Code-Grundlage
> Alle Datei:Zeile-Angaben beziehen sich auf den Buendel-Worktree
> `.claude/worktrees/2026-08-07-ideal-teile-1-5` (Stand v1.49.0, am 2026-10-12 neu verifiziert,
> gelesen, nicht veraendert). Pfade relativ zu `IdealAkeWms/`, sofern nicht anders angegeben.

## Ziel / Nutzen (das Warum)

Anwender in Lager, Leitstand und Verwaltung uebertragen Listeninhalte heute per Hand oder per
Kopieren aus dem Browser nach Excel. Ein einheitlicher CSV-Export liefert genau den Ausschnitt,
den der Anwender gerade gefiltert hat und sieht, ohne Medienbruch. Weil er querschnittlich ist,
muss er **ein** Baustein sein. 40 Einzelloesungen waeren 40 Stellen, an denen Spalten, Filter oder
Rechte auseinanderlaufen. Dieselbe Fehlerklasse hat beim Druck schon drei synchron zu haltende
Stellen erzeugt (siehe [[fallstricke]], „Lagerbestellungs-Druck: drei Stellen synchron halten“).

## Umfang (In-Scope / Out-of-Scope)

**In-Scope (Antwort 7, eine Spec, kein Teil-Schnitt):**
- Ein CSV-Export-Knopf an jeder Tabelle mit der Klasse `filterable-table` (**41** Tabellen in **39**
  View-Dateien, Inventar unten; Erstfassung: 40 in 38, neu ist `FaHierarchyBeschichtung/Summiert`
  aus v1.48.0). Das gilt fuer AKE- und IDEAL-Listen gleichermassen, also bei Master
  `ProduktionsauftragHierarchisch` `false` und `true`.
- Export aller Filtertreffer ueber alle Seiten (Antwort 1) mit den eingeblendeten Spalten
  (Antwort 2), in Bildschirm-Sortierung (Antwort 10), Excel-tauglich (Antwort 3).
- Gruppierte Listen flach (Antwort 4), Gruppenschluessel als Spalte „HauptFA“ (Antwort 14 b).
- CSV-Injection-Schutz mit Zahlen-Ausnahme und Rechtegleichheit mit der Ansicht (Antwort 5).
- Client-Mode-Listen: alle Filtertreffer, Stueckliste unabhaengig vom Klappzustand (Antwort 12).
- Hilfeseite (Abschnitt Listen) und Anwender-Changelog, Testszenario-Kapitel, additiver Nachtrag
  in ADR 0005.

**Out-of-Scope (Antwort 7):**
- Baumansichten ohne `filterable-table`: `FaHierarchy/Index.cshtml` Z. 103 (`FaHierarchyStructure`),
  `Tracking/Index.cshtml` Z. 72 (hierarchischer Zweig, `trackingTree`), `Tracking/OseonIndex.cshtml`
  Z. 100 (`OseonTracking`). ADR 0005 fuehrt BOM-Tree und Tracking/Index ausdruecklich als
  Ausnahmen. Fuer diese Ansichten gilt: Folge-Backlog, falls gewuenscht.
- Formular-, Detail- und Einstellungstabellen ohne Listencharakter, z. B. `Settings/Index`,
  `ServiceSettings/Index`, `Users/RoleOverview`, `Articles/Info`, `*/Edit`,
  `StockMovements/Inbound*|LocationTransfer|OutboundAll`, `BdeTerminal/Index`, alle `Print*`-Views
  und `FaHierarchy/_FaWorkOperationsModal`.
- Excel-Format (`.xlsx`): Im Web-Projekt gibt es keine Excel-Bibliothek. `IdealAkeWms.csproj`
  Z. 10-20 enthaelt nur BarcodeLib, EF Core, Serilog, SkiaSharp und DirectoryServices. Eine neue
  Abhaengigkeit braeuchte eine eigene Entscheidung.
- Import (CSV → WMS).

## Fachliche Anforderungen

1. Jede Tabelle im Scope zeigt einen Knopf „CSV“ an derselben Stelle: im Toolbar-Host ueber der
   Tabelle, neben dem Spaltenauswahl-Zahnrad, wo es eines gibt (Antwort 6: ein Skript im Layout).
2. Der Export enthaelt die Zeilen, die die Ansicht mit dem **aktuellen** Filterstand liefert
   (Filterkarte + Spaltenfilter `colf_*` + Ansichtsschalter wie `showDone`/`target`). Das gilt ueber
   **alle Seiten** (Antwort 1), begrenzt durch `PageSize.AllCap` (Antwort 9).
3. Der Export enthaelt die Spalten **wie eingeblendet** (Antwort 2): genau die beim Klick sichtbaren
   Spalten in Bildschirm-Reihenfolge. Kopfzeile = sichtbare Spaltenueberschriften. Spalten ohne
   Ueberschrifttext (Aktionen, Knoepfe, Bulk-Checkbox) entfallen (Antwort 14 a).
4. Der Export zeigt nie mehr als die Ansicht: weder Zeilen, die der Anwender mit seinem Recht nicht
   sieht, noch ausgeblendete Spalten. Export-Recht = Leserecht der Ansicht, kein eigenes Recht,
   kein eigener Feature-Toggle, keine Export-Protokollierung (Antwort 5 b, c).
5. Format (Antwort 3): Trenner Semikolon, UTF-8 **mit BOM**, Zeilenende CRLF, **keine** `sep=;`-Zeile,
   Zahlen mit Dezimalkomma wie angezeigt, Datum `dd.MM.yyyy` **ohne** KW-Suffix, Dateiname
   `<Ansicht>_yyyy-MM-dd_HHmm.csv`. Die Datei oeffnet sich per Doppelklick in Excel (Windows,
   deutsches Gebietsschema) mit korrekten Umlauten, getrennten Spalten und als Zahl erkannten
   Mengen.
6. Gruppierte Listen werden **flach** exportiert (Antwort 4): keine Gruppen-Kopfzeilen, keine
   Kombigeraete-Variantenzeile als Zeile. Wo der Gruppenschluessel nur in `tbody[data-group]` steht
   und keine `hauptfa`-Spalte sichtbar ist, steht er als erste Spalte „HauptFA“ (Antwort 14 b).
7. CSV-Injection (Antwort 5 a): Zellinhalte, die mit `=`, `+`, `-`, `@`, Tab oder CR beginnen,
   erhalten ein fuehrendes `'`. Ausgenommen sind Werte, die **vollstaendig** eine Zahl im
   Anzeigeformat sind (z. B. `-5,000`, `-1.234,500`), damit negative Mengen Zahlen bleiben.
8. Ist die Treffermenge gekappt, werden die **ersten 5000** exportiert, und vor oder mit dem
   Download erscheint ein **sichtbarer Hinweis** (Antwort 9) mit dem Zusatzsatz „Sortierung gilt nur
   innerhalb der exportierten 5.000“ (Antwort 13). Ein stilles Abschneiden ist ausgeschlossen
   (Hausregel „Melden statt still behandeln“). „Export sperren“ und serverseitiges Sortieren sind
   verworfen (Antworten 9, 13).
9. Der Export uebernimmt die **aktive Bildschirm-Sortierung** (Antwort 10): Spalte und Richtung des
   Spaltenkopfs mit Sortierindikator, auch wenn sie aus der Default-Sortierung der Spaltenauswahl
   bzw. `data-fallback-sort-column` stammt. Sortiert wird ueber **alle** exportierten Zeilen, bei
   Gruppenlisten innerhalb jeder Gruppe, Gruppenreihenfolge wie vom Server. Ohne aktive Sortierung
   gilt die Serverreihenfolge. Variante „immer Serverreihenfolge“ ist verworfen.
10. Checkboxen in Zellen werden als **„Ja“/„Nein“** exportiert (Antwort 11). Nur farblich
    transportierte Kennzeichen (Zeilenklassen wie `table-secondary`/`table-danger`) werden **nicht**
    exportiert (Antwort 11: „farbige Kennzeichen egal“).
11. Weitere Zellwerte (Antwort 14 c-e): Badges sind Teil des Zellwerts; ein Eingabefeld liefert
    seinen Wert, ein Select den Text der Auswahl; eine reine Icon-Zelle liefert `title` bzw.
    `aria-label`, sonst leer. Eine Zelle mit `data-csv-value` liefert diesen Wert (Nachschaerfung
    je Liste, nur wo noetig).
12. Client-Mode-Listen (ohne `data-server-column-filter`) exportieren alle Zeilen, die der
    Spaltenfilter nicht ausblendet; in der Stueckliste unabhaengig davon, ob ein Baumknoten
    zugeklappt ist (Antwort 12). Listen ohne aktiven Spaltenfilter exportieren alle Zeilen.

## Technischer Loesungsentwurf

### Ausgangslage (belegt)

**Es gibt keinen CSV- oder Excel-Export.** Gesucht wurde im Buendel-Worktree unter `IdealAkeWms/`
(`*.cs`, `*.cshtml`, `*.js`, `*.csproj`, ohne `wwwroot/lib/`) nach dem Muster
`text/csv|\.csv\b|ExportCsv|toCsv|CsvHelper|ClosedXML|EPPlus|xlsx|Export`. Es gab nur zwei
Kommentartreffer, beide ohne Funktion:
- `wwwroot/js/fa-liste-wiederholung.js` Z. 6: „damit … ein spaeterer Export den Wert weiterhin sehen“.
  Unterdrueckte Wiederholungswerte bleiben also im DOM, nur unsichtbar.
- `wwwroot/css/site.css` Z. 1261: derselbe Hinweis.

Es gibt auch keine JS-Testinfrastruktur: kein `package.json`, keine `*.test.js` und keine
`jest.config*` im Worktree.

**Filterzustand liegt vollstaendig in der URL.** Server-Mode-Filter navigieren zu `?colf_<key>=…`
(`table-filter.js` Z. 45-64, „In Server-Filter-Mode ist die URL die Source-of-Truth“, Z. 24). Die
Pagination setzt `?page=`/`?pageSize=` und laesst alle anderen Parameter stehen (`site.js` Z. 91-129).
Alle paginierten Listen-Actions lesen `pageSize` ueber `PageSize.Resolve(pageSize, …)`. Grep
`PageSize\.Resolve\(` in `Controllers/` ergibt (v1.49.0) 36 Treffer in 32 Controllern (Erstfassung:
35), darunter auch `Summiert` (`FaHierarchyKommissionierListenController.cs` Z. 115,
`FaHierarchyVormontageController.cs` Z. 108, neu `FaHierarchyBeschichtungController.cs` Z. 152),
`TrackingController.cs` Z. 308 (OseonIndex) und `FaHierarchyController.cs` Z. 60, z. B.
`ProductionOrdersController.cs` Z. 82. Keine Index-Action speichert `pageSize` als Benutzer-Default:
`DefaultPageSize` wird nur in `AccountController.cs` Z. 211 und `UsersController.cs` Z. 125/299
geschrieben, alle Listen lesen es nur (`GetDefaultPageSizeAsync`). Der zweite Aufruf mit
`pageSize=0` veraendert also nicht die Seitengroesse des Anwenders.
`pageSize=0` bedeutet „Alle“ und wird auf `AllCap = 5000` gekappt (`Services/PageSize.cs`
Z. 12-30). In Gruppenlisten paginiert der Server ueber **Gruppen**, nicht ueber Zeilen
(`ProductionOrdersController.cs` Z. 249-252, 376 und 402).

**Spaltensichtbarkeit und -reihenfolge leben im DOM.** `column-preferences.js` blendet per
`style.display` aus (Z. 162-175) und ordnet Zellen physisch um (Z. 221-230, `appendChild` Z. 227).
Der Druck liest die sichtbaren `th[data-col-key]` zur Klickzeit (`print-visible-columns.js` Z. 14-19,
Regel Z. 16 `style.display !== 'none' && !classList.contains('d-none')`). Die Regel ist eine
Inline-Bedingung im Klick-Handler, keine aufrufbare Funktion; dieselbe Bedingung steht ein zweites
Mal in `Picking/Bom.cshtml` Z. 1275. Diese Konvention stammt aus
[[2026-09-25-kommissionierliste-summierung-pdf-spec]], Entscheidung B1. Die vom Server
ausgelieferte HTML-Fassung hat die **Serverreihenfolge** und **keine** angewandten Praeferenzen.

**Sortierung ist rein clientseitig** und wird nicht in der URL gefuehrt. `sortTable`/`compareRows`
stehen in `table-filter.js` Z. 333-345, die Sortierung laeuft je `<tbody>` getrennt (siehe
[[fallstricke]], Abschnitt Sort-Bug). Ein Export ueber alle Seiten kann die Bildschirmreihenfolge
daher nur durch Nachsortieren treffen (Antwort 10: uebernehmen). Keine Index-Action nimmt einen
Sortierparameter entgegen (grep `(string|int)\?? (sort|sortBy|sortColumn|orderBy|sortDir)` in
`Controllers/`: einziger Treffer `WarehousePickingController.Print` Z. 359, `sortCol`/`sortDir`).

**Zahlenformat haengt teils an der Server-Kultur.** `Program.cs` enthaelt weder
`RequestLocalization` noch eine Kultur-Festlegung (grep `RequestLocalization|CultureInfo\(|DefaultThreadCurrentCulture|de-AT|de-DE`
ohne Treffer). In `*.json`/`*.config` von `IdealAkeWms/` steht ebenfalls keine Kultur. Manche Views
formatieren mit fester Kultur, z. B. `FaHierarchyKommissionierListen/Index.cshtml` Z. 6 und 148:
`ToString("N3", deCulture)`. Andere nutzen die Umgebungskultur, z. B.
`ProductionOrders/_ProductionOrderRow.cshtml` Z. 66 und `StockMovements/Index.cshtml` Z. 111:
`ToString("N3")`. **Unverifiziert:** Welches Dezimalzeichen die Umgebungskultur auf dem
IIS-Server liefert, ist hier nicht zu sehen. Pruefschritt siehe Abschnitt Deploy.

**Zellen enthalten mehr als den Wert.** Belege aus `ProductionOrders/_ProductionOrderRow.cshtml`:
- Die FA-Nr.-Zelle enthaelt die Badges „HauptFA“, „In Sage gelöscht“ und „vermisst seit …“ sowie
  enaio-Badges (Z. 47-65).
- Die Werkbank-Zelle enthaelt das Badge „BG n“ (Z. 89-96).
- Die Lack-T-Zelle ist ein reines SVG-Icon mit `title` bzw. „–“ (Z. 113-122).
- Der Status „erledigt“ steht nur in der Zeilenklasse `table-secondary` (Z. 21).
- Datumszellen haben die Form `dd.MM.yyyy KWnn` (Z. 6-11).

Im Leitstand stehen Glas, Fremdbezug und Lack als **Checkboxen** in der Zelle
(`PickingLeitstand/_PickingLeitstandRow.cshtml` Z. 108-132), die Prioritaet als `<input type="number">`
(Z. 255). Deren `textContent` ist leer (geregelt durch Antwort 11 bzw. 14 d).

**Gruppenstruktur:** Je Gruppe gibt es ein `<tbody>`. Die FA-Listen-Familie traegt den
HauptFA-Schluessel **nur** in `tbody[data-group]`: `ProductionOrders/Index.cshtml` Z. 137,
`PickingLeitstand/Index.cshtml` Z. 183, `FaCompletion/Index.cshtml` Z. 96,
`FaWorklist/Index.cshtml` Z. 131, `Picking/Index.cshtml` Z. 82 und `Tracking/Index.cshtml` Z. 90.
Die Zeile selbst zeigt dort die Sub-FA-Nummer (`_ProductionOrderRow.cshtml` Z. 13). Die
IDEAL-FaHierarchy-Listen haben dagegen eine eigene Spalte `hauptfa`
(`FaHierarchyKommissionierListen/Index.cshtml` Z. 75, `Summiert.cshtml` Z. 81,
`FaHierarchyBeschichtung/Index.cshtml` Z. 49 (Erstfassung: Z. 38), neu
`FaHierarchyBeschichtung/Summiert.cshtml` Z. 53, `FaHierarchyVormontage/Index.cshtml` Z. 75).
Gruppenkoepfe sind `td[colspan]`-Zeilen (z. B. `ProductionOrders/Index.cshtml` Z. 138-168,
`FaHierarchyKommissionierListen/Index.cshtml` Z. 103-140, `FaHierarchyBeschichtung/Summiert.cshtml`
Z. 78-92 mit PDF-Knopf und Briefkopf-Partial `_BeschichtungKopf` aus v1.49.0). Die
Kombigeraete-Variantenzeile enthaelt eine **verschachtelte Tabelle** (`ProductionOrders/Index.cshtml`
Z. 176-203, `<table>` Z. 179); ebenso der flache Tracking-Zweig (`Tracking/Index.cshtml` Z. 242).
Hier gilt die `:scope >`-Regel aus [[fallstricke]].

**Client-Filter blendet per `style.display` aus.** `table-filter.js` `applyFilters` (Z. 293-329)
setzt je `:scope > tr` des ersten `tbody` `row.style.display` (Z. 326). In der Stueckliste
ueberschreibt `updateBomVisibility` (`Picking/Bom.cshtml` Z. 871-911) dieselbe Eigenschaft mit
`treeVisible && filterVisible` (Z. 908-909). Ob eine unsichtbare Stuecklisten-Zeile am Filter oder am
zugeklappten Knoten scheitert, ist am DOM deshalb **nicht** ablesbar; das Filterergebnis
`filterVisible` (Z. 895-906) existiert nur als lokale Variable.

**Zwei Inventar-Tabellen haben die Klasse, aber keinen Filter.** `OrderRecipients/ArticleGroupMappings.cshtml`
(Z. 28) und `OseonReporting/_OseonReportingTable.cshtml` (Z. 4) tragen `filterable-table`, binden
aber `table-filter.js` nicht ein (grep `table-filter.js` unter `Views/`: 38 Dateien, beide fehlen;
`ArticleGroupMappings.cshtml` hat gar keinen `<script>`). Ein Filter, eine Sortierung oder eine
Spaltenauswahl ist dort also nicht wirksam. Das Partial wird in `OseonReporting/OperationsOverview.cshtml`
Z. 161 **je Zukunftstag-Gruppe** und Z. 166 fuer die Hauptliste gerendert, also mehrere Tabellen auf
einer Seite. (Erstfassung fuehrte beide als „Client-Spaltenfilter“, ueberholt.)

### Abgleich Keyuser-Antworten ↔ Code (Rework 2026-10-07)

Geprueft im Buendel-Worktree (Bezug dieser Spec) und gegen `main`. Ergebnis: alle sechs Antworten
sind mit dem Bestand umsetzbar; die Wechselwirkung (9 × 10) war Rueckfrage 13 und ist durch
Antwort 13 (Warnsatz) geregelt. Zeilenangaben am 2026-10-12 gegen v1.49.0 bestaetigt.

| Antwort | Bestand (Beleg) | Vereinbar? |
|---|---|---|
| 1 alle Seiten | `pageSize=0` → `PageSize.Resolve` liefert `AllCap` 5000 (`Services/PageSize.cs` Z. 12, 25-30, in `main` identisch); Pagination laesst Filterparameter stehen (`site.js` Z. 91-129) | ja |
| 2 wie eingeblendet | Spaltenauswahl wird **nicht** in sessionStorage gehalten, sondern je Benutzer serverseitig unter `/api/user-view-preferences/<viewKey>` (`column-preferences.js` Z. 25-27, 305-337; Speichern verzoegert um 1500 ms, Z. 20/297-303) und beim Laden ins DOM angewandt (Z. 162-175, 227). sessionStorage nutzt nur `table-filter.js` fuer **Filterwerte** im Client-Mode (Z. 18-37). Massgeblich ist deshalb der DOM-Zustand zur Klickzeit, nicht das gespeicherte Profil; das deckt auch die noch nicht gespeicherte Aenderung ab (AK 3). Tabellen ohne `#view-config` (Inventar, Prefs = nein) zeigen immer alle Spalten. Keine `<th>` im Scope traegt eine responsive `d-none`-Klasse (grep `<th[^>]*d-none` unter `Views/`: kein Treffer) | ja |
| 4 flach | `:scope > tbody > tr` ohne `td[colspan]`, wie `sortTable` (`table-filter.js` Z. 392-394) | ja |
| 9 erste 5000 + Warnung | `PaginationState.IsCappedAtAll` = `PageSizeRaw == 0 && TotalCount > AllCap` (`PaginationState.cs` Z. 32-33); die abgerufene `pageSize=0`-Antwort enthaelt den Banner `_Pagination.cshtml` Z. 6-12 bereits | ja; Wechselwirkung mit 10 durch Warnsatz geregelt (Antwort 13) |
| 10 Bildschirm-Sortierung | rein clientseitig je `tbody` (`table-filter.js` Z. 333-406); Zustand in `th[data-sort-dir]`; Comparator nur als innere Funktion (Z. 345) → Herausloesen noetig (Loesung Schritt 9). Am Bildschirm sortiert die Klick-Sortierung im Server-Mode nur die **aktuelle Seite**; der Export sortiert dagegen alle Treffer. Das ist die gewollte Lesart „sortiert korrekt“, nicht eine Seite-fuer-Seite-Kopie | ja, mit Refactor |
| 11 Checkbox Ja/Nein, Farbe egal | Checkboxen im Leitstand `_PickingLeitstandRow.cshtml` Z. 108-132, Zeilenstatus nur als Klasse `_ProductionOrderRow.cshtml` Z. 21 | ja |

> [!note] Ueberholt (2026-10-12)
> Die Umsetzung startet **nicht** aus `main` nach dem Buendel-Merge, sondern direkt im Buendel
> (Entscheidung Gerald 2026-10-09, siehe Kasten oben). Die folgende Liste ist nur noch Hintergrund:
> Sie zeigt, warum der Export den Buendel-Stand braucht (gruppenweises `sortTable`,
> `print-visible-columns.js`). Die Buendel-Zeilenangaben zu `table-filter.js`, `column-preferences.js`,
> `_Pagination.cshtml` und `PageSize.cs` sind am 2026-10-12 gegen v1.49.0 unveraendert bestaetigt.

**Unterschiede `main` ↔ Buendel-Worktree** (Stand 2026-10-07; damals relevant, weil die Umsetzung
nach dem Buendel-Merge aus `main` starten sollte — *ueberholt*):
- `table-filter.js`: In `main` sortiert `sortTable` (Z. 314-358) nur das **erste** `tbody`
  (`_tbody`), ohne `:scope >`, mit anonymem Inline-Comparator (Z. 322-353); die Gruppensortierung je
  `tbody`, der Name `compareRows`, die Warnung B-2 und das Event `table-sorted` gibt es nur im
  Buendel (Z. 333-406). Antwort 10 „je Gruppe“ setzt den Buendel-Stand voraus.
- `print-visible-columns.js` existiert in `main` **nicht** (Glob ohne Treffer), nur im Buendel. Die
  wiederverwendete Sichtbarkeitsregel (Variante A Schritt 2) setzt ebenfalls den Buendel-Merge
  voraus.
- `column-preferences.js`: gleiche Logik, Zeilen in `main` um 9 nach oben verschoben (Ausblenden
  Z. 153-166, Zahnrad-Einfuegeort Z. 378-379, Speichern Z. 298).
- `_Pagination.cshtml` und `PageSize.cs`: in den zitierten Zeilen identisch.

### Inventar der Tabellen (Buendel-Stand v1.49.0, verifiziert 2026-10-12)

Legende: **reg.** = `viewKey` in `ColumnDefinitions.GetByViewKey` (`Models/ViewModels/ColumnDefinitions.cs`
Z. 492-511; Erstfassung Z. 467-485). **Prefs** = `#view-config`-Block vorhanden, also wirksame
Spaltenauswahl. **Gr.** = gruppiert (mehrere `<tbody>`). Zugriff = Klassen-Attribut am Controller,
ausser wo eine Aktion genannt ist. Eine mit Gate markierte Zeile hat zusaetzlich ein Feature-Gate.

**Aenderungen gegenueber der Erstfassung (Stand 1.47.2 → 1.49.0):** neu Nr. **9a**
`FaHierarchyBeschichtung/Summiert` (v1.48.0, „Summiert als Standard“, TS-81). Entfallen ist keine
Liste. Zeilen verschoben: Nr. 9 (Tabelle Z. 35 → 46, Pagination 124 → 109, Prefs 126 → 111, Gate-Zeilen
27-28 → 28-29), Nr. 36 Stueckliste (Z. 142 → 144, Prefs 655 → 657), Nr. 37 Lagerbestellung-Details
(Z. 55 → 60, Prefs 187 → 192), alle `reg.`-Zeilen um +25. Nr. 39 und 40 waren faelschlich als
Client-Filter gefuehrt (siehe Ausgangslage).

**Server-Spaltenfilter + Pagination (36 Tabellen):**

| # | View (Tabelle) | data-view-key | reg. / Prefs | Pagination | Gr. | Zugriff |
|---|---|---|---|---|---|---|
| 1 | `ProductionOrders/Index.cshtml` Z. 75 | ProductionOrders | ja Z. 494 / ja Z. 238 | Z. 236 | hier. | `ProductionOrdersController.cs` Z. 12 `RequirePickingOrTrackingOrLeitstandAccess` |
| 2 | `PickingLeitstand/Index.cshtml` Z. 101 | PickingLeitstand | ja Z. 495 / ja Z. 386 | Z. 239 | hier. | `PickingLeitstandController.cs` Z. 11 `RequirePickingOrLeitstandAccess` |
| 3 | `FaCompletion/Index.cshtml` Z. 63 | FaCompletion | ja Z. 496 / ja Z. 146 | Z. 144 | hier. | `FaCompletionController.cs` Z. 20 `RequireFaCompletionAccess` |
| 4 | `FaWorklist/Index.cshtml` Z. 99 | FaWorklist | ja Z. 497 / ja Z. 181 | Z. 179 | hier. | `FaWorklistController.cs` Z. 20 `RequireVorbauAccess` |
| 5 | `Picking/Index.cshtml` Z. 48 | Picking | ja Z. 498 / ja Z. 116 | Z. 113 | hier. | `PickingController.cs` Z. 118 (Aktion) `RequirePickingAccess` |
| 6 | `BdeBookings/Index.cshtml` Z. 82 | BdeBookings | ja Z. 501 / **nein** (kein Config-Block → `column-preferences.js` `readConfig` Z. 844 bricht ab) | Z. 192 | – | `BdeBookingsController.cs` Z. 12-13 `RequireBdeActive` (Gate) + `RequireBdeShiftleadAccess` |
| 7 | `FaHierarchyKommissionierListen/Index.cshtml` Z. 72 | FaHierarchyKommissionierListen | ja Z. 503 / ja Z. 165 | Z. 163 | ja | `FaHierarchyKommissionierListenController.cs` Z. 30-31 `RequireLagerProcessingAccess` + Gate |
| 8 | `FaHierarchyKommissionierListen/Summiert.cshtml` Z. 78 | FaHierarchyKommissionierSummiert | ja Z. 504 / ja Z. 136 | Z. 134 | ja | wie 7 |
| 9 | `FaHierarchyBeschichtung/Index.cshtml` Z. 46 | FaHierarchyBeschichtung | ja Z. 505 / ja Z. 111 | Z. 109 | ja | `FaHierarchyBeschichtungController.cs` Z. 28-29 `RequireBeschichtungsauftragAccess` + `RequireFaHierarchyBeschichtungAktiv` (Gate) |
| 9a | `FaHierarchyBeschichtung/Summiert.cshtml` Z. 50 (**neu** seit Erstfassung; Gruppenkopf mit Briefkopf Z. 78-92) | FaHierarchyBeschichtungSummiert | ja Z. 506 / ja Z. 121 | Z. 119 | ja | wie 9 (Aktion `Summiert` Z. 147) |
| 10 | `FaHierarchyVormontage/Index.cshtml` Z. 72 | FaHierarchyVormontageEinzeln | ja Z. 507 / ja Z. 150 | Z. 148 | ja | `FaHierarchyVormontageController.cs` Z. 29-30 `RequireVorbauAccess` + Gate |
| 11 | `FaHierarchyVormontage/Summiert.cshtml` Z. 77 | FaHierarchyVormontageSummiert | ja Z. 508 / ja Z. 109 | Z. 107 | – | wie 10 |
| 12 | `Tracking/Index.cshtml` Z. 193 (nur flacher Zweig) | Tracking | nein / nein | Z. 306 | – | `TrackingController.cs` Z. 12 `RequireTrackingAccess` |
| 13 | `Articles/Index.cshtml` Z. 59 | – | – | Z. 132 | – | `ArticlesController.cs` Z. 10 `RequireMasterDataReadAccess` |
| 14 | `StockOverview/Index.cshtml` Z. 71 | – | – | Z. 155 | – | `StockOverviewController.cs` Z. 10 `RequireStockReadAccess` |
| 15 | `StockMovements/Index.cshtml` Z. 78 | – | – | Z. 153 | – | `StockMovementsController.cs` Z. 39 (Aktion) `RequireStockReadAccess` |
| 16 | `StorageLocations/Index.cshtml` Z. 63 | – | – | Z. 152 | – | `StorageLocationsController.cs` Z. 11 `RequireMasterDataReadAccess` |
| 17 | `SageBookingQueue/Index.cshtml` Z. 51 | – | – | Z. 128 | – | `SageBookingQueueController.cs` Z. 15 `RequireStockReadAccess` |
| 18 | `PartRequisitions/Index.cshtml` Z. 60 | – | – | Z. 189 | – | `PartRequisitionsController.cs` Z. 10 `RequirePickingOrStockAccess` |
| 19 | `MissingParts/Index.cshtml` Z. 88-90 | MissingParts | nein / nein | Z. 127 | – | `MissingPartsController.cs` Z. 10-11 `RequireStockOrLagerbestellungAccess` + Gate |
| 20 | `MissingPartsLager/Index.cshtml` Z. 71-73 | MissingPartsLager | nein / nein | Z. 112 | – | `MissingPartsLagerController.cs` Z. 10-11 `RequireLagerProcessingAccess` + Gate |
| 21 | `WarehouseRequisitions/Index.cshtml` Z. 78 | WarehouseRequisitions | nein / nein | Z. 116 | – | `WarehouseRequisitionsController.cs` Z. 10-11 + Gate |
| 22 | `WarehousePicking/Index.cshtml` Z. 60 | WarehousePicking | nein / nein | Z. 99 | – | `WarehousePickingController.cs` Z. 10-11 `RequireLagerProcessingAccess` + Gate |
| 23 | `SyncLog/Index.cshtml` Z. 38 | SyncLog | nein / nein | Z. 73 | – | `SyncLogController.cs` Z. 11 `RequireAdminAccess` |
| 24 | `Users/Index.cshtml` Z. 26 | Users | nein / nein | Z. 85 | – | `UsersController.cs` Z. 10 `RequireAdminAccess` |
| 25 | `Roles/Index.cshtml` Z. 22 | Roles | nein / nein | Z. 64 | – | `RolesController.cs` Z. 10 `RequireAdminAccess` |
| 26 | `Workstations/Index.cshtml` Z. 22 | Workstations | nein / nein | Z. 67 | – | `WorkstationsController.cs` Z. 10 `RequireAdminAccess` |
| 27 | `WorkSteps/Index.cshtml` Z. 22 | WorkSteps | nein / nein | Z. 81 | – | `WorkStepsController.cs` Z. 9 `RequireMasterDataReadAccess` |
| 28 | `ProductionWorkplaces/Index.cshtml` Z. 22 | ProductionWorkplaces | nein / nein | Z. 99 | – | `ProductionWorkplacesController.cs` Z. 12 `RequireMasterDataReadAccess` |
| 29 | `OrderRecipients/Index.cshtml` Z. 25 | OrderRecipients | nein / nein | Z. 70 | – | `OrderRecipientsController.cs` Z. 10 `RequireMasterDataReadAccess` |
| 30 | `ArticleAttributes/Index.cshtml` Z. 27 | ArticleAttributes | nein / nein | Z. 164 | – | `ArticleAttributesController.cs` Z. 9 `RequireMasterDataReadAccess` |
| 31 | `ArticleCategories/Index.cshtml` Z. 27 | ArticleCategories | nein / nein | Z. 102 | – | `ArticleCategoriesController.cs` Z. 9 `RequireMasterDataReadAccess` |
| 32 | `FaAttributes/Index.cshtml` Z. 27 | FaAttributes | nein / nein | Z. 148 | – | `FaAttributesController.cs` Z. 16 `RequireMasterDataReadAccess` |
| 33-35 | `BdeMasterData/Index.cshtml` Z. 40 / 92 / 142 (je Tab-Request nur eine, [[fallstricke]] „Eine filterbare Tabelle pro gerenderter Seite“) | BdeMasterDataOperators / -Activities / -Terminals | nein / nein | Z. 177 | – | `BdeMasterDataController.cs` Z. 10-11 `RequireBdeActive` (Gate) + `RequireBdeShiftleadAccess` |

**Ohne Server-Filter, ohne Pagination (5 Tabellen; 36-38 mit Client-Spaltenfilter, 39-40 ohne
jeden Filter):**

| # | View (Tabelle) | data-view-key | reg. / Prefs | Filter | Zugriff |
|---|---|---|---|---|---|
| 36 | `Picking/Bom.cshtml` Z. 144 (Baum + Filter, ADR-0005-Ausnahme „BOM-Tree“) | Bom | ja Z. 500 / ja Z. 657 | Client, Sichtbarkeit ueber `updateBomVisibility` Z. 871-911 | `PickingController.cs` Z. 279 (Aktion `Bom`) `RequirePickingAccess` |
| 37 | `WarehousePicking/Details.cshtml` Z. 60 | WarehousePickingDetails | ja Z. 502 / ja Z. 192 | Client | `WarehousePickingController.cs` Z. 10-11 |
| 38 | `Tracking/ByWorkplace.cshtml` Z. 55 | TrackingByWorkplace | nein / nein | Client | `TrackingController.cs` Z. 12 |
| 39 | `OrderRecipients/ArticleGroupMappings.cshtml` Z. 28 | – | – | **keiner** (kein `table-filter.js`) | `OrderRecipientsController.cs` Z. 10 |
| 40 | `OseonReporting/_OseonReportingTable.cshtml` Z. 4 (mehrfach je Seite: `OperationsOverview.cshtml` Z. 161 + 166) | – | – | **keiner** (kein `table-filter.js`) | `OseonReportingController.cs` Z. 11 `RequireReportingAccess` |

Herkunft (2026-10-12 neu gezaehlt): grep `filterable-table` und `data-server-column-filter` unter
`Views/` (41 Tabellen, ohne den Changelog-Texttreffer `Help/Changelog.cshtml` Z. 1822 und den
JS-Selektor `WarehousePicking/Details.cshtml` Z. 316), `_Pagination` unter `Views/` und
`id="view-config"` unter `Views/` (15 Treffer, Erstfassung 14; davon `FaHierarchy/Index` und `Tracking/OseonIndex`
ausserhalb des Scopes). Nicht registrierte `data-view-key` sind fuer den
Export unschaedlich. Sie dienen nur der sessionStorage-Filterpersistenz in `table-filter.js`
Z. 18-24 und brauchen **keine** Registrierung, solange die Liste keine Spaltenauswahl bekommt.

### Loesung (entschieden: Erzeugung im Browser aus derselben Index-Action, Antwort 8)

Ein Skript `wwwroot/js/csv-export.js` wird einmal in `Views/Shared/_Layout.cshtml` nach `site.js`
(Z. 406) und vor `RenderSectionAsync("Scripts")` (Z. 407) mit `asp-append-version="true"` eingebunden
(Antwort 6) und arbeitet so:
1. Es haengt an jede `table.filterable-table` ohne `data-csv-export="false"` einen Knopf „CSV“
   (`btn btn-sm btn-outline-secondary`, sichtbarer Text „CSV“ plus `title`/`aria-label`
   „Als CSV exportieren“). Einfuegeort nach demselben Mechanismus wie das Zahnrad
   (`column-preferences.js` `insertGearButton` Z. 367-389): ins `previousElementSibling` der
   `.table-responsive`, wenn es ein `div` ist, sonst ein eigener `d-flex justify-content-end mb-1`-
   Container davor. Es wird **nichts** zwischen Toolbar-Host und `.table-responsive` eingefuegt
   ([[fallstricke]] §19). Mehrere Tabellen auf einer Seite (Nr. 40, `OperationsOverview.cshtml`
   Z. 161/166; BdeMasterData rendert je Tab-Request nur eine) bekommen je einen eigenen Knopf, der
   nur seine Tabelle exportiert.
2. Beim Klick liest es die **sichtbaren** `thead th[data-col-key]` der Live-Tabelle in
   DOM-Reihenfolge. Sichtbar heisst: `style.display !== 'none' && !classList.contains('d-none')`,
   dieselbe Bedingung wie `print-visible-columns.js` Z. 16 und `Picking/Bom.cshtml` Z. 1275.
   *Ueberholt (2026-10-12):* „Sichtbarkeitsregel wird wiederverwendet, nicht kopiert“ — die Regel
   ist an beiden Stellen eine Inline-Bedingung ohne aufrufbare Funktion; die einzeilige Bedingung
   wird im Skript gleichlautend verwendet, ein Herausloesen ist nicht Teil dieser Spec.
   Spalten ohne Ueberschrifttext (Aktions- und Checkbox-Spalten wie `actions`/`row-actions`,
   `ProductionOrders/Index.cshtml` Z. 80 und 111) fallen weg (Antwort 2 „wie eingeblendet“,
   Antwort 14 a).
3. **Server-Mode** (`data-server-column-filter="true"`): Das Skript holt per `fetch` dieselbe URL
   mit `pageSize=0&page=1` (same-origin, Windows-Auth-Cookie bzw. Negotiate wie die Seite),
   parst die Antwort mit `DOMParser` und ordnet die Zellen ueber den `data-col-key`-Index der
   **abgerufenen** Kopfzeile den sichtbaren Spalten zu. Grund: Die Antwort hat Serverreihenfolge
   ohne Praeferenzen. Das ist dieselbe Action mit denselben Filter-Attributen und denselben
   Parametern. Rechte, Gates, Filter und Datumspostfilter gelten deshalb **per Konstruktion**
   identisch (AK 8). Es entsteht kein neuer Endpunkt.
   **Client-Mode** (Antwort 12): Das Skript nimmt die Zeilen der Live-Tabelle, die der
   Spaltenfilter nicht ausgeblendet hat, also `row.style.display !== 'none'` (`table-filter.js`
   Z. 326). Tabellen ohne `table-filter.js` (Nr. 39, 40) liefern damit alle Zeilen.
   **Stueckliste (Nr. 36):** Weil `updateBomVisibility` Filter und Klappzustand in **einer**
   Eigenschaft zusammenfasst (`Picking/Bom.cshtml` Z. 908-909), legt es zusaetzlich je Datenzeile
   das reine Filterergebnis ab: `row.setAttribute('data-csv-filter-match', filterVisible ? 'true' :
   'false')` (eine Zeile nach Z. 906). Das Skript bevorzugt dieses Attribut, wo es vorhanden ist,
   vor `style.display`. Ergebnis: alle Filtertreffer, unabhaengig davon, ob ihr Elternknoten
   zugeklappt ist; ohne aktiven Filter die ganze Stueckliste.
4. Zeilen: alle `:scope > tbody > tr`. Zeilen mit `td[colspan]` werden ausgelassen: Gruppenkoepfe,
   Kombigeraete-Variantenzeile und Leerzustand. Fuer jede DOM-Iteration gilt `:scope >`
   ([[fallstricke]]: „Jede DOM-Iteration im Tree-Table braucht `:scope >`“), sonst laesst die
   verschachtelte Variantentabelle Fremdzeilen ins CSV.
5. Gruppenschluessel (Antwort 4 „flach“, Antwort 14 b): Steht er in `tbody[data-group]` und hat
   die Tabelle keine sichtbare `hauptfa`-Spalte, kommt eine erste Spalte „HauptFA“ mit dem Wert von
   `data-group` hinzu (betrifft Nr. 1-5 und 12 bei Master `true`; die IDEAL-Listen 7-11 haben eine
   eigene `hauptfa`-Spalte). Ist die `hauptfa`-Spalte per Zahnrad ausgeblendet, faellt sie weg wie
   jede andere Spalte; eine Ersatzspalte entsteht dort nicht, weil `data-group` fehlt.
6. Zellwert, in dieser Reihenfolge:
   - `data-csv-value` an der Zelle → dieser Wert (Nachschaerfung je Liste, nur wo noetig);
   - `input[type=checkbox]` → „Ja“/„Nein“ nach `checked` (Antwort 11);
   - anderes `input` (z. B. Prio, `_PickingLeitstandRow.cshtml` Z. 255) → `value`; `select` → Text
     der gewaehlten Option (Antwort 14 d);
   - Zelle ohne Text, aber mit `title`/`aria-label` an der Zelle oder einem Kind (z. B. Lack-T-Icon
     `_ProductionOrderRow.cshtml` Z. 113-122) → dieser Text, sonst leer (Antwort 14 e);
   - sonst `textContent` der Zelle, **inklusive** Badges (Antwort 14 c, z. B. „HauptFA“, „In Sage
     gelöscht“, „BG 2“).
   Zeilenklassen und Farben werden nicht ausgewertet (Antwort 11). Danach: Whitespace auf ein
   Leerzeichen normalisieren und trimmen; ein Wert der Form `dd.MM.yyyy KWnn` wird auf `dd.MM.yyyy`
   gekuerzt (Antwort 3); CSV-Injection entschaerfen (Antwort 5 a): beginnt der Wert mit `=`, `+`,
   `-`, `@`, Tab oder CR **und** ist er nicht vollstaendig eine Zahl im Anzeigeformat (optionales
   Vorzeichen, Ziffern mit optionalen Tausenderpunkten, optional Komma mit Nachkommastellen), wird
   `'` vorangestellt. Felder werden nach RFC 4180 maskiert: Anfuehrungszeichen bei `;`, `"`, CR oder
   LF, innere `"` verdoppelt.
7. Ausgabe als `Blob` (`text/csv;charset=utf-8`) mit UTF-8-BOM, Trenner `;`, Zeilenende CRLF,
   **ohne** `sep=;`-Zeile (Antwort 3). Download ueber `a[download]`, Dateiname
   `<data-view-key bzw. Controller-Segment der URL>_yyyy-MM-dd_HHmm.csv` (lokale Zeit), Zeichen
   `\ / : * ? " < > |` durch `_` ersetzt.
8. Cap (Antwort 9 + 13): `_Pagination.cshtml` bekommt am `.pagination-bar` (Z. 25-26) die Attribute
   `data-total-count` (= `TotalCount`) und `data-capped` (= `PaginationState.IsCappedAtAll`,
   `PaginationState.cs` Z. 32-33). Das Skript liest sie aus der **abgerufenen** Antwort
   (`pageSize=0`), exportiert die gelieferten ersten 5000 Treffer (bei Gruppenlisten 5000 Gruppen)
   und zeigt bei `data-capped="true"` sichtbar (Bootstrap-`alert-warning` neben dem Knopf, kein
   reines `console`/Tooltip) die Warnung: „Export auf 5.000 Treffer begrenzt, bitte Filter
   eingrenzen. Sortierung gilt nur innerhalb der exportierten 5.000.“ Der Zusatzsatz steht
   **immer** mit, wenn gekappt wird (Wortlaut Antwort 13: „Warnung ergänzen … sonst nichts bauen“).
   Die Datei wird trotzdem erzeugt. Kein Sperren, kein serverseitiges Sortieren. Ohne Kappung keine
   Warnung.
9. Sortierung (Antwort 10): Das Skript liest den aktiven Sortierzustand aus der **Live**-Tabelle,
   `thead th[data-sort-dir]` mit `data-col-key` (gesetzt bei Klick `table-filter.js` Z. 246-255 und
   bei Default-Sortierung ueber `window.triggerSort` Z. 263-266, 623-629). Es sortiert die
   abgerufenen Zeilen je `tbody` (Gruppenreihenfolge bleibt) mit dem Wert der Sortierspalte aus der
   **abgerufenen** Kopfzeile, auch wenn diese Spalte ausgeblendet ist. Der Vergleich ist heute eine
   innere Funktion `compareRows` mit DOM-Zellzugriff ueber den physischen Index der Live-Tabelle
   (Z. 345-378) und damit von aussen nicht nutzbar. Umsetzung: den Wertvergleich (Z. 352-377,
   Datum → Zahl → `localeCompare('de')`) als `window.tableFilterCompareValues(valA, valB, dir)`
   herausloesen und von `compareRows` **und** `csv-export.js` aufrufen. Kopieren ist
   ausgeschlossen, sonst entstuende die „drei Stellen synchron halten“-Klasse aus [[fallstricke]].
   Kein Sortierindikator gesetzt → Serverreihenfolge. Seiten ohne `table-filter.js` (Nr. 39, 40)
   haben nie einen Sortierindikator; das Skript ruft `window.tableFilterCompareValues` nur auf,
   wenn ein Indikator gesetzt ist, und bricht bei fehlender Funktion mit sichtbarer Fehlermeldung
   ab statt unsortiert zu exportieren. Im Client-Mode wird ebenfalls nach dem Indikator sortiert;
   die Live-Zeilen haben die Bildschirmreihenfolge ohnehin schon.
10. Waehrend des Abrufs wird der Knopf deaktiviert und zeigt einen Spinner. Ein Fehler (HTTP ≠ 200,
    Redirect auf Login/AccessDenied, keine Tabelle in der Antwort) zeigt eine sichtbare
    Fehlermeldung, und es wird **keine** Datei erzeugt. Ein leeres CSV ist ausgeschlossen.

**Dev-Lauf-Pflicht — Wirkungsfreiheit:** Der Export ruft die Index-Action ein zweites Mal per GET
(mit `pageSize=0`) auf.
Eine Stichprobe (grep `\.(Add|Update|Remove|Upsert|Save|Mark|Set)\w*Async\(` in den
Controllern der FA-Familie, IDEAL-Listen, Tracking, MissingParts und WarehousePicking) fand **keinen**
Schreibaufruf innerhalb der Index-/Summiert-Rumpfe. Schreibvorgaenge, die indirekt ueber Services
laufen, sind damit **nicht** ausgeschlossen. Der Dev-Lauf prueft deshalb je Index-/Summiert-Action
im Scope (inkl. der seit der Erstfassung neuen `FaHierarchyBeschichtungController.Summiert`, Z. 147)
den Aufrufpfad auf Schreibzugriffe und dokumentiert das Ergebnis als Tabelle im QA-Nachweis. Ein
Fund (Schreibzugriff im GET-Pfad) stoppt den Lauf und kommt als Rueckfrage in die Spec, er wird
nicht still umgangen. Belegt ist bereits: Die Benutzer-Seitengroesse wird im GET-Pfad nicht
geschrieben (siehe Ausgangslage).

Ponytail-Leiter: Wiederverwendet werden Index-Action, Rechte, Filter, `PageSize.AllCap`, der
Einfuegemechanismus des Zahnrads und der Sort-Wertvergleich. Neu ist nur ein Skript, zwei Attribute
im Pagination-Partial und ein Attribut in `updateBomVisibility`. Bewusst entfaellt: neue Endpunkte,
eine C#-CSV-Bibliothek und Code je Controller.

**Grenzen der Loesung:**
- Der CSV-Inhalt ist der **gerenderte** Zellentext. Das ist derselbe Grundsatz wie beim
  Spaltenfilter (ADR 0005: „der ColumnMap-Getter liefert den gerenderten Zellentext“).
- Die Injection- und Escape-Logik ist mangels JS-Testinfrastruktur nicht per CI testbar. Sie wird
  abgesichert durch den Quelltext-Waechter (AK 13) und durch Negativfaelle im Manual-UAT.
- Das Dezimalzeichen in Views ohne feste Kultur haengt an der Server-Kultur (unverifiziert,
  Pruefschritt im Abschnitt Deploy); der Export uebernimmt den angezeigten Text unveraendert.

**Entschieden: Erzeugung im Browser (Antwort 8).** Rechtegleichheit und Filtergleichheit sind per
Konstruktion gegeben und nicht Ergebnis von Disziplin. Der Umfang passt in einen Dev-Lauf. Die
Zellwert-Schwaechen lassen sich je Liste gezielt mit `data-csv-value` nachschaerfen. Die
Alternative „Export-Action je Controller“ steht nur noch im Protokoll-Abschnitt „Verworfen:
Variante B (Antwort 8, 2026-10-09)“ vor dem Deploy-Abschnitt.

## Migrations-/SQL-Auswirkungen

Keine. Es gibt weder Schema- noch Datenaenderung noch Migration. Damit entfallen das
`SQL/XX_*.sql` mit `OBJECT_ID`-Guard, der `00_FreshInstall.sql`-Nachzug und eine Wirkung auf
`SQL/AgentJobs/*`.

## Audit-Feld-Auswirkungen

Keine. Der Export liest nur. Es wird keine `AuditableEntity` geschrieben, und `ModifiedAt`,
`ModifiedBy` und `ModifiedByWindows` bleiben unberuehrt. Eine Export-Protokollierung ist nicht
vorgesehen (Antwort 5 c). Der Abruf erscheint im normalen Request-Log wie ein Seitenaufruf mit
`pageSize=0`.

**Rollen und Zugriffsfilter:** Es gibt keine neue Rolle und keinen neuen Filter (Antwort 5 b).
Deshalb ist auch kein Eintrag in `controller.md` oder `RoleOverview.cshtml` noetig. Der Export erbt
die Klassen- und Aktions-Attribute der jeweiligen Index-/Summiert-Action samt kumulativer
Feature-Gates (Inventar, Spalte Zugriff), weil er genau diese Action aufruft. Client-Mode-Listen
exportieren nur, was bereits auf der Seite steht.

**Listen-View-Pattern (ADR 0005):** Es entsteht keine neue Tabellenansicht, daher fallen fuer diese
Spec keine neuen Pagination-, Filterkarten-, Spaltenfilter- oder viewKey-Pflichten an. Der Export
ist eine Ergaenzung des bestehenden Musters und wird beim Umsetzen als additiver Nachtrag in ADR
0005 vermerkt: Jede Tabelle mit `filterable-table` bekommt den Export automatisch ueber das
Layout-Skript; Opt-out per `data-csv-export="false"`.

## Akzeptanzkriterien

1. Jede Tabelle aus dem Inventar (Nr. 1-40 inkl. 9a, 41 Tabellen) zeigt einen Knopf „CSV“, auch
   jede der mehrfach gerenderten OSEON-Reporting-Tabellen. Baumansichten (`FaHierarchy/Index`,
   `Tracking/Index` hierarchischer Zweig, `Tracking/OseonIndex`) und Formular-/Detail-/Print-Tabellen
   zeigen ihn nicht (Antwort 7).
2. Liste mit 3 Seiten à 25, Spaltenfilter gesetzt: Das CSV enthaelt **alle** Filtertreffer, nicht nur
   die angezeigte Seite. Die Zeilenzahl entspricht der Pagination-Anzeige „von N“ (bei Gruppenlisten:
   Summe der Zeilen aller Gruppen).
3. Spalte per Zahnrad ausgeblendet und **sofort** exportiert, also ohne Reload: Die Spalte fehlt im
   CSV. Spalten in anderer Reihenfolge angeordnet: Das CSV hat die Bildschirm-Reihenfolge.
4. Aktions-, Knopf- und Auswahlspalten ohne Ueberschrifttext (z. B. `actions`, `row-actions` der
   FA-Liste, Bulk-Checkbox im Leitstand) erscheinen nie im CSV (Antwort 14 a).
5. Format (Antwort 3): Die Datei beginnt mit dem UTF-8-BOM (Bytes `EF BB BF`), Trenner `;`,
   Zeilenende CRLF, keine `sep=`-Zeile. Per Doppelklick in Excel (de-AT) oeffnen: korrekte Umlaute
   (ä ö ü ß, „–“), getrennte Spalten, Mengen wie `1.234,500` als Zahl erkannt (Summenformel rechnet),
   Datum als `dd.MM.yyyy` **ohne** KW-Suffix und von Excel als Datum erkannt.
6. Hierarchische FA-Liste (Master `true`): Das CSV ist flach. Eine Spalte „HauptFA“ mit dem
   HauptFA-Schluessel steht als erste Spalte (Antwort 14 b). Gruppen-Kopfzeilen und die
   Kombigeraete-Variantentabelle erscheinen **nicht** als Zeilen. Keine Zeile hat eine abweichende
   Spaltenzahl. In den IDEAL-Listen 7-11 (eigene `hauptfa`-Spalte) gibt es **keine** zweite
   HauptFA-Spalte.
7. Injection (Antwort 5 a): Eine Zelle mit Inhalt `=1+1`, `+43 664 …`, `@SUMME(…)`, `-x` oder
   beginnend mit Tab/CR beginnt im CSV mit `'` und wird in Excel als Text angezeigt, nicht berechnet.
   Die reinen Zahlen `-5,000` und `-1.234,500` bleiben unveraendert und werden als Zahl erkannt.
8. Ein Anwender ohne Leserecht der Ansicht, bzw. bei ausgeschaltetem Feature-Gate, erhaelt auch
   ueber den Export keine Daten. Das CSV enthaelt bei jedem Anwender genau die Zeilen, die seine
   Ansicht mit denselben Filtern zeigt. Es gibt kein eigenes Export-Recht und keinen eigenen
   Toggle (Antwort 5 b).
9. Mehr als 5000 Treffer: Es erscheint sichtbar die Warnung „Export auf 5.000 Treffer begrenzt,
   bitte Filter eingrenzen. Sortierung gilt nur innerhalb der exportierten 5.000.“ (Antworten 9,
   13), und das CSV enthaelt die ersten 5000 Treffer in Serverreihenfolge (bei Gruppenlisten: die
   Zeilen der ersten 5000 Gruppen), danach nach Bildschirm-Sortierung geordnet; nicht weniger und
   nicht mehr. Die Datei wird trotzdem erzeugt. Ohne Kappung erscheint keine Warnung.
10. Ein Abruf schlaegt fehl (z. B. Sitzung abgelaufen, Redirect auf Login/AccessDenied,
    Serverfehler, keine Tabelle in der Antwort): Es erscheint eine sichtbare Fehlermeldung, und es
    wird keine Datei heruntergeladen. Waehrend des Abrufs ist der Knopf deaktiviert.
11. AKE-Regression: Bei Master `false` sind alle Listen in Darstellung, Filterung, Sortierung,
    Spaltenauswahl und Druck unveraendert. Der einzige Unterschied ist der zusaetzliche Knopf; das
    Zahnrad sitzt weiter an seinem Platz.
12. Dateiname nach dem Muster `<Ansicht>_yyyy-MM-dd_HHmm.csv` (Ansicht = `data-view-key`, sonst
    Controller-Segment), ohne Zeichen, die unter Windows im Dateinamen ungueltig sind.
13. Der Quelltext-Waechter `CsvExportCoverageTests` ist gruen. Er prueft: (a) `_Layout.cshtml`
    bindet `csv-export.js` ein; (b) jede View mit `filterable-table` nutzt das Layout (kein
    `Layout = null`), ausser den Opt-outs mit `data-csv-export="false"`; (c) `csv-export.js` iteriert
    Zeilen/Zellen nur mit `:scope >`; (d) `table-filter.js` definiert `window.tableFilterCompareValues`
    und `compareRows` ruft ihn auf; (e) `Picking/Bom.cshtml` setzt `data-csv-filter-match`;
    (f) `_Pagination.cshtml` traegt `data-capped`.
14. Build gruen, alle Web- und Service-Tests gruen.
15. Sortierung (Antwort 10): Liste ueber mehrere Seiten, Spaltenkopf einer Datumsspalte absteigend
    angeklickt → das CSV ist ueber **alle** Treffer nach dieser Spalte absteigend sortiert (Datum,
    Zahl und Text wie am Bildschirm). Bei Gruppenlisten ist innerhalb jeder Gruppe sortiert, die
    Gruppenreihenfolge entspricht dem Bildschirm. Ist die Sortierspalte per Zahnrad ausgeblendet
    (Default-Sortierung), wird trotzdem nach ihr sortiert. Ohne Sortierindikator: Serverreihenfolge.
    Die Bildschirm-Sortierung selbst verhaelt sich nach dem Herausloesen des Vergleichs unveraendert.
16. Zellwerte (Antwort 11): Leitstand-Checkboxen Glas, Fremdbezug und Lack erscheinen als „Ja“ bzw.
    „Nein“ entsprechend dem Haken, nie als leere Zelle. Erledigte/stornierte Zeilen haben keine
    zusaetzliche Statusspalte.
17. Zellwerte (Antwort 14 c-e): Die FA-Nr.-Zelle enthaelt den Badge-Text (z. B. „… HauptFA“,
    „… In Sage gelöscht“); die Werkbank-Zelle „… BG 2“; die Leitstand-Prio den Zahlenwert des
    Eingabefelds; die Lack-T-Zelle den `title` des Icons („Lackierteile erledigt“) bzw. „–“.
18. Client-Mode (Antwort 12): Stueckliste mit aktivem Spaltenfilter und einem **zugeklappten**
    Baumknoten, unter dem Filtertreffer liegen → das CSV enthaelt diese Treffer. Ohne Filter → das
    CSV enthaelt alle Stuecklistenzeilen, unabhaengig vom Klappzustand. Lagerbestellung-Details und
    Tracking je Werkbank → genau die nach Spaltenfilter sichtbaren Zeilen. Artikelgruppen-Zuordnung
    und OSEON-Reporting (kein Filter) → alle Zeilen der jeweiligen Tabelle. Die Bildschirmanzeige der
    Stueckliste (Klappzustand, Filter, rekursive Suche) ist unveraendert.
19. Wirkungsfreiheit (Dev-Lauf-Pflicht): Der QA-Nachweis enthaelt je Index-/Summiert-Action im Scope
    das Pruefergebnis „kein Schreibzugriff im GET-Pfad“ mit Beleg; ein Fund ist als Rueckfrage
    gemeldet statt umgangen.

## Test-Szenarien

Neues Kapitel **TS-&lt;naechste freie im Buendel&gt; — CSV-Export fuer alle Tabellen (v&lt;naechste
freie&gt;)** in `docs/TESTSZENARIEN.md` des Buendel-Worktrees (Stand 2026-10-12: hoechstes Kapitel
TS-82, Z. 9339 → voraussichtlich TS-83), danach `secondbrain/tests/testszenarien-index.md` im
Hauptcheckout nachziehen. Jedes Unterszenario mit Vorbedingungen, Schritten, erwartetem Verhalten
und Negativfall. Gliederung:

- **.1 Grundfall AKE (Master false):** In der FA-Liste Spaltenfilter „Werkbank“ setzen,
  Seitengroesse 25, mehrere Seiten. CSV-Zeilenzahl = „von N“, Spalten = sichtbare Spalten.
- **.2 Spaltenauswahl sofort:** Spalte ausblenden und ohne Reload exportieren; Spalte nach vorn
  ziehen und exportieren (Pendant zu TS-80.6).
- **.3 IDEAL hierarchisch (Master true):** FA-Liste, Leitstand, FaCompletion, FaWorklist und
  Picking. Pruefen: Spalte HauptFA, keine Kopfzeilen, Kombigeraet-HauptFA ohne Fremdzeilen aus der
  Variantentabelle.
- **.4 IDEAL-Listen:** Kommissionierliste (Liste und Summiert), Beschichtung (Liste **und**
  Summiert, neu seit v1.48.0; Gruppenkopf mit Briefkopf erscheint nicht als Zeile), Vormontage
  (beide Sichten). HauptFA-Spalte nicht doppelt; `hauptfa` per Zahnrad ausgeblendet → fehlt im CSV.
- **.5 Excel / Format (Antwort 3):** Datei im Hex-/Texteditor: BOM, `;`, CRLF, keine `sep=`-Zeile,
  Dateiname `<Ansicht>_yyyy-MM-dd_HHmm.csv`. Doppelklick oeffnen: Umlaute korrekt, Mengen als Zahl
  (Summenformel funktioniert), Datum ohne KW als Datum erkannt.
- **.6 CSV-Injection (Negativfall, Antwort 5 a):** Testdatensatz mit Bemerkung
  `=HYPERLINK("http://x";"klick")`, `@SUMME(A1:A2)`, `+43 664 1234567` bzw. `-x`. Der Wert erscheint
  im CSV mit `'`, und Excel fuehrt nichts aus.
- **.7 Negative Mengen (Antwort 5 a, Zahlen-Ausnahme):** Bestand oder Bewegung mit negativer Menge.
  Bleibt ohne `'` und als Zahl erkannt. Berechnete Bestaende sind vorzeichenbehaftet
  (`Data/Repositories/StockMovementRepository.cs` Z. 64-71 und 249; Erstfassung zitierte Z. 70,
  222-223); einen Guard gegen negative Werte gibt es dort nicht.
- **.8 Rechte (Negativfall):** Anwender ohne `stock_read` ruft die Bestands-URL inkl. Export auf.
  Kein Zugriff. Ein Anwender mit Leitstand- aber ohne Picking-Recht sieht in FA-Liste und CSV
  dieselben Spalten (die Aktionsspalte ist ohnehin nicht im CSV).
- **.9 Feature-Gate aus (Negativfall):** Mit `FaHierarchyKommissionierlistenAktiv = false` gibt es
  weder Liste noch Export.
- **.10 Cap (Antworten 9, 13):** Filter so waehlen, dass mehr als 5000 Treffer entstehen (z. B.
  Bewegungshistorie ohne Filter), Datumsspalte absteigend sortieren. Warnung inkl. Satz
  „Sortierung gilt nur innerhalb der exportierten 5.000“ sichtbar, Datei wird trotzdem erzeugt,
  CSV enthaelt genau 5000 Treffer, innerhalb absteigend sortiert. Gegenprobe mit eingegrenztem
  Filter unter 5000: keine Warnung.
- **.11 Fehlerfall:** Sitzung ablaufen lassen bzw. Server stoppen, dann exportieren. Sichtbare
  Meldung, keine Datei; Knopf waehrend des Abrufs deaktiviert.
- **.12 Client-Mode-Listen (Antwort 12):** Stueckliste mit Komm.-Ziel-Filter und einem zugeklappten
  Knoten, unter dem Treffer liegen → Treffer im CSV; ohne Filter → ganze Stueckliste im CSV;
  Bildschirm (Klappzustand, Filter) unveraendert. Lagerbestellung-Details und Tracking je Werkbank
  mit Spaltenfilter → nur sichtbare Zeilen. Artikelgruppen-Zuordnung und OSEON-Reporting
  (Arbeitsgang-Uebersicht mit Zukunftstag-Gruppen) → je Tabelle ein Knopf, alle Zeilen dieser
  Tabelle.
- **.13 Zellwerte (Antworten 11, 14 c-e):** Leitstand mit Glas, Fremdbezug und Lack → „Ja“/„Nein“ je
  Haken; erledigte/stornierte Zeile ohne Statusspalte; Prio als Zahl aus dem Eingabefeld;
  Lack-T-Icon als „Lackierteile erledigt“ bzw. „–“; FA-Nr. mit Badge-Text; Werkbank mit „BG n“.
- **.14 Stammdaten-Stichprobe:** Artikel, Lagerplaetze, Benutzer (Admin) und BDE-Stammdaten je Tab.
- **.15 Sortierung (Antwort 10):** Mehrseitige Liste, Datumsspalte absteigend klicken, exportieren:
  CSV ueber alle Treffer absteigend. Dasselbe mit Zahlen- und Textspalte. IDEAL-Gruppenliste: je
  Gruppe sortiert, Gruppenreihenfolge unveraendert. Default-Sortierung auf ausgeblendete Spalte:
  CSV trotzdem danach sortiert. Regression: Bildschirm-Sortierung (Klick, Default, Gruppen,
  Kombigeraete-Variantentabelle bleibt gefuellt) unveraendert.

## Etappen (nur bei epic: true)

Entfaellt: `epic: false`, `etappen: []` (Antwort 8 = Erzeugung im Browser, ein Dev-Lauf). Der
Lauf endet mit einem Commit im Buendel-Zweig; kein eigener Merge.

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | entfaellt (Antwort 8) | – | – |

## Verworfen: Variante B (Antwort 8, 2026-10-09)

> [!abstract] Protokoll — nicht umsetzen
> Aus Rumpf, affected_code, AK und Testszenarien entfernt. Hier nur noch als Begruendungsspur.

**Variante B: Export-Action je Controller.** Jeder Controller bekommt eine `Export`-Action. Sie baut
die Liste wie `Index`, nur ungepaginiert, und schreibt das CSV ueber einen gemeinsamen, per xUnit
testbaren `CsvWriter`-Helper.
- Vorteil: getypte Werte (Zahlen und Daten ohne Anzeigeformat), Logik in C# testbar.
- Nachteil: Die Index-Rumpfe bauen ihr ViewModel inline, z. B. `ProductionOrdersController.Index`
  Z. 73-466 mit Flach- und Hierarchiezweig, Postfiltern und Kopfdaten-Anreicherung (Stand
  Erstfassung). Jede Liste braucht also eine Extraktion der Abfragelogik oder eine zweite Kopie,
  das heisst 35 bis 41 Eingriffe in produktive Controller. Die Spaltenauswahl muss serverseitig
  nachgebaut werden (`ResolveVisibleColumnsAsync`-Muster aus
  [[2026-09-25-kommissionierliste-summierung-pdf-spec]]), ebenso die Zuordnung col-key → Wert je
  Liste, also eine zweite Wahrheit neben der View. Jede `Export`-Action haette dieselben Attribute
  tragen muessen wie `Index` (Pflicht-Testmuster je Controller nach Art der
  Attribut-Reflection-Tests).
- Umfang: **Epic** mit Etappen: (1) `CsvWriter`-Helper + Tests + Pilot ProductionOrders
  flach/hierarchisch; (2) FA-Familie Leitstand, FaCompletion, FaWorklist, Picking; (3) IDEAL-Listen
  7-11; (4) Lager/Bestellungen 13-22; (5) Stammdaten, Admin und BDE 6, 12, 23-35; (6) Client-Mode-
  Listen 36-40 + Doku/TS. Branch-Pflege mit `scripts/sync-worktree.ps1`.

Verworfen, weil Rechte- und Filtergleichheit bei der Browser-Loesung per Konstruktion gelten und
der Umfang in einen Lauf passt (Entscheidung Gerald 2026-10-09).

## Deploy

**Final (QA 2026-10-12, aus dem echten Diff `d6d6beb0..b43edb25`, 12 Dateien; QA-Nachlauf: `d6d6beb0..9ad794b1`,
15 Dateien — zusätzlich nur Views `StockMovements/Index`, `OrderRecipients/ArticleGroupMappings`,
`Beschichtungsauftraege/Details` (alle Web), Einstufung unverändert: Web ja, Service nur Versionsanzeige, Migration nein).**

- **Web-App: ja** — `IdealAkeWms/` geändert: neu `wwwroot/js/csv-export.js`; `wwwroot/js/table-filter.js`
  (Vergleich herausgelöst), `Views/Shared/_Layout.cshtml`, `Views/Shared/_Pagination.cshtml`, `Views/Picking/Bom.cshtml`,
  `Views/Help/Index.cshtml`, `Views/Help/Changelog.cshtml`, `AppVersion.cs` (1.53.0).
- **Service: nein (fachlich)** — einzige Änderung unter `IDEALAKEWMSService/` ist der Versions-Bump `AppVersion.cs`
  1.52.0 → 1.53.0. Service-Publish nur, wenn die angezeigte Service-Version gleichziehen soll.
- **Migration: nein** — keine Datei unter `*/Migrations/`, kein `SQL/`-Diff, kein Einmal-Skript.
- **Version:** 1.53.0, geht mit dem gemeinsamen Bündel-Merge (Schranke 2) live, nicht separat.
- **Ablauf:** Publish aus dem Worktree → Testsystem → TS-86 → Merge (Bündel). Ohne DB-Schritt: nur Web-App tauschen.
- **Publish-Befehl** (im Worktree `C:\git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5`):

```bash
dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
# optional, nur fuer die Versionsanzeige des Service:
# dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService
```

  Nach dem Merge nur dann erneut aus `main` publishen, wenn der Merge getestete Dateien mit parallelen `main`-Änderungen
  zusammengeführt hat.
- **Pruefschritt vor der Abnahme (unverifizierte Umgebungsannahme):** Auf dem Testsystem in der
  FA-Liste die Spalte „Stk.“ ansehen (`_ProductionOrderRow.cshtml` Z. 66, `ToString("N3")` ohne
  Kultur).
  - Zeigt sie `1,000`, ist die Server-Kultur deutsch, und AK 5 gilt ohne Eingriff.
  - Zeigt sie `1.000` im Sinne von eins, ist die Kultur englisch. Dann liefern Views ohne feste
    Kultur Dezimalpunkte, und das muss vor der Abnahme als Rueckfrage zurueck in die Spec.
- **Browser-Cache:** Das Skript wird mit `asp-append-version="true"` eingebunden, ein Hard-Reload ist
  also nicht noetig.

## Offene Rueckfragen

> [!note] Stand 2026-10-12
> **Alle 14 Rueckfragen sind beantwortet und eingearbeitet.** 1, 2, 4, 9, 10, 11 (Keyuser
> 2026-10-07) standen bereits im Rumpf und wurden am 2026-10-12 nur noch von „vorbehaltlich“-
> Formulierungen bereinigt; 3, 5, 6, 7, 8, 12, 13, 14 (Gerald 2026-10-09) sind am 2026-10-12 in
> Rumpf, affected_code, AK, Testszenarien und Deploy gezogen. Die Fragetexte unten bleiben als
> Audit-Spur stehen. Keine offene Rueckfrage.
>
> *Ueberholt:* Stand 2026-10-07 — „Offen bleiben 3, 5, 6, 7, 8, 12 sowie neu 13 und 14“.

1. **[Eingearbeitet (Antwort 1)]** **[Entschieden: alle Seiten]** **Welche Zeilen?** Alle Zeilen des aktuellen Filters ueber alle Seiten oder nur die sichtbare
   Seite?
   *Empfehlung (Notiz):* alle Zeilen des Filters. Technisch ueber `pageSize=0`, gekappt bei 5000
   (siehe 9).
   *Ueberholt:* „nur die sichtbare Seite“.
2. **[Eingearbeitet (Antwort 2)]** **[Entschieden: wie eingeblendet]** **Welche Spalten?** Die beim Klick sichtbaren, in Bildschirm-Reihenfolge? Spalten ohne
   Ueberschrift (Aktionen, Knoepfe, Bulk-Checkbox) nie?
   *Empfehlung (Notiz):* sichtbare Spalten in Bildschirm-Reihenfolge, gleiche Konvention wie
   Druck/PDF in [[2026-09-25-kommissionierliste-summierung-pdf-spec]]. Dazu die Spec-Empfehlung:
   Spalten ohne Ueberschrifttext nie exportieren. Einen Server-Rueckfall wie beim Druck braucht es
   bei Variante A nicht, weil der Export immer aus der offenen Seite startet.
   *Offen geblieben:* Weglassen der Spalten ohne Ueberschrift → Rueckfrage 14 (inzwischen
   eingearbeitet, Antwort 14 a).
3. **[Eingearbeitet (Antwort 3): wie empfohlen, Variante (a)]** **Format fuer Excel in Oesterreich?** Semikolon, UTF-8 mit BOM, Dezimalkomma, Datum `dd.MM.yyyy`.
   Datumszellen haben heute die Form `dd.MM.yyyy KWnn` (`_ProductionOrderRow.cshtml` Z. 6-11).
   Soll der KW-Suffix im Export entfallen, damit Excel ein Datum erkennt (a)? Oder bleibt er wie
   angezeigt, dann als Text (b)? Oder zwei Spalten Datum + KW (c)? Dateiname
   `<Ansicht>_yyyy-MM-dd_HHmm.csv`?
   *Empfehlung (Notiz):* Semikolon, UTF-8-BOM, Dezimalkomma, `dd.MM.yyyy`, Dateiname mit Ansicht
   und Zeitstempel. Daraus folgt (a). Keine `sep=;`-Zeile: Sie hebelt nach bekanntem Excel-Verhalten
   die BOM-Erkennung aus. Das ist unverifiziert und wird im UAT TS-.5 geprueft. Das Dezimalkomma
   haengt teils an der Server-Kultur (Pruefschritt im Abschnitt Deploy).
4. **[Eingearbeitet (Antwort 4)]** **[Entschieden: flach]** **Gruppierte Listen?** Flach exportieren mit Gruppenschluessel als eigener Spalte? Gruppen-
   Kopfzeilen samt deren Kopfdaten (Kunde und FE-Termin in der Kopfzeile, `ProductionOrders/Index.cshtml`
   Z. 152-162) und die Kombigeraete-Variantenzeile entfallen als Zeilen?
   *Empfehlung (Notiz):* flach, Gruppenschluessel als eigene Spalte. Dazu die Spec-Empfehlung:
   „HauptFA“ aus `tbody[data-group]` nur dort ergaenzen, wo keine `hauptfa`-Spalte sichtbar ist.
   Die Kopfdaten der FA-Liste stehen ohnehin in den Zeilenspalten (Kunde, Prio, AB-Nr.,
   KO-Termin, `_ProductionOrderRow.cshtml` Z. 69-76; Kommentar `ProductionOrders/Index.cshtml`
   Z. 147-151).
   *Offen geblieben:* eigene Spalte „HauptFA“ aus `tbody[data-group]` → Rueckfrage 14 (inzwischen
   eingearbeitet, Antwort 14 b).
5. **[Eingearbeitet (Antwort 5): a, b, c wie empfohlen]** **Sicherheit?**
   (a) Zellen, die mit `=`, `+`, `-`, `@`, Tab oder CR beginnen, erhalten ein fuehrendes `'`.
   Ausgenommen sind Werte, die vollstaendig eine Zahl im Anzeigeformat sind (z. B. `-5,000`), sonst
   werden negative Mengen zu Text.
   (b) Export nur mit dem Leserecht der jeweiligen Ansicht (bestehende `Require*Access`-Filter,
   Feature-Gates kumulativ), ohne eigenes Export-Recht und ohne eigenen Feature-Toggle.
   (c) Keine gesonderte Export-Protokollierung.
   *Empfehlung (Notiz):* CSV-Injection entschaerfen, Export nur mit dem Leserecht der Ansicht. Die
   Spec-Empfehlung ergaenzt die Zahlen-Ausnahme in (a) sowie (b) und (c) wie formuliert.
6. **[Eingearbeitet (Antwort 6): Skript im Layout]** **Gemeinsamer Baustein:** Helper + Button-Partial je View oder ein Skript im Layout, das sich
   selbst an jede `filterable-table` haengt (Opt-out `data-csv-export="false"`)?
   *Empfehlung (Notiz):* ein gemeinsamer Baustein (Helper + Button-Partial) statt Einzelloesungen.
   *Spec-Empfehlung, Abweichung:* Skript im Layout, weil es weniger Code braucht, keine View
   vergessen werden kann und kuenftige Listen den Export automatisch haben. Das Ziel der Notiz, ein
   Baustein statt Einzelloesungen, bleibt erfuellt.
7. **[Eingearbeitet (Antwort 7): eine Spec, Baeume/Formulartabellen ausserhalb; Inventar inzwischen 41 Tabellen]** **Umfang:** Alle 40 `filterable-table`-Tabellen aus dem Inventar in **einer** Spec? Baumansichten
   (`FaHierarchyStructure`, Tracking hierarchisch, `OseonTracking`) und Formular-/Detailtabellen
   ausserhalb? Teil-Specs?
   *Empfehlung (Notiz):* Liste aller betroffenen Ansichten ermitteln, ist mit diesem Inventar
   erledigt. Bei grossem Umfang Teil-Specs nach Bereich: Baustein + Pilotliste zuerst, dann der
   Rest. *Spec-Empfehlung:* Bei Variante A ist **kein** Schnitt noetig, weil der Baustein alle
   Tabellen auf einmal abdeckt. Die Pilotliste (FA-Liste, AKE + IDEAL) wird zuerst im UAT geprueft.
   Baeume und Formulartabellen bleiben ausserhalb, ein Folge-Backlog bei Bedarf.
8. **[Eingearbeitet (Antwort 8): A – Erzeugung im Browser; Etappenvorschlag unten *ueberholt*, siehe Abschnitt „Verworfen: Variante B“]** **Variante:** A = Erzeugung im Browser aus derselben Index-Action (kein neuer Endpunkt, ein
   Dev-Lauf) oder B = `Export`-Action je Controller mit getestetem C#-Helper?
   *Spec-Empfehlung:* **A** (Begruendung im Abschnitt Technischer Loesungsentwurf). Bei **B**
   schlaegt die Spec `epic: true` mit diesen Etappen vor:
   (1) `CsvWriter`-Helper + Tests + Pilot ProductionOrders flach/hierarchisch;
   (2) FA-Familie Leitstand, FaCompletion, FaWorklist, Picking;
   (3) IDEAL-Listen 7-11;
   (4) Lager/Bestellungen 13-22;
   (5) Stammdaten, Admin und BDE 6, 12, 23-35;
   (6) Client-Mode-Listen 36-40 + Doku/TS.
   Jede Etappe ist ein Commit, am Ende steht ein Merge.
   `freigabe_entscheidung` = A oder B.
9. **[Eingearbeitet (Antwort 9)]** **[Entschieden: erste 5000 + Warnung]** **Mehr als 5000 Treffer** (`PageSize.AllCap`; bei Gruppenlisten 5000 **Gruppen**, also
   potenziell deutlich mehr Zeilen)?
   *Spec-Empfehlung:* die ersten 5000 exportieren und eine sichtbare Warnung „auf 5.000 begrenzt,
   bitte Filter eingrenzen“ zeigen (analog Banner `_Pagination.cshtml` Z. 6-12). Alternative: Export
   sperren, bis der Filter eingrenzt. Die Notiz trifft dazu keine Aussage.
   *Ueberholt:* „Export sperren“.
10. **[Eingearbeitet (Antwort 10)]** **[Entschieden: Bildschirm-Sortierung uebernehmen]** **Sortierung:** Soll der Export die aktive Bildschirm-Sortierung uebernehmen? Sie ist rein
    clientseitig und gilt je Gruppe. Uebernahme ueber den Comparator, den `table-filter.js`
    bereitstellt; Alternative ist die Serverreihenfolge.
    *Spec-Empfehlung:* uebernehmen, weil Anwender „wie am Bildschirm“ erwarten. Ohne Uebernahme gilt
    die Default-Sortierung der Action (`data-fallback-sort-column` greift nur clientseitig). Die
    Notiz trifft dazu keine Aussage.
    *Ueberholt:* „Serverreihenfolge“ (gilt nur noch, wenn keine Sortierung aktiv ist).
11. **[Eingearbeitet (Antwort 11; Rest ueber Antwort 14)]** **[Teilweise entschieden: Checkbox Ja/Nein, Farbkennzeichen nicht exportieren]** **Zellinhalt:**
    - Badges in Zellen, z. B. „HauptFA“, „In Sage gelöscht“, „BG 2“ und enaio: als Teil des Werts
      oder weglassen?
    - Checkboxen (Leitstand Glas/Fremdbezug/Lack): als „Ja“/„Nein“?
    - Eingabefelder (Prio): mit ihrem Wert?
    - Reine Icon-Zellen (Lack-T): ueber `title`?
    - Zeilenstatus, der nur farblich transportiert wird (`table-secondary` = erledigt, `table-danger`
      = storniert, `_ProductionOrderRow.cshtml` Z. 21): ausserhalb lassen?

    *Spec-Empfehlung:* Badges mit exportieren (nichts geht verloren, kein Aufwand je View).
    Checkbox „Ja“/„Nein“, Eingabefeld mit Wert, Select mit dem Text der Auswahl, Icon-Zelle ueber
    `title` bzw. `aria-label`, sonst leer. Wo das nicht reicht, kann eine Zelle per `data-csv-value`
    einen eigenen Exportwert setzen. Der nur farbliche Zeilenstatus bleibt ausserhalb. Er
    widerspricht schon am Bildschirm der Regel „Farbe nie alleiniger Bedeutungstraeger“ und gehoert
    in einen eigenen Backlog-Eintrag. Die Notiz trifft dazu keine Aussage.
    *Offen geblieben:* Badges, Eingabefelder/Selects, Icon-Zellen → Rueckfrage 14 (inzwischen
    eingearbeitet, Antwort 14 c-e).
12. **[Eingearbeitet (Antwort 12); Umsetzung ueber `data-csv-filter-match` in `updateBomVisibility`, Nr. 39/40 haben keinen Filter]** **Client-Mode-Listen** (Inventar 36-40): Exportiert werden die vom Client-Filter nicht
    ausgeblendeten Zeilen. Sollen in der Stueckliste auch Filtertreffer exportiert werden, die nur
    wegen eines **zugeklappten** Baumknotens unsichtbar sind (`updateBomVisibility`, [[fallstricke]]
    „BOM-Baum hat Vorrang vor dem Spaltenfilter“)?
    *Spec-Empfehlung:* ja, alle Filtertreffer unabhaengig vom Klappzustand. Der Klappzustand ist
    Navigation, kein Filter. Die Notiz trifft dazu keine Aussage.
13. **[Eingearbeitet (Antwort 13): Warnsatz ergaenzt, sonst nichts gebaut; Sperren/serverseitiges Sortieren *ueberholt*]** **Konflikt Antwort 9 × Antwort 10 (neu, Rework 2026-10-07):** Der Server kappt bei `pageSize=0`
    auf die ersten 5000 Treffer (bzw. Gruppen) in **seiner** Reihenfolge (`PageSize.cs` Z. 29); keine
    Index-Action kennt einen Sortierparameter (siehe Ausgangslage). Die Bildschirm-Sortierung wird
    erst im Browser auf diese 5000 angewandt. Bei gekapptem Export mit aktiver Sortierung sind das
    also **nicht** die ersten 5000 nach Bildschirm-Sortierung (z. B. nicht die 5000 juengsten
    Bewegungen, wenn nach Datum absteigend sortiert ist).
    *Spec-Empfehlung:* Warnung aus Antwort 9 um den Satz „Sortierung gilt nur innerhalb der
    exportierten 5.000“ ergaenzen, sonst nichts bauen. Alternative: bei Kappung + aktiver Sortierung
    Export sperren (widerspraeche Antwort 9) oder serverseitiges Sortieren je Action (Umfang wie
    Variante B).
14. **[Eingearbeitet (Antwort 14): a-e wie empfohlen]** **Restpunkte ohne ausdrueckliche Antwort (neu, Rework 2026-10-07):** Die Antworten 2, 4 und 11
    entscheiden die Kernfrage, nicht diese Details. Bestaetigung der Spec-Empfehlungen:
    (a) Spalten ohne Ueberschrift (Aktionen, Bulk-Checkbox) weglassen, obwohl sie „eingeblendet“
    sind; (b) Gruppenschluessel als eigene erste Spalte „HauptFA“, wo keine `hauptfa`-Spalte sichtbar
    ist; (c) Badges als Teil des Zellwerts; (d) Eingabefeld mit Wert, Select mit Auswahltext;
    (e) Icon-Zelle ueber `title`/`aria-label`, sonst leer.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. Welche Zeilen (alle Seiten / nur sichtbare Seite) → **Alle Treffer des Filters über alle Seiten (Keyuser 2026-10-07).**
2. Welche Spalten (sichtbare in Bildschirm-Reihenfolge, ohne Aktionsspalten) → **Wie eingeblendet (Keyuser 2026-10-07).**
3. Format (Semikolon, UTF-8-BOM, Dezimalkomma, Datum ohne KW a/b/c, Dateiname) → **Wie empfohlen (Gerald 2026-10-09).**
4. Gruppierte Listen (flach + HauptFA-Spalte, Kopf-/Variantenzeilen entfallen) → **Flach (Keyuser 2026-10-07).**
5. Sicherheit (a Injection mit Zahlen-Ausnahme, b nur Leserecht ohne Toggle, c keine Protokollierung) → **Wie empfohlen (Gerald 2026-10-09).**
6. Baustein (Skript im Layout / Button-Partial je View) → **Wie empfohlen (Gerald 2026-10-09).**
7. Umfang (40 Tabellen in einer Spec, Baeume + Formulartabellen ausserhalb) → **Wie empfohlen (Gerald 2026-10-09).**
8. Variante (A Browser / B Export-Action je Controller = Epic) → **A – Erzeugung im Browser (Gerald 2026-10-09).**
9. Mehr als 5000 Treffer (erste 5000 + Warnung / sperren) → **Erste 5000 + sichtbare Warnung (mit Antwort 1 bestätigt) (Keyuser 2026-10-07).**
10. Sortierung (Bildschirm-Sortierung uebernehmen / Serverreihenfolge) → **Bildschirm-Sortierung übernehmen („sortiert korrekt“) (Keyuser 2026-10-07).**
11. Zellinhalt (Badges, Checkbox Ja/Nein, Eingaben, Icons, Farbstatus) → **Checkbox als Ja/Nein; farbige Kennzeichen egal (Keyuser 2026-10-07).**
12. Client-Mode-Listen / Stueckliste unabhaengig vom Klappzustand → **Wie empfohlen (Gerald 2026-10-09).**
13. Kappung x Sortierung (Warnung ergaenzen / sperren / serverseitig sortieren) → **Warnung ergänzen: „Sortierung gilt nur innerhalb der exportierten 5.000“; sonst nichts bauen (Gerald 2026-10-09).**
14. Restpunkte a-e (ohne Ueberschrift weglassen, HauptFA-Spalte, Badges, Eingaben, Icons) → **Wie empfohlen (Gerald 2026-10-09).**

## Umsetzung (2026-10-12, Dev-Lauf)

Direkt im Bündel `feature/2026-08-07-ideal-teile-1-5`, Start `d6d6beb0` (Endstand FA-Arbeitsgänge-Symbole Teil 1, v1.52.0).
Aufgabe [[2026-10-12-csv-export-alle-tabellen-umsetzung]]. **Version 1.53.0, TS-86, keine Migration.**

| Commit | Inhalt |
|---|---|
| `2e6752a1` | `wwwroot/js/csv-export.js` (neu), `table-filter.js` (Vergleich als `window.tableFilterCompareValues` herausgelöst, `compareRows` ruft ihn), `_Layout.cshtml` (Einbindung nach `site.js`), `_Pagination.cshtml` (`data-total-count`, `data-capped`), `Picking/Bom.cshtml` (`data-csv-filter-match` + `data-csv-filter-source="attribute"`); `CsvExportCoverageTests` + `IdealAkeWms.Tests/Js/csv-export.check.js` |
| `400a7520` | v1.53.0, Anwender-Changelog, Hilfe, TS-86, Meldungstexte mit Umlauten + `aria-busy` — **baut allein nicht** (Razor: `@` in der Hilfe) |
| `b43edb25` | Fix: `@@` in der Hilfe — Build wieder grün |

### Pflichtprüfung Wirkungsfreiheit (AK 19) — kein Fund, kein Stopp

Der Export ruft im Server-Mode dieselbe Action ein zweites Mal per GET mit `pageSize=0&page=1`. Geprüft (2026-10-12, Bündel
`d6d6beb0`): alle **37** Actions mit `PageSize.Resolve` unter `Controllers/` —
ArticleAttributes.Index, ArticleCategories.Index, Articles.Index, BdeBookings.Index, BdeMasterData.Index,
Beschichtungsauftraege.Index (neu v1.51.0), FaAttributes.Index, FaCompletion.Index, FaHierarchyBeschichtung.Index + .Summiert,
FaHierarchyKommissionierListen.Index + .Summiert, FaHierarchyVormontage.Index + .Summiert, FaWorklist.Index, MissingParts.Index,
MissingPartsLager.Index, OrderRecipients.Index, PartRequisitions.Index, Picking.Index, PickingLeitstand.Index,
ProductionOrders.Index, ProductionWorkplaces.Index, Roles.Index, SageBookingQueue.Index, StockMovements.Index,
StockOverview.Index, StorageLocations.Index, SyncLog.Index, Tracking.Index (+ OseonIndex, ausser Scope), Users.Index,
WarehousePicking.Index, WarehouseRequisitions.Index, WorkSteps.Index, Workstations.Index.
- **Stufe 1 (Action-Rumpf):** jeder Feld-/Service-Aufruf je Rumpf aufgelistet (Skript, Brace-Matching). Nur lesende Namen
  (`Get*`, `Build*`, `Compute*`, `Ermitteln*`, `Can*/Has*`, `Parse*/Subtract*`). Kein `SaveChanges`, kein `Add/Update/Remove`
  am Context, kein Cookie/Session-Schreiben; `TempData` nur in `FaHierarchy.Index` (Baum, ausser Scope, kein Export).
- **Stufe 2 (transitiv):** alle aufgerufenen Methoden rekursiv bis Tiefe 4 (113 Methoden, Namensauflösung über alle
  gleichnamigen Implementierungen = Obermenge) auf `SaveChanges|Execute(Update|Delete|Sql)|BeginTransaction|_db/_context.X.(Add|Update|Remove|Attach)|Mail`
  → **0 Treffer**. Die 6 Treffer der weiten Suche sind belegt harmlos: Listen-`Add` im Speicher (`OseonGroupViewModelBuilder`,
  `StockMovementRepository.GetStockByProductionOrderAsync`, `BusinessDayService.ParsePickupDays`), Typname `SyncLog`, Log-Zeilen
  (`MatchcodeLookup`, `BeschichtungsauftragService` — der Export erzeugt dieselben Warn-Logzeilen wie ein Seitenaufruf).
- **Pipeline:** keine globalen MVC-Filter; Middleware `WindowsAutoLoginMiddleware` + Login-Redirect lesen nur die Session
  (Same-Origin-`fetch` trägt das Session-Cookie wie der Seitenaufruf). `DefaultPageSize` wird im GET-Pfad nicht geschrieben.

### Inventar-Abgleich gegen den Bündel-Stand (AK 1)

41 View-Dateien mit `table.filterable-table` (Spec: 39) — **neu** `Beschichtungsauftraege/Index` (Server-Mode, Pagination,
v1.51.0) und `Beschichtungsauftraege/Details` (Positionstabelle, Client-Mode; im Entwurf Menge = Wert des Eingabefelds,
„Entfernen" ohne `data-col-key` fehlt). Beide bekommen den Knopf; kein Opt-out nötig. FA-Arbeitsgänge-Symbole Teil 1 (v1.52.0)
hat keine `filterable-table` (Baum) → kein Knopf, wie Spec. Jede View mit `_Pagination` ist Server-Mode (Wächter
`PaginierteTabelle_IstServerMode`); Client-Mode = Stückliste, Lagerbestellung-Details, Tracking je Werkbank,
Artikelgruppen-Zuordnung, OSEON-Reporting (mehrfach je Seite), Beschichtungsauftrag-Detail.

### Entscheidungen im Dev-Lauf (Rulings)

- **R1 Stückliste:** zusätzlich `data-csv-filter-source="attribute"` am `#bomTable` — eine Zeile ohne
  `data-csv-filter-match` (z. B. bevor `updateBomVisibility` gelaufen ist) gilt als Treffer, nie als „zugeklappt". Grund:
  ohne den Schalter fiele das Skript auf `style.display` zurück und verlöre zugeklappte Zeilen (Antwort 12).
- **R2 Toolbar-Host:** Der Knopf geht nur in ein direkt vorangehendes `div.d-flex`, sonst in einen eigenen
  `d-flex justify-content-end mb-1`-Host (strenger als das Zahnrad, das jedes `DIV` nimmt — so landet der Knopf nie in einem
  Hinweis-Kasten). Das Zahnrad findet danach diesen Host und hängt sich dazu ([[fallstricke]] §19 bleibt erfüllt).
- **R3 Spaltenzuordnung** über die **erste** Kopfzeile (`:scope > thead > tr`) — `table-filter.js` fügt die Filterzeile nur
  im Live-DOM hinzu. Sortierwert = roher Zelltext wie am Bildschirm (`textContent.trim()`), Exportwert = Zellwert-Regel.
- **R4 Mehrere Tabellen je Seite:** die abgerufene Tabelle wird über die Position unter den `filterable-table` der Seite
  (plus `id`-Abgleich) gefunden.
- **R5 Kern testbar per Node:** der DOM-freie Kern (`cleanValue`, `quoteField`, `toCsv`, `fileName`) exportiert sich unter
  Node; `Kern_NodeSelbstpruefung` (`SkippableFact`, vorhandenes Paket) führt `csv-export.check.js` aus, ohne Node wird er
  übersprungen. DOM-Teil (Zellwerte, Gruppen, Sortierung, Abruf) = Wächter + Manual-UAT.
- **R6 Injection:** geprüft wird der normalisierte Wert; ein führender Tab/CR verschwindet beim Trimmen, ein danach
  folgendes `=` wird entschärft (`"\t=1"` → `'=1`). Zahl-Ausnahme: `^[+-]?(\d{1,3}(\.\d{3})+|\d+)(,\d+)?$`.

**Beweis (vor QA):** `dotnet build IdealAkeWms.slnx` 0 Fehler (Warnungen vorbestehend); `dotnet test IdealAkeWms.slnx` Web
**1726 bestanden, 1 übersprungen** (vorher 1636; neu `CsvExportCoverageTests` inkl. Theorien je Tabellen-View und
Node-Kernprüfung — lief, nicht übersprungen), Service **272**. `docs/TESTSZENARIEN.md` Index-Blob LF.

## QA (2026-10-12)

Unabhängige QA im Bündel-Worktree, Diff `d6d6beb0..b43edb25` (3 Commits, 12 Dateien), selbst gelesen und selbst gebaut.
**Urteil: nicht Testbereit — 2 offene Important-Befunde (I-1, I-2).** Status bleibt `InUmsetzung`.

### 1. Build und Tests (selbst ausgeführt)

- `dotnet build IdealAkeWms.slnx`: **0 Fehler**, 12 Warnungen, alle vorbestehend (NU1902 MailKit/MimeKit, CS8602
  `TrackingController.cs` Z. 332, CS8321 `FormatDateWithKw` in drei Views).
- `dotnet test IdealAkeWms.slnx`:
  - `IdealAkeWms.Tests`: **1726 bestanden, 1 übersprungen, 0 Fehler** (1727 gesamt). Übersprungen ist der vorbestehende
    `ProductionOrderEagerCreateAgentJobTests.EagerCreate_TwoMergesProduce2Rows_PerNewOrder_AndAreIdempotent` (Integration),
    **nicht** die Node-Prüfung.
  - `IDEALAKEWMSService.Tests`: **272 bestanden**, 0 übersprungen, 0 Fehler.
- `Kern_NodeSelbstpruefung`: laut TRX **Passed** (Node v24.21.0 vorhanden); direkt nachgeprüft:
  `node IdealAkeWms.Tests/Js/csv-export.check.js <pfad>/csv-export.js` → `csv-export core ok`, Exit 0.
- `CsvExportCoverageTests` (Filter `~CsvExport`): 90 bestanden (Theorien je Tabellen-View).
- Commit-Blobs LF (python, `git show HEAD:<datei>`, Zählung `\r\n`): alle 10 geänderten Text-Dateien inkl.
  `docs/TESTSZENARIEN.md` = **0 CRLF**.
- **Commit `400a7520` baut allein nicht** (Razor: `@` in `Help/Index.cshtml`); `b43edb25` ändert genau diese eine Zeile
  auf `@@` (verifiziert: 1 Datei, 1 Zeile). Bewertung: Minor (M-7), nur Bisect-Lücke; das Bündel wird als Ganzes gemergt.

### 2. Code-Review (selbst, in diesem Lauf)

Gelesen: `csv-export.js` vollständig; `table-filter.js`-Diff gegen den alten Inline-Vergleich; `Bom.cshtml`
`updateBomVisibility` Z. 871-913 (Attribut wird bei **jedem** Lauf für jede Datenzeile gesetzt, auch ohne Filter → kein
veralteter Wert); `_Pagination.cshtml`; `_Layout.cshtml` Z. 409-416; `column-preferences.js` `insertGearButton`
Z. 367-390 und Init Z. 855-880; `PaginationState.cs`, `PageSize.cs` und alle 37 `ResolveRaw`-Stellen (liefern bei
`pageSize=0` den Rohwert 0 → `IsCappedAtAll` greift); alle Filter unter `Filters/` (Rechte → Redirect
`Account/AccessDenied`, Gates → `Home/Index` bzw. 404; keine dieser Zielseiten enthält eine `filterable-table` →
Fehlermeldung statt Fremddaten); Kopf- und Zellmarkup aller 41 Tabellen-Views per Skript (Toolbar-Vorgänger, `th` ohne
`data-col-key`, Anzahl `thead`-Zeilen = überall 1).

**Befunde**

| # | Schwere | Fundstelle | Befund | Umgang |
|---|---|---|---|---|
| I-1 | **Important** | `csv-export.js` Z. 122-123 (`if (!key \|\| !isVisible(th)) return;`) mit `Views/StockMovements/Index.cshtml` Z. 85 (`<th>Menge</th>`) und `Views/OrderRecipients/ArticleGroupMappings.cshtml` Z. 34 (`<th …>@group.Name</th>`) | Eingeblendete Spalten **mit** Überschrift, aber **ohne** `data-col-key` fallen **still** aus dem CSV. Bewegungshistorie: die Spalte **Menge** fehlt — genau die Liste aus TS-86.7 (negative Mengen) und TS-86.10 (Kappung). Artikelgruppen-Zuordnung: alle Gruppen-Spalten fehlen, das CSV enthält nur „Artikelgruppe“, die eigentliche Zuordnungsmatrix geht verloren. Verstößt gegen Anforderung 3 („genau die beim Klick sichtbaren Spalten“) und AK 18; Hausregel „Melden statt still behandeln“. Der Inventar-Abgleich des Dev-Laufs hat das nicht erfasst. | **Offen.** Vorschlag (kleinster Eingriff): `data-col-key="quantity"` an StockMovements Z. 85 und `data-col-key="group-@group.Id"` an ArticleGroupMappings Z. 34 (ohne `data-filterable` bleiben Filter und Sortierung unverändert), **plus** Wächter in `CsvExportCoverageTests`: jede `th` mit Text in einer `filterable-table` trägt `data-col-key` (Ausnahme: das bewusst entfallende „Entfernen“ in `Beschichtungsauftraege/Details.cshtml`). Danach TS-86.7, 86.10 und 86.12 prüfen. |
| I-2 | **Important** | `csv-export.js` Z. 81-85 (`input:not([type=hidden])` → sonst `return input.value`) mit `Views/WarehousePicking/Details.cshtml` Z. 130-143 (Spalte `shortage` „Fehlteil-Status“, Z. 72) | Radio-Gruppen werden wie ein Textfeld behandelt: geliefert wird der `value` des **ersten** Radios (`"1"`), egal welches gewählt ist und ob überhaupt eins gewählt ist. In Lagerbestellung-Details (editierbarer Zustand) steht damit in **jeder** Zeile „1“ unter „Fehlteil-Status“ — ein falscher Wert ohne Hinweis. Anforderung 11 deckt Radios nicht ab (nennt Checkbox, Eingabefeld, Select); das Ergebnis widerspricht AK 4/18 („nie mehr oder anderes als die Ansicht“). | **Offen.** Vorschlag: Radio → Beschriftung (`label[for]`) des gewählten Radios, sonst leer — oder `data-csv-value` an der Zelle (sieht Anforderung 11 vor). Manual-UAT TS-86.12 um den Fall ergänzen. |
| M-1 | Minor | `csv-export.js` Z. 225-233 mit `column-preferences.js` Z. 372-380 | Das Zahnrad **wandert** in 10 Listen. Bisher hängte `insertGearButton` es an jedes vorangehende `DIV`: in FA-Liste, Leitstand, FaCompletion, FaWorklist, Kommissionierung, Kommissionierliste (Liste/Summiert), Beschichtung (Liste) und Vormontage (beide) war das die Filterkarte bzw. die Zählzeile „N Aufträge · M Sub-FAs“. Jetzt legt `csv-export.js` (lädt früher, R2) einen eigenen Host direkt vor `.table-responsive` an, und das Zahnrad landet dort neben „CSV“. Funktional unkritisch und §19-konform, aber AK 11 und TS-86.16 sagen „Zahnrad am alten Platz“. | Mensch entscheidet im UAT: neuen Platz annehmen (dann Wortlaut von AK 11/TS-86.16 nachziehen) oder zurück. Kein Code-Fix durch die QA. |
| M-2 | Minor | `csv-export.js` Z. 15 (`KW_DATE`) mit `Views/BdeBookings/Index.cshtml` Z. 20 (`{d:dd.MM.yyyy HH:mm} KW{kw}`) | Zellen mit Datum und Uhrzeit behalten den KW-Suffix (`12.10.2026 14:30 KW42`); Excel erkennt sie nicht als Datum. Anforderung 5 nennt nur `dd.MM.yyyy`, der Fall ist in der Spec nicht bedacht. | Offen, Folgepunkt; bei der Nachbesserung von I-1/I-2 mitnehmbar (Regex um optionales ` HH:mm` erweitern, Node-Fall dazu). |
| M-3 | Minor | `csv-export.js` Z. 192-197 | Ein Login-/AccessDenied-Redirect wird nur indirekt erkannt („keine Tabelle an Position N“), `res.redirected` wird nicht geprüft. Heute sicher (Zielseiten ohne `filterable-table`, belegt). Hätte eine künftige Startseite eine Liste, würde still eine fremde Tabelle exportiert. | Hinweis; optional `res.redirected` plus Pfadvergleich. |
| M-4 | Minor | `csv-export.js` Z. 168 | `URL.revokeObjectURL` nach 0 ms; in manchen Browsern (Firefox historisch) kann das den Download abbrechen. Edge/Chrome unkritisch. | Manual-UAT im Browser der Fertigung (Checkliste Nr. 5). |
| M-5 | Minor | `Views/Shared/_Pagination.cshtml` Z. 26 | `data-total-count` wird geschrieben, aber nirgends gelesen. Spec-konform (Schritt 8), totes Attribut. | Hinweis, kein Fix. |
| M-6 | Minor | `IdealAkeWms.Tests/Js/csv-export.check.js` Z. 2 | Der Kommentar nennt `CsvExportCoreTests`, die Klasse heißt `CsvExportCoverageTests`. | Kosmetik. |
| M-7 | Minor | Commit `400a7520` | Baut allein nicht; `b43edb25` behebt genau das. | Kein Handlungsbedarf (Bündel-Merge); optional vor dem Merge zusammenfassen. |
| M-8 | Minor | `csv-export.js` Z. 125 und 129 | Fehlt ein Live-Key in der abgerufenen Kopfzeile, fällt die Spalte still weg bzw. bleibt die Sortierung still aus. Theoretisch (gleiche Action, gleiche Parameter). | Hinweis. |

**Bewusst geprüft, ohne Befund:**
- `table-filter.js`: Der Vergleich ist **wortgleich** verschoben (Datum → Zahl → `localeCompare('de')`, gleiche
  Richtungslogik); `compareRows` übergibt weiter `textContent.trim()`, der Guard `!cellA || !cellB → 0` bleibt in
  `compareRows`. `window.tableFilterCompareValues` wird beim Laden der IIFE gesetzt, nicht hinter einem frühen `return`.
  Die Bildschirm-Sortierung verhält sich unverändert.
- In `<script>` eingebettete Werte: keine. `hidden` + `d-*`: nicht berührt. Positionsbindung paralleler Arrays: nicht
  berührt. Regel nur im Browser, die der Server durchsetzen müsste: keine (reiner Lesepfad, Rechte serverseitig per
  Konstruktion).
- Eine Regel an mehreren Stellen: Der Sortiervergleich steht nur noch einmal (Wächter `localeCompare(` = 1). Die
  Sichtbarkeitsregel ist bewusst die gleichlautende Inline-Bedingung (Spec Schritt 2, Vermerk „Überholt“) — akzeptiert.
- `:scope >` in allen Zeilen-/Zell-Iterationen; `tr.children[i]` greift nur auf eigene Zellen. Gruppenkopf-,
  Kombigeräte- und Leerzeilen (`:scope > td[colspan]`) entfallen. Beschichtung Summiert: Briefkopf, PDF und Vorschlag
  liegen im `td[colspan]`-Kopf → keine Zeile.
- HauptFA-Spalte: nur bei `tbody[data-group]` (6 Views, davon `Tracking/Index` Z. 90 = Baumzweig außerhalb des Scopes).
  Die IDEAL-Listen haben kein `tbody[data-group]` → bei ausgeblendetem `hauptfa` keine Ersatzspalte (wie Spec Schritt 5).
- Kappung: `data-capped` aus `IsCappedAtAll` der **abgerufenen** Antwort; Warntext wortgleich mit AK 9; die Datei wird
  trotzdem erzeugt; ohne Kappung wird eine alte Meldung gelöscht.
- Fehlerpfad: Der Download liegt hinter allen `throw`. Der Knopf ist `disabled`, trägt `aria-busy` und einen Spinner;
  `finally` stellt ihn zurück.
- Injection/Format (per Node nachgeprüft plus eigene Stichprobe): `-`→`'-`, `- 5`→`'- 5`, `-0,5` und `-12.345.678,9`
  bleiben, `" =x"`→`'=x`, `–` (Gedankenstrich) bleibt; BOM `EF BB BF`, CRLF, kein `sep=`.
- Stückliste: `data-csv-filter-source="attribute"` + `data-csv-filter-match` (R1) schlüssig; Zeilen ohne Attribut gelten
  als Treffer.
- Toolbar-Host: Nichts wird zwischen einen bestehenden Host und `.table-responsive` gesetzt. In Bom,
  WarehousePicking/Details, Beschichtungsaufträge Index/Details und FaHierarchyBeschichtung/Summiert landen CSV und
  Zahnrad im vorhandenen leeren Host; OSEON-Reporting (Vorgänger `<h6>`) und Artikelgruppen-Zuordnung (kein Vorgänger)
  bekommen je einen eigenen Host.

### Rulings R1-R6 gegen Rumpf und Antworten 1-14

R1 (Stückliste, Antwort 12) ✔. R2 (Toolbar-Host) ✔ §19, die Nebenwirkung M-1 ist aber nicht erwähnt. R3 (erste
Kopfzeile, Sortierwert = Rohtext wie am Bildschirm) ✔ Antwort 10. R4 (Position + id) ✔, siehe M-3. R5 (Node-Kern) ✔ —
geht über die Spec hinaus („nicht per CI testbar“); der Kern ist jetzt automatisiert. R6 (Injection nach Normalisierung,
Zahl-Regex) ✔ Antwort 5 a, Regex wortgleich zur Spec.

### Wirkungsfreiheit (AK 19) — eigene Stichprobe

Action-Rümpfe per Skript (Klammer-Matching) auf alle Aufrufe gelistet, danach die aufgerufenen Lesemethoden auf
`SaveChanges|ExecuteUpdate|ExecuteDelete|ExecuteSql|BeginTransaction|.Add/.AddRange/.Update/.Remove/.Attach|Send*` geprüft.

| Action | Fundstelle | Aufrufe im Rumpf | Transitiv geprüft | Ergebnis |
|---|---|---|---|---|
| ProductionOrders.Index | `ProductionOrdersController.cs` Z. 73-474 | nur `Get*`, `Can*/Has*`, `Parse*/Subtract*/FindPrevious*` | `ProductionOrderRepository.GetForLeitstandAsync` Z. 29, `GetForLeitstandGroupedAsync` Z. 53, `FaHierarchyOrderInfoRepository.GetByHauptFaKeysAsync` Z. 39 | kein Schreibzugriff |
| PickingLeitstand.Index | `PickingLeitstandController.cs` Z. 66-352 | nur `Get*`, `Can*` | wie oben + `FaWorkStepRepository.GetWorkStepDetailPivotAsync` Z. 65 | kein Schreibzugriff |
| FaWorklist.Index | `FaWorklistController.cs` Z. 79-338 | nur `Get*`; `Add(` Z. 285 = `rows.Add(row)` (Liste im Speicher) | `FaWorkStepRepository.GetForWorkStepAsync` | kein Schreibzugriff |
| BdeBookings.Index | `BdeBookingsController.cs` Z. 39-168 | `_ctx.BdeBookings.AsNoTracking`, `Get*`, `Compute*`; `Add(` Z. 121 = `operatorDays.Add` (Speicher) | `BdeBookingRepository.GetHistoryAsync` Z. 75, `BdeTimeSplitService.ComputeForOperatorDayAsync` Z. 16 / `ComputeCumulativeEffectiveDurationAsync` Z. 119 | kein Schreibzugriff |
| StockOverview.Index | `StockOverviewController.cs` Z. 41-95 | nur `Get*` | `StockMovementRepository.GetCurrentStockAsync` Z. 15; `GetStockByProductionOrderAsync` Z. 194 (`entries.Add` = Liste) | kein Schreibzugriff |
| Beschichtungsauftraege.Index | `BeschichtungsauftraegeController.cs` Z. 72-98 | `GetDefaultPageSizeAsync`, `GetAlleAsync` | `BeschichtungsauftragRepository.GetAlleAsync` Z. 55 (`AsNoTracking`) | kein Schreibzugriff |
| FaHierarchyBeschichtung.Index | `FaHierarchyBeschichtungController.cs` Z. 62-89 | `Get*`, `Has*`, `BuildAsync`, `ErmittelnAsync` | `BeschichtungsauftragService.BuildAsync` Z. 94, `BeschichtungsauftragVergabeAnzeige.ErmittelnAsync` Z. 37 | kein Schreibzugriff |
| FaHierarchyBeschichtung.Summiert | `FaHierarchyBeschichtungController.cs` Z. 152-180 | `Get*`, `Has*`, `BuildSummiertAsync`, `ErmittelnAsync` | `BeschichtungsauftragService.BuildSummiertAsync` Z. 157 | kein Schreibzugriff |

Kein `SaveChanges`, kein `TempData`, kein Cookie/Session-Schreiben in den Rümpfen; `CurrentUserService.GetDefaultPageSizeAsync`
Z. 152 liest nur. Die Dev-Tabelle (37 Actions, Stufe 1/2) ist für diese Stichprobe bestätigt.

### Inventar (AK 1)

41 View-Dateien mit `table.filterable-table` (43 Tabellen-Tags, BdeMasterData 3, je Tab eine gerendert), keine mit
`Layout = null`, kein Opt-out. 35 Server-Mode-Views mit `_Pagination` (alle mit `pagination-bar`) und 6 Client-Mode-Views
(Stückliste, Lagerbestellung-Details, Tracking je Werkbank, Artikelgruppen-Zuordnung, OSEON-Reporting,
Beschichtungsauftrag-Detail). Beschichtungsaufträge/Index (Server-Mode, eigener Host, `ResolveRaw` Z. 77 → Kappung
greift) und /Details (Client-Mode, Menge = Wert des Eingabefelds, „Entfernen“ ohne Key entfällt) sind korrekt abgedeckt.
Es fehlen nur die zwei Spalten aus **I-1**.

### 3. Beweisart je Akzeptanzkriterium

| AK | Beweis | Bemerkung |
|---|---|---|
| 1 Knopf an jeder Tabelle | Wächter (teilweise) + **Manual-UAT** | Wächter: Layout bindet ein, keine View ohne Layout. Sichtbarkeit des Knopfs = Browser. |
| 2 alle Seiten | **Manual-UAT** | Echter zweiter GET, Model-Binding `pageSize=0` — nicht InMemory-testbar. |
| 3 Spalten wie eingeblendet | Wächter (jede Spalte mit Überschrift hat Key) + **Manual-UAT** | DOM-Zustand zur Klickzeit. I-1 behoben (QA-Nachlauf 2026-10-12). |
| 4 ohne Überschrift weg | **Manual-UAT** | Code-Review ✔ (`label`-Prüfung Z. 125). |
| 5 Format/Excel | Node-Kern (BOM, `;`, CRLF, kein `sep=`, KW-Kürzung) + **Manual-UAT** (Excel-Erkennung, Server-Kultur) | Excel-Verhalten nicht automatisierbar; siehe M-2. |
| 6 flach + HauptFA | **Manual-UAT** | Code-Review ✔. |
| 7 Injection + Zahlen-Ausnahme | **Automatisiert (Node)** + Manual-UAT Excel | Kern bewiesen; Excel-Anzeige per UAT. |
| 8 Rechte/Gates | **Manual-UAT** | Per Konstruktion (gleiche Action), Filterverhalten belegt; Browser-Fetch nicht testbar. |
| 9 Kappung + Warntext | Wächter (Text, `data-capped`) + **Manual-UAT** | Braucht > 5000 Datensätze. |
| 10 Fehlerfall | **Manual-UAT** | Code-Review ✔. |
| 11 AKE-Regression | **Manual-UAT** | M-1 beachten. |
| 12 Dateiname | **Automatisiert (Node)** | Muster + Ersetzung unzulässiger Zeichen. |
| 13 Wächter grün | **Automatisiert** ✔ | (a)-(f) vorhanden und grün; (b) prüft nur `Layout = null`. |
| 14 Build/Tests grün | **Automatisiert** ✔ | 1726/1 übersprungen + 272. |
| 15 Sortierung | Wächter (ein Vergleich) + **Manual-UAT** | Browser-JS. |
| 16 Checkbox Ja/Nein | **Manual-UAT** | Code-Review ✔; Lack-Checkbox nur bei `HasCoatingParts`, sonst leer (korrekt). |
| 17 Badges/Prio/Icon | **Manual-UAT** | Code-Review ✔ (SVG-`title` wird gefunden). |
| 18 Client-Mode | Wächter (Bom-Attribut, Spalten-Key) + **Manual-UAT** | Artikelgruppen-Matrix (I-1) und Fehlteil-Status (I-2) behoben (QA-Nachlauf 2026-10-12); Radio-Auswertung ist Browser-JS → UAT. |
| 19 Wirkungsfreiheit | **Code-Review** ✔ | Dev-Tabelle + QA-Stichprobe oben. |

Grün gezählt (automatisiert bewiesen): 7 (Kern), 12, 13, 14. Alles andere ist Manual-UAT und **nicht** als grün gezählt.

### 4. Geänderte bestehende Tests

Keine. Der Diff fügt nur `IdealAkeWms.Tests/Helpers/CsvExportCoverageTests.cs` und `IdealAkeWms.Tests/Js/csv-export.check.js`
hinzu (`git diff --name-status`: beide `A`). Sie beweisen: Einbindung, Layout-Nutzung, paginiert ⇒ Server-Mode,
skopierte Selektoren, genau ein Sortiervergleich, Bom-/Pagination-Attribute, Warntext, reiner Kern. Sie beweisen nicht
den DOM-Teil (Spaltenwahl, Zellwerte, Gruppen, Sortierung, Abruf, Download) — das ist Manual-UAT. Lücke, die I-1 gefangen
hätte: kein Wächter „jede `th` mit Text hat `data-col-key`“.

### 5. CLAUDE.md-Checkliste

- Migration / `SQL/` / FreshInstall / AgentJobs: entfällt (kein Schema) ✔. Audit-Felder: entfällt (nur Lesen) ✔.
- Version in beiden `AppVersion.cs` 1.53.0 ✔, Anwender-Changelog v1.53.0 ✔, Hilfe Abschnitt Listen ✔ (`@@` korrekt,
  rendert `@`).
- `docs/TESTSZENARIEN.md` TS-86.1-86.16 mit Vorbedingungen, Schritten, Erwartung und Negativfällen ✔ (LF).
- **Offen (Brain; nicht von der QA geschrieben, Schreibgrenze dieser QA = nur diese Spec):** TS-86 fehlt in
  `secondbrain/tests/testszenarien-index.md` (letzter Eintrag 85); `feature-map.md` hat keinen Eintrag zu CSV/1.53.0; kein
  Brain-Changelog `2026-10-12-v1-53-0-…`. Der ADR-0005-Nachtrag liegt gestagt im Hauptcheckout (nicht angefasst).
- Neue Rolle/neuer Filter: keine ✔.

### Wiedervorlage

Nach Behebung von I-1 und I-2 (plus Wächter) erneut QA: Build/Test, Node-Kern, Review der Nachbesserung. M-1 entscheidet
der Mensch im UAT; M-2 bis M-8 sind Hinweise.

### Manuelle Checkliste (für Gerald, nach I-1/I-2)

1. Prüfschritt Server-Kultur (Abschnitt Deploy): Zeigt die FA-Liste in „Stk.“ `1,000`?
2. FA-Liste AKE, Spaltenfilter + 25 je Seite, mehrere Seiten → CSV: Zeilenzahl = „von N“, Spalten = sichtbare, keine Aktionsspalte (TS-86.1).
3. Zahnrad: Spalte ausblenden und ohne Reload exportieren; Spalte verschieben → Reihenfolge im CSV (TS-86.2). **Dabei die neue Zahnrad-Position (M-1) ansehen und entscheiden.**
4. IDEAL: FA-Liste, Leitstand, FaCompletion, FaWorklist, Kommissionierung → erste Spalte „HauptFA“, keine Kopfzeilen, Kombigerät ohne Fremdzeilen (TS-86.3); IDEAL-Listen inkl. Beschichtung Summiert und Beschichtungsaufträge Index/Detail (TS-86.4).
5. Datei in Notepad++/Hex: `EF BB BF`, `;`, CRLF, kein `sep=`, Dateiname; Doppelklick in Excel: Umlaute, Mengen rechnen, Datum erkannt (TS-86.5). Im Browser der Fertigung prüfen, dass der Download zuverlässig startet (M-4).
6. Injection-Datensatz `=HYPERLINK(...)`, `@SUMME(...)`, `+43 …`, `-x` → `'` davor, Excel führt nichts aus (TS-86.6).
7. Bewegungshistorie mit negativer Menge → **Spalte Menge ist vorhanden** (I-1), `-5,000` als Zahl (TS-86.7).
8. Benutzer ohne `stock_read` → kein Zugriff, kein Export; Gate `FaHierarchyKommissionierlistenAktiv = false` → keine Liste, kein Export (TS-86.8/.9).
9. Bewegungshistorie > 5000, Datum absteigend → gelbe Warnung wortgleich, Datei mit genau 5000 Zeilen, innerhalb sortiert; Gegenprobe < 5000 ohne Warnung (TS-86.10).
10. In zweitem Tab abmelden, exportieren → rote Meldung, keine Datei, Knopf während des Abrufs deaktiviert (TS-86.11).
11. Stückliste mit Komm.-Ziel-Filter und zugeklappter Baugruppe → Treffer im CSV; ohne Filter alles; Bildschirm unverändert. Lagerbestellung-Details im **editierbaren** Zustand mit gesetztem Fehlteil-Status → richtiger Status im CSV (I-2). Tracking je Werkbank; Artikelgruppen-Zuordnung (**alle Gruppen-Spalten mit Ja/Nein**, I-1); OSEON-Reporting mit Zukunftstagen (je Tabelle ein Knopf) (TS-86.12).
12. Leitstand-Zellwerte: Glas/Zukauf/Lack als Ja/Nein, Prio als Zahl, Lack-T-Titel, Badge-Texte, „BG n“ (TS-86.13).
13. Stammdaten-Stichprobe: Artikel (inkl. dynamischer Merkmal-Spalten), Lagerplätze, Benutzer, BDE je Tab. BDE-Buchungen: Die Datum/Uhrzeit-Spalte enthält noch „KWnn“ (M-2, bekannt) (TS-86.14).
14. Sortierung Datum/Zahl/Text über alle Seiten, Gruppenliste je Gruppe, Default-Sortierung auf ausgeblendeter Spalte; Regression der Bildschirm-Sortierung (TS-86.15).
15. AKE-Regression aller Listen: Darstellung, Filter, Sortierung, Spaltenauswahl, Druck (TS-86.16).

### QA-Nachlauf (2026-10-12)

Gezielter Nachlauf im Bündel-Worktree, Fix-Commit `9ad794b1` (Diff `b43edb25..9ad794b1`, 6 Dateien, +52/−14), selbst
gelesen und selbst gebaut. **Urteil: Testbereit — I-1 und I-2 behoben, kein offenes Critical/Important.** Status
`InUmsetzung` → `Testbereit`.

**Build und Tests (selbst ausgeführt, Worktree sauber):**
- `dotnet build IdealAkeWms.slnx`: **0 Fehler**, 12 Warnungen (unverändert vorbestehend: NU1902, CS8602, CS8321).
- `dotnet test IdealAkeWms.slnx`:
  - `IdealAkeWms.Tests`: **1767 bestanden, 1 übersprungen, 0 Fehler** (1768). +41 gegenüber der ersten QA = neue Theorie
    `JedeUeberschriebeneSpalte_HatEinenSchluessel` je Tabellen-View. Übersprungen: weiter nur der Integrationstest
    `ProductionOrderEagerCreateAgentJobTests.EagerCreate_TwoMergesProduce2Rows_PerNewOrder_AndAreIdempotent`.
  - `IDEALAKEWMSService.Tests`: **272 bestanden**, 0 übersprungen, 0 Fehler.
- Node-Kern direkt: `node IdealAkeWms.Tests/Js/csv-export.check.js …/csv-export.js` → `csv-export core ok`, Exit 0
  (inkl. neuem Fall `12.10.2026 07:45 KW42` → `12.10.2026 07:45`).
- Commit-Blobs `9ad794b1` der 6 Dateien: 0 CRLF.

**Prüfung der Nachbesserung**

| Punkt | Ergebnis | Beleg |
|---|---|---|
| I-1 Keys | **behoben** | `StockMovements/Index.cshtml` Z. 85 `data-col-key="quantity"`, `ArticleGroupMappings.cshtml` Z. 34 `data-col-key="group-@group.Id"` (eindeutig je Gruppe; Zellen = Checkbox → Ja/Nein). Beide **ohne** `data-filterable`: `table-filter.js` bindet Sortierung/Filter nur an `th[data-filterable]` (Z. 145, 244) → Bildschirm unverändert. Weitere Leser von `th[data-col-key]` (`column-preferences.js` Z. 745/763, `print-visible-columns.js` Z. 14) greifen nur bei `data-view-key`, den beide Views nicht haben. |
| I-1 Abbruch statt still | **behoben** | `csv-export.js` Z. 128-134: sichtbare Spalte mit Überschrift ohne Key bzw. ohne Gegenstück in der abgerufenen Tabelle → `throw` → rote Meldung (Z. 225), keine Datei. Damit ist auch M-8 (Spaltenteil) erledigt; die Sortierung (Z. 138) bleibt bei fehlendem Key weiter still aus — Rest von M-8, Hinweis. |
| I-1 Wächter | **wirksam** | `CsvExportCoverageTests.cs` Z. 79-97, Theorie über alle 41 Views; Opt-out `data-csv-export="false"` am `<th>` wird vom Skript (Z. 128) und vom Wächter beachtet. `Beschichtungsauftraege/Details.cshtml` Z. 222 „Entfernen" = gewollt ohne Export (Spec-Inventar: entfällt). |
| Neue Abbrüche in bestehenden Listen? | **keine gefunden** | Eigener Skript-Scan aller 41 Views (erste Kopfzeile je `filterable-table`): keine Kopfzellen aus Partials/HTML-Helpern, keine Attribute mit `>`, überall genau eine Kopfzeile. Per JS erzeugte `th`: nur die Filterzeile (`table-filter.js` Z. 154, zweite Zeile — `headerRow` liest die erste) und das Kalender-Popup (eigene Tabelle, Z. 486); keine JS-erzeugte `filterable-table` (Grep). Bedingte Kopfspalten in Server-Mode-Views (`Articles`, `FaWorklist` = Merkmal-Definitionen; sonst Rechte/Settings/`Hierarchical`) hängen nicht von den Seitendaten ab → Live- und Voll-Abruf haben dieselben Keys. Kein `th` mit `d-none`-Responsive-Klassen. |
| I-2 Radio | **behoben** | `csv-export.js` Z. 81-86: gewähltes Radio → `label[for=id]`-Text, sonst `value`, keine Auswahl → leer. `WarehousePicking/Details.cshtml` Z. 130-143: IDs `sr_<Id>_1/2`, Labels „Fehlteil" / „Wird nicht nachgeliefert" → im CSV der sichtbare Text. `data-csv-value` hat weiter Vorrang. |
| M-1 Zahnrad | **behoben** | Host-Regel `csv-export.js` Z. 239-244 ist wortgleich zu `column-preferences.js` `insertGearButton` Z. 372-389. Reihenfolge: `csv-export.js` hängt seine `span` beim `DOMContentLoaded`; `column-preferences.js` startet erst per `requestIdleCallback` (Timeout 500) bzw. `setTimeout 100` (Z. 870-874) und danach asynchron `loadSettings` (Z. 878) → `previousElementSibling` ist zu diesem Zeitpunkt noch derselbe Container (die `span` liegt **in** ihm), das Zahnrad landet im selben Container wie vor v1.53.0, als letztes Kind rechts von „CSV". Fehlt ein DIV-Vorgänger, legt csv-export den Host an, den sonst das Zahnrad selbst angelegt hätte (gleiche Klassen) → gleiche Position. Fremde Container bekommen keine Klassen mehr. Sichtbar anders: das Zahnrad rückt im Container um die Breite von „CSV" nach rechts — UAT TS-86.16. |
| M-2 KW nach Uhrzeit | **behoben** | `KW_DATE` Z. 15 erlaubt optional ` HH:mm`; Whitespace wird vorher normalisiert (Z. 21); einzige Quelle `BdeBookings/Index.cshtml` Z. 20. |
| M-6 | behoben | Kommentar nennt `CsvExportCoverageTests.Kern_NodeSelbstpruefung`. |

**Befunde des Nachlaufs**

| # | Schwere | Fundstelle | Befund | Umgang |
|---|---|---|---|---|
| M-9 | Minor | `docs/TESTSZENARIEN.md` Z. 9602-9616 (TS-86.12/86.14/86.16) | Die Nachbesserung ist in den Szenarien nicht nachgezogen: kein Fall „Fehlteil-Status im editierbaren Zustand → Beschriftung der gewählten Option" (I-2), „Artikelgruppen-Zuordnung: alle Gruppen-Spalten mit Ja/Nein" (I-1), BDE-Buchungen Datum/Uhrzeit ohne KW (M-2), Negativfall Meldung „Spalte … ist nicht zuordenbar". Die manuelle Checkliste unten deckt alles ab; das SSOT-Dokument nicht. | Offen, vor dem Merge in `docs/TESTSZENARIEN.md` (Worktree) ergänzen. Kein Code-Fix. |
| M-10 | Minor | `csv-export.js` Z. 188-192 mit Z. 245-247 | Die Meldung (`div.alert`) wird jetzt in die Inline-`span` gesetzt, die je nach Liste in der Filterkarte oder der Zählzeile (`small text-muted`) liegt — Darstellung der gelben/roten Meldung dort unerprobt. | UAT TS-86.10/86.11 ansehen. |
| — | Hinweis | Brain (Hauptcheckout) | Unverändert offen aus der ersten QA: TS-86 fehlt in `secondbrain/tests/testszenarien-index.md` (letzter Eintrag 85), kein Eintrag in `feature-map.md`, kein Brain-Changelog v1.53.0. Nicht von der QA geschrieben (Schreibgrenze dieses Laufs = nur diese Spec). | Aufrufer zieht nach. |

M-3, M-4, M-5, M-7 bewusst unverändert (Hinweise). Regressionen durch den Fix: keine gefunden.

**Beweisart AK 3 und AK 18 (berichtigt, Tabelle oben angepasst):** beide **Manual-UAT**, gestützt durch den neuen Wächter
(jede Spalte mit Überschrift hat einen Key oder ist bewusst ausgenommen). Der Wächter beweist die Zuordenbarkeit im
Quelltext, nicht den DOM-Zustand zur Klickzeit und nicht die Radio-Auswertung — nicht als grün gezählt. Grün gezählt
bleiben 7 (Kern), 12, 13, 14.

**Geänderte bestehende Tests:** keine vorbestehenden (aus `main`). Der Fix ergänzt `CsvExportCoverageTests.cs` (auf diesem
Zweig neu) um eine Theorie und `csv-export.check.js` um einen Fall plus Kommentar — beides Erweiterung, keine
Anpassung bestehender Erwartungen.

### Manuelle Checkliste — Ergänzung aus dem Nachlauf

Die Checkliste oben (1-15) gilt weiter; „Zahnrad-Position entscheiden" in Nr. 3 entfällt (M-1 behoben). Zusätzlich:

16. Bewegungshistorie → CSV enthält die Spalte **Menge** an der Bildschirmposition (I-1).
17. Artikelgruppen-Zuordnung → CSV hat „Artikelgruppe" plus **je Gruppe eine Spalte** mit Ja/Nein passend zu den Haken (I-1).
18. Lagerbestellung-Details im editierbaren Zustand: eine Zeile „Fehlteil", eine „Wird nicht nachgeliefert", eine ohne Wahl,
    eine Option nur am Bildschirm umschalten (nicht speichern) → CSV zeigt genau diese Texte bzw. leer (I-2).
19. BDE-Buchungen → Datum/Uhrzeit-Spalte ohne „KWnn", Excel erkennt Datum mit Uhrzeit (M-2).
20. In einer FA-Liste (IDEAL hierarchisch, Zählzeile) und einer Liste mit Filterkarte: Zahnrad im selben Bereich wie vor
    v1.53.0, „CSV" links daneben; Kappungs- und Fehlermeldung dort gut lesbar (M-1, M-10).
21. Beschichtungsauftrag-Detail (Entwurf, bearbeitbar) → CSV ohne Spalte „Entfernen", kein Abbruch.

### Abschluss Dev-Lauf (2026-10-12)

- **M-9 behoben** (`87db96c8`): TS-86.17 deckt Menge, Gruppen-Spalten, Radio-Beschriftung, BDE-Datum/Uhrzeit ohne KW, den Abbruch „nicht zuordenbar" und das fehlende „Entfernen" ab.
- Offen wie von QA eingestuft (Minor, akzeptiert): M-3 (`res.redirected` ungeprüft, heute sicher), M-4 (`revokeObjectURL` nach 0 ms — im Produktivbrowser prüfen), M-5 (`data-total-count` ungelesen, Spec-Vorgabe), M-7 (`400a7520` baut allein nicht), Rest M-8, M-10 (Meldung im fremden Container — UAT).
