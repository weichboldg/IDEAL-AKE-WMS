---
typ: bug
---
# FA-Baum: gedimmte Vorfahrenzeilen unter WCAG AA

**Aufgenommen 2026-10-12** – QA-Fund M1 aus dem Lauf FA-Struktur Teil 1 (v1.52.0), aelter als dieser Lauf.

## Befund (laut qa-agent, von der Koordination nicht am Bildschirm geprueft)

Ist im FA-Baum (`/FaHierarchy`) ein Spaltenfilter aktiv, werden die zur Orientierung mitgezeigten Vorfahrenzeilen
auf 60 % Deckkraft gedimmt. Damit faellt in diesen Zeilen aller Text – auch die neuen Arbeitsgang-Kuerzel aus
[[2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-1-spec]] – unter den WCAG-AA-Kontrast. Im Stylesheet ist ein
Hinweis vermerkt.

## Vorschlag

Statt Deckkraft eine andere Kennzeichnung (z. B. Hintergrundton oder Kursivschrift) mit AA-konformem Textkontrast.
Kleiner Einzel-Fix, kann bei Gelegenheit ins Buendel.

**Uebernommen 2026-10-13:** wird in [[2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-2-spec]] mitbehoben (Antwort 11 = C, Gerald).

**Erledigt 2026-10-13:** mit FA-Struktur Teil 2 (v1.55.0) – Deckkraft 0,6 entfernt, Kontextzeilen kursiv mit grauem Streifen links (Kontrast laut Bericht 7,83–8,96:1).
