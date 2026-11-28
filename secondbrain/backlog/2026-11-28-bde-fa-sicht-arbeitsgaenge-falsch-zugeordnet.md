---
type: backlog
typ: bug
status: in-spec
jira: WMS-10
created: 2026-11-28
standort: IDEAL
quelle: "Mail „WMS Anpassungen“ (Benjamin Andl-Braun, IDEAL) Punkt 1, weitergeleitet von Gerald im Chat 2026-11-28"
anhaenge:
  - anhaenge/2026-11-28-mail-wms-anpassungen-1.png
---
# BDE FA-Sicht: Arbeitsgänge passen nicht zum angezeigten Teil

## Meldung (Mail Punkt 1)
In der BDE-Teileliste, **FA-Sicht**, stehen bei Sub-FAs Arbeitsgänge, die laut Spalte **Arbeitsschritte** (korrekt) gar nicht an diesem Teil
liegen – „eventuell ein Zuweisungsfehler“. In den beiden anderen Teile-Sichten (Einzeln, Summiert) passt es.

Beispiel Screenshot (HauptFA 1069617, Terminal KA Kanten): Sub-FA 1069721 zeigt AG-Status VM·ST·EG·KA, Arbeitsschritte nur „VM“;
1069724 zeigt M31·ST·EG·KA, Arbeitsschritte „M31“; 1069728/1069730 (Arbeitsschritte „KA“) zeigen nur KA – stimmig.

## Befund (2026-11-28, nur gelesen, Worktree v1.99.89)
- FA-Sicht: `BdeFaAngebot.cs:107-130`. Die AG-Symbole kommen aus allen `WorkOperations` des **Auftrags** (Sub-FA), die Strukturzeile
  (Arbeitsschritte, Bezeichnung, Artikel, Matchcode) aus dem **ersten Knoten nach Position** der Sub-FA:
  `FaHierarchyNodes … GroupBy(HauptFA, SubFA) … OrderBy(Position).First()` (`:117-119`).
- **Hypothese (ungeprüft):** Hat eine Sub-FA mehrere Knoten, zeigt die Zeile die Daten eines Unterteils statt des Sub-FA-Kopfteils; die AG
  gehören aber zur ganzen Sub-FA. Alternativ stimmen die WMS-Arbeitsgänge der Sub-FA selbst nicht (AG-Abgleich/Mapping).
- Klären per Prüfabfrage: Knoten und `WorkOperations` je Sub-FA 1069721/1069724 im IDEAL-WMS gegen die Sage-FA-Arbeitsgänge.

## Rückfragen (Vorschlag)
- Welche Zeile ist „der Teil“ einer Sub-FA – der Kopfartikel der Sub-FA (Baugruppe) oder die einzelnen Positionen?
- Sollen in der FA-Sicht nur die AG stehen, die auch in den Arbeitsschritten der Zeile vorkommen, oder ist die Strukturzeile falsch gewählt?
