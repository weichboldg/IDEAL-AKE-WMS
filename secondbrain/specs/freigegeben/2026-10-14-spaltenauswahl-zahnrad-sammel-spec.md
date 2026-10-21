---
type: spec
title: Spaltenauswahl/Zahnrad — Sammel-Spec (zentraler Toolbar-Host, Gruppenkopf, Speicherfehler, Anzeigename)
slug: 2026-10-14-spaltenauswahl-zahnrad-sammel-spec
status: Testbereit
created: 2026-10-14
updated: 2026-10-21
source_backlog: "[[2026-10-06-zahnrad-host-zentral-marker]]"
sources:
  - "[[2026-10-06-zahnrad-host-zentral-marker]]"
  - "[[2026-10-06-lagerbestellung-zahnrad-unsichtbar]]"
  - "[[2026-10-06-stueckliste-zahnrad-bug-matchcode]]"
  - "[[2026-09-28-gruppenkopf-verschwindet-bei-spaltenumsortierung]]"
  - "[[2026-09-28-spaltenpraeferenz-speicherfehler-still]]"
  - "[[2026-10-13-spalteneinstellungen-anzeigename-vorschlagsliste]]"
task: "[[2026-10-14-spaltenauswahl-zahnrad-sammel-umsetzung]]"
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
affected_code:
  - IdealAkeWms/wwwroot/js/site.js (neu window.tableToolbarHost)
  - IdealAkeWms/wwwroot/js/column-preferences.js (insertGearButton Z. 367-390, setColVisibility Z. 159-177, saveSettings Z. 305-314, resetToDefault Z. 339-348, renderOffcanvasContent Z. 421-559)
  - IdealAkeWms/wwwroot/js/csv-export.js (attach Z. 237-256)
  - IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs (Z. 377, Z. 402 DisplayName)
  - "Views mit Marker: Picking/Bom, WarehousePicking/Details, Beschichtungsauftraege/Index+Details, FaHierarchyBeschichtung/Summiert, PickingLeitstand/Index, FaHierarchy/Index, Tracking/OseonIndex, ProductionOrders/Index, FaCompletion/Index, FaWorklist/Index, Picking/Index"
  - IdealAkeWms/Views/Help/Index.cshtml (Abschnitt Spalten-Einstellungen, Z. 134-146)
  - IdealAkeWms.Tests/Views/BomZahnradHostTests.cs + BeschichtungsauftraegeViewTests.cs Z. 59-60 (abgeloest durch neuen Waechter)
  - IdealAkeWms.Tests/Helpers/TableScriptScopedSelectorTests.cs (ergaenzen)
open_questions: []
epic: false
etappen: []
deploy:
  web: true
  service: false
  migration: false
freigabe_entscheidung: "Freigegeben wie Empfehlungen: 1 ja (Zahnrad einheitlich rechts ueber der Tabelle, auch AKE), 2 ja (Z1 und Z2 mit beheben), 3 ja (Matchcode Stueckliste in die Matchcode-Nachlese). Umsetzung nach dem Merge des IDEAL-Buendels auf neuem Zweig ab main; Zeilennummern gegen main nachmessen."
freigabe_von: "Gerald Weichbold"
freigabe_am: "2026-10-14"
---

# Spaltenauswahl/Zahnrad — Sammel-Spec

> [!warning] Code-Grundlage und Zeilennummern
> Alle Fundstellen sind gegen den **Bündel-Worktree** `.claude/worktrees/2026-08-07-ideal-teile-1-5` gemessen
> (HEAD `d4108f1a`, v1.59.0), nicht gegen `main`. Umgesetzt wird **nach dem Merge des Bündels** in einem neuen
> Zweig ab `main` (worktree/branch bleiben bis dahin leer). **Vor dem /dev alle Zeilennummern gegen `main`
> nachmessen.** Git-Historie konnte der Spec-Lauf nicht abfragen (keine Shell); Commit-Belege stammen aus den
> Brain-Changelogs und sind am Code gegengeprüft.

> [!important] Freigabe-Nachtrag 2026-10-21 (geht dem Rumpf und dem Nachtrag vom 14.10. vor)
> **Ort geändert (Gerald 2026-10-21):** Umsetzung **im Bündel-Worktree** `.claude/worktrees/2026-08-07-ideal-teile-1-5`
> (Zweig `feature/2026-08-07-ideal-teile-1-5`, Stand v1.64.1), nicht nach dem Merge ab `main`. Zeilennummern gegen v1.64.1
> nachmessen. Version **v1.65.0**, Testszenario **TS-98** (TS-93 ist an die Sage-Views vergeben). Die Antworten 1–3 gelten unverändert.

## Ziel / Nutzen (das Warum)

Das Zahnrad (Spaltenauswahl) und seit v1.53 der CSV-Knopf hängen sich per **Nachbar-Heuristik** an „das DIV direkt
vor der Tabelle“. Diese Regel steht nur in Kommentaren und hat dreimal still Fehler erzeugt (Stückliste v1.42,
Lagerbestellung v1.30.1, Leitstand hierarchisch). Ziel: **ein** Ort, der den Platz bestimmt, ein Fallback, der nie
in fremde Elemente greift, plus zwei vorbestehende Fehler im selben Skript (Gruppenkopf verschwindet,
Speicherfehler unsichtbar) und ein Anzeigename.

## Umfang (In-Scope / Out-of-Scope)

### Stand der sechs Quellen

| Quelle | Stand im Bündel | Beleg |
|---|---|---|
| [[2026-10-06-lagerbestellung-zahnrad-unsichtbar]] | **erledigt** — Bündel v1.47.5 (`7e78f9a6`), AKE-Hotfix v1.30.2 (`0a6fb3cd`) | `Views/WarehousePicking/Details.cshtml:53` Hinweis über dem Host, `:58` leerer Host, `:59` `.table-responsive`; Wächter `IdealAkeWms.Tests/Views/BomZahnradHostTests.cs:44-52`; [[2026-10-08-v1-47-5-zahnrad-lagerbestellung-lackflag-kind-sub-fa]], [[2026-10-08-v1-30-2-ake-hotfix-zahnrad-lagerbestellung]], [[fallstricke]] §19 Nachtrag |
| [[2026-10-06-stueckliste-zahnrad-bug-matchcode]] — Teil Zahnrad | **erledigt** — v1.47.3 | `Views/Picking/Bom.cshtml:129-133` Badges/Leerzustand über dem Host, `:142` Host, `:143` `.table-responsive`; Wächter `BomZahnradHostTests.cs:31-36`; [[2026-10-06-v1-47-3-stueckliste-zahnrad-sichtbar]] |
| [[2026-10-06-stueckliste-zahnrad-bug-matchcode]] — Teil Matchcode | **offen**, gehört nicht zur Spaltenauswahl → Rückfrage 3 | `Bom.cshtml:147-184` kein `data-col-key="matchcode"`; deckt sich mit [[2026-09-15-matchcode-nachlese]] Punkt 1 |
| [[2026-10-06-zahnrad-host-zentral-marker]] | **offen** | `column-preferences.js:367-390` unverändert |
| [[2026-09-28-gruppenkopf-verschwindet-bei-spaltenumsortierung]] | **offen** | `column-preferences.js:171-176` ohne `colspan`-Ausnahme |
| [[2026-09-28-spaltenpraeferenz-speicherfehler-still]] | **offen** | `column-preferences.js:305-314`, `:339-348` |
| [[2026-10-13-spalteneinstellungen-anzeigename-vorschlagsliste]] | **offen** | `ColumnDefinitions.cs:377`, `:402` |

### In-Scope

- **A** Zentraler Toolbar-Host für Zahnrad **und** CSV-Knopf (Marker + sicherer Fallback), Wächter.
- **B** Gruppen-Kopfzeilen und Leerzustandszeilen bleiben beim Aus-/Einblenden von Spalten stehen.
- **C** Fehlgeschlagenes Speichern/Zurücksetzen der Spaltenauswahl wird sichtbar gemeldet.
- **D** Anzeigename der beiden Vorschlagslisten-Einträge angleichen.
- Z1/Z2 (Rückfrage 2 = ja, Gerald 2026-10-14).

### Out-of-Scope

- Matchcode-Spalte in der Stückliste (Rückfrage 3, Empfehlung: [[2026-09-15-matchcode-nachlese]]).
- **Zusatzfund Z3, nicht mitbauen:** 18 Views binden `column-preferences.js` ein, liefern aber kein
  `#view-config`/`#column-config` und haben deshalb **gar kein** Zahnrad (`column-preferences.js:863-866` steigt
  aus): ArticleCategories, ArticleAttributes, BdeBookings, BdeMasterData, FaAttributes, MissingParts,
  MissingPartsLager, WorkSteps, ProductionWorkplaces, OrderRecipients, Workstations, SyncLog, Tracking/Index,
  Tracking/ByWorkplace, WarehouseRequisitions, WarehousePicking/Index, Users, Roles. Belegt durch Grep
  `id="view-config"` (17 Treffer, keine der genannten). Damit ist die Aussage der Marker-Notiz „Roles, Users,
  Workstations, WorkSteps, ProductionWorkplaces, OrderRecipients: Zahnrad im blauen `alert-info`“ **am Code
  widerlegt** — dort gibt es kein Zahnrad. Was heute tatsächlich im Hinweis landet, ist der **CSV-Knopf**
  (`csv-export.js:237-247` mit derselben Heuristik; `Roles/Index.cshtml:16-21` `alert-info` direkt vor
  `.table-responsive` bei Nur-Lesen). Den CSV-Teil behebt Paket A; die fehlende Spaltenauswahl dieser 18 Listen
  (ADR-0005-Pflichtbestandteil) ist eigene Arbeit → eigene Backlog-Notiz durch die Koordination.
- Titel der Druckansichten `FaHierarchyBeschichtung/Print.cshtml:12`/`PrintSummiert.cshtml:12` („Beschichtungsauftrag
  (IDEAL)“) und der Rollenname in `Users/RoleOverview.cshtml:194` — nicht Teil der Aufgabe.
- Kontextmenü-Einträge (`column-preferences.js:683-722`) sind `div` ohne Tastaturbedienung — vorbestehend, nicht hier.

## Fachliche Anforderungen

1. Zahnrad und CSV-Knopf stehen auf jeder Liste an **einem** vorhersehbaren Platz — so, wie die Hilfe es
   beschreibt: „Zahnrad-Icon oben rechts über der Tabelle“ (`Views/Help/Index.cshtml:141`), CSV links daneben
   (`Help/Index.cshtml:253`). Nie in einem versteckten, bedingten oder fremden Element (Filterkasten, Zählzeile,
   Hinweis).
2. Ausblenden oder Umsortieren von Spalten lässt Gruppenköpfe (HauptFA, Struktur, Kombigeräte-Varianten) und
   Leerzustandszeilen unangetastet.
3. Scheitert Speichern oder Zurücksetzen, sieht der Anwender das, ohne dass die Liste blockiert wird (Hausregel
   „melden statt still behandeln“).
4. In den gespeicherten Ansichts-Einstellungen eines Benutzers heißen die Einträge wie die Seite.

Standorte: A–C wirken auf **AKE und IDEAL** (gemeinsames JS). Die Gruppenköpfe aus B gibt es nur im
hierarchischen Modus (IDEAL, `ProduktionsauftragHierarchisch`) und auf den IDEAL-Listen; im AKE-Flachmodus betrifft
B nur die Leerzustandszeilen. D betrifft nur IDEAL-Listen.

### Freigabe-Nachtrag 2026-10-14 (geht vor, Gerald)

- **Rückfrage 1 = ja:** Zahnrad (und CSV) stehen auf allen Listen einheitlich rechts über der Tabelle — auch auf den
  AKE-Listen FA-Liste, FA-Vervollständigung, FA-Abarbeitungsliste und Kommissionierung (bisher Filterkasten bzw. unter dem
  Schalter). Changelog-Text für Anwender: „Das Zahnrad steht jetzt überall rechts über der Tabelle neben CSV."
- **Rückfrage 2 = ja:** Z1 (Laden scheitert still) und Z2 (Verlassen innerhalb 1,5 s verliert die Änderung) sind
  **In-Scope**; die „nur bei Ja"-Abschnitte (Lösungsentwurf C, AK 21, TS 93.13) gelten unbedingt.
- **Rückfrage 3 = ja:** Matchcode-Spalte der Stückliste ist **Out-of-Scope**; Umsetzung in [[2026-09-15-matchcode-nachlese]] Punkt 1.
- Umsetzung **nach** dem Merge des IDEAL-Bündels auf neuem Zweig ab `main`; Zeilennummern vor dem Start gegen `main` nachmessen.

## Technischer Loesungsentwurf

### A — Zentraler Toolbar-Host

**Ist (Beleg):**
- `column-preferences.js:367-390` `insertGearButton`: nimmt `tableWrapper.previousElementSibling`, sofern
  `DIV` oder `d-flex` (Z. 375), sonst eigener Container (Z. 382-389).
- `csv-export.js:237-247` `attach`: **zweite Kopie** derselben Regel (Kommentar Z. 233-236 nennt das bewusst
  „gleiche Regel“). Läuft für **jede** `table.filterable-table` (Z. 258-261; Layout bindet das Skript auf allen
  Seiten ein, `_Layout.cshtml:417`), also auf 42 View-Dateien.
- Zahnrad-Ansichten = die 17 Views mit `#view-config`. Wohin das Zahnrad heute fällt (Vorgänger von
  `.table-responsive`):

| View | Vorgänger heute | Bewertung |
|---|---|---|
| `Picking/Bom.cshtml:142` | leerer Host | ok (v1.47.3) |
| `WarehousePicking/Details.cshtml:58` | leerer Host | ok (v1.47.5) |
| `Beschichtungsauftraege/Index.cshtml:42`, `Details.cshtml:209` | leerer Host | ok |
| `FaHierarchyBeschichtung/Summiert.cshtml:48` | leerer Host | ok |
| `PickingLeitstand/Index.cshtml:90` | Host — **aber** hierarchisch steht die Zählzeile `:95-98` dazwischen | **falsch** (Zahnrad in der Zählzeile; flach ok) |
| `ProductionOrders/Index.cshtml:66-74`, `FaCompletion/Index.cshtml:54-62`, `FaWorklist/Index.cshtml:90-98`, `Picking/Index.cshtml:39-47` | hierarchisch: Zählzeile `div.small.text-muted`; flach: Filterkarte (`Picking`: Schalterblock `:11-18` bzw. eigener Container) | **falsch** in beiden Modi gegenüber der Hilfe |
| `FaHierarchyKommissionierListen/Index.cshtml:34-69`, `Summiert.cshtml:36-75`, `FaHierarchyVormontage/Index.cshtml:43-69`, `Summiert.cshtml:45-74`, `FaHierarchyBeschichtung/Index.cshtml:36-43` | Filterkarte `.card.filter-card` → Knopf unten links **in** der Karte | gegenüber Hilfe falsch |
| `FaHierarchy/Index.cshtml:89-108`, `Tracking/OseonIndex.cshtml:83-97` | Werkzeugzeile `d-flex justify-content-between` → Zahnrad als drittes Kind | gewollt nah, aber zwischen den Gruppen verteilt |

**Ursache:** Platz wird aus der Nachbarschaft geraten statt angegeben; zwei Skripte raten unabhängig.

**Soll (ponytail: Fix im gemeinsamen Code, Heuristik löschen statt ergänzen):**
1. `site.js` (auf jeder Seite vor allen Listen-Skripten geladen, `_Layout.cshtml:414`) bekommt eine Funktion
   `window.tableToolbarHost(table)`:
   - liefert das erste Element mit Attribut `data-table-toolbar` im Dokument (eine filterbare Tabelle je Seite,
     [[fallstricke]] §3/§22);
   - fehlt es, legt sie **direkt vor** `table.closest('.table-responsive') || table.parentElement` ein
     `<div class="d-flex justify-content-end mb-1" data-table-toolbar></div>` an und liefert es — der zweite Aufrufer
     (Zahnrad nach CSV) findet dann denselben Host.
   - `ponytail:`-Kommentar: dokumentweite Suche, Grenze = eine filterbare Tabelle je Seite; bei Instanz-Umbau
     (§22) auf Suche relativ zur Tabelle umstellen.
2. `insertGearButton` und `csv-export.js attach` rufen nur noch diese Funktion; `previousElementSibling` fliegt in
   beiden raus. Reihenfolge im Host bleibt: CSV (DOMContentLoaded) vor Zahnrad (Idle-Init) → Zahnrad rechts.
3. Marker setzen:
   - vorhandene leere Hosts bekommen `data-table-toolbar`: Bom `:142`, WarehousePicking/Details `:58`,
     Beschichtungsauftraege Index `:42`/Details `:209`, FaHierarchyBeschichtung/Summiert `:48`, PickingLeitstand `:90`
     (steht schon **über** der Zählzeile → hierarchischer Fehler damit behoben);
   - Werkzeugzeilen: Marker auf den Knopf-Container `FaHierarchy/Index.cshtml:94` bzw. `OseonIndex.cshtml:85`
     (Zahnrad rechts neben „Alle auf/zu“);
   - neuer leerer Host mit Marker **über** der Zählzeile (wie Leitstand) in ProductionOrders (vor `:66`),
     FaCompletion (vor `:54`), FaWorklist (vor `:90`, im `else`-Zweig), Picking/Index (vor `:39`, im `else`-Zweig);
   - Filterkarten-Listen (KommListen ×2, Vormontage ×2, Beschichtung Index) **ohne** Marker: der Fallback erzeugt
     genau den gewünschten Host direkt über der Tabelle.
   - CSV-only-Listen (27 View-Dateien ohne `#view-config`): kein Marker, Fallback.
4. Kommentare „MUSS direkter Vorgänger“ an den Hosts (Bom `:135-141`, Details `:54-57`, BeschichtungSummiert
   `:45-47`, Leitstand `:89`, Beschichtungsauftraege `:41`/`:208`) durch einen Einzeiler auf den Marker ersetzen.

**Wächter (Quelltext, ersetzt die drei `*ZahnradHostTests` + `BeschichtungsauftraegeViewTests.cs:59-60`):** neue
Testklasse, z. B. `IdealAkeWms.Tests/Views/TableToolbarHostTests.cs`:
- `column-preferences.js` und `csv-export.js` enthalten kein `previousElementSibling` und rufen `tableToolbarHost`;
- jede View enthält `data-table-toolbar` höchstens einmal, und das Marker-Tag trägt weder `hidden` noch `d-none`
  noch `display:none`;
- jeder `viewKey` aus einem `#view-config`-Block (Regex `"viewKey":\s*"([^"]+)"`) ist in
  `ColumnDefinitions.GetByViewKey` registriert — beantwortet Frage 2 der Speicherfehler-Notiz: strukturell gegen
  die häufigste 400-Ursache ([[fallstricke]] §3). Heute sind alle 17 Keys registriert (`ColumnDefinitions.cs:541-562`
  gegen Grep `id="view-config"`), der Test hält das fest.
- Hinweis: `BomZahnradHostTests.cs` liegt wortgleich auch auf `main` (AKE-Hotfix); nach dem Bündel-Merge ist das
  eine Datei — Ersetzen ist konfliktfrei.

### B — Gruppenkopf bleibt stehen

**Ist:** `setColVisibility` (`column-preferences.js:171-176`) setzt `display` auf `cells[physicalIdx]` **jeder**
`:scope > tbody > tr`. Gruppen- und Leerzustandszeilen haben genau **eine** `<td colspan>`; bei `physicalIdx == 0`
trifft es diese Zelle, die Zeile verschwindet. Möglich, weil gesperrte Spalten trotzdem verschiebbar sind (Ziehgriff
hängt nur an `supportsReorder`, `:450`), z. B. `hauptfa` (`ColumnDefinitions.cs:361`, Locked) nach hinten ziehen,
dann die neue erste Spalte ausblenden. Beim Laden wiederholt `applySettings` (`:130-148`) das bei jedem Aufruf.
Betroffen (Grep `colspan` in den 17 Ansichten): Gruppenkopf KommListen `Index.cshtml:103-104`/`Summiert:104`,
Beschichtung `Index:77`/`Summiert:79`, Vormontage `Index:100`, FA-Struktur `FaHierarchy/Index.cshtml:142-143` und
Kopfdaten-Zeile `:213-214`, hierarchische FA-Liste/Leitstand/Kommissionierung/FA-Vervollständigung/Abarbeitungsliste
(`ProductionOrders/Index.cshtml:138-139`, Kombigeräte-Varianten `:176-177` usw.), dazu alle Leerzustandszeilen
(„Keine … gefunden“). `reorderDomColumns` (`:217-244`) ist für Ein-Zellen-Zeilen harmlos (hängt die eine Zelle nur
neu an).

**Soll:** in `setColVisibility` Zeilen mit **direktem** `td[colspan]`-Kind überspringen:
`if (row.querySelector(':scope > td[colspan]')) return;` — Muster aus `table-filter.js:315`/`:403`, aber mit
`:scope >` (Regel [[fallstricke]] §3 „Jede DOM-Iteration im Tree-Table braucht `:scope >`“; die Varianten-Zeile
trägt eine Kindtabelle). Belegt: in den 17 Ansichten hat keine Datenzeile eine `colspan`-Zelle neben anderen
Zellen (alle `colspan`-Treffer sind Ein-Zellen-Zeilen). `colspan` wird **nicht** verkleinert — ein zu großer
`colspan` ist im HTML zulässig. Filterzeile unverändert (`:164-169`), damit `column-filter-syntax.js:188-205`
(„?“ wandert in die erste sichtbare Filterzelle, liest `style.display`) weiter greift.
`TableScriptScopedSelectorTests` um eine Contain-Assertion auf den neuen Selektor ergänzen.

### C — Speicherfehler sichtbar

**Ist:** `saveSettings` (`:305-314`) prüft `response.ok` nicht; `fetch` wirft bei HTTP-Fehlern nicht → 400/401/500
sind komplett still, nur Netzwerkfehler landen in `console.warn`. `resetToDefault` (`:339-348`) lädt in **beiden**
Fällen neu → bei Fehler sieht der Anwender den alten Stand ohne Meldung. Mögliche Antworten der API
(`Controllers/Api/UserViewPreferencesApiController.cs`): 401 ohne App-Benutzer (`:42-43`), 400 unbekannter viewKey
(`:45-46`) oder > 64 KB (`:48-49`), 500 bei DB-Fehler. Login-Weiterleitung ist ausgeschlossen: `/api/` ist von der
Login-Middleware ausgenommen (`Program.cs:547`).

**Soll (Muster wiederverwenden: `csv-export.js:184-193` `showMessage`):**
- Ein Helfer `showPrefsMessage(text)` in `column-preferences.js`: entfernt einen vorhandenen Hinweis
  (`.column-prefs-msg`), fügt bei `text` ein `div.column-prefs-msg.alert.alert-warning.py-1.px-2.mb-0.me-2.small`
  mit `role="alert"` als erstes Kind in den Toolbar-Host (aus A) ein. Text ist Bedeutungsträger, nicht die Farbe.
- `saveSettings`: `!response.ok` oder `.catch` → Hinweis „Spaltenauswahl konnte nicht gespeichert werden (Fehler
  <Status> | keine Verbindung). Die Anzeige gilt nur bis zum Neuladen.“ + `console.warn` mit Status. Erfolg →
  Hinweis entfernen.
- Ist das Offcanvas offen (es überdeckt rechts den Host), zeigt `renderOffcanvasContent` denselben Text oben im
  Panel (Modulvariable mit dem aktuellen Fehlertext; beim Erfolg geleert, `refreshOffcanvas` zeichnet neu).
- `resetToDefault`: nur bei `response.ok` neu laden; sonst kein Reload, Hinweis „Zurücksetzen fehlgeschlagen
  (Fehler <Status> | keine Verbindung). Die gespeicherte Spaltenauswahl ist unverändert.“
- Kein Toast: Toasts gibt es nur im BDE-Terminal (`Views/BdeTerminal/Index.cshtml:7`); Listen melden per
  Bootstrap-Alert (CSV-Export, TempData). Damit ist Frage 1 der Speicherfehler-Notiz entschieden.
- **Z1 (nur bei Ja zu Rückfrage 2):** `loadSettings` (`:316-337`) behandelt `!resp.ok` und Netzwerkfehler heute
  still als „keine Einstellungen“ (`:320`, `:333-336`) → Standardansicht, und die nächste Änderung überschreibt die
  gespeicherte Auswahl. Soll: Hinweis „Gespeicherte Spaltenauswahl konnte nicht geladen werden (Fehler …).
  Standardansicht wird gezeigt.“ (204 bleibt stiller Normalfall).
- **Z2 (nur bei Ja zu Rückfrage 2):** Speichern ist um `SAVE_DELAY = 1500` ms entprellt (`:20`, `:297-303`);
  navigiert der Anwender vorher weg (z. B. ENTER im Server-Spaltenfilter), verfällt der Timer still. Soll: auf
  `pagehide` einen ausstehenden Timer sofort per `fetch(..., { keepalive: true })` absenden.

### D — Anzeigename

`ColumnDefinitions.cs:377` „Beschichtungsauftrag (IDEAL)“ → **„Vorschlagsliste Beschichtung (IDEAL)“**,
`:402` „Beschichtungsauftrag — Summiert (IDEAL)“ → **„Vorschlagsliste Beschichtung — Summiert (IDEAL)“** — wie der
Seitentitel `FaHierarchyBeschichtung/Index.cshtml:5,15` und `Summiert.cshtml:5,14`. `ViewKey` (erster Parameter,
`ColumnDefinitions.cs:5`) bleibt, sonst gingen gespeicherte Einstellungen verloren. `DisplayName` wird nur in
`Views/Users/Edit.cshtml:196` und `Controllers/UsersController.cs:339` gelesen (Grep `\.DisplayName` über
`ColumnDefinitions`); kein Test prüft den alten Text (Treffer `BeschichtungsauftraegeViewTests.cs:131` betrifft den
Druck-`<title>`, nicht `DisplayName`).

### Regressionsrisiko gemeinsames JS

| Skript | Wirkt auf | Absicherung |
|---|---|---|
| `site.js` (neu `tableToolbarHost`) | alle 42 Views mit `filterable-table` (CSV) + FA-Struktur/OSEON (nur Zahnrad) | Wächter A, TS-98.1 Matrix |
| `csv-export.js attach` | wie oben; CSV-Platz ändert sich auf CSV-only-Listen — CSV kam mit v1.53 im Bündel, in Produktion noch nicht ausgerollt | TS-98.1, Regression CSV-Szenario (Kapitel der v1.53) |
| `column-preferences.js` | 17 Zahnrad-Ansichten, beide Standorte | AK „Bestandsverhalten“, TS-98.5–98.7, TS-91.B9, TS-72 |

Die v1.53-Zusage „Zahnrad bleibt an seinem bisherigen Platz“ (`csv-export.js:235`) wird bewusst abgelöst. Der
Dev-Lauf prüft `docs/TESTSZENARIEN.md` auf Aussagen zum Platz von Zahnrad/CSV (u. a. TS-18.10, TS-79.20,
CSV-Kapitel) und zieht sie nach. **Frontend-Lauf: Skill `frontend-design` Pflicht**, WCAG AA (Hinweistext ≥ 4,5:1,
Knöpfe ≥ 3:1 messen), Tastatur: Zahnrad/CSV per Tab erreichbar, Hinweis stiehlt keinen Fokus.

### Empfohlene Commit-Reihenfolge (flache Spec, ein Dev-Lauf)

A (Host + Marker + Wächter) → B → C → D → Doku/Version. Begründung **kein Epic:** Der Marker ist für B und D keine
Voraussetzung; C braucht nur einen sichtbaren Ort für den Hinweis, den A liefert, aber in derselben Datei im selben
Lauf. Gesamtumfang: drei JS-Dateien, zwölf Ein-Zeilen-View-Änderungen, zwei Strings, ein Testkurs — passt in einen
Lauf; Etappen mit eigenem Zwischenstand brächten nur Verwaltungsaufwand.

## Migrations-/SQL-Auswirkungen

**Keine Migration, kein SQL-Skript, `00_FreshInstall.sql` und `SQL/AgentJobs/` unberührt.** Beleg: kein
Entity-/`DbContext`-Eingriff; `DisplayName` ist eine C#-Konstante im Record `ViewConfig` (`ColumnDefinitions.cs:5`),
nicht in der DB; gespeicherte Präferenzen bleiben JSON-Strings unter unverändertem `ViewKey`
(`UserViewPreferencesApiController.cs:51-56`); der Marker ist ein HTML-Attribut.

## Audit-Feld-Auswirkungen

Keine. Der einzige Schreibweg (`UserViewPreferencesApiController.Put` → `_repository.SaveAsync(userId, viewKey,
json, GetDisplayName(), GetWindowsUserName())`, `:51-56`) bleibt unverändert und setzt die Audit-Angaben wie bisher.
Neue Schreibwege entstehen nicht (Z2 nutzt denselben `PUT`).

## Rollen / Zugriffsfilter

Keine neue Rolle, kein neuer Filter. Die Präferenz-API hat bewusst keinen Rollenfilter, nur „angemeldeter
App-Benutzer“ (`UserViewPreferencesApiController.cs:8-10`, `:74-79`); die Listen behalten ihre Filter. Anzeige der
Namen aus D nur in der Benutzerverwaltung (`[RequireAdminAccess]`, `UsersController.cs:10`).
**Listen-View-Pattern:** keine neue Tabellen-View; Paket A ändert nur, wo Zahnrad/CSV erscheinen.

## Akzeptanzkriterien

**A — Toolbar-Host**
1. `site.js` stellt `window.tableToolbarHost(table)` bereit; es liefert das Element mit `data-table-toolbar`, sonst
   einen neu angelegten Host direkt vor dem Tabellen-Wrapper, der selbst `data-table-toolbar` trägt; ein zweiter
   Aufruf liefert dasselbe Element.
2. `column-preferences.js` und `csv-export.js` enthalten kein `previousElementSibling` mehr und platzieren Zahnrad
   bzw. CSV ausschließlich über `tableToolbarHost` (Quelltext-Wächter grün).
3. Marker wie im Lösungsentwurf A.3 gesetzt (6 vorhandene Hosts, 2 Werkzeugzeilen, 4 neue Hosts über der Zählzeile).
4. Auf allen 17 Zahnrad-Ansichten steht genau ein Zahnrad rechts über der Tabelle (FA-Struktur/OSEON: rechts neben
   „Alle auf/zu“), auf Listen mit CSV der CSV-Knopf direkt links daneben — in IDEAL (hierarchisch) und AKE (flach);
   nie in Filterkarte, Zählzeile, Schalterblock oder Hinweis.
5. Kommissionier-Leitstand hierarchisch: Zahnrad steht **über** der Zählzeile „… Aufträge · … Sub-FAs“, nicht darin.
6. Lagerbestellung-Details: Pflichthinweis `#incomplete-alert` einblenden → Zahnrad bleibt sichtbar und bedienbar
   (Regression TS-18.10).
7. Stückliste: Leerzustand „Alle Ziele ohne Treffer“ und Standardfilter-Badges sichtbar → Zahnrad bleibt sichtbar
   (Regression TS-79.20).
8. Rollen-Liste als Nur-Lesen-Benutzer: CSV-Knopf steht rechts über der Tabelle, nicht im blauen Hinweis.
9. Wächter: jede View hat `data-table-toolbar` höchstens einmal und nie an einem Tag mit `hidden`/`d-none`/
   `display:none`; jeder `viewKey` aus `#view-config` ist in `ColumnDefinitions.GetByViewKey` registriert.

**B — Gruppenkopf**
10. Kommissionierlisten (Liste): `HauptFA` per Ziehgriff nach hinten, dann die nun erste Spalte ausblenden →
    jede HauptFA-Kopfzeile inkl. PDF-Link bleibt sichtbar und klickbar; nach Neuladen ebenso.
11. Dasselbe gilt für Kommissionierlisten Summiert, Vorschlagsliste Beschichtung (Liste, Summiert), Vormontage
    Einzeln, FA-Struktur (Strukturkopf **und** Kopfdaten-Zeile) und die hierarchischen Listen FA-Liste, Leitstand,
    Kommissionierung, FA-Vervollständigung, FA-Abarbeitungsliste (Gruppenkopf und Kombigeräte-Varianten-Zeile).
12. Leerzustandszeilen („Keine … gefunden“) bleiben bei ausgeblendeter erster Spalte sichtbar.
13. Datenzellen werden weiterhin unter der richtigen Überschrift aus-/eingeblendet und umgeordnet; der „?“-Knopf
    der Filtersyntax wandert weiter in die erste sichtbare Filterzelle (Regression TS-91.B9).
14. Der neue Selektor ist `:scope >`-skopiert; `TableScriptScopedSelectorTests` grün und um ihn ergänzt.

**C — Speicherfehler**
15. Antwortet das Speichern mit einem HTTP-Fehler (z. B. 400) oder scheitert die Verbindung, erscheint im
    Toolbar-Host ein Hinweis mit Text und Fehlerstatus („… Fehler 400 …“ bzw. „… keine Verbindung …“),
    `role="alert"`, die Liste bleibt bedienbar (kein Modal, kein Fokusraub).
16. Ist das Spalten-Panel dabei offen, steht derselbe Hinweis oben im Panel.
17. Es steht höchstens ein Hinweis zugleich; der nächste erfolgreiche Speichervorgang entfernt ihn.
18. Scheitert „Auf Standard zurücksetzen“, lädt die Seite **nicht** neu und zeigt „Zurücksetzen fehlgeschlagen …“;
    gelingt es, lädt sie wie bisher neu.
19. Erfolgsfall: kein Hinweis, Verhalten wie bisher.
20. Hinweis erfüllt WCAG AA (Text ≥ 4,5:1 gemessen); Bedeutung über Text, nicht allein über Farbe.
21. *(nur bei Ja zu Rückfrage 2)* Z1: scheitert das Laden (≠ 200/204), erscheint „Gespeicherte Spaltenauswahl konnte
    nicht geladen werden …“; Z2: Spalte ausblenden und innerhalb 1 s per ENTER im Spaltenfilter navigieren →
    nach dem Laden ist die Spalte weiter ausgeblendet.

**D — Anzeigename**
22. Benutzerverwaltung → Benutzer bearbeiten → „Ansichts-Einstellungen“ zeigt „Vorschlagsliste Beschichtung (IDEAL)“
    bzw. „Vorschlagsliste Beschichtung — Summiert (IDEAL)“; die Zurücksetzen-Meldung nennt denselben Namen; zuvor
    gespeicherte Einstellungen dieser Listen sind nach dem Deploy unverändert wirksam.

**Allgemein**
23. Bestandsverhalten unverändert auf allen 17 Zahnrad-Ansichten: Spalten ein/aus, Umordnen, Breite ziehen/
    Doppelklick, Standard-Sortierung, Kontextmenü, Laden gespeicherter Einstellungen; Filter, Sortierung,
    Paginierung und CSV-**Inhalt** unverändert.
24. Build und alle Tests (Web + Service) grün; Version in beiden `AppVersion.cs`, Anwender-Changelog, Hilfeseite
    (Abschnitt Spalten-Einstellungen `Help/Index.cshtml:134-146`: Satz zum Fehlerhinweis) gepflegt.

## Test-Szenarien

Neues Kapitel **TS-98 Spaltenauswahl/Zahnrad** (Nummer beim /dev gegen `docs/TESTSZENARIEN.md` messen; letzte
vergebene laut [[testszenarien-index]] ist TS-92). Regressionen: TS-18.10, TS-79.20, TS-91.B9, TS-72, CSV-Kapitel
(v1.53). Vorbedingung allgemein: Testsystem, Benutzer mit gespeicherten und ohne gespeicherte Einstellungen, einmal
IDEAL (`ProduktionsauftragHierarchisch` an), einmal AKE (aus); Browser Edge.

- **93.1 Platz von Zahnrad und CSV (Matrix).** Jede der 17 Ansichten öffnen (FA-Liste, Leitstand, Kommissionierung,
  FA-Vervollständigung, Abarbeitungsliste mit gewähltem AG, Stückliste, Lagerbestellung-Details, Kommissionierlisten
  Liste/Summiert, Vorschlagsliste Beschichtung Liste/Summiert, Vormontage Einzeln/Summiert, Beschichtungsaufträge
  Liste/Details, FA-Struktur, OSEON-Teileverfolgung), beide Modi. *Erwartet:* genau ein Zahnrad rechts über der
  Tabelle, CSV links daneben (wo vorhanden); FA-Struktur/OSEON rechts neben „Alle auf/zu“. Stichprobe 3 CSV-only-
  Listen (Bestand, Lagerbewegungen, Rollen): CSV rechts über der Tabelle. *Negativ:* Knopf in Filterkarte,
  Zählzeile, Hinweis oder doppelt = Fehler.
- **93.2 Leitstand hierarchisch.** *Erwartet:* Zahnrad über der Zählzeile.
- **93.3 Lagerbestellung-Details Pflichthinweis.** Bestellung mit nicht vollständig gelieferter Position schließen
  wollen → Hinweis erscheint. *Erwartet:* Zahnrad weiter sichtbar, Panel öffnet.
- **93.4 Stückliste Leerzustand.** Kommissionierziel ohne Treffer wählen. *Erwartet:* gelber Hinweis über dem Zahnrad,
  Zahnrad sichtbar.
- **93.5 Nur-Lesen Rollen.** Benutzer ohne Stammdatenrecht → Rollen. *Erwartet:* CSV nicht im blauen Hinweis.
- **93.6 Gruppenkopf.** Kommissionierlisten (IDEAL): Panel → `HauptFA` ans Ende ziehen → „Haupt-Artnr.“ ausblenden.
  *Erwartet:* alle HauptFA-Köpfe mit PDF-Link sichtbar; PDF-Link lädt; F5 → unverändert. Wiederholen in Summiert,
  Vorschlagsliste Beschichtung, Vormontage Einzeln, FA-Struktur (Struktur nach hinten, Matchcode nach hinten,
  „Bezeichnung“ ausblenden → Kopf **und** Kopfdaten-Tabelle sichtbar), FA-Liste hierarchisch mit Kombigerät
  (Varianten-Tabelle bleibt gefüllt). Filter auf nicht vorhandenen Wert → Leerzustandszeile sichtbar.
  *Negativ:* Daten stehen unter falscher Überschrift = Fehler; Spalte wieder einblenden → Ausgangsbild.
- **93.7 „?“-Knopf.** Erste Spalte ausblenden. *Erwartet:* „?“ in der nächsten sichtbaren Filterzelle, Popover öffnet
  (wie TS-91.B9).
- **93.8 Speicherfehler 400.** Liste öffnen, in der Browser-Konsole
  `columnPreferences.getViewConfig().viewKey = 'Unbekannt'`, dann eine Spalte ausblenden, 2 s warten.
  *Erwartet:* Hinweis „… nicht gespeichert … Fehler 400 …“ über der Tabelle; Panel offen → Hinweis auch im Panel;
  Liste bedienbar. F5 → Spalte wieder sichtbar (Bestätigung, dass nicht gespeichert wurde).
- **93.9 Verbindungsfehler.** DevTools → Netzwerk → Anforderungs-URL `/api/user-view-preferences/*` blockieren, Spalte
  ausblenden. *Erwartet:* Hinweis „… keine Verbindung …“. Sperre aufheben, nächste Änderung → Hinweis verschwindet,
  F5 zeigt den neuen Stand.
- **93.10 Zurücksetzen scheitert.** Sperre wie 93.9, Panel → „Auf Standard zurücksetzen“. *Erwartet:* kein Neuladen,
  Hinweis „Zurücksetzen fehlgeschlagen …“. Ohne Sperre: Seite lädt neu im Standard.
- **93.11 Erfolgsfall.** Ohne Eingriff Spalten ändern → kein Hinweis; F5 → Stand erhalten.
- **93.12 Anzeigename.** Als Admin Benutzer mit gespeicherten Einstellungen der Vorschlagsliste Beschichtung
  bearbeiten. *Erwartet:* neue Namen; „Zurücksetzen“ meldet den neuen Namen; vorher gespeicherte Auswahl wirkte bis
  zum Zurücksetzen.
- **93.13 (bedingt, Rückfrage 2)** Z1: GET blockieren, Liste laden → Hinweis „… nicht geladen …“. Z2: Spalte ausblenden
  und sofort ENTER im Spaltenfilter → nach dem Laden bleibt sie ausgeblendet.
- **Tastatur/Kontrast:** Tab erreicht CSV und Zahnrad in Lesereihenfolge; Hinweis per Screenreader angesagt;
  Kontrast Hinweistext gemessen ≥ 4,5:1.

## Etappen (nur bei epic: true)

Entfällt — flache Spec (Begründung im Lösungsentwurf, „Empfohlene Commit-Reihenfolge“).

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | – | – | – |

## Deploy

**Von der QA am 2026-10-21 gegen den echten Diff `d52bc44d..c27f3b6d` bestätigt** (`git diff --stat`: 27 Dateien, +420/−161;
kein QA-Fix-Commit).

- **Web-App:** ja. Geändert unter `IdealAkeWms/`: `wwwroot/js/site.js`, `wwwroot/js/column-preferences.js`, `wwwroot/js/csv-export.js`,
  `Models/ViewModels/ColumnDefinitions.cs` (zwei `DisplayName`), 13 Views mit `data-table-toolbar` (Picking/Bom, Picking/Index,
  WarehousePicking/Details, Beschichtungsauftraege/Index+Details, FaHierarchyBeschichtung/Summiert, PickingLeitstand/Index,
  OperationConfigs/Index, FaHierarchy/Index, Tracking/OseonIndex, ProductionOrders/Index, FaCompletion/Index, FaWorklist/Index),
  `Views/Help/Index.cshtml`, `Views/Help/Changelog.cshtml`, `AppVersion.cs` (1.65.0).
- **Service:** in diesem Spec-Diff nur `IDEALAKEWMSService/AppVersion.cs` (1.65.0), keine Verhaltensänderung. Für diese Spec allein
  nicht nötig; im Bündel wird der Dienst ohnehin neu publiziert (v1.63.0, [[fallstricke]] §5).
- **Migration:** nein. Keine Datei unter `*/Migrations/`, kein `SQL/`-Skript, nichts unter `SQL/Einmalig/`, `00_FreshInstall.sql` und
  `SQL/AgentJobs/` unberührt.
- **Reihenfolge:** Publish Web aus dem Worktree, App-Pool neu starten. Browser-Cache: `site.js`, `csv-export.js` (`_Layout.cshtml:414/417`)
  und `column-preferences.js` laufen über `asp-append-version`, kein harter Reload nötig.
- **Publish-Befehle (im Worktree):**
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  (Dienst nur im Bündel: `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`)
- **Ablauf Mensch:** Publish aus dem Worktree → Testsystem → TS-98 testen → Merge. Nach dem Merge nur dann erneut aus `main`
  publizieren, wenn der Merge getestete Dateien mit parallelen `main`-Änderungen zusammengeführt hat. (Geprüft: `BomZahnradHostTests.cs`
  liegt entgegen Spec-Rumpf Z. 189 **nicht** auf `main` — `git ls-tree main IdealAkeWms.Tests/Views/` ohne Treffer; kein Löschkonflikt.)
- Wirkt an **beiden** Standorten (gemeinsames JS).

**Brain-Pflichten des Dev-Laufs:** [[fallstricke]] §19 additiv nachtragen („Nachbar-Regel abgelöst durch
`data-table-toolbar` + Fallback; Regel ‚direkt vor `.table-responsive`‘ überholt“), §3 Eintrag zum `colspan`-Skip,
`codebase/module.md` (`site.js`: `tableToolbarHost`), [[feature-map]], Changelog, [[testszenarien-index]];
Quell-Notizen per Wikilink auf diese Spec verweisen lassen.

## Offene Rueckfragen

1. **Zahnrad-Platz auf AKE-Listen.** Auf FA-Liste, FA-Vervollständigung, FA-Abarbeitungsliste hängt das Zahnrad heute
   unten links **im Filterkasten**, auf der Kommissionierung (mit Kommissionierer-Zuordnung) unter dem Schalter
   „Alle Kommissionieraufträge anzeigen“. Künftig steht es — wie in der Hilfe beschrieben — rechts über der Tabelle
   neben „CSV“. Das ist für AKE-Anwender eine sichtbare Änderung. **Empfehlung: ja, einheitlich** (Hilfe stimmt dann,
   CSV und Zahnrad stehen überall gleich, der Fallstrick verschwindet). Alternative: Marker in die Filterkarte setzen
   (alter Platz bleibt).
2. **Zusatzfunde Z1 und Z2 mitnehmen?** Z1: Scheitert das *Laden*, zeigt die Liste still die Standardansicht, und die
   nächste Änderung überschreibt die gespeicherte Auswahl. Z2: Wer innerhalb von 1,5 s nach einer Änderung die Seite
   verlässt (z. B. ENTER im Spaltenfilter), verliert die Änderung still. **Empfehlung: beide ja** — je wenige Zeilen in
   derselben Datei, gleiche Fehlerklasse wie die Notiz.
3. **Matchcode in der Stückliste ausgliedern?** Die Notiz [[2026-10-06-stueckliste-zahnrad-bug-matchcode]] wünscht eine
   Matchcode-Spalte (Bildschirm, `PrintBom`, Spaltenauswahl, Filter). Das ist eine neue Datenspalte aus dem
   Artikelstamm in der BOM-Quelle, kein Zahnrad-Thema, und steht bereits in [[2026-09-15-matchcode-nachlese]] Punkt 1.
   **Empfehlung: ja, dort umsetzen** (eigene Spec), diese Spec bleibt reines Frontend.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. Zahnrad-Platz auf AKE-Listen einheitlich rechts über der Tabelle → **Ja (Gerald 2026-10-14).**
2. Zusatzfunde Z1 (Laden still) und Z2 (Verlassen < 1,5 s) mit beheben → **Ja, beide (Gerald 2026-10-14).**
3. Matchcode-Spalte Stückliste in die Matchcode-Nachlese ausgliedern → **Ja (Gerald 2026-10-14).**

## Umsetzung (Dev-Lauf 2026-10-21)

Im Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Basis `d52bc44d` (v1.64.1). Commits `f1108e94` (A–D + Wächter),
`c27f3b6d` (Doku, Hilfe, v1.65.0, TS-98). Aufgabe [[2026-10-14-spaltenauswahl-zahnrad-sammel-umsetzung]].

### Nachmessung gegen v1.64.1

Alle Fundstellen des Rumpfs stimmen mit v1.64.1 überein: Hosts Bom `:142`, WarehousePicking/Details `:58`, Beschichtungsauftraege
`:42`/`:209`, BeschichtungSummiert `:48`, Leitstand `:90`, Zählzeilen ProductionOrders `:66`, FaCompletion `:54`, FaWorklist `:90`,
Picking/Index `:39`, Werkzeugzeilen FaHierarchy `:94`, Oseon `:85`. In `column-preferences.js` sind die Zeilen unverändert. Ausnahme: Der
Scrollbalken-Fix v1.64.1 hat nur `site.js` `initStickyScrollbar` geändert, und der Host steht davor als eigene Funktion. Der Fix ist
unangetastet.

### Abweichungen vom Rumpf (begründet)

1. **Host-Suche zweistufig statt dokumentweit.** Der Rumpf sah vor: „erstes `[data-table-toolbar]` im Dokument, sonst anlegen“. Dabei
   finden auf `BdeMasterData/Index.cshtml:27/79/129` (drei `filterable-table`, drei Reiter) die Tabellen 2 und 3 den Fallback-Host von
   Tabelle 1, alle CSV-Knöpfe stünden über Reiter 1. Umgesetzt ist deshalb: (1) Marker direkt vor dem eigenen Wrapper, (2) expliziter
   Marker im Dokument, (3) Fallback `data-table-toolbar="auto"`, den Stufe 2 nicht findet. AK 1 („zweiter Aufruf liefert dasselbe
   Element“) gilt weiter, über Stufe 1.
2. **18 statt 17 Zahnrad-Ansichten:** `OperationConfigs/Index` (v1.61.0) hat einen Host an `:62`. Er bekommt ebenfalls den Marker.
   Damit sind es 13 markierte Views (7 Hosts, 2 Werkzeugzeilen, 4 neue Hosts).
3. **Wächter ersetzt mehr als genannt:** zusätzlich `OperationConfigsViewTests.ZahnradHost_DirektVorTableResponsive` (dieselbe abgelöste
   Regel). Neu dazu `ColumnPreferencesFehlerhinweisTests` für C/Z1/Z2 (Quelltext). Der Anzeigename-Test steckt in `TableToolbarHostTests`.
4. **Hinweis im Host mit `d-inline-block`:** In den Werkzeugzeilen FA-Struktur/OSEON ist der Host ein einfacher `<div>`, kein Flex-Container.
   So bleibt der Hinweis in der Zeile der Knöpfe.

### Prüfungen

- Matchcode Stückliste (Antwort 3): seit v1.62.0 vorhanden. Belege: `Bom.cshtml:150` `data-col-key="matchcode"`, `ColumnDefinitions.cs:40`,
  `Bom.cshtml:679` `#column-config`, `PrintBom.cshtml:223/256`. Nichts gebaut.
- `colspan` in den 18 Ansichten und ihren Partials: keine Datenzeile hat eine `colspan`-Zelle neben anderen `<td>`. Der einzige Treffer,
  `_FaWorkOperationsModal.cshtml:43`, ist eine Modal-Tabelle ohne `data-view-key`.
- Kontrast des Hinweises: Bootstrap 5.3.3 `alert-warning` #664d03 auf #fff3cd = **7,2 : 1** (berechnet nach WCAG-Formel, Ziel 4,5),
  `site.css` überschreibt nichts.
- Aussagen zum Platz in `docs/TESTSZENARIEN.md` nachgezogen: TS-18.10/v1.47.5, TS-79.20, TS-81, TS-84, TS-86.16 (als überholt markiert),
  TS-95.
- EOL im Index (§21): `docs/TESTSZENARIEN.md`, alle JS, Views und Tests stehen auf `i/lf`.

### Beweis Build/Test (Dev-Lauf)

- `dotnet build IdealAkeWms.slnx`: **0 Fehler**, 28 Warnungen (Bestand).
- `dotnet test IdealAkeWms.slnx`: Web **2138 bestanden, 1 übersprungen, 0 Fehler** (2139). Service **316 bestanden, 0 Fehler**.
- `node vm.Script` Syntaxprüfung von `site.js`, `csv-export.js`, `column-preferences.js`: ok.

### Nicht gebaut (Befunde)

- Z3 (18 Listen binden `column-preferences.js` ohne `#view-config` ein) bleibt Out-of-Scope, wie in der Spec festgelegt.
- `Help/Index.cshtml` „Verfügbar in: Fertigungsaufträge, Kommissionierliste, OSEON Teileverfolgung, Stückliste“ ist veraltet (18 Ansichten).
  Das ist vorbestehend und nicht angefasst.

## QA (2026-10-21)

Unabhängiger QA-Lauf (qa-agent), synchron in diesem Lauf, Diff `d52bc44d..c27f3b6d` (Commits `f1108e94`, `c27f3b6d`) im Worktree
`.claude/worktrees/2026-08-07-ideal-teile-1-5`. Kein QA-Fix-Commit nötig.

### 1. Build und Tests (selbst ausgeführt)

- `dotnet build IdealAkeWms.slnx`: **0 Fehler**, 28 Warnungen (Bestand, z. B. CS8601 `BdeTerminalAuftraegeTests.cs:23`).
- `dotnet test IdealAkeWms.slnx --no-build`:
  - `IdealAkeWms.Tests`: **2138 bestanden, 1 übersprungen, 0 Fehler** (gesamt 2139).
  - `IDEALAKEWMSService.Tests`: **316 bestanden, 0 übersprungen, 0 Fehler** (gesamt 316).

### 2. Code-Review — Befunde

Geprüft: `site.js` (neu `tableToolbarHost`, `initStickyScrollbar` unverändert), `column-preferences.js` (`insertGearButton`,
`setColVisibility`, `showPrefsMessage`/`failureText`, `saveSettings`, `pagehide`, `loadSettings`, `resetToDefault`,
`renderOffcanvasContent`, Init-Reihenfolge `loadSettingsAndApply`), `csv-export.js attach`, 13 Views mit Marker, `ColumnDefinitions`,
`UserViewPreferencesApiController` (401/400-Wege), `_Layout.cshtml` Skriptreihenfolge, alle Views mit `filterable-table`/`data-view-key`,
neue und gelöschte Tests, `docs/TESTSZENARIEN.md` TS-98, Index.

| # | Befund | Schwere | Behandlung |
|---|---|---|---|
| Q1 | **Scrollbalken-Fix v1.64.1 unberührt.** Diff in `site.js` ist nur die vorangestellte Funktion (Z. 4–23); `initStickyScrollbar` fügt Sticky-Bar und Sentinel **nach** dem Wrapper ein (`site.js:48`, `:74`), der Host kommt **davor** (`site.js:21`) — keine Wechselwirkung mit Stufe 1 (`previousElementSibling` des Wrappers). | – (Prüfung) | keine Aktion; Regression TS-95.17–95.20 in TS-98.15 |
| Q2 | **Fehlplatzierung durch Stufe 2 (dokumentweite Marker-Suche)** wäre nur möglich, wenn eine markierte Seite eine zweite `filterable-table` hat. Geprüft: alle 13 markierten Views haben genau eine `filterable-table` (FaHierarchy/OseonIndex: keine, nur Zahnrad), auch keine in ihren Partials; `WarehousePicking/Details.cshtml:313` ist JS-Text. Mehrtabellen-Seite `BdeMasterData/Index` (3) ist unmarkiert → je Tabelle eigener `auto`-Host (Stufe 3). Kein JS fügt Elemente zwischen Host und Wrapper ein (Grep `insertBefore`/`.before(` in `wwwroot/js` und `Views`). | – (Prüfung) | keine Aktion; Grenze steht als `ponytail:`-Kommentar `site.js:9-10`, Wächter `JedeView_HoechstensEinMarker_NieVersteckt` |
| Q3 | Reihenfolge CSV vor Zahnrad: `csv-export.js` hängt auf `DOMContentLoaded` an (`_Layout.cshtml:417`, vor Seiten-Skripten), `column-preferences.js` erst per `requestIdleCallback` nach dem Laden (`loadSettingsAndApply`) → `appendChild` setzt Zahnrad rechts von CSV. Fehlerhinweis per `insertBefore(host.firstChild)` links davon. | – (Prüfung) | keine Aktion; Manual-UAT TS-98.1 |
| Q4 | Bedingte Marker `FaWorklist/Index` und `Picking/Index` stehen im `else`-Zweig; im `if`-Zweig gibt es keine Tabelle (nur `alert-info`) → kein Zahnrad nötig. `OseonIndex`/`FaHierarchy` dito. | – (Prüfung) | keine Aktion |
| Q5 | Fehlerklassen-Durchsicht: keine Werte in `<script>`-Blöcke eingebettet; kein `hidden` an Elementen mit `d-*` (Host `d-flex`, Hinweis `d-inline-block` ohne `hidden`); keine stille Ersatzlogik (im Gegenteil: drei stille Wege jetzt gemeldet); keine Parallel-Array-Bindung; serverseitige Regel unverändert (API prüft `viewKey`/64 KB selbst, `UserViewPreferencesApiController.cs:45-49`); Platzregel jetzt an **einem** Ort (`site.js`) statt zwei Kopien. | – (Prüfung) | keine Aktion |
| Q6 | 401 der Präferenz-API entsteht nur ohne Session-App-Benutzer (`CurrentUserService.cs:55-58`); Listen-Seiten setzen Anmeldung voraus → praktisch nur bei abgelaufener Session. Dann erscheint jetzt der Hinweis „Fehler 401“ statt still — gewollt („melden statt still“). | Hinweis | keine Aktion |
| Q7 | `loadSettings`: Wirft `resp.json()` (ungültige Antwort) oder der Callback, landet das im `.catch` und meldet „(keine Verbindung)“ — Text unpräzise; die doppelte Callback-Ausführung im Fehlerfall ist vorbestehend. | gering | nicht behoben (Randfall, kein Datenverlust); bei Bedarf Text „Antwort unlesbar“ unterscheiden |
| Q8 | Z2: Der `keepalive`-PUT startet erst auf `pagehide`; die Folgeseite liest per GET erst nach `requestIdleCallback`. PUT vor GET ist praktisch gegeben, aber nicht garantiert (zwei Verbindungen). | Hinweis | Manual-UAT TS-98.13b deckt es ab |
| Q9 | Die Wächter sind Quelltext-Prüfungen (Contain-Assertions auf exakte Zeilen); Verhalten im Browser beweisen sie nicht. | Hinweis | AK-Zuordnung unten, Rest Manual-UAT |
| Q10 | `Help/Index.cshtml` „Verfügbar in: …“ nennt nur vier Ansichten (18 tatsächlich) — vorbestehend, vom Dev-Lauf gemeldet. | gering, vorbestehend | nicht im Umfang; Folgearbeit |
| Q11 | Spec-Rumpf Z. 189 („`BomZahnradHostTests.cs` liegt wortgleich auch auf `main`“) stimmt nicht: `git ls-tree main IdealAkeWms.Tests/Views/` ohne Treffer. Folge: kein Löschkonflikt beim Merge. | Hinweis (Doku) | im Deploy-Abschnitt vermerkt |

**Ergebnis Review:** keine blockierenden Befunde.

### 3. Beweisart je Akzeptanzkriterium

| AK | Beweis | Beleg |
|---|---|---|
| 1 | Quelltext automatisiert; Verhalten „zweiter Aufruf = dasselbe Element“ **Manual-UAT** | `TableToolbarHostTests.SiteJs_StelltHostBereit_FallbackNurUeberEigenenWrapper`; TS-98.1 |
| 2 | automatisiert (Quelltext) | `Skripte_PlatzierenNurUeberTableToolbarHost` (2 Fälle) |
| 3 | automatisiert (Quelltext) | `MarkerGesetzt` (13 Fälle), `Marker_StehtUeberDerZaehlzeile` (5 Fälle) |
| 4 | **Manual-UAT** | TS-98.1 |
| 5 | Quelltext-Reihenfolge automatisiert, Sichtbild **Manual-UAT** | `Marker_StehtUeberDerZaehlzeile`; TS-98.2 |
| 6 | **Manual-UAT** | TS-98.3 |
| 7 | **Manual-UAT** | TS-98.4 |
| 8 | **Manual-UAT** | TS-98.5 |
| 9 | automatisiert (Quelltext) | `JedeView_HoechstensEinMarker_NieVersteckt`, `JederViewKeyAusViewConfig_IstRegistriert` |
| 10–12 | **Manual-UAT** (Browser-JS) | TS-98.6 |
| 13 | **Manual-UAT** | TS-98.7 |
| 14 | automatisiert (Quelltext) | `TableScriptScopedSelectorTests.ColumnPreferences_SetColVisibility_SkipsColspanRows`, `ColumnPreferences_HasNoUnscopedTableIteration` |
| 15–17 | Quelltext automatisiert, Verhalten **Manual-UAT** | `ColumnPreferencesFehlerhinweisTests.Hinweis_IstAlertImToolbarHost`, `Speichern_PrueftResponseOk`, `Panel_ZeigtDenselbenHinweis`; TS-98.8/98.9 |
| 18 | Quelltext automatisiert, Verhalten **Manual-UAT** | `Zuruecksetzen_LaedtNurBeiErfolgNeu`; TS-98.10 |
| 19 | **Manual-UAT** | TS-98.11 |
| 20 | **Manual-UAT** (Kontrast rechnerisch 7,2 : 1, Bootstrap 5.3 `alert-warning`) | TS-98.14 |
| 21 | Quelltext automatisiert, Verhalten **Manual-UAT** | `Laden_MeldetFehler_Z1`, `AusstehendesSpeichern_BeiPagehideMitKeepalive_Z2`; TS-98.13 |
| 22 | `DisplayName` automatisiert, Anzeige in der Benutzerverwaltung **Manual-UAT** | `Vorschlagsliste_AnzeigenameWieSeitentitel`; TS-98.12 |
| 23 | **Manual-UAT** | TS-98.15 |
| 24 | automatisiert/geprüft | Build+Tests oben; beide `AppVersion.cs` 1.65.0; `Help/Changelog.cshtml` v1.65.0; `Help/Index.cshtml` Abschnitt Fehlerhinweis |

Nicht grün gezählt: alles mit **Manual-UAT** — Platz der Knöpfe, Gruppenköpfe, Hinweise und `keepalive` sind Browser-JavaScript.

### 4. Geänderte/gelöschte Bestandstests

| Test | Änderung | Klassifikation |
|---|---|---|
| `Views/BomZahnradHostTests.cs` (3 Klassen: `BomZahnradHostTests`, `WarehousePickingDetailsZahnradHostTests`, `FaHierarchyBeschichtungSummiertZahnradHostTests`) | gelöscht | Verhalten bewusst durch die Spec geändert (Lösungsentwurf A „Wächter ersetzt“, AK 2/9): Regel „Host direkt vor `.table-responsive`“ abgelöst; Ersatz `TableToolbarHostTests` |
| `BeschichtungsauftraegeViewTests.ZahnradHost_DirektVorTableResponsive` (Theory, 2 Fälle) + Doc-Kommentar | entfernt | wie oben (Spec Z. 180 nennt ihn ausdrücklich) |
| `OperationConfigsViewTests.ZahnradHost_DirektVorTableResponsive` + Doc-Kommentar | entfernt | wie oben; in der Spec nicht genannt, Dev-Abweichung 3 begründet (dieselbe abgelöste Regel); Ersatz `MarkerGesetzt("OperationConfigs/Index.cshtml")` |
| `TableScriptScopedSelectorTests` | nur ergänzt (neuer Fact) | keine Änderung an Bestandsfällen |

Kein Fall „sonstiges“ (keine Fixture-Anpassung bis grün).

### 5. CLAUDE.md-Checkliste

- Migration/SQL/FreshInstall/AgentJobs: entfällt (kein Schema). Audit-Felder: unverändert (einziger Schreibweg `Put` wie bisher).
- Version 1.65.0 in **beiden** `AppVersion.cs`, Anwender-Changelog und Hilfe gepflegt.
- `docs/TESTSZENARIEN.md` TS-98.1–98.15 vorhanden (ab `docs/TESTSZENARIEN.md:10306`), Platz-Aussagen bei TS-18.10/TS-79.20/TS-86
  nachgezogen (`:3888`, `:9025`, `:9631`); Index-Zeile 98 in [[testszenarien-index]] (Hauptcheckout, Z. 145) vorhanden.
- EOL ([[fallstricke]] §21): `git ls-files --eol` über alle 26 im Diff verbliebenen Dateien → **alle `i/lf`**.
- Brain des Dev-Laufs: [[fallstricke]], `codebase/module.md`, [[feature-map]] erwähnen `tableToolbarHost`/v1.65.0; Brain-Changelog
  `2026-10-21-v1-65-0-spaltenauswahl-zahnrad-toolbar-host.md` vorhanden. Die Aufgabe [[2026-10-14-spaltenauswahl-zahnrad-sammel-umsetzung]]
  steht noch auf `InUmsetzung` (vom QA-Lauf nicht geändert).

## Manuelle Test-Checkliste (Schranke 2)

Testsystem nach Publish Web aus dem Worktree; Edge; je einmal IDEAL (`ProduktionsauftragHierarchisch` an) und AKE (aus).

1. TS-98.1: alle 18 Zahnrad-Ansichten in beiden Modi — genau ein Zahnrad rechts über der Tabelle, CSV direkt links daneben; FA-Struktur
   und OSEON rechts neben „Alle auf/zu“. Nie in Filterkarte, Zählzeile, Schalterblock, Hinweis.
2. TS-98.1 Stichprobe CSV-only: Bestand, Lagerbewegungen, **BDE-Stammdaten alle drei Reiter** (jeder Reiter eigener CSV-Knopf über seiner Tabelle).
3. TS-98.2: Leitstand, FA-Liste, Kommissionierung, FA-Vervollständigung, Abarbeitungsliste hierarchisch — Zahnrad über der Zählzeile.
4. TS-98.3: Lagerbestellung-Details, Pflichthinweis auslösen → Zahnrad sichtbar, Panel öffnet.
5. TS-98.4: Stückliste mit Kommissionierziel ohne Treffer → Hinweis darüber, Zahnrad sichtbar.
6. TS-98.5: Rollen als Nur-Lesen-Benutzer → CSV nicht im blauen Hinweis.
7. TS-98.6: Kommissionierlisten `HauptFA` ans Ende, erste Spalte ausblenden → alle Köpfe + PDF-Link sichtbar, nach F5 ebenso; wiederholen
   in Summiert, Vorschlagsliste Beschichtung, Vormontage, FA-Struktur (Kopf + Kopfdaten), FA-Liste mit Kombigerät; Leerzustandszeile.
8. TS-98.7: erste Spalte ausblenden → „?“ in der nächsten sichtbaren Filterzelle.
9. TS-98.8: Konsole `columnPreferences.getViewConfig().viewKey = "Unbekannt"`, Spalte ausblenden → Hinweis „… (Fehler 400) …“, auch im
   offenen Panel; bei zweiter Änderung nur ein Hinweis; F5 → Spalte wieder sichtbar.
10. TS-98.9: `/api/user-view-preferences/*` im Netzwerk blockieren → „(keine Verbindung)“; Sperre aufheben, Änderung → Hinweis weg.
11. TS-98.10: mit Sperre „Auf Standard zurücksetzen“ → kein Neuladen, „Zurücksetzen fehlgeschlagen …“; ohne Sperre → Neuladen im Standard.
12. TS-98.11: Erfolgsfall ohne Hinweis, F5 behält den Stand.
13. TS-98.12: Benutzerverwaltung → „Vorschlagsliste Beschichtung (IDEAL)“ / „… — Summiert (IDEAL)“, gespeicherte Auswahl wirkt weiter.
14. TS-98.13: a) GET blockieren → „… nicht geladen …“; b) FA-Liste Spalte ausblenden und sofort ENTER im Spaltenfilter → Spalte bleibt
    ausgeblendet (Netzwerk: PUT mit `keepalive` vor dem GET der Folgeseite — Q8).
15. TS-98.14: Tab-Reihenfolge CSV → Zahnrad; Screenreader sagt den Hinweis an.
16. TS-98.15 + TS-95.17–95.20: Bestandsverhalten (ein/aus, umordnen, Breite, Kontextmenü, Sortierung, CSV-Inhalt); Stückliste mit genau
    einem Scrollbalken, keine Seiten-Querscrollleiste.
