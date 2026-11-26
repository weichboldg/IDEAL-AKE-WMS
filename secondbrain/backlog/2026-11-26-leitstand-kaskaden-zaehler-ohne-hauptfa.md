---
type: backlog
typ: bug
status: offen
created: 2026-11-26
quelle: "Gerald im Chat 2026-11-26: alle Befunde als Backlog, Spec nach der Schluss-QA von [[2026-11-26-buendel-1-lagerbestellung-goal-lauf]]"
standort: IDEAL
---
# Leitstand: Kaskaden-Dialog zählt die HauptFA als Sub-FA mit

Befund F-1 aus der QA von [[2026-11-26-beschichtung-summe-leitstand-hauptfa-kommissionierauftrag-liste-teil-2-spec]] (v1.99.88), vorbestehend.

- Symptom: „Alle Sub-FAs fertigmelden“ bei HauptFA 1064052 (DEV IDEAL): Bestätigungsdialog „39 Sub-FA(s)“, die Zeile daneben „+ 38 Sub-FAs“.
- Ursache laut QA: `CascadeDonePreview` zählt mit `CountByOrderNumberAsync` alle Zeilen der OrderNumber inkl. HauptFA-Zeile.
- Erwartet: Dialog und Zeile nennen dieselbe Zahl (nur Sub-FAs). Prüfen, ob die Kaskade selbst die HauptFA mitmeldet und ob das gewollt ist.
