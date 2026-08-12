---
type: spec
title: "FA-Struktur: Darstellung, Spaltenfilter und Knoten-Icons (Nachtrag zu Teil 2)"
slug: 2026-08-12-fa-struktur-darstellung-spec
status: Freigegeben
created: 2026-08-12
updated: 2026-08-12
source_backlog: "[[2026-08-12-fa-struktur-darstellung]]"
depends_on: "[[2026-07-29-standort-ideal-teil-2-spec]]"
task: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
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

## Dev-Lauf angehalten (2026-08-12)

`/dev` wurde ausgeloest, aber NICHT umgesetzt — der Gate ist nicht erfuellt:
1. **`status: Entwurf`** (nicht Freigegeben), `freigabe_*` leer — Schranke 1 ist formal nicht genommen
   (das Kopieren nach `specs/freigegeben/` setzt den Status nicht; die Kopie dort ist zudem ein
   untracktes Duplikat).
2. **Offene BLOCKER aus „Kritische Pruefung (2026-08-12)" ungeklaert** — insbesondere die
   **Tabellen-Topologie** (seitenweite `<table>` wie OSEON mit `<tbody>`-Gruppen vs. Tabelle-je-Karte:
   `column-preferences.js`/`table-filter.js` binden strikt an EINE Tabelle/erstes tbody), der
   **column-prefs-Scope-Widerspruch** (Antwort 2 „in diese Spec" vs. Rumpf 3x out-of-scope) und der
   **unterzeichnete JS-Umbau** (`expandAncestors` im Table-Modell neu, ganzer Skriptblock 201-274).
Naechster Schritt: Topologie entscheiden, Rumpf/AK/`affected_code`/column-prefs-Anschluss angleichen,
dann Freigabe. Die Umsetzung gehoert laut Freigabe-Antwort 1 ohnehin als Etappe in den
ideal-teile-1-5-Epic (via `/epic-stage`), nicht als eigenstaendiger `/dev`-Lauf.

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
