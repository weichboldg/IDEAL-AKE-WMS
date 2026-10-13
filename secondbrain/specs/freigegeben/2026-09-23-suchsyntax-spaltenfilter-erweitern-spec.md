---
type: spec
title: "Spaltenfilter: Suchsyntax erweitern (beginnt/endet/genau/von-bis/Vergleich/leer) - EIN Parser fuer alle Listen"
slug: 2026-09-23-suchsyntax-spaltenfilter-erweitern-spec
status: InUmsetzung
created: 2026-10-13
updated: 2026-10-13
source_backlog: "[[2026-09-23-suchsyntax-spaltenfilter-erweitern]]"
task: "[[2026-10-13-suchsyntax-spaltenfilter-erweitern-umsetzung]]"
worktree: ""
branch: ""
affected_code:
  - "IdealAkeWms/Services/ColumnFilterHelper.cs - Parse (45-60) liefert kuenftig strukturierte Tokens; Apply (71-93) wird der Speicher-Auswerter; NEU: SQL-Ausdrucksbauer (Expression-Trees, kein EF.Functions.Like) - Etappe A"
  - "IdealAkeWms/wwwroot/js/column-filter-syntax.js (NEU) - JS-Spiegel des Parsers/Auswerters, UMD-Waechter wie csv-export.js:47, eingebunden in Views/Shared/_Layout.cshtml vor RenderSection (418) - Etappe A"
  - "IdealAkeWms/wwwroot/js/table-filter.js - matchesFilter (272-279) ersetzt durch Kern-Aufruf mit Spaltentyp; Typ aus <th data-filter-type> bzw. data-date-filter (160); Syntax-Hilfe-Knopf + aria-describedby an jedem Filterfeld (Aufbau 149-225) - Etappe A"
  - "IdealAkeWms.Tests/Js/column-filter-cases.json (NEU) - EINE gemeinsame Testtabelle fuer C# und JS - Etappe A"
  - "IdealAkeWms.Tests/Js/column-filter-syntax.check.js (NEU) - Node-Lauf der Tabelle, Muster csv-export.check.js - Etappe A"
  - "IdealAkeWms.Tests/Services/ColumnFilterHelperTests.cs (NEU) - Tabelle gegen C#-Auswerter + SQL-Bauer (ToQueryString gegen nie geoeffneten SqlServer-Context) + SkippableFact Node-Lauf - Etappe A"
  - "IdealAkeWms/Data/Repositories/ProductionOrderRepository.cs - ApplyLeitstandColumnFilter (271-342), BuildCustomerColumnFilterPredicate/BuildCustomerTokenPredicate (364-390), BuildExtraInfoOrContains (425-461) auf den zentralen Bauer; Freitext 104-136 nur nach Antwort 8 - Etappe A (FA-Liste; wirkt zugleich auf den Leitstand-SQL-Teil)"
  - "IdealAkeWms/Controllers/ProductionOrdersController.cs - MatchFaListDateFilter (506-521), MatchFaListComputedFilter (526-543), Parse-Aufrufe 349/358/432/440; Typ-Registrierung der Datumsspalten (FaListDateColumnKeys 483-486) + Zahl prio - Etappe A"
  - "IdealAkeWms/Views/Help/Index.cshtml - Abschnitt Filter-Mini-Syntax (203-212) neu - Etappe A"
  - "IdealAkeWms/Controllers/PickingLeitstandController.cs - MatchLeitstandWorkStepFilter/TextFilter/DateFilter (604-650), Parse 227/305 - Etappe B"
  - "IdealAkeWms/Data/Repositories/ArticleRepository.cs - ApplyArticleColumnFilter (93-119), Parse 67 - Etappe B"
  - "IdealAkeWms/Data/Repositories/StockMovementRepository.cs - ApplyMovementColumnFilter (559-601), ApplyMovementTypeFilter (612-628), ApplyMovementDateFilter (630-675), Parse 346 - Etappe B"
  - "IdealAkeWms/Data/Repositories/BdeBookingRepository.cs - ApplyHistoryColumnFilters (118-166), BuildOrContains (180-198) - Etappe B"
  - "IdealAkeWms/Controllers/BdeBookingsController.cs - MatchComputedColumnFilter (317-330), Parse 99 - Etappe B"
  - "IdealAkeWms/Data/Repositories/WarehouseRequisitionRepository.cs - ApplyMissingPartsTextFilter (451-509), ApplyMissingPartsNullableTextFilter (511-526) - abweichende Per-Token-Negation, Antwort 5 - Etappe B"
  - "IdealAkeWms/Data/Repositories/OseonProductionOrderRepository.cs - ApplyOseonColumnFilter (165-194), Parse 88 - umstellen oder loeschen nach Antwort 10 - Etappe B"
  - "IdealAkeWms/Views/Picking/Bom.cshtml - bomMatchesFilter (842-853) + Aufrufe 901/1144/1169, isAlleZiele (839) auf den Kern; quantity-<th> (156) typisieren - Etappe B"
  - "IdealAkeWms/wwwroot/js/fa-hierarchy-tree.js - rowMatchesAll (290-297) Textzweig auf den Kern (Zeilen nach Teil-3-Lauf neu messen) - Etappe B"
  - "IdealAkeWms/Services/ColumnFilterTypes.cs (NEU, Vorschlag) - Zahl-/Datumsspalten je Liste, Server-Seite; Drift-Waechter gegen <th>-Markierung - Etappe A (Geruest + FA-Liste), Etappe B (alle Listen)"
  - "Views mit Zahl-/Datumsspalten (ca. 38 Zahl- + ca. 10 bisher unmarkierte Datumsspalten in ca. 25 Views) - <th data-filter-type> - Etappe B"
  - "IdealAkeWms/Controllers/FaWorklistController.cs - quantity-Getter (416) rendert InvariantCulture statt wie die Zelle (_FaWorklistRow.cshtml:58) - Etappe B"
  - "docs/TESTSZENARIEN.md - neues Kapitel (naechste freie TS-Nummer zum Umsetzungszeitpunkt, Messstand: hoechste TS-89)"
  - "secondbrain/tests/testszenarien-index.md"
open_questions: []
epic: true
etappen:
  - "A: Zentraler Parser (strukturierte Tokens) + SQL-Ausdrucksbauer + Speicher-Auswerter (Apply) + JS-Spiegel (column-filter-syntax.js) + gemeinsame Testtabelle (JSON, C# + Node) + Syntax-Hilfe in table-filter.js + Hilfeseite; Referenzliste FA-Liste (ApplyLeitstandColumnFilter + C#-Postfilter) umgestellt. Eigener Commit. STOPP - Mensch prueft am Testsystem."
  - "B: Alle uebrigen Stellen: Leitstand-Postfilter, Artikel, Bewegungshistorie, BDE-Buchungen (Repo + Controller), Fehlteile, OSEON (nach Antwort 10), Stueckliste (bomMatchesFilter), FA-Baum (rowMatchesAll); Zahl-/Datums-Typmarkierung in allen Listen + Drift-Waechter; alte Parse-Signatur entfernt; TS-Kapitel, Brain-Pflichten. Eigener Commit, danach QA fuer das Epic."
deploy:
  web: true
  service: false
  migration: false
freigabe_entscheidung: "Freigegeben wie Antworten 1-10 und Entscheidungen der Backlog-Notiz: Epic, Etappe A (zentraler Parser, SQL-Bauer, Speicher-Auswerter, JS-Spiegel, gemeinsame Testtabelle, Syntax-Hilfe, Referenz FA-Liste) mit STOPP zur Sichtpruefung, dann Etappe B. Von-bis nur Zahl/Datum, Komma/Semikolon-Regel, P-1 auf beiden Servern. Direkt im Buendel."
freigabe_von: "Gerald Weichbold"
freigabe_am: 2026-10-13
# Flache Schluessel mit Absicht: Obsidians Property-Editor kann verschachtelte
# YAML-Objekte NICHT bearbeiten - und genau diesen Block fuellt der Mensch aus.
---

> [!info] Umsetzungsort und Messstand
> **Direkt im Bündel** `feature/2026-08-07-ideal-teile-1-5` (Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`),
> wie alle Läufe (Entscheidung Gerald 2026-10-13, Punkt 4). Version und TS-Nummer: **nächste freie im Bündel zum
> Umsetzungszeitpunkt**.
> **Zeilenangaben Stand HEAD `d3746373` (Branch-Ref, 2026-10-13), `AppVersion` 1.56.0 vom 2026-10-13, letzter
> Changelog [[2026-10-13-v1-56-0-menue-beschichtung-hilfe]], höchstes Kapitel TS-89 (`docs/TESTSZENARIEN.md:9725`).
> Parallel läuft der Dev-Lauf [[2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-3-spec]] (u. a.
> `wwwroot/js/fa-hierarchy-tree.js`). Nach Ende des Teil-3-Laufs neu messen (Pflicht vor Etappe A).**
> Der Branch wird während der Arbeit vom Menschen über `scripts/sync-worktree.ps1` aktuell gehalten.

## Ziel / Nutzen (das Warum)

Die Spaltenfilter kennen heute nur „enthält", `,` (ODER) und `!` (NICHT). Kurze Codes sind damit
mehrdeutig (`KA` trifft `KA2`), Zahlen- und Terminbereiche sind nicht filterbar, leere Felder nicht
auffindbar. Gewünscht sind `7%`, `%7`, `a...b`, dazu `=`, `>`/`<`/`>=`/`<=`, `(leer)`/`!(leer)` und eine
Syntax-Hilfe am Feld (Entscheidung 1).

Der eigentliche Aufwand ist nicht die Syntax, sondern dass sie heute **an mindestens sechs Stellen mit
eigener Bedeutung** umgesetzt ist (Erhebung unten). Erweitert man nur eine, verhalten sich Listen je
nach Filterweg verschieden. Ziel ist deshalb **ein Parser, eine Bedeutung**: ein C#-Kern (Speicher-Auswerter
+ SQL-Ausdrucksbauer), ein JS-Spiegel, belegt durch **eine** gemeinsame Testtabelle. Nebenbei verschwindet
ein vorbestehender Fehler: Eingaben gehen heute unmaskiert in `LIKE` (P2).

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Neue Syntax laut Entscheidung 1–3 (Grammatik unten), in **allen** Listen mit Spaltenfilter (Server-Mode,
  Client-Mode, Stückliste, FA-Baum-Spaltenfilter).
- Escaping aller Benutzereingaben für SQL (P2).
- Spaltentyp Text/Zahl/Datum je filterbarer Spalte, Client und Server.
- Syntax-Hilfe an jeder Filterzeile, Hilfeseite.
- Prüfabfrage der gespeicherten Standardfilter (P4).
- Behebung der in der Erhebung gefundenen stillen Abweichungen, soweit freigegeben (Antworten 5, 8, 9, 10).

**Out-of-Scope**
- Relative Datumsangaben (`heute`, `KW40...KW42`) – Backlog „eher nicht jetzt". `KW42` als „enthält" bleibt.
- `*` als Platzhalter (Antwort 1, Empfehlung nein).
- Freitextsuchen, die keine Spaltenfilter sind: `#faClientFilter` (`fa-hierarchy-tree.js:261-279`, hebt nur
  hervor), `FaHierarchyController.MatchesFreeText` (246), Artikel-Typeahead, Filterkarten-Freitexte
  (Ausnahme: nur Escaping, Antwort 8).
- `WorkbenchFilter` / `User.DefaultWorkbenches` – eigene Komma-Liste, keine Spaltenfilter-Syntax.
- Der tote Auto-Expand-Zweig `fa-liste-gruppierung.js:77-87` (kein `[data-fa-liste-filter]` in den Views,
  bekannter Befund F2) – bleibt, wie er ist.
- Zwei filterbare Tabellen je Seite (Fallstrick §22) – unverändert.

## Fachliche Anforderungen

### Grammatik (verbindlich für C# und JS)

```
Filterwert   := ['!'] Teil { Trenner Teil }
Trenner      := ';'                      (überall)
              | ','                      (nur in Textspalten; in Zahl-/Datumsspalten ist ',' Dezimalkomma)
Teil         := '(leer)'
              | '=' Wert                 genau gleich
              | ('>=' | '<=' | '>' | '<') Wert     nur Zahl/Datum
              | Wert '...' Wert          von-bis, beide Grenzen inklusive, nur Zahl/Datum
              | '%' Wert                 endet mit
              | Wert '%'                 beginnt mit
              | '%' Wert '%' | Wert      enthält
```

Regeln:
1. Groß/Klein egal; Teile und Werte werden getrimmt.
2. `!` steht nur am Anfang und gilt für **alle** Teile: `!a;b` = weder a noch b (wie heute,
   `ColumnFilterHelper.cs:42-43`, `table-filter.js:273-275`). `!(leer)` = nicht leer.
3. `%` hat nur am Anfang/Ende Bedeutung; im Inneren ist es ein gewöhnliches Zeichen. `_` und `[` sind immer
   gewöhnliche Zeichen (P2). Ein Teil nur aus `%` ist wirkungslos.
4. Der erste Operator gewinnt: `=7%` heißt „genau `7%`".
5. „enthält", `%` und `=` wirken in **allen** Spaltentypen auf den **angezeigten Zelltext** (ADR 0005:
   Getter liefert gerenderten Text). Dadurch bleibt `kw42` / `10.2026` in Datumsspalten gültig.
6. Vergleiche und Bereiche wirken auf den **Wert**: Zahl im deutschen Format (`.` Tausender, `,` Dezimal –
   wie `tableFilterCompareValues`, `table-filter.js:350`; Fallstricke „Punkt ist Tausendertrenner"), Datum
   `dd.MM.yyyy` (auch `d.M.yyyy`, `dd.MM.yy` wie `StockMovementRepository.cs:642`). Der Zellwert wird aus dem
   angezeigten Text gelesen (führendes Datum bzw. Zahl).
7. Bereich mit vertauschten Grenzen (`70000...40000`) gilt als `40000...70000`.
8. `(leer)` = angezeigter Zelltext leer oder nur Leerzeichen. Mehrspaltige Schlüssel (z. B. FA-Nr. =
   `OrderNumber` ODER `SubOrderNumber`): ein Teil trifft, wenn er **eine** der Spalten trifft; `(leer)`
   trifft, wenn **alle** leer sind.
9. Ungültiges (Bereich/Vergleich auf Text, unlesbare Zahl/Datum): siehe Antwort 6.
10. Zeilen ohne Wert (leere Zelle) treffen bei `!x` (wie heute `ProductionOrderRepository.cs:419-421`).

Folgen, die in die Hilfe gehören: in Zahl-/Datumsspalten ist `kw41,kw42` künftig **kein** ODER mehr
(→ `kw41;kw42`); in Textspalten ist ein `;` nicht mehr suchbar.

### Erhebung aller Filterwege (Stand `d3746373`)

| # | Fundstelle (Datei:Zeile) | Was | Weg | Syntax heute | Etappe |
|---|---|---|---|---|---|
| Z1 | `Services/ColumnFilterHelper.cs:45-60` `Parse` | Parser für alle Server-Wege | Server | `,` ODER, `!` gesamt, lowercase | A |
| Z2 | `Services/ColumnFilterHelper.cs:71-93` `Apply` | Speicher-Filter, 30 Aufrufstellen (Liste unten) | Server, C# | enthält | A (zentral) |
| Z3 | `wwwroot/js/table-filter.js:272-279` `matchesFilter` | alle Client-Mode-Tabellen | Client | `,` / `!`, enthält | A (zentral) |
| S1 | `Data/Repositories/ProductionOrderRepository.cs:271-342` `ApplyLeitstandColumnFilter` (+ 364-390 Kunde-Subquery, 425-461 `BuildExtraInfoOrContains`) | FA-Liste **und** Leitstand, 12 Keys | Server, SQL | `EF.Functions.Like` mit `$"%{t}%"` (276) bzw. `ToLower().Contains` | A |
| S1f | `ProductionOrderRepository.cs:104-136` | Filterkarten-Freitext FA-Nr./Artikel/Kunde | Server, SQL | `Like($"%{x}%")` ohne Syntax | A nach Antwort 8 |
| M1 | `Controllers/ProductionOrdersController.cs:506-521`, `526-543`, `366-372` | FA-Liste: Datumsspalten, K1-Kopfdaten + Matchcode, Kunde-Freitext | Server, C# | enthält auf `dd.MM.yyyy kwNN` | A |
| M2 | `Controllers/PickingLeitstandController.cs:604-650` | Leitstand: VK–VA, Matchcode, Datumsspalten | Server, C# | enthält | B |
| S2 | `Data/Repositories/ArticleRepository.cs:93-119` | Artikelliste, 6 Keys | Server, SQL | `Like` `%t%` (96) | B |
| S3 | `Data/Repositories/OseonProductionOrderRepository.cs:165-194` | OSEON-Teileverfolgung, 4 Keys | Server, SQL | `Like` `%t%` (168) – **nur per URL erreichbar**, `Views/Tracking/OseonIndex.cshtml:103-122` ohne `data-filterable` | B / Antwort 10 |
| S4 | `Data/Repositories/StockMovementRepository.cs:559-601`, `612-628`, `630-675` | Bewegungshistorie: Text-Keys, Bewegungsart (Enum-Name), Datum/Zeit (Tag/Monat/Jahr als Zeitraum) | Server, SQL | `Like` `%t%` (562); Datum eigene Logik | B |
| S5 | `Data/Repositories/BdeBookingRepository.cs:118-198` | BDE-Buchungen: Operator, Werkbank, Ziel, Typ/Status (Enum) | Server, SQL | `ToLower().Contains` | B |
| M3 | `Controllers/BdeBookingsController.cs:317-330` | BDE-Buchungen: Start/Ende (`dd.MM.yyyy HH:mm kwNN`), Gut/Ausschuss | Server, C# | enthält | B |
| S6 | `Data/Repositories/WarehouseRequisitionRepository.cs:451-526` | Fehlteile + Fehlteile-Lager, 5 Keys | Server, SQL | `Contains`; **`!` je Wert** (460-461, 518-519) – abweichend | B / Antwort 5 |
| J2 | `Views/Picking/Bom.cshtml:842-853` `bomMatchesFilter` (+ 901, 1144, 1169; `isAlleZiele` 839) | Stückliste (überschreibt `table-filter.js`), Sichtbarkeit, Vorfahren-Aufklappen, Leerzustand, `data-csv-filter-match` (909) | Client | `,`/`!` + Sonderwert `!(leer)` | B |
| J3 | `wwwroot/js/fa-hierarchy-tree.js:282-297` `activeFilters`/`rowMatchesAll` | FA-Struktur-Baum-Spaltenfilter | Client | **keine** Syntax: reines enthält; `<select>` exakt | B |

**Apply-Aufrufstellen (Z2, 30):** `ArticleAttributesController.cs:50`, `ArticleCategoriesController.cs:48`,
`BdeMasterDataController.cs:84/87/90`, `BeschichtungsauftraegeController.cs:84`, `FaAttributesController.cs:68`,
`FaCompletionController.cs:157`, `WorkStepsController.cs:49`, `WorkstationsController.cs:53`,
`PickingController.cs:196`, `WarehouseRequisitionsController.cs:87`, `WarehousePickingController.cs:79` und `:386`
(Druck einer Client-Mode-Liste – P5-Fall), `UsersController.cs:82`, `PartRequisitionsController.cs:85`,
`TrackingController.cs:118`, `SyncLogController.cs:39`, `OrderRecipientsController.cs:48`,
`StorageLocationsController.cs:62`, `StockOverviewController.cs:69`, `SageBookingQueueController.cs:75`,
`RolesController.cs:58`, `ProductionWorkplacesController.cs:71`, `OperationConfigsController.cs:52`,
`FaWorklistController.cs:291`, `FaHierarchyListBuilder.cs:137` (Kommissionierlisten, Beschichtungsauftrag,
Vormontage – je Einzelansicht), `BeschichtungsauftragService.cs:218`, `KommissionierListenService.cs:192`,
`VormontageService.cs:259`.

**Client-Mode-Tabellen (Z3, ohne `data-server-column-filter`):** `Picking/Bom.cshtml:144` (über J2),
`WarehousePicking/Details.cshtml:60`, `Beschichtungsauftraege/Details.cshtml:211`, `Tracking/ByWorkplace.cshtml:55`,
`OrderRecipients/ArticleGroupMappings.cshtml:28`, `OseonReporting/_OseonReportingTable.cshtml:4` (bei den letzten
beiden ist die Einbindung von `table-filter.js` im Dev-Lauf zu prüfen – in der Einbindungsliste nicht gefunden).

**Nicht betroffen (geprüft):** CSV-Export filtert selbst nicht – Client-Mode liest Sichtbarkeit bzw.
`data-csv-filter-match` (`csv-export.js:112-114`), Server-Mode ruft dieselbe URL mit `pageSize=0`
(`csv-export.js:3-4`); der ausgelagerte **Sortiervergleich** `compareValues` liegt in `table-filter.js:335-360`
(`window.tableFilterCompareValues`) und filtert nicht. Druck/PDF der IDEAL-Listen laufen über dieselben Services
(Z2). `UserViewPreference.SettingsJson` speichert nur Spalten und Sortierung (`column-preferences.js:12`),
keine Filterwerte. sessionStorage `tableFilters:<viewKey>` (`table-filter.js:30`) ist sitzungsflüchtig.

**Zahl der umzustellenden Stellen**

| | Server | Client |
|---|---|---|
| **Etappe A** | 2 zentral (Z1 Parser, Z2 Apply) + neuer SQL-Bauer; FA-Liste: S1 (3 Bauweisen in einer Funktion) + M1 (2 Funktionen) = **3 Listen-Stellen** (+ S1f nach Antwort 8). Wirkung auf 30 Apply-Aufrufstellen ohne deren Änderung (Antwort 4). | 1 zentral (Z3) + neuer JS-Kern; wirkt auf alle Client-Mode-Tabellen außer Stückliste |
| **Etappe B** | SQL: **5 Repository-Stellen / 8 Funktionen** (S2, S3, S4×3, S5, S6×2); C#-Postfilter: **2 Stellen / 4 Funktionen** (M2×3, M3); Typ-Registrierung für die Apply-Listen mit Zahl-/Datumsspalten (ca. 25 Listen); 13 Aufrufe der alten `Parse`-Signatur (Grep `ColumnFilterHelper.Parse`) | **2 Stellen** (J2, J3) + Typmarkierung `<th>` in ca. 25 Views |

### Spaltentyp-Befund

- `ColumnDef` kennt **keinen** Typ: `record ColumnDef(string Key, string Label, bool Locked, int? DefaultWidth, bool DefaultHidden)`
  (`Models/ViewModels/ColumnDefinitions.cs:3`). Zudem sind nur 18 Ansichten in `GetByViewKey` registriert
  (`ColumnDefinitions.cs:541-562`), gefilterte Tabellen gibt es in rund 40 Views – ein Feld am `ColumnDef`
  erreicht also nicht alle Listen.
- Das `#column-config`-JSON trägt ebenfalls keinen Typ (Beispiel `FaWorklist/Index.cshtml:192`:
  `key/label/locked/defaultWidth`).
- **Datum** kennt heute nur der Client über `<th data-date-filter>` (`table-filter.js:160`, Kalenderknopf): 19 Spalten
  in 6 Views (FA-Liste, Leitstand, FA-Abarbeitungsliste, FA-Vervollständigung, Kommissionierliste, BDE-Buchungen).
  Der Server hält Datums-Keys je Liste in Ad-hoc-Mengen (`ProductionOrdersController.cs:483-486`,
  `BdeBookingsController.cs:297-303`, `PickingLeitstandController.cs:567`). **Unmarkiert** sind u. a.
  Beschichtungsaufträge `start`/`retour`/`angelegtam` (`Beschichtungsauftraege/Index.cshtml:52/53/57`),
  `SageBookingQueue created-at` (60), Bewegungshistorie `datetime` (83), Protokoll `Timestamp` (41),
  Bestellungen `created` (85), Benutzer und Lagerplätze `created-at` – rund **10**.
- **Zahl** ist nirgends gekennzeichnet. Grep über `<th data-filterable>` mit Mengen-/Maß-/Zähl-Keys ergibt rund
  **38** Spalten (u. a. `sollmenge`, `fertigungmenge`, `breite`/`hoehe`/`tiefe`, `positionen`, `quantity`, `menge`,
  `good-qty`/`scrap-qty`, `prio`/`priority`, `current-stock`, `pre-picking-days`, `article-count`, `user-count`,
  `spec-count`, `sequence`).
- **Vorschlag:** Client `<th data-filter-type="number">` (Datum weiter über `data-date-filter`, das auch den Kalender
  steuert); Server ein Register `ColumnFilterTypes` je Liste (Zahl- und Datums-Keys, löst die Ad-hoc-Mengen ab),
  Muster wie `FaListDateColumnKeys`. **Drift-Wächter** (Quelltext-Test wie `CsvExportCoverageTests`): je
  Server-Mode-View stimmen die markierten `<th>`-Keys mit dem Register überein – sonst filtern Client-Hinweis und
  Server verschieden, ohne Meldung.
- Folge der Regel 6 (Zellwert aus angezeigtem Text): Zahl-Getter müssen wie die Zelle rendern.
  **Abweichung gefunden:** `FaWorklistController.cs:416` liefert `Quantity.ToString(InvariantCulture)`, die Zelle
  rendert `@item.Quantity` mit Server-Kultur (`_FaWorklistRow.cshtml:58`) – in B angleichen.

## Technischer Lösungsentwurf

Muster: Listen-View-Pattern ([[0005-listen-view-pattern-mit-server-side-spaltenfilter]]), Repository-Zugriff
unverändert, Node-Selbstprüfung wie CSV-Export ([[2026-10-06-csv-export-alle-tabellen-spec]]).

**C#-Kern (`ColumnFilterHelper`)**
- `Parse(raw, type)` → strukturiertes Ergebnis: `Negate`, Liste von Teilen (`Art` ∈ {Enthaelt, Beginnt, Endet,
  Genau, Vergleich(op), Bereich, Leer}, Werte), `Ungueltig` (Teile + Grund). Die alte Signatur
  `(List<string>, bool)` bleibt bis Etappe B für die noch nicht umgestellten Aufrufer stehen und wird dort entfernt.
- **Speicher-Auswerter** `Matches(string? zelltext, parsed, type)`; `Apply` bekommt die Typen der Liste (optional,
  Standard Text) und nutzt ihn. ColumnMap-Signatur `Func<T,string?>` bleibt.
- **SQL-Ausdrucksbauer**: liefert `Expression<Func<T,bool>>` aus einem oder mehreren String-Selektoren; baut
  `string.Contains/StartsWith/EndsWith`/`==`-Aufrufe mit Konstanten als Ausdrucksbaum, OR-Ketten je Teil als
  `OrElse` (kein `tokens.Any(...)`-Lambda, kein `EF.Functions.Like` – beides scheitert unter EF-InMemory,
  [[fallstricke]] §12). EF maskiert Platzhalter in solchen Mustern selbst – **im Test per `ToQueryString()` zu
  belegen** (AK 4), nicht vorauszusetzen. Für die Kunde-Subquery (S1, 364-390) liefert der Bauer das
  Token-Prädikat `Expression<Func<string?,bool>>`, das über den vorhandenen `ParameterRebinder`
  (`ProductionOrderRepository.cs:401-413`) eingesetzt wird. Groß/Klein in SQL: Kollation (Prüfschritt P-2), kein
  `ToLower()` auf Spalten (sonst kein Indexzugriff für „beginnt mit", P6).
- **Datum in SQL** braucht nur die Bewegungshistorie (`Timestamp`, S4); Zahl in SQL kommt heute **nicht** vor (alle
  Zahlspalten werden in C# gefiltert) – der Bauer bekommt deshalb keinen Zahlenzweig (YAGNI).

**JS-Spiegel** `wwwroot/js/column-filter-syntax.js`: reiner Kern (keine DOM-Zugriffe), `window.ColumnFilterSyntax`
im Browser, `module.exports` unter Node (Wächter wie `csv-export.js:47`); in `_Layout.cshtml` vor
`RenderSectionAsync("Scripts")` (418) eingebunden, damit `table-filter.js`, `Bom.cshtml` und `fa-hierarchy-tree.js`
ihn ohne 41 Einzel-Einbindungen nutzen. `table-filter.js` liest den Typ aus dem `<th>`.

**Gemeinsame Testtabelle** `IdealAkeWms.Tests/Js/column-filter-cases.json`, eine Zeile je Fall:
`{ "id", "type": "text|number|date", "filter", "cell", "match": true|false, "invalid": false, "sql": true }`.
- C#: xUnit-Theory liest die Datei über den Repo-Pfad (wie `CsvExportCoverageTests.cs:141`) und prüft den
  Speicher-Auswerter; Fälle mit `"sql": true` zusätzlich gegen den SQL-Bauer, kompiliert über LINQ-to-Objects
  (nur Fälle ohne Groß/Klein-Unterschied, da C# ordinal vergleicht).
- JS: `IdealAkeWms.Tests/Js/column-filter-syntax.check.js` liest dieselbe Datei; Aufruf als `SkippableFact` mit
  `node` wie `CsvExportCoverageTests.Kern_NodeSelbstpruefung` (138-162). **Ein JS-Test-Runner existiert im Repo
  nicht** (kein `package.json`); der Node-Skript-Weg ist bereits eingeführt und reicht.
- Pflichtfälle mindestens: jeder Operator × Typ, Negation, `;`/`,` je Typ, Escaping `A_1`/`50%`/`[x]`,
  `(leer)`/`!(leer)`, vertauschter Bereich, Datumsformat `dd.MM.yyyy kwNN` und `dd.MM.yyyy HH:mm kwNN`, Zahl
  `1.234,5`, ungültige Eingaben, mehrere Teile.

**Syntax-Hilfe** (frontend-design-Skill im Dev-Lauf Pflicht): `table-filter.js` setzt beim Aufbau der Filterzeile
einen kleinen Knopf „?" (`btn btn-sm btn-outline-secondary`, `aria-label="Hilfe zur Filtersyntax"`) in die erste
Filterzelle und öffnet ein Bootstrap-Popover (im Bundle vorhanden, `_Layout.cshtml:413`; Tooltips sind bereits in
Gebrauch, `PartRequisitions/Index.cshtml:230-232`) mit `trigger: 'focus'`/Klick, per Tastatur erreichbar,
Escape schließt. Jedes Filterfeld bekommt `aria-describedby` auf einen einmaligen, visuell versteckten
Kurztext. Nicht zwischen Zahnrad-Host und `.table-responsive` einfügen ([[fallstricke]] §19). Dieselbe
Hilfe im FA-Baum und in der Stückliste. Kurztext-Vorschlag:

> `7%` beginnt mit · `%7` endet mit · `=KA` genau · `;` oder · `!` nicht · `(leer)` leer ·
> Zahl/Datum: `>5`, `<=01.10.2026`, `40000...70000` · Text: `,` = oder

Ungültige Teile (Antwort 6): Feld `is-invalid` + `invalid-feedback`-Text (z. B. „Bereich nur in Zahl- und
Datumsspalten"), Farbe nicht alleiniger Träger (Text), WCAG AA. Im Server-Mode validiert `table-filter.js` beim
Wiederherstellen aus der URL (`restoreFiltersFromUrl`, 41-52), so ist auch ein Standardfilter oder geteilter Link
markiert.

**Laufzeit (P6)**: `Articles` hat 108.818 Zeilen (gemessen 2026-09-13, [[2026-09-13-matchcode-artikelstamm-spec]]
Z. 164), Indizes: UQ auf `ArticleNumber`, keiner auf `Description` (Z. 170-172). Der Spaltenfilter der Artikelliste
ist heute schon `LIKE '%x%'` (`ArticleRepository.cs:96`) – Full Scan. „beginnt mit" auf `article-number` kann den UQ-Index
nutzen, „endet mit"/„enthält" nicht – keine Verschlechterung. **Abweichung zur Notiz:** Die Regeln „ab 3 Zeichen +
Entprellung" gelten für den Artikel-Typeahead ([[2026-09-15-v1-40-0-matchcode-artikelstamm]] Z. 41-42), **nicht** für
den Spaltenfilter – der filtert erst bei ENTER (`table-filter.js:183/215`). Festlegung statt Rückfrage: AK 12.

**Betroffene Module:** [[controller]], [[services]], [[module]] (Listen), Stückliste, FA-Struktur.

## Migrations-/SQL-Auswirkungen

- **Keine Schemaänderung**, keine EF-Migration, kein `SQL/XX_*.sql`, `00_FreshInstall.sql` unberührt,
  `SQL/AgentJobs/*` unberührt (kein Pflichtfeld, kein MERGE betroffen).
- **Keine automatische Datenkorrektur** gespeicherter Standardfilter (Antwort 7). Vorher Prüfabfrage P-1.
- Gespeicherte Standardfilter liegen in `dbo.Users` (`ApplicationDbContext.cs:78`), fünf Spalten
  (`Models/User.cs:31/35/84/91/98`). Alle fünf zielen auf **Textspalten**: Beschaffung, Artikelgruppe,
  Bezeichnung 1, Komm.-Ziel der Stückliste (`Bom.cshtml:1207-1213`) und Bezeichnung 1 der FA-Abarbeitungsliste
  (`FaWorklistController.cs:145`). „`,` in Zahlspalten" kann dort strukturell nicht vorkommen. **Korrektur zur
  Notiz (P4):** Alle fünf werden heute in JS (Stückliste) oder im Speicher (`Apply`, FA-Abarbeitungsliste) ausgewertet –
  dort war `%` nie Platzhalter, sondern ein Zeichen. Ein gespeichertes `7%` bedeutet heute „enthält ‚7%'" (trifft
  praktisch nichts), danach „beginnt mit 7".

**Prüfabfrage P-1 (Gerald, Produktions-DB `IDEAL_AKE_WMS`, nur lesen):**

```sql
SELECT u.Id, u.Name, v.Spalte, v.Wert
FROM dbo.Users u
CROSS APPLY (VALUES
    ('DefaultFilterBeschaffung',             u.DefaultFilterBeschaffung),
    ('DefaultFilterArtikelgruppe',           u.DefaultFilterArtikelgruppe),
    ('DefaultFilterFaWorklistDescription1',  u.DefaultFilterFaWorklistDescription1),
    ('DefaultFilterBomDescription1',         u.DefaultFilterBomDescription1),
    ('DefaultFilterBomKommissionierziel',    u.DefaultFilterBomKommissionierziel)
) v(Spalte, Wert)
WHERE v.Wert IS NOT NULL AND (
       v.Wert LIKE '%[%]%'                                   -- % : kuenftig beginnt/endet
    OR v.Wert LIKE '%;%'                                     -- ; : kuenftig ODER
    OR v.Wert LIKE '%...%'                                   -- Bereich (in Textspalten ungueltig)
    OR REPLACE(v.Wert, ' ', '') LIKE '[!=<>]%'               -- Operator am Anfang (! ist ok, nur zur Sicht)
    OR REPLACE(v.Wert, ' ', '') LIKE '%[,;][=<>]%'           -- Operator am Anfang eines weiteren Teils
    OR (v.Wert LIKE '%(leer)%' AND v.Wert <> '!(leer)')      -- (leer) ausser dem normalisierten Komm.-Ziel
);
```

`_` wird bewusst nicht abgefragt: in JS/Speicher war es nie Platzhalter, also ändert sich dort nichts.

**Prüfabfrage P-2 (Kollation, Groß/Klein in SQL):**
`SELECT DATABASEPROPERTYEX('IDEAL_AKE_WMS', 'Collation');` – erwartet `…_CI_…`. Bis zum Ergebnis ist
„SQL-Wege ignorieren Groß/Klein" **unverifiziert**.

**Prüfschritt P-3 (Server-Kultur, Zahlen):** Die App setzt keine Request-Kultur ([[fallstricke]] §22). Am
Testsystem eine Zahlspalte mit Nachkommastellen ansehen (z. B. Beschichtungsauftrag `breite`): steht `2,5` (de) oder
`2.5` (en)? Regel 6 setzt deutsches Format voraus – bei `2.5` würde `>2` falsch gelesen. **Unverifiziert** bis
gesehen; bei en-Kultur stoppt der Dev-Lauf und meldet.

## Audit-Feld-Auswirkungen

Keine. Filter lesen nur; es entsteht keine neue Schreibstelle und kein neues Feld. Anpassungen gespeicherter
Standardfilter (Antwort 7) laufen über die bestehende Benutzerpflege.

## Akzeptanzkriterien

Etappe A
1. `7%` (beginnt), `%7` (endet), `=KA` (genau, Groß/Klein egal), `(leer)`, `!(leer)`, `;` und in Textspalten `,`
   wirken in der FA-Liste auf jede filterbare Textspalte; `!` negiert alle Teile.
2. FA-Liste: `>=01.10.2026`, `01.10.2026...31.10.2026`, `<01.10.2026` wirken auf jede Datumsspalte (inkl.
   KO-Termin); `kw42` und `10.2026` wirken wie heute als „enthält".
3. FA-Liste, Spalte Prio (Zahl): `>2`, `1...3`, `=1` wirken numerisch.
4. Test mit `ToQueryString()` gegen nie geöffneten SqlServer-Context: für `A_1`, `50%`, `[x]` enthält das SQL kein
   unmaskiertes `_`/`%`/`[` als Platzhalter (Escape/ESCAPE sichtbar); kein `EF.Functions.Like` mehr in
   `ApplyLeitstandColumnFilter` (Grep).
5. Gemeinsame Testtabelle: C#-Theory grün **und** Node-Lauf grün; im QA-Nachweis ist belegt, dass der Node-Test
   **nicht übersprungen** wurde (Testausgabe „…ok").
6. Gleiche Treffer client/server (P5): dieselbe Eingabe in WarehousePicking/Details (Client-Mode) und in dessen
   Druck (`WarehousePickingController.cs:386`, Server-`Apply`) liefert dieselben Positionen – Testszenario + ein
   Testtabellen-Lauf beider Kerne.
7. Bereich oder `>`/`<` in einer Textspalte: Feld rot mit Hinweistext, Teil wirkungslos (Antwort 6), Client und
   Server gleich; Hinweis erscheint auch nach Reload aus der URL.
8. Syntax-Hilfe: Knopf „?" an jeder Filterzeile, per Tab erreichbar, Popover per Enter/Leertaste, Escape schließt,
   jedes Filterfeld hat `aria-describedby`; Kontrast WCAG AA.
9. Hilfeseite `Help/Index.cshtml` beschreibt die neue Syntax (ersetzt 203-212).
10. Nach Antwort 8: Filterkarten-Freitext FA-Nr. `A_1` trifft nicht mehr `AX1`. Nach Antwort 9: Filter
    „Übergeordnete Sub-FA" wirkt in der FA-Liste.

Etappe B
11. Alle Stellen der Erhebung (S2–S6, M2, M3, J2, J3) nutzen den Kern; Grep findet kein `EF.Functions.Like` mit
    Benutzereingabe mehr und keinen Aufruf der alten `Parse`-Signatur; `bomMatchesFilter` und `rowMatchesAll`
    enthalten keine eigene Syntax-Logik mehr.
12. Laufzeit Artikelliste (Testsystem, `SET STATISTICS TIME, IO ON` auf das per `ToQueryString` erzeugte SQL):
    „enthält" auf Artikelnummer/Bezeichnung nicht langsamer als vorher (gleicher Plan); „beginnt mit" auf
    Artikelnummer zeigt einen Index Seek auf dem UQ-Index.
13. Jede gefilterte Zahl- und Datumsspalte ist markiert; der Drift-Wächter schlägt fehl, wenn `<th>`-Markierung und
    Server-Register auseinanderlaufen.
14. Stückliste: `!(leer)`-Standardfilter, Komm.-Ziel-Dropdown, Vorfahren-Aufklappen, Leerzustand und CSV
    (`data-csv-filter-match`) verhalten sich wie vorher; neu wirken `=`, `%`, `(leer)`, Bereich auf `Menge`.
15. FA-Baum: Textfilter verstehen die Syntax; `<select>`-Filter bleiben exakt; Kontextzeilen/Zähler wie vorher.
16. Bewegungshistorie Datum/Zeit: `01.10.2026`, `10.2026`, `2026` wie heute; neu `>=01.10.2026`, Bereich;
    Tagesgrenze nach Antwort 3.
17. Fehlteile: Negation nach Antwort 5; BDE-Buchungen: Start/Ende/Gut/Ausschuss mit Vergleich/Bereich.
18. FA-Abarbeitungsliste `quantity`: Getter rendert wie die Zelle; `>1` trifft eine Menge `2`.
19. Bestehende Tests grün; `docs/TESTSZENARIEN.md` + `secondbrain/tests/testszenarien-index.md` nachgezogen.

## Test-Szenarien

Neues Kapitel **TS-<nächste freie>** „Spaltenfilter-Suchsyntax" in `docs/TESTSZENARIEN.md` (Messstand: TS-89 belegt,
Teil-3-Lauf belegt vermutlich die nächste). Index [[testszenarien-index]] nachziehen.

Etappe A (Vorbedingung: Testsystem mit IDEAL-Daten, Master `ProduktionsauftragHierarchisch` an; zusätzlich ein Lauf
mit Master aus für den AKE-Flachmodus)
- A.1 FA-Liste Text: `4711%`, `%-1`, `=KA`, `KA;SW`, `!KA;SW`, `(leer)`/`!(leer)` auf Kunde/Matchcode – Trefferzahl
  plausibel, Paginierung/`TotalCount` stimmt.
- A.2 FA-Liste Datum/Zahl: `>=01.10.2026`, Bereich, `kw42`, Kalender-Klick; Prio `>2`.
- A.3 Escaping: `A_1` trifft nicht `AX1`; `50%` in Bezeichnung.
- A.4 Ungültig: `1...5` in Artikelnummer → rot + Hinweis, Liste ungefiltert; Reload über URL zeigt den Hinweis.
- A.5 Client/Server gleich: WarehousePicking/Details filtern, Druck öffnen – gleiche Positionen.
- A.6 Hilfe: Tastaturbedienung „?", Screenreader-Beschreibung, Kontrast.
- A.7 Mitgeschaltete Listen (Antwort 4): je eine Apply-Liste (z. B. Lagerplätze) und eine Client-Liste mit `=` und `;`.
- Negativ: alte Eingaben `960,886` und `!960` liefern dasselbe wie vor der Änderung.

Etappe B
- B.1 Artikelliste `5000%` (Index Seek), `%-1`, Laufzeitvergleich (AK 12).
- B.2 Bewegungshistorie Datum/Zeit: Tag/Monat/Jahr wie heute + Vergleich/Bereich.
- B.3 BDE-Buchungen Start `>=01.10.2026`, Gut `>0`.
- B.4 Fehlteile `a;!b` vs. `!a;b` nach Antwort 5.
- B.5 Stückliste: Standardfilter `!(leer)` Komm.-Ziel, Menge `>=10`, CSV-Export der gefilterten Stückliste.
- B.6 FA-Baum: Textfilter `=KA`, Auswahlfilter unverändert.
- B.7 Leitstand: VK-Spalte `=fertig`, Matchcode `%x`, Termine Bereich.
- B.8 Zahl-/Datumsspalten aller übrigen Listen stichprobenartig (Beschichtungsaufträge `start`, Summiert-Sichten
  `sollmenge`).
- Negativ: P-1-Treffer (falls vorhanden) vorher/nachher prüfen.

## Etappen (nur bei epic: true)

Ein Worktree (das Bündel), zwei Etappen, je ein Commit, **ein** Merge mit dem Bündel. Branch-Sync durch den
Menschen über `scripts/sync-worktree.ps1`. Vor Etappe A: Zeilen neu messen (Teil-3-Lauf).

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| A | Zentraler Parser (strukturierte Tokens), SQL-Ausdrucksbauer, Speicher-Auswerter, JS-Spiegel `column-filter-syntax.js`, gemeinsame Testtabelle (JSON + C#-Theory + Node-Skript), Syntax-Hilfe in `table-filter.js`, Hilfeseite, Typ-Register-Gerüst; Referenzliste **FA-Liste** (S1, M1; S1f/Antwort 8; Sub-FA-Filter/Antwort 9). **STOPP – Mensch prüft am Testsystem.** | **umgesetzt 2026-10-13, wartet auf Sichtprüfung** (v1.58.0, TS-91.A) | `3fe70103`, `fe8f531c`, `6dc74c9b`, `e5d0467b`, QA `c33f010b`, Fix `10ccf1f5` |
| B | Alle übrigen Stellen (M2, S2, S3/Antwort 10, S4, S5, M3, S6/Antwort 5, J2, J3), Typmarkierung aller Zahl-/Datumsspalten + Drift-Wächter, alte `Parse`-Signatur entfernt, FaWorklist-Getter, TS-Kapitel, Brain-Pflichten. Danach QA für das Epic. | offen | |

Zwischenzustand nach A (bewusst, kein Merge dazwischen): Der Leitstand teilt S1 mit der FA-Liste, filtert seine
SQL-Spalten also schon neu, seine C#-Postfilter (M2) noch alt; Zahl-/Datumsspalten der Apply-Listen gelten bis B als
Text.

**Brain-Pflichten bei Umsetzung**
- ADR-Kandidat **„Ein Parser für die Spaltenfilter-Syntax"** (nächste ADR-Nummer, Stand 0016): eine Grammatik, ein
  C#-Kern, ein JS-Spiegel, gemeinsame Testtabelle, kein eigenes `matches` in Views/Repos. Additiver Nachtrag in
  [[0005-listen-view-pattern-mit-server-side-spaltenfilter]] (deren Zeile „Mini-Syntax: OR `,`, NOT `!`" ist
  überholt).
- [[fallstricke]]: Eintrag §17 „`!(leer)` … VIERTE Umsetzung" als erledigt ergänzen; neuer Eintrag
  „Spaltenfilter nie selbst auswerten – Kern nutzen; Typ an `<th>` UND im Register".
- [[feature-map]], Brain-Changelog, Anwender-Changelog, beide `AppVersion.cs`, Hilfeseite.

## Deploy

**Vorläufig – QA bestätigt gegen den echten Diff.**
- **Web-App:** ja (Controller, Repositories, Views, JS).
- **Service:** nein (kein Service-Code betroffen; `ColumnFilterHelper` wird im Service nicht aufgerufen).
- **Migration:** nein.
- **Vor dem Deploy:** Prüfabfrage P-1 (und Antwort 7 umsetzen), P-2 Kollation, P-3 Kultur.
- **Publish-Befehle** (im Bündel-Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
- Anwender informieren: in Zahl-/Datumsspalten ODER nur noch mit `;`.

## Offene Rueckfragen

1. **`*` zusätzlich zu `%`?** Empfehlung **nein**: ein Platzhalter reicht, `*` erweitert jede Testmatrix und
   kollidiert mit keiner bestehenden Nutzung, die es rechtfertigt.
2. **Schreibweise „leer"**: `(leer)` / `!(leer)`, Groß/Klein egal? Empfehlung **ja** – so ist es in der Stückliste
   eingeführt (`Bom.cshtml:837-839`, case-insensitiv erkannt) und in gespeicherten Profilen normalisiert
   (`KommissionierzielFilterWert.cs:12`, `Profile.cshtml:53`).
3. **Datum mit Uhrzeit** (`dd.MM.yyyy HH:mm`, z. B. BDE-Buchungen Start/Ende `BdeBookingsController.cs:309-315`,
   Bewegungshistorie `datetime`): `=T` = ganzer Tag, `>T` = ab Folgetag 00:00, `>=T` = ab T 00:00, `<T` = vor T,
   `<=T` = bis Ende T, Bereich = ganze Tage inklusive? Empfehlung **ja**: Der Anwender sieht und tippt Tage; so
   arbeiten schon der Datum-Spaltenfilter der Bewegungshistorie (`StockMovementRepository.cs:645-647`, Tag =
   `[start, start+1)`) und die Bis-Grenze der Filterkarte (`StockMovementRepository.cs:319`).
4. **Etappe A schaltet mehr als die FA-Liste**: Weil `Apply` (30 Aufrufstellen) und `table-filter.js` zentral sind,
   verstehen nach A alle diese Listen die neue Syntax für Textspalten. Einverstanden? Empfehlung **ja** – die
   Alternative wäre ein zweiter Parser parallel, genau das Problem dieser Spec.
5. **Fehlteile-Negation vereinheitlichen?** Heute `a,!b` = enthält a UND NICHT b, `!a,b` = NICHT a UND enthält b
   (`WarehouseRequisitionRepository.cs:458-461`, 516-519); überall sonst gilt `!` für alle Werte. Empfehlung **ja**,
   gemeinsame Bedeutung (Hilfe nennt es); sonst bleibt eine sechste Bedeutung bestehen.
6. **Ungültige Eingaben**: Teil wirkungslos, Feld rot mit Hinweistext, Client und Server gleich? Empfehlung **ja** –
   sichtbar statt still, und keine leere Liste, die wie „keine Daten" aussieht.
7. **Gespeicherte Standardfilter**: nach P-1 kein Migrationsskript; Treffer passt der Admin in der Benutzerpflege an
   (oder informiert den Benutzer), bei 0 Treffern nichts? Empfehlung **ja** – fünf Freitextfelder, wenige Benutzer,
   eine stille Umschreibung könnte eine gewollte Eingabe verfälschen.
8. **Filterkarten-Freitext** FA-Nr./Artikelnummer/Kunde (`ProductionOrderRepository.cs:104-136`, auch Leitstand)
   geht unmaskiert in `LIKE`. Nur Escaping in Etappe A mitnehmen, ohne neue Syntax? Empfehlung **ja** – gleiche
   Datei, gleicher Fehler, wenige Zeilen.
9. **„Übergeordnete Sub-FA"** ist in FA-Liste, Leitstand, FA-Abarbeitungsliste, FA-Vervollständigung und
   Kommissionierliste filterbar (`<th data-filterable data-col-key="parent-sub-order-number">`, z. B.
   `ProductionOrders/Index.cshtml:110`), hat aber nirgends ein Server-Mapping (Grep in `*.cs`: nur
   `ColumnDefinitions`/ViewModels) – `ApplyLeitstandColumnFilter` fällt auf `_ => q` (340), die ColumnMaps kennen
   den Key nicht (z. B. `FaWorklistController.cs:404-425`). Der Filter tut still nichts. Mitbeheben? Empfehlung **ja**
   (A: FA-Liste, B: übrige vier).
10. **OSEON-Teileverfolgung**: `ApplyOseonColumnFilter` ist ohne Filterzeile in der View nur per Hand-URL erreichbar.
    Löschen statt umstellen? Empfehlung **löschen** – kleinerer Diff, keine verborgene zweite Bedeutung; wird später
    eine Filterzeile gebraucht, nutzt sie den Kern.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. `*` zusätzlich zu `%` → **Nein, kein `*` (Gerald 2026-10-13).**
2. Schreibweise `(leer)` / `!(leer)` wie Stückliste → **Ja, wie Stückliste (Gerald 2026-10-13).**
3. Datum mit Uhrzeit tageweise → **Ja, tageweise (Gerald 2026-10-13).**
4. Etappe A schaltet alle Apply-/Client-Listen für Textspalten mit um → **Ja (Gerald 2026-10-13); Abnahme deckt alle Listen mit Textspalten-Filter ab.**
5. Fehlteile-Negation vereinheitlichen → **Ja (Gerald 2026-10-13).**
6. Ungültige Eingaben: wirkungslos + rot mit Hinweis → **Ja (Gerald 2026-10-13).**
7. Gespeicherte Standardfilter: kein Migrationsskript, manuelle Anpassung → **Ja (Gerald 2026-10-13): kein Skript; P-1 vor dem Deploy auf BEIDEN Servern (AKE und IDEAL, je DB IDEAL_AKE_WMS) ausführen, Treffer korrigiert ein Admin in der Benutzerpflege bzw. informiert den Benutzer.**
8. Filterkarten-Freitext: nur Escaping in Etappe A → **Ja (Gerald 2026-10-13).**
9. „Übergeordnete Sub-FA"-Filter mitbeheben → **Ja (Gerald 2026-10-13).**
10. OSEON-Spaltenfilter-Code löschen → **Ja, löschen (Gerald 2026-10-13).**

## Umsetzung Etappe A (2026-10-13, Dev-Lauf)

Direkt im Bündel `feature/2026-08-07-ideal-teile-1-5`, Start `d4f326ef` (v1.57.0; Worktree sauber, keine offenen wip-Commits).
Plan `docs/superpowers/plans/2026-10-13-suchsyntax-spaltenfilter-etappe-a.md`. Commits `3fe70103` (Kern C#/JS, SQL-Bauer, Apply,
Testtabelle), `fe8f531c` (FA-Liste Server), `6dc74c9b` (table-filter.js, Hilfe-Popover, Layout, Prio-Typ), `e5d0467b` (v1.58.0,
Hilfeseite, Changelog, TS-91.A). **Version 1.58.0, TS-91 (Unterkapitel A).** Keine Migration. **Status bleibt InUmsetzung —
Etappe B erst auf Befehl.** Aufgabe [[2026-10-13-suchsyntax-spaltenfilter-erweitern-umsetzung]].

**Neu gemessen gegen `d4f326ef`:** Alle Zeilenangaben der Etappe-A-Stellen waren gegenüber `d3746373` unverändert (der Teil-3-Lauf
berührte keine dieser Dateien außer `site.css`): `ColumnFilterHelper.cs:45-60/71-93`, `table-filter.js:149-225/272-279`,
`ProductionOrderRepository.cs:104-136/271-342/364-390/425-461`, `ProductionOrdersController.cs:349/358/432/440/483-486/506-543`,
`Help/Index.cshtml:203-212`, `_Layout.cshtml:418`.

**Code-Einstieg:**
- `Services/ColumnFilterSyntax.cs`: `ColumnFilterSyntax.Parse(raw, type)` → `ParsedColumnFilter` (Negate, Parts, Invalid),
  `.Matches(cell, parsed, type)` (Speicher-Auswerter); `ColumnFilterSql.Predicate<T>(parsed, selectors…)` (SQL-Bauer:
  `string.Contains/StartsWith/EndsWith/==` auf Konstanten, Null-Guards für Navigationsketten, `(leer)` = alle leer),
  `ColumnFilterSql.ValuePredicate` + `.Splice` (Teil-Prädikat in Compiler-Lambdas, Kunde-Subquery); `ColumnFilterTypes.FaListe`
  (Register-Gerüst: 6 Datums-Keys + `prio`).
- `ColumnFilterHelper.Apply` ruft den Kern (optional Typen, Standard Text) — die 30 Aufrufstellen bleiben unverändert
  (Antwort 4). Alte `Parse`-Signatur bleibt bis Etappe B.
- `wwwroot/js/column-filter-syntax.js` (UMD, eingebunden in `_Layout.cshtml` vor `RenderSectionAsync("Scripts")`);
  `table-filter.js`: Typ je Spalte (`data-date-filter` / `data-filter-type`), `matchesFilter` entfernt, ungültige Teile
  `is-invalid` + Text (auch nach Reload aus URL/Storage), „?“-Knopf mit Popover in der ersten Filterzelle, `aria-describedby`
  an jedem Filterfeld.
- FA-Liste: `ProductionOrderRepository.ApplyLeitstandColumnFilter` komplett über den SQL-Bauer (inkl. ExtraInfo-Spalten,
  `BuildExtraInfoOrContains` entfernt), Kunde-Subquery per `Splice`, `parent-sub-order-number` gemappt (Antwort 9);
  Filterkarten-Freitext per `string.Contains` (Antwort 8); `ProductionOrdersController` Datums-/Kopfdaten-Postfilter über den
  Kern (Datum tageweise, Prio Zahl); `<th data-col-key="prio" data-filter-type="number">`.

**Rulings (ohne Rückfrage entschieden):**
- R1 — **`=` in Zahl-/Datumsspalten vergleicht den Wert**, wenn er lesbar ist (sonst Text). Grund: Regel 5 („`=` auf den
  Zelltext“) widerspricht AK 3 (`=1` numerisch) und Antwort 3 (`=T` = ganzer Tag); Zelltexte tragen ` kwNN` bzw. Uhrzeit, ein
  Textvergleich träfe nie. Kosten bei Irrtum: `=` in Datumsspalten müsste wieder Text vergleichen.
- R2 — Groß/Klein: der SQL-Bauer setzt **kein** `ToLower()` auf Spalten (Spec, Index); damit hängt die Groß/Klein-Unabhängigkeit
  in SQL an der CI-Kollation (**P-2 wird Pflicht vor dem Deploy**). Folge: **vier Bestandstests** in
  `ProductionOrderRepositoryTests` (Kältemittel-Filter, Kunde-Spaltenfilter ×2, Kunde-Freitext) bekamen Testdaten in
  Kleinschreibung, weil EF-InMemory ordinal vergleicht; Kommentar im Test. Vorher war der ExtraInfo-Pfad per `ToLower()` und
  der Kunde per `EF.Functions.Like` auch unter InMemory unabhängig von Groß/Klein.
- R3 — Ein Teil nur aus `%` und ein leeres `=` sind wirkungslos (kein Fehler); ein Filter, dessen Teile alle ungültig sind, wirkt
  gar nicht (Liste ungefiltert, Feld rot).
- R4 — Syntax-Hilfe als Popover mit `trigger: 'click'` (Enter/Leertaste am Knopf), Escape schließt global; Inhalt per DOM, keine
  HTML-Strings.
- R5 — Der Hilfetext sagt ausdrücklich, dass Vergleich/Bereich bis Etappe B nur in der FA-Liste wirken (übrige Zahl-/Datumsspalten
  = Text, Zwischenzustand laut Spec).

**AK-Beleg Etappe A:** AK 1/3/10 InMemory-Test + `FaListePostfilterSyntaxTests`; AK 2 `FaListePostfilterSyntaxTests` (Datum inkl.
KO-Termin, kw, Monat, leer); AK 4 `ToQueryString`-Tests (`a\_1`, `5\%0`, `\[x]` mit `ESCAPE`) + Quelltext-Test (kein
`EF.Functions.Like(` in `ProductionOrderRepository.cs`); AK 5 C#-Theory (57 Fälle) **und** Node-Lauf
`Kern_NodeSelbstpruefung_GleicheTabelle` — **bestanden, nicht übersprungen** (Ausgabe „column-filter-syntax core ok (57 Faelle)“);
AK 6 gleiche Tabelle beider Kerne + TS-91.A5; AK 7 Kern (`Invalid`) + Quelltext-Wächter (`is-invalid`) + TS-91.A4; AK 8/9
Quelltext-Wächter + TS-91.A6. **Laufzeit (AK 12, Etappe B):** in A nur code-belegt — „beginnt mit“ erzeugt `LIKE N'4711%'` ohne
`LOWER(` (Test); die Messung mit `SET STATISTICS` auf der Artikelliste gehört zu B.1.

**Beweis:** `dotnet build IdealAkeWms.slnx` → 0 Fehler; `dotnet test` → Web 1922 bestanden / 1 übersprungen (+77), Service 272.

**Offen für die Sichtprüfung (STOPP):** TS-91.A1–A8 am Testsystem; vorher P-2 (Kollation) und P-3 (Kultur) ansehen.

## QA Etappe A (2026-10-13)

qa-agent, eigener Lauf, Review synchron in diesem Lauf. Geprüft: Bereich `d4f326ef..e5d0467b` (4 Commits) im Worktree
`.claude/worktrees/2026-08-07-ideal-teile-1-5`, Zweig `feature/2026-08-07-ideal-teile-1-5`. QA-Fix-Commit **`c33f010b`**.
**Status bleibt `InUmsetzung`** (Epic, Etappe B offen) — keine Deploy-Finalisierung, keine Testbereit-Checkliste.

**Ergebnis: Etappe A ist abnahmefähig für die Sichtprüfung (TS-91.A)** — mit den Befunden I-1/I-2 als Hinweis an den Prüfer.

### Build und Tests (nach `c33f010b`)

- `dotnet build IdealAkeWms.slnx` → 0 Fehler, 9 Warnungen (keine in den geänderten Dateien).
- `dotnet test` → **IdealAkeWms.Tests 1935 bestanden / 1 übersprungen / 0 Fehler** (vor QA-Fix 1922/1/0; +13 neue Tabellenfälle),
  **IDEALAKEWMSService.Tests 272 / 0 / 0**.
- Übersprungen ist nur der vorbestehende
  `Integration.ProductionOrderEagerCreateAgentJobTests.EagerCreate_TwoMergesProduce2Rows_PerNewOrder_AndAreIdempotent`
  (per trx belegt, Outcome `NotExecuted`).
- **Node-Lauf nicht übersprungen (AK 5):** `ColumnFilterSyntaxTests.Kern_NodeSelbstpruefung_GleicheTabelle` → „Bestanden [85 ms]“
  (Einzellauf, Ausführlichkeit normal); direkter Lauf `node column-filter-syntax.check.js …` → `column-filter-syntax core ok (70 Faelle)`.

### Review-Befunde

Geprüft: `ColumnFilterSyntax.cs` (Parser, Speicher-Auswerter, SQL-Bauer, Splice), `column-filter-syntax.js`, `ColumnFilterHelper.Apply`,
`ProductionOrderRepository` (Freitext, `ApplyLeitstandColumnFilter`, Kunde-Subquery), `ProductionOrdersController` (Aufteilung
SQL/Postfilter, Datum/Prio), `table-filter.js` (Typ, Validierung, Hilfe, Restore, applyFilters), Zusammenspiel mit
`column-preferences.js`, `Bom.cshtml`, `FaHierarchy/Index.cshtml`; Layout, CSS, Hilfe, Changelog, TS-91.A, beide `AppVersion.cs`.
Erzeugtes SQL zusätzlich mit einem temporären `ToQueryString`-Lauf angesehen (Datei danach gelöscht, nicht committet).

**Critical:** keine.

**Important**
- **I-1 Client-Typ und Server-Typ laufen in fünf Server-Mode-Listen auseinander (Zwischenzustand).** `table-filter.js` typisiert jede
  `<th data-date-filter>` als Datum. Außer der FA-Liste tragen das noch FA-Abarbeitungsliste (4), FA-Vervollständigung (1),
  Kommissionierliste `Picking/Index` (1) — Server über `Apply` ohne Typen = Text — sowie Leitstand (5) und BDE-Buchungen (2) mit
  alter `Parse`-Logik. Folge: `>=01.10.2026` wird dort im Browser **nicht** rot markiert, der Server ignoriert es (Apply-Listen:
  Liste ungefiltert) bzw. sucht es als Text (Leitstand/BDE: leere Liste). Das widerspricht in diesen Listen Antwort 6/AK 7
  („Client und Server gleich“) und der Erwartung in TS-91.A7 („`>5` → rot“ gilt nur für unmarkierte Spalten).
  **Behandlung:** kein Fix in A (wäre Wegwerfcode) — das Typ-Register für alle Listen + Drift-Wächter ist Kern von Etappe B
  (AK 13). Die Hilfeseite sagt bereits, dass Vergleiche bis zum Ausbau nur in der FA-Liste wirken. Prüfer-Hinweis unten; in B
  Pflichtpunkt.
- **I-2 Stückliste zeigt „?“-Hilfe und Rot-Markierung für eine Syntax, die sie noch nicht kann.** `Bom.cshtml:700` bindet
  `table-filter.js` ein → „?“-Popover (`;`, `=`, `%`, `(leer)`) und `is-invalid` erscheinen; gefiltert wird aber weiter mit
  `bomMatchesFilter` (`Bom.cshtml:842-853`, nur `,`/`!`/Alle Ziele). `=KA` oder `KA;SW` liefern dort eine leere Liste; `1...5` wird
  als „ignoriert“ markiert, filtert aber als Text. **Behandlung:** J2 ist Etappe B (AK 14); bis dahin bewusst so gelassen, dem Prüfer
  als bekannter Zwischenzustand genannt. Kein Merge zwischen A und B, daher kein Produktionsrisiko.

**Minor**
- **M-1 Kontrast (AK 8) — behoben in `c33f010b`.** „?“-Knopf (`btn-outline-secondary` #6c757d) und Hinweistext
  (`invalid-feedback` #dc3545) auf dem Filterzeilen-Grau #f8f9fa: gerechnet ≈4,45:1 bzw. ≈4,3:1, unter AA 4,5:1 für kleine Schrift.
  Jetzt #495057 (≈7,8:1) und #b02a37 (≈6,2:1), weiter Bootstrap-Töne (`site.css`, Block `.col-filter-*`).
- **M-2 Testtabelle deckte „jeder Operator × Typ“ nicht ganz ab — ergänzt in `c33f010b`.** Fehlten u. a. `<` und `;` in Zahl,
  `%` in Zahl/Datum, `=` mit unlesbarem Wert (R1-Rückfall auf Text), `(leer)` Zahl, `!(leer)` Datum, `>` ohne Wert, vertauschter
  Datumsbereich, Ein-Tages-Bereich bis Tagesende, leeres `=` (R3). 13 Fälle neu (57 → 70), C#-Theory und Node grün, keine
  Kernänderung nötig. `50%` ist per Grammatik „beginnt mit 50“; der Literal-`%`-Fall ist `5%0` (T11/T12) — korrekt.
- **M-3 Testname `GetForLeitstand_FiltersOnKaeltemittel_CaseInsensitive_WithNullGuard` verspricht Groß/Klein-Unabhängigkeit, die er
  nach R2 nicht mehr belegt** (Daten jetzt klein). Groß/Klein hängt in SQL an der Kollation → Manual (P-2). Nicht umbenannt
  (Kommentar im Test erklärt es) — in B mitnehmen.
- **M-4 P-2 prüft nur die DB-Kollation.** Spalten können eine eigene Kollation tragen. Empfehlung für beide Server (AKE und IDEAL):
  `SELECT t.name, c.name, c.collation_name FROM sys.columns c JOIN sys.tables t ON t.object_id = c.object_id WHERE t.name IN
  ('ProductionOrders','ProductionOrderExtraInfos','FaHierarchyOrderInfos','ProductionWorkplaces') AND c.collation_name IS NOT NULL;`
  — alle `…_CI_…` erwartet. Neu abhängig von der Kollation sind nur die ExtraInfo-Spalten (vorher `ToLower()`); die übrigen
  S1-Spalten hingen schon mit `EF.Functions.Like` + kleingeschriebenem Token daran.
- **M-5 „?“-Knopf sitzt in der physisch ersten Filterzelle.** Blendet der Benutzer die erste Spalte über das Zahnrad aus, verschwindet
  der Knopf mit (`column-preferences.js:165-169` setzt `display:none` auf die Filterzelle); beim Umordnen wandert er mit der Spalte.
  Der `aria-describedby`-Kurztext bleibt an jedem Feld. Kein Fix in A; in TS-91.A6 einmal mit ausgeblendeter erster Spalte ansehen.
- **M-6 Doku-Kommentar in `ColumnFilterHelper.Apply`** hat eine Leerzeile mitten im `///`-Block (kosmetisch, keine Build-Warnung).

**Ohne Befund geprüft**
- **Grammatik C# = JS (Regeln 1–10):** gleiche Reihenfolge (`(leer)` → `>=`/`<=`/`>`/`<` → `=` → `...` → `%`), gleiche Regexe
  (Zahl/Datum, `(?!\d)`, `yy`+2000, Tagesprüfung), gleiche Trenner je Typ, `!` nur am Anfang für alle Teile, Trim/Kleinschreibung,
  ungültige Teile wirkungslos mit Grund. Einzige Abweichungsmöglichkeit `decimal` vs. JS-`number` — ohne Arithmetik unkritisch.
- **R1 (`=` in Zahl/Datum = Wertvergleich, sonst Text): vertretbar.** Regel 5 wörtlich kollidiert mit AK 3 (`=1` numerisch) und
  Antwort 3 (`=T` ganzer Tag); Zelltexte tragen ` kwNN`/Uhrzeit, ein Textvergleich träfe nie. R1 ist die einzige Lesart, die AK 3 und
  Antwort 3 erfüllt; der Text-Rückfall bei unlesbarem Wert ist jetzt durch N17/D17 belegt. Regel 5 in B/ADR präzisieren.
- **SQL-Bauer:** Konstanten über `string.Contains/StartsWith/EndsWith/==` → `LIKE N'…' ESCAPE N'\'` (`a\_1`, `5\%0`, `\[x]`);
  Freitext als Parameter `@…_contains = N'%A\_1%'` mit `ESCAPE` (Antwort 8, temporärer Lauf). Null-Guards: positiv
  `[p0].[Id] IS NOT NULL AND …`, negiert `[p0].[Id] IS NULL OR [p0].[Name] IS NULL OR … NOT LIKE` → leere Zellen treffen `!x`
  (Regel 10). `(leer)` über FA-Nr./Sub-FA = UND (Regel 8). `!(leer)` → `IS NOT NULL AND <> N''` (SQL-`=` ignoriert Leerzeichen am
  Ende = „nur Leerzeichen“). Kunde-Subquery per `Splice`: `!muster;(leer)` → `(Customer IS NULL OR NOT LIKE) AND NOT EXISTS(…)
  AND (Customer <> '' OR EXISTS(Kopf nicht leer))` — korrekt. Kein `EF.Functions.Like(` mehr in `ProductionOrderRepository.cs`
  (nur noch in Kommentaren; Grep).
- **Antwort 4:** keine `ColumnFilterHelper.Apply`-Aufrufstelle geändert (der Diff berührt außer der FA-Liste nur
  `ColumnFilterHelper.cs`); `types` optional, Standard Text; `960,886` / `!960` per `Apply_AlteEingabenUnveraendert_TypenOptional`.
- **Antwort 6/AK 7:** Server ignoriert ungültige Teile (Parse → `IsEmpty` → kein Prädikat, SQL wie Postfilter); Client markiert
  nach `restoreFiltersFromUrl`/`restoreFiltersFromStorage` und bei Eingabe; Hinweis als Text + `aria-invalid` + `aria-describedby`.
  Einschränkung siehe I-1/I-2.
- **table-filter.js Regression:** Sortierung, Kalender (setzt den Wert programmgesteuert, immer gültiges Datum), Server-Mode ENTER,
  sessionStorage, `getActiveFilters` unverändert; Popover-Inhalt per DOM/`textContent` (kein HTML-String, `sanitize:false`
  unschädlich); der Hinweis wird entfernt statt per `hidden` (keine `hidden`+`d-*`-Falle). Der FA-Baum bindet `table-filter.js` nicht
  ein (`FaHierarchy/Index.cshtml:308`) → unberührt.
- **Antwort 9:** `parent-sub-order-number` → `o.ParentSubOrderNumber` (SQL), InMemory-Test `=901`.
- **Grenzen:** alte `Parse`-Signatur bleibt (7 Aufrufe); Article-/StockMovement-/BdeBooking-/WarehouseRequisition-/Oseon-Repositories,
  Leitstand-/BDE-Controller, `Bom.cshtml`, `fa-hierarchy-tree.js` unverändert — nichts aus Etappe B angefasst.
- **Doku:** Hilfeseite ersetzt die Mini-Syntax (inkl. `kw41;kw42`-Hinweis und Zwischenzustand R5); Changelog v1.58.0; beide
  `AppVersion.cs` 1.58.0; TS-91.A in `docs/TESTSZENARIEN.md` (LF, keine CR). Keine Migration, kein SQL-Skript, kein Service-Code.
- Bekannte Fehlerklassen: keine Werte in `<script>`-Blöcken, kein `?? 0`, keine Parallel-Arrays, Regel serverseitig erzwungen.

### Beweisart je Akzeptanzkriterium (Etappe A)

| AK | Beweis | Art |
|---|---|---|
| 1 Text-Syntax FA-Liste | `Spaltenfilter_InMemory_NeueSyntax_FaListe_AK1_AK10`, Tabelle T01–T32; Groß/Klein in SQL = Kollation | Automatisiert (Semantik) + **Manual-UAT** TS-91.A1 (echtes SQL, P-2) |
| 2 Datum FA-Liste | `FaListePostfilterSyntaxTests.Datum_…_AK2` (C#-Postfilter) | Automatisiert + Manual TS-91.A2 (Kalender, Paginierung) |
| 3 Prio Zahl | `Prio_Numerisch_AK3`; `<th data-filter-type="number">` per Quelltext-Test | Automatisiert + Manual TS-91.A2 (P-3 Kultur) |
| 4 Escaping / kein Like | `Spaltenfilter_SqlServer_PlatzhalterMaskiert` (`ToQueryString`, SqlServer-Provider), Quelltext-Grep | Automatisiert (SQL-Text); Ausführung gegen echte DB Manual TS-91.A3 |
| 5 gemeinsame Tabelle | C#-Theory 70 Fälle + Node-Lauf **bestanden, nicht übersprungen** | Automatisiert |
| 6 Client = Server (WarehousePicking) | gleiche Tabelle beider Kerne; reale Seite + Druck | Teil automatisiert, **Manual-UAT** TS-91.A5 |
| 7 Ungültig rot + wirkungslos, auch nach Reload | Server: Tabelle T28–T30, InMemory `1...5`; Client nur Quelltext-Wächter (`is-invalid`) | **Manual-UAT** TS-91.A4 (Browser-JS); Einschränkung I-1/I-2 |
| 8 Hilfe „?“, Tastatur, aria, Kontrast | Quelltext-Wächter (Text/aria vorhanden); Kontrast per Rechnung + Fix M-1 | **Manual-UAT** TS-91.A6 |
| 9 Hilfeseite | Diff `Help/Index.cshtml` gelesen | **Manual** (Sicht) TS-91.A6 |
| 10 Freitext `A_1` / Sub-FA-Filter | `Freitext_SqlServer_…_AK10` (SQL-Text), InMemory-Test | Automatisiert + **Manual-UAT** TS-91.A3/A1 |

Nicht als grün gezählt: alles, was echtes SQL-Server-Verhalten (Kollation, LIKE-Ausführung), Browser-JavaScript oder Bedienung braucht.

### Geänderte Bestandstests

Nur vier Tests in `IdealAkeWms.Tests/Repositories/ProductionOrderRepositoryTests.cs`, jeweils **Testdaten auf Kleinschreibung**
(Erklär-Kommentar im Test), Assertions unverändert:
1. `GetForLeitstand_FiltersOnKaeltemittel_CaseInsensitive_WithNullGuard`
2. `Leitstand_Hierarchical_CustomerFreetextFilter_MatchesViaOrderInfo`
3. `Leitstand_Hierarchical_CustomerColumnFilter_MatchesViaOrderInfo`
4. `Leitstand_Hierarchical_CustomerColumnFilter_TwoTokens_OrAcrossRows`

Einstufung: **Verhalten bewusst durch die Spec geändert** (Technischer Entwurf „kein `ToLower()` auf Spalten“, „kein
`EF.Functions.Like`“; Groß/Klein über Kollation P-2; Ruling R2). EF-InMemory vergleicht `string.Contains` ordinal; vorher machten
`ToLower()` bzw. InMemory-`Like` den Test unabhängig von Groß/Klein. Kein verdecktes Fehlverhalten: in SQL Server hängt dasselbe an
der CI-Kollation (Pflicht P-2 vor Deploy, siehe M-4); die Groß/Klein-Eigenschaft ist damit Manual-UAT. Name von Test 1 siehe M-3.
Keine Kategorie „Sonstiges“.

### Hinweise an den Sichtprüfer (zusätzlich zu TS-91.A1–A8)

1. Vorher P-2 auf **beiden** Servern inkl. Spaltenkollation (M-4) und P-3 (Kultur `2,5`).
2. Datumsspalten in FA-Abarbeitungsliste, FA-Vervollständigung, Kommissionierliste, Leitstand, BDE-Buchungen: Vergleich/Bereich
   werden dort noch nicht rot markiert und wirken nicht (I-1, Etappe B).
3. Stückliste: die „?“-Hilfe zeigt die neue Syntax, die Stückliste filtert noch alt (I-2, Etappe B).
4. TS-91.A6 einmal mit ausgeblendeter erster Spalte (M-5) und die neuen Farben von „?“/Hinweis ansehen.

**Offen für Etappe B (aus dieser QA):** I-1 (Typ-Register aller Listen), I-2 (J2), M-3, M-5, Regel 5 auf R1 präzisieren (ADR).

### Nachtrag Dev-Lauf nach QA Etappe A (2026-10-13)

- **I-1 behoben** (`10ccf1f5`): Zahl/Datum gilt im Browser nur in Tabellen mit `data-filter-syntax="typed"` (Etappe A: nur die
  FA-Liste); alle übrigen Listen behandeln ihre Datums-/Zahlspalten im Browser als Text — genau wie ihr Server bis Etappe B. Ein
  Vergleich dort wird jetzt rot markiert statt still eine leere bzw. ungefilterte Liste zu liefern. TS-91.A9.
- **I-2 behoben** (`10ccf1f5`): Stückliste `data-filter-syntax="legacy"` — kein „?“-Knopf, keine Rot-Markierung, Filter wie bisher
  bis Etappe B.
- Minor: Testname `…Kaeltemittel_WithNullGuard_CaseViaCollation`, Leerzeile im Doku-Kommentar entfernt. Offen bleibt (Minor):
  „?“-Knopf verschwindet, wenn die erste Spalte ausgeblendet wird; P-2 auch je Spalte abfragen (steht im QA-Abschnitt).
- **Etappe B muss:** `typed` in allen Listen setzen (bzw. Attribut entfernen und Typ immer aus `<th>`), `legacy` an der Stückliste
  entfernen, Regel 5 der Grammatik auf Ruling R1 präzisieren.
- Beweis nach dem Fix: Build 0 Fehler; Web 1936 bestanden / 1 übersprungen (vorbestehend), Service 272; Node-Lauf bestanden.
