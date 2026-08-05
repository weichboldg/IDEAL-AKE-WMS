---
type: spec
title: "Bewegungshistorie: Scan-Button fuer WA-Strichcode mit Trennzeichen-Kuerzung"
slug: 2026-08-05-wms-bugs-improvements-teil-3-spec
status: Testbereit
created: 2026-08-05
updated: 2026-08-05
source_backlog: "[[2026-08-05-WmsBugs&Improvements]]"
depends_on: ""
task: "[[2026-08-05-deploy-wms-bugs-teil-1-2-3]]"
worktree: ".claude/worktrees/2026-08-05-wms-bugs-improvements-teil-1-2-3"
branch: "feature/2026-08-05-wms-bugs-improvements-teil-1-2-3"
affected_code:
  - IdealAkeWms/Views/StockMovements/Index.cshtml
  - IdealAkeWms/wwwroot/js/barcode-scanner.js
  - docs/TESTSZENARIEN.md
  - secondbrain/tests/testszenarien-index.md
open_questions: []
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
nur die eigentliche WA-Basisnummer für die Filterung verwendet werden. Der gedruckte
Strichcode trägt gelegentlich eine zusätzliche Ergänzung nach einem „-" oder „_" (z. B. eine
fortlaufende Zahl für Teillieferungen/Positionen), die für die Filterung irrelevant ist und den
Treffer sonst verhindert oder verfälscht.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Ein Scan-Button für das Filterfeld „Fertigungsauftrag" auf der Bewegungshistorie-Seite
  (`/StockMovements/Index`) — existiert dort aktuell **nicht** (siehe Ist-Zustand).
- Client-seitige Kürzung des gescannten Werts auf die WA-Basisnummer (alles ab dem ersten
  Trennzeichen `-` oder `_` wird abgeschnitten), bevor er in das Filterfeld übernommen wird.

**Out-of-Scope**
- Keine Änderung an der server-seitigen Filterlogik
  (`StockMovementRepository.GetMovementHistoryAsync`, `ApplyMovementColumnFilter`) — die
  `Contains`-Filterung bleibt unverändert, nur der **gescannte Eingabewert** wird vor dem
  Absenden gekürzt (ausschließlich beim Scan; manuelle Eingabe bleibt unangetastet).
- Keine Änderung an anderen Scan-Stellen im Projekt (Einbuchung/Ausbuchung/Umbuchung,
  Tracking-Teileverfolgung) — dort gilt weiterhin die bestehende Komma-Suffix-Logik
  (`.split(',')[0]`, Fallstrick „QR-Code Komma-Suffix").
- Keine Änderung am server-seitigen Spaltenfilter `production-order` (Server-Mode-Filter über
  `?colf_production-order=`) — nur der klassische GET-Filter `filterProductionOrder` (Textfeld im
  Filter-Formular) bekommt den Scan-Button.

## Fachliche Anforderungen

1. Ein Scan-Button neben dem Textfeld „Fertigungsauftrag" im Filter-Formular der
   Bewegungshistorie.
2. Nach dem Scan wird der erkannte Wert auf die WA-Basisnummer gekürzt und in das Filterfeld
   übernommen: alles ab dem **ersten** Trennzeichen `-` oder `_` (inklusive) wird abgeschnitten.
   Enthält der Wert kein Trennzeichen, wird er unverändert durchgereicht (kein fester
   Längen-Cap, insbesondere **kein** `substring(0,7)`).
3. Beispiel: Scan liefert `2610063-1` oder `2610063_02` → Filterfeld erhält `2610063`.
4. Die Kürzung greift **ausschließlich beim Scannen**. Manuelles Eintippen oder Einfügen in das
   Filterfeld bleibt vollständig unverändert.

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
   `Inbound.cshtml`/`OutboundAll.cshtml`) plus die `html5-qrcode`-Bibliothek. Die Bibliothek ist
   **lokal** einzubinden (`~/lib/html5-qrcode/html5-qrcode.min.js`, exakt wie
   `Views/Tracking/OseonIndex.cshtml:233`) — **nicht** per `unpkg.com`-CDN (wie in
   `StockOverview/Index.cshtml:159`), weil das CDN im Produktions-Intranet nicht verlässlich
   erreichbar ist.
3. Die Kürzung lebt ausschließlich im `onScanned`-Callback von `initTextInputScanner`, **nicht**
   in `processScannedValue` und **nicht** in der Filter-Logik. Der bewährte `valueExtractor:'fa'`
   (mappt intern auf `scanType:'productionOrder'`) bleibt unangetastet; der Callback kürzt den
   Feldwert danach trennzeichen-basiert und feuert ein `input`-Event nach. Diese Variante ändert
   die gemeinsam genutzte `processScannedValue`-Funktion nicht und hat damit keine Nebenwirkung
   auf die anderen `productionOrder`-Scans (Tracking-Filter). Weil der Callback nur beim Scan
   feuert, bleibt manuelles Eintippen/Einfügen unberührt (siehe Anforderung 4).
4. Initialisierung im `Index.cshtml`-Script-Block (trennzeichen-basiert, kein Längen-Cap):
   ```js
   initTextInputScanner('btnScanProductionOrder', 'filterProductionOrder', 'fa', function () {
       var input = document.getElementById('filterProductionOrder');
       if (input && input.value) {
           input.value = input.value.trim().split(/[-_]/)[0];
           input.dispatchEvent(new Event('input', { bubbles: true }));
       }
   });
   ```
   `split(/[-_]/)[0]` schneidet alles ab dem ersten `-` oder `_` ab; ohne Trennzeichen bleibt der
   Wert unverändert. Damit ist die Kürzung immun gegen Längen-Annahmen (auch eine künftige
   8-stellige IDEAL-Nummer ohne Trennzeichen wird nicht zerstört).
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
2. Ein Scan mit Rohwert `2610063-1` füllt das Filterfeld mit `2610063` (Trennzeichen `-`).
3. Ein Scan mit Rohwert `2610063_02` füllt das Filterfeld mit `2610063` (Trennzeichen `_`).
4. Ein Scan mit Rohwert ohne Ergänzung (`2610063`) füllt das Filterfeld unverändert mit
   `2610063` (kein Trennzeichen → unverändert durchgereicht).
5. **Mehrere Trennzeichen:** Ein Scan mit Rohwert `2610063-1-2` füllt das Filterfeld mit
   `2610063` (nur bis zum **ersten** Trennzeichen).
6. **Nicht-numerischer Suffix:** Ein Scan mit Rohwert `2610063-A` füllt das Filterfeld mit
   `2610063` (die Regel wertet nur das Trennzeichen, nicht die Suffix-Art).
7. **Längeres/kürzeres Basisteil ohne Trennzeichen:** Ein Scan mit Rohwert `26100631` (8-stellig,
   ohne Trennzeichen) füllt das Filterfeld unverändert mit `26100631` — es findet **keine**
   Längenkürzung statt. Ebenso wird ein Wert kürzer als 7 Zeichen ohne Trennzeichen (z. B.
   `26100`) unverändert übernommen (keine Exception).
8. **Nur beim Scannen:** Manuelles Eintippen oder Einfügen von `2610063-1` in das Filterfeld
   lässt den Wert unverändert (`2610063-1`) — die Kürzung greift ausschließlich über den
   Scan-Callback.
9. Nach dem Scan ist das Filterfeld befüllt, aber die Liste wird erst nach Klick auf „Filtern"
   (bzw. Enter im Formular) neu geladen — kein automatisches Submit.

## Test-Szenarien

Neues Szenario in `docs/TESTSZENARIEN.md` Kapitel 2 (Lager):
- **Vorbedingung:** Bewegungshistorie mit Buchungen zu FA `2610063`.
- **Schritte:** Scan-Button neben „Fertigungsauftrag" klicken, Barcode mit Wert `2610063-1`
  scannen (bzw. Testbild verwenden), „Filtern" klicken.
- **Erwartetes Verhalten:** Filterfeld zeigt `2610063`, Liste zeigt alle Bewegungen zu FA
  `2610063`.
- **Randfälle (jeweils testbar):** `2610063_02` → `2610063`; `2610063-1-2` → `2610063` (nur bis
  erstem Trennzeichen); `2610063-A` (nicht-numerischer Suffix) → `2610063`; `2610063` (kein
  Suffix) → unverändert; `26100631` (8-stellig, kein Trennzeichen) → **unverändert** (keine
  Längenkürzung).
- **Negativfall 1:** Barcode-Wert ohne Trennzeichen (auch kürzer als 7 Zeichen, z. B. `26100`) →
  Feld übernimmt den vollen Wert unverändert (kein Fehler, keine Exception). Mit der
  trennzeichen-basierten Regel entfällt jede Längen-Sonderbehandlung.
- **Negativfall 2 (nur beim Scannen):** Manuelles Eintippen/Einfügen von `2610063-1` → Feld
  behält `2610063-1` (Kürzung greift nicht bei manueller Eingabe).

`secondbrain/tests/testszenarien-index.md` Kapitel 2 entsprechend ergänzen.

## Deploy

**Finalisiert durch QA (2026-08-05) anhand des echten Diffs im gemeinsamen Worktree
`.claude/worktrees/2026-08-05-wms-bugs-improvements-teil-1-2-3` (Branch
`feature/2026-08-05-wms-bugs-improvements-teil-1-2-3`, geteilt mit Teil 1 + Teil 2).**

- **Web-App:** ja (`Views/StockMovements/Index.cshtml` — Scan-Button, lokale
  `html5-qrcode.min.js`-Einbindung, `barcode-scanner.js`-Einbindung + Kürzungs-Callback — Teil des
  gemeinsamen Diffs). `wwwroot/js/barcode-scanner.js` selbst wurde **nicht** verändert, die Kürzung
  lebt ausschließlich im `onScanned`-Callback in `Index.cshtml`.
- **Service:** nein (nur `IDEALAKEWMSService/AppVersion.cs` Versions-Bump auf 1.29.0, keine
  funktionale Service-Änderung).
- **Migration:** nein — `git diff main --stat` gegen den Worktree bestätigt: keine Datei unter
  `*/Migrations/` oder `SQL/` verändert.
- **Publish-Befehle (aus dem Worktree, VOR dem Merge):**

```
dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
```

Fluss: Publish aus dem Worktree → Testsystem → manueller Test (Schranke 2) → danach Merge. Der
Worktree ist mit Teil 1 und Teil 2 geteilt (ein gemeinsamer Branch) — ein einziger Publish deckt
alle drei Teile ab. **Hinweis:** Nach dem Merge nur dann erneut aus `main` publishen, wenn der
Merge tatsächlich getestete Dateien mit parallelen `main`-Änderungen zusammengeführt hat.

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

1. →ja
2. →ja, nur beim scannen
3. →bitte robuster

## Kritische Pruefung (2026-08-05)

Rolle: Anwalt des Teufels. Geprueft gegen echten Code (`barcode-scanner.js`,
`Views/StockMovements/Index.cshtml`, `StockMovementRepository.GetMovementHistoryAsync`,
Referenzen `OseonIndex.cshtml` / `StockOverview/Index.cshtml`) und die drei Freigabe-Antworten.

### BLOCKER

- **B1 — Antwort 3 („bitte robuster") widerspricht der eigenen Loesungsskizze (`substring(0,7)`)
  und dem Spec-Titel („7-stelliger Truncation").** Das sind ZWEI verschiedene Algorithmen:
  - *fixe 7 Zeichen* (`value.substring(0,7)`) — bricht jede Basis-WA, die laenger als 7 Zeichen
    ist und KEIN Trennzeichen hat (z. B. eine kuenftige 8-stellige IDEAL-Nummer `26100631` →
    faelschlich `2610063`).
  - *trennzeichen-basiert* (alles ab erstem `-`/`_` abschneiden, sonst Rohwert unveraendert) —
    liefert bei allen Beispielen dasselbe Ergebnis, ist aber gegen Laengen-Annahmen immun.

  Antwort 1 („verlaesslich 7-stellig") und Antwort 3 („robuster") zeigen damit in
  entgegengesetzte Richtungen: wer 7-stellig garantiert, braucht kein „robuster"; wer „robuster"
  will, darf sich nicht auf Position 7 verlassen. Der Umsetzer kann so nicht eindeutig
  implementieren.
  **Frage an den Menschen:** Bitte den exakten Algorithmus bestaetigen:
  „Schneide alles ab dem ersten `-` oder `_` ab (`value.trim().split(/[-_]/)[0]`); ist kein
  `-`/`_` vorhanden, bleibt der Rohwert unveraendert — KEIN zusaetzlicher harter 7-Zeichen-Cap."
  Ja/Nein? Falls ein 7-Zeichen-Cap zusaetzlich gewuenscht ist, macht er die Robustheit aus
  Antwort 3 wieder zunichte — das bitte explizit entscheiden.
Antwort: korrekt
### SOLLTE

- **S1 — Spec-Text an Antwort 3 nachziehen (aktuell inkonsistent).** Nach Klaerung von B1 muessen
  Fachliche Anforderung 2, Loesungsentwurf Schritt 4 (Code-Snippet) und die Akzeptanzkriterien
  von „ersten 7 Zeichen"/`substring(0,7)` auf die Trennzeichen-Regel umgeschrieben werden.
  Konkreter Callback-Vorschlag (ersetzt das Snippet in Schritt 4):
  ```js
  initTextInputScanner('btnScanProductionOrder', 'filterProductionOrder', 'fa', function () {
      var input = document.getElementById('filterProductionOrder');
      if (input && input.value) {
          input.value = input.value.trim().split(/[-_]/)[0];
          input.dispatchEvent(new Event('input', { bubbles: true }));
      }
  });
  ```
- **S2 — html5-qrcode-Quelle festlegen: lokal, nicht CDN.** Die Spec sagt nur „plus die
  html5-qrcode-Bibliothek". Im Code existieren zwei Muster: `OseonIndex.cshtml:233` laedt lokal
  (`~/lib/html5-qrcode/html5-qrcode.min.js`), `StockOverview/Index.cshtml:159` laedt vom
  `unpkg.com`-CDN. Im Produktions-Intranet ist das CDN ggf. nicht erreichbar. Vorschlag: die
  **lokale** Variante wie OseonIndex verwenden und das in Schritt 2 fixieren.
- **S3 — Akzeptanzkriterien/Testszenarien um die Robust-Randfaelle erweitern.** Aktuell nur drei
  Happy-Beispiele. Nach B1 ergaenzen: mehrere Trennzeichen (`2610063-1-2` → `2610063`),
  `_`-Suffix, nicht-numerischer Suffix (`2610063-A` → `2610063`), gar kein Suffix (unveraendert),
  und — bei trennzeichen-basiert — den Negativfall neu formulieren: „Wert kuerzer als 7 ohne
  Trennzeichen" wird jetzt NICHT mehr gekuerzt (der bisherige `substring`-Negativfall ist mit der
  neuen Regel gegenstandslos).

### HINWEIS

- **H1 — Scan-Feedback zeigt den UNgekuerzten Wert.** `showScanFeedback` (barcode-scanner.js:304)
  laeuft im `_origProcessScannedValue` VOR dem `onScanned`-Callback und zeigt daher
  „Gescannt: 2610063-1", waehrend das Feld anschliessend auf `2610063` gekuerzt wird. Kosmetisch;
  optional im Callback das Feedback neu rendern.
- **H2 — Antwort 2 („nur beim scannen") ist im Code sauber trennbar — bestaetigt.** Die Kuerzung
  lebt ausschliesslich im `onScanned`-Callback von `initTextInputScanner`, der nur beim Scan
  feuert. Manuelles Eintippen/Einfuegen in `filterProductionOrder` bleibt voellig unberuehrt.
  Scan-Handler und Filter-Logik sind getrennt — Antwort 2 ist umsetzbar wie formuliert.
- **H3 — Inkonsistenz zu StockOverview (Folgearbeit).** `StockOverview/Index.cshtml:165` hat
  bereits einen Scan-Button auf einem `filterProductionOrder`-Feld
  (`initScanner('btnScanPO', 'filterProductionOrder', 'productionOrder')`) OHNE 7-/Trennzeichen-
  Kuerzung. Derselbe WA-Strichcode wird dort also nicht gekuerzt. Laut Backlog nur
  Bewegungshistorie in Scope — aber der Anwender koennte dieselbe Bequemlichkeit im
  Lagerbestand-FA-Filter erwarten. Als Aufgabe/Folge notieren.
- **H4 — Server-Filter ist `Contains` — Kuerzung ist noetig, nicht optional.**
  `StockMovementRepository.cs:293` filtert `sm.ProductionOrder.Contains(filterProductionOrder)`.
  Der gespeicherte `ProductionOrder` traegt keinen `-/_`-Suffix; ein `Contains("2610063-1")`
  liefert daher KEINEN Treffer. Die Kuerzung ist also fachlich zwingend (bestaetigt die
  „verhindert Treffer"-Begruendung im Ziel).
- **H5 — Keine Kollision/Interaktion mit teil-1.** teil-1 fasst nur
  `StockMovementRepository.cs` / `IStockMovementRepository.cs` an (Methode
  `GetStockByProductionOrderAsync`), nicht `GetMovementHistoryAsync`, nicht die View, nicht
  `barcode-scanner.js`. Kein Datei-Overlap, kein Merge-Konflikt, kein Verhaltens-Einfluss. Der
  in Frage 6 vermutete `Contains`-Teilstring-Effekt betrifft eine andere Methode/andere Views —
  hier irrelevant.
- **H6 — Sage v1.28.0 (Cross-Cutting) irrelevant — bestaetigt.** Der Sage-Decorator haengt am
  Schreibpfad `IStockMovementRepository.AddAsync`; teil-3 ist reiner Lese-/Filter-/Scan-Pfad und
  ruft `AddAsync` nie. Die neuen `MovementType.Sage*`-Eintraege stehen zwar im Bewegungsart-Filter
  der Index-View (Zeilen 42-43), haben mit dem Scan-Button aber nichts zu tun.
- **H7 — `'fa'`-Extractor durchlaeuft die `;`/Komma-QR-Logik.** `valueExtractor:'fa'` mappt auf
  `scanType:'productionOrder'`, das in `processScannedValue` (Zeilen 277-284) `;`-Split +
  `,`-Suffix-Kuerzung macht. Fuer einen reinen 1D-WA-Strichcode (`2610063-1`, kein `;`/`,`) ist
  das ein harmloser No-op; erst der Callback kuerzt. Akzeptabel — nur bewusst so lassen.
- **H8 — Annahme dokumentieren:** Basis-WA-Nummern enthalten selbst nie `-`/`_`. Alle Beispiele
  sind rein numerisch, daher sicher; sollte je eine legitime FA einen Bindestrich enthalten,
  wuerde die Trennzeichen-Regel sie zerschneiden.
- **Deploy/Groesse:** web-only, keine Migration, kein Service — plausibel; Publish-Befehl korrekt.

NACHBESSERUNG NOETIG: B1 (Algorithmus fixe-7 vs. trennzeichen-basiert) klaeren, dann S1/S2/S3
(Spec-Text, html5-qrcode-Quelle, Randfall-Kriterien) nachziehen.

## Finalisierung (2026-08-05)

Menschliche Freigabe-Antworten: 1. ja; 2. ja, nur beim Scannen; 3. bitte robuster. Auf dieser
Basis wurden die offenen Punkte aus der Kritischen Pruefung wie folgt aufgeloest — ohne weitere
Rueckfrage:

- **B1 (Algorithmus) ENTSCHIEDEN = trennzeichen-basiert.** Antwort 3 („robuster") ist gegenueber
  Antwort 1 („7-stellig") massgeblich: der Umsetzer schneidet den gescannten Wert am **ersten**
  `-` oder `_` ab (`value.trim().split(/[-_]/)[0]`); ohne Trennzeichen bleibt der Rohwert
  **unveraendert**. **Kein** `substring(0,7)`, **kein** Laengen-Cap. Damit wird eine >7-stellige
  Nummer ohne Trennzeichen (z. B. eine kuenftige 8-stellige IDEAL-Nummer) nicht zerstoert. Titel,
  Fachliche Anforderung 2 und Loesungsentwurf Schritt 3+4 wurden vom „ersten 7 Stellen"-Wortlaut
  auf „Suffix ab erstem Trennzeichen entfernen" umgeschrieben.
- **Antwort 2 („nur beim Scannen") festgeschrieben.** Neue Fachliche Anforderung 4 und
  Loesungsentwurf Schritt 3 halten fest: die Kuerzung lebt ausschliesslich im `onScanned`-Callback
  von `initTextInputScanner` (Scan-Pfad), **nicht** in `processScannedValue` und **nicht** in der
  Filter-Logik. Manuelle Eingabe bleibt unangetastet (Akzeptanzkriterium 8, Negativfall 2).
- **S1 (Spec-Text nachgezogen).** Ziel, Umfang (In-Scope), Fachliche Anforderungen, Loesungsentwurf
  und Akzeptanzkriterien sind konsistent trennzeichen-basiert; das Code-Snippet in Schritt 4 nutzt
  jetzt `split(/[-_]/)[0]` + `dispatchEvent('input')`.
- **S2 (html5-qrcode LOKAL).** Loesungsentwurf Schritt 2 fixiert die lokale Einbindung
  (`~/lib/html5-qrcode/html5-qrcode.min.js`, wie `OseonIndex.cshtml`), ausdruecklich **nicht** per
  `unpkg.com`-CDN — Begruendung Produktions-Intranet.
- **S3 (Randfall-Kriterien ergaenzt).** Akzeptanzkriterien um mehrere Trennzeichen (`2610063-1-2`),
  nicht-numerischen Suffix (`2610063-A`), kein Suffix (unveraendert), 8-stellig ohne Trennzeichen
  (unveraendert) und „nur beim Scannen" erweitert. Test-Szenarien mit denselben Randfaellen und
  neu formulierten Negativfaellen aktualisiert (der alte `substring`-Negativfall ist
  gegenstandslos).
- **open_questions (Frontmatter)** auf `[]` getrimmt — alle drei Fragen sind durch die
  Freigabe-Antworten und B1 aufgeloest.

Nicht veraendert (bewusst): `status: Entwurf`, Dateiname/Ablageort, der Block
„## Freigabe-Antworten", Anwendungscode. HINWEIS-Punkte H1–H8 aus der Kritischen Pruefung bleiben
als Umsetzungs-/Folgehinweise stehen (u. a. H3 StockOverview-Konsistenz als Folgearbeit, H1
Scan-Feedback kosmetisch).

BEREIT ZUR FREIGABE

## QA-Nachweis (2026-08-05)

Verifikation im Worktree `.claude/worktrees/2026-08-05-wms-bugs-improvements-teil-1-2-3`
(gemeinsamer Branch mit Teil 1 + Teil 2, HEAD `a1573d4`).

**Build:**
```
dotnet build IdealAkeWms.slnx
...
Der Buildvorgang wurde erfolgreich ausgeführt.
    9 Warnung(en)
    0 Fehler
```
(Warnungen vorbestehend — NU1902 MailKit/MimeKit-Advisories, CS8602 in `TrackingController.cs`.)

**Tests — Web (`IdealAkeWms.Tests`):**
```
Bestanden! : Fehler: 0, erfolgreich: 1074, übersprungen: 1, gesamt: 1075
```
**Kein automatisierter Test für diesen Teil möglich:** Die Änderung ist reines Client-JS
(`onScanned`-Callback in `Views/StockMovements/Index.cshtml`, trennzeichen-basierte Kürzung
`split(/[-_]/)[0]`) + Markup. Das Projekt hat keine JS-Test-Infrastruktur (kein Jest/Karma o. ä.,
verifiziert: keine `*.test.js`/`jest.config*` im Repo) — Manual-UAT ist hier der **einzige**
Nachweis (vgl. CLAUDE.md „Tests, EF, SQL Server"-Fallstrick zu nicht InMemory-testbaren Teilen).

**Diff-Verifikation (Code-Review-Ersatz, da kein Unit-Test greift):**
- `git diff main -- IdealAkeWms/Views/StockMovements/Index.cshtml` zeigt exakt den in der Spec
  vorgegebenen Callback (`value.trim().split(/[-_]/)[0]` + `dispatchEvent('input')`) — deckt sich
  mit AK2–AK7 (Trennzeichen `-`/`_`, mehrere Trennzeichen, nicht-numerischer Suffix, kein Suffix,
  keine Längenkürzung).
- `~/lib/html5-qrcode/html5-qrcode.min.js` lokal vorhanden (`IdealAkeWms/wwwroot/lib/html5-qrcode/`)
  — bestätigt S2 (lokale Einbindung statt unpkg-CDN).
- `wwwroot/js/barcode-scanner.js` selbst **unverändert** (`git diff` liefert kein Ergebnis für diese
  Datei) — bestätigt, dass die Kürzung ausschließlich im Callback lebt und andere
  `productionOrder`-Scans (Tracking-Filter) nicht beeinflusst (H5/H7 der Kritischen Prüfung).

**Tests — Service (`IDEALAKEWMSService.Tests`):**
```
Bestanden! : Fehler: 0, erfolgreich: 195, übersprungen: 0, gesamt: 195
```

**Diff-Nachweis (kein Migrations-/SQL-Impact):**
```
git diff main --stat -- '*/Migrations/*' 'SQL/*'   → leer
```

**CLAUDE.md-Checkliste (dieser Teil):** Migration/SQL — n/a. Audit-Felder — n/a (reine
Lese-/Filter-Funktion). Versions-Bump auf 1.29.0 + Anwender-Changelog — geteilt mit Teil 1/2, ein
gemeinsamer Release. `docs/TESTSZENARIEN.md` um TS-2.25 ergänzt (inkl. aller Randfälle:
`_`-Trennzeichen, mehrere Trennzeichen, nicht-numerischer Suffix, kein Suffix, 8-stellig ohne
Trennzeichen, manuelle Eingabe unverändert), `secondbrain/tests/testszenarien-index.md`
(Hauptcheckout) Kapitel 2 nachgezogen.

Ergebnis: **Build 0 Fehler, alle Tests grün (keine neuen Unit-Tests möglich/nötig für dieses
reine Client-JS-Feature) — Manual-UAT ist hier zwingend, nicht optional.**

## Manuelle Test-Checkliste (Schranke 2)

Referenz: `docs/TESTSZENARIEN.md` **TS-2.25 — WA-Scan-Button in der Bewegungshistorie kuerzt
Suffix (v1.29.0, Teil 3)**. Dieser Teil hat **keinen** automatisierten Test — die folgenden
Schritte sind der einzige Nachweis.

1. Bewegungshistorie öffnen (`/StockMovements`). Prüfen: neben dem Filterfeld „Fertigungsauftrag"
   erscheint ein Scan-Button.
2. Scan-Button klicken, Testbild/Barcode mit Wert `2610063-1` scannen. **Erwartet:** Filterfeld
   zeigt `2610063` (nicht `2610063-1`), Liste lädt noch **nicht** automatisch neu.
3. „Filtern" klicken. **Erwartet:** Liste zeigt alle Bewegungen zu FA `2610063`.
4. Randfälle jeweils gegenprüfen (gescannt → erwarteter Filterwert):
   `2610063_02` → `2610063`; `2610063-1-2` → `2610063` (nur erstes Trennzeichen);
   `2610063-A` → `2610063`; `2610063` (kein Suffix) → `2610063` unverändert;
   `26100631` (8-stellig, kein Trennzeichen) → `26100631` unverändert (keine Längenkürzung);
   `26100` (kürzer als 7, kein Trennzeichen) → `26100` unverändert (kein Fehler).
5. Negativfall: `2610063-1` **manuell eintippen** (nicht scannen). **Erwartet:** Feld behält
   `2610063-1` — Kürzung greift nur beim Scan.
6. `html5-qrcode` lädt sichtbar ohne Konsolenfehler (lokale Datei, kein CDN-Ladeversuch) — im
   Produktions-Intranet ohne Internetzugriff testen, falls möglich (Kernbegründung für die lokale
   Einbindung).
7. Regressionscheck: Bestehender Scan-Pfad in der Bestandsübersicht
   (`StockOverview/Index.cshtml`, `btnScanPO`) weiterhin unverändert nutzbar (dort **ohne**
   Kürzung, wie bisher — bewusst außerhalb des Scopes, siehe H3 der Kritischen Prüfung).
