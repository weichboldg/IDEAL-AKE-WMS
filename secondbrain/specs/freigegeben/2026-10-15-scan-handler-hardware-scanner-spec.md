---
type: spec
title: Scan-Handler fuer Hardware-Barcodescanner (Tastatur-Eingabe mit Praefix)
slug: 2026-10-15-scan-handler-hardware-scanner-spec
status: Testbereit
created: 2026-10-15
updated: 2026-10-21
source_backlog: "[[2026-10-15-scan-handler-hardware-scanner]]"
task: "[[2026-10-15-scan-handler-hardware-scanner-umsetzung]]"
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
affected_code:
  - IdealAkeWms/wwwroot/js/hardware-scanner.js (NEU - reiner Kern + DOM-Anbindung, im _Layout geladen)
  - IdealAkeWms/wwwroot/js/barcode-scanner.js (initScanner Z. 8-18, initTextInputScanner Z. 391-405 - Ziel-Register + Listener auf das Scan-Ereignis; processScannedValue Z. 269-348 und Wrapper Z. 410-426 UNVERAENDERT)
  - IdealAkeWms/Views/Shared/_Layout.cshtml (Skriptblock Z. 412-420, data-Attribute am body)
  - IdealAkeWms/Models/AppSettingKeys.cs (3 neue Keys, kein Seed)
  - IdealAkeWms/Models/Standort/StandortSettingsCatalog.cs (Felder + Gruppe)
  - IdealAkeWms/Views/Account/Profile.cshtml (Geraete-Schalter, nur localStorage)
  - IdealAkeWms/Views/Help/Index.cshtml (Scanner-Abschnitt um Z. 90)
  - IdealAkeWms.Tests/Js/hardware-scanner.check.js (NEU) + Tests-Klasse mit SkippableFact (Muster ColumnFilterSyntaxTests.cs Z. 96-120)
  - README.md (AppSettings-Tabelle), docs/TESTSZENARIEN.md (TS-99)
open_questions: []
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: false
freigabe_entscheidung: ""
freigabe_von: "Gerald Weichbold"
freigabe_am: "2026-10-21"
---

# Scan-Handler fuer Hardware-Barcodescanner

> [!info] Code-Grundlage
> Alle Datei:Zeile-Angaben gemessen im Buendel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`
> (HEAD `2524bb03`, v1.60.0), nur gelesen. Abweichungen zu `main` stehen in Rueckfrage 1.
> Standorte: **AKE und IDEAL** gleichermassen - die Scan-Seiten und `barcode-scanner.js` sind
> standortneutral, keine Verzweigung nach `ProduktionsauftragHierarchisch`.

> [!important] Freigabe-Nachtrag 2026-10-21 (geht dem Rumpf und Punkt 1 des Nachtrags vom 16.10. vor)
> **Ort geändert (Gerald 2026-10-21):** Umsetzung **im Bündel-Worktree** `.claude/worktrees/2026-08-07-ideal-teile-1-5`
> (Zweig `feature/2026-08-07-ideal-teile-1-5`), **nach** der Zahnrad-Sammel-Spec (v1.65.0). Zeilennummern gegen den dann
> aktuellen Bündel-Stand nachmessen. Version **v1.66.0**, Testszenario **TS-99**. Punkte 2–6 vom 16.10. gelten unverändert.

> [!important] Freigabe-Nachtrag 2026-10-16 (geht dem Rumpf vor)
> Antworten Gerald im Block „Freigabe-Antworten“, hier fuer den Dev-Lauf zusammengezogen:
> 1. **Ort:** eigener Worktree ab `main` **nach** dem Merge des IDEAL-Buendels.
> 2. **Geraet:** Testgeraet Android-Handheld **Netum AIDC**; die Loesung muss **geraeteunabhaengig** sein
>    (keine herstellerspezifische Logik, keine Abhaengigkeit von AIM).
> 3. **Praefix-Variante B** (ersetzt die Empfehlung A in §5): eigener Praefix aus `HardwareScannerPraefix`,
>    **Default `~09`**, Enter als Abschluss. Variante A (AIM) wird **nicht** gebaut; damit entfaellt Regel 5 (d)
>    „AIM-eindeutiges Ziel“ - (d) gilt nur noch als „einziges Ziel“. Der Default in §4 ist `~09`.
>    `~` kann auf manchen Layouts eine Tottaste sein: TS-99 prueft das am Netum; der Praefix bleibt konfigurierbar.
> 4. **Codes:** Code 128 (Lagerplatz, HauptFA, Beschichtungsauftrag) und Artikel-QR (Etiketten teils
>    selbst gedruckt). **EAN nicht in dieser Spec** - fuer Einkaufsartikel gewuenscht, eigene Spec
>    [[2026-10-16-ean-einkaufsartikel-scan]] (Artikelstamm + Sync + Lookup).
> 5. **Ebenen:** Standort-Hauptschalter + Geraete-Abschalter, **kein** Benutzerfeld, **keine** Migration.
> 6. **Ziel-Regel** (a) Fokus > (b) freies Feld > (d) einziges Ziel > Hinweis; **alle 14 Seiten + BDE in einem Lauf.**

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
- Node-Selbstpruefung des reinen Kerns, TS-99, Hilfe, README.

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
DE-Layout konfigurieren; TS-99 prueft Praefix **und** einen Code mit `y`, `z`, `-`.

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

Neues Kapitel **TS-99 — Hardware-Barcodescanner (Tastatur-Scan mit Praefix)** in
`docs/TESTSZENARIEN.md` (hoechstes vorhandenes nach der Zahnrad-Spec: TS-98), danach
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

*Final (QA-Lauf 2026-10-21) aus dem echten Diff `c27f3b6d..789d0a78`.*

- **Web-App:** ja — `IdealAkeWms/` (hardware-scanner.js neu, barcode-scanner.js, _Layout, BdeTerminal/Index, Account/Profile,
  Help, AppSettingKeys, StandortSettingsCatalog, StandortEinstellungenController, AppVersion).
- **Service:** ja, aber **nur Versionsstand** — einzige Änderung `IDEALAKEWMSService/AppVersion.cs` (1.65.0 → 1.66.0), funktional
  nichts. Mitpublizieren, damit Web und Dienst dieselbe Version zeigen.
- **Migration:** nein (kein neuer Ordner-Eintrag unter `*/Migrations/`, kein `SQL/`-Skript, FreshInstall und `SQL/AgentJobs/`
  unberührt). AppSettings-Zeilen entstehen beim ersten Speichern der Standorteinstellungen.
- **Ablauf des Menschen:** aus dem Worktree publizieren → Testsystem → TS-99 testen → Merge.
  ```
  dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
  dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService
  ```
- Nach dem Merge nur dann erneut aus `main` publizieren, wenn der Merge getestete Dateien mit parallelen `main`-Änderungen
  zusammengeführt hat.
- Nach Deploy: Schalter bleibt aus, bis ein Admin ihn je Standort einschaltet; Scanner-Konfiguration der Geräte (DE-Layout,
  Präfix `~09` bzw. Ersatz ohne Tottaste, Enter-Suffix, Ausgabe als Tastendrücke) vor dem Einschalten.

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
   Tasten-Ereignisse statt IME? Empfehlung: Modell nennen und ein Geraet fuer TS-99 bereitstellen; die
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

## Umsetzung (Dev-Lauf 2026-10-21)

Bündel-Worktree, Basis `c27f3b6d` (v1.65.0), Commit `cb49f262` (wip feature-complete), **v1.66.0**, **TS-99**. Aufgabe
[[2026-10-15-scan-handler-hardware-scanner-umsetzung]].

**Nachgemessen gegen v1.65.0:** `barcode-scanner.js` unverändert seit der Spec (`initScanner` :8-18, `processScannedValue` :269-348,
`initTextInputScanner` :391-405, Wrapper :410-426); `_Layout.cshtml` Skriptblock jetzt :412-420 (vor dem Umbau), Inject `AppSettings` :2;
`StandortSettingsCatalog.Fields` :34-64, `GroupOrder` :69-72; BDE `scanOperator` `Index.cshtml:47`, `scanFaAg` :76.

**Was gebaut ist**
- `wwwroot/js/hardware-scanner.js` (neu): Kern `HwScanCore.create({prefix, maxGapMs})` → `key(k,t)`/`tick(t)`, dualer Export. DOM:
  Capture-`keydown` auf `window`, nur `isTrusted`, IME (`isComposing`/229) und Passwortfeld nie, Kürzel nur ohne AltGr, Modifier
  (Shift, Dead …) brechen nichts ab. Ereignis `wms:hardware-scan` mit `{code, endedWithEnter, freiesFeld, handled, ambiguous}`; danach
  Regel (b) einfügen + `input`/`change` + synthetisches `keydown` Enter, (d) einziges sichtbares `[data-hw-scan-ziel]`, sonst Hinweis
  (`alert-warning`, fixiert oben, `role="status"`, `textContent`, 5 s).
- `barcode-scanner.js`: `_hwScanTargets` aus beiden Init-Wegen; Listener Regel (a) Ziel/Knopf/`.input-group`/offenes Select2 → (d)
  genau ein Ziel, sonst `ambiguous`; Aufruf der **globalen** `processScannedValue`, bei Text-Zielen `_textInputScanCallback` wie der
  Klick-Handler. Kamera-Pfad unverändert.
- BDE: nur Markup `data-hw-scan-ziel` an `scanOperator` und `scanFaAg`; `bde-terminal.js` unverändert. Vor der Anmeldung ist nur
  `scanOperator` sichtbar, danach nur `scanFaAg` → Regel (d) füllt das Feld und löst Enter aus → `scanFaAgInput` (v1.64.0-Auflösung).
- `_Layout`: `HardwareScannerAktiv == "true"` → `<body data-hw-scan-praefix/abstand>` (null-Attribute lässt Razor weg) und Skript vor
  `RenderSectionAsync("Scripts")`.
- Einstellungen: 3 Keys in `AppSettingKeys`, Gruppe „Hardware-Scanner“ im Katalog. Profil: Karte „Dieses Gerät“ (form-switch,
  `localStorage`), nur sichtbar bei Standort-Schalter an; wirkt sofort (Skript liest den Schalter bei jeder Taste).
- Hilfe (Scanner-Abschnitt), Anwender-Changelog, README-AppSettings, v1.66.0 in beiden `AppVersion.cs`, TS-99.

**Abweichungen**
1. **Leeres Int-Feld in den Standorteinstellungen = unverändert** (`StandortEinstellungenController.Save`, +4 Zeilen). Ohne Seed hat
   `HardwareScannerMaxZeichenabstandMs` keine Zeile, die Maske schickt `""` und die bisherige Validierung („erwartet eine ganze Zahl“)
   hätte **jedes** Speichern der Maske blockiert. Gilt auch für `FaHierarchyMaxTiefe` (leer → bleibt, statt Fehler). Test
   `Save_EmptyInt_LeavesKeyUnchanged_AndSavesTheRest`.
2. **BDE ohne Fokus** über das generische Markup-Attribut `data-hw-scan-ziel` statt Sonderlogik — der Rumpf (Regel c) sah nur (b) vor; der
   Schwerpunkt 3 des Auftrags verlangt, dass der Scan im FA-Feld landet, auch wenn nach der Anmeldung nichts fokussiert ist.
3. Ein Ziel im Fokus (Regel a) löst kein zusätzliches Enter aus — wie der Kamera-Scan; `onScanned`-Callbacks (FA-Liste) wenden an.
4. Geräte-Schalter wirkt **ohne Neuladen** (die Spec sah „registriert keinen Listener“ beim Laden vor).

**Nachweis**
- `dotnet build IdealAkeWms.slnx`: 0 Fehler, 28 Warnungen (Bestand).
- `dotnet test`: Web **2146 bestanden, 1 übersprungen** (Bestand), Dienst **316 bestanden**.
- `node IdealAkeWms.Tests/Js/hardware-scanner.check.js …/hardware-scanner.js` → `hardware-scanner core ok (16 Pruefungen)` (auch im
  Testlauf über `HardwareScannerTests.Kern_NodeSelbstpruefung`, Node v24.22.0).

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. Umsetzungsort (eigener Worktree ab main nach Buendel-Merge / Buendel) → **Wie empfohlen: eigener Worktree ab main nach dem Merge des IDEAL-Buendels (Gerald 2026-10-16).**
2. Geraete/Scanner-Software, Konfigurierbarkeit → **Testgeraet: Android-Handheld Netum AIDC. Die Loesung muss aber geraeteunabhaengig mit anderen Scannern funktionieren (Gerald 2026-10-16).**
3. Praefix A (AIM) oder B (welcher) → **B: eigener Praefix, in den Standorteinstellungen konfigurierbar, Default `~09`, Enter als Abschluss. AIM wird nicht gebaut (Gerald 2026-10-16: geraeteunabhaengig, Praefix am Geraet konfigurierbar).**
4. Code-Arten, Herkunft Artikel-QR, EAN ausklammern + Sage-Abfrage → **Code 128 (Lagerplatz, HauptFA, Beschichtungsauftrag) und Artikel-QR. Artikel-Etiketten druckt IDEAL-AKE teilweise selbst (Gerald 2026-10-16). EAN ausgeklammert: fuer Einkaufsartikel gewuenscht, wird eigene Spec (Backlog [[2026-10-16-ean-einkaufsartikel-scan]], Gerald 2026-10-16).**
5. Einstellungs-Ebenen (Standort + Geraet / zusaetzlich Benutzer) → **Wie empfohlen: Standort-Hauptschalter + Geraete-Abschalter, kein Benutzerfeld, keine Migration (Gerald 2026-10-16).**
6. Ziel-Regel + alle Seiten in einem Lauf → **Wie empfohlen: Regel (a) Fokus > (b) freies Feld > (d) einziges Ziel > Hinweis; alle 14 Seiten + BDE in einem Lauf (Gerald 2026-10-16).**

## QA-Nachweis (qa-agent, 2026-10-21)

Geprüft im Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Diff `c27f3b6d..cb49f262` plus QA-Fix `789d0a78`.
Web und Dienst wurden **nicht** lokal gestartet (Auftrag).

### 1. Build und Tests (selbst ausgeführt, nach dem QA-Fix wiederholt)
- `dotnet build IdealAkeWms.slnx`: **0 Fehler**, 25 Warnungen (Bestand, u. a. NU1903 `System.Security.Cryptography.Pkcs`).
- `dotnet test`:
  - `IdealAkeWms.Tests`: **2146 bestanden, 1 übersprungen, 0 fehlgeschlagen** (übersprungen = Bestand
    `ProductionOrderEagerCreateAgentJobTests.EagerCreate_TwoMergesProduce2Rows_PerNewOrder_AndAreIdempotent`).
  - `IDEALAKEWMSService.Tests`: **316 bestanden, 0 übersprungen, 0 fehlgeschlagen**.
- `HardwareScannerTests.Kern_NodeSelbstpruefung` lief **echt** (nicht übersprungen); direkt: `node hardware-scanner.check.js` →
  `hardware-scanner core ok (16 Pruefungen)`.
- Index-Zeilenenden nach dem Fix: `git ls-files --eol` → `i/lf` für alle drei geänderten Dateien.

### 2. Unabhängiges Code-Review (selbst, in diesem Lauf)
Geprüft: `hardware-scanner.js` (Kern + DOM vollständig), `barcode-scanner.js` (Register, Listener, Zusammenspiel mit
`processScannedValue`/Wrapper/`showScanFeedback`), `_Layout.cshtml` (body-Attribute, Skript-Reihenfolge), `BdeTerminal/Index.cshtml` mit
`bde-terminal.js:95-175` (Sichtbarkeit, Fokus, Enter-Handler), `Account/Profile.cshtml`, `StandortEinstellungenController.Save`,
`StandortSettingsCatalog`, Hilfe, Changelog, README, TS-99, alle `initScanner`/`initTextInputScanner`-Aufrufe in `Views/`, alle
Enter-/keyup-/keypress-Handler in `wwwroot/js/*.js` und `Views/`.

**Befunde**

- **B-1 (blockierend, behoben in `789d0a78`)** — ausgeblendetes, noch fokussiertes Feld galt als „freies Feld“.
  `hardware-scanner.js:90-92` (vorher `freiesFeld` ohne Sichtbarkeit). BDE: Die Anmeldung erfolgt mit Fokus in `scanOperator`; danach blendet
  `bde-terminal.js:137` `#operatorScan` per `d-none` aus. Behält der Browser den Fokus auf dem versteckten Feld, griff Regel (b): Die FA-Nummer
  landete im unsichtbaren `scanOperator`, das synthetische Enter startete `scanOperatorInput` (`bde-terminal.js:100-101`). Folge: stille Meldung
  „Unbekannte Personalnummer“ im ausgeblendeten Feedback, oder ein **Operator-Wechsel**, wenn die FA-Nummer zufällig eine Personalnummer ist.
  Fix: `freiesFeld` verlangt `el.getClientRects().length` (auch für den Replay-Pfad), damit greift Regel (d) und der Scan geht in das einzige
  sichtbare Ziel `scanFaAg`. Abgesichert durch den Quelltext-Wächter `HardwareScannerTests.DomTeil_…`; Manual-UAT TS-99.7 e (neu).
- **N-1 (nicht blockierend, Variante-B-Grenze, specgemäß)** — Stückliste: Das Artikelziel `bomScanArticle` ist `type=hidden`
  (`Picking/Bom.cshtml:74`), das einzige zugehörige fokussierbare Element ist der Knopf `btnScanBomArticle` (`:67`). Auf einem Touch-Gerät
  löst Antippen den Klick aus, also die Kamera. Bei offenem Kamera-Fenster liegt der Fokus im Modal, Regel (a) greift nicht
  (`barcode-scanner.js:437-446`). Regel (d) findet zwei Ziele und zeigt den Hinweis. **Am Handheld kann auf der Stückliste kein Artikel per
  Hardware-Scanner kommissioniert werden**, nur der Wagen-Lagerplatz. AK 10 Variante B verlangt nur den Hinweis, ist also erfüllt.
  TS-99.4 war am Touch-Gerät nicht ausführbar („Knopf antippen, nicht auslösen“) und ist präzisiert. **Entscheidung Gerald:** Reicht das? Sonst
  Folgespec, kleinste Lösung: Regel (a) erkennt das offene Kamera-Fenster und nimmt dessen Ziel.
- **N-2 (nicht blockierend)** — `StandortEinstellungenController.cs:70-71`: Leeres Int = unverändert. Ein einmal gespeicherter Zeichenabstand
  lässt sich in der Maske nicht mehr auf „leer = 50“ zurücksetzen; der Hilfetext „Leer = 50.“ (`StandortSettingsCatalog.cs:72`) stimmt nur, bis
  zum ersten Speichern eines Werts. Dazu fehlt eine Bereichsprüfung: `0` wird in `hardware-scanner.js:81` per `|| 50` zu 50, ein
  negativer Wert lässt **nie** einen Scan erkennen (still). Nur Admin, am Gerät sofort sichtbar. Empfehlung: Wert 50 eintragen statt leeren;
  ggf. später Bereich 10–500 serverseitig.
- **N-3 (nicht blockierend, Grenze)** — Tottasten **innerhalb** eines Codes (DE: Zirkumflex, Akut, Gravis): `Dead` geht als Modifier durch
  (`hardware-scanner.js:15`, `:36`), das Folgezeichen wird verschluckt, das Akzentzeichen kann fehlen. Codes im Umfang (Code 128
  Lagerplatz/FA/Beschichtungsauftrag, Artikel-QR) enthalten das nach heutigem Stand nicht. Die Tottaste **im Präfix** `~` deckt TS-99.1 b ab.
- **N-4 (nicht blockierend, a11y)** — Die Hinweis-Box (`role="status"`, `aria-live`) wird erst beim ersten Hinweis erzeugt und im selben Zug
  gefüllt (`hardware-scanner.js:111-124`). Manche Screenreader lesen den **ersten** Hinweis dann nicht vor. TS-99.9 prüft das. Behebung bei Bedarf:
  Box beim Laden leer anlegen.
- **N-5 (nicht blockierend, Bestand)** — LocationTransfer registriert `SourceStorageLocationId` doppelt (Auto-Init `barcode-scanner.js:467` plus
  `LocationTransfer.cshtml:128`). Das ist eine bestehende Doppelbindung des Kamera-Knopfs. Für den Hardware-Scan ist sie harmlos, weil
  `filter()[0]` genau einen Aufruf macht und die Seite ohnehin zwei Ziele hat.
- **N-6 (nicht blockierend)** — Die Defaults `~09`/`50` stehen an zwei Stellen: `_Layout.cshtml:24-25` und als JS-Rückfall
  `hardware-scanner.js:81-82`. Der JS-Rückfall ist praktisch tot, weil das Skript nur mit gesetzten Attributen geladen wird. Bei einer
  Default-Änderung beide Stellen ändern.

**Gezielt geprüfte Fehlerklassen (ohne Befund)**
- **Werte in `<script>`:** Keine. Präfix und Abstand gehen als Razor-encodierte body-Attribute (`_Layout.cshtml:28`) und werden per
  `getAttribute` gelesen. Der Hinweis setzt den Code per `textContent`; `showScanFeedback` escaped (`barcode-scanner.js:363-365`).
- **`hidden` + `d-*`:** Profil-Karte `class="card mt-3" … hidden` hat keine `d-*`-Klasse. `.card` setzt `display:flex`, aber Bootstrap
  `[hidden]{display:none!important}` (`lib/bootstrap/dist/css/bootstrap.css:594-596`) gewinnt. Die Hinweis-Box hat ebenfalls keine `d-*`-Klasse.
- **Stille Rückfälle:** Präfix/Abstand leer → dokumentierte Defaults (Hilfe, README, Katalog). Der Fall `0` bzw. negativ steht unter N-2.
- **Parallele Arrays:** nicht betroffen.
- **Nur-Browser-Validierung:** Int-Prüfung bleibt serverseitig (`StandortEinstellungenController.cs:72-76`). Der Schalter ist `[RequireAdminAccess]`.
- **Eine Regel an mehreren Orten:** Die Auswertung bleibt allein in `processScannedValue`. Der Hardware-Pfad ruft die **globale**, gewrappte
  Funktion (`barcode-scanner.js:459`), damit `onScanned` feuert. Die Defaults stehen unter N-6.
- **Ereignis-Reihenfolge / Enter vs. Submit:** Der Capture-Listener auf `window` wird zuerst registriert. `preventDefault` auf `keydown`
  unterdrückt `keypress` und damit die implizite Formular-Übermittlung, `stopImmediatePropagation` hält die Seiten-Handler ab. In
  `wwwroot/js` und `Views` gibt es **keine** `keyup`-/`keypress`-Handler (Suche), die ein verschlucktes Enter trotzdem sähen.
- **Synthetisches Enter:** Alle Enter-Handler prüfen `e.key` (`bde-terminal.js:101`, `:167`; `table-filter.js:71`;
  `PickingLeitstand/Index.cshtml:538`) und funktionieren daher mit `new KeyboardEvent('keydown',{key:'Enter'})`, obwohl `keyCode` dabei 0 ist.
  `!e.isTrusted` verhindert die Wiederverarbeitung. Ein synthetisches Ereignis löst keinen nativen Submit aus.
- **Replay:** Wird synchron im `keydown` vor der Standardaktion der aktuellen Taste eingefügt, die Reihenfolge stimmt. Die Zeitgrenze per
  Timer `maxGapMs + 5` (`performance.now()` gegen `e.timeStamp`, gleiche Zeitbasis). Nachgetragene Zeichen lösen nur `input` aus (Spec-Grenze).
- **AltGr/Modifier:** Unter Windows kommen `Control` und `AltGraph` als eigene keydowns. Beide stehen in `MODIFIER`, gehen durch und
  setzen `last` nicht. `~` kommt mit `ctrlKey && altKey` und `getModifierState('AltGraph')` und gilt deshalb nicht als Kürzel. Echte Kürzel
  brechen einen Präfix-Kandidaten mit Replay ab (Node-Test „Kuerzel“).
- **Select2:** Der geschlossene Select2 liegt in derselben `.input-group` (`Inbound.cshtml:20-26`), dafür greift Regel (a). Beim offenen
  Dropdown greift die Prüfung `select2-container--open` plus `.select2-dropdown`, danach wird es geschlossen. Die Artikel-Auflösung läuft
  über die API wie bei der Kamera.
- **`offsetParent`-Sichtbarkeit (Regel d, BDE):** Vor der Anmeldung ist `#operationsCard` `display:none`, danach `#operatorScan` `d-none`
  (`bde-terminal.js:136-139`, `:147-150`). Damit ist jeweils genau ein `[data-hw-scan-ziel]` sichtbar. Beide Felder sind nicht `position:fixed`.
- **Controller-Abweichung leeres Int:** Ein leeres Int-Feld wird übersprungen und nicht geschrieben; die übrigen Felder werden atomar
  gespeichert. Gilt auch für Service-Int-Keys (z. B. `Sync:ProductionWorkplaceSyncMandant`: leer = unverändert statt Fehler). Der
  Bestandstest `Save_InvalidInt_WritesNothing_AndKeepsEnteredValues` bleibt grün (`abc` → Fehler). Siehe N-2.

### 3. Beweisart je Akzeptanzkriterium
| AK | Beweis | Zählt grün? |
|---|---|---|
| 1 Schalter aus → kein Skript | Quelltext-Wächter `Layout_LaedtSkriptNurBeiSchalterAn_…` (Bedingung + Reihenfolge); echtes Rendering **Manual-UAT** TS-99.1 a | teilweise |
| 2 Inbound Artikel-QR → Select2 | **Manual-UAT** TS-99.2 (Browser, Select2, API) | nein |
| 3 Enter-Suffix kein Submit | Kern: Enter → `swallow` (Node); Browser-Submit **Manual-UAT** TS-99.2 | teilweise |
| 4 Lagerplatz-Select | **Manual-UAT** TS-99.2 | nein |
| 5 langsames Tippen, AltGr+9 | Kern: Reihenfolge/Replay (Node „langsam“, „Praefix-Zeichen im Text“); AltGr am Gerät **Manual-UAT** TS-99.11 | teilweise |
| 6 Passwortfeld | Quelltext-Wächter; Verhalten **Manual-UAT** Negativfall a | nein |
| 7 BDE Operator + FA,AG | **Manual-UAT** TS-99.7 a–e | nein |
| 8 freies Feld + Enter-Handler | **Manual-UAT** TS-99.8 | nein |
| 9 einziges Ziel Articles/Info | **Manual-UAT** TS-99.6 | nein |
| 10 Stückliste Variante B → Hinweis | **Manual-UAT** TS-99.4 (siehe N-1) | nein |
| 11 Hinweis ohne Ziel, escaped | Quelltext-Wächter (`textContent`, `role`, kein `innerHTML`); Darstellung **Manual-UAT** TS-99.9 | teilweise |
| 12 Geräte-Schalter | Quelltext-Wächter `Profil_…`; Verhalten **Manual-UAT** TS-99.10 | nein |
| 13 Standorteinstellungen 3 Felder, nur Admin | automatisiert: `Standorteinstellungen_GruppeMitDreiKeys_KeinSeed`, `Controller_IsAdminOnly`, `Save_EmptyInt_…`; „wirkt nach Neuladen“ **Manual-UAT** TS-99.12 | teilweise |
| 14 Kamera-Regression | **Manual-UAT** TS-99.13 | nein |
| 15 Node-Selbstprüfung | automatisiert, grün, lief echt (Node v24); AIM entfällt (Variante B) | ja |
| 16 Hilfe + README | Review: `Help/Index.cshtml` Abschnitt „Hardware-Scanner“ und README-AppSettings nennen die 3 Keys, Default aus, Geräte-Schalter, DE-Layout/Präfix/Enter | Review |

### 4. Geänderte Bestandstests
- Keine. `StandortEinstellungenControllerTests.cs` erhält nur den **neuen** Test `Save_EmptyInt_LeavesKeyUnchanged_AndSavesTheRest`,
  bestehende Tests sind unverändert. `HardwareScannerTests.cs` ist neu im Branch, der QA-Fix ergänzt dort eine Assertion (B-1).

### 5. CLAUDE.md-Checkliste
- Migration / SQL / FreshInstall / AgentJobs: entfällt (keine Schemaänderung, geprüft im Diff).
- Audit-Felder: keine neue Entität; das Speichern läuft über den bestehenden `SaveAtomicAsync`-Weg.
- Version 1.66.0 in **beiden** `AppVersion.cs` ✓, Anwender-Changelog `Help/Changelog.cshtml` ✓, Brain-Changelog
  [[2026-10-21-v1-66-0-hardware-barcodescanner]] ✓, feature-map ✓.
- README (AppSettings) ✓, Hilfeseite ✓.
- `docs/TESTSZENARIEN.md` TS-99 ✓; im QA-Lauf TS-99.4 präzisiert (Touch/PC, N-1) und TS-99.7 e ergänzt (B-1). Der **Dead-Key-Fall `~`** ist
  in TS-99.1 b vollständig beschrieben: Symptom, Ersatz-Präfix, Wiederholung. [[testszenarien-index]] Zeile 99 ✓.
- Keine neue Rolle und kein neuer Filter.

## Manuelle Test-Checkliste (Schranke 2, Mensch)

Vorher: aus dem Worktree publizieren (Deploy-Abschnitt) → Testsystem. Admin: Standorteinstellungen → Hardware-Scanner aktiv, Präfix
`~09`, Abstand leer. Netum AIDC: DE-Layout, Präfix `~09`, Suffix Enter, Ausgabe als Tastendrücke.

1. **TS-99.1 a:** Schalter aus: Quelltext ohne `hardware-scanner.js`, `<body>` ohne `data-hw-scan-*`. Scan in einen Spaltenfilter zeigt den
   Rohtext `~09<Code>` mit richtigen `y`/`z`/`-`.
2. **TS-99.1 b, Tottaste:** Erscheint `~` nicht sofort oder falsch, am Gerät und in den Einstellungen einen anderen Präfix setzen (z. B.
   `#09`) und wiederholen. Ergebnis in der Spec notieren.
3. **TS-99.1 c:** Schalter an, Abstand 50/30/100 testen und den kleinsten stabilen Wert eintragen. Ihn als **Zahl** speichern, nicht leer
   lassen (N-2).
4. **TS-99.2:** Inbound: Artikel-QR ins angetippte Artikelfeld ergibt „✓ Gescannt“, kein Absenden. Lagerplatz-Etikett ins Lagerplatzfeld.
5. **TS-99.3:** Transfer: drei Ziele nacheinander. Ohne angetipptes Feld kommt der Hinweis.
6. **TS-99.4:** Stückliste ohne Fokus: Hinweis. Touch-Gerät: Ergebnis bei offenem Kamera-Fenster notieren (N-1). PC: Knopf per Tab
   fokussieren und scannen, der Kommissionier-Ablauf startet. Wagen-Etikett ins Ziel-Lagerplatzfeld.
7. **TS-99.5:** FA-Liste und Leitstand: FA-Filter und Artikel-Filter werden gesetzt und angewendet.
8. **TS-99.6:** Articles/Info ohne Fokus: Artikelnummer gefüllt.
9. **TS-99.7 a–d:** BDE: Anmeldung per Scan, danach Haupt-FA ohne Fokus ins FA-Feld (Gruppe klappt auf), Sub-FA direkt, `FA,AG` mit Fokus.
10. **TS-99.7 e (B-1):** dasselbe in Firefox am PC. Kein Operator-Wechsel, keine Meldung „Unbekannte Personalnummer“.
11. **TS-99.8:** Server-Spaltenfilter: Der Code steht drin und der Filter wird angewendet. Textarea: Einfügen an der Cursorposition, kein Absenden.
12. **TS-99.9:** Startseite: Hinweis „nicht verwendbar“, verschwindet nach ~5 s. `<b>` erscheint als Text. Den **ersten** Hinweis mit
    Screenreader prüfen (N-4).
13. **TS-99.10:** Mein Profil → „Dieses Gerät“ aus: sofort Rohtext. Am anderen Gerät weiter aktiv. Danach wieder einschalten.
14. **TS-99.11:** Simulation am PC: `~09ABC{Enter}` schnell wird als Scan erkannt. `~09` und `a~b` langsam getippt bleiben vollständig.
    AltGr-Zeichen funktionieren.
15. **TS-99.12:** Standorteinstellungen: Speichern mit leerem Abstand gelingt. Abstand 80 → `data-hw-scan-abstand="80"`. Nicht-Admin hat keinen Zugriff.
16. **TS-99.13:** Kamera-Scan auf Inbound, Bom und ProductionOrders unverändert.
17. **Negativfälle a–e:** Passwortfeld, US-Layout, offenes Kamera-Modal, EAN „Nicht gefunden“, Bildschirmtastatur unverändert.
18. **Entscheidung N-1:** Stückliste-Artikel per Hardware-Scanner am Handheld nötig? Wenn ja, Folgespec.
