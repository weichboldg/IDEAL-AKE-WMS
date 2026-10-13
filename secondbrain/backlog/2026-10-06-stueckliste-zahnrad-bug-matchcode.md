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
