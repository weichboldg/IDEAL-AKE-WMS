# WMS Second Brain - Dashboard

## Pipeline

### Backlog offen (noch keine Spec verweist darauf)
```dataview
LIST
FROM "backlog"
WHERE file.name != "README"
  AND !contains(join(map(file.inlinks, (l) => l.type)), "spec")
SORT file.ctime DESC
```
> Verschwindet automatisch, sobald eine Spec mit `source_backlog: [[diese-datei]]`
> existiert - kein Verschieben noetig. Wird ein Eintrag hier NICHT ausgeblendet,
> obwohl es eine Spec gibt, fehlt in der Spec der `source_backlog`-Wikilink.

### Backlog verarbeitet (Herkunftsbelege, bleiben liegen)
```dataview
LIST
FROM "backlog"
WHERE file.name != "README"
  AND contains(join(map(file.inlinks, (l) => l.type)), "spec")
SORT file.ctime DESC
```

### Specs im Entwurf (warten auf Freigabe - Schranke 1)
```dataview
TABLE status, created, open_questions FROM "specs/entwurf" WHERE type = "spec" SORT created DESC
```

### Freigegeben / in Umsetzung / testbereit
```dataview
TABLE status, branch, updated FROM "specs/freigegeben" WHERE type = "spec" SORT updated DESC
```

## Offene Bugs
```dataview
TABLE status, severity, file.mtime AS "Geaendert" FROM "bugs" WHERE status != "behoben" SORT severity DESC
```

## Letzte ADRs
```dataview
TABLE id, status, date FROM "architektur/adr" SORT date DESC LIMIT 10
```
