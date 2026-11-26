---
type: backlog
typ: bug
status: offen
created: 2026-11-26
quelle: "Gerald im Chat 2026-11-26: alle Befunde als Backlog, Spec nach der Schluss-QA von [[2026-11-26-buendel-1-lagerbestellung-goal-lauf]]"
standort: AKE, IDEAL
---
# Leitstand: Spaltenfilter Glas, Zukauf, Lackteil und Status wirken nicht

Befund F-2 aus der QA von [[2026-11-26-beschichtung-summe-leitstand-hauptfa-kommissionierauftrag-liste-teil-2-spec]], vorbestehend.

- Die Spalten haben ein Filterfeld (`data-filterable`, Keys `glass`, `purchase`, `coating-part`, `status`), aber weder SQL-Prädikat
  (`ApplyLeitstandColumnFilter` in `ProductionOrderRepository`, unbekannter Key = No-Op) noch Memory-Filter (`LeitstandMemoryText`).
- Erwartet: jede Spalte mit Filterfeld filtert (Suchsyntax wie übrige Spalten; Checkbox-Spalten z. B. ja/nein), im Nur-HauptFA-Modus auf den
  zusammengefassten Wert — oder Filterfeld weg, wo Filtern nicht sinnvoll ist. Kein stilles Nichtstun.
