---
type: referenz
updated: 2026-07-27
---
# Bekannte Fallstricke

Uebertragen aus CLAUDE.md (Nacherfassung 2026-07-27). Jeder Eintrag nennt die Falle **und** das
Warum — die Begruendung ist der eigentliche Wert, weil sie verhindert, dass jemand die Regel
„wegoptimiert". Vollstaendige Vorfassung: `../../docs/CLAUDE-full-backup-2026-07.md`.

Verwandt: [[0004-migrations-und-sql-disziplin]], [[0005-listen-view-pattern-mit-server-side-spaltenfilter]],
[[0009-app-status-in-satelliten-tabellen-neben-sage-master]], [[0010-aktivitaets-protokoll-mit-isolierten-dbcontexts]].

---

## 1. Fremdsystem-Datenfelder (Sage / OSEON / enaio)

### Artikelnummer vs Ressourcenummer
`Artikelnummer` = Geraete-Artikelnummer, `Ressourcenummer` = Bauteil-Artikelnummer. Fuer
Bauteil-Operationen **immer** `Ressourcenummer`.
**Warum:** Beide Felder stehen nebeneinander in der BOM-Zeile und sehen gleich aus. Wer die
Geraete-Nummer verwendet, bucht Bestand auf das Endprodukt statt auf das Bauteil — ein Fehler,
der erst bei der Inventur auffaellt. Genau das war der Bug vom 16.02.2026:
`PickingItem.BomArticleNumber` speicherte `bom.Artikelnummer`, dadurch fand `TransferPicked` den
Artikel nicht und schrieb **keine** `StockMovement` — die Kommissionierung lief scheinbar durch,
ohne zu buchen. (Nach so einem Fix muessen bestehende `PickingItems` ggf. geloescht werden, damit
sie neu initialisiert werden.) Feldsemantik der View: [[integrationen]].

### BOM-`Menge` ist die Menge in der Baugruppe, nicht im Geraet
**Warum:** Wer `Menge` als Gesamtbedarf liest, kommissioniert bei mehrstufigen Stuecklisten zu
wenig oder zu viel. Die Gesamtmenge ergibt sich erst aus der Multiplikation ueber den Baum.

### Baugruppen-Picking bucht NUR die Baugruppe
Ist eine Zeile eine Baugruppe (`IsBaugruppe`), wird bei der Kommissionierung ausschliesslich auf die
Baugruppe gebucht — die darunterliegenden Teile werden **ignoriert**.
**Warum:** Die Baugruppe ist bereits vormontiert und liegt als eigener Bestand vor. Wuerde man
zusaetzlich die Einzelteile buchen, waere der Bestand doppelt abgebucht. Wer die Picking-Logik
anfasst, muss diese Verzweigung erhalten.

### Sage VB6-Booleans sind BIT mit `-1` fuer TRUE
Sage-Tabellen (`KHKArtikel`, `KHKArtikelvarianten`, …) speichern Boolean-Spalten als BIT, aber mit
`-1` fuer TRUE. Filter immer als `IstBestellartikel = -1 AND Aktiv = -1`.
**Warum:** VB6-Legacy von Sage. `= 1` liefert stillschweigend **null** Zeilen — der Sync laeuft
„erfolgreich" und importiert nichts.

### Artikelgruppe BOM vs Articles
SAGE liefert `"940 - Kleinmaterial"`, `Articles` speichert `"940"`. Beim Matching
`.split(' - ')[0].trim()`.
**Warum:** Zwei Quellen mit unterschiedlicher Formatierung derselben fachlichen Angabe. Ohne
Normalisierung matcht keine Artikelgruppen-Regel (Glas-Filter, Empfaenger-Mapping,
Meldebestand-Farbe).

### OSEON `pa.ID` ist bigint
`long` verwenden, nicht `int`.
**Warum:** Die IDs sind bereits jenseits von `int.MaxValue`; ein `int`-Cast wirft zur Laufzeit
oder trunkiert.

### enaio `object1.id` ist int
`Convert.ToInt64(reader.GetValue(0))` verwenden.
**Warum:** Der Reader liefert den nativen Typ; ein direkter `GetInt64` wirft
`InvalidCastException`.

### enaio DMS-Sync hat kein Delta
Die `angelegt`-Spalte in enaio ist statisch (Bulk-Import 2013). Deshalb Full-Sync statt Delta;
MERGE verhindert Duplikate. `EnaioDmsSyncService.cs` liest ALLE Werkstattauftraege/Zeichnungen
ohne Datumsfilter.
**Warum:** Ein Delta auf `angelegt` wuerde nach dem ersten Lauf nie wieder etwas finden, weil das
Datum sich bei Aenderungen nicht bewegt.

### enaio-Quelle ist die View `vw_IDEAL-AKE_Fertigungsauftraege`
FA-Zuordnung kommt aus dieser View mit `WaNummerTrimmed` als FA-Nummer (frueher `feld44` bzw.
`left(feld43,7)`). `DocumentType` kann `Werkstattauftrag`, `Werkstattauftrag+Zeichnung` oder
`Zeichnung` sein. Icon + Sortierung **zentral**: Partial `Views/Shared/_EnaioDmsBadges.cshtml`,
Sortier-Vorrang in `EnaioDmsDocumentRepository.GetByOrderNumbersAsync`.
**Warum:** Vier Views (ProductionOrders, PickingLeitstand, FaWorklist, FaCompletion) zeigen
dieselben Badges. Inline-Kopien driften garantiert auseinander — dann zeigt eine Liste eine andere
Reihenfolge als die andere, und Anwender halten das fuer fehlende Dokumente.

---

## 2. FA-Status, Satelliten-Tabellen, Vorbau

### `IsDone` vs `IsDonePicking` — die Lese-Seite ist die Falle
`Picking/ToggleDone` schreibt `PickingStatus.IsDonePicking`; Sage-`IsDone` darf die App nie
beschreiben. **Alle** Stellen, die „FA erledigt" anzeigen oder filtern, muessen
`IsDone || IsDonePicking` pruefen — FA-Liste, Leitstand, Picking-Worklist, `FaCompletion/Index`,
`FaWorklist/Index`, und service-seitig `BomCacheSyncService.ReadOpenOrdersInWindowAsync` (raw SQL)
sowie `FaWorkStepDetectionService` (**beide** Kandidaten-Queries).
**Warum:** Weil der Sage-Sync `IsDone` ueberschreiben wuerde, muss der App-Status getrennt liegen
(siehe [[0009-app-status-in-satelliten-tabellen-neben-sage-master]]). Von v1.11 bis v1.21.0 hat
**keine** Query das Flag gelesen — der „Abschliessen"-Button war ueber ein Jahr wirkungslos, und
komm-erledigte FAs belegten die BomCache-Plaetze und verdraengten echte offene FAs aus der
Artikelinfo.

### `.Include(o => o.PickingStatus)` ist Voraussetzung
`ProductionOrderRepository.GetAllOrderedAsync()` muss `PickingStatus` per `.Include` laden.
**Warum:** Ohne Include ist `o.PickingStatus` null, das Flag wird nie ausgewertet, und der Filter
verhaelt sich wie „nie erledigt" — ohne Fehlermeldung.

### `IsCancelled` in ALLEN Offen-Queries
`ProductionOrder.IsCancelled` verhaelt sich wie `IsDone`: verwaiste FAs (in Sage geloescht) werden
storniert und muessen aus offenen Sichten verschwinden. **Jede** Offen-Query fuehrt
`!IsCancelled` — EF **und** raw SQL.
**Warum:** Ein storniertes FA, das weiter in Listen und im BomCache-Fenster steht, verbraucht
Kapazitaet und laesst Werker an Auftraegen arbeiten, die es nicht mehr gibt.

### FA-Reconciliation nie im AgentJob
Das Stornieren verwaister FAs laeuft im Service (`ProductionOrderReconciler.Plan`) mit Guard
(leerer Sage-Read → Skip) und Cap (`ReconcileMaxCancelPerRun` → Skip + Fehlermail), hinter Flag
`Sync:ProductionOrderReconcileEnabled` (Default false).
**Warum:** Ein guard-loses `WHEN NOT MATCHED BY SOURCE` im AgentJob wuerde bei einem leeren oder
teilweisen View-Read **alle** offenen FAs stornieren. Der Guard und der Cap sind genau dieser
Schutz — deshalb liegt die Entscheidung in unit-getestetem C#, nicht in SQL.

### `FaWorkSteps`: `IsRemoved` statt Delete, Detection ist nur-hinzufuegend
Genau eine Zeile je FA+WorkStep (UNIQUE). Manuelle Abwahl setzt `IsRemoved=1`; der Detection-Sync
darf solche Zeilen **nie** re-adden. Manuelles Wieder-Hinzufuegen reaktiviert
(`IsRemoved=0`, `Source='Manual'`).
**Warum:** Manuelle Abwahl ist eine fachliche Entscheidung eines Menschen. Wuerde die Erkennung
sie ueberschreiben, kaeme der abgewaehlte Arbeitsgang bei jedem Sync-Lauf zurueck — der Werker
wuerde gegen die Maschine ankaempfen.

### Zwei Completion-Flags auf `FaWorkStep`: `IsSpecComplete` vs `Status`
`IsSpecComplete` (FA-Vervollstaendigung, „Vollstaendig definiert", Planer) blendet **nichts** aus.
`Status` (`FaWorkStepStatus` Offen/InBearbeitung/Fertig, Abarbeitungsliste, Werker) ist das
**einzige** Flag mit Ausblende-Logik — und nur `Fertig` blendet aus, `InBearbeitung` bleibt
sichtbar. Schreibpfade: `ToggleSpecComplete` → `SetIsSpecCompleteAsync` vs
`/api/fa-work-steps/set-status` → `SetStatusAsync`.
**Warum:** Zwei verschiedene Personen mit zwei verschiedenen Fragen („ist geplant?" vs „ist
gebaut?"). Wuerde das Planer-Flag ausblenden, verschwaende der FA aus der Werker-Liste, bevor
jemand ihn gebaut hat.

### FaWorkStep-Detection haengt nicht am BomCache-ContentHash-Pfad
`FaWorkStepDetectionService` laeuft als **eigener idempotenter Schritt** direkt nach dem
BomCache-Sync (`Sync:FaWorkStepDetectionEnabled`, eigener Protokoll-Eintrag).
**Warum:** Der BomCache-Sync skippt Artikel mit unveraendertem `ContentHash`. Haenge die Erkennung
dort ein, bekaemen neue FAs zu bereits gecachten Artikeln **nie** eine Erkennung.

### „Ohne Treffer" / „nicht im Cache" heisst nicht „Fehler"
Kann am Cache-Fenster oder am Cap liegen (Info bei Detection, **Warning** bei BomCache-Cap).
**Warum:** Wer das als Fehler liest, sucht im Code statt in der Konfiguration.

### Leitstand-Spalten VK–VA sind statisch
Der WorkStep-Katalog ist erweiterbar, der Leitstand zeigt bewusst nur die 5 Spalten
VK/VL/VE/VT/VA (Filter-Keys cooling/fan/electric/doors/superstructure). Neue Katalog-AGs
erscheinen dort **nicht** automatisch.
**Warum:** YAGNI-Entscheid der Spec. Eine sechste Spalte braucht View + ViewModel + Pivot — das
ist bewusst sichtbarer Aufwand, statt eine generische Pivot-Mechanik zu bauen, die niemand
gebraucht hat.

### Leitstand VK–VA zeigen den Erledigt-Status, nicht „anwendbar"
Datenquelle ist `GetWorkStepDetailPivotAsync` (orderId → Code → `FaWorkStepPivotCell`), gesetzt
wird ueber `/api/fa-work-steps/set-status`. Fehlender Code = AG nicht anwendbar = **leere Zelle**.
„Anwendbar" wird im Leitstand nicht mehr gesetzt.
**Warum:** Leitstand und Abarbeitungsliste zeigen dasselbe Flag — sonst haetten Leitstand und
Werkstatt zwei widersprechende Wahrheiten ueber denselben Arbeitsgang.

### FA-Abarbeitungsliste ist arbeitsgang-zentriert, nicht werkbank-zentriert
`FaWorklistController.Index` filtert auf **einen** WorkStep (`?workStepId=`) ueber alle
Werkbaenke; die Werkbank ist ein optionaler Zusatzfilter (`?workbenches=`, Komma-OR-Semantik via
`WorkbenchFilter.Matches`). Vorauswahl aus `User.DefaultWorkStepId` bzw. `User.DefaultWorkbenches`.
**Warum:** Ein Vorbau-Arbeitsgang wird von einem Team gemacht, nicht von einer Werkbank. Das
frueher gepflegte Werkbank-AG-Mapping bleibt Stammdatum, treibt die Liste aber nicht mehr.

### Text-Merkmale gibt es nur bei FaAttributes, nie bei ArticleAttributes
Der Enum `AttributeType` ist zwischen Article- und FA-Merkmalen **geteilt** und hat `Text = 2`,
aber der Freitextwert lebt ausschliesslich in `FaAttributeValue.TextValue`.
`ArticleAttributeValue` hat **kein** TextValue.
**Warum:** Der geteilte Enum verleitet dazu, `Text` auch in der ArticleAttributes-UI anzubieten —
der Wert haette dann nirgends einen Speicherort und ginge still verloren.

### UI-Label „FA-Vorbau-AG", Code bleibt `WorkStep`
Nur Anzeige-Labels wurden umbenannt; Controller, Routen (`/WorkSteps/...`) und Entity bleiben.
BDE- und OSEON-Arbeitsgaenge sind ein **anderer** Kontext und behalten „Arbeitsgang".
**Warum:** Drei verschiedene Dinge hiessen „Arbeitsgang". Das Label trennt sie fuer Anwender,
ohne ein Rename-Grossprojekt durch Code, DB und Routen zu ziehen.

### FA-Zusatzinfos: Auto-Erledigt ist einweg und gecappt
Sage-Status **verpackt/abgeholt** setzt `PickingStatus.IsDonePicking` (nie Sage-`IsDone`).
Statusquelle ist der **frische** `row.Status`, nicht `info.SageStatus`. Status-Rueckfall oeffnet
nicht wieder; manuelles Oeffnen haelt nur bis zum naechsten Lauf. Ueber
`Sync:FaZusatzinfoAutoDoneMaxPerRun` → kein Write + Warn + Fehlermail.
**Warum:** Ein automatischer Zustandswechsel auf Basis eines Fremdsystem-Feldes ist gefaehrlich —
ein View-Defekt koennte hunderte FAs schliessen. Der Cap ist die Reissleine, der Einweg-Charakter
verhindert Ping-Pong zwischen Mensch und Sync.

### Beschichtungstermin: Backward-Compat bei leerer Kategorie
Ist `LackierteilKategorieName` leer, gilt der Beschichtungstermin fuer **alle** Auftraege; ist er
gesetzt, nur fuer FAs mit `HasCoatingParts`. Formel zentral in `CoatingDateCalculator.Compute(...)`.
**Warum:** Vor Einfuehrung der Kategorie galt der Termin generell. Ein Default „nur mit Flag"
haette bestehende Installationen still um alle Beschichtungstermine gebracht.

---

## 3. Views, Forms, JavaScript

### Hidden + Checkbox mit gleichem `name` funktioniert nicht
Loesung: der Hidden-Input traegt den `name`, die Checkbox hat **keinen** `name` und synct per
`onchange`-Handler den Hidden-Wert.
**Warum:** Der Model-Binder nimmt bei zwei Feldern gleichen Namens den ersten Wert — die
Checkbox-Stellung geht verloren. Betrifft u. a. die `/ServiceSettings`-Bool-Toggles.

### `data-col-key` ist Pflicht auf allen `<th>` in filterable-tables
Bei neuen Spalten: Key in `ColumnDefinitions.cs` definieren **und** in der View verwenden.
**Warum:** Filter- und Spalten-Preferences-Logik adressiert Spalten ueber diesen Key. Fehlt er,
ist die Spalte nicht filterbar und nicht konfigurierbar — ohne Fehlermeldung.

### `column-preferences.js` MUSS vor `table-filter.js` eingebunden werden
**Warum:** `column-preferences.js` dispatcht das `column-preferences-ready`-Event, auf das
`table-filter.js` wartet. Umgekehrte Reihenfolge = Event vor dem Listener = stille Fehlfunktion.

### Eine filterbare Tabelle pro gerenderter Seite
`table-filter.js` / `column-preferences.js` sind Single-Table.
**Warum:** Beide Skripte greifen auf „die" Tabelle zu. Zwei filterbare Tabellen auf einer Seite
konkurrieren um denselben Zustand. Deshalb rendert z. B. BdeMasterData pro Tab-Request nur eine.

### `defaultHidden`: gespeicherte Prefs muessen je Spalte gewinnen
`ColumnDef.DefaultHidden` + `defaultHidden` im inline `#column-config`-JSON;
`column-preferences.js` faellt in `buildDefaultSettings()` **und** `mergeWithDefaults()` auf
`visible: !c.defaultHidden` zurueck.
**Warum:** Ein hart auf `true` gesetzter Default im Merge waere der Bug: neue Spalten wuerden bei
jedem User mit gespeicherten Prefs sichtbar aufpoppen.

### Bootstrap-Table-Styles ueberschreiben Custom-CSS
`!important` noetig.
**Warum:** Bootstrap-Selektoren sind spezifischer als die Projekt-Klassen. Betrifft u. a. die
CI-Farbe der Kommissionierungs-Spalte in der FA-Liste.

### BOM-Baum hat Vorrang vor dem Spaltenfilter
In `Bom.cshtml` kombiniert `updateBomVisibility()` Baum-Zustand **und** Filter: eine Zeile ist nur
sichtbar, wenn die Parent-Baugruppe aufgeklappt ist **und** der Filter passt. Auch
`window.setColumnFilter` ist dort ueberschrieben, damit ein Default-Filter aus dem Benutzerprofil
den Baum-Zustand respektiert.
**Warum:** Ohne die Kombination zeigte Expand/Collapse auch nicht-passende Kinder an — der Filter
wirkte scheinbar zufaellig. Wer im BOM Sichtbarkeit anfasst, muss durch diese Funktion gehen und
nicht direkt `style.display` setzen.

### Razor: `v@Namespace.Class` wird als E-Mail geparst
`v@(Namespace.Class)` verwenden.
**Warum:** Razor erkennt `x@y.z` als E-Mail-Adresse und rendert es literal statt als Ausdruck.

### Select2-Textformat
Die API liefert `"ArticleNumber - Description"`; Parsing mit `.split(' - ')[0]` — **kein**
Em-Dash.
**Warum:** Der optisch aehnliche Em-Dash matcht nicht; das Parsing liefert dann den ganzen String
als Artikelnummer.

### QR-Code Komma-Suffix
FA-Nummer kann ein Komma-Suffix tragen → `.split(',')[0]`.
**Warum:** Der ungetrimmte Wert findet keinen FA.

### Scanner-Endlosschleife durch `confirm()`
Nach einem Scan-Fehler kein `confirm()` — Bootstrap-Modal verwenden.
**Warum:** Der Scanner feuert weiter, waehrend der modale Browser-Dialog blockiert; jeder neue
Scan erzeugt einen neuen Dialog.

### iOS Safari + `getUserMedia` nur im synchronen Gesture-Stack
`navigator.mediaDevices.getUserMedia()` direkt im Click-Handler aufrufen, **vor** Modal-Show/await.
Pattern: erst `await requestCameraPermission()` (Pre-Warm im Click), dann Modal + Scanner-Init.
**Warum:** iOS Safari erteilt die Kamera-Permission nur innerhalb der User-Gesture. Nach einem
`await` gilt der Stack als verlassen und die Anfrage wird verweigert.

### Event-Delegation fuer AJAX-nachgeladenen Inhalt
Im OseonIndex-Inline-JS binden Sub-Row-Handler ueber
`document.addEventListener('click', …)` + `e.target.closest('.oseon-tree-sub')`, nicht per
`forEach(row.addEventListener(...))`.
**Warum:** Direkt gebundene Handler erreichen nur die beim Init vorhandenen Zeilen; spaeter per
AJAX eingefuegte Rows bleiben tot.

### Radio-3-State (Doppelklick → None) ist selbstgebaut
In `Details.cshtml` via `mousedown`-Snapshot des `checked`-Status + `click`-Handler, der den Radio
wieder unchecked setzt.
**Warum:** Bootstrap-/HTML-Radios koennen per Design nicht zurueck auf „keine Auswahl".

### Model-Binder: `int[]` **skippt** leere Strings
Leere `name="quantitiesPicked"`-Inputs kommen **nicht** als 0 an — sie verschwinden und
verschieben die Array-Indices. Loesung in `WarehousePicking/Details.cshtml`:
`normalizeEmptyQuantitiesToZero()` vor jedem Submit, `collectProgress()` sendet `"0"`.
**Warum:** Ein paralleler Mapping-Loop (`qtyDict[itemIds[idx]] = quantitiesPicked[idx]`) schreibt
dann Mengen auf die **falschen** Positionen — Datenfehler ohne jede Fehlermeldung.

### Decimal/Culture-Bug in Form-Inputs
`decimal` mit `ToString(InvariantCulture)` aus einer `DECIMAL(18,4)`-Spalte ergibt `"4.0000"`; der
Default-Binder parst mit deutscher Request-Culture → **40000**. Loesung in Lagerbestellungen:
`type="number" step="1"` + `int[]`-Binding.
**Warum:** Punkt ist im Deutschen Tausendertrenner. Bei Mengen-Eingaben also integer verwenden
oder explizit `InvariantCulture` parsen.

### Programmatisches `input.value = …` loest kein `input`-Event aus
Kalender-Klicks, „Filter entfernen" und `window.setColumnFilter` rufen **seit v1.24.0**
`applyColumnFilterNow()` — **kein** synthetisches `input`-Event mehr faken.
**Warum:** Seit v1.24.0 navigiert der Server-Mode erst bei ENTER; ein synthetisches input-Event
wuerde dort gar nichts mehr ausloesen. Der direkte Aufruf wirkt in beiden Modi.

### Server-Filter-Mode: kein clientseitiges `applyFilters()` beim Init
**Warum:** Der Server hat bereits gefiltert und paginiert. Ein zusaetzlicher Client-Filter laeuft
auf DOM-Zellentext und versteckt insbesondere Datumsspalten („dd.MM.yyyy KWxx"), die er nicht
versteht — die Liste erscheint leer, obwohl Daten da sind.

### Android-Softkeyboard: `enterkeyhint="search"` auf Server-Filter-Inputs
Nicht entfernen.
**Warum:** Ohne das Attribut zeigt Android bei mehreren Feldern eine „Weiter"-Taste, die nur ins
naechste Feld springt statt ein `keydown`-Enter zu feuern — Filtern ist auf Android unmoeglich.

### Universal-Filter-Pattern: Getter liefert **gerenderten** Text
ColumnMap-Getter muessen den angezeigten Zellentext liefern (Badges, Ja/Nein, Datumsformate),
Apply **vor** Pagination, `TotalCount` aus der gefilterten Menge. Bei SQL-paginierten Repos
Query **und** Count identisch filtern (Expression-Trees, **kein** `EF.Functions.Like` wegen
InMemory-Tests).
**Warum:** Der Anwender filtert nach dem, was er sieht. Filtert der Server auf den Rohwert, ist
das Ergebnis fuer den Anwender unerklaerlich. Divergierende Query/Count ergeben falsche
Seitenzahlen.

### Pagination-AllCap 5000
`PageSize.Resolve` mapped „Alle" (Sentinel 0) auf `PageSize.AllCap = 5000`;
`PaginationState.IsCappedAtAll` triggert den Banner-Hinweis.
**Warum:** „Alle" muss eine Grenze haben, aber ein **stiller** Cap taeuscht Vollstaendigkeit vor —
deshalb der sichtbare Hinweis.

### Notiz-Autosave vor „Drucken": Tab synchron oeffnen
In `WarehousePicking/Details` wird der Tab synchron mit `about:blank` geoeffnet und **erst nach**
`await saveNotes()` zur Print-URL navigiert.
**Warum:** Ein `window.open` nach einem await gilt als nicht-user-initiiert und wird vom
Popup-Blocker verworfen.

### Lagerbestellungs-Druck: drei Stellen synchron halten
Bei neuen Spalten immer `ColumnDefinitions.WarehousePickingDetails`, das inline `#column-config`
in `Views/WarehousePicking/Details.cshtml` **und** `WarehousePickingPrintLayout.CellText`.
Reihenfolge im Controller: Filtern → Sortieren → sichtbare Spalten. `pos` + `article-number` sind
locked.
**Warum:** Der Druck soll die GUI spiegeln. Die Sortierung repliziert bewusst die
Vergleichslogik aus `table-filter.js` (de-Zahl vs de-String) — sonst druckt das Papier eine andere
Reihenfolge als der Bildschirm, und niemand traut dem Ausdruck mehr.

### BOM-Button fuer Lagerbestellung: eigene Checkbox-Klasse, vor dem read-only-Return
`Views/Picking/Bom.cshtml` nutzt `.warehouse-select` (nicht `.picking-checkbox`) und laeuft im JS
**vor** `if (readOnly) return;`. Der Sticky-Balken `#bomBulkActionBar` bedient beide Bulk-Sets,
`bomSyncBulkBar()` ist null-safe.
**Warum:** Die `.picking-checkbox`-Elemente existieren nur bei `!ReadOnly`. Der
Lagerbestellungs-Button muss aber auch in der read-only Vorbau-BOM greifen.

---

## 4. Authentifizierung, Session, Antiforgery

Siehe auch [[0002-dual-auth-session-login-plus-windows-sso]].

### Windows-Auth und AD-LDAP sind nicht InMemory-testbar
Unit-getestet ist die Entscheidungslogik der `WindowsAutoLoginMiddleware` (via
`IChallengeIssuer`-Abstraktion + Fake-`IUserRepository`) und `UserAgentHelper.IsWindowsDesktop`.
Der echte 401-Negotiate-Handshake und die LDAP-Strecke bleiben Manual-UAT
(`../../docs/TESTSZENARIEN.md` Kap. 40).
**Warum:** Beides braucht eine echte Domaene und den IIS. Gruene Tests sind hier **kein** Beweis,
dass SSO funktioniert.

### `AddNegotiate()` ist unter IIS in-process falsch
Korrekt ist `AddAuthentication(IISServerDefaults.AuthenticationScheme)`. Beide IIS-Auth-Modi
(windows **und** anonymous) muessen aktiv bleiben.
**Warum:** Die IIS-Integration setzt `HttpContext.User` selbst; `AddNegotiate()` ist fuer
Kestrel/HTTP.sys. Anonymous haelt den Formular-Fallback offen — wird es abgeschaltet, sind
Nicht-Domaenen-Geraete ausgesperrt.

### LoginRedirect zaehlt Account-Pfade **einzeln** auf
Die inline LoginRedirect-Middleware in `Program.cs` schliesst **nicht** pauschal `/account/*` aus,
sondern nur `/account/login`, `/account/logout`, `/account/windowslogin` (+ `/api/*`, statische
Pfade, alles mit `.`). Jede neue anonym erreichbare Account-Action muss hier ergaenzt werden.
**Warum:** Genau das war der eigentliche Grund, warum der Button „Mit Windows anmelden" wirkungslos
blieb: der anonyme `GET /Account/WindowsLogin` wurde **vor** dem Action-Aufruf auf
`/Account/Login?returnUrl=…` umgeleitet — die Action, die das ForceSso-Cookie setzt, lief nie.
Symptom im Log: `WindowsLogin 302 → Login 200` ohne dazwischenliegendes `GET /`.

### ForceSso muss **beide** Middleware-Sperren durchhalten
`ForceSsoCookie` ueberschreibt (1) die `NoAutoLogin`-Sperre in `ShouldTryAsync` und (2) den
`AutoLoginTried`-Gate im Challenge-Zweig.
**Warum:** Sonst ist der Button nach einem Logout oder nach einem ersten fehlgeschlagenen
Auto-Versuch wieder wirkungslos. Bei Aenderungen an der Challenge-Bedingung beide mitdenken.

### Kein `AutoLoginTried`-Cookie setzen, wenn nicht gechallenged wird
**Warum:** Bei Nicht-Windows-UA ohne ForceSso wuerde das Cookie einen spaeteren Button-Force
blockieren. Ebenso: `ForceSsoCookie` an **jedem** SSO-Pfad-Ausgang loeschen, sonst bleibt ein
verwaister Force zurueck.

### Logout ohne `[ValidateAntiForgeryToken]` (Mobile-400)
Direkt nach dem SSO wird die Seite unter der Windows-Identitaet gerendert; der Antiforgery-Token
ist daran gebunden. Desktop-Domaenen-Browser senden NTLM beim POST automatisch mit, Mobile-Browser
**nicht** → Token-Identitaet ≠ Request-Identitaet → **400** (danach 405, weil ein Mobile-Reload
den POST zu GET macht).
**Warum:** Der Token laesst sich nicht gleichzeitig fuer Desktop (NTLM-Resend) und Mobile (anonym)
passend binden, solange er an die schwankende Windows-Identitaet koppelt. Logout ist
Low-CSRF-Risk. **Nicht** dasselbe wie der DataProtection-Key-Fall (ephemere Keys nach Recycle →
Token nicht entschluesselbar → auch 400, aber andere Ursache, geloest durch persistente Keys).

### Antiforgery-Wurzel-Fix: Identitaet normalisieren
`NormalizeUserForSession` setzt `HttpContext.User` auf anonym — sobald eine App-Session existiert
**und** auf allen `/account/*`-Pfaden. Danach sind alle Token konsistent anonym gebunden.
**Warum:** Das Problem war nie auf Logout beschraenkt — **jeder** POST unter SSO konnte 400en.
Ungefaehrlich ist das Strippen, weil die App fuer Autorisierung ausschliesslich Session +
`RequireXxx`-Filter nutzt (kein `[Authorize]`, kein `User.IsInRole`). Preis: beim
**Formular**-Login ist `GetWindowsUserName()` `"SYSTEM"` (bewusst akzeptiert).

---

## 5. Windows-Service, Syncs, Protokoll

Siehe auch [[0010-aktivitaets-protokoll-mit-isolierten-dbcontexts]] und
[[0008-servicesettings-db-first-mit-typisiertem-katalog]].

### `SyncLogger` nutzt `IDbContextFactory`, nicht den Scope-DbContext
**Warum:** Diagnose-Logs duerfen nicht in Sync-Transaktionen mitrollen — sonst fehlen sie genau
im Fehlerfall.

### `SyncLog.Timestamp` ist `DateTime.Now`, nicht `UtcNow`
`SyncRun.WriteEntryAsync` setzt den Timestamp nicht explizit; der Model-Default (Lokalzeit)
greift. Die UI zeigt ihn ohne Konversion.
**Warum:** Wer `UtcNow` setzt, produziert Eintraege, die in der DESC-Sortierung zwei Stunden
„frueher" erscheinen und unter aelteren verschwinden — der Bug v1.15.0–v1.15.2 (gefixt in
v1.15.3, Commit `a2a3275`). Bei einer echten Multi-Timezone-Umstellung konsistent umstellen, nicht
halb.

### UI „Aktivitaets-Protokoll" vs Tabelle `SyncLogs`
Bewusste Asymmetrie: UI-Label neu, DB-Tabelle/Klassen/Interfaces behalten `SyncLog*`. Route bleibt
`/SyncLog/Index`, Filter-Parameter `?colf_Service=…`.
**Warum:** Ein Rename durch DB, Code und Routen haette hohen Diff-Aufwand ohne fachlichen Gewinn.

### `ISyncLogger` ist der **letzte** Konstruktor-Parameter (nach `ILogger<T>`)
Und: Connection-String-Validierung liegt **innerhalb** des try-Blocks nach `BeginRunAsync`.
**Warum:** Einheitliche Reihenfolge macht die Services vergleichbar. Liegt die Validierung vor
`BeginRunAsync`, feuert `FinishFailedAsync` bei Config-Fehlern nicht — der Lauf bleibt ewig
„offen".

### Neuer Service-Name muss in `SyncLogServices.All`
**Warum:** Sonst kennt der Protokoll-Filter den Namen nicht und der Lauf ist in der UI nicht
auffindbar.

### Service referenziert das Web-Projekt (seit BDE Phase 2.3)
`IDEALAKEWMSService` referenziert `IdealAkeWms` direkt. Neue Service-Features nutzen den shared
`ApplicationDbContext` und Web-Repositories statt Dapper-Duplikate.
**Warum:** Bis Phase 2.2 gab es zwei Welten (eigene DTOs + raw SQL im Service). Der
BdeAutoPauseWorker brauchte `ApplicationDbContext`, `BdeShiftCalendarService` und den
`BdeBookingStatus`-Enum — Duplizieren haette drei Wahrheiten erzeugt.

### Sync-Bloecke sind einzeln gekapselt (`RunResilientAsync`)
Ein Fehler killt die anderen Syncs nicht mehr.
**Warum:** Vorher liess ein `throw;` in `SyncProductionOrdersAsync` **alle** Folge-Syncs des
Zyklus ausfallen — ein Sage-Hickup legte damit auch OSEON, enaio und die Mails still.

### BDE Auto-Pause setzt `EndedAt` auf das exakte Schichtende
Nicht auf `DateTime.Now`.
**Warum:** Sonst haengt die gebuchte Dauer am Worker-Tick (`Sync:BdeAutoPauseIntervalMinutes`) —
die Buchung waere bis zu einer Stunde zu lang.

### Lagerbestand-Nullsetzen: `sagePresentKeys` aus den **rohen** Zeilen
`sagePresentKeys` wird aus `rawSageRows` **vor** dem Dedup gebaut. `managedStock` muss Netto-0-Paare
und Umbuchungs-Quellseiten-Keys ausfiltern. Guard: leerer Sage-Read → Skip (keine Mail); Cap
`Sync:LagerbestandNullsetzenMaxPerRun` ueberschritten → Skip + Fehlermail, **kein** throw.
**Warum:** Sage liefert 0-Bestand-Zeilen gar nicht mehr — ein reiner Delta-Vergleich wuerde sie
nie auf 0 korrigieren. Baut man die Keys **nach** dem Dedup, wuerden mehrdeutige Duplikat-Paare,
die in Sage sehr wohl existieren, faelschlich genullt. Der Cap schuetzt vor Sage-Teil-Reads.

### `SubOrderNumber` nur schreiben, wenn die Spalte existiert
`SageImportService.SyncProductionOrdersAsync` prueft per `COL_LENGTH`
(`SageProductionOrderSql.BuildUpsert`).
**Warum:** Das IDEAL-Schema hat `SubOrderNumber NOT NULL + UNIQUE`, die andere Linie kennt die
Spalte nicht. Quick-Fix-Charakter: laeuft der Nicht-IDEAL-Import gegen eine IDEAL-DB, matcht der
`OrderNumber`-Upsert-Key ggf. bestehende Zeilen. Sauberer Zielzustand bleibt die
Branch-Zusammenfuehrung.

### Cleanup: Stichtag aus `DateTime.Now`, self-cleaning
`CleanupWorker` (24h-Takt, DryRun-bewusst). `N <= 0` = deaktiviert = **still**, kein
Protokoll-Eintrag. Erst loeschen, dann `FinishSuccess`.
**Warum:** Stichtag konsistent mit der Timestamp-Speicherung (Lokalzeit). Der eigene Lauf-Eintrag
ist neuer als der Stichtag, deshalb loescht er sich nicht selbst.

### Mails immer `multipart/alternative` mit nackter URL im TextBody
`IMailService.SendAsync` nimmt ein optionales `string? textBody` (vor `ct`).
**Warum:** Reines `HtmlBody` fuehrt in Outlook/Copilot zur `[URL]Text`-Rohtext-Darstellung von
`<a>`-Links. Der Plain-Text-Teil braucht die **nackte** URL ohne Markup — Clients linkifizieren
selbst. Moq-Setups auf `SendAsync` brauchen einen zusaetzlichen `It.IsAny<string?>()` vor dem
`CancellationToken`.

---

## 6. BDE-Buchungslogik

### Mehrfach-Buchungs-Regel wird im Service erzwungen, nicht per UNIQUE-Index
Ohne Konfiguration: ein Operator hat eine aktive Buchung, ein Arbeitsgang hat eine aktive
Buchung. `BdeMehrfachBuchungProOperator` und `BdeMehrfachBuchungProArbeitsgang` lockern das
**unabhaengig** voneinander. Die Indexes `IX_BdeBookings_*_Active` sind seit Phase 2.2 nicht mehr
UNIQUE.
**Warum:** Ein UNIQUE-Index kann nicht konfigurierbar sein. Die Regel musste schaltbar werden,
also wanderte sie in den Service.

### `Paused` hat `EndedAt` gesetzt
Fortsetzung erzeugt eine **neue** Buchung mit `ParentBookingId`. Die Cockpit-Query
`WHERE EndedAt IS NULL` zeigt daher nur Running.
**Warum:** Zeit-Split braucht abgeschlossene Intervalle. Wer „pausiert" als offene Buchung
modelliert, kann die Pausendauer nicht herausrechnen.

### `SaveChangesAsync` zwischen Schliessen und Neu-Anlegen
Bei Auto-Close-und-New-Start: Helfer `FinishAndSaveAsync`, eingebettet in
`BeginTransactionAsync()`.
**Warum:** Sonst sieht die DB fuer einen Moment zwei aktive Buchungen und die
Enforcement-Pruefung schlaegt gegen die eigene Aenderung an.

### Deaktivierter Operator mit offener Buchung
Offene Buchungen bleiben sichtbar; der Schichtleiter muss sie manuell schliessen.
**Warum:** Automatisches Schliessen wuerde eine erfundene Endzeit in die Zeitwirtschaft
schreiben.

### BDE-Sperre bei verpackt/abgeholt
Guard `EnsureOrderNotPackedAsync` in `StartPlannedAsync` (nach Werkbank-Gate, **vor** der
Auto-Close-Transition) und `ResumeAsync` (nur wenn `parent.WorkOperationId.HasValue` —
Activity-Resume bleibt frei). Kein Guard in Beenden/Pausieren/Mengen. `IsDoneBde` blockt nichts.
Ohne Satellit keine Sperre. `GetOpenByWorkplaceIdAsync(workplaceId, excludePackedOrders = false)`
— Default false erhaelt Tracking/ByWorkplace, nur der BDE-Aufrufer setzt true.
**Warum:** Wer nicht mehr starten darf, muss eine **laufende** Buchung noch beenden koennen —
sonst haengen Zeiten offen. Der Default-false-Parameter schuetzt die bestehenden Aufrufer;
Moq-Setups muessen beide Argumente explizit angeben.

---

## 7. Lager, Bestand, Bestellwesen

### Kommissionierwagen (`IsPickingTransport`) — wo gefiltert wird und wo nicht
**Gefiltert** in: Stueckliste-Quell-Dropdown, Bestand, Meldebestand-Farbcodierung,
`StockCheckService`. **Nicht gefiltert** in: Bestandsuebersicht-Liste, Bewegungshistorie. Das
Ziel-Dropdown zeigt **nur** Wagen.
**Warum:** Ein Wagen ist Transport, kein Lagerbestand — er darf Verfuegbarkeits- und
Meldebestandsrechnungen nicht verfaelschen. Fuer Nachvollziehbarkeit muss er in Uebersicht und
Historie aber sichtbar bleiben.

### `IsActive` vs `IstBuchbar`
Zwei unabhaengige Flags auf `StorageLocation`. `IsActive` ist Sage-controlled (Phase-1-Sync),
`IstBuchbar` ist user-controlled. Buchungs-Dropdowns filtern auf **beide**;
Bestand-Aggregation und Sage-Korrektur-Buchungen ignorieren `IstBuchbar`. Default: Manual=true,
Sage=false.
**Warum:** Sage entscheidet, ob der Platz existiert; das WMS entscheidet, ob dort gebucht werden
darf. Wuerde die Aggregation `IstBuchbar` beachten, verschwaende vorhandener Bestand aus der
Bilanz.

### Picking Source-Lagerplatz-Fallback ist NAN
Auto-Suggest beruecksichtigt nur Plaetze, die im Dropdown erscheinen koennen (`IstBuchbar=true`,
kein Wagen). Sage-Plaetze mit Bestand aber `IstBuchbar=false` werden **nicht** vorgeschlagen; auch
gespeicherte, inzwischen nicht mehr buchbare `SourceStorageLocationId`s werden ignoriert und
ersetzt.
**Warum:** Ein Vorschlag, den der Anwender im Dropdown nicht auswaehlen kann, ist schlimmer als
keiner.

### Hauptlagerplatz: Lock-Semantik ohne Source-Flag
`SagePrimaryStorageLocation` nicht leer ⇒ Wert stammt aus Sage ⇒ in der App gesperrt (der
Edit-POST ignoriert die eingehende `PrimaryStorageLocationId` **serverseitig**); leer ⇒
app-editierbar. Sync: Sage liefert Wert → Rohcode + FK-Lookup, kein Match → FK null + Warn +
Count `hauptlagerplatz_fehlt`; Sage leer → FK **unberuehrt**.
**Warum:** Der Rohcode ist gleichzeitig der Herkunftsnachweis — ein zusaetzliches Source-Flag
waere redundant und koennte divergieren. „Sage leer lasst FK unberuehrt" bewahrt die manuelle
App-Wahl. Sortierung „Hauptlagerplatz zuerst" liegt zentral in
`StockMovementRepository.GetCurrentStockAsync` / `GetStockByArticleNumbersAsync`; das Badge ⭐
gibt es nur in `StockOverview/Index`.

### `MovementType`-Erweiterung trifft 6 Stellen
Bei jeder neuen `MovementType` die Aggregations-Logik in `StockMovementRepository` (5 Stellen) und
`PickingTransferService` aktualisieren.
**Warum:** Die kollabierten Switches (`Ausbuchung ? -Quantity : Quantity`) behandeln unbekannte
Werte **still falsch** — der Bestand wird lautlos verkehrt gerechnet.

### `PartiallyDelivered` ist kein End-Status
Solche Bestellungen bleiben in `WarehousePicking/Index` bearbeitbar; der Status wird beim erneuten
Close neu abgeleitet. `GetForWarehouseAsync` nimmt ein `WarehouseRequisitionStatus[]` und zeigt
ohne expliziten Filter beide offenen Status.
**Warum:** Restlieferungen sind der Normalfall, nicht die Ausnahme. Ein End-Status haette den
Lager gezwungen, eine neue Bestellung anzulegen.

### `ShortageStatus`-Enum statt `IsFinalShortage`-Bool
`None=0` / `WillBeRestocked=1` / `NoRestock=2`. Order wird `PartiallyDelivered` bei
`WillBeRestocked`, sonst `Closed`. MissingParts-Tabs: `WillBeRestocked` = „Offene Fehlteile",
`NoRestock` = „Wird nicht nachgeliefert".
**Warum:** Ein Bool konnte „kommt nach" nicht von „gar nichts vermerkt" unterscheiden. Die
Migration ist **daten-destruktiv** — `Down()` verliert die Unterscheidung None/WillBeRestocked
(Backup vor Deploy).

### `Note` vs `NoteEinkauf`
Property heisst `Note`, UI-Label aber „Notiz Lager". Die zweite Notiz heisst in Code und UI
`NoteEinkauf` / „Notiz EK". Form-Parameter-Reihenfolge: `notes` vor `notesEinkauf` vor
`shortageStatuses`.
**Warum:** Das Rename Note→NoteLager wurde bewusst **nicht** gemacht (grosser Diff, keine
semantische Notwendigkeit im DB-Layer). Wer den Namen fuer die Einkaufs-Notiz haelt, schreibt in
die falsche Spalte.

### `MissingPartsController` filtert per Default auf eigene Werkbaenke
`mineOnly=true`. Die Lager-Sicht laeuft ueber den separaten `MissingPartsLagerController`. User
ohne Workplace-Zuordnung sehen eine **leere** Liste mit Info-Banner — kein automatischer Fallback
auf alle Fehlteile. Das Werkbank-Dropdown zeigt nur eigene Werkbaenke (`GetByUserIdAsync`).
**Warum:** Ein stiller Fallback auf „alle" haette dem Werker eine fremde Arbeitsliste als seine
eigene praesentiert.

### `ProductionWorkplace.OverridePrePickingDays` ist wirkungslos (Stand 2026-07-27)
Das Feld „Abweichende Vorkommissioniertage" je Werkbank wird gepflegt, gespeichert und in der
Werkbank-Liste angezeigt — aber von **keiner** Terminberechnung gelesen (`BusinessDayService` und
die Termin-Getter kennen es nicht; verifiziert per grep, nur CRUD- und Anzeige-Treffer).
**Warum das gefaehrlich ist:** ein Admin kann pro Werkbank einen Override setzen und erwartet eine
Wirkung auf den Vorkommissioniertermin — es passiert nichts, ohne Fehlermeldung. Entweder das Feld
in `BusinessDayService` einbeziehen oder es im UI als „derzeit ohne Funktion" kennzeichnen. Steht
als offener Punkt in [[feature-map]].

### `StorageLocation.Code`: DB 50 Zeichen, manuell 12
DB-Spalte ist `NVARCHAR(50)`; manuelle Codes bleiben per `IValidatableObject.Validate` auf 12
Zeichen (Barcode-Lesbarkeit), Sage-Codes nutzen den vollen Platz.
**Warum:** Sage-Codes sind laenger als das, was der Handscanner zuverlaessig liest — die
Beschraenkung gilt daher nur fuer selbst angelegte Plaetze.

### Glas-Bestellung: Typ steht nur bei der Anlage fest, Enforcement zweifach
`WarehouseRequisition.Type` (Lager=1/Glas=2) — kein Typwechsel, keine gemischten Auftraege.
Die Artikelsuche filtert per `?type=` **und** die AddItem-API validiert serverseitig. Zentrale
Regel in `GlasArticleGroupFilter` (`NormalizeGroup`, `IsAllowedForType`); `GemeinsameArtikelgruppen`
(Default `EUZ`) immer beidseitig erlaubt.
**Warum:** Client-Filter allein reicht nie — ein manipulierter Request wuerde sonst Glas in eine
Lagerbestellung schmuggeln. **Test-Fallstrick:** `Type` hat `HasDefaultValue(Lager)`; in Tests
nie `(WarehouseRequisitionType)0` setzen, 0 ist kein gueltiger Enum-Wert.

### Feature-Toggle mit Default **true** braucht die richtige Read-Semantik
`LagerbestellungAktiv`: `!string.Equals(raw, "false", OrdinalIgnoreCase)` — null/`"true"` = aktiv,
nur `"false"` sperrt. **Nicht** das `?.Equals("true") == true`-Muster.
**Warum:** Letzteres defaultet auf false und haette alle bestehenden Installationen beim Upgrade
dunkel geschaltet. Siehe [[0011-feature-toggles-ueber-appsettings]].

### Quick-Add aus der BOM: Typ-Ableitung und Redirect-Semantik
`POST /api/warehouserequisitions/quick-add` leitet den Typ aus der Artikelgruppe ab (Glas-Gruppe →
Glas, gemeinsame/EUZ → Lager), legt je Typ **einen** Draft pro Lauf an (offener Draft wird
wiederverwendet), Werkbank = **erste** zugeordnete User-Werkbank (keine → BadRequest). Ungueltige
Items → `skipped`; alles skipped → BadRequest. Genau ein Typ betroffen → Redirect auf
`Edit/{id}`, gemischt → Uebersicht.
**Warum:** Ohne Draft-Wiederverwendung haette jeder Klick eine neue Bestellung erzeugt.

---

## 8. Tests, EF, SQL Server

### InMemory-DB unterstuetzt kein `rowversion`
Tests nutzen `TestApplicationDbContext`.
**Warum:** Der InMemory-Provider kennt keine Concurrency-Token; ohne den Test-Kontext scheitert
jedes `SaveChanges` auf einer Entitaet mit RowVersion.

### InMemory-DB erzwingt keine UNIQUE-Indexe
Duplicate-Detection braucht einen App-Layer-Guard, wenn sie testbar sein soll.
**Warum:** Ein Test, der sich auf den Index verlaesst, ist gruen und die Produktion wirft — oder
umgekehrt.

### SQL Server parst Batches vorab
Tabellen in einem separaten Batch (`GO`) erstellen, `OBJECT_ID`-Guard verwenden.
**Warum:** Der Parser prueft den gesamten Batch, bevor er ihn ausfuehrt — Referenzen auf eine
Tabelle, die im selben Batch erst entsteht, sind Syntaxfehler.

### `EF PendingModelChangesWarning`
Nach Model-Aenderungen immer `dotnet ef migrations add` ausfuehren.
**Warum:** EF vergleicht Modell-Snapshot und Modell beim Start und verweigert sonst den Dienst.

### FreshInstall braucht **zwei** Ergaenzungen pro Migration
(1) Schema-Objekte im konsolidierten Schema, (2) die `MigrationId` im
`__EFMigrationsHistory`-INSERT-Block am Ende.
**Warum:** Fehlt (1), scheitert FreshInstall direkt. Fehlt (2), scheitert der **erste App-Start
danach** — EF replayt die Migration gegen ein Schema, in dem die Objekte bereits existieren. Der
zweite Fall ist der gemeinere, weil er erst beim Kunden auffaellt. Siehe
[[0004-migrations-und-sql-disziplin]].

### `AppSettings` ist **kein** `AuditableEntity`
Nur `Key` (PK), `Value`, `Description`.
**Warum:** Konfigurations-Key-Value-Ablage, keine fachliche Entitaet. Siehe
[[0003-auditableentity-als-entity-basis]].

### `ServiceSettings.GetIntSafeAsync` liest aus der DB, nicht aus `IConfiguration`
In InMemory-Tests fallen Caps deshalb immer auf ihren Default (z. B. 100) — Cap-Tests muessen
entsprechend viele Datensaetze seeden.
**Warum:** Wer den Cap per Testkonfiguration setzen will, wundert sich, dass der Test nicht
greift.

### Raw-SQL-Pfade sind nicht InMemory-testbar
Betrifft u. a. `BomCacheSyncService.ReadOpenOrdersInWindowAsync`, das Reconcile-UPDATE, den
Artikel-/Bestand-Sync und die Protokollzeilen darin. Testbar sind die daneben liegenden
Entscheidungs-Helper (`ProductionOrderReconciler`, `LagerbestandZeroingPlanner`,
`ActivityLogCleanupPlanner`, `BomCacheCoverage`).
**Warum:** Der `bebcca0`-Test prueft z. B. nur die **ungenutzte** EF-Methode und faengt den
raw-SQL-Pfad nicht ab. Gruene Tests sind hier kein Beweis — bei Aenderungen an der Auswahl immer
**beide** Stellen pruefen und Manual-UAT fahren.

### `SyncWorkerTests` sichern nur die Invariante
„Ohne DB laufen true-Default-Gates, false-Default-Gates skippen, kein Crash." Die wertabhaengige
„laeuft-wenn-in-DB-enabled"-Wirkung ist Manual-UAT.
**Warum:** Damit niemand aus gruenen Worker-Tests schliesst, dass die Toggles wirken.
