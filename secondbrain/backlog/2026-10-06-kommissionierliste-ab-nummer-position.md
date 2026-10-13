---
typ: feature
---
# Kommissionierliste: AB-Nummer und Position anzeigen

**Aufgenommen 2026-10-06** (Anforderung Gerald, Koordination).

## Anforderung (Gerald)

In den Kommissionierlisten AB-Nummer und Position anzeigen.

## Ausgangslage (am Code belegt, Bündel-Worktree)

- `ABNr` und `Pos` liegen in `FaHierarchyOrderInfo` (`Models/FaHierarchyOrderInfo.cs:23-24`),
  Granularität laut Klassenkommentar: **ein Datensatz = ein Auftrag (ABNr + Pos)**, verknüpft über
  `HauptFA`.
- **Kombinationsgeräte** tragen denselben `HauptFA` mit mehreren `FaHierarchyOrderInfo`-Zeilen
  (Klassenkommentar Z. 7-9). Kopfdaten werden bewusst **nicht** per Join an die Positionszeilen gehängt
  (kartesisches Produkt), siehe [[2026-08-06-kombinationsgeraete-montageabteilung]].
- Kommissionierlisten: summierte Ansicht und Liste, Druck und PDF
  ([[2026-09-25-kommissionierliste-summierung-pdf-spec]]).

## Für die Spec zu klären (Empfehlungen der Koordination in Klammern)

1. **Wo:** im **Gruppenkopf je HauptFA** (Empfehlung – AB/Pos hängen am Auftrag, nicht an der
   Position) oder als Spalte in jeder Zeile.
2. **Kombinationsgeräte:** alle AB/Pos des HauptFA anzeigen, nicht eine willkürlich wählen.
3. Beide Ansichten (Summiert, Liste), Druckansicht und PDF; Suche/Filter nach AB-Nummer gewünscht?
4. Bei Spalten-Lösung: Spaltenauswahl-Registrierung (`ColumnDefinitions`, [[fallstricke]] §3).

## Hinweis zur Reihenfolge

Klein. Baut auf v1.47 im Bündel auf – Umsetzung nach Schranke 2 des Bündels, oder als Etappe im Bündel,
wenn Gerald es vor dem Merge braucht.
