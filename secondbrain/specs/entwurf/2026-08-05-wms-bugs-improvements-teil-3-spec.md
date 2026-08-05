---
type: spec
title: "Bewegungshistorie: Scan-Button fuer WA-Strichcode mit 7-stelliger Truncation"
slug: 2026-08-05-wms-bugs-improvements-teil-3-spec
status: Entwurf
created: 2026-08-05
updated: 2026-08-05
source_backlog: "[[2026-08-05-WmsBugs&Improvements]]"
depends_on: ""
task: ""
worktree: ""
branch: ""
affected_code:
  - IdealAkeWms/Views/StockMovements/Index.cshtml
  - IdealAkeWms/wwwroot/js/barcode-scanner.js
  - docs/TESTSZENARIEN.md
  - secondbrain/tests/testszenarien-index.md
open_questions:
  - "Ist die FA-/WA-Nummer an BEIDEN Standorten (AKE und IDEAL, vgl. laufende Spec [[2026-07-28-ideal-anpassungen-neu-nachbilden-spec]]) verlaesslich genau 7-stellig? Falls die IDEAL-Linie eine andere Laenge verwendet, waere ein hartes substring(0,7) dort falsch."
  - "Soll die Kuerzung ausschliesslich beim Scannen greifen (Client-JS, wie hier vorgeschlagen), oder auch beim manuellen Eintippen/Einfuegen in das Fertigungsauftrag-Filterfeld der Bewegungshistorie?"
  - "Reicht ein einfaches substring(0,7) auf den rohen Scan-Wert, oder soll vorher gezielt an '-' bzw. '_' getrennt werden (robuster falls die Ergaenzung nicht exakt ab Position 8 beginnt)?"
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

Beim Scannen eines WA-/FA-Strichcodes in der Bewegungshistorie (Filter „Fertigungsauftrag") soll
nur die eigentliche, 7-stellige WA-Nummer für die Filterung verwendet werden. Der gedruckte
Strichcode trägt gelegentlich eine zusätzliche Ergänzung nach einem „-" oder „_" (z. B. eine
fortlaufende Zahl für Teillieferungen/Positionen), die für die Filterung irrelevant ist und den
Treffer sonst verhindert oder verfälscht.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Ein Scan-Button für das Filterfeld „Fertigungsauftrag" auf der Bewegungshistorie-Seite
  (`/StockMovements/Index`) — existiert dort aktuell **nicht** (siehe Ist-Zustand).
- Client-seitige Kürzung des gescannten Werts auf die ersten 7 Zeichen, bevor er in das
  Filterfeld übernommen und die Filterung ausgelöst wird.

**Out-of-Scope**
- Keine Änderung an der server-seitigen Filterlogik
  (`StockMovementRepository.GetMovementHistoryAsync`, `ApplyMovementColumnFilter`) — die
  `Contains`-Filterung bleibt unverändert, nur der **Eingabewert** wird vor dem Absenden gekürzt.
- Keine Änderung an anderen Scan-Stellen im Projekt (Einbuchung/Ausbuchung/Umbuchung,
  Tracking-Teileverfolgung) — dort gilt weiterhin die bestehende Komma-Suffix-Logik
  (`.split(',')[0]`, Fallstrick „QR-Code Komma-Suffix").
- Keine Änderung am server-seitigen Spaltenfilter `production-order` (Server-Mode-Filter über
  `?colf_production-order=`) — nur der klassische GET-Filter `filterProductionOrder` (Textfeld im
  Filter-Formular) bekommt den Scan-Button.

## Fachliche Anforderungen

1. Ein Scan-Button neben dem Textfeld „Fertigungsauftrag" im Filter-Formular der
   Bewegungshistorie.
2. Nach dem Scan wird der erkannte Wert auf die ersten 7 Zeichen gekürzt und in das Filterfeld
   übernommen.
3. Beispiel: Scan liefert `2610063-1` oder `2610063_02` → Filterfeld erhält `2610063`.

## Ist-Zustand (Code-Referenzen)

`IdealAkeWms/Views/StockMovements/Index.cshtml:56-59`:
```html
<div class="col-md-3">
    <label class="form-label">Fertigungsauftrag</label>
    <input type="text" name="filterProductionOrder" value="@Model.FilterProductionOrder" class="form-control" />
</div>
```
Das Feld hat **keine** `id`, **keinen** Scan-Button und die View lädt in ihrem `@section Scripts`
(Zeile 145-147) nur `table-filter.js` — **nicht** `barcode-scanner.js`. Ein Scan von der
Bewegungshistorie aus ist heute technisch nicht möglich; das Backlog-Item verlangt also eine
**neue** Funktion (Scan-Button), nicht nur eine Korrektur einer bestehenden.

Vorhandene, wiederverwendbare Scan-Infrastruktur (`IdealAkeWms/wwwroot/js/barcode-scanner.js`):
- `initTextInputScanner(buttonId, targetInputId, valueExtractor, onScanned)`
  (Zeilen 391-405) — leichtgewichtiger Scanner für reine Text-Inputs (kein Select2), bereits
  produktiv genutzt in `Views/Tracking/OseonIndex.cshtml:320-326` für das FA-Filterfeld dort
  (`valueExtractor: 'fa'`).
- `valueExtractor: 'fa'` mappt intern auf `scanType: 'productionOrder'`
  (`initTextInputScanner`, Zeile 403), was in `processScannedValue` (Zeilen 269-294) für
  `scanType === 'productionOrder'` bereits eine Komma-Suffix-Kürzung durchführt
  (`parts[2].trim().split(',')[0]` bzw. Fallback `value.trim().split(',')[0]`) — das ist die
  bestehende, **andere** Ergänzungslogik (Komma) für das eigene QR-Format des Projekts, nicht die
  hier geforderte 7-Zeichen-Kürzung für den WA-Strichcode.

## Technischer Lösungsentwurf

1. `Index.cshtml`: `id="filterProductionOrder"` auf das bestehende Input-Feld setzen, daneben
   einen Scan-Button (`btnScanProductionOrder`, gleiches Markup/Icon wie die übrigen
   `scan-btn`-Buttons im Projekt) einfügen.
2. `barcode-scanner.js` einbinden (`~/js/barcode-scanner.js`, wie bereits auf
   `Inbound.cshtml`/`OutboundAll.cshtml`) plus die `html5-qrcode`-Bibliothek.
3. Neuer `valueExtractor`-Wert für `initTextInputScanner`, z. B. `'wa7'`, der nach dem Setzen des
   rohen Scan-Werts zusätzlich auf 7 Zeichen kürzt — entweder als eigener Zweig in
   `processScannedValue` (analog dem bestehenden `productionOrder`-Zweig) oder als
   `onScanned`-Callback von `initTextInputScanner`, der den Feldwert nachträglich auf
   `value.trim().substring(0, 7)` kürzt und ein `input`/`change`-Event nachfeuert (letzteres ist
   der invasivere-freie Weg, da er den bestehenden `processScannedValue`-Kern nicht anfassen muss).
   Empfehlung: **Callback-Variante**, weil sie ohne Änderung an der gemeinsam genutzten
   `processScannedValue`-Funktion auskommt und damit keine Nebenwirkung auf die anderen
   `productionOrder`-Scans (Tracking-Filter) hat.
4. Initialisierung im `Index.cshtml`-Script-Block:
   ```js
   initTextInputScanner('btnScanProductionOrder', 'filterProductionOrder', 'fa', function (value) {
       var input = document.getElementById('filterProductionOrder');
       if (input && input.value) {
           input.value = input.value.trim().substring(0, 7);
       }
   });
   ```
   (Exakte Trennzeichen-Behandlung — reines `substring(0,7)` vs. vorheriges Abtrennen an `-`/`_`
   — siehe offene Rückfrage 3.)
5. Der Scan setzt nur den Feldwert; das Absenden des Filters bleibt wie bisher über den
   „Filtern"-Button (kein automatisches Submit beim Scan, konsistent mit dem übrigen
   Server-Mode-Verhalten der Seite).

## Migrations-/SQL-Auswirkungen

Keine.

## Audit-Feld-Auswirkungen

Keine — reine Lese-/Filter-Funktion.

## Rollen- und Zugriffsfilter-Auswirkungen

Keine Änderung. Der Scan-Button erscheint innerhalb der bestehenden
`StockMovementsController.Index`-Seite unter `[RequireStockReadAccess]`.

## Listen-View-Pattern-Pflichten

Keine neuen Spalten/Filter-Keys — der bestehende `filterProductionOrder`-GET-Parameter und der
server-seitige `production-order`-Spaltenfilter bleiben unverändert; es wird nur ein
Eingabe-Komfort ergänzt.

## Akzeptanzkriterien

1. Auf der Bewegungshistorie-Seite existiert neben dem Feld „Fertigungsauftrag" ein Scan-Button.
2. Ein Scan mit Rohwert `2610063-1` füllt das Filterfeld mit `2610063`.
3. Ein Scan mit Rohwert `2610063_02` füllt das Filterfeld mit `2610063`.
4. Ein Scan mit Rohwert ohne Ergänzung (`2610063`) füllt das Filterfeld unverändert mit `2610063`.
5. Nach dem Scan ist das Filterfeld befüllt, aber die Liste wird erst nach Klick auf „Filtern"
   (bzw. Enter im Formular) neu geladen — kein automatisches Submit.

## Test-Szenarien

Neues Szenario in `docs/TESTSZENARIEN.md` Kapitel 2 (Lager):
- **Vorbedingung:** Bewegungshistorie mit Buchungen zu FA `2610063`.
- **Schritte:** Scan-Button neben „Fertigungsauftrag" klicken, Barcode mit Wert `2610063-1`
  scannen (bzw. Testbild verwenden), „Filtern" klicken.
- **Erwartetes Verhalten:** Filterfeld zeigt `2610063`, Liste zeigt alle Bewegungen zu FA
  `2610063`.
- **Negativfall:** Barcode-Wert kürzer als 7 Zeichen → Feld übernimmt den vollen (kürzeren) Wert
  unverändert (kein Fehler, keine Exception bei `substring`).

`secondbrain/tests/testszenarien-index.md` Kapitel 2 entsprechend ergänzen.

## Deploy

- **Web-App:** ja.
- **Service:** nein.
- **Migration:** nein.
- **Publish-Befehle:**

```
dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
```

## Offene Rückfragen

1. Ist die FA-/WA-Nummer an BEIDEN Standorten (AKE und IDEAL, vgl. laufende Spec
   [[2026-07-28-ideal-anpassungen-neu-nachbilden-spec]]) verlässlich genau 7-stellig? Falls die
   IDEAL-Linie eine andere Länge verwendet, wäre ein hartes `substring(0,7)` dort falsch.
2. Soll die Kürzung ausschließlich beim Scannen greifen (Client-JS, wie hier vorgeschlagen), oder
   auch beim manuellen Eintippen/Einfügen in das Fertigungsauftrag-Filterfeld der
   Bewegungshistorie?
3. Reicht ein einfaches `substring(0,7)` auf den rohen Scan-Wert, oder soll vorher gezielt an
   „-" bzw. „_" getrennt werden (robuster, falls die Ergänzung nicht exakt ab Position 8
   beginnt)?

## Freigabe-Antworten (Mensch füllt aus — Schranke 1)

1. →
2. →
3. →
