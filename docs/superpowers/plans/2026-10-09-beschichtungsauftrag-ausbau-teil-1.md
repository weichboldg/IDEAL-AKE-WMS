# Beschichtungsauftrag Teil 1 — Summierte Ansicht Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Zweite, standardmaessige Sicht „Summiert" des IDEAL-Beschichtungsauftrags (je HauptFA + Artnr summierte Sollmenge, Maße mit Invarianten-Warnung) inkl. Druck/PDF.

**Architecture:** Wiederverwendung des Kommissionier-Summiert-Musters (v1.47.0): statische, DB-freie `AggregateSummiert` im `BeschichtungsauftragService`, eigene `SummiertColumnMap`, neue Controller-Actions `Summiert`/`PrintSummiert`/`PdfSummiert`, neue Views. Die Sichtbare-Spalten-Aufloesung des Kommissionier-Controllers wird in einen gemeinsamen statischen Helfer verschoben (eine Implementierung, zwei Aufrufer). Kopfdaten-Markup als Partial, das Index/Print und Summiert/PrintSummiert teilen (Ausgabe der Einzelsicht unveraendert).

**Tech Stack:** ASP.NET Core 10 MVC, Razor, Bootstrap 5, xUnit/FluentAssertions/Moq.

**Spec:** `C:\git\IDEAL-AKE-WMS\secondbrain\specs\freigegeben\2026-10-06-beschichtungsauftrag-ausbau-teil-1-spec.md`

## Global Constraints

- Basis Buendel `feature/2026-08-07-ideal-teile-1-5` (v1.47.5); Version **1.48.0** in beiden `AppVersion.cs`.
- Summierschluessel `HauptFA` + `Artnr` (getrimmt, wie Kommissionier). Maße nur Anzeige; abweichend ⇒ Werte der ersten Position (Sortierung `Position`, dann `SubFA`) + sichtbarer Text „Maße uneinheitlich" (Bildschirm und Druck) + `ILogger`-Warnung mit HauptFA und Artnr. `NULL` = `0` (Sync `FaHierarchySyncService.cs:302`).
- Nur `Beschichtet == true`, alle Ebenen. Summe nur `Sollmenge`, plus Positionsanzahl.
- Gruppen-Paging je HauptFA, `TotalCount` = Gruppen. ADR 0005 (Filterkarte, `data-server-column-filter`, `data-col-key`, `_Pagination`).
- Navigation `_Layout.cshtml` → `asp-action="Summiert"`. Umschalter „Liste | Summiert" in beiden Sichten; geteilte Keys ohne `colf_sollmenge`.
- Einzelliste, Druck, PDF unveraendert (AK 11).
- PDF-Dateiname `Beschichtungsauftrag_<HauptFA>_<yyyyMMdd-HHmm>.pdf` (`PdfFileNameBuilder.Build("Beschichtungsauftrag", …)`), Fehler → `WarningMessage` + Redirect `Summiert`.
- Zahnrad-Host (fallstricke §19): leerer `<div class="d-flex justify-content-end mb-1"></div>` direkt vor `.table-responsive` in `Summiert.cshtml`.
- UI-Texte Deutsch, Code Englisch; frontend-design: Konsistenz zu `FaHierarchyKommissionierListen/Summiert.cshtml`.
- `docs/TESTSZENARIEN.md` nach Bearbeitung byte-genau LF pruefen (fallstricke §21).

## Review Focus

1. Positionen ohne `Artnr` (leer/NULL) werden je HauptFA zu EINER Zeile zusammengefasst (Kommissionier-Praezedenz) — Test pinnt das Verhalten, Spec-Befund meldet es.
2. Maße-Abweichung nur in einer Dimension (z. B. nur Tiefe) muss ebenfalls flaggen — Test.
3. Spaltenfilter auf „Positionen" und „Summe Sollmenge" filtern Aggregatwerte, nicht Einzelwerte — Test.
4. HauptFA ohne OrderInfo: Kopf zeigt „Keine Auftrags-Kopfdaten", kein Absturz — Test (OrderInfos-Map leer).
5. PDF fuer HauptFA, den der Spaltenfilter ausblendet → `NotFound` — Controller-Test.

---

### Task 1: Aggregation (Service + ViewModels)

**Files:**
- Modify: `IdealAkeWms/Models/ViewModels/FaHierarchyBeschichtungViewModels.cs`
- Modify: `IdealAkeWms/Services/BeschichtungsauftragService.cs`
- Modify: `IdealAkeWms/Services/KommissionierListenService.cs` (Overload `BuildToggleQuery(request, keys)`)
- Test: `IdealAkeWms.Tests/Services/BeschichtungsauftragServiceTests.cs`

**Interfaces – Produces:**
```csharp
public class FaHierarchyBeschichtungSummiertRowViewModel {
  int HauptFA; string? HauptArtnr; string Artnr; string? Matchcode;
  decimal SummeSollmenge; int PositionCount; decimal Breite, Hoehe, Tiefe; bool MasseUneinheitlich; }
public class FaHierarchyBeschichtungSummiertResult {
  List<FaHierarchyBeschichtungSummiertRowViewModel> Rows; int TotalRowCount; int TotalGroupCount;
  IReadOnlyDictionary<int, List<FaHierarchyOrderInfo>> OrderInfos; }   // nur HauptFA der Rows
public class FaHierarchyBeschichtungSummiertViewModel { Rows; OrderInfos; Pagination; PdfVerfuegbar; }
public class FaHierarchyBeschichtungSummiertPrintViewModel { Rows; OrderInfos; Barcodes; IsFiltered; VisibleColumns; }
static BeschichtungsauftragService.SummiertColumnMap  // hauptfa, hauptartnr, artnr, matchcode, breite, hoehe, tiefe, positionen, sollmenge
static FaHierarchyBeschichtungSummiertResult AggregateSummiert(IEnumerable<FaHierarchyNode>, IEnumerable<FaHierarchyOrderInfo>, IReadOnlyDictionary<string,string>? columnFilters, int page, int pageSize, bool paginate)
Task<FaHierarchyBeschichtungSummiertResult> BuildSummiertAsync(columnFilters, page, pageSize, paginate)  // loggt Maße + Mehrdeutigkeit
static string[] ToggleSharedQueryKeys = ["colf_hauptfa","colf_hauptartnr","colf_artnr","colf_matchcode","colf_breite","colf_hoehe","colf_tiefe"]
static string BuildToggleQuery(HttpRequest?)
KommissionierListenService.BuildToggleQuery(HttpRequest?, IEnumerable<string> keys)
```

- [ ] Step 1: Tests schreiben (rot): Zusammenfassen (Summe, PositionCount 2), andere Artnr → 2 Zeilen, HauptFA-Grenze, nur Beschichtet + SubFA≠0 zaehlt, Maße gleich → kein Flag, Maße in nur einer Dimension abweichend → Flag + Werte der ersten Position (nach `Position`), leere Artnr fasst zusammen (Review Focus 1), Spaltenfilter `sollmenge` auf Summe, `positionen` auf Anzahl, Gruppen-Paging (TotalGroupCount, Seite 2), OrderInfos nur fuer Seiten-HauptFA, `BuildSummiertAsync` loggt Warnung mit HauptFA+Artnr bei Maße-Flag.
- [ ] Step 2: `dotnet test --filter BeschichtungsauftragServiceTests` → Kompilierfehler/rot.
- [ ] Step 3: Implementieren (Code in Task-Umsetzung: GroupBy `(HauptFA, Artnr.Trim())`, `ordered = g.OrderBy(Position ?? int.MaxValue).ThenBy(SubFA)`, `first = ordered.First()`, `MasseUneinheitlich = g.Any(n => n.Breite != first.Breite || n.Hoehe != first.Hoehe || n.Tiefe != first.Tiefe)`; Sortierung HauptFA, Artnr; `ColumnFilterHelper.Apply`; Gruppen-Paging wie Kommissionier `:197-203`; OrderInfos `allInfos.Where(i => pageHauptFas.Contains(i.HauptFA)).GroupBy(HauptFA)`). `LogAmbiguities` intern auf `LogAmbiguity(int hauptFa, int count)` umstellen, Summiert ruft sie je HauptFA mit >1 OrderInfo.
- [ ] Step 4: Tests gruen. Step 5: Commit `wip: …-teil-1 Aggregation`.

### Task 2: Gemeinsame Sichtbare-Spalten-Aufloesung + Controller

**Files:**
- Create: `IdealAkeWms/Services/PrintVisibleColumns.cs` (statisch, Inhalt 1:1 aus `FaHierarchyKommissionierListenController.ResolveVisibleColumnsAsync`)
- Modify: `IdealAkeWms/Controllers/FaHierarchyKommissionierListenController.cs` (privates `ResolveVisibleColumnsAsync` delegiert)
- Modify: `IdealAkeWms/Controllers/FaHierarchyBeschichtungController.cs` (+ `IUserViewPreferenceRepository`, Actions `Summiert`, `PrintSummiert`, `PdfSummiert`)
- Modify: `IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs` (`FaHierarchyBeschichtungSummiert` + Switch)
- Test: `IdealAkeWms.Tests/Controllers/FaHierarchyBeschichtungControllerTests.cs` (neu)

```csharp
public static Task<IReadOnlyList<string>> ResolveAsync(string? visibleColumns, string viewKey,
    IReadOnlyList<ColumnDef> defs, ICurrentUserService user, IUserViewPreferenceRepository prefs)
```
- [ ] Tests (rot): `Summiert_PaginationCountsGroups`, `PrintSummiert_UsesSummiertViewKey`, `PdfSummiert_UnknownHauptFa_NotFound`, `PdfSummiert_RenderFails_RedirectsToSummiert` (View-Name `PrintSummiert` gepinnt), `PdfSummiert_FileName_StartsWithBeschichtungsauftrag`.
- [ ] Implementieren, bestehende `FaHierarchyKommissionierListenControllerTests` muessen unveraendert gruen bleiben. Commit.

### Task 3: Views, Navigation, Waechter

**Files:**
- Create: `Views/FaHierarchyBeschichtung/Summiert.cshtml`, `PrintSummiert.cshtml`, `_BeschichtungKopf.cshtml` (Bildschirm-Kopfzeile, aus `Index.cshtml:75-101`), `_BeschichtungKopfDruck.cshtml` (Druck-Kopf, aus `Print.cshtml:70-112`)
- Modify: `Index.cshtml` (Umschalter + Partial), `Print.cshtml` (Partial), `Views/Shared/_Layout.cshtml:118`
- Test: `IdealAkeWms.Tests/Views/BomZahnradHostTests.cs` (+ `FaHierarchyBeschichtungSummiertZahnradHostTests`), `IdealAkeWms.Tests/Views/KommissionierSpaltenKonsistenzTests.cs` (ViewPairs um Ordner erweitern, Beschichtung-Summiert + `SummiertColumnMap` aufnehmen)
- [ ] frontend-design-Skill aufrufen. Tests zuerst (rot: Datei fehlt), dann Views. „Maße uneinheitlich" als `badge text-bg-warning` in der (gesperrten, immer sichtbaren) Matchcode-Zelle, im Druck als Text in der Matchcode-Zelle. Leerfall-Hinweis. `data-print-link="FaHierarchyBeschichtungSummiert"` + `print-visible-columns.js`.
- [ ] Build + Tests gruen. Commit.

### Task 4: Version, Doku, Testszenarien

- Version 1.48.0 (beide `AppVersion.cs`), `Help/Changelog.cshtml`-Karte, `Help/Index.cshtml` Abschnitt „IDEAL: Beschichtungsauftrag — Summiert als Standard (seit v1.48.0)", `docs/TESTSZENARIEN.md` neues Kapitel TS-81 (LF pruefen), Commit `wip: …-teil-1 feature-complete`.
- Brain im Hauptcheckout: testszenarien-index, changelog, feature-map, Aufgabe, Spec (QA-Nachweis via qa-agent, Status Testbereit).
