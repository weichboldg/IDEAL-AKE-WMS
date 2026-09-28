# Kommissionierliste: Summiert als Standard, Druck/PDF nur sichtbare Spalten — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Die IDEAL-Summiert-Ansicht der Kommissionierlisten wird Standard-Einstieg (ohne KW-Pflicht, mit Matchcode/Hauptlagerplatz, Gruppen-Paging je HauptFA, eigenem Druck/PDF), und Druck/PDF beider Ansichten zeigen genau die beim Klick sichtbaren Spalten.

**Architecture:** Alles im Web-Projekt. Service `KommissionierListenService` (reine `AggregateSummiert` erweitert, Umschalter-Query-Helfer), `ColumnDefinitions`-Eintrag, Controller mit `ResolveVisibleColumnsAsync` (Query-Parameter `visibleColumns`, Rückfall auf gespeicherte Präferenz), Views mit `ShowCol` (PrintBom-Muster), kleines neues JS `print-visible-columns.js`. `BuildFlagPredicate` bleibt unverändert.

**Tech Stack:** ASP.NET Core 10 MVC, EF Core 10, xUnit + FluentAssertions + Moq, Razor + Vanilla-JS, Bootstrap 5.

**Spec:** `C:\git\IDEAL-AKE-WMS\secondbrain\specs\freigegeben\2026-09-25-kommissionierliste-summierung-pdf-spec.md` (Hauptcheckout; Rumpf + `affected_code` = Auftrag; bei Widerspruch gilt **FREIGABE-ANTWORT 5** am Dateiende).

Arbeitsverzeichnis für ALLE Befehle: `C:\git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5`. Niemals in `secondbrain/` schreiben. Nie `git add -A`; `CLAUDE.md` ist lokal geändert — nicht anfassen/stagen. Commit-Trailer (zwei Zeilen, als zweites/drittes `-m`):
`Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>` und `Claude-Session: https://claude.ai/code/session_01Q6Td8tbiqPyZv1nhXwnDdK`.

## Global Constraints

- Version `1.46.0` → `1.47.0` (beide `AppVersion.cs`). Testszenarien-Kapitel **TS-80**. Keine Migration, kein SQL, keine Service-Änderung (außer Versions-Konstante).
- `KommissionierListenService.BuildFlagPredicate` NICHT ändern.
- `WarehousePickingPrintLayout` NICHT ändern (keine Überladung; nur `ParsePrefs` lesen).
- `wwwroot/js/table-filter.js` und `column-preferences.js` NICHT ändern.
- Test `Summiert_NoKw_ReturnsEmpty_NoImplicitTotal` wird UMGEKEHRT (umbenannt + neue Assertion), nicht gelöscht. **Alle übrigen bestehenden Tests laufen ohne Fixture-Änderung grün** — muss eines angepasst werden: STOP und melden.
- Umschalter-Keys (EINE Stelle, `KommissionierListenService.ToggleSharedQueryKeys`): `target, kwVon, kwBis, colf_hauptfa, colf_artnr, colf_matchcode, colf_kommissionieren, colf_hauptlagerplatz` — **ohne** `colf_sollmenge`. KW wirkt in der Liste NICHT (Freigabe-Antwort 5, reiner Pass-Through).
- Gruppen-Paging Summiert: Seiteneinheit = HauptFA (Muster `FaHierarchyListBuilder.Build` Schritt 4); `Pagination.TotalCount = TotalGroupCount`; `TotalRowCount` bleibt.
- Druckspalten: `visibleColumns` gesetzt → direkt `Split(',')`; fehlt/leer → gespeicherte Präferenz (`IUserViewPreferenceRepository.GetByUserAndViewAsync` + `WarehousePickingPrintLayout.ParsePrefs` + `defs.Where(d => d.Locked || !hidden.Contains(d.Key))`); keine Präferenz/kein User → leere Liste = alle Spalten.
- `PdfSummiert`-Fehlerpfad: `RedirectToAction(nameof(Summiert), new { target, kwVon, kwBis })` + `TempData["WarningMessage"]`.
- Beide Menü-Links in `_Layout.cshtml` (Z. 184 + Z. 203-204) → `asp-action="Summiert"`.
- Kein Filterwert/Freitext per `'@Html.Raw(...)'` in ein JS-Literal; Werte in `<script>` nur als JSON (`fallstricke` §17).
- UI-Texte deutsch; exakt: Banner „Summierte Ansicht“; Summiert-Spaltenlabels `HauptFA`, `Artnr.`, `Matchcode`, `Kommissionieren`, `Hauptlagerplatz`, `Summe Sollmenge`.
- TempData nur `SuccessMessage`/`WarningMessage`.
- Build `dotnet build IdealAkeWms.slnx`, Tests `dotnet test IdealAkeWms.Tests` — nie parallel zu einem anderen dotnet-Lauf im Worktree.

## Review Focus

1. **Summiert ohne KW mit vielen HauptFA** → alle erscheinen, gruppiert, keine Gruppe reißt über Seiten; Seitenzahl nach Gruppen (Test in Task 1).
2. **Direkt aufgerufene Druck-URL ohne `visibleColumns`, Anwender mit/ohne Präferenz** → Präferenz bzw. alle Spalten; gesperrte (`Locked`) Spalten nie ausgeblendet (Tests in Task 3).
3. **Umschalter mit `colf_sollmenge` + Liste-exklusivem Filter (`colf_arbeitsbereich`)** → beide entfallen, `target`/KW/gemeinsame Filter bleiben (Test in Task 1).
4. **`PdfSummiert` für eine HauptFA, die nach Filter nicht (mehr) vorkommt** → `NotFound`; bei Render-Fehler Rückweg auf `Summiert` mit Parametern (Tests in Task 3).
5. **Ziel-/Filterwert mit `"`/`</script>`** → keine Einbettung in Script-Blöcke; Links bleiben HTML-attribut-kodiert (Task 4 Selbstprüfung + TS-80.17).

---

### Task 1: Service — KW optional, Anzeigefelder, Gruppen-Paging, Umschalter-Query

**Files:**
- Modify: `IdealAkeWms/Services/KommissionierListenService.cs`
- Modify: `IdealAkeWms/Models/ViewModels/FaHierarchyKommissionierSummiertViewModel.cs` (Row: `Matchcode`, `Hauptlagerplatz`; Result: `TotalGroupCount`; Doc-Kommentare zu N2d/Seiteneinheit anpassen)
- Test: `IdealAkeWms.Tests/Services/KommissionierListenServiceTests.cs`

**Interfaces — Produces:** `FaHierarchyKommissionierSummiertRowViewModel.Matchcode/Hauptlagerplatz (string?)`, `FaHierarchyKommissionierSummiertResult.TotalGroupCount (int)`, `KommissionierListenService.ToggleSharedQueryKeys (string[])`, `static string KommissionierListenService.BuildToggleQuery(HttpRequest? request)` (liefert `""` oder `"?k=v&..."`, Werte URL-kodiert).

- [ ] **Step 1: Tests (RED)** — in `KommissionierListenServiceTests.cs`:
  - `Summiert_NoKw_ReturnsEmpty_NoImplicitTotal` umbenennen zu `Summiert_NoKw_IncludesAllHauptFa` (gleiches Fixture!) + zweite HauptFA ergänzen ist eine Fixture-Änderung → NICHT; stattdessen gleiches Fixture, Assertion: `result.Rows.Should().ContainSingle(); result.Rows[0].SummeSollmenge.Should().Be(3m); result.TotalRowCount.Should().Be(1); result.TotalGroupCount.Should().Be(1);` Kommentar: „Antwort 4 (Spec 2026-09-25-kommissionierliste-summierung-pdf): ohne KW ALLE HauptFA — ersetzt N2d“.
  - Neu `Summiert_CarriesMatchcodeAndHauptlagerplatz_WithoutChangingKey`: zwei Positionen gleicher (HauptFA, Artnr, Ziel) mit Matchcode „M1“/Hauptlagerplatz „L1“ → eine Zeile, Summe, `Matchcode == "M1"`, `Hauptlagerplatz == "L1"`. (Fixture-Helfer `Node(...)` lesen; falls er Matchcode/Hauptlagerplatz nicht setzen kann, Objekt-Initializer `Node(...)` mit anschließender Zuweisung `n.Matchcode = "M1"` verwenden — Helfer selbst NICHT ändern.)
  - Neu `Summiert_GroupPaging_NeverSplitsHauptFa`: HauptFA 100 mit 3 Artnr, HauptFA 200 mit 1 Artnr, kein KW, `page: 1, pageSize: 1, paginate: true` → alle Zeilen von 100 (3), `TotalGroupCount == 2`, `TotalRowCount == 4`; `page: 2` → genau die Zeile von 200.
  - Neu `Summiert_ColumnFilterOnMatchcode`: `columnFilters = { ["matchcode"] = "M1" }` filtert auf Zeilen mit Matchcode M1.
  - Neu `BuildToggleQuery_KeepsSharedKeys_DropsSollmengeAndListOnly`: `DefaultHttpContext` mit Query `?target=LAGER&kwVon=2026-W10&colf_artnr=A1&colf_sollmenge=5&colf_arbeitsbereich=X&page=3` → Ergebnis enthält `target=LAGER`, `kwVon=2026-W10`, `colf_artnr=A1`; enthält NICHT `colf_sollmenge`, `colf_arbeitsbereich`, `page`. Leere Query / `null` → `""`. Wert mit Leerzeichen/Sonderzeichen wird URL-kodiert.
- [ ] **Step 2:** `dotnet test IdealAkeWms.Tests --filter "FullyQualifiedName~KommissionierListenServiceTests"` → RED.
- [ ] **Step 3: Implementierung** (`AggregateSummiert`):

```csharp
        // (1) KW waehlt die HauptFA optional ueber KO_Termin. null = keine Einschraenkung
        //     (Antwort 4, Spec 2026-09-25-kommissionierliste-summierung-pdf — ersetzt N2d).
        var includedHauptFas = kwRange is null
            ? null
            : IsoWeekRange.HauptFasInRange(
                allInfos ?? Enumerable.Empty<FaHierarchyOrderInfo>(), o => o.KO_Termin, kwRange.Value);

        var predicate = BuildFlagPredicate(targetValue);
        var relevant = allNodes.Where(n => (includedHauptFas is null || includedHauptFas.Contains(n.HauptFA)) && predicate(n));
```

  Aggregat-Projektion ergänzen: `Matchcode = g.First().Matchcode, Hauptlagerplatz = g.First().Hauptlagerplatz,` (Kommentar: Artikeleigenschaften, je Summenschlüssel konstant — Schlüssel unverändert). Nach dem Spaltenfilter:

```csharp
        var filtered = aggregates.ToList();
        var totalRowCount = filtered.Count;

        // (6) Gruppen-Paging: Seiteneinheit = HauptFA (Muster FaHierarchyListBuilder.Build), nie ueber Seiten getrennt.
        var groups = filtered.GroupBy(a => a.HauptFA).ToList(); // Reihenfolge bleibt HauptFA-sortiert
        var pageRows = (paginate
                ? groups.Skip(Math.Max(0, (page - 1) * pageSize)).Take(pageSize)
                : groups)
            .SelectMany(g => g)
            .ToList();

        return new FaHierarchyKommissionierSummiertResult
        {
            Rows = pageRows,
            TotalRowCount = totalRowCount,
            TotalGroupCount = groups.Count
        };
```

  `SummiertColumnMap` um `["matchcode"] = a => a.Matchcode, ["hauptlagerplatz"] = a => a.Hauptlagerplatz` ergänzen. Doc-Kommentar der Methode (Schritt 1 „KW-Vorfilter (Pflicht)“ → „optional“, Schritt 5 „Paging auf Aggregatzeilen-Ebene“ → „Gruppen-Paging je HauptFA“) anpassen. Umschalter:

```csharp
    /// <summary>Query-Keys, die der Umschalter Liste&lt;-&gt;Summiert mitnimmt (Spec 2026-09-25-kommissionierliste-
    /// summierung-pdf, Abschnitt E): Ziel, KW (in der Liste wirkungslos, nur Rueckweg — Freigabe-Antwort 5) und die
    /// Spaltenfilter der in BEIDEN Ansichten vorhandenen Spalten. Bewusst OHNE colf_sollmenge: dort Einzelmenge,
    /// hier Summe — derselbe Wert wuerde still umgedeutet.</summary>
    public static readonly string[] ToggleSharedQueryKeys =
    [
        "target", "kwVon", "kwBis",
        "colf_hauptfa", "colf_artnr", "colf_matchcode", "colf_kommissionieren", "colf_hauptlagerplatz",
    ];

    public static string BuildToggleQuery(HttpRequest? request)
    {
        if (request == null) return string.Empty;
        var qb = new QueryBuilder();
        foreach (var key in ToggleSharedQueryKeys)
        {
            var value = request.Query[key].ToString();
            if (!string.IsNullOrEmpty(value)) qb.Add(key, value);
        }
        return qb.ToQueryString().ToString();
    }
```
  (`using Microsoft.AspNetCore.Http; using Microsoft.AspNetCore.Http.Extensions;` — `QueryBuilder` kodiert korrekt. Prüfen, wie `ColumnFilterHelper.ReadFromQuery` die `colf_`-Keys benennt, und exakt diese Schreibweise verwenden.)
- [ ] **Step 4:** GREEN; danach voller `dotnet test IdealAkeWms.Tests`. `git diff IdealAkeWms.Tests` zeigt: nur Umbenennung/Assertion des einen Tests + neue Tests.
- [ ] **Step 5:** Commit `wip: 2026-09-25-kommissionierliste-summierung-pdf service (kw optional, anzeigefelder, gruppen-paging, umschalter)`.

---

### Task 2: `ColumnDefinitions.FaHierarchyKommissionierSummiert` (6 Spalten) + Registrierung

**Files:** Modify `IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs` (nach `FaHierarchyKommissionierListen`, Z. ~328-346; `GetByViewKey` Z. ~445-460). Test: vorhandene `ColumnDefinitions`-Testdatei (per `grep -rl "GetByViewKey" IdealAkeWms.Tests` finden) — neuer Test.

- [ ] **Step 1 (RED):** Test `GetByViewKey_FaHierarchyKommissionierSummiert_HasSixColumns`: nicht null; Keys in Reihenfolge `hauptfa, artnr, matchcode, kommissionieren, hauptlagerplatz, sollmenge`; `hauptfa` und `matchcode` Locked; `SupportsSortDefault == false`.
- [ ] **Step 2:** Eintrag exakt wie Spec Abschnitt D (Labels: `HauptFA`, `Artnr.`, `Matchcode`, `Kommissionieren`, `Hauptlagerplatz`, `Summe Sollmenge` mit `DefaultWidth: 55`), Doc-Kommentar im Stil des Nachbarn; `GetByViewKey` um `"FaHierarchyKommissionierSummiert" => FaHierarchyKommissionierSummiert,` ergänzen. Prüfen, ob ein bestehender Test alle registrierten ViewKeys aufzählt (Drift-Guard) — ggf. dort nur ERGÄNZEN, wenn er sonst rot wird, und melden.
- [ ] **Step 3:** GREEN + voller Testlauf. Commit `wip: 2026-09-25-kommissionierliste-summierung-pdf columndefinitions summiert`.

---

### Task 3: Controller — sichtbare Druckspalten, Summiert-Paging, PrintSummiert/PdfSummiert

**Files:**
- Modify: `IdealAkeWms/Controllers/FaHierarchyKommissionierListenController.cs`
- Modify: `IdealAkeWms/Models/ViewModels/FaHierarchyKommissionierGruppeViewModel.cs` (`FaHierarchyKommissionierPrintViewModel.VisibleColumns`)
- Modify: `IdealAkeWms/Models/ViewModels/FaHierarchyKommissionierSummiertViewModel.cs` (`FaHierarchyKommissionierSummiertViewModel.PdfVerfuegbar`; `HasKwFilter` bleibt; NEU `FaHierarchyKommissionierSummiertPrintViewModel`)
- Create: `IdealAkeWms.Tests/Controllers/FaHierarchyKommissionierListenControllerTests.cs`

**Interfaces — Consumes:** Task 1 (`TotalGroupCount`), Task 2 (`ColumnDefinitions.FaHierarchyKommissionierSummiert`). **Produces:** `IReadOnlyList<string> VisibleColumns` auf beiden Print-VMs (leer = alle), `FaHierarchyKommissionierSummiertPrintViewModel { List<FaHierarchyKommissionierSummiertRowViewModel> Rows; IReadOnlyDictionary<int,string> Barcodes; string? SelectedTarget; bool IsFiltered; IReadOnlyList<string> VisibleColumns; }`, Actions `PrintSummiert(string? target, string? kwVon, string? kwBis, string? visibleColumns)`, `PdfSummiert(int hauptFa, string? target, string? kwVon, string? kwBis, string? visibleColumns)` mit Route `[HttpGet("FaHierarchyKommissionierListen/PdfSummiert/{hauptFa:int}")]`; `Print(string? target, string? visibleColumns)`, `Pdf(int hauptFa, string? target, string? visibleColumns)`.

- [ ] **Step 1 (RED)** — Controller-Tests (Controller mit Moq-Abhängigkeiten; `KommissionierListenService` hat konkrete Abhängigkeiten `IFaHierarchyNodeRepository`, `IFaHierarchyOrderInfoRepository`, `FaHierarchyListBuilder` — mit Mocks/echtem Builder instanziieren, wie andere Tests es tun: `grep -rn "new KommissionierListenService" IdealAkeWms.Tests`). `ControllerContext` mit `DefaultHttpContext` (Query setzbar), `TempData` via `TempDataDictionary`:
  - `Print_WithVisibleColumnsParam_UsesItVerbatim_NoPrefRead`: `visibleColumns: "artnr,sollmenge"` → VM.VisibleColumns == [artnr, sollmenge]; `_viewPrefs.GetByUserAndViewAsync` nie aufgerufen.
  - `Print_WithoutParam_FallsBackToSavedPref`: User 7, Pref-JSON mit `material` und `artnr` unsichtbar → VisibleColumns enthält alle Keys von `FaHierarchyKommissionierListen` außer `artnr`, `material` (in `ColumnDefinitions`-Reihenfolge). Pref-JSON-Format an `WarehousePickingPrintLayout.PrintPrefs` ablesen.
  - `Print_WithoutParam_LockedColumnsNeverHidden`: Pref markiert `matchcode` (Locked) unsichtbar → `matchcode` bleibt enthalten.
  - `Print_WithoutParam_NoPref_ReturnsEmpty` und `..._NoUser_ReturnsEmpty` → leere Liste.
  - `PrintSummiert_UsesSummiertViewKey`: ohne Param, Pref für `FaHierarchyKommissionierSummiert` → Rückfall liest genau diesen viewKey.
  - `Summiert_PaginationCountsGroups`: zwei HauptFA mit je 2 Aggregatzeilen → `Pagination.TotalCount == 2`.
  - `PdfSummiert_UnknownHauptFa_NotFound`.
  - `PdfSummiert_RenderFails_RedirectsToSummiertWithParams`: `IPdfRenderService` wirft `PdfRenderException` → `RedirectToActionResult` mit `ActionName == "Summiert"`, RouteValues `target`, `kwVon`, `kwBis`; `TempData["WarningMessage"]` gesetzt. (`PdfRenderException`-Konstruktor ablesen.)
- [ ] **Step 2:** RED.
- [ ] **Step 3: Implementierung:**
  - Ctor-Parameter `IUserViewPreferenceRepository viewPrefs` (DI ist registriert — prüfen in `Program.cs`).
  - `ResolveVisibleColumnsAsync` exakt wie Spec Abschnitt B (Code dort), `defs` = `IReadOnlyList<ColumnDef>` der jeweiligen `ViewConfig.Columns`.
  - `Print`/`Pdf`: Parameter `string? visibleColumns`, VM-Feld über Helfer mit `"FaHierarchyKommissionierListen"` + `ColumnDefinitions.FaHierarchyKommissionierListen.Columns`.
  - `Summiert`: `TotalCount = result.TotalGroupCount`, `PdfVerfuegbar = _pdfRenderStatus.IsAvailable`; Doc-Kommentar „Ohne KW-Eingabe Hinweistext“ → „ohne KW alle HauptFA (KW optional)“.
  - `PrintSummiert`: Code aus Spec Abschnitt C (`BuildSummiertAsync(..., 1, int.MaxValue, paginate: false)`, Barcodes je distinct HauptFA).
  - `PdfSummiert`: wie `Pdf`, aber `rows = result.Rows.Where(r => r.HauptFA == hauptFa).ToList()`, leer → `NotFound()`; rendert `"PrintSummiert"`; Dateiname `PdfFileNameBuilder.Build("Kommissionierliste-summiert", hauptFa, DateTime.Now)`; `catch (PdfRenderException ex)` → `TempData["WarningMessage"] = ex.UserMessage; return RedirectToAction(nameof(Summiert), new { target, kwVon, kwBis });`.
  - Hinweis: `PrintSummiert.cshtml` existiert erst nach Task 4 — Tests arbeiten mit `ViewResult`/gemocktem `IRazorViewRenderer`, kein echtes Rendering.
- [ ] **Step 4:** GREEN + voller Testlauf. Commit `wip: 2026-09-25-kommissionierliste-summierung-pdf controller (visibleColumns, printsummiert, pdfsummiert)`.

---

### Task 4: Views, JS, Menü

**Pflicht zuerst:** Skill `frontend-design` aufrufen. Leitplanken: Bootstrap 5, bestehende Muster (Index.cshtml ist Vorlage für Summiert), keine neue Bibliothek, WCAG AA, deutsch.

**Files:**
- Create: `IdealAkeWms/wwwroot/js/print-visible-columns.js` (Code aus Spec Abschnitt B, in IIFE + `DOMContentLoaded`; Kommentar zu Zweck; KEIN `preventDefault`)
- Modify: `IdealAkeWms/Views/FaHierarchyKommissionierListen/Index.cshtml` — Drucken-Link + PDF-Link je Gruppe: `data-print-link="FaHierarchyKommissionierListen"`; Umschalter (Z. ~25/28): `href="@(Url.Action("Summiert") + KommissionierListenService.BuildToggleQuery(Context.Request))"` bzw. für „Liste“ (aktiv) `Url.Action("Index") + BuildToggleQuery(...)`; Script-Include `print-visible-columns.js` in `@section Scripts`.
- Modify: `IdealAkeWms/Views/FaHierarchyKommissionierListen/Print.cshtml` — `@{ bool ShowCol(string key) => !Model.VisibleColumns.Any() || Model.VisibleColumns.Contains(key); }`; jede Positions-`<th>`/`<td>` hinter `@if (ShowCol("..."))` mit ihrem Key (`hauptartnr, artnr, matchcode, sollmenge, hauptlagerplatz, kommissionieren, arbeitsbereich, artikeltyp, beschichtet, material`) — `hauptfa` ist Gruppenkopf, keine Positionsspalte.
- Create: `IdealAkeWms/Views/FaHierarchyKommissionierListen/PrintSummiert.cshtml` — Layout/CSS wie `Print.cshtml` (kopieren und reduzieren), Titel „Kommissionierliste (IDEAL) — Summiert“, je `Model.Rows.GroupBy(r => r.HauptFA)` ein `.fa-group` mit Banner-Block: „Summierte Ansicht“ (immer, eigenes `.sum-banner`, Wortlaut z. B. „<strong>Summierte Ansicht</strong> — je Artikelnummer und Kommissionierziel summierte Sollmenge, keine Einzelpositionen.“) + „Gefilterte Ansicht“-Banner wie Print (wenn `IsFiltered`) — beide nebeneinander/untereinander sichtbar (AK 10); Überschrift „Kommissionierliste (summiert) — HauptFA X“, Barcode; Positions-Tabelle mit `ShowCol` über `artnr, matchcode, kommissionieren, hauptlagerplatz, sollmenge` (Label „Summe Sollmenge“, `N3` de-DE, `text-end`). Leerzustand-Text wie Print.
- Modify: `IdealAkeWms/Views/FaHierarchyKommissionierListen/Summiert.cshtml` — Umbau:
  - `colCount = 6`; Header-Zeile mit seitenweitem „Drucken“-Link (`href="@(Url.Action("PrintSummiert") + Context.Request.QueryString)"`, `target="_blank"`, `data-print-link="FaHierarchyKommissionierSummiert"`) wie Index.
  - Umschalter über `BuildToggleQuery` (s. o.).
  - Filterkarte bleibt; Erklärtext anpassen: KW **optional** („Ohne Kalenderwoche werden alle HauptFAs einbezogen.“).
  - `!Model.HasKwFilter`-Hinweis-Zweig ENTFÄLLT; Tabelle immer.
  - `<thead>`: sechs `<th data-filterable data-col-key>` in Reihenfolge `hauptfa, artnr, matchcode, kommissionieren, hauptlagerplatz, sollmenge`.
  - Leerzustand: „Keine Kommissionier-Positionen für die aktuelle Filterung gefunden.“
  - Gruppen: `@foreach (var group in Model.Rows.GroupBy(r => r.HauptFA))` → `<tbody class="fa-komm-group">`, Kopfzeile `<tr class="table-active"><td colspan="@colCount"><strong>HauptFA X</strong>` + PDF-Link (`Url.Action("PdfSummiert", new { hauptFa = group.Key }) + Context.Request.QueryString`, `data-print-link="FaHierarchyKommissionierSummiert"`, gleiches SVG/Markup wie Index) wenn `Model.PdfVerfuegbar`; dann Zeilen mit sechs Zellen (Reihenfolge wie `<th>`).
  - `#view-config`: `"supportsSortDefault": false`; `#column-config` mit 6 Einträgen passend zu `ColumnDefinitions` (Labels/locked/defaultWidth identisch).
  - `@section Scripts`: `print-visible-columns.js` ergänzen.
- Modify: `IdealAkeWms/Views/Shared/_Layout.cshtml` Z. 184 und Z. 203-204: `asp-action="Summiert"`.
- [ ] **Step 1-3:** umsetzen. Selbstprüfung: `grep -n "Html.Raw" <geänderte Views>` → keine neue Einbettung in `<script>`; `<th data-col-key>`-Menge == `#column-config` == `ColumnDefinitions` je View.
- [ ] **Step 4:** `dotnet build IdealAkeWms.slnx` + voller Testlauf.
- [ ] **Step 5:** Commit `wip: 2026-09-25-kommissionierliste-summierung-pdf views (summiert standard, druckspalten, printsummiert)`.

---

### Task 5: Drift-Guard-Test (Vier-Stellen-Konsistenz)

**Files:** Create `IdealAkeWms.Tests/Views/KommissionierSpaltenKonsistenzTests.cs` (Ordner ggf. neu; Muster Repo-Root-Suche aus `IdealAkeWms.Tests/Helpers/TableScriptScopedSelectorTests.cs` Z. ~50-65).

- [ ] **Step 1:** Für beide Views (`Index.cshtml`+`Print.cshtml` ↔ `FaHierarchyKommissionierListen`; `Summiert.cshtml`+`PrintSummiert.cshtml` ↔ `FaHierarchyKommissionierSummiert`) per Regex aus dem Quelltext lesen:
  - `<th ... data-col-key="X">` → Menge A (Bildschirm-View),
  - `#column-config`-JSON (`<script type="application/json" id="column-config">…</script>`, mit `System.Text.Json` parsen, `key`) → Menge B,
  - `ColumnDefinitions.<View>.Columns.Select(c => c.Key)` → Menge C,
  - `ShowCol("X")` in der Druckvorlage → Menge D.
  Assertions: A == B == C (als Mengen, zusätzlich B-Reihenfolge == C-Reihenfolge), D == C \ {"hauptfa"}. Fehlermeldung nennt fehlende/zusätzliche Keys je Quelle.
- [ ] **Step 2:** Test läuft grün gegen den Stand nach Task 4. Gegenprobe (nicht committen): einen Key in `#column-config` von Summiert temporär umbenennen → Test rot → zurücksetzen; im Report dokumentieren.
- [ ] **Step 3:** Commit `wip: 2026-09-25-kommissionierliste-summierung-pdf drift-guard spaltenschluessel`.

---

### Task 6: Version, Changelog, Hilfe, TS-80

**Files:** beide `AppVersion.cs` → `1.47.0`; `Views/Help/Changelog.cshtml` (neue v1.47.0-Karte, Datum 28.09.2026); `Views/Help/Index.cshtml` (Abschnitt Kommissionierlisten IDEAL: Summiert ist Einstieg, KW optional, Matchcode/Hauptlagerplatz, Gruppen je HauptFA, Druck/PDF zeigen die beim Klick sichtbaren Spalten, Umschalter nimmt Ziel/KW/gemeinsame Filter mit, nicht den Mengenfilter; KW wirkt in der Liste nicht); `docs/TESTSZENARIEN.md` neues Kapitel `## TS-80 — IDEAL: Kommissionierliste — Summiert als Standard, Druck/PDF nur sichtbare Spalten (v1.47.0)` nach TS-79 mit **TS-80.1 = Einheiten-Prüfschritt** (Vorbedingung der Abnahme, Sage-DB IDEAL, `EXEC sp_helptext 'vw_IDEAL-AKE_Kommissionierung_FAListe'` + `INFORMATION_SCHEMA.COLUMNS … LIKE '%einheit%'`, kein Treffer = Falle 2 erledigt, Treffer = melden, nicht weiterbauen) und danach den Spec-Szenarien (TS-80.2 ff.) inkl. AK-Mapping, Vorbedingungen, Schritte, Erwartung, Negativfälle; Schlusszeile „Stand“ ergänzen.
- [ ] Verify: build + `dotnet test IdealAkeWms.Tests` + `dotnet test IDEALAKEWMSService.Tests`. Commit `wip: 2026-09-25-kommissionierliste-summierung-pdf feature-complete (v1.47.0)`.

## Nach den Tasks (Orchestrator, Hauptcheckout)

Final-Review, QA-Agent; Brain: Vermerk an AK N2d in `2026-07-29-standort-ideal-teil-3-spec`, `fallstricke.md` Mengeneinheit (Quelle `vw_IDEAL-AKE_Kommissionierung_FAListe`), Changelog-Brain, feature-map, Testindex.
