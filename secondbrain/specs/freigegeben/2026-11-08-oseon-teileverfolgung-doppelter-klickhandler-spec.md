---
type: spec
title: OSEON-Teileverfolgung – doppelter Klickhandler am Artikel-QR-Knopf (erst nachstellen, dann ggf. Guard in initScanner)
slug: 2026-11-08-oseon-teileverfolgung-doppelter-klickhandler-spec
status: InUmsetzung
created: 2026-11-07
updated: 2026-11-08
source_backlog: "[[2026-11-07-oseon-teileverfolgung-doppelter-klickhandler]]"
bug: "[[2026-11-08-oseon-teileverfolgung-doppelter-klickhandler-bug]]"
buendel: "[[2026-11-08-buendel-uebersicht]]"
task: "[[2026-11-08-buendel-2026-11-08-goal-lauf]]"
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
version: "1.82.0"   # nur bei Codeaenderung (Schritt 1 reproduziert); sonst keine Version
standort: beide
affected_code:
  - "IdealAkeWms/wwwroot/js/barcode-scanner.js:10-21 (initScanner ohne Ziel-Pruefung)"
  - "IdealAkeWms/wwwroot/js/barcode-scanner.js:670-675 (globaler Start initScanner('btnScanArticle','ArticleId',…))"
  - "IdealAkeWms/wwwroot/js/barcode-scanner.js:56-70, 185-229 (openScannerModal ersetzt Fenster; startCameraScanner-catch setzt _activeScanner = null)"
  - "IdealAkeWms/wwwroot/js/barcode-scanner.js:415-431 (initTextInputScanner prueft Ziel, :418)"
  - "IdealAkeWms/Views/Tracking/OseonIndex.cshtml:26 (Knopf btnScanArticle), :233-239, :312-318"
  - "IdealAkeWms/Views/StockMovements/LocationTransfer.cshtml:124-128 (gleicher Mechanismus, gleiches Ziel - Rueckfrage 2)"
  - "IdealAkeWms.Tests/Views/HardwareScannerTests.cs:112, 122-138 (Text-Waechter-Muster)"
  - "docs/TESTSZENARIEN.md (Kap. 17.3, neu TS-114)"
open_questions:
  - "Was gilt in Schritt 1 als 'reproduziert'? Empfehlung: nur eine sichtbare Fehlwirkung (falsches Ziel im Fenster, Scan fuellt das Feld nicht / kein Submit, Kamera bleibt nach dem Schliessen an, Meldung 'Kamera nicht verfuegbar' trotz Kamera, Konsolenfehler, doppelte Kamera-Freigabe-Abfrage). Zwei registrierte Listener allein = nicht reproduziert: keine Codeaenderung, Befund festhalten."
  - "LocationTransfer.cshtml:128 registriert btnScanSourceLocation ein zweites Mal (gleiches Ziel wie barcode-scanner.js:673). In Schritt 1 mit nachstellen und bei Befund mitbeheben (Zeile loeschen)? Empfehlung: ja, gleicher Mechanismus, eine Zeile."
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: false
freigabe_entscheidung: ""
freigabe_von: "Gerald Weichbold"
freigabe_am: "2026-11-07"
---

## Ziel / Nutzen (das Warum)

Am AKE-Standort öffnet der QR-Knopf neben „Artikelnummer“ in der OSEON-Teileverfolgung möglicherweise zwei Kamera-Fenster gleichzeitig. Welches
bleibt, hängt dann vom Timing ab. Der Werker scannt, und der Filter greift nicht oder die Kamera bleibt an. Der Befund ist **zur Laufzeit ungeprüft**
(Backlog). Diese Spec schreibt deshalb **zuerst das Nachstellen** vor. Code ändert sich nur, wenn der Fehler sichtbar wird.

**Mechanismus (Code, belegt):**
- `OseonIndex.cshtml:26` gibt dem Knopf die ID `btnScanArticle`. Diese ID ist auch in `barcode-scanner.js` reserviert.
- Reihenfolge beim Laden: `barcode-scanner.js` wird in `OseonIndex.cshtml:235` vor dem Inline-Skript `:238-239` geladen. Zuerst registriert der
  globale `DOMContentLoaded`-Start `initScanner('btnScanArticle', 'ArticleId', 'article')` (`barcode-scanner.js:670-671`). Danach registriert
  die Seite `initTextInputScanner('btnScanArticle', 'filterArticle', …)` (`OseonIndex.cshtml:312`).
- `initScanner` prüft nur den Knopf, nicht das Ziel (`barcode-scanner.js:11-12`). `initTextInputScanner` prüft beides (`:416-418`).
  Ein Element `ArticleId` gibt es in `OseonIndex.cshtml` nicht. Damit hängen am Knopf **zwei** Klick-Handler, und `_hwScanTargets` hat zwei
  Einträge (`:20`, `:429`).
- Ein Klick ruft `openScannerModal` zweimal auf, mit den Zielen `ArticleId` und `filterArticle`. Mit Kamera (sicherer Kontext, `:61-64`) wartet
  jeder Aufruf auf `getUserMedia`. Der später fertige Aufruf entfernt das Fenster des früheren (`:68-70`) und startet einen zweiten `Html5Qrcode`
  (`:152-154`, `:185-190`).
- Mögliche Folgen, **unverifiziert**:
  - (a) Zwei Kamera-Freigabe-Abfragen.
  - (b) Der erste Scanner läuft auf einem entfernten Element weiter. Die Kamera bleibt dann an, weil `stopScanner` nur den zuletzt gesetzten
    `_activeScanner` stoppt (`:231-240`).
  - (c) Schlägt der Start des ersten Scanners fehl, setzt sein `catch` `_activeScanner = null` und schaltet im **verbliebenen** Fenster auf
    „Kamera nicht verfügbar. Bitte Bild hochladen.“ (`:213-228`).
  - (d) Gewinnt das Fenster mit Ziel `ArticleId`, findet `processScannedValue` kein Element und kehrt still zurück (`:317-318`). Das Feld bleibt
    leer, und es gibt keinen Submit.
- Ohne Kamera bzw. unsicheren Kontext läuft alles synchron. Das zweite Fenster (`filterArticle`) gewinnt dann, und der Fehler ist vermutlich unsichtbar.
- Der Hardware-Scanner ist **nicht** betroffen: Er nimmt nur Ziele, deren Element existiert (`:616`).

**Gleicher Mechanismus anderswo:**
- Grep `init(TextInput)?Scanner('btnScan(Article|StorageLocation|SourceLocation|ArticleInfo)'` liefert nur `OseonIndex.cshtml:312` und
  `LocationTransfer.cshtml:128`.
- Letzterer registriert `btnScanSourceLocation` mit **demselben** Ziel wie `barcode-scanner.js:673`. Auch dort hängen zwei Handler (Rückfrage 2).
- Alle anderen Seiten mit global reservierten Knopf-IDs haben ihr Ziel-Element:
  - `ArticleId`: `Inbound.cshtml:34`, `Outbound.cshtml:21`, `Transfer.cshtml:21`
  - `StorageLocationId`: `Inbound.cshtml:59`, `Outbound.cshtml:46`, `Transfer.cshtml:63`, `InboundBulk.cshtml:23`, `OutboundAll.cshtml:19`
  - `SourceStorageLocationId`: `Transfer.cshtml:46`, `LocationTransfer.cshtml:19`
  - `articleNumber`: `Articles/Info.cshtml:20`

Bug-Record: Der Auftrag dieses Spec-Laufs beschränkt die Schreibziele auf die Spec-Dateien. Den Bug-Record legt der Mensch bzw. die Koordination an.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- **Schritt 1 (Pflicht, vor jeder Codeänderung):** im Browser nachstellen, Befund in diese Spec schreiben.
- Nur wenn Schritt 1 reproduziert (Definition: Rückfrage 1):
  - Ein Guard in `initScanner` (Ziel muss existieren).
  - Bei Rückfrage 2 = ja: `LocationTransfer.cshtml:128` löschen.
  - Text-Wächter-Test, TS-114, Anwender-Changelog, Version v1.82.0.
- Nicht reproduziert: **keine Codeänderung**, keine Version. Die Spec schließt mit dem Befund (Abschnitt „Befund Schritt 1“) und einer Empfehlung an den Menschen.

**Out-of-Scope (nur melden)**
- `LocationTransfer.cshtml:124` und `Articles/Info.cshtml:278` laden `html5-qrcode` vom CDN `unpkg.com`, `OseonIndex.cshtml:234` aus `~/lib`.
  Das ist eine abweichende Quelle und ein Betriebsrisiko ohne Internet, aber nicht Teil dieses Befunds.
- Ein Umbenennen der Knopf-ID in `OseonIndex` (z. B. `btnScanArticleFilter` wie `StockOverview/Index.cshtml:17,159`) entfällt zugunsten des Guards.
  Das Umbenennen würde nur eine Seite heilen und die Liste `HardwareScannerTests.cs:134` ändern.

## Fachliche Anforderungen

1. Ein Klick auf den QR-Knopf „Artikelnummer“ in `/Tracking/OseonIndex` öffnet genau **ein** Scan-Fenster mit Ziel `filterArticle`.
2. Ein Scan bzw. Bild-Upload füllt das Feld Artikelnummer und schickt das Formular ab (TS-17.3 unverändert).
3. Nach Schließen oder Treffer ist die Kamera aus (Geräte-Anzeige/Browser-Symbol).
4. Alle anderen Scan-Knöpfe verhalten sich unverändert.

## Technischer Loesungsentwurf

### Schritt 1 – Nachstellen (Pflicht, Gate für alles Weitere)

Umgebung: AKE-DEV `http://localhost:8081` (`localhost` gilt als sicherer Kontext, `barcode-scanner.js:28`, also wird der Kamera-Pfad durchlaufen),
alternativ AKE-Test. Gerät mit Kamera (Notebook-Webcam oder Tablet), Chrome/Edge mit DevTools. Rolle mit Zugriff auf die OSEON-Teileverfolgung.

1. `/Tracking/OseonIndex` öffnen, DevTools-Konsole:
   - `getEventListeners(document.getElementById('btnScanArticle')).click.length` → **erwartet mit Fehler: 2** (nur Chrome/Edge-DevTools).
   - `_hwScanTargets.filter(z => z.buttonId === 'btnScanArticle').map(z => z.targetId)` → **erwartet: `['ArticleId','filterArticle']`**.
2. Knopf klicken, fünfmal wiederholen (Fenster jeweils schließen). Notieren:
   - Anzahl Kamera-Freigabe-Abfragen beim ersten Mal (vorher die Freigabe für die Seite zurücksetzen).
   - `document.getElementById('scannerModal').dataset.zielId` direkt nach dem Öffnen (`filterArticle` oder `ArticleId`).
   - `document.querySelectorAll('#scannerModal').length`.
   - Erscheint „Kamera nicht verfügbar. Bitte Bild hochladen.“, obwohl die Kamera auf `/StockOverview` funktioniert?
   - Bleibt die Kamera-Anzeige nach „Abbrechen“ an?
   - Rote Einträge in der Konsole.
3. Einen Artikel-QR scannen (oder „Bild hochladen“ mit QR-Bild). Notieren: Wird das Feld gefüllt und das Formular abgeschickt (URL enthält `filterArticle=`)?
4. Gegenprobe ohne Kamera (Rechner ohne Webcam oder Kamera im Browser blockiert): einmal Schritt 2/3. Notieren: Was geschieht?
5. (Rückfrage 2) `/StockMovements/LocationTransfer` mit `btnScanSourceLocation`: Schritt 1 und 2 analog.

**Auswertung:** Ein Befund laut Rückfrage 1 heißt „reproduziert“, dann folgt Schritt 2. Sonst ist die Spec mit Befund abgeschlossen: Ergebnisse unter
„Befund Schritt 1“ eintragen, keine Codeänderung, keine Version, Empfehlung an den Menschen.
**Kann der Lauf keinen Browser bedienen** (kein Zugriff auf DevTools/Kamera), trägt er „Schritt 1 offen – Mensch“ als Blocker ein und ändert **keinen**
Code. Er rät nicht.

### Schritt 2 – Fix (nur nach Reproduktion)

- `barcode-scanner.js:12`: `if (!button || !document.getElementById(targetSelectId)) return;`. Das ist dieselbe Regel wie `initTextInputScanner:418`, eine Zeile an der
  gemeinsamen Stelle. Damit registriert der globale Start auf `OseonIndex` keinen Handler für das fehlende `ArticleId` mehr. Seiten mit Ziel
  (Liste oben) bleiben unverändert.
- (Rückfrage 2 = ja) `LocationTransfer.cshtml:128` löschen. Der globale Start (`barcode-scanner.js:673`) registriert genau denselben Knopf mit demselben
  Ziel. `:129` (`btnScanTargetLocation`) bleibt, denn diese ID ist nicht global.
- Ein Idempotenz-Guard in beiden Init-Funktionen (gleicher Knopf zweimal → ignorieren) kommt **nicht**. Bei OseonIndex würde er den falschen,
  zuerst registrierten Handler behalten.
- Wächter-Test in `HardwareScannerTests.cs` im bestehenden Text-Muster (`:112`):
  - (a) `barcode-scanner.js` enthält die Guard-Zeile.
  - (b) Keine View ruft `initScanner`/`initTextInputScanner` mit einer der vier global reservierten Knopf-IDs auf (`btnScanArticle`,
    `btnScanStorageLocation`, `btnScanSourceLocation`, `btnScanArticleInfo`). Ausnahme ist `OseonIndex` mit `initTextInputScanner('btnScanArticle', 'filterArticle', …)`,
    das nach dem Guard das einzige Ziel ist. Diese Ausnahme steht als ausdrückliche Liste im Test, damit eine neue Doppelregistrierung auffällt.
- Die Liste `HardwareScannerTests.cs:122-135` (Kamera-Knöpfe) bleibt unverändert, denn keine ID ändert sich.

**Frontend:** Skill `frontend-design` ist formal Pflicht (Oberflächen-JavaScript). Sichtbar ändert sich nichts.

## Migrations-/SQL-Auswirkungen

Keine. Kein Schema, keine Migration, kein `SQL/XX`, kein `00_FreshInstall.sql`, keine `SQL/AgentJobs/`. Falls der Dev-Lauf doch Schema braucht,
stoppt er und meldet das als offene Frage. Er vergibt keine SQL-Nummer selbst.

## Einstellungen

keine. Die Scanner-Einstellungen (`HardwareScannerAktiv`/`-Praefix`, `QrErstesFeld`, `QrTrennzeichen`) bleiben unverändert.

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| – | – | – | – | – | – | – | – |

## Audit-Feld-Auswirkungen

Keine. Das ist reines Frontend, nichts wird geschrieben.

## Rollen / Zugriff

Unverändert. Es gibt keine neue Rolle, keinen neuen Filter und keine Server-Änderung.

## Listen-View-Pattern

Nicht betroffen (keine Tabelle, keine Filterlogik).

## Akzeptanzkriterien

1. **Schritt 1 dokumentiert:** Der Abschnitt „Befund Schritt 1“ enthält Umgebung, Browser, Gerät, Konsolenwerte aus 1.1, Beobachtungen aus 1.2 bis 1.4 (und 1.5 bei
   Rückfrage 2) und das Urteil „reproduziert“ / „nicht reproduziert“ laut Rückfrage 1.
2. Bei „nicht reproduziert“: Der Diff enthält **keine** Änderung an Code, View, Version oder Changelog. Die Spec endet mit dem Befund und einer Empfehlung.
3. Bei „reproduziert“: Auf `/Tracking/OseonIndex` ist `getEventListeners(...btnScanArticle).click.length` = 1 und `_hwScanTargets` für den Knopf
   = nur `filterArticle`.
4. Bei „reproduziert“: Fünf Klicks hintereinander öffnen jeweils genau ein Fenster mit `dataset.zielId = 'filterArticle'`, die Kamera ist nach dem Schließen aus. Ein
   Scan füllt das Feld und schickt das Formular ab.
5. Bei „reproduziert“: Regression auf `Inbound`, `Outbound`, `Transfer`, `InboundBulk`, `OutboundAll`, `LocationTransfer`, `Articles/Info`. Jeder
   Scan-Knopf öffnet sein Fenster, ein Scan füllt das richtige Feld.
6. Wächter-Test (a) und (b) sind grün. `dotnet build IdealAkeWms.slnx` und `dotnet test` sind grün. Version 1.82.0 in beiden `AppVersion.cs`, Anwender-Changelog.

## Test-Szenarien

Neues Kapitel **TS-114** in `docs/TESTSZENARIEN.md` (nur bei Codeänderung; sonst bekommt TS-17.3 eine Notiz mit dem Befund). Querverweis aus TS-17.3,
danach `secondbrain/tests/testszenarien-index.md`.

**TS-114 – OSEON-Teileverfolgung: ein Scan-Fenster je Klick (v1.82.0)**
- Vorbedingungen: AKE (OSEON aktiv). Gerät mit Kamera, Chrome/Edge. Rolle für die OSEON-Teileverfolgung. Artikel-QR eines Artikels mit OSEON-Aufträgen.
- Schritte / Erwartet:
  1. `/Tracking/OseonIndex`, QR-Knopf „Artikelnummer“. **Erwartet:** genau ein Fenster, eine Kamera-Freigabe-Abfrage (beim ersten Mal).
  2. QR scannen. **Erwartet:** Feld gefüllt, Formular abgeschickt, Liste gefiltert (TS-17.3).
  3. Erneut öffnen, „Abbrechen“. **Erwartet:** Kamera aus. Fünfmal wiederholen, gleiches Ergebnis.
  4. QR-Knopf „Auftrags-/Kundenauftragsnr.“. **Erwartet:** unverändert ein Fenster, FA-Wert übernommen.
- Negativfälle / Regression:
  5. Ohne Kamera: „Bild hochladen“ mit QR-Bild. **Erwartet:** Feld gefüllt, Submit.
  6. Hardware-Scanner (falls aktiv) mit Fenster offen. **Erwartet:** wie TS-99, Wert landet in Artikelnummer.
  7. Je ein Scan auf Einbuchung, Ausbuchung, Umbuchung, Lagerplatz-Umbuchung, Sammel-Einbuchung, Ausbuchung (alle) und Artikelinfo. **Erwartet:** unverändert.
  8. IDEAL-DEV (`localhost:8082`): Smoke-Test einer Scan-Seite (z. B. Artikelinfo). **Erwartet:** unverändert.

## Etappen (nur bei epic: true)

Kein Epic. Die Tabelle hält die **verbindliche Reihenfolge** der zwei Schritte fest. Schritt 2 startet nur nach „reproduziert“ in Schritt 1.

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| 1 | Nachstellen im Browser (AKE-DEV/AKE-Test), Befund in die Spec | offen | – (nur Spec) |
| 2 | Guard `initScanner` + ggf. `LocationTransfer.cshtml:128` + Wächter-Test + TS-114 + v1.82.0 | offen, bedingt | |

## Befund Schritt 1

*Vom Dev-Lauf auszufüllen (Umgebung, Werte, Urteil).*

## Deploy

*Vorläufig, der QA-Lauf bestätigt gegen den echten Diff. Bei „nicht reproduziert“ entfällt der Deploy.*

- **Web-App:** ja (nur bei Fix). Betroffen sind `wwwroot/js/barcode-scanner.js`, ggf. `Views/StockMovements/LocationTransfer.cshtml`, `Help/Changelog`, `AppVersion.cs`.
  Statische Datei: `asp-append-version` erneuert den Cache-Schlüssel, ein Browser-Neuladen genügt.
- **Service:** ja, **nur Versions-Bump** (1.82.0) bei Fix.
- **Migration:** nein.
- **Publish-Befehle** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`
- **Standort:** beide. Der Befund betrifft nur die OSEON-Seite (AKE). Der Guard sitzt aber in `barcode-scanner.js`, das an beiden Standorten alle Scan-Seiten
  laden. Deshalb ist IDEAL ein Regressions-Smoke-Test.

## Offene Rueckfragen

1. **Definition „reproduziert“ für Schritt 1.** *Empfehlung:* Es zählt nur eine sichtbare Fehlwirkung: falsches Ziel im Fenster
   (`zielId = ArticleId`), Scan füllt das Feld nicht bzw. kein Submit, Kamera bleibt nach dem Schließen an, „Kamera nicht verfügbar“ trotz Kamera,
   Konsolenfehler oder doppelte Kamera-Freigabe-Abfrage. Zwei registrierte Listener allein (Konsolenwert 1.1) gelten als **nicht reproduziert**:
   keine Codeänderung, Befund festhalten. So steht es auch im Auftrag „bei nicht reproduzierbarem Befund keine Codeänderung“.
2. **`LocationTransfer.cshtml:128`** registriert `btnScanSourceLocation` ein zweites Mal mit demselben Ziel wie `barcode-scanner.js:673`. In Schritt 1 mit
   nachstellen und bei Befund mitbeheben (Zeile löschen)? *Empfehlung: ja.* Gleicher Mechanismus, eine Zeile. Der Wächter-Test (b) würde den Fall sonst
   als Ausnahme führen müssen.

## Schritt-1-Ergebnis (Dev, 2026-11-08, AKE-DEV `localhost:8081`, v1.79.0) — BLOCKER: Schritt 1 offen – Mensch (Kamera)

> [!warning] Korrektur nach QA (2026-11-08)
> Die Einordnung „keine sichtbare Fehlwirkung“ trägt nicht: Der Lauf-PC hat keine Kamera, `startCameraScanner` wird nie erreicht, und
> alle möglichen Effekte (a)–(d) der Spec liegen hinter diesem Zweig. Laut Spec gilt dann „Schritt 1 offen – Mensch“. Codeänderung und
> Version 1.82.0 bleiben aus, bis Gerald entscheidet: (1) Schritt 1 am Gerät mit Kamera nachstellen, oder (2) den geplanten Guard
> (`initScanner`, eine Zeile, plus Entfernen `LocationTransfer.cshtml:128`) gleich bauen und TS-114 am Gerät als Abnahme nehmen.
> QA-Leseweise (nicht gemessen): Mit Kamera starten zwei Html5Qrcode-Instanzen; entweder bleibt die Kamera nach „Abbrechen“ an, oder die
> erste schlägt fehl und schaltet das Fenster auf „Kamera nicht verfügbar“.

Nachgestellt im Browser (Automations-Chrome, keine Kamera am Gerät):
- **OSEON-Teileverfolgung** `/Tracking/OseonIndex`: `_hwScanTargets` enthält für `btnScanArticle` **zwei** Einträge (`ArticleId`, `filterArticle`).
  Ein Klick ruft `openScannerModal` **zweimal** auf (erst `ArticleId|article`, dann `filterArticle|article`; Zählung per Wrapper um
  `openScannerModal`). Ergebnis im DOM: **ein** `#scannerModal`, `data-ziel-id = filterArticle` (der zweite Aufruf gewinnt), kein Backdrop-Rest.
  Hardware-Scan `00722001` im Fenster (Ereignis `wms:hardware-scan`, Fokus im Handscanner-Feld): Feld „Artikel“ gefüllt, Filter **einmal**
  abgeschickt (`filterArticle=00722001`). Das nicht vorhandene `ArticleId` fällt im Handler über `vorhanden` heraus.
- **Lagerplatz umbuchen** `/StockMovements/LocationTransfer`: `btnScanSourceLocation` zweimal registriert (beide `SourceStorageLocationId`),
  ein Klick = zwei `openScannerModal`-Aufrufe mit **gleichem** Ziel.
- **Bewertung nach Freigabe-Antwort 1:** keine sichtbare Fehlwirkung auf DEV (ein Fenster, richtiges Ziel, ein Absenden) → **keine Codeänderung,
  keine Version 1.82.0**. Nicht prüfbar im Lauf: der Kamera-Fall am Gerät (zwei Starts von html5-qrcode kurz hintereinander, ggf. doppelte
  Kamera-Abfrage). Zeigt sich das am Handgerät sichtbar, wird die Spec mit dem Guard aus dem Lösungsentwurf wieder aufgenommen.
- **Zusatzfund (nicht mitgebaut, Backlog [[2026-11-08-oseon-relevanzfilter-schaltet-sich-ab]]):** Auf derselben Seite schaltet jedes Absenden
  der Filterkarte „Fremdsystem-Relevanz“ still ab. Das versteckte `false`-Feld steht **vor** der Checkbox (`OseonIndex.cshtml:52-53`), angehakt
  kommt `useRelevanceFilter=false&useRelevanceFilter=true`, der Binder nimmt `false` (DEV: nach dem Scan-Absenden Haken aus).

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. Definition „reproduziert“ (nur sichtbare Fehlwirkung; zwei Listener allein = keine Änderung) → **wie empfohlen (Gerald 2026-11-07)**
2. `LocationTransfer.cshtml:128` mit nachstellen und bei Befund mitbeheben → **wie empfohlen (Gerald 2026-11-07)**

## QA-Bewertung (qa-agent, 2026-11-07, Bündel 2026-11-08 Lauf „A + Wareneingang“) – kein Status gesetzt

**Ergebnis: Die Einordnung „nicht reproduziert“ trägt nicht. Status bleibt `InUmsetzung`, nicht Testbereit.** Begründung unten. Code-Diff: keiner (korrekt, nichts zu reviewen).

### Unabhängig nachgestellt (AKE-DEV `localhost:8081`, v1.81.0, Automations-Chrome)
- `/Tracking/OseonIndex`, Knopf `btnScanArticle` einmal geklickt: Konsole zeigt **zweimal** `Camera permission pre-warm failed: NotFoundError: Requested device not found`
  (`barcode-scanner.js:50`, je `openScannerModal`-Aufruf eine Meldung) → zwei Aufrufe je Klick bestätigt. Fenster „Kamera nicht verfügbar. Bitte ein Bild des Codes hochladen.“
  (hier korrekt, es gibt keine Kamera).
- `/StockMovements/LocationTransfer`, `btnScanSourceLocation`: ebenfalls **zwei** Meldungen je Klick → Doppelregistrierung bestätigt.
- Konsolenwerte 1.1 (`getEventListeners`, `_hwScanTargets`) habe ich nicht selbst ausgelesen (kein JS-Werkzeug in diesem Lauf); die zwei Konsolenmeldungen belegen dasselbe.

### Warum die Einordnung nicht trägt
1. **Der riskante Pfad lief nie.** Auf diesem Rechner gibt es keine Kamera: `requestCameraPermission()` liefert `false` (NotFoundError), damit ist `cameraAvailable = false`
   und `startCameraScanner` (`barcode-scanner.js:151-153`) wird in **keinem** der beiden Aufrufe erreicht. Alle in der Spec genannten möglichen Folgen (a)–(d) liegen
   hinter diesem Zweig. „Keine sichtbare Fehlwirkung“ ist hier also kein Befund über den Fehler, sondern über eine Umgebung, die ihn nicht auslösen kann.
2. **Die Spec regelt genau diesen Fall selbst** (Schritt 1, Auswertung): „Kann der Lauf keinen Browser bedienen (kein Zugriff auf DevTools/**Kamera**), trägt er
   ‚Schritt 1 offen – Mensch‘ als Blocker ein und ändert keinen Code. Er rät nicht.“ Richtig wäre also: Schritt 1 offen – Mensch, nicht „nicht reproduziert“.
3. **Der Code macht eine sichtbare Folge mit Kamera wahrscheinlich** (Lesart `barcode-scanner.js:56-70,151-153,185-229`, nicht gemessen):
   Beide Aufrufe warten parallel auf `getUserMedia`. Der erste baut Fenster 1 und startet `Html5Qrcode` #1 auf `#scannerReader`; der zweite entfernt Fenster 1 sofort,
   baut Fenster 2 und startet #2, `_activeScanner` = #2. Dann gilt eines von beiden:
   - #1 startet trotzdem auf dem entfernten Element → Stream bleibt offen, `stopScanner()` stoppt nur #2 → **Kamera bleibt nach „Abbrechen“ an** (Folge b).
   - #1 scheitert (z. B. Android: Kamera schon belegt, `NotReadableError`) → sein `catch` setzt `_activeScanner = null` und schaltet das **verbliebene** Fenster auf
     „Kamera nicht verfügbar“ (Folge c) – trotz Kamera; ein laufender #2 wird danach von `stopScanner` nicht mehr gestoppt.
   Beides fällt unter die Definition „reproduziert“ aus Freigabe-Antwort 1. Auf `LocationTransfer` gilt dasselbe (gleiches Ziel, zwei Starts).

### Empfehlung an den Menschen (Entscheidung, keine Codeänderung durch QA)
- **Option 1 (Spec wie geschrieben):** Schritt 1 am Handgerät mit Kamera (AKE-Test oder DEV über `localhost`/HTTPS) nachstellen: Schritte 1.2 (Kamera-LED nach „Abbrechen“,
  Meldung „Kamera nicht verfügbar“) und 1.5. Zeigt sich (b) oder (c): Schritt 2 (Guard + `LocationTransfer.cshtml:128` löschen + Wächter-Test + TS-114 + v1.82.0).
- **Option 2 (vorgezogen):** Schritt 2 ohne Gerätebeweis umsetzen – eine Guard-Zeile an der gemeinsamen Stelle plus eine gelöschte Zeile; Risiko gering, da alle anderen
  Seiten ihr Ziel-Element haben (Spec-Liste). Dann ist TS-114 am Gerät der Abnahme-Nachweis.
- Ein Zustand „Testbereit ohne Codeänderung“ ist hier kein gültiger Abschluss, weil der Nachweis, auf dem er beruhen müsste (Kamera-Fall), gerade fehlt.

### Build/Tests/Deploy (Bündel, zur Vollständigkeit)
Build 0 Fehler; `IdealAkeWms.Tests` 2451/1 übersprungen/0, `IDEALAKEWMSService.Tests` 497/0/0; DEV AKE und IDEAL auf v1.81.0 (HEAD `747c1ced`), Exit 0. Keine DEV-Daten geändert.

### Manuelle Testliste (offen für den Menschen)
1. Schritt 1 am Gerät mit Kamera auf `/Tracking/OseonIndex` (5× öffnen/abbrechen, Kamera-Anzeige, Meldung, Scan füllt `filterArticle` und schickt ab).
2. Dasselbe auf `/StockMovements/LocationTransfer` mit `btnScanSourceLocation`.
3. Entscheidung Option 1 oder 2 (siehe oben).
