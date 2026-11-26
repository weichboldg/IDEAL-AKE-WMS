---
type: changelog
version: 1.99.88
date: 2026-11-26
---
# v1.99.88 — IDEAL-Leitstand eine Zeile je HauptFA + Summenzeile nur Teilesumme

Spec [[2026-11-26-beschichtung-summe-leitstand-hauptfa-kommissionierauftrag-liste-teil-2-spec]] (Bündel 1 Teil 2) und
Freigabe-Nachtrag N-1 von [[2026-11-26-beschichtung-summe-leitstand-hauptfa-kommissionierauftrag-liste-teil-1-spec]].
Lauf-Notiz [[2026-11-26-buendel-1-lagerbestellung-goal-lauf]]. Worktree `feature/2026-08-07-ideal-teile-1-5`, Commits
`5d7639dc`, `a89d98cc` (N-1), `ebb16bd7` (QA-Fix). **Keine Migration, keine neue Einstellung.** TS-208, TS-205 angepasst. Nicht gemergt.

## Kernpunkte

- Leitstand IDEAL ohne Gruppenkopf: eine Zeile je HauptFA, „+ n Sub-FAs“; Lack-T/Glas/Zukauf/VK–VA zusammengefasst
  (`LeitstandHauptFaZusammenfassung`), Klick speichert nur HauptFA, Hinweis „Sub-FA bestimmt den Wert weiter“, W3/W4 gesperrt.
- Schalter „Sub-FAs anzeigen“ = alte Ansicht; Sub-FA-Nummernsuche nur dort, sonst Hinweis. Banner bei Auftrag ohne HauptFA-Zeile.
- Neu laden nach Klick nur bei `data-neu-laden=true` (QA-Fix ebb16bd7).
- N-1: Beschichtungsauftrag-Druck/PDF „Summe Teile im Auftrag N“ ohne Positionsanzahl; `PositionenGesamt` entfernt.

## Deploy-Risiken

- Nur Web. Manifest `nachlauf.psd1`: `Pruefung/LeitstandOhneHauptFaZeile`, `v1.99.88-LeitstandNurHauptFa`.
- QA-Befunde ohne Blocker: F-1 Kaskaden-Dialog zählt HauptFA mit („39“ vs „+ 38“, Bestand), F-2 Spaltenfilter Glas/Zukauf
  wirkungslos (Bestand), F-3 Hinweis macht VK-Spalte breit.
