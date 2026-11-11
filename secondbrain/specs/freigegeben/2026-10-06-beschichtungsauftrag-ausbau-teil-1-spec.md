---
type: spec
title: "Beschichtungsauftrag-Ausbau Teil 1 — Summierte Ansicht"
slug: 2026-10-06-beschichtungsauftrag-ausbau-teil-1-spec
status: Testbereit
created: 2026-10-06
updated: 2026-10-09
source_backlog: "[[2026-10-06-beschichtungsauftrag-ausbau]]"
depends_on: ""
task: "[[2026-10-09-beschichtungsauftrag-ausbau-teil-1-umsetzung]]"
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
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
freigabe_entscheidung: "Freigegeben wie Antworten 1-2: Schluessel HauptFA + Artnr, Masse nur Anzeige mit Warnung; Summiert ist Standard mit Druck und PDF. Umsetzung aus dem Buendel-Zweig."
freigabe_von: "Gerald Weichbold"
freigabe_am: 2026-10-09
---

> **Umsetzungsort (Entscheidung Freigabe 2026-10-09):** eigener Worktree, abgezweigt vom Buendel-Zweig
> `feature/2026-08-07-ideal-teile-1-5` (Stand v1.47.5), weil die Beschichtungsansicht nur im Buendel existiert.
> Merge erst nach Schranke 2 des Buendels. TS-Nummer und Version: naechste freie (voraussichtlich 1.48.0). Code-Belege dieser Spec
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

*Final (qa-agent 2026-10-09, gegen `git diff f102fbf5..HEAD` = 25 Dateien).*

- **Web-App:** ja — `IdealAkeWms/` Controller (`FaHierarchyBeschichtungController`, `FaHierarchyKommissionierListenController`
  Refactor), Services (`BeschichtungsauftragService`, `KommissionierListenService`, neu `PrintVisibleColumns`), ViewModels,
  `ColumnDefinitions`, Views (`FaHierarchyBeschichtung/*`, `_Layout`, `RoleOverview`, `Help/*`), `AppVersion.cs`.
- **Service:** nein — unter `IDEALAKEWMSService/` aendert sich nur die Versionskonstante `AppVersion.cs` (1.47.5 → 1.48.0),
  kein Service-Code. Wie bei v1.47.0 kein Service-Publish noetig; wer die Versionsanzeige des Service angleichen will,
  publiziert ihn optional mit.
- **Migration:** nein — keine Datei unter `*/Migrations/`, kein `SQL/`, kein `SQL/Einmalig/`, `SQL/AgentJobs/` unberuehrt.
- **Publish (aus dem Worktree, Testsystem → Test → Merge):**
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
- **Reihenfolge:** nur Web-Publish, kein DB-Schritt, kein Backup-Zwang (reine Lesefunktion).
- **Nach dem Merge:** nur dann erneut aus `main` publizieren, wenn der Merge die getesteten Dateien mit parallelen
  `main`-/Buendel-Aenderungen kombiniert hat (Zweig sitzt auf `feature/2026-08-07-ideal-teile-1-5` @ `f102fbf5`; Merge erst
  nach Schranke 2 des Buendels).

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

## Umsetzung (Dev-Lauf 2026-10-09)

Worktree `.claude/worktrees/2026-10-06-beschichtungsauftrag-ausbau-teil-1`, Zweig
`feature/2026-10-06-beschichtungsauftrag-ausbau-teil-1`, abgezweigt von `feature/2026-08-07-ideal-teile-1-5` @ `f102fbf5`
(v1.47.5). Plan: `docs/superpowers/plans/2026-10-09-beschichtungsauftrag-ausbau-teil-1.md` (im Worktree).
Commits `850258e3` (Aggregation), `578aec6b` (Controller), `c226a465` (Views), `8d9cefd2` (feature-complete, v1.48.0).

**Was gebaut ist (Abweichungen vom Lösungsentwurf begründet):**
- `BeschichtungsauftragService.AggregateSummiert` (statisch, DB-frei), `SummiertColumnMap`, `BuildSummiertAsync` (Log-Warnung
  „Masse uneinheitlich" je Seitenzeile + Mehrdeutigkeit je HauptFA), `ToggleSharedQueryKeys`/`BuildToggleQuery`.
  Die Kopfzeilen kommen im Ergebnis mit (`OrderInfos` je HauptFA der Seite) — die Kommissionier-Summiert-Vorlage hat
  keinen Kopf, Anforderung 4 verlangt ihn.
- Erste Position = Sortierung nach `Position`, dann `SubFA` (deterministisch; die Spec sagt nur „erste").
- Zusätzliche Spalte **„Positionen"** (Anforderung 2 „Zahl der aggregierten Positionen wird angezeigt"), filterbar.
- Hinweis „Maße uneinheitlich" in der **Matchcode-Zelle**: Matchcode ist gesperrt (nie ausblendbar), damit ist der
  Hinweis am Bildschirm und im Druck garantiert sichtbar, auch wenn Maß-Spalten ausgeblendet sind.
- Sichtbare Druckspalten: die Kommissionier-Variante passte ohne Anpassung; sie liegt jetzt als gemeinsamer Helfer
  `Services/PrintVisibleColumns.cs`, der Kommissionier-Controller delegiert (Verhalten unverändert, seine 9 Controller-Tests grün).
- Kopf-Markup als Partials `_BeschichtungKopf.cshtml` / `_BeschichtungKopfDruck.cshtml`, verbatim aus `Index.cshtml` /
  `Print.cshtml` herausgelöst und dort eingebunden (AK 11: gleiche Ausgabe; Regression TS-81.14).
- Zahnrad-Host direkt vor `.table-responsive` in `Summiert.cshtml` (fallstricke §19), Wächter
  `FaHierarchyBeschichtungSummiertZahnradHostTests`; Drift-Wächter `KommissionierSpaltenKonsistenzTests` um die
  Beschichtung-Summiert-Views und `SummiertColumnMap` erweitert.
- Navigation `_Layout.cshtml` → `Summiert`; `RoleOverview.cshtml` nennt die neue Standardsicht (Rolle/Filter unverändert).
- Version 1.48.0, Anwender-Changelog, Hilfe (`Help/Index.cshtml`, Abschnitt „seit v1.48.0"), TS-81.

**Nachweis (Dev-Lauf, vor QA):**
```
dotnet build IdealAkeWms.slnx         -> 0 Fehler
dotnet test IdealAkeWms.Tests         -> Bestanden! Fehler: 0, erfolgreich: 1473, übersprungen: 1, gesamt: 1474
dotnet test IDEALAKEWMSService.Tests  -> Bestanden! Fehler: 0, erfolgreich: 272, gesamt: 272
```
Neu: 12 Aggregations-/Log-Tests, 5 Controller-Tests, 1 Zahnrad-Wächter, 1 Drift-Wächter-Fall (Basis vorher 1454).
Rot-vor-Grün belegt für Aggregation (Kompilierfehler), Controller (Kompilierfehler) und beide View-Wächter (FAIL ohne View).
`docs/TESTSZENARIEN.md` im Index-Blob byte-genau LF (fallstricke §21).

**Zusatzfunde (gemeldet, nicht mitgebaut):**
1. **Leere Artnr wird je HauptFA zusammengefasst** — gleiche Regel wie `KommissionierListenService.AggregateSummiert`
   (`(n.Artnr ?? "").Trim()` als Schlüssel). Positionen ohne Artikelnummer landen in EINER Zeile; die Maße-Warnung schlägt
   dann meist an. Test `Summiert_EmptyArtnr_IsMergedPerHauptFa_LikeKommissionierliste` pinnt das Verhalten. Prüfabfrage:
   `SELECT HauptFA, COUNT(*) FROM dbo.FaHierarchyNodes WHERE Beschichtet = 1 AND (Artnr IS NULL OR LTRIM(RTRIM(Artnr)) = '') GROUP BY HauptFA;`
   — Treffer ⇒ fachlich entscheiden (für beide Summierungen).
2. **Spaltenfilter: Komma ist ODER** (`ColumnFilterHelper.Parse`) — ein Filter „5,000" auf eine Mengenspalte sucht
   „5" ODER „000". Vorbestehend für alle Dezimalspalten; in TS-81.8 als Bedienhinweis vermerkt.
3. **Vorbestehende Drift** `Views/FaHierarchyBeschichtung/Index.cshtml` `#view-config` `supportsSortDefault: true` vs.
   `ColumnDefinitions.FaHierarchyBeschichtung` `SupportsSortDefault: false` — nicht angefasst (AK 11).

## QA-Nachweis (qa-agent, 2026-10-09)

Synchroner Lauf im Worktree `.claude/worktrees/2026-10-06-beschichtungsauftrag-ausbau-teil-1`, Zweig
`feature/2026-10-06-beschichtungsauftrag-ausbau-teil-1`, HEAD `8d9cefd2`, Diff `f102fbf5..HEAD` (4 Commits, 25 Dateien,
+1243/-108). Worktree sauber (`git status` leer), kein `secondbrain/` im Worktree (Sparse-Checkout greift).

### 1. Build und Tests (selbst ausgefuehrt)

```
dotnet build IdealAkeWms.slnx   -> 0 Fehler, 12 Warnungen (alle vorbestehend: NU1902 MailKit/MimeKit, CS8602 TrackingController:332,
                                   CS8321 FormatDateWithKw in 3 Views; keine Warnung in einer geaenderten Datei)
dotnet test --no-build
  IDEALAKEWMSService.Tests.dll  -> Bestanden! Fehler: 0, erfolgreich: 272, uebersprungen: 0, gesamt: 272
  IdealAkeWms.Tests.dll         -> Bestanden! Fehler: 0, erfolgreich: 1473, uebersprungen: 1, gesamt: 1474
```
Deckt sich mit dem Dev-Nachweis.

### 2. Code-Review (Befunde)

**Critical:** keine. **Important:** keine.

**Minor (gemeldet, keine Blocker, nicht behoben — kein Code-Auftrag an QA):**
- **M1 — Log-Rauschen bei PDF/Druck.** `PdfSummiert` (`FaHierarchyBeschichtungController.cs`, Action `PdfSummiert`) ruft
  `BuildSummiertAsync(..., paginate: false)` fuer die ganze Menge auf und filtert erst danach auf den einen HauptFA; die
  Warnungen „Masse uneinheitlich"/„Kopfzeilen" in `BeschichtungsauftragService.BuildSummiertAsync` werden dabei fuer
  ALLE HauptFA geschrieben, ebenso bei jedem Seitenaufruf erneut. Kein Fehlverhalten, nur Log-Volumen (gleiches Muster
  wie `LogAmbiguities` der Liste).
- **M2 — Locked-Spalten im Druck nur clientseitig garantiert.** `PrintVisibleColumns.ResolveAsync`
  (`Services/PrintVisibleColumns.cs`) uebernimmt einen expliziten `visibleColumns`-Parameter ungeprueft; nur der
  Praeferenz-Rueckfall erzwingt `Locked`. Ein von Hand gebauter Link ohne `matchcode` druckt deshalb ohne den Hinweis
  „Maße uneinheitlich". Normalbedienung unkritisch (`print-visible-columns.js` sammelt nur sichtbare `<th>`, Locked-Spalten
  sind im Zahnrad nicht ausblendbar). Vorbestehendes Verhalten der Kommissionier-Variante, unveraendert uebernommen.
  Folgearbeit falls gewuenscht: Locked-Keys im expliziten Zweig immer ergaenzen.
- **M3 — Testluecke AK 8.** Die Schluesselliste `BeschichtungsauftragService.ToggleSharedQueryKeys` ist durch keinen Test
  gepinnt (der Mechanismus `BuildToggleQuery(request, keys)` ist ueber
  `KommissionierListenServiceTests.BuildToggleQuery_KeepsSharedKeys_DropsSollmengeAndListOnly` abgedeckt). Inhaltlich
  geprueft: 7 Keys, kein `colf_sollmenge`, kein `colf_positionen`/`colf_beschichtet` — korrekt. AK 8 bleibt Manual-UAT.
- **M4 — Gross-/Kleinschreibung der Artnr.** Gruppierung `(HauptFA, Artnr.Trim())` ist case-sensitiv, die Sortierung
  `OrdinalIgnoreCase` — „a1"/„A1" waeren zwei Zeilen. Identisch zu `KommissionierListenService.AggregateSummiert`; nur
  relevant, falls Sage gemischte Schreibweisen liefert. Zusatzfund 1 des Dev-Laufs (leere Artnr je HauptFA
  zusammengefasst) bestaetigt.

**Was geprueft wurde (ohne Befund):**
- *Aggregation* (`BeschichtungsauftragService.AggregateSummiert`): Schluessel `(HauptFA, Artnr getrimmt)`; Summe nur
  `Sollmenge`; erste Position deterministisch `Position ?? int.MaxValue`, dann `SubFA` (Ordnungsschluessel, kein stiller
  Wertersatz); Masse-Invariante `Any(Breite/Hoehe/Tiefe != first)` — `decimal`-Gleichheit ist skalenunabhaengig, NULL=0
  ist durch den nicht-nullbaren Typ gegeben (`FaHierarchyNode.cs:48-50`); Spaltenfilter auf der Projektion vor dem
  Gruppen-Paging; `TotalGroupCount` zaehlt Gruppen; `OrderInfos` nur fuer HauptFA der Seite (Test
  `Summiert_OrderInfos_OnlyForHauptFaOnPage_AndMissingHeadIsEmpty`); `SummiertColumnMap`-Getter = Zelltext der View
  (gleiche `FormatMass`/`FormatMenge`, eine Regel an einer Stelle).
- *Logging:* Warnung mit HauptFA + Artnr + Anzahl; Mehrdeutigkeit ueber dieselbe `LogAmbiguity`-Methode wie die Liste
  (Bedingung `Count > 1` = `FaHierarchyListGroupViewModel.IsAmbiguous`, `:30`).
- *Controller:* Zugriff ueber Class-Level `[RequireBeschichtungsauftragAccess]` + `[RequireFaHierarchyBeschichtungAktiv]`
  (`FaHierarchyBeschichtungController.cs:28-29`) gilt fuer alle drei neuen Actions; `PdfSummiert` → `NotFound` bei
  unbekanntem/weggefiltertem HauptFA, `PdfRenderException` → `WarningMessage` + Redirect `Summiert`, Dateiname ueber
  `PdfFileNameBuilder.Build("Beschichtungsauftrag", ...)`; kein `ErrorMessage`.
- *Refactor `PrintVisibleColumns`:* Rumpf zeichengleich aus `FaHierarchyKommissionierListenController` herausgeloest,
  der Controller delegiert mit denselben Argumenten — Kommissionier-Verhalten unveraendert (Controller-Tests gruen).
  `KommissionierListenService.BuildToggleQuery(request)` delegiert an die neue Ueberladung mit derselben Key-Liste.
- *Views/ADR 0005:* `Summiert.cshtml` mit Filterkarte, `data-server-column-filter="true"`, `data-col-key` an allen 9 `<th>`,
  `_Pagination`; `#column-config` = `ColumnDefinitions.FaHierarchyBeschichtungSummiert` (Reihenfolge, Locked, Breiten;
  Drift-Waechter gruen); Zahnrad-Host leer direkt vor `.table-responsive` (§19, Waechter gruen); Umschalter in Index ueber
  der Filterkarte (nicht zwischen Host und Tabelle).
- *AK 11 (Partials):* `_BeschichtungKopf.cshtml` / `_BeschichtungKopfDruck.cshtml` Zeile fuer Zeile gegen den entfernten
  Block aus `Index.cshtml`/`Print.cshtml` verglichen: identisches Markup, `group.IsAmbiguous` → `Model.Count > 1`
  (gleiche Definition), `group.OrderInfos.Count` → `Model.Count`, Modelltyp `List<FaHierarchyOrderInfo>` passt
  (`FaHierarchyListGroupViewModel.cs:27`). Einzige sichtbare Aenderung der Liste ist der von AK 1 verlangte Umschalter.
- *Druck:* CSS eingebettet, Barcode als `data:image/png;base64`, `ShowCol` fuer alle 8 Nicht-HauptFA-Spalten, Hinweis
  „Maße uneinheitlich" in der Matchcode-Zelle (Locked), `.ambiguous-banner`-CSS vorhanden, Leerfall-Hinweis.
- *Defektklassen-Liste:* keine Werte in `<script>` eingebettet (beide JSON-Bloecke statisch); kein `hidden` mit `d-*`;
  keine stillen `?? 0`-Fallbacks (`GetValueOrDefault(...) ?? new List` fuehrt sichtbar zu „Keine Auftrags-Kopfdaten");
  keine Parallel-Array-Bindung; keine reine Browser-Validierung (nur M2); keine driftende Mehrfachregel.
- *WCAG/Text:* Badge `bg-warning text-dark` (Schwarz auf #ffc107, deutlich ueber 4,5:1), Druck #7a5900 auf #fff8e1
  (ueber 4,5:1), beide Hinweise als Text, nicht nur Farbe.
- *Navigation:* `_Layout.cshtml:118` → `Summiert`, Active-Vergleich per Controller unveraendert gueltig;
  `RoleOverview.cshtml` nur Text (Rolle/Filter unveraendert, keine Drei-Stellen-Pflicht ausgeloest).

### 3. Beweisart je Akzeptanzkriterium

| AK | Beweis | Nachweis |
|---|---|---|
| 1 Navigation + Umschalter | Manual-UAT | TS-81.2 (Markup/Routing nicht automatisiert) |
| 2 gleiche Artnr → eine Zeile, Summe, Anzahl 2 | automatisiert + UAT | `Summiert_SameHauptFaAndArtnr_IsOneRow_WithSumAndPositionCount`; TS-81.3 (Sage-Pruefschritt TS-81.1 offen) |
| 3 andere Artnr → zwei Zeilen | automatisiert | `Summiert_DifferentArtnr_StaysTwoRows`; TS-81.4 |
| 4 nie ueber HauptFA zusammenfassen | automatisiert | `Summiert_DifferentHauptFa_NeverMerged`; TS-81.4 |
| 4a Masse + Hinweis + Log, nicht aufgeteilt | teilweise automatisiert | Flag/erste Position: `Summiert_DimensionDiffersInOneAxis_FlagsAndKeepsFirstPositionValues`; Log: `BuildSummiertAsync_LogsWarning_ForInconsistentDimensions_AndAmbiguousHead`; sichtbarer Text Bildschirm/Druck: **Manual-UAT TS-81.5** |
| 5 nur Beschichtet, alle Ebenen | automatisiert | `Summiert_OnlyBeschichtet_AllLevelsCount`; TS-81.6 |
| 6 Kopf + mehrdeutig + Log | teilweise automatisiert | Daten: `Summiert_OrderInfos_OnlyForHauptFaOnPage_AndMissingHeadIsEmpty`; Log: `BuildSummiertAsync_LogsWarning_*`; Darstellung: **Manual-UAT TS-81.7** |
| 7 ADR 0005, Gruppen-Paging, Filter auf Summe | teilweise automatisiert | `Summiert_GroupPaging_CountsHauptFaGroups`, `Summiert_PaginationCountsGroups`, `Summiert_ColumnFilterOnSollmenge_FiltersTheSum`, `Summiert_ColumnFilterOnPositionen_FiltersTheCount`, Drift-Waechter `KommissionierSpaltenKonsistenzTests`; Server-Filter ueber echte Query/JS: **Manual-UAT TS-81.8/81.9** |
| 8 Umschalter traegt Filter, ohne `colf_sollmenge` | Manual-UAT | TS-81.8 (Key-Liste per Review geprueft, kein Test — M3) |
| 9 Druck = Bildschirm, PDF-Dateiname, Fehler-Rueckweg | teilweise automatisiert | `PdfSummiert_Success_FileNameStartsWithBeschichtungsauftrag`, `PdfSummiert_RenderFails_RedirectsToSummiertWithWarning`, `PdfSummiert_UnknownOrFilteredOutHauptFa_NotFound`, `PrintSummiert_UsesSummiertViewKey_ForSavedColumns`; Druckinhalt/Edge-PDF: **Manual-UAT TS-81.10/81.11/81.12** |
| 10 Unit-Tests gruen | automatisiert | 12 Service- + 5 Controller-Tests gruen (Lauf oben) |
| 11 Einzelsicht unveraendert | Manual-UAT | TS-81.14 (Partial-Gleichheit per Review belegt, kein Render-Test) |

Nicht gruen gezaehlt: alles als Manual-UAT Markierte (Razor-Rendering, Routing, Browser-JS, Headless-Edge-PDF,
Sage-Pruefschritt TS-81.1).

### 4. Geaenderte Bestandstests

- `IdealAkeWms.Tests/Views/KommissionierSpaltenKonsistenzTests.cs` — **Struktur-/Typ-Aenderung**: `ViewsDir` → `ViewsRoot`
  plus Ordner-Parameter in `ViewPairs`/Theory-Signatur; die zwei Bestandsfaelle zeigen auf dieselben Dateien wie vorher
  (`FaHierarchyKommissionierListen/...`), Assertions unveraendert; je ein neuer Fall fuer Beschichtung summiert
  (View-Paar und `SummiertColumnMap`). Kein Verhalten aufgeweicht.
- `IdealAkeWms.Tests/Views/BomZahnradHostTests.cs` — **rein additiv**: neue Klasse
  `FaHierarchyBeschichtungSummiertZahnradHostTests` nutzt die vorhandenen statischen Helfer; Bestandstests unberuehrt.
- `IdealAkeWms.Tests/Services/BeschichtungsauftragServiceTests.cs` — **rein additiv** (neue `Summiert_*`-Tests, Helfer
  `SNode`/`Aggregate`); keine Bestandszeile geaendert.
- Neu: `IdealAkeWms.Tests/Controllers/FaHierarchyBeschichtungControllerTests.cs`.
Kategorie „Sonstiges" (Fixture gedreht, bis gruen): keine.

### 5. Checkliste CLAUDE.md und Testszenarien

- Migration/SQL/FreshInstall/AgentJobs: nicht betroffen (kein Schema). Audit-Felder: nicht betroffen (reine Lesefunktion).
- Version: beide `AppVersion.cs` 1.47.5 → 1.48.0 (2026-10-09); Anwender-Changelog `Views/Help/Changelog.cshtml` + Hilfe
  `Views/Help/Index.cshtml` ergaenzt; Brain-Changelog [[2026-10-09-v1-48-0-beschichtungsauftrag-summiert]] vorhanden.
- `docs/TESTSZENARIEN.md` TS-81.1–81.14: ohne Rueckfragen abarbeitbar (Vorbedingungen, Testdaten-Abfragen, erwartetes
  Verhalten, Negativ- und Leerfall, Zugriff, Regression); Ergebnisfelder fuer TS-81.1/81.5 vorhanden.
- Zeilenenden byte-genau (python ueber `git show`): Index-Blob und HEAD-Blob je 500963 Byte, `\r\n` = 0, `\n` = 9341;
  Basis `f102fbf5` ebenfalls 0 CRLF — Datei bleibt durchgaengig LF (fallstricke §21), Arbeitskopie identisch.
- `secondbrain/tests/testszenarien-index.md:107` Eintrag 81 vorhanden, verweist auf TS-81.1–81.14.

### 6. Manuelle Testcheckliste (Schranke 2)

1. Web aus dem Worktree publizieren (Deploy-Abschnitt), Testsystem IDEAL, Toggle `FaHierarchyBeschichtungAktiv` an.
2. TS-81.1 Sage-Pruefschritt Mengeneinheit ausfuehren und Ergebnis notieren (vor Abnahme von AK 2).
3. TS-81.2 Menue → `/FaHierarchyBeschichtung/Summiert`, Umschalter hin und zurueck.
4. TS-81.3/81.4 Summenzeile gegen die Einzelliste nachrechnen; andere Artnr bzw. anderer HauptFA bleiben getrennt.
5. TS-81.5 Abfrage auf uneinheitliche Masse; bei Treffer Hinweis am Bildschirm, im Druck, im PDF und Log-Warnung pruefen.
6. TS-81.6/81.7 SubFA-Position zaehlt mit; Kopf, Kombigeraet „mehrdeutig", HauptFA ohne Kopf.
7. TS-81.8/81.9 Paging nach Gruppen, Filter auf Summe, Umschalter nimmt Artnr-Filter mit, Sollmengen-Filter nicht; Zahnrad.
8. TS-81.10–81.12 Druck, PDF (Dateiname), PDF-Fehlerpfad, Leerfall.
9. TS-81.13 Zugriff ohne Rolle und mit Toggle aus.
10. TS-81.14 Regression Einzelliste, Druck und PDF (TS-60/TS-74).

**Ergebnis:** Build und Tests gruen, keine offenen Critical/Important-Befunde → `status: Testbereit`. Merge und Push
bleiben Schranke 2 (Mensch); Merge erst nach Schranke 2 des Buendels `feature/2026-08-07-ideal-teile-1-5`.

## Nachzug nach QA (Dev, 2026-10-09)

- **M3 behoben:** Test `BeschichtungsauftragServiceTests.Summiert_ToggleQuery_CarriesSharedFilters_ButNotSollmenge`
  pinnt die Umschalter-Query (gemeinsame Filter ja, `colf_sollmenge`/`colf_positionen`/`page` nein) — AK 8 ist damit
  fuer die Query automatisiert, die Klick-Navigation bleibt Manual-UAT (TS-81.8). Commit `cc86d373`.
  `dotnet test IdealAkeWms.Tests` -> Bestanden! Fehler: 0, erfolgreich: 1474, uebersprungen: 1, gesamt: 1475.
- **M1, M2, M4 nicht mitgebaut** (Log-Volumen bei Druck/PDF ueber die ganze Menge; explizites `visibleColumns` ohne
  Locked-Ergaenzung — geerbt aus der Kommissionierliste; Artnr-Gruppierung case-sensitiv wie dort). Kandidaten fuer eine
  gemeinsame Nachlese beider Summierungen, zusammen mit Zusatzfund 1 (leere Artnr).

## Nachtrag: ins Buendel uebernommen (2026-10-09)

2026-10-09 per Fast-Forward ins Buendel uebernommen (`cc86d373`): `git merge --ff-only feature/2026-10-06-beschichtungsauftrag-ausbau-teil-1` im Buendel-Worktree,
HEAD `f102fbf5` -> `cc86d373`. Danach im Buendel: `dotnet build IdealAkeWms.slnx` 0 Fehler; Web-Tests 1474 gruen,
1 uebersprungen; Service-Tests 272 gruen; AppVersion 1.48.0. **Status bleibt Testbereit** — Schranke 2 (Manual-UAT TS-81)
und Merge nach `main` gehen mit dem ganzen Buendel. Zweig/Worktree `…-teil-1` bestehen weiter (Aufraeumen erst auf Freigabe).
