---
type: spec
title: "Beschichtungsauftrag-Ausbau Teil 1 — Summierte Ansicht"
slug: 2026-10-06-beschichtungsauftrag-ausbau-teil-1-spec
status: Entwurf
created: 2026-10-06
updated: 2026-10-07
source_backlog: "[[2026-10-06-beschichtungsauftrag-ausbau]]"
depends_on: ""
task: ""
worktree: ""
branch: ""
affected_code:
  - IdealAkeWms/Controllers/FaHierarchyBeschichtungController.cs (neue Actions Summiert, PrintSummiert, PdfSummiert/{hauptFa}; Muster FaHierarchyKommissionierListenController.cs:122-156 und :220-275)
  - IdealAkeWms/Services/BeschichtungsauftragService.cs (neu BuildSummiertAsync + statische, DB-freie AggregateSummiert + SummiertColumnMap + Umschalter-Query; Muster KommissionierListenService.cs:75-84, :116-235)
  - IdealAkeWms/Models/ViewModels/FaHierarchyBeschichtungViewModels.cs (neu Summiert-Zeile, -Ergebnis, -Bildschirm- und -Druck-ViewModel; Muster FaHierarchyKommissionierSummiertViewModel.cs)
  - IdealAkeWms/Views/FaHierarchyBeschichtung/Summiert.cshtml (neu, ADR-0005-Liste)
  - IdealAkeWms/Views/FaHierarchyBeschichtung/PrintSummiert.cshtml (neu, CSS eingebettet, Barcode als Data-URI)
  - IdealAkeWms/Views/FaHierarchyBeschichtung/Index.cshtml (Umschalter Liste/Summiert neben dem Drucken-Knopf, Zeilen 14-21)
  - IdealAkeWms/Views/Shared/_Layout.cshtml (Navigationseintrag Beschichtungsauftrag :117-118 auf asp-action="Summiert" — Summiert ist Standard, Antwort 2; Muster Kommissionierliste :184, :203)
  - IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs (neuer ViewConfig FaHierarchyBeschichtungSummiert + Eintrag im Switch bei :479-480)
  - IdealAkeWms.Tests/Services/BeschichtungsauftragServiceTests.cs (Aggregations-Tests)
  - IdealAkeWms/Views/Help/ (Hilfeseite Beschichtungsauftrag ergaenzen) + Views/Help/Changelog.cshtml + beide AppVersion.cs
  - docs/TESTSZENARIEN.md (Kapitel TS-60 erweitern oder neues Kapitel, naechste freie Nummer)
  - secondbrain/tests/testszenarien-index.md
open_questions: []
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

> **Umsetzungsort (Hinweis, keine Entscheidung):** nach Schranke 2 des IDEAL-Buendels, aus main (neuer
> Worktree). TS-Nummer und Version: naechste freie zum Umsetzungszeitpunkt. Code-Belege dieser Spec
> beziehen sich auf den Buendel-Stand `.claude/worktrees/2026-08-07-ideal-teile-1-5` (v1.47.1); nach
> dem Merge vor Umsetzung die Zeilennummern gegen main pruefen.

Teil 1 von 4 — Uebersicht: [[2026-10-06-beschichtungsauftrag-ausbau-uebersicht]].
Standort: **IDEAL** (Datenbasis `FaHierarchyNode`/`FaHierarchyOrderInfo`, nur von
`FaHierarchySyncService` befuellt). AKE unberuehrt.

## Ziel / Nutzen (das Warum)

Der Beschichtungsauftrag listet heute jede `FaHierarchyNode`-Zeile mit `Beschichtet == true` einzeln
(`BeschichtungsauftragService.cs:85`, `:93-109`). Gleiche Teile eines Geraets stehen dadurch mehrfach
untereinander; der Beschichter braucht die Stueckzahl je gleichem Teil. Die Kommissionierliste hat
genau diese Sicht seit v1.47.0 (TS-80) — Teil 1 uebertraegt das Muster, ohne neue Mechanik.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Zweite Sicht „Summiert" im bestehenden `FaHierarchyBeschichtungController`, Umschalter Liste/Summiert.
- **Summiert ist die Standardansicht** (Antwort 2): der Navigationseintrag zeigt auf `Summiert`; die
  Einzelliste bleibt ueber den Umschalter und ihre bisherige Route `/FaHierarchyBeschichtung` erreichbar.
- Aggregation der heutigen gefilterten Menge (Flag `Beschichtet == true`, alle Ebenen,
  `leafOnly: false` wie `BeschichtungsauftragService.cs:105`) nach dem Summierschluessel
  **HauptFA + Artnr** (Antwort 1).
- Kopfdaten je HauptFA (Dienstleister, RAL, Termine, Mehrdeutigkeits-Kennzeichnung) wie in der Liste.
- Druck und PDF der summierten Sicht (Antwort 2). ~~nur falls Rueckfrage 2 das bejaht~~ *(entschieden mit
  Antwort 2)*

**Out-of-Scope**
- Speichern, Nummer, Status — das ist Teil 3.
- Briefkopf/Corporate Design — Teil 2.
- Trennung nach Montage-Abteilung bei Kombigeraeten ([[2026-08-06-kombinationsgeraete-montageabteilung]]).
- KW-Filter (die Kommissionier-Summiert-Sicht hat einen ueber `KO_Termin`; fuer den Beschichtungsauftrag
  nicht angefordert).

## Fachliche Anforderungen

1. **Summierschluessel = `HauptFA` + `Artnr`** (Antwort 1). Maße (`Breite`/`Hoehe`/`Tiefe`) sind **kein**
   Schluesselteil, werden aber in der Zeile angezeigt und mitgedruckt — Begruendung der Antwort: Artikelnummer
   bzw. Matchcode sind eindeutig, damit sind die Maße je Artikel gleich. Diese Annahme ist eine fachliche
   Aussage, keine am Code belegte: `FaHierarchyNode` haelt die Maße je Strukturzeile (`FaHierarchyNode.cs:48-50`),
   nichts im Modell erzwingt gleiche Maße je `Artnr`. Deshalb **Invarianten-Warnung** (Hausregel „melden statt
   still behandeln"): weichen innerhalb einer Gruppe die Maße voneinander ab, zeigt die Zeile die Maße der
   ersten Position plus sichtbaren Hinweis „Maße uneinheitlich" (Text, nicht nur Farbe) und schreibt eine
   `ILogger`-Warnung mit HauptFA und Artnr. Belegte Ausgangslage, auf der die Entscheidung fusst:
   - **RAL liegt nicht auf der Position.** `FaHierarchyNode` hat kein RAL-Feld (`FaHierarchyNode.cs:13-63`).
     RAL steht nur in `FaHierarchyOrderInfo.RAL` (`FaHierarchyOrderInfo.cs:28`), geschrieben ausschliesslich
     vom Sync aus der Sage-View FAInfos (`FaHierarchySyncService.cs:283`, Quelle `i.[RAL]` in
     `FaHierarchySql.cs:139`); es gibt keinen WMS-Schreibpfad (Grep `\bRAL\s*=` in `*.cs`/`*.cshtml`:
     nur Sync-Zeile 283 und Migration; die Views lesen nur). Ist `HauptFA` im Schluessel, ist RAL je Gruppe konstant — ausser beim Kombigeraet
     (mehrere OrderInfo-Zeilen, `FaHierarchyListGroupViewModel.cs:30`), das bereits als mehrdeutig
     gekennzeichnet wird.
   - **Masse: `NULL` und `0` sind nicht unterscheidbar.** `Breite`/`Hoehe`/`Tiefe` sind nicht-nullable
     `decimal` (`FaHierarchyNode.cs:48-50`); der Sync mappt `NULL` aus Sage auf `0`
     (`FaHierarchySyncService.cs:302`, `Dec`). ~~Ein Schluessel mit Massen fasst „Mass fehlt" und „Mass 0"
     zusammen.~~ *(ueberholt mit Antwort 1 — Maße sind kein Schluesselteil; fuer die Invarianten-Pruefung
     gelten `NULL` und `0` als gleich, weil sie im Cache nicht unterscheidbar sind.)*
   - **Mengeneinheit:** `FaHierarchyNode` hat keine Einheit — dieselbe Lage und dieselbe Belegpflicht wie
     bei der Kommissionierliste (`fallstricke.md` §18). Solange `Artnr` im Schluessel steht, gilt die
     dortige Begruendung (Einheit ist Artikeleigenschaft).
   - **Bedeutung von `Sollmenge`** (Gesamtbedarf je HauptFA oder Menge je Vater-Einheit) ist am Code nicht
     belegbar. Teil 1 uebernimmt dieselbe Annahme wie die Kommissionier-Summierung
     (`KommissionierListenService.cs:180`, Summe der `Sollmenge`), prueft sie aber nicht neu — siehe
     Pruefschritt in den Test-Szenarien.
2. Summiert wird nur `Sollmenge` (wie Kommissionierliste). Matchcode, HauptArtnr und die Maße
   `Breite`/`Hoehe`/`Tiefe` werden als Eigenschaften der ersten Zeile mitgefuehrt (Muster
   `KommissionierListenService.cs:182-184`; Maße mit Invarianten-Pruefung aus Anforderung 1); zusaetzlich
   wird die Zahl der aggregierten Positionen angezeigt.
3. Seiteneinheit ist die HauptFA-Gruppe, `TotalCount` zaehlt Gruppen (Muster
   `KommissionierListenService.cs:197-203`).
4. Der Kopf je HauptFA zeigt dieselben Angaben wie die Liste (`Index.cshtml:75-101`): Dienstleister, RAL,
   Start, Retour, AB/Pos, Montage-Abteilung; bei mehreren Kopfzeilen alle Varianten plus Badge
   „mehrdeutig". Kein Fan-out-Join — Kopfdaten aus derselben separaten OrderInfo-Menge wie heute.
5. Leerfall: ohne Treffer Hinweiszeile statt leerer Tabelle; im Druck Hinweis statt leerem Blatt (wie
   `Print.cshtml:44-49`).
6. Zugriff unveraendert: Class-Level `[RequireBeschichtungsauftragAccess]` + `[RequireFaHierarchyBeschichtungAktiv]`
   (`FaHierarchyBeschichtungController.cs:27-28`) gelten automatisch fuer die neuen Actions (reine Lese-Actions,
   kein Edit-Split noetig).
7. **Standardansicht** (Antwort 2): Der Navigationseintrag „Beschichtungsauftrag" (`_Layout.cshtml:117-118`,
   heute `asp-action="Index"`) zeigt auf `Summiert` — dasselbe Vorgehen wie bei der Kommissionierliste seit
   v1.47.0 (`_Layout.cshtml:184`, `:203` zeigen auf `Summiert`). Die Active-Markierung des Eintrags
   (Controller-Vergleich, `:117`) bleibt fuer beide Sichten gueltig. `Index` behaelt seine Route (Lesezeichen,
   Redirect der Einzel-PDF bei Fehler `FaHierarchyBeschichtungController.cs:134`).

## Technischer Loesungsentwurf

Reine Wiederverwendung des Kommissionier-Musters (ponytail: Sprosse 2):
- `BeschichtungsauftragService.AggregateSummiert(allNodes, allInfos, columnFilters, page, pageSize, paginate)`
  — statisch und DB-frei, gleiche Reihenfolge wie `KommissionierListenService.AggregateSummiert`
  (`:147-211`): Flag-Praedikat `FlagPredicate` (`BeschichtungsauftragService.cs:85`) → GroupBy
  (`HauptFA`, `Artnr`) → Maße-Invarianten-Pruefung je Gruppe (Flag in der Zeile + `ILogger`-Warnung) →
  `ColumnFilterHelper.Apply` mit eigener `SummiertColumnMap` (Getter = gerenderter Zelltext, Formate
  `FormatMenge`/`FormatMass` aus `BeschichtungsauftragService.cs:61-64`) → Gruppen-Paging. Die Zeile traegt
  zusaetzlich die `OrderInfos` des HauptFA (aus `allInfos`, gruppiert nach HauptFA) fuer den Kopf.
- Mehrdeutigkeits-Logging: `LogAmbiguities` (`BeschichtungsauftragService.cs:116-126`) auch fuer die
  summierte Sicht aufrufen.
- Controller: `Summiert(int page, int? pageSize)`, `PrintSummiert(string? visibleColumns)`,
  `[HttpGet("FaHierarchyBeschichtung/PdfSummiert/{hauptFa:int}")] PdfSummiert(int hauptFa, ...)` — Kopie der
  Struktur `FaHierarchyKommissionierListenController.cs:122-156`/`:220-275` ohne Ziel/KW; Fehlerpfad
  `PdfRenderException` → `TempData["WarningMessage"]` → Redirect auf `Summiert` (nicht `Index`).
- Umschalter: Query-Keys, die beide Sichten teilen (`colf_hauptfa`, `colf_artnr`, `colf_matchcode`,
  `colf_hauptartnr`, `colf_breite`, `colf_hoehe`, `colf_tiefe` — Spaltenschluessel wie `Index.cshtml:38-46`),
  **ohne** `colf_sollmenge` (Einzel- vs. Summenwert — Begruendung `KommissionierListenService.cs:213-216`).
  ~~Mass-Spalten falls im Schluessel~~ *(Maße sind mit Antwort 1 Anzeigespalten; sie werden mitgetragen,
  weil sie je Artnr gleich sein sollen.)*
- ADR 0005: Summiert.cshtml mit `data-server-column-filter="true"`, `data-col-key` an jedem `<th>`,
  `_Pagination`-Partial, Filterkarte (Text wie `Index.cshtml:25-32`). Neue Spalten **gleichzeitig** in
  `<script id="column-config">` und `ColumnDefinitions` eintragen (`fallstricke.md`, Abschnitt
  Spalten-Praeferenzen, Zeilen 960-966).
- Navigation: `_Layout.cshtml:118` `asp-action="Index"` → `"Summiert"` (Anforderung 7).
- Druck/PDF (Antwort 2): PrintSummiert.cshtml mit eingebettetem CSS und Barcode als Data-URI
  (Pflicht laut [[2026-08-06-pdf-erzeugung-fahierarchy-druck-spec]], sonst fehlt im PDF, was am Bildschirm
  sichtbar ist); Filter-Banner bei aktivem Spaltenfilter wie `Print.cshtml:55-61`. Sichtbare-Spalten-Logik
  (`visibleColumns`) nur, wenn die Kommissionier-Variante (`ResolveVisibleColumnsAsync`) ohne Anpassung
  passt — sonst alle Spalten drucken.

## Migrations-/SQL-Auswirkungen

Keine. Keine neue Entitaet, keine Spalte, kein `SQL/XX_*.sql`, `SQL/00_FreshInstall.sql` unveraendert,
`SQL/AgentJobs/` unberuehrt.

## Audit-Feld-Auswirkungen

Keine — reine Lesefunktion auf Cache-Tabellen, die bewusst nicht `AuditableEntity` sind
(`FaHierarchyNode.cs:8-10`, `FaHierarchyOrderInfo.cs:10`).

## Akzeptanzkriterien

1. Der Navigationseintrag „Beschichtungsauftrag" oeffnet `/FaHierarchyBeschichtung/Summiert`; beide Sichten
   haben einen Umschalter „Liste | Summiert", „Liste" oeffnet `/FaHierarchyBeschichtung` (Einzelliste).
2. Zwei Positionen desselben HauptFA mit gleicher `Artnr` erscheinen in der summierten Sicht als **eine**
   Zeile mit der Summe der `Sollmenge` und Positionsanzahl 2.
3. Zwei Positionen desselben HauptFA mit verschiedener `Artnr` bleiben zwei Zeilen.
4. Positionen verschiedener HauptFA werden nie zusammengefasst.
4a. Die summierte Zeile zeigt `Breite`/`Hoehe`/`Tiefe`; weichen die Maße innerhalb einer Gruppe ab, steht
    sichtbar „Maße uneinheitlich" in der Zeile (Bildschirm und Druck) und das Log enthaelt eine Warnung mit
    HauptFA und Artnr; die Gruppe wird trotzdem **nicht** aufgeteilt.
5. Nur `Beschichtet == true` geht ein; Positionen aller Ebenen (auch `SubFA != 0`) zaehlen mit.
6. Der Kopf jeder Gruppe zeigt Dienstleister/RAL/Termine wie die Liste; bei mehreren OrderInfo-Zeilen
   alle Varianten + Badge „mehrdeutig" + `ILogger`-Warnung.
7. ADR 0005: Pagination nach Gruppen, Filterkarte, Server-Spaltenfilter an jeder Spalte; Spaltenfilter
   auf „Sollmenge" filtert die **Summe**.
8. Der Umschalter traegt die gemeinsamen Spaltenfilter mit, `colf_sollmenge` nicht.
9. Druck der summierten Sicht zeigt dieselben Zeilen wie der Bildschirm (ohne
   Paging), PDF je HauptFA liefert `Beschichtungsauftrag_<HauptFA>_<yyyyMMdd-HHmm>.pdf`; PDF-Fehler fuehrt
   mit WarningMessage zurueck auf `Summiert`.
10. Unit-Tests fuer `AggregateSummiert` (Zusammenfassen, Nicht-Zusammenfassen bei anderer Artnr, HauptFA-
    Grenze, Maße-Invariante gesetzt/nicht gesetzt, Spaltenfilter auf Summe, Gruppen-Paging) sind gruen.
11. Liste, Druck und PDF der Einzelsicht bleiben unveraendert (Regression TS-60/TS-74).

## Test-Szenarien

Kapitel TS-60 (Beschichtungsauftrag) um Unterkapitel „Summiert" erweitern oder neues Kapitel (naechste
freie Nummer zum Umsetzungszeitpunkt):
- **Vorbedingung/Pruefschritt (Sage, vor Abnahme, Ergebnis im Kapitel notieren):** auf der Sage-DB IDEAL
  `EXEC sp_helptext 'vw_IDEAL-AKE_Kommissionierung_FAListe';` — welche Tabelle liefert `Sollmenge` und gibt
  es dort eine eigene Positionseinheit? Danach
  `SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '<Positionstabelle>' AND COLUMN_NAME LIKE '%einheit%';`
  Treffer ⇒ melden (Einheit gehoert in den Schluessel), nicht still uebergehen. Bis dahin gilt AK 2 unter
  dieser Bedingung (wie `fallstricke.md` §18).
- Navigation „Beschichtungsauftrag" → summierte Sicht; Umschalter → Einzelliste und zurueck.
- Happy Path: HauptFA mit mehrfach vorkommendem gleichem Teil → eine Summenzeile mit Maßen.
- Negativfall: verschiedene Artnr im selben HauptFA → zwei Zeilen.
- Invariante: gleiche Artnr, abweichendes Mass (Testdaten suchen:
  `SELECT HauptFA, Artnr FROM dbo.FaHierarchyNodes WHERE Beschichtet = 1 GROUP BY HauptFA, Artnr HAVING COUNT(DISTINCT CONCAT(Breite,'|',Hoehe,'|',Tiefe)) > 1;`
  — leer ⇒ Fall im Ist-Bestand nicht vorhanden, Ergebnis notieren) → eine Zeile + „Maße uneinheitlich" + Log.
- ~~Negativfall: gleiche Artnr, abweichendes Mass → zwei Zeilen (falls Masse im Schluessel).~~ *(verworfen mit
  Antwort 1)*
- Kombigeraet (falls Testdaten vorhanden): alle Kopfvarianten + „mehrdeutig".
- Spaltenfilter auf Summe; Umschalter traegt Filter mit.
- Leerfall: Filter ohne Treffer → Hinweis, Druck → Hinweis statt leerem Blatt.
- Druck/PDF summiert, PDF ohne Edge → Knopf fehlt.
- Zugriff: ohne Rolle `beschichtungsauftrag`/`admin` → AccessDenied; Toggle aus → gesperrt.
- Regression Einzelliste/Druck/PDF.

## Etappen (nur bei epic: true)

Nicht zutreffend (`epic: false`).

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| 1 | — | — | — |

## Deploy

*Vorlaeufig — der QA-Lauf bestaetigt gegen den echten Diff.*

- **Web-App:** ja (Controller, Service, Views).
- **Service:** nein (nur Versions-Bump in `AppVersion.cs`).
- **Migration:** nein.
- **Publish-Befehle:** `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`

## Offene Rueckfragen

*Alle beantwortet (Freigabe-Antworten 1 und 2, 2026-10-07); Entscheidungen sind in den Rumpf eingearbeitet.*

1. **Summierschluessel (Notiz Punkt 1):** HauptFA + Artikelnummer + Masse (Breite/Hoehe/Tiefe)? Ist RAL Teil
   des Schluessels? Summiert wird nur, was fachlich gleich beschichtet wird.
   *Beantwortet mit Antwort 1: `HauptFA + Artnr`, Maße nur Anzeige.*
   *Empfehlung:* ~~`HauptFA + Artnr + Breite + Hoehe + Tiefe`~~ *(verworfen mit Antwort 1)*. RAL **nicht** in den Schluessel: es liegt nur am
   Auftragskopf (`FaHierarchyOrderInfo.cs:28`), nicht an der Position, und ist bei HauptFA im Schluessel
   ohnehin konstant (Kombigeraet bleibt als mehrdeutig gekennzeichnet). Zu beachten: „Mass fehlt" und
   „Mass 0" sind dieselbe Gruppe (`FaHierarchySyncService.cs:302`).
2. **Umfang der summierten Sicht:** Bleibt die Einzelliste Standardansicht, und bekommt die summierte Sicht
   Druck + PDF?
   *Beantwortet mit Antwort 2: Summiert ist Standard, mit Druck und PDF.*
   *Empfehlung:* ~~Einzelliste bleibt Standard — sie ist die Vorschlagsliste, aus der Teil 4 Positionen
   waehlt.~~ *(verworfen mit Antwort 2; Teil 4 waehlt ohnehin keine Einzelpositionen mehr, Teil-4-Antwort 2.)*
   Druck + PDF der summierten Sicht ja (Muster `PrintSummiert`/`PdfSummiert` der Kommissionierliste
   vorhanden, Aufwand klein). ~~Anders als bei der Kommissionierliste (dort Summiert = Standard seit v1.47.0).~~
   *(verworfen mit Antwort 2 — jetzt wie dort.)*

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. Summierschluessel → **HauptFA + gleiche Artikelnummer (Keyuser 2026-10-07). Maße nicht genannt – Auslegung: Maße hängen an der Artikelnummer und sind kein eigener Schlüsselteil (zu bestätigen).** **Nachtrag (Gerald 2026-10-07): Schlüssel HauptFA + Artikelnummer; Artikelnummer/Matchcode sind eindeutig, damit sind auch die Maße gleich. Maße werden mit angedruckt (Anzeige, kein Schlüsselteil).**
2. Standardansicht / Druck + PDF der summierten Sicht → **Summierte Ansicht ist Standard (Keyuser 2026-10-07) (abweichend vom Vorschlag), mit Druck und PDF.**
