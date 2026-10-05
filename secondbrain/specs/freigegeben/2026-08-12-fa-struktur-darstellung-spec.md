---
type: spec
title: "FA-Struktur: Darstellung, Spaltenfilter und Knoten-Icons (Nachtrag zu Teil 2)"
slug: 2026-08-12-fa-struktur-darstellung-spec
status: Testbereit
created: 2026-08-12
updated: 2026-08-19
source_backlog: "[[2026-08-12-fa-struktur-darstellung]]"
depends_on: "[[2026-07-29-standort-ideal-teil-2-spec]]"
task: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
affected_code:
  - "IdealAkeWms/Views/FaHierarchy/Index.cshtml (bestehend aus Teil 2 — aktuell NUR im noch nicht gemergten Worktree .claude/worktrees/2026-08-07-ideal-teile-1-5, Branch feature/2026-08-07-ideal-teile-1-5, Status Testbereit; nicht in main. Umbau: Karten-je-Struktur wird zu EINER seitenweiten <table data-view-key=\"FaHierarchyStructure\"> mit einem <tbody> je Struktur, analog Views/Tracking/OseonIndex.cshtml; heutige Kopf-Badges [Kunde/Status/Termine/Abteilung/Waisen-/Mehrdeutig-/Fehler-Badge/Knotenzahl] wandern in eine Struktur-Kopfzeile mit colspan; #view-config/#column-config-Bloecke + Skript-Includes ergaenzt — Entscheidungen zu B-1/B-2, Kritische Pruefung)"
  - "IdealAkeWms/Views/FaHierarchy/_FaHierarchyNode.cshtml (Umbau von rekursivem <div class=\"fa-node\">/.fa-node-children auf rekursiv gerenderte, flache Geschwister-<tr>-Zeilen innerhalb des Struktur-tbody, analog _OseonGroupDetails.cshtml; data-node-id/data-parent-id bleiben erhalten, Elternbezug laeuft nur noch ueber Daten-Attribute statt DOM-Verschachtelung — Entscheidung zu B-2/B-3)"
  - "IdealAkeWms/wwwroot/css/site.css (Kontrast-Fix .fa-structure-header — als eigener, sofortiger ERSTER Commit vorgezogen, siehe Technischer Loesungsentwurf/Entscheidung zu S-4; neue .fa-tree-table-*/.fa-node-context-Klassen, Legende)"
  - "IdealAkeWms/wwwroot/js/fa-hierarchy-tree.js (NEU; kompletter Ersatz des heutigen Inline-Skriptblocks in Index.cshtml — Zeilen 201–274 im aktuellen Worktree-Stand: setExpanded/Toggle-Handler/'Alle auf/zu'/expandAncestors/#faClientFilter-Highlight werden NEU geschrieben, weil sie heute fundamental auf .fa-node-children-DOM-Verschachtelung beruhen, die es im flachen Tabellenzeilen-Modell nicht mehr gibt — plus NEUER Spaltenfilter mit Baum-Semantik inkl. Auswahlfilter-Widget je Spalte. Neuentwicklung, kein kleiner Zusatz — Entscheidung zu B-3)"
  - "IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs (NEU: ViewConfig-Konstante FaHierarchyStructure — Name Vorschlag, Dev-Lauf darf abweichen, sofern kollisionsfrei — + zusaetzlicher case-Zweig im GetByViewKey-Switch, SupportsReorder: true, SupportsSortDefault: false. Ohne diesen Eintrag antwortet UserViewPreferencesApiController mit 400 und Einstellungen gehen bei jedem Reload verloren, derselbe Fehler wie bereits einmal bei FaWorklist — Entscheidung zu B-1, siehe auch [[2026-08-12-listen-spaltenauswahl-spec]] Fachliche Anforderung 2)"
  - "IdealAkeWms/Models/ViewModels/FaHierarchyTreeViewModel.cs (ggf. kleine, rein praesentative Hilfs-Property fuer den Icon-Typ je Knoten — kein neues DB-Feld)"
  - "docs/TESTSZENARIEN.md"
  - "secondbrain/tests/testszenarien-index.md"
  - "GELESEN, NICHT GEAENDERT (Referenz): IdealAkeWms/Views/Tracking/OseonIndex.cshtml, IdealAkeWms/Views/Tracking/_OseonGroupDetails.cshtml, IdealAkeWms/Views/Picking/Bom.cshtml, IdealAkeWms/wwwroot/js/table-filter.js (nur als Muster-Referenz fuer Client-Filter-Zeilen — NICHT auf /FaHierarchy eingebunden, der Baumfilter bleibt ein eigener, lokaler Baustein in fa-hierarchy-tree.js, siehe Freigabe-Antwort 6), IdealAkeWms/wwwroot/js/column-preferences.js (unveraendert, aber ab jetzt aktiv eingebunden), IdealAkeWms/Controllers/Api/UserViewPreferencesApiController.cs (unveraendert, prueft weiterhin GetByViewKey)"
open_questions:
  - "Tatsaechliche Aufloesung der Fertigungsterminals bestaetigen: Der Schwellenwert fuer 'schmaler Bildschirm' ist auf den Bootstrap-Breakpoint lg (992px) festgelegt (Entscheidung zu H-4, Kritische Pruefung). Liegt die reale Terminal-Aufloesung darunter, ist der Schmalfall der Normalfall und die Spalten-Default-Sichtbarkeit (welche Spalten defaultHidden sind) ist im Dev-Lauf entsprechend zu bedefaulten."
epic: false
etappen: []
deploy:
  web: true
  service: false
  migration: false
freigabe_entscheidung: ""
freigabe_von: "Gerald Weichbold"
freigabe_am: 2026-08-12
---

## Ziel / Nutzen (das Warum)

Teil 2 der Standort-IDEAL-Epic ([[2026-07-29-standort-ideal-teil-2-spec]]) hat die FA-Struktur
erstmals sichtbar gemacht — fachlich korrekt, aber in der Darstellung noch roh: freifliessender
Baum ohne ausgerichtete Spalten, ein Kontrastdefekt im Kopfband, kein Spaltenfilter innerhalb einer
Struktur, keine Knotentyp-Icons. Diese Spec ist ein **reiner Präsentations-Nachtrag** — sie ändert
nichts an `FaHierarchyNode`, am Import oder an der server-seitigen Struktur-Filterlogik aus Teil 2.

Der Kontrastpunkt ist **kein Geschmacksthema**: Die Ansicht läuft an Fertigungsterminals, teils bei
schlechtem Licht. Ein Kopfband, dessen Text WCAG AA nicht erreicht, ist ein Defekt mit
Funktionsausfall (Information nicht lesbar), kein Stilfehler.

Das Tree-Table ist ausserdem **Vorbedingung** für den Anschluss dieser Ansicht selbst an die
per-Benutzer-Spaltenauswahl: `column-preferences.js` arbeitet über eine echte `<table>` mit
stabilen Spaltenindizes, die es in `/FaHierarchy` heute nicht gibt. Anders als in einer früheren
Fassung dieser Spec angenommen, ist dieser Anschluss **Teil dieser Spec selbst** — letzter
Abschnitt, siehe Freigabe-Antwort 2 zur Schwester-Spec [[2026-08-12-listen-spaltenauswahl-spec]]:
„wer die Tabelle baut, verdrahtet sie auch". Präzisierung gegenüber der ursprünglichen
Ziel-Formulierung (Entscheidung zu S-1, Kritische Prüfung): Die Schwester-Spec behandelt vier
**bereits bestehende, flache** Listen-Tabellen und fasst die Baumanzeige **nicht** an — das
Tree-Table ist also **nur** Vorbedingung für den eigenen column-prefs-Anschluss dieser Spec, nicht
für die Schwester-Spec. Deren erprobtes Drei-Blöcke-Muster (`view-config`/`column-config`/
Skript-Include, `ColumnDefinitions`-Registrierung) wird hier übernommen, weshalb jene Spec zuerst
umzusetzen ist (Reihenfolge-Hinweis: Freigabe-Antwort 2).

**Wichtiger Fund vorab (Code-Lage).** Der Code aus Teil 2 existiert aktuell **ausschliesslich** im
noch nicht gemergten Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`
(Branch `feature/2026-08-07-ideal-teile-1-5`, Spec-Status laut `feature-map.md`: **Testbereit**,
wartet auf Schranke 2 = Manual-UAT + Merge durch den Menschen). In `main` gibt es aktuell **keine**
`Views/FaHierarchy*`-Dateien. Diese Spec beschreibt den Zielzustand auf Basis des Codes in jenem
Worktree; laut Freigabe-Antwort 1 wird sie als zusätzliche Etappe **im bestehenden**
Epic-Worktree umgesetzt, nicht in einem neuen.

**Umsetzungsreihenfolge innerhalb dieser Spec (Entscheidung zu S-4, Kritische Prüfung):** Der
Kontrast-Fix (Fachliche Anforderungen, Abschnitt 1) ist eigenständig, risikoarm und sofort
lieferbar (ein bis drei CSS-Zeilen) und wird im Dev-Lauf als **erster, separater Commit**
vorgezogen — er ist kein Geisel des grossen Tree-Table-Umbaus. Der Tree-Table-/Filter-/Icon-/
column-prefs-Umbau folgt danach als zweiter, grösserer Block.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope (diese Runde):**

1. **Kontrast-Fix (Defekt).** Kopfband-Text, Badges (`K-02`, `S-01`, `H4-04`, `Komm.: PG`,
   `beschichtet`, Status-/Waisen-/Fehler-Badges) und der abgesetzte Matchcode in eckigen Klammern
   erreichen WCAG AA — **4,5:1** für Fliesstext, **3:1** für grosse Schrift/Bedienelemente (Chevron,
   Buttons). Prüfkriterium ist der Kontrastwert, nicht subjektives Empfinden.
2. **Umbau von freifliessendem Baum auf EINE seitenweite Tree-Table (OSEON-Stil).** Statt einer
   Bootstrap-Card je Struktur mit verschachtelten `<div>`s gibt es **eine**
   `<table data-view-key="FaHierarchyStructure">` für die gesamte Seite (analog
   `Views/Tracking/OseonIndex.cshtml`, `#oseonTree`); jede Struktur ist ein eigener `<tbody>`-Block
   mit einer Struktur-Kopfzeile (`<tr>` mit `colspan`), die die heutigen Kopf-Badges (HauptFA-Nr.,
   Kunde, Status/KO-/FE-Termin/Abteilung bei eindeutigem Kopf, Waisen-/Mehrdeutig-/Fehler-Badge,
   Knotenzahl) trägt. **Begründung (Entscheidung zu B-2, Kritische Prüfung):**
   `column-preferences.js` bindet genau **eine** Tabelleninstanz je `data-view-key`
   (`document.querySelector('table[data-view-key]')`) — mehrere Tabellen mit demselben `viewKey`
   würden sich gegenseitig überschreiben. Ausserdem müssen Spalten über alle Strukturen hinweg
   fluchten, sonst sind gespeicherte Breiten/Reihenfolge und der neue Spaltenfilter bedeutungslos.
   Präzedenzfall im eigenen Haus: die Kommissionierlisten sind bereits genau so gebaut (eine
   Tabelle, ein `<tbody>` je `HauptFA`-Gruppe, Gruppen-Kopfzeile). Spalten laut Backlog-Tabelle:
   Struktur (Einrückung+Expander+Icon+FA-Nummer — **einzige** wachsende Spalte), Matchcode,
   Bezeichnung, Arbeitsbereich, Komm.-Ziel, Soll/Fert., SubFA, Status. Auf schmalen Bildschirmen
   werden hintere Spalten ausgeblendet, **nicht** die Struktur-Spalte gequetscht. **Preis, bewusst
   akzeptiert:** die bisherige eigenständige Karten-Optik je Struktur entfällt zugunsten der
   Ausrichtung und Wiederverwendung der bestehenden JS-Bausteine.
3. **Spaltenfilter mit Baum-Semantik** innerhalb einer bereits angezeigten Struktur: Treffer
   hervorheben, Pfad zur Wurzel gedimmt als Kontext sichtbar, nicht treffende Geschwisterzweige
   samt Unterbaum ausblenden (Freigabe-Antwort 4). Auswahlfilter (`<select>`) für Spalten mit
   kleinem Wertebereich (Arbeitsbereich, Komm.-Ziel, Status). Klare, unterscheidbare Beschriftung
   gegenüber dem bestehenden Freitextfeld „Knoten in angezeigten Strukturen hervorheben". Die
   Baumspalten selbst sind **nicht sortierbar** (Entscheidung zu S-3) — siehe Fachliche
   Anforderungen, Abschnitt 3.
4. **Icons nach Knotentyp**, abgeleitet aus vorhandenen Daten (`SubFA`, `Beschaffungsartikel`,
   Wurzel-Position) — kein neues Feld, als **geordnete Präzedenzkette** (Entscheidung zu S-2).
   Icon nie alleiniger Bedeutungsträger (Tooltip/`aria-label`), Farbe nie alleiniger Unterscheider
   (Form ergänzt), Bootstrap-Icons-Glyphen als Inline-SVG **oder** vorhandenes Paket (siehe
   Technischer Lösungsentwurf), kleine ausklappbare Legende.
5. **Verhaltensübernahme vom OSEON-Tracking-Vorbild** (Chevron-Toggle, Expand/Collapse auf Knoten-
   **und** Struktur-Ebene, Einrückungs-Leitlinien je Ebene, „Alle auf/zu", dichtes Zeilenraster) —
   Details siehe Technischer Lösungsentwurf.
6. **Anschluss der Baumanzeige an `column-preferences.js`** (Freigabe-Antwort 2 zur Schwester-Spec:
   „wer die Tabelle baut, verdrahtet sie auch" — letzter Schritt dieser Spec, **in-scope**, nicht
   mehr „separate Spec" wie in einer früheren Fassung). Neuer `viewKey` (Vorschlag
   `FaHierarchyStructure`) wird in `ColumnDefinitions.GetByViewKey` registriert (sonst antwortet die
   Prefs-API mit `400` und Einstellungen gehen bei jedem Reload verloren — derselbe Fehler, der
   bereits einmal bei `FaWorklist` passiert ist, siehe Schwester-Spec Fachliche Anforderung 2).
   `#view-config`/`#column-config`-JSON-Blöcke und das Skript-Include `column-preferences.js`
   **vor** dem neuen Baum-Skript werden ergänzt; jedes `<th>` trägt ein `data-col-key`.
   `SupportsSortDefault: false`, weil Baumspalten grundsätzlich nicht sortierbar sind (siehe Punkt 3).

**Out-of-Scope:**

- `FaHierarchyNode`, `FaHierarchyOrderInfo`, `FaHierarchySyncService`, Import/Sage-Views — unverändert.
- Die **server-seitige** Struktur-Filterkarte aus Teil 2 (welche Strukturen erscheinen) — unverändert,
  bleibt bestehen. Diese Spec fügt nur den **zusätzlichen, client-seitigen** Spaltenfilter *innerhalb*
  einer bereits angezeigten Struktur hinzu.
- Die Filterlogik/Mengen-Bausteine aus Teil 3–5 (`FaHierarchyListBuilder`, Kommissionier-/
  Beschichtungs-/Vormontagelisten) — die sind bereits echte Tabellen und von diesem Nachtrag nicht
  betroffen (bestätigt die Schwester-Notiz [[2026-08-12-listen-spaltenauswahl]]). Der Anschluss
  dieser **vier flachen Listen** an `column-preferences.js` ist Gegenstand der separaten Spec
  [[2026-08-12-listen-spaltenauswahl-spec]] und wird von dieser Spec nicht berührt. Umgekehrt gilt:
  der Anschluss **dieser** Baumanzeige ist laut Freigabe-Antwort 2 In-Scope (siehe oben, Punkt 6).
- Zugriffsschutz/Rollen — unverändert (`[RequirePickingOrTrackingOrLeitstandAccess]` bleibt, keine
  neue Rolle, kein neuer Filter, kein neues Feature-Toggle).
- Keine neue Bibliothek (weder Icon- noch Baum- noch Tabellen-Bibliothek) — nur Bootstrap 5 +
  vorhandene projekteigene JS/CSS-Bausteine (ausser ggf. bereits vorhandenem `bootstrap-icons`,
  siehe Technischer Lösungsentwurf).
- Materialisierung nach `ProductionOrders` (Teil 7), Druck/PDF der Struktur (eigener
  Querschnitts-Baustein, [[2026-08-06-pdf-erzeugung-fahierarchy-druck]]).

## Fachliche Anforderungen

### 1. Kontrast (Defekt)

**Root Cause im Code identifiziert** (`IdealAkeWms/Views/FaHierarchy/Index.cshtml`, Zeilen
174–176 im aktuellen Worktree-Stand): Die globale `.card-header`-Regel in `site.css` setzt
`background-color: var(--ake-primary)` (`#053153`, dunkles Navy) **und** `color: var(--ake-white)`.
Die view-eigene Regel `.fa-structure-header { background-color: #f8f9fa; }` (im `@section Scripts`
der View, cascade-mässig nach dem `<link>` auf `site.css`) überschreibt bei gleicher Spezifität nur
den Hintergrund auf ein **sehr helles** Grau — die geerbte **weisse** Textfarbe bleibt unverändert.
Ergebnis: nahezu weisser Text auf nahezu weissem Hintergrund im Kopfband — exakt der im Backlog
beschriebene Defekt. Root Cause im Code verifiziert (Entscheidung zu H-1, Kritische Prüfung), nicht
mehr nur vermutet.

- Kopfband-Text und Hintergrund zusammen müssen **mindestens 4,5:1** (Fliesstext) erreichen.
- Bedienelemente (Chevron-Toggle-Hitbox, „Alle auf/zu"-Buttons) und grossformatiger Text (FA-Nummer)
  mindestens **3:1**.
- Alle Badge-Varianten (`bg-primary`, `bg-secondary`, `bg-light text-dark border`, `bg-info
  text-dark`, `bg-warning text-dark`, `bg-danger`) und der Matchcode in `text-muted small` sind
  gegen ihren tatsächlichen Hintergrund zu prüfen — nicht nur der auffälligste Fall (Kopfband).
- Fix darf **keine neue Farbpalette** einführen, sondern nutzt die bestehenden `--ake-*`-Variablen
  aus `site.css` konsistent (Konsistenz vor Eigenständigkeit, `frontend-design`-Skill verbindlich).
- Prüfmethode (Freigabe-Antwort 5): **rechnerisch** aus den CSS-Werten (Vorder-/Hintergrundfarbe
  ermitteln, Verhältnis gegen WCAG AA berechnen), nicht durch Terminal-Screenshot-Raten — das
  Ergebnis ist so nachprüfbar statt Geschmack. Eine zusätzliche Sichtprüfung am realen Terminal
  gehört in die manuelle Test-Checkliste als **Bestätigung**, nicht als Messverfahren.

### 2. Tree-Table statt freifliessender Baum (Topologie: seitenweite Tabelle, `<tbody>` je Struktur)

- Die Seite rendert **eine** `<table id="faTree" data-view-key="FaHierarchyStructure" class="table table-sm">`
  (analog `#oseonTree` in `Views/Tracking/OseonIndex.cshtml`) für alle auf der aktuellen
  Struktur-Seite angezeigten Strukturen. Jede Struktur ist ein eigener
  `<tbody class="fa-structure-group">`-Block:
  - Erste `<tr>` je `<tbody>` = Struktur-Kopfzeile mit `colspan` über alle Spalten, trägt die
    heutigen Kopf-Badges (HauptFA-Nr., Kunde, Status/KO-/FE-Termin/Abteilung bei eindeutigem Kopf,
    Waisen-/Mehrdeutig-/Fehler-Badge, Knotenzahl) sowie einen eigenen Auf-/Zuklapp-Chevron für die
    ganze Struktur (analog `oseon-tree-group`).
  - Danach folgen die Knoten-`<tr>`s der Struktur, rekursiv aus `_FaHierarchyNode.cshtml` gerendert
    (siehe Technischer Lösungsentwurf), als flache Geschwister-Zeilen **innerhalb desselben
    `<tbody>`**, nicht als eigene Tabelle.
- Jede Ebene ist eine `<tr>`; **nur** die Struktur-Spalte (`<td>` mit Chevron + Icon + `Artnr`)
  trägt die Einrückung (per `padding-left` proportional zur Tiefe, analog
  `Views/Tracking/_OseonGroupDetails.cshtml` Zeilen 36/75: `padding-left: 28px` / `52px` je Ebene).
  Alle anderen Spalten (Matchcode, Bezeichnung, Arbeitsbereich, Komm.-Ziel, Soll/Fert., SubFA,
  Status) bleiben senkrecht ausgerichtet über alle Zeilen, Ebenen **und Strukturen** hinweg — das
  ist der eigentliche Zweck der Ein-Tabelle-Topologie.
- Die Kopfdaten-Tabelle `fa-head-table` (Montage-Abt./Kunde/Status/Termine je
  `FaHierarchyOrderInfo`-Zeile bei Kombigeräten) bleibt als eigene, kleine Tabelle **innerhalb**
  der Struktur-Kopfzeile bestehen — sie ist keine Baum-Tabelle und nicht Teil der
  column-preferences-Spaltenlogik.
- Knotenidentität bleibt über `data-node-id`/`data-parent-id` (bereits vorhanden in
  `_FaHierarchyNode.cshtml`), Expand/Collapse über Zeilen-Sichtbarkeit (`style="display:none"` auf
  `<tr>`, wie beim OSEON-Tracking-Vorbild) statt über `.fa-node-children`-Container-Sichtbarkeit —
  das gilt sowohl für die Struktur-Ebene (ganzer `<tbody>`-Block ein-/ausblendbar, analog
  `oseon-group-details`) als auch für einzelne Knoten-Unterbäume.
- Schmale Bildschirme (Fertigungsterminal-Breite, Schwellenwert Bootstrap-Breakpoint `lg`/992px,
  Entscheidung zu H-4 — tatsächliche Terminal-Auflösung noch zu bestätigen, siehe Offene
  Rückfrage): hintere Spalten (z. B. SubFA, Soll/Fert.) werden ausgeblendet oder scrollbar gemacht
  — die Struktur-Spalte wird **nie** komprimiert, sie trägt die Orientierung (Backlog-Kernregel).
- **Bewusst verworfen** (Entscheidung zu H-3): Der im Polish-Commit `b6b6d83` gerade gebaute
  flex-basierte Zeilen-Stil (`ms-auto`, Inline-Badges, `.fa-node-desc`) und die Karten-Kopfoptik
  werden vom Tree-Table-Umbau grossteils ersetzt — eine bewusste Wegwerf-Entscheidung, kein
  stillschweigender Verlust.

### 3. Spaltenfilter mit Baum-Semantik

- **Zwei unterscheidbare Mechanismen im UI**, klar getrennt beschriftet:
  1. Bestehendes Feld „Knoten in angezeigten Strukturen hervorheben" (`#faClientFilter`) —
     hervorheben **ohne** die Struktur zu verändern, bleibt unverändert in seiner Funktion
     (Baum-Integrität).
  2. **Neu:** Spaltenfilter-Zeile im Tabellenkopf (`filter-row` unter dem `<thead>`, ein
     lokaler, für FaHierarchy gebauter Baustein — siehe Technischer Lösungsentwurf) — filtert
     **innerhalb** einer bereits angezeigten Struktur: Treffer hervorheben, Pfad zur Wurzel
     **gedimmt** sichtbar lassen (neue CSS-Klasse `.fa-node-context`), nicht treffende
     Geschwisterzweige **samt Unterbaum** ausblenden. Fällt eine Struktur dadurch auf null
     Treffer, verschwindet ihr ganzer `<tbody>`-Block. Ein Zähler „X von Y Knoten" macht die
     Einschränkung sichtbar (Freigabe-Antwort 4).
- **Auswahlfilter (`<select>`)** für Spalten mit kleinem Wertebereich (Arbeitsbereich, Komm.-Ziel,
  Status) statt Freitext — Werteliste wird aus den tatsächlich in der aktuell geladenen Seite
  vorkommenden Werten abgeleitet (kein neuer API-Endpunkt). Für Spalten mit grossem Wertebereich
  (Struktur/Artnr, Matchcode, Bezeichnung, Soll/Fert., SubFA) bleibt Freitext.
- Es gibt im Repo **keinen** bestehenden `<select>`-basierten Spaltenfilter-Präzedenzfall
  (`table-filter.js` kennt nur Freitext mit OR-`,`/NOT-`!`-Mini-Syntax) — dieser Baustein ist
  fachlich neu, wird aber **lokal für FaHierarchy** gebaut, nicht generalisiert (Freigabe-Antwort
  6: Abstraktion erst mit einem zweiten echten Verbraucher).
- **Baumspalten sind nicht sortierbar** (Entscheidung zu S-3, Kritische Prüfung): Bei einem Baum
  ist die Zeilenreihenfolge die Hierarchie — eine Spaltensortierung würde Eltern und Kinder
  auseinanderreissen, unabhängig vom Multi-`tbody`-Sortier-Fallstrick der Schwester-Spec. Kein
  `data-sortable`/`data-sort-key` an den `<th>`; `SupportsSortDefault: false` im `view-config`
  (siehe Umfang Punkt 6). Der in der Schwester-Spec belegte Sortier-Fallstrick von
  `table-filter.js` (sortiert nur das erste `<tbody>`) kann diese Ansicht damit **strukturell gar
  nicht** treffen, weil `table-filter.js` hier nicht als Sortier-Engine eingebunden wird.

### 4. Icons nach Knotentyp

Klassifikation als **geordnete Präzedenzkette** (Entscheidung zu S-2, Kritische Prüfung), nicht als
unabhängige Tabelle — die Reihenfolge legt fest, welches Icon gewinnt, wenn ein Knoten mehrere
Kriterien gleichzeitig erfüllt (eine Wurzel ist typischerweise zugleich Baugruppe und erfüllt sowohl
das Wurzel- als auch das `SubFA != 0`-Kriterium; das Wurzel-Icon gewinnt):

1. **Wurzel/Endprodukt** — oberste Zeile der Struktur (`VaterFA IS NULL`, bereits im Tree-Builder
   bekannt), **unabhängig** von `SubFA`.
2. **Baugruppe (Eigenfertigung)** — sonst, wenn `SubFA != 0` (`FaHierarchyNode.SubFA`).
3. **Zukaufteil** — sonst, wenn `SubFA == 0` **und** `Beschaffungsartikel == true`
   (`FaHierarchyNode.Beschaffungsartikel`, bool, bereits vorhanden — Entscheidung zu H-2 bestätigt
   das Feld existiert, kein neues DB-Feld nötig).
4. **Lager-/Fertigungsmaterial** — sonst (`SubFA == 0`, `Beschaffungsartikel == false`).

- Zustandsmarker (`Beschichtet`, `Kommissionieren`) bleiben Badges, nicht ins Typ-Icon gemischt
  (bereits so umgesetzt, unverändert zu übernehmen).
- Jedes Icon führt `title=`/`aria-label` mit Klartext (z. B. „Baugruppe — eigener Sub-FA").
- Form unterscheidet zusätzlich zur Farbe (z. B. Kreis vs. Quadrat/Raute vs. Dreieck), nicht nur
  Farbton — heutige Icons (Punkt für Blatt, Box für Baugruppe) sind ein Anfang, decken aber nicht
  alle vier Typen ab (Zukauf/Material aktuell nicht unterschieden).
- Kleine, ausklappbare Legende oberhalb der Strukturliste (z. B. `<details>` oder Bootstrap-Collapse).

### 5. Verhalten wie OSEON-Tracking

**Bestätigte Referenz** (Freigabe-Antwort 3): `Views/Tracking/OseonIndex.cshtml` +
`Views/Tracking/_OseonGroupDetails.cshtml` („OSEON Teileverfolgung"), **nicht**
`Views/OseonReporting/_OseonReportingTable.cshtml` (die frühere Backlog-Referenz war ein
Fehlgriff aus einer Dateinamen-Suche, korrigiert). Vorbild: eine echte `<table id="oseonTree">`,
dreistufiger Baum (Kundenauftrag → Subauftrag → Arbeitsgang) über
`<tr class="oseon-tree-group/-sub/-op">`, `oseon-toggle`/`oseon-chevron`, „Alle aufklappen"/„Alle
zuklappen"-Buttons, Einrückung ausschliesslich über `padding-left` in der Namens-Spalte, bereits an
`column-preferences.js` angeschlossen (`view-config`/`column-config`). **Zu übernehmen:** dieses
Chevron-/Expand-Collapse-Muster auf **beiden** Ebenen — Struktur-Ebene (ganzer `<tbody>`-Block wie
`oseon-group-details`) und Knoten-Ebene (einzelner Unterbaum) —, Einrückungs-Leitlinien je Ebene
(in `_FaHierarchyNode.cshtml` bereits ansatzweise über `.fa-node-children { border-left: 1px
dashed }` vorhanden — im Tree-Table-Umbau als vertikale Leitlinie je Einrückungsstufe
fortzuführen), „Alle auf/zu" (in `Index.cshtml` bereits vorhanden, Verhalten beibehalten), dichtes
Zeilenraster (kleine `padding`, wie im OSEON-Vorbild). Anders als beim OSEON-Vorbild wird kein
eigener spalten-basierter Sort eingebaut (siehe Abschnitt 3: Baumspalten nicht sortierbar).

### 6. Anschluss an `column-preferences.js`

- **Server-seitige Registrierung ist Pflicht, nicht optional** (Entscheidung zu B-1): In
  `Models/ViewModels/ColumnDefinitions.cs` wird eine neue `ViewConfig`-Konstante
  `FaHierarchyStructure` (Namensvorschlag) ergänzt sowie ein zusätzlicher `case`-Zweig im
  `GetByViewKey`-Switch (analog `OseonTracking`). Ohne diesen Eintrag prüft
  `UserViewPreferencesApiController.Get/Put/Delete` `GetByViewKey(viewKey) == null → BadRequest` —
  das Zahnrad erschiene zwar (rein clientseitig aus dem inline `#column-config`), aber Änderungen
  gingen bei jedem Reload verloren (der `PUT` schlägt still fehl). Exakt dieser Fehler ist am Code
  bereits einmal passiert (`ColumnDefinitions.cs`-Kommentar bei `FaWorklist`).
- Spalten der `ViewConfig`: `structure` (locked, einzige wachsende Spalte), `matchcode` (locked —
  zweiter identifizierender Schlüssel), `description`, `workarea`, `picking-target`, `quantity`,
  `subfa`, `status` — deckungsgleich mit den `data-col-key`-Werten der `<th>` in `Index.cshtml`.
- `SupportsReorder: true`, `SupportsSortDefault: false` (Baumspalten sind fachlich nicht
  sortierbar, siehe Abschnitt 3 — nicht aus Vorsicht vor dem `table-filter.js`-Sortier-Fallstrick
  der Schwester-Spec, den diese Ansicht strukturell nicht treffen kann).
- `Index.cshtml`: `#view-config`- und `#column-config`-JSON-Blöcke (Muster identisch zur
  Schwester-Spec/`OseonIndex.cshtml`), Einbindung `column-preferences.js` **vor** dem neuen
  `fa-hierarchy-tree.js` (Reihenfolge-Pflicht, ADR 0005/`fallstricke.md`).

## Technischer Lösungsentwurf

- **Tabellen-Topologie (Entscheidung zu B-2):** EINE `<table id="faTree"
  data-view-key="FaHierarchyStructure" class="table table-sm">` für die gesamte Seite, analog
  `#oseonTree` in `Views/Tracking/OseonIndex.cshtml`. Jede Struktur wird zu einem eigenen
  `<tbody>` mit einer Kopfzeile (`colspan`) und den rekursiv gerenderten Knoten-Zeilen.
  `Index.cshtml` verliert die heutige `foreach`-Schleife über `<div class="card fa-structure">`
  und rendert stattdessen eine `foreach`-Schleife über `<tbody>`-Blöcke innerhalb der einen
  `<table>`.
- **Markup-Umbau `_FaHierarchyNode.cshtml`:** von rekursivem `<div class="fa-node">`/
  `.fa-node-children` auf rekursiv gerenderte `<tr>`-Zeilen — analog `_OseonGroupDetails.cshtml`,
  wo Sub-Order- und Operation-Zeilen ebenfalls als flache Geschwister-`<tr>`s mit
  `data-parent-*`-Attributen gerendert werden, nicht als DOM-verschachtelte Container.
  `HasChildren`/`Children` aus `FaHierarchyTreeNode` bleiben unverändert, nur das Rendering-Target
  ändert sich. `data-node-id`/`data-parent-id` bleiben erhalten (bestehender JS-Haken).
- **JS-Umbau ist eine Neuentwicklung, keine Wiederverwendung (Entscheidung zu B-1/B-3):** Der
  komplette heutige Inline-Skriptblock aus `Index.cshtml` (Zeilen 201–274 im aktuellen
  Worktree-Stand: `setExpanded`, Toggle-Click-Handler, `setAll`/„Alle auf/zu", `expandAncestors`,
  der `#faClientFilter`-Highlight-Handler) wird **neu geschrieben**, weil `setExpanded`/
  `expandAncestors` heute fundamental auf DOM-Verschachtelung (`.fa-node-children`-Container,
  `parentElement`-Traversal) beruhen — im flachen Tabellenzeilen-Modell gibt es diese Container
  nicht mehr, die Eltern-Kind-Beziehung läuft nur noch über `data-parent-id`. Zielverhalten bleibt
  erhalten (Zeilen-Sichtbarkeit statt Container-Sichtbarkeit, siehe Fachliche Anforderungen
  Abschnitt 2), die JS-Haken `data-node-id`/`data-parent-id`, „Alle auf/zu"
  (`#btnFaExpandAll`/`#btnFaCollapseAll`) und `#faClientFilter` bleiben als Public-Contract
  erhalten und müssen im neuen Code weiterhin bedient werden. Wegen des Umfangs wird der Block
  nach `wwwroot/js/fa-hierarchy-tree.js` ausgelagert (statt weiter inline in `Index.cshtml`) —
  Grössenordnung vergleichbar mit einem kompletten Neubau, kein kleiner Zusatz.
- **Spaltenfilter (neuer, eigener Baustein, lokal für FaHierarchy — Freigabe-Antwort 6):** Teil
  desselben neuen `fa-hierarchy-tree.js`. Pro Spalte ein Filter-Input/-Select in einer
  `filter-row` unter dem `<thead>`; bei Eingabe/Auswahl wird je Knoten-`<tr>` geprüft, ob sie
  selbst **oder** irgendein Nachfahre passt → Zeile bleibt sichtbar (als Treffer oder als gedimmter
  Vorfahrenpfad-Kontext, `.fa-node-context`), sonst `display:none`; nicht treffende
  Geschwisterzweige samt Unterbaum werden ausgeblendet; ein Zähler „X von Y Knoten" macht die
  Einschränkung sichtbar; fällt eine Struktur auf null Treffer, verschwindet ihr ganzer
  `<tbody>`-Block (Freigabe-Antwort 4). Auswahlfilter (`<select>`) für Arbeitsbereich/Komm.-Ziel/
  Status, Freitext für die übrigen Spalten. Kein bestehender `<select>`-Präzedenzfall im Repo
  (`table-filter.js` kennt nur Freitext) — dieser Baustein bleibt bewusst lokal, keine
  Generalisierung vor einem zweiten echten Verbraucher.
- **Icons:** Technik abhängig vom Repo-Befund (Entscheidung zu Freigabe-Antwort 7): Ist
  `bootstrap-icons` bereits als Paket eingebunden (`libman.json`/`wwwroot/lib`), wird es
  verwendet. Sonst Inline-`<svg>`-Pfaddaten mit `currentColor` (aktuelle Projekt-Konvention, siehe
  bestehende Chevron-/Box-Icons in `_FaHierarchyNode.cshtml`/`OseonIndex.cshtml`) — `currentColor`
  ist auch für den Kontrastpunkt wichtig, weil das Icon so die Textfarbe erbt. Kein CDN, kein
  neues Paket ohne vorherige Prüfung. Vier Icons rechtfertigen keine neue Abhängigkeit; ein bereits
  vorhandenes Set aber sehr wohl die Wiederverwendung.
- **Kontrast-Fix:** `.fa-structure-header` setzt zusätzlich zum Hintergrund eine dazu passende,
  geprüfte Textfarbe (statt die geerbte `.card-header`-Weiss-Regel unkommentiert zu
  überschreiben), oder verzichtet ganz auf das lokale Überschreiben und übernimmt das dunkle
  `.card-header`-Band aus `site.css` unverändert (einfachste, konsistenteste Lösung — kein neuer
  Farbwert nötig). Beide Varianten müssen rechnerisch WCAG AA erfüllen (siehe Fachliche
  Anforderungen Abschnitt 1) und mit dem übrigen Corporate Design konsistent sein.
  **Umsetzungsreihenfolge (Entscheidung zu S-4):** Dieser Fix wird im Dev-Lauf als **erster,
  separater Commit** vorgezogen, bevor der grosse Tree-Table-Umbau beginnt — er ist kein Geisel
  des Umbaus und liefert sofort einen behobenen Funktionsausfall.
- **column-preferences-Anschluss (in-scope, Entscheidung zu B-1, letzter Schritt, siehe Umfang
  Punkt 6 / Fachliche Anforderungen Abschnitt 6):** `Models/ViewModels/ColumnDefinitions.cs`
  bekommt die neue `ViewConfig`-Konstante `FaHierarchyStructure` sowie den zusätzlichen
  `case`-Zweig im `GetByViewKey`-Switch (analog `OseonTracking`, aktuell Zeile ~284 im
  Worktree-Stand). `Index.cshtml` bekommt die `#view-config`/`#column-config`-Blöcke und die
  Skript-Reihenfolge `column-preferences.js` **vor** `fa-hierarchy-tree.js`. Jedes `<th>` der
  Baum-Tabelle trägt ein `data-col-key`, das mit den `key`-Werten in `#column-config` und
  `ColumnDefinitions.cs` übereinstimmt.
- **Bleibt unverändert:** server-seitige Struktur-Filterkarte (`FaHierarchyController.Index`,
  `FilterHauptFas(...)`), Tiefen-Cap/Zyklenschutz (`FaHierarchyTreeBuilder`), Pagination auf
  Struktur-Ebene, Zugriffsschutz `[RequirePickingOrTrackingOrLeitstandAccess]` — diese Spec
  berührt ausschliesslich `Views/FaHierarchy/*.cshtml`, `wwwroot/js/fa-hierarchy-tree.js`,
  `wwwroot/css/site.css` und additiv `Models/ViewModels/ColumnDefinitions.cs`, keine sonstige
  Controller-/Service-Logik.

## Migrations-/SQL-Auswirkungen

Keine. Reine Razor-/CSS-/JS-Änderung an bestehenden Views plus eine additive C#-Erweiterung von
`ColumnDefinitions.cs`; keine neuen Entitäten, keine Schema-Änderung, kein neues
`AppSettings`/`ServiceSettings`-Feature-Toggle. `UserViewPreference`/die zugehörige Tabelle
existieren bereits und sind `viewKey`-agnostisch (Freitextspalte, keine Fremdschlüssel-/
Check-Constraint auf bekannte Keys, bereits bei der `FaWorklist`-Präzedenz ohne Migration belegt) —
der neue `viewKey` `FaHierarchyStructure` braucht daher keinen Migrationsschritt, kein
`SQL/XX_*.sql`, keinen `00_FreshInstall.sql`-Eintrag.

## Audit-Feld-Auswirkungen

Keine neue fachliche Entität wird geschrieben oder verändert; reine Anzeige. `UserViewPreference`
schreibt beim Speichern bereits `_currentUserService.GetDisplayName()`/`GetWindowsUserName()`
(bestehender Code, unverändert durch diese Spec) — dieselbe, bereits etablierte
Persistenz-Logik wie bei allen anderen `viewKey`s.

## Betroffene Rollen / Zugriffsfilter

Keine Änderung. `FaHierarchyController` behält `[RequirePickingOrTrackingOrLeitstandAccess]`
(Class-Level, Read-only-View, kein Edit-Split) unverändert bei — dieselbe Rolle/derselbe Filter wie
in Teil 2 festgelegt. Kein neues Feature-Toggle, keine neue Rolle. Die additive Erweiterung von
`ColumnDefinitions.cs` ändert weder Zugriffsschutz noch Feature-Toggles.

## Listen-View-Pattern-Pflichten (ADR 0005)

`/FaHierarchy` bleibt eine **dokumentierte Ausnahme** vom Server-Mode-Spaltenfilter-Pattern (ADR
0005, „hierarchische Baumdarstellung", BOM-Tree-Präzedenzfall) — diese Einstufung ändert sich durch
den Tree-Table-Umbau **nicht**: Der neue Spaltenfilter bleibt Client-Mode (kein
`data-server-column-filter="true"`), weil die server-seitige Struktur-Filterkarte weiterhin die
Struktur-Ebene entscheidet und ein echter Server-Spaltenfilter innerhalb einer Struktur wieder
Kinder verwaisen liesse (dieselbe Begründung wie in Teil 2, Fachliche Anforderungen, Abschnitt
„Server-Filter vs. Baum-Integrität"). Pagination (Struktur-Ebene) und Filterkarte (Struktur-Ebene)
bleiben unverändert bestehen.

**Spaltenpräferenzen (`column-preferences.js`) sind — anders als in einer früheren Fassung dieser
Spec — Teil dieser Spec** (siehe Umfang Punkt 6, Fachliche Anforderungen Abschnitt 6, Technischer
Lösungsentwurf): Der neue `viewKey` `FaHierarchyStructure` wird nach demselben, in
[[2026-08-12-listen-spaltenauswahl-spec]] erprobten Muster registriert (`ColumnDefinitions.cs`,
`#view-config`/`#column-config`, Skript-Reihenfolge), jedoch mit `SupportsSortDefault: false`, weil
Baumspalten aus fachlichen Gründen (Zeilenreihenfolge = Hierarchie) nicht sortierbar sind — nicht
wegen des in der Schwester-Spec entdeckten `table-filter.js`-Sortier-Fallstricks (der diese Ansicht
strukturell gar nicht treffen kann, siehe Entscheidung zu S-3, da `table-filter.js` hier nicht als
Sortier-Engine eingebunden wird).

## Akzeptanzkriterien

1. Kopfband-Text (`HauptFA <Nr.>`, Badges, Matchcode-Klammern) erreicht gegen seinen tatsächlichen
   Hintergrund mindestens 4,5:1 (Fliesstext) bzw. 3:1 (grosse Schrift/Bedienelemente) — geprüft
   rechnerisch anhand der CSS-Werte an allen im Backlog genannten Elementen (`K-02`, `S-01`,
   `H4-04`, `Komm.: PG`, `beschichtet`, Matchcode), ergänzend am realen Terminal sichtgeprüft.
2. Alle auf der Seite angezeigten Strukturen erscheinen als **eine** `<table
   data-view-key="FaHierarchyStructure">` mit je einem `<tbody>` pro Struktur; Spalten fluchten
   über **alle** Strukturen und Ebenen hinweg, **nur** die Struktur-Spalte trägt Einrückung —
   verifiziert an mindestens zwei Strukturen unterschiedlicher Tiefe auf derselben Seite sowie an
   einer Struktur mit mindestens 4 Ebenen und 50+ Positionen (produktivnahe Daten, siehe
   Vorbedingung im Backlog).
3. Auf einem schmalen Bildschirm (unterhalb Bootstrap-Breakpoint `lg`/992px, Entscheidung zu H-4,
   Bestätigung der realen Terminal-Auflösung steht aus, siehe Offene Rückfrage) werden hintere
   Spalten ausgeblendet/scrollbar; die Struktur-Spalte bleibt in voller Breite lesbar, wird nicht
   komprimiert.
4. Ein Spaltenfilter-Treffer tief in einer Struktur hebt sich hervor, sein Pfad zur Wurzel bleibt
   sichtbar (gedimmt als Kontext), nicht treffende Geschwisterzweige samt Unterbaum ohne eigenen
   Treffer werden ausgeblendet, ein Zähler „X von Y Knoten" zeigt die Einschränkung; fällt eine
   Struktur dadurch auf null Treffer, verschwindet ihr `<tbody>`-Block vollständig — kein Knoten
   verschwindet ohne sichtbaren Zusammenhang zu seinem Elternpfad.
5. Für Arbeitsbereich, Komm.-Ziel und Status steht ein Auswahlfilter (`<select>`) mit den
   tatsächlich vorkommenden Werten zur Verfügung statt eines Freitextfelds.
6. Das bestehende Feld „Knoten in angezeigten Strukturen hervorheben" und der neue Spaltenfilter
   sind im UI durch ihre Beschriftung eindeutig als zwei unterschiedliche Mechanismen erkennbar
   (z. B. „hervorheben (Struktur bleibt vollständig)" vs. „filtern (blendet aus)").
7. Jeder Knoten zeigt ein Typ-Icon (Wurzel/Baugruppe/Zukauf/Material, Präzedenzkette
   Wurzel → Baugruppe → Zukaufteil → Material) mit Tooltip/`aria-label` und formseitig (nicht nur
   farblich) unterscheidbarer Grafik; eine Legende ist vorhanden und ausklappbar. Ein Knoten, der
   gleichzeitig Wurzel und Baugruppe wäre (`VaterFA IS NULL` und `SubFA != 0`), zeigt das
   Wurzel-Icon.
8. Chevron-Toggle, Expand/Collapse je Knoten **und** je Struktur (ganzer `<tbody>`-Block) sowie
   „Alle aufklappen"/„Alle zuklappen" verhalten sich funktional wie im
   `Views/Tracking/OseonIndex.cshtml`-Vorbild.
9. Keine Spalte der Baum-Tabelle trägt `data-sortable`/`data-sort-key`; im Zahnrad-Dialog gibt es
   keine Option „Standard-Sortierung speichern" (`SupportsSortDefault: false`).
10. Auf `/FaHierarchy` erscheint ein Zahnrad-Symbol zur Spaltenkonfiguration (Offcanvas), analog
    zum Verhalten in `ProductionOrders/Index`/`OseonIndex`.
11. Sichtbarkeit, Breite und Reihenfolge einer Spalte lassen sich ändern, bleiben nach Reload
    erhalten (`PUT /api/user-view-preferences/FaHierarchyStructure` liefert `200`, nicht `400`) und
    sind je Benutzer getrennt (zweiter Benutzer sieht seine eigene, unabhängige Konfiguration).
12. `GET /api/user-view-preferences/FaHierarchyStructure` liefert `204 NoContent` ohne gespeicherte
    Präferenz bzw. `200` mit den gespeicherten Settings — in keinem Fall `400 BadRequest`
    (Regressionstest analog zum bestehenden `FaWorklist`-Testfall).
13. Struktur-Spalte und Matchcode-Spalte lassen sich im Zahnrad-Dialog **nicht** ausblenden
    (`locked: true`).
14. Bei mindestens zwei gleichzeitig angezeigten Strukturen (`<tbody>`-Blöcken): Eine Spalte
    ausblenden bzw. umordnen wirkt in **jeder** Struktur, nicht nur der ersten — beweist die
    Multi-`tbody`-Tauglichkeit von `column-preferences.js` für diese Ansicht, analog zur in der
    Schwester-Spec geforderten Absicherung (dort SOLLTE 2).
15. `FaHierarchyController`, `FaHierarchyTreeBuilder`, die server-seitige Struktur-Filterkarte, der
    Tiefen-Cap/Zyklenschutz und der Zugriffsschutz bleiben byte-identisch unverändert (reine
    View-/JS-/CSS-Änderung plus additive `ColumnDefinitions.cs`-Erweiterung, keine Regression an
    Teil 2 und keine Änderung an bestehenden `ColumnDefinitions`-Einträgen).
16. Kein neues NuGet-/npm-/CDN-Paket wird eingebunden — Icons als bereits vorhandenes
    `bootstrap-icons`-Paket (falls im Repo vorhanden) oder Inline-SVG, kein Grid-/Tree-Framework,
    keine neue JS-Bibliothek für den Spaltenfilter.

## Test-Szenarien

Neues Kapitel „IDEAL Teil 2 — FA-Struktur: Darstellung/Spaltenfilter/Icons/Spaltenauswahl
(Nachtrag)" in `docs/TESTSZENARIEN.md`, ergänzend zum bestehenden Teil-2-Kapitel:

- **Kontrast-Test:** Kopfband, alle Badge-Varianten und Matchcode-Klammern per Kontrast-Messwerkzeug
  (z. B. Browser-DevTools-Kontrastprüfung) gegen 4,5:1/3:1 geprüft — Soll: alle bestehen.
- **Tree-Table-Grunddarstellung:** produktivnahe Struktur mit ≥4 Ebenen und 50+ Positionen laden;
  prüfen, dass nur die Struktur-Spalte einrückt und alle anderen Spalten über alle Ebenen und
  Strukturen bündig sind.
- **Schmaler Bildschirm:** Ansicht auf Fertigungsterminal-typischer Breite (< 992px) öffnen;
  hintere Spalten werden ausgeblendet/scrollbar, Struktur-Spalte bleibt unkomprimiert lesbar.
- **Struktur-Ebene Auf-/Zuklappen:** eine Struktur über ihre Kopfzeile ein- und ausklappen — der
  komplette `<tbody>`-Block der Struktur wird sichtbar/unsichtbar, andere Strukturen bleiben
  unberührt.
- **Spaltenfilter-Baumsemantik:** Filtertext auf einer Spalte (z. B. Bezeichnung) tief in einer
  Struktur eingeben → Treffer hervorgehoben, Pfad zur Wurzel gedimmt sichtbar, nicht treffende
  Geschwisterzweige samt Unterbaum ausgeblendet, Zähler „X von Y Knoten" korrekt, Struktur bleibt
  vollständig navigierbar.
- **Auswahlfilter:** Spalte „Arbeitsbereich" zeigt Dropdown mit den in der Seite vorkommenden Werten;
  Auswahl filtert wie beim Freitextfilter.
- **Unterscheidbarkeit der zwei Hervorheben-/Filter-Mechanismen:** Testperson kann anhand der
  Beschriftung ohne Vorwissen sagen, welches Feld nur hervorhebt und welches ausblendet.
- **Icon-Test inkl. Präzedenz:** je ein Beispielknoten pro Typ (Wurzel, Baugruppe, Zukauf,
  Material) zeigt ein unterscheidbares Icon mit Tooltip; ein Knoten, der Wurzel **und** Baugruppe
  wäre, zeigt das Wurzel-Icon; Legende ist sichtbar/ausklappbar.
- **Verhaltensparität zum OSEON-Vorbild:** Chevron-Rotation, Zeilen-Sichtbarkeit auf Knoten- und
  Struktur-Ebene, „Alle auf/zu" verhalten sich wie in `Views/Tracking/OseonIndex.cshtml`.
- **Keine Sortierbarkeit:** kein Spaltenkopf der Baum-Tabelle reagiert auf Klick als Sortier-Trigger;
  Zahnrad-Dialog bietet keine „Standard-Sortierung speichern"-Option.
- **Zahnrad-Dialog `/FaHierarchy`:** Spalte aus-/einblenden, Breite ändern, Reihenfolge ändern →
  bleibt nach Reload erhalten, je Benutzer getrennt; Struktur- und Matchcode-Spalte nicht
  ausblendbar.
- **Mehrfach-Struktur-Test (Regression gegen den Multi-`tbody`-Fallstrick der Schwester-Spec):**
  Seite mit mindestens zwei Strukturen laden, eine Spalte ausblenden bzw. umordnen → Änderung wirkt
  in **jeder** Struktur, nicht nur der ersten.
- **Regressionsfall:** server-seitige Struktur-Filterkarte, Tiefen-Cap-/Zyklen-Verhalten und
  Zugriffsschutz aus Teil 2 unverändert (bestehende Teil-2-Testszenarien weiterhin grün).

**Automatisiert (`UserViewPreferencesApiControllerTests.cs`):** neuer Testfall für
`FaHierarchyStructure` analog zum bestehenden `FaWorklist`-Regressionstest — `Get`/`Put`/`Delete`
liefern **keinen** `400 BadRequest`.

Nach Abschluss `secondbrain/tests/testszenarien-index.md` nachziehen.

## Deploy

- **Web-App:** ja (reine Razor-/CSS-/JS-Änderung an `Views/FaHierarchy/*` plus additive
  `ColumnDefinitions.cs`-Erweiterung, keine sonstige Controller-/Service-/Migrationsänderung).
- **Service:** nein.
- **Migration:** nein.
- **Kontext/Reihenfolge (Freigabe-Antwort 1):** Wird als **zusätzliche Etappe im bestehenden**
  Epic-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5` (Branch
  `feature/2026-08-07-ideal-teile-1-5`) umgesetzt, **nach** der Schwester-Spec
  [[2026-08-12-listen-spaltenauswahl-spec]] (Reihenfolge-Hinweis: Freigabe-Antwort 2 dort und
  hier). Damit entsteht **kein** zusätzlicher Deploy-Schritt — die Spec geht im selben
  Publish/Merge des Epic-Bündels mit. Innerhalb dieser Etappe wird der Kontrast-Fix (Fachliche
  Anforderungen Abschnitt 1) als eigener, erster kleiner Commit vorgezogen (Entscheidung zu S-4),
  bevor der grosse Tree-Table-/Filter-/Icon-/Prefs-Umbau folgt — beide bleiben aber Teil derselben
  Etappe/desselben Worktrees, kein separater Merge.
  **Preis, bewusst akzeptiert (Freigabe-Antwort 1):** Der Epic geht von `Testbereit` auf
  `InUmsetzung` zurück, und die QA (Build/Tests, Evidenz in der Spec) ist am Ende **erneut** zu
  fahren.
- **Publish-Befehle (finalisiert durch QA anhand des echten Diffs `961749f^..767f06f`, alle 9
  geänderten Dateien liegen unter `IdealAkeWms/`, `IdealAkeWms.Tests/` bzw. `docs/` — keine Datei
  unter `IDEALAKEWMSService/`, kein neues `*/Migrations/*`):**
  ```
  dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
  ```
  Aus dem **Worktree** publizieren (Test-System), erst danach testen, erst danach mergen. Nach dem
  Merge nur dann erneut aus `main` publizieren, wenn der Merge tatsächlich getestete Dateien mit
  parallelen `main`-Änderungen zusammengeführt hat.

## Offene Rückfragen

Die ursprünglichen sieben Rückfragen dieser Spec sind durch die Freigabe-Antworten (unten) sowie
die Entscheidungen aus der Kritischen Prüfung (zweiter Durchgang, unten) beantwortet. Eine
Rückfrage bleibt tatsächlich offen:

1. **Tatsächliche Auflösung der Fertigungsterminals bestätigen?** Der Schwellenwert für „schmaler
   Bildschirm" ist auf den Bootstrap-Breakpoint `lg` (992 px) festgelegt (Entscheidung zu H-4,
   Kritische Prüfung). Liegt die reale Terminal-Auflösung darunter, ist der Schmalfall der
   Normalfall und die Spalten-Default-Sichtbarkeit (welche Spalten `defaultHidden` sind) ist im
   Dev-Lauf entsprechend zu bedefaulten. →

## Freigabe-Antworten (Mensch füllt aus — Schranke 1)

1. → **Als zusaetzliche Etappen im BESTEHENDEN Epic-Worktree, nicht als eigener Worktree.**
   Drei Gruende, der erste ist zwingend:
   - Die betroffenen Views existieren **nur** in
     `.claude/worktrees/2026-08-07-ideal-teile-1-5` — nicht auf `main`. Ein neuer Worktree von
     `main` faende nichts zum Anfassen.
   - Beide neuen Specs sind an **dieselbe Vorbedingung** gebunden wie der Epic selbst: ein
     befuelltes IDEAL-Testsystem. Getrennte Merges hiessen mehrere Testrunden auf dieselben
     fehlenden Daten — exakt das Argument, mit dem die Teile 1-5 gebuendelt wurden.
   - Ein Merge, eine Abnahme, ein Deploy statt dreier.
   **Preis, bewusst akzeptiert:** Der Epic geht von `Testbereit` auf `InUmsetzung` zurueck, und die
   QA (Build/Tests, Evidenz in der Spec) ist am Ende **erneut** zu fahren. Das ist billig, solange
   die Abnahme ohnehin auf Daten wartet — und waere teuer, sobald der Epic gemergt ist. Falls das
   Testsystem absehbar **doch nicht** befuellt wird und der Epic ohne diese Darstellung produktiv
   gehen soll: dann zuerst mergen und beide Specs danach von `main` aus bauen.
2. → **Reihenfolge und Scope-Grenze:**
   - **Zuerst [[2026-08-12-listen-spaltenauswahl]]** (vier echte Tabellen, keine Vorbedingung,
     kleiner Umfang, bringt sofort Nutzen).
   - **Dann diese Spec** — Tree-Table-Umbau, Kontrast, Icons, Baum-Spaltenfilter.
   - **Der Anschluss der Baumanzeige an `column-preferences.js` gehoert in DIESE Spec**, als
     letzter Schritt: Wer die Tabelle baut, verdrahtet sie auch. Kein dritter Spec-Vorgang fuer
     zwanzig Zeilen — die drei Bloecke (`view-config`, `column-config`, Skript-Include) sind dann
     bereits durch die Schwester-Spec erprobt, samt der `ColumnDefinitions`-Registrierung.
   **Scope-Grenze klar:** Die Schwester-Spec fasst die **Baumansicht nicht an**; diese Spec fasst
   die **vier Listen nicht an**. Beruehrungspunkt ist ausschliesslich das Muster.
3. → **Bestaetigt: `Views/Tracking/OseonIndex.cshtml` + `_OseonGroupDetails.cshtml` ist das richtige
   Vorbild.** Meine Backlog-Referenz auf `OseonReporting/_OseonReportingTable.cshtml` war ein
   Fehlgriff — sie stammte aus einer Dateinamen-Suche, nicht aus dem Lesen des Inhalts. Der
   Spec-Lauf hat den Code gelesen; der Code gewinnt. Die Backlog-Notiz
   [[2026-08-12-fa-struktur-darstellung]] ist entsprechend zu korrigieren, damit der falsche
   Verweis nicht weiterwandert.
4. → **Zwei Bedienelemente mit bewusst VERSCHIEDENER Semantik — und beide behalten ihre.**
   - Das bestehende Feld „Knoten hervorheben": **hebt hervor, entfernt nie** (Baum-Integritaet).
     Unveraendert.
   - Die **neuen Spaltenfilter: schraenken ein** — sonst waeren es keine Filter, und der Anwender
     erwartet von einem Spaltenfilter ueberall im WMS dasselbe Verhalten.
   Semantik der Einschraenkung: Treffer sichtbar, **Vorfahrenpfad als Kontext** sichtbar (gedimmt,
   als Nicht-Treffer erkennbar), nicht passende Geschwister **samt Unterbaum** ausgeblendet. Faellt
   eine Struktur dadurch auf null Treffer, verschwindet sie ganz. Ein Zaehler („X von Y Knoten")
   macht die Einschraenkung sichtbar.
   **Arbeitsteilung, die sich daraus ergibt:** Der server-seitige Filter aus Teil 2 entscheidet,
   **welche Strukturen** erscheinen; die Spaltenfilter wirken **innerhalb** der angezeigten
   Strukturen, client-seitig. Das passt zur bestehenden Architektur und vermeidet einen zweiten
   Server-Roundtrip.
   **Pflicht:** Beide Bedienelemente muessen unterscheidbar beschriftet sein (z. B. „hervorheben
   (Struktur bleibt vollstaendig)" vs. „filtern (blendet aus)"), sonst erwartet der Anwender vom
   einen, was das andere tut.
5. → **Nicht am Terminal gegenpruefen, sondern RECHNEN.** Der Kontrast ergibt sich deterministisch
   aus den CSS-Werten — Vorder- und Hintergrundfarbe der betroffenen Klassen ermitteln, Verhaeltnis
   berechnen, gegen WCAG AA pruefen (4,5:1 Fliesstext, 3:1 grosse Schrift/Bedienelemente). Dafuer
   braucht es weder Screenshot noch Terminal, und das Ergebnis ist nachpruefbar statt Geschmack.
   Ein Sichtpruefung am realen Terminal gehoert **zusaetzlich** in die manuelle Test-Checkliste —
   als Bestaetigung, nicht als Messverfahren.
6. → **Lokal fuer FaHierarchy bauen, nicht generalisieren.** Die Baum-Filtersemantik (Vorfahren als
   Kontext, Unterbaum-Ausblendung) unterscheidet sich grundlegend von den Flach-Tabellen-Filtern.
   Eine Abstraktion vor dem zweiten echten Verbraucher hiesse, die Naht zu raten — genau der Fehler,
   den wir bei Teil 3 vermieden haben, indem der gemeinsame Baustein erst mit **drei** realen
   Verbrauchern geschnitten wurde. Taucht ein zweiter Baum auf, wird dann extrahiert.
7. → **Erst pruefen, dann entscheiden — Konsistenz geht vor.** Ist das `bootstrap-icons`-Paket im
   Projekt bereits eingebunden (Layout, `libman.json`, `wwwroot/lib`), wird **es** verwendet.
   Nur wenn nicht, kommen die vier Symbole als **Inline-SVG** (kein neues Paket, kein Font-Ladevorgang,
   `currentColor` erbt die Textfarbe — wichtig fuer den Kontrastpunkt). Vier Icons rechtfertigen
   keine neue Abhaengigkeit; ein bereits vorhandenes Set aber sehr wohl die Wiederverwendung.
   **Unabhaengig von der Technik gilt:** Icon nie alleiniger Bedeutungstraeger (`title`/`aria-label`),
   Farbe nie alleiniger Unterscheider (die Form muss ebenfalls unterscheiden).

## Kritische Pruefung (2026-08-12)

Anwalt-des-Teufels-Durchsicht VOR der Freigabe. Gegengelesen gegen den **echten, aktuellen**
Worktree-Code (`.claude/worktrees/2026-08-07-ideal-teile-1-5`, inkl. Polish-Commit `b6b6d83`):
`Views/FaHierarchy/Index.cshtml`, `_FaHierarchyNode.cshtml`, `Views/Tracking/OseonIndex.cshtml`,
`wwwroot/js/column-preferences.js`, `wwwroot/js/table-filter.js`, `wwwroot/css/site.css`,
`Models/FaHierarchyNode.cs`; sowie die Schwester-Spec [[2026-08-12-listen-spaltenauswahl-spec]],
die Teil-2-Spec [[2026-07-29-standort-ideal-teil-2-spec]] und ADR 0005.

### BLOCKER — vor Freigabe/Dev-Lauf zu klaeren

**B-1 — Freigabe-Antwort 2 und der Spec-Rumpf widersprechen sich beim column-preferences-Anschluss
(gleiche Fehlerklasse wie „stale Rumpf" in der Teil-2-Pruefung, H-4 dort).**
Freigabe-Antwort 2 sagt woertlich: „**Der Anschluss der Baumanzeige an `column-preferences.js`
gehoert in DIESE Spec**, als letzter Schritt". Der Spec-Rumpf sagt an **drei** Stellen das Gegenteil:
Out-of-Scope (Zeilen 102–105: „ist Gegenstand der separaten Spec … diese Spec liefert nur die … 
Tabellenstruktur als Vorbedingung"), Technischer Loesungsentwurf (Zeilen 235–236: „**Kein** Anschluss
an `column-preferences.js` in dieser Spec"), ADR-0005-Abschnitt (Zeilen 262–263: „Spaltenpraeferenzen
… bewusst **out of scope**"). Die Antwort ist die spaetere, spezifischere Entscheidung und hat
Vorrang — aber der Rumpf ist damit stale. Folge: **kein** Akzeptanzkriterium, **kein** Testszenario,
**kein** `affected_code`-Eintrag deckt den Anschluss ab. Insbesondere fehlt
`Models/ViewModels/ColumnDefinitions.cs` in `affected_code`: Die Schwester-Spec belegt (deren
Fachliche Anforderung 2), dass ein neuer `viewKey` **zwingend** dort registriert werden muss, sonst
antwortet die Prefs-API `400` und speichert stillschweigend nichts. Ebenso unerwaehnt bleiben das
`data-view-key`-Attribut an der Tabelle sowie die `#view-config`/`#column-config`-Bloecke. Ein
`/dev`-Lauf auf dem heutigen Rumpf baut den Anschluss **nicht**. In-Scope, Out-of-Scope, Technischer
Entwurf, AKs, Testszenarien und `affected_code` sind vor dem Dev-Lauf an Antwort 2 anzugleichen.

**B-2 — Die Tree-Table-Topologie ist ungeklaert und kollidiert mit den einzeltabellen-basierten
JS-Bausteinen — genau den, die Antwort 2 anschliessen will.**
Spec-Abschnitt 2 (Zeile 133) fordert: „**Jede Struktur-Karte** (`.fa-structure`) rendert ihre Knoten
als **eine** `<table>`". Bei Struktur-Pagination (Default 25 Strukturen/Seite, aus Teil 2) sind das
**bis zu 25 `<table>`-Elemente pro Seite**. Der Code, an den angeschlossen werden soll, ist aber
**strikt einzeltabellen-gebunden**:
- `column-preferences.js` (Zeile 847): `_table = document.querySelector('table[data-view-key]')` —
  **genau eine** Tabelle (die erste). Sichtbarkeit/Breite/Reihenfolge/Zahnrad wuerden nur auf die
  **erste** Struktur der Seite wirken, alle uebrigen blieben unberuehrt.
- `table-filter.js` (Zeilen 126/130): `_table = document.querySelector('.filterable-table')` und
  `_tbody = _table.querySelector('tbody')` — ebenfalls nur die erste Tabelle / das erste `<tbody>`.
- ADR-0005-Fallstrick: „**eine** filterbare Tabelle pro gerenderter Seite".
Zusaetzlich **widerspricht die Ein-Tabelle-pro-Karte-Idee dem eigenen zitierten Vorbild**:
`OseonIndex.cshtml` ist **eine einzige** seitenweite Tabelle (`<table id="oseonTree"
data-view-key="OseonTracking">`, Zeile 100), in der die Gruppen als mehrere `<tbody>`-Bloecke
**innerhalb** derselben Tabelle stehen (Zeilen 134/170) — **nicht** eine Tabelle je Gruppe/Karte.
Entweder (a) eine seitenweite Tabelle wie OSEON — dann kollidiert das mit dem gepolishten
Pro-Struktur-Karten-Layout (jede Struktur ist heute eine Bootstrap-Card mit eigenem
`card-header`, Badges und einer eigenen `fa-head-table`-Kopfdatentabelle) — oder (b) je Struktur
eine Tabelle — dann sind column-preferences **und** der neue Client-Spaltenfilter nur auf der
ersten Struktur funktionsfaehig, ausser man aendert die gemeinsamen JS-Bausteine (was Abschnitt
„keine neue Bibliothek/minimal" und die Schwester-Spec-Grenze „keine Aenderung an
`column-preferences.js`" ausschliessen). Diese Topologie-Entscheidung ist tragend fuer den gesamten
Umbau und muss VOR dem Dev-Lauf fallen; die Spec trifft sie nicht.

**B-3 — „`expandAncestors()` … wird wiederverwendet" ist im Tree-Table-Modell technisch falsch und
widerspricht dem eigenen Abschnitt 2.**
Technischer Loesungsentwurf (Zeilen 228–230): „Bestehender `expandAncestors()`-Mechanismus (Zeilen
239–253 …) wird wiederverwendet". Der reale `expandAncestors`/`setExpanded`-Code (Index.cshtml,
Zeilen 207–253) haengt **fundamental an DOM-Verschachtelung**: `setExpanded` liest
`querySelector(':scope > .fa-node-children')`, `expandAncestors` laeuft `parentElement` hoch und
sucht `.fa-node-children`-Container. Ein Tree-**Table** nach OSEON-Vorbild rendert alle Ebenen als
**flache Geschwister-`<tr>`** in **einem** `<tbody>` (Sichtbarkeit ueber `display:none` je Zeile,
Eltern-Bezug ueber `data-parent-id`) — es gibt dann **keine** `.fa-node-children`-Container mehr,
`expandAncestors`/`setExpanded` sind nicht wiederverwendbar, sondern **komplett neu zu schreiben**.
Abschnitt 2 (Zeilen 141–142) sagt das sogar selbst („Expand/Collapse ueber Zeilen-Sichtbarkeit …
**statt** ueber `.fa-node-children`-Container-Sichtbarkeit") — und widerspricht damit dem
„wiederverwendet" des Technischen Entwurfs. Konsequenz fuer den Umfang: **der gesamte
Skriptblock** von `Index.cshtml` (Zeilen 201–274: Toggle, „Alle auf/zu", Highlight, Auto-Expand)
muss mitumgeschrieben werden, nicht nur „ein neues, kleines Skript fuer den Spalten-Baumfilter"
(so aber `affected_code`, Zeile 17). Umfang und `affected_code` unterzeichnen den JS-Aufwand.

### SOLLTE — macht den Dev-Lauf sicherer

**S-1 — Die Ziel-Formulierung erzeugt einen Reihenfolge-Scheinzirkel.**
Ziel (Zeilen 51–54): „Das Tree-Table ist ausserdem **Vorbedingung** fuer … die per-Benutzer-
Spaltenauswahl aus [[2026-08-12-listen-spaltenauswahl]]". Das liest sich, als haenge die **ganze**
Schwester-Spec am Tree-Table. Tatsaechlich fasst die Schwester-Spec die Baumanzeige **gar nicht** an
(deren Out-of-Scope) und behandelt vier **flache** Listen, die bereits echte `<table>`s sind und das
Tree-Table **nicht** brauchen. Aufgeloest ist der Zirkel durch Antwort 2 („Schwester zuerst"), aber
die Ziel-Formulierung bleibt irrefuehrend — praezisieren: Vorbedingung ist das Tree-Table nur fuer
den **eigenen** column-prefs-Anschluss der Baumanzeige, nicht fuer die Schwester-Spec.

**S-2 — Icon-Klassifikation ohne Praezedenz fuer die Wurzel.**
Tabelle Zeilen 169–174: Eine Wurzel ist „oberste Zeile (`VaterFA IS NULL`)", eine Baugruppe ist
„`SubFA != 0`". Eine reale Wurzel ist typischerweise die oberste Baugruppe und erfuellt **beide**
Zeilen gleichzeitig. Welches Icon gewinnt, ist nicht ausgeschrieben (die Tabellen-Reihenfolge legt
„Wurzel zuerst" nahe, sagt es aber nicht). Praezedenz explizit festlegen (Wurzel-Check vor
SubFA-Check), sonst raet der Dev-Lauf.

**S-3 — Sortier-Fallstrick der Schwester-Spec trifft auch die Baumanzeige, falls seitenweite
Tabelle mit mehreren `<tbody>`.**
Wird B-2 zugunsten einer OSEON-artigen seitenweiten Tabelle entschieden (ein `<table>`, ein `<tbody>`
je Struktur), gilt der in der Schwester-Spec (deren Fachliche Anforderung 5) belegte Defekt auch
hier: `table-filter.js` sortiert nur das **erste** `<tbody>`. Sobald ein `<th>` `data-sortable`/
`data-filterable` traegt, sortiert ein Klick auf die Spaltenueberschrift nur die erste Struktur —
sichtbar kaputtes Verhalten. Die Spec sagt zur Sortierbarkeit der Baumspalten nichts; entweder
Spalten explizit **nicht** sortierbar auslegen oder dieselbe defensive Linie
(`supportsSortDefault: false`) uebernehmen.

**S-4 — Umfang fuer einen sauberen Dev-Lauf zu gross; Split erwaegen.**
Kontrast-Fix + kompletter Markup-Umbau `_FaHierarchyNode.cshtml` + kompletter JS-Umbau `Index.cshtml`
+ zwei Filtertypen (Freitext **und** neuartiges `<select>`-Widget ohne Repo-Praezedenz) + vier Icons
+ Legende + (per Antwort 2) column-prefs-Anschluss inkl. `ColumnDefinitions`-Registrierung in **einem**
Lauf ist viel — und der riskanteste Teil (Topologie, B-2) ist ungeklaert. Der **Kontrast-Fix** ist
davon sauber abtrennbar (ein bis drei CSS-Zeilen, sofort und risikoarm lieferbar) und sollte nicht
Geisel des grossen Tree-Table-Umbaus sein. Split erwaegen: (a) Kontrast jetzt, (b) Tree-Table +
Filter + Icons + Prefs als eigener, nach der Topologie-Entscheidung geschnittener Lauf.

### HINWEIS

**H-1 — Positiv bestaetigt: Der Kontrast-Root-Cause stimmt und die Zeilenangaben sind NICHT stale.**
`site.css` `.card-header` (Zeilen 110–116) setzt `background-color: var(--ake-primary)` **und**
`color: var(--ake-white)`; `.fa-structure-header` (Index.cshtml Zeile 176) ueberschreibt nur den
Hintergrund auf `#f8f9fa` → weisser Text auf sehr hellem Grau. Die Spec-Referenzen „Zeilen 174–176"
und „`expandAncestors` Zeilen 239–253" decken sich **exakt** mit dem aktuellen Worktree-Stand — der
Verdacht veralteter Zeilennummern bestaetigt sich hier nicht.

**H-2 — Positiv bestaetigt: `Beschaffungsartikel` existiert** als `bool` auf
`Models/FaHierarchyNode.cs` (Zeile 53). Die Vier-Typen-Icon-Klassifikation ist datenseitig baubar,
kein neues Feld noetig — wie die Spec behauptet.

**H-3 — Polish-Ueberlappung korrekt erfasst, aber Rework-Kosten unterzeichnet.** Die Spec erkennt
richtig, dass der Polish (`b6b6d83`) Kopf-Karte, Blatt-/Baugruppe-Icons, `beschichtet`/`Komm.`-Badges,
Baumlinien und rechtsbuendige Mengen bereits gebaut hat, und erweitert nur (2 → 4 Icon-Typen). Nicht
ausgesprochen: Der Tree-Table-Umbau **verwirft** den Grossteil des gerade gebauten flex-basierten
`_FaHierarchyNode.cshtml`-Layouts (ms-auto, inline-Badges, `.fa-node-desc`). Das ist vertretbar, aber
als bewusste Wegwerf-Rework-Entscheidung zu benennen, nicht stillschweigend.

**H-4 — „schmaler Bildschirm" (AK 3, Zeilen 274/303) ist nicht in px definiert.** Fuer einen
objektiv pruefbaren Test fehlt eine Breiten-Schwelle (Fertigungsterminal-Aufloesung benennen). Die
Kontrast-AKs sind dagegen objektiv rechnerisch pruefbar (Antwort 5) — gut.

**H-5 — Protokoll: Die Spec liegt identisch in `entwurf/` UND `freigegeben/`.** Bearbeitet wurde die
`entwurf/`-Version; die Doublette ist vor/bei der Freigabe zu bereinigen (eine Quelle).

NACHBESSERUNG NOETIG: column-preferences-Anschluss aus Antwort 2 in Rumpf/AK/Test/`affected_code`
(inkl. `ColumnDefinitions.cs`) einarbeiten (B-1), Tree-Table-Topologie entscheiden — seitenweite
Einzeltabelle vs. Tabelle-je-Karte — und mit den einzeltabellen-gebundenen JS-Bausteinen in Einklang
bringen (B-2), sowie den JS-Umbau (`expandAncestors`/Toggle neu statt „wiederverwendet") in Umfang
und `affected_code` ehrlich abbilden (B-3).

## ANTWORTEN auf die Kritische Pruefung (2026-08-12, zweiter Durchgang)

**Zu B-2 — Topologie: EINE seitenweite Tabelle, ein `<tbody>` je Struktur. [ENTSCHEIDUNG]**
Nicht eine Tabelle je Karte. Vier Gruende:
- `column-preferences.js` bindet **eine** Instanz je `data-view-key`. N Tabellen waeren N Instanzen,
  die denselben `viewKey` beschreiben — sie ueberschrieben sich gegenseitig.
- Spalten muessen **ueber Strukturen hinweg fluchten**. Sonst sind gespeicherte Breiten und
  Spaltenfilter bedeutungslos, weil jede Karte ihre eigene Spaltenaufteilung haette.
- **Praezedenz im eigenen Haus:** Die Kommissionierlisten sind bereits genau so gebaut — eine
  Tabelle, ein `<tbody>` je `HauptFA`-Gruppe, Gruppen-Kopfzeile. Gleiches Muster, gleiche
  JS-Bausteine, kein Sonderweg fuer diese eine Ansicht.
- Die Karten-Kopfdaten (Montage-Abt./Kunde/Status; bei Kombigeraet mehrere Zeilen +
  Mehrdeutigkeits-Badge, siehe Antwort zu Teil-2-Rueckfrage 1) werden zur **Gruppen-Kopfzeile mit
  `colspan`** — ebenfalls wie in den Listen.
**Ehrlicher Preis:** Die Karten-Optik entfaellt. Bewusste Abwaegung — Ausrichtung und
Wiederverwendung schlagen das Karten-Bild.

**Zu S-3 — Baumspalten sind NICHT sortierbar. Und zwar nicht bloss defensiv.**
Bei einem Baum **ist die Zeilenreihenfolge die Hierarchie**. Eine Spaltensortierung risse Eltern und
Kinder auseinander — sie ist hier semantisch sinnlos, voellig unabhaengig vom Multi-`tbody`-Defekt.
Also: **kein `data-sortable` an den Baumspalten**, `supportsSortDefault: false`. Der Defekt aus der
Schwester-Spec kann die Baumanzeige damit gar nicht treffen. In AK und Umfang festschreiben, damit
es niemand „nachruestet".

**Zu B-1 und B-3 — uebernommen, ohne Einschraenkung.** Der column-prefs-Anschluss aus Antwort 2
gehoert vollstaendig in Rumpf, AK, Testszenarien und `affected_code` (inkl.
`Models/ViewModels/ColumnDefinitions.cs` — ohne den Eintrag antwortet die Prefs-API mit 400). Und
der JS-Teil ist ein **Umbau**, keine Wiederverwendung: `expandAncestors` und die Toggle-Logik
arbeiten heute auf `.fa-node`/`.fa-node-children`-Verschachtelung; in einer flachen Tabellenzeilen-
Struktur gibt es diese DOM-Verschachtelung nicht mehr, die Eltern-Kind-Beziehung muss ueber
Daten-Attribute laufen. Das ehrlich als Neuentwicklung ausweisen.

**Zu S-1 — praezisiert.** Das Tree-Table ist Vorbedingung **nur fuer den column-prefs-Anschluss der
Baumanzeige selbst**, nicht fuer die Schwester-Spec (die vier flache, bereits echte Tabellen
behandelt und die Baumansicht nicht anfasst). Ziel-Formulierung entsprechend umschreiben.

**Zu S-2 — Praezedenz explizit: Wurzel-Pruefung VOR SubFA-Pruefung.** Eine Wurzel ist typischerweise
zugleich Baugruppe und erfuellt beide Regeln; das Wurzel-Icon gewinnt. Als geordnete Kette
ausschreiben (Wurzel → Baugruppe → Zukaufteil → Material), nicht als Tabelle, deren Reihenfolge man
erraten muss.

**Zu S-4 — Split angenommen.** Zwei Etappen: **(a) Kontrast-Fix sofort** (ein bis drei CSS-Zeilen,
risikoarm, behebt einen Funktionsausfall) und **(b) Tree-Table + Filter + Icons + Prefs** als
eigener Lauf, geschnitten nach der Topologie-Entscheidung oben. Der Kontrast-Fix darf nicht Geisel
des grossen Umbaus sein — gute Beobachtung.

**Zu H-3 — aufgenommen als bewusste Wegwerf-Entscheidung.** Der Tree-Table-Umbau verwirft den
Grossteil des gerade in `b6b6d83` gebauten flex-basierten Layouts (`ms-auto`, Inline-Badges,
`.fa-node-desc`). Das ist vertretbar — aber es gehoert benannt, nicht stillschweigend gemacht.

**Zu H-4 — Schwelle:** unterhalb des Bootstrap-Breakpoints **`lg` (992 px)** werden die hinteren
Spalten ausgeblendet; die Struktur-Spalte bleibt immer. **Zu bestaetigen:** die tatsaechliche
Aufloesung der Fertigungsterminals — liegt sie darunter, ist der Schmalfall der Normalfall und die
Spaltenauswahl entsprechend zu bedefaulten.

**Zu H-5 — Doppelablage aufloesen, bevor irgendetwas gebaut wird.** Massgeblich ist die
`entwurf/`-Fassung (hier wurde gearbeitet); die Kopie in `freigegeben/` ist verfrueht und inhaltlich
veraltet. Zwei Dateien mit demselben Slug und verschiedenem Inhalt sind genau die Falle, die uns bei
der Postman-Spec fast erwischt haette — nur diesmal ueber zwei Dateien statt innerhalb einer.
**Die `freigegeben/`-Kopie entfernen** (Ordner-Geste des Menschen).

### Nachbesserung (2026-08-12)

Je BLOCKER der Kritischen Pruefung, ausgeraeumt durch die jeweilige Entscheidung im Abschnitt
„ANTWORTEN auf die Kritische Pruefung (zweiter Durchgang)" oben, eingearbeitet in den Rumpf dieser
Fassung:

- **B-1** (column-prefs-Widerspruch zwischen Freigabe-Antwort 2 und Rumpf) — behoben durch
  Entscheidung „Zu B-1 und B-3" (Abschnitt ANTWORTEN, zweiter Durchgang). Eingearbeitet in: Umfang
  (In-Scope Punkt 6, Out-of-Scope-Klarstellung), Fachliche Anforderungen (neuer Abschnitt 6),
  Technischer Loesungsentwurf (column-prefs-Anschluss-Absatz), Listen-View-Pattern-Pflichten (ADR
  0005), Akzeptanzkriterien 10–14, Test-Szenarien (Zahnrad-Dialog + Mehrfach-Struktur-Test),
  `affected_code` (`Models/ViewModels/ColumnDefinitions.cs` neu aufgenommen).
- **B-2** (Tree-Table-Topologie ungeklaert/kollidiert mit einzeltabellen-gebundenem JS) — behoben
  durch Entscheidung „Zu B-2" (Abschnitt ANTWORTEN, zweiter Durchgang): seitenweite `<table
  data-view-key="FaHierarchyStructure">`, ein `<tbody>` je Struktur, analog `OseonIndex.cshtml`.
  Eingearbeitet in: Umfang (In-Scope Punkt 2), Fachliche Anforderungen Abschnitt 2, Technischer
  Loesungsentwurf (Tabellen-Topologie-Absatz), Akzeptanzkriterium 2, `affected_code`
  (`Index.cshtml`/`_FaHierarchyNode.cshtml`).
- **B-3** (JS-Umbau als „Wiederverwendung" bezeichnet statt als Neuentwicklung, Umfang
  unterzeichnet) — behoben durch Entscheidung „Zu B-1 und B-3" (Abschnitt ANTWORTEN, zweiter
  Durchgang): kompletter Inline-Skriptblock wird als Neuentwicklung ausgewiesen, ausgelagert nach
  `wwwroot/js/fa-hierarchy-tree.js`. Eingearbeitet in: Technischer Loesungsentwurf
  (JS-Umbau-Absatz), `affected_code` (`fa-hierarchy-tree.js (NEU)` statt „kleines Zusatz-Skript").

Ergaenzend wurden auch die SOLLTE-Punkte S-1 (Ziel-Formulierung praezisiert), S-2 (Icon-Praezedenzkette
Wurzel → Baugruppe → Zukaufteil → Material ausgeschrieben), S-3 (Baumspalten explizit nicht
sortierbar, `SupportsSortDefault: false`) und S-4 (Kontrast-Fix als separater erster Commit vor dem
grossen Umbau, siehe Ziel/Technischer Loesungsentwurf/Deploy) in den Rumpf uebernommen; H-1 bis H-3
sind als Kontext/Begruendung eingeflossen. H-5 (Doppelablage `entwurf/`/`freigegeben/`) betrifft die
Dateiablage, nicht den Rumpfinhalt, und bleibt Sache der Freigabe-Ordnergeste des Menschen.

## QA-Nachweis (2026-08-19)

**Befund vor dem QA-Lauf:** Die Spec war im Worktree bereits vollständig umgesetzt (Commits
`961749f` Kontrast-Fix + `767f06f` Tree-Table/Icons/Baum-Spaltenfilter/column-prefs, siehe
Aufgaben-Notiz `2026-08-12-fa-struktur-darstellung`). Der QA-Lauf hat **nichts neu gebaut**, sondern
den vorhandenen Stand verifiziert.

**Build + Tests (Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`,
Branch `feature/2026-08-07-ideal-teile-1-5`):**
- `dotnet build IdealAkeWms.slnx -c Debug`: **0 Fehler**, 8 Warnungen (bestehende `NU1902`
  MailKit/MimeKit-Advisories, nicht durch diese Spec verursacht).
- `dotnet test IdealAkeWms.Tests -c Debug --no-build`: **1219 erfolgreich, 1 übersprungen
  (Integrationstest `ProductionOrderEagerCreateAgentJobTests`, bestehender Skip), 0 Fehler** —
  deckt sich mit dem zuletzt bekannten Bündel-Stand.
- `dotnet test IDEALAKEWMSService.Tests -c Debug --no-build`: **231 erfolgreich, 0 Fehler.**
- Diff-Nachweis `git diff --stat 961749f^..767f06f`: 9 Dateien geändert, ausschließlich unter
  `IdealAkeWms/`, `IdealAkeWms.Tests/` und `docs/` — keine Datei unter `IDEALAKEWMSService/`, kein
  neues `*/Migrations/*` (bestätigt Deploy-Klassifikation Web ja / Service nein / Migration nein).

**Alle 16 Akzeptanzkriterien einzeln gegen den Code geprüft — alle erfüllt:**

1. **Kontrast:** `.fa-structure-header` in `site.css` (`--ake-text` `#4A494A` auf
   `--ake-light-gray` `#F5F5F5`) — eigenständig **nachgerechnet** (nicht nur den CSS-Kommentar
   übernommen): relative Luminanzen 0,0672 / 0,9133 → Kontrast **8,22:1** ≥ 4,5:1. Weitere
   Badge-/Icon-Kombinationen laut `TS-62.3` (Bootstrap-5-Defaults, plausibel, nicht einzeln
   nachgerechnet).
2. **Tree-Table-Topologie:** `Index.cshtml` — eine `<table id="faTree"
   data-view-key="FaHierarchyStructure">`, `<tbody class="fa-structure-group">` je Struktur mit
   `colspan`-Kopfzeile; `_FaHierarchyNode.cshtml` rendert flache `<tr class="fa-node-row">` mit
   `data-parent-id`/`data-node-id`; nur `td[data-col-key="structure"]` trägt `padding-left` je
   Tiefe, alle anderen Spalten fluchten.
3. **Schmaler Bildschirm:** `@media (max-width: 991.98px)` blendet `subfa`/`quantity` aus,
   `.table-responsive` erlaubt Scroll; `structure`-Spalte hat `min-width: 240px`, wird nie
   ausgeblendet. Reale Terminal-Sichtprüfung bleibt Manual-UAT (siehe Checkliste Punkt 5).
4. **Spaltenfilter-Baumsemantik:** `fa-hierarchy-tree.js` `applyColumnFilters()` — bottom-up
   `keep`-Set (Treffer + Vorfahren), `.fa-filter-hit` vs. `.fa-node-context` (gedimmt), Zähler
   `#faNodeCounter`, `tbody.style.display='none'` bei 0 Treffern einer Struktur.
5. **Auswahlfilter:** `<select class="fa-col-filter">` für `workarea`/`picking-target`/`status`,
   Optionen dynamisch aus `data-f-*`-Attributen der angezeigten Zeilen befüllt (kein neuer
   API-Call).
6. **Zwei Mechanismen:** `#faClientFilter` „Knoten hervorheben (Struktur bleibt vollständig)"
   (nur `.fa-hl`, blendet nichts aus) vs. Spaltenfilter-Zeile „blendet nicht passende Zweige aus" —
   im UI-Text explizit gegenübergestellt (`Index.cshtml` Zeilen 59–65, 84).
7. **Icons + Präzedenz:** `FaNodeClassifier.Classify` (`FaHierarchyTreeViewModel.cs`) — Kette
   Wurzel (`VaterFA == null`) → Baugruppe (`SubFA != 0`) → Zukauf (`Beschaffungsartikel`) →
   Material, exakt wie in Freigabe-Antwort/Kritischer-Prüfung-Entscheidung S-2 verlangt; Legende
   als `<details>`; jedes Icon mit `title`/`aria-label` **und** unterschiedlicher SVG-Form
   (nicht nur Farbe).
8. **OSEON-Verhaltensparität:** `toggleNode`/`toggleStruct`/`btnFaExpandAll`/`btnFaCollapseAll`
   in `fa-hierarchy-tree.js`, Zeilen-Sichtbarkeit statt Container-Sichtbarkeit, Struktur-Ebene
   klappt ganzen `<tbody>` (Kopf + Knoten) auf/zu.
9. **Nicht sortierbar:** kein `data-sortable`/`data-sort-key` an den `<th>`; `#view-config`
   `"supportsSortDefault": false`.
10. **Zahnrad/column-prefs:** `#view-config`/`#column-config`-Blöcke vorhanden,
    `column-preferences.js` **vor** `fa-hierarchy-tree.js` eingebunden, jedes `<th>` trägt
    `data-col-key`.
11. **Persistenz/kein 400:** `ColumnDefinitions.GetByViewKey` hat `case "FaHierarchyStructure"`;
    Regressionstests `UserViewPreferencesApiControllerTests.Get_FaHierarchyStructureViewKey_IsAccepted`
    (→ `NoContentResult`) und `Put_...` (→ `OkResult`) grün.
12. Wie 11 — dieselben Tests decken `GET 204`/`PUT 200`, kein `400` ab.
13. **Locked-Spalten:** `ColumnDefinitions.cs` `FaHierarchyStructure` — `structure`/`matchcode`
    beide mit `Locked: true`.
14. **Multi-`tbody`-Tauglichkeit:** `fa-hierarchy-tree.js` baut `groups` über **alle**
    `tbody.fa-structure-group`-Blöcke der Seite auf (kein „nur erstes `<tbody>`"-Fallstrick, wie
    er bei `table-filter.js` besteht); `column-preferences.js` verschiebt nur Zellen innerhalb
    jeder Zeile, betrifft daher alle `<tbody>`s gleichermaßen. Struktureller Nachweis über Code;
    der visuelle Live-Beweis an ≥2 Strukturen bleibt Manual-UAT (Checkliste Punkt 12).
15. **Keine Regression:** `git diff --stat 961749f^..767f06f` enthält **keine** Datei aus
    `Controllers/FaHierarchyController.cs`, `Services/FaHierarchyTreeBuilder.cs` oder verwandten
    Import-/Filterkarten-Dateien — nur Views/CSS/JS/`ColumnDefinitions.cs`/Tests/Doku.
16. **Keine neue Bibliothek:** `git diff` auf `*.csproj`/`libman.json`/`package.json` zwischen den
    beiden Commits ist **leer**; Icons sind Inline-`<svg>` mit `currentColor`.

**TS-62 (`docs/TESTSZENARIEN.md`, Kapitel „TS-62") geprüft:** 16 Szenarien (TS-62.1–62.16), decken
alle 16 AK 1:1 ab, keine Lücke gefunden. Bereits in `secondbrain/tests/testszenarien-index.md`
(Kapitel 62) indexiert.

**Code-Review (`code-review`-Skill, Effort medium, Diff `961749f^..767f06f`) — Ergebnis nachträglich
eingetroffen und verifiziert.** Beide Funde treffen **keine** der 16 nummerierten
Akzeptanzkriterien (dort explizit geprüft, siehe oben — alle 16 grün), betreffen aber Fliesstext-
Anforderungen bzw. einen Randfall und werden hier transparent für den Menschen dokumentiert, **ohne
selbst gefixt zu werden** (QA-Mandat: melden, nicht fixen):

1. **Einrückungs-Leitlinien fehlen (Fachliche Anforderungen Abschnitt 5).** Vor dem Umbau gab es in
   `Index.cshtml` (`<style>`-Block) `.fa-node-children { border-left: 1px dashed #d5dbe0; }` als
   vertikale Leitlinie je Verschachtelungsebene — durch den Umbau auf flache `<tr>`s entfernt und
   **nicht** ersetzt (verifiziert: kein `border-left`/`dashed` in `site.css` oder den beiden Views
   nach dem Diff). Der Spec-Text fordert explizit: „Einrückungs-Leitlinien je Ebene … im
   Tree-Table-Umbau als vertikale Leitlinie je Einrückungsstufe fortzuführen." Bei tiefen
   Strukturen (≥4 Ebenen) ist die Eltern-Kind-Zuordnung dadurch **nur** über `padding-left`
   erkennbar, ohne verbindende Linie — schwächere Orientierung als im OSEON-Vorbild und als vor
   dem Umbau. Kein AK-Bruch (keiner der 16 AK verlangt die Leitlinie wörtlich), aber eine
   dokumentierte Abweichung vom Fliesstext, die vor dem Merge nachgezogen werden sollte
   (kleiner, risikoarmer CSS-Nachtrag, keine Baum-Logik-Änderung).
   **NACHGEZOGEN (2026-08-19):** Leitlinie wiederhergestellt als vertikale Einrückungs-**Leiter**,
   rein in der Struktur-Zelle: `_FaHierarchyNode.cshtml` exponiert die Einrück-Breite inline als
   `--fa-indent`; `site.css` `.fa-struct-cell` zeichnet ein `repeating-linear-gradient` (Periode 18px =
   Einrück-Schritt, 1px-Linie je Ebene), per `background-size: var(--fa-indent)` auf den
   Einrückungsbereich begrenzt → über Geschwister-Zeilen fluchtende, durchgehende Linien links vom
   Inhalt. Kein JS, kein Markup-Umbau, keine Baum-Logik-Änderung (Timebox eingehalten); rein dekorativ
   (die Einrückung trägt die Information). Build grün; visuelle Bestätigung = Manual-UAT.
2. **Icon-Klassifikation bei Waisen-Strukturen ist ein ungeklärter Randfall.**
   `FaNodeClassifier.Classify` prüft strikt `node.VaterFA == null` für das Wurzel-Icon. Bei einer
   Waisen-Pseudowurzel (`structure.IsOrphan == true`, `FaHierarchyTreeBuilder.cs`) ist `VaterFA`
   **gesetzt** (zeigt nur auf keinen importierten Parent) — der alleinige oberste Knoten dieser
   Struktur (`structure.Roots[0]`, Level 0, optisch identisch zur echten Wurzel jeder anderen
   Struktur) bekommt dadurch **kein** Wurzel-Icon, sondern Baugruppe/Zukauf/Material je nach
   `SubFA`/`Beschaffungsartikel`. Das ist **konsistent mit dem wörtlichen AK-7-Text** („Wurzel …
   `VaterFA IS NULL`"), aber die Spec hat den Waisen-Fall an dieser Stelle nicht bedacht — visuell
   inkonsistent, weil die Waisen-Zeile an derselben strukturellen Position steht wie eine echte
   Wurzel. Kein AK-Bruch, aber eine offene fachliche Frage (Produktentscheidung: soll die
   Waisen-Pseudowurzel das Wurzel-Icon zeigen?), zur Klärung an den Menschen.
   **ENTSCHEIDUNG (2026-08-19, Mensch): Eine Waisen-Pseudowurzel bekommt bewusst KEIN Wurzel-Icon —
   das Verhalten bleibt unverändert.** Begründung: Das **Icon kodiert den Knotentyp**, das **Badge
   den Zustand**. Die „Wurzelhaftigkeit" einer Waise ist kein Knotentyp, sondern ein **Artefakt
   unvollständiger Daten** (der `VaterFA` zeigt auf einen nicht importierten Parent) — und dieser
   Zustand wird bereits durch das **Waisen-Badge** in der Struktur-Kopfzeile („Verwaist — VaterFA … nicht
   gefunden") sichtbar gemacht. Ein Wurzel-Icon würde einen Typ vortäuschen, der nicht vorliegt, und
   die Typ-/Zustands-Trennung aufweichen. Der `FaNodeClassifier.Classify`-Check (`VaterFA == null`)
   bleibt daher absichtlich strikt. **Festgehalten, damit dies nicht später als vermeintliche
   Inkonsistenz „repariert" wird.**

**Nicht automatisiert prüfbar (Manual-UAT, Schranke 2):**
- Sichtprüfung des Kontrasts am realen Fertigungsterminal (Bildschirm/Lichtverhältnisse).
- Reale Terminal-Auflösung vs. 992px-Schwelle (offene Rückfrage der Spec bleibt offen).
- Tree-Table-Ausrichtung an einer produktivnahen Struktur mit ≥4 Ebenen/50+ Positionen.
- Multi-`tbody`-Spaltenauswahl-Effekt visuell an ≥2 gleichzeitig geladenen Strukturen.

### Manuelle Test-Checkliste (aus TS-62, für Schranke 2)

1. `/FaHierarchy` mit ≥2 Strukturen unterschiedlicher Tiefe laden, dazu eine Struktur mit
   ≥4 Ebenen/50+ Positionen: **eine** Tabelle, ein `<tbody>` je Struktur, nur die Struktur-Spalte
   rückt ein, alle übrigen Spalten fluchten über alle Ebenen/Strukturen (TS-62.4).
2. Kontrast der Struktur-Kopfzeile am realen Bildschirm bestätigen (TS-62.3, ergänzend zur
   rechnerischen Prüfung).
3. Alle Badge-Varianten (Kunde, Status, KO/FE-Termin, Montage-Abt., Verwaist, Mehrdeutig, Fehler)
   auf Lesbarkeit prüfen (TS-62.3).
4. Fensterbreite < 992px: hintere Spalten (SubFA, Soll/Fert.) werden ausgeblendet/scrollbar,
   Struktur-Spalte bleibt in voller Breite lesbar (TS-62.5). Reale Terminal-Auflösung notieren.
5. Chevron eines Knotens klappt nur dessen Unterbaum; Klick auf Struktur-Kopfzeile klappt den
   ganzen `<tbody>`-Block; „Alle auf"/„Alle zu" wirken über alle Strukturen; Tastatur
   (Enter/Space) auf fokussiertem Chevron funktioniert (TS-62.6).
6. Je ein Beispielknoten pro Typ (Wurzel, Baugruppe, Zukauf, Material) zeigt ein in Form **und**
   Farbe unterscheidbares Icon mit Tooltip; ein Knoten mit `VaterFA IS NULL` **und** `SubFA != 0`
   zeigt das Wurzel-Icon; Legende ist sichtbar/ausklappbar (TS-62.7).
7. Spaltenfilter (Kopfzeile) auf „Bezeichnung" mit Treffer tief in einer Struktur: Treffer amber
   hervorgehoben, Pfad zur Wurzel gedimmt sichtbar, nicht treffende Geschwister samt Unterbaum
   ausgeblendet, Zähler „X von Y Knoten" korrekt; Struktur mit 0 Treffern verschwindet ganz
   (TS-62.8).
8. Auswahlfilter (`<select>`) für Arbeitsbereich/Komm.-Ziel/Status zeigt nur auf der Seite
   vorkommende Werte und filtert wie der Freitextfilter (TS-62.9).
9. Ohne Vorwissen anhand der Beschriftung erkennen: „Knoten hervorheben" markiert nur,
   Spaltenfilter blendet aus; beide gleichzeitig aktiv verhalten sich wie erwartet (TS-62.10).
10. Kein Spaltenkopf reagiert auf Klick als Sortier-Trigger; Zahnrad-Dialog zeigt keine
    „Standard-Sortierung speichern"-Option (TS-62.11).
11. Zahnrad-Symbol öffnet Offcanvas; eine nicht gesperrte Spalte (z. B. SubFA) ausblenden, Breite
    ändern, umordnen → Reload → bleibt erhalten; zweiter Benutzer hat eigene, unabhängige
    Konfiguration (TS-62.12/62.13).
12. Struktur- und Matchcode-Spalte lassen sich im Zahnrad-Dialog **nicht** ausblenden (TS-62.14).
13. Bei ≥2 gleichzeitig angezeigten Strukturen: eine Spalte ausblenden/umordnen wirkt in **jeder**
    Struktur, nicht nur der ersten (TS-62.15).
14. Regressionsprobe Teil 2: server-seitige Struktur-Filterkarte, Pagination, Tiefen-Cap/Zyklen-/
    Waisen-/Kombigerät-Verhalten und Zugriffsschutz unverändert grün (TS-62.16, bestehende
    TS-58-Szenarien erneut durchspielen).
