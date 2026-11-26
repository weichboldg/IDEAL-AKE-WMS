---
type: uebersicht
title: "IDEAL: FA-Liste nur HauptFA + FA-Struktur uebersichtlich (Split-Uebersicht)"
status: Freigegeben
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
| 1 | [[2026-11-26-fa-liste-nur-hauptfa-und-fa-struktur-neu-teil-1-spec]] | FA-Liste zeigt je HauptFA eine Zeile (Sub-FA-Zeilen ganz weg), 17 Strukturfelder (Kopf + Wurzel, inkl. PPS-Status und Material) im Zahnrad, Default ausgeblendet; Matchcode-Rueckfall auf die Strukturwurzel; Knopf je Zeile in die FA-Struktur | – | keine (nur Lesen) |
| 2 | [[2026-11-26-fa-liste-nur-hauptfa-und-fa-struktur-neu-teil-2-spec]] | FA-Struktur Variante D (Gerald 2026-11-26): je HauptFA eine Zeile mit 15 Kopfspalten (Keys/Labels wie FA-Liste, alle sichtbar), Struktur/Matchcode beim Querscrollen angeheftet, Baum per Chevron, Teile per Knopf; ~~Entscheidungsvorlage A/B/C, Empfehlung B~~ | Teil 1 (Sprungziel; Kopfspalten-Keys aus Teil 1 — Teil 1 zuerst bauen) | keine |

Stand Rework 2026-11-26: Teil 1 Freigabe-Antworten 1-5 und Teil 2 Freigabe-Antworten 1-10 sind in den Rumpf gezogen; Teil 2
B-1 ist damit erledigt. Offen nur noch Teil 1 Rueckfrage 6 (Reihenfolge Matchcode-Rueckfall, nicht blockierend).

## Abhaengigkeiten ausserhalb dieses Splits

- **Leitstand-Spec (paralleler Entwurf):** [[2026-11-26-beschichtung-summe-leitstand-hauptfa-kommissionierauftrag-liste-teil-2-spec]]
  fuehrt in `ProductionOrderRepository.GetForLeitstandGroupedAsync`/`BuildLeitstandQuery` den Parameter `nurHauptFa` und eine
  Zaehlung „ohne HauptFA-Zeile“ ein. Teil 1 nutzt **denselben** Parameter. Wer zuerst gebaut wird, fuehrt ihn ein; der andere
  Lauf verwendet ihn (kein zweiter Mechanismus). Im selben Buendel-Worktree: Leitstand-Spec zuerst, dann Teil 1. Deren AK 9
  („FA-Liste unveraendert“) gilt nur bis Teil 1 gemergt ist — beim zweiten Lauf den AK des anderen nicht zurueckdrehen.
- **Gruppenansicht FA-Liste:** [[2026-08-18-fa-liste-hierarchie-anzeige-spec]] (Testbereit) — Teil 1 hebt sie fuer die FA-Liste auf
  (Teil 1 Freigabe-Antwort 1: Sub-FA-Zeilen ganz weg, Gruppen-Zweig und `fa-liste-wiederholung.js` entfallen), die anderen Ansichten bleiben.
- **Backlog ohne Spec:** [[2026-11-17-ideal-fa-listen-und-views-anzeige-durchgang]] — Teil 1 nimmt dessen FA-Listen-Spaltenpruefung
  teilweise vorweg (Mapping-Tabelle); Kommissionierlisten, Vormontage, Beschichtung, BDE-Teileliste bleiben dort.
- **Struktur-Abgleich:** [[2026-11-17-fa-struktur-abgleich-statt-full-replace-spec]] — liefert `VerschwundenSeit` (Pruefabfragen beider Teile filtern darauf); keine Aenderung daran.

## Gemeinsame Vorab-Pruefungen (vor dem Dev-Lauf bzw. UAT; keine Entscheidung haengt mehr daran)

- Teil 1: W-1 (eine Wurzel je HauptFA), W-2 (Auftraege ohne HauptFA-Zeile), W-3 (Fuellgrad Kopffelder), S-1 (Sage IDEAL: FAZusatzinformationen-View vorhanden?).
- Teil 2: T-1 (Blaetter mit Arbeitsschritten), T-2 (Zeilen je Struktur), T-3 (mehrere Auftragskoepfe).

Testszenarien: Stand 2026-11-26 ist TS-202 die letzte Nummer im Index; je Teil ein Kapitel, der Dev-Lauf vergibt die Nummern.
