# BDE-Masken Etappe 1 – Teile Einzeln + BDE mobil kompakt (v1.99.73) Implementation Plan

> **For agentic workers:** Native ausgefuehrt (ein Etappen-Lauf, executing-plans).

**Goal:** Teileliste Einzeln mit AG-Status je Token, Baugruppen-Kindern als Tabellenzeilen (Filter trifft Kinder), Teile-Knopf am Matchcode, kompakter Mobil-Ansicht.

**Architecture:** Status-Ladung der FA-Struktur wandert in `FaAgStatusService.ZuordnenAsync` (eine Stelle, FA-Struktur + Teileliste).
Die Teileliste baut je Haupt-FA eine flache `FaHierarchyStructure`, ruft denselben Lauf und legt danach die Teilmeldungen
(aller Kuerzel) per `BdeTeilelisteService.TeilmeldungenAnwenden` darueber. `Bauen` bekommt die Symbole je Knoten, macht Kinder zu `TeilelisteZeile`
und behaelt Eltern bei Kind-Treffer. Mobil rein per CSS-Scope `.bde-tl-seite` (992 px).

**Spec:** `secondbrain/specs/freigegeben/2026-11-25-bde-masken-vereinheitlichung-spec.md` (Etappe 1, AK 1–10, E1-1…E1-4) + Mobil-Spec B1–B14.

## Global Constraints
- v1.99.73 (beide AppVersion.cs), TS-194, keine Migration.
- FA-Struktur-Anzeige unveraendert (Antwort 7): Teilmeldungs-Quelle nur in der Teileliste.
- Keine Kuerzel-Literale in Views/BdeTeileliste + bde-teileliste.js (Waechter). `table-responsive` bleibt (Waechter).
- site.css, site.js, table-filter.js, column-preferences.js, _Pagination.cshtml unangetastet.

## Review Focus
- Kind trifft Filter, Eltern nicht → Eltern bleibt, `Aufgeklappt`, Kind `Treffer` (Test `Filter_KindTreffer_BehaeltElternAufgeklappt`).
- „gemeldete ausblenden“ wirkt nur auf Eltern; Kinder bleiben Anzeige (Test im selben Fall).
- Teilmeldung ohne WorkOperation → erledigt (Test `Teilmeldung_OhneWo_Erledigt`).
- Token ohne WO, Kuerzel ohne Teileliste-Werkbank → „kein BDE-Arbeitsgang“; mit Teileliste-Werkbank → offen/ueberfaellig (Test `KeinWo_TeilelisteKuerzel_OffenSonstNeutral`).
- Stornierte Teilmeldung zaehlt nicht (Ladeabfrage `!IsCancelled`, wie bisher).

### Task 1: FaAgStatusService (Extraktion)
- Create `IdealAkeWms/Services/FaAgStatusService.cs`: `Task<FaAgStatusLauf?> ZuordnenAsync(IReadOnlyList<FaHierarchyStructure>)` – Namen immer, Status nur bei BdeAktiv und >0 Strukturen.
- `FaHierarchyController` ruft ihn (Ctor: workplace/holiday/businessDays/operationConfig raus, Service rein). DI in Program.cs.
- Nachweis: bestehende FaArbeitsgangStatusTests + Build.

### Task 2: TeilmeldungenAnwenden + Bauen (TDD)
- Tests in `IdealAkeWms.Tests/Services/BdeTeilelisteAgStatusTests.cs`: Teilmeldung→Erledigt, KeinWo/Teileliste-Kuerzel, Kinder als Zeilen mit Symbolen, Kind-Treffer, Pagination-Grundlage (Zeilen = nur Eltern).
- `TeilelisteZeile`: `Kinder` → `IReadOnlyList<TeilelisteZeile>`, neu `Arbeitsgaenge`, `Treffer`, `Aufgeklappt`. Getter `ag-status` = AriaLabels.

### Task 3: Views/CSS/JS
- Partial `Views/Shared/_FaAgSymbole.cshtml` (Model `IReadOnlyList<FaArbeitsschrittSymbol>`), genutzt von `_FaHierarchyNode` und Teileliste.
- Index: Spalte ag-status, data-col-key + data-label an jeder Zelle, Teile-Knopf in Matchcode-Zelle, Kinderzeilen `tr.bde-tl-kind collapse`.
- _Kopf: Filterkarte collapse < 992 px mit Kurzfassung, 44 px, `<details>`. bde.css Scope-Regeln. bde-teileliste.js: Kinder nach `table-sorted` zurueck.
- ColumnDefinitions + column-config JSON um ag-status.

### Task 4: Version, Doku, TS-194, Brain, Commits.
