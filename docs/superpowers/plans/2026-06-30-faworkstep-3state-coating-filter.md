# FaWorkStep 3-State-Status + Beschichtungstermin + Filter-nach-ENTER — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Den FA-Vorbau-Erledigt-Status von einem Boolean auf einen 3-Wert-Status (Offen/in Bearbeitung/Fertig) als Dropdown in Abarbeitungsliste + Leitstand umstellen, den Beschichtungstermin in der Abarbeitungsliste ergänzen und Server-Mode-Spaltenfilter erst bei ENTER navigieren lassen.

**Architecture:** (1) `FaWorkStep.IsCompleted` (bool) wird durch `Status` (Enum `FaWorkStepStatus` Offen=0/InBearbeitung=1/Fertig=2) ersetzt — Migration 76 konvertiert (IsCompleted=1→Fertig) und dropt die alte Spalte; alle Lese-/Schreibstellen (Repo, API, 2 Controller, 2 Views, ViewModel-Pivot) ziehen mit; ein gemeinsames Razor-Partial + ein gemeinsames JS rendern/steuern das Dropdown. (2) Die bereits im Leitstand existierende Beschichtungstermin-Formel wird in einen Helper `CoatingDateCalculator` extrahiert und in der Abarbeitungsliste als neue filterbare Spalte genutzt. (3) `table-filter.js` löst im Server-Mode die Navigation nur noch bei ENTER aus (Kalender/Clear/programmatisch weiterhin sofort), Client-Mode bleibt live.

**Tech Stack:** ASP.NET Core 10 MVC, EF Core 10 (SQL Server), xUnit + FluentAssertions + Moq + EF InMemory, Vanilla-JS (`wwwroot/js`), Razor Views.

**Branch/Worktree:** Weiterarbeit auf `feature/windows-auth-ad-users` im bestehenden Worktree `C:\Git\IDEAL-AKE-WMS\.claude\worktrees\missingparts-include-pd`. **KEIN Merge nach main**, **kein Worktree-/Branch-Cleanup** ohne ausdrücklichen User-Wunsch.

**Version:** Bump auf **v1.24.0** (Datum 2026-06-30). Die Vorarbeit auf diesem Branch trägt v1.23.0 (Windows-Auth) — die drei neuen Features bekommen eine eigene Changelog-Karte v1.24.0.

**Spec:** [docs/superpowers/specs/2026-06-25-faworkstep-3state-coating-filter-design.md](../specs/2026-06-25-faworkstep-3state-coating-filter-design.md)

**Commits (PowerShell, kein Git-Bash):** Vor jedem Commit `Set-Location 'C:\Git\IDEAL-AKE-WMS\.claude\worktrees\missingparts-include-pd'`. Commit-Messages via Single-Quote-Here-String (`@'` … `'@`, schließendes `'@` an Spaltenposition 0). Co-Authored-By-Footer beibehalten.

---

## File Structure

**Neu:**
- `IdealAkeWms/Models/FaWorkStepStatus.cs` — Enum Offen=0/InBearbeitung=1/Fertig=2 (eine Verantwortung: der 3-Wert-Status).
- `IdealAkeWms/Models/ViewModels/FaWorkStepStatusSelectViewModel.cs` — VM für das gemeinsame Status-Dropdown-Partial.
- `IdealAkeWms/Views/Shared/_FaWorkStepStatusSelect.cshtml` — gemeinsames `<select>`-Partial (Abarbeitungsliste + Leitstand).
- `IdealAkeWms/wwwroot/js/fa-work-step-status.js` — gemeinsamer AJAX-Handler für `.fa-status-select` (POST `set-status`).
- `IdealAkeWms/Services/CoatingDateCalculator.cs` — extrahierte Beschichtungstermin-Formel (DRY Leitstand + Abarbeitungsliste).
- `IdealAkeWms/Migrations/<ts>_ReplaceFaWorkStepIsCompletedWithStatus.cs` (+ `.Designer.cs`, Snapshot-Update) — generiert.
- `SQL/76_ReplaceFaWorkStepIsCompletedWithStatus.sql` — idempotentes Migrationsskript.
- `IdealAkeWms.Tests/Services/CoatingDateCalculatorTests.cs` — Unit-Tests des Helpers.

**Geändert (Change 1 — Type-Swap, compile-gekoppelt):**
- `IdealAkeWms/Models/FaWorkStep.cs` — `bool IsCompleted` → `FaWorkStepStatus Status`.
- `IdealAkeWms/Data/Repositories/IFaWorkStepRepository.cs` — Record `FaWorkStepPivotCell` + Methode `SetStatusAsync`.
- `IdealAkeWms/Data/Repositories/FaWorkStepRepository.cs` — `SetStatusAsync` + Pivot-Projektion.
- `IdealAkeWms/Models/ViewModels/FaWorklistViewModel.cs` — `FaWorklistCell.Status` (+ später `FaWorklistRow.BeschichtungTermin`).
- `IdealAkeWms/Controllers/FaWorkStepsApiController.cs` — `toggle-completed` → `set-status`.
- `IdealAkeWms/Controllers/FaWorklistController.cs` — Hide-Logik + Cell-Mapping (+ später Coating).
- `IdealAkeWms/Controllers/PickingLeitstandController.cs` — VK-VA-Filtertext (+ später Coating-Refactor).
- `IdealAkeWms/Views/FaWorklist/Index.cshtml` — Dropdown statt Checkbox + JS-Include (+ später Coating-Spalte).
- `IdealAkeWms/Views/PickingLeitstand/Index.cshtml` — 5 VK-VA-Dropdowns statt Checkboxen + JS-Include + toggle-field-Handler bereinigen.
- `SQL/00_FreshInstall.sql` — FaWorkSteps-Spalte + History-Insert.

**Geändert (Tests):**
- `IdealAkeWms.Tests/Repositories/FaWorkStepRepositoryTests.cs`
- `IdealAkeWms.Tests/Controllers/FaWorkStepsApiControllerTests.cs`
- `IdealAkeWms.Tests/Controllers/FaWorklistControllerTests.cs`
- `IdealAkeWms.Tests/Controllers/PickingLeitstandControllerTests.cs`
- `IdealAkeWms.Tests/Controllers/FaCompletionControllerTests.cs`

**Geändert (Change 3 + Doku):**
- `IdealAkeWms/wwwroot/js/table-filter.js`
- `IdealAkeWms/AppVersion.cs`, `IDEALAKEWMSService/AppVersion.cs`
- `IdealAkeWms/Views/Help/Changelog.cshtml`, `IdealAkeWms/Views/Help/Index.cshtml`
- `docs/TESTSZENARIEN.md`, `PROJECT_STATUS.md`, `CLAUDE.md`

---

## Task 0: Pre-Flight Baseline

**Files:** keine Änderungen.

- [ ] **Step 1: Branch + Worktree verifizieren**

Run (PowerShell):
```powershell
Set-Location 'C:\Git\IDEAL-AKE-WMS\.claude\worktrees\missingparts-include-pd'
git rev-parse --abbrev-ref HEAD
```
Expected: `feature/windows-auth-ad-users`.

- [ ] **Step 2: Baseline Build + Tests grün**

Run:
```powershell
dotnet build IdealAkeWms\IdealAkeWms.csproj
dotnet test
```
Expected: Build erfolgreich, alle Tests grün. (Falls rot: STOP, melden — kein Feature auf rote Baseline aufsetzen.)

- [ ] **Step 3: Höchste Migration bestätigen**

Bestätige, dass `IdealAkeWms/Migrations/20260625061803_ReplaceUserDefaultWorkplaceWithWorkbenches.cs` die neueste Migration ist und das SQL-Skript `SQL/75_*.sql` existiert → die neue Migration wird `SQL/76_*.sql`.

---

## Task 1: Change 1 — 3-State-Status (Produktionscode + Razor + Migration + Schema)

> Diese Task ist compile-gekoppelt: der Typwechsel `bool IsCompleted` → `FaWorkStepStatus Status` bricht den Build in mehreren Dateien, bis ALLE Consumer gezogen sind. Erst danach lässt sich die Migration generieren. Tests werden in Task 2 nachgezogen. Verifikation dieser Task ist der **Web-Projekt-Build** (nicht die Solution/Tests).

**Files:**
- Create: `IdealAkeWms/Models/FaWorkStepStatus.cs`
- Create: `IdealAkeWms/Models/ViewModels/FaWorkStepStatusSelectViewModel.cs`
- Create: `IdealAkeWms/Views/Shared/_FaWorkStepStatusSelect.cshtml`
- Create: `IdealAkeWms/wwwroot/js/fa-work-step-status.js`
- Modify: `IdealAkeWms/Models/FaWorkStep.cs`
- Modify: `IdealAkeWms/Data/Repositories/IFaWorkStepRepository.cs`
- Modify: `IdealAkeWms/Data/Repositories/FaWorkStepRepository.cs`
- Modify: `IdealAkeWms/Models/ViewModels/FaWorklistViewModel.cs`
- Modify: `IdealAkeWms/Controllers/FaWorkStepsApiController.cs`
- Modify: `IdealAkeWms/Controllers/FaWorklistController.cs`
- Modify: `IdealAkeWms/Controllers/PickingLeitstandController.cs`
- Modify: `IdealAkeWms/Views/FaWorklist/Index.cshtml`
- Modify: `IdealAkeWms/Views/PickingLeitstand/Index.cshtml`
- Generate + edit: Migration `<ts>_ReplaceFaWorkStepIsCompletedWithStatus`
- Create: `SQL/76_ReplaceFaWorkStepIsCompletedWithStatus.sql`
- Modify: `SQL/00_FreshInstall.sql`

- [ ] **Step 1: Enum anlegen**

`IdealAkeWms/Models/FaWorkStepStatus.cs`:
```csharp
namespace IdealAkeWms.Models;

/// <summary>
/// Erledigt-Status eines FA-Vorbau-Arbeitsgangs (FA-Abarbeitungsliste + Leitstand-VK-VA).
/// Ersetzt das frühere bool IsCompleted (v1.24.0). Nur Fertig blendet die FA aus der
/// Abarbeitungsliste aus; InBearbeitung bleibt sichtbar.
/// </summary>
public enum FaWorkStepStatus
{
    Offen = 0,
    InBearbeitung = 1,
    Fertig = 2
}
```

- [ ] **Step 2: Model umstellen**

`IdealAkeWms/Models/FaWorkStep.cs` — ersetze:
```csharp
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? CompletedBy { get; set; }
```
durch:
```csharp
    /// <summary>Erledigt-Status (Abarbeitungsliste + Leitstand). Fertig blendet die FA aus.</summary>
    public FaWorkStepStatus Status { get; set; }
    public DateTime? CompletedAt { get; set; }   // gesetzt wenn Status==Fertig, sonst null
    public string? CompletedBy { get; set; }
```

- [ ] **Step 3: Pivot-Record + Repo-Interface umstellen**

`IdealAkeWms/Data/Repositories/IFaWorkStepRepository.cs` — ersetze die Record-Zeile:
```csharp
/// <summary>Detail-Pivot-Zelle: FaWorkStep-Id + Erledigt-Status (IsCompleted) eines aktiven AGs.</summary>
public record FaWorkStepPivotCell(int FaWorkStepId, bool IsCompleted);
```
durch:
```csharp
/// <summary>Detail-Pivot-Zelle: FaWorkStep-Id + Erledigt-Status eines aktiven AGs.</summary>
public record FaWorkStepPivotCell(int FaWorkStepId, FaWorkStepStatus Status);
```
und ersetze die Methodensignatur:
```csharp
    /// <summary>Setzt IsCompleted + CompletedAt/By bzw. null.</summary>
    Task SetIsCompletedAsync(int faWorkStepId, bool value, string modifiedBy, string modifiedByWindows);
```
durch:
```csharp
    /// <summary>Setzt Status + CompletedAt/By (bei Fertig) bzw. null.</summary>
    Task SetStatusAsync(int faWorkStepId, FaWorkStepStatus status, string modifiedBy, string modifiedByWindows);
```
(`using IdealAkeWms.Models;` ist bereits in Zeile 1 vorhanden.)

- [ ] **Step 4: Repo-Implementierung umstellen**

`IdealAkeWms/Data/Repositories/FaWorkStepRepository.cs` — in `GetWorkStepDetailPivotAsync` ersetze die Projektion + das Dict-Befüllen:
```csharp
                .Select(f => new { f.ProductionOrderId, f.WorkStep.Code, f.Id, f.IsCompleted })
```
durch:
```csharp
                .Select(f => new { f.ProductionOrderId, f.WorkStep.Code, f.Id, f.Status })
```
und:
```csharp
                dict[r.Code] = new FaWorkStepPivotCell(r.Id, r.IsCompleted);
```
durch:
```csharp
                dict[r.Code] = new FaWorkStepPivotCell(r.Id, r.Status);
```
Ersetze die gesamte Methode `SetIsCompletedAsync` durch:
```csharp
    public async Task SetStatusAsync(int faWorkStepId, FaWorkStepStatus status, string modifiedBy, string modifiedByWindows)
    {
        var row = await _context.FaWorkSteps.FirstOrDefaultAsync(f => f.Id == faWorkStepId)
            ?? throw new InvalidOperationException($"FaWorkStep row missing for Id {faWorkStepId}.");

        row.Status = status;
        var done = status == FaWorkStepStatus.Fertig;
        row.CompletedAt = done ? DateTime.Now : null;
        row.CompletedBy = done ? modifiedBy : null;
        row.ModifiedAt = DateTime.Now;
        row.ModifiedBy = modifiedBy;
        row.ModifiedByWindows = modifiedByWindows;
        await _context.SaveChangesAsync();
    }
```

- [ ] **Step 5: ViewModel-Cell umstellen**

`IdealAkeWms/Models/ViewModels/FaWorklistViewModel.cs` — ergänze oben den Using (nach `using IdealAkeWms.Data.Repositories;`):
```csharp
using IdealAkeWms.Models;
```
und ersetze die Klasse `FaWorklistCell`:
```csharp
public class FaWorklistCell
{
    public int FaWorkStepId { get; set; }
    public bool IsCompleted { get; set; }
}
```
durch:
```csharp
public class FaWorklistCell
{
    public int FaWorkStepId { get; set; }
    public FaWorkStepStatus Status { get; set; }
}
```

- [ ] **Step 6: API `set-status`**

`IdealAkeWms/Controllers/FaWorkStepsApiController.cs` — ergänze oben `using IdealAkeWms.Models;`. Ersetze die Zeile:
```csharp
    public record ToggleCompletedRequest(int FaWorkStepId, bool Value);
```
durch:
```csharp
    public record SetStatusRequest(int FaWorkStepId, int Status);
```
und ersetze die gesamte Action `ToggleCompleted` (das `[HttpPost("toggle-completed")]`-Block):
```csharp
    [HttpPost("toggle-completed")]
    [RequireVorbauOrPickingOrLeitstandAccess] // Abarbeitungsliste (vorbau) + Leitstand-VK-VA (picking/leitstand)
    public async Task<IActionResult> ToggleCompleted([FromBody] ToggleCompletedRequest req)
    {
        var row = await _faWorkStepRepository.GetByIdAsync(req.FaWorkStepId);
        if (row == null) return NotFound();

        await _faWorkStepRepository.SetIsCompletedAsync(req.FaWorkStepId, req.Value,
            _currentUserService.GetDisplayName(), _currentUserService.GetWindowsUserName());
        return Ok();
    }
```
durch:
```csharp
    [HttpPost("set-status")]
    [RequireVorbauOrPickingOrLeitstandAccess] // Abarbeitungsliste (vorbau) + Leitstand-VK-VA (picking/leitstand)
    public async Task<IActionResult> SetStatus([FromBody] SetStatusRequest req)
    {
        if (!Enum.IsDefined(typeof(FaWorkStepStatus), req.Status))
            return BadRequest(new { error = $"Ungueltiger Status: {req.Status}" });

        var row = await _faWorkStepRepository.GetByIdAsync(req.FaWorkStepId);
        if (row == null) return NotFound();

        await _faWorkStepRepository.SetStatusAsync(req.FaWorkStepId, (FaWorkStepStatus)req.Status,
            _currentUserService.GetDisplayName(), _currentUserService.GetWindowsUserName());
        return Ok();
    }
```

- [ ] **Step 7: FaWorklistController Hide-Logik + Cell**

`IdealAkeWms/Controllers/FaWorklistController.cs` — ersetze:
```csharp
            // Default: erledigte FAs (gewaehlter AG IsCompleted) ausblenden.
            if (!showDone && selectedStep.IsCompleted)
            {
                continue;
            }
```
durch:
```csharp
            // Default: fertige FAs (gewaehlter AG Status==Fertig) ausblenden — InBearbeitung bleibt sichtbar.
            if (!showDone && selectedStep.Status == FaWorkStepStatus.Fertig)
            {
                continue;
            }
```
und ersetze:
```csharp
                WorkStepCell = new FaWorklistCell
                {
                    FaWorkStepId = selectedStep.Id,
                    IsCompleted = selectedStep.IsCompleted,
                },
```
durch:
```csharp
                WorkStepCell = new FaWorklistCell
                {
                    FaWorkStepId = selectedStep.Id,
                    Status = selectedStep.Status,
                },
```

- [ ] **Step 8: PickingLeitstandController VK-VA-Filtertext**

`IdealAkeWms/Controllers/PickingLeitstandController.cs` — ersetze die Methode `FormatWorkStepForFilter` (inkl. XML-Doc) durch:
```csharp
    /// <summary>
    /// Gerenderter Zellentext fuer eine VK-VA-Spalte (damit der Filter sinnvoll bleibt):
    /// AG nicht anwendbar -> "" (leere Zelle), sonst der Status-Text "offen"/"in bearbeitung"/"fertig".
    /// </summary>
    private static string FormatWorkStepForFilter(PickingLeitstandItem item, string code)
    {
        if (!item.WorkSteps.TryGetValue(code, out var cell)) return string.Empty;
        return cell.Status switch
        {
            FaWorkStepStatus.Fertig => "fertig",
            FaWorkStepStatus.InBearbeitung => "in bearbeitung",
            _ => "offen"
        };
    }
```

- [ ] **Step 9: Status-Dropdown-VM + Partial**

`IdealAkeWms/Models/ViewModels/FaWorkStepStatusSelectViewModel.cs`:
```csharp
namespace IdealAkeWms.Models.ViewModels;

/// <summary>VM fuer das gemeinsame 3-State-Status-Dropdown (_FaWorkStepStatusSelect).</summary>
public class FaWorkStepStatusSelectViewModel
{
    public int FaWorkStepId { get; set; }
    public Models.FaWorkStepStatus Status { get; set; }
    public bool Disabled { get; set; }
}
```

`IdealAkeWms/Views/Shared/_FaWorkStepStatusSelect.cshtml`:
```cshtml
@using IdealAkeWms.Models
@model IdealAkeWms.Models.ViewModels.FaWorkStepStatusSelectViewModel
<select class="form-select form-select-sm fa-status-select" data-fa-work-step-id="@Model.FaWorkStepId" @(Model.Disabled ? "disabled" : "")>
    <option value="0" selected="@(Model.Status == FaWorkStepStatus.Offen)">Offen</option>
    <option value="1" selected="@(Model.Status == FaWorkStepStatus.InBearbeitung)">in Bearbeitung</option>
    <option value="2" selected="@(Model.Status == FaWorkStepStatus.Fertig)">Fertig</option>
</select>
```

- [ ] **Step 10: Gemeinsamer JS-Handler**

`IdealAkeWms/wwwroot/js/fa-work-step-status.js`:
```javascript
// Gemeinsamer AJAX-Handler fuer das 3-State-Status-Dropdown (.fa-status-select) der FA-Vorbau-AGs.
// Verwendet in FA-Abarbeitungsliste (FaWorklist) UND Leitstand (VK-VA).
// Sendet POST /api/fa-work-steps/set-status { faWorkStepId, status }. Fehlerfall: voriger Wert zurueck.
(function () {
    'use strict';

    // Vorigen Wert beim Fokus merken — change feuert erst NACH dem Wechsel.
    document.addEventListener('focus', function (e) {
        var sel = e.target && e.target.closest ? e.target.closest('.fa-status-select') : null;
        if (sel) sel.dataset.prevValue = sel.value;
    }, true);

    document.addEventListener('change', function (e) {
        var sel = e.target.closest('.fa-status-select');
        if (!sel) return;

        var faWorkStepId = parseInt(sel.getAttribute('data-fa-work-step-id'));
        var status = parseInt(sel.value);

        fetch('/api/fa-work-steps/set-status', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ faWorkStepId: faWorkStepId, status: status })
        }).then(function (resp) {
            if (!resp.ok) {
                sel.value = sel.dataset.prevValue || '0';
                alert('Fehler beim Speichern.');
            }
        }).catch(function () {
            sel.value = sel.dataset.prevValue || '0';
            alert('Fehler beim Speichern.');
        });
    });
})();
```

- [ ] **Step 11: FaWorklist-View — Dropdown statt Checkbox + JS-Include**

`IdealAkeWms/Views/FaWorklist/Index.cshtml` — ersetze die Erledigt-Zelle (der `<td class="text-center">`-Block mit `worklist-complete`):
```cshtml
                            <td class="text-center">
                                @if (item.WorkStepCell != null)
                                {
                                    <input type="checkbox" class="form-check-input worklist-complete"
                                           data-fa-work-step-id="@item.WorkStepCell.FaWorkStepId" @(item.WorkStepCell.IsCompleted ? "checked" : "") />
                                }
                            </td>
```
durch:
```cshtml
                            <td class="text-center">
                                @if (item.WorkStepCell != null)
                                {
                                    @await Html.PartialAsync("_FaWorkStepStatusSelect", new IdealAkeWms.Models.ViewModels.FaWorkStepStatusSelectViewModel
                                    {
                                        FaWorkStepId = item.WorkStepCell.FaWorkStepId,
                                        Status = item.WorkStepCell.Status,
                                    })
                                }
                            </td>
```
Ersetze im `@section Scripts`-Block das gesamte Inline-`<script>` (der `document.addEventListener('change', …'.worklist-complete'…)`-Block) durch einen Script-Include. D. h. ersetze:
```cshtml
    <script>
        // Erledigt-Toggle per AJAX — Event-Delegation, damit es keine per-Row-Handler braucht.
        // Fehlerfall: Checkbox zuruecksetzen (Muster Leitstand-Toggle-Fields).
        document.addEventListener('change', function (e) {
            var cb = e.target.closest('.worklist-complete');
            if (!cb) return;

            var faWorkStepId = parseInt(cb.getAttribute('data-fa-work-step-id'));
            var value = cb.checked;

            fetch('/api/fa-work-steps/toggle-completed', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ faWorkStepId: faWorkStepId, value: value })
            }).then(function (resp) {
                if (!resp.ok) {
                    cb.checked = !value;
                    alert('Fehler beim Speichern.');
                }
            }).catch(function () {
                cb.checked = !value;
                alert('Fehler beim Speichern.');
            });
        });
    </script>
```
durch:
```cshtml
    <script src="~/js/fa-work-step-status.js" asp-append-version="true"></script>
```

- [ ] **Step 12: Leitstand-View — 5 VK-VA-Dropdowns + JS-Include + toggle-field bereinigen**

`IdealAkeWms/Views/PickingLeitstand/Index.cshtml` — ersetze die fünf VK/VL/VE/VT/VA-Zellen (jede ist ein `<td class="text-center"> @if (item.WorkSteps.TryGetValue("XX", out var xx)) { <input … toggle-field … fa-work-steps/toggle-completed …/> } </td>`) durch die Partial-Variante. Für **VK**:
```cshtml
                    <td class="text-center">
                        @if (item.WorkSteps.TryGetValue("VK", out var vk))
                        {
                            @await Html.PartialAsync("_FaWorkStepStatusSelect", new IdealAkeWms.Models.ViewModels.FaWorkStepStatusSelectViewModel { FaWorkStepId = vk.FaWorkStepId, Status = vk.Status, Disabled = !Model.CanPick })
                        }
                    </td>
```
Analog für **VL** (`vl`), **VE** (`ve`), **VT** (`vt`), **VA** (`va`) — jeweils denselben Block mit dem passenden Code/Variablennamen.

Im `@section Scripts` (nach der `<script src="~/js/table-filter.js" …>`-Zeile) den gemeinsamen Handler einbinden:
```cshtml
    <script src="~/js/fa-work-step-status.js" asp-append-version="true"></script>
```
Im `toggle-field`-Handler den nun toten `fa-work-steps/toggle-completed`-Zweig entfernen. Ersetze:
```javascript
                    var body;
                    if (endpoint === '/api/fa-work-steps/toggle-completed') {
                        body = { faWorkStepId: parseInt(cb.dataset.faWorkStepId), value: value };
                    } else if (endpoint === '/api/picking-status/toggle' || endpoint === '/api/bde-status/toggle') {
                        body = { productionOrderId: id, field: field, value: value };
                    } else {
                        console.error('Unknown toggle-field endpoint:', endpoint);
                        cb.checked = !value;
                        return;
                    }
```
durch:
```javascript
                    var body;
                    if (endpoint === '/api/picking-status/toggle' || endpoint === '/api/bde-status/toggle') {
                        body = { productionOrderId: id, field: field, value: value };
                    } else {
                        console.error('Unknown toggle-field endpoint:', endpoint);
                        cb.checked = !value;
                        return;
                    }
```
Den Kommentar darüber anpassen (die Zeile „fa-work-steps/toggle-completed sendet …" entfernen).

- [ ] **Step 13: Web-Projekt-Build grün**

Run:
```powershell
dotnet build IdealAkeWms\IdealAkeWms.csproj
```
Expected: Build erfolgreich (Tests-Projekt ist hier noch rot — wird in Task 2 gezogen; `dotnet ef` baut nur das Web-Projekt).

- [ ] **Step 14: Migration generieren**

Run:
```powershell
dotnet ef migrations add ReplaceFaWorkStepIsCompletedWithStatus --project IdealAkeWms\IdealAkeWms.csproj --startup-project IdealAkeWms\IdealAkeWms.csproj
```
Notiere die generierte MigrationId (Dateiname-Timestamp, z. B. `20260630HHMMSS_ReplaceFaWorkStepIsCompletedWithStatus`). Diese ID wird in Step 16 + 17 benötigt.

- [ ] **Step 15: Migration-Body von Hand auf Daten-Konvertierung umschreiben**

Öffne die generierte `IdealAkeWms/Migrations/<ts>_ReplaceFaWorkStepIsCompletedWithStatus.cs` und ersetze `Up`/`Down` vollständig durch (der Generator dropt `IsCompleted` VOR jeder Konvertierung — das muss korrigiert werden, Muster wie `ReplaceIsFinalShortageWithShortageStatus`):
```csharp
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "FaWorkSteps",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"
                UPDATE [dbo].[FaWorkSteps]
                SET [Status] = CASE WHEN [IsCompleted] = 1 THEN 2 ELSE 0 END;
            ");

            migrationBuilder.DropColumn(
                name: "IsCompleted",
                table: "FaWorkSteps");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCompleted",
                table: "FaWorkSteps",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(@"
                UPDATE [dbo].[FaWorkSteps]
                SET [IsCompleted] = CASE WHEN [Status] = 2 THEN 1 ELSE 0 END;
            ");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "FaWorkSteps");
        }
```
(Die `using`-Zeile + Klassengerüst des Generators unverändert lassen.)

- [ ] **Step 16: Idempotentes SQL-Skript SQL/76**

`SQL/76_ReplaceFaWorkStepIsCompletedWithStatus.sql` (ersetze `<ts>` durch die echte MigrationId aus Step 14):
```sql
-- =============================================
-- SQL/76 — FA-Vorbau v1.24.0
-- FaWorkSteps.IsCompleted (bit) -> Status (int): 0=Offen, 1=InBearbeitung, 2=Fertig.
-- Daten-Konvertierung: IsCompleted=1 -> Fertig(2), sonst Offen(0). Idempotent.
-- =============================================

IF COL_LENGTH('dbo.FaWorkSteps', 'Status') IS NULL
BEGIN
    ALTER TABLE [dbo].[FaWorkSteps]
        ADD [Status] INT NOT NULL CONSTRAINT DF_FaWorkSteps_Status DEFAULT 0;
    PRINT 'Spalte FaWorkSteps.Status hinzugefuegt.';
END
GO

IF COL_LENGTH('dbo.FaWorkSteps', 'IsCompleted') IS NOT NULL
BEGIN
    UPDATE [dbo].[FaWorkSteps]
        SET [Status] = CASE WHEN [IsCompleted] = 1 THEN 2 ELSE 0 END;
    PRINT 'FaWorkSteps.Status aus IsCompleted konvertiert.';

    DECLARE @c NVARCHAR(200) = (
        SELECT dc.name FROM sys.default_constraints dc
        INNER JOIN sys.columns c ON dc.parent_object_id = c.object_id
            AND dc.parent_column_id = c.column_id
        WHERE c.object_id = OBJECT_ID('[dbo].[FaWorkSteps]') AND c.name = 'IsCompleted');
    IF @c IS NOT NULL EXEC('ALTER TABLE [dbo].[FaWorkSteps] DROP CONSTRAINT [' + @c + ']');

    ALTER TABLE [dbo].[FaWorkSteps] DROP COLUMN [IsCompleted];
    PRINT 'Spalte FaWorkSteps.IsCompleted entfernt.';
END
GO

IF NOT EXISTS (SELECT * FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '<ts>_ReplaceFaWorkStepIsCompletedWithStatus')
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('<ts>_ReplaceFaWorkStepIsCompletedWithStatus', '10.0.2');
GO
```

- [ ] **Step 17: FreshInstall synchronisieren**

`SQL/00_FreshInstall.sql` — in der FaWorkSteps-Tabelle (Abschnitt „8d. FA-Vorbau") ersetze:
```sql
        [IsCompleted]       BIT               NOT NULL,
```
durch:
```sql
        [Status]            INT               NOT NULL CONSTRAINT DF_FaWorkSteps_Status DEFAULT 0,
```
Im `__EFMigrationsHistory`-Block (nach dem `20260625061803_ReplaceUserDefaultWorkplaceWithWorkbenches`-Insert, vor dem abschließenden `GO`) ergänze (mit echter MigrationId):
```sql
IF NOT EXISTS (SELECT * FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '<ts>_ReplaceFaWorkStepIsCompletedWithStatus')
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('<ts>_ReplaceFaWorkStepIsCompletedWithStatus', '10.0.2');
```

- [ ] **Step 18: Web-Projekt-Build erneut grün**

Run:
```powershell
dotnet build IdealAkeWms\IdealAkeWms.csproj
```
Expected: Build erfolgreich, keine `PendingModelChangesWarning`.

- [ ] **Step 19: Commit**

```powershell
Set-Location 'C:\Git\IDEAL-AKE-WMS\.claude\worktrees\missingparts-include-pd'
git add IdealAkeWms SQL
git commit -m @'
feat(fa-vorbau): FaWorkStep Erledigt-Status als 3-State (Offen/in Bearbeitung/Fertig)

IsCompleted (bool) -> Status (FaWorkStepStatus). Dropdown statt Checkbox in
Abarbeitungsliste + Leitstand-VK-VA, gemeinsames Partial + JS, set-status-API.
Migration 76 (IsCompleted=1 -> Fertig) + SQL/76 + FreshInstall.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
'@
```

---

## Task 2: Change 1 — Tests anpassen + 3-State-Verhalten absichern

**Files:**
- Modify: `IdealAkeWms.Tests/Repositories/FaWorkStepRepositoryTests.cs`
- Modify: `IdealAkeWms.Tests/Controllers/FaWorkStepsApiControllerTests.cs`
- Modify: `IdealAkeWms.Tests/Controllers/FaWorklistControllerTests.cs`
- Modify: `IdealAkeWms.Tests/Controllers/PickingLeitstandControllerTests.cs`
- Modify: `IdealAkeWms.Tests/Controllers/FaCompletionControllerTests.cs`

- [ ] **Step 1: Repo-Tests umstellen**

`FaWorkStepRepositoryTests.cs`:
- In `GetWorkStepDetailPivot_ReturnsActiveCellsWithCompletion`: ersetze die drei `IsCompleted = …`-Initialisierungen durch `Status = …`:
  - `var ve = new FaWorkStep { ProductionOrderId = 1, WorkStepId = 10, IsRemoved = false, Status = FaWorkStepStatus.Fertig };`
  - `var vl = new FaWorkStep { ProductionOrderId = 1, WorkStepId = 11, IsRemoved = false, Status = FaWorkStepStatus.Offen };`
  - `var vt = new FaWorkStep { ProductionOrderId = 1, WorkStepId = 12, IsRemoved = true, Status = FaWorkStepStatus.Offen };`
  - Assertions: `pivot[1]["VE"].IsCompleted.Should().BeTrue();` → `pivot[1]["VE"].Status.Should().Be(FaWorkStepStatus.Fertig);` und `pivot[1]["VL"].IsCompleted.Should().BeFalse();` → `pivot[1]["VL"].Status.Should().Be(FaWorkStepStatus.Offen);`
- Ersetze den gesamten Test `SetIsCompleted_SetsAuditAndCompletedFields` durch:
```csharp
    [Fact]
    public async Task SetStatus_Fertig_SetsAuditAndCompletedFields()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new FaWorkStepRepository(ctx);
        ctx.ProductionOrders.Add(new ProductionOrder { Id = 1, OrderNumber = "FA1" });
        ctx.WorkSteps.Add(new WorkStep { Id = 10, Code = "VL", Name = "L" });
        var row = new FaWorkStep { ProductionOrderId = 1, WorkStepId = 10 };
        ctx.FaWorkSteps.Add(row);
        await ctx.SaveChangesAsync();

        await repo.SetStatusAsync(row.Id, FaWorkStepStatus.Fertig, "tester", "win\\tester");

        var reloaded = await ctx.FaWorkSteps.FindAsync(row.Id);
        reloaded!.Status.Should().Be(FaWorkStepStatus.Fertig);
        reloaded.CompletedAt.Should().NotBeNull();
        reloaded.CompletedBy.Should().Be("tester");
    }

    [Fact]
    public async Task SetStatus_InBearbeitung_ClearsCompletedFields()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new FaWorkStepRepository(ctx);
        ctx.ProductionOrders.Add(new ProductionOrder { Id = 1, OrderNumber = "FA1" });
        ctx.WorkSteps.Add(new WorkStep { Id = 10, Code = "VL", Name = "L" });
        var row = new FaWorkStep { ProductionOrderId = 1, WorkStepId = 10, Status = FaWorkStepStatus.Fertig, CompletedAt = DateTime.Now, CompletedBy = "x" };
        ctx.FaWorkSteps.Add(row);
        await ctx.SaveChangesAsync();

        await repo.SetStatusAsync(row.Id, FaWorkStepStatus.InBearbeitung, "tester", "win\\tester");

        var reloaded = await ctx.FaWorkSteps.FindAsync(row.Id);
        reloaded!.Status.Should().Be(FaWorkStepStatus.InBearbeitung);
        reloaded.CompletedAt.Should().BeNull();
        reloaded.CompletedBy.Should().BeNull();
    }
```
- In `SetIsSpecComplete_SetsSpecFieldsNotWorkDone`: ersetze `reloaded.IsCompleted.Should().BeFalse(); // Arbeit-erledigt unberuehrt` durch `reloaded.Status.Should().Be(FaWorkStepStatus.Offen); // Arbeit-erledigt unberuehrt`.
- In `GetCounts_SpecCompleteCount_CountsSpecCompleteNotWorkDone`: ersetze `IsCompleted = false`/`IsCompleted = true` in den beiden `new FaWorkStep { … }` durch `Status = FaWorkStepStatus.Offen` bzw. `Status = FaWorkStepStatus.Fertig`.

- [ ] **Step 2: API-Tests umstellen**

`FaWorkStepsApiControllerTests.cs` — ersetze den Test `ToggleCompleted_SetsIsCompleted` durch zwei Tests:
```csharp
    [Fact]
    public async Task SetStatus_SetsStatus_ForValidValue()
    {
        _faWorkSteps.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new FaWorkStep
        {
            Id = 5,
            ProductionOrderId = 1,
            WorkStepId = 10
        });

        var result = await _controller.SetStatus(
            new FaWorkStepsApiController.SetStatusRequest(5, 2));

        result.Should().BeOfType<OkResult>();
        _faWorkSteps.Verify(r => r.SetStatusAsync(
            5, FaWorkStepStatus.Fertig, "TestUser", "DOMAIN\\testuser"), Times.Once);
    }

    [Fact]
    public async Task SetStatus_ReturnsBadRequest_ForInvalidValue()
    {
        var result = await _controller.SetStatus(
            new FaWorkStepsApiController.SetStatusRequest(5, 9));

        result.Should().BeOfType<BadRequestObjectResult>();
        _faWorkSteps.Verify(r => r.SetStatusAsync(
            It.IsAny<int>(), It.IsAny<FaWorkStepStatus>(),
            It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
```
(`using IdealAkeWms.Models;` ist bereits vorhanden.)

- [ ] **Step 3: FaWorklistController-Tests umstellen + InBearbeitung-Sichtbarkeit**

`FaWorklistControllerTests.cs`:
- Im Helper `SeedFaWorkStep` ersetze `IsCompleted = isCompleted,` durch `Status = isCompleted ? FaWorkStepStatus.Fertig : FaWorkStepStatus.Offen,`.
- Ergänze einen neuen Test, der absichert, dass InBearbeitung NICHT ausblendet (Pattern wie die bestehenden Index-Tests dieser Datei — `Build()`, `SeedWorkplace`, `SeedWorkStep`, `SeedOrder`, `SeedFaWorkStep`; orientiere dich am vorhandenen Hide-Test). Der neue Test setzt einen FaWorkStep auf Status InBearbeitung (z. B. via direkter `ctx.FaWorkSteps`-Mutation oder einer kleinen Erweiterung des Seed-Helpers) und prüft, dass die FA bei `showDone:false` in `vm.Items` enthalten ist. Falls der bestehende `SeedFaWorkStep`-bool das nicht hergibt, eine zusätzliche Status-Variante im Helper ergänzen (Parameter `FaWorkStepStatus? status = null`, das `isCompleted` überschreibt).

- [ ] **Step 4: PickingLeitstandController-Tests umstellen**

`PickingLeitstandControllerTests.cs` — im Pivot-Setup die `FaWorkStepPivotCell(id, bool)` auf `FaWorkStepPivotCell(id, FaWorkStepStatus)` umstellen und die Assertions anpassen:
- `new FaWorkStepPivotCell(101, true)` → `new FaWorkStepPivotCell(101, FaWorkStepStatus.Fertig)`
- `new FaWorkStepPivotCell(103, false)` → `new FaWorkStepPivotCell(103, FaWorkStepStatus.Offen)`
- `new FaWorkStepPivotCell(202, true)` → `new FaWorkStepPivotCell(202, FaWorkStepStatus.Fertig)`
- `new FaWorkStepPivotCell(204, false)` → `new FaWorkStepPivotCell(204, FaWorkStepStatus.Offen)`
- `new FaWorkStepPivotCell(205, true)` → `new FaWorkStepPivotCell(205, FaWorkStepStatus.Fertig)`
- Assertions: alle `…WorkSteps["XX"].IsCompleted.Should().BeTrue();` → `…WorkSteps["XX"].Status.Should().Be(FaWorkStepStatus.Fertig);` und `…IsCompleted.Should().BeFalse();` → `…Status.Should().Be(FaWorkStepStatus.Offen);` (Kommentare „erledigt/offen" passen weiter). Den Kommentar in Zeile „Detail-Pivot: Code -> Cell(FaWorkStepId, IsCompleted)…" auf `Status` anpassen.
- Ggf. fehlendes `using IdealAkeWms.Models;` ergänzen.

- [ ] **Step 5: FaCompletionController-Tests umstellen**

`FaCompletionControllerTests.cs`:
- Im Seed-Helper (um Zeile 97) `IsCompleted = isCompleted,` durch `Status = isCompleted ? FaWorkStepStatus.Fertig : FaWorkStepStatus.Offen,` ersetzen.
- Die Assertion (um Zeile 836-837) `reloaded.IsCompleted.Should().BeFalse();` durch `reloaded.Status.Should().Be(FaWorkStepStatus.Offen);` ersetzen.
- Ggf. fehlendes `using IdealAkeWms.Models;` ergänzen.

- [ ] **Step 6: Solution-Build + alle Tests grün**

Run:
```powershell
dotnet build
dotnet test
```
Expected: Build + alle Tests grün.

- [ ] **Step 7: Commit**

```powershell
Set-Location 'C:\Git\IDEAL-AKE-WMS\.claude\worktrees\missingparts-include-pd'
git add IdealAkeWms.Tests
git commit -m @'
test(fa-vorbau): FaWorkStep-Status-Tests auf 3-State umstellen + InBearbeitung-Sichtbarkeit

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
'@
```

---

## Task 3: Change 2 — Beschichtungstermin in der Abarbeitungsliste

**Files:**
- Create: `IdealAkeWms/Services/CoatingDateCalculator.cs`
- Create: `IdealAkeWms.Tests/Services/CoatingDateCalculatorTests.cs`
- Modify: `IdealAkeWms/Controllers/PickingLeitstandController.cs`
- Modify: `IdealAkeWms/Controllers/FaWorklistController.cs`
- Modify: `IdealAkeWms/Models/ViewModels/FaWorklistViewModel.cs`
- Modify: `IdealAkeWms/Views/FaWorklist/Index.cshtml`

- [ ] **Step 1: Failing Test — CoatingDateCalculator**

`IdealAkeWms.Tests/Services/CoatingDateCalculatorTests.cs`:
```csharp
using FluentAssertions;
using IdealAkeWms.Services;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Services;

public class CoatingDateCalculatorTests
{
    private static Mock<IBusinessDayService> MockDays(DateTime raw, DateTime pickup)
    {
        var m = new Mock<IBusinessDayService>();
        m.Setup(s => s.SubtractBusinessDays(It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<HashSet<DateTime>>()))
            .Returns(raw);
        m.Setup(s => s.FindPreviousPickupDay(It.IsAny<DateTime>(), It.IsAny<HashSet<DayOfWeek>>()))
            .Returns(pickup);
        return m;
    }

    [Fact]
    public void FeatureInactive_ComputesForAll_RegardlessOfCoatingParts()
    {
        var pickup = new DateTime(2026, 6, 16);
        var m = MockDays(new DateTime(2026, 6, 18), pickup);

        var result = CoatingDateCalculator.Compute(
            new DateTime(2026, 7, 1), 10, new HashSet<DateTime>(), new HashSet<DayOfWeek>(),
            hasCoatingParts: false, featureActive: false, m.Object);

        result.Should().Be(pickup);
    }

    [Fact]
    public void FeatureActive_NoCoatingParts_ReturnsNull()
    {
        var m = MockDays(new DateTime(2026, 6, 18), new DateTime(2026, 6, 16));

        var result = CoatingDateCalculator.Compute(
            new DateTime(2026, 7, 1), 10, new HashSet<DateTime>(), new HashSet<DayOfWeek>(),
            hasCoatingParts: false, featureActive: true, m.Object);

        result.Should().BeNull();
    }

    [Fact]
    public void FeatureActive_WithCoatingParts_ComputesDate()
    {
        var pickup = new DateTime(2026, 6, 16);
        var m = MockDays(new DateTime(2026, 6, 18), pickup);

        var result = CoatingDateCalculator.Compute(
            new DateTime(2026, 7, 1), 10, new HashSet<DateTime>(), new HashSet<DayOfWeek>(),
            hasCoatingParts: true, featureActive: true, m.Object);

        result.Should().Be(pickup);
    }

    [Fact]
    public void NullVorkommissionierTermin_ReturnsNull()
    {
        var m = MockDays(new DateTime(2026, 6, 18), new DateTime(2026, 6, 16));

        var result = CoatingDateCalculator.Compute(
            null, 10, new HashSet<DateTime>(), new HashSet<DayOfWeek>(),
            hasCoatingParts: true, featureActive: false, m.Object);

        result.Should().BeNull();
    }
}
```

- [ ] **Step 2: Test rot verifizieren**

Run:
```powershell
dotnet test --filter FullyQualifiedName~CoatingDateCalculatorTests
```
Expected: FAIL (CoatingDateCalculator existiert nicht / kompiliert nicht).

- [ ] **Step 3: Helper implementieren**

`IdealAkeWms/Services/CoatingDateCalculator.cs`:
```csharp
namespace IdealAkeWms.Services;

/// <summary>
/// Beschichtungstermin = VorkommissionierTermin - BeschichtungTage (Arbeitstage), dann auf den
/// vorherigen Abholtag. Backward-Compat: Feature inaktiv (<paramref name="featureActive"/>=false)
/// => Termin fuer ALLE Auftraege; aktiv => nur wenn <paramref name="hasCoatingParts"/>.
/// Gemeinsam genutzt von Leitstand + FA-Abarbeitungsliste.
/// </summary>
public static class CoatingDateCalculator
{
    public static DateTime? Compute(
        DateTime? vorkommissionierTermin,
        int beschichtungTage,
        HashSet<DateTime> holidays,
        HashSet<DayOfWeek> pickupDays,
        bool hasCoatingParts,
        bool featureActive,
        IBusinessDayService businessDayService)
    {
        if (!vorkommissionierTermin.HasValue) return null;
        if (featureActive && !hasCoatingParts) return null;

        var raw = businessDayService.SubtractBusinessDays(vorkommissionierTermin.Value, beschichtungTage, holidays);
        return businessDayService.FindPreviousPickupDay(raw, pickupDays);
    }
}
```

- [ ] **Step 4: Test grün verifizieren**

Run:
```powershell
dotnet test --filter FullyQualifiedName~CoatingDateCalculatorTests
```
Expected: PASS (4/4).

- [ ] **Step 5: Leitstand auf den Helper refactoren (Verhalten unverändert)**

`IdealAkeWms/Controllers/PickingLeitstandController.cs` — ersetze den Coating-Block (innerhalb `if (o.ProductionDate.HasValue)`):
```csharp
                // Backward compat: when feature is inactive (setting empty), calculate for ALL orders
                // When feature is active, only calculate if HasCoatingParts == true
                if (!coatingFeatureActive || (ps?.HasCoatingParts ?? false))
                {
                    // Beschichtungstermin: Baugruppentermin - BeschichtungTage, dann auf vorherigen Abholtag
                    var rawBeschichtung = _businessDayService.SubtractBusinessDays(
                        item.VorkommissionierTermin.Value, beschichtungTage, holidays);
                    item.BeschichtungTermin = _businessDayService.FindPreviousPickupDay(rawBeschichtung, pickupDays);
                }
                // else: leave BeschichtungTermin null
```
durch:
```csharp
                item.BeschichtungTermin = CoatingDateCalculator.Compute(
                    item.VorkommissionierTermin, beschichtungTage, holidays, pickupDays,
                    ps?.HasCoatingParts ?? false, coatingFeatureActive, _businessDayService);
```

- [ ] **Step 6: ViewModel — BeschichtungTermin auf FaWorklistRow**

`IdealAkeWms/Models/ViewModels/FaWorklistViewModel.cs` — in `FaWorklistRow` nach `public DateTime? VorkommissionierTermin { get; set; }  // BG-Termin` ergänzen:
```csharp
    public DateTime? BeschichtungTermin { get; set; }
```

- [ ] **Step 7: FaWorklistController — Coating berechnen + ColumnMap**

`IdealAkeWms/Controllers/FaWorklistController.cs` — nach den vorhandenen Settings-Loads in Schritt 5 (`kommissionierTage`/`vorkommissionierTage`/`holidays`) ergänzen:
```csharp
        var beschichtungTage = await _settingRepository.GetIntValueAsync("BeschichtungTage", 10);
        var beschichtungAbholtageSetting = await _settingRepository.GetValueAsync(AppSettingKeys.BeschichtungAbholtage) ?? "Dienstag,Donnerstag";
        var pickupDays = _businessDayService.ParsePickupDays(beschichtungAbholtageSetting);
        var lackierteilName = await _settingRepository.GetValueAsync(AppSettingKeys.LackierteilKategorieName);
        var coatingFeatureActive = !string.IsNullOrWhiteSpace(lackierteilName);
```
Im `if (order.ProductionDate.HasValue)`-Block, direkt nach der `row.VorkommissionierTermin = …`-Zuweisung, ergänzen:
```csharp
                row.BeschichtungTermin = CoatingDateCalculator.Compute(
                    row.VorkommissionierTermin, beschichtungTage, holidays, pickupDays,
                    order.PickingStatus?.HasCoatingParts ?? false, coatingFeatureActive, _businessDayService);
```
In `BuildColumnMap` nach der Zeile `["bg-date"] = …` ergänzen (Key wie im Leitstand: `coating-date`):
```csharp
            ["coating-date"] = r => FormatDateForFilter(r.BeschichtungTermin),
```

- [ ] **Step 8: FaWorklist-View — Beschicht.-Spalte (vor BG-Termin)**

`IdealAkeWms/Views/FaWorklist/Index.cshtml`:
- `columnCount` erhöhen: ersetze `var columnCount = 9 + Model.AttributeColumns.Count + 1;` durch `var columnCount = 10 + Model.AttributeColumns.Count + 1;` und passe den Kommentar darüber an (FA-Nr, Werkbank, Artikel, Bez1, Bez2, Stk, **Beschicht.**, BG, Komm, Fert).
- `<thead>`: vor `<th … data-col-key="bg-date" …>BG-Termin</th>` einfügen:
```cshtml
                    <th class="text-nowrap" data-filterable data-col-key="coating-date" data-date-filter>Beschicht.</th>
```
- Datenzeile: vor `<td class="text-nowrap">@FormatDateWithKw(item.VorkommissionierTermin)</td>` einfügen:
```cshtml
                            <td class="text-nowrap">@FormatDateWithKw(item.BeschichtungTermin)</td>
```
- `#column-config`-JSON: vor `{ "key": "bg-date", … }` einfügen:
```cshtml
        { "key": "coating-date", "label": "Beschicht.", "locked": false, "defaultWidth": null },
```

- [ ] **Step 9: FaWorklistController-Test für Coating-Spalte**

`IdealAkeWms.Tests/Controllers/FaWorklistControllerTests.cs` — einen Test ergänzen, der prüft, dass bei gesetztem `ProductionDate` der `BeschichtungTermin` der gerenderten Row nicht null ist (Feature inaktiv = LackierteilKategorieName leer → für alle berechnet). Nutze die bestehenden Seed-Helper + `Build()`. Mindest-Assertion: `vm.Items.Single().BeschichtungTermin.Should().NotBeNull();` (Vorbedingung: ProductionDate gesetzt, kein LackierteilKategorieName-Setting). Orientiere dich am bestehenden Index-Termin-Test der Datei.

- [ ] **Step 10: Build + Tests grün**

Run:
```powershell
dotnet build
dotnet test
```
Expected: grün.

- [ ] **Step 11: Commit**

```powershell
Set-Location 'C:\Git\IDEAL-AKE-WMS\.claude\worktrees\missingparts-include-pd'
git add IdealAkeWms IdealAkeWms.Tests
git commit -m @'
feat(fa-vorbau): Beschichtungstermin in der Abarbeitungsliste

CoatingDateCalculator extrahiert (DRY Leitstand + Abarbeitungsliste); neue
filterbare Spalte "Beschicht." (coating-date) in FaWorklist.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
'@
```

---

## Task 4: Change 3 — Server-Mode-Spaltenfilter erst bei ENTER

**Files:**
- Modify: `IdealAkeWms/wwwroot/js/table-filter.js`

> Reines JS, kein Backend. Verifikation manuell (Build sicherstellt nur, dass nichts anderes bricht). Client-Mode bleibt unverändert (live). Kalender/Clear/`setColumnFilter` müssen weiterhin SOFORT wirken.

- [ ] **Step 1: `scheduleServerNavigate` → `applyServerFilters` + `applyColumnFilterNow`**

Ersetze die Funktion `scheduleServerNavigate` (der gesamte Block) durch:
```javascript
    function applyServerFilters() {
        try {
            var filters = window.getActiveFilters();
            var url = new URL(window.location.href);
            Array.from(url.searchParams.keys())
                .filter(function (k) { return k.indexOf('colf_') === 0; })
                .forEach(function (k) { url.searchParams.delete(k); });
            Object.keys(filters).forEach(function (colKey) {
                if (filters[colKey]) url.searchParams.set('colf_' + colKey, filters[colKey]);
            });
            url.searchParams.delete('page');
            window.location.href = url.toString();
        } catch (e) { /* */ }
    }

    // ENTER im Server-Mode loest die Navigation aus (Tippen tut es NICHT mehr).
    function onServerFilterKeydown(e) {
        if (e.key === 'Enter') {
            e.preventDefault();
            applyServerFilters();
        }
    }

    // Einheitlicher Trigger fuer programmatische Aenderungen (Kalender, Clear, setColumnFilter):
    // Server-Mode navigiert sofort, Client-Mode filtert live.
    function applyColumnFilterNow() {
        if (isServerColumnFilter()) {
            applyServerFilters();
        } else {
            applyFilters();
        }
    }
```
Entferne außerdem die nun ungenutzte Variable `var _serverFilterTimer = null;` (oben im IIFE).

- [ ] **Step 2: Date-Spalten-Input — ENTER statt debounced input (Server-Mode)**

Ersetze (im Date-Col-Zweig):
```javascript
                    // Server-Filter-Mode: auch Date-Spalten triggern URL-Navigation —
                    // Controller matched gegen das gerenderte Format "dd.MM.yyyy KWxx".
                    if (isServerColumnFilter()) {
                        input.addEventListener('input', scheduleServerNavigate);
                    } else {
                        input.addEventListener('input', applyFilters);
                    }
```
durch:
```javascript
                    // Server-Filter-Mode: Tippen navigiert NICHT (sonst Reload mitten im Tippen) —
                    // erst ENTER. Kalender-Auswahl wirkt weiterhin sofort.
                    if (isServerColumnFilter()) {
                        input.addEventListener('keydown', onServerFilterKeydown);
                    } else {
                        input.addEventListener('input', applyFilters);
                    }
```

- [ ] **Step 3: Text-Spalten-Input — ENTER statt debounced input (Server-Mode)**

Ersetze (im Text-Col-Zweig):
```javascript
                    // Server-Filter-Mode: Text-Spalten triggern debounced URL-Navigation
                    // (Date-Filter laufen weiterhin clientseitig — Komplexitaet KW/Kalender).
                    if (isServerColumnFilter()) {
                        input.addEventListener('input', scheduleServerNavigate);
                    } else {
                        input.addEventListener('input', applyFilters);
                    }
```
durch:
```javascript
                    // Server-Filter-Mode: Tippen navigiert NICHT — erst ENTER.
                    if (isServerColumnFilter()) {
                        input.addEventListener('keydown', onServerFilterKeydown);
                    } else {
                        input.addEventListener('input', applyFilters);
                    }
```

- [ ] **Step 4: Kalender-KW-Klick — sofort anwenden**

Ersetze im KW-Zellen-Click-Handler:
```javascript
                        input.value = 'KW' + kwVal;
                        input.dispatchEvent(new Event('input', { bubbles: true }));
                        closeDatePicker();
```
durch:
```javascript
                        input.value = 'KW' + kwVal;
                        applyColumnFilterNow();
                        closeDatePicker();
```

- [ ] **Step 5: Kalender-Tag-Klick — sofort anwenden**

Ersetze im Tages-Zellen-Click-Handler:
```javascript
                                input.value = formatted;
                                input.dispatchEvent(new Event('input', { bubbles: true }));
                                closeDatePicker();
```
durch:
```javascript
                                input.value = formatted;
                                applyColumnFilterNow();
                                closeDatePicker();
```

- [ ] **Step 6: „Filter entfernen" — sofort anwenden**

Ersetze im Clear-Button-Handler:
```javascript
                input.value = '';
                input.dispatchEvent(new Event('input', { bubbles: true }));
                closeDatePicker();
```
durch:
```javascript
                input.value = '';
                applyColumnFilterNow();
                closeDatePicker();
```

- [ ] **Step 7: `window.setColumnFilter` — sofort anwenden**

Ersetze:
```javascript
        if (input) {
            input.value = value;
            input.dispatchEvent(new Event('input', { bubbles: true }));
        }
```
durch:
```javascript
        if (input) {
            input.value = value;
            applyColumnFilterNow();
        }
```

- [ ] **Step 8: Build grün (keine JS-Regression im Build)**

Run:
```powershell
dotnet build IdealAkeWms\IdealAkeWms.csproj
```
Expected: Build erfolgreich. (JS wird nicht kompiliert — Build dient nur als Sanity-Check.)

- [ ] **Step 9: Commit**

```powershell
Set-Location 'C:\Git\IDEAL-AKE-WMS\.claude\worktrees\missingparts-include-pd'
git add IdealAkeWms\wwwroot\js\table-filter.js
git commit -m @'
feat(filter): Server-Mode-Spaltenfilter erst bei ENTER statt beim Tippen

Tippen navigiert nicht mehr (kein Reload mitten im Tippen); ENTER loest aus.
Kalender/Clear/setColumnFilter wirken weiterhin sofort. Client-Mode unveraendert.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
'@
```

---

## Task 5: Doku + Versionierung (v1.24.0)

**Files:**
- Modify: `IdealAkeWms/AppVersion.cs`, `IDEALAKEWMSService/AppVersion.cs`
- Modify: `IdealAkeWms/Views/Help/Changelog.cshtml`
- Modify: `IdealAkeWms/Views/Help/Index.cshtml`
- Modify: `docs/TESTSZENARIEN.md`
- Modify: `PROJECT_STATUS.md`
- Modify: `CLAUDE.md`

- [ ] **Step 1: Version bumpen**

`IdealAkeWms/AppVersion.cs` und `IDEALAKEWMSService/AppVersion.cs` — beide auf:
```csharp
    public const string Version = "1.24.0";
    public const string Date = "2026-06-30";
```

- [ ] **Step 2: Changelog-Karte v1.24.0**

`IdealAkeWms/Views/Help/Changelog.cshtml` — neue Versions-Karte ganz oben (Struktur der jüngsten vorhandenen Karte kopieren) mit Titel „Version 1.24.0 (2026-06-30)" und drei Punkten:
- „FA-Vorbau-Erledigt-Status ist jetzt ein 3-Wert-Status (**Offen / in Bearbeitung / Fertig**) als Auswahlfeld — in der FA-Abarbeitungsliste und im Leitstand (VK-VA). Nur ‚Fertig' blendet eine FA aus der Abarbeitungsliste aus."
- „Die FA-Abarbeitungsliste zeigt jetzt zusätzlich den **Beschichtungstermin** (filterbare Spalte)."
- „Spaltenfilter in großen Listen (Server-Modus) filtern erst bei **ENTER** statt beim Tippen — kein störender Seiten-Reload mehr mitten in der Eingabe."

- [ ] **Step 3: Hilfeseite**

`IdealAkeWms/Views/Help/Index.cshtml` — kurzer Hinweis im FA-Vorbau-/Abarbeitungs-Abschnitt: Status-Auswahl (Offen/in Bearbeitung/Fertig) statt Haken; Beschichtungstermin-Spalte; in den großen Listen filtern Spaltenfilter erst nach ENTER.

- [ ] **Step 4: TESTSZENARIEN**

`docs/TESTSZENARIEN.md` — neues Kapitel mit drei Szenarien:
1. **3-State-Status:** In Abarbeitungsliste eine FA von Offen→in Bearbeitung (bleibt sichtbar, kein Ausblenden) →Fertig (verschwindet bei „Erledigte" aus). DB: `SELECT Status, CompletedAt, CompletedBy FROM FaWorkSteps` — Fertig setzt CompletedAt/By, Offen/InBearbeitung = NULL. Im Leitstand dasselbe Dropdown je VK-VA; ein Wechsel dort wirkt sofort (AJAX) und spiegelt sich in der Abarbeitungsliste. Negativ: ungültiger Status-POST → 400.
2. **Beschichtungstermin:** Abarbeitungsliste zeigt die Spalte „Beschicht."; bei leerem `LackierteilKategorieName` für alle FAs gefüllt, bei gesetztem nur für FAs mit Lackierteilen; Filter per KW/Datum funktioniert; identischer Wert wie im Leitstand für dieselbe FA.
3. **Filter-ENTER:** In einer Server-Mode-Liste (z. B. FA-Liste) im Spaltenfilter tippen → KEIN Reload; ENTER → Liste filtert. Kalender-KW-Klick → sofort gefiltert; „Filter entfernen" → sofort. Client-Mode-Liste (Tracking/ByWorkplace) bleibt live.

- [ ] **Step 5: PROJECT_STATUS**

`PROJECT_STATUS.md` — Eintrag v1.24.0: 3-State-Status (Migration 76, daten-konvertierend), Beschichtungstermin in Abarbeitungsliste, Filter-ENTER (Server-Mode). Hinweis: DB-Backup vor Deploy (Migration 76 dropt `IsCompleted`).

- [ ] **Step 6: CLAUDE.md Fallstricke**

`CLAUDE.md` — zwei Bullets ergänzen:
- Unter „Bekannte Fallstricke": **FaWorkStep-Erledigt = 3-State `FaWorkStepStatus` (seit v1.24.0)** — `FaWorkStep.IsCompleted` (bool) ersetzt durch `Status` (Offen=0/InBearbeitung=1/Fertig=2); Migration 76 daten-konvertierend (IsCompleted=1→Fertig) + dropt die Spalte (Down() verliert Offen/InBearbeitung-Unterschied → DB-Backup vor Deploy). Schreib-Pfad: API `/api/fa-work-steps/set-status {faWorkStepId, status}` → `SetStatusAsync` (CompletedAt/By nur bei Fertig). Ausblende-Logik FaWorklist = `Status == Fertig` (InBearbeitung bleibt sichtbar). Dropdown in Abarbeitungsliste + Leitstand via Partial `_FaWorkStepStatusSelect` + JS `fa-work-step-status.js`. Pivot-Zelle `FaWorkStepPivotCell(FaWorkStepId, Status)`. VK-VA-Spaltenfilter-Text: „offen/in bearbeitung/fertig". Der alte `toggle-completed`-Endpoint + die `.worklist-complete`/`.toggle-field`-Checkboxen sind entfallen.
- Beschichtungstermin: Formel jetzt in `CoatingDateCalculator.Compute(...)` (DRY Leitstand + Abarbeitungsliste); Abarbeitungsliste hat Spalte `coating-date`.
- Im Abschnitt „Pagination & Server-Side Spaltenfilter": ergänzen, dass Server-Mode-Spaltenfilter seit v1.24.0 erst bei **ENTER** navigieren (nicht mehr debounced beim Tippen); Kalender/Clear/`setColumnFilter` rufen `applyColumnFilterNow()` und wirken sofort; Client-Mode unverändert live.

- [ ] **Step 7: Build + Tests grün + Commit**

Run:
```powershell
dotnet build
dotnet test
```
Expected: grün.
```powershell
Set-Location 'C:\Git\IDEAL-AKE-WMS\.claude\worktrees\missingparts-include-pd'
git add -A
git commit -m @'
docs(v1.24.0): Changelog/Help/TESTSZENARIEN/PROJECT_STATUS/CLAUDE + Version-Bump

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
'@
```

---

## Task 6: Final-Check + Code-Review (PAUSE — kein Merge)

**Files:** keine Änderungen (außer ggf. Review-Fixes).

- [ ] **Step 1: Vollständiger Build + Tests**

Run:
```powershell
dotnet build
dotnet test
```
Expected: Build + alle Tests grün.

- [ ] **Step 2: Grep-Restcheck — kein `IsCompleted`/`toggle-completed`/`worklist-complete` mehr im Produktivcode**

Mit dem Grep-Tool prüfen, dass im Produktivcode (`IdealAkeWms/**`, ohne `Migrations/**`, ohne Doku) kein `FaWorkStep`-`IsCompleted`, kein `/api/fa-work-steps/toggle-completed`, kein `.worklist-complete` und kein `SetIsCompletedAsync` mehr referenziert wird. (Historische Migrationsskripte `SQL/68`, `SQL/69` + Migrations-`.cs` bleiben unverändert — die beschreiben den damaligen Stand.)

- [ ] **Step 3: Zwei-Stufen-Review**

Spec-Compliance-Review (gegen die Spec) + Code-Quality-Review der gesamten Branch-Diff seit Commit `16e3c80` (Spec-Commit). Gefundene Critical/Important-Punkte fixen, dann erneut Build+Tests.

- [ ] **Step 4: PAUSE — auf User-Bestätigung warten**

**KEIN Merge nach main, kein Worktree-/Branch-Cleanup.** Dem User den fertigen Stand melden: drei Features umgesetzt, Migration 76, Build+Tests grün, manuelle UAT (TESTSZENARIEN) offen. Auf ausdrückliche Freigabe warten.

---

## Self-Review (gegen die Spec)

**1. Spec-Coverage:**
- Change 1 (Datenmodell, Migration 76, Repo/API, Ausblende-Logik, Views, Filtertext): Task 1 + Task 2. ✔
- Change 2 (BeschichtungTermin + Spalte + Helper): Task 3. ✔
- Change 3 (table-filter.js ENTER, Server-Mode only, Kalender/Clear sofort): Task 4. ✔
- Doku/Version (Changelog, CLAUDE, TESTSZENARIEN, Help, PROJECT_STATUS): Task 5. ✔
- Tests (Repo SetStatus, Pivot, Hide-Logik, API set-status, Coating-Helper): Task 2 + Task 3. ✔

**2. Placeholder-Scan:** Migrations-Timestamp `<ts>` ist ein zur Laufzeit ermittelter Wert (Step 14) mit klarer Anweisung, kein offener Platzhalter. Changelog/Help/TESTSZENARIEN geben konkrete Inhalte vor + verweisen auf die vorhandene Karten-/Kapitelstruktur als Vorlage (Datei ist der Template). Kein „TBD".

**3. Typ-Konsistenz:** `FaWorkStepStatus` (Models), `FaWorkStepPivotCell(int, FaWorkStepStatus)`, `SetStatusAsync(int, FaWorkStepStatus, string, string)`, `SetStatusRequest(int, int)`, `FaWorklistCell.Status`, `FaWorkStepStatusSelectViewModel.Status`, `CoatingDateCalculator.Compute(DateTime?, int, HashSet<DateTime>, HashSet<DayOfWeek>, bool, bool, IBusinessDayService)`, `FaWorklistRow.BeschichtungTermin`, JS-Klasse `.fa-status-select` + Endpoint `/api/fa-work-steps/set-status` + Body `{faWorkStepId, status}` — durchgängig konsistent über alle Tasks.

**4. Reihenfolge-Korrektheit:** Produktionscode (Task 1) muss vor `dotnet ef migrations add` kompilieren — deshalb alle Consumer + Views in Task 1, Web-Projekt-Build als Gate; Tests erst Task 2. Migration generieren → Body hand-editieren (Generator dropt sonst `IsCompleted` vor der Konvertierung) → SQL/76 + FreshInstall mit identischer MigrationId.
