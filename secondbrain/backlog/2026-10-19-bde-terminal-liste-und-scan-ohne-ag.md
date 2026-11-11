---
typ: feature
---
# BDE-Terminal: Arbeitsgang-Liste übersichtlich, Scan ohne AG, Prio, Kleinpunkte aus v1.63.0

**Vorgemerkt 2026-10-19** aus dem Test von v1.63.0 an IDEAL (Werkbank 65 „Entgraten Fläche“, FA 1059463). Umsetzung
**im Bündel vor dem Merge** (eigene Spec, v1.64.0), weil das Terminal bei IDEAL sonst kaum bedienbar ist.
Vorgänger: [[2026-10-19-bde-stufe-1-werkbaenke-ag-fertigmeldung-spec]], Lastenheft-Abgleich [[2026-10-19-bde-lastenheft-abgleich]].

## Befund (Gerald 2026-10-19, am Code belegt)

- Klick auf einen AG wählt ihn aus, markiert ihn aber nicht; die Knöpfe „Rüsten/Produktion starten“ landen in
  `#actionPanel` unter BEIDEN Spalten (`Views/BdeTerminal/Index.cshtml:108`, `wwwroot/js/bde-terminal.js:298-301`, `:546-549`).
  Bei vielen AGs ganz unten, außerhalb des Sichtbereichs. Gerald bestätigt: „war ganz unten“.
- Normal-Modus verlangt im Scan `FA,AG` (`bde-terminal.js:178-181`), obwohl `resolve-scan` die AG-Nummer schon optional
  nimmt (`BdeApiController.cs:85`, `BdeScanResolver.cs:182-183`) und werkbank-/familien-gescopt auflöst (Exact/Ambiguous/NotInScope).
  Sub-FA-Nummer wird per Fallback `SubOrderNumber` gefunden (`BdeScanResolver.cs:103-112`).

## Wünsche / Entscheidungen Gerald 2026-10-19

1. Liste nach **Haupt-FA**, max. **5 Haupt-FAs** angezeigt (Rest über „weitere anzeigen“ o. ä.).
2. **Klick auf den Haupt-FA öffnet die zugehörigen Sub-FAs**, gefiltert auf die Arbeitsgänge dieser Werkbank (Kürzel-Familie).
3. Gewählter AG sichtbar markiert, Aktionsknöpfe direkt bei der Liste.
4. **Barcode ohne AG** („EG möchte ich nicht im Barcode“): AG ergibt sich aus der Werkbank. Sage druckt nach Geralds
   Erinnerung **nur den Haupt-FA** (ungeprüft) → Scan Haupt-FA öffnet dieselbe Sub-FA-Auswahl wie der Klick; Scan einer
   Sub-FA-Nummer (eigene Etiketten) wählt direkt. `FA,AG` bleibt gültig.
5. Mit aufnehmen (Koordination empfohlen, Gerald „gleich mit einbauen“):
   - Lastenheft Nr. 11 Priorität: `FaHierarchyOrderInfo.Prio` (`Models/FaHierarchyOrderInfo.cs:39`) am Terminal anzeigen
     und mitsortieren (Prio, dann Fertigungstermin). Ungeprüft: FAInfos liefert zuletzt nur 3 Haupt-FAs → Prio oft leer.
   - Server weist negative Mengen an allen BDE-Mengeneingängen ab (Befund v1.63.0).
   - FA-Baum: Werkbank-Name am Symbol auch bei Kürzel an mehreren Werkbänken (Befund v1.63.0).
   - Hilfeseite „Andere Listen“: BDE-Buchungen filtern seit v1.58.1 serverseitig (`Views/Help/Index.cshtml:245`,
     aus [[2026-10-14-buendel-fixlauf-vor-merge]]).

## Nicht hier

- Zwei gleichzeitige Fertigmeldungen desselben AG (braucht Schema) → eigene Notiz.
- Lastenheft Stufe 2 (Nr. 9, 2, 17, 7) und Grob-/Feinplanung.
- Hardware-Scanner-Präfix: [[2026-10-15-scan-handler-hardware-scanner-spec]] (danach; übernimmt das Format ohne AG).
