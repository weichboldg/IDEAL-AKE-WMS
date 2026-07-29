# Ideen

Denkraum VOR dem Backlog. Hier sammelst du Anforderungen, Einfaelle, "waere
schoen wenn", Skizzen - OHNE dass etwas loslaeuft. Der Watcher beobachtet
diesen Ordner NICHT. Nichts wird hier spezifiziert, nichts umgesetzt.

Eine Idee darf hier beliebig lange reifen, unfertig sein, offene Fragen haben.
Du kannst sie ausarbeiten, ergaenzen, wieder verwerfen.

## Die Geste: Idee -> Backlog (Kette startet)

Wenn eine Idee reif ist und die Pipeline loslaufen soll, verschiebst du die
Datei nach `../backlog/`. ERST dieser Umzug ist der Startschuss - der Watcher
sieht die Ankunft im Backlog und laesst den Spec-Lauf beginnen.

Das ist derselbe Gedanke wie bei den anderen Schranken: eine bewusste
Ordner-Geste des Menschen loest den naechsten Schritt aus. Ideen -> Backlog ist
die "nullte" Schranke (Idee wird zur Aufgabe).

## Format

Voellig frei. Ein Titel und ein paar Zeilen genuegen. Optional Frontmatter,
das spaeter im Backlog ausgewertet wird - dann ist es beim Verschieben schon
gesetzt:

```
---
typ: feature        # oder bug
split: true         # optional, spaeter im Backlog wirksam
epic: true          # optional
---
Titel der Idee

Worum geht es, warum, grobe Richtung. Offene Fragen ruhig festhalten.
```

Frontmatter ist hier reine Vorbereitung und bleibt wirkungslos, solange die
Datei in ideen/ liegt - der Watcher liest den Ordner nicht. Erst nach dem
Verschieben nach backlog/ wird es ausgewertet.

## Abgrenzung

- **ideen/** = Denkraum, kein Automatismus. Reifen lassen.
- **backlog/** = Motor-Eingang. Ankunft startet die Kette.
- Nur dokumentieren, nie umsetzen wollen? Bei Bugs: direkt in `../bugs/`
  (siehe backlog/README.md).
