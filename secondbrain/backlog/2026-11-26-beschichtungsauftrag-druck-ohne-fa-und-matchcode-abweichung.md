---
type: backlog
typ: feature
status: offen
created: 2026-11-26
quelle: "Gerald im Chat 2026-11-26: alle Befunde als Backlog, Spec nach der Schluss-QA von [[2026-11-26-buendel-1-lagerbestellung-goal-lauf]]"
standort: AKE, IDEAL
---
# Beschichtungsauftrag-Druck: „ohne FA“ bei ausgeblendeter Artnr + abweichende Matchcodes in einer Summenzeile

Hinweise H-1/H-2 aus der QA von [[2026-11-26-beschichtung-summe-leitstand-hauptfa-kommissionierauftrag-liste-teil-1-spec]] (v1.99.87).

- H-1 (vorbestehend seit v1.99.41): Das Badge „ohne FA“ sitzt in der Artnr-Zelle. Ist die Spalte Artnr. per Zahnrad ausgeblendet, fehlt es; die freie
  Zeile ist nur noch über die Reihenfolge (zuletzt) von einer gleichnamigen FA-Zeile unterscheidbar (DEV AKE BA-2026-00005).
- H-2: Weichen Matchcode oder Haupt-Artnr. innerhalb einer Summenzeile (gleiche Artnr) ab, wird das nicht gemeldet (anders als „Maße uneinheitlich“).
- Wunsch: „ohne FA“ in einer immer sichtbaren Zelle (wie „Maße uneinheitlich“ in Verpackt); prüfen, ob Matchcode-Abweichung analog gemeldet werden soll.
