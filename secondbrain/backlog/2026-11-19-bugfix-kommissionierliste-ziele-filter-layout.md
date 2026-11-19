---
typ: backlog
status: erledigt
created: 2026-11-19
herkunft: "Gerald 2026-11-19 09:55 mit Screenshot (v1.99.43, IDEAL); bereits als Befund 1 im Doku-Lauf [[2026-11-19-doku-v1-99-43]]"
standort: IDEAL
art: bugfix
---
# Bugfix: Ziel-Auswahl der Kommissionierliste rechts schmal untereinander statt nebeneinander

![[2026-11-19-kommissionierliste-ziele-schmal.png]]

## Beobachtung
Filterkarte Kommissionierlisten (Summiert): Überschrift „Kommissionier-Ziele“ links, die Kästchen („— alle Ziele —“, EM, H1-03 … K-01, K-02,
KOM, LÖT …) stehen ganz rechts in einer schmalen Spalte, Texte umgebrochen („K-“/„01“), Karte wird sehr hoch und waagrecht scrollbar.

## Ursache (Code, Bündel v1.99.43, nur gelesen)
`Views/FaHierarchyKommissionierListen/_KommissionierZiele.cshtml:10-14`: zwischen `<legend>` (Z. 11) und dem Flex-Container (Z. 14)
steht `<input type="hidden" name="target" value="" />` (Z. 13). Bootstrap-Reboot setzt `legend { float:left; width:100% }` und hebt
das Fließen nur mit `legend + * { clear:left }` auf – das trifft jetzt das versteckte Feld, nicht den Container. Der Container fließt
neben die 100 % breite Legende und wird auf die Restbreite gequetscht.

## Fix
Verstecktes Feld vor die `<legend>` oder hinter den Container verschieben (Formular-Wert bleibt gleich), alternativ `clear-both`/`w-100`
am Container. Prüfen in Liste + Summiert, Breite 1440 px und 390 px; Drift-Wächter/Tests unverändert. Kleiner Fix für den Sammellauf.

## Erledigt
2026-11-19 in v1.99.44, Commit `0fe4cbcf`, Bündel `feature/2026-08-07-ideal-teile-1-5` (nicht gemergt). Changelog [[2026-11-19-v1-99-44-sammel-bugfix]], TS-166.
