---
type: spec
title: Scan-Handler fuer Hardware-Barcodescanner (Tastatur-Eingabe mit Praefix)
slug: 2026-10-15-scan-handler-hardware-scanner-spec
status: Entwurf
created: 2026-10-15
updated: 2026-10-15
source_backlog: "[[2026-10-15-scan-handler-hardware-scanner]]"
task: ""
worktree: ""
branch: ""
affected_code:
  - IdealAkeWms/wwwroot/js/hardware-scanner.js (NEU - reiner Kern + DOM-Anbindung, im _Layout geladen)
  - IdealAkeWms/wwwroot/js/barcode-scanner.js (initScanner Z. 8-18, initTextInputScanner Z. 391-405 - Ziel-Register + Listener auf das Scan-Ereignis; processScannedValue Z. 269-348 und Wrapper Z. 410-426 UNVERAENDERT)
  - IdealAkeWms/Views/Shared/_Layout.cshtml (Skriptblock Z. 412-420, data-Attribute am body)
  - IdealAkeWms/Models/AppSettingKeys.cs (3 neue Keys, kein Seed)
  - IdealAkeWms/Models/Standort/StandortSettingsCatalog.cs (Felder + Gruppe)
  - IdealAkeWms/Views/Account/Profile.cshtml (Geraete-Schalter, nur localStorage)
  - IdealAkeWms/Views/Help/Index.cshtml (Scanner-Abschnitt um Z. 90)
  - IdealAkeWms.Tests/Js/hardware-scanner.check.js (NEU) + Tests-Klasse mit SkippableFact (Muster ColumnFilterSyntaxTests.cs Z. 96-120)
  - README.md (AppSettings-Tabelle), docs/TESTSZENARIEN.md (TS-94)
open_questions:
  - "Umsetzungsort: eigener Worktree ab main NACH dem Merge des IDEAL-Buendels (Empfehlung) oder im Buendel-Worktree 2026-08-07-ideal-teile-1-5?"
  - "Welche Geraete/Scanner-Software (Hersteller, Modell, z. B. Zebra DataWedge, Honeywell, Urovo)? Sind AIM-Kennung, eigener Praefix, Enter-Suffix, Tastaturlayout und Ausgabe als Tasten-Ereignisse einstellbar?"
  - "Praefix-Variante A (AIM-Symbologie-Kennung, Empfehlung) oder B (eigener fester Praefix, Vorschlag ~09)?"
  - "Welche Codes werden gescannt (Code 128 Lagerplatz/HauptFA/Beschichtungsauftrag, Artikel-QR, EAN)? EAN hat das WMS nicht - EAN in dieser Spec ausklammern (Empfehlung) und Sage-Pruefabfrage laufen lassen?"
  - "Einstellungs-Ebenen: Standort-Hauptschalter + Geraete-Abschalter ohne Benutzerfeld (Empfehlung, keine Migration) oder zusaetzlich pro Benutzer (Migration)?"
  - "Ziel-Regel ohne Fokus bei mehreren Scan-Zielen und Umfang: alle 14 Seiten mit Kamera-Scan + BDE ueber die Fokus-Regel in einem Lauf (Empfehlung) statt Seiten-Reihenfolge?"
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

# Scan-Handler fuer Hardware-Barcodescanner

> [!info] Code-Grundlage
> Alle Datei:Zeile-Angaben gemessen im Buendel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`
> (HEAD `2524bb03`, v1.60.0), nur gelesen. Abweichungen zu `main` stehen in Rueckfrage 1.
> Standorte: **AKE und IDEAL** gleichermassen - die Scan-Seiten und `barcode-scanner.js` sind
> standortneutral, keine Verzweigung nach `ProduktionsauftragHierarchisch`.

## Ziel / Nutzen (das Warum)

Mobile Geraete mit eingebautem Scanner (Android-Handy, Handheld) lesen Codes schneller und
verlaesslicher als die Kamera ueber `html5-qrcode`. Der Scanner sendet den Code wie eine Tastatur.
Heute landet diese Eingabe nur dort sinnvoll, wo zufaellig ein Textfeld den Fokus hat; die
Kamera-Ziele (Select2-Artikel, Lagerplatz-Select, versteckte Felder wie `bomScanArticle`) erreicht
sie gar nicht. Ein seitenweiter Listener, der Scanner-Eingaben am Praefix erkennt, abfaengt und an
**dieselbe Auswertung wie der Kamera-Scan** gibt, macht den Scanner auf allen Scan-Seiten nutzbar,
ohne jede Seite umzubauen.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Neues Skript `wwwroot/js/hardware-scanner.js`, im `_Layout` geladen (nur wenn der Standort-Schalter an ist):
  Praefix-Erkennung, Puffern/Abfangen, Ende per Enter/Tab oder Zeitgrenze, Schutz gegen Fehlerkennung,
  Weitergabe als ein Ereignis.
- `barcode-scanner.js`: Scan-Ziele, die `initScanner`/`initTextInputScanner` ohnehin anlegen, werden
  registriert; ein Hardware-Scan laeuft durch dieselbe `processScannedValue`-Kette wie der Kamera-Scan.
- Verhalten je Seitentyp (Ziel / Fokus im Feld / BDE / kein Ziel), sichtbarer Hinweis.
- Einstellungen Standort (AppSettings, Standorteinstellungen-Maske) + Geraet (localStorage).
- Node-Selbstpruefung des reinen Kerns, TS-94, Hilfe, README.

**Out-of-Scope**
- Globale Aktionen (FA scannen -> FA oeffnen, Lagerplatz -> Bestand) - Ausblick, eigener Backlog.
- EAN-Unterstuetzung im Artikelstamm (siehe Rueckfrage 4).
- Aenderung von `processScannedValue`-Erkennungsregeln, Kamera-Modal, `bde-terminal.js`-Logik.
- Kamera-Bibliothek vereinheitlichen (CDN vs. lokal, siehe Ist-Stand) - Befund, kein Umfang.

## Ist-Stand (belegt)

### `barcode-scanner.js` (Kamera-Scan)
- Kamera-Bibliothek `html5-qrcode`; Formate `QR_CODE, CODE_128, CODE_39, EAN_13, EAN_8, CODE_93`
  (`barcode-scanner.js:162-169`). Eingebunden je Seite, teils per CDN
  `unpkg.com/html5-qrcode@2.3.8` (10 Views), teils lokal `~/lib/html5-qrcode/` (OseonIndex:233,
  StockMovements/Index:159, InboundBulk:136).
- `initScanner(buttonId, targetSelectId, scanType)` (`:8-18`): liest `data-qr-fa-enabled` und
  `data-fa-target` vom Knopf, bindet Klick -> `openScannerModal`.
- `processScannedValue(value, targetSelectId, scanType, qrFaEnabled, faTargetId)` (`:269-348`),
  **exakte Erkennungsregeln**:
  - `scanType === 'productionOrder'`: `value.split(';')`; bei >= 3 Teilen und nicht-leerem Teil 3 ->
    `parts[2].trim().split(',')[0]`, sonst `value.trim().split(',')[0]` (`:277-284`).
  - `scanType === 'article'`: `parts[0].trim()` (Teil vor dem ersten `;`); FA aus `parts[2]` nur wenn
    `qrFaEnabled` und >= 3 Teile (`:285-293`).
  - `storageLocation` (und jeder andere Typ): Rohwert unveraendert (`:270`).
  - Danach nach Ziel-Element: `INPUT` -> Wert setzen + `change`, Rueckmeldung nur wenn nicht hidden
    (`:300-308`); Select2-Artikel -> `GET /api/articles/by-number/{nr}` (`:311-330`,
    `ArticlesApiController.cs:57-68`, Treffer nur ueber `GetByArticleNumberAsync`); natives `<select>` ->
    erste Option, deren Text gleich ist oder mit dem Wert **beginnt** (`:333-347`).
  - Es gibt **keine** Inhaltserkennung „ist das ein Artikel/Lagerplatz/FA?" - die Bedeutung kommt
    allein aus dem `scanType` des Ziels. Ein Artikel-QR hat das Format
    `Artikelnummer;Feld2;FA-Nummer[,Suffix];...` (Kommentar `:274`); erzeugt wird er nicht im WMS
    (Suche `QRCoder|ZXing|QR` in `IdealAkeWms/**/*.cs`: kein QR-Erzeuger; einzige Barcode-Bibliothek
    `BarcodeLib` 3.1.5, `IdealAkeWms.csproj:10`).
- `showScanFeedback(element, success, value)` (`:350-370`): haengt an `element.parentNode` ein
  `<small>` mit Zeichen ✓/✗ + Text „Gescannt:"/„Nicht gefunden:" an, entfernt nach 5 s. Farbe ist
  nicht alleiniger Traeger (Zeichen + Wort).
- `initTextInputScanner(buttonId, targetInputId, valueExtractor, onScanned)` (`:391-405`): setzt
  beim Klick `_textInputScanCallback`; der Wrapper um `processScannedValue` (`:410-426`) feuert danach
  `input` + `onScanned(value)`. Der Wrapper ist eine **Neuzuweisung** der globalen Funktion - wer
  `processScannedValue` ruft, muss die globale (gewrappte) nehmen.
- Auto-Init bei `DOMContentLoaded` fuer vier feste IDs (`:429-434`); `initScanner` kehrt ohne Knopf
  sofort zurueck (`:10`). Doppel-Laden des Skripts wuerde doppelt binden (Warnkommentar
  `InboundBulk.cshtml:291`) - das Skript darf daher **nicht** zusaetzlich ins `_Layout`.

### Einbindungen (14 Views, gleiche Liste in main)

| View (Script-Zeile) | Ziel-Feld (Typ) | Init |
|---|---|---|
| StockMovements/Inbound (:104) | `ArticleId` (article), `StorageLocationId` (storageLocation) | Auto-Init `:430-431` |
| StockMovements/Outbound (:77) | `ArticleId`, `StorageLocationId` | Auto-Init |
| StockMovements/Transfer (:94) | `ArticleId`, `SourceStorageLocationId`, `StorageLocationId` | Auto-Init `:430-432` |
| StockMovements/OutboundAll (:116) | `StorageLocationId` | Auto-Init |
| StockMovements/InboundBulk (:137) | `StorageLocationId`; `bulkScanBuffer` (article, Text) | Auto-Init + `initTextInputScanner` `:293` |
| StockMovements/LocationTransfer (:125) | `SourceStorageLocationId`, `TargetStorageLocationId` | `:128-129` |
| StockMovements/Index (:160) | `filterProductionOrder` (fa, Text) | `:167` |
| StockOverview/Index (:160) | `filterStorageLocationId`, `filterArticle`, `filterProductionOrder` | `:163-165` |
| ProductionOrders/Index (:274) | `filterOrderNumber` (fa), `filterArticleNumber` (article) | `:286`, `:292` |
| PickingLeitstand/Index (:429) | `filterOrderNumber` (fa), `filterArticleNumber` (article) | `:487`, `:493` |
| Tracking/OseonIndex (:234) | `filterArticle` (article), `filterCustomerOrder` (fa) | `:311`, `:320` |
| Picking/Bom (:698) | `TargetStorageLocationId` (storageLocation), `bomScanArticle` (article, **hidden**) | `:713-714`, nur `!ReadOnly` |
| Articles/Info (:222) | `articleNumber` (article) | Auto-Init `:433` |
| (Home `_ArtikelinfoTile.cshtml:2`) | kein Scanner - nur Link auf Articles/Info | - |

Die Notiz nennt „Home (Artikelinfo-Kachel)" als Einbindung - das ist nur ein Link (Abweichung).

### BDE-Terminal
- `bde-terminal.js` laedt **nicht** `barcode-scanner.js` (Suche `barcode-scanner` in
  `Views/BdeTerminal/`: kein Treffer). Die „Scannen"-Knoepfe `btnScanOperator`/`btnScanFaAg`
  (`Views/BdeTerminal/Index.cshtml:48`, `:77`) loesen nur die Auswertung des Feldinhalts aus - kein Kamera-Scan.
- `scanOperator` (`Index.cshtml:47`, `autofocus`) und `scanFaAg` (`:76`): `keydown` Enter ->
  `preventDefault` + Auswertung (`bde-terminal.js:88-90`, `:153-155`). Ein Tastatur-Scanner mit
  Enter-Suffix funktioniert dort heute, wenn das Feld den Fokus hat (Notiz bestaetigt).
- Die Seite nutzt das `_Layout` (kein `Layout = null`; Suche `Layout\s*=` in Views: nur Login und
  Print-Views setzen `null`).

### Barcode-Erzeugung (Code 128)
- `BarcodeService.EncodeCode128Base64` (`Services/BarcodeService.cs:21-27`), Singleton (`Program.cs:121`):
  HauptFA auf Kommissionierlisten (`FaHierarchyKommissionierListenController.cs:157/187/217/246`) und
  Beschichtung je HauptFA (`FaHierarchyBeschichtungController.cs:103/130/190/217`),
  Beschichtungsauftragsnummer (`BeschichtungsauftraegeController.cs:281`).
- Lagerplatz-Etiketten: eigener Code-128-Erzeuger `StorageLocationsController.cs:188-193`, Inhalt
  `BarcodeValue ?? Code` (`:165`, `:179`).
- Inhalte sind reine Ziffern/Codes **ohne** eigenes Kennzeichen; HauptFA und Lagerplatz sind am Inhalt
  nicht sicher unterscheidbar.

### EAN / GTIN
- Im WMS gibt es **keine** EAN: `Article` hat `ArticleNumber, Description, Matchcode, Unit,
  ArticleGroup, ReorderLevel, ...` (`Models/Article.cs:24-58`); Suche `\bEAN|GTIN` in `*.cs`/`*.sql`
  ausser `lib/`/Migrations: nur die Kamera-Formatliste (`barcode-scanner.js:166`), Hilfe
  (`Help/Index.cshtml:90`) und README:243. Ein EAN-Scan endet daher heute wie morgen in
  „Nicht gefunden".
- Ob Sage eine EAN fuehrt, ist **unverifiziert**. Pruefabfrage fuer Gerald (je Sage-Mandanten-DB;
  AKE-DB heisst `ake` laut `SQL/AgentJobs/02_Import_Artikel.sql:39`, IDEAL-DB-Name einsetzen):

```sql
SELECT c.TABLE_NAME, c.COLUMN_NAME, c.DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS c
WHERE c.TABLE_NAME LIKE 'KHKArtikel%'
  AND (c.COLUMN_NAME LIKE '%EAN%' OR c.COLUMN_NAME LIKE '%GTIN%'
       OR c.COLUMN_NAME LIKE '%Barcode%' OR c.COLUMN_NAME LIKE '%Strichcode%');
-- je Treffer: Befuellungsgrad
-- SELECT COUNT(*) AS gesamt, COUNT(NULLIF(LTRIM(RTRIM(<Spalte>)), '')) AS befuellt FROM [dbo].[<Tabelle>];
```

### Einstellungs-Muster
- AppSettings-Keys ohne Seed/Migration: Definitionsort `Models/AppSettingKeys.cs` (z. B.
  `BriefkopfVorlagePfad` `:77-80`: „Kein Seed, keine Migration - die Zeile entsteht beim ersten
  Speichern"). Gepflegt in der kuratierten Maske `StandortSettingsCatalog.Fields`
  (`Models/Standort/StandortSettingsCatalog.cs:34-62`), Controller `[RequireAdminAccess]`
  (`StandortEinstellungenController.cs:20`). Lesen im Layout ist vorhanden: `_Layout.cshtml:2`
  injiziert `IAppSettingRepository` (gecacht, `Program.cs:80` `CachedSettingRepository`).
- Benutzerfelder: `User.RecursiveFilterSearch` (`Models/User.cs:37-38`) zieht sich durch
  `ProfileViewModel`, `UserEditViewModel`, `AccountController.cs:169/207`,
  `UsersController.cs:123/254/297`, `Profile.cshtml:61`, `Users/Edit.cshtml:123` - ein neues
  Benutzerfeld kostet diese 8 Stellen **plus** Migration.

### Test-Vorbild Node
- `IdealAkeWms.Tests/Js/column-filter-syntax.check.js` (reines `assert`, Modul per `require` aus
  `argv[2]`), gestartet aus `ColumnFilterSyntaxTests.Kern_NodeSelbstpruefung_GleicheTabelle`
  (`IdealAkeWms.Tests/Services/ColumnFilterSyntaxTests.cs:96-120`) als `[SkippableFact]`: ohne Node
  `Skip.If(...)`. Der JS-Kern exportiert dual: `if (typeof module !== 'undefined' && module.exports)
  module.exports = api; else root.ColumnFilterSyntax = api;` (`column-filter-syntax.js:229`).

## Fachliche Anforderungen

1. **Erkennung:** Eine Tastenfolge gilt als Scan, wenn sie mit dem Praefix beginnt und alle Zeichen
   im Abstand <= `HardwareScannerMaxZeichenabstandMs` (Default 50 ms) folgen.
2. **Abfangen:** Ab dem ersten Praefix-Zeichen werden Zeichen gepuffert (`preventDefault` +
   `stopImmediatePropagation` im Capture-Handler auf `window`), sie erscheinen nicht im fokussierten
   Feld. Bestaetigt sich der Praefix nicht (falsches Folgezeichen oder Zeitgrenze), werden die
   gepufferten Zeichen in das fokussierte Text-/Textarea-Feld nachgetragen (`setRangeText` +
   `input`-Ereignis) - der Mensch merkt hoechstens die Verzoegerung von <= 50 ms.
3. **Ende:** Enter oder Tab waehrend eines Scans beenden ihn und werden verschluckt (kein
   Formular-Submit, kein Fokuswechsel); ohne Suffix endet der Scan, wenn die Zeitgrenze ohne neues
   Zeichen ablaeuft. Kein eigener Suffix-Schalter (Abweichung von der Notiz `Scan:Suffix`).
4. **Weitergabe:** Code (ohne Praefix, bei AIM ohne die 3-stellige Kennung) + ggf. Symbologie gehen
   als **ein** Ereignis an die Seite; ausgewertet wird ausschliesslich ueber `processScannedValue`
   (keine zweite Auswertung).
5. **Verhalten je Seitentyp** (Reihenfolge = Vorrang):
   - (a) **Fokus in einem registrierten Scan-Ziel** (Ziel-Element, sein Scan-Knopf oder ein Element in
     derselben `.input-group`, z. B. Select2-Container) -> dieses Ziel, Rueckmeldung `showScanFeedback`
     wie beim Kamera-Scan; bei `initTextInputScanner`-Zielen feuert der `onScanned`-Callback wie beim Kamera-Scan.
   - (b) **Fokus in einem anderen Text-Eingabefeld/Textarea** (nicht `type=password`) -> Code ohne
     Praefix ins Feld (an Cursorposition), `input` + `change`; endete der Scan mit Enter, wird
     anschliessend ein synthetisches `keydown` Enter am Feld ausgeloest, damit seitenspezifische
     Enter-Handler laufen (BDE `bde-terminal.js:88/:153`, ENTER-Spaltenfilter). Ein nativer
     Formular-Submit entsteht dadurch nicht.
   - (c) **BDE-Terminal:** keine Sonderlogik - es greift (b); `bde-terminal.js` bleibt unveraendert.
   - (d) **Kein Fokus-Ziel** -> registrierte Ziele der Seite: genau eines -> dieses; mehrere -> per
     AIM-Kennung eingrenzen (Q = QR -> `article`, E = EAN -> `article`, C/A/G = Code 128/39/93 ->
     `storageLocation`/`productionOrder`), bleibt genau eines -> dieses; sonst **sichtbarer Hinweis**
     „Code <Wert> gescannt – bitte zuerst das Feld antippen, in das er soll" bzw. ohne jedes Ziel
     „Code <Wert> gescannt – auf dieser Seite nicht verwendbar".
6. **Passwortfelder** werden nie abgefangen (Puffer wird nicht begonnen, wenn
   `document.activeElement` ein `input[type=password]` ist). Login-Seite hat kein Layout
   (`Account/Login.cshtml:4`) und damit keinen Listener.
7. **Hinweis-Darstellung:** eigenes kleines Element (fixiert oben), `role="status"`
   `aria-live="polite"`, Zeichen + Text (nicht nur Farbe), Bootstrap `alert-warning` mit dunklem Text
   (WCAG AA), verschwindet nach 5 s wie `showScanFeedback`; Wert per `textContent` (kein innerHTML).
8. **Einstellungen:** Standort-Hauptschalter (Default aus) + Praefix + Zeichenabstand in den
   Standorteinstellungen; Geraete-Abschalter in „Mein Profil" (localStorage). Hierarchie siehe Entwurf.

## Technischer Loesungsentwurf

**Muster:** Feature-Toggle ueber AppSettings, Default aus ([[0011-feature-toggles-ueber-appsettings]]);
reiner JS-Kern mit Node-Selbstpruefung wie die Suchsyntax; Wiederverwendung von `processScannedValue`
und `showScanFeedback` (ponytail-Sprosse 2).

### 1. `hardware-scanner.js` - zwei Teile in einer Datei
- **Reiner Kern** `HwScanCore` (keine DOM-Zugriffe, dualer Export wie `column-filter-syntax.js:229`):
  ```
  HwScanCore.create({ mode: 'aim' | 'fest', prefix: '~09', maxGapMs: 50 })
    .key(k, t)  -> { action: 'pass' | 'hold' | 'swallow', replay?: string, scan?: { code, symbology } }
    .tick(t)    -> { replay?: string, scan?: { code, symbology } }     // Zeitgrenze
  ```
  `k` = `KeyboardEvent.key` (ein Zeichen, `'Enter'`, `'Tab'`); `t` = `event.timeStamp`. Zustaende:
  Leerlauf -> Praefix-Kandidat -> Scan -> Ende. AIM-Modus: Praefix = `]` + 2 Zeichen
  (`[A-Za-z][0-9A-Za-z]`), Kennung wird als `symbology` geliefert und vom Code entfernt.
  Fest-Modus: Literal-Praefix. **Nur die in Rueckfrage 3 gewaehlte Variante bauen.**
- **DOM-Anbindung:** `window.addEventListener('keydown', h, true)` (Capture, vor Seiten-Handlern).
  Zeichen erkennen: `e.key.length === 1`; **AltGr beachten** - auf deutschem Layout kommt `]` als
  AltGr+9 mit `ctrlKey && altKey` (Windows); Filter darf solche Tasten nicht als Tastenkuerzel
  verwerfen (`e.getModifierState('AltGraph')`). `e.isComposing` oder `e.key === 'Unidentified'`/
  `'Process'` (keyCode 229, IME) -> `pass` (nicht abfangbar, siehe Risiken).
  Bei `scan`: `document.dispatchEvent(new CustomEvent('wms:hardware-scan', { detail: { code,
  symbology, endedWithEnter, handled: false } }))`; danach - wenn `detail.handled` false - Regel (b),
  sonst Regel (d)-Hinweis.
- Konfiguration aus `<body data-hw-scan-praefix="..." data-hw-scan-modus="..." data-hw-scan-abstand="...">`.

### 2. `barcode-scanner.js` - Ziel-Register (kleine Ergaenzung)
- `initScanner` und `initTextInputScanner` legen nach erfolgreichem Binden einen Eintrag in einem
  Modul-Array ab (`{ buttonId, targetId, scanType, qrFaEnabled, faTargetId, onScanned }`).
- Ein Listener auf `wms:hardware-scan` waehlt das Ziel nach Regel (a)/(d), setzt bei
  Text-Input-Zielen `_textInputScanCallback` (wie der Klick-Handler `:398-402`) und ruft die **globale**
  `processScannedValue(...)` (damit der Wrapper `:411` greift). Danach `detail.handled = true`.
  Faellt der Fokus nicht auf ein Ziel und ist das fokussierte Element ein freies Textfeld, bleibt
  `handled` false -> Regel (b) im Layout-Skript.
- Kamera-Pfad bleibt unveraendert.

### 3. `_Layout.cshtml`
- Liest `HardwareScannerAktiv` per vorhandenem `AppSettings`-Inject (Muster `:38`), nur bei `"true"`:
  body-data-Attribute + `<script src="~/js/hardware-scanner.js" asp-append-version="true">` **vor**
  `RenderSectionAsync("Scripts")` (`:420`), damit das Ereignis-Protokoll steht, bevor Seiten-Skripte laufen.
- Geraete-Abschalter: Skript liest `localStorage['wms.hardwareScanner.geraet'] === 'aus'` und
  registriert dann keinen Listener.

### 4. Einstellungen
| Key (`AppSettingKeys`) | Typ | Default ohne DB-Zeile | Maske |
|---|---|---|---|
| `HardwareScannerAktiv` | Bool | aus (nur `"true"` schaltet ein, Muster ADR 0011) | Standorteinstellungen, neue Gruppe „Hardware-Scanner" |
| `HardwareScannerPraefix` | String | je Rueckfrage 3 (`]` AIM bzw. `~09`) | dito |
| `HardwareScannerMaxZeichenabstandMs` | Int | 50 (Kalibrier-Knopf, TS misst am Geraet) | dito |

PascalCase statt `Scan:*` der Notiz: `Sync:*`-Doppelpunkt-Keys sind die ServiceSettings-Konvention
([[0008-servicesettings-db-first-mit-typisiertem-katalog]]), AppSettings-Keys sind PascalCase
(`AppSettingKeys.cs`).

**Hierarchie (Empfehlung, Rueckfrage 5):** Standort aus -> ueberall aus (Skript wird nicht geladen).
Standort an -> auf jedem Geraet aktiv, ausser das Geraet ist in „Mein Profil" abgeschaltet.
Begruendung: der Listener reagiert nur auf Praefix + Scanner-Tempo, auf Geraeten ohne Scanner bleibt
er wirkungslos; der Scanner haengt am Geraet, nicht an der Person (derselbe Benutzer arbeitet am PC
ohne Scanner). Ein Benutzerfeld waere daher nur ein weiterer Ausschalter ohne eigenen Nutzen und
kostet Migration + 8 Code-Stellen.

### 5. Praefix-Varianten
| | A: AIM-Symbologie-Kennung | B: eigener fester Praefix |
|---|---|---|
| Was kommt an | `]` + Kennbuchstabe + Modifikator vor jedem Code. Ueblich (ISO/IEC 15424): Code 128 `]C0` (`]C1` = GS1-128), EAN-13 `]E0`, EAN-8 `]E4`, QR `]Q1`, Code 39 `]A0`, Code 93 `]G0`, DataMatrix `]d1` (`]d2` = GS1). **Am Geraet pruefen** - Hersteller-Doku, nicht im Code belegbar. | Frei gewaehltes Literal, z. B. `~09` |
| Code-Art-Erkennung | ja (Regel 5 d) | nein |
| Konfiguration | am Geraet einschalten (eine Option) | Praefix-String je Geraet eintragen |
| Fehlerkennung | `]` tippt im WMS praktisch niemand; plus Tempo-Pruefung | `~` ebenso selten; plus Tempo-Pruefung |
| Layout-Risiko | `]` ist auf DE-Layout AltGr+9 - Scanner muss auf DE-Layout stehen, sonst kommt ein anderes Zeichen | `~` ist auf DE-Layout AltGr++; auf einigen Plattform-Layouts eine Tottaste (wartet auf Folgezeichen) - am Geraet testen |

**Empfehlung A**, sofern das Geraet AIM kann (Rueckfrage 2): eine Einstellung am Geraet, und die
Code-Art loest die Mehrdeutigkeit auf Seiten mit Artikel- **und** Lagerplatz-Ziel ohne Fokus
(Stueckliste: `bomScanArticle` ist hidden und nie fokussierbar, `Bom.cshtml:74`).

**Steuerzeichen (STX/ETX) nicht:** sie erzeugen im Browser kein `KeyboardEvent.key` mit einem Zeichen
(nicht druckbar) und kommen je nach Plattform gar nicht oder als `Unidentified` an - nicht verlaesslich erkennbar.

**Tastaturlayout allgemein:** HID-/Wedge-Scanner senden Tastencodes fuer das **am Scanner**
eingestellte Layout. Steht der Scanner auf US und das Geraet auf DE, werden neben `]`/`~` auch `y`/`z`
vertauscht und `-`/`/` falsch - das betrifft auch Artikelnummern. Gegenmassnahme: Scanner auf
DE-Layout konfigurieren; TS-94 prueft Praefix **und** einen Code mit `y`, `z`, `-`.

## Migrations-/SQL-Auswirkungen

- **Keine Migration** bei der Empfehlung (Rueckfrage 5): AppSettings-Zeilen entstehen beim ersten
  Speichern der Standorteinstellungen, kein Seed (Muster `AppSettingKeys.cs:70-80`); Geraete-Schalter
  liegt im Browser. Kein `SQL/AgentJobs/*` betroffen. FreshInstall unveraendert.
- **Nur falls Rueckfrage 5 „pro Benutzer"**: Spalte `Users.HardwareScannerAktiv bit NOT NULL DEFAULT 1`
  (oder nullable), Model -> `dotnet ef migrations add AddUserHardwareScanner` -> `SQL/NN_AddUserHardwareScanner.sql`
  mit `COL_LENGTH`/`OBJECT_ID`-Guard, DDL in eigenem Batch, `__EFMigrationsHistory`-Insert separat ->
  `00_FreshInstall.sql` an beiden Stellen. Naechste freie Nummer gemessen: Buendel hoechste `95`
  (`SQL/95_RenameOperationConfigsAndAddAudit.sql`), main hoechste `88`
  (`SQL/88_AllowMultipleDummyRequisitionItems.sql`) -> **96**; im Dev-Lauf erneut messen.

## Audit-Feld-Auswirkungen

- Keine neue Entitaet. AppSettings-Speichern laeuft ueber die bestehende Standorteinstellungen-Maske
  (Audit wie dort). Geraete-Schalter: kein Server-Schreiben.
- Nur bei Benutzerfeld (Rueckfrage 5): `User` erbt `AuditableEntity` (`User.cs:5`); Profil-/Benutzerpflege-
  Speichern setzt die Modified-Felder bereits - Dev-Lauf prueft, dass der neue Wert im selben Update landet.

## Rollen / Zugriff

- Keine neue Rolle, kein neuer Filter. Der Listener speist nur Ziele, die die Seite ohnehin rendert;
  deren Zugriffsschutz (`RequireXxxAccess`) bleibt massgeblich. `Bom.cshtml:712` bindet im
  ReadOnly-Modus keine Ziele -> dort greift nur Regel (b)/(d).
- Einstellungen: nur Admin (`StandortEinstellungenController.cs:20`). Geraete-Schalter: jeder
  angemeldete Benutzer in „Mein Profil".
- Keine Listen-View neu -> keine Listen-View-Pattern-Pflichten.

## Akzeptanzkriterien

1. `HardwareScannerAktiv` fehlt oder ist nicht `"true"`: Seitenquelltext enthaelt kein
   `hardware-scanner.js`; Tastatur-Scanner verhalten sich exakt wie vor der Aenderung.
2. Schalter an, StockMovements/Inbound, Fokus im Artikel-Feld: Scan eines Artikel-QR waehlt den
   Artikel im Select2, „✓ Gescannt: <Nr>" erscheint; weder Praefix noch Code stehen als Text in einem Feld.
3. Schalter an, Scan mit Enter-Suffix in einem Formular: kein Formular-Submit, kein Seitenwechsel.
4. Schalter an, Fokus im Lagerplatz-Select, Code-128-Lagerplatz-Etikett: Option gewaehlt wie beim Kamera-Scan.
5. Langsames Tippen des Praefix (Abstand > Zeichenabstand) in einem Textfeld: alle Zeichen erscheinen
   vollstaendig und in richtiger Reihenfolge; `]` per AltGr+9 auf DE-Layout erscheint.
6. Fokus in einem Passwortfeld (Mein Profil, neues Passwort): auch schnell getippte Praefix-Folge wird
   nicht abgefangen.
7. BDE-Terminal, Fokus in `scanOperator`: Scan der Personalnummer mit Praefix meldet den Operator an
   wie bisher ohne Praefix; Feld enthaelt danach keinen Praefix. Gleiches fuer `scanFaAg` mit `FA,AG`.
8. Fokus in einem freien Textfeld ohne Scan-Ziel (z. B. Spaltenfilter): Code ohne Praefix steht im
   Feld, ein Enter-Handler des Felds (ENTER-Spaltenfilter) wird ausgeloest.
9. Kein Fokus, Seite mit genau einem Ziel (Articles/Info): Scan fuellt `articleNumber`.
10. Kein Fokus, Stueckliste (Lagerplatz- + Artikelziel), Variante A: QR-Artikel geht an `bomScanArticle`
    (Kommissionier-Ablauf startet), Code-128 an `TargetStorageLocationId`. Variante B: Hinweis
    „bitte zuerst das Feld antippen".
11. Seite ohne Ziel und ohne fokussiertes Feld (z. B. Startseite): sichtbarer Hinweis mit Zeichen + Text,
    `role="status"`, verschwindet nach ~5 s; Wert wird escaped angezeigt (Test mit `<b>` im Code).
12. „Mein Profil" -> Hardware-Scanner auf diesem Geraet aus: auf diesem Browser kein Abfangen mehr
    (AK 1-Verhalten), auf einem zweiten Geraet weiterhin aktiv.
13. Standorteinstellungen zeigen Gruppe „Hardware-Scanner" mit 3 Feldern; nur Admin kann speichern;
    Aenderung des Zeichenabstands wirkt nach Neuladen.
14. Kamera-Scan auf Inbound, Bom und ProductionOrders funktioniert unveraendert (Regression).
15. `dotnet test`: Node-Selbstpruefung `hardware-scanner.check.js` gruen (oder sauber uebersprungen
    ohne Node) und deckt ab: Praefix erkannt/abgelehnt, Zeitgrenze, Enter/Tab-Ende, Ende per Timeout,
    Replay bei Fehlstart, AIM-Kennung abgetrennt (bei A), Praefix-Zeichen mitten im Text.
16. Hilfe (`Help/Index.cshtml` Scanner-Abschnitt) und README-AppSettings-Tabelle nennen die 3 Keys,
    Default aus, Geraete-Schalter und Scanner-Konfiguration (DE-Layout, Praefix, Enter).

## Test-Szenarien

Neues Kapitel **TS-94 — Hardware-Barcodescanner (Tastatur-Scan mit Praefix)** in
`docs/TESTSZENARIEN.md` (hoechstes vorhandenes: TS-93 `docs/TESTSZENARIEN.md:9962`), danach
[[testszenarien-index]].

- **Vorbedingungen:** Admin setzt Standorteinstellungen „Hardware-Scanner aktiv", Praefix je Variante,
  Abstand 50. Geraet von Gerald mit Scanner auf **DE-Layout**, Praefix/AIM und Enter-Suffix
  konfiguriert. Etiketten: Lagerplatz (Code 128), Kommissionierliste mit HauptFA-Barcode, Artikel-QR,
  ein Code mit `y`/`z`/`-` im Inhalt.
- **94.1 Kalibrierung:** Scan in ein leeres Textfeld bei Schalter aus -> Rohtext pruefen (Praefix-Zeichen
  korrekt? Layout?). Danach Schalter an, Zeichenabstand 50 / 30 / 100 probieren; kleinster stabiler Wert notieren.
- **94.2-94.5 Seiten mit Ziel:** Inbound (Artikel, Lagerplatz), Transfer (3 Ziele, Fokus wechseln),
  Stueckliste (Artikel-QR ohne Fokus, Lagerplatz), FA-Liste (FA-Filter, Callback submittet wie Kamera).
- **94.6 BDE-Terminal:** Personalnummer + FA,AG per Scanner mit Praefix.
- **94.7 Freies Feld:** Spaltenfilter, Bemerkungsfeld.
- **94.8 Kein Ziel:** Startseite -> Hinweis; Screenreader/Kontrast-Sichtpruefung.
- **94.9 Geraete-Schalter** in Mein Profil, zweites Geraet.
- **94.10 Simulation per Tastatur (ohne Geraet):** Praefix + Code per Textersetzer/AutoHotkey o. ae.
  in < 20 ms je Zeichen = Scan; Praefix von Hand langsam tippen = normaler Text (AK 5); AltGr+9.
- **Negativfaelle:** Passwortfeld (AK 6); Scanner auf US-Layout (erwartet: kein Scan erkannt bzw.
  `y`/`z` vertauscht -> dokumentiert Konfigurationspflicht); Scan waehrend offenem Kamera-Modal
  (wird nach Regel a/d behandelt, Modal bleibt offen); EAN-Scan -> „Nicht gefunden".

## Risiken / Grenzen (bewusst)

- **IME-Ausgabe auf Android:** Sendet die Scanner-Software ueber die Bildschirmtastatur/IME
  (`keyCode 229`, `key 'Unidentified'`), sieht der Listener keine Zeichen - dann wirkt nur Regel (b)
  „von selbst" ohne Abfangen. Die Scanner-Software muss Tasten-Ereignisse senden (z. B. bei Zebra
  DataWedge die Keystroke-Ausgabe; genaue Option am Geraet pruefen - unverifiziert, Rueckfrage 2).
- Nachgetragene Praefix-Zeichen (Fehlstart) loesen nur `input`, kein `keydown` der Seite aus -
  ponytail-Grenze, betrifft nur das manuelle Tippen des Praefix-Zeichens.
- Autofill fuellt Felder ohne Tasten-Ereignisse - keine Wechselwirkung.

## Etappen (nur bei epic: true)

Kein Epic: neue JS-Datei (~150 Zeilen), kleine Ergaenzung `barcode-scanner.js`, Layout, Katalog,
Profil-Schalter, Hilfe/README, ein Node-Check - passt in einen Dev-Lauf.

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| - | entfaellt | - | - |

## Deploy

*Vorlaeufig - der QA-Lauf bestaetigt gegen den echten Diff.*

- **Web-App:** ja (JS, Layout, Views, Katalog).
- **Service:** nein.
- **Migration:** nein (bei Rueckfrage 5 „pro Benutzer": ja, Migration 96 vor dem Web-Deploy).
- **Publish-Befehle:** im Umsetzungs-Worktree
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
- Nach Deploy: Schalter bleibt aus, bis Admin ihn je Standort einschaltet; Scanner-Konfiguration der
  Geraete (DE-Layout, Praefix/AIM, Enter) vor dem Einschalten.
- Dev-Lauf: Skill `frontend-design` ist Pflicht (Hinweis-Element, Profil-Schalter); Version in beiden
  `AppVersion.cs` + Anwender-Changelog.

## Offene Rueckfragen

1. **Umsetzungsort.** Empfehlung: **eigener Worktree ab main nach dem Merge des IDEAL-Buendels.**
   Begruendung: Das Feature haengt an Buendel-Stand - `StandortSettingsCatalog` existiert in main nicht
   (Suche in `C:\Git\IDEAL-AKE-WMS\IdealAkeWms`: kein Treffer), das Node-Check-Muster
   (`column-filter-syntax.check.js`, `SkippableFact` in `ColumnFilterSyntaxTests`) ebenfalls nicht, und
   der `_Layout`-Skriptblock unterscheidet sich (main `:329-331` nur jQuery/Bootstrap/site.js, Buendel
   `:412-420`). `barcode-scanner.js` und die 14 Einbindungen sind in main identisch (gleiche
   Funktionszeilen 8/269/350/391/410). Das Buendel wartet auf Schranke 2 und sollte nicht weiter
   wachsen; Gerald hat bei der Spaltenauswahl-Spec genauso entschieden. Alternative: im Buendel, wenn
   der Scanner vor dem Buendel-Merge gebraucht wird.
2. **Geraete/Scanner-Software:** Hersteller/Modell und Software (Zebra DataWedge, Honeywell, Urovo …)?
   Einstellbar: AIM-Kennung, eigener Praefix, Enter-Suffix, Tastaturlayout, Ausgabe als
   Tasten-Ereignisse statt IME? Empfehlung: Modell nennen und ein Geraet fuer TS-94 bereitstellen; die
   Spec setzt Tasten-Ereignisse + DE-Layout voraus.
3. **Praefix A oder B?** Empfehlung **A (AIM)**, wenn das Geraet es kann (eine Option, Code-Art fuer die
   Ziel-Wahl ohne Fokus); sonst **B** mit `~09` als Default. Gebaut wird nur die gewaehlte Variante.
4. **Code-Arten:** Bestaetigen, dass gescannt werden: Code 128 (Lagerplatz-Etiketten, HauptFA auf
   Kommissionier-/Beschichtungslisten, Beschichtungsauftrag) und Artikel-QR
   (`Artikelnummer;…;FA` - woher stammen diese Etiketten? Im WMS nicht erzeugt). **EAN:** das WMS hat
   keine EAN; Empfehlung: in dieser Spec ausklammern, Sage-Pruefabfrage (Abschnitt Ist-Stand/EAN)
   laufen lassen, bei Bedarf eigener Backlog (Artikelstamm + Sync + Lookup).
5. **Einstellungs-Ebenen:** Empfehlung **Standort-Hauptschalter + Geraete-Abschalter, kein
   Benutzerfeld** (keine Migration, Begruendung im Entwurf §4). Alternative: zusaetzlich pro Benutzer
   (Migration 96, 8 Code-Stellen; Hierarchie dann Standort aus > Geraet aus > Benutzer aus).
6. **Ziel-Regel und Umfang:** Regel 5 (a) Fokus-Ziel > (b) freies Feld > (d) einziges/AIM-eindeutiges
   Ziel > Hinweis so freigeben? Empfehlung ja, und **alle 14 Seiten + BDE in einem Lauf** - eine
   Seiten-Reihenfolge entfaellt, weil die Ziele ueber `initScanner`/`initTextInputScanner` automatisch
   registriert werden.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. Umsetzungsort (eigener Worktree ab main nach Buendel-Merge / Buendel) →
2. Geraete/Scanner-Software, Konfigurierbarkeit →
3. Praefix A (AIM) oder B (welcher) →
4. Code-Arten, Herkunft Artikel-QR, EAN ausklammern + Sage-Abfrage →
5. Einstellungs-Ebenen (Standort + Geraet / zusaetzlich Benutzer) →
6. Ziel-Regel + alle Seiten in einem Lauf →
