# Backlog

Hier legst DU (oder ein Kollege) formlose Anforderungen als .md ab.
Dateiname: YYYY-MM-DD-kurzer-slug.md. Inhalt: Freitext genuegt -
der Spec-Agent macht daraus eine vollstaendige Spec in specs/entwurf/.

Eine Backlog-Datei gilt als "verarbeitet", sobald eine Spec mit
source_backlog-Verweis auf sie existiert.

## Bugs melden

Einen Bug meldest du hier genauso - als formlose Notiz. Setze im Frontmatter
`typ: bug`, dann legt der Spec-Agent zusaetzlich einen Eintrag im Register
`bugs/` an (Symptom/Reproduktion/Severity) und verlinkt beides. Beispiel:

```
---
typ: bug
---
Beim Speichern eines FA ohne Werkbank kommt eine 500er-Fehlerseite
statt einer Validierungsmeldung. Erwartet: freundlicher Hinweis.
```

So bleibt `bugs/` das Register aller bekannten Probleme, waehrend die Backlog-
Notiz die Pipeline antreibt. Ein Bug, den du nur dokumentieren (aber nicht
sofort fixen) willst, legst du direkt in `bugs/` ab - der wird dann nicht
automatisch bearbeitet.
