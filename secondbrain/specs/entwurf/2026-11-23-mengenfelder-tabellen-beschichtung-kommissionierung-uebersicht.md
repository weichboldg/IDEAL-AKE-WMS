---
type: uebersicht
title: "Sammelmeldung 2026-11-23: Mengenfelder, Tabellen, Beschichtungsauftrag, Lagerbestellung, Klärungen"
status: Entwurf
created: 2026-11-23
updated: 2026-11-23
source_backlog: "[[2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung]]"
standort: beide
bug: "[[2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung-bug]]"
teile:
  - "[[2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung-teil-1-spec]]"
  - "[[2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung-teil-2-spec]]"
  - "[[2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung-teil-3-spec]]"
  - "[[2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung-teil-4-spec]]"
  - "[[2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung-teil-5-spec]]"
open_questions: []
---

# Übersicht: Sammelmeldung vom 2026-11-23 (sechs Punkte, fünf Teile)

Quelle: [[2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung]]. Codestand für alle Fundstellen: Bündel-Worktree
`.claude/worktrees/2026-08-07-ideal-teile-1-5` (AppVersion 1.99.49 laut Auftrag; die Specs v1.99.52/1.99.53 sind dort schon Testbereit).

## Warum aufgeteilt

Die sechs Punkte berühren getrennte Module, haben getrennte Entscheidungen (Varianten in Teil 3 und Teil 4) und lassen sich einzeln
freigeben und testen. Ein Epic würde eine Freigabe an alle sechs Antworten binden. Zwei Punkte (3 und 5) brauchen vor jeder Code-Zeile
eine Prüfung am Zielsystem; sie stehen deshalb zusammen in einem Klärungs-Teil am Ende.

Die Teile sind fachlich **unabhängig**. Die `depends_on`-Kette in den Teil-Specs regelt nur die Reihenfolge im gemeinsamen
Bündel-Worktree (Versionsnummern, `docs/TESTSZENARIEN.md`, `Changelog.cshtml` würden sonst kollidieren). Jeder Teil darf übersprungen werden.

## Reihenfolge

| Teil | Backlog-Punkt | Zweck (eine Zeile) | Vorschlag Version / TS | SQL |
|---|---|---|---|---|
| [[2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung-teil-1-spec]] | 1 | Ein globaler Haken in `site.js`: Pfeil/Spinner an Dezimal-Zahlenfeldern zählt ±1, Mausrad ändert keinen Wert | v1.99.54 / TS-179 | keine |
| [[2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung-teil-2-spec]] | 2 | Spalten-Umsortieren an 7 Listen einschalten (inkl. „WE zu Bestellung“), 16 Raster bewusst ohne Zahnrad festhalten | v1.99.55 / TS-180 | keine |
| [[2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung-teil-3-spec]] | 4 | Beschichtungsauftrag: Status-Knopf speichert den ungespeicherten Entwurf mit (Variante a) | v1.99.56 / TS-181 | keine |
| [[2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung-teil-4-spec]] | 6 | Lagerbestellung „überall NAN“: Entscheidung a/b/c zu Antwort 10 der v1.99.53-Spec, mindestens ehrlicher Hinweis | v1.99.57 / TS-182 | keine |
| [[2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung-teil-5-spec]] | 3 + 5 | Klärung am Zielsystem: Lagerplatzbezeichnung in Artikelbestände, Menge IST am AKE-Test (Bündel hat Ist-Menge seit v1.88.0) | ggf. v1.99.58 / TS-183 | keine |

Versionen und TS-Nummern sind Vorschläge zum Schreibzeitpunkt (letzte belegte TS-178, `docs/TESTSZENARIEN.md:13619`; TS-171/172 in
anderen Entwürfen reserviert). Der Dev-Lauf prüft beide erneut. **SQL-Nummernblock:** keiner der Teile braucht ein Schema-, Daten- oder
Einmal-Skript.

## Überschneidungen

- [[2026-11-23-wareneingang-zu-bestellung-bedienung-spec]] (v1.99.52, Testbereit): dieselbe Positionstabelle `PurchaseOrders/Receive.cshtml`.
  Dort ist Umsortieren bewusst Out-of-Scope (Z. 69-70, 158, 163) und von `ViewKeyDriftGuardTests.cs:63` festgeschrieben. Teil 2 kehrt
  das um, falls Gerald zustimmt (Teil 2, Rückfrage 1).
- [[2026-11-23-lagerbestellung-verbrauchsbuchung-bei-lieferung-spec]] (v1.99.53, Testbereit, nicht gemerged): Teil 4 ist eine Nachbesserung
  dazu. Die v1.99.53-Spec wird nicht geändert.
- [[2026-11-18-beschichtungsauftrag-ausbau-checkboxen-versand-druck-spec]]: Teil 3 ändert das dort eingeführte „EIN Formular, EIN Speichern“ (A8) nicht, nur die
  Status-Knöpfe.

## Offene Rückfragen

Keine zur Aufteilung selbst. Die fachlichen Fragen stehen je Teil.

## Freigabe 2026-11-23 (Gerald)

Teile 1–4 nach Empfehlung freigegeben. Teil 5: Punkt 3 (Artikelbestände) ohne Code, vermutlich Stammdaten am Zielsystem; Punkt 5 als
eigene Spalte „Menge IST“ freigegeben (Nachtrag in [[2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung-teil-5-spec]], v1.99.58,
TS-183). Alle fünf Teil-Specs liegen in `specs/freigegeben/`.
