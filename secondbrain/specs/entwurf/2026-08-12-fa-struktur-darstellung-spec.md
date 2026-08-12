---
type: spec
title: "FA-Struktur: Darstellung, Spaltenfilter und Knoten-Icons (Nachtrag zu Teil 2)"
slug: 2026-08-12-fa-struktur-darstellung-spec
status: Entwurf
created: 2026-08-12
updated: 2026-08-12
source_backlog: "[[2026-08-12-fa-struktur-darstellung]]"
depends_on: "[[2026-07-29-standort-ideal-teil-2-spec]]"
task: ""
worktree: ""
branch: ""
affected_code:
  - "IdealAkeWms/Views/FaHierarchy/Index.cshtml (bestehend aus Teil 2 — aktuell NUR im noch nicht gemergten Worktree .claude/worktrees/2026-08-07-ideal-teile-1-5, Branch feature/2026-08-07-ideal-teile-1-5, Status Testbereit; nicht in main)"
  - "IdealAkeWms/Views/FaHierarchy/_FaHierarchyNode.cshtml (bestehend, wird von der freifliessenden Div-Struktur auf eine Tree-Table-Zeile umgebaut)"
  - "IdealAkeWms/wwwroot/css/site.css (Kontrast-Fix .fa-structure-header, neue .fa-tree-table-* / .fa-node-context-Klassen, Legende)"
  - "IdealAkeWms/wwwroot/js/ (neues, kleines Inline- oder Datei-Skript fuer Spalten-Baumfilter inkl. Auswahlfilter-Widget je Spalte — analog dem bestehenden Inline-Skript in Index.cshtml)"
  - "IdealAkeWms/Models/ViewModels/FaHierarchyTreeViewModel.cs (ggf. kleine, rein praesentative Hilfs-Property fuer den Icon-Typ je Knoten — kein neues DB-Feld)"
  - "docs/TESTSZENARIEN.md"
  - "secondbrain/tests/testszenarien-index.md"
  - "GELESEN, NICHT GEAENDERT (Referenz): IdealAkeWms/Views/Tracking/OseonIndex.cshtml, IdealAkeWms/Views/Tracking/_OseonGroupDetails.cshtml, IdealAkeWms/Views/Picking/Bom.cshtml, IdealAkeWms/wwwroot/js/table-filter.js, IdealAkeWms/wwwroot/js/column-preferences.js"
open_questions:
  - "Eigenstaendige Spec/Worktree vs. Etappe im laufenden ideal-teile-1-5-Bundle?"
  - "Reihenfolge/Scope-Grenze zur Schwester-Spec zu [[2026-08-12-listen-spaltenauswahl]]?"
  - "Backlog-Referenz OseonReporting vs. tatsaechliches Vorbild Tracking/OseonIndex — bestaetigen?"
  - "Exakte Baumfilter-Semantik fuer 'nicht passende Geschwister ausblenden'?"
  - "Kontrast-Root-Cause ohne Screenshot am realen Terminal gegenpruefen?"
  - "Auswahlfilter-Widget lokal fuer FaHierarchy oder generischer Baustein?"
  - "Icon-Technik (Inline-SVG aus Bootstrap-Icons-Set statt bootstrap-icons-Paket) bestaetigen?"
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

## Ziel / Nutzen (das Warum)

Teil 2 der Standort-IDEAL-Epic ([[2026-07-29-standort-ideal-teil-2-spec]]) hat die FA-Struktur
erstmals sichtbar gemacht — fachlich korrekt, aber in der Darstellung noch roh: freifliessender
Baum ohne ausgerichtete Spalten, ein Kontrastdefekt im Kopfband, kein Spaltenfilter innerhalb einer
Struktur, keine Knotentyp-Icons. Diese Spec ist ein **reiner Präsentations-Nachtrag** — sie ändert
nichts an `FaHierarchyNode`, am Import oder an der server-seitigen Struktur-Filterlogik aus Teil 2.

Der Kontrastpunkt ist **kein Geschmacksthema**: Die Ansicht läuft an Fertigungsterminals, teils bei
schlechtem Licht. Ein Kopfband, dessen Text WCAG AA nicht erreicht, ist ein Defekt mit
Funktionsausfall (Information nicht lesbar), kein Stilfehler. Das Tree-Table ist ausserdem
**Vorbedingung** für ein zweites, unabhängiges Vorhaben — die per-Benutzer-Spaltenauswahl aus
[[2026-08-12-listen-spaltenauswahl]] — weil `column-preferences.js` über Spaltenindizes einer
echten `<table>` arbeitet, die es in `/FaHierarchy` heute nicht gibt (siehe Offene Rückfrage 2).

**Wichtiger Fund vorab (Code-Lage).** Der Code aus Teil 2 existiert aktuell **ausschliesslich** im
noch nicht gemergten Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`
(Branch `feature/2026-08-07-ideal-teile-1-5`, Spec-Status laut `feature-map.md`: **Testbereit**,
wartet auf Schranke 2 = Manual-UAT + Merge durch den Menschen). In `main` gibt es aktuell **keine**
`Views/FaHierarchy*`-Dateien. Diese Spec beschreibt den Zielzustand auf Basis des Codes in jenem
Worktree; der tatsächliche Startpunkt des Dev-Laufs hängt von Offener Rückfrage 1 ab.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope (diese Runde):**

1. **Kontrast-Fix (Defekt).** Kopfband-Text, Badges (`K-02`, `S-01`, `H4-04`, `Komm.: PG`,
   `beschichtet`, Status-/Waisen-/Fehler-Badges) und der abgesetzte Matchcode in eckigen Klammern
   erreichen WCAG AA — **4,5:1** für Fliesstext, **3:1** für grosse Schrift/Bedienelemente (Chevron,
   Buttons). Prüfkriterium ist der Kontrastwert, nicht subjektives Empfinden.
2. **Umbau von freifliessendem Baum auf Tree-Table.** Jede Struktur-Karte bekommt eine **echte
   `<table>`** (analog `Views/Tracking/OseonIndex.cshtml`) statt verschachtelter `<div>`s. Spalten
   laut Backlog-Tabelle: Struktur (Einrückung+Expander+Icon+FA-Nummer — **einzige** wachsende
   Spalte), Matchcode, Bezeichnung, Arbeitsbereich, Komm.-Ziel, Soll/Fert., SubFA, Status. Auf
   schmalen Bildschirmen werden hintere Spalten ausgeblendet, **nicht** die Struktur-Spalte gequetscht.
3. **Spaltenfilter mit Baum-Semantik** innerhalb einer bereits angezeigten Struktur: Treffer
   hervorheben, Pfad zur Wurzel gedimmt als Kontext sichtbar, nicht treffende Geschwisterzweige
   ausblenden (Semantikdetail siehe Offene Rückfrage 4). Auswahlfilter (`<select>`) für Spalten mit
   kleinem Wertebereich (Arbeitsbereich, Komm.-Ziel, Status). Klare, unterscheidbare Beschriftung
   gegenüber dem bestehenden Freitextfeld „Knoten in angezeigten Strukturen hervorheben".
4. **Icons nach Knotentyp**, abgeleitet aus vorhandenen Daten (`SubFA`, `Beschaffungsartikel`,
   Wurzel-Position) — kein neues Feld. Icon nie alleiniger Bedeutungsträger (Tooltip/`aria-label`),
   Farbe nie alleiniger Unterscheider (Form ergänzt), Bootstrap-Icons-Glyphen als Inline-SVG
   (bestehende Projekt-Konvention, siehe Technischer Lösungsentwurf), kleine ausklappbare Legende.
5. **Verhaltensübernahme vom OSEON-Tracking-Vorbild** (Chevron-Toggle, Expand/Collapse,
   Einrückungs-Leitlinien je Ebene, „Alle auf/zu", dichtes Zeilenraster) — Details und
   Referenzkorrektur siehe Technischer Lösungsentwurf und Offene Rückfrage 3.

**Out-of-Scope:**

- `FaHierarchyNode`, `FaHierarchyOrderInfo`, `FaHierarchySyncService`, Import/Sage-Views — unverändert.
- Die **server-seitige** Struktur-Filterkarte aus Teil 2 (welche Strukturen erscheinen) — unverändert,
  bleibt bestehen. Diese Spec fügt nur den **zusätzlichen, client-seitigen** Spaltenfilter *innerhalb*
  einer bereits angezeigten Struktur hinzu.
- Die Filterlogik/Mengen-Bausteine aus Teil 3–5 (`FaHierarchyListBuilder`, Kommissionier-/
  Beschichtungs-/Vormontagelisten) — die sind bereits echte Tabellen und von diesem Nachtrag nicht
  betroffen (bestätigt die Schwester-Notiz [[2026-08-12-listen-spaltenauswahl]]).
- Zugriffsschutz/Rollen — unverändert (`[RequirePickingOrTrackingOrLeitstandAccess]` bleibt, keine
  neue Rolle, kein neuer Filter, kein neues Feature-Toggle).
- Keine neue Bibliothek (weder Icon- noch Baum- noch Tabellen-Bibliothek) — nur Bootstrap 5 +
  vorhandene projekteigene JS/CSS-Bausteine.
- Das eigentliche `view-config`/`column-config`/`column-preferences.js`-Anschliessen an
  `/FaHierarchy` (per-Benutzer Spaltenauswahl) — das ist Gegenstand der separaten Spec zu
  [[2026-08-12-listen-spaltenauswahl]]; diese Spec liefert nur die dafür nötige Tabellenstruktur
  als Vorbedingung (siehe Offene Rückfrage 2).
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
beschriebene Defekt. Zu verifizieren am realen Terminal, siehe Offene Rückfrage 5.

- Kopfband-Text und Hintergrund zusammen müssen **mindestens 4,5:1** (Fliesstext) erreichen.
- Bedienelemente (Chevron-Toggle-Hitbox, „Alle auf/zu"-Buttons) und grossformatiger Text (FA-Nummer)
  mindestens **3:1**.
- Alle Badge-Varianten (`bg-primary`, `bg-secondary`, `bg-light text-dark border`, `bg-info
  text-dark`, `bg-warning text-dark`, `bg-danger`) und der Matchcode in `text-muted small` sind
  gegen ihren tatsächlichen Hintergrund zu prüfen — nicht nur der auffälligste Fall (Kopfband).
- Fix darf **keine neue Farbpalette** einführen, sondern nutzt die bestehenden `--ake-*`-Variablen
  aus `site.css` konsistent (Konsistenz vor Eigenständigkeit, `frontend-design`-Skill verbindlich).

### 2. Tree-Table statt freifliessender Baum

- Jede Struktur-Karte (`.fa-structure`) rendert ihre Knoten als **eine** `<table>` (nicht
  verschachtelte `<div>`s wie heute in `_FaHierarchyNode.cshtml`). Jede Ebene ist eine `<tr>`;
  **nur** die Struktur-Spalte (`<td>` mit Chevron + Icon + `Artnr`) trägt die Einrückung (per
  `padding-left` proportional zur Tiefe, analog `Views/Tracking/_OseonGroupDetails.cshtml`
  Zeilen 36/75: `padding-left: 28px` / `52px` je Ebene). Alle anderen Spalten (Matchcode,
  Bezeichnung, Arbeitsbereich, Komm.-Ziel, Soll/Fert., SubFA, Status) bleiben senkrecht
  ausgerichtet über alle Zeilen und Ebenen hinweg.
- Knotenidentität bleibt über `data-node-id`/`data-parent-id` (bereits vorhanden in
  `_FaHierarchyNode.cshtml`), Expand/Collapse über Zeilen-Sichtbarkeit (`style="display:none"` auf
  `<tr>`, wie beim OSEON-Tracking-Vorbild) statt über `.fa-node-children`-Container-Sichtbarkeit.
- Schmale Bildschirme (Fertigungsterminal-Breite): hintere Spalten (z. B. SubFA, Soll/Fert.) werden
  ausgeblendet oder scrollbar gemacht — die Struktur-Spalte wird **nie** komprimiert, sie trägt die
  Orientierung (Backlog-Kernregel).

### 3. Spaltenfilter mit Baum-Semantik

- **Zwei unterscheidbare Mechanismen im UI**, klar getrennt beschriftet:
  1. Bestehendes Feld „Knoten in angezeigten Strukturen hervorheben" (`#faClientFilter`) —
     hervorheben **ohne** die Struktur zu verändern, bleibt unverändert in seiner Funktion.
  2. **Neu:** Spaltenfilter-Zeile im Tabellenkopf jeder Struktur (analog der `filter-row` aus
     `table-filter.js`, aber Client-Mode je ADR-0005-Baum-Ausnahme) — filtert **innerhalb** einer
     bereits angezeigten Struktur: Treffer hervorheben, Pfad zur Wurzel **gedimmt** sichtbar lassen
     (neue CSS-Klasse, z. B. `.fa-node-context`), nicht treffende Geschwisterzweige ausblenden.
     Genaue Blende-Semantik: Offene Rückfrage 4.
- **Auswahlfilter (`<select>`)** für Spalten mit kleinem Wertebereich (Arbeitsbereich, Komm.-Ziel,
  Status) statt Freitext — Werteliste wird aus den tatsächlich in der aktuell geladenen Seite
  vorkommenden Werten abgeleitet (kein neuer API-Endpunkt). Für Spalten mit grossem Wertebereich
  (Struktur/Artnr, Matchcode, Bezeichnung, Soll/Fert., SubFA) bleibt Freitext.
- Es gibt im Repo **keinen** bestehenden `<select>`-basierten Spaltenfilter-Präzedenzfall
  (`table-filter.js` kennt nur Freitext mit OR-`,`/NOT-`!`-Mini-Syntax) — dieser Baustein ist
  fachlich neu, aber ohne neue Bibliothek umzusetzen. Reichweite: Offene Rückfrage 6.

### 4. Icons nach Knotentyp

Klassifikation ausschliesslich aus bereits vorhandenen `FaHierarchyNode`-Feldern:

| Typ | Erkennung | Quelle |
|---|---|---|
| Endprodukt / Wurzel | oberste Zeile der Struktur (`VaterFA IS NULL`) | bereits im Tree-Builder |
| Baugruppe (Eigenfertigung) | `SubFA != 0` | `FaHierarchyNode.SubFA` |
| Zukaufteil | `SubFA == 0` **und** `Beschaffungsartikel == true` | `FaHierarchyNode.Beschaffungsartikel` (bool, bereits vorhanden) |
| Lager-/Fertigungsmaterial | `SubFA == 0`, `Beschaffungsartikel == false` | wie oben |

- Zustandsmarker (`Beschichtet`, `Kommissionieren`) bleiben Badges, nicht ins Typ-Icon gemischt
  (bereits so umgesetzt, unverändert zu übernehmen).
- Jedes Icon führt `title=`/`aria-label` mit Klartext (z. B. „Baugruppe — eigener Sub-FA").
- Form unterscheidet zusätzlich zur Farbe (z. B. Kreis vs. Quadrat/Raute vs. Dreieck), nicht nur
  Farbton — heutige Icons (Punkt für Blatt, Box für Baugruppe) sind ein Anfang, decken aber nicht
  alle vier Typen ab (Zukauf/Material aktuell nicht unterschieden).
- Kleine, ausklappbare Legende oberhalb der Strukturliste (z. B. `<details>` oder Bootstrap-Collapse).

### 5. Verhalten wie OSEON-Tracking

**Korrigierte Referenz** (siehe Offene Rückfrage 3): Das im Backlog genannte
`Views/OseonReporting/_OseonReportingTable.cshtml` + `OperationsOverview.cshtml` hat **kein**
Chevron-/Expand-Collapse-Verhalten (geprüft — reine flache Tabelle + KPI-Kacheln, keine
`chevron`/`toggle`-Klassen im Code). Das tatsächlich passende, im Repo vorhandene Vorbild für
„OSEON-Tracking" ist **`Views/Tracking/OseonIndex.cshtml`** + **`Views/Tracking/_OseonGroupDetails.cshtml`**
(„OSEON Teileverfolgung"): eine echte `<table id="oseonTree">`, dreistufiger Baum
(Kundenauftrag → Subauftrag → Arbeitsgang) über `<tr class="oseon-tree-group/-sub/-op">`,
`oseon-toggle`/`oseon-chevron`, „Alle aufklappen"/„Alle zuklappen"-Buttons, Einrückung ausschliesslich
über `padding-left` in der Namens-Spalte, bereits an `column-preferences.js` angeschlossen
(`view-config`/`column-config`). **Zu übernehmen:** dieses Chevron-/Expand-Collapse-Muster,
Einrückungs-Leitlinien je Ebene (in `_FaHierarchyNode.cshtml` bereits ansatzweise über
`.fa-node-children { border-left: 1px dashed }` vorhanden — im Tree-Table-Umbau als vertikale
Leitlinie je Einrückungsstufe fortzuführen), „Alle auf/zu" (in `Index.cshtml` bereits vorhanden,
Verhalten beibehalten), dichtes Zeilenraster (kleine `padding`, wie im OSEON-Vorbild).

## Technischer Lösungsentwurf

- **Markup-Umbau** `_FaHierarchyNode.cshtml`: von rekursivem `<div class="fa-node">`/
  `.fa-node-children` auf rekursiv gerenderte `<tr>`-Zeilen innerhalb der einen `<table>` aus
  `Index.cshtml` je Struktur — analog `_OseonGroupDetails.cshtml` (dort werden Sub-Order- und
  Operation-Zeilen ebenfalls rekursiv/iterativ als Geschwister-`<tr>`s mit `data-parent-*`-Attributen
  gerendert, nicht als DOM-verschachtelte Container). `HasChildren`/`Children` aus
  `FaHierarchyTreeNode` bleiben wie sie sind, nur das Rendering-Target ändert sich.
- **Icons:** Bootstrap-Icons-**Glyphen** als Inline-`<svg>`-Pfaddaten — **keine** neue Bibliothek.
  Im Repo ist `bootstrap-icons` (Font/CSS-Paket) nirgends unter `wwwroot/lib` eingebunden; die
  bestehende, durchgängige Konvention (siehe Chevron- und Box-Icons in `_FaHierarchyNode.cshtml`,
  `OseonIndex.cshtml`) ist, Bootstrap-Icons-Glyphen als kopierte Inline-`<svg>`-Pfade einzubetten.
  Diese Spec setzt dieselbe Konvention für die vier neuen Typ-Icons fort (kein `<i class="bi
  bi-*">`-Webfont, keine CDN-Einbindung) — siehe Offene Rückfrage 7.
- **Kontrast-Fix:** `.fa-structure-header` setzt zusätzlich zum Hintergrund eine dazu passende,
  geprüfte Textfarbe (statt die geerbte `.card-header`-Weiss-Regel unkommentiert zu überschreiben),
  oder verzichtet ganz auf das lokale Überschreiben und übernimmt das dunkle `.card-header`-Band
  aus `site.css` unverändert (einfachste, konsistenteste Lösung — kein neuer Farbwert nötig).
  Endgültige Entscheidung (heller vs. dunkler Header) ist Aufgabe des Dev-Laufs mit dem
  `frontend-design`-Skill; beide Varianten müssen WCAG AA erfüllen und mit dem übrigen
  Corporate Design konsistent sein.
- **Spaltenfilter (Client-Mode, ADR-0005-Baum-Ausnahme):** neue kleine JS-Erweiterung im
  bestehenden Inline-Skript-Block von `Index.cshtml` (oder ausgelagert nach
  `wwwroot/js/fa-hierarchy-tree.js`, falls Umfang das rechtfertigt): pro Spalte ein
  Filter-Input/-Select in einer `filter-row` unter dem `<thead>`; bei Eingabe/Auswahl wird je
  Zeile geprüft, ob sie selbst **oder** irgendein Nachfahre passt (Standard-Baumfilter-Semantik,
  siehe Offene Rückfrage 4) → Zeile bleibt sichtbar (ggf. gedimmt als Kontext), sonst
  `display:none`. Bestehender `expandAncestors()`-Mechanismus (Zeilen 239–253 im aktuellen
  Worktree-Stand) wird wiederverwendet, um den Pfad zur Wurzel bei Treffern automatisch
  aufzuklappen.
- **Bleibt unverändert:** server-seitige Struktur-Filterkarte (`FaHierarchyController.Index`,
  `FilterHauptFas(...)`), Tiefen-Cap/Zyklenschutz (`FaHierarchyTreeBuilder`), Pagination auf
  Struktur-Ebene, Zugriffsschutz `[RequirePickingOrTrackingOrLeitstandAccess]` — diese Spec
  berührt ausschliesslich `Views/FaHierarchy/*.cshtml` + CSS/JS, keine Controller-/Service-Logik.
- **Kein Anschluss an `column-preferences.js`** in dieser Spec (siehe Out-of-Scope) — aber die
  entstehende `<table>` mit `data-col-key` je `<th>` ist die Vorbedingung dafür (Schwester-Spec).

## Migrations-/SQL-Auswirkungen

Keine. Reine Razor-/CSS-/JS-Änderung an bestehenden Views; keine neuen Entitäten, keine
Schema-Änderung, kein neues `AppSettings`/`ServiceSettings`-Feature-Toggle.

## Audit-Feld-Auswirkungen

Keine. Keine Entität wird geschrieben oder verändert; reine Anzeige.

## Betroffene Rollen / Zugriffsfilter

Keine Änderung. `FaHierarchyController` behält `[RequirePickingOrTrackingOrLeitstandAccess]`
(Class-Level, Read-only-View, kein Edit-Split) unverändert bei — dieselbe Rolle/derselbe Filter wie
in Teil 2 festgelegt. Kein neues Feature-Toggle, keine neue Rolle.

## Listen-View-Pattern-Pflichten (ADR 0005)

`/FaHierarchy` bleibt eine **dokumentierte Ausnahme** vom Server-Mode-Spaltenfilter-Pattern (ADR
0005, „hierarchische Baumdarstellung", BOM-Tree-Präzedenzfall) — diese Einstufung ändert sich durch
den Tree-Table-Umbau **nicht**: Der neue Spaltenfilter bleibt Client-Mode (kein
`data-server-column-filter="true"`), weil die server-seitige Struktur-Filterkarte weiterhin die
Struktur-Ebene entscheidet und ein echter Server-Spaltenfilter innerhalb einer Struktur wieder
Kinder verwaisen liesse (dieselbe Begründung wie in Teil 2, Fachliche Anforderungen, Abschnitt
„Server-Filter vs. Baum-Integrität"). Pagination (Struktur-Ebene) und Filterkarte (Struktur-Ebene)
bleiben unverändert bestehen. Spaltenpräferenzen (`column-preferences.js`) sind bewusst
**out of scope** dieser Spec (siehe oben).

## Akzeptanzkriterien

1. Kopfband-Text (`HauptFA <Nr.>`, Badges, Matchcode-Klammern) erreicht gegen seinen tatsächlichen
   Hintergrund mindestens 4,5:1 (Fliesstext) bzw. 3:1 (grosse Schrift/Bedienelemente) — geprüft an
   allen im Backlog genannten Elementen (`K-02`, `S-01`, `H4-04`, `Komm.: PG`, `beschichtet`,
   Matchcode).
2. Jede Struktur wird als **eine** `<table>` mit über alle Ebenen ausgerichteten Spalten
   dargestellt; **nur** die Struktur-Spalte trägt Einrückung — verifiziert an einer Struktur mit
   mindestens 4 Ebenen und 50+ Positionen (produktivnahe Daten, siehe Vorbedingung im Backlog).
3. Auf einem schmalen Bildschirm (Fertigungsterminal-Breite) werden hintere Spalten
   ausgeblendet/scrollbar; die Struktur-Spalte bleibt in voller Breite lesbar, wird nicht komprimiert.
4. Ein Spaltenfilter-Treffer tief in einer Struktur hebt sich hervor, sein Pfad zur Wurzel bleibt
   sichtbar (gedimmt als Kontext), nicht treffende Geschwisterzweige ohne eigenen Treffer werden
   ausgeblendet — kein Knoten verschwindet ohne sichtbaren Zusammenhang zu seinem Elternpfad.
5. Für Arbeitsbereich, Komm.-Ziel und Status steht ein Auswahlfilter (`<select>`) mit den
   tatsächlich vorkommenden Werten zur Verfügung statt eines Freitextfelds.
6. Das bestehende Feld „Knoten in angezeigten Strukturen hervorheben" und der neue Spaltenfilter
   sind im UI durch ihre Beschriftung eindeutig als zwei unterschiedliche Mechanismen erkennbar.
7. Jeder Knoten zeigt ein Typ-Icon (Wurzel/Baugruppe/Zukauf/Material) mit Tooltip/`aria-label` und
   formseitig (nicht nur farblich) unterscheidbarer Grafik; eine Legende ist vorhanden und
   ausklappbar.
8. Chevron-Toggle, Expand/Collapse je Knoten und „Alle aufklappen"/„Alle zuklappen" verhalten sich
   funktional wie im `Views/Tracking/OseonIndex.cshtml`-Vorbild.
9. `FaHierarchyController`, `FaHierarchyTreeBuilder`, die server-seitige Struktur-Filterkarte, der
   Tiefen-Cap/Zyklenschutz und der Zugriffsschutz bleiben byte-identisch unverändert (reine
   View-Änderung, keine Regression an Teil 2).
10. Kein neues NuGet-/npm-/CDN-Paket wird eingebunden (Icons als Inline-SVG, kein Grid-/Tree-Framework).

## Test-Szenarien

Neues Kapitel „IDEAL Teil 2 — FA-Struktur: Darstellung/Spaltenfilter/Icons (Nachtrag)" in
`docs/TESTSZENARIEN.md`, ergänzend zum bestehenden Teil-2-Kapitel:

- **Kontrast-Test:** Kopfband, alle Badge-Varianten und Matchcode-Klammern per Kontrast-Messwerkzeug
  (z. B. Browser-DevTools-Kontrastprüfung) gegen 4,5:1/3:1 geprüft — Soll: alle bestehen.
- **Tree-Table-Grunddarstellung:** produktivnahe Struktur mit ≥4 Ebenen und 50+ Positionen laden;
  prüfen, dass nur die Struktur-Spalte einrückt und alle anderen Spalten über alle Ebenen bündig sind.
- **Schmaler Bildschirm:** Ansicht auf Fertigungsterminal-typischer Breite öffnen; hintere Spalten
  werden ausgeblendet/scrollbar, Struktur-Spalte bleibt unkomprimiert lesbar.
- **Spaltenfilter-Baumsemantik:** Filtertext auf einer Spalte (z. B. Bezeichnung) tief in einer
  Struktur eingeben → Treffer hervorgehoben, Pfad zur Wurzel gedimmt sichtbar, nicht treffende
  Geschwisterzweige ausgeblendet, Struktur bleibt vollständig navigierbar.
- **Auswahlfilter:** Spalte „Arbeitsbereich" zeigt Dropdown mit den in der Seite vorkommenden Werten;
  Auswahl filtert wie beim Freitextfilter.
- **Unterscheidbarkeit der zwei Hervorheben-/Filter-Mechanismen:** Testperson kann anhand der
  Beschriftung ohne Vorwissen sagen, welches Feld nur hervorhebt und welches ausblendet.
- **Icon-Test:** je ein Beispielknoten pro Typ (Wurzel, Baugruppe, Zukauf, Material) zeigt ein
  unterscheidbares Icon mit Tooltip; Legende ist sichtbar/ausklappbar.
- **Verhaltensparität zum OSEON-Vorbild:** Chevron-Rotation, Zeilen-Sichtbarkeit, „Alle auf/zu"
  verhalten sich wie in `Views/Tracking/OseonIndex.cshtml`.
- **Regressionsfall:** server-seitige Struktur-Filterkarte, Tiefen-Cap-/Zyklen-Verhalten und
  Zugriffsschutz aus Teil 2 unverändert (bestehende Teil-2-Testszenarien weiterhin grün).

Nach Klärung der Offenen Rückfragen (insbesondere 4 und 6) zu präzisieren.

## Deploy

- **Web-App:** ja (reine Razor-/CSS-/JS-Änderung an `Views/FaHierarchy/*`, keine Controller-/
  Service-/Migrationsänderung).
- **Service:** nein.
- **Migration:** nein.
- **Publish-Befehle:** `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  (provisorisch, vom Dev-Lauf zu bestätigen). Reihenfolge/Worktree-Frage siehe Offene Rückfrage 1 —
  je nach Antwort ändert sich, aus welchem Branch heraus publiziert wird, nicht der Befehl selbst.

## Offene Rückfragen

1. **Eigenständige Spec/Worktree vs. Etappe im laufenden ideal-teile-1-5-Bundle?** Der Code aus
   Teil 2 existiert aktuell nur im noch nicht gemergten Worktree
   `.claude/worktrees/2026-08-07-ideal-teile-1-5` (Branch `feature/2026-08-07-ideal-teile-1-5`,
   Status Testbereit, wartet auf Schranke 2). Optionen: (a) diese Spec als sechste Etappe in
   denselben Worktree/Branch aufnehmen (ein gemeinsamer Merge mit Teil 1–5), oder (b) als
   eigenständige Folge-Spec mit eigenem, neuem Worktree **nach** dem Merge von Teil 1–5 umsetzen.
   Empfehlung: (b) — rein präsentational und fachlich unabhängig, sollte aber nicht **vor** dem
   Merge von Teil 1–5 starten, sonst entstehen zwei konkurrierende Worktrees auf demselben,
   ungemergten Code.
2. **Reihenfolge/Scope-Grenze zur Schwester-Spec zu [[2026-08-12-listen-spaltenauswahl]]?** Laut
   jener Notiz ist dieses Tree-Table Vorbedingung für die per-Benutzer-Spaltenauswahl der
   FA-Struktur. Soll diese Spec das `view-config`/`column-config`-Markup + die Einbindung von
   `column-preferences.js` für `/FaHierarchy` gleich mitliefern (Scope wächst um einen kleinen,
   klar abgegrenzten Block), oder bleibt das strikt der separaten Spec zu
   [[2026-08-12-listen-spaltenauswahl]] vorbehalten (dann muss deren Reihenfolge „nach dieser
   Spec" dort explizit vermerkt werden)? Empfehlung: strikt getrennt halten
   (Single-Responsibility je Spec), Reihenfolge in beiden Specs explizit vermerken.
3. **Backlog-Referenz `OseonReporting` vs. tatsächliches Vorbild `Tracking/OseonIndex` —
   bestätigen?** Die im Backlog genannten Dateien `Views/OseonReporting/_OseonReportingTable.cshtml`
   + `OperationsOverview.cshtml` haben im aktuellen Code **kein** Chevron-/Expand-Collapse-Verhalten
   (geprüft, reine flache Tabelle + KPI-Kacheln). Das tatsächlich passende Vorbild ist
   `Views/Tracking/OseonIndex.cshtml` + `Views/Tracking/_OseonGroupDetails.cshtml`
   („OSEON Teileverfolgung"). Diese Spec geht von der korrigierten Referenz aus — bitte bestätigen,
   dass das gemeint war (falls tatsächlich ein anderes, hier nicht auffindbares Verhalten aus
   `OseonReporting` gemeint war, bitte konkretisieren).
4. **Exakte Filtersemantik „nicht passende Geschwister ausblenden"?** Gilt das Ausblenden nur für
   Geschwister-**Zweige**, die selbst UND alle Nachfahren keinen Treffer enthalten (Standard-
   Baumfilter-Semantik — Nachfahren eines Treffers bleiben sichtbar), oder soll jeder einzelne
   nicht-treffende Knoten unabhängig von seinen Kindern ausgeblendet werden (riskiert Waisen,
   widerspräche der Baum-Integritätsregel aus Teil 2)? Empfehlung: Standard-Semantik (Zweig bleibt
   sichtbar, wenn er selbst oder irgendein Nachfahre trifft).
5. **Kontrast-Root-Cause ohne Screenshot am realen Terminal gegenprüfen?** Das Backlog referenziert
   eine konkrete Bildschirmansicht („HauptFA 1043111") ohne Anhang (Anhang-Prüfung: es sind laut
   Backlog-Frontmatter keine `anhaenge` vorhanden). Der wahrscheinliche Root Cause wurde im Code
   identifiziert (siehe Fachliche Anforderungen, Abschnitt 1). Zu bestätigen: exakt dieser Defekt,
   oder gibt es am realen Terminal (andere Auflösung/Skalierung/Monitor-Kalibrierung) einen
   weiteren, hier nicht sichtbaren Effekt? Vor dem Dev-Lauf am realen System gegenprüfen.
6. **Auswahlfilter-Widget lokal für FaHierarchy oder generischer Baustein?** Es existiert im Repo
   aktuell kein `<select>`-basierter Spaltenfilter-Präzedenzfall (`table-filter.js` kennt nur
   Freitext mit OR-/NOT-Mini-Syntax). Soll das Widget nur für diese Ansicht gebaut werden, oder ist
   ein generischerer, später wiederverwendbarer Baustein gewünscht (eigener Vorlauf-Baustein statt
   Einzellösung)? Empfehlung: lokal/minimal für FaHierarchy (kein Over-Engineering), Generalisierung
   erst bei einem zweiten tatsächlichen Verbraucher.
7. **Icon-Technik bestätigen?** Bootstrap Icons ist im Repo nicht als Font-/CSS-Bibliothek
   eingebunden (kein `wwwroot/lib/bootstrap-icons`); die bestehende Konvention ist, Bootstrap-Icons-
   Glyphen als kopierte Inline-`<svg>`-Pfaddaten einzubetten (siehe bestehende Chevron-/Box-Icons).
   Diese Spec geht davon aus, dass „Bootstrap Icons verwenden, keine neue Bibliothek" exakt diese
   bestehende Konvention meint, nicht das Hinzufügen des `bootstrap-icons`-Pakets. Bitte bestätigen.

## Freigabe-Antworten (Mensch füllt aus — Schranke 1)

1. →
2. →
3. →
4. →
5. →
6. →
7. →
