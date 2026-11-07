---
typ: bug
---
# Stückliste: Fehler im Einstellungsfenster (Zahnrad) und fehlende Spalte Matchcode

**Aufgenommen 2026-10-06** (Meldung Gerald, Koordination).

## Meldung (Gerald)

- Bug in der Anzeige der Stückliste im Einstellungsfenster (Zahnrad).
- Prüfen, ob die Spalte Matchcode vorhanden ist; falls nicht, soll sie angezeigt werden.

## Symptom des Zahnrad-Fehlers (Gerald 2026-10-06, Screenshot IDEAL, FA 1035235)

**Das Zahnrad ist in der Stückliste gar nicht sichtbar** – die Spaltenauswahl lässt sich nicht öffnen.

## Ursache (am Code belegt, Bündel-Worktree, v1.47.2)

- `column-preferences.js:367-389` (`insertGearButton`) hängt das Zahnrad an das Element **direkt vor**
  `.table-responsive` (`previousElementSibling`), sofern es ein `DIV` ist.
- `Views/Picking/Bom.cshtml:127-131` legt dafür einen leeren Toolbar-Host an – mit dem Kommentar, dass er
  **direkt** vor `.table-responsive` stehen MUSS.
- Mit [[2026-09-18-stueckliste-kommissionierziel-filter-spec]] (v1.42) kamen **dazwischen** zwei weitere DIVs
  hinzu: `#bomDefaultFilterBadges` (Z. 135) und `#bomKzEmptyState` (Z. 136, Attribut `hidden`).
  `.table-responsive` steht jetzt in Z. 141; ihr direkter Vorgänger ist `#bomKzEmptyState`.
- Folge: Das Zahnrad landet **im versteckten Leerzustand-Hinweis**. Sichtbar würde es nur, wenn der gelbe
  Hinweis „Alle Ziele ohne Treffer“ erscheint – dann mitten in diesem Hinweis.
- `main` (AKE) ist **nicht** betroffen: dort steht der Host direkt vor der Tabelle
  (`IdealAkeWms/Views/Picking/Bom.cshtml:117-123`, ohne die beiden neuen DIVs).
- Fehlerklasse: eine Platzierungsregel, die nur im Kommentar steht und von einer späteren Änderung
  übersehen wurde. Gehört vor den Merge des Bündels behoben (Regression aus v1.42).

## Matchcode (am Code belegt, Bündel-Worktree)

- `Views/Picking/Bom.cshtml` hat **keine** Matchcode-Spalte. Vorhandene `data-col-key`: `position`,
  `assembly-group`, `resource-number`, `description1`, `description2`, `quantity`, `procurement`,
  `article-group`, `category`, `kommissionieren`, `hauptlagerplatz`, `sage-position`, `ebene`,
  `vater-sub-fa`, `order`, `pick-control`, `source-location`, `storage-location`, `warehouse-order`.
  `viewKey` der Spaltenauswahl: `Bom` (Bom.cshtml:656).
- Der Matchcode existiert am Artikel (`Article.Matchcode`, [[2026-09-13-matchcode-artikelstamm-spec]])
  und am Strukturknoten (`FaHierarchyNode.Matchcode`). Welche Quelle für die Stückliste gilt, klärt die
  Spec (die Stückliste kommt aus dem BOM-Cache/der Bridge, nicht aus `FaHierarchyNode`).

## Für die Spec zu klären

1. Matchcode-Spalte in allen Stücklisten-Varianten, im Druck (`PrintBom`) und in der Spaltenauswahl
   (`ColumnDefinitions` für `Bom`, [[fallstricke]] §3); als Spaltenfilter durchsuchbar.
2. Zahnrad-Fehler erst nach Symptom-Beschreibung spezifizieren.

## Hinweis

`Bom.cshtml` ist im Bündel stark verändert (hierarchische Stückliste). Umsetzung im Bündel-Stand bzw.
nach Schranke 2, nicht auf `main`.

## Nachtrag 2026-10-16 (Gerald, Screenshot IDEAL-Test, Picking/Bom/131, FA 1059463, 263 Positionen)

1. **Matchcode fehlt in der Stückliste weiterhin – bitte hinzufügen.** Das Zahnrad ist im Screenshot sichtbar
   (rechts über der Tabelle), der Zahnrad-Teil dieser Notiz ist auf dem Testsystem also offenbar behoben – im Spec-Lauf
   am Code bestätigen.
   - Vorschlag Koordination: Quelle Artikelstamm über das vorhandene `Services/MatchcodeLookup.cs` (ein Batch-Lookup,
     Muster `PickingController.cs:150-167`), damit AKE und IDEAL gleich funktionieren; Spalte nach „Bezeichnung 2“,
     filterbar, in Spaltenauswahl (`data-view-key="Bom"`, BOM-Bridge-Liste `Bom.cshtml:672-686`), CSV und Druck (`PrintBom`).
2. **Viele Lagerplätze -> Probleme mit der Scrollbar.** Die Spalte „Lagerplatz“ listet alle Bestandsplätze in einer Zeile
   (`Bom.cshtml` Zelle `string.Join(", ", item.StockLocations…)`), und `site.css:250-253` erzwingt für alle Zellen in
   `.table-responsive` `white-space: nowrap !important`. Eine Zeile mit vielen Plätzen macht die ganze Tabelle sehr breit;
   die horizontale Scrollbar sitzt unter 263 Zeilen am Tabellenende.
   - Vorschlag Koordination: nur diese Zelle darf umbrechen (eigene Klasse mit `white-space: normal` und Max-Breite,
     z. B. 28rem), Hauptlagerplatz zuerst; kein Tooltip-Kürzen, weil am Touch-Gerät nicht bedienbar. Prüfen, ob dieselbe
     Liste in `PrintBom`/`WarehousePicking` gleich gelöst werden soll.

### Entscheidung Gerald 2026-10-16 zu Punkt 2 (ersetzt den Umbruch-Vorschlag der Koordination)

- **Max-Lagerplatz-Limit** in der Zelle „Lagerplatz“; weitere Plätze **auf Hover** o. ä. anzeigen.
- Ausgestaltung (Vorschlag Koordination, in der Spec zu bestätigen):
  - Sichtbar die ersten **3** Plätze (Hauptlagerplatz zuerst, dann nach Menge absteigend), danach Badge „+n weitere“.
  - Badge öffnet die volle Liste per **Bootstrap-Popover** mit Trigger Hover **und** Klick/Tap (Touch-Geräte haben kein Hover).
  - Limit als Konstante oder Einstellung (Spec begründet); Spaltenfilter und CSV arbeiten weiter auf der **vollen** Liste
    (z. B. `data-filter-value` / CSV-Attribut mit allen Plätzen), damit Filtern nach einem verdeckten Platz trifft.

> Umgesetzt in [[2026-10-14-spaltenauswahl-zahnrad-sammel-spec]] (v1.65.0, Bündel, 2026-10-21).
