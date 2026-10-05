---
typ: feature
---
# IDEAL: Arbeitsgaenge (FaWorkSteps) aus `Arbeitsschritte` der Struktur ableiten

**Vorgemerkt am 2026-09-08** als eigener Folgeblock (Entscheidung 3 in
[[2026-09-08-bom-schnittstellen-bridge-hierarchisch]]).

> **SPERRE AUFGEHOBEN (2026-09-10):** Die Vorbedingung „nicht spezifizieren, bis BOM-Bridge und
> Thema 2 durch sind" ist erfuellt — beide stehen auf `Testbereit`
> ([[2026-09-08-bom-schnittstellen-bridge-hierarchisch-spec]] v1.36,
> [[2026-08-20-materialisierung-fachliche-felder-spec]] v1.37). **Spezifizierbar, sobald Frage 1
> beantwortet ist.**

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

## Offene Fragen — auf EINE reduziert (Stand 2026-09-10)

**1. Welche Token kommen real vor, und passen sie zum WMS-Katalog? — OFFEN, aber halb messbar.**
Die erste Haelfte ist **keine Fachfrage, sondern eine Abfrage**:
```sql
SELECT DISTINCT value
FROM FaHierarchyNode
CROSS APPLY STRING_SPLIT(Arbeitsschritte, ' ')
WHERE Arbeitsschritte IS NOT NULL AND Arbeitsschritte <> ''
ORDER BY value;
```
Das liefert die tatsaechliche Token-Menge (Trennzeichen **Leerzeichen**, nicht Komma — Anhang Z. 74).
Erst die zweite Haelfte — ob jedes Token einem `WorkStep` im Katalog entspricht — braucht den
Fachbereich. Mit der Liste in der Hand ist das ein Abgleich von Minuten statt einer offenen Frage.

**2. Unbekannte Token: anlegen oder melden? → BEANTWORTET durch Analogie: MELDEN.**
Dieselbe Frage wurde fuer die Werkbank-Stammdaten entschieden
([[2026-08-20-materialisierung-fachliche-felder-spec]], Antwort 1): **nicht automatisch anlegen.**
Stammdaten aus einer Fremdquelle zu erzeugen fuellt den Katalog mit Tippfehlern und Altlasten, ohne
dass es jemand entschieden hat. Stattdessen **Sammelmeldung je Sync-Lauf** mit der Liste der
unbekannten Token **und der Anzahl betroffener Positionen**, damit das Nachpflegen einmalig und
trivial ist. Mail nur bei Aenderung der Menge (sonst Rauschen, vgl. S1 dort).
**Zu pruefen wie dort:** Ist die `WorkStep`-Zuordnung ein Fremdschluessel? Dann greift die Anlage
erst nach dem Pflegen — Zwei-Lauf-Ablauf, gehoert in den Deploy-Abschnitt.

**3. Ebene der Zuordnung → BEANTWORTET durch Analogie: `DirectChildren`.**
Wie bei der BOM-Bridge: Die Arbeitsgaenge eines Sub-FA ergeben sich aus **seiner eigenen Zeile plus
seinen direkten Kindern** — nicht aus allen Nachfahren. Begruendung dieselbe: Was in einem anderen
Sub-FA gefertigt wird, hat dort seine eigenen Arbeitsgaenge; ueber alle Ebenen zu sammeln erzeugte
dieselbe Doppelzaehlung wie bei der Stueckliste.
**Ausdruecklich zu bestaetigen** — die Analogie ist plausibel, aber nicht bewiesen.

## Bezug

[[2026-09-08-bom-schnittstellen-bridge-hierarchisch]] (Klasse D, Gates),
[[2026-08-20-materialisierung-fachliche-felder-spec]] (K2, Werkbank-Stammdaten-Frage),
[[2026-07-29-standort-ideal-teil-8-spec]] (BDE je Sub-FA).
