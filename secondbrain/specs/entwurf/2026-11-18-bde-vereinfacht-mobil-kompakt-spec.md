---
type: spec
title: BDE-Teileliste kompakt und mobil bedienbar (Filter einklappbar, Kartenansicht)
slug: 2026-11-18-bde-vereinfacht-mobil-kompakt-spec
status: Ueberholt
created: 2026-11-18
updated: 2026-11-18
source_backlog: "[[2026-11-18-bde-vereinfacht-mobil-kompakt]]"
task: ""
jira: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: ""
standort: IDEAL
affected_code:
  - IdealAkeWms/Views/BdeTeileliste/_Kopf.cshtml
  - IdealAkeWms/Views/BdeTeileliste/Index.cshtml
  - IdealAkeWms/Views/BdeTeileliste/Summiert.cshtml
  - IdealAkeWms/wwwroot/css/bde.css
  - IdealAkeWms/Views/Help/Index.cshtml
  - IdealAkeWms/Views/Help/Changelog.cshtml
  - IdealAkeWms/AppVersion.cs
  - IDEALAKEWMSService/AppVersion.cs
  - docs/TESTSZENARIEN.md
open_questions:
  - "Filter auch am Terminal/Desktop kleiner: Eingaben und Knoepfe der Filterkarte von 60 px (btn-lg) auf 44 px, Hinweistext einklappbar? Empfehlung: ja."
  - "Schwelle fuer Kartenansicht und eingeklappte Filterkarte: unter 992 px (Handy und Tablet hochkant) oder nur unter 768 px (nur Handy)? Empfehlung: unter 992 px."
  - "Spaltenfilter sind in der Kartenansicht nicht bedienbar (Kopfzeile entfaellt), nur Anzahl in der Kurzanzeige und Zuruecksetzen. Akzeptiert? Empfehlung: ja."
  - "(Fachbereich: Walter/Benjamin) Mit welchem Konto wird die Teileliste am Handy geoeffnet? Die Seite verlangt einen Web-Benutzer mit zugeordnetem BDE-Terminal. Empfehlung: bestehendes Terminal-Konto, keine Codeaenderung."
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

# BDE-Teileliste kompakt und mobil bedienbar

> [!warning] Aufgegangen im Epic [[2026-11-25-bde-masken-vereinheitlichung]] (Etappe 1, Gerald 2026-11-25). Diese Spec nicht einzeln umsetzen; Inhalt und offene Fragen wandern in die Epic-Spec.

Bezug: [[2026-11-13-bde-vereinfacht-teileliste-blechfertigung-spec]] (Status Testbereit, liegt in `specs/entwurf/`, nicht in
`specs/freigegeben/`), Nachtrag Kontrast v1.99.36. Termin: [[2026-11-18-termin-ideal-1000-notizen]].

> [!important] Umsetzung
> Der Dev-Lauf ruft den Skill **`frontend-design`** ausdrücklich auf (CLAUDE.md, Frontend-Arbeit). Bootstrap 5.3.3 (`wwwroot/lib/bootstrap/dist/css/bootstrap.min.css:2`),
> bestehende Muster, **keine** neue Bibliothek. Code-Belege unten beziehen sich auf den Bündel-Worktree `2026-08-07-ideal-teile-1-5`, Stand v1.99.42.

## Ziel / Nutzen (das Warum)

Walter und Benjamin wollen die Teileliste auch am Handy und Tablet bedienen. Heute ist die Seite für den Terminal-Desktop gebaut: Auf 390 px
Breite füllen Kopf, Werker-Leiste, Reiter und die Filterkarte mit dreizeiligem Hinweistext den ersten Bildschirm. Die Tabelle hat 10 sichtbare
Standardspalten und scrollt quer, „Fertig melden“ steht ganz rechts außerhalb des Bilds. Ziel: weniger Leerraum, Filter eingeklappt mit
Kurzanzeige, je Zeile eine Karte mit Knöpfen ohne Querscrollen. Trefferflächen (≥ 44 px) und Kontrast (WCAG AA) aus v1.99.32/v1.99.36 bleiben.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope** (nur Darstellung):
- `_Kopf.cshtml` (gemeinsam für Einzeln und Summiert): Abstände, Filterkarte unterhalb der Schwelle einklappbar mit Kurzanzeige, Hinweistext einklappbar.
- `Index.cshtml` und `Summiert.cshtml`: Seiten-Scope-Klasse, `data-label` an den Datenzellen, Kennklassen für Auswahl- und Aktionszelle, Checkbox-Trefferfläche.
- `bde.css`: neue Regeln **nur** unter der Seiten-Scope-Klasse `.bde-tl-seite`.
- Hilfe, Anwender-Changelog, Version, TS-Kapitel.

**Out-of-Scope:**
- Keine Daten-, Schema-, Controller-, Service- oder Repository-Änderung, keine Migration, keine neue Einstellung, keine Rolle.
- `site.css`, `site.js`, `table-filter.js`, `column-preferences.js`, `hardware-scanner.js`, `_Pagination.cshtml`: **unverändert** (gemeinsamer Code, siehe Regressionsrisiko).
- `Anmelden.cshtml`: unverändert (Scan-Ziel, siehe AK 12).
- BDE-Terminal (`BdeTerminal/Index`), BDE-Cockpit: unverändert, obwohl sie `bde.css` laden (`BdeTerminal/Index.cshtml:5`, `BdeCockpit/Index.cshtml:3`).
- Spaltenfilter-Bedienung in der Kartenansicht (Rückfrage 3).

## Fachliche Anforderungen

F1. **Weniger Leerraum** auf allen Größen: Abstände zwischen Kopf, Werker-Leiste, Reitern, Filterkarte und Tabelle von `mb-3` auf `mb-2`
    (heute `_Kopf.cshtml:6,21,27,40`); Werker-Leiste unterhalb der Schwelle mit weniger Innenabstand und kleinerer Schrift (heute
    `padding: 0.75rem 1rem; font-size: 1.4rem`, `bde.css:6-14`); Überschrift unterhalb der Schwelle kleiner.

F2. **Filterkarte unterhalb der Schwelle eingeklappt.** Statt der Karte steht ein Knopf über die volle Breite, der zugleich die **Kurzanzeige
    der aktiven Filter** ist, z. B. „Filter: KW 47/2026 – KW 48/2026 · gemeldete ausgeblendet · 2 Spaltenfilter ▾“. Tippen klappt die Karte auf
    (Zustand per `aria-expanded` und Zeichen ▾/▴, nicht nur Farbe). Ab der Schwelle ist die Karte immer offen und der Knopf unsichtbar.
    Kurzanzeige-Regeln: leere KW-Felder → „alle Wochen“; `GemeldeteZeigen` → „gemeldete sichtbar“ bzw. „gemeldete ausgeblendet“;
    Spaltenfilter-Anzahl nur, wenn > 0.

F3. **Filter kleiner** (Umfang am Terminal je Rückfrage 1): Hinweistext (`_Kopf.cshtml:71-75`) in ein `<details>` „Hinweise zur Liste“
    (Muster wie `FaHierarchy/Index.cshtml:9-10`), Eingaben und Knöpfe der Filterkarte 44 px statt 60 px.

F4. **Kartenansicht unterhalb der Schwelle** (Einzeln und Summiert): jede Datenzeile als Karte, je sichtbarer Spalte eine Zeile
    „Bezeichnung: Wert“, Auswahl-Checkbox oben, Aktionsknöpfe („Fertig melden“, „Teile (n)“) unten über die volle Breite. Kein Querscrollen.
    Die Spaltenauswahl (Zahnrad) wirkt weiter: ausgeblendete Spalten fehlen auch in der Karte, die Reihenfolge der Karte folgt der Spaltenreihenfolge.
    Baugruppen-Teile („Teile (n)“) klappen weiterhin unter der Karte auf; zugeklappt bleiben sie unsichtbar.

F5. **Trefferflächen ≥ 44 px** für alle Bedienelemente der Seite auf allen Größen, einschließlich der heute zu kleinen Auswahl-Checkbox je Zeile
    und der Seitennavigation (siehe Befunde B4, B5).

F6. **Kontrast**: die Regeln aus v1.99.36 (`bde.css:84-96`) bleiben wirksam; neue Texte (Kurzanzeige, Kartenbeschriftungen) erreichen
    4,5:1, Bedienelemente und Ränder 3:1. Farbe trägt nie allein Bedeutung.

## Technischer Loesungsentwurf

### Befunde (Ist-Stand, belegt)

| # | Befund | Beleg |
|---|---|---|
| B1 | `bde.css` laden fünf Views; `.bde-terminal` steht auch am BDE-Terminal. Jede Regel unter `.bde-terminal` wirkt dort mit. | `Views/BdeTerminal/Index.cshtml:5`, `BdeCockpit/Index.cshtml:3`, `BdeTeileliste/{Index,Summiert,Anmelden}.cshtml:7/7/5`; Wrapper `container-fluid bde-terminal` `Index.cshtml:9`, `Summiert.cshtml:9`, `Anmelden.cshtml:7` |
| B2 | `.bde-teileliste` ist schon die **Tabellen**-Klasse, kein Seiten-Scope. | `Index.cshtml:26`, `Summiert.cshtml:26`, `bde.css:82` |
| B3 | Global erzwingt `site.css` Tabellen in Breite `max-content` und Zellen ohne Umbruch, beides `!important`. Eine Kartenansicht muss das im Scope mit höherer Spezifität und `!important` aufheben. | `site.css:248-251`, `site.css:254-257` |
| B4 | Auswahl-Checkbox je Zeile ist 1,6 em (≈ 26 px, unter 576 px wegen `.table td { font-size: 0.85rem }` ≈ 22 px), kein umschließendes Label → Trefferfläche < 44 px (vorbestehend). | `bde.css:82`, `Index.cshtml:65-66`, `Summiert.cshtml:50-52`, `site.css:922-926` |
| B5 | Seitennavigation ist `pagination-sm` (≈ 31 px), unter 576 px global `min-height: 36px`; Seitengrößen-Auswahl `form-select-sm` → unter 44 px (vorbestehend). | `_Pagination.cshtml:41,76`, `site.css:898-904` |
| B6 | Spaltenauswahl blendet Zellen per **Inline-Stil** `display:none` aus und ordnet Zellen physisch um. Eine CSS-Kartenansicht ohne `!important` auf `display` respektiert das; Beschriftungen müssen an der Zelle hängen (nicht per `nth-child`). | `column-preferences.js:160-180` (Reorder `:182 ff.`, `supportsReorder: true` `Index.cshtml:202`) |
| B7 | Baugruppen-Kinderzeile ist `<tr class="collapse">`. Eine Regel wie `.bde-tl-seite tr { display:block }` (Spezifität 0,1,1+) schlägt Bootstraps `.collapse:not(.show){display:none}` (0,2,0) und würde **alle zugeklappten Teile sichtbar machen**. | `Index.cshtml:142-161`, Bootstrap 5.3.3 |
| B8 | Spaltenfilterzeile wird von `table-filter.js` in `<thead>` erzeugt; blendet die Kartenansicht `<thead>` aus, sind Spaltenfilter dort nicht bedienbar (bleiben aber aktiv, Server-Mode). | `table-filter.js:150-156`, Controller `BdeTeilelisteController.cs:114,124-126` |
| B9 | Hardware-Scanner: Ziel per `[data-hw-scan-ziel]`, nur **sichtbare** Ziele zählen. Auf Index/Summiert gibt es kein Ziel (Scan ohne Fokus → Hinweis „auf dieser Seite nicht verwendbar“), auf Anmelden genau eins (Personalnummer). Die Filterfelder sind keine Scan-Ziele → Einklappen ändert das Scanverhalten nicht. | `hardware-scanner.js:267,290-300`, `Anmelden.cshtml:16-18` |
| B10 | Sticky-Scrollbar: `site.js` hängt an jede `.table-responsive` eine Ersatzleiste, die sich bei fehlendem Überlauf selbst ausblendet (`no-overflow`). In der Kartenansicht entsteht kein Überlauf → genau kein Querbalken. | `site.js:33-37,99-104` |
| B11 | Es gibt **keine** einklappbare Filterkarte im Code (Suche `collapse`/`filter-card` in `Views/`: nur Navbar, ServiceSettings, Tracking, Teileliste-Kinder). Vorbilder sind Bootstrap-Collapse (`Index.cshtml:137-144`) und `<details>` ohne JS (`SageSdataFa/Index.cshtml:12`, `FaHierarchy/Index.cshtml:9`). | Grep 2026-11-18 |
| B12 | Die Seite verlangt einen Web-Benutzer mit zugeordnetem BDE-Terminal, sonst Umleitung auf Home mit Warnung. | `BdeTeilelisteController.cs:87-91` |
| B13 | `ResponsiveCssTests` verlangt, dass jede View mit `<table>` eine `table-responsive` enthält → Markup der Tabelle bleibt, die Kartenansicht entsteht nur per CSS. | `IdealAkeWms.Tests/Helpers/ResponsiveCssTests.cs:126-143` |
| B14 | Kürzel-Wächter scannt `Views/BdeTeileliste/` und `bde-teileliste.js` auf Kürzel-Literale (`"KA"`, `"ST"` …) → Kurzanzeige/Labels dürfen kein Kürzel als Konstante enthalten. | `TeilelisteKuerzelKonstantenWaechterTests.cs:14-27` |

### Variantenwahl Tabelle auf schmalen Bildschirmen (Empfehlung: Kartenansicht per CSS)

| | **A: Kartenansicht per CSS (empfohlen)** | B: Spaltenreduktion je Breakpoint |
|---|---|---|
| Querscrollen am Handy | keins | bleibt: Auswahl + Matchcode + Menge + Status + Aktion („Fertig melden“ + „Teile (n)“) ≈ 470 px > 390 px |
| Zahnrad (Spaltenauswahl) | wirkt weiter (B6) | zweiter, konkurrierender Ausblend-Mechanismus; Zahnrad-Wahl wird am Handy still übersteuert |
| Aufwand | ein CSS-Block im Scope + `data-label` an den `<td>` | CSS je Spalte, trotzdem Knöpfe außerhalb des Bilds |
| Spaltenfilter | am Handy nicht bedienbar (Rückfrage 3) | bedienbar, aber in quergescrollter Kopfzeile |
| JS | keins | keins |

A gewinnt: der eigentliche Zweck am Handy ist „Teil finden und melden“, und genau der Knopf liegt bei B außerhalb des Bilds.

### Umsetzung

1. **Seiten-Scope:** Wrapper in `Index.cshtml:9` und `Summiert.cshtml:9` wird `container-fluid bde-terminal bde-tl-seite`. Alle neuen Regeln in
   `bde.css` beginnen mit `.bde-tl-seite` (B1, B2). Keine neue Regel unter `.bde-terminal` allein, keine Änderung an `site.css`.
2. **Schwelle:** eine Schwelle für Kartenansicht **und** eingeklappte Filterkarte, `lg` (unter 992 px), gemäß Rückfrage 2 (Alternative `md`,
   unter 768 px). CSS `@media (max-width: 991.98px)`, Markup `d-lg-none`/`d-lg-block`.
3. **Filterkarte einklappbar ohne eigenes JS (Bootstrap-Collapse):** Karte `class="card filter-card mb-2 collapse d-lg-block" id="tlFilter"`;
   davor ein Knopf `btn btn-outline-secondary bde-touch w-100 d-lg-none text-start` mit `data-bs-toggle="collapse" data-bs-target="#tlFilter"
   aria-expanded="false" aria-controls="tlFilter"`, Text = Kurzanzeige (F2). `.d-lg-block` ist `!important` und hält die Karte ab 992 px offen,
   egal ob `.show` gesetzt ist. Kurzanzeige wird im `_Kopf` aus dem ViewModel gebildet (`KwVon`, `KwBis`, `GemeldeteZeigen`,
   `BdeTeilelisteViewModel.cs:15-17`); KW-Darstellung über `IsoWeekRange.Parse` (`Services/IsoWeekRange.cs:29`), Spaltenfilter-Anzahl über
   `ColumnFilterHelper.ReadFromQuery(Context.Request).Count` (`Services/ColumnFilterHelper.cs:18`) – kein neues ViewModel-Feld nötig.
   Das Zeichen ▾/▴ folgt `aria-expanded` per CSS (`[aria-expanded="true"]`), nicht per JS.
4. **Filter kleiner (F3):** `form-control-lg`/`btn-lg` in der Filterkarte (`_Kopf.cshtml:48,52,61,64`) auf Normalgröße + `bde-touch`
   (44 px, `bde.css:81`); für die `type="week"`-Eingaben Regel `.bde-tl-seite .filter-card .form-control { min-height: 44px }`.
   Hinweistext als `<details class="small">` mit `<summary>` „Hinweise zur Liste“ (Summary-Zeile selbst ≥ 44 px).
   Gilt je Rückfrage 1 auf allen Größen oder nur unterhalb der Schwelle.
5. **Leerraum (F1):** `mb-3` → `mb-2` im `_Kopf`; unterhalb der Schwelle `.bde-tl-seite h2 { font-size: 1.35rem }`,
   `.bde-tl-seite .bde-operator-badge { padding: .4rem .75rem; font-size: 1.1rem }` (Scope, damit das BDE-Terminal unverändert bleibt).
   Erläuterung neben „Auswahl fertigmelden“ (`Index.cshtml:21`, `Summiert.cshtml:21`) unterhalb der Schwelle ausblenden (`d-none d-lg-inline`);
   die Bestätigungsfrage nennt die volle Sollmenge weiterhin (`bde-teileliste.js:51`).
6. **Kartenansicht (F4)**, nur unterhalb der Schwelle, nur `.bde-tl-seite .bde-teileliste`:
   - Markup: jede Datenzelle bekommt `data-label="<Spaltentitel>"` (identisch zum `label` in `column-config`, `Index.cshtml:206-225`,
     `Summiert.cshtml:76-83`); Auswahlzelle Klasse `tl-zelle-auswahl`, Aktionszelle `tl-zelle-aktion`. Leer- und Kinderzeilen (`td[colspan]`) ohne Label.
   - `thead` ausblenden; `table`, `tbody` `display:block`; Tabelle `width: 100% !important` mit Selektor stärker als `site.css:248` (B3).
   - Zeilen: **nur** `tr:not(.collapse)` und `tr.collapse.show` als Karte (`display:flex; flex-direction:column`, Rand 1px #6c757d (4,7:1),
     Radius, `margin-bottom: .5rem`) – B7.
   - Zellen: `display:flex; justify-content:space-between; gap; white-space: normal !important` (B3), `::before { content: attr(data-label) }`
     fett, Farbe #212529. **Kein** `!important` auf `display` der Zellen (B6).
   - `tl-zelle-auswahl` `order:-1`, `tl-zelle-aktion` `order:99`, Aktionsknöpfe dort `flex: 1 1 auto` volle Breite.
   - Streifen (`table-striped`) in der Karte neutralisieren (`--bs-table-bg-type: transparent` im Scope), damit der Kartenhintergrund einheitlich ist.
7. **Trefferflächen (F5)**, alle Größen, Scope `.bde-tl-seite`:
   - Auswahl-Checkbox in ein `<label class="tl-check">` mit `min-width/min-height: 44px` (flex, zentriert) einbetten; `aria-label` am Input
     bleibt; das `change`-Ereignis erreicht `bde-teileliste.js:45-48` unverändert.
   - `.bde-tl-seite .pagination .page-link { min-height: 44px; min-width: 44px; display:flex; align-items:center; justify-content:center }`
     und `.bde-tl-seite .pagination-page-size { min-height: 44px }` – nur im Scope, `_Pagination.cshtml` bleibt.
   - Spaltenfilter-Eingaben und Zahnrad (`column-prefs-gear`, `btn-sm`, `column-preferences.js:411`; Kalenderknopf `table-filter.js:193`)
     im Scope ab der Schwelle `min-height: 44px`.
8. **Kontrast (F6):** `bde.css:84-96` unverändert lassen; die Kurzanzeige nutzt `btn-outline-secondary` (Bootstrap-Standard #6c757d auf Weiß
   4,7:1, in `site.css` nicht überschrieben – Grep `btn-outline-secondary` in `site.css` ohne Treffer). Neue Farben nur mit gemessenem Wert im CSS-Kommentar.
9. **Modal „Fertig melden“** (`Index.cshtml:171-199`): unverändert; auf 390 px nutzbar (Knöpfe `btn-lg` 60 px, `bde.css:1`).

### Regressionsrisiko gemeinsamer Code

- `bde.css` wird von BDE-Terminal und BDE-Cockpit geladen (B1) → **jede** neue Regel mit `.bde-tl-seite` beginnen; QA prüft BDE-Terminal am
  Desktop und auf 390 px auf unveränderte Darstellung (AK 14).
- `_Kopf.cshtml` ist gemeinsam für Einzeln und Summiert → beide Sichten testen.
- `site.css`/`site.js`/`table-filter.js`/`column-preferences.js`/`hardware-scanner.js`/`_Pagination.cshtml` werden **nicht** geändert; die
  Teileliste überschreibt im Scope. Würde ein Lauf doch eine dieser Dateien ändern müssen: anhalten und als Befund melden.
- Wächter-Tests B13/B14 bleiben grün.

### Ponytail (bewusst nicht gebaut)

- Kein eigenes JS für Auf-/Zuklappen, Kurzanzeige oder Karten: Bootstrap-Collapse, Razor, CSS.
- Kein Merken des Klappzustands (localStorage): jede Seite startet am Handy eingeklappt, die Kurzanzeige zeigt alles Nötige.
- Keine zweite Partial für Karten (doppelte Zeilenlogik mit Status/Rücknahme würde auseinanderlaufen).
- Keine Spaltenfilter-Oberfläche in der Kartenansicht (Rückfrage 3).
- Kein `modal-fullscreen-*`: das Modal passt auf 390 px.

### Zusatzfund (melden, nicht mitbauen)

- `_Pagination.cshtml:75-76`: `<label for="pageSizeSelect_…">` zeigt auf eine zufällige Id, das `<select>` hat keine Id → Label nicht verknüpft
  (alle Listen, Barrierefreiheit). Eigene Aufgabe, nicht Teil dieser Spec.

## Migrations-/SQL-Auswirkungen

Keine. Keine Model-Änderung, keine EF-Migration, kein `SQL/XX_*.sql`, `SQL/00_FreshInstall.sql` und `SQL/AgentJobs/*` unberührt.

## Einstellungen

keine (Schwelle und Klappverhalten sind Darstellung, kein fachlicher Schalter).

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| – | – | – | – | – | – | – | – |

## Audit-Feld-Auswirkungen

Keine. Es wird nichts Neues geschrieben. Der Filter-Speicher (`UserViewPreference` über `_prefs.SaveAsync`, `BdeTeilelisteController.cs:122-127`)
bleibt unverändert, ebenso Meldung/Rücknahme.

**Rollen/Zugriff:** unverändert `[RequireBdeActive]` + `[RequireBdeUserAccess]` (`BdeTeilelisteController.cs:17-18`) und Terminal-Pflicht
(`:87-91`). Keine neue Rolle → `controller.md`/`RoleOverview` unberührt.

**Listen-View-Pattern:** keine neue Tabelle. Pagination (`_Pagination`, `Index.cshtml:168`), Server-Spaltenfilter (`data-server-column-filter="true"`,
alle `<th>` mit `data-col-key`) und Filterkarte bleiben erhalten; nur die Darstellung unterhalb der Schwelle ändert sich.

## Akzeptanzkriterien

Testgeräte/-breiten: **Handy hochkant 390 × 844**, **Handy quer 844 × 390**, **Tablet hochkant 800 × 1280**, **Tablet quer 1280 × 800**,
**Terminal-Desktop 1920 × 1080** (Browser-Entwicklertools Geräteemulation + mindestens ein echtes Android-Gerät). Schwelle im Folgenden
„S“ = 992 px (oder 768 px je Rückfrage 2).

1. **Filter eingeklappt (< S):** Beim Öffnen von Einzeln und Summiert ist die Filterkarte eingeklappt; sichtbar ist ein Knopf über die volle Breite mit
   Kurzanzeige (F2). Tippen klappt auf/zu; `aria-expanded` wechselt; Zeichen ▾/▴ wechselt.
2. **Kurzanzeige korrekt:** zeigt KW von/bis (bzw. „alle Wochen“ bei leeren Feldern), „gemeldete sichtbar/ausgeblendet“ und – nur wenn > 0 –
   die Anzahl aktiver Spaltenfilter; nach „Filtern“ und „Filter zurücksetzen“ stimmt sie mit dem neuen Zustand überein.
3. **Filter offen (≥ S):** Karte immer sichtbar, kein Klappknopf; Verhalten von Filtern, Zurücksetzen und gespeichertem Filter je Werker/Werkbank unverändert.
4. **Filter kleiner (je Rückfrage 1):** Filterkarte ohne dreizeiligen Hinweistext (in „Hinweise zur Liste“ aufklappbar); Eingaben und Knöpfe 44 px hoch
   (gemessen in den Entwicklertools), nicht 60 px.
5. **Kartenansicht (< S):** keine waagerechte Scrollleiste auf Seite oder Tabelle (`document.documentElement.scrollWidth <= innerWidth`; keine
   sichtbare `.table-sticky-scrollbar`); jede Zeile eine Karte mit „Bezeichnung: Wert“ je sichtbarer Spalte, Checkbox oben, Aktionsknöpfe unten.
6. **Zahnrad wirkt in der Kartenansicht:** eine per Zahnrad ausgeblendete Spalte fehlt in jeder Karte; eine umsortierte Spalte erscheint in der
   neuen Reihenfolge mit der richtigen Beschriftung.
7. **Baugruppe:** zugeklappte Teile-Zeilen sind in der Kartenansicht unsichtbar; „Teile (n)“ klappt sie unter der Karte auf und wieder zu.
8. **Melden am Handy:** Einzelmeldung über „Fertig melden“ (Modal, Gut/Ausschuss) und Blockmeldung über Checkboxen + „Auswahl fertigmelden
   (n Zeilen, Summe x)“ funktionieren auf 390 px; „Zurücknehmen“ funktioniert; Zähler und Bestätigungsfrage unverändert.
9. **Tabelle (≥ S):** Terminal-Desktop zeigt die Tabelle wie v1.99.42 (Spalten, Spaltenfilterzeile, Zahnrad, Sortierung, CSV), nur mit geringeren Abständen.
10. **Trefferflächen ≥ 44 px (hart):** auf allen fünf Testbreiten haben alle Bedienelemente der Teileliste eine Trefferfläche von mindestens 44 × 44 px
    bzw. 44 px Höhe bei Textknöpfen: Werkbank-Auswahl, „Werker wechseln“, Reiter, Klappknopf, Filtereingaben, Schalter „gemeldete zeigen“,
    „Filtern“, „Filter zurücksetzen“, „Hinweise zur Liste“, „alle auf dieser Seite“, Auswahl-Checkbox je Zeile (Label-Fläche),
    „Auswahl fertigmelden“, „Fertig melden“, „Teile (n)“, „Zurücknehmen“, Seitenlinks, Seitengröße; ab S zusätzlich Zahnrad und Spaltenfilterfelder.
    Nachweis: Messung in den Entwicklertools (Box-Modell) je Element, Tabelle im QA-Nachweis.
11. **Kontrast WCAG AA (hart):** Text ≥ 4,5:1, Bedienelemente/Kartenränder ≥ 3:1, gemessen (z. B. Entwicklertools-Kontrastanzeige) für Kurzanzeige,
    Kartenbeschriftung, Kartenrand, „Filtern“ (blau) und „Fertig melden“ (grün) mit dunkler Schrift wie v1.99.36 in normal und Hover;
    „gemeldet“/„nicht eindeutig“ weiterhin mit Text + Symbol.
12. **Hardware-Scanner unverändert:** Anmelden-Seite: Scan ohne Fokus landet in „Personalnummer“ und meldet an (390 px und Desktop). Teileliste:
    Scan ohne Fokus zeigt wie bisher den Hinweis „… auf dieser Seite nicht verwendbar“, mit auf- wie zugeklappter Filterkarte.
13. **Keine Datenänderung:** kein Unterschied im Diff an Controller, Service, Repository, Model, `SQL/`; Build und alle Tests grün (inkl.
    `ResponsiveCssTests`, `TeilelisteKuerzelKonstantenWaechterTests`, `HardwareScannerTests`, `TableToolbarHostTests`).
14. **Kein Übergriff:** `site.css`, `site.js`, `table-filter.js`, `column-preferences.js`, `hardware-scanner.js`, `_Pagination.cshtml` unverändert;
    jede neue Regel in `bde.css` beginnt mit `.bde-tl-seite`. BDE-Terminal (Desktop und 390 px) und eine fremde Liste mit `_Pagination`
    (z. B. `/BfAuftraege`) sehen aus wie vorher.

## Test-Szenarien

Neues Kapitel **TS-165 „BDE-Teileliste kompakt und mobil (v1.99.44)“** in `docs/TESTSZENARIEN.md` (Nummer vorläufig: TS-163 belegt durch v1.99.42,
TS-164 reserviert für die Kommissionierliste v1.99.43; Dev-Lauf prüft erneut), danach Zeile in [[testszenarien-index]]. TS-152.12 (Kontrast) bleibt
gültig und wird in TS-165.6 referenziert.

Vorbedingungen: DEV IDEAL, Werkbank mit wirksamer Teileliste (wie QA v1.99.36: Werkbank mit Kürzel, `BdeTeileliste = 1` temporär, danach
zurücksetzen), Web-Benutzer mit BDE-Terminal (B12), aktiver Werker, mindestens eine offene Zeile, eine Baugruppe mit Teilen, eine gemeldete
eigene Zeile, mehr Zeilen als eine Seite fasst. Geräte/Breiten wie in den Akzeptanzkriterien.

- **TS-165.1 Kopf und Filter am Handy (390 px):** Seite öffnen → Karte eingeklappt, Kurzanzeige korrekt; aufklappen, KW ändern, „Filtern“ →
  Kurzanzeige aktualisiert; Spaltenfilter am Desktop setzen, am Handy öffnen → „1 Spaltenfilter“; „Filter zurücksetzen“ → Kurzanzeige ohne Spaltenfilter (AK 1, 2).
  Negativ: leere KW-Felder → „alle Wochen“.
- **TS-165.2 Kartenansicht Einzeln und Summiert (390 px, 800 px hochkant):** kein Querscrollen, Karten mit Beschriftungen; Zahnrad: Spalte
  ausblenden und umsortieren → Karten folgen; Baugruppe auf/zu (AK 5–7). Negativ: zugeklappte Teile-Zeilen dürfen nie sichtbar sein.
- **TS-165.3 Melden am Handy:** Einzelmeldung mit Ausschuss, Blockmeldung zweier Zeilen, Rücknahme der eigenen Meldung (AK 8).
- **TS-165.4 Terminal-Desktop und Tablet quer:** Tabelle wie bisher, Filterkarte offen, kein Klappknopf, Hinweise aufklappbar, Filter 44 px (AK 3, 4, 9).
- **TS-165.5 Trefferflächen:** Messtabelle aller Elemente aus AK 10 auf den fünf Breiten.
- **TS-165.6 Kontrast:** Messwerte aus AK 11; Hover auf „Filtern“/„Fertig melden“.
- **TS-165.7 Scanner:** Anmeldung per Handscanner (Anmelden-Seite) auf Handy und Terminal; Scan auf der Teileliste bei auf- und zugeklappter Karte (AK 12).
- **TS-165.8 Regression:** BDE-Terminal Desktop/390 px, `/BfAuftraege` Seitennavigation, Breite genau an der Schwelle (991 px vs. 992 px bzw. 767/768) (AK 14).

## Etappen (nur bei epic: true)

Entfällt (`epic: false`, ein Commit). Arbeit im Bündel-Worktree `2026-08-07-ideal-teile-1-5`; Branch vor Beginn mit `scripts/sync-worktree.ps1` aktuell halten.

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | entfällt | – | – |

## Deploy

*Vorläufig – der QA-Lauf bestätigt gegen den echten Diff.*

- **Web-App:** ja (Views `BdeTeileliste/*`, `bde.css`, Hilfe, Changelog, Version)
- **Service:** nein (nur Versionsnummer in `IDEALAKEWMSService/AppVersion.cs` mitziehen, kein Service-Deploy nötig)
- **Migration:** nein
- **Version (vorläufig):** **1.99.44** in beiden `AppVersion.cs` (1.99.42 = Stand Worktree `IdealAkeWms/AppVersion.cs:5`; 1.99.43 reserviert für
  [[2026-11-18-ideal-kommissionierliste-filter-spalten-leitstand-spec]]). Dev-Lauf prüft erneut.
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`, ADR 0022):

| Id | Version | Typ | Firma | Phase | Ausführung | Text |
|---|---|---|---|---|---|---|
| `v1.99.44-BdeTeilelisteMobilInfo` | 1.99.44 | Info | IDEAL | Nachher | anzeigen | „Werker informieren: BDE-Teileliste am Handy/Tablet – Filter über den Knopf ‚Filter: …‘ aufklappen, Zeilen als Karten, Spaltenfilter nur am Terminal. Browser-Cache am Gerät ggf. leeren (bde.css).“ |

  Kein SQL-, Einmal-, Prüf-, Einstellungs-, Leitstand- oder Ordner-Eintrag.
- **Publish-Befehle** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`

Brain-Pflichten des Dev-Laufs: Hilfe-Abschnitt BDE-Teileliste (`Views/Help/Index.cshtml:1881 ff.`) um „Handy/Tablet: Filter einklappbar,
Kartenansicht, Spaltenfilter nur am Terminal“ ergänzen; Anwender- und Brain-Changelog; [[feature-map]]; [[testszenarien-index]];
`fallstricke.md` neuer Eintrag zu B7 (Kartenansicht-`display` schlägt `.collapse:not(.show)`) mit Warum.

## Offene Rueckfragen

1. **Filter auch am Terminal kleiner?** „Filter kleiner“ aus dem Termin: Soll die Filterkarte auch am Terminal-Desktop kompakter werden
   (Eingaben/Knöpfe 44 px statt 60 px, Hinweistext einklappbar), oder nur unterhalb der Schwelle? *Empfehlung: ja, überall* – 44 px erfüllen die
   Terminal-Vorgabe, die Karte schrumpft am Terminal um gut die Hälfte; die Meldeknöpfe im Modal bleiben 60 px.
2. **Schwelle:** Kartenansicht und eingeklappte Filterkarte unter **992 px** (Handy und Tablet hochkant) oder nur unter **768 px** (nur Handy)?
   *Empfehlung: 992 px* – am Tablet hochkant (≈ 800 px) braucht die Tabelle mit 10 Standardspalten sonst Querscrollen und „Fertig melden“
   liegt außerhalb des Bilds; Terminal-Desktop und Tablet quer (≥ 1280 px) behalten die Tabelle.
3. **Spaltenfilter in der Kartenansicht:** Die Spaltenfilterzeile gehört zum Tabellenkopf und entfällt in der Kartenansicht (B8). Aktive
   Spaltenfilter wirken weiter, die Kurzanzeige nennt ihre Anzahl, „Filter zurücksetzen“ hebt sie auf. Reicht das am Handy?
   *Empfehlung: ja* – am Handy wird über KW eingegrenzt und gemeldet; Spaltenfilter setzt man am Terminal. Eine eigene Spaltenfilter-Oberfläche
   für Karten wäre neues JS in gemeinsamem Code.
4. **(Fachbereich: Walter/Benjamin) Konto am Handy:** Die Teileliste öffnet nur für einen Web-Benutzer mit zugeordnetem BDE-Terminal
   (B12, sonst Umleitung mit Warnung). Mit welchem Konto/Gerät wird am Handy gearbeitet? *Empfehlung:* bestehendes Terminal-Konto der Werkbank
   bzw. ein Terminal je Gerät durch den BDE-Admin anlegen – reine Stammdatenpflege, keine Codeänderung; für den Test auf DEV der vorhandene Terminal-Benutzer.

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. Filter auch am Terminal kleiner (44 px, Hinweise einklappbar)? →
2. Schwelle Kartenansicht/eingeklappter Filter: 992 px oder 768 px? →
3. Spaltenfilter in der Kartenansicht nicht bedienbar (Anzahl + Zurücksetzen) – akzeptiert? →
4. Konto/Gerät am Handy (Terminal-Zuordnung)? →

> [!note] Zurückgestellt (Gerald 2026-11-18 19:20: „hinten anstellen“). Spec bleibt Entwurf, Rückfragen offen.
