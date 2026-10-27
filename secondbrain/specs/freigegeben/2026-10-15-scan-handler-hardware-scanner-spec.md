---
type: spec
title: Scan-Handler fuer Hardware-Barcodescanner (Tastatur-Eingabe mit Praefix)
slug: 2026-10-15-scan-handler-hardware-scanner-spec
status: Testbereit
created: 2026-10-15
updated: 2026-10-27
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

> [!important] Freigabe-Nachtrag 6 vom 2026-10-27 — Sichtbares Scan-Feld „Scan bereit“ auf der Einbuchung, v1.68.1 (geht dem Rumpf und Nachtrag 1–5 vor)
> Gerald 2026-10-27: Pilot aus Nachtrag 5 greift am Netum nicht. Befund Diagnose (v1.68.0, alle drei Geräte-Schalter an, Einbuchung):
> nur `keydown key="Unidentified" keyCode=0` und `Enter`, **keine** `input`-Zeile, kein „über Scan-Feld (ohne Fokus)“ – der Text
> erreicht das unsichtbare Feld nicht. Stand v1.68.0: Feld `#hwScanOhneFeld` mit `aria-hidden="true"`, `tabindex="-1"`,
> `inputmode="none"` (`Views/StockMovements/Inbound.cshtml:11-12`), CSS 1×1 px, `opacity:0`, `pointer-events:none` (`site.css:1445`),
> Fokus nur per Skript (`hardware-scanner.js:308-320`). Ursache ungeprüft: vermutlich findet die Netum-Fokus-Eingabe ein verborgenes/
> unsichtbares Feld nicht, oder das Feld hat beim Scan keinen Fokus. Ändert Nachtrag 5 Punkt 2; Punkte 1, 3, 4, 5 gelten weiter.
> 1. **Sichtbares, echtes Eingabefeld statt unsichtbarem**: kleines Feld „Scan bereit“ (ca. 140×30 px) gut sichtbar oben auf der
>    Einbuchung, nur wenn der Geräte-Schalter „Scannen ohne Feld“ an ist. **Ohne** `aria-hidden`, **ohne** `opacity:0`/1-px-Trick,
>    **ohne** `pointer-events:none`; antippbar. `inputmode="none"` bleibt (keine Bildschirmtastatur), `autocomplete="off"`.
>    Eingegebener Text ist nur Durchlauf: nach jedem Scan wird das Feld geleert.
> 2. **Fokus-Anzeige**: Hat das Feld den Fokus → deutlich hervorgehoben (z. B. grüner Rahmen, Text „Scan bereit“); sonst grau
>    („Scan: tippen zum Aktivieren“). Antippen setzt den Fokus selbst (Benutzergeste).
> 3. **Automatischer Fokus wie bisher** (Laden, Blur eines Feldes, Schließen Fenster/Dropdown), nur ohne Scrollen/Springen.
> 4. **Diagnose erweitern**: Jede Diagnose-Zeile (auch einzelne `keydown`) zeigt das aktive Element (`#id`, sonst Tag, bzw. `body`).
>    Zusätzlich je Fokuswechsel des Scan-Felds eine Zeile „Scan-Feld Fokus an/aus“.
> 5. Version **v1.68.1**, Tests (Quelltext-Wächter an das neue Feld anpassen: kein `aria-hidden`, kein `opacity:0`), TS-99-Unterpunkt
>    aus Nachtrag 5 anpassen (Netum, Einbuchung: „Scan bereit“ grün → Scan ohne Antippen eines Formularfelds), Hilfe; nur Web.

> [!important] Freigabe-Nachtrag 5 vom 2026-10-23 — Pilot „Scannen ohne Feld“ nur auf der Einbuchung, v1.67.2 (geht dem Rumpf und Nachtrag 1–4 vor)
> Gerald 2026-10-23 nach dem Netum-Test (TS-99.17 a bestanden): Die Erkennung soll auch funktionieren, ohne dass ein Textfeld aktiv ist.
> Befund Diagnose: Ohne Feld mit Fokus sendet der Netum („Fokus-Eingabe“) nur `keydown key="Unidentified" keyCode=0` und Enter, keinen Text.
> 1. **Nur Seite Einbuchung** (`StockMovements/Inbound`) als Pilot. Andere Seiten unverändert.
> 2. **Unsichtbares Scan-Feld** (kein Bildschirmtastatur: `inputmode="none"`, für Screenreader verborgen, kein Scrollen/Springen beim
>    Fokussieren). Es erhält den Fokus, wenn kein anderes Eingabefeld/Knopf den Fokus hat: beim Laden, nach Blur eines Feldes, nach
>    dem Schließen eines Fensters/Dropdowns. Text darin läuft durch die Text-Erkennung aus Nachtrag 4 (Präfix).
> 3. **Nur mit Geräte-Schalter** „Scannen ohne Feld“ in Mein Profil (nur dieses Gerät, localStorage, Standard aus). Aus → Seite
>    verhält sich exakt wie v1.67.1.
> 4. **Zuordnung nach Inhalt** (nur für Scans über das unsichtbare Feld):
>    - Code mit `;` (Artikel-QR) → Feld **Artikel** (wie Kamera/Hardware-Scan; FA-Übernahme nur bei `QrMitFaNummer`).
>    - Code ohne `;` → gibt es einen Lagerplatz mit genau diesem Code, wird er im Feld **Lagerplatz** gewählt; sonst als Artikelnummer
>      ins Feld Artikel.
>    - Passt nichts → Hinweis „Code … nicht zuordenbar“, nichts geändert.
>    Reihenfolge Artikel/Lagerplatz beliebig. Das Formular wird nie durch den Scan abgeschickt.
> 5. Antippen eines Feldes oder Kamera-Knopfs hat Vorrang (bisherige Regeln). Scanner-Diagnose zeigt „über Scan-Feld (ohne Fokus)“ und
>    die Zuordnung.
> 6. Version **v1.67.2**, Tests (Node-Kern/Quelltext-Wächter), neuer TS-99-Unterpunkt (Netum, Einbuchung ohne Antippen), Hilfe; nur Web.

> [!important] Freigabe-Nachtrag 4 vom 2026-10-23 — Fokus-Eingabe-Geräte (Netum), v1.67.1 (geht dem Rumpf und Nachtrag 1–3 vor)
> Befund am Gerät (Gerald 2026-10-23): Der Netum AIDC hat keinen Tastatur-Emulationsmodus. Seine Ausgabe „Fokus-Eingabe“ schreibt den
> ganzen Code (`~09<Code>` + Enter) direkt in das Feld mit Fokus; einzelne Tastenanschläge kommen nicht (Android meldet keyCode 229 bzw.
> nur `input`-Ereignisse). Ohne Feld mit Fokus gibt das Gerät nichts aus; „Broadcast senden“ ist für den Browser unbrauchbar.
> 1. **Erkennung am eingefügten Text:** Landet in einem Eingabefeld (inkl. Select2-Suchfeld, Kamera-Fenster-Kontext, BDE-Scanfeld)
>    ein Text, der mit dem Präfix beginnt, wird der Präfix entfernt und der Rest wie ein Hardware-Scan über dieselbe Kette ausgewertet
>    (Auswahlfeld wählen, Filter anwenden, Stückliste kommissionieren, offenes Kamera-Fenster als Ziel). Das Feld enthält danach nicht den
>    Präfix. Ein Enter-Suffix beendet den Scan; ohne Enter gilt die bisherige Zeitgrenze.
> 2. **Tastenanschlag-Erkennung bleibt** unverändert für Scanner mit Tastatur-Emulation.
> 3. **Diagnose-Anzeige:** Schalter „Scanner-Diagnose“ in Mein Profil, nur für dieses Gerät (localStorage wie „Dieses Gerät“), Standard aus.
>    Bei an: unten auf jeder Seite die letzten ca. 30 `keydown`- und `input`-Ereignisse mit Taste/keyCode/isComposing/Abstand in ms bzw.
>    eingefügtem Text und der Entscheidung des Skripts (z. B. „Präfix erkannt (Text)“, „ignoriert: keyCode 229“, „Scan an Feld X“,
>    „kein Ziel“); Knopf „Kopieren“. Keine Daten an den Server.
> 4. **Grenze dokumentieren** (Hilfe, Hinweistext): Bei Geräten mit Fokus-Eingabe zuerst Feld oder Kamera-Knopf antippen; ohne Fokus
>    kommt nichts an, Regel (d) „einziges Ziel“ und der Hinweis „bitte zuerst das Feld antippen“ greifen dort nicht.
> 5. Version **v1.67.1** (Bündel steht auf v1.67.0), Tests (Node-Kern, Quelltext-Wächter) und neue TS-99-Unterpunkte; nur Web.

> [!important] Freigabe-Nachtrag 3 vom 2026-10-22 — Nachbesserung v1.66.2 (geht dem Rumpf und Nachtrag 2 vor)
> Gerald 2026-10-22 auf die Rückfrage aus dem Bericht v1.66.1:
> 1. Das Verhalten aus Nachtrag 2 Punkt 1 (offenes Kamera-Fenster nimmt den Hardware-Scan an, Auswertung wie ein
>    Kamera-Treffer, Fenster schließt) gilt auf **allen Seiten mit mehr als einem Scan-Feld**: Wareneingang, Ausgang,
>    Umbuchung, Lagerplatz, Bestandsübersicht, FA-Liste, Leitstand, OSEON und der Ziel-Lagerplatz auf der Stückliste.
>    Je Kamera-Knopf freischalten wie bei „Artikel scannen“ (eine Zeile im HTML); der Wächter-Test aus v1.66.1, der genau
>    eine freigeschaltete Stelle sichert, wird auf die vollständige Liste umgestellt.
> 2. **Der Kamera-Knopf bleibt überall unverändert** (nicht alle Geräte haben einen Barcode-Scanner). Ohne offenes
>    Fenster gilt die bisherige Ziel-Regel.
> 3. Version **v1.66.2**, Tests und TS-99-Unterpunkte ergänzen.

> [!important] Freigabe-Nachtrag 2 vom 2026-10-21 — Nachbesserung v1.66.1 (geht dem Rumpf und AK 10 vor)
> Gerald 2026-10-21 nach dem QA-Bericht (Befunde N-1/N-2 im Abschnitt „QA-Nachweis“):
> 1. **Stückliste am Handheld:** Ist das Kamera-Fenster der Artikel-Erfassung auf der Stückliste geöffnet, gilt es als
>    angetipptes Ziel. Ein Hardware-Scan landet dort und läuft durch dieselbe Auswertung wie ein Kamera-Treffer (Artikel-QR
>    kommissionieren). AK 10 Variante B (nur Hinweis) ist damit für diesen Fall überholt. Ohne offenes Fenster gilt die
>    bisherige Ziel-Regel.
> 2. **Bereichsprüfung „Größter Zeichenabstand“** (`HardwareScannerMaxZeichenabstandMs`): Werte < 1 oder unplausibel groß werden
>    beim Speichern mit Meldung abgewiesen (Grenzen im Dev-Lauf festlegen und begründen). Leer speichern setzt auf den
>    Standard 50 zurück; das noch nie gespeicherte Feld blockiert das Speichern der Maske weiterhin nicht.
> 3. Version **v1.66.1**, Tests und Szenarien als Erweiterung von **TS-99** (neue Unterpunkte), bestehende TS-99-Punkte bleiben.

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

*Final QA-Lauf v1.68.1 (2026-10-27), aus dem echten Diff `ca48b22d..8540c4d6` (`69bb4fd1` + QA-Fix `8540c4d6`):* geändert sind nur
`IdealAkeWms/` (`wwwroot/js/hardware-scanner.js`, `wwwroot/css/site.css`, `Views/StockMovements/Inbound.cshtml`, `Views/Help/Index.cshtml` +
`Changelog.cshtml`, `AppVersion.cs`), `IDEALAKEWMSService/AppVersion.cs` (nur Version 1.68.1), Tests und `docs/TESTSZENARIEN.md`.
- **Web-App:** ja. **Service:** funktional nichts, nur die Version; mitpublizieren, damit beide 1.68.1 zeigen. **Migration:** nein (nichts
  unter `*/Migrations/`, kein `SQL/`, FreshInstall und `SQL/AgentJobs/` unberührt, kein Seed, keine neue AppSetting).
  ```
  dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
  dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService
  ```
- Ablauf: aus dem Worktree publizieren → Testsystem → TS-99.19 (a–q) + Regression TS-99.17 a/i → Merge. Nach dem Merge nur dann erneut aus
  `main` publizieren, wenn der Merge getestete Dateien mit parallelen `main`-Änderungen zusammengeführt hat.
- Am Netum vor dem Test: Browser-Cache leeren bzw. hart neu laden (JS und CSS geändert), „Scannen ohne Feld“ und Diagnose an.

*Final QA-Lauf v1.67.2 (2026-10-27), aus dem echten Diff `a2651019..a9462036` (`20b573de` + QA-Fix `a9462036`, nur Doku):* geändert sind
nur `IdealAkeWms/` (`wwwroot/js/hardware-scanner.js`, `wwwroot/js/barcode-scanner.js`, `wwwroot/css/site.css`, `Views/StockMovements/Inbound.cshtml`,
`Views/Account/Profile.cshtml`, `Views/Help/Index.cshtml` + `Changelog.cshtml`, `AppVersion.cs`), `IDEALAKEWMSService/AppVersion.cs` (nur
Version 1.67.2, Datum bleibt 2026-10-23), Tests und `docs/TESTSZENARIEN.md`.
- **Web-App:** ja. **Service:** funktional nichts, nur die Version. Mitpublizieren, damit beide 1.67.2 zeigen.
  **Migration:** nein (nichts unter `*/Migrations/`, kein `SQL/`, FreshInstall und `SQL/AgentJobs/` unberührt, keine neue AppSetting; der
  Schalter „Scannen ohne Feld“ liegt nur im `localStorage` des Geräts).
  ```
  dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
  dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService
  ```
- Ablauf: aus dem Worktree publizieren → Testsystem → TS-99.19 (+ Regression TS-99.17 a/i, TS-99.16 Inbound) → Merge.
  Nach dem Merge nur dann erneut aus `main` publizieren, wenn der Merge getestete Dateien mit parallelen `main`-Änderungen zusammengeführt hat.
- Am Netum vor dem Test: Browser-Cache leeren bzw. hart neu laden (geänderte JS/CSS-Dateien), dann in „Mein Profil“ „Scannen ohne Feld“ einschalten.

*Nachtrag QA-Lauf v1.67.1 (2026-10-23) aus dem echten Diff `4bfa4576..a2651019` (`eb4b28ea` + QA-Fix `a2651019`):* geändert sind nur
`IdealAkeWms/` (`wwwroot/js/hardware-scanner.js`, `wwwroot/js/barcode-scanner.js`, `wwwroot/css/site.css`, `Views/Account/Profile.cshtml`,
`Views/Help/Index.cshtml` + `Changelog.cshtml`, `Models/Standort/StandortSettingsCatalog.cs` nur Hinweistext, `AppVersion.cs`),
`IDEALAKEWMSService/AppVersion.cs` (nur Version 1.67.1 / 2026-10-23), Tests und `docs/TESTSZENARIEN.md`.
- **Web-App:** ja. **Service:** nur Versionsstand, funktional nichts. Mitpublizieren, damit beide 1.67.1 zeigen (wie bei v1.66.x).
  **Migration:** nein (kein Eintrag unter `*/Migrations/`, kein `SQL/`, FreshInstall und `SQL/AgentJobs/` unberührt, keine neue AppSetting).
- Ablauf: aus dem Worktree publizieren (beide Befehle unten) → Testsystem → TS-99.17/99.18 (+ Regression TS-99.1 ff., 99.16) → Merge.
  Nach dem Merge nur dann erneut aus `main` publizieren, wenn der Merge getestete Dateien mit parallelen `main`-Änderungen zusammengeführt hat.
- Am Netum vor dem Test: Ausgabe „Fokus-Eingabe“, Präfix `~09`, Suffix Enter. Browser-Cache am Gerät leeren bzw. hart neu laden
  (geänderte JS-Dateien).

*Stand v1.66.2 (darunter, unverändert gültig für die älteren Teile):*

*Final (QA-Lauf v1.66.2, 2026-10-22) aus dem echten Diff `c27f3b6d..845e38c9` (v1.66.0, v1.66.1 + QA-Fix, v1.66.2 + QA-Fix Datum).
v1.66.2 (`1d78e7f5..845e38c9`) ändert nur `IdealAkeWms/` (10 Scan-Views: Attribut `data-hw-scan-kamera-ziel`; `barcode-scanner.js`: nur
Kommentar; Help/Index + Changelog; AppVersion), `IDEALAKEWMSService/AppVersion.cs`, Tests und `docs/`.*

- **Web-App:** ja — `IdealAkeWms/` (v1.66.0: hardware-scanner.js neu, barcode-scanner.js, _Layout, BdeTerminal/Index, Account/Profile,
  Help, AppSettingKeys, StandortSettingsCatalog, StandortEinstellungenController; v1.66.1 zusätzlich Picking/Bom.cshtml,
  barcode-scanner.js, hardware-scanner.js, StandortSettingsCatalog, StandortEinstellungenController, Help/Index + Changelog, AppVersion).
- **Service:** ja, aber **nur Versionsstand** — einzige Änderung `IDEALAKEWMSService/AppVersion.cs` (→ 1.66.2, Datum 2026-10-22), funktional nichts.
  Mitpublizieren, damit Web und Dienst dieselbe Version zeigen.
- **Migration:** nein (kein neuer Eintrag unter `*/Migrations/`, kein `SQL/`-Skript, FreshInstall und `SQL/AgentJobs/` unberührt).
  AppSettings-Zeilen entstehen beim ersten Speichern der Standorteinstellungen (ab v1.66.1 auch `HardwareScannerMaxZeichenabstandMs`
  mit `""` = Standard 50).
- **Ablauf des Menschen:** aus dem Worktree publizieren → Testsystem → TS-99 (inkl. 99.14–99.16) testen → Merge.
  ```
  dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
  dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService
  ```
- Nach dem Merge nur dann erneut aus `main` publizieren, wenn der Merge getestete Dateien mit parallelen `main`-Änderungen
  zusammengeführt hat.
- Nach Deploy: Schalter bleibt aus, bis ein Admin ihn je Standort einschaltet; Scanner-Konfiguration der Geräte (DE-Layout,
  Präfix `~09` bzw. Ersatz ohne Tottaste, Enter-Suffix, Ausgabe als Tastendrücke) vor dem Einschalten. Ein vorher gespeicherter
  Zeichenabstand außerhalb 10–500 wirkt als 50 und muss vor dem nächsten Speichern der Maske korrigiert werden.

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

## Nachbesserung v1.66.1 (Dev-Lauf 2026-10-21, Freigabe-Nachtrag 2)

Basis `789d0a78` (v1.66.0), Commit `c72be07e` (wip feature-complete), **v1.66.1**, TS-99.14/99.15 neu.

**N-1 Stückliste am Handheld**
- `barcode-scanner.js openScannerModal`: Das Kamera-Fenster merkt sich sein Ziel (`modal.dataset.zielId`).
- Neu `hwScanKameraZiel`: Ist das Fenster offen und trägt der Knopf seines Ziels `data-hw-scan-kamera-ziel`, wird der Scan vor allen
  anderen Regeln verarbeitet, in derselben Abfolge wie ein Kamera-Treffer (`startCameraScanner`): `_scannerClosing = true`, globale
  `processScannedValue`, `closeScannerModal()`.
- Opt-in nur an `btnScanBomArticle` (`Picking/Bom.cshtml:67-68`). Ein Wächter verlangt genau eine Stelle mit dem Attribut.
- Ohne offenes Fenster gilt die bisherige Regel. Ebenso bei offenem Fenster eines anderen Knopfs, z. B. Ziel-Lagerplatz der Stückliste.

**N-2 Zeichenabstand**
- `StandortField` hat neu die optionalen Werte `Min`/`Max`/`EmptyResetsToDefault`.
- `HardwareScannerMaxZeichenabstandMs`: Min 10, Max 500, leer = Standard. Leer speichern schreibt `""`, das Layout nimmt dann 50. Das
  nie gespeicherte Feld blockiert nicht. Andere Int-Felder bleiben bei leer unverändert, wie in v1.66.0.
- Außerhalb von 10–500 erscheint „'Größter Zeichenabstand (ms)' muss zwischen 10 und 500 liegen (war: …)“ und es wird nichts
  gespeichert, wie beim bestehenden Fehler bei ungültigen Zahlen.
- **Begründung der Grenzen:**
  - Unter 10 ms schwanken Browser-Zeitstempel und der Ereignistakt unter Android stärker, als ein Scanner pro Zeichen braucht. Scans
    würden abbrechen.
  - Ab etwa 500 ms läge der Wert im Bereich schnellen menschlichen Tippens. Ein getippter Präfix würde als Scan erkannt, und ein
    getipptes `~` erschiene sichtbar verzögert.
- Client: `HwScanCore.abstand(raw)` nimmt nur 10–500, sonst 50 (`hardware-scanner.js`). Ein negativer Wert schaltet die Erkennung nicht
  mehr still ab.

**Andere Seiten mit Kamera-Fenster (nur geprüft, nicht ausgeweitet)**
- Alle 14 Scan-Seiten öffnen dasselbe Fenster über `openScannerModal`.
- Bei offenem Fenster gilt dort weiter die normale Regel: Ein Ziel im Fokus bekommt den Scan, das Fenster bleibt offen. Liegt der Fokus
  nicht auf dem Knopf (am Touch-Gerät üblich), erscheint der Hinweis bzw. greift das einzige Ziel.
- Kandidaten für dasselbe Attribut, falls gewünscht: alle Knöpfe, deren Ziel ohne Fokus mehrdeutig ist. Das sind Inbound, Outbound und
  Transfer (Artikel + Lagerplatz), LocationTransfer, StockOverview, ProductionOrders und PickingLeitstand (FA + Artikel), OseonIndex sowie
  der Ziel-Lagerplatz der Stückliste.
- Je Knopf genügt eine Zeile `data-hw-scan-kamera-ziel` und eine Anpassung des Wächters. Das wäre deine Entscheidung.

**Nachweis**
- `dotnet build IdealAkeWms.slnx`: 0 Fehler, 28 Warnungen (Bestand).
- `dotnet test`: Web **2153 bestanden, 1 übersprungen** (Bestand), Dienst **316 bestanden**.
- `node …/hardware-scanner.check.js`: `hardware-scanner core ok (27 Pruefungen)`, 11 davon neu für `abstand`.
- **Geänderter Bestandstest** (eigener Test aus v1.66.0, nur im Bündel): `Save_EmptyInt_LeavesKeyUnchanged_AndSavesTheRest` heißt jetzt
  `Save_EmptyInt_NeverBlocks_ScannerResetsToDefault_OtherIntsStayUnchanged`. Erwartet ist für den Scanner-Abstand nun `""` statt
  „Key fehlt“, laut Nachtrag 2. Neu sind `Save_ScannerAbstandAusserhalb10Bis500_WritesNothing_WithMessage` (9/0/−5/501),
  `Save_ScannerAbstandAnDenGrenzen_IsSaved` (10/500) und `HardwareScannerTests.Stueckliste_OffenesKameraFenster_IstZiel_WieKameraTreffer`.

## Nachbesserung v1.66.2 (Dev-Lauf 2026-10-22, Freigabe-Nachtrag 3)

Basis `1d78e7f5` (v1.66.1), Commit `543edbe2` (wip feature-complete), **v1.66.2**, TS-99.16 neu, TS-99.14 b) und Negativfall c) angepasst.

**Seiten-Liste gegen den Code belegt.** Regel des Nachtrags: jede Seite mit mehr als einem Scan-Feld. Erfasst sind alle Kamera-Knöpfe
(`id="btnScan…"` in `Views/**`). Ihre Register-Einträge kommen aus `initScanner`/`initTextInputScanner`, auch aus dem globalen Teil
`barcode-scanner.js:488-491`.

| Seite | freigeschaltete Knöpfe (`data-hw-scan-kamera-ziel`) |
|---|---|
| StockMovements/Inbound (Wareneingang) | `btnScanArticle`, `btnScanStorageLocation` |
| StockMovements/InboundBulk (Wareneingang Sammel) | `btnScanStorageLocation`, `btnScanArticleBulk` |
| StockMovements/Outbound (Ausgang) | `btnScanArticle`, `btnScanStorageLocation` |
| StockMovements/Transfer (Umbuchung) | `btnScanArticle`, `btnScanSourceLocation`, `btnScanStorageLocation` |
| StockMovements/LocationTransfer (Lagerplatz) | `btnScanSourceLocation`, `btnScanTargetLocation` |
| StockOverview/Index (Bestandsübersicht) | `btnScanArticleFilter`, `btnScanLocationFilter`, `btnScanPO` |
| ProductionOrders/Index (FA-Liste) | `btnScanOrderNumber`, `btnScanArticleNumber` |
| PickingLeitstand/Index (Leitstand) | `btnScanOrderNumber`, `btnScanArticleNumber` |
| Tracking/OseonIndex (OSEON) | `btnScanCustomerOrder`, `btnScanArticle` |
| Picking/Bom (Stückliste) | `btnScanTargetLocation` (neu), `btnScanBomArticle` (seit v1.66.1) |

- 21 neue Knöpfe, insgesamt 22.
- **Abweichung von der Aufzählung:** InboundBulk ist nicht namentlich genannt. Die Seite hat aber zwei Scan-Felder und fällt damit unter die
  Regel „mehr als ein Scan-Feld“. Ihr Artikel-Ziel `bulkScanBuffer` ist wie `bomScanArticle` ein verstecktes Feld und ohne Fenster nur über
  den Knopf erreichbar.
- **Bewusst nicht freigeschaltet** (genau ein Scan-Feld, dort greift schon Regel d):
  - Articles/Info (`btnScanArticleInfo`)
  - StockMovements/OutboundAll (`btnScanStorageLocation`)
  - StockMovements/Index (`btnScanProductionOrder`)
- BDE-Terminal: nicht betroffen, es nutzt sein eigenes Scan-Feld ohne Kamera-Register.

**Unverändert:** Kamera-Knöpfe, Klick-Handler und Kamera-Erfassung. Geändert ist je Knopf nur das Attribut sowie ein Kommentar in
`barcode-scanner.js` (`hwScanKameraZiel`); die Logik ist dieselbe wie in v1.66.1. Ohne offenes Fenster gilt die bisherige Ziel-Regel.

**QA-Fix v1.66.1 auf allen Seiten:** Der Wächter belegt, dass keine View einen eigenen `Html5Qrcode` baut. Jeder Kamera-Treffer läuft also
über `startCameraScanner` und dessen Sperre `if (_scannerClosing || _activeScanner !== html5QrCode) return;`.

**Wächter-Test:** `HardwareScannerTests.OffenesKameraFenster_IstZiel_WieKameraTreffer`, vormals `Stueckliste_…`. Er prüft jetzt die
vollständige Liste Seite|Knopf (22 Einträge, `BeEquivalentTo`, keiner mehr und keiner weniger) und dazu „keine View mit
`new Html5Qrcode`“.

**Zusatzfund (nicht mitgebaut):** Auf OSEON bindet der globale `initScanner('btnScanArticle','ArticleId')` (`barcode-scanner.js:488`)
einen zweiten Klick-Handler an den Artikel-Knopf, obwohl die Seite kein `ArticleId` hat. Beide öffnen das Fenster. Ohne Kamera gewinnt
das Fenster der Seite (`filterArticle`). Mit Kamera entscheidet die Reihenfolge der zwei Kamera-Freigaben, siehe QA-Befund Q3-2. Der Fehler bestand schon vorher. Abhilfe wäre eine Zeile in `initScanner`: ohne Ziel-Element nicht
binden.

**Nachweis**
- `dotnet build IdealAkeWms.slnx`: 0 Fehler.
- `dotnet test`: Web **2153 bestanden, 1 übersprungen** (Bestand), Dienst **316 bestanden**.

## Nachbesserung v1.67.1 (Dev-Lauf 2026-10-23, Freigabe-Nachtrag 4)

Basis `4bfa4576` (v1.67.0). Commit `eb4b28ea` (wip feature-complete). Nur Web, keine Migration.

**Text-Erkennung (Punkt 1):**
- `hardware-scanner.js`: neuer reiner Kern `createText` + `eingefuegt(alt, neu)` (Diff über gemeinsamen Anfang/Ende).
  - Eine Eingabe-Folge (Abstand ≤ Zeichenabstand) wird gegen den Feldwert **vor** der Folge verglichen (`beforeinput`/`focusin`).
  - Beginnt der eingefügte Text mit dem Präfix: Feld sofort auf den Stand davor zurück, Cursor an die Einfügestelle.
    `stopImmediatePropagation` in der Capture-Phase, damit Seiten-Handler (Select2-Suche, Live-Filter) den Präfix nie sehen.
  - Ende: Enter (`keydown` key `Enter`/keyCode 13, **vor** der 229-Ausnahme abgefangen und verschluckt), Zeilenumbruch im Text oder
    `beforeinput insertLineBreak`, sonst die Zeitgrenze.
  - Danach `zustellen(scan)`, dieselbe Kette wie der Tasten-Scan: Kamera-Fenster → Regel (a) Fokus → (b) freies Feld (einfügen +
    synthetisches Enter) → (d) → Hinweis. Damit sind Select2-Suchfeld (Regel a), BDE `scanFaAg` und Spaltenfilter (Regel b) abgedeckt.
  - Zeichenweise Fokus-Eingabe (jede Taste ein `input`, keyCode 229) läuft über denselben Kern: der Präfix-Anfang geht durch, ab dem
    vollen Präfix wird zurückgesetzt.
- **Keine Doppelauswertung:** Nur `isTrusted`-Ereignisse; eigene `input`-Ereignisse aus `feldFuellen`/Replay sind synthetisch. Während
  ein Tasten-Scan läuft (`core.pending()`), wird Text ignoriert. Tasten, die der Tasten-Kern verschluckt, erzeugen kein `input`.
- Passwortfelder: `freiesFeld` kennt `password` nicht; auch in der Diagnose ohne Taste/Text.
- **Kamera-Fenster:** Ein Fokus-Eingabe-Gerät gibt ohne Feld mit Fokus nichts aus, und das Fenster hatte kein Feld. Darum bekommt es bei
  Standort-Schalter an ein Feld „Handscanner“ (`inputmode="none"`, keine Bildschirmtastatur), das beim Öffnen den Fokus bekommt.
  `hwScanKameraZiel` erkennt das offene Fenster als Ziel, wenn der Knopf `data-hw-scan-kamera-ziel` trägt **oder** der Fokus im
  Handscanner-Feld liegt. Sonst würde auf Seiten mit einem Scan-Feld (Artikel-Info) Regel (b) den Code ins Fensterfeld schreiben.

**Tasten-Erkennung (Punkt 2):** Kern `create` unverändert: die bisherige Node-Prüfung (Stand `4bfa4576`, 27 Prüfungen) läuft gegen das neue Skript grün. Der `keydown`-Kopf ist umgebaut:
Diagnose-Zeile je Entscheidung, Enter der Fokus-Eingabe vor der 229-Ausnahme. Die Regeln sind gleich: nur `isTrusted`, IME/229
nicht abfangbar, Passwort, Gerät aus, AltGr.

**Diagnose (Punkt 3):**
- „Mein Profil“ → Karte „Dieses Gerät“ → Schalter „Scanner-Diagnose“ (`localStorage` `wms.hardwareScanner.diagnose` = `an`, Standard
  aus, wirkt ab dem nächsten Seitenaufruf).
- `hardware-scanner.js` baut eine Bootstrap-Karte vor dem Seitenfuß: `pre` mit den letzten 30 Zeilen `+… ms`, Taste/keyCode/isComposing
  bzw. `input`-Typ und Text sowie die Entscheidung (z. B. „Präfix erkannt (Text)“, „ignoriert: keyCode 229 / Komposition“,
  `Scan "…" an Feld #…`, „kein Ziel“).
- Knopf „Kopieren“ (Clipboard-API, sonst Text markieren mit Hinweis), mit Präfix, Zeichenabstand und Browser-Kennung. Keine
  Server-Anfrage (Wächter-Test).
- Grenze: Die Diagnose läuft im Scanner-Skript und ist daher nur bei Standort-Schalter an verfügbar (die Profil-Karte ist dann auch
  nur sichtbar).

**Hilfe/Hinweis (Punkt 4):**
- Hilfe, Scanner-Abschnitt: Absatz „Geräte mit Fokus-Eingabe“ (zuerst Feld oder Kamera-Knopf antippen; Regel 3/4 greifen nicht);
  „Scanner einrichten“ nennt Fokus-Eingabe und dass Broadcast im Browser nicht geht; neuer Punkt „Scanner kommt nicht an?“ (Diagnose).
- Hinweistext in den Standorteinstellungen (`HardwareScannerAktiv`) ergänzt.

**Tests (Punkt 5):**
- `hardware-scanner.check.js` +18 Prüfungen zum Text-Kern, jetzt 45: ganzer Code, Zeilenumbruch, Zeitgrenze, zeichenweise, zwei
  Ereignisse, Hand-Eingabe bleibt Text, Präfix ohne Code, Löschen, verpasste Zeitgrenze, anderer Präfix.
- `HardwareScannerTests`: neu `FokusEingabe_TextErkennung_NurEchteEingaben_KeineDoppelauswertung`,
  `Diagnose_NurDiesesGeraet_NichtsAnDenServer` und `KameraFenster_HandscannerFeld_FuerFokusEingabe`.
- TS-99.17 (Netum, Fokus-Eingabe, a–m) und TS-99.18 (Diagnose, a–g). v1.67.1 ×2, Changelog.

**Abweichungen:**
- **Geänderter Bestandstest:** `DomTeil_PasswortIMEUndSynthetischeTastenNieAbfangen` prüfte die alte Kopfzeile
  `if (!e.isTrusted || e.isComposing || e.keyCode === 229 || geraetAus()) return;` wörtlich. Er prüft jetzt dieselben Regeln einzeln:
  `isTrusted` als erste Zeile, 229/Komposition, Gerät aus und Passwort ohne Protokoll der Taste.
- **Neues Bedienelement:** das Feld „Handscanner“ im Kamera-Fenster. Nötig, weil Punkt 1 „offenes Kamera-Fenster“ für
  Fokus-Eingabe-Geräte sonst nicht erreichbar ist.
- DOM-Teil nur per Wegwerf-Mock in Node durchgespielt (Netum ganzer Text + Enter, zeichenweise 229). Kein jsdom, keine neue
  Abhängigkeit. Nachweis am Gerät: Manual-UAT TS-99.17.

**Grenzen (bewusst):**
- Ein von Hand **eingefügter** Text (Strg+V), der mit dem Präfix beginnt, gilt als Scan.
- Setzt das Gerät Text per IME-Komposition (`insertCompositionText`), kann das Zurücksetzen des Feldwerts die Komposition stören
  (TS-99.17 am Netum).
- Ob `inputmode="none"` die Fokus-Eingabe des Netum zulässt, zeigt erst TS-99.17 c.

**Nachweis:** `dotnet build IdealAkeWms.slnx` ergibt 0 Fehler, 28 Warnungen (alle schon vorher vorhanden). `dotnet test`: Web **2181
bestanden, 1 übersprungen** (schon vorher), Dienst **328 bestanden**. Node-Kern: „hardware-scanner core ok (45 Pruefungen)“.

## Nachbesserung v1.67.2 (Dev-Lauf 2026-10-23, Freigabe-Nachtrag 5)

Basis `a2651019` (v1.67.1). Commit `20b573de` (wip feature-complete). Nur Web, keine Migration. Nur Seite Einbuchung.

**Unsichtbares Scan-Feld (Punkt 1/2):**
- `Views/StockMovements/Inbound.cshtml`: `<input id="hwScanOhneFeld" data-hw-scan-ohne-feld data-artikel-ziel="ArticleId"
  data-lagerplatz-ziel="StorageLocationId" inputmode="none" tabindex="-1" aria-hidden="true">` **vor** dem `<form>` (ein Enter darin kann nie
  absenden). CSS `.hw-scan-ohne-feld`: `position: fixed` oben links, 1 px, `opacity: 0`, `pointer-events: none`. Nicht `display:none`/
  `visibility:hidden`, die nehmen keinen Fokus; `freiesFeld` braucht `getClientRects()`, darum 1 px statt 0.
- `hardware-scanner.js`: nur wenn `localStorage wms.hardwareScanner.ohneFeld = an` (Profil „Scannen ohne Feld“, Standard aus) und das Gerät
  nicht aus ist. `parken()` fokussiert das Feld mit `preventScroll`, wenn `activeElement` body/html ist oder Select2 den Fokus nach dem Schließen
  an seine Auswahl zurückgegeben hat (kein Dropdown offen). Auslöser: Laden, `focusout` (Capture), MutationObserver auf `body` (Select2-Dropdown
  und Kamera-Fenster hängen dort; Entfernen eines fokussierten Elements löst kein verlässliches `blur` aus).
- Text im Feld läuft unverändert durch die Text-Erkennung aus v1.67.1; ein Tasten-Scanner (USB) ebenso über den Tasten-Kern.

**Zuordnung (Punkt 3/4):**
- `zustellen` setzt `detail.ohneFeld`, wenn der Fokus im unsichtbaren Feld liegt; dann **nie** Regel (b)/(d) (sonst landete der Code im
  unsichtbaren Feld). `detail.hinweis(text)` zeigt den gelben Hinweis und schreibt die Diagnose, auch später aus dem AJAX-Ergebnis.
- `barcode-scanner.js` `hwScanOhneFeld`, aufgerufen **nach** `hwScanKameraZiel` (offenes Kamera-Fenster hat Vorrang): reiner Kern
  `HwScanCore.zuordnung` — mit `;` Artikel-QR → `processScannedValue(…, 'article', qrFaEnabled, faTargetId)` des Artikel-Ziels (FA nur bei
  `QrMitFaNummer`, wie Kamera); ohne `;` und Option mit **genau** diesem Code (getrimmt) → Lagerplatz direkt gesetzt + `change` +
  „✓ Gescannt“ (nicht über `processScannedValue`, das auch per `startsWith` trifft); sonst als Artikelnummer ins Artikel-Feld. Kein Treffer
  (AJAX 404) oder leer → Hinweis „Code … nicht zuordenbar“, nichts geändert.
- `processScannedValue` bekommt einen optionalen 6. Parameter `nichtGefunden` (Wrapper reicht ihn durch); alle anderen Aufrufer übergeben ihn
  nicht, Verhalten unverändert.
- Diagnose: `Scan "…" über Scan-Feld (ohne Fokus) → ArticleId (Artikel-QR | Artikelnummer)` bzw. `→ StorageLocationId (Lagerplatz)`.
- Angetipptes Feld/Knopf: hat den Fokus, `parken` greift nicht, es gelten die bisherigen Regeln (a)/(b).

**Tests (Punkt 5):** `hardware-scanner.check.js` +7 Prüfungen (52), u. a. beide Hannes-Etiketten, genauer Lagerplatz statt `startsWith`,
Trim, leer. Neu `HardwareScannerTests.ScannenOhneFeld_NurAufEinbuchung_UnsichtbarOhneTastatur_NurMitSchalter` (Feld nur in Inbound,
`inputmode`/`aria-hidden`/`tabindex`, vor `<form>`, Schalter Standard aus, Kamera-Vorrang, Diagnose-Text). TS-99.19 (a–m). Hilfe, Changelog,
v1.67.2 ×2 (Datum bleibt 2026-10-23). DOM-Teil per Wegwerf-Mock in Node durchgespielt (Netum-Text im unsichtbaren Feld → `ohneFeld`, keine
Regel (b), Feld leer).

**Abweichungen / Grenzen:**
- Nach einer Auswahl in einer **nativen** Liste (Lagerplatz von Hand) behält das `<select>` den Fokus; dann kommt ein Fokus-Eingabe-Scan erst
  nach Tippen auf eine freie Stelle an (Spec nennt Blur, Select2 und Kamera-Fenster; in Hilfe und TS-99.19 g beschrieben). Nicht mitgebaut.
- Mit Schalter an nimmt das Parken Select2 nach dem Schließen den Fokus ab; Tastatur-Bedienung am PC (Tab-Reihenfolge) ist dann gestört.
  Gedacht nur für den Handheld, Schalter je Gerät.
- Fokus auf einem `aria-hidden`-Element ist laut ARIA unschön, aber so verlangt („für Screenreader verborgen“); TS-99.19 m prüft TalkBack.
- Ob Android trotz `inputmode="none"` beim Fokussieren scrollt oder die Tastatur zeigt: TS-99.19 e am Gerät.

**Nachweis:** `dotnet build IdealAkeWms.slnx` 0 Fehler, 28 Warnungen (unverändert). `dotnet test`: Web **2182 bestanden, 1 übersprungen**
(vorher 2181 + neuer Wächter), Dienst **328 bestanden**. Node-Kern „hardware-scanner core ok (52 Pruefungen)“.

## Nachbesserung v1.68.1 (Dev-Lauf 2026-10-27, Freigabe-Nachtrag 6)

Basis `ca48b22d` (v1.68.0). Commit `69bb4fd1` (wip feature-complete). Nur Web (Dienst nur `AppVersion.cs`), keine Migration, kein Seed.
Nur Einbuchung. `processScannedValue` und die Zuordnung aus Nachtrag 5 Punkt 4 (`hwScanOhneFeld`, `HwScanCore.zuordnung`) unverändert.

**Sichtbares Feld (Punkt 1/2):**
- `Inbound.cshtml`: `#hwScanOhneFeld` bleibt vor dem `<form>`, jetzt **ohne** `aria-hidden` und `tabindex="-1"`, mit
  `aria-label="Scan-Feld"`, `placeholder="Scan: tippen zum Aktivieren"` und `hidden`. `inputmode="none"`, `autocomplete="off"` bleiben.
- `hardware-scanner.js`: nur bei Geräte-Schalter an `ohneFeld.hidden = false`; `focus`/`blur` setzen den Platzhalter „✓ Scan bereit“ /
  „Scan: tippen zum Aktivieren“ und schreiben die Diagnose „Scan-Feld Fokus an/aus“. Schalter aus → Feld bleibt `hidden`, nie fokussiert.
- `site.css` `.hw-scan-ohne-feld`: 30 px hoch, 15em breit (≈ 195 px), ohne Fokus grau gestrichelt (`#495057` auf `#f8f9fa`, ca. 8:1), mit Fokus
  grün durchgezogen + Schatten (`#0a3622` auf `#d1e7dd`, ca. 11:1). Zustand an Text **und** Rahmenform, nicht nur Farbe. Kein `opacity`,
  kein 1 px, kein `pointer-events`.
- Nach jedem Scan über das Feld: `ohneFeld.value = ''` in `zustellen` (vor der Auswertung).

**Fokus (Punkt 3):** `parken()` unverändert (Laden, `focusout`, MutationObserver, Select2 nach dem Schließen, `preventScroll`). `freiesFeld()`
lässt das sichtbare Feld wie bisher als Ziel der Text-Erkennung zu; „ohne Fokus“ bestimmt weiterhin `aktiv === ohneFeld`
(`[data-hw-scan-ohne-feld]`).

**Diagnose (Punkt 4):** `diag()` hängt an **jede** Zeile `[aktiv: #id | tag | body]` an (auch einzelne `keydown`).

**Tests/Doku (Punkt 5):** Wächter `ScannenOhneFeld_*` umgestellt (kein `aria-hidden`/`tabindex="-1"`, `hidden`, CSS-Regel ohne
`opacity`/`pointer-events`/`1px`, `:focus`-Regel, `hidden = false`, Fokus-Diagnose, Leeren, `[aktiv:`). TS-99.19 Vorbedingung um „Scan
bereit“ ergänzt, m) Screenreader liest „Scan-Feld“, neu n) Schalter aus = kein Feld. Hilfe-Absatz, Changelog, v1.68.1 ×2.
DOM-Teil per Wegwerf-Mock in Node durchgespielt (Feld wird sichtbar, `[aktiv: #hwScanOhneFeld]` je Zeile).

**Abweichungen:**
- **Geänderter Bestandstest:** `HardwareScannerTests.ScannenOhneFeld_NurAufEinbuchung_UnsichtbarOhneTastatur_NurMitSchalter` prüfte
  `aria-hidden="true"`, `tabindex="-1"` und `position: fixed` – vom Nachtrag ausdrücklich ersetzt (Testname bleibt).
- Breite ≈ 195 px statt ca. 140 px: Bei 140 px wird „Scan: tippen zum Aktivieren“ abgeschnitten.
- `tabindex="-1"` entfernt (Nachtrag nennt es nicht): ein sichtbares, antippbares Feld gehört in die Tab-Folge; am PC ohne Schalter ist es
  ohnehin `hidden`.
- Die Diagnose-Ergänzung `[aktiv: …]` wirkt bei Diagnose an auf allen Seiten (gemeinsames Skript), unabhängig vom Schalter „Scannen ohne Feld“.

**Nachweis:** `dotnet build IdealAkeWms.slnx` 0 Fehler, 28 Warnungen (unverändert). `dotnet test`: Web **2195 bestanden, 1 übersprungen**,
Dienst **328 bestanden**. Node-Kern „hardware-scanner core ok (52 Pruefungen)“.

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

## QA-Nachweis v1.66.1 (qa-agent, 2026-10-21, Freigabe-Nachtrag 2)

Geprüft im Bündel-Worktree, Diff `789d0a78..c72be07e` plus QA-Fix `1d78e7f5`. Web und Dienst **nicht** lokal gestartet (Auftrag).

### 1. Build und Tests (selbst ausgeführt, vor und nach dem QA-Fix)
- `dotnet build IdealAkeWms.slnx`: **0 Fehler**, 28 bzw. 25 Warnungen (Bestand, inkrementell unterschiedlich gezählt).
- `dotnet test` (vor und nach dem QA-Fix identisch):
  - `IdealAkeWms.Tests`: **2153 bestanden, 1 übersprungen, 0 fehlgeschlagen** (übersprungen = Bestand
    `ProductionOrderEagerCreateAgentJobTests.EagerCreate_TwoMergesProduce2Rows_PerNewOrder_AndAreIdempotent`).
  - `IDEALAKEWMSService.Tests`: **316 bestanden, 0 übersprungen, 0 fehlgeschlagen**.
- Direkt: `node IdealAkeWms.Tests/Js/hardware-scanner.check.js <absoluter Pfad>/hardware-scanner.js` → `hardware-scanner core ok (27 Pruefungen)`
  (Node v24.22.0; der Testlauf übergibt den absoluten Pfad, ein relativer scheitert an `require` — nur Aufruf-Detail).
- Index-Zeilenenden: `git ls-files --eol` → `i/lf` für alle 14 Dateien des Diffs und die 3 Dateien des QA-Fixes.

### 2. Unabhängiges Code-Review (selbst, in diesem Lauf)
Geprüft: kompletter Diff; `barcode-scanner.js` ganz (Modal-Aufbau, `startCameraScanner`, `scanFromFile`, `closeScannerModal`, Wrapper,
Listener); html5-qrcode `scanContext`/`foreverScan`/`stop` (`wwwroot/lib/html5-qrcode/html5-qrcode.min.js`); `Picking/Bom.cshtml`
(`bomScanArticle`-Change-Handler ab :767, Retry-Knopf um :1608, Init :726-727); `StandortEinstellungenController.Save` +
`BuildViewModelAsync`; `StandortEinstellungen/Index.cshtml` (Fehlerliste :15-24, Int-Feld :84); `StandortSettingsWriter.SaveAtomicAsync`;
`_Layout.cshtml:15-28`; `AppSettingKeys.cs:50`; Tests, TS-99.14/99.15, Hilfe, Changelog, README.

**Befunde**
- **Q-1 (behoben in `1d78e7f5`)** — Race Hardware-Scan und laufender Kamera-Decode. html5-qrcode ruft den Treffer-Callback aus
  `decodeAsync().then(...)` ohne erneute `shouldScan`-Prüfung; ein Decode, der vor `stop()` begann, meldet sich danach noch.
  `closeScannerModal` setzt `_scannerClosing` sofort wieder auf `false` (`barcode-scanner.js:271`), der bisherige Wächter griff also nicht.
  Nach einem Hardware-Scan im Fenster (N-1-Pfad `:463-472`) konnte der Kamera-Callback `bomScanArticle` ein zweites Mal füllen: bei
  gleichem Artikel das Modal „nicht in der Kommissionierliste“ (Zeilen schon abgehakt), bei einem anderen sichtbaren QR eine **zweite
  Kommissionierung**. Derselbe Fehler bestand schon vorher bei „Abbrechen“ während eines Decodes.
  Fix `:193`: `if (_scannerClosing || _activeScanner !== html5QrCode) return;` — `stopScanner` setzt `_activeScanner = null` synchron,
  ein normaler Treffer bleibt unverändert. Wächter in `HardwareScannerTests.Stueckliste_OffenesKameraFenster_IstZiel_WieKameraTreffer`,
  Manual-UAT TS-99.14 e und Checkliste 21.
- **Q-2 (nicht blockierend, Grenze)** — `openScannerModal` ist `async`: Das Fenster entsteht erst nach `await requestCameraPermission()`
  (`:63`, angehängt `:118`). Ein Hardware-Scan in dieser Zeit (beim ersten Aufruf mit Freigabe-Dialog spürbar) läuft nach der alten Regel
  und zeigt den Hinweis. Kein Datenfehler; TS-99.14 d.
- **Q-3 (nicht blockierend, nicht behoben)** — Reiter „Bild hochladen“: Läuft `scanFile` noch (`:246-249`) und kommt ein Hardware-Scan,
  schließt der Hardware-Pfad das Fenster; das spätere Datei-Ergebnis wertet trotzdem aus. Setzt gleichzeitiges Foto und Scan voraus.
- **Q-4 (nicht blockierend, Hinweis)** — Die Bereichsmeldung (`StandortEinstellungenController.cs:82-86`) setzt Min **und** Max voraus;
  mit nur einer Grenze stünde „zwischen 10 und “ da. Heute hat genau ein Feld Grenzen, und zwar beide.
- **Q-5 (Info, specgemäß)** — Jedes Speichern der Maske mit leerem Abstand schreibt `HardwareScannerMaxZeichenabstandMs = ""`
  (`StandortSettingsWriter.cs:56-62`, Cache-Invalidierung nach Commit); `_Layout.cshtml:25` macht daraus `50`.

**Gezielt geprüft, ohne Befund**
- **Fenster eines anderen Knopfs:** `hwScanKameraZiel` (`:451-458`) verlangt `targetId === modal.dataset.zielId` **und** das Attribut am
  Knopf genau dieses Ziels. Ziel-Lagerplatz-Fenster der Stückliste (`btnScanTargetLocation`, ohne Attribut) → `null` → alte Regel,
  Fenster bleibt offen; ebenso alle Fenster anderer Seiten inkl. `initTextInputScanner`. Der Wächter-Test zählt genau eine Stelle mit dem
  Attribut in allen Views.
- **Gleiche Abfolge wie der Kamera-Treffer:** sperren → globale (gewrappte) `processScannedValue` → `closeScannerModal`; `bomScanArticle`
  ist `INPUT type=hidden` → `change` synchron → Kommissionier-Handler. `_textInputScanCallback` nur bei Text-Zielen (Stückliste nicht).
- **Übrige Int-Felder:** `FaHierarchyMaxTiefe` und `Sync:ProductionWorkplaceSyncMandant` haben weder Grenzen noch
  `EmptyResetsToDefault` → leer unverändert, `abc` weiter Fehler (Bestandstest `Save_InvalidInt_WritesNothing_AndKeepsEnteredValues` grün).
  Der geänderte Test nutzt den echten Key-String (`AppSettingKeys.cs:50` = `FaHierarchyMaxTiefe`), seine Aussage ist nicht leer.
- **Fehler-Redisplay:** Bereichsfehler → `continue`, `ModelState` ungültig → `View(Index, BuildViewModelAsync(settings))` mit den
  eingegebenen Werten, nichts geschrieben (Test verifiziert `SaveAtomicAsync` **nie**). Anzeige „Nicht gespeichert:“ + Liste
  (`Index.cshtml:15-24`), Meldung Razor-encodiert.
- **Writer mit `""`:** `value ?? string.Empty` (`StandortSettingsWriter.cs:58`), Layout prüft `IsNullOrWhiteSpace`.
- **Fehlerklassen:** keine neuen Werte in `<script>`; kein `hidden` + `d-*`; der Client-Rückfall auf 50 bei ungültigem Wert ist gewollt
  (Nachtrag 2) und nur per SQL erreichbar, der Server erzwingt 10–500; keine parallelen Arrays; Regel 10–500 an zwei Orten (Katalog,
  `hardware-scanner.js:77-80`), gegen Drift durch `HardwareScannerTests` gesichert (Tupel `(10, 500, true)` **und** JS-Literal). Default 50
  jetzt an drei Stellen (`_Layout.cshtml:25`, `abstand`, Hinweistext) — Ergänzung zu N-6.

### 3. Beweisart je Punkt des Nachtrags 2
| Punkt | Beweis | Zählt grün? |
|---|---|---|
| N-1 Fenster = Ziel, gleiche Auswertung wie Kamera | Quelltext-Wächter (vor Regel a, Attribut genau einmal, Abfolge, Q-1); Verhalten **Manual-UAT** TS-99.14 | nein |
| N-1 ohne Fenster / anderes Fenster = alte Regel | Review; **Manual-UAT** TS-99.14 a/b | nein |
| N-2 Server 10–500 mit Meldung, nichts gespeichert | automatisiert `Save_ScannerAbstandAusserhalb10Bis500_WritesNothing_WithMessage` (9/0/−5/501), `Save_ScannerAbstandAnDenGrenzen_IsSaved` (10/500); echtes Form-Binding **Manual-UAT** TS-99.15 a/b | ja (Controller) |
| N-2 leer = 50, nie gespeichertes Feld blockiert nicht | automatisiert `Save_EmptyInt_NeverBlocks_…`; Layout-Ausgabe **Manual-UAT** TS-99.15 c/d | teilweise |
| N-2 Client ignoriert ungültige Werte | Node `abstand` (11 Fälle) + Quelltext-Wächter der Verdrahtung; am Gerät **Manual-UAT** TS-99.15 e | ja (Kern) |
| Version, Changelog, Hilfe, README | Review: beide `AppVersion.cs` = 1.66.1, `Help/Changelog.cshtml`, `Help/Index.cshtml`, README | Review |

### 4. Geänderte Bestandstests
- `StandortEinstellungenControllerTests.Save_EmptyInt_LeavesKeyUnchanged_AndSavesTheRest` → `Save_EmptyInt_NeverBlocks_ScannerResetsToDefault_OtherIntsStayUnchanged`:
  **Verhalten absichtlich geändert** durch Nachtrag 2 Punkt 2 (leer = Standard statt „Key fehlt“); zusätzlich geprüft, dass ein anderes
  Int-Feld leer unverändert bleibt. Test stammt aus v1.66.0 dieses Zweigs (nicht in `main`).
- `HardwareScannerTests` (v1.66.0, Zweig): nur **ergänzt** (Grenzen-Tupel, JS-Literal; QA-Fix: eine Assertion). Kein Fixture angepasst,
  keine Änderung der Klasse „sonstiges“.

### 5. CLAUDE.md-Checkliste
- Migration / SQL / FreshInstall / AgentJobs: entfällt (keine Schemaänderung, im Diff geprüft). Audit: keine Entität geändert.
- Version 1.66.1 in **beiden** `AppVersion.cs` ✓, `Help/Changelog.cshtml` ✓, Brain-Changelog
  [[2026-10-21-v1-66-1-hardware-scanner-nachbesserung]] ✓ (QA-Fix ergänzt), feature-map ✓ (QA-Fix + Status). README ✓, Hilfe ✓.
- `docs/TESTSZENARIEN.md` TS-99.14/99.15 ✓, im QA-Lauf TS-99.14 d/e ergänzt; [[testszenarien-index]] Zeile 99 ✓ (Testnamen aktualisiert).

## QA-Nachweis v1.66.2 (qa-agent, 2026-10-22, Freigabe-Nachtrag 3)

Geprüft: `git diff 1d78e7f5..543edbe2` (17 Dateien) im Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, dazu der vollständige
`wwwroot/js/barcode-scanner.js`, `hardware-scanner.js` (Ereignis-Abfolge :141-153), alle `id="btnScan…"` in `IdealAkeWms/Views/**`, alle
`initScanner`/`initTextInputScanner`-Aufrufe (Views + `barcode-scanner.js:488-493`) und die Ziel-Elemente je Seite. QA-Commit `845e38c9`.

### 1. Build und Tests (selbst ausgeführt, HEAD `845e38c9`)
- `dotnet build IdealAkeWms.slnx`: **0 Fehler**, 28 Warnungen (Bestand).
- `dotnet test`: **IdealAkeWms.Tests 2153 bestanden, 1 übersprungen, 0 Fehler** (gesamt 2154; der übersprungene ist Bestand);
  **IDEALAKEWMSService.Tests 316 bestanden, 0 übersprungen, 0 Fehler**. Gleiches Ergebnis schon auf `543edbe2` vor dem QA-Commit.
- `HardwareScannerTests` einzeln (`-v n`): alle 8 bestanden, darunter `OffenesKameraFenster_IstZiel_WieKameraTreffer` und
  `Kern_NodeSelbstpruefung` (läuft, nicht übersprungen).
- Web und Dienst wurden nicht gestartet (Auftrag).

### 2. Code-Review

**Punkt 1 – Liste der freigeschalteten Knöpfe: vollständig und korrekt.** Alle 27 `id="btnScan…"` in Views gegen die Register gelegt:

| Seite | Scan-Felder laut Register (Knopf → Ziel) | Anzahl | `data-hw-scan-kamera-ziel` |
|---|---|---|---|
| StockMovements/Inbound | btnScanArticle → ArticleId, btnScanStorageLocation → StorageLocationId (beide global :489/:490) | 2 | beide ✓ |
| StockMovements/InboundBulk | btnScanStorageLocation → StorageLocationId (global), btnScanArticleBulk → bulkScanBuffer (Seite :293) | 2 | beide ✓ |
| StockMovements/Outbound | wie Inbound | 2 | beide ✓ |
| StockMovements/Transfer | btnScanArticle, btnScanSourceLocation → SourceStorageLocationId, btnScanStorageLocation (alle global) | 3 | alle ✓ |
| StockMovements/LocationTransfer | btnScanSourceLocation (global :491 **und** Seite :128, gleiches Ziel), btnScanTargetLocation (Seite :129) | 2 | beide ✓ |
| StockOverview/Index | btnScanLocationFilter, btnScanArticleFilter, btnScanPO (Seite :163-165) | 3 | alle ✓ |
| ProductionOrders/Index | btnScanOrderNumber, btnScanArticleNumber (Seite :288/:294, Text) | 2 | beide ✓ |
| PickingLeitstand/Index | wie ProductionOrders (Seite :487/:493) | 2 | beide ✓ |
| Tracking/OseonIndex | btnScanArticle → filterArticle, btnScanCustomerOrder (Seite :312/:321); global btnScanArticle → ArticleId, Ziel fehlt auf der Seite | 2 | beide ✓ |
| Picking/Bom | btnScanTargetLocation, btnScanBomArticle (Seite :726/:727) | 2 | beide ✓ |
| Articles/Info | btnScanArticleInfo → articleNumber (global :492) | 1 | – (richtig) |
| StockMovements/OutboundAll | btnScanStorageLocation → StorageLocationId (global) | 1 | – (richtig) |
| StockMovements/Index | btnScanProductionOrder → filterProductionOrder (Seite :167) | 1 | – (richtig) |
| BdeTerminal/Index | btnScanOperator/btnScanFaAg sind keine Kamera-Knöpfe (`bde-terminal.js:99/:165`, kein Register) | – | – (richtig) |

22 Attribute, genau die Liste des Wächters. **InboundBulk:** nicht namentlich genannt, fällt aber unter die Regel „mehr als ein Scan-Feld“.
Das Artikel-Ziel `bulkScanBuffer` ist ein `<input type="hidden">` (:106) und ohne Fenster nie fokussierbar. Die Aufnahme ist regelkonform
und fachlich nötig; Bestätigung durch Gerald an Schranke 2 (Checkliste Punkt 24). **Ausgelassene Seiten** (Info, OutboundAll,
Bewegungsliste): Dort steht im Register genau ein vorhandenes Ziel, Regel (d) wertet den Scan schon aus – das Fenster bleibt dabei offen
(TS-99 Negativ c). Das ist nachtragskonform, kann am Gerät aber überraschen (Checkliste Punkt 25).

**Punkt 2 – Ergebnis gleich dem Kamera-Treffer.** `hwScanKameraZiel` (`barcode-scanner.js:452-459`) nimmt den Register-Eintrag, dessen
`targetId` dem `modal.dataset.zielId` (:75) entspricht. Der Eintrag trägt dieselben Werte, die der Klick-Handler an `openScannerModal`
übergibt (`initScanner` :17-20, `initTextInputScanner` :401-411). Abfolge :467-471 = `startCameraScanner` :193-198 (sperren, globale
`processScannedValue`, schließen); `_textInputScanCallback` wird wie im Klick-Handler gesetzt. Je Seitentyp nachvollzogen:
- Select2-Artikel (Inbound/Outbound/Transfer): gleicher AJAX-Lookup, FA-Feld über `qrFaEnabled`/`faTargetId` aus dem Register ✓.
- Native Selects (Lagerplätze, Bom `TargetStorageLocationId`, StockOverview Lagerplatz): Option-Iteration ✓.
- Text-Input mit `onScanned`/Formular-Absenden (FA-Liste, Leitstand, OSEON): Wrapper :418-433 feuert `input` + `onScanned` genau einmal ✓.
- InboundBulk-Puffer: hidden INPUT → `change`, Wrapper → `onArticleScanned` ✓.
- StockOverview `btnScanPO` (`productionOrder`, 3. QR-Teil) und `btnScanArticleFilter` (FA in `filterProductionOrder`) ✓.
- `d.handled = true` verhindert Regel (b)/(d)/Hinweis in `hardware-scanner.js:143` ✓. Ohne Fenster bzw. bei Fenster eines Knopfs ohne
  Attribut liefert `hwScanKameraZiel` `null`, die bisherige Regel läuft unverändert ✓. Kamera-Knöpfe, Klick-Handler und Kamera-Erfassung
  sind im Diff unberührt ✓.
- QA-Fix v1.66.1 auf allen Seiten: Die einzige Stelle mit `new Html5Qrcode` ist `barcode-scanner.js` (Grep über `wwwroot/js` und Views).
  Alle Kamera-Knöpfe öffnen über `openScannerModal` → `startCameraScanner` mit der Sperre :193 ✓.

**Befunde**

| Nr | Schwere | Befund | Behandlung |
|---|---|---|---|
| Q3-1 | niedrig | `AppVersion.Date` stand in beiden `AppVersion.cs` noch auf `2026-10-21`, der Changelog nennt 22.10.2026. Konvention des Zweigs: Datum mit Version (`c27f3b6d`, `4179e38d`). Der Dienst schreibt das Datum ins Startlog (`SyncWorker.cs:27` u. a.). | **Behoben** in `845e38c9` (`qa: …`). |
| Q3-2 | mittel, vorbestehend, nicht blockierend | Zusatzfund des Berichts bestätigt, aber die Begründung „das Seiten-Fenster gewinnt, weil es zuletzt registriert ist“ stimmt nur ohne Kamera. Beide Klick-Handler rufen `openScannerModal`, beide warten auf `requestCameraPermission()` (:63). Welches Fenster bleibt, entscheidet die Reihenfolge, in der die zwei `getUserMedia`-Zusagen zurückkommen. Gewinnt das globale (`zielId = ArticleId`), trifft weder ein Kamera-Treffer noch der Hardware-Scan das Fenster (`processScannedValue` endet an :302; `hwScanKameraZiel` = null → alte Regel). Zusätzlich wird der erste `Html5Qrcode` nie gestoppt, nur `_activeScanner`. Dasselbe Doppel-Öffnen gibt es auf **LocationTransfer** (`btnScanSourceLocation` global + Seite, gleiches Ziel – Ziel stimmt, nur zwei Kamera-Starts). | Im QA-Lauf nicht gebaut (Zusatzfund, vorbestehend, betrifft die Kamera genauso). Abhilfe-Vorschlag: in `initScanner` `if (!button \|\| !document.getElementById(targetSelectId)) return;` (OSEON) und `LocationTransfer.cshtml:128` streichen, wie in InboundBulk :290-291 kommentiert. Am Gerät beobachten (Checkliste Punkt 26); Entscheidung Gerald. |
| Q3-3 | niedrig, vorbestehend | `scanFromFile` (Bild hochladen, :233-259) hat keine Sperre wie :193. Wählt jemand ein Bild und scannt vor Ende der Bild-Auswertung mit dem Hardware-Scanner, wird das Bild danach zusätzlich ausgewertet. | Nur gemeldet (am Handheld kaum erreichbar, dort läuft die Kamera-Lasche). |
| Q3-4 | Info | Der Wächter pinnt die Liste als Quelltext (`BeEquivalentTo`, 22 Einträge). Er beweist nicht die Regel „mehr als ein Scan-Feld“, zwingt aber jede neue Scan-Seite zur bewussten Entscheidung. Gewollt. | keine |

Klassen aus dem Pflichtkatalog: keine Werte neu in `<script>` eingebettet, kein `hidden` + `d-*` neu, keine stillen Fallbacks neu, keine
parallelen Arrays, keine serverseitige Regel betroffen (rein clientseitiges Ziel-Routing). Die Ziel-Regel bleibt an einer Stelle
(`barcode-scanner.js` + Markup-Attribut).

**Punkt 3 – Doku/Version:** Wächter vollständig (22 Seite|Knopf, dazu „keine View mit `new Html5Qrcode`“) ✓. `docs/TESTSZENARIEN.md`:
TS-99.16 a–h, TS-99.14 b) auf v1.66.2 umgestellt, Negativ c) auf Seiten mit genau einem Scan-Feld eingegrenzt, Endzeile v1.66.0–v1.66.2 ✓.
v1.66.2 in beiden `AppVersion.cs` ✓, `Help/Changelog.cshtml` Karte v1.66.2 (Struktur wie die Nachbarkarten) ✓. `Help/Index.cshtml`: neuer
`<p>` zwischen `</ol>` und `<dl>`, Tags geschlossen, der Stückliste-Satz aus Punkt 4 der Liste ist konsistent umgezogen ✓.
[[testszenarien-index]] Zeile 99 um TS-99.16 und den Testnamen ergänzt (QA-Lauf).

### 3. Beweisart je Punkt des Nachtrags 3
| Punkt | Beweis | Zählt grün? |
|---|---|---|
| 1 Knopf-Liste vollständig | Quelltext-Wächter `OffenesKameraFenster_IstZiel_WieKameraTreffer` + Review-Tabelle oben | ja (Markup) |
| 1 Verhalten am Gerät (Fenster nimmt Scan, schließt) | Browser-JavaScript → **Manual-UAT** TS-99.16 a–e | nein |
| 2 Kamera unverändert, ohne Fenster alte Regel | Review (Diff berührt Klick-/Kamera-Pfad nicht); **Manual-UAT** TS-99.16 f/h, TS-99.13 | nein |
| 2 kein zweiter Treffer nach dem Schließen, alle Seiten | Wächter (Sperre + keine eigene `Html5Qrcode`-Instanz); **Manual-UAT** TS-99.16 g | teilweise |
| 3 Version, Changelog, Hilfe, Szenarien | Review | Review |

### 4. Geänderte Bestandstests
- `HardwareScannerTests.Stueckliste_OffenesKameraFenster_IstZiel_WieKameraTreffer` → `OffenesKameraFenster_IstZiel_WieKameraTreffer`:
  **Verhalten absichtlich geändert** durch Nachtrag 3 Punkt 1/3 (von „genau eine Stelle“ auf die vollständige Liste). Die JS-Assertions
  davor sind unverändert, neu ist „keine View baut `Html5Qrcode`“. Der Test stammt aus v1.66.1 dieses Zweigs (nicht in `main`). Kein
  weiterer Bestandstest geändert, kein Fixture angepasst.

### 5. CLAUDE.md-Checkliste
- Migration / `SQL/` / FreshInstall / `SQL/AgentJobs/`: entfällt (kein Eintrag unter `*/Migrations/`, kein SQL im Diff). Audit: keine Entität.
- Version 1.66.2 in beiden `AppVersion.cs` ✓ (Datum per QA-Fix), Anwender-Changelog ✓, Hilfe ✓. README: keine neue Einstellung.
- Brain-Changelog v1.66.2 und feature-map: nicht Teil dieses QA-Laufs; vom Auftraggeber mit dem Brain-Commit nachzuziehen, falls offen.
- `docs/TESTSZENARIEN.md` ✓, [[testszenarien-index]] ✓.

**Ergebnis:** grün, keine offenen Blocker → `status: Testbereit`.

## QA-Nachweis v1.67.1 (qa-agent, 2026-10-23, Freigabe-Nachtrag 4)

Geprüft wurde `git diff 4bfa4576..eb4b28ea` (12 Dateien, +386/−18) im Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`. Dazu der
umgebende Code: `hardware-scanner.js` komplett, in `barcode-scanner.js` Modal, Ziel-Register und `wms:hardware-scan`-Listener,
die Scan-Handler in `bde-terminal.js`, `Profile.cshtml` und die Modal-Farben in `site.css`. QA-Fix `a2651019` (nur Doku). Das Review lief
selbst, in diesem Lauf.

### 1. Build und Tests (selbst ausgeführt, vor und nach dem QA-Fix)
- `dotnet build IdealAkeWms.slnx`: **0 Fehler**, 28 bzw. 27 Warnungen (alle schon vorher vorhanden, u. a. CS8601 in Tests).
- `dotnet test`: **IdealAkeWms.Tests 2181 bestanden, 1 übersprungen, 0 fehlgeschlagen (2182)**; **IDEALAKEWMSService.Tests 328 bestanden,
  0 übersprungen, 0 fehlgeschlagen**. `HardwareScannerTests` 11/11 bestanden, `Kern_NodeSelbstpruefung` lief (nicht übersprungen).
- Node direkt: `hardware-scanner.check.js` → „hardware-scanner core ok (45 Pruefungen)“. Die **alte Prüfung von `4bfa4576`** läuft gegen
  das neue Skript mit „ok (27 Pruefungen)“ durch. Damit ist belegt, dass der Tasten-Kern `create` unverändert ist.

### 2. Code-Review: Befunde und Behandlung
| # | Schwere | Fundstelle | Befund | Behandlung |
|---|---|---|---|---|
| Q4-1 | mittel (Doku-Drift) | `barcode-scanner.js:127-128`, `:467-469`; `Help/Index.cshtml` Absatz „Kamera-Fenster offen“; `docs/TESTSZENARIEN.md` TS-99 Negativ c | Das Feld „Handscanner“ bekommt beim Öffnen **immer** den Fokus. Über das ODER ist das Fenster damit auf **jeder** Seite Ziel, auch für Tastatur-Scanner und auf Seiten mit einem Scan-Feld (Articles/Info, Ausgang „Alles“, Bewegungsliste). TS-99 Negativ c („Modal bleibt offen“) und der Hilfe-Satz „auf allen Seiten mit mehreren Scan-Feldern“ widersprachen dem neuen Verhalten (TS-99.17 d). Fachlich ist das gewollt (Nachtrag 4 Punkt 1 „offenes Kamera-Fenster als Ziel“) und beantwortet Checklistenpunkt 25. | **Behoben** `a2651019`: Hilfe-Satz ergänzt, TS-99 Negativ c als ab v1.67.1 überholt markiert. Der alte Fall gilt nur noch, wenn der Fokus im Fenster woanders liegt. |
| Q4-2 | niedrig (Restrisiko, Manual-UAT) | `hardware-scanner.js:304-309`, `:335-340` | Das Enter-Suffix wird nur als `keydown` mit key `Enter`/keyCode 13 abgefangen, in einer Textarea zusätzlich als `beforeinput insertLineBreak/Paragraph`. Meldet ein Gerät Enter als keyCode 229 bzw. `Unidentified`, greift das nicht. Dann kann ein Formular per impliziter Übermittlung abgeschickt werden, bevor die Zeitgrenze den Scan liefert. | Ohne Gerät nicht änderbar. TS-99.17 prüft „das Formular wird **nicht** abgeschickt“, die Diagnose zeigt den keyCode des Enters. Checkliste Punkt 27. |
| Q4-3 | niedrig (Grenze) | `hardware-scanner.js:115-116` (`createText.eingabe`) | Erkannt wird nur reines **Einfügen** (`entfernt === 0`). Ist im Feld Text **markiert** und das Gerät ersetzt ihn, bleibt `~09<Code>` als Text stehen. Kein Feld markiert seinen Inhalt beim Fokus (`.select()` in Views und JS: kein Treffer), relevant ist das also nur bei manueller Markierung. Ebenso kann der Diff über gemeinsamen Anfang und Ende den Präfix verfehlen, wenn direkt hinter dem Cursor ein `~` steht (Feld `A~`, `~09B` an Position 1). | Hingenommen (Randfall), hier dokumentiert. |
| Q4-4 | niedrig (Grenze) | `hardware-scanner.js:94-100` (`createText.ende`), `:292-298` | Präfix ohne Code plus Enter innerhalb der Zeitgrenze (von Hand `~09` und Enter in ≤ 50 ms): Der Präfix kommt zurück ins Feld, das Enter bleibt aber verschluckt. Bei verpasster Zeitgrenze (Timer gedrosselt) wird der alte Code **nach** der neuen Eingabe eingefügt, die Reihenfolge ist also vertauscht. Das ist schon mit `ponytail:` markiert. | Hingenommen (in der Praxis nicht relevant). |
| Q4-5 | Hinweis | `barcode-scanner.js:113-128` | Das Feld „Handscanner“ erscheint und nimmt den Fokus auch dann, wenn „Dieses Gerät“ aus ist, denn die Bedingung ist nur `data-hw-scan-praefix`. Von Hand Getipptes plus Enter bewirkt in diesem Feld nichts. | Hingenommen, harmlos. Ggf. als Folgewunsch. |

Gezielt geprüft, ohne Befund:
- **Präfix nie im Feld, Seiten-Handler sehen ihn nicht:** Der `input`-Listener sitzt in der Capture-Phase am `window`. Bei
  `zuruecksetzen` ruft er `stopImmediatePropagation` auf und setzt Wert und Cursor zurück (`:320-324`). Eigene Ereignisse aus
  `feldFuellen`, Replay und `zurueck` sind synthetisch und fallen über `isTrusted` heraus (`:303`, `:315`, `:330`). Bei zeichenweiser Eingabe
  erreichen `~` und `~0` die Seite kurz (Select2 sucht kurz nach `~`), ab dem vollen Präfix wird zurückgesetzt. Das entspricht der
  Beschreibung.
- **Dieselbe Kette:** `textAuswerten` → `zustellen` (`:258-281`) → `wms:hardware-scan` → `hwScanKameraZiel`/`hwScanFokusIn` → globale
  (gewrappte) `processScannedValue` (`barcode-scanner.js:473-497`).
  - Select2-Suchfeld: Regel (a) über `.select2-dropdown`. Das echte Enter erreicht Select2 nicht, es wählt also keinen markierten Eintrag;
    danach `select2('close')`.
  - BDE `scanOperator`/`scanFaAg`: Regel (b), Code einfügen plus synthetisches Enter an die `keydown`-Handler
    `bde-terminal.js:100-102` bzw. `:166-168`. Das echte Enter ist vorher verschluckt, es gibt keinen doppelten Aufruf.
  - Spaltenfilter: Regel (b).
  - Der eigene Code hat keine `keyup`/`keypress`-Handler (grep). Ein durchgelassenes `keyup` Enter löst daher nichts doppelt aus.
- **Keine Doppelauswertung:** Text wird ignoriert, solange der Tasten-Kern läuft (`:318`). Tasten, die der Tasten-Kern verschluckt,
  erzeugen kein `input`. Offen bleibt nur der Fall, dass ein Gerät echte Tasten **und** einen IME-Commit sendet; das prüfen TS-99.17 m
  und die Diagnose.
- **Regelgleichheit im `keydown`-Kopf:** Alt war `!isTrusted || isComposing || 229 || geraetAus → return; !pending && password → return`.
  Neu ist die Reihenfolge `isTrusted`, Passwort (`!pending`), Gerät aus, **[neu: Enter bei `textCore.aktiv()`]**, 229/Komposition.
  - Alle Zweige vor dem Enter-Zweig enden wie vorher mit `return`. Dass Passwort jetzt vor 229 steht, ändert nichts: beide enden mit
    `return`, und bei `pending` fällt 229 weiter auf `return`.
  - Der Enter-Zweig greift nur innerhalb der Zeitgrenze nach einem erkannten Text-Präfix (`aktiv()` heißt `code !== null`, sonst beendet
    der Timer nach `maxGapMs + 5`). Normales Enter auf anderen Seiten bleibt unberührt.
  - `preventDefault` auf `keydown` unterbindet die implizite Formular-Übermittlung.
- **Passwort:** `freiesFeld` kennt `password` nicht (`:218`). `input`/`beforeinput` werden dort also gar nicht ausgewertet oder
  protokolliert. `keydown` im Passwortfeld wird ohne Taste protokolliert (`:333`).
- **Diagnose:**
  - Gespeichert nur in `localStorage` `wms.hardwareScanner.diagnose` = `an` (Profil `Profile.cshtml:204-208`), Standard aus.
  - Ringpuffer mit 30 Zeilen (`:162`), Ausgabe nur per `textContent` (kein HTML).
  - Kein `fetch`/XHR/`sendBeacon`/`$.ajax` in der Datei (Wächter-Test).
  - Kopieren über die Clipboard-API, ohne HTTPS oder ohne Recht wird der Text stattdessen markiert.
  - Darstellung: Karte vor `footer.footer`, `pre` mit Umbruch, 0,875 rem, `max-height:50vh`, per Tab erreichbar. Knopf ohne `btn-sm`,
    das Touch-Ziel reicht.
- **Bekannte Fehlerklassen des Projekts:**
  - Keine Werte per `@Html.Raw` in `<script>`.
  - `hidden` nur an `#hwScanGeraet`, das die Klassen `card mt-3` trägt, also kein `d-*`.
  - Keine stillen `?? 0`-Fallbacks. Präfix-Default `~09` und Abstand-Default 50 sind bewusst gesetzt und dokumentiert.
  - Keine Arrays, kein Model-Binding. Keine Server-Regel betroffen: alles läuft im Browser, nichts wird gespeichert.
  - Die Präfix-Regel liegt jetzt in **zwei** Kernen, `create` für Tasten und `createText` für Text. Das sind zwei Eingabekanäle; Präfix
    und Abstand kommen aus denselben body-Attributen, jeder Kern hat seine eigene Node-Prüfung. Bei den Einstellungen gibt es daher kein
    Drift-Risiko.
- **WCAG AA:**
  - Kamera-Fenster auf `#1a1a1a`: Label `#fff` ≈ 17,4:1, Hilfetext `#ced4da` ≈ 11,6:1, Eingabefeld im Bootstrap-Standard (dunkel auf weiß).
  - Diagnose-Karte im Bootstrap-Standard: Fließtext auf weiß, `btn-outline-secondary` `#6c757d` ≈ 4,7:1.
  - Bedeutung hängt nie allein an der Farbe.

### 3. Beweisart je Punkt des Nachtrags 4
| Punkt | Beweis |
|---|---|
| 1 Erkennung am eingefügten Text (Kern): ganzer Code, Zeilenumbruch, Zeitgrenze, zeichenweise, Hand-Eingabe bleibt Text, Präfix ohne Code, Löschen, anderer Präfix | **automatisiert**: Node `hardware-scanner.check.js` (18 neue Prüfungen) |
| 1 DOM: Präfix nie im Feld, Zustellung an Select2, Kamera-Fenster, BDE und Spaltenfilter, Enter verschluckt, kein Submit | Quelltext-Wächter `FokusEingabe_*` und `KameraFenster_HandscannerFeld_*` sichern nur die Code-Stellen. **Manual-UAT** TS-99.17 a–h (Browser-JS, Gerät) |
| 1 Verhalten am Netum: `inputmode="none"` mit Fokus-Eingabe, keyCode des Enters, IME-Komposition | **Manual-UAT** TS-99.17 c/h/m (Gerät) |
| 2 Tasten-Erkennung unverändert | **automatisiert**: alte Node-Prüfung (27) gegen das neue Skript grün. Kopf: Wächter plus Review. Am Gerät **Manual-UAT** TS-99.17 m, TS-99.1 ff. |
| 3 Diagnose: localStorage, Standard aus, 30 Zeilen, nichts an den Server | Wächter `Diagnose_NurDiesesGeraet_NichtsAnDenServer` (Quelltext). Anzeige, Kopieren und Netzwerk: **Manual-UAT** TS-99.18 |
| 4 Hilfe und Hinweistext | Review (Text gelesen), Sichtprüfung als **Manual-UAT** |
| 5 Version, Tests, TS | Review: 1.67.1 / 2026-10-23 in beiden `AppVersion.cs`, Changelog, TS-99.17/99.18 |

Was als „Manual-UAT“ markiert ist, zählt nicht als grün.

### 4. Geänderte Bestandstests
- `HardwareScannerTests.DomTeil_PasswortIMEUndSynthetischeTastenNieAbfangen` prüfte die alte Kopfzeile wörtlich. Jetzt prüft er dieselben
  Regeln einzeln: erste Zeile `isTrusted`, 229/Komposition, Gerät aus, Passwort.
  - **Einordnung:** Quelltext-Wächter an den umgebauten Kopf angepasst. Umgebaut wurde er durch Nachtrag 4 Punkt 3 (Diagnose-Zeile je
    Entscheidung) und Punkt 1 (Enter vor 229).
  - Die Regelgleichheit ist im Review bestätigt (§2). Kein Fixture wurde angeglichen.
- `hardware-scanner.check.js` wurde nur ergänzt, die bestehenden Prüfungen sind unverändert.

### 5. CLAUDE.md-Checkliste
- Migration, `SQL/`, FreshInstall, `SQL/AgentJobs/`: entfällt. Audit: keine Entität. README: keine neue Einstellung, denn die Diagnose
  ist Browser-lokal.
- Version 1.67.1 in beiden `AppVersion.cs` ✓, Anwender-Changelog ✓, Hilfe ✓ (+ QA-Fix), Brain-Changelog
  [[2026-10-23-v1-67-1-hardware-scanner-fokus-eingabe]] ✓, feature-map ✓.
- `docs/TESTSZENARIEN.md` TS-99.17/99.18 ✓ (+ Negativ c angepasst), [[testszenarien-index]] Zeile 99 ✓.

**Ergebnis:** grün, keine offenen Blocker → `status: Testbereit`.

## QA-Nachweis v1.67.2 (qa-agent, 2026-10-23, Freigabe-Nachtrag 5)

*Lauf am 2026-10-27.* Geprüft wurde `git diff a2651019..20b573de` (12 Dateien, +184/−8) im Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`.
Dazu der umgebende Code: `hardware-scanner.js` DOM-Teil komplett (`freiesFeld`, `zustellen`, Text- und Tasten-Erkennung),
`barcode-scanner.js` komplett (Modal öffnen/schließen, `processScannedValue` + Wrapper, Ziel-Register, `wms:hardware-scan`-Listener),
`Inbound.cshtml` komplett (Formular-Grenze, Lagerplatz-Optionstext = `Code`, Seiten-Skripte), Skript-Reihenfolge im `_Layout`
(`RenderBody` Z. 411 vor `hardware-scanner.js` Z. 436 vor `Scripts`), globale `keydown`-Handler in `wwwroot/js`. QA-Fix `a9462036` (nur Doku).
Das Review lief selbst, in diesem Lauf.

### 1. Build und Tests (selbst ausgeführt, HEAD `20b573de`)
- `dotnet build IdealAkeWms.slnx`: **0 Fehler**, 28 Warnungen (alle schon vorher vorhanden, u. a. CS8601 in Tests).
- `dotnet test`: **IdealAkeWms.Tests 2182 bestanden, 1 übersprungen, 0 fehlgeschlagen (2183)**; **IDEALAKEWMSService.Tests 328 bestanden,
  0 übersprungen, 0 fehlgeschlagen**. `HardwareScannerTests` 12/12 bestanden (`Kern_NodeSelbstpruefung` lief, nicht übersprungen).
- Node direkt: neue Prüfung → „hardware-scanner core ok (52 Pruefungen)“. Die **alte Prüfung von `a2651019`** läuft gegen das neue Skript
  mit „ok (45 Pruefungen)“. Die Kerne `create` und `createText` sind damit unverändert.
- Der QA-Fix `a9462036` ändert nur `docs/`, deshalb kein erneuter Lauf.

### 2. Code-Review: Befunde und Behandlung
| # | Schwere | Fundstelle | Befund | Behandlung |
|---|---|---|---|---|
| Q5-1 | niedrig (Doku) | `docs/TESTSZENARIEN.md` TS-99.19 j | Der Vorrang-Test nahm das Feld „Menge“. Das ist `type="number"` (`Inbound.cshtml:45`): Es kann `~09…` nicht aufnehmen, der Browser leert den Wert, und das erwartete „Code landet in Menge“ ist so nicht prüfbar. | **Behoben** `a9462036`: j) nutzt jetzt das Textfeld „Fertigungsauftrag“ (`ProductionOrder`, Regel (b)). |
| Q5-2 | niedrig (eine Regel, zwei Orte) | `barcode-scanner.js:510-512` gegen `:351-353` | Ohne Feld wird der Lagerplatz **genau** gesucht (`o.text.trim() === c`). Kamera und angetipptes Feld finden ihn weiter über `processScannedValue` mit `===` **oder** `startsWith`, der erste Treffer gewinnt. Derselbe Code kann deshalb über den Kamera-Knopf einen anderen Platz wählen als ohne Feld, z. B. `A-1` → `A-10`, wenn `A-10` vor `A-1` sortiert ist. | So gewollt: Nachtrag 5 Punkt 4 verlangt „genau diesem Code“. Das alte `startsWith` ist nicht Teil dieses Auftrags. Kandidat für eine Folgeaufgabe, kein Blocker. |
| Q5-3 | niedrig (Grenze) | `hardware-scanner.js:303-321`, `Inbound.cshtml:11` | Ist der Schalter an und nichts angetippt, landet von Hand Getipptes (PC-Tastatur) und ein Scan **ohne** Präfix (Gerät falsch eingestellt) unsichtbar im Scan-Feld. Das Feld wird nie geleert, und es erscheint kein Hinweis, weil ohne Präfix kein Scan erkannt wird. Die Diagnose zeigt dann „durchgelassen“. Formular und Daten betrifft das nicht: Das Feld liegt außerhalb von `<form>` und hat kein `name`. | Hingenommen (Pilot, nur Handheld, Schalter je Gerät). In Checkliste Punkt 35 aufgenommen. |
| Q5-4 | niedrig (Grenze, schon vorbestehend) | `barcode-scanner.js:390-396` `clearAndFillFaNumber` | Nach einem gefundenen Artikel wird das FA-Feld immer überschrieben. Ohne `QrMitFaNummer` bzw. ohne FA im QR wird es geleert. Wer zuerst die FA von Hand eintippt und dann den Artikel scannt, verliert die FA. Bei Kamera und angetipptem Feld ist das schon so. | Nicht geändert, weil vorbestehend. TS-99.19 a („sonst bleibt FA leer“) passt dazu. Checkliste Punkt 36: fachlich bestätigen. |
| Q5-5 | Hinweis (a11y) | `Inbound.cshtml:11-12` | Das Feld ist fokussierbar und trägt `aria-hidden="true"`. axe meldet das als `aria-hidden-focus`. Der Nachtrag verlangt „für Screenreader verborgen“, und der Dev-Lauf hat das schon als Grenze notiert. | Hingenommen. TS-99.19 m (TalkBack) prüft es am Gerät. |

Gezielt geprüft, ohne Befund:
- **Schalter aus = v1.67.1.**
  - `ohneFeld` bleibt `null` (`hardware-scanner.js:306-307`). Dann ist `ohne` falsch, `freiesFeld` wird wie vorher berechnet, `if (ohne) return` greift nicht, die Diagnose zeigt wie vorher `an Feld #`, `d.ohneFeld` ist falsch, und `parken` samt Listener und Observer wird nicht registriert.
  - Das Feld bleibt im DOM, wird aber nie fokussiert: `tabindex="-1"`, `pointer-events:none`, kein `data-hw-scan-ziel` (Regel (d) unberührt), kein `name`, außerhalb des Formulars.
  - Kein Skript auf der Seite oder im Layout fokussiert „das erste Feld“. In `site.js` gibt es kein `.focus(`.
- **Andere Seiten:** Das Attribut steht nur in `Inbound.cshtml` (Wächter `ScannenOhneFeld_*`). Ohne Feld ist `ohneFeld` `null`, es gilt also derselbe Pfad wie bei Schalter aus.
  - Das neue Feld `detail.hinweis` liest kein Bestands-Listener.
- **`processScannedValue`, 6. Parameter:** Alle 5 Bestandsaufrufer (`barcode-scanner.js:207, 258, 484, 499` und Wrapper `:432`) übergeben ihn nicht. `if (nichtGefunden)` ist dann falsch, das Verhalten bleibt gleich.
  - Der Wrapper reicht den Parameter nur durch. `_textInputScanCallback` greift nur bei passendem `targetId`, und `ArticleId` ist kein Text-Ziel.
  - Außerhalb von `barcode-scanner.js` ruft niemand `processScannedValue` auf (grep über `IdealAkeWms/`).
- **Doppelauswertung:**
  - Ein Netum-Scan im unsichtbaren Feld läuft genau einmal durch `createText`. Das Enter wird bei `textCore.aktiv()` verschluckt (`:370-374`).
  - Ein Tasten-Scanner läuft über den Tasten-Kern, der Text-Pfad ist dann über `core.pending()` gesperrt (`:353`).
  - `hwScanOhneFeld` setzt `d.handled = true`. Danach folgen weder Regel (b) noch (d) noch der Hinweis aus `zustellen`, und `processScannedValue` wird höchstens einmal aufgerufen.
  - Ein Kamera-Treffer und ein Hardware-Scan zugleich sind über `_scannerClosing` gesperrt wie bisher.
- **Nie Submit:**
  - Das Feld steht **vor** `<form>` (`Inbound.cshtml:11` gegen `:21`). Das Layout umschließt `RenderBody` mit keinem Formular, die Logout-Formulare liegen in der Navigation (`_Layout.cshtml:357/379`). Ein Enter dort kann nichts implizit absenden, auch wenn es als keyCode 229 nicht abgefangen wird (Restrisiko Q4-2 entfällt hier).
  - Die Zuordnung setzt nur Werte und löst `change` aus.
  - Globale `keydown`-Handler (`column-preferences.js:795`, `column-filter-syntax.js:223`) reagieren nur auf Escape.
- **Fokus-Parken, keine Schleife:** `parken` läuft verzögert (`setTimeout 0`) und fokussiert nur, wenn `body`/`html` oder eine geschlossene Select2-Auswahl den Fokus hat.
  - Das Fokussieren selbst ändert keine `body`-Kinder. Ein `focusout` der Select2-Auswahl führt zu einem zweiten `parken`, das dann nichts mehr tut, weil der Fokus schon im Feld liegt.
  - Der Hinweis-Kasten und die Diagnose-Karte werden je einmal eingehängt und lösen ein `parken` ohne Wirkung aus.
- **Kein Fokusraub beim Antippen:**
  - Feld antippen: `focusout` → Zeitgeber → `activeElement` ist das neue Feld, also nichts. Ein Knopf, den Chrome Android fokussiert, wird ebenso behandelt.
  - Select2 öffnen: Die Klasse `--open` steht schon, bevor `parken` prüft, deshalb nichts.
  - Select2 schließen: Der Fokus geht an `.select2-selection`, es ist kein Dropdown offen, also wird geparkt.
  - Native `<select>` und offene Picker behalten den Fokus. Das ist die dokumentierte Grenze (TS-99.19 g, Hilfe).
- **Kamera-Fenster:**
  - Öffnen: Das Fenster hängt am `body`, und `scannerHwInput` bekommt den Fokus synchron (`barcode-scanner.js:125-128`), also kein Parken.
  - Schließen: `modal.remove()` (`:278`) löst den Observer aus, der Fokus liegt auf `body`, also wird geparkt.
  - Tippt man im Fenster auf eine freie Stelle, wird das Seitenfeld geparkt, während das Fenster offen ist. Der Scan geht trotzdem ans Fenster, weil `hwScanKameraZiel` vor `d.ohneFeld` geprüft wird (`:479-489`) und beide Inbound-Knöpfe `data-hw-scan-kamera-ziel` tragen. Die Diagnose zeigt in diesem Fall `an Feld #… (Kamera-Fenster)`.
- **Zuordnung:** Die Lagerplatz-Optionen tragen als Text genau `Code` (`SelectList(…, "Id", "Code")`, `Inbound.cshtml:53`), der genaue Vergleich trifft also. Die leere Option ist über `o.value` ausgeschlossen.
  - Codes mit `;` gehen immer an den Artikel, auch wenn der erste Teil ein Lagerplatz wäre (Node-Prüfung).
  - Bei einer Artikelnummer ohne Treffer liefert der AJAX-Aufruf 404. Dann erscheinen „✗ Nicht gefunden“ und der gelbe Hinweis „Code … nicht zuordenbar“, und nichts wird geändert.
  - `change` als nicht-bubbelndes `Event` ist dasselbe Muster wie das Seiten-Skript (`Inbound.cshtml:253`).
- **Unsichtbar ohne Tastatur:**
  - Das Feld trägt `inputmode="none"` und `autocomplete="off"`, und die CSS-Regel macht es `fixed`, 1 px groß, mit `opacity:0`. `freiesFeld` braucht `getClientRects`, deshalb 1 px statt `display:none`.
  - `focus({ preventScroll: true })`.
  - Ob Android trotzdem scrollt oder die Tastatur zeigt, prüft TS-99.19 e am Gerät.
- **Diagnose-Text:** `über Scan-Feld (ohne Fokus) → ArticleId (Artikel-QR|Artikelnummer)` bzw. `→ StorageLocationId (Lagerplatz)` bzw. `→ nicht zuordenbar` plus Hinweis-Zeile (`hardware-scanner.js:274-276`). Der Hinweis aus dem AJAX-Fehler wird über `detail.hinweis` nachträglich protokolliert.
- **Bekannte Fehlerklassen:**
  - Keine Werte per `@Html.Raw` in `<script>`.
  - Kein `hidden` an `d-*`-Elementen, das neue Feld nutzt eine eigene Klasse.
  - Kein stiller `?? 0`-Fallback: Ein leerer Code wird als „nicht zuordenbar“ gemeldet.
  - Keine Arrays, kein Model-Binding, keine Server-Regel: Es wird nichts gespeichert, das Absenden prüft der Server wie bisher.
  - Lagerplatz-Vergleich an zwei Orten, siehe Q5-2.
- **Profil-Schalter:** wie „Scanner-Diagnose“ nur im `localStorage` (`wms.hardwareScanner.ohneFeld` = `an`, aus = Schlüssel entfernt), Standard aus, mit `aria-describedby`.

### 3. Beweisart je Punkt des Nachtrags 5
| Punkt | Beweis |
|---|---|
| 1 Nur Einbuchung | **automatisiert** (Quelltext-Wächter: Attribut nur in `StockMovements/Inbound.cshtml`). Regression anderer Seiten: Review + **Manual-UAT** TS-99.19 l |
| 2 Unsichtbares Feld: `inputmode`, `aria-hidden`, `tabindex`, außerhalb `<form>`, `preventScroll` | Markup und Codestellen: **Quelltext-Wächter**. Wirkung (keine Tastatur, kein Springen, TalkBack still): **Manual-UAT** TS-99.19 e/m |
| 2 Fokus-Parken (Laden, Blur, Select2, Kamera-Fenster) | **Manual-UAT** TS-99.19 a/f/g (Browser-JS, Fokusverhalten). Review §2 |
| 3 Schalter, Standard aus, aus = v1.67.1 | Wächter (Profil-Code, Bedingung im Skript). Verhalten: **Manual-UAT** TS-99.19 k |
| 4 Zuordnung nach Inhalt (`;`, genauer Lagerplatz, Trim, leer → nicht zuordenbar) | **automatisiert**: Node `zuordnung` (7 neue Prüfungen) |
| 4 DOM-Zuordnung (Artikel per AJAX, Lagerplatz setzen, Hinweis, nichts geändert, kein Submit) | **Manual-UAT** TS-99.19 a–d/h/i (Browser-JS, `/api/articles/by-number` gegen echte Daten) |
| 5 Vorrang angetipptes Feld/Kamera-Knopf, Diagnose-Text | Reihenfolge Kamera vor ohne Feld: Wächter. Verhalten: **Manual-UAT** TS-99.19 j, Diagnose-Zeilen a/b/d |
| 6 Version, Tests, TS, Hilfe | Review: 1.67.2 in beiden `AppVersion.cs`, Changelog, Hilfe-Absatz, TS-99.19 a–m vollständig (Vorbedingung, Schritte, Erwartung, Negativ i–m) |

Was als „Manual-UAT“ markiert ist, zählt nicht als grün.

### 4. Geänderte Bestandstests
Keine. `git diff a2651019..20b573de -- IdealAkeWms.Tests` enthält nur hinzugefügte Zeilen: `hardware-scanner.check.js` +13 und
`HardwareScannerTests.ScannenOhneFeld_*` neu. Die alte Node-Prüfung (45) läuft unverändert gegen das neue Skript.

### 5. CLAUDE.md-Checkliste
- Migration, `SQL/`, FreshInstall, `SQL/AgentJobs/`: entfällt. Audit: keine Entität. README: keine neue AppSetting, der Schalter ist Browser-lokal.
- Version 1.67.2 in beiden `AppVersion.cs` ✓, Anwender-Changelog ✓, Hilfe ✓, Brain-Changelog [[2026-10-23-v1-67-2-einbuchung-scannen-ohne-feld]] ✓,
  feature-map ✓.
- `docs/TESTSZENARIEN.md` TS-99.19 ✓ (+ QA-Fix j), [[testszenarien-index]] Zeile 99 nennt v1.67.2 / TS-99.19 ✓.
- Frontend: Es gibt keine neue sichtbare Oberfläche außer dem Profil-Schalter, und der folgt dem Muster der Diagnose. Der Kontrast entspricht dem Bootstrap-Standard.

**Ergebnis:** grün, keine Blocker → `status: Testbereit`.

## QA-Nachweis v1.68.1 (qa-agent, 2026-10-27, Freigabe-Nachtrag 6)

Geprüft `git diff ca48b22d..69bb4fd1` (9 Dateien, +63/−18) im Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, dazu der umgebende
Code: `hardware-scanner.js` komplett (Kerne `create`/`createText`, `diag`, `freiesFeld`, `zustellen`, `parken`, `input`/`beforeinput`/`keydown`),
`barcode-scanner.js` `wms:hardware-scan`-Listener und `hwScanOhneFeld` (`:535-584`), Bootstrap-Regel `[hidden]{display:none!important}`.
Review selbst, in diesem Lauf. QA-Fix `8540c4d6`.

### 1. Build und Tests (selbst ausgeführt, nach dem QA-Fix wiederholt, HEAD `8540c4d6`)
- `dotnet build IdealAkeWms.slnx`: **0 Fehler** (vor dem Fix 28 Warnungen, nach dem Fix inkrementell 25; alle vorbestehend, u. a. CS8601).
- `dotnet test`: **IdealAkeWms.Tests 2195 bestanden, 1 übersprungen, 0 fehlgeschlagen (2196)**; **IDEALAKEWMSService.Tests 328 bestanden,
  0 übersprungen, 0 fehlgeschlagen** – vor und nach dem Fix gleich. `HardwareScannerTests` 13/13 bestanden (`Kern_NodeSelbstpruefung` lief).
- Node direkt: `node IdealAkeWms.Tests/Js/hardware-scanner.check.js <pfad>` → „hardware-scanner core ok (52 Pruefungen)“; `node --check` ok.
- Web/Dienst nicht gestartet (Auftrag).

### 2. Code-Review: Befunde und Behandlung
| # | Schwere | Fundstelle | Befund | Behandlung |
|---|---|---|---|---|
| Q6-1 | mittel | `hardware-scanner.js:278` (`69bb4fd1`) gegen `textAuswerten` `:337` | Neues Leeren `ohneFeld.value = ''` läuft **nach** `textAuswerten`, das `vor = { wert: el.value }` schon gesetzt hat. Steht Rest-Text im Feld (Scan ohne Präfix, PC-Tastatur, `~09` ohne Code), vergleicht der nächste Text-Scan gegen den alten Wert: `eingefuegt('xx', '~09B')` → `entfernt ≠ 0` → nicht erkannt, `~09B` bleibt im Feld; erst der übernächste Scan klappt wieder. Nachgestellt mit dem echten `createText` in Node: ohne Fix „Scan A \| nicht erkannt \| Scan C“, mit Fix „A \| B \| C“. Vor v1.68.1 gab es kein Leeren, darum kein Fehler. | **Behoben** `8540c4d6`: `if (ohne) { ohneFeld.value = ''; vor = { el: ohneFeld, wert: '' }; }`; Wächter angepasst; TS-99.19 o. |
| Q6-2 | niedrig (kosmetisch) | `Inbound.cshtml:11-14` | Der Wrapper `<div class="mb-2">` bleibt bei Schalter aus stehen: 0,5 rem mehr Abstand unter der Überschrift als in v1.68.0. Sonst Seite gleich (Feld `hidden`, nie fokussiert). | Hingenommen, Checkliste 39. |
| Q6-3 | niedrig | `hardware-scanner.js:314-315`, `site.css` | Der Zustandstext steht im Platzhalter. Steht Rest-Text im Feld, ist er verdeckt; der Zustand ist dann nur an Rahmenform (gestrichelt/durchgezogen) und Farbe erkennbar. Rest-Text ist jetzt sichtbar (Q5-3 damit entschärft) und verschwindet beim nächsten erkannten Scan. | Hingenommen (Pilot). |
| Q6-4 | Hinweis | `:314-315`, `parken` | Fokus-Diagnose: jedes Antippen eines Formularfelds bzw. einer freien Stelle schreibt genau eine Zeile „aus“ bzw. „an“. Keine Schleife: `parken` hängt nur an `focusout`/`childList` von `body`, `focus()` ändert keine `body`-Kinder, `diag` schreibt nur in `<pre>`-Text (Observer ohne `subtree`). Viele Taps können Scan-Zeilen aus dem 30er-Puffer schieben. | Hingenommen, TS-99.19 p. |
| Q6-5 | Hinweis | `HardwareScannerTests.cs` | Testname `ScannenOhneFeld_NurAufEinbuchung_UnsichtbarOhneTastatur_NurMitSchalter` sagt noch „Unsichtbar“. | Nicht umbenannt (Dev-Lauf bewusst), kein Einfluss. |

Gezielt geprüft, ohne Befund:
- **Grenzen:** Diff berührt nur `Inbound.cshtml` als View; `barcode-scanner.js` unverändert (also `processScannedValue`, `hwScanOhneFeld`), Kern
  `zuordnung` und `create`/`createText` unverändert (Node-Prüfung 52 grün). Kein Seed, keine Migration, kein `SQL/`.
- **Schalter aus = v1.68.0:** `ohneFeld` bleibt `null` (`:310`), `hidden` wird nie entfernt, keine Listener; Bootstrap `[hidden]{display:none!important}`,
  das Feld trägt keine `d-*`-Klasse und die eigene Regel kein `display`. Nicht fokussierbar, weil `display:none`. Einzige Abweichung Q6-2.
- **Sichtbares Feld:** kein `aria-hidden`, kein `tabindex="-1"`, CSS ohne `opacity`/1 px/`pointer-events`/`position:fixed`; `inputmode="none"`,
  `autocomplete="off"`, `aria-label="Scan-Feld"`, außerhalb `<form>`, kein `name` → kann nichts absenden oder mitsenden.
- **Kontrast (nachgerechnet):** grau `#495057` auf `#f8f9fa` ≈ 7,8:1, Rahmen `#6c757d` auf Weiß ≈ 4,7:1; grün `#0a3622` auf `#d1e7dd` ≈ 10:1,
  Rahmen `#198754` ≈ 4,5:1. Zustand zusätzlich über Text (Platzhalter) und Rahmenform – nicht nur Farbe.
- **Leeren vs. Erkennung:** Text-Pfad setzt das Feld schon per `zuruecksetzen` auf den Stand vor der Folge, der Tasten-Pfad verschluckt die Zeichen;
  das Leeren wirkt also nur auf Rest-Text (siehe Q6-1). `hwScanOhneFeld` liest `feld.value` nicht (nur `data-*`), ein späteres AJAX-Ergebnis
  ist unberührt. Leeren erst nach dem Dispatch.
- **`freiesFeld`:** das sichtbare Feld ist `text`, sichtbar → Text-Erkennung greift; „ohne Fokus“ bleibt `aktiv === ohneFeld` (`:273`),
  Regel (b)/(d) nie ins Scan-Feld (`if (ohne) return`).
- **Auto-Fokus:** `parken` unverändert, `focus({ preventScroll: true })`. Ob Android beim Antippen trotz `inputmode="none"` eine Tastatur zeigt
  oder scrollt: Manual-UAT.
- **Diagnose:** `[aktiv: #id | tag | body]` an jeder Zeile (auch `keydown`), nur `id`/Tag, nie Feldinhalt (Passwortfeld unkritisch). Gilt bei
  Diagnose an auf allen Seiten – so verlangt („jede Diagnose-Zeile“).
- **Fehlerklassen:** kein `@Html.Raw` in `<script>`; `hidden` nicht an `d-*`; kein stiller Fallback; keine Arrays/Model-Binding/Server-Regel
  (nichts gespeichert); keine neue Regel an zwei Orten.

### 3. Beweisart je Punkt des Nachtrags 6
| Punkt | Beweis |
|---|---|
| 1 Sichtbares Feld (kein `aria-hidden`/`opacity`/1 px/`pointer-events`, `inputmode`, `autocomplete`, nur mit Schalter, leer nach Scan) | Markup/CSS/Codestellen: **Quelltext-Wächter** `ScannenOhneFeld_*`. Wirkung (Netum schreibt hinein, keine Tastatur, antippbar, leer): **Manual-UAT** TS-99.19 Vorbedingung, a–e, o, q |
| 2 Fokus-Anzeige grün/grau, Antippen fokussiert | `:focus`-Regel und Platzhaltertexte: Wächter. Darstellung, Kontrast am Gerät: **Manual-UAT** TS-99.19 Vorbedingung, p, q |
| 3 Auto-Fokus wie bisher | Code unverändert (Review). Verhalten: **Manual-UAT** TS-99.19 f/g |
| 4 Diagnose `[aktiv: …]`, „Scan-Feld Fokus an/aus“ | Wächter (Strings). Inhalt am Gerät: **Manual-UAT** TS-99.19 Vorbedingung, p |
| 5 Version, Tests, TS, Hilfe, nur Web | Review: 1.68.1 in beiden `AppVersion.cs`, Changelog, Hilfe-Absatz, TS-99.19 a–q (Vorbedingung, Schritte, Erwartung, Negativ i–n) |
| Q6-1-Fix | Node-Nachstellung mit echtem `createText` (Wegwerfskript, nicht eingecheckt) + Wächter; am Gerät **Manual-UAT** TS-99.19 o |

Browser-JS und Fokusverhalten sind nicht automatisiert beweisbar – alles „Manual-UAT“ zählt nicht als grün.

### 4. Geänderte Bestandstests
- `HardwareScannerTests.ScannenOhneFeld_NurAufEinbuchung_UnsichtbarOhneTastatur_NurMitSchalter` (`69bb4fd1`): Prüfungen `aria-hidden="true"`,
  `tabindex="-1"`, `.hw-scan-ohne-feld { position: fixed;` ersetzt durch `NotContain("aria-hidden")`, `NotContain("tabindex=\"-1\"")`, `hidden`,
  CSS ohne `opacity`/`pointer-events`/`1px`, `:focus`, Fokus-Diagnose, Leeren, `[aktiv:`. **Verhalten absichtlich geändert** durch Nachtrag 6
  Punkt 1 und 5 (Nachtrag nennt den Wächter ausdrücklich). `tabindex` nennt der Nachtrag nicht; die Entfernung folgt aus „sichtbar, antippbar“
  (Abweichung im Dev-Lauf begründet). Kein Fixture-Trick.
- QA-Fix `8540c4d6`: dieselbe Prüfung des Leerens auf die neue Zeile umgestellt (folgt der Code-Korrektur Q6-1).
- `hardware-scanner.check.js` unverändert.

### 5. CLAUDE.md-Checkliste
- Migration, `SQL/`, FreshInstall, `SQL/AgentJobs/`: entfällt. Audit: keine Entität. README: keine neue AppSetting.
- Version 1.68.1 in beiden `AppVersion.cs` ✓, Anwender-Changelog ✓, Hilfe ✓, Brain-Changelog [[2026-10-27-v1-68-1-scan-feld-sichtbar]] ✓ (QA-Fix
  nachgetragen), feature-map ✓.
- `docs/TESTSZENARIEN.md` TS-99.19 ✓ (QA o–q ergänzt), [[testszenarien-index]] Zeile 99 nachgezogen ✓.

**Ergebnis:** grün, Q6-1 behoben, keine Blocker → `status: Testbereit`.

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

**Ergänzung v1.66.1** (Punkt 3 „als Zahl speichern“ und Punkt 18 sind damit überholt; Touch-Fall aus Punkt 6 siehe 19):

19. **TS-99.14:** Stückliste am Netum: „Artikel scannen“ antippen, bei offenem Kamera-Fenster Artikel-QR mit dem eingebauten Scanner →
    Zeile abgehakt, Fenster schließt, kein Hinweis. Negativ: a) Fenster abgebrochen → Hinweis; b) Fenster Ziel-Lagerplatz offen → alte
    Regel, Fenster bleibt; c) fremder Artikel → Meldung wie Kamera; d) Scan vor dem Erscheinen des Fensters → Hinweis; e) Kamera sieht einen
    anderen QR der Liste, gleichzeitig Hardware-Scan → genau **ein** Artikel abgehakt, danach kein „nicht gefunden“ (QA-Fix Q-1).
20. **TS-99.15:** Zeichenabstand `5`/`0`/`-5`/`501` → „Nicht gespeichert … zwischen 10 und 500“, Eingaben bleiben stehen, nichts
    gespeichert. `10`/`500` gespeichert und in `data-hw-scan-abstand` sichtbar. Leer → gespeichert, `data-hw-scan-abstand="50"`. Frisches
    System, nur Firmenname ändern → speichert. Wert per SQL auf `-5` → Scanner erkennt weiter (50 ms).
21. **TS-99.13 erneut:** Kamera-Scan auf Inbound, Bom (Artikel + Ziel-Lagerplatz) und ProductionOrders unverändert, dazu „Abbrechen“,
    während die Kamera einen Code sieht → nichts übernommen (der QA-Fix berührt `startCameraScanner`).
22. **Entscheidung:** `data-hw-scan-kamera-ziel` auf weitere Seiten ausweiten (Kandidaten im Abschnitt „Nachbesserung v1.66.1“)? Wenn ja,
    Folgespec.

**Ergänzung v1.66.2** (Punkt 22 ist mit Nachtrag 3 entschieden; Punkt 19 b gilt jetzt wie TS-99.16 e):

23. **TS-99.16 a–e:** Am Netum je Seite den Kamera-Knopf antippen, Fenster abwarten, mit dem eingebauten Scanner scannen → Ergebnis wie ein
    Kamera-Treffer, Fenster schließt, kein Hinweis: Inbound, Outbound, Transfer (3 Knöpfe), LocationTransfer (2), InboundBulk (Lagerplatz
    + Artikel-Zeile), StockOverview (Artikel/Lagerplatz/FA), FA-Liste und Leitstand (FA + Artikel, Filter genau einmal angewendet), OSEON
    (Kundenauftrag + Artikel), Stückliste Ziel-Lagerplatz (Wagen gewählt). Negativ: f) abgebrochenes Fenster → Hinweis; g) Kamera sieht
    einen Code und gleichzeitig Hardware-Scan (je eine Seite aus a und d) → genau eine Auswertung; h) Gerät ohne Scanner bzw.
    Geräte-Schalter aus → Kamera unverändert.
24. **InboundBulk-Entscheidung:** Die Seite war im Nachtrag nicht genannt, ist aber nach der Regel „mehr als ein Scan-Feld“ freigeschaltet.
    Bestätigen oder Rücknahme anordnen.
25. **Seiten mit genau einem Scan-Feld** (Articles/Info, Ausgang „Alles“, Bewegungsliste): Kamera-Knopf antippen, Hardware-Scan → Feld
    gefüllt, **Fenster bleibt offen** (TS-99 Negativ c). Am Gerät so gewollt? Sonst Folgeauftrag: Attribut auch dort.
26. **Q3-2 beobachten:** OSEON „Artikel“-Kamera-Knopf 5× hintereinander öffnen, abwechselnd mit Kamera und Scanner scannen → Filter wird
    jedes Mal gesetzt. Schlägt es sporadisch fehl (Fenster bleibt, nichts gefiltert), ist das der vorbestehende Doppel-Handler (Abhilfe im
    QA-Nachweis v1.66.2). Auf LocationTransfer „Quell-Lagerplatz“ prüfen, ob die Kamera nach dem Schließen wirklich aus ist.

**Ergänzung v1.67.1** (Nachtrag 4). Punkt 25 ist damit beantwortet: Das Fenster schließt, solange das Feld „Handscanner“ den Fokus hat.

27. **TS-99.17 a–m** am Netum, Ausgabe „Fokus-Eingabe“, Diagnose an. In keinem Feld darf je `~09` stehen, kein Formular darf abgeschickt
    werden. Geprüft werden Inbound, Outbound, das Kamera-Fenster auf Transfer, Stückliste, Articles/Info, BDE, der Spaltenfilter der
    FA-Liste und ein Bemerkungsfeld. In der Diagnose die `keydown`-Zeile des Enters ansehen. Steht dort `keyCode=229` bzw.
    `key="Unidentified"` statt `Enter`/13, wird das Enter nicht abgefangen (Q4-2): Diagnose kopieren und melden.
28. **TS-99.17 c:** Kamera-Knopf antippen. Öffnet sich **keine** Bildschirmtastatur, und kommt der Scan im Feld „Handscanner“ an? Gibt
    der Netum bei `inputmode="none"` nichts aus, bitte melden.
29. **TS-99.17 m + TS-99.1/99.11:** USB-Scanner bzw. Simulation am PC verhalten sich wie vorher, genau eine Auswertung je Scan. Neu dazu:
    auf Articles/Info Kamera-Fenster offen, Tastatur-Scan → Feld gefüllt, Fenster **schließt** (TS-99 Negativ c ist angepasst).
30. **TS-99.18 a–g:**
    - Diagnose ist standardmäßig aus.
    - Die Karte steht vor dem Seitenfuß und zeigt höchstens 30 Zeilen.
    - „Kopieren“ legt den Text unter HTTPS in die Zwischenablage, unter HTTP wird er markiert.
    - Der Netzwerk-Tab zeigt keine Anfragen.
    - Am Handheld lesbar.
    - Passwortfeld erscheint ohne Taste.
31. **Regression TS-99.16** stichprobenhaft mit dem Netum (Inbound, Stückliste Artikel): Das Fenster ist Ziel wie vorher.

**Ergänzung v1.67.2** (Nachtrag 5, Pilot nur Einbuchung). Vorher am Netum: Cache leeren, „Mein Profil“ → „Scannen ohne Feld“ an, Diagnose an.

32. **TS-99.19 a–d:** Inbound öffnen, **nichts antippen**. Artikel-QR (mit und ohne FA), Lagerplatz-Etikett und eine reine Artikelnummer
    scannen, in beiden Reihenfolgen. Jedes Mal wird das richtige Feld gesetzt („✓ Gescannt“). Die Diagnose zeigt `über Scan-Feld (ohne Fokus) → …`
    mit der Zuordnung.
33. **TS-99.19 e–h:** keine Bildschirmtastatur, die Seite springt nicht. Nach einer Select2-Auswahl von Hand und nach dem Schließen des
    Kamera-Fensters kommt der nächste Scan ohne Antippen an. Nach der Lagerplatz-Liste von Hand einmal auf eine freie Stelle tippen. Kein
    Scan schickt das Formular ab (es gibt auch keine Meldung „gespeichert“).
34. **TS-99.19 i–m:** Bei `XYZ999` erscheint der Hinweis „nicht zuordenbar“, und nichts ändert sich. Ein angetipptes Feld „Fertigungsauftrag“
    bekommt den Code. Mit dem Kamera-Knopf geht der Code wie bisher an das Fenster. Mit dem Schalter aus kommt nichts an, wie in v1.67.1.
    Auf Warenausgang ändert sich nichts. TalkBack liest das Feld nicht vor (Q5-5).
35. **Q5-3 beobachten:** Am PC mit Schalter an auf der Einbuchung ohne Fokus tippen: Die Zeichen verschwinden unsichtbar. Für den Pilot am
    Handheld hinnehmbar? Scannt der Netum ohne Präfix (Einstellung verloren), passiert sichtbar nichts. Dann zeigt die Diagnose „durchgelassen“.
36. **Q5-4 entscheiden:** Zuerst die FA von Hand eintippen, dann den Artikel scannen. Die FA wird geleert bzw. durch die FA aus dem QR
    ersetzt. Bei Kamera und angetipptem Feld ist das seit jeher so. Gewollt? Sonst als Folgeauftrag.
37. **Q5-2 (optional):** Gibt es Lagerplatz-Codes, die Anfang eines anderen sind (z. B. `A-1`/`A-10`)? Dann denselben Code einmal ohne Feld
    und einmal über den Kamera-Knopf scannen und vergleichen. Ohne Feld wird genau dieser Platz gewählt, über den Kamera-Knopf ggf. der
    erste per Anfang passende.

**Ergänzung v1.68.1** (Nachtrag 6). Punkt 34 „TalkBack liest das Feld nicht vor“ ist überholt (jetzt „Scan-Feld“, TS-99.19 m); Punkt 35 ist
entschärft (Rest-Text ist sichtbar). Vorher am Netum Cache leeren, „Scannen ohne Feld“ und Diagnose an.

38. **TS-99.19 Vorbedingung:** Inbound öffnen → oben Feld grün „✓ Scan bereit“, Diagnose „Scan-Feld Fokus an … [aktiv: #hwScanOhneFeld]“.
    Ist es grau, einmal antippen → grün, **keine** Bildschirmtastatur. **Kernfrage des Nachtrags:** Kommt jetzt eine `input`-Zeile in der
    Diagnose und wird der Scan zugeordnet (a–d)? Wenn nicht: Diagnose kopieren und melden (dann trägt auch ein sichtbares Feld nicht).
39. **TS-99.19 n:** Schalter aus → kein Scan-Feld, Seite wie v1.68.0 (bis auf ca. 8 px Abstand unter der Überschrift, Q6-2 – stört das?).
40. **TS-99.19 o (QA-Fix Q6-1):** drei Codes direkt hintereinander → jeder erkannt, Feld nach jedem leer. Zusätzlich am PC mit Schalter an:
    `xx` ins Scan-Feld tippen, dann zweimal hintereinander (Simulation) scannen → beide erkannt.
41. **TS-99.19 p:** Formularfeld antippen / freie Stelle antippen → je genau eine Zeile „Fokus aus“ / „Fokus an“, keine Wiederholung.
42. **TS-99.19 q:** Am Handheld hochkant Feld ganz sichtbar, Text nicht abgeschnitten, kein Querscrollen; Zustand ohne Farbe erkennbar.
43. **TS-99.19 a–m** wie Punkte 32–34 erneut (Zuordnung unverändert), dazu **TS-99.17 a/i** auf einer anderen Seite: Diagnose-Zeilen tragen
    dort ebenfalls `[aktiv: …]`, sonst unverändert.
