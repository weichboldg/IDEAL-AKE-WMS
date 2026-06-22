# Lagerbestellungs-Druck spiegelt GUI (Spalten + Sortierung + Filter) — Design

> Status: Entwurf zur Review
> Datum: 2026-06-19
> Branch: `feature/windows-auth-ad-users` (Worktree `.claude/worktrees/missingparts-include-pd`)
> Ziel-Version: v1.23.0 (zusätzliche Changelog-Punkte)

## 1. Kontext & Ziel

Der Druck der Lagerbestellungen (`WarehousePicking/Print/{id}`) rendert heute eine **fixe** Tabelle:
10 hartcodierte Spalten in fester Reihenfolge, Zeilen immer `OrderBy(Position)`. Die zugehörige
Bildschirm-Ansicht (`WarehousePicking/Details/{id}`) wurde mit `ffee40a` filter-/sortierbar
gemacht (Client-Mode `filterable-table`: Klick-Sort + Spaltenfilter), hat aber **keine**
Spalten-Einstellung (Zahnrad/Ein-Ausblenden/Reihenfolge) wie die großen Server-Listen.

**Ziel:** Der Druck soll exakt die in der GUI eingestellte Darstellung übernehmen:
1. **Spalten-Sichtbarkeit + -Reihenfolge** (persistent pro User),
2. **Sortierung** (aktueller Klick-Sort bzw. konfigurierter Default-Sort),
3. **aktive Spaltenfilter** (nur die sichtbaren/gefilterten Zeilen werden gedruckt).

Dafür wird die Details-Tabelle zuerst auf das volle Spalten-Preferences-Muster gehoben
(Zahnrad wie die großen Listen), und der Druck spiegelt diesen Zustand.

## 2. Nicht-Ziele (YAGNI)

- **Keine** Server-Side-Pagination/Server-Column-Filter für die Details-Tabelle. Sie bleibt
  Client-Mode (kleine, vorgefilterte Liste je Bestellung). Nur das **Spalten-Preferences**-Muster
  (Sichtbarkeit/Reihenfolge/Default-Sort) kommt hinzu.
- **Keine** Änderung am Datenmodell (keine Migration). Spalten-Prefs laufen über die bestehende
  `UserViewPreferences`-Tabelle/-API.
- **Keine** Spaltenfilter/Sortierung am Druck, die über das hinausgehen, was die GUI kann.
- **Keine** Änderung an anderen Print-Views (Picking/Print BOM, MissingParts hat keine Print-View).

## 3. Architektur & Datenfluss

### 3.1 Wo der Zustand lebt (Ist-Analyse)
- **Persistent** (Server, `UserViewPreferences` pro UserId+ViewKey, JSON):
  Spalten-Sichtbarkeit, -Reihenfolge, -Breite und ein im Zahnrad konfigurierter Default-Sort
  (`defaultSortColumn`/`defaultSortDirection`). Geschrieben von `column-preferences.js`
  (PUT `/api/user-view-preferences/{viewKey}`), validiert gegen `ColumnDefinitions.GetByViewKey`.
- **Flüchtig** (nur Browser-DOM): Klick-Sort auf Spaltenkopf (`data-sort-dir`) und aktive
  Spaltenfilter-Inputs (beides aus `table-filter.js`, **nicht** persistiert).

### 3.2 Hybrid-Ansatz (gewählt)
- **Spalten (Sichtbarkeit + Reihenfolge):** liest der `Print`-Controller **server-seitig** aus den
  gespeicherten Preferences des aktuellen Users für ViewKey `WarehousePickingDetails`.
- **Sort + Filter (flüchtig):** der „Drucken"-Button hängt den **Live-DOM-Zustand** als
  Query-Parameter an die Print-URL: `?sortCol=<key>&sortDir=asc|desc` und je aktivem Filter
  `?colf_<key>=<wert>` (gleiche Mini-Syntax wie die Server-Listen: OR `,`, NOT `!`).

### 3.3 Controller-Reihenfolge im `Print`
1. Items der Bestellung laden + zu `WarehouseRequisitionDetailItemViewModel` mappen (wie bisher).
2. **Filtern** anhand `colf_*` (in C#, gegen den gerenderten Zellentext je Col-Key).
3. **Sortieren**: Live-`sortCol`/`sortDir` → sonst persistierter `defaultSortColumn` →
   sonst `Position` (asc).
4. **Sichtbare Spalten + Reihenfolge** aus den Preferences bestimmen (Fallback: alle 10 in
   definierter Default-Reihenfolge). Locked-Spalten immer sichtbar.
5. ViewModel an die Print-View: geordnete sichtbare Spalten-Keys + sortierte/gefilterte Items.

### 3.4 Komponenten
| Einheit | Verantwortung | Abhängigkeit |
|--------|---------------|--------------|
| `ColumnDefinitions.WarehousePickingDetails` | View-Key + 10 Spalten-Defs (Labels, Locked-Flags) | — |
| `column-preferences.js` (bestehend) | Zahnrad-UI, Persistenz Sichtbarkeit/Reihenfolge/Default-Sort | UserViewPreferences-API |
| `Details.cshtml` | `data-view-key`, Zahnrad-Markup, JS-Order, Print-Button-Param-Anhang | column-preferences.js, table-filter.js |
| `WarehousePickingController.Print` | Prefs lesen, filtern, sortieren, sichtbare Spalten bauen | `IUserViewPreferenceRepository`, `ColumnFilterHelper` |
| `Print.cshtml` | dynamische Spalten (switch je Col-Key) | ViewModel |
| `WarehousePickingPrintViewModel` | trägt geordnete Spalten-Keys + Items | — |

### 3.5 Col-Key → Property / Render-Mapping (Single Source)
Ein zentrales Mapping (im Controller oder Helper) ordnet jedem Col-Key zu:
- **Sortwert** (typisiert wo sinnvoll: `pos`/`requested`/`picked` numerisch; Rest String),
- **Filter-/Render-Text** (gerenderter Zellentext, wie die Server-Listen ihn liefern —
  z. B. `shortage` → "Fehlteil"/"Wird nicht nachgeliefert"/""),
- **Header-Label** (deckungsgleich mit `ColumnDefinitions`).

| Col-Key | Header | Sortwert-Typ | Render-Text |
|---------|--------|--------------|-------------|
| `pos` | Pos | int | Position |
| `article-number` | Artikel-Nr | string | ArticleNumber |
| `description` | Bezeichnung | string | ArticleDescription |
| `requested` | Bestellt | int | gerundete Menge |
| `picked` | Ist | int (nullable) | gerundete Menge oder "" |
| `unit` | ME | string | Unit |
| `storage` | Lagerplatz | string | StorageLocations |
| `note-lager` | Notiz Lager | string | Note |
| `note-ek` | Notiz EK | string | NoteEinkauf |
| `shortage` | Fehlteil | enum | "Fehlteil" / "Wird nicht nachgeliefert" / "" |

> Der Klick-Sort in `table-filter.js` sortiert auf den DOM-Zellentext. Im Plan wird `sortTable`
> kurz geprüft, ob es numerisch oder lexikalisch vergleicht, damit die C#-Sortierung für die
> numerischen Spalten (`pos`/`requested`/`picked`) dasselbe Ergebnis liefert wie der Bildschirm.

## 4. Edge Cases & Fehlerverhalten
- **Keine Prefs / neuer User:** alle 10 Spalten in Default-Reihenfolge, Sort = `Position`.
- **Kein `sortCol`** (kein Klick-Sort): persistierter `defaultSortColumn` → sonst `Position`.
- **Ungültiger `sortCol`/`colf_<key>`** (unbekannter Key): ignorieren, Fallback greift.
- **Filter ergibt 0 Zeilen:** leere Tabelle drucken (konsistent zur GUI).
- **Locked-Spalten** (`pos`, `article-number`): immer sichtbar, auch wenn Prefs sie (fehlerhaft)
  als hidden führten.
- **Print bleibt** wie bisher per `[RequirePickingOrStockOrLagerbestellungAccess]`/aktuellem
  Filter erreichbar (kein Zugriffswechsel).

## 5. Tests
**Unit (xUnit, server-seitig):**
- `Print` mit gemocktem `IUserViewPreferenceRepository`:
  - Prefs mit umsortierten + teilweise versteckten Spalten → ViewModel liefert genau diese
    sichtbaren Keys in dieser Reihenfolge (Locked immer dabei).
  - `sortCol=storage&sortDir=desc` → Items entsprechend sortiert.
  - `colf_article-number=ABC` → nur passende Items.
  - Keine Prefs → alle Spalten, Default-Reihenfolge, Sort=Position.
  - `sortCol` leer + persistierter Default-Sort → Default greift.
- Col-Key→Sort/Filter-Mapping: numerische vs. textuelle Spalten korrekt.

**Manuell (TESTSZENARIEN, neues Kapitel):**
- Zahnrad: Spalte ausblenden + umordnen → Drucken zeigt genau dieses Layout.
- Klick-Sort auf „Lagerplatz" desc → Drucken in dieser Reihenfolge.
- Spaltenfilter setzen → Drucken nur der gefilterten Zeilen.
- Default-Zustand (keine Prefs, kein Sort/Filter) → Drucken wie bisher (alle Spalten, nach Pos).

## 6. Doku & Versionierung
- `AppVersion` (Web + Service) + `Changelog.cshtml` v1.23.0 um Bullet ergänzen.
- `CLAUDE.md`: WarehousePickingDetails in die „server-filter/prefs"-Liste bzw. Pagination-Abschnitt
  aufnehmen (Client-Mode + Spalten-Prefs + Print-Spiegelung als Sonderfall dokumentieren).
- `docs/TESTSZENARIEN.md`: neues Kapitel.
- `PROJECT_STATUS.md`: kurzer Eintrag.

## 7. Offene Punkte / Risiken
- `column-preferences.js` lief bisher nur auf Server-Mode-Tabellen; die Details-Tabelle ist
  Client-Mode. Das JS arbeitet rein auf dem DOM (per `data-col-key`) und ist voraussichtlich
  mode-agnostisch. **Im Plan zu verifizieren:** kein Konflikt zwischen column-preferences
  (Reorder/Hide) und dem Client-Sort/-Filter aus `table-filter.js` (Init-Reihenfolge:
  column-preferences.js VOR table-filter.js).
- Persistenz-Format: Clientseitig sendet `column-preferences.js` den Body als JSON-String-Literal
  (`JSON.stringify(settingsJsonString)`), damit `[FromBody] string` bindet. Die DB-Spalte
  `UserViewPreferences.SettingsJson` enthält daher das **einfache** Settings-JSON
  (`{"columns":[...],"defaultSortColumn":...}`). Server-seitig im `Print` genügt **ein**
  `JsonSerializer.Deserialize<…>(pref.SettingsJson)` — Property-Namen via
  `JsonNamingPolicy.CamelCase` (Keys: `columns[].key/visible/order`, `defaultSortColumn`,
  `defaultSortDirection`). Im Plan wird das genaue JSON-Schema aus `column-preferences.js`
  (Funktionen `saveSettings`/`buildDefaultSettings`) verifiziert und 1:1 nachgebildet.
