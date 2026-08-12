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
  - docs/TESTSZENARIEN.md (Kapitel TS-59/TS-60/TS-61 um Zahnrad-Dialog-Schritte ergaenzen)
  - secondbrain/tests/testszenarien-index.md (nachziehen)
  - secondbrain/architektur/adr/0005-listen-view-pattern-mit-server-side-spaltenfilter.md (additiv ergaenzen ODER neuer praezisierender ADR — Form siehe Offene Rueckfrage 3)
  - secondbrain/architektur/fallstricke.md (additiv: ColumnDefinitions-Registrierungspflicht bei neuem viewKey; empfohlen auch der in dieser Spec entdeckte Sortier-Fallstrick bei mehr-tbody/gruppierten Tabellen, siehe Fachliche Anforderungen Punkt 5)
open_questions:
  - "Vorbedingung Tree-Table (FaHierarchy/Index.cshtml, Teil 2 der IDEAL-Struktur-Baumanzeige): column-preferences.js braucht eine echte Tabelle mit stabilen Spaltenindizes (thead tr:first-child th); der heutige freifliessende Baum hat das nicht. Die Umstellung ist Gegenstand von [[2026-08-12-fa-struktur-darstellung]] und explizit NICHT Teil dieser Spec. Reihenfolge: sobald jene Spec umgesetzt ist, braucht die Baumanzeige einen eigenen (kleinen) Nachtrag nach demselben Muster wie hier."
  - "Ausfuehrungsort: im bereits offenen, noch nicht gemergten Epic-Worktree .claude/worktrees/2026-08-07-ideal-teile-1-5 (Branch feature/2026-08-07-ideal-teile-1-5, Status Testbereit, wartet auf Schranke 2) als zusaetzliche Etappe fortsetzen, oder eigener neuer Worktree? Empfehlung: im bestehenden Worktree fortsetzen (die betroffenen Views existieren nur dort, noch nicht auf main; ein zweiter Worktree wuerde auf main nichts zum Anfassen finden und beim spaeteren Merge kollidieren)."
  - "ADR-0005-Form: additiver Nachtrag direkt in der bestehenden ADR-0005-Datei (Praezedenzfall: der Kasten „Nachtraeglich erfasst" steht dort bereits) oder neuer ADR (z. B. 0012), der 0005 praezisiert? Empfehlung: additiver Nachtrag in 0005 selbst, da keine bestehende Entscheidung revidiert, sondern nur ein vierter, bisher unbenannter Pflichtbestandteil ergaenzt wird — aber Form ist bewusst Schranke-1-Entscheidung, nicht Spec-Agent-Entscheidung."
  - "Ausgeblendete Spalte mit aktivem Server-Spaltenfilter: die Server-Filter wirken ueber die URL (?colf_<col-key>=...) voellig unabhaengig von der (DB-gespeicherten) Spalten-Sichtbarkeit — ein Anwender kann also eine gefilterte, aber unsichtbare Spalte haben, ohne dass der Grund fuer die kuerzere Liste sichtbar ist (technisch verifiziert, siehe Fachliche Anforderungen Punkt 4). Soll das in dieser Spec bereits mit einem Chip-Hinweis geloest werden (echte Code-Aenderung an table-filter.js/column-preferences.js, damit Mehr-Umfang als „nur anschliessen") oder nur dokumentiert und als eigener Backlog-Punkt vertagt werden? Empfehlung: fuer diese Spec nur dokumentieren/vertagen (Scope-Disziplin, Backlog-Vorgabe „keine Aenderung an column-preferences.js ausser begruendet")."
  - "Der in dieser Spec entdeckte Sortier-Fallstrick (Fachliche Anforderungen Punkt 5: Klick auf eine sortierbare Spaltenkopfzeile sortiert bei den drei gruppierten FaHierarchy-Listen nur die ERSTE HauptFA-Gruppe, weil table-filter.js sich global auf table.querySelector('tbody') — also nur das erste tbody-Element — stuetzt) ist ein vorbestehender, von dieser Spec unabhaengiger Defekt im noch nicht gemergten Epic-Worktree, keine Colone-Preferences-Neuerung. Soll er (a) nur als Fallstricke-Eintrag + separate Bug-Meldung an die Epic-Naht dokumentiert werden (diese Spec setzt defensiv nur supportsSortDefault:false), oder (b) im selben Aufwasch in table-filter.js root-cause-behoben werden? Empfehlung: (a) — Root-Cause-Fix an gemeinsam genutztem JS ist ein eigener, sauber abgrenzbarer Task, keine Nebensache dieser Spec."
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
zweigeteilt:

1. **Vier konkrete Listen** bekommen den fehlenden Anschluss (Views + eine bislang uebersehene
   Server-Registrierung).
2. **ADR 0005** (Listen-View-Pattern) wird um einen bisher unbenannten vierten Pflichtbestandteil
   ergaenzt — Spaltenpraeferenzen —, damit dieselbe Luecke nicht bei jeder kuenftigen Liste erneut
   entsteht. Die Teil-3-Spec der IDEAL-Epic hatte den ADR-0005-Listenteil explizit eingefordert
   (Pagination, Filterkarte, Server-Spaltenfilter), die Spaltenpraeferenzen aber nie erwaehnt — der
   Dev-Lauf hat exakt das gebaut, was verlangt war, nicht mehr. Das ist kein Einzelversehen, sondern
   eine Luecke im Muster selbst.

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
- ADR-0005-Ergaenzung um den vierten Pflichtbestandteil "Spaltenpraeferenzen" (Form: Offene
  Rueckfrage 3).
- Testszenarien-Ergaenzung in den bestehenden Kapiteln TS-59/TS-60/TS-61.

**Out-of-Scope:**
- `IdealAkeWms/Views/FaHierarchy/Index.cshtml` (Teil-2-Baumanzeige, freifliessender Baum ohne
  ausgerichtete Spalten) — Vorbedingung Tree-Table ist [[2026-08-12-fa-struktur-darstellung]], siehe
  Offene Rueckfrage 1.
- `Print.cshtml`-Views (`FaHierarchyKommissionierListen/Print.cshtml`,
  `FaHierarchyBeschichtung/Print.cshtml`): geprueft — beide sind bereits eigenstaendige,
  `Layout = null`-HTML-Dokumente mit fest verdrahtetem Inline-CSS, komplett unabhaengig von
  `filterable-table`/`column-preferences.js`. Die Bildschirm-Spaltenauswahl hat auf den Ausdruck
  **strukturell keinen Einfluss** — das entspricht bereits dem in der Backlog-Notiz vorgeschlagenen
  "festes Druck-Layout". Keine Aenderung noetig, kein Abgleich mehr offen.
- Keine Aenderung an `column-preferences.js`, `table-filter.js` oder der
  `UserViewPreferencesApiController`-API-Vertrag (Endpunkte/Contract bleiben unveraendert — die neuen
  `ViewConfig`-Eintraege sind reine Daten, keine Logikaenderung).
- Keine neue Migration, keine neue Rolle, kein neues AppSetting/Toggle (Access-Filter und
  Feature-Toggles dieser vier Views bleiben aus Teil 3/4/5 unveraendert).
- Kein Fix des in Fachlicher Anforderung 5 entdeckten Sortier-Fallstricks in `table-filter.js` (siehe
  Offene Rueckfrage 5).
- Kein Chip-Hinweis fuer "ausgeblendete Spalte mit aktivem Filter" (siehe Offene Rueckfrage 4).

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
   dafuer **keine** Loesung (kein Chip-Hinweis, siehe Out-of-Scope/Offene Rueckfrage 4) — die
   Wechselwirkung ist hiermit aber belegt, nicht mehr nur vermutet, und damit entscheidungsreif fuer
   Schranke 1.

5. **Sortier-Fallstrick bei den drei gruppierten Listen — technisch verifiziert, vorbestehend,
   unabhaengig von dieser Spec.** `Kommissionierlisten`, `Beschichtung` und `Vormontage-Einzeln`
   rendern je `HauptFA`-Gruppe ein **eigenes** `<tbody>` (Kommentar in den Views: "Je HauptFA-Gruppe
   ein eigenes tbody ... table-filter.js kann Zeilen nie ueber Gruppengrenzen mischen"). Das stimmt
   fuer den **Filter**, aber **nicht** fuer die **Sortierung**: `table-filter.js` bindet beim Init
   `_tbody = _table.querySelector('tbody')` — das liefert nur das **erste** `<tbody>`-Element im DOM
   — und `sortTable()`/`th`-Klick-Handler (bereits heute unbedingt an jedes `<th data-filterable>`
   gebunden, unabhaengig vom Server-/Client-Filter-Modus) sortieren ausschliesslich Zeilen **dieses
   ersten** `tbody`. Ein Klick auf eine sortierbare Spaltenkopfzeile sortiert bei diesen drei Listen
   also nur die **erste** `HauptFA`-Gruppe um, alle anderen Gruppen bleiben unveraendert — ein
   irrefuehrendes, vorbestehendes Verhalten im noch nicht gemergten Epic-Worktree, ausgeloest allein
   durch das bereits vorhandene `data-filterable`, unabhaengig von dieser Spec.
   `column-preferences.js` wuerde diesen Defekt **zusaetzlich sichtbar** machen, weil
   `supportsSortDefault: true` dem Anwender im Zahnrad-Dialog anbietet, eine Standard-Sortierung zu
   **speichern**, die dann bei **jedem** Laden automatisch (`window.triggerSort`) genau denselben
   Fehler ausloest. **Fuer diese Spec gilt daher defensiv:** `supportsSortDefault: false` fuer
   `FaHierarchyKommissionierListen`, `FaHierarchyBeschichtung` und `FaHierarchyVormontageEinzeln`
   (Muster wie `OseonTracking`/`Bom` in `ColumnDefinitions.cs`, die aus demselben Grund — strukturierte
   statt flache Darstellung — `SupportsSortDefault: false` tragen). `FaHierarchyVormontageSummiert`
   ist eine flache, nicht gruppierte Tabelle (ein `<tbody>`, keine Gruppen) und bekommt
   `supportsSortDefault: true` wie `ProductionOrders`. Der Root-Cause-Fix des Sortier-Fallstricks
   selbst ist **nicht** Teil dieser Spec (siehe Offene Rueckfrage 5).

6. **Keine Aenderung der bestehenden Zugriffs-/Toggle-Logik.** Die Access-Filter
   (`RequireLagerProcessingAccessAttribute` bzw. `RequireBeschichtungsauftragAccessAttribute`) und
   Feature-Toggles (`FaHierarchyKommissionierlistenAktiv`, `FaHierarchyBeschichtungAktiv`,
   `FaHierarchyVormontageAktiv` o. ae.) der vier Views bleiben unveraendert; diese Spec fuegt nur
   Anzeige-/Praeferenz-Bloecke hinzu.

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

**Kein Eingriff in `Services/FaHierarchyListBuilder.cs` oder die Controller** — die Spaltenwerte
selbst (welche Zellen gerendert werden) aendern sich nicht, nur ihre Sichtbarkeit/Reihenfolge/Breite
im Browser sowie deren Persistenz je Benutzer.

**ADR-0005-Ergaenzung** (Form laut Offener Rueckfrage 3, hier die inhaltliche Substanz unabhaengig
von der Form): ADR 0005 bekommt einen vierten, verbindlichen Pattern-Bestandteil neben Pagination,
Filterkarte und Server-Spaltenfilter:

> **Spaltenpraeferenzen — Pflicht fuer alle Tabellen-Views mit Server- oder Client-Spaltenfilter.**
> Jede neue Listen-View liefert zusaetzlich: `#view-config`- und `#column-config`-JSON-Bloecke mit
> denselben `key`-Werten wie die `data-col-key`-Attribute, sowie die Einbindung von
> `wwwroot/js/column-preferences.js` **vor** `table-filter.js`. Identifizierende Spalten
> (`locked: true`), selten gebrauchte Spalten (`defaultHidden: true`). Referenzimplementierung:
> `Views/ProductionOrders/Index.cshtml`. Gruppierte/strukturierte Tabellen (mehrere `<tbody>` je
> Gruppe) setzen `supportsSortDefault: false`, solange `table-filter.js` Sortierung nur innerhalb des
> ersten `<tbody>`-Elements ausfuehrt (siehe `fallstricke.md`).

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

## Audit-Feld-Auswirkungen

Keine neuen Entitaeten. `UserViewPreference` schreibt beim Speichern bereits
`_currentUserService.GetDisplayName()`/`GetWindowsUserName()` (bestehender Code, unveraendert durch
diese Spec).

## Akzeptanzkriterien

1. Auf allen vier Views (`FaHierarchyKommissionierListen/Index`, `FaHierarchyBeschichtung/Index`,
   `FaHierarchyVormontage/Index`, `FaHierarchyVormontage/Summiert`) erscheint ein Zahnrad-Symbol zur
   Spaltenkonfiguration (Offcanvas), identisch zum Verhalten in `ProductionOrders/Index`.
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

**Automatisiert (`UserViewPreferencesApiControllerTests.cs`):** vier neue Testfaelle analog zum
bestehenden `FaWorklist`-Regressionstest — `Get`/`Put`/`Delete` fuer jeden der vier neuen `viewKey`
liefern **keinen** `400 BadRequest`.

Nach Abschluss `secondbrain/tests/testszenarien-index.md` nachziehen.

## Deploy

- **Web-App:** ja.
- **Service:** nein.
- **Migration:** nein.
- **Kontext/Reihenfolge (siehe Offene Rueckfrage 2):** Die betroffenen vier Views existieren aktuell
  ausschliesslich im noch nicht gemergten Epic-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`
  (Branch `feature/2026-08-07-ideal-teile-1-5`, Status laut Aufgaben-Notiz `Testbereit`, wartet auf
  Schranke 2/manuelles UAT). Wird diese Spec dort als zusaetzliche Etappe VOR dem Merge umgesetzt,
  entsteht **kein** zusaetzlicher Deploy-Schritt — sie geht im selben Publish/Merge des Epic-Buendels
  mit. Wird stattdessen ein eigener Worktree gewaehlt, kann er erst NACH dem Merge des Epic-Buendels
  sinnvoll arbeiten (die Views muessen auf `main` existieren) und braucht einen eigenen,
  nachgelagerten Publish-Schritt.
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

## Dev-Lauf angehalten (2026-08-12)

`/dev` wurde ausgeloest, aber NICHT umgesetzt — der Gate ist nicht erfuellt:
1. **`status: Entwurf`** (nicht Freigegeben), `freigabe_*` leer — Schranke 1 formal nicht genommen
   (die Kopie in `specs/freigegeben/` ist ein untracktes Duplikat, ebenfalls Entwurf).
2. **Offener, verdikt-tragender Punkt aus „Kritische Pruefung (2026-08-12)":** der sichtbare
   **Sortier-Defekt** (`table-filter.js` sortiert nur das erste `<tbody>`, Klick-Sort unbedingt an
   jeden Header gebunden → die drei neuen gruppierten Listen mis-sortieren) ist nur durch einen
   **noch nicht angelegten** Bug-Task abgesichert, ohne Merge-Gate. Zu entscheiden: Fix in diese Spec
   ziehen ODER Bug-Record anlegen + als Epic-Merge-Vorbedingung verankern.
Inhalt sonst freigabereif. Umsetzung gehoert laut Schwester-Spec (Antwort 2) als Etappe in den
ideal-teile-1-5-Epic (via `/epic-stage`), Reihenfolge: DIESE Spec zuerst, dann fa-struktur.

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
**mindestens zwei `HauptFA`-Gruppen**, Spalte ausblenden → verschwindet in **jeder** Gruppe; analog
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
