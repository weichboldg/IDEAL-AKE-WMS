---
type: spec
title: "Beschichtungsauftrag-Ausbau Teil 1 — Summierte Ansicht"
slug: 2026-10-06-beschichtungsauftrag-ausbau-teil-1-spec
status: Entwurf
created: 2026-10-06
updated: 2026-10-06
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
  - IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs (neuer ViewConfig FaHierarchyBeschichtungSummiert + Eintrag im Switch bei :479-480)
  - IdealAkeWms.Tests/Services/BeschichtungsauftragServiceTests.cs (Aggregations-Tests)
  - IdealAkeWms/Views/Help/ (Hilfeseite Beschichtungsauftrag ergaenzen) + Views/Help/Changelog.cshtml + beide AppVersion.cs
  - docs/TESTSZENARIEN.md (Kapitel TS-60 erweitern oder neues Kapitel, naechste freie Nummer)
  - secondbrain/tests/testszenarien-index.md
open_questions:
  - "Summierschluessel (Notiz Punkt 1): HauptFA + Artnr + Breite/Hoehe/Tiefe? Ist RAL Teil des Schluessels? Empfehlung: HauptFA + Artnr + Breite + Hoehe + Tiefe; RAL NICHT im Schluessel, weil RAL nur am Auftragskopf (FaHierarchyOrderInfo) liegt und je HauptFA konstant ist (ausser Kombigeraet, das bleibt als mehrdeutig gekennzeichnet)."
  - "Umfang der summierten Sicht: bleibt die Einzelliste die Standardansicht (Vorschlagsliste fuer Teil 4) und bekommt die summierte Sicht Druck + PDF wie die Kommissionierliste (v1.47.0)?"
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
- Aggregation der heutigen gefilterten Menge (Flag `Beschichtet == true`, alle Ebenen,
  `leafOnly: false` wie `BeschichtungsauftragService.cs:105`) nach dem Summierschluessel aus Rueckfrage 1.
- Kopfdaten je HauptFA (Dienstleister, RAL, Termine, Mehrdeutigkeits-Kennzeichnung) wie in der Liste.
- Druck und PDF der summierten Sicht — **nur falls Rueckfrage 2 das bejaht**.

**Out-of-Scope**
- Speichern, Nummer, Status — das ist Teil 3.
- Briefkopf/Corporate Design — Teil 2.
- Trennung nach Montage-Abteilung bei Kombigeraeten ([[2026-08-06-kombinationsgeraete-montageabteilung]]).
- KW-Filter (die Kommissionier-Summiert-Sicht hat einen ueber `KO_Termin`; fuer den Beschichtungsauftrag
  nicht angefordert).

## Fachliche Anforderungen

1. **Summierschluessel** laut Antwort auf Rueckfrage 1. Belegte Ausgangslage fuer die Entscheidung:
   - **RAL liegt nicht auf der Position.** `FaHierarchyNode` hat kein RAL-Feld (`FaHierarchyNode.cs:13-63`).
     RAL steht nur in `FaHierarchyOrderInfo.RAL` (`FaHierarchyOrderInfo.cs:28`), geschrieben ausschliesslich
     vom Sync aus der Sage-View FAInfos (`FaHierarchySyncService.cs:283`, Quelle `i.[RAL]` in
     `FaHierarchySql.cs:139`); es gibt keinen WMS-Schreibpfad (Grep `\bRAL\s*=` in `*.cs`/`*.cshtml`:
     nur Sync-Zeile 283 und Migration; die Views lesen nur). Ist `HauptFA` im Schluessel, ist RAL je Gruppe konstant — ausser beim Kombigeraet
     (mehrere OrderInfo-Zeilen, `FaHierarchyListGroupViewModel.cs:30`), das bereits als mehrdeutig
     gekennzeichnet wird.
   - **Masse: `NULL` und `0` sind nicht unterscheidbar.** `Breite`/`Hoehe`/`Tiefe` sind nicht-nullable
     `decimal` (`FaHierarchyNode.cs:48-50`); der Sync mappt `NULL` aus Sage auf `0`
     (`FaHierarchySyncService.cs:302`, `Dec`). Ein Schluessel mit Massen fasst „Mass fehlt" und „Mass 0"
     zusammen.
   - **Mengeneinheit:** `FaHierarchyNode` hat keine Einheit — dieselbe Lage und dieselbe Belegpflicht wie
     bei der Kommissionierliste (`fallstricke.md` §18). Solange `Artnr` im Schluessel steht, gilt die
     dortige Begruendung (Einheit ist Artikeleigenschaft).
   - **Bedeutung von `Sollmenge`** (Gesamtbedarf je HauptFA oder Menge je Vater-Einheit) ist am Code nicht
     belegbar. Teil 1 uebernimmt dieselbe Annahme wie die Kommissionier-Summierung
     (`KommissionierListenService.cs:180`, Summe der `Sollmenge`), prueft sie aber nicht neu — siehe
     Pruefschritt in den Test-Szenarien.
2. Summiert wird nur `Sollmenge` (wie Kommissionierliste). Matchcode und HauptArtnr werden als
   Eigenschaften der ersten Zeile mitgefuehrt (Muster `KommissionierListenService.cs:182-184`); zusaetzlich
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

## Technischer Loesungsentwurf

Reine Wiederverwendung des Kommissionier-Musters (ponytail: Sprosse 2):
- `BeschichtungsauftragService.AggregateSummiert(allNodes, allInfos, columnFilters, page, pageSize, paginate)`
  — statisch und DB-frei, gleiche Reihenfolge wie `KommissionierListenService.AggregateSummiert`
  (`:147-211`): Flag-Praedikat `FlagPredicate` (`BeschichtungsauftragService.cs:85`) → GroupBy Schluessel →
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
  `colf_hauptartnr`, Mass-Spalten falls im Schluessel), **ohne** `colf_sollmenge` (Einzel- vs. Summenwert —
  Begruendung `KommissionierListenService.cs:213-216`).
- ADR 0005: Summiert.cshtml mit `data-server-column-filter="true"`, `data-col-key` an jedem `<th>`,
  `_Pagination`-Partial, Filterkarte (Text wie `Index.cshtml:25-32`). Neue Spalten **gleichzeitig** in
  `<script id="column-config">` und `ColumnDefinitions` eintragen (`fallstricke.md`, Abschnitt
  Spalten-Praeferenzen, Zeilen 960-966).
- Druck/PDF (falls Rueckfrage 2 = ja): PrintSummiert.cshtml mit eingebettetem CSS und Barcode als Data-URI
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

1. In `/FaHierarchyBeschichtung` gibt es einen Umschalter „Liste | Summiert"; „Summiert" oeffnet
   `/FaHierarchyBeschichtung/Summiert`.
2. Zwei Positionen desselben HauptFA mit gleichem Summierschluessel (laut Antwort 1) erscheinen in der
   summierten Sicht als **eine** Zeile mit der Summe der `Sollmenge` und Positionsanzahl 2.
3. Zwei Positionen, die sich in genau einem Schluesselfeld unterscheiden, bleiben zwei Zeilen.
4. Positionen verschiedener HauptFA werden nie zusammengefasst.
5. Nur `Beschichtet == true` geht ein; Positionen aller Ebenen (auch `SubFA != 0`) zaehlen mit.
6. Der Kopf jeder Gruppe zeigt Dienstleister/RAL/Termine wie die Liste; bei mehreren OrderInfo-Zeilen
   alle Varianten + Badge „mehrdeutig" + `ILogger`-Warnung.
7. ADR 0005: Pagination nach Gruppen, Filterkarte, Server-Spaltenfilter an jeder Spalte; Spaltenfilter
   auf „Sollmenge" filtert die **Summe**.
8. Der Umschalter traegt die gemeinsamen Spaltenfilter mit, `colf_sollmenge` nicht.
9. Falls Rueckfrage 2 = ja: Druck der summierten Sicht zeigt dieselben Zeilen wie der Bildschirm (ohne
   Paging), PDF je HauptFA liefert `Beschichtungsauftrag_<HauptFA>_<yyyyMMdd-HHmm>.pdf`; PDF-Fehler fuehrt
   mit WarningMessage zurueck auf `Summiert`.
10. Unit-Tests fuer `AggregateSummiert` (Zusammenfassen, Nicht-Zusammenfassen je Schluesselfeld, HauptFA-
    Grenze, Spaltenfilter auf Summe, Gruppen-Paging) sind gruen.
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
- Happy Path: HauptFA mit mehrfach vorkommendem gleichem Teil → eine Summenzeile.
- Negativfall: gleiche Artnr, abweichendes Mass → zwei Zeilen (falls Masse im Schluessel).
- Kombigeraet (falls Testdaten vorhanden): alle Kopfvarianten + „mehrdeutig".
- Spaltenfilter auf Summe; Umschalter traegt Filter mit.
- Leerfall: Filter ohne Treffer → Hinweis, Druck → Hinweis statt leerem Blatt.
- Druck/PDF summiert (falls Rueckfrage 2 = ja), PDF ohne Edge → Knopf fehlt.
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

1. **Summierschluessel (Notiz Punkt 1):** HauptFA + Artikelnummer + Masse (Breite/Hoehe/Tiefe)? Ist RAL Teil
   des Schluessels? Summiert wird nur, was fachlich gleich beschichtet wird.
   *Empfehlung:* `HauptFA + Artnr + Breite + Hoehe + Tiefe`. RAL **nicht** in den Schluessel: es liegt nur am
   Auftragskopf (`FaHierarchyOrderInfo.cs:28`), nicht an der Position, und ist bei HauptFA im Schluessel
   ohnehin konstant (Kombigeraet bleibt als mehrdeutig gekennzeichnet). Zu beachten: „Mass fehlt" und
   „Mass 0" sind dieselbe Gruppe (`FaHierarchySyncService.cs:302`).
2. **Umfang der summierten Sicht:** Bleibt die Einzelliste Standardansicht, und bekommt die summierte Sicht
   Druck + PDF?
   *Empfehlung:* Einzelliste bleibt Standard — sie ist die Vorschlagsliste, aus der Teil 4 Positionen
   waehlt. Druck + PDF der summierten Sicht ja (Muster `PrintSummiert`/`PdfSummiert` der Kommissionierliste
   vorhanden, Aufwand klein). Anders als bei der Kommissionierliste (dort Summiert = Standard seit v1.47.0).

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. Summierschluessel →
2. Standardansicht / Druck + PDF der summierten Sicht →
