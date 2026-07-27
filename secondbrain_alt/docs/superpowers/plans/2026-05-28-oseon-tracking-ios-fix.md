# OSEON-Tracking iOS-Fix + Performance-Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. **Working directory: `.claude/worktrees/oseon-tracking-ios` — alle Befehle laufen relativ zu diesem Worktree, NICHT zu `main`.**

**Goal:** OSEON-Tracking-Seite auf iOS Safari bedienbar machen via Architektur-Refactor (Top-Level-Liste + Lazy-AJAX-Detail-Load) plus drei iOS-spezifische Defensiv-Fixes (Permission-Pre-Warm vor Modal-Show, html5-qrcode lokal hosten, column-preferences-Init defer).

**Architecture:** `OseonIndex` returnt nur 25 Top-Level-Group-Rows. Neuer `OseonGroupDetails`-Endpoint liefert per AJAX die SubAuftraege + AGs einer Gruppe als PartialView. Gemeinsame Logik (Ampel-/Termin-/Sub-Order-Mapping) in neuem `OseonGroupViewModelBuilder`-Service ausgelagert. Bei aktivem Artikel-Filter Prefetch (kein Lazy-Load) wie heute.

**Tech Stack:** ASP.NET Core 10.0, EF Core 10.0, Razor PartialView, Bootstrap 5, vanilla JavaScript (fetch API), html5-qrcode 2.3.8.

**Spec:** [secondbrain/docs/superpowers/specs/2026-05-28-oseon-tracking-ios-fix-design.md](../specs/2026-05-28-oseon-tracking-ios-fix-design.md) (Commit `d748086` im Worktree)

---

## File Structure

**Modifiziert:**
- `IdealAkeWms/Controllers/TrackingController.cs` — `OseonIndex` umbau auf optionalen Prefetch; neue Action `OseonGroupDetails`
- `IdealAkeWms/Data/Repositories/IOseonProductionOrderRepository.cs` — neue Methoden-Signaturen
- `IdealAkeWms/Data/Repositories/OseonProductionOrderRepository.cs` — Methoden-Implementierungen
- `IdealAkeWms/Views/Tracking/OseonIndex.cshtml` — Sub-Order-Markup ausgelagert; Lazy-Marker-tbody pro Gruppe
- `IdealAkeWms/wwwroot/js/barcode-scanner.js` — Permission-Pre-Warm vor Modal-Show
- `IdealAkeWms/wwwroot/js/column-preferences.js` — Init via `requestIdleCallback`
- `IdealAkeWms/Views/Shared/_Layout.cshtml` — Viewport-Meta erweitert
- `IdealAkeWms/Program.cs` — DI-Registrierung des neuen Builder-Service
- `IdealAkeWms/AppVersion.cs` — 1.16.0
- `IdealAkeWms/Views/Help/Changelog.cshtml` — v1.16.0-Eintrag
- `IdealAkeWms/Views/Help/Index.cshtml` — Lazy-Load-Hinweis bei OSEON-Tracking-Sektion
- `docs/TESTSZENARIEN.md` — Kapitel 30
- `PROJECT_STATUS.md` — Fortschritts-Sektion + Hauptfunktion + Roadmap
- `CLAUDE.md` — iOS/getUserMedia-Fallstrick

**Neu:**
- `IdealAkeWms/Services/Oseon/OseonGroupViewModelBuilder.cs` — Helper-Service fuer ViewModel-Aufbau
- `IdealAkeWms/Services/Oseon/IOseonGroupViewModelBuilder.cs` — Interface
- `IdealAkeWms/Views/Tracking/_OseonGroupDetails.cshtml` — Partial-View mit Sub-Order- + Operation-Markup
- `IdealAkeWms/wwwroot/js/oseon-tracking-lazy.js` — Click-Handler + AJAX-Fetch
- `IdealAkeWms/wwwroot/lib/html5-qrcode/html5-qrcode.min.js` — lokale Kopie
- `IdealAkeWms.Tests/Controllers/TrackingControllerTests.cs` — neue Test-Datei

---

## Task 0: Pre-flight — Sanity Checks

- [ ] **Step 1: Worktree-Pfad verifizieren**

```bash
cd C:\Git\IDEAL-AKE-WMS\.claude\worktrees\oseon-tracking-ios
git branch --show-current
```

Erwartet: `bugfix/oseon-tracking-ios`. Falls nein → STOP, falscher Working-Dir.

- [ ] **Step 2: Bestaetigen das Repo schon eine `GetByCustomerOrderNumberAsync` hat**

```bash
git grep -n "GetByCustomerOrderNumberAsync" -- 'IdealAkeWms/Data/Repositories/*.cs'
```

Erwartet: Treffer in `IOseonProductionOrderRepository.cs` und `OseonProductionOrderRepository.cs` (Zeile 21). Diese Methode existiert bereits — wir koennen sie erweitern oder eine Overload-Variante hinzufuegen (Task 2 entscheidet).

- [ ] **Step 3: Build-Baseline**

```bash
dotnet build IdealAkeWms/IdealAkeWms.csproj -c Debug --nologo
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --no-restore -c Debug --nologo
```

Erwartet: 0 Fehler, ~585+1 Tests gruen (state nach v1.15.3). Falls nein → STOP.

---

## Task 1: OseonGroupViewModelBuilder — Helper-Service extrahieren

**Files:**
- Create: `IdealAkeWms/Services/Oseon/IOseonGroupViewModelBuilder.cs`
- Create: `IdealAkeWms/Services/Oseon/OseonGroupViewModelBuilder.cs`
- Modify: `IdealAkeWms/Program.cs` — DI-Registrierung
- Modify: `IdealAkeWms/Controllers/TrackingController.cs` — OseonIndex nutzt den Builder

**Strategie:** Erst Interface + Implementierung anlegen, dann OseonIndex refactoren auf den Builder. Bestehende Tests muessen weiter gruen sein (Verhalten unveraendert).

### Step 1: Interface anlegen

Datei `IdealAkeWms/Services/Oseon/IOseonGroupViewModelBuilder.cs`:

```csharp
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;

namespace IdealAkeWms.Services.Oseon;

/// <summary>
/// Baut die OseonOrderGroupViewModel-Aggregate (inkl. SubOrders + Operations + Ampelfarben + Termine)
/// aus einer Liste von OseonProductionOrder-Entitaeten auf. Wird sowohl von TrackingController.OseonIndex
/// (im Prefetch-Pfad bei aktivem Artikel-Filter) als auch von TrackingController.OseonGroupDetails
/// (Lazy-Load eines einzelnen Customer-Order-Schluessels) verwendet.
/// </summary>
public interface IOseonGroupViewModelBuilder
{
    Task<OseonOrderGroupViewModel> BuildAsync(
        string customerOrderKey,
        IEnumerable<OseonProductionOrder> subOrders,
        bool useRelevanceFilter,
        string? filterArticle,
        CancellationToken ct = default);
}
```

### Step 2: Implementierung anlegen

Datei `IdealAkeWms/Services/Oseon/OseonGroupViewModelBuilder.cs`:

```csharp
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Helpers;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;

namespace IdealAkeWms.Services.Oseon;

public sealed class OseonGroupViewModelBuilder : IOseonGroupViewModelBuilder
{
    private readonly IOseonTrafficLightService _trafficLightService;
    private readonly IOseonOperationConfigRepository _operationConfigRepository;
    private readonly IBusinessDayService _businessDayService;
    private readonly IHolidayRepository _holidayRepository;

    public OseonGroupViewModelBuilder(
        IOseonTrafficLightService trafficLightService,
        IOseonOperationConfigRepository operationConfigRepository,
        IBusinessDayService businessDayService,
        IHolidayRepository holidayRepository)
    {
        _trafficLightService = trafficLightService;
        _operationConfigRepository = operationConfigRepository;
        _businessDayService = businessDayService;
        _holidayRepository = holidayRepository;
    }

    public async Task<OseonOrderGroupViewModel> BuildAsync(
        string customerOrderKey,
        IEnumerable<OseonProductionOrder> subOrders,
        bool useRelevanceFilter,
        string? filterArticle,
        CancellationToken ct = default)
    {
        var opConfigs = await _operationConfigRepository.GetAllAsDictionaryAsync();
        var holidays = await _holidayRepository.GetHolidayDatesAsync();

        var subOrderViewModels = new List<OseonSubOrderViewModel>();
        foreach (var o in subOrders.OrderBy(o => o.OseonOrderNumber))
        {
            var operations = new List<OseonOperationViewModel>();
            foreach (var op in o.WorkOperations.OrderBy(op => op.PositionNumber))
            {
                var hasConfig = opConfigs.TryGetValue(op.Name, out var opConfig);
                var isRelevant = !hasConfig || opConfig!.IsOseonRelevant;

                DateTime? calculatedDueDate = null;
                if (o.DueDate.HasValue)
                {
                    calculatedDueDate = hasConfig
                        ? OseonDueDateCalculator.Calculate(o.DueDate.Value, opConfig!.DueDateOffsetDays, _businessDayService, holidays)
                        : o.DueDate.Value.Date;
                }

                var opColor = await _trafficLightService.GetColorForOperationAsync(op.OseonStatus, calculatedDueDate);

                operations.Add(new OseonOperationViewModel
                {
                    PositionNumber = op.PositionNumber,
                    Name = op.Name,
                    Description = op.Description,
                    OseonStatus = op.OseonStatus,
                    StatusText = OseonStatusHelper.GetStatusText(op.OseonStatus),
                    StatusBadgeClass = OseonStatusHelper.GetStatusBadgeClass(op.OseonStatus),
                    IsFirstOperation = op.IsFirstOperation,
                    IsLastOperation = op.IsLastOperation,
                    Color = opColor,
                    CalculatedDueDate = calculatedDueDate,
                    IsOseonRelevant = isRelevant,
                });
            }

            int effectiveStatus;
            TrafficLightColor orderColor;
            if (useRelevanceFilter)
            {
                var relevantOps = operations.Where(op => op.IsOseonRelevant).ToList();
                var noRelevantOps = relevantOps.Count == 0 && operations.Count > 0;
                var allRelevantFinished = noRelevantOps || (relevantOps.Count > 0 && relevantOps.All(op => op.OseonStatus is 90 or 95));
                orderColor = allRelevantFinished
                    ? TrafficLightColor.Green
                    : (relevantOps.Count > 0 ? relevantOps.Max(op => op.Color) : TrafficLightColor.Gray);
                effectiveStatus = allRelevantFinished ? 90 : o.OseonStatus;
            }
            else
            {
                orderColor = await _trafficLightService.GetColorAsync(o.OseonStatus, o.DueDate);
                effectiveStatus = o.OseonStatus;
            }

            var maxCalculatedDueDate = operations
                .Where(op => op.CalculatedDueDate.HasValue)
                .Select(op => op.CalculatedDueDate!.Value)
                .DefaultIfEmpty()
                .Max();
            var displayDueDate = maxCalculatedDueDate != default ? maxCalculatedDueDate : o.DueDate;

            subOrderViewModels.Add(new OseonSubOrderViewModel
            {
                Id = o.Id,
                OseonOrderNumber = o.OseonOrderNumber,
                ArticleNumber = o.ArticleNumber,
                Description1 = o.Description1,
                Description2 = o.Description2,
                WorkplaceName = o.WorkplaceName,
                OseonStatus = effectiveStatus,
                StatusText = OseonStatusHelper.GetStatusText(effectiveStatus),
                StatusBadgeClass = OseonStatusHelper.GetStatusBadgeClass(effectiveStatus),
                QuantityTarget = o.QuantityTarget,
                QuantityActual = o.QuantityActual,
                DueDate = displayDueDate,
                Color = orderColor,
                Operations = operations,
            });
        }

        var totalSubsInGroup = subOrderViewModels.Count;
        var finishedSubsInGroup = subOrderViewModels.Count(s => s.OseonStatus is 90 or 95);

        var displaySubs = subOrderViewModels;
        if (!string.IsNullOrWhiteSpace(filterArticle))
        {
            var artTerm = filterArticle.Trim();
            displaySubs = subOrderViewModels
                .Where(s => s.ArticleNumber != null
                            && s.ArticleNumber.Contains(artTerm, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var worstColor = subOrderViewModels.Count > 0
            ? subOrderViewModels.Max(s => s.Color)
            : TrafficLightColor.Gray;
        var worstStatus = subOrderViewModels.Count > 0
            ? GetWorstStatus(subOrderViewModels.Select(s => s.OseonStatus))
            : 0;

        return new OseonOrderGroupViewModel
        {
            CustomerOrderNumber = customerOrderKey,
            WorstColor = worstColor,
            TotalSubOrders = totalSubsInGroup,
            FinishedSubOrders = finishedSubsInGroup,
            GroupStatusText = OseonStatusHelper.GetStatusText(worstStatus),
            GroupStatusBadgeClass = OseonStatusHelper.GetStatusBadgeClass(worstStatus),
            SubOrders = displaySubs,
        };
    }

    private static int GetWorstStatus(IEnumerable<int> statuses)
    {
        var statusList = statuses.ToList();
        int[] priority = [70, 60, 30, 20, 10, 90, 95];
        foreach (var p in priority)
        {
            if (statusList.Contains(p)) return p;
        }
        return statusList.FirstOrDefault();
    }
}
```

### Step 3: DI-Registrierung in Program.cs

In `IdealAkeWms/Program.cs` — neue Zeile bei den Service-Registrierungen einfuegen:

```csharp
builder.Services.AddScoped<IdealAkeWms.Services.Oseon.IOseonGroupViewModelBuilder,
                          IdealAkeWms.Services.Oseon.OseonGroupViewModelBuilder>();
```

(Position: vor `builder.Services.AddControllersWithViews(...)` o.ae. — siehe Konvention im File. Falls unklar: am Ende der `AddScoped<...>`-Block einfuegen.)

### Step 4: TrackingController.OseonIndex auf den Builder umstellen

Datei `IdealAkeWms/Controllers/TrackingController.cs`:

- Im Konstruktor neuen Parameter `IOseonGroupViewModelBuilder groupBuilder` ergaenzen (als letzten Parameter, Konvention "Service-Deps am Ende")
- Feld `_groupBuilder` ergaenzen
- In `OseonIndex` (Zeile 226+) den inline `foreach (var g in pagedResult.Items.GroupBy(...))`-Block (Zeilen 256-292 inkl. der GetWorstStatus-Logik) ersetzen durch:

```csharp
var groups = new List<OseonOrderGroupViewModel>();
foreach (var g in pagedResult.Items.GroupBy(o => o.CustomerOrderNumber ?? o.OseonOrderNumber))
{
    var group = await _groupBuilder.BuildAsync(g.Key, g, useRelevanceFilter, filterArticle, HttpContext.RequestAborted);
    groups.Add(group);
}

groups = groups
    .OrderByDescending(g => g.WorstColor)
    .ThenBy(g => g.CustomerOrderNumber)
    .ToList();
```

Die private `GetWorstStatus`-Methode in `TrackingController.cs` ist nun unbenutzt und kann entfernt werden.

### Step 5: Build verifizieren

```bash
dotnet build IdealAkeWms/IdealAkeWms.csproj -c Debug --nologo
```

Erwartet: 0 Fehler.

### Step 6: Tests laufen

```bash
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --no-restore -c Debug --nologo
```

Erwartet: alle ~585+ Tests gruen. Bestehende Tests, die OseonIndex prueefen, muessen ohne Anpassung gruen sein (Builder ist transparent — gleiches Verhalten).

### Step 7: Commit

```bash
git add IdealAkeWms/Services/Oseon/IOseonGroupViewModelBuilder.cs IdealAkeWms/Services/Oseon/OseonGroupViewModelBuilder.cs IdealAkeWms/Program.cs IdealAkeWms/Controllers/TrackingController.cs
git commit -m "refactor(oseon): extract OseonGroupViewModelBuilder service from TrackingController.OseonIndex"
```

---

## Task 2: Repository — GetByCustomerOrderNumberAsync mit voller Filter-Param-Liste

**Files:**
- Modify: `IdealAkeWms/Data/Repositories/IOseonProductionOrderRepository.cs`
- Modify: `IdealAkeWms/Data/Repositories/OseonProductionOrderRepository.cs`

**Strategie:** Bestehende `GetByCustomerOrderNumberAsync(string customerOrderNumber)` ist zu simpel — sie laedt nicht zwingend WorkOperations mit. Wir fuegen eine Overload-Variante hinzu, die analog zu `GetPagedAsync` arbeitet aber auf einen einzelnen `CustomerOrderNumber` filtert.

### Step 1: Interface erweitern

In `IdealAkeWms/Data/Repositories/IOseonProductionOrderRepository.cs` neue Methoden-Signatur ergaenzen (nach der bestehenden `GetByCustomerOrderNumberAsync`):

```csharp
/// <summary>
/// Laedt alle Sub-Orders + WorkOperations einer einzelnen Kundenauftrag-Gruppe.
/// Wird vom OseonGroupDetails-AJAX-Endpoint genutzt fuer Lazy-Load.
/// </summary>
Task<List<OseonProductionOrder>> GetSubOrdersForCustomerOrderAsync(
    string customerOrderNumber,
    bool showFinished,
    HashSet<string>? relevantOperationNames,
    CancellationToken ct = default);
```

### Step 2: Implementierung ergaenzen

In `OseonProductionOrderRepository.cs` (nach der bestehenden `GetByCustomerOrderNumberAsync` einfuegen):

```csharp
public async Task<List<OseonProductionOrder>> GetSubOrdersForCustomerOrderAsync(
    string customerOrderNumber,
    bool showFinished,
    HashSet<string>? relevantOperationNames,
    CancellationToken ct = default)
{
    IQueryable<OseonProductionOrder> query = _context.OseonProductionOrders
        .Include(o => o.WorkOperations)
        .Where(o => o.CustomerOrderNumber == customerOrderNumber);

    if (!showFinished)
    {
        query = query.Where(o => o.OseonStatus != 90 && o.OseonStatus != 95);
    }

    var results = await query.ToListAsync(ct);

    if (relevantOperationNames != null && relevantOperationNames.Count > 0)
    {
        foreach (var order in results)
        {
            order.WorkOperations = order.WorkOperations
                .Where(op => relevantOperationNames.Contains(op.Name) || !relevantOperationNames.Any())
                .ToList();
        }
    }

    return results;
}
```

**Wichtig:** Die `relevantOperationNames`-Filterung ist Post-Hoc im Code (nicht im SQL), weil der `OseonGroupViewModelBuilder` die `isRelevant`-Berechnung selbst nochmal braucht (fuer das Markup). Beide Schichten haben denselben Filter — DRY-Verstoss in Kauf genommen weil die Builder-Logik ohne `isRelevant`-Info auf der Operation auch nicht funktioniert.

**Alternative-Hinweis fuer den Engineer:** Wenn die `OseonProductionOrder.WorkOperations`-Navigation eine `IList` ist und EF Core sie nicht zurueckschreibt, ist das `.ToList()`-Assignment oben sicher. Falls Read-Only-Issue auftaucht: einen anonymen Projection-Schritt vorschalten.

### Step 3: Build verifizieren

```bash
dotnet build IdealAkeWms/IdealAkeWms.csproj -c Debug --nologo
```

Erwartet: 0 Fehler.

### Step 4: Commit

```bash
git add IdealAkeWms/Data/Repositories/IOseonProductionOrderRepository.cs IdealAkeWms/Data/Repositories/OseonProductionOrderRepository.cs
git commit -m "feat(oseon): add GetSubOrdersForCustomerOrderAsync repo method"
```

---

## Task 3: PartialView _OseonGroupDetails.cshtml + TrackingController.OseonGroupDetails Action

**Files:**
- Create: `IdealAkeWms/Views/Tracking/_OseonGroupDetails.cshtml`
- Modify: `IdealAkeWms/Controllers/TrackingController.cs` — neue Action

### Step 1: PartialView erstellen

Datei `IdealAkeWms/Views/Tracking/_OseonGroupDetails.cshtml`:

```html
@model IdealAkeWms.Models.ViewModels.OseonOrderGroupViewModel

@* Das gleiche Markup wie die Zeilen 168-247 der bestehenden OseonIndex.cshtml,
   aber empfaengt die OseonOrderGroupViewModel.SubOrders + Operations als Model. *@

@foreach (var subOrder in Model.SubOrders)
{
    <tr class="oseon-tree-sub"
        data-sort-oseon-order-number="@subOrder.OseonOrderNumber"
        data-sort-article-number="@subOrder.ArticleNumber"
        data-sort-workplace="@subOrder.WorkplaceName"
        data-sort-status="@subOrder.OseonStatus"
        data-sort-due-date="@(subOrder.DueDate?.ToString("yyyy-MM-dd") ?? "")">
        @* Hier kommt das exakte Markup der heutigen Sub-Order-Zeilen aus OseonIndex.cshtml:174-213 hin.
           Der Engineer kopiert das Markup 1:1 aus OseonIndex.cshtml, ersetzt Variablen-Namen
           passend zum lokalen Modell (subOrder statt s). *@
        <td>...</td>
    </tr>

    @if (subOrder.Operations.Any())
    {
        <tr class="oseon-tree-op-container">
            <td colspan="N">
                <div class="collapse oseon-tree-ops-collapse" id="ops-@subOrder.Id">
                    @foreach (var op in subOrder.Operations)
                    {
                        <tr class="oseon-tree-op @(op.IsOseonRelevant ? "" : "oseon-op-irrelevant")">
                            @* Markup aus OseonIndex.cshtml:224-244 hier exakt uebernehmen *@
                            <td>...</td>
                        </tr>
                    }
                </div>
            </td>
        </tr>
    }
}
```

**Hinweis fuer den Engineer:** Das Markup-Detail kopiert der Engineer 1:1 aus der bestehenden `OseonIndex.cshtml` Zeilen 174-244. Der Refactor von OseonIndex selbst kommt in Task 4 — bis dahin ist die View **doppelt** (sowohl OseonIndex.cshtml als auch _OseonGroupDetails.cshtml haben das Markup). Das ist absichtlich um die Tasks unabhaengig zu halten — Task 4 entfernt dann das duplicate aus OseonIndex.

Auch das `colspan="N"` muss auf die richtige Anzahl Spalten gesetzt werden — dieselbe wie in OseonIndex.cshtml heute.

### Step 2: TrackingController.OseonGroupDetails Action

In `IdealAkeWms/Controllers/TrackingController.cs` neue Action ergaenzen (nach `OseonIndex`):

```csharp
[RequireTrackingAccess]
public async Task<IActionResult> OseonGroupDetails(
    string customerOrderNumber,
    bool useRelevanceFilter = true,
    bool showFinished = false,
    string? filterArticle = null,
    CancellationToken ct = default)
{
    if (string.IsNullOrWhiteSpace(customerOrderNumber))
    {
        return BadRequest("customerOrderNumber is required.");
    }

    var opConfigs = await _operationConfigRepository.GetAllAsDictionaryAsync();
    HashSet<string>? relevantOpNames = null;
    if (useRelevanceFilter)
    {
        relevantOpNames = opConfigs
            .Where(kvp => kvp.Value.IsOseonRelevant)
            .Select(kvp => kvp.Key)
            .ToHashSet();
    }

    var subOrders = await _oseonRepository.GetSubOrdersForCustomerOrderAsync(
        customerOrderNumber, showFinished, relevantOpNames, ct);

    if (subOrders.Count == 0)
    {
        return NotFound();
    }

    var group = await _groupBuilder.BuildAsync(
        customerOrderNumber, subOrders, useRelevanceFilter, filterArticle, ct);

    return PartialView("_OseonGroupDetails", group);
}
```

### Step 3: Build + Tests verifizieren

```bash
dotnet build IdealAkeWms/IdealAkeWms.csproj -c Debug --nologo
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --no-restore -c Debug --nologo
```

Erwartet: 0 Fehler, alle Tests bleiben gruen.

### Step 4: Manueller Smoke-Test

```bash
# In einer separaten PowerShell:
cd C:\Git\IDEAL-AKE-WMS\.claude\worktrees\oseon-tracking-ios
dotnet run --project IdealAkeWms/IdealAkeWms.csproj
```

Im Browser: `http://localhost:5xxx/Tracking/OseonGroupDetails?customerOrderNumber=<echte-nummer>` → erwartet HTML-Response mit Sub-Order-Rows.

Falls keine echte Customer-Order-Nummer bekannt: ueberspringen — kommt in Task 10 (Tests) wieder.

### Step 5: Commit

```bash
git add IdealAkeWms/Views/Tracking/_OseonGroupDetails.cshtml IdealAkeWms/Controllers/TrackingController.cs
git commit -m "feat(oseon): add _OseonGroupDetails partial + OseonGroupDetails AJAX action"
```

---

## Task 4: OseonIndex.cshtml — Refactor auf Lazy-Marker-Tbodys

**Files:**
- Modify: `IdealAkeWms/Views/Tracking/OseonIndex.cshtml`

**Strategie:** Entferne den Sub-Order/Operation-Markup-Block (Zeilen 168-247). Ersetze pro Gruppe durch einen leeren `<tbody class="oseon-group-details" data-loaded="..." data-customer-order="...">`. Bei aktivem `filterArticle` wird der `_OseonGroupDetails`-Partial inline gerendert und `data-loaded="true"` gesetzt.

### Step 1: ViewModel-Erweiterung (Kennzeichnung "ist prefetched")

In `IdealAkeWms/Models/ViewModels/OseonTrackingViewModel.cs` (oder wo `OseonOrderGroupViewModel` definiert ist), neue Property `IsPrefetched` (bool) ergaenzen:

```csharp
public bool IsPrefetched { get; set; }
```

### Step 2: Controller setzt IsPrefetched

In `TrackingController.OseonIndex`, nach dem `_groupBuilder.BuildAsync`-Aufruf:

```csharp
var group = await _groupBuilder.BuildAsync(g.Key, g, useRelevanceFilter, filterArticle, HttpContext.RequestAborted);
group.IsPrefetched = !string.IsNullOrWhiteSpace(filterArticle);
groups.Add(group);
```

### Step 3: OseonIndex.cshtml — Sub-Order-Block durch Marker-Tbody ersetzen

Bestehende Loop-Struktur:

```html
@foreach (var group in Model.OrderGroups)
{
    <tbody>
        <tr class="oseon-tree-group">...</tr>

        @* HIER waren bisher die Sub-Order-Rows direkt eingebettet (Zeilen 168-247) *@
        @foreach (var subOrder in group.SubOrders) { ... }
    </tbody>
}
```

Neu:

```html
@foreach (var group in Model.OrderGroups)
{
    <tbody>
        <tr class="oseon-tree-group" data-customer-order="@group.CustomerOrderNumber">
            @* bestehender Header-Markup unveraendert *@
            ...
        </tr>
    </tbody>
    <tbody class="oseon-group-details"
           data-loaded="@(group.IsPrefetched ? "true" : "false")"
           data-customer-order="@group.CustomerOrderNumber"
           style="@(group.IsPrefetched ? "" : "display: none;")">
        @if (group.IsPrefetched)
        {
            @await Html.PartialAsync("_OseonGroupDetails", group)
        }
    </tbody>
}
```

**Wichtig:** Das Tbody ist separat (eigenes `<tbody>`) statt verschachtelt im Header-Tbody — HTML erlaubt mehrere `<tbody>` in einer Tabelle, das stoert die Bootstrap-Styles nicht. Vorteil: Toggle einfacher per JS.

### Step 4: Toggle-Chevron-Click-Handler-Markierung

In der `<tr class="oseon-tree-group">` muss der Chevron-Click jetzt nicht mehr direkt das `<tbody>` anzeigen, sondern den AJAX-Handler triggern. Vorhandenes JS in OseonIndex.cshtml (Inline-Script-Block) anpassen:

- Bisheriger Toggle-Code (vermutlich `document.querySelectorAll(".oseon-tree-group").forEach(...)` mit show/hide auf Sub-Order-Rows) → ersetzen durch Click-Handler, der `oseon-tracking-lazy.js` (Task 5) ruft.

**Engineer-Hinweis:** Wenn die bestehende OseonIndex.cshtml Toggle-Logik komplex ist, kann sie zunaechst stehen bleiben und in Task 5 wird sie ueberschrieben. Das ist OK — Tests in Task 10 fangen Regressionen.

### Step 5: Build + Smoke-Test

```bash
dotnet build IdealAkeWms/IdealAkeWms.csproj -c Debug --nologo
```

Browser: `http://localhost:5xxx/Tracking/OseonIndex` → Top-Level-Gruppen sichtbar, Sub-Orders aktuell unsichtbar (display:none). Klick auf Chevron noch ohne Effekt (kommt in Task 5).

`http://localhost:5xxx/Tracking/OseonIndex?filterArticle=<irgendwas>` → Treffer-Gruppen sind sichtbar mit prefetched Sub-Orders.

### Step 6: Commit

```bash
git add IdealAkeWms/Views/Tracking/OseonIndex.cshtml IdealAkeWms/Models/ViewModels/OseonTrackingViewModel.cs IdealAkeWms/Controllers/TrackingController.cs
git commit -m "refactor(oseon): replace inline sub-order markup with lazy-tbody markers"
```

---

## Task 5: oseon-tracking-lazy.js — AJAX-Click-Handler

**Files:**
- Create: `IdealAkeWms/wwwroot/js/oseon-tracking-lazy.js`
- Modify: `IdealAkeWms/Views/Tracking/OseonIndex.cshtml` — Script-Tag einfuegen

### Step 1: JS-Datei erstellen

Datei `IdealAkeWms/wwwroot/js/oseon-tracking-lazy.js`:

```javascript
(function () {
    'use strict';

    const SPINNER_HTML = `
        <tr class="oseon-lazy-spinner">
            <td colspan="100" class="text-center py-3">
                <div class="spinner-border spinner-border-sm" role="status"></div>
                <span class="ms-2">Lade Details...</span>
            </td>
        </tr>`;

    function showError(tbody, msg) {
        tbody.innerHTML = `
            <tr class="oseon-lazy-error">
                <td colspan="100" class="text-center py-3 text-danger">
                    Fehler beim Laden: ${escapeHtml(msg)}
                    <button type="button" class="btn btn-sm btn-outline-secondary ms-2 oseon-lazy-retry">
                        Erneut versuchen
                    </button>
                </td>
            </tr>`;
    }

    function escapeHtml(s) {
        const div = document.createElement('div');
        div.textContent = s;
        return div.innerHTML;
    }

    function getQueryParam(name) {
        const params = new URLSearchParams(window.location.search);
        return params.get(name);
    }

    async function loadGroupDetails(customerOrderNumber, tbody) {
        if (tbody.dataset.loaded === 'true' || tbody.dataset.loading === 'true') return;
        tbody.dataset.loading = 'true';
        tbody.innerHTML = SPINNER_HTML;
        tbody.style.display = '';

        try {
            const useRelevance = getQueryParam('useRelevanceFilter') ?? 'true';
            const showFinished = getQueryParam('showFinished') ?? 'false';
            const filterArticle = getQueryParam('filterArticle') ?? '';

            const params = new URLSearchParams({
                customerOrderNumber: customerOrderNumber,
                useRelevanceFilter: useRelevance,
                showFinished: showFinished,
            });
            if (filterArticle) params.set('filterArticle', filterArticle);

            const url = `/Tracking/OseonGroupDetails?${params.toString()}`;
            const resp = await fetch(url, { headers: { 'Accept': 'text/html' } });
            if (!resp.ok) {
                throw new Error(`HTTP ${resp.status}`);
            }
            const html = await resp.text();
            tbody.innerHTML = html;
            tbody.dataset.loaded = 'true';
        } catch (err) {
            showError(tbody, err.message);
        } finally {
            tbody.dataset.loading = 'false';
        }
    }

    function toggleGroup(groupRow) {
        const customerOrder = groupRow.dataset.customerOrder;
        if (!customerOrder) return;
        const tbody = document.querySelector(
            `tbody.oseon-group-details[data-customer-order="${CSS.escape(customerOrder)}"]`);
        if (!tbody) return;

        if (tbody.dataset.loaded === 'true') {
            tbody.style.display = (tbody.style.display === 'none') ? '' : 'none';
            return;
        }

        loadGroupDetails(customerOrder, tbody);
    }

    function onClick(e) {
        const groupRow = e.target.closest('tr.oseon-tree-group');
        if (!groupRow) return;
        const retryBtn = e.target.closest('button.oseon-lazy-retry');
        if (retryBtn) {
            const tbody = retryBtn.closest('tbody.oseon-group-details');
            if (tbody) {
                tbody.dataset.loaded = 'false';
                const customerOrder = tbody.dataset.customerOrder;
                if (customerOrder) loadGroupDetails(customerOrder, tbody);
            }
            return;
        }
        toggleGroup(groupRow);
    }

    document.addEventListener('DOMContentLoaded', function () {
        document.addEventListener('click', onClick);
    });
})();
```

### Step 2: Script in OseonIndex.cshtml einbinden

Am Ende der bestehenden `@section Scripts { ... }` (oder vor dem schliessenden `</script>` des Inline-Blocks) ergaenzen:

```html
<script src="~/js/oseon-tracking-lazy.js" asp-append-version="true"></script>
```

**Wichtig:** Wenn die bestehende Inline-JS-Toggle-Logik fuer Gruppen-Rows noch da ist, jetzt entfernen, weil das neue Script die Aufgabe uebernimmt.

### Step 3: Manueller Test

```bash
dotnet run --project IdealAkeWms/IdealAkeWms.csproj
```

Browser:
1. `http://localhost:5xxx/Tracking/OseonIndex` (kein Filter) → Gruppen sichtbar, klick auf Chevron → Spinner → SubOrders laden.
2. Zweiter Click → Toggle Hide.
3. Dritter Click → Toggle Show (kein AJAX).
4. Mit `?filterArticle=...` → Treffer-Gruppen direkt sichtbar mit SubOrders.

### Step 4: Build + Tests

```bash
dotnet build IdealAkeWms/IdealAkeWms.csproj -c Debug --nologo
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --no-restore -c Debug --nologo
```

Erwartet: alles gruen.

### Step 5: Commit

```bash
git add IdealAkeWms/wwwroot/js/oseon-tracking-lazy.js IdealAkeWms/Views/Tracking/OseonIndex.cshtml
git commit -m "feat(oseon): lazy-load AJAX handler for group details"
```

---

## Task 6: html5-qrcode lokal hosten

**Files:**
- Create: `IdealAkeWms/wwwroot/lib/html5-qrcode/html5-qrcode.min.js`
- Modify: `IdealAkeWms/Views/Tracking/OseonIndex.cshtml` — Script-Src umstellen

### Step 1: Library herunterladen

```bash
mkdir -p IdealAkeWms/wwwroot/lib/html5-qrcode
curl -L https://unpkg.com/html5-qrcode@2.3.8/html5-qrcode.min.js \
     -o IdealAkeWms/wwwroot/lib/html5-qrcode/html5-qrcode.min.js
```

(Alternativ via PowerShell `Invoke-WebRequest -Uri ... -OutFile ...`.)

Erwartet: Datei ist ca. 320 KB gross.

### Step 2: Script-Tag in OseonIndex.cshtml umstellen

In `IdealAkeWms/Views/Tracking/OseonIndex.cshtml` Zeile 303:

```html
@* vorher *@
<script src="https://unpkg.com/html5-qrcode@2.3.8/html5-qrcode.min.js"></script>

@* nachher *@
<script src="~/lib/html5-qrcode/html5-qrcode.min.js" asp-append-version="true"></script>
```

### Step 3: Build + Smoke-Test

```bash
dotnet build IdealAkeWms/IdealAkeWms.csproj -c Debug --nologo
dotnet run --project IdealAkeWms/IdealAkeWms.csproj
```

Browser: OseonIndex laden, Devtools Network-Tab → `html5-qrcode.min.js` wird von `/lib/html5-qrcode/...` (lokal) geladen, nicht von unpkg.com.

### Step 4: Commit

```bash
git add IdealAkeWms/wwwroot/lib/html5-qrcode/html5-qrcode.min.js IdealAkeWms/Views/Tracking/OseonIndex.cshtml
git commit -m "feat(scanner): host html5-qrcode locally instead of CDN"
```

---

## Task 7: barcode-scanner.js — Permission-Pre-Warm fuer iOS

**Files:**
- Modify: `IdealAkeWms/wwwroot/js/barcode-scanner.js`

**Strategie:** Im Click-Handler bevor das Modal geoeffnet wird, `getUserMedia` synchron im User-Gesture-Stack aufrufen (Permission-Prompt erscheint sofort). Bei OK: Stream wieder stoppen + Modal-Show + html5-qrcode wie heute. Damit ist iOS Safari zufrieden.

### Step 1: Funktion `requestCameraPermission` ergaenzen

Am Anfang der `barcode-scanner.js` (nach den existierenden Hilfsfunktionen), neue Funktion einfuegen:

```javascript
/**
 * Pre-Warm der Camera-Permission im synchronen User-Gesture-Stack.
 * Notwendig fuer iOS Safari, das nach Modal-Show keine Permission mehr erteilt.
 * Bei Erfolg: Stream sofort wieder stoppen (wir wollen nur die Permission).
 * Bei Fehler: Promise rejected, Caller kann auf Fallback-UI wechseln.
 */
async function requestCameraPermission() {
    if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
        throw new Error('Camera API not supported');
    }
    const stream = await navigator.mediaDevices.getUserMedia({
        video: { facingMode: 'environment' }
    });
    // Stream sofort wieder stoppen
    stream.getTracks().forEach(track => track.stop());
}
```

### Step 2: openScannerModal anpassen

In `openScannerModal` (Zeile 28+), die ersten Zeilen so umbauen, dass die Permission VOR dem Modal-Erstellen angefragt wird:

```javascript
async function openScannerModal(targetSelectId, scanType, qrFaEnabled, faTargetId) {
    // iOS-Fix: Permission im synchronen User-Gesture-Stack anfragen,
    // BEVOR das Modal geoeffnet wird. html5-qrcode-Library kommt erst spaeter dran.
    let cameraAvailable = false;
    if (isCameraSupported() && isSecureContext()) {
        try {
            await requestCameraPermission();
            cameraAvailable = true;
        } catch (err) {
            console.warn('Camera permission pre-warm failed, falling back to file upload:', err);
            cameraAvailable = false;
        }
    }

    // ... ab hier weiter wie bisher, mit `cameraAvailable` als state ...
}
```

Die bestehende `isCameraSupported()`-Logik im Modal-Body bleibt — aber der Camera-Block wird nur dann gerendert, wenn `cameraAvailable === true`. Wenn `false`: File-Upload-Fallback wie heute.

**Engineer-Hinweis:** Die exakte Stelle wo das alte `isCameraSupported() && isSecureContext()`-Check im Code steht, muss ggf. auf den neuen `cameraAvailable`-Wert umgebaut werden. Es kann auch sein, dass der bestehende Code zwei verschiedene Check-Stellen hat — beide auf `cameraAvailable` umstellen.

### Step 3: form.submit() in setTimeout(50) wrappen

In `IdealAkeWms/Views/Tracking/OseonIndex.cshtml` Zeile 410-413 (der `initTextInputScanner`-Callback fuer Artikel-Filter):

```javascript
@* vorher *@
initTextInputScanner('btnScanArticle', 'filterArticle', 'article', function() {
    var form = document.getElementById('filterArticle').closest('form');
    if (form) form.submit();
});

@* nachher *@
initTextInputScanner('btnScanArticle', 'filterArticle', 'article', function() {
    var form = document.getElementById('filterArticle').closest('form');
    if (form) {
        // iOS-Fix: setTimeout damit Virtual-Keyboard-State-Teardown vor Navigation komplett ist.
        setTimeout(function () { form.submit(); }, 50);
    }
});
```

Falls es einen analogen Block fuer `btnScanCustomerOrder` gibt (Zeile ~407): dort identisch wrappen.

### Step 4: Build + Smoke-Test

```bash
dotnet build IdealAkeWms/IdealAkeWms.csproj -c Debug --nologo
dotnet run --project IdealAkeWms/IdealAkeWms.csproj
```

Browser (Desktop Chrome): QR-Button klicken → Browser fragt Permission → Modal oeffnet → html5-qrcode startet.

iOS-Test: spaeter manuell.

### Step 5: Commit

```bash
git add IdealAkeWms/wwwroot/js/barcode-scanner.js IdealAkeWms/Views/Tracking/OseonIndex.cshtml
git commit -m "fix(ios): pre-warm camera permission in user-gesture stack before modal-show"
```

---

## Task 8: column-preferences.js — Init via requestIdleCallback defer

**Files:**
- Modify: `IdealAkeWms/wwwroot/js/column-preferences.js`

### Step 1: DOMContentLoaded-Init wrappen

In `column-preferences.js` Zeile 845 — der bestehende `DOMContentLoaded`-Listener startet sofort `loadSettings(...)`. Stattdessen via `requestIdleCallback` defern:

```javascript
@* vorher (Zeile 845) *@
document.addEventListener('DOMContentLoaded', function () {
    _table = document.querySelector('table[data-view-key]');
    if (!_table) return;
    if (!readConfig()) {
        document.dispatchEvent(new CustomEvent('column-preferences-ready'));
        return;
    }
    loadSettings(function () {
        applySettings();
        insertGearButton();
        attachContextMenus();
        attachResizeHandles();
        // ...
        document.dispatchEvent(new CustomEvent('column-preferences-ready'));
    });
});

@* nachher *@
document.addEventListener('DOMContentLoaded', function () {
    _table = document.querySelector('table[data-view-key]');
    if (!_table) return;

    var initFn = function () {
        if (!readConfig()) {
            document.dispatchEvent(new CustomEvent('column-preferences-ready'));
            return;
        }
        loadSettings(function () {
            applySettings();
            insertGearButton();
            attachContextMenus();
            attachResizeHandles();
            // ... existing observer/setup code ...
            document.dispatchEvent(new CustomEvent('column-preferences-ready'));
        });
    };

    // Defer init bis Browser idle ist — entlastet First-Paint auf langsamen Geraeten (iOS).
    if (typeof window.requestIdleCallback === 'function') {
        window.requestIdleCallback(initFn, { timeout: 500 });
    } else {
        setTimeout(initFn, 100);
    }
});
```

### Step 2: Build + Smoke-Test

```bash
dotnet build IdealAkeWms/IdealAkeWms.csproj -c Debug --nologo
dotnet run --project IdealAkeWms/IdealAkeWms.csproj
```

Browser: irgendeine Liste mit `column-preferences` (z.B. FA-Liste) → die Spalten-Sichtbarkeit/Sortierung wird nach kurzem Delay angewendet, aber visuell unmerklich.

### Step 3: Commit

```bash
git add IdealAkeWms/wwwroot/js/column-preferences.js
git commit -m "perf(ui): defer column-preferences init via requestIdleCallback"
```

---

## Task 9: Viewport-Meta erweitern

**Files:**
- Modify: `IdealAkeWms/Views/Shared/_Layout.cshtml`

### Step 1: Viewport-Tag erweitern

In `IdealAkeWms/Views/Shared/_Layout.cshtml` Zeile 8:

```html
@* vorher *@
<meta name="viewport" content="width=device-width, initial-scale=1.0" />

@* nachher *@
<meta name="viewport" content="width=device-width, initial-scale=1.0, viewport-fit=cover" />
```

`viewport-fit=cover` sorgt auf iPhones mit Notch fuer korrektes Layout. `user-scalable=yes` bewusst NICHT gesetzt — der Browser-Default ist bereits "yes", und ein explizites `yes` macht keinen Unterschied (im Gegenteil, ein `user-scalable=no` waere problematisch).

### Step 2: Build verifizieren

```bash
dotnet build IdealAkeWms/IdealAkeWms.csproj -c Debug --nologo
```

### Step 3: Commit

```bash
git add IdealAkeWms/Views/Shared/_Layout.cshtml
git commit -m "fix(ios): viewport-meta erweitert (viewport-fit=cover)"
```

---

## Task 10: TrackingController-Tests — neue Datei

**Files:**
- Create: `IdealAkeWms.Tests/Controllers/TrackingControllerTests.cs`

### Step 1: Test-Datei erstellen

Datei `IdealAkeWms.Tests/Controllers/TrackingControllerTests.cs`:

```csharp
using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Services.Oseon;
using IdealAkeWms.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace IdealAkeWms.Tests.Controllers;

public class TrackingControllerTests
{
    private static TrackingController BuildController(
        Mock<IOseonProductionOrderRepository>? oseonRepo = null,
        Mock<IOseonGroupViewModelBuilder>? builder = null,
        Mock<IOseonOperationConfigRepository>? opConfigRepo = null)
    {
        oseonRepo ??= new Mock<IOseonProductionOrderRepository>();
        builder ??= new Mock<IOseonGroupViewModelBuilder>();
        opConfigRepo ??= new Mock<IOseonOperationConfigRepository>();
        opConfigRepo
            .Setup(r => r.GetAllAsDictionaryAsync())
            .ReturnsAsync(new Dictionary<string, OseonOperationConfig>());

        var workOpRepo = new Mock<IWorkOperationRepository>();
        var workplaceRepo = new Mock<IProductionWorkplaceRepository>();
        var currentUser = new FakeCurrentUserService();
        var trafficLight = new Mock<IOseonTrafficLightService>();
        var businessDays = new Mock<IBusinessDayService>();
        var holidayRepo = new Mock<IHolidayRepository>();
        holidayRepo.Setup(r => r.GetHolidayDatesAsync()).ReturnsAsync(new HashSet<DateOnly>());

        return new TrackingController(
            workOpRepo.Object,
            workplaceRepo.Object,
            currentUser,
            oseonRepo.Object,
            trafficLight.Object,
            opConfigRepo.Object,
            businessDays.Object,
            holidayRepo.Object,
            builder.Object);
    }

    [Fact]
    public async Task OseonGroupDetails_returns_NotFound_for_unknown_customer_order()
    {
        var oseonRepo = new Mock<IOseonProductionOrderRepository>();
        oseonRepo
            .Setup(r => r.GetSubOrdersForCustomerOrderAsync(
                It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<HashSet<string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OseonProductionOrder>());

        var ctrl = BuildController(oseonRepo);

        var result = await ctrl.OseonGroupDetails("UNKNOWN-12345");

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OseonGroupDetails_returns_BadRequest_for_empty_customer_order()
    {
        var ctrl = BuildController();
        var result = await ctrl.OseonGroupDetails("");
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task OseonGroupDetails_returns_PartialView_with_built_group()
    {
        var oseonRepo = new Mock<IOseonProductionOrderRepository>();
        oseonRepo
            .Setup(r => r.GetSubOrdersForCustomerOrderAsync(
                "K-100", false, It.IsAny<HashSet<string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OseonProductionOrder>
            {
                new OseonProductionOrder { Id = 1, OseonOrderNumber = "K-100-1", CustomerOrderNumber = "K-100" }
            });

        var builder = new Mock<IOseonGroupViewModelBuilder>();
        var fakeGroup = new OseonOrderGroupViewModel
        {
            CustomerOrderNumber = "K-100",
            SubOrders = new List<OseonSubOrderViewModel>()
        };
        builder
            .Setup(b => b.BuildAsync("K-100", It.IsAny<IEnumerable<OseonProductionOrder>>(),
                                     true, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeGroup);

        var ctrl = BuildController(oseonRepo, builder);

        var result = await ctrl.OseonGroupDetails("K-100");

        var partial = result.Should().BeOfType<PartialViewResult>().Subject;
        partial.ViewName.Should().Be("_OseonGroupDetails");
        partial.Model.Should().Be(fakeGroup);
    }

    [Fact]
    public async Task OseonGroupDetails_includes_finished_when_showFinished_true()
    {
        var oseonRepo = new Mock<IOseonProductionOrderRepository>();
        oseonRepo
            .Setup(r => r.GetSubOrdersForCustomerOrderAsync(
                "K-200", true, It.IsAny<HashSet<string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OseonProductionOrder>
            {
                new OseonProductionOrder { Id = 2, OseonOrderNumber = "K-200-1", CustomerOrderNumber = "K-200", OseonStatus = 90 }
            });

        var builder = new Mock<IOseonGroupViewModelBuilder>();
        builder
            .Setup(b => b.BuildAsync(It.IsAny<string>(), It.IsAny<IEnumerable<OseonProductionOrder>>(),
                                     It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OseonOrderGroupViewModel { CustomerOrderNumber = "K-200" });

        var ctrl = BuildController(oseonRepo, builder);

        var result = await ctrl.OseonGroupDetails("K-200", showFinished: true);

        result.Should().BeOfType<PartialViewResult>();
        oseonRepo.Verify(r => r.GetSubOrdersForCustomerOrderAsync(
            "K-200", true, It.IsAny<HashSet<string>?>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

### Step 2: Tests laufen

```bash
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --no-restore -c Debug --nologo --filter "FullyQualifiedName~TrackingControllerTests"
```

Erwartet: 4 Tests gruen.

```bash
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --no-restore -c Debug --nologo
```

Erwartet: alle ~589 Tests gruen (585 vorher + 4 neu).

### Step 3: Commit

```bash
git add IdealAkeWms.Tests/Controllers/TrackingControllerTests.cs
git commit -m "test(oseon): add TrackingController.OseonGroupDetails tests"
```

---

## Task 11: Version-Bump v1.16.0 + Changelog

**Files:**
- Modify: `IdealAkeWms/AppVersion.cs` — 1.16.0
- Modify: `IdealAkeWms/Views/Help/Changelog.cshtml`
- Modify: `IdealAkeWms/Views/Help/Index.cshtml`

### Step 1: AppVersion bumpen (NUR Web — Service bleibt)

`IdealAkeWms/AppVersion.cs`:
```csharp
public const string Version = "1.16.0";
public const string Date = "2026-05-28";
```

`IDEALAKEWMSService/AppVersion.cs` bleibt unveraendert (1.15.3 / 2026-05-27).

### Step 2: Changelog-Eintrag

In `IdealAkeWms/Views/Help/Changelog.cshtml`, neuer Card-Block ueber dem v1.15.3-Eintrag:

```html
<div class="card mb-3">
    <div class="card-header text-white" style="background-color: var(--ake-primary);">
        <strong>v1.16.0</strong> <span class="text-white-50 ms-2">28.05.2026</span>
    </div>
    <div class="card-body">
        <h6>OSEON-Tracking: iOS-Tauglichkeit + Performance-Refactor</h6>
        <ul>
            <li><strong>Lazy-Load der Subauftraege:</strong> Die OSEON-Tracking-Seite rendert
                jetzt nur die 25 Top-Level-Kundenauftrag-Zeilen. Beim Aufklappen einer Gruppe werden
                die Subauftraege + Arbeitsgaenge per AJAX nachgeladen. Spart ~95% des initialen
                DOM-Volumens und macht die Seite auf iOS Safari bedienbar.</li>
            <li><strong>iOS-Safari-Bug-Fixes:</strong> Der QR-Code-Button funktioniert jetzt
                zuverlaessig (Permission-Pre-Warm im User-Gesture-Stack), html5-qrcode wird
                lokal gehostet statt vom CDN, und Spalten-Einstellungen werden mit
                <code>requestIdleCallback</code> defered geladen.</li>
            <li><strong>Artikel-Filter unveraendert:</strong> Bei gesetztem Artikel-Filter werden
                die Treffer-Gruppen weiterhin direkt mit ihren Subauftraegen geladen (Server-Side-Prefetch).</li>
            <li><em>Hinweis:</em> Beim ersten Aufklappen einer Gruppe ist ein kurzes Spinner-Symbol
                sichtbar (typisch &lt; 200ms). Beim erneuten Aufklappen wird der DOM gecached.</li>
        </ul>
    </div>
</div>
```

### Step 3: Help/Index.cshtml — Lazy-Load-Notiz

In `IdealAkeWms/Views/Help/Index.cshtml` — beim Block zur OSEON-Teileverfolgung (Suche nach "OSEON" o.ae., um die richtige Stelle zu finden) ergaenzen:

```html
<p>
    <strong>Seit v1.16.0:</strong> Die Subauftraege werden beim Aufklappen einer Gruppe geladen
    (Lazy-Load). Ein kurzer Spinner zeigt das Nachladen; danach bleiben die Details im DOM.
    Bei aktivem Artikel-Filter sind die Treffer direkt expanded.
</p>
```

### Step 4: Build verifizieren

```bash
dotnet build IdealAkeWms/IdealAkeWms.csproj -c Debug --nologo
```

### Step 5: Commit

```bash
git add IdealAkeWms/AppVersion.cs IdealAkeWms/Views/Help/Changelog.cshtml IdealAkeWms/Views/Help/Index.cshtml
git commit -m "feat(version): bump to v1.16.0 (oseon iOS-fix + lazy-load)"
```

---

## Task 12: Doku — TESTSZENARIEN + PROJECT_STATUS + CLAUDE.md

**Files:**
- Modify: `docs/TESTSZENARIEN.md`
- Modify: `PROJECT_STATUS.md`
- Modify: `CLAUDE.md`

### Step 1: TESTSZENARIEN Kapitel 30

Am Ende von `docs/TESTSZENARIEN.md` anhaengen:

```markdown
## Kapitel 30: OSEON-Tracking iOS-Fix + Lazy-Load (v1.16.0)

### Szenario 30.1: Desktop Chrome — Initial-Load-Performance
**Schritt:**
1. `/Tracking/OseonIndex` ohne Filter oeffnen.
2. Devtools-Network-Tab: Hauptrequest abwarten.
**Erwartet:** Erster Paint < 1s, 25 Group-Header-Zeilen sichtbar, alle initial collapsed.

### Szenario 30.2: Desktop Chrome — Lazy-Aufklappen
**Schritt:**
1. Klick auf Chevron einer Gruppe.
**Erwartet:** Spinner-Row erscheint, dann SubAuftrags-Zeilen einblenden < 500ms.
Zweites Click-Toggle: instant (kein AJAX im Network-Tab).

### Szenario 30.3: Desktop Chrome — Artikel-Filter Prefetch
**Schritt:**
1. URL: `/Tracking/OseonIndex?filterArticle=12345` (echte Treffer-Nummer).
**Erwartet:** Treffer-Gruppen sind initial expanded sichtbar. Kein zusaetzlicher AJAX-Call beim Aufklappen.

### Szenario 30.4: iOS Safari — Bedienbarkeit
**Schritt:**
1. Page auf aktuellem iOS Safari oeffnen (iPhone oder iPad, kein Desktop-Mode).
2. Artikel-Filter-Input antippen, "12345" eintippen, Filtern.
**Erwartet:** Scrolling fluessig, Tipp-Input ohne Verzoegerung, Form-Submit ohne Lock-up.

### Szenario 30.5: iOS Safari — QR-Code-Scan
**Schritt:**
1. Auf "Artikelnummer scannen" tippen.
**Erwartet:** Permission-Prompt (Kamera-Zugriff) erscheint sofort. Bei Accept: Modal mit Kamera-Vorschau. Scan eines QR-Codes -> Filter-Input ist gefuellt, Form auto-submitted nach 50ms.

### Szenario 30.6: iOS Safari — QR-Fallback
**Schritt:**
1. In Safari-Settings den Kamera-Zugriff fuer die Seite explizit verweigern.
2. Page neu laden, QR-Button antippen.
**Erwartet:** Statt Kamera-Modal erscheint File-Upload-UI. User kann Bild auswaehlen, das wird gescannt.
```

### Step 2: PROJECT_STATUS.md aktualisieren

In der "Aktueller Fortschritt (laufend)"-Sektion neue Sub-Sektion vor den anderen einfuegen:

```markdown
### v1.16.0 — OSEON-Tracking iOS-Fix + Performance-Refactor

| # | Sub-Task | Status |
|---|---------|--------|
| 0 | Pre-flight + Baseline-Build | ✅ erledigt |
| 1 | OseonGroupViewModelBuilder Helper-Service extrahiert | ✅ erledigt |
| 2 | Repository: GetSubOrdersForCustomerOrderAsync ergaenzt | ✅ erledigt |
| 3 | PartialView _OseonGroupDetails + OseonGroupDetails Action | ✅ erledigt |
| 4 | OseonIndex.cshtml auf Lazy-Marker-Tbodys refactored | ✅ erledigt |
| 5 | oseon-tracking-lazy.js mit AJAX-Click-Handler | ✅ erledigt |
| 6 | html5-qrcode lokal gehostet (statt CDN) | ✅ erledigt |
| 7 | barcode-scanner.js: Permission-Pre-Warm im User-Gesture-Stack | ✅ erledigt |
| 8 | column-preferences.js: Init via requestIdleCallback defered | ✅ erledigt |
| 9 | Viewport-Meta erweitert (viewport-fit=cover) | ✅ erledigt |
| 10 | TrackingControllerTests (4 Tests neu) | ✅ erledigt |
| 11 | Version-Bump v1.16.0 + Changelog + Help/Index | ✅ erledigt |
| 12 | TESTSZENARIEN + PROJECT_STATUS + CLAUDE.md | ⏳ in Arbeit (dieser Task) |
```

Hauptfunktionen-Tabelle ergaenzen:
```markdown
| OSEON-Tracking Lazy-Load + iOS-Tauglichkeit | Fertig (v1.16.0) |
```

Roadmap:
```markdown
- v1.16.0 (2026-05-28) — OSEON-Tracking iOS-Fix + Lazy-Load-Refactor. 50x weniger initiales DOM, QR-Scanner-Permission-Pre-Warm, html5-qrcode lokal.
```

### Step 3: CLAUDE.md — neuer Fallstrick

Im "Bekannte Fallstricke"-Block einen neuen Punkt:

```markdown
- **iOS Safari + getUserMedia (seit v1.16.0)**: `navigator.mediaDevices.getUserMedia()` muss im **synchronen User-Gesture-Stack** aufgerufen werden — also direkt im Click-Handler, **bevor** asynchrone Operationen (Modal-Show, await) laufen. iOS Safari verweigert sonst die Permission. Pattern: erst `await requestCameraPermission()` (Pre-Warm im Click), dann Modal-Show + Scanner-Init. Siehe `barcode-scanner.js` und Spec [secondbrain/docs/superpowers/specs/2026-05-28-oseon-tracking-ios-fix-design.md](secondbrain/docs/superpowers/specs/2026-05-28-oseon-tracking-ios-fix-design.md) §4.3.
```

### Step 4: Build + komplette Test-Suite

```bash
dotnet build IdealAkeWms/IdealAkeWms.csproj -c Debug --nologo
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --no-restore -c Debug --nologo
```

Erwartet: alle Builds 0 errors, ~589 Tests gruen.

### Step 5: Commit

```bash
git add docs/TESTSZENARIEN.md PROJECT_STATUS.md CLAUDE.md
git commit -m "docs(oseon): testszenarien, project-status, claude.md for v1.16.0"
```

### Step 6: PROJECT_STATUS finalisieren

Sub-Task 12 auf ✅ setzen + Commit-Hash:

```bash
# PROJECT_STATUS.md editieren — Sub-Task 12 status auf ✅ erledigt mit Hash
git add PROJECT_STATUS.md
git commit -m "docs(project-status): mark v1.16.0 oseon iOS-fix rollout complete"
```

---

## Task 13: Merge in main + Worktree-Cleanup

**Files:**
- (Branches: `bugfix/oseon-tracking-ios` -> `main`)

### Step 1: Manuelle iOS-Tests durchfuehren

Vor dem Merge in main: User testet die Page auf einem echten iOS-Geraet. Wenn alle TESTSZENARIEN 30.4-30.6 bestanden sind → weiter zu Step 2. Wenn nicht → Issues fixen, neue Commits im Worktree, dann erneut testen.

### Step 2: Worktree-Branch in main mergen

```bash
cd C:\Git\IDEAL-AKE-WMS   # zurueck in main-Worktree
git merge bugfix/oseon-tracking-ios --no-ff -m "merge bugfix/oseon-tracking-ios into main (v1.16.0)"
git push origin main
```

### Step 3: Worktree aufraeumen

```bash
git worktree remove .claude/worktrees/oseon-tracking-ios
git branch -d bugfix/oseon-tracking-ios
```

Falls Permission-Denied auf Worktree-Remove (Windows-Lock): `rm -rf .claude/worktrees/oseon-tracking-ios`.

### Step 4: Final-Check auf main

```bash
git log --oneline -20
dotnet build IdealAkeWms/IdealAkeWms.csproj -c Debug --nologo
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --no-restore -c Debug --nologo
```

Erwartet: Commits aus dem Worktree sichtbar in main, Build + Tests gruen.

---

## Validierung am Schluss

- [ ] **Final-Check 1:** `git log --oneline d748086..HEAD` (im Worktree) zeigt ~14 atomare Commits.
- [ ] **Final-Check 2:** `dotnet test` ueber das Web-Test-Projekt → alle gruen, +4 neue Tests.
- [ ] **Final-Check 3:** Manuelles UI-Smoke auf Desktop Chrome (Szenarien 30.1-30.3).
- [ ] **Final-Check 4:** Manuelles iOS-Safari-Smoke (Szenarien 30.4-30.6) — vor Merge in main!
- [ ] **Final-Check 5:** Merge in main + Worktree-Cleanup.
