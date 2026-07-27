# WMS Second Brain - Dashboard

## Pipeline

### Backlog (noch ohne Spec)
```dataview
LIST FROM "backlog" SORT file.ctime DESC
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
