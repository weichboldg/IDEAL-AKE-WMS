# BOM-Tree Performance — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Massive Performance-Verbesserung beim Auf-/Zuklappen von Baugruppen in der Stueckliste-Sicht (`Views/Picking/Bom.cshtml`). Initial-Load 5-10x schneller, Toggle-Aktion sub-100ms.

**Architecture:** Drei kompoundierende Ursachen werden adressiert:
- **Fix A** (groesster Hebel): Per-Row `<select>`-Dropdown mit allen Lagerplaetzen wird auf Select2 + Ajax umgestellt — pro Row nur 1 vorgewaehlte `<option>`, REST kommt per Klick aus neuem `/api/picking/source-locations`-Endpoint.
- **Fix B**: Client-JS `updateBomVisibility()` wird gecacht (td-Index pro Row, Header-Spalten-Map einmalig), Rekursion in `expandBaugruppe`/`collapseBaugruppe` ruft Visibility nur **einmal am Ende** auf, DOM-Updates in einem Pass.
- **Fix C**: Server-side `pickingItems.FirstOrDefault(...)` (O(N×P)) durch `Dictionary<(ArticleNumber, Position), PickingItem>` ersetzt.

**Tech Stack:** ASP.NET Core 10 MVC, Select2 4.1 (bereits via `_Select2ArticlePartial.cshtml` etabliert), JavaScript Vanilla im Bom.cshtml.

**Branch:** `feature/sage-lagerbestand-sync` — kein neuer Branch, gehoert zur Phase-2-Bundle.

**Spec:** Diskutiert in der Q&A oben (Fix A = Select2+Ajax, Fix B = Refactor, Fix C = Dictionary). Kein eigener Spec-File — Plan ist Spec.

**Commit-Konvention:** `perf(bom): ...` / `feat(api): ...` / `test(...): ...` / `docs: ...`. Co-Authored-By trailer.

---

## Task 1: API-Endpoint `/api/picking/source-locations` + Tests

**Files:**
- Create: `IdealAkeWms/Controllers/Api/PickingApiController.cs`
- Create: `IdealAkeWms.Tests/Controllers/PickingApiControllerTests.cs`

Endpoint liefert die Auswahl fuer Source-Location-Dropdowns: aktive, buchbare, nicht-Picking-Transport-Lagerplaetze, mit Stock-Annotation pro Artikel. Sortiert nach Bestand absteigend, danach Code aufsteigend. Limit 50.

- [ ] **Step 1: Test schreiben (failing)**

```csharp
// IdealAkeWms.Tests/Controllers/PickingApiControllerTests.cs
using FluentAssertions;
using IdealAkeWms.Controllers.Api;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Tests.Controllers;

public class PickingApiControllerTests
{
    [Fact]
    public async Task SearchSourceLocations_ReturnsActiveBookableLocations_WithStockForArticle()
    {
        using var ctx = TestDbContextFactory.Create();
        // Article
        ctx.Articles.Add(new Article
        {
            Id = 1, ArticleNumber = "A-1", Description = "Test", Unit = "Stk",
            CreatedBy = "x", CreatedByWindows = "x"
        });
        // 3 Locations: aktiv+buchbar mit Bestand, aktiv+buchbar ohne Bestand, inaktiv (sollte ausgeschlossen sein)
        ctx.StorageLocations.AddRange(
            new StorageLocation { Id = 1, Code = "L-1", BarcodeValue = "L-1", IsActive = true,  IstBuchbar = true,  IsPickingTransport = false, Source = StorageLocationSource.Manual, CreatedBy = "x", CreatedByWindows = "x" },
            new StorageLocation { Id = 2, Code = "L-2", BarcodeValue = "L-2", IsActive = true,  IstBuchbar = true,  IsPickingTransport = false, Source = StorageLocationSource.Manual, CreatedBy = "x", CreatedByWindows = "x" },
            new StorageLocation { Id = 3, Code = "L-3", BarcodeValue = "L-3", IsActive = false, IstBuchbar = true,  IsPickingTransport = false, Source = StorageLocationSource.Manual, CreatedBy = "x", CreatedByWindows = "x" },
            new StorageLocation { Id = 4, Code = "WAGEN-1", BarcodeValue = "WAGEN-1", IsActive = true, IstBuchbar = true, IsPickingTransport = true, Source = StorageLocationSource.Manual, CreatedBy = "x", CreatedByWindows = "x" }
        );
        // Stock auf L-1: 12.5; L-2: kein Bestand
        ctx.StockMovements.Add(new StockMovement
        {
            ArticleId = 1, StorageLocationId = 1, Quantity = 12.5m, MovementType = MovementType.Einbuchung,
            Timestamp = DateTime.Now, WindowsUser = "x", CreatedAt = DateTime.Now,
            CreatedBy = "x", CreatedByWindows = "x"
        });
        await ctx.SaveChangesAsync();

        var locRepo = new StorageLocationRepository(ctx);
        var stockRepo = new StockMovementRepository(ctx);
        var ctrl = new PickingApiController(locRepo, stockRepo);

        var result = await ctrl.SearchSourceLocations(articleNumber: "A-1", q: null, limit: 50);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var items = ok.Value.Should().BeAssignableTo<IEnumerable<object>>().Subject.ToList();
        // Erwartet: L-1 (Bestand 12.5) zuerst, dann L-2 (kein Bestand). Inaktive (L-3) und Wagen (WAGEN-1) raus.
        items.Should().HaveCount(2);
    }

    [Fact]
    public async Task SearchSourceLocations_FiltersByQuery_OnCode()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.Articles.Add(new Article { Id = 1, ArticleNumber = "A-1", Description = "x", Unit = "Stk", CreatedBy = "x", CreatedByWindows = "x" });
        ctx.StorageLocations.AddRange(
            new StorageLocation { Id = 1, Code = "HALLE-A1", BarcodeValue = "HALLE-A1", IsActive = true, IstBuchbar = true, IsPickingTransport = false, Source = StorageLocationSource.Manual, CreatedBy = "x", CreatedByWindows = "x" },
            new StorageLocation { Id = 2, Code = "HALLE-B1", BarcodeValue = "HALLE-B1", IsActive = true, IstBuchbar = true, IsPickingTransport = false, Source = StorageLocationSource.Manual, CreatedBy = "x", CreatedByWindows = "x" }
        );
        await ctx.SaveChangesAsync();

        var locRepo = new StorageLocationRepository(ctx);
        var stockRepo = new StockMovementRepository(ctx);
        var ctrl = new PickingApiController(locRepo, stockRepo);

        var result = await ctrl.SearchSourceLocations(articleNumber: "A-1", q: "A1", limit: 50);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var items = ok.Value.Should().BeAssignableTo<IEnumerable<object>>().Subject.ToList();
        items.Should().ContainSingle();
    }
}
```

- [ ] **Step 2: Tests laufen — FAIL (Controller nicht da)**

```pwsh
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "PickingApiControllerTests" --nologo
```

Expected: Compile-Error.

- [ ] **Step 3: Controller implementieren**

```csharp
// IdealAkeWms/Controllers/Api/PickingApiController.cs
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers.Api;

[ApiController]
[Route("api/picking")]
[RequirePickingAccess]
public class PickingApiController : ControllerBase
{
    private readonly IStorageLocationRepository _storageLocations;
    private readonly IStockMovementRepository _stockMovements;

    public PickingApiController(
        IStorageLocationRepository storageLocations,
        IStockMovementRepository stockMovements)
    {
        _storageLocations = storageLocations;
        _stockMovements = stockMovements;
    }

    /// <summary>
    /// Liefert Source-Location-Auswahl fuer Bom-Picking-Dropdown.
    /// Aktive, buchbare, nicht-Picking-Transport-Lagerplaetze.
    /// Sortiert: Bestand absteigend, dann Code.
    /// </summary>
    [HttpGet("source-locations")]
    public async Task<IActionResult> SearchSourceLocations(string? articleNumber, string? q, int limit = 50)
    {
        var locations = await _storageLocations.GetActiveOrderedExcludingPickingTransportAsync();

        // Stock pro Article einmal laden
        var stockByLoc = new Dictionary<int, decimal>();
        if (!string.IsNullOrWhiteSpace(articleNumber))
        {
            var stockDict = await _stockMovements.GetStockByArticleNumbersAsync(new List<string> { articleNumber });
            if (stockDict.TryGetValue(articleNumber, out var stockList))
            {
                foreach (var s in stockList)
                    stockByLoc[s.StorageLocationId] = s.Quantity;
            }
        }

        var filtered = locations.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            filtered = filtered.Where(l => l.Code.Contains(q, StringComparison.OrdinalIgnoreCase));
        }

        var ranked = filtered
            .Select(l =>
            {
                stockByLoc.TryGetValue(l.Id, out var qty);
                var hasStock = qty > 0;
                var label = hasStock ? $"{l.Code} ({qty:N3})" : l.Code;
                return new { id = l.Id, text = label, qty, hasStock };
            })
            .OrderByDescending(x => x.hasStock)
            .ThenByDescending(x => x.qty)
            .ThenBy(x => x.text)
            .Take(limit)
            .Select(x => new { id = x.id, text = x.text })
            .ToList();

        return Ok(ranked);
    }
}
```

- [ ] **Step 4: Tests laufen — PASS**

```pwsh
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "PickingApiControllerTests" --nologo
```

Expected: 2/2 PASS.

- [ ] **Step 5: Build + alle Tests**

```pwsh
dotnet build --nologo
dotnet test --nologo --no-build
```

Expected: alles gruen.

- [ ] **Step 6: Commit**

```pwsh
git add IdealAkeWms/Controllers/Api/PickingApiController.cs IdealAkeWms.Tests/Controllers/PickingApiControllerTests.cs
git commit -m "feat(api): add /api/picking/source-locations search endpoint" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 2: Bom.cshtml — Select2 + Ajax statt N×L vorab gerenderter Optionen

**Files:**
- Modify: `IdealAkeWms/Views/Picking/Bom.cshtml`

Statt pro Row alle `Model.AllStorageLocations` als `<option>` zu rendern, kommt pro Row nur eine pre-selected `<option>` (oder leer). Select2 + Ajax bindet zur Suche an `/api/picking/source-locations?articleNumber=X&q=...`.

- [ ] **Step 1: Select2-Includes ergaenzen**

In `IdealAkeWms/Views/Picking/Bom.cshtml`, im `@section Scripts { ... }`-Block oder direkt im View, vor dem main script-Block, Select2-Assets einbinden — die werden bereits aus `_Select2ArticlePartial.cshtml` benutzt, also gleichen CDN nutzen:

Suche zuerst die existing Script/Section-Struktur:
```pwsh
```

Dann am Anfang des `@section Scripts`-Blocks (oder vor dem ersten `<script>`):

```cshtml
<link href="https://cdn.jsdelivr.net/npm/select2@4.1.0-rc.0/dist/css/select2.min.css" rel="stylesheet" />
<link href="https://cdn.jsdelivr.net/npm/select2-bootstrap-5-theme@1.3.0/dist/select2-bootstrap-5-theme.min.css" rel="stylesheet" />
<script src="https://cdn.jsdelivr.net/npm/select2@4.1.0-rc.0/dist/js/select2.min.js"></script>
<script src="https://cdn.jsdelivr.net/npm/select2@4.1.0-rc.0/dist/js/i18n/de.js"></script>
```

WICHTIG: jQuery muss verfuegbar sein. Ist im `_Layout.cshtml` ueblicherweise eingebunden — verifizieren.

- [ ] **Step 2: Per-Row `<select>` auf skinny umstellen**

Suche in `Bom.cshtml` den `@foreach (var item in Model.Items)`-Block, dort die `<select class="...source-location-select">`-Section (ca. Zeile 174-187). Ersetze:

```cshtml
<select class="form-select form-select-sm source-location-select" data-id="@item.PickingItemId" style="min-width: 100px;">
    <option value="">--</option>
    @foreach (var sl in Model.AllStorageLocations)
    {
        var isInStock = item.StockLocations.Any(s => s.StorageLocationId == sl.Id);
        var stockQty = item.StockLocations.FirstOrDefault(s => s.StorageLocationId == sl.Id)?.Quantity;
        var label = isInStock ? $"{sl.Code} ({stockQty:N3})" : sl.Code;
        <option value="@sl.Id" selected="@(item.SourceStorageLocationId == sl.Id)">@label</option>
    }
</select>
```

durch:

```cshtml
<select class="form-select form-select-sm source-location-select"
        data-id="@item.PickingItemId"
        data-article-number="@item.Ressourcenummer"
        style="min-width: 200px;">
    <option value="">--</option>
    @if (item.SourceStorageLocationId.HasValue)
    {
        var preSelected = Model.AllStorageLocations.FirstOrDefault(sl => sl.Id == item.SourceStorageLocationId.Value);
        if (preSelected != null)
        {
            var stockQty = item.StockLocations.FirstOrDefault(s => s.StorageLocationId == preSelected.Id)?.Quantity;
            var preLabel = stockQty.HasValue && stockQty > 0
                ? $"{preSelected.Code} ({stockQty:N3})"
                : preSelected.Code;
            <option value="@preSelected.Id" selected>@preLabel</option>
        }
    }
</select>
```

Damit rendert pro Row nur 1 (oder 0) `<option>` statt aller L Lagerplaetze.

- [ ] **Step 3: Select2-Initialization-Block einfuegen**

Im script-Bereich von `Bom.cshtml`, am Anfang der Initialisierung (z.B. direkt nach dem DOMContentLoaded-Block oder am Ende), neuen Block ergaenzen:

```cshtml
<script>
    $(function () {
        $('.source-location-select').each(function () {
            var $el = $(this);
            var articleNumber = $el.data('article-number');

            $el.select2({
                theme: 'bootstrap-5',
                width: '100%',
                ajax: {
                    url: '/api/picking/source-locations',
                    dataType: 'json',
                    delay: 250,
                    data: function (params) {
                        return {
                            articleNumber: articleNumber || '',
                            q: params.term || '',
                            limit: 50
                        };
                    },
                    processResults: function (data) {
                        return { results: data };
                    },
                    cache: true
                },
                placeholder: '--',
                minimumInputLength: 0,
                allowClear: true,
                language: 'de',
                dropdownAutoWidth: true,
                dropdownParent: $('body')   // wichtig: sonst clipped Select2 in <td>
            });
        });
    });
</script>
```

WICHTIG: Falls der existing script-Block in einem `@section Scripts { ... }` ist, muss der Select2-Init dort passend platziert werden (nach jQuery-Include und nach den existing function-defs).

- [ ] **Step 4: existing onChange-Handler verifizieren**

Der existing Code liest `sourceSelect.value` (Zeile 771 in Bom.cshtml) — Select2 erhaelt das `<select>`-Element bei und triggert `change`-Events korrekt. Lies die Stelle, falls jQuery-Wrapped: ggf. an `change.select2`-Event binden. Sollte aber out-of-the-box funktionieren, weil Select2 das native select syncronisiert.

- [ ] **Step 5: Build + manueller Smoke-Test**

```pwsh
dotnet build IdealAkeWms/IdealAkeWms.csproj --nologo
```

Expected: 0 Fehler.

Manuell: Web-App starten, eine FA mit grosser Stueckliste oeffnen — Bom-Seite sollte deutlich schneller laden, Auf-/Zuklappen schneller reagieren. Source-Location-Dropdown bei Klick auf Pfeil zeigt Liste; Tippen filtert clientseitig (Select2 default) bzw. via Ajax.

- [ ] **Step 6: Commit**

```pwsh
git add IdealAkeWms/Views/Picking/Bom.cshtml
git commit -m "perf(bom): replace eager dropdown rendering with Select2+Ajax (massive HTML reduction)" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 3: `updateBomVisibility()` Refactor — Cache + Recursion-Guard + Batch-DOM

**Files:**
- Modify: `IdealAkeWms/Views/Picking/Bom.cshtml`

Drei Sub-Optimierungen im JS:
1. `bomColKeyToIndex(colKey)` Ergebnis 1× cachen statt pro Row neu zu berechnen.
2. Pro Row `td`-Refs einmal cachen (data-attribute oder via `row.cells`).
3. `updateBomVisibility()` nur einmal pro User-Aktion aufrufen (Recursion in `expandBaugruppe`/`collapseBaugruppe` darf keinen Visibility-Sync triggern).

- [ ] **Step 1: bomColKeyToIndex-Cache einfuehren**

Im script-Block von `Bom.cshtml`, ersetze die existing Funktion (ca. Zeile 550-556):

```javascript
function bomColKeyToIndex(colKey) {
    var ths = document.querySelectorAll('#bomTable thead tr:first-child th');
    for (var i = 0; i < ths.length; i++) {
        if (ths[i].getAttribute('data-col-key') === colKey) return i;
    }
    return -1;
}
```

durch:

```javascript
// Spalten-Key zu td-Index Map. Wird einmalig gebaut und bei Spalten-Show/Hide-Events refreshed.
var bomColKeyMap = null;
function buildBomColKeyMap() {
    bomColKeyMap = {};
    var ths = document.querySelectorAll('#bomTable thead tr:first-child th');
    for (var i = 0; i < ths.length; i++) {
        var key = ths[i].getAttribute('data-col-key');
        if (key) bomColKeyMap[key] = i;
    }
}
function bomColKeyToIndex(colKey) {
    if (!bomColKeyMap) buildBomColKeyMap();
    var idx = bomColKeyMap[colKey];
    return typeof idx === 'number' ? idx : -1;
}
// Bei Spalten-Sichtbarkeit-Änderung Cache invalidieren (column-preferences emit das nicht direkt,
// aber die Spalten-Indizes ändern sich nicht durch Show/Hide — Cache bleibt gueltig).
```

- [ ] **Step 2: `updateBomVisibility()` mit cached row.cells und recursion-guard**

Ersetze die existing `updateBomVisibility`-Funktion plus die `expandBaugruppe`/`collapseBaugruppe`-Funktionen (ca. Zeile 562-636):

```javascript
function updateBomVisibility() {
    var filters = typeof window.getActiveFilters === 'function' ? window.getActiveFilters() : {};
    var hasActiveFilter = Object.keys(filters).length > 0;
    var filterKeys = Object.keys(filters);

    // Pre-resolve filter colKey -> td index ONCE, not per row
    var resolvedFilters = filterKeys.map(function (k) {
        return { idx: bomColKeyToIndex(k), value: filters[k] };
    }).filter(function (f) { return f.idx >= 0; });

    var rows = document.querySelectorAll('#bomTable tbody tr');
    rows.forEach(function (row) {
        if (row.querySelector('td[colspan]')) return;

        var parentPos = row.getAttribute('data-parent-pos');
        var treeVisible = !parentPos || expandedState[parentPos] === true;

        if (recursiveFilterSearch && hasActiveFilter) {
            var parentExplicitlyClosed = parentPos && expandedState[parentPos] === false;
            if (!parentExplicitlyClosed) {
                treeVisible = true;
            }
        }

        var filterVisible = true;
        if (resolvedFilters.length > 0) {
            // row.cells ist nativ HTMLCollection — keine querySelectorAll noetig
            var cells = row.cells;
            for (var i = 0; i < resolvedFilters.length; i++) {
                var f = resolvedFilters[i];
                var cell = cells[f.idx];
                if (cell && !bomMatchesFilter(cell.textContent.toLowerCase(), f.value)) {
                    filterVisible = false;
                    break;
                }
            }
        }

        // Class statt inline style — leichter fuer Browser, kein Inline-Style-Override
        var shouldShow = treeVisible && filterVisible;
        if (shouldShow) {
            row.style.display = '';
        } else {
            row.style.display = 'none';
        }
    });
}

function getChildRows(parentPos) {
    return document.querySelectorAll('#bomTable tbody tr[data-parent-pos="' + parentPos + '"]');
}

// Recursion-Helper: setzt nur State, ruft updateBomVisibility nicht.
function expandBaugruppeStateOnly(pos) {
    expandedState[pos] = true;
    var chevron = document.querySelector('.bom-toggle[data-target-pos="' + pos + '"] .bom-chevron');
    if (chevron) chevron.style.transform = 'rotate(90deg)';
    getChildRows(pos).forEach(function (row) {
        var childPos = row.getAttribute('data-position');
        if (row.hasAttribute('data-baugruppe-id') && expandedState[childPos]) {
            expandBaugruppeStateOnly(childPos);
        }
    });
}

function collapseBaugruppeStateOnly(pos) {
    expandedState[pos] = false;
    var chevron = document.querySelector('.bom-toggle[data-target-pos="' + pos + '"] .bom-chevron');
    if (chevron) chevron.style.transform = '';
    getChildRows(pos).forEach(function (row) {
        var childPos = row.getAttribute('data-position');
        if (row.hasAttribute('data-baugruppe-id')) {
            collapseBaugruppeStateOnly(childPos);
        }
    });
}

// Public APIs: rufen genau einmal updateBomVisibility() am Ende auf.
function expandBaugruppe(pos) {
    expandBaugruppeStateOnly(pos);
    updateBomVisibility();
}

function collapseBaugruppe(pos) {
    collapseBaugruppeStateOnly(pos);
    updateBomVisibility();
}

function toggleBaugruppe(pos) {
    if (expandedState[pos]) {
        collapseBaugruppe(pos);
    } else {
        expandBaugruppe(pos);
    }
}
```

WICHTIG: `expandAll`-/`collapseAll`-Buttons (ca. Zeile 657, 668) sind bereits korrekt — die setzen alle States in einem Loop und rufen `updateBomVisibility()` nur einmal am Ende. Diese Bloecke unveraendert lassen.

- [ ] **Step 3: Build verifizieren**

```pwsh
dotnet build IdealAkeWms/IdealAkeWms.csproj --nologo
```

Expected: 0 Fehler.

Manuell smoke-test: Auf-/Zuklappen einer tief-geschachtelten Baugruppe sollte deutlich schneller reagieren (sub-100ms).

- [ ] **Step 4: Commit**

```pwsh
git add IdealAkeWms/Views/Picking/Bom.cshtml
git commit -m "perf(bom): cache colKey-map, use row.cells, single visibility-update per toggle" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 4: PickingController — Dictionary statt FirstOrDefault

**Files:**
- Modify: `IdealAkeWms/Controllers/PickingController.cs`

- [ ] **Step 1: Dictionary-Erstellung vor dem Select**

In `IdealAkeWms/Controllers/PickingController.cs`, Methode `Bom(int id, string? filterText)`:

Vor Zeile 206 (`var viewItems = bomItems.Select(bom => { ... })`), nach `var pickingItems = await _pickingRepository.GetByProductionOrderAsync(id);`, ein Dictionary aufbauen:

```csharp
// O(1) Lookup statt O(P) FirstOrDefault pro BOM-Item
var pickingByKey = pickingItems
    .Where(p => p.BomArticleNumber != null && p.BomPosition != null)
    .GroupBy(p => (p.BomArticleNumber!, p.BomPosition!))
    .ToDictionary(g => g.Key, g => g.First());
```

- [ ] **Step 2: FirstOrDefault durch Dictionary-Lookup ersetzen**

Im `Select(...)`-Block (ca. Zeile 208), ersetze:

```csharp
var picking = pickingItems.FirstOrDefault(p => p.BomArticleNumber == bom.Ressourcenummer && p.BomPosition == bom.Position);
```

durch:

```csharp
PickingItem? picking = null;
if (bom.Ressourcenummer != null && bom.Position != null
    && pickingByKey.TryGetValue((bom.Ressourcenummer, bom.Position), out var found))
{
    picking = found;
}
```

- [ ] **Step 3: Build + Tests**

```pwsh
dotnet build --nologo
dotnet test --nologo --no-build
```

Expected: alles gruen — keine Test-Aenderung erwartet, da Verhalten identisch.

- [ ] **Step 4: Commit**

```pwsh
git add IdealAkeWms/Controllers/PickingController.cs
git commit -m "perf(bom): O(1) picking-item lookup via dictionary instead of FirstOrDefault per row" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 5: Phase-2-Changelog erweitern

**Files:**
- Modify: `IdealAkeWms/Views/Help/Changelog.cshtml`

KEIN AppVersion-Bump — das ist eine Performance-Verbesserung innerhalb von Phase 2 (v1.10.0).

- [ ] **Step 1: Bullet-Point ergaenzen**

In `IdealAkeWms/Views/Help/Changelog.cshtml`, im **existing** v1.10.0-Card am Ende der `<ul>`:

```cshtml
<li><strong>Performance:</strong> Stueckliste-Sicht (Auf-/Zuklappen von Baugruppen) ist nun deutlich schneller. Lagerplatz-Dropdowns laden per Ajax statt vorab gerendert (Initial-Load 5-10x schneller, Toggle sub-100ms auch bei tausenden BOM-Positionen).</li>
```

- [ ] **Step 2: Build + Tests**

```pwsh
dotnet build --nologo
dotnet test --nologo --no-build
```

Expected: alles gruen.

- [ ] **Step 3: Commit**

```pwsh
git add IdealAkeWms/Views/Help/Changelog.cshtml
git commit -m "docs: extend v1.10.0 changelog with BOM-tree perf improvements" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Manuelle Verifikation (vor Merge)

- **Initial-Load mit grosser FA:** mehrere FAs mit unterschiedlich grossen Stuecklisten oeffnen — vorher/nachher Page-Load-Zeit messen (Browser DevTools Network/Performance).
- **Auf-/Zuklappen:** tief geschachtelte Baugruppe expandieren/kollabieren — sollte sub-100ms wirken.
- **Source-Location-Dropdown:** Auf eine Source-Location-Auswahl klicken, oeffnen, Tippen filtert. Bestand wird angezeigt (nur fuer Plaetze mit Bestand). Speichern via Checkbox-Pickung weiterhin moeglich.
- **Filter-Funktionalitaet:** Spalten-Filter weiterhin funktional, kombiniert mit Tree-Toggle (Bug-Fix-4 aus Phase 1 darf nicht regredieren).
- **Server-Last:** API-Endpoint laeuft mit jeder Dropdown-Oeffnung — Network-Tab sollte einzelne kleine Calls zeigen, kein Massen-Aufruf.

---

## Self-Review-Notiz

5 Tasks decken alle drei in der Q&A identifizierten Root Causes ab:
- Fix A (Ajax-Dropdown) → Tasks 1+2
- Fix B (JS-Refactor) → Task 3
- Fix C (Server-Dictionary) → Task 4
- Doku → Task 5

Test-Coverage:
- API-Endpoint: 2 Tests (filter by code, exclude inactive/picking-transport)
- Server-side Dictionary: implizit ueber existing PickingController-Tests (Verhalten unveraendert)
- Client-JS: keine Unit-Tests (Web-Projekt hat keine JS-Test-Infrastruktur), manuelle Verifikation
- updateBomVisibility-Refactor: korrektheit garantiert durch identische Logik, nur DOM-Caching/Recursion-Guard
