---
type: spec
title: "AKE-Hotfix: Stuecklisten-Druck HTTP 404.15 (Teil 1) + Lagerbestellung IST nicht vorbefuellen (Teil 2)"
slug: 2026-09-28-ake-hotfix-stueckliste-druck-lagerbestellung-ist-spec
status: Entwurf
created: 2026-09-28
updated: 2026-09-29
source_backlog: "[[2026-09-28-ake-hotfix-stueckliste-druck-lagerbestellung-ist]]"
task: ""
worktree: ""
branch: ""
zielzweig: main
umsetzungsort: "neuer kleiner Worktree/Branch aus main (Slug 2026-09-28-ake-hotfix-stueckliste-druck-lagerbestellung-ist, via scripts/new-worktree.ps1) — NICHT das Buendel"
folge_merge: "Vorwaerts-Merge main -> feature/2026-08-07-ideal-teile-1-5 nach Umsetzung in main"
affected_code:
  - "IdealAkeWms/Controllers/PickingController.cs (main + Buendel, PrintBom-Action) — Teil 1"
  - "IdealAkeWms/Views/Picking/Bom.cshtml (main + Buendel, Druck-Button-JS) — Teil 1"
  - "IdealAkeWms/Views/Picking/PrintBom.cshtml (main + Buendel, unveraendert erwartet) — Teil 1"
  - "IdealAkeWms/Views/WarehousePicking/Details.cshtml (Teil 2, massgeblich: Placeholder Z.85 entfernt, collectProgress Z.292-303 sendet '' statt '0', close-confirm-modal Z.183-198 + emptyRows/fillSollAsIst Z.358-378 + normalizeEmptyQuantitiesToZero Z.402-410 entfallen, neue getIncompleteRows/markIncomplete, performPrintAndClose-Fehlerbehandlung erweitert)"
  - "IdealAkeWms/Controllers/WarehousePickingController.cs (Teil 2: Close Z.146-199 + PrintAndClose Z.263-307 auf int?[] quantitiesPicked umgestellt, werten die Pflichtpruefungs-Rueckgabe von CloseAsync aus)"
  - "IdealAkeWms/Data/Repositories/IWarehouseRequisitionRepository.cs (Teil 2: CloseAsync-Signatur Z.31-36 — itemQuantitiesPicked auf decimal?, Rueckgabetyp auf IReadOnlyList<(int Position, string ArticleNumber)>)"
  - "IdealAkeWms/Data/Repositories/WarehouseRequisitionRepository.cs (Teil 2: CloseAsync Z.192-223 — decimal?, Pflichtpruefung vor SaveChangesAsync, kein stilles 0-Fallback mehr)"
  - "IdealAkeWms.Tests/Repositories/WarehouseRequisitionRepositoryTests.cs (Teil 2: ca. 28 Stellen `Dictionary<int, decimal>` -> `Dictionary<int, decimal?>`, neue Tests fuer Pflichtpruefung)"
  - "IdealAkeWms.Tests/Controllers/WarehousePickingControllerTests.cs (Teil 2: `int[]`- auf `int?[]`-Literale bei Close/PrintAndClose-Aufrufen, neue Tests fuer Negativpruefung + Pflichtpruefung)"
  - "neuer Bindungstest fuer echte ASP.NET-Modellbindung von `int?[]` aus Formulardaten (Teil 2, Pflicht — siehe Technischer Loesungsentwurf Teil 2, Abschnitt F, Ort z. B. IdealAkeWms.Tests/ModelBinding/NullableIntArrayBindingTests.cs)"
  - "SQL/94_ResetAutosaveZeroQuantityPickedSubmitted.sql (neu, Teil 2, einmaliges Datenskript ohne Schema-Aenderung — siehe Migrations-/SQL-Auswirkungen)"
  - "IdealAkeWms/AppVersion.cs (Web + Service, main) — Version 1.30.1"
  - "IdealAkeWms/Views/Help/Changelog.cshtml (main)"
  - "docs/TESTSZENARIEN.md (main, Kapitel 5 + Kapitel 18)"
  - "secondbrain/tests/testszenarien-index.md"
open_questions:
  - "KONFLIKT 1 (Nachtrag Spec-Agent 2026-09-29): Einmal-Datenskript ohne Schema-Aenderung hat keine Hauspraxis (ADR 0004 deckt nur Migrationen). Vorschlag: SQL/94 als Nummer, aber OHNE __EFMigrationsHistory- und OHNE FreshInstall-Eintrag. Bestaetigen?"
  - "KONFLIKT 2 (Nachtrag Spec-Agent 2026-09-29): Reset-Skript mit Zusatzbedingung ShortageStatus = None (0 an Fehlteil-Zeilen bleibt) oder woertlich nach Antwort 6b fuer ALLE Zeilen im Status Submitted? Hinweis Koordinator: Die Zusatzbedingung stammt aus dem Auftrag des Koordinators an den Spec-Agenten, NICHT aus Antwort 6b. Beide Varianten sind nach dem neuen Pflichtpruef-Grundsatz unkritisch (Fehlteil-Zeile gilt auch mit NULL als bestaetigt) — Unterschied ist nur der gespeicherte Wert."
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

> [!info] Nachgezogen 2026-09-29
> Diese Spec wurde nach der Kritischen Pruefung (Freigabe-Antworten 1-6 + Bestaetigung des
> Menschen + Nachtrag Koordinator K1-K4, alle unten **unveraendert** stehend) in Rumpf,
> `affected_code`, Akzeptanzkriterien, Test-Szenarien und Deploy nachgezogen. Teil 1 blieb
> inhaltlich unveraendert. Teil 2 wurde komplett neu entworfen (Pflichtfeld statt Sammel-Rueckfrage,
> Autosave-Fix, Reset-Skript) — die urspruengliche, durch den Code widerlegte Erstfassung von Teil 2
> steht als Protokoll weiter unten, sichtbar als ueberholt markiert.

## Ziel / Nutzen (das Warum)

Zwei unabhaengige, kleine Korrekturen fuer den AKE-Betrieb (`main`), die beide fachlich UND fuer
den IDEAL-Buendel gelten (Vorwaerts-Merge, siehe eigener Abschnitt unten):

- **Teil 1:** Der Stuecklisten-Druck scheitert bei grossen Stuecklisten mit HTTP 404.15
  (`maxQueryString` der IIS-`RequestFilteringModule` gesprengt), weil die Liste **aller sichtbaren
  Positionsnummern** im GET-Query-String uebergeben wird. Betroffen ist unabhaengig vom Anwender
  jede Stueckliste ab einer bestimmten Groesse — reproduzierbar, kein Rand-Fall.
- **Teil 2:** Nicht Geliefertes muss **bestaetigt** werden — durch einen eingetippten IST-Wert
  (auch `0`) oder eine explizite Fehlteil-Markierung. Bisher passiert genau das Gegenteil: Ein
  leeres IST-Feld wird schon beim ersten Autosave still als `0` in die Datenbank geschrieben
  (Nachtrag Koordinator K1), und der Abschluss-Dialog „Soll = Ist buchen?" bucht bei „Nein" ebenso
  0, ohne dass „0" je bewusst gewaehlt wurde (K2). Beides widerspricht dem Grundsatz, den der
  Mensch am 2026-09-28 bestaetigt hat: *„Wenn nicht geliefert, muss das bestaetigt werden — manuell
  eintippen bzw. das Fehlteil gesetzt."*

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
- Teil 2 (massgeblich, siehe Freigabe-Antworten + Nachtrag Koordinator): Autosave-Fix (K1: leeres
  IST-Feld sendet `''` statt `'0'`), serverseitige **Pflichtpruefung** beim Abschliessen (IST-Wert
  oder Fehlteil-Markierung je Zeile — sonst blockiert, fuer Close UND PrintAndClose), Entfall der
  Sammel-Rueckfrage „Soll = Ist buchen?" (`close-confirm-modal`, `fillSollAsIst`,
  `normalizeEmptyQuantitiesToZero`), Entfall des Bestellt-Menge-Placeholders im IST-Feld, sowie ein
  einmaliges Reset-Skript fuer bereits vom Autosave-Fehler betroffene, noch offene (Submitted)
  Bestellungen.
- Versions-Bump auf `1.30.1` (Web + Service `AppVersion.cs`), Anwender-Changelog,
  `docs/TESTSZENARIEN.md` + Index.

**Out-of-Scope**
- `web.config`/`maxQueryString` erhoehen als alleinige Loesung (siehe Backlog, Loesungsweg (a)) —
  verschiebt nur die Grenze, naechste Stueckliste scheitert wieder; `maxUrl`/http.sys warten
  dahinter. Nicht Teil dieser Spec.
- Serverseitiges Nachbilden der Browser-Filterlogik (Loesungsweg (d)) — geht nicht, siehe Backlog
  und [[2026-09-23-suchsyntax-spaltenfilter-erweitern]].
- Eine Migration des IST-Feldes — nicht noetig, das Feld ist bereits `decimal?` (siehe Migrations-
  /SQL-Auswirkungen); die Aenderung betrifft nur Bindung, Pruefung und einmalige Datenkorrektur.
- Eine Pro-Zeile-„IST = Bestellt"-Schaltflaeche — laut Freigabe-Antwort 4 ausdruecklich **nicht**
  gewuenscht.
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

### Teil 2 — Lagerbestellung IST (massgeblich, siehe Freigabe-Antworten + Nachtrag Koordinator K1-K4)

5. Das IST-Eingabefeld einer neuen bzw. noch nicht gezaehlten Position zeigt **keinen Wert und
   keinen Placeholder**, der wie ein bereits eingetragener Wert aussieht (Freigabe-Antwort 5: der
   Bestellt-Menge-Placeholder entfaellt ersatzlos, die Bestellt-Menge bleibt in ihrer eigenen
   Spalte sichtbar).
6. Der Autosave (`SaveProgress`) speichert ein leeres IST-Feld als tatsaechliches **`NULL`** in der
   Datenbank, nicht mehr als `0` (Korrektur des Autosave-Fehlers K1 — Freigabe-Antwort 6a: Pflicht,
   sonst bleiben Antworten 1/2 wirkungslos).
7. **Pflichtpruefung beim Abschliessen** (Freigabe-Antwort 2, bestaetigt durch den Menschen): jede
   Position braucht entweder einen eingetragenen IST-Wert (eine getippte `0` zaehlt als bewusste
   Bestaetigung) oder eine explizite Fehlteil-Markierung. Fehlt beides, wird das Abschliessen — bei
   „Speichern + Abschliessen" UND bei „Drucken und Abschliessen" — mit einer Meldung blockiert, die
   die betroffenen Positionen (Position + Artikelnummer) nennt. Diese Pruefung greift **serverseitig**,
   unabhaengig davon, ob JavaScript lief oder ein Formular direkt gepostet wurde.
8. Die bisherige Sammel-Rueckfrage „Soll = Ist-Menge buchen?" (`close-confirm-modal`) entfaellt
   ersatzlos (Freigabe-Antwort 4); es gibt **keine** Schaltflaeche mehr, die offene Positionen
   automatisch mit der Bestellt-Menge befuellt — weder als Sammel- noch als Pro-Zeile-Variante.
9. Bereits laufende Lagerbestellungen mit Status **PartiallyDelivered** oder bereits **abgeschlossene/
   stornierte** Bestellungen behalten ihre eingetragenen IST-Werte unveraendert. Fuer Status
   **Submitted** werden vom Autosave-Fehler K1 geschriebene, von echten getippten Nullen nicht mehr
   unterscheidbare `0`-Werte **einmalig per Skript** auf `NULL` zurueckgesetzt (Freigabe-Antwort 6b),
   siehe Migrations-/SQL-Auswirkungen — mit einer dort gekennzeichneten offenen Detailfrage zur
   Fehlteil-Ausnahme.

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

### Teil 2 (massgeblich, 2026-09-29)

> [!warning] Ersetzt die urspruengliche Erstfassung
> Die urspruengliche Erstfassung von Teil 2 (inkl. „Wichtiger Befund", altem Loesungsentwurf und
> alter Folgewirkungs-Tabelle) enthielt zwei durch den Code widerlegte Kernaussagen (Nachtrag
> Koordinator K1/K2). Sie steht unveraendert im Abschnitt **„Protokoll: ueberholte Erstfassung Teil 2
> (2026-09-28)"** weiter unten, dort klar als ueberholt gekennzeichnet. Massgeblich ist ausschliesslich
> dieser Abschnitt.

Kein struktureller Umbau des Datenmodells (`QuantityPicked` ist bereits `decimal?`, siehe
Migrations-/SQL-Auswirkungen). Die Aenderung betrifft drei Schreibwege plus die Client-Oberflaeche.

#### A) Autosave-Fix (K1) — `Details.cshtml:292-303`, `collectProgress()`

Heute: `.map(i => (i.value && i.value.trim()) || '0')` — ein leeres Feld wird als `'0'` gesendet,
mit dem (falschen) Kommentar, das sei noetig, weil der Model-Binder fuer `int[]` leere Strings
skippt und die Indizes gegenueber `itemIds` verschieben wuerden.

Neu:
```js
const quantitiesPicked = Array.from(form.querySelectorAll('input[name="quantitiesPicked"]'))
    .map(i => i.value.trim());
```
`SaveProgress` (`WarehousePickingController.cs:221-234`) bindet bereits `[FromForm] int?[]?
quantitiesPicked` und schreibt via `SaveProgressAsync` (`WarehouseRequisitionRepository.cs:274-...`)
exakt das, was ankommt — beide sind **unveraendert korrekt**, sobald der Client kein `'0'` mehr
untermogelt. Der alte Kommentar wird durch einen Verweis auf den Bindungstest (Abschnitt F) ersetzt.

#### B) Serverseitige Pflichtpruefung beim Abschliessen — Repository + Controller

**`IWarehouseRequisitionRepository.cs:31-36`** — Signatur von `CloseAsync` aendert sich zweifach:

```csharp
Task<IReadOnlyList<(int Position, string ArticleNumber)>> CloseAsync(int id,
    IReadOnlyDictionary<int, decimal?> itemQuantitiesPicked,
    IReadOnlyDictionary<int, string?> itemNotes,
    IReadOnlyDictionary<int, string?> itemNotesEinkauf,
    IReadOnlyDictionary<int, ShortageStatus> itemShortageStatuses,
    int closedByUserId, string user, string winUser, byte[] rowVersion);
```

- `itemQuantitiesPicked` von `decimal` auf `decimal?` — ein `null` darf ab jetzt **ankommen und
  bleiben**, statt als `?? 0m` zu verschwinden.
- Rueckgabewert: leere Liste = Abschluss durchgefuehrt; nicht-leere Liste = Abschluss **abgelehnt**,
  Eintraege sind die unvollstaendigen Zeilen (Position + Artikelnummer) fuer die Meldung.

**`WarehouseRequisitionRepository.cs:192-223`, `CloseAsync`:**
```csharp
foreach (var item in r.Items)
{
    item.QuantityPicked = itemQuantitiesPicked.TryGetValue(item.Id, out var q) ? q : null; // war: ?? 0m
    // ... Note/NoteEinkauf/ShortageStatus unveraendert ...
}

var incomplete = r.Items
    .Where(i => i.QuantityPicked == null && i.ShortageStatus == ShortageStatus.None)
    .OrderBy(i => i.Position)
    .Select(i => (i.Position, i.ArticleNumber))
    .ToList();
if (incomplete.Count > 0)
    return incomplete; // nichts wird gespeichert — SaveChangesAsync folgt nicht

r.Status = DeriveStatus(r);
// ... unveraendert ...
await _context.SaveChangesAsync();
return Array.Empty<(int, string)>();
```
Die Pflichtpruefung liest damit **dieselben** Werte, die ohnehin aus dem POST auf die Entity
geschrieben wurden — keine zusaetzliche DB-Lesestelle, keine zusaetzliche Bindungsfrage bei
`shortageStatuses` (bleibt `int[]?` wie bisher). Bricht die Pruefung ab, ist `r` zwar im
Change-Tracker modifiziert, aber ohne `SaveChangesAsync` wird nichts geschrieben — der
scope-gebundene `DbContext` verwirft die Aenderung am Request-Ende.

**`WarehousePickingController.cs`:**
- `Close` (Z.146-199): Parameter `int[] quantitiesPicked` → `int?[] quantitiesPicked`. Negativ-Pruefung
  null-sicher: `quantitiesPicked.Any(q => q.HasValue && q.Value < 0)`. `qtyDict` wird
  `Dictionary<int, decimal?>`, Befuellung `idx < quantitiesPicked.Length ? (decimal?)quantitiesPicked[idx] : null`.
  Nach dem `CloseAsync`-Aufruf:
  ```csharp
  var incomplete = await _repo.CloseAsync(id, qtyDict, noteDict, noteEkDict, statusDict,
      _user.GetCurrentAppUserId() ?? 0, _user.GetDisplayName(), _user.GetWindowsUserName(), rowVersion);
  if (incomplete.Count > 0)
  {
      TempData["WarningMessage"] = "Ist-Menge oder Fehlteil-Markierung fehlt bei: " +
          string.Join(", ", incomplete.Select(x => $"Pos {x.Position} ({x.ArticleNumber})"));
      return RedirectToAction(nameof(Details), new { id });
  }
  ```
  (Hausmuster: `TempData["WarningMessage"]` + `RedirectToAction`, kein `ErrorMessage`.)
- `PrintAndClose` (Z.263-307): gleiche Signatur-/Dict-Aenderung. Bei unvollstaendigen Zeilen:
  ```csharp
  return BadRequest(new { error = "Ist-Menge oder Fehlteil-Markierung fehlt bei: " +
      string.Join(", ", incomplete.Select(x => $"Pos {x.Position} ({x.ArticleNumber})")) });
  ```

#### C) Client: Bedienungshilfe statt Sammel-Dialog — `Details.cshtml`

Die serverseitige Pruefung ist die einzige **massgebliche** Schranke; die Client-Seite markiert nur
vorab, damit niemand unnoetig einen Redirect/Fehler abwartet (Hausregel „Melden statt still
behandeln" gilt auch hier — sichtbare Markierung statt stiller Blockade).

- **Z.74**: `<tr data-requested="@requestedInt">` um `data-position="@i.Position"
  data-article="@i.ArticleNumber"` ergaenzen (fuer die Client-Meldung).
- **Z.85**: `placeholder="@requestedInt"` entfaellt ersatzlos (Anforderung 5). Kein Ersatz-Placeholder
  — die bestellte Menge bleibt in der Nachbarspalte „Bestellt" (Z.78) sichtbar.
- **Z.183-198** (`close-confirm-modal`-HTML) entfaellt vollstaendig.
- **Z.356-357** (`bootstrap.Modal`-Instanziierung fuer das entfallene Modal) entfaellt.
- **Z.358-378** (`emptyRows()`/`fillSollAsIst()`) werden ersetzt:
  ```js
  function getIncompleteRows() {
      // Eine getippte 0 zaehlt als Wert (Freigabe-Antwort 1) — nur echte Leere zaehlt als offen.
      return Array.from(form.querySelectorAll('input[name="quantitiesPicked"]')).filter(i => {
          if (i.value.trim() !== '') return false;
          const hidden = i.closest('tr').querySelector('.shortage-hidden');
          return (hidden ? hidden.value : '0') === '0'; // ShortageStatus.None
      });
  }
  function markIncomplete(rows) {
      rows.forEach(i => i.classList.add('is-invalid'));
      if (rows[0]) rows[0].focus();
      const details = rows.map(i => {
          const tr = i.closest('tr');
          return `Pos ${tr.dataset.position} (${tr.dataset.article})`;
      }).join(', ');
      alert('Bitte Ist-Menge eintragen oder Fehlteil markieren: ' + details);
  }
  ```
- **Z.380-386** (`closeBtn`-Handler mit Modal) wird:
  ```js
  closeBtn.addEventListener('click', () => {
      const incomplete = getIncompleteRows();
      if (incomplete.length > 0) { markIncomplete(incomplete); return; }
      form.submit();
  });
  ```
- **Z.388-399** (`printAndCloseBtn`-Handler mit Modal) wird:
  ```js
  if (printAndCloseBtn) {
      printAndCloseBtn.addEventListener('click', () => {
          const incomplete = getIncompleteRows();
          if (incomplete.length > 0) { markIncomplete(incomplete); return; }
          performPrintAndClose();
      });
  }
  ```
- **Z.402-410** (`normalizeEmptyQuantitiesToZero()`) entfaellt vollstaendig, inkl. des Aufrufs in
  `performPrintAndClose()` (Z.413).

Diese View-/JS-Aenderung faellt unter die CLAUDE.md-Pflicht „Frontend-Arbeit: `frontend-design`-
Skill ist PFLICHT" — der Dev-Lauf ruft ihn explizit auf, auch wenn die Aenderung hier primaer
Verhalten (kein neues Markup ausser `.is-invalid`) betrifft.

#### D) Fehlerrueckmeldung bei PrintAndClose — `performPrintAndClose()`, Z.412-431

Bisher: jeder `!resp.ok`-Fall zeigt dieselbe generische Meldung und laedt die Seite neu (verwirft
ungespeicherte Eingaben). Neu: bei einer Validierungsablehnung (HTTP 400 aus Abschnitt B) bleibt die
Seite stehen, die Meldung nennt die betroffenen Zeilen, nichts wird verworfen:

```js
if (!resp.ok) {
    if (printTab) printTab.close();
    let message = 'Fehler beim Abschliessen — bitte Liste neu laden.';
    let reload = true;
    try {
        const data = await resp.json();
        if (data && data.error) message = data.error;
    } catch { /* keine JSON-Antwort, z.B. 500 */ }
    if (resp.status === 400) reload = false; // Validierungsfehler: Formular bleibt editierbar
    alert(message);
    if (reload) window.location.reload();
    return;
}
```
Kein Druck-Tab mit Fehlerseite: `printTab` wird wie bisher sofort geschlossen, bevor die Meldung
erscheint (Anforderung "kein Druck-Tab mit Fehlerseite" aus der Aufgabenstellung).

#### E) Folgewirkungs-Tabelle — Lesestellen **und** Schreibwege (main **und** Buendel identisch)

| Zweig | Datei:Zeile | Rolle | Verhalten nach dem Fix | Aendert sich? |
|---|---|---|---|---|
| main+Buendel | `Details.cshtml:292-303` (`collectProgress`) | **Schreibweg** (Autosave) | sendet `''` statt `'0'` fuer leere Felder | **ja — dieser Fix (A)** |
| main+Buendel | `WarehousePickingController.cs:221-234` (`SaveProgress`) | Schreibweg (Autosave) | bindet bereits `int?[]?`, schreibt `null` korrekt, sobald der Client es sendet | nein (Empfaenger war schon korrekt) |
| main+Buendel | `Details.cshtml:380-410` (Close/PrintAndClose-Handler + `normalizeEmptyQuantitiesToZero`) | **Schreibweg** (Abschluss) | sendet echte Feldwerte inkl. `''`, kein Zwangs-`'0'` mehr | **ja — dieser Fix (C)** |
| main+Buendel | `WarehousePickingController.cs:146-199`, `263-307` (`Close`/`PrintAndClose`) | **Schreibweg** (Bindung) | `int?[]` statt `int[]` — `''` bindet an gleichem Index zu `null` (siehe Bindungstest F) | **ja — dieser Fix (B)** |
| main+Buendel | `WarehouseRequisitionRepository.cs:205` (`CloseAsync`) | **Schreibweg** | kein `?? 0m`-Fallback mehr; `null` bleibt `null`, Pflichtpruefung greift davor | **ja — dieser Fix (B)** |
| main+Buendel | `WarehouseRequisitionRepository.cs:228` (`DeriveStatus`) | Leseweg | `(i.QuantityPicked ?? 0) >= i.QuantityRequested` — weiterhin null-sicher, unveraendert | nein |
| main+Buendel | `WarehouseRequisitionRepository.cs:300-302` (`SaveProgressAsync`) | Leseweg/Schreibweg | schreibt weiterhin genau das, was ankommt (kann `null` sein) | nein |
| main+Buendel | `WarehouseRequisitionRepository.cs:424-425` (`GetMissingPartsAsync`-Projektion) | Leseweg | `i.QuantityPicked ?? 0m` — betrifft nur bereits **geschlossene** Zeilen, die nach der Pflichtpruefung nie mehr `null` sein koennen; bleibt als Absicherung fuer Altdaten stehen | nein |
| main+Buendel | `Views/WarehousePicking/Details.cshtml:71-91` | Leseweg | `HasValue`-Check, leer statt Exception; Placeholder entfaellt (siehe C) | nur Placeholder |
| main+Buendel | `WarehousePickingPrintLayout.cs:74-76` (`CellText`) | Leseweg (Druck/Filter/Sort) | verifiziert: `HasValue`-Check, **leerer String** bei `null` — nicht `"0"` und nicht `"—"` | nein |
| main+Buendel | `Views/MissingParts/Index.cshtml:115`, `Views/MissingPartsLager/Index.cshtml:99` | Leseweg | liest `MissingPartRow.QuantityPicked`, dort bereits `decimal` (nicht nullable) nach der `?? 0m`-Projektion | nein |

Damit ist die Aussage der Erstfassung „alle Lesestellen sind bereits null-sicher" zwar fuer die
**Lesestellen** richtig, aber unvollstaendig: Die drei jetzt gefetteten **Schreibwege** waren die
eigentliche Fehlerquelle (K1-K3) und sind Gegenstand dieses Fixes.

#### F) Bindungstest — Pflichtnachweis, kein Vorschuss auf Annahme

Das Projekt hat weder `Microsoft.AspNetCore.Mvc.Testing` noch `Microsoft.AspNetCore.TestHost` im
Testprojekt referenziert und keinen `WebApplicationFactory`-Praezedenzfall — ein reiner
Controller-Unit-Test (Moq/EF-InMemory, wie im Rest der Testklasse) ruft die Action-Methode direkt
mit bereits fertig gebundenen `.NET`-Werten auf und testet damit **nicht** die Modellbindung selbst,
um die es hier geht.

Vorschlag (kleinstmoeglicher Eingriff, kein neues NuGet-Paket noetig — `Microsoft.AspNetCore.TestHost`
ist Teil der bereits per `FrameworkReference Include="Microsoft.AspNetCore.App"` referenzierten
Shared-Framework-Assemblies): ein minimaler `TestServer` mit einem eigenen Test-Controller, der
exakt dieselbe Parameter-Signatur wie `Close` hat, plus ein POST mit **wiederholten** Formular-Keys
(`FormUrlEncodedContent` erlaubt Duplikate):

```csharp
[Fact]
public async Task NullableIntArrayBinding_TreatsEmptyStringAsNullAtSameIndex()
{
    using var host = await new HostBuilder()
        .ConfigureWebHost(web => web.UseTestServer()
            .ConfigureServices(s => s.AddControllers())
            .Configure(app => { app.UseRouting(); app.UseEndpoints(e => e.MapControllers()); }))
        .StartAsync();
    var client = host.GetTestClient();

    var form = new[]
    {
        new KeyValuePair<string,string>("quantitiesPicked", "5"),
        new KeyValuePair<string,string>("quantitiesPicked", ""),
        new KeyValuePair<string,string>("quantitiesPicked", "7"),
    };
    var resp = await client.PostAsync("/bindtest/close", new FormUrlEncodedContent(form));
    var bound = await resp.Content.ReadFromJsonAsync<int?[]>();

    bound.Should().Equal(5, null, 7);
}
```
**Faellt dieser Test rot aus** (z. B. weil ASP.NET Core die leere Position doch ueberspringt statt
sie als `null` zu binden), gilt die im Nachtrag Koordinator K3 vorgesehene Rueckfallanweisung: auf
eine schluesselbasierte Bindung umstellen (`quantitiesPicked[<itemId>]` statt paralleler Arrays) und
dies im QA-Nachweis der Spec vermerken — **nicht** stillschweigend beim `int[]`-Verhalten bleiben.

## Migrations-/SQL-Auswirkungen

**Keine EF-Migration, kein Schema-Objekt.** Beleg (main; Buendel identisch da unveraendert seit
Fork):

- Modell: `IdealAkeWms/Models/WarehouseRequisitionItem.cs:21` — `public decimal? QuantityPicked`
  (bereits nullable, unveraendert durch diese Spec).
- `ApplicationDbContext.cs:1141` — `entity.Property(e => e.QuantityPicked)
  .HasColumnType("decimal(18,4)")`, kein `.IsRequired()`.
- `SQL/00_FreshInstall.sql:1672` — `[QuantityPicked] DECIMAL(18,4) NULL`.
- `WarehouseRequisitionItem : AuditableEntity` (verifiziert) — Audit-Felder liegen auf Zeilenebene.

**Einmaliges Datenskript `SQL/94_ResetAutosaveZeroQuantityPickedSubmitted.sql`** (Freigabe-Antwort
6b): verifizierte Rahmendaten —

- `WarehouseRequisitionStatus.Submitted = 2` (`WarehouseRequisitionStatus.cs:6`, gespeichert als
  `TINYINT`, siehe `SQL/00_FreshInstall.sql:1630-1634`, `Status.*table.Column<byte>`).
- `ShortageStatus.None = 0` (`ShortageStatus.cs:12`, `TINYINT`, `SQL/00_FreshInstall.sql:1676`).
- Numerierung: main steht bei `SQL/88_*`, das Buendel bei `SQL/93_*` (verifiziert per Verzeichnis-
  Listing, Stand 2026-09-29); die parallel laufende Spec
  [[2026-09-25-kommissionierliste-summierung-pdf-spec]] hat **keine** SQL-Nummer beansprucht (siehe
  ihr Abschnitt „Migrations-/SQL-Auswirkungen": „Keine"). `SQL/94_*` ist damit in **beiden** Zweigen
  frei.

> [!warning] Konflikt/offener Punkt — Hauspraxis fuer Einmal-Datenskripte
> ADR [[0004-migrations-und-sql-disziplin]] beschreibt den fuenfstufigen Workflow ausschliesslich
> fuer **Schema**-Aenderungen; jedes bisherige `SQL/XX_*.sql` ist 1:1 an eine EF-Migration mit
> `__EFMigrationsHistory`-Eintrag gekoppelt. Fuer ein reines Datenskript ohne Schema-Aenderung gibt
> es **keinen Praezedenzfall**. Entscheidung dieser Spec (dokumentiert, nicht still getroffen): das
> Skript bekommt trotzdem eine laufende Nummer (Auffindbarkeit/Chronologie im selben Ordner), aber
> **ohne** `__EFMigrationsHistory`-Eintrag (kein Schema-Objekt entsteht) und **ohne** Eintrag in
> `SQL/00_FreshInstall.sql` (ein frisch installiertes System hat keine fehlerhaften Autosave-Nullen
> zu bereinigen). Der Dateikopf markiert das Skript ausdruecklich als „EINMALIGES DATENSKRIPT, KEINE
> MIGRATION". Freigabe hierzu bei Schranke 1 einholen.

> [!warning] Konflikt — Reset-Wortlaut (Antwort 6b) vs. Fehlteil-Ausnahme
> Freigabe-Antwort 6b sagt woertlich: „`Submitted`: `QuantityPicked = 0` -> `NULL`" — ohne
> Einschraenkung nach `ShortageStatus`. Die Aufgabenstellung fuer diesen Nachzieh-Lauf fordert
> zusaetzlich, Zeilen mit gesetzter Fehlteil-Markierung **nicht** zurueckzusetzen, weil dort „0 eine
> bewusste Entscheidung" sei. Das ist mit dem Autosave-Fehler K1 **nicht sauber vereinbar**: K1
> schreibt `0` fuer **jede** leere Zeile bei **jedem** Autosave — unabhaengig davon, ob zuvor ein
> Fehlteil-Radio angeklickt wurde. Eine Zeile kann also `ShortageStatus <> None` **und** eine vom
> Autosave erzeugte, nie bewusst getippte `0` gleichzeitig haben. Diese Spec entscheidet sich fuer
> die **vorsichtigere** Variante (ShortageStatus = None als Zusatzbedingung, siehe Skript unten), weil
> das versehentliche Zuruecksetzen einer echten, bereits gebuchten Fehlteil-Entscheidung riskanter
> waere als eine verbleibende, nicht bereinigte Null — das wird hier **ausdruecklich als Abweichung
> vom woertlichen Text der Antwort 6b gekennzeichnet** und muss bei Schranke 1 bestaetigt oder
> korrigiert werden.

Skript (Entwurf, Dev-Lauf uebernimmt nach Bestaetigung des obigen Punkts):

```sql
-- SQL/94_ResetAutosaveZeroQuantityPickedSubmitted.sql
-- EINMALIGES DATENSKRIPT -- KEINE SCHEMA-AENDERUNG, KEIN __EFMigrationsHistory-Eintrag,
-- KEIN Eintrag in SQL/00_FreshInstall.sql.
-- Hotfix v1.30.1 (siehe [[2026-09-28-ake-hotfix-stueckliste-druck-lagerbestellung-ist-spec]]):
-- Der bisherige Autosave (Details.cshtml collectProgress) hat leere Ist-Mengen-Felder als 0 statt
-- NULL gespeichert (Nachtrag Koordinator K1). Fuer noch nicht abgeschlossene Bestellungen (Status
-- Submitted = 2) macht dieses Skript diese autogespeicherten Nullen wieder zu echten NULLs, damit
-- die neue Pflichtpruefung beim Abschliessen nicht durch alte Fehlbuchungen ausgehebelt wird.
-- PartiallyDelivered/Closed/Cancelled werden NICHT angefasst (dort kann eine 0 bereits gebucht sein).
-- Zeilen mit gesetzter Fehlteil-Markierung (ShortageStatus <> 0) werden NICHT zurueckgesetzt
-- (Abweichung vom woertlichen Text der Freigabe-Antwort 6b, siehe Konflikt-Hinweis im Spec-Rumpf --
-- bei abweichender Freigabe die WHERE-Klausel entsprechend anpassen).
-- VOR DEM LAUF: DB-Backup! Erst NACH dem Deploy der neuen Web-Version ausfuehren (sonst schreibt
-- der noch aktive alte Autosave sofort wieder 0).
SET NOCOUNT ON;

-- Zaehl-SELECT vor dem Reset -- Ergebnis vor dem UPDATE protokollieren.
SELECT COUNT(DISTINCT wr.Id) AS BetroffeneBestellungen, COUNT(*) AS BetroffeneZeilen
FROM dbo.WarehouseRequisitionItems wri
JOIN dbo.WarehouseRequisitions wr ON wr.Id = wri.WarehouseRequisitionId
WHERE wr.Status = 2          -- Submitted
  AND wri.QuantityPicked = 0
  AND wri.ShortageStatus = 0; -- None

UPDATE wri
SET wri.QuantityPicked = NULL,
    wri.ModifiedAt = GETDATE(),
    wri.ModifiedBy = 'Hotfix-1.30.1',
    wri.ModifiedByWindows = SYSTEM_USER
FROM dbo.WarehouseRequisitionItems wri
JOIN dbo.WarehouseRequisitions wr ON wr.Id = wri.WarehouseRequisitionId
WHERE wr.Status = 2          -- Submitted
  AND wri.QuantityPicked = 0
  AND wri.ShortageStatus = 0; -- None

SELECT @@ROWCOUNT AS ZeilenZurueckgesetzt;
```

## Audit-Feld-Auswirkungen

- `CloseAsync` und `SaveProgressAsync` setzen weiterhin bei jedem Update `ModifiedAt`/`ModifiedBy`/
  `ModifiedByWindows` (`WarehouseRequisitionRepository.cs:212-214`, `Z.298-...`) — unveraendert in
  ihrer Grundmechanik; lediglich der `QuantityPicked`-Typ wird nullable durchgereicht.
- Das Reset-Skript (siehe oben) setzt dieselben drei Felder auf den betroffenen Zeilen
  (`ModifiedBy = 'Hotfix-1.30.1'`, `ModifiedByWindows = SYSTEM_USER`, `ModifiedAt = GETDATE()`) —
  analog zum Muster, nach dem Service-Syncs ihren Servicenamen eintragen (ADR
  [[0003-auditableentity-als-entity-basis]]), hier mit einem Hotfix-Bezeichner statt eines
  Servicenamens, weil es sich um ein einmaliges manuelles Skript und keinen laufenden Dienst
  handelt.
- `PrintBom` (Teil 1) ist eine reine Lese-/Druck-Action ohne Entity-Schreibzugriff, daher keine
  Audit-Feld-Beruehrung.

## Akzeptanzkriterien

**Teil 1**
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

**Teil 2**
6. Eine neu erfasste, noch nicht bearbeitete Lagerbestellungs-Position zeigt im IST-Feld weder
   einen Wert noch einen Placeholder.
7. Der Autosave (`SaveProgress`) speichert ein leeres IST-Feld als `NULL`; ein DB-Blick nach einem
   Autosave mit einem leeren Feld zeigt `NULL`, nicht `0`.
8. Ein Controller-/Bindungstest belegt: `itemIds=[1,2,3]`, `quantitiesPicked=["5","","7"]` bindet
   an denselben Indizes zu `[5, null, 7]` (kein Verschieben der Werte).
9. Ein Abschliessen (Close **und** PrintAndClose) mit mindestens einer Zeile ohne IST-Wert **und**
   ohne Fehlteil-Markierung wird serverseitig blockiert — auch bei einem direkten POST ohne
   JavaScript — mit einer Meldung, die die betroffenen Zeilen (Position + Artikelnummer) nennt;
   nichts wird gebucht.
10. Eine getippte `0` als IST-Wert geht beim Abschliessen durch und wird als `0` gebucht.
11. Eine leere IST-Zeile **mit** Fehlteil-Markierung (Fehlteil oder Wird nicht nachgeliefert) geht
    beim Abschliessen durch.
12. Der „Soll = Ist buchen?"-Dialog (`close-confirm-modal`), `fillSollAsIst` und
    `normalizeEmptyQuantitiesToZero` existieren im Code nicht mehr.
13. Nach Ausfuehrung des Reset-Skripts sind in `Submitted`-Bestellungen alle Zeilen mit
    `QuantityPicked = 0` und `ShortageStatus = None` auf `NULL` zurueckgesetzt; Zeilen mit
    `ShortageStatus <> None` sowie alle Zeilen in `PartiallyDelivered`/`Closed`/`Cancelled`-
    Bestellungen bleiben unveraendert (vorbehaltlich Bestaetigung des Konflikt-Hinweises oben).
14. Bestehende Lagerbestellungen mit bereits eingetragenen, echten IST-Werten (Status
    PartiallyDelivered oder abgeschlossen) zeigen diese Werte nach dem Deploy unveraendert an.

**Uebergreifend**
15. `dotnet build` und `dotnet test` sind gruen; kein neuer EF-Migrations-Eintrag wird erzeugt.
16. Version in `IdealAkeWms/AppVersion.cs` **und** `IDEALAKEWMSService/AppVersion.cs` ist auf
    `1.30.1` erhoeht, `Views/Help/Changelog.cshtml` hat einen neuen Eintrag fuer beide Teile.
17. `docs/TESTSZENARIEN.md` enthaelt TS-5.11 (Teil 1) und TS-18.10 (Teil 2, aktualisiert um
    Pflichtpruefung/Autosave/Reset); `secondbrain/tests/testszenarien-index.md` ist nachgezogen.

## Test-Szenarien

Neue/aktualisierte Szenarien (main steht bei TS-5.1–TS-5.10 und TS-18.1–TS-18.9, verifiziert in
`docs/TESTSZENARIEN.md` — TS-5.11 und TS-18.10 sind frei):

**TS-5.11 — Stueckliste mit vielen Positionen drucken (Regression zu TS-5.3/TS-5.9)**
- Vorbedingung: FA mit einer Stueckliste ≥ 300 Positionen (z. B. per Testdaten oder ein bekannt
  grosses Baugruppen-FA), Picking- oder Vorbau-Zugriff.
- Schritte: `Picking/Bom` oeffnen, keinen Filter setzen, auf „Stueckliste drucken" klicken.
- Erwartet: neues Tab mit der vollstaendigen Stueckliste, kein HTTP 404.15, keine Fehlermeldung.
- Negativfall: einen Spaltenfilter setzen, der die Positionen auf wenige reduziert, erneut drucken
  → nur die sichtbaren Positionen erscheinen (wie bisher, TS-5.9-Regression).
- Negativfall 2: `/Picking/PrintBom/{id}` direkt in die Adresszeile eingeben (GET, keine Parameter)
  → vollstaendiger Druck ohne Fehler.

**TS-18.10 — Lagerbestellung: Ist-Feld ohne Vorbefuellung, Pflichtpruefung beim Abschliessen (v1.30.1)**
- Vorbedingung: offene Lagerbestellung (Status Submitted) mit mindestens drei Positionen, keine
  bisher bearbeitet.
- Schritt 1: `WarehousePicking/Details` oeffnen → IST-Feld zeigt weder Wert noch Placeholder.
- Schritt 2: Position 1 mit IST=5 befuellen, ein anderes Feld antriggern (Autosave), Seite neu
  laden → Position 1 zeigt 5, Positionen 2 und 3 bleiben **leer** (nicht 0) — Autosave-Regression
  zu K1.
- Schritt 3: „Speichern + Abschliessen" klicken, ohne Position 2/3 zu befuellen → Abschliessen wird
  verweigert, Meldung nennt Pos 2 und Pos 3 mit Artikelnummer; Bestellung bleibt Submitted, nichts
  gebucht.
- Schritt 4: Position 2 mit „0" befuellen, Position 3 als „Fehlteil" markieren, erneut abschliessen
  → geht durch; Position 2 wird mit 0 gebucht.
- Negativfall 1: analog Schritt 3, aber ueber „Drucken und Abschliessen" — kein Druck-Tab mit
  Fehlerseite, stattdessen eine Meldung im aktuellen Fenster; das Formular bleibt editierbar.
- Negativfall 2: ein direkter POST an `/WarehousePicking/Close/{id}` ohne JavaScript (z. B.
  DevTools/Postman) mit einer leeren Zeile ohne Fehlteil-Markierung → wird ebenso blockiert
  (serverseitige Pruefung ist nicht JS-abhaengig).
- Negativfall 3 (Bestandsdaten): eine bereits laufende Bestellung mit vor dem Fix eingetragenen,
  echten IST-Werten (Status PartiallyDelivered) oeffnen → Werte erscheinen unveraendert (Reset-
  Skript fasst nur `Submitted` an).

## Deploy

- **Web-App:** ja (AKE) — `PickingController`, `WarehousePickingController`,
  `WarehouseRequisitionRepository`, `IWarehouseRequisitionRepository`, `Bom.cshtml`,
  `Details.cshtml`, `AppVersion.cs`, `Changelog.cshtml`.
- **Service:** nein (nur Versions-Konstante aus Konsistenzgruenden, keine Verhaltensaenderung).
- **Migration:** nein (siehe Migrations-/SQL-Auswirkungen); **ein einmaliges Datenskript** ist Teil
  des Deploys.
- **Publish-Befehle** (im Worktree, nach bestandenem Test, vor dem Merge):
  ```
  dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
  dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService
  ```
- **Reihenfolge AKE (jetzt, mit v1.30.1):**
  1. DB-Backup.
  2. Web-Publish auf AKE-IIS (neue Web-Version live — Autosave schreibt ab sofort `null` statt `0`).
  3. `SQL/94_ResetAutosaveZeroQuantityPickedSubmitted.sql` **danach** ausfuehren (nicht davor —
     sonst schreibt der noch aktive alte Autosave sofort wieder `0` in dieselben Zeilen).
  4. Manueller Test (Schranke 2).
  5. Merge in `main`.
  Der Service-Publish ist nur der Versionsgleichstand wegen noetig, keine funktionale Aenderung.
- **Reihenfolge IDEAL/Buendel:** Das Skript **erst** ausfuehren, wenn dort — nach dem
  Vorwaerts-Merge **und** dessen eigenem, spaeteren Deploy — die neue Web-Version live ist (gleicher
  Grund: sonst schreibt der dortige alte Autosave die Nullen sofort wieder).

## Uebertrag ins Buendel (Vorwaerts-Merge)

**Ablauf:** Nach Abnahme + Merge in `main`: `git merge main` im Worktree
`.claude/worktrees/2026-08-07-ideal-teile-1-5` (Branch `feature/2026-08-07-ideal-teile-1-5`).
**Nicht mergen, waehrend dort ein `/dev`-Lauf aktiv ist** — dessen Ende abwarten.

**Gemessene Ausgangslage (2026-09-28, read-only ermittelt, bei diesem Nachzieh-Lauf erneut
sondiert):**
- Merge-Base `main`/Buendel: `2a34ff46`.
- `WarehousePicking`-Dateien (`Views/WarehousePicking/*`, `WarehousePickingController.cs`,
  `Data/Repositories/WarehouseRequisitionRepository.cs`,
  `Data/Repositories/IWarehouseRequisitionRepository.cs`,
  `IdealAkeWms.Tests/Controllers/WarehousePickingControllerTests.cs`,
  `IdealAkeWms.Tests/Repositories/WarehouseRequisitionRepositoryTests.cs`) hat das Buendel seit dem
  Merge-Base weiterhin **nicht** veraendert — Teil 2 ist dort voraussichtlich **konfliktfrei**,
  trotz der jetzt deutlich groesseren Diff-Flaeche (Signaturaenderung `CloseAsync`, ~28
  Testaufrufstellen, neues JS). Das ersetzt nicht die oben durchgefuehrte Folgewirkungs-Pruefung
  ausserhalb dieser Dateien (Sage-Buchung, BDE, Druck, Services) — dort wurde ebenfalls kein
  zusaetzlicher Buendel-Fund gemacht.
- Buendel-Stand bei SQL-Skripten: `SQL/93_AddProductionWorkplaceSageFields.sql` (hoechste Nummer,
  verifiziert 2026-09-29). `SQL/94_ResetAutosaveZeroQuantityPickedSubmitted.sql` ist eine **neue**
  Datei — kein Merge-Konflikt zu erwarten, sofern das Buendel bis zum Merge-Zeitpunkt keine eigene
  `94_*`-Datei anlegt.
- `Bom.cshtml`/`PickingController.cs`/`AppVersion.cs`/`Changelog.cshtml`/`docs/TESTSZENARIEN.md`:
  Konfliktlage unveraendert zur urspruenglichen Einschaetzung (Teil 1 ist inhaltlich gleich
  geblieben) — Druck-Handler an unterschiedlichen Zeilen, beide `AppVersion`/`Changelog`-Eintraege
  behalten, Versionsnummer nach dem Merge **hoeher** als der Buendel-Stand vor dem Merge setzen.
- Migrations-Ordner ist `IdealAkeWms/Migrations/` (nicht `Data/Migrations`).

**Konfliktaufloesung (Vorgaben fuer den Merge-Lauf):**
- `Bom.cshtml`/`PrintBom`: der neue POST-Ausloeser muss **nach** der bestehenden Leerzustand-Sperre
  des Buendels (`Bom.cshtml:1231-1238`) eingehaengt werden — die Sperre bleibt unveraendert
  bestehen, nur der Druck-Aufruf danach wird ersetzt.
- `AppVersion.cs`/`Changelog.cshtml`: **beide** Aenderungen behalten, die Versionsnummer nach dem
  Merge auf einen Stand **hoeher als das Buendel vor dem Merge** setzen — **nicht** auf `1.30.1`
  zurueckfallen, sonst waere das ein Ruckschritt im Buendel.
- `docs/TESTSZENARIEN.md`: beide Kapitel-Ergaenzungen behalten; TS-5.11/TS-18.10 kollidieren nicht
  mit den Buendel-eigenen flachen `TS-NN`-Kapiteln.
- `WarehouseRequisitionRepository.cs`/`WarehousePickingController.cs`/Tests: da das Buendel diese
  Dateien seit dem Fork nicht angefasst hat, ist ein sauberer Fast-Forward auf den Datei-Inhalt zu
  erwarten — trotzdem nach dem Merge `dotnet build`/`dotnet test` pruefen, weil die
  `CloseAsync`-Signatur jetzt oeffentlich sichtbar anders ist (ein spaeterer Buendel-Caller wuerde
  sonst erst hier auffallen).
- Falls das Buendel bis zum Merge-Zeitpunkt selbst eine `SQL/94_*`-Datei angelegt hat: das
  AKE-Hotfix-Skript auf die naechste freie Nummer umbenennen (Inhalt bleibt gleich).
- **Pflicht-Deliverable des Merge-Laufs:** die tatsaechlich aufgetretenen Konfliktdateien
  protokollieren (Abgleich mit der obigen Vorhersage) — in der Aufgaben-Notiz zu diesem Merge
  festhalten.

**Akzeptanzkriterien nach dem Merge:**
- A1. Alle Buendel-Tests sind nach dem Merge gruen.
- A2. Eine grosse Stueckliste (≥ 300 Positionen) druckt im Buendel ohne HTTP 404.15.
- A3. Die Buendel-Leerzustand-Sperre („Alle Ziele ohne Treffer") funktioniert unveraendert.
- A4. Die Lagerbestellung im Buendel zeigt kein vorbefuelltes IST-Feld mehr; die Pflichtpruefung
  beim Abschliessen greift auch dort (Close und PrintAndClose).
- A5. Kein IDEAL-spezifischer Pfad (FA-Hierarchie, Sub-FA, Sage-Ruecklauf) behandelt ein leeres IST
  falsch — erneute Kurzpruefung der Folgewirkungs-Tabelle nach dem Merge, falls das Buendel bis
  dahin neue Lesestellen hinzugefuegt hat.
- A6. Das Reset-Skript wird im Buendel-Zielsystem (IDEAL) **erst** nach dessen eigenem Deploy der
  gemergten Web-Version ausgefuehrt, nicht vorher (siehe Deploy-Abschnitt).

## Protokoll: ueberholte Erstfassung Teil 2 (2026-09-28)

> [!warning] UEBERHOLT (2026-09-29) — siehe Nachtrag Koordinator K1-K4 und Abschnitt
> "Technischer Loesungsentwurf Teil 2 (massgeblich)" oben. Der folgende Text ist die urspruengliche,
> vor dem Code-Abgleich des Koordinators geschriebene Fassung. Er bleibt **ausschliesslich als
> Protokoll** stehen und ist **nicht** die Arbeitsgrundlage fuer die Umsetzung.

### Wichtiger Befund vor der Umsetzung (Teil 2) — ueberholt

> [!warning] UEBERHOLT — die Aussage "Ein leises 0-Buchen ohne Nachfrage gibt es nicht" ist laut
> Nachtrag Koordinator K1/K2 falsch. Siehe Korrektur oben.

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
  ~~Ein leises 0-Buchen ohne Nachfrage gibt es nicht.~~ **Falsch, siehe K1/K2.**
- Die urspruengliche Design-Vorlage (`docs/superpowers/specs/2026-04-30-lagerbestellung-aus-produktion-design.md:370`)
  sah tatsaechlich „Ist (Number-Input, default = Bestellt)" vor — das wurde bei der Umsetzung **nicht**
  so gebaut (Wert bleibt leer), nur der Placeholder zitiert die Bestellt-Menge.

**Konsequenz (Stand 2026-09-28, ueberholt):** Die tatsaechlich vorhandene, dem Backlog am naechsten
kommende Stelle ist der `close-confirm-modal`-„Ja"-Button — das ist inhaltlich bereits die in
Rueckfrage 6 befuerchtete „Komfort-Schaltflaeche IST = Bestellt", nur als **Sammelaktion fuer alle
leeren Zeilen** statt pro Zeile, und **hinter** einer expliziten Rueckfrage statt automatisch. Ob
dieser Mechanismus (a) unveraendert bleibt, (b) der „Ja"-Button entfaellt (harter Zwang zu zaehlen
oder Fehlteil zu markieren), oder (c) nur der irrefuehrende Placeholder verschwindet, ist eine
**fachliche** Entscheidung — siehe Rueckfragen 2/4/5. Es wird **nicht geraten**; die Spec schlaegt
(c) als risikoarme Mindestmassnahme vor und stellt (b) als Alternative zur Wahl.

### Technischer Loesungsentwurf Teil 2 — ueberholte Erstfassung

> [!warning] UEBERHOLT — "Keine Aenderung an WarehousePickingController/Repository noetig" und
> "close-confirm-modal bleibt unveraendert (Empfehlung)" sind durch K1-K3 ueberholt. Siehe
> "Technischer Loesungsentwurf Teil 2 (massgeblich)" oben fuer die tatsaechliche Umsetzung.

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
- ~~Keine Aenderung an `WarehousePickingController.cs`, `WarehouseRequisitionRepository.cs`,
  `WarehousePickingPrintLayout.cs` noetig — alle bestehenden Lese-/Schreibpfade sind bereits
  null-sicher (siehe Folgewirkungs-Tabelle).~~ **Falsch — siehe K3 und den massgeblichen
  Loesungsentwurf oben.**

**Folgewirkung — Lesestellen von `QuantityPicked` — ueberholte Tabelle (main **und** Buendel
identisch, da diese Dateien seit dem gemeinsamen Vorfahren `2a34ff46` in keinem der beiden Zweige
veraendert wurden):**

| Zweig | Datei:Zeile | Was passiert heute bei `null` | Soll sich aendern? |
|---|---|---|---|
| main+Buendel | `WarehouseRequisitionRepository.cs:205` (`CloseAsync`) | liest aus dem Formular-Dict, Default `0m` falls Key fehlt — Key fehlt nie (jede Zeile hat ein Hidden-`itemIds`) | ~~nein~~ **ja, siehe massgeblicher Entwurf** |
| main+Buendel | `WarehouseRequisitionRepository.cs:228` (`DeriveStatus`) | `(i.QuantityPicked ?? 0) >= i.QuantityRequested` — bereits null-sicher | nein |
| main+Buendel | `WarehouseRequisitionRepository.cs:300-302` (`SaveProgressAsync`) | schreibt genau das, was der Client sendet (kann explizit `null` sein) | nein |
| main+Buendel | `WarehouseRequisitionRepository.cs:424-425` (`GetMissingPartsAsync`-Projektion) | `i.QuantityPicked ?? 0m` — null-sicher | nein |
| main+Buendel | `Views/WarehousePicking/Details.cshtml:71-91` | `HasValue`-Check, leer statt Exception | nein (nur Placeholder, siehe oben) |
| main+Buendel | `WarehousePickingPrintLayout.cs:74-76` (`CellText`, Druck/Filter/Sort) | `HasValue`-Check, leerer String statt Exception | nein |
| main+Buendel | `Views/MissingParts/Index.cshtml:115`, `Views/MissingPartsLager/Index.cshtml:99` | liest `MissingPartRow.QuantityPicked`, dort bereits `decimal` (nicht nullable) **nach** der `?? 0m`-Projektion in `WarehouseRequisitionRepository.cs:424` | nein |

~~**Kein Fund** einer Stelle (in keinem der beiden Zweige), die bei `QuantityPicked == null` eine
Exception wirft oder ungewollt eine `0` bucht, ohne dass der Anwender vorher gefragt wurde.~~ **Diese
Aussage betrifft nur Lesestellen und ist fuer diese richtig — die eigentlichen Schreibwege
(`collectProgress`, `normalizeEmptyQuantitiesToZero`, `Close`/`PrintAndClose`-Bindung) buchen laut
K1/K2 sehr wohl still eine `0`. Siehe Abschnitt E des massgeblichen Loesungsentwurfs oben.**

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
6. **(durch den Code erzwungen, siehe Nachtrag K1-K3)** Wird der Autosave-Fehler K1 — leere IST-Felder
   werden als `0` gespeichert — in diesem Hotfix mitbehoben? Und was gilt fuer offene Bestellungen, in
   denen der Autosave bereits `0` fuer ungezaehlte Zeilen gespeichert hat?

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

1. → **Leer.** Das Feld ist bereits nullable, `null` bedeutet "nicht gezaehlt", `0` bedeutet "gezaehlt,
   nichts da". Beides muss unterscheidbar bleiben — das setzt die Korrektur K1 voraus (Antwort 6).

2. → **Pflichtfeld beim Abschliessen.** Jede Zeile braucht vor dem Abschliessen einen **eingetragenen**
   IST-Wert (auch `0`, aber getippt) **oder** eine explizite Fehlteil-Markierung. Fehlt beides, wird das
   Abschliessen blockiert, und die Meldung nennt die betroffenen Zeilen. Beim **Autosave** bleibt ein
   leeres Feld leer (`null`) — zwischengespeichert wird der Arbeitsstand, nicht eine Annahme.
   **Warum nicht "leer = nicht bearbeitet" beim Abschliessen:** Dann stellte sich sofort die Frage, was mit
   solchen Zeilen gebucht wird, und jede Antwort darauf waere entweder die stille 0 in anderer Form oder
   eine Teillieferungs-Logik, die nicht Teil dieses Hotfixes ist. Die Anforderung zielt auf eine
   **bewusste Eingabe** — das Pflichtfeld erzwingt genau sie.
   Das Repository behandelt `null` beim Abschliessen **nie** als `?? 0m`; die Pflichtpruefung verhindert,
   dass es dort ankommt, und ein `null` an dieser Stelle ist ein Fehler, keine Menge.

3. → **Siehe Antwort 6.** Die Frage setzte eine Vorbefuellung voraus, die es nicht gibt (Nachtrag K1).
   Betroffen sind nicht vorbefuellte Werte, sondern vom Autosave geschriebene Nullen.

4. → **Die Sammel-Variante (`close-confirm-modal`, "Ja" = Soll als Ist) entfaellt, keine Pro-Zeile-
   Variante.** Sie ist die Vorbefuellung mit "Bestellt" — nur verschoben auf den Moment des Abschliessens.
   Mit Antwort 2 wird die Rueckfrage "Soll = Ist buchen?" ohnehin gegenstandslos: Das Abschliessen geht
   entweder durch (alle Zeilen erfasst) oder wird mit Meldung blockiert. `fillSollAsIst` und
   `normalizeEmptyQuantitiesToZero` entfallen.
   **Das ist die Aenderung, die das Lager im Alltag spuert** — "alles wie bestellt" per Klick gibt es nicht
   mehr. Vor dem Deploy ankuendigen.

5. → **Die Bestellt-Menge aus dem Placeholder entfernen.** Eine graue Zahl im Eingabefeld sieht aus wie ein
   Wert — vermutlich genau der Eindruck, der zur Anforderung "nicht mehr vorbefuellen" gefuehrt hat. Die
   bestellte Menge steht in ihrer eigenen Spalte. Kein Placeholder, oder hoechstens ein neutraler ohne Zahl.

6. → **(a) Ja, K1 wird in diesem Hotfix mitbehoben — Pflicht.** Ohne die Korrektur des Autosaves gibt es
   nach dem ersten Speichern keine leeren Felder mehr, und Antworten 1 und 2 blieben wirkungslos.
   **(b) Offene Bestellungen: einmaliger Ruecksetzer, aber NUR fuer Status `Submitted`.**
   Mit dem Pflichtfeld aus Antwort 2 gilt eine vom Autosave geschriebene `0` als eingetragener Wert — das
   Abschliessen ginge durch und buchte `0` fuer eine Zeile, die nie gezaehlt wurde. Der alte Fehler
   ueberlebte also in allen heute offenen Bestellungen.
   - **`Submitted`** (nie abgeschlossen, also nie gebucht): `QuantityPicked = 0` -> `NULL`. Kosten:
     Eine echt getippte `0` muss erneut eingegeben werden. Das ist der richtige Tausch — eine
     Wiedereingabe gegen eine falsche Buchung.
   - **`PartiallyDelivered` und abgeschlossene Bestellungen: unveraendert.** Dort kann eine `0` bereits
     gebucht sein; sie zurueckzusetzen, koennte beim naechsten Abschliessen doppelt buchen.
   - Zaehl-`SELECT` vorab; Audit-Regel beachten (`ModifiedByWindows = SYSTEM_USER`, `ModifiedAt`); im
     Deploy-Abschnitt beider Systeme dokumentieren — AKE jetzt, IDEAL mit dem Buendel.

**Version: `1.30.1`** — Vorschlag K4 uebernommen. Ein Patch, in keinem Zweig vergeben, sortiert vor dem
Buendel.

**Zur Bindung (K3):** `int?[]` fuer `Close`/`PrintAndClose` wie vorgeschlagen — **der Test mit
`["5","","7"]` ist Pflicht**, nicht die Annahme. Faellt er rot aus (Indizes verschieben sich), auf eine
schluesselbasierte Bindung umstellen (Positions-ID -> Menge) statt paralleler Arrays.

**BESTAETIGT vom Menschen (2026-09-28):** *"Wenn nicht geliefert, muss das bestaetigt werden — manuell
eintippen bzw. das Fehlteil gesetzt."*
Damit gelten Antwort 2 (Pflichtfeld: eingetippter Wert oder Fehlteil) und Antwort 4 (kein "alles wie
bestellt" per Klick) verbindlich. **Antwort 6b folgt aus demselben Grundsatz:** Eine vom Autosave
geschriebene `0` ist keine Bestaetigung — niemand hat sie eingetippt. Das Zuruecksetzen in nie
abgeschlossenen Bestellungen (`Submitted`) setzt den Grundsatz fuer die Altdaten durch.

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

## Nachtrag Spec-Agent (2026-09-29) — Verifikation und Konflikte dieses Nachzieh-Laufs

Diese Spec wurde gemaess der Aufgabenstellung "NACHZIEHEN der Entwurfs-Spec" ueberarbeitet. Folgendes
wurde am Code verifiziert (main, `C:\git\IDEAL-AKE-WMS`) bzw. am Buendel-Worktree
(`.claude\worktrees\2026-08-07-ideal-teile-1-5`):

- **Version main:** `IdealAkeWms/AppVersion.cs:5` = `"1.30.0"` (bestaetigt frei fuer `1.30.1`).
- **TS-Nummern:** `docs/TESTSZENARIEN.md` hat TS-5.1–TS-5.10 und TS-18.1–TS-18.9 (verifiziert per
  Grep) — TS-5.11/TS-18.10 sind frei, wie schon in der Erstfassung angenommen.
- **SQL-Nummern:** main hoechste `SQL/88_AllowMultipleDummyRequisitionItems.sql`, Buendel hoechste
  `SQL/93_AddProductionWorkplaceSageFields.sql` (Verzeichnis-Listing, kein weiterer Worktree
  vorhanden). Die parallele Spec [[2026-09-25-kommissionierliste-summierung-pdf-spec]] beansprucht
  laut ihrem eigenen Migrations-Abschnitt **keine** SQL-Nummer. `SQL/94_*` ist frei in beiden Zweigen.
- **`WarehouseRequisitionItem : AuditableEntity`** bestaetigt (`Models/WarehouseRequisitionItem.cs:5`)
  — Audit-Felder liegen auf Zeilenebene, kein Konflikt mit Antwort 6b.
- **`WarehouseRequisitionStatus.Submitted = 2`**, **`ShortageStatus.None = 0`**, beide als `TINYINT`
  gespeichert (`WarehouseRequisitionStatus.cs`, `ShortageStatus.cs`, `SQL/00_FreshInstall.sql:1630-1696`).
- **Kein Repository-Decorator/Caching** vor `WarehouseRequisitionRepository` gefunden (nur
  `Program.cs`-Registrierung) — die Signaturaenderung von `CloseAsync` betrifft ausschliesslich
  Interface, Implementierung, zwei Controller-Aufrufstellen und die Tests.
- **Testinfrastruktur:** kein `WebApplicationFactory`/`TestHost`-Praezedenzfall im Projekt; die
  Empfehlung in Abschnitt F (eigener minimaler `TestServer`, kein neues NuGet-Paket) ist eine
  **neue** Vorgehensweise fuer dieses Projekt und wird deshalb explizit benannt statt als
  selbstverstaendlich vorausgesetzt.

**Konflikte (nicht still aufgeloest):**
1. **Ueberholte Aussagen im Originaltext** — siehe die Callouts direkt an den betroffenen Stellen und
   der gesamte Abschnitt „Protokoll: ueberholte Erstfassung Teil 2 (2026-09-28)".
2. **SQL-Nummer fuer Einmal-Datenskripte** — keine Hauspraxis vorhanden (ADR
   [[0004-migrations-und-sql-disziplin]] deckt nur Schema-Aenderungen ab); Entscheidung dokumentiert
   im Abschnitt Migrations-/SQL-Auswirkungen, zur Bestaetigung bei Schranke 1.
3. **Reset-Skript: ShortageStatus-Ausnahme vs. woertlicher Text der Antwort 6b** — siehe Callout im
   Abschnitt Migrations-/SQL-Auswirkungen; diese Spec waehlt die vorsichtigere Variante, markiert
   das aber ausdruecklich als Abweichung, die bei Schranke 1 bestaetigt oder korrigiert werden muss.
4. **Testaufwand fuer die Signaturaenderung:** die Umstellung von `CloseAsync` auf `decimal?` bricht
   ca. 28 bestehende `Dictionary<int, decimal>`-Stellen in
   `IdealAkeWms.Tests/Repositories/WarehouseRequisitionRepositoryTests.cs` sowie mehrere
   `int[]`-Literale in `IdealAkeWms.Tests/Controllers/WarehousePickingControllerTests.cs` (Compile-
   Bruch, kein Verhaltensbruch — beide Dateien nutzen an anderer Stelle bereits `int?[]`-Literale
   fuer `SaveProgress`-Tests, das Muster existiert also schon im Projekt). Kein Konflikt, aber als
   Aufwandshinweis fuer den Dev-Lauf festgehalten.
