---
typ: feature
---
# Listen: Spalten ein-/ausblendbar je Benutzer

Der Benutzer soll in den Listen selbst entscheiden koennen, **welche Spalten er sieht**.

## Die Mechanik existiert bereits — sie wird ANGESCHLOSSEN, nicht neu gebaut

Vorgabe: **konsistent zum Bestehenden, gleiche Logik.** Im Repo liegt dafuer bereits alles:

| Baustein | Datei |
|---|---|
| Client-Logik | `wwwroot/js/column-preferences.js` |
| API | `Controllers/Api/UserViewPreferencesApiController.cs` (`/api/user-view-preferences/{viewKey}`) |
| Persistenz | `Models/UserViewPreference.cs`, `Data/Repositories/UserViewPreferenceRepository.cs` |

Die bestehende Implementierung kann bereits: **Sichtbarkeit, Breite, Reihenfolge und
Standard-Sortierung je Ansicht (`viewKey`) und Benutzer** — mit Zahnrad-Dialog (Offcanvas),
Kontextmenue, Ziehgriffen zum Verbreitern und verzoegertem Auto-Speichern.

**Damit sind die Fragen aus dem ersten Entwurf dieser Notiz erledigt:**
- **Speicherort:** pro Benutzer in der **Datenbank** (nicht `localStorage`) — folgt dem Benutzer
  ueber Terminals hinweg. Die Ueberlegung zu gemeinsam genutzten Terminals ist gegenstandslos, das
  Problem ist bereits geloest.
- **Pflichtspalten:** existiert als `locked` in der Spalten-Konfiguration.
- **Neue Spalten nach einem Update:** existiert als `defaultHidden` plus `mergeWithDefaults` —
  eine gespeicherte Auswahl blendet unbekannte Spalten nicht aus, die Konfiguration entscheidet.

## Was tatsaechlich zu tun ist — Befund am Worktree-Stand (2026-08-12)

**Die Kommissionierlisten-View ist bereits zu 90 % verdrahtet.** In
`Views/FaHierarchyKommissionierListen/Index.cshtml` stehen schon:
`data-view-key="FaHierarchyKommissionierListen"`, `filterable-table`,
`data-server-column-filter="true"` und ein `data-col-key` an **jedem** `<th>`.
**Es fehlen exakt drei Dinge** — Vorlage ist `Views/ProductionOrders/Index.cshtml` (Dateiende):

1. **View-Config** unmittelbar vor der `Scripts`-Section:
   ```html
   <script type="application/json" id="view-config">
   { "viewKey": "FaHierarchyKommissionierListen", "supportsReorder": true, "supportsSortDefault": true }
   </script>
   ```
2. **Column-Config** mit denselben `key`-Werten wie die `data-col-key` der `<th>`:
   ```html
   <script type="application/json" id="column-config">
   [
     { "key": "hauptfa", "label": "HauptFA", "locked": true, "defaultWidth": 90 },
     { "key": "matchcode", "label": "Matchcode", "locked": true, "defaultWidth": null },
     ... die uebrigen mit "locked": false
   ]
   </script>
   ```
   `locked: true` fuer die identifizierenden Spalten (`hauptfa`, `matchcode`), alles andere frei.
   Selten gebrauchte Spalten (`artikeltyp`, `material`, `beschichtet`) koennen
   `"defaultHidden": true` bekommen — die Liste ist mit 11 Spalten breit.
3. **Skript einbinden**, vor `table-filter.js`:
   ```html
   <script src="~/js/column-preferences.js" asp-append-version="true"></script>
   ```

Aufwand je Liste: rund zwanzig Zeilen. **Kein C#, keine Migration, keine API-Aenderung** — die
Server-Seite (`UserViewPreferencesApiController`, Repository, Tabelle) steht bereits und ist
view-key-agnostisch.

**Deshalb gehoert das in den LAUFENDEN Epic, nicht in einen spaeteren Durchgang.** Teil 3 schneidet
gerade den gemeinsamen Listen-Baustein; nimmt er die drei Bloecke auf, erben Teil 4 und Teil 5 sie
kostenlos. Spaeter nachgezogen sind es drei Views einzeln — plus die Frage, warum sie sich anders
verhalten als der Rest der Anwendung.

## Offene Punkte (nur diese)

- **Tree-Table als Vorbedingung.** `column-preferences.js` arbeitet ueber Spaltenindizes
  (`thead tr:first-child th`) — es braucht eine **echte Tabelle**. Die FA-Struktur ist heute ein
  freifliessender Baum ohne ausgerichtete Spalten. Die Umstellung auf ein Tree-Table aus
  [[2026-08-12-fa-struktur-darstellung]] ist damit **Voraussetzung**, nicht Beiwerk. Fuer die
  Listen der Teile 3-5 gilt das nicht — die sind ohnehin Tabellen.
- **Zusammenspiel mit den Spaltenfiltern pruefen.** Beide nutzen `data-col-key`, und die
  Client-Logik kennt die Filterzeile bereits (`thead tr.filter-row`). Zu pruefen, wie sich eine
  ausgeblendete Spalte mit **aktivem** Filter heute verhaelt: Filtert die Liste weiter, ohne dass
  der Grund sichtbar ist? Falls ja, aktive Filter ausgeblendeter Spalten als Chip ueber der Liste
  anzeigen — das nimmt dem Benutzer nichts weg und erklaert das Ergebnis. **Erst am Bestand
  pruefen, dann entscheiden.**
- **Druck.** Folgt der Ausdruck der Bildschirmauswahl oder hat er ein festes Layout?
  -> Vorschlag: **festes Druck-Layout**. Der Ausdruck geht an Dritte (Beschichter) und darf nicht
  von der Bildschirmeinstellung eines einzelnen Werkers abhaengen. Wie handhaben es die
  bestehenden Druckansichten?

## Abgrenzung

- **Keine neue Mechanik, keine neue Bibliothek** — ausschliesslich Anschluss an das Bestehende.
- Keine Aenderung an `column-preferences.js` ausser dort, wo eine der neuen Ansichten eine
  Faehigkeit braucht, die noch fehlt (dann gesondert begruenden).

## Ursachenbehebung: ADR 0005 ergaenzen (wichtiger als die Einzelviews)

**Warum es gefehlt hat:** Die Teil-3-Spec hat den ADR-0005-Listenteil verlangt — Pagination,
Filterkarte, Server-Side-Spaltenfilter — die **Spaltenpraeferenzen aber nie erwaehnt**. Der
Dev-Lauf hat exakt gebaut, was dastand, und die Tabelle sogar schon korrekt ausgezeichnet. Das ist
kein Einzelversehen: Es wiederholt sich bei jeder neuen Liste, solange die Vollstaendigkeit nur im
Gedaechtnis einzelner Menschen existiert und nicht im Muster selbst.

**ADR 0005 bekommt die Spaltenpraeferenzen als vierten, verbindlichen Bestandteil:**

> Eine neue Liste ist erst vollstaendig, wenn sie **alle vier** Bestandteile hat:
> (1) Pagination (`PageSize.Resolve` + `PaginationState` + `_Pagination`),
> (2) Filterkarte,
> (3) Server-Side-Spaltenfilter (`data-server-column-filter`, `data-col-key` je `<th>`),
> (4) **Spaltenpraeferenzen** (`view-config` + `column-config` + `column-preferences.js`).

Konkret: den vierten Punkt in die ADR-Checkliste, `column-preferences.js` in die Dateiliste, und
`Views/ProductionOrders/Index.cshtml` als Referenzimplementierung benennen.

**Wirkung:** Jede kuenftige Spec, die „nach ADR 0005" sagt, verlangt damit automatisch auch die
Spaltenauswahl. **Das ist der eigentliche Gewinn dieser Notiz** — die drei fehlenden Bloecke in den
IDEAL-Views sind nur das Symptom, die lueckenhafte Musterbeschreibung war die Ursache.

**Nebenbefund zum Pruefen:** Wenn ADR 0005 ergaenzt wird, lohnt ein Durchgang ueber die
bestehenden Listen — gibt es weitere, die (3) oder (4) nicht haben? Dann faellt der Abgleich
einmalig an statt haeppchenweise.

## Bezug

[[2026-08-12-fa-struktur-darstellung]] (Tree-Table — Vorbedingung fuer die FA-Struktur),
[[2026-07-29-standort-ideal-teil-3-spec]] (gemeinsamer Listen-Baustein).
