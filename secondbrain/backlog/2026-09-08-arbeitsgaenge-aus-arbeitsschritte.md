---
typ: feature
---
# IDEAL: Arbeitsgaenge (FaWorkSteps) aus `Arbeitsschritte` der Struktur ableiten

**Vorgemerkt am 2026-09-08** als eigener Folgeblock (Entscheidung 3 in
[[2026-09-08-bom-schnittstellen-bridge-hierarchisch]]). Noch nicht spezifizieren, bis die
BOM-Bridge und Thema 2 ([[2026-08-20-materialisierung-fachliche-felder-spec]]) durch sind.

## Ausgangslage

Auf AKE legt `FaWorkStepDetectionService` die `FaWorkSteps` je Auftrag per **Textheuristik** an:
`CachedBomItems.Bezeichnung1/2.ToLower().Contains(term)` je Suchbegriff eines `WorkStep`
(`FaWorkStepDetectionService.cs:62-65`), Nur-hinzufuegen-Semantik, `Source = Sync`.

IDEAL liefert die Wahrheit **explizit**: `FaHierarchyNode.Arbeitsschritte` = **leerzeichen-getrennte**
Liste der Schritte je Position (Anhang [[sage-views-ideal]] Z. 74 — **nicht** Komma!), dazu
`Arbeitsbereich` je Position (Z. 73). Die Heuristik auf IDEAL-Daten laufen zu lassen erzeugte
Fehldaten; sie wird durch die BOM-Bridge fuer hierarchische Auftraege hart abgeschaltet.

## Was zu bauen ist (Skizze, nicht Spec)

- Token-Mapping `Arbeitsschritte` → `WorkSteps`-Katalog (welche Tokens existieren bei IDEAL?
  Katalog-Pflege: automatisch anlegen oder nur melden — dieselbe Frage wie bei den Werkbaenken in
  Thema 2, Rueckfrage 1).
- Anlage der `FaWorkSteps` je **Sub-FA-Auftrag** aus den Tokens seiner Positionen (welche Ebene:
  eigene Zeile, direkte Kinder, alle Nachfahren? — analog zur Scope-Frage der BOM-Bridge, Partition
  `DirectChildren` bevorzugt).
- Nur-hinzufuegen-Semantik beibehalten (manuelle Schritte nie loeschen), `Source` unterscheidbar
  (z. B. `Struktur`), Audit-Felder, `ISyncLogger`.
- Beruehrt `FaWorklist` (Abarbeitungsliste mit Merkmal-Spalten je AG, seit v1.35 gruppiert) —
  dort muss nichts umgebaut werden, wenn die `FaWorkSteps` korrekt vorliegen.

## Offene Fragen (vor der Spec vom Fachbereich)

1. Welche Token kommen in `Arbeitsschritte` real vor, und entsprechen sie 1:1 dem WMS-Katalog?
2. Unbekannte Token: anlegen oder melden?
3. Ebene der Zuordnung (Sub-FA eigene Zeile vs. Kinder).

## Bezug

[[2026-09-08-bom-schnittstellen-bridge-hierarchisch]] (Klasse D, Gates),
[[2026-08-20-materialisierung-fachliche-felder-spec]] (K2, Werkbank-Stammdaten-Frage),
[[2026-07-29-standort-ideal-teil-8-spec]] (BDE je Sub-FA).
