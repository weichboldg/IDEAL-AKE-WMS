---
type: changelog
version: 1.58.0
date: 2026-10-13
---
# v1.58.0 — Spaltenfilter-Suchsyntax, Etappe A (Kern, Hilfe, FA-Liste)

[[2026-09-23-suchsyntax-spaltenfilter-erweitern-spec]] (Epic, Antworten 1–10). **Direkt im Bündel**
`feature/2026-08-07-ideal-teile-1-5`, Start `d4f326ef` (v1.57.0), Commits `3fe70103`, `fe8f531c`, `6dc74c9b`, `e5d0467b`, QA `c33f010b`, Fix `10ccf1f5`.
**Keine Migration.** Umsetzung [[2026-10-13-suchsyntax-spaltenfilter-erweitern-umsetzung]]. **Etappe B offen** (Epic nicht fertig).

**Warum:** Die Spaltenfilter kannten nur „enthält“, `,` und `!` — an mindestens sechs Stellen mit eigener Bedeutung, und
Eingaben gingen unmaskiert in `LIKE`. Ziel: ein Parser, eine Bedeutung, eine Testtabelle für C# und JS.

## Kernpunkte

- Grammatik-Kern `Services/ColumnFilterSyntax.cs` (Parse/Matches, SQL-Bauer `ColumnFilterSql`, Register `ColumnFilterTypes`)
  und JS-Spiegel `wwwroot/js/column-filter-syntax.js`; gemeinsame Tabelle `IdealAkeWms.Tests/Js/column-filter-cases.json`
  (70 Fälle) gegen C#, SQL-Bauer und Node.
- `ColumnFilterHelper.Apply` (30 Aufrufer) und `table-filter.js` (alle Client-Mode-Tabellen außer Stückliste) laufen über den
  Kern — Textspalten aller dieser Listen verstehen die neue Syntax (Antwort 4).
- FA-Liste komplett: SQL-Spalten über den Bauer (kein `EF.Functions.Like` mehr), Freitext-Escaping (Antwort 8),
  „Übergeordnete Sub-FA“ gemappt (Antwort 9), Termine/Prio mit Vergleich und Bereich.
- Syntax-Hilfe („?“-Popover, `aria-describedby`), ungültige Teile rot + Text (Antwort 6), Hilfeseite neu.
- Tests Web 1936 (+91), Service 272. Zwischenzustand: Zahl/Datum nur in der FA-Liste typisiert (`data-filter-syntax="typed"`), Stückliste `legacy`.

## Deploy-Risiken

Nur Web. **Vor dem Deploy P-1 (beide Server), P-2 Kollation (jetzt Pflicht: SQL-Filter ohne `LOWER`) und P-3 Kultur.**
Anwender informieren: in Zahl-/Datumsspalten „oder“ nur noch mit `;`. Zwischenzustand bis Etappe B: Leitstand-C#-Spalten,
übrige Repositories, Stückliste und FA-Baum filtern noch mit der alten Syntax.

Merge-Commit: — (Bündel, nicht gemergt)
