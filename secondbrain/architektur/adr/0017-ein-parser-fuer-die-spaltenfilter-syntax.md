---
type: adr
id: 0017
title: Ein Parser für die Spaltenfilter-Syntax — C#-Kern, JS-Spiegel, eine Testtabelle
status: accepted
date: 2026-10-13
supersedes: ""
superseded_by: ""
---

## Kontext und Problem

Die Spaltenfilter ([[0005-listen-view-pattern-mit-server-side-spaltenfilter]]) kannten nur „enthält“, `,` (ODER) und `!`
(NICHT). Die Erhebung der Spec [[2026-09-23-suchsyntax-spaltenfilter-erweitern-spec]] fand diese Syntax an **mindestens sechs
Stellen mit eigener Bedeutung** umgesetzt: `ColumnFilterHelper.Parse/Apply`, `table-filter.js` `matchesFilter`, je eigene
`EF.Functions.Like`-/`Contains`-Bauten in fünf Repositories, C#-Postfilter in drei Controllern, `bomMatchesFilter` in der
Stückliste und ein Baum-Filter ohne Syntax. Fehlteile werteten `!` sogar je Wert statt für alle Werte aus, und Eingaben gingen
unmaskiert in `LIKE` (`_` war Platzhalter). Eine neue Syntax (`7%`, `%7`, `=x`, `a...b`, `>`/`<`, `(leer)`) an einer Stelle
einzubauen hätte die Listen je nach Filterweg noch weiter auseinanderlaufen lassen.

## Betrachtete Optionen

- **Syntax je Stelle nachrüsten** — kleinster Einzeldiff, aber sechs (und mehr) Bedeutungen, keine gemeinsame Prüfung.
- **Nur serverseitig parsen, Client schickt alles an den Server** — Client-Mode-Listen (unpaginiert, live) und Stückliste
  müssten umgebaut werden; Druck einer Client-Liste filtert trotzdem serverseitig.
- **Ein Kern in C#, ein Spiegel in JS, beide gegen dieselbe Testtabelle** — gewählt.

## Entscheidung

- Die Grammatik lebt **einmal** in C#: `Services/ColumnFilterSyntax.cs` — `ColumnFilterSyntax.Parse(raw, type)` (strukturierte
  Teile, Negation, ungültige Teile mit Grund) und `.Matches` (Speicher-Auswerter); `ColumnFilterSql` baut daraus EF-Ausdrücke
  (`string.Contains/StartsWith/EndsWith/==` auf Konstanten, EF maskiert Platzhalter; kein `EF.Functions.Like`, kein `ToLower()`
  auf Spalten).
- Der Browser nutzt `wwwroot/js/column-filter-syntax.js`, eingebunden im Layout vor allen Seiten-Skripten.
- **Beide** Kerne prüfen dieselbe Tabelle `IdealAkeWms.Tests/Js/column-filter-cases.json` (C#-Theory + Node-Lauf als
  `SkippableFact`). Eine Regeländerung heißt: Tabelle erweitern, beide Kerne anpassen, beide Läufe grün.
- Listen, Repositories und Views werten Filterwerte **nicht selbst** aus; sie geben Spalten/Selektoren und den Spaltentyp
  (Text/Zahl/Datum: `<th data-date-filter>` bzw. `data-filter-type="number"` im Browser, Register `ColumnFilterTypes` auf dem
  Server) an den Kern.
- Ungültige Teile sind wirkungslos und werden im Browser sichtbar markiert — gleich auf beiden Seiten.

## Konsequenzen

- Gut: eine Bedeutung je Eingabe in allen Listen; Client- und Server-Treffer gleich (belegt durch die Tabelle); kein Platzhalter-
  Durchschlag mehr in `LIKE`.
- Gut: „beginnt mit“ bleibt indexfähig (kein `LOWER(...)` im SQL).
- Schlecht: Groß/Klein-Unabhängigkeit der SQL-Wege hängt an der CI-Kollation der Datenbank (Prüfschritt P-2); EF-InMemory
  vergleicht ordinal — Tests mit SQL-Filtern brauchen Testdaten in passender Schreibweise.
- Schlecht: Zwei Implementierungen (C#, JS) bleiben nötig; die gemeinsame Tabelle ist die einzige Klammer — sie darf nicht
  übersprungen werden (Node-Lauf im QA-Nachweis belegen).
- Umsetzung in zwei Etappen: Etappe A (v1.58.0) Kern + FA-Liste + alle `Apply`-/Client-Listen für Textspalten; Etappe B die
  übrigen eigenen Filterwege, Typmarkierung aller Listen und Entfernen der alten `Parse`-Signatur.
