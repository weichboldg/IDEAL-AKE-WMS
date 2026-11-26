---
type: uebersicht
title: "Leitstand: QA-Befunde F-1 bis F-3 aus v1.99.88 (Kaskaden-Zahl, Spaltenfilter, Hinweisbreite)"
status: Entwurf
created: 2026-11-26
updated: 2026-11-26
standort: beide
teile:
  - "[[2026-11-26-leitstand-kaskaden-zaehler-ohne-hauptfa-spec]]"
  - "[[2026-11-26-leitstand-spaltenfilter-glas-zukauf-ohne-wirkung-spec]]"
  - "[[2026-11-26-leitstand-hinweis-vk-spalte-breit-spec]]"
open_questions: []
---

# Übersicht: Leitstand-Befunde F-1 bis F-3

Herkunft: QA-Befunde der Spec [[2026-11-26-beschichtung-summe-leitstand-hauptfa-kommissionierauftrag-liste-teil-2-spec]] (v1.99.88), als Backlog
abgelegt von Gerald am 2026-11-26. Codestand aller Fundstellen: Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5` (v1.99.89).

Die drei Specs sind fachlich unabhängig und einzeln freigebbar. Sie berühren aber dieselben Dateien (`PickingLeitstandController.cs`,
`Views/PickingLeitstand/Index.cshtml`, `_PickingLeitstandRow.cshtml`, `docs/TESTSZENARIEN.md`); die `depends_on`-Kette regelt nur die Reihenfolge im
gemeinsamen Bündel-Worktree, damit Versionen, Testkapitel und Changelog nicht kollidieren. Jede Spec darf übersprungen werden.

## Reihenfolge

| # | Spec | Zweck (eine Zeile) | Bug-Record | Standort | Vorschlag TS | SQL-Nummern |
|---|---|---|---|---|---|---|
| 1 | [[2026-11-26-leitstand-kaskaden-zaehler-ohne-hauptfa-spec]] | Kaskaden-Dialog, Meldung und Log zählen Sub-FAs wie „+ n Sub-FAs“ (HauptFA-Zeile und stornierte nach Rückfrage) | [[2026-11-26-leitstand-kaskaden-zaehler-ohne-hauptfa-bug]] | IDEAL | TS-210 | keine (keine Schema-Änderung) |
| 2 | [[2026-11-26-leitstand-spaltenfilter-glas-zukauf-ohne-wirkung-spec]] | Glas, Zukauf, Status, Kommissionierer (und Lack-T) filtern als Memory-Filter auf den angezeigten Wert | [[2026-11-26-leitstand-spaltenfilter-glas-zukauf-ohne-wirkung-bug]] | beide | TS-211 | keine |
| 3 | [[2026-11-26-leitstand-hinweis-vk-spalte-breit-spec]] | Hinweise im Nur-HauptFA-Modus als Kurzzeichen mit Tooltip/Legende statt Satz | – (Feature) | IDEAL | TS-212 | keine |

Versionen: je Spec eine Patch-Version ab der nächsten freien nach v1.99.89; der Dev-Lauf prüft Version und TS-Nummer beim Start
(Stand 2026-11-26: letztes Kapitel im Index TS-209, keine Reservierung TS-21x in anderen Specs).

## Abhängigkeiten

- 2 nach 1: beide ändern `PickingLeitstandController.cs` (1: Kaskaden-Aktionen `:713-753`; 2: Memory-Filter `:94-103`, `:804-815`) - keine fachliche Kopplung.
- 3 nach 2: beide ändern `Index.cshtml` (2: Kopf `:189`; 3: Zählzeile `:144-147`) - keine fachliche Kopplung.
- Deploy: alle drei nur Web, keine Migration, keine Manifest-Einträge. QA auf DEV IDEAL (1, 3) und DEV AKE + IDEAL (2).
