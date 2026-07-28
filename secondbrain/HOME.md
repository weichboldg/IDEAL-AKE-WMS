# WMS Second Brain - Dashboard

## Aktive Arbeit (noch nicht Gemerged)
```dataviewjs
// Alle Specs, die noch nicht durch sind - nach Pipeline-Stufe sortiert.
const rang = { "Entwurf":1, "Freigegeben":2, "InUmsetzung":3, "In Umsetzung":3, "Testbereit":4 };
const aktiv = dv.pages('"specs"')
  .where(p => p.type == "spec" && p.status != "Gemerged")
  .sort(p => rang[p.status] ?? 99, 'asc');
if (aktiv.length) {
  dv.table(["Spec", "Status", "Branch", "Offene Fragen"],
    aktiv.map(p => [
      p.file.link,
      p.status,
      p.branch || "\u2014",
      (p.open_questions && p.open_questions.length) ? p.open_questions.length : "\u2014"
    ]));
} else {
  dv.paragraph("_Alles gemerged \u2014 nichts in Arbeit._");
}
```
> Was ist noch nicht fertig. Entwurf = wartet auf Freigabe (Schranke 1),
> Testbereit = wartet auf manuellen Test + Merge (Schranke 2).

## Pipeline

### Backlog offen (noch keine Spec verweist darauf)
```dataviewjs
// Robust gegen Namensgleichheit + Wikilink-Feinheiten:
// gleicht Backlog-Dateinamen direkt gegen das source_backlog-Feld ALLER Specs ab.
const specs = dv.pages('"specs"').where(p => p.type == "spec");
const verwiesen = new Set();
for (const s of specs) {
  let sb = s.source_backlog;
  if (!sb) continue;
  // sb kann Link oder String sein -> auf reinen Dateinamen normalisieren
  const name = (sb.path ?? String(sb))
    .replace(/^.*[\/\\]/, "").replace(/\.md$/, "").replace(/[\[\]]/g, "").split("|")[0].trim();
  verwiesen.add(name);
}
const offen = dv.pages('"backlog"')
  .where(p => p.file.name != "README" && !verwiesen.has(p.file.name))
  .sort(p => p.file.ctime, 'desc');
if (offen.length) dv.list(offen.map(p => p.file.link));
else dv.paragraph("_Keine offenen Backlog-Eintraege._");
```
> Zeigt Backlog-Dateien, auf die (noch) keine Spec via `source_backlog` verweist.
> Robust gegen Namensgleichheit Backlog/Spec und gegen Wikilink- vs. String-Form.

### Backlog verarbeitet (Herkunftsbelege, bleiben liegen)
```dataviewjs
const specs = dv.pages('"specs"').where(p => p.type == "spec");
const verwiesen = new Set();
for (const s of specs) {
  let sb = s.source_backlog;
  if (!sb) continue;
  const name = (sb.path ?? String(sb))
    .replace(/^.*[\/\\]/, "").replace(/\.md$/, "").replace(/[\[\]]/g, "").split("|")[0].trim();
  verwiesen.add(name);
}
const erledigt = dv.pages('"backlog"')
  .where(p => p.file.name != "README" && verwiesen.has(p.file.name))
  .sort(p => p.file.ctime, 'desc');
if (erledigt.length) dv.list(erledigt.map(p => p.file.link));
else dv.paragraph("_Noch nichts verarbeitet._");
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
