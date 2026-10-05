---
typ: feature
---
# FA-Struktur: Darstellung, Spaltenfilter und Knoten-Icons

Nachtrag zu [[2026-07-29-standort-ideal-teil-2-spec]]. Die Baumanzeige stellt die Hierarchie
fachlich korrekt dar; die Darstellung ist aber noch roh. **Reine Praesentationsebene** — kein neues
Feld, keine Aenderung an Import, Datenmodell oder der Filterlogik der Teile 3-5.

## 1. Lesbarkeit (Defekt, nicht Geschmack)

Im dunklen Kopfband neben „HauptFA 1043111" steht Text in nahezu identischer Farbe zum Hintergrund
— praktisch unlesbar. Ebenfalls zu pruefen: die Badges (`K-02`, `S-01`, `H4-04`, `Komm.: PG`,
`beschichtet`) und die abgesetzten Matchcodes in eckigen Klammern.

Pruefkriterium ist **Kontrast, nicht Empfinden: WCAG AA — 4,5:1 fuer Fliesstext, 3:1 fuer grosse
Schrift und Bedienelemente.** Die Ansicht laeuft an Fertigungsterminals, teils bei schlechtem
Licht — Kontrast ist hier Funktion.

## 2. Tree-Table statt freifliessender Baum

Heute reihen sich die Angaben je Zeile inline aneinander; durch die Einrueckung steht nichts
untereinander. **Fuer Spaltenfilter braucht es zuerst ausgerichtete Spalten.** Vorschlag:

| Spalte | Inhalt |
|---|---|
| Struktur | Einrueckung + Expander + Icon + FA-Nummer (**einzige** Spalte, die mit der Tiefe waechst) |
| Matchcode | `[G-KT-FRR-700-...]` |
| Bezeichnung | Klartext |
| Arbeitsbereich | `K-02`, `S-01`, `H4-04` |
| Komm.-Ziel | `PG`, `LOET`, leer |
| Soll / Fert. | Mengen |
| SubFA | Nummer bzw. `0` |
| Status | `beschichtet` u. a. |

**Kernregel:** Nur die erste Spalte traegt die Einrueckung, alle anderen bleiben buendig. Sonst ist
es kein Tree-Table, sondern eingerueckter Fliesstext mit Tabellenrahmen.

Bei schmalen Bildschirmen die hinteren Spalten ausblendbar machen — **nicht** die Struktur-Spalte
quetschen, sie traegt die Orientierung.

## 3. Spaltenfilter — auf einem Baum eine andere Semantik

Auf einer flachen Liste blendet ein Filter Zeilen aus. Hier ginge dabei der Kontext verloren: Ein
Treffer in der Tiefe schwebte ohne seine Vorfahren im Nichts.

- **Treffer** hervorheben, **Pfad zur Wurzel** als Kontext sichtbar lassen (gedimmt), nicht
  passende Geschwister ausblenden. Der Pfad klappt automatisch auf — bereits als Q4 in Teil 2
  entschieden.
- Das bestehende Feld „Knoten in angezeigten Strukturen hervorheben" macht ausdruecklich das
  **andere**: hervorheben, ohne die Struktur zu veraendern. Beide Mechanismen nebeneinander
  muessen **unterscheidbar beschriftet** sein, sonst erwartet der Nutzer vom einen, was das
  andere tut.
- Fuer Spalten mit kleinem Wertebereich (Arbeitsbereich, Komm.-Ziel, Status) ist ein
  **Auswahlfilter** besser als ein Textfeld — sonst weiss der Nutzer nicht, welche Werte es gibt.

## 4. Icons nach Knotentyp

Die Klassifikation steckt **bereits in den Daten** — kein neues Feld noetig:

| Typ | Erkennung |
|---|---|
| Endprodukt / Wurzel | oberste Zeile der Struktur |
| Baugruppe (Eigenfertigung) | `SubFA != 0` — hat einen eigenen Auftrag |
| Zukaufteil | `SubFA = 0` **und** Beschaffungsartikel |
| Lager-/Fertigungsmaterial | `SubFA = 0`, kein Beschaffungsartikel |

Zustandsmarker (`beschichtet`, Komm.-Ziel) bleiben **Badges** — das ist ein Zustand, kein
Knotentyp, und gehoert nicht ins Typ-Icon vermischt.

**Regeln:**
- **Icon nie alleiniger Bedeutungstraeger** — immer Tooltip/`aria-label` mit Klartext. Wer die
  Ansicht zum ersten Mal sieht, kennt die Symbolik nicht.
- **Farbe nie alleiniger Unterscheider** — die Form muss ebenfalls unterscheiden
  (Farbfehlsichtigkeit, schlechte Terminalbildschirme).
- **Bootstrap Icons** verwenden (das Projekt laeuft auf Bootstrap 5) — keine neue Bibliothek.
- Kleine, ausklappbare **Legende** ueber dem Baum.

## 5. Verhalten wie OSEON-Tracking

Referenzimplementierung im Repo: **`Views/OseonReporting/_OseonReportingTable.cshtml`** und
`OperationsOverview.cshtml`. **Lesen und uebernehmen, nicht neu erfinden** — Chevron-Toggle,
Expand/Collapse, Zustandsverhalten. Kein d3, keine neue Baum-Bibliothek (Vorgabe aus Teil 2).

Aus dem Vorbild (EPLAN-Seitenbaum) zusaetzlich uebernehmenswert:
- **Einrueckungs-Leitlinien** (senkrechte Linien je Ebene) — machen die Tiefe auf einen Blick
  klar, gerade ab vier Ebenen. Der **groesste Einzelgewinn** fuer die Lesbarkeit, groesser als
  jedes Icon.
- **„Alle auf / alle zu"** als Schalter ueber dem Baum.
- Ruhiges, dichtes Zeilenraster — bei 50+ Positionen zaehlt jede Zeilenhoehe.

## Abgrenzung

- Keine Aenderung an `FaHierarchyNode`, am Import oder an den Filtern der Teile 3-5.
- Keine neue Bibliothek (weder Icons noch Baum noch Tabelle).
- **Konsistenz vor Eigenstaendigkeit:** Die Ansicht soll aussehen wie der Rest des WMS, nicht wie
  ein Fremdkoerper. Der `frontend-design`-Skill ist Pflicht (CLAUDE.md) und liefert das Handwerk
  fuer Typografie, Kontrast und Hierarchie — wo er zu einem eigenen Stil draengt, gewinnt die
  Hausschrift.

## Vorbedingung

Sinnvoll beurteilbar erst mit **produktivnahen Daten**: Ob Einrueckung, Spaltenbreiten und
Zeilendichte tragen, zeigt sich an einer Struktur mit vielen Ebenen und 50+ Positionen — nicht an
einem Beispiel mit drei Zeilen.

## Bezug

[[2026-07-29-standort-ideal-teil-2-spec]] (Baumanzeige),
[[2026-07-29-standort-ideal-uebersicht]].
