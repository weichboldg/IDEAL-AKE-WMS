---
type: spec
title: "Listen: Spaltenauswahl an die IDEAL-Listen anschliessen + ADR 0005 ergaenzen"
slug: 2026-08-12-listen-spaltenauswahl-spec
status: Freigegeben
created: 2026-08-12
updated: 2026-08-12
source_backlog: "[[2026-08-12-listen-spaltenauswahl]]"
task: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
affected_code:
  - IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs (vier neue ViewConfig-Eintraege + GetByViewKey-Switch — vom Backlog NICHT erwaehnt, aber ohne diese Eintraege antwortet die Prefs-API mit 400 und speichert nichts, siehe Fachliche Anforderungen Punkt 2)
  - IdealAkeWms/Views/FaHierarchyKommissionierListen/Index.cshtml (view-config + column-config + Skript-Include)
  - IdealAkeWms/Views/FaHierarchyBeschichtung/Index.cshtml (view-config + column-config + Skript-Include)
  - IdealAkeWms/Views/FaHierarchyVormontage/Index.cshtml (view-config + column-config + Skript-Include)
  - IdealAkeWms/Views/FaHierarchyVormontage/Summiert.cshtml (view-config + column-config + Skript-Include)
  - IdealAkeWms/wwwroot/js/table-filter.js (Sortier-Fix, IN DIESE SPEC gezogen 2026-08-12: sortTable() sortiert kuenftig innerhalb JEDES `<tbody>`-Elements separat statt nur im ersten — behebt den in der Kritischen Pruefung verdikt-tragenden Sort-Defekt bei den drei gruppierten IDEAL-Listen; fuer bestehende Ein-tbody-Listen verhaltensgleich, siehe Fachliche Anforderungen Punkt 5)
  - docs/TESTSZENARIEN.md (Kapitel TS-59/TS-60/TS-61 um Zahnrad-Dialog-Schritte, Multi-Gruppen-Faelle und den Sortier-Regressionstest ergaenzen)
  - secondbrain/tests/testszenarien-index.md (nachziehen)
  - secondbrain/architektur/adr/0005-listen-view-pattern-mit-server-side-spaltenfilter.md (additiv ergaenzen ODER neuer praezisierender ADR — Form siehe Freigabe-Antwort 3: additiver Nachtrag)
  - secondbrain/architektur/fallstricke.md (additiv: ColumnDefinitions-Registrierungspflicht bei neuem viewKey; sowie der in dieser Spec entdeckte UND in dieser Spec behobene Sortier-Fallstrick bei mehr-tbody/gruppierten Tabellen — als Fallstricke-Eintrag dokumentieren, dass table-filter.js vor diesem Fix nur das erste `<tbody>` sortierte, damit dieselbe Fehlerklasse bei kuenftigen gruppierten Tabellen sofort erkannt wird)
open_questions: []
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

Anwender sollen in den vier neuen IDEAL-Listen (Kommissionierlisten, Beschichtungsauftrag,
Vormontage-Einzeln, Vormontage-Summiert) dieselbe Spaltenauswahl (Sichtbarkeit, Breite, Reihenfolge,
Standard-Sortierung je Benutzer, DB-persistiert) nutzen koennen, die im uebrigen WMS bereits Standard
ist (`ProductionOrders`, `PickingLeitstand`, `FaWorklist` u. a.). Die Mechanik existiert vollstaendig
und wird **ausschliesslich angeschlossen**, nicht neu gebaut. Der eigentliche Nutzen dieser Spec ist
dreigeteilt:

1. **Vier konkrete Listen** bekommen den fehlenden Anschluss (Views + eine bislang uebersehene
   Server-Registrierung).
2. **ADR 0005** (Listen-View-Pattern) wird um einen bisher unbenannten vierten Pflichtbestandteil
   ergaenzt — Spaltenpraeferenzen —, damit dieselbe Luecke nicht bei jeder kuenftigen Liste erneut
   entsteht. Die Teil-3-Spec der IDEAL-Epic hatte den ADR-0005-Listenteil explizit eingefordert
   (Pagination, Filterkarte, Server-Spaltenfilter), die Spaltenpraeferenzen aber nie erwaehnt — der
   Dev-Lauf hat exakt das gebaut, was verlangt war, nicht mehr. Das ist kein Einzelversehen, sondern
   eine Luecke im Muster selbst.
3. **Der beim Anschliessen sichtbar gewordene Sortier-Defekt in `table-filter.js`** (nur das erste
   `<tbody>` einer Tabelle wird sortiert) wird im selben Zug behoben — er waere sonst mit drei
   brandneuen, gruppierten Listen sofort produktiv sichtbar geworden (siehe Fachliche Anforderungen
   Punkt 5).

## Umfang (In-Scope / Out-of-Scope)

**In-Scope:**
- `IdealAkeWms/Views/FaHierarchyKommissionierListen/Index.cshtml`
- `IdealAkeWms/Views/FaHierarchyBeschichtung/Index.cshtml`
- `IdealAkeWms/Views/FaHierarchyVormontage/Index.cshtml` (Sicht 1, "Einzelne Teile")
- `IdealAkeWms/Views/FaHierarchyVormontage/Summiert.cshtml` (Sicht 2, "Summiert")
- Je View: `#view-config`-JSON, `#column-config`-JSON, Einbindung von `column-preferences.js` **vor**
  `table-filter.js` (Reihenfolge-Pflicht, siehe `fallstricke.md`).
- **Server-seitige Registrierung** der vier neuen `viewKey`-Werte in
  `IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs` (neue `ViewConfig`-Konstanten + Eintrag im
  `GetByViewKey`-Switch). Ohne diese Registrierung liefert
  `UserViewPreferencesApiController.Get/Put/Delete` `BadRequest` fuer jeden dieser `viewKey` — die
  Kachel-Verdrahtung in der View allein reicht **nicht** (siehe Fachliche Anforderungen Punkt 2; das
  ist bereits einmal genau so bei `FaWorklist` passiert, siehe Code-Kommentar dort).
- **`IdealAkeWms/wwwroot/js/table-filter.js`** — Sortier-Fix fuer gruppierte Tabellen (siehe Fachliche
  Anforderung 5): `sortTable()` sortiert kuenftig **innerhalb jedes** `<tbody>`-Elements einer Tabelle
  separat, statt ausschliesslich im ersten. Betrifft sowohl den Header-Klick-Sort als auch
  `window.triggerSort()` (Default-Sortierung aus den Benutzereinstellungen). Fuer Tabellen mit genau
  **einem** `<tbody>` (alle bestehenden flachen Listen) ist das Ergebnis **unveraendert** — harte
  Regressions-AK (siehe Akzeptanzkriterien).
- ADR-0005-Ergaenzung um den vierten Pflichtbestandteil "Spaltenpraeferenzen" (Form: additiver
  Nachtrag, siehe Freigabe-Antwort 3).
- Testszenarien-Ergaenzung in den bestehenden Kapiteln TS-59/TS-60/TS-61.

**Out-of-Scope:**
- `IdealAkeWms/Views/FaHierarchy/Index.cshtml` (Teil-2-Baumanzeige, freifliessender Baum ohne
  ausgerichtete Spalten) — Vorbedingung Tree-Table ist [[2026-08-12-fa-struktur-darstellung-spec]],
  siehe Freigabe-Antwort 1/2. **Scope-Grenze bestaetigt:** diese Spec fasst ausschliesslich die vier
  flachen IDEAL-Listen an, NICHT die Baumanzeige; Reihenfolge: diese Spec zuerst.
- `Print.cshtml`-Views (`FaHierarchyKommissionierListen/Print.cshtml`,
  `FaHierarchyBeschichtung/Print.cshtml`): geprueft — beide sind bereits eigenstaendige,
  `Layout = null`-HTML-Dokumente mit fest verdrahtetem Inline-CSS, komplett unabhaengig von
  `filterable-table`/`column-preferences.js`. Die Bildschirm-Spaltenauswahl hat auf den Ausdruck
  **strukturell keinen Einfluss** — das entspricht bereits dem in der Backlog-Notiz vorgeschlagenen
  "festes Druck-Layout". Keine Aenderung noetig, kein Abgleich mehr offen.
- Keine Aenderung an `column-preferences.js` oder der `UserViewPreferencesApiController`-API-Vertrag
  (Endpunkte/Contract bleiben unveraendert — die neuen `ViewConfig`-Eintraege sind reine Daten, keine
  Logikaenderung). **Ausnahme:** `table-filter.js` erhaelt den unter Fachlicher Anforderung 5
  beschriebenen, minimal-invasiven Sortier-Fix — das ist der einzige Eingriff in gemeinsam genutzten
  Code in dieser Spec, und er ist fuer Ein-`tbody`-Tabellen verhaltensgleich zum bisherigen Code
  (siehe harte Regressions-AK).
- Keine neue Migration, keine neue Rolle, kein neues AppSetting/Toggle (Access-Filter und
  Feature-Toggles dieser vier Views bleiben aus Teil 3/4/5 unveraendert).
- Kein Chip-Hinweis fuer "ausgeblendete Spalte mit aktivem Filter" (siehe Freigabe-Antwort 4 —
  vertagt/dokumentiert, eigener Backlog-Punkt).

## Fachliche Anforderungen

1. **Drei Bloecke je View, identisch zum Muster in `Views/ProductionOrders/Index.cshtml`:**
   - `<script type="application/json" id="view-config">{ "viewKey": "<Key>", "supportsReorder": true, "supportsSortDefault": <true|false, siehe Punkt 5> }</script>` unmittelbar vor der
     `Scripts`-Section (bzw. direkt vor der bestehenden `<partial name="_Pagination" .../>`-Nachbarschaft,
     wie im Referenzcode).
   - `<script type="application/json" id="column-config">[...]</script>` mit **exakt** denselben
     `key`-Werten wie die vorhandenen `data-col-key`-Attribute der `<th>` in derselben View (siehe
     Konkrete Spaltenlisten unten). Kein neuer, kein fehlender, kein umbenannter Key — sonst
     entkoppeln sich Spaltenfilter und Spaltenpraeferenzen (Fallstricke: "`data-col-key` ist Pflicht
     ... Filter- und Spalten-Preferences-Logik adressiert Spalten ueber diesen Key").
   - `<script src="~/js/column-preferences.js" asp-append-version="true"></script>` **vor**
     `<script src="~/js/table-filter.js" ...>` (Reihenfolge-Pflicht, siehe Umfang).

2. **Server-seitige `ColumnDefinitions`-Registrierung ist Pflicht, nicht optional — Korrektur der
   Backlog-Einschaetzung "kein C#".** `UserViewPreferencesApiController.Get/Put/Delete` prueft vor
   jedem Zugriff `ColumnDefinitions.GetByViewKey(viewKey) == null → BadRequest`. Ohne einen Eintrag
   fuer `FaHierarchyKommissionierListen`, `FaHierarchyBeschichtung`,
   `FaHierarchyVormontageEinzeln` und `FaHierarchyVormontageSummiert` in
   `ColumnDefinitions.GetByViewKey` scheitert jedes Laden/Speichern der Einstellungen mit 400 — das
   Zahnrad wuerde zwar erscheinen (rein clientseitig aus dem inline `#column-config`), aber
   Aenderungen gingen bei jedem Reload verloren (der `PUT` schlaegt fehl, still, ohne
   Benutzer-Fehlermeldung). Exakt dieser Fehler ist am Code bereits einmal passiert und dokumentiert
   (`ColumnDefinitions.cs`, Kommentar bei `FaWorklist`: "vorher kannte GetByViewKey den Key nicht,
   die Prefs-API antwortete 400 und Zahnrad-Einstellungen gingen bei jedem Reload verloren"). Diese
   Spec baut daher fuer jede der vier Views eine `ViewConfig`-Konstante (Spalten identisch zum
   `#column-config`-JSON der jeweiligen View) und erweitert den `GetByViewKey`-Switch um die vier
   neuen `case`-Zweige. Das ist **keine** Aenderung an der API-Signatur/dem HTTP-Vertrag, sondern
   reine Daten-Registrierung — insofern bleibt die Backlog-Aussage "keine API-Aenderung" im engeren
   Sinn richtig, "kein C#" ist es nicht.

3. **Konkrete Spaltenlisten (Key/Label/Locked/DefaultHidden), aus den vorhandenen `<th
   data-col-key="...">` der jeweiligen View abgeleitet:**

   **`FaHierarchyKommissionierListen`** (11 Spalten): `hauptfa` (locked), `hauptartnr`, `artnr`,
   `matchcode` (locked — zweiter identifizierender Schluessel neben `hauptfa`), `sollmenge`,
   `hauptlagerplatz`, `kommissionieren`, `arbeitsbereich`, `artikeltyp` (defaultHidden),
   `beschichtet` (defaultHidden), `material` (defaultHidden).

   **`FaHierarchyBeschichtung`** (9 Spalten): `hauptfa` (locked), `hauptartnr`, `artnr`, `matchcode`
   (locked), `sollmenge`, `beschichtet`, `breite`, `hoehe`, `tiefe`.

   **`FaHierarchyVormontageEinzeln`** (8 Spalten): `hauptfa` (locked), `hauptartnr`, `artnr`,
   `matchcode` (locked), `sollmenge`, `fertigungmenge`, `vmbedarf`, `material`.

   **`FaHierarchyVormontageSummiert`** (3 Spalten, flache, nicht gruppierte Tabelle): `matchcode`
   (locked — einziger Identifikator dieser aggregierten Sicht), `sollmenge` (Label "Summe
   Sollmenge"), `fertigungmenge` (Label "Summe Fertigungmenge").

   `defaultWidth` durchgehend `null` ausser bei den bereits schmal ausgelegten Zahlenspalten
   (`sollmenge`, `fertigungmenge`, `breite`, `hoehe`, `tiefe` — analog zu `quantity`/`coating-part`
   in `ProductionOrders`, dort 55px); die exakte Zahl ist ein UI-Feinschliff des Dev-Laufs, keine
   fachliche Entscheidung.

4. **Wechselwirkung Spaltenpraeferenzen ↔ Server-Spaltenfilter — technisch verifiziert, nicht
   spekuliert.** Die vier Views laufen im Server-Filter-Mode (`data-server-column-filter="true"`):
   Filter werden ausschliesslich ueber die URL-Query (`?colf_<col-key>=...`) transportiert und vom
   Controller serverseitig via `ColumnFilterHelper.ReadFromQuery` gelesen — voellig unabhaengig von
   der (DB-persistierten) Spaltensichtbarkeit aus den Benutzereinstellungen. Blendet ein Anwender
   eine Spalte aus, bleibt ein zuvor gesetzter `colf_`-Parameter dieser Spalte in der URL weiterhin
   wirksam: Die Liste ist kuerzer, ohne dass eine sichtbare Spalte den Grund zeigt. Diese Spec baut
   dafuer **keine** Loesung (kein Chip-Hinweis, siehe Out-of-Scope) — die Wechselwirkung ist hiermit
   aber belegt, nicht mehr nur vermutet, und damit entscheidungsreif fuer Schranke 1.

5. **Sortier-Fallstrick bei den drei gruppierten Listen — jetzt IN-SCOPE dieser Spec
   (verdikt-tragender Punkt der Kritischen Pruefung, per menschlicher Entscheidung vom 2026-08-12 in
   diesen Lauf gezogen; ersetzt die fruehere Trennungs-Entscheidung aus Freigabe-Antwort 5/den
   ANTWORTEN unten — siehe Nachbesserung).** `Kommissionierlisten`, `Beschichtung` und
   `Vormontage-Einzeln` rendern je `HauptFA`-Gruppe ein **eigenes** `<tbody>` (Kommentar in den Views:
   "Je HauptFA-Gruppe ein eigenes tbody ... table-filter.js kann Zeilen nie ueber Gruppengrenzen
   mischen"). Das stimmt fuer den **Filter**, galt aber **nicht** fuer die **Sortierung**:
   `table-filter.js` band beim Init `_tbody = _table.querySelector('tbody')` (Zeile 130, im Worktree
   verifiziert) — das liefert nur das **erste** `<tbody>`-Element im DOM — und `sortTable()` (Zeilen
   314–358) sowie der `th`-Klick-Handler (Zeilen 222–246, unbedingt an jedes `th[data-filterable]`
   gebunden, unabhaengig vom Server-/Client-Filter-Modus) sortierten ausschliesslich Zeilen **dieses
   ersten** `tbody`. Ein Klick auf eine sortierbare Spaltenkopfzeile sortierte bei diesen drei Listen
   also nur die **erste** `HauptFA`-Gruppe um, alle anderen Gruppen blieben unveraendert — sichtbar
   mit drei brandneuen Listen, sofort irrefuehrend fuer den Anwender.

   **Fix (in dieser Spec umgesetzt):** `sortTable()` wird von der global auf `_tbody` (den beim
   `init()` einmalig gecachten ersten `<tbody>`) fixierten Sortierung auf eine
   **Pro-`<tbody>`-Sortierung** umgestellt: Statt ausschliesslich im gecachten `_tbody` zu sortieren,
   iteriert `sortTable()` kuenftig ueber `_table.querySelectorAll('tbody')` und sortiert die
   Datenzeilen (weiterhin unter Ausschluss von `td[colspan]`-Gruppenkopfzeilen) **innerhalb jedes
   einzelnen** `<tbody>` fuer sich, ohne Zeilen ueber `<tbody>`-Grenzen zu verschieben — exakt die vom
   Menschen als klein und risikoarm eingeschaetzte Richtung ("ueber alle tbody-Elemente iterieren und
   innerhalb jedes Elements sortieren ... erhaelt zugleich die gewollte Eigenschaft, dass Zeilen nie
   ueber Gruppengrenzen wandern"). Der Fix wirkt sowohl auf den Header-Klick-Sort als auch auf
   `window.triggerSort()` (programmatischer Default-Sort aus den Benutzereinstellungen).

   **Regressionsgarantie fuer bestehende flache AKE-Listen (harte Anforderung):** Fuer Tabellen mit
   **genau einem** `<tbody>` (alle heute bestehenden `filterable-table`-Listen, u. a.
   `ProductionOrders`, `PickingLeitstand`, `FaWorklist`, `OseonTracking`, sowie die neue
   `FaHierarchyVormontageSummiert`) liefert `querySelectorAll('tbody')` weiterhin genau **ein**
   Element — das Sortierergebnis ist damit **unveraendert** zum bisherigen Verhalten. Diese Listen
   duerfen sich durch den Fix in keiner Weise aendern (siehe Akzeptanzkriterien, harte AK).

   `supportsSortDefault` bleibt trotz des Fixes **unveraendert `false`** fuer
   `FaHierarchyKommissionierListen`, `FaHierarchyBeschichtung` und `FaHierarchyVormontageEinzeln`
   (Muster wie `OseonTracking`/`Bom` in `ColumnDefinitions.cs`) — das ist nach dem Fix eine bewusst
   konservative, aber nicht mehr technisch zwingende UX-Entscheidung: der Fix macht auch den
   automatischen Default-Sort (`triggerSort`) korrekt, eine Aktivierung von
   `supportsSortDefault: true` fuer die drei gruppierten Listen ist damit technisch moeglich, wird in
   dieser Spec aber bewusst **nicht** vorgenommen (kein Anlass, jede reparierte Fehlerklasse sofort in
   ein neues Feature umzumuenzen — eigener, spaeterer Schritt, falls gewuenscht).
   `FaHierarchyVormontageSummiert` ist weiterhin eine flache, nicht gruppierte Tabelle (ein `<tbody>`,
   keine Gruppen) und behaelt `supportsSortDefault: true` wie `ProductionOrders`.

6. **Keine Aenderung der bestehenden Zugriffs-/Toggle-Logik.** Die Access-Filter
   (`RequireLagerProcessingAccessAttribute` bzw. `RequireBeschichtungsauftragAccessAttribute`) und
   Feature-Toggles (`FaHierarchyKommissionierlistenAktiv`, `FaHierarchyBeschichtungAktiv`,
   `FaHierarchyVormontageAktiv` o. ae.) der vier Views bleiben unveraendert; diese Spec fuegt nur
   Anzeige-/Praeferenz-Bloecke sowie den Sortier-Fix hinzu.

## Technischer Loesungsentwurf

**Views (identisches Muster viermal, Referenz `Views/ProductionOrders/Index.cshtml`):** In jeder der
vier Views werden — unmittelbar vor bzw. innerhalb der bestehenden Struktur, ohne sonstige
Aenderung an Markup/Controller/Service — die drei Bloecke aus Fachlicher Anforderung 1 ergaenzt.
Reihenfolge im `Scripts`-Abschnitt: `column-preferences.js` **vor** `table-filter.js` (aktuell steht
in allen vier Views nur `table-filter.js`).

**`ColumnDefinitions.cs`:** vier neue `public static readonly ViewConfig`-Konstanten
(`FaHierarchyKommissionierListen`, `FaHierarchyBeschichtung`, `FaHierarchyVormontageEinzeln`,
`FaHierarchyVormontageSummiert`), Spalten wie in Fachlicher Anforderung 3, sowie vier neue
`case`-Zweige im `GetByViewKey`-Switch. Reine additive Erweiterung der bestehenden statischen Klasse,
kein Eingriff in bestehende Eintraege.

**`wwwroot/js/table-filter.js`:** `sortTable(colKey, dir)` wird umgebaut, um ueber
`_table.querySelectorAll('tbody')` zu iterieren statt ausschliesslich das beim `init()` gecachte
`_tbody` zu verwenden; je `<tbody>` werden die Datenzeilen (weiterhin unter Ausschluss von
`td[colspan]`-Zeilen) unabhaengig sortiert und in **dasselbe** `<tbody>` zurueckgehaengt. Keine
Aenderung an `init()`, am Header-Klick-Handler selbst, an `applyFilters()`/der
Server-Filter-Navigation oder an der Datumspicker-/KW-Logik — reine, lokale Aenderung innerhalb von
`sortTable()`. Betrifft alle Aufrufer unveraendert (`th`-Klick, `window.triggerSort()`), da die
Funktionssignatur gleich bleibt.

**Kein Eingriff in `Services/FaHierarchyListBuilder.cs` oder die Controller** — die Spaltenwerte
selbst (welche Zellen gerendert werden) aendern sich nicht, nur ihre Sichtbarkeit/Reihenfolge/Breite
im Browser sowie deren Persistenz je Benutzer und die Sortierlogik innerhalb der Gruppen.

**ADR-0005-Ergaenzung** (additiver Nachtrag in ADR 0005 selbst, siehe Freigabe-Antwort 3):
ADR 0005 bekommt einen vierten, verbindlichen Pattern-Bestandteil neben Pagination,
Filterkarte und Server-Spaltenfilter:

> **Spaltenpraeferenzen — Pflicht fuer alle Tabellen-Views mit Server- oder Client-Spaltenfilter.**
> Jede neue Listen-View liefert zusaetzlich: `#view-config`- und `#column-config`-JSON-Bloecke mit
> denselben `key`-Werten wie die `data-col-key`-Attribute, sowie die Einbindung von
> `wwwroot/js/column-preferences.js` **vor** `table-filter.js`. Identifizierende Spalten
> (`locked: true`), selten gebrauchte Spalten (`defaultHidden: true`). Referenzimplementierung:
> `Views/ProductionOrders/Index.cshtml`. Gruppierte/strukturierte Tabellen (mehrere `<tbody>` je
> Gruppe): `table-filter.js` sortiert seit 2026-08-12 innerhalb jedes `<tbody>`-Elements separat
> (siehe `fallstricke.md`) — `supportsSortDefault` ist dort trotzdem defensiv zu pruefen, aber nicht
> mehr technisch erzwungen `false` zu setzen.

Zusaetzlich Verweis auf `Controllers/Api/UserViewPreferencesApiController.cs` +
`Models/ViewModels/ColumnDefinitions.cs` in der ADR-Dateiliste, da eine neue Liste ohne die
`ColumnDefinitions`-Registrierung die Praeferenzen zwar anzeigt, aber nicht persistiert (Fachliche
Anforderung 2).

**Einmaliger Abgleich (Nebenbefund der Backlog-Notiz, hier bestaetigt als sinnvoll, aber nicht
Teil dieser Spec):** Ein separater Durchgang ueber alle bestehenden `filterable-table`-Views auf
Vollstaendigkeit von (3) Server-Spaltenfilter und (4) Spaltenpraeferenzen lohnt sich, sobald ADR 0005
ergaenzt ist — als eigene, kleine Aufgabe in `secondbrain/aufgaben/`, nicht als Teil dieser Spec (die
vier IDEAL-Listen sind bereits identifiziert und abschliessend behandelt).

## Migrations-/SQL-Auswirkungen

Keine. `UserViewPreference`/`UserViewPreferenceRepository`/die zugehoerige Tabelle existieren
bereits und sind `viewKey`-agnostisch (Freitextspalte, keine Fremdschluessel-/Check-Constraint auf
bekannte Keys). Kein neuer Migrationsschritt, kein `SQL/XX_*.sql`, kein `00_FreshInstall.sql`-Eintrag.
Der `table-filter.js`-Sortier-Fix ist reines Client-JS ohne Datenbankbezug.

## Audit-Feld-Auswirkungen

Keine neuen Entitaeten. `UserViewPreference` schreibt beim Speichern bereits
`_currentUserService.GetDisplayName()`/`GetWindowsUserName()` (bestehender Code, unveraendert durch
diese Spec).

## Akzeptanzkriterien

1. Auf allen vier Views (`FaHierarchyKommissionierListen/Index`, `FaHierarchyBeschichtung/Index`,
   `FaHierarchyVormontage/Index`, `FaHierarchyVormontage/Summiert`) ist ein Zahnrad-Symbol zur
   Spaltenkonfiguration (Offcanvas) **vorhanden und funktional** — abgeschwaecht gegenueber "identisch
   zu `ProductionOrders/Index`" (Kritische Pruefung, HINWEIS): `insertGearButton` haengt den Knopf an
   das `previousElementSibling` von `.table-responsive`, das je View unterschiedlich ist (u. a. ein
   Anomalie-Warnbanner bei Kommissionier-/Vormontage-Liste). Die Platzierung muss **robust** sein —
   der Knopf bleibt auffindbar und bedienbar, auch wenn davor ein Anomalie-Banner steht — aber nicht
   pixelgleich zu `ProductionOrders`.
2. Sichtbarkeit, Breite und Reihenfolge einer Spalte lassen sich je Liste aendern, bleiben nach
   Reload erhalten (Server-Persistenz via `PUT /api/user-view-preferences/{viewKey}` liefert `200`,
   nicht `400`) und sind je Benutzer getrennt (zweiter Benutzer sieht seine eigene, unabhaengige
   Konfiguration).
3. `GET /api/user-view-preferences/FaHierarchyKommissionierListen` (und die drei weiteren `viewKey`)
   liefert `204 NoContent` ohne gespeicherte Praeferenz bzw. `200` mit den gespeicherten Settings —
   in keinem Fall `400 BadRequest` (Regressionstest fuer Fachliche Anforderung 2, analog zum
   bestehenden `FaWorklist`-Testfall in `UserViewPreferencesApiControllerTests.cs`).
4. Identifizierende Spalten (`hauptfa`+`matchcode` bzw. nur `matchcode` bei
   `FaHierarchyVormontageSummiert`) lassen sich im Zahnrad-Dialog **nicht** ausblenden (`locked:
   true`).
5. Bei `FaHierarchyKommissionierListen` sind `artikeltyp`, `beschichtet`, `material` im
   Erstzustand (kein gespeichertes Profil) ausgeblendet; alle anderen Spalten sichtbar.
6. `FaHierarchyKommissionierListen`, `FaHierarchyBeschichtung`, `FaHierarchyVormontageEinzeln` bieten
   im Zahnrad-Dialog **keine** Option "Standard-Sortierung speichern" (`supportsSortDefault: false`);
   `FaHierarchyVormontageSummiert` bietet sie (`supportsSortDefault: true`), analog `ProductionOrders`.
7. `column-preferences.js` ist in allen vier Views **vor** `table-filter.js` eingebunden (Code-Review-
   pruefbar, siehe `fallstricke.md`-Regel).
8. `Print.cshtml`-Ausdrucke (`FaHierarchyKommissionierListen/Print`, `FaHierarchyBeschichtung/Print`)
   bleiben von dieser Aenderung unberuehrt — identischer Spaltenumfang/-reihenfolge unabhaengig von
   der Bildschirm-Konfiguration eines Benutzers.
9. Bestehende Funktionalitaet der vier Views (Server-Spaltenfilter, Gruppen-Pagination,
   Anomalie-Banner, Zugriffskontrolle) bleibt vollstaendig unveraendert — keine Regression in den
   bestehenden `FaHierarchyListBuilderTests` u. ae.
10. Bei einer Liste mit **mindestens zwei** `HauptFA`-Gruppen wirkt eine Sichtbarkeits-/Reihenfolge-
    Aenderung im Zahnrad-Dialog **in jeder Gruppe gleich** (nicht nur in der ersten) — `column-
    preferences.js` arbeitet bereits tabellenweit (`_table.querySelectorAll('tbody tr')`), dieser
    Test beweist es statt es anzunehmen (Kritische Pruefung, SOLLTE 2).
11. Bei `FaHierarchyKommissionierListen`, `FaHierarchyBeschichtung` und
    `FaHierarchyVormontageEinzeln` sortiert ein Klick auf eine sortierbare Spaltenkopfzeile bei einer
    Liste mit **mindestens zwei** `HauptFA`-Gruppen die Positionszeilen **innerhalb jeder** Gruppe
    fuer sich; Gruppenkopfzeilen bleiben unveraendert an ihrer Position, keine Positionszeile wandert
    in eine andere Gruppe (Regressionstest fuer den Sortier-Fix aus Fachlicher Anforderung 5).
12. **Harte Regressions-AK:** Bestehende, nicht-gruppierte (Ein-`tbody`) `filterable-table`-Listen
    (mindestens stichprobenartig geprueft an `ProductionOrders/Index`) zeigen nach dem
    `table-filter.js`-Sortier-Fix **exakt** dasselbe Sortierverhalten wie zuvor — keine sichtbare
    Aenderung, keine Regression.

## Test-Szenarien

Ergaenzung der bestehenden Kapitel in `docs/TESTSZENARIEN.md`:

**TS-59 (Teil 3 — Kommissionierlisten), TS-60 (Teil 4 — Beschichtungsauftrag), TS-61 (Teil 5 —
Vormontage-Listen):** je Kapitel neuer Abschnitt "Spaltenauswahl":
- Zahnrad oeffnen, eine nicht-gesperrte Spalte ausblenden, Seite neu laden → Spalte bleibt
  ausgeblendet.
- Spaltenbreite per Ziehgriff aendern, Seite neu laden → Breite bleibt erhalten.
- Zwei verschiedene Benutzer (oder zwei Browserprofile) auf derselben Liste → unabhaengige
  Konfigurationen, keine gegenseitige Ueberschreibung.
- `hauptfa`/`matchcode` (bzw. nur `matchcode` bei Summiert) lassen sich nicht ausblenden.
- Standard-Sortierung: bei `FaHierarchyVormontageSummiert` speicherbar und wirksam nach Reload; bei
  den drei gruppierten Listen ist die Option im Dialog **nicht vorhanden**.
- Server-Spaltenfilter (`?colf_...`) funktionieren nach Ausblenden der gefilterten Spalte weiter
  unveraendert (Filterung bleibt aktiv, auch ohne sichtbare Spalte — bewusst dokumentiertes,
  unveraendertes Verhalten dieser Spec, siehe Fachliche Anforderungen Punkt 4).
- Druck (`Print`) zeigt weiterhin alle Spalten unabhaengig von der Bildschirm-Konfiguration.
- **Neu — Multi-Gruppen-Test Spaltenauswahl:** Liste mit **mindestens zwei** `HauptFA`-Gruppen: eine
  Spalte im Zahnrad-Dialog ausblenden bzw. per Drag umordnen → Aenderung wirkt in **jeder** Gruppe
  gleich, nicht nur in der ersten (Kritische Pruefung, SOLLTE 2).
- **Neu — Multi-Gruppen-Test Sortierung:** Liste mit **mindestens zwei** `HauptFA`-Gruppen: Klick auf
  eine sortierbare Spaltenkopfzeile → Positionszeilen sortieren sich **innerhalb jeder** Gruppe fuer
  sich, Gruppenkopfzeilen bleiben an ihrer Position, keine Position wandert in eine andere Gruppe
  (Sortier-Fix aus Fachlicher Anforderung 5).

**Regressions-Sichtpruefung (Pflicht vor Merge):** Eine bestehende flache Liste (z. B.
`ProductionOrders/Index`) nach dem `table-filter.js`-Fix erneut sortieren/filtern und mit dem
Verhalten vor der Aenderung vergleichen — keine sichtbare Abweichung zulaessig. Kein eigenes
`docs/TESTSZENARIEN.md`-Kapitel dafuer noetig (`ProductionOrders` hat bereits ein eigenes Kapitel);
hier nur als Pflicht-Teilschritt der Abnahme dieser Spec vermerkt.

**Automatisiert (`UserViewPreferencesApiControllerTests.cs`):** vier neue Testfaelle analog zum
bestehenden `FaWorklist`-Regressionstest — `Get`/`Put`/`Delete` fuer jeden der vier neuen `viewKey`
liefern **keinen** `400 BadRequest`. Fuer den `table-filter.js`-Sortier-Fix existiert kein
JS-Test-Runner im Projekt (reines Client-JS) — die Absicherung erfolgt ausschliesslich ueber die
manuellen Multi-Gruppen-/Regressions-Testszenarien oben.

Nach Abschluss `secondbrain/tests/testszenarien-index.md` nachziehen.

## Deploy

- **Web-App:** ja.
- **Service:** nein.
- **Migration:** nein.
- **Kontext/Reihenfolge:** Die betroffenen vier Views existieren aktuell ausschliesslich im noch
  nicht gemergten Epic-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5` (Branch
  `feature/2026-08-07-ideal-teile-1-5`, Status laut Aufgaben-Notiz `Testbereit`, wartet auf
  Schranke 2/manuelles UAT). Diese Spec wird dort als zusaetzliche Etappe VOR dem Merge umgesetzt
  (Freigabe-Antwort 2) — es entsteht **kein** zusaetzlicher Deploy-Schritt, sie geht im selben
  Publish/Merge des Epic-Buendels mit.
- **Gemeinsam genutztes JS:** `table-filter.js` wird von **allen** `filterable-table`-Listen der
  Anwendung eingebunden, nicht nur den vier IDEAL-Listen dieser Spec. Der Sortier-Fix ist zwar laut
  Analyse fuer Ein-`tbody`-Tabellen verhaltensgleich, ist aber trotzdem ein Eingriff in geteilten
  Code — vor dem Merge zusaetzlich eine kurze Regressions-Sichtpruefung auf mindestens einer
  bestehenden flachen Liste einplanen (siehe Test-Szenarien).
- **Publish-Befehle (nachgelagerter Fall):**
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`

## Offene Rueckfragen

1. → **Vorbedingung Tree-Table:** Die FA-Struktur-Baumanzeige (`FaHierarchy/Index.cshtml`, Teil 2)
   ist bewusst NICHT Teil dieser Spec — `column-preferences.js` braucht eine echte Tabelle, die es
   dort erst nach Umsetzung von [[2026-08-12-fa-struktur-darstellung]] gibt. Bestaetigung erbeten,
   dass die Baumanzeige-Spaltenauswahl als separater Nachtrag NACH jener Spec folgt (nicht Teil
   dieser oder einer gemeinsamen Spec).
2. → **Ausfuehrungsort:** Im bestehenden, noch offenen Epic-Worktree
   `.claude/worktrees/2026-08-07-ideal-teile-1-5` fortsetzen (Empfehlung) oder eigener neuer
   Worktree nach dem Epic-Merge?
3. → **ADR-0005-Form:** Additiver Nachtrag direkt in der bestehenden ADR-0005-Datei (Empfehlung)
   oder neuer, praezisierender ADR?
4. → **Chip-Hinweis fuer ausgeblendete Spalte mit aktivem Server-Filter:** Fuer diese Spec vertagen/
   nur dokumentieren (Empfehlung, Scope-Disziplin) oder jetzt als echte Erweiterung von
   `table-filter.js`/`column-preferences.js` mitbauen?
5. → **Sortier-Fallstrick bei gruppierten Tabellen (nur erstes `<tbody>` wird sortiert):** Nur
   dokumentieren + separate Bug-Meldung fuer die Epic-Naht (Empfehlung) oder im selben Aufwasch
   root-cause-beheben?

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

1. → **Bestaetigt: Tree-Table ist Vorbedingung und NICHT Teil dieser Spec.** Der Anschluss der
   Baumanzeige an `column-preferences.js` wandert aber **nicht** in eine dritte Spec, sondern in
   [[2026-08-12-fa-struktur-darstellung]] als letzten Schritt — wer die Tabelle baut, verdrahtet
   sie auch. Reihenfolge: **diese Spec zuerst** (vier echte Tabellen, keine Vorbedingung), dann die
   Baum-Spec; die drei Bloecke sind bis dahin erprobt.
2. → **Im bestehenden Epic-Worktree fortsetzen** — Empfehlung des Spec-Laufs bestaetigt. Zwingend,
   weil die vier Views nur dort existieren und nicht auf `main`. Beide neuen Specs laufen als
   zusaetzliche Etappen; **ein** Merge, **eine** Abnahme. Preis bewusst akzeptiert: Der Epic geht
   von `Testbereit` auf `InUmsetzung` zurueck und die QA ist am Ende erneut zu fahren — billig,
   solange die Abnahme ohnehin auf ein befuelltes Testsystem wartet.
3. → **Additiver Nachtrag direkt in ADR 0005**, nicht als neuer ADR. Es wird keine Entscheidung
   revidiert, sondern ein bisher unbenannter vierter Pflichtbestandteil ergaenzt — und ein
   praezisierender Zweit-ADR wuerde das Listenmuster auf zwei Dokumente verteilen. Genau diese
   Zersplitterung ist die Ursache des Problems: Wer „nach ADR 0005" liest, muss **alles** finden,
   was eine vollstaendige Liste ausmacht. Nachtrag mit Datum und Begruendung kennzeichnen
   (Praezedenzfall „Nachtraeglich erfasst" existiert dort bereits).
4. → **Vertagen — dokumentieren statt bauen, mit einem Zusatz.** Empfehlung des Spec-Laufs
   bestaetigt: Ein Chip-Hinweis waere eine echte Aenderung an gemeinsam genutztem JS und damit mehr
   als „anschliessen". Zusatz, der es vertretbar macht: Der bestehende
   `data-clear-table-filters`-Link raeumt auch Filter unsichtbarer Spalten weg — es gibt also einen
   Notausgang. **In die Spec und in `fallstricke.md` aufnehmen**, damit der Fall bekannt ist, wenn
   ihn jemand meldet, und als eigener Backlog-Punkt fuehren.
5. → **(a) dokumentieren und trennen — aber der Fix muss VOR den produktiven Einsatz der Listen.**
   Die Analyse ist ueberzeugend: `table-filter.js` greift auf `table.querySelector('tbody')` und
   sortiert damit nur die erste `HauptFA`-Gruppe. Das ist ein vorbestehender Defekt, kein
   Nebenprodukt dieser Spec — also **nicht hier mitfixen** (Root-Cause-Aenderung an gemeinsam
   genutztem JS ist ein eigener, abgrenzbarer Task) und `supportsSortDefault: false` defensiv
   setzen. Korrekt.
   **Aber nicht nur ein Fallstricke-Eintrag:** Es gehoert ein **Bug-Record in `bugs/`**, denn der
   Effekt ist fuer den Anwender sichtbar und wirkt wie ein kaputtes Programm — er klickt auf eine
   Spaltenueberschrift, und nur die oberste Gruppe sortiert sich. Bei drei brandneuen Listen trifft
   ihn das sofort. **Empfehlung zur Reihenfolge:** den Fix als eigenen kleinen Task noch
   **vor** dem Epic-Merge einplanen. Die Richtung ist absehbar klein — ueber alle `tbody`-Elemente
   iterieren und **innerhalb** jedes Elements sortieren — was zugleich die gewollte Eigenschaft
   erhaelt, dass Zeilen nie ueber Gruppengrenzen wandern.

## Kritische Pruefung (2026-08-12)

Anwalt-des-Teufels-Durchgang gegen den TATSAECHLICHEN Worktree-Stand
(`.claude/worktrees/2026-08-07-ideal-teile-1-5`, incl. `column-preferences.js`, `table-filter.js`,
`ColumnDefinitions.cs`, `UserViewPreferencesApiController.cs`, die vier IDEAL-Views, ADR 0005). Die
technische Substanz der Spec ist ueberdurchschnittlich belastbar — die vier `data-view-key`-Werte,
alle `data-col-key`-Listen, `migration: false` und der Sortier-Fallstrick wurden **im Code
verifiziert und stimmen** (Details unten). Trotzdem drei Punkte, die vor der Freigabe zu klaeren
sind.

**Verifiziert (kein Handlungsbedarf, zur Beweissicherung):**
- viewKeys der Views == Spec == neue `ColumnDefinitions`-Keys: `FaHierarchyKommissionierListen`,
  `FaHierarchyBeschichtung`, `FaHierarchyVormontageEinzeln`, `FaHierarchyVormontageSummiert`
  (Views haben `data-view-key` **bereits** aus Teil 3/4/5; Spec fuegt korrekt nur die zwei
  JSON-Bloecke + Skript-Include hinzu, kein Markup-Umbau noetig).
- Spaltenlisten (11/9/8/3) und `data-col-key` je `<th>` deckungsgleich mit Fachlicher Anforderung 3.
- `UserViewPreferencesApiController` prueft tatsaechlich `GetByViewKey(...) == null → BadRequest`
  (Zeilen 29/45/67) → die C#-Registrierung ist zwingend, Backlog-„kein C#" korrekt widerlegt.
  Der `FaWorklist`-Regressionstest existiert (`UserViewPreferencesApiControllerTests.cs`,
  `Get_FaWorklistViewKey_IsAccepted`) — die vier neuen Analog-Tests sind sauber ableitbar.
- Sortier-Fallstrick exakt bestaetigt: `table-filter.js` Zeile 130 `_tbody =
  _table.querySelector('tbody')` (erstes tbody), Klick-Sort Zeilen 222–246 **unbedingt** an jedes
  `th[data-filterable]` gebunden — server- wie client-Mode. `migration: false` korrekt
  (`UserViewPreference` ist viewKey-agnostisch, `FaWorklist`-Praezedenz ohne Migration).
- Scope-Grenze zur Schwester-Spec [[2026-08-12-fa-struktur-darstellung]] **widerspruchsfrei**:
  beide Specs sagen „diese Spec zuerst", die Baumanzeige wird hier NICHT angefasst, und das
  spaetere `column-preferences`-Anschliessen der Baumanzeige liegt in der fa-struktur-Spec.

**BLOCKER** — keiner (Inhalt ist umsetzbar).

**SOLLTE**

1. **Sichtbarer Sort-Defekt geht ungefixt mit drei brandneuen Listen live — die Gegenmassnahme
   haengt an einem noch nicht existierenden Task.** `supportsSortDefault: false` verhindert nur die
   **automatische** Sortierung beim Laden (`triggerSort`), **nicht** den Klick auf eine
   Spaltenueberschrift: der Handler in `table-filter.js` (Z. 230) ist unabhaengig davon gebunden und
   sortiert nur die erste HauptFA-Gruppe. Die Spec ist darueber ehrlich, aber ihre Absicherung ist
   Prosa (Freigabe-Antwort 5: „Fix vor produktivem Einsatz", „Bug-Record in `bugs/`", „vor Epic-Merge
   einplanen") — ohne angelegten Record und ohne harte Merge-Gate. Da diese Spec die drei Listen erst
   auffaellig macht, gehoert die Absicherung in die Spec selbst: **entweder** den kleinen
   `table-filter.js`-Fix (ueber ALLE `<tbody>` iterieren, **innerhalb** jedes sortieren) in DIESEN
   Dev-Lauf ziehen, **oder** den Bug-Record jetzt anlegen, hier verlinken und im Deploy-Abschnitt als
   verbindliche Vorbedingung des Epic-Merge fuehren. „Erledigt" darf nicht von Goodwill abhaengen.

2. **Test-Luecke: Spalte-ausblenden/-umordnen ueber MEHRERE Gruppen nicht abgesichert.** Genau die
   Fehlerklasse „nur das erste tbody" ist in diesem Codebestand nachweislich lebendig (Sort-Bug).
   `column-preferences.js` blendet/ordnet zwar korrekt table-weit (`_table.querySelectorAll('tbody
   tr')`, verifiziert — funktioniert ueber alle Gruppen), aber die Test-Szenarien fordern das nicht
   ab. AK 5 / TS-59-61 um einen Fall ergaenzen: Liste mit **mindestens zwei HauptFA-Gruppen**, Spalte
   ausblenden → Spalte in **jeder** Gruppe verschwindet (nicht nur in der ersten); analog fuer
   Umordnen. Beweist die Multi-tbody-Tauglichkeit statt sie anzunehmen.

3. **Doppelablage inkonsistent: die Spec liegt bereits in `freigegeben/`, dort aber mit
   `status: Entwurf` und leerem `freigabe_entscheidung`/`freigabe_von`/`freigabe_am`.** Eine Datei in
   `freigegeben/` mit Status „Entwurf" und ohne erfasste Schranke-1-Entscheidung ist ein Widerspruch,
   der Cockpit/Pipeline verwirrt. Vor der Freigabe aufloesen: entweder die verfruehte Kopie in
   `freigegeben/` entfernen (bis Schranke 1 formal erfasst ist), oder — wenn die Freigabe erfolgt —
   Status/`freigabe_*` in **beiden** Kopien konsistent setzen. (Die Prosa-Antworten sind vollstaendig;
   die Frontmatter-Freigabefelder sind es nicht.)

**HINWEIS**

- **AK 1 „Zahnrad identisch zu ProductionOrders" ist zu stark.** `insertGearButton` haengt den
  Button an das `previousElementSibling` der `.table-responsive`, sofern das ein `<div>` ist. Ueber
  den vier Views steht Heterogenes: Kommissionier/Vormontage haben ein Anomalie-Warnbanner (`<div
  class="alert">`) → der Button landet **im gelben Banner**; Vormontage hat davor `<ul class="nav">`
  (kein div) → Fallback-Wrapper; Beschichtung hat die `filter-card`. Die Platzierung wird also je View
  unterschiedlich und teils unschoen (im Alert). Kein Blocker — bestehendes Shared-Verhalten —, aber
  der Dev-Lauf soll die Zahnrad-Position je View sichten und AK 1 auf „Zahnrad vorhanden und bedienbar"
  statt „identisch platziert" abschwaechen.
- **Brain-Schreibziele im Worktree-Lauf.** `affected_code` listet ADR 0005, `fallstricke.md`,
  `testszenarien-index.md` — diese sind Brain und muessen in den HAUPTCHECKOUT
  `C:\Git\IDEAL-AKE-WMS\secondbrain\` geschrieben werden, nicht in den Worktree (sparse-checkout
  blendet `secondbrain/` dort aus). Nur `docs/TESTSZENARIEN.md` ist Zweig-Inhalt (Worktree).
- ADR 0005 nennt „column-preferences.js vor table-filter.js" schon heute in den Risiken (Z. 81–82),
  fuehrt Spaltenpraeferenzen aber nicht als Pflicht-Pattern-Teil — die additive Ergaenzung ist also
  berechtigt und kollidiert nicht mit Bestehendem.

**NACHBESSERUNG NOETIG:** Die Absicherung des sichtbaren Sort-Defekts (SOLLTE 1) darf nicht an einem
uneingeplanten Task und Prosa haengen — Fix einziehen ODER Bug-Record anlegen + als Merge-Gate
verankern; zusaetzlich Multi-Gruppen-Test (SOLLTE 2) und die `freigegeben/`-Doppelablage (SOLLTE 3)
bereinigen. Der Rest der Spec ist inhaltlich freigabereif.

## ANTWORTEN auf die Kritische Pruefung (2026-08-12, zweiter Durchgang)

**Zu SOLLTE 1 — Einwand berechtigt. Bug-Record + HARTES Merge-Gate, nicht Fix in diesem Lauf.**
„Erledigt darf nicht von Goodwill abhaengen" trifft zu; meine vorige Antwort war an dieser Stelle zu
weich. Gewaehlt wird trotzdem die Trennung, aber mit Zaehnen:
- **Nicht in diesen Lauf ziehen.** `table-filter.js` ist gemeinsam genutztes JS — jede Liste der
  Anwendung haengt daran. Eine Aenderung dort verdient einen eigenen Testumfang und nicht die
  Mitnahme in einem View-Verdrahtungs-Task. Die Scope-Disziplin bleibt.
- **Bug-Record JETZT anlegen** (`bugs/`), hier verlinken, und im **Deploy-Abschnitt als verbindliche
  Vorbedingung des Epic-Merge** fuehren: Der Epic wird nicht gemergt, solange der Defekt offen ist.
  Das ist das Gate, das der Reviewer zu Recht verlangt.
- **`supportsSortDefault: false`** bleibt defensiv gesetzt.
- **Umsetzungsweg:** eigener kleiner Worktree von `main`, Fix (ueber alle `<tbody>` iterieren,
  **innerhalb** jedes sortieren — fuer Ein-`tbody`-Tabellen verhaltensgleich), Merge nach `main`,
  danach `sync-worktree.ps1` zieht ihn in den Epic-Zweig. Reihenfolge ist damit sauber.

**Zu SOLLTE 2 — uebernommen, ohne Vorbehalt.** AK 5 und TS-59/60/61 bekommen den Fall: Liste mit
**mindestens zwei** `HauptFA`-Gruppen, Spalte ausblenden → verschwindet in **jeder** Gruppe; analog
fuer Umordnen. Der Punkt ist stark, weil genau diese Fehlerklasse in diesem Codebestand nachweislich
lebt — dass `column-preferences.js` es richtig macht, wird damit **bewiesen** statt angenommen.

**Zu SOLLTE 3 — Doppelablage aufloesen, vor der Freigabe.** Massgeblich ist die `entwurf/`-Fassung
(dort wurde gearbeitet). Die Kopie in `freigegeben/` traegt zudem `status: Entwurf` und leere
`freigabe_*`-Felder — eine Datei im Freigabe-Ordner ohne erfasste Schranke-1-Entscheidung ist ein
Widerspruch, den Cockpit und Pipeline nicht aufloesen koennen. **Die verfruehte `freigegeben/`-Kopie
entfernen** (Ordner-Geste des Menschen); die Freigabe erfolgt danach regulaer mit gesetztem
Frontmatter. Gleiches gilt fuer die Schwester-Spec.

**Zu HINWEIS „Zahnrad-Platzierung" — AK abschwaechen UND die Ursache beheben.**
AK 1 wird auf **„Zahnrad vorhanden und bedienbar"** abgeschwaecht — richtig, `insertGearButton`
haengt am `previousElementSibling`, und das ist je View etwas anderes. Aber die Konsequenz ist nicht
nur kosmetisch: In Kommissionier- und Vormontage-Liste landete der Knopf **im gelben Warnbanner**.
Ein Bedienelement in einer Warnmeldung sieht nach Fehler aus.
**Deshalb zusaetzlich, ohne JS-Aenderung:** In den betroffenen Views einen konsistenten
`<div>`-Container unmittelbar vor `.table-responsive` vorsehen, damit der Knopf ueberall an
derselben, ruhigen Stelle landet. Das ist eine View-Aenderung im Umfang dieser Spec — kein Eingriff
in gemeinsam genutztes JS.

**Zu HINWEIS „Brain-Schreibziele" — bestaetigt, gilt unveraendert.** ADR 0005, `fallstricke.md` und
`testszenarien-index.md` sind Brain und gehen in den **Hauptcheckout**; nur `docs/TESTSZENARIEN.md`
ist Zweig-Inhalt. Der Worktree hat per sparse-checkout ohnehin kein `secondbrain/`.

**Zu HINWEIS „ADR 0005 nennt die Reihenfolge bereits in den Risiken" — zur Kenntnis, und es
stuetzt die Ergaenzung.** Dass dort `column-preferences.js` vor `table-filter.js` bereits als Risiko
auftaucht, das Pattern selbst die Spaltenpraeferenzen aber **nicht** als Pflichtbestandteil fuehrt,
ist genau die Luecke: Die Reihenfolge einer Sache zu regeln, die man nicht verlangt, hilft niemandem.
Der additive Nachtrag schliesst das.

### Nachbesserung (2026-08-12)

Vom Menschen nachtraeglich getroffene Entscheidung (nach den ANTWORTEN oben): Der Sort-Defekt-Fix wird
**doch** in diese Spec gezogen, statt als separater Bug-Task mit Merge-Gate zu laufen. Diese
Entscheidung ersetzt die entsprechenden Teile von Freigabe-Antwort 5 und der ANTWORTEN-Sektion zu
SOLLTE 1 (dort noch: „nicht in diesen Lauf ziehen"). Stand je Befund:

- **SOLLTE 1 (Sortier-Defekt-Absicherung) — behoben, in diese Spec gezogen.** `table-filter.js`
  bekommt den unter Fachlicher Anforderung 5 beschriebenen Pro-`<tbody>`-Sortier-Fix als Teil dieser
  Spec (siehe In-Scope, `affected_code`, Technischer Loesungsentwurf, AK 11/12). Ein separater
  Bug-Record in `bugs/` und ein eigenes Merge-Gate sind damit **nicht mehr** noetig — der Fix ist
  Bestandteil des Dev-Laufs dieser Spec und wird mit ihr gemeinsam getestet/gemergt.
- **SOLLTE 2 (Multi-Gruppen-Test) — erledigt.** AK 10/11 und die Testszenarien-Ergaenzung zu
  TS-59/60/61 decken jetzt sowohl Spaltenauswahl (Ausblenden/Umordnen) als auch Sortierung ueber
  **mehrere** `HauptFA`-Gruppen ab — genau die Fehlerklasse „nur erstes tbody", die in diesem
  Codebestand nachweislich lebte.
- **SOLLTE 3 (Doppelablage `freigegeben/`) — entfaellt, geprueft.** Es existiert aktuell **keine**
  Kopie dieser Spec unter `secondbrain/specs/freigegeben/` (Verzeichnis-Check 2026-08-12) — die
  fruehere verfruehte Kopie wurde bereits entfernt. Zusaetzlich korrigiert: Das Status-Feld dieser
  `entwurf/`-Datei stand faelschlich auf `Freigegeben`, obwohl `freigabe_entscheidung` weiterhin leer
  ist (Schranke 1 formal nicht erfasst) — zurueckgesetzt auf `status: Entwurf`.
- **HINWEIS „Zahnrad-Platzierung" — erledigt, wie vorgegeben abgeschwaecht.** AK 1 verlangt jetzt nur
  noch „vorhanden und funktional; Platzierung robust (auch wenn ein Anomalie-Banner davor steht)".
  Der in den ANTWORTEN skizzierte zusaetzliche, vereinheitlichende Container-`<div>` vor
  `.table-responsive` wird **nicht** als Pflichtteil dieser Spec gefuehrt (Scope-Disziplin) — der
  Dev-Lauf kann ihn optional ergaenzen, ohne dass es fuer die Abnahme erforderlich ist.
- **HINWEIS „Brain-Schreibziele" und „ADR-0005-Risiko-Erwaehnung" — unveraendert gueltig,** keine
  weitere Aenderung noetig.

**Scope-Grenze zur Schwester-Spec [[2026-08-12-fa-struktur-darstellung-spec]] bestaetigt:** Diese Spec
umfasst ausschliesslich die **vier flachen IDEAL-Listen** (Kommissionierlisten, Beschichtungsauftrag,
Vormontage-Einzeln, Vormontage-Summiert) inkl. des hier neu aufgenommenen `table-filter.js`-Sort-Fixes;
die **Baumanzeige** (`Views/FaHierarchy/Index.cshtml`, Tree-Table-Umbau) ist und bleibt **nicht** Teil
dieser Spec. Reihenfolge unveraendert: **diese Spec zuerst**, danach die Baum-Spec (dort auch der
`column-preferences.js`-Anschluss der Baumanzeige, siehe deren Freigabe-Antwort 2).
