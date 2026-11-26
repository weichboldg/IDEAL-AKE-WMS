---
type: uebersicht
title: "Beschichtung Summe im Druck, Leitstand nur HauptFA, Kommissionierauftrag Maske Stückliste/Liste"
status: Entwurf
created: 2026-11-26
updated: 2026-11-26
source_backlog: "[[2026-11-26-beschichtung-summe-leitstand-hauptfa-kommissionierauftrag-liste]]"
standort: IDEAL
teile:
  - "[[2026-11-26-beschichtung-summe-leitstand-hauptfa-kommissionierauftrag-liste-teil-1-spec]]"
  - "[[2026-11-26-beschichtung-summe-leitstand-hauptfa-kommissionierauftrag-liste-teil-2-spec]]"
  - "[[2026-11-26-beschichtung-summe-leitstand-hauptfa-kommissionierauftrag-liste-teil-3-spec]]"
open_questions: []
---

# Übersicht: Beschichtung Summe, Leitstand nur HauptFA, Kommissionierauftrag als Liste

Quelle: [[2026-11-26-beschichtung-summe-leitstand-hauptfa-kommissionierauftrag-liste]]. Codestand aller Fundstellen: Bündel-Worktree
`.claude/worktrees/2026-08-07-ideal-teile-1-5` (AppVersion 1.99.81).

## Warum aufgeteilt

Drei getrennte Module (Beschichtungsauftrag-Druck, Leitstand, Kommissionierauftrag-Seite) mit eigenen Entscheidungen. Die Teile sind
fachlich und technisch **unabhängig**; die `depends_on`-Kette regelt nur die Reihenfolge im gemeinsamen Bündel-Worktree (Version,
`docs/TESTSZENARIEN.md`, `Changelog.cshtml`). Jeder Teil darf einzeln freigegeben, übersprungen oder gemergt werden.

## Reihenfolge

| Teil | Zweck (eine Zeile) | Standort | Version / TS (Vorschlag) | SQL |
|---|---|---|---|---|
| [[2026-11-26-beschichtung-summe-leitstand-hauptfa-kommissionierauftrag-liste-teil-1-spec]] | Beschichtungsauftrag Druck/PDF immer summiert je Artikel, Summenzeile „Summe Teile im Auftrag“; hebt Antwort 11 der Spec 2026-11-18 auf (Rückfrage 1) | beide (Rückfrage 4) | v1.99.82 / TS-203 | keine |
| [[2026-11-26-beschichtung-summe-leitstand-hauptfa-kommissionierauftrag-liste-teil-2-spec]] | Leitstand hierarchisch nur HauptFA-Zeilen, Schalter „Sub-FAs anzeigen“, Banner bei fehlender HauptFA-Zeile; Freigabe-Haken am HauptFA existiert bereits (Befund klären) | IDEAL | v1.99.83 / TS-204 | keine |
| [[2026-11-26-beschichtung-summe-leitstand-hauptfa-kommissionierauftrag-liste-teil-3-spec]] | Kommissionierauftrag-Seite mit Umschalter Stückliste/Liste, Standard „Liste“ bei `FaHierarchyKommissionierlistenAktiv`; Liste je Artikel summiert, Ist-Menge in Positionsreihenfolge auf die Zeilen verteilt (Epic, Etappen A/B, Rückfrage 7) | IDEAL | v1.99.84 / TS-205 | keine |

Versionen und TS-Nummern sind Vorschläge zum Schreibzeitpunkt (letzte belegte TS-202); der Dev-Lauf prüft beide erneut.
**SQL-Nummernblock:** keiner der Teile braucht ein Schema-, Daten- oder Einmal-Skript. Manifest: Teil 1 keine Einträge; Teil 2 eine
Pruefung + ein Info; Teil 3 ein Info.

## Überschneidungen

- [[2026-11-18-beschichtungsauftrag-ausbau-checkboxen-versand-druck-spec]] (v1.99.41): Teil 1 kehrt deren Antwort 11 (Druck je Position) um.
- [[2026-09-25-kommissionierung-nur-hauptfa-spec]] (v1.46.0) und [[2026-11-18-ideal-kommissionierliste-filter-spalten-leitstand-spec]]
  (v1.99.43): Grundlage von Teil 2 (Freigabe nur am HauptFA, Leitstand-Gate).
- Backlog [[2026-09-25-leitstand-subfa-readonly-stueckliste]]: betrifft nach Teil 2 nur noch die Ansicht „Sub-FAs anzeigen“.
- [[2026-11-08-kommissionierung-auftraege-kopf-zeilen-spec]]: Grundlage der Auftragsseite, die Teil 3 um eine Maske erweitert.

## Offene Rückfragen

Keine zur Aufteilung selbst. Die fachlichen Fragen stehen je Teil.
