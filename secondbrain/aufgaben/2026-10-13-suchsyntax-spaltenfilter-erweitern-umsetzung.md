---
typ: notiz
spec: "[[2026-09-23-suchsyntax-spaltenfilter-erweitern-spec]]"
status: Testbereit
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# Umsetzung: Spaltenfilter-Suchsyntax (Epic, Etappen A + B)

Spec [[2026-09-23-suchsyntax-spaltenfilter-erweitern-spec]] · **direkt im Bündel**. Ein Parser (C# + JS-Spiegel, gemeinsame
Testtabelle) für `7%`, `%7`, `=x`, `a...b`, Vergleiche, `(leer)`, `!`, `;`/`,`.

## Stand

- 2026-10-13: **Etappe A umgesetzt** (v1.58.0, TS-91.A), Start `d4f326ef`, Commits `3fe70103`, `fe8f531c`, `6dc74c9b`,
  `e5d0467b`, QA `c33f010b`, Fix `10ccf1f5` (QA I-1/I-2). Build 0 Fehler, Web 1936 / Service 272 grün, Node-Lauf der Testtabelle (70 Fälle) bestanden. QA: keine Critical, I-1/I-2 behoben.
- **STOPP:** Gerald prüft Etappe A am Testsystem (TS-91.A, vorher P-2 Kollation, P-3 Kultur). **Etappe B erst auf eigenen Befehl.**
- Brain bei A erledigt: ADR [[0017-ein-parser-fuer-die-spaltenfilter-syntax]], Changelog [[2026-10-13-v1-58-0-suchsyntax-spaltenfilter-etappe-a]],
  feature-map, Testindex (TS-91). **Für Etappe B offen:** additiver Nachtrag in [[0005-listen-view-pattern-mit-server-side-spaltenfilter]]
  (Zeile „Mini-Syntax: OR `,`, NOT `!`“ überholt), [[fallstricke]] §17 als erledigt + neuer Eintrag „Spaltenfilter nie selbst
  auswerten – Kern nutzen; Typ an `<th>` UND im Register“, TS-91.B.
- 2026-10-13: **Etappe B umgesetzt** (v1.58.1, TS-91.B), Start `10ccf1f5`, Commits `0e82363b`, `3816542d`, `404a1b49`. Bestätigte
  Entscheidungen (Gerald): `=` in Zahl/Datum = Wertvergleich, kein `LOWER` (P-2 je Spalte Pflicht), Zwischenzustand A
  zurückgebaut. Build 0 Fehler, Web 1998 / Service 272, Node-Lauf (84 Fälle) bestanden. Brain: ADR-0005-Nachtrag,
  [[fallstricke]] §26, Changelog [[2026-10-13-v1-58-1-suchsyntax-spaltenfilter-etappe-b]]. QA Epic → **Testbereit**, Fix `ead16fc4` (I-1: Banner „Gefilterte Ansicht“ bei Zahlvergleich, `HasAny` mit Typen), Minor M-1..M-6 in der Spec.
- Offen für die Abnahme: Laufzeitmessung AK 12 am Testsystem (TS-91.B1), P-1/P-2/P-3 vor dem Deploy.
