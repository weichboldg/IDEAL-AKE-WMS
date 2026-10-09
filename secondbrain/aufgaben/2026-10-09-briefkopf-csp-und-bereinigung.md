---
typ: aufgabe
status: offen
erstellt: 2026-10-09
---
# Briefkopf: CSP-Gürtel und Grenzen der Regex-Bereinigung

Aus dem QA-Lauf zu [[2026-10-06-beschichtungsauftrag-ausbau-teil-2-spec]] (Befund M-1, v1.49.0). **Nicht blockierend** —
innerhalb der dokumentierten `ponytail:`-Grenze (Antwort 11: Schutz vor Versehen, Pfad nur Admin, Vorlage von der IT).

**Belegte Umgehungen mit absichtlich gebautem Markup** (QA-Proben gegen `BriefkopfVorlageService.Load`):
- `<scr<meta>ipt>` wird nach dem Entfernen von `<meta>` zu `<script>` zusammengesetzt.
- Attribute ohne Leerzeichen davor (`<div/onclick=…>`, `href="x"onclick=…`) bleiben stehen.
- Entity-kodiertes `&#106;avascript:` wird nicht erkannt.
- Fremdladen über `<img/src=https…>`, `alt="a>b"` vor `src`, Attribute `background`/`poster`, CSS `image-set()`.

**Billigster Gürtel:** eine CSP-`<meta>` in `FaHierarchyBeschichtung/Print.cshtml` und `PrintSummiert.cshtml`, z. B.
`default-src 'none'; img-src data:; font-src data:; style-src 'unsafe-inline'; script-src 'unsafe-inline'` (die eigenen
`onclick`-Knöpfe der Druckansicht brauchen Inline-Script; prüfen, ob Headless Edge die CSP auch beim PDF anwendet).
Sperrt Fremdladen browserseitig unabhängig von der Bereinigung.

**Upgrade-Pfad**, falls die Vorlage je aus Anwender-Hand kommt: HtmlSanitizer (AngleSharp) statt Regex (Antwort 11 B).

Entscheidung beim Menschen.

## Ergänzung QA-Nachlauf (M-11)

Seit `020bf8af` fallen offener Kommentar, kaputtes `</style …>`, offene Rawtext-Elemente und eine ungleiche
`<div>`/`</div>`-Zahl auf den Ersatz-Kopf (mit Hinweis). Weiter **ohne** Hinweis durch: gleiche div-Zahl in falscher
Reihenfolge (`</div><div>a` schließt die `.fa-group` trotzdem), offenes `<table>` (zieht den Rest in die Tabelle),
offenes `<a>`/`<b>` (Rest wird Link/fett). Billigster nächster Schritt: div-Tiefe darf nie unter 0 fallen, `table`/`a`/`b`
mitzählen; vollständig nur mit HTML-Parser (heute keine Abhängigkeit).
