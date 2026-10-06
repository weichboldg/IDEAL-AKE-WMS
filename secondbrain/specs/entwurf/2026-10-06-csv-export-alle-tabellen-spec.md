---
type: spec
title: "CSV-Export fuer alle Tabellen (querschnittlich)"
slug: 2026-10-06-csv-export-alle-tabellen-spec
status: Entwurf
created: 2026-10-06
updated: 2026-10-06
source_backlog: "[[2026-10-06-csv-export-alle-tabellen]]"
task: ""
worktree: ""
branch: ""
affected_code:
  - "NEU IdealAkeWms/wwwroot/js/csv-export.js — gemeinsamer Baustein (Variante A): Knopf je table.filterable-table, Zeilen holen, Spalten aus dem DOM, CSV erzeugen, Download"
  - "IdealAkeWms/Views/Shared/_Layout.cshtml — csv-export.js einmal einbinden (Variante A, Rueckfrage 6); bei Partial-Variante stattdessen je View"
  - "IdealAkeWms/Views/Shared/_Pagination.cshtml Z. 6-12 + 25-26 — maschinenlesbares data-total-count/data-capped am .pagination-bar (Cap-Erkennung, Rueckfrage 9)"
  - "IdealAkeWms/wwwroot/js/print-visible-columns.js Z. 14-19 — Sichtbarkeitsregel (style.display/d-none) wird wiederverwendet, nicht kopiert"
  - "IdealAkeWms/wwwroot/js/column-preferences.js Z. 162-175 (Ausblenden per style.display), Z. 227 (physisches Umordnen), Z. 387-388 (Einfuegeort Zahnrad-Knopf) — Bezugspunkte, keine Aenderung erwartet"
  - "IdealAkeWms/wwwroot/js/table-filter.js Z. 333-345 (sortTable/compareRows) — nur falls Rueckfrage 10 = Sortierung uebernehmen: Comparator exportieren statt duplizieren"
  - "IdealAkeWms/Services/PageSize.cs Z. 12 (AllCap = 5000) — Obergrenze des Exports, keine Aenderung"
  - "40 Tabellen-Views (Inventar im Abschnitt Technischer Loesungsentwurf) — bei Variante A nur dort, wo Opt-out oder Zell-Exportwerte noetig sind"
  - "IdealAkeWms/Views/Help/*.cshtml (Hilfeseite Listen) + Views/Help/Changelog.cshtml, beide AppVersion.cs"
  - "NEU IdealAkeWms.Tests/Helpers/CsvExportCoverageTests.cs — Quelltext-Waechter nach Muster TableScriptScopedSelectorTests.cs"
  - "docs/TESTSZENARIEN.md + secondbrain/tests/testszenarien-index.md — neues Kapitel TS-<naechste freie>"
open_questions:
  - "1 Welche Zeilen: alle Zeilen des aktuellen Filters ueber alle Seiten (Empfehlung) oder nur die sichtbare Seite?"
  - "2 Welche Spalten: die beim Klick sichtbaren, in Bildschirm-Reihenfolge (Empfehlung, Druck/PDF-Konvention)? Aktions-/Checkbox-Spalten ohne Ueberschrift nie?"
  - "3 Format: Semikolon, UTF-8 mit BOM, Dezimalkomma, Datum dd.MM.yyyy ohne KW-Suffix, Dateiname <Ansicht>_yyyy-MM-dd_HHmm.csv (Empfehlung)?"
  - "4 Gruppierte Listen flach mit Gruppenschluessel (HauptFA) als eigener Spalte (Empfehlung)? Gruppen-Kopfzeilen und Kombigeraete-Variantenzeile entfallen?"
  - "5 Sicherheit: CSV-Injection per Apostroph-Praefix bei = + - @ Tab CR, reine Zahlen ausgenommen; Export-Recht = Leserecht der Ansicht, kein eigenes Recht, kein Toggle, keine Export-Protokollierung (Empfehlung)?"
  - "6 Gemeinsamer Baustein: ein Skript im Layout, das sich an jede filterbare Tabelle haengt (Spec-Empfehlung, weicht von Helper+Button-Partial der Notiz ab) oder Button-Partial je View?"
  - "7 Umfang: alle 40 Tabellen mit Klasse filterable-table in EINER Spec; Baumansichten und Formular-/Detailtabellen ausserhalb (Empfehlung)? Teil-Specs noetig?"
  - "8 Variante: A = Erzeugung im Browser aus derselben Index-Action (Empfehlung, ein Dev-Lauf) oder B = Export-Action je Controller (Epic mit Etappen)?"
  - "9 Mehr als 5000 Treffer (PageSize.AllCap, bei Gruppenlisten 5000 Gruppen): erste 5000 exportieren plus sichtbare Warnung (Empfehlung) oder Export sperren?"
  - "10 Sortierung: aktive Bildschirm-Sortierung auf den Export anwenden, je Gruppe, mit dem Comparator aus table-filter.js (Empfehlung) oder Serverreihenfolge?"
  - "11 Zellinhalt: Text inkl. Badges normalisiert, Checkbox als Ja/Nein, Eingabefelder mit Wert, Icon-Zellen ueber title (Empfehlung)? Nur farblich transportierter Zeilenstatus bleibt aussen vor?"
  - "12 Client-Mode-Listen (Stueckliste, Lagerbestellung-Details, Tracking je Werkbank, Artikelgruppen-Zuordnung, OSEON-Reporting): alle Filtertreffer, in der Stueckliste unabhaengig vom Klappzustand des Baums (Empfehlung)?"
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

> [!info] Umsetzungsort (Hinweis, keine Entscheidung)
> Nach Schranke 2 des IDEAL-Buendels, aus `main` (neuer Worktree). Das Buendel aendert fast alle
> betroffenen Listen-Views. TS-Nummer und Version: jeweils die **naechste freie zum
> Umsetzungszeitpunkt** (Buendel-Stand: `AppVersion.cs` Z. 5 = **1.47.2**, `docs/TESTSZENARIEN.md`
> endet bei **TS-80**, Z. 8959).

> [!note] Code-Grundlage
> Alle Datei:Zeile-Angaben beziehen sich auf den Buendel-Worktree
> `.claude/worktrees/2026-08-07-ideal-teile-1-5` (gelesen, nicht veraendert). Pfade relativ zu
> `IdealAkeWms/`, sofern nicht anders angegeben.

## Ziel / Nutzen (das Warum)

Anwender in Lager, Leitstand und Verwaltung uebertragen Listeninhalte heute per Hand oder per
Kopieren aus dem Browser nach Excel. Ein einheitlicher CSV-Export liefert genau den Ausschnitt,
den der Anwender gerade gefiltert hat und sieht, ohne Medienbruch. Weil er querschnittlich ist,
muss er **ein** Baustein sein. 40 Einzelloesungen waeren 40 Stellen, an denen Spalten, Filter oder
Rechte auseinanderlaufen. Dieselbe Fehlerklasse hat beim Druck schon drei synchron zu haltende
Stellen erzeugt (siehe [[fallstricke]], „Lagerbestellungs-Druck: drei Stellen synchron halten“).

## Umfang (In-Scope / Out-of-Scope)

**In-Scope (vorbehaltlich Rueckfrage 7):**
- Ein CSV-Export-Knopf an jeder Tabelle mit der Klasse `filterable-table` (40 Tabellen in 38
  View-Dateien, Inventar unten). Das gilt fuer AKE- und IDEAL-Listen gleichermassen, also bei
  Master `ProduktionsauftragHierarchisch` `false` und `true`.
- Export der gefilterten Zeilen mit den sichtbaren Spalten, Excel-tauglich (Rueckfragen 1-3).
- Gruppierte Listen flach (Rueckfrage 4).
- CSV-Injection-Schutz und Rechtegleichheit mit der Ansicht (Rueckfrage 5).
- Hilfeseite (Abschnitt Listen) und Anwender-Changelog, Testszenario-Kapitel.

**Out-of-Scope (Empfehlung, Rueckfrage 7):**
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

1. Jede Tabelle im Scope zeigt einen Knopf „CSV“ an derselben Stelle (Empfehlung: links neben dem
   Spaltenauswahl-Zahnrad bzw. ueber der Tabelle).
2. Der Export enthaelt die Zeilen, die die Ansicht mit dem **aktuellen** Filterstand liefert
   (Filterkarte + Spaltenfilter `colf_*` + Ansichtsschalter wie `showDone`/`target`). Das gilt ueber
   alle Seiten (Rueckfrage 1), begrenzt durch `PageSize.AllCap` (Rueckfrage 9).
3. Der Export enthaelt genau die beim Klick sichtbaren Spalten in Bildschirm-Reihenfolge
   (Rueckfrage 2). Kopfzeile = sichtbare Spaltenueberschriften.
4. Der Export zeigt nie mehr als die Ansicht: weder Zeilen, die der Anwender mit seinem Recht nicht
   sieht, noch ausgeblendete Spalten (Rueckfrage 5).
5. Die Datei oeffnet sich per Doppelklick in Excel (Windows, deutsches Gebietsschema) mit korrekten
   Umlauten, getrennten Spalten und als Zahl erkannten Mengen (Rueckfrage 3).
6. Gruppierte Listen werden flach exportiert (Rueckfrage 4).
7. Zellinhalte, die mit `=`, `+`, `-` oder `@` beginnen, werden entschaerft (Rueckfrage 5).
8. Ist die Treffermenge gekappt, erscheint vor oder mit dem Download ein sichtbarer Hinweis
   (Rueckfrage 9). Ein stilles Abschneiden ist ausgeschlossen (Hausregel „Melden statt still
   behandeln“).

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
`PageSize\.Resolve\(` in `Controllers/` ergibt 35 Treffer in 32 Controllern, darunter auch
`Summiert` (`FaHierarchyKommissionierListenController.cs` Z. 128, `FaHierarchyVormontageController.cs`
Z. 108), `TrackingController.cs` Z. 308 (OseonIndex) und `FaHierarchyController.cs` Z. 60, z. B.
`ProductionOrdersController.cs` Z. 82.
`pageSize=0` bedeutet „Alle“ und wird auf `AllCap = 5000` gekappt (`Services/PageSize.cs`
Z. 12-30). In Gruppenlisten paginiert der Server ueber **Gruppen**, nicht ueber Zeilen
(`ProductionOrdersController.cs` Z. 249-252, 376 und 402).

**Spaltensichtbarkeit und -reihenfolge leben im DOM.** `column-preferences.js` blendet per
`style.display` aus (Z. 162-175) und ordnet Zellen physisch um (Z. 227). Der Druck liest die
sichtbaren `th[data-col-key]` zur Klickzeit (`print-visible-columns.js` Z. 14-19, Regel
`style.display !== 'none' && !classList.contains('d-none')`). Diese Konvention stammt aus
[[2026-09-25-kommissionierliste-summierung-pdf-spec]], Entscheidung B1. Die vom Server
ausgelieferte HTML-Fassung hat die **Serverreihenfolge** und **keine** angewandten Praeferenzen.

**Sortierung ist rein clientseitig** und wird nicht in der URL gefuehrt. `sortTable`/`compareRows`
stehen in `table-filter.js` Z. 333-345, die Sortierung laeuft je `<tbody>` getrennt (siehe
[[fallstricke]], Abschnitt Sort-Bug). Ein Export ueber alle Seiten kann die Bildschirmreihenfolge
daher nur durch Nachsortieren treffen (Rueckfrage 10).

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
(Z. 255). Deren `textContent` ist leer (Rueckfrage 11).

**Gruppenstruktur:** Je Gruppe gibt es ein `<tbody>`. Die FA-Listen-Familie traegt den
HauptFA-Schluessel **nur** in `tbody[data-group]`: `ProductionOrders/Index.cshtml` Z. 137,
`PickingLeitstand/Index.cshtml` Z. 183, `FaCompletion/Index.cshtml` Z. 96,
`FaWorklist/Index.cshtml` Z. 131, `Picking/Index.cshtml` Z. 82 und `Tracking/Index.cshtml` Z. 90.
Die Zeile selbst zeigt dort die Sub-FA-Nummer (`_ProductionOrderRow.cshtml` Z. 13). Die
IDEAL-FaHierarchy-Listen haben dagegen eine eigene Spalte `hauptfa`
(`FaHierarchyKommissionierListen/Index.cshtml` Z. 75, `Summiert.cshtml` Z. 81,
`FaHierarchyBeschichtung/Index.cshtml` Z. 38, `FaHierarchyVormontage/Index.cshtml` Z. 75).
Gruppenkoepfe sind `td[colspan]`-Zeilen (z. B. `ProductionOrders/Index.cshtml` Z. 138-168 und
`FaHierarchyKommissionierListen/Index.cshtml` Z. 103-140). Die Kombigeraete-Variantenzeile enthaelt
eine **verschachtelte Tabelle** (`ProductionOrders/Index.cshtml` Z. 176-203). Hier gilt die
`:scope >`-Regel aus [[fallstricke]].

### Inventar der Tabellen (Buendel-Stand)

Legende: **reg.** = `viewKey` in `ColumnDefinitions.GetByViewKey` (`Models/ViewModels/ColumnDefinitions.cs`
Z. 467-485). **Prefs** = `#view-config`-Block vorhanden, also wirksame Spaltenauswahl. **Gr.** =
gruppiert (mehrere `<tbody>`). Zugriff = Klassen-Attribut am Controller, ausser wo eine Aktion
genannt ist. Eine mit Gate markierte Zeile hat zusaetzlich ein Feature-Gate.

**Server-Spaltenfilter + Pagination (35 Tabellen):**

| # | View (Tabelle) | data-view-key | reg. / Prefs | Pagination | Gr. | Zugriff |
|---|---|---|---|---|---|---|
| 1 | `ProductionOrders/Index.cshtml` Z. 75 | ProductionOrders | ja Z. 469 / ja Z. 238 | Z. 236 | hier. | `ProductionOrdersController.cs` Z. 12 `RequirePickingOrTrackingOrLeitstandAccess` |
| 2 | `PickingLeitstand/Index.cshtml` Z. 101 | PickingLeitstand | ja Z. 470 / ja Z. 386 | Z. 239 | hier. | `PickingLeitstandController.cs` Z. 11 `RequirePickingOrLeitstandAccess` |
| 3 | `FaCompletion/Index.cshtml` Z. 63 | FaCompletion | ja Z. 471 / ja Z. 146 | Z. 144 | hier. | `FaCompletionController.cs` Z. 20 `RequireFaCompletionAccess` |
| 4 | `FaWorklist/Index.cshtml` Z. 99 | FaWorklist | ja Z. 472 / ja Z. 181 | Z. 179 | hier. | `FaWorklistController.cs` Z. 20 `RequireVorbauAccess` |
| 5 | `Picking/Index.cshtml` Z. 48 | Picking | ja Z. 473 / ja Z. 116 | Z. 113 | hier. | `PickingController.cs` Z. 118 (Aktion) `RequirePickingAccess` |
| 6 | `BdeBookings/Index.cshtml` Z. 82 | BdeBookings | ja Z. 476 / **nein** (Skript Z. 195, kein Config-Block → `column-preferences.js` Z. 844 bricht ab) | Z. 192 | – | `BdeBookingsController.cs` Z. 12-13 `RequireBdeActive` (Gate) + `RequireBdeShiftleadAccess` |
| 7 | `FaHierarchyKommissionierListen/Index.cshtml` Z. 72 | FaHierarchyKommissionierListen | ja Z. 478 / ja Z. 165 | Z. 163 | ja | `FaHierarchyKommissionierListenController.cs` Z. 30-31 `RequireLagerProcessingAccess` + Gate |
| 8 | `FaHierarchyKommissionierListen/Summiert.cshtml` Z. 78 | FaHierarchyKommissionierSummiert | ja Z. 479 / ja Z. 136 | Z. 134 | ja | wie 7 |
| 9 | `FaHierarchyBeschichtung/Index.cshtml` Z. 35 | FaHierarchyBeschichtung | ja Z. 480 / ja Z. 126 | Z. 124 | ja | `FaHierarchyBeschichtungController.cs` Z. 27-28 `RequireBeschichtungsauftragAccess` + Gate |
| 10 | `FaHierarchyVormontage/Index.cshtml` Z. 72 | FaHierarchyVormontageEinzeln | ja Z. 481 / ja Z. 150 | Z. 148 | ja | `FaHierarchyVormontageController.cs` Z. 29-30 `RequireVorbauAccess` + Gate |
| 11 | `FaHierarchyVormontage/Summiert.cshtml` Z. 77 | FaHierarchyVormontageSummiert | ja Z. 482 / ja Z. 109 | Z. 107 | – | wie 10 |
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

**Client-Spaltenfilter, ohne Pagination (5 Tabellen):**

| # | View (Tabelle) | data-view-key | reg. / Prefs | Zugriff |
|---|---|---|---|---|
| 36 | `Picking/Bom.cshtml` Z. 142 (Baum + Filter, ADR-0005-Ausnahme „BOM-Tree“) | Bom | ja Z. 475 / ja Z. 655 | `PickingController.cs` Z. 279 (Aktion `Bom`) `RequirePickingAccess` |
| 37 | `WarehousePicking/Details.cshtml` Z. 55 | WarehousePickingDetails | ja Z. 477 / ja Z. 187 | `WarehousePickingController.cs` Z. 10-11 |
| 38 | `Tracking/ByWorkplace.cshtml` Z. 55 | TrackingByWorkplace | nein / nein | `TrackingController.cs` Z. 12 |
| 39 | `OrderRecipients/ArticleGroupMappings.cshtml` Z. 28 | – | – | `OrderRecipientsController.cs` Z. 10 |
| 40 | `OseonReporting/_OseonReportingTable.cshtml` Z. 4 | – | – | `OseonReportingController.cs` Z. 11 `RequireReportingAccess` |

Herkunft: grep `filterable-table` und `data-server-column-filter` unter `Views/` (40 Tabellen,
ohne den Changelog-Texttreffer `Help/Changelog.cshtml` Z. 1728 und den JS-Selektor
`WarehousePicking/Details.cshtml` Z. 311), `_Pagination` unter `Views/` und
`id="view-config"` unter `Views/` (14 Treffer). Nicht registrierte `data-view-key` sind fuer den
Export unschaedlich. Sie dienen nur der sessionStorage-Filterpersistenz in `table-filter.js`
Z. 18-24 und brauchen **keine** Registrierung, solange die Liste keine Spaltenauswahl bekommt.

### Variante A (Empfehlung): Erzeugung im Browser aus derselben Index-Action

Ein Skript `wwwroot/js/csv-export.js` wird einmal im Layout eingebunden (Rueckfrage 6) und arbeitet
so:
1. Es haengt an jede `table.filterable-table` ohne `data-csv-export="false"` einen Knopf „CSV“.
   Der Einfuegeort ist derselbe Mechanismus wie das Zahnrad (`column-preferences.js` Z. 387-388).
2. Beim Klick liest es die **sichtbaren** `thead th[data-col-key]` der Live-Tabelle in
   DOM-Reihenfolge. Dafuer gilt dieselbe Regel wie in `print-visible-columns.js` Z. 16. Spalten
   ohne Ueberschrifttext (Aktions- und Checkbox-Spalten wie `actions`/`row-actions`,
   `ProductionOrders/Index.cshtml` Z. 80 und 111) fallen weg (Rueckfrage 2).
3. **Server-Mode** (`data-server-column-filter="true"`): Das Skript holt per `fetch` dieselbe URL
   mit `pageSize=0&page=1` (same-origin, Windows-Auth-Cookie bzw. Negotiate wie die Seite),
   parst die Antwort mit `DOMParser` und ordnet die Zellen ueber den `data-col-key`-Index der
   **abgerufenen** Kopfzeile den sichtbaren Spalten zu. Grund: Die Antwort hat Serverreihenfolge
   ohne Praeferenzen. Das ist dieselbe Action mit denselben Filter-Attributen und denselben
   Parametern. Rechte, Gates, Filter und Datumspostfilter gelten deshalb **per Konstruktion**
   identisch (AK 8). Es entsteht kein neuer Endpunkt.
   **Client-Mode:** Das Skript nimmt die Zeilen der Live-Tabelle, die der Client-Filter nicht
   ausgeblendet hat (Rueckfrage 12).
4. Zeilen: alle `:scope > tbody > tr`. Zeilen mit `td[colspan]` werden ausgelassen: Gruppenkoepfe,
   Kombigeraete-Variantenzeile und Leerzustand. Fuer jede DOM-Iteration gilt `:scope >`
   ([[fallstricke]]: „Jede DOM-Iteration im Tree-Table braucht `:scope >`“), sonst laesst die
   verschachtelte Variantentabelle Fremdzeilen ins CSV.
5. Gruppenschluessel: Steht er in `tbody[data-group]` und hat die Tabelle keine sichtbare
   `hauptfa`-Spalte, kommt eine erste Spalte „HauptFA“ hinzu (Rueckfrage 4).
6. Zellwert: siehe Rueckfrage 11. Danach werden Whitespace normalisiert, der KW-Suffix an Datumswerten
   entfernt (Rueckfrage 3) und die CSV-Injection entschaerft (Rueckfrage 5). Felder werden nach
   RFC 4180 maskiert: Anfuehrungszeichen bei `;`, `"`, Zeilenumbruch, `"` verdoppelt.
7. Ausgabe als `Blob` mit UTF-8-BOM, Trenner `;`, Zeilenende CRLF. Download ueber `a[download]`,
   Dateiname `<data-view-key | Controller>_yyyy-MM-dd_HHmm.csv`.
8. Cap: `_Pagination.cshtml` bekommt am `.pagination-bar` (Z. 25) die Attribute `data-total-count`
   und `data-capped`. Ist `data-capped="true"`, zeigt das Skript sichtbar eine Warnung
   (Rueckfrage 9).
9. Sortierung: siehe Rueckfrage 10. Falls die Sortierung uebernommen wird, stellt
   `table-filter.js` seinen Comparator bereit, z. B. `window.tableFilterCompare`. Kopieren ist
   ausgeschlossen, sonst entstuende die „drei Stellen synchron halten“-Klasse aus [[fallstricke]].
10. Waehrend des Abrufs wird der Knopf deaktiviert und zeigt einen Spinner. Ein Fehler (HTTP ≠ 200,
    Redirect auf Login/AccessDenied, keine Tabelle in der Antwort) zeigt eine sichtbare
    Fehlermeldung, und es wird **keine** Datei erzeugt. Ein leeres CSV ist ausgeschlossen.

Pflicht zur Wirkungsfreiheit: Der Export ruft die Index-Action ein zweites Mal per GET auf.
Eine Stichprobe (grep `\.(Add|Update|Remove|Upsert|Save|Mark|Set)\w*Async\(` in den
Controllern der FA-Familie, IDEAL-Listen, Tracking, MissingParts und WarehousePicking) fand **keinen**
Schreibaufruf innerhalb der Index-/Summiert-Rumpfe. Schreibvorgaenge, die indirekt ueber Services
laufen, sind damit **nicht** ausgeschlossen. Der Dev-Lauf prueft deshalb je Index-Action im Scope den
Aufrufpfad auf Schreibzugriffe und dokumentiert das Ergebnis im QA-Nachweis.

Ponytail-Leiter: Wiederverwendet werden Index-Action, Rechte, Filter, `PageSize.AllCap`, die
Sichtbarkeitsregel und optional der Sort-Comparator. Neu ist nur ein Skript plus zwei Attribute im
Pagination-Partial. Bewusst entfaellt: neue Endpunkte, eine C#-CSV-Bibliothek und Code je
Controller.

**Grenzen von A:**
- Der CSV-Inhalt ist der **gerenderte** Zellentext. Das ist derselbe Grundsatz wie beim
  Spaltenfilter (ADR 0005: „der ColumnMap-Getter liefert den gerenderten Zellentext“).
- Die Injection- und Escape-Logik ist mangels JS-Testinfrastruktur nicht per CI testbar. Sie wird
  abgesichert durch den Quelltext-Waechter (Einbindung, `:scope >`) und durch Negativfaelle im
  Manual-UAT.

### Variante B (Alternative): Export-Action je Controller

Jeder Controller bekommt eine `Export`-Action. Sie baut die Liste wie `Index`, nur ungepaginiert,
und schreibt das CSV ueber einen gemeinsamen, per xUnit testbaren `CsvWriter`-Helper.
- Vorteil: getypte Werte (Zahlen und Daten ohne Anzeigeformat), Logik in C# testbar.
- Nachteil: Die Index-Rumpfe bauen ihr ViewModel inline, z. B. `ProductionOrdersController.Index`
  Z. 73-466 mit Flach- und Hierarchiezweig, Postfiltern und Kopfdaten-Anreicherung. Jede Liste
  braucht also eine Extraktion der Abfragelogik oder eine zweite Kopie, das heisst 35 bis 40
  Eingriffe in produktive Controller. Die Spaltenauswahl muss serverseitig nachgebaut werden
  (`ResolveVisibleColumnsAsync`-Muster aus [[2026-09-25-kommissionierliste-summierung-pdf-spec]]),
  ebenso die Zuordnung col-key → Wert je Liste, also eine zweite Wahrheit neben der View.
- Umfang: **Epic** (Etappenvorschlag in Rueckfrage 8).

**Empfehlung: A.** Rechtegleichheit und Filtergleichheit sind bei A per Konstruktion gegeben und
nicht Ergebnis von Disziplin. Der Umfang passt in einen Dev-Lauf. Die Zellwert-Schwaechen von A
lassen sich je Liste gezielt mit `data-csv-value` nachschaerfen, statt alles doppelt zu bauen.

## Migrations-/SQL-Auswirkungen

Keine. Es gibt weder Schema- noch Datenaenderung noch Migration. Damit entfallen das
`SQL/XX_*.sql` mit `OBJECT_ID`-Guard, der `00_FreshInstall.sql`-Nachzug und eine Wirkung auf
`SQL/AgentJobs/*`. Das gilt fuer beide Varianten.

## Audit-Feld-Auswirkungen

Keine. Der Export liest nur. Es wird keine `AuditableEntity` geschrieben, und `ModifiedAt`,
`ModifiedBy` und `ModifiedByWindows` bleiben unberuehrt. Eine Export-Protokollierung ist nicht
vorgesehen (Rueckfrage 5). Bei Variante A erscheint der Abruf im normalen Request-Log wie ein
Seitenaufruf mit `pageSize=0`.

**Rollen und Zugriffsfilter:** Es gibt keine neue Rolle und keinen neuen Filter. Deshalb ist auch
kein Eintrag in `controller.md` oder `RoleOverview.cshtml` noetig. Der Export erbt bei Variante A
die Klassen- und Aktions-Attribute der jeweiligen Index-Action samt kumulativer Feature-Gates
(Inventar, Spalte Zugriff). Bei Variante B **muss** jede `Export`-Action dieselben Attribute tragen
wie `Index`. Dann gilt ein Pflicht-Testmuster je Controller nach Art der Attribut-Reflection-Tests.

**Listen-View-Pattern (ADR 0005):** Es entsteht keine neue Tabellenansicht, daher fallen fuer diese
Spec keine neuen Pagination-, Filterkarten-, Spaltenfilter- oder viewKey-Pflichten an. Der Export
ist eine Ergaenzung des bestehenden Musters und wird beim Umsetzen als additiver Nachtrag in ADR
0005 vermerkt: Jede neue Liste bekommt den Export automatisch bzw. per Partial.

## Akzeptanzkriterien

1. Jede Tabelle aus dem Inventar (Nr. 1-40, abzueglich der in Rueckfrage 7 ausgenommenen) zeigt
   einen Knopf „CSV“. Keine Tabelle ausserhalb des Scopes zeigt ihn.
2. Liste mit 3 Seiten à 25, Spaltenfilter gesetzt: Das CSV enthaelt **alle** Filtertreffer, nicht nur
   die angezeigte Seite. Die Zeilenzahl entspricht der Pagination-Anzeige „von N“ (bei Gruppenlisten:
   Summe der Zeilen aller Gruppen).
3. Spalte per Zahnrad ausgeblendet und **sofort** exportiert, also ohne Reload: Die Spalte fehlt im
   CSV. Spalten in anderer Reihenfolge angeordnet: Das CSV hat die Bildschirm-Reihenfolge.
4. Aktions-, Knopf- und Auswahlspalten ohne Ueberschrift erscheinen nie im CSV.
5. Das CSV oeffnet sich per Doppelklick in Excel (de-AT) mit korrekten Umlaeufen (ä ö ü ß, „–“) und
   getrennten Spalten. Mengen wie `1.234,500` werden als Zahl erkannt. Ein Datum erscheint als
   `dd.MM.yyyy` ohne KW-Suffix, sofern Rueckfrage 3 so entschieden wird.
6. Hierarchische FA-Liste (Master `true`): Das CSV ist flach. Eine Spalte „HauptFA“ steht vor den
   Zeilenspalten. Gruppen-Kopfzeilen und die Kombigeraete-Variantentabelle erscheinen **nicht** als
   Zeilen. Keine Zeile hat eine abweichende Spaltenzahl.
7. Eine Zelle mit Inhalt `=1+1`, `+43…`, `@SUMME(…)` oder `-x` beginnt im CSV mit `'` und wird in
   Excel als Text angezeigt, nicht berechnet. Die reine Zahl `-5,000` bleibt eine Zahl
   (vorbehaltlich Rueckfrage 5).
8. Ein Anwender ohne Leserecht der Ansicht, bzw. bei ausgeschaltetem Feature-Gate, erhaelt auch
   ueber den Export keine Daten. Das CSV enthaelt bei jedem Anwender genau die Zeilen, die seine
   Ansicht mit denselben Filtern zeigt.
9. Mehr als 5000 Treffer: Es erscheint ein sichtbarer Hinweis „auf 5.000 begrenzt“. Das CSV enthaelt
   die ersten 5000 Treffer, bzw. das in Rueckfrage 9 entschiedene Verhalten.
10. Ein Abruf schlaegt fehl (z. B. Sitzung abgelaufen, Serverfehler): Es erscheint eine sichtbare
    Fehlermeldung, und es wird keine Datei heruntergeladen.
11. AKE-Regression: Bei Master `false` sind alle Listen in Darstellung, Filterung, Sortierung,
    Spaltenauswahl und Druck unveraendert. Der einzige Unterschied ist der zusaetzliche Knopf.
12. Dateiname nach dem Muster `<Ansicht>_yyyy-MM-dd_HHmm.csv`, ohne Zeichen, die unter Windows im
    Dateinamen ungueltig sind.
13. Der Quelltext-Waechter `CsvExportCoverageTests` ist gruen. Er prueft: Einbindung wirksam fuer
    alle Views mit `filterable-table` ausser den Opt-outs, und `:scope >` in `csv-export.js`.
14. Build gruen, alle Web- und Service-Tests gruen.

## Test-Szenarien

Neues Kapitel **TS-&lt;naechste freie&gt; — CSV-Export fuer alle Tabellen** in `docs/TESTSZENARIEN.md`,
danach `secondbrain/tests/testszenarien-index.md` nachziehen. Gliederung:

- **.1 Grundfall AKE (Master false):** In der FA-Liste Spaltenfilter „Werkbank“ setzen,
  Seitengroesse 25, mehrere Seiten. CSV-Zeilenzahl = „von N“, Spalten = sichtbare Spalten.
- **.2 Spaltenauswahl sofort:** Spalte ausblenden und ohne Reload exportieren; Spalte nach vorn
  ziehen und exportieren (Pendant zu TS-80.6).
- **.3 IDEAL hierarchisch (Master true):** FA-Liste, Leitstand, FaCompletion, FaWorklist und
  Picking. Pruefen: Spalte HauptFA, keine Kopfzeilen, Kombigeraet-HauptFA ohne Fremdzeilen aus der
  Variantentabelle.
- **.4 IDEAL-Listen:** Kommissionierliste (Liste und Summiert), Beschichtung, Vormontage (beide
  Sichten). HauptFA-Spalte nicht doppelt.
- **.5 Excel:** Doppelklick oeffnen. Umlaute korrekt, Mengen als Zahl (Summenformel funktioniert),
  Datum als Datum.
- **.6 CSV-Injection (Negativfall):** Testdatensatz mit Bemerkung `=HYPERLINK("http://x";"klick")`
  bzw. `@SUMME(A1:A2)`. Der Wert erscheint im CSV mit `'`, und Excel fuehrt nichts aus.
- **.7 Negative Mengen:** Bestand oder Bewegung mit negativer Menge. Bleibt Zahl, entsprechend
  Rueckfrage 5. Berechnete Bestaende sind vorzeichenbehaftet (`StockMovementRepository.cs` Z. 70,
  222-223); einen Guard gegen negative Werte gibt es dort nicht.
- **.8 Rechte (Negativfall):** Anwender ohne `stock_read` ruft die Bestands-URL inkl. Export auf.
  Kein Zugriff. Ein Anwender mit Leitstand- aber ohne Picking-Recht sieht in FA-Liste und CSV
  dieselben Spalten (die Aktionsspalte ist ohnehin nicht im CSV).
- **.9 Feature-Gate aus (Negativfall):** Mit `FaHierarchyKommissionierlistenAktiv = false` gibt es
  weder Liste noch Export.
- **.10 Cap:** Filter so waehlen, dass mehr als 5000 Treffer entstehen (z. B. Bewegungshistorie ohne
  Filter). Warnung sichtbar, Inhalt entsprechend Rueckfrage 9.
- **.11 Fehlerfall:** Sitzung ablaufen lassen bzw. Server stoppen, dann exportieren. Sichtbare
  Meldung, keine Datei.
- **.12 Client-Mode-Listen:** Stueckliste mit Komm.-Ziel-Filter und teilweise zugeklapptem Baum,
  Lagerbestellung-Details, Tracking je Werkbank. Verhalten entsprechend Rueckfrage 12.
- **.13 Zellwerte:** Leitstand mit Glas, Fremdbezug und Lack als Ja/Nein, Prio als Zahl; Lack-T-Icon;
  FA-Nr. mit Badges. Ergebnis entsprechend Rueckfrage 11.
- **.14 Stammdaten-Stichprobe:** Artikel, Lagerplaetze, Benutzer (Admin) und BDE-Stammdaten je Tab.

## Etappen (nur bei epic: true)

Entfaellt bei der empfohlenen Variante A (`epic: false`). Wird in Rueckfrage 8 Variante B gewaehlt,
gilt der Etappenvorschlag dort. Die Spec wird dann im Rework auf `epic: true` gestellt, und der
Branch wird waehrend der Arbeit mit `scripts/sync-worktree.ps1` auf `main`-Stand gehalten.

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | entfaellt (Variante A) | – | – |

## Deploy

> [!warning] Vorlaeufig
> Die QA bestaetigt diesen Abschnitt am echten Diff.

- **Web-App:** ja. Neues Skript, Layout bzw. Views, `_Pagination.cshtml`, Hilfe und Changelog.
- **Service:** nein.
- **Migration:** nein.
- **Publish-Befehle** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
- **Pruefschritt vor der Abnahme (unverifizierte Umgebungsannahme):** Auf dem Testsystem in der
  FA-Liste die Spalte „Stk.“ ansehen (`_ProductionOrderRow.cshtml` Z. 66, `ToString("N3")` ohne
  Kultur).
  - Zeigt sie `1,000`, ist die Server-Kultur deutsch, und AK 5 gilt ohne Eingriff.
  - Zeigt sie `1.000` im Sinne von eins, ist die Kultur englisch. Dann liefern Views ohne feste
    Kultur Dezimalpunkte, und das muss vor der Abnahme als Rueckfrage zurueck in die Spec.
- **Browser-Cache:** Das Skript wird mit `asp-append-version="true"` eingebunden, ein Hard-Reload ist
  also nicht noetig.

## Offene Rueckfragen

1. **Welche Zeilen?** Alle Zeilen des aktuellen Filters ueber alle Seiten oder nur die sichtbare
   Seite?
   *Empfehlung (Notiz):* alle Zeilen des Filters. Technisch ueber `pageSize=0`, gekappt bei 5000
   (siehe 9).
2. **Welche Spalten?** Die beim Klick sichtbaren, in Bildschirm-Reihenfolge? Spalten ohne
   Ueberschrift (Aktionen, Knoepfe, Bulk-Checkbox) nie?
   *Empfehlung (Notiz):* sichtbare Spalten in Bildschirm-Reihenfolge, gleiche Konvention wie
   Druck/PDF in [[2026-09-25-kommissionierliste-summierung-pdf-spec]]. Dazu die Spec-Empfehlung:
   Spalten ohne Ueberschrifttext nie exportieren. Einen Server-Rueckfall wie beim Druck braucht es
   bei Variante A nicht, weil der Export immer aus der offenen Seite startet.
3. **Format fuer Excel in Oesterreich?** Semikolon, UTF-8 mit BOM, Dezimalkomma, Datum `dd.MM.yyyy`.
   Datumszellen haben heute die Form `dd.MM.yyyy KWnn` (`_ProductionOrderRow.cshtml` Z. 6-11).
   Soll der KW-Suffix im Export entfallen, damit Excel ein Datum erkennt (a)? Oder bleibt er wie
   angezeigt, dann als Text (b)? Oder zwei Spalten Datum + KW (c)? Dateiname
   `<Ansicht>_yyyy-MM-dd_HHmm.csv`?
   *Empfehlung (Notiz):* Semikolon, UTF-8-BOM, Dezimalkomma, `dd.MM.yyyy`, Dateiname mit Ansicht
   und Zeitstempel. Daraus folgt (a). Keine `sep=;`-Zeile: Sie hebelt nach bekanntem Excel-Verhalten
   die BOM-Erkennung aus. Das ist unverifiziert und wird im UAT TS-.5 geprueft. Das Dezimalkomma
   haengt teils an der Server-Kultur (Pruefschritt im Abschnitt Deploy).
4. **Gruppierte Listen?** Flach exportieren mit Gruppenschluessel als eigener Spalte? Gruppen-
   Kopfzeilen samt deren Kopfdaten (Kunde und FE-Termin in der Kopfzeile, `ProductionOrders/Index.cshtml`
   Z. 152-162) und die Kombigeraete-Variantenzeile entfallen als Zeilen?
   *Empfehlung (Notiz):* flach, Gruppenschluessel als eigene Spalte. Dazu die Spec-Empfehlung:
   „HauptFA“ aus `tbody[data-group]` nur dort ergaenzen, wo keine `hauptfa`-Spalte sichtbar ist.
   Die Kopfdaten der FA-Liste stehen ohnehin in den Zeilenspalten (Kunde, Prio, AB-Nr.,
   KO-Termin, `_ProductionOrderRow.cshtml` Z. 69-76; Kommentar `ProductionOrders/Index.cshtml`
   Z. 147-151).
5. **Sicherheit?**
   (a) Zellen, die mit `=`, `+`, `-`, `@`, Tab oder CR beginnen, erhalten ein fuehrendes `'`.
   Ausgenommen sind Werte, die vollstaendig eine Zahl im Anzeigeformat sind (z. B. `-5,000`), sonst
   werden negative Mengen zu Text.
   (b) Export nur mit dem Leserecht der jeweiligen Ansicht (bestehende `Require*Access`-Filter,
   Feature-Gates kumulativ), ohne eigenes Export-Recht und ohne eigenen Feature-Toggle.
   (c) Keine gesonderte Export-Protokollierung.
   *Empfehlung (Notiz):* CSV-Injection entschaerfen, Export nur mit dem Leserecht der Ansicht. Die
   Spec-Empfehlung ergaenzt die Zahlen-Ausnahme in (a) sowie (b) und (c) wie formuliert.
6. **Gemeinsamer Baustein:** Helper + Button-Partial je View oder ein Skript im Layout, das sich
   selbst an jede `filterable-table` haengt (Opt-out `data-csv-export="false"`)?
   *Empfehlung (Notiz):* ein gemeinsamer Baustein (Helper + Button-Partial) statt Einzelloesungen.
   *Spec-Empfehlung, Abweichung:* Skript im Layout, weil es weniger Code braucht, keine View
   vergessen werden kann und kuenftige Listen den Export automatisch haben. Das Ziel der Notiz, ein
   Baustein statt Einzelloesungen, bleibt erfuellt.
7. **Umfang:** Alle 40 `filterable-table`-Tabellen aus dem Inventar in **einer** Spec? Baumansichten
   (`FaHierarchyStructure`, Tracking hierarchisch, `OseonTracking`) und Formular-/Detailtabellen
   ausserhalb? Teil-Specs?
   *Empfehlung (Notiz):* Liste aller betroffenen Ansichten ermitteln, ist mit diesem Inventar
   erledigt. Bei grossem Umfang Teil-Specs nach Bereich: Baustein + Pilotliste zuerst, dann der
   Rest. *Spec-Empfehlung:* Bei Variante A ist **kein** Schnitt noetig, weil der Baustein alle
   Tabellen auf einmal abdeckt. Die Pilotliste (FA-Liste, AKE + IDEAL) wird zuerst im UAT geprueft.
   Baeume und Formulartabellen bleiben ausserhalb, ein Folge-Backlog bei Bedarf.
8. **Variante:** A = Erzeugung im Browser aus derselben Index-Action (kein neuer Endpunkt, ein
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
9. **Mehr als 5000 Treffer** (`PageSize.AllCap`; bei Gruppenlisten 5000 **Gruppen**, also
   potenziell deutlich mehr Zeilen)?
   *Spec-Empfehlung:* die ersten 5000 exportieren und eine sichtbare Warnung „auf 5.000 begrenzt,
   bitte Filter eingrenzen“ zeigen (analog Banner `_Pagination.cshtml` Z. 6-12). Alternative: Export
   sperren, bis der Filter eingrenzt. Die Notiz trifft dazu keine Aussage.
10. **Sortierung:** Soll der Export die aktive Bildschirm-Sortierung uebernehmen? Sie ist rein
    clientseitig und gilt je Gruppe. Uebernahme ueber den Comparator, den `table-filter.js`
    bereitstellt; Alternative ist die Serverreihenfolge.
    *Spec-Empfehlung:* uebernehmen, weil Anwender „wie am Bildschirm“ erwarten. Ohne Uebernahme gilt
    die Default-Sortierung der Action (`data-fallback-sort-column` greift nur clientseitig). Die
    Notiz trifft dazu keine Aussage.
11. **Zellinhalt:**
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
12. **Client-Mode-Listen** (Inventar 36-40): Exportiert werden die vom Client-Filter nicht
    ausgeblendeten Zeilen. Sollen in der Stueckliste auch Filtertreffer exportiert werden, die nur
    wegen eines **zugeklappten** Baumknotens unsichtbar sind (`updateBomVisibility`, [[fallstricke]]
    „BOM-Baum hat Vorrang vor dem Spaltenfilter“)?
    *Spec-Empfehlung:* ja, alle Filtertreffer unabhaengig vom Klappzustand. Der Klappzustand ist
    Navigation, kein Filter. Die Notiz trifft dazu keine Aussage.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. Welche Zeilen (alle Seiten / nur sichtbare Seite) →
2. Welche Spalten (sichtbare in Bildschirm-Reihenfolge, ohne Aktionsspalten) →
3. Format (Semikolon, UTF-8-BOM, Dezimalkomma, Datum ohne KW a/b/c, Dateiname) →
4. Gruppierte Listen (flach + HauptFA-Spalte, Kopf-/Variantenzeilen entfallen) →
5. Sicherheit (a Injection mit Zahlen-Ausnahme, b nur Leserecht ohne Toggle, c keine Protokollierung) →
6. Baustein (Skript im Layout / Button-Partial je View) →
7. Umfang (40 Tabellen in einer Spec, Baeume + Formulartabellen ausserhalb) →
8. Variante (A Browser / B Export-Action je Controller = Epic) →
9. Mehr als 5000 Treffer (erste 5000 + Warnung / sperren) →
10. Sortierung (Bildschirm-Sortierung uebernehmen / Serverreihenfolge) →
11. Zellinhalt (Badges, Checkbox Ja/Nein, Eingaben, Icons, Farbstatus) →
12. Client-Mode-Listen / Stueckliste unabhaengig vom Klappzustand →
