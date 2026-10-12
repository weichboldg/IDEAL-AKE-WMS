# FA-Struktur Arbeitsgänge als Symbole — Teil 1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Sage-Kürzel aus `FaHierarchyNode.Arbeitsschritte` als neutrale Symbole je Baumzeile in `/FaHierarchy`, Werkbank-Name per `aria-label`/`title`.

**Architecture:** Reine Funktion `FaArbeitsschrittSymbole` (Tokenisierung wie `WorkOperationStructureDetectionService`, Namens-Lookup aus dem Werkbank-Katalog, Zuordnung an die `FaHierarchyTreeNode`s der Seite). `FaHierarchyController.Index` ruft einmal `IProductionWorkplaceRepository.GetAllOrderedAsync` und weist zu. Neue Baumspalte `operations` (Kopf, Filterzeile, `data-f-operations`, column-config, `ColumnDefinitions.FaHierarchyStructure`), `colspan` 8 → 9. Modal unverändert.

**Tech Stack:** ASP.NET Core 10 MVC/Razor, Bootstrap 5, site.css, xUnit/FluentAssertions.

**Spec:** `C:\git\IDEAL-AKE-WMS\secondbrain\specs\freigegeben\2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-1-spec.md`

## Global Constraints

- Direkt im Bündel, Start `5764c664`; Version **1.52.0**; **TS-85**; keine Migration; Web-App nicht lokal starten.
- Quelle S, Feld-Reihenfolge, kein Dedupe, Ausschlussliste NICHT filtern, Service-Key nicht lesen; keine `WorkOperation`-Abfrage zusätzlich zum bestehenden Existenz-Check; höchstens eine Katalog-Abfrage.
- Symbol = sichtbares Kürzel; Name in `aria-label` + `title`; ohne eindeutigen Treffer (unbekannt/mehrdeutig) Kürzel als `aria-label`; alle Symbole gleich (keine Statusfarbe); WCAG AA (Text ≥ 4,5:1, Kontur ≥ 3:1); Umbruch statt Querscrollen; leeres Feld → Strich.
- Modal (`_FaWorkOperationsModal.cshtml`, `FaWorkOperationsViewModel`, Endpunkt `WorkOperations`) ohne Diff.

## Review Focus

1. Feld mit Tab/Doppel-/Randleerzeichen → keine leeren Symbole (Trennung nur Leerzeichen wie die Erkennung; Tab bleibt Teil des Tokens — wie Sage/Erkennung). Test `Tokenize_*`.
2. Kürzel in anderer Groß-/Kleinschreibung bzw. mit Leerzeichen im Katalog → Name trotzdem gefunden. Test `NamenLookup_TrimUndCaseInsensitiv`.
3. Kürzel auf mehreren Werkbänken → kein Name (kein erfundener). Test `NamenLookup_Mehrdeutig_OhneNamen`.
4. Sub-FA-Zeile zeigt nicht die Kürzel der Kinder; Blatt zeigt eigene. Test `Zuordnen_JeZeileNurEigeneKuerzel`.
5. Spaltenquellen (th Kopf, th Filterzeile, column-config, ColumnDefinitions) und `colspan` driften nicht. Test `FaHierarchyStrukturSpaltenTests`.

### Task 1: Funktion + Controller + Tests
`Models/ViewModels/FaHierarchyTreeViewModel.cs`: `FaArbeitsschrittSymbol(Kuerzel, Name?)`, `FaArbeitsschrittSymbole.Tokenize/NamenLookup/Zuordnen`, `FaHierarchyTreeNode.Arbeitsgaenge`. `FaHierarchyController`: Ctor `IProductionWorkplaceRepository`, Index weist zu. Tests `FaArbeitsschrittSymboleTests`.

### Task 2: View + CSS + Spaltenregistrierung + Wächter
frontend-design; `_FaHierarchyNode.cshtml` Zelle + `data-f-operations`; `Index.cshtml` th/Filter/colspan/column-config; `ColumnDefinitions.FaHierarchyStructure`; `site.css` `.fa-ag-symbol`. Test Spaltenquellen.

### Task 3: Doku, Version, TS-85, Brain, QA
