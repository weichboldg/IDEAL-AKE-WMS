---
type: uebersicht
title: "IDEAL: FA-Liste nur HauptFA + FA-Struktur uebersichtlich (Split-Uebersicht)"
status: Entwurf
created: 2026-11-26
updated: 2026-11-26
source_backlog: "[[2026-11-26-fa-liste-nur-hauptfa-und-fa-struktur-neu]]"
standort: IDEAL
teile:
  - "[[2026-11-26-fa-liste-nur-hauptfa-und-fa-struktur-neu-teil-1-spec]]"
  - "[[2026-11-26-fa-liste-nur-hauptfa-und-fa-struktur-neu-teil-2-spec]]"
open_questions: []
---

# FA-Liste nur HauptFA + FA-Struktur uebersichtlich

Zerlegung laut Backlog (`split: true`), Reihenfolge wie vom Menschen vorgegeben. Jeder Teil ist fuer sich
test- und mergebar; beide aendern nur die Web-App, keine Migration.

| # | Spec | Zweck | depends_on | SQL-Nummern |
|---|---|---|---|---|
| 1 | [[2026-11-26-fa-liste-nur-hauptfa-und-fa-struktur-neu-teil-1-spec]] | FA-Liste zeigt je HauptFA eine Zeile, Strukturfelder (Kopf + Wurzel) im Zahnrad, Knopf je Zeile in die FA-Struktur | – | keine (nur Lesen) |
| 2 | [[2026-11-26-fa-liste-nur-hauptfa-und-fa-struktur-neu-teil-2-spec]] | Entscheidungsvorlage FA-Struktur: Varianten A/B/C, Empfehlung B (Baugruppen-Baum, Teile per Schalter) | Teil 1 (Sprungziel; technisch unabhaengig) | keine |

## Abhaengigkeiten ausserhalb dieses Splits

- **Leitstand-Spec (paralleler Entwurf):** [[2026-11-26-beschichtung-summe-leitstand-hauptfa-kommissionierauftrag-liste-teil-2-spec]]
  fuehrt in `ProductionOrderRepository.GetForLeitstandGroupedAsync`/`BuildLeitstandQuery` den Parameter `nurHauptFa` und eine
  Zaehlung „ohne HauptFA-Zeile“ ein. Teil 1 nutzt **denselben** Parameter. Wer zuerst gebaut wird, fuehrt ihn ein; der andere
  Lauf verwendet ihn (kein zweiter Mechanismus). Im selben Buendel-Worktree: Leitstand-Spec zuerst, dann Teil 1. Deren AK 9
  („FA-Liste unveraendert“) gilt nur bis Teil 1 gemergt ist — beim zweiten Lauf den AK des anderen nicht zurueckdrehen.
- **Gruppenansicht FA-Liste:** [[2026-08-18-fa-liste-hierarchie-anzeige-spec]] (Testbereit) — Teil 1 hebt sie fuer die FA-Liste auf
  (Rueckfrage 1 in Teil 1), die anderen Ansichten bleiben.
- **Backlog ohne Spec:** [[2026-11-17-ideal-fa-listen-und-views-anzeige-durchgang]] — Teil 1 nimmt dessen FA-Listen-Spaltenpruefung
  teilweise vorweg (Mapping-Tabelle); Kommissionierlisten, Vormontage, Beschichtung, BDE-Teileliste bleiben dort.
- **Struktur-Abgleich:** [[2026-11-17-fa-struktur-abgleich-statt-full-replace-spec]] — liefert `VerschwundenSeit` (Pruefabfragen beider Teile filtern darauf); keine Aenderung daran.

## Gemeinsame Vorab-Pruefungen (vor Schranke 1 sinnvoll)

- Teil 1: W-1 (eine Wurzel je HauptFA), W-2 (Auftraege ohne HauptFA-Zeile), W-3 (Fuellgrad Kopffelder), S-1 (Sage IDEAL: FAZusatzinformationen-View vorhanden?).
- Teil 2: T-1 (Blaetter mit Arbeitsschritten), T-2 (Zeilen je Struktur).

Testszenarien: Stand 2026-11-26 ist TS-202 die letzte Nummer im Index; je Teil ein Kapitel, der Dev-Lauf vergibt die Nummern.
