# Spaltenfilter-Suchsyntax — Etappe A Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:executing-plans (native, ein Lauf). Steps use checkbox syntax.

**Goal:** EIN Parser (C# + JS-Spiegel, gemeinsame Testtabelle) für `7%`, `%7`, `=x`, `a...b`, `>`/`<`/`>=`/`<=`, `(leer)`,
`!`, `;`/`,`; SQL ohne unmaskierte Platzhalter; Referenzliste FA-Liste; Syntax-Hilfe. Etappe B nicht.

**Architecture:** `ColumnFilterHelper.ParseFilter(raw, type)` → `ParsedColumnFilter` (Negate, Parts, Invalid);
`ColumnFilterHelper.Matches(cell, parsed, type)` (Speicher); `Apply` nutzt beide (optional Typen, Standard Text) — die 30
Aufrufer bleiben unverändert (Antwort 4). `ColumnFilterSql.Predicate<T>(parsed, selectors…)` baut Ausdrucksbäume mit
`string.Contains/StartsWith/EndsWith/==` auf Konstanten (EF maskiert), Null-Guards, `(leer)` = alle leer; `Splice` für die
Kunde-Subquery. Alte `Parse`-Signatur bleibt bis B. JS `column-filter-syntax.js` (UMD) gleiche Logik; `table-filter.js`
nutzt ihn mit Typ aus `<th>`, markiert Ungültiges, „?“-Popover. FA-Liste: S1 (Repository), S1f (Escaping), M1 (Controller),
`parent-sub-order-number`, `data-filter-type="number"` an Prio, Register `ColumnFilterTypes.FaListe`.

**Spec:** `C:\git\IDEAL-AKE-WMS\secondbrain\specs\freigegeben\2026-09-23-suchsyntax-spaltenfilter-erweitern-spec.md`

## Global Constraints

- Direkt im Bündel, Start `d4f326ef` (v1.57.0) → **1.58.0**, **TS-91 (Unterkapitel A)**; keine Migration; Web-App nicht starten.
- Status bleibt **InUmsetzung**, Etappe B nicht anfangen.
- Kein `EF.Functions.Like` mit Benutzereingabe in `ProductionOrderRepository`; kein `ToLower()` auf Spalten im neuen SQL-Bauer.

## Review Focus

1. Alte Eingaben `960,886`, `!960` in Textspalten unverändert.
2. `!x` trifft leere Zellen; `!(leer)` nicht.
3. Null-Navigation (Werkbank, ExtraInfo) unter InMemory und SQL.
4. Datum `=T`/Vergleich tageweise; `kw42`/`10.2026` weiter „enthält“.
5. Ungültig → wirkungslos auf beiden Seiten, rot im Browser auch nach Reload.

### Task 1: C#-Kern (Parse, Matches, Apply) + JSON-Tabelle + Theory
### Task 2: JS-Spiegel + Node-Check + SkippableFact
### Task 3: SQL-Bauer + FA-Liste (S1, S1f, M1, Sub-FA) + ToQueryString-Tests
### Task 4: table-filter.js (Kern, Typ, Ungültig, Hilfe), FA-Liste-View, Layout-Einbindung (frontend-design)
### Task 5: Hilfeseite, Version, Changelog, TS-91 A, Brain, QA
