---
typ: feature
---
# Vormontage: Isolierfraesen-Export (Zwischenablage)

Herausgeloest aus [[2026-07-29-standort-ideal-teil-5-spec]] (Freigabe-Antworten 1 und 2). Der
Export war urspruenglich die dritte Sicht von Teil 5, laesst sich aber ohne Format-Vorgabe nicht
bauen \u2014 er waere ein leeres Akzeptanzkriterium geblieben.

## Worum geht es

Die Vormontage-Liste soll ihre Positionen in die Zwischenablage exportieren koennen, in einem
Format, das sich **unveraendert in die Isolierfraesen-Software importieren** laesst. Umsetzung
ueber `navigator.clipboard`, analog zu den bestehenden Clipboard-Mustern im Projekt.

## Vorbedingungen \u2014 beides fehlt derzeit

1. **Format-Spezifikation der Isolierfraesen-Software:** exakte Spalten, Reihenfolge,
   Trennzeichen, Zeilenende, Kopfzeile ja/nein, Zahlen-/Datumsformat. Der Anhang verweist nur auf
   \"kompatibel zum Import\", ohne Format-Dokument.
2. **Referenz-Screenshot `Konzept_Uebertrag_MDE_System.docx`** (Vormontage-Ansicht mit Reitern je
   Arbeitsbereich) \u2014 lag der Spec-Runde nicht vor.

Ohne (1) ist keine Abnahme moeglich: Man kann nicht pruefen, ob der Export \"unveraendert
importierbar\" ist, wenn das Zielformat unbekannt ist.

## Bezug

Setzt [[2026-07-29-standort-ideal-teil-5-spec]] voraus (die beiden Sichten Einzelteile und
Summiert). Aufwand danach klein \u2014 eine zusaetzliche Sicht plus Clipboard-Ausgabe.
