---
type: adr
id: 0005
title: Einheitliches Listen-View-Pattern — Pagination, Filterkarte, Spaltenfilter server-seitig
status: accepted
date: 2026-05-22
supersedes: ""
superseded_by: ""
---

> **Nachtraeglich erfasst** (2026-07-27). Bestandsdokumentation; eingefuehrt in v1.14.0,
> auf alle Tabellen ausgerollt in v1.21.0, ENTER-Semantik ergaenzt in v1.24.0.

## Kontext und Problem

Die Anwendung besteht in weiten Teilen aus Listen (FA-Liste, Bestand, Bewegungshistorie,
Bestellungen, Buchungen …). Vor v1.14.0 hatte jede View ihre eigene Loesung: harte `Take`-Caps,
teils Client-Filter auf DOM-Text, teils gar keine Filterung. Bei Listen mit mehreren Tausend
Zeilen war Client-Filtern zweifach falsch — es filtert nur die schon geladene Seite, und es
filtert gerenderten Text, was bei berechneten Spalten (Terminen) auseinanderlaeuft.

## Betrachtete Optionen

- **Client-seitig filtern und sortieren** (JS auf der Tabelle) — sofortiges Feedback, aber nur
  auf der aktuellen Seite korrekt; bei Pagination fachlich falsch.
- **Fertige Grid-Komponente** (DataTables server-side, o. ae.) — viel Funktion, aber eine
  weitere Abhaengigkeit im Frontend und schwer mit den bestehenden Spalten-Preferences und dem
  Corporate-Design zu verheiraten.
- **Eigenes, verbindliches Pattern aus Pagination + URL-getriebenem Spaltenfilter** — Aufwand
  pro Liste, aber ein Mechanismus fuer alle Listen, druck- und teilbar via URL.

## Entscheidung

Jede neue Listen-View liefert verbindlich:

- **Pagination** via `PageSize.Resolve` + `PaginationState` im Controller und dem Partial
  `../../../IdealAkeWms/Views/Shared/_Pagination.cshtml`. Groessen 25/50/100/„Alle",
  „Alle" = `PageSize.AllCap` (5000). Kein hartcodierter Take/Cap. User-Default aus
  `User.DefaultPageSize`.
- **Filterkarte** (`<div class="card filter-card mb-3">`) ueber dem Tabellenblock fuer globale
  Filter.
- **Spaltenfilter — Pflicht fuer alle Tabellen-Views.** Standard ist **Server-Mode**
  (`data-server-column-filter="true"`): Filter navigieren zu `?colf_<col-key>=value`, der
  Controller liest `ColumnFilterHelper.ReadFromQuery(...)` und mappt Col-Keys auf Properties.
  Alle `<th>` brauchen `data-col-key`.
  Client-Mode (ohne das Attribut) nur fuer kleine, unpaginierte, vorgefilterte Ansichten.
- **Datumsspalten** werden server-seitig **in C# nach** der Termin-Berechnung gefiltert
  (Format `dd.MM.yyyy KWxx`, lowercase) — nicht in SQL, weil die Termine berechnet und nicht
  gespeichert sind.
- **Filter-Mini-Syntax** identisch in Server- und Client-Mode: OR mit `,`, NOT mit `!`.
- **Layout-Konsistenz**: `<h2 class="page-header">`, TempData-Alerts, `.table-responsive`,
  Page-Header-Buttons mit `d-flex justify-content-between flex-wrap gap-2`.

Der zentrale Grundsatz des Filter-Rollouts: **der ColumnMap-Getter liefert den gerenderten
Zellentext** (Badges, Ja/Nein, Datumsformate), Filter wird **vor** der Pagination angewandt, und
`TotalCount` kommt aus der gefilterten Menge.

Referenz-Implementierungen: `../../../IdealAkeWms/Controllers/ProductionOrdersController.cs`
(mit Datumsfilter), `../../../IdealAkeWms/Controllers/StockOverviewController.cs` (einfacher Fall).

Begruendete Ausnahmen (keine echte Datenliste bzw. hierarchische Spezialdarstellung):
Home, Help, Settings, ServiceSettings, BdeCockpit, BdeShiftCalendar, BdeTerminal, BOM-Tree,
Tracking/Index.

## Konsequenzen

**Positiv**
- Filter und Seitenwahl sind in der URL — teilbar, bookmarkbar, und der Druck kann denselben
  Zustand spiegeln (`WarehousePicking/Print`).
- Grosse Listen bleiben performant; kein „stiller" Cap, der Vollstaendigkeit vortaeuscht
  (`IsCappedAtAll` triggert einen Banner-Hinweis).
- Ein Mechanismus, den Agenten und Menschen bei jeder neuen Liste kopieren koennen.

**Negativ / Risiken**
- Pro Liste ist ein Col-Key→Property-Mapping zu pflegen; neue Spalten brauchen einen Eintrag in
  `ColumnDefinitions.cs` **und** in der View.
- Bei SQL-paginierten Repos muessen Query und Count **identisch** filtern, sonst stimmt die
  Seitenzahl nicht.
- Datumsfilter erzwingen C#-Pagination (alle text-gefilterten Rows laden) — bei sehr grossen
  Mengen ein Kostenpunkt.
- Mehrere Init-Reihenfolge-Fallen im Frontend (`column-preferences.js` vor `table-filter.js`,
  eine filterbare Tabelle pro gerenderter Seite) — siehe [[fallstricke]].

## Spaltenpraeferenzen (Ergaenzung 2026-08-12)

> **Additiver Nachtrag**, ergaenzt v1.31.0 (IDEAL-Teile-1-5-Buendel, Etappe 6, Commit `3894e3d`).
> Die Entscheidung oben bleibt unveraendert; dieser Abschnitt benennt einen Pflichtbestandteil,
> den das urspruengliche Muster nur implizit voraussetzte.

Das Listen-View-Pattern hat **vier** Pflichtbestandteile, nicht drei — neben Pagination,
Filterkarte und Server-Spaltenfilter gehoert die **per-Benutzer-Spaltenauswahl** dazu:

- In der View: `column-preferences.js` einbinden (**vor** `table-filter.js`, siehe [[fallstricke]])
  plus die beiden Inline-JSON-Bloecke `#view-config` (traegt den `viewKey`) und `#column-config`
  (Spaltenkatalog mit `defaultHidden`). Ohne diese speichert/liest die Liste keine Spaltenwahl.
- Im Backend **zwingend**: der `viewKey` muss in `ColumnDefinitions.GetByViewKey` registriert sein.
  Fehlt die Registrierung, antwortet `UserViewPreferencesApiController` mit **400** und speichert
  **still nichts** — die Einstellung geht bei jedem Reload verloren, ohne Fehlermeldung fuer den
  Anwender.

**Warum der Nachtrag:** Die vier IDEAL-Listen (Teil 3-5) hatten Pagination, Filterkarte und
Server-Spaltenfilter, aber die Spaltenpraeferenzen fehlten — schlicht weil das Muster sie nie
ausdruecklich als Pflichtbestandteil benannte. Etappe 6 hat sie an alle vier Listen angeschlossen
und die `viewKey` (`FaHierarchyKommissionierListen`, `FaHierarchyBeschichtung`,
`FaHierarchyVormontageEinzeln`, `FaHierarchyVormontageSummiert`, spaeter `FaHierarchyStructure` fuer
die Baumanzeige) in `ColumnDefinitions.GetByViewKey` registriert. Details der beiden Fallstricke
(Registrierungspflicht + der bis `3894e3d` nur das erste `<tbody>` sortierende Sort-Bug in
`table-filter.js`) stehen in [[fallstricke]].

## Nachtrag 2026-10-12 — CSV-Export ([[2026-10-06-csv-export-alle-tabellen-spec]], v1.53.0)

Jede Tabelle mit `filterable-table` bekommt den CSV-Export **automatisch** über das Layout-Skript `csv-export.js`
(Opt-out `data-csv-export="false"`). Server-Mode: dieselbe Action ein zweites Mal per GET mit `pageSize=0` (gleiche Rechte,
Gates, Filter; Kappung `PageSize.AllCap` mit sichtbarer Warnung). Folgen für dieses Muster:
- Eine paginierte Liste **muss** Server-Mode sein, sonst exportierte sie nur die aktuelle Seite (Wächter
  `CsvExportCoverageTests.PaginierteTabelle_IstServerMode`).
- Eine Listen-Action darf im GET-Pfad **nichts schreiben** — der Export ruft sie ein zweites Mal auf (Prüfung 2026-10-12:
  alle 37 Actions ohne Schreibzugriff).
- Der Exportwert ist der gerenderte Zellentext (wie die ColumnMap-Regel); wo das nicht reicht, setzt die Zelle `data-csv-value`.
- Spaltenüberschriften ohne Text (Aktionen, Auswahlhaken) werden nicht exportiert.

## Nachtrag 2026-10-13 — Suchsyntax ([[2026-09-23-suchsyntax-spaltenfilter-erweitern-spec]], v1.58.0/v1.58.1)

Die Zeile „Filter-Mini-Syntax: OR mit `,`, NOT mit `!`“ oben ist **überholt**. Seit v1.58.0/v1.58.1 gilt in allen Listen dieselbe
erweiterte Syntax (`7%`, `%7`, `=x`, `a...b`, `>`/`<`/`>=`/`<=`, `(leer)`, `!` für alle Teile, ODER mit `;`, in Textspalten auch `,`),
ausgewertet von **einem** Kern — siehe [[0017-ein-parser-fuer-die-spaltenfilter-syntax]]. Für dieses Muster heißt das zusätzlich:
Zahl- und Datumsspalten tragen ihren Typ am `<th>` (`data-filter-type="number"` bzw. `data-date-filter`) **und** im Server-Register
`ColumnFilterTypes` (Schlüssel = View-Pfad); `ColumnFilterHelper.Apply` bekommt das Register der Liste. Getter typisierter Spalten
rendern wie die Zelle. Ein Drift-Wächter (`ColumnFilterTypesDriftTests`) hält `<th>` und Register gleich.
