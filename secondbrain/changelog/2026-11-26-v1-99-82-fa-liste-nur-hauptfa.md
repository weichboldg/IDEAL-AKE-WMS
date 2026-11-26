---
type: changelog
version: 1.99.82
date: 2026-11-26
---
# v1.99.82 — IDEAL: FA-Liste nur HauptFA, Strukturfelder, Knopf FA-Struktur

Spec [[2026-11-26-fa-liste-nur-hauptfa-und-fa-struktur-neu-teil-1-spec]], Lauf-Notiz
[[2026-11-26-fa-liste-hauptfa-fa-struktur-goal-lauf]]. Worktree `feature/2026-08-07-ideal-teile-1-5`, Commit `557a9062`.
**Keine Migration, keine neue Einstellung.** TS-203. Nicht gemergt.

## Kernpunkte

- `ProductionOrderRepository.GetForLeitstandGroupedAsync(..., nurHauptFa)`: je Auftrag genau die HauptFA-Zeile; die FA-Nr.-Karte
  findet den HauptFA auch über eine Sub-FA-Nummer. Banner zählt Aufträge ohne HauptFA-Zeile (`CountGruppenOhneHauptFaAsync`).
- Gruppenansicht der FA-Liste entfernt (`fa-liste-gruppierung.js`/`fa-liste-wiederholung.js` nicht mehr eingebunden bzw. gelöscht).
- 17 Strukturfelder aus `FaHierarchyOrderInfos` (Kopf) und Wurzelknoten als Spalten, `DefaultHidden`; Postfilter je Typ.
- Matchcode: FA-Struktur-Wurzel zuerst, sonst Artikelstamm (wie Stückliste). Kombigerät: Kopffelder leer, Zeichen „Kombigerät“.
- Knopf „FA-Struktur“ je Zeile.

## Deploy-Risiken

- Kein Schema. Gespeicherte Spalteneinstellungen bleiben; neue Spalten sind ausgeblendet.
- FA-Nr.-Spaltenfilter (LIKE-Prädikat) nur per Manual-UAT TS-203.2 prüfbar (InMemory).
