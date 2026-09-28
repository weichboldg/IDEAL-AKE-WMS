---
type: spec
title: "AKE-Hotfix: Stuecklisten-Druck HTTP 404.15 (Teil 1) + Lagerbestellung IST nicht vorbefuellen (Teil 2)"
slug: 2026-09-28-ake-hotfix-stueckliste-druck-lagerbestellung-ist-spec
status: Entwurf
created: 2026-09-28
updated: 2026-09-28
source_backlog: "[[2026-09-28-ake-hotfix-stueckliste-druck-lagerbestellung-ist]]"
task: ""
worktree: ""
branch: ""
zielzweig: main
umsetzungsort: "neuer kleiner Worktree/Branch aus main (Slug 2026-09-28-ake-hotfix-stueckliste-druck-lagerbestellung-ist, via scripts/new-worktree.ps1) — NICHT das Buendel"
folge_merge: "Vorwaerts-Merge main -> feature/2026-08-07-ideal-teile-1-5 nach Umsetzung in main"
affected_code:
  - "IdealAkeWms/Controllers/PickingController.cs (main + Buendel, PrintBom-Action)"
  - "IdealAkeWms/Views/Picking/Bom.cshtml (main + Buendel, Druck-Button-JS)"
  - "IdealAkeWms/Views/Picking/PrintBom.cshtml (main + Buendel, unveraendert erwartet)"
  - "IdealAkeWms/Views/WarehousePicking/Details.cshtml (main; Buendel identisch, seit Fork nicht veraendert)"
  - "IdealAkeWms/AppVersion.cs (Web + Service, main)"
  - "IdealAkeWms/Views/Help/Changelog.cshtml (main)"
  - "docs/TESTSZENARIEN.md (main, Kapitel 5 + Kapitel 18)"
  - "secondbrain/tests/testszenarien-index.md"
open_questions:
  - "Leer oder 0 als tatsaechlicher Feldwert? (bereits so im Code — siehe Fund unten)"
  - "Verhalten beim Speichern/Abschliessen mit leerem IST: Pflichtfeld mit sichtbarer Meldung oder 'leer = noch nicht bearbeitet'?"
  - "Bestehende offene Bestellungen unveraendert lassen?"
  - "Komfort-Schaltflaeche 'IST = Bestellt' je Zeile — behalten, wie im Bestand als Sammel-Variante, oder entfernen?"
  - "Placeholder-Text im IST-Feld: entfernen oder auf einen neutralen Hinweis aendern?"
  - "(Nachtrag Koordinator K1/K3) Autosave-Fehler mitbeheben (leeres IST wird heute per collectProgress als 0 gespeichert)? Und Bestandsdaten mit bereits autogespeicherten 0-Werten hinnehmen oder beim Deploy sichten?"
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

Zwei unabhaengige, kleine Korrekturen fuer den AKE-Betrieb (`main`), die beide fachlich UND fuer
den IDEAL-Buendel gelten (Vorwaerts-Merge, siehe eigener Abschnitt unten):

- **Teil 1:** Der Stuecklisten-Druck scheitert bei grossen Stuecklisten mit HTTP 404.15
  (`maxQueryString` der IIS-`RequestFilteringModule` gesprengt), weil die Liste **aller sichtbaren
  Positionsnummern** im GET-Query-String uebergeben wird. Betroffen ist unabhaengig vom Anwender
  jede Stueckliste ab einer bestimmten Groesse — reproduzierbar, kein Rand-Fall.
- **Teil 2:** Das Feld **IST** in der Lagerbestellung soll nicht mehr suggerieren, es sei bereits
  mit der Bestellt-Menge befuellt — ein Anwender, der nicht bewusst zaehlt, bestaetigt sonst
  unbemerkt Differenzen weg.

Beide Teile laufen in **einem** kleinen Worktree/Branch aus `main` (nicht im IDEAL-Buendel), da sie
klein, unabhaengig voneinander und mit demselben Deploy-Ziel (AKE, web) sind. Nach Abnahme in `main`
bringt ein **Vorwaerts-Merge** `main → feature/2026-08-07-ideal-teile-1-5` beide Teile ins Buendel
(siehe „Uebertrag ins Buendel" unten) — **keine zweite Umsetzung**.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Teil 1: `PickingController.PrintBom` (main) von GET-Query auf einen POST-Body fuer die
  Positionsliste umstellen; GET bleibt fuer Alt-Links/Lesezeichen funktionsfaehig (dann immer „alle
  Positionen"); sind ohnehin alle Positionen sichtbar, wird gar keine Liste uebertragen (wie
  `visibleColumns` heute schon).
- Teil 1: Erhebung weiterer GET-Listen-Uebergaben an Druck/PDF-Endpunkte (siehe Tabelle unten).
- Teil 2: Sichtbares Verhalten des IST-Feldes in `WarehousePicking/Details.cshtml` so aendern, dass
  nichts wie eine vorbefuellte Bestellt-Menge aussieht; Entscheidung ueber die bestehende
  „Soll = Ist buchen?"-Sammelfunktion (siehe Fund unten) einholen.
- Versions-Bump (Web + Service `AppVersion.cs`), Anwender-Changelog, `docs/TESTSZENARIEN.md` +
  Index.

**Out-of-Scope**
- `web.config`/`maxQueryString` erhoehen als alleinige Loesung (siehe Backlog, Loesungsweg (a)) —
  verschiebt nur die Grenze, naechste Stueckliste scheitert wieder; `maxUrl`/http.sys warten
  dahinter. Nicht Teil dieser Spec.
- Serverseitiges Nachbilden der Browser-Filterlogik (Loesungsweg (d)) — geht nicht, siehe Backlog
  und [[2026-09-23-suchsyntax-spaltenfilter-erweitern]].
- Migration des IST-Feldes — nicht noetig (siehe Fund unten), daher kein Migrations-Abschnitt mit
  Schema-Aenderung.
- Die eigentliche Umsetzung im Buendel — der Vorwaerts-Merge traegt den fertigen Fix, es wird
  **nicht** parallel im Buendel entwickelt.

## Fachliche Anforderungen

### Teil 1 — Stuecklisten-Druck

1. Ein Druck aus `Picking/Bom` funktioniert unabhaengig von der Anzahl sichtbarer Positionen (auch
   bei mehreren hundert Zeilen ohne aktiven Filter).
2. Sind alle Positionen der Stueckliste sichtbar (kein Filter, kein eingeklappter Baum), wird keine
   Positionsliste uebertragen — der Server druckt dann alle Positionen (wie beim fehlenden
   `visibleColumns` heute schon „alle Spalten").
3. Ein direkter GET-Aufruf der Druck-URL (Lesezeichen, alter Link, manuell eingegeben) bleibt
   funktionsfaehig und druckt in diesem Fall **immer alle Positionen** — eine gefilterte
   GET-Variante wird nicht mehr unterstuetzt (das war exakt die Fehlerquelle).
4. Der Druck-Button in `Bom.cshtml` loest weiterhin ein neues Browser-Tab aus einer echten
   Nutzer-Interaktion aus (kein durch Popup-Blocker unterdruecktes Fenster).

### Teil 2 — Lagerbestellung IST

5. Das IST-Eingabefeld einer neuen bzw. noch nicht gezaehlten Position zeigt **keinen Wert und
   keinen Text, der wie ein bereits eingetragener Wert aussieht**.
6. Ein leeres IST wird beim Abschliessen **nie still** als 0 gebucht — der Anwender wird in jedem
   Fall vorher gefragt (siehe Fund: das passiert im Code bereits ueber den bestehenden
   „Soll = Ist buchen?"-Dialog).
7. Bestehende, bereits laufende Lagerbestellungen (Status Submitted/PartiallyDelivered) behalten
   ihre bisher eingetragenen IST-Werte unveraendert; nur das Aussehen/Verhalten fuer noch nicht
   bearbeitete Positionen aendert sich.

### Wichtiger Befund vor der Umsetzung (Teil 2)

Der Code-Abgleich zeigt: Das ist **kein einfacher „defaultfuellender" Bug**. Es gibt **keine**
Code-Stelle (main **und** Buendel identisch, siehe unten), die `QuantityPicked` beim Laden oder
Anlegen automatisch auf die Bestellt-Menge setzt:

- `WarehouseRequisitionItem.QuantityPicked` ist **bereits nullable** (`decimal?`,
  `IdealAkeWms/Models/WarehouseRequisitionItem.cs:21`) und wird bei der Positions-Erfassung nicht
  gesetzt (`WarehouseRequisitionRepository.cs:136-147`, `AddItemAsync`).
- `Views/WarehousePicking/Details.cshtml:71-86`: das Eingabefeld hat `value="@(pickedInt?.ToString()
  ?? string.Empty)"` — bei `QuantityPicked == null` ist der **Wert** leer. Was wie eine Vorbefuellung
  aussehen kann, ist der **Placeholder** `placeholder="@requestedInt"` (Zeile 85) — ein
  Ghost-Text mit der Bestellt-Menge, kein echter Wert.
- `Close`/`PrintAndClose` (`WarehousePickingController.cs:147-199`, `264-307`) lesen ausschliesslich
  das, was das Formular tatsaechlich mitschickt.
- Es existiert bereits ein Sammel-Mechanismus **genau fuer den Fall „IST leer"**:
  `close-confirm-modal` (`Details.cshtml:183-198`) + `emptyRows()`/`fillSollAsIst()`
  (`Details.cshtml:358-378`) fragt beim Abschliessen **immer**, wenn irgendeine Position leer ist:
  „Bei einigen Positionen wurde keine Ist-Menge eingegeben. Soll = Ist-Menge buchen?" — „Ja" fuellt
  **alle** leeren Zeilen mit der Bestellt-Menge (`fillSollAsIst`), „Nein" bucht sie explizit mit 0.
  Ein leises 0-Buchen ohne Nachfrage gibt es nicht.
- Die urspruengliche Design-Vorlage (`docs/superpowers/specs/2026-04-30-lagerbestellung-aus-produktion-design.md:370`)
  sah tatsaechlich „Ist (Number-Input, default = Bestellt)" vor — das wurde bei der Umsetzung **nicht**
  so gebaut (Wert bleibt leer), nur der Placeholder zitiert die Bestellt-Menge.

**Konsequenz:** Die tatsaechlich vorhandene, dem Backlog am naechsten kommende Stelle ist der
`close-confirm-modal`-„Ja"-Button — das ist inhaltlich bereits die in Rueckfrage 6 befuerchtete
„Komfort-Schaltflaeche IST = Bestellt", nur als **Sammelaktion fuer alle leeren Zeilen** statt pro
Zeile, und **hinter** einer expliziten Rueckfrage statt automatisch. Ob dieser Mechanismus
(a) unveraendert bleibt, (b) der „Ja"-Button entfaellt (harter Zwang zu zaehlen oder Fehlteil zu
markieren), oder (c) nur der irrefuehrende Placeholder verschwindet, ist eine **fachliche**
Entscheidung — siehe Rueckfragen 2/4/5. Es wird **nicht geraten**; die Spec schlaegt (c) als
risikoarme Mindestmassnahme vor und stellt (b) als Alternative zur Wahl.

## Technischer Loesungsentwurf

### Teil 1

**Server (`IdealAkeWms/Controllers/PickingController.cs:474-549`, main):**
`PrintBom` bleibt als GET-Action fuer Alt-Links bestehen, verliert aber den `visiblePositions`-
Parameter (GET druckt ab sofort immer „alle", das entspricht Anforderung 3). Die eigentliche Logik
wandert in eine private Hilfsmethode (z. B. `BuildPrintBomViewModelAsync(id, visiblePositions,
filterInfo, visibleColumns)`), die von zwei Actions aufgerufen wird:

```csharp
[RequirePickingOrVorbauOrFaCompletionAccess]
public async Task<IActionResult> PrintBom(int id, string? filterInfo, string? visibleColumns)
    => await RenderPrintBomAsync(id, visiblePositions: null, filterInfo, visibleColumns);

[HttpPost, ValidateAntiForgeryToken]
[ActionName("PrintBom")]
[RequirePickingOrVorbauOrFaCompletionAccess]
public async Task<IActionResult> PrintBomFiltered(int id,
    [FromForm] string? visiblePositions, [FromForm] string? filterInfo, [FromForm] string? visibleColumns)
    => await RenderPrintBomAsync(id, visiblePositions, filterInfo, visibleColumns);
```

Die bestehende „leer = alle"-Behandlung (`PickingController.cs:500-531`) bleibt unveraendert — sie
funktioniert bereits fuer Anforderung 2, es muss nur der Client keine Liste mehr schicken, wenn
alle Positionen sichtbar sind.

**View/JS (`Views/Picking/Bom.cshtml`, main **und** Buendel — siehe Uebertrag):**
Der Klick-Handler auf `#btnPrintBom` (main `Bom.cshtml:944-980`; Buendel identisch strukturiert,
zusaetzlich mit der Leerzustand-Sperre bei `Bom.cshtml:1220-1279`, Guard `1229-1238`) baut aktuell
eine URL mit `window.open(url, '_blank')`. Neu: **synchron im Click-Handler** (keine Promise/await
davor — Popup-Blocker-Fallstrick) ein `<form method="post" action="...PrintBom/<id>" target="_blank">`
per JS erzeugen, mit `document.body.appendChild`, `__RequestVerificationToken` (Wert aus der
bereits vorhandenen Variable `token`, main `Bom.cshtml:721` / Buendel `Bom.cshtml:788`, dort schon
fuer AJAX-Header verwendet), `visiblePositions`, `filterInfo`, `visibleColumns` als Hidden-Inputs,
`form.submit()`, danach das Form-Element wieder entfernen. Ein `<form target="_blank">`-Submit ist
selbst eine Nutzer-Interaktion und wird von keinem Popup-Blocker unterdrueckt — robuster als
`window.open` nach irgendeiner Zwischen-Aktion.

Anforderung 2 (leer = alle): `visiblePositions` nur ins Formular aufnehmen, wenn
`visiblePositions.length < totalRows` (`totalRows = document.querySelectorAll('#bomTable tbody
tr').length`, dieselbe Selektion, die den Klick-Handler bereits fuer „sichtbar" nutzt). Sind alle
Zeilen sichtbar, faehrt kein `visiblePositions`-Feld im Formular mit — der Server sieht `null` und
druckt alles (bereits vorhandenes Verhalten).

Die Leerzustand-Sperre im Buendel (`if (visiblePositions.length === 0) { ...; return; }`,
Buendel `Bom.cshtml:1231-1238`) bleibt **vor** dem Formular-Bau stehen — unveraendert, nur der Teil
danach (URL/`window.open`) wird durch den Formular-Bau ersetzt.

**Erhebung — weitere Listen-Uebergaben per GET-Query:**

| Zweig | Datei:Zeile | Endpoint | Listentyp | realistische max. Laenge | mitbeheben? |
|---|---|---|---|---|---|
| main+Buendel | `PickingController.cs:474` `Bom.cshtml:944-980`/`1220-1279` | `Picking/PrintBom` | Positionsnummern (`visiblePositions`) | mehrere hundert Positionen × ~6 Zeichen — **sprengt 2048** | **ja — dieser Fix** |
| main+Buendel | `PickingController.cs:474` | `Picking/PrintBom` | Spaltenschluessel (`visibleColumns`) | ≤ 10 feste Spalten-Keys, kurz | nein — unkritisch (Spaltenlisten, siehe Backlog-Abgrenzung) |
| main | `WarehousePickingController.cs:327`, `Details.cshtml:321-338` | `WarehousePicking/Print` | nur `sortCol`/`sortDir` + `colf_*`-Filterwerte je Spalte | wenige Filterwerte, keine Zeilen-/Positionsliste | nein |
| Buendel only | `FaHierarchyKommissionierListenController.cs:189-248` (`Pdf`, `PdfSummiert`) | `/FaHierarchyKommissionierListen/Pdf(Summiert)/{hauptFa}` | `hauptFa` als **Routen-Int**, `target`/`kwVon`/`kwBis`/`visibleColumns` als Query | eine einzelne Id + kurze Filterwerte, keine Liste | nein |
| main | `StorageLocationsController.cs:157` (`PrintLabels`) | `StorageLocations/PrintLabels` | keine Parameter — druckt immer alle aktiven Lagerplaetze | fest, keine Liste | nein |

Ergebnis: `Picking/PrintBom` ist die **einzige** Stelle mit dem beschriebenen Fehlerbild; keine
weitere Fundstelle mit echter Zeilen-/Positionsliste per GET.

### Teil 2

Kein struktureller Umbau (siehe Fund oben). Konkrete Aenderung, unabhaengig vom Ausgang der
Rueckfragen 2/4:

- `Views/WarehousePicking/Details.cshtml:85`: `placeholder="@requestedInt"` entfernen oder durch
  einen erkennbar neutralen Hinweis ersetzen (z. B. `placeholder="zaehlen"` oder leer), damit das
  Feld nicht wie ein bereits eingetragener Wert aussieht. Die Bestellt-Menge bleibt in der
  Nachbarspalte „Bestellt" (Zeile 78) sichtbar — sie geht also nicht verloren, nur die
  Doppelanzeige im IST-Feld entfaellt.
- Abhaengig von Rueckfrage 4/6: entweder der `close-confirm-modal`-Mechanismus
  (`Details.cshtml:183-198`, `358-386`) bleibt unveraendert (Empfehlung, falls die Sammel-Rueckfrage
  als ausreichende Absicherung gilt), oder der „Ja"-Button (Soll = Ist fuer alle leeren Zeilen)
  entfaellt und `close-no` wird zur einzigen Fortsetzungs-Option neben Abbrechen (haertere Variante).
  Diese Spec trifft die Entscheidung **nicht** vorweg.
- Keine Aenderung an `WarehousePickingController.cs`, `WarehouseRequisitionRepository.cs`,
  `WarehousePickingPrintLayout.cs` noetig — alle bestehenden Lese-/Schreibpfade sind bereits
  null-sicher (siehe Folgewirkungs-Tabelle).

**Folgewirkung — Lesestellen von `QuantityPicked` (main **und** Buendel identisch, da diese Dateien
seit dem gemeinsamen Vorfahren `2a34ff46` in keinem der beiden Zweige veraendert wurden):**

| Zweig | Datei:Zeile | Was passiert heute bei `null` | Soll sich aendern? |
|---|---|---|---|
| main+Buendel | `WarehouseRequisitionRepository.cs:205` (`CloseAsync`) | liest aus dem Formular-Dict, Default `0m` falls Key fehlt — Key fehlt nie (jede Zeile hat ein Hidden-`itemIds`) | nein |
| main+Buendel | `WarehouseRequisitionRepository.cs:228` (`DeriveStatus`) | `(i.QuantityPicked ?? 0) >= i.QuantityRequested` — bereits null-sicher | nein |
| main+Buendel | `WarehouseRequisitionRepository.cs:300-302` (`SaveProgressAsync`) | schreibt genau das, was der Client sendet (kann explizit `null` sein) | nein |
| main+Buendel | `WarehouseRequisitionRepository.cs:424-425` (`GetMissingPartsAsync`-Projektion) | `i.QuantityPicked ?? 0m` — null-sicher | nein |
| main+Buendel | `Views/WarehousePicking/Details.cshtml:71-91` | `HasValue`-Check, leer statt Exception | nein (nur Placeholder, siehe oben) |
| main+Buendel | `WarehousePickingPrintLayout.cs:74-76` (`CellText`, Druck/Filter/Sort) | `HasValue`-Check, leerer String statt Exception | nein |
| main+Buendel | `Views/MissingParts/Index.cshtml:115`, `Views/MissingPartsLager/Index.cshtml:99` | liest `MissingPartRow.QuantityPicked` — dort bereits `decimal` (nicht nullable) **nach** der `?? 0m`-Projektion in `WarehouseRequisitionRepository.cs:424` | nein |

**Kein Fund** einer Stelle (in keinem der beiden Zweige), die bei `QuantityPicked == null` eine
Exception wirft oder ungewollt eine `0` bucht, ohne dass der Anwender vorher gefragt wurde. Damit
entfaellt auch die im Backlog befuerchtete Gefahr „Stelle im Buendel bekommt nach dem Merge
ploetzlich `null`" — es gibt dort keine zusaetzliche Lesestelle (identischer Dateibestand seit
Fork).

## Migrations-/SQL-Auswirkungen

**Keine Migration, kein SQL-Skript.** Beleg (main; Buendel identisch da unveraendert seit Fork):

- Modell: `IdealAkeWms/Models/WarehouseRequisitionItem.cs:21` — `public decimal? QuantityPicked`
  (bereits nullable).
- Erstmigration: `IdealAkeWms/Migrations/20260430183954_AddWarehouseRequisitions.cs:72` —
  `QuantityPicked = table.Column<decimal>(type: "decimal(18,4)", nullable: true)`.
- `ApplicationDbContext.cs:1141` — `entity.Property(e => e.QuantityPicked)
  .HasColumnType("decimal(18,4)")`, kein `.IsRequired()`.
- `SQL/00_FreshInstall.sql:1672` — `[QuantityPicked] DECIMAL(18,4) NULL`.

Teil 1 aendert kein Schema. Sollte die Freigabe-Antwort zu Rueckfrage 4/6 wider Erwarten eine neue
Spalte verlangen (z. B. ein separates „wurde gezaehlt"-Flag statt der bestehenden
Null/0-Unterscheidung), gilt fuer die dann noetige Migration die Nummern-Reservierung wie in der
Aufgabenstellung beschrieben: main steht aktuell bei `SQL/88_*`, das Buendel bereits bei `SQL/93_*`
(`AddProductionWorkplaceSageFields`, Stand 2026-09-28) — eine neue main-Migration muesste dann
**`SQL/94_*` oder hoeher** verwenden, nicht `89`, damit die Nummer in beiden Zweigen frei bleibt.
Dieser Fall wird als **nicht erwartet** eingestuft (siehe Migrations-Beleg oben), aber hier
dokumentiert, falls die Freigabe anders entscheidet.

## Audit-Feld-Auswirkungen

Keine neuen Schreibpfade. Die bestehenden Schreibpfade auf `WarehouseRequisitionItem`
(`AuditableEntity`) setzen `ModifiedAt`/`ModifiedBy`/`ModifiedByWindows` bereits korrekt bei jedem
Update (`WarehouseRequisitionRepository.cs:212-214` in `CloseAsync`, `253-255` in `SaveNotesAsync`,
weitere analog in `SaveProgressAsync`) — unveraendert durch diese Spec. `PrintBom` (Teil 1) ist eine
reine Lese-/Druck-Action ohne Entity-Schreibzugriff, daher keine Audit-Feld-Beruehrung.

## Akzeptanzkriterien

1. Eine Stueckliste mit ≥ 300 Positionen (kein Filter aktiv) laesst sich ueber den Druck-Button
   ohne HTTP 404.15 drucken; das Druck-Fenster zeigt alle Positionen.
2. Bei aktivem Filter/eingeklapptem Baum druckt der Button weiterhin nur die aktuell sichtbaren
   Positionen (Regressionstest zu TS-5.9).
3. Ein direkter GET-Aufruf `/Picking/PrintBom/{id}` (ohne `visiblePositions`) liefert weiterhin die
   vollstaendige Stueckliste (kein 404, kein leerer Druck).
4. Der Druck-Button oeffnet in Chrome, Firefox und Edge (Standard-Popup-Blocker-Einstellung) ein
   neues Tab ohne Blockierung.
5. Im Buendel bleibt die Leerzustand-Sperre erhalten: „Alle Ziele ohne Treffer" zeigt weiterhin den
   Hinweis „Keine sichtbaren Positionen — nichts zu drucken" statt eines leeren Druck-Tabs.
6. Eine neu erfasste, noch nicht bearbeitete Lagerbestellungs-Position zeigt im IST-Feld weder einen
   Wert noch einen Placeholder, der wie ein bereits eingetragener Wert aussieht.
7. Ein Abschliessen mit mindestens einer leeren IST-Position zeigt weiterhin (oder je nach
   Freigabe-Antwort: zeigt zwingend, ohne Sammel-Alternative) eine explizite Rueckfrage, bevor
   irgendetwas gebucht wird — nie eine stille 0-Buchung.
8. Bestehende Lagerbestellungen mit bereits eingetragenen IST-Werten (Status Submitted oder
   PartiallyDelivered) zeigen diese Werte nach dem Deploy unveraendert an.
9. `dotnet build` und `dotnet test` sind gruen; kein neuer EF-Migrations-Eintrag wird erzeugt.
10. Version in `IdealAkeWms/AppVersion.cs` **und** `IDEALAKEWMSService/AppVersion.cs` ist erhoeht,
    `Views/Help/Changelog.cshtml` hat einen neuen Eintrag fuer beide Teile.
11. `docs/TESTSZENARIEN.md` enthaelt TS-5.11 (Teil 1) und TS-18.10 (Teil 2);
    `secondbrain/tests/testszenarien-index.md` ist nachgezogen.

## Test-Szenarien

Neue Szenarien als Ergaenzung der bestehenden Kapitel (Fachbereichs-Nummerierung, wie beim
Vorbild TS-2.22 ff. fuer aehnliche Hotfix-Buendel):

**TS-5.11 — Stueckliste mit vielen Positionen drucken (Regression zu TS-5.3/TS-5.9)**
- Vorbedingung: FA mit einer Stueckliste ≥ 300 Positionen (z. B. per Testdaten oder ein bekannt
  grosses Baugruppen-FA), Picking- oder Vorbau-Zugriff.
- Schritte: `Picking/Bom` oeffnen, keinen Filter setzen, auf „Stueckliste drucken" klicken.
- Erwartet: neues Tab mit der vollstaendigen Stueckliste, kein HTTP 404.15, keine Fehlermeldung.
- Negativfall: einen Spaltenfilter setzen, der die Positionen auf wenige reduziert, erneut drucken
  → nur die sichtbaren Positionen erscheinen (wie bisher, TS-5.9-Regression).
- Negativfall 2: `/Picking/PrintBom/{id}` direkt in die Adresszeile eingeben (GET, keine Parameter)
  → vollstaendiger Druck ohne Fehler.

**TS-18.10 — Lagerbestellung: IST-Feld ohne Vorbefuellung**
- Vorbedingung: offene Lagerbestellung im Status „Abgeschickt" mit mindestens zwei Positionen, von
  denen keine bisher bearbeitet wurde.
- Schritte: `WarehousePicking/Details` oeffnen.
- Erwartet: IST-Feld ist leer, kein sichtbarer Wert, der wie „Bestellt" aussieht.
- Schritt „Speichern + Abschliessen" ohne eine IST-Menge einzutragen.
- Erwartet: die bestehende Rueckfrage „Soll = Ist-Menge buchen?" erscheint (bzw. das laut
  Freigabe-Antwort geaenderte Verhalten); ohne explizite Bestaetigung wird nichts gebucht.
- Negativfall: eine bereits laufende Bestellung mit vorhandenen IST-Werten (aus der Zeit vor dem
  Fix) oeffnen → Werte erscheinen unveraendert.

## Deploy

- **Web-App:** ja (AKE) — `PickingController`, `Bom.cshtml`, `Details.cshtml`, `AppVersion.cs`,
  `Changelog.cshtml`.
- **Service:** nein (nur Versions-Konstante aus Konsistenzgruenden, kein Verhaltensaenderung).
- **Migration:** nein.
- **Publish-Befehle** (im Worktree, nach bestandenem Test, vor dem Merge):
  ```
  dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
  dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService
  ```
- Reihenfolge: Web-Publish auf AKE-IIS, danach manueller Test (Schranke 2), danach Merge in `main`.
  Der Service-Publish ist nur der Versionsgleichstand wegen noetig, keine funktionale Aenderung.
- Im Buendel wirkt der Fix erst nach dem Vorwaerts-Merge **und** dessen eigenem, spaeteren Deploy.

## Uebertrag ins Buendel (Vorwaerts-Merge)

**Ablauf:** Nach Abnahme + Merge in `main`: `git merge main` im Worktree
`.claude/worktrees/2026-08-07-ideal-teile-1-5` (Branch `feature/2026-08-07-ideal-teile-1-5`).
**Nicht mergen, waehrend dort ein `/dev`-Lauf aktiv ist** — dessen Ende abwarten.

**Gemessene Ausgangslage (2026-09-28, read-only ermittelt):**
- Merge-Base `main`/Buendel: `2a34ff46`. `main` liegt 170 Commits voraus (fast ausschliesslich
  `secondbrain/`), das Buendel 164 Commits.
- `git merge-tree --write-tree Buendel main` liefert **aktuell** (vor diesem Hotfix) exit 0 = **kein
  Konflikt** — `main` hat seit dem Merge-Base ausserhalb von `secondbrain/` nur
  `.claude/commands/dev.md`, `.claude/commands/epic-stage.md` und `CLAUDE.md` veraendert. Jeder
  Konflikt beim spaeteren Vorwaerts-Merge entsteht also **durch diesen Hotfix selbst**, nicht durch
  vorbestehende Divergenz.
- Das Buendel hat seit dem Merge-Base an den fuer diesen Hotfix relevanten Dateien geaendert:
  `PickingController.cs` (129 Zeilen), `Views/Picking/Bom.cshtml` (321 Zeilen),
  `Views/Picking/PrintBom.cshtml` (11 Zeilen), `AppVersion.cs` (4 Zeilen, beide Projekte),
  `Views/Help/Changelog.cshtml` (+466 Zeilen), `SQL/00_FreshInstall.sql` (105 Zeilen),
  `docs/TESTSZENARIEN.md` (+2598 Zeilen), `IdealAkeWms/Migrations/ApplicationDbContextModelSnapshot.cs`
  (+226 Zeilen).
- **Vorhergesagte Konflikte:** `Bom.cshtml` (Druck-Handler liegt an unterschiedlichen Zeilen,
  Buendel hat zusaetzlich die Leerzustand-Sperre und das Kommissionierziel-Feature drumherum),
  `PickingController.cs` (falls die `PrintBom`-Umgebung im Diff ueberlappt), ggf.
  `PrintBom.cshtml`, `AppVersion.cs` (Web **und** Service — beide Zweige bumpen dieselbe Zeile),
  `Changelog.cshtml` (beide fuegen am selben Einstiegspunkt oben einen neuen Card-Block ein),
  `docs/TESTSZENARIEN.md` (die „Stand:"-Kopfzeile bzw. das Datei-Ende) und — nur falls dieser
  Hotfix doch eine Migration braeuchte — `ApplicationDbContextModelSnapshot.cs` +
  `SQL/00_FreshInstall.sql`.
- `WarehousePicking`-Dateien (`Views/WarehousePicking/*`, `WarehousePickingController.cs`,
  `Services/WarehousePicking*`, `Models/WarehousePicking*`) hat das Buendel seit dem Merge-Base
  **nicht** veraendert (0 Zeilen) — Teil 2 ist dort voraussichtlich **konfliktfrei**. Das ersetzt
  nicht die oben durchgefuehrte Folgewirkungs-Pruefung ausserhalb dieser Dateien (Sage-Buchung, BDE,
  Druck, Services) — dort wurde ebenfalls kein zusaetzlicher Buendel-Fund gemacht (siehe Tabelle
  oben, identischer Dateibestand).
- Migrations-Ordner ist `IdealAkeWms/Migrations/` (nicht `Data/Migrations`).

**Konfliktaufloesung (Vorgaben fuer den Merge-Lauf):**
- `Bom.cshtml`/`PrintBom`: der neue POST-Ausloeser (Formular-Bau statt `window.open(url)`) muss
  **nach** der bestehenden Leerzustand-Sperre des Buendels (`if (visiblePositions.length === 0)
  {...; return;}`, Buendel `Bom.cshtml:1231-1238`) eingehaengt werden — die Sperre bleibt
  unveraendert bestehen, nur der Druck-Aufruf danach wird ersetzt.
- `AppVersion.cs`/`Changelog.cshtml`: **beide** Aenderungen behalten (main-Eintrag **und**
  Buendel-Eintrag), die Versionsnummer nach dem Merge auf einen Stand **hoeher als das Buendel vor
  dem Merge** (aktuell `1.46.0`) setzen — **nicht** auf die main-Hotfix-Nummer zurückfallen, sonst
  waere das ein Ruckschritt im Buendel.
- `docs/TESTSZENARIEN.md`: beide Kapitel-Ergaenzungen behalten; die neuen main-Eintraege TS-5.11 /
  TS-18.10 kollidieren nicht mit den Buendel-eigenen `TS-58`…`TS-80`+ (unterschiedliche
  Nummerierungs-Schemata: main nutzt „Kapitel N, TS-N.M", das Buendel ab „Kapitel 57" flache
  `TS-NN`-Kapitel — beide Schemata bestehen im selben Dokument nebeneinander, siehe Datei-Realitaet).
- Falls dieser Hotfix doch eine Migration erzeugt (siehe Rueckfrage-Fall oben): Snapshot- und
  `SQL/00_FreshInstall.sql`-Konflikt so aufloesen, dass **beide** Aenderungen (main + Buendel)
  erhalten bleiben, danach `dotnet ef migrations list` und Build pruefen.
- **Pflicht-Deliverable des Merge-Laufs:** die tatsaechlich aufgetretenen Konfliktdateien
  protokollieren (Abgleich mit der obigen Vorhersage) — in der Aufgaben-Notiz zu diesem Merge
  festhalten, nicht nur im Kopf des Ausfuehrenden.

**Akzeptanzkriterien nach dem Merge:**
- A1. Alle Buendel-Tests sind nach dem Merge gruen.
- A2. Eine grosse Stueckliste (≥ 300 Positionen) druckt im Buendel ohne HTTP 404.15.
- A3. Die Buendel-Leerzustand-Sperre („Alle Ziele ohne Treffer") funktioniert unveraendert.
- A4. Die Lagerbestellung im Buendel zeigt kein vorbefuelltes/irrefuehrendes IST-Feld mehr.
- A5. Kein IDEAL-spezifischer Pfad (FA-Hierarchie, Sub-FA, Sage-Ruecklauf) behandelt ein leeres IST
  falsch — erneute Kurzpruefung der Folgewirkungs-Tabelle nach dem Merge, falls das Buendel bis
  dahin neue Lesestellen hinzugefuegt hat.

## Offene Rueckfragen

1. Leer oder 0 als tatsaechlicher Feldwert? Der Code liefert heute bereits **leer** als Wert (nur
   der Placeholder zeigt die Bestellt-Menge als Ghost-Text) — Empfehlung: dabei bleiben, nur den
   Placeholder anpassen (siehe Rueckfrage 5).
2. Verhalten beim Speichern/Abschliessen mit leerem IST: soll die bestehende
   „Soll = Ist buchen?"-Sammel-Rueckfrage (`close-confirm-modal`) unveraendert bleiben, oder soll
   ein leeres IST zu einem **Pflichtfeld mit blockierender Meldung** werden (kein Abschliessen ohne
   Eintrag oder explizite Fehlteil-Markierung je Zeile)?
3. Bestehende offene Lagerbestellungen (Status Submitted/PartiallyDelivered) unveraendert lassen?
   Empfehlung: ja — es gibt ohnehin keine Datenmigration, nur eine Anzeige-/Verhaltensaenderung fuer
   noch nicht bearbeitete Positionen.
4. Komfort-Schaltflaeche „IST = Bestellt": im Code existiert bereits eine **Sammel**-Variante davon
   (`close-confirm-modal`-„Ja"-Button, fuellt alle leeren Zeilen mit der Bestellt-Menge). Bleibt
   dieser Mechanismus bestehen, wird er entfernt, oder soll er durch eine Pro-Zeile-Variante ersetzt
   werden? Vorsicht laut Backlog: jede Variante davon bringt das unbestaetigte Bestaetigen nur einen
   Klick weiter weg zurueck.
5. Placeholder-Text im IST-Feld (`Details.cshtml:85`, zeigt aktuell die Bestellt-Menge als
   Ghost-Text): entfernen, durch einen neutralen Hinweis ersetzen (z. B. „zaehlen"), oder
   unveraendert lassen (falls Rueckfrage 2/4 den Mechanismus ohnehin haerter macht und der
   Placeholder dann keine praktische Rolle mehr spielt)?

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

1. →
2. →
3. →
4. →
5. →

## Nachtrag Koordinator (2026-09-28) — Korrektur zu Teil 2 und zur Version

> Nach dem Spec-Agent-Lauf am Code (main, `Views/WarehousePicking/Details.cshtml` +
> `WarehousePickingController.cs`) nachgemessen. **Zwei Aussagen oben sind falsch** und hiermit
> korrigiert; der Originaltext bleibt zur Nachvollziehbarkeit stehen. Massgeblich fuer Teil 2 ist
> dieser Nachtrag. Die Dateien sind im Buendel seit `2a34ff46` unveraendert — der Befund gilt fuer
> **beide** Zweige.

### K1 — Ein leeres IST wird heute bereits STILL als 0 gespeichert (Autosave)

Oben steht: „Ein leises 0-Buchen ohne Nachfrage gibt es nicht." — **falsch.**

- `collectProgress()` (`Details.cshtml` ~Z.292-303) sendet leere IST-Felder **als `'0'`**:
  `.map(i => (i.value && i.value.trim()) || '0')`. Kommentar dort: sonst ueberspringe der Model-Binder
  fuer `int[]` leere Strings und die Indizes gegenueber `itemIds` verschoeben sich.
- `saveProgress()` (~Z.305-318) schickt das per `fetch` an `SaveProgress` — das ist der **Autosave**,
  er laeuft ohne Zutun des Anwenders, sobald irgendetwas im Formular geaendert wurde (`dirty`).
- Serverseitig nimmt `SaveProgress` **bereits `int?[]`** (`WarehousePickingController.cs` ~Z.222-234)
  und wuerde `null` korrekt speichern — aber der Client schickt nie `null`, sondern `0`.

**Folge heute:** Traegt ein Lagermitarbeiter in Zeile 3 eine Menge ein, speichert der Autosave fuer
**alle** noch leeren Zeilen `QuantityPicked = 0` in die DB. Beim Neuladen steht dort `0`, nicht leer.
Die „leer vs. 0"-Unterscheidung, auf die Rueckfrage 1 zielt, geht damit schon beim ersten Autosave
verloren. `emptyRows()` behandelt `0` und leer gleich (`parseInt(...) === 0`) — deshalb faellt es in der
Close-Rueckfrage nicht auf, wohl aber in der DB.

### K2 — „Nein" im Abschluss-Dialog bucht leere Zeilen als 0, ohne dass „0" gewaehlt wurde

- `Close`/`PrintAndClose` binden **`int[] quantitiesPicked`** (Controller ~Z.147/264). Vor jedem Submit
  setzt `normalizeEmptyQuantitiesToZero()` (~Z.401-410) jedes leere Feld auf `'0'` — auf **allen**
  Wegen: ohne Dialog, bei „Ja" (nach `fillSollAsIst`, fuer Zeilen mit Fehlteil-Status) und bei „Nein".
- Die Dialogfrage lautet „Soll = Ist-Menge buchen?". **„Nein" heisst dort nicht erkennbar „0 buchen"** —
  man kann es auch als „nein, ich zaehle noch" lesen. Gebucht wird trotzdem 0.
- Die Invariante des Backlogs („Ein leeres IST darf nie still als 0 gebucht werden") ist damit **heute
  schon verletzt** — nicht erst durch diesen Hotfix.

### K3 — Konsequenz fuer den Umfang von Teil 2

Die Wurzel ist die **Parallel-Array-Bindung** (`itemIds[]` + `quantitiesPicked[]` als `int[]`), die leer
nicht transportieren kann. Solange sie bleibt, ist „leer" am Server nicht darstellbar — egal, was die
View anzeigt. Wer Rueckfrage 2 mit „leer = noch nicht bearbeitet" beantwortet, muss deshalb:
- im Client leere Felder als **leeren String** senden (nicht `'0'`) — in `collectProgress` **und**
  `normalizeEmptyQuantitiesToZero` (diese Funktion entfaellt bzw. wird ersetzt);
- `Close`/`PrintAndClose` auf **`int?[]`** umstellen (wie `SaveProgress` schon ist) — ASP.NET bindet bei
  `Nullable<int>[]` einen leeren String als `null` **an seinem Index** (kein Verschieben). **Im Dev-Lauf
  per Test belegen**, nicht annehmen (Controller-Test mit `itemIds=[1,2,3]`, `quantitiesPicked=["5","","7"]`
  → Zeile 2 = `null`);
- im Repository (`CloseAsync`, `Default 0m falls Key fehlt`, `WarehouseRequisitionRepository.cs` ~Z.205)
  festlegen, was mit `null` beim **Abschliessen** passiert — genau Rueckfrage 2 (Pflichtfeld/Meldung vs.
  „nicht bearbeitet"). Eine stille `?? 0m` ist ausgeschlossen.

Wird Rueckfrage 2 dagegen mit „Pflichtfeld" beantwortet, reicht es, das Abschliessen bei leeren Zeilen
**ohne** Fehlteil-Status zu blockieren (sichtbare Meldung) — aber der **Autosave-Fehler K1** muss
trotzdem behoben werden, sonst gibt es nach dem ersten Autosave nie wieder leere Felder, und die
Pflichtpruefung greift ins Leere.

**Die Folgewirkungs-Tabelle oben („alle Lesestellen null-sicher") ist deshalb unvollstaendig:** Sie
bewertet die Lesestellen korrekt fuer den Fall `null` in der DB — aber `null` kommt heute gar nicht an.
Die entscheidenden Stellen sind die **Schreibwege** (`collectProgress`, `normalizeEmptyQuantitiesToZero`,
`Close`/`PrintAndClose`-Bindung). Der Dev-Lauf ergaenzt die Tabelle um diese drei Zeilen.

**Neue offene Rueckfrage 6 (durch den Code erzwungen):** Der Autosave-Fehler K1 besteht unabhaengig
von der Vorbefuellungs-Frage. Wird er **in diesem Hotfix** mitbehoben (Empfehlung: ja — ohne ihn ist
jede Antwort auf Rueckfrage 1/2 wirkungslos), und was gilt fuer Bestandsdaten: Offene Bestellungen, bei
denen der Autosave schon `0` fuer eigentlich ungezaehlte Zeilen gespeichert hat, sind von echten
Null-Mengen **nicht mehr unterscheidbar** — hinnehmen (Empfehlung, keine Datenkorrektur moeglich) oder
beim Deploy offene Bestellungen einmal sichten?

### K4 — Versionsnummer: 1.31.0 kollidiert mit dem Buendel

Der Vorschlag oben (main `1.30.0` → Hotfix `1.31.0`) erzeugt nach dem Vorwaerts-Merge **zwei
verschiedene v1.31.0**: Im Buendel ist v1.31.0 bereits „IDEAL Teile 1-5" (Anwender-Changelog und
Brain-Changelog `2026-08-10-v1-31-0-ideal-teile-1-5`). **Vorschlag:** Der Hotfix ist ein Patch →
**`1.30.1`** (in keinem Zweig vergeben, sortiert korrekt vor dem Buendel). Nach dem Vorwaerts-Merge gilt
weiter die Regel oben: Buendel-Version **hoeher** als `1.46.0`, nicht zurueckfallen.
